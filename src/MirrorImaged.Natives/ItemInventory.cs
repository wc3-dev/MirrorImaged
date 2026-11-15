namespace MirrorImaged.Natives;

public partial class unit
{
    internal readonly item?[] Slots = new item?[6];
}

// A hero's inventory: six slots, the item events inside the calls, powerups, as the items probe measures them
// (docs/facts.md, "Items (W2-3)"). The orders that need a unit to walk to a place (dropping at a point or to a unit),
// the abilities an item gives and the shops are later waves'.
public sealed partial class GameNatives
{
    const int UnitDropItem = 85, UnitPickupItem = 86, UnitUseItem_ = 87, PlayerDropItem = 48, PlayerPickupItem = 49, PlayerUseItem = 50;

    static int Size(unit u) => IsHero(u.Type.Id) ? 6 : 0;

    // How far south of its carrier an item lands when it is taken from a slot (probe items: -1400 reads -1424)
    const float Dropped = 24;

    /// <summary>Six for a hero, 0 for any other unit (probe items). Close: a unit with the inventory ability has the size its data gives.</summary>
    [Native(Level.Close)]
    public int UnitInventorySize(unit? whichUnit) => whichUnit is null ? 0 : Size(whichUnit);

    [Native(Level.Close)]
    public int UnitInventoryCount(unit? whichUnit) => whichUnit?.Slots.Count(i => i is not null) ?? 0;

    /// <summary>The item in the slot 0 to 5, nil for an empty one and for a slot out of range (probe items).</summary>
    [Native(Level.Close)]
    public item? UnitItemInSlot(unit? whichUnit, int itemSlot) =>
        whichUnit is not null && itemSlot >= 0 && itemSlot < Size(whichUnit) ? whichUnit.Slots[itemSlot] : null;

    [Native(Level.Close)]
    public bool UnitHasItem(unit? whichUnit, item? whichItem) => whichUnit is not null && Live(whichItem)?.Holder == whichUnit;

    // The item into the slot, owned by the unit and hidden; the pickup events fire with it in place
    void Put(unit u, item i, int slot)
    {
        if (i.Holder is { } before) Take(before, i, announce: true);
        u.Slots[slot] = i;
        i.Holder = u;
        FireUnit(new(u, Item: i), UnitPickupItem, PlayerPickupItem);
    }

    // The item out of its slot, back on the ground south of its carrier, the drop events firing first with it still in
    // its slot (probe items)
    void Take(unit u, item i, bool announce = false)
    {
        if (announce) FireUnit(new(u, Item: i), UnitDropItem, PlayerDropItem);
        var slot = Array.IndexOf(u.Slots, i);
        if (slot >= 0) u.Slots[slot] = null;
        (i.Holder, i.X, i.Y) = (null, u.X, u.Y - Dropped);
    }

    // A powerup is picked up and dropped at once and gone two steps on, its effect being the abilities' (W2-7)
    void Consume(unit u, item i)
    {
        FireUnit(new(u, Item: i), UnitPickupItem, PlayerPickupItem);
        FireUnit(new(u, Item: i), UnitDropItem, PlayerDropItem);
        _game.At(_game.Step + 2, () => RemoveItem(i));
    }

    bool Add(unit u, item i)
    {
        if (i.Type.Integer(ItemPowerup) != 0 && Size(u) > 0)
        {
            Consume(u, i);
            return true;
        }
        var slot = Array.IndexOf(u.Slots, null);
        if (slot < 0 || slot >= Size(u)) return false;
        Put(u, i, slot);
        return true;
    }

    /// <summary>
    /// Puts the item in the first free slot, taking it from the unit that has it with that unit's drop event, and fires
    /// the pickup events with it in place (the player's first); a powerup is picked up and dropped at once and gone two
    /// steps on. False for no room, and for a unit with no inventory (probe items).
    /// </summary>
    [Native(Level.Close)]
    public bool UnitAddItem(unit? whichUnit, item? whichItem) =>
        whichUnit is not null && Live(whichItem) is { } i && !whichUnit.Dead && Add(whichUnit, i);

    /// <summary>
    /// Makes the item at the unit's place and adds it: the item, or nil when there is no room, the item then lying where
    /// the unit is; a powerup is returned (probe items).
    /// </summary>
    [Native(Level.Close)]
    public item? UnitAddItemById(unit? whichUnit, int itemId)
    {
        if (whichUnit is null || CreateItem(itemId, whichUnit.X, whichUnit.Y) is not { } i) return null;
        return Add(whichUnit, i) ? i : null;
    }

