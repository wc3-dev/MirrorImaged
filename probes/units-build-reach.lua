-- How near its site a worker is when the building starts: a peasant ordered to build a farm and a barracks, and an acolyte a crypt, each
-- from 800 away. At the construct start event: the seconds since the order and the worker's distance from the site, then the building's
-- collision size. The crypt's site is on the blight of a necropolis 550 away.
local p = Player(0)
SetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD, 5000)
SetPlayerState(p, PLAYER_STATE_RESOURCE_LUMBER, 5000)
local t = 0
TimerStart(CreateTimer(), 0.05, true, function() t = t + 1 end)
CreateUnit(p, FourCC("unpl"), 800, 1150, 270)
local jobs = {}
local function build(worker, building, x, y)
  local w = CreateUnit(p, FourCC(worker), x - 800, y, 0)
  jobs[w] = { building, x, y, t }
  out(building .. " order " .. tostring(IssueBuildOrderById(w, FourCC(building), x, y)))
end
local tr = CreateTrigger()
TriggerRegisterPlayerUnitEvent(tr, p, EVENT_PLAYER_UNIT_CONSTRUCT_START, nil)
TriggerAddAction(tr, function()
  local b = GetConstructingStructure()
  for w, job in pairs(jobs) do
    if FourCC(job[1]) == GetUnitTypeId(b) then
      local dx, dy = GetUnitX(w) - job[2], GetUnitY(w) - job[3]
      out(string.format("%s start %.2f s distance %.0f site %.0f %.0f collision %.0f", job[1], (t - job[4]) * 0.05, SquareRoot(dx * dx + dy * dy),
        GetUnitX(b), GetUnitY(b), BlzGetUnitCollisionSize(b)))
    end
  end
end)
build("hpea", "hhou", -1000, -1500)
build("hpea", "hbar", -1000, 0)
build("uaco", "usep", 800, 600)
TimerStart(CreateTimer(), 12, false, function() done() end)
