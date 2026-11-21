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

public partial class unit
{
    internal readonly Dictionary<int, (int Stock, int Max, double Open)> Stocked = [];   // the goods AddItemToStock gave a shop: left in stock, its limit, when it is on sale
}

// The random item and unit picks: ChooseRandomItem and its kin, the item and unit pools, and the stock natives of the shops. The picks come from the game's random numbers here, not the game's
// own, so what they pick is the map's seed's, not the game's (decisions D338).
public sealed partial class GameNatives
{
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
    /// <remarks>Close: the draw is the map's random numbers, not the game's (D338).</remarks>
    [Native(Level.Close)]
    public int ChooseRandomItem(int level) => Pick(RandomItems(9, level, false));

    /// <summary>As ChooseRandomItem, for the item class asked (any for ITEM_TYPE_ANY); 0 for none.</summary>
    /// <remarks>Close: the draw is the map's random numbers, not the game's (D338).</remarks>
    [Native(Level.Close)]
    public int ChooseRandomItemEx(itemtype? whichType, int level) => Pick(RandomItems(whichType?.Id ?? 9, level, false));

    /// <summary>Close: the equipment type and tag filters have nothing to filter (no equipment yet), so they are ignored.</summary>
    /// <remarks>Close: the equipment type and tag filters are ignored, and the draw is the map's random numbers, not the game's (D338).</remarks>
    [Native(Level.Close)]
    public int ChooseRandomItemExWithFilter(itemtype? whichType, int level, equipmentType? whichEquipmentType, itemTag? whichTag) =>
        ChooseRandomItemEx(whichType, level);

    /// <summary>As ChooseRandomItemEx, with the choice of including the non-pick-random types; the invalid morphs flag is ignored.</summary>
    /// <remarks>Close: the invalid morphs flag is ignored, and the draw is the map's random numbers, not the game's (D338).</remarks>
    [Native(Level.Close)]
    public int ChooseRandomItemExWithFilterAndIncludes(itemtype? whichType, int level, equipmentType? whichEquipmentType, itemTag? whichTag, bool includeInvalidMorphs, bool includeNonPickRandom) =>
        Pick(RandomItems(whichType?.Id ?? 9, level, includeNonPickRandom));

    static readonly int UnitTilesets = Code("util"), UnitSpecial = Code("uspe"), NeutralBuildingRandom = Code("unbr");

    // A unit type the data lists that a random pick may choose, as the game's lists were (probe units-random-creeps): for the map's tileset or all of them
    bool OnTileset(GameObject o) =>
        (o.Text(UnitTilesets) ?? "").Split(',', StringSplitOptions.TrimEntries).Any(tileset => tileset == "*" || tileset.Length == 1 && tileset[0] == (char)(Data.Map?.Files.Info?.Tileset ?? 0));

    // The creeps that aren't buildings, special or heroes (33 of the 864 units on the probe map's tileset), of the level asked (-1 any; none has no level: 0)
    /// <summary>A random creep type of the level (-1 for any) that is no building, special or hero and is on the map's tileset; 0 for none.</summary>
    /// <remarks>Close: The game's lists for every level were read on the Lordaeron Summer map of the probe and matched by the data's rule above, whose draw is the map's random numbers, not the game's.</remarks>
    [Native(Level.Close, Fact = "probe units-random-creeps (11-21, D794)", Reason = "The game's lists for every level were read on the Lordaeron Summer map of the probe and matched by the data's rule above, whose draw is the map's random numbers, not the game's")]
    public int ChooseRandomCreep(int level) =>
        Pick([.. Data.Objects(ObjectKind.Unit).Where(o => o.Text(Race) == "creeps" && !IsHero(o.Id) && o.Integer(IsBuilding) == 0 && o.Integer(UnitSpecial) == 0 && OnTileset(o)
            && (level == -1 || o.Integer(UnitLevel) == level)).Select(o => o.Id)]);

