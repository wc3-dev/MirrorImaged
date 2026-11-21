using System.Diagnostics;
using MirrorImaged.Data;
using MirrorImaged.Lua;
using War3Net.Build.Info;
using Xunit.Abstractions;

namespace MirrorImaged.Tests;

/// <summary>
/// A4 and A10 on the installed game's melee maps: a person in the first slot, who gives no orders, and computers in the others play 30 simulated
/// minutes. One test is the regression check (no script error, and the computers' units grow); another asserts the rest of A4's line (each
/// computer trains units, a computer takes a second town hall and a unit is killed) and fails on the maps where it doesn't hold. Both read one
/// play of the maps. A 4-player map goes through 30 game minutes in under 60 seconds. Set MIRROR_MELEE to a part of a map's name to run only the
/// maps with it. The whole set runs in parts (<see cref="Corpus"/>).
/// </summary>
[Trait("Needs", "Game")]
[Collection(Corpus.Name)]
public sealed class MeleeCorpusTests(ITestOutputHelper output) : IDisposable
{
    readonly string _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;

    public void Dispose() => Directory.Delete(_folder, true);

    static (string Path, int User, int[] Computers)? Seats(string path)
    {
        var players = MapArchive.Read(path).Files.Info?.Players ?? [];
        var slots = players.Where(p => p.Controller is PlayerController.User or PlayerController.Computer).Select(p => p.Id).ToList();
        // A melee map has two or more slots for people and the melee flag; a scenario (a tower defence, a race) is not one
        var info = MapArchive.Read(path).Files.Info;
        return players.Count(p => p.Controller == PlayerController.User) >= 2 && info is not null && info.MapFlags.HasFlag(MapFlags.MeleeMap) ? (path, slots[0], [.. slots.Skip(1)]) : null;
    }

    string Extract(CascStorage storage, string name)
    {
        var path = Path.Combine(_folder, Path.GetFileName(name));
        File.WriteAllBytes(path, storage.Read(name) ?? throw new InvalidDataException($"{name} isn't stored locally"));
        return path;
    }

