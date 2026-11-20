using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>
/// A7 and A11 (D700): the state at a step doesn't depend on how the run is advanced or on how often it is read. One map is advanced as one call,
/// in steps of 1, 3, 12 and 60 seconds, and with the hash read at every step, and all end on the same hash, and each reading is the one the run
/// that reads every second made at that time. The same for a recording replayed.
/// </summary>
public sealed class StepDeterminismTests : IDisposable
{
    readonly TestMaps _maps = new();
    public void Dispose() => _maps.Dispose();

    static StandardData Standard() => new StandardData()
        .Add(ObjectKind.Unit, "hmag", new Dictionary<string, string> { ["unam"] = "Mage", ["uhpm"] = "300", ["umvs"] = "270", ["umvt"] = "foot", ["umpm"] = "200", ["umpi"] = "10", ["umpr"] = "0.9", ["ucol"] = "31" });

    // Mana that comes back by itself and is read only by the hash and by one chat event, timers, a wait and a chat event
    const string Main = """
        local mage = CreateUnit(Player(0), FourCC("hmag"), 0, 0, 0)
        SetRandomSeed(7)
        TimerStart(CreateTimer(), 1.3, true, function()
            IssuePointOrder(mage, "move", GetRandomReal(-500, 500), GetRandomReal(-500, 500))
        end)
        local talk = CreateTrigger()
        TriggerRegisterPlayerChatEvent(talk, Player(0), "go", true)
        TriggerAddAction(talk, function()
            TriggerSleepAction(2.5)
            SetUnitState(mage, UNIT_STATE_MANA, GetUnitState(mage, UNIT_STATE_MANA) / 2)
            PauseUnit(mage, true)
            TriggerSleepAction(1.1)
            PauseUnit(mage, false)
        end)
        """;

    Simulation Open() => Simulation.Load(_maps.Build($$"""
        function config() SetPlayers(2) end
        function main()
        {{Main}}
        end
        """), "", data: Standard());

    // Advances one run to the end in parts, reading the hash after each when asked: the hash by time of every part's end
    static SortedDictionary<double, string> Advance(Simulation simulation, double total, double part, bool read)
    {
        var hashes = new SortedDictionary<double, string>();
        for (var at = part; at <= total + 1e-9; at += part)
        {
            simulation.Advance(TimeSpan.FromSeconds(part));
            hashes[at] = read ? simulation.StateHash() : "";
        }
        if (simulation.Step < total * 1024) simulation.AdvanceSteps((long)(total * 1024) - simulation.Step);
        hashes[total] = simulation.StateHash();
        return hashes;
    }

    static void AssertSame(string label, Func<Simulation> open, double total, Action<Simulation>? before = null)
    {
        using var reference = open();
        before?.Invoke(reference);
        var every = Advance(reference, total, 1, true);
        foreach (var part in new[] { total, 1, 3, 12, 60 }.Where(part => part <= total))
            foreach (var read in new[] { false, true })
            {
                using var simulation = open();
                before?.Invoke(simulation);
                var seen = Advance(simulation, total, part, read);
                Assert.True(every[total] == seen[total], $"{label}: in parts of {part} s, read {read}: {seen[total]}, advancing a second at a time: {every[total]}");
                foreach (var (at, hash) in seen.Where(pair => pair.Value != "" && every.ContainsKey(pair.Key)))
                    Assert.True(every[at] == hash, $"{label}: at {at} s in parts of {part} s: {hash}, a second at a time: {every[at]}");
            }
    }

    [Fact]
    public void A_made_up_map_ends_on_one_hash_however_it_is_advanced_or_read()
    {
        AssertSame("made-up", Open, 120, simulation => simulation.Say(0, "go"));
    }

    [Fact]
    public void A_recording_replays_to_the_same_hash_in_whole_or_in_parts()
    {
        Recording recording;
        using (var recorded = Open())
        {
            recorded.Advance(TimeSpan.FromSeconds(5));
            recorded.Say(0, "go");
            recorded.Advance(TimeSpan.FromSeconds(30));
            recorded.Say(0, "go");
            recorded.Advance(TimeSpan.FromSeconds(30));
            recording = recorded.Recording;
        }
        var steps = recording.Steps;
        string Replay(long every)
        {
            using var simulation = Open();
            var inputs = recording.Inputs.OrderBy(i => i.Step).ToList();
            var next = 0;
            for (var at = every; ; at = Math.Min(at + every, steps))
            {
                while (next < inputs.Count && inputs[next].Step <= at)
                {
                    if (inputs[next].Step > simulation.Step) simulation.AdvanceSteps(inputs[next].Step - simulation.Step);
                    simulation.Give(inputs[next++]);
                }
                simulation.AdvanceSteps(at - simulation.Step);
                if (every < steps) simulation.StateHash();
                if (at >= steps) return simulation.StateHash();
            }
        }
        var whole = Replay(steps);
        foreach (var seconds in new[] { 1, 3, 12, 60 }) Assert.Equal(whole, Replay(seconds * 1024L));
        using var followed = Open();
        followed.Follow(recording);
        Assert.Equal(whole, followed.StateHash());
    }

    [InstallFact]
    [Trait("Needs", "Game")]
    public void A_melee_map_with_computers_ends_on_one_hash_however_it_is_advanced_or_read()
    {
        AssertSame("Adrenaline", () => Simulation.Load("War3.w3mod:Maps/(4)Adrenaline.w3m", new SimulationOptions { Users = [0], Computers = [1, 2, 3] }), 120);
    }
}
