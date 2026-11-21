using MirrorImaged.Data;

namespace MirrorImaged.Natives;

// The captains of the computer players' AI (common.ai): the attack captain is a group of the units of the wave the AI asked for, which the
// natives send to a place to fight, bring home and ask about; the defence captain is not kept apart. Close: units of a wave type join in the
// order of their ids, and the rules of the wave (reinforcing, retreating on losses) are the script's, as the AI calls them (decisions D366).
public sealed partial class GameNatives
{
    sealed class War
    {
        public readonly List<(int Qty, int Id)> Wave = [];
        public readonly HashSet<unit> Members = [];
        public (float X, float Y) Home, DefenceHome;
        public (float X, float Y)? Goal;
        public bool Retreating;
    }

    readonly Dictionary<player, War> _wars = [];
    const float AtGoalReach = 600, HomeReach = 700;
    const int OrderMoveId = 851986, OrderAttackId = 851983;

    War WarOf(player p) => _wars.TryGetValue(p, out var war) ? war : _wars[p] = new War { Home = Towns() is [var town, ..] ? town : (0, 0) };

    // The units of the wave that stand in the captain: those already in it, then the others of the types asked for, up to the numbers asked for
    List<unit> Captain()
    {
        var war = WarOf(Ai);
        war.Members.RemoveWhere(u => u.Dead || u.Removed);
        var picked = new List<unit>();
        foreach (var (qty, id) in war.Wave)
        {
            var of = Mine(Ai).Where(u => u.Type.Id == id && !u.Constructing).OrderBy(u => war.Members.Contains(u) ? 0 : 1).ThenBy(u => u.Id).Take(qty);
            foreach (var u in of) if (!picked.Contains(u)) picked.Add(u);
        }
        war.Members.Clear();
        foreach (var u in picked) war.Members.Add(u);
        if (war.Goal is { } goal && !war.Retreating)
            foreach (var u in picked.Where(u => u.Order == 0)) Give(u, OrderAttackId, goal.X, goal.Y, null, 1);
        return picked;
    }

    // The units of the wave still to be made are trained at the buildings that train them
    void MakeWave()
    {
        foreach (var (qty, id) in WarOf(Ai).Wave)
        {
            var missing = qty - CountOf(Ai, id, false);
            if (missing > 0) SetProduce(missing, id, -1);
        }
    }

    /// <summary>The wave asks for the number of units of the type more; true when it can be made.</summary>
    /// <remarks>Close: the wave's units are asked for at once through SetProduce; the game's own wave rules are not measured (D366).</remarks>
    [Native(Level.Close)]
    public bool AddAssault(int qty, int id)
    {
        if (qty <= 0 || Data.Object(ObjectKind.Unit, id) is null) return false;
        WarOf(Ai).Wave.Add((qty, id));
        MakeWave();
        return true;
    }

    /// <summary>Empties the wave.</summary>
    /// <remarks>Close: the game's own wave state is not measured (D366).</remarks>
    [Native(Level.Close)]
    public void InitAssault() => WarOf(Ai).Wave.Clear();

    /// <summary>Makes the AI player's war state if it has none.</summary>
    /// <remarks>Close: only the attack captain is modelled; the defence captain is not kept apart (D366).</remarks>
    [Native(Level.Close)]
    public void CreateCaptains() => WarOf(Ai);

    /// <summary>Sets where the attack captain (0) or the defence captain (any other number) comes home; nothing reads the defence home back.</summary>
    /// <remarks>Close: which number is which captain is not measured in the game; here 0 is the attack captain.</remarks>
    [Native(Level.Close)]
    public void SetCaptainHome(int which, float x, float y)
    {
        var war = WarOf(Ai);
        if (which == 0) war.Home = (x, y); else war.DefenceHome = (x, y);
    }

    /// <summary>Forgets the attack goal.</summary>
    /// <remarks>Close: the model keeps one goal; what else the game resets is not measured.</remarks>
    [Native(Level.Close)] public void ResetCaptainLocs() => WarOf(Ai).Goal = null;
    /// <summary>Changes nothing; the captain keeps its goal.</summary>
    /// <remarks>Recorded: Whether the engine lets the attack captain change its goal while it fights; the simulation's captain keeps the goal the script gave it (D798).</remarks>
    [Native(Level.Recorded, Reason = "Whether the engine lets the attack captain change its goal while it fights; the simulation's captain keeps the goal the script gave it (D798)")]
    public void SetCaptainChanges(bool allow) { }
    /// <summary>Forgets the attack goal.</summary>
    /// <remarks>Close: the model keeps one goal; what else the game clears is not measured.</remarks>
    [Native(Level.Close)] public void ClearCaptainTargets() => WarOf(Ai).Goal = null;
    /// <summary>Changes nothing; the captain is not pointed at a player.</summary>
    /// <remarks>Recorded: Points the attack captain at a player, so it picks that player's units and buildings as targets; the simulation's captain goes to the place the script names (D798).</remarks>
    [Native(Level.Recorded, Reason = "Points the attack captain at a player, so it picks that player's units and buildings as targets; the simulation's captain goes to the place the script names (D798)")]
    public void CaptainVsPlayer(player? id) { }

