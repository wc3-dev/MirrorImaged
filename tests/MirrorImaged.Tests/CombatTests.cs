using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>Walking and fighting: the 1/32 s step, orders that move and attack, acquiring enemies, and the damage events of a real attack.</summary>
public sealed class CombatTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static StandardData Standard() => new StandardData()
        .Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=100;umvs=270;umvt=foot;uacq=500;uaen=1;ua1w=normal;ua1t=normal;ua1g=ground;ua1b=10;ua1d=0;ua1s=1;ua1c=1;ua1r=90;udp1=0.5;ucol=31;udef=0;udty=normal;urac=human"))
        .Add(ObjectKind.Unit, "hpea", Values("unam=Peasant;uhpm=50;umvs=190;umvt=foot;uaen=0;ucol=31;udef=0"));

    List<string> Run(string main, double seconds)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            local function f1(x) return string.format("%.1f", x) end
            local function make(id, p, x, y) return CreateUnit(p or Player(0), FourCC(id), x or 0, y or 0, 0) end
            function config() end
            function main()
            {{main}}
            end
            """), "", data: Standard());
        simulation.Advance(TimeSpan.FromSeconds(seconds));
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];
    }

    // a move order walks at the unit's speed in a straight line and ends on arrival
    [Fact]
    public void A_move_order_walks_at_the_units_speed_and_ends_on_arrival()
    {
        var lines = Run("""
            local u = make("hfoo")
            IssuePointOrder(u, "move", 270, 0)
            TimerStart(CreateTimer(), 0.5, false, function() print("half", f1(GetUnitX(u)), GetUnitCurrentOrder(u)) end)
            TimerStart(CreateTimer(), 1.5, false, function() print("done", f1(GetUnitX(u)), f1(GetUnitY(u)), GetUnitCurrentOrder(u)) end)
            """, 2);

        Assert.Equal(["half\t126.6\t851986", "done\t270.0\t0.0\t0"], lines);
    }

    // queued orders run one after another, each firing its event when it starts
    [Fact]
    public void A_queued_order_starts_when_the_one_before_it_ends()
    {
        var lines = Run("""
            local u = make("hfoo")
            local t = CreateTrigger()
            TriggerRegisterUnitEvent(t, u, EVENT_UNIT_ISSUED_POINT_ORDER)
            TriggerAddAction(t, function() print("order", GetOrderPointX(), GetOrderPointY()) end)
            IssuePointOrder(u, "move", 135, 0)
            BlzQueuePointOrderById(u, 851986, 135, 270)
            TimerStart(CreateTimer(), 3, false, function() print("end", f1(GetUnitX(u)), f1(GetUnitY(u)), GetUnitCurrentOrder(u)) end)
            """, 4);

        Assert.Equal(["order\t135.0\t0.0", "order\t135.0\t270.0", "end\t135.0\t270.0\t0"], lines);
    }

    // an attack order walks into range, swings, and the hit goes through the damage events as a real attack until the target dies
    [Fact]
    public void An_attack_order_hits_through_the_damage_events_until_the_target_dies()
    {
        var lines = Run("""
            local a, b = make("hfoo", Player(0), 0, 0), make("hpea", Player(1), 400, 0)
            local t = CreateTrigger()
            TriggerRegisterUnitEvent(t, b, EVENT_UNIT_DAMAGED)
            TriggerAddAction(t, function() print("hit", f1(GetEventDamage()), BlzGetEventIsAttack(), GetUnitName(GetEventDamageSource()), f1(GetWidgetLife(b))) end)
            local d = CreateTrigger()
            TriggerRegisterUnitEvent(d, b, EVENT_UNIT_DEATH)
            TriggerAddAction(d, function() print("died", GetUnitName(GetKillingUnit())) end)
            IssueTargetOrder(a, "attack", b)
            TimerStart(CreateTimer(), 10, false, function() print("end", f1(GetUnitX(a)) , GetUnitCurrentOrder(a), UnitAlive(b)) end)
            """, 11);

        Assert.Equal(["hit\t10.0\ttrue\tFootman\t50.0", "hit\t10.0\ttrue\tFootman\t40.0", "hit\t10.0\ttrue\tFootman\t30.0", "hit\t10.0\ttrue\tFootman\t20.0", "hit\t10.0\ttrue\tFootman\t10.0", "died\tFootman", "end\t286.9\t0\tfalse"], lines);
    }

    // idle units that may attack each other fight; allied and passive neutral ones don't
    [Fact]
    public void Idle_enemies_in_range_fight_and_allies_do_not()
    {
        var lines = Run("""
            local a, b = make("hfoo", Player(0), 0, 0), make("hfoo", Player(1), 300, 0)
            local ally, mate = make("hfoo", Player(2), 0, 2000), make("hfoo", Player(3), 300, 2000)
            SetPlayerAlliance(Player(2), Player(3), ALLIANCE_PASSIVE, true)
            SetPlayerAlliance(Player(3), Player(2), ALLIANCE_PASSIVE, true)
            TimerStart(CreateTimer(), 20, false, function()
                print("enemies", UnitAlive(a), UnitAlive(b), GetUnitCurrentOrder(a))
                print("allies", f1(GetWidgetLife(ally)), f1(GetWidgetLife(mate)))
            end)
            """, 21);

        Assert.Equal(2, lines.Count);
        Assert.Equal("allies\t100.0\t100.0", lines[1]);
        Assert.StartsWith("enemies\t", lines[0]);
        Assert.True(lines[0] == "enemies\ttrue\tfalse\t0" || lines[0] == "enemies\tfalse\ttrue\t0", lines[0]);
    }

    // an attack-move walks on after it has killed what it came upon
    [Fact]
    public void An_attack_move_fights_what_it_meets_and_walks_on()
    {
        var lines = Run("""
            local a, b = make("hfoo", Player(0), 0, 0), make("hpea", Player(1), 300, 0)
            IssuePointOrder(a, "attack", 700, 0)
            TimerStart(CreateTimer(), 12, false, function() print("end", f1(GetUnitX(a)), UnitAlive(b), GetUnitCurrentOrder(a)) end)
            """, 13);

        Assert.Equal(["end\t700.0\tfalse\t0"], lines);
    }
}
