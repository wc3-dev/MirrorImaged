-- Flame Strike, Silence, Wind Walk, Immolation, Mirror Image, Earthquake and Starfall (level 1, each added to an Archmage) on the standard data, every
-- caster and its foes inside the 32 x 32 map: nine places 1300 apart, a spell each, cast at once, and Starfall alone afterwards, as it reaches 1000.
-- A foe is a footman with its attack taken away (Aatk), as an acquisition range of 0 isn't taken (probe units), and not paused: a paused unit takes a
-- spell's first hit and none of its buffs, which one paused foe by each spell shows. A foe that is hurt runs, so each is held where it stands
-- (SetUnitPropWindow of 0). For each spell: when its effect comes and what it costs, each hit a foe takes (when, how much) by its distance from the
-- spell's place, and when its buff comes and goes. The heroes have 500 mana of 1000 and a hero that casts nothing tells what comes back by itself.
-- Times are from the spell's effect. A buff is looked for every 0.05 s and its line gives when it came, to 0.1 s, and how long it was on, so a
-- look a moment later in MirrorImaged than in the game reads the same; the orders come at 1.02 s, off the looks' own instants.
local clock = CreateTimer()
TimerStart(clock, 1000, false, nil)
local function now() return TimerGetElapsed(clock) end
local function after(s, f) TimerStart(CreateTimer(), s, false, guard(f)) end
local function every(s, f)
  local t = CreateTimer()
  TimerStart(t, s, true, guard(function() if f() then PauseTimer(t) DestroyTimer(t) end end))