    /// <summary>Changes nothing; the captain is not pointed at a player's units.</summary>
    /// <remarks>Recorded: Points the attack captain at a player's units, not its buildings; the simulation's captain goes to the place the script names (D798).</remarks>
    [Native(Level.Recorded, Reason = "Points the attack captain at a player's units, not its buildings; the simulation's captain goes to the place the script names (D798)")]
    public void CaptainVsUnits(player? id) { }

    /// <summary>The captain fights its way to the place.</summary>
    /// <remarks>Close: the units get an attack order to the place; the wave's reinforcing and retreating are the script's, not the game's (D366).</remarks>
    [Native(Level.Close)]
    public void CaptainAttack(float x, float y)
    {
        var war = WarOf(Ai);
        (war.Goal, war.Retreating) = ((x, y), false);
        foreach (var u in Captain()) Give(u, OrderAttackId, x, y, null, 1);
    }

    /// <summary>As <c>CaptainAttack</c>, to a place given in whole numbers.</summary>
    /// <remarks>Close: as CaptainAttack; the game's own rules for it are not measured.</remarks>
    [Native(Level.Close)] public void AttackMoveXY(int x, int y) => CaptainAttack(x, y);

    /// <summary>As <c>CaptainAttack</c>, to where the target stands now; nothing for nil.</summary>
    /// <remarks>Close: the captain goes to the place the target stands at now, not after it as it moves; the game's own rules are not measured.</remarks>
    [Native(Level.Close)]
    public void AttackMoveKill(unit? target)
    {
        if (target is not null) CaptainAttack(target.X, target.Y);
    }

    /// <summary>The captain comes home and stays.</summary>
    /// <remarks>Close: the units get a move order to the home; the game's own retreat is not measured (D366).</remarks>
    [Native(Level.Close)]
    public void CaptainGoHome()
    {
        var war = WarOf(Ai);
        (war.Goal, war.Retreating) = (null, true);
        foreach (var u in Captain()) Give(u, OrderMoveId, war.Home.X, war.Home.Y, null, 1);
    }

    /// <summary>Puts every unit of the captain at the place, as <c>SetUnitPosition</c> does.</summary>
    /// <remarks>Close: where the game puts the units is not measured.</remarks>
    [Native(Level.Close)]
    public void TeleportCaptain(float x, float y)
    {
        foreach (var u in Captain()) SetUnitPosition(u, x, y);
    }

    /// <summary>How many units the captain has.</summary>
    /// <remarks>Close: the captain is the wave's units picked by type and id, the model's own pick; the game's is not measured (D366).</remarks>
    [Native(Level.Close)] public int CaptainGroupSize() => Captain().Count;
    /// <summary>Whether the captain has no units.</summary>
    /// <remarks>Close: the captain is the wave's units picked by type and id, the model's own pick; the game's is not measured (D366).</remarks>
    [Native(Level.Close)] public bool CaptainIsEmpty() => Captain().Count == 0;

    /// <summary>Whether the captain has the number of every type the wave asked for; false for an empty wave.</summary>
    /// <remarks>Close: the game's own test is not measured (D366).</remarks>
    [Native(Level.Close)]
    public bool CaptainIsFull()
    {
        var wave = WarOf(Ai).Wave;
        return wave.Count > 0 && Captain() is var units && wave.All(w => units.Count(u => u.Type.Id == w.Id) >= w.Qty);
    }

    /// <summary>Whether the captain has units and all are within 600 of the attack goal; false with no goal.</summary>
    /// <remarks>Close: the reach of 600 is the model's, not measured in the game.</remarks>
    [Native(Level.Close)]
    public bool CaptainAtGoal() =>
        WarOf(Ai).Goal is { } goal && Captain() is { Count: > 0 } units && units.All(u => Distance2(u.X - goal.X, u.Y - goal.Y) <= AtGoalReach * AtGoalReach);

