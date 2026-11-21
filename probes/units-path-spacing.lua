-- How near two units come: a walker goes 1200 along a line through an idle unit of the same player (pairs on rows 400 apart, all at once),
-- and per pair at 8 s: the nearest their centres came (rounded to 2), the most the walker left its line and the idle unit moved (to 8), and
-- whether the walker arrived. Then each pair again, the walker put by SetUnitPosition on the idle unit's place: how far from it it stands.
local p = Player(0)
local pairs = { { "hfoo", "hfoo" }, { "hpea", "hpea" }, { "hkni", "hkni" }, { "uabo", "uabo" }, { "hfoo", "uabo" }, { "hpea", "hfoo" }, { "ewsp", "ewsp" }, { "hpea", "uabo" } }
local runs = {}
for i, pair in ipairs(pairs) do
  local y = (i - 4.5) * 400
  local still = CreateUnit(p, FourCC(pair[2]), 0, y, 0)
  local w = CreateUnit(p, FourCC(pair[1]), -600, y, 0)
  IssuePointOrder(w, "move", 600, y)
  runs[i] = { w = w, still = still, y = y, near = 99999, dev = 0, moved = 0, sx = GetUnitX(still), sy = GetUnitY(still) }
end
local t = 0
local timer = CreateTimer()
TimerStart(timer, 0.05, true, function()
  t = t + 1
  for _, r in ipairs(runs) do
    local x, y = GetUnitX(r.w), GetUnitY(r.w)
    local ox, oy = GetUnitX(r.still), GetUnitY(r.still)
    r.near = math.min(r.near, SquareRoot((x - ox) ^ 2 + (y - oy) ^ 2))
    r.dev = math.max(r.dev, math.abs(y - r.y))
    r.moved = math.max(r.moved, SquareRoot((ox - r.sx) ^ 2 + (oy - r.sy) ^ 2))
  end
  if t >= 160 then
    PauseTimer(timer)
    for i, r in ipairs(runs) do
      local arrived = SquareRoot((GetUnitX(r.w) - 600) ^ 2 + (GetUnitY(r.w) - r.y) ^ 2) < 40
      out(string.format("%s past %s: nearest %d, left its line by %d, idle one moved %d, %s", pairs[i][1], pairs[i][2], math.floor(r.near / 2 + 0.5) * 2,
        math.floor(r.dev / 8 + 0.5) * 8, math.floor(r.moved / 8 + 0.5) * 8, arrived and "arrived" or "not arrived"))
      SetUnitPosition(r.w, GetUnitX(r.still), GetUnitY(r.still))
      out(string.format("%s put on %s: stands %d from it", pairs[i][1], pairs[i][2],
        math.floor(SquareRoot((GetUnitX(r.w) - GetUnitX(r.still)) ^ 2 + (GetUnitY(r.w) - GetUnitY(r.still)) ^ 2) / 2 + 0.5) * 2))
    end
    done()
  end
end)
