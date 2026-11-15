-- What Locust (Aloc) does to a unit (W2-7, left open by W2-10's units-more): whether the unit is selectable, and which
-- enumerations and range tests find it (orders are wave 3's), at once after the add, later, and after the ability is removed. Four
-- footmen of Player 0 beside each other: plain, Locust, hidden and Locust again (the last takes it away, then gets it
-- back). Player 1's footman stands 100 from them, the units placed apart so none is moved aside.
for i = 0, 15 do
  for j = 0, 15 do SetPlayerAlliance(Player(i), Player(j), ALLIANCE_PASSIVE, true) end
end
local Aloc = FourCC("Aloc")
local names = {}
local function make(label, p, x, y)
  local u = CreateUnit(p or Player(0), FourCC("hfoo"), x, y, 0)
  names[GetHandleId(u)] = label
  return u
end
local plain, loc, hid, back = make("plain", nil, 0, 0), make("loc", nil, 100, 0), make("hid", nil, 0, 100), make("back", nil, 100, 100)
local enemy = make("enemy", Player(1), 200, 0)
local function tf(v) return v and "t" or "f" end
local function list(g)
  local t = {}
  ForGroup(g, function() t[#t + 1] = names[GetHandleId(GetEnumUnit())] end)
  table.sort(t)
  return #t .. " " .. table.concat(t, ",")
end
local function report(tag)
  local g = CreateGroup()
  GroupEnumUnitsInRange(g, 50, 50, 300, nil); out(tag .. " range " .. list(g))
  GroupEnumUnitsInRect(g, Rect(-300, -300, 300, 300), nil); out(tag .. " rect " .. list(g))
  GroupEnumUnitsOfPlayer(g, Player(0), nil); out(tag .. " player " .. list(g))
  GroupEnumUnitsOfType(g, "footman", nil); out(tag .. " type " .. list(g))
  GroupEnumUnitsInRange(g, 50, 50, 300, Filter(function() return true end)); out(tag .. " range filter " .. list(g))
  local s = ""
  for _, u in ipairs({ plain, loc, hid, back }) do s = s .. names[GetHandleId(u)] .. ":" .. tf(BlzIsUnitSelectable(u)) .. " " end
  out(tag .. " selectable " .. s)
  s = ""
  for _, u in ipairs({ plain, loc, hid, back }) do
    s = s .. names[GetHandleId(u)] .. ":" .. tf(IsUnitInRangeXY(u, 50, 50, 100)) .. tf(IsUnitInRange(u, enemy, 90)) .. tf(IsUnitInRange(enemy, u, 90)) .. " "
  end
  out(tag .. " inrange " .. s)
  s = ""
  for _, u in ipairs({ plain, loc, hid, back }) do s = s .. names[GetHandleId(u)] .. ":" .. GetUnitAbilityLevel(u, Aloc) .. " " end
  out(tag .. " level " .. s)
  DestroyGroup(g)
end

report("0 before")
out("add loc " .. tf(UnitAddAbility(loc, Aloc)) .. " again " .. tf(UnitAddAbility(loc, Aloc)))
ShowUnit(hid, false)
UnitAddAbility(back, Aloc)
report("1 now")
out("remove back " .. tf(UnitRemoveAbility(back, Aloc)))
report("2 removed")
-- A group made before the ability, and orders to and from a Locust unit
local held = CreateGroup()
GroupEnumUnitsOfPlayer(held, Player(0), nil)
UnitAddAbility(plain, Aloc)
out("held " .. list(held) .. " in group " .. tf(IsUnitInGroup(plain, held)))
UnitRemoveAbility(plain, Aloc)
TimerStart(CreateTimer(), 0.5, false, function()
  report("3 later")
  TimerStart(CreateTimer(), 3, false, function()
    report("4 much later")
    done()
  end)
end)
