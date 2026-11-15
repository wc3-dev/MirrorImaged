using MirrorImaged.Data;
using War3Net.Build;
using War3Net.Build.Environment;

namespace MirrorImaged.Tests;

/// <summary>
/// Pathing, blight, painted tiles and craters against the facts' "Terrain changes" (game-facts.md, probes of 11-09) and
/// the pathing facts of 10-25 and 10-26, on a 32 by 32 tile map whose pathing file blocks one cell.
/// </summary>
public sealed class TerrainChangesTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    // The map's pathing grid is 128 cells a side; the cell under (0, 0) is walkable and unbuildable, found from the world's left edge
    static void Pathing(Map map)
    {
        var left = map.Environment!.Left;
        var pathing = new MapPathingMap(MapPathingMapFormatVersion.v0) { Width = 128, Height = 128 };
        for (var i = 0; i < 128 * 128; i++) pathing.Cells.Add(0);
        var cell = (int)((0 - map.Environment.Bottom) / 32) * 128 + (int)((0 - left) / 32);
        pathing.Cells[cell] = PathingType.Build | PathingType.Walk;
        map.PathingMap = pathing;
    }

    List<string> Run(string main, Action<Simulation>? after = null)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            local function f2(x) return string.format("%.2f", x) end
            function config() end
            function main()
            {{main}}
            end
            """, map: Pathing), "", data: new StandardData());
        after?.Invoke(simulation);
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];
    }

    // facts: true means not pathable; a cell is 32 on the world's grid; SetTerrainPathable changes one cell for one kind, at once
    [Fact]
    public void Pathing_comes_from_the_map_and_one_cell_changes_for_one_kind()
    {
        var lines = Run("""
            local function kinds(x, y)
                return tostring(IsTerrainPathable(x, y, PATHING_TYPE_WALKABILITY)) .. "," .. tostring(IsTerrainPathable(x, y, PATHING_TYPE_BUILDABILITY)) .. "," .. tostring(IsTerrainPathable(x, y, PATHING_TYPE_FLYABILITY))
            end
            print(kinds(0, 0), kinds(31, 31), kinds(32, 0), kinds(-1, 0))
            SetTerrainPathable(0, 0, PATHING_TYPE_WALKABILITY, true)
            SetTerrainPathable(40, 40, PATHING_TYPE_FLYABILITY, false)
            print(kinds(0, 0), kinds(31, 31), kinds(40, 40), kinds(63, 63), kinds(64, 40))
            print(IsTerrainPathable(1e9, 0, PATHING_TYPE_WALKABILITY))
            """);

        Assert.Equal(["true,true,false\ttrue,true,false\tfalse,false,false\tfalse,false,false", "false,true,false\tfalse,true,false\tfalse,false,true\tfalse,false,true\tfalse,false,false", "true"], lines);
    }

    // facts: SetBlight of radius 300 reached 240 along a line read every 16, and taking it off with 150 cleared to 112; it changes nothing but the blight answer
    [Fact]
    public void Blight_reaches_eight_tenths_of_its_radius_and_touches_nothing_else()
    {
        var lines = Run("""
            local function row()
                local t = {}
                for x = 0, 272, 16 do t[#t + 1] = IsPointBlighted(x, 0) and "1" or "0" end
                return table.concat(t)
            end
            print(row(), IsTerrainPathable(0, 0, PATHING_TYPE_BLIGHTPATHING))
            SetBlight(Player(0), 0, 0, 300, true)
            print(row(), IsTerrainPathable(0, 0, PATHING_TYPE_BLIGHTPATHING), IsTerrainPathable(0, 0, PATHING_TYPE_WALKABILITY), IsTerrainPathable(0, 0, PATHING_TYPE_BUILDABILITY))
            SetBlight(Player(0), 0, 0, 150, false)
            print(row())
            SetBlightPoint(Player(0), 1000, 1000, true)
            print(IsPointBlighted(1000, 1000), IsPointBlighted(1000 + 600, 1000))
            SetBlightRect(Player(0), Rect(-500, -500, -300, -300), true)
            print(IsPointBlighted(-400, -400), IsPointBlighted(-200, -400))
            SetBlightLoc(Player(0), Location(-1000, -1000), 100, true)
            print(IsPointBlighted(-1000, -1000))
            """);

        Assert.Equal([
            "000000000000000000\tfalse",
            "111111111111111100\ttrue\ttrue\ttrue",
            "000000001111111100",
            "true\tfalse",
            "true\tfalse",
            "true",
        ], lines.Select(line => line.Replace(" ", "")).ToList());
    }

    // facts: SetTerrainType paints the corners around the corner nearest the point, a half rounding up: area 0 none, area n the corners within n - 1 of it, a circle for shape 0 and a square for shape 1
    [Fact]
    public void Painting_covers_the_corners_around_the_nearest_one()
    {
        var lines = Run("""
            local mark = FourCC("Dlav")
            local function painted()
                local n = 0
                for dy = -5, 5 do for dx = -5, 5 do if GetTerrainType(dx * 128, dy * 128) == mark then n = n + 1 end end end
                return n
            end
            local function paint(x, area, shape)
                SetTerrainType(x, 0, mark, 0, area, shape)
                local n = painted()
                SetTerrainType(x, 0, FourCC("Ldrt"), 0, 7, 1)
                return n
            end
            print(painted(), paint(0, 0, 0), paint(0, 1, 0), paint(0, 2, 0), paint(0, 3, 0), paint(0, 2, 1), paint(0, 3, 1))
            SetTerrainType(64, 0, mark, 3, 1, 0)
            print(GetTerrainType(0, 0) == mark, GetTerrainType(128, 0) == mark, GetTerrainVariance(128, 0), GetTerrainType(1e9, 0))
            """);

        Assert.Equal(["0 0 1 5 13 9 25", "false true 3 0"], lines.Select(line => line.Replace('\t', ' ')).ToList());
    }

    // facts: a lasting crater moves the 128 corners by -depth x cos(90 degrees x distance / radius) from the next moment, over its duration; craters add up; stopping takes it off from the next moment
    [Fact]
    public void A_crater_moves_the_corners_it_covers_from_the_next_moment_until_stopped()
    {
        var lines = Run("""
            local at = Location(0, 0)
            local function z(x, y) MoveLocation(at, x, y) return f2(GetLocationZ(at)) end
            local crater
            print(z(0, 0))
            crater = TerrainDeformCrater(0, 0, 256, -300, 0, true)
            print(z(0, 0))
            TimerStart(CreateTimer(), 0.5, false, function()
                print(z(0, 0), z(128, 0), z(256, 0), z(64, 0))
                local second = TerrainDeformCrater(0, 0, 256, -100, 0, true)
                TerrainDeformStop(crater, 0)
                print(z(0, 0))
            end)
            TimerStart(CreateTimer(), 1, false, function() print(z(0, 0)) end)
            """, simulation => simulation.Advance(TimeSpan.FromSeconds(2)));

        Assert.Equal(["0.00", "0.00", "300.00	212.13	0.00	256.07", "300.00", "100.00"], lines);
    }
}
