-- The unit kinds that come from buffs and abilities, the buff flags, and what a pause, a stun and BlzPauseUnitEx hold back, on the standard data.
-- Kinds: IsUnitType's stunned, plagued, snared, poisoned, polymorphed, resistant, ethereal and magic immune (in that order, t or f) on a unit with
-- nothing, a hero, units with Resistant Skin and Spell Immunity, and units under Storm Bolt, Sleep, Ensnare (on the ground and in the air), Web,
-- Entangling Roots, Polymorph, Hex, Envenomed Spears, Slow Poison, Disease Cloud, Banish, Avatar, PauseUnit and BlzPauseUnitEx. Flags: what
-- UnitCountBuffsEx counts for each flag alone and some together on a slowed unit, one in Devotion Aura, a poisoned one and an ensnared one. Holds:
-- a walking footman held from 1 s to 3 s by PauseUnit, by BlzPauseUnitEx and by Storm Bolt, its order events with their times, its current order
-- and how far it walks; an attacking footman held by BlzPauseUnitEx, its hits; and Slow's time on a footman and on a Mountain Giant (Resistant Skin).
-- Every unit that only stands there has its attack taken away (Aatk), so nothing fights unordered.
local clock = CreateTimer()
TimerStart(clock, 1000, false, nil)
local function now() return TimerGetElapsed(clock) end
local function after(s, f) TimerStart(CreateTimer(), s, false, guard(f)) end
local function f2(x) return string.format("%.2f", x) end
local function tf(v) return v and "t" or "f" end
for _, r in ipairs({{"Roen", 1}, {"Ruwb", 1}, {"Rhst", 2}, {"Rovs", 1}, {"Rupc", 1}}) do SetPlayerTechResearched(Player(0), FourCC(r[1]), r[2]) end

local who = {}
local function unit(label, p, id, x, y, armed)
  local u = CreateUnit(Player(p), FourCC(id), x, y, 0)
  if not armed then UnitRemoveAbility(u, FourCC("Aatk")) end
  who[GetHandleId(u)] = label
  return u
end
local function name(u) return u and (who[GetHandleId(u)] or GetUnitName(u)) or "none" end
-- A caster with mana enough for any spell, its ability added
local function caster(label, id, ability, x, y)
  local c = unit(label, 0, id, x, y)
  BlzSetUnitMaxMana(c, 1000)
  SetUnitState(c, UNIT_STATE_MANA, 1000)
  if ability then UnitAddAbility(c, FourCC(ability)) end
  return c
end
local function weak(u) BlzSetUnitBaseDamage(u, 0, 0) BlzSetUnitDiceNumber(u, 1, 0) BlzSetUnitDiceSides(u, 1, 0) return u end

local KINDS = {UNIT_TYPE_STUNNED, UNIT_TYPE_PLAGUED, UNIT_TYPE_SNARED, UNIT_TYPE_POISONED, UNIT_TYPE_POLYMORPHED, UNIT_TYPE_RESISTANT, UNIT_TYPE_ETHEREAL, UNIT_TYPE_MAGIC_IMMUNE}
local function kinds(u)
  local s = ""
  for _, k in ipairs(KINDS) do s = s .. tf(IsUnitType(u, k)) end
  return s
end
local function show(label, u, buff)
  out(string.format("kinds %s %s order %d paused %s type %s%s", label, kinds(u), GetUnitCurrentOrder(u), tf(IsUnitPaused(u)), name(u) == label and GetUnitName(u) or name(u),
    buff and (" buff " .. GetUnitAbilityLevel(u, FourCC(buff))) or ""))
end

-- Prints every order event the unit gets, with its kind, target and time
local function orders(label, u)
  local t = CreateTrigger()
  TriggerRegisterUnitEvent(t, u, EVENT_UNIT_ISSUED_ORDER)
  TriggerRegisterUnitEvent(t, u, EVENT_UNIT_ISSUED_POINT_ORDER)
  TriggerRegisterUnitEvent(t, u, EVENT_UNIT_ISSUED_TARGET_ORDER)
  TriggerAddAction(t, guard(function()
    local e = GetTriggerEventId()
    local kind = e == EVENT_UNIT_ISSUED_ORDER and "none" or e == EVENT_UNIT_ISSUED_POINT_ORDER and "point" or "target"
    out(string.format("order %s %d %s %s at %s %s now %s", label, GetIssuedOrderId(), kind, name(GetOrderTargetUnit()), f2(GetOrderPointX()), f2(GetOrderPointY()), f2(now())))
  end))
end

