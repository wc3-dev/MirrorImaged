using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>What a person does at a player: keys, the mouse, selecting, orders and leaving, as a test gives them and a recording repeats them.</summary>
public sealed class InputTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static StandardData Standard() => new StandardData()
        .Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=100;umvs=270;umvt=foot;uacq=500;uaen=1;ua1w=normal;ua1t=normal;ua1g=ground;ua1b=5;ua1d=2;ua1s=3;ua1c=1;ua1r=90;udp1=0.5;ucol=31;udef=0;udty=normal"));

    string Map(string main) => _maps.Build($$"""
        function config() SetPlayers(2) end
        function main()
        {{main}}
        end
        """);

    [Fact]
    public void A_key_press_fires_the_key_registrations_with_the_key_and_whether_it_is_down()
    {
        using var simulation = Simulation.Load(Map("""
            local t = CreateTrigger()
            BlzTriggerRegisterPlayerKeyEvent(t, Player(0), OSKEY_A, 0, true)
            TriggerAddAction(t, function() print("key", GetPlayerId(GetTriggerPlayer()), GetHandleId(BlzGetTriggerPlayerKey()) == GetHandleId(OSKEY_A), BlzGetTriggerPlayerIsKeyDown(), BlzIsKeyPressed(OSKEY_A)) end)
            """), "", data: Standard());

        simulation.Key(0, 65, true);
        simulation.Key(0, 65, false);
        simulation.Key(0, 66, true);
        simulation.Advance(TimeSpan.FromSeconds(1));

        Assert.Single(simulation.Log, line => line.Contains("key"));
        Assert.Contains(simulation.Log, line => line.EndsWith("key\t0\ttrue\ttrue\ttrue"));
    }

    [Fact]
    public void The_mouse_fires_its_events_with_the_button_and_the_place()
    {
        using var simulation = Simulation.Load(Map("""
            local t = CreateTrigger()
            TriggerRegisterPlayerEvent(t, Player(0), EVENT_PLAYER_MOUSE_DOWN)
            TriggerAddAction(t, function() print("mouse", BlzGetTriggerPlayerMouseX(), BlzGetTriggerPlayerMouseY(), BlzGetTriggerPlayerMouseButton() == MOUSE_BUTTON_TYPE_RIGHT) end)
            """), "", data: Standard());

        simulation.Mouse(0, 305, 3, 120, -40);
        simulation.Advance(TimeSpan.FromSeconds(1));

        Assert.Contains(simulation.Log, line => line.EndsWith("mouse\t120.0\t-40.0\ttrue"));
    }

    [Fact]
    public void Selecting_fires_selected_and_deselected_events_and_reads_back()
    {
        using var simulation = Simulation.Load(Map("""
            local a = CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
            local b = CreateUnit(Player(0), FourCC("hfoo"), 100, 0, 0)
            local t, d = CreateTrigger(), CreateTrigger()
            TriggerRegisterPlayerUnitEvent(t, Player(0), EVENT_PLAYER_UNIT_SELECTED, nil)
            TriggerRegisterPlayerUnitEvent(d, Player(0), EVENT_PLAYER_UNIT_DESELECTED, nil)
            TriggerAddAction(t, function() print("selected", GetUnitName(GetTriggerUnit())) end)
            TriggerAddAction(d, function() print("deselected", GetUnitName(GetTriggerUnit())) end)
            TimerStart(CreateTimer(), 5, false, function() print("is", IsUnitSelected(a, Player(0)), IsUnitSelected(b, Player(0))) end)
            """), "", data: Standard());
        var units = simulation.Units(0);

        simulation.Select(0, units[0].Id);
        simulation.Select(0, units[1].Id);
        simulation.Advance(TimeSpan.FromSeconds(6));

        Assert.Equal(["selected", "deselected", "selected"], simulation.Log.Select(line => line.Split('\t')[0].Split(' ')[^1]).Where(word => word is "selected" or "deselected"));
        Assert.Contains(simulation.Log, line => line.EndsWith("is\tfalse\ttrue"));
    }

    [Fact]
    public void A_click_on_a_displayed_dialog_fires_the_button_then_the_dialog_events_and_closes_it()
    {
        using var simulation = Simulation.Load(Map("""
            local d = DialogCreate()
            local one, two = DialogAddButton(d, "One", 49), DialogAddButton(d, "Two", 50)
            local tb, td = CreateTrigger(), CreateTrigger()
            TriggerRegisterDialogButtonEvent(tb, two)
            TriggerRegisterDialogEvent(td, d)
            TriggerAddAction(tb, function() print("button", GetPlayerId(GetTriggerPlayer()), GetClickedButton() == two, GetClickedDialog() == d) end)
            TriggerAddAction(td, function() print("dialog", GetPlayerId(GetTriggerPlayer()), GetClickedButton() == two, GetClickedDialog() == d) end)
            TimerStart(CreateTimer(), 1, false, function() DialogDisplay(Player(0), d, true) end)
            TimerStart(CreateTimer(), 4, false, function() print("after", GetClickedButton(), GetClickedDialog()) end)
            """), "", data: Standard());

        simulation.Click(0, 1);
        simulation.Advance(TimeSpan.FromSeconds(2));
        simulation.Click(1, 1);
        simulation.Click(0, 5);
        simulation.Click(0, 1);
        simulation.Click(0, 1);
        simulation.Advance(TimeSpan.FromSeconds(3));

        Assert.Equal(["button\t0\ttrue\ttrue", "dialog\t0\ttrue\ttrue", "after\tnil\tnil"], simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..]));
    }

    [Fact]
    public void A_hidden_dialog_takes_no_click_and_a_quit_button_ends_the_run()
    {
        using var simulation = Simulation.Load(Map("""
            local d = DialogCreate()
            local stay, quit = DialogAddButton(d, "Stay", 49), DialogAddQuitButton(d, false, "Quit", 81)
            local t = CreateTrigger()
            TriggerRegisterDialogButtonEvent(t, stay)
            TriggerAddAction(t, function() print("stay") end)
            DialogDisplay(Player(0), d, true)
            DialogDisplay(Player(0), d, false)
            TimerStart(CreateTimer(), 1, false, function() DialogDisplay(Player(0), d, true) end)
            """), "", data: Standard());

        simulation.Click(0, 0);
        simulation.Advance(TimeSpan.FromSeconds(2));
        simulation.Click(0, 1);

        Assert.Empty(simulation.Log);
        Assert.True(simulation.Ended);
    }

    [Fact]
    public void Leaving_marks_the_slot_left_and_fires_the_leave_event()
    {
        using var simulation = Simulation.Load(Map("""
            local t = CreateTrigger()
            TriggerRegisterPlayerEvent(t, Player(0), EVENT_PLAYER_LEAVE)
            TriggerAddAction(t, function() print("left", GetPlayerId(GetTriggerPlayer()), GetPlayerSlotState(Player(0)) == PLAYER_SLOT_STATE_LEFT) end)
            """), "", data: Standard());

        simulation.Leave(0);
        simulation.Leave(0);
        simulation.Advance(TimeSpan.FromSeconds(1));

        Assert.Single(simulation.Log, line => line.Contains("left"));
        Assert.Contains(simulation.Log, line => line.EndsWith("left\t0\ttrue"));
    }

    [Fact]
    public void A_player_orders_only_its_own_units_and_a_test_waits_for_the_result()
    {
        using var simulation = Simulation.Load(Map("""
            CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
            CreateUnit(Player(1), FourCC("hfoo"), 0, 500, 0)
            """), "", data: Standard());
        var (mine, theirs) = (simulation.Units(0)[0].Id, simulation.Units(1)[0].Id);

        simulation.Order(0, mine, "move", 600, 0);
        simulation.Order(0, theirs, "move", 600, 500);
        var arrived = simulation.Until(() => simulation.Unit(mine)!.X > 590, TimeSpan.FromSeconds(10));

        Assert.True(arrived);
        Assert.Equal(0, simulation.Unit(theirs)!.X, 1);
    }

    [Fact]
    public void A_unit_view_gives_its_order_as_the_id_and_the_string()
    {
        using var simulation = Simulation.Load(Map("""CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)"""), "", data: Standard());
        var mine = simulation.Units(0)[0];

        simulation.Order(0, mine.Id, "move", 300, 0);
        var moving = simulation.Unit(mine.Id)!;
        var done = simulation.Until(() => simulation.Unit(mine.Id)!.Order == 0, TimeSpan.FromSeconds(10));
        var there = simulation.Unit(mine.Id)!;

        Assert.Equal((0, ""), (mine.Order, mine.OrderName));
        Assert.Equal((851986, "move"), (moving.Order, moving.OrderName));
        Assert.True(done);
        Assert.Equal((300, ""), (Math.Round(there.X), there.OrderName));
    }

    // Races are kept as the game's numbers, so a recording written before the Race enum reads the same
    [Fact]
    public void A_recording_keeps_the_races_as_numbers()
    {
        var recording = Recording.FromJson("""{"Map": "x.w3x", "Races": {"0": 3, "1": 1}}""");

        Assert.Equal(new Dictionary<int, Race> { [0] = Race.Undead, [1] = Race.Human }, recording.Races);
        Assert.Contains("\"0\": 3", recording.ToJson());
    }

    [Fact]
    public void A_recording_repeats_the_run_exactly()
    {
        var path = Map("""
            SetRandomSeed(3)
            for i = 0, 3 do
                CreateUnit(Player(0), FourCC("hfoo"), -200, i * 80, 0)
                CreateUnit(Player(1), FourCC("hfoo"), 200, i * 80, 0)
            end
            """);
        string hash;
        Recording recording;
        using (var simulation = Simulation.Load(path, "", data: Standard()))
        {
            simulation.Advance(TimeSpan.FromSeconds(2));
            var mine = simulation.Units(0)[0].Id;
            simulation.Order(0, mine, "attack", 200, 0);
            simulation.Advance(TimeSpan.FromSeconds(3));
            simulation.Say(0, "-hello");
            simulation.AdvanceSteps(1000);
            hash = simulation.StateHash();
            recording = Recording.FromJson(simulation.Recording.ToJson());
        }

        using var again = Simulation.Load(path, "", data: Standard());
        again.Follow(recording);

        Assert.Equal(hash, again.StateHash());
        Assert.Equal(recording.Steps, again.Step);
        Assert.Equal(["order", "say"], recording.Inputs.Select(input => input.Kind));
    }
}
