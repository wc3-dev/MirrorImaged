using MirrorImaged.Data;

namespace MirrorImaged.Natives;

public partial class unit
{
    internal readonly item?[] Bag = new item?[30];
    internal readonly item?[] Loadout = new item?[9]; // head, chest, gloves, boots, ring, second ring, primary, offhand, trinket
}

public partial class item
{
    internal int Where; // 0 in an inventory slot or none, 1 in the bag, 2 equipped
}

// The 3.0 equipment and bag (game-facts, "Items"; probes of 10-19 to 11-09): a unit with `AEqu` has nine equipment slots and one
// with `AIni` a bag of 30; an item whose data names an equipment type goes to the bag when given, equips into the slot of its type,
// and works from the equipment slots, not from the bag.
public sealed partial class GameNatives
{
    const string EquipFacts = "game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)";
    const int UnitEquip = 320, PlayerEquip = 321, UnitUnequip = 322, PlayerUnequip = 323;
    static readonly int EquipmentAbility = Code("AEqu"), BagAbility = Code("AIni"), ItemEquipment = Code("iequ");
    static readonly Dictionary<string, int> EquipmentWords = Words("none 0 head 1 chest 2 gloves 3 boots 4 ring 5 primary 6 offhand 7 trinket 8");

    int EquipmentOf(item i) => EquipmentWords.GetValueOrDefault(i.Type.Text(ItemEquipment) ?? "", 0);

    // The slot an equipment type of 1 to 8 goes in: the second ring's after the first's, the others one for one
    static int SlotOf(int type) => type switch { 5 => 4, >= 1 and <= 4 => type - 1, >= 6 and <= 8 => type, _ => -1 };

    bool HasBag(unit u) => Find(u, BagAbility) is not null;

    bool CanWear(unit u) => Find(u, EquipmentAbility) is not null;

    // The item out of the bag or the equipment slot it is in, its abilities with it; the slots of the inventory are Release's
    void Unplace(unit u, item i)
    {
        var bag = Array.IndexOf(u.Bag, i);
        if (bag >= 0) u.Bag[bag] = null;
        var worn = Array.IndexOf(u.Loadout, i);
        if (worn >= 0)
        {
            u.Loadout[worn] = null;
            Carry(u, () =>
            {
                foreach (var given in i.Given) Take(given);
                i.Given.Clear();
            });
        }
        i.Where = 0;
    }

    // The item in the bag's first free place, owned and hidden, the pick-up events firing with it in place
    bool Bagged(unit u, item i)
    {
        var place = Array.IndexOf(u.Bag, null);
        if (place < 0) return false;
        if (i.Holder is { } before) Take(before, i, announce: true);
        (u.Bag[place], i.Holder, i.Where) = (i, u, 1);
        FireUnit(new(u, Item: i), UnitPickupItem, PlayerPickupItem);
        return true;
    }

    // Wears the item in the slot of its type, or in the bag when the slots of the type are full
    bool Wear(unit u, item i)
    {
        var type = EquipmentOf(i);
        var slot = SlotOf(type);
        if (slot < 0 || !CanWear(u) || Live(i) is null) return false;
        if (u.Loadout[slot] is not null && type == 5 && u.Loadout[5] is null) slot = 5;
        if (u.Loadout[slot] is not null) return Bagged(u, i) || true;
        if (i.Holder is { } before) Take(before, i, announce: true);
        Carry(u, () =>
        {
            u.Loadout[slot] = i;
            (i.Holder, i.Where) = (u, 2);
            foreach (var type in AbilitiesOf(i)) i.Given.Add(Give(u, type, 0));
        });
        FireUnit(new(u, Item: i), UnitPickupItem, PlayerPickupItem);
        FireUnit(new(u, Item: i), UnitEquip, PlayerEquip);
        return true;
    }

    // The item to the bag, the drop, pick-up and unequip events firing
    bool Unwear(unit u, item i)
    {
        if (i.Where != 2 || i.Holder != u) return false;
        Take(u, i, announce: true);
        Bagged(u, i);
        FireUnit(new(u, Item: i), UnitUnequip, PlayerUnequip);
        return true;
    }