-- Kinds of units as they are, and with abilities that give a kind
do
  local x, y = -1700, 1700
  show("footman", unit("footman", 1, "hfoo", x, y))
  show("paladin", unit("paladin", 1, "Hpal", x + 150, y))
  show("giant", unit("giant", 1, "emtg", x + 300, y))
  show("misha", unit("misha", 1, "ngzc", x + 450, y))
  show("dryad", unit("dryad", 1, "edry", x + 600, y))
  show("breaker", unit("breaker", 1, "hspt", x + 750, y))
  local immune = unit("immune", 1, "hfoo", x + 900, y)
  UnitAddAbility(immune, FourCC("ACmi"))
  show("immune", immune)
  show("golem", unit("golem", 1, "ngst", x + 1050, y))
end

-- Spells that give a kind: each caster casts at 0.5 s on its own footman (a gryphon for the fliers), and the target is read at 2 s and 8 s
local spells = {
  {"bolt", "Hmkg", "AHtb", "thunderbolt", "hfoo", "BSTN"},
  {"sleep", "Udre", "AUsl", "sleep", "hfoo", "BUsl"},
  {"ensnare", "orai", nil, "ensnare", "hfoo", "Beng"},
  {"ensnareair", "orai", nil, "ensnare", "hgry", "Bena"},
  {"web", "ucry", nil, "web", "hgry", "Bwea"},
  {"roots", "Ekee", "AEer", "entanglingroots", "hfoo", "BEer"},
  {"polymorph", "hsor", nil, "polymorph", "hfoo", "Bply"},
  {"hex", "Oshd", "AOhx", "hex", "hfoo", "BOhx"},
  {"banish", "Hblm", "AHbn", "banish", "hfoo", "BHbn"},
}
for i, s in ipairs(spells) do
  local x, y = -1700 + (i - 1) * 380, 1100
  local c = caster(s[1] .. " caster", s[2], s[3], x, y)
  if s[2] == "hsor" then UnitRemoveAbility(c, FourCC("Aslo")) UnitRemoveAbility(c, FourCC("Aivs")) end
  local t = unit(s[1], 1, s[5], x + 200, y)
  after(0.5, function() out(s[1] .. " order " .. tf(IssueTargetOrder(c, s[4], t))) end)
  after(2, function() show(s[1], t, s[6]) end)
  after(8, function() show(s[1], t, s[6]) end)
end

-- Avatar on its caster
do
  local c = caster("avatar", "Hmkg", "AHav", -1700, 500)
  after(0.5, function() out("avatar order " .. tf(IssueImmediateOrder(c, "avatar"))) end)
  after(2, function() show("avatar", c, "BHav") end)
end

-- Attacks that poison, and Disease Cloud: an attacker with 1 damage a hit on its own footman, ordered at 0.5 s; the Abomination stands by its footman
local poisoners = {{"spears", "owyv", "Bpoi"}, {"slowpoison", "edry", "Bspo"}}
for i, p in ipairs(poisoners) do
  local x, y = -900 + (i - 1) * 500, 500
  local a = weak(unit(p[1] .. " attacker", 0, p[2], x, y, true))
  UnitRemoveAbility(a, FourCC("Aadm"))
  local t = unit(p[1], 1, "hfoo", x + 150, y)
  after(0.5, function() IssueTargetOrder(a, "attack", t) end)
  after(3, function() show(p[1], t, p[3]) IssueImmediateOrder(a, "stop") end)
end
do
  local x, y = 200, 500
  unit("abomination", 0, "uabo", x, y)
  local t = unit("plague", 1, "hfoo", x + 120, y)
  after(3, function() show("plague", t, "Bapl") end)
end

-- What the buff flags count: on a slowed footman, one in Devotion Aura, a poisoned one (Envenomed Spears) and an ensnared one, at 4 s
local FLAGS = {
  {"none", false, false, false, false, false}, {"magic", true, false, false, false, false}, {"physical", false, true, false, false, false},
  {"both", true, true, false, false, false}, {"timed", false, false, true, false, false}, {"aura", false, false, false, true, false},
  {"dispel", false, false, false, false, true}, {"all", true, true, true, true, true},
}
local function flags(label, u)
  local s = ""
  for _, f in ipairs(FLAGS) do s = s .. " " .. f[1] .. " " .. UnitCountBuffsEx(u, true, true, f[2], f[3], f[4], f[5], f[6]) end
  out("flags " .. label .. s)
