-- When a cast's five events come, by the caster's cast point and backswing (the unit's own fields, set, and other unit types' own), by the
-- ability's casting time, with a stop order before the cast point and in the backswing, and with the target behind the caster; when Storm Bolt's
-- missile lands by its distance and speed; how many summon events Mirror Image fires at each level; and when a buff ends on a unit that is paused
-- for part of its time. Standard data, level 1, every case at a place of its own 450 apart, all ordered at 1.02 s. The casters have no attack,
-- intelligence 0 and 500 mana of 1000, so that what a cast spent reads whole. Times are from the order, to 0.01 s.
local clock = CreateTimer()
TimerStart(clock, 1000, false, nil)
local function now() return TimerGetElapsed(clock) end
local function after(s, f) TimerStart(CreateTimer(), s, false, guard(f)) end
local function every(s, f)
  local t = CreateTimer()
  TimerStart(t, s, true, guard(function() if f() then PauseTimer(t) DestroyTimer(t) end end))
end
local function tf(v) return v and "t" or "f" end
local Aatk = FourCC("Aatk")
local sites = 0
local function site()
  sites = sites + 1
  return -1350 + 450 * ((sites - 1) % 7), -1350 + 450 * ((sites - 1) // 7)
end
-- A caster; a Priest's Heal is switched off, as it would cast by itself
local function caster(id, ability, x, y, facing)
  local c = CreateUnit(Player(0), FourCC(id), x, y, facing or 0)
  UnitRemoveAbility(c, Aatk)
  if IsUnitType(c, UNIT_TYPE_HERO) then SetHeroInt(c, 0, true) end
  BlzSetUnitMaxMana(c, 1000)
  SetUnitState(c, UNIT_STATE_MANA, 500)
  UnitAddAbility(c, FourCC(ability))
  IssueImmediateOrder(c, "healoff")
  return c
end
local function foe(id, x, y)
  local u = CreateUnit(Player(1), FourCC(id), x, y, 0)
  UnitRemoveAbility(u, Aatk)
  SetUnitPropWindow(u, 0)
  BlzSetUnitMaxHP(u, 5000)
  SetWidgetLife(u, 5000)
  return u
end
local function hits(u)
  local h, t = {}, CreateTrigger()
  TriggerRegisterUnitEvent(t, u, EVENT_UNIT_DAMAGED)
  TriggerAddAction(t, guard(function() h[#h + 1] = { now(), GetEventDamage() } end))
  return h
end

-- Gives the order and keeps when each of the cast's events came; line() is "order t: channel +0.00, cast +0.00, effect +0.30, finish +2.70, end
-- cast +2.70; mana spent 75", an event that never came left out, one that came twice given twice, and one that fired before the order's call was
-- over marked "in the call"
local events = { { EVENT_UNIT_SPELL_CHANNEL, "channel" }, { EVENT_UNIT_SPELL_CAST, "cast" }, { EVENT_UNIT_SPELL_EFFECT, "effect" }, { EVENT_UNIT_SPELL_FINISH, "finish" },
  { EVENT_UNIT_SPELL_ENDCAST, "end cast" } }
local function cast(c, order)
  local s = { seen = {} }
  for _, e in ipairs(events) do
    local t = CreateTrigger()
    TriggerRegisterUnitEvent(t, c, e[1])
    TriggerAddAction(t, guard(function()
      s.seen[#s.seen + 1] = string.format("%s%s +%.2f", e[2], s.ordered == nil and " in the call" or "", now() - s.t0)
      if e[2] == "effect" then s.effect = now() end
    end))
  end
  local had = GetUnitState(c, UNIT_STATE_MANA)
  s.t0 = now()
  s.ordered = order()
  function s.line()
    return string.sub(string.format("order %s: %s; mana spent %.0f", tf(s.ordered), #s.seen > 0 and table.concat(s.seen, ", ") or "no event", had - GetUnitState(c, UNIT_STATE_MANA)), 1, 170)
  end
  return s
end
local function points(c) return string.format("cast point %.2f backswing %.2f", BlzGetUnitRealField(c, UNIT_RF_CAST_POINT), BlzGetUnitRealField(c, UNIT_RF_CAST_BACK_SWING)) end

local start = {}
local function at(f) start[#start + 1] = f end

-- Storm Bolt's missile: when its hits come after the effect, by the target's distance and the missile's speed
local function stormbolt(distance, speed)
  local x, y = site()
  local c = caster("Hmkg", "AHtb", x, y)
  local a = BlzGetUnitAbility(c, FourCC("AHtb"))
  if speed then BlzSetAbilityIntegerField(a, ABILITY_IF_MISSILE_SPEED, speed) end
  local target = foe("hfoo", x + distance, y)
  local h = hits(target)
  at(function()
    local s = cast(c, function() return IssueTargetOrder(c, "thunderbolt", target) end)
    after(10, function()
      local landed = "no hit"
      if h[1] and s.effect then
        landed = ""
        for i = 1, math.min(#h, 3) do landed = landed .. string.format("%s%.1f +%.2f", i > 1 and ", " or "", h[i][2], h[i][1] - s.effect) end
        landed = #h .. ": " .. landed
      end
      out(string.sub(string.format("storm bolt at %d, missile speed %d: %s; hits after the effect %s", distance, BlzGetAbilityIntegerField(a, ABILITY_IF_MISSILE_SPEED), s.line(), landed), 1, 250))
    end)
  end)
end
stormbolt(200)
stormbolt(400)
stormbolt(600)
stormbolt(400, 500)
stormbolt(400, 2000)

-- Silence (16 s on a Priest) on Priests 100 from its place: one free, one paused before it and freed 20 s after the effect, one paused 4 s after
-- the effect and freed 20 s after it, and one paused before it and never freed. When each one's buff ends, looked at every 0.05 s
do
  local x, y = site()
  local c = caster("Hamg", "ANsi", x, y)
  local function priest(dx, dy) return foe("hmpr", x + 300 + dx, y + dy) end
  local cases = { { "free", priest(100, 0) }, { "paused before, freed at +20", priest(-100, 0) }, { "paused at +4, freed at +20", priest(0, 100) }, { "paused before, never freed", priest(0, -100) } }
  PauseUnit(cases[2][2], true)
  PauseUnit(cases[4][2], true)
  at(function()
    local s = cast(c, function() return IssuePointOrder(c, "silence", x + 300, y) end)
    for _, e in ipairs(cases) do
      every(0.05, function()
        local on = GetUnitAbilityLevel(e[2], FourCC("BNsi")) > 0
        if on then e.on = e.on or now() elseif e.on then e.off = now() return true end
      end)
    end
    after(4.3, function() PauseUnit(cases[3][2], true) end)
    after(20.3, function() PauseUnit(cases[2][2], false) PauseUnit(cases[3][2], false) end)
    after(45, function()
      for _, e in ipairs(cases) do
        out(string.format("silence on a Priest, %s: buff %s", e[1], not e.on and "never" or e.off and string.format("until +%.1f", e.off - s.effect) or "still on at +44.7"))
      end
    end)
  end)
end

-- Silence (no casting time) at a place 300 ahead: the Archmage as it is, with its cast point or backswing set, and other unit types as they are
local function silence(label, id, point, swing, behind, extra)
  local x, y = site()
  local c = caster(id, "ANsi", x, y)
  if point then BlzSetUnitRealField(c, UNIT_RF_CAST_POINT, point) end
  if swing then BlzSetUnitRealField(c, UNIT_RF_CAST_BACK_SWING, swing) end
  at(function()
    local s = cast(c, function() return IssuePointOrder(c, "silence", x + (behind and -300 or 300), y) end)
    if extra then extra(c, s) end
    after(10, function() out(string.format("silence, %s, %s: %s", label, points(c), s.line())) end)
  end)
end
silence("an Archmage", "Hamg")
silence("cast point set to 0", "Hamg", 0)
silence("cast point set to 0.1", "Hamg", 0.1)
silence("cast point set to 0.75", "Hamg", 0.75)
silence("backswing set to 0", "Hamg", nil, 0)
silence("backswing set to 0.5", "Hamg", nil, 0.5)
silence("backswing set to 1", "Hamg", nil, 1)
for _, id in ipairs({ "Hpal", "Hmkg", "Hblm", "Udea", "Ofar", "Edem", "hmpr", "hsor", "ushd" }) do silence("a " .. id, id) end
silence("the place behind it", "Hamg", nil, nil, true)
silence("stop 0.15 s after the order", "Hamg", nil, nil, false, function(c, s)
  after(0.15, function() s.stop = IssueImmediateOrder(c, "stop") end)
  after(5, function() out(string.format("silence, stop 0.15 s after the order: stop %s, cooldown left 5 s in %.1f", tf(s.stop), BlzGetUnitAbilityCooldownRemaining(c, FourCC("ANsi")))) end)
end)
silence("stop 1 s after the order", "Hamg", nil, nil, false, function(c, s)
  after(1, function() s.stop = IssueImmediateOrder(c, "stop") end)
  after(5, function() out(string.format("silence, stop 1 s after the order: stop %s, cooldown left 5 s in %.1f", tf(s.stop), BlzGetUnitAbilityCooldownRemaining(c, FourCC("ANsi")))) end)
end)
silence("move 1 s after the order", "Hamg", nil, nil, false, function(c, s)
  local x, y = GetUnitX(c), GetUnitY(c)
  after(1, function() s.move = IssuePointOrder(c, "move", x, y + 150) end)
  after(1.5, function() out(string.format("silence, move 1 s after the order: move %s, 0.5 s later it is %.0f from its place", tf(s.move), GetUnitY(c) - y)) end)
end)

-- Flame Strike (casting time 1.33) at a place 300 ahead, the place beside it left empty (a Priest healed the next caster there): as it is, with the backswing or the cast point set, and stopped inside its casting time
local function flamestrike(label, point, swing, stop)
  local x, y = site()
  site()
  local c = caster("Hamg", "AHfs", x, y)
  if point then BlzSetUnitRealField(c, UNIT_RF_CAST_POINT, point) end
  if swing then BlzSetUnitRealField(c, UNIT_RF_CAST_BACK_SWING, swing) end
  local h = hits(foe("hfoo", x + 300, y))
  at(function()
    local s = cast(c, function() return IssuePointOrder(c, "flamestrike", x + 300, y) end)
    if stop then after(stop, function() IssueImmediateOrder(c, "stop") end) end
    after(10, function()
      out(string.format("flamestrike, %s, %s: %s; first hit %s", label, points(c), s.line(), h[1] and string.format("+%.2f", h[1][1] - s.t0) or "none"))
    end)
  end)
end
flamestrike("an Archmage")
flamestrike("backswing set to 0.5", nil, 0.5)
flamestrike("backswing set to 3", nil, 3)
flamestrike("cast point set to 0.75", 0.75)
flamestrike("stop 1 s after the order", nil, nil, 1)

-- A spell on a unit (Holy Light on a hurt footman 300 ahead) and one with no target (Thunder Clap)
do
  local x, y = site()
  local c = caster("Hpal", "AHhb", x, y)
  local hurt = CreateUnit(Player(0), FourCC("hfoo"), x + 300, y, 0)
  UnitRemoveAbility(hurt, Aatk)
  SetWidgetLife(hurt, 100)
  at(function()
    local s = cast(c, function() return IssueTargetOrder(c, "holybolt", hurt) end)
    after(10, function() out(string.format("holy light, a Paladin, %s: %s", points(c), s.line())) end)
  end)
end
do
  local x, y = site()
  local c = caster("Hmkg", "AHtc", x, y)
  at(function()
    local s = cast(c, function() return IssueImmediateOrder(c, "thunderclap") end)
    after(10, function() out(string.format("thunder clap, a Mountain King, %s: %s", points(c), s.line())) end)
  end)
end

-- Mirror Image at levels 1 to 3, 750 or more from the other cases' foes (an image attacks, for 0): its summon events, when they come after the effect, and how
-- many have a unit
sites = 46
for level = 1, 3 do
  local x, y = site()
  local c = caster("Hamg", "AOmi", x, y)
  SetUnitAbilityLevel(c, FourCC("AOmi"), level)
  local s, times, illusions, without = nil, {}, 0, 0
  local t = CreateTrigger()
  TriggerRegisterPlayerUnitEvent(t, Player(0), EVENT_PLAYER_UNIT_SUMMON, nil)
  TriggerAddAction(t, guard(function()
    if GetSummoningUnit() ~= c then return end
    local u = GetSummonedUnit()
    times[#times + 1] = now() - (s.effect or s.t0)
    if not u then without = without + 1 elseif IsUnitIllusion(u) then illusions = illusions + 1 end
  end))
  at(function()
    s = cast(c, function() return IssueImmediateOrder(c, "mirrorimage") end)
    after(10, function()
      table.sort(times)
      out(string.format("mirror image, level %d: %s; summon events %d, the first +%.2f after the effect and the last +%.2f; of illusions %d, without a unit %d", level, s.line(), #times,
        times[1] or 0, times[#times] or 0, illusions, without))
    end)
  end)
end

after(1.02, function() for _, f in ipairs(start) do f() end end)
after(48, function() done() end)