    /// <summary>30 for a unit with AIni (the extended inventory), 0 for any other unit (facts, 10-19).</summary>
    /// <remarks>Close: measured in part (game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)); modelled on the ability AIni alone, a bag from any other source is not measured.</remarks>
    [Native(Level.Close, Fact = EquipFacts)]
    public int UnitExtendedInventorySize(unit? whichUnit) => whichUnit is not null && HasBag(whichUnit) ? 30 : 0;

    /// <summary>As UnitExtendedInventorySize: 30 with AIni, else 0.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)); same model as UnitExtendedInventorySize, a bag from any other source is not measured.</remarks>
    [Native(Level.Close, Fact = EquipFacts)]
    public int BlzUnitExtendedInventorySize(unit? whichUnit) => UnitExtendedInventorySize(whichUnit);

    /// <summary>The equipment type of the item's data (<c>iequ</c>: head, chest, gloves, boots, ring, primary, offhand, trinket), 0 for none (probe: types 1 to 8).</summary>
    /// <remarks>Close: measured in part (game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)); read from the item's data, nil for a removed item is the model's and not measured in the game.</remarks>
    [Native(Level.Close, Fact = EquipFacts)]
    public equipmentType? GetItemEquipmentType(item? whichItem) => Live(whichItem) is { } i ? Convert<equipmentType>(EquipmentOf(i)) : null;

    /// <summary>As GetItemEquipmentType.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)); same as GetItemEquipmentType, nil for a removed item is not measured in the game.</remarks>
    [Native(Level.Close, Fact = EquipFacts)]
    public equipmentType? BlzGetItemEquipmentType(item? whichItem) => GetItemEquipmentType(whichItem);

    /// <summary>Close: the item tags aren't mapped; every item reads tag 0.</summary>
    /// <remarks>Close: no tag is mapped, every live item reads 0; the game's tags are not measured.</remarks>
    [Native(Level.Close)]
    public itemTag? GetItemTag(item? whichItem) => Live(whichItem) is null ? null : Convert<itemTag>(0);

    /// <summary>As GetItemTag: 0 for a live item, nil for a removed one.</summary>
    /// <remarks>Close: as GetItemTag, no tag is mapped; the game's tags are not measured.</remarks>
    [Native(Level.Close)]
    public itemTag? BlzGetItemTag(item? whichItem) => GetItemTag(whichItem);

    // False in each of 34,560 calls: every unit type of the data, with and without the equipment and bag abilities, for each equipment type 0 to 9 (probe
    // units-equip-types, D792), and for gloves while equipping them works (facts, 10-19)
    /// <summary>False for every unit and equipment type, as the game answered in every case measured.</summary>
    /// <remarks>Exact: matches the game wherever measured (probe units-equip-types (11-21, D792); game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)).</remarks>
    [Native(Level.Exact, Fact = "probe units-equip-types (11-21, D792); " + EquipFacts)]
    public bool UnitCanEquipItemOfEquipmentType(unit? whichUnit, equipmentType? equipmentId) => false;

    /// <summary>As UnitCanEquipItemOfEquipmentType: false in every case.</summary>
    /// <remarks>Exact: matches the game wherever measured (probe units-equip-types (11-21, D792); game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)).</remarks>
    [Native(Level.Exact, Fact = "probe units-equip-types (11-21, D792); " + EquipFacts)]
    public bool BlzUnitCanEquipItemOfEquipmentType(unit? whichUnit, equipmentType? equipmentId) => false;

    /// <summary>Puts the item on in the slot of its type, or in the bag when that slot is taken; false for a dead unit, one without AEqu, or an item with no equipment type.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)); the slot rules and the bag fall-back are modelled from the probes, a full bag is not measured.</remarks>
    [Native(Level.Close, Fact = EquipFacts)]
    public bool UnitEquipItem(unit? whichUnit, item? whichItem) => whichUnit is not null && !whichUnit.Dead && Live(whichItem) is { } i && Wear(whichUnit, i);

