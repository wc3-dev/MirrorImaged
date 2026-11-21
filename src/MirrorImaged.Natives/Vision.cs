namespace MirrorImaged.Natives;

public partial class unit
{
    internal readonly HashSet<player> SharesWith = [];
    internal float ExploredX = float.MinValue, ExploredY = float.MinValue;
    // Where the unit stood and how far it saw when its sight was last marked explored for a player, and how many cells had been taken back by then
    internal readonly Dictionary<player, (float X, float Y, float Sight, long Unexplored)> Marked = [];
}

public partial class fogmodifier
{
    internal player Player = null!;
    internal int State;
    internal float X, Y, Radius, MinX, MinY, MaxX, MaxY;
    internal bool Round, Active, Destroyed;
    internal long Started;
}

// What a player sees: a point is visible within the sight of a unit the player owns or sees through, a fog modifier's state while it
// runs (the later started wins), else fogged where it was seen before and masked where it never was (game-facts, "Presentation":
// probes of 10-09 and 11-06). Fog and the mask turn off as FogEnable and FogMaskEnable say. Selection is a set of units for each
// player. Close: invisibility and detection need abilities (wave 4), and the game's slower recompute of overlapping modifiers
// isn't modelled.
public sealed partial class GameNatives
{
    const string VisionFacts = "game-facts.md, Presentation: fog modifiers and the fog flags (10-09, 11-06)";
    const int FogMasked = 1, FogFogged = 2, FogVisible = 4, FogCell = 64;

    readonly Dictionary<player, HashSet<(int, int)>> _explored = [];
    readonly List<fogmodifier> _modifiers = [];
    readonly Dictionary<player, HashSet<unit>> _selected = [];
    long _modifierOrder, _unexplored;

    HashSet<(int, int)> Explored(player p) => _explored.TryGetValue(p, out var set) ? set : _explored[p] = [];

    // The sight of a unit now: its sight field by the time of day, none for a dead, hidden or removed one
    float SightOf(unit u) => u.Dead || u.Hidden || u.Removed ? 0 : BlzGetUnitRealField(u, Convert<unitrealfield>(Sight));

    // Whether the unit's sight counts for the player: its own, or one the unit's owner shares, or one the unit itself shares
    bool SeenThrough(unit u, player p) => u.Owner == p || u.SharesWith.Contains(p) || GetPlayerAlliance(u.Owner, p, Convert<alliancetype>(5));

    // What a unit sees for the players that see through it becomes explored once it has moved a cell from where it last did
    void ExploreUnit(unit u, bool made)
    {
        if (!made && Distance2(u.X - u.ExploredX, u.Y - u.ExploredY) < FogCell * FogCell) return;
        (u.ExploredX, u.ExploredY) = (u.X, u.Y);
        for (var n = 0; n < 28; n++)
            if (Player(n) is { } p && SeenThrough(u, p)) Explore(p, u);
    }

    // What the player's units see now becomes explored (called by every read)
    void Explore(player p)
    {
        foreach (var u in _units.Values) Explore(p, u);
    }

    // What the one unit sees, if it is seen through for the player, becomes explored
    void Explore(player p, unit u)
    {
        if (!SeenThrough(u, p) || SightOf(u) is not (var sight and > 0)) return;
        // Marking the same circle again changes nothing unless cells were taken back since: a map that asks what a player sees thousands of
        // times a second would otherwise mark every unit's circle each time
        var mark = (u.X, u.Y, sight, _unexplored);
        if (u.Marked.TryGetValue(p, out var last) && last == mark) return;
        u.Marked[p] = mark;
        var seen = Explored(p);
        for (var cy = (int)MathF.Floor((u.Y - sight) / FogCell); cy <= (int)MathF.Floor((u.Y + sight) / FogCell); cy++)
            for (var cx = (int)MathF.Floor((u.X - sight) / FogCell); cx <= (int)MathF.Floor((u.X + sight) / FogCell); cx++)
                if (Distance2((cx + 0.5f) * FogCell - u.X, (cy + 0.5f) * FogCell - u.Y) <= sight * sight) seen.Add((cx, cy));
    }

