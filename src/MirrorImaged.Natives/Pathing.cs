using MirrorImaged.Data;

namespace MirrorImaged.Natives;

public partial class unit
{
    internal readonly List<(float X, float Y)> Path = [];  // the waypoints a walker takes round what blocks it
    internal (float X, float Y)? NoWayTo;                   // a place a search found no way toward, and until when
    internal double NoWayUntil, ReplanAt;
    internal (float X, float Y)? PathGoal;                  // where that path leads
    internal double WaitingSince = -1;                      // when another unit stopped its walk, -1 while it walks
    internal (float X, float Y)? SpotFor;                   // the place another unit stood on, for which it makes for the nearest clear one
}

// Finding a way round what blocks a walker: the unwalkable cells of the map and the buildings standing in the way, on the 32 grid of the
// pathing file, by A* over eight neighbours. A unit walks straight until a step is blocked, then follows the path to the place it wants (or
// as near it as the search gets), and finds it again if the way is blocked once more. Close: buildings block as circles of their collision
// size and trees not at all, units don't block each other, and the game's own search is not copied (decisions D359).
public sealed partial class GameNatives
{
    const float SearchCell = 32;
    static readonly int UnitPathTexture = Code("upat");
    const int PathNodes = 40000;
    // The search leans toward the goal, finding a way sooner at the cost of a way a little longer than the shortest
    const float HeuristicWeight = 2f;
    bool _lastReached;

    // Whether a building (not one the unit is working on or standing inside) is in the way at the place. The buildings are listed again once in a
    // tick, in a grid of squares of 256, as the search for a path asks about thousands of places
    readonly Dictionary<(int, int), List<(unit Building, float X, float Y, float Reach, Footprint? Footprint)>> _buildingGrid = [];
    long _buildingGridAt = -1;
    int _buildingGridOf;

    bool BlockedByBuilding(unit u, float x, float y)
    {
        if (_buildingGridAt != _ticks || _buildingGridOf != _units.Count)
        {
            (_buildingGridAt, _buildingGridOf) = (_ticks, _units.Count);
            _buildingGrid.Clear();
            foreach (var b in _units.Values.Where(b => !b.Dead && !b.Removed && IsBuildingNow(b)))
            {
                var footprint = b.Type.Text(UnitPathTexture) is { Length: > 4 } texture ? Data.Footprint(texture) : null;
                var reach = footprint is null ? b.Type.Real(Collision) + 8 : 16 * MathF.Max(footprint.Across, footprint.Up) + 64;
                for (var cx = (int)MathF.Floor((b.X - reach) / 256); cx <= (int)MathF.Floor((b.X + reach) / 256); cx++)
                    for (var cy = (int)MathF.Floor((b.Y - reach) / 256); cy <= (int)MathF.Floor((b.Y + reach) / 256); cy++)
                    {
                        if (!_buildingGrid.TryGetValue((cx, cy), out var list)) _buildingGrid[(cx, cy)] = list = [];
                        list.Add((b, b.X, b.Y, reach, footprint));
                    }
            }
        }
        if (!_buildingGrid.TryGetValue(((int)MathF.Floor(x / 256), (int)MathF.Floor(y / 256)), out var near)) return false;
        var (below, above) = Extent(u);
        foreach (var (b, bx, by, reach, footprint) in near)
        {
            if (b == u || b.Dead || b.Removed) continue;
            if (b == u.OrderTarget || b == u.AttackTarget || b == u.Building || b == u.HarvestTarget) continue;
            // A worker bringing its load walks up to its own town hall
            if (u.Order == OrderReturn && b.Owner == u.Owner && IsTownHall(b)) continue;
            if (footprint is not null)
            {
                if (!OnFootprint(footprint, bx, by, below, above, x, y) || OnFootprint(footprint, bx, by, below, above, u.X, u.Y)) continue;
                return true;
            }
            if (Distance2(bx - x, by - y) > reach * reach) continue;
            // A unit already inside may walk out
            if (Distance2(bx - u.X, by - u.Y) <= reach * reach) continue;
            return true;
        }
        return false;
    }

