namespace MirrorImaged.Natives;

public partial class unit
{
    internal int CarryGold, CarryLumber;
    internal widget? HarvestTarget;   // the mine or tree a worker goes back to after dropping its load
    internal double HarvestEnds;      // when the worker's present work at the mine or tree is done, 0 when it isn't at one
    internal readonly List<unit> InMine = [];   // a mine's: the workers working it now
}

// A worker given 'harvest' on a gold mine or a tree walks to it, works, takes a load and carries it to the nearest town hall of its owner
// ('returnresources'), then goes back until the mine or tree is spent (probes units-harvest and units-harvest-2, facts "Harvesting"). A gold
// mine holds as many workers at once as its Agld's Gld3 (1) for Gld2 seconds (1) each, so five peasants or more take its 10 gold a second; a
// worker carries its harvest ability's gold or lumber capacity; a tree loses 1 life a chop, a chop every cooldown of the worker's second
// weapon. A haunted mine (Abgm) takes Gld3 acolytes (5) and pays Gld1 gold (10) straight to the player, the first Gld2 seconds after one comes
// and then every Gld2 x Gld3 seconds (decisions D643). Close: a worker in a mine isn't hidden, and where workers wait their turn isn't measured.
public sealed partial class GameNatives
{
    const int OrderHarvest = 852018, OrderReturn = 852020, TripLoad = 10, TownHallKind = 64;
    static readonly int GoldMine = Code("Agld"), HauntedMine = Code("Abgm"), Gld1 = Code("Gld1"), Gld2 = Code("Gld2"), Gld3 = Code("Gld3"),
        LumberLoad = Code("Har2"), GoldLoad = Code("Har3");
    static readonly int[] Harvesting = [Code("Ahar"), Code("Ahrl"), Code("Awha"), Code("Aaha")];

    static readonly int[] GoldHarvesting = [Code("Ahar"), Code("Aaha"), Code("Awha")], LumberHarvesting = [Code("Ahar"), Code("Ahrl"), Code("Awha")];

    // Whether the worker can harvest the target: a tree, or a mine with gold, with an ability that harvests it
    internal bool CanHarvest(unit u, widget target) =>
        (target is destructable ? LumberHarvesting : target is unit mine && GetResourceAmount(mine) > 0 ? GoldHarvesting : []).Any(id => Find(u, id) is not null);

    static bool SpentTarget(GameNatives game, widget? t) => t switch
    {
        unit mine => mine.Dead || mine.Removed || game.GetResourceAmount(mine) <= 0,
        destructable tree => tree.Dead || tree.Life <= 0.405f,
        _ => true,
    };

    int IntegerOf(unit u, int ability, int field, int otherwise) =>
        Find(u, ability) is { } a && BlzGetAbilityIntegerLevelField(a, Convert<abilityintegerlevelfield>(field), 0) is > 0 and var value ? value : otherwise;

    float RealOf(unit u, int ability, int field, float otherwise) =>
        Find(u, ability) is { } a && BlzGetAbilityRealLevelField(a, Convert<abilityreallevelfield>(field), 0) is > 0 and var value ? value : otherwise;

    // What the worker carries a trip: its harvest ability's capacity, or 10 where its data gives none
    int Load(unit u, int field) => Harvesting.Select(id => IntegerOf(u, id, field, 0)).FirstOrDefault(value => value > 0, TripLoad);

    bool Harvest(unit u)
    {
        var target = u.OrderTarget;
        if (SpentTarget(this, target)) return Finish(u);
        var (tx, ty) = target is unit mine ? (mine.X, mine.Y) : (GetWidgetX(target), GetWidgetY(target));
        if (Distance2(tx - u.X, ty - u.Y) > 150 * 150)
        {
            u.HarvestEnds = 0;
            return Walk(u, tx, ty, follow: true);
        }
        return target is unit gold ? Find(gold, HauntedMine) is not null ? Haunt(u, gold) : Mine(u, gold) : Chop(u, (destructable)target!);
    }

    // Whether the worker has a place in the mine, taking one if there is room
    bool InMine(unit u, unit mine, int room)
    {
        if (mine.InMine.Contains(u)) return true;
        mine.InMine.RemoveAll(w => w.Dead || w.Removed || w.Order != OrderHarvest || w.OrderTarget != mine);
        if (mine.InMine.Count >= room) return false;
        mine.InMine.Add(u);
        return true;
    }

