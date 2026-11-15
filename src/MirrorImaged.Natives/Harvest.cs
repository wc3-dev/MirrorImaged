namespace MirrorImaged.Natives;

public partial class unit
{
    internal int CarryGold, CarryLumber;
    internal widget? HarvestTarget;   // the mine or tree a worker goes back to after dropping its load
    internal double HarvestEnds;      // when the trip's work at the mine or tree is done, 0 when it isn't at one
}

// A worker given 'harvest' on a gold mine or a tree walks to it, works for a time, takes a load and carries it to the nearest town hall of its
// owner ('returnresources'), then goes back until the mine or tree is spent. Close: trip times and loads are the common ones (10 gold, 10
// lumber, a second at the mine), not read from the worker's abilities; a mine isn't entered; a tree gives 10 lumber for 10 of its life.
public sealed partial class GameNatives
{
    const int OrderHarvest = 852018, OrderReturn = 852020, TripLoad = 10, TownHallKind = 64;
    const double WorkSeconds = 1.0;

    static bool SpentTarget(GameNatives game, widget? t) => t switch
    {
        unit mine => mine.Dead || mine.Removed || game.GetResourceAmount(mine) <= 0,
        destructable tree => tree.Dead || tree.Life <= 0.405f,
        _ => true,
    };

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
        if (u.HarvestEnds == 0) u.HarvestEnds = Seconds + WorkSeconds;
        if (Seconds < u.HarvestEnds) return true;
        u.HarvestEnds = 0;
        if (target is unit gold)
        {
            var take = Math.Min(TripLoad, GetResourceAmount(gold));
            AddResourceAmount(gold, -take);
            u.CarryGold = take;
        }
        else if (target is destructable tree)
        {
            SetDestructableLife(tree, tree.Life - TripLoad);
            u.CarryLumber = TripLoad;
        }
        u.HarvestTarget = target;
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
        (u.CarryGold, u.CarryLumber) = (0, 0);
        if (SpentTarget(this, u.HarvestTarget)) return Finish(u);
        u.Order = OrderHarvest;
        u.OrderTarget = u.HarvestTarget;
        return true;
    }
}
