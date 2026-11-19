-- Harvesting and rally points. Gold: 1, 3, 5 and 8 peasants at a gold mine 770 from a town hall (a melee base's spacing), then 5 acolytes
-- at a mine blighted for the player (a haunted mine) beside a necropolis; each for 16 s from the order: the gold taken from the mine at 4, 8, 12 and 16 s, when it was first
-- taken, and the most workers hidden at once (a worker is hidden while it is in the mine). Lumber, at the same time: 2 peasants (2 ghouls
-- with the acolytes) at trees 500 from the hall: the player's lumber at 16 s. Rally: a barracks given smart at a place, and the hall given
-- smart at the mine, then a peasant trained there: its order once made.
local p = Player(0)
local np = Player(PLAYER_NEUTRAL_PASSIVE)
SetPlayerState(p, PLAYER_STATE_RESOURCE_FOOD_CAP, 100)
SetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD, 1000)
local phases = { { "hpea", 1, "hpea" }, { "hpea", 3, "hpea" }, { "hpea", 5, "hpea" }, { "hpea", 8, "hpea" }, { "uaco", 5, "ugho" } }
local hall, mine, trained
local phase, t, first, most, workers, line, left = 0, 0, -1, 0, {}, "", 0
for i = 0, 9 do CreateDestructable(FourCC("LTlt"), -500 - 64 * (i % 2), -64 * i, 0, 1, 0) end

local b = CreateUnit(p, FourCC("hbar"), -1400, 1000, 270)
out("barracks smart " .. tostring(IssuePointOrder(b, "smart", -1000, 600)) .. " order " .. GetUnitCurrentOrder(b) .. " rally " ..
  string.format("%.0f %.0f", GetLocationX(GetUnitRallyPoint(b)), GetLocationY(GetUnitRallyPoint(b))))

local function start()
  phase = phase + 1
  for _, w in ipairs(workers) do RemoveUnit(w) end
  workers = {}
  if phase > #phases then
    done()
    return
  end
  local kind, count, cutter = phases[phase][1], phases[phase][2], phases[phase][3]
  if hall == nil or (kind == "uaco") ~= (GetUnitTypeId(hall) == FourCC("unpl")) then
    if hall ~= nil then RemoveUnit(hall) RemoveUnit(mine) end
    hall = CreateUnit(p, FourCC(kind == "uaco" and "unpl" or "htow"), 0, 0, 270)
    mine = CreateUnit(np, FourCC("ngol"), 512, -576, 270)
    if kind == "uaco" then
      -- as blizzard.j's BlightGoldMineForPlayerBJ makes one
      local gold = GetResourceAmount(mine)
      RemoveUnit(mine)
      mine = CreateBlightedGoldmine(p, 512, -576, 270)
      SetResourceAmount(mine, gold)
    end
  end
  for i = 1, count do
    local w = CreateUnit(p, FourCC(kind), 160 + 20 * i, -200 - 10 * i, 270)
    workers[#workers + 1] = w
    IssueTargetOrder(w, "harvest", mine)
  end
  for i = 1, 2 do
    local w = CreateUnit(p, FourCC(cutter), -200, -100 * i, 270)
    workers[#workers + 1] = w
    local tree
    EnumDestructablesInRect(Rect(-700, -700, -400, 100), nil, function()
      if tree == nil and GetDestructableLife(GetEnumDestructable()) > 0 then tree = GetEnumDestructable() end
    end)
    IssueTargetOrder(w, "harvest", tree)
  end
  SetPlayerState(p, PLAYER_STATE_RESOURCE_LUMBER, 0)
  if phase == 1 then
    out("hall smart " .. tostring(IssueTargetOrder(hall, "smart", mine)) .. " rally unit " .. tostring(GetUnitRallyUnit(hall) == mine))
    out("train " .. tostring(IssueImmediateOrderById(hall, FourCC("hpea"))))
    local tr = CreateTrigger()
    TriggerRegisterUnitEvent(tr, hall, EVENT_UNIT_TRAIN_FINISH)
    TriggerAddAction(tr, function()
      trained = GetTrainedUnit()
      TimerStart(CreateTimer(), 0.5, false, function()
        out("trained order " .. GetUnitCurrentOrder(trained) .. " at mine " .. tostring(GetUnitCurrentOrder(trained) == 852018))
        RemoveUnit(trained)
      end)
    end)
  end
  t, first, most, line, left = 0, -1, 0, kind .. " " .. count .. ":", GetResourceAmount(mine)
end

start()
TimerStart(CreateTimer(), 0.1, true, function()
  t = t + 1
  local taken = left - GetResourceAmount(mine)
  if first < 0 and taken > 0 then first = t end
  local hidden = 0
  for _, w in ipairs(workers) do if IsUnitHidden(w) then hidden = hidden + 1 end end
  if hidden > most then most = hidden end
  if t % 40 == 0 then line = line .. " " .. taken end
  if t == 160 then
    out(string.format("%s first %.1f hidden %d lumber %d", line, first / 10, most, GetPlayerState(p, PLAYER_STATE_RESOURCE_LUMBER)))
    start()
  end
end)
