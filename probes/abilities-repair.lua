-- Repair, to measure in the game (the runner could not log in to Battle.net from the agent's session, so it has not been run): a peasant given the repair order on a barracks hurt to
-- 300 of its life: the building's life and the player's gold and lumber every 2 seconds until it is whole, and what it cost in all. MirrorImaged repairs a full building in
-- its build time times 1.5 for 35% of its price (decisions D387); this gives the real ratios. To run it, copy it to probes/ and run the probe "repair" with the game runner, then
-- record the lines in probes/expected/repair.txt.
local p = Player(0)
SetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD, 2000)
SetPlayerState(p, PLAYER_STATE_RESOURCE_LUMBER, 2000)
SetPlayerState(p, PLAYER_STATE_RESOURCE_FOOD_CAP, 20)
local b = CreateUnit(p, FourCC("hbar"), 0, 0, 0)
local w = CreateUnit(p, FourCC("hpea"), 250, 0, 0)
local most = GetUnitState(b, UNIT_STATE_MAX_LIFE)
SetWidgetLife(b, 300)
out("start life " .. GetWidgetLife(b) .. " of " .. most)
out("order " .. tostring(IssueTargetOrder(w, "repair", b)))
local n = 0
TimerStart(CreateTimer(), 2, true, function()
  n = n + 1
  local life = GetWidgetLife(b)
  out(string.format("t=%d life=%.0f gold=%d lumber=%d order=%d", n * 2, life, GetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD),
    GetPlayerState(p, PLAYER_STATE_RESOURCE_LUMBER), GetUnitCurrentOrder(w)))
  if life >= most - 1 or n >= 60 then
    out(string.format("spent gold=%d lumber=%d", 2000 - GetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD), 2000 - GetPlayerState(p, PLAYER_STATE_RESOURCE_LUMBER)))
    done()
  end
end)
