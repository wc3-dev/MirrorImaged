namespace MirrorImaged.Natives;

public partial class leaderboard
{
    internal readonly List<LeaderboardItem> Items = [];
    internal string Label = "";
    internal bool Displayed, Destroyed;
}

sealed class LeaderboardItem(string label, int value, player? owner)
{
    public string Label = label;
    public int Value = value;
    public readonly player? Owner = owner;
}

public partial class multiboard
{
    internal int Rows, Columns;
    internal string Title = "";
    internal bool Displayed, Minimized, Destroyed;
    internal readonly Dictionary<(int Row, int Column), multiboarditem> Cells = [];
}

public partial class quest
{
    internal string Title = "", Description = "", Icon = "";
    internal bool Required = true, Completed, Failed, Discovered = true, Enabled = true, Destroyed;
}

public partial class questitem
{
    internal string Description = "";
    internal bool Completed;
}

public partial class timerdialog
{
    internal bool Displayed, Destroyed;
}

public partial class dialog
{
    internal string Message = "";
    internal readonly List<button> Buttons = [];
}

public partial class button
{
    internal string Text = "";
    internal int Hotkey;
    internal bool Quits;
    internal bool ScoreScreen;
}

// The boards, quests, timer dialogs and dialogs of the interface (game-facts, "Presentation" and "UI: frames"): what their
// getters read back is kept; what they draw is Recorded. A click on a dialog needs input (wave 6).
public sealed partial class GameNatives
{
    const string BoardFacts = "game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)";

    /// <summary>A new leaderboard with no rows and an empty label, not shown.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)); only what the getters read is kept; the game's drawing isn't simulated.</remarks>
    [Native(Level.Close, Fact = BoardFacts)]
    public leaderboard? CreateLeaderboard() => Make(new leaderboard());

    /// <summary>Marks the leaderboard destroyed and not shown; its rows and label stay.</summary>
    /// <remarks>Recorded: The leaderboard is drawn; it keeps its rows and flags.</remarks>
    [Native(Level.Recorded, Reason = "The leaderboard is drawn; it keeps its rows and flags")]
    public void DestroyLeaderboard(leaderboard? lb)
    {
        if (lb is null) return;
        (lb.Destroyed, lb.Displayed) = (true, false);
    }

    /// <summary>A row for the player after the others, also for a player who has one (the count goes to 2: facts, 11-07).</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)).</remarks>
    [Native(Level.Exact, Fact = BoardFacts)]
    public void LeaderboardAddItem(leaderboard? lb, string? label, int value, player? p) => lb?.Items.Add(new(label ?? "", value, p));

    /// <summary>Removes every row of the leaderboard.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)).</remarks>
    [Native(Level.Exact, Fact = BoardFacts)]
    public void LeaderboardClear(leaderboard? lb) => lb?.Items.Clear();

    /// <summary>The number of rows; 0 for a null leaderboard.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)).</remarks>
    [Native(Level.Exact, Fact = BoardFacts)]
    public int LeaderboardGetItemCount(leaderboard? lb) => lb?.Items.Count ?? 0;

    /// <summary>The leaderboard's label, empty until set; nil for a null leaderboard.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)).</remarks>
    [Native(Level.Exact, Fact = BoardFacts)]
    public string? LeaderboardGetLabelText(leaderboard? lb) => lb?.Label;

    /// <summary>The index of the player's first row, as sorted; -1 without one (facts: 0 for a player with two).</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)).</remarks>
    [Native(Level.Exact, Fact = BoardFacts)]
    public int LeaderboardGetPlayerIndex(leaderboard? lb, player? p) => lb is null || p is null ? -1 : lb.Items.FindIndex(item => item.Owner == p);

    /// <summary>Whether the player has a row in the leaderboard; false for a null leaderboard or player.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)).</remarks>
    [Native(Level.Exact, Fact = BoardFacts)]
    public bool LeaderboardHasPlayerItem(leaderboard? lb, player? p) => LeaderboardGetPlayerIndex(lb, p) >= 0;

    /// <summary>Removes the row at the index; nothing for an index outside the rows.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)); an index outside the rows is ignored, not measured in the game.</remarks>
    [Native(Level.Close, Fact = BoardFacts)]
    public void LeaderboardRemoveItem(leaderboard? lb, int index)
    {
        if (lb is not null && index >= 0 && index < lb.Items.Count) lb.Items.RemoveAt(index);
    }

    /// <summary>Removes the player's first row; nothing if the player has none.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)); which of a player's two rows goes is not measured in the game.</remarks>
    [Native(Level.Close, Fact = BoardFacts)]
    public void LeaderboardRemovePlayerItem(leaderboard? lb, player? p)
    {
        if (LeaderboardGetPlayerIndex(lb, p) is var at and >= 0) lb!.Items.RemoveAt(at);
    }

