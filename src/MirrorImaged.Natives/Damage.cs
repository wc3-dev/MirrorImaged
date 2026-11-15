using MirrorImaged.World;

namespace MirrorImaged.Natives;

// One hit of damage as its events read and change it: its amount, types and whether it is an attack. The same object
// goes through both events, so a change in DAMAGING reaches the armour and one in DAMAGED the life taken.
sealed class Hit(unit source, widget target, float amount, bool isAttack, bool ranged, attacktype? attackType, damagetype? damageType, weapontype? weaponType)
{
    public readonly unit Source = source;
    public readonly widget Target = target;
    public float Amount = amount;
    public bool IsAttack = isAttack, Ranged = ranged;
    public attacktype? AttackType = attackType;
    public damagetype? DamageType = damageType;
    public weapontype? WeaponType = weaponType;
}

// Damage from code: UnitDamageTarget and UnitDamagePoint, the damaging and damaged events, the armour formula, and the
// killer of a death (game-facts, "Damage"). What abilities change in a hit (Hardened Skin, Mana Shield, evasion and the
// rest) comes with wave 4, and real attacks with wave 3.
public sealed partial class GameNatives
{
    const string DamageFacts = "game-facts.md, Damage: probes of 10-16 to 10-31 and 11-06";
    const int UnitDamaged = 52, UnitDamaging = 314, PlayerUnitDamaged = 308, PlayerUnitDamaging = 315; // EVENT_UNIT_*, EVENT_PLAYER_UNIT_*
    const int NormalDamage = 4, MagicAttack = 4;

    // The damage types spell immunity stops (fire, cold, lightning, divine, magic, sonic, force, death, mind, plant,
    // defensive, spirit link and shadow strike), and the names of the bonus rows by attack type (facts: Damage)
    static readonly int[] StoppedBySpellImmunity = [8, 9, 10, 13, 14, 15, 17, 18, 19, 20, 21, 24, 25];
    static readonly string[] AttackRows = ["Spells", "Normal", "Pierce", "Siege", "Magic", "Chaos", "Hero"];
    static readonly int MagicImmunity = Code("ACmi");

    Hit? Striking => Response(UnitDamaged, UnitDamaging, PlayerUnitDamaged, PlayerUnitDamaging)?.Hit;

    /// <summary>
    /// Deals the damage as its events and the armour give it: DAMAGING reads the amount as given, then the armour (damage
    /// type normal only) and the target's defence type cut it, then DAMAGED reads what the life loses and the life drops
    /// after its actions, a killing blow's death inside the call. A dead target answers true with no events, an
    /// invulnerable one reads 0 in DAMAGED, and a spell-immune one gets no DAMAGED and loses nothing for the damage and
    /// attack types facts name. A destructable loses the amount from its life. Close: a nil source here deals nothing
    /// and answers false (the game's answer is not measured), and the abilities that change a hit are wave 4's.
    /// </summary>
    [Native(Level.Close, Fact = DamageFacts)]
    public bool UnitDamageTarget(unit? whichUnit, widget? target, float amount, bool attack, bool ranged, attacktype? attackType, damagetype? damageType, weapontype? weaponType)
    {
        if (whichUnit is null || target is null) return false;
        var hit = new Hit(whichUnit, target, amount, attack, ranged, attackType, damageType, weaponType);
        if (target is unit u) return Strike(hit, u);
        if (target is not destructable d) return false;
        SetDestructableLife(d, d.Life - amount);
        return true;
    }

    bool Strike(Hit hit, unit target)
    {
        if (target.Dead || target.Removed) return true;
        FireUnit(new(target, Hit: hit), UnitDamaging, PlayerUnitDamaging);
        if (target.Dead) return true;
        if (Find(target, MagicImmunity) is not null && (StoppedBySpellImmunity.Contains(hit.DamageType?.Id ?? 0) || hit.AttackType?.Id == MagicAttack))
            return true;
        hit.Amount = Reduced(hit, target);
        FireUnit(new(target, Hit: hit), UnitDamaged, PlayerUnitDamaged);
        if (!target.Dead) SetLife(target, target.Life - hit.Amount, hit.Source);
        return true;
    }

