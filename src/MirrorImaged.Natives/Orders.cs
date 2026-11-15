using MirrorImaged.Data;

namespace MirrorImaged.Natives;

public partial class unit
{
    internal int Order;                                  // the current order's id, 0 when idle
    internal readonly List<(int Order, float X, float Y, widget? Target)> Queue = [];
    internal float RallyX, RallyY;
    internal widget? RallyTarget;
    internal bool GateActive;
    internal float GateX, GateY;
}

// Orders given from code, as game-facts' "Orders and events" measures them: the order is the unit's current one and its event fires
// inside the call, an unknown order answers false, 'stop' clears the order at once, and orders can be queued. What an order does
// (walking, fighting, casting, building) is the later waves': an order stays current here until another replaces it.
public sealed partial class GameNatives
{
    const string OrderFacts = "game-facts.md, Orders and events (10-21, 11-03, 11-05)";
    const int UnitIssuedTargetOrder = 77, PlayerUnitIssuedTargetOrder = 40, SetRally = 851980, Smart = 851971;

    // Whether the unit takes orders: not removed, and a dead one only an attack (facts: it answers true)
    static bool Takes(unit u, int order) => !u.Removed && (!u.Dead || order == 851983);

    // The order given: current at once, its event fired with the place or target, in the game's three kinds
    bool Give(unit? whichUnit, int order, float x, float y, widget? target, int kind)
    {
        if (whichUnit is null || order == 0 || !Takes(whichUnit, order)) return false;
        whichUnit.Order = order == Stop ? 0 : order;
        if (order == SetRally) (whichUnit.RallyX, whichUnit.RallyY, whichUnit.RallyTarget) = (x, y, target);
        var (unitKind, playerKind) = kind switch { 0 => (UnitIssuedOrder, PlayerUnitIssuedOrder), 1 => (UnitIssuedPointOrder, PlayerUnitIssuedPointOrder), _ => (UnitIssuedTargetOrder, PlayerUnitIssuedTargetOrder) };
        FireUnit(new(whichUnit, Order: order, PointX: x, PointY: y, Target: target), unitKind, playerKind);
        return true;
    }

    int Named(string? order) => order is null ? 0 : OrderIds.GetValueOrDefault(order.ToLowerInvariant());

    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueImmediateOrderById(unit? whichUnit, int order) => Give(whichUnit, order, 0, 0, null, 0);

    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueImmediateOrder(unit? whichUnit, string? order) => Give(whichUnit, Named(order), 0, 0, null, 0);

    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssuePointOrderById(unit? whichUnit, int order, float x, float y) => Give(whichUnit, order, x, y, null, 1);

    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssuePointOrder(unit? whichUnit, string? order, float x, float y) => Give(whichUnit, Named(order), x, y, null, 1);

    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssuePointOrderByIdLoc(unit? whichUnit, int order, location? whichLocation) => whichLocation is not null && IssuePointOrderById(whichUnit, order, whichLocation.X, whichLocation.Y);

    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssuePointOrderLoc(unit? whichUnit, string? order, location? whichLocation) => whichLocation is not null && IssuePointOrder(whichUnit, order, whichLocation.X, whichLocation.Y);

    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueTargetOrderById(unit? whichUnit, int order, widget? targetWidget) =>
        targetWidget is not null && Give(whichUnit, order, targetWidget is unit u ? u.X : GetWidgetX(targetWidget), targetWidget is unit v ? v.Y : GetWidgetY(targetWidget), targetWidget, 2);

    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueTargetOrder(unit? whichUnit, string? order, widget? targetWidget) => IssueTargetOrderById(whichUnit, Named(order), targetWidget);

    /// <summary>An instant order: given as the plain one is (the cast's instant nature comes with abilities, wave 4).</summary>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueInstantPointOrderById(unit? whichUnit, int order, float x, float y, widget? instantTargetWidget) => IssuePointOrderById(whichUnit, order, x, y);

    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueInstantPointOrder(unit? whichUnit, string? order, float x, float y, widget? instantTargetWidget) => IssuePointOrder(whichUnit, order, x, y);

    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueInstantTargetOrderById(unit? whichUnit, int order, widget? targetWidget, widget? instantTargetWidget) => IssueTargetOrderById(whichUnit, order, targetWidget);

    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueInstantTargetOrder(unit? whichUnit, string? order, widget? targetWidget, widget? instantTargetWidget) => IssueTargetOrder(whichUnit, order, targetWidget);

