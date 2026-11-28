using MirrorImaged.World;

namespace MirrorImaged.Natives;

// A buff on a unit: its type, who gave it, when it ends, and what it changes
sealed class ActiveBuff(int id, unit? source, bool positive, bool magic)
{
    public readonly int Id = id;
    public readonly unit? Source = source;
    public readonly bool Positive = positive;
    public bool Magic = magic;
    public long EndsAt;                 // the step it ends at, 0 for one that lasts or whose unit is paused
    public long Left;                   // the steps it has left, while its unit is paused
    public float SpeedFactor = 1;       // times the unit's move speed
    public float AttackSpeedBonus;      // added to 1 for the attack rate
    public float ArmorBonus;            // added to the armour
    public bool Stun;                   // the unit does nothing
    public bool Invulnerable, Root, Silence;     // nothing hurts the unit; the unit can't move
    public bool Ethereal;               // Banish: the unit can't attack, and only magic reaches it
    public Action? Ended;               // what happens when its time is over, not when it is taken off before
    public Action? Dispelled;           // what happens when it is taken off before its time
    public float LifeSteal, DamageBonus; // the share of an attack's damage the attacker gets back; added to the damage of ranged attacks
    public bool Sneaks, Invisible;      // Wind Walk: the unit attacks nothing by itself; its foes don't see it
    public float Backstab;              // added to its next attack, which ends the buff
    // The unit kinds IsUnitType reads from it: Ensnare's and Web's snare, the poisons, Disease Cloud's plague, Polymorph's and Hex's form, Avatar's immunity
    public bool Snare, Poison, Plague, Polymorph, MagicImmune;
    public bool Aura;                   // given by an aura: not magic, timed or dispellable
    public float? PerSecond;            // a hit of this much from its giver each second while it is on (a poison, a plague), of the damage type
    public int TickType;
    public bool Physical => !Magic && !Aura;
    public bool Timed => !Aura && (EndsAt > 0 || Left > 0);
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

    // The buff on the unit, made or refreshed, ending after the seconds (0: it stays); the first change's setter fills in what it does. A unit
    // holds one buff of an id: a second cast's strength applies at once, a weaker one's too, and the buff ends at the later of the two ends, so a cast
    // never shortens it (game-facts, 10-31: Faerie Fire and Inner Fire of two levels, from one caster and two, and Soul Burn)
    ActiveBuff Apply(unit target, unit? source, int id, double seconds, bool positive, Action<ActiveBuff> change)
    {
        var had = target.Buffs.Find(b => b.Id == id);
        var left = had is null ? 0 : had.Left > 0 ? had.Left : had.EndsAt > 0 ? had.EndsAt - _game.Step : 0;
        target.Buffs.RemoveAll(b => b.Id == id);
        var stunned = IsStunned(target);
        var buff = new ActiveBuff(id, source, positive, magic: true);
        change(buff);
        target.Buffs.Add(buff);
        if (buff.Stun && !stunned) StunBegins(target, source);
        // One clock of hits a buff id, which a refreshed buff keeps
        if (buff.PerSecond is not null && had?.PerSecond is null) Tick(target, id);
        if (seconds > 0)
        {
            buff.Left = Math.Max(Math.Max(1, Game.Steps(seconds)), left);
            if (!target.Paused) Run(target, buff);
        }
        return buff;
    }

    // A buff's hit each second while a buff of the id is on, from its giver (game-facts, 11-03: a dryad's Slow Poison of 0 a second hit a footman with
    // 0 every second while it lasted); its time stands still in a pause, its hits don't (D456)
    void Tick(unit target, int id) => _game.At(_game.Step + Game.Steps(1), () =>
    {
        if (target.Dead || target.Removed || target.Buffs.Find(b => b.Id == id) is not { PerSecond: { } amount } buff) return;
        if (buff.Source is { Removed: false } source)
            UnitDamageTarget(source, target, amount, false, false, Convert<attacktype>(0), Convert<damagetype>(buff.TickType), Convert<weapontype>(0));
        Tick(target, id);
    });

