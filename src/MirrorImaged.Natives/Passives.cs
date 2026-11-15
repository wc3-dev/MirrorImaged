namespace MirrorImaged.Natives;

// The passive abilities of an attack's chance (wave 4): Bash (a chance of extra damage and a stun), Critical Strike (a chance of a multiple of the damage)
// and Evasion (a chance that the attack of an enemy misses). Each draws from the game's random numbers when its unit attacks or is attacked, reading its
// chance and numbers from the ability's data by level (docs/decisions.md, D384). Close: the abilities of other names with the same data (Orb of Venom, Pulverize,
// Cleave, Pillage...) are not here.
public sealed partial class GameNatives
{
    static readonly int BashBase = Code("AHbh"), CriticalBase = Code("AOcr"), EvasionBase = Code("AEev");

    ability? Passive(unit u, int baseId) => u.Abilities.Find(a => !a.Disabled && a.Level > 0 && a.Type.BaseId == baseId);

    // Whether the target gets out of the attack, by its Evasion
    bool Evades(unit target, unit attacker) =>
        Passive(target, EvasionBase) is { } evasion && _game.RandomInt(1, 100) <= Datum(evasion, 'A') * 100;

    // What Bash and Critical Strike do to the damage of a hit about to land, and whether a stun comes with it
    void PassivesOfAttack(unit attacker, unit target, bool ranged, ref float damage, ref bool stun)
    {
        if (Passive(attacker, CriticalBase) is { } critical && _game.RandomInt(1, 100) <= Datum(critical, 'A')) damage *= Math.Max(Datum(critical, 'B'), 1);
        if (!ranged && Passive(attacker, BashBase) is { } bash && _game.RandomInt(1, 100) <= Datum(bash, 'A'))
        {
            damage += Datum(bash, 'C');
            stun = true;
        }
    }

    double StunSeconds(unit attacker, unit target) => Passive(attacker, BashBase) is { } bash ? Lasts(bash, target) : 0;
}
