using MirrorImaged.Data;
using MirrorImaged.World;

namespace MirrorImaged.Natives;

public partial class unit
{
    internal double CorpseAt;                    // when a killed unit's death time is over and its body is a corpse; 0 for one made as a corpse
    internal readonly List<unit> Beetles = [];   // the beetles its Carrion Beetles called
}

// Carrion Beetles (probe abilities-beetles): the ability's order takes a corpse as its target and no other form; carrionscarabsinstant
// takes none and uses a corpse within the ability's area, and the autocast orders switch on a cast whenever the ability is ready and a
// corpse is there. A body is a corpse once its death time is over (a footman's 3 s) or when CreateCorpse made it, of either side. DataA
// beetles of the type in DataC rise where the corpse was, which is gone; they have no timed life, and one that would be more than DataE
// of the caster's dies as it comes. Close: which corpse of several the untargeted order takes (here the nearest), where the beetles
// stand, and a hero's or a building's body, which are left alone here, are not measured.
public sealed partial class GameNatives
{
    const int BeetlesOrderId = 852551, BeetlesOn = 852552, BeetlesOff = 852553, BeetlesInstant = 852554;
    const double AutocastEvery = 0.25;
    static readonly int BeetlesBase = Code("AUcb");

    bool IsCorpse(unit u) => u.Dead && !u.Removed && Seconds >= u.CorpseAt && !IsHero(u.Type.Id) && !IsBuildingNow(u);

    unit? CorpseNear(unit caster, ability a) =>
        _units.Values.Where(u => IsCorpse(u) && Distance(caster, u) <= Field(a, CastArea)).OrderBy(u => Distance(caster, u)).ThenBy(u => u.Id).FirstOrDefault();

    // One of the four orders given to a unit with the ability: the answer when the order ends here, null when it goes on as the
    // ability's order at a corpse
    bool? BeetlesOrder(unit u, ref int order, ref float x, ref float y, ref widget? target, ref int kind)
    {
        if (order is < BeetlesOrderId or > BeetlesInstant || u.Abilities.Find(a => a.Type.BaseId == BeetlesBase && !a.Disabled) is not { } ability) return null;
        if (order is BeetlesOn or BeetlesOff)
        {
            if (kind != 0) return false;
            var on = order == BeetlesOn && !ability.Autocast;
            ability.Autocast = order == BeetlesOn;
            if (on) Autocast(u, ability);
            return true;
        }
        if (kind == 0 && order == BeetlesInstant) target = CorpseNear(u, ability);
        else if (kind != 2) return false;
        if (target is not unit body || !IsCorpse(body)) return false;
        (order, x, y, kind) = (BeetlesOrderId, body.X, body.Y, 2);
        return null;
    }

    void Autocast(unit u, ability a)
    {
        if (!a.Autocast || u.Dead || u.Removed || !u.Abilities.Contains(a)) return;
        if (u.Casting is null && !u.Paused && Ready(u, a) && CorpseNear(u, a) is { } body) IssueTargetOrderById(u, BeetlesOrderId, body);
        _game.At(_game.Step + Game.Steps(AutocastEvery), () => Autocast(u, a));
    }

    void Beetles(unit caster, ability a, unit body)
    {
        var (x, y) = (body.X, body.Y);
        RemoveUnit(body);
        var type = Data.FieldAt(ObjectKind.Ability, "DataC") is { } field && a.Type.Text(Data.Own(a.Type, field), a.Level) is { Length: 4 } id ? Code(id) : SummonTypes(a).FirstOrDefault();
        for (var n = 0; n < Math.Max((int)Datum(a, 'A'), 1); n++)
        {
            if (Create(caster.Owner, type, x, y, 270, type) is not { } beetle) continue;
            caster.Beetles.RemoveAll(b => b.Dead || b.Removed);
            caster.Beetles.Add(beetle);
            FireUnit(new(caster, Other: beetle, Skill: a.Type.Id), UnitSummon, PlayerUnitSummon);
            if (Datum(a, 'E') is > 0 and var most && caster.Beetles.Count > most) Kill(beetle);
        }
    }
}
