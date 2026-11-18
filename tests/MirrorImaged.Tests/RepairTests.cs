using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>A worker repairs a hurt building for a share of its price over a share of its build time.</summary>
public sealed class RepairTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static StandardData Standard() => new StandardData()
        .Add(ObjectKind.Unit, "hpea", Values("unam=Peasant;uhpm=220;umvs=190;umvt=foot;uaen=0;ucol=31"))
        .Add(ObjectKind.Unit, "hbar", Values("unam=Barracks;uhpm=1000;ubdg=1;ugol=160;ulum=60;ubld=10;ucol=80;umvs=0"));

    List<string> Run(string main, double seconds)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            local function f0(x) return string.format("%.0f", x) end
            function config() end
            function main()
            {{main}}
            end
            """), "", data: Standard());
        simulation.Advance(TimeSpan.FromSeconds(seconds));
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];
    }

    [Fact]
    public void A_worker_repairs_a_hurt_building_and_pays_a_share_of_its_price_and_stops_when_it_is_whole()
    {
        var lines = Run("""
            SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, 500)
            SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_LUMBER, 500)
            local barracks, worker = CreateUnit(Player(0), FourCC("hbar"), 0, 0, 0), CreateUnit(Player(0), FourCC("hpea"), 300, 0, 0)
            SetWidgetLife(barracks, 500)
            print(IssueTargetOrder(worker, "repair", barracks))
            TimerStart(CreateTimer(), 4, false, function() print("half", GetWidgetLife(barracks) > 650 and GetWidgetLife(barracks) < 800) end)
            TimerStart(CreateTimer(), 30, false, function()
                print("whole", f0(GetWidgetLife(barracks)), GetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD), GetPlayerState(Player(0), PLAYER_STATE_RESOURCE_LUMBER), GetUnitCurrentOrder(worker))
            end)
            """, 31);

        Assert.Equal(["true", "half\ttrue", "whole\t1000\t471\t489\t0"], lines);
    }

    [Fact]
    public void A_worker_stops_repairing_when_the_player_cannot_pay()
    {
        var lines = Run("""
            SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, 0)
            local barracks, worker = CreateUnit(Player(0), FourCC("hbar"), 0, 0, 0), CreateUnit(Player(0), FourCC("hpea"), 300, 0, 0)
            SetWidgetLife(barracks, 500)
            IssueTargetOrder(worker, "repair", barracks)
            TimerStart(CreateTimer(), 10, false, function() print(GetWidgetLife(barracks) < 530, GetUnitCurrentOrder(worker)) end)
            """, 11);

        Assert.Equal(["true\t0"], lines);
    }
}