    /// <summary>A random neutral building type the data marks for random picks and that is on the map's tileset; 0 for none.</summary>
    /// <remarks>Close: The game's list was read on the Lordaeron Summer map of the probe (9 buildings) and matched by the data's rule, whose draw is the map's random numbers, not the game's.</remarks>
    [Native(Level.Close, Fact = "probe units-random-creeps (11-21, D794)", Reason = "The game's list was read on the Lordaeron Summer map of the probe (9 buildings) and matched by the data's rule, whose draw is the map's random numbers, not the game's")]
    public int ChooseRandomNPBuilding() =>
        Pick([.. Data.Objects(ObjectKind.Unit).Where(o => o.Integer(NeutralBuildingRandom) != 0 && OnTileset(o)).Select(o => o.Id)]);

    /// <summary>A new, empty item pool.</summary>
    /// <remarks>Close: the pool's picks are the map's random numbers, not the game's (D338).</remarks>
    [Native(Level.Close)]
    public itempool? CreateItemPool() => Make(new itempool());

    /// <summary>Removes the item pool from the game; nothing for a null pool.</summary>
    /// <remarks>Close: what the game does with a destroyed pool that is used again is not measured.</remarks>
    [Native(Level.Close)]
    public void DestroyItemPool(itempool? whichItemPool)
    {
        if (whichItemPool is not null) _game.Remove(whichItemPool.Key);
    }

    /// <summary>Adds the type with its weight, or changes the weight of one the pool has.</summary>
    /// <remarks>Close: the weights count in the pick as given, 0 or less counting nothing; the game's use of them is not measured.</remarks>
    [Native(Level.Close)]
    public void ItemPoolAddItemType(itempool? whichItemPool, int itemId, float weight) => Weigh(whichItemPool?.Types, itemId, weight);

    /// <summary>Takes the type out of the item pool; nothing for a null pool or a type it doesn't hold.</summary>
    /// <remarks>Close: a type the pool doesn't hold is ignored, not measured in the game.</remarks>
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
    /// <remarks>Close: the pick is drawn from the map's random numbers, not the game's (D338).</remarks>
    [Native(Level.Close)]
    public item? PlaceRandomItem(itempool? whichItemPool, float x, float y) =>
        whichItemPool is not null && Weighted(whichItemPool.Types) is var id and not 0 ? CreateItem(id, x, y) : null;

    /// <summary>A new, empty unit pool.</summary>
    /// <remarks>Close: the pool's picks are the map's random numbers, not the game's (D338).</remarks>
    [Native(Level.Close)]
    public unitpool? CreateUnitPool() => Make(new unitpool());

    /// <summary>Removes the unit pool from the game; nothing for a null pool.</summary>
    /// <remarks>Close: what the game does with a destroyed pool that is used again is not measured.</remarks>
    [Native(Level.Close)]
    public void DestroyUnitPool(unitpool? whichPool)
    {
        if (whichPool is not null) _game.Remove(whichPool.Key);
    }

    /// <summary>Adds the type with its weight to the unit pool, or changes the weight of one the pool has; nothing for a null pool.</summary>
    /// <remarks>Close: the weights count in the pick as given, 0 or less counting nothing; the game's use of them is not measured.</remarks>
    [Native(Level.Close)]
    public void UnitPoolAddUnitType(unitpool? whichPool, int unitId, float weight) => Weigh(whichPool?.Types, unitId, weight);

    /// <summary>Takes the type out of the unit pool; nothing for a null pool or a type it doesn't hold.</summary>
    /// <remarks>Close: a type the pool doesn't hold is ignored, not measured in the game.</remarks>
    [Native(Level.Close)]
    public void UnitPoolRemoveUnitType(unitpool? whichPool, int unitId) => whichPool?.Types.RemoveAll(t => t.Id == unitId);