    /// <summary>Sets the leaderboard's label; nothing for a null leaderboard.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)).</remarks>
    [Native(Level.Exact, Fact = BoardFacts)]
    public void LeaderboardSetLabel(leaderboard? lb, string? label)
    {
        if (lb is not null) lb.Label = label ?? "";
    }

    /// <summary>Sets the label of the row at the index; nothing for an index outside the rows.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)); an index outside the rows is ignored, not measured in the game.</remarks>
    [Native(Level.Close, Fact = BoardFacts)]
    public void LeaderboardSetItemLabel(leaderboard? lb, int whichItem, string? val)
    {
        if (lb is not null && whichItem >= 0 && whichItem < lb.Items.Count) lb.Items[whichItem].Label = val ?? "";
    }

    /// <summary>Sets the value of the row at the index; nothing for an index outside the rows.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)); an index outside the rows is ignored, not measured in the game.</remarks>
    [Native(Level.Close, Fact = BoardFacts)]
    public void LeaderboardSetItemValue(leaderboard? lb, int whichItem, int val)
    {
        if (lb is not null && whichItem >= 0 && whichItem < lb.Items.Count) lb.Items[whichItem].Value = val;
    }

    /// <summary>Sets whether the leaderboard is shown, for IsLeaderboardDisplayed; nothing is drawn.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)); only the flag the getter reads is kept; the game's drawing isn't simulated.</remarks>
    [Native(Level.Close, Fact = BoardFacts)]
    public void LeaderboardDisplay(leaderboard? lb, bool show)
    {
        if (lb is not null) lb.Displayed = show;
    }

    /// <summary>Whether the leaderboard is shown; false for a null leaderboard.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)); only the flag set by LeaderboardDisplay is read; the game's drawing isn't simulated.</remarks>
    [Native(Level.Close, Fact = BoardFacts)]
    public bool IsLeaderboardDisplayed(leaderboard? lb) => lb?.Displayed ?? false;

    /// <summary>Sorts the rows by label in code-point order, the order of equals kept; highest first when not ascending.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)); the game's order of labels is not measured; here it is by code point.</remarks>
    [Native(Level.Close, Fact = BoardFacts)]
    public void LeaderboardSortItemsByLabel(leaderboard? lb, bool ascending) => SortRows(lb, item => item.Label, ascending, StringComparer.Ordinal);

    /// <summary>Sorts the rows by value, the order of equals kept; highest first when not ascending.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)); the game's order of equal values is not measured; here it is kept.</remarks>
    [Native(Level.Close, Fact = BoardFacts)]
    public void LeaderboardSortItemsByValue(leaderboard? lb, bool ascending) => SortRows(lb, item => item.Value, ascending);

    /// <summary>Sorts the rows by the player's number, a row without a player first, the order of equals kept; highest first when not ascending.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)); the game's order of equal players is not measured; here it is kept.</remarks>
    [Native(Level.Close, Fact = BoardFacts)]
    public void LeaderboardSortItemsByPlayer(leaderboard? lb, bool ascending) => SortRows(lb, item => item.Owner is null ? -1 : GetPlayerId(item.Owner), ascending);

    // Highest first when not ascending, the order of equals kept
    static void SortRows<T>(leaderboard? lb, Func<LeaderboardItem, T> key, bool ascending, IComparer<T>? comparer = null)
    {
        if (lb is null) return;
        var sorted = (ascending ? lb.Items.OrderBy(key, comparer) : lb.Items.OrderByDescending(key, comparer)).ToList();
        lb.Items.Clear();
        lb.Items.AddRange(sorted);
    }

    const string BoardDrawn = "The leaderboard's colours, styles and size are drawn; no native reads them back";

