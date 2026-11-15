using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>A worker harvesting gold: it walks to the mine, works, carries a load to the town hall, and goes back until the mine is spent.</summary>
public sealed class HarvestTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static StandardData Standard() => new StandardData()
        .Add(ObjectKind.Unit, "hpea", Values("unam=Peasant;uhpm=220;umvs=190;umvt=foot;uaen=0;ucol=31"))
        .Add(ObjectKind.Unit, "htow", Values("unam=Town Hall;uhpm=1500;ubdg=1;utyp=townhall;ucol=100;umvs=0"))
        .Add(ObjectKind.Unit, "ngol", Values("unam=Gold Mine;ubdg=1;uhpm=2000;uabi=Agld;umvs=0;ucol=80"))
        .Add(ObjectKind.Ability, "Agld", Values("anam=Gold Mine;alev=1;Gld1:1=12500"));

    [Fact]
    public void A_worker_carries_gold_from_the_mine_to_the_town_hall_until_the_mine_is_spent()
    {
        using var simulation = Simulation.Load(_maps.Build("""
            function config() end
            function main()
                local p = Player(0)
                SetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD, 0)
                local hall = CreateUnit(p, FourCC("htow"), 0, 0, 0)
                local mine = CreateUnit(Player(PLAYER_NEUTRAL_PASSIVE), FourCC("ngol"), 800, 0, 0)
                SetResourceAmount(mine, 25)
                local w = CreateUnit(p, FourCC("hpea"), 200, 0, 0)
                print(IssueTargetOrder(w, "harvest", mine), GetUnitCurrentOrder(w))
                TimerStart(CreateTimer(), 10, false, function() print("mid", GetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD), GetResourceAmount(mine)) end)
                TimerStart(CreateTimer(), 60, false, function() print("end", GetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD), GetResourceAmount(mine), GetUnitCurrentOrder(w)) end)
            end
            """), "", data: Standard());
        simulation.Advance(TimeSpan.FromSeconds(61));
        var lines = simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..]).ToList();

        Assert.Equal("true\t852018", lines[0]);
        Assert.Equal("end\t25\t0\t0", lines[2]);
        Assert.StartsWith("mid\t", lines[1]);
        Assert.True(int.Parse(lines[1].Split('\t')[1]) is >= 10 and <= 20, lines[1]);
    }
}
