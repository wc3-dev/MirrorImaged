using MirrorImaged.Data;

namespace MirrorImaged.Natives;

// The economy natives of the computer players' AI (common.ai): what the AI player has and can do about its units, upgrades and workers, on the
// buildings, training queue and harvesting of wave 3. The AI player is the one the AI script runs for (the lobby's computer, or the local player
// where no AI runs). Close: a town is a place, where a town hall or a mine of the AI player's own first stood away from its other towns; it
// stays when its hall dies, and a mine belongs to the town it stands within 1,000 of (decisions D357, D490).
public sealed partial class GameNatives
{
    /// <summary>The player the AI natives act for while an AI script runs (the local player where none does).</summary>
    internal player? AiFor { get; set; }
    const float TownReach = 1000;
    const float Lane = 96; // the room the AI leaves between its buildings, so that its workers and army walk through its base

    player Ai => AiFor ?? _local ?? Player(0)!;

    /// <summary>The difficulty the computers play at: 0 Easy, 1 Normal, 2 Insane; Normal unless the run says otherwise.</summary>
    public int Difficulty { get; init; } = 1;

    /// <summary>The difficulty the computer plays at, as GetAIDifficulty.</summary>
    /// <remarks>Close: the number comes from the run's setting, not measured in the game.</remarks>
    [Native(Level.Close)]
    public int MeleeDifficulty() => Difficulty;

    /// <summary>The player number of the player the AI natives act for.</summary>
    /// <remarks>Close: which player that is rests on how the run was set up, not measured in the game.</remarks>
    [Native(Level.Close)]
    public int GetAiPlayer() => GetPlayerId(Ai);

    IEnumerable<unit> Mine(player p) => _units.Values.Where(u => u.Owner == p && !u.Dead && !u.Removed);

    int CountOf(player p, int id, bool done) => Mine(p).Count(u => u.Type.Id == id && (!done || !u.Constructing))
        + (done ? 0 : Mine(p).Sum(b => b.Train.Count(t => !t.Research && t.Id == id)));

    /// <summary>The AI player's units of the type, those under construction and queued in training too.</summary>
    /// <remarks>Close: buildings under construction and units queued count, as the model's own count does; the game's is not measured.</remarks>
    [Native(Level.Close)] public int GetUnitCount(int unitid) => CountOf(Ai, unitid, false);
    /// <summary>The AI player's finished units of the type; those under construction and in training are left out.</summary>
    /// <remarks>Close: the game's own count is not measured.</remarks>
    [Native(Level.Close)] public int GetUnitCountDone(int unitid) => CountOf(Ai, unitid, true);
    /// <summary>The player's units of the type, those under construction and queued in training too; 0 for nil.</summary>
    /// <remarks>Close: buildings under construction and units queued count, as the model's own count does; the game's is not measured.</remarks>
    [Native(Level.Close)] public int GetPlayerUnitTypeCount(player? p, int unitid) => p is null ? 0 : CountOf(p, unitid, false);
    /// <summary>The gold left in the mines the AI player works; the script expands when it runs low.</summary>
    /// <remarks>Close: the game's own sum is not measured; this adds the gold left in the mine of each town the AI player works.</remarks>
    [Native(Level.Close)] public int GetGoldOwned() => Towns().Sum(town => WorkedMine(town) is { } mine ? GetResourceAmount(mine) : 0);
    /// <summary>The gold the unit type costs; 0 for an unknown type.</summary>
    /// <remarks>Close: read from the type's object data, not measured in the game.</remarks>
    [Native(Level.Close)] public int GetUnitGoldCost(int unitid) => Data.Object(ObjectKind.Unit, unitid)?.Integer(Cost) ?? 0;
    /// <summary>The lumber the unit type costs; 0 for an unknown type.</summary>
    /// <remarks>Close: read from the type's object data, not measured in the game.</remarks>
    [Native(Level.Close)] public int GetUnitWoodCost(int unitid) => Data.Object(ObjectKind.Unit, unitid)?.Integer(LumberCost) ?? 0;
    /// <summary>The build time of the unit type in seconds, cut to a whole number; 0 for an unknown type.</summary>
    /// <remarks>Close: read from the type's object data, not measured in the game.</remarks>
    [Native(Level.Close)] public int GetUnitBuildTime(int unitid) => (int)(Data.Object(ObjectKind.Unit, unitid)?.Real(BuildTime) ?? 0);
    /// <summary>The level the AI player has of the upgrade; 0 for none.</summary>
    /// <remarks>Close: read from the player's tech levels, not measured in the game.</remarks>
    [Native(Level.Close)] public int GetUpgradeLevel(int id) => Ai.TechLevels.GetValueOrDefault(id);