    /// <summary>Changes nothing; the row's label colour isn't kept.</summary>
    /// <remarks>Recorded: The leaderboard's colours, styles and size are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = BoardDrawn)] public void LeaderboardSetItemLabelColor(leaderboard? lb, int whichItem, int red, int green, int blue, int alpha) { }
    /// <summary>Changes nothing; the row's style isn't kept.</summary>
    /// <remarks>Recorded: The leaderboard's colours, styles and size are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = BoardDrawn)] public void LeaderboardSetItemStyle(leaderboard? lb, int whichItem, bool showLabel, bool showValue, bool showIcon) { }
    /// <summary>Changes nothing; the row's value colour isn't kept.</summary>
    /// <remarks>Recorded: The leaderboard's colours, styles and size are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = BoardDrawn)] public void LeaderboardSetItemValueColor(leaderboard? lb, int whichItem, int red, int green, int blue, int alpha) { }
    /// <summary>Changes nothing; the label's colour isn't kept.</summary>
    /// <remarks>Recorded: The leaderboard's colours, styles and size are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = BoardDrawn)] public void LeaderboardSetLabelColor(leaderboard? lb, int red, int green, int blue, int alpha) { }
    /// <summary>Changes nothing; the size isn't kept.</summary>
    /// <remarks>Recorded: The leaderboard's colours, styles and size are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = BoardDrawn)] public void LeaderboardSetSizeByItemCount(leaderboard? lb, int count) { }
    /// <summary>Changes nothing; the leaderboard's style isn't kept.</summary>
    /// <remarks>Recorded: The leaderboard's colours, styles and size are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = BoardDrawn)] public void LeaderboardSetStyle(leaderboard? lb, bool showLabel, bool showNames, bool showValues, bool showIcons) { }
    /// <summary>Changes nothing; the values' colour isn't kept.</summary>
    /// <remarks>Recorded: The leaderboard's colours, styles and size are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = BoardDrawn)] public void LeaderboardSetValueColor(leaderboard? lb, int red, int green, int blue, int alpha) { }

    readonly Dictionary<player, leaderboard> _boards = [];

    /// <summary>
    /// A leaderboard some player has two rows for closes the game, as it did twice (facts, 11-07); the player's board is
    /// kept for PlayerGetLeaderboard.
    /// </summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)); the player's board is kept only for PlayerGetLeaderboard; what the game shows the player isn't simulated.</remarks>
    [Native(Level.Close, Fact = BoardFacts)]
    public void PlayerSetLeaderboard(player? toPlayer, leaderboard? lb)
    {
        if (toPlayer is null) return;
        if (lb is not null && lb.Items.Where(item => item.Owner is not null).GroupBy(item => item.Owner).Any(rows => rows.Count() > 1))
            throw new GameClosedException("The game closes: a leaderboard with two rows for one player was set as a player's leaderboard");
        if (lb is null) _boards.Remove(toPlayer);
        else _boards[toPlayer] = lb;
    }

    /// <summary>The leaderboard set for the player; nil if none was set or for a null player.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)); only what PlayerSetLeaderboard kept is read back, and a player's default board is not measured in the game.</remarks>
    [Native(Level.Close, Fact = BoardFacts)]
    public leaderboard? PlayerGetLeaderboard(player? toPlayer) => toPlayer is not null && _boards.TryGetValue(toPlayer, out var lb) ? lb : null;

    /// <summary>A new multiboard with no rows or columns and an empty title, not shown.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)); only what the getters read is kept; the game's drawing isn't simulated.</remarks>
    [Native(Level.Close, Fact = BoardFacts)]
    public multiboard? CreateMultiboard() => Make(new multiboard());

    /// <summary>Marks the board destroyed and not shown; its rows, columns and title stay.</summary>
    /// <remarks>Recorded: The board is drawn; it keeps its rows, columns and flags.</remarks>
    [Native(Level.Recorded, Reason = "The board is drawn; it keeps its rows, columns and flags")]
    public void DestroyMultiboard(multiboard? lb)
    {
        if (lb is null) return;
        (lb.Destroyed, lb.Displayed) = (true, false);
    }

    /// <summary>Sets the number of rows, not below 0, and drops the cells of the rows that go.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)).</remarks>
    [Native(Level.Exact, Fact = BoardFacts)]
    public void MultiboardSetRowCount(multiboard? lb, int count)
    {
        if (lb is null) return;
        lb.Rows = Math.Max(count, 0);
        foreach (var key in lb.Cells.Keys.Where(cell => cell.Row >= lb.Rows).ToArray()) lb.Cells.Remove(key);
    }

    /// <summary>The number of rows; 0 for a null board.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)).</remarks>
    [Native(Level.Exact, Fact = BoardFacts)]
    public int MultiboardGetRowCount(multiboard? lb) => lb?.Rows ?? 0;

    /// <summary>Sets the number of columns, not below 0, and drops the cells of the columns that go.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)).</remarks>
    [Native(Level.Exact, Fact = BoardFacts)]
    public void MultiboardSetColumnCount(multiboard? lb, int count)
    {
        if (lb is null) return;
        lb.Columns = Math.Max(count, 0);
        foreach (var key in lb.Cells.Keys.Where(cell => cell.Column >= lb.Columns).ToArray()) lb.Cells.Remove(key);
    }

