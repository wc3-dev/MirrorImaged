using MirrorImaged.Data;

namespace MirrorImaged.Natives;

public partial class item
{
    internal GameObject Type = null!;
    internal float X, Y, Life, MaxLife;
    internal int Charges, Level, UserData, Skin;
    internal player Owner = null!;
    internal unit? Holder; // the unit carrying it
    internal bool Visible = true, Invulnerable, Removed;
    internal string? Name, Tooltip, Description, Extended, Icon;
    internal readonly Dictionary<int, object> Fields = []; // what the Blz field natives and the flag setters set
}

// Items on the ground and in the inventory, as the items probe measures them (facts, "Items (W2-3)"). An item's
// data is its type's with its own changes on top; once removed it reads 0 or nil at once, as no unit does.
public sealed partial class GameNatives
{
    const string ItemProbe = "facts, Items (W2-3): probe items (11-15)";

    static readonly int ItemClass = Code("icla"), ItemLevel = Code("ilev"), ItemUses = Code("iuse"), ItemHitPoints = Code("ihtp"), ItemPowerup = Code("ipow"),
        ItemSellable = Code("isel"), ItemPawnable = Code("ipaw"), ItemPerishable = Code("iper"), ItemRandom = Code("iprn"), ItemUsable = Code("iusa"),
        ItemDroppable = Code("idro"), ItemDropped = Code("idrp"), ItemPriority = Code("ipri"), ItemArmor = Code("iarm"), ItemScale = Code("isca"),
        ItemCooldown = Code("icid"), ItemModel = Code("ifil"), ItemName = Code("unam"), ItemTip = Code("utip"), ItemDescription = Code("ides"),
        ItemExtended = Code("utub"), ItemIcon = Code("iico"), ItemCurrentHitPoints = Code("ihpc");

    static readonly Dictionary<string, int> ItemClasses = Words("permanent 0 charged 1 powerup 2 artifact 3 purchasable 4 campaign 5");

    const float ItemDying = 0.405f;

    readonly List<item> _items = []; // in the order made
    item? _enumItem; // EnumItemsInRect's, for its filter and its callback

    static item? Live(item? i) => i is { Removed: false } ? i : null;

    float LifeOf(item i) => Live(i)?.Life ?? 0;

    int Integer(item i, int field) => Own(i, field, i.Type.Integer(field));

    static T Own<T>(item i, int field, T otherwise) => i.Fields.TryGetValue(field, out var value) && value is T own ? own : otherwise;

    bool Flag(item i, int field) => Own(i, field, i.Type.Integer(field) != 0);

    item? NewItem(int id, float x, float y, int skin)
    {
        if (Data.Object(ObjectKind.Item, id) is not { } type) return null;
        var i = Make(new item
        {
            Type = type, X = x, Y = y, Skin = skin, Owner = Player(27)!, Life = type.Integer(ItemHitPoints), MaxLife = type.Integer(ItemHitPoints),
            Charges = type.Integer(ItemUses), Level = type.Integer(ItemLevel),
        });
        _items.Add(i);
        return i;
    }

    /// <summary>
    /// An item of the type at the place, alive, with its type's life, charges and level, the neutral passive player's
    /// (27); nil for an id that is no item's (a unit's, 0, unknown). One handle id (probe items). Close: the game keeps a
    /// place inside the map, here it is as given.
    /// </summary>
    /// <remarks>Close: the game keeps a place inside the map; here it is as given.</remarks>
    [Native(Level.Close)]
    public item? CreateItem(int itemid, float x, float y) => NewItem(itemid, x, y, itemid);

    /// <summary>As CreateItem, with the skin's id kept for BlzGetItemSkin and its name read as GetItemName (probe items: "Claws of Attack +12").</summary>
    /// <remarks>Close: the place is as given, as in CreateItem; the game keeping it inside the map is not modelled.</remarks>
    [Native(Level.Close)]
    public item? BlzCreateItemWithSkin(int itemid, float x, float y, int skinId) => NewItem(itemid, x, y, skinId);

