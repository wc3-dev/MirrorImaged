using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>A shop sells the items and units it lists to a player with a unit near, for the price, and fires the sell events.</summary>
public sealed class ShopTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static Dictionary<string, string> Item(string own)
    {
        var values = Values("ihtp=75;ilev=1;isel=1;ipaw=1;iper=0;ipow=0;iprn=0;iusa=0;idro=1;idrp=0;ipri=100;iarm=Wood;isca=1;ifil=Models/chest.mdl;icid=AIxx;icla=Permanent;iuse=0");
        foreach (var (key, value) in Values(own)) values[key] = value;
        return values;
    }

    static StandardData Standard() => new StandardData()
        .Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=420;umpm=0;umpi=0;umvs=270;umvt=foot;ugol=135;ufoo=2;uaen=0;ucol=31"))
        .Add(ObjectKind.Unit, "nmrk", Values("unam=Marketplace;uhpm=1500;umpm=0;umpi=0;ubdg=1;umvs=0;usei=phea;ucol=80"))
        .Add(ObjectKind.Unit, "ntav", Values("unam=Tavern;uhpm=1500;umpm=0;umpi=0;ubdg=1;umvs=0;useu=hfoo;ucol=80"))
        .Add(ObjectKind.Item, "phea", Item("unam=Potion of Healing;igol=100;ilum=0;iabi=AIh1"));

    List<string> Run(string main, double seconds)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            function config() end
            function main()
                SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, 300)
                SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_FOOD_CAP, 20)
            {{main}}
            end
            """), "", data: Standard());
        simulation.Advance(TimeSpan.FromSeconds(seconds));
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];
    }

    [Fact]
    public void A_player_buys_an_item_from_a_shop_for_its_price_when_a_unit_of_it_is_near_and_a_unit_from_a_tavern()
    {
        var lines = Run("""
            local shop, tavern = CreateUnit(Player(PLAYER_NEUTRAL_PASSIVE), FourCC("nmrk"), 0, 0, 0), CreateUnit(Player(PLAYER_NEUTRAL_PASSIVE), FourCC("ntav"), 2000, 0, 0)
            local far = CreateUnit(Player(0), FourCC("hfoo"), 4000, 4000, 0)
            local t, u = CreateTrigger(), CreateTrigger()
            TriggerRegisterPlayerUnitEvent(t, Player(0), EVENT_PLAYER_UNIT_SELL_ITEM, nil)
            TriggerAddAction(t, function() print("item", GetItemName(GetSoldItem()), GetUnitName(GetSellingUnit()), GetUnitName(GetBuyingUnit())) end)
            TriggerRegisterPlayerUnitEvent(u, Player(0), EVENT_PLAYER_UNIT_SELL, nil)
            TriggerAddAction(u, function() print("unit", GetUnitName(GetSoldUnit()), GetUnitName(GetSellingUnit()), GetUnitName(GetBuyingUnit())) end)
            print("no one near", IssueNeutralImmediateOrderById(Player(0), shop, FourCC("phea")), GetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD))
            local buyer = CreateUnit(Player(0), FourCC("hfoo"), 100, 0, 0)
            print("bought", IssueNeutralImmediateOrderById(Player(0), shop, FourCC("phea")), GetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD), UnitItemInSlot(buyer, 0) ~= nil)
            SetUnitPosition(buyer, 2100, 0)
            print("recruited", IssueNeutralImmediateOrderById(Player(0), tavern, FourCC("hfoo")), GetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD))
            print("poor", IssueNeutralImmediateOrderById(Player(0), tavern, FourCC("hfoo")), GetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD))
            """, 2);

        Assert.Equal(["no one near\tfalse\t300", "item\tPotion of Healing\tMarketplace\tFootman", "bought\ttrue\t200\tfalse", "unit\tFootman\tTavern\tFootman", "recruited\ttrue\t65", "poor\tfalse\t65"], lines);
    }
}
