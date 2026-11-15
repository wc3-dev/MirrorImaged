using System.Diagnostics;
using System.Globalization;
using MirrorImaged;
using MirrorImaged.Lua;
using MirrorImaged.Natives;

const double StepsPerSecond = 1024;

if (args is ["coverage"])
{
    Console.Write(Coverage.Of(typeof(GameNatives)));
    return 0;
}

if (args is ["check", var mapPath])
{
    try
    {
        var script = MirrorImaged.Data.MapArchive.Read(mapPath).Script;
        var levels = typeof(GameNatives).GetMethods()
            .Select(method => (method.Name, Level: method.GetCustomAttributes(typeof(NativeAttribute), false).OfType<NativeAttribute>().FirstOrDefault()?.Level))
            .Where(native => native.Level is not null).ToDictionary(native => native.Name, native => native.Level!.Value);
        var text = script is null ? "" : System.Text.Encoding.UTF8.GetString(script);
        var used = System.Text.RegularExpressions.Regex.Matches(text, @"[A-Za-z_][A-Za-z0-9_]*").Select(match => match.Value)
            .Where(levels.ContainsKey).GroupBy(name => name).Select(group => (Name: group.Key, Count: group.Count(), Level: levels[group.Key]))
            .OrderByDescending(native => native.Level).ThenBy(native => native.Name, StringComparer.Ordinal).ToList();
        foreach (var native in used) Console.Write($"{native.Level,-9}{native.Name} x{native.Count}\n");
        Console.Write($"{used.Count} natives called, {used.Count(native => native.Level == Level.Missing)} Missing\n");
        return used.Any(native => native.Level == Level.Missing) ? 1 : 0;
    }
    catch (Exception e)
    {
        Console.Error.Write(e.Message + "\n");
        return 1;
    }
}

// The commands that take a map or a recording, then --name value pairs
if (args is [var command and ("run" or "replay" or "desync" or "bench" or "fuzz" or "test" or "snapshot" or "compare" or "view"), var file, .. var rest])
{
    if (Flags(rest) is not { } flags) return Usage();
    double Number(string name, double fallback) => flags.TryGetValue(name, out var v) && double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : fallback;
    var (seconds, seed) = (Number("--seconds", command == "bench" ? 600 : 60), (int)Number("--seed", 0));
    try
    {
        switch (command)
        {
            case "run": return Run(file, flags, seconds, seed);
            case "replay": return Replay(file, flags, Number("--hash", 0));
            case "desync": return DesyncCheck(file, seconds, seed);
            case "bench": return Bench(file, seconds, seed);
            case "test": return Tests(file, flags.GetValueOrDefault("--junit"));
            case "snapshot": return Save(file, Number("--at", seconds), seed, flags.GetValueOrDefault("--out") ?? "snapshot.json");
            case "view": return View(file, seconds, seed, Number("--every", 0.5), flags.GetValueOrDefault("--out"));
            case "compare": return Compare(file, flags.GetValueOrDefault("--map"));
            default: return Fuzz(file, seconds, seed, (int)Number("--orders", 1));
        }
    }
    catch (Exception e)
    {
        Console.Error.Write(e.Message + "\n");
        return 1;
    }
}

return Usage();

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
            "--record" or "--input" or "--map" or "--junit" or "--out" => true,
            "--at" or "--every" => double.TryParse(options[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var at) && at >= 0,
            _ => false,
        };
        if (!valid) return null;
        flags[options[i]] = options[i + 1];
    }
    return flags;
}

static int Finish(Simulation simulation)
{
    foreach (var line in simulation.Log) Console.Write(line + "\n");
    if (simulation.Stopped is null) return 0;
    Console.Error.Write(simulation.Stopped.Message + "\n");
    return 1;
}