    /// <summary>
    /// The item reads as nothing from this call on: type id 0, place 0, life 0, no charges and no player; its carrier
    /// loses it without an event (probe items: the reads were 0 in the call, which none of a unit's are).
    /// </summary>
    /// <remarks>Close: measured for the reads the probe made; the other getters of a removed item read 0 or nil by the same model, not measured.</remarks>
    [Native(Level.Close)]
    public void RemoveItem(item? whichItem)
    {
        if (Live(whichItem) is not { } i) return;
        if (i.Holder is { } holder) Take(holder, i);
        i.Removed = true;
        _items.Remove(i);
        _widgetEvents.Remove(i);
        _game.Remove(i.Key);
    }

    /// <summary>The id of the item's type; 0 for a null or removed item.</summary>
    /// <remarks>Close: 0 for a removed item is the model of the probe's reads, not measured for this getter alone.</remarks>
    [Native(Level.Close)]
    public int GetItemTypeId(item? i) => Live(i)?.Type.Id ?? 0;

    // A carried item is where its carrier is
    /// <summary>The item's x; its carrier's x while carried; 0 for a null or removed item.</summary>
    /// <remarks>Close: a carried item reading its carrier's place is the model's, not measured in the game.</remarks>
    [Native(Level.Close)]
    public float GetItemX(item? i) => Live(i) is { } item ? item.Holder?.X ?? item.X : 0;

    /// <summary>The item's y; its carrier's y while carried; 0 for a null or removed item.</summary>
    /// <remarks>Close: a carried item reading its carrier's place is the model's, not measured in the game.</remarks>
    [Native(Level.Close)]
    public float GetItemY(item? i) => Live(i) is { } item ? item.Holder?.Y ?? item.Y : 0;

    /// <summary>Moves the item; one carried is let go as the game does not: not measured. Close: the game keeps it inside the map (5000, -5000 read 2016, -2048).</summary>
    /// <remarks>Close: the game keeps the place inside the map (5000, -5000 read 2016, -2048); here it is as given.</remarks>
    [Native(Level.Close)]
    public void SetItemPosition(item? i, float x, float y)
    {
        if (Live(i) is { } item) (item.X, item.Y) = (x, y);
    }

    /// <summary>The name BlzSetItemName gave this item, else its skin's, else its type's (probe items).</summary>
    /// <remarks>Close: modelled on the probe's reads; a skin with no name falling to the type's is not measured in the game.</remarks>
    [Native(Level.Close)]
    public string? GetItemName(item? whichItem) => Live(whichItem) is { } i ? i.Name ?? Data.Name(i.Skin) ?? i.Type.Text(ItemName) ?? "" : "";

    /// <summary>Keeps the name for GetItemName; nothing for a removed item or a null name.</summary>
    /// <remarks>Close: kept on the item and read back by GetItemName; whether the game also changes other readers is not measured.</remarks>
    [Native(Level.Close)]
    public void BlzSetItemName(item? whichItem, string? name)
    {
        if (Live(whichItem) is { } i && name is not null) i.Name = name;
    }

    /// <summary>The tooltip set on the item, else its type's; empty for a null or removed item.</summary>
    /// <remarks>Close: read from the item's data and its own set, not measured in the game.</remarks>
    [Native(Level.Close)]
    public string? BlzGetItemTooltip(item? whichItem) => Live(whichItem) is { } i ? i.Tooltip ?? i.Type.Text(ItemTip) ?? "" : "";

    /// <summary>Keeps the tooltip for BlzGetItemTooltip; nothing for a removed item or a null text.</summary>
    /// <remarks>Close: kept on the item, not measured in the game beyond the read-back.</remarks>
    [Native(Level.Close)]
    public void BlzSetItemTooltip(item? whichItem, string? tooltip)
    {
        if (Live(whichItem) is { } i && tooltip is not null) i.Tooltip = tooltip;
    }

    /// <summary>The description set on the item, else its type's; empty for a null or removed item.</summary>
    /// <remarks>Close: read from the item's data and its own set, not measured in the game.</remarks>
    [Native(Level.Close)]
    public string? BlzGetItemDescription(item? whichItem) => Live(whichItem) is { } i ? i.Description ?? i.Type.Text(ItemDescription) ?? "" : "";

