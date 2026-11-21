-- The room a unit keeps from another: a unit stands at (0, 0) and another is put by SetUnitPosition at 0 to 158 from it (every 2), along x
-- and at 45 degrees; # where it is moved off. Each pair alone, the units' collision sizes first.
local p = Player(0)
local pairs = { { "hfoo", "hfoo" }, { "hpea", "hpea" }, { "hkni", "hkni" }, { "uabo", "uabo" }, { "hpea", "hfoo" }, { "hfoo", "uabo" }, { "hpea", "uabo" } }
for _, pair in ipairs(pairs) do
  local still = CreateUnit(p, FourCC(pair[2]), 0, 0, 0)
  local w = CreateUnit(p, FourCC(pair[1]), 1500, 1500, 0)
  local sx, sy = GetUnitX(still), GetUnitY(still)
  out(string.format("%s by %s: collision %.0f and %.0f, the one stands at %.0f,%.0f", pair[1], pair[2], BlzGetUnitCollisionSize(w), BlzGetUnitCollisionSize(still), sx, sy))
  for _, dir in ipairs({ { 1, 0 }, { 0.70710678, 0.70710678 } }) do
    local marks = {}
    for d = 0, 158, 2 do
      local x, y = sx + dir[1] * d, sy + dir[2] * d
      SetUnitPosition(w, x, y)
      marks[#marks + 1] = (math.abs(GetUnitX(w) - x) > 0.5 or math.abs(GetUnitY(w) - y) > 0.5) and "#" or "."
    end
    out(string.format("%s by %s %s %s", pair[1], pair[2], dir[2] == 0 and "x" or "diagonal", table.concat(marks)))
  end
  RemoveUnit(still) RemoveUnit(w)
end
done()
