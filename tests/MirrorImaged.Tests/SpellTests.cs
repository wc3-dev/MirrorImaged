using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>The cast flow: spell orders, range, mana and cooldown, the five spell events and what the modelled spells do.</summary>
public sealed class SpellTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static Field Column(string column, string type, params string[] specific) => new(column, PerLevel.Column, -1, type, false, [.. specific.Select(Ids.Of)]);

    static StandardData Standard()
    {
        var data = new StandardData();
        foreach (var (id, field) in new (string, Field)[]
        {
            ("alev", new("levels", PerLevel.None, -1, "int")), ("code", new("code", PerLevel.None, -1)), ("aord", new("Order", PerLevel.None, -1, "string")),
            ("acas", Column("Cast", "unreal")), ("aran", Column("Rng", "unreal")), ("aare", Column("Area", "unreal")), ("acdn", Column("Cool", "unreal")), ("amcs", Column("Cost", "int")),
            ("Hhb1", Column("DataA", "unreal", "AHhb")), ("Htb1", Column("DataA", "unreal", "AHtb")),
        })
            data.Add(ObjectKind.Ability, id, field);
        return data
            .Add(ObjectKind.Ability, "AHhb", Values("code=AHhb;alev=1;aord=holybolt;acas:1=0.3;aran:1=800;aare:1=0;acdn:1=5;amcs:1=65;Hhb1:1=200"))
            .Add(ObjectKind.Ability, "AHtb", Values("code=AHtb;alev=1;aord=thunderbolt;acas:1=0.3;aran:1=600;aare:1=0;acdn:1=9;amcs:1=75;Htb1:1=100"))
            .Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=420;umpm=100;umpi=100;umvs=270;umvt=foot;uaen=0;ucol=31;udef=0"));
    }

    List<string> Run(string main, double seconds)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            local function f1(x) return string.format("%.1f", x) end
            local function make(p, x, y) return CreateUnit(p or Player(0), FourCC("hfoo"), x or 0, y or 0, 0) end
            local function watch(kind, name)
                local t = CreateTrigger()
                TriggerRegisterPlayerUnitEvent(t, Player(0), kind, nil)
                TriggerAddAction(t, function() print(name, GetUnitName(GetSpellAbilityUnit()), GetSpellAbilityId() == FourCC("AHhb") or GetSpellAbilityId() == FourCC("AHtb"), GetSpellTargetUnit() ~= nil) end)
            end
            function config() end
            function main()
            {{main}}
            end
            """), "", data: Standard());
        simulation.Advance(TimeSpan.FromSeconds(seconds));
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];
    }

    [Fact]
    public void A_spell_order_walks_into_range_and_casts_through_its_events_spending_mana_and_starting_the_cooldown()
    {
        var lines = Run("""
            local caster, ally = make(Player(0), 0, 0), make(Player(0), 1200, 0)
            UnitAddAbility(caster, FourCC("AHhb"))
            SetWidgetLife(ally, 100)
            watch(EVENT_PLAYER_UNIT_SPELL_CHANNEL, "channel")
            watch(EVENT_PLAYER_UNIT_SPELL_CAST, "cast")
            watch(EVENT_PLAYER_UNIT_SPELL_EFFECT, "effect")
            watch(EVENT_PLAYER_UNIT_SPELL_FINISH, "finish")
            watch(EVENT_PLAYER_UNIT_SPELL_ENDCAST, "endcast")
            print(IssueTargetOrder(caster, "holybolt", ally), GetUnitCurrentOrder(caster) ~= 0)
            TimerStart(CreateTimer(), 6, false, function()
                print("after", f1(GetWidgetLife(ally)), f1(GetUnitState(caster, UNIT_STATE_MANA)), GetUnitCurrentOrder(caster), f1(BlzGetUnitAbilityCooldownRemaining(caster, FourCC("AHhb"))) )
                print(IssueTargetOrder(caster, "holybolt", ally))
            end)
            TimerStart(CreateTimer(), 20, false, function() print(IssueTargetOrder(caster, "holybolt", ally), IssueTargetOrder(caster, "holybolt", ally)) end)
            """, 21);

        Assert.Equal("true\ttrue", lines[0]);
        Assert.Equal(["channel\tFootman\ttrue\ttrue", "cast\tFootman\ttrue\ttrue", "effect\tFootman\ttrue\ttrue", "finish\tFootman\ttrue\ttrue", "endcast\tFootman\ttrue\ttrue"], lines.Skip(1).Take(5).ToList());
        Assert.Equal("after\t300.0\t35.0\t0\t0.7", lines[6]);
    }

    [Fact]
    public void A_spell_without_mana_or_during_its_cooldown_is_refused_and_storm_bolt_damages()
    {
        var lines = Run("""
            local caster, foe = make(Player(0), 0, 0), make(Player(1), 300, 0)
            UnitAddAbility(caster, FourCC("AHtb"))
            SetUnitState(caster, UNIT_STATE_MANA, 50)
            print(IssueTargetOrder(caster, "thunderbolt", foe))
            SetUnitState(caster, UNIT_STATE_MANA, 100)
            local d = CreateTrigger()
            TriggerRegisterUnitEvent(d, foe, EVENT_UNIT_DAMAGED)
            TriggerAddAction(d, function() print("damaged", f1(GetEventDamage()), GetHandleId(BlzGetEventDamageType())) end)
            print(IssueTargetOrder(caster, "thunderbolt", foe))
            TimerStart(CreateTimer(), 3, false, function() print(f1(GetWidgetLife(foe)), IssueTargetOrder(caster, "thunderbolt", foe)) end)
            """, 4);

        Assert.Equal(["false", "true", "damaged\t0.0\t0", "damaged\t100.0\t15", "320.0\tfalse"], lines.Where(line => !line.StartsWith("hit\t")).ToList());
    }
}
