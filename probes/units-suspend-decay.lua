-- UnitSuspendDecay in each place it can be called: before the footman dies, inside its death event, in the flesh stage
-- (1 s), in the bone stage (4 s, after the decay event), lifted again in each, and never. Each footman's decay event
-- and the time it is gone (its type id 0) are read to 100 s, past the 90 s a body lasts after its decay.
local clock = CreateTimer()
TimerStart(clock, 1000.0, false, nil)
local function now() return string.format("%.2f", TimerGetElapsed(clock)) end
local watched = {}

local function watch(label)
  local u = CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
  local t = CreateTrigger()
  TriggerRegisterUnitEvent(t, u, EVENT_UNIT_DEATH)
  TriggerRegisterUnitEvent(t, u, EVENT_UNIT_DECAY)
  TriggerAddAction(t, function()
    local death = GetTriggerEventId() == EVENT_UNIT_DEATH
    out(label .. " " .. (death and "death" or "decay") .. " " .. now())
    if death and label == "inside death" then UnitSuspendDecay(u, true) end
  end)
  watched[#watched + 1] = { label, u }
  return u
end

local function later(seconds, f) TimerStart(CreateTimer(), seconds, false, guard(f)) end

TimerStart(CreateTimer(), 0.1, false, guard(function()
  local never = watch("never")
  local before = watch("before")
  UnitSuspendDecay(before, true)
  local inside = watch("inside death")
  local flesh = watch("flesh stage")
  local bones = watch("bone stage")
  local lifted = watch("lifted in flesh")
  local liftedBones = watch("lifted in bones")
  local beforeLifted = watch("before lifted at 20")
  UnitSuspendDecay(beforeLifted, true)
  for _, u in ipairs({ never, before, inside, flesh, bones, lifted, liftedBones, beforeLifted }) do KillUnit(u) end
  later(0.9, function() UnitSuspendDecay(flesh, true) UnitSuspendDecay(lifted, true) end)
  later(1.4, function() UnitSuspendDecay(lifted, false) end)
  later(4, function() UnitSuspendDecay(bones, true) UnitSuspendDecay(liftedBones, true) end)
  later(5, function() UnitSuspendDecay(liftedBones, false) end)
  later(20, function() UnitSuspendDecay(beforeLifted, false) end)
end))

local tick = CreateTimer()
TimerStart(tick, 0.25, true, guard(function()
  for i = #watched, 1, -1 do
    local label, u = watched[i][1], watched[i][2]
    if GetUnitTypeId(u) == 0 then
      out(label .. " gone " .. now())
      table.remove(watched, i)
    end
  end
  if TimerGetElapsed(clock) > 100 then
    for _, w in ipairs(watched) do out(w[1] .. " still there " .. tostring(UnitAlive(w[2])) .. " " .. GetWidgetLife(w[2])) end
    PauseTimer(tick)
    done()
  end
end))