    /// <summary>A build order's id is the building's type; false unless the unit's data lists it (the abilities' build menus come later).</summary>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueBuildOrderById(unit? whichPeon, int unitId, float x, float y) =>
        whichPeon is not null && Data.Object(ObjectKind.Unit, unitId) is not null && Builds(whichPeon, unitId) && Give(whichPeon, unitId, x, y, null, 1);

    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueBuildOrder(unit? whichPeon, string? unitToBuild, float x, float y) =>
        unitToBuild is { Length: 4 } code && IssueBuildOrderById(whichPeon, Code(code), x, y);

    // The unit's data lists the type among what it builds (ubui), or it is a worker's type without a list
    bool Builds(unit u, int type) => (u.Type.Text(Code("ubui")) ?? "").Split(',', StringSplitOptions.TrimEntries).Contains(Name(type));

    // Neutral orders go to a unit the neutral player doesn't own, as the shops take them
    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueNeutralImmediateOrderById(player? forWhichPlayer, unit? neutralStructure, int unitId) => forWhichPlayer is not null && Give(neutralStructure, unitId, 0, 0, null, 0);

    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueNeutralImmediateOrder(player? forWhichPlayer, unit? neutralStructure, string? unitToBuild) => unitToBuild is { Length: 4 } code && IssueNeutralImmediateOrderById(forWhichPlayer, neutralStructure, Code(code));

    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueNeutralPointOrderById(player? forWhichPlayer, unit? neutralStructure, int unitId, float x, float y) => forWhichPlayer is not null && Give(neutralStructure, unitId, x, y, null, 1);

    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueNeutralPointOrder(player? forWhichPlayer, unit? neutralStructure, string? unitToBuild, float x, float y) => unitToBuild is { Length: 4 } code && IssueNeutralPointOrderById(forWhichPlayer, neutralStructure, Code(code), x, y);

    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueNeutralTargetOrderById(player? forWhichPlayer, unit? neutralStructure, int unitId, widget? target) => forWhichPlayer is not null && target is not null && IssueTargetOrderById(neutralStructure, unitId, target);

    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueNeutralTargetOrder(player? forWhichPlayer, unit? neutralStructure, string? unitToBuild, widget? target) => unitToBuild is { Length: 4 } code && IssueNeutralTargetOrderById(forWhichPlayer, neutralStructure, Code(code), target);

    // ---- Group orders: each unit of the group takes the order; true when every one did

    bool EachOf(group? whichGroup, Func<unit, bool> order) => whichGroup is not null && whichGroup.Units.ToArray().Aggregate(true, (all, u) => (Live(u) is { } alive && order(alive)) & all);

    [Native(Level.Close, Fact = OrderFacts)]
    public bool GroupImmediateOrderById(group? whichGroup, int order) => EachOf(whichGroup, u => IssueImmediateOrderById(u, order));

    [Native(Level.Close, Fact = OrderFacts)]
    public bool GroupImmediateOrder(group? whichGroup, string? order) => EachOf(whichGroup, u => IssueImmediateOrder(u, order));

    [Native(Level.Close, Fact = OrderFacts)]
    public bool GroupPointOrderById(group? whichGroup, int order, float x, float y) => EachOf(whichGroup, u => IssuePointOrderById(u, order, x, y));

    [Native(Level.Close, Fact = OrderFacts)]
    public bool GroupPointOrder(group? whichGroup, string? order, float x, float y) => EachOf(whichGroup, u => IssuePointOrder(u, order, x, y));

    [Native(Level.Close, Fact = OrderFacts)]
    public bool GroupPointOrderByIdLoc(group? whichGroup, int order, location? whichLocation) => whichLocation is not null && GroupPointOrderById(whichGroup, order, whichLocation.X, whichLocation.Y);

