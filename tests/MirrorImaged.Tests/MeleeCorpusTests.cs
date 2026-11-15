using System.Diagnostics;
using MirrorImaged.Data;
using MirrorImaged.Lua;
using War3Net.Build.Info;
using Xunit.Abstractions;

namespace MirrorImaged.Tests;

/// <summary>
/// A4 and A10 on the installed game's melee maps: a person in the first slot and computers in the others play 30 simulated minutes without a
/// script error, the computers train units, and a 4-player map goes through 30 game minutes in under 60 seconds. Set MIRROR_MELEE to a part of a
/// map's name to run only the maps with it (the whole set takes a while).
/// </summary>
[Trait("Needs", "Game")]
public sealed class MeleeCorpusTests(ITestOutputHelper output) : IDisposable
{
    readonly string _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;

    public void Dispose() => Directory.Delete(_folder, true);

    static (string Path, int User, int[] Computers)? Seats(string path)
    {
        var players = MapArchive.Read(path).Files.Info?.Players ?? [];
        var slots = players.Where(p => p.Controller is PlayerController.User or PlayerController.Computer).Select(p => p.Id).ToList();
        // A melee map has two or more slots for people; a scenario of one person and its computers is not one
        return players.Count(p => p.Controller == PlayerController.User) >= 2 ? (path, slots[0], [.. slots.Skip(1)]) : null;
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

    [InstallFact]
    public void Every_melee_map_runs_30_minutes_with_computers_without_a_script_error()
    {
        using var storage = CascStorage.Open(GameInstall.Require());
        var only = Environment.GetEnvironmentVariable("MIRROR_MELEE");
        var maps = storage.Names
            .Where(name => name.StartsWith("War3.w3mod:Maps/", StringComparison.OrdinalIgnoreCase) && Path.GetExtension(name).ToLowerInvariant() is ".w3x" or ".w3m")
            .Where(name => only is null || name.Contains(only, StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.Ordinal).ToList();
        var problems = new List<string>();
        foreach (var name in maps)
        {
            var path = Extract(storage, name);
            try
            {
                if (Seats(path) is not { } seats) continue;
                using var simulation = Simulation.Load(path, new SimulationOptions { Users = [seats.User], Computers = seats.Computers });
                var before = simulation.Units().Count(u => seats.Computers.Contains(u.Player));
                var clock = Stopwatch.StartNew();
                try { simulation.Advance(TimeSpan.FromMinutes(30)); }
                catch (LuaException) when (simulation.Stopped is not null) { }
                var after = simulation.Units().Count(u => seats.Computers.Contains(u.Player));
                output.WriteLine($"{name}: {clock.Elapsed.TotalSeconds:0.0} s, computers' units {before} to {after}");
                if (simulation.Stopped is not null) problems.Add($"{name}: stopped: {simulation.Stopped.Message.Split('\n')[0]}");
                foreach (var line in simulation.Log.Where(l => l.Split(' ').ElementAtOrDefault(1) == "error:").Take(1)) problems.Add($"{name}: {line}");
                if (after <= before) problems.Add($"{name}: the computers trained no units ({before} to {after})");
            }
            catch (Exception e) { problems.Add($"{name}: {e.GetType().Name}: {e.Message.Split('\n')[0]}"); }
            finally { File.Delete(path); }
        }

        Assert.True(problems.Count == 0, string.Join('\n', problems));
    }
}
