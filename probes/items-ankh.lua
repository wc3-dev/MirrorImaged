-- The Ankh of Reincarnation (W2-3's leftover): a hero carrying one dies, and what happens at each moment after: the
-- item events, the death and revive events, the hero's life and place, the slots, and the items on the ground. Then a
-- hero with two, one with an ankh that has no charges, and one killed again after it came back.
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
local function slots(h)
  local s = {}
  for i = 0, 5 do
    local it = UnitItemInSlot(h, i)
    s[#s + 1] = id(it) .. (it and ("x" .. GetItemCharges(it)) or "")
  end
  return table.concat(s, ",")
end
local function state(h)
  return "alive " .. tostring(UnitAlive(h)) .. " life " .. GetWidgetLife(h) .. "/" .. BlzGetUnitMaxHP(h) .. " mana " .. GetUnitState(h, UNIT_STATE_MANA)
    .. " at " .. GetUnitX(h) .. " " .. GetUnitY(h) .. " slots " .. slots(h)
end
local function ground(h)
  local seen = {}
  local r = Rect(GetUnitX(h) - 300, GetUnitY(h) - 300, GetUnitX(h) + 300, GetUnitY(h) + 300)
  EnumItemsInRect(r, nil, function() seen[#seen + 1] = id(GetEnumItem()) .. "@" .. (GetItemX(GetEnumItem()) - GetUnitX(h)) .. "," .. (GetItemY(GetEnumItem()) - GetUnitY(h)) end)
  RemoveRect(r)
  return table.concat(seen, " ")
end
local function watch(h, label)
  local function event(name, e)
    local t = CreateTrigger()
    TriggerRegisterUnitEvent(t, h, e)
    TriggerAddAction(t, function() out(label .. " " .. name .. " item " .. id(GetManipulatedItem()) .. " " .. state(h)) end)
  end
  event("use", EVENT_UNIT_USE_ITEM)
  event("drop", EVENT_UNIT_DROP_ITEM)
  event("death", EVENT_UNIT_DEATH)
  local function player(name, e, who)
    local t = CreateTrigger()
    TriggerRegisterPlayerUnitEvent(t, Player(0), e, nil)
    TriggerAddAction(t, function()
      local u = who()
      if u == h then out(label .. " " .. name .. " " .. state(h)) end
    end)
  end
  player("revive start", EVENT_PLAYER_HERO_REVIVE_START, GetRevivingUnit)
  player("revive finish", EVENT_PLAYER_HERO_REVIVE_FINISH, GetRevivingUnit)
  player("revivable", EVENT_PLAYER_HERO_REVIVABLE, GetRevivableUnit)
end
local function sample(h, label, times)
  for _, t in ipairs(times) do
    TimerStart(CreateTimer(), t, false, function() out(label .. " +" .. t .. " " .. state(h) .. " ground [" .. ground(h) .. "]") end)
  end
end

TimerStart(CreateTimer(), 15.0, false, guard(function() done() end))

TimerStart(CreateTimer(), 0.05, false, function()
  local h = make("Hpal")
  UnitAddItemById(h, FourCC("rst1"))
  local ankh = UnitAddItemById(h, FourCC("ankh"))
  out("one ankh " .. state(h) .. " charges " .. GetItemCharges(ankh))
  watch(h, "one")
  KillUnit(h)
  out("one killed " .. state(h) .. " ground [" .. ground(h) .. "]" .. " charges " .. GetItemCharges(ankh))
  sample(h, "one", { 0, 0.5, 1, 2, 3, 4, 5, 6, 7, 8, 9 })
end)

TimerStart(CreateTimer(), 0.1, false, function()
  local h = make("Hpal")
  local a = UnitAddItemById(h, FourCC("ankh"))
  local b = UnitAddItemById(h, FourCC("ankh"))
  out("two ankhs " .. state(h) .. " charges " .. GetItemCharges(a) .. " " .. GetItemCharges(b))
  watch(h, "two")
  KillUnit(h)
  out("two killed " .. state(h) .. " ground [" .. ground(h) .. "]")
  sample(h, "two", { 0, 1, 9 })
end)

TimerStart(CreateTimer(), 0.15, false, function()
  local h = make("Hpal")
  local a = UnitAddItemById(h, FourCC("ankh"))
  SetItemCharges(a, 0)
  out("empty ankh " .. state(h))
  watch(h, "empty")
  KillUnit(h)
  out("empty killed " .. state(h) .. " ground [" .. ground(h) .. "]")
  sample(h, "empty", { 0, 9 })
end)

TimerStart(CreateTimer(), 0.2, false, function()
  local h = make("Hpal")
  local a = UnitAddItemById(h, FourCC("ankh"))
  SetItemCharges(a, 2)
  out("charged ankh " .. state(h))
  watch(h, "charged")
  KillUnit(h)
  sample(h, "charged", { 0, 9 })
  TimerStart(CreateTimer(), 10, false, function()
    out("charged back " .. state(h))
    KillUnit(h)
    out("charged killed again " .. state(h))
    sample(h, "charged", { 0, 9 })
  end)
end)