    // The cost of the upgrade's next level for the AI player
    int UpgradeCost(int id, int baseField, int modField) =>
        Data.Object(ObjectKind.Upgrade, id) is { } upgrade ? upgrade.Integer(baseField) + Ai.TechLevels.GetValueOrDefault(id) * upgrade.Integer(modField) : 0;

    /// <summary>The gold the upgrade's next level costs the AI player, its base plus the level had times the increase; 0 for an unknown upgrade.</summary>
    /// <remarks>Close: read from the upgrade's base and increase data, not measured in the game.</remarks>
    [Native(Level.Close)] public int GetUpgradeGoldCost(int id) => UpgradeCost(id, Code("gglb"), Code("gglm"));
    /// <summary>The lumber the upgrade's next level costs the AI player, its base plus the level had times the increase; 0 for an unknown upgrade.</summary>
    /// <remarks>Close: read from the upgrade's base and increase data, not measured in the game.</remarks>
    [Native(Level.Close)] public int GetUpgradeWoodCost(int id) => UpgradeCost(id, Code("glmb"), Code("glmm"));

    readonly Dictionary<player, List<(float X, float Y)>> _towns = [];

    bool Within((float X, float Y) town, unit u) => Distance2(u.X - town.X, u.Y - town.Y) <= TownReach * TownReach;

    // The towns of the AI player, a town its index: its town halls and its own mines (an undead's haunted one) each make a town where they
    // stand away from the towns it has, the halls first and the first made first
    List<(float X, float Y)> Towns()
    {
        if (!_towns.TryGetValue(Ai, out var towns)) _towns[Ai] = towns = [];
        foreach (var u in Mine(Ai).Where(u => IsTownHall(u) || GetResourceAmount(u) > 0).OrderBy(u => IsTownHall(u) ? 0 : 1).ThenBy(u => u.Id))
            if (!towns.Any(town => Within(town, u))) towns.Add((u.X, u.Y));
        return towns;
    }

    unit? HallAt((float X, float Y) town) => Mine(Ai).Where(u => !u.Constructing && IsTownHall(u) && Within(town, u)).OrderBy(u => u.Id).FirstOrDefault();

    // The nearest mine with gold the AI player may take: nobody's, or its own
    unit? MineNear((float X, float Y) town) =>
        _units.Values.Where(m => !m.Dead && !m.Removed && !m.Constructing && GetResourceAmount(m) > 0 && (m.Owner == Ai || GetPlayerId(m.Owner) >= 24) && Within(town, m))
            .OrderBy(m => Distance2(m.X - town.X, m.Y - town.Y)).FirstOrDefault();

    // The town's mine when the AI player has it: its own, or one beside its town hall
    unit? WorkedMine((float X, float Y) town) => MineNear(town) is { } mine && (mine.Owner == Ai || HallAt(town) is not null) ? mine : null;

