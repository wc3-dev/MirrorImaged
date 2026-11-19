using MirrorImaged.Data;
using MirrorImaged.Natives;

namespace MirrorImaged;

/// <summary>A game minute of a replay's run: the orders given in it, those a unit took, and the units the orders named so far and found so far.</summary>
public sealed record ReplayMinute(int Minute, int Orders, int Taken, int Named, int Found);

/// <summary>
/// A replay played on its map: each player's selection, groups, orders, chat and leaving, given at the step of its time. A replay names a unit by
/// the game's own numbers for it, which a run here does not have, so the run works out which unit each number is (decisions D422): a player's
/// units are matched in the order they were made, by the type the replay tells of them where it does, and an order's target by where it stands.
/// What the units then do is as close to the game as MirrorImaged is, so the run drifts from the game that was played; the counts here say how
/// much of the replay still reached a unit.
/// </summary>
public sealed class ReplayRun
{
    const int Smart = 851971, Stop = 851972, Attack = 851983, Move = 851986, Patrol = 851990, Hold = 851993;

    readonly Simulation _simulation;
    readonly GameNatives _natives;
    readonly Replay _replay;
    readonly Dictionary<int, int> _slots;
    readonly Dictionary<int, List<long>> _selected = [];
    readonly Dictionary<(int Slot, int Group), List<long>> _groups = [];
    readonly Dictionary<int, int> _subgroups = [];
    readonly Dictionary<long, int> _types = [];
    readonly Dictionary<long, bool> _buildings = [];
    readonly Dictionary<long, int> _units = [];
    readonly HashSet<long> _named = [];
    int _next;

    /// <summary>The orders of the replay given so far.</summary>
    public int Orders { get; private set; }

    /// <summary>Those of them a unit took.</summary>
    public int Taken { get; private set; }

    /// <summary>Those of them given with no unit of the selection known here: the unit was never made in this run, or is dead in it.</summary>
    public int Unowned { get; private set; }

    /// <summary>How many orders the units here refused, by the order's id.</summary>
    public SortedDictionary<int, int> Refused { get; } = [];

    /// <summary>The items taken out of a building's queue (action 0x1E) that were there to take.</summary>
    public int Dequeued { get; private set; }

    /// <summary>The run by game minute, those with orders.</summary>
    public IReadOnlyList<ReplayMinute> Minutes => _minutes;

    /// <summary>The first order no unit took: when, whose, which and why; null while every order was taken.</summary>
    public string? FirstMiss { get; private set; }

    readonly List<ReplayMinute> _minutes = [];

    /// <summary>The units the replay's orders were for.</summary>
    public int Named => _named.Count;

    /// <summary>Those of them found in this run.</summary>
    public int Found => _named.Count(_units.ContainsKey);

    /// <summary>
    /// How many actions of the replay are read and not played, by the replay's number for them: giving and dropping items (0x13), since the
    /// replay names the item by the game's numbers and no action before tells which item of the unit's they are; and the escape key (0x61),
    /// which changes only the player's own screen.
    /// </summary>
    public SortedDictionary<int, int> NotPlayed { get; } = [];

    /// <summary>The step of the replay's end.</summary>
    public long Steps => StepOf(_replay.DurationMillis);

    internal ReplayRun(Simulation simulation, Replay replay)
    {
        (_simulation, _natives, _replay) = (simulation, simulation.Natives, replay);
        _slots = replay.Players.Where(player => player.Id > 0).ToDictionary(player => player.Id, player => player.Slot);
        foreach (var (code, count) in replay.ActionCounts)
            if (code is 0x13 or 0x61) NotPlayed[code] = count;
    }

    static long StepOf(long millis) => (long)Math.Round(millis * 1024.0 / 1000);

    /// <summary>Plays the replay on to the step: each action up to it at its own step.</summary>
    public void To(long step)
    {
        for (; _next < _replay.Actions.Count && StepOf(_replay.Actions[_next].Millis) <= step; _next++)
        {
            var action = _replay.Actions[_next];
            if (StepOf(action.Millis) > _simulation.Step) _simulation.AdvanceSteps(StepOf(action.Millis) - _simulation.Step);
            if (_slots.TryGetValue(action.Player, out var slot) && _natives.Player(slot) is { } player) Play(action, slot, player);
        }
        if (step > _simulation.Step) _simulation.AdvanceSteps(step - _simulation.Step);
    }