    /// <summary>Whether every unit of the captain is within 700 of its home; true for an empty captain.</summary>
    /// <remarks>Close: the reach of 700 is the model's, not measured in the game.</remarks>
    [Native(Level.Close)]
    public bool CaptainIsHome() =>
        Captain() is var units && WarOf(Ai).Home is var home && units.All(u => Distance2(u.X - home.X, u.Y - home.Y) <= HomeReach * HomeReach);

    /// <summary>Whether the captain was sent home and has not got there yet.</summary>
    /// <remarks>Close: the game's own test is not measured.</remarks>
    [Native(Level.Close)] public bool CaptainRetreating() => WarOf(Ai).Retreating && !CaptainIsHome();
    /// <summary>Whether the attack captain (true given) has a unit with an attack target; false for the other.</summary>
    /// <remarks>Close: a unit with an attack target counts as fighting; the game's own test is not measured.</remarks>
    [Native(Level.Close)] public bool CaptainInCombat(bool attack_captain) => attack_captain && Captain().Any(u => u.AttackTarget is not null);

    // How much of the wave stands ready, in percent
    /// <summary>The share of the wave's units the captain has, in percent, cut to a whole number; 0 for an empty wave.</summary>
    /// <remarks>Close: the game's own formula is not measured.</remarks>
    [Native(Level.Close)]
    public int CaptainReadiness()
    {
        var wave = WarOf(Ai).Wave;
        var total = wave.Sum(w => w.Qty);
        if (total == 0) return 0;
        var units = Captain();
        return 100 * wave.Sum(w => Math.Min(w.Qty, units.Count(u => u.Type.Id == w.Id))) / total;
    }

    /// <summary>The average life of the captain's units in percent, cut to a whole number; 0 with no units.</summary>
    /// <remarks>Close: the game's own formula is not measured.</remarks>
    [Native(Level.Close)] public int CaptainReadinessHP() => Captain() is { Count: > 0 } units ? (int)(100 * units.Average(u => u.Life / Math.Max(u.MaxLife, 1))) : 0;
    /// <summary>The average mana of the captain's units in percent, a unit with no mana counting full; 0 with no units.</summary>
    /// <remarks>Close: the game's own formula is not measured.</remarks>
    [Native(Level.Close)] public int CaptainReadinessMa() => Captain() is { Count: > 0 } units ? (int)(100 * units.Average(u => u.MaxMana > 0 ? u.Mana / u.MaxMana : 1)) : 0;

    /// <summary>The nearest building of an enemy player to the AI player's first town hall, a town hall first.</summary>
    /// <remarks>Close: the order (town halls first, then nearest) is the model's pick, not measured in the game.</remarks>
    [Native(Level.Close)]
    public unit? GetEnemyBase()
    {
        var from = Towns() is [var town, ..] ? town : WarOf(Ai).Home;
        return _units.Values.Where(b => !b.Dead && !b.Removed && IsBuildingNow(b) && b.Owner != Ai && GetPlayerId(b.Owner) < 24 && IsPlayerEnemy(Ai, b.Owner))
            .OrderBy(b => (BlzGetUnitIntegerField(b, Convert<unitintegerfield>(Classification)) & TownHallKind) != 0 ? 0 : 1)
            .ThenBy(b => Distance2(b.X - from.Item1, b.Y - from.Item2)).FirstOrDefault();
    }

    /// <summary>Does nothing; <c>GetEnemyBase</c> answers at once.</summary>
    /// <remarks>Close: The game searches for the base over time; here GetEnemyBase answers at once, so the search is always done (D431).</remarks>
    [Native(Level.Close, Reason = "The game searches for the base over time; here GetEnemyBase answers at once, so the search is always done (D431)")]
    public void StartGetEnemyBase() { }

    /// <summary>Whether the search is still going; always false.</summary>
    /// <remarks>Close: False: the search StartGetEnemyBase starts is already done (D431).</remarks>
    [Native(Level.Close, Reason = "False: the search StartGetEnemyBase starts is already done (D431)")]
    public bool WaitGetEnemyBase() => false;

    /// <summary>How strong the enemies are: their fighting units that stand.</summary>
    /// <remarks>Close: it counts armed units, not their strength; the game's own measure is not measured.</remarks>
    [Native(Level.Close)]
    public int GetEnemyPower() =>
        _units.Values.Count(u => !u.Dead && !u.Removed && !IsBuildingNow(u) && u.Owner != Ai && GetPlayerId(u.Owner) < 24 && IsPlayerEnemy(Ai, u.Owner) && u.Type.Integer(WeaponsOn) != 0);

