using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>Orders given from code against the facts' "Orders and events" (game-facts.md, 10-21, 11-03, 11-05): what is current, the events, the queue.</summary>
public sealed class OrderTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static StandardData Standard() => new StandardData()
        .Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=420;umvs=270;uaen=1"))
        .Add(ObjectKind.Unit, "hpea", Values("unam=Peasant;uhpm=220;umvs=190;ubui=hbar,hhou"))
        .Add(ObjectKind.Unit, "hbar", Values("unam=Barracks;uhpm=1500;ubdg=1"))
        .Add(ObjectKind.Unit, "hhou", Values("unam=Farm;uhpm=500;ubdg=1"));

    List<string> Run(string main)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            local function foot(x, y) return CreateUnit(Player(0), FourCC("hfoo"), x or 0, y or 0, 0) end
            local function watch(u, kind)
                local t = CreateTrigger()
                TriggerRegisterUnitEvent(t, u, kind)
                TriggerAddAction(t, function()
                    print("event", GetIssuedOrderId(), GetUnitCurrentOrder(GetOrderedUnit()), GetOrderPointX(), GetOrderPointY(), GetOrderTargetUnit() ~= nil)
                end)
            end
            function config() end
            function main()
            {{main}}
            end
            """), "", data: Standard());
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];
    }

    // facts: an unknown order string answers false; an order is current at once and its event fires inside the call; 'stop' clears the order
    [Fact]
    public void An_order_is_current_at_once_and_fires_its_event_inside_the_call()
    {
        var lines = Run("""
            local u = foot()
            watch(u, EVENT_UNIT_ISSUED_POINT_ORDER)
            watch(u, EVENT_UNIT_ISSUED_ORDER)
            print(IssuePointOrder(u, "mvoe", 1, 2), GetUnitCurrentOrder(u))
            print(IssuePointOrder(u, "move", 100, 200), GetUnitCurrentOrder(u))
            print(IssueImmediateOrder(u, "stop"), GetUnitCurrentOrder(u))
            print(IssueImmediateOrderById(u, 851972), IssuePointOrderById(u, 851986, 5, 6), IssuePointOrderLoc(u, "move", Location(7, 8)))
            """);

        Assert.Equal([
            "false\t0",
            "event\t851986\t851986\t100.0\t200.0\tfalse", "true\t851986",
            "event\t851972\t0\t0.0\t0.0\tfalse", "true\t0",
            "event\t851972\t0\t0.0\t0.0\tfalse", "event\t851986\t851986\t5.0\t6.0\tfalse", "event\t851986\t851986\t7.0\t8.0\tfalse", "true\ttrue\ttrue",
        ], lines);
    }

    // facts: a target order's event has the target and its place; 'attack' on a dead unit answers true and 'move' does not; a removed one answers false
    [Fact]
    public void Target_orders_carry_their_target_and_dead_units_take_only_attack()
    {
        var lines = Run("""
            local u, v = foot(), foot(300, 400)
            watch(u, EVENT_UNIT_ISSUED_TARGET_ORDER)
            print(IssueTargetOrder(u, "attack", v), IssueTargetOrder(u, "attack", nil), GetUnitCurrentOrder(u))
            KillUnit(u)
            print(IssueTargetOrder(u, "move", v), IssueImmediateOrder(u, "move"), IssueTargetOrder(u, "attack", v))
            RemoveUnit(v)
            print(IssuePointOrder(v, "move", 0, 0))
            print(GetOrderTarget(), GetOrderPointX(), GetIssuedOrderId())
            """);

        Assert.Equal(["event\t851983\t851983\t300.0\t400.0\ttrue", "true\tfalse\t851983", "event\t851983\t851983\t300.0\t400.0\ttrue", "false\tfalse\ttrue", "false", "nil\t0.0\t0"], lines);
    }

    // facts: a queued order on an idle unit fires its event at once, on a busy one it waits and BlzGetUnitOrderCount counts it
    [Fact]
    public void Queued_orders_wait_behind_the_current_one()
    {
        var lines = Run("""
            local u = foot()
            watch(u, EVENT_UNIT_ISSUED_POINT_ORDER)
            print(BlzGetUnitOrderCount(u), BlzQueuePointOrderById(u, 851986, 1, 1), BlzGetUnitOrderCount(u))
            print(BlzQueuePointOrderById(u, 851986, 2, 2), BlzQueueImmediateOrderById(u, 851993), BlzGetUnitOrderCount(u), GetUnitCurrentOrder(u))
            BlzUnitClearOrders(u, true)
            print(BlzGetUnitOrderCount(u), GetUnitCurrentOrder(u))
            BlzQueueImmediateOrderById(u, 851993)
            BlzUnitForceStopOrder(u, true)
            print(BlzGetUnitOrderCount(u), GetUnitCurrentOrder(u))
            BlzQueuePointOrderById(u, 851986, 3, 3)
            BlzUnitClearOrders(u, false)
            print(BlzGetUnitOrderCount(u), GetUnitCurrentOrder(u))
            """);

        Assert.Equal([
            "event\t851986\t851986\t1.0\t1.0\tfalse", "0\ttrue\t1",
            "true\ttrue\t3\t851986",
            "1\t851986",
            "0\t0",
            "event\t851986\t851986\t3.0\t3.0\tfalse", "0\t0",
        ], lines);
    }

    // facts: build orders carry the building's id; the build list is the unit's data's; rally points and group orders
    [Fact]
    public void Build_orders_group_orders_and_rally_points()
    {
        var lines = Run("""
            local p, a, b = CreateUnit(Player(0), FourCC("hpea"), 0, 0, 0), foot(), foot()
            print(IssueBuildOrder(p, "hbar", 10, 10), GetUnitCurrentOrder(p) == FourCC("hbar"), IssueBuildOrderById(p, FourCC("hfoo"), 0, 0), IssueBuildOrder(p, "zzzz", 0, 0))
            local g = CreateGroup()
            GroupAddUnit(g, a)
            GroupAddUnit(g, b)
            print(GroupPointOrder(g, "move", 5, 5), GetUnitCurrentOrder(a), GetUnitCurrentOrder(b), GroupImmediateOrder(g, "nonsense"))
            IssuePointOrder(a, "setrally", 50, 60)
            print(GetLocationX(GetUnitRallyPoint(a)), GetLocationY(GetUnitRallyPoint(a)), GetUnitRallyUnit(a))
            local gate = foot()
            WaygateSetDestination(gate, 7, 8)
            WaygateActivate(gate, true)
            print(WaygateIsActive(gate), WaygateGetDestinationX(gate), WaygateGetDestinationY(gate))
            """);

        Assert.Equal(["true\ttrue\tfalse\tfalse", "true\t851986\t851986\tfalse", "50.0\t60.0\tnil", "true\t7.0\t8.0"], lines);
    }
}
