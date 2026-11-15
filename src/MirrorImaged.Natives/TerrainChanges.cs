using MirrorImaged.World;

namespace MirrorImaged.Natives;

// A lasting crater: where it is, how deep and over how long it grows, and when it was stopped
sealed class Crater(float x, float y, float radius, float depth, long created, double start, double seconds)
{
    public readonly float X = x, Y = y, Radius = radius, Depth = depth;
    public readonly long Created = created;
    public readonly double Start = start, Seconds = seconds;
    public long? Stopped;
    public double StoppedAt, Fade;
}

public partial class terraindeformation
{
    internal object? Crater;
}

// The ground as the map's code changes it: tiles painted with SetTerrainType, the 32-cell pathing grid and its blight, and
// the craters that move the 128 corners (game-facts, "Terrain changes", probes of 11-09, and the pathing and blight
// facts of 10-25 to 11-11). A load keeps pathing and blight and loses painted tiles, which a test run never meets.
public sealed partial class GameNatives
{
    const string GroundFacts = "game-facts.md, Terrain changes: probe of 11-09; Units: IsTerrainPathable (10-25, 10-26)";
    const float PathCell = 32, Blighting = 64, BlightReach = 0.8f, PointBlight = 200;
    const byte BlightBit = 32;
    static readonly byte[] PathingBits = [2, 4, 8, 16, 32, 64, 128]; // PATHING_TYPE_* in order: walk, fly, build, peon harvest, blight, float, amphibious

    readonly Dictionary<int, (int Type, int Variation)> _painted = [];
    readonly List<Crater> _craters = [];
    byte[]? _cells;

    // The pathing grid from the map's war3map.wpm, a cell a byte; none where the map has no such file or terrain
    byte[]? Cells()
    {
        if (_cells is not null || Data.Map?.Files.PathingMap is not { } pathing) return _cells;
        _cells = [.. pathing.Cells.Select(cell => (byte)cell)];
        return _cells;
    }

    // The cell the point falls in, or -1 outside the grid
    int CellAt(float x, float y)
    {
        if (Data.Terrain is not { } terrain || Data.Map?.Files.PathingMap is not { } pathing) return -1;
        var (_, _, left, bottom) = terrain.Grid;
        int column = (int)MathF.Floor((x - left) / PathCell), row = (int)MathF.Floor((y - bottom) / PathCell);
        return column < 0 || row < 0 || column >= pathing.Width || row >= pathing.Height ? -1 : (int)(row * pathing.Width + column);
    }

    /// <summary>
    /// True when the cell has the kind of pathing blocked (true means not pathable; facts: walkability false at a start
    /// location, true on water), read from the map's pathing file and what SetTerrainPathable and the blight natives
    /// changed, in the same instant; a point off the grid reads blocked. Close: a map with no pathing file reads open,
    /// where the game works its own out from the ground.
    /// </summary>
    [Native(Level.Close, Fact = GroundFacts)]
    public bool IsTerrainPathable(float x, float y, pathingtype? t)
    {
        if (t is null) return false;
        var cell = CellAt(x, y);
        if (cell < 0) return Data.Terrain is not null;
        return Cells() is { } cells && t.Id >= 0 && t.Id < PathingBits.Length && (cells[cell] & PathingBits[t.Id]) != 0;
    }

    /// <summary>One 32 by 32 cell on the world's grid takes the flag: true makes it pathable for the kind, false blocks it.</summary>
    [Native(Level.Exact, Fact = GroundFacts)]
    public void SetTerrainPathable(float x, float y, pathingtype? t, bool flag)
    {
        var cell = CellAt(x, y);
        if (t is null || cell < 0 || Cells() is not { } cells || t.Id < 0 || t.Id >= PathingBits.Length) return;
        cells[cell] = flag ? (byte)(cells[cell] & ~PathingBits[t.Id]) : (byte)(cells[cell] | PathingBits[t.Id]);
    }

    /// <summary>Whether the cell is blighted, as IsTerrainPathable reads blight pathing.</summary>
    [Native(Level.Close, Fact = GroundFacts)]
    public bool IsPointBlighted(float x, float y) => CellAt(x, y) is var cell and >= 0 && Cells() is { } cells && (cells[cell] & BlightBit) != 0;

    // The 64 by 64 cells (each two pathing cells across and up) whose centre lies within the reach of a place, or inside a
    // rect, taking blight or losing it: blight touches nothing else (facts: radius 300 reached 240 along a line, taking off
    // with 150 cleared to 112)
    void Blight(float x, float y, float reach, bool add)
    {
        var half = Blighting / 2;
        for (var cy = MathF.Floor((y - reach) / Blighting) * Blighting; cy <= y + reach; cy += Blighting)
            for (var cx = MathF.Floor((x - reach) / Blighting) * Blighting; cx <= x + reach; cx += Blighting)
                if (Distance2(cx + half - x, cy + half - y) <= reach * reach)
                    Blighted(cx, cy, add);
    }

