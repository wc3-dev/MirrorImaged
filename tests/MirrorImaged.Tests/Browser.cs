using System.Diagnostics;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MirrorImaged.Tests;

/// <summary>
/// A headless browser driven over its debugging port: the one <c>MIRROR_BROWSER</c> names (<c>http://127.0.0.1:9333</c>, an Edge or Chrome
/// started with <c>--headless=new --remote-debugging-port=9333 --user-data-dir=&lt;a folder&gt;</c>), or an Edge or Chrome started here and closed
/// again. Where the browser doesn't answer, <see cref="Missing"/> says why. On Windows the browser started here runs on a desktop of its own, so
/// that no window of it is on the desktop the person at the computer sees.
/// </summary>
public sealed class Browser : IAsyncLifetime
{
    static readonly string[] Places =
    [
        @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe", @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
        @"C:\Program Files\Google\Chrome\Application\chrome.exe", "/usr/bin/google-chrome", "/usr/bin/chromium", "/usr/bin/microsoft-edge",
        "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome", "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge",
    ];

    /// <summary>Why no browser can be had on this computer: none is named and none installed; null when there is one to try.</summary>
    public static string? None =>
        Environment.GetEnvironmentVariable("MIRROR_BROWSER") is { Length: > 0 } || Places.Any(File.Exists) ? null
        : "No Edge or Chrome is installed; set MIRROR_BROWSER to a browser's debugging address (http://127.0.0.1:9333) to run it";

