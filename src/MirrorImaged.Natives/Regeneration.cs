using MirrorImaged.World;

namespace MirrorImaged.Natives;

public partial class unit
{
    internal Action<unit>? Regenerate;   // adds the mana that came back since ManaTick, before the mana is set or what decides it changes
    internal Func<unit, float>? Regrown; // the mana as it is now, with what came back since ManaTick, changing nothing: reading it can't change the run (D700)
    internal long ManaTick;              // the engine step (1/32 s) its mana is as of
    float _mana, _maxMana;
    internal float RawMana => _mana;

    internal float Mana
    {
        get => Regrown?.Invoke(this) ?? _mana;
        set { Regenerate?.Invoke(this); _mana = value; }
    }

    internal float MaxMana
    {
        get => _maxMana;
        set { Regenerate?.Invoke(this); _maxMana = value; }
    }

    internal float Raised(float mana) => Math.Min(_mana + mana, Math.Max(_maxMana, _mana));
    internal void Fold(float mana) => _mana = Raised(mana);
}

// Mana comes back by itself (probe heroes-mana-regeneration): to a hero IntRegenBonus (0.05) a second for each point of its intelligence, its
// type's own regeneration not counting (an Archmage of intelligence 0 got none back), and to any other unit its type's regeneration (umpr); to a
// hidden unit too, not to a paused or dead one, and not past the most. It is added when the mana is read or set and before what decides it changes
// (a pause, a death, the intelligence, the most mana), so nothing runs while no one looks, and it comes in the engine's steps of 1/32 s, so the
// mana doesn't move inside one: a mana event's second look a frame after a set still sees what was set (decisions D455).
// shortcut: a unit state event on mana doesn't fire as regeneration passes its limit; upgrade by scheduling the crossing when one is registered.
public sealed partial class GameNatives
{
    static readonly int ManaRegeneration = Code("umpr");

    float ManaRate(unit u) => IsHero(u.Type.Id) ? WithBonus(u, 2) * Data.Constant("IntRegenBonus", 0.05f) : Own(u, ManaRegeneration, u.Type.Real(ManaRegeneration));

    long ManaTicks(unit u) => _game.Step / Game.Steps(TickSeconds) - u.ManaTick;

    // What came back since ManaTick, as a sum made once for the whole time, so the mana at a step is the same whenever, and however often, it is read
    float? Regained(unit u) => ManaTicks(u) is > 0 and var ticks && !u.Dead && !u.Paused && ManaRate(u) is > 0 and var rate ? (float)(rate * ticks * TickSeconds) : null;

    float RegrownMana(unit u) => Regained(u) is { } gain ? u.Raised(gain) : u.RawMana;

    void Regenerate(unit u)
    {
        if (ManaTicks(u) <= 0) return;
        if (Regained(u) is { } gain) u.Fold(gain);
        u.ManaTick = _game.Step / Game.Steps(TickSeconds);
    }
}