    static bool Covers(fogmodifier m, float x, float y) =>
        m.Round ? Distance2(m.X - x, m.Y - y) <= m.Radius * m.Radius : x >= m.MinX && x <= m.MaxX && y >= m.MinY && y <= m.MaxY;

    // The player's state at the place: 4 visible, 2 fogged, 1 masked
    int FogAt(float x, float y, player? p)
    {
        if (p is null) return FogMasked;
        if (!_fog && !_fogMask) return FogVisible;
        Explore(p);
        var modifier = _modifiers.Where(m => m.Active && !m.Destroyed && m.Player == p && Covers(m, x, y)).OrderByDescending(m => m.Started).FirstOrDefault();
        var state = modifier?.State
            ?? (_units.Values.Any(u => SeenThrough(u, p) && SightOf(u) is var sight and > 0 && Distance2(u.X - x, u.Y - y) <= sight * sight) ? FogVisible
                : Explored(p).Contains(((int)MathF.Floor(x / FogCell), (int)MathF.Floor(y / FogCell))) ? FogFogged : FogMasked);
        // The fog off shows what was seen as seen, the mask off shows what never was as fogged
        if (!_fog && state == FogFogged) state = FogVisible;
        if (!_fogMask && state == FogMasked) state = _fog ? FogFogged : FogVisible;
        return state;
    }

    /// <summary>Whether the point is visible to the player; false for a nil player, and true everywhere with fog and mask both off.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: fog modifiers and the fog flags (10-09, 11-06)); the game's slower recompute of overlapping fog modifiers is not modelled.</remarks>
    [Native(Level.Close, Fact = VisionFacts)] public bool IsVisibleToPlayer(float x, float y, player? whichPlayer) => FogAt(x, y, whichPlayer) == FogVisible;
    /// <summary>Whether the point is fogged for the player: seen before and not now; false for a nil player.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: fog modifiers and the fog flags (10-09, 11-06)); the game's slower recompute of overlapping fog modifiers is not modelled.</remarks>
    [Native(Level.Close, Fact = VisionFacts)] public bool IsFoggedToPlayer(float x, float y, player? whichPlayer) => FogAt(x, y, whichPlayer) == FogFogged;
    /// <summary>Whether the point is masked for the player: never seen; true for a nil player.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: fog modifiers and the fog flags (10-09, 11-06)); the game's slower recompute of overlapping fog modifiers is not modelled.</remarks>
    [Native(Level.Close, Fact = VisionFacts)] public bool IsMaskedToPlayer(float x, float y, player? whichPlayer) => FogAt(x, y, whichPlayer) == FogMasked;
    /// <summary>Whether the location's point is visible to the player; false for a nil location.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: fog modifiers and the fog flags (10-09, 11-06)); a nil location gives false; the game's slower recompute of overlapping fog modifiers is not modelled.</remarks>
    [Native(Level.Close, Fact = VisionFacts)] public bool IsLocationVisibleToPlayer(location? whichLocation, player? whichPlayer) => whichLocation is not null && IsVisibleToPlayer(whichLocation.X, whichLocation.Y, whichPlayer);
    /// <summary>Whether the location's point is fogged for the player; false for a nil location.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: fog modifiers and the fog flags (10-09, 11-06)); a nil location gives false; the game's slower recompute of overlapping fog modifiers is not modelled.</remarks>
    [Native(Level.Close, Fact = VisionFacts)] public bool IsLocationFoggedToPlayer(location? whichLocation, player? whichPlayer) => whichLocation is not null && IsFoggedToPlayer(whichLocation.X, whichLocation.Y, whichPlayer);
    /// <summary>Whether the location's point is masked for the player; false for a nil location.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: fog modifiers and the fog flags (10-09, 11-06)); a nil location gives false; the game's slower recompute of overlapping fog modifiers is not modelled.</remarks>
    [Native(Level.Close, Fact = VisionFacts)] public bool IsLocationMaskedToPlayer(location? whichLocation, player? whichPlayer) => whichLocation is not null && IsMaskedToPlayer(whichLocation.X, whichLocation.Y, whichPlayer);

