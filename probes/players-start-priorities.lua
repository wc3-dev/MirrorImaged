-- The start locations' priorities (GetStartLocPrio and GetStartLocPrioSlot) as SetStartLocPrioCount and SetStartLocPrio leave
-- them, called from the script after the game has started: each slot of a start location read as its other location
-- and its priority, before any count, after a count, after sets inside and past the count, and after a smaller count.
local function name(p)
  return p == MAP_LOC_PRIO_LOW and "L" or p == MAP_LOC_PRIO_HIGH and "H" or p == MAP_LOC_PRIO_NOT and "N" or "?"
end
local function slots(loc)
  local t = {}
  for slot = 0, 4 do t[#t + 1] = GetStartLocPrioSlot(loc, slot) .. name(GetStartLocPrio(loc, slot)) end
  return table.concat(t, " ")
end
out("priorities " .. GetHandleId(MAP_LOC_PRIO_LOW) .. " " .. GetHandleId(MAP_LOC_PRIO_HIGH) .. " " .. GetHandleId(MAP_LOC_PRIO_NOT) .. " " .. name(MAP_LOC_PRIO_LOW) .. name(MAP_LOC_PRIO_HIGH) .. name(MAP_LOC_PRIO_NOT))
out("before " .. slots(0) .. " | " .. slots(1) .. " | " .. slots(-1) .. " | " .. slots(40))
SetStartLocPrio(0, 0, 1, MAP_LOC_PRIO_HIGH)
out("set with no count " .. slots(0))
SetStartLocPrioCount(0, 3)
out("count 3 " .. slots(0))
SetStartLocPrio(0, 0, 1, MAP_LOC_PRIO_HIGH)
SetStartLocPrio(0, 1, 2, MAP_LOC_PRIO_LOW)
SetStartLocPrio(0, 2, 3, MAP_LOC_PRIO_NOT)
SetStartLocPrio(0, 3, 4, MAP_LOC_PRIO_HIGH)
SetStartLocPrio(0, -1, 5, MAP_LOC_PRIO_HIGH)
out("sets inside and past " .. slots(0))
SetStartLocPrioCount(0, 2)
out("count 2 " .. slots(0))
SetStartLocPrioCount(0, 3)
out("count 3 again " .. slots(0))
SetStartLocPrioCount(0, 5)
out("count 5 " .. slots(0))
SetStartLocPrioCount(0, 0)
out("count 0 " .. slots(0))
SetStartLocPrioCount(0, -1)
out("count -1 " .. slots(0))
SetStartLocPrioCount(1, 2)
SetStartLocPrio(1, 1, 7, MAP_LOC_PRIO_NOT)
out("other location " .. slots(0) .. " | " .. slots(1) .. " | " .. slots(2))
SetStartLocPrioCount(1, 2)
out("count 2 again " .. slots(1))
done()
