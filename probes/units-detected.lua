-- The detected event: an enemy footman with permanent invisibility (Apiv) and a footman of Player 0 with the detector's
-- ability (Adts) that comes within and leaves its radius; the unit and player events with what GetDetectedUnit,
-- GetEventDetectingPlayer, the trigger unit and the trigger player read, and the responses outside the events.
local clock = CreateTimer()
TimerStart(clock, 1000.0, false, nil)
local function now() return string.format("%.2f", TimerGetElapsed(clock)) end
local function show(v) return v == nil and "nil" or tostring(v) end
local function pid(p) return p == nil and "nil" or tostring(GetPlayerId(p)) end
local enemy = Player(PLAYER_NEUTRAL_AGGRESSIVE)

local hidden = CreateUnit(enemy, FourCC("hfoo"), 600, 0, 0)
out("added invisibility " .. show(UnitAddAbility(hidden, FourCC("Apiv"))))
local detector = CreateUnit(Player(0), FourCC("hfoo"), -1500, 0, 0)
out("added detector " .. show(UnitAddAbility(detector, FourCC("Adts"))))

local function describe()
  return "detected " .. show(GetDetectedUnit() == hidden) .. " detecting " .. pid(GetEventDetectingPlayer()) .. " trigger unit " .. show(GetTriggerUnit() == hidden)
    .. " " .. show(GetTriggerUnit() == detector) .. " trigger player " .. pid(GetTriggerPlayer())
end
local t = CreateTrigger()
TriggerRegisterUnitEvent(t, hidden, EVENT_UNIT_DETECTED)
TriggerAddAction(t, function() out("unit event " .. now() .. " " .. describe()) end)
local p = CreateTrigger()
TriggerRegisterPlayerUnitEvent(p, Player(0), EVENT_PLAYER_UNIT_DETECTED, nil)
TriggerRegisterPlayerUnitEvent(p, enemy, EVENT_PLAYER_UNIT_DETECTED, nil)
TriggerAddAction(p, function() out("player event " .. now() .. " " .. describe()) end)

local function state(label)
  out(label .. " " .. now() .. " outside " .. show(GetDetectedUnit()) .. " " .. show(GetEventDetectingPlayer()))
end

local steps = {}
local function step(wait, f) steps[#steps + 1] = { wait, f } end
local function run(i)
  if i > #steps then done() return end
  TimerStart(CreateTimer(), steps[i][1], false, guard(function() steps[i][2]() run(i + 1) end))
end
step(1, function() state("far") end)
step(0.5, function() SetUnitPosition(detector, 450, 0) out("moved near " .. now()) end)
step(0.1, function() state("just near") end)
step(1.5, function() state("near") end)
step(0.1, function() SetUnitPosition(detector, -1500, 0) out("moved away " .. now()) end)
step(1.5, function() state("away") end)
step(0.1, function() SetUnitPosition(detector, 450, 0) out("moved near again " .. now()) end)
step(1.5, function() state("near again") end)
step(0.1, function() RemoveUnit(detector) out("detector removed " .. now()) end)
step(1.5, function() state("removed") end)
run(1)
