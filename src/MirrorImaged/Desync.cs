using MirrorImaged.Data;
using War3Net.Build.Info;

namespace MirrorImaged;

/// <summary>Where two players' runs of one map first differ: the step, the part of the world, and the first line of the log that differs.</summary>
public sealed record DesyncReport(long Step, string Part, IReadOnlyList<int> Players, IReadOnlyList<string> Hashes, string? LogLine);

/// <summary>
/// The desync check: the map runs once for each person's slot, as that player's own game, and the runs are compared after each step of the
/// time asked. A map that changes the game from what only one player's game knows (<c>GetLocalPlayer</c>) makes its runs differ, and the check
/// says where first.
/// </summary>
public static class Desync
{
    /// <summary>The slots of the map that a person plays.</summary>
    public static IReadOnlyList<int> UserSlots(string path) =>
        [.. MapArchive.Read(path).Files.Info?.Players.Where(p => p.Controller == PlayerController.User).Select(p => p.Id) ?? []];

    /// <summary>Runs the map for each person's slot and compares the runs every <paramref name="every"/>; null when they stay the same to the end.</summary>
    public static DesyncReport? Check(string path, TimeSpan time, int seed = 0, TimeSpan? every = null)
    {
        var slots = UserSlots(path);
        if (slots.Count < 2) return null;
        var simulations = slots.Select(slot => Simulation.Load(path, new SimulationOptions { Seed = seed, Users = [slot, .. slots.Where(other => other != slot)] })).ToList();
        try
        {
            var slice = every ?? TimeSpan.FromSeconds(1);
            for (var spent = TimeSpan.Zero; spent < time; spent += slice)
            {
                foreach (var simulation in simulations)
                    try { simulation.Advance(slice); }
                    catch (Lua.LuaException) when (simulation.Stopped is not null) { }
                var hashes = simulations.Select(simulation => simulation.StateHash()).ToList();
                if (hashes.Distinct().Count() > 1) return Report(slots, simulations, hashes);
            }
            return null;
        }
        finally
        {
            foreach (var simulation in simulations) simulation.Dispose();
        }
    }

    static DesyncReport Report(IReadOnlyList<int> slots, List<Simulation> simulations, List<string> hashes)
    {
        var parts = simulations.Select(simulation => simulation.Natives.HashParts().ToList()).ToList();
        var count = parts.Min(p => p.Count);
        var at = Enumerable.Range(0, count).FirstOrDefault(i => parts.Select(p => p[i].Hash).Distinct().Count() > 1, -1);
        var part = at >= 0 ? parts[0][at].Name : parts.Select(p => p.Count).Distinct().Count() > 1 ? "unit count" : "state";
        var logs = simulations.Select(simulation => simulation.Log).ToList();
        var line = Enumerable.Range(0, logs.Max(log => log.Count)).Select(i => logs.Select(log => i < log.Count ? log[i] : null).ToList())
            .FirstOrDefault(row => row.Distinct().Count() > 1)?.FirstOrDefault(entry => entry is not null);
        return new(simulations[0].Step, part, slots, hashes, line);
    }
}
