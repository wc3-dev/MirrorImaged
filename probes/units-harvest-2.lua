-- Lumber and rally points over 45 s. A peasant at a tree from the start: the tree's life and the player's lumber each time they change. Two
-- town halls, one given smart at a gold mine and one at a tree, each training a peasant: the trained peasant's order 0.5, 3, 6 and 10 s after
-- it is made, and the gold taken from the mine and the life of the rallied tree then.
local p = Player(0)
local np = Player(PLAYER_NEUTRAL_PASSIVE)
SetPlayerState(p, PLAYER_STATE_RESOURCE_FOOD_CAP, 100)
SetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD, 1000)
local t = 0
local cut = CreateDestructable(FourCC("LTlt"), -500, 0, 0, 1, 0)
local rallied = CreateDestructable(FourCC("LTlt"), -500, 1200, 0, 1, 0)
local h1 = CreateUnit(p, FourCC("htow"), 0, 0, 270)
local h2 = CreateUnit(p, FourCC("htow"), 0, 1200, 270)
local mine = CreateUnit(np, FourCC("ngol"), 512, -576, 270)
local left = GetResourceAmount(mine)
local w = CreateUnit(p, FourCC("hpea"), -200, 0, 270)
out("harvest tree " .. tostring(IssueTargetOrder(w, "harvest", cut)) .. " life " .. GetDestructableLife(cut))
out("rally mine " .. tostring(IssueTargetOrder(h1, "smart", mine)) .. " tree " .. tostring(IssueTargetOrder(h2, "smart", rallied)))
IssueImmediateOrderById(h1, FourCC("hpea"))
IssueImmediateOrderById(h2, FourCC("hpea"))
local tr = CreateTrigger()
TriggerRegisterUnitEvent(tr, h1, EVENT_UNIT_TRAIN_FINISH)
TriggerRegisterUnitEvent(tr, h2, EVENT_UNIT_TRAIN_FINISH)
TriggerAddAction(tr, function()
  local made, from = GetTrainedUnit(), GetTriggerUnit() == h1 and "mine" or "tree"
  out(string.format("%.1f trained at %s", t / 10, from))
  for _, after in ipairs({ 0.5, 3, 6, 10 }) do
    TimerStart(CreateTimer(), after, false, function()
      out(string.format("%s +%.1f order %d mine taken %d tree life %.0f", from, after, GetUnitCurrentOrder(made), left - GetResourceAmount(mine), GetDestructableLife(rallied)))
    end)
  end
end)
local life, lumber = GetDestructableLife(cut), 0
TimerStart(CreateTimer(), 0.1, true, function()
  t = t + 1
  if GetDestructableLife(cut) ~= life then
    life = GetDestructableLife(cut)
    out(string.format("%.1f tree %.0f", t / 10, life))
  end
  if GetPlayerState(p, PLAYER_STATE_RESOURCE_LUMBER) ~= lumber then
    lumber = GetPlayerState(p, PLAYER_STATE_RESOURCE_LUMBER)
    out(string.format("%.1f lumber %d", t / 10, lumber))
  end
  if t == 450 then done() end
end)
