using MirrorImaged.Data;
using MirrorImaged.World;

namespace MirrorImaged.Natives;

public partial class unit
{
    internal ability? Casting;      // the ability its current order casts
    internal double CastEffectAt;   // when the cast point is over, 0 before the unit is in range
}

// The cast flow (game-facts, "Spells and abilities"): an order whose name is the Order of an ability the unit has is a spell order, taken
// when the unit has the mana and the ability is off cooldown; the unit walks into the ability's range, then the channel and cast events
// fire, the cast point passes, mana and cooldown are spent and the effect event fires with the effect, then finish and end cast, and the
// unit is idle. Effects are data-driven for the abilities named in decisions D355 (Holy Light, Storm Bolt, Thunder Clap) and the others
// only fire their events (Close).
public sealed partial class GameNatives
{
    const int UnitSpellChannel = 289, UnitSpellCast = 290, UnitSpellEffect = 291, UnitSpellFinish = 292, UnitSpellEndCast = 293;
    const int PlayerSpellChannel = 272, PlayerSpellCast = 273, PlayerSpellEffect = 274, PlayerSpellFinish = 275, PlayerSpellEndCast = 276;
    static readonly int CastTime = Code("acas"), CastRange = Code("aran"), CastArea = Code("aare");

    // The commands of the command card that are abilities with an order but no cast: they are the unit's own orders, not spells
    static readonly HashSet<string> Commands = ["smart", "stop", "move", "attack", "patrol", "holdposition", "setrally", "getitem", "harvest", "returnresources", "repair", "load", "unload",
        "defend", "undefend", "cancel", "build", "humanbuild", "orcbuild", "nightelfbuild", "undeadbuild", "neutralbuild", "autoharvestgold", "autoharvestlumber", "resumeharvest",
        "attackground", "attackonce", "revive", "resumebuild", "stunned"];

    // The ability of the unit whose order has this id, if it has one
    ability? SpellOf(unit u, int order)
    {
        if (OrderNames.TryGetValue(order, out var named) && Commands.Contains(named)) return null;
        if (Data.FieldAt(ObjectKind.Ability, "Order") is not { } field) return null;
        return u.Abilities.Find(a => !a.Disabled && !a.Hidden && a.Type.Text(field) is { Length: > 0 } name && OrderIds.GetValueOrDefault(name) == order);
    }

    float Field(ability a, int field) => BlzGetAbilityRealLevelField(a, Convert<abilityreallevelfield>(field), a.Level - 1);

    // Whether the unit can start the spell: alive, the mana, no cooldown running
    bool Ready(unit u, ability a) => !u.Dead && !u.Buffs.Any(b => b.Silence) && CooldownLeft(a) <= 0 && u.Mana >= BlzGetAbilityIntegerLevelField(a, Convert<abilityintegerlevelfield>(ManaCost), a.Level - 1);

    // A spell order taken before the order itself is given; false from here when the unit can't cast it
    bool Spell(unit u, int order, out bool taken)
    {
        taken = false;
        if (SpellOf(u, order) is not { } ability) return false;
        taken = Ready(u, ability);
        if (taken) (u.Casting, u.CastEffectAt) = (ability, 0);
        return true;
    }

    // One step of a casting unit: into range, then through the cast point
    bool Cast(unit u)
    {
        if (u.Casting is not { } ability) return Finish(u);
        var (x, y, target) = (u.OrderX, u.OrderY, u.OrderTarget);
        // shortcut: an immediate order has no place and stores 0, 0, so it is told from a point order by that place; a point order at exactly 0, 0 then casts where the caster stands
        if (target is null && x == 0 && y == 0) (x, y) = (u.X, u.Y);
        var range = Field(ability, CastRange) + (target is unit t ? t.Type.Real(Collision) : 0);
        if (u.CastEffectAt == 0)
        {
            if (range > 0 && Distance2(x - u.X, y - u.Y) > range * range) return Walk(u, x, y, follow: true);
            var happening = new Happening(u, Skill: ability.Type.Id, PointX: x, PointY: y, Target: target);
            FireUnit(happening, UnitSpellChannel, PlayerSpellChannel);
            if (u.Casting != ability) return true;
            FireUnit(happening, UnitSpellCast, PlayerSpellCast);
            u.CastEffectAt = Seconds + Math.Max(Field(ability, CastTime), 0.001);
            return true;
        }
        if (Seconds < u.CastEffectAt) return true;
        var cost = BlzGetAbilityIntegerLevelField(ability, Convert<abilityintegerlevelfield>(ManaCost), ability.Level - 1);
        var cooldown = Field(ability, Cooldown);
        var effect = new Happening(u, Skill: ability.Type.Id, PointX: x, PointY: y, Target: target);
        if (u.Mana >= cost && !u.Dead)
        {
            SetMana(u, u.Mana - cost);
            (ability.Cooldown, ability.CooldownEnd) = (cooldown, Seconds + cooldown);
            FireUnit(effect, UnitSpellEffect, PlayerSpellEffect);
            Effect(u, ability, x, y, target);
            FireUnit(effect, UnitSpellFinish, PlayerSpellFinish);
        }
        FireUnit(effect, UnitSpellEndCast, PlayerSpellEndCast);
        u.Casting = null;
        return Finish(u);
    }

