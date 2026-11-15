using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>Weapon fields, base damage, dice and cooldown, and BlzUnitHeal, against the facts' "Units" (game-facts.md, 10-19, 10-20, 11-08).</summary>
public sealed class WeaponTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static StandardData Standard() => new StandardData()
        .Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=420;uaen=1;ua1w=normal;ua1t=normal;ua1g=ground,structure,debris,item,ward;ua1b=11;ua1d=1;ua1s=2;ua1c=1.35;ua1r=90;ua1m=;ucs1=MetalMediumSlice;udty=large;udef=2"))
        .Add(ObjectKind.Unit, "hsor", Values("unam=Sorceress;uhpm=300;uaen=3;ua1t=magic;ua1g=air,enemies;ua1b=5;ua1d=1;ua1s=2;ua1c=1.5;ua2t=pierce;ua2b=9;ua2c=2.5;ucs1=WoodLightBash"))
        .Add(ObjectKind.Unit, "Hpal", Values("unam=Paladin;uhpm=100;ustr=22;uagi=13;uint=17;upra=STR;uaen=1;ua1t=hero;ua1b=2;ua1d=1;ua1s=6"));

    List<string> Run(string main)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            local function f2(x) return string.format("%.2f", x) end
            local function make(id) return CreateUnit(Player(0), FourCC(id), 0, 0, 0) end
            function config() end
            function main()
            {{main}}
            end
            """), "", data: Standard());
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];
    }

    // facts: ua1t reads JASS's attack type numbers (normal 1, magic 4); ua1g reads its flags (footman 314, "air,enemies" 65540)
    [Fact]
    public void Weapon_fields_read_the_attack_columns_as_jass_numbers()
    {
        var lines = Run("""
            local f, s = make("hfoo"), make("hsor")
            local type, targets, sound = UNIT_WEAPON_IF_ATTACK_ATTACK_TYPE, UNIT_WEAPON_IF_ATTACK_TARGETS_ALLOWED, UNIT_WEAPON_IF_ATTACK_WEAPON_SOUND
            print(BlzGetUnitWeaponIntegerField(f, type, 0), BlzGetUnitWeaponIntegerField(s, type, 0), BlzGetUnitWeaponIntegerField(s, type, 1))
            print(BlzGetUnitWeaponIntegerField(f, targets, 0), BlzGetUnitWeaponIntegerField(s, targets, 0))
            print(BlzGetUnitWeaponIntegerField(f, sound, 0), BlzGetUnitWeaponIntegerField(s, sound, 0))
            print(BlzGetUnitWeaponBooleanField(s, UNIT_WEAPON_BF_ATTACKS_ENABLED, 0), BlzGetUnitWeaponBooleanField(s, UNIT_WEAPON_BF_ATTACKS_ENABLED, 1), BlzGetUnitWeaponBooleanField(f, UNIT_WEAPON_BF_ATTACKS_ENABLED, 1))
            print(f2(BlzGetUnitWeaponRealField(f, UNIT_WEAPON_RF_ATTACK_RANGE, 0)), f2(BlzGetUnitWeaponRealField(s, UNIT_WEAPON_RF_ATTACK_BASE_COOLDOWN, 1)))
            print(BlzSetUnitWeaponIntegerField(f, type, 0, 4), BlzGetUnitWeaponIntegerField(f, type, 0), BlzGetUnitWeaponIntegerField(f, type, 1))
            """);

        Assert.Equal(["1\t4\t2", "314\t65540", "5\t14", "true\ttrue\tfalse", "90.00\t2.50", "true\t4\t0"], lines);
    }

    // facts: the base damage, dice and cooldown of a weapon; a hero's base damage includes its primary attribute; the cooldown is the base one
    [Fact]
    public void Base_damage_dice_and_cooldown_read_and_set_per_weapon()
    {
        var lines = Run("""
            local f, s, h = make("hfoo"), make("hsor"), make("Hpal")
            print(BlzGetUnitBaseDamage(f, 0), BlzGetUnitDiceNumber(f, 0), BlzGetUnitDiceSides(f, 0), f2(BlzGetUnitAttackCooldown(f, 0)))
            print(BlzGetUnitBaseDamage(s, 0), BlzGetUnitBaseDamage(s, 1), f2(BlzGetUnitAttackCooldown(s, 1)))
            print(BlzGetUnitBaseDamage(h, 0), BlzGetUnitDiceSides(h, 0))
            BlzSetUnitBaseDamage(f, 20, 0)
            BlzSetUnitDiceNumber(f, 2, 0)
            BlzSetUnitDiceSides(f, 8, 0)
            BlzSetUnitAttackCooldown(f, 0.75, 0)
            BlzSetUnitBaseDamage(s, 30, 1)
            BlzSetUnitBaseDamage(h, 30, 0)
            print(BlzGetUnitBaseDamage(f, 0), BlzGetUnitDiceNumber(f, 0), BlzGetUnitDiceSides(f, 0), f2(BlzGetUnitAttackCooldown(f, 0)))
            print(BlzGetUnitBaseDamage(s, 0), BlzGetUnitBaseDamage(s, 1), BlzGetUnitBaseDamage(h, 0))
            print(BlzGetUnitBaseDamage(make("hfoo"), 0))
            """);

        Assert.Equal(["11\t1\t2\t1.35", "5\t9\t2.50", "24\t6", "20\t2\t8\t0.75", "5\t30\t30", "11"], lines);
    }

    // facts: BlzUnitHeal with a nil source heals nothing and answers 0; with a unit it heals and answers the amount, a negative amount taking life
    [Fact]
    public void Healing_needs_a_source_and_a_negative_amount_takes_life()
    {
        var lines = Run("""
            local u, src = make("hfoo"), make("hsor")
            SetWidgetLife(u, 100)
            print(BlzUnitHeal(u, nil, 50, false, false), GetWidgetLife(u))
            print(BlzUnitHeal(u, src, 50, true, true), GetWidgetLife(u))
            print(BlzUnitHeal(u, src, -30, false, false), GetWidgetLife(u))
            print(BlzUnitHeal(u, src, 1000, false, false), GetWidgetLife(u))
            print(BlzGetNumDoodads(), BlzGetDoodadX(3), BlzGetDoodadId(-1))
            """);

        Assert.Equal(["0.0\t100.0", "50.0\t150.0", "-30.0\t120.0", "1000.0\t300.0".Replace("300.0", "420.0"), "0\t0.0\t0"], lines);
    }
}