    [Native(Level.Close, Fact = OrderFacts)]
    public bool GroupPointOrderLoc(group? whichGroup, string? order, location? whichLocation) => whichLocation is not null && GroupPointOrder(whichGroup, order, whichLocation.X, whichLocation.Y);

    [Native(Level.Close, Fact = OrderFacts)]
    public bool GroupTargetOrderById(group? whichGroup, int order, widget? targetWidget) => EachOf(whichGroup, u => IssueTargetOrderById(u, order, targetWidget));

    [Native(Level.Close, Fact = OrderFacts)]
    public bool GroupTargetOrder(group? whichGroup, string? order, widget? targetWidget) => EachOf(whichGroup, u => IssueTargetOrder(u, order, targetWidget));

    // ---- Queues: an idle unit takes the order at once (the event fires), a busy one keeps it after the current order

    bool Queued(unit? whichUnit, int order, float x, float y, widget? target, int kind)
    {
        if (whichUnit is null || order == 0 || !Takes(whichUnit, order)) return false;
        if (whichUnit.Order == 0) return Give(whichUnit, order, x, y, target, kind);
        whichUnit.Queue.Add((order, x, y, target));
        return true;
    }

    [Native(Level.Close, Fact = OrderFacts)]
    public bool BlzQueueImmediateOrderById(unit? whichUnit, int order) => Queued(whichUnit, order, 0, 0, null, 0);

    [Native(Level.Close, Fact = OrderFacts)]
    public bool BlzQueuePointOrderById(unit? whichUnit, int order, float x, float y) => Queued(whichUnit, order, x, y, null, 1);

    [Native(Level.Close, Fact = OrderFacts)]
    public bool BlzQueueTargetOrderById(unit? whichUnit, int order, widget? targetWidget) => targetWidget is not null && Queued(whichUnit, order, GetWidgetX(targetWidget), GetWidgetY(targetWidget), targetWidget, 2);

    [Native(Level.Close, Fact = OrderFacts)]
    public bool BlzQueueInstantPointOrderById(unit? whichUnit, int order, float x, float y, widget? instantTargetWidget) => BlzQueuePointOrderById(whichUnit, order, x, y);

    [Native(Level.Close, Fact = OrderFacts)]
    public bool BlzQueueInstantTargetOrderById(unit? whichUnit, int order, widget? targetWidget, widget? instantTargetWidget) => BlzQueueTargetOrderById(whichUnit, order, targetWidget);

    [Native(Level.Close, Fact = OrderFacts)]
    public bool BlzQueueBuildOrderById(unit? whichPeon, int unitId, float x, float y) =>
        whichPeon is not null && Data.Object(ObjectKind.Unit, unitId) is not null && Builds(whichPeon, unitId) && Queued(whichPeon, unitId, x, y, null, 1);

    [Native(Level.Close, Fact = OrderFacts)]
    public bool BlzQueueNeutralImmediateOrderById(player? forWhichPlayer, unit? neutralStructure, int unitId) => forWhichPlayer is not null && Queued(neutralStructure, unitId, 0, 0, null, 0);

    [Native(Level.Close, Fact = OrderFacts)]
    public bool BlzQueueNeutralPointOrderById(player? forWhichPlayer, unit? neutralStructure, int unitId, float x, float y) => forWhichPlayer is not null && Queued(neutralStructure, unitId, x, y, null, 1);

    [Native(Level.Close, Fact = OrderFacts)]
    public bool BlzQueueNeutralTargetOrderById(player? forWhichPlayer, unit? neutralStructure, int unitId, widget? target) => forWhichPlayer is not null && target is not null && Queued(neutralStructure, unitId, GetWidgetX(target), GetWidgetY(target), target, 2);

    /// <summary>The current order's id, 0 when idle ('stop' and a never-ordered unit read 0).</summary>
    [Native(Level.Close, Fact = OrderFacts)]
    public int GetUnitCurrentOrder(unit? whichUnit) => whichUnit?.Order ?? 0;

