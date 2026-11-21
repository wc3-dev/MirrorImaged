-- UnitResetCooldown, BlzUnitEnableAuras, BlzResetUnitAttack and BlzUnitInterruptAttack (standard data). Every part runs at a place of its own at once and
-- reads on timers. Cooldown: a Paladin casts Holy Light (cooldown 5 s) on a hurt footman, and Divine Shield (35 s). Aura: a Paladin's Devotion Aura on a
-- footman 200 away. Attack: footmen of 10 damage with no dice attack a foe of 50000 life that does not fight back; a line gives each footman's attacked
-- and hit times after its order, and its order a little after the call.
local P0, P1 = Player(0), Player(PLAYER_NEUTRAL_AGGRESSIVE)
SetPlayerState(P0, PLAYER_STATE_RESOURCE_GOLD, 100000)
local clock = CreateTimer()
TimerStart(clock, 1000, false, nil)
local function now() return TimerGetElapsed(clock) end
local jobs, lines = {}, {}
local function later(delay, f) jobs[#jobs + 1] = {now() + delay, f} end
local function id(s) return FourCC(s) end
local function f2(x) return string.format("%.2f", x) end

-- Cooldowns
local pal = CreateUnit(P0, id("Hpal"), -1500, 1500, 0)
local hurt = CreateUnit(P0, id("hfoo"), -1400, 1500, 0)
SetWidgetLife(hurt, 100)
UnitAddAbility(pal, id("AHhb"))
UnitAddAbility(pal, id("AHds"))
SetUnitState(pal, UNIT_STATE_MANA, 1000)
local function left() return f2(BlzGetUnitAbilityCooldownRemaining(pal, id("AHhb"))) .. "/" .. f2(BlzGetUnitAbilityCooldownRemaining(pal, id("AHds"))) end
local cast = IssueTargetOrder(pal, "holybolt", hurt)
later(0.3, function()
  lines[#lines + 1] = string.format("cooldown: order %s, 0.3 s after: Holy Light/Divine Shield left %s, life of the foot %.0f", tostring(cast), left(), GetWidgetLife(hurt))
  IssueImmediateOrder(pal, "divineshield")
end)
later(1.0, function()
  lines[#lines + 1] = "cooldown 1.0 s: left " .. left() .. ", Divine Shield ordered; in the buff " .. tostring(GetUnitAbilityLevel(pal, id("BHds")))
  UnitResetCooldown(pal)
  lines[#lines + 1] = "cooldown after UnitResetCooldown: left " .. left()
  SetWidgetLife(hurt, 100)
  lines[#lines + 1] = "cast again: " .. tostring(IssueTargetOrder(pal, "holybolt", hurt))
end)
later(1.6, function() lines[#lines + 1] = string.format("cooldown 0.6 s after the second cast: left %s, life of the foot %.0f", left(), GetWidgetLife(hurt)) end)

-- Auras
local pa = CreateUnit(P0, id("Otch"), 1500, 1500, 0)
local fa = CreateUnit(P0, id("hfoo"), 1700, 1500, 0)
UnitAddAbility(pa, id("AOae"))
local speed = function() return string.format("%.1f/%.1f", GetUnitMoveSpeed(fa), GetUnitMoveSpeed(pa)) end
lines[#lines + 1] = "aura now: speed of the footman/the Tauren " .. speed()
later(3, function()
  lines[#lines + 1] = "aura 3 s: " .. speed()
  BlzUnitEnableAuras(pa, false, true)
end)
later(3.1, function() lines[#lines + 1] = "aura 0.1 s after off: " .. speed() end)
later(4, function() lines[#lines + 1] = "aura 1 s after off: " .. speed() end)
later(6, function()
  lines[#lines + 1] = "aura 3 s after off: " .. speed()
  BlzUnitEnableAuras(pa, true, false)
end)
later(6.1, function() lines[#lines + 1] = "aura 0.1 s after on: " .. speed() end)
later(7, function() lines[#lines + 1] = "aura 1 s after on: " .. speed() end)

-- Attacks
local sites, rows = 0, {}
local Aatk = id("Aatk")
local function fight(label, call, delay, from)
  sites = sites + 1
  local row = sites
  local x, y = -1600 + 340 * (sites - 1), -1500
  local u = CreateUnit(P0, id("hfoo"), x, y, 0)
  BlzSetUnitBaseDamage(u, 9, 0)
  BlzSetUnitDiceNumber(u, 1, 0)
  BlzSetUnitDiceSides(u, 1, 0)
  local foe = CreateUnit(P1, id("hfoo"), x + 100, y, 180)
  UnitRemoveAbility(foe, Aatk)
  SetUnitPropWindow(foe, 0)
  BlzSetUnitMaxHP(foe, 50000)
  SetWidgetLife(foe, 50000)
  local attacked, hit, t0 = {}, {}, now()
  local t = CreateTrigger()
  TriggerRegisterUnitEvent(t, foe, EVENT_UNIT_ATTACKED)
  TriggerAddAction(t, guard(function()
    if GetAttacker() == u then
      attacked[#attacked + 1] = now() - t0
      if from == "attacked" and #attacked == 1 and call then later(delay, function() call(u) end) end
    end
  end))
  t = CreateTrigger()
  TriggerRegisterUnitEvent(t, foe, EVENT_UNIT_DAMAGED)
  TriggerAddAction(t, guard(function()
    if GetEventDamageSource() == u then
      hit[#hit + 1] = now() - t0
      if from == "hit" and #hit == 1 and call then later(delay, function() call(u) end) end
    end
  end))
  t0 = now()
  local ordered = IssueTargetOrder(u, "attack", foe)
  local seen
  if from == "order" and call then later(delay, function() call(u) end) end
  later(8, function()
    local a, h = {}, {}
    for i = 1, math.min(#attacked, 5) do a[i] = f2(attacked[i]) end
    for i = 1, math.min(#hit, 5) do h[i] = f2(hit[i]) end
    rows[row] = string.format("attack %s: ordered %s; attacked %s; hit %s; order %s/%s", label, tostring(ordered), table.concat(a, " "), table.concat(h, " "), tostring(seen), OrderId2String(GetUnitCurrentOrder(u)))
  end)
  later(8 - 0.01, function() seen = OrderId2String(GetUnitCurrentOrder(u)) end)
end
fight("plain")
fight("interrupt 0.2 after the attacked event", function(u) BlzUnitInterruptAttack(u) end, 0.2, "attacked")
fight("interrupt 0.4 after the attacked event", function(u) BlzUnitInterruptAttack(u) end, 0.4, "attacked")
fight("interrupt 0.2 after the first hit", function(u) BlzUnitInterruptAttack(u) end, 0.2, "hit")
fight("interrupt 0.6 after the first hit", function(u) BlzUnitInterruptAttack(u) end, 0.6, "hit")
fight("reset 0.1 after the attacked event", function(u) BlzResetUnitAttack(u, 0) end, 0.1, "attacked")
fight("reset 0.2 after the first hit", function(u) BlzResetUnitAttack(u, 0) end, 0.2, "hit")
fight("reset 0.8 after the first hit", function(u) BlzResetUnitAttack(u, 0) end, 0.8, "hit")
fight("interrupt at once", function(u) BlzUnitInterruptAttack(u) end, 0.0, "order")

local ticker = CreateTimer()
TimerStart(ticker, 0.01, true, guard(function()
  local n = now()
  table.sort(jobs, function(a, b) return a[1] < b[1] end)
  while #jobs > 0 and jobs[1][1] <= n do
    local j = table.remove(jobs, 1)
    j[2]()
  end
  if #jobs == 0 then
    PauseTimer(ticker)
    for _, line in ipairs(lines) do out(line) end
    for _, line in ipairs(rows) do out(line) end
    done()
  end
end))
