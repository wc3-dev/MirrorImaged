-- The stock natives (standard data): what a shop sells once goods are added to its stock, counted, taken off, and added to every shop. Neutral shops are bought
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

local a, ha = shop("nmrk", PASS, 0, 0)
local b, hb = shop("nmrk", HOST, 2000, 0)
local c, hc = shop("nmrk", HOST, 0, 2000)
local d, hd = shop("nmrk", PASS, 2000, 2000)
local e, he = shop("nmrk", HOST, -2000, 0)
local g, hg = shop("nmrk", HOST, -2000, -2000)
local farm = CreateUnit(P0, id("hfar"), -2000, 2000, 0)
local camp, hcamp = shop("nmer", PASS, 0, -2000)
out(string.format("abilities Asid %d Asud %d Apit %d on the passive shop; Asid %d on the hostile one", GetUnitAbilityLevel(a, id("Asid")), GetUnitAbilityLevel(a, id("Asud")), GetUnitAbilityLevel(a, id("Apit")), GetUnitAbilityLevel(b, id("Asid"))))

-- Everything is added in the instant the game starts; the hostile shop g is bought from at once and as time passes, to see when an added good comes on sale
AddItemToStock(a, claws, 1, 2)
AddItemToStock(b, claws, 3, 3)
AddItemToStock(c, claws, 0, 2)
AddItemToStock(d, claws, 2, 2)
AddItemToStock(e, claws, 1, 1)
AddItemToStock(g, claws, 9, 9)
local when = {}
out("g at once: " .. buys(g, hg, { claws }))
for _, t in ipairs({ 0.25, 0.5, 1, 1.5 }) do
  step(t - (when[#when] or 0), function() out("g at " .. t .. " s: " .. buys(g, hg, { claws })) end)
  when[#when + 1] = t
end
step(0.5, function()
  RemoveItemFromStock(e, claws)
end)
step(0.5, function()
  out("passive 1,2: " .. buys(a, ha, { claws, claws }))
  out("hostile 3,3: " .. buys(b, hb, { claws, claws, claws, claws }))
  out("hostile 0,2: " .. buys(c, hc, { claws }))
  out("passive 2,2: " .. buys(d, hd, { claws, claws, claws }))
  out("hostile 1,1 then removed: " .. buys(e, he, { claws }))
  AddItemToStock(c, claws, 2, 2)
  AddItemToStock(b, claws, 5, 3)
  AddItemToStock(e, claws, 1, 3)
  AddItemToStock(e, claws, 2, 3)
  AddItemToStock(a, claws6, -1, 3)
end)
step(2, function()
  out("hostile 0,2 then 2,2: " .. buys(c, hc, { claws, claws, claws }))
  out("hostile 5,3: " .. buys(b, hb, { claws, claws, claws, claws, claws, claws }))
  out("hostile 1,3 then 2,3: " .. buys(e, he, { claws, claws, claws }))
  out("passive -1,3: " .. buys(a, ha, { claws6 }))
  AddItemToAllStock(ring, 2, 2)
end)
local after, hafter
step(0.1, function() after, hafter = shop("nmrk", PASS, 4000, 0) end)
step(2, function()
  out("all stock 2,2: a " .. buys(a, ha, { ring, ring, ring }) .. ", hostile b " .. buys(b, hb, { ring, ring, ring }) .. ", camp " .. buys(camp, hcamp, { ring }) .. ", made after " .. buys(after, hafter, { ring }))
  RemoveItemFromAllStock(ring)
  AddItemToAllStock(regen, 1, 1)
  RemoveItemFromAllStock(regen)
end)
step(2, function()
  out("all stock then removed: " .. buys(c, hc, { ring }) .. " " .. buys(d, hd, { regen, regen }))
  UnitAddAbility(farm, id("Asud"))
  AddUnitToStock(farm, id("hfoo"), 1, 1)
end)
step(2, function()
  local ok = IssueImmediateOrderById(farm, id("hfoo"))
  out(string.format("farm with Sell Units, hfoo 1,1: order %s, order id %d", tostring(ok), GetUnitCurrentOrder(farm)))
  RemoveUnitFromStock(farm, id("hfoo"))
  out("farm after RemoveUnitFromStock: " .. tostring(IssueImmediateOrderById(farm, id("hfoo"))))
  AddUnitToAllStock(id("hfoo"), 1, 1)
end)
step(2, function()
  out("camp after AddUnitToAllStock: " .. tostring(IssueNeutralImmediateOrderById(P0, camp, id("hfoo"))))
  RemoveUnitFromAllStock(id("hfoo"))
end)
run(1)