    /// <summary>A unit of a type the pool picks by weight, made for the player; nil for an empty pool.</summary>
    /// <remarks>Close: the pick is drawn from the map's random numbers, not the game's (D338).</remarks>
    [Native(Level.Close)]
    public unit? PlaceRandomUnit(unitpool? whichPool, player? forWhichPlayer, float x, float y, float facing) =>
        whichPool is not null && Weighted(whichPool.Types) is var id and not 0 ? CreateUnit(forWhichPlayer, id, x, y, facing) : null;

    // ---- Stock (probe items-stock, D796): a good added to a shop with Sell Items is sold as many times as its stock says, from two seconds into the game or from
    // the add, and the shop's own goods are not changed by the natives. Close: a sold-out good is not restocked, and the slots of a card limit nothing here.

    const string StockFact = "game-facts.md, Orders and events: the stock natives (11-08); probe items-stock (11-21)";
    const double StockOpens = 2.05;
    static readonly int SellItems = Code("Asid");

    void Stock(unit? shop, int itemId, int current, int stockMax)
    {
        if (shop is not null && Find(shop, SellItems) is not null) shop.Stocked[itemId] = (Math.Clamp(Math.Min(current, stockMax), 0, int.MaxValue), stockMax, Math.Max(Seconds, StockOpens));
    }

    IEnumerable<unit> Shops() => _units.Values.Where(u => !u.Removed && Find(u, SellItems) is not null).ToList();

    // An added good that is on sale now
    bool OnSale(unit shop, int itemId) => shop.Stocked.TryGetValue(itemId, out var added) && added.Stock > 0 && Seconds >= added.Open;

    /// <summary>Adds the good to the shop's stock, if the unit has Sell Items; it goes on sale 2.05 s into the game or at the add, as many times as its stock says.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events: the stock natives (11-08); probe items-stock (11-21)); a sold-out good is not restocked, and the slots of a shop's card limit nothing here.</remarks>
    [Native(Level.Close, Fact = StockFact)]
    public void AddItemToStock(unit? whichUnit, int itemId, int currentStock, int stockMax) => Stock(whichUnit, itemId, currentStock, stockMax);

    /// <summary>Adds the good to the stock of every shop, as AddItemToStock does for one.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events: the stock natives (11-08); probe items-stock (11-21)); a sold-out good is not restocked, and the slots of a shop's card limit nothing here.</remarks>
    [Native(Level.Close, Fact = StockFact)]
    public void AddItemToAllStock(int itemId, int currentStock, int stockMax)
    {
        foreach (var shop in Shops()) Stock(shop, itemId, currentStock, stockMax);
    }

    /// <summary>Takes the good out of the shop's stock; nothing for a null unit.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events: the stock natives (11-08); probe items-stock (11-21)); a sold-out good is not restocked, and the slots of a shop's card limit nothing here.</remarks>
    [Native(Level.Close, Fact = StockFact)]
    public void RemoveItemFromStock(unit? whichUnit, int itemId) => whichUnit?.Stocked.Remove(itemId);

    /// <summary>Takes the good out of the stock of every shop.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Orders and events: the stock natives (11-08); probe items-stock (11-21)); a sold-out good is not restocked, and the slots of a shop's card limit nothing here.</remarks>
    [Native(Level.Close, Fact = StockFact)]
    public void RemoveItemFromAllStock(int itemId)
    {
        foreach (var shop in Shops()) shop.Stocked.Remove(itemId);
    }

    const string UnitStock = "A unit added to a shop's stock showed on its card (probe-shop-unit-slots) but was bought by no order (IssueNeutralImmediateOrderById on a neutral shop, IssueImmediateOrderById on a player's Farm with Sell Units: probe items-stock), so no native reads it back (D796)";
    const string SlotStock = "The slots of a shop's card limit the goods it shows (11 units on a player's shop, probe-shop-unit-slots), not the goods it sells: with slots 0, 1, 2, 11 and 12, set before and after the shop was made, a shop sold all four goods added (probe items-stock-slots, D796)";

