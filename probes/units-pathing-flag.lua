-- SetUnitPathing (standard data): a footman walks 800 east through a wall of five held footmen of its own player across its way 40 apart, with its
-- pathing on, off, and off then on again. A line gives where it stands after 6 s, the least and most it strayed north or south on the way, and whether
-- it got through.
local P0 = Player(0)
local jobs = {}
local clock = CreateTimer()
TimerStart(clock, 1000, false, nil)
local function now() return TimerGetElapsed(clock) end
local function later(delay, f) jobs[#jobs + 1] = {now() + delay, f} end
local rows, lines = 0, {}
local function walk(label, setup, undo)
  rows = rows + 1
  local row = rows
  local y = -1500 + 600 * rows
  local u = CreateUnit(P0, FourCC("hfoo"), -400, y, 0)
  for i = -2, 2 do
    local w = CreateUnit(P0, FourCC("hfoo"), 0, y + 40 * i, 0)
    IssueImmediateOrder(w, "holdposition")
  end
  if setup then setup(u) end
  IssuePointOrder(u, "move", 400, y)
  local least, most = 0, 0
  local ticker = CreateTimer()
  TimerStart(ticker, 0.05, true, guard(function()
    local dy = GetUnitY(u) - y
    least, most = math.min(least, dy), math.max(most, dy)
  end))
  if undo then later(undo[1], function() undo[2](u) end) end
  later(6, function()
    PauseTimer(ticker)
    lines[row] = (string.format("%s: got through %s, strayed %d to %d", label, tostring(GetUnitX(u) > 380), math.floor(least + 0.5), math.floor(most + 0.5)))
  end)
end
walk("pathing on", nil)
walk("pathing off", function(u) SetUnitPathing(u, false) end)
walk("pathing off then on at 0.5 s", function(u) SetUnitPathing(u, false) end, {0.5, function(u) SetUnitPathing(u, true) end})
local ticker = CreateTimer()
TimerStart(ticker, 0.01, true, guard(function()
  local n = now()
  table.sort(jobs, function(a, b) return a[1] < b[1] end)
  while #jobs > 0 and jobs[1][1] <= n do table.remove(jobs, 1)[2]() end
  if #jobs == 0 then
    PauseTimer(ticker)
    for _, line in ipairs(lines) do out(line) end
    done()
  end
end))