    void Play(ReplayAction action, int slot, player player)
    {
        var selected = _selected.TryGetValue(slot, out var current) ? current : _selected[slot] = [];
        switch (action.Kind)
        {
            case "select":
                if (action.Code == 2) selected.RemoveAll(action.Objects.Contains);
                else selected.AddRange(action.Objects.Where(id => !selected.Contains(id)));
                Show(slot, selected);
                break;
            case "group":
                _groups[(slot, action.Code)] = [.. action.Objects];
                break;
            case "pick":
                selected.Clear();
                selected.AddRange(_groups.GetValueOrDefault((slot, action.Code)) ?? []);
                Show(slot, selected);
                break;
            case "subgroup":
                (_types[action.Objects[0]], _subgroups[slot]) = (action.Code, action.Code);
                break;
            case "ability":
                // A building's queue, or a worker's build menu, says which of the two the unit is
                if (Simulation.FourCC(action.Code) is var ability && (ability == "Aque" || ability.EndsWith("bu", StringComparison.Ordinal)))
                    _buildings[action.Objects[0]] = ability == "Aque";
                break;
            case "order":
                Order(action, slot, player, selected);
                break;
            case "dequeue":
                // The building whose queue is shown: the selection's unit of the subgroup's type first
                if (selected.OrderBy(id => (ulong)id >> 32).Select(id => Unit(id, player, selected.Count > 1)).OfType<unit>()
                    .OrderBy(u => u.Type.Id == _subgroups.GetValueOrDefault(slot) ? 0 : 1)
                    .Any(u => _simulation.Act(new(_simulation.Step, "act", slot, [u.Id, action.Code, 4, (int)action.Objects[0], 0])))) Dequeued++;
                break;
            case "chat":
                _simulation.Give(new(_simulation.Step, "say", slot, Text: action.Text));
                break;
            case "leave":
                _simulation.Give(new(_simulation.Step, "leave", slot));
                break;
        }
    }

    // The selection in the run is the units of the replay's that are known here
    void Show(int slot, List<long> selected) =>
        _simulation.Give(new(_simulation.Step, "select", slot, [.. selected.Select(id => _units.GetValueOrDefault(id)).Where(id => id != 0)]));

    void Order(ReplayAction action, int slot, player player, List<long> selected)
    {
        Orders++;
        _named.UnionWith(selected);
        // The game counts the second number up as it makes objects, so the selection in that order is in the order its units were made
        var units = selected.OrderBy(id => (ulong)id >> 32).Select(id => Unit(id, player, selected.Count > 1)).OfType<unit>().ToList();
        if (units.Count == 0)
        {
            Unowned++;
            Count(action, false, "no unit of the selection is in this run");
            return;
        }
        var (mode, target) = float.IsNaN(action.X) ? (0, 0) : action.TargetType != 0 ? OfType(action.TargetType, action.X, action.Y)
            : action.Objects is [var named] && named != -1 ? Target(named, action.X, action.Y) : (1, 0);
        bool Give(unit u) => _simulation.Act(new(_simulation.Step, "act", slot, [u.Id, action.Code, mode, target, action.Queued ? 1 : 0], mode == 0 ? null : [action.X, action.Y]));
        bool taken;
        if (action.Code is Smart or Stop or Attack or Move or Patrol or Hold) taken = units.Count(Give) > 0;
        // A button of the units' card goes to one unit of the subgroup that is shown (decisions D423)
        else taken = units.OrderBy(u => u.Type.Id == _subgroups.GetValueOrDefault(slot) ? 0 : 1).Any(Give);
        if (taken) Taken++;
        else Refused[action.Code] = Refused.GetValueOrDefault(action.Code) + 1;
        Count(action, taken, $"{string.Join(", ", units.Select(u => Simulation.FourCC(u.Type.Id)).Distinct())} refused it");
    }

    void Count(ReplayAction action, bool taken, string why)
    {
        var minute = (int)(action.Millis / 60000);
        var row = _minutes.Count > 0 && _minutes[^1].Minute == minute ? _minutes[^1] : new ReplayMinute(minute, 0, 0, 0, 0);
        row = row with { Orders = row.Orders + 1, Taken = row.Taken + (taken ? 1 : 0), Named = Named, Found = Found };
        if (_minutes.Count > 0 && _minutes[^1].Minute == minute) _minutes[^1] = row;
        else _minutes.Add(row);
        if (!taken && FirstMiss is null)
            FirstMiss = $"{TimeSpan.FromMilliseconds(action.Millis):m\\:ss}, player {_slots[action.Player]}, {OrderName(action.Code)}: {why}";
    }

