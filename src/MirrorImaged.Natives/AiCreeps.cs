namespace MirrorImaged.Natives;

public partial class unit
{
    internal bool? CreepGuard;           // what SetUnitCreepGuard said; null for the owner's own way
    internal (float X, float Y)? Post;   // where the unit guards, from the first step it stood idle
}

// A creep guards the place it stands at: it fights what comes near, follows it no further than the game's GuardReturnDistance from its post, and
// walks back when it has nothing to fight. Close: the distance is the constant's, the rule is not measured (decisions D435).
public sealed partial class GameNatives
{
    bool Guards(unit u) => u.CreepGuard ?? GetPlayerId(u.Owner) == GetPlayerNeutralAggressive();

    // An idle unit that guards a post and is off it goes back; true when it was sent
    bool GoBackToPost(unit u)
    {
        if (!Guards(u) || GetUnitMoveSpeed(u) <= 0) return false;
        var (x, y) = u.Post ??= (u.X, u.Y);
        var limit = u.AttackTarget is null ? 64 : Data.Constant("GuardReturnDistance", 1000);
        if (Distance2(u.X - x, u.Y - y) <= limit * limit) return false;
        u.AttackTarget = null;
        Give(u, OrderMoveId, x, y, null, 1);
        return true;
    }

    /// <summary>Whether the unit guards its post as a creep does: a creep does until told not to, another unit once told to, from where it stands then.</summary>
    /// <remarks>Close: The distance is the constant's, the rule itself is not measured (D435).</remarks>
    [Native(Level.Close)]
    public void SetUnitCreepGuard(unit? whichUnit, bool creepGuard)
    {
        if (whichUnit is null) return;
        if (creepGuard && !Guards(whichUnit)) whichUnit.Post = (whichUnit.X, whichUnit.Y);
        whichUnit.CreepGuard = creepGuard;
    }

    /// <summary>The unit no longer goes back to a post.</summary>
    /// <remarks>Close: The distance is the constant's, the rule itself is not measured (D435).</remarks>
    [Native(Level.Close)]
    public void RemoveGuardPosition(unit? hUnit)
    {
        if (hUnit is not null) (hUnit.CreepGuard, hUnit.Post) = (false, null);
    }

    /// <summary>The unit guards where it stands now.</summary>
    /// <remarks>Close: The distance is the constant's, the rule itself is not measured (D435).</remarks>
    [Native(Level.Close)]
    public void RecycleGuardPosition(unit? hUnit)
    {
        if (hUnit is not null) (hUnit.CreepGuard, hUnit.Post) = (true, (hUnit.X, hUnit.Y));
    }

    /// <summary>None of the player's units goes back to a post.</summary>
    /// <remarks>Close: The distance is the constant's, the rule itself is not measured (D435).</remarks>
    [Native(Level.Close)]
    public void RemoveAllGuardPositions(player? num)
    {
        foreach (var u in _units.Values.Where(u => u.Owner == num)) RemoveGuardPosition(u);
    }
}
