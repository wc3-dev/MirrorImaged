using MirrorImaged.Data;

namespace MirrorImaged.Natives;

public partial class itempool
{
    internal readonly List<(int Id, float Weight)> Types = [];
}

public partial class unitpool
{
    internal readonly List<(int Id, float Weight)> Types = [];
}

// The random item and unit picks: ChooseRandomItem and its kin, the item and unit pools, and the stock a shop would
// sell, which nothing here sells (wave 3's orders). The picks come from the game's random numbers here, not the game's
// own, so what they pick is the map's seed's, not the game's (decisions D338).
public sealed partial class GameNatives
{
    const string StockReason = "A shop sells and restocks through orders and the clock of wave 3; no native here reads a stock back";

    static readonly int ItemPickRandom = Code("iprn");

    // An item type the data lists, in the data's order, that a random pick may choose: its level and class as asked (the
    // level -1 any, the class ITEM_TYPE_ANY (9) any), and one with pickRandom unless the caller includes the others
    List<int> RandomItems(int type, int level, bool nonPickRandom) =>
        [.. Data.Objects(ObjectKind.Item)
            .Where(o => (nonPickRandom || o.Integer(ItemPickRandom) != 0) && (level < 0 || o.Integer(ItemLevel) == level)
                && (type == 9 || ItemClasses.GetValueOrDefault(o.Text(ItemClass) ?? "", 8) == type))
            .Select(o => o.Id)];

    int Pick(List<int> ids) => ids.Count == 0 ? 0 : ids[_game.RandomInt(0, ids.Count - 1)];

    /// <summary>A random item type of the level (-1 for any) the data marks pick-random, 0 for none. Close: the draw is the map's random numbers, not the game's.</summary>
    [Native(Level.Close)]
    public int ChooseRandomItem(int level) => Pick(RandomItems(9, level, false));

    [Native(Level.Close)]
    public int ChooseRandomItemEx(itemtype? whichType, int level) => Pick(RandomItems(whichType?.Id ?? 9, level, false));

    /// <summary>Close: the equipment type and tag filters have nothing to filter (no equipment yet), so they are ignored.</summary>
    [Native(Level.Close)]
    public int ChooseRandomItemExWithFilter(itemtype? whichType, int level, equipmentType? whichEquipmentType, itemTag? whichTag) =>
        ChooseRandomItemEx(whichType, level);

    [Native(Level.Close)]
    public int ChooseRandomItemExWithFilterAndIncludes(itemtype? whichType, int level, equipmentType? whichEquipmentType, itemTag? whichTag, bool includeInvalidMorphs, bool includeNonPickRandom) =>
        Pick(RandomItems(whichType?.Id ?? 9, level, includeNonPickRandom));

    /// <summary>A random creep unit type of the level: not brought (needs the creep data's levels and the camps); 0.</summary>
    [Native(Level.Close)]
    public int ChooseRandomCreep(int level) => 0;

    /// <summary>A random neutral building type: not brought; 0.</summary>
    [Native(Level.Close)]
    public int ChooseRandomNPBuilding() => 0;

    [Native(Level.Close)]
    public itempool? CreateItemPool() => Make(new itempool());

    [Native(Level.Close)]
    public void DestroyItemPool(itempool? whichItemPool)
    {
        if (whichItemPool is not null) _game.Remove(whichItemPool.Key);
    }

    /// <summary>Adds the type with its weight, or changes the weight of one the pool has.</summary>
    [Native(Level.Close)]
    public void ItemPoolAddItemType(itempool? whichItemPool, int itemId, float weight) => Weigh(whichItemPool?.Types, itemId, weight);

    [Native(Level.Close)]
    public void ItemPoolRemoveItemType(itempool? whichItemPool, int itemId) => whichItemPool?.Types.RemoveAll(t => t.Id == itemId);

    static void Weigh(List<(int Id, float Weight)>? types, int id, float weight)
    {
        if (types is null) return;
        types.RemoveAll(t => t.Id == id);
        types.Add((id, weight));
    }

    // A type with its chance in the total of the weights, the weights of 0 or less counting nothing
    int Weighted(List<(int Id, float Weight)> types)
    {
        var total = types.Sum(t => Math.Max(t.Weight, 0));
        if (total <= 0) return 0;
        var at = _game.RandomReal(0, total);
        foreach (var (id, weight) in types)
            if (weight > 0 && (at -= weight) < 0) return id;
        return types.Last(t => t.Weight > 0).Id;
    }

    /// <summary>An item of a type the pool picks by weight, made at the place; nil for an empty pool.</summary>
    [Native(Level.Close)]
    public item? PlaceRandomItem(itempool? whichItemPool, float x, float y) =>
        whichItemPool is not null && Weighted(whichItemPool.Types) is var id and not 0 ? CreateItem(id, x, y) : null;

    [Native(Level.Close)]
    public unitpool? CreateUnitPool() => Make(new unitpool());

    [Native(Level.Close)]
    public void DestroyUnitPool(unitpool? whichPool)
    {
        if (whichPool is not null) _game.Remove(whichPool.Key);
    }

    [Native(Level.Close)]
    public void UnitPoolAddUnitType(unitpool? whichPool, int unitId, float weight) => Weigh(whichPool?.Types, unitId, weight);

    [Native(Level.Close)]
    public void UnitPoolRemoveUnitType(unitpool? whichPool, int unitId) => whichPool?.Types.RemoveAll(t => t.Id == unitId);

    /// <summary>A unit of a type the pool picks by weight, made for the player; nil for an empty pool.</summary>
    [Native(Level.Close)]
    public unit? PlaceRandomUnit(unitpool? whichPool, player? forWhichPlayer, float x, float y, float facing) =>
        whichPool is not null && Weighted(whichPool.Types) is var id and not 0 ? CreateUnit(forWhichPlayer, id, x, y, facing) : null;

    // ---- Stock

    [Native(Level.Recorded, Reason = StockReason)]
    public void AddItemToAllStock(int itemId, int currentStock, int stockMax)
    {
    }

    [Native(Level.Recorded, Reason = StockReason)]
    public void AddItemToStock(unit? whichUnit, int itemId, int currentStock, int stockMax)
    {
    }

    [Native(Level.Recorded, Reason = StockReason)]
    public void AddUnitToAllStock(int unitId, int currentStock, int stockMax)
    {
    }

    [Native(Level.Recorded, Reason = StockReason)]
    public void AddUnitToStock(unit? whichUnit, int unitId, int currentStock, int stockMax)
    {
    }

    [Native(Level.Recorded, Reason = StockReason)]
    public void RemoveItemFromAllStock(int itemId)
    {
    }

    [Native(Level.Recorded, Reason = StockReason)]
    public void RemoveItemFromStock(unit? whichUnit, int itemId)
    {
    }

    [Native(Level.Recorded, Reason = StockReason)]
    public void RemoveUnitFromAllStock(int unitId)
    {
    }

    [Native(Level.Recorded, Reason = StockReason)]
    public void RemoveUnitFromStock(unit? whichUnit, int unitId)
    {
    }

    [Native(Level.Recorded, Reason = StockReason)]
    public void SetAllItemTypeSlots(int slots)
    {
    }

    [Native(Level.Recorded, Reason = StockReason)]
    public void SetAllUnitTypeSlots(int slots)
    {
    }

    [Native(Level.Recorded, Reason = StockReason)]
    public void SetItemTypeSlots(unit? whichUnit, int slots)
    {
    }

    [Native(Level.Recorded, Reason = StockReason)]
    public void SetUnitTypeSlots(unit? whichUnit, int slots)
    {
    }
}
