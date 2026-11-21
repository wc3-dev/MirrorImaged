-- CripplePlayer (standard data, the map with a computer): a footman of the user (player 0) stands where nothing of the computer (player 1) sees: the computer's
-- footman is 3000 away. Each line reads visible/fogged/masked of player 0's footman for player 1, and the point it stands on, as player 0's and 1's cripples are
-- turned on and off to each force.
local P0, P1 = Player(0), Player(1)
local a = CreateUnit(P0, FourCC("hfoo"), 0, 0, 0)
local h = CreateUnit(P1, FourCC("hfoo"), 3000, 0, 180)
local function seen(u, viewer) return (IsUnitVisible(u, viewer) and "v" or "-") .. (IsUnitFogged(u, viewer) and "f" or "-") .. (IsUnitMasked(u, viewer) and "m" or "-") end
local function read(label)
  out(string.format("%s: p0's footman for p1 %s, p1's for p0 %s; point of p0's for p1 %s, a point of p1's far corner %s", label, seen(a, P1), seen(h, P0),
    tostring(IsVisibleToPlayer(0, 0, P1)), tostring(IsVisibleToPlayer(1500, 0, P0))))
end
local function force(...)
  local f = CreateForce()
  for _, who in ipairs({...}) do ForceAddPlayer(f, who) end
  return f
end
local steps = {}
local function step(wait, f) steps[#steps + 1] = {wait, f} end
local function run(i)
  if i > #steps then done() return end
  TimerStart(CreateTimer(), steps[i][1], false, guard(function() steps[i][2]() run(i + 1) end))
end
step(0.5, function()
  read("start")
  CripplePlayer(P0, force(P1), true)
  read("p0 crippled to p1, now")
end)
step(0.5, function()
  read("p0 crippled to p1, 0.5 s")
  CripplePlayer(P0, force(P1), false)
end)
step(0.5, function()
  read("p0 crippled to p1 then off, 0.5 s")
  CripplePlayer(P0, force(P0), true)
end)
step(0.5, function()
  read("p0 crippled to p0, 0.5 s")
  CripplePlayer(P0, force(P0), false)
  CripplePlayer(P1, force(P0), true)
end)
step(0.5, function()
  read("p1 crippled to p0, 0.5 s")
  CripplePlayer(P1, force(P0), false)
  CripplePlayer(P0, force(P0, P1), true)
end)
step(0.5, function()
  read("p0 crippled to p0 and p1, 0.5 s")
  CripplePlayer(P0, force(P0, P1), false)
  CripplePlayer(P0, CreateForce(), true)
end)
step(0.5, function()
  read("p0 crippled to an empty force, 0.5 s")
  CripplePlayer(P0, CreateForce(), false)
  FogEnable(false)
end)
step(0.5, function()
  read("fog off")
  FogEnable(true)
  CripplePlayer(P0, force(P1), true)
  FogMaskEnable(false)
end)
step(0.5, function()
  read("crippled, mask off")
  FogMaskEnable(true)
  CripplePlayer(P0, force(P1), false)
end)
run(1)