    /// <summary>The number of columns; 0 for a null board.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)).</remarks>
    [Native(Level.Exact, Fact = BoardFacts)]
    public int MultiboardGetColumnCount(multiboard? lb) => lb?.Columns ?? 0;

    /// <summary>Sets the board's title; nothing for a null board.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)).</remarks>
    [Native(Level.Exact, Fact = BoardFacts)]
    public void MultiboardSetTitleText(multiboard? lb, string? label)
    {
        if (lb is not null) lb.Title = label ?? "";
    }

    /// <summary>The board's title, empty until set; nil for a null board.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)).</remarks>
    [Native(Level.Exact, Fact = BoardFacts)]
    public string? MultiboardGetTitleText(multiboard? lb) => lb?.Title;

    /// <summary>The flag is the board's own: with two boards shown the read is true for both (facts, 11-07).</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)).</remarks>
    [Native(Level.Exact, Fact = BoardFacts)]
    public void MultiboardDisplay(multiboard? lb, bool show)
    {
        if (lb is not null) lb.Displayed = show;
    }

    /// <summary>Whether the board is shown; false for a null board.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)).</remarks>
    [Native(Level.Exact, Fact = BoardFacts)]
    public bool IsMultiboardDisplayed(multiboard? lb) => lb?.Displayed ?? false;

    /// <summary>Sets whether the board is minimised, for IsMultiboardMinimized; nothing is drawn.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)); only the flag the getter reads is kept; the game's drawing isn't simulated.</remarks>
    [Native(Level.Close, Fact = BoardFacts)]
    public void MultiboardMinimize(multiboard? lb, bool minimize)
    {
        if (lb is not null) lb.Minimized = minimize;
    }

    /// <summary>Whether the board is minimised; false for a null board.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)); only the flag set by MultiboardMinimize is read; the game's drawing isn't simulated.</remarks>
    [Native(Level.Close, Fact = BoardFacts)]
    public bool IsMultiboardMinimized(multiboard? lb) => lb?.Minimized ?? false;

    /// <summary>Sets the rows and columns to 0 and drops every cell.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)); only what the getters read is kept; the game's drawing isn't simulated.</remarks>
    [Native(Level.Close, Fact = BoardFacts)]
    public void MultiboardClear(multiboard? lb)
    {
        if (lb is null) return;
        (lb.Rows, lb.Columns) = (0, 0);
        lb.Cells.Clear();
    }

    /// <summary>The cell at the row and column (from 0), nil outside the board; the same handle each call until released.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)); what the game does with a cell handle after MultiboardReleaseItem is not measured.</remarks>
    [Native(Level.Close, Fact = BoardFacts)]
    public multiboarditem? MultiboardGetItem(multiboard? lb, int row, int column)
    {
        if (lb is null || row < 0 || column < 0 || row >= lb.Rows || column >= lb.Columns) return null;
        return lb.Cells.TryGetValue((row, column), out var cell) ? cell : lb.Cells[(row, column)] = Make(new multiboarditem());
    }

    /// <summary>Changes nothing; the cell and its handle stay as they were.</summary>
    /// <remarks>Recorded: The cell handle is let go; the cell keeps what it shows.</remarks>
    [Native(Level.Recorded, Reason = "The cell handle is let go; the cell keeps what it shows")]
    public void MultiboardReleaseItem(multiboarditem? mbi) { }

    /// <summary>Changes nothing; whether boards show during a cinematic isn't kept.</summary>
    /// <remarks>Recorded: Whether the boards show during the cinematic is drawn; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "Whether the boards show during the cinematic is drawn; no native reads it back")]
    public void MultiboardSuppressDisplay(bool flag) { }

    const string CellDrawn = "A multiboard cell's text, icon, colour and width are drawn; no native reads them back";

