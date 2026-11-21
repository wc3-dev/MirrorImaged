using MirrorImaged.Data;
using MirrorImaged.World;

namespace MirrorImaged.Natives;

// What a building has in its queue: a unit to train or an upgrade to research, with what it cost and when it is done
sealed class Training(bool research, int id, int gold, int lumber, int food, double seconds, bool upgrade = false, bool revive = false)
{
    public readonly bool Research = research, Upgrade = upgrade, Revive = revive;
    public readonly int Id = id, Gold = gold, Lumber = lumber, Food = food;
    public readonly double Seconds = seconds;
    public long Done;   // the step it finishes at once it has started, 0 until then
    public bool Started, Token;   // Token: paid for with a hero token, not with gold and lumber
}

public partial class unit
{
    internal bool Constructing;                       // a building still being made
    internal unit? Builder, Building;                 // the worker making it, and what a worker is making
    internal long ConstructDone;
    internal readonly List<Training> Train = [];
}

// Buildings, training and research (waves 3's economy): a worker given a build order walks to the place, pays and starts the building,
// which finishes after its build time; a building given a unit's or an upgrade's id as an immediate order queues it, pays at once, trains one
// at a time and fires the start, cancel and finish events. Gold and lumber are the player's states, food is held while a unit is in the
// queue. Close: the game's refunds, rally orders and the building's life while it grows are not measured (decisions D353).
public sealed partial class GameNatives
{
    const int OrderCancel = 851976;
    // common.j has no unit-level construct start event (63 is EVENT_UNIT_RESCUED): this id is registered by nothing
    const int UnitConstructStart = 9063, UnitConstructCancel = 64, UnitConstructFinish = 65, PlayerConstructStart = 26, PlayerConstructCancel = 27, PlayerConstructFinish = 28;
    const int UnitTrainStart = 69, UnitTrainCancel = 70, UnitTrainFinish = 71, PlayerTrainStart = 32, PlayerTrainCancel = 33, PlayerTrainFinish = 34;
    const int UnitResearchStart = 72, UnitResearchCancel = 73, UnitResearchFinish = 74, PlayerResearchStart = 35, PlayerResearchCancel = 36, PlayerResearchFinish = 37;
    const int QueueLength = 7, HeroTokens = 3;
    const float StartLifeShare = 0.1f, CancelRefund = 0.75f;

    static readonly int Cost = Code("ugol"), LumberCost = Code("ulum"), BuildTime = Code("ubld"), Trains = Code("utra"), Researches = Code("ures"), FoodMadeField = Code("ufma"), Revives = Code("urev");

    // Whether the building revives the hero: the game's data marks a building that revives heroes (an altar) with 1, a map's may list them
    bool Revive(unit building, int id) => Listed(building, Revives, id) || building.Type.Text(Revives) == "1" && IsHero(id);

    bool Listed(unit u, int field, int id) => (u.Type.Text(field) ?? "").Split(',', StringSplitOptions.TrimEntries).Contains(Name(id));

    int Spend(player p, int state) => p.States[state];

    bool CanPay(player p, int gold, int lumber) => p.States[Gold] >= gold && p.States[Lumber] >= lumber;

    void Pay(player p, int gold, int lumber)
    {
        SetPlayerState(p, Convert<playerstate>(Gold), p.States[Gold] - gold);
        SetPlayerState(p, Convert<playerstate>(Lumber), p.States[Lumber] - lumber);
    }

    // ---- Construction

    const float HauntReach = 300;
    // How near its site a worker starts the building: 59 to 64 in the game (probe units-build-reach), as it walks on to just inside this (D720)
    const float BuildReach = 60;

