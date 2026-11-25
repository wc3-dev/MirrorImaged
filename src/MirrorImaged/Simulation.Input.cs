using System.Text.Json;
using MirrorImaged.Data;
using MirrorImaged.Natives;
using MirrorImaged.Lua;
using MirrorImaged.World;

namespace MirrorImaged;

/// <summary>A unit as a test reads it.</summary>
public sealed record UnitView(int Id, string Type, string Name, int Player, float X, float Y, float Life, float MaxLife, float Mana, int Order, bool Dead);

/// <summary>A missile in flight as a test reads it: the run's count of it, the player it was shot for, the unit it follows (none for a shell, which flies to a place) and where it is.</summary>
public sealed record MissileView(int Id, int Player, int? Target, float X, float Y);

/// <summary>
/// One thing a person did at a step of the run. <see cref="Kind"/> says what, and what the rest hold:
/// <list type="bullet">
/// <item><c>say</c>: <see cref="Text"/> is what the player said.</item>
/// <item><c>order</c>: <see cref="Text"/> is the order string; <see cref="Ints"/> the unit, then the target unit if any; <see cref="Floats"/> x and y for an order at a place.</item>
/// <item><c>select</c>: <see cref="Ints"/> the units selected.</item>
/// <item><c>key</c>: <see cref="Ints"/> the key (an OS key code), the meta keys held (1 shift, 2 control, 4 alt, 8 meta) and 1 pressed or 0 released.</item>
/// <item><c>mouse</c>: <see cref="Ints"/> what the mouse did (305 pressed, 306 released, 307 moved) and the button (1 left, 2 middle, 3 right); <see cref="Floats"/> x and y.</item>
/// <item><c>click</c>: <see cref="Ints"/> the button of the dialog the player is shown, counting from 0.</item>
/// <item><c>leave</c>: nothing more; the player leaves the game.</item>
/// <item><c>act</c>: an order read from a <c>.w3g</c> replay, which only a replay's run makes: <see cref="Ints"/> the unit, the order id, how it is given
/// (0 alone, 1 at the place, 2 at the unit, 3 at the tree at the place or else the place, 4 taken out of the building's queue), the target unit
/// (for 4, the place in the queue) and 1 when queued; <see cref="Floats"/> x and y.</item>
/// </list>
/// </summary>
public sealed record Input(long Step, string Kind, int Player, int[]? Ints = null, float[]? Floats = null, string? Text = null);

/// <summary>
/// What a run was given: its map, seed, seats and settings, each input at its step and the step it ran to. Running the map again with it, and the
/// documents the run started with, repeats the run exactly.
/// </summary>
public sealed class Recording
{
    /// <summary>The format this MirrorImaged writes and reads (decisions D903).</summary>
    public const int Format = 1;

    /// <summary>The format of the file, <see cref="Format"/>; a recording written before formats were numbered reads as 1.</summary>
    public int Version { get; set; } = Format;
    /// <summary>The map the run was started from.</summary>
    public string Map { get; set; } = "";
    /// <summary>The seed of the random numbers.</summary>
    public int Seed { get; set; }
    /// <summary>The slots people play, the first the local player; null for the map's first user.</summary>
    public int[]? Users { get; set; }
    /// <summary>The slots computers play; null for the map's computers.</summary>
    public int[]? Computers { get; set; }
    /// <summary>The difficulty the computers play at.</summary>
    public Difficulty Difficulty { get; set; } = Difficulty.Normal;
    /// <summary>The graphics mode the run was in.</summary>
    public GraphicsMode Graphics { get; set; }
    /// <summary>The race the lobby chose for a slot, as <see cref="SimulationOptions.Races"/>; null for the map's.</summary>
    public Dictionary<int, int>? Races { get; set; }
    /// <summary>The start location the lobby placed a slot at, as <see cref="SimulationOptions.StartLocations"/>; null for the map's.</summary>
    public Dictionary<int, int>? StartLocations { get; set; }
    /// <summary>The natives the run ran as Missing, <see cref="SimulationOptions.TreatAsMissing"/> and <c>MIRROR_MISSING</c>'s, in order; null for none.</summary>
    public string[]? TreatAsMissing { get; set; }
    /// <summary>What was given, in the order it was given.</summary>
    public List<Input> Inputs { get; set; } = [];
    /// <summary>The step the run had reached.</summary>
    public long Steps { get; set; }

