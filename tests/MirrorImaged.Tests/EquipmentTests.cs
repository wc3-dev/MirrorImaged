using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>The 3.0 bag and equipment slots against the facts' "Items" (game-facts.md, 10-19, 10-28, 11-03, 11-08).</summary>
public sealed class EquipmentTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static StandardData Standard() => new StandardData()
        .Add(ObjectKind.Unit, "Hpal", Values("unam=Paladin;uhpm=100;ustr=22;uagi=13;uint=17;upra=STR;uaen=1"))
        .Add(ObjectKind.Ability, "AEqu", Values("alev=1"))
        .Add(ObjectKind.Ability, "AIni", Values("alev=1"))
        .Add(ObjectKind.Item, "erbg", Values("unam=Rusty Bronze Gauntlets;icla=Permanent;iequ=gloves"))
        .Add(ObjectKind.Item, "ring", Values("unam=Ring;icla=Permanent;iequ=ring"))
        .Add(ObjectKind.Item, "ratc", Values("unam=Claws of Attack;icla=Permanent"));

    List<string> Run(string main)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            local function hero(extra)
                local u = CreateUnit(Player(0), FourCC("Hpal"), 0, 0, 0)
                if extra then UnitAddAbility(u, FourCC("AEqu")) UnitAddAbility(u, FourCC("AIni")) end
                return u
            end
            local function item(id) return CreateItem(FourCC(id), 0, 0) end
            function config() end
            function main()
            {{main}}
            end
            """), "", data: Standard());
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];
    }

    // facts: AIni gives the 30-place bag; an item of an equipment type goes to the bag, a plain one to the inventory; a bagged item is owned and hidden but not in the inventory
    [Fact]
    public void Equipment_items_go_to_the_bag_of_a_unit_with_one()
    {
        var lines = Run("""
            local plain, h = hero(false), hero(true)
            print(UnitExtendedInventorySize(plain), UnitExtendedInventorySize(h), BlzUnitExtendedInventorySize(h))
            local gloves, claws = item("erbg"), item("ratc")
            print(UnitAddItem(h, gloves), UnitAddItem(h, claws))
            print(IsItemInBag(gloves), BlzIsItemInBag(claws), UnitHasItem(h, gloves), UnitHasItem(h, claws), UnitHasItemBagged(h, gloves), IsItemOwned(gloves), IsItemVisible(gloves))
            print(UnitItemInBagSlot(h, 0) == gloves, UnitItemInBagSlot(h, 1), UnitItemInSlot(h, 0) == claws)
            """);

        Assert.Equal(["0\t30\t30", "true\ttrue", "true\tfalse\tfalse\ttrue\ttrue\ttrue\tfalse", "true\tnil\ttrue"], lines);
    }

    // facts: equipping from the bag puts the item in the slot of its type, unequipping puts it back in the bag; the equip and unequip events
    [Fact]
    public void Equipping_moves_an_item_between_the_bag_and_its_slot_with_events()
    {
        var lines = Run("""
            local h = hero(true)
            local gloves = item("erbg")
            local t = CreateTrigger()
            TriggerRegisterPlayerUnitEvent(t, Player(0), EVENT_PLAYER_UNIT_EQUIP_ITEM, nil)
            TriggerAddAction(t, function() print("equip", GetEquippedItem() == gloves, GetManipulatedItem()) end)
            local u = CreateTrigger()
            TriggerRegisterPlayerUnitEvent(u, Player(0), EVENT_PLAYER_UNIT_UNEQUIP_ITEM, nil)
            TriggerAddAction(u, function() print("unequip", GetEquippedItem(), GetUnequippedItem() == gloves) end)
            UnitAddItem(h, gloves)
            print(UnitEquipItem(h, gloves), IsItemEquipped(gloves), IsItemInBag(gloves), UnitItemInEquipmentSlot(h, EQUIPMENT_LOADOUT_SLOT_GLOVES) == gloves)
            print(UnitHasItemEquipped(h, gloves), UnitHasAnyItemEquiped(h), UnitHasItemEquipmentOfType(h, EQUIPMENT_TYPE_GLOVES), UnitHasItemEquipmentOfType(h, EQUIPMENT_TYPE_BOOTS), UnitHasLoadoutSlotEmpty(h, EQUIPMENT_LOADOUT_SLOT_GLOVES), UnitHasLoadoutSlotEmpty(h, EQUIPMENT_LOADOUT_SLOT_HEAD))
            print(GetHandleId(GetItemEquipmentType(gloves)), UnitItemInBagSlot(h, 0))
            UnitUnequipItem(h, gloves)
            print(IsItemEquipped(gloves), IsItemInBag(gloves), UnitItemInBagSlot(h, 0) == gloves, UnitHasAnyItemEquiped(h))
            print(UnitUnequipItemFromSlot(h, EQUIPMENT_LOADOUT_SLOT_GLOVES))
            """);

        Assert.Equal([
            "equip\ttrue\tnil", "true\ttrue\tfalse\ttrue",
            "true\ttrue\ttrue\tfalse\tfalse\ttrue",
            "3\tnil",
            "unequip\tnil\ttrue", "false\ttrue\ttrue\tfalse",
            "nil",
        ], lines);
    }

    // facts: a first ring takes the first ring slot, a second the other, a third goes to the bag with no equip event
    [Fact]
    public void A_third_ring_goes_to_the_bag()
    {
        var lines = Run("""
            local h = hero(true)
            local a, b, c = item("ring"), item("ring"), item("ring")
            print(UnitEquipItem(h, a), UnitEquipItem(h, b), UnitEquipItem(h, c))
            print(UnitItemInEquipmentSlot(h, EQUIPMENT_LOADOUT_SLOT_RING) == a, UnitItemInEquipmentSlot(h, EQUIPMENT_LOADOUT_SLOT_RINGALT) == b, IsItemInBag(c), IsItemEquipped(c))
            print(UnitEquipItem(hero(false), item("erbg")), UnitEquipItem(h, item("ratc")))
            """);

        Assert.Equal(["true\ttrue\ttrue", "true\ttrue\ttrue\tfalse", "false\tfalse"], lines);
    }
}
