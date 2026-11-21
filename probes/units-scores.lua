-- GetPlayerScore for each of its 25 kinds (0 to 24, as PLAYER_SCORE_* number them) at the start, after units, a
-- building and a hero are made, after kills (a footman, a hero, a building, a unit with no killer, one of the player's own),
-- items put in the hero's pack, one picked up by an order, a research, and up to 60 s into a unit's training, a building's construction and a worker's
-- mining, which run together. The scores are read as a line of 25 numbers.
local function name(n) return n == 0 and "0" or string.char(n >> 24 & 255, n >> 16 & 255, n >> 8 & 255, n & 255) end
local function scores(p)
  local s = {}
  for i = 0, 24 do s[#s + 1] = GetPlayerScore(p, ConvertPlayerScore(i)) end
  return table.concat(s, " ")
end
local steps = {}
local function step(wait, label, f) steps[#steps + 1] = { wait, label, f } end
local function run(i)
  if i > #steps then done() return end
  TimerStart(CreateTimer(), steps[i][1], false, guard(function()
    steps[i][3]()
    out(steps[i][2] .. " " .. scores(Player(0)) .. " structures " .. GetPlayerStructureCount(Player(0), true) .. " units " .. GetPlayerUnitCount(Player(0), true))
    run(i + 1)
  end))
end

local p0, enemy = Player(0), Player(PLAYER_NEUTRAL_AGGRESSIVE)
SetPlayerState(p0, PLAYER_STATE_RESOURCE_GOLD, 5000)
SetPlayerState(p0, PLAYER_STATE_RESOURCE_LUMBER, 2000)
SetPlayerState(p0, PLAYER_STATE_RESOURCE_FOOD_CAP, 100)
out("start " .. scores(p0))
local foo, bar, hall, hero, pea, mine
step(0.1, "made", function()
  foo = CreateUnit(p0, FourCC("hfoo"), 0, 0, 0)
  CreateUnit(p0, FourCC("hfoo"), 100, 0, 0)
  bar = CreateUnit(p0, FourCC("hbar"), -1500, -1500, 0)
  hall = CreateUnit(p0, FourCC("htow"), 1300, 1000, 0)
  hero = CreateUnit(p0, FourCC("Hpal"), 0, 200, 0)
end)
step(0.5, "later", function() end)
local function kill(label, victimOwner, type, byUnit)
  step(0.1, label, function()
    local victim = CreateUnit(victimOwner, FourCC(type), 300, 300, 0)
    if byUnit then UnitDamageTarget(byUnit(), victim, 100000, true, false, ATTACK_TYPE_NORMAL, DAMAGE_TYPE_NORMAL, WEAPON_TYPE_WHOKNOWS)
    else KillUnit(victim) end
  end)
end
kill("killed footman", enemy, "hfoo", function() return foo end)
kill("killed hero", enemy, "Hpal", function() return foo end)
kill("killed building", enemy, "hbar", function() return foo end)
kill("killed with no killer", enemy, "hfoo")
kill("killed by hero", enemy, "hfoo", function() return hero end)
step(0.1, "own killed by enemy", function()
  local mine = CreateUnit(p0, FourCC("hfoo"), 500, 500, 0)
  local foe = CreateUnit(enemy, FourCC("hfoo"), 520, 500, 0)
  UnitDamageTarget(foe, mine, 100000, true, false, ATTACK_TYPE_NORMAL, DAMAGE_TYPE_NORMAL, WEAPON_TYPE_WHOKNOWS)
  RemoveUnit(foe)
end)
step(0.1, "own killed by self", function()
  KillUnit(CreateUnit(p0, FourCC("hfoo"), 500, 500, 0))
end)
step(0.1, "items", function()
  UnitAddItemById(hero, FourCC("phea"))
  UnitAddItemById(hero, FourCC("rst1"))
  UnitAddItemById(hero, FourCC("tdex"))
  CreateItem(FourCC("phea"), 600, 600)
end)
step(0.1, "researched", function() SetPlayerTechResearched(p0, FourCC("Rhme"), 1) end)
step(0.1, "begun", function()
  IssueImmediateOrderById(bar, FourCC("hfoo"))
  IssueImmediateOrderById(bar, FourCC("hfoo"))
  pea = CreateUnit(p0, FourCC("hpea"), -1000, -1300, 0)
  out("build order " .. tostring(IssueBuildOrderById(pea, FourCC("hhou"), -1000, -1500)))
  mine = CreateUnit(Player(PLAYER_NEUTRAL_PASSIVE), FourCC("ngol"), 1000, 1000, 0)
  local miner = CreateUnit(p0, FourCC("hpea"), 1200, 1000, 0)
  IssueTargetOrder(miner, "harvest", mine)
end)
step(0.1, "pick up", function()
  local item = CreateItem(FourCC("phea"), 30, 200)
  out("pick up order " .. tostring(IssueTargetOrderById(hero, 851971, item)))
end)
step(3, "picked up", function() end)
step(19, "25 s", function() end)
step(21, "46 s", function() end)
step(14, "60 s", function() end)
run(1)
