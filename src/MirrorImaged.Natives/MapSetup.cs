namespace MirrorImaged.Natives;

// What config() tells the lobby, kept for the getters. What only the lobby reads (the map's name and description, the
// enemy start priorities) is Recorded: no native reads it back (decisions D98).
public sealed partial class GameNatives
{
    int _playerCount, _teamCount = 24, _placement = 2, _mapFlags; // MAP_PLACEMENT_USE_MAP_SETTINGS
    readonly Dictionary<int, (float X, float Y)> _startLocations = [];
    readonly Dictionary<(int Location, int Slot), (int Other, int Priority)> _startPriorities = [];

    /// <summary>Changes nothing; the name isn't kept.</summary>
    /// <remarks>Recorded: Names the map in the lobby and on the score screen; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "Names the map in the lobby and on the score screen; no native reads it back")]
    public void SetMapName(string? name) { }

    /// <summary>Changes nothing; the description isn't kept.</summary>
    /// <remarks>Recorded: Describes the map in the lobby; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "Describes the map in the lobby; no native reads it back")]
    public void SetMapDescription(string? description) { }

    /// <summary>Keeps the player count, for <c>GetPlayers</c>.</summary>
    /// <remarks>Close: Kept as set, whatever the value; the game's limit on the count is not measured.</remarks>
    [Native(Level.Close)]
    public void SetPlayers(int playercount) => _playerCount = playercount;

    /// <summary>The count <c>SetPlayers</c> kept; 0 until then.</summary>
    /// <remarks>Close: Kept as set; the 0 before <c>SetPlayers</c> is a default, not measured.</remarks>
    [Native(Level.Close)]
    public int GetPlayers() => _playerCount;

    /// <summary>Keeps the team count, for <c>GetTeams</c>.</summary>
    /// <remarks>Close: Kept as set, whatever the value; the game's limit on the count is not measured.</remarks>
    [Native(Level.Close)]
    public void SetTeams(int teamcount) => _teamCount = teamcount;

    /// <summary>The count <c>SetTeams</c> kept; 24 until then.</summary>
    /// <remarks>Close: Kept as set; the 24 before <c>SetTeams</c> is a default, not measured.</remarks>
    [Native(Level.Close)]
    public int GetTeams() => _teamCount;

    /// <summary>Keeps the placement type, for <c>GetGamePlacement</c>; nil changes nothing.</summary>
    /// <remarks>Close: Kept as set; what the lobby does with it is not simulated.</remarks>
    [Native(Level.Close)]
    public void SetGamePlacement(placement? whichPlacementType)
    {
        if (whichPlacementType is not null) _placement = whichPlacementType.Id;
    }

    /// <summary>The placement set; the map's settings until then.</summary>
    /// <remarks>Close: The map-settings placement before <c>SetGamePlacement</c> is a default, not measured.</remarks>
    [Native(Level.Close)]
    public placement? GetGamePlacement() => Convert<placement>(_placement);

    /// <summary>
    /// Melee, as a <c>-loadfile</c> game is, from <c>config</c> on, with custom forces or without (the players and lobby
    /// probes; custom forces without fixed settings not measured).
    /// </summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Players and the game: the players and lobby probes).</remarks>
    [Native(Level.Exact, Fact = "facts, Players and the game: the players and lobby probes")]
    public gametype? GetGameTypeSelected() => Convert<gametype>(1); // GAME_TYPE_MELEE

    /// <summary>Keeps the start location's point, for the two getters.</summary>
    /// <remarks>Close: Kept as set, with no check against the map's bounds, not measured.</remarks>
    [Native(Level.Close)]
    public void DefineStartLocation(int whichStartLoc, float x, float y) => _startLocations[whichStartLoc] = (x, y);

    /// <summary>The start location's x; 0 for one not defined (the players probe).</summary>
    /// <remarks>Close: Kept from <c>DefineStartLocation</c> only; other sources of start locations are not measured.</remarks>
    [Native(Level.Close)]
    public float GetStartLocationX(int whichStartLocation) => _startLocations.GetValueOrDefault(whichStartLocation).X;

    /// <summary>The start location's y; 0 for one not defined.</summary>
    /// <remarks>Close: Kept from <c>DefineStartLocation</c> only; other sources of start locations are not measured.</remarks>
    [Native(Level.Close)]
    public float GetStartLocationY(int whichStartLocation) => _startLocations.GetValueOrDefault(whichStartLocation).Y;

    // Only the slots inside the count are kept (probe players-start-priorities, D794): a slot made by a bigger count reads -1 as its other location, one past
    // the count 0, and a smaller count drops the slots past it; a set outside the count changes nothing
    readonly Dictionary<int, int> _startPriorityCounts = [];

