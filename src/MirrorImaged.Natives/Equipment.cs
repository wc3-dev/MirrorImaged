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
    [Native(Level.Close, Fact = EquipFacts)]
    public int UnitExtendedInventorySize(unit? whichUnit) => whichUnit is not null && HasBag(whichUnit) ? 30 : 0;

    [Native(Level.Close, Fact = EquipFacts)]
    public int BlzUnitExtendedInventorySize(unit? whichUnit) => UnitExtendedInventorySize(whichUnit);

    /// <summary>The equipment type of the item's data (<c>iequ</c>: head, chest, gloves, boots, ring, primary, offhand, trinket), 0 for none (probe: types 1 to 8).</summary>
    [Native(Level.Close, Fact = EquipFacts)]
    public equipmentType? GetItemEquipmentType(item? whichItem) => Live(whichItem) is { } i ? Convert<equipmentType>(EquipmentOf(i)) : null;

    [Native(Level.Close, Fact = EquipFacts)]
    public equipmentType? BlzGetItemEquipmentType(item? whichItem) => GetItemEquipmentType(whichItem);

    /// <summary>Close: the item tags aren't mapped; every item reads tag 0.</summary>
    [Native(Level.Close)]
    public itemTag? GetItemTag(item? whichItem) => Live(whichItem) is null ? null : Convert<itemTag>(0);

    [Native(Level.Close)]
    public itemTag? BlzGetItemTag(item? whichItem) => GetItemTag(whichItem);

    /// <summary>False for gloves while equipping them works (facts, 10-19): nothing here says true.</summary>
    [Native(Level.Close, Fact = EquipFacts)]
    public bool UnitCanEquipItemOfEquipmentType(unit? whichUnit, equipmentType? equipmentId) => false;

    [Native(Level.Close, Fact = EquipFacts)]
    public bool BlzUnitCanEquipItemOfEquipmentType(unit? whichUnit, equipmentType? equipmentId) => false;

    [Native(Level.Close, Fact = EquipFacts)]
    public bool UnitEquipItem(unit? whichUnit, item? whichItem) => whichUnit is not null && !whichUnit.Dead && Live(whichItem) is { } i && Wear(whichUnit, i);

    [Native(Level.Close, Fact = EquipFacts)]
    public bool BlzUnitEquipItem(unit? whichUnit, item? whichItem) => UnitEquipItem(whichUnit, whichItem);

    /// <summary>Puts the item in the bag, with its drop, pick-up and unequip events; nothing for an item the unit doesn't wear.</summary>
    [Native(Level.Close, Fact = EquipFacts)]
    public void UnitUnequipItem(unit? whichUnit, item? whichItem)
    {
        if (whichUnit is not null && Live(whichItem) is { } i) Unwear(whichUnit, i);
    }

    [Native(Level.Close, Fact = EquipFacts)]
    public void BlzUnitUnequipItem(unit? whichUnit, item? whichItem) => UnitUnequipItem(whichUnit, whichItem);

    /// <summary>The item taken off the slot, now in the bag; nil for an empty slot.</summary>
    [Native(Level.Close, Fact = EquipFacts)]
    public item? UnitUnequipItemFromSlot(unit? whichUnit, loadoutslot? slot)
    {
        if (slot is null || UnitItemInEquipmentSlot(whichUnit, slot) is not { } i) return null;
        UnitUnequipItem(whichUnit, i);
        return i;
    }

    [Native(Level.Close, Fact = EquipFacts)]
    public item? BlzUnitUnequipItemFromSlot(unit? whichUnit, loadoutslot? slot) => UnitUnequipItemFromSlot(whichUnit, slot);

    [Native(Level.Close, Fact = EquipFacts)]
    public bool UnitHasItemEquipped(unit? whichUnit, item? whichItem) => whichUnit is not null && Live(whichItem) is { Where: 2 } i && i.Holder == whichUnit;

    [Native(Level.Close, Fact = EquipFacts)]
    public bool BlzUnitHasItemEquipped(unit? whichUnit, item? whichItem) => UnitHasItemEquipped(whichUnit, whichItem);

    [Native(Level.Close, Fact = EquipFacts)]
    public bool UnitHasItemBagged(unit? whichUnit, item? whichItem) => whichUnit is not null && Live(whichItem) is { Where: 1 } i && i.Holder == whichUnit;

    [Native(Level.Close, Fact = EquipFacts)]
    public bool BlzUnitHasItemBagged(unit? whichUnit, item? whichItem) => UnitHasItemBagged(whichUnit, whichItem);

    [Native(Level.Close, Fact = EquipFacts)]
    public bool UnitHasAnyItemEquiped(unit? whichUnit) => whichUnit is not null && whichUnit.Loadout.Any(i => i is not null);

    [Native(Level.Close, Fact = EquipFacts)]
    public bool BlzUnitHasAnyItemEquipped(unit? whichUnit) => UnitHasAnyItemEquiped(whichUnit);

    [Native(Level.Close, Fact = EquipFacts)]
    public bool UnitHasItemEquipmentOfType(unit? whichUnit, equipmentType? equipmentId) =>
        whichUnit is not null && equipmentId is not null && whichUnit.Loadout.Any(i => i is not null && (equipmentId.Id == 9 || EquipmentOf(i) == equipmentId.Id));

    [Native(Level.Close, Fact = EquipFacts)]
    public bool BlzUnitHasItemEquipmentOfType(unit? whichUnit, equipmentType? equipmentId) => UnitHasItemEquipmentOfType(whichUnit, equipmentId);

    [Native(Level.Close, Fact = EquipFacts)]
    public bool UnitHasLoadoutSlotEmpty(unit? whichUnit, loadoutslot? itemSlot) => whichUnit is not null && itemSlot is { Id: >= 0 and < 9 } && whichUnit.Loadout[itemSlot.Id] is null;

    [Native(Level.Close, Fact = EquipFacts)]
    public bool BlzUnitHasLoadoutSlotEmpty(unit? whichUnit, loadoutslot? itemSlot) => UnitHasLoadoutSlotEmpty(whichUnit, itemSlot);

    /// <summary>The item in the bag place from 0, nil for an empty one and one out of range.</summary>
    [Native(Level.Close, Fact = EquipFacts)]
    public item? UnitItemInBagSlot(unit? whichUnit, int itemSlot) => whichUnit is not null && itemSlot >= 0 && itemSlot < whichUnit.Bag.Length ? whichUnit.Bag[itemSlot] : null;

    [Native(Level.Close, Fact = EquipFacts)]
    public item? BlzUnitItemInBagSlot(unit? whichUnit, int itemSlot) => UnitItemInBagSlot(whichUnit, itemSlot);

    /// <summary>The item worn in the slot (0 head, 1 chest, 2 gloves, 3 boots, 4 and 5 rings, 6 primary, 7 offhand, 8 trinket).</summary>
    [Native(Level.Close, Fact = EquipFacts)]
    public item? UnitItemInEquipmentSlot(unit? whichUnit, loadoutslot? itemSlot) =>
        whichUnit is not null && itemSlot is { Id: >= 0 and < 9 } ? whichUnit.Loadout[itemSlot.Id] : null;

    [Native(Level.Close, Fact = EquipFacts)]
    public item? BlzUnitItemInEquipmentSlot(unit? whichUnit, loadoutslot? itemSlot) => UnitItemInEquipmentSlot(whichUnit, itemSlot);

    /// <summary>The item of an equip event: the item put on; nil in any other event (the unequip event names none).</summary>
    [Native(Level.Close, Fact = EquipFacts)]
    public item? GetEquippedItem() => Response(UnitEquip, PlayerEquip)?.Item;

    [Native(Level.Close, Fact = EquipFacts)]
    public item? BlzGetEquippedItem() => GetEquippedItem();

    [Native(Level.Close, Fact = EquipFacts)]
    public item? GetUnequippedItem() => Response(UnitUnequip, PlayerUnequip)?.Item;

    [Native(Level.Close, Fact = EquipFacts)]
    public item? BlzGetUnequippedItem() => GetUnequippedItem();

    // The abilities an item was given and had taken away: kept by id, in the order added, for the natives below
    static List<int> AddedAbilities(item i) => i.Fields.TryGetValue(-2, out var added) && added is List<int> list ? list : (List<int>)(i.Fields[-2] = new List<int>());

    /// <summary>Adds the ability to the item's list; false for one it has already or one the data lacks. Close: no ability handle of an item is made.</summary>
    [Native(Level.Close)]
    public bool BlzItemAddAbility(item? whichItem, int abilCode)
    {
        if (Live(whichItem) is not { } i || Data.Object(ObjectKind.Ability, abilCode) is null || AddedAbilities(i).Contains(abilCode)) return false;
        AddedAbilities(i).Add(abilCode);
        return true;
    }

    [Native(Level.Close)]
    public bool BlzItemRemoveAbility(item? whichItem, int abilCode) => Live(whichItem) is { } i && AddedAbilities(i).Remove(abilCode);

    /// <summary>Close: the ability objects of items aren't made; nil.</summary>
    [Native(Level.Close)]
    public ability? BlzGetItemAbility(item? whichItem, int abilCode) => null;

    [Native(Level.Close)]
    public ability? BlzGetItemAbilityByIndex(item? whichItem, int index) => null;
}
