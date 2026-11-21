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

    // A footman of 1 s cooldown and 0.5 s damage point attacks a peasant that doesn't fight back; the call is made the delay after its first attacked event or
    // its first hit (probe units-resets). Gives the times, after the order, of its first two attacked events and first two hits.
    double[] Swings(string call, bool afterHit, double delay)
    {
        var lines = Run($$"""
            local a, b = make("hfoo", Player(0), 0, 0), make("hpea", Player(1), 60, 0)
            BlzSetUnitMaxHP(b, 5000)
            SetWidgetLife(b, 5000)
            local c = CreateTimer()
            TimerStart(c, 1000, false, nil)
            local attacked, hit = {}, {}
            local function later() TimerStart(CreateTimer(), {{delay}}, false, function() {{call}} end) end
            local t = CreateTrigger()
            TriggerRegisterUnitEvent(t, b, EVENT_UNIT_ATTACKED)
            TriggerAddAction(t, function()
                attacked[#attacked + 1] = TimerGetElapsed(c)
                if #attacked == 1 and not {{(afterHit ? "true" : "false")}} then later() end
            end)
            t = CreateTrigger()
            TriggerRegisterUnitEvent(t, b, EVENT_UNIT_DAMAGED)
            TriggerAddAction(t, function()
                hit[#hit + 1] = TimerGetElapsed(c)
                if #hit == 1 and {{(afterHit ? "true" : "false")}} then later() end
            end)
            IssueTargetOrder(a, "attack", b)
            TimerStart(CreateTimer(), 3, false, function() print(attacked[1], attacked[2], hit[1], hit[2]) end)
            """, 4);
        return [.. lines.Single().Split((char)9).Select(double.Parse)];
    }

    [Fact]
    public void Interrupting_an_attack_before_its_damage_point_drops_that_hit_and_the_unit_swings_again_at_once()
    {
        var plain = Swings("", false, 0.2);
        var swing = Swings("BlzUnitInterruptAttack(a)", false, 0.2);

        Assert.InRange(plain[2] - plain[0], 0.49, 0.56);
        Assert.InRange(swing[1] - swing[0], 0.2, 0.4);
        Assert.InRange(swing[2] - swing[1], 0.49, 0.56);
    }

    [Fact]
    public void Interrupting_an_attack_after_its_hit_leaves_the_cooldown_running()
    {
        var swing = Swings("BlzUnitInterruptAttack(a)", true, 0.2);

        Assert.InRange(swing[1] - swing[0], 0.95, 1.1);
    }

    [Fact]
    public void A_unit_interrupted_just_after_its_attack_order_goes_on_to_attack_the_foe_it_acquires()
    {
        var lines = Run("""
            local a, b = make("hfoo", Player(0), 0, 0), make("hfoo", Player(PLAYER_NEUTRAL_AGGRESSIVE), 100, 0)
            UnitRemoveAbility(b, FourCC("Aatk"))
            SetUnitPropWindow(b, 0)
            BlzSetUnitMaxHP(b, 5000)
            SetWidgetLife(b, 5000)
            IssueTargetOrder(a, "attack", b)
            TimerStart(CreateTimer(), 0.01, false, function() BlzUnitInterruptAttack(a) end)
            TimerStart(CreateTimer(), 3, false, function() print(GetWidgetLife(b) < 5000, GetUnitCurrentOrder(a)) end)
            """, 4);

        Assert.Equal(["true\t0"], lines);
    }

    [Fact]
    public void Resetting_an_attack_drops_a_swing_under_way_and_starts_the_next_at_once_whatever_the_cooldown()
    {
        var during = Swings("BlzResetUnitAttack(a, 0)", false, 0.1);
        var cooling = Swings("BlzResetUnitAttack(a, 0)", true, 0.2);

        Assert.InRange(during[1] - during[0], 0.08, 0.2);
        Assert.InRange(during[2] - during[1], 0.49, 0.56);
        Assert.InRange(cooling[1] - cooling[2], 0.15, 0.3);
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

    // the attacked event fires at each swing for the target, naming the attacker; the acquired-target event for the unit that takes a target;
    // a unit hit from beyond its acquisition range hits back
    [Fact]
    public void Attacks_and_acquisitions_fire_their_events_and_the_hit_unit_hits_back()
    {
        var lines = Run("""
            local a, b = make("hfoo", Player(0), 0, 0), make("hfoo", Player(1), 2000, 0)
            local hit = CreateTrigger()
            TriggerRegisterUnitEvent(hit, b, EVENT_UNIT_ATTACKED)
            TriggerAddAction(hit, function() print("attacked", GetUnitName(GetTriggerUnit()), GetUnitX(GetAttacker()), GetAttacker() == a) end)
            local acq = CreateTrigger()
            TriggerRegisterUnitEvent(acq, b, EVENT_UNIT_ACQUIRED_TARGET)
            TriggerAddAction(acq, function() print("acquired", GetEventTargetUnit() == a, GetUnitCurrentOrder(b)) end)
            IssueTargetOrder(a, "attack", b)
            """, 40);

        Assert.True(lines.Count >= 3, string.Join("|", lines));
        Assert.Contains(lines, line => line.StartsWith("attacked	Footman	"));
        Assert.Contains("acquired	true	0", lines);
    }
}
