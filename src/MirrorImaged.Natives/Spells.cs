using MirrorImaged.Data;
using MirrorImaged.World;

namespace MirrorImaged.Natives;

public partial class unit
{
    internal ability? Casting;      // the ability its current order casts
    internal Happening? Cast;       // the cast under way, from its channel event until its end cast; null while the unit walks into range
    internal int Given;             // how many orders it has been given, so that a cast's end tells an order given inside its last event
}

// The cast flow (probe abilities-cast-timing; decisions D450, D451): an order whose name is the Order of an ability the unit has is a spell order,
// taken when the unit has the mana and the ability is off cooldown. The unit walks into the ability's range; there the channel and cast events
// fire, inside the order's call when it is in range already. The caster's cast point later the mana and cooldown are spent and the effect event
// fires, and what the spell does comes with it, or after the ability's casting time when it has one. The finish and end-cast events fire the
// caster's backswing after the effect (after the casting time instead, for an ability that has one), and the unit is idle. Another order before
// that ends the cast with the end-cast event alone: before the cast point nothing is spent, and inside a casting time the spell does nothing.
// Close: the caster turns to its target at once, where the game's Archmage took 0.21 s to turn round. Effects are data-driven for the abilities
// named in decisions D355 (Holy Light, Storm Bolt, Thunder Clap) and SpellEffects; the others only fire their events.
public sealed partial class GameNatives
{
    const int UnitSpellChannel = 289, UnitSpellCast = 290, UnitSpellEffect = 291, UnitSpellFinish = 292, UnitSpellEndCast = 293;
    const int PlayerSpellChannel = 272, PlayerSpellCast = 273, PlayerSpellEffect = 274, PlayerSpellFinish = 275, PlayerSpellEndCast = 276;
    static readonly int CastTime = Code("acas"), CastRange = Code("aran"), CastArea = Code("aare"), MirrorImage = Code("AOmi"),
        CastPoint = Code("ucpt"), CastBackswing = Code("ucbs");

    // The spells cast inside their order's call, every event of the cast with it (probe abilities-spells: Wind Walk, Immolation)
    static readonly HashSet<int> Instant = [Code("AOwk"), Code("AEim")];

    // The commands of the command card that are abilities with an order but no cast: they are the unit's own orders, not spells
    static readonly HashSet<string> Commands = ["smart", "stop", "move", "attack", "patrol", "holdposition", "setrally", "getitem", "harvest", "returnresources", "repair", "load", "unload",
        "defend", "undefend", "cancel", "build", "humanbuild", "orcbuild", "nightelfbuild", "undeadbuild", "neutralbuild", "autoharvestgold", "autoharvestlumber", "resumeharvest",
        "attackground", "attackonce", "revive", "resumebuild", "stunned"];

    // The ability of the unit whose order has this id, if it has one
    ability? SpellOf(unit u, int order)
    {
        if (_itemSpell is { } itemSpell && order >= ItemOrder) return itemSpell;
        if (OrderNames.TryGetValue(order, out var named) && Commands.Contains(named)) return null;
        if (Data.FieldAt(ObjectKind.Ability, "Order") is not { } field) return null;
        return u.Abilities.Find(a => !a.Disabled && !a.Hidden && a.Type.Text(field) is { Length: > 0 } name && OrderIds.GetValueOrDefault(name) == order);
    }

    float Field(ability a, int field) => BlzGetAbilityRealLevelField(a, Convert<abilityreallevelfield>(field), a.Level - 1);

    // Whether the unit can start the spell: alive, the mana, no cooldown running
    bool Ready(unit u, ability a) => !u.Dead && !u.Buffs.Any(b => b.Silence) && CooldownLeft(a) <= 0 && u.Mana >= BlzGetAbilityIntegerLevelField(a, Convert<abilityintegerlevelfield>(ManaCost), a.Level - 1);

    // Whether the order is a spell's; the ability it casts, or null when the unit can't cast it now
    bool Spell(unit u, int order, out ability? taken)
    {
        var ability = SpellOf(u, order);
        taken = ability is not null && Ready(u, ability) ? ability : null;
        return ability is not null;
    }

    // Another order, or the caster's death, ends a cast that had begun: its end-cast event fires, with no finish
    void BreakCast(unit u)
    {
        var cast = u.Cast;
        (u.Casting, u.Cast) = (null, null);
        if (cast is not null && !u.Removed) FireUnit(cast, UnitSpellEndCast, PlayerSpellEndCast);
    }

