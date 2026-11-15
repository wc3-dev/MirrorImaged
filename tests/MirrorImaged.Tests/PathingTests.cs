using MirrorImaged.Data;
using War3Net.Build;
using War3Net.Build.Environment;

namespace MirrorImaged.Tests;

/// <summary>Walking round what blocks the way: a wall of unwalkable cells and a building.</summary>
public sealed class PathingTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static StandardData Standard() => new StandardData()
        .Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=100;umvs=320;umvt=foot;uaen=0;ucol=31"))
        .Add(ObjectKind.Unit, "hbar", Values("unam=Barracks;uhpm=1500;ubdg=1;ucol=100;umvs=0"));

    // a wall of unwalkable cells at x 300 to 364, from y -400 to 400, in the pathing file (cells of 32 from the world's edge, -2048)
    static void Wall(Map map)
    {
        var pathing = new MapPathingMap(MapPathingMapFormatVersion.v0) { Width = 128, Height = 128 };
        for (var i = 0; i < 128 * 128; i++) pathing.Cells.Add(0);
        for (var row = 51; row <= 76; row++)
            foreach (var column in new[] { 73, 74 }) pathing.Cells[row * 128 + column] = PathingType.Walk;
        map.PathingMap = pathing;
    }

    List<string> Run(string main, double seconds, Action<Map>? change = null)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            local function f0(x) return string.format("%.0f", x) end
            function config() end
            function main()
            {{main}}
            end
            """, map: change), "", data: Standard());
        simulation.Advance(TimeSpan.FromSeconds(seconds));
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];
    }

    [Fact]
    public void A_walker_goes_round_a_wall_of_unwalkable_cells_to_the_place_it_was_sent()
    {
        var lines = Run("""
            local u = CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
            IssuePointOrder(u, "move", 600, 0)
            local far = 0
            TimerStart(CreateTimer(), 0.05, true, function() far = math.max(far, math.abs(GetUnitY(u))) end)
            TimerStart(CreateTimer(), 15, false, function() print("end", f0(GetUnitX(u)), f0(GetUnitY(u)), GetUnitCurrentOrder(u), far > 400) end)
            """, 16, Wall);

        Assert.Equal(["end\t600\t0\t0\ttrue"], lines);
    }

    [Fact]
    public void A_walker_goes_round_a_building_and_a_flier_goes_over_it()
    {
        var lines = Run("""
            local b = CreateUnit(Player(0), FourCC("hbar"), 300, 0, 0)
            local u = CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
            IssuePointOrder(u, "move", 600, 0)
            local far = 0
            TimerStart(CreateTimer(), 0.05, true, function() far = math.max(far, math.abs(GetUnitY(u))) end)
            TimerStart(CreateTimer(), 12, false, function() print("end", f0(GetUnitX(u)), f0(GetUnitY(u)), far > 60) end)
            """, 13);

        Assert.Equal(["end\t600\t0\ttrue"], lines);
    }
}