    string OrderName(int code) => _natives.OrderId2String(code) is { Length: > 0 } name ? name : Simulation.FourCC(code);

    // The unit of the player that the replay's numbers name: the one found for them before, or the first of the player's units no numbers have
    // yet, made between the units of the numbers before and after these, of the type the replay told of, if it did
    unit? Unit(long id, player player, bool many)
    {
        if (_units.TryGetValue(id, out var known)) return _natives.UnitById(known) is { Dead: false, Removed: false } unit ? unit : null;
        var (after, before) = (0, int.MaxValue);
        foreach (var (other, unit) in _units)
            if (_natives.UnitById(unit)?.Owner == player)
            {
                if ((ulong)other >> 32 < (ulong)id >> 32) after = Math.Max(after, unit);
                else before = Math.Min(before, unit);
            }
        var free = _natives.AllUnits.Where(u => u.Owner == player && !u.Dead && !u.Removed && u.Id > after && u.Id < before && !_units.ContainsValue(u.Id)).OrderBy(u => u.Id);
        var found = _types.TryGetValue(id, out var type) ? free.FirstOrDefault(u => u.Type.Id == type)
            : _buildings.TryGetValue(id, out var building) ? free.FirstOrDefault(u => _natives.Structure(u) == building)
            : many ? free.FirstOrDefault(u => !_natives.Structure(u))
            : free.FirstOrDefault();
        if (found is not null) _units[id] = found.Id;
        return found;
    }

    // An order at an object the replay names by its type: the unit of that type nearest the place, or the tree there, or the place
    (int Mode, int Target) OfType(int type, float x, float y) =>
        _natives.AllUnits.Where(u => !u.Dead && !u.Removed && u.Type.Id == type && Math.Abs(u.X - x) <= TypeReach && Math.Abs(u.Y - y) <= TypeReach)
            .OrderBy(u => (u.X - x) * (u.X - x) + (u.Y - y) * (u.Y - y)).ThenBy(u => u.Id).FirstOrDefault() is { } near ? (2, near.Id) : (3, 0);

    // How far from an order's place the unit of the type it names may stand: half a large building's side
    const float TypeReach = 128;

    // What an order with a target is given as: at the unit the target's numbers name, known or standing where the order says; or at the tree or
    // the ground there
    (int Mode, int Target) Target(long id, float x, float y)
    {
        if (_units.TryGetValue(id, out var known)) return _natives.UnitById(known) is { Dead: false, Removed: false } ? (2, known) : (1, 0);
        var near = _natives.AllUnits.Where(u => !u.Dead && !u.Removed && !_units.ContainsValue(u.Id) && Math.Abs(u.X - x) <= 64 && Math.Abs(u.Y - y) <= 64)
            .OrderBy(u => (u.X - x) * (u.X - x) + (u.Y - y) * (u.Y - y)).ThenBy(u => u.Id).FirstOrDefault();
        if (near is null) return (3, 0);
        _units[id] = near.Id;
        return (2, near.Id);
    }
}

public sealed partial class Simulation
{
    /// <summary>
    /// Loads the map for the replay: the people of its lobby whose slots the map has, each with the race the lobby holds for it and at the start
    /// place nearest the first place it gave an order at, since a replay does not say where the game put its players (decisions D421); in the
    /// graphics mode given, as a replay does not say which the players had.
    /// </summary>
    public static Simulation Load(string map, Replay replay, int seed = 0, GraphicsMode graphics = GraphicsMode.Reforged) =>
        Load(map, Seats(map, replay, seed, graphics));