    /// <summary>Changes nothing; the cell's icon isn't kept.</summary>
    /// <remarks>Recorded: A multiboard cell's text, icon, colour and width are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = CellDrawn)] public void MultiboardSetItemIcon(multiboarditem? mbi, string? iconFileName) { }
    /// <summary>Changes nothing; the cell's style isn't kept.</summary>
    /// <remarks>Recorded: A multiboard cell's text, icon, colour and width are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = CellDrawn)] public void MultiboardSetItemStyle(multiboarditem? mbi, bool showValue, bool showIcon) { }
    /// <summary>Changes nothing; the cell's text isn't kept.</summary>
    /// <remarks>Recorded: A multiboard cell's text, icon, colour and width are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = CellDrawn)] public void MultiboardSetItemValue(multiboarditem? mbi, string? val) { }
    /// <summary>Changes nothing; the cell's colour isn't kept.</summary>
    /// <remarks>Recorded: A multiboard cell's text, icon, colour and width are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = CellDrawn)] public void MultiboardSetItemValueColor(multiboarditem? mbi, int red, int green, int blue, int alpha) { }
    /// <summary>Changes nothing; the cell's width isn't kept.</summary>
    /// <remarks>Recorded: A multiboard cell's text, icon, colour and width are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = CellDrawn)] public void MultiboardSetItemWidth(multiboarditem? mbi, float width) { }
    /// <summary>Changes nothing; the icon of the cells isn't kept.</summary>
    /// <remarks>Recorded: A multiboard cell's text, icon, colour and width are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = CellDrawn)] public void MultiboardSetItemsIcon(multiboard? lb, string? iconPath) { }
    /// <summary>Changes nothing; the style of the cells isn't kept.</summary>
    /// <remarks>Recorded: A multiboard cell's text, icon, colour and width are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = CellDrawn)] public void MultiboardSetItemsStyle(multiboard? lb, bool showValues, bool showIcons) { }
    /// <summary>Changes nothing; the text of the cells isn't kept.</summary>
    /// <remarks>Recorded: A multiboard cell's text, icon, colour and width are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = CellDrawn)] public void MultiboardSetItemsValue(multiboard? lb, string? value) { }
    /// <summary>Changes nothing; the colour of the cells isn't kept.</summary>
    /// <remarks>Recorded: A multiboard cell's text, icon, colour and width are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = CellDrawn)] public void MultiboardSetItemsValueColor(multiboard? lb, int red, int green, int blue, int alpha) { }
    /// <summary>Changes nothing; the width of the cells isn't kept.</summary>
    /// <remarks>Recorded: A multiboard cell's text, icon, colour and width are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = CellDrawn)] public void MultiboardSetItemsWidth(multiboard? lb, float width) { }
    /// <summary>Changes nothing; the title's colour isn't kept.</summary>
    /// <remarks>Recorded: The title's colour is drawn; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "The title's colour is drawn; no native reads it back")] public void MultiboardSetTitleTextColor(multiboard? lb, int red, int green, int blue, int alpha) { }

    /// <summary>A new quest, required, discovered and enabled, not completed or failed, with an empty title, description and icon.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: quests (11-07); defaults not measured); only what the getters read is kept; the quest log isn't drawn.</remarks>
    [Native(Level.Close, Fact = "game-facts.md, UI: quests (11-07); defaults not measured")]
    public quest? CreateQuest() => Make(new quest());

