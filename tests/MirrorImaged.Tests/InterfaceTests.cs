using MirrorImaged.Data;
using MirrorImaged.Natives;

namespace MirrorImaged.Tests;

/// <summary>Leaderboards, multiboards, quests, timer dialogs and dialogs against the facts' 11-07 probe: what their getters read back.</summary>
public sealed class InterfaceTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    List<string> Run(string main)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            function config() end
            function main()
            {{main}}
            end
            """), "", data: new StandardData());
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];
    }

    // facts: a second row for a player who has one, the index of the first; rows sort by value highest first, and the index follows
    [Fact]
    public void A_leaderboard_keeps_its_rows_and_sorts_them()
    {
        var lines = Run("""
            local lb = CreateLeaderboard()
            LeaderboardAddItem(lb, "a", 5, Player(1))
            LeaderboardAddItem(lb, "b", 9, Player(2))
            LeaderboardAddItem(lb, "c", 7, Player(1))
            print(LeaderboardGetItemCount(lb), LeaderboardGetPlayerIndex(lb, Player(1)), LeaderboardHasPlayerItem(lb, Player(2)), LeaderboardHasPlayerItem(lb, Player(3)), LeaderboardGetPlayerIndex(lb, Player(3)))
            LeaderboardSortItemsByValue(lb, false)
            print(LeaderboardGetPlayerIndex(lb, Player(2)), LeaderboardGetPlayerIndex(lb, Player(1)))
            LeaderboardSetLabel(lb, "Score")
            LeaderboardSetItemValue(lb, 2, 1)
            LeaderboardSortItemsByValue(lb, true)
            print(LeaderboardGetLabelText(lb), LeaderboardGetPlayerIndex(lb, Player(1)), IsLeaderboardDisplayed(lb))
            LeaderboardDisplay(lb, true)
            LeaderboardRemovePlayerItem(lb, Player(1))
            LeaderboardRemoveItem(lb, 0)
            print(IsLeaderboardDisplayed(lb), LeaderboardGetItemCount(lb))
            LeaderboardClear(lb)
            print(LeaderboardGetItemCount(lb), PlayerGetLeaderboard(Player(0)))
            """);

        Assert.Equal(["3\t0\ttrue\tfalse\t-1", "0\t1", "Score\t0\tfalse", "true\t1", "0\tnil"], lines);
    }

    // facts: a leaderboard with two rows for a player, set as the player's, closes the game
    [Fact]
    public void A_leaderboard_with_two_rows_for_a_player_closes_the_game_as_a_players_board()
    {
        using var simulation = Simulation.Load(_maps.Build("""
            function config() end
            function main()
                local ok = CreateLeaderboard()
                LeaderboardAddItem(ok, "a", 1, Player(0))
                PlayerSetLeaderboard(Player(0), ok)
                print(PlayerGetLeaderboard(Player(0)) == ok)
                local bad = CreateLeaderboard()
                LeaderboardAddItem(bad, "a", 1, Player(0))
                LeaderboardAddItem(bad, "b", 2, Player(0))
                PlayerSetLeaderboard(Player(0), bad)
                print("not reached")
            end
            """), "", data: new StandardData());

        Assert.Contains("two rows for one player", Assert.IsType<GameClosedException>(simulation.Stopped?.InnerException).Message);
        Assert.DoesNotContain(simulation.Log, line => line.Contains("not reached"));
    }

    // facts: a multiboard's rows and columns set in one instant read back; the displayed flag is the board's own
    [Fact]
    public void A_multiboard_reads_its_counts_title_and_flags()
    {
        var lines = Run("""
            local a, b = CreateMultiboard(), CreateMultiboard()
            print(MultiboardGetRowCount(a), MultiboardGetColumnCount(a), MultiboardGetItem(a, 0, 0))
            MultiboardSetColumnCount(a, 2)
            for _, rows in ipairs({0, 5, 2, 9, 1, 12, 0, 7}) do MultiboardSetRowCount(a, rows) end
            local cell = MultiboardGetItem(a, 6, 1)
            print(MultiboardGetRowCount(a), MultiboardGetColumnCount(a), cell == MultiboardGetItem(a, 6, 1), MultiboardGetItem(a, 7, 0), MultiboardGetItem(a, 0, 2))
            MultiboardSetItemValue(cell, "x")
            MultiboardReleaseItem(cell)
            MultiboardSetTitleText(a, "Title")
            MultiboardDisplay(a, true)
            MultiboardDisplay(b, true)
            MultiboardMinimize(b, true)
            print(MultiboardGetTitleText(a), IsMultiboardDisplayed(a), IsMultiboardDisplayed(b), IsMultiboardMinimized(a), IsMultiboardMinimized(b))
            MultiboardClear(a)
            print(MultiboardGetRowCount(a), MultiboardGetColumnCount(a))
            """);

        Assert.Equal(["0\t0\tnil", "7\t2\ttrue\tnil\tnil", "Title\ttrue\ttrue\tfalse\ttrue", "0\t0"], lines);
    }

    [Fact]
    public void Quests_timer_dialogs_and_dialogs_read_their_flags_back()
    {
        var lines = Run("""
            local q = CreateQuest()
            local item = QuestCreateItem(q)
            QuestSetRequired(q, false)
            QuestSetCompleted(q, true)
            QuestSetFailed(q, true)
            QuestSetDiscovered(q, false)
            QuestSetEnabled(q, false)
            QuestItemSetCompleted(item, true)
            print(IsQuestRequired(q), IsQuestCompleted(q), IsQuestFailed(q), IsQuestDiscovered(q), IsQuestEnabled(q), IsQuestItemCompleted(item))
            local t = CreateTimerDialog(CreateTimer())
            print(IsTimerDialogDisplayed(t))
            TimerDialogDisplay(t, true)
            print(IsTimerDialogDisplayed(t))
            local d = DialogCreate()
            DialogSetMessage(d, "Pick")
            local one, two = DialogAddButton(d, "One", 49), DialogAddQuitButton(d, true, "Quit", 81)
            DialogDisplay(Player(0), d, true)
            print(one ~= nil, two ~= nil, GetClickedButton(), GetClickedDialog(), TriggerRegisterDialogEvent(CreateTrigger(), d) ~= nil)
            DialogClear(d)
            """);

        Assert.Equal(["false\ttrue\ttrue\tfalse\tfalse\ttrue", "false", "true", "true\ttrue\tnil\tnil\ttrue"], lines);
    }
}
