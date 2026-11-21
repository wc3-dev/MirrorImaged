-- Where a unit can stand next to a building, finer: each building made alone at (0, 0); a unit of each size put by SetUnitPosition every 4
-- along the row through the building's centre and the column through it (from -300 to 296, 8 to the side of the centre), # where it is moved off.
local p = Player(0)
local buildings = { "htow", "hhou", "unpl" }
local walkers = { "hpea", "hfoo", "hkni", "uabo" }
local i = 0
local timer = CreateTimer()
TimerStart(timer, 0.1, true, function()
  i = i + 1
  if i > #buildings then PauseTimer(timer) done() return end
  local b = CreateUnit(p, FourCC(buildings[i]), 0, 0, 270)
  local cx, cy = GetUnitX(b), GetUnitY(b)
  for _, kind in ipairs(walkers) do
    local w = CreateUnit(p, FourCC(kind), 1500, 1500, 0)
    out(string.format("%s %s collision %.0f", buildings[i], kind, BlzGetUnitCollisionSize(w)))
    for axis = 1, 2 do
      local marks = {}
      for d = -300, 296, 4 do
        local x, y = cx + (axis == 1 and d or 8), cy + (axis == 1 and 8 or d)
        SetUnitPosition(w, x, y)
        marks[#marks + 1] = (math.abs(GetUnitX(w) - x) > 0.5 or math.abs(GetUnitY(w) - y) > 0.5) and "#" or "."
      end
      out(string.format("%s %s %s %s", buildings[i], kind, axis == 1 and "x" or "y", table.concat(marks)))
    end
    RemoveUnit(w)
  end
  RemoveUnit(b)
end)