    /// <summary>A unit is visible where its place is; its own player always sees it (a hidden or removed unit is never visible).</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: fog modifiers and the fog flags (10-09, 11-06)); detection, and invisibility from abilities other than Wind Walk's, are not modelled.</remarks>
    [Native(Level.Close, Fact = VisionFacts)]
    public bool IsUnitVisible(unit? whichUnit, player? whichPlayer) =>
        whichUnit is { Hidden: false, Removed: false } u && whichPlayer is not null && (u.Owner == whichPlayer || FogAt(u.X, u.Y, whichPlayer) == FogVisible && !IsUnitInvisible(u, whichPlayer));

    /// <summary>Whether the unit's place is fogged for the player; false for nil.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: fog modifiers and the fog flags (10-09, 11-06)); the game's slower recompute of overlapping fog modifiers is not modelled.</remarks>
    [Native(Level.Close, Fact = VisionFacts)]
    public bool IsUnitFogged(unit? whichUnit, player? whichPlayer) => whichUnit is not null && whichPlayer is not null && FogAt(whichUnit.X, whichUnit.Y, whichPlayer) == FogFogged;

    /// <summary>Whether the unit's place is masked for the player; false for nil.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: fog modifiers and the fog flags (10-09, 11-06)); the game's slower recompute of overlapping fog modifiers is not modelled.</remarks>
    [Native(Level.Close, Fact = VisionFacts)]
    public bool IsUnitMasked(unit? whichUnit, player? whichPlayer) => whichUnit is not null && whichPlayer is not null && FogAt(whichUnit.X, whichUnit.Y, whichPlayer) == FogMasked;

    /// <summary>
    /// Whether a buff hides the unit from the player: Wind Walk's, from a foe (probe abilities-spells). Close: detection, and the other
    /// abilities that hide a unit, are not here.
    /// </summary>
    /// <remarks>Close: Only a hiding buff counts, and only against a foe; detection and other ways to hide are not modelled.</remarks>
    [Native(Level.Close)]
    public bool IsUnitInvisible(unit? whichUnit, player? whichPlayer) =>
        whichUnit is not null && whichPlayer is not null && whichUnit.Owner != whichPlayer && !IsPlayerAlly(whichUnit.Owner, whichPlayer) && whichUnit.Buffs.Any(b => b.Invisible);
    /// <summary>Whether the player sees the unit, as <c>IsUnitVisible</c> says.</summary>
    /// <remarks>Close: Taken as visible, as detection is not modelled; not measured in the game.</remarks>
    [Native(Level.Close)] public bool IsUnitDetected(unit? whichUnit, player? whichPlayer) => IsUnitVisible(whichUnit, whichPlayer);
    const string NoDetectedEvent = "No detected event fires here, as none did in the game (probe units-detected: an invisible footman came within a detector's range and left it, 6 s, no event), so every read is outside one: nil";

    /// <summary>Nil: no detected event fires here.</summary>
    /// <remarks>Close: No detected event fires here, as none did in the game (probe units-detected: an invisible footman came within a detector's range and left it, 6 s, no event), so every read is outside one: nil.</remarks>
    [Native(Level.Close, Fact = "probe units-detected (11-21, D791)", Reason = NoDetectedEvent)] public unit? GetDetectedUnit() => null;
    /// <summary>Nil: no detected event fires here.</summary>
    /// <remarks>Close: No detected event fires here, as none did in the game (probe units-detected: an invisible footman came within a detector's range and left it, 6 s, no event), so every read is outside one: nil.</remarks>
    [Native(Level.Close, Fact = "probe units-detected (11-21, D791)", Reason = NoDetectedEvent)] public player? GetEventDetectingPlayer() => null;

