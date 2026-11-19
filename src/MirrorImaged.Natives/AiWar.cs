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
    [Native(Level.Close)]
    public bool AddAssault(int qty, int id)
    {
        if (qty <= 0 || Data.Object(ObjectKind.Unit, id) is null) return false;
        WarOf(Ai).Wave.Add((qty, id));
        MakeWave();
        return true;
    }

    /// <summary>Empties the wave.</summary>
    [Native(Level.Close)]
    public void InitAssault() => WarOf(Ai).Wave.Clear();

    [Native(Level.Close)]
    public void CreateCaptains() => WarOf(Ai);

    [Native(Level.Close)]
    public void SetCaptainHome(int which, float x, float y)
    {
        var war = WarOf(Ai);
        if (which == 0) war.Home = (x, y); else war.DefenceHome = (x, y);
    }

    [Native(Level.Close)] public void ResetCaptainLocs() => WarOf(Ai).Goal = null;
    [Native(Level.Close)] public void SetCaptainChanges(bool allow) { }
    [Native(Level.Close)] public void ClearCaptainTargets() => WarOf(Ai).Goal = null;
    [Native(Level.Close)] public void CaptainVsPlayer(player? id) { }
    [Native(Level.Close)] public void CaptainVsUnits(player? id) { }

    /// <summary>The captain fights its way to the place.</summary>
    [Native(Level.Close)]
    public void CaptainAttack(float x, float y)
    {
        var war = WarOf(Ai);
        (war.Goal, war.Retreating) = ((x, y), false);
        foreach (var u in Captain()) Give(u, OrderAttackId, x, y, null, 1);
    }

    [Native(Level.Close)] public void AttackMoveXY(int x, int y) => CaptainAttack(x, y);

    [Native(Level.Close)]
    public void AttackMoveKill(unit? target)
    {
        if (target is not null) CaptainAttack(target.X, target.Y);
    }

    /// <summary>The captain comes home and stays.</summary>
    [Native(Level.Close)]
    public void CaptainGoHome()
    {
        var war = WarOf(Ai);
        (war.Goal, war.Retreating) = (null, true);
        foreach (var u in Captain()) Give(u, OrderMoveId, war.Home.X, war.Home.Y, null, 1);
    }

    [Native(Level.Close)]
    public void TeleportCaptain(float x, float y)
    {
        foreach (var u in Captain()) SetUnitPosition(u, x, y);
    }

    [Native(Level.Close)] public int CaptainGroupSize() => Captain().Count;
    [Native(Level.Close)] public bool CaptainIsEmpty() => Captain().Count == 0;

    [Native(Level.Close)]
    public bool CaptainIsFull()
    {
        var wave = WarOf(Ai).Wave;
        return wave.Count > 0 && Captain() is var units && wave.All(w => units.Count(u => u.Type.Id == w.Id) >= w.Qty);
    }

    [Native(Level.Close)]
    public bool CaptainAtGoal() =>
        WarOf(Ai).Goal is { } goal && Captain() is { Count: > 0 } units && units.All(u => Distance2(u.X - goal.X, u.Y - goal.Y) <= AtGoalReach * AtGoalReach);

    [Native(Level.Close)]
    public bool CaptainIsHome() =>
        Captain() is var units && WarOf(Ai).Home is var home && units.All(u => Distance2(u.X - home.X, u.Y - home.Y) <= HomeReach * HomeReach);

    [Native(Level.Close)] public bool CaptainRetreating() => WarOf(Ai).Retreating && !CaptainIsHome();
    [Native(Level.Close)] public bool CaptainInCombat(bool attack_captain) => attack_captain && Captain().Any(u => u.AttackTarget is not null);

    // How much of the wave stands ready, in percent
    [Native(Level.Close)]
    public int CaptainReadiness()
    {
        var wave = WarOf(Ai).Wave;
        var total = wave.Sum(w => w.Qty);
        if (total == 0) return 0;
        var units = Captain();
        return 100 * wave.Sum(w => Math.Min(w.Qty, units.Count(u => u.Type.Id == w.Id))) / total;
    }

    [Native(Level.Close)] public int CaptainReadinessHP() => Captain() is { Count: > 0 } units ? (int)(100 * units.Average(u => u.Life / Math.Max(u.MaxLife, 1))) : 0;
    [Native(Level.Close)] public int CaptainReadinessMa() => Captain() is { Count: > 0 } units ? (int)(100 * units.Average(u => u.MaxMana > 0 ? u.Mana / u.MaxMana : 1)) : 0;

    /// <summary>The nearest building of an enemy player to the AI player's first town hall, a town hall first.</summary>
    [Native(Level.Close)]
    public unit? GetEnemyBase()
    {
        var from = Towns() is [var town, ..] ? town : WarOf(Ai).Home;
        return _units.Values.Where(b => !b.Dead && !b.Removed && IsBuildingNow(b) && b.Owner != Ai && GetPlayerId(b.Owner) < 24 && IsPlayerEnemy(Ai, b.Owner))
            .OrderBy(b => (BlzGetUnitIntegerField(b, Convert<unitintegerfield>(Classification)) & TownHallKind) != 0 ? 0 : 1)
            .ThenBy(b => Distance2(b.X - from.Item1, b.Y - from.Item2)).FirstOrDefault();
    }

    [Native(Level.Close)] public void StartGetEnemyBase() { }
    [Native(Level.Close)] public bool WaitGetEnemyBase() => false;

    /// <summary>How strong the enemies are: their fighting units that stand.</summary>
    [Native(Level.Close)]
    public int GetEnemyPower() =>
        _units.Values.Count(u => !u.Dead && !u.Removed && !IsBuildingNow(u) && u.Owner != Ai && GetPlayerId(u.Owner) < 24 && IsPlayerEnemy(Ai, u.Owner) && u.Type.Integer(WeaponsOn) != 0);

    // The gold mine nearest the AI player's first town that is nobody's and that no town hall stands within reach of, or none
    unit? FreeMine()
    {
        var home = Towns() is [var town, ..] ? town : WarOf(Ai).Home;
        var halls = _units.Values.Where(h => !h.Dead && !h.Removed && IsTownHall(h)).ToList();
        return _units.Values.Where(m => !m.Dead && !m.Removed && GetResourceAmount(m) > 0 && GetPlayerId(m.Owner) >= 24 && !halls.Any(h => Distance2(h.X - m.X, h.Y - m.Y) <= TownReach * TownReach))
            .OrderBy(m => Distance2(m.X - home.Item1, m.Y - home.Item2)).FirstOrDefault();
    }

    // The creeps stand near the place: living units of the neutral aggressive player, a building not counted
    List<unit> CreepsNear(float x, float y, float reach) =>
        [.. _units.Values.Where(c => !c.Dead && !c.Removed && GetPlayerId(c.Owner) == GetPlayerNeutralAggressive() && !IsBuildingNow(c) && Distance2(c.X - x, c.Y - y) <= reach * reach)];

    /// <summary>0 when a gold mine without a town hall remains to expand to, else -1.</summary>
    [Native(Level.Close)] public int GetNextExpansion() => FreeMine() is null ? -1 : 0;
    [Native(Level.Close)] public int GetExpansionX() => FreeMine() is { } mine ? (int)mine.X : -1;
    [Native(Level.Close)] public int GetExpansionY() => FreeMine() is { } mine ? (int)mine.Y : -1;

    /// <summary>A creep guarding the mine to expand to, or nil.</summary>
    [Native(Level.Close)]
    public unit? GetExpansionFoe() =>
        FreeMine() is { } mine ? CreepsNear(mine.X, mine.Y, 700).OrderBy(c => Distance2(c.X - mine.X, c.Y - mine.Y)).FirstOrDefault() : null;

    /// <summary>The worker nearest the mine to expand to that is idle or gathering, or nil.</summary>
    [Native(Level.Close)]
    public unit? GetExpansionPeon() =>
        FreeMine() is { } mine ? Mine(Ai).Where(w => !w.Constructing && w.Building is null && IsWorker(w) && w.Order is 0 or OrderHarvest or OrderReturn)
            .OrderBy(w => Distance2(w.X - mine.X, w.Y - mine.Y)).FirstOrDefault() : null;

    /// <summary>The worker goes to build the town hall type beside the mine to expand to; true when it was sent.</summary>
    [Native(Level.Close)]
    public bool SetExpansion(unit? peon, int id)
    {
        if (peon is null || FreeMine() is not { } mine || Data.Object(ObjectKind.Unit, id) is not { } type) return false;
        if (Mine(Ai).Any(w => w.Order == id)) return true; // a worker is on its way to build it
        var from = Towns() is [var town, ..] ? town : WarOf(Ai).Home;
        var (dx, dy) = (from.Item1 - mine.X, from.Item2 - mine.Y);
        var length = MathF.Max(MathF.Sqrt(dx * dx + dy * dy), 1);
        var away = mine.Type.Real(Collision) + type.Real(Collision) + 60;
        return CanPay(Ai, type.Integer(Cost), type.Integer(LumberCost)) && IssueBuildOrderById(peon, id, mine.X + dx / length * away, mine.Y + dy / length * away);
    }

    /// <summary>A creep of the nearest camp whose creeps level to between the numbers, with no flyers unless they are allowed, or nil.</summary>
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
    [Native(Level.Close)]
    public unit? GetEnemyExpansion() =>
        _units.Values.Where(h => !h.Dead && !h.Removed && !h.Constructing && h.Owner != Ai && GetPlayerId(h.Owner) < 24 && IsPlayerEnemy(Ai, h.Owner)
                && (BlzGetUnitIntegerField(h, Convert<unitintegerfield>(Classification)) & TownHallKind) != 0)
            .GroupBy(h => h.Owner).SelectMany(g => g.OrderBy(h => h.Id).Skip(1)).OrderBy(h => h.Id).FirstOrDefault();
}