    /// <summary>Marks the quest destroyed; it keeps what it held.</summary>
    /// <remarks>Close: a destroyed quest's getters still read what it held; not measured in the game.</remarks>
    [Native(Level.Close)] public void DestroyQuest(quest? whichQuest) { if (whichQuest is not null) whichQuest.Destroyed = true; }
    /// <summary>Sets the quest's title; nothing for a null quest.</summary>
    /// <remarks>Close: nothing reads the title back; not measured in the game.</remarks>
    [Native(Level.Close)] public void QuestSetTitle(quest? whichQuest, string? title) { if (whichQuest is not null) whichQuest.Title = title ?? ""; }
    /// <summary>Sets the quest's description; nothing for a null quest.</summary>
    /// <remarks>Close: nothing reads the description back; not measured in the game.</remarks>
    [Native(Level.Close)] public void QuestSetDescription(quest? whichQuest, string? description) { if (whichQuest is not null) whichQuest.Description = description ?? ""; }
    /// <summary>Sets the path of the quest's icon; nothing for a null quest.</summary>
    /// <remarks>Close: nothing reads the icon back; not measured in the game.</remarks>
    [Native(Level.Close)] public void QuestSetIconPath(quest? whichQuest, string? iconPath) { if (whichQuest is not null) whichQuest.Icon = iconPath ?? ""; }
    /// <summary>Sets whether the quest is required; nothing for a null quest.</summary>
    /// <remarks>Close: kept for IsQuestRequired; not measured in the game.</remarks>
    [Native(Level.Close)] public void QuestSetRequired(quest? whichQuest, bool required) { if (whichQuest is not null) whichQuest.Required = required; }
    /// <summary>Sets whether the quest is completed; nothing for a null quest.</summary>
    /// <remarks>Close: kept for IsQuestCompleted; not measured in the game.</remarks>
    [Native(Level.Close)] public void QuestSetCompleted(quest? whichQuest, bool completed) { if (whichQuest is not null) whichQuest.Completed = completed; }
    /// <summary>Sets whether the quest has failed; nothing for a null quest.</summary>
    /// <remarks>Close: kept for IsQuestFailed; not measured in the game.</remarks>
    [Native(Level.Close)] public void QuestSetFailed(quest? whichQuest, bool failed) { if (whichQuest is not null) whichQuest.Failed = failed; }
    /// <summary>Sets whether the quest is discovered; nothing for a null quest.</summary>
    /// <remarks>Close: kept for IsQuestDiscovered; not measured in the game.</remarks>
    [Native(Level.Close)] public void QuestSetDiscovered(quest? whichQuest, bool discovered) { if (whichQuest is not null) whichQuest.Discovered = discovered; }
    /// <summary>Sets whether the quest is enabled; nothing for a null quest.</summary>
    /// <remarks>Close: kept for IsQuestEnabled; not measured in the game.</remarks>
    [Native(Level.Close)] public void QuestSetEnabled(quest? whichQuest, bool enabled) { if (whichQuest is not null) whichQuest.Enabled = enabled; }
    /// <summary>Whether the quest is required; true for a new quest, false for a null one.</summary>
    /// <remarks>Close: not measured in the game.</remarks>
    [Native(Level.Close)] public bool IsQuestRequired(quest? whichQuest) => whichQuest?.Required ?? false;
    /// <summary>Whether the quest is completed; false for a new quest or a null one.</summary>
    /// <remarks>Close: not measured in the game.</remarks>
    [Native(Level.Close)] public bool IsQuestCompleted(quest? whichQuest) => whichQuest?.Completed ?? false;
    /// <summary>Whether the quest has failed; false for a new quest or a null one.</summary>
    /// <remarks>Close: not measured in the game.</remarks>
    [Native(Level.Close)] public bool IsQuestFailed(quest? whichQuest) => whichQuest?.Failed ?? false;
    /// <summary>Whether the quest is discovered; true for a new quest, false for a null one.</summary>
    /// <remarks>Close: not measured in the game.</remarks>
    [Native(Level.Close)] public bool IsQuestDiscovered(quest? whichQuest) => whichQuest?.Discovered ?? false;
    /// <summary>Whether the quest is enabled; true for a new quest, false for a null one.</summary>
    /// <remarks>Close: not measured in the game.</remarks>
    [Native(Level.Close)] public bool IsQuestEnabled(quest? whichQuest) => whichQuest?.Enabled ?? false;
    /// <summary>A new quest item; nil for a null quest.</summary>
    /// <remarks>Close: the item isn't tied to the quest here; not measured in the game.</remarks>
    [Native(Level.Close)] public questitem? QuestCreateItem(quest? whichQuest) => whichQuest is null ? null : Make(new questitem());
    /// <summary>Sets the quest item's description; nothing for a null item.</summary>
    /// <remarks>Close: nothing reads the description back; not measured in the game.</remarks>
    [Native(Level.Close)] public void QuestItemSetDescription(questitem? whichQuestItem, string? description) { if (whichQuestItem is not null) whichQuestItem.Description = description ?? ""; }
    /// <summary>Sets whether the quest item is completed; nothing for a null item.</summary>
    /// <remarks>Close: kept for IsQuestItemCompleted; not measured in the game.</remarks>
    [Native(Level.Close)] public void QuestItemSetCompleted(questitem? whichQuestItem, bool completed) { if (whichQuestItem is not null) whichQuestItem.Completed = completed; }
    /// <summary>Whether the quest item is completed; false for a new item or a null one.</summary>
    /// <remarks>Close: not measured in the game.</remarks>
    [Native(Level.Close)] public bool IsQuestItemCompleted(questitem? whichQuestItem) => whichQuestItem?.Completed ?? false;

    /// <summary>Changes nothing; no button flashes.</summary>
    /// <remarks>Recorded: The quest log's button flash is drawn; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "The quest log's button flash is drawn; no native reads it back")] public void FlashQuestDialogButton() { }
    /// <summary>Changes nothing; no quest log is redrawn.</summary>
    /// <remarks>Recorded: The quest log is redrawn; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "The quest log is redrawn; no native reads it back")] public void ForceQuestDialogUpdate() { }
    /// <summary>Makes a defeat condition handle; nothing is listed.</summary>
    /// <remarks>Recorded: A defeat condition is listed in the quest log; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "A defeat condition is listed in the quest log; no native reads it back")] public defeatcondition? CreateDefeatCondition() => Make(new defeatcondition());
    /// <summary>Changes nothing; nothing is listed or removed.</summary>
    /// <remarks>Recorded: A defeat condition is listed in the quest log; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "A defeat condition is listed in the quest log; no native reads it back")] public void DestroyDefeatCondition(defeatcondition? whichCondition) { }
    /// <summary>Changes nothing; the description isn't kept.</summary>
    /// <remarks>Recorded: A defeat condition is listed in the quest log; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "A defeat condition is listed in the quest log; no native reads it back")] public void DefeatConditionSetDescription(defeatcondition? whichCondition, string? description) { }

