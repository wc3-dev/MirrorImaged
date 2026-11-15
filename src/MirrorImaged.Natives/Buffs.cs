using MirrorImaged.World;

namespace MirrorImaged.Natives;

// A buff on a unit: its type, who gave it, when it ends, and what it changes
sealed class ActiveBuff(int id, unit? source, bool positive, bool magic)
{
    public readonly int Id = id;
    public readonly unit? Source = source;
    public readonly bool Positive = positive, Magic = magic;
    public long EndsAt;                 // the step it ends at, 0 for one that lasts
    public float SpeedFactor = 1;       // times the unit's move speed
    public float AttackSpeedBonus;      // added to 1 for the attack rate
    public float ArmorBonus;            // added to the armour
    public bool Stun;                   // the unit does nothing
    public bool Invulnerable, Root;     // nothing hurts the unit; the unit can't move
    public float LifeSteal, DamageBonus; // the share of an attack's damage the attacker gets back; added to the damage of ranged attacks
}

public partial class unit
{
    internal readonly List<ActiveBuff> Buffs = [];
}

// Buffs: timed changes of a unit's speed, attack rate and armour, and the stun, given by spells and read by the natives of buffs (game-facts,
// "Spells and abilities"). A buff of a type a unit has already is refreshed, not stacked; it ends at its time, with the unit's death, or when
// dispelled; `GetUnitAbilityLevel` of its id reads 1 while it is on. Close: the stats a buff changes are the common ones (speed, attack rate,
// armour, stun), and the dispel flags are the buff's sign and whether a spell gave it (decisions D356).
public sealed partial class GameNatives
{
    static readonly int StunBuff = Code("BSTN"), SlowBuff = Code("Bslo"), BloodlustBuff = Code("Bblo");

    // The buff on the unit, made or refreshed, ending after the seconds (0: it stays); the first change's setter fills in what it does
    ActiveBuff Apply(unit target, unit? source, int id, double seconds, bool positive, Action<ActiveBuff> change)
    {
        target.Buffs.RemoveAll(b => b.Id == id);
        var buff = new ActiveBuff(id, source, positive, magic: true);
        change(buff);
        target.Buffs.Add(buff);
        if (seconds > 0)
        {
            buff.EndsAt = _game.Step + Math.Max(1, Game.Steps(seconds));
            _game.At(buff.EndsAt, () => target.Buffs.Remove(buff));
        }
        return buff;
    }

    float SpeedFactor(unit u) => u.Buffs.Aggregate(1f, (factor, buff) => factor * buff.SpeedFactor);
    float AttackRate(unit u) => Math.Max(1 + u.Buffs.Sum(buff => buff.AttackSpeedBonus), 0.2f);
    float ArmorBonus(unit u) => u.Buffs.Sum(buff => buff.ArmorBonus);
    bool IsStunned(unit u) => u.Buffs.Any(buff => buff.Stun);

    // The buffs the flags pick: the positive or negative ones, magic or not (no buff here is physical, timed life, an aura or auto-dispelled)
    IEnumerable<ActiveBuff> Picked(unit? u, bool positive, bool negative, bool magic) =>
        u is null ? [] : u.Buffs.Where(b => (b.Positive ? positive : negative) && (b.Magic ? magic : true));

    [Native(Level.Close, Fact = "game-facts.md, Spells and abilities: UnitRemoveBuffs takes the buffs off (10-31)")]
    public void UnitRemoveBuffs(unit? whichUnit, bool removePositive, bool removeNegative) => UnitRemoveBuffsEx(whichUnit, removePositive, removeNegative, true, true, false, false, false);

    [Native(Level.Close, Fact = "game-facts.md, Spells and abilities: UnitRemoveBuffs takes the buffs off (10-31)")]
    public void UnitRemoveBuffsEx(unit? whichUnit, bool removePositive, bool removeNegative, bool magic, bool physical, bool timedLife, bool aura, bool autoDispel)
    {
        if (whichUnit is null) return;
        var picked = Picked(whichUnit, removePositive, removeNegative, magic).ToArray();
        foreach (var buff in picked) whichUnit.Buffs.Remove(buff);
    }

    [Native(Level.Close, Fact = "game-facts.md, Spells and abilities: UnitCountBuffsEx (10-31)")]
    public bool UnitHasBuffsEx(unit? whichUnit, bool removePositive, bool removeNegative, bool magic, bool physical, bool timedLife, bool aura, bool autoDispel) =>
        UnitCountBuffsEx(whichUnit, removePositive, removeNegative, magic, physical, timedLife, aura, autoDispel) > 0;

    [Native(Level.Close, Fact = "game-facts.md, Spells and abilities: UnitCountBuffsEx (10-31)")]
    public int UnitCountBuffsEx(unit? whichUnit, bool removePositive, bool removeNegative, bool magic, bool physical, bool timedLife, bool aura, bool autoDispel) =>
        Picked(whichUnit, removePositive, removeNegative, magic).Count();
}