    // Where the casts' steps come among what is due in one instant: after the rest, in the order the units were made
    const long CastPlace = 1L << 60;

    // The next part of a cast, the seconds later, unless the cast was ended before; a paused caster's waits until it is free, and a dead one's is over.
    // Casts due in the same instant go on in the order their units were made, not the order of their orders (probe abilities-beetles: of two heroes
    // ordered in one instant, the one made first had its effect first, though it was ordered second)
    void Later(unit u, Happening cast, double seconds, Action next)
    {
        if (Game.Steps(seconds) <= 0) { next(); return; }
        _game.At(_game.Step + Game.Steps(seconds), CastPlace + u.Id, () =>
        {
            if (u.Cast != cast) return;
            if (u.Dead || u.Removed) BreakCast(u);
            else if (Held(u)) Later(u, cast, TickSeconds, next);
            else next();
        });
    }

    // One step of a casting unit: it walks into range, and there the cast begins. Inside the order's call it doesn't walk
    bool Cast(unit u, bool walk = true)
    {
        if (u.Casting is not { } ability) return Finish(u);
        if (u.Cast is not null) return true;
        var (x, y, target) = (u.OrderX, u.OrderY, u.OrderTarget);
        // shortcut: an immediate order has no place and stores 0, 0, so it is told from a point order by that place; a point order at exactly 0, 0 then casts where the caster stands
        if (target is null && x == 0 && y == 0) (x, y) = (u.X, u.Y);
        var range = Field(ability, CastRange) + (target is unit t ? t.Type.Real(Collision) : 0);
        if (range > 0 && Distance2(x - u.X, y - u.Y) > range * range) return !walk || Walk(u, x, y, follow: true);
        var cast = u.Cast = new Happening(u, Skill: ability.Type.Id, PointX: x, PointY: y, Target: target);
        var instant = Instant.Contains(ability.Type.BaseId) || u.UsedItem is not null && Field(ability, CastTime) <= 0;
        FireUnit(cast, UnitSpellChannel, PlayerSpellChannel);
        if (u.Cast == cast) FireUnit(cast, UnitSpellCast, PlayerSpellCast);
        if (u.Cast == cast) Later(u, cast, instant ? 0 : Own(u, CastPoint, u.Type.Real(CastPoint)), () => Effected(u, ability, cast, instant));
        return true;
    }

    // The cast point: the mana and the cooldown are spent, the effect event fires, and the spell does what it does, now or after its casting time
    void Effected(unit u, ability ability, Happening cast, bool instant)
    {
        var cost = BlzGetAbilityIntegerLevelField(ability, Convert<abilityintegerlevelfield>(ManaCost), ability.Level - 1);
        var cooldown = Field(ability, Cooldown);
        if (u.Mana < cost || u.Dead)
        {
            var given = u.Given;
            BreakCast(u);
            if (u.Given == given) Finish(u);
            return;
        }
        SetMana(u, u.Mana - cost);
        (ability.Cooldown, ability.CooldownEnd) = (cooldown, Seconds + cooldown);
        FireUnit(cast, UnitSpellEffect, PlayerSpellEffect);
        var casting = instant ? 0 : Field(ability, CastTime);
        if (Game.Steps(casting) > 0)
        {
            // Flame Strike's fire came with its finish event, its casting time after the effect whatever the backswing, and not at all when
            // the caster was stopped before; taken to be so for every ability with a casting time
            Later(u, cast, casting, () => { Effect(u, ability, cast.PointX, cast.PointY, cast.Target); Done(u, ability, cast); });
            return;
        }
        Effect(u, ability, cast.PointX, cast.PointY, cast.Target);
        // A channelled spell keeps its order, and its own end fires the last events (SpellEffects: Channelled)
        if (u.Cast == cast && u.Channel is not null) (u.Casting, u.Cast) = (null, null);
        // Mirror Image's cast ends with its effect (probe abilities-cast-timing)
        if (u.Cast == cast) Later(u, cast, instant || ability.Type.BaseId == MirrorImage ? 0 : Own(u, CastBackswing, u.Type.Real(CastBackswing)), () => Done(u, ability, cast));
    }

