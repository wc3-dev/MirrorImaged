-- Carrion Beetles (AUcb, level 1) on the standard data, which the game refused on probe abilities-spells's corpse: which corpse and which order it takes. Nine
-- casters 1300 apart, each with a footman's corpse 200 away: an enemy's killed a second before the order or eight, or made with CreateCorpse; ordered with the
-- corpse as target or with no target, by the ability's order and by carrionscarabsinstant. One has no corpse and then one of its own side, one orders again
-- while its cooldown runs, one has autocast switched on and no order, and one casts seven times, a cooldown apart, for the limit. A caster's mana is
-- filled before each order, so the mana an effect reads is the cost alone.
local clock = CreateTimer()
TimerStart(clock, 1000, false, nil)
local function now() return TimerGetElapsed(clock) end
local function after(s, f) TimerStart(CreateTimer(), s, false, guard(f)) end
local function tf(v) return v and "t" or "f" end
local function cc(id) return id == 0 and "none" or string.char((id >> 24) & 255, (id >> 16) & 255, (id >> 8) & 255, id & 255) end
local AUcb, hfoo = FourCC("AUcb"), FourCC("hfoo")
local labels, slot = {}, 0

local function hero(label)
  local x, y = (slot % 3 - 1) * 1300, (slot // 3 - 1) * 1300
  slot = slot + 1
  local c = CreateUnit(Player(0), FourCC("Hamg"), x, y, 0)
  UnitRemoveAbility(c, FourCC("Aatk"))
  SetUnitState(c, UNIT_STATE_MANA, GetUnitState(c, UNIT_STATE_MAX_MANA))
  UnitAddAbility(c, AUcb)
  labels[GetHandleId(c)] = label
  return c, x, y
end
local function corpse(how, p, x, y)
  if how == "made" then return CreateCorpse(Player(p), hfoo, x, y, 0) end
  local u = CreateUnit(Player(p), hfoo, x, y, 0)
  KillUnit(u)
  return u
end

-- Player 0's living units within 600 of the caster but the caster, by type
local function beetles(c)
  local g, counts, types = CreateGroup(), {}, {}
  GroupEnumUnitsInRange(g, GetUnitX(c), GetUnitY(c), 600, nil)
  ForGroup(g, function()
    local u = GetEnumUnit()
    if u == c or GetOwningPlayer(u) ~= Player(0) or not UnitAlive(u) then return end
    local id = cc(GetUnitTypeId(u))
    if not counts[id] then counts[id] = 0 types[#types + 1] = id end
    counts[id] = counts[id] + 1
  end)
  DestroyGroup(g)
  table.sort(types)
  local s = ""
  for _, id in ipairs(types) do s = s .. " " .. id .. " " .. counts[id] end
  return s == "" and " none" or s
end

local summoned = {}
local summon = CreateTrigger()
TriggerRegisterPlayerUnitEvent(summon, Player(0), EVENT_PLAYER_UNIT_SUMMON, nil)
TriggerAddAction(summon, guard(function()
  local label, u = labels[GetHandleId(GetSummoningUnit())] or "?", GetSummonedUnit()
  summoned[label] = summoned[label] or {}
  table.insert(summoned[label], u)
  labels[GetHandleId(u)] = label .. "'s beetle " .. #summoned[label]
  out(string.format("%s summons %s in second %d: life %.0f", label, cc(GetUnitTypeId(u)), math.floor(now()), GetUnitState(u, UNIT_STATE_MAX_LIFE)))
end))
local effect = CreateTrigger()
TriggerRegisterPlayerUnitEvent(effect, Player(0), EVENT_PLAYER_UNIT_SPELL_EFFECT, nil)
TriggerAddAction(effect, guard(function()
  local c = GetTriggerUnit()
  out(string.format("%s effect: mana %.0f, target %s", labels[GetHandleId(c)] or "?", GetUnitState(c, UNIT_STATE_MANA), cc(GetUnitTypeId(GetSpellTargetUnit()))))
end))
local death = CreateTrigger()
TriggerRegisterPlayerUnitEvent(death, Player(0), EVENT_PLAYER_UNIT_DEATH, nil)
TriggerAddAction(death, guard(function()
  if labels[GetHandleId(GetTriggerUnit())] then out(labels[GetHandleId(GetTriggerUnit())] .. " dies in second " .. math.floor(now())) end
end))

local function try(c, kind, body)
  SetUnitState(c, UNIT_STATE_MANA, GetUnitState(c, UNIT_STATE_MAX_MANA))
  local s = ""
  for _, order in ipairs({ "carrionscarabs", "carrionscarabsinstant" }) do
    local ok
    if kind == "target" then ok = IssueTargetOrder(c, order, body) else ok = IssueImmediateOrder(c, order) end
    s = s .. " " .. order .. " " .. tf(ok)
    if ok then break end
  end
  return s
end

local function order(label, c, kind, body, at)
  after(at, function()
    out(label .. " order:" .. try(c, kind, body))
    after(2, function() out(label .. " +2: beetles" .. beetles(c) .. ", corpse " .. (body and cc(GetUnitTypeId(body)) or "none")) end)
  end)
end
local function cell(label, how, kind, at)
  local c, x, y = hero(label)
  order(label, c, kind, how and corpse(how, 1, x + 200, y), at)
  return c, x, y
end

-- Killed a second before: no order takes it. One caster then orders it every 0.1 s until it is taken; the other, its autocast off, leaves the corpse alone
do
  local c, x, y = hero("fresh target")
  local body = corpse("killed", 1, x + 200, y)
  order("fresh target", c, "target", body, 1)
  after(1.05, function()
    local t = CreateTimer()
    TimerStart(t, 0.1, true, guard(function()
      if not IssueTargetOrder(c, "carrionscarabs", body) then return end
      out(string.format("fresh target: taken %.2f s after the kill", now()))
      PauseTimer(t) DestroyTimer(t)
    end))
  end)
  local idle = cell("fresh untargeted", "killed", "untargeted", 1)
  after(20, function() out("fresh untargeted at 20: beetles" .. beetles(idle)) end)
end
cell("aged target", "killed", "target", 8)
cell("aged untargeted", "killed", "untargeted", 8)
do
  -- Then a corpse of the caster's own side as the target, once the cooldown is over
  local c, x, y = cell("made target", "made", "target", 1)
  after(8, function() order("own target", c, "target", corpse("made", 0, x + 200, y), 1) end)
end
do
  -- Then a second corpse while the cooldown runs, and the order again once it is over
  local c, x, y = cell("made untargeted", "made", "untargeted", 1)
  after(3, function()
    corpse("made", 1, x + 200, y)
    out(string.format("cooling order:%s, %.0f s of the cooldown left", try(c, "untargeted"), BlzGetUnitAbilityCooldownRemaining(c, AUcb)))
  end)
  after(9, function() out("cooled order:" .. try(c, "untargeted")) end)
end
do
  -- No corpse, and then one of the caster's own side
  local c, x, y = cell("none", nil, "untargeted", 1)
  after(5, function() order("own untargeted", c, "untargeted", corpse("made", 0, x + 200, y), 1) end)
end

-- Autocast: switched on beside an aged corpse, with no order; a second corpse comes at 10 s
do
  local c, x, y = hero("autocast")
  local body = corpse("killed", 1, x + 200, y)
  after(8, function() out("autocast on: " .. tf(IssueImmediateOrder(c, "carrionscarabson"))) end)
  after(10, function() corpse("made", 1, x + 200, y) SetUnitState(c, UNIT_STATE_MANA, GetUnitState(c, UNIT_STATE_MAX_MANA)) end)
  after(20, function() out("autocast at 20: beetles" .. beetles(c) .. ", corpse " .. cc(GetUnitTypeId(body))) end)
end

-- The limit: eight corpses and a cast every 6.5 s, the cooldown being 6
do
  local c, x, y = hero("limit")
  local a = BlzGetUnitAbility(c, AUcb)
  out(string.format("data: mana %d cooldown %.1f duration %.1f hero %.1f area %.0f range %.0f", BlzGetAbilityIntegerLevelField(a, ABILITY_ILF_MANA_COST, 0),
    BlzGetAbilityRealLevelField(a, ABILITY_RLF_COOLDOWN, 0), BlzGetAbilityRealLevelField(a, ABILITY_RLF_DURATION_NORMAL, 0), BlzGetAbilityRealLevelField(a, ABILITY_RLF_DURATION_HERO, 0),
    BlzGetAbilityRealLevelField(a, ABILITY_RLF_AREA_OF_EFFECT, 0), BlzGetAbilityRealLevelField(a, ABILITY_RLF_CAST_RANGE, 0)))
  local bodies = {}
  for i = 1, 8 do bodies[i] = corpse("made", 1, x + 200 * math.cos(i * 0.785), y + 200 * math.sin(i * 0.785)) end
  for n = 1, 7 do
    after(1 + (n - 1) * 6.5, function()
      SetUnitState(c, UNIT_STATE_MANA, GetUnitState(c, UNIT_STATE_MAX_MANA))
      out("limit cast " .. n .. ": " .. tf(IssueImmediateOrder(c, "carrionscarabsinstant")))
      after(2, function()
        local left, alive = 0, ""
        for i = 1, 8 do if GetUnitTypeId(bodies[i]) ~= 0 then left = left + 1 end end
        for i, u in ipairs(summoned.limit or {}) do if UnitAlive(u) then alive = alive .. " " .. i end end
        out("limit after cast " .. n .. ": beetles" .. beetles(c) .. ", corpses left " .. left .. ", alive of those summoned:" .. (alive == "" and " none" or alive))
      end)
    end)
  end
end
after(44, function() done() end)