    /// <summary>Makes the item and puts it in the slot if that is 0 to 5 and free; true if it did (probe items).</summary>
    [Native(Level.Close)]
    public bool UnitAddItemToSlotById(unit? whichUnit, int itemId, int itemSlot)
    {
        if (whichUnit is null || itemSlot < 0 || itemSlot >= Size(whichUnit) || whichUnit.Slots[itemSlot] is not null) return false;
        if (CreateItem(itemId, whichUnit.X, whichUnit.Y) is not { } i) return false;
        Put(whichUnit, i, itemSlot);
        return true;
    }

    /// <summary>Lets the item go, the drop events firing after it is out; nothing for an item the unit doesn't carry.</summary>
    [Native(Level.Close)]
    public void UnitRemoveItem(unit? whichUnit, item? whichItem)
    {
        if (whichUnit is not null && Live(whichItem) is { } i && i.Holder == whichUnit) Take(whichUnit, i, announce: true);
    }

    /// <summary>Takes the item in the slot out, as UnitRemoveItem, and gives it; nil for an empty slot or one out of range.</summary>
    [Native(Level.Close)]
    public item? UnitRemoveItemFromSlot(unit? whichUnit, int itemSlot)
    {
        if (UnitItemInSlot(whichUnit, itemSlot) is not { } i) return null;
        Take(whichUnit!, i, announce: true);
        return i;
    }

    /// <summary>Moves the unit's item to the slot, swapping with what is there, with no event; false for an item it doesn't carry or a slot out of range (probe items).</summary>
    [Native(Level.Close)]
    public bool UnitDropItemSlot(unit? whichUnit, item? whichItem, int slot)
    {
        if (whichUnit is null || Live(whichItem) is not { } i || i.Holder != whichUnit || slot < 0 || slot >= Size(whichUnit)) return false;
        var from = Array.IndexOf(whichUnit.Slots, i);
        (whichUnit.Slots[from], whichUnit.Slots[slot]) = (whichUnit.Slots[slot], i);
        return true;
    }

    /// <summary>
    /// Answers true for an item the unit carries and does nothing: the game walks the unit to the place and drops it
    /// there some time after (probe items), which comes with orders and movement (wave 3). Close.
    /// </summary>
    [Native(Level.Close)]
    public bool UnitDropItemPoint(unit? whichUnit, item? whichItem, float x, float y) => UnitHasItem(whichUnit, whichItem);

    /// <summary>As UnitDropItemPoint, to a widget.</summary>
    [Native(Level.Close)]
    public bool UnitDropItemTarget(unit? whichUnit, item? whichItem, widget? target) => UnitHasItem(whichUnit, whichItem) && target is not null;

    /// <summary>
    /// Uses a carried item: one charge less, the use event with it still in its slot, and an item out of charges that
    /// perishes dropped and gone after the step; the abilities an item gives are W2-7's (a potion's heal isn't done).
    /// </summary>
    [Native(Level.Close)]
    public bool UnitUseItem(unit? whichUnit, item? whichItem)
    {
        if (whichUnit is null || Live(whichItem) is not { } i || i.Holder != whichUnit) return false;
        if (i.Charges > 0) i.Charges--;
        FireUnit(new(whichUnit, Item: i), UnitUseItem_, PlayerUseItem);
        if (i.Charges == 0 && Flag(i, ItemPerishable) && i.Type.Integer(ItemUses) > 0)
        {
            Take(whichUnit, i, announce: true);
            _game.At(_game.Step + 2, () => RemoveItem(i));
        }
        return true;
    }

    /// <summary>The item of a pickup, drop or use event; nil outside one.</summary>
    [Native(Level.Close)]
    public item? GetManipulatedItem() => ItemResponse()?.Item;

    [Native(Level.Close)]
    public unit? GetManipulatingUnit() => ItemResponse()?.Unit;

    Happening? ItemResponse() => Response(UnitDropItem, UnitPickupItem, UnitUseItem_, PlayerDropItem, PlayerPickupItem, PlayerUseItem);

    /// <summary>Items don't stack here, so no stack event fires and these read nil and 0 (probe items: two potions stayed two).</summary>
    [Native(Level.Close)]
    public item? BlzGetStackingItemSource() => null;

    [Native(Level.Close)]
    public item? BlzGetStackingItemTarget() => null;

    [Native(Level.Close)]
    public int BlzGetStackingItemTargetPreviousCharges() => 0;
}
