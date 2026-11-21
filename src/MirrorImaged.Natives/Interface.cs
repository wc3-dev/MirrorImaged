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

    [Native(Level.Close, Fact = BoardFacts)]
    public leaderboard? CreateLeaderboard() => Make(new leaderboard());

    [Native(Level.Recorded, Reason = "The leaderboard is drawn; it keeps its rows and flags")]
    public void DestroyLeaderboard(leaderboard? lb)
    {
        if (lb is null) return;
        (lb.Destroyed, lb.Displayed) = (true, false);
    }

    /// <summary>A row for the player after the others, also for a player who has one (the count goes to 2: facts, 11-07).</summary>
    [Native(Level.Exact, Fact = BoardFacts)]
    public void LeaderboardAddItem(leaderboard? lb, string? label, int value, player? p) => lb?.Items.Add(new(label ?? "", value, p));

    [Native(Level.Exact, Fact = BoardFacts)]
    public void LeaderboardClear(leaderboard? lb) => lb?.Items.Clear();

    [Native(Level.Exact, Fact = BoardFacts)]
    public int LeaderboardGetItemCount(leaderboard? lb) => lb?.Items.Count ?? 0;

    [Native(Level.Exact, Fact = BoardFacts)]
    public string? LeaderboardGetLabelText(leaderboard? lb) => lb?.Label;

    /// <summary>The index of the player's first row, as sorted; -1 without one (facts: 0 for a player with two).</summary>
    [Native(Level.Exact, Fact = BoardFacts)]
    public int LeaderboardGetPlayerIndex(leaderboard? lb, player? p) => lb is null || p is null ? -1 : lb.Items.FindIndex(item => item.Owner == p);

    [Native(Level.Exact, Fact = BoardFacts)]
    public bool LeaderboardHasPlayerItem(leaderboard? lb, player? p) => LeaderboardGetPlayerIndex(lb, p) >= 0;

    [Native(Level.Close, Fact = BoardFacts)]
    public void LeaderboardRemoveItem(leaderboard? lb, int index)
    {
        if (lb is not null && index >= 0 && index < lb.Items.Count) lb.Items.RemoveAt(index);
    }

    [Native(Level.Close, Fact = BoardFacts)]
    public void LeaderboardRemovePlayerItem(leaderboard? lb, player? p)
    {
        if (LeaderboardGetPlayerIndex(lb, p) is var at and >= 0) lb!.Items.RemoveAt(at);
    }

    [Native(Level.Exact, Fact = BoardFacts)]
    public void LeaderboardSetLabel(leaderboard? lb, string? label)
    {
        if (lb is not null) lb.Label = label ?? "";
    }

    [Native(Level.Close, Fact = BoardFacts)]
    public void LeaderboardSetItemLabel(leaderboard? lb, int whichItem, string? val)
    {
        if (lb is not null && whichItem >= 0 && whichItem < lb.Items.Count) lb.Items[whichItem].Label = val ?? "";
    }

    [Native(Level.Close, Fact = BoardFacts)]
    public void LeaderboardSetItemValue(leaderboard? lb, int whichItem, int val)
    {
        if (lb is not null && whichItem >= 0 && whichItem < lb.Items.Count) lb.Items[whichItem].Value = val;
    }

    [Native(Level.Close, Fact = BoardFacts)]
    public void LeaderboardDisplay(leaderboard? lb, bool show)
    {
        if (lb is not null) lb.Displayed = show;
    }

    [Native(Level.Close, Fact = BoardFacts)]
    public bool IsLeaderboardDisplayed(leaderboard? lb) => lb?.Displayed ?? false;

    [Native(Level.Close, Fact = BoardFacts)]
    public void LeaderboardSortItemsByLabel(leaderboard? lb, bool ascending) => SortRows(lb, item => item.Label, ascending, StringComparer.Ordinal);

    [Native(Level.Close, Fact = BoardFacts)]
    public void LeaderboardSortItemsByValue(leaderboard? lb, bool ascending) => SortRows(lb, item => item.Value, ascending);

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

    [Native(Level.Recorded, Reason = BoardDrawn)] public void LeaderboardSetItemLabelColor(leaderboard? lb, int whichItem, int red, int green, int blue, int alpha) { }
    [Native(Level.Recorded, Reason = BoardDrawn)] public void LeaderboardSetItemStyle(leaderboard? lb, int whichItem, bool showLabel, bool showValue, bool showIcon) { }
    [Native(Level.Recorded, Reason = BoardDrawn)] public void LeaderboardSetItemValueColor(leaderboard? lb, int whichItem, int red, int green, int blue, int alpha) { }
    [Native(Level.Recorded, Reason = BoardDrawn)] public void LeaderboardSetLabelColor(leaderboard? lb, int red, int green, int blue, int alpha) { }
    [Native(Level.Recorded, Reason = BoardDrawn)] public void LeaderboardSetSizeByItemCount(leaderboard? lb, int count) { }
    [Native(Level.Recorded, Reason = BoardDrawn)] public void LeaderboardSetStyle(leaderboard? lb, bool showLabel, bool showNames, bool showValues, bool showIcons) { }
    [Native(Level.Recorded, Reason = BoardDrawn)] public void LeaderboardSetValueColor(leaderboard? lb, int red, int green, int blue, int alpha) { }

    readonly Dictionary<player, leaderboard> _boards = [];

    /// <summary>
    /// A leaderboard some player has two rows for closes the game, as it did twice (facts, 11-07); the player's board is
    /// kept for PlayerGetLeaderboard.
    /// </summary>
    [Native(Level.Close, Fact = BoardFacts)]
    public void PlayerSetLeaderboard(player? toPlayer, leaderboard? lb)
    {
        if (toPlayer is null) return;
        if (lb is not null && lb.Items.Where(item => item.Owner is not null).GroupBy(item => item.Owner).Any(rows => rows.Count() > 1))
            throw new GameClosedException("The game closes: a leaderboard with two rows for one player was set as a player's leaderboard");
        if (lb is null) _boards.Remove(toPlayer);
        else _boards[toPlayer] = lb;
    }

    [Native(Level.Close, Fact = BoardFacts)]
    public leaderboard? PlayerGetLeaderboard(player? toPlayer) => toPlayer is not null && _boards.TryGetValue(toPlayer, out var lb) ? lb : null;

    [Native(Level.Close, Fact = BoardFacts)]
    public multiboard? CreateMultiboard() => Make(new multiboard());

    [Native(Level.Recorded, Reason = "The board is drawn; it keeps its rows, columns and flags")]
    public void DestroyMultiboard(multiboard? lb)
    {
        if (lb is null) return;
        (lb.Destroyed, lb.Displayed) = (true, false);
    }

    [Native(Level.Exact, Fact = BoardFacts)]
    public void MultiboardSetRowCount(multiboard? lb, int count)
    {
        if (lb is null) return;
        lb.Rows = Math.Max(count, 0);
        foreach (var key in lb.Cells.Keys.Where(cell => cell.Row >= lb.Rows).ToArray()) lb.Cells.Remove(key);
    }

    [Native(Level.Exact, Fact = BoardFacts)]
    public int MultiboardGetRowCount(multiboard? lb) => lb?.Rows ?? 0;

    [Native(Level.Exact, Fact = BoardFacts)]
    public void MultiboardSetColumnCount(multiboard? lb, int count)
    {
        if (lb is null) return;
        lb.Columns = Math.Max(count, 0);
        foreach (var key in lb.Cells.Keys.Where(cell => cell.Column >= lb.Columns).ToArray()) lb.Cells.Remove(key);
    }

    [Native(Level.Exact, Fact = BoardFacts)]
    public int MultiboardGetColumnCount(multiboard? lb) => lb?.Columns ?? 0;

    [Native(Level.Exact, Fact = BoardFacts)]
    public void MultiboardSetTitleText(multiboard? lb, string? label)
    {
        if (lb is not null) lb.Title = label ?? "";
    }

    [Native(Level.Exact, Fact = BoardFacts)]
    public string? MultiboardGetTitleText(multiboard? lb) => lb?.Title;

    /// <summary>The flag is the board's own: with two boards shown the read is true for both (facts, 11-07).</summary>
    [Native(Level.Exact, Fact = BoardFacts)]
    public void MultiboardDisplay(multiboard? lb, bool show)
    {
        if (lb is not null) lb.Displayed = show;
    }

    [Native(Level.Exact, Fact = BoardFacts)]
    public bool IsMultiboardDisplayed(multiboard? lb) => lb?.Displayed ?? false;

    [Native(Level.Close, Fact = BoardFacts)]
    public void MultiboardMinimize(multiboard? lb, bool minimize)
    {
        if (lb is not null) lb.Minimized = minimize;
    }

    [Native(Level.Close, Fact = BoardFacts)]
    public bool IsMultiboardMinimized(multiboard? lb) => lb?.Minimized ?? false;

    [Native(Level.Close, Fact = BoardFacts)]
    public void MultiboardClear(multiboard? lb)
    {
        if (lb is null) return;
        (lb.Rows, lb.Columns) = (0, 0);
        lb.Cells.Clear();
    }

    /// <summary>The cell at the row and column (from 0), nil outside the board; the same handle each call until released.</summary>
    [Native(Level.Close, Fact = BoardFacts)]
    public multiboarditem? MultiboardGetItem(multiboard? lb, int row, int column)
    {
        if (lb is null || row < 0 || column < 0 || row >= lb.Rows || column >= lb.Columns) return null;
        return lb.Cells.TryGetValue((row, column), out var cell) ? cell : lb.Cells[(row, column)] = Make(new multiboarditem());
    }

    [Native(Level.Recorded, Reason = "The cell handle is let go; the cell keeps what it shows")]
    public void MultiboardReleaseItem(multiboarditem? mbi) { }

    [Native(Level.Recorded, Reason = "Whether the boards show during the cinematic is drawn; no native reads it back")]
    public void MultiboardSuppressDisplay(bool flag) { }

    const string CellDrawn = "A multiboard cell's text, icon, colour and width are drawn; no native reads them back";

    [Native(Level.Recorded, Reason = CellDrawn)] public void MultiboardSetItemIcon(multiboarditem? mbi, string? iconFileName) { }
    [Native(Level.Recorded, Reason = CellDrawn)] public void MultiboardSetItemStyle(multiboarditem? mbi, bool showValue, bool showIcon) { }
    [Native(Level.Recorded, Reason = CellDrawn)] public void MultiboardSetItemValue(multiboarditem? mbi, string? val) { }
    [Native(Level.Recorded, Reason = CellDrawn)] public void MultiboardSetItemValueColor(multiboarditem? mbi, int red, int green, int blue, int alpha) { }
    [Native(Level.Recorded, Reason = CellDrawn)] public void MultiboardSetItemWidth(multiboarditem? mbi, float width) { }
    [Native(Level.Recorded, Reason = CellDrawn)] public void MultiboardSetItemsIcon(multiboard? lb, string? iconPath) { }
    [Native(Level.Recorded, Reason = CellDrawn)] public void MultiboardSetItemsStyle(multiboard? lb, bool showValues, bool showIcons) { }
    [Native(Level.Recorded, Reason = CellDrawn)] public void MultiboardSetItemsValue(multiboard? lb, string? value) { }
    [Native(Level.Recorded, Reason = CellDrawn)] public void MultiboardSetItemsValueColor(multiboard? lb, int red, int green, int blue, int alpha) { }
    [Native(Level.Recorded, Reason = CellDrawn)] public void MultiboardSetItemsWidth(multiboard? lb, float width) { }
    [Native(Level.Recorded, Reason = "The title's colour is drawn; no native reads it back")] public void MultiboardSetTitleTextColor(multiboard? lb, int red, int green, int blue, int alpha) { }

    [Native(Level.Close, Fact = "game-facts.md, UI: quests (11-07); defaults not measured")]
    public quest? CreateQuest() => Make(new quest());

    [Native(Level.Close)] public void DestroyQuest(quest? whichQuest) { if (whichQuest is not null) whichQuest.Destroyed = true; }
    [Native(Level.Close)] public void QuestSetTitle(quest? whichQuest, string? title) { if (whichQuest is not null) whichQuest.Title = title ?? ""; }
    [Native(Level.Close)] public void QuestSetDescription(quest? whichQuest, string? description) { if (whichQuest is not null) whichQuest.Description = description ?? ""; }
    [Native(Level.Close)] public void QuestSetIconPath(quest? whichQuest, string? iconPath) { if (whichQuest is not null) whichQuest.Icon = iconPath ?? ""; }
    [Native(Level.Close)] public void QuestSetRequired(quest? whichQuest, bool required) { if (whichQuest is not null) whichQuest.Required = required; }
    [Native(Level.Close)] public void QuestSetCompleted(quest? whichQuest, bool completed) { if (whichQuest is not null) whichQuest.Completed = completed; }
    [Native(Level.Close)] public void QuestSetFailed(quest? whichQuest, bool failed) { if (whichQuest is not null) whichQuest.Failed = failed; }
    [Native(Level.Close)] public void QuestSetDiscovered(quest? whichQuest, bool discovered) { if (whichQuest is not null) whichQuest.Discovered = discovered; }
    [Native(Level.Close)] public void QuestSetEnabled(quest? whichQuest, bool enabled) { if (whichQuest is not null) whichQuest.Enabled = enabled; }
    [Native(Level.Close)] public bool IsQuestRequired(quest? whichQuest) => whichQuest?.Required ?? false;
    [Native(Level.Close)] public bool IsQuestCompleted(quest? whichQuest) => whichQuest?.Completed ?? false;
    [Native(Level.Close)] public bool IsQuestFailed(quest? whichQuest) => whichQuest?.Failed ?? false;
    [Native(Level.Close)] public bool IsQuestDiscovered(quest? whichQuest) => whichQuest?.Discovered ?? false;
    [Native(Level.Close)] public bool IsQuestEnabled(quest? whichQuest) => whichQuest?.Enabled ?? false;
    [Native(Level.Close)] public questitem? QuestCreateItem(quest? whichQuest) => whichQuest is null ? null : Make(new questitem());
    [Native(Level.Close)] public void QuestItemSetDescription(questitem? whichQuestItem, string? description) { if (whichQuestItem is not null) whichQuestItem.Description = description ?? ""; }
    [Native(Level.Close)] public void QuestItemSetCompleted(questitem? whichQuestItem, bool completed) { if (whichQuestItem is not null) whichQuestItem.Completed = completed; }
    [Native(Level.Close)] public bool IsQuestItemCompleted(questitem? whichQuestItem) => whichQuestItem?.Completed ?? false;

    [Native(Level.Recorded, Reason = "The quest log's button flash is drawn; no native reads it back")] public void FlashQuestDialogButton() { }
    [Native(Level.Recorded, Reason = "The quest log is redrawn; no native reads it back")] public void ForceQuestDialogUpdate() { }
    [Native(Level.Recorded, Reason = "A defeat condition is listed in the quest log; no native reads it back")] public defeatcondition? CreateDefeatCondition() => Make(new defeatcondition());
    [Native(Level.Recorded, Reason = "A defeat condition is listed in the quest log; no native reads it back")] public void DestroyDefeatCondition(defeatcondition? whichCondition) { }
    [Native(Level.Recorded, Reason = "A defeat condition is listed in the quest log; no native reads it back")] public void DefeatConditionSetDescription(defeatcondition? whichCondition, string? description) { }

    [Native(Level.Close, Fact = BoardFacts)]
    public timerdialog? CreateTimerDialog(timer? t) => Make(new timerdialog());

    [Native(Level.Close)] public void DestroyTimerDialog(timerdialog? whichDialog) { if (whichDialog is null) return; (whichDialog.Destroyed, whichDialog.Displayed) = (true, false); }
    [Native(Level.Close)] public void TimerDialogDisplay(timerdialog? whichDialog, bool display) { if (whichDialog is not null) whichDialog.Displayed = display; }
    [Native(Level.Close)] public bool IsTimerDialogDisplayed(timerdialog? whichDialog) => whichDialog?.Displayed ?? false;
    [Native(Level.Recorded, Reason = "The timer window's title, time and colours are drawn; no native reads them back")] public void TimerDialogSetTitle(timerdialog? whichDialog, string? title) { }
    [Native(Level.Recorded, Reason = "The timer window's title, time and colours are drawn; no native reads them back")] public void TimerDialogSetTitleColor(timerdialog? whichDialog, int red, int green, int blue, int alpha) { }
    [Native(Level.Recorded, Reason = "The timer window's title, time and colours are drawn; no native reads them back")] public void TimerDialogSetTimeColor(timerdialog? whichDialog, int red, int green, int blue, int alpha) { }
    [Native(Level.Recorded, Reason = "The timer window's title, time and colours are drawn; no native reads them back")] public void TimerDialogSetSpeed(timerdialog? whichDialog, float speedMultFactor) { }
    [Native(Level.Recorded, Reason = "The timer window's title, time and colours are drawn; no native reads them back")] public void TimerDialogSetRealTimeRemaining(timerdialog? whichDialog, float timeRemaining) { }

    [Native(Level.Close, Fact = BoardFacts)]
    public dialog? DialogCreate() => Make(new dialog());

    // What each player is shown, one dialog at a time, and the registrations a click fires: a button's, then its dialog's (D795)
    readonly Dictionary<player, dialog> _shown = [];
    readonly List<(dialog? Dialog, button? Button, @event Event)> _dialogEvents = [];
    (dialog Dialog, button Button)? _clicked;

    [Native(Level.Close)] public void DialogDestroy(dialog? whichDialog) { whichDialog?.Buttons.Clear(); Hide(whichDialog); }

    void Hide(dialog? whichDialog)
    {
        foreach (var (shown, _) in _shown.Where(pair => pair.Value == whichDialog).ToList()) _shown.Remove(shown);
    }
    [Native(Level.Close)] public void DialogClear(dialog? whichDialog) => whichDialog?.Buttons.Clear();
    [Native(Level.Close)] public void DialogSetMessage(dialog? whichDialog, string? messageText) { if (whichDialog is not null) whichDialog.Message = messageText ?? ""; }

    [Native(Level.Close, Fact = BoardFacts)]
    public button? DialogAddButton(dialog? whichDialog, string? buttonText, int hotkey)
    {
        if (whichDialog is null) return null;
        var added = Make(new button { Text = buttonText ?? "", Hotkey = hotkey });
        whichDialog.Buttons.Add(added);
        return added;
    }

    [Native(Level.Close, Fact = BoardFacts)]
    public button? DialogAddQuitButton(dialog? whichDialog, bool doScoreScreen, string? buttonText, int hotkey)
    {
        var added = DialogAddButton(whichDialog, buttonText, hotkey);
        if (added is not null) (added.Quits, added.ScoreScreen) = (true, doScoreScreen);
        return added;
    }

    // Shown until a button is clicked or it is hidden. Close: a probe cannot click, so the click events and what they answer are the documented ones (D795)
    [Native(Level.Close)]
    public void DialogDisplay(player? whichPlayer, dialog? whichDialog, bool flag)
    {
        if (whichPlayer is null || whichDialog is null) return;
        if (flag) _shown[whichPlayer] = whichDialog;
        else if (_shown.GetValueOrDefault(whichPlayer) == whichDialog) _shown.Remove(whichPlayer);
    }

    [Native(Level.Close)]
    public @event? TriggerRegisterDialogEvent(trigger? whichTrigger, dialog? whichDialog)
    {
        if (whichTrigger is null || whichDialog is null) return null;
        var registered = Register(whichTrigger);
        _dialogEvents.Add((whichDialog, null, registered));
        return registered;
    }

    [Native(Level.Close)]
    public @event? TriggerRegisterDialogButtonEvent(trigger? whichTrigger, button? whichButton)
    {
        if (whichTrigger is null || whichButton is null) return null;
        var registered = Register(whichTrigger);
        _dialogEvents.Add((null, whichButton, registered));
        return registered;
    }

    [Native(Level.Close)] public button? GetClickedButton() => _clicked?.Button;
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
