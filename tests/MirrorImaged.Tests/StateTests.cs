using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>The state hash (the same map and seed give the same hash at every step), chat sent by a test, and what changes the hash.</summary>
public sealed class StateTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static StandardData Standard() => new StandardData()
        .Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=100;umvs=270;umvt=foot;uacq=500;uaen=1;ua1w=normal;ua1t=normal;ua1g=ground;ua1b=5;ua1d=2;ua1s=3;ua1c=1;ua1r=90;udp1=0.5;ucol=31;udef=0;udty=normal"));

    string Map(string main) => _maps.Build($$"""
        function config() end
        function main()
        {{main}}
        end
        """);

    // two runs of a fight with dice give the same hash at every second; another seed gives another fight
    [Fact]
    public void The_same_map_and_seed_give_the_same_hash_at_every_step()
    {
        List<string> Run(int seed)
        {
            var path = Map($"""
                SetRandomSeed({seed})
                for i = 0, 5 do
                    CreateUnit(Player(0), FourCC("hfoo"), -200, i * 80, 0)
                    CreateUnit(Player(1), FourCC("hfoo"), 200, i * 80, 0)
                end
                """);
            using var simulation = Simulation.Load(path, "", data: Standard());
            var hashes = new List<string> { simulation.StateHash() };
            for (var second = 0; second < 12; second++)
            {
                simulation.Advance(TimeSpan.FromSeconds(1));
                hashes.Add(simulation.StateHash());
            }
            return hashes;
        }

        var (first, second, other) = (Run(7), Run(7), Run(8));
        Assert.Equal(first, second);
        Assert.Equal(13, first.Distinct().Count());
        Assert.NotEqual(first, other);
    }

    // chat a test sends fires the registrations that match: a part of the message, or all of it when exact
    [Fact]
    public void Chat_sent_by_a_test_fires_the_matching_registrations()
    {
        using var simulation = Simulation.Load(Map("""
            local any, exact = CreateTrigger(), CreateTrigger()
            TriggerRegisterPlayerChatEvent(any, Player(0), "-go", false)
            TriggerRegisterPlayerChatEvent(exact, Player(0), "-go", true)
            TriggerAddAction(any, function() print("part", GetEventPlayerChatString(), GetEventPlayerChatStringMatched(), GetPlayerId(GetTriggerPlayer())) end)
            TriggerAddAction(exact, function() print("exact", GetEventPlayerChatString()) end)
            """), "", data: Standard());
        simulation.Say(0, "-go now");
        simulation.Say(0, "-go");
        simulation.Say(1, "-go");
        var lines = simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..]).ToList();

        Assert.Equal(["part\t-go now\t-go\t0", "part\t-go\t-go\t0", "exact\t-go"], lines);
    }
}
