-- Banish (AHbn, level 1) on the standard data, which probe abilities-spells could not see on a calm foe: the buff's length on a unit, a hero and a friend,
-- the banished unit's speed and its ethereal flag, what 100 damage of each attack type and four damage types takes from it, from a footman and from a hero
-- that are not banished, what a melee attack, a magic attack and Storm Bolt do to it, and whether a banished footman still attacks. The units that only
-- stand there have their attack taken away (Aatk): the heroes attacked on their own in a first run, as an acquisition range of 0 isn't taken (probe units).
local clock = CreateTimer()
TimerStart(clock, 1000, false, nil)
local function now() return TimerGetElapsed(clock) end
local function after(s, f) TimerStart(CreateTimer(), s, false, guard(f)) end
local function every(s, f)
  local t = CreateTimer()
  TimerStart(t, s, true, guard(function() if f() then PauseTimer(t) DestroyTimer(t) end end))
end
local function f1(x) return string.format("%.1f", x) end
local function tf(v) return v and "t" or "f" end
local BHbn = FourCC("BHbn")
local who = {}
local function unit(label, p, id, x, y)
  local u = CreateUnit(Player(p), FourCC(id), x, y, 0)
  if label ~= "footman" and label ~= "priest" then UnitRemoveAbility(u, FourCC("Aatk")) end
  who[GetHandleId(u)] = label
  return u
end
local function hero(label, id, ability, x, y)
  local c = unit(label, 0, id, x, y)
  SetUnitState(c, UNIT_STATE_MANA, GetUnitState(c, UNIT_STATE_MAX_MANA))
  BlzSetUnitBaseDamage(c, 0, 0) BlzSetUnitDiceNumber(c, 0, 0)
  UnitAddAbility(c, FourCC(ability))
  return c
end
local function fixed(u) BlzSetUnitBaseDamage(u, 49, 0) BlzSetUnitDiceNumber(u, 1, 0) BlzSetUnitDiceSides(u, 1, 0) return u end

-- Prints each hit the unit takes: every one from a spell, the first from each attacker
local quiet = false
local function hits(label, u)
  local seen, t = {}, CreateTrigger()
  TriggerRegisterUnitEvent(t, u, EVENT_UNIT_DAMAGED)
  TriggerAddAction(t, guard(function()
    local from = who[GetHandleId(GetEventDamageSource())] or "?"
    if quiet or seen[from] then return end
    if from == "footman" or from == "priest" then seen[from] = true end
    out(string.format("%s hit %.1f from %s attack type %d damage type %d", label, GetEventDamage(), from, GetHandleId(BlzGetEventAttackType()), GetHandleId(BlzGetEventDamageType())))
  end))
  return seen
end

-- Casts Banish on the unit a second in, and prints the effect, the buff's start and its end
local function banish(label, c, t, onEffect)
  local on
  local trigger = CreateTrigger()
  TriggerRegisterUnitEvent(trigger, c, EVENT_UNIT_SPELL_EFFECT)
  TriggerAddAction(trigger, guard(function()
    out(string.format("%s effect: caster mana %.0f", label, GetUnitState(c, UNIT_STATE_MANA)))
    if onEffect then onEffect() end
  end))
  after(1, function() out(label .. " order " .. tf(IssueTargetOrder(c, "banish", t))) end)
  every(0.05, function()
    local has = GetUnitAbilityLevel(t, BHbn) > 0
    if has and not on then
      on = now()
      out(string.format("%s buff on: speed %s of %s ethereal %s", label, f1(GetUnitMoveSpeed(t)), f1(GetUnitDefaultMoveSpeed(t)), tf(IsUnitType(t, UNIT_TYPE_ETHEREAL))))
    elseif on and not has then
      out(string.format("%s buff off after %.2f: speed %s ethereal %s", label, now() - on, f1(GetUnitMoveSpeed(t)), tf(IsUnitType(t, UNIT_TYPE_ETHEREAL))))
      return true
    end
  end)
end

-- What 100 damage takes from the unit, by attack type 0 to 6, for four damage types and for an attack
local function matrix(label, from, u)
  quiet = true
  for _, row in ipairs({ { 4, false }, { 8, false }, { 14, false }, { 26, false }, { 4, true } }) do
    local s = string.format("%s loses to 100 of damage type %d%s by attack type 0 to 6:", label, row[1], row[2] and " as an attack" or "")
    for a = 0, 6 do
      SetWidgetLife(u, 400)
      local before = GetWidgetLife(u)
      UnitDamageTarget(from, u, 100, row[2], false, ConvertAttackType(a), ConvertDamageType(row[1]), WEAPON_TYPE_WHOKNOWS)
      s = s .. " " .. f1(before - GetWidgetLife(u))
    end
    out(s)
  end
  SetWidgetLife(u, 400)
  quiet = false