    /// <summary>Whether the unit's owner is in the force; false for a nil unit.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: fog modifiers and the fog flags (10-09, 11-06)); answered through the owner's place in the force; a nil force is as <c>IsPlayerInForce</c> answers, not measured.</remarks>
    [Native(Level.Close, Fact = VisionFacts)]
    public bool IsUnitInForce(unit? whichUnit, force? whichForce) => whichUnit is not null && IsPlayerInForce(whichUnit.Owner, whichForce);

    /// <summary>Lets the player see through the unit's sight (or stops it).</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: fog modifiers and the fog flags (10-09, 11-06)); kept for each unit and player; not measured beyond what the fog probes show.</remarks>
    [Native(Level.Close, Fact = VisionFacts)]
    public void UnitShareVision(unit? whichUnit, player? whichPlayer, bool share)
    {
        if (whichUnit is null || whichPlayer is null) return;
        if (share) whichUnit.SharesWith.Add(whichPlayer);
        else whichUnit.SharesWith.Remove(whichPlayer);
    }

    // ---- Fog states and modifiers

    void FogOnce(player? p, fogstate? state, Func<float, float, bool> covers, float x0, float y0, float x1, float y1)
    {
        if (p is null || state is null) return;
        var seen = Explored(p);
        for (var cy = (int)MathF.Floor(y0 / FogCell); cy <= (int)MathF.Floor(y1 / FogCell); cy++)
            for (var cx = (int)MathF.Floor(x0 / FogCell); cx <= (int)MathF.Floor(x1 / FogCell); cx++)
                if (covers((cx + 0.5f) * FogCell, (cy + 0.5f) * FogCell))
                {
                    if (state.Id != FogMasked) seen.Add((cx, cy));
                    else if (seen.Remove((cx, cy))) _unexplored++;
                }
    }

    /// <summary>One instant of the state: visible and fogged mark the ground explored, masked takes it away (a stopped modifier leaves its area explored: facts, 10-09).</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: fog modifiers and the fog flags (10-09, 11-06)); the shared-vision flag is ignored, and the circle is taken by the centres of 64-unit cells; the game's own cells are not measured.</remarks>
    [Native(Level.Close, Fact = VisionFacts)]
    public void SetFogStateRadius(player? forWhichPlayer, fogstate? whichState, float centerx, float centerY, float radius, bool useSharedVision) =>
        FogOnce(forWhichPlayer, whichState, (x, y) => Distance2(x - centerx, y - centerY) <= radius * radius, centerx - radius, centerY - radius, centerx + radius, centerY + radius);

    /// <summary>As <c>SetFogStateRadius</c>, at the location's point; nothing for nil.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: fog modifiers and the fog flags (10-09, 11-06)); the shared-vision flag is ignored, as in <c>SetFogStateRadius</c>.</remarks>
    [Native(Level.Close, Fact = VisionFacts)]
    public void SetFogStateRadiusLoc(player? forWhichPlayer, fogstate? whichState, location? center, float radius, bool useSharedVision)
    {
        if (center is not null) SetFogStateRadius(forWhichPlayer, whichState, center.X, center.Y, radius, useSharedVision);
    }

    /// <summary>As <c>SetFogStateRadius</c>, for the rect; nothing for nil.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: fog modifiers and the fog flags (10-09, 11-06)); the rect is taken by the centres of 64-unit cells; the game's own cells are not measured.</remarks>
    [Native(Level.Close, Fact = VisionFacts)]
    public void SetFogStateRect(player? forWhichPlayer, fogstate? whichState, rect? where, bool useSharedVision)
    {
        if (where is not null) FogOnce(forWhichPlayer, whichState, (x, y) => x >= where.MinX && x <= where.MaxX && y >= where.MinY && y <= where.MaxY, where.MinX, where.MinY, where.MaxX, where.MaxY);
    }