end
local function tf(v) return v and "t" or "f" end
local function cc(id) return id > 0x20202020 and string.char((id >> 24) & 255, (id >> 16) & 255, (id >> 8) & 255, id & 255) or tostring(id) end
local Aatk = FourCC("Aatk")
local foes, spot = {}, {}
local function unit(p, id, x, y, armed)
  local u = CreateUnit(Player(p), FourCC(id), x, y, 0)
  if not armed then UnitRemoveAbility(u, Aatk) end
  if p == 1 then foes[#foes + 1], spot[u] = u, { GetUnitX(u), GetUnitY(u) } end
  return u
end
local function foe(x, y, paused, id)
  local u = unit(1, id or "hfoo", x, y)
  SetUnitPropWindow(u, 0)
  PauseUnit(u, paused == true)
  return u
end
local function tough(u) BlzSetUnitMaxHP(u, 5000) SetWidgetLife(u, 5000) BlzSetUnitArmor(u, 0) return u end
local function fixed(u) BlzSetUnitBaseDamage(u, 49, 0) BlzSetUnitDiceNumber(u, 1, 0) BlzSetUnitDiceSides(u, 1, 0) return u end
local function hero(ability, x, y, armed)
  local c = unit(0, "Hamg", x, y, armed)
  BlzSetUnitMaxMana(c, 1000)
  SetUnitState(c, UNIT_STATE_MANA, 500)
  if ability then UnitAddAbility(c, FourCC(ability)) end
  return c
end
local function site(i) return (i % 3 - 1) * 1300, (i // 3 - 1) * 1300 end
local function away(u, x, y) return math.sqrt((GetUnitX(u) - x) ^ 2 + (GetUnitY(u) - y) ^ 2) end

local control = hero(nil, site(0))
local function mana(u) return GetUnitState(u, UNIT_STATE_MANA) end
-- How many of the foes are not where they were made
local function strayed()
  local n = 0
  for _, u in ipairs(foes) do if away(u, spot[u][1], spot[u][2]) > 1 then n = n + 1 end end
  return n
end

-- A unit's hits: when, how much, and the types
local function hits(u)
  local h, t = {}, CreateTrigger()
  TriggerRegisterUnitEvent(t, u, EVENT_UNIT_DAMAGED)
  TriggerAddAction(t, guard(function() h[#h + 1] = { now(), GetEventDamage(), GetHandleId(BlzGetEventAttackType()), GetHandleId(BlzGetEventDamageType()) } end))
  return h
end
-- The hits from the first-th on, as runs of one amount and kind: "8 of 15.0 from +1.33 to +3.64 (attack type 0 damage type 8), 4.0 at +4.64", the
-- types given where they change
local function runs(h, t0, first)
  first = first or 1
  if #h < first then return "no hit" end
  local parts, i, types = {}, first
  while i <= #h do
    local j, kind = i, string.format(" (attack type %d damage type %d)", h[i][3], h[i][4])
    while j < #h and math.abs(h[j + 1][2] - h[i][2]) < 0.05 and h[j + 1][3] == h[i][3] and h[j + 1][4] == h[i][4] do j = j + 1 end
    if #parts == 5 then parts[6] = "and more" break end
    parts[#parts + 1] = (j == i and string.format("%.1f at +%.2f", h[i][2], h[i][1] - t0)
      or string.format("%d of %.1f from +%.2f to +%.2f", j - i + 1, h[i][2], h[i][1] - t0, h[j][1] - t0)) .. (kind ~= types and kind or "")
    i, types = j + 1, kind
  end
  return table.concat(parts, ", ")
end
local function since(h, t) local n = 0 for _, e in ipairs(h) do if e[1] > t then n = n + 1 end end return n end

-- Foes round a place, each { how far, x's part, y's part, paused }, and a line for each: its hits
local function ring(x, y, places, make)
  local r = {}
  for _, p in ipairs(places) do
    local u = (make or foe)(x + p[1] * p[2], y + p[1] * p[3], p[4])
    r[#r + 1] = { at = p[1] .. (p[4] and ", paused" or ""), u = u, h = hits(u) }
  end
  return r
end
local function report(label, r, s)
  for _, e in ipairs(r) do out(string.format("%s at %s: %s", label, e.at, runs(e.h, s.t0))) end
end

-- When something is so, looked at every 0.05 s: "+0.0 for 12.00 s", "+0.0 on" while it still is, "never"
local function watch(test, ended)
  local w = {}
  every(0.05, function()
    local so = test()
    if so and not w.on then w.on = now()
    elseif w.on and not so then
      w.off = now()
      if ended then ended() end
      return true
    end
  end)
  return w
end
local function buffed(u, id) return function() return GetUnitAbilityLevel(u, FourCC(id)) > 0 end end
local function span(w, t0)
  if not w.on then return "never" end
  return string.format("+%.1f ", w.on - t0) .. (w.off and string.format("for %.2f s", w.off - w.on) or "on")
end

-- Gives the order and prints it and the cast's events, those that fire inside the order's call after its line; s.t0 is the effect's time (the
-- order's until it comes)
local function cast(label, c, id, order, effect)
  local s, had, back, inside = { t0 = now() }, mana(c), mana(control), {}
  local function say(line) if inside then inside[#inside + 1] = line else out(line) end end
  function s.spent() return had - mana(c) + mana(control) - back end
  local a = BlzGetUnitAbility(c, FourCC(id))
  local function on(event, f)
    local t = CreateTrigger()
    TriggerRegisterUnitEvent(t, c, event)
    TriggerAddAction(t, guard(f))
  end
  on(EVENT_UNIT_SPELL_EFFECT, function()
    if s.effect then say(string.format("%s effect again +%.2f", label, now() - s.t0)) return end
    local ordered = s.t0
    s.t0, s.effect = now(), true
    say(string.format("%s effect %.2f after the order: mana spent %.0f, duration %.1f hero %.1f area %.0f range %.0f cooldown %.0f", label, s.t0 - ordered, s.spent(),
      BlzGetAbilityRealLevelField(a, ABILITY_RLF_DURATION_NORMAL, 0), BlzGetAbilityRealLevelField(a, ABILITY_RLF_DURATION_HERO, 0),
      BlzGetAbilityRealLevelField(a, ABILITY_RLF_AREA_OF_EFFECT, 0), BlzGetAbilityRealLevelField(a, ABILITY_RLF_CAST_RANGE, 0), BlzGetAbilityRealLevelField(a, ABILITY_RLF_COOLDOWN, 0)))
    if effect then effect() end
  end)
  on(EVENT_UNIT_SPELL_FINISH, function() say(string.format("%s finish +%.2f", label, now() - s.t0)) end)
  on(EVENT_UNIT_SPELL_ENDCAST, function() say(string.format("%s end cast +%.2f", label, now() - s.t0)) end)
  out(label .. " order " .. tf(order()))
  for _, line in ipairs(inside) do out(line) end
  inside = nil
  return s
end

-- Nothing cast: the foes lose nothing, and the hero's mana comes back
do
  local x, y = site(0)
  local near, far = foe(x + 300, y), foe(x + 500, y)
  local h1, h2, l0 = hits(near), hits(far), GetWidgetLife(near)
  after(25, function()
    out(string.format("control: lost %.1f and %.1f, hits %d", l0 - GetWidgetLife(near), l0 - GetWidgetLife(far), #h1 + #h2))
    out(string.format("control: mana back in 25 s %.1f; foes not where they were made %d", mana(control) - 500, strayed()))
  end)
end

-- Flame Strike at a place 400 away, foes at its middle and 200, 225 and 260 from it, and a paused one 100 from it
do
  local x, y = site(1)
  local c = hero("AHfs", x, y)
  local r = ring(x + 400, y, { { 0, 0, 0 }, { 200, 1, 0 }, { 225, -1, 0 }, { 260, 0, 1 }, { 100, 0, -1, true } })
  after(1.02, function()
    local s = cast("flamestrike", c, "AHfs", function() return IssuePointOrder(c, "flamestrike", x + 400, y) end)
    after(15, function() report("flamestrike", r, s) end)
  end)
end

-- Silence at a place 400 away: a Paladin with Divine Shield at its middle, Priests 100, 200, 225 and 260 from it, a paused one and a footman 100 from
-- it. The Paladin is ordered to shield itself two seconds in, and again once its buff is over
do
  local x, y = site(2)
  local c = hero("ANsi", x, y)
  local paladin = unit(1, "Hpal", x + 400, y)
  UnitAddAbility(paladin, FourCC("AHds"))
  local function shield(when)
    local ordered = IssueImmediateOrder(paladin, "divineshield")
    after(1, function() out(string.format("silence: the Paladin's Divine Shield order %s %s, its shield a second later %s", when, tf(ordered), tf(GetUnitAbilityLevel(paladin, FourCC("BHds")) > 0))) end)
  end
  local on = { { at = 0, w = watch(buffed(paladin, "BNsi"), function() shield("after its buff") end) } }
  local function priest(px, py, paused) return foe(px, py, paused, "hmpr") end
  for _, e in ipairs(ring(x + 400, y, { { 100, 0, 1 }, { 200, 1, 0 }, { 225, -1, 0 }, { 260, 0, -1 }, { 100, 0.6, -0.8, true } }, priest)) do on[#on + 1] = { at = e.at, w = watch(buffed(e.u, "BNsi")) } end
  on[#on + 1] = { at = "100, a footman", w = watch(buffed(foe(x + 320, y - 60), "BNsi")) }
  after(1.02, function()
    local s = cast("silence", c, "ANsi", function() return IssuePointOrder(c, "silence", x + 400, y) end, function() after(2, function() shield("under it") end) end)
    after(25, function()
      for i, e in ipairs(on) do out(string.format("silence at %s%s: buff %s", e.at, i == 1 and " (the Paladin)" or "", span(e.w, s.t0))) end
    end)
  end)
end

-- Wind Walk, with a foe 300 away to see the caster: its buff, when the foe's owner no longer sees it, and its speed
do
  local x, y = site(3)
  local c = hero("AOwk", x, y)
  foe(x + 300, y)
  local buff, unseen = watch(buffed(c, "BOwk")), watch(function() return IsUnitInvisible(c, Player(1)) end)
  after(1.02, function()
    local s = cast("windwalk", c, "AOwk", function() return IssueImmediateOrder(c, "windwalk") end, function()
      after(1, function() out(string.format("windwalk +1: speed %.1f of %.1f, visible to the foe %s", GetUnitMoveSpeed(c), GetUnitDefaultMoveSpeed(c), tf(IsUnitVisible(c, Player(1))))) end)
    end)
    after(25, function() out(string.format("windwalk: buff %s, invisible %s, speed %.1f", span(buff, s.t0), span(unseen, s.t0), GetUnitMoveSpeed(c))) end)
  end)
end

-- Wind Walk on a hero that attacks (50 a hit), a foe of armour 0 coming 200 away a second in, as in the 0.6 s before it is invisible the hero
-- attacked a foe in one run and not in the next: whether it attacks by itself, and what its hits take once it is ordered to, three seconds in
do
  local x, y = site(4)
  local c = fixed(hero("AOwk", x, y, true))
  local buff = watch(buffed(c, "BOwk"))
  after(1.02, function()
    local s
    s = cast("windwalk attacker", c, "AOwk", function() return IssueImmediateOrder(c, "windwalk") end, function()
      local victim, h, before
      after(1, function()
        victim = tough(foe(x + 200, y))
        h, before = hits(victim), 0
      end)
      after(3, function()
        out(string.format("windwalk attacker: hits before it is ordered: %s; order %d, attack order %s", runs(h, s.t0), GetUnitCurrentOrder(c), tf(IssueTargetOrder(c, "attack", victim))))
        before = #h
      end)
      after(9, function()
        PauseUnit(c, true)
        out("windwalk attacker: buff " .. span(buff, s.t0))
        out("windwalk attacker: hits " .. runs(h, s.t0, before + 1))
      end)
    end)
  end)
end

-- Immolation, foes 100, 160, 185 and 220 away and a paused one 100 away: switched on, the mana it has taken after 1.52, 3.52 and 8.52 s, and
-- switched off at 9.52 s
do
  local x, y = site(5)
  local c = hero("AEim", x, y)
  local r = ring(x, y, { { 100, 1, 0 }, { 160, -1, 0 }, { 185, 0, 1 }, { 220, 0, -1 }, { 100, 0, -1, true } })
  local buff = watch(buffed(c, "BEim"))
  after(1.02, function()
    local s, off
    s = cast("immolation", c, "AEim", function() return IssueImmediateOrder(c, "immolation") end, function()
      local spent = "immolation: mana spent"
      for _, t in ipairs({ 1.52, 3.52, 8.52 }) do after(t, function() spent = spent .. string.format(" +%.2f %.1f", now() - s.t0, s.spent()) end) end
      after(9.52, function()
        out(spent)
        off = now()
        out(string.format("immolation: off order +%.2f %s", off - s.t0, tf(IssueImmediateOrder(c, "unimmolation"))))
      end)
      after(13, function()
        report("immolation", r, s)
        out(string.format("immolation: buff %s, hits after the off order %d, mana spent %.1f", span(buff, s.t0), since(r[1].h, off), s.spent()))
      end)
    end)
  end)
end

-- Immolation on a hero with its cost and 20 mana more, a foe 100 away: when it stops by itself. The hero has intelligence 0, so that next to no mana
-- comes back to it
do
  local x, y = site(6)
  local c = unit(0, "Hamg", x, y)
  SetHeroInt(c, 0, true)
  BlzSetUnitMaxMana(c, 1000)
  UnitAddAbility(c, FourCC("AEim"))
  local cost = BlzGetUnitAbilityManaCost(c, FourCC("AEim"), 0)
  SetUnitState(c, UNIT_STATE_MANA, cost + 20)
  local h = hits(foe(x + 100, y))
  local s, left
  local buff = watch(buffed(c, "BEim"), function() left = mana(c) end)
  after(1.02, function()
    s = cast("immolation on little mana", c, "AEim", function() return IssueImmediateOrder(c, "immolation") end)
    after(13, function()
      out(string.format("immolation on little mana: cost %d, buff %s, under 1 mana left as it went %s; at 100: %s", cost, span(buff, s.t0), tf(left and left < 1), runs(h, s.t0)))
    end)
  end)
end

-- Mirror Image, a foe of armour 0 250 away: the summon event, where the caster and its image are, what 100 damage takes from each, what the image's
-- attack takes from the foe, and when the image is gone
do
  local x, y = site(7)
  local c = fixed(hero("AOmi", x, y))
  local victim = tough(foe(x + 250, y))
  local h = hits(victim)
  local deaths, s = 0
  local summon, death = CreateTrigger(), CreateTrigger()
  TriggerRegisterPlayerUnitEvent(summon, Player(0), EVENT_PLAYER_UNIT_SUMMON, nil)
  TriggerAddAction(summon, guard(function()
    local u = GetSummonedUnit()
    out(string.format("mirror: summon event +%.2f of %s, illusion %s, by the caster %s, the unit of the event %s", now() - s.t0, cc(GetUnitTypeId(u)), tf(IsUnitIllusion(u)), tf(GetSummoningUnit() == c),
      cc(GetUnitTypeId(GetTriggerUnit()))))
  end))
  TriggerRegisterPlayerUnitEvent(death, Player(0), EVENT_PLAYER_UNIT_DEATH, nil)
  TriggerAddAction(death, function() deaths = deaths + 1 end)
  local function took(u)
    local before = GetWidgetLife(u)
    UnitDamageTarget(victim, u, 100, false, false, ATTACK_TYPE_CHAOS, DAMAGE_TYPE_UNIVERSAL, WEAPON_TYPE_WHOKNOWS)
    return before - GetWidgetLife(u)
  end
  after(1.02, function()
    s = cast("mirror", c, "AOmi", function() return IssueImmediateOrder(c, "mirrorimage") end, function()
      after(1.5, function()
        local own, images, g = 0, {}, CreateGroup()
        GroupEnumUnitsInRange(g, x, y, 700, nil)
        ForGroup(g, function()
          local u = GetEnumUnit()
          if GetOwningPlayer(u) ~= Player(0) or not UnitAlive(u) then return end
          own = own + 1
          if u ~= c then images[#images + 1] = u end
        end)
        DestroyGroup(g)
        local image = images[1]
        out(string.format("mirror +1.5: own units %d, the caster %.0f, %.0f from its place", own, GetUnitX(c) - x, GetUnitY(c) - y))
        if not image then return end
        out(string.format("mirror image: %s %.0f, %.0f from the caster's place, illusion %s, life %.0f of %.0f, mana as the caster's %s, attack ability %d, damage %d + %dd%d", cc(GetUnitTypeId(image)), GetUnitX(image) - x, GetUnitY(image) - y,
          tf(IsUnitIllusion(image)), GetWidgetLife(image), GetUnitState(image, UNIT_STATE_MAX_LIFE), tf(math.abs(mana(c) - mana(image)) < 0.5), GetUnitAbilityLevel(image, Aatk),
          BlzGetUnitBaseDamage(image, 0), BlzGetUnitDiceNumber(image, 0), BlzGetUnitDiceSides(image, 0)))
        out(string.format("mirror: 100 of chaos and universal takes %.1f from the caster and %.1f from the image", took(c), took(image)))
        local before = #h
        UnitAddAbility(image, Aatk)
        out("mirror image: attack order " .. tf(IssueTargetOrder(image, "attack", victim)))
        after(5, function()
          out("mirror image: its hits " .. runs(h, s.t0, before + 1))
          UnitRemoveAbility(image, Aatk)
        end)
        watch(function() return UnitAlive(image) end, function()
          local gone = now()
          after(0.5, function() out(string.format("mirror image: gone +%.2f, death events %d", gone - s.t0, deaths)) end)
        end)
      end)
    end)
  end)
end

-- Earthquake at a place 384 away: a Farm at its middle and one 448 from it, a footman that is free 140 from it and one 330 from it. Stopped at 9.7 s
do
  local x, y = site(8)
  local c = hero("AOeq", x, y)
  local tx = x + 384
  local function farm(fx, fy) return tough(unit(1, "hhou", fx, fy)) end
  local r = ring(tx, y, { { 0, 0, 0 }, { 448, 0, -1 } }, farm)
  local inside, outside = unit(1, "hfoo", tx - 140, y), unit(1, "hfoo", tx, y + 330)
  local hi, ho = hits(inside), hits(outside)
  local slow = watch(function() return GetUnitMoveSpeed(inside) < GetUnitDefaultMoveSpeed(inside) end)
  after(1.02, function()
    local s, stopped
    s = cast("earthquake", c, "AOeq", function() return IssuePointOrder(c, "earthquake", tx, y) end, function()
      after(1.2, function()
        out(string.format("earthquake +1.2: the footman 140 away has speed %.1f of %.1f and buff %s, the one 330 away %.1f", GetUnitMoveSpeed(inside), GetUnitDefaultMoveSpeed(inside),
          tf(GetUnitAbilityLevel(inside, FourCC("BOeq")) > 0), GetUnitMoveSpeed(outside)))
      end)
      after(9.7, function()
        stopped = now()
        out(string.format("earthquake: stop order +%.2f %s", stopped - s.t0, tf(IssueImmediateOrder(c, "stop"))))
      end)
      after(13, function()
        report("earthquake", r, s)
        out(string.format("earthquake: hits after the stop %d, the footmen's hits %d, slow %s", since(r[1].h, stopped), #hi + #ho, span(slow, s.t0)))
      end)
    end)
  end)
end

-- Starfall alone, the other spells' foes gone, from the middle of the map (250 from its west edge it left foes 990 to 1030 east of it alone): foes
-- 300 to 2000 away, one of them paused, and the channel stopped at 10.2 s
after(27, function()
  for _, u in ipairs(foes) do RemoveUnit(u) end
  foes = {}
  local x, y = -650, -650
  local c = hero("AEsf", x, y)
  local r = ring(x, y, { { 300, 1, 0 }, { 500, 1, 0, true }, { 800, 1, 0 }, { 990, 0, 1 }, { 1030, 0.6, -0.8 }, { 1040, -0.6, 0.8 }, { 1200, 0, -1 }, { 2000, 1, 0 } })
  after(1.02, function()
    local s, stopped
    s = cast("starfall", c, "AEsf", function() return IssueImmediateOrder(c, "starfall") end, function()
      after(10.2, function()
        stopped = now()
        out(string.format("starfall: stop order +%.2f %s", stopped - s.t0, tf(IssueImmediateOrder(c, "stop"))))
      end)
      after(13, function()
        report("starfall", r, s)
        out(string.format("starfall: hits after the stop %d, foes not where they were made %d", since(r[1].h, stopped), strayed()))
      end)
    end)
  end)
end)
after(66, function() done() end)
