using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>
/// The engine's timing as probes abilities-cast-timing, units-attack-timing and heroes-mana-regeneration measured it: a cast's events by cast point,
/// backswing and casting time, a hero's attack rate by its agility, a missile's flight, the mana that comes back, and a paused unit's buff.
/// </summary>
public sealed class EngineTimingTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static Field Column(string column, string type, params string[] specific) => new(column, PerLevel.Column, -1, type, false, [.. specific.Select(Ids.Of)]);

    // An Archmage's cast point and backswing, a Paladin's and an archer's attack, a Priest's mana, and the three spells with the game's numbers
    static StandardData Standard()
    {
        var data = new StandardData();
        foreach (var (id, field) in new (string, Field)[]
        {
            ("alev", new("levels", PerLevel.None, -1, "int")), ("code", new("code", PerLevel.None, -1)), ("aord", new("Order", PerLevel.None, -1, "string")),
            ("amsp", new("Missilespeed", PerLevel.None, -1, "int")),
            ("acas", Column("Cast", "unreal")), ("aran", Column("Rng", "unreal")), ("aare", Column("Area", "unreal")), ("acdn", Column("Cool", "unreal")), ("amcs", Column("Cost", "int")),
            ("adur", Column("Dur", "unreal")), ("ahdu", Column("HeroDur", "unreal")), ("Hhb1", Column("DataA", "unreal", "AHhb")), ("Htb1", Column("DataA", "unreal", "AHtb")),
            ("Slo1", Column("DataA", "unreal", "Aslo")), ("Slo2", Column("DataB", "unreal", "Aslo")),
            ("Hfs1", Column("DataA", "unreal", "AHfs")), ("Hfs2", Column("DataB", "unreal", "AHfs")), ("Hfs3", Column("DataC", "unreal", "AHfs")), ("Hfs4", Column("DataD", "unreal", "AHfs")),
        })
            data.Add(ObjectKind.Ability, id, field);
        return data
            .Add(ObjectKind.Ability, "AHhb", Values("code=AHhb;alev=1;aord=holybolt;acas:1=0;aran:1=800;aare:1=0;acdn:1=5;amcs:1=65;Hhb1:1=200"))
            .Add(ObjectKind.Ability, "AHtb", Values("code=AHtb;alev=1;aord=thunderbolt;amsp=1000;acas:1=0;aran:1=600;aare:1=0;acdn:1=9;amcs:1=75;Htb1:1=100;adur:1=5;ahdu:1=3"))
            .Add(ObjectKind.Ability, "Aslo", Values("code=Aslo;alev=1;aord=slow;acas:1=0;aran:1=600;aare:1=0;acdn:1=1;amcs:1=10;Slo1:1=0.6;Slo2:1=0.25;adur:1=3;ahdu:1=3"))
            .Add(ObjectKind.Ability, "AHfs", Values("code=AHfs;alev=1;aord=flamestrike;acas:1=1.33;aran:1=800;aare:1=200;acdn:1=10;amcs:1=135;Hfs1:1=15;Hfs2:1=0.33;Hfs3:1=4;Hfs4:1=1;adur:1=9;ahdu:1=2.67"))
            .Add(ObjectKind.Unit, "Hamg", Values("unam=Archmage;uhpm=450;umpm=1000;umpi=0;ustr=14;uagi=17;uint=19;umvs=320;umvt=foot;uaen=0;ucol=32;udef=0;ucpt=0.3;ucbs=2.4;umpr=0.01"))
            .Add(ObjectKind.Unit, "Hpal", Values("unam=Paladin;uhpm=650;umpm=0;ustr=22;uagi=13;uint=17;umvs=270;umvt=foot;uacq=600;uaen=1;ua1w=normal;ua1t=hero;ua1g=ground;ua1b=10;ua1d=0;ua1s=1;ua1c=2.2;ua1r=100;udp1=0.433;ucol=32;udef=0"))
            .Add(ObjectKind.Unit, "earc", Values("unam=Archer;uhpm=245;umvs=270;umvt=foot;uacq=700;uaen=1;ua1w=missile;ua1z=900;ulpy=62;ua1t=pierce;ua1g=ground;ua1b=10;ua1d=0;ua1s=1;ua1c=1.5;ua1r=700;udp1=0.72;ucol=16;udef=0"))
            .Add(ObjectKind.Unit, "hmpr", Values("unam=Priest;uhpm=290;umpm=200;umpi=0;umpr=0.67;umvs=270;umvt=foot;uaen=0;ucol=16;udef=0;ucpt=0.5;ucbs=0.67"))
            .Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=50000;umvs=0;umvt=foot;uaen=0;ucol=31;udef=0"));
    }

    List<string> Run(string main, double seconds)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            local clock = CreateTimer()
            local function now() return string.format("%.2f", TimerGetElapsed(clock)) end
            local function at(s, f) TimerStart(CreateTimer(), s, false, f) end
            local function make(id, p, x, y) return CreateUnit(Player(p or 0), FourCC(id), x or 0, y or 0, 0) end
            local function mana(u) return string.format("%.2f", GetUnitState(u, UNIT_STATE_MANA)) end
            -- A caster with the ability and 500 mana, none of which comes back to a hero (intelligence 0), its cast's events printed with their times
            local function caster(id, ability, x, y)
                local c = make(id, 0, x, y)
                if IsUnitType(c, UNIT_TYPE_HERO) then SetHeroInt(c, 0, true) end
                UnitAddAbility(c, FourCC(ability))
                SetUnitState(c, UNIT_STATE_MANA, 500)
                for name, event in pairs({ channel = EVENT_UNIT_SPELL_CHANNEL, cast = EVENT_UNIT_SPELL_CAST, effect = EVENT_UNIT_SPELL_EFFECT, finish = EVENT_UNIT_SPELL_FINISH, endcast = EVENT_UNIT_SPELL_ENDCAST }) do
                    local t = CreateTrigger()
                    TriggerRegisterUnitEvent(t, c, event)
                    TriggerAddAction(t, function() print(name, now()) end)
                end
                return c
            end
            -- The times of a unit's hits, and of its attacked events
            local function hits(u)
                local h, a = {}, {}
                local t = CreateTrigger()
                TriggerRegisterUnitEvent(t, u, EVENT_UNIT_DAMAGED)
                TriggerAddAction(t, function() h[#h + 1] = TimerGetElapsed(clock) end)
                t = CreateTrigger()
                TriggerRegisterUnitEvent(t, u, EVENT_UNIT_ATTACKED)
                TriggerAddAction(t, function() a[#a + 1] = TimerGetElapsed(clock) end)
                return h, a
            end
            function config() end
            function main()
            TimerStart(clock, 1000, false, nil)
            {{main}}
            end
            """), "", data: Standard());
        simulation.Advance(TimeSpan.FromSeconds(seconds));
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];
    }

    // The game's Archmage (cast point 0.30, backswing 2.40): channel and cast in the order's call, effect +0.30, finish and end cast +2.70
    [Fact]
    public void A_cast_begins_in_the_orders_call_takes_effect_at_the_cast_point_and_ends_after_the_backswing()
    {
        var lines = Run("""
            local c, ally = caster("Hamg", "AHhb"), make("hfoo", 0, 300, 0)
            SetWidgetLife(ally, 100)
            print("ordered", IssueTargetOrder(c, "holybolt", ally))
            at(0.29, function() print("before", mana(c), GetWidgetLife(ally)) end)
            at(0.31, function() print("after", mana(c), GetWidgetLife(ally), GetUnitCurrentOrder(c) ~= 0) end)
            at(3, function() print("idle", GetUnitCurrentOrder(c)) end)
            """, 4);

        Assert.Equal(["channel\t0.00", "cast\t0.00", "ordered\ttrue", "before\t500.00\t100.0", "effect\t0.30", "after\t435.00\t300.0\ttrue", "finish\t2.70", "endcast\t2.70", "idle\t0"], lines);
    }

    // Set to 0, the cast point puts the effect in the order's call too; a Priest's own 0.50 and 0.67 give +0.50 and +1.17
    [Fact]
    public void The_cast_point_and_backswing_are_the_units_own()
    {
        var lines = Run("""
            local c, ally = caster("Hamg", "AHhb"), make("hfoo", 0, 300, 0)
            BlzSetUnitRealField(c, UNIT_RF_CAST_POINT, 0)
            BlzSetUnitRealField(c, UNIT_RF_CAST_BACK_SWING, 0.5)
            print("ordered", IssueTargetOrder(c, "holybolt", ally))
            at(5, function()
                local p = caster("hmpr", "AHhb", 0, 1000)
                IssueTargetOrder(p, "holybolt", make("hfoo", 0, 300, 1000))
            end)
            """, 8);

        Assert.Equal(["channel\t0.00", "cast\t0.00", "effect\t0.00", "ordered\ttrue", "finish\t0.50", "endcast\t0.50",
            "channel\t5.00", "cast\t5.00", "effect\t5.50", "finish\t6.17", "endcast\t6.17"], lines);
    }

    // Stopped 0.15 s in: end cast alone, no mana spent, no cooldown. Stopped 1 s in: the effect, then end cast at the stop with no finish
    [Fact]
    public void Another_order_ends_a_cast_with_its_end_cast_event_and_before_the_cast_point_nothing_is_spent()
    {
        var early = Run("""
            local c, ally = caster("Hamg", "AHhb"), make("hfoo", 0, 300, 0)
            IssueTargetOrder(c, "holybolt", ally)
            at(0.15, function() IssueImmediateOrder(c, "stop") end)
            at(5, function() print(mana(c), BlzGetUnitAbilityCooldownRemaining(c, FourCC("AHhb"))) end)
            """, 6);
        var late = Run("""
            local c, ally = caster("Hamg", "AHhb"), make("hfoo", 0, 300, 0)
            IssueTargetOrder(c, "holybolt", ally)
            at(1, function() IssuePointOrder(c, "move", 0, 150) end)
            at(1.25, function() print("moved", GetUnitY(c) > 0) end)
            """, 6);

        Assert.Equal(["channel\t0.00", "cast\t0.00", "endcast\t0.15", "500.00\t0.0"], early);
        Assert.Equal(["channel\t0.00", "cast\t0.00", "effect\t0.30", "endcast\t1.00", "moved\ttrue"], late);
    }

    // An order given inside the end-cast event is the unit's, whether the cast ran out or was stopped
    [Fact]
    public void An_order_given_in_the_end_cast_event_stays()
    {
        var lines = Run("""
            local c, ally = caster("Hamg", "AHhb"), make("hfoo", 0, 300, 0)
            local t = CreateTrigger()
            TriggerRegisterUnitEvent(t, c, EVENT_UNIT_SPELL_ENDCAST)
            TriggerAddAction(t, function() IssuePointOrder(c, "move", 0, 3000) end)
            IssueTargetOrder(c, "holybolt", ally)
            at(2.75, function() print(GetUnitCurrentOrder(c) == 851986) end)
            """, 3);

        Assert.Equal(["channel\t0.00", "cast\t0.00", "effect\t0.30", "finish\t2.70", "endcast\t2.70", "true"], lines);
    }

    // Flame Strike (casting time 1.33): effect +0.30, its finish and first hit +1.63 whatever the backswing, and no fire when stopped 1 s in
    [Fact]
    public void A_casting_time_comes_between_the_effect_event_and_what_the_spell_does()
    {
        var lines = Run("""
            local c, foe = caster("Hamg", "AHfs"), make("hfoo", 1, 300, 0)
            BlzSetUnitRealField(c, UNIT_RF_CAST_BACK_SWING, 3)
            local h = hits(foe)
            IssuePointOrder(c, "flamestrike", 300, 0)
            at(3, function() print("first hit", string.format("%.2f", h[1]), mana(c)) end)
            """, 4);
        var stopped = Run("""
            local c, foe = caster("Hamg", "AHfs"), make("hfoo", 1, 300, 0)
            local h = hits(foe)
            IssuePointOrder(c, "flamestrike", 300, 0)
            at(1, function() IssueImmediateOrder(c, "stop") end)
            at(12, function() print("hits", #h, GetUnitState(c, UNIT_STATE_MANA) < 400) end)
            """, 13);

        Assert.Equal(["channel\t0.00", "cast\t0.00", "effect\t0.30", "finish\t1.63", "endcast\t1.63", "first hit\t1.63\t365.00"], lines);
        Assert.Equal(["channel\t0.00", "cast\t0.00", "effect\t0.30", "endcast\t1.00", "hits\t0\ttrue"], stopped);
    }

    // A Paladin (cooldown 2.20, damage point 0.433): 2.20 s between hits at agility 0, 1.10 at 50 and 0.44 from 200 up, and the hit 0.43, 0.22 and
    // 0.09 s after the attacked event
    [Theory]
    [InlineData(0, "2.200", "0.43")]
    [InlineData(13, "1.746", "0.34")]
    [InlineData(50, "1.100", "0.22")]
    [InlineData(200, "0.440", "0.09")]
    [InlineData(400, "0.440", "0.09")]
    public void A_heros_agility_quickens_its_attacks_up_to_five_times(int agility, string between, string point)
    {
        var lines = Run($$"""
            local hero, foe = make("Hpal"), make("hfoo", 1, 100, 0)
            SetHeroAgi(hero, {{agility}}, true)
            local h, a = hits(foe)
            IssueTargetOrder(hero, "attack", foe)
            at(20, function() print(string.format("%.3f", (h[#h] - h[1]) / (#h - 1)), string.format("%.2f", h[1] - a[1])) end)
            """, 21);

        Assert.Equal([$"{between}\t{point}"], lines);
    }

    // An archer (damage point 0.72, missile speed 900, launched 62 ahead): 0.72 s from the attacked event to the hit at 150, 0.87 at 200, 1.10 at 400
    // and 1.21 at 500; at 400 with the speed set to 300, 1.85
    [Theory]
    [InlineData(100, 0, "0.72")]
    [InlineData(150, 0, "0.72")]
    [InlineData(200, 0, "0.87")]
    [InlineData(400, 0, "1.10")]
    [InlineData(500, 0, "1.21")]
    [InlineData(400, 300, "1.85")]
    [InlineData(400, 1800, "0.91")]
    public void A_missile_flies_the_way_from_its_launch_point_at_its_speed(int distance, int speed, string landed)
    {
        var lines = Run($$"""
            local archer, foe = make("earc"), make("hfoo", 1, {{distance}}, 0)
            if {{speed}} > 0 then BlzSetUnitWeaponRealField(archer, UNIT_WEAPON_RF_ATTACK_PROJECTILE_SPEED, 0, {{speed}}) end
            local h, a = hits(foe)
            IssueTargetOrder(archer, "attack", foe)
            at(5, function() print(string.format("%.2f", h[1] - a[1]), string.format("%.2f", h[2] - h[1])) end)
            """, 6);

        Assert.Equal([$"{landed}\t1.50"], lines);
    }

    // Storm Bolt (missile speed 1000) from a Priest (cast point 0.50): its two hits 0.40 s after the effect at 400, and 0.80 with the speed set to 500
    [Theory]
    [InlineData(0, "0.90")]
    [InlineData(500, "1.30")]
    public void Storm_bolt_lands_after_its_missiles_flight(int speed, string landed)
    {
        var lines = Run($$"""
            local c, foe = make("hmpr"), make("hfoo", 1, 400, 0)
            UnitAddAbility(c, FourCC("AHtb"))
            SetUnitState(c, UNIT_STATE_MANA, 200)
            if {{speed}} > 0 then BlzSetAbilityIntegerField(BlzGetUnitAbility(c, FourCC("AHtb")), ABILITY_IF_MISSILE_SPEED, {{speed}}) end
            local h = hits(foe)
            IssueTargetOrder(c, "thunderbolt", foe)
            at(3, function() print(#h, string.format("%.2f", h[1]), string.format("%.2f", h[2])) end)
            """, 4);

        Assert.Equal([$"2\t{landed}\t{landed}"], lines);
    }

    // An Archmage of intelligence 19 got 9.50, 19.00 and 28.50 back in 10, 20 and 30 s, one of 0 none (its type's 0.01 doesn't count), a Priest
    // (0.67 a second) 6.70; a paused one none, a hidden one as a shown one, and a full one stays full
    [Fact]
    public void Mana_comes_back_by_a_heros_intelligence_and_by_another_units_own_regeneration()
    {
        var lines = Run("""
            local hero, dull, priest, paused, hidden, full = make("Hamg"), make("Hamg"), make("hmpr"), make("Hamg"), make("Hamg"), make("Hamg")
            SetHeroInt(dull, 0, true)
            PauseUnit(paused, true)
            ShowUnit(hidden, false)
            at(1, function()
                for _, u in ipairs({ hero, dull, paused, hidden }) do SetUnitState(u, UNIT_STATE_MANA, 500) end
                SetUnitState(priest, UNIT_STATE_MANA, 50)
                SetUnitState(full, UNIT_STATE_MANA, 5000)
                for i = 1, 3 do at(10 * i, function() print(mana(hero), mana(dull), mana(priest), mana(paused), mana(hidden), mana(full)) end) end
            end)
            at(40, function() PauseUnit(paused, false) end)
            at(50, function() print("freed", mana(paused)) end)
            """, 51);

        Assert.Equal(["509.50\t500.00\t56.70\t500.00\t509.50\t1285.00", "519.00\t500.00\t63.40\t500.00\t519.00\t1285.00", "528.50\t500.00\t70.10\t500.00\t528.50\t1285.00",
            "freed\t509.50"], lines);
    }

    // What is read mid-way is what had come back by then, and a change of intelligence counts from when it is made
    [Fact]
    public void Mana_that_came_back_is_kept_when_the_intelligence_changes()
    {
        var lines = Run("""
            local hero = make("Hamg")
            SetHeroInt(hero, 10, true)
            SetUnitState(hero, UNIT_STATE_MANA, 100)
            at(10, function() SetHeroInt(hero, 20, true) print(mana(hero)) end)
            at(20, function() print(mana(hero)) end)
            """, 21);

        Assert.Equal(["255.00", "265.00"], lines);
    }

    // Slow (3 s) on a unit paused from 1 s to 5 s ends 7 s in, and on a unit paused before it only once the unit is free again
    [Fact]
    public void A_buffs_time_stands_still_while_its_unit_is_paused()
    {
        var lines = Run("""
            local c, free, held, early = make("Hamg"), make("hfoo", 1, 100, 0), make("hfoo", 1, 0, 100), make("hfoo", 1, -100, 0)
            UnitAddAbility(c, FourCC("Aslo"))
            SetUnitState(c, UNIT_STATE_MANA, 500)
            BlzSetUnitRealField(c, UNIT_RF_CAST_POINT, 0)
            BlzSetUnitRealField(c, UNIT_RF_CAST_BACK_SWING, 0)
            local function slow(u) BlzEndUnitAbilityCooldown(c, FourCC("Aslo")) return IssueTargetOrder(c, "slow", u) end
            local function on(u) return GetUnitAbilityLevel(u, FourCC("Bslo")) end
            PauseUnit(early, true)
            print(slow(free), slow(held), slow(early))
            at(1, function() PauseUnit(held, true) end)
            at(5, function() PauseUnit(held, false) end)
            at(10, function() PauseUnit(early, false) end)
            for _, s in ipairs({ 2.9, 3.1, 6.9, 7.1, 12.9, 13.1 }) do at(s, function() print(s, on(free), on(held), on(early)) end) end
            """, 14);

        Assert.Equal(["true\ttrue\ttrue", "2.9\t1\t1\t1", "3.1\t0\t1\t1", "6.9\t0\t1\t1", "7.1\t0\t0\t1", "12.9\t0\t0\t1", "13.1\t0\t0\t0"], lines);
    }
}