    /// <summary>Whether the town has a finished town hall of the AI player; false for an index out of range.</summary>
    /// <remarks>Close: a town is the model's own idea (D357, D490); the game's is not measured.</remarks>
    [Native(Level.Close)] public bool TownHasHall(int townid) => townid >= 0 && Towns() is var towns && townid < towns.Count && HallAt(towns[townid]) is not null;
    /// <summary>Whether a mine with gold, nobody's or the AI player's, stands within 1000 of the town; false for an index out of range.</summary>
    /// <remarks>Close: a town is the model's own idea (D357, D490); the game's is not measured.</remarks>
    [Native(Level.Close)] public bool TownHasMine(int townid) => townid >= 0 && Towns() is var towns && townid < towns.Count && MineNear(towns[townid]) is not null;
    /// <summary>How many towns have a mine the AI player works, its own or one by its town hall.</summary>
    /// <remarks>Close: a town is the model's own idea (D357, D490); the game's is not measured.</remarks>
    [Native(Level.Close)] public int GetMinesOwned() => Towns().Count(town => WorkedMine(town) is not null);
    /// <summary>The index of the first town with a mine the AI player works; -1 for none.</summary>
    /// <remarks>Close: a town is the model's own idea (D357, D490); the game's is not measured.</remarks>
    [Native(Level.Close)] public int TownWithMine() => Towns().FindIndex(town => WorkedMine(town) is not null);

    /// <summary>The AI player's units of the type within 1000 of the town, finished ones only when the flag is set; 0 for an index out of range.</summary>
    /// <remarks>Close: a town is the model's own idea (D357, D490); the game's is not measured.</remarks>
    [Native(Level.Close)]
    public int GetTownUnitCount(int id, int tn, bool dn) =>
        Towns() is var towns && tn >= 0 && tn < towns.Count && towns[tn] is var town
            ? Mine(Ai).Count(u => u.Type.Id == id && (!dn || !u.Constructing) && Within(town, u)) : 0;

    /// <summary>A building of the player (the first made), nil for none.</summary>
    /// <remarks>Close: the first made stands for the game's own pick, which is not measured.</remarks>
    [Native(Level.Close)]
    public unit? GetBuilding(player? p) => p is null ? null : Mine(p).Where(u => IsBuildingNow(u)).OrderBy(u => u.Id).FirstOrDefault();

    /// <summary>Makes the number of units of the type more: a building is built by a worker near the town's hall, a unit trained at the buildings that train it; true when all were started (or queued).</summary>
    /// <remarks>Close: the building, the worker and the site are the model's picks, not measured in the game; a unit nothing trains counts as made (D433).</remarks>
    [Native(Level.Close)]
    public bool SetProduce(int qty, int id, int town)
    {
        if (qty <= 0) return true;
        // A building another one turns into (a Keep from a Town Hall) is made there, not built anew
        if (Mine(Ai).Where(b => Listed(b, UpgradesTo, id)).OrderBy(b => b.Id).ToList() is { Count: > 0 } lower) return lower.Any(b => QueueOrder(b, id));
        if (Data.Object(ObjectKind.Unit, id) is { } type && type.Integer(IsBuilding) != 0) return BuildFor(Ai, id, qty, town);
        // What nothing of the player's trains is not waited for: the script has set its price aside and goes on to what comes after (D433)
        if (!Mine(Ai).Any(b => Listed(b, Trains, id))) return true;
        var missing = qty;
        foreach (var building in Mine(Ai).Where(b => !b.Constructing && Listed(b, Trains, id)).OrderBy(b => b.Train.Count))
            while (missing > 0 && QueueOrder(building, id)) missing--;
        return missing == 0;
    }

    /// <summary>Researches the next level of the upgrade at a building that lists it and is free; true when it was queued.</summary>
    /// <remarks>Close: which building takes the research is the model's pick, not measured in the game.</remarks>
    [Native(Level.Close)]
    public bool SetUpgrade(int id) =>
        Mine(Ai).Where(b => !b.Constructing && Listed(b, Researches, id)).OrderBy(b => b.Train.Count).Any(b => QueueOrder(b, id));

