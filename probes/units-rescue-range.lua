-- The range a rescuable neutral unit has until SetUnitRescueRange changes it, found by distance: a neutral passive footman and hero made
-- rescuable by Player 0 at distances from a footman of Player 0 that stands still, and a footman by a hero, which of them were rescued by the first
-- check 0.5 s after (the units are far enough apart that no other rescuer is near). A building isn't tried: it stands on the pathing grid.
local rescued = {}
local t = CreateTrigger()
TriggerRegisterPlayerUnitEvent(t, Player(PLAYER_NEUTRAL_PASSIVE), EVENT_PLAYER_UNIT_RESCUED, nil)
TriggerAddAction(t, function() rescued[GetTriggerUnit()] = true end)

local cases = {}
local function round(at, kind, rescuerType, distances)
  TimerStart(CreateTimer(), at, false, guard(function()
    local made = {}
    for i, d in ipairs(distances) do
      local col, row = (i - 1) % 3, (i - 1) // 3
      local x, y = col * 1400 - 1400, row * 900 - 1350
      local r = CreateUnit(Player(0), FourCC(rescuerType), x, y, 0)
      local u = CreateUnit(Player(PLAYER_NEUTRAL_PASSIVE), FourCC(kind), x + d, y, 0)
      SetUnitRescuable(u, Player(0), true)
      made[#made + 1] = { d, u }
      made[#made + 1] = { 0, r }
    end
    TimerStart(CreateTimer(), 0.9, false, guard(function()
      local got, missed = {}, {}
      for i = 1, #made, 2 do
        local d, u = made[i][1], made[i][2]
        if rescued[u] then got[#got + 1] = d else missed[#missed + 1] = d end
      end
      out(kind .. " by " .. rescuerType .. " rescued " .. table.concat(got, ",") .. " not " .. table.concat(missed, ","))
      for _, m in ipairs(made) do RemoveUnit(m[2]) end
    end))
  end))
end

local fine = { 405, 410, 411, 412, 413, 414, 415, 416, 417, 418, 419, 420 }
round(1.0, "hfoo", "hfoo", fine)
round(3.0, "Hpal", "hfoo", fine)
round(5.0, "hfoo", "Hpal", fine)
TimerStart(CreateTimer(), 7.5, false, guard(function() done() end))