    /// <summary>As UnitEquipItem.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)); same as UnitEquipItem, a full bag is not measured.</remarks>
    [Native(Level.Close, Fact = EquipFacts)]
    public bool BlzUnitEquipItem(unit? whichUnit, item? whichItem) => UnitEquipItem(whichUnit, whichItem);

    /// <summary>Puts the item in the bag, with its drop, pick-up and unequip events; nothing for an item the unit doesn't wear.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)); the order of the events is modelled from the probes, a full bag is not measured.</remarks>
    [Native(Level.Close, Fact = EquipFacts)]
    public void UnitUnequipItem(unit? whichUnit, item? whichItem)
    {
        if (whichUnit is not null && Live(whichItem) is { } i) Unwear(whichUnit, i);
    }

    /// <summary>As UnitUnequipItem.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)); same as UnitUnequipItem, a full bag is not measured.</remarks>
    [Native(Level.Close, Fact = EquipFacts)]
    public void BlzUnitUnequipItem(unit? whichUnit, item? whichItem) => UnitUnequipItem(whichUnit, whichItem);

    /// <summary>The item taken off the slot, now in the bag; nil for an empty slot.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)); built on UnitUnequipItem, a slot out of range gives nil and is not measured in the game.</remarks>
    [Native(Level.Close, Fact = EquipFacts)]
    public item? UnitUnequipItemFromSlot(unit? whichUnit, loadoutslot? slot)
    {
        if (slot is null || UnitItemInEquipmentSlot(whichUnit, slot) is not { } i) return null;
        UnitUnequipItem(whichUnit, i);
        return i;
    }

    /// <summary>As UnitUnequipItemFromSlot.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)); same as UnitUnequipItemFromSlot, a slot out of range is not measured.</remarks>
    [Native(Level.Close, Fact = EquipFacts)]
    public item? BlzUnitUnequipItemFromSlot(unit? whichUnit, loadoutslot? slot) => UnitUnequipItemFromSlot(whichUnit, slot);

    /// <summary>True when this unit wears the item in an equipment slot; false for a null unit or a removed item.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)); read from the model's record of where the item is, not measured beyond the probes' equip and unequip.</remarks>
    [Native(Level.Close, Fact = EquipFacts)]
    public bool UnitHasItemEquipped(unit? whichUnit, item? whichItem) => whichUnit is not null && Live(whichItem) is { Where: 2 } i && i.Holder == whichUnit;

    /// <summary>As UnitHasItemEquipped.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)); same as UnitHasItemEquipped, not measured beyond the probes' equip and unequip.</remarks>
    [Native(Level.Close, Fact = EquipFacts)]
    public bool BlzUnitHasItemEquipped(unit? whichUnit, item? whichItem) => UnitHasItemEquipped(whichUnit, whichItem);

    /// <summary>True when the item is in this unit's bag; false for a null unit or a removed item.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)); read from the model's record of where the item is, not measured beyond the probes' bag results.</remarks>
    [Native(Level.Close, Fact = EquipFacts)]
    public bool UnitHasItemBagged(unit? whichUnit, item? whichItem) => whichUnit is not null && Live(whichItem) is { Where: 1 } i && i.Holder == whichUnit;

    /// <summary>As UnitHasItemBagged.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)); same as UnitHasItemBagged, not measured beyond the probes' bag results.</remarks>
    [Native(Level.Close, Fact = EquipFacts)]
    public bool BlzUnitHasItemBagged(unit? whichUnit, item? whichItem) => UnitHasItemBagged(whichUnit, whichItem);

    /// <summary>True when any of the nine equipment slots holds an item; false for a null unit.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)); read from the model's slots, not measured beyond the probes' equip and unequip.</remarks>
    [Native(Level.Close, Fact = EquipFacts)]
    public bool UnitHasAnyItemEquiped(unit? whichUnit) => whichUnit is not null && whichUnit.Loadout.Any(i => i is not null);