    static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    /// <summary>The recording as JSON.</summary>
    public string ToJson() => JsonSerializer.Serialize(this, Json);

    /// <summary>A recording from the JSON that <see cref="ToJson"/> makes, by this or an earlier MirrorImaged.</summary>
    /// <exception cref="InvalidDataException">The JSON isn't a recording, or is of a format this MirrorImaged doesn't read.</exception>
    public static Recording FromJson(string json) => Checked(JsonSerializer.Deserialize<Recording>(json) ?? throw new InvalidDataException("Not a recording"));

    internal static Recording Checked(Recording recording) =>
        recording.Version == Format ? recording : throw new InvalidDataException($"The recording is of format {recording.Version}, and this MirrorImaged reads format {Format}");

    /// <summary>
    /// The options that run the map as the recording's run was: its seed, seats, difficulty, graphics mode, races, start locations and natives run as
    /// Missing, with the documents given (new, empty ones when null), which a recording doesn't keep.
    /// </summary>
    public SimulationOptions Options(Documents? documents = null) => Options(documents, 0);

    // As Options, on the desync check's machine
    internal SimulationOptions Options(Documents? documents, int machine) => new()
    {
        Seed = Seed, Users = Users, Computers = Computers, Difficulty = Difficulty, Graphics = Graphics, Races = Races, StartLocations = StartLocations,
        TreatAsMissing = TreatAsMissing, Documents = documents ?? new(), Machine = machine,
    };
}

// The tests' hands: what a person at a player can do, the units a test can read, and the recording of both.
public sealed partial class Simulation
{
    readonly Recording _recording = new();

    /// <summary>What the run was given so far, as <see cref="Recording"/>; <see cref="Recording.Steps"/> is where it has got to.</summary>
    public Recording Recording
    {
        get
        {
            _recording.Steps = _game.Step;
            return _recording;
        }
    }

    /// <summary>The step of the run now, 1024 to the second.</summary>
    public long Step => _game.Step;

    internal static string FourCC(int id) => new([(char)(id >> 24 & 255), (char)(id >> 16 & 255), (char)(id >> 8 & 255), (char)(id & 255)]);

    UnitView View(unit u) => new(u.Id, FourCC(u.Type.Id), _natives.GetUnitName(u) ?? "", _natives.GetPlayerId(u.Owner), u.X, u.Y, u.Life, u.MaxLife, u.Mana, u.Order, u.Dead);

    /// <summary>The units of the run, living ones, the first made first; of one player only when given.</summary>
    public IReadOnlyList<UnitView> Units(int? player = null) =>
        [.. _natives.AllUnits.Where(u => !u.Dead && !u.Removed && (player is null || _natives.GetPlayerId(u.Owner) == player)).Select(View)];

    /// <summary>The missiles in flight now, the first to leave first.</summary>
    public IReadOnlyList<MissileView> Missiles()
    {
        MissileView View(GameNatives.Missile m)
        {
            var (x, y) = m.Place(_game.Step);
            return new(m.Id, m.Player, m.Target?.Id, (float)x, (float)y);
        }
        return [.. _natives.Missiles.Select(View)];
    }

    /// <summary>The unit with the id, or null when there is none.</summary>
    public UnitView? Unit(int id) => _natives.UnitById(id) is { } u ? View(u) : null;