end
do
  local x, y = -1700, -200
  local slower, slowed = caster("slow caster", "hsor", nil, x, y), unit("slowed", 1, "hfoo", x + 200, y)
  UnitRemoveAbility(slower, FourCC("Aivs"))
  UnitRemoveAbility(slower, FourCC("Aply"))
  after(0.5, function() IssueTargetOrder(slower, "slow", slowed) end)
  local aura = unit("aura paladin", 1, "Hpal", x + 600, y)
  SelectHeroSkill(aura, FourCC("AHad"))
  local inaura = unit("inaura", 1, "hfoo", x + 700, y)
  local a = weak(unit("flags attacker", 0, "owyv", x + 1100, y, true))
  local poisoned = unit("poisoned", 1, "hfoo", x + 1250, y)
  after(0.5, function() IssueTargetOrder(a, "attack", poisoned) end)
  local r = caster("flags raider", "orai", nil, x + 1700, y)
  local snared = unit("snared", 1, "hfoo", x + 1900, y)
  after(0.5, function() IssueTargetOrder(r, "ensnare", snared) end)
  after(4, function()
    IssueImmediateOrder(a, "stop")
    flags("slowed", slowed) flags("inaura", inaura) flags("poisoned", poisoned) flags("snared", snared)
  end)
end

-- Slow's time on a footman and on a Mountain Giant (Resistant Skin): Slow lasts 60 s on a unit and 10 on a hero
do
  local x, y = -1700, -700
  local c1, foot = caster("slow foot caster", "hsor", nil, x, y), unit("slow foot", 1, "hfoo", x + 200, y)
  local c2, giant = caster("slow giant caster", "hsor", nil, x + 500, y), unit("slow giant", 1, "emtg", x + 700, y)
  for _, c in ipairs({c1, c2}) do UnitRemoveAbility(c, FourCC("Aivs")) UnitRemoveAbility(c, FourCC("Aply")) end
  after(0.5, function() IssueTargetOrder(c1, "slow", foot) IssueTargetOrder(c2, "slow", giant) end)
  after(12, function() out("slow time foot " .. GetUnitAbilityLevel(foot, FourCC("Bslo")) .. " giant " .. GetUnitAbilityLevel(giant, FourCC("Bslo"))) end)
end

-- Holds: a footman walks east from 1 s; a pause, BlzPauseUnitEx and a stun (Storm Bolt, 5 s on a unit: from about 1.2 s) hold it, the first two
-- until 3 s. Its order events, its current order and kinds while held, and its place, every half second to 7 s
local function walker(label, y, hold, free)
  local u = unit(label, 1, "hfoo", -1700, y)
  orders(label, u)
  local x0 = GetUnitX(u)
  after(0.5, function() IssuePointOrder(u, "move", 1800, y) end)
  after(1, function() hold(u) out(string.format("hold %s order %d kinds %s", label, GetUnitCurrentOrder(u), kinds(u))) end)
  after(2, function() out(string.format("held %s order %d kinds %s paused %s", label, GetUnitCurrentOrder(u), kinds(u), tf(IsUnitPaused(u)))) end)
  if free then after(3, function() free(u) out(string.format("free %s order %d kinds %s", label, GetUnitCurrentOrder(u), kinds(u))) end) end
  for n = 1, 14 do after(n * 0.5, function() out(string.format("walk %s %s %s order %d", label, f2(now()), f2(GetUnitX(u) - x0), GetUnitCurrentOrder(u))) end) end
  return u
end
walker("pause", -1200, function(u) PauseUnit(u, true) end, function(u) PauseUnit(u, false) end)
walker("pauseex", -1400, function(u) BlzPauseUnitEx(u, true) end, function(u) BlzPauseUnitEx(u, false) end)
walker("pauseex2", -1600, function(u) BlzPauseUnitEx(u, true) BlzPauseUnitEx(u, true) end, function(u) BlzPauseUnitEx(u, false) end)
do
  local bolter = caster("bolter", "Hmkg", "AHtb", -1700, -1900)
  walker("stun", -1800, function(u) IssueTargetOrder(bolter, "thunderbolt", u) end)
end

-- An attacking footman held by BlzPauseUnitEx from 2 s to 4 s: its hits on a footman of its own, with their times
do
  local x, y = 600, -200
  local a = weak(unit("exattacker", 0, "hfoo", x, y, true))
  local t = unit("exattacked", 1, "hfoo", x + 90, y)
  SetUnitState(t, UNIT_STATE_LIFE, 400)
  local hits = CreateTrigger()
  TriggerRegisterUnitEvent(hits, t, EVENT_UNIT_DAMAGED)
  TriggerAddAction(hits, guard(function() out("exhit " .. f2(now())) end))
  after(0.5, function() IssueTargetOrder(a, "attack", t) end)
  after(2, function() BlzPauseUnitEx(a, true) out("exattacker held order " .. GetUnitCurrentOrder(a)) end)
  after(4, function() BlzPauseUnitEx(a, false) out("exattacker free order " .. GetUnitCurrentOrder(a)) end)
  after(6, function() IssueImmediateOrder(a, "stop") end)
end

after(13, function() done() end)
