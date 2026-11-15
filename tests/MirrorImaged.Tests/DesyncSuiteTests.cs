using War3Net.Build.Common;
using War3Net.Build.Info;

namespace MirrorImaged.Tests;

/// <summary>
/// A8: the desync check finds each pattern the facts' "Desyncs" section names that this runtime can show (a change, a draw of the shared random
/// numbers, a handle made, in one player's game only), says where, and does not cry wolf at what only one player's screen shows.
/// </summary>
[Trait("Needs", "Game")]
public sealed class DesyncSuiteTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    string Map(string local) => _maps.Build($$"""
        function config() SetPlayers(2) end
        function main()
            TimerStart(CreateTimer(), 2, false, function()
                if GetLocalPlayer() == Player(1) then
                    {{local}}
                end
            end)
            TimerStart(CreateTimer(), 4, false, function() SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, GetRandomInt(1, 1000)) end)
        end
        """, change: info =>
    {
        info.Players.Clear();
        info.Players.AddRange([Slot(0), Slot(1)]);
    });

    static PlayerData Slot(int id) => new() { Id = id, Controller = PlayerController.User, Race = PlayerRace.Human, Name = $"Player {id + 1}" };

    // What each pattern is, and the part of the world where the games first differ
    [InstallTheory]
    [InlineData("SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, 5)", "player 0")]
    [InlineData("CreateUnit(Player(1), FourCC('hfoo'), 0, 0, 0)", "handle ids")]
    [InlineData("local n = GetRandomInt(1, 10)", "random numbers")]
    [InlineData("local r = GetRandomReal(0, 1)", "random numbers")]
    [InlineData("local g = CreateGroup()", "handle ids")]
    [InlineData("local g = CreateGroup() GroupEnumUnitsInRange(g, 0, 0, 500, nil) ForGroup(g, function() end)", "handle ids")]
    [InlineData("local l = Location(1, 2)", "handle ids")]
    [InlineData("local t = CreateTimer()", "handle ids")]
    [InlineData("local f = BlzCreateFrame('EscMenuBackdrop', BlzGetOriginFrame(ORIGIN_FRAME_GAME_UI, 0), 0, 0)", "handle ids")]
    public void A_pattern_that_changes_one_players_game_is_found_where_it_differs(string local, string part)
    {
        var report = Desync.Check(Map(local.Replace('\'', '"')), TimeSpan.FromSeconds(8));

        Assert.NotNull(report);
        Assert.Equal(part, report.Part);
        Assert.InRange(report.Step, 2 * 1024, 3 * 1024 + 8);
    }

    // What only one player's screen shows is not a difference of the games
    [InstallTheory]
    [InlineData("DisplayTextToPlayer(Player(1), 0, 0, 'only you')")]
    [InlineData("SetCameraPosition(100, 100)")]
    [InlineData("SetMusicVolume(40)")]
    [InlineData("local n = GetPlayerName(Player(1))")]
    [InlineData("local x = GetCameraTargetPositionX()")]
    public void A_local_change_of_what_one_player_sees_is_not_a_desync(string local)
    {
        Assert.Null(Desync.Check(Map(local.Replace('\'', '"')), TimeSpan.FromSeconds(8)));
    }
}