    readonly string _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;
    readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(5) };
    readonly StringBuilder _said = new();
    Process? _process;
    nint _desktop;
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
        // The browser writes the port it listens on into its profile, and says it too. On Windows Edge's first process ends at once and leaves the
        // browser running, so the file is what says it started there. Elsewhere what it prints is read as it comes: a browser whose output nobody
        // reads writes into a closed pipe
        var clock = Stopwatch.StartNew();
        long firstLine = -1;
        var profile = Path.Combine(_folder, "profile");
        string[] switches = ["--headless=new", "--disable-gpu", "--no-sandbox", "--no-first-run", "--disable-dev-shm-usage", "--remote-debugging-port=0", $"--user-data-dir={profile}", "about:blank"];
        var timing = Environment.GetEnvironmentVariable("MIRROR_BROWSER_TIMES") is { Length: > 0 };
        if (timing) switches = ["--enable-logging=stderr", "--v=1", .. switches];
        var load = File.Exists("/proc/loadavg") ? File.ReadAllText("/proc/loadavg").Trim() : "";
        var others = Directory.Exists("/proc") ? Directory.GetDirectories("/proc").Count(d => int.TryParse(Path.GetFileName(d), out _) && File.Exists(Path.Combine(d, "cmdline"))
            && TryRead(Path.Combine(d, "cmdline")).Contains("MirrorImaged.GameTests")) : -1;
        static string TryRead(string path) { try { return File.ReadAllText(path); } catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return ""; } }
        try
        {
            if (OperatingSystem.IsWindows()) StartOnItsOwnDesktop(browser, switches);
            else
            {
                void Said(object sender, DataReceivedEventArgs line)
                {
                    lock (_said)
                    {
                        if (firstLine < 0 && !string.IsNullOrEmpty(line.Data)) firstLine = clock.ElapsedMilliseconds;
                        _said.AppendLine(line.Data);
                    }
                }
                _process = Process.Start(new ProcessStartInfo(browser, switches) { RedirectStandardOutput = true, RedirectStandardError = true });
                if (_process is not null)
                {
                    _process.OutputDataReceived += Said;
                    _process.ErrorDataReceived += Said;
                    _process.BeginOutputReadLine();
                    _process.BeginErrorReadLine();
                }
            }
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            Missing = $"{browser} didn't start: {e.Message}";
            return;
        }
        var file = Path.Combine(profile, "DevToolsActivePort");
        for (var waited = 0; waited < 300 && _endpoint is null; waited++)
        {
            await Task.Delay(100);
            string? port = null;
            try { port = File.Exists(file) ? File.ReadLines(file).FirstOrDefault() : null; }
            catch (IOException) { }
            lock (_said) port ??= Regex.Match(_said.ToString(), @"DevTools listening on ws://[^:/]+:(\d+)/") is { Success: true } listening ? listening.Groups[1].Value : null;
            if (port is { Length: > 0 } && await Version($"http://127.0.0.1:{port}") is not null) (_endpoint, _started) = ($"http://127.0.0.1:{port}", true);
        }
        if (Environment.GetEnvironmentVariable("MIRROR_BROWSER_TIMES") is { Length: > 0 } times)
            lock (_said)
            {
                File.AppendAllText(times, $"{clock.ElapsedMilliseconds} {(_endpoint is null ? "fail" : "ok")} first-line={firstLine} dbus={_said.ToString().Contains("dbus/bus.cc")} load=[{load}] game-tests-processes={others}\n");
                // A slow start's log: each line that came a second or more after the one before it, with the gap
                if (clock.ElapsedMilliseconds > 3000)
                {
                    double? last = null;
                    foreach (var line in _said.ToString().Split('\n'))
                        if (Regex.Match(line, @"^\[\d+:\d+:\d+/(\d\d)(\d\d)(\d\d\.\d+)") is { Success: true } at)
                        {
                            var seconds = int.Parse(at.Groups[1].Value) * 3600 + int.Parse(at.Groups[2].Value) * 60 + double.Parse(at.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);
                            if (last is { } before && seconds - before >= 0.5) File.AppendAllText(times, $"  +{seconds - before:0.0}s {line[..Math.Min(line.Length, 240)]}\n");
                            last = seconds;
                        }
                }
            }
        if (_endpoint is not null) return;
        lock (_said) Missing = $"{browser} gave no debugging port in 30 s{(_process is { HasExited: true } ? $" and ended with code {_process.ExitCode}" : "")}; it said: {_said.ToString().Trim()}";
    }

    // A headless Edge or Chrome on Windows still makes a top-level window with the page's title: never shown, but on the desktop's list of windows,
    // and so in Alt-Tab (Sven saw them, 2026-11-19). No switch of the browser's takes it away (--headless, =new and =old, and a place off the screen
    // all leave it), so the browser is started on a desktop of its own, where its windows are no part of the desktop a person sees. Its output
    // isn't read there: the port's file says it started
    void StartOnItsOwnDesktop(string browser, string[] switches)
    {
        var name = $"MirrorImaged browser {Path.GetFileName(_folder)}";
        // DESKTOP_READOBJECTS and DESKTOP_CREATEWINDOW: without the second, CreateDesktop fails
        _desktop = CreateDesktop(name, 0, 0, 0, 0x1 | 0x2, 0);
        if (_desktop == 0) throw new System.ComponentModel.Win32Exception();
        var startup = new StartupInfo { Size = Marshal.SizeOf<StartupInfo>(), Desktop = name };
        var command = new StringBuilder($"\"{browser}\" {string.Join(' ', switches.Select(s => s.Contains(' ') ? $"\"{s}\"" : s))}");
        if (!CreateProcess(browser, command, 0, 0, false, 0, 0, null, ref startup, out var started)) throw new System.ComponentModel.Win32Exception();
        CloseHandle(started.Thread);
        CloseHandle(started.Process);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct StartupInfo
    {
        public int Size;
        public string? Reserved, Desktop, Title;
        public int X, Y, Width, Height, CountCharsX, CountCharsY, FillAttribute, Flags;
        public short ShowWindow, Reserved2Size;
        public nint Reserved2, StdInput, StdOutput, StdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct ProcessInformation
    {
        public nint Process, Thread;
        public int ProcessId, ThreadId;
    }

    [DllImport("user32", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern nint CreateDesktop(string name, nint device, nint mode, int flags, uint access, nint attributes);

    [DllImport("user32")]
    static extern bool CloseDesktop(nint desktop);

    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool CreateProcess(string application, StringBuilder commandLine, nint processAttributes, nint threadAttributes, bool inheritHandles,
        int flags, nint environment, string? directory, ref StartupInfo startup, out ProcessInformation information);

    [DllImport("kernel32")]
    static extern bool CloseHandle(nint handle);

    async Task<JsonElement?> Version(string endpoint)
    {
        try { return JsonDocument.Parse(await _http.GetStringAsync($"{endpoint}/json/version")).RootElement; }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException or UriFormatException or InvalidOperationException) { return null; }
    }

    /// <summary>Opens the file in a tab of its own, 1100 by 700 at one pixel a pixel, and waits until the page has loaded.</summary>
    /// <param name="file">The page's file.</param>
    /// <param name="ready">What must hold in the page, beside its load, before it is handed over: an expression of its script.</param>
    public async Task<BrowserPage> Open(string file, string ready = "true")
    {
        // A new tab starts on an empty document that is complete at once, so the tab is sized first, then sent to the page, and the wait is for
        // the page's own document: a wait for a complete document alone can be over before the page has begun to load
        var response = await _http.PutAsync($"{_endpoint}/json/new?about:blank", null);
        var tab = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var page = new BrowserPage(tab.GetProperty("id").GetString()!, _endpoint!, _http);
        await page.Connect(tab.GetProperty("webSocketDebuggerUrl").GetString()!);
        await page.Send("Emulation.setDeviceMetricsOverride", new { width = 1100, height = 700, deviceScaleFactor = 1, mobile = false });
        await page.Send("Page.navigate", new { url = new Uri(file).AbsoluteUri });
        await page.Until($"location.href !== 'about:blank' && document.readyState === 'complete' && ({ready})", TimeSpan.FromSeconds(30));
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
        if (_process is not null)
        {
            if (!_process.WaitForExit(3000)) _process.Kill(true);
            _process.Dispose();
        }
        // The desktop goes when its last window has
        if (_desktop != 0) CloseDesktop(_desktop);
        // The browser's processes may hold their profile for a moment more; the folder is the system's temporary one
        for (var tries = 0; tries < 20; tries++)
        {
            try { Directory.Delete(_folder, true); return; }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { await Task.Delay(100); }
        }
    }
}

/// <summary>
/// The tests that start a browser, which run when no other test of the assembly runs. A browser's first start on a fresh machine reads its files
/// from the disk: on the Linux runner that took under a second alone and up to 22 s beside the rest of the suite, against the 30 s
/// <see cref="Browser"/> waits for it (decisions D690).
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class BrowserTests
{
    public const string Name = "Browser";
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

    /// <summary>
    /// Waits until the expression holds in the page, looking again as soon as the page has answered. A look that throws counts as not yet: the
    /// document it ran in may have just been replaced.
    /// </summary>
    /// <exception cref="TimeoutException">It didn't hold in the time; the message says where the page was.</exception>
    public async Task Until(string condition, TimeSpan within)
    {
        var (clock, last) = (System.Diagnostics.Stopwatch.StartNew(), "");
        while (clock.Elapsed < within)
        {
            try
            {
                if (await Read($"!!({condition})") == "true") return;
                last = "";
            }
            catch (InvalidOperationException e) { last = e.Message; }
            await Task.Yield();
        }
        string state;
        try { state = await Read("'at ' + location.href + ', ' + document.readyState"); }
        catch (InvalidOperationException e) { state = $"not answering ({e.Message})"; }
        throw new TimeoutException($"The page didn't get ready in {within.TotalSeconds} s: {condition} never held. It is {state}.{(last.Length > 0 ? $" The last look threw: {last}" : "")}");
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
