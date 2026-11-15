using System.Buffers.Binary;
using MirrorImaged.Data;

namespace MirrorImaged.Natives;

// A unit's weapons: the attack fields BlzGetUnitWeapon*Field and BlzSetUnitWeapon*Field read and set (the weapon index 0 or 1
// picks the data's digit: ua1b, ua2b), the base damage, dice and cooldown, and BlzUnitHeal (game-facts, "Units" and
// "Damage"; probe units, 11-08), and the doodads the map places.
public sealed partial class GameNatives
{
    const string WeaponFacts = "game-facts.md, Units: ua1t and ua1g read through the weapon fields (10-20); BlzGetUnitBaseDamage (11-08)";

    // What the data writes for an attack type and the numbers JASS's ATTACK_TYPE_* gives (the footman's normal reads 1)
    static readonly Dictionary<string, int> AttackWords = Words("spells 0 normal 1 pierce 2 siege 3 magic 4 chaos 5 hero 6");

    // The weapon sounds and WEAPON_TYPE_*'s numbers, in common.j's order
    static readonly string[] WeaponSounds =
    [
        "", "MetalLightChop", "MetalMediumChop", "MetalHeavyChop", "MetalLightSlice", "MetalMediumSlice", "MetalHeavySlice", "MetalMediumBash",
        "MetalHeavyBash", "MetalMediumStab", "MetalHeavyStab", "WoodLightSlice", "WoodMediumSlice", "WoodHeavySlice", "WoodLightBash",
        "WoodMediumBash", "WoodHeavyBash", "WoodLightStab", "WoodMediumStab", "ClawLightSlice", "ClawMediumSlice", "ClawHeavySlice", "AxeMediumChop", "RockHeavyBash",
    ];

    // A weapon field's code for the weapon: the digit its name carries (ua1b, umh1) becomes the weapon's number; the attacks
    // enabled field (uaen) has none
    static int WeaponCode(int field, int index)
    {
        var name = Name(field);
        var at = char.IsDigit(name[2]) ? 2 : char.IsDigit(name[3]) ? 3 : -1;
        return at < 0 ? field : Code(name.Remove(at, 1).Insert(at, (index + 1).ToString()));
    }

    // Weapons 1 and 2 only: anything else is the first, as the game reads an index it has no weapon for (not measured)
    static int WeaponIndex(int index) => index == 1 ? 1 : 0;

    /// <summary>
    /// A weapon's integer field: the data's number, or JASS's number for its word (attack type: the footman's normal reads 1,
    /// magic 4; the weapon sound), or the flags of the words of the targets it allows (the footman's 314; "air,enemies" 65540).
    /// </summary>
    [Native(Level.Close, Fact = WeaponFacts)]
    public int BlzGetUnitWeaponIntegerField(unit? whichUnit, unitweaponintegerfield? whichField, int index)
    {
        if (whichUnit is null || whichField is null) return 0;
        var code = WeaponCode(whichField.Id, WeaponIndex(index));
        if (whichUnit.Fields.TryGetValue(code, out var own) && own is int set) return set;
        var type = whichUnit.Type;
        var name = Name(whichField.Id);
        return name switch
        {
            "ua1t" => AttackWords.GetValueOrDefault(type.Text(code) ?? "", 0),
            "ua1g" or "ua1p" => Flags(type.Text(code), AttackTargets),
            "ucs1" => Math.Max(Array.FindIndex(WeaponSounds, sound => sound.Equals(type.Text(code), StringComparison.OrdinalIgnoreCase)), 0),
            _ => type.Integer(code),
        };
    }

    // The targets of a weapon: the unit targets and the enemies word the attack columns add
    static readonly Dictionary<string, int> AttackTargets = new(TargetFlags, StringComparer.OrdinalIgnoreCase) { ["enemies"] = 65536 };

    [Native(Level.Close, Fact = WeaponFacts)]
    public bool BlzSetUnitWeaponIntegerField(unit? whichUnit, unitweaponintegerfield? whichField, int index, int value)
    {
        if (whichUnit is null || whichField is null) return false;
        whichUnit.Fields[WeaponCode(whichField.Id, WeaponIndex(index))] = value;
        return true;
    }

    [Native(Level.Close, Fact = WeaponFacts)]
    public float BlzGetUnitWeaponRealField(unit? whichUnit, unitweaponrealfield? whichField, int index)
    {
        if (whichUnit is null || whichField is null) return 0;
        var code = WeaponCode(whichField.Id, WeaponIndex(index));
        return whichUnit.Fields.TryGetValue(code, out var own) && own is float set ? set : whichUnit.Type.Real(code);
    }