static int Run(string map, Dictionary<string, string> flags, double seconds, int seed)
{
    using var simulation = Simulation.Load(map, new SimulationOptions { Seed = seed });
    try
    {
        if (flags.TryGetValue("--input", out var inputFile))
        {
            // The inputs of a recording, or a list of them, at their steps; the run goes on to the time asked
            var text = File.ReadAllText(inputFile);
            var recording = text.TrimStart().StartsWith('[') ? new Recording { Inputs = System.Text.Json.JsonSerializer.Deserialize<List<Input>>(text)! } : Recording.FromJson(text);
            recording.Steps = Math.Max(recording.Steps, (long)(seconds * StepsPerSecond));
            simulation.Follow(recording);
        }
        else if (flags.TryGetValue("--hash", out var every) && double.TryParse(every, NumberStyles.Float, CultureInfo.InvariantCulture, out var period) && period > 0)
            for (double at = period; at <= seconds + 1e-9 && simulation.Stopped is null; at += period)
            {
                simulation.Advance(TimeSpan.FromSeconds(period));
                Console.Write($"hash {at.ToString("0.###", CultureInfo.InvariantCulture)} {simulation.StateHash()}\n");
            }
        else simulation.Advance(TimeSpan.FromSeconds(seconds));
    }
    catch (LuaException) when (simulation.Stopped is not null) { }
    if (flags.TryGetValue("--record", out var recorded)) File.WriteAllText(recorded, simulation.Recording.ToJson());
    return Finish(simulation);
}

static int Replay(string recordingFile, Dictionary<string, string> flags, double hashEvery)
{
    var recording = Recording.FromJson(File.ReadAllText(recordingFile));
    var steps = recording.Steps;
    recording.Steps = 0;
    using var simulation = Simulation.Load(flags.GetValueOrDefault("--map") ?? recording.Map, new SimulationOptions { Seed = recording.Seed, Users = recording.Users, Computers = recording.Computers });
    try
    {
        if (hashEvery > 0)
        {
            // Stepping through the recording, the hash after each period
            var period = (long)(hashEvery * StepsPerSecond);
            var inputs = recording.Inputs.OrderBy(i => i.Step).ToList();
            var next = 0;
            for (var at = period; at <= steps && simulation.Stopped is null; at += period)
            {
                while (next < inputs.Count && inputs[next].Step <= at)
                {
                    if (inputs[next].Step > simulation.Step) simulation.AdvanceSteps(inputs[next].Step - simulation.Step);
                    simulation.Give(inputs[next++]);
                }
                simulation.AdvanceSteps(at - simulation.Step);
                Console.Write($"hash {(double)at / StepsPerSecond:0.###} {simulation.StateHash()}\n");
            }
        }
        else
        {
            recording.Steps = steps;
            simulation.Follow(recording);
            Console.Write($"hash {(double)simulation.Step / StepsPerSecond:0.###} {simulation.StateHash()}\n");
        }
    }
    catch (LuaException) when (simulation.Stopped is not null) { }
    return Finish(simulation);
}

static int DesyncCheck(string map, double seconds, int seed)
{
    var slots = Desync.UserSlots(map);
    if (slots.Count < 2)
    {
        Console.Write($"{map} has {slots.Count} user slot(s): there is no second player's game to compare with\n");
        return 0;
    }
    var report = Desync.Check(map, TimeSpan.FromSeconds(seconds), seed);
    if (report is null)
    {
        Console.Write($"No desync in {seconds.ToString("0.###", CultureInfo.InvariantCulture)} s between the games of players {string.Join(", ", slots)}\n");
        return 0;
    }
    Console.Write($"Desync at step {report.Step} ({(double)report.Step / StepsPerSecond:0.###} s): {report.Part} differs\n");
    foreach (var (slot, hash) in report.Players.Zip(report.Hashes)) Console.Write($"  player {slot}'s game: {hash}\n");
    if (report.LogLine is not null) Console.Write($"  first differing log line: {report.LogLine}\n");
    return 1;
}

static int Bench(string map, double seconds, int seed)
{
    using var simulation = Simulation.Load(map, new SimulationOptions { Seed = seed });
    var clock = Stopwatch.StartNew();
    try { simulation.Advance(TimeSpan.FromSeconds(seconds)); }
    catch (LuaException) when (simulation.Stopped is not null) { }
    var took = clock.Elapsed.TotalSeconds;
    var played = (double)simulation.Step / StepsPerSecond;
    Console.Write($"{played.ToString("0.###", CultureInfo.InvariantCulture)} s of game in {took.ToString("0.###", CultureInfo.InvariantCulture)} s: {(played / Math.Max(took, 1e-9)).ToString("0.#", CultureInfo.InvariantCulture)}x real time\n");
    return simulation.Stopped is null ? 0 : 1;
}

// Every second, each player's units each get a random legal order of the number asked, from a generator of the seed; the script errors and desyncs it finds are reported
static int Fuzz(string map, double seconds, int seed, int orders)
{
    var random = new Random(seed);
    string[] commands = ["move", "attack", "smart", "stop", "holdposition"];
    using var simulation = Simulation.Load(map, new SimulationOptions { Seed = seed });
    var players = Enumerable.Range(0, 24).ToList();
    try
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
    }
    catch (LuaException) when (simulation.Stopped is not null) { }
    var errors = simulation.Log.Where(line => line.Split(' ').ElementAtOrDefault(1) == "error:").ToList();
    foreach (var error in errors) Console.Write(error + "\n");
    Console.Write($"{simulation.Units().Count} units alive after {(double)simulation.Step / StepsPerSecond:0.###} s, {errors.Count} script error(s)\n");
    return errors.Count == 0 && simulation.Stopped is null ? 0 : 1;
}

static int Tests(string path, string? junit)
{
    var files = Directory.Exists(path) ? Directory.GetFiles(path, "*.test.lua", SearchOption.AllDirectories).Order(StringComparer.Ordinal).ToList() : [path];
    if (files.Count == 0)
    {
        Console.Error.Write($"No *.test.lua files in {path}\n");
        return 1;
    }
    var results = new List<LuaTestResult>();
    foreach (var file in files)
    {
        using var host = new LuaTestHost();
        foreach (var result in host.Run(file))
        {
            results.Add(result);
            Console.Write($"{(result.Passed ? "PASS" : "FAIL")} {result.File}: {result.Name}\n");
            if (!result.Passed) Console.Write($"     {result.Message}\n");
        }
    }
    Console.Write($"{results.Count(r => r.Passed)} passed, {results.Count(r => !r.Passed)} failed\n");
    if (junit is not null) File.WriteAllText(junit, LuaTestHost.JUnit(results));
    return results.All(r => r.Passed) ? 0 : 1;
}

static int Save(string file, double at, int seed, string output)
{
    Simulation simulation;
    if (file.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
    {
        var recording = Recording.FromJson(File.ReadAllText(file));
        recording.Steps = (long)(at * StepsPerSecond);
        simulation = Simulation.Replay(recording);
    }
    else
    {
        simulation = Simulation.Load(file, new SimulationOptions { Seed = seed });
        try { simulation.Advance(TimeSpan.FromSeconds(at)); }
        catch (LuaException) when (simulation.Stopped is not null) { }
    }
    using (simulation)
    {
        var snapshot = simulation.Snapshot();
        File.WriteAllText(output, snapshot.ToJson());
        Console.Write($"Snapshot at step {snapshot.Step} ({(double)snapshot.Step / StepsPerSecond:0.###} s): {snapshot.Hash}, {snapshot.Parts.Count} parts, saved to {output}\n");
        return 0;
    }
}

static int Compare(string file, string? map)
{
    var snapshot = Snapshot.FromJson(File.ReadAllText(file));
    using var simulation = Simulation.Restore(snapshot, map);
    var differences = snapshot.Differences(simulation);
    if (differences.Count == 0)
    {
        Console.Write($"Same world at step {snapshot.Step}: {snapshot.Hash}\n");
        return 0;
    }
    Console.Write($"The world at step {snapshot.Step} differs from the snapshot ({snapshot.Hash}, now {simulation.StateHash()}):\n");
    foreach (var difference in differences.Take(20)) Console.Write($"  {difference}\n");
    return 1;
}

static int View(string file, double seconds, int seed, double every, string? output)
{
    Simulation simulation;
    if (file.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
    {
        var recording = Recording.FromJson(File.ReadAllText(file));
        simulation = Simulation.Load(recording.Map, new SimulationOptions { Seed = recording.Seed, Users = recording.Users, Computers = recording.Computers });
        foreach (var input in recording.Inputs.OrderBy(i => i.Step)) if (input.Step > simulation.Step) { simulation.AdvanceSteps(input.Step - simulation.Step); simulation.Give(input); }
        seconds = Math.Max(seconds, (double)recording.Steps / StepsPerSecond - (double)simulation.Step / StepsPerSecond);
    }
    else simulation = Simulation.Load(file, new SimulationOptions { Seed = seed });
    using (simulation)
    {
        var trace = Viewer.Record(simulation, TimeSpan.FromSeconds(seconds), TimeSpan.FromSeconds(Math.Max(every, 0.05)));
        output ??= Path.ChangeExtension(Path.GetFileName(file), ".view.html");
        File.WriteAllText(output, Viewer.Page(trace));
        Console.Write($"{trace.Frames.Count} frames of {seconds:0.###} s and {trace.Log.Count} log lines saved to {Path.GetFullPath(output)}\n");
        return trace.Stopped is null ? 0 : 1;
    }
}

static int Usage()
{
    Console.Error.Write("""
        usage:
          mirror coverage                                  How many natives are at each trust level, and which are Missing
          mirror check <map>                               Lists the natives a map's script calls with their trust levels
          mirror run <map> [--seconds <n>] [--seed <n>] [--hash <n>] [--record <file>] [--input <file>]
                                                           Runs a Lua or JASS map for n simulated seconds (60) with a
                                                           seed (0), then prints its log; with --hash, the state hash
                                                           every n seconds; with --input, the inputs of a recording
                                                           (or a list of them) at their steps; with --record, saves
                                                           the run as a recording
          mirror replay <recording> [--map <map>] [--hash <n>]
                                                           Runs a recording again, exactly, and prints the state hash at
                                                           its end (or every n seconds)
          mirror desync <map> [--seconds <n>] [--seed <n>] Runs the map for each person's slot as that player's own game
                                                           and reports where the runs first differ
          mirror snapshot <map or recording> [--at <n>] [--seed <n>] [--out <file>]
                                                           Saves the world after n simulated seconds with what it takes to run
                                                           to it again
          mirror view <map or recording> [--seconds <n>] [--every <n>] [--out <file>]
                                                           Runs it and saves a page that opens in a browser: the map from
                                                           above, the units, a timeline to scrub and the log
          mirror compare <snapshot> [--map <map>]         Runs the snapshot's run again to its step and lists what differs
          mirror test <file or folder> [--junit <file>]    Runs the Lua tests (*.test.lua) with the Lua test API, prints each result and
                                                           writes JUnit XML to the file
          mirror bench <map> [--seconds <n>] [--seed <n>]  Times a run: how many times faster than real time it goes
          mirror fuzz <map> [--seconds <n>] [--seed <n>] [--orders <n>]
                                                           Gives every player's units random legal orders each second
                                                           and reports script errors

        """);
    return 2;
}
