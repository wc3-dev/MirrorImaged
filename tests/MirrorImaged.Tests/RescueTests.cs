using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>
/// Rescuing, against the probes units-rescue and units-rescue-range (facts, "Units"): a neutral unit made rescuable by a
/// player is checked every 0.5 s from then, and the first check that finds a unit of that player within its range fires the rescue
/// events, the owner's registrations first, with that unit as the rescuer; the owner doesn't change, and nothing fires again.
/// </summary>
public sealed class RescueTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static StandardData Standard() => new StandardData()
        .Add(ObjectKind.Unit, "hfoo", Values("name=footman;unam=Footman;uhpm=420;udtm=3.04;udea=3;ufoo=2;umvt=foot;ucol=31"))
        .Add(ObjectKind.Unit, "Hpal", Values("name=paladin;unam=Paladin;uhpm=100;udtm=1.5;udea=2;ufoo=5;umvt=foot;ucol=32;ustr=22;uint=17;umpm=0;umpi=100"))
        .Add(ObjectKind.Unit, "hbar", Values("name=humanbarracks;ubdg=1;uhpm=1500;udtm=2.34;udea=2;ucol=144"));

    // The script is main's body, with a clock, a log of the rescue events (the player's, then the unit's) and a rescuable footman of
    // the neutral player made `far` away from the player's own footman at the origin
    List<string> Run(string main, double seconds = 5)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            local clock = CreateTimer()
            TimerStart(clock, 1000, false, nil)
            local function now() return string.format("%.2f", TimerGetElapsed(clock)) end
            local function pid(p) return p == nil and "nil" or tostring(GetPlayerId(p)) end
            local neutral = Player(PLAYER_NEUTRAL_PASSIVE)
            local function watch(label, u)
                local t = CreateTrigger()
                TriggerRegisterUnitEvent(t, u, EVENT_UNIT_RESCUED)
                TriggerAddAction(t, function()
                    print(label .. " unit " .. now() .. " " .. tostring(GetTriggerUnit() == u) .. " rescuer " .. GetUnitTypeId(GetRescuer()) .. " owner " .. pid(GetOwningPlayer(u)) .. " trigger player " .. pid(GetTriggerPlayer()))
                end)
            end
            local players = CreateTrigger()
            for _, p in ipairs({ Player(0), neutral }) do TriggerRegisterPlayerUnitEvent(players, p, EVENT_PLAYER_UNIT_RESCUED, nil) end
            TriggerAddAction(players, function() print("player " .. now() .. " " .. pid(GetTriggerPlayer()) .. " " .. GetUnitTypeId(GetTriggerUnit()) .. " by " .. GetUnitTypeId(GetRescuer())) end)
            function config() end
            function main()
            {{main}}
            end
            """), "", data: Standard());
        simulation.Advance(TimeSpan.FromSeconds(seconds));
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];
    }

    const string Footman = "1751543663";

    [Fact]
    public void The_first_check_half_a_second_later_that_finds_a_rescuer_in_range_fires_the_events_once_and_the_owner_stays()
    {
        var lines = Run("""
            CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
            TimerStart(CreateTimer(), 1.2, false, function()
                local u = CreateUnit(neutral, FourCC("hfoo"), 300, 0, 0)
                watch("a", u)
                SetUnitRescuable(u, Player(0), true)
            end)
            """);

        Assert.Equal([$"player 1.70 27 {Footman} by {Footman}", "a unit 1.70 true rescuer " + Footman + " owner 27 trigger player 27"], lines);
    }

    // probe units-rescue-range: a footman rescued a unit 414 away and not 415, a Paladin 415 and not 416
    [Theory]
    [InlineData("hfoo", 414, true)]
    [InlineData("hfoo", 415, false)]
    [InlineData("Hpal", 415, true)]
    [InlineData("Hpal", 416, false)]
    public void The_default_range_is_400_and_a_half_of_the_rescuers_size_less_a_half(string rescuer, int distance, bool rescued)
    {
        var lines = Run($$"""
            CreateUnit(Player(0), FourCC("{{rescuer}}"), 0, 0, 0)
            local u = CreateUnit(neutral, FourCC("hfoo"), {{distance}}, 0, 0)
            watch("a", u)
            SetUnitRescuable(u, Player(0), true)
            """, 3);

        Assert.Equal(rescued ? 1 : 0, lines.Count(line => line.Contains("a unit")));
    }

    [Fact]
    public void A_rescuer_out_of_range_waits_for_the_check_that_finds_it_in_range()
    {
        var lines = Run("""
            local r = CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
            local u = CreateUnit(neutral, FourCC("hfoo"), 900, 0, 0)
            watch("a", u)
            SetUnitRescuable(u, Player(0), true)
            TimerStart(CreateTimer(), 1.7, false, function() SetUnitPosition(r, 800, 0) end)
            """);

        Assert.Equal([$"player 2.00 27 {Footman} by {Footman}", "a unit 2.00 true rescuer " + Footman + " owner 27 trigger player 27"], lines);
    }

    [Fact]
    public void The_range_is_the_set_one_and_none_at_0()
    {
        var lines = Run("""
            CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
            local function rescuable(label, at, range)
                local u = CreateUnit(neutral, FourCC("hfoo"), at, 0, 0)
                if range then SetUnitRescueRange(u, range) end
                watch(label, u)
                SetUnitRescuable(u, Player(0), true)
            end
            rescuable("in", 250, 300)
            rescuable("out", 350, 300)
            rescuable("near", 100, 50)
            rescuable("zero", 10, 0)
            rescuable("negative", 10, -1)
            rescuable("far default", 600)
            """);

        Assert.Equal(["in unit 0.50 true rescuer " + Footman + " owner 27 trigger player 27"], lines.Where(line => line.Contains("unit")).ToList());
    }

    // The rescuer is a footman of the player at the origin and the rescuable unit 100 away, each set up as the case says
    [Theory]
    [InlineData("a living unit of the player", "", true)]
    [InlineData("a dead rescuer", "KillUnit(r)", false)]
    [InlineData("a hidden rescuer", "ShowUnit(r, false)", false)]
    [InlineData("the rescue switched off", "SetUnitRescuable(u, Player(0), false)", false)]
    [InlineData("a rescue by another player", "SetUnitRescuable(u, Player(0), false) SetUnitRescuable(u, Player(1), true)", false)]
    [InlineData("a unit of the player that is removed", "RemoveUnit(r)", false)]
    public void Only_a_living_shown_unit_of_a_player_the_unit_is_rescuable_by_rescues(string why, string setup, bool rescued)
    {
        var lines = Run($$"""
            local r = CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
            local u = CreateUnit(neutral, FourCC("hfoo"), 100, 0, 0)
            watch("case", u)
            SetUnitRescuable(u, Player(0), true)
            {{setup}}
            """, 3);

        Assert.True(lines.Count(line => line.Contains("case unit")) == (rescued ? 1 : 0), why);
    }

    [Fact]
    public void A_building_or_a_hero_in_range_rescues_too()
    {
        var lines = Run("""
            CreateUnit(Player(0), FourCC("hbar"), 0, 0, 0)
            local u = CreateUnit(neutral, FourCC("hfoo"), 100, 0, 0)
            watch("by a building", u)
            SetUnitRescuable(u, Player(0), true)
            """, 3);

        Assert.Equal(["by a building unit 0.50 true rescuer 1751277938 owner 27 trigger player 27"], lines.Where(line => line.Contains("unit")).ToList());
    }

    [Fact]
    public void A_unit_owned_by_a_player_is_rescued_on_that_players_registration()
    {
        var lines = Run("""
            CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
            local u = CreateUnit(neutral, FourCC("hfoo"), 100, 0, 0)
            watch("owned", u)
            SetUnitRescuable(u, Player(0), true)
            SetUnitOwner(u, Player(0), true)
            """);

        Assert.Equal([$"player 0.50 0 {Footman} by {Footman}", "owned unit 0.50 true rescuer " + Footman + " owner 0 trigger player 0"], lines);
    }

    [Fact]
    public void GetRescuer_is_nil_outside_a_rescue()
    {
        var lines = Run("print(GetRescuer())", 1);

        Assert.Equal(["nil"], lines);
    }
}