    // The cast's end: the finish and end-cast events, and the unit is idle unless one of them gave it an order. A spell that began to channel
    // after its casting time keeps its order; Mirror Image's cast has no finish event (probe abilities-spells)
    void Done(unit u, ability ability, Happening cast)
    {
        if (u.Cast != cast) return;
        if (u.Channel is not null) { (u.Casting, u.Cast) = (null, null); return; }
        if (ability.Type.BaseId != MirrorImage) FireUnit(cast, UnitSpellFinish, PlayerSpellFinish);
        if (u.Cast != cast) return;
        var given = u.Given;
        var used = u.UsedItem;
        BreakCast(u);
        if (used is not null && !used.Removed) Spend(u, used, now: false);
        if (u.Given == given && Finish(u)) Wake();
    }

    // How long a buff of the ability lasts on the unit: its duration, a hero's own where it has one
    // A spell's time on the unit: a hero's and a unit with Resistant Skin's is the ability's hero duration (the skin's tooltip; not measured)
    double Lasts(ability a, unit target) => (IsHero(target.Type.Id) || Resistant(target)) && Field(a, Code("ahdu")) is var hero and > 0 ? hero : Field(a, Code("adur"));

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
                // Storm Bolt: its missile flies as an attack's does and follows the foe, at the ability's missile speed (probe abilities-cast-timing:
                // 0.40 s to a foe 400 away at 1000, 0.80 at 500), then a hit of 0 of unknown type and the damage of type sonic (facts, Damage)
                void Land(Missile _)
                {
                    if (foe.Dead || foe.Removed) return;
                    UnitDamageTarget(caster, foe, 0, false, false, Convert<attacktype>(0), Convert<damagetype>(0), Convert<weapontype>(0));
                    UnitDamageTarget(caster, foe, amount, false, false, Convert<attacktype>(0), Convert<damagetype>(15), Convert<weapontype>(0));
                    // its stun, and a hit of 0 0.02 s after the stun's time (probe abilities-cast-timing: 5.02 s after the first two, with a stun of 5), but
                    // none on a banished unit (probe abilities-banish: none came)
                    if (!foe.Dead) Apply(foe, caster, StunBuff, Lasts(a, foe), false, b => (b.Stun, b.Ended) = (true, () => _game.At(_game.Step + Game.Steps(0.02), () =>
                    {
                        if (!foe.Dead && !foe.Removed && !caster.Removed && !IsEthereal(foe)) Hit0(caster, foe);
                    })));
                }
                Fire(caster, foe, BlzGetAbilityIntegerField(a, Convert<abilityintegerfield>(Code("amsp"))), true, Land);
                break;
            case var id when id == Code("Aslo") && target is unit slowed && !slowed.Dead:
                Hit0(caster, slowed);
                if (!slowed.Dead) Apply(slowed, caster, SlowBuff, Lasts(a, slowed), false, b => (b.SpeedFactor, b.AttackSpeedBonus) = (1 - amount, -TagValue(a.Type, "DataB" + a.Level)));
                break;
            case var id when id == Code("Ablo") && target is unit hasted && !hasted.Dead:
                Apply(hasted, caster, BloodlustBuff, Lasts(a, hasted), true, b => (b.AttackSpeedBonus, b.SpeedFactor) = (amount, 1 + TagValue(a.Type, "DataB" + a.Level)));
                break;
            case var id when id == Code("AHtc"):
                // Thunder Clap: a hit of 0 and DataA of sonic damage to the foes on the ground in the area, then its buff slows their moving by DataC and
                // their attacks by DataD for its time (a hero's own); the hit of 0 before the buff is the game's (game-facts, 11-03), the order of the two hits
                // is Storm Bolt's
                var area = Field(a, CastArea);
                foreach (var near in _units.Values.ToArray())
                    if (Hostile(caster, near) && !Flies(near) && Distance2(near.X - caster.X, near.Y - caster.Y) <= area * area)
                    {
                        Hit0(caster, near);
                        if (!near.Dead) UnitDamageTarget(caster, near, amount, false, false, Convert<attacktype>(0), Convert<damagetype>(15), Convert<weapontype>(0));
                        if (!near.Dead) Apply(near, caster, BuffOf(a, 0), Lasts(a, near), false, b => (b.SpeedFactor, b.AttackSpeedBonus) = (1 - Datum(a, 'C'), -Datum(a, 'D')));
                    }
                break;
        }
    }

    Happening? SpellResponse => Response(UnitSpellChannel, UnitSpellCast, UnitSpellEffect, UnitSpellFinish, UnitSpellEndCast,
        PlayerSpellChannel, PlayerSpellCast, PlayerSpellEffect, PlayerSpellFinish, PlayerSpellEndCast);

    /// <summary>The id of the ability being cast, in a spell event; 0 outside one.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Spells and abilities: the cast events (10-31, 11-05)); the events come from the model's cast flow (D450, D451), where the caster turns to its target at once; outside a spell event the answer is not measured.</remarks>
    [Native(Level.Close, Fact = "game-facts.md, Spells and abilities: the cast events (10-31, 11-05)")]
    public int GetSpellAbilityId() => SpellResponse?.Skill ?? 0;

    /// <summary>The caster's ability of the id being cast, in a spell event; null outside one.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Spells and abilities: the cast events (10-31, 11-05)); found by id among the caster's abilities as they are now, so one removed since gives null; the events come from the model's cast flow (D450, D451), where the caster turns to its target at once; outside a spell event the answer is not measured.</remarks>
    [Native(Level.Close, Fact = "game-facts.md, Spells and abilities: the cast events (10-31, 11-05)")]
    public ability? GetSpellAbility() => SpellResponse is { } e ? e.Unit.Abilities.Find(a => a.Type.Id == e.Skill) : null;

    /// <summary>The casting unit, in a spell event; null outside one.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Spells and abilities: the cast events (10-31, 11-05)); the events come from the model's cast flow (D450, D451), where the caster turns to its target at once; outside a spell event the answer is not measured.</remarks>
    [Native(Level.Close, Fact = "game-facts.md, Spells and abilities: the cast events (10-31, 11-05)")]
    public unit? GetSpellAbilityUnit() => SpellResponse?.Unit;

    /// <summary>The target of the cast when it is a unit, else null.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Spells and abilities: the cast events (10-31, 11-05)); the events come from the model's cast flow (D450, D451), where the caster turns to its target at once; outside a spell event the answer is not measured.</remarks>
    [Native(Level.Close, Fact = "game-facts.md, Spells and abilities: the cast events (10-31, 11-05)")]
    public unit? GetSpellTargetUnit() => SpellResponse?.Target as unit;

    /// <summary>The target of the cast when it is an item, else null.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Spells and abilities: the cast events (10-31, 11-05)); the events come from the model's cast flow (D450, D451), where the caster turns to its target at once; outside a spell event the answer is not measured.</remarks>
    [Native(Level.Close, Fact = "game-facts.md, Spells and abilities: the cast events (10-31, 11-05)")]
    public item? GetSpellTargetItem() => SpellResponse?.Target as item;

    /// <summary>The target of the cast when it is a destructable, else null.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Spells and abilities: the cast events (10-31, 11-05)); the events come from the model's cast flow (D450, D451), where the caster turns to its target at once; outside a spell event the answer is not measured.</remarks>
    [Native(Level.Close, Fact = "game-facts.md, Spells and abilities: the cast events (10-31, 11-05)")]
    public destructable? GetSpellTargetDestructable() => SpellResponse?.Target as destructable;

    /// <summary>The x the order gave, a target's place when the order was given; 0 outside a spell event.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Spells and abilities: the cast events (10-31, 11-05)); a unit target's place is where it was when the order was given, not at the cast; the events come from the model's cast flow (D450, D451), where the caster turns to its target at once; outside a spell event the answer is not measured.</remarks>
    [Native(Level.Close, Fact = "game-facts.md, Spells and abilities: the cast events (10-31, 11-05)")]
    public float GetSpellTargetX() => SpellResponse?.PointX ?? 0;

    /// <summary>The y the order gave, a target's place when the order was given; 0 outside a spell event.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Spells and abilities: the cast events (10-31, 11-05)); a unit target's place is where it was when the order was given, not at the cast; the events come from the model's cast flow (D450, D451), where the caster turns to its target at once; outside a spell event the answer is not measured.</remarks>
    [Native(Level.Close, Fact = "game-facts.md, Spells and abilities: the cast events (10-31, 11-05)")]
    public float GetSpellTargetY() => SpellResponse?.PointY ?? 0;

    /// <summary>A location at the cast's target point; null outside a spell event.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Spells and abilities: the cast events (10-31, 11-05)); a unit target's place is where it was when the order was given, not at the cast; the events come from the model's cast flow (D450, D451), where the caster turns to its target at once; outside a spell event the answer is not measured.</remarks>
    [Native(Level.Close, Fact = "game-facts.md, Spells and abilities: the cast events (10-31, 11-05)")]
    public location? GetSpellTargetLoc() => SpellResponse is { } e ? Location(e.PointX, e.PointY) : null;
}
