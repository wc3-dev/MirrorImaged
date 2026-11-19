using System.Text.Json;

namespace MirrorImaged;

/// <summary>One part of the world at a snapshot: its name (the time, a player, a unit) and a hash of what it holds.</summary>
public sealed record SnapshotPart(string Name, string Hash);

/// <summary>
/// The world at a step, kept as what recreates it and what it was: the recording up to the step (map, seed, seats and inputs), the
/// state hash and the hash of each part. A run that is the same reaches the same hash at the same step, so a later run is compared against
/// a snapshot by running to its step and reading the parts that differ.
/// </summary>
public sealed class Snapshot
{
    /// <summary>The step the snapshot was taken at.</summary>
    public long Step { get; set; }
    /// <summary>The state hash at that step.</summary>
    public string Hash { get; set; } = "";
    /// <summary>Each part of the world and its hash.</summary>
    public List<SnapshotPart> Parts { get; set; } = [];
    /// <summary>What runs the map again to that step.</summary>
    public Recording Recording { get; set; } = new();

    static readonly JsonSerializerOptions Format = new() { WriteIndented = true };

    /// <summary>The snapshot as JSON.</summary>
    public string ToJson() => JsonSerializer.Serialize(this, Format);
    /// <summary>A snapshot from the JSON that <see cref="ToJson"/> makes.</summary>
    public static Snapshot FromJson(string json) => JsonSerializer.Deserialize<Snapshot>(json) ?? throw new InvalidDataException("Not a snapshot");

    /// <summary>What differs between the snapshot and the simulation, which must be at the snapshot's step: nothing when it is the same world.</summary>
    public IReadOnlyList<string> Differences(Simulation simulation)
    {
        if (simulation.Step != Step) return [$"the simulation is at step {simulation.Step}, the snapshot at {Step}"];
        var now = simulation.Parts();
        var (was, is_) = (Parts.ToDictionary(part => part.Name, part => part.Hash), now.ToDictionary(part => part.Name, part => part.Hash));
        return
        [
            .. was.Where(part => !is_.ContainsKey(part.Key)).Select(part => $"{part.Key} is gone"),
            .. is_.Where(part => !was.ContainsKey(part.Key)).Select(part => $"{part.Key} is new"),
            .. was.Where(part => is_.TryGetValue(part.Key, out var hash) && hash != part.Value).Select(part => $"{part.Key} differs"),
        ];
    }
}

public sealed partial class Simulation
{
    internal IReadOnlyList<SnapshotPart> Parts() => [.. _natives.HashParts().Select(part => new SnapshotPart(part.Name, part.Hash))];

    /// <summary>The world now, with what it takes to run to this step again.</summary>
    public Snapshot Snapshot() => new()
    {
        Step = _game.Step,
        Hash = StateHash(),
        Parts = [.. Parts()],
        Recording = Recording.FromJson(Recording.ToJson()),
    };

    /// <summary>Runs the snapshot's map again, exactly as it was run, to the snapshot's step.</summary>
    public static Simulation Restore(Snapshot snapshot, string? map = null) =>
        Restore(snapshot, Load(map ?? snapshot.Recording.Map, Options(snapshot.Recording)));

    static SimulationOptions Options(Recording recording) => new() { Seed = recording.Seed, Users = recording.Users, Computers = recording.Computers, Difficulty = recording.Difficulty };

    static Simulation Restore(Snapshot snapshot, Simulation simulation)
    {
        var recording = Recording.FromJson(snapshot.Recording.ToJson());
        recording.Inputs = [.. recording.Inputs.Where(input => input.Step <= snapshot.Step)];
        recording.Steps = snapshot.Step;
        simulation.Follow(recording);
        return simulation;
    }

    /// <summary>
    /// The run as it was <paramref name="steps"/> steps ago, a new simulation made by running the map again to that step; the state is the
    /// one the run had then. This simulation goes on as it is.
    /// </summary>
    public Simulation StepBack(long steps)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(steps);
        var at = Math.Max(_game.Step - steps, 0);
        var snapshot = new Snapshot { Step = at, Recording = Recording.FromJson(Recording.ToJson()) };
        return Restore(snapshot, _reload(Options(snapshot.Recording)));
    }
}