    [Native(Level.Close, Fact = WeaponFacts)]
    public bool BlzSetUnitWeaponRealField(unit? whichUnit, unitweaponrealfield? whichField, int index, float value)
    {
        if (whichUnit is null || whichField is null) return false;
        whichUnit.Fields[WeaponCode(whichField.Id, WeaponIndex(index))] = value;
        return true;
    }

    /// <summary>The attacks enabled field is a bit of the weapon in the data's bit mask; the others read the data's 0 or 1.</summary>
    [Native(Level.Close, Fact = WeaponFacts)]
    public bool BlzGetUnitWeaponBooleanField(unit? whichUnit, unitweaponbooleanfield? whichField, int index)
    {
        if (whichUnit is null || whichField is null) return false;
        var code = WeaponCode(whichField.Id, WeaponIndex(index));
        if (whichUnit.Fields.TryGetValue(code, out var own) && own is bool set) return set;
        return Name(whichField.Id) == "uaen" ? (whichUnit.Type.Integer(WeaponsOn) & (1 << WeaponIndex(index))) != 0 : whichUnit.Type.Integer(code) != 0;
    }

    [Native(Level.Close, Fact = WeaponFacts)]
    public bool BlzSetUnitWeaponBooleanField(unit? whichUnit, unitweaponbooleanfield? whichField, int index, bool value)
    {
        if (whichUnit is null || whichField is null) return false;
        whichUnit.Fields[WeaponCode(whichField.Id, WeaponIndex(index))] = value;
        return true;
    }

    [Native(Level.Close, Fact = WeaponFacts)]
    public string? BlzGetUnitWeaponStringField(unit? whichUnit, unitweaponstringfield? whichField, int index)
    {
        if (whichUnit is null || whichField is null) return null;
        var code = WeaponCode(whichField.Id, WeaponIndex(index));
        return whichUnit.Fields.TryGetValue(code, out var own) && own is string set ? set : whichUnit.Type.Text(code) ?? "";
    }

    [Native(Level.Close, Fact = WeaponFacts)]
    public bool BlzSetUnitWeaponStringField(unit? whichUnit, unitweaponstringfield? whichField, int index, string? value)
    {
        if (whichUnit is null || whichField is null || value is null) return false;
        whichUnit.Fields[WeaponCode(whichField.Id, WeaponIndex(index))] = value;
        return true;
    }

    int WeaponInt(unit u, string suffix, int index) =>
        BlzGetUnitWeaponIntegerField(u, Convert<unitweaponintegerfield>(Code($"ua1{suffix}")), index);

    /// <summary>The weapon's base damage; a hero's counts its primary attribute too (22 strength reads 23 with +1: facts, 11-08).</summary>
    [Native(Level.Close, Fact = WeaponFacts)]
    public int BlzGetUnitBaseDamage(unit? whichUnit, int weaponIndex) =>
        whichUnit is null ? 0 : WeaponInt(whichUnit, "b", weaponIndex) + (IsHero(whichUnit.Type.Id) ? Whole(Attribute(whichUnit)) : 0);

    // The hero's primary attribute: strength, intelligence or agility, as the data names it (strength where it names none)
    double Attribute(unit hero) => Attributes.GetValueOrDefault(hero.Type.Text(Primary) ?? "", 1) switch { 2 => hero.Int, 3 => hero.Agi, _ => hero.Str };

    [Native(Level.Close, Fact = WeaponFacts)]
    public void BlzSetUnitBaseDamage(unit? whichUnit, int baseDamage, int weaponIndex)
    {
        if (whichUnit is not null) BlzSetUnitWeaponIntegerField(whichUnit, Convert<unitweaponintegerfield>(Code("ua1b")), weaponIndex, baseDamage - (IsHero(whichUnit.Type.Id) ? Whole(Attribute(whichUnit)) : 0));
    }

    [Native(Level.Close, Fact = WeaponFacts)]
    public int BlzGetUnitDiceNumber(unit? whichUnit, int weaponIndex) => whichUnit is null ? 0 : WeaponInt(whichUnit, "d", weaponIndex);

    [Native(Level.Close, Fact = WeaponFacts)]
    public void BlzSetUnitDiceNumber(unit? whichUnit, int diceNumber, int weaponIndex) =>
        BlzSetUnitWeaponIntegerField(whichUnit, Convert<unitweaponintegerfield>(Code("ua1d")), weaponIndex, diceNumber);

    [Native(Level.Close, Fact = WeaponFacts)]
    public int BlzGetUnitDiceSides(unit? whichUnit, int weaponIndex) => whichUnit is null ? 0 : WeaponInt(whichUnit, "s", weaponIndex);

    [Native(Level.Close, Fact = WeaponFacts)]
    public void BlzSetUnitDiceSides(unit? whichUnit, int diceSides, int weaponIndex) =>
        BlzSetUnitWeaponIntegerField(whichUnit, Convert<unitweaponintegerfield>(Code("ua1s")), weaponIndex, diceSides);