    // What the target's armour and defence type leave of a hit before DAMAGED: only damage type normal is cut, by the
    // attack type's row of the constants for the defence type, then by the armour; a negative armour adds
    // 2 - 0.94^n (n its whole part, to -20)
    float Reduced(Hit hit, unit target)
    {
        if (Find(target, Invulnerable) is not null) return 0;
        if (hit.DamageType?.Id != NormalDamage) return hit.Amount;
        var row = Data.Constant("DamageBonus" + AttackRows[Math.Clamp(hit.AttackType?.Id ?? 0, 0, AttackRows.Length - 1)])?.Split(',');
        var defence = BlzGetUnitIntegerField(target, Convert<unitintegerfield>(DefenseType));
        var bonus = row is not null && defence < row.Length && float.TryParse(row[defence], System.Globalization.CultureInfo.InvariantCulture, out var factor) ? factor : 1;
        var armor = BlzGetUnitArmor(target);
        var step = Data.Constant("DefenseArmor", 0.06f);
        var cut = armor >= 0 ? 1 / (1 + step * armor) : 2 - MathF.Pow(1 - step, Math.Min(MathF.Truncate(-armor), 20));
        return hit.Amount * bonus * cut;
    }

    /// <summary>
    /// After the delay, deals the amount to each living unit whose origin is within the radius of the place, by
    /// UnitDamageTarget (the source's own is included, the order the units' grid gives). Answers true. Close: the delay
    /// is only tested for its end, not for the source dying before it, and the splash's order isn't measured.
    /// </summary>
    [Native(Level.Close, Fact = DamageFacts)]
    public bool UnitDamagePoint(unit? whichUnit, float delay, float radius, float x, float y, float amount, bool attack, bool ranged, attacktype? attackType, damagetype? damageType, weapontype? weaponType)
    {
        if (whichUnit is null) return false;
        void Burst()
        {
            foreach (var u in ByPlace(t => !t.Dead && Distance2(t.X - x, t.Y - y) <= radius * radius).ToArray())
                UnitDamageTarget(whichUnit, u, amount, attack, ranged, attackType, damageType, weaponType);
        }
        if (delay <= 0) Burst();
        else _game.At(_game.Step + Math.Max(1, Game.Steps(delay)), Burst);
        return true;
    }

    /// <summary>The amount of the damage event it is read in, as the actions have changed it; 0 outside one.</summary>
    [Native(Level.Exact, Fact = DamageFacts)]
    public float GetEventDamage() => Striking?.Amount ?? 0;

    [Native(Level.Exact, Fact = DamageFacts)]
    public unit? GetEventDamageSource() => Striking?.Source;

    [Native(Level.Exact, Fact = DamageFacts)]
    public unit? BlzGetEventDamageTarget() => Striking?.Target as unit;

    [Native(Level.Exact, Fact = DamageFacts)]
    public attacktype? BlzGetEventAttackType() => Striking?.AttackType;

    [Native(Level.Exact, Fact = DamageFacts)]
    public damagetype? BlzGetEventDamageType() => Striking?.DamageType;

    [Native(Level.Exact, Fact = DamageFacts)]
    public weapontype? BlzGetEventWeaponType() => Striking?.WeaponType;

    /// <summary>False for a hit from code even with the attack flag, true for a real attack (facts: Damage, 09-30 to 10-31).</summary>
    [Native(Level.Close, Fact = DamageFacts)]
    public bool BlzGetEventIsAttack() => false;

    /// <summary>Changes the amount the armour, or the life, works from; false outside a damage event.</summary>
    [Native(Level.Exact, Fact = DamageFacts)]
    public void BlzSetEventDamage(float damage)
    {
        if (Striking is { } hit) hit.Amount = damage;
    }

    [Native(Level.Exact, Fact = DamageFacts)]
    public bool BlzSetEventAttackType(attacktype? attackType)
    {
        if (Striking is not { } hit) return false;
        hit.AttackType = attackType;
        return true;
    }

    /// <summary>Before the armour it decides it; after it, nothing changes (facts: Damage, 09-30 and 10-31).</summary>
    [Native(Level.Exact, Fact = DamageFacts)]
    public bool BlzSetEventDamageType(damagetype? damageType)
    {
        if (Striking is not { } hit) return false;
        hit.DamageType = damageType;
        return true;
    }

    [Native(Level.Exact, Fact = DamageFacts)]
    public bool BlzSetEventWeaponType(weapontype? weaponType)
    {
        if (Striking is not { } hit) return false;
        hit.WeaponType = weaponType;
        return true;
    }

    /// <summary>The unit's armour: its type's, a hero's agility's, or the value BlzSetUnitArmor gave (facts: Damage, 11-06).</summary>
    [Native(Level.Exact, Fact = DamageFacts)]
    public float BlzGetUnitArmor(unit? whichUnit) =>
        whichUnit is null ? 0 : BlzGetUnitRealField(whichUnit, Convert<unitrealfield>(Defense));

    [Native(Level.Exact, Fact = DamageFacts)]
    public void BlzSetUnitArmor(unit? whichUnit, float armorAmount)
    {
        if (whichUnit is not null) BlzSetUnitRealField(whichUnit, Convert<unitrealfield>(Defense), armorAmount);
    }
}