    /// <summary>Makes a stopped round fog modifier for the player; nil for a nil player or state.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: fog modifiers and the fog flags (10-09, 11-06)); the shared-vision and after-units flags are ignored, and the area is taken as <c>SetFogStateRadius</c> takes it.</remarks>
    [Native(Level.Close, Fact = VisionFacts)]
    public fogmodifier? CreateFogModifierRadius(player? forWhichPlayer, fogstate? whichState, float centerx, float centerY, float radius, bool useSharedVision, bool afterUnits) =>
        forWhichPlayer is null || whichState is null ? null : Made(new fogmodifier { Player = forWhichPlayer, State = whichState.Id, X = centerx, Y = centerY, Radius = radius, Round = true });

    /// <summary>As <c>CreateFogModifierRadius</c>, at the location's point; nil for a nil location.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: fog modifiers and the fog flags (10-09, 11-06)); the shared-vision and after-units flags are ignored, as in <c>CreateFogModifierRadius</c>.</remarks>
    [Native(Level.Close, Fact = VisionFacts)]
    public fogmodifier? CreateFogModifierRadiusLoc(player? forWhichPlayer, fogstate? whichState, location? center, float radius, bool useSharedVision, bool afterUnits) =>
        center is null ? null : CreateFogModifierRadius(forWhichPlayer, whichState, center.X, center.Y, radius, useSharedVision, afterUnits);

    /// <summary>Makes a stopped square fog modifier for the rect; nil for a nil player, state or rect.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: fog modifiers and the fog flags (10-09, 11-06)); the shared-vision and after-units flags are ignored, and the area is taken by the centres of 64-unit cells.</remarks>
    [Native(Level.Close, Fact = VisionFacts)]
    public fogmodifier? CreateFogModifierRect(player? forWhichPlayer, fogstate? whichState, rect? where, bool useSharedVision, bool afterUnits) =>
        forWhichPlayer is null || whichState is null || where is null
            ? null
            : Made(new fogmodifier { Player = forWhichPlayer, State = whichState.Id, MinX = where.MinX, MinY = where.MinY, MaxX = where.MaxX, MaxY = where.MaxY });

    fogmodifier Made(fogmodifier modifier)
    {
        _modifiers.Add(modifier);
        return Make(modifier);
    }

    /// <summary>Starts it: the later started of two on one place wins at once (facts, 11-06).</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: fog modifiers and the fog flags (10-09, 11-06)); the game's slower recompute of overlapping fog modifiers is not modelled.</remarks>
    [Native(Level.Close, Fact = VisionFacts)]
    public void FogModifierStart(fogmodifier? whichFogModifier)
    {
        if (whichFogModifier is not { Destroyed: false } m) return;
        (m.Active, m.Started) = (true, ++_modifierOrder);
    }

    /// <summary>Stops it, leaving what it showed explored and out of sight (facts, 10-09).</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: fog modifiers and the fog flags (10-09, 11-06)); what it leaves explored is as measured for a stop; the game's recompute of overlapping modifiers is not modelled.</remarks>
    [Native(Level.Close, Fact = VisionFacts)]
    public void FogModifierStop(fogmodifier? whichFogModifier)
    {
        if (whichFogModifier is not { Active: true } m) return;
        m.Active = false;
        if (m.State == FogMasked) return;
        var seen = Explored(m.Player);
        var (x0, y0, x1, y1) = m.Round ? (m.X - m.Radius, m.Y - m.Radius, m.X + m.Radius, m.Y + m.Radius) : (m.MinX, m.MinY, m.MaxX, m.MaxY);
        for (var cy = (int)MathF.Floor(y0 / FogCell); cy <= (int)MathF.Floor(y1 / FogCell); cy++)
            for (var cx = (int)MathF.Floor(x0 / FogCell); cx <= (int)MathF.Floor(x1 / FogCell); cx++)
                if (Covers(m, (cx + 0.5f) * FogCell, (cy + 0.5f) * FogCell)) seen.Add((cx, cy));
    }