    // How long a buff of the ability lasts on the unit: its duration, a hero's own where it has one
    double Lasts(ability a, unit target) => IsHero(target.Type.Id) && Field(a, Code("ahdu")) is var hero and > 0 ? hero : Field(a, Code("adur"));

    // What the spell does: the data-driven effects of the abilities modelled here (waves 4's first set), nothing for the rest
    void Effect(unit caster, ability a, float x, float y, widget? target)
    {
        if (MoreEffect(caster, a, x, y, target)) return;
        var amount = TagValue(a.Type, "DataA" + a.Level);
        switch (a.Type.BaseId)
        {
            case var id when id == Code("AHhb") && target is unit ally && !ally.Dead:
                SetLife(ally, ally.Life + amount);
                break;
            case var id when id == Code("AHtb") && target is unit foe && !foe.Dead:
                // Storm Bolt: a hit of 0 of unknown type, then the damage of type sonic (facts, Damage)
                UnitDamageTarget(caster, foe, 0, false, false, Convert<attacktype>(0), Convert<damagetype>(0), Convert<weapontype>(0));
                UnitDamageTarget(caster, foe, amount, false, false, Convert<attacktype>(0), Convert<damagetype>(15), Convert<weapontype>(0));
                if (!foe.Dead) Apply(foe, caster, StunBuff, Lasts(a, foe), false, b => b.Stun = true);
                break;
            case var id when id == Code("Aslo") && target is unit slowed && !slowed.Dead:
                Apply(slowed, caster, SlowBuff, Lasts(a, slowed), false, b => (b.SpeedFactor, b.AttackSpeedBonus) = (1 - amount, -TagValue(a.Type, "DataB" + a.Level)));
                break;
            case var id when id == Code("Ablo") && target is unit hasted && !hasted.Dead:
                Apply(hasted, caster, BloodlustBuff, Lasts(a, hasted), true, b => (b.AttackSpeedBonus, b.SpeedFactor) = (amount, 1 + TagValue(a.Type, "DataB" + a.Level)));
                break;
            case var id when id == Code("AHtc"):
                var area = Field(a, CastArea);
                foreach (var near in _units.Values.ToArray())
                    if (Hostile(caster, near) && !Flies(near) && Distance2(near.X - caster.X, near.Y - caster.Y) <= area * area)
                        UnitDamageTarget(caster, near, amount, false, false, Convert<attacktype>(0), Convert<damagetype>(15), Convert<weapontype>(0));
                break;
        }
    }

    Happening? SpellResponse => Response(UnitSpellChannel, UnitSpellCast, UnitSpellEffect, UnitSpellFinish, UnitSpellEndCast,
        PlayerSpellChannel, PlayerSpellCast, PlayerSpellEffect, PlayerSpellFinish, PlayerSpellEndCast);

    [Native(Level.Close, Fact = "game-facts.md, Spells and abilities: the cast events (10-31, 11-05)")]
    public int GetSpellAbilityId() => SpellResponse?.Skill ?? 0;

    [Native(Level.Close, Fact = "game-facts.md, Spells and abilities: the cast events (10-31, 11-05)")]
    public ability? GetSpellAbility() => SpellResponse is { } e ? e.Unit.Abilities.Find(a => a.Type.Id == e.Skill) : null;

    [Native(Level.Close, Fact = "game-facts.md, Spells and abilities: the cast events (10-31, 11-05)")]
    public unit? GetSpellAbilityUnit() => SpellResponse?.Unit;

    [Native(Level.Close, Fact = "game-facts.md, Spells and abilities: the cast events (10-31, 11-05)")]
    public unit? GetSpellTargetUnit() => SpellResponse?.Target as unit;

    [Native(Level.Close, Fact = "game-facts.md, Spells and abilities: the cast events (10-31, 11-05)")]
    public item? GetSpellTargetItem() => SpellResponse?.Target as item;

    [Native(Level.Close, Fact = "game-facts.md, Spells and abilities: the cast events (10-31, 11-05)")]
    public destructable? GetSpellTargetDestructable() => SpellResponse?.Target as destructable;

    [Native(Level.Close, Fact = "game-facts.md, Spells and abilities: the cast events (10-31, 11-05)")]
    public float GetSpellTargetX() => SpellResponse?.PointX ?? 0;

    [Native(Level.Close, Fact = "game-facts.md, Spells and abilities: the cast events (10-31, 11-05)")]
    public float GetSpellTargetY() => SpellResponse?.PointY ?? 0;

    [Native(Level.Close, Fact = "game-facts.md, Spells and abilities: the cast events (10-31, 11-05)")]
    public location? GetSpellTargetLoc() => SpellResponse is { } e ? Location(e.PointX, e.PointY) : null;
}
