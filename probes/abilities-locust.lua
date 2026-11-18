-- Locust Swarm (AUls, level 1) on the standard data, with foes in range: how many locusts there are and when they come out, what they are, what each hit
-- takes from a foe, how much a locust carries back and what the caster gets for it, and when the swarm is gone. Two casters 2200 apart cast at once, one
-- with foes of armour 2 and one with foes of armour 8, which tells a locust that turns back after so many hits from one that turns back after so much
-- damage. Each caster starts at 100 life of 10000, as does a hero that casts nothing, whose life is the regeneration alone. The foes are footmen of 5000
-- life with their attack taken away, at 300, 500, 600, 750 and 1000 from their caster, the one at 600 free and the others paused. Which locust hits whom
-- and when moves from run to run: those numbers are {t} in the expected lines.
local clock = CreateTimer()
TimerStart(clock, 1000, false, nil)
local function now() return TimerGetElapsed(clock) end
local function after(s, f) TimerStart(CreateTimer(), s, false, guard(f)) end
local function every(s, f)
  local t = CreateTimer()
  TimerStart(t, s, true, guard(function() if f() then PauseTimer(t) DestroyTimer(t) end end))
end
local function tf(v) return v and "t" or "f" end
local function cc(id) return string.char((id >> 24) & 255, (id >> 16) & 255, (id >> 8) & 255, id & 255) end
local AUls, uloc, Aatk = FourCC("AUls"), FourCC("uloc"), FourCC("Aatk")
local places = { "at300", "at500", "free600", "at750", "at1000" }

local function hero(x, y)
  local c = CreateUnit(Player(0), FourCC("Hamg"), x, y, 0)
  UnitRemoveAbility(c, Aatk)
  SetUnitState(c, UNIT_STATE_MANA, GetUnitState(c, UNIT_STATE_MAX_MANA))
  BlzSetUnitMaxHP(c, 10000) SetWidgetLife(c, 100)
  return c
end
local control = hero(0, -1500)
local function gained(c) return GetWidgetLife(c) - GetWidgetLife(control) end

local summons = 0
local summon = CreateTrigger()
TriggerRegisterPlayerUnitEvent(summon, Player(0), EVENT_PLAYER_UNIT_SUMMON, nil)
TriggerAddAction(summon, function() summons = summons + 1 end)

