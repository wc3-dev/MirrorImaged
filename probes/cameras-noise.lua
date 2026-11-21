-- CameraSetSourceNoise, CameraSetTargetNoise and their Ex forms: how far the target and the eye read from where they
-- stand while a noise shakes them, over 40 reads 0.05 s apart (after a 0.3 s wait), for several magnitudes and
-- velocities; the largest and the mean distance on each axis, then that the noise clears when it is set to 0.
local function F(x) return string.format("%.4f", x) end
local function target() return GetCameraTargetPositionX(), GetCameraTargetPositionY(), GetCameraTargetPositionZ() end
local function eye() return GetCameraEyePositionX(), GetCameraEyePositionY(), GetCameraEyePositionZ() end

local steps = {}
local function step(wait, f) steps[#steps + 1] = { wait, f } end
local function run(i)
  if i > #steps then done() return end
  TimerStart(CreateTimer(), steps[i][1], false, guard(function() steps[i][2]() run(i + 1) end))
end

SetCameraBounds(-2000, -2000, 2000, 2000, -2000, 2000, 2000, -2000)
SetCameraPosition(0, 0)
local base = {}

local function measure(label, apply)
  local top, sum = { 0, 0, 0, 0, 0, 0 }, { 0, 0, 0, 0, 0, 0 }
  local reads = 0
  step(0.5, function()
    apply()
  end)
  step(0.3, function()
    local timer = CreateTimer()
    TimerStart(timer, 0.05, true, guard(function()
      local t = { target() }
      local e = { eye() }
      for k = 1, 3 do
        local a, b = math.abs(t[k] - base.t[k]), math.abs(e[k] - base.e[k])
        top[k], top[k + 3] = math.max(top[k], a), math.max(top[k + 3], b)
        sum[k], sum[k + 3] = sum[k] + a, sum[k + 3] + b
      end
      reads = reads + 1
      if reads == 40 then
        PauseTimer(timer)
        local mean = {}
        for k = 1, 6 do mean[k] = F(sum[k] / reads) end
        out(label .. " target max " .. F(top[1]) .. " " .. F(top[2]) .. " " .. F(top[3]) .. " eye max " .. F(top[4]) .. " " .. F(top[5]) .. " " .. F(top[6]))
        out(label .. " target mean " .. mean[1] .. " " .. mean[2] .. " " .. mean[3] .. " eye mean " .. mean[4] .. " " .. mean[5] .. " " .. mean[6])
        CameraSetSourceNoise(0, 0)
        CameraSetTargetNoise(0, 0)
      end
    end))
  end)
  step(2.5, function()
    local t = { target() }
    local e = { eye() }
    out(label .. " cleared " .. F(math.abs(t[1] - base.t[1]) + math.abs(t[2] - base.t[2]) + math.abs(t[3] - base.t[3])) .. " "
      .. F(math.abs(e[1] - base.e[1]) + math.abs(e[2] - base.e[2]) + math.abs(e[3] - base.e[3])))
  end)
end

step(0.5, function()
  base.t = { target() }
  base.e = { eye() }
  out("still target " .. F(base.t[1]) .. " " .. F(base.t[2]) .. " " .. F(base.t[3]) .. " eye " .. F(base.e[1]) .. " " .. F(base.e[2]) .. " " .. F(base.e[3]))
end)
measure("source 50 50", function() CameraSetSourceNoise(50, 50) end)
measure("target 50 50", function() CameraSetTargetNoise(50, 50) end)
measure("both 50 50", function() CameraSetSourceNoise(50, 50) CameraSetTargetNoise(50, 50) end)
measure("target 10 50", function() CameraSetTargetNoise(10, 50) end)
measure("target 200 50", function() CameraSetTargetNoise(200, 50) end)
measure("target 50 10", function() CameraSetTargetNoise(50, 10) end)
measure("target 50 500", function() CameraSetTargetNoise(50, 500) end)
measure("target 50 0", function() CameraSetTargetNoise(50, 0) end)
measure("target 0 50", function() CameraSetTargetNoise(0, 50) end)
measure("source 200 50", function() CameraSetSourceNoise(200, 50) end)
measure("source ex 50 50 vertical", function() CameraSetSourceNoiseEx(50, 50, true) end)
measure("source ex 50 50 not vertical", function() CameraSetSourceNoiseEx(50, 50, false) end)
measure("target ex 50 50 vertical", function() CameraSetTargetNoiseEx(50, 50, true) end)
measure("target ex 50 50 not vertical", function() CameraSetTargetNoiseEx(50, 50, false) end)
measure("target -50 50", function() CameraSetTargetNoise(-50, 50) end)
run(1)
