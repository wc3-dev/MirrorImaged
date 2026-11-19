using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace MirrorImaged.Tests;

/// <summary>
/// A headless browser driven over its debugging port: the one <c>MIRROR_BROWSER</c> names (<c>http://127.0.0.1:9333</c>, an Edge or Chrome
/// started with <c>--headless=new --remote-debugging-port=9333 --user-data-dir=&lt;a folder&gt;</c>), or an Edge or Chrome started here and closed
/// again. Where the browser doesn't answer, <see cref="Missing"/> says why.
/// </summary>
public sealed class Browser : IAsyncLifetime
{
    static readonly string[] Places =
    [
        @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe", @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
        @"C:\Program Files\Google\Chrome\Application\chrome.exe", "/usr/bin/microsoft-edge", "/usr/bin/google-chrome", "/usr/bin/chromium",
        "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge",
    ];

    /// <summary>Why no browser can be had on this computer: none is named and none installed; null when there is one to try.</summary>
    public static string? None =>
        Environment.GetEnvironmentVariable("MIRROR_BROWSER") is { Length: > 0 } || Places.Any(File.Exists) ? null
        : "No Edge or Chrome is installed; set MIRROR_BROWSER to a browser's debugging address (http://127.0.0.1:9333) to run it";

    readonly string _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;
    readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };
    string? _endpoint;
    bool _started;

    /// <summary>Why there is no browser; null when there is one.</summary>
    public string? Missing { get; private set; }

    public async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("MIRROR_BROWSER") is { Length: > 0 } named)
        {
            if (await Version(named.TrimEnd('/')) is null) Missing = $"No browser answers at MIRROR_BROWSER ({named})";
            else _endpoint = named.TrimEnd('/');
            return;
        }
        if (Places.FirstOrDefault(File.Exists) is not { } browser)
        {
            Missing = None;
            return;
        }
        // The browser writes the port it listens on into its profile; on Windows Edge's first process ends at once and leaves the browser running,
        // so the file is what says it started, and its output is never read
        var profile = Path.Combine(_folder, "profile");
        var start = new ProcessStartInfo(browser, ["--headless=new", "--disable-gpu", "--no-sandbox", "--no-first-run", "--remote-debugging-port=0", $"--user-data-dir={profile}", "about:blank"])
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        try { Process.Start(start)?.Dispose(); }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException) { }
        var port = Path.Combine(profile, "DevToolsActivePort");
        for (var waited = 0; waited < 150 && _endpoint is null; waited++)
        {
            await Task.Delay(100);
            if (!File.Exists(port)) continue;
            try
            {
                if (File.ReadLines(port).FirstOrDefault() is { Length: > 0 } number && await Version($"http://127.0.0.1:{number}") is not null)
                    (_endpoint, _started) = ($"http://127.0.0.1:{number}", true);
            }
            catch (IOException) { }
        }
        if (_endpoint is null) Missing = $"{browser} gave no debugging port in 15 s; set MIRROR_BROWSER to a browser's debugging address to run it";
    }

    async Task<JsonElement?> Version(string endpoint)
    {
        try { return JsonDocument.Parse(await _http.GetStringAsync($"{endpoint}/json/version")).RootElement; }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException or UriFormatException or InvalidOperationException) { return null; }
    }

    /// <summary>Opens the file in a tab of its own, 1100 by 700 at one pixel a pixel, and waits until the page has loaded.</summary>
    public async Task<BrowserPage> Open(string file)
    {
        var response = await _http.PutAsync($"{_endpoint}/json/new?{new Uri(file).AbsoluteUri}", null);
        var tab = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var page = new BrowserPage(tab.GetProperty("id").GetString()!, _endpoint!, _http);
        await page.Connect(tab.GetProperty("webSocketDebuggerUrl").GetString()!);
        await page.Send("Emulation.setDeviceMetricsOverride", new { width = 1100, height = 700, deviceScaleFactor = 1, mobile = false });
        for (var waited = 0; waited < 100 && await page.Read("document.readyState") != "complete"; waited++) await Task.Delay(100);
        return page;
    }

    public async Task DisposeAsync()
    {
        // Only a browser started here is closed
        if (_started && await Version(_endpoint!) is { } version)
        {
            await using var all = new BrowserPage("", _endpoint!, _http);
            try
            {
                await all.Connect(version.GetProperty("webSocketDebuggerUrl").GetString()!);
                await all.Send("Browser.close", null);
            }
            catch (Exception e) when (e is WebSocketException or IOException or OperationCanceledException or InvalidOperationException) { }
        }
        _http.Dispose();
        // The browser's processes may hold their profile for a moment more; the folder is the system's temporary one
        for (var tries = 0; tries < 20; tries++)
        {
            try { Directory.Delete(_folder, true); return; }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { await Task.Delay(100); }
        }
    }
}

