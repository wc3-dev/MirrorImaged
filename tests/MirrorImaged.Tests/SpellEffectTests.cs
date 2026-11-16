using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>The spells and auras of the heroes that damage, heal, slow, root, shield and stun, and what auras give the allies near.</summary>
public sealed class SpellEffectTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static Field Column(string column, string type, params string[] specific) => new(column, PerLevel.Column, -1, type, false, [.. specific.Select(Ids.Of)]);

    // Each ability: its order string, and its numbers at level 1 (Data A to E, area, duration)
    static readonly (string Id, string Order, string Numbers)[] Abilities =
    [
        ("AUdc", "deathcoil", "A=200"),
        ("AOcl", "chainlightning", "A=100;B=3;C=0.2;area=500"),
        ("AOhw", "healingwave", "A=100;B=2;C=0.25;area=500"),
        ("AUfn", "frostnova", "A=50;B=30;area=200;dur=4"),
        ("AOws", "stomp", "A=40;area=250;dur=3"),
        ("AEmb", "manaburn", "A=60;C=1"),
        ("AEer", "entanglingroots", "A=10;dur=3"),
        ("AHds", "divineshield", "dur=5"),
        ("AUfu", "frostarmor", "B=4;dur=5"),
        ("AHad", "", "A=2;area=900"),
        ("AOae", "", "A=0.1;B=0.2;area=900"),
        ("AUau", "", "A=0.1;B=5;area=900"),
        ("AUav", "", "A=0.5;area=900"),
        ("AEar", "", "A=0.5;area=900"),
        ("AHbh", "", "A=100;C=25;dur=2"),
        ("AOcr", "", "A=100;B=3"),
        ("AEev", "", "A=1"),
        ("AHbz", "blizzard", "A=3;B=30;area=200"),
        ("AUan", "animatedead", "A=2;area=900;dur=20"),
        ("AOhx", "hex", "A=99;dur=3;unit=nfro"),
        ("AEme", "metamorphosis", "dur=3;unit=hbig"),
        ("AHav", "avatar", "A=5;B=500;dur=3"),
        ("AHre", "resurrection", "A=2;area=900"),
        ("AUin", "inferno", "A=50;area=250;dur=20;unit=hfoo"),
        ("AHab", "", "A=5;area=900"),
        ("AEim", "immolation", "A=10;area=200;dur=3"),
        ("AOmi", "mirrorimage", "A=2;dur=20"),
        ("AEah", "", "A=0.5"),
        ("ANsi", "drain", "A=10;dur=3"),
        ("AOwk", "windwalk", "B=0.5;dur=3"),
        ("AUcb", "carrionscarabs", "A=2;dur=20;unit=hfoo"),
        ("AHfs", "flamestrike", "A=100;B=10;area=200;dur=2"),
        ("AEsf", "starfall", "A=30;area=900;dur=2"),
        ("AHwe", "waterelemental", "A=2;dur=20;unit=hfoo"),
        ("AOsf", "spiritwolf", "B=3;dur=20;unit=hfoo"),
    ];

    static StandardData Standard()
    {
        var data = new StandardData();
        foreach (var (id, field) in new (string, Field)[]
        {
            ("alev", new("levels", PerLevel.None, -1, "int")), ("code", new("code", PerLevel.None, -1)), ("aord", new("Order", PerLevel.None, -1, "string")),
            ("acas", Column("Cast", "unreal")), ("aran", Column("Rng", "unreal")), ("aare", Column("Area", "unreal")), ("acdn", Column("Cool", "unreal")), ("amcs", Column("Cost", "int")),
            ("adur", Column("Dur", "unreal")), ("ahdu", Column("HeroDur", "unreal")), ("uuid", Column("UnitID", "string", "AHwe", "AOsf", "AOhx", "AEme", "AUin")),
        })
            data.Add(ObjectKind.Ability, id, field);
        foreach (var letter in "ABCDE")
            data.Add(ObjectKind.Ability, "Z" + letter + "01", Column("Data" + letter, "unreal", [.. Abilities.Select(a => a.Id)]));
        foreach (var (id, order, numbers) in Abilities)
        {
            var n = Values(numbers + ";x=0");
            var own = $"code={id};alev=1;aord={order};acas:1=0.1;aran:1=900;aare:1={n.GetValueOrDefault("area", "0")};acdn:1=1;amcs:1=10;adur:1={n.GetValueOrDefault("dur", "0")};ahdu:1={n.GetValueOrDefault("dur", "0")}";
            foreach (var letter in "ABCDE") if (n.TryGetValue(letter.ToString(), out var value)) own += $";Z{letter}01:1={value}";
            if (n.TryGetValue("unit", out var summoned)) own += $";uuid:1={summoned}";
            data.Add(ObjectKind.Ability, id, Values(own));
        }
        return data.Add(ObjectKind.Unit, "nfro", Values("unam=Frog;uhpm=50;umvs=100;umvt=foot;uaen=0;ucol=31")).Add(ObjectKind.Unit, "hbig", Values("unam=Big;uhpm=2000;umvs=300;umvt=foot;uaen=0;ucol=31")).Add(ObjectKind.Unit, "hatk", Values("unam=Attacker;uhpm=1000;umpm=500;umpi=500;umvs=270;umvt=foot;uacq=500;uaen=1;ua1w=normal;ua1t=normal;ua1g=ground;ua1b=10;ua1d=1;ua1s=1;ua1c=1;ua1r=90;udp1=0.5;ucol=31;udef=0")).Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=1000;umpm=500;umpi=500;umvs=270;umvt=foot;uacq=500;udea=3;uaen=0;ua1w=normal;ua1t=normal;ua1g=ground;ua1b=10;ua1d=1;ua1s=1;ua1c=1;ua1r=90;udp1=0.5;ucol=31;udef=0"));
    }

    List<string> Run(string main, double seconds)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            local function f1(x) return string.format("%.1f", x) end
            local function make(p, x, y) return CreateUnit(p or Player(0), FourCC("hfoo"), x or 0, y or 0, 0) end
            function config() SetPlayers(2) end
            function main()
            {{main}}
            end
            """), "", data: Standard());
        simulation.Advance(TimeSpan.FromSeconds(seconds));
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];
    }

    [Fact]
    public void Death_coil_hurts_a_foe_and_heals_a_friend()
    {
        var lines = Run("""
            local caster, foe, friend = make(Player(0)), make(Player(1), 300, 0), make(Player(0), 0, 200)
            SetWidgetLife(friend, 100)
            UnitAddAbility(caster, FourCC("AUdc"))
            IssueTargetOrder(caster, "deathcoil", foe)
            TimerStart(CreateTimer(), 2, false, function() IssueTargetOrder(caster, "deathcoil", friend) end)
            TimerStart(CreateTimer(), 3, false, function() print(f1(GetWidgetLife(foe)), f1(GetWidgetLife(friend))) end)
            """, 4);

        Assert.Equal(["800.0\t300.0"], lines);
    }

    [Fact]
    public void Chain_lightning_jumps_to_the_nearest_foes_for_less_each_time_and_healing_wave_heals_the_hurt()
    {
        var lines = Run("""
            local caster = make(Player(0))
            local a, b, c, d = make(Player(1), 300, 0), make(Player(1), 500, 0), make(Player(1), 700, 0), make(Player(1), 1500, 0)
            UnitAddAbility(caster, FourCC("AOcl"))
            UnitAddAbility(caster, FourCC("AOhw"))
            local f1a, f1b = make(Player(0), 0, 200), make(Player(0), 0, 400)
            SetWidgetLife(f1a, 500); SetWidgetLife(f1b, 500)
            IssueTargetOrder(caster, "chainlightning", a)
            TimerStart(CreateTimer(), 1, false, function() IssueTargetOrder(caster, "healingwave", f1a) end)
            TimerStart(CreateTimer(), 3, false, function()
                print(f1(GetWidgetLife(a)), f1(GetWidgetLife(b)), f1(GetWidgetLife(c)), f1(GetWidgetLife(d)))
                print(f1(GetWidgetLife(f1a)), f1(GetWidgetLife(f1b)))
            end)
            """, 4);

        Assert.Equal(["900.0\t920.0\t936.0\t1000.0", "600.0\t575.0"], lines);
    }

    [Fact]
    public void Frost_nova_stomp_and_mana_burn_and_roots_do_what_their_data_says()
    {
        var lines = Run("""
            local caster = make(Player(0))
            local a, b = make(Player(1), 300, 0), make(Player(1), 400, 0)
            UnitAddAbility(caster, FourCC("AUfn")); UnitAddAbility(caster, FourCC("AOws")); UnitAddAbility(caster, FourCC("AEmb")); UnitAddAbility(caster, FourCC("AEer"))
            IssueTargetOrder(caster, "frostnova", a)
            TimerStart(CreateTimer(), 1, false, function() print("nova", f1(GetWidgetLife(a)), f1(GetWidgetLife(b)), f1(GetUnitMoveSpeed(b))) end)
            TimerStart(CreateTimer(), 2, false, function() IssueTargetOrder(caster, "manaburn", a) end)
            TimerStart(CreateTimer(), 3, false, function() print("burn", f1(GetWidgetLife(a)), f1(GetUnitState(a, UNIT_STATE_MANA))) IssueTargetOrder(caster, "entanglingroots", b) end)
            TimerStart(CreateTimer(), 4.5, false, function() print("roots", f1(GetWidgetLife(b)), IssuePointOrder(b, "move", 900, 0)) end)
            TimerStart(CreateTimer(), 6, false, function() print("held", f1(GetUnitX(b))) end)
            TimerStart(CreateTimer(), 12, false, function() print("free", GetUnitX(b) > 600) end)
            local c = make(Player(1), 100, 0)
            TimerStart(CreateTimer(), 8, false, function() IssueImmediateOrder(caster, "stomp") end)
            TimerStart(CreateTimer(), 9, false, function() print("stomp", f1(GetWidgetLife(c))) end)
            """, 13);

        Assert.Equal(["nova\t950.0\t970.0\t135.0", "burn\t890.0\t440.0", "roots\t960.0\ttrue", "held\t400.0", "stomp\t930.0", "free\ttrue"], lines);
    }

    [Fact]
    public void Divine_shield_makes_a_unit_take_nothing_for_its_time()
    {
        var lines = Run("""
            local shielded, foe = make(Player(0)), CreateUnit(Player(1), FourCC("hatk"), 60, 0, 0)
            UnitAddAbility(shielded, FourCC("AHds"))
            IssueImmediateOrder(shielded, "divineshield")
            TimerStart(CreateTimer(), 3, false, function() print("shielded", f1(GetWidgetLife(shielded))) end)
            IssueTargetOrder(foe, "attack", shielded)
            TimerStart(CreateTimer(), 10, false, function() print("after", GetWidgetLife(shielded) < 1000) end)
            """, 11);

        Assert.Equal(["shielded\t1000.0", "after\ttrue"], lines);
    }

    [Fact]
    public void An_aura_gives_the_allies_near_its_buff_once_a_second()
    {
        var lines = Run("""
            local hero, ally, far, foe = make(Player(0)), make(Player(0), 300, 0), make(Player(0), 3000, 0), make(Player(1), 300, 100)
            UnitAddAbility(hero, FourCC("AHad")); UnitAddAbility(hero, FourCC("AOae")); UnitAddAbility(hero, FourCC("AUau"))
            SetWidgetLife(ally, 500)
            TimerStart(CreateTimer(), 2.2, false, function()
                print("armour", f1(BlzGetUnitArmor(ally)), "speed", f1(GetUnitMoveSpeed(ally)), "life", f1(GetWidgetLife(ally)))
                print("far", f1(GetUnitMoveSpeed(far)), "foe", f1(GetUnitMoveSpeed(foe)))
            end)
            TimerStart(CreateTimer(), 5, false, function() print("gone", f1(GetUnitMoveSpeed(far))) KillUnit(hero) end)
            TimerStart(CreateTimer(), 8, false, function() print("after death", f1(GetUnitMoveSpeed(ally))) end)
            """, 9);

        Assert.Equal(["armour\t0.0\tspeed\t326.7\tlife\t510.0", "far\t270.0\tfoe\t270.0", "gone\t270.0", "after death\t270.0"], lines);
    }

    [Fact]
    public void A_summoning_spell_calls_units_that_end_with_their_time_and_fire_the_summon_event()
    {
        var lines = Run("""
            local caster = make(Player(0))
            UnitAddAbility(caster, FourCC("AHwe")); UnitAddAbility(caster, FourCC("AOsf"))
            local t = CreateTrigger()
            TriggerRegisterPlayerUnitEvent(t, Player(0), EVENT_PLAYER_UNIT_SUMMON, nil)
            TriggerAddAction(t, function() print("summon", GetUnitName(GetSummonedUnit()), GetSummoningUnit() == caster, IsUnitType(GetSummonedUnit(), UNIT_TYPE_SUMMONED)) end)
            IssueImmediateOrder(caster, "waterelemental")
            TimerStart(CreateTimer(), 2, false, function() IssueImmediateOrder(caster, "spiritwolf") end)
            local function count() local g = CreateGroup() GroupEnumUnitsOfPlayer(g, Player(0), nil) local n = 0 ForGroup(g, function() if UnitAlive(GetEnumUnit()) then n = n + 1 end end) DestroyGroup(g) return n end
            TimerStart(CreateTimer(), 4, false, function() print("count", count()) end)
            TimerStart(CreateTimer(), 30, false, function() print("later", count()) end)
            """, 31);

        Assert.Equal(["summon	Footman	true	true", "summon	Footman	true	true", "summon	Footman	true	true", "summon	Footman	true	true", "summon	Footman	true	true", "count	6", "later	1"], lines);
    }

    [Fact]
    public void Critical_strike_multiplies_the_damage_bash_adds_to_it_and_stuns_and_evasion_misses()
    {
        var lines = Run("""
            local crit, bash, plain = CreateUnit(Player(0), FourCC("hatk"), 0, 0, 0), CreateUnit(Player(0), FourCC("hatk"), 0, 400, 0), CreateUnit(Player(0), FourCC("hatk"), 0, 800, 0)
            local v1, v2, v3 = make(Player(1), 60, 0), make(Player(1), 60, 400), make(Player(1), 60, 800)
            UnitAddAbility(crit, FourCC("AOcr")); UnitAddAbility(bash, FourCC("AHbh"))
            local dodger, hitter = make(Player(1), 5000, 0), CreateUnit(Player(0), FourCC("hatk"), 5060, 0, 0)
            UnitAddAbility(dodger, FourCC("AEev"))
            IssueTargetOrder(crit, "attack", v1); IssueTargetOrder(bash, "attack", v2); IssueTargetOrder(plain, "attack", v3); IssueTargetOrder(hitter, "attack", dodger)
            TimerStart(CreateTimer(), 1.5, false, function()
                print("crit", f1(1000 - GetWidgetLife(v1)), "bash", f1(1000 - GetWidgetLife(v2)), "plain", f1(1000 - GetWidgetLife(v3)), "evaded", GetWidgetLife(dodger))
            end)
            """, 2);

        Assert.Equal(["crit	33.0	bash	36.0	plain	11.0	evaded	1000.0"], lines);
    }

    [Fact]
    public void Blizzard_hits_the_ground_units_in_its_area_once_a_second_and_animate_dead_raises_corpses()
    {
        var lines = Run("""
            local caster = make(Player(0))
            UnitAddAbility(caster, FourCC("AHbz")); UnitAddAbility(caster, FourCC("AUan"))
            local a, b = make(Player(1), 400, 0), make(Player(1), 400, 100)
            local far = make(Player(1), 1400, 0)
            IssuePointOrder(caster, "blizzard", 400, 50)
            TimerStart(CreateTimer(), 5, false, function() print("blizzard", f1(1000 - GetWidgetLife(a)), f1(1000 - GetWidgetLife(b)), f1(1000 - GetWidgetLife(far))) end)
            local d1, d2, d3 = make(Player(1), 1000, 1000), make(Player(1), 1050, 1000), make(Player(1), 1100, 1000)
            SetUnitPosition(caster, 900, 900)
            TimerStart(CreateTimer(), 6, false, function() KillUnit(d1); KillUnit(d2); KillUnit(d3); IssuePointOrder(caster, "animatedead", 1050, 1000) end)
            local function mine() local g = CreateGroup() GroupEnumUnitsOfPlayer(g, Player(0), nil) local n = 0 ForGroup(g, function() if UnitAlive(GetEnumUnit()) then n = n + 1 end end) DestroyGroup(g) return n end
            TimerStart(CreateTimer(), 9, false, function() print("raised", mine()) end)
            TimerStart(CreateTimer(), 40, false, function() print("later", mine()) end)
            """, 41);

        Assert.Equal(["blizzard	90.0	90.0	0.0", "raised	3", "later	1"], lines);
    }

    [Fact]
    public void Hex_and_metamorphosis_change_a_unit_for_a_time_and_avatar_gives_armour_and_life()
    {
        var lines = Run("""
            local caster, victim = make(Player(0)), make(Player(1), 300, 0)
            for _, a in ipairs({"AOhx", "AEme", "AHav"}) do UnitAddAbility(caster, FourCC(a)); UnitMakeAbilityPermanent(caster, true, FourCC(a)) end
            IssueTargetOrder(caster, "hex", victim)
            TimerStart(CreateTimer(), 1, false, function() print("hexed", GetUnitName(victim), f1(GetUnitMoveSpeed(victim)), f1(GetUnitState(victim, UNIT_STATE_MAX_LIFE))) end)
            TimerStart(CreateTimer(), 5, false, function() print("back", GetUnitName(victim), f1(GetUnitMoveSpeed(victim))) IssueImmediateOrder(caster, "metamorphosis") end)
            TimerStart(CreateTimer(), 6, false, function() print("morphed", GetUnitName(caster), f1(GetUnitState(caster, UNIT_STATE_MAX_LIFE))) end)
            TimerStart(CreateTimer(), 10, false, function() print("normal", GetUnitName(caster)) IssueImmediateOrder(caster, "avatar") end)
            TimerStart(CreateTimer(), 11, false, function() print("avatar", UnitCountBuffsEx(caster, true, false, true, true, true, true, true) > 0, f1(GetUnitState(caster, UNIT_STATE_MAX_LIFE))) end)
            TimerStart(CreateTimer(), 16, false, function() print("over", UnitCountBuffsEx(caster, true, false, true, true, true, true, true) > 0, f1(GetUnitState(caster, UNIT_STATE_MAX_LIFE))) end)
            """, 17);

        Assert.Equal(["hexed	Frog	100.0	50.0", "back	Footman	270.0", "morphed	Big	2000.0", "normal	Footman", "avatar	true	1500.0", "over	false	1000.0"], lines);
    }

    [Fact]
    public void Resurrection_raises_friends_corpses_inferno_lands_with_damage_and_brilliance_gives_mana()
    {
        var lines = Run("""
            local caster = make(Player(0))
            UnitAddAbility(caster, FourCC("AHre")); UnitAddAbility(caster, FourCC("AUin")); UnitAddAbility(caster, FourCC("AHab"))
            local friend1, friend2, foe = make(Player(0), 200, 0), make(Player(0), 250, 0), make(Player(1), 1000, 0)
            local near = make(Player(1), 1100, 0)
            local low = make(Player(0), 0, 300)
            SetUnitState(low, UNIT_STATE_MANA, 0)
            TimerStart(CreateTimer(), 2, false, function() KillUnit(friend1); KillUnit(friend2) end)
            TimerStart(CreateTimer(), 3, false, function() IssueImmediateOrder(caster, "resurrection") end)
            local function alive(p) local g = CreateGroup() GroupEnumUnitsOfPlayer(g, p, nil) local n = 0 ForGroup(g, function() if UnitAlive(GetEnumUnit()) then n = n + 1 end end) DestroyGroup(g) return n end
            TimerStart(CreateTimer(), 4, false, function() print("raised", alive(Player(0))) IssuePointOrder(caster, "inferno", 1050, 0) end)
            TimerStart(CreateTimer(), 8, false, function() print("inferno", f1(1000 - GetWidgetLife(foe)), f1(1000 - GetWidgetLife(near)), alive(Player(0))) end)
            TimerStart(CreateTimer(), 9, false, function() print("brilliance", GetUnitState(low, UNIT_STATE_MANA) > 20) end)
            """, 10);

        Assert.Equal(["raised	4", "inferno	50.0	50.0	5", "brilliance	true"], lines);
    }

    [Fact]
    public void Flame_strike_burns_the_place_and_starfall_hits_foes_round_the_caster_each_second()
    {
        var strike = Run("""
            local caster, foe, far = make(Player(0)), make(Player(1), 300, 0), make(Player(1), 700, 0)
            UnitAddAbility(caster, FourCC("AHfs"))
            IssuePointOrder(caster, "flamestrike", 300, 0)
            TimerStart(CreateTimer(), 5, false, function() print("strike", f1(1000 - GetWidgetLife(foe)), f1(1000 - GetWidgetLife(far))) end)
            """, 6);
        var fall = Run("""
            local caster, foe = make(Player(0)), make(Player(1), 300, 0)
            UnitAddAbility(caster, FourCC("AEsf"))
            IssueImmediateOrder(caster, "starfall")
            TimerStart(CreateTimer(), 5, false, function() print("fall", f1(1000 - GetWidgetLife(foe))) end)
            """, 6);

        Assert.Equal(["strike	120.0	0.0"], strike);
        Assert.Equal(["fall	60.0"], fall);
    }

    [Fact]
    public void Siphon_mana_moves_mana_wind_walk_speeds_up_and_carrion_beetles_summon()
    {
        var lines = Run("""
            local caster, foe = make(Player(0)), make(Player(1), 300, 0)
            SetUnitState(caster, UNIT_STATE_MANA, 100)
            UnitAddAbility(caster, FourCC("ANsi")); UnitAddAbility(caster, FourCC("AOwk")); UnitAddAbility(caster, FourCC("AUcb"))
            IssueTargetOrder(caster, "drain", foe)
            TimerStart(CreateTimer(), 5, false, function() print("siphon", f1(GetUnitState(caster, UNIT_STATE_MANA)), f1(GetUnitState(foe, UNIT_STATE_MANA))) IssueImmediateOrder(caster, "windwalk") end)
            TimerStart(CreateTimer(), 5.5, false, function() print("speed", f1(GetUnitMoveSpeed(caster))) IssuePointOrder(caster, "carrionscarabs", 0, 100) end)
            local function count() local g = CreateGroup() GroupEnumUnitsOfPlayer(g, Player(0), nil) local n = 0 ForGroup(g, function() n = n + 1 end) DestroyGroup(g) return n end
            TimerStart(CreateTimer(), 7, false, function() print("beetles", count()) end)
            """, 8);

        Assert.Equal(["siphon	120.0	470.0", "speed	405.0", "beetles	3"], lines);
    }

    [Fact]
    public void Immolation_burns_mirror_image_copies_and_thorns_return_melee_damage()
    {
        var lines = Run("""
            local caster, foe = make(Player(0)), make(Player(1), 100, 0)
            UnitAddAbility(caster, FourCC("AEim")); UnitAddAbility(caster, FourCC("AOmi"))
            IssueImmediateOrder(caster, "immolation")
            TimerStart(CreateTimer(), 5, false, function() print("burn", f1(1000 - GetWidgetLife(foe))) IssueImmediateOrder(caster, "mirrorimage") end)
            local function count() local g = CreateGroup() GroupEnumUnitsOfPlayer(g, Player(0), nil) local n = 0 ForGroup(g, function() n = n + 1 end) DestroyGroup(g) return n end
            TimerStart(CreateTimer(), 7, false, function() print("images", count()) end)
            """, 8);

        Assert.Equal(["burn	40.0", "images	3"], lines);

        var thorns = Run("""
            local target, attacker = make(Player(0)), CreateUnit(Player(1), FourCC("hatk"), 40, 0, 0)
            UnitAddAbility(target, FourCC("AEah"))
            IssueTargetOrder(attacker, "attack", target)
            TimerStart(CreateTimer(), 3, false, function() print("thorns", GetWidgetLife(attacker) < 1000) end)
            """, 4);
        Assert.Equal(["thorns	true"], thorns);
    }
}
