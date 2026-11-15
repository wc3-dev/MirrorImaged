-- StoreUnit, RestoreUnit, HaveStoredUnit, FlushStoredUnit and SyncStoredUnit (W2-3's leftover), without ever saving a
-- cache: what each answers, what a restored unit keeps (type, owner, place, life, mana, flags, a hero's level,
-- experience, points, skills, attributes, name, items with their charges, abilities) and what the original and the
-- cache do after. Each unit is made on a spot of its own (a grid 200 apart).
local function show(v) return v == nil and "nil" or tostring(v) end
local function kind(h) return h == nil and "nil" or tostring(h):match("^[%a_]+") end
local function pid(p) return p == nil and "nil" or tostring(GetPlayerId(p)) end
local function id(i)
  local n = GetItemTypeId(i)
  return n == 0 and "0" or string.char(n >> 24 & 255, n >> 16 & 255, n >> 8 & 255, n & 255)
end
local function tid(u)
  local n = GetUnitTypeId(u)
  return n == 0 and "0" or string.char(n >> 24 & 255, n >> 16 & 255, n >> 8 & 255, n & 255)
end
local spot = 0
local function make(uid, p)
  spot = spot % 225 + 1
  return CreateUnit(p or Player(0), FourCC(uid), (spot % 15) * 200 - 1400, (spot // 15) * 200 - 1400, 0)
end
local function part(at, label, f)
  TimerStart(CreateTimer(), at, false, function()
    out(label)
    local ok, err = pcall(f)
    if not ok then out(label .. " error " .. tostring(err)) end
  end)
end
local function slots(h)
  local s = {}
  for i = 0, 5 do
    local it = UnitItemInSlot(h, i)
    s[#s + 1] = id(it) .. (it and ("x" .. GetItemCharges(it)) or "")
  end
  return table.concat(s, ",")
end
local function abilities(h)
  local n = 0
  while BlzGetUnitAbilityByIndex(h, n) do n = n + 1 end
  return n
end
local function basics(u)
  return tid(u) .. " player " .. pid(GetOwningPlayer(u)) .. " at " .. GetUnitX(u) .. " " .. GetUnitY(u) .. " face " .. GetUnitFacing(u)
    .. " life " .. GetWidgetLife(u) .. "/" .. BlzGetUnitMaxHP(u) .. " mana " .. GetUnitState(u, UNIT_STATE_MANA) .. "/" .. BlzGetUnitMaxMana(u)
    .. " data " .. GetUnitUserData(u) .. " name " .. GetUnitName(u) .. " dead " .. tostring(not UnitAlive(u)) .. " abilities " .. abilities(u)
end
local function hero(h)
  return "level " .. GetHeroLevel(h) .. " xp " .. GetHeroXP(h) .. " points " .. GetHeroSkillPoints(h) .. " str " .. GetHeroStr(h, false) .. "/"
    .. GetHeroStr(h, true) .. " agi " .. GetHeroAgi(h, false) .. "/" .. GetHeroAgi(h, true) .. " int " .. GetHeroInt(h, false) .. "/"
    .. GetHeroInt(h, true) .. " proper " .. tostring(GetHeroProperName(h) == "Stored One") .. " AHhb " .. GetUnitAbilityLevel(h, FourCC("AHhb"))
    .. " AHds " .. GetUnitAbilityLevel(h, FourCC("AHds")) .. " slots " .. slots(h)
end

TimerStart(CreateTimer(), 9.0, false, guard(function() done() end))

local cache = InitGameCache("mirrorimaged-store.w3v")
local function rest(m, k, p)
  spot = spot % 225 + 1
  return RestoreUnit(cache, m, k, p, (spot % 15) * 200 - 1400, (spot // 15) * 200 - 1400, 0)
end

-- a footman
part(0.05, "footman", function()
  local f = make("hfoo")
  SetWidgetLife(f, 100)
  SetUnitUserData(f, 55)
  BlzSetUnitName(f, "Stored Footman")
  SetUnitState(f, UNIT_STATE_MANA, 0)
  out("have before " .. tostring(HaveStoredUnit(cache, "m", "f")) .. " restore before " .. kind(rest("m", "f", Player(1))))
  out("store " .. tostring(StoreUnit(cache, "m", "f", f)) .. " have " .. tostring(HaveStoredUnit(cache, "m", "f")) .. " original " .. basics(f))
  local r = RestoreUnit(cache, "m", "f", Player(1), 300.5, -400.25, 90)
  out("restored " .. kind(r) .. " same " .. tostring(r == f) .. " " .. basics(r))
  out("original after " .. basics(f))
  local r2 = rest("M", "F", Player(2))
  out("restored again, other case " .. kind(r2) .. " same " .. tostring(r2 == r) .. " " .. basics(r2))
  SetWidgetLife(f, 50)
  out("store again " .. tostring(StoreUnit(cache, "m", "f", f)) .. " " .. GetWidgetLife(rest("m", "f", Player(0))))
  out("restore nil player " .. kind(rest("m", "f", nil)))
  out("restore missing " .. kind(rest("m", "none", Player(0))) .. " " .. kind(rest("other", "f", Player(0))))
  out("other types " .. tostring(HaveStoredInteger(cache, "m", "f")) .. " " .. tostring(HaveStoredString(cache, "m", "f")))
  StoreInteger(cache, "m", "f", 7)
  out("integer beside it " .. tostring(HaveStoredInteger(cache, "m", "f")) .. " " .. tostring(HaveStoredUnit(cache, "m", "f")))
  SyncStoredUnit(cache, "m", "f")
  out("sync " .. tostring(HaveStoredUnit(cache, "m", "f")))
  FlushStoredUnit(cache, "m", "f")
  out("flushed " .. tostring(HaveStoredUnit(cache, "m", "f")) .. " " .. kind(rest("m", "f", Player(0))) .. " integer " .. tostring(HaveStoredInteger(cache, "m", "f")))
  StoreUnit(cache, "m", "g", f)
  FlushStoredMission(cache, "m")
  out("mission flushed " .. tostring(HaveStoredUnit(cache, "m", "g")))
  StoreUnit(cache, "m", "g", f)
  out("stored nil " .. tostring(StoreUnit(cache, "m", "h", nil)) .. " " .. tostring(HaveStoredUnit(cache, "m", "h")) .. " nil cache " .. tostring(StoreUnit(nil, "m", "h", f))
    .. " " .. tostring(HaveStoredUnit(nil, "m", "g")) .. " " .. kind(RestoreUnit(nil, "m", "g", Player(0), 0, 0, 0)))
  local other = InitGameCache("mirrorimaged-store.w3v")
  out("second handle " .. tostring(HaveStoredUnit(other, "m", "g")))
  FlushGameCache(cache)
  out("flushed cache " .. tostring(HaveStoredUnit(cache, "m", "g")) .. " " .. tostring(StoreUnit(cache, "m", "g", f)) .. " " .. tostring(HaveStoredUnit(cache, "m", "g")))
  cache = InitGameCache("mirrorimaged-store.w3v")
end)

-- a hero with everything
part(0.5, "paladin", function()
  local h = make("Hpal")
  SetHeroLevel(h, 5, false)
  SelectHeroSkill(h, FourCC("AHhb"))
  UnitAddItemById(h, FourCC("rst1"))
  UnitAddItemById(h, FourCC("belv"))
  local p = UnitAddItemById(h, FourCC("phea"))
  SetItemCharges(p, 3)
  UnitAddItemById(h, FourCC("tdex"))
  SetHeroStr(h, 30, true)
  SetWidgetLife(h, 400)
  SetUnitState(h, UNIT_STATE_MANA, 100)
  BlzSetHeroProperName(h, "Stored One")
  SetUnitUserData(h, 9)
  out("original " .. basics(h))
  out("original " .. hero(h))
  local t1 = CreateTrigger()
  local pickups, levels = 0, 0
  TriggerRegisterPlayerUnitEvent(t1, Player(0), EVENT_PLAYER_UNIT_PICKUP_ITEM, nil)
  TriggerAddAction(t1, function() pickups = pickups + 1 end)
  local t2 = CreateTrigger()
  TriggerRegisterPlayerUnitEvent(t2, Player(0), EVENT_PLAYER_HERO_LEVEL, nil)
  TriggerAddAction(t2, function() levels = levels + 1 end)
  out("store " .. tostring(StoreUnit(cache, "m", "h", h)) .. " have " .. tostring(HaveStoredUnit(cache, "m", "h")))
  out("after store " .. basics(h) .. " " .. hero(h))
  local r = RestoreUnit(cache, "m", "h", Player(0), 100, 200, 45)
  out("restored " .. kind(r) .. " same " .. tostring(r == h) .. " " .. basics(r))
  out("restored " .. hero(r))
  out("events " .. pickups .. " " .. levels)
  TimerStart(CreateTimer(), 0.1, false, function()
    out("later " .. basics(r))
    out("later " .. hero(r))
    out("events later " .. pickups .. " " .. levels)
  end)
  local e = make("Hpal", Player(3))
  out("restore for 3 " .. pid(GetOwningPlayer(rest("m", "h", Player(3)))))
  -- the bonus of a carried item and the Gloves of Haste, as the facts measured them
  local g = make("Hpal")
  UnitAddItemById(g, FourCC("gcel"))
  out("gloves store " .. tostring(StoreUnit(cache, "m", "g", g)))
  local gr = rest("m", "g", Player(0))
  out("gloves restored " .. kind(gr) .. " " .. slots(gr) .. " abilities " .. abilities(gr) .. " AIsx " .. GetUnitAbilityLevel(gr, FourCC("AIsx")))
end)

-- a dead hero, a building, a unit that is gone, a unit in a state
part(1.0, "others", function()
  local d = make("Hpal")
  UnitAddItemById(d, FourCC("rst1"))
  KillUnit(d)
  out("dead store " .. tostring(StoreUnit(cache, "m", "d", d)) .. " " .. tostring(HaveStoredUnit(cache, "m", "d")))
  local r = rest("m", "d", Player(0))
  out("dead restored " .. kind(r) .. " " .. (r and (basics(r) .. " " .. hero(r)) or ""))
  local b = make("hbar")
  out("building store " .. tostring(StoreUnit(cache, "m", "b", b)))
  local rb = rest("m", "b", Player(0))
  out("building restored " .. kind(rb) .. " " .. (rb and basics(rb) or ""))
  local s = make("hfoo")
  RemoveUnit(s)
  out("removed store " .. tostring(StoreUnit(cache, "m", "s", s)) .. " " .. tostring(HaveStoredUnit(cache, "m", "s")))
  TimerStart(CreateTimer(), 0.1, false, function()
    out("gone store " .. tostring(StoreUnit(cache, "m", "s2", s)) .. " " .. tostring(HaveStoredUnit(cache, "m", "s2")))
  end)
  local i = make("hfoo")
  SetUnitInvulnerable(i, true)
  ShowUnit(i, false)
  PauseUnit(i, true)
  UnitAddAbility(i, FourCC("AHhb"))
  SetUnitAbilityLevel(i, FourCC("AHhb"), 2)
  SetUnitColor(i, PLAYER_COLOR_GREEN)
  SetUnitScale(i, 2, 2, 2)
  SetUnitMoveSpeed(i, 100)
  StoreUnit(cache, "m", "i", i)
  local ri = rest("m", "i", Player(0))
  out("flags " .. tostring(BlzIsUnitInvulnerable(ri)) .. " hidden " .. tostring(IsUnitHidden(ri))
    .. " AHhb " .. GetUnitAbilityLevel(ri, FourCC("AHhb")) .. " abilities " .. abilities(ri) .. " " .. abilities(i) .. " speed " .. GetUnitMoveSpeed(ri)
    .. " scale " .. BlzGetUnitRealField(ri, UNIT_RF_SCALING_VALUE) .. " color " .. GetHandleId(GetPlayerColor(GetOwningPlayer(ri))))
end)
