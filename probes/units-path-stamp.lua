-- A building's footprint, read straight: each building is made alone at (0, 0), and on a grid of 16 round where it stands (the cell
-- centres' quarters, 8 to 296 from its centre each way) two rows of marks a row of the grid, top first: "walk" is IsTerrainPathable's
-- walkability there (# blocked), "stand" whether a footman put there by SetUnitPosition stays there (# moved off). Before them, where a
-- building made off the grid stands.
local p = Player(0)
local types = { "htow", "unpl", "hbar", "hhou", "halt", "etol", "ugol" }
for _, at in ipairs({ { 40, 40 }, { 8, 8 }, { -50, 70 }, { 0, 0 } }) do
  local b = CreateUnit(p, FourCC("hbar"), at[1], at[2], 270)
  out(string.format("hbar made at %d,%d stands %.0f,%.0f", at[1], at[2], GetUnitX(b), GetUnitY(b)))
  RemoveUnit(b)
end
local f = CreateUnit(p, FourCC("hfoo"), 1500, 1500, 0)
local i = 0
local timer = CreateTimer()
TimerStart(timer, 0.1, true, function()
  i = i + 1
  if i > #types then PauseTimer(timer) done() return end
  local b = CreateUnit(p, FourCC(types[i]), 0, 0, 270)
  local cx, cy = GetUnitX(b), GetUnitY(b)
  out(string.format("%s stands %.0f,%.0f, collision %.0f", types[i], cx, cy, BlzGetUnitCollisionSize(b)))
  for row = 18, -19, -1 do
    local y = cy + row * 16 + 8
    local walk, stand = {}, {}
    for col = -19, 18 do
      local x = cx + col * 16 + 8
      walk[#walk + 1] = IsTerrainPathable(x, y, PATHING_TYPE_WALKABILITY) and "#" or "."
      SetUnitPosition(f, x, y)
      stand[#stand + 1] = (math.abs(GetUnitX(f) - x) > 0.5 or math.abs(GetUnitY(f) - y) > 0.5) and "#" or "."
    end
    out(string.format("%s %4d walk %s", types[i], row * 16 + 8, table.concat(walk)))
    out(string.format("%s %4d stand %s", types[i], row * 16 + 8, table.concat(stand)))
  end
  SetUnitPosition(f, 1500, 1500)
  RemoveUnit(b)
end)
