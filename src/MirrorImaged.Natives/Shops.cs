using MirrorImaged.Data;

namespace MirrorImaged.Natives;

// Shops: a building that lists items (`usei`) or units (`useu`) it sells gives one to a player who orders it, for the price, to the player's unit nearest it
// within reach; the sell events fire with the item or the unit sold, the shop and the buyer. Close: a shop's stock is not kept (it always has the thing), nor
// is the stock's wait, and a hero is only bought as the units of a tavern are, for the price (docs/decisions.md, D382).
public sealed partial class GameNatives
{
    const int UnitSell = 286, UnitSellItem = 288, PlayerUnitSell = 269, PlayerUnitSellItem = 271, UnitPawnItem = 294, PlayerUnitPawnItem = 277;
    const float ShopReach = 900;
    static readonly int ItemsSold = Code("usei"), UnitsSold = Code("useu"), ItemGold = Code("igol"), ItemLumber = Code("ilum");

    /// <summary>The item or unit that was bought, in a sell event.</summary>
    [Native(Level.Close)] public item? GetSoldItem() => Response(UnitSellItem, PlayerUnitSellItem, UnitPawnItem, PlayerUnitPawnItem)?.Item;
    [Native(Level.Close)] public unit? GetSoldUnit() => Response(UnitSell, PlayerUnitSell)?.Other;

    /// <summary>The shop in a sell event.</summary>
    [Native(Level.Close)] public unit? GetSellingUnit() => Response(UnitSell, UnitSellItem, PlayerUnitSell, PlayerUnitSellItem, UnitPawnItem, PlayerUnitPawnItem)?.Unit;

    /// <summary>The unit that bought, in a sell event.</summary>
    [Native(Level.Close)] public unit? GetBuyingUnit() => Response(UnitSell, UnitSellItem, PlayerUnitSell, PlayerUnitSellItem, UnitPawnItem, PlayerUnitPawnItem)?.Target as unit;

    bool Sells(unit shop, int id) => Listed(shop, ItemsSold, id) || Listed(shop, UnitsSold, id);

    // The player buys: its unit nearest the shop pays and takes the item, or the shop makes the unit for it
    bool Buy(player p, unit shop, int id)
    {
        var buyer = Mine(p).Where(u => u != shop && !u.Constructing && Distance2(u.X - shop.X, u.Y - shop.Y) <= ShopReach * ShopReach).OrderBy(u => Distance2(u.X - shop.X, u.Y - shop.Y)).FirstOrDefault();
        if (buyer is null) return false;
        if (Listed(shop, ItemsSold, id) && Data.Object(ObjectKind.Item, id) is { } item)
        {
            var (gold, lumber) = (item.Integer(ItemGold), item.Integer(ItemLumber));
            if (!CanPay(p, gold, lumber)) return false;
            Pay(p, gold, lumber);
            if (CreateItem(id, buyer.X, buyer.Y) is not { } made) return false;
            UnitAddItem(buyer, made);
            FireUnit(new(shop, Item: made, Target: buyer), UnitSellItem, PlayerUnitSellItem, p);
            return true;
        }
        if (Data.Object(ObjectKind.Unit, id) is { } type)
        {
            var (gold, lumber, food) = (type.Integer(Cost), type.Integer(LumberCost), type.Integer(FoodUsed));
            if (!CanPay(p, gold, lumber) || p.States[FoodUsedState] + food > Math.Min(p.States[FoodCap], p.States[FoodCapCeiling])) return false;
            Pay(p, gold, lumber);
            if (Create(p, id, shop.X, shop.Y - shop.Type.Real(Collision) - 40, 270, id) is not { } made) return false;
            FireUnit(new(shop, Other: made, Target: buyer), UnitSell, PlayerUnitSell, p);
            return true;
        }
        return false;
    }

    static readonly int PawnAbility = Code("Apit");

    // A shop with the pawn ability that takes the item: one that is not "pawnable" is not taken
    bool Pawns(unit shop, item i) => IsBuildingNow(shop) && Find(shop, PawnAbility) is not null && i.Type.Integer(ItemPawnable) != 0;

    // The unit near the shop sells the item to it for a share of its price (the constants' PawnItemRate, half), and the pawn events fire for its player
    bool Pawn(unit seller, item i, unit shop)
    {
        if (Distance2(seller.X - shop.X, seller.Y - shop.Y) > ShopReach * ShopReach) return false;
        var rate = Data.Constant("PawnItemRate", 0.5f);
        var (gold, lumber) = ((int)(i.Type.Integer(ItemGold) * rate), (int)(i.Type.Integer(ItemLumber) * rate));
        SetPlayerState(seller.Owner, Convert<playerstate>(Gold), seller.Owner.States[Gold] + gold);
        SetPlayerState(seller.Owner, Convert<playerstate>(Lumber), seller.Owner.States[Lumber] + lumber);
        FireUnit(new(shop, Item: i, Target: seller), UnitPawnItem, PlayerUnitPawnItem, seller.Owner);
        UnitRemoveItem(seller, i);
        RemoveItem(i);
        return true;
    }
}
