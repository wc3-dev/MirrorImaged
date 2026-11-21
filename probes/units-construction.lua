-- UnitSetConstructionProgress and UnitSetUpgradeProgress (standard data): farms are built by peasants 500 apart, each one's progress set to a
-- percentage a few seconds after its start; the line gives when each finished after its start and its life just before and after the set. Then
-- town halls upgraded to keeps, a barracks training a footman, and a finished farm.
local P0 = Player(0)
SetPlayerState(P0, PLAYER_STATE_RESOURCE_GOLD, 100000)
SetPlayerState(P0, PLAYER_STATE_RESOURCE_LUMBER, 100000)
SetPlayerState(P0, PLAYER_STATE_FOOD_CAP, 100)
local clock = CreateTimer()
TimerStart(clock, 1000, false, nil)
local function now() return TimerGetElapsed(clock) end
local jobs = {}
local function later(delay, f) jobs[#jobs + 1] = {now() + delay, f} end
local function id(s) return FourCC(s) end
local function life(u) return string.format("%.1f", GetWidgetLife(u)) end

local site = 0
local function place() site = site + 1 return -1500 + 500 * ((site - 1) % 7), -1500 + 500 * ((site - 1) // 7) end
local label, started, finished, built = {}, {}, {}, {}
local function farm(name, set, wait, set2, wait2)
  local x, y = place()
  label[x .. "," .. y] = name
  local p = CreateUnit(P0, id("hpea"), x + 150, y, 0)
  IssueBuildOrderById(p, id("hhou"), x, y)
  built[name] = {set = set, wait = wait, set2 = set2, wait2 = wait2}
end
farm("plain")
farm("set 50 at 5 s", 50, 5)
farm("set 100 at 5 s", 100, 5)
farm("set 0 at 5 s", 0, 5)
farm("set 150 at 5 s", 150, 5)
farm("set -20 at 5 s", -20, 5)
farm("set 25 at 5 s then 75 at 10 s", 25, 5, 75, 5)
farm("set 90 at 1 s", 90, 1)
farm("set 99 at 1 s", 99, 1)

local t = CreateTrigger()
TriggerRegisterPlayerUnitEvent(t, P0, EVENT_PLAYER_UNIT_CONSTRUCT_START, nil)
TriggerAddAction(t, guard(function()
  local b = GetConstructingStructure()
  local name
  for key, n in pairs(label) do
    local x, y = key:match("(-?%d+),(-?%d+)")
    if math.abs(GetUnitX(b) - x) < 200 and math.abs(GetUnitY(b) - y) < 200 then name = n end
  end
  if not name then return end
  local spec = built[name]
  started[b] = {name = name, at = now(), unit = b}
  local line = {}
  if spec.set then
    later(spec.wait, function()
      local before = life(b)
      UnitSetConstructionProgress(b, spec.set)
      started[b].note = string.format("life %s -> %s", before, life(b))
      if spec.set2 then
        later(spec.wait2, function()
          local before2 = life(b)
          UnitSetConstructionProgress(b, spec.set2)
          started[b].note = started[b].note .. string.format(", then %s -> %s", before2, life(b))
        end)
      end
    end)
  end
end))
t = CreateTrigger()
TriggerRegisterPlayerUnitEvent(t, P0, EVENT_PLAYER_UNIT_CONSTRUCT_FINISH, nil)
TriggerAddAction(t, guard(function()
  local b = GetConstructedStructure()
  if started[b] then started[b].done = now() - started[b].at end
end))

local upgraded = {}
t = CreateTrigger()
TriggerRegisterPlayerUnitEvent(t, P0, EVENT_PLAYER_UNIT_UPGRADE_FINISH, nil)
TriggerAddAction(t, guard(function() upgraded[GetTriggerUnit()] = now() end))
-- Town halls to keeps, and a barracks training a footman
local halls = {}
local function hall(name, set, wait)
  local x, y = place()
  local h = CreateUnit(P0, id("htow"), x, y, 0)
  local ok = IssueImmediateOrderById(h, id("hkee"))
  halls[#halls + 1] = {u = h, name = name, set = set, wait = wait, at = now(), ok = ok}
  later(wait, function() UnitSetUpgradeProgress(h, set) halls[#halls].mark = true end)
end
hall("keep set 90 at 2 s", 90, 2)
hall("keep set 99 at 2 s", 99, 2)
hall("keep set 100 at 2 s", 100, 2)
local barracks = {}
local function trainer(name, set, wait)
  local x, y = place()
  local b = CreateUnit(P0, id("hbar"), x, y, 0)
  local ok = IssueImmediateOrderById(b, id("hfoo"))
  barracks[#barracks + 1] = {u = b, name = name, at = now(), ok = ok, x = x, y = y}
  if set then later(wait, function() UnitSetUpgradeProgress(b, set) end) end
end
trainer("footman plain")
trainer("footman, upgrade progress 90 at 2 s", 90, 2)
trainer("footman, construction progress 90 at 2 s")
later(2, function() UnitSetConstructionProgress(barracks[3].u, 90) end)
local done_farm = CreateUnit(P0, id("hhou"), 1500, 1500, 0)
local fl = life(done_farm)
UnitSetConstructionProgress(done_farm, 10)
out("a finished farm, construction progress 10: life " .. fl .. " -> " .. life(done_farm))

local seenHall, seenFoot = {}, {}
local last = now()
local ticker = CreateTimer()
TimerStart(ticker, 0.01, true, guard(function()
  local n = now()
  for i = #jobs, 1, -1 do
    if jobs[i][1] <= n then local j = table.remove(jobs, i) j[2]() end
  end
  for _, h in ipairs(halls) do
    if upgraded[h.u] and not h.finished then h.finished = upgraded[h.u] - h.at end
  end
  for _, b in ipairs(barracks) do
    if not b.finished then
      local g = CreateGroup()
      GroupEnumUnitsOfPlayer(g, P0, nil)
      -- a footman appears at the barracks' rally place, in front of it
      ForGroup(g, function()
        local f = GetEnumUnit()
        if GetUnitTypeId(f) == id("hfoo") and math.abs(GetUnitX(f) - b.x) < 300 and math.abs(GetUnitY(f) - b.y) < 300 then b.finished = n - b.at end
      end)
      DestroyGroup(g)
    end
  end
  if n > 60 then
    PauseTimer(ticker)
    local names = {}
    for _, s in pairs(started) do names[#names + 1] = s end
    table.sort(names, function(a, b) return a.name < b.name end)
    for _, s in ipairs(names) do out(string.format("farm %s: done %s%s; life at the end %s", s.name, s.done and string.format("%.2f", s.done) or "not", s.note and ("; " .. s.note) or "", life(s.unit))) end
    for _, h in ipairs(halls) do out(string.format("%s: order %s, finished %s, type %s", h.name, tostring(h.ok), h.finished and string.format("%.2f", h.finished) or "not", GetUnitName(h.u))) end
    for _, b in ipairs(barracks) do out(string.format("%s: order %s, footman %s", b.name, tostring(b.ok), b.finished and string.format("%.2f", b.finished) or "not")) end
    done()
  end
end))
