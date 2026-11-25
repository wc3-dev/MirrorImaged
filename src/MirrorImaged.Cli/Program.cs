using System.Diagnostics;
using System.Globalization;
using MirrorImaged;
using MirrorImaged.Data;
using MirrorImaged.Lua;
using MirrorImaged.Natives;

const double StepsPerSecond = 1024;

// Asked for anywhere on the line, so no command takes --help for its map
if (args is ["help", ..] || args.Contains("--help") || args.Contains("-h"))
    return Ui.Help(args.FirstOrDefault(word => word is not ("help" or "--help" or "-h")));
if (args is ["--version"]) { Console.Out.Write($"mirror {System.Reflection.CustomAttributeExtensions.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>(typeof(Program).Assembly)!.InformationalVersion}\n"); return 0; }

if (args is ["coverage"])
{
    Ui.Coverage(Coverage.Of(typeof(GameNatives)));
    return 0;
}

// The commands that take a map or a recording, then --name value pairs
if (args is [var command and ("check" or "run" or "replay" or "desync" or "bench" or "fuzz" or "test" or "snapshot" or "compare" or "view"), var file, .. var rest])
{
    if (Flags(rest) is not { } flags) return Ui.Usage();
    double Number(string name, double fallback) => flags.TryGetValue(name, out var v) && double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : fallback;
    var (seconds, seed) = (Number("--seconds", command == "bench" ? 600 : 60), (int)Number("--seed", 0));
    Seats.Users = flags.TryGetValue("--users", out var users) ? [.. users.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse)] : null;
    Seats.Computers = flags.TryGetValue("--computers", out var computers) ? [.. computers.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse)] : null;
    Seats.Melee = flags.GetValueOrDefault("--melee") == "true";
    Seats.Graphics = flags.GetValueOrDefault("--graphics") == "classic" ? GraphicsMode.Classic : GraphicsMode.Reforged;
    try
    {
        switch (command)
        {
            case "check": return Check(file);
            case "run": return Run(file, flags, seconds, seed);
            case "replay": return PlayRecording(file, flags, Number("--hash", 0));
            case "desync": return DesyncCheck(file, seconds, seed, Seats.Graphics);
            case "bench": return Bench(file, seconds, seed);
            case "test": return Tests(file, flags.GetValueOrDefault("--junit"));
            case "snapshot": return Save(file, Number("--at", seconds), Number("--back", 0), seed, flags.GetValueOrDefault("--out") ?? "snapshot.json");
            case "view": return View(file, seconds, seed, Number("--every", 0.5), flags.GetValueOrDefault("--out"));
            case "compare": return Compare(file, flags.GetValueOrDefault("--map"));
            default: return Fuzz(file, seconds, seed, (int)Number("--orders", 1));
        }
    }
    catch (Exception e)
    {
        Ui.Error(e.Message);
        return 1;
    }
}

return Ui.Usage();

