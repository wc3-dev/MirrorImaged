-- How fast a hero attacks by its agility, and how long a missile flies by its distance and speed. Standard data. Each attacker has a foe of its own
-- (a footman of 50000 life, held and without its attack, made at 1.02 s the distance away at 30 degrees, clear of the other places) and a place
-- of its own 450 apart, faces it, and is ordered to attack it then; its damage is set to
-- 50 with no dice. A line gives the attack cooldown the natives read, when the foe's attacked event and its first hit came after the order, the time
-- between hits (the mean of all, and the least and the most), and for a missile the time from each of the first three attacked events to its hit.
local clock = CreateTimer()
TimerStart(clock, 1000, false, nil)
local function now() return TimerGetElapsed(clock) end
local function after(s, f) TimerStart(CreateTimer(), s, false, guard(f)) end
local Aatk = FourCC("Aatk")
local sites = 0
local function site()
  sites = sites + 1
  return -1350 + 450 * ((sites - 1) % 7), -1350 + 450 * ((sites - 1) // 7)
end
local start = {}

local function fight(label, id, distance, setup, seconds)
  local x, y = site()
  local u = CreateUnit(Player(0), FourCC(id), x, y, 30)
  BlzSetUnitBaseDamage(u, 49, 0)
  BlzSetUnitDiceNumber(u, 1, 0)
  BlzSetUnitDiceSides(u, 1, 0)
  if setup then setup(u) end
  start[#start + 1] = function()
    local foe = CreateUnit(Player(1), FourCC("hfoo"), x + distance * math.cos(math.pi / 6), y + distance * math.sin(math.pi / 6), 210)
    UnitRemoveAbility(foe, Aatk)
    SetUnitPropWindow(foe, 0)
    BlzSetUnitMaxHP(foe, 50000)
    SetWidgetLife(foe, 50000)
    local attacked, hit = {}, {}
    local t = CreateTrigger()
    TriggerRegisterUnitEvent(t, foe, EVENT_UNIT_ATTACKED)
    TriggerAddAction(t, guard(function() if GetAttacker() == u then attacked[#attacked + 1] = now() end end))
    t = CreateTrigger()
    TriggerRegisterUnitEvent(t, foe, EVENT_UNIT_DAMAGED)
    TriggerAddAction(t, guard(function() if GetEventDamageSource() == u then hit[#hit + 1] = now() end end))
    local t0 = now()
    local ordered = IssueTargetOrder(u, "attack", foe)
    after(seconds or 24, function()
      PauseUnit(u, true)
      local line = string.format("%s: cooldown %.2f, damage point %.2f", label, BlzGetUnitAttackCooldown(u, 0), BlzGetUnitWeaponRealField(u, UNIT_WEAPON_RF_ATTACK_DAMAGE_POINT, 0))
      if not ordered or #hit < 2 then out(line .. string.format("; order %s, hits %d", ordered and "t" or "f", #hit)) return end
      local least, most = 1000, 0
      for i = 2, #hit do
        local gap = hit[i] - hit[i - 1]
        least, most = math.min(least, gap), math.max(most, gap)
      end
      line = line .. string.format("; attacked +%.2f, first hit +%.2f; between hits %.3f (%.2f to %.2f)", attacked[1] - t0, hit[1] - t0, (hit[#hit] - hit[1]) / (#hit - 1), least, most)
      if distance > 100 or id ~= "Hpal" and id ~= "Obla" and id ~= "hfoo" then
        line = line .. "; attacked to hit"
        for i = 1, math.min(3, #hit, #attacked) do line = line .. string.format(" %.2f", hit[i] - attacked[i]) end
        line = line .. string.format("; it stands %.0f from the foe", math.sqrt((GetUnitX(foe) - GetUnitX(u)) ^ 2 + (GetUnitY(foe) - GetUnitY(u)) ^ 2))
      end
      out(line)
    end)
  end
end

-- A footman, with no agility, and Paladins and Blademasters of an agility that is set
fight("a footman", "hfoo", 100)
for _, agility in ipairs({ 0, 10, 13, 25, 50, 100, 150, 200, 250, 400 }) do
  fight("a Paladin of agility " .. agility, "Hpal", 100, function(u) SetHeroAgi(u, agility, true) end)
end
for _, agility in ipairs({ 0, 30, 100 }) do
  fight("a Blademaster of agility " .. agility, "Obla", 100, function(u) SetHeroAgi(u, agility, true) end)
end

-- Missiles: archers and Archmages (agility 0) by their distance, and archers with the missile's speed or arc set
local function swift(speed, arc)
  return function(u)
    if speed then BlzSetUnitWeaponRealField(u, UNIT_WEAPON_RF_ATTACK_PROJECTILE_SPEED, 0, speed) end
    if arc then BlzSetUnitWeaponRealField(u, UNIT_WEAPON_RF_ATTACK_PROJECTILE_ARC, 0, arc) end
  end
end
local function flight(u) return string.format("missile speed %.0f arc %.2f", BlzGetUnitWeaponRealField(u, UNIT_WEAPON_RF_ATTACK_PROJECTILE_SPEED, 0), BlzGetUnitWeaponRealField(u, UNIT_WEAPON_RF_ATTACK_PROJECTILE_ARC, 0)) end
local function archer(distance, speed, arc)
  local read
  fight(string.format("an archer at %d%s%s", distance, speed and ", speed set to " .. speed or "", arc and ", arc set to " .. arc or ""), "earc", distance, function(u)
    swift(speed, arc)(u)
    read = flight(u)
  end)
  local n = #start
  local run = start[n]
  start[n] = function() out(string.format("an archer at %d%s%s: %s", distance, speed and ", speed set to " .. speed or "", arc and ", arc set to " .. arc or "", read)) run() end
end
for distance = 100, 500, 50 do archer(distance) end
archer(400, 300)
archer(400, 450)
archer(400, 1800)
archer(400, nil, 0)
archer(400, nil, 0.5)
for _, distance in ipairs({ 100, 200, 300, 400, 500, 600 }) do
  fight("an Archmage of agility 0 at " .. distance, "Hamg", distance, function(u) SetHeroAgi(u, 0, true) end)
end
for _, id in ipairs({ "ohun", "hmpr", "hsor" }) do
  for _, distance in ipairs({ 200, 400 }) do fight("a " .. id .. " at " .. distance, id, distance) end
end
fight("a rifleman at 400", "hrif", 400)

after(1.02, function() for _, f in ipairs(start) do f() end end)
after(27, function() done() end)
