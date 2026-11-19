using System.Text.Json;
using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>The viewer: a trace of a run with its units over time, its missiles and its log, and the page that holds it; <see cref="ViewerBrowserTests"/> opens the page.</summary>
public sealed class ViewerTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static StandardData Standard() => new StandardData()
        .Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=100;umvs=270;umvt=foot;uacq=500;uaen=1;ua1w=normal;ua1t=normal;ua1g=ground;ua1b=5;ua1d=2;ua1s=3;ua1c=1;ua1r=90;udp1=0.5;ucol=31;udef=0;udty=normal"));

    Trace Run(double seconds = 6)
    {
        var path = _maps.Build("""
            function config() end
            function main()
                CreateUnit(Player(0), FourCC("hfoo"), -300, 0, 0)
                CreateUnit(Player(1), FourCC("hfoo"), 300, 0, 0)
                TimerStart(CreateTimer(), 2, false, function() print("two seconds </script>") end)
            end
            """);
        using var simulation = Simulation.Load(path, "", data: Standard());
        simulation.Order(0, simulation.Units(0)[0].Id, "move", 0, 0);
        return Viewer.Record(simulation, TimeSpan.FromSeconds(seconds), TimeSpan.FromSeconds(0.5));
    }

    [Fact]
    public void A_trace_has_the_units_at_each_sampled_time_and_the_log_with_its_times()
    {
        var trace = Run();

        Assert.Equal(13, trace.Frames.Count);
        Assert.Equal([0, 0.5], trace.Frames.Take(2).Select(frame => frame.Time));
        Assert.Equal(2, trace.Frames[0].Units.Count);
        Assert.Contains("Footman", trace.Names);
        var line = Assert.Single(trace.Log);
        Assert.Equal(2, line.Time, 0.01);
        Assert.Contains("two seconds", line.Text);
        // The two footmen walk at each other
        var (first, last) = (trace.Frames[0].Units.OrderBy(u => (int)u[0]).First(), trace.Frames[^1].Units.OrderBy(u => (int)u[0]).First());
        Assert.NotEqual(first[3], last[3]);
    }

    [Fact]
    public void The_page_holds_the_trace_and_a_script_end_in_the_log_cannot_break_it()
    {
        var page = Viewer.Page(Run());

        Assert.Contains("<canvas", page);
        Assert.Contains("\"frames\"", page);
        Assert.DoesNotContain("two seconds </script>", page);
        var json = page[(page.IndexOf("type=\"application/json\">") + "type=\"application/json\">".Length)..];
        json = json[..json.IndexOf("</script>")];
        Assert.Equal(13, JsonDocument.Parse(json).RootElement.GetProperty("frames").GetArrayLength());
    }

    // The archer of the browser tests' fight shoots at a mark 600 away every 1.5 s: each arrow leaves 62 ahead of it, 0.75 s in the first, and lands
    // on the mark 0.6 s later
    [Fact]
    public void A_trace_has_each_missiles_flight()
    {
        var trace = ViewerPage.Record(_maps);

        Assert.Equal(4, trace.Missiles.Count);
        Assert.All(trace.Missiles, flight => Assert.Equal([0, -138, 800, 400, 800], flight[2..]));
        Assert.Equal([0.75, 2.25, 3.75, 5.25], trace.Missiles.Select(flight => Math.Round(flight[0], 2)));
        Assert.All(trace.Missiles, flight => Assert.Equal(538.0 / 900, flight[1] - flight[0], 0.001));
        Assert.Contains("\"missiles\":[[", Viewer.Page(trace));
    }
}
