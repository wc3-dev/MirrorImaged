using MirrorImaged.Data;

namespace MirrorImaged.Natives;

public partial class unit
{
    internal readonly List<(float X, float Y)> Path = [];  // the waypoints a walker takes round what blocks it
    internal (float X, float Y)? NoWayTo;                   // a place a search found no way toward, and until when
    internal double NoWayUntil, ReplanAt;
    internal (float X, float Y)? PathGoal;                  // where that path leads
    internal double WaitingSince = -1;                      // when another unit stopped its walk, -1 while it walks
    internal int Side;                                      // the side it steps aside to while held up, 0 while it walks
    internal (float X, float Y)? SpotFor;                   // the place another unit stood on, for which it makes for the nearest clear one
    internal unit? Gate;                                    // the Way Gate its path goes through, to ground the place is on
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
    // A search toward a place the walker's own ground reaches looks much further (D822): Korea's way round a long cliff needs it
    const int LandPathNodes = 400000;
    // The search leans toward the goal, finding a way sooner at the cost of a way a little longer than the shortest
    const float HeuristicWeight = 2f;
    bool _lastReached;
    // The search's cells, kept from one search to the next and told apart by the search's number
    float[] _pathCost = [];
    int[] _pathFrom = [], _pathSeen = [], _freeSeen = [];
    bool[] _pathFree = [];
    int _pathSearch;

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
        if (u.Gate is { } gate && AtGate(u, gate))
        {
            ThroughGate(u, gate);
            return true;
        }
        if (u.PathGoal != (x, y))
        {
            u.Path.Clear();
            u.PathGoal = (x, y);
            u.SpotFor = null;
            u.Gate = null;
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
            if (u.WaitingSince < 0 || UnitInTheWay(u, nx, ny, strict: true) is null) (u.WaitingSince, u.Side) = (-1, 0);
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
    unit? UnitInTheWay(unit u, float x, float y, bool strict = false)
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
        var waited = u.WaitingSince < 0 || strict ? 0 : Seconds - u.WaitingSince;
        if (waited > 2) return null;
        var (sx, sy) = ((int)MathF.Floor(x / UnitSquare), (int)MathF.Floor(y / UnitSquare));
        for (var cx = sx - 1; cx <= sx + 1; cx++)
            for (var cy = sy - 1; cy <= sy + 1; cy++)
                if (_unitGrid.TryGetValue((cx, cy), out var near))
                    foreach (var o in near)
                    {
                        if (o == u || o == u.OrderTarget || o == u.AttackTarget || !Collides(o) || waited > 1 && !Standing(o)) continue;
                        if (Apart(o, x, y) < Room(u, o) && (o.X - u.X) * (x - u.X) + (o.Y - u.Y) * (y - u.Y) > 0.01f) return o;
                    }
        return null;
    }