    // What the AI wants its workers to be doing this round: a slot for each worker it asked for, gold or wood at a town, filled in the order asked
    readonly List<(bool Gold, int Town)> _slots = [];

    /// <summary>Forgets what the AI asked of its workers, so the next round of requests says it afresh.</summary>
    /// <remarks>Close: the game's own harvest state is not measured; here it is a list of slots.</remarks>
    [Native(Level.Close)]
    public void ClearHarvestAI() => _slots.Clear();

    readonly Dictionary<player, HashSet<destructable>> _unreachable = [];

    // The tree nearest the town that stands, however far: when the trees round the town are cut the workers go on to the next (D730)
    // except one a worker of the player stopped short of, which it can't reach (D731)
    destructable? TreeNear((float X, float Y) town) =>
        _destructables.Where(d => !d.Dead && d.Life > 0.405f && IsTree(d.Type) && !(_unreachable.TryGetValue(Ai, out var lost) && lost.Contains(d)))
            .MinBy(d => Distance2(d.X - town.X, d.Y - town.Y));

    // Whether a destructable of the type is a tree, kept by type: a map has thousands of them and the AI looks every round
    readonly Dictionary<GameObject, bool> _trees = [];

    bool IsTree(GameObject type)
    {
        if (!_trees.TryGetValue(type, out var tree)) _trees[type] = tree = (type.Text(Code("btar")) ?? "").Contains("tree", StringComparison.OrdinalIgnoreCase);
        return tree;
    }

    // A unit that gathers lumber without being a worker that builds, as the undead ghoul does (the lumber harvest ability)
    bool GathersLumberOnly(unit u) => u.Abilities.Any(a => a.Type.Id == LumberHarvest) && !IsWorker(u);

    static readonly int LumberHarvest = Code("Ahrl");

    // The workers go to the slots in the order asked, each slot taking the first idle or gathering worker by id that can do its work (a ghoul
    // only cuts wood, so it takes the wood slots first), each keeping what it does when that is the kind of work its slot is
    void Assign()
    {
        var pool = Mine(Ai).Where(u => !u.Constructing && u.Building is null && (IsWorker(u) || GathersLumberOnly(u)) && u.Order is 0 or OrderHarvest or OrderReturn).OrderBy(u => u.Id).ToList();
        var used = new HashSet<unit>();
        var towns = Towns();
        // A worker idle at a tree that stands, farther than it chops from, was sent where it can't walk: the player's workers don't go there again
        foreach (var w in pool.Where(w => w.Order == 0 && w.OrderTarget is destructable { Dead: false } tree && Distance2(tree.X - w.X, tree.Y - w.Y) > 150 * 150))
        {
            if (!_unreachable.TryGetValue(Ai, out var lost)) _unreachable[Ai] = lost = [];
            lost.Add((destructable)w.OrderTarget!);
        }
        foreach (var (gold, town) in _slots)
        {
            var worker = pool.Where(w => !used.Contains(w) && (!gold || IsWorker(w))).OrderBy(w => !gold && GathersLumberOnly(w) ? 0 : 1).FirstOrDefault();
            if (worker is null) continue;
            used.Add(worker);
            if (town < 0 || town >= towns.Count) continue;
            var current = worker.Order == OrderReturn ? worker.HarvestTarget : worker.OrderTarget;
            if (worker.Order != 0 && (gold ? current is unit : current is destructable) && !SpentTarget(this, current)) continue;
            widget? target = gold ? MineNear(towns[town]) : TreeNear(towns[town]);
            if (target is null) continue;
            if (worker.Order == OrderReturn && worker.CarryGold + worker.CarryLumber > 0) continue;
            Give(worker, OrderHarvest, GetWidgetX(target), GetWidgetY(target), target, 2);
        }
    }

