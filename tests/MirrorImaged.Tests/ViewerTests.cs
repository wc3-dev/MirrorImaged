using System.Diagnostics;
using System.Text.Json;
using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>The viewer: a trace of a run with its units over time and its log, the page that holds it, and the page opened in a headless browser.</summary>
public sealed class ViewerTests : IDisposable
{
    readonly TestMaps _maps = new();
    readonly string _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;

    public void Dispose()
    {
        _maps.Dispose();
        Directory.Delete(_folder, true);
    }

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

    // The page opens in a browser, reads the trace and shows how many frames it has (when Edge is installed)
    [Fact]
    public async Task The_page_opens_in_a_headless_browser()
    {
        var edge = new[] { @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe", @"C:\Program Files\Microsoft\Edge\Application\msedge.exe", "/usr/bin/microsoft-edge", "/usr/bin/google-chrome", "/usr/bin/chromium" }
            .FirstOrDefault(File.Exists);
        if (edge is null) return;
        var file = Path.Combine(_folder, "run.html");
        File.WriteAllText(file, Viewer.Page(Run()));

        var start = new ProcessStartInfo(edge, ["--headless=new", "--disable-gpu", "--no-sandbox", $"--user-data-dir={Path.Combine(_folder, "profile")}", "--virtual-time-budget=3000", "--dump-dom", new Uri(file).AbsoluteUri])
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var process = Process.Start(start)!;
        var error = process.StandardError.ReadToEndAsync();
        var dom = await process.StandardOutput.ReadToEndAsync();
        process.WaitForExit(30000);
        if (!process.HasExited) process.Kill(true);

        // Some machines start no headless browser from here (it prints nothing and ends): then there is nothing to check
        if (dom.Length == 0) return;
        Assert.True(dom.Contains("13 frames, 1 log lines"), $"{dom}{await error}");
        Assert.Contains("two seconds", dom);
    }
}
