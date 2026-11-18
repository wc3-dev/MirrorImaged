using MirrorImaged.World;

namespace MirrorImaged.Natives;

public partial class unit
{
    internal bool Swarming; // a locust of Locust Swarm: the spell moves it and hits with it, the unit engine leaves it alone
}

// Locust Swarm (probe abilities-locust): DataA locusts of the ability's unit come out of the caster, the first with the effect and one every
// DataB seconds after, and are gone a little after the ability's time. A locust hits a foe within the ability's area of the caster with its
// own attack; after four hits it is back at the caster, which gets DataD of what the four took, and what the locusts carry as the swarm ends
// comes back too, so the caster gets DataD of all the swarm dealt. No summon event fires. Close: the game's locusts fly about and choose
// among the foes, most of them the one nearest the caster and fewer the next; here the foes are taken nearest the caster first, DataC
// locusts each (7: the field's name is the most swarm units a target has, which was not measured), and the rest of the swarm takes the
// last. A locust hits its foe every 3 s (the game's hits were 2.8 to 3.3 s apart on average, 0.87 at least), the first 2 s after it comes
// out, and is back 1 s after its fourth hit; the swarm ends with its caster's death, which is not measured. Some of the game's locusts came
// back with five hits from foes of more armour, which is not modelled, and DataE (20) was not seen to do anything.
public sealed partial class GameNatives
{
    const double LocustFirstHit = 2, LocustHitEvery = 3, LocustBack = 1, LocustsLinger = 1.6;
    const int LocustHits = 4;

    void LocustSwarm(unit caster, ability a)
    {
        var type = SummonTypes(a).FirstOrDefault();
        var (area, share, ends) = (Field(a, CastArea), Datum(a, 'D'), _game.Step + Game.Steps(Field(a, Code("adur"))));
        for (var n = 0; type != 0 && n < (int)Datum(a, 'A'); n++)
        {
            var place = n / Math.Max((int)Datum(a, 'C'), 1);
            _game.At(_game.Step + Game.Steps(n * Datum(a, 'B')), () =>
            {
                if (caster.Dead || Create(caster.Owner, type, caster.X, caster.Y, 270, type) is not { } locust) return;
                locust.Swarming = true;
                var (load, hits) = (0f, 0);
                void Back()
                {
                    if (locust.Removed) return;
                    Move(locust, caster.X, caster.Y);
                    if (!caster.Dead) SetLife(caster, Math.Min(caster.Life + load * share, caster.MaxLife));
                    (load, hits) = (0, 0);
                }
                void Bite()
                {
                    if (locust.Removed || _game.Step >= ends) return;
                    if (caster.Dead) { RemoveUnit(locust); return; }
                    var foes = Foes(caster, caster.X, caster.Y, area).OrderBy(foe => Distance(caster, foe)).ThenBy(foe => foe.Id).ToArray();
                    if (foes.Length > 0)
                    {
                        var foe = foes[Math.Min(place, foes.Length - 1)];
                        var life = foe.Life;
                        Move(locust, foe.X, foe.Y);
                        Hit(locust, foe, 0);
                        load += life - foe.Life;
                        if (++hits == LocustHits) _game.At(_game.Step + Game.Steps(LocustBack), Back);
                    }
                    _game.At(_game.Step + Game.Steps(LocustHitEvery), Bite);
                }
                _game.At(_game.Step + Game.Steps(LocustFirstHit), Bite);
                _game.At(ends, Back);
                _game.At(ends + Game.Steps(LocustsLinger), () => RemoveUnit(locust));
            });
        }
    }
}