    /// <summary>Asks for this many workers on the gold mine of the town, after those asked for so far this round.</summary>
    /// <remarks>Close: how workers are picked and sent to the mine is the model's, not measured in the game.</remarks>
    [Native(Level.Close)]
    public void HarvestGold(int town, int peons)
    {
        for (var i = 0; i < peons; i++) _slots.Add((true, town));
        Assign();
    }

    /// <summary>Asks for this many workers on the trees near the town's hall, after those asked for so far this round.</summary>
    /// <remarks>Close: how workers are picked and sent to the trees is the model's, not measured in the game (D730, D731).</remarks>
    [Native(Level.Close)]
    public void HarvestWood(int town, int peons)
    {
        for (var i = 0; i < peons; i++) _slots.Add((false, town));
        Assign();
    }

    bool IsWorker(unit u) => (BlzGetUnitIntegerField(u, Convert<unitintegerfield>(Classification)) & 16) != 0;

    // The AI builds what is missing: a worker that can build it, idle or mining, is sent to a site in the town; true when one was sent
    bool BuildFor(player p, int id, int missing, int town)
    {
        var sent = false;
        if (Towns() is not { Count: > 0 } towns || Data.Object(ObjectKind.Unit, id) is not { } type) return false;
        var hall = town >= 0 && town < towns.Count ? towns[town] : towns[0];
        for (; missing > 0; missing--)
        {
            var worker = Mine(p).Where(w => !w.Constructing && w.Building is null && w.Order is 0 or OrderHarvest or OrderReturn && Listed(w, Code("ubui"), id))
                .OrderBy(w => w.Order == 0 ? 0 : 1).ThenBy(w => Distance2(w.X - hall.X, w.Y - hall.Y)).FirstOrDefault();
            if (worker is null || !CanPay(p, type.Integer(Cost), type.Integer(LumberCost)) || Site(hall, type) is not var (x, y)) break;
            sent |= IssueBuildOrderById(worker, id, x, y);
        }
        return sent;
    }

    // A place in the town for the building: rings of places round it, the first whose ground can be built on and which no building, worker
    // order or mine crowds
    (float, float)? Site((float X, float Y) hall, GameObject type)
    {
        var half = type.Real(Collision);
        var buildings = _units.Values.Where(b => !b.Dead && !b.Removed && IsBuildingNow(b)).ToList();
        var orders = _units.Values.Where(w => w.Order != 0 && !OrderNames.ContainsKey(w.Order) && Data.Object(ObjectKind.Unit, w.Order) is not null).ToList();
        for (var radius = (HallAt(hall)?.Type.Real(Collision) ?? 0) + half + 60; radius <= 900; radius += 64)
            for (var step = 0; step < 16; step++)
            {
                var angle = (step * 2 * MathF.PI / 16) + radius * 0.01f;
                var (x, y) = (hall.X + radius * MathF.Cos(angle), hall.Y + radius * MathF.Sin(angle));
                if (Data.Terrain is { } terrain && terrain.Bounds is var (minX, minY, maxX, maxY) && (x < minX + 128 || x > maxX - 128 || y < minY + 128 || y > maxY - 128)) continue;
                if (Data.Map?.Files.PathingMap is not null && IsTerrainPathable(x, y, Convert<pathingtype>(3)) || Data.Map?.Files.PathingMap is not null && IsTerrainPathable(x, y, Convert<pathingtype>(1))) continue;
                if (buildings.Any(b => Distance2(b.X - x, b.Y - y) < MathF.Pow(b.Type.Real(Collision) + half + Lane, 2))) continue;
                if (orders.Any(w => Distance2(w.OrderX - x, w.OrderY - y) < MathF.Pow(half * 2 + Lane, 2))) continue;
                if (_units.Values.Any(m => !m.Dead && GetResourceAmount(m) > 0 && Distance2(m.X - x, m.Y - y) < MathF.Pow(m.Type.Real(Collision) + half + 150, 2))) continue;
                return (x, y);
            }
        return null;
    }
}
