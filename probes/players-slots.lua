-- The slots the game names (GetBJMaxPlayers, the neutral aggressive, victim, extra and passive ones) and what each slot
-- from 0 to 27 is as the game starts: its controller, slot state and name; on the probe map, whose info names the editor
-- version it was built with.
local function handle(h) return h == nil and "nil" or tostring(GetHandleId(h)) end
out("slots " .. GetBJMaxPlayers() .. " " .. GetPlayerNeutralAggressive() .. " " .. GetBJPlayerNeutralVictim() .. " " .. GetBJPlayerNeutralExtra()
  .. " " .. GetPlayerNeutralPassive())
out("constants " .. PLAYER_NEUTRAL_AGGRESSIVE .. " " .. PLAYER_NEUTRAL_PASSIVE)
for i = 0, 27 do
  local p = Player(i)
  out("slot " .. i .. " " .. handle(GetPlayerController(p)) .. " " .. handle(GetPlayerSlotState(p)) .. " [" .. GetPlayerName(p) .. "] team " .. GetPlayerTeam(p))
end
done()
