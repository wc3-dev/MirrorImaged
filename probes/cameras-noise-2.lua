-- How the shake of CameraSetTargetNoise(50, velocity) grows and moves: the target's height read every 0.05 s for 3 s from the moment the
-- noise is set (two settings of velocity, then 500 and a magnitude of 200), as its distance from where it stood, ten reads a line.
local function F(x) return string.format("%.1f", x) end
SetCameraBounds(-2000, -2000, 2000, 2000, -2000, 2000, 2000, -2000)
SetCameraPosition(0, 0)
local steps = {}
local function step(wait, f) steps[#steps + 1] = { wait, f } end
local function run(i)
  if i > #steps then done() return end
  TimerStart(CreateTimer(), steps[i][1], false, guard(function() steps[i][2]() run(i + 1) end))
end
local base
step(0.5, function() base = GetCameraTargetPositionZ() end)
local function series(label, magnitude, velocity)
  step(0.5, function()
    CameraSetTargetNoise(magnitude, velocity)
    local reads = {}
    local timer = CreateTimer()
    local line = {}
    TimerStart(timer, 0.05, true, guard(function()
      reads[#reads + 1] = F(GetCameraTargetPositionZ() - base)
      if #reads % 10 == 0 then
        out(label .. " " .. #reads .. " " .. table.concat(reads, " ", #reads - 9, #reads))
      end
      if #reads == 60 then PauseTimer(timer) CameraSetTargetNoise(0, 0) end
    end))
  end)
  step(3.5, function() end)
end
series("50 10", 50, 10)
series("50 50", 50, 50)
series("50 500", 50, 500)
series("200 50", 200, 50)
run(1)
