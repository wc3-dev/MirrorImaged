-- When an added good comes on sale (standard data): a hostile Marketplace is given a good for 3 at the start of the game, or at 5 s, and bought from every 0.1 s until one buy
-- answers true; the line gives the time of that buy after the add. Neutral shops are bought from with IssueNeutralImmediateOrderById by the one hero of player 0 beside them.
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
local clock = CreateTimer()
TimerStart(clock, 1000, false, nil)
local function watch(label, shop, hero, item, from)
  local first
  local t = CreateTimer()
  TimerStart(t, 0.1, true, guard(function()
    if first then return end
    if buy(shop, hero, item):sub(1, 1) == "t" then
      first = TimerGetElapsed(clock) - from
      out(string.format("%s: first bought %.1f s after the add", label, first))
      PauseTimer(t)
    end
  end))
end
local a, ha = shop("nmrk", HOST, 0, 0)
local b, hb = shop("nmrk", HOST, 2000, 0)
AddItemToStock(a, claws, 3, 3)
watch("added at 0", a, ha, claws, 0)
step(5, function()
  AddItemToStock(b, claws, 3, 3)
  watch("added at 5 s", b, hb, claws, TimerGetElapsed(clock))
end)
step(4, function() out("end") end)
run(1)
