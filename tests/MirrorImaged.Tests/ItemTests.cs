using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>
/// Items and inventories, against the probe items (facts, "Items (W2-3)"), on maps with the data the test gives
/// (decisions D207): the standard items the probe made.
/// </summary>
public sealed class ItemTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    // An item's data: the common values, those of the item on top
    static Dictionary<string, string> Item(string own)
    {
        var values = Values("ihtp=75;ilev=1;isel=1;ipaw=1;iper=0;ipow=0;iprn=0;iusa=0;idro=1;idrp=0;ipri=100;iarm=Wood;isca=1;ifil=Models/chest.mdl;icid=AIxx");
        foreach (var (key, value) in Values(own)) values[key] = value;
        return values;
    }

    static StandardData Standard()
    {
        var data = new StandardData()
            .Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=420"))
            .Add(ObjectKind.Unit, "Hpal", Values("unam=Paladin;uhpm=100;umpm=0;umpi=100;ustr=22;uagi=13;uint=17"))
            .Add(ObjectKind.Item, "phea", Item("unam=Potion of Healing;utip=Purchase Potion;ides=Restores life;utub=Heals when used;iico=Art/potion.blp;icla=Purchasable;iuse=1;iper=1;iusa=1"))
            .Add(ObjectKind.Item, "ratc", Item("unam=Claws of Attack +12;icla=Permanent;ilev=5;iprn=1"))
            .Add(ObjectKind.Item, "rag1", Item("unam=Ring of Agility;icla=Permanent;iprn=1"))
            .Add(ObjectKind.Item, "tdex", Item("unam=Tome of Agility;icla=PowerUp;iuse=1;ipow=1;isel=0;iprn=1"))
            .Add(ObjectKind.Item, "wneg", Item("unam=Wand of Negation;icla=Purchasable;iuse=3;iper=1;ilev=2;iusa=1"))
            .Add(ObjectKind.Item, "rej1", Item("unam=Replenishment;icla=Miscellaneous;ilev=2"));
        foreach (var (key, value) in Values("StrHitPointBonus=25;IntManaBonus=15;AgiDefenseBase=-2;AgiDefenseBonus=0.3"))
            data.Constants[key] = value;
        return data;
    }

    Simulation Run(string main) => Simulation.Load(_maps.Build($$"""
        local function show(v) return v == nil and "nil" or tostring(v) end
        local function kind(h) return h == nil and "nil" or tostring(h):match("^[%a_]+") end
        local function pid(p) return p == nil and "nil" or tostring(GetPlayerId(p)) end
        local function id(i)
            local n = GetItemTypeId(i)
            return n == 0 and "0" or string.char(n >> 24 & 255, n >> 16 & 255, n >> 8 & 255, n & 255)
        end
        local function UnitInventoryCount(u)
            local n = 0
            for i = 0, UnitInventorySize(u) - 1 do if UnitItemInSlot(u, i) then n = n + 1 end end
            return n
        end
        local function slots(u)
            local s = {}
            for i = 0, 5 do s[#s + 1] = id(UnitItemInSlot(u, i)) end
            return table.concat(s, ",") .. " count " .. UnitInventoryCount(u)
        end
        local function make(uid, x) return CreateUnit(Player(0), FourCC(uid), x or 0, 0, 0) end
        function config() end
        function main()
        {{main}}
        end
        """), "", data: Standard());

    static List<string> Printed(Simulation simulation) => [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..].Replace('\t', ' '))];

    // probe items: one handle id; the type's place, life, charges, level, class and flags; the neutral passive player's (27),
    // not owned; nil for a unit's id, 0 and unknown ids; the skin's name; an item read at once as nothing once removed
    [Fact]
    public void An_item_is_made_with_its_types_values_and_reads_nothing_once_removed()
    {
        using var simulation = Run("""
            local before = CreateTimer()
            local i = CreateItem(FourCC("phea"), 100.5, -200.25)
            local after = CreateTimer()
            print(GetHandleId(i) - GetHandleId(before), GetHandleId(after) - GetHandleId(i), kind(i))
            print(GetItemX(i), GetItemY(i), GetWidgetX(i), GetWidgetY(i), GetWidgetLife(i), pid(GetItemPlayer(i)), GetItemUserData(i), GetItemName(i))
            print(IsItemVisible(i), IsItemOwned(i), IsItemInvulnerable(i), IsItemPowerup(i), IsItemSellable(i), IsItemPawnable(i), IsItemInBag(i), IsItemEquipped(i))
            for _, n in ipairs({ "phea", "ratc", "tdex", "rej1" }) do
                local it = CreateItem(FourCC(n), 0, 0)
                print(n, GetItemCharges(it), GetItemLevel(it), GetHandleId(GetItemType(it)), IsItemIdPowerup(FourCC(n)), IsItemIdSellable(FourCC(n)), IsItemIdPawnable(FourCC(n)))
            end
            print(kind(CreateItem(FourCC("hfoo"), 0, 0)), kind(CreateItem(0, 0, 0)), kind(CreateItem(FourCC("zzzz"), 0, 0)))
            local s = BlzCreateItemWithSkin(FourCC("phea"), 0, 0, FourCC("ratc"))
            print(id(s), BlzGetItemSkin(s) == FourCC("ratc"), GetItemName(s), GetItemCharges(s))
            print(BlzGetItemTooltip(i), BlzGetItemDescription(i), BlzGetItemExtendedTooltip(i), BlzGetItemIconPath(i))
            BlzSetItemName(i, "Probe Potion") BlzSetItemTooltip(i, "Probe Tip")
            print(GetItemName(i), BlzGetItemTooltip(i), GetObjectName(FourCC("phea")))
            RemoveItem(i)
            print(GetItemTypeId(i), GetItemX(i), GetWidgetLife(i), GetItemCharges(i), pid(GetItemPlayer(i)))
            """);

        Assert.Equal([
            "1 1 item",
            "100.5 -200.25 100.5 -200.25 75.0 27 0 Potion of Healing",
            "true false false false true true false false",
            "phea 1 1 4 false true true",
            "ratc 0 5 0 false true true",
            "tdex 1 1 2 true false true",
            "rej1 0 2 8 false true true",
            "nil nil nil",
            "phea true Claws of Attack +12 1",
            "Purchase Potion Restores life Heals when used Art/potion.blp",
            "Probe Potion Probe Tip Potion of Healing",
            "0 0.0 0.0 0 nil",
        ], Printed(simulation));
    }

    // probe items: charges kept from 0; the flags and fields set read back (the boolean setters answer false, the real field
    // too, and keep them); sold to merchants is pawnable; a set of the level, charges and life reads through the natives
    [Fact]
    public void An_items_values_and_fields_are_set()
    {
        using var simulation = Run("""
            local i = CreateItem(FourCC("wneg"), 0, 0)
            SetItemCharges(i, 7) print(GetItemCharges(i))
            SetItemCharges(i, -2) print(GetItemCharges(i))
            SetItemUserData(i, 42) print(GetItemUserData(i))
            SetItemPlayer(i, Player(2), true) print(pid(GetItemPlayer(i)), IsItemOwned(i))
            SetItemInvulnerable(i, true) SetItemVisible(i, false) print(IsItemInvulnerable(i), IsItemVisible(i))
            SetItemPawnable(i, false) print(IsItemPawnable(i), IsItemIdPawnable(FourCC("wneg")))
            SetItemDropOnDeath(i, true) SetItemDroppable(i, false)
            print(BlzGetItemBooleanField(i, ITEM_BF_DROPPED_WHEN_CARRIER_DIES), BlzGetItemBooleanField(i, ITEM_BF_CAN_BE_DROPPED),
                BlzGetItemBooleanField(i, ITEM_BF_CAN_BE_SOLD_TO_MERCHANTS), BlzGetItemBooleanField(i, ITEM_BF_PERISHABLE), BlzGetItemBooleanField(i, ITEM_BF_ACTIVELY_USED))
            print(BlzGetItemIntegerField(i, ITEM_IF_LEVEL), BlzGetItemIntegerField(i, ITEM_IF_NUMBER_OF_CHARGES), BlzGetItemIntegerField(i, ITEM_IF_MAX_HIT_POINTS),
                BlzGetItemIntegerField(i, ITEM_IF_HIT_POINTS), BlzGetItemIntegerField(i, ITEM_IF_PRIORITY), BlzGetItemIntegerField(i, ITEM_IF_ARMOR_TYPE),
                BlzGetItemIntegerField(i, ITEM_IF_TINTING_COLOR_RED), BlzGetItemIntegerField(i, ITEM_IF_TINTING_COLOR_ALPHA))
            print(BlzGetItemRealField(i, ITEM_RF_SCALING_VALUE), BlzGetItemStringField(i, ITEM_SF_MODEL_USED))
            print(BlzSetItemIntegerField(i, ITEM_IF_LEVEL, 9), GetItemLevel(i), BlzSetItemIntegerField(i, ITEM_IF_NUMBER_OF_CHARGES, 4), GetItemCharges(i))
            print(BlzSetItemIntegerField(i, ITEM_IF_HIT_POINTS, 30), GetWidgetLife(i), BlzSetItemIntegerField(i, ITEM_IF_MAX_HIT_POINTS, 50), BlzGetItemIntegerField(i, ITEM_IF_MAX_HIT_POINTS), GetWidgetLife(i))
            print(BlzSetItemBooleanField(i, ITEM_BF_PERISHABLE, false), BlzGetItemBooleanField(i, ITEM_BF_PERISHABLE),
                BlzSetItemBooleanField(i, ITEM_BF_USE_AUTOMATICALLY_WHEN_ACQUIRED, true), IsItemPowerup(i))
            print(BlzSetItemRealField(i, ITEM_RF_SCALING_VALUE, 2), BlzGetItemRealField(i, ITEM_RF_SCALING_VALUE))
            SetItemPosition(i, 300.5, 400.25) print(GetItemX(i), GetItemY(i))
            """);

        Assert.Equal([
            "7", "0", "42", "2 false", "true false", "false true",
            "true false false true true",
            "2 0 75 75 100 3 255 255",
            "1.0 Models/chest.mdl",
            "true 9 true 4",
            "true 30.0 true 50 30.0",
            "false false false true",
            "false 2.0",
            "300.5 400.25",
        ], Printed(simulation));
    }

    // probe items: life is kept to the most (500 reads 75); below 0.405 the item dies, life 0, the widget death event inside
    // the call, once, also for an invulnerable item, and it stays
    [Fact]
    public void An_items_life_ends_in_a_death_event_inside_the_call()
    {
        using var simulation = Run("""
            local i = CreateItem(FourCC("phea"), 200, 200)
            local t = CreateTrigger()
            TriggerRegisterDeathEvent(t, i)
            TriggerAddAction(t, function() print("death", GetHandleId(GetTriggerEventId()), GetTriggerWidget() == i, GetWidgetLife(i), id(i), show(GetTriggerUnit())) end)
            SetWidgetLife(i, 30.5) print(GetWidgetLife(i))
            SetWidgetLife(i, 500) print(GetWidgetLife(i))
            SetWidgetLife(i, 0.3) print(GetWidgetLife(i), id(i))
            SetWidgetLife(i, 0) print(GetWidgetLife(i), id(i))
            local v = CreateItem(FourCC("phea"), 200, 200)
            SetItemInvulnerable(v, true) SetWidgetLife(v, 0) print(GetWidgetLife(v), id(v))
            """);

        Assert.Equal(["30.5", "75.0", "death 89 true 0.0 phea nil", "0.0 phea", "0.0 phea", "0.0 phea"], Printed(simulation));
    }

    // probe items: a rect finds the items on the ground, hidden ones too, the last made first in a square, each through the
    // filter then the callback with GetFilterItem and GetEnumItem; a removed one is not found, nor a carried one
    [Fact]
    public void A_rect_enumerates_the_items_on_the_ground()
    {
        using var simulation = Run("""
            local r = Rect(-1900, 1500, -1500, 1900)
            local made = {}
            for k, n in ipairs({ "phea", "ratc", "rag1", "wneg" }) do made[k] = CreateItem(FourCC(n), -1800 + k * 60, 1700 - k * 40) end
            SetItemVisible(made[2], false)
            local seen = {}
            EnumItemsInRect(r, Filter(function() return GetItemTypeId(GetFilterItem()) ~= FourCC("wneg") end), function() seen[#seen + 1] = id(GetEnumItem()) .. "/" .. id(GetFilterItem()) end)
            print(table.concat(seen, " "), show(GetEnumItem()))
            RemoveItem(made[1])
            seen = {}
            EnumItemsInRect(r, nil, function() seen[#seen + 1] = id(GetEnumItem()) end)
            print(table.concat(seen, " "))
            UnitAddItem(make("Hpal"), made[3])
            seen = {}
            EnumItemsInRect(r, nil, function() seen[#seen + 1] = id(GetEnumItem()) end)
            print(table.concat(seen, " "))
            """);

        Assert.Equal(["rag1/rag1 ratc/ratc phea/phea nil", "wneg rag1 ratc", "wneg ratc"], Printed(simulation));
    }

    // probe items: a hero has six slots, a footman none; an item added goes to the first free slot, owned and hidden at the
    // unit's place, its pickup events (the player's first) with it in place; no stacking; a full inventory refuses and leaves
    // the item where the unit is; slots by number; a drop to a slot swaps; taking one out drops it south, with its drop events
    // fired first; using an item takes a charge, fires the use event and drops a spent perishable one
    [Fact]
    public void A_hero_carries_six_items_with_events_inside_the_calls()
    {
        using var simulation = Run("""
            local h = make("Hpal", 100)
            print(UnitInventorySize(h), UnitInventorySize(make("hfoo")), slots(h))
            local t = CreateTrigger()
            for _, e in ipairs({ EVENT_UNIT_PICKUP_ITEM, EVENT_UNIT_DROP_ITEM, EVENT_UNIT_USE_ITEM }) do TriggerRegisterUnitEvent(t, h, e) end
            for _, e in ipairs({ EVENT_PLAYER_UNIT_PICKUP_ITEM, EVENT_PLAYER_UNIT_DROP_ITEM, EVENT_PLAYER_UNIT_USE_ITEM }) do TriggerRegisterPlayerUnitEvent(t, Player(0), e, nil) end
            TriggerAddAction(t, function()
                local m = GetManipulatedItem()
                print("event", GetHandleId(GetTriggerEventId()), GetManipulatingUnit() == h, GetTriggerUnit() == h, pid(GetTriggerPlayer()), id(m), UnitHasItem(h, m), GetItemCharges(m), slots(h))
            end)
            local r = UnitAddItemById(h, FourCC("ratc"))
            print("added", id(r), slots(h), IsItemOwned(r), pid(GetItemPlayer(r)), IsItemVisible(r), GetItemX(r), GetItemY(r))
            print("slot 3", UnitAddItemToSlotById(h, FourCC("rag1"), 3), slots(h), UnitAddItemToSlotById(h, FourCC("rag1"), 3), UnitAddItemToSlotById(h, FourCC("rag1"), 6))
            print("slots", kind(UnitItemInSlot(h, -1)), kind(UnitItemInSlot(h, 6)))
            local p1 = UnitAddItemById(h, FourCC("phea"))
            local p2 = UnitAddItemById(h, FourCC("phea"))
            print("two", slots(h), GetItemCharges(p1), GetItemCharges(p2))
            print("ground", UnitAddItem(h, CreateItem(FourCC("phea"), 600, 600)), slots(h))
            local out = UnitRemoveItemFromSlot(h, 3)
            print("out", id(out), slots(h), GetItemX(out), GetItemY(out), IsItemVisible(out), IsItemOwned(out), kind(UnitRemoveItemFromSlot(h, 3)))
            print("use", UnitUseItem(h, p1), GetItemCharges(p1), slots(h))
            for _, n in ipairs({ "ratc", "rag1", "ratc", "rag1" }) do UnitAddItemById(h, FourCC(n)) end
            print("full", slots(h), kind(UnitAddItemById(h, FourCC("phea"))), UnitAddItem(h, CreateItem(FourCC("phea"), 0, 0)))
            print("drop", UnitDropItemSlot(h, UnitItemInSlot(h, 0), 4), slots(h), UnitDropItemSlot(h, UnitItemInSlot(h, 0), 6))
            print("point", UnitDropItemPoint(h, UnitItemInSlot(h, 1), 0, 0), slots(h))
            """);

        var lines = Printed(simulation);
        Assert.Equal("6 0 0,0,0,0,0,0 count 0", lines[0]);
        Assert.Equal([
            "event 49 true true 0 ratc true 0 ratc,0,0,0,0,0 count 1",
            "event 86 true true 0 ratc true 0 ratc,0,0,0,0,0 count 1",
            "added ratc ratc,0,0,0,0,0 count 1 true 27 false 100.0 0.0",
            "event 49 true true 0 rag1 true 0 ratc,0,0,rag1,0,0 count 2",
            "event 86 true true 0 rag1 true 0 ratc,0,0,rag1,0,0 count 2",
            "slot 3 true ratc,0,0,rag1,0,0 count 2 false false",
            "slots nil nil",
            "event 49 true true 0 phea true 1 ratc,phea,0,rag1,0,0 count 3",
            "event 86 true true 0 phea true 1 ratc,phea,0,rag1,0,0 count 3",
            "event 49 true true 0 phea true 1 ratc,phea,phea,rag1,0,0 count 4",
            "event 86 true true 0 phea true 1 ratc,phea,phea,rag1,0,0 count 4",
            "two ratc,phea,phea,rag1,0,0 count 4 1 1",
            "event 49 true true 0 phea true 1 ratc,phea,phea,rag1,phea,0 count 5",
            "event 86 true true 0 phea true 1 ratc,phea,phea,rag1,phea,0 count 5",
            "ground true ratc,phea,phea,rag1,phea,0 count 5",
            "event 48 true true 0 rag1 true 0 ratc,phea,phea,rag1,phea,0 count 5",
            "event 85 true true 0 rag1 true 0 ratc,phea,phea,rag1,phea,0 count 5",
            "out rag1 ratc,phea,phea,0,phea,0 count 4 100.0 -24.0 true false nil",
            "event 50 true true 0 phea true 0 ratc,phea,phea,0,phea,0 count 4",
            "event 87 true true 0 phea true 0 ratc,phea,phea,0,phea,0 count 4",
            "event 48 true true 0 phea true 0 ratc,phea,phea,0,phea,0 count 4",
            "event 85 true true 0 phea true 0 ratc,phea,phea,0,phea,0 count 4",
            "use true 0 ratc,0,phea,0,phea,0 count 3",
        ], lines.Skip(1).Take(23).ToList());
        Assert.Equal(["full ratc,ratc,phea,rag1,phea,ratc count 6 nil false", "drop true phea,ratc,phea,rag1,ratc,ratc count 6 false", "point true phea,ratc,phea,rag1,ratc,ratc count 6"],
            lines.Skip(1).Skip(23).Where(line => !line.StartsWith("event")));
    }

    // probe items: a powerup is picked up and dropped at once with both events, is returned, and goes a little later; its
    // effect is the abilities' (W2-7)
    [Fact]
    public void A_powerup_is_picked_up_and_dropped_at_once_and_goes_later()
    {
        using var simulation = Run("""
            local h = make("Hpal")
            local t = CreateTrigger()
            for _, e in ipairs({ EVENT_UNIT_PICKUP_ITEM, EVENT_UNIT_DROP_ITEM }) do TriggerRegisterUnitEvent(t, h, e) end
            TriggerAddAction(t, function() print("event", GetHandleId(GetTriggerEventId()), id(GetManipulatedItem()), UnitHasItem(h, GetManipulatedItem())) end)
            local tome = UnitAddItemById(h, FourCC("tdex"))
            print("tome", id(tome), slots(h))
            TimerStart(CreateTimer(), 0, false, function() print("later", id(tome)) end)
            TimerStart(CreateTimer(), 0.5, false, function() print("then", id(tome)) end)
            """);
        simulation.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(["event 86 tdex false", "event 85 tdex false", "tome tdex 0,0,0,0,0,0 count 0", "later tdex", "then 0"], Printed(simulation));
    }

    // Wave 2's task: the random item picks take the data's pick-random items of the level and class, in a fixed order; pools
    // pick by weight and make what they pick; an empty pool or no match gives nothing
    [Fact]
    public void Pools_and_random_picks_choose_from_the_data()
    {
        using var simulation = Run("""
            local seen = {}
            for _ = 1, 40 do seen[ChooseRandomItem(-1)] = true end
            local names = {}
            for code in pairs(seen) do names[#names + 1] = string.char(code >> 24 & 255, code >> 16 & 255, code >> 8 & 255, code & 255) end
            table.sort(names)
            print(table.concat(names, " "), ChooseRandomItem(5) == FourCC("ratc"), ChooseRandomItem(9), ChooseRandomItemEx(ITEM_TYPE_POWERUP, -1) == FourCC("tdex") or ChooseRandomItemEx(ITEM_TYPE_POWERUP, -1))
            print(ChooseRandomItemExWithFilterAndIncludes(ITEM_TYPE_PURCHASABLE, 1, nil, nil, false, true) == FourCC("phea"))
            local pool = CreateItemPool()
            print(kind(PlaceRandomItem(pool, 0, 0)))
            ItemPoolAddItemType(pool, FourCC("phea"), 1) ItemPoolAddItemType(pool, FourCC("wneg"), 0) ItemPoolAddItemType(pool, FourCC("hfoo"), 1) ItemPoolRemoveItemType(pool, FourCC("hfoo"))
            local i = PlaceRandomItem(pool, 10, 20)
            print(id(i), GetItemX(i), GetItemY(i))
            DestroyItemPool(pool)
            local units = CreateUnitPool()
            UnitPoolAddUnitType(units, FourCC("hfoo"), 5)
            local u = PlaceRandomUnit(units, Player(1), 30, 40, 90)
            print(GetUnitTypeId(u) == FourCC("hfoo"), GetUnitX(u), GetUnitFacing(u), pid(GetOwningPlayer(u)), kind(PlaceRandomUnit(CreateUnitPool(), Player(0), 0, 0, 0)))
            """);

        Assert.Equal(["rag1 ratc tdex true 0 true", "true", "nil", "phea 10.0 20.0", "true 30.0 90.0 1 nil"], Printed(simulation));
    }
}