    /// <summary>
    /// Moves the clock on by whole steps (1/1024 s), exactly. As <see cref="Advance"/>, which moves it by a time, rounded to the step
    /// nearest all the time asked for so far.
    /// </summary>
    public void AdvanceSteps(long steps)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(steps);
        if (Stopped is not null) throw Stopped;
        try { _game.Advance(steps); }
        catch (LuaException e) when (_lua.Halted is not null)
        {
            Stopped = e;
            throw;
        }
        _time = TimeSpan.FromSeconds((double)_game.Step / Game.StepsPerSecond);
    }

    /// <summary>Advances, a step of the time at a time, until the check holds or the time is out; true when it held.</summary>
    public bool Until(Func<bool> check, TimeSpan within, TimeSpan? every = null)
    {
        var slice = every ?? TimeSpan.FromMilliseconds(100);
        for (var spent = TimeSpan.Zero; ; spent += slice)
        {
            if (check()) return true;
            if (spent >= within) return false;
            Advance(slice);
        }
    }

    /// <summary>Gives the input, as a person's: the call the methods below make, and the one a recording's inputs are replayed with.</summary>
    public void Give(Input input) => Apply(input, true);

    // Gives the input, as a person's, and keeps it for the recording when asked
    internal void Apply(Input input, bool record)
    {
        if (record) _recording.Inputs.Add(input);
        if (_natives.Player(input.Player) is not { } player) return;
        switch (input.Kind)
        {
            case "say":
                _natives.Say(player, input.Text ?? "");
                break;
            case "order":
                if (_natives.UnitById(input.Ints![0]) is { } u && u.Owner == player) GiveOrder(u, input);
                break;
            case "select":
                _natives.Choose(player, [.. input.Ints!.Select(_natives.UnitById).OfType<unit>().Where(u => u.Owner == player)]);
                break;
            case "key":
                _natives.Key(player, input.Ints![0], input.Ints[1], input.Ints[2] != 0);
                break;
            case "mouse":
                _natives.Mouse(player, input.Ints![0], input.Ints[1], input.Floats![0], input.Floats[1]);
                break;
            case "leave":
                _natives.Leave(player);
                break;
            case "click":
                _natives.Click(player, input.Ints![0]);
                break;
            case "act":
                Act(input, false);
                break;
            default:
                throw new ArgumentException($"Unknown input {input.Kind}");
        }
    }

    void GiveOrder(unit u, Input input)
    {
        var id = _natives.OrderId(input.Text);
        if (id == 0) throw new ArgumentException($"Unknown order {input.Text}");
        if (input.Ints!.Length > 1 && _natives.UnitById(input.Ints[1]) is { } target) _natives.IssueTargetOrderById(u, id, target);
        else if (input.Floats is { Length: 2 }) _natives.IssuePointOrderById(u, id, input.Floats[0], input.Floats[1]);
        else _natives.IssueImmediateOrderById(u, id);
    }

    /// <summary>The player orders the unit, as the game's order string says (<c>move</c>, <c>attack</c>, <c>smart</c>), to a place or another unit, or alone; only a player's own units obey.</summary>
    public void Order(int player, int unit, string order, float? x = null, float? y = null, int? target = null) =>
        Apply(new(_game.Step, "order", player, target is { } t ? [unit, t] : [unit], x is { } px && y is { } py ? [px, py] : null, order), true);

    /// <summary>The player selects exactly these units, which it owns.</summary>
    public void Select(int player, params int[] units) => Apply(new(_game.Step, "select", player, units), true);

    /// <summary>The player presses or releases the key (an OS key code) with the meta keys (1 shift, 2 control, 4 alt, 8 meta) held.</summary>
    public void Key(int player, int key, bool down, int meta = 0) => Apply(new(_game.Step, "key", player, [key, meta, down ? 1 : 0]), true);

    /// <summary>The player presses (<paramref name="kind"/> 305), releases (306) or moves (307) the mouse with the button (1 left, 2 middle, 3 right) at the place.</summary>
    public void Mouse(int player, int kind, int button, float x, float y) => Apply(new(_game.Step, "mouse", player, [kind, button], [x, y]), true);

    /// <summary>The player clicks the button (counting from 0) of the dialog it is shown.</summary>
    public void Click(int player, int button) => Apply(new(_game.Step, "click", player, [button]), true);

    /// <summary>The player leaves the game.</summary>
    public void Leave(int player) => Apply(new(_game.Step, "leave", player), true);

    /// <summary>
    /// Runs the map again as the recording says: its seed, seats and settings, each input at its step, to the step it ran to; with the documents the run
    /// started with, when it read any.
    /// </summary>
    public static Simulation Replay(Recording recording, string? map = null, Documents? documents = null)
    {
        var simulation = Load(map ?? recording.Map, recording.Options(documents));
        simulation.Follow(recording);
        return simulation;
    }

    /// <summary>Gives a loaded simulation the recording's inputs at their steps and runs it to the step the recording reached.</summary>
    public void Follow(Recording recording)
    {
        foreach (var input in recording.Inputs.OrderBy(i => i.Step))
        {
            if (input.Step > _game.Step) AdvanceSteps(input.Step - _game.Step);
            Apply(input, true);
        }
        if (recording.Steps > _game.Step) AdvanceSteps(recording.Steps - _game.Step);
    }
}
