using MirrorImaged.Data;

namespace MirrorImaged.Natives;

// The economy natives of the computer players' AI (common.ai): what the AI player has and can do about its units, upgrades and workers, on the
// buildings, training queue and harvesting of wave 3. The AI player is the one the AI script runs for (the lobby's computer, or the local player
// where no AI runs). Close: towns are the AI player's town halls in the order they were made, a mine belongs to the town it stands within 1,000
// of, and wood isn't harvested yet (docs/decisions.md, D357).
public sealed partial class GameNatives
{
    /// <summary>The player the AI natives act for while an AI script runs (the local player where none does).</summary>
    internal player? AiFor { get; set; }
    const float TownReach = 1000;

    player Ai => AiFor ?? _local ?? Player(0)!;

    [Native(Level.Close)]
    public int GetAiPlayer() => GetPlayerId(Ai);

    IEnumerable<unit> Mine(player p) => _units.Values.Where(u => u.Owner == p && !u.Dead && !u.Removed);

    int CountOf(player p, int id, bool done) => Mine(p).Count(u => u.Type.Id == id && (!done || !u.Constructing))
        + (done ? 0 : Mine(p).Sum(b => b.Train.Count(t => !t.Research && t.Id == id)));

    [Native(Level.Close)] public int GetUnitCount(int unitid) => CountOf(Ai, unitid, false);
    [Native(Level.Close)] public int GetUnitCountDone(int unitid) => CountOf(Ai, unitid, true);
    [Native(Level.Close)] public int GetPlayerUnitTypeCount(player? p, int unitid) => p is null ? 0 : CountOf(p, unitid, false);
    [Native(Level.Close)] public int GetGoldOwned() => Ai.States[Gold];
    [Native(Level.Close)] public int GetUnitGoldCost(int unitid) => Data.Object(ObjectKind.Unit, unitid)?.Integer(Cost) ?? 0;
    [Native(Level.Close)] public int GetUnitWoodCost(int unitid) => Data.Object(ObjectKind.Unit, unitid)?.Integer(LumberCost) ?? 0;
    [Native(Level.Close)] public int GetUnitBuildTime(int unitid) => (int)(Data.Object(ObjectKind.Unit, unitid)?.Real(BuildTime) ?? 0);
    [Native(Level.Close)] public int GetUpgradeLevel(int id) => Ai.TechLevels.GetValueOrDefault(id);

    // The cost of the upgrade's next level for the AI player
    int UpgradeCost(int id, int baseField, int modField) =>
        Data.Object(ObjectKind.Upgrade, id) is { } upgrade ? upgrade.Integer(baseField) + Ai.TechLevels.GetValueOrDefault(id) * upgrade.Integer(modField) : 0;

    [Native(Level.Close)] public int GetUpgradeGoldCost(int id) => UpgradeCost(id, Code("gglb"), Code("gglm"));
    [Native(Level.Close)] public int GetUpgradeWoodCost(int id) => UpgradeCost(id, Code("glmb"), Code("glmm"));

    // The town halls of the AI player, the first made first; a town is its index
    List<unit> Towns() => [.. Mine(Ai).Where(u => !u.Constructing && (BlzGetUnitIntegerField(u, Convert<unitintegerfield>(Classification)) & TownHallKind) != 0).OrderBy(u => u.Id)];

    unit? MineNear(unit hall) =>
        _units.Values.Where(m => !m.Dead && GetResourceAmount(m) > 0 && Distance2(m.X - hall.X, m.Y - hall.Y) <= TownReach * TownReach).OrderBy(m => Distance2(m.X - hall.X, m.Y - hall.Y)).FirstOrDefault();

    [Native(Level.Close)] public bool TownHasHall(int townid) => townid >= 0 && townid < Towns().Count;
    [Native(Level.Close)] public bool TownHasMine(int townid) => TownHasHall(townid) && MineNear(Towns()[townid]) is not null;
    [Native(Level.Close)] public int GetMinesOwned() => Towns().Count(hall => MineNear(hall) is not null);
    [Native(Level.Close)] public int TownWithMine() => Towns().FindIndex(hall => MineNear(hall) is not null);

    [Native(Level.Close)]
    public int GetTownUnitCount(int id, int tn, bool dn) =>
        TownHasHall(tn) ? Mine(Ai).Count(u => u.Type.Id == id && (!dn || !u.Constructing) && Distance2(u.X - Towns()[tn].X, u.Y - Towns()[tn].Y) <= TownReach * TownReach) : 0;

    /// <summary>A building of the player (the first made), nil for none.</summary>
    [Native(Level.Close)]
    public unit? GetBuilding(player? p) => p is null ? null : Mine(p).Where(u => IsBuildingNow(u)).OrderBy(u => u.Id).FirstOrDefault();

    /// <summary>Has the AI player at least this many units of the type, training what is missing at buildings that train it; true when some were queued or none were missing.</summary>
    [Native(Level.Close)]
    public bool SetProduce(int qty, int id, int town)
    {
        var missing = qty - CountOf(Ai, id, false);
        if (missing <= 0) return true;
        var queued = false;
        foreach (var building in Mine(Ai).Where(b => !b.Constructing && Listed(b, Trains, id)).OrderBy(b => b.Train.Count))
            while (missing > 0 && QueueOrder(building, id)) (missing, queued) = (missing - 1, true);
        return queued;
    }

    /// <summary>Researches the next level of the upgrade at a building that lists it and is free; true when it was queued.</summary>
    [Native(Level.Close)]
    public bool SetUpgrade(int id) =>
        Mine(Ai).Where(b => !b.Constructing && Listed(b, Researches, id)).OrderBy(b => b.Train.Count).Any(b => QueueOrder(b, id));

    /// <summary>Sends up to the number of idle workers of the AI player to the gold mine nearest the town's hall.</summary>
    [Native(Level.Close)]
    public void HarvestGold(int town, int peons)
    {
        if (!TownHasHall(town) || MineNear(Towns()[town]) is not { } mine) return;
        foreach (var worker in Mine(Ai).Where(u => u.Order == 0 && !u.Constructing && Listed(u, Code("ubui"), Code("hbar")) | IsWorker(u)).Take(Math.Max(peons, 0)).ToArray())
            Give(worker, OrderHarvest, mine.X, mine.Y, mine, 2);
    }

    // Wood needs the trees' list and isn't harvested here yet
    [Native(Level.Close)]
    public void HarvestWood(int town, int peons) { }

    bool IsWorker(unit u) => (BlzGetUnitIntegerField(u, Convert<unitintegerfield>(Classification)) & 16) != 0;
}