    /// <summary>Stops the fog modifier, as <c>FogModifierStop</c> does, and removes it for good; nothing for nil.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Presentation: fog modifiers and the fog flags (10-09, 11-06)); what it leaves explored is taken from stopping it, not measured on its own.</remarks>
    [Native(Level.Close, Fact = VisionFacts)]
    public void DestroyFogModifier(fogmodifier? whichFogModifier)
    {
        if (whichFogModifier is null) return;
        FogModifierStop(whichFogModifier);
        whichFogModifier.Destroyed = true;
        _modifiers.Remove(whichFogModifier);
    }

    // ---- Selection: SelectUnit and ClearSelection act on the local player's

    HashSet<unit> Selected(player p) => _selected.TryGetValue(p, out var set) ? set : _selected[p] = [];

    /// <summary>Adds the unit to the local player's selection or takes it off; a dead or removed unit is never added, and with no local player nothing happens.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Units: selection (10-21)); only the local player's selection is changed, and no limit on its size is applied, not measured.</remarks>
    [Native(Level.Close, Fact = "game-facts.md, Units: selection (10-21)")]
    public void SelectUnit(unit? whichUnit, bool flag)
    {
        if (whichUnit is null || _local is null) return;
        if (flag && !whichUnit.Dead && !whichUnit.Removed) Selected(_local).Add(whichUnit);
        else Selected(_local).Remove(whichUnit);
    }

    /// <summary>Empties the local player's selection.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Units: selection (10-21)); only the local player's selection is cleared; the others' are not touched.</remarks>
    [Native(Level.Close, Fact = "game-facts.md, Units: selection (10-21)")]
    public void ClearSelection()
    {
        if (_local is not null) Selected(_local).Clear();
    }

    /// <summary>Whether the player has the unit selected; a dead or removed unit never is.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Units: selection (10-21)); a hidden unit's, or a unit given to another player's, selection is not modelled.</remarks>
    [Native(Level.Close, Fact = "game-facts.md, Units: selection (10-21)")]
    public bool IsUnitSelected(unit? whichUnit, player? whichPlayer) =>
        whichUnit is { Dead: false, Removed: false } && whichPlayer is not null && Selected(whichPlayer).Contains(whichUnit);

    /// <summary>Fills the group with the player's selected units that pass the filter, in id order.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Units: selection (10-21)); the order of the group is the ids', not measured against the game's.</remarks>
    [Native(Level.Close, Fact = "game-facts.md, Units: selection (10-21)")]
    public void GroupEnumUnitsSelected(group? whichGroup, player? whichPlayer, boolexpr? filter)
    {
        if (whichPlayer is not null) Enumerate(whichGroup, Selected(whichPlayer).Where(u => !u.Dead && !u.Removed).OrderBy(u => u.Id).ToArray(), filter);
    }

    /// <summary>Changes nothing; the selections here are kept for each player.</summary>
    /// <remarks>Recorded: Sends the local player's selection to the others; the selections are kept for each player here.</remarks>
    [Native(Level.Recorded, Reason = "Sends the local player's selection to the others; the selections are kept for each player here")]
    public void SyncSelections() { }

    /// <summary>Nil: no mouse is over a unit in a test run.</summary>
    /// <remarks>Close: A test run has no mouse over a unit: the game answered nil with the pointer nowhere on one; a unit under a scripted mouse isn't read.</remarks>
    [Native(Level.Close, Fact = TestRunProbe, Reason = "A test run has no mouse over a unit: the game answered nil with the pointer nowhere on one; a unit under a scripted mouse isn't read")]
    public unit? BlzGetMouseFocusUnit() => null;

    /// <summary>Registers; fires when units walk into range, which comes with movement (wave 3).</summary>
    /// <remarks>Close: The range test is a model, not measured against the game's; range events come with movement (wave 3).</remarks>
    [Native(Level.Close)]
    public @event? TriggerRegisterUnitInRange(trigger? whichTrigger, unit? whichUnit, float range, boolexpr? filter) =>
        whichTrigger is null || whichUnit is null ? null : Register(whichTrigger);
}
