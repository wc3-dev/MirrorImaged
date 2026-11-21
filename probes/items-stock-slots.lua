-- The slot natives of the stock (standard data): how many added goods a shop sells when its slots are set, before and after it is made. Neutral shops are bought
-- from with IssueNeutralImmediateOrderById by the one hero of player 0 beside them (the hero's bag is emptied after each buy). A line gives, for each
-- buy, whether it answered true, whether gold was taken and the type of the item the hero then held.
local P0 = Player(0)
local PASS, HOST = Player(PLAYER_NEUTRAL_PASSIVE), Player(PLAYER_NEUTRAL_AGGRESSIVE)
SetPlayerState(P0, PLAYER_STATE_RESOURCE_GOLD, 1000000)
SetPlayerState(P0, PLAYER_STATE_RESOURCE_FOOD_CAP, 50)
local function id(s) return FourCC(s) end
local claws, claws6, ring, regen = id("ratc"), id("rat6"), id("rde1"), id("rlif")
local function buy(shop, hero, item)
  local before = GetPlayerState(P0, PLAYER_STATE_RESOURCE_GOLD)
  local ok = IssueNeutralImmediateOrderById(P0, shop, item)
  local paid = GetPlayerState(P0, PLAYER_STATE_RESOURCE_GOLD) < before
  local held = UnitItemInSlot(hero, 0)
  local kind = held and string.format("%c%c%c%c", GetItemTypeId(held) >> 24, GetItemTypeId(held) >> 16 & 255, GetItemTypeId(held) >> 8 & 255, GetItemTypeId(held) & 255) or "-"
  if held then RemoveItem(held) end
  return string.format("%s/%s/%s", ok and "t" or "f", paid and "p" or "-", kind)
end
local function buys(shop, hero, list)
  local t = {}
  for _, item in ipairs(list) do t[#t + 1] = buy(shop, hero, item) end
  return table.concat(t, " ")
end
local function shop(kind, owner, x, y)
  local s = CreateUnit(owner, id(kind), x, y, 0)
  local h = CreateUnit(P0, id("Hpal"), x + 100, y, 0)
  return s, h
end

local steps = {}
local function step(wait, f) steps[#steps + 1] = {wait, f} end
local function run(i)
  if i > #steps then done() return end
  TimerStart(CreateTimer(), steps[i][1], false, guard(function() steps[i][2]() run(i + 1) end))
end

local goods = { claws, claws6, ring, regen }
local function fill(s) for _, g in ipairs(goods) do AddItemToStock(s, g, 1, 1) end end
local a, ha = shop("nmrk", PASS, 0, 0)
local b, hb = shop("nmrk", PASS, 2000, 0)
local c, hc = shop("nmrk", PASS, 0, 2000)
local d, hd = shop("nmrk", PASS, 2000, 2000)
local e, he = shop("nmrk", PASS, -2000, 0)
SetItemTypeSlots(c, 2)
SetItemTypeSlots(b, 1)
SetItemTypeSlots(d, 0)
SetItemTypeSlots(e, 11)
SetAllItemTypeSlots(1)
local f, hf = shop("nmrk", PASS, -2000, -2000)
SetAllItemTypeSlots(12)
local g, hg = shop("nmrk", PASS, 0, -2000)
for _, s in ipairs({ a, b, c, d, e, f, g }) do fill(s) end
step(3, function()
  out("default, 4 goods: " .. buys(a, ha, goods))
  out("slots 1 set after the shop is made: " .. buys(b, hb, goods))
  out("slots 2: " .. buys(c, hc, goods))
  out("slots 0: " .. buys(d, hd, goods))
  out("slots 11: " .. buys(e, he, goods))
  out("made, then SetAllItemTypeSlots(1) and (12): " .. buys(f, hf, goods))
  out("made after SetAllItemTypeSlots(12): " .. buys(g, hg, goods))
end)
run(1)