    /// <summary>Sets how many priority slots the start location has: new slots read -1 as the other location and low as the priority, and slots past a smaller count are dropped.</summary>
    /// <remarks>Exact: matches the game wherever measured (probe players-start-priorities (11-21, D794)).</remarks>
    [Native(Level.Exact, Fact = "probe players-start-priorities (11-21, D794)")]
    public void SetStartLocPrioCount(int whichStartLoc, int prioSlotCount)
    {
        var (was, now) = (_startPriorityCounts.GetValueOrDefault(whichStartLoc), Math.Max(prioSlotCount, 0));
        for (var slot = now; slot < was; slot++) _startPriorities.Remove((whichStartLoc, slot));
        for (var slot = was; slot < now; slot++) _startPriorities[(whichStartLoc, slot)] = (-1, 0);
        _startPriorityCounts[whichStartLoc] = now;
    }

    /// <summary>Sets the other location and the priority of a slot inside the count; a slot outside it changes nothing.</summary>
    /// <remarks>Exact: matches the game wherever measured (probe players-start-priorities (11-21, D794)).</remarks>
    [Native(Level.Exact, Fact = "probe players-start-priorities (11-21, D794)")]
    public void SetStartLocPrio(int whichStartLoc, int prioSlotIndex, int otherStartLocIndex, startlocprio? priority)
    {
        if (prioSlotIndex >= 0 && prioSlotIndex < _startPriorityCounts.GetValueOrDefault(whichStartLoc)) _startPriorities[(whichStartLoc, prioSlotIndex)] = (otherStartLocIndex, priority?.Id ?? 0);
    }

    // The other start location of the priority; -1 for a slot inside the count not set and 0 past it (probe players-start-priorities)
    /// <summary>The other start location of the priority slot; -1 for a slot not set, 0 past the count.</summary>
    /// <remarks>Exact: matches the game wherever measured (probe players-start-priorities (11-21, D794)).</remarks>
    [Native(Level.Exact, Fact = "probe players-start-priorities (11-21, D794)")]
    public int GetStartLocPrioSlot(int whichStartLoc, int prioSlotIndex) => _startPriorities.GetValueOrDefault((whichStartLoc, prioSlotIndex)).Other;

    // The priority set; MAP_LOC_PRIO_LOW for one not set
    /// <summary>The priority of the slot; low for one not set.</summary>
    /// <remarks>Exact: matches the game wherever measured (probe players-start-priorities (11-21, D794)).</remarks>
    [Native(Level.Exact, Fact = "probe players-start-priorities (11-21, D794)")]
    public startlocprio? GetStartLocPrio(int whichStartLoc, int prioSlotIndex) =>
        Convert<startlocprio>(_startPriorities.GetValueOrDefault((whichStartLoc, prioSlotIndex)).Priority);

    /// <summary>Changes nothing; only the lobby reads it.</summary>
    /// <remarks>Recorded: Only the lobby's placement of players reads it; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "Only the lobby's placement of players reads it; no native reads it back")]
    public void SetEnemyStartLocPrioCount(int whichStartLoc, int prioSlotCount) { }

    /// <summary>Changes nothing; only the lobby reads it.</summary>
    /// <remarks>Recorded: Only the lobby's placement of players reads it; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "Only the lobby's placement of players reads it; no native reads it back")]
    public void SetEnemyStartLocPrio(int whichStartLoc, int prioSlotIndex, int otherStartLocIndex, startlocprio? priority) { }

    /// <summary>Close: only MAP_LOCK_ALLIANCE_CHANGES set false is measured, and it reads back false (facts).</summary>
    /// <remarks>Close: Kept as set for every flag; what the game does with the others is not measured.</remarks>
    [Native(Level.Close)]
    public void SetMapFlag(mapflag? whichMapFlag, bool value)
    {
        if (whichMapFlag is not null) _mapFlags = value ? _mapFlags | whichMapFlag.Id : _mapFlags & ~whichMapFlag.Id;
    }

    /// <summary>
    /// A flag set, or the lobby's, once it has seated the players after <c>config</c>: none, but
    /// MAP_LOCK_ALLIANCE_CHANGES and 2^20 for custom forces with fixed player settings (the lobby probe).
    /// </summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Version and launching: custom forces and fixed player settings start with MAP_LOCK_ALLIANCE_CHANGES set (11-06); facts, Players and the game: no flag without them).</remarks>
    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: custom forces and fixed player settings start with MAP_LOCK_ALLIANCE_CHANGES set (11-06); facts, Players and the game: no flag without them")]
    public bool IsMapFlagSet(mapflag? whichMapFlag) => whichMapFlag is not null && (_mapFlags & whichMapFlag.Id) != 0;
}
