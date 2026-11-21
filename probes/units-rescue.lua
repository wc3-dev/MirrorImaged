-- Rescuing: a neutral passive unit made rescuable by Player 0, made at several moments and distances from a footman of
-- Player 0 that stands still. When the rescue events fire (each against the moment the unit was made), what GetRescuer,
-- the trigger unit and player read, and whether the owner changes; the range a unit has until SetUnitRescueRange
-- changes it, for a footman, a hero and a building; a unit rescuable by another player; one made rescuable after it
-- stood there; and a rescue switched off.
local clock = CreateTimer()
TimerStart(clock, 1000.0, false, nil)
local function now() return string.format("%.2f", TimerGetElapsed(clock)) end
local function name(n) return n == 0 and "0" or string.char(n >> 24 & 255, n >> 16 & 255, n >> 8 & 255, n & 255) end
local function pid(p) return p == nil and "nil" or tostring(GetPlayerId(p)) end
local neutral = Player(PLAYER_NEUTRAL_PASSIVE)

local rescuers = {}
local function listen(label, u)
  local t = CreateTrigger()
  TriggerRegisterUnitEvent(t, u, EVENT_UNIT_RESCUED)
  TriggerAddAction(t, function()
    out(label .. " unit rescued " .. now() .. " rescuer " .. tostring(GetRescuer() == rescuers[label]) .. " " .. name(GetUnitTypeId(GetRescuer() or u))
      .. " trigger unit " .. tostring(GetTriggerUnit() == u) .. " trigger player " .. pid(GetTriggerPlayer()) .. " owner " .. pid(GetOwningPlayer(u))
      .. " response " .. tostring(GetTriggerEventId() == EVENT_UNIT_RESCUED))
  end)
end
local playerEvents = CreateTrigger()
for _, p in ipairs({ Player(0), neutral, Player(1) }) do TriggerRegisterPlayerUnitEvent(playerEvents, p, EVENT_PLAYER_UNIT_RESCUED, nil) end
TriggerAddAction(playerEvents, function()
  out("player rescued " .. now() .. " trigger player " .. pid(GetTriggerPlayer()) .. " unit " .. name(GetUnitTypeId(GetTriggerUnit())) .. " rescuer "
    .. name(GetUnitTypeId(GetRescuer())) .. " owner " .. pid(GetOwningPlayer(GetTriggerUnit())))
end)

-- A round of up to six cases in cells 1500 apart; each case has a rescuer that stands still and a rescuable unit made the
-- distance from it, towards the middle; the round's units are removed 2.8 s after it starts.
local function round(at, cases)
  local made = {}
  for i, c in ipairs(cases) do
    local label, distance, setup, rescuerType, kind = c[1], c[2], c[3], c[4], c[5]
    TimerStart(CreateTimer(), at + (c[6] or 0), false, guard(function()
      local col, row = (i - 1) % 3, (i - 1) // 3
      local x, y = col * 1500 - 1500, row * 1400 - 700
      local r = CreateUnit(Player(0), FourCC(rescuerType or "hfoo"), x, y, 0)
      rescuers[label] = r
      local u = CreateUnit(neutral, FourCC(kind or "hfoo"), col == 0 and x + distance or x - distance, y, 0)
      made[#made + 1] = r
      made[#made + 1] = u
      listen(label, u)
      SetUnitRescuable(u, Player(0), true)
      if setup then setup(u, r) end
      out(label .. " made " .. now())
    end))
  end
  TimerStart(CreateTimer(), at + 2.8, false, guard(function() for _, u in ipairs(made) do RemoveUnit(u) end end))
end

round(1.0, { { "t0", 100 }, { "t1", 100, nil, nil, nil, 0.2 }, { "t2", 100, nil, nil, nil, 0.4 }, { "t3", 100, nil, nil, nil, 0.6 },
  { "t4", 100, nil, nil, nil, 0.8 }, { "t5", 100, nil, nil, nil, 1.0 } })
round(4.0, { { "d25", 25 }, { "d50", 50 }, { "d150", 150 }, { "d200", 200 }, { "d300", 300 }, { "d400", 400 } })
round(7.0, { { "d500", 500 }, { "d650", 650 }, { "d800", 800 }, { "range 300 at 250", 250, function(u) SetUnitRescueRange(u, 300) end },
  { "range 300 at 350", 350, function(u) SetUnitRescueRange(u, 300) end }, { "range 50 at 100", 100, function(u) SetUnitRescueRange(u, 50) end } })
round(10.0, { { "range 1000 at 900", 900, function(u) SetUnitRescueRange(u, 1000) end }, { "range 0 at 10", 10, function(u) SetUnitRescueRange(u, 0) end },
  { "range -1 at 10", 10, function(u) SetUnitRescueRange(u, -1) end }, { "hero", 150, nil, "hfoo", "Hpal" }, { "building", 150, nil, "hfoo", "hbar" },
  { "hero rescuer", 150, nil, "Hpal", "hfoo" } })
round(13.0, { { "building rescuer", 150, nil, "hbar", "hfoo" },
  { "for player 1", 100, function(u) SetUnitRescuable(u, Player(0), false) SetUnitRescuable(u, Player(1), true) end },
  { "switched off", 100, function(u) SetUnitRescuable(u, Player(0), false) end }, { "dead rescuer", 100, function(u, r) KillUnit(r) end },
  { "owned by player 0", 100, function(u) SetUnitOwner(u, Player(0), true) end }, { "owned by player 1", 100, function(u) SetUnitOwner(u, Player(1), true) end } })
round(16.0, { { "paused", 100, function(u) PauseUnit(u, true) end }, { "hidden rescuer", 100, function(u, r) ShowUnit(r, false) end } })

-- rescuable after standing there, and a rescuer arriving
TimerStart(CreateTimer(), 16.0, false, guard(function()
  local made = {}
  local r = CreateUnit(Player(0), FourCC("hfoo"), 0, 1400, 0)
  rescuers["late"] = r
  local u = CreateUnit(neutral, FourCC("hfoo"), 100, 1400, 0)
  listen("late", u)
  local r2 = CreateUnit(Player(0), FourCC("hfoo"), -1000, -1500, 0)
  rescuers["arrives"] = r2
  local u2 = CreateUnit(neutral, FourCC("hfoo"), 700, -1500, 0)
  listen("arrives", u2)
  SetUnitRescuable(u2, Player(0), true)
  TimerStart(CreateTimer(), 1.5, false, guard(function()
    out("late made rescuable " .. now())
    SetUnitRescuable(u, Player(0), true)
    out("arrives set position " .. now())
    SetUnitPosition(r2, 650, -1500)
  end))
  TimerStart(CreateTimer(), 3.5, false, guard(function() for _, x in ipairs({ r, u, r2, u2 }) do RemoveUnit(x) end end))
end))

TimerStart(CreateTimer(), 20, false, guard(function() done() end))
