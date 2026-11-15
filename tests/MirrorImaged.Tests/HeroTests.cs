using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>
/// Heroes, against the probes heroes, heroes-nonhero, heroes-nonhero-level, -str and -setstr (docs/facts.md, "Heroes
/// (W2-3)") and game-facts' "Heroes" experience tables, on maps with the data the test gives (decisions D207).
/// </summary>
public sealed class HeroTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static StandardData Standard(string constants = "")
    {
        var data = new StandardData()
            .Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=420;umpm=0;umpi=0"))
            .Add(ObjectKind.Unit, "Hpal", Values("unam=Paladin;upro=Granis Darkhammer,Jorn the Redeemer;uhpm=100;umpm=0;umpi=100;ustr=22;uagi=13;uint=17;ustp=2.7;uagp=1.5;uinp=1.8;uhab=AHhb,AHre;udef=2"))
            .Add(ObjectKind.Ability, "AHhb", Values("anam=Holy Light;alev=3;arlv=1;alsk=0"))
            .Add(ObjectKind.Ability, "AHre", Values("anam=Resurrection;alev=1;arlv=6;alsk=0"))
            .Add(ObjectKind.Ability, "AHbz", Values("anam=Blizzard;alev=3;arlv=1;alsk=0"));
        foreach (var (key, value) in Values("StrHitPointBonus=25;IntManaBonus=15;AgiDefenseBase=-2;AgiDefenseBonus=0.3" + constants))
            data.Constants[key] = value;
        return data;
    }

    Simulation Run(string main, string constants = "") => Simulation.Load(_maps.Build($$"""
        local function pid(p) return p == nil and "nil" or tostring(GetPlayerId(p)) end
        local function make(id, x) return CreateUnit(Player(0), FourCC(id), x or 0, 0, 0) end
        local function show(h)
            return table.concat({ GetHeroLevel(h), GetHeroXP(h), GetHeroSkillPoints(h), GetHeroStr(h, false), GetHeroAgi(h, false), GetHeroInt(h, false),
                GetWidgetLife(h), BlzGetUnitMaxHP(h), GetUnitState(h, UNIT_STATE_MANA), BlzGetUnitMaxMana(h), BlzGetUnitRealField(h, UNIT_RF_DEFENSE) }, " ")
        end
        function config() end
        function main()
        {{main}}
        end
        """), "", data: Standard(constants));

    static List<string> Printed(Simulation simulation) => [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..].Replace('\t', ' '))];

    // probe heroes: a new Paladin is level 1 with no experience and one skill point; its life and mana count its strength
    // and intelligence; each level adds the gains, whole attributes are the whole part (22 + 2.7 x 9 reads 46)
    [Fact]
    public void A_hero_starts_at_level_one_and_gains_its_attributes_a_level()
    {
        using var simulation = Run("""
            local h = make("Hpal")
            print(show(h))
            for _, level in ipairs({ 2, 3, 5, 10 }) do SetHeroLevel(h, level, false) print(show(h)) end
            """);

        Assert.Equal([
            "1 0 1 22 13 17 650.0 650 255.0 255 3.9",
            "2 200 2 24 14 18 700.0 700 270.0 270 4.2",
            "3 500 3 27 16 20 775.0 775 300.0 300 4.8",
            "5 1400 5 32 19 24 900.0 900 360.0 360 5.7",
            "10 5400 10 46 26 33 1250.0 1250 495.0 495 7.8",
        ], Printed(simulation));
    }

    // probe heroes: a level adds to the life and mana the unit has (325 of 650 at level 4 reads 525 of 850), not a share
    [Fact]
    public void A_levels_gain_is_added_to_the_life_and_mana_the_hero_has()
    {
        using var simulation = Run("""
            local h = make("Hpal")
            SetWidgetLife(h, 325)
            SetUnitState(h, UNIT_STATE_MANA, 100)
            SetHeroLevel(h, 4, false)
            print(show(h))
            local g = make("Hpal")
            BlzSetUnitRealField(g, UNIT_RF_STRENGTH_PER_LEVEL, 10)
            SetHeroLevel(g, 3, false)
            print(show(g))
            """);

        Assert.Equal(["4 900 4 30 17 22 525.0 850 175.0 330 5.1", "3 500 3 42 16 20 1150.0 1150 300.0 300 4.8"], Printed(simulation));
    }

    // probe heroes: SetHeroStr/Agi/Int take at once (0 and -5 too) and move the most life, mana and armour; the life and
    // mana move by the same; the fraction a level left is gone
    [Fact]
    public void An_attribute_set_moves_the_life_mana_and_armour()
    {
        using var simulation = Run("""
            local h = make("Hpal")
            SetHeroStr(h, 30, true) print(show(h))
            SetHeroAgi(h, 20, true) SetHeroInt(h, 25, true) print(show(h))
            SetHeroLevel(h, 2, false) print(show(h))
            SetHeroStr(h, 0, true) print(show(h))
            SetHeroStr(h, -5, false) print(show(h))
            local d = make("Hpal")
            SetWidgetLife(d, 325)
            SetUnitState(d, UNIT_STATE_MANA, 100)
            SetHeroStr(d, 32, true) SetHeroInt(d, 27, true) print(show(d))
            SetHeroStr(d, 12, true) SetHeroInt(d, 7, true) print(show(d))
            """);

        Assert.Equal([
            "1 0 1 30 13 17 850.0 850 255.0 255 3.9",
            "1 0 1 30 20 25 850.0 850 375.0 375 6.0",
            "2 200 2 32 21 26 900.0 900 390.0 390 6.3",
            "2 200 2 0 21 26 100.0 100 390.0 390 6.3",
            "2 200 2 -5 21 26 -25.0 -25 390.0 390 6.3",
            "1 0 1 32 13 27 575.0 900 250.0 405 3.9",
            "1 0 1 12 13 7 75.0 400 0.0 105 3.9",
        ], Printed(simulation));
    }

    // probe heroes: the attribute fields set the attribute (the field with bonus refuses); the permanent ones leave it 1;
    // a level field set from level 1 reads one more than set, from level 5 reads as set
    [Fact]
    public void The_attribute_and_level_fields_set_the_hero()
    {
        using var simulation = Run("""
            local h = make("Hpal")
            print(BlzSetUnitIntegerField(h, UNIT_IF_STRENGTH, 30), BlzSetUnitIntegerField(h, UNIT_IF_STRENGTH_WITH_BONUS, 5), show(h))
            print(BlzSetUnitIntegerField(h, UNIT_IF_STRENGTH_PERMANENT, 7), show(h))
            local five = make("Hpal")
            SetHeroLevel(five, 5, false)
            BlzSetUnitIntegerField(five, UNIT_IF_LEVEL, 3) print(show(five))
            local one = make("Hpal")
            BlzSetUnitIntegerField(one, UNIT_IF_LEVEL, 3) print(show(one))
            BlzSetUnitIntegerField(one, UNIT_IF_LEVEL, 1) print(show(one))
            """);

        Assert.Equal([
            "true false 1 0 1 30 13 17 850.0 850 255.0 255 3.9",
            "true 1 0 1 1 13 17 125.0 125 255.0 255 3.9",
            "3 500 3 27 16 20 775.0 775 300.0 300 4.8",
            "4 900 4 30 17 22 850.0 850 330.0 330 5.1",
            "1 0 1 22 13 17 650.0 650 255.0 255 3.9",
        ], Printed(simulation));
    }

    // game-facts "Heroes", probe heroes: experience only raises (SetHeroXP lower changes nothing), one level event inside
    // the call reading the final state, the player's event first, the experience capped at the last level's, AddHeroXP
    // lowering the experience but not the level, a suspended hero's three natives doing nothing, stripping taking levels
    [Fact]
    public void Experience_raises_levels_and_fires_one_event_inside_the_call()
    {
        using var simulation = Run("""
            local h = make("Hpal")
            local t = CreateTrigger()
            TriggerRegisterUnitEvent(t, h, EVENT_UNIT_HERO_LEVEL)
            TriggerRegisterPlayerUnitEvent(t, Player(0), EVENT_PLAYER_HERO_LEVEL, nil)
            TriggerAddAction(t, function()
                print("event", GetHandleId(GetTriggerEventId()), GetLevelingUnit() == h, GetTriggerUnit() == h, pid(GetTriggerPlayer()), show(h))
            end)
            SetHeroXP(h, 450, false) print(show(h))
            SetHeroXP(h, 100, false) print("lower", show(h))
            AddHeroXP(h, 100, false) print("add", show(h))
            AddHeroXP(h, -1000, false) print("minus", show(h))
            AddHeroXP(h, 1000, false) print("past", show(h))
            SuspendHeroXP(h, true)
            AddHeroXP(h, 1000, false) SetHeroXP(h, 99999, false) SetHeroLevel(h, 9, false) print("suspended", IsSuspendedXP(h), show(h))
            SuspendHeroXP(h, false)
            SetHeroXP(h, 99999, false) print("top", show(h))
            AddHeroXP(h, 100, false) print("top add", show(h))
            print(UnitStripHeroLevel(h, 2), show(h))
            print(UnitStripHeroLevel(h, 20), show(h))
            """);

        Assert.Equal([
            "event 41 true true 0 2 450 2 24 14 18 700.0 700 270.0 270 4.2",
            "event 78 true true 0 2 450 2 24 14 18 700.0 700 270.0 270 4.2",
            "2 450 2 24 14 18 700.0 700 270.0 270 4.2",
            "lower 2 450 2 24 14 18 700.0 700 270.0 270 4.2",
            "event 41 true true 0 3 550 3 27 16 20 775.0 775 300.0 300 4.8",
            "event 78 true true 0 3 550 3 27 16 20 775.0 775 300.0 300 4.8",
            "add 3 550 3 27 16 20 775.0 775 300.0 300 4.8",
            "minus 3 0 3 27 16 20 775.0 775 300.0 300 4.8",
            "event 41 true true 0 4 1000 4 30 17 22 850.0 850 330.0 330 5.1",
            "event 78 true true 0 4 1000 4 30 17 22 850.0 850 330.0 330 5.1",
            "past 4 1000 4 30 17 22 850.0 850 330.0 330 5.1",
            "suspended true 4 1000 4 30 17 22 850.0 850 330.0 330 5.1",
            "event 41 true true 0 10 5400 10 46 26 33 1250.0 1250 495.0 495 7.8",
            "event 78 true true 0 10 5400 10 46 26 33 1250.0 1250 495.0 495 7.8",
            "top 10 5400 10 46 26 33 1250.0 1250 495.0 495 7.8",
            "top add 10 5400 10 46 26 33 1250.0 1250 495.0 495 7.8",
            "true 8 3500 8 40 23 29 1100.0 1100 435.0 435 6.9",
            "true 1 0 1 22 13 17 650.0 650 255.0 255 3.9",
        ], Printed(simulation));
    }

    // game-facts "Heroes": the map's war3mapMisc gives levels 2 and 3 from NeedHeroXP's list as totals, the rest by the
    // formula with its fraction kept and cut when read, and the last level
    [Fact]
    public void The_experience_table_is_the_constants()
    {
        using var simulation = Run("""
            local h = make("Hpal")
            local xps = {}
            for level = 2, 15 do SetHeroLevel(h, level, false) xps[#xps + 1] = GetHeroXP(h) end
            print(table.concat(xps, " "))
            SetHeroLevel(h, 16, false) print(GetHeroLevel(h))
            local g = make("Hpal")
            SetHeroXP(g, 494, false) print(GetHeroLevel(g))
            SetHeroXP(g, 495, false) print(GetHeroLevel(g))
            """, ";MaxHeroLevel=15;NeedHeroXP=100,300;NeedHeroXPFormulaA=1.5;NeedHeroXPFormulaB=10;NeedHeroXPFormulaC=5");

        Assert.Equal(["100 300 495 797 1261 1966 3035 4647 7076 10730 16220 24465 36843 55420", "15", "3", "4"], Printed(simulation));
    }

    // probe heroes: a skill is learned at its required level (Holy Light's second at hero level 3, Resurrection's first at
    // 6) from the skill points, one skill event inside the call before the point is spent; points change by the amount
    // to 0 at the least; nothing for an ability the type doesn't list; the learned skill reads 0 outside the event
    [Fact]
    public void A_skill_is_learned_from_a_point_at_its_level()
    {
        using var simulation = Run("""
            local h = make("Hpal")
            local t = CreateTrigger()
            TriggerRegisterUnitEvent(t, h, EVENT_UNIT_HERO_SKILL)
            TriggerRegisterPlayerUnitEvent(t, Player(0), EVENT_PLAYER_HERO_SKILL, nil)
            TriggerAddAction(t, function()
                print("event", GetHandleId(GetTriggerEventId()), GetLearningUnit() == h, GetTriggerUnit() == h, pid(GetTriggerPlayer()),
                    GetLearnedSkill() == FourCC("AHhb"), GetLearnedSkillLevel(), GetHeroSkillPoints(h))
            end)
            local function learn(label, id) SelectHeroSkill(h, FourCC(id)) print(label, GetHeroSkillPoints(h), GetLearnedSkill(), GetLearnedSkillLevel()) end
            learn("ultimate at 1", "AHre")
            learn("AHhb", "AHhb")
            learn("no point", "AHhb")
            SetHeroLevel(h, 2, false)
            learn("level 2 at hero 2", "AHhb")
            SetHeroLevel(h, 3, false)
            learn("level 2 at hero 3", "AHhb")
            learn("not the type's", "AHbz")
            print(UnitModifySkillPoints(h, 3), GetHeroSkillPoints(h), UnitModifySkillPoints(h, -10), GetHeroSkillPoints(h))
            SetHeroLevel(h, 6, false)
            learn("ultimate at 6", "AHre")
            """);

        Assert.Equal([
            "ultimate at 1 1 0 0",
            "event 42 true true 0 true 1 1",
            "event 79 true true 0 true 1 1",
            "AHhb 0 0 0",
            "no point 0 0 0",
            "level 2 at hero 2 1 0 0",
            "event 42 true true 0 true 2 2",
            "event 79 true true 0 true 2 2",
            "level 2 at hero 3 1 0 0",
            "not the type's 1 0 0",
            "true 4 true 0",
            "event 42 true true 0 false 1 3",
            "event 79 true true 0 false 1 3",
            "ultimate at 6 2 0 0",
        ], Printed(simulation).Where(line => !line.StartsWith("event 41") && !line.StartsWith("event 78")));
    }

    // probe heroes: ReviveHero is false for a hero that lives; the start event fires with the hero dead where it died, then
    // it lives at the place with its most life and its type's starting mana, and the finish event fires, all inside the call
    [Fact]
    public void A_dead_hero_is_revived_with_events_inside_the_call()
    {
        using var simulation = Run("""
            local h = make("Hpal", 100)
            SetHeroInt(h, 30, true)
            SetHeroLevel(h, 3, false)
            local t = CreateTrigger()
            TriggerRegisterUnitEvent(t, h, EVENT_UNIT_HERO_REVIVE_START)
            TriggerRegisterUnitEvent(t, h, EVENT_UNIT_HERO_REVIVE_FINISH)
            TriggerRegisterPlayerUnitEvent(t, Player(0), EVENT_PLAYER_HERO_REVIVE_START, nil)
            TriggerRegisterPlayerUnitEvent(t, Player(0), EVENT_PLAYER_HERO_REVIVE_FINISH, nil)
            TriggerAddAction(t, function()
                print("event", GetHandleId(GetTriggerEventId()), GetRevivingUnit() == h, GetTriggerUnit() == h, UnitAlive(h), GetWidgetLife(h), GetUnitState(h, UNIT_STATE_MANA), GetUnitX(h), GetUnitY(h))
            end)
            print("living", ReviveHero(h, 0, 0, false))
            KillUnit(h)
            print("revived", ReviveHero(h, 300.5, -400.25, false), show(h), GetUnitX(h), GetUnitY(h))
            print("footman", ReviveHero(make("hfoo"), 0, 0, false))
            """);

        Assert.Equal([
            "living false",
            "event 44 true true false 0.0 495.0 100.0 0.0",
            "event 81 true true false 0.0 495.0 100.0 0.0",
            "event 46 true true true 775.0 100.0 300.5 -400.25",
            "event 83 true true true 775.0 100.0 300.5 -400.25",
            "revived true 3 500 3 27 16 33 775.0 775 100.0 495 4.8 300.5 -400.25",
            "footman false",
        ], Printed(simulation));
    }

    // probe heroes: the names are the type's, drawn at random and numbered once taken; BlzSetHeroProperName and the string
    // field change the one hero's, not its unit name; a footman has none
    [Fact]
    public void A_hero_has_a_proper_name_of_its_type()
    {
        using var simulation = Run("""
            local names = {}
            for i = 1, 6 do names[i] = GetHeroProperName(make("Hpal")) end
            print(table.concat(names, "|"))
            local h = make("Hpal")
            BlzSetHeroProperName(h, "Probe Name")
            print(GetHeroProperName(h), BlzGetUnitStringField(h, UNIT_SF_PROPER_NAMES), GetUnitName(h))
            BlzSetUnitStringField(h, UNIT_SF_PROPER_NAMES, "Field Name")
            print(GetHeroProperName(h), BlzGetUnitStringField(h, UNIT_SF_PROPER_NAMES))
            local f = make("hfoo")
            BlzSetHeroProperName(f, "Probe Name")
            print("[" .. GetHeroProperName(f) .. "]", GetUnitName(f))
            """);

        var lines = Printed(simulation);
        var names = lines[0].Split('|');
        Assert.All(names, name => Assert.Matches(@"^(Granis Darkhammer|Jorn the Redeemer)( [IVX]+)?$", name));
        Assert.Equal(6, names.Distinct().Count());
        Assert.Equal(["Probe Name Probe Name Paladin", "Field Name Field Name", "[] Footman"], lines.Skip(1));
    }

    // probes heroes-nonhero, -level, -str and -setstr: on a footman the getters read 0 and the setters change nothing,
    // IsSuspendedXP is true before any suspend, and nothing closes the game
    [Fact]
    public void A_unit_that_is_no_hero_has_no_level_experience_or_attributes()
    {
        using var simulation = Run("""
            local f = make("hfoo")
            print(GetHeroLevel(f), GetHeroXP(f), GetHeroSkillPoints(f), IsSuspendedXP(f), "[" .. GetHeroProperName(f) .. "]", GetHeroStr(f, false), GetHeroAgi(f, true), GetHeroInt(f, true))
            SetHeroLevel(f, 3, false) SetHeroXP(f, 500, false) AddHeroXP(f, 500, false) SetHeroStr(f, 30, true) SelectHeroSkill(f, FourCC("AHhb"))
            print(GetHeroLevel(f), GetHeroXP(f), GetWidgetLife(f), BlzGetUnitMaxHP(f), UnitStripHeroLevel(f, 1), UnitModifySkillPoints(f, 3), GetHeroSkillPoints(f))
            KillUnit(f)
            print(ReviveHero(f, 0, 0, false), UnitAlive(f))
            print(GetHeroLevel(nil), GetHeroXP(nil), GetHeroStr(nil, true), IsSuspendedXP(nil))
            """);

        Assert.Equal(["0 0 0 true [] 0 0 0", "0 0 420.0 420 false false 0", "false false", "0 0 0 false"], Printed(simulation));
    }
}