    /// <summary>Keeps the description for BlzGetItemDescription; nothing for a removed item or a null text.</summary>
    /// <remarks>Close: kept on the item, not measured in the game beyond the read-back.</remarks>
    [Native(Level.Close)]
    public void BlzSetItemDescription(item? whichItem, string? description)
    {
        if (Live(whichItem) is { } i && description is not null) i.Description = description;
    }

    /// <summary>The extended tooltip with its tags (<c>&lt;AIh1,DataA1&gt;</c>) filled: "Heals 250 hit points when used." (probe items).</summary>
    /// <remarks>Close: measured in part (game-facts.md, Spells and abilities: tooltip tags and ParseTags (10-29, 11-08); MirrorImaged probe data-tags); the tags are filled by ParseTags, whose columns are measured in part.</remarks>
    [Native(Level.Close, Fact = TagFacts)]
    public string? BlzGetItemExtendedTooltip(item? whichItem) => ParseTags(Live(whichItem) is { } i ? i.Extended ?? i.Type.Text(ItemExtended) ?? "" : "");

    /// <summary>Keeps the extended tooltip for BlzGetItemExtendedTooltip; nothing for a removed item or a null text.</summary>
    /// <remarks>Close: kept on the item with its tags unfilled, not measured in the game beyond the read-back.</remarks>
    [Native(Level.Close)]
    public void BlzSetItemExtendedTooltip(item? whichItem, string? extendedTooltip)
    {
        if (Live(whichItem) is { } i && extendedTooltip is not null) i.Extended = extendedTooltip;
    }

    /// <summary>The icon path set on the item, else its type's; empty for a null or removed item.</summary>
    /// <remarks>Close: read from the item's data and its own set, not measured in the game.</remarks>
    [Native(Level.Close)]
    public string? BlzGetItemIconPath(item? whichItem) => Live(whichItem) is { } i ? i.Icon ?? i.Type.Text(ItemIcon) ?? "" : "";

    /// <summary>Keeps the icon path for BlzGetItemIconPath; nothing for a removed item or a null path.</summary>
    /// <remarks>Close: kept on the item, not measured in the game beyond the read-back.</remarks>
    [Native(Level.Close)]
    public void BlzSetItemIconPath(item? whichItem, string? iconPath)
    {
        if (Live(whichItem) is { } i && iconPath is not null) i.Icon = iconPath;
    }

    /// <summary>The skin id kept: the type's id from CreateItem, the given one from BlzCreateItemWithSkin; 0 for a null or removed item.</summary>
    /// <remarks>Close: kept as given, not measured in the game beyond the probe's name read.</remarks>
    [Native(Level.Close)]
    public int BlzGetItemSkin(item? whichItem) => Live(whichItem)?.Skin ?? 0;

    /// <summary>Keeps the skin id, whose name GetItemName reads when none is set; nothing for a removed item.</summary>
    /// <remarks>Close: kept as given, the game's change of the item's look is not measured.</remarks>
    [Native(Level.Close)]
    public void BlzSetItemSkin(item? whichItem, int skinId)
    {
        if (Live(whichItem) is { } i) i.Skin = skinId;
    }

    /// <summary>The item's charges: its type's uses until set; 0 for a null or removed item.</summary>
    /// <remarks>Close: modelled on the items probe; a case it did not reach is not measured.</remarks>
    [Native(Level.Close)]
    public int GetItemCharges(item? whichItem) => Live(whichItem)?.Charges ?? 0;

    /// <summary>Kept from 0 up (-2 reads 0, 7 reads 7); at 0 a perishable item is not removed by this (probe items).</summary>
    /// <remarks>Close: -2 read 0 and 7 read 7 in the probe; any larger or other value is not measured.</remarks>
    [Native(Level.Close)]
    public void SetItemCharges(item? whichItem, int charges)
    {
        if (Live(whichItem) is { } i) i.Charges = Math.Max(charges, 0);
    }

    /// <summary>The item's level: its type's until a set changed it; 0 for a null or removed item.</summary>
    /// <remarks>Close: modelled on the items probe; a case it did not reach is not measured.</remarks>
    [Native(Level.Close)]
    public int GetItemLevel(item? whichItem) => Live(whichItem)?.Level ?? 0;