    // A building made stands on the build grid: its footprint's corner on the grid of 64, the place moved toward 0 (probe units-path-stamp: a
    // barracks made at 40, 40 stands at 0, 0, at -50, 70 at 0, 64; an altar, 10 cells across, at 0, 0 at 32, 32)
    float OnBuildGrid(GameObject type, float at, bool x)
    {
        if (type.Integer(IsBuilding) == 0 || type.Text(UnitPathTexture) is not { Length: > 4 } texture || Data.Footprint(texture) is not { } footprint) return at;
        var odd = (x ? footprint.Across : footprint.Up) / 2 % 2 == 1 ? 32 : 0;
        return MathF.Truncate((at - odd) / 64) * 64 + odd;
    }

    // A ground unit's square against footprints, below and above its place on each axis: 32 a cell, a cell for each 16 of its collision (at
    // least one), and an even number of cells set 16 up and right (probe units-path-stamp-2: a peasant and a footman 16 each way, a knight 16
    // below and 48 above, an abomination 48 and 48)
    static (float Below, float Above) Extent(unit u)
    {
        var cells = Cells(u);
        var shift = cells % 2 == 0 ? 16 : 0;
        return (16 * cells - shift, 16 * cells + shift);
    }

    // Whether a unit at the place stands on the building's footprint: its square meets an unwalkable cell of the pathing texture, a square of
    // 32 a cell set 16 up and to the right of the building's place (probe units-path-stamp)
    static bool OnFootprint(Footprint footprint, float bx, float by, float below, float above, float x, float y)
    {
        var (left, bottom) = (bx - footprint.Across * 16 + 16, by - footprint.Up * 16 + 16);
        int c0 = (int)MathF.Floor((x - below - left) / 32), c1 = (int)MathF.Floor((x + above - left) / 32);
        int r0 = (int)MathF.Floor((y - below - bottom) / 32), r1 = (int)MathF.Floor((y + above - bottom) / 32);
        for (var c = c0; c <= c1; c++)
            for (var r = r0; r <= r1; r++)
                if (footprint.Unwalkable(c, r)) return true;
        return false;
    }

    // One step toward the place: straight, or along the path round what blocks the way; true while the unit goes on
    bool Go(unit u, float x, float y, float step)
    {
        if (u.PathGoal != (x, y))
        {
            u.Path.Clear();
            u.PathGoal = (x, y);
            u.SpotFor = null;
            // The game's unit sets out round what stands between it and the place, not when it reaches it (probe units-path-footprint)
            // (not toward a place that is itself blocked, as one past the world's edge: the walk goes straight on until it is stopped)
                        if (Seconds >= u.ReplanAt && !Blocked(u, x, y) && LineBlocked(u, u.X, u.Y, x, y))
            {
                u.ReplanAt = Seconds + 0.5;
                var ahead = FindPath(u, x, y);
                if (!_lastReached) u.ReplanAt = Seconds + 2;
                if (ahead is { Count: > 0 }) u.Path.AddRange(ahead);
            }
        }
        var (tx, ty) = u.Path.Count > 0 ? u.Path[0] : (x, y);
        var (dx, dy) = (tx - u.X, ty - u.Y);
        var left = MathF.Sqrt(dx * dx + dy * dy);
        if (left <= step && u.Path.Count > 0)
        {
            u.Path.RemoveAt(0);
            return true;
        }
        var (nx, ny) = left <= step ? (tx, ty) : (u.X + dx / left * step, u.Y + dy / left * step);
        if (!Blocked(u, nx, ny))
        {
            if (UnitInTheWay(u, nx, ny) is { } other) return GoRound(u, other, dx / left, dy / left, step, x, y);
            u.WaitingSince = -1;
            if (left > 0) u.Facing.Go(MathF.Atan2(dy, dx), Seconds, 0);
            Move(u, nx, ny);
            return true;
        }
        // On a found way, a step that clips what the way goes round slides along it: a step along one axis, as far as the waypoint
        if (u.Path.Count > 0)
            foreach (var (sx, sy) in new[] { (u.X + Math.Clamp(tx - u.X, -step, step), u.Y), (u.X, u.Y + Math.Clamp(ty - u.Y, -step, step)) })
                if ((sx != u.X || sy != u.Y) && !Blocked(u, sx, sy))
                {
                    Move(u, sx, sy);
                    return true;
                }
        if (u.NoWayTo == (x, y) && Seconds < u.NoWayUntil) return Finish(u);
        // Blocked again soon after a search: wait a moment before the next one, as a search costs thousands of looks at the ground
        if (Seconds < u.ReplanAt) return true;
        u.ReplanAt = Seconds + 0.5;
        var way = FindPath(u, x, y);
        // A way that stops short of the place leaves the unit to try again later than one that gets there
        if (!_lastReached) u.ReplanAt = Seconds + 2;
        if (way is null || way.Count == 0)
        {
            (u.NoWayTo, u.NoWayUntil) = ((x, y), Seconds + 2);
            return Finish(u);
        }
        u.Path.Clear();
        u.Path.AddRange(way);
        return true;
    }

