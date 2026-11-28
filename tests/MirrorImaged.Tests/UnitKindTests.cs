using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>
/// The unit kinds a unit's buffs and abilities give it (IsUnitType's stunned, plagued, snared, poisoned, polymorphed,
/// resistant and magic immune), the spells and attacks that give them, the buff flags, and what a pause, a stun and
/// BlzPauseUnitEx hold back and give back.
/// </summary>
public sealed class UnitKindTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(";", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Select(pair => pair.Split("=", 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static Field Column(string column, string type, params string[] specific) => new(column, PerLevel.Column, -1, type, false, [.. specific.Select(Ids.Of)]);

    // Each ability: its code, order string, and its numbers at level 1 (Data A to E, area, duration, hero duration, buffs)
    static readonly (string Id, string Code, string Order, string Numbers)[] Abilities =
    [
        ("Aens", "Aens", "ensnare", "dur=4;hdur=2;buff=Bena,Beng"),
        ("Aweb", "Aweb", "web", "dur=4;buff=Bwea,Bweb"),
        ("Aply", "Aply", "polymorph", "B=nshe;C=nshf;dur=3;buff=Bply"),
        ("Aslo", "Aslo", "slow", "A=0.5;B=0.25;dur=6;hdur=2;buff=Bslo"),
        ("AHav", "AHav", "avatar", "A=5;B=100;dur=5;buff=BHav"),
        ("AUdc", "AUdc", "deathcoil", "A=50"),
        ("Aspo", "Aspo", "", "A=4;B=0.5;C=0.25;dur=5;buff=Bspo"),
        ("Aven", "Aven", "", "A=3;dur=5;buff=Bpoi"),
        ("Aap1", "Aapl", "", "A=4;B=2;area=200;buff=Bapl"),
        ("AHad", "AHad", "", "A=2;area=900"),
        ("Amim", "Amim", "", ""),
        ("ACm2", "Amim", "", ""),
        ("ACrk", "Arsk", "", ""),
        ("AOws", "AOws", "stomp", "area=300;dur=1;buff=BSTN"),
    ];

    static StandardData Standard()
    {
        var data = new StandardData();
        foreach (var (id, field) in new (string, Field)[]
        {
            ("alev", new("levels", PerLevel.None, -1, "int")), ("code", new("code", PerLevel.None, -1)), ("aord", new("Order", PerLevel.None, -1, "string")),
            ("acas", Column("Cast", "unreal")), ("aran", Column("Rng", "unreal")), ("aare", Column("Area", "unreal")), ("acdn", Column("Cool", "unreal")), ("amcs", Column("Cost", "int")),
            ("adur", Column("Dur", "unreal")), ("ahdu", Column("HeroDur", "unreal")), ("abuf", Column("BuffID", "string")),
        })
            data.Add(ObjectKind.Ability, id, field);
        foreach (var letter in "ABCDE")
            data.Add(ObjectKind.Ability, "Z" + letter + "01", Column("Data" + letter, "unreal", [.. Abilities.Select(a => a.Id)]));
        foreach (var (id, code, order, numbers) in Abilities)
        {
            var n = Values(numbers + ";x=0");
            var own = $"code={code};alev=1;aord={order};acas:1=0;aran:1=900;aare:1={n.GetValueOrDefault("area", "0")};acdn:1=1;amcs:1=0;adur:1={n.GetValueOrDefault("dur", "0")};ahdu:1={n.GetValueOrDefault("hdur", n.GetValueOrDefault("dur", "0"))}";
            foreach (var letter in "ABCDE") if (n.TryGetValue(letter.ToString(), out var value)) own += $";Z{letter}01:1={value}";
            if (n.TryGetValue("buff", out var buff)) own += $";abuf:1={buff}";
            data.Add(ObjectKind.Ability, id, Values(own));
        }
        const string Walker = "uhpm=1000;umvs=270;umvt=foot;uacq=500;uaen=1;ua1w=normal;ua1t=normal;ua1g=ground;ua1b=10;ua1d=1;ua1s=1;ua1c=1;ua1r=90;udp1=0.3;ucol=16;udef=0";
        return data.Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;" + Walker))
            .Add(ObjectKind.Unit, "hgry", Values("unam=Gryphon;uhpm=1000;umvs=300;umvt=fly;uaen=0;ucol=16"))
            .Add(ObjectKind.Unit, "nshe", Values("unam=Sheep;uhpm=15;umvs=200;umvt=foot;uaen=0;ucol=16"))
            .Add(ObjectKind.Unit, "nshf", Values("unam=Flying Sheep;uhpm=15;umvs=200;umvt=fly;uaen=0;ucol=16"))
            .Add(ObjectKind.Unit, "Hpal", Values("unam=Hero;" + Walker))
            .Add(ObjectKind.Ability, "Aatk", Values("code=Aatk;alev=1"));
    }

    List<string> Run(string main, double seconds)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            local function f1(x) return string.format("%.1f", x) end
            local function tf(v) return v and "t" or "f" end
            local function make(p, x, y, id) return CreateUnit(p or Player(0), FourCC(id or "hfoo"), x or 0, y or 0, 0) end
            local function still(u) UnitRemoveAbility(u, FourCC("Aatk")) return u end
            local function at(s, f) TimerStart(CreateTimer(), s, false, f) end
            local KINDS = {UNIT_TYPE_STUNNED, UNIT_TYPE_PLAGUED, UNIT_TYPE_SNARED, UNIT_TYPE_POISONED, UNIT_TYPE_POLYMORPHED, UNIT_TYPE_RESISTANT, UNIT_TYPE_ETHEREAL, UNIT_TYPE_MAGIC_IMMUNE}
            local function kinds(u) local s = "" for _, k in ipairs(KINDS) do s = s .. tf(IsUnitType(u, k)) end return s end
            local function orders(label, u)
              local t = CreateTrigger()
              TriggerRegisterUnitEvent(t, u, EVENT_UNIT_ISSUED_ORDER)
              TriggerRegisterUnitEvent(t, u, EVENT_UNIT_ISSUED_POINT_ORDER)
              TriggerRegisterUnitEvent(t, u, EVENT_UNIT_ISSUED_TARGET_ORDER)
              TriggerAddAction(t, function() print("order", label, GetIssuedOrderId(), string.format("%.3f", TimerGetElapsed(clock))) end)
            end
            function config() SetPlayers(2) end
            function main()
            clock = CreateTimer()
            TimerStart(clock, 1000, false, nil)
            {{main}}
            end
            """), "", data: Standard());
        simulation.Advance(TimeSpan.FromSeconds(seconds));
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];
    }

    [Fact]
    public void Resistant_skin_and_spell_immunity_and_their_copies_give_their_kinds_and_a_plain_unit_has_none()
    {
        var lines = Run("""
            local plain, skin, immune, copy = still(make(Player(1))), still(make(Player(1), 200)), still(make(Player(1), 400)), still(make(Player(1), 600))
            UnitAddAbility(skin, FourCC("ACrk"))
            UnitAddAbility(immune, FourCC("Amim"))
            UnitAddAbility(copy, FourCC("ACm2"))
            print(kinds(plain), kinds(skin), kinds(immune), kinds(copy))
            """, 0.1);

        Assert.Equal(["ffffffff\tffffftff\tffffffft\tffffffft"], lines);
    }

    [Fact]
    public void Ensnare_and_web_snare_and_hold_a_unit_on_the_ground_and_in_the_air_with_the_second_buff_of_their_list()
    {
        var lines = Run("""
            local caster = still(make(Player(0)))
            UnitAddAbility(caster, FourCC("Aens"))
            UnitAddAbility(caster, FourCC("Aweb"))
            local walker, flier = still(make(Player(1), 300, 0)), still(make(Player(1), 300, 300, "hgry"))
            IssueTargetOrder(caster, "ensnare", walker)
            IssuePointOrder(walker, "move", 2000, 0)
            at(1, function() print("snared", kinds(walker), f1(GetUnitX(walker)), GetUnitAbilityLevel(walker, FourCC("Beng")), GetUnitAbilityLevel(walker, FourCC("Bena"))) end)
            at(1.1, function() IssueTargetOrder(caster, "web", flier) end)
            at(2, function() print("webbed", kinds(flier), GetUnitAbilityLevel(flier, FourCC("Bwea")), UnitCountBuffsEx(walker, false, true, false, true, false, false, false)) end)
            at(4.5, function() print("free", kinds(walker), GetUnitX(walker) > 300) end)
            """, 5);

        Assert.Equal(["snared\tfftfffff\t300.0\t1\t0", "webbed\tfftfffff\t0\t1", "free\tffffffff\ttrue"], lines);
    }

    [Fact]
    public void Polymorph_keeps_the_units_type_and_it_reads_polymorphed_until_its_time_or_a_dispel()
    {
        var lines = Run("""
            local caster = still(make(Player(0)))
            UnitAddAbility(caster, FourCC("Aply"))
            local a, b, c = still(make(Player(1), 300, 0)), still(make(Player(1), 300, 300)), still(make(Player(1), 300, -300, "hgry"))
            IssueTargetOrder(caster, "polymorph", a)
            at(0.5, function() print("sheep", GetUnitName(a), kinds(a)) IssueTargetOrder(caster, "polymorph", b) end)
            at(1, function() UnitRemoveBuffs(b, false, true) print("dispelled", GetUnitName(b), kinds(b)) IssueTargetOrder(caster, "polymorph", c) end)
            at(1.5, function() print("flier", GetUnitName(c)) end)
            at(3.5, function() print("back", GetUnitName(a), kinds(a)) end)
            """, 4);

        Assert.Equal(["sheep\tFootman\tfffftfff", "dispelled\tFootman\tffffffff", "flier\tGryphon", "back\tFootman\tffffffff"], lines);
    }

    [Fact]
    public void An_attacks_poison_hits_0_before_the_attack_then_each_second_and_slow_poison_slows_and_reads_plagued_too()
    {
        var lines = Run("""
            local attacker, target = make(Player(0)), still(make(Player(1), 100, 0))
            UnitAddAbility(attacker, FourCC("Aspo"))
            local t = CreateTrigger()
            TriggerRegisterUnitEvent(t, target, EVENT_UNIT_DAMAGED)
            TriggerAddAction(t, function() print("hit", f1(GetEventDamage()), GetHandleId(BlzGetEventDamageType()), GetUnitAbilityLevel(target, FourCC("Bspo"))) end)
            IssueTargetOrder(attacker, "attack", target)
            at(0.6, function() UnitRemoveAbility(attacker, FourCC("Aatk")) print("poisoned", kinds(target), f1(GetUnitMoveSpeed(target)), UnitCountBuffsEx(target, false, true, false, true, false, false, false), UnitCountBuffsEx(target, false, true, true, false, false, false, false)) end)
            at(6, function() print("over", kinds(target), f1(GetUnitMoveSpeed(target))) end)
            """, 6.5);

        Assert.Equal(["hit\t0.0\t0\t0", "hit\t11.0\t4\t1", "poisoned\tftftffff\t135.0\t1\t0", "hit\t4.0\t23\t1", "hit\t4.0\t23\t1", "hit\t4.0\t23\t1", "hit\t4.0\t23\t1", "over\tffffffff\t270.0"],
            lines.Where(line => !line.StartsWith("hit\t4.0") || lines.IndexOf(line) >= 0).ToList());
    }

    [Fact]
    public void Disease_cloud_plagues_the_foes_round_its_unit_who_take_its_damage_each_second_after_they_leave()
    {
        var lines = Run("""
            local abomination, foe = still(make(Player(0))), still(make(Player(1), 150, 0))
            UnitAddAbility(abomination, FourCC("Aap1"))
            at(1.5, function() print("plagued", kinds(foe), GetUnitAbilityLevel(foe, FourCC("Bapl")), f1(GetWidgetLife(foe))) SetUnitPosition(foe, 1500, 0) end)
            at(3.5, function() print("away", kinds(foe), f1(GetWidgetLife(foe))) end)
            at(6.5, function() print("over", kinds(foe), f1(GetWidgetLife(foe))) end)
            """, 7);

        // the pulse plagues it at 1 s; 2 a second from 2 s on, for 4 s from the last pulse that reached it
        Assert.Equal(["plagued\tftffffff\t1\t1000.0", "away\tftffffff\t996.0", "over\tffffffff\t994.0"], lines);
    }

    [Fact]
    public void Avatar_makes_its_hero_magic_immune_for_its_time()
    {
        var lines = Run("""
            local hero, foe = still(make(Player(0), 0, 0, "Hpal")), still(make(Player(1), 300, 0))
            UnitAddAbility(hero, FourCC("AHav"))
            UnitAddAbility(foe, FourCC("AUdc"))
            IssueImmediateOrder(hero, "avatar")
            at(0.5, function() print("avatar", kinds(hero)) IssueTargetOrder(foe, "deathcoil", hero) end)
            at(1, function() print("coiled", f1(GetWidgetLife(hero))) end)
            at(6, function() print("over", kinds(hero)) end)
            """, 6.5);

        Assert.Equal(["avatar\tffffffft", "coiled\t1100.0", "over\tffffffff"], lines);
    }

    [Fact]
    public void A_unit_with_resistant_skin_takes_a_spells_hero_duration()
    {
        var lines = Run("""
            local caster, plain, skin = still(make(Player(0))), still(make(Player(1), 300, 0)), still(make(Player(1), 300, 300))
            UnitAddAbility(caster, FourCC("Aslo"))
            UnitAddAbility(skin, FourCC("ACrk"))
            IssueTargetOrder(caster, "slow", plain)
            at(0.5, function() IssueTargetOrder(caster, "slow", skin) end)
            at(3, function() print(GetUnitAbilityLevel(plain, FourCC("Bslo")), GetUnitAbilityLevel(skin, FourCC("Bslo"))) end)
            """, 3.5);

        Assert.Equal(["1\t0"], lines);
    }

    // game-facts, 10-31: one buff of an id, and a cast never shortens it: it ends at the later of the two ends
    [Fact]
    public void A_buff_cast_again_keeps_the_later_end()
    {
        var lines = Run("""
            local caster, foe = still(make(Player(0))), still(make(Player(1), 300, 0))
            UnitAddAbility(caster, FourCC("Aslo"))
            IssueTargetOrder(caster, "slow", foe)
            at(4, function()
              BlzSetAbilityRealLevelField(BlzGetUnitAbility(caster, FourCC("Aslo")), ABILITY_RLF_DURATION_NORMAL, 0, 1)
              IssueTargetOrder(caster, "slow", foe)
            end)
            at(4.5, function() print("again", f1(GetUnitMoveSpeed(foe)), GetUnitAbilityLevel(foe, FourCC("Bslo"))) end)
            at(5.5, function() print("still", GetUnitAbilityLevel(foe, FourCC("Bslo"))) end)
            at(6.5, function() print("over", GetUnitAbilityLevel(foe, FourCC("Bslo"))) end)
            """, 7);

        Assert.Equal(["again\t135.0\t1", "still\t1", "over\t0"], lines);
    }

    // probe units-kinds: an aura's buff counts only with the aura flag; the others ask for their kind; magic and physical together pick none
    [Fact]
    public void The_buff_flags_tell_a_spells_buff_from_an_auras_and_an_attacks_poison()
    {
        var lines = Run("""
            local caster, hero, foe = still(make(Player(0))), still(make(Player(1), 0, 300, "Hpal")), still(make(Player(1), 300, 0))
            UnitAddAbility(caster, FourCC("Aslo"))
            UnitAddAbility(hero, FourCC("AHad"))
            IssueTargetOrder(caster, "slow", foe)
            local function count(u, m, p, t, a, d) return UnitCountBuffsEx(u, true, true, m, p, t, a, d) end
            at(1.5, function()
              print("slowed and in aura", count(foe, false, false, false, false, false), count(foe, true, false, false, false, false), count(foe, false, true, false, false, false),
                count(foe, true, true, false, false, false), count(foe, false, false, true, false, false), count(foe, false, false, false, true, false), count(foe, false, false, false, false, true),
                count(foe, true, true, true, true, true))
              UnitRemoveBuffsEx(foe, true, true, true, false, false, false, false)
              print("magic off", GetUnitAbilityLevel(foe, FourCC("Bslo")), count(foe, false, false, false, true, false))
            end)
            """, 2);

        Assert.Equal(["slowed and in aura\t1\t1\t0\t0\t1\t2\t1\t0", "magic off\t0\t1"], lines);
    }

    // game-facts, 11-03: UnitRemoveAbility of a buff's id answers true and ends it at once
    [Fact]
    public void Removing_a_buffs_id_ends_the_buff_and_its_effect_at_once()
    {
        var lines = Run("""
            local caster, foe = still(make(Player(0))), still(make(Player(1), 300, 0))
            UnitAddAbility(caster, FourCC("Aslo"))
            IssueTargetOrder(caster, "slow", foe)
            at(1, function() print(f1(GetUnitMoveSpeed(foe)), UnitRemoveAbility(foe, FourCC("Bslo")), f1(GetUnitMoveSpeed(foe)), GetUnitAbilityLevel(foe, FourCC("Bslo")), UnitRemoveAbility(foe, FourCC("Bslo"))) end)
            """, 1.5);

        Assert.Equal(["135.0\ttrue\t270.0\t0\tfalse"], lines);
    }

    // probe units-kinds: a paused unit reads the stunned order until its move comes back as an order event a frame after it is let go
    [Fact]
    public void A_paused_unit_reads_stunned_and_gets_its_order_back_after_it_is_let_go_unless_idle_or_given_another()
    {
        var lines = Run("""
            local walker, idle, other = still(make(Player(1))), still(make(Player(1), 0, 300)), still(make(Player(1), 0, -300))
            orders("walker", walker) orders("idle", idle) orders("other", other)
            IssuePointOrder(walker, "move", 2000, 0)
            IssuePointOrder(other, "move", 2000, -300)
            at(0.5, function() PauseUnit(walker, true) PauseUnit(idle, true) PauseUnit(other, true) print("paused", GetUnitCurrentOrder(walker), GetUnitCurrentOrder(idle), kinds(walker)) end)
            at(0.7, function() IssuePointOrder(other, "move", -2000, -300) end)
            at(1, function() local x = GetUnitX(walker) PauseUnit(walker, false) PauseUnit(idle, false) PauseUnit(other, false) print("let go", GetUnitCurrentOrder(walker), f1(x)) end)
            at(2, function() print("walks", GetUnitX(walker) > 200, GetUnitX(other) < 0) end)
            """, 2.5);

        Assert.Equal([
            "order\twalker\t851986\t0.000", "order\tother\t851986\t0.000",
            "order\twalker\t851973\t0.500", "order\tidle\t851973\t0.500", "order\tother\t851973\t0.500", "paused\t851973\t851973\tffffffff",
            "order\tother\t851986\t0.700",
            "let go\t851973\t126.6",
            "order\twalker\t851986\t1.010",
            "walks\ttrue\ttrue",
        ], lines);
    }

    [Fact]
    public void BlzPauseUnitEx_holds_the_unit_with_the_stunned_order_until_its_count_is_back_to_0_and_gives_its_order_back()
    {
        var lines = Run("""
            local walker = still(make(Player(1)))
            orders("walker", walker)
            IssuePointOrder(walker, "move", 2000, 0)
            at(0.5, function() BlzPauseUnitEx(walker, true) BlzPauseUnitEx(walker, true) print("held", GetUnitCurrentOrder(walker), kinds(walker), IsUnitPaused(walker), f1(GetUnitX(walker))) end)
            at(1, function() BlzPauseUnitEx(walker, false) print("once", f1(GetUnitX(walker)), kinds(walker)) end)
            at(1.5, function() BlzPauseUnitEx(walker, false) print("twice", f1(GetUnitX(walker)), kinds(walker)) end)
            at(2.5, function() print("walks", GetUnitX(walker) > 200) end)
            """, 3);

        Assert.Equal([
            "order\twalker\t851986\t0.000", "order\twalker\t851973\t0.500", "held\t851973\tffffffff\tfalse\t126.6", "once\t126.6\tffffffff", "twice\t126.6\tffffffff",
            "order\twalker\t851986\t1.510", "walks\ttrue",
        ], lines);
    }

    // probe units-kinds: Storm Bolt's stun gave the stunned order as a target order at the hit, read stunned to the current order and not to IsUnitType,
    // and the move came back a frame after its end
    [Fact]
    public void A_stun_gives_the_stunned_order_and_its_end_gives_the_order_back()
    {
        var lines = Run("""
            local caster, walker = still(make(Player(0), 0, 100)), still(make(Player(1)))
            UnitAddAbility(caster, FourCC("AOws"))
            orders("walker", walker)
            IssuePointOrder(walker, "move", 2000, 0)
            at(0.5, function() IssueImmediateOrder(caster, "stomp") print("stunned", GetUnitCurrentOrder(walker), kinds(walker), IsUnitPaused(walker)) end)
            at(1, function() print("held", f1(GetUnitX(walker))) end)
            at(2.5, function() print("walks", GetUnitX(walker) > 200, kinds(walker)) end)
            """, 3);

        Assert.Equal([
            "order\twalker\t851986\t0.000", "order\twalker\t851973\t0.500", "stunned\t851973\tffffffff\tfalse", "held\t126.6",
            "order\twalker\t851986\t1.510", "walks\ttrue\tffffffff",
        ], lines);
    }
}