    /// <summary>The type's class as common.j numbers it: permanent 0, charged 1, power up 2, artifact 3, purchasable 4, campaign 5, any other 8 (the data's "Miscellaneous" read 8: probe items).</summary>
    /// <remarks>Close: the six class words follow common.j's numbering; 8 for any other is measured only for "Miscellaneous".</remarks>
    [Native(Level.Close)]
    public itemtype? GetItemType(item? whichItem) =>
        Live(whichItem) is { } i ? Convert<itemtype>(ItemClasses.GetValueOrDefault(i.Type.Text(ItemClass) ?? "", 8)) : null;

    /// <summary>True when the item type's data marks it a power up; false for an id that is no item's.</summary>
    /// <remarks>Close: read from the type's data, not measured against the game for every type.</remarks>
    [Native(Level.Close)]
    public bool IsItemIdPowerup(int itemId) => Data.Object(ObjectKind.Item, itemId)?.Integer(ItemPowerup) is > 0;

    /// <summary>True when the item type's data marks it sellable; false for an id that is no item's.</summary>
    /// <remarks>Close: read from the type's data, not measured against the game for every type.</remarks>
    [Native(Level.Close)]
    public bool IsItemIdSellable(int itemId) => Data.Object(ObjectKind.Item, itemId)?.Integer(ItemSellable) is > 0;

    /// <summary>True when the item type's data marks it pawnable; false for an id that is no item's.</summary>
    /// <remarks>Close: read from the type's data, not measured against the game for every type.</remarks>
    [Native(Level.Close)]
    public bool IsItemIdPawnable(int itemId) => Data.Object(ObjectKind.Item, itemId)?.Integer(ItemPawnable) is > 0;

    /// <summary>The player of the item: the neutral passive player's (27) whoever carries it, until SetItemPlayer (probe items); nil once removed.</summary>
    /// <remarks>Close: modelled on the items probe; a case it did not reach is not measured.</remarks>
    [Native(Level.Close)]
    public player? GetItemPlayer(item? whichItem) => Live(whichItem)?.Owner;

    /// <summary>Gives the item to the player; whether the colour changes, and whether the item is then owned, are not what the flag says: it reads not owned (probe items).</summary>
    /// <remarks>Close: the colour flag changes nothing here; the game's change of colour is not measured.</remarks>
    [Native(Level.Close)]
    public void SetItemPlayer(item? whichItem, player? whichPlayer, bool changeColor)
    {
        if (Live(whichItem) is { } i && whichPlayer is not null) i.Owner = whichPlayer;
    }

    /// <summary>The number SetItemUserData kept; 0 until set, and for a null or removed item.</summary>
    /// <remarks>Close: kept per item, not measured in the game beyond the read-back.</remarks>
    [Native(Level.Close)]
    public int GetItemUserData(item? whichItem) => Live(whichItem)?.UserData ?? 0;

    /// <summary>Keeps the number for GetItemUserData; nothing for a removed item.</summary>
    /// <remarks>Close: kept per item, not measured in the game beyond the read-back.</remarks>
    [Native(Level.Close)]
    public void SetItemUserData(item? whichItem, int data)
    {
        if (Live(whichItem) is { } i) i.UserData = data;
    }

    /// <summary>Whether a unit carries it; not whether its player is another's (probe items: set to player 2 it read false).</summary>
    /// <remarks>Close: read as carried by a unit; the game's own notion beyond the probe is not measured.</remarks>
    [Native(Level.Close)]
    public bool IsItemOwned(item? whichItem) => Live(whichItem)?.Holder is not null;

    /// <summary>True when the item is shown and no unit carries it; false for a null or removed item.</summary>
    /// <remarks>Close: modelled on the items probe; a case it did not reach is not measured.</remarks>
    [Native(Level.Close)]
    public bool IsItemVisible(item? whichItem) => Live(whichItem) is { } i && i.Visible && i.Holder is null;

