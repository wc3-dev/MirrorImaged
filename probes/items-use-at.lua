-- What using a carried item at a place and at a target does (UnitUseItemPoint and UnitUseItemTarget): the answer, the order and the events inside the call
-- and a second and a half later, the charges and the mana, for the items of the game that need a target, those that don't and a target that is wrong, far,
-- an enemy or the hero itself, and for a call with a nil item, unit or target, a dead unit and an item on the ground. Each unit is made on a spot of its own
-- (a grid 200 apart).
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

-- using an item at a place and at a target, each case alone: the call, then a second and a half later
local function using(t, label, fn)
  local hero, mate, item, events
  step(0.2, "use " .. label, function()
    hero = make("Hpal")
    SetHeroLevel(hero, 5, false)
    SetUnitState(hero, UNIT_STATE_MANA, 1000)
    local x, y = GetUnitX(hero), GetUnitY(hero)
    mate = CreateUnit(Player(0), FourCC("hfoo"), x + 150, y, 0)
    SetWidgetLife(mate, 200)
    item = UnitAddItemById(hero, FourCC(t))
    events = {}
    local trigger = CreateTrigger()
    TriggerRegisterUnitEvent(trigger, hero, EVENT_UNIT_ISSUED_POINT_ORDER)
    TriggerRegisterUnitEvent(trigger, hero, EVENT_UNIT_ISSUED_TARGET_ORDER)
    TriggerRegisterUnitEvent(trigger, hero, EVENT_UNIT_ISSUED_ORDER)
    TriggerRegisterUnitEvent(trigger, hero, EVENT_UNIT_USE_ITEM)
    TriggerRegisterUnitEvent(trigger, hero, EVENT_UNIT_SPELL_CHANNEL)
    TriggerRegisterUnitEvent(trigger, hero, EVENT_UNIT_SPELL_CAST)
    TriggerRegisterUnitEvent(trigger, hero, EVENT_UNIT_SPELL_EFFECT)
    TriggerRegisterUnitEvent(trigger, hero, EVENT_UNIT_SPELL_FINISH)
    TriggerRegisterUnitEvent(trigger, hero, EVENT_UNIT_SPELL_ENDCAST)
    TriggerAddAction(trigger, function()
      events[#events + 1] = GetHandleId(GetTriggerEventId()) .. ":" .. GetIssuedOrderId() .. ":" .. show(GetSpellAbilityId() ~= 0 and name(GetSpellAbilityId()) or nil)
    end)
    local answer = fn(hero, item, mate, x, y)
    out(label .. " " .. t .. " answer " .. show(answer) .. " inside " .. table.concat(events, " ") .. " order " .. GetUnitCurrentOrder(hero)
      .. " charges " .. (item and GetItemCharges(item) or -1) .. " mana " .. math.floor(GetUnitState(hero, UNIT_STATE_MANA)))
  end)
  step(1.5, "later", function()
    out(label .. " " .. t .. " later events " .. table.concat(events, " ") .. " order " .. GetUnitCurrentOrder(hero) .. " charges "
      .. (item and GetItemCharges(item) or -1) .. " carried " .. show(item and UnitHasItem(hero, item)) .. " mana " .. math.floor(GetUnitState(hero, UNIT_STATE_MANA))
      .. " mate life " .. math.floor(GetWidgetLife(mate)) .. " at " .. math.floor(GetUnitX(hero) - GetUnitX(mate)))
  end)
end
for _, t in ipairs({ "phea", "wneg", "stwp", "wlsd", "ssil", "ankh", "ratc" }) do
  using(t, "point", function(h, i, m, x, y) return UnitUseItemPoint(h, i, x + 300, y + 100) end)
  using(t, "target", function(h, i, m) return UnitUseItemTarget(h, i, m) end)
end
using("wneg", "point near", function(h, i, m, x, y) return UnitUseItemPoint(h, i, x + 50, y) end)
using("wneg", "point on itself", function(h, i, m, x, y) return UnitUseItemPoint(h, i, x, y) end)
using("ssil", "point near", function(h, i, m, x, y) return UnitUseItemPoint(h, i, x + 50, y) end)
using("wlsd", "target far", function(h, i, m, x, y) return UnitUseItemTarget(h, i, CreateUnit(Player(0), FourCC("hfoo"), x + 900, y, 0)) end)
using("wlsd", "target an enemy", function(h, i, m, x, y) return UnitUseItemTarget(h, i, CreateUnit(Player(PLAYER_NEUTRAL_AGGRESSIVE), FourCC("hfoo"), x + 100, y + 100, 0)) end)
using("wlsd", "target itself", function(h, i) return UnitUseItemTarget(h, i, h) end)
using("stwp", "target a building", function(h, i, m, x, y) return UnitUseItemTarget(h, i, CreateUnit(Player(0), FourCC("htow"), x + 300, y + 300, 0)) end)
using("phea", "point of another's item", function(h, i, m, x, y) return UnitUseItemPoint(m, i, x + 300, y) end)
using("wneg", "point no item", function(h, i, m, x, y) return UnitUseItemPoint(h, nil, x + 300, y) end)
using("wneg", "target nil", function(h, i, m) return UnitUseItemTarget(h, i, nil) end)
using("wneg", "point nil unit", function(h, i, m, x, y) return UnitUseItemPoint(nil, i, x, y) end)
using("wneg", "point dead", function(h, i, m, x, y) KillUnit(h) return UnitUseItemPoint(h, i, x + 300, y) end)
using("wneg", "point item on ground", function(h, i, m, x, y) UnitRemoveItem(h, i) return UnitUseItemPoint(h, i, x + 300, y) end)
run(1)