    bool Mine(unit u, unit mine)
    {
        if (!InMine(u, mine, IntegerOf(mine, GoldMine, Gld3, 1))) return true;
        if (u.HarvestEnds == 0) u.HarvestEnds = Seconds + RealOf(mine, GoldMine, Gld2, 1);
        if (Seconds < u.HarvestEnds) return true;
        (u.HarvestEnds, u.CarryLumber) = (0, 0);
        mine.InMine.Remove(u);
        var take = Math.Min(Load(u, GoldLoad), GetResourceAmount(mine));
        AddResourceAmount(mine, -take);
        u.CarryGold = take;
        return Carry(u, mine);
    }

    bool Haunt(unit u, unit mine)
    {
        if (!InMine(u, mine, IntegerOf(mine, HauntedMine, Gld3, 5))) return true;
        var every = RealOf(mine, HauntedMine, Gld2, 1);
        if (u.HarvestEnds == 0) u.HarvestEnds = Seconds + every;
        if (Seconds < u.HarvestEnds) return true;
        u.HarvestEnds += every * IntegerOf(mine, HauntedMine, Gld3, 5);
        var take = Math.Min(IntegerOf(mine, HauntedMine, Gld1, TripLoad), GetResourceAmount(mine));
        AddResourceAmount(mine, -take);
        SetPlayerState(u.Owner, Convert<playerstate>(Gold), u.Owner.States[Gold] + take);
        u.Owner.GoldMined += take;
        return true;
    }

    // A chop on coming to the tree, then one each cooldown of the worker's second weapon, until it carries its load
    bool Chop(unit u, destructable tree)
    {
        if (u.HarvestEnds == 0) u.HarvestEnds = Seconds;
        if (Seconds < u.HarvestEnds) return true;
        u.CarryGold = 0;
        SetDestructableLife(tree, tree.Life - 1);
        u.CarryLumber++;
        var cooldown = BlzGetUnitWeaponRealField(u, Convert<unitweaponrealfield>(Code("ua1c")), 1);
        u.HarvestEnds = Seconds + (cooldown > 0 ? cooldown : 1);
        if (u.CarryLumber < Load(u, LumberLoad) && !SpentTarget(this, tree)) return true;
        u.HarvestEnds = 0;
        return Carry(u, tree);
    }

    bool Carry(unit u, widget from)
    {
        u.HarvestTarget = from;
        u.Order = OrderReturn;
        u.OrderTarget = null;
        return true;
    }

    // Whether a unit type is a town hall, kept by type, as a worker asks about every unit of the game each step
    readonly Dictionary<int, bool> _hallTypes = [];

    bool IsTownHall(unit u)
    {
        if (!_hallTypes.TryGetValue(u.Type.Id, out var hall))
            _hallTypes[u.Type.Id] = hall = (BlzGetUnitIntegerField(u, Convert<unitintegerfield>(Classification)) & TownHallKind) != 0;
        return hall;
    }

    // The town halls of each player, listed again once in a tick
    readonly Dictionary<player, List<unit>> _hallsOf = [];
    long _hallsAt = -1;
    int _hallsUnits;

    // The nearest finished town hall of the worker's owner, or none
    unit? DropOff(unit u)
    {
        if (_hallsAt != _ticks || _hallsUnits != _units.Count)
        {
            (_hallsAt, _hallsUnits) = (_ticks, _units.Count);
            _hallsOf.Clear();
            foreach (var hall in _units.Values.Where(h => !h.Dead && !h.Removed && !h.Constructing && IsTownHall(h)))
            {
                if (!_hallsOf.TryGetValue(hall.Owner, out var list)) _hallsOf[hall.Owner] = list = [];
                list.Add(hall);
            }
        }
        return _hallsOf.TryGetValue(u.Owner, out var halls) ? halls.MinBy(hall => Distance2(hall.X - u.X, hall.Y - u.Y)) : null;
    }

    bool ReturnLoad(unit u)
    {
        if (u.CarryGold + u.CarryLumber == 0 || DropOff(u) is not { } hall) return Finish(u);
        var reach = hall.Type.Real(Collision) + 100;
        if (Distance2(hall.X - u.X, hall.Y - u.Y) > reach * reach) return Walk(u, hall.X, hall.Y, follow: true);
        SetPlayerState(u.Owner, Convert<playerstate>(Gold), u.Owner.States[Gold] + u.CarryGold);
        SetPlayerState(u.Owner, Convert<playerstate>(Lumber), u.Owner.States[Lumber] + u.CarryLumber);
        u.Owner.GoldMined += u.CarryGold;
        (u.CarryGold, u.CarryLumber) = (0, 0);
        if (SpentTarget(this, u.HarvestTarget)) return Finish(u);
        u.Order = OrderHarvest;
        u.OrderTarget = u.HarvestTarget;
        return true;
    }
}