    /// <summary>Shows or hides the item, as IsItemVisible reads; nothing for a removed item.</summary>
    /// <remarks>Close: kept as a flag, the game's drawing of it is not measured.</remarks>
    [Native(Level.Close)]
    public void SetItemVisible(item? whichItem, bool show)
    {
        if (Live(whichItem) is { } i) i.Visible = show;
    }

    /// <summary>The flag SetItemInvulnerable kept; false for a null or removed item.</summary>
    /// <remarks>Close: kept per item, not measured in the game beyond the read-back.</remarks>
    [Native(Level.Close)]
    public bool IsItemInvulnerable(item? whichItem) => Live(whichItem)?.Invulnerable ?? false;

    /// <summary>Keeps the flag for IsItemInvulnerable; nothing for a removed item.</summary>
    /// <remarks>Close: kept per item; an invulnerable item still dies below the dying life, as the probe read.</remarks>
    [Native(Level.Close)]
    public void SetItemInvulnerable(item? whichItem, bool flag)
    {
        if (Live(whichItem) is { } i) i.Invulnerable = flag;
    }

    /// <summary>The item's power up flag: its type's until a field set changed it; false for a null or removed item.</summary>
    /// <remarks>Close: modelled on the items probe; a case it did not reach is not measured.</remarks>
    [Native(Level.Close)]
    public bool IsItemPowerup(item? whichItem) => Live(whichItem) is { } i && Flag(i, ItemPowerup);

    /// <summary>The item's sellable flag: its type's until a field set changed it; false for a null or removed item.</summary>
    /// <remarks>Close: modelled on the items probe; a case it did not reach is not measured.</remarks>
    [Native(Level.Close)]
    public bool IsItemSellable(item? whichItem) => Live(whichItem) is { } i && Flag(i, ItemSellable);

    /// <summary>The item's own flag: what SetItemPawnable set, not the type's (IsItemIdPawnable still reads true; probe items).</summary>
    /// <remarks>Close: modelled on the items probe; a case it did not reach is not measured.</remarks>
    [Native(Level.Close)]
    public bool IsItemPawnable(item? whichItem) => Live(whichItem) is { } i && Flag(i, ItemPawnable);

    /// <summary>Keeps the item's own pawnable flag, which IsItemPawnable reads; the type's is left as it was.</summary>
    /// <remarks>Close: kept on the item only; what a shop does with it is not measured.</remarks>
    [Native(Level.Close)]
    public void SetItemPawnable(item? i, bool flag)
    {
        if (Live(i) is { } item) item.Fields[ItemPawnable] = flag;
    }

    /// <summary>Keeps the drop-on-death flag as the item's own field, which BlzGetItemBooleanField reads.</summary>
    /// <remarks>Close: kept only; whether a carrier's death uses it is not measured.</remarks>
    [Native(Level.Close)]
    public void SetItemDropOnDeath(item? whichItem, bool flag)
    {
        if (Live(whichItem) is { } i) i.Fields[ItemDropped] = flag;
    }

    /// <summary>Keeps the droppable flag as the item's own field, which BlzGetItemBooleanField reads.</summary>
    /// <remarks>Close: kept only; whether a unit's drop order uses it is not measured.</remarks>
    [Native(Level.Close)]
    public void SetItemDroppable(item? i, bool flag)
    {
        if (Live(i) is { } item) item.Fields[ItemDroppable] = flag;
    }

    /// <summary>What the item's carrier drops when it dies instead (the drop id): kept, nothing reads it yet.</summary>
    /// <remarks>Close: kept only, nothing reads it; the game's use of it on death is not measured.</remarks>
    [Native(Level.Close)]
    public void SetItemDropID(item? whichItem, int unitId)
    {
        if (Live(whichItem) is { } i) i.Fields[-1] = unitId;
    }

    /// <summary>True for an item in a unit's bag (facts: inside the pick-up event into it too).</summary>
    /// <remarks>Close: read from where the model keeps the item; not measured beyond the bag facts the equipment natives cite.</remarks>
    [Native(Level.Close)]
    public bool IsItemInBag(item? whichItem) => Live(whichItem) is { Where: 1, Holder: not null };

