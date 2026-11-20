namespace MirrorImaged.Natives;

public partial class unit
{
    internal readonly List<(float X, float Y)> Path = [];  // the waypoints a walker takes round what blocks it
    internal (float X, float Y)? NoWayTo;                   // a place a search found no way toward, and until when
    internal double NoWayUntil, ReplanAt;
    internal (float X, float Y)? PathGoal;                  // where that path leads
}

// Finding a way round what blocks a walker: the unwalkable cells of the map and the buildings standing in the way, on the 32 grid of the
// pathing file, by A* over eight neighbours. A unit walks straight until a step is blocked, then follows the path to the place it wants (or
// as near it as the search gets), and finds it again if the way is blocked once more. Close: buildings block as circles of their collision
// size and trees not at all, units don't block each other, and the game's own search is not copied (decisions D359).
public sealed partial class GameNatives
{
    const float SearchCell = 32;
    const int PathNodes = 40000;
    // The search leans toward the goal, finding a way sooner at the cost of a way a little longer than the shortest
    const float HeuristicWeight = 2f;
    bool _lastReached;

    // Whether a building (not one the unit is working on or standing inside) is in the way at the place. The buildings are listed again once in a
    // tick, in a grid of squares of 256, as the search for a path asks about thousands of places
    readonly Dictionary<(int, int), List<(unit Building, float X, float Y, float Reach)>> _buildingGrid = [];
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
                var reach = b.Type.Real(Collision) + 8;
                for (var cx = (int)MathF.Floor((b.X - reach) / 256); cx <= (int)MathF.Floor((b.X + reach) / 256); cx++)
                    for (var cy = (int)MathF.Floor((b.Y - reach) / 256); cy <= (int)MathF.Floor((b.Y + reach) / 256); cy++)
                    {
                        if (!_buildingGrid.TryGetValue((cx, cy), out var list)) _buildingGrid[(cx, cy)] = list = [];
                        list.Add((b, b.X, b.Y, reach));
                    }
            }
        }
        if (!_buildingGrid.TryGetValue(((int)MathF.Floor(x / 256), (int)MathF.Floor(y / 256)), out var near)) return false;
        foreach (var (b, bx, by, reach) in near)
        {
            if (b == u || b.Dead || b.Removed) continue;
            if (b == u.OrderTarget || b == u.AttackTarget || b == u.Building || b == u.HarvestTarget) continue;
            // A worker bringing its load walks up to its own town hall
            if (u.Order == OrderReturn && b.Owner == u.Owner && IsTownHall(b)) continue;
            if (Distance2(bx - x, by - y) > reach * reach) continue;
            // A unit already inside may walk out
            if (Distance2(bx - u.X, by - u.Y) <= reach * reach) continue;
            return true;
        }
        return false;
    }

    // One step toward the place: straight, or along the path round what blocks the way; true while the unit goes on
    bool Go(unit u, float x, float y, float step)
    {
        if (u.PathGoal != (x, y))
        {
            u.Path.Clear();
            u.PathGoal = (x, y);
            // The game's unit sets out round what stands between it and the place, not when it reaches it (probe units-path-footprint)
            if (Seconds >= u.ReplanAt && LineBlocked(u, u.X, u.Y, x, y))
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
            if (left > 0) u.Facing.Go(MathF.Atan2(dy, dx), Seconds, 0);
            Move(u, nx, ny);
            return true;
        }
        // On a found way, a step that clips what the way goes round slides along it: the part of the step along one axis
        if (u.Path.Count > 0)
            foreach (var (sx, sy) in new[] { (nx, u.Y), (u.X, ny) })
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