    // The gold mine nearest the AI player's first town that is nobody's and that no town hall stands within reach of, one its land reaches
    // first (D789), or none
    unit? FreeMine()
    {
        var home = Towns() is [var town, ..] ? town : WarOf(Ai).Home;
        var halls = _units.Values.Where(h => !h.Dead && !h.Removed && IsTownHall(h)).ToList();
        var land = LandOf(home.Item1, home.Item2);
        return _units.Values.Where(m => !m.Dead && !m.Removed && GetResourceAmount(m) > 0 && GetPlayerId(m.Owner) >= 24 && !halls.Any(h => Distance2(h.X - m.X, h.Y - m.Y) <= TownReach * TownReach))
            .OrderBy(m => land is null || OnLand(land, m.X, m.Y) ? 0 : 1).ThenBy(m => Distance2(m.X - home.Item1, m.Y - home.Item2)).FirstOrDefault();
    }

    // The walkable cells of the pathing grid joined to a place, by eight neighbours as a walker's search goes; found once a place, as the
    // ground seldom changes. Null on a map with no ground
    readonly Dictionary<(float, float), (float Left, float Bottom, int Columns, bool[] Reached)?> _lands = [];

    (float Left, float Bottom, int Columns, bool[] Reached)? LandOf(float x, float y)
    {
        if (_lands.TryGetValue((x, y), out var known)) return known;
        if (Data.Terrain is not { } terrain) return _lands[(x, y)] = null;
        var (minX, minY, maxX, maxY) = terrain.Bounds;
        int columns = (int)((maxX - minX) / SearchCell) + 1, rows = (int)((maxY - minY) / SearchCell) + 1;
        var walk = Convert<pathingtype>(1);
        bool Open(int c, int r) => c >= 0 && r >= 0 && c < columns && r < rows && !IsTerrainPathable(minX + (c + 0.5f) * SearchCell, minY + (r + 0.5f) * SearchCell, walk);
        var reached = new bool[columns * rows];
        var start = (Math.Clamp((int)((x - minX) / SearchCell), 0, columns - 1), Math.Clamp((int)((y - minY) / SearchCell), 0, rows - 1));
        var queue = new Queue<(int C, int R)>();
        queue.Enqueue(start);
        reached[start.Item2 * columns + start.Item1] = true;
        while (queue.TryDequeue(out var here))
            for (var dr = -1; dr <= 1; dr++)
                for (var dc = -1; dc <= 1; dc++)
                {
                    var (c, r) = (here.C + dc, here.R + dr);
                    if (c < 0 || r < 0 || c >= columns || r >= rows || reached[r * columns + c] || !Open(c, r)) continue;
                    if (dc != 0 && dr != 0 && (!Open(here.C + dc, here.R) || !Open(here.C, here.R + dr))) continue;
                    reached[r * columns + c] = true;
                    queue.Enqueue((c, r));
                }
        return _lands[(x, y)] = (minX, minY, columns, reached);
    }

    // Whether the land reaches within 192 of the place (a gold mine's own cells are not walkable)
    static bool OnLand((float Left, float Bottom, int Columns, bool[] Reached)? land, float x, float y)
    {
        var (left, bottom, columns, reached) = land!.Value;
        int c0 = (int)((x - left) / SearchCell), r0 = (int)((y - bottom) / SearchCell);
        for (var dc = -6; dc <= 6; dc++)
            for (var dr = -6; dr <= 6; dr++)
                if (c0 + dc >= 0 && r0 + dr >= 0 && c0 + dc < columns && (r0 + dr) * columns + c0 + dc < reached.Length && reached[(r0 + dr) * columns + c0 + dc]) return true;
        return false;
    }

    // The creeps stand near the place: living units of the neutral aggressive player, a building not counted
    List<unit> CreepsNear(float x, float y, float reach) =>
        [.. _units.Values.Where(c => !c.Dead && !c.Removed && GetPlayerId(c.Owner) == GetPlayerNeutralAggressive() && !IsBuildingNow(c) && Distance2(c.X - x, c.Y - y) <= reach * reach)];

    /// <summary>0 when a gold mine without a town hall remains to expand to, else -1.</summary>
    /// <remarks>Close: the game's own ranking of places to expand is not measured; only whether a free mine is left is told.</remarks>
    [Native(Level.Close)] public int GetNextExpansion() => FreeMine() is null ? -1 : 0;
    /// <summary>The x of the mine to expand to, cut to a whole number; -1 for none.</summary>
    /// <remarks>Close: the mine is the model's pick (D789), not measured in the game.</remarks>
    [Native(Level.Close)] public int GetExpansionX() => FreeMine() is { } mine ? (int)mine.X : -1;
    /// <summary>The y of the mine to expand to, cut to a whole number; -1 for none.</summary>
    /// <remarks>Close: the mine is the model's pick (D789), not measured in the game.</remarks>
    [Native(Level.Close)] public int GetExpansionY() => FreeMine() is { } mine ? (int)mine.Y : -1;