    /// <summary>The current order and those queued after it; the stunned order of a pause counts for one in its own event (facts, 11-05).</summary>
    [Native(Level.Close, Fact = OrderFacts)]
    public int BlzGetUnitOrderCount(unit? whichUnit) => whichUnit is null ? 0 : Math.Max(whichUnit.Order != 0 ? 1 : 0, Response(UnitIssuedPointOrder, PlayerUnitIssuedPointOrder)?.Unit == whichUnit && Response(UnitIssuedPointOrder, PlayerUnitIssuedPointOrder)?.Order == Stunned ? 1 : 0) + whichUnit.Queue.Count;

    [Native(Level.Close, Fact = OrderFacts)]
    public void BlzUnitClearOrders(unit? whichUnit, bool onlyQueued)
    {
        if (whichUnit is null) return;
        whichUnit.Queue.Clear();
        if (!onlyQueued) whichUnit.Order = 0;
    }

    [Native(Level.Close, Fact = OrderFacts)]
    public void BlzUnitForceStopOrder(unit? whichUnit, bool clearQueue)
    {
        if (whichUnit is null) return;
        whichUnit.Order = 0;
        if (clearQueue) whichUnit.Queue.Clear();
    }

    // ---- What an order event reads

    Happening? TargetOrder => Response(UnitIssuedTargetOrder, PlayerUnitIssuedTargetOrder);
    Happening? AnyOrder => Response(UnitIssuedOrder, PlayerUnitIssuedOrder, UnitIssuedPointOrder, PlayerUnitIssuedPointOrder, UnitIssuedTargetOrder, PlayerUnitIssuedTargetOrder);

    [Native(Level.Close, Fact = OrderFacts)]
    public widget? GetOrderTarget() => TargetOrder?.Target;

    [Native(Level.Close, Fact = OrderFacts)]
    public unit? GetOrderTargetUnit() => TargetOrder?.Target as unit;

    [Native(Level.Close, Fact = OrderFacts)]
    public item? GetOrderTargetItem() => TargetOrder?.Target as item;

    [Native(Level.Close, Fact = OrderFacts)]
    public destructable? GetOrderTargetDestructable() => TargetOrder?.Target as destructable;

    /// <summary>The place of a point order, and of a target order (the target's place); 0 for an immediate one and outside an order event.</summary>
    [Native(Level.Close, Fact = OrderFacts)]
    public float GetOrderPointX() => AnyOrder is { } order && Current.Event.Id != UnitIssuedOrder && Current.Event.Id != PlayerUnitIssuedOrder ? order.PointX : 0;

    [Native(Level.Close, Fact = OrderFacts)]
    public float GetOrderPointY() => AnyOrder is { } order && Current.Event.Id != UnitIssuedOrder && Current.Event.Id != PlayerUnitIssuedOrder ? order.PointY : 0;

    [Native(Level.Close, Fact = OrderFacts)]
    public location? GetOrderPointLoc() => AnyOrder is null ? null : Location(GetOrderPointX(), GetOrderPointY());

    // ---- Rally points and waygates

    /// <summary>The place a 'setrally' order gave (0, 0 before one).</summary>
    [Native(Level.Close, Fact = OrderFacts)]
    public location? GetUnitRallyPoint(unit? whichUnit) => whichUnit is null ? null : Location(whichUnit.RallyX, whichUnit.RallyY);

    [Native(Level.Close, Fact = OrderFacts)]
    public unit? GetUnitRallyUnit(unit? whichUnit) => whichUnit?.RallyTarget as unit;

    [Native(Level.Close, Fact = OrderFacts)]
    public destructable? GetUnitRallyDestructable(unit? whichUnit) => whichUnit?.RallyTarget as destructable;

    [Native(Level.Close)]
    public void WaygateActivate(unit? waygate, bool activate)
    {
        if (waygate is not null) waygate.GateActive = activate;
    }

    [Native(Level.Close)]
    public bool WaygateIsActive(unit? waygate) => waygate?.GateActive ?? false;

    [Native(Level.Close)]
    public void WaygateSetDestination(unit? waygate, float x, float y)
    {
        if (waygate is not null) (waygate.GateX, waygate.GateY) = (x, y);
    }

    [Native(Level.Close)]
    public float WaygateGetDestinationX(unit? waygate) => waygate?.GateX ?? 0;

    [Native(Level.Close)]
    public float WaygateGetDestinationY(unit? waygate) => waygate?.GateY ?? 0;
}
