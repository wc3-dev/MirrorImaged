using System.Globalization;
using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>
/// The page of a small fight, open in a headless browser for <see cref="ViewerBrowserTests"/>: two footmen who walk at each other and fight, an archer
/// who shoots at a mark 600 away, and a region.
/// </summary>
public sealed class ViewerPage : IAsyncLifetime
{
    readonly TestMaps _maps = new();
    readonly Browser _browser = new();
    readonly string _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;

    public Trace Trace { get; private set; } = null!;
    public BrowserPage? Page { get; private set; }
    /// <summary>Why the page is not open in a browser; null when it is.</summary>
    public string? Missing => _browser.Missing;

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    /// <summary>Six seconds of the fight, sampled every half second.</summary>
    public static Trace Record(TestMaps maps)
    {
        var data = new StandardData()
            .Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=100;umvs=270;umvt=foot;uacq=500;uaen=1;ua1w=normal;ua1t=normal;ua1g=ground;ua1b=5;ua1d=2;ua1s=3;ua1c=1;ua1r=90;udp1=0.5;ucol=31;udef=0;udty=normal"))
            .Add(ObjectKind.Unit, "earc", Values("unam=Archer;uhpm=245;umvs=270;umvt=foot;uacq=700;uaen=1;ua1w=missile;ua1z=900;ulpy=62;ua1t=pierce;ua1g=ground;ua1b=10;ua1d=0;ua1s=1;ua1c=1.5;ua1r=700;udp1=0.72;ucol=16;udef=0"))
            .Add(ObjectKind.Unit, "hmrk", Values("unam=Mark;uhpm=50000;umvs=0;umvt=foot;uaen=0;ucol=31;udef=0"));
        using var simulation = Simulation.Load(maps.Build("""
            function config() end
            function main()
                CreateUnit(Player(0), FourCC("hfoo"), -300, 0, 0)
                CreateUnit(Player(1), FourCC("hfoo"), 300, 0, 0)
                IssueTargetOrder(CreateUnit(Player(0), FourCC("earc"), -200, 800, 0), "attack", CreateUnit(Player(1), FourCC("hmrk"), 400, 800, 0))
                local region = CreateRegion()
                RegionAddRect(region, Rect(-960, -960, -448, -448))
                TriggerRegisterEnterRegion(CreateTrigger(), region, nil)
                TimerStart(CreateTimer(), 2, false, function() print("two seconds </script>") end)
            end
            """), "", data: data);
        simulation.Order(0, simulation.Units(0)[0].Id, "move", 0, 0);
        return Viewer.Record(simulation, TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(0.5));
    }

    public async Task InitializeAsync()
    {
        Trace = Record(_maps);
        await _browser.InitializeAsync();
        if (_browser.Missing is not null) return;
        var file = Path.Combine(_folder, "run.html");
        File.WriteAllText(file, Viewer.Page(Trace));
        Page = await _browser.Open(file);
    }

    public async Task DisposeAsync()
    {
        if (Page is not null) await Page.DisposeAsync();
        await _browser.DisposeAsync();
        _maps.Dispose();
        Directory.Delete(_folder, true);
    }
}

/// <summary>
/// A12: the viewer's page in a headless browser opens a recording, scrubs its timeline, draws units, missiles and regions, and inspects a clicked
/// unit. What is drawn is read from the canvas's pixels at the places the page says it drew at. Skipped on a computer with no browser, and failed
/// where there is one that doesn't answer.
/// </summary>
public sealed class ViewerBrowserTests(ViewerPage viewer) : IClassFixture<ViewerPage>
{
    static readonly string[] Palette = ["#e03131", "#1c7ed6"];

    // The page, at its first frame with no unit selected
    async Task<BrowserPage> Page()
    {
        var page = viewer.Page ?? throw new InvalidOperationException(viewer.Missing);
        await page.Read("mirrorViewer.select(null)");
        await Scrub(page, 0);
        return page;
    }