    /// <summary>True for an item a unit wears in an equipment slot; false for a null or removed item.</summary>
    /// <remarks>Close: read from where the model keeps the item; not measured beyond the equipment facts the equipment natives cite.</remarks>
    [Native(Level.Close)]
    public bool IsItemEquipped(item? whichItem) => Live(whichItem) is { Where: 2, Holder: not null };

    /// <summary>As IsItemInBag.</summary>
    /// <remarks>Close: same as IsItemInBag, not measured beyond the bag facts the equipment natives cite.</remarks>
    [Native(Level.Close)]
    public bool BlzIsItemInBag(item? whichItem) => IsItemInBag(whichItem);

    /// <summary>As IsItemEquipped.</summary>
    /// <remarks>Close: same as IsItemEquipped, not measured beyond the equipment facts the equipment natives cite.</remarks>
    [Native(Level.Close)]
    public bool BlzIsItemEquipped(item? whichItem) => IsItemEquipped(whichItem);

    // ---- Life and death

    // Life is kept from 0 to its most (500 reads the most, 75); below 0.405 the item dies: life 0, the widget death
    // event inside the call, and it stays (also an invulnerable one) (probe items)
    void SetItemLife(item i, float life)
    {
        if (Live(i) is null) return;
        var old = i.Life;
        i.Life = Math.Min(life, i.MaxLife);
        if (i.Life >= ItemDying) return;
        i.Life = 0;
        if (old <= 0) return;
        foreach (var (kind, registered) in _widgetEvents.GetValueOrDefault(i)?.ToArray() ?? [])
            if (kind == WidgetDeath) Fire(registered, Convert<widgetevent>(kind), item: i);
    }

    // ---- Fields

    /// <summary>
    /// The item's integer field: its level, charges, life now and most, cooldown group (the id of its cooldown ability),
    /// priority, armour type and tint (255 whole), unless a set changed it (probe items).
    /// </summary>
    /// <remarks>Close: the fields listed are modelled on the probe; any other field reads the type's value, not measured in the game.</remarks>
    [Native(Level.Close)]
    public int BlzGetItemIntegerField(item? whichItem, itemintegerfield? whichField)
    {
        if (Live(whichItem) is not { } i || whichField is null) return 0;
        var field = whichField.Id;
        return field == ItemLevel ? i.Level
            : field == ItemUses ? i.Charges
            : field == ItemCurrentHitPoints ? (int)i.Life
            : field == ItemHitPoints ? (int)i.MaxLife
            : field == ItemCooldown ? Ids.Of(i.Type.Text(field) ?? "")
            : field == ItemArmor ? ArmorTypes.GetValueOrDefault(i.Type.Text(field) ?? "")
            : field is 1768120684 or 1768123490 or 1768123495 or 1768123506 ? 255
            : Integer(i, field);
    }

    /// <summary>Sets the item's level, charges, life now or most, or keeps another field for BlzGetItemIntegerField; true for a live item, false for a null one.</summary>
    /// <remarks>Close: answers true; the game's answer is not measured, as its boolean and real sets answered false.</remarks>
    [Native(Level.Close)]
    public bool BlzSetItemIntegerField(item? whichItem, itemintegerfield? whichField, int value)
    {
        if (Live(whichItem) is not { } i || whichField is null) return false;
        var field = whichField.Id;
        if (field == ItemLevel) i.Level = value;
        else if (field == ItemUses) i.Charges = Math.Max(value, 0);
        else if (field == ItemCurrentHitPoints) i.Life = Math.Min(value, i.MaxLife);
        else if (field == ItemHitPoints) i.MaxLife = value;
        else i.Fields[field] = value;
        return true;
    }

    /// <summary>
    /// The item's boolean field, as its data code names it ('ipaw', pawnable, is the sold-to-merchants one; 'ipow', the
    /// powerup flag, is the use-automatically one): its type's flag unless set (probe items).
    /// </summary>
    /// <remarks>Close: modelled on the probe's reads; a flag it did not read is not measured.</remarks>
    [Native(Level.Close)]
    public bool BlzGetItemBooleanField(item? whichItem, itembooleanfield? whichField) =>
        Live(whichItem) is { } i && whichField is not null && Flag(i, whichField.Id);

