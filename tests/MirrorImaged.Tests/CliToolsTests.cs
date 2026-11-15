using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>The command line's record, replay, bench, fuzz and desync commands.</summary>
[Trait("Needs", "Game")]
public sealed class CliToolsTests : IDisposable
{
    readonly TestMaps _maps = new();
    readonly string _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;

    public void Dispose()
    {
        _maps.Dispose();
        Directory.Delete(_folder, true);
    }

    string Map(string main) => _maps.Build($$"""
        function config() end
        function main()
        {{main}}
        end
        """);

    [InstallFact]
    public void A_recorded_run_replays_to_the_same_hash()
    {
        var map = Map("""
            SetRandomSeed(4)
            TimerStart(CreateTimer(), 1, true, function() SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, GetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD) + GetRandomInt(1, 9)) end)
            """);
        var inputs = Path.Combine(_folder, "inputs.json");
        File.WriteAllText(inputs, """[{"Step": 2048, "Kind": "say", "Player": 0, "Ints": null, "Floats": null, "Text": "-hi"}]""");
        var recording = Path.Combine(_folder, "run.json");

        var (exit, _, _) = CoverageTests.Mirror("run", map, "--seconds", "10", "--seed", "4", "--input", inputs, "--record", recording);
        var replayed = CoverageTests.Mirror("replay", recording);
        var again = CoverageTests.Mirror("replay", recording);

        Assert.Equal(0, exit);
        Assert.Equal(0, replayed.Exit);
        Assert.Matches(@"^hash 10 [0-9A-F]{16}\n", replayed.Output);
        Assert.Equal(replayed.Output, again.Output);
        Assert.Contains("\"Kind\": \"say\"", File.ReadAllText(recording));
    }

    [InstallFact]
    public void Bench_says_how_much_faster_than_real_time_a_run_goes()
    {
        var map = Map("");

        var (exit, output, _) = CoverageTests.Mirror("bench", map, "--seconds", "30");

        Assert.Equal(0, exit);
        Assert.Matches(@"^30 s of game in [0-9.]+ s: [0-9.]+x real time", output);
    }

    [InstallFact]
    public void Fuzz_reports_a_script_error_and_no_error_otherwise()
    {
        var quiet = Map("");
        var broken = Map("""TimerStart(CreateTimer(), 2, false, function() error("boom") end)""");

        var ok = CoverageTests.Mirror("fuzz", quiet, "--seconds", "5");
        var bad = CoverageTests.Mirror("fuzz", broken, "--seconds", "5");

        Assert.Equal(0, ok.Exit);
        Assert.Contains("0 script error(s)", ok.Output);
        Assert.Equal(1, bad.Exit);
        Assert.Contains("boom", bad.Output);
    }

    [InstallFact]
    public void Desync_on_a_one_player_map_says_there_is_nothing_to_compare()
    {
        var (exit, output, _) = CoverageTests.Mirror("desync", Map(""));

        Assert.Equal(0, exit);
        Assert.Contains("1 user slot(s)", output);
    }

    [InstallFact]
    public void A_snapshot_compares_equal_to_its_own_run_and_unequal_to_another_map()
    {
        var map = Map("""
            TimerStart(CreateTimer(), 1, true, function() SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, GetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD) + GetRandomInt(1, 9)) end)
            """);
        var other = Map("""
            TimerStart(CreateTimer(), 1, true, function() SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, GetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD) + 50) end)
            """);
        var snapshot = Path.Combine(_folder, "s.json");

        var saved = CoverageTests.Mirror("snapshot", map, "--at", "5", "--seed", "2", "--out", snapshot);
        var same = CoverageTests.Mirror("compare", snapshot);
        var different = CoverageTests.Mirror("compare", snapshot, "--map", other);

        Assert.Equal(0, saved.Exit);
        Assert.Contains("Snapshot at step 5120", saved.Output);
        Assert.Equal(0, same.Exit);
        Assert.Contains("Same world at step 5120", same.Output);
        Assert.Equal(1, different.Exit);
        Assert.Contains("player 0 differs", different.Output);
    }
}