    /// <summary>A new timer dialog, not shown; the timer isn't tied to it.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)); only whether it is shown is kept; the timer's time isn't shown or read.</remarks>
    [Native(Level.Close, Fact = BoardFacts)]
    public timerdialog? CreateTimerDialog(timer? t) => Make(new timerdialog());

    /// <summary>Marks the timer dialog destroyed and not shown.</summary>
    /// <remarks>Close: not measured in the game.</remarks>
    [Native(Level.Close)] public void DestroyTimerDialog(timerdialog? whichDialog) { if (whichDialog is null) return; (whichDialog.Destroyed, whichDialog.Displayed) = (true, false); }
    /// <summary>Sets whether the timer dialog is shown.</summary>
    /// <remarks>Close: only the flag the getter reads is kept; not measured in the game.</remarks>
    [Native(Level.Close)] public void TimerDialogDisplay(timerdialog? whichDialog, bool display) { if (whichDialog is not null) whichDialog.Displayed = display; }
    /// <summary>Whether the timer dialog is shown; false for a new one or a null one.</summary>
    /// <remarks>Close: only the flag set by TimerDialogDisplay is read; not measured in the game.</remarks>
    [Native(Level.Close)] public bool IsTimerDialogDisplayed(timerdialog? whichDialog) => whichDialog?.Displayed ?? false;
    /// <summary>Changes nothing; the title isn't kept.</summary>
    /// <remarks>Recorded: The timer window's title, time and colours are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "The timer window's title, time and colours are drawn; no native reads them back")] public void TimerDialogSetTitle(timerdialog? whichDialog, string? title) { }
    /// <summary>Changes nothing; the title's colour isn't kept.</summary>
    /// <remarks>Recorded: The timer window's title, time and colours are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "The timer window's title, time and colours are drawn; no native reads them back")] public void TimerDialogSetTitleColor(timerdialog? whichDialog, int red, int green, int blue, int alpha) { }
    /// <summary>Changes nothing; the time's colour isn't kept.</summary>
    /// <remarks>Recorded: The timer window's title, time and colours are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "The timer window's title, time and colours are drawn; no native reads them back")] public void TimerDialogSetTimeColor(timerdialog? whichDialog, int red, int green, int blue, int alpha) { }
    /// <summary>Changes nothing; the speed isn't kept.</summary>
    /// <remarks>Recorded: The timer window's title, time and colours are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "The timer window's title, time and colours are drawn; no native reads them back")] public void TimerDialogSetSpeed(timerdialog? whichDialog, float speedMultFactor) { }
    /// <summary>Changes nothing; the time remaining isn't kept.</summary>
    /// <remarks>Recorded: The timer window's title, time and colours are drawn; no native reads them back.</remarks>
    [Native(Level.Recorded, Reason = "The timer window's title, time and colours are drawn; no native reads them back")] public void TimerDialogSetRealTimeRemaining(timerdialog? whichDialog, float timeRemaining) { }

    /// <summary>A new dialog with no buttons and an empty message.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)); only the message and the buttons are kept; the game's drawing isn't simulated.</remarks>
    [Native(Level.Close, Fact = BoardFacts)]
    public dialog? DialogCreate() => Make(new dialog());

    // What each player is shown, one dialog at a time, and the registrations a click fires: a button's, then its dialog's (D795)
    readonly Dictionary<player, dialog> _shown = [];
    readonly List<(dialog? Dialog, button? Button, @event Event)> _dialogEvents = [];
    (dialog Dialog, button Button)? _clicked;

    /// <summary>Removes the dialog's buttons and hides it from every player.</summary>
    /// <remarks>Close: not measured in the game.</remarks>
    [Native(Level.Close)] public void DialogDestroy(dialog? whichDialog) { whichDialog?.Buttons.Clear(); Hide(whichDialog); }

    void Hide(dialog? whichDialog)
    {
        foreach (var (shown, _) in _shown.Where(pair => pair.Value == whichDialog).ToList()) _shown.Remove(shown);
    }
    /// <summary>Removes the dialog's buttons.</summary>
    /// <remarks>Close: not measured in the game.</remarks>
    [Native(Level.Close)] public void DialogClear(dialog? whichDialog) => whichDialog?.Buttons.Clear();
    /// <summary>Sets the dialog's message; nothing for a null dialog.</summary>
    /// <remarks>Close: not measured in the game.</remarks>
    [Native(Level.Close)] public void DialogSetMessage(dialog? whichDialog, string? messageText) { if (whichDialog is not null) whichDialog.Message = messageText ?? ""; }