    /// <summary>Sets the flag and answers false, as the real field does (probe items: a perishable item set false read false after false).</summary>
    /// <remarks>Close: measured for the perishable flag only; the other flags are set the same way, not measured.</remarks>
    [Native(Level.Close)]
    public bool BlzSetItemBooleanField(item? whichItem, itembooleanfield? whichField, bool value)
    {
        if (Live(whichItem) is { } i && whichField is not null) i.Fields[whichField.Id] = value;
        return false;
    }

    /// <summary>The value set for the field, else the type's scaling value whatever the field; 0 for a null or removed item.</summary>
    /// <remarks>Close: only the scaling field is measured; other real fields are not.</remarks>
    [Native(Level.Close)]
    public float BlzGetItemRealField(item? whichItem, itemrealfield? whichField) =>
        Live(whichItem) is { } i && whichField is not null ? Own(i, whichField.Id, i.Type.Real(ItemScale)) : 0;

    /// <summary>Keeps the value but answers false (probe items: the scaling value set to 2 read 2 after false).</summary>
    /// <remarks>Close: measured for the scaling field only; other real fields are set the same way, not measured.</remarks>
    [Native(Level.Close)]
    public bool BlzSetItemRealField(item? whichItem, itemrealfield? whichField, float value)
    {
        if (Live(whichItem) is { } i && whichField is not null) i.Fields[whichField.Id] = value;
        return false;
    }

    /// <summary>The string set for the field, else the type's model path whatever the field; empty for a null or removed item.</summary>
    /// <remarks>Close: only the model field is modelled; other string fields are not measured.</remarks>
    [Native(Level.Close)]
    public string? BlzGetItemStringField(item? whichItem, itemstringfield? whichField) =>
        Live(whichItem) is { } i && whichField is not null ? Own(i, whichField.Id, i.Type.Text(ItemModel) ?? "") : "";

    /// <summary>Keeps the string for the field and answers true; false for a null item, field or text.</summary>
    /// <remarks>Close: answers true; the game's answer is not measured, as its boolean and real sets answered false.</remarks>
    [Native(Level.Close)]
    public bool BlzSetItemStringField(item? whichItem, itemstringfield? whichField, string? value)
    {
        if (Live(whichItem) is not { } i || whichField is null || value is null) return false;
        i.Fields[whichField.Id] = value;
        return true;
    }

    // ---- Enumeration

    /// <summary>
    /// Every item not carried whose 32 cell lies in the rect (as EnumDestructablesInRect finds them), a hidden one
    /// too, each passed to the filter, then the callback, with GetFilterItem and GetEnumItem it; in squares of 256, the
    /// last made first in a square (probe items).
    /// </summary>
    /// <remarks>Close: the 32 cell test is taken over from EnumDestructablesInRect, not measured for items beyond the probe.</remarks>
    [Native(Level.Close)]
    public void EnumItemsInRect(rect? r, boolexpr? filter, Action? actionFunc)
    {
        var outer = _enumItem;
        try
        {
            if (r is null) return;
            var found = _items.Where(i => i.Holder is null && Inside(r, i.X, i.Y))
                .OrderBy(i => MathF.Floor(i.Y / 256)).ThenBy(i => MathF.Floor(i.X / 256)).ThenByDescending(i => i.Key).ToList();
            foreach (var i in found)
            {
                if (i.Removed) continue;
                _enumItem = i;
                if (filter is null || Holds(filter, alone: true)) actionFunc?.Invoke();
            }
        }
        finally
        {
            _enumItem = outer;
            Release(actionFunc);
        }
    }

    /// <summary>The item EnumItemsInRect is passing to its callback; nil outside one.</summary>
    /// <remarks>Close: nil outside an enumeration is the model's, not measured in the game.</remarks>
    [Native(Level.Close)]
    public item? GetEnumItem() => _enumItem;

    /// <summary>The item EnumItemsInRect is passing to its filter; nil outside one.</summary>
    /// <remarks>Close: nil outside an enumeration is the model's, not measured in the game.</remarks>
    [Native(Level.Close)]
    public item? GetFilterItem() => _enumItem;
}