    // Moves the timeline's slider to the frame, as dragging it does
    static Task<string> Scrub(BrowserPage page, int frame) =>
        page.Read($"(function () {{ var s = document.getElementById('scrub'); s.value = {frame}; s.dispatchEvent(new Event('input')); return ''; }})()");

    static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    // The colour of the canvas at a place in the world, moved by pixels on the screen
    static Task<string> Pixel(BrowserPage page, double x, double y, int right = 0, int down = 0) => page.Read($$"""
        (function () {
            var at = mirrorViewer.place({{Number(x)}}, {{Number(y)}});
            var d = document.getElementById('canvas').getContext('2d').getImageData(Math.round(at[0]) + {{right}}, Math.round(at[1]) + {{down}}, 1, 1).data;
            return '#' + [d[0], d[1], d[2]].map(function (v) { return ('0' + v.toString(16)).slice(-2); }).join('');
        })()
        """);

    static Task<string> Colour(BrowserPage page, string name) => page.Read($"getComputedStyle(document.documentElement).getPropertyValue('--{name}').trim()");

    static Task<string> Text(BrowserPage page, string id) => page.Read($"document.getElementById('{id}').innerText");

    // A person's click on the element's middle
    static async Task Click(BrowserPage page, string id, double right = double.NaN, double down = double.NaN)
    {
        var box = await page.Read<double[]>($"(function () {{ var r = document.getElementById('{id}').getBoundingClientRect(); return [r.left, r.top, r.width, r.height]; }})()");
        await page.Click(box[0] + (double.IsNaN(right) ? box[2] / 2 : right), box[1] + (double.IsNaN(down) ? box[3] / 2 : down));
    }

    [BrowserFact]
    public async Task The_page_opens_the_recording()
    {
        var page = await Page();

        Assert.Equal("13 frames, 1 log lines", await Text(page, "counts"));
        Assert.EndsWith(".w3x", await Text(page, "map"));
        Assert.Equal("0.0 s", await Text(page, "time"));
        Assert.Contains("two seconds </script>", await Text(page, "log"));
        Assert.Equal(13, await page.Read<int>("mirrorViewer.frames.length"));
    }

    [BrowserFact]
    public async Task Scrubbing_the_timeline_shows_another_time_and_the_buttons_step_a_frame()
    {
        var page = await Page();

        await Scrub(page, 4);
        Assert.Equal("2.0 s", await Text(page, "time"));
        await Click(page, "forward");
        Assert.Equal(("2.5 s", 5), (await Text(page, "time"), await page.Read<int>("mirrorViewer.index")));
        await Click(page, "back");
        await Click(page, "back");
        Assert.Equal(("1.5 s", 3), (await Text(page, "time"), await page.Read<int>("mirrorViewer.index")));
    }

    [BrowserFact]
    public async Task Each_unit_is_drawn_at_its_place_in_its_players_colour_and_moves_with_the_time()
    {
        var page = await Page();
        var ground = await Colour(page, "ground");

        var first = viewer.Trace.Frames[0].Units;
        Assert.Equal(4, first.Count);
        Assert.Equal(first.Select(u => new[] { Convert.ToDouble(u[0]), Convert.ToDouble(u[3]), Convert.ToDouble(u[4]) }), await page.Read<double[][]>("mirrorViewer.drawn.units"));
        foreach (var unit in first)
            Assert.Equal(Palette[(int)unit[2]], await Pixel(page, Convert.ToDouble(unit[3]), Convert.ToDouble(unit[4])));

        // The first footman walks from -300 to the middle: two seconds on it is drawn there, and the ground where it was
        await Scrub(page, 4);
        var walker = viewer.Trace.Frames[4].Units[0];
        Assert.NotEqual(first[0][3], walker[3]);
        Assert.Equal(Palette[0], await Pixel(page, Convert.ToDouble(walker[3]), 0));
        Assert.Equal(ground, await Pixel(page, -300, 0));
    }

