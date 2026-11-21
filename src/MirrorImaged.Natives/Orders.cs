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
    static readonly int RallyAbility = Code("ARal");

    static bool Takes(unit u, int order) => !u.Removed && (!u.Dead || order == 851983);

    // The order given: current at once, its event fired with the place or target, in the game's three kinds
    bool Give(unit? whichUnit, int order, float x, float y, widget? target, int kind)
    {
        if (whichUnit is null || order == 0 || !Takes(whichUnit, order)) return false;
        if (BeetlesOrder(whichUnit, ref order, ref x, ref y, ref target, ref kind) is { } answer) return answer;
        if (ImmolationOff(whichUnit, order) is { } off) return off;
        if (Spell(whichUnit, order, out var spell) && spell is null) return false;
        // A building that rallies takes smart as its rally point, the order answering false at a place and true at a unit or tree (probe units-harvest)
        if (order == Smart && Find(whichUnit, RallyAbility) is not null)
        {
            (whichUnit.RallyX, whichUnit.RallyY, whichUnit.RallyTarget) = (x, y, target);
            return target is not null;
        }
        whichUnit.Given++;
        BreakCast(whichUnit);
        (whichUnit.UsedItem, _itemToUse) = (_itemToUse, null);
        whichUnit.Casting = spell;
        EndChannel(whichUnit, false);
        if (CargoOrder(whichUnit, order, x, y, target, out var carried)) return carried;
        if (Economy(whichUnit, order, kind, out var taken))
        {
            if (taken) FireUnit(new(whichUnit, Order: order, PointX: x, PointY: y, Target: target), UnitIssuedOrder, PlayerUnitIssuedOrder);
            return taken;
        }
        // A worker given another order leaves the building it is making, which goes on without it
        if (whichUnit.Building is { } site) (site.Builder, whichUnit.Building) = (null, null);
        (whichUnit.Order, whichUnit.OrderX, whichUnit.OrderY, whichUnit.OrderTarget, whichUnit.AttackTarget) = (order == Stop ? 0 : order, x, y, target, null);
        if (order == 851993) (whichUnit.PatrolX, whichUnit.PatrolY) = (whichUnit.X, whichUnit.Y);
        if (order == 851990) (whichUnit.PatrolX, whichUnit.PatrolY) = (whichUnit.X, whichUnit.Y);
        Wake();
        if (order == SetRally) (whichUnit.RallyX, whichUnit.RallyY, whichUnit.RallyTarget) = (x, y, target);
        var (unitKind, playerKind) = kind switch { 0 => (UnitIssuedOrder, PlayerUnitIssuedOrder), 1 => (UnitIssuedPointOrder, PlayerUnitIssuedPointOrder), _ => (UnitIssuedTargetOrder, PlayerUnitIssuedTargetOrder) };
        FireUnit(new(whichUnit, Order: order, PointX: x, PointY: y, Target: target), unitKind, playerKind);
        // A cast begins inside the order's call when the unit is in range (probe abilities-cast-timing: the channel and cast events fired in it)
        if (whichUnit.Casting == spell && spell is not null && !whichUnit.Paused) Cast(whichUnit, walk: false);
        return true;
    }

    int Named(string? order) => order is null ? 0 : OrderIds.GetValueOrDefault(order.ToLowerInvariant());

    /// <summary>Gives the unit an order with no target, true when it takes it; false for a null, removed or dead unit (an attack excepted), id 0 or an order it refuses.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); the order stays current until another replaces it; what it makes the unit do is only partly modelled.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueImmediateOrderById(unit? whichUnit, int order) => Give(whichUnit, order, 0, 0, null, 0);

    /// <summary>As the id form, with the order's name; an unknown name answers false.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); the order stays current until another replaces it; what it makes the unit do is only partly modelled.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueImmediateOrder(unit? whichUnit, string? order) => Give(whichUnit, Named(order), 0, 0, null, 0);

    /// <summary>Gives the unit an order at a point, true when it takes it; the order's event carries the place.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); the order stays current until another replaces it; what it makes the unit do is only partly modelled.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssuePointOrderById(unit? whichUnit, int order, float x, float y) => Give(whichUnit, order, x, y, null, 1);

    /// <summary>As the id form, with the order's name; an unknown name answers false.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); the order stays current until another replaces it; what it makes the unit do is only partly modelled.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssuePointOrder(unit? whichUnit, string? order, float x, float y) => Give(whichUnit, Named(order), x, y, null, 1);

    /// <summary>As the point form, at a location's place; false for a null location.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); the order stays current until another replaces it; what it makes the unit do is only partly modelled.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssuePointOrderByIdLoc(unit? whichUnit, int order, location? whichLocation) => whichLocation is not null && IssuePointOrderById(whichUnit, order, whichLocation.X, whichLocation.Y);

    /// <summary>As the named point form, at a location's place; false for a null location.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); the order stays current until another replaces it; what it makes the unit do is only partly modelled.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssuePointOrderLoc(unit? whichUnit, string? order, location? whichLocation) => whichLocation is not null && IssuePointOrder(whichUnit, order, whichLocation.X, whichLocation.Y);

    /// <summary>Gives the unit an order on a widget, the order's place being the widget's; false for a null target.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); the order stays current until another replaces it; what it makes the unit do is only partly modelled.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueTargetOrderById(unit? whichUnit, int order, widget? targetWidget) =>
        targetWidget is not null && Give(whichUnit, order, targetWidget is unit u ? u.X : GetWidgetX(targetWidget), targetWidget is unit v ? v.Y : GetWidgetY(targetWidget), targetWidget, 2);

    /// <summary>As the id form, with the order's name; an unknown name answers false.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); the order stays current until another replaces it; what it makes the unit do is only partly modelled.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueTargetOrder(unit? whichUnit, string? order, widget? targetWidget) => IssueTargetOrderById(whichUnit, Named(order), targetWidget);

    /// <summary>An instant order: given as the plain one is (the cast's instant nature comes with abilities, wave 4).</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); the instant target is ignored, and the order's instant nature is not modelled; the order stays current until another replaces it; what it makes the unit do is only partly modelled.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueInstantPointOrderById(unit? whichUnit, int order, float x, float y, widget? instantTargetWidget) => IssuePointOrderById(whichUnit, order, x, y);

    /// <summary>As <c>IssuePointOrder</c>; the instant target is ignored.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); the instant target is ignored, and the order's instant nature is not modelled.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueInstantPointOrder(unit? whichUnit, string? order, float x, float y, widget? instantTargetWidget) => IssuePointOrder(whichUnit, order, x, y);

    /// <summary>As <c>IssueTargetOrderById</c>; the instant target is ignored.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); the instant target is ignored, and the order's instant nature is not modelled.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueInstantTargetOrderById(unit? whichUnit, int order, widget? targetWidget, widget? instantTargetWidget) => IssueTargetOrderById(whichUnit, order, targetWidget);

    /// <summary>As <c>IssueTargetOrder</c>; the instant target is ignored.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); the instant target is ignored, and the order's instant nature is not modelled.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueInstantTargetOrder(unit? whichUnit, string? order, widget? targetWidget, widget? instantTargetWidget) => IssueTargetOrder(whichUnit, order, targetWidget);

    /// <summary>A build order's id is the building's type; false unless the unit's data lists it (the abilities' build menus come later).</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); whether the game's own build checks (place, cost) all apply here is not measured.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueBuildOrderById(unit? whichPeon, int unitId, float x, float y) =>
        whichPeon is not null && Data.Object(ObjectKind.Unit, unitId) is not null && Builds(whichPeon, unitId) && Give(whichPeon, unitId, x, y, null, 1);

    /// <summary>As the id form, with the building's four-character code as text; any other length answers false.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); as the id form; the game's own build checks are not all measured here.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueBuildOrder(unit? whichPeon, string? unitToBuild, float x, float y) =>
        unitToBuild is { Length: 4 } code && IssueBuildOrderById(whichPeon, Code(code), x, y);

    // The unit's data lists the type among what it builds (ubui), or it is a worker's type without a list
    bool Builds(unit u, int type) => (u.Type.Text(Code("ubui")) ?? "").Split(',', StringSplitOptions.TrimEntries).Contains(Name(type));

    // A neutral shop answers false for an item or a unit it does not sell (probe items-stock, D796); a player's own building takes a unit's id as its train order
    bool Refuses(unit? shop, int id) => shop is { Owner.Number: >= NeutralAggressive } && (Data.Object(ObjectKind.Item, id) is not null || Data.Object(ObjectKind.Unit, id) is not null);

    // Neutral orders go to a unit the neutral player doesn't own, as the shops take them
    /// <summary>Orders a neutral building for a player: an id the building sells is bought for the player, any other is given as an order; false for a null player.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); a neutral shop's refusal of an item or unit it does not sell is measured (D796); other neutral orders are not.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueNeutralImmediateOrderById(player? forWhichPlayer, unit? neutralStructure, int unitId) =>
        forWhichPlayer is not null && (neutralStructure is not null && Sells(neutralStructure, unitId) ? Buy(forWhichPlayer, neutralStructure, unitId) : !Refuses(neutralStructure, unitId) && Give(neutralStructure, unitId, 0, 0, null, 0));

    /// <summary>As the id form, with the four-character code as text; any other length answers false.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); a neutral shop's refusal of an item or unit it does not sell is measured (D796); other neutral orders are not.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueNeutralImmediateOrder(player? forWhichPlayer, unit? neutralStructure, string? unitToBuild) => unitToBuild is { Length: 4 } code && IssueNeutralImmediateOrderById(forWhichPlayer, neutralStructure, Code(code));

    /// <summary>Gives the neutral building an order at a point; false for a null player.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); a neutral shop's refusal of an item or unit it does not sell is measured (D796); other neutral orders are not.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueNeutralPointOrderById(player? forWhichPlayer, unit? neutralStructure, int unitId, float x, float y) => forWhichPlayer is not null && Give(neutralStructure, unitId, x, y, null, 1);

    /// <summary>As the id form, with the four-character code as text; any other length answers false.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); a neutral shop's refusal of an item or unit it does not sell is measured (D796); other neutral orders are not.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueNeutralPointOrder(player? forWhichPlayer, unit? neutralStructure, string? unitToBuild, float x, float y) => unitToBuild is { Length: 4 } code && IssueNeutralPointOrderById(forWhichPlayer, neutralStructure, Code(code), x, y);

    /// <summary>Gives the neutral building an order on a widget; false for a null player or target.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); a neutral shop's refusal of an item or unit it does not sell is measured (D796); other neutral orders are not.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueNeutralTargetOrderById(player? forWhichPlayer, unit? neutralStructure, int unitId, widget? target) => forWhichPlayer is not null && target is not null && IssueTargetOrderById(neutralStructure, unitId, target);

    /// <summary>As the id form, with the four-character code as text; any other length answers false.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); a neutral shop's refusal of an item or unit it does not sell is measured (D796); other neutral orders are not.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool IssueNeutralTargetOrder(player? forWhichPlayer, unit? neutralStructure, string? unitToBuild, widget? target) => unitToBuild is { Length: 4 } code && IssueNeutralTargetOrderById(forWhichPlayer, neutralStructure, Code(code), target);

    // ---- Group orders: each unit of the group takes the order; true when every one did

    bool EachOf(group? whichGroup, Func<unit, bool> order) => whichGroup is not null && whichGroup.Units.ToArray().Aggregate(true, (all, u) => (Live(u) is { } alive && order(alive)) & all);

    /// <summary>Gives every unit of the group the order; true only when all took it, false for a null group.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); a group order is one order per unit, so it is as close as they are; the game's own group-order handling is not measured.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool GroupImmediateOrderById(group? whichGroup, int order) => EachOf(whichGroup, u => IssueImmediateOrderById(u, order));

    /// <summary>As the id form, with the order's name.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); a group order is one order per unit, so it is as close as they are; the game's own group-order handling is not measured.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool GroupImmediateOrder(group? whichGroup, string? order) => EachOf(whichGroup, u => IssueImmediateOrder(u, order));

    /// <summary>Gives every unit of the group an order at a point; true only when all took it, false for a null group.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); a group order is one order per unit, so it is as close as they are; the game's own group-order handling is not measured.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool GroupPointOrderById(group? whichGroup, int order, float x, float y) => EachOf(whichGroup, u => IssuePointOrderById(u, order, x, y));

    /// <summary>As the id form, with the order's name.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); a group order is one order per unit, so it is as close as they are; the game's own group-order handling is not measured.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool GroupPointOrder(group? whichGroup, string? order, float x, float y) => EachOf(whichGroup, u => IssuePointOrder(u, order, x, y));

    /// <summary>As the point form, at a location's place; false for a null location.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); a group order is one order per unit, so it is as close as they are; the game's own group-order handling is not measured.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool GroupPointOrderByIdLoc(group? whichGroup, int order, location? whichLocation) => whichLocation is not null && GroupPointOrderById(whichGroup, order, whichLocation.X, whichLocation.Y);

    /// <summary>As the named point form, at a location's place; false for a null location.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); a group order is one order per unit, so it is as close as they are; the game's own group-order handling is not measured.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool GroupPointOrderLoc(group? whichGroup, string? order, location? whichLocation) => whichLocation is not null && GroupPointOrder(whichGroup, order, whichLocation.X, whichLocation.Y);

    /// <summary>Gives every unit of the group an order on a widget; true only when all took it, false for a null group.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); a group order is one order per unit, so it is as close as they are; the game's own group-order handling is not measured.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool GroupTargetOrderById(group? whichGroup, int order, widget? targetWidget) => EachOf(whichGroup, u => IssueTargetOrderById(u, order, targetWidget));

    /// <summary>As the id form, with the order's name.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); a group order is one order per unit, so it is as close as they are; the game's own group-order handling is not measured.</remarks>
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

    /// <summary>Gives the order at once to an idle unit, else adds it to the unit's queue; false for a null, removed or dead unit or id 0.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); queueing is a model of the game's; when a queued order starts is not measured.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool BlzQueueImmediateOrderById(unit? whichUnit, int order) => Queued(whichUnit, order, 0, 0, null, 0);

    /// <summary>As the immediate queue order, at a point.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); queueing is a model of the game's; when a queued order starts is not measured.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool BlzQueuePointOrderById(unit? whichUnit, int order, float x, float y) => Queued(whichUnit, order, x, y, null, 1);

    /// <summary>As the immediate queue order, on a widget; false for a null target.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); queueing is a model of the game's; when a queued order starts is not measured.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool BlzQueueTargetOrderById(unit? whichUnit, int order, widget? targetWidget) => targetWidget is not null && Queued(whichUnit, order, GetWidgetX(targetWidget), GetWidgetY(targetWidget), targetWidget, 2);

    /// <summary>As <c>BlzQueuePointOrderById</c>; the instant target is ignored.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); the instant target is ignored; queueing is a model of the game's; when a queued order starts is not measured.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool BlzQueueInstantPointOrderById(unit? whichUnit, int order, float x, float y, widget? instantTargetWidget) => BlzQueuePointOrderById(whichUnit, order, x, y);

    /// <summary>As <c>BlzQueueTargetOrderById</c>; the instant target is ignored.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); the instant target is ignored; queueing is a model of the game's; when a queued order starts is not measured.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool BlzQueueInstantTargetOrderById(unit? whichUnit, int order, widget? targetWidget, widget? instantTargetWidget) => BlzQueueTargetOrderById(whichUnit, order, targetWidget);

    /// <summary>Queues a build order; false unless the unit's data lists the building.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); queueing is a model of the game's; when a queued order starts is not measured.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool BlzQueueBuildOrderById(unit? whichPeon, int unitId, float x, float y) =>
        whichPeon is not null && Data.Object(ObjectKind.Unit, unitId) is not null && Builds(whichPeon, unitId) && Queued(whichPeon, unitId, x, y, null, 1);

    /// <summary>As the immediate queue order on a neutral building; false for a null player.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); queueing is a model of the game's; when a queued order starts is not measured.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool BlzQueueNeutralImmediateOrderById(player? forWhichPlayer, unit? neutralStructure, int unitId) => forWhichPlayer is not null && Queued(neutralStructure, unitId, 0, 0, null, 0);

    /// <summary>As the point queue order on a neutral building; false for a null player.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); queueing is a model of the game's; when a queued order starts is not measured.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool BlzQueueNeutralPointOrderById(player? forWhichPlayer, unit? neutralStructure, int unitId, float x, float y) => forWhichPlayer is not null && Queued(neutralStructure, unitId, x, y, null, 1);

    /// <summary>As the target queue order on a neutral building; false for a null player or target.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); queueing is a model of the game's; when a queued order starts is not measured.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public bool BlzQueueNeutralTargetOrderById(player? forWhichPlayer, unit? neutralStructure, int unitId, widget? target) => forWhichPlayer is not null && target is not null && Queued(neutralStructure, unitId, GetWidgetX(target), GetWidgetY(target), target, 2);

    /// <summary>The current order's id, 0 when idle ('stop' and a never-ordered unit read 0).</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); an order stays current until another replaces it, so one that would have finished in the game still reads.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public int GetUnitCurrentOrder(unit? whichUnit) => whichUnit?.Order ?? 0;

    /// <summary>The current order and those queued after it; the stunned order of a pause counts for one in its own event (facts, 11-05).</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); the stunned order counts for one only in its own event (11-05); the count at other moments is the model's.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public int BlzGetUnitOrderCount(unit? whichUnit) => whichUnit is null ? 0 : Math.Max(whichUnit.Order != 0 ? 1 : 0, Response(UnitIssuedPointOrder, PlayerUnitIssuedPointOrder)?.Unit == whichUnit && Response(UnitIssuedPointOrder, PlayerUnitIssuedPointOrder)?.Order == Stunned ? 1 : 0) + whichUnit.Queue.Count;

    /// <summary>Empties the unit's queue, and also ends the current order unless <paramref name="onlyQueued"/>.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); the current order is cleared with no event fired; the game's effect on a running order is not measured.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public void BlzUnitClearOrders(unit? whichUnit, bool onlyQueued)
    {
        if (whichUnit is null) return;
        whichUnit.Queue.Clear();
        if (!onlyQueued) whichUnit.Order = 0;
    }

    /// <summary>Ends the current order, and empties the queue when <paramref name="clearQueue"/>.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); the current order is cleared with no event fired; the game's effect on a running order is not measured.</remarks>
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

    /// <summary>The widget a target order was given on, in an order event; null in any other.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); null outside a target-order event is the model's answer, not measured in the game.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public widget? GetOrderTarget() => TargetOrder?.Target;

    /// <summary>The order's target when it is a unit, else null.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); as <c>GetOrderTarget</c>, a target of another kind answering null; not measured.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public unit? GetOrderTargetUnit() => TargetOrder?.Target as unit;

    /// <summary>The order's target when it is an item, else null.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); as <c>GetOrderTarget</c>, a target of another kind answering null; not measured.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public item? GetOrderTargetItem() => TargetOrder?.Target as item;

    /// <summary>The order's target when it is a destructable, else null.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); as <c>GetOrderTarget</c>, a target of another kind answering null; not measured.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public destructable? GetOrderTargetDestructable() => TargetOrder?.Target as destructable;

    /// <summary>The place of a point order, and of a target order (the target's place); 0 for an immediate one and outside an order event.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); the 0 for an immediate order and outside an order event is not measured in every case.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public float GetOrderPointX() => AnyOrder is { } order && Current.Event.Id != UnitIssuedOrder && Current.Event.Id != PlayerUnitIssuedOrder ? order.PointX : 0;

    /// <summary>As <c>GetOrderPointX</c>, the y.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); the 0 for an immediate order and outside an order event is not measured in every case.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public float GetOrderPointY() => AnyOrder is { } order && Current.Event.Id != UnitIssuedOrder && Current.Event.Id != PlayerUnitIssuedOrder ? order.PointY : 0;

    /// <summary>A location at the order's place, null outside an order event.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); the place is as <c>GetOrderPointX</c> and <c>GetOrderPointY</c> give it; null outside an order event is not measured.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public location? GetOrderPointLoc() => AnyOrder is null ? null : Location(GetOrderPointX(), GetOrderPointY());

    // ---- Rally points and waygates

    /// <summary>The place a 'setrally' order gave (0, 0 before one).</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); kept on any unit, whether or not it can rally; the game's answer for one that cannot is not measured.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public location? GetUnitRallyPoint(unit? whichUnit) => whichUnit is null ? null : Location(whichUnit.RallyX, whichUnit.RallyY);

    /// <summary>The unit a rally order targeted; null when the rally was a place or another kind of target.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); the rally target is kept as given; the game's answer once the target is removed is not measured.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public unit? GetUnitRallyUnit(unit? whichUnit) => whichUnit?.RallyTarget as unit;

    /// <summary>The destructable a rally order targeted; null when the rally was a place or another kind of target.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events (10-21, 11-03, 11-05)); the rally target is kept as given; the game's answer once the target is removed is not measured.</remarks>
    [Native(Level.Close, Fact = OrderFacts)]
    public destructable? GetUnitRallyDestructable(unit? whichUnit) => whichUnit?.RallyTarget as destructable;

    /// <summary>Switches the waygate on or off; nothing for a null unit.</summary>
    /// <remarks>Close: the value is kept on the unit and read back; the game's own waygate use is not modelled or measured.</remarks>
    [Native(Level.Close)]
    public void WaygateActivate(unit? waygate, bool activate)
    {
        if (waygate is not null) waygate.GateActive = activate;
    }

    /// <summary>Whether the waygate was switched on; false for a null unit and before any call.</summary>
    /// <remarks>Close: the value is kept on the unit and read back; the game's own waygate use is not modelled or measured.</remarks>
    [Native(Level.Close)]
    public bool WaygateIsActive(unit? waygate) => waygate?.GateActive ?? false;

    /// <summary>Keeps the destination for the getters; nothing moves through it.</summary>
    /// <remarks>Close: the value is kept on the unit and read back; the game's own waygate use is not modelled or measured.</remarks>
    [Native(Level.Close)]
    public void WaygateSetDestination(unit? waygate, float x, float y)
    {
        if (waygate is not null) (waygate.GateX, waygate.GateY) = (x, y);
    }

    /// <summary>The destination's x; 0 for a null unit and before one is set.</summary>
    /// <remarks>Close: the value is kept on the unit and read back; the game's own waygate use is not modelled or measured.</remarks>
    [Native(Level.Close)]
    public float WaygateGetDestinationX(unit? waygate) => waygate?.GateX ?? 0;

    /// <summary>The destination's y; 0 for a null unit and before one is set.</summary>
    /// <remarks>Close: the value is kept on the unit and read back; the game's own waygate use is not modelled or measured.</remarks>
    [Native(Level.Close)]
    public float WaygateGetDestinationY(unit? waygate) => waygate?.GateY ?? 0;
}
