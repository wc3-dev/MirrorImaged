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

    static StandardData Standard()
    {
        var data = new StandardData()
            .Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=100;umvs=320;umvt=foot;uaen=0;ucol=31"))
            .Add(ObjectKind.Unit, "hbar", Values("unam=Barracks;uhpm=1500;ubdg=1;ucol=100;umvs=0"))
            .Add(ObjectKind.Unit, "nwgt", Values("unam=Way Gate;uhpm=500;ubdg=1;ucol=64;umvs=0"))
            .Add(ObjectKind.Unit, "hhou", Values(@"unam=Farm;uhpm=500;ubdg=1;ucol=72;umvs=0;upat=PathTexturesx4SimpleSolid.tga"))
            .Add(ObjectKind.Unit, "halt", Values(@"unam=Altar;uhpm=900;ubdg=1;ucol=144;umvs=0;upat=PathTexturesx10Simple.tga"));
        data.Footprints[@"PathTexturesx4SimpleSolid.tga"] = new Footprint(4, 4, [.. Enumerable.Repeat(true, 16)]);
        data.Footprints[@"PathTexturesx10Simple.tga"] = new Footprint(10, 10, [.. Enumerable.Range(0, 100).Select(i => i % 10 is >= 2 and < 8 && i / 10 is >= 2 and < 8)]);
        return data;
    }

    // a wall of unwalkable cells at x 300 to 364, from y -400 to 400, in the pathing file (cells of 32 from the world's edge, -2048)
    static void Wall(Map map)
    {
        var pathing = new MapPathingMap(MapPathingMapFormatVersion.v0) { Width = 128, Height = 128 };
        for (var i = 0; i < 128 * 128; i++) pathing.Cells.Add(0);
        for (var row = 51; row <= 76; row++)
            foreach (var column in new[] { 73, 74 }) pathing.Cells[row * 128 + column] = PathingType.Walk;
        map.PathingMap = pathing;
    }

    // the wall from the world's bottom edge to its top, so the ground is two pieces
    static void WholeWall(Map map)
    {
        Wall(map);
        for (var row = 0; row < 128; row++)
            foreach (var column in new[] { 73, 74 }) map.PathingMap!.Cells[row * 128 + column] = PathingType.Walk;
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
    public void A_walker_sent_to_ground_it_cant_walk_to_goes_through_an_active_way_gate_and_stops_at_its_ground_without_one()
    {
        var lines = Run("""
            local gate = CreateUnit(Player(PLAYER_NEUTRAL_PASSIVE), FourCC("nwgt"), -400, 0, 0)
            WaygateSetDestination(gate, 700, 500)
            WaygateActivate(gate, true)
            local u = CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
            IssuePointOrder(u, "move", 600, 0)
            local stuck = CreateUnit(Player(0), FourCC("hfoo"), 0, -600, 0)
            TimerStart(CreateTimer(), 4, false, function() WaygateActivate(gate, false) IssuePointOrder(stuck, "move", 600, -600) end)
            TimerStart(CreateTimer(), 15, false, function() print("end", f0(GetUnitX(u)), f0(GetUnitY(u)), GetUnitX(stuck) < 300) end)
            """, 16, WholeWall);

        Assert.Equal(["end	600	0	true"], lines);
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

    [Fact]
    public void A_walker_sets_out_round_a_building_at_once_and_loses_next_to_no_time()
    {
        // 1000 straight at 320 is 3.1 s; the way round the barracks's 108 circle at the start costs a little more, but not the stop and turn at its edge
        var lines = Run("""
            local b = CreateUnit(Player(0), FourCC("hbar"), 500, 0, 0)
            local u = CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
            IssuePointOrder(u, "move", 1000, 0)
            local t, bendsAt = 0, nil
            TimerStart(CreateTimer(), 0.05, true, function()
              t = t + 1
              if bendsAt == nil and math.abs(GetUnitY(u)) > 5 then bendsAt = GetUnitX(u) end
            end)
            TimerStart(CreateTimer(), 3.4, false, function() print("end", f0(GetUnitX(u)), f0(GetUnitY(u)), bendsAt < 200) end)
            """, 4);

        Assert.Equal(["end	1000	0	true"], lines);
    }

    [Fact]
    public void Unwalkable_ground_set_in_a_map_with_no_pathing_file_leaves_a_walker_at_the_nearest_ground_that_can_be_reached()
    {
        var lines = Run("""
            for i = -4, 3 do for j = -4, 3 do SetTerrainPathable(300 + i * 32 + 16, j * 32 + 16, PATHING_TYPE_WALKABILITY, false) end end
            local u = CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
            IssuePointOrder(u, "move", 300, 0)
            TimerStart(CreateTimer(), 8, false, function() print("end", math.abs(GetUnitY(u)) >= 128, GetUnitCurrentOrder(u)) end)
            """, 9);

        Assert.Equal(["end	true	0"], lines);
    }

    [Fact]
    public void A_building_made_stands_with_its_footprints_corner_on_the_grid_of_64()
    {
        var lines = Run("""
            for _, at in ipairs({ { "hhou", 40, 40 }, { "hhou", -50, 70 }, { "halt", 0, 0 }, { "hfoo", 40, 40 } }) do
              local b = CreateUnit(Player(0), FourCC(at[1]), at[2], at[3], 270)
              print(at[1], f0(GetUnitX(b)), f0(GetUnitY(b)))
            end
            """, 0.1);

        Assert.Equal(["hhou	0	0", "hhou	0	64", "halt	32	32", "hfoo	40	40"], lines);
    }

    [Fact]
    public void A_footman_keeps_16_off_a_footprint_and_passes_one_its_line_misses()
    {
        // The farm's footprint, 16 up and right of its place, is -48 to 80 each way: the line at y 100 clears it by 20, more than a footman's 16
        var lines = Run("""
            CreateUnit(Player(0), FourCC("hhou"), 0, 0, 270)
            local a = CreateUnit(Player(0), FourCC("hfoo"), -600, 100, 0)
            local b = CreateUnit(Player(0), FourCC("hfoo"), -600, -600, 0)
            IssuePointOrder(a, "move", 600, 100)
            IssuePointOrder(b, "move", 600, 600)
            local far, near = 0, 9999
            TimerStart(CreateTimer(), 0.05, true, function()
              far = math.max(far, math.abs(GetUnitY(a) - 100))
              near = math.min(near, math.max(math.abs(GetUnitX(b) - 16), math.abs(GetUnitY(b) - 16)))
            end)
            TimerStart(CreateTimer(), 6, false, function() print(f0(GetUnitX(a)), f0(GetUnitY(a)), far, f0(GetUnitX(b)), f0(GetUnitY(b)), near >= 64 + 16) end)
            """, 7);

        Assert.Equal(["600	100	0	600	600	true"], lines);
    }

    [Fact]
    public void A_walker_goes_round_a_unit_standing_in_its_way_and_one_with_pathing_off_walks_through()
    {
        var lines = Run("""
            local still = CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
            local a = CreateUnit(Player(0), FourCC("hfoo"), -600, 0, 0)
            local ghost = CreateUnit(Player(0), FourCC("hfoo"), -600, 300, 0)
            CreateUnit(Player(0), FourCC("hfoo"), 0, 300, 0)
            SetUnitPathing(ghost, false)
            IssuePointOrder(a, "move", 600, 0)
            IssuePointOrder(ghost, "move", 600, 300)
            local near, far = 9999, 0
            TimerStart(CreateTimer(), 0.05, true, function()
              near = math.min(near, SquareRoot((GetUnitX(a) - GetUnitX(still)) ^ 2 + (GetUnitY(a) - GetUnitY(still)) ^ 2))
              far = math.max(far, math.abs(GetUnitY(ghost) - 300))
            end)
            TimerStart(CreateTimer(), 6, false, function() print(f0(GetUnitX(a)), f0(GetUnitY(a)), near >= 62, f0(GetUnitX(still)), f0(GetUnitY(still)), f0(GetUnitX(ghost)), far) end)
            """, 7);

        Assert.Equal(["600	0	true	0	0	600	0"], lines);
    }

    [Fact]
    public void A_walker_sent_where_a_unit_stands_stops_next_to_it()
    {
        var lines = Run("""
            local still = CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
            local a = CreateUnit(Player(0), FourCC("hfoo"), -600, 0, 0)
            IssuePointOrder(a, "move", 0, 0)
            TimerStart(CreateTimer(), 4, false, function()
              local d = SquareRoot((GetUnitX(a) - GetUnitX(still)) ^ 2 + (GetUnitY(a) - GetUnitY(still)) ^ 2)
              print(GetUnitCurrentOrder(a), d >= 62 and d < 80)
            end)
            """, 5);

        Assert.Equal(["0	true"], lines);
    }
}
