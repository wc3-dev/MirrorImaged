-- A place that can't be reached: a footman from 1000 away ordered to move to the middle of a solid 256 block of unwalkable ground (row -500),
-- and one to the middle of a walled-in open square (row 500; walls 96 thick, 192 open inside, no way in). Every second to 24 s, per footman:
-- its x and y (rounded to 8) and its current order id; and the second its order became 0 (idle), or never.
local p = Player(0)
local function wall(cx, cy, blocked)
  for i = -6, 5 do
    for j = -6, 5 do
      if blocked(i, j) then SetTerrainPathable(cx + i * 32 + 16, cy + j * 32 + 16, PATHING_TYPE_WALKABILITY, false) end
    end
  end
end
wall(0, -500, function(i, j) return i >= -4 and i <= 3 and j >= -4 and j <= 3 end)
wall(0, 500, function(i, j) return not (i >= -3 and i <= 2 and j >= -3 and j <= 2) end)
local a = CreateUnit(p, FourCC("hfoo"), -1000, -500, 0)
local b = CreateUnit(p, FourCC("hfoo"), -1000, 500, 0)
out("order a " .. tostring(IssuePointOrder(a, "move", 0, -500)) .. ", order b " .. tostring(IssuePointOrder(b, "move", 0, 500)))
local t, idleA, idleB = 0, nil, nil
local function r8(v) return math.floor(v / 8 + 0.5) * 8 end
TimerStart(CreateTimer(), 0.05, true, function()
  t = t + 1
  if idleA == nil and GetUnitCurrentOrder(a) == 0 then idleA = t * 0.05 end
  if idleB == nil and GetUnitCurrentOrder(b) == 0 then idleB = t * 0.05 end
  if t % 20 == 0 and t <= 480 then
    out(string.format("%2d s: a %d %d order %d, b %d %d order %d", t // 20, r8(GetUnitX(a)), r8(GetUnitY(a)), GetUnitCurrentOrder(a), r8(GetUnitX(b)), r8(GetUnitY(b)), GetUnitCurrentOrder(b)))
  end
  if t == 480 then
    out(string.format("idle at: a %s, b %s", idleA and string.format("%.2f", idleA) or "never", idleB and string.format("%.2f", idleB) or "never"))
    done()
  end
end)
