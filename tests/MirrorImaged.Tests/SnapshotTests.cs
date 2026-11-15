using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>Snapshots of the world, stepping back to an earlier step, and the command line's snapshot and compare.</summary>
public sealed class SnapshotTests : IDisposable
{
    readonly TestMaps _maps = new();
    readonly string _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;

    public void Dispose()
    {
        _maps.Dispose();
        Directory.Delete(_folder, true);
    }

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static StandardData Standard() => new StandardData()
        .Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=100;umvs=270;umvt=foot;uacq=500;uaen=1;ua1w=normal;ua1t=normal;ua1g=ground;ua1b=5;ua1d=2;ua1s=3;ua1c=1;ua1r=90;udp1=0.5;ucol=31;udef=0;udty=normal"));

    string Map() => _maps.Build("""
        function config() end
        function main()
            SetRandomSeed(5)
            for i = 0, 3 do
                CreateUnit(Player(0), FourCC("hfoo"), -200, i * 80, 0)
                CreateUnit(Player(1), FourCC("hfoo"), 200, i * 80, 0)
            end
        end
        """);

    [Fact]
    public void A_snapshot_of_a_world_matches_the_same_run_and_names_what_differs_in_another()
    {
        var path = Map();
        using var simulation = Simulation.Load(path, "", data: Standard());
        simulation.Advance(TimeSpan.FromSeconds(3));
        var snapshot = Snapshot.FromJson(simulation.Snapshot().ToJson());

        using var same = Simulation.Load(path, "", data: Standard());
        same.Advance(TimeSpan.FromSeconds(3));
        using var other = Simulation.Load(path, "", data: Standard());
        other.Advance(TimeSpan.FromSeconds(3));
        other.Order(0, other.Units(0)[0].Id, "move", 0, 900);

        Assert.Empty(snapshot.Differences(same));
        Assert.Equal(simulation.StateHash(), snapshot.Hash);
        Assert.NotEmpty(snapshot.Differences(other));
        Assert.All(snapshot.Differences(other), line => Assert.Contains("differs", line));
        using var earlier = simulation.StepBack(1024);
        Assert.Equal([$"the simulation is at step {earlier.Step}, the snapshot at {snapshot.Step}"], snapshot.Differences(earlier));
    }

    [Fact]
    public void Stepping_back_lands_on_the_state_the_run_had_then()
    {
        var path = Map();
        using var simulation = Simulation.Load(path, "", data: Standard());
        var hashes = new Dictionary<long, string>();
        for (var second = 1; second <= 8; second++)
        {
            simulation.Advance(TimeSpan.FromSeconds(1));
            hashes[simulation.Step] = simulation.StateHash();
        }
        var now = simulation.Step;

        using var back = simulation.StepBack(3 * 1024);

        Assert.Equal(now - 3 * 1024, back.Step);
        Assert.Equal(hashes[now - 3 * 1024], back.StateHash());
        Assert.Equal(hashes[now], simulation.StateHash());
    }
}
