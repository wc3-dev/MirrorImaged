using War3Net.Build.Common;
using War3Net.Build.Info;

namespace MirrorImaged.Tests;

/// <summary>The desync check: a map that changes the game from one player's own game differs between the players' runs, and the check says where.</summary>
[Trait("Needs", "Game")]
public sealed class DesyncTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    string Map(string main) => _maps.Build($$"""
        function config() SetPlayers(2) end
        function main()
        {{main}}
        end
        """, change: info =>
    {
        info.Players.Clear();
        info.Players.AddRange([Slot(0), Slot(1)]);
    });

    static PlayerData Slot(int id) => new() { Id = id, Controller = PlayerController.User, Race = PlayerRace.Human, Name = $"Player {id + 1}" };

    [InstallFact]
    public void A_map_that_changes_the_game_for_one_local_player_is_a_desync()
    {
        var path = Map("""
            TimerStart(CreateTimer(), 3, false, function()
                if GetLocalPlayer() == Player(1) then SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, 999) end
            end)
            """);

        var report = Desync.Check(path, TimeSpan.FromSeconds(10));

        Assert.NotNull(report);
        Assert.Equal("player 0", report.Part);
        Assert.Equal(3 * 1024, report.Step);
        Assert.Equal(["0", "999"], report.Values);
    }

    // The step is the one the games differ in, however far apart they are compared
    [InstallTheory]
    [InlineData(2.5, 1)]
    [InlineData(2.5, 0.3)]
    [InlineData(0.001, 1)]
    public void The_step_is_exact(double at, double every)
    {
        var path = Map($$"""
            TimerStart(CreateTimer(), {{at.ToString(System.Globalization.CultureInfo.InvariantCulture)}}, false, function()
                if GetLocalPlayer() == Player(1) then SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, 999) end
            end)
            """);

        var report = Desync.Check(path, TimeSpan.FromSeconds(5), new DesyncOptions { Every = TimeSpan.FromSeconds(every) });

        Assert.Equal((long)Math.Round(at * 1024), report!.Step);
    }

    [InstallFact]
    public void The_command_prints_the_step_the_value_and_the_stack()
    {
        var path = Map("""
            TimerStart(CreateTimer(), 3, false, function()
                if GetLocalPlayer() == Player(1) then SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, 999) end
            end)
            """);

        var (exit, output, _) = CoverageTests.Mirror("desync", path, "--seconds", "5");

        Assert.Equal(1, exit);
        Assert.Contains("Desync at step 3072 (3 s): player 0 differs", output);
        Assert.Contains("player 0's game: PLAYER_STATE_RESOURCE_GOLD is 0", output);
        Assert.Contains("player 1's game: PLAYER_STATE_RESOURCE_GOLD is 999", output);
        Assert.Contains("[C]: in function 'SetPlayerState'", output);
        Assert.Contains("war3map.lua:4:", output);
    }

    [InstallFact]
    public void A_map_that_runs_the_same_for_each_player_is_not()
    {
        var path = Map("""
            TimerStart(CreateTimer(), 3, false, function() SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, 999) end)
            """);

        Assert.Null(Desync.Check(path, TimeSpan.FromSeconds(10)));
    }
}
