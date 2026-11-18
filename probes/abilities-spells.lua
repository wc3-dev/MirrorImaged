-- Spells beyond the first set (D395 to D400), on the standard data: for each, a hero casts it at a footman a few steps away and a second one far away, units set to
-- not attack, and what changes is printed after 1, 3 and 8 seconds: the life lost by the near and the far foe, the foes' mana, the caster's speed and mana, and the units
-- of each side within 700 of the caster. MirrorImaged models these from the fields' names (Close); this gives the game's numbers.
local function after(s, f) TimerStart(CreateTimer(), s, false, guard(f)) end
local function f1(x) return string.format("%.1f", x) end
local slot = 0
local function calm(u) SetUnitAcquireRange(u, 0) return u end
local function count(p, x, y)
  local g, n = CreateGroup(), 0
  GroupEnumUnitsInRange(g, x, y, 700, nil)
  ForGroup(g, function() if GetOwningPlayer(GetEnumUnit()) == p and UnitAlive(GetEnumUnit()) then n = n + 1 end end)
  DestroyGroup(g)
  return n
end
local function run(label, id, order, kind, foeType, startAt)
  slot = slot + 1
  local x, y = (slot % 5) * 2000 - 4000, (slot // 5) * 2500 - 1500
  local c = calm(CreateUnit(Player(0), FourCC("Hamg"), x, y, 0))
  SetUnitState(c, UNIT_STATE_MANA, GetUnitState(c, UNIT_STATE_MAX_MANA))
  BlzSetUnitBaseDamage(c, 0, 0) BlzSetUnitDiceNumber(c, 0, 0)
  if id then UnitAddAbility(c, FourCC(id)) end
  local t = calm(CreateUnit(Player(1), FourCC(foeType or "hfoo"), x + 300, y, 180))
  local t2 = calm(CreateUnit(Player(1), FourCC("hfoo"), x + 500, y, 180))
  if kind == "corpse" then local d = CreateUnit(Player(1), FourCC("hfoo"), x + 300, y + 100, 0) KillUnit(d) t = d end
  PauseUnit(t, true) PauseUnit(t2, true)
  local m0, l0 = GetUnitState(c, UNIT_STATE_MANA), GetWidgetLife(t2)
  after(startAt or 1, function()
    local ok = false
    if kind == "none" then ok = true elseif kind == "point" then ok = IssuePointOrder(c, order, x + 300, y)
    elseif kind == "target" or kind == "corpse" then ok = IssueTargetOrder(c, order, t)
    else ok = IssueImmediateOrder(c, order) end
    out(string.format("%s level %d order %s id %d cost %.0f", label, id and GetUnitAbilityLevel(c, FourCC(id)) or 0, tostring(ok), order and OrderId(order) or 0, m0 - 0))
    for _, s in ipairs({1, 3, 8}) do
      after(s, function()
        out(string.format("%s +%d near %s far %s foe mana %s,%s caster mana %s speed %s own %d foes %d", label, s, f1(l0 - GetWidgetLife(t)), f1(l0 - GetWidgetLife(t2)),
          f1(GetUnitState(t, UNIT_STATE_MANA)), f1(GetUnitState(t2, UNIT_STATE_MANA)), f1(GetUnitState(c, UNIT_STATE_MANA)), f1(GetUnitMoveSpeed(c)), count(Player(0), x, y), count(Player(1), x, y)))
      end)
    end
  end)
end
run("control", nil, nil, "none")
run("flamestrike", "AHfs", "flamestrike", "point")
run("starfall", "AEsf", "starfall", "immediate", nil, 14)
run("silence", "ANsi", "silence", "point")
run("windwalk", "AOwk", "windwalk", "immediate")
run("carrion", "AUcb", "carrionscarabs", "corpse")
run("immolation", "AEim", "immolation", "immediate")
run("mirror", "AOmi", "mirrorimage", "immediate")
run("earthquake", "AOeq", "earthquake", "point")
run("banish", "AHbn", "banish", "target")
run("locust", "AUls", "locustswarm", "immediate")
after(26, function() done() end)