    void Blighted(float cx, float cy, bool add)
    {
        if (Cells() is not { } cells) return;
        foreach (var (dx, dy) in new[] { (0f, 0f), (PathCell, 0f), (0f, PathCell), (PathCell, PathCell) })
            if (CellAt(cx + dx + 1, cy + dy + 1) is var cell and >= 0)
                cells[cell] = add ? (byte)(cells[cell] | BlightBit) : (byte)(cells[cell] & ~BlightBit);
    }

    /// <summary>Blights (or clears) the 64 cells whose centres are within 0.8 of the radius; the player changes nothing here.</summary>
    [Native(Level.Close, Fact = GroundFacts)]
    public void SetBlight(player? whichPlayer, float x, float y, float radius, bool addBlight)
    {
        if (whichPlayer is not null) Blight(x, y, radius * BlightReach, addBlight);
    }

    [Native(Level.Close, Fact = GroundFacts)]
    public void SetBlightLoc(player? whichPlayer, location? whichLocation, float radius, bool addBlight)
    {
        if (whichLocation is not null) SetBlight(whichPlayer, whichLocation.X, whichLocation.Y, radius, addBlight);
    }

    /// <summary>One point's blight, of the size of a radius of 200 (the game's reached about 160 along a line; Close).</summary>
    [Native(Level.Close, Fact = GroundFacts)]
    public void SetBlightPoint(player? whichPlayer, float x, float y, bool addBlight) => SetBlight(whichPlayer, x, y, PointBlight, addBlight);

    /// <summary>Every 64 cell whose centre is inside the rect.</summary>
    [Native(Level.Close, Fact = GroundFacts)]
    public void SetBlightRect(player? whichPlayer, rect? r, bool addBlight)
    {
        if (whichPlayer is null || r is null) return;
        for (var cy = MathF.Floor(r.MinY / Blighting) * Blighting; cy <= r.MaxY; cy += Blighting)
            for (var cx = MathF.Floor(r.MinX / Blighting) * Blighting; cx <= r.MaxX; cx += Blighting)
                if (cx + Blighting / 2 >= r.MinX && cx + Blighting / 2 <= r.MaxX && cy + Blighting / 2 >= r.MinY && cy + Blighting / 2 <= r.MaxY)
                    Blighted(cx, cy, addBlight);
    }

    /// <summary>
    /// A Haunted Gold Mine ("ugol") for the player at the place, with the blight of a point around it; nil where the data
    /// has no such unit. Close: the size of its blight isn't measured.
    /// </summary>
    [Native(Level.Close, Fact = GroundFacts)]
    public unit? CreateBlightedGoldmine(player? id, float x, float y, float face)
    {
        var mine = CreateUnit(id, Code("ugol"), x, y, face);
        if (mine is not null) SetBlightPoint(id, x, y, true);
        return mine;
    }

    // The corner nearest the point, a half rounding up, or null off the map
    (int Column, int Row)? NearestCorner(float x, float y)
    {
        if (Data.Terrain is not { } terrain) return null;
        var (columns, rows, left, bottom) = terrain.Grid;
        int column = (int)MathF.Floor((x - left) / 128 + 0.5f), row = (int)MathF.Floor((y - bottom) / 128 + 0.5f);
        return column < 0 || row < 0 || column >= columns || row >= rows ? null : (column, row);
    }

    /// <summary>The tile id of the corner nearest the point (as painted), 0 off the map.</summary>
    [Native(Level.Close, Fact = GroundFacts)]
    public int GetTerrainType(float x, float y) =>
        NearestCorner(x, y) is not var (column, row) ? 0
        : _painted.TryGetValue(row * Data.Terrain!.Grid.Columns + column, out var painted) ? painted.Type : Data.Terrain.TileAt(column, row).Type;

    /// <summary>The variation of the corner nearest the point (as painted), 0 off the map.</summary>
    [Native(Level.Close, Fact = GroundFacts)]
    public int GetTerrainVariance(float x, float y) =>
        NearestCorner(x, y) is not var (column, row) ? 0
        : _painted.TryGetValue(row * Data.Terrain!.Grid.Columns + column, out var painted) ? painted.Variation : Data.Terrain.TileAt(column, row).Variation;