static Dictionary<string, string>? Flags(string[] options)
{
    var flags = new Dictionary<string, string>();
    for (var i = 0; i < options.Length; i += 2)
    {
        if (i + 1 >= options.Length) return null;
        var valid = options[i] switch
        {
            "--seconds" => double.TryParse(options[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) && seconds >= 0,
            "--hash" => double.TryParse(options[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out _),
            "--seed" or "--orders" => int.TryParse(options[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
            "--record" or "--input" or "--map" or "--junit" or "--out" or "--users" or "--computers" => true,
            "--melee" => options[i + 1] is "true" or "false",
            "--graphics" => options[i + 1] is "reforged" or "classic",
            "--at" or "--back" or "--every" => double.TryParse(options[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var at) && at >= 0,
            _ => false,
        };
        if (!valid) return null;
        flags[options[i]] = options[i + 1];
    }
    return flags;
}

static int Finish(Simulation simulation)
{
    foreach (var line in simulation.Log)
        if (IsError(line)) Ui.Line($"[red]{line}[/]");
        else Ui.Line($"{line}");
    if (simulation.Stopped is null) return 0;
    Ui.Error(simulation.Stopped.Message);
    return 1;
}

static bool IsError(string line) => line.Split(' ').ElementAtOrDefault(1) == "error:";

static int Check(string map)
{
    var script = MirrorImaged.Data.MapArchive.Read(map).Script;
    var levels = typeof(GameNatives).GetMethods()
        .Select(method => (method.Name, Level: method.GetCustomAttributes(typeof(NativeAttribute), false).OfType<NativeAttribute>().FirstOrDefault()?.Level))
        .Where(native => native.Level is not null).ToDictionary(native => native.Name, native => native.Level!.Value);
    var text = script is null ? "" : System.Text.Encoding.UTF8.GetString(script);
    var used = System.Text.RegularExpressions.Regex.Matches(text, @"[A-Za-z_][A-Za-z0-9_]*").Select(match => match.Value)
        .Where(levels.ContainsKey).GroupBy(name => name).Select(group => (Name: group.Key, Count: group.Count(), Level: levels[group.Key]))
        .OrderByDescending(native => native.Level).ThenBy(native => native.Name, StringComparer.Ordinal).ToList();
    Ui.Natives(used);
    var missing = used.Count(native => native.Level == Level.Missing);
    if (missing > 0) Ui.Line($"{used.Count} natives called, [red]{missing} Missing[/]");
    else Ui.Line($"{used.Count} natives called, [green]{missing} Missing[/]");
    return missing > 0 ? 1 : 0;
}

static int Run(string map, Dictionary<string, string> flags, double seconds, int seed)
{
    using var simulation = Seats.Open(map, seed);
    try
    {
        Ui.Work($"Running {map}", () =>
        {
            var hashEvery = flags.TryGetValue("--hash", out var every) && double.TryParse(every, NumberStyles.Float, CultureInfo.InvariantCulture, out var period) ? period : 0;
            if (flags.TryGetValue("--input", out var inputFile))
            {
                // The inputs of a recording, or a list of them, at their steps; the run goes on to the time asked
                var text = File.ReadAllText(inputFile);
                var recording = text.TrimStart().StartsWith('[') ? new Recording { Inputs = System.Text.Json.JsonSerializer.Deserialize<List<Input>>(text)! } : Recording.FromJson(text);
                recording.Steps = Math.Max(recording.Steps, (long)(seconds * StepsPerSecond));
                Follow(simulation, recording, hashEvery);
            }
            else if (hashEvery > 0)
                for (double at = hashEvery; at <= seconds + 1e-9 && simulation.Stopped is null; at += hashEvery)
                {
                    simulation.Advance(TimeSpan.FromSeconds(hashEvery));
                    Ui.Line($"hash {at.ToString("0.###", CultureInfo.InvariantCulture)} {simulation.StateHash()}");
                }
            else simulation.Advance(TimeSpan.FromSeconds(seconds));
        });
    }
    catch (LuaException) when (simulation.Stopped is not null) { }
    if (flags.TryGetValue("--record", out var recorded)) File.WriteAllText(recorded, simulation.Recording.ToJson());
    return Finish(simulation);
}

static int PlayRecording(string recordingFile, Dictionary<string, string> flags, double hashEvery)
{
    if (recordingFile.EndsWith(".w3g", StringComparison.OrdinalIgnoreCase)) return PlayReplay(recordingFile, flags, hashEvery);
    var recording = Recording.FromJson(File.ReadAllText(recordingFile));
    using var simulation = Simulation.Load(flags.GetValueOrDefault("--map") ?? recording.Map, recording.Options());
    try
    {
        Ui.Work($"Replaying {recordingFile}", () =>
        {
            Follow(simulation, recording, hashEvery);
            if (hashEvery <= 0) Ui.Line($"hash {(double)simulation.Step / StepsPerSecond:0.###} {simulation.StateHash()}");
        });
    }
    catch (LuaException) when (simulation.Stopped is not null) { }
    return Finish(simulation);
}

// Gives the recording's inputs at their steps and runs to its step, printing the hash at the end of each period of the seconds given (none for 0), as
// the inputs up to then left it
static void Follow(Simulation simulation, Recording recording, double hashEvery)
{
    var period = (long)(hashEvery * StepsPerSecond);
    var inputs = recording.Inputs.OrderBy(i => i.Step).ToList();
    var next = 0;
    void To(long step)
    {
        var count = inputs.Skip(next).TakeWhile(input => input.Step <= step).Count();
        simulation.Follow(new Recording { Inputs = inputs.GetRange(next, count), Steps = step });
        next += count;
    }
    for (var at = period; period > 0 && at <= recording.Steps && simulation.Stopped is null; at += period)
    {
        To(at);
        Ui.Line($"hash {(double)at / StepsPerSecond:0.###} {simulation.StateHash()}");
    }
    if (simulation.Stopped is null) To(recording.Steps);
}

static int PlayReplay(string file, Dictionary<string, string> flags, double hashEvery)
{
    var replay = Replay.Read(file);
    var map = flags.GetValueOrDefault("--map") ?? throw new ArgumentException($"A replay needs the map it was played on ({replay.Map}): --map <map>");
    Ui.Line($"Build {replay.Build}, {TimeSpan.FromMilliseconds(replay.DurationMillis):h\\:mm\\:ss}, on {replay.Map}: {string.Join(", ", replay.Players.Where(p => p.Id > 0).Select(p => $"{p.Name} (slot {p.Slot})"))}");
    using var simulation = Simulation.Load(map, replay, new ReplayOptions { Graphics = Seats.Graphics });
    var run = simulation.Play(replay);
    try
    {
        Ui.Work($"Replaying {file}", () =>
        {
            var period = hashEvery > 0 ? (long)(hashEvery * StepsPerSecond) : run.Steps;
            for (var at = period; at < run.Steps; at += period)
            {
                run.To(at);
                Ui.Line($"hash {(double)at / StepsPerSecond:0.###} {simulation.StateHash()}");
            }
            run.To(run.Steps);
            Ui.Line($"hash {(double)simulation.Step / StepsPerSecond:0.###} {simulation.StateHash()}");
        });
    }
    catch (LuaException) when (simulation.Stopped is not null) { }
    // What of the replay reached a unit: the run is as close to the game as MirrorImaged is, and no closer
    Ui.Line($"Orders: {run.Orders}, taken by a unit {run.Taken}, with no unit of the selection in this run {run.Unowned}, refused {run.Refused.Values.Sum()}");
    Ui.Line($"Units the orders named: {run.Named}, found in this run {run.Found}");
    Ui.Line($"By game minute (orders, taken, share; units named and found so far):");
    foreach (var minute in run.Minutes)
        Ui.Line($"  {minute.Minute,3}  {minute.Orders,5} {minute.Taken,5} {100.0 * minute.Taken / minute.Orders,4:0}%  {minute.Named,4} {minute.Found,4}");
    if (run.FirstMiss is { } miss) Ui.Line($"First order not taken: {miss}");
    if (run.Dequeued > 0) Ui.Line($"Taken out of a building's queue: {run.Dequeued}");
    if (run.NotPlayed.Count > 0) Ui.Line($"Read and not played: {string.Join(", ", run.NotPlayed.Select(action => $"0x{action.Key:X2} x {action.Value}"))}");
    if (replay.Actions.Count(action => action.Kind == "other") is var unknown and > 0) Ui.Line($"[yellow]Actions not read: {unknown}[/]");
    return Finish(simulation);
}

static int DesyncCheck(string map, double seconds, int seed, GraphicsMode graphics)
{
    var slots = Desync.UserSlots(map);
    if (slots.Count < 2)
    {
        Ui.Line($"[yellow]{map} has {slots.Count} user slot(s): there is no second player's game to compare with[/]");
        return 0;
    }
    var report = Ui.Work($"Comparing the games of players {string.Join(", ", slots)}", () => Desync.Check(map, TimeSpan.FromSeconds(seconds), new DesyncOptions { Seed = seed, Graphics = graphics }));
    if (report is null)
    {
        Ui.Line($"[green]No desync in {seconds.ToString("0.###", CultureInfo.InvariantCulture)} s between the games of players {string.Join(", ", slots)}[/]");
        return 0;
    }
    Ui.Line($"[red]Desync at step {report.Step} ({(double)report.Step / StepsPerSecond:0.###} s): {report.Part} differs[/]");
    foreach (var (slot, n) in report.Players.Select((slot, n) => (slot, n)))
    {
        Ui.Line($"  player {slot}'s game: {report.What} is {report.Values[n] ?? "not there"} (state {report.Hashes[n]})");
        foreach (var call in report.Stacks[n]?.Split('\n').Skip(1) ?? []) Ui.Line($"    {call.Trim()}");
    }
    if (report.LogLine is not null) Ui.Line($"  first differing log line: {report.LogLine}");
    return 1;
}

static int Bench(string map, double seconds, int seed)
{
    using var simulation = Seats.Open(map, seed);
    var clock = new Stopwatch();
    try
    {
        Ui.Work($"Timing {map}", () =>
        {
            clock.Start();
            try { simulation.Advance(TimeSpan.FromSeconds(seconds)); }
            finally { clock.Stop(); }
        });
    }
    catch (LuaException) when (simulation.Stopped is not null) { }
    var took = clock.Elapsed.TotalSeconds;
    var played = (double)simulation.Step / StepsPerSecond;
    Ui.Line($"{played.ToString("0.###", CultureInfo.InvariantCulture)} s of game in {took.ToString("0.###", CultureInfo.InvariantCulture)} s: [bold]{(played / Math.Max(took, 1e-9)).ToString("0.#", CultureInfo.InvariantCulture)}x real time[/]");
    return simulation.Stopped is null ? 0 : 1;
}

// Every second, each player's units each get a random legal order of the number asked, from a generator of the seed; the script errors and desyncs it finds are reported
static int Fuzz(string map, double seconds, int seed, int orders)
{
    var random = new Random(seed);
    string[] commands = ["move", "attack", "smart", "stop", "holdposition"];
    using var simulation = Seats.Open(map, seed);
    var players = Enumerable.Range(0, 24).ToList();
    try
    {
        Ui.Work($"Giving random orders in {map}", () =>
        {
            for (var second = 0; second < seconds && simulation.Stopped is null; second++)
            {
                foreach (var player in players)
                {
                    var units = simulation.Units(player);
                    for (var n = 0; n < orders && units.Count > 0; n++)
                    {
                        var unit = units[random.Next(units.Count)];
                        var command = commands[random.Next(commands.Length)];
                        if (command is "move" or "attack" or "smart") simulation.Order(player, unit.Id, command, unit.X + random.Next(-1500, 1500), unit.Y + random.Next(-1500, 1500));
                        else simulation.Order(player, unit.Id, command);
                    }
                }
                simulation.Advance(TimeSpan.FromSeconds(1));
            }
        });
    }
    catch (LuaException) when (simulation.Stopped is not null) { }
    var errors = simulation.Log.Where(IsError).ToList();
    foreach (var error in errors) Ui.Line($"[red]{error}[/]");
    if (errors.Count == 0) Ui.Line($"{simulation.Units().Count} units alive after {(double)simulation.Step / StepsPerSecond:0.###} s, [green]{errors.Count} script error(s)[/]");
    else Ui.Line($"{simulation.Units().Count} units alive after {(double)simulation.Step / StepsPerSecond:0.###} s, [red]{errors.Count} script error(s)[/]");
    return errors.Count == 0 && simulation.Stopped is null ? 0 : 1;
}

static int Tests(string path, string? junit)
{
    var files = Directory.Exists(path) ? Directory.GetFiles(path, "*.test.lua", SearchOption.AllDirectories).Order(StringComparer.Ordinal).ToList() : [path];
    if (files.Count == 0)
    {
        Ui.Error($"No *.test.lua files in {path}");
        return 1;
    }
    var results = new List<LuaTestResult>();
    Ui.Work("Running the tests", () =>
    {
        foreach (var file in files)
        {
            using var host = new LuaTestHost();
            foreach (var result in host.Run(file))
            {
                results.Add(result);
                if (result.Passed) Ui.Line($"[green]PASS[/] {result.File}: {result.Name}");
                else
                {
                    Ui.Line($"[red]FAIL[/] {result.File}: {result.Name}");
                    Ui.Line($"     [grey]{result.Message}[/]");
                }
            }
        }
    });
    var (passed, failed) = (results.Count(r => r.Passed), results.Count(r => !r.Passed));
    if (failed == 0) Ui.Line($"[green]{passed} passed[/], {failed} failed");
    else Ui.Line($"{passed} passed, [red]{failed} failed[/]");
    if (junit is not null) File.WriteAllText(junit, LuaTestHost.JUnit(results));
    return results.All(r => r.Passed) ? 0 : 1;
}

static int Save(string file, double at, double back, int seed, string output)
{
    Simulation simulation;
    if (file.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
    {
        var recording = Recording.FromJson(File.ReadAllText(file));
        recording.Steps = (long)(at * StepsPerSecond);
        simulation = Ui.Work($"Replaying {file}", () => Simulation.Replay(recording));
    }
    else
    {
        simulation = Seats.Open(file, seed);
        try { Ui.Work($"Running {file}", () => simulation.Advance(TimeSpan.FromSeconds(at))); }
        catch (LuaException) when (simulation.Stopped is not null) { }
    }
    if (back > 0)
        using (var ahead = simulation)
            simulation = Ui.Work($"Stepping back {back.ToString("0.###", CultureInfo.InvariantCulture)} s", () => ahead.StepBack((long)(back * StepsPerSecond)));
    using (simulation)
    {
        var snapshot = simulation.Snapshot();
        File.WriteAllText(output, snapshot.ToJson());
        Ui.Line($"Snapshot at step {snapshot.Step} ({(double)snapshot.Step / StepsPerSecond:0.###} s): {snapshot.Hash}, {snapshot.Parts.Count} parts, saved to [bold]{output}[/]");
        return 0;
    }
}

static int Compare(string file, string? map)
{
    var snapshot = Snapshot.FromJson(File.ReadAllText(file));
    using var simulation = Ui.Work($"Running to step {snapshot.Step} again", () => Simulation.Restore(snapshot, map));
    var differences = snapshot.Differences(simulation);
    if (differences.Count == 0)
    {
        Ui.Line($"[green]Same world at step {snapshot.Step}: {snapshot.Hash}[/]");
        return 0;
    }
    Ui.Line($"[red]The world at step {snapshot.Step} differs from the snapshot ({snapshot.Hash}, now {simulation.StateHash()}):[/]");
    foreach (var difference in differences.Take(20)) Ui.Line($"  {difference}");
    return 1;
}

static int View(string file, double seconds, int seed, double every, string? output)
{
    Simulation simulation;
    if (file.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
    {
        var recording = Recording.FromJson(File.ReadAllText(file));
        simulation = Simulation.Load(recording.Map, recording.Options());
        // Every input given at its step, the run up to the last of them; the page shows the rest of the recording from there
        try { simulation.Follow(new Recording { Inputs = recording.Inputs }); }
        catch (LuaException) when (simulation.Stopped is not null) { }
        seconds = Math.Max(seconds, (double)recording.Steps / StepsPerSecond - (double)simulation.Step / StepsPerSecond);
    }
    else simulation = Seats.Open(file, seed);
    using (simulation)
    {
        var trace = Ui.Work($"Recording {file}", () => Viewer.Record(simulation, TimeSpan.FromSeconds(seconds), TimeSpan.FromSeconds(Math.Max(every, 0.05))));
        output ??= Path.ChangeExtension(Path.GetFileName(file), ".view.html");
        File.WriteAllText(output, Viewer.Page(trace));
        Ui.Line($"{trace.Frames.Count} frames of {seconds:0.###} s and {trace.Log.Count} log lines saved to [bold]{Path.GetFullPath(output)}[/]");
        return trace.Stopped is null ? 0 : 1;
    }
}

// Who plays: the slots given by --users and --computers, or with --melee true the map's first user slot a person and every other slot of a user or
// a computer a computer
static class Seats
{
    public static int[]? Users, Computers;
    public static bool Melee;
    public static GraphicsMode Graphics;

    public static Simulation Open(string path, int seed)
    {
        var (users, computers) = (Users, Computers);
        if (Melee && users is null && computers is null)
        {
            var players = MirrorImaged.Data.MapArchive.Read(path).Files.Info?.Players.Where(p => p.Controller is War3Net.Build.Info.PlayerController.User or War3Net.Build.Info.PlayerController.Computer).Select(p => p.Id).ToList() ?? [];
            if (players.Count > 0) (users, computers) = ([players[0]], [.. players.Skip(1)]);
        }
        return Ui.Work($"Loading {path}", () => Simulation.Load(path, new SimulationOptions { Seed = seed, Users = users, Computers = computers, Graphics = Graphics }));
    }
}