end

-- A unit: attacked by a footman and a priest (50 a hit each) while banished, then the damage table, then Storm Bolt; a control is not banished, and its
-- priest comes later, so the two first hits come in one order
do
  local x, y = -1200, 1000
  local c, t = hero("caster", "Hamg", "AHbn", x, y), unit("target", 1, "hfoo", x + 300, y)
  local bolt = hero("bolt", "Hmkg", "AHtb", x + 300, y + 400)
  local control = unit("control", 1, "hfoo", x + 300, y - 1200)
  local seen = hits("unit", t)
  hits("control", control)
  banish("unit", c, t, function()
    after(0.5, function()
      local footman, priest, other = fixed(unit("footman", 0, "hfoo", x + 400, y)), fixed(unit("priest", 0, "hmpr", x + 300, y - 300))
      out("unit attack orders " .. tf(IssueTargetOrder(footman, "attack", t)) .. tf(IssueTargetOrder(priest, "attack", t)))
      after(1.5, function()
        other = fixed(unit("priest", 0, "hmpr", x + 300, y - 1500))
        out("control attack order " .. tf(IssueTargetOrder(other, "attack", control)))
      end)
      after(3.5, function()
        out("unit hit by the footman " .. tf(seen.footman) .. " by the priest " .. tf(seen.priest))
        PauseUnit(footman, true) PauseUnit(priest, true) PauseUnit(other, true)
      end)
    end)
    after(5, function()
      matrix("unit", c, t)
      matrix("control", c, control)
    end)
    after(6, function() out("unit bolt order " .. tf(IssueTargetOrder(bolt, "thunderbolt", t))) end)
  end)
end

-- A banished footman that was attacking: its hits before, while banished (from a second after the effect, as a swing may be under way) and after
do
  local x, y = 400, 1000
  local c = hero("caster", "Hamg", "AHbn", x - 200, y)
  local victim = CreateUnit(Player(0), FourCC("hfoo"), x + 400, y, 0)
  PauseUnit(victim, true)
  local attacker = CreateUnit(Player(1), FourCC("hfoo"), x + 300, y, 0)
  local before, during, later, state = 0, 0, 0, "before"
  local t = CreateTrigger()
  TriggerRegisterUnitEvent(t, victim, EVENT_UNIT_DAMAGED)
  TriggerAddAction(t, guard(function()
    if state == "before" then before = before + 1 elseif state == "during" then during = during + 1 elseif state == "after" then later = later + 1 end
  end))
  IssueTargetOrder(attacker, "attack", victim)
  local trigger = CreateTrigger()
  TriggerRegisterUnitEvent(trigger, c, EVENT_UNIT_SPELL_EFFECT)
  TriggerAddAction(trigger, guard(function()
    state = "landing"
    after(1, function() state = "during" end)
    after(2, function() out("attacker banished: order " .. GetUnitCurrentOrder(attacker) .. " speed " .. f1(GetUnitMoveSpeed(attacker))) end)
    every(0.05, function()
      if GetUnitAbilityLevel(attacker, BHbn) > 0 then return false end
      state = "after"
      IssueTargetOrder(attacker, "attack", victim)
      return true
    end)
  end))
  after(4.5, function() IssueTargetOrder(c, "banish", attacker) end)
  after(24, function() out("attacker hit before " .. tf(before > 0) .. ", while banished " .. during .. ", ordered again after " .. tf(later > 0)) end)
end

-- A hero and a friend as targets: the hero takes the damage table once its buff is over, and the friend's speed is 400, as half of a footman's 270 is
-- under the least speed there is
do
  local x, y = -1200, -1300
  local c, paladin = hero("caster", "Hamg", "AHbn", x, y), unit("target", 1, "Hpal", x + 300, y)
  banish("hero", c, paladin, function() after(6, function() matrix("hero", c, paladin) end) end)
  local friend = unit("target", 0, "hfoo", x + 1900, y)
  SetUnitMoveSpeed(friend, 400)
  banish("friend", hero("caster", "Hamg", "AHbn", x + 1600, y), friend)
end
after(25, function() done() end)
