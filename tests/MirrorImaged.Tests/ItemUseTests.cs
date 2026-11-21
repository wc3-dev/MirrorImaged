using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>
/// UnitUseItemPoint and UnitUseItemTarget, against the probe items-use-at (facts, "Items, equipment and the camera"): a carried item's ability is cast
/// by the order of its slot (852008 and the slot), the target one answering true and the point one false; the cast events, then the use event and a
/// charge less when it ends.
/// </summary>
public sealed class ItemUseTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static Field Column(string column, string type) => new(column, PerLevel.Column, -1, type);

    static Dictionary<string, string> Item(string own)
    {
        var values = Values("ihtp=75;ilev=1;isel=1;ipaw=1;iper=1;ipow=0;iprn=0;iusa=1;idro=1;idrp=0;ipri=100;iarm=Wood;isca=1;ifil=Models/chest.mdl;icid=AIxx;icla=Charged;iuse=3");
        foreach (var (key, value) in Values(own)) values[key] = value;
        return values;
    }

    static StandardData Standard()
    {
        var data = new StandardData();
        foreach (var (id, field) in new (string, Field)[]
        {
            ("alev", new("levels", PerLevel.None, -1, "int")), ("code", new("code", PerLevel.None, -1)), ("aord", new("Order", PerLevel.None, -1, "string")),
            ("acas", Column("Cast", "unreal")), ("aran", Column("Rng", "unreal")), ("aare", Column("Area", "unreal")), ("acdn", Column("Cool", "unreal")),
            ("amcs", Column("Cost", "int")), ("atar", Column("targs", "string")),
        })
            data.Add(ObjectKind.Ability, id, field);
        return data
            // A Wand of Lightning Shield: a unit of any side but the caster; a Staff of Silence and a Wand of Negation: a place; a Scroll of Town Portal: a structure; a potion: none
            .Add(ObjectKind.Ability, "AIls", Values("code=Alsh;alev=1;aord=lightningshield;atar:1=ground,friend,enemy,neutral;aran:1=600;aare:1=160;acas:1=0;acdn:1=0;amcs:1=0"))
            .Add(ObjectKind.Ability, "AIse", Values("code=ANsi;alev=1;aord=silence;atar:1=air,ground,enemy,organic,neutral;aran:1=700;aare:1=225;acas:1=0;acdn:1=0;amcs:1=0"))
            .Add(ObjectKind.Ability, "AIdi", Values("code=AIdi;alev=1;atar:1=air,ground,ward,invu,vuln,tree;aran:1=500;aare:1=200;acas:1=0;acdn:1=0;amcs:1=0"))
            .Add(ObjectKind.Ability, "AItp", Values("code=AItp;alev=1;atar:1=structure,vuln,invu;aran:1=99999;aare:1=1100;acas:1=5;acdn:1=0;amcs:1=0"))
            .Add(ObjectKind.Ability, "AIh1", Values("code=AIhe;alev=1;aran:1=100;acas:1=0"))
            .Add(ObjectKind.Item, "wlsd", Item("unam=Wand of Lightning Shield;iabi=AIls"))
            .Add(ObjectKind.Item, "ssil", Item("unam=Staff of Silence;iuse=0;iabi=AIse"))
            .Add(ObjectKind.Item, "wneg", Item("unam=Wand of Negation;iabi=AIdi"))
            .Add(ObjectKind.Item, "stwp", Item("unam=Scroll of Town Portal;iuse=1;iabi=AItp"))
            .Add(ObjectKind.Item, "phea", Item("unam=Potion of Healing;iuse=1;iabi=AIh1"))
            .Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=420;umvs=270;umvt=foot;uaen=0;ucol=31;udef=0"))
            .Add(ObjectKind.Unit, "htow", Values("unam=Town Hall;uhpm=1500;ubdg=1;ucol=100;umvs=0"))
            .Add(ObjectKind.Unit, "Hpal", Values("unam=Paladin;uhpm=100;umpm=0;umpi=100;ustr=22;agi=13;uint=17;umvs=270;umvt=foot;uaen=0;ucol=32"));
    }

    // A Paladin with the item in its first slot, a footman of its own 150 away, and a log of its order and cast events with the order's id and the charges
    List<string> Run(string item, string use, double seconds = 4)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            local log = {}
            local hero, item, mate
            local function charges() return item and GetItemCharges(item) or -1 end
            function config() end
            function main()
                hero = CreateUnit(Player(0), FourCC("Hpal"), 0, 0, 0)
                mate = CreateUnit(Player(0), FourCC("hfoo"), 150, 0, 0)
                item = UnitAddItemById(hero, FourCC("{{item}}"))
                local t = CreateTrigger()
                for _, e in ipairs({ EVENT_UNIT_ISSUED_ORDER, EVENT_UNIT_ISSUED_POINT_ORDER, EVENT_UNIT_ISSUED_TARGET_ORDER, EVENT_UNIT_SPELL_CHANNEL, EVENT_UNIT_SPELL_CAST,
                    EVENT_UNIT_SPELL_EFFECT, EVENT_UNIT_SPELL_FINISH, EVENT_UNIT_SPELL_ENDCAST, EVENT_UNIT_USE_ITEM }) do TriggerRegisterUnitEvent(t, hero, e) end
                TriggerAddAction(t, function()
                    local id = GetHandleId(GetTriggerEventId())
                    log[#log + 1] = id .. (id >= 289 and id <= 293 and (":" .. string.char(GetSpellAbilityId() >> 24, GetSpellAbilityId() >> 16 & 255, GetSpellAbilityId() >> 8 & 255, GetSpellAbilityId() & 255)) or ":" .. GetIssuedOrderId())
                end)
                local answer = {{use}}
                print("answer " .. tostring(answer) .. " inside " .. table.concat(log, " ") .. " order " .. GetUnitCurrentOrder(hero) .. " charges " .. charges())
                TimerStart(CreateTimer(), 3, false, function()
                    print("later " .. table.concat(log, " ") .. " order " .. GetUnitCurrentOrder(hero) .. " charges " .. charges())
                end)
            end
            """), "", data: Standard());
        simulation.Advance(TimeSpan.FromSeconds(seconds));
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];
    }

    const string Cast = "289:AIls 290:AIls 291:AIls 292:AIls 293:AIls 87:0";

    [Fact]
    public void A_target_in_range_is_answered_true_and_cast_on_inside_the_call_with_the_use_event_and_a_charge_less()
    {
        var lines = Run("wlsd", "UnitUseItemTarget(hero, item, mate)");

        Assert.Equal($"answer true inside 77:852008 {Cast} order 0 charges 2", lines[0].Replace('\t', ' '));
    }

    [Fact]
    public void A_target_out_of_range_keeps_the_slots_order_and_the_cast_comes_after_the_walk()
    {
        var lines = Run("wlsd", "UnitUseItemTarget(hero, item, CreateUnit(Player(PLAYER_NEUTRAL_AGGRESSIVE), FourCC(\"hfoo\"), 900, 0, 0))");

        Assert.Equal("answer true inside 77:852008 order 852008 charges 3", lines[0].Replace('\t', ' '));
        Assert.Equal($"later 77:852008 {Cast} order 0 charges 2", lines[1].Replace('\t', ' '));
    }

    [Theory]
    [InlineData("wlsd", "hero")]
    [InlineData("stwp", "mate")]
    [InlineData("wneg", "mate")]
    [InlineData("phea", "mate")]
    public void A_target_the_ability_doesnt_take_is_refused_with_no_order(string item, string target)
    {
        var lines = Run(item, $"UnitUseItemTarget(hero, item, {target})");

        Assert.Equal($"answer false inside  order 0 charges {(item == "wlsd" ? 3 : item == "wneg" ? 3 : 1)}", lines[0].Replace('\t', ' '));
        Assert.StartsWith("later  order 0", lines[1].Replace('\t', ' '));
    }

    // The game walked 424 to the building and fired only the channel in 1.5 s, the order kept; here its cast range (99999) casts at once and the casting time keeps the order
    [Fact]
    public void A_structure_is_taken_by_the_town_portal_and_its_cast_goes_on()
    {
        var lines = Run("stwp", "UnitUseItemTarget(hero, item, CreateUnit(Player(0), FourCC(\"htow\"), 424, 0, 0))");

        Assert.StartsWith("answer true inside 77:852008 289:AItp", lines[0].Replace('	', ' '));
        Assert.EndsWith("order 852008 charges 1", lines[1].Replace('	', ' '));
    }

    [Theory]
    [InlineData("wneg", "AIdi", 2)]
    [InlineData("ssil", "AIse", 0)]
    public void A_point_is_answered_false_but_the_order_is_given_and_the_cast_made_for_an_ability_that_takes_a_place(string item, string ability, int after)
    {
        var lines = Run(item, "UnitUseItemPoint(hero, item, 50, 0)");

        var cast = $"289:{ability} 290:{ability} 291:{ability} 292:{ability} 293:{ability} 87:0";
        Assert.Equal($"answer false inside 76:852008 {cast} order 0 charges {after}", lines[0].Replace('\t', ' '));
    }

    [Theory]
    [InlineData("wlsd")]
    [InlineData("stwp")]
    [InlineData("phea")]
    public void A_point_for_an_item_whose_ability_takes_none_is_refused_with_no_order(string item)
    {
        var lines = Run(item, "UnitUseItemPoint(hero, item, 50, 0)");

        Assert.StartsWith("answer false inside  order 0", lines[0].Replace('\t', ' '));
    }

    [Theory]
    [InlineData("UnitUseItemTarget(hero, nil, mate)")]
    [InlineData("UnitUseItemTarget(hero, item, nil)")]
    [InlineData("UnitUseItemTarget(nil, item, mate)")]
    [InlineData("UnitUseItemTarget(mate, item, hero)")]
    [InlineData("(UnitRemoveItem(hero, item) or UnitUseItemTarget(hero, item, mate))")]
    [InlineData("UnitUseItemPoint(hero, nil, 50, 0)")]
    [InlineData("UnitUseItemPoint(nil, item, 50, 0)")]
    public void A_call_with_a_missing_unit_item_or_target_or_the_item_of_another_unit_or_a_dead_unit_is_refused(string use)
    {
        var lines = Run("wlsd", use);

        Assert.StartsWith("answer false inside  order 0", lines[0].Replace('\t', ' '));
    }
}
