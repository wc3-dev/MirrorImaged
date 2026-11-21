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
    /// <remarks>Close: measured in part (game-facts.md, Units: ua1t and ua1g read through the weapon fields (10-20); BlzGetUnitBaseDamage (11-08)); the other fields read as the data gives them, and an index other than 1 is the first weapon, not measured.</remarks>
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

    /// <summary>Keeps the value for the weapon's field on the unit, true; false for a nil unit or field.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Units: ua1t and ua1g read through the weapon fields (10-20); BlzGetUnitBaseDamage (11-08)); a value is kept as given, with no check against the field's range, not measured.</remarks>
    [Native(Level.Close, Fact = WeaponFacts)]
    public bool BlzSetUnitWeaponIntegerField(unit? whichUnit, unitweaponintegerfield? whichField, int index, int value)
    {
        if (whichUnit is null || whichField is null) return false;
        whichUnit.Fields[WeaponCode(whichField.Id, WeaponIndex(index))] = value;
        return true;
    }

    /// <summary>The weapon's real field: the value set on the unit, else the data's; 0 for a nil unit or field.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Units: ua1t and ua1g read through the weapon fields (10-20); BlzGetUnitBaseDamage (11-08)); the other fields read as the data gives them, and an index other than 1 is the first weapon, not measured.</remarks>
    [Native(Level.Close, Fact = WeaponFacts)]
    public float BlzGetUnitWeaponRealField(unit? whichUnit, unitweaponrealfield? whichField, int index)
    {
        if (whichUnit is null || whichField is null) return 0;
        var code = WeaponCode(whichField.Id, WeaponIndex(index));
        return whichUnit.Fields.TryGetValue(code, out var own) && own is float set ? set : whichUnit.Type.Real(code);
    }

    /// <summary>Keeps the value for the weapon's field on the unit, true; false for a nil unit or field.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Units: ua1t and ua1g read through the weapon fields (10-20); BlzGetUnitBaseDamage (11-08)); a value is kept as given, with no check against the field's range, not measured.</remarks>
    [Native(Level.Close, Fact = WeaponFacts)]
    public bool BlzSetUnitWeaponRealField(unit? whichUnit, unitweaponrealfield? whichField, int index, float value)
    {
        if (whichUnit is null || whichField is null) return false;
        whichUnit.Fields[WeaponCode(whichField.Id, WeaponIndex(index))] = value;
        return true;
    }

    /// <summary>The attacks enabled field is a bit of the weapon in the data's bit mask; the others read the data's 0 or 1.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Units: ua1t and ua1g read through the weapon fields (10-20); BlzGetUnitBaseDamage (11-08)); the other fields read as the data gives them, and an index other than 1 is the first weapon, not measured.</remarks>
    [Native(Level.Close, Fact = WeaponFacts)]
    public bool BlzGetUnitWeaponBooleanField(unit? whichUnit, unitweaponbooleanfield? whichField, int index)
    {
        if (whichUnit is null || whichField is null) return false;
        var code = WeaponCode(whichField.Id, WeaponIndex(index));
        if (whichUnit.Fields.TryGetValue(code, out var own) && own is bool set) return set;
        return Name(whichField.Id) == "uaen" ? (whichUnit.Type.Integer(WeaponsOn) & (1 << WeaponIndex(index))) != 0 : whichUnit.Type.Integer(code) != 0;
    }

    /// <summary>Keeps the value for the weapon's field on the unit, true; false for a nil unit or field.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Units: ua1t and ua1g read through the weapon fields (10-20); BlzGetUnitBaseDamage (11-08)); a value is kept as given, not measured in the game.</remarks>
    [Native(Level.Close, Fact = WeaponFacts)]
    public bool BlzSetUnitWeaponBooleanField(unit? whichUnit, unitweaponbooleanfield? whichField, int index, bool value)
    {
        if (whichUnit is null || whichField is null) return false;
        whichUnit.Fields[WeaponCode(whichField.Id, WeaponIndex(index))] = value;
        return true;
    }

    /// <summary>The weapon's text field: the value set on the unit, else the data's, "" for none; null for a nil unit or field.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Units: ua1t and ua1g read through the weapon fields (10-20); BlzGetUnitBaseDamage (11-08)); the other fields read as the data gives them, and an index other than 1 is the first weapon, not measured.</remarks>
    [Native(Level.Close, Fact = WeaponFacts)]
    public string? BlzGetUnitWeaponStringField(unit? whichUnit, unitweaponstringfield? whichField, int index)
    {
        if (whichUnit is null || whichField is null) return null;
        var code = WeaponCode(whichField.Id, WeaponIndex(index));
        return whichUnit.Fields.TryGetValue(code, out var own) && own is string set ? set : whichUnit.Type.Text(code) ?? "";
    }

    /// <summary>Keeps the text for the weapon's field on the unit, true; false for a nil unit, field or text.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Units: ua1t and ua1g read through the weapon fields (10-20); BlzGetUnitBaseDamage (11-08)); a text is kept as given, not measured in the game.</remarks>
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
    /// <remarks>Close: measured in part (game-facts.md, Units: ua1t and ua1g read through the weapon fields (10-20); BlzGetUnitBaseDamage (11-08)); only strength as the primary attribute was measured.</remarks>
    [Native(Level.Close, Fact = WeaponFacts)]
    public int BlzGetUnitBaseDamage(unit? whichUnit, int weaponIndex) =>
        whichUnit is null ? 0 : WeaponInt(whichUnit, "b", weaponIndex) + (IsHero(whichUnit.Type.Id) ? Whole(Attribute(whichUnit)) : 0);

    // The hero's primary attribute: strength, intelligence or agility, as the data names it (strength where it names none)
    double Attribute(unit hero) => Attributes.GetValueOrDefault(hero.Type.Text(Primary) ?? "", 1) switch { 2 => hero.Int, 3 => hero.Agi, _ => hero.Str };

    /// <summary>Sets the base damage as the data's base would need to give it: a hero's primary attribute is taken off first.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Units: ua1t and ua1g read through the weapon fields (10-20); BlzGetUnitBaseDamage (11-08)); as <c>BlzGetUnitBaseDamage</c> reads it, so only strength as the primary attribute was measured.</remarks>
    [Native(Level.Close, Fact = WeaponFacts)]
    public void BlzSetUnitBaseDamage(unit? whichUnit, int baseDamage, int weaponIndex)
    {
        if (whichUnit is not null) BlzSetUnitWeaponIntegerField(whichUnit, Convert<unitweaponintegerfield>(Code("ua1b")), weaponIndex, baseDamage - (IsHero(whichUnit.Type.Id) ? Whole(Attribute(whichUnit)) : 0));
    }

    /// <summary>The number of damage dice of the weapon; 0 for a nil unit.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Units: ua1t and ua1g read through the weapon fields (10-20); BlzGetUnitBaseDamage (11-08)); the dice are read as the data gives them, not measured in the game.</remarks>
    [Native(Level.Close, Fact = WeaponFacts)]
    public int BlzGetUnitDiceNumber(unit? whichUnit, int weaponIndex) => whichUnit is null ? 0 : WeaponInt(whichUnit, "d", weaponIndex);

    /// <summary>Sets the number of damage dice of the weapon.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Units: ua1t and ua1g read through the weapon fields (10-20); BlzGetUnitBaseDamage (11-08)); the dice are kept as given, not measured in the game.</remarks>
    [Native(Level.Close, Fact = WeaponFacts)]
    public void BlzSetUnitDiceNumber(unit? whichUnit, int diceNumber, int weaponIndex) =>
        BlzSetUnitWeaponIntegerField(whichUnit, Convert<unitweaponintegerfield>(Code("ua1d")), weaponIndex, diceNumber);

    /// <summary>The number of sides of each damage die of the weapon; 0 for a nil unit.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Units: ua1t and ua1g read through the weapon fields (10-20); BlzGetUnitBaseDamage (11-08)); the sides are read as the data gives them, not measured in the game.</remarks>
    [Native(Level.Close, Fact = WeaponFacts)]
    public int BlzGetUnitDiceSides(unit? whichUnit, int weaponIndex) => whichUnit is null ? 0 : WeaponInt(whichUnit, "s", weaponIndex);

    /// <summary>Sets the number of sides of each damage die of the weapon.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Units: ua1t and ua1g read through the weapon fields (10-20); BlzGetUnitBaseDamage (11-08)); the sides are kept as given, not measured in the game.</remarks>
    [Native(Level.Close, Fact = WeaponFacts)]
    public void BlzSetUnitDiceSides(unit? whichUnit, int diceSides, int weaponIndex) =>
        BlzSetUnitWeaponIntegerField(whichUnit, Convert<unitweaponintegerfield>(Code("ua1s")), weaponIndex, diceSides);

    /// <summary>The base cooldown, not the bonus attack speed gives (facts: 1.35 with and without Gloves of Haste, 10-19).</summary>
    /// <remarks>Close: measured in part (game-facts.md, Units: ua1t and ua1g read through the weapon fields (10-20); BlzGetUnitBaseDamage (11-08)); measured only against the base cooldown, with and without a Gloves of Haste bonus (10-19); other cases are read from the data.</remarks>
    [Native(Level.Close, Fact = WeaponFacts)]
    public float BlzGetUnitAttackCooldown(unit? whichUnit, int weaponIndex) =>
        BlzGetUnitWeaponRealField(whichUnit, Convert<unitweaponrealfield>(Code("ua1c")), weaponIndex);

    /// <summary>Sets the weapon's base cooldown, in seconds.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Units: ua1t and ua1g read through the weapon fields (10-20); BlzGetUnitBaseDamage (11-08)); a set cooldown is kept as given, not measured against the game's attack speed.</remarks>
    [Native(Level.Close, Fact = WeaponFacts)]
    public void BlzSetUnitAttackCooldown(unit? whichUnit, float cooldown, int weaponIndex) =>
        BlzSetUnitWeaponRealField(whichUnit, Convert<unitweaponrealfield>(Code("ua1c")), weaponIndex, cooldown);

    // A swing under way is dropped (its hit never lands) and its cooldown with it; one after its hit keeps its cooldown (probe units-resets, D796)
    void DropSwing(unit u, bool clearCooldown)
    {
        var swinging = Seconds < u.SwingEnds - HalfStep;
        if (swinging) u.SwingNumber++;
        if (swinging || clearCooldown) (u.NextAttack, u.SwingEnds) = (Seconds, Seconds);
    }

    // Close: the second weapon isn't told apart, and the next swing starts a step after, not in the same instant
    /// <summary>Drops a swing under way and clears the cooldown, so the unit can swing again a step later.</summary>
    /// <remarks>Close: measured in part (probes/expected/units-resets.txt: attack reset); the second weapon isn't told apart, and the next swing starts a step later, not in the same instant.</remarks>
    [Native(Level.Close, Fact = "probes/expected/units-resets.txt: attack reset")]
    public void BlzResetUnitAttack(unit? whichUnit, int weaponIndex)
    {
        if (whichUnit is null) return;
        DropSwing(whichUnit, true);
        Wake();
    }

    // The order ends, and an idle unit goes on to acquire the foe again
    /// <summary>Drops a swing under way, keeping its cooldown, and ends the unit's order and target; an idle unit goes on to find a foe again.</summary>
    /// <remarks>Close: measured in part (probes/expected/units-resets.txt: attack interrupt); how soon the unit finds a foe again is modelled, not measured.</remarks>
    [Native(Level.Close, Fact = "probes/expected/units-resets.txt: attack interrupt")]
    public void BlzUnitInterruptAttack(unit? whichUnit)
    {
        if (whichUnit is null) return;
        DropSwing(whichUnit, false);
        (whichUnit.Order, whichUnit.AttackTarget) = (0, null);
        Wake();
    }

    /// <summary>
    /// Heals the amount (a negative one takes life: -30 takes 30) and answers it; a nil source heals nothing and answers 0
    /// (facts, Damage). The flags change nothing seen.
    /// </summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Units: BlzUnitHeal (10-19)).</remarks>
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

    /// <summary>The number of placed doodads in the map that aren't destructables.</summary>
    /// <remarks>Close: measured in part (game-facts.md, World Editor and map files: doodads (11-09)); the count is read from the map's file, and a doodad of a type the data lacks is not counted, not measured in the game.</remarks>
    [Native(Level.Close, Fact = "game-facts.md, World Editor and map files: doodads (11-09)")]
    public int BlzGetNumDoodads() => PlacedDoodads().Count;

    /// <summary>The type id of the doodad at the index, in the file's order; 0 for an index with none.</summary>
    /// <remarks>Close: Read from the map's file, not measured in the game.</remarks>
    [Native(Level.Close)] public int BlzGetDoodadId(int index) => Doodad(index) is { } d ? BinaryPrimitives.ReverseEndianness(d.TypeId) : 0;
    /// <summary>The variation of the doodad at the index; 0 for an index with none.</summary>
    /// <remarks>Close: Read from the map's file, not measured in the game.</remarks>
    [Native(Level.Close)] public int BlzGetDoodadVariation(int index) => Doodad(index)?.Variation ?? 0;
    /// <summary>The x of the doodad at the index; 0 for an index with none.</summary>
    /// <remarks>Close: Read from the map's file, not measured in the game.</remarks>
    [Native(Level.Close)] public float BlzGetDoodadX(int index) => Doodad(index)?.Position.X ?? 0;
    /// <summary>The y of the doodad at the index; 0 for an index with none.</summary>
    /// <remarks>Close: Read from the map's file, not measured in the game.</remarks>
    [Native(Level.Close)] public float BlzGetDoodadY(int index) => Doodad(index)?.Position.Y ?? 0;
    /// <summary>The z of the doodad at the index; 0 for an index with none.</summary>
    /// <remarks>Close: Read from the map's file, not measured in the game.</remarks>
    [Native(Level.Close)] public float BlzGetDoodadZ(int index) => Doodad(index)?.Position.Z ?? 0;
    /// <summary>The yaw of the doodad at the index, as the file has it; 0 for an index with none.</summary>
    /// <remarks>Close: Returned as the file stores the angle; the game's unit for it is not measured.</remarks>
    [Native(Level.Close)] public float BlzGetDoodadYaw(int index) => Doodad(index)?.Rotation ?? 0;
    /// <summary>The pitch of the doodad at the index, as the file has it; 0 for an index with none.</summary>
    /// <remarks>Close: Returned as the file stores the angle; the game's unit for it is not measured.</remarks>
    [Native(Level.Close)] public float BlzGetDoodadPitch(int index) => Doodad(index)?.Pitch ?? 0;
    /// <summary>The roll of the doodad at the index, as the file has it; 0 for an index with none.</summary>
    /// <remarks>Close: Returned as the file stores the angle; the game's unit for it is not measured.</remarks>
    [Native(Level.Close)] public float BlzGetDoodadRoll(int index) => Doodad(index)?.Roll ?? 0;
    /// <summary>The x scale of the doodad at the index; 0 for an index with none.</summary>
    /// <remarks>Close: Read from the map's file, not measured in the game.</remarks>
    [Native(Level.Close)] public float BlzGetDoodadScaleX(int index) => Doodad(index)?.Scale.X ?? 0;
    /// <summary>The y scale of the doodad at the index; 0 for an index with none.</summary>
    /// <remarks>Close: Read from the map's file, not measured in the game.</remarks>
    [Native(Level.Close)] public float BlzGetDoodadScaleY(int index) => Doodad(index)?.Scale.Y ?? 0;
    /// <summary>The z scale of the doodad at the index; 0 for an index with none.</summary>
    /// <remarks>Close: Read from the map's file, not measured in the game.</remarks>
    [Native(Level.Close)] public float BlzGetDoodadScaleZ(int index) => Doodad(index)?.Scale.Z ?? 0;

    /// <summary>False for every index.</summary>
    /// <remarks>Close: False until measured: the game answered false for a doodad that isn't there (probe natives-test-run, none on its map); a map placing doodads, written by War3Net with each state byte, closed the game, so a doodad's own answer, which the file's state bits would give, wasn't read.</remarks>
    [Native(Level.Close, Fact = TestRunProbe, Reason = "False until measured: the game answered false for a doodad that isn't there (probe natives-test-run, none on its map); a map placing doodads, written by War3Net with each state byte, closed the game, so a doodad's own answer, which the file's state bits would give, wasn't read")]
    public bool BlzGetDoodadIsUsingModelAxes(int index) => false;

    /// <summary>Changes nothing; no animation is drawn.</summary>
    /// <remarks>Recorded: A doodad's animation is drawn; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "A doodad's animation is drawn; no native reads it back")]
    public void BlzSetSingleDoodadAnimation(int index, string? animName, bool animRandom) { }

    /// <summary>Changes nothing; no colour is drawn.</summary>
    /// <remarks>Recorded: A doodad's colour is drawn; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "A doodad's colour is drawn; no native reads it back")]
    public void BlzSetSingleDoodadColor(int index, playercolor? whichColor) { }

    // Close: the game leaves 0.01 s on a cooldown that was running, and the order is taken meanwhile; here none is left
    /// <summary>Ends the cooldown of each of the unit's abilities now.</summary>
    /// <remarks>Close: measured in part (probes/expected/units-resets.txt: cooldown reset); the game leaves 0.01 s on a cooldown that was running, and here none is left.</remarks>
    [Native(Level.Close, Fact = "probes/expected/units-resets.txt: cooldown reset")]
    public void UnitResetCooldown(unit? whichUnit)
    {
        if (whichUnit is not null) foreach (var a in whichUnit.Abilities) a.CooldownEnd = 0;
    }
}