    // The buff's time runs from now for what it has left
    void Run(unit target, ActiveBuff buff)
    {
        var ends = buff.EndsAt = _game.Step + buff.Left;
        buff.Left = 0;
        _game.At(ends, () =>
        {
            if (buff.EndsAt != ends || !target.Buffs.Remove(buff)) return;
            buff.Ended?.Invoke();
            Freed(target, buff);
        });
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
    bool Has(unit u, Func<ActiveBuff, bool> kind) => u.Buffs.Any(kind);

    // The buffs the flags pick (probe units-kinds, on a slowed footman, one in Devotion Aura, an ensnared and a poisoned one): the positive or negative
    // ones, an aura's only with the aura flag, and of those the ones that are each kind the other flags ask for: magic (Slow), physical (Ensnare, a
    // poison), timed, dispellable (Slow; not Ensnare or a poison). So magic and physical together, and every flag, pick none (game-facts, 10-31 too)
    IEnumerable<ActiveBuff> Picked(unit? u, bool positive, bool negative, bool magic, bool physical, bool timedLife, bool aura, bool autoDispel) =>
        u is null ? [] : u.Buffs.Where(b => (b.Positive ? positive : negative) && (aura || !b.Aura) && (!magic || b.Magic) && (!physical || b.Physical)
            && (!timedLife || b.Timed) && (!autoDispel || b.Magic));

    /// <summary>Takes off the unit's positive buffs, its negative ones or both.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Spells and abilities: UnitRemoveBuffs takes the buffs off (10-31)); an aura's buff is taken as a spell's is, and comes back with the aura's next pulse.</remarks>
    [Native(Level.Close, Fact = "game-facts.md, Spells and abilities: UnitRemoveBuffs takes the buffs off (10-31)")]
    public void UnitRemoveBuffs(unit? whichUnit, bool removePositive, bool removeNegative) => UnitRemoveBuffsEx(whichUnit, removePositive, removeNegative, false, false, false, true, false);

    /// <summary>Takes off the unit's buffs of the signs asked for, an aura's only with the aura flag, and of them the ones of each kind the other flags ask for: magic, physical, timed, dispellable.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Spells and abilities: UnitRemoveBuffs takes the buffs off (10-31)); the flags pick as probe units-kinds measured them on four kinds of buff; an attack's poison is one buff here where Envenomed Spears gave two.</remarks>
    [Native(Level.Close, Fact = "game-facts.md, Spells and abilities: UnitRemoveBuffs takes the buffs off (10-31)")]
    public void UnitRemoveBuffsEx(unit? whichUnit, bool removePositive, bool removeNegative, bool magic, bool physical, bool timedLife, bool aura, bool autoDispel)
    {
        if (whichUnit is null) return;
        var picked = Picked(whichUnit, removePositive, removeNegative, magic, physical, timedLife, aura, autoDispel).ToArray();
        foreach (var buff in picked) Dispel(whichUnit, buff);
    }

    /// <summary>True when UnitCountBuffsEx finds at least one buff.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Spells and abilities: UnitCountBuffsEx (10-31)); the flags pick as probe units-kinds measured them on four kinds of buff; an attack's poison is one buff here where Envenomed Spears gave two.</remarks>
    [Native(Level.Close, Fact = "game-facts.md, Spells and abilities: UnitCountBuffsEx (10-31); probe units-kinds (11-28)")]
    public bool UnitHasBuffsEx(unit? whichUnit, bool removePositive, bool removeNegative, bool magic, bool physical, bool timedLife, bool aura, bool autoDispel) =>
        UnitCountBuffsEx(whichUnit, removePositive, removeNegative, magic, physical, timedLife, aura, autoDispel) > 0;

    /// <summary>How many of the unit's buffs the flags pick, as UnitRemoveBuffsEx picks them; 0 for a null unit.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Spells and abilities: UnitCountBuffsEx (10-31)); the flags pick as probe units-kinds measured them on four kinds of buff; an attack's poison is one buff here where Envenomed Spears gave two.</remarks>
    [Native(Level.Close, Fact = "game-facts.md, Spells and abilities: UnitCountBuffsEx (10-31); probe units-kinds (11-28)")]
    public int UnitCountBuffsEx(unit? whichUnit, bool removePositive, bool removeNegative, bool magic, bool physical, bool timedLife, bool aura, bool autoDispel) =>
        Picked(whichUnit, removePositive, removeNegative, magic, physical, timedLife, aura, autoDispel).Count();

    // A buff taken off before its time: what it held ends with it (a polymorphed unit's form comes back)
    void Dispel(unit u, ActiveBuff buff)
    {
        if (!u.Buffs.Remove(buff)) return;
        buff.Dispelled?.Invoke();
        Freed(u, buff);
    }

    // A buff gone from a living unit: the last stun's end lets it go (LetGo)
    void Freed(unit u, ActiveBuff buff)
    {
        if (buff.Stun && !u.Dead) LetGo(u);
    }
}