    // A worker at the site: the building made and paid for, the construct events fired; false when the player can't pay. A haunted mine is
    // made of the gold mine beside the site and keeps its gold, as BlightGoldMineForPlayerBJ makes one, and is not made where none stands.
    // shortcut: the gold mine doesn't come back when the haunted one dies or is cancelled; model that when a map is seen to need it
    bool Place(unit worker, int id, float x, float y)
    {
        if (Data.Object(ObjectKind.Unit, id) is not { } type) return false;
        var (gold, lumber) = (type.Integer(Cost), type.Integer(LumberCost));
        var haunts = (type.Text(Code("uabi")) ?? "").Split(',', StringSplitOptions.TrimEntries).Contains("Abgm");
        var under = !haunts ? null : _units.Values.Where(m => !m.Dead && !m.Removed && GetPlayerId(m.Owner) >= 24 && GetResourceAmount(m) > 0 && Distance2(m.X - x, m.Y - y) <= HauntReach * HauntReach)
            .OrderBy(m => Distance2(m.X - x, m.Y - y)).ThenBy(m => m.Id).FirstOrDefault();
        if (haunts && under is null) return false;
        if (!CanPay(worker.Owner, gold, lumber)) return false;
        Pay(worker.Owner, gold, lumber);
        var left = GetResourceAmount(under);
        if (under is not null)
        {
            (x, y) = (under.X, under.Y);
            RemoveUnit(under);
        }
        if (Create(worker.Owner, id, x, y, 270, id) is not { } building) return false;
        if (under is not null) SetResourceAmount(building, left);
        building.Constructing = true;
        building.Builder = worker;
        worker.Building = building;
        building.Life = Math.Max(building.MaxLife * StartLifeShare, 1);
        // The food a building makes counts when it is done
        worker.Owner.States[FoodCap] -= type.Integer(FoodMadeField);
        var seconds = Math.Max(type.Real(BuildTime), 0.1);
        FireUnit(new(building), UnitConstructStart, PlayerConstructStart);
        building.ConstructDone = _game.Step + Math.Max(1, Game.Steps(seconds));
        _game.At(building.ConstructDone, () => FinishBuilding(building));
        return true;
    }

    void FinishBuilding(unit building)
    {
        if (!building.Constructing || building.Dead || building.Removed) return;
        building.Constructing = false;
        building.Owner.States[FoodCap] += building.Type.Integer(FoodMadeField);
        SetLife(building, building.MaxLife);
        if (building.Builder is { } worker)
        {
            worker.Building = null;
            if (worker.Order == building.Type.Id) Finish(worker);
        }
        FireUnit(new(building), UnitConstructFinish, PlayerConstructFinish);
    }

    void CancelBuilding(unit building)
    {
        if (!building.Constructing) return;
        building.Constructing = false;
        var type = building.Type;
        var share = CancelRefund;
        var gold = (int)(type.Integer(Cost) * share);
        var lumber = (int)(type.Integer(LumberCost) * share);
        SetPlayerState(building.Owner, Convert<playerstate>(Gold), building.Owner.States[Gold] + gold);
        SetPlayerState(building.Owner, Convert<playerstate>(Lumber), building.Owner.States[Lumber] + lumber);
        if (building.Builder is { } worker)
        {
            worker.Building = null;
            if (worker.Order == type.Id) Finish(worker);
        }
        building.Owner.States[FoodCap] += type.Integer(FoodMadeField);
        FireUnit(new(building), UnitConstructCancel, PlayerConstructCancel);
        RemoveUnit(building);
    }

    // ---- Training and research

    // An order a building takes into its queue: a unit type it trains or an upgrade it researches
    bool QueueOrder(unit building, int order)
    {
        if (building.Constructing || building.Dead || building.Train.Count >= QueueLength) return false;
        Training item;
        if (Revive(building, order) && ReviveCandidate(building.Owner, order) is { } dead && !building.Train.Any(t => t.Revive))
        {
            // A dead hero the building revives: it costs a share of the hero's own price, more for each level it had
            var type = dead.Type;
            // The constants of the game: the share of the price (a base and a part for each level above the first, at most ReviveMaxFactor), the most it costs in gold and lumber,
            // and the share of the build time (at most HeroMaxReviveTime)
            var share = Math.Min(Data.Constant("ReviveBaseFactor", 0.4f) + Data.Constant("ReviveLevelFactor", 0.1f) * (Math.Max(dead.HeroLevel, 1) - 1), Data.Constant("ReviveMaxFactor", 4f));
            var gold = Math.Min((int)(type.Integer(Cost) * share), (int)Data.Constant("HeroMaxReviveCostGold", 700));
            var lumber = Math.Min((int)(type.Integer(LumberCost) * (Data.Constant("ReviveBaseLumberFactor", 0) + Data.Constant("ReviveLumberLevelFactor", 0) * (Math.Max(dead.HeroLevel, 1) - 1))), (int)Data.Constant("HeroMaxReviveCostLumber", 0));
            var seconds = Math.Min(type.Real(BuildTime) * Data.Constant("ReviveTimeFactor", 0.65f), Data.Constant("HeroMaxReviveTime", 150));
            item = new Training(false, order, gold, lumber, 0, Math.Max(seconds, 0.1), revive: true);
            if (!CanPay(building.Owner, item.Gold, item.Lumber)) return false;
        }
        else if (Listed(building, Trains, order) && Data.Object(ObjectKind.Unit, order) is { } unitType)
        {
            var p = building.Owner;
            // A hero trained while the player has a hero token costs the token and no gold or lumber
            var token = IsHero(order) && p.States[HeroTokens] > 0;
            item = new Training(false, order, token ? 0 : unitType.Integer(Cost), token ? 0 : unitType.Integer(LumberCost), unitType.Integer(FoodUsed), Math.Max(unitType.Real(BuildTime), 0.1)) { Token = token };
            // A hero is one a player has once: alive, dead (then it is revived) or in training
            if (IsHero(order) && (_units.Values.Any(h => h.Owner == p && h.Type.Id == order && !h.Removed) || _units.Values.Any(b => b.Owner == p && b.Train.Any(t => !t.Research && !t.Revive && t.Id == order)))) return false;
            if (!CanPay(p, item.Gold, item.Lumber) || p.States[FoodUsedState] + item.Food > Math.Min(p.States[FoodCap], p.States[FoodCapCeiling])) return false;
            p.States[FoodUsedState] += item.Food;
        }
        else if (Listed(building, Researches, order) && Data.Object(ObjectKind.Upgrade, order) is { } upgrade)
        {
            var level = building.Owner.TechLevels.GetValueOrDefault(order) + building.Train.Count(t => t.Research && t.Id == order);
            if (level >= upgrade.Levels) return false;
            item = new Training(true, order, upgrade.Integer(Code("gglb")) + level * upgrade.Integer(Code("gglm")), upgrade.Integer(Code("glmb")) + level * upgrade.Integer(Code("glmm")), 0,
                Math.Max(upgrade.Integer(Code("gtib")) + level * upgrade.Integer(Code("gtim")), 1));
            if (!CanPay(building.Owner, item.Gold, item.Lumber)) return false;
        }
        else if (Listed(building, UpgradesTo, order) && Data.Object(ObjectKind.Unit, order) is { } target && building.Train.Count == 0)
        {
            // The building becomes the other one when its time is out; it trains nothing meanwhile
            item = new Training(false, order, target.Integer(Cost), target.Integer(LumberCost), 0, Math.Max(target.Real(BuildTime), 0.1), upgrade: true);
            if (!CanPay(building.Owner, item.Gold, item.Lumber)) return false;
        }
        else return false;
        if (building.Train.Count > 0 && building.Train[0].Upgrade) return false;
        Pay(building.Owner, item.Gold, item.Lumber);
        if (item.Token) building.Owner.States[HeroTokens]--;
        building.Train.Add(item);
        if (building.Train.Count == 1) StartTraining(building);
        return true;
    }

