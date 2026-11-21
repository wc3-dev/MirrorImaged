namespace MirrorImaged.Natives;

// What a person at a player does, given by a test or a recording: keys, the mouse, a selection, and leaving the game (wave 6). Each fires the
// events a map registered for it and sets what the map can read back. Close: the events and what they answer are the documented ones; their
// order against other events of one step is the order the input was given in (decisions D367).
public sealed partial class GameNatives
{
    const int PlayerLeave = 15, MouseDown = 305, MouseUp = 306, MouseMove = 307, UnitSelectedKind = 57, UnitDeselectedKind = 58, PlayerUnitSelectedKind = 24, PlayerUnitDeselectedKind = 25;

    readonly List<(player Player, oskeytype Key, int Meta, bool Down, @event Event)> _keyEvents = [];
    readonly HashSet<(player Player, int Key)> _keysDown = [];
    readonly HashSet<(player Player, int Button)> _buttonsDown = [];
    (oskeytype Key, int Meta, bool Down)? _keyNow;
    (int Button, float X, float Y)? _mouseNow;
    (float X, float Y) _mouseAt;

    /// <summary>Registers the trigger for the player pressing or releasing the key with the meta keys held; nil for a null trigger, player or key.</summary>
    /// <remarks>Close: the event and its answers are the documented ones, not measured in the game; it fires in the order the input was given (D367).</remarks>
    [Native(Level.Close)]
    public @event? BlzTriggerRegisterPlayerKeyEvent(trigger? whichTrigger, player? whichPlayer, oskeytype? key, int metaKey, bool keyDown)
    {
        if (whichTrigger is null || whichPlayer is null || key is null) return null;
        var registered = Register(whichTrigger);
        _keyEvents.Add((whichPlayer, key, metaKey, keyDown, registered));
        return registered;
    }

    /// <summary>True when the key event now firing is a press; false outside one.</summary>
    /// <remarks>Close: the documented answer, not measured in the game; false outside a key event is the model's.</remarks>
    [Native(Level.Close)] public bool BlzGetTriggerPlayerIsKeyDown() => _keyNow?.Down ?? false;
    /// <summary>The key of the key event now firing; nil outside one.</summary>
    /// <remarks>Close: the documented answer, not measured in the game; nil outside a key event is the model's.</remarks>
    [Native(Level.Close)] public oskeytype? BlzGetTriggerPlayerKey() => _keyNow?.Key;
    /// <summary>The meta keys held in the key event now firing, as given; 0 outside one.</summary>
    /// <remarks>Close: the documented answer, not measured in the game; the bits' own meaning is not measured.</remarks>
    [Native(Level.Close)] public int BlzGetTriggerPlayerMetaKey() => _keyNow?.Meta ?? 0;
    /// <summary>True while the local player holds the key, from the presses and releases given; false for a null key or no local player.</summary>
    /// <remarks>Close: the documented answer, read from the input given and not measured against a real keyboard.</remarks>
    [Native(Level.Close)] public bool BlzIsKeyPressed(oskeytype? key) => key is not null && _local is not null && _keysDown.Contains((_local, key.Id));
    /// <summary>False for every meta key: no key is held in a test run.</summary>
    /// <remarks>Close: No key is held in a test run: the game answered false for -1 to 9. A scripted press of a modifier key isn't read as a meta key here, as the bits' own meaning isn't measured.</remarks>
    [Native(Level.Close, Fact = TestRunProbe, Reason = "No key is held in a test run: the game answered false for -1 to 9. A scripted press of a modifier key isn't read as a meta key here, as the bits' own meaning isn't measured")]
    public bool BlzIsMetaKeyPressed(int metakey) => false;
    /// <summary>True while the local player holds the mouse button, from the presses and releases given; false for a null button or no local player.</summary>
    /// <remarks>Close: the documented answer, read from the input given and not measured against a real mouse.</remarks>
    [Native(Level.Close)] public bool BlzIsMouseButtonPressed(mousebuttontype? mouseButtonType) => mouseButtonType is not null && _local is not null && _buttonsDown.Contains((_local, mouseButtonType.Id));
    /// <summary>The button of the mouse event now firing; nil outside one.</summary>
    /// <remarks>Close: the documented answer, not measured in the game; nil outside a mouse event is the model's.</remarks>
    [Native(Level.Close)] public mousebuttontype? BlzGetTriggerPlayerMouseButton() => _mouseNow is { } now ? Convert<mousebuttontype>(now.Button) : null;
    /// <summary>The x in the world of the mouse event now firing; 0 outside one.</summary>
    /// <remarks>Close: the documented answer, not measured in the game; 0 outside a mouse event is the model's.</remarks>
    [Native(Level.Close)] public float BlzGetTriggerPlayerMouseX() => _mouseNow?.X ?? 0;
    /// <summary>The y in the world of the mouse event now firing; 0 outside one.</summary>
    /// <remarks>Close: the documented answer, not measured in the game; 0 outside a mouse event is the model's.</remarks>
    [Native(Level.Close)] public float BlzGetTriggerPlayerMouseY() => _mouseNow?.Y ?? 0;
    /// <summary>A new location at the world place of the mouse event now firing; nil outside one.</summary>
    /// <remarks>Close: the documented answer, not measured in the game; nil outside a mouse event is the model's.</remarks>
    [Native(Level.Close)] public location? BlzGetTriggerPlayerMousePosition() => _mouseNow is { } now ? Location(now.X, now.Y) : null;
    /// <summary>The x of the last mouse place given, cut to a whole number; 0 before any.</summary>
    /// <remarks>Close: the screen place is taken as the world place given, not measured in the game.</remarks>
    [Native(Level.Close)] public int BlzGetMouseScreenPosX() => (int)_mouseAt.X;
    /// <summary>The y of the last mouse place given, cut to a whole number; 0 before any.</summary>
    /// <remarks>Close: the screen place is taken as the world place given, not measured in the game.</remarks>
    [Native(Level.Close)] public int BlzGetMouseScreenPosY() => (int)_mouseAt.Y;