    // Units keep room from each other (probes units-path-gap, units-path-room): a ground unit standing in the world, not a building,
    // and not a worker on its gathering round, which the game's pass through each other at the mine
    bool Collides(unit u) => !u.Dead && !u.Removed && !u.Hidden && !u.PathingOff && u.Transport is null && !u.Locust && u.Order is not (OrderHarvest or OrderReturn);

    const float UnitSquare = 256;
    readonly Dictionary<(int, int), List<unit>> _unitGrid = [];
    long _unitGridAt = -1;

    // The unit whose room the step would enter, stepping toward it (a unit may always step aside or away from one it overlaps); null for none.
    // The grid is listed once a tick and the places read as they are, as no two units keep more room than a square
    unit? UnitInTheWay(unit u, float x, float y)
    {
        if (!Collides(u) || Flies(u)) return null;
        if (_unitGridAt != _ticks)
        {
            _unitGridAt = _ticks;
            _unitGrid.Clear();
            foreach (var o in _units.Values)
                if (Collides(o) && !Flies(o) && !IsBuildingNow(o))
                {
                    var square = ((int)MathF.Floor(o.X / UnitSquare), (int)MathF.Floor(o.Y / UnitSquare));
                    if (!_unitGrid.TryGetValue(square, out var list)) _unitGrid[square] = list = [];
                    list.Add(o);
                }
        }
        // A walker held up a second passes units that walk, two seconds all of them: the game's group goes through a gap one by one
        var waited = u.WaitingSince < 0 ? 0 : Seconds - u.WaitingSince;
        if (waited > 2) return null;
        var (sx, sy) = ((int)MathF.Floor(x / UnitSquare), (int)MathF.Floor(y / UnitSquare));
        for (var cx = sx - 1; cx <= sx + 1; cx++)
            for (var cy = sy - 1; cy <= sy + 1; cy++)
                if (_unitGrid.TryGetValue((cx, cy), out var near))
                    foreach (var o in near)
                    {
                        if (o == u || o == u.OrderTarget || o == u.AttackTarget || !Collides(o) || waited > 1 && o.Order != 0) continue;
                        if (Apart(o, x, y) < Room(u, o) && (o.X - u.X) * (x - u.X) + (o.Y - u.Y) * (y - u.Y) > 0.01f) return o;
                    }
        return null;
    }