    /// <summary>Changes nothing; the unit isn't added to any stock.</summary>
    /// <remarks>Recorded: A unit added to a shop's stock showed on its card (probe-shop-unit-slots) but was bought by no order (IssueNeutralImmediateOrderById on a neutral shop, IssueImmediateOrderById on a player's Farm with Sell Units: probe items-stock), so no native reads it back (D796).</remarks>
    [Native(Level.Recorded, Reason = UnitStock)] public void AddUnitToAllStock(int unitId, int currentStock, int stockMax) { }
    /// <summary>Changes nothing; the unit isn't added to any stock.</summary>
    /// <remarks>Recorded: A unit added to a shop's stock showed on its card (probe-shop-unit-slots) but was bought by no order (IssueNeutralImmediateOrderById on a neutral shop, IssueImmediateOrderById on a player's Farm with Sell Units: probe items-stock), so no native reads it back (D796).</remarks>
    [Native(Level.Recorded, Reason = UnitStock)] public void AddUnitToStock(unit? whichUnit, int unitId, int currentStock, int stockMax) { }
    /// <summary>Changes nothing; no unit stock is kept to remove it from.</summary>
    /// <remarks>Recorded: A unit added to a shop's stock showed on its card (probe-shop-unit-slots) but was bought by no order (IssueNeutralImmediateOrderById on a neutral shop, IssueImmediateOrderById on a player's Farm with Sell Units: probe items-stock), so no native reads it back (D796).</remarks>
    [Native(Level.Recorded, Reason = UnitStock)] public void RemoveUnitFromAllStock(int unitId) { }
    /// <summary>Changes nothing; no unit stock is kept to remove it from.</summary>
    /// <remarks>Recorded: A unit added to a shop's stock showed on its card (probe-shop-unit-slots) but was bought by no order (IssueNeutralImmediateOrderById on a neutral shop, IssueImmediateOrderById on a player's Farm with Sell Units: probe items-stock), so no native reads it back (D796).</remarks>
    [Native(Level.Recorded, Reason = UnitStock)] public void RemoveUnitFromStock(unit? whichUnit, int unitId) { }
    /// <summary>Changes nothing; the slots of a shop's card aren't kept.</summary>
    /// <remarks>Recorded: The slots of a shop's card limit the goods it shows (11 units on a player's shop, probe-shop-unit-slots), not the goods it sells: with slots 0, 1, 2, 11 and 12, set before and after the shop was made, a shop sold all four goods added (probe items-stock-slots, D796).</remarks>
    [Native(Level.Recorded, Reason = SlotStock)] public void SetAllItemTypeSlots(int slots) { }
    /// <summary>Changes nothing; the slots of a shop's card aren't kept.</summary>
    /// <remarks>Recorded: The slots of a shop's card limit the goods it shows (11 units on a player's shop, probe-shop-unit-slots), not the goods it sells: with slots 0, 1, 2, 11 and 12, set before and after the shop was made, a shop sold all four goods added (probe items-stock-slots, D796).</remarks>
    [Native(Level.Recorded, Reason = SlotStock)] public void SetAllUnitTypeSlots(int slots) { }
    /// <summary>Changes nothing; the slots of a shop's card aren't kept.</summary>
    /// <remarks>Recorded: The slots of a shop's card limit the goods it shows (11 units on a player's shop, probe-shop-unit-slots), not the goods it sells: with slots 0, 1, 2, 11 and 12, set before and after the shop was made, a shop sold all four goods added (probe items-stock-slots, D796).</remarks>
    [Native(Level.Recorded, Reason = SlotStock)] public void SetItemTypeSlots(unit? whichUnit, int slots) { }
    /// <summary>Changes nothing; the slots of a shop's card aren't kept.</summary>
    /// <remarks>Recorded: The slots of a shop's card limit the goods it shows (11 units on a player's shop, probe-shop-unit-slots), not the goods it sells: with slots 0, 1, 2, 11 and 12, set before and after the shop was made, a shop sold all four goods added (probe items-stock-slots, D796).</remarks>
    [Native(Level.Recorded, Reason = SlotStock)] public void SetUnitTypeSlots(unit? whichUnit, int slots) { }
}