    // The seats of the replay on the map, as Load's summary says
    internal static SimulationOptions Seats(string map, Replay replay, int seed = 0, GraphicsMode graphics = GraphicsMode.Reforged)
    {
        var players = MapArchive.Read(map).Files.Info?.Players ?? [];
        var seated = replay.Players.Where(player => player.Id > 0 && players.Any(p => p.Id == player.Slot && p.Controller == War3Net.Build.Info.PlayerController.User)).ToList();
        if (seated.Count == 0) throw new ArgumentException($"None of the replay's players has a slot of {map}");
        var starts = new Dictionary<int, int>();
        foreach (var player in seated)
            if (replay.Actions.FirstOrDefault(action => action.Player == player.Id && action.Kind == "order" && !float.IsNaN(action.X)) is { } first)
                starts[player.Slot] = players.Where(p => !starts.ContainsValue(p.Id))
                    .MinBy(p => (p.StartPosition.X - first.X) * (p.StartPosition.X - first.X) + (p.StartPosition.Y - first.Y) * (p.StartPosition.Y - first.Y))!.Id;
        foreach (var player in seated.Where(player => !starts.ContainsKey(player.Slot)))
            starts[player.Slot] = players.First(p => !starts.ContainsValue(p.Id)).Id;
        // The lobby's race flags: human 1, orc 2, night elf 4, undead 8; a player who took random has none of them, and gets a random race here
        var races = seated.Where(player => (player.Race & 15) != 0).ToDictionary(player => player.Slot, player => (player.Race & 15) switch { 1 => 1, 2 => 2, 4 => 4, _ => 3 });
        return new SimulationOptions { Seed = seed, Users = [.. seated.Select(player => player.Slot)], Computers = [], Races = races, StartLocations = starts, Graphics = graphics };
    }

    /// <summary>Starts playing the replay on this run, which <see cref="Load(string, Replay, int, GraphicsMode)"/> made for it; <see cref="ReplayRun.To"/> plays it on.</summary>
    public ReplayRun Play(Replay replay) => new(this, replay);

    /// <summary>Plays the replay on this run to its end.</summary>
    public ReplayRun Follow(Replay replay)
    {
        var run = Play(replay);
        run.To(run.Steps);
        return run;
    }

    // An order of a replay, given as the game gives a person's: Ints are the unit, the order, how it is given (0 alone, 1 at the place, 2 at the
    // unit, 3 at the tree at the place or else the place, 4 the type the order names taken out of the building's queue at the place the
    // fourth number gives), the target unit, and 1 when it is queued; true when the unit took it
    static readonly int Rally = Ids.Of("ARal");

    internal bool Act(Input input, bool record = true)
    {
        if (record) _recording.Inputs.Add(input);
        var (order, mode, queued) = (input.Ints![1], input.Ints[2], input.Ints[4] != 0);
        if (_natives.Player(input.Player) is not { } player || _natives.UnitById(input.Ints[0]) is not { } u || u.Owner != player) return false;
        if (mode == 4) return _natives.Dequeue(u, input.Ints[3], order);
        var (x, y) = input.Floats is [var px, var py] ? (px, py) : (0f, 0f);
        var builds = mode != 0 && _natives.Data.Object(ObjectKind.Unit, order) is not null;
        if (builds) return queued ? _natives.BlzQueueBuildOrderById(u, order, x, y) : _natives.IssueBuildOrderById(u, order, x, y);
        widget? target = mode switch { 2 => _natives.UnitById(input.Ints[3]), 3 => _natives.TreeAt(x, y), _ => null };
        // A person's smart order of a worker at a mine or a tree is the game's harvest order (decisions D424)
        if (order == 851971 && target is not null && _natives.CanHarvest(u, target)) order = 852018;
        // A building's smart is its rally point, which the game sets though the order answers false at a place: it took the order (decisions D644)
        if (order == 851971 && _natives.GetUnitAbilityLevel(u, Rally) > 0)
        {
            if (target is not null) _natives.IssueTargetOrderById(u, order, target);
            else _natives.IssuePointOrderById(u, order, x, y);
            return true;
        }
        if (target is not null) return queued ? _natives.BlzQueueTargetOrderById(u, order, target) : _natives.IssueTargetOrderById(u, order, target);
        if (mode != 0) return queued ? _natives.BlzQueuePointOrderById(u, order, x, y) : _natives.IssuePointOrderById(u, order, x, y);
        if (_natives.Data.Object(ObjectKind.Ability, order) is not null)
        {
            // A hero's skill learned from its card
            var points = _natives.GetHeroSkillPoints(u);
            _natives.SelectHeroSkill(u, order);
            return _natives.GetHeroSkillPoints(u) < points;
        }
        return queued ? _natives.BlzQueueImmediateOrderById(u, order) : _natives.IssueNeutralImmediateOrderById(player, u, order);
    }
}
