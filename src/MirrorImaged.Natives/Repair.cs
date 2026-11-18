namespace MirrorImaged.Natives;

// Repair (wave 3): a worker given the repair order on a hurt building of its owner or an ally walks to it and restores its life at the rate the build time
// and the ratios give, paying its price as it goes, until the building is whole, the player cannot pay or the order changes. Close: the cost and time
// ratios (RepairCostRatio, RepairTimeRatio, 0.35 and 1.5) are the constants' names as I know them, and mechanical units are not repaired (decisions D387).
public sealed partial class GameNatives
{
    const int OrderRepair = 852024;

    bool Repairable(unit worker, unit target) =>
        !target.Dead && !target.Removed && target != worker && !target.Constructing && IsBuildingNow(target) && target.Life < target.MaxLife
        && (target.Owner == worker.Owner || IsPlayerAlly(worker.Owner, target.Owner));

    // One step of a repairing worker: into reach, then the building gets its share of life for its share of the price
    bool Repairing(unit worker)
    {
        if (worker.OrderTarget is not unit target || !Repairable(worker, target)) return Finish(worker);
        var reach = worker.Type.Real(Collision) + target.Type.Real(Collision) + 100;
        if (Distance2(target.X - worker.X, target.Y - worker.Y) > reach * reach) return Walk(worker, target.X, target.Y, follow: true);
        var seconds = Math.Max(target.Type.Real(BuildTime), 1) * Data.Constant("RepairTimeRatio", 1.5f);
        var heal = Math.Min(target.MaxLife / seconds * (float)TickSeconds, target.MaxLife - target.Life);
        var share = heal / target.MaxLife * Data.Constant("RepairCostRatio", 0.35f);
        var (gold, lumber) = (target.Type.Integer(Cost) * share, target.Type.Integer(LumberCost) * share);
        // The price is taken in whole points, a point at a time as soon as one is owed (the game charged 45 gold and 17 lumber for 44.8 and 16.8)
        worker.RepairOwed = (worker.RepairOwed.Gold + gold, worker.RepairOwed.Lumber + lumber);
        var (wholeGold, wholeLumber) = ((int)Math.Ceiling(worker.RepairOwed.Gold), (int)Math.Ceiling(worker.RepairOwed.Lumber));
        if (!CanPay(worker.Owner, wholeGold, wholeLumber)) return Finish(worker);
        if (wholeGold > 0 || wholeLumber > 0)
        {
            Pay(worker.Owner, wholeGold, wholeLumber);
            worker.RepairOwed = (worker.RepairOwed.Gold - wholeGold, worker.RepairOwed.Lumber - wholeLumber);
        }
        SetLife(target, Math.Min(target.Life + heal, target.MaxLife));
        return true;
    }
}

public partial class unit
{
    internal (float Gold, float Lumber) RepairOwed;
}
