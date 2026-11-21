-- The abilities an item holds as handles (BlzGetItemAbility and BlzGetItemAbilityByIndex), free and carried, and what they read; and which
-- units can equip each equipment type. Each unit is made on a spot of its own (a grid 200 apart).
local function show(v) return v == nil and "nil" or tostring(v) end
local function kind(h) return h == nil and "nil" or tostring(h):match("^[%a_]+") end
local function name(n)
  return n == 0 and "0" or string.char(n >> 24 & 255, n >> 16 & 255, n >> 8 & 255, n & 255)
end
local spot = 0
local function place()
  spot = spot % 225 + 1
  return (spot % 15) * 200 - 1400, (spot // 15) * 200 - 1400
end
local function make(uid)
  local x, y = place()
  return CreateUnit(Player(0), FourCC(uid), x, y, 0)
end
local steps = {}
local function step(wait, label, f) steps[#steps + 1] = { wait, label, f } end
local function run(i)
  if i > #steps then done() return end
  TimerStart(CreateTimer(), steps[i][1], false, guard(function()
    out(steps[i][2])
    local ok, err = pcall(steps[i][3])
    if not ok then out(steps[i][2] .. " error " .. tostring(err)) end
    run(i + 1)
  end))
end

-- the abilities each item holds, by index and by id, and the handle's reads
step(0, "item abilities", function()
  for _, t in ipairs({ "phea", "rst1", "stwp", "wneg", "ratc", "ankh", "ssil", "wlsd" }) do
    local i = CreateItem(FourCC(t), 0, 0)
    local list, byId = {}, {}
    for n = -1, 4 do
      local a = BlzGetItemAbilityByIndex(i, n)
      list[#list + 1] = a == nil and "nil" or name(BlzGetAbilityId(a))
      if a ~= nil then byId[#byId + 1] = show(BlzGetItemAbility(i, BlzGetAbilityId(a)) == a) end
    end
    out(t .. " by index -1..4 " .. table.concat(list, ",") .. " by id same " .. table.concat(byId, ","))
  end
  local i = CreateItem(FourCC("phea"), 0, 0)
  local a = BlzGetItemAbilityByIndex(i, 0)
  local b = BlzGetItemAbilityByIndex(i, 0)
  out("phea handle " .. kind(a) .. " same " .. show(a == b) .. " id " .. name(BlzGetAbilityId(a)) .. " other item "
    .. show(BlzGetItemAbility(CreateItem(FourCC("phea"), 0, 0), BlzGetAbilityId(a)) == a) .. " absent " .. show(BlzGetItemAbility(i, FourCC("AHhb"))) .. " nil item "
    .. show(BlzGetItemAbility(nil, FourCC("AIh1"))))
  out("phea fields cooldown " .. BlzGetAbilityRealLevelField(a, ABILITY_RLF_COOLDOWN, 0) .. " mana " .. BlzGetAbilityIntegerLevelField(a, ABILITY_ILF_MANA_COST, 0)
    .. " levels " .. BlzGetAbilityIntegerField(a, ABILITY_IF_LEVELS) .. " heal " .. BlzGetAbilityIntegerLevelField(a, ABILITY_ILF_HIT_POINTS_GAINED_IHPG, 0))
  out("set " .. show(BlzSetAbilityRealLevelField(a, ABILITY_RLF_COOLDOWN, 0, 7)) .. " " .. BlzGetAbilityRealLevelField(a, ABILITY_RLF_COOLDOWN, 0)
    .. " other " .. BlzGetAbilityRealLevelField(BlzGetItemAbilityByIndex(CreateItem(FourCC("phea"), 0, 0), 0), ABILITY_RLF_COOLDOWN, 0))
  out("added " .. show(BlzItemAddAbility(i, FourCC("AHhb"))) .. " " .. show(BlzGetItemAbility(i, FourCC("AHhb")) ~= nil) .. " index 1 "
    .. name(BlzGetAbilityId(BlzGetItemAbilityByIndex(i, 1))) .. " twice " .. show(BlzItemAddAbility(i, FourCC("AHhb"))))
  local added = BlzGetItemAbility(i, FourCC("AHhb"))
  out("removed " .. show(BlzItemRemoveAbility(i, FourCC("AHhb"))) .. " " .. show(BlzGetItemAbility(i, FourCC("AHhb"))) .. " handle " .. show(BlzGetAbilityId(added)))
  RemoveItem(i)
  out("removed item " .. show(BlzGetItemAbilityByIndex(i, 0)) .. " " .. show(BlzGetItemAbility(i, FourCC("AIh1"))))
end)

-- the abilities of an item a hero carries
step(0.1, "carried item abilities", function()
  local h = make("Hpal")
  local i = UnitAddItemById(h, FourCC("phea"))
  local a = BlzGetItemAbilityByIndex(i, 0)
  out("phea carried by index " .. show(a ~= nil) .. " " .. (a and name(BlzGetAbilityId(a)) or "-") .. " by id " .. show(BlzGetItemAbility(i, FourCC("AIh1")) ~= nil)
    .. " same " .. show(BlzGetItemAbility(i, FourCC("AIh1")) == a) .. " unit's " .. show(BlzGetUnitAbility(h, FourCC("AIh1")) ~= nil)
    .. " same as unit's " .. show(a ~= nil and BlzGetUnitAbility(h, FourCC("AIh1")) == a))
  local r = UnitAddItemById(h, FourCC("rst1"))
  local b = BlzGetItemAbilityByIndex(r, 0)
  out("rst1 carried by index " .. show(b ~= nil) .. " " .. (b and name(BlzGetAbilityId(b)) or "-") .. " second " .. show(BlzGetItemAbilityByIndex(r, 1) ~= nil))
  UnitRemoveItem(h, i)
  out("phea dropped " .. show(BlzGetItemAbilityByIndex(i, 0) ~= nil) .. " " .. show(BlzGetItemAbility(i, FourCC("AIh1")) ~= nil))
  local e = CreateItem(FourCC("erbg"), 0, 0)
  UnitAddItem(h, e)
  out("gloves by index " .. show(BlzGetItemAbilityByIndex(e, 0) ~= nil) .. " " .. show(BlzGetItemAbilityByIndex(e, 1) ~= nil))
  out("item add " .. show(BlzItemAddAbility(i, FourCC("AIh1"))) .. " " .. show(BlzItemAddAbility(i, FourCC("AIh1"))) .. " " .. show(BlzItemAddAbility(i, FourCC("AIl1")))
    .. " " .. show(BlzItemAddAbility(i, FourCC("zzzz"))) .. " remove " .. show(BlzItemRemoveAbility(i, FourCC("AIl1"))) .. " " .. show(BlzItemRemoveAbility(i, FourCC("AIl1")))
    .. " " .. show(BlzItemRemoveAbility(i, FourCC("AIh1"))) .. " " .. show(BlzItemRemoveAbility(nil, FourCC("AIh1"))))
end)

-- which units can equip each equipment type
step(0.1, "equipment types", function()
  local function row(u, label)
    local a, b = {}, {}
    for n = 0, 9 do
      a[#a + 1] = show(UnitCanEquipItemOfEquipmentType(u, ConvertEquipmentType(n)))
      b[#b + 1] = show(BlzUnitCanEquipItemOfEquipmentType(u, ConvertEquipmentType(n)))
    end
    out(label .. " " .. table.concat(a, ",") .. " | " .. table.concat(b, ","))
  end
  local h = make("Hpal")
  row(h, "paladin")
  UnitAddAbility(h, FourCC("AEqu"))
  row(h, "paladin equip")
  UnitAddAbility(h, FourCC("AIni"))
  row(h, "paladin equip bag")
  row(make("hfoo"), "footman")
  local f = make("hfoo")
  UnitAddAbility(f, FourCC("AEqu"))
  row(f, "footman equip")
  out("nil " .. show(UnitCanEquipItemOfEquipmentType(nil, ConvertEquipmentType(1))) .. " " .. show(UnitCanEquipItemOfEquipmentType(h, nil)))
  local g = CreateItem(FourCC("erbg"), 0, 0)
  UnitAddItem(h, g)
  UnitEquipItem(h, g)
  row(h, "paladin gloves on")
end)

run(1)