    [BrowserFact]
    public async Task A_missile_is_drawn_on_its_way_while_it_flies()
    {
        var page = await Page();
        var (ground, ink) = (await Colour(page, "ground"), await Colour(page, "ink"));

        // The archer's first arrow leaves 0.75 s in and lands 0.6 s later: at the frame of 1.0 s it is on its way, and at 0.5 s and 1.5 s not there
        var flight = viewer.Trace.Missiles[0];
        Assert.InRange(1.0, flight[0], flight[1]);
        var x = flight[3] + (flight[5] - flight[3]) * (1.0 - flight[0]) / (flight[1] - flight[0]);
        await Scrub(page, 2);
        var drawn = Assert.Single(await page.Read<double[][]>("mirrorViewer.drawn.missiles"));
        Assert.Equal(x, drawn[0], 0.01);
        Assert.Equal(800, drawn[1], 0.01);
        Assert.Equal(ink, await Pixel(page, x, 800));

        foreach (var frame in new[] { 1, 3 })
        {
            await Scrub(page, frame);
            Assert.Empty(await page.Read<double[][]>("mirrorViewer.drawn.missiles"));
            Assert.Equal(ground, await Pixel(page, x, 800));
        }
    }

    [BrowserFact]
    public async Task A_region_is_outlined_and_tinted()
    {
        var page = await Page();
        var ground = await Colour(page, "ground");

        var region = Assert.Single(viewer.Trace.Regions);
        Assert.Equal(1, await page.Read<int>("mirrorViewer.drawn.regions"));
        var (west, south, east, north) = (region[1], region[2], region[3], region[4]);
        var (edge, inside, outside) = (await Pixel(page, (west + east) / 2, north), await Pixel(page, (west + east) / 2, (south + north) / 2), await Pixel(page, (west + east) / 2, north, down: -6));
        Assert.Equal(ground, outside);
        Assert.NotEqual(ground, inside);
        Assert.NotEqual(ground, edge);
        Assert.NotEqual(inside, edge);
        // Every side is drawn
        Assert.NotEqual(ground, await Pixel(page, (west + east) / 2, south));
        Assert.NotEqual(ground, await Pixel(page, west, (south + north) / 2));
        Assert.NotEqual(ground, await Pixel(page, east, (south + north) / 2));
    }

    [BrowserFact]
    public async Task A_click_on_a_unit_shows_it_in_the_panel_which_follows_it_through_time()
    {
        var page = await Page();

        Assert.Contains("Click a unit to inspect it.", await Text(page, "unit"));
        var foe = viewer.Trace.Frames[0].Units[1];
        var at = await page.Read<double[]>($"mirrorViewer.place({Number(Convert.ToDouble(foe[3]))}, {Number(Convert.ToDouble(foe[4]))})");
        await Click(page, "canvas", at[0], at[1]);
        Assert.Equal($"UNIT\nName\nFootman\nId\n{foe[0]}\nType\nhfoo\nPlayer\n1\nPlace\n300, 0\nLife\n100 / 100\nOrder\n0", (await Text(page, "unit")).ReplaceLineEndings("\n"));

        // By the last frame it has walked and been hurt
        await Scrub(page, 12);
        var later = viewer.Trace.Frames[12].Units.Single(u => Equals(u[0], foe[0]));
        Assert.True(Convert.ToDouble(later[5]) < 100);
        Assert.Contains($"Place\n{Number(Convert.ToDouble(later[3]))}, {Number(Convert.ToDouble(later[4]))}\nLife\n{Number(Convert.ToDouble(later[5]))} / 100", (await Text(page, "unit")).ReplaceLineEndings("\n"));

        // A click on the bare ground lets go of it
        await Click(page, "canvas", at[0], at[1] - 60);
        Assert.Contains("Click a unit to inspect it.", await Text(page, "unit"));
    }
}
