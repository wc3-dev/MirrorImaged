namespace MirrorImaged.Natives;

public partial class unit
{
    internal readonly List<(float X, float Y)> Path = [];  // the waypoints a walker takes round what blocks it
    internal (float X, float Y)? PathGoal;                  // where that path leads
}

// Finding a way round what blocks a walker: the unwalkable cells of the map and the buildings standing in the way, on the 32 grid of the
// pathing file, by A* over eight neighbours. A unit walks straight until a step is blocked, then follows the path to the place it wants (or
// as near it as the search gets), and finds it again if the way is blocked once more. Close: buildings block as circles of their collision
// size and trees not at all, units don't block each other, and the game's own search is not copied (decisions D359).
public sealed partial class GameNatives
{
    const float SearchCell = 32;
    const int PathNodes = 6000;

    // Whether a building (not one the unit is working on or standing inside) is in the way at the place
    // The buildings, listed again once in a tick, as the search for a path asks about thousands of places
    List<unit> _buildingList = [];
    long _buildingListAt = -1;
    int _buildingListOf;

    bool BlockedByBuilding(unit u, float x, float y)
    {
        if (_buildingListAt != _ticks || _buildingListOf != _units.Count)
        {
            (_buildingListAt, _buildingListOf) = (_ticks, _units.Count);
            _buildingList = _units.Values.Where(b => !b.Dead && !b.Removed && IsBuildingNow(b)).ToList();
        }
        foreach (var b in _buildingList)
        {
            if (b == u || b.Dead || b.Removed) continue;
            if (b == u.OrderTarget || b == u.AttackTarget || b == u.Building || b == u.HarvestTarget) continue;
            var reach = b.Type.Real(Collision) + 8;
            if (Distance2(b.X - x, b.Y - y) > reach * reach) continue;
            // A unit already inside may walk out
            if (Distance2(b.X - u.X, b.Y - u.Y) <= reach * reach) continue;
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
        var way = FindPath(u, x, y);
        if (way is null || way.Count == 0) return Finish(u);
        u.Path.Clear();
        u.Path.AddRange(way);
        return true;
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
        var freeCells = new Dictionary<(int, int), bool>();
        bool Free(int c, int r)
        {
            if (c < 0 || r < 0 || c >= columns || r >= rows) return false;
            if (!freeCells.TryGetValue((c, r), out var free)) freeCells[(c, r)] = free = !Blocked(u, CenterX(c), CenterY(r));
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
                    open.Enqueue(next, through + H(next));
                }
        }
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
        return way;
    }
}
