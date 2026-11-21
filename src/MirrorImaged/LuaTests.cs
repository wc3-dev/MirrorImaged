using System.Globalization;
using System.Text;
using MirrorImaged.Data;
using MirrorImaged.Lua;

namespace MirrorImaged;

/// <summary>What one Lua test did.</summary>
public sealed record LuaTestResult(string File, string Name, bool Passed, string? Message, TimeSpan Time);

/// <summary>
/// Tests written in Lua: a file calls <c>test(name, function)</c>, and inside a test <c>mirror.load(map, {seed = n, difficulty = "easy", graphics = "classic"})</c> (the difficulty of the computers: easy, normal or insane; the player's graphics mode: reforged or classic) gives a simulation
/// with the calls of the C# API (<c>advance</c>, <c>units</c>, <c>order</c>, <c>select</c>, <c>key</c>, <c>mouse</c>, <c>leave</c>, <c>say</c>,
/// <c>log</c>, <c>hash</c>, <c>wait_until</c>, <c>recording</c>, <c>recorded</c>; a map path that is not rooted is read from the test file's folder), and <c>assert_eq</c>, <c>assert_true</c>, <c>assert_close</c> and
/// <c>assert_contains</c> check results. The test files run in a Lua state of their own, apart from the games they load.
/// </summary>
public sealed class LuaTestHost : IDisposable
{
    readonly LuaState _lua = new();
    readonly Dictionary<int, Simulation> _simulations = [];
    int _next = 1;
    string _folder = "";