    // A unit in the way: the walker steps aside, turning as little as it can to the side away from it, and waits when it can't; held up
    // either way until a step toward the place is clear
    bool GoRound(unit u, unit other, float ux, float uy, float step, float x, float y)
    {
        // A unit standing on the way's next point, which the search doesn't see: the walker goes on to the point after it
        if (Standing(other) && u.Path.Count > 0 && Apart(other, u.Path[0].X, u.Path[0].Y) < Room(u, other))
        {
            u.Path.RemoveAt(0);
            return true;
        }
        // A unit standing on the place: the walker makes for the nearest place clear of it, and ends there
        if (Standing(other) && Apart(other, x, y) < Room(u, other))
        {
            if (u.SpotFor == (x, y)) return Finish(u);
            var (fx, fy) = Clear(u, x, y);
            if (Distance2(fx - u.X, fy - u.Y) <= step * step) return Finish(u);
            u.SpotFor = (x, y);
            u.Path.Clear();
            u.Path.Add((fx, fy));
            return true;
        }
        // The side away from the first unit in the way, kept while held up, so that a walker goes along a row of them
        if (u.Side == 0) u.Side = (other.X - u.X) * uy - (other.Y - u.Y) * ux > 0 ? 1 : -1;
        var away = u.Side;
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

    // A unit that stays where it is: idle or holding its place
    static bool Standing(unit o) => o.Order is 0 or 851993;

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
        if (_pathSeen.Length < columns * rows) (_pathCost, _pathFrom, _pathSeen, _freeSeen, _pathFree) = (new float[columns * rows], new int[columns * rows], new int[columns * rows], new int[columns * rows], new bool[columns * rows]);
        var search = ++_pathSearch;
        bool Free(int c, int r)
        {
            if (c < 0 || r < 0 || c >= columns || r >= rows) return false;
            var at = r * columns + c;
            if (_freeSeen[at] != search) (_freeSeen[at], _pathFree[at]) = (search, !Blocked(u, moveType, CenterX(c), CenterY(r)));
            return _pathFree[at];
        }
        var start = (c: Math.Clamp(Column(u.X), 0, columns - 1), r: Math.Clamp(Row(u.Y), 0, rows - 1));
        var goal = (c: Math.Clamp(Column(goalX), 0, columns - 1), r: Math.Clamp(Row(goalY), 0, rows - 1));
        // A walker sent to a place its own ground doesn't reach goes as near it as that ground gets: the search aims for that ground's
        // nearest cell, not the place, which would read every cell the search allows before it gave up (D870)
        // A Way Gate that leads, gate by gate, to the place's ground is the way there: the walker makes for the gate (D873)
        var asked = true;
        if (moveType is 1 or 4) u.Gate = null;
        if (moveType is 1 or 4 && LandLabels(out var landColumns) is { } lands && landColumns == columns
            && WalkerLand(u.X, u.Y) is not 0 and var land && lands[goal.r * columns + goal.c] != land)
        {
            if (GateToward(land, WalkerLand(goalX, goalY)) is { } gate)
            {
                u.Gate = gate;
                (goal, goalX, goalY, asked) = ((Math.Clamp(Column(gate.X), 0, columns - 1), Math.Clamp(Row(gate.Y), 0, rows - 1)), gate.X, gate.Y, false);
            }
            else if (NearestOn(lands, columns, land, goal.c, goal.r) is { } nearest)
                (goal, goalX, goalY, asked) = (nearest, CenterX(nearest.c), CenterY(nearest.r), false);
        }
        float H((int c, int r) cell) => MathF.Sqrt(Distance2(cell.c - goal.c, cell.r - goal.r));
        var open = new PriorityQueue<(int c, int r), float>();
        int At((int c, int r) cell) => cell.r * columns + cell.c;
        (_pathSeen[At(start)], _pathCost[At(start)], _pathFrom[At(start)]) = (search, 0, -1);
        open.Enqueue(start, H(start));
        var best = start;
        var bestH = H(start);
        var expanded = 0;
        var limit = moveType is 1 or 4 && Free(goal.c, goal.r) && SameLand(u.X, u.Y, goalX, goalY) ? LandPathNodes : PathNodes;
        while (open.TryDequeue(out var here, out _) && expanded++ < limit)
        {
            if (here == goal) { best = here; break; }
            if (H(here) < bestH) (best, bestH) = (here, H(here));
            for (var dr = -1; dr <= 1; dr++)
                for (var dc = -1; dc <= 1; dc++)
                {
                    if (dc == 0 && dr == 0) continue;
                    var next = (c: here.c + dc, r: here.r + dr);
                    if (!Free(next.c, next.r) || (dc != 0 && dr != 0 && (!Free(here.c + dc, here.r) || !Free(here.c, here.r + dr)))) continue;
                    var through = _pathCost[At(here)] + (dc != 0 && dr != 0 ? 1.4142f : 1f);
                    var n = At(next);
                    if (_pathSeen[n] == search && _pathCost[n] <= through) continue;
                    (_pathSeen[n], _pathCost[n], _pathFrom[n]) = (search, through, At(here));
                    open.Enqueue(next, through + H(next) * HeuristicWeight);
                }
        }
        _lastReached = best == goal && asked;
        if (best == start) return null;
        var cells = new List<(int, int)> { best };
        for (var before = _pathFrom[At(cells[^1])]; before >= 0 && before != At(start); before = _pathFrom[before]) cells.Add((before % columns, before / columns));
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

    // Each cell of the pathing grid's piece of walkable ground, joined by eight neighbours as LandOf joins them, 0 where it is unwalkable;
    // found once, as the ground seldom changes
    int[]? _landLabels;

    // The first Way Gate on the ground of the first number on a way of active gates to the ground of the second, the fewest gates first and the
    // gates in the order they were made; null for none (D873)
    unit? GateToward(int from, int to)
    {
        if (to == 0 || to == from) return null;
        var gates = _units.Values.Where(g => g.GateActive && !g.Dead && !g.Removed && (g.GateX != 0 || g.GateY != 0)).OrderBy(g => g.Id)
            .Select(g => (Gate: g, From: WalkerLand(g.X, g.Y), To: WalkerLand(g.GateX, g.GateY))).Where(e => e.From != 0 && e.To != 0 && e.From != e.To).ToList();
        var first = new Dictionary<int, unit?> { [from] = null };
        var queue = new Queue<int>();
        queue.Enqueue(from);
        while (queue.TryDequeue(out var here))
            foreach (var (gate, on, leads) in gates)
            {
                if (on != here || first.ContainsKey(leads)) continue;
                first[leads] = first[here] ?? gate;
                if (leads == to) return first[leads];
                queue.Enqueue(leads);
            }
        return null;
    }

    // A walker at its Way Gate: within reach of the gate's edge, as it would be when it walks into the gate
    static bool AtGate(unit u, unit gate) =>
        !gate.Dead && !gate.Removed && gate.GateActive && Distance2(u.X - gate.X, u.Y - gate.Y) is var near
        && near <= (gate.Type.Real(Collision) + u.Type.Real(Collision) + 64) * (gate.Type.Real(Collision) + u.Type.Real(Collision) + 64);

    // The walker goes through the gate to its destination, the nearest clear place there, and finds its way on from there
    void ThroughGate(unit u, unit gate)
    {
        var (x, y) = Clear(u, gate.GateX, gate.GateY);
        Move(u, x, y);
        u.Gate = null;
        u.Path.Clear();
        (u.PathGoal, u.ReplanAt, u.NoWayTo) = (null, 0, null);
    }

    // The pieces of ground, labelled once, with the grid's columns (null on a map with no ground)
    int[]? LandLabels(out int columns)
    {
        columns = 0;
        if (Data.Terrain is not { } terrain) return null;
        var (minX, minY, maxX, maxY) = terrain.Bounds;
        int cols = (int)((maxX - minX) / SearchCell) + 1, rows = (int)((maxY - minY) / SearchCell) + 1;
        columns = cols;
        if (_landLabels is not null) return _landLabels;
        var walk = Convert<pathingtype>(1);
        bool Open(int c, int r) => c >= 0 && r >= 0 && c < cols && r < rows && !IsTerrainPathable(minX + (c + 0.5f) * SearchCell, minY + (r + 0.5f) * SearchCell, walk);
        var labels = new int[cols * rows];
        var next = 0;
        var queue = new Queue<(int C, int R)>();
        for (var r0 = 0; r0 < rows; r0++)
            for (var c0 = 0; c0 < cols; c0++)
            {
                if (labels[r0 * cols + c0] != 0 || !Open(c0, r0)) continue;
                labels[r0 * cols + c0] = ++next;
                queue.Enqueue((c0, r0));
                while (queue.TryDequeue(out var here))
                    for (var dr = -1; dr <= 1; dr++)
                        for (var dc = -1; dc <= 1; dc++)
                        {
                            var (c, r) = (here.C + dc, here.R + dr);
                            if (!Open(c, r) || labels[r * cols + c] != 0) continue;
                            if (dc != 0 && dr != 0 && (!Open(here.C + dc, here.R) || !Open(here.C, here.R + dr))) continue;
                            labels[r * cols + c] = next;
                            queue.Enqueue((c, r));
                        }
            }
        return _landLabels = labels;
    }

    // The piece of ground the place is on, 0 for unwalkable ground or none
    int LandAt(float x, float y)
    {
        if (LandLabels(out var columns) is not { } labels) return 0;
        var (minX, minY, _, _) = Data.Terrain!.Bounds;
        int c = (int)((x - minX) / SearchCell), r = (int)((y - minY) / SearchCell);
        return c >= 0 && r >= 0 && c < columns && r * columns + c < labels.Length ? labels[r * columns + c] : 0;
    }

    // The piece of ground a walker at the place stands on: its cell's, or, on an unwalkable cell (a walker pushed against a cliff), that of the
    // nearest walkable cell within 6; 0 for none
    int WalkerLand(float x, float y)
    {
        var land = LandAt(x, y);
        if (land != 0 || LandLabels(out var columns) is not { } labels) return land;
        var (minX, minY, _, _) = Data.Terrain!.Bounds;
        int c0 = (int)((x - minX) / SearchCell), r0 = (int)((y - minY) / SearchCell), rows = labels.Length / columns;
        var (found, foundAt) = (0, int.MaxValue);
        for (var dr = -6; dr <= 6; dr++)
            for (var dc = -6; dc <= 6; dc++)
                if (c0 + dc >= 0 && r0 + dr >= 0 && c0 + dc < columns && r0 + dr < rows && labels[(r0 + dr) * columns + c0 + dc] is not 0 and var near
                    && dc * dc + dr * dr < foundAt)
                    (found, foundAt) = (near, dc * dc + dr * dr);
        return found;
    }

    // Whether walkable ground joins the walker's place to the other's own cell (D870)
    bool SameLand(float x0, float y0, float x1, float y1) => WalkerLand(x0, y0) is not 0 and var land && LandAt(x1, y1) == land;

    // The cell of the piece of ground nearest the cell, by the squares of the distances, the first found in ring order on a tie; null when
    // the ground has no cell (D870)
    static (int c, int r)? NearestOn(int[] labels, int columns, int land, int gc, int gr)
    {
        var rows = labels.Length / columns;
        (int c, int r)? best = null;
        var bestAt = long.MaxValue;
        bool Look(int c, int r)
        {
            if (c < 0 || r < 0 || c >= columns || r >= rows || labels[r * columns + c] != land) return false;
            var at = (long)(c - gc) * (c - gc) + (long)(r - gr) * (r - gr);
            if (at < bestAt) (best, bestAt) = ((c, r), at);
            return true;
        }
        for (var ring = 0; ring <= Math.Max(columns, rows) && (long)ring * ring <= bestAt; ring++)
        {
            for (var c = gc - ring; c <= gc + ring; c++)
            {
                Look(c, gr - ring);
                if (ring > 0) Look(c, gr + ring);
            }
            for (var r = gr - ring + 1; r <= gr + ring - 1; r++)
            {
                Look(gc - ring, r);
                Look(gc + ring, r);
            }
        }
        return best;
    }
}
