-- Walking round a building: a footman walks 2000 along a line through the middle of a building and parallel to it, the line offset
-- 0 to 224 to one side, 3 lines at once on rows 1040 apart, each with its own building (a town hall, a necropolis, a barracks, a farm;
-- 8 phases, 3 offsets in each). Per line: the most the footman leaves its line (rounded to 8, the way round), the seconds it takes to come within 40 of the end
-- (0.05 s), and the nearest it came to the building's centre. A line no building touches takes 2000 / 270 s and leaves the line by nothing.
local p = Player(0)
local types = { "htow", "unpl", "hbar", "hhou" }
local sets = { { 0, 128, 192 }, { 96, 160, 224 } }
local phase = 0
local function start()
  phase = phase + 1
  if phase > #types * 2 then done() return end
  local kind, offsets = types[(phase + 1) // 2], sets[(phase - 1) % 2 + 1]
  local runs = {}
  for i, off in ipairs(offsets) do
    local y = (i - 2) * 1040
    local b = CreateUnit(p, FourCC(kind), 0, y, 270)
    local w = CreateUnit(p, FourCC("hfoo"), -1000, y + off, 0)
    IssuePointOrder(w, "move", 1000, y + off)
    runs[i] = { b = b, w = w, y = y + off, cy = y, dev = 0, near = 99999, t = nil }
  end
  local t = 0
  local timer = CreateTimer()
  TimerStart(timer, 0.05, true, function()
    t = t + 1
    for _, r in ipairs(runs) do
      local x, y = GetUnitX(r.w), GetUnitY(r.w)
      r.dev = math.max(r.dev, math.abs(y - r.y))
      r.near = math.min(r.near, SquareRoot(x * x + (y - r.cy) ^ 2))
      if r.t == nil and SquareRoot((x - 1000) ^ 2 + (y - r.y) ^ 2) < 40 then r.t = t end
    end
    if t >= 200 then
      PauseTimer(timer) DestroyTimer(timer)
      for i, r in ipairs(runs) do
        out(string.format("%s offset %d: deviation %d, arrived %s s, nearest %d, collision %.0f", kind, offsets[i], math.floor(r.dev / 8 + 0.5) * 8,
          r.t and string.format("%.2f", r.t * 0.05) or "never", math.floor(r.near / 8 + 0.5) * 8, BlzGetUnitCollisionSize(r.b)))
        RemoveUnit(r.b) RemoveUnit(r.w)
      end
      start()
    end
  end)
end
start()