    // A unit in the way: the walker steps aside, turning as little as it can to the side away from it, and waits when it can't; held up
    // either way until a step toward the place is clear
    bool GoRound(unit u, unit other, float ux, float uy, float step, float x, float y)
    {
        // A unit standing on the way's next point, which the search doesn't see: the walker goes on to the point after it
        if (other.Order == 0 && u.Path.Count > 0 && Apart(other, u.Path[0].X, u.Path[0].Y) < Room(u, other))
        {
            u.Path.RemoveAt(0);
            return true;
        }
        // A unit standing on the place: the walker makes for the nearest place clear of it, and ends there
        if (other.Order == 0 && Apart(other, x, y) < Room(u, other))
        {
            if (u.SpotFor == (x, y)) return Finish(u);
            var (fx, fy) = Clear(u, x, y);
            if (Distance2(fx - u.X, fy - u.Y) <= step * step) return Finish(u);
            u.SpotFor = (x, y);
            u.Path.Clear();
            u.Path.Add((fx, fy));
            return true;
        }
        var away = (other.X - u.X) * uy - (other.Y - u.Y) * ux > 0 ? 1 : -1;
        foreach (var (c, sine) in SideTurns)
            foreach (var side in new[] { away, -away })
            {
                var s = sine * side;
                var (nx, ny) = (u.X + (ux * c - uy * s) * step, u.Y + (ux * s + uy * c) * step);
                if (Blocked(u, nx, ny) || UnitInTheWay(u, nx, ny) is not null) continue;
                u.Facing.Go(MathF.Atan2(ny - u.Y, nx - u.X), Seconds, 0);
                Move(u, nx, ny);
                if (u.WaitingSince < 0) u.WaitingSince = Seconds;
                return true;
            }
        if (u.WaitingSince < 0) u.WaitingSince = Seconds;
        return true;
    }

    // The room two ground units keep, across or up: 16 for each of their cells and 32 more (probe units-path-room: two footmen 64, two knights
    // 96, two abominations 128); a walker passes an idle unit as wide, give or take 12 (probe units-path-spacing)
    static float Room(unit u, unit o) => 16 * (Cells(u) + Cells(o)) + 32;

    static int Cells(unit u) => Math.Max(1, (int)(u.Type.Real(Collision) / 16));

    static float Apart(unit o, float x, float y) => MathF.Max(MathF.Abs(o.X - x), MathF.Abs(o.Y - y));

