namespace MirrorImaged.Natives;

public partial class unit
{
    internal item? UsedItem; // the carried item whose ability its current cast is of, spent when the cast ends
}

// UnitUseItemPoint and UnitUseItemTarget (probe items-use-at, facts, "Items, equipment and the camera"; D794): the order of the item's slot (852008 and the
// slot) is given as a point or target order and casts the ability the item put on its carrier, as a spell order casts it, at once for an ability without a
// casting time and a target in range; when the cast ends the use event fires and a charge is spent, as UnitUseItem's. The target native answers true when the
// order was given; the point one answers false, whether or not it gave one (all 20 of the game's calls did). Close: whether an ability takes a place, a unit or
// nothing isn't in the data and is read from its targets (a place when no friend or structure is named), and the game walked to some targets (a place 316 away, an enemy
// 141 away) that the cast ranges of the data let be cast from here.
public sealed partial class GameNatives
{
    const string ItemUseFacts = "probe items-use-at (11-21, D794)";
    const int ItemOrder = 852008;
    static readonly int AbilityTargets = Code("atar");

    ability? _itemSpell;
    item? _itemToUse;

    // The tokens of the ability's targets
    HashSet<string> Targeted(ability a) => [.. (a.Type.Text(AbilityTargets, a.Level) ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];

    // What the ability of a carried item is aimed at: 0 nothing, 1 a place, 2 a unit
    int Aim(ability a)
    {
        var targets = Targeted(a);
        return targets.Count == 0 ? 0 : targets.Contains("friend") || targets.Contains("structure") ? 2 : 1;
    }

    // Whether the ability takes the unit as its target: its alignment when it names one, its kind (a structure or what moves)
    bool Takes(unit caster, ability a, unit target)
    {
        var targets = Targeted(a);
        string[] alignments = ["self", "friend", "enemy", "neutral"];
        var side = target == caster ? "self" : IsPlayerAlly(caster.Owner, target.Owner) ? "friend" : GetPlayerId(target.Owner) == GetPlayerNeutralPassive() ? "neutral" : "enemy";
        if (targets.Overlaps(alignments) && !targets.Contains(side)) return false;
        return targets.Contains(IsBuildingNow(target) ? "structure" : Flies(target) ? "air" : "ground");
    }

    bool UseItem(unit? whichUnit, item? whichItem, int aim, float x, float y, widget? target)
    {
        if (whichUnit is null || whichUnit.Dead || Live(whichItem) is not { } i || i.Holder != whichUnit) return false;
        var slot = Array.IndexOf(whichUnit.Slots, i);
        if (slot < 0 || i.Given.Find(a => Aim(a) == aim && (aim != 2 || target is unit t && Takes(whichUnit, a, t))) is not { } ability) return false;
        (_itemSpell, _itemToUse) = (ability, i);
        try { return Give(whichUnit, ItemOrder + slot, x, y, target, aim == 2 ? 2 : 1); }
        finally { (_itemSpell, _itemToUse) = (null, null); }
    }

    /// <summary>Gives the order of the item's slot at the point, casting its ability as a spell order does; always false.</summary>
    /// <remarks>Close: Always false, as in the game's 20 calls, but the order is given and the ability cast as the game did; whether an ability takes a place is read from its targets, not from the data, and a place the game walked to from 316 away is cast from here when within the ability's range (probe differences).</remarks>
    [Native(Level.Close, Fact = ItemUseFacts, Reason = "Always false, as in the game's 20 calls, but the order is given and the ability cast as the game did; whether an ability takes a place is read from its targets, not from the data, and a place the game walked to from 316 away is cast from here when within the ability's range (probe differences)")]
    public bool UnitUseItemPoint(unit? whichUnit, item? whichItem, float x, float y)
    {
        UseItem(whichUnit, whichItem, 1, x, y, null);
        return false;
    }

    /// <summary>Gives the order of the item's slot on the target, casting its ability as a spell order does; true when the order was given.</summary>
    /// <remarks>Close: True when the order of the slot is given, for a target the ability's alignment and kind take, false with no order for another (the hero itself, a footman for Town Portal); the game walked to an enemy 141 away and kept Town Portal to its channel event for 1.5 s, where here the cast ranges let them cast at once, and whether an ability takes a unit is read from its targets, not from the data.</remarks>
    [Native(Level.Close, Fact = ItemUseFacts, Reason = "True when the order of the slot is given, for a target the ability's alignment and kind take, false with no order for another (the hero itself, a footman for Town Portal); the game walked to an enemy 141 away and kept Town Portal to its channel event for 1.5 s, where here the cast ranges let them cast at once, and whether an ability takes a unit is read from its targets, not from the data")]
    public bool UnitUseItemTarget(unit? whichUnit, item? whichItem, widget? target) =>
        target is not null && UseItem(whichUnit, whichItem, 2, GetWidgetX(target), GetWidgetY(target), target);
}