    /// <summary>Makes a host with the test API ready.</summary>
    public LuaTestHost()
    {
        _lua.Register("__load", args => Open(Text(args, 0), Int(args, 1), args.Length > 2 && args[2] is string difficulty ? Enum.Parse<Difficulty>(difficulty, true) : Difficulty.Normal,
            args.Length > 3 && args[3] is string graphics ? Enum.Parse<GraphicsMode>(graphics, true) : GraphicsMode.Reforged));
        _lua.Register("__close", args =>
        {
            if (_simulations.Remove(Int(args, 0), out var simulation)) simulation.Dispose();
            return [];
        });
        _lua.Register("__advance", args => With(args, simulation => simulation.Advance(TimeSpan.FromSeconds(Number(args, 1)))));
        _lua.Register("__advance_steps", args => With(args, simulation => simulation.AdvanceSteps((long)Number(args, 1))));
        _lua.Register("__step", args => [(double)Sim(args).Step]);
        _lua.Register("__ticks", args => [(double)TimeSpan.FromSeconds(Number(args, 0)).Ticks]);
        _lua.Register("__hash", args => [Sim(args).StateHash()]);
        _lua.Register("__stopped", args => [Sim(args).Stopped?.Message]);
        _lua.Register("__log", args => [string.Join('\n', Sim(args).Log)]);
        _lua.Register("__recording", args => [Sim(args).Recording.ToJson()]);
        _lua.Register("__recorded_count", args => [Sim(args).RecordedCalls.Count]);
        _lua.Register("__recorded", args =>
        {
            var call = Sim(args).RecordedCalls[Int(args, 1) - 1];
            return [(double)call.Step, call.Native, .. call.Arguments.Select(a => a switch { Natives.handle h => h.Id, Delegate => "function", _ => a })];
        });
        _lua.Register("__units", args => [string.Join('\n', Sim(args).Units(args[1] is null ? null : Int(args, 1)).Select(Row))]);
        _lua.Register("__unit", args => [Sim(args).Unit(Int(args, 1)) is { } unit ? Row(unit) : null]);
        _lua.Register("__order", args => With(args, simulation =>
            simulation.Order(Int(args, 1), Int(args, 2), Text(args, 3), args[4] is null ? null : (float)Number(args, 4), args[5] is null ? null : (float)Number(args, 5), args[6] is null ? null : Int(args, 6))));
        _lua.Register("__select", args => With(args, simulation =>
            simulation.Select(Int(args, 1), [.. Text(args, 2).Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse)])));
        _lua.Register("__key", args => With(args, simulation => simulation.Key(Int(args, 1), Int(args, 2), (bool)args[3]!, Int(args, 4))));
        _lua.Register("__mouse", args => With(args, simulation => simulation.Mouse(Int(args, 1), Int(args, 2), Int(args, 3), (float)Number(args, 4), (float)Number(args, 5))));
        _lua.Register("__leave", args => With(args, simulation => simulation.Leave(Int(args, 1))));
        _lua.Register("__say", args => With(args, simulation => simulation.Say(Int(args, 1), Text(args, 2))));
        _lua.Run(Prelude, "mirror-tests");
    }

    static string Row(UnitView u) => string.Join('\t', u.Id, u.Type, u.Name.Replace('\t', ' ').Replace('\n', ' '), u.Player,
        u.X.ToString("R", CultureInfo.InvariantCulture), u.Y.ToString("R", CultureInfo.InvariantCulture), u.Life.ToString("R", CultureInfo.InvariantCulture),
        u.MaxLife.ToString("R", CultureInfo.InvariantCulture), u.Mana.ToString("R", CultureInfo.InvariantCulture), u.Order, u.Dead ? 1 : 0);

    static double Number(object?[] args, int i) => Convert.ToDouble(args[i], CultureInfo.InvariantCulture);
    static int Int(object?[] args, int i) => (int)Convert.ToDouble(args[i], CultureInfo.InvariantCulture);
    static string Text(object?[] args, int i) => (string)args[i]!;

    Simulation Sim(object?[] args) => _simulations.TryGetValue(Int(args, 0), out var simulation) ? simulation : throw new InvalidOperationException("The simulation is closed");

    object?[] With(object?[] args, Action<Simulation> action)
    {
        var simulation = Sim(args);
        try { action(simulation); }
        catch (LuaException) when (simulation.Stopped is not null) { }
        return [];
    }

    object?[] Open(string path, int seed, Difficulty difficulty, GraphicsMode graphics)
    {
        var id = _next++;
        _simulations[id] = Simulation.Load(Path.IsPathRooted(path) ? path : Path.Combine(_folder, path), new SimulationOptions { Seed = seed, Difficulty = difficulty, Graphics = graphics });
        return [id];
    }

    /// <summary>Runs the tests of the Lua file, each in turn; a test that raises an error, in the file or a check, fails with its message.</summary>
    public IReadOnlyList<LuaTestResult> Run(string path)
    {
        var results = new List<LuaTestResult>();
        var file = Path.GetFileName(path);
        _folder = Path.GetDirectoryName(Path.GetFullPath(path)) ?? "";
        try { _lua.Run(File.ReadAllText(path), "@" + file); }
        catch (LuaException e)
        {
            results.Add(new(file, "(loading the file)", false, e.Message, TimeSpan.Zero));
            return results;
        }
        var count = Convert.ToInt32(_lua.Call("__count")[0], CultureInfo.InvariantCulture);
        for (var i = 1; i <= count; i++)
        {
            var name = (string)_lua.Call("__name", i)[0]!;
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var outcome = _lua.Call("__run", i);
            results.Add(new(file, name, outcome[0] is true, outcome.Length > 1 ? outcome[1] as string : null, clock.Elapsed));
        }
        _lua.Call("__reset");
        return results;
    }

    /// <summary>The names of the tests a Lua test file makes, in its order, without running them or any map.</summary>
    public static IReadOnlyList<string> Names(string path)
    {
        using var host = new LuaTestHost();
        try { host._lua.Run(File.ReadAllText(path), "@" + Path.GetFileName(path)); }
        catch (LuaException) { return []; }
        var count = Convert.ToInt32(host._lua.Call("__count")[0], CultureInfo.InvariantCulture);
        return [.. Enumerable.Range(1, count).Select(i => (string)host._lua.Call("__name", i)[0]!)];
    }

    /// <summary>The results as the XML that CI services and editors read (JUnit).</summary>
    public static string JUnit(IReadOnlyList<LuaTestResult> results)
    {
        static string Escape(string text) => System.Security.SecurityElement.Escape(text) ?? "";
        var text = new StringBuilder();
        text.Append($"""<?xml version="1.0" encoding="UTF-8"?>{"\n"}<testsuites tests="{results.Count}" failures="{results.Count(r => !r.Passed)}">{"\n"}""");
        foreach (var file in results.GroupBy(r => r.File))
        {
            text.Append($"""  <testsuite name="{Escape(file.Key)}" tests="{file.Count()}" failures="{file.Count(r => !r.Passed)}" time="{file.Sum(r => r.Time.TotalSeconds).ToString("0.###", CultureInfo.InvariantCulture)}">{"\n"}""");
            foreach (var result in file)
            {
                var time = result.Time.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture);
                text.Append($"""    <testcase classname="{Escape(file.Key)}" name="{Escape(result.Name)}" time="{time}">""");
                if (!result.Passed) text.Append($"""<failure message="{Escape(result.Message ?? "failed")}"/>""");
                text.Append("</testcase>\n");
            }
            text.Append("  </testsuite>\n");
        }
        text.Append("</testsuites>\n");
        return text.ToString();
    }

    /// <summary>Closes the simulations the tests opened and the host.</summary>
    public void Dispose()
    {
        foreach (var simulation in _simulations.Values) simulation.Dispose();
        _simulations.Clear();
        _lua.Dispose();
    }

    const string Prelude = """
        local tests, sims = {}, {}

        local function fields(row)
          local f = {}
          for v in (row .. "\t"):gmatch("([^\t]*)\t") do f[#f + 1] = v end
          return {id = tonumber(f[1]), type = f[2], name = f[3], player = tonumber(f[4]), x = tonumber(f[5]), y = tonumber(f[6]),
                  life = tonumber(f[7]), max_life = tonumber(f[8]), mana = tonumber(f[9]), order = tonumber(f[10]), dead = f[11] == "1"}
        end

        local function lines(text)
          local list = {}
          if text == "" then return list end
          for line in (text .. "\n"):gmatch("([^\n]*)\n") do list[#list + 1] = line end
          return list
        end

        mirror = {}

        function mirror.load(path, options)
          local h = __load(path, (options and options.seed) or 0, options and options.difficulty, options and options.graphics)
          local sim = {}
          sims[#sims + 1] = sim
          function sim:advance(seconds) __advance(h, seconds) end
          function sim:advance_steps(steps) __advance_steps(h, steps) end
          function sim:step() return __step(h) end
          function sim:time() return __step(h) / 1024 end
          function sim:hash() return __hash(h) end
          function sim:stopped() return __stopped(h) end
          function sim:log() return lines(__log(h)) end
          function sim:recording() return __recording(h) end
          function sim:recorded()
            local list = {}
            for i = 1, __recorded_count(h) do
              local call = table.pack(__recorded(h, i))
              list[i] = {step = call[1], native = call[2], args = table.pack(table.unpack(call, 3, call.n))}
            end
            return list
          end
          function sim:units(player)
            local list = {}
            for _, row in ipairs(lines(__units(h, player))) do list[#list + 1] = fields(row) end
            return list
          end
          function sim:unit(id)
            local row = __unit(h, id)
            return row and fields(row)
          end
          function sim:order(player, unit, order, x, y, target) __order(h, player, unit, order, x, y, target) end
          function sim:select(player, ...) __select(h, player, table.concat({...}, ",")) end
          function sim:key(player, key, down, meta) __key(h, player, key, down and true or false, meta or 0) end
          function sim:mouse(player, kind, button, x, y) __mouse(h, player, kind, button, x, y) end
          function sim:leave(player) __leave(h, player) end
          function sim:say(player, text) __say(h, player, text) end
          function sim:wait_until(check, within, every)
            every = every or 0.1
            -- counted in .NET ticks, as Until counts: added up as numbers, ten 0.1 s fall short of 1 s and an eleventh runs
            local slice, limit, spent = __ticks(every), __ticks(within), 0
            while true do
              if check() then return true end
              if spent >= limit then return false end
              __advance(h, every)
              spent = spent + slice
            end
          end
          function sim:close() __close(h) end
          return sim
        end

        function test(name, fn) tests[#tests + 1] = {name = name, fn = fn} end

        function fail(message) error(message or "failed", 2) end

        function assert_true(value, message)
          if not value then error(message or "expected a true value", 2) end
        end

        function assert_eq(actual, expected, message)
          if actual ~= expected then
            error((message and (message .. ": ") or "") .. "expected " .. tostring(expected) .. ", got " .. tostring(actual), 2)
          end
        end

        function assert_close(actual, expected, tolerance, message)
          if type(actual) ~= "number" or math.abs(actual - expected) > (tolerance or 0.001) then
            error((message and (message .. ": ") or "") .. "expected " .. tostring(expected) .. " within " .. tostring(tolerance or 0.001) .. ", got " .. tostring(actual), 2)
          end
        end

        function assert_contains(list, wanted, message)
          if type(list) == "string" then
            if not list:find(wanted, 1, true) then error((message or "") .. "expected \"" .. list .. "\" to contain \"" .. wanted .. "\"", 2) end
            return
          end
          for _, item in ipairs(list) do
            if item == wanted or (type(item) == "string" and type(wanted) == "string" and item:find(wanted, 1, true)) then return end
          end
          error((message or "") .. "expected the list to contain " .. tostring(wanted), 2)
        end

        function __count() return #tests end
        function __name(i) return tests[i].name end
        function __reset() tests = {} end

        function __run(i)
          local ok, err = pcall(tests[i].fn)
          for _, sim in ipairs(sims) do pcall(sim.close, sim) end
          sims = {}
          if ok then return true end
          return false, tostring(err)
        end
        """;
}