    // Where SetUnitPosition puts a ground unit: the place, or the nearest one round it, every 16 ring by ring, off footprints and unwalkable
    // ground and out of the room other units keep from it
    (float X, float Y) Clear(unit u, float x, float y)
    {
        if (!Collides(u) || Flies(u) || IsBuildingNow(u)) return (x, y);
        var others = _units.Values.Where(o => o != u && Collides(o) && !Flies(o) && !IsBuildingNow(o)).ToList();
        bool Free(float px, float py) => !Blocked(u, px, py) && !others.Any(o => Apart(o, px, py) < Room(u, o));
        for (var ring = 0; ring <= 32; ring++)
        {
            var round = new List<(int, int)>();
            for (var dx = -ring; dx <= ring; dx++)
                for (var dy = -ring; dy <= ring; dy++)
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) == ring) round.Add((dx, dy));
            foreach (var (dx, dy) in round.OrderBy(d => d.Item1 * d.Item1 + d.Item2 * d.Item2).ThenBy(d => d.Item2).ThenBy(d => d.Item1))
                if (Free(x + dx * 16, y + dy * 16)) return (x + dx * 16, y + dy * 16);
        }
        return (x, y);
    }

    // 30, 60 and 90 degrees, as numbers so that every platform turns alike
    static readonly (float Cos, float Sin)[] SideTurns = [(0.8660254f, 0.5f), (0.5f, 0.8660254f), (0f, 1f)];

    // Whether something stops the unit on the straight line between two places, looked at every 8
    bool LineBlocked(unit u, float x0, float y0, float x1, float y1)
    {
        var moveType = BlzGetUnitIntegerField(u, Convert<unitintegerfield>(MoveType));
        var (dx, dy) = (x1 - x0, y1 - y0);
        var n = (int)MathF.Ceiling(MathF.Sqrt(dx * dx + dy * dy) / 8);
        for (var k = 1; k <= n; k++)
            if (Blocked(u, moveType, x0 + dx * k / n, y0 + dy * k / n)) return true;
        return false;
    }

    // A* from the unit to the place over the 32 cells: the cell centres to walk through, ending at the place itself when it can be reached, or
    // at the cell nearest it that can; null when there is nowhere to go
    List<(float, float)>? FindPath(unit u, float goalX, float goalY)
    {
        if (Data.Terrain is not { } terrain) return null;
        var (minX, minY, maxX, maxY) = terrain.Bounds;
        int Column(float x) => (int)MathF.Floor((x - minX) / SearchCell);
        int Row(float y) => (int)MathF.Floor((y - minY) / SearchCell);
        var (columns, rows) = (Column(maxX) + 1, Row(maxY) + 1);
        float CenterX(int c) => minX + (c + 0.5f) * SearchCell;
        float CenterY(int r) => minY + (r + 0.5f) * SearchCell;
        var moveType = BlzGetUnitIntegerField(u, Convert<unitintegerfield>(MoveType));
        var freeCells = new Dictionary<(int, int), bool>();
        bool Free(int c, int r)
        {
            if (c < 0 || r < 0 || c >= columns || r >= rows) return false;
            if (!freeCells.TryGetValue((c, r), out var free)) freeCells[(c, r)] = free = !Blocked(u, moveType, CenterX(c), CenterY(r));
            return free;
        }
        var start = (c: Math.Clamp(Column(u.X), 0, columns - 1), r: Math.Clamp(Row(u.Y), 0, rows - 1));
        var goal = (c: Math.Clamp(Column(goalX), 0, columns - 1), r: Math.Clamp(Row(goalY), 0, rows - 1));
        float H((int c, int r) cell) => MathF.Sqrt(Distance2(cell.c - goal.c, cell.r - goal.r));
        var open = new PriorityQueue<(int c, int r), float>();
        var cost = new Dictionary<(int, int), float> { [start] = 0 };
        var from = new Dictionary<(int, int), (int, int)>();
        open.Enqueue(start, H(start));
        var best = start;
        var bestH = H(start);
        var expanded = 0;
        while (open.TryDequeue(out var here, out _) && expanded++ < PathNodes)
        {
            if (here == goal) { best = here; break; }
            if (H(here) < bestH) (best, bestH) = (here, H(here));
            for (var dr = -1; dr <= 1; dr++)
                for (var dc = -1; dc <= 1; dc++)
                {
                    if (dc == 0 && dr == 0) continue;
                    var next = (c: here.c + dc, r: here.r + dr);
                    if (!Free(next.c, next.r) || (dc != 0 && dr != 0 && (!Free(here.c + dc, here.r) || !Free(here.c, here.r + dr)))) continue;
                    var through = cost[here] + (dc != 0 && dr != 0 ? 1.4142f : 1f);
                    if (cost.TryGetValue(next, out var known) && known <= through) continue;
                    cost[next] = through;
                    from[next] = here;
                    open.Enqueue(next, through + H(next) * HeuristicWeight);
                }
        }
        _lastReached = best == goal;
        if (best == start) return null;
        var cells = new List<(int, int)> { best };
        while (from.TryGetValue(cells[^1], out var before) && before != start) cells.Add(before);
        cells.Reverse();
        var way = cells.Select(cell => (CenterX(cell.Item1), CenterY(cell.Item2))).ToList();
        // Drop the cells on a straight line, and end at the place when the last cell is the one it is in
        for (var i = way.Count - 2; i > 0; i--)
        {
            var (a, b, c) = (way[i - 1], way[i], way[i + 1]);
            if (MathF.Abs((b.Item1 - a.Item1) * (c.Item2 - b.Item2) - (b.Item2 - a.Item2) * (c.Item1 - b.Item1)) < 1e-3f) way.RemoveAt(i);
        }
        if (best == goal) way[^1] = (goalX, goalY);
        // Pull the way taut: from each place on to the farthest one in sight, so the walker cuts the corners the cells made
        var taut = new List<(float, float)>();
        var from0 = (u.X, u.Y);
        for (var i = 0; i < way.Count;)
        {
            var j = way.Count - 1;
            while (j > i && LineBlocked(u, from0.Item1, from0.Item2, way[j].Item1, way[j].Item2)) j--;
            taut.Add(way[j]);
            (from0, i) = (way[j], j + 1);
        }
        return taut;
    }
}
