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
        ("AOeq", "earthquake", "A=0.5;B=20;C=0.75;area=300;dur=10"),
        ("AEim", "immolation", "A=10;B=7;area=200;dur=1"),
        ("AOmi", "mirrorimage", "A=2;B=0;C=2;D=0.5;dur=20"),
        ("AEah", "", "A=0.5"),
        ("ANsi", "silence", "dur=3;area=300"),
        ("AOwk", "windwalk", "A=0.6;B=0.5;C=40;dur=10"),
        ("AUcb", "carrionscarabs", "A=1;C=hbet;E=3;area=900"),
        ("AHbn", "banish", "A=0.5;dur=12;hdur=4"),
        ("AUls", "locustswarm", "A=5;B=0.2;C=2;D=0.75;area=800;dur=12.5;unit=uloc"),
        ("AHfs", "flamestrike", "A=15;B=0.33;C=4;D=1;area=200;dur=9;hdur=2.67"),
        ("AEsf", "starfall", "A=30;B=1.5;area=900;dur=10"),
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
            ("adur", Column("Dur", "unreal")), ("ahdu", Column("HeroDur", "unreal")), ("uuid", Column("UnitID", "string", "AHwe", "AOsf", "AOhx", "AEme", "AUin", "AUls")),
        })
            data.Add(ObjectKind.Ability, id, field);
        foreach (var letter in "ABCDE")
            data.Add(ObjectKind.Ability, "Z" + letter + "01", Column("Data" + letter, "unreal", [.. Abilities.Select(a => a.Id)]));
        foreach (var (id, order, numbers) in Abilities)
        {
            var n = Values(numbers + ";x=0");
            var own = $"code={id};alev=1;aord={order};acas:1=0.1;aran:1=900;aare:1={n.GetValueOrDefault("area", "0")};acdn:1=1;amcs:1=10;adur:1={n.GetValueOrDefault("dur", "0")};ahdu:1={n.GetValueOrDefault("hdur", n.GetValueOrDefault("dur", "0"))}";
            foreach (var letter in "ABCDE") if (n.TryGetValue(letter.ToString(), out var value)) own += $";Z{letter}01:1={value}";
            if (n.TryGetValue("unit", out var summoned)) own += $";uuid:1={summoned}";
            data.Add(ObjectKind.Ability, id, Values(own));
        }
        return data.Add(ObjectKind.Unit, "hbld", Values("unam=Hall;uhpm=1000;ubdg=1;umvt=;uaen=0;ucol=31")).Add(ObjectKind.Unit, "nfro", Values("unam=Frog;uhpm=50;umvs=100;umvt=foot;uaen=0;ucol=31")).Add(ObjectKind.Unit, "hbig", Values("unam=Big;uhpm=2000;umvs=300;umvt=foot;uaen=0;ucol=31")).Add(ObjectKind.Unit, "Hfoo", Values("unam=Hero;uhpm=1000;umvs=270;umvt=foot;uaen=0;ucol=31")).Add(ObjectKind.Unit, "hbet", Values("unam=Beetle;uhpm=140;umvs=270;umvt=foot;uaen=0;ucol=16")).Add(ObjectKind.Unit, "uloc", Values("unam=Locust;uhpm=65;umvs=400;umvt=fly;uacq=900;uaen=1;ua1w=missile;ua1t=spells;ua1g=ground,air;ua1b=12;ua1d=1;ua1s=1;ua1c=1;ua1r=10;udp1=0;uabi=Aloc;ucol=0")).Add(ObjectKind.Unit, "hmag", Values("unam=Mage;uhpm=300;umvs=270;umvt=foot;uacq=600;uaen=1;ua1w=missile;ua1t=magic;ua1g=ground;ua1b=10;ua1d=1;ua1s=1;ua1c=2;ua1r=600;udp1=0.5;ucol=16;udef=0")).Add(ObjectKind.Ability, "Aloc", Values("code=Aloc;alev=1")).Add(ObjectKind.Ability, "Aatk", Values("code=Aatk;alev=1")).Add(ObjectKind.Unit, "hatk", Values("unam=Attacker;uhpm=1000;umpm=500;umpi=500;umvs=270;umvt=foot;uacq=500;uaen=1;ua1w=normal;ua1t=normal;ua1g=ground;ua1b=10;ua1d=1;ua1s=1;ua1c=1;ua1r=90;udp1=0.5;ucol=31;udef=0")).Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=1000;umpm=500;umpi=500;umvs=270;umvt=foot;uacq=500;udea=3;udtm=3;uaen=0;ua1w=normal;ua1t=normal;ua1g=ground;ua1b=10;ua1d=1;ua1s=1;ua1c=1;ua1r=90;udp1=0.5;ucol=31;udef=0"));
    }

    List<string> Run(string main, double seconds, string constants = "")
    {
        var data = Standard();
        if (constants.Length > 0) foreach (var (key, value) in Values(constants)) data.Constants[key] = value;
        using var simulation = Simulation.Load(_maps.Build($$"""
            local function f1(x) return string.format("%.1f", x) end
            local function make(p, x, y) return CreateUnit(p or Player(0), FourCC("hfoo"), x or 0, y or 0, 0) end
            function config() SetPlayers(2) end
            function main()
            {{main}}
            end
            """), "", data: data);
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

    // probe units-resets: an aura switched off stops being given and its buff ends with the buff's time; switched on it gives again
    [Fact]
    public void An_aura_switched_off_stops_giving_its_buff_and_switched_on_gives_it_again()
    {
        var lines = Run("""
            local hero, ally = make(Player(0)), make(Player(0), 300, 0)
            UnitAddAbility(hero, FourCC("AOae"))
            local function at(t, label) TimerStart(CreateTimer(), t, false, function() print(label, f1(GetUnitMoveSpeed(ally))) end) end
            at(2.2, "on")
            TimerStart(CreateTimer(), 3.5, false, function() BlzUnitEnableAuras(hero, false, true) end)
            at(4.4, "1 s after off")
            at(6, "3 s after off")
            TimerStart(CreateTimer(), 6.05, false, function() BlzUnitEnableAuras(hero, true, true) end)
            at(8, "on again")
            """, 9);

        Assert.Equal(["on\t297.0", "1 s after off\t297.0", "3 s after off\t270.0", "on again\t297.0"], lines);
    }

    // probe units-resets: the cooldown running on the unit's abilities ends at once
    [Fact]
    public void Resetting_cooldowns_ends_those_running_and_lets_the_spell_be_cast_again()
    {
        var lines = Run("""
            local caster, foe = make(Player(0)), make(Player(1), 300, 0)
            UnitAddAbility(caster, FourCC("AUdc"))
            IssueTargetOrder(caster, "deathcoil", foe)
            TimerStart(CreateTimer(), 0.5, false, function()
                print(f1(BlzGetUnitAbilityCooldownRemaining(caster, FourCC("AUdc"))))
                UnitResetCooldown(caster)
                print(f1(BlzGetUnitAbilityCooldownRemaining(caster, FourCC("AUdc"))), IssueTargetOrder(caster, "deathcoil", foe))
            end)
            """, 1);

        Assert.Equal(["0.5", "0.0\ttrue"], lines);
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

        Assert.Equal(["summon\tFootman\ttrue\ttrue", "summon\tFootman\ttrue\ttrue", "summon\tFootman\ttrue\ttrue", "summon\tFootman\ttrue\ttrue", "summon\tFootman\ttrue\ttrue", "count\t6", "later\t1"], lines);
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

        Assert.Equal(["crit\t33.0\tbash\t36.0\tplain\t11.0\tevaded\t1000.0"], lines);
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

        Assert.Equal(["blizzard\t90.0\t90.0\t0.0", "raised\t3", "later\t1"], lines);
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
            TimerStart(CreateTimer(), 11, false, function() print("avatar", UnitCountBuffsEx(caster, true, false, true, false, false, false, false) > 0, f1(GetUnitState(caster, UNIT_STATE_MAX_LIFE))) end)
            TimerStart(CreateTimer(), 16, false, function() print("over", UnitCountBuffsEx(caster, true, false, true, false, false, false, false) > 0, f1(GetUnitState(caster, UNIT_STATE_MAX_LIFE))) end)
            """, 17);

        // probe units-kinds: a hexed footman keeps its own type and name
        Assert.Equal(["hexed\tFootman\t270.0\t1000.0", "back\tFootman\t270.0", "morphed\tBig\t2000.0", "normal\tFootman", "avatar\ttrue\t1500.0", "over\tfalse\t1000.0"], lines);
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

        Assert.Equal(["raised\t4", "inferno\t50.0\t50.0\t5", "brilliance\ttrue"], lines);
    }

    // Probe abilities-spells: 15 every 0.33 s from the casting time on for the hero duration (9 hits), then 4 a second to the duration's end (6), to
    // the foes whose edge is in the area; a paused foe takes the first hit alone
    [Fact]
    public void Flame_strike_burns_hard_then_low_the_foes_whose_edge_is_at_the_place()
    {
        var lines = Run("""
            local caster, foe, edge, far, paused = make(Player(0)), make(Player(1), 300, 0), make(Player(1), 525, 0), make(Player(1), 300, 260), make(Player(1), 300, -100)
            PauseUnit(paused, true)
            UnitAddAbility(caster, FourCC("AHfs"))
            IssuePointOrder(caster, "flamestrike", 300, 0)
            local function lost(u) return f1(1000 - GetWidgetLife(u)) end
            TimerStart(CreateTimer(), 1, false, function() print("at 1", lost(foe)) end)
            TimerStart(CreateTimer(), 3.5, false, function() print("at 3.5", lost(foe), lost(edge), lost(far), lost(paused)) end)
            TimerStart(CreateTimer(), 12, false, function() print("at 12", lost(foe), lost(edge), lost(far), lost(paused)) end)
            """, 13);

        Assert.Equal(["at 1\t45.0", "at 3.5\t135.0\t135.0\t0.0\t15.0", "at 12\t159.0\t159.0\t0.0\t15.0"], lines);
    }

    // Probe abilities-spells: a wave every DataB seconds while the caster channels; another order ends it, with the end-cast event
    [Fact]
    public void Starfall_hits_the_foes_round_the_caster_every_wave_until_another_order_ends_it()
    {
        var lines = Run("""
            local caster, foe, edge, far = make(Player(0)), make(Player(1), 300, 0), make(Player(1), 930, 0), make(Player(1), 0, 940)
            UnitAddAbility(caster, FourCC("AEsf"))
            BlzSetUnitRealField(caster, UNIT_RF_CAST_BACK_SWING, 8)
            local ends, t = 0, CreateTrigger()
            TriggerRegisterUnitEvent(t, caster, EVENT_UNIT_SPELL_ENDCAST)
            TriggerAddAction(t, function() ends = ends + 1 end)
            IssueImmediateOrder(caster, "starfall")
            local function lost(u) return f1(1000 - GetWidgetLife(u)) end
            TimerStart(CreateTimer(), 5, false, function()
                print("channelling", lost(foe), lost(edge), lost(far), ends, GetUnitCurrentOrder(caster) ~= 0)
                IssueImmediateOrder(caster, "stop")
            end)
            TimerStart(CreateTimer(), 9, false, function() print("stopped", lost(foe), ends, GetUnitCurrentOrder(caster)) end)
            """, 10);

        Assert.Equal(["channelling\t90.0\t90.0\t0.0\t0\ttrue", "stopped\t90.0\t1\t0"], lines);
    }

    // Probe abilities-spells: only what has mana is silenced (a footman 100 away got no buff), by its edge
    [Fact]
    public void Silence_stops_the_foes_with_mana_at_the_place_from_casting()
    {
        var lines = Run("""
            local caster, foe, drained, far = make(Player(0)), make(Player(1), 300, 0), make(Player(1), 300, 100), make(Player(1), 300, -340)
            BlzSetUnitMaxMana(drained, 0)
            UnitAddAbility(caster, FourCC("ANsi")); UnitAddAbility(foe, FourCC("AHre"))
            IssuePointOrder(caster, "silence", 300, 0)
            local function has(u) return GetUnitAbilityLevel(u, FourCC("BNsi")) end
            TimerStart(CreateTimer(), 1, false, function() print("silenced", tostring(IssueImmediateOrder(foe, "resurrection")), has(foe), has(drained), has(far)) end)
            TimerStart(CreateTimer(), 5, false, function() print("free", tostring(IssueImmediateOrder(foe, "resurrection")), has(foe)) end)
            """, 6);

        Assert.Equal(["silenced\tfalse\t1\t0\t0", "free\ttrue\t0"], lines);
    }

    // Probe abilities-spells: faster at once, unseen by a foe from DataA seconds in, no attack of its own, and the buff ends as its ordered attack
    // lands, which takes a hit of 0 and then DataC more than the weapon's 11
    [Fact]
    public void Wind_walk_hides_and_speeds_the_unit_which_attacks_nothing_by_itself_and_ends_it_with_a_harder_hit()
    {
        var lines = Run("""
            local caster, foe = CreateUnit(Player(0), FourCC("hatk"), 0, 0, 0), make(Player(1), 60, 0)
            PauseUnit(caster, true)
            UnitAddAbility(caster, FourCC("AOwk"))
            local hits, t = "", CreateTrigger()
            TriggerRegisterUnitEvent(t, foe, EVENT_UNIT_DAMAGED)
            TriggerAddAction(t, function() hits = hits .. " " .. f1(GetEventDamage()) end)
            local function state() return f1(GetUnitMoveSpeed(caster)) .. " " .. tostring(IsUnitInvisible(caster, Player(1))) .. " " .. tostring(IsUnitInvisible(caster, Player(0))) .. " " .. GetUnitAbilityLevel(caster, FourCC("BOwk")) .. hits end
            local function at(s, f) TimerStart(CreateTimer(), s, false, f) end
            at(1, function() PauseUnit(caster, false) IssueImmediateOrder(caster, "windwalk") end)
            at(1.5, function() print("fading", state()) end)
            at(2, function() print("unseen", state()) end)
            at(4, function() print("still", state()) IssueTargetOrder(caster, "attack", foe) end)
            at(5, function() print("struck", state()) end)
            """, 6);

        Assert.Equal(["fading\t405.0 false false 1", "unseen\t405.0 true false 1", "still\t405.0 true false 1", "struck\t270.0 false false 0 0.0 51.0"], lines);
    }

    // Probe abilities-spells: 10 the moment it is on and every second after, 7 mana a second taken evenly, until it is ordered off or the mana is out;
    // a paused foe takes the first hit alone, and a foe is in reach by its edge. It is cast inside the order's call: 10 and 4.6 s of 7 are gone at 4.6
    [Fact]
    public void Immolation_burns_and_takes_mana_until_it_is_ordered_off_or_the_mana_is_out()
    {
        var lines = Run("""
            local caster, foe, edge, far, paused = make(Player(0)), make(Player(1), 100, 0), make(Player(1), 0, 230), make(Player(1), 0, -240), make(Player(1), -100, 0)
            PauseUnit(paused, true)
            UnitAddAbility(caster, FourCC("AEim"))
            local function lost(u) return f1(1000 - GetWidgetLife(u)) end
            local function state() return lost(foe) .. " " .. lost(edge) .. " " .. lost(far) .. " " .. lost(paused) .. " " .. f1(GetUnitState(caster, UNIT_STATE_MANA)) .. " " .. GetUnitAbilityLevel(caster, FourCC("BEim")) end
            local function at(s, f) TimerStart(CreateTimer(), s, false, f) end
            print("off before", tostring(IssueImmediateOrder(caster, "unimmolation")))
            IssueImmediateOrder(caster, "immolation")
            at(4.6, function() print("on", state()) print("off", tostring(IssueImmediateOrder(caster, "unimmolation"))) end)
            at(7, function()
                print("after", state())
                SetUnitState(caster, UNIT_STATE_MANA, 24)
                IssueImmediateOrder(caster, "immolation")
            end)
            at(10, function() print("out", state()) end)
            """, 11);

        Assert.Equal(["off before\tfalse", "on\t50.0 50.0 0.0 10.0 457.8 1", "off\ttrue", "after\t50.0 50.0 0.0 10.0 457.8 0", "out\t70.0 70.0 0.0 20.0 0.1 0"], lines);
    }

    // Probe abilities-spells: the images come DataD seconds in, illusions with the caster's life and mana that deal DataB of their damage and take
    // DataC times a hit, and die when the ability's time is over. Probe abilities-cast-timing: a summon event for each image and one with no unit
    [Fact]
    public void Mirror_image_makes_illusions_that_deal_nothing_take_double_and_end_with_the_abilitys_time()
    {
        var lines = Run("""
            local caster, foe = CreateUnit(Player(0), FourCC("hatk"), 0, 0, 0), make(Player(1), 2000, 0)
            SetWidgetLife(caster, 800) SetUnitState(caster, UNIT_STATE_MANA, 300)
            UnitAddAbility(caster, FourCC("AOmi"))
            local summons, unitless, t = 0, 0, CreateTrigger()
            TriggerRegisterPlayerUnitEvent(t, Player(0), EVENT_PLAYER_UNIT_SUMMON, nil)
            TriggerAddAction(t, function() summons = summons + 1 if GetSummonedUnit() == nil and GetSummoningUnit() == caster then unitless = unitless + 1 end end)
            local function images()
                local g, found = CreateGroup(), {}
                GroupEnumUnitsOfPlayer(g, Player(0), nil)
                ForGroup(g, function() if GetEnumUnit() ~= caster and UnitAlive(GetEnumUnit()) then found[#found + 1] = GetEnumUnit() end end)
                DestroyGroup(g)
                return found
            end
            local function at(s, f) TimerStart(CreateTimer(), s, false, f) end
            IssueImmediateOrder(caster, "mirrorimage")
            at(0.4, function() print("before", #images(), summons) end)
            at(1, function()
                local image = images()[1]
                print("images", #images(), summons, tostring(IsUnitIllusion(image)), tostring(IsUnitIllusion(caster)), f1(GetWidgetLife(image)), f1(GetUnitState(image, UNIT_STATE_MANA)))
                UnitDamageTarget(foe, image, 100, false, false, ATTACK_TYPE_CHAOS, DAMAGE_TYPE_UNIVERSAL, WEAPON_TYPE_WHOKNOWS)
                UnitDamageTarget(foe, caster, 100, false, false, ATTACK_TYPE_CHAOS, DAMAGE_TYPE_UNIVERSAL, WEAPON_TYPE_WHOKNOWS)
                UnitDamageTarget(image, foe, 100, false, false, ATTACK_TYPE_CHAOS, DAMAGE_TYPE_UNIVERSAL, WEAPON_TYPE_WHOKNOWS)
                print("hits", f1(GetWidgetLife(image)), f1(GetWidgetLife(caster)), f1(GetWidgetLife(foe)))
            end)
            at(20.4, function() print("near the end", #images(), unitless) end)
            at(20.8, function() print("ended", #images()) end)
            """, 21);

        Assert.Equal(["before\t0\t0", "images\t2\t3\ttrue\tfalse\t800.0\t290.0", "hits\t600.0\t700.0\t1000.0", "near the end\t2\t1", "ended\t0"], lines);
    }

    [Fact]
    public void Thorns_return_melee_damage()
    {
        var thorns = Run("""
            local target, attacker = make(Player(0)), CreateUnit(Player(1), FourCC("hatk"), 40, 0, 0)
            UnitAddAbility(target, FourCC("AEah"))
            IssueTargetOrder(attacker, "attack", target)
            TimerStart(CreateTimer(), 3, false, function() print("thorns", GetWidgetLife(attacker) < 1000) end)
            """, 4);
        Assert.Equal(["thorns\ttrue"], thorns);
    }

    // Probe abilities-spells: channelled; from DataA seconds in and every second the buildings at the place take DataB and the other foes there are
    // slowed by DataC for two seconds, and nothing more comes once the caster is ordered to stop
    [Fact]
    public void Earthquake_hurts_buildings_and_slows_units_at_the_place_while_it_is_channelled()
    {
        var lines = Run("""
            local caster, foe, hall, far = make(Player(0)), make(Player(1), 300, 100), CreateUnit(Player(1), FourCC("hbld"), 300, 0, 0), make(Player(1), 300, 400)
            UnitAddAbility(caster, FourCC("AOeq"))
            IssuePointOrder(caster, "earthquake", 300, 0)
            local function state() return f1(1000 - GetWidgetLife(foe)) .. " " .. f1(1000 - GetWidgetLife(hall)) .. " " .. f1(GetUnitMoveSpeed(foe)) .. " " .. GetUnitAbilityLevel(foe, FourCC("BOeq")) .. " " .. f1(GetUnitMoveSpeed(far)) end
            local function at(s, f) TimerStart(CreateTimer(), s, false, f) end
            at(0.4, function() print("before", state()) end)
            at(3.3, function() print("shaking", state()) IssueImmediateOrder(caster, "stop") end)
            at(4.4, function() print("still slow", state()) end)
            at(6, function() print("over", state()) end)
            """, 7, "MinUnitSpeed=150");

        Assert.Equal(["before\t0.0 0.0 270.0 0 270.0", "shaking\t0.0 60.0 150.0 1 270.0", "still slow\t0.0 60.0 150.0 1 270.0", "over\t0.0 60.0 270.0 0 270.0"], lines);
    }

    // The constants the game's data has (probe abilities-banish): a slowed unit keeps MinUnitSpeed, magic and spells reach an ethereal unit for 1.66
    // times, and here every attack type's row but magic's leaves a hit as it is
    const string Ethereal = "MinUnitSpeed=150;EtherealDamageBonus=0,0,0,1.66,0,1.66,0;DamageBonusMagic=2,2,2,2,2,2,2,2";

    [Fact]
    public void Banish_makes_a_unit_ethereal_and_half_as_fast_for_its_time_and_a_hero_for_its_own()
    {
        var lines = Run("""
            local caster, foe, hero, fast = make(Player(0)), make(Player(1), 300, 0), CreateUnit(Player(1), FourCC("Hfoo"), 300, 200, 0), make(Player(0), 300, -200)
            SetUnitMoveSpeed(fast, 400)
            UnitAddAbility(caster, FourCC("AHbn"))
            local function state(u) return f1(GetUnitMoveSpeed(u)) .. " " .. tostring(IsUnitType(u, UNIT_TYPE_ETHEREAL)) .. " " .. GetUnitAbilityLevel(u, FourCC("BHbn")) end
            local function at(s, f) TimerStart(CreateTimer(), s, false, f) end
            IssueTargetOrder(caster, "banish", foe)
            at(1, function() print(state(foe)) end)
            at(1.5, function() IssueTargetOrder(caster, "banish", hero) end)
            at(3, function() IssueTargetOrder(caster, "banish", fast) end)
            at(3.5, function() print(state(fast)) end)
            at(5.5, function() print(state(hero)) end)
            at(5.9, function() print(state(hero)) end)
            at(12, function() print(state(foe)) end)
            at(12.4, function() print(state(foe)) end)
            """, 13, Ethereal);

        Assert.Equal(["150.0 true 1", "200.0 true 1", "150.0 true 1", "270.0 false 0", "150.0 true 1", "270.0 false 0"], lines);
    }

    [Fact]
    public void Only_magic_and_spells_reach_an_ethereal_unit_and_an_attack_types_row_cuts_every_damage_type()
    {
        var lines = Run("""
            local caster, foe, other = make(Player(0)), make(Player(1), 300, 0), make(Player(1), 300, 300)
            UnitAddAbility(caster, FourCC("AHbn"))
            IssueTargetOrder(caster, "banish", foe)
            TimerStart(CreateTimer(), 1, false, function()
                local events = 0
                local t = CreateTrigger()
                TriggerRegisterUnitEvent(t, foe, EVENT_UNIT_DAMAGED)
                TriggerAddAction(t, function() events = events + 1 end)
                local function lose(u, attack, damage)
                    SetWidgetLife(u, 900)
                    UnitDamageTarget(caster, u, 100, false, false, ConvertAttackType(attack), ConvertDamageType(damage), WEAPON_TYPE_WHOKNOWS)
                    return f1(900 - GetWidgetLife(u))
                end
                print(lose(foe, 0, 14), lose(foe, 0, 4), lose(foe, 1, 14), lose(foe, 4, 4), lose(foe, 4, 26), lose(foe, 6, 14), events)
                print(lose(other, 0, 14), lose(other, 4, 14), lose(other, 4, 4))
            end)
            """, 2, Ethereal);

        Assert.Equal(["166.0\t0.0\t0.0\t332.0\t332.0\t0.0\t3", "100.0\t200.0\t200.0"], lines);
    }

    [Fact]
    public void A_banished_unit_takes_a_hit_of_nothing_as_the_buff_comes_and_goes_and_no_attack_but_a_magic_one()
    {
        var lines = Run("""
            local caster, foe = make(Player(0)), make(Player(1), 300, 0)
            UnitAddAbility(caster, FourCC("AHbn"))
            local t = CreateTrigger()
            TriggerRegisterUnitEvent(t, foe, EVENT_UNIT_DAMAGED)
            TriggerAddAction(t, function() print("hit", f1(GetEventDamage())) end)
            IssueTargetOrder(caster, "banish", foe)
            TimerStart(CreateTimer(), 1, false, function()
                print("melee", IssueTargetOrder(CreateUnit(Player(0), FourCC("hatk"), 340, 0, 0), "attack", foe))
                print("magic", IssueTargetOrder(CreateUnit(Player(0), FourCC("hmag"), 260, 0, 0), "attack", foe))
            end)
            """, 2.4, Ethereal);

        // 11 of attack type magic: twice by its row, 1.66 times for the ethereal unit
        Assert.Equal(["hit\t0.0", "melee\ttrue", "magic\ttrue", "hit\t36.5"], lines);

        var after = Run("""
            local caster, foe = make(Player(0)), make(Player(1), 300, 0)
            UnitAddAbility(caster, FourCC("AHbn"))
            IssueTargetOrder(caster, "banish", foe)
            TimerStart(CreateTimer(), 12, false, function()
                local t = CreateTrigger()
                TriggerRegisterUnitEvent(t, foe, EVENT_UNIT_DAMAGED)
                TriggerAddAction(t, function() print("hit", f1(GetEventDamage())) end)
            end)
            """, 12.4, Ethereal);

        Assert.Equal(["hit\t0.0"], after);
    }

    [Fact]
    public void A_banished_unit_attacks_nothing_and_neither_does_a_unit_without_its_attack()
    {
        var lines = Run("""
            local caster, victim, attacker = make(Player(0)), make(Player(0), 300, 40), CreateUnit(Player(1), FourCC("hatk"), 300, 0, 0)
            UnitAddAbility(caster, FourCC("AHbn"))
            local hits = 0
            local t = CreateTrigger()
            TriggerRegisterUnitEvent(t, victim, EVENT_UNIT_DAMAGED)
            TriggerAddAction(t, function() hits = hits + 1 end)
            IssueTargetOrder(attacker, "attack", victim)
            local function at(s, f) TimerStart(CreateTimer(), s, false, f) end
            at(2, function() print("before", hits > 0) IssueTargetOrder(caster, "banish", attacker) end)
            at(3.2, function() print("order", GetUnitCurrentOrder(attacker)) hits = 0 end)
            at(14, function() print("banished", hits) end)
            at(14.5, function() IssueTargetOrder(attacker, "attack", victim) end)
            at(16, function() print("after", hits > 0, UnitRemoveAbility(attacker, FourCC("Aatk"))) end)
            at(17, function() hits = 0 IssueTargetOrder(attacker, "attack", victim) end)
            at(19, function() print("disarmed", hits, GetUnitCurrentOrder(attacker)) end)
            """, 20, Ethereal);

        Assert.Equal(["before\ttrue", "order\t0", "banished\t0", "after\ttrue\ttrue", "disarmed\t0\t0"], lines);
    }

    [Fact]
    public void Carrion_beetles_take_a_corpse_as_target_or_one_near_by_the_instant_order_once_its_death_time_is_over()
    {
        var lines = Run("""
            local caster, body = make(Player(0)), make(Player(1), 200, 0)
            UnitAddAbility(caster, FourCC("AUcb"))
            KillUnit(body)
            local function orders(target)
                return tostring(IssueTargetOrder(caster, "carrionscarabs", target)) .. " " .. tostring(IssueImmediateOrder(caster, "carrionscarabs")) .. " " .. tostring(IssueImmediateOrder(caster, "carrionscarabsinstant"))
            end
            local function beetles()
                local g, n = CreateGroup(), 0
                GroupEnumUnitsOfPlayer(g, Player(0), nil)
                ForGroup(g, function() if GetUnitTypeId(GetEnumUnit()) == FourCC("hbet") and UnitAlive(GetEnumUnit()) then n = n + 1 end end)
                DestroyGroup(g)
                return n
            end
            local t = CreateTrigger()
            TriggerRegisterPlayerUnitEvent(t, Player(0), EVENT_PLAYER_UNIT_SUMMON, nil)
            TriggerAddAction(t, function() print("summon", GetUnitTypeId(GetSummonedUnit()) == FourCC("hbet"), GetSummoningUnit() == caster) end)
            local e = CreateTrigger()
            TriggerRegisterUnitEvent(e, caster, EVENT_UNIT_SPELL_EFFECT)
            TriggerAddAction(e, function() print("effect", GetSpellTargetUnit() == body, GetUnitState(caster, UNIT_STATE_MANA)) end)
            local function at(s, f) TimerStart(CreateTimer(), s, false, f) end
            print("fresh", orders(body))
            at(2.9, function() print("fresh", orders(body)) end)
            at(3.1, function() print("corpse", orders(body)) end)
            at(4, function() print(beetles(), GetUnitTypeId(body), "none", orders(body)) end)
            at(5, function()
                local own = CreateCorpse(Player(0), FourCC("hfoo"), 0, 200, 0)
                print("made", IssueTargetOrder(caster, "carrionscarabs", own))
            end)
            at(5.3, function() CreateCorpse(Player(1), FourCC("hfoo"), 0, 300, 0) print("cooling", IssueImmediateOrder(caster, "carrionscarabsinstant")) end)
            at(60, function() print(beetles()) end)
            """, 61);

        Assert.Equal(["fresh\tfalse false false", "fresh\tfalse false false", "effect\ttrue\t490.0", "corpse\ttrue false false", "summon\ttrue\ttrue", "1\t0\tnone\tfalse false false",
            "effect\tfalse\t480.0", "made\ttrue", "summon\ttrue\ttrue", "cooling\tfalse", "2"], lines);
    }

    [Fact]
    public void A_beetle_over_the_casters_most_dies_as_it_comes_and_autocast_takes_each_corpse_when_the_cooldown_is_over()
    {
        var lines = Run("""
            local caster = make(Player(0))
            UnitAddAbility(caster, FourCC("AUcb"))
            for i = 1, 5 do CreateCorpse(Player(1), FourCC("hfoo"), 100 * i, 0, 0) end
            local summons, deaths = 0, 0
            local t = CreateTrigger()
            TriggerRegisterPlayerUnitEvent(t, Player(0), EVENT_PLAYER_UNIT_SUMMON, nil)
            TriggerAddAction(t, function() summons = summons + 1 end)
            local d = CreateTrigger()
            TriggerRegisterPlayerUnitEvent(d, Player(0), EVENT_PLAYER_UNIT_DEATH, nil)
            TriggerAddAction(d, function() deaths = deaths + 1 end)
            for n = 0, 4 do
                TimerStart(CreateTimer(), 1.5 * n, false, function() IssueImmediateOrder(caster, "carrionscarabsinstant") end)
                TimerStart(CreateTimer(), 1.5 * n + 1, false, function() print(GetPlayerUnitTypeCount(Player(0), FourCC("hbet")), summons, deaths) end)
            end
            """, 8);

        Assert.Equal(["1\t1\t0", "2\t2\t0", "3\t3\t0", "3\t4\t1", "3\t5\t2"], lines);

        var auto = Run("""
            local caster = make(Player(0))
            UnitAddAbility(caster, FourCC("AUcb"))
            for i = 1, 2 do CreateCorpse(Player(1), FourCC("hfoo"), 100 * i, 0, 0) end
            local function beetles() return GetPlayerUnitTypeCount(Player(0), FourCC("hbet")) end
            TimerStart(CreateTimer(), 1, false, function() print(beetles(), "on", IssueImmediateOrder(caster, "carrionscarabson")) end)
            TimerStart(CreateTimer(), 1.5, false, function() print(beetles()) end)
            TimerStart(CreateTimer(), 4, false, function() print(beetles(), "off", IssueImmediateOrder(caster, "carrionscarabsoff")) CreateCorpse(Player(1), FourCC("hfoo"), 300, 0, 0) end)
            TimerStart(CreateTimer(), 7, false, function() print(beetles()) end)
            """, 8);

        Assert.Equal(["0\ton\ttrue", "1", "2\toff\ttrue", "2"], auto);
    }

    [Fact]
    public void Locust_swarm_sends_its_locusts_out_one_by_one_and_gives_the_caster_its_share_of_what_each_four_hits_took()
    {
        var lines = Run("""
            local caster, near, mid, far = make(Player(0)), make(Player(1), 300, 0), make(Player(1), 500, 0), make(Player(1), 1000, 0)
            UnitAddAbility(caster, FourCC("AUls"))
            SetWidgetLife(caster, 100)
            local t = CreateTrigger()
            TriggerRegisterPlayerUnitEvent(t, Player(0), EVENT_PLAYER_UNIT_SUMMON, nil)
            TriggerAddAction(t, function() print("summon") end)
            local d = CreateTrigger()
            TriggerRegisterUnitEvent(d, near, EVENT_UNIT_DAMAGED)
            TriggerAddAction(d, function()
                if d then print("hit", f1(GetEventDamage()), GetUnitTypeId(GetEventDamageSource()) == FourCC("uloc"), BlzGetEventIsAttack(), GetHandleId(BlzGetEventAttackType())) end
                d = nil
            end)
            local function locusts()
                local g, n = CreateGroup(), 0
                GroupEnumUnitsOfPlayer(g, Player(0), nil)
                ForGroup(g, function() if GetUnitTypeId(GetEnumUnit()) == FourCC("uloc") and UnitAlive(GetEnumUnit()) then n = n + 1 end end)
                DestroyGroup(g)
                return n
            end
            local function at(s, f) TimerStart(CreateTimer(), s, false, f) end
            IssueImmediateOrder(caster, "locustswarm")
            at(0.25, function() print(locusts()) end)
            at(0.65, function() print(locusts()) end)
            at(1.2, function() print(locusts()) end)
            at(11, function() print(f1(1000 - GetWidgetLife(near)), f1(1000 - GetWidgetLife(mid)), f1(1000 - GetWidgetLife(far)), f1(GetWidgetLife(caster))) end)
            at(12.5, function() print(f1(GetWidgetLife(caster))) end)
            at(12.7, function() print(f1(GetWidgetLife(caster)), f1(2000 - GetWidgetLife(near) - GetWidgetLife(mid)), locusts()) end)
            at(14.5, function() print(locusts()) end)
            """, 15);

        // Five locusts 0.2 s apart; two take the nearest foe and the rest the next (DataC 2), 13 a hit; the first two are back by 12.5 with 52 each, and
        // as the swarm ends the caster has 0.75 of all 260
        Assert.Equal(["1", "3", "5", "hit\t13.0\ttrue\ttrue\t0", "78.0\t117.0\t0.0\t100.0", "178.0", "295.0\t260.0\t5", "0"], lines);
    }
}
