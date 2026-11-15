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
        Assert.InRange(report.Step, 3 * 1024, 4 * 1024 + 8);
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
