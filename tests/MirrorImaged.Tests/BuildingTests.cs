using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>Construction, training and research: payment, the queue, the events and their responses.</summary>
public sealed class BuildingTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static StandardData Standard() => new StandardData()
        .Add(ObjectKind.Unit, "hpea", Values("unam=Peasant;uhpm=220;umvs=190;umvt=foot;ubui=hbar,hhou;uaen=0;ucol=31"))
        .Add(ObjectKind.Unit, "hbar", Values("unam=Barracks;uhpm=1500;ubdg=1;ugol=160;ulum=60;ubld=10;utra=hfoo;ures=Rhme;ucol=80;umvs=0"))
        .Add(ObjectKind.Unit, "hhou", Values("unam=Farm;uhpm=500;ubdg=1;ugol=80;ulum=20;ubld=5;ufma=6;umvs=0"))
        .Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=420;umvs=270;ugol=135;ufoo=2;ubld=20;uaen=0;ucol=31"))
        .Add(ObjectKind.Unit, "halt", Values("unam=Altar of Kings;uhpm=900;ubdg=1;utra=Hamg;ucol=80;umvs=0"))
        .Add(ObjectKind.Unit, "Hamg", Values("unam=Archmage;uhpm=450;umvs=320;ugol=425;ulum=100;ufoo=5;ubld=55;uaen=0;ucol=32"))
        .Add(ObjectKind.Upgrade, "Rhme", Values("glvl=3;gglb=100;gglm=75;glmb=50;glmm=75;gtib=60;gtim=60"));

    List<string> Run(string main, double seconds)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            local p = Player(0)
            local function money(gold, lumber, cap)
                SetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD, gold)
                SetPlayerState(p, PLAYER_STATE_RESOURCE_LUMBER, lumber)
                SetPlayerState(p, PLAYER_STATE_RESOURCE_FOOD_CAP, cap or 10)
            end
            local function make(id, x, y) return CreateUnit(p, FourCC(id), x or 0, y or 0, 0) end
            local function trig(kind, text, fn)
                local t = CreateTrigger()
                TriggerRegisterPlayerUnitEvent(t, p, kind, nil)
                TriggerAddAction(t, fn)
            end
            local function state() return GetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD) .. "/" .. GetPlayerState(p, PLAYER_STATE_RESOURCE_LUMBER) .. "/" .. GetPlayerState(p, PLAYER_STATE_RESOURCE_FOOD_USED) .. "/" .. GetPlayerState(p, PLAYER_STATE_RESOURCE_FOOD_CAP) end
            function config() end
            function main()
            {{main}}
            end
            """), "", data: Standard());
        simulation.Advance(TimeSpan.FromSeconds(seconds));
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];
    }

    // a build order walks to the site, pays when the building starts, and the worker is busy until it is done
    [Fact]
    public void A_worker_pays_and_builds_a_building_that_finishes_after_its_build_time()
    {
        var lines = Run("""
            money(500, 200)
            local w = make("hpea")
            trig(EVENT_PLAYER_UNIT_CONSTRUCT_START, "", function() print("start", GetUnitName(GetConstructingStructure()), state()) end)
            trig(EVENT_PLAYER_UNIT_CONSTRUCT_FINISH, "", function() print("finish", GetUnitName(GetConstructedStructure()), GetUnitCurrentOrder(w) == 0) end)
            print(IssueBuildOrder(w, "hbar", 300, 0), IssueBuildOrder(w, "hfoo", 300, 0), state())
            TimerStart(CreateTimer(), 5, false, function() print("busy", GetUnitCurrentOrder(w) == FourCC("hbar")) end)
            """, 20);

        Assert.Equal(["true\tfalse\t500/200/0/10", "start\tBarracks\t340/140/0/10", "busy\ttrue", "finish\tBarracks\ttrue"], lines);
    }

    // A worker ordered elsewhere while its building goes up is free to build again (decisions D640)
    [Fact]
    public void A_worker_sent_away_from_its_building_builds_the_next_one()
    {
        var lines = Run("""
            money(1000, 500, 20)
            local w = make("hpea")
            IssueBuildOrder(w, "hhou", 300, 0)
            TimerStart(CreateTimer(), 3, false, function() IssuePointOrder(w, "move", -300, 0) end)
            TimerStart(CreateTimer(), 4, false, function() print(IssueBuildOrder(w, "hhou", 0, 300)) end)
            TimerStart(CreateTimer(), 15, false, function()
                local g = CreateGroup()
                GroupEnumUnitsOfPlayer(g, p, nil)
                local farms = 0
                ForGroup(g, function() if GetUnitTypeId(GetEnumUnit()) == FourCC("hhou") then farms = farms + 1 end end)
                print(farms, state())
            end)
            """, 16);

        Assert.Equal(["true", "2	840/460/0/32"], lines);
    }

    // A melee player's hero token pays for a hero: no gold or lumber, and a cancel gives the token back (decisions D641)
    [Fact]
    public void A_hero_token_pays_for_a_hero_and_comes_back_when_it_is_cancelled()
    {
        var lines = Run("""
            money(0, 0, 10)
            SetPlayerState(p, PLAYER_STATE_RESOURCE_HERO_TOKENS, 1)
            local a = make("halt")
            local function tokens() return GetPlayerState(p, PLAYER_STATE_RESOURCE_HERO_TOKENS) end
            print(IssueImmediateOrderById(a, FourCC("Hamg")), state(), tokens())
            IssueImmediateOrderById(a, 851976)
            print(state(), tokens())
            money(1000, 1000, 10)
            print(IssueImmediateOrderById(a, FourCC("Hamg")), state(), tokens())
            """, 1);

        Assert.Equal(["true	0/0/5/10	0", "0/0/0/10	1", "true	1000/1000/5/10	0"], lines);
    }

    [Fact]
    public void A_worker_that_cannot_pay_makes_nothing_and_a_cancel_gives_most_back()
    {
        var lines = Run("""
            money(100, 200)
            local w = make("hpea")
            trig(EVENT_PLAYER_UNIT_CONSTRUCT_START, "", function() print("start") end)
            trig(EVENT_PLAYER_UNIT_CONSTRUCT_CANCEL, "", function() print("cancel", GetUnitName(GetCancelledStructure()), state()) end)
            IssueBuildOrder(w, "hbar", 300, 0)
            TimerStart(CreateTimer(), 5, false, function() print("poor", state(), GetUnitCurrentOrder(w)) money(500, 200) IssueBuildOrder(w, "hbar", 300, 0) end)
            TimerStart(CreateTimer(), 9, false, function()
                local g = CreateGroup()
                GroupEnumUnitsOfPlayer(g, p, nil)
                local b = FirstOfGroup(g)
                while b ~= nil and not IsUnitType(b, UNIT_TYPE_STRUCTURE) do GroupRemoveUnit(g, b) b = FirstOfGroup(g) end
                IssueImmediateOrderById(b, 851976)
            end)
            """, 15);

        Assert.Equal(["poor\t100/200/0/10\t0", "start", "cancel\tBarracks\t460/185/0/10"], lines);
    }

    // a building trains a unit it lists: paid and food held at once, the events, the unit at the end; without food it refuses
    [Fact]
    public void A_building_trains_a_unit_with_its_events_and_holds_the_food()
    {
        var lines = Run("""
            money(1000, 0, 4)
            local b = make("hbar")
            trig(EVENT_PLAYER_UNIT_TRAIN_START, "", function() print("start", GetTrainedUnitType() == FourCC("hfoo"), state()) end)
            trig(EVENT_PLAYER_UNIT_TRAIN_FINISH, "", function() print("finish", GetUnitName(GetTrainedUnit()), GetUnitName(GetTriggerUnit()), state()) end)
            trig(EVENT_PLAYER_UNIT_TRAIN_CANCEL, "", function() print("cancel", GetTrainedUnitType() == FourCC("hfoo"), state()) end)
            print(IssueImmediateOrderById(b, FourCC("hfoo")), IssueImmediateOrderById(b, FourCC("hfoo")), IssueImmediateOrderById(b, FourCC("hfoo")), state())
            IssueImmediateOrderById(b, 851976)
            """, 50);

        Assert.Equal(["start\ttrue\t865/0/2/4", "true\ttrue\tfalse\t730/0/4/4", "cancel\ttrue\t865/0/2/4", "finish\tFootman\tBarracks\t865/0/2/4"], lines.Select(line => line.Replace("start\ttrue\t865/0/2/4", "start\ttrue\t865/0/2/4")).ToList());
    }

    [Fact]
    public void A_building_researches_an_upgrade_level_by_level()
    {
        var lines = Run("""
            money(1000, 300)
            local b = make("hbar")
            trig(EVENT_PLAYER_UNIT_RESEARCH_START, "", function() print("start", GetResearched() == FourCC("Rhme"), state()) end)
            trig(EVENT_PLAYER_UNIT_RESEARCH_FINISH, "", function() print("finish", GetResearched() == FourCC("Rhme"), GetPlayerTechCount(p, FourCC("Rhme"), true), GetUnitName(GetResearchingUnit())) end)
            print(IssueImmediateOrderById(b, FourCC("Rhme")), state())
            TimerStart(CreateTimer(), 61, false, function() print(IssueImmediateOrderById(b, FourCC("Rhme")), state()) end)
            """, 200);

        Assert.Equal(["start\ttrue\t900/250/0/10", "true\t900/250/0/10", "finish\ttrue\t1\tBarracks", "start\ttrue\t725/125/0/10", "true\t725/125/0/10", "finish\ttrue\t2\tBarracks"], lines);
    }
}