local swarms, victims, owner, load = {}, {}, {}, {}   -- a foe's swarm and place, a locust's swarm, and what a locust carries
local function swarm(label, x, side, armour)
  local s = { label = label, c = hero(x, 0), hits = 0, total = 0, low = 1e9, high = 0, by = {}, locusts = {}, most = 0,
    four = 0, more = 0, fewer = 0, unmatched = 0, loadLow = 1e9, loadHigh = 0, ratioLow = 1e9, ratioHigh = 0, gaps = 0, gapTotal = 0, gapLow = 1e9, came = {} }
  swarms[#swarms + 1] = s
  UnitAddAbility(s.c, AUls)
  for i, at in ipairs({ { 300 * side, 0 }, { 500 * side, 0 }, { 0, 600 }, { 0, -750 }, { 0, 1000 } }) do
    local u = CreateUnit(Player(1), FourCC("hfoo"), x + at[1], at[2], 180)
    UnitRemoveAbility(u, Aatk)
    BlzSetUnitMaxHP(u, 5000) SetWidgetLife(u, 5000)
    BlzSetUnitArmor(u, armour)
    PauseUnit(u, i ~= 3)
    victims[GetHandleId(u)] = { s, places[i] }
  end
  return s
end
local plain, armoured = swarm("armour 2", -1100, 1, 2), swarm("armour 8", 1100, -1, 8)

-- Player 0's living locusts, as only the enumeration by player finds a unit with Locust; a new one is its nearest caster's
local function count()
  for _, s in ipairs(swarms) do s.locusts = {} end
  local g = CreateGroup()
  GroupEnumUnitsOfPlayer(g, Player(0), nil)
  ForGroup(g, function()
    local u = GetEnumUnit()
    if GetUnitTypeId(u) ~= uloc or not UnitAlive(u) then return end
    local s = owner[GetHandleId(u)]
    if not s then
      s = math.abs(GetUnitX(u) - GetUnitX(plain.c)) < math.abs(GetUnitX(u) - GetUnitX(armoured.c)) and plain or armoured
      owner[GetHandleId(u)] = s
      load[GetHandleId(u)] = { damage = 0, hits = 0 }
      s.came[#s.came + 1] = now()
    end
    s.locusts[#s.locusts + 1] = u
  end)
  DestroyGroup(g)
end

local t0, first
local damaged = CreateTrigger()
TriggerRegisterPlayerUnitEvent(damaged, Player(1), EVENT_PLAYER_UNIT_DAMAGED, nil)
TriggerAddAction(damaged, guard(function()
  local from, d, victim = GetEventDamageSource(), GetEventDamage(), victims[GetHandleId(GetTriggerUnit())]
  local s, l = victim[1], load[GetHandleId(from)]
  if GetUnitTypeId(from) ~= uloc or not l or owner[GetHandleId(from)] ~= s then
    out(string.format("a hit of %.2f at %s of %s from %s", d, victim[2], s.label, cc(GetUnitTypeId(from))))
    return
  end
  if not first then
    first = true
    out(string.format("first hit: attack type %d damage type %d attack %s", GetHandleId(BlzGetEventAttackType()), GetHandleId(BlzGetEventDamageType()), tf(BlzGetEventIsAttack())))
  end
  s.hits, s.total, s.low, s.high = s.hits + 1, s.total + d, math.min(s.low, d), math.max(s.high, d)
  s.by[victim[2]] = (s.by[victim[2]] or 0) + 1
  if l.last then s.gaps, s.gapTotal, s.gapLow = s.gaps + 1, s.gapTotal + now() - l.last, math.min(s.gapLow, now() - l.last) end
  l.damage, l.hits, l.last = l.damage + d, l.hits + 1, now()
end))

-- A jump in what the caster has gained on the control is a locust back, or two: the loaded locusts nearest the caster whose loads the jump is 0.75 of
-- are taken for it
local function back(s, l, share)
  if l.hits == 4 then s.four = s.four + 1 elseif l.hits > 4 then s.more = s.more + 1 else s.fewer = s.fewer + 1 end
  s.loadLow, s.loadHigh, s.ratioLow, s.ratioHigh = math.min(s.loadLow, l.damage), math.max(s.loadHigh, l.damage), math.min(s.ratioLow, share), math.max(s.ratioHigh, share)
  l.damage, l.hits = 0, 0
end
local function heal(s, jump)
  local loaded = {}
  for _, u in ipairs(s.locusts) do
    local l = load[GetHandleId(u)]
    l.far = (GetUnitX(u) - GetUnitX(s.c)) ^ 2 + (GetUnitY(u) - GetUnitY(s.c)) ^ 2
    if l.hits > 0 then loaded[#loaded + 1] = l end
  end
  table.sort(loaded, function(a, b) return a.far < b.far end)
  for _, l in ipairs(loaded) do
    if math.abs(l.damage * 0.75 - jump) < 0.1 then return back(s, l, jump / l.damage) end
  end
  for i, a in ipairs(loaded) do
    for j = i + 1, #loaded do
      local b = loaded[j]
      if math.abs((a.damage + b.damage) * 0.75 - jump) < 0.1 then
        local share = jump / (a.damage + b.damage)
        back(s, a, share)
        return back(s, b, share)
      end
    end
  end
  s.unmatched = s.unmatched + 1
end

local effect = CreateTrigger()
TriggerRegisterUnitEvent(effect, plain.c, EVENT_UNIT_SPELL_EFFECT)
TriggerAddAction(effect, guard(function()
  t0 = now()
  local a = BlzGetUnitAbility(plain.c, AUls)
  out(string.format("effect: caster mana %.0f, duration %.1f hero %.1f area %.0f range %.0f cooldown %.0f", GetUnitState(plain.c, UNIT_STATE_MANA),
    BlzGetAbilityRealLevelField(a, ABILITY_RLF_DURATION_NORMAL, 0), BlzGetAbilityRealLevelField(a, ABILITY_RLF_DURATION_HERO, 0),
    BlzGetAbilityRealLevelField(a, ABILITY_RLF_AREA_OF_EFFECT, 0), BlzGetAbilityRealLevelField(a, ABILITY_RLF_CAST_RANGE, 0), BlzGetAbilityRealLevelField(a, ABILITY_RLF_COOLDOWN, 0)))
  for _, s in ipairs(swarms) do s.last = gained(s.c) end
  local seen, gone = false, false
  every(0.02, function()
    count()
    for _, s in ipairs(swarms) do
      local d = gained(s.c)
      if d - s.last > 1 and now() - t0 < 29.5 then heal(s, d - s.last) end
      s.last, s.most = d, math.max(s.most, #s.locusts)
    end
    if not seen and #plain.locusts > 0 then
      seen = true
      local l = plain.locusts[1]
      out(string.format("a locust: %s life %.0f speed %.0f damage %d + %dd%d cooldown %.2f locust %d", cc(GetUnitTypeId(l)), GetUnitState(l, UNIT_STATE_MAX_LIFE),
        GetUnitMoveSpeed(l), BlzGetUnitBaseDamage(l, 0), BlzGetUnitDiceNumber(l, 0), BlzGetUnitDiceSides(l, 0), BlzGetUnitAttackCooldown(l, 0), GetUnitAbilityLevel(l, FourCC("Aloc"))))
    elseif seen and not gone and #plain.locusts == 0 then
      gone = true
      out(string.format("the swarm of %d is gone at +%.1f", plain.most, now() - t0))
    end
    return now() - t0 > 36
  end)
  local counts = "locusts out after 0.9, 1.9, 2.9, 4.9, 10, 20 and 29 s:"
  for _, s in ipairs({ 0.9, 1.9, 2.9, 4.9, 10, 20, 29 }) do after(s, function() counts = counts .. " " .. #plain.locusts end) end
  after(29.1, function()
    local came = plain.came
    out(string.format("locusts 2 to 5 came %.2f %.2f %.2f %.2f after the one before, the 20th %.2f after the first", came[2] - came[1], came[3] - came[2], came[4] - came[3], came[5] - came[4], came[20] - came[1]))
    out(counts)
  end)
  after(37, function()
    for _, s in ipairs(swarms) do
      out(string.format("%s: %d hits of %.2f to %.2f, total %.1f; the caster gained %.1f more than the control, %.3f of the total", s.label, s.hits, s.low, s.high, s.total, gained(s.c), gained(s.c) / s.total))
      out(string.format("%s: a locust's hits were %.2f s apart at least, %.2f on average", s.label, s.gapLow, s.gapTotal / s.gaps))
      out(string.format("%s: of the locusts back before 29.5 s, %d had 4 hits, %d more and %d fewer, carrying %.2f to %.2f, the heal %.3f to %.3f of it; %d heals not told apart",
        s.label, s.four, s.more, s.fewer, s.loadLow, s.loadHigh, s.ratioLow, s.ratioHigh, s.unmatched))
      local line = s.label .. ": hit"
      for _, place in ipairs(places) do line = line .. " " .. place .. " " .. tf((s.by[place] or 0) > 0) end
      out(line)
    end
    out("summon events " .. summons)
  end)
end))
after(1, function() out("orders " .. tf(IssueImmediateOrder(plain.c, "locustswarm")) .. tf(IssueImmediateOrder(armoured.c, "locustswarm"))) end)
after(40, function() done() end)