    /// <summary>A creep guarding the mine to expand to, or nil.</summary>
    /// <remarks>Close: the reach of 700 round the mine is the model's, not measured in the game.</remarks>
    [Native(Level.Close)]
    public unit? GetExpansionFoe() =>
        FreeMine() is { } mine ? CreepsNear(mine.X, mine.Y, 700).OrderBy(c => Distance2(c.X - mine.X, c.Y - mine.Y)).FirstOrDefault() : null;

    /// <summary>The worker nearest the mine to expand to that is idle or gathering, or nil.</summary>
    /// <remarks>Close: the nearest idle or gathering worker is the model's pick, not measured in the game.</remarks>
    [Native(Level.Close)]
    public unit? GetExpansionPeon() =>
        FreeMine() is { } mine ? Mine(Ai).Where(w => !w.Constructing && w.Building is null && IsWorker(w) && w.Order is 0 or OrderHarvest or OrderReturn)
            .OrderBy(w => Distance2(w.X - mine.X, w.Y - mine.Y)).FirstOrDefault() : null;

    /// <summary>The worker goes to build the town hall type beside the mine to expand to; true when it was sent.</summary>
    /// <remarks>Close: the site beside the mine is the model's pick, not measured in the game.</remarks>
    [Native(Level.Close)]
    public bool SetExpansion(unit? peon, int id)
    {
        if (peon is null || FreeMine() is not { } mine || Data.Object(ObjectKind.Unit, id) is not { } type) return false;
        if (Mine(Ai).Any(w => w.Order == id)) return true; // a worker is on its way to build it
        var from = Towns() is [var town, ..] ? town : WarOf(Ai).Home;
        var (dx, dy) = (from.Item1 - mine.X, from.Item2 - mine.Y);
        var length = MathF.Max(MathF.Sqrt(dx * dx + dy * dy), 1);
        var away = mine.Type.Real(Collision) + type.Real(Collision) + 60;
        // On out from the mine until the worker can stand there (D821): Korea's haunted mine site touched the mine's footprint, and no way ends on it
        for (var more = 0; more < 128 && Blocked(peon, mine.X + dx / length * away, mine.Y + dy / length * away); more += 16) away += 16;
        return CanPay(Ai, type.Integer(Cost), type.Integer(LumberCost)) && IssueBuildOrderById(peon, id, mine.X + dx / length * away, mine.Y + dy / length * away);
    }

    /// <summary>A creep of the nearest camp whose creeps level to between the numbers, with no flyers unless they are allowed, or nil.</summary>
    /// <remarks>Close: a camp is the creeps within 600 of one and its level their levels added, the model's reading; the game's camps are not measured.</remarks>
    [Native(Level.Close)]
    public unit? GetCreepCamp(int min, int max, bool flyers_ok)
    {
        var home = Towns() is [var town, ..] ? town : WarOf(Ai).Home;
        return CreepsNear(home.Item1, home.Item2, 100000).OrderBy(c => Distance2(c.X - home.Item1, c.Y - home.Item2))
            .Select(c => (Creep: c, Camp: CreepsNear(c.X, c.Y, 600)))
            .Where(c => c.Camp.Sum(u => Math.Max(GetUnitLevel(u), 1)) is var level && level >= min && level <= max && (flyers_ok || !c.Camp.Any(Flies)))
            .Select(c => c.Creep).FirstOrDefault();
    }

    /// <summary>An enemy town hall other than the first one an enemy player made, or nil.</summary>
    /// <remarks>Close: an enemy's first town hall is taken as its main, the model's rule; the game's is not measured.</remarks>
    [Native(Level.Close)]
    public unit? GetEnemyExpansion() =>
        _units.Values.Where(h => !h.Dead && !h.Removed && !h.Constructing && h.Owner != Ai && GetPlayerId(h.Owner) < 24 && IsPlayerEnemy(Ai, h.Owner)
                && (BlzGetUnitIntegerField(h, Convert<unitintegerfield>(Classification)) & TownHallKind) != 0)
            .GroupBy(h => h.Owner).SelectMany(g => g.OrderBy(h => h.Id).Skip(1)).OrderBy(h => h.Id).FirstOrDefault();
}