    /// <summary>Adds a button with the text and hotkey; nil for a null dialog.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)); a click is not measured, as a probe cannot click (D795).</remarks>
    [Native(Level.Close, Fact = BoardFacts)]
    public button? DialogAddButton(dialog? whichDialog, string? buttonText, int hotkey)
    {
        if (whichDialog is null) return null;
        var added = Make(new button { Text = buttonText ?? "", Hotkey = hotkey });
        whichDialog.Buttons.Add(added);
        return added;
    }

    /// <summary>As DialogAddButton, for a button that ends the run when clicked, with the score screen if asked.</summary>
    /// <remarks>Close: measured in part (game-facts.md, UI: boards and dialogs (11-07, probe probe-boards-and-dialogs)); a click is not measured, as a probe cannot click (D795).</remarks>
    [Native(Level.Close, Fact = BoardFacts)]
    public button? DialogAddQuitButton(dialog? whichDialog, bool doScoreScreen, string? buttonText, int hotkey)
    {
        var added = DialogAddButton(whichDialog, buttonText, hotkey);
        if (added is not null) (added.Quits, added.ScoreScreen) = (true, doScoreScreen);
        return added;
    }

    // Shown until a button is clicked or it is hidden. Close: a probe cannot click, so the click events and what they answer are the documented ones (D795)
    /// <summary>Shows the dialog to the player in place of any it is shown, or hides it if it is the one shown; nothing for a null player or dialog.</summary>
    /// <remarks>Close: a probe cannot click, so the click events and what they answer are the documented ones (D795).</remarks>
    [Native(Level.Close)]
    public void DialogDisplay(player? whichPlayer, dialog? whichDialog, bool flag)
    {
        if (whichPlayer is null || whichDialog is null) return;
        if (flag) _shown[whichPlayer] = whichDialog;
        else if (_shown.GetValueOrDefault(whichPlayer) == whichDialog) _shown.Remove(whichPlayer);
    }

    /// <summary>Registers the trigger for a click on any button of the dialog; nil for a null trigger or dialog.</summary>
    /// <remarks>Close: a probe cannot click, so the click events are the documented ones (D795).</remarks>
    [Native(Level.Close)]
    public @event? TriggerRegisterDialogEvent(trigger? whichTrigger, dialog? whichDialog)
    {
        if (whichTrigger is null || whichDialog is null) return null;
        var registered = Register(whichTrigger);
        _dialogEvents.Add((whichDialog, null, registered));
        return registered;
    }

    /// <summary>Registers the trigger for a click on the button; nil for a null trigger or button.</summary>
    /// <remarks>Close: a probe cannot click, so the click events are the documented ones (D795).</remarks>
    [Native(Level.Close)]
    public @event? TriggerRegisterDialogButtonEvent(trigger? whichTrigger, button? whichButton)
    {
        if (whichTrigger is null || whichButton is null) return null;
        var registered = Register(whichTrigger);
        _dialogEvents.Add((null, whichButton, registered));
        return registered;
    }

    /// <summary>The button of the click being answered; nil outside one.</summary>
    /// <remarks>Close: a probe cannot click, so what it reads is the documented one (D795).</remarks>
    [Native(Level.Close)] public button? GetClickedButton() => _clicked?.Button;
    /// <summary>The dialog of the click being answered; nil outside one.</summary>
    /// <remarks>Close: a probe cannot click, so what it reads is the documented one (D795).</remarks>
    [Native(Level.Close)] public dialog? GetClickedDialog() => _clicked?.Dialog;

    // The player clicks a button of the dialog it is shown: the dialog closes, the button's events fire, then the dialog's, and a quit button ends the run
    internal void Click(player p, int index)
    {
        if (!_shown.TryGetValue(p, out var dialog) || index < 0 || index >= dialog.Buttons.Count) return;
        var button = dialog.Buttons[index];
        _shown.Remove(p);
        var outer = _clicked;
        _clicked = (dialog, button);
        try
        {
            foreach (var (_, registeredButton, registered) in _dialogEvents.Where(e => e.Button == button).ToArray()) Fire(registered, Convert<dialogevent>(90), player: p);
            foreach (var (registeredDialog, _, registered) in _dialogEvents.Where(e => e.Dialog == dialog).ToArray()) Fire(registered, Convert<dialogevent>(91), player: p);
        }
        finally { _clicked = outer; }
        if (button.Quits) EndGame(button.ScoreScreen);
    }
}