    /// <summary>The base cooldown, not the bonus attack speed gives (facts: 1.35 with and without Gloves of Haste, 10-19).</summary>
    [Native(Level.Close, Fact = WeaponFacts)]
    public float BlzGetUnitAttackCooldown(unit? whichUnit, int weaponIndex) =>
        BlzGetUnitWeaponRealField(whichUnit, Convert<unitweaponrealfield>(Code("ua1c")), weaponIndex);

    [Native(Level.Close, Fact = WeaponFacts)]
    public void BlzSetUnitAttackCooldown(unit? whichUnit, float cooldown, int weaponIndex) =>
        BlzSetUnitWeaponRealField(whichUnit, Convert<unitweaponrealfield>(Code("ua1c")), weaponIndex, cooldown);

    [Native(Level.Recorded, Reason = "Resets an attack in progress, which needs combat (wave 3)")]
    public void BlzResetUnitAttack(unit? whichUnit, int weaponIndex) { }

    [Native(Level.Recorded, Reason = "Interrupts an attack in progress, which needs combat (wave 3)")]
    public void BlzUnitInterruptAttack(unit? whichUnit) { }

    /// <summary>
    /// Heals the amount (a negative one takes life: -30 takes 30) and answers it; a nil source heals nothing and answers 0
    /// (facts, Damage). The flags change nothing seen.
    /// </summary>
    [Native(Level.Exact, Fact = "game-facts.md, Units: BlzUnitHeal (10-19)")]
    public float BlzUnitHeal(unit? whichUnit, unit? source, float life, bool isItem, bool applyStatBonuses)
    {
        if (whichUnit is null || source is null) return 0;
        if (!whichUnit.Dead) SetLife(whichUnit, whichUnit.Life + life, life < 0 ? source : null);
        return life;
    }

    // The placed doodads that aren't destructables, in the file's order (BlzGetNumDoodads and its index)
    List<War3Net.Build.Widget.DoodadData> PlacedDoodads() =>
        [.. (Data.Map?.Files.Doodads?.Doodads ?? []).Where(placed => Data.Object(ObjectKind.Doodad, BinaryPrimitives.ReverseEndianness(placed.TypeId)) is not null)];

    War3Net.Build.Widget.DoodadData? Doodad(int index) => PlacedDoodads() is var all && index >= 0 && index < all.Count ? all[index] : null;

    [Native(Level.Close, Fact = "game-facts.md, World Editor and map files: doodads (11-09)")]
    public int BlzGetNumDoodads() => PlacedDoodads().Count;

    [Native(Level.Close)] public int BlzGetDoodadId(int index) => Doodad(index) is { } d ? BinaryPrimitives.ReverseEndianness(d.TypeId) : 0;
    [Native(Level.Close)] public int BlzGetDoodadVariation(int index) => Doodad(index)?.Variation ?? 0;
    [Native(Level.Close)] public float BlzGetDoodadX(int index) => Doodad(index)?.Position.X ?? 0;
    [Native(Level.Close)] public float BlzGetDoodadY(int index) => Doodad(index)?.Position.Y ?? 0;
    [Native(Level.Close)] public float BlzGetDoodadZ(int index) => Doodad(index)?.Position.Z ?? 0;
    [Native(Level.Close)] public float BlzGetDoodadYaw(int index) => Doodad(index)?.Rotation ?? 0;
    [Native(Level.Close)] public float BlzGetDoodadPitch(int index) => Doodad(index)?.Pitch ?? 0;
    [Native(Level.Close)] public float BlzGetDoodadRoll(int index) => Doodad(index)?.Roll ?? 0;
    [Native(Level.Close)] public float BlzGetDoodadScaleX(int index) => Doodad(index)?.Scale.X ?? 0;
    [Native(Level.Close)] public float BlzGetDoodadScaleY(int index) => Doodad(index)?.Scale.Y ?? 0;
    [Native(Level.Close)] public float BlzGetDoodadScaleZ(int index) => Doodad(index)?.Scale.Z ?? 0;

    /// <summary>Whether the doodad turns on its model's axes, which the file's state bits would say; false until measured.</summary>
    [Native(Level.Close)] public bool BlzGetDoodadIsUsingModelAxes(int index) => false;

    [Native(Level.Recorded, Reason = "A doodad's animation is drawn; no native reads it back")]
    public void BlzSetSingleDoodadAnimation(int index, string? animName, bool animRandom) { }

    [Native(Level.Recorded, Reason = "A doodad's colour is drawn; no native reads it back")]
    public void BlzSetSingleDoodadColor(int index, playercolor? whichColor) { }

    [Native(Level.Recorded, Reason = "Cooldowns come with abilities and buffs (wave 4); nothing here reads one back")]
    public void UnitResetCooldown(unit? whichUnit) { }
}
