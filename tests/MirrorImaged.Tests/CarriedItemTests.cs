using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>
/// What carried items and powerups give a hero, the heal of a use, the ankh, and a unit stored in a game cache, against
/// the probes items-carried, items-ankh and heroes-store (facts, "Items carried (W2-3)" and "Stored units
/// (W2-3)"), on maps with the data the test gives (decisions D207).
/// </summary>
public sealed class CarriedItemTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static Field Column(string column, params string[] specific) => new(column, PerLevel.Column, -1, "int", false, [.. specific.Select(Ids.Of)]);

    // An item of the data's own shape: the class, its ability, whether it is a powerup or perishes
    static Dictionary<string, string> Item(string own)
    {
        var values = Values("ihtp=75;ilev=1;isel=1;ipaw=1;iper=0;ipow=0;iprn=0;iusa=0;idro=1;idrp=0;ipri=100;iarm=Wood;isca=1;ifil=Models/chest.mdl;icid=AIxx;icla=Permanent;iuse=0");
        foreach (var (key, value) in Values(own)) values[key] = value;
        return values;
    }

    static StandardData Standard()
    {
        var data = new StandardData()
            .Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=420;umpm=0;umpi=0"))
            .Add(ObjectKind.Unit, "hbar", Values("unam=Barracks;uhpm=1500;umpm=0;umpi=0;ubdg=1"))
            .Add(ObjectKind.Unit, "Hpal", Values("unam=Paladin;upro=Granis Darkhammer,Jorn the Redeemer;uhpm=100;umpm=0;umpi=100;ustr=22;uagi=13;uint=17;ustp=2.7;uagp=1.5;uinp=1.8;uhab=AHhb;udef=2"))
            .Add(ObjectKind.Ability, "AHhb", Values("anam=Holy Light;alev=3;arlv=1;alsk=0"))
            .Add(ObjectKind.Item, "rst1", Item("unam=Gauntlets of Ogre Strength +3;iabi=AIs3"))
            .Add(ObjectKind.Item, "rag1", Item("unam=Slippers of Agility +3;iabi=AIa3"))
            .Add(ObjectKind.Item, "prvt", Item("unam=Periapt of Vitality;iabi=AIlf"))
            .Add(ObjectKind.Item, "pmna", Item("unam=Pendant of Mana;iabi=AIbm"))
            .Add(ObjectKind.Item, "rde1", Item("unam=Ring of Protection +2;iabi=AId2"))
            .Add(ObjectKind.Item, "tstr", Item("unam=Tome of Strength;icla=PowerUp;ipow=1;iuse=1;iabi=AIsm"))
            .Add(ObjectKind.Item, "manh", Item("unam=Manual of Health;icla=PowerUp;ipow=1;iuse=1;iabi=AImh"))
            .Add(ObjectKind.Item, "texp", Item("unam=Tome of Experience;icla=PowerUp;ipow=1;iuse=1;iabi=AIem"))
            .Add(ObjectKind.Item, "phea", Item("unam=Potion of Healing;icla=Purchasable;iuse=1;iper=1;iusa=1;iabi=AIh1"))
            .Add(ObjectKind.Item, "pman", Item("unam=Potion of Mana;icla=Purchasable;iuse=1;iper=1;iusa=1;iabi=AIm1"))
            .Add(ObjectKind.Item, "ankh", Item("unam=Ankh of Reincarnation;icla=Charged;iuse=1;iper=1;iusa=1;iabi=AIrc"))
            .Add(ObjectKind.Ability, "AIs3", Values("alev=1;Istr:1=3;Iagi:1=0;Iint:1=0"))
            .Add(ObjectKind.Ability, "AIa3", Values("alev=1;Istr:1=0;Iagi:1=3;Iint:1=0"))
            .Add(ObjectKind.Ability, "AIsm", Values("alev=1;Istr:1=1;Iagi:1=0;Iint:1=0"))
            .Add(ObjectKind.Ability, "AIlf", Values("alev=1;Ilif:1=150"))
            .Add(ObjectKind.Ability, "AImh", Values("alev=1;Ilif:1=50"))
            .Add(ObjectKind.Ability, "AIbm", Values("alev=1;Iman:1=250"))
            .Add(ObjectKind.Ability, "AId2", Values("alev=1;Idef:1=2"))
            .Add(ObjectKind.Ability, "AIem", Values("alev=1;Ixpg:1=100"))
            .Add(ObjectKind.Ability, "AIh1", Values("alev=1;Ihpg:1=250"))
            .Add(ObjectKind.Ability, "AIm1", Values("alev=1;Impg:1=150"))
            .Add(ObjectKind.Ability, "AIrc", Values("alev=1;Ircd:1=7;irc2:1=500"));
        foreach (var (id, field) in new (string, Field)[]
        {
            ("alev", new("levels", PerLevel.None, -1, "int")), ("arlv", new("reqLevel", PerLevel.None, -1, "int")), ("alsk", new("levelSkip", PerLevel.None, -1, "int")),
            ("Istr", Column("DataC", "AIs3", "AIsm")), ("Iagi", Column("DataA", "AIa3")), ("Iint", Column("DataB", "AIs3", "AIa3", "AIsm")),
            ("Ilif", Column("DataA", "AIlf", "AImh")), ("Iman", Column("DataA", "AIbm")), ("Idef", Column("DataA", "AId2")), ("Ixpg", Column("DataA", "AIem")),
            ("Ihpg", Column("DataA", "AIh1")), ("Impg", Column("DataA", "AIm1")), ("Ircd", Column("DataA", "AIrc")), ("irc2", Column("DataB", "AIrc")),
        })
            data.Add(ObjectKind.Ability, id, field);
        foreach (var (key, value) in Values("StrHitPointBonus=25;IntManaBonus=15;AgiDefenseBase=-2;AgiDefenseBonus=0.3"))
            data.Constants[key] = value;
        return data;
    }

    Simulation Run(string main, double seconds = 0) => Simulation.Load(_maps.Build($$"""
        local function id(i)
            local n = GetItemTypeId(i)
            return n == 0 and "0" or string.char(n >> 24 & 255, n >> 16 & 255, n >> 8 & 255, n & 255)
        end
        local function slots(h)
            local s = {}
            for i = 0, 5 do
                local it = UnitItemInSlot(h, i)
                s[#s + 1] = id(it) .. (it and ("x" .. GetItemCharges(it)) or "")
            end
            return table.concat(s, ",")
        end
        local function abilities(h)
            local n = 0
            while BlzGetUnitAbilityByIndex(h, n) do n = n + 1 end
            return n
        end
        local function state(h)
            return GetHeroStr(h, false) .. "/" .. GetHeroStr(h, true) .. " " .. GetHeroAgi(h, false) .. "/" .. GetHeroAgi(h, true) .. " " .. GetWidgetLife(h) .. "/"
                .. BlzGetUnitMaxHP(h) .. " " .. GetUnitState(h, UNIT_STATE_MANA) .. "/" .. BlzGetUnitMaxMana(h) .. " " .. BlzGetUnitRealField(h, UNIT_RF_DEFENSE)
        end
        local function make(uid, x) return CreateUnit(Player(0), FourCC(uid), x or 0, 0, 0) end
        function config() end
        function main()
        {{main}}
        end
        """), "", data: Standard());

    static List<string> Printed(Simulation simulation) => [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..].Replace('\t', ' '))];

    // probe items-carried: a carried item's bonus counts at once (strength +3 gives 75 life), the item's ability is on the
    // unit first in its list, the base stays what it was, and taking the item out gives it all back
    [Fact]
    public void A_carried_item_gives_its_bonus_and_its_ability()
    {
        using var simulation = Run("""
            local h = make("Hpal")
            print(state(h), abilities(h))
            UnitAddItemById(h, FourCC("rst1"))
            print(state(h), abilities(h), GetUnitAbilityLevel(h, FourCC("AIs3")), BlzGetAbilityId(BlzGetUnitAbilityByIndex(h, 0)) == FourCC("AIs3"))
            print(BlzGetUnitIntegerField(h, UNIT_IF_STRENGTH), BlzGetUnitIntegerField(h, UNIT_IF_STRENGTH_PERMANENT), BlzGetUnitIntegerField(h, UNIT_IF_STRENGTH_WITH_BONUS))
            UnitAddItemById(h, FourCC("rag1"))
            print(state(h), abilities(h))
            UnitRemoveItemFromSlot(h, 0)
            print(state(h), abilities(h), GetUnitAbilityLevel(h, FourCC("AIs3")))
            """);

        Assert.Equal([
            "22/22 13/13 650.0/650 255.0/255 3.9 1",
            "22/25 13/13 725.0/725 255.0/255 3.9 2 1 true",
            "22 22 25",
            "22/25 13/16 725.0/725 255.0/255 4.8 3",
            "22/22 13/16 650.0/650 255.0/255 4.8 2 0",
        ], Printed(simulation));
    }

    // probe items-carried: the life and mana keep their share of the most when an item changes it (300 of 650 with +75 read
    // 335 of 725, and 300 of 650 with +150 read 369 of 800), the armour item adds its armour, and the pendant gives mana
    [Fact]
    public void An_item_that_changes_the_most_life_or_mana_keeps_the_heros_share()
    {
        using var simulation = Run("""
            local h = make("Hpal")
            SetWidgetLife(h, 300)
            SetUnitState(h, UNIT_STATE_MANA, 50)
            UnitAddItemById(h, FourCC("rst1"))
            print(state(h))
            UnitRemoveItemFromSlot(h, 0)
            print(state(h))
            UnitAddItemById(h, FourCC("prvt"))
            UnitAddItemById(h, FourCC("pmna"))
            print(state(h))
            UnitAddItemById(h, FourCC("rde1"))
            print(state(h))
            """);

        Assert.Equal([
            "22/25 13/13 335.0/725 50.0/255 3.9",
            "22/22 13/13 300.0/650 50.0/255 3.9",
            "22/22 13/13 369.0/800 99.0/505 3.9",
            "22/22 13/13 369.0/800 99.0/505 5.9",
        ], Printed(simulation));
    }

    // probe items-carried: a set of an attribute sets the base under a bonus, and the bonus stays on top
    [Fact]
    public void An_attribute_set_under_a_bonus_sets_the_base()
    {
        using var simulation = Run("""
            local h = make("Hpal")
            UnitAddItemById(h, FourCC("rag1"))
            UnitAddItemById(h, FourCC("rst1"))
            SetHeroAgi(h, 20, false)
            SetHeroStr(h, 30, true)
            print(state(h))
            UnitRemoveItemFromSlot(h, 0)
            print(state(h))
            """);

        Assert.Equal([
            "30/33 20/23 925.0/925 255.0/255 6.9",
            "30/33 20/20 925.0/925 255.0/255 6.0",
        ], Printed(simulation));
    }

    // probe items-carried: a tome gives its attribute for good, its life share kept (400 of 650 reads 415 of 675); a manual's
    // 50 life is added (450 of 700); experience is added unless suspended; the tome is in no slot
    [Fact]
    public void A_powerup_gives_its_gift_and_is_in_no_slot()
    {
        using var simulation = Run("""
            local h = make("Hpal")
            SetWidgetLife(h, 400)
            UnitAddItemById(h, FourCC("tstr"))
            print(state(h), slots(h))
            UnitAddItemById(h, FourCC("manh"))
            print(state(h))
            UnitAddItemById(h, FourCC("texp"))
            print(GetHeroXP(h), GetHeroLevel(h))
            SuspendHeroXP(h, true)
            UnitAddItemById(h, FourCC("texp"))
            print(GetHeroXP(h))
            """);

        Assert.Equal([
            "23/23 13/13 415.0/675 255.0/255 3.9 0,0,0,0,0,0",
            "23/23 13/13 465.0/725 255.0/255 3.9",
            "100 1",
            "100",
        ], Printed(simulation));
    }

    // probe items-carried: a potion heals inside the call, before the use event reads it; at full life or mana, and for a
    // dead hero, the use is refused with its charge kept
    [Fact]
    public void A_potion_heals_when_used_and_is_refused_when_it_cannot()
    {
        using var simulation = Run("""
            local h = make("Hpal")
            local t = CreateTrigger()
            TriggerRegisterUnitEvent(t, h, EVENT_UNIT_USE_ITEM)
            TriggerAddAction(t, function() print("event", GetWidgetLife(h), slots(h)) end)
            SetWidgetLife(h, 100)
            local p = UnitAddItemById(h, FourCC("phea"))
            print(UnitUseItem(h, p), GetWidgetLife(h), GetItemCharges(p), slots(h))
            p = UnitAddItemById(h, FourCC("phea"))
            SetWidgetLife(h, 650)
            print(UnitUseItem(h, p), GetWidgetLife(h), GetItemCharges(p), slots(h))
            local m = UnitAddItemById(h, FourCC("pman"))
            SetUnitState(h, UNIT_STATE_MANA, 200)
            print(UnitUseItem(h, m), GetUnitState(h, UNIT_STATE_MANA))
            local d = make("Hpal", 500)
            local q = UnitAddItemById(d, FourCC("phea"))
            KillUnit(d)
            print(UnitUseItem(d, q), GetItemCharges(q))
            """);

        Assert.Equal([
            "event 350.0 pheax0,0,0,0,0,0",
            "true 350.0 0 0,0,0,0,0,0",
            "false 650.0 1 pheax1,0,0,0,0,0",
            "event 650.0 pheax1,pmanx0,0,0,0,0",
            "true 255.0",
            "false 1",
        ], Printed(simulation));
    }

    // probe items-ankh: the hero killed with a charged ankh uses it as it dies (use event with the hero dead, no death event), is
    // alive 7 s later at 500 life with its mana; a drop and the item gone if it was the last charge; with none left it dies
    [Fact]
    public void An_ankh_brings_a_hero_back_after_its_delay()
    {
        using var simulation = Run("""
            local h = make("Hpal")
            local ankh = UnitAddItemById(h, FourCC("ankh"))
            SetItemCharges(ankh, 2)
            for _, e in ipairs({ { EVENT_UNIT_USE_ITEM, "use" }, { EVENT_UNIT_DROP_ITEM, "drop" }, { EVENT_UNIT_DEATH, "death" } }) do
                local t = CreateTrigger()
                TriggerRegisterUnitEvent(t, h, e[1])
                TriggerAddAction(t, function() print(e[2], UnitAlive(h), GetWidgetLife(h), slots(h)) end)
            end
            KillUnit(h)
            print("killed", UnitAlive(h), slots(h))
            TimerStart(CreateTimer(), 6.9, false, function() print("6.9", UnitAlive(h)) end)
            TimerStart(CreateTimer(), 7.1, false, function()
                print("7.1", UnitAlive(h), GetWidgetLife(h), GetUnitState(h, UNIT_STATE_MANA))
                KillUnit(h)
                print("killed again", UnitAlive(h), slots(h))
            end)
            TimerStart(CreateTimer(), 15, false, function() print("15", UnitAlive(h), slots(h)) end)
            local e = make("Hpal", 300)
            UnitAddItemById(e, FourCC("ankh"))
            SetItemCharges(UnitItemInSlot(e, 0), 0)
            KillUnit(e)
            TimerStart(CreateTimer(), 8, false, function() print("empty", UnitAlive(e)) end)
            """);
        simulation.Advance(TimeSpan.FromSeconds(16));

        Assert.Equal([
            "use false 0.0 ankhx1,0,0,0,0,0",
            "killed false ankhx1,0,0,0,0,0",
            "6.9 false",
            "7.1 true 500.0 255.0",
            "use false 0.0 ankhx0,0,0,0,0,0",
            "drop false 0.0 ankhx0,0,0,0,0,0",
            "killed again false 0,0,0,0,0,0",
            "empty false",
            "15 true 0,0,0,0,0,0",
        ], Printed(simulation));
    }

    // probe heroes-store: StoreUnit answers false and keeps the unit; RestoreUnit makes a new one for the player at the place
    // with its level, experience, points, skill, attributes and items (charges too), at full life and mana, its pickup
    // events fired and one level event; the name and user data are not kept
    [Fact]
    public void A_stored_hero_is_restored_with_its_level_skills_attributes_and_items()
    {
        using var simulation = Run("""
            local cache = InitGameCache("c.w3v")
            local h = make("Hpal")
            SetHeroLevel(h, 5, false)
            SelectHeroSkill(h, FourCC("AHhb"))
            UnitAddItemById(h, FourCC("rst1"))
            local p = UnitAddItemById(h, FourCC("phea"))
            SetItemCharges(p, 3)
            SetHeroStr(h, 30, true)
            SetWidgetLife(h, 400)
            SetUnitUserData(h, 9)
            print("have", HaveStoredUnit(cache, "m", "h"), "store", StoreUnit(cache, "m", "h", h), HaveStoredUnit(cache, "M", "H"))
            print("original", GetHeroLevel(h), GetHeroXP(h), GetHeroSkillPoints(h), GetUnitAbilityLevel(h, FourCC("AHhb")), abilities(h), state(h), slots(h))
            local picked, levelled = 0, 0
            local t = CreateTrigger()
            TriggerRegisterPlayerUnitEvent(t, Player(2), EVENT_PLAYER_UNIT_PICKUP_ITEM, nil)
            TriggerAddAction(t, function() picked = picked + 1 end)
            local l = CreateTrigger()
            TriggerRegisterPlayerUnitEvent(l, Player(2), EVENT_PLAYER_HERO_LEVEL, nil)
            TriggerAddAction(l, function() levelled = levelled + 1 end)
            local r = RestoreUnit(cache, "m", "h", Player(2), 100.5, 200.25, 45)
            print("restored", r ~= h, GetPlayerId(GetOwningPlayer(r)), GetUnitX(r), GetUnitY(r), GetUnitFacing(r), GetUnitUserData(r), picked, levelled)
            print(GetHeroLevel(r), GetHeroXP(r), GetHeroSkillPoints(r), GetUnitAbilityLevel(r, FourCC("AHhb")), abilities(r), state(r), slots(r))
            print("original kept", GetWidgetLife(h), GetUnitUserData(h))
            print(RestoreUnit(cache, "m", "none", Player(0), 0, 0, 0), RestoreUnit(cache, "m", "h", nil, 0, 0, 0))
            FlushStoredUnit(cache, "m", "h")
            print("flushed", HaveStoredUnit(cache, "m", "h"), RestoreUnit(cache, "m", "h", Player(0), 0, 0, 0))
            """);

        Assert.Equal([
            "have false store false true",
            "original 5 1400 4 1 4 30/33 19/19 400.0/925 360.0/360 5.7 rst1x0,pheax3,0,0,0,0",
            "restored true 2 100.5 200.25 45.0 0 2 1",
            "5 1400 4 1 4 30/33 19/19 925.0/925 360.0/360 5.7 rst1x0,pheax3,0,0,0,0",
            "original kept 400.0 9",
            "nil nil",
            "flushed false nil",
        ], Printed(simulation));
    }

    // probe items-abilities: the abilities an item holds are the ones it puts on its carrier, the unit's own handles, by index and by id, and an item
    // that isn't carried (or one that is dropped) holds none
    [Fact]
    public void A_carried_items_abilities_are_the_ones_it_puts_on_its_carrier_and_a_free_item_holds_none()
    {
        using var simulation = Run("""
            local h = make("Hpal")
            local i = CreateItem(FourCC("phea"), 500, 500)
            print(BlzGetItemAbilityByIndex(i, 0), BlzGetItemAbility(i, FourCC("AIh1")))
            UnitAddItem(h, i)
            local a = BlzGetItemAbilityByIndex(i, 0)
            print(BlzGetAbilityId(a) == FourCC("AIh1"), BlzGetItemAbility(i, FourCC("AIh1")) == a, BlzGetUnitAbility(h, FourCC("AIh1")) == a,
                BlzGetItemAbilityByIndex(i, 1), BlzGetItemAbilityByIndex(i, -1), BlzGetItemAbility(i, FourCC("AIs3")), BlzGetItemAbility(nil, FourCC("AIh1")))
            UnitRemoveItem(h, i)
            print(BlzGetItemAbilityByIndex(i, 0), BlzGetItemAbility(i, FourCC("AIh1")))
            """);

        Assert.Equal(["nil nil", "true true true nil nil nil nil", "nil nil"], Printed(simulation));
    }
}