    /// <summary>
    /// Paints the corners within area - 1 corners of the nearest one, a circle for shape 0 and a square for shape 1,
    /// with the tile and variation (-1 keeps the variation 0 here); it changes no pathing and no blight (facts). Close:
    /// a random variation (-1) isn't drawn.
    /// </summary>
    [Native(Level.Close, Fact = GroundFacts)]
    public void SetTerrainType(float x, float y, int terrainType, int variation, int area, int shape)
    {
        if (NearestCorner(x, y) is not var (column, row) || Data.Terrain is not { } terrain || area < 1) return;
        var (columns, rows, _, _) = terrain.Grid;
        var reach = area - 1;
        for (var dy = -reach; dy <= reach; dy++)
            for (var dx = -reach; dx <= reach; dx++)
                if ((shape == 1 || dx * dx + dy * dy <= reach * reach) && column + dx >= 0 && row + dy >= 0 && column + dx < columns && row + dy < rows)
                    _painted[(row + dy) * columns + column + dx] = (terrainType, Math.Max(variation, 0));
    }

    /// <summary>
    /// A crater that moves the 128 corners within its radius by -depth x cos(90 degrees x distance / radius), from the next
    /// moment, over the duration (milliseconds, linearly; 0 at once) and lasting until stopped, adding up with the others
    /// (facts, Terrain changes). Close: the game's ramp over a duration starts slower than a straight line (2.5% at 0.1 s
    /// of 1 s).
    /// </summary>
    [Native(Level.Close, Fact = GroundFacts)]
    public terraindeformation? TerrainDeformCrater(float x, float y, float radius, float depth, int duration, bool permanent)
    {
        var handle = Make(new terraindeformation());
        var crater = new Crater(x, y, radius, depth, _game.Step, Seconds, Math.Max(duration, 0) / 1000.0);
        handle.Crater = crater;
        _craters.Add(crater);
        return handle;
    }

    /// <summary>The crater stops after the duration, fading over it; at once (0) it is gone from the next moment, still read in this one.</summary>
    [Native(Level.Close, Fact = GroundFacts + "; facts: a stopped crater still reads in the instant it is stopped (11-13)")]
    public void TerrainDeformStop(terraindeformation? deformation, int duration)
    {
        if (deformation?.Crater is not Crater { Stopped: null } crater) return;
        (crater.Stopped, crater.StoppedAt, crater.Fade) = (_game.Step, Seconds, Math.Max(duration, 0) / 1000.0);
    }

    [Native(Level.Close, Fact = GroundFacts)]
    public void TerrainDeformStopAll()
    {
        foreach (var crater in _craters.Where(c => c.Stopped is null)) (crater.Stopped, crater.StoppedAt, crater.Fade) = (_game.Step, Seconds, 0);
    }

    [Native(Level.Recorded, Reason = "Ripples the ground for a while, which GetLocationZ would show; only the crater is measured and modelled")]
    public terraindeformation? TerrainDeformRipple(float x, float y, float radius, float depth, int duration, int count, float spaceWaves, float timeWaves, float radiusStartPct, bool limitNeg) =>
        Make(new terraindeformation());

    [Native(Level.Recorded, Reason = "Shakes the ground at random, which GetLocationZ would show; only the crater is measured and modelled")]
    public terraindeformation? TerrainDeformRandom(float x, float y, float radius, float minDelta, float maxDelta, int duration, int updateInterval) =>
        Make(new terraindeformation());

    [Native(Level.Recorded, Reason = "Sends a wave over the ground, which GetLocationZ would show; only the crater is measured and modelled")]
    public terraindeformation? TerrainDeformWave(float x, float y, float dirX, float dirY, float distance, float speed, float radius, float depth, int trailTime, int count) =>
        Make(new terraindeformation());

    // How far the craters move a corner of the 128 grid now: each from the moment after it is made, grown over its
    // duration and faded after its stop; a stopped one is gone once the moment of its stop has passed
    float Shifted(int column, int row)
    {
        var (_, _, left, bottom) = Data.Terrain!.Grid;
        float x = left + column * 128, y = bottom + row * 128, shift = 0;
        foreach (var crater in _craters)
        {
            if (_game.Step <= crater.Created) continue;
            var distance = MathF.Sqrt(Distance2(x - crater.X, y - crater.Y));
            if (distance >= crater.Radius) continue;
            var grown = crater.Seconds <= 0 ? 1 : Math.Clamp((Seconds - crater.Start) / crater.Seconds, 0, 1);
            var remains = crater.Stopped is null ? 1 : crater.Fade <= 0 ? (_game.Step <= crater.Stopped ? 1 : 0) : Math.Clamp(1 - (Seconds - crater.StoppedAt) / crater.Fade, 0, 1);
            shift -= crater.Depth * MathF.Cos(MathF.PI / 2 * distance / crater.Radius) * (float)(grown * remains);
        }
        return shift;
    }

    // The ground's height here: the map's corners moved by the craters
    float Ground(float x, float y) =>
        Data.Terrain is not { } terrain ? 0 : _craters.Count == 0 ? terrain.Height(x, y) : terrain.Height(x, y, Shifted);
}
