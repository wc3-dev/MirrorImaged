-- BlzResetUnitTalents on a Paladin that has the talent abilities (ATal and ATua) given by a script, and on one without,
-- with the abilities, the skill points and the level read before and after.
local function show(v) return v == nil and "nil" or tostring(v) end
local function name(n) return n == 0 and "0" or string.char(n >> 24 & 255, n >> 16 & 255, n >> 8 & 255, n & 255) end
local function state(u)
  local list = {}
  local i = 0
  while BlzGetUnitAbilityByIndex(u, i) do list[#list + 1] = name(BlzGetAbilityId(BlzGetUnitAbilityByIndex(u, i))) i = i + 1 end
  return "level " .. GetHeroLevel(u) .. " points " .. GetHeroSkillPoints(u) .. " abilities " .. table.concat(list, ",")
end
local plain = CreateUnit(Player(0), FourCC("Hpal"), 0, 0, 0)
SetHeroLevel(plain, 10, false)
out("plain " .. state(plain))
BlzResetUnitTalents(plain)
out("plain reset " .. state(plain))
local talented = CreateUnit(Player(0), FourCC("Hpal"), 200, 0, 0)
SetHeroLevel(talented, 10, false)
out("added " .. show(UnitAddAbility(talented, FourCC("ATal"))) .. " " .. show(UnitAddAbility(talented, FourCC("ATua"))) .. " " .. state(talented))
BlzResetUnitTalents(talented)
out("talented reset " .. state(talented))
for _, id in ipairs({ "ATal", "ATua" }) do out(id .. " level " .. GetUnitAbilityLevel(talented, FourCC(id))) end
BlzResetUnitTalents(nil)
done()
