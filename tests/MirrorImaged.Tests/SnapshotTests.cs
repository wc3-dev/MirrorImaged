using MirrorImaged.Data;
using MirrorImaged.Lua;
using MirrorImaged.World;

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

    [Fact]
    public void Stepping_back_keeps_the_natives_run_as_missing()
    {
        var path = _maps.Build("""
            function config() end
            function main()
                TimerStart(CreateTimer(), 2, false, function() print(BlzGetUnitAnimationDurationByIndex(nil, 0)) end)
            end
            """);
        using var simulation = Simulation.Load(path, "", new SimulationOptions { TreatAsMissing = ["BlzGetUnitAnimationDurationByIndex"] }, Standard());
        simulation.Advance(TimeSpan.FromSeconds(1));

        using var back = simulation.StepBack(512);
        var recording = Snapshot.FromJson(simulation.Snapshot().ToJson()).Recording;

        Assert.Throws<LuaException>(() => back.Advance(TimeSpan.FromSeconds(2)));
        Assert.Contains("BlzGetUnitAnimationDurationByIndex", back.Stopped!.Message);
        Assert.Equal(["BlzGetUnitAnimationDurationByIndex"], recording.TreatAsMissing!);
        Assert.Equal(["BlzGetUnitAnimationDurationByIndex"], recording.Options().TreatAsMissing!);
    }

    // The map reads a count from a file in CustomMapData, makes that many footmen and writes the count one higher
    [Fact]
    public void Stepping_back_starts_from_the_documents_the_run_started_with()
    {
        var path = _maps.Build("""
            function config() end
            function main()
                Preloader("count.pld")
                local count = GetStoredInteger(InitGameCache("c"), "m", "n") + 1
                for i = 1, count do CreateUnit(Player(0), FourCC("hfoo"), 0, i * 80, 0) end
                PreloadGenClear()
                PreloadGenStart()
                Preload("\")\ncall StoreInteger(InitGameCache(\"c\"), \"m\", \"n\", " .. count .. ")\n//")
                PreloadGenEnd("count.pld")
            end
            """);
        var documents = new Documents();
        using (Simulation.Load(path, "", new SimulationOptions { Documents = documents }, Standard())) { }
        using var simulation = Simulation.Load(path, "", new SimulationOptions { Documents = documents }, Standard());
        simulation.Advance(TimeSpan.FromSeconds(1));
        var (hash, written) = (simulation.StateHash(), documents.CustomMapData["count.pld"]);
        simulation.Advance(TimeSpan.FromSeconds(1));

        using var back = simulation.StepBack(1024);

        Assert.Equal(2, simulation.Units(0).Count);
        Assert.Equal(hash, back.StateHash());
        Assert.Same(written, documents.CustomMapData["count.pld"]);
        Assert.NotSame(documents, back.Documents);
    }

    [Fact]
    public void A_recording_or_snapshot_of_another_format_is_refused_and_one_from_before_formats_reads()
    {
        var path = Map();
        using var simulation = Simulation.Load(path, "", data: Standard());
        simulation.Advance(TimeSpan.FromSeconds(1));
        var recording = simulation.Recording.ToJson();
        var snapshot = simulation.Snapshot().ToJson();

        Assert.Contains("\"Version\": 1", recording);
        Assert.Equal(1, Recording.FromJson(recording.Replace("\"Version\": 1,", "")).Version);
        Assert.Equal(1, Snapshot.FromJson(snapshot.Replace("\"Version\": 1,", "")).Version);
        Assert.Contains("format 2", Assert.Throws<InvalidDataException>(() => Recording.FromJson(recording.Replace("\"Version\": 1", "\"Version\": 2"))).Message);
        Assert.Contains("format 2", Assert.Throws<InvalidDataException>(() => Snapshot.FromJson(snapshot.Replace("\"Version\": 1", "\"Version\": 2"))).Message);
    }

    [Fact]
    public void A_run_at_insane_runs_again_from_its_recording_at_insane()
    {
        var path = _maps.Build("""
            function config() end
            function main()
                for i = 0, GetHandleId(GetAIDifficulty(Player(0))) do CreateUnit(Player(0), FourCC("hfoo"), 0, i * 80, 0) end
            end
            """);
        using var simulation = Simulation.Load(path, "", new SimulationOptions { Seed = 3, Difficulty = Difficulty.Insane }, Standard());
        simulation.Order(0, simulation.Units(0)[0].Id, "move", 500, 0);
        simulation.Advance(TimeSpan.FromSeconds(2));

        var recording = Recording.FromJson(simulation.Recording.ToJson());
        using var again = Simulation.Load(path, "", recording.Options(), Standard());
        again.Follow(recording);

        Assert.Equal(3, simulation.Units(0).Count);
        Assert.Equal(simulation.StateHash(), again.StateHash());
    }
}
