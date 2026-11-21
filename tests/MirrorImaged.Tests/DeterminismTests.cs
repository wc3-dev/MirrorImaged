using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>
/// A7: the same map, seed and input give the same state hash after every step on every operating system. The hashes below were made on
/// Windows; CI runs this test on Windows, Linux and macOS, so a platform whose arithmetic, random numbers or ordering differs fails here.
/// </summary>
public sealed class DeterminismTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static StandardData Standard() => new StandardData()
        .Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=420;umvs=270;umvt=foot;uacq=500;uaen=1;ua1w=normal;ua1t=normal;ua1g=ground;ua1b=12;ua1d=2;ua1s=3;ua1c=1.35;ua1r=90;udp1=0.5;ucol=31;udef=2;udty=normal"));

    // Dice, trigonometry, a fight, timers and the order of many units
    const string Main = """
        SetRandomSeed(11)
        for i = 0, 9 do
            CreateUnit(Player(0), FourCC("hfoo"), -300 + math.cos(i) * 150, math.sin(i) * 150, 0)
            CreateUnit(Player(1), FourCC("hfoo"), 300 + math.cos(i * 2) * 150, math.sin(i * 3) * 150, 0)
        end
        TimerStart(CreateTimer(), 0.7, true, function()
            SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, GetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD) + GetRandomInt(1, 50))
            print("gold", GetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD), GetRandomReal(0, 1), math.sin(GetRandomReal(0, 100)), math.sqrt(GetRandomInt(2, 99)))
        end)
        """;

    [Fact]
    public void The_hash_at_each_second_is_the_same_on_every_platform()
    {
        var path = _maps.Build($$"""
            function config() end
            function main()
            {{Main}}
            end
            """);
        using var simulation = Simulation.Load(path, "", data: Standard());
        var hashes = new List<string>();
        for (var second = 1; second <= 20; second++)
        {
            simulation.Advance(TimeSpan.FromSeconds(1));
            if (second is 1 or 5 or 10 or 15 or 20) hashes.Add(simulation.StateHash());
        }

        Assert.Equal(Expected, hashes);
        Assert.Contains(simulation.Log, line => line.Contains("gold"));
    }

    static readonly string[] Expected = ["9320620CDB7918C5", "F6A677A55A6818B5", "E58EEC7CF8001752", "7974892A4FC235A0", "26889D8545F297CB"];
}