    void StartTraining(unit building)
    {
        if (building.Train.Count == 0) return;
        var item = building.Train[0];
        item.Started = true;
        item.Done = _game.Step + Math.Max(1, Game.Steps(item.Seconds));
        FireUnit(new(building, TypeId: item.Id), item.Research ? UnitResearchStart : UnitTrainStart, item.Research ? PlayerResearchStart : PlayerTrainStart);
        _game.At(item.Done, () => FinishTraining(building, item));
    }

    void FinishTraining(unit building, Training item)
    {
        if (building.Dead || building.Removed || building.Train.Count == 0 || building.Train[0] != item) return;
        building.Train.RemoveAt(0);
        if (item.Upgrade) Transform(building, Data.Object(ObjectKind.Unit, item.Id)!);
        else if (item.Revive)
        {
            if (ReviveCandidate(building.Owner, item.Id) is { } hero) ReviveHero(hero, building.X, building.Y - building.Type.Real(Collision) - 40, true);
        }
        else if (item.Research)
        {
            building.Owner.TechLevels[item.Id] = building.Owner.TechLevels.GetValueOrDefault(item.Id) + 1;
            FireUnit(new(building, TypeId: item.Id), UnitResearchFinish, PlayerResearchFinish);
        }
        else
        {
            var made = Create(building.Owner, item.Id, building.X, building.Y - building.Type.Real(Collision) - 40, 270, item.Id);
            building.Owner.States[FoodUsedState] -= item.Food;
            if (made is not null)
            {
                // A worker made with its rally at a mine or a tree harvests it (probe units-harvest-2)
                if (building.RallyX != 0 || building.RallyY != 0 || building.RallyTarget is not null)
                    Give(made, building.RallyTarget is { } at && CanHarvest(made, at) ? OrderHarvest : Smart, building.RallyX, building.RallyY, building.RallyTarget, building.RallyTarget is not null ? 2 : 1);
                FireUnit(new(building, TypeId: item.Id, Other: made), UnitTrainFinish, PlayerTrainFinish);
            }
        }
        StartTraining(building);
    }