    /// <summary>The player presses or releases the key (an OS key code of <c>ConvertOsKeyType</c>) with the meta keys held: the key events registered for it fire.</summary>
    internal void Key(player p, int key, int meta, bool down)
    {
        if (down) _keysDown.Add((p, key)); else _keysDown.Remove((p, key));
        var outer = _keyNow;
        _keyNow = (Convert<oskeytype>(key), meta, down);
        try
        {
            foreach (var (player, registeredKey, registeredMeta, registeredDown, registered) in _keyEvents.ToArray())
                if (player == p && registeredKey.Id == key && registeredMeta == meta && registeredDown == down) Fire(registered, Convert<playerevent>(311), player: p);
        }
        finally { _keyNow = outer; }
    }

    /// <summary>The player moves the mouse over the map, or presses or releases the button (1 left, 2 middle, 3 right) there.</summary>
    internal void Mouse(player p, int kind, int button, float x, float y)
    {
        if (kind == MouseDown) _buttonsDown.Add((p, button));
        else if (kind == MouseUp) _buttonsDown.Remove((p, button));
        _mouseAt = (x, y);
        var outer = _mouseNow;
        _mouseNow = (button, x, y);
        try { FirePlayerEvent(p, kind, p); }
        finally { _mouseNow = outer; }
    }

    /// <summary>The player selects exactly these units: the others are deselected first, then the new ones selected, each with its events.</summary>
    internal void Choose(player p, IReadOnlyList<unit> units)
    {
        var chosen = units.Where(u => !u.Dead && !u.Removed).Distinct().ToList();
        var now = Selected(p);
        foreach (var u in now.Where(u => !chosen.Contains(u)).OrderBy(u => u.Id).ToList())
        {
            now.Remove(u);
            FireUnit(new(u), UnitDeselectedKind, PlayerUnitDeselectedKind);
        }
        foreach (var u in chosen.Where(u => !now.Contains(u)))
        {
            now.Add(u);
            FireUnit(new(u), UnitSelectedKind, PlayerUnitSelectedKind);
        }
    }

    /// <summary>The player leaves the game: its slot is Left, and the events registered for it leaving fire.</summary>
    internal void Leave(player p)
    {
        if (p.SlotState != Playing) return;
        p.SlotState = Left;
        FirePlayerEvent(p, PlayerLeave, p);
    }

    /// <summary>The units of the run, the first made first.</summary>
    internal IEnumerable<unit> AllUnits => _units.Values;

    internal unit? UnitById(int id) => _units.GetValueOrDefault(id);

    /// <summary>Whether the unit's type is a building.</summary>
    internal bool Structure(unit u) => u.Type.Integer(IsBuilding) != 0;

    /// <summary>The living destructable within 64 of the place either way, the nearest; a replay's order at a tree names it by the game's own number.</summary>
    internal destructable? TreeAt(float x, float y) =>
        _destructables.Where(d => !d.Dead && Math.Abs(d.X - x) <= 64 && Math.Abs(d.Y - y) <= 64).MinBy(d => (d.X - x) * (d.X - x) + (d.Y - y) * (d.Y - y));

    /// <summary>The pathing grid of the map, a byte a cell of 32 with the bits of <c>PATHING_TYPE_*</c>, from its south-west corner; none where the map has none.</summary>
    internal (int Width, int Height, float Left, float Bottom, byte[] Cells)? PathingGrid() =>
        Data.Terrain is { } terrain && Cells() is { } cells && Data.Map?.Files.PathingMap is { } pathing ? ((int)pathing.Width, (int)pathing.Height, terrain.Grid.Item3, terrain.Grid.Item4, cells) : null;

    /// <summary>The cells the regions of the map hold, as rects in the world, the added blocks of each region in order.</summary>
    internal IEnumerable<(int Region, float X1, float Y1, float X2, float Y2)> RegionRects() =>
        _regionEvents.Select(e => e.Region).Distinct().SelectMany((region, index) =>
            region.Blocks.Where(block => block.Added).Select(block => (index, block.West * 32f, block.South * 32f, (block.East + 1) * 32f, (block.North + 1) * 32f)));

    /// <summary>The bounds of the map: west, south, east and north.</summary>
    internal (float MinX, float MinY, float MaxX, float MaxY)? WorldBounds() => Data.Terrain?.Bounds;

    /// <summary>The order string of the order id, or null for one the game has no name for.</summary>
    internal static string? NameOfOrder(int id) => OrderNames.GetValueOrDefault(id);
}
