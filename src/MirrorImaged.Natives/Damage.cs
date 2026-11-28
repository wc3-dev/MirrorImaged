using MirrorImaged.Data;
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
    public bool Real; // an attack of a unit fighting, not a hit from code (the game reads isAttack false for code even with the flag)
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
    static readonly int MagicImmunity = Code("ACmi"), SpellImmunity = Code("Amim"), ResistantSkin = Code("Arsk");

    // Spell immunity: an ability made from Amim (the creeps' ACmi, a Spellbreaker's or Dryad's Amim, a map's copy), or Avatar's buff
    bool IsMagicImmune(unit u) => Find(u, MagicImmunity) is not null || HasCode(u, SpellImmunity) || Has(u, b => b.MagicImmune);

    // Resistant Skin, once its owner has its research (probe units-kinds: a Misha's ACrk read resistant, a Mountain Giant's Arsk without Resistant Skin
    // researched didn't, and Slow lasted its unit's time on it)
    bool Resistant(unit u) => u.Abilities.Any(a => a.Level > 0 && Coded(a.Type) == ResistantSkin && Unlocked(u, a.Type));

    // Whether the unit's owner has what the ability requires (its Requires and Requiresamount fields)
    bool Unlocked(unit u, GameObject type)
    {
        if (Data.FieldAt(ObjectKind.Ability, "Requires") is not { } field || type.Text(field) is not { Length: > 0 } listed) return true;
        var amounts = (Data.FieldAt(ObjectKind.Ability, "Requiresamount") is { } count ? type.Text(count) : null)?.Split(',') ?? [];
        var techs = listed.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        for (var n = 0; n < techs.Length; n++)
            if (techs[n].Length == 4 && GetPlayerTechCount(u.Owner, Code(techs[n]), true) < (n < amounts.Length && int.TryParse(amounts[n], out var need) ? need : 1)) return false;
        return true;
    }

    Hit? Striking => Response(UnitDamaged, UnitDamaging, PlayerUnitDamaged, PlayerUnitDamaging)?.Hit;

    /// <summary>
    /// Deals the damage as its events and the armour give it: DAMAGING reads the amount as given, then the armour (damage
    /// type normal only) and the target's defence type cut it, then DAMAGED reads what the life loses and the life drops
    /// after its actions, a killing blow's death inside the call. A dead target answers true with no events, an
    /// invulnerable one reads 0 in DAMAGED, and a spell-immune one gets no DAMAGED and loses nothing for the damage and
    /// attack types facts name, and an ethereal one for all but magic (<see cref="EtherealBonus"/>). A destructable loses
    /// the amount from its life. Close: a nil source here deals nothing and answers false (the game's answer is not
    /// measured). Of the abilities that change a hit, Magic Immunity, invulnerability, armour buffs, an illusion's share
    /// and Life Steal (for a melee attack) apply; Hardened Skin and Mana Shield are not modelled, and Evasion, Critical
    /// Strike, Bash and Thorns Aura act on a unit's own attacks, not on this call.
    /// </summary>
    /// <remarks>Close: measured in part (game-facts.md, Damage: probes of 10-16 to 10-31 and 11-06); a null source or target gives false, not measured in the game, and of the abilities that change a hit, Hardened Skin and Mana Shield are not modelled.</remarks>
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
        if (IsMagicImmune(target) && (StoppedBySpellImmunity.Contains(hit.DamageType?.Id ?? 0) || hit.AttackType?.Id == MagicAttack))
            return true;
        if (IsEthereal(target) && EtherealBonus(hit) == 0) return true;
        // An illusion's hit takes what its spell lets it, and a hit takes more from one (probe abilities-spells: Mirror Image, 0 and twice)
        hit.Amount = Reduced(hit, target) * hit.Source.Deals * target.Takes;
        FireUnit(new(target, Hit: hit), UnitDamaged, PlayerUnitDamaged);
        if (!target.Dead) SetLife(target, target.Life - hit.Amount, hit.Source);
        // An attack in melee gives its unit back a share of the damage
        if (hit.IsAttack && !hit.Ranged && hit.Source is { Dead: false } thief && LifeStealOf(thief) is > 0 and var share) SetLife(thief, Math.Min(thief.Life + hit.Amount * share, thief.MaxLife));
        return true;
    }

    // What the target's armour and defence type leave of a hit before DAMAGED: every damage type is cut by the attack
    // type's row of the constants for the defence type (probe abilities-banish: 100 of fire, magic and universal take 200
    // from a footman by attack type magic and 75 from a hero by spells), and an ethereal target's bonus is added; then
    // damage type normal alone is cut by the armour; a negative armour adds 2 - 0.94^n (n its whole part, to -20)
    float Reduced(Hit hit, unit target)
    {
        if (Find(target, Invulnerable) is not null || IsInvulnerableByBuff(target)) return 0;
        var row = Data.Constant("DamageBonus" + AttackRows[Math.Clamp(hit.AttackType?.Id ?? 0, 0, AttackRows.Length - 1)])?.Split(',');
        var defence = BlzGetUnitIntegerField(target, Convert<unitintegerfield>(DefenseType));
        var bonus = row is not null && defence < row.Length && float.TryParse(row[defence], System.Globalization.CultureInfo.InvariantCulture, out var factor) ? factor : 1;
        if (IsEthereal(target)) bonus *= EtherealBonus(hit);
        if (hit.DamageType?.Id != NormalDamage) return hit.Amount * bonus;
        var armor = BlzGetUnitArmor(target) + ArmorBonus(target);
        var step = Data.Constant("DefenseArmor", 0.06f);
        var cut = armor >= 0 ? 1 / (1 + step * armor) : 2 - MathF.Pow(1 - step, Math.Min(MathF.Truncate(-armor), 20));
        return hit.Amount * bonus * cut;
    }

    // What reaches an ethereal unit (probe abilities-banish): attack type magic with any damage type, and attack type
    // spells with any but normal, each times the constants' EtherealDamageBonus (1.66 for both, the fourth and sixth of
    // its seven entries); every other hit takes nothing and has no DAMAGED
    float EtherealBonus(Hit hit)
    {
        var attack = hit.AttackType?.Id ?? 0;
        if (attack != MagicAttack && (attack != 0 || hit.DamageType?.Id == NormalDamage)) return 0;
        return Data.Constant("EtherealDamageBonus")?.Split(',') is { Length: > 5 } row
            && float.TryParse(row[attack == 0 ? 3 : 5], System.Globalization.CultureInfo.InvariantCulture, out var bonus) ? bonus : 1.66f;
    }

    /// <summary>
    /// After the delay, deals the amount to each living unit whose origin is within the radius of the place, by
    /// UnitDamageTarget (the source's own is included, the order the units' grid gives). Answers true. Close: the delay
    /// is only tested for its end, not for the source dying before it, and the splash's order isn't measured.
    /// </summary>
    /// <remarks>Close: measured in part (game-facts.md, Damage: probes of 10-16 to 10-31 and 11-06); the delay is only tested for its end, not for the source dying before it, and the order of the splash is not measured.</remarks>
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
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Damage: probes of 10-16 to 10-31 and 11-06).</remarks>
    [Native(Level.Exact, Fact = DamageFacts)]
    public float GetEventDamage() => Striking?.Amount ?? 0;

    /// <summary>The unit that deals the damage of the damage event; nil outside one.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Damage: probes of 10-16 to 10-31 and 11-06).</remarks>
    [Native(Level.Exact, Fact = DamageFacts)]
    public unit? GetEventDamageSource() => Striking?.Source;

    /// <summary>The unit that takes the damage of the damage event; nil outside one, and for a destructable.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Damage: probes of 10-16 to 10-31 and 11-06).</remarks>
    [Native(Level.Exact, Fact = DamageFacts)]
    public unit? BlzGetEventDamageTarget() => Striking?.Target as unit;

    /// <summary>The attack type of the damage event, as the actions have changed it; nil outside one.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Damage: probes of 10-16 to 10-31 and 11-06).</remarks>
    [Native(Level.Exact, Fact = DamageFacts)]
    public attacktype? BlzGetEventAttackType() => Striking?.AttackType;

    /// <summary>The damage type of the damage event, as the actions have changed it; nil outside one.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Damage: probes of 10-16 to 10-31 and 11-06).</remarks>
    [Native(Level.Exact, Fact = DamageFacts)]
    public damagetype? BlzGetEventDamageType() => Striking?.DamageType;

    /// <summary>The weapon type of the damage event, as the actions have changed it; nil outside one.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Damage: probes of 10-16 to 10-31 and 11-06).</remarks>
    [Native(Level.Exact, Fact = DamageFacts)]
    public weapontype? BlzGetEventWeaponType() => Striking?.WeaponType;

    /// <summary>False for a hit from code even with the attack flag, true for a real attack (facts: Damage, 09-30 to 10-31).</summary>
    /// <remarks>Close: measured in part (game-facts.md, Damage: probes of 10-16 to 10-31 and 11-06); outside a damage event it gives false, not measured in the game.</remarks>
    [Native(Level.Close, Fact = DamageFacts)]
    public bool BlzGetEventIsAttack() => Striking?.Real ?? false;

    /// <summary>Changes the amount the armour, or the life, works from; false outside a damage event.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Damage: probes of 10-16 to 10-31 and 11-06).</remarks>
    [Native(Level.Exact, Fact = DamageFacts)]
    public void BlzSetEventDamage(float damage)
    {
        if (Striking is { } hit) hit.Amount = damage;
    }

    /// <summary>Changes the attack type of the damage event; false outside one.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Damage: probes of 10-16 to 10-31 and 11-06).</remarks>
    [Native(Level.Exact, Fact = DamageFacts)]
    public bool BlzSetEventAttackType(attacktype? attackType)
    {
        if (Striking is not { } hit) return false;
        hit.AttackType = attackType;
        return true;
    }

    /// <summary>Before the armour it decides it; after it, nothing changes (facts: Damage, 09-30 and 10-31).</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Damage: probes of 10-16 to 10-31 and 11-06).</remarks>
    [Native(Level.Exact, Fact = DamageFacts)]
    public bool BlzSetEventDamageType(damagetype? damageType)
    {
        if (Striking is not { } hit) return false;
        hit.DamageType = damageType;
        return true;
    }

    /// <summary>Changes the weapon type of the damage event; false outside one.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Damage: probes of 10-16 to 10-31 and 11-06).</remarks>
    [Native(Level.Exact, Fact = DamageFacts)]
    public bool BlzSetEventWeaponType(weapontype? weaponType)
    {
        if (Striking is not { } hit) return false;
        hit.WeaponType = weaponType;
        return true;
    }

    /// <summary>The unit's armour: its type's, a hero's agility's, or the value BlzSetUnitArmor gave (facts: Damage, 11-06).</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Damage: probes of 10-16 to 10-31 and 11-06).</remarks>
    [Native(Level.Exact, Fact = DamageFacts)]
    public float BlzGetUnitArmor(unit? whichUnit) =>
        whichUnit is null ? 0 : BlzGetUnitRealField(whichUnit, Convert<unitrealfield>(Defense));

    /// <summary>Sets the unit's armour; nothing for a null unit.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Damage: probes of 10-16 to 10-31 and 11-06).</remarks>
    [Native(Level.Exact, Fact = DamageFacts)]
    public void BlzSetUnitArmor(unit? whichUnit, float armorAmount)
    {
        if (whichUnit is not null) BlzSetUnitRealField(whichUnit, Convert<unitrealfield>(Defense), armorAmount);
    }
}
