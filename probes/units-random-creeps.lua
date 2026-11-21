-- ChooseRandomCreep for each level and ChooseRandomNPBuilding: the unit types each answers in 600 draws, sorted, and
-- how many there are (a type of low odds may be missing from a list).
local function id(n)
  return n == 0 and "0" or string.char(n >> 24 & 255, n >> 16 & 255, n >> 8 & 255, n & 255)
end

local function listed(label, draw)
  local seen, ids = {}, {}
  for _ = 1, 600 do
    local n = draw()
    if not seen[n] then seen[n] = true ids[#ids + 1] = id(n) end
  end
  table.sort(ids)
  out(label .. " " .. #ids)
  for first = 1, #ids, 40 do out(label .. " " .. table.concat(ids, " ", first, math.min(first + 39, #ids))) end
end

for level = -2, 12 do listed("creep " .. level, function() return ChooseRandomCreep(level) end) end
listed("building", ChooseRandomNPBuilding)
done()
