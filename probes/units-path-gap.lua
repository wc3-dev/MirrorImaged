-- A group through a gap: two houses face to face on x = 0 with their centres 128 to 320 apart (5 gaps at once, on rows 450 apart), and 4
-- footmen from 1000 away, spread 48 apart across the gap, each ordered to a point 1000 beyond it on its own lane. Per gap, at 14 s: how many have
-- come within 40 of their point, the seconds the first and the last took (0.05 s; never when not all came), and the most they left their lane.
local p = Player(0)
local centres = { 128, 160, 192, 256, 320 }
local runs = {}
for i, c in ipairs(centres) do
  local y = (i - 3) * 450
  local r = { y = y, c = c, hs = {}, dev = 0, times = {} }
  CreateUnit(p, FourCC("hhou"), 0, y + c / 2, 270)
  CreateUnit(p, FourCC("hhou"), 0, y - c / 2, 270)
  for k = 1, 4 do
    local w = CreateUnit(p, FourCC("hfoo"), -1000, y + (k - 2.5) * 48, 0)
    r.hs[k] = w
    IssuePointOrder(w, "move", 1000, y + (k - 2.5) * 48)
  end
  runs[i] = r
end
local t = 0
local timer = CreateTimer()
TimerStart(timer, 0.05, true, function()
  t = t + 1
  for _, r in ipairs(runs) do
    for k, w in ipairs(r.hs) do
      local x, y = GetUnitX(w), GetUnitY(w)
      r.dev = math.max(r.dev, math.abs(y - (r.y + (k - 2.5) * 48)))
      if r.times[k] == nil and SquareRoot((x - 1000) ^ 2 + (y - (r.y + (k - 2.5) * 48)) ^ 2) < 40 then r.times[k] = t * 0.05 end
    end
  end
  if t >= 280 then
    PauseTimer(timer)
    for _, r in ipairs(runs) do
      local n, first, last = 0, 99, 0
      for _, v in pairs(r.times) do n = n + 1 first = math.min(first, v) last = math.max(last, v) end
      out(string.format("centres %d apart: %d of 4 arrived, first %s s, last %s s, left the lane by %d", r.c, n, n > 0 and string.format("%.2f", first) or "never",
        n == 4 and string.format("%.2f", last) or "never", math.floor(r.dev / 16 + 0.5) * 16))
    end
    done()
  end
end)