    // A queued item gone with its cost back (all of it), the newest unless the place in the queue is given; false when there is none there
    bool CancelQueue(unit building, int at = -1)
    {
        if (at < 0) at = building.Train.Count - 1;
        if (at < 0 || at >= building.Train.Count) return false;
        var item = building.Train[at];
        building.Train.RemoveAt(at);
        SetPlayerState(building.Owner, Convert<playerstate>(Gold), building.Owner.States[Gold] + item.Gold);
        SetPlayerState(building.Owner, Convert<playerstate>(Lumber), building.Owner.States[Lumber] + item.Lumber);
        building.Owner.States[FoodUsedState] -= item.Food;
        if (item.Token) building.Owner.States[HeroTokens]++;
        FireUnit(new(building, TypeId: item.Id), item.Research ? UnitResearchCancel : UnitTrainCancel, item.Research ? PlayerResearchCancel : PlayerTrainCancel);
        if (item.Started) StartTraining(building);
        return true;
    }

    // An order the economy takes before the order itself is given: cancelling, and a queue order on a building
    bool Economy(unit u, int order, int kind, out bool answer)
    {
        answer = false;
        if (order == OrderCancel)
        {
            answer = u.Constructing ? Cancelled(u) : CancelQueue(u);
            return true;
        }
        if (kind == 0 && (u.Type.Integer(IsBuilding) != 0 || IsBuildingNow(u)) && (Listed(u, Trains, order) || Listed(u, Researches, order) || Listed(u, UpgradesTo, order) || Revive(u, order)))
        {
            answer = QueueOrder(u, order);
            return true;
        }
        return false;
    }

    // A person taking the item at a place of the building's queue out of it, as a replay's action 0x1E does: the item there if it is of the
    // type, else the newest of the type
    internal bool Dequeue(unit building, int at, int type) =>
        CancelQueue(building, at < building.Train.Count && building.Train[at].Id == type ? at : building.Train.FindLastIndex(t => t.Id == type) is var last and >= 0 ? last : int.MaxValue);

    bool Cancelled(unit building)
    {
        CancelBuilding(building);
        return true;
    }

    [Native(Level.Close)] public unit? GetConstructingStructure() => Response(UnitConstructStart, PlayerConstructStart)?.Unit;
    [Native(Level.Close)] public unit? GetConstructedStructure() => Response(UnitConstructFinish, PlayerConstructFinish)?.Unit;
    [Native(Level.Close)] public unit? GetCancelledStructure() => Response(UnitConstructCancel, PlayerConstructCancel)?.Unit;
    [Native(Level.Close)] public unit? GetTrainedUnit() => Response(UnitTrainFinish, PlayerTrainFinish)?.Other;
    [Native(Level.Close)] public int GetTrainedUnitType() => Response(UnitTrainStart, PlayerTrainStart, UnitTrainCancel, PlayerTrainCancel, UnitTrainFinish, PlayerTrainFinish)?.TypeId ?? 0;
    [Native(Level.Close)] public int GetResearched() => Response(UnitResearchStart, PlayerResearchStart, UnitResearchCancel, PlayerResearchCancel, UnitResearchFinish, PlayerResearchFinish)?.TypeId ?? 0;
    [Native(Level.Close)] public unit? GetResearchingUnit() => Response(UnitResearchStart, PlayerResearchStart, UnitResearchCancel, PlayerResearchCancel, UnitResearchFinish, PlayerResearchFinish)?.Unit;

    // The player's dead hero of the type that a revive order names, the first made
    unit? ReviveCandidate(player p, int type) => _units.Values.Where(h => h.Owner == p && h.Type.Id == type && h.Dead && !h.Removed).OrderBy(h => h.Id).FirstOrDefault();

    // The building is the other type from now on: its life and mana keep their share, its abilities are the new type's, the food it makes changes
    void Transform(unit building, GameObject type)
    {
        var old = building.Type;
        var (lifeShare, manaShare) = (building.MaxLife > 0 ? building.Life / building.MaxLife : 1, building.MaxMana > 0 ? building.Mana / building.MaxMana : 0);
        building.Owner.States[FoodCap] += type.Integer(FoodMadeField) - building.Type.Integer(FoodMadeField);
        building.Type = type;
        building.MaxLife = type.Integer(HitPoints);
        building.MaxMana = type.Integer(ManaMaximum);
        building.Life = building.MaxLife * lifeShare;
        building.Mana = Math.Min(building.MaxMana * manaShare, building.MaxMana);
        building.Scale = type.Real(ModelScale);
        // What the old type gave goes and the new type's comes; what a script gave the unit stays only when it made it permanent (UnitMakeAbilityPermanent), as in the game
        var given = (old == type ? "" : old.Text(AbilityList) ?? "").Split(',', StringSplitOptions.TrimEntries).Select(Code).ToHashSet();
        var kept = building.Abilities.Where(a => !given.Contains(a.Type.Id) && a.Permanent).ToList();
        building.Abilities.Clear();
        GiveAbilities(building);
        foreach (var a in kept) if (Find(building, a.Type.Id) is null) building.Abilities.Add(a);
    }
}
