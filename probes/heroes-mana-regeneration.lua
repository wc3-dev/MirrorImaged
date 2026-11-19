-- How much mana comes back to a unit by itself: Archmages of an intelligence that is set, other heroes and casters as they are, a paused hero and
-- one that is full. Standard data. Each unit has 5000 mana at most and 500 of it from 1 s in (the full one 5000 of 5000), and a line gives its
-- type's mana regeneration as the natives read it, its intelligence, and the mana it has more 10, 20 and 30 s later (a hair under none reads none).
local function after(s, f) TimerStart(CreateTimer(), s, false, guard(f)) end
local sites, rows = 0, {}
local function row(label, id, setup, mana)
  sites = sites + 1
  local u = CreateUnit(Player(0), FourCC(id), -1350 + 450 * ((sites - 1) % 7), -1350 + 450 * ((sites - 1) // 7), 0)
  if setup then setup(u) end
  BlzSetUnitMaxMana(u, 5000)
  rows[#rows + 1] = { label = label, u = u, from = mana or 500, got = {} }
end
for _, intelligence in ipairs({ 0, 1, 10, 19, 40, 100, 300 }) do
  row("an Archmage of intelligence " .. intelligence, "Hamg", function(u) SetHeroInt(u, intelligence, true) end)
end
for _, id in ipairs({ "Hpal", "Hblm", "Udea", "Ulic", "Ofar", "Edem", "hmpr", "hsor", "uobs", "nmed" }) do row("a " .. id, id) end
row("an Archmage, paused", "Hamg", function(u) PauseUnit(u, true) end)
row("an Archmage, full", "Hamg", nil, 5000)
row("an Archmage, hidden", "Hamg", function(u) ShowUnit(u, false) end)

after(1, function()
  for _, r in ipairs(rows) do SetUnitState(r.u, UNIT_STATE_MANA, r.from) end
  for i = 1, 3 do
    after(10 * i, function()
      for _, r in ipairs(rows) do r.got[i] = math.max(GetUnitState(r.u, UNIT_STATE_MANA) - r.from, 0) end
    end)
  end
  after(30.5, function()
    for _, r in ipairs(rows) do
      out(string.format("%s: regeneration %.2f, intelligence %d; mana back %.2f, %.2f, %.2f", r.label, BlzGetUnitRealField(r.u, UNIT_RF_MANA_REGENERATION),
        IsUnitType(r.u, UNIT_TYPE_HERO) and GetHeroInt(r.u, true) or 0, r.got[1], r.got[2], r.got[3]))
    end
    done()
  end)
end)
