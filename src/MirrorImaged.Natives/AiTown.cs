using MirrorImaged.Data;

namespace MirrorImaged.Natives;

// What the melee AI scripts ask before and while they attack, and the units they make from others. Close: the game's own rules for these are not
// measured; each is the smallest that gives the script the answer its code is written for (docs/decisions.md, D432 to D434).
public sealed partial class GameNatives
{
    const float TowerReach = 900;

    readonly Dictionary<player, unit> _allianceTargets = [];

    bool AiFoe(unit u) => !u.Dead && !u.Removed && !u.Hidden && u.Owner != Ai && GetPlayerId(u.Owner) < 24 && IsPlayerEnemy(Ai, u.Owner);

    /// <summary>Whether an armed unit of an enemy player stands within reach of one of the AI player's town halls; the script then keeps its army home.</summary>
    [Native(Level.Close)]
    public bool TownThreatened() =>
        Mine(Ai).Where(u => !u.Constructing && IsTownHall(u)).ToList() is { Count: > 0 } halls && _units.Values.Any(u => AiFoe(u) && !IsBuildingNow(u) && u.Type.Integer(WeaponsOn) != 0
            && halls.Any(hall => Distance2(u.X - hall.X, u.Y - hall.Y) <= TownReach * TownReach));

    /// <summary>Whether a finished building of an enemy that attacks stands near the unit; the script then waits for siege before it goes there.</summary>
    [Native(Level.Close)]
    public bool IsTowered(unit? target) =>
        target is not null && _units.Values.Any(t => AiFoe(t) && !t.Constructing && IsBuildingNow(t) && t.Type.Integer(WeaponsOn) != 0
            && Distance2(t.X - target.X, t.Y - target.Y) <= TowerReach * TowerReach);

    /// <summary>Whether a buff hides the unit, whoever looks.</summary>
    [Native(Level.Close)]
    public bool UnitInvis(unit? id) => id is not null && id.Buffs.Any(b => b.Invisible);

    /// <summary>Whether a creep lives.</summary>
    [Native(Level.Close)]
    public bool CreepsOnMap() => _units.Values.Any(c => !c.Dead && !c.Removed && GetPlayerId(c.Owner) == GetPlayerNeutralAggressive() && !IsBuildingNow(c));

    /// <summary>Tells the AI player's allies what it goes to attack; nil takes it back.</summary>
    [Native(Level.Close)]
    public void SetAllianceTarget(unit? id)
    {
        if (id is null) _allianceTargets.Remove(Ai);
        else _allianceTargets[Ai] = id;
    }

    /// <summary>What an allied AI player said it goes to attack, while it lives (the lowest slot's first); nil for none.</summary>
    [Native(Level.Close)]
    public unit? GetAllianceTarget() =>
        _allianceTargets.Where(told => told.Key != Ai && IsPlayerAlly(told.Key, Ai) && !told.Value.Dead && !told.Value.Removed)
            .OrderBy(told => told.Key.Number).Select(told => told.Value).FirstOrDefault();

    unit? Ready(int id) => Mine(Ai).Where(u => u.Type.Id == id && !u.Constructing).OrderBy(u => u.Id).FirstOrDefault();

    // The unit becomes one of the other type where it stands, for that type's price; false when the AI player can't pay
    bool Becomes(unit from, GameObject type)
    {
        if (!CanPay(Ai, type.Integer(Cost), type.Integer(LumberCost))) return false;
        Pay(Ai, type.Integer(Cost), type.Integer(LumberCost));
        var (x, y, facing) = (from.X, from.Y, GetUnitFacing(from));
        RemoveUnit(from);
        CreateUnit(Ai, type.Id, x, y, facing);
        return true;
    }

    /// <summary>Makes units of the last type out of pairs of the first two (an archer mounts a hippogryph) until the AI player has the number; true when it has.</summary>
    [Native(Level.Close)]
    public bool MergeUnits(int qty, int a, int b, int make)
    {
        if (Data.Object(ObjectKind.Unit, make) is not { } type) return false;
        while (CountOf(Ai, make, false) < qty && Ready(a) is { } rider && Ready(b) is { } mount && mount != rider)
        {
            if (!Becomes(mount, type)) break;
            RemoveUnit(rider);
        }
        return CountOf(Ai, make, false) >= qty;
    }

    // The type a unit's ability turns it into (an Obsidian Statue's Destroyer Form), or none
    GameObject? OtherForm(unit u) =>
        u.Abilities.Select(a => (a.Type.Text(Code("Emeu"), 1) ?? a.Type.Text(Code("Emeu"))) is { Length: 4 } id ? Data.Object(ObjectKind.Unit, Code(id)) : null).FirstOrDefault(form => form is not null && form.Id != u.Type.Id);

    /// <summary>Turns units of the type into what their ability makes of them until the AI player has the number of those; true when it has.</summary>
    [Native(Level.Close)]
    public bool ConvertUnits(int qty, int id)
    {
        if (Ready(id) is not { } first || OtherForm(first) is not { } form) return false;
        while (CountOf(Ai, form.Id, false) < qty && Ready(id) is { } from && Becomes(from, form)) { }
        return CountOf(Ai, form.Id, false) >= qty;
    }
}