/// <summary>A test that needs a browser; skipped on a computer that has none.</summary>
public sealed class BrowserFactAttribute : FactAttribute
{
    public BrowserFactAttribute() => Skip = Browser.None;
}

/// <summary>A tab of the browser: its script is run, and the mouse clicked, over the DevTools protocol.</summary>
public sealed class BrowserPage(string id, string endpoint, HttpClient http) : IAsyncDisposable
{
    readonly ClientWebSocket _socket = new();
    int _sent;

    internal Task Connect(string address) => _socket.ConnectAsync(new Uri(address), new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

    /// <summary>Sends a command and answers its result.</summary>
    public async Task<JsonElement> Send(string method, object? parameters)
    {
        using var limit = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var sent = ++_sent;
        await _socket.SendAsync(JsonSerializer.SerializeToUtf8Bytes(new { id = sent, method, @params = parameters ?? new { } }), WebSocketMessageType.Text, true, limit.Token);
        var buffer = new byte[1 << 16];
        while (true)
        {
            using var message = new MemoryStream();
            WebSocketReceiveResult part;
            do
            {
                part = await _socket.ReceiveAsync(buffer, limit.Token);
                message.Write(buffer, 0, part.Count);
            } while (!part.EndOfMessage);
            // The browser's own events come between; the answer carries the command's number
            var answer = JsonDocument.Parse(message.ToArray()).RootElement;
            if (!answer.TryGetProperty("id", out var number) || number.GetInt32() != sent) continue;
            if (answer.TryGetProperty("error", out var error)) throw new InvalidOperationException($"{method}: {error}");
            return answer.GetProperty("result");
        }
    }

    /// <summary>What the expression gives in the page, as text: a string as it is, anything else as JSON.</summary>
    public async Task<string> Read(string expression)
    {
        var result = await Send("Runtime.evaluate", new { expression = $"(function () {{ var v = ({expression}); return typeof v === 'string' ? v : JSON.stringify(v); }})()", returnByValue = true });
        if (result.TryGetProperty("exceptionDetails", out var thrown)) throw new InvalidOperationException($"{expression}: {thrown}");
        return result.GetProperty("result").TryGetProperty("value", out var value) ? value.GetString() ?? "" : "";
    }

    /// <summary>What the expression gives in the page, read from its JSON.</summary>
    public async Task<T> Read<T>(string expression) => JsonSerializer.Deserialize<T>(await Read($"JSON.stringify({expression})"))!;

    /// <summary>Presses and releases the left mouse button at the place in the page, as a person's click.</summary>
    public async Task Click(double x, double y)
    {
        foreach (var type in new[] { "mousePressed", "mouseReleased" })
            await Send("Input.dispatchMouseEvent", new { type, x, y, button = "left", buttons = type == "mousePressed" ? 1 : 0, clickCount = 1 });
    }

    public async ValueTask DisposeAsync()
    {
        _socket.Dispose();
        if (id.Length == 0) return;
        try { await http.GetAsync($"{endpoint}/json/close/{id}"); }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or ObjectDisposedException) { }
    }
}
