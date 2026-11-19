-- Whether a missile follows a target that walks. Standard data. Each attacker (damage 50, no dice) has a foe of its own (a footman of 50000 life
-- without its attack) and is ordered to attack it 1.02 s in. A foe that walks starts at the first attacked event, already facing its way, at the
-- speed given; it is where the line says at the launch (the attacker's damage point after the attacked event) and at the first hit. A line gives the
-- time from the launch to the hit, the foe's place from the attacker's at both, the hit's damage, and for a foe that walks straight away or closer
-- how fast the missile closed on it: the way at the launch, less the 62 an archer's missile starts ahead (probe units-attack-timing), over the time.
-- Storm Bolt's foe starts at the effect event, which is its launch. A foe that stands is the old probe's: held by a propulsion window of 0.
local clock = CreateTimer()
TimerStart(clock, 1000, false, nil)
local function now() return TimerGetElapsed(clock) end
local function after(s, f) TimerStart(CreateTimer(), s, false, guard(f)) end
local Aatk = FourCC("Aatk")
local start, report = {}, {}

local function footman(x, y, facing, speed)
  local foe = CreateUnit(Player(1), FourCC("hfoo"), x, y, facing)
  UnitRemoveAbility(foe, Aatk)
  BlzSetUnitMaxHP(foe, 50000)
  SetWidgetLife(foe, 50000)
  if speed == 0 then SetUnitPropWindow(foe, 0) else SetUnitMoveSpeed(foe, speed) end
  return foe
end

-- way: the foe's walk as a direction (wx, wy), or none for one that stands
local function fight(label, id, x, y, fx, fy, speed, wx, wy, setup, ahead)
  local u = CreateUnit(Player(0), FourCC(id), x, y, Atan2(fy, fx) * 180 / math.pi)
  BlzSetUnitBaseDamage(u, 49, 0)
  BlzSetUnitDiceNumber(u, 1, 0)
  BlzSetUnitDiceSides(u, 1, 0)
  if setup then setup(u) end
  start[#start + 1] = function()
    local foe = footman(x + fx, y + fy, wx and Atan2(wy, wx) * 180 / math.pi or 210, wx and speed or 0)
    local attacked, launched, hit, lx, ly, hx, hy, amount
    local t = CreateTrigger()
    TriggerRegisterUnitEvent(t, foe, EVENT_UNIT_ATTACKED)
    TriggerAddAction(t, guard(function()
      if attacked or GetAttacker() ~= u then return end
      attacked = now()
      if wx then IssuePointOrder(foe, "move", GetUnitX(foe) + 3000 * wx, GetUnitY(foe) + 3000 * wy) end
      after(BlzGetUnitWeaponRealField(u, UNIT_WEAPON_RF_ATTACK_DAMAGE_POINT, 0), function()
        launched, lx, ly = now(), GetUnitX(foe) - GetUnitX(u), GetUnitY(foe) - GetUnitY(u)
        x, y = GetUnitX(u), GetUnitY(u)
      end)
    end))
    t = CreateTrigger()
    TriggerRegisterUnitEvent(t, foe, EVENT_UNIT_DAMAGED)
    TriggerAddAction(t, guard(function()
      if hit or GetEventDamageSource() ~= u then return end
      hit, hx, hy, amount = now(), GetUnitX(foe) - x, GetUnitY(foe) - y, GetEventDamage()
      PauseUnit(u, true)
      PauseUnit(foe, true)
    end))
    local ordered = IssueTargetOrder(u, "attack", foe)
    report[#report + 1] = function()
      local line = string.format("%s: missile speed %.0f, homing %s", label, BlzGetUnitWeaponRealField(u, UNIT_WEAPON_RF_ATTACK_PROJECTILE_SPEED, 0),
        tostring(BlzGetUnitWeaponBooleanField(u, UNIT_WEAPON_BF_ATTACK_PROJECTILE_HOMING_ENABLED, 0)))
      if not ordered or not launched then out(line .. string.format("; order %s, no launch", ordered and "t" or "f")) return end
      line = line .. string.format("; the foe (%.0f, %.0f) at the launch", lx, ly)
      if not hit then out(line .. string.format(", no hit, and (%.0f, %.0f) at the end", GetUnitX(foe) - x, GetUnitY(foe) - y)) return end
      line = line .. string.format(" and (%.0f, %.0f) at the hit %.2f s later; damage %.1f", hx, hy, hit - launched, amount)
      if ahead and hit > launched then line = line .. string.format("; closed at %.0f a second", (math.sqrt(lx * lx + ly * ly) - ahead) / (hit - launched)) end
      out(line)
    end
  end
end

local function missile(speed, homing)
  return function(u)
    if speed then BlzSetUnitWeaponRealField(u, UNIT_WEAPON_RF_ATTACK_PROJECTILE_SPEED, 0, speed) end
    if homing ~= nil then BlzSetUnitWeaponBooleanField(u, UNIT_WEAPON_BF_ATTACK_PROJECTILE_HOMING_ENABLED, 0, homing) end
  end
end

local row = 0
local function west() row = row + 1 return -1500, -1750 + 350 * row end

-- Archers: a foe that stands, then foes that walk away, closer and across
do local x, y = west() fight("an archer, the foe standing at 400", "earc", x, y, 400, 0, 0) end
do local x, y = west() fight("an archer, the foe walking away at 150", "earc", x, y, 300, 0, 150, 1, 0, nil, 62) end
do local x, y = west() fight("an archer of missile speed 300, the foe walking away at 150", "earc", x, y, 300, 0, 150, 1, 0, missile(300), 62) end
do local x, y = west() fight("an archer of missile speed 300 with homing set, the foe walking away at 150", "earc", x, y, 300, 0, 150, 1, 0, missile(300, true), 62) end
do local x, y = west() fight("an archer of missile speed 300, the foe walking closer at 150", "earc", x, y, 500, 0, 150, -1, 0, missile(300), 62) end
fight("an archer of missile speed 300, the foe walking across at 150", "earc", 0, -1400, 300, 0, 150, 0, 1, missile(300))
fight("an archer of missile speed 450, the foe walking across at 270", "earc", 500, -1400, 300, 0, 270, 0, 1, missile(450))

-- Mortar teams, whose weapon is artillery
local function areas(u)
  out(string.format("a mortar team: areas %.0f, %.0f and %.0f; factors %.2f and %.2f", BlzGetUnitWeaponRealField(u, UNIT_WEAPON_RF_ATTACK_AREA_OF_EFFECT_FULL_DAMAGE, 0),
    BlzGetUnitWeaponRealField(u, UNIT_WEAPON_RF_ATTACK_AREA_OF_EFFECT_MEDIUM_DAMAGE, 0), BlzGetUnitWeaponRealField(u, UNIT_WEAPON_RF_ATTACK_AREA_OF_EFFECT_SMALL_DAMAGE, 0),
    BlzGetUnitWeaponRealField(u, UNIT_WEAPON_RF_ATTACK_DAMAGE_FACTOR_MEDIUM, 0), BlzGetUnitWeaponRealField(u, UNIT_WEAPON_RF_ATTACK_DAMAGE_FACTOR_SMALL, 0)))
end
do local x, y = west() fight("a mortar team, the foe standing at 600", "hmtm", x, y, 600, 0, 0, nil, nil, areas) end
do local x, y = west() fight("a mortar team, the foe walking away at 270", "hmtm", x, y, 500, 0, 270, 1, 0) end
fight("a mortar team, the foe walking across at 270", "hmtm", 1000, -1400, 600, 0, 270, 0, 1)

-- Storm Bolt (missile speed set to 300) at a foe that walks away from 300 at 150 from the effect event on
do
  local x, y = west()
  local king = CreateUnit(Player(0), FourCC("Hmkg"), x, y, 0)
  local bolt = FourCC("AHtb")
  SelectHeroSkill(king, bolt)
  BlzSetAbilityIntegerField(BlzGetUnitAbility(king, bolt), ABILITY_IF_MISSILE_SPEED, 300)
  start[#start + 1] = function()
    local foe = footman(x + 300, y, 0, 150)
    local effect, hits, hx = nil, {}, nil
    local t = CreateTrigger()
    TriggerRegisterUnitEvent(t, king, EVENT_UNIT_SPELL_EFFECT)
    TriggerAddAction(t, guard(function()
      effect = now()
      IssuePointOrder(foe, "move", GetUnitX(foe) + 3000, GetUnitY(foe))
    end))
    t = CreateTrigger()
    TriggerRegisterUnitEvent(t, foe, EVENT_UNIT_DAMAGED)
    TriggerAddAction(t, guard(function()
      -- The bolt's two hits; the king's own attacks come after
      if #hits == 2 then return end
      if #hits == 0 then hx = GetUnitX(foe) - x end
      hits[#hits + 1] = string.format("%.0f at %.2f", GetEventDamage(), now() - (effect or 0))
    end))
    local ordered = IssueTargetOrder(king, "thunderbolt", foe)
    report[#report + 1] = function()
      out(string.format("Storm Bolt of missile speed 300, the foe walking away at 150 from 300: order %s; hits %s after the effect; the foe %.0f away at the first",
        ordered and "t" or "f", #hits > 0 and table.concat(hits, ", ") or "none", hx or -1))
    end
  end
end

after(1.02, function() for _, f in ipairs(start) do f() end end)
after(12, function() for _, f in ipairs(report) do f() end done() end)