    /// <summary>As UnitHasAnyItemEquiped.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)); same as UnitHasAnyItemEquiped, not measured beyond the probes' equip and unequip.</remarks>
    [Native(Level.Close, Fact = EquipFacts)]
    public bool BlzUnitHasAnyItemEquipped(unit? whichUnit) => UnitHasAnyItemEquiped(whichUnit);

    /// <summary>True when a worn item has the equipment type; type 9 matches any worn item; false for a null unit or type.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)); type 9 as any worn item is the model's, not measured in the game.</remarks>
    [Native(Level.Close, Fact = EquipFacts)]
    public bool UnitHasItemEquipmentOfType(unit? whichUnit, equipmentType? equipmentId) =>
        whichUnit is not null && equipmentId is not null && whichUnit.Loadout.Any(i => i is not null && (equipmentId.Id == 9 || EquipmentOf(i) == equipmentId.Id));

    /// <summary>As UnitHasItemEquipmentOfType.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)); same as UnitHasItemEquipmentOfType, type 9 is not measured in the game.</remarks>
    [Native(Level.Close, Fact = EquipFacts)]
    public bool BlzUnitHasItemEquipmentOfType(unit? whichUnit, equipmentType? equipmentId) => UnitHasItemEquipmentOfType(whichUnit, equipmentId);

    /// <summary>True when the slot, 0 to 8, holds no item; false for a null unit or a slot out of range.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)); a slot out of range reads false, not measured in the game.</remarks>
    [Native(Level.Close, Fact = EquipFacts)]
    public bool UnitHasLoadoutSlotEmpty(unit? whichUnit, loadoutslot? itemSlot) => whichUnit is not null && itemSlot is { Id: >= 0 and < 9 } && whichUnit.Loadout[itemSlot.Id] is null;

    /// <summary>As UnitHasLoadoutSlotEmpty.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)); same as UnitHasLoadoutSlotEmpty, a slot out of range is not measured.</remarks>
    [Native(Level.Close, Fact = EquipFacts)]
    public bool BlzUnitHasLoadoutSlotEmpty(unit? whichUnit, loadoutslot? itemSlot) => UnitHasLoadoutSlotEmpty(whichUnit, itemSlot);

    /// <summary>The item in the bag place from 0, nil for an empty one and one out of range.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)); the bag's 30 places are modelled from the probes, a place out of range is not measured.</remarks>
    [Native(Level.Close, Fact = EquipFacts)]
    public item? UnitItemInBagSlot(unit? whichUnit, int itemSlot) => whichUnit is not null && itemSlot >= 0 && itemSlot < whichUnit.Bag.Length ? whichUnit.Bag[itemSlot] : null;

    /// <summary>As UnitItemInBagSlot.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)); same as UnitItemInBagSlot, a place out of range is not measured.</remarks>
    [Native(Level.Close, Fact = EquipFacts)]
    public item? BlzUnitItemInBagSlot(unit? whichUnit, int itemSlot) => UnitItemInBagSlot(whichUnit, itemSlot);

    /// <summary>The item worn in the slot (0 head, 1 chest, 2 gloves, 3 boots, 4 and 5 rings, 6 primary, 7 offhand, 8 trinket).</summary>
    /// <remarks>Close: measured in part (game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)); a slot out of range gives nil, not measured in the game.</remarks>
    [Native(Level.Close, Fact = EquipFacts)]
    public item? UnitItemInEquipmentSlot(unit? whichUnit, loadoutslot? itemSlot) =>
        whichUnit is not null && itemSlot is { Id: >= 0 and < 9 } ? whichUnit.Loadout[itemSlot.Id] : null;

    /// <summary>As UnitItemInEquipmentSlot.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)); same as UnitItemInEquipmentSlot, a slot out of range is not measured.</remarks>
    [Native(Level.Close, Fact = EquipFacts)]
    public item? BlzUnitItemInEquipmentSlot(unit? whichUnit, loadoutslot? itemSlot) => UnitItemInEquipmentSlot(whichUnit, itemSlot);

    /// <summary>The item of an equip event: the item put on; nil in any other event (the unequip event names none).</summary>
    /// <remarks>Close: measured in part (game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)); nil outside an equip event is the model's, not measured in the game.</remarks>
    [Native(Level.Close, Fact = EquipFacts)]
    public item? GetEquippedItem() => Response(UnitEquip, PlayerEquip)?.Item;

