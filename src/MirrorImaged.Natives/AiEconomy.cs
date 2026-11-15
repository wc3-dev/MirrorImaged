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

    /// <summary>The difficulty the computer plays at: 1, Normal, as the lobby seats it (GetAIDifficulty).</summary>
    [Native(Level.Close)]
    public int MeleeDifficulty() => 1;

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

    /// <summary>Makes the number of units of the type more: a building is built by a worker near the town's hall, a unit trained at the buildings that train it; true when all were started (or queued).</summary>
    [Native(Level.Close)]
    public bool SetProduce(int qty, int id, int town)
    {
        if (qty <= 0) return true;
        if (Data.Object(ObjectKind.Unit, id) is { } type && type.Integer(IsBuilding) != 0) return BuildFor(Ai, id, qty, town);
        var missing = qty;
        foreach (var building in Mine(Ai).Where(b => !b.Constructing && Listed(b, Trains, id)).OrderBy(b => b.Train.Count))
            while (missing > 0 && QueueOrder(building, id)) missing--;
        return missing == 0;
    }

    /// <summary>Researches the next level of the upgrade at a building that lists it and is free; true when it was queued.</summary>
    [Native(Level.Close)]
    public bool SetUpgrade(int id) =>
        Mine(Ai).Where(b => !b.Constructing && Listed(b, Researches, id)).OrderBy(b => b.Train.Count).Any(b => QueueOrder(b, id));

    // What the AI wants its workers to be doing this round: a slot for each worker it asked for, gold or wood at a town, filled in the order asked
    readonly List<(bool Gold, int Town)> _slots = [];

    /// <summary>Forgets what the AI asked of its workers, so the next round of requests says it afresh.</summary>
    [Native(Level.Close)]
    public void ClearHarvestAI() => _slots.Clear();

    // The tree nearest the hall that stands, within the reach of the town and a little beyond
    destructable? TreeNear(unit hall) =>
        _destructables.Where(d => !d.Dead && d.Life > 0.405f && (d.Type.Text(Code("btar")) ?? "").Contains("tree", StringComparison.OrdinalIgnoreCase)
                && Distance2(d.X - hall.X, d.Y - hall.Y) <= 1500 * 1500).OrderBy(d => Distance2(d.X - hall.X, d.Y - hall.Y)).FirstOrDefault();

    // The workers go to the slots in the order of their ids, each keeping what it does when that is the kind of work its slot is
    void Assign()
    {
        var workers = Mine(Ai).Where(u => !u.Constructing && u.Building is null && IsWorker(u) && u.Order is 0 or OrderHarvest or OrderReturn).OrderBy(u => u.Id).ToList();
        for (var i = 0; i < _slots.Count && i < workers.Count; i++)
        {
            var (gold, town) = _slots[i];
            if (!TownHasHall(town)) continue;
            var worker = workers[i];
            var current = worker.Order == OrderReturn ? worker.HarvestTarget : worker.OrderTarget;
            if (worker.Order != 0 && (gold ? current is unit : current is destructable) && !SpentTarget(this, current)) continue;
            widget? target = gold ? MineNear(Towns()[town]) : TreeNear(Towns()[town]);
            if (target is null) continue;
            if (worker.Order == OrderReturn && worker.CarryGold + worker.CarryLumber > 0) continue;
            Give(worker, OrderHarvest, GetWidgetX(target), GetWidgetY(target), target, 2);
        }
    }

    /// <summary>Asks for this many workers on the gold mine of the town, after those asked for so far this round.</summary>
    [Native(Level.Close)]
    public void HarvestGold(int town, int peons)
    {
        for (var i = 0; i < peons; i++) _slots.Add((true, town));
        Assign();
    }

    /// <summary>Asks for this many workers on the trees near the town's hall, after those asked for so far this round.</summary>
    [Native(Level.Close)]
    public void HarvestWood(int town, int peons)
    {
        for (var i = 0; i < peons; i++) _slots.Add((false, town));
        Assign();
    }

    bool IsWorker(unit u) => (BlzGetUnitIntegerField(u, Convert<unitintegerfield>(Classification)) & 16) != 0;

    // The AI builds what is missing: a worker that can build it, idle or mining, is sent to a site near the town's hall; true when one was sent
    bool BuildFor(player p, int id, int missing, int town)
    {
        var sent = false;
        var hall = Towns() is var halls && town >= 0 && town < halls.Count ? halls[town] : halls.FirstOrDefault();
        if (hall is null || Data.Object(ObjectKind.Unit, id) is not { } type) return false;
        for (; missing > 0; missing--)
        {
            var worker = Mine(p).Where(w => !w.Constructing && w.Building is null && w.Order is 0 or OrderHarvest or OrderReturn && Listed(w, Code("ubui"), id))
                .OrderBy(w => w.Order == 0 ? 0 : 1).ThenBy(w => Distance2(w.X - hall.X, w.Y - hall.Y)).FirstOrDefault();
                if (worker is null || !CanPay(p, type.Integer(Cost), type.Integer(LumberCost)) || Site(hall, type) is not var (x, y)) break;
            sent |= IssueBuildOrderById(worker, id, x, y);
        }
        return sent;
    }

    // A place near the hall for the building: rings of places round it, the first whose ground can be built on and which no building, worker
    // order or mine crowds
    (float, float)? Site(unit hall, GameObject type)
    {
        var half = type.Real(Collision);
        var buildings = _units.Values.Where(b => !b.Dead && !b.Removed && IsBuildingNow(b)).ToList();
        var orders = _units.Values.Where(w => w.Order != 0 && !OrderNames.ContainsKey(w.Order) && Data.Object(ObjectKind.Unit, w.Order) is not null).ToList();
        for (var radius = hall.Type.Real(Collision) + half + 60; radius <= 900; radius += 64)
            for (var step = 0; step < 16; step++)
            {
                var angle = (step * 2 * MathF.PI / 16) + radius * 0.01f;
                var (x, y) = (hall.X + radius * MathF.Cos(angle), hall.Y + radius * MathF.Sin(angle));
                if (Data.Terrain is { } terrain && terrain.Bounds is var (minX, minY, maxX, maxY) && (x < minX + 128 || x > maxX - 128 || y < minY + 128 || y > maxY - 128)) continue;
                if (Data.Map?.Files.PathingMap is not null && IsTerrainPathable(x, y, Convert<pathingtype>(3)) || Data.Map?.Files.PathingMap is not null && IsTerrainPathable(x, y, Convert<pathingtype>(1))) continue;
                if (buildings.Any(b => Distance2(b.X - x, b.Y - y) < MathF.Pow(b.Type.Real(Collision) + half + 24, 2) && !(b.Constructing && false))) continue;
                if (orders.Any(w => Distance2(w.OrderX - x, w.OrderY - y) < MathF.Pow(half * 2 + 24, 2))) continue;
                if (_units.Values.Any(m => !m.Dead && GetResourceAmount(m) > 0 && Distance2(m.X - x, m.Y - y) < MathF.Pow(m.Type.Real(Collision) + half + 150, 2))) continue;
                return (x, y);
            }
        return null;
    }
}
