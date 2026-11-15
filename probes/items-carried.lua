-- What carried items and powerups give a hero (W2-3's leftover): the attribute bonuses of carried items and what they
-- do to life, mana and armour, the permanent ones of tomes, the life, mana and armour items, the abilities an item puts
-- on its carrier, a set of an attribute with a bonus, the attributes read inside the pickup and drop events, and the
-- heal, mana and experience of a use. Each hero is made on a spot of its own (a grid 200 apart).
local function show(v) return v == nil and "nil" or tostring(v) end
local function id(i)
  local n = GetItemTypeId(i)
  return n == 0 and "0" or string.char(n >> 24 & 255, n >> 16 & 255, n >> 8 & 255, n & 255)
end
local spot = 0
local function make(uid)
  spot = spot % 225 + 1
  return CreateUnit(Player(0), FourCC(uid), (spot % 15) * 200 - 1400, (spot // 15) * 200 - 1400, 0)
end
local function part(at, label, f)
  TimerStart(CreateTimer(), at, false, function()
    out(label)
    local ok, err = pcall(f)
    if not ok then out(label .. " error " .. tostring(err)) end
  end)
end
local function attrs(h)
  return "str " .. GetHeroStr(h, false) .. "/" .. GetHeroStr(h, true) .. " agi " .. GetHeroAgi(h, false) .. "/" .. GetHeroAgi(h, true)
    .. " int " .. GetHeroInt(h, false) .. "/" .. GetHeroInt(h, true)
end
local function fields(h)
  return BlzGetUnitIntegerField(h, UNIT_IF_STRENGTH) .. " " .. BlzGetUnitIntegerField(h, UNIT_IF_STRENGTH_PERMANENT) .. " "
    .. BlzGetUnitIntegerField(h, UNIT_IF_STRENGTH_WITH_BONUS) .. " " .. BlzGetUnitIntegerField(h, UNIT_IF_AGILITY) .. " "
    .. BlzGetUnitIntegerField(h, UNIT_IF_AGILITY_PERMANENT) .. " " .. BlzGetUnitIntegerField(h, UNIT_IF_AGILITY_WITH_BONUS) .. " "
    .. BlzGetUnitIntegerField(h, UNIT_IF_INTELLIGENCE) .. " " .. BlzGetUnitIntegerField(h, UNIT_IF_INTELLIGENCE_PERMANENT) .. " "
    .. BlzGetUnitIntegerField(h, UNIT_IF_INTELLIGENCE_WITH_BONUS)
end
local function state(h)
  return "life " .. GetWidgetLife(h) .. "/" .. BlzGetUnitMaxHP(h) .. " mana " .. GetUnitState(h, UNIT_STATE_MANA) .. "/" .. BlzGetUnitMaxMana(h)
    .. " armour " .. BlzGetUnitRealField(h, UNIT_RF_DEFENSE)
end
local function abilities(h)
  local n = 0
  while BlzGetUnitAbilityByIndex(h, n) do n = n + 1 end
  return n
end
local function slots(h)
  local s = {}
  for i = 0, 5 do s[#s + 1] = id(UnitItemInSlot(h, i)) end
  return table.concat(s, ",")
end

TimerStart(CreateTimer(), 8.0, false, guard(function() done() end))

-- every item alone: what it gives, what it puts on the carrier, and what is left when it is taken out
part(0.05, "singles", function()
  local base = make("Hpal")
  out("base " .. attrs(base) .. " " .. state(base) .. " abilities " .. abilities(base))
  for _, n in ipairs({ "rst1", "rag1", "rin1", "bgst", "belv", "ciri", "ckng", "cnob", "rnsp", "rde1", "prvt", "pmna", "rlif", "ratc", "modt", "gcel" }) do
    local h = make("Hpal")
    local i = UnitAddItemById(h, FourCC(n))
    local first = BlzGetUnitAbilityByIndex(h, 0)
    out(n .. " in " .. slots(h) .. " " .. attrs(h) .. " " .. state(h) .. " abilities " .. abilities(h) .. " first " .. show(first ~= nil and BlzGetAbilityId(first)))
    out(n .. " fields " .. fields(h))
    local item = UnitItemInSlot(h, 0)
    for _, a in ipairs({ "AIs3", "AIa3", "AIi3", "AIs6", "AIa6", "AIi6", "AIx5", "AIx2", "AIx1", "AId2", "AIlf", "AIbm", "Arel", "AItc", "AIva", "AIsx" }) do
      local l = GetUnitAbilityLevel(h, FourCC(a))
      if l ~= 0 then out(n .. " has " .. a .. " " .. l) end
    end
    UnitRemoveItemFromSlot(h, 0)
    out(n .. " out " .. attrs(h) .. " " .. state(h) .. " abilities " .. abilities(h))
  end
end)

-- below full: the life and mana an item adds are added to what the hero has
part(0.5, "wounded", function()
  for _, n in ipairs({ "rst1", "prvt", "pmna", "rin1" }) do
    local h = make("Hpal")
    SetWidgetLife(h, 300)
    SetUnitState(h, UNIT_STATE_MANA, 50)
    UnitAddItemById(h, FourCC(n))
    out(n .. " wounded in " .. attrs(h) .. " " .. state(h))
    UnitRemoveItemFromSlot(h, 0)
    out(n .. " wounded out " .. attrs(h) .. " " .. state(h))
  end
  local h = make("Hpal")
  UnitAddItemById(h, FourCC("prvt"))
  SetWidgetLife(h, 700)
  UnitRemoveItemFromSlot(h, 0)
  out("prvt out at 700 " .. state(h))
end)

-- two of a kind, and a swap of slots
part(0.8, "stack", function()
  local h = make("Hpal")
  UnitAddItemById(h, FourCC("rst1"))
  UnitAddItemById(h, FourCC("rst1"))
  UnitAddItemById(h, FourCC("bgst"))
  out("three strength items " .. slots(h) .. " " .. attrs(h) .. " " .. state(h))
  out("abilities with three " .. abilities(h) .. " AIs3 " .. GetUnitAbilityLevel(h, FourCC("AIs3")) .. " AIs6 " .. GetUnitAbilityLevel(h, FourCC("AIs6")))
  out("swap " .. tostring(UnitDropItemSlot(h, UnitItemInSlot(h, 0), 4)) .. " " .. slots(h) .. " " .. attrs(h))
  KillUnit(h)
  out("dead carrying " .. slots(h) .. " " .. attrs(h) .. " " .. state(h))
end)

-- the tomes and manuals: permanent, and picked up and dropped at once
part(1.0, "powerups", function()
  for _, n in ipairs({ "tstr", "tdex", "tint", "manh", "texp" }) do
    local h = make("Hpal")
    SetWidgetLife(h, 400)
    UnitAddItemById(h, FourCC(n))
    out(n .. " " .. attrs(h) .. " " .. state(h) .. " level " .. GetHeroLevel(h) .. " xp " .. GetHeroXP(h) .. " fields " .. fields(h) .. " slots " .. slots(h))
  end
  local h = make("Hpal")
  UnitAddItemById(h, FourCC("belv"))
  UnitAddItemById(h, FourCC("tdex"))
  out("tome with boots " .. attrs(h) .. " " .. state(h))
  UnitRemoveItemFromSlot(h, 0)
  out("tome then boots out " .. attrs(h) .. " " .. state(h))
  local x = make("Hpal")
  SetHeroLevel(x, 4, false)
  UnitAddItemById(x, FourCC("texp"))
  out("tome of experience at level 4 " .. GetHeroLevel(x) .. " " .. GetHeroXP(x))
  SuspendHeroXP(x, true)
  UnitAddItemById(x, FourCC("texp"))
  out("tome of experience suspended " .. GetHeroLevel(x) .. " " .. GetHeroXP(x))
end)

-- the attributes set while a bonus is carried
part(1.3, "set", function()
  for _, perm in ipairs({ false, true }) do
    local h = make("Hpal")
    UnitAddItemById(h, FourCC("belv"))
    UnitAddItemById(h, FourCC("rst1"))
    SetHeroAgi(h, 20, perm)
    SetHeroStr(h, 30, perm)
    out("set " .. tostring(perm) .. " " .. attrs(h) .. " " .. state(h) .. " fields " .. fields(h))
    UnitRemoveItemFromSlot(h, 0)
    out("set " .. tostring(perm) .. " boots out " .. attrs(h) .. " " .. state(h))
  end
  local h = make("Hpal")
  UnitAddItemById(h, FourCC("belv"))
  SetHeroLevel(h, 5, false)
  out("level with boots " .. attrs(h) .. " " .. state(h) .. " fields " .. fields(h))
  local f = BlzSetUnitIntegerField(h, UNIT_IF_AGILITY, 10)
  out("agility field " .. tostring(f) .. " " .. attrs(h) .. " fields " .. fields(h))
  f = BlzSetUnitIntegerField(h, UNIT_IF_AGILITY_WITH_BONUS, 30)
  out("with bonus field " .. tostring(f) .. " " .. attrs(h) .. " fields " .. fields(h))
end)

-- what a pickup and a drop event read
part(1.6, "events", function()
  local h = make("Hpal")
  local t = CreateTrigger()
  TriggerRegisterUnitEvent(t, h, EVENT_UNIT_PICKUP_ITEM)
  TriggerAddAction(t, function() out("pickup " .. id(GetManipulatedItem()) .. " " .. attrs(h) .. " " .. state(h) .. " slots " .. slots(h)) end)
  local d = CreateTrigger()
  TriggerRegisterUnitEvent(d, h, EVENT_UNIT_DROP_ITEM)
  TriggerAddAction(d, function() out("drop " .. id(GetManipulatedItem()) .. " " .. attrs(h) .. " " .. state(h) .. " slots " .. slots(h)) end)
  UnitAddItemById(h, FourCC("ckng"))
  UnitAddItemById(h, FourCC("tstr"))
  UnitRemoveItemFromSlot(h, 0)
  out("after " .. attrs(h))
end)

-- uses
part(2.0, "uses", function()
  local ev = make("Hpal")
  local et = CreateTrigger()
  TriggerRegisterUnitEvent(et, ev, EVENT_UNIT_USE_ITEM)
  TriggerAddAction(et, function() out("use event " .. id(GetManipulatedItem()) .. " " .. state(ev) .. " charges " .. GetItemCharges(GetManipulatedItem()) .. " " .. slots(ev)) end)
  SetWidgetLife(ev, 100)
  UnitUseItem(ev, UnitAddItemById(ev, FourCC("phea")))
  for _, c in ipairs({ { "phea", 100 }, { "phea", 700 }, { "pghe", 400 }, { "pghe", 700 } }) do
    local h = make("Hpal")
    local it = UnitAddItemById(h, FourCC(c[1]))
    SetWidgetLife(h, c[2])
    out("use " .. c[1] .. " from " .. c[2] .. " " .. tostring(UnitUseItem(h, it)) .. " " .. state(h) .. " charges " .. GetItemCharges(it) .. " " .. slots(h))
  end
  local h = make("Hpal")
  local it = UnitAddItemById(h, FourCC("pman"))
  SetUnitState(h, UNIT_STATE_MANA, 50)
  out("use pman from 50 " .. tostring(UnitUseItem(h, it)) .. " " .. state(h) .. " charges " .. GetItemCharges(it) .. " " .. slots(h))
  it = UnitAddItemById(h, FourCC("pman"))
  SetUnitState(h, UNIT_STATE_MANA, 300)
  out("use pman from 300 " .. tostring(UnitUseItem(h, it)) .. " " .. state(h))
  local d = make("Hpal")
  it = UnitAddItemById(d, FourCC("phea"))
  KillUnit(d)
  out("dead use " .. tostring(UnitUseItem(d, it)) .. " " .. state(d) .. " charges " .. GetItemCharges(it))
  TimerStart(CreateTimer(), 1.0, false, function() out("uses later " .. state(h) .. " " .. slots(h)) end)
end)