    [InstallFact]
    public void A_four_player_melee_map_goes_through_30_game_minutes_in_under_a_minute()
    {
        using var storage = CascStorage.Open(GameInstall.Require());
        var path = Extract(storage, "War3.w3mod:Maps/(4)Adrenaline.w3m");
        var seats = Seats(path)!.Value;
        using var simulation = Simulation.Load(path, new SimulationOptions { Users = [seats.User], Computers = seats.Computers });

        var clock = Stopwatch.StartNew();
        simulation.Advance(TimeSpan.FromMinutes(30));
        output.WriteLine($"30 game minutes in {clock.Elapsed.TotalSeconds:0.0} s");

        Assert.Null(simulation.Stopped);
        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(60), $"{clock.Elapsed.TotalSeconds:0.0} s");
    }

    // The town halls of the four races, each with what it upgrades to: more than one is a computer that expanded; so are two haunted gold
    // mines, as the Undead's script expands with one and no necropolis (D824)
    static readonly HashSet<string> Halls = ["htow", "hkee", "hcas", "ogre", "ostr", "ofrt", "etol", "etoa", "etoe", "unpl", "unp1", "unp2"];

    // The maps where no computer can walk off its start's ground to a second gold mine, so none can expand, and may find no creep or enemy to
    // kill (D825). The melee scripts use no Way Gate or shipyard; they buy a zeppelin at 60 food, which the simulation doesn't (D431, D798). One
    // that expands after all is reported, so the list stays true
    static readonly Dictionary<string, string> NoWayOffTheStart = new()
    {
        ["(8)Plaguelands.w3m"] = "each start's ground has its own mine only; the way out is by Way Gates",
        ["(4)Islands.w3x"] = "each island has its own mine only; the map sells ships (Goblin Shipyards), no zeppelins",
        ["(4)DesertStrife.w3x"] = "each start's plateau has its own mine only, no ramp off it (seen in the game for start 0)",
    };

    sealed record Played(List<string> Lines, List<string> Errors, List<string> Unmet);

    // Each part is played once, by whichever of the two tests asks first
    static readonly System.Collections.Concurrent.ConcurrentDictionary<int, Lazy<Played>> Results = [];

    Played Result(int part)
    {
        var played = Results.GetOrAdd(part, _ => new Lazy<Played>(() => Play(part))).Value;
        foreach (var line in played.Lines) output.WriteLine(line);
        return played;
    }

    [InstallTheory]
    [MemberData(nameof(Corpus.Parts), MemberType = typeof(Corpus))]
    public void Every_melee_map_runs_30_minutes_with_computers_without_a_script_error(int part)
    {
        var errors = Result(part).Errors;
        Assert.True(errors.Count == 0, string.Join('\n', errors));
    }

    /// <summary>The rest of A4's line, a map at a time; it fails on the maps where the line doesn't hold, and names which part of it, but for the expansion and the kill on the maps <see cref="NoWayOffTheStart"/> names.</summary>
    [InstallTheory]
    [MemberData(nameof(Corpus.Parts), MemberType = typeof(Corpus))]
    public void On_every_melee_map_each_computer_trains_and_one_expands_and_a_unit_is_killed(int part)
    {
        var unmet = Result(part).Unmet;
        Assert.True(unmet.Count == 0, string.Join('\n', unmet));
    }

    Played Play(int part)
    {
        using var storage = CascStorage.Open(GameInstall.Require());
        var only = Environment.GetEnvironmentVariable("MIRROR_MELEE");
        var maps = Corpus.Part(storage.Names
            .Where(name => name.StartsWith("War3.w3mod:Maps/", StringComparison.OrdinalIgnoreCase) && Path.GetExtension(name).ToLowerInvariant() is ".w3x" or ".w3m")
            .Where(name => only is null || name.Contains(only, StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.Ordinal), part);
        var lines = new string?[maps.Count];
        var (found, short_) = (new List<string>[maps.Count], new List<string>[maps.Count]);
        Corpus.Each(maps, (at, name) =>
        {
            var (problems, unmet) = (found[at] = [], short_[at] = []);
            var path = Path.Combine(_folder, at + Path.GetExtension(name));
            try
            {
                File.WriteAllBytes(path, Corpus.Read(storage, name));
                if (Seats(path) is not { } seats) return;
                using var simulation = Simulation.Load(path, new SimulationOptions { Users = [seats.User], Computers = seats.Computers });
                // Looked at each game minute: a player's units, a computer's town halls, and the units that lie dead, which a fight killed (one
                // that is gone without a body was taken away: a defeated player's, a worker that became its building)
                int[] sides = [24, seats.User, .. seats.Computers];
                var (first, most, halls, killed, last) = (new int[sides.Length], new int[sides.Length], new int[sides.Length], new int[sides.Length], new int[sides.Length]);
                var alive = new Dictionary<int, int>(); // each living unit's side, as it was when last looked at
                var clock = Stopwatch.StartNew();
                for (var minute = 0; minute <= 30 && simulation.Stopped is null; minute++)
                {
                    if (minute > 0)
                        try { simulation.Advance(TimeSpan.FromMinutes(1)); }
                        catch (LuaException) when (simulation.Stopped is not null) { }
                    var now = sides.Select(side => simulation.Units(side)).ToArray();
                    var ids = now.SelectMany((units, side) => units.Select(u => (u.Id, side))).ToDictionary(u => u.Id, u => u.side);
                    foreach (var (id, side) in alive)
                        if (!ids.ContainsKey(id) && simulation.Unit(id) is { Dead: true }) killed[side]++;
                    alive = ids;
                    for (var n = 0; n < sides.Length; n++)
                    {
                        if (minute == 0) first[n] = now[n].Count;
                        (most[n], halls[n], last[n]) = (Math.Max(most[n], now[n].Count), Math.Max(halls[n], Math.Max(now[n].Count(u => Halls.Contains(u.Type)), now[n].Count(u => u.Type == "ugol"))), now[n].Count);
                    }
                }
                lines[at] = $"{name}: {clock.Elapsed.TotalSeconds:0.0} s, creeps {first[0]} to {last[0]} ({killed[0]} killed), the person's units {first[1]} to {last[1]} ({killed[1]} killed), computers "
                    + string.Join(", ", seats.Computers.Select((player, n) => $"{player}: {first[n + 2]} to {last[n + 2]} units, {most[n + 2]} at most, {halls[n + 2]} halls at most, {killed[n + 2]} killed"));
                if (simulation.Stopped is not null) problems.Add($"{name}: stopped: {simulation.Stopped.Message.Split('\n')[0]}");
                foreach (var line in simulation.Log.Where(l => l.Split(' ').ElementAtOrDefault(1) == "error:").Take(1)) problems.Add($"{name}: {line}");
                if (last.Skip(2).Sum() <= first.Skip(2).Sum()) problems.Add($"{name}: the computers trained no units ({first.Skip(2).Sum()} to {last.Skip(2).Sum()})");
                for (var n = 2; n < sides.Length; n++)
                    if (most[n] <= first[n]) unmet.Add($"{name}: computer {sides[n]} trained no units ({first[n]} at the start, {most[n]} at most)");
                var expanded = halls.Skip(2).Any(count => count >= 2);
                var stuck = NoWayOffTheStart.TryGetValue(Path.GetFileName(name), out var why);
                if (stuck)
                {
                    if (expanded) unmet.Add($"{name}: a computer took a second town hall, though the test lists it as a map it can't ({why})");
                }
                else if (!expanded) unmet.Add($"{name}: no computer took a second town hall");
                if (killed.Sum() == 0 && !stuck) unmet.Add($"{name}: no unit was killed");
            }
            catch (Exception e) { problems.Add($"{name}: {e.GetType().Name}: {e.Message.Split('\n')[0]}"); }
            finally { File.Delete(path); }
        });

        return new([.. lines.OfType<string>()], [.. found.SelectMany(problems => problems)], [.. short_.SelectMany(unmet => unmet)]);
    }
}
