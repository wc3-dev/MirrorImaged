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
    ];

    static StandardData Standard()
    {
        var data = new StandardData();
        foreach (var (id, field) in new (string, Field)[]
        {
            ("alev", new("levels", PerLevel.None, -1, "int")), ("code", new("code", PerLevel.None, -1)), ("aord", new("Order", PerLevel.None, -1, "string")),
            ("acas", Column("Cast", "unreal")), ("aran", Column("Rng", "unreal")), ("aare", Column("Area", "unreal")), ("acdn", Column("Cool", "unreal")), ("amcs", Column("Cost", "int")),
            ("adur", Column("Dur", "unreal")), ("ahdu", Column("HeroDur", "unreal")),
        })
            data.Add(ObjectKind.Ability, id, field);
        foreach (var letter in "ABCDE")
            data.Add(ObjectKind.Ability, "Z" + letter + "01", Column("Data" + letter, "unreal", [.. Abilities.Select(a => a.Id)]));
        foreach (var (id, order, numbers) in Abilities)
        {
            var n = Values(numbers + ";x=0");
            var own = $"code={id};alev=1;aord={order};acas:1=0.1;aran:1=900;aare:1={n.GetValueOrDefault("area", "0")};acdn:1=1;amcs:1=10;adur:1={n.GetValueOrDefault("dur", "0")};ahdu:1={n.GetValueOrDefault("dur", "0")}";
            foreach (var letter in "ABCDE") if (n.TryGetValue(letter.ToString(), out var value)) own += $";Z{letter}01:1={value}";
            data.Add(ObjectKind.Ability, id, Values(own));
        }
        return data.Add(ObjectKind.Unit, "hatk", Values("unam=Attacker;uhpm=1000;umpm=500;umpi=500;umvs=270;umvt=foot;uacq=500;uaen=1;ua1w=normal;ua1t=normal;ua1g=ground;ua1b=10;ua1d=1;ua1s=1;ua1c=1;ua1r=90;udp1=0.5;ucol=31;udef=0")).Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=1000;umpm=500;umpi=500;umvs=270;umvt=foot;uacq=500;uaen=0;ua1w=normal;ua1t=normal;ua1g=ground;ua1b=10;ua1d=1;ua1s=1;ua1c=1;ua1r=90;udp1=0.5;ucol=31;udef=0"));
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

        Assert.Equal(4, lines.Count);
        Assert.Contains("far\t270.0\tfoe\t270.0", lines[1]);
        Assert.Equal("after death\t270.0", lines[^1]);
    }
}
