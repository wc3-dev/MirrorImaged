using MirrorImaged.World;

namespace MirrorImaged.Natives;

// A buff on a unit: its type, who gave it, when it ends, and what it changes
sealed class ActiveBuff(int id, unit? source, bool positive, bool magic)
{
    public readonly int Id = id;
    public readonly unit? Source = source;
    public readonly bool Positive = positive, Magic = magic;
    public long EndsAt;                 // the step it ends at, 0 for one that lasts or whose unit is paused
    public long Left;                   // the steps it has left, while its unit is paused
    public float SpeedFactor = 1;       // times the unit's move speed
    public float AttackSpeedBonus;      // added to 1 for the attack rate
    public float ArmorBonus;            // added to the armour
    public bool Stun;                   // the unit does nothing
    public bool Invulnerable, Root, Silence;     // nothing hurts the unit; the unit can't move
    public bool Ethereal;               // Banish: the unit can't attack, and only magic reaches it
    public Action? Ended;               // what happens when its time is over, not when it is taken off before
    public float LifeSteal, DamageBonus; // the share of an attack's damage the attacker gets back; added to the damage of ranged attacks
    public bool Sneaks, Invisible;      // Wind Walk: the unit attacks nothing by itself; its foes don't see it
    public float Backstab;              // added to its next attack, which ends the buff
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
            buff.Left = Math.Max(1, Game.Steps(seconds));
            if (!target.Paused) Run(target, buff);
        }
        return buff;
    }

    // The buff's time runs from now for what it has left
    void Run(unit target, ActiveBuff buff)
    {
        var ends = buff.EndsAt = _game.Step + buff.Left;
        buff.Left = 0;
        _game.At(ends, () => { if (buff.EndsAt == ends && target.Buffs.Remove(buff)) buff.Ended?.Invoke(); });
    }

    // A buff's time stands still while its unit is paused (probe abilities-cast-timing: 16 s of Silence on a Priest paused for 20 s of them
    // ended 36 s in, and on one paused 4 s in and freed at 20 s, 32 s in)
    void HoldBuffs(unit u)
    {
        foreach (var buff in u.Buffs.ToArray())
            if (u.Paused && buff.EndsAt > 0) (buff.Left, buff.EndsAt) = (Math.Max(1, buff.EndsAt - _game.Step), 0);
            else if (!u.Paused && buff.Left > 0) Run(u, buff);
    }

    float SpeedFactor(unit u) => u.Buffs.Aggregate(1f, (factor, buff) => factor * buff.SpeedFactor);
    // How many times faster the unit attacks: a hero's agility adds AgiAttackSpeedBonus (0.02) a point, and the rate is 5 at most (probe
    // units-attack-timing: a Paladin's 2.20 s a hit is 1.10 at agility 50 and 0.44 from 200 up). That the buffs' share adds to the agility's is not measured
    float AttackRate(unit u) =>
        Math.Clamp(1 + (IsHero(u.Type.Id) ? WithBonus(u, 1) * Data.Constant("AgiAttackSpeedBonus", 0.02f) : 0) + u.Buffs.Sum(buff => buff.AttackSpeedBonus), 0.2f, 5);
    float ArmorBonus(unit u) => u.Buffs.Sum(buff => buff.ArmorBonus);
    bool IsStunned(unit u) => u.Buffs.Any(buff => buff.Stun);
    bool IsEthereal(unit u) => u.Buffs.Any(buff => buff.Ethereal);
    bool Sneaks(unit u) => u.Buffs.Any(buff => buff.Sneaks);

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