    /// <summary>As GetEquippedItem.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)); same as GetEquippedItem, nil outside an equip event is not measured.</remarks>
    [Native(Level.Close, Fact = EquipFacts)]
    public item? BlzGetEquippedItem() => GetEquippedItem();

    /// <summary>The item of an unequip event: the item taken off; nil in any other event.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)); nil outside an unequip event is the model's, not measured in the game.</remarks>
    [Native(Level.Close, Fact = EquipFacts)]
    public item? GetUnequippedItem() => Response(UnitUnequip, PlayerUnequip)?.Item;

    /// <summary>As GetUnequippedItem.</summary>
    /// <remarks>Close: measured in part (game-facts.md, Items: the 3.0 bag and equipment slots (10-28, 11-03, 11-08)); same as GetUnequippedItem, nil outside an unequip event is not measured.</remarks>
    [Native(Level.Close, Fact = EquipFacts)]
    public item? BlzGetUnequippedItem() => GetUnequippedItem();

    // The abilities an item was given and had taken away: kept by id, in the order added, for the natives below
    static List<int> AddedAbilities(item i) => i.Fields.TryGetValue(-2, out var added) && added is List<int> list ? list : (List<int>)(i.Fields[-2] = new List<int>());

    /// <summary>Adds the ability to the item's list; false for one it has already or one the data lacks. Close: no ability handle of an item is made.</summary>
    /// <remarks>Close: refuses an ability the item already has, as the game did (probe items-abilities, D794); no ability handle of an item is made.</remarks>
    [Native(Level.Close)]
    public bool BlzItemAddAbility(item? whichItem, int abilCode)
    {
        if (Live(whichItem) is not { } i || Data.Object(ObjectKind.Ability, abilCode) is null || AddedAbilities(i).Contains(abilCode)) return false;
        AddedAbilities(i).Add(abilCode);
        return true;
    }

    /// <summary>Takes the ability out of the list BlzItemAddAbility kept; false for one not in it.</summary>
    /// <remarks>Close: only an ability added by BlzItemAddAbility can be removed; one the item's data lists is not measured.</remarks>
    [Native(Level.Close)]
    public bool BlzItemRemoveAbility(item? whichItem, int abilCode) => Live(whichItem) is { } i && AddedAbilities(i).Remove(abilCode);

    // The abilities an item holds are the ones it put on its carrier, the unit's own handles in the order of the item's list; a free item holds none (probe
    // items-abilities, D794)
    /// <summary>The ability with that code the carrier holds from the item; nil for a free item or a code it holds none of.</summary>
    /// <remarks>Close: Only what the item's data lists and the carrier has from it; what BlzItemAddAbility adds isn't held (the game read none of those either, and refused the add of one it had).</remarks>
    [Native(Level.Close, Fact = "probe items-abilities (11-21, D794)", Reason = "Only what the item's data lists and the carrier has from it; what BlzItemAddAbility adds isn't held (the game read none of those either, and refused the add of one it had)")]
    public ability? BlzGetItemAbility(item? whichItem, int abilCode) => Live(whichItem)?.Given.Find(a => a.Type.Id == abilCode) is { } found ? Out(found) : null;

    /// <summary>The carrier's ability from the item at the index, in the order of the item's list; nil for a free item or an index out of range.</summary>
    /// <remarks>Close: Only what the item's data lists and the carrier has from it; what BlzItemAddAbility adds isn't held (the game read none of those either, and refused the add of one it had).</remarks>
    [Native(Level.Close, Fact = "probe items-abilities (11-21, D794)", Reason = "Only what the item's data lists and the carrier has from it; what BlzItemAddAbility adds isn't held (the game read none of those either, and refused the add of one it had)")]
    public ability? BlzGetItemAbilityByIndex(item? whichItem, int index) =>
        Live(whichItem) is { } i && index >= 0 && index < i.Given.Count ? Out(i.Given[index]) : null;
}
