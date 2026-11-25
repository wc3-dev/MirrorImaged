using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using MirrorImaged.Lua;

namespace MirrorImaged;

/// <summary>A run seen from above: the map, then what each unit was at each sampled time, each missile's flight, and what the log said when.</summary>
internal sealed class Trace
{
    /// <summary>The file name of the map.</summary>
    public string Map { get; set; } = "";
    /// <summary>The map's west, south, east and north edges; null when the map has no terrain.</summary>
    public float[]? Bounds { get; set; }
    /// <summary>The columns of the pathing grid.</summary>
    public int GridWidth { get; set; }
    /// <summary>The rows of the pathing grid.</summary>
    public int GridHeight { get; set; }
    /// <summary>The west edge of the pathing grid.</summary>
    public float GridLeft { get; set; }
    /// <summary>The south edge of the pathing grid.</summary>
    public float GridBottom { get; set; }
    /// <summary>The pathing grid, a byte a cell of 32 in base 64.</summary>
    public string? Grid { get; set; }
    /// <summary>The regions the map made, each as the index of the region and the west, south, east and north edges of a rect it holds.</summary>
    public List<float[]> Regions { get; set; } = [];
    /// <summary>The names units have, each unit holding the index of its name.</summary>
    public List<string> Names { get; set; } = [];
    /// <summary>The units at each sampled time.</summary>
    public List<TraceFrame> Frames { get; set; } = [];
    /// <summary>
    /// The missiles that flew, in the order they landed, then the ones still flying at the end: each as the times in seconds it left and landed (or
    /// the end), its player, the place it left and the place it landed (or was at the end).
    /// </summary>
    public List<double[]> Missiles { get; set; } = [];
    /// <summary>The log, each line with the time it was shown at.</summary>
    public List<TraceLine> Log { get; set; } = [];
    /// <summary>What stopped the run, if something did.</summary>
    public string? Stopped { get; set; }
}

/// <summary>The units at a time: each as its id, type, player, place, life, most life, order and the index of its name in <see cref="Trace.Names"/>.</summary>
internal sealed record TraceFrame(double Time, List<object[]> Units);

/// <summary>A line of the log, and the time in seconds it was shown at.</summary>
/// <param name="Time">The time in seconds.</param>
/// <param name="Text">What was shown, with where it came from.</param>
internal sealed record TraceLine(double Time, string Text);

/// <summary>The viewer: a page that opens a trace of a run, scrubs through it, shows the units, missiles and regions, and inspects a unit.</summary>
internal static class Viewer
{
    static readonly JsonSerializerOptions Format = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>Runs the simulation on for the time, sampling the units every <paramref name="every"/>, and writes down what it saw.</summary>
    internal static Trace Record(Simulation simulation, TimeSpan time, TimeSpan? every = null)
    {
        var slice = every ?? TimeSpan.FromSeconds(0.5);
        var trace = new Trace { Map = Path.GetFileName(simulation.Recording.Map) };
        var names = new Dictionary<string, int>();
        simulation.KeepFlights();
        void Sample()
        {
            var units = simulation.Units().Select(u =>
            {
                if (!names.TryGetValue(u.Name, out var name)) names[u.Name] = name = names.Count;
                return new object[] { u.Id, u.Type, u.Player, Math.Round(u.X, 1), Math.Round(u.Y, 1), Math.Round(u.Life, 1), Math.Round(u.MaxLife, 1), u.Order, name };
            }).ToList();
            trace.Frames.Add(new((double)simulation.Step / 1024, units));
        }
        Sample();
        for (var spent = TimeSpan.Zero; spent < time && simulation.Stopped is null; spent += slice)
        {
            try { simulation.Advance(slice); }
            catch (LuaException) when (simulation.Stopped is not null) { }
            Sample();
        }
        trace.Names = [.. names.OrderBy(pair => pair.Value).Select(pair => pair.Key)];
        trace.Log = [.. simulation.Log.Select(Line)];
        trace.Stopped = simulation.Stopped?.Message;
        trace.Missiles = simulation.Flights();
        simulation.Describe(trace);
        return trace;
    }

    // A log entry is "12.345 print: text": the time, then what it was
    static TraceLine Line(string entry)
    {
        var space = entry.IndexOf(' ');
        return space > 0 && double.TryParse(entry[..space], NumberStyles.Float, CultureInfo.InvariantCulture, out var at) ? new(at, entry[(space + 1)..]) : new(0, entry);
    }

    /// <summary>The page, a file that opens by itself in a browser, with the trace inside it.</summary>
    internal static string Page(Trace trace)
    {
        using var stream = typeof(Viewer).Assembly.GetManifestResourceStream("MirrorImaged.viewer.html")!;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        // A "</" in the data would end the script; "<\/" is the same text to JSON
        var json = JsonSerializer.Serialize(trace, Format).Replace("</", "<\\/");
        return reader.ReadToEnd().Replace("/*TRACE*/null", json);
    }
}

public sealed partial class Simulation
{
    // From here on the natives keep each missile's flight as it lands
    internal void KeepFlights() => _natives.Flights ??= [];

    // The flights kept, and those of the missiles in flight now
    internal List<double[]> Flights() => [.. _natives.Flights ?? [], .. _natives.Missiles.Select(_natives.Flight)];

    // The map's own shape: its bounds, pathing grid and regions
    internal void Describe(Trace trace)
    {
        if (_natives.WorldBounds() is { } bounds) trace.Bounds = [bounds.MinX, bounds.MinY, bounds.MaxX, bounds.MaxY];
        if (_natives.PathingGrid() is { } grid)
            (trace.GridWidth, trace.GridHeight, trace.GridLeft, trace.GridBottom, trace.Grid) = (grid.Width, grid.Height, grid.Left, grid.Bottom, Convert.ToBase64String(grid.Cells));
        trace.Regions = [.. _natives.RegionRects().Select(rect => new[] { rect.Region, rect.X1, rect.Y1, rect.X2, rect.Y2 })];
    }
}
