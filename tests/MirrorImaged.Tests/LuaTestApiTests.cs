using System.Globalization;
using System.Text;

namespace MirrorImaged.Tests;

/// <summary>The Lua test API and <c>mirror test</c>: the same calls as the C# API, and a case list both run to the same state.</summary>
[Trait("Needs", "Game")]
public sealed class LuaTestApiTests : IDisposable
{
    readonly TestMaps _maps = new();
    readonly string _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;

    public void Dispose()
    {
        _maps.Dispose();
        Directory.Delete(_folder, true);
    }

    string Map(string main) => _maps.Build($$"""
        function config() end
        function main()
        {{main}}
        end
        """);

    // The shared cases (A9): a map's script, and what a test does to it, as steps both APIs can make
    sealed record Case(string Name, string Main, string[] Steps);

    static readonly Case[] Cases =
    [
        new("a timer prints as time passes", """TimerStart(CreateTimer(), 1, true, function() print("tick", GetRandomInt(1, 100)) end)""", ["advance 3.5"]),
        new("chat fires its trigger", """
            local t = CreateTrigger()
            TriggerRegisterPlayerChatEvent(t, Player(0), "-go", false)
            TriggerAddAction(t, function() print("said", GetEventPlayerChatString()) end)
            """, ["advance 1", "say 0 -go now", "advance 1"]),
        new("a key press fires its trigger", """
            local t = CreateTrigger()
            BlzTriggerRegisterPlayerKeyEvent(t, Player(0), OSKEY_Q, 0, true)
            TriggerAddAction(t, function() print("key") end)
            """, ["key 0 81 down", "key 0 81 up", "advance 2"]),
        new("a unit obeys its owner", """CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)""", ["advance 1", "order 0 move 300 0", "advance 4"]),
        new("leaving fires the leave trigger", """
            local t = CreateTrigger()
            TriggerRegisterPlayerEvent(t, Player(0), EVENT_PLAYER_LEAVE)
            TriggerAddAction(t, function() print("left") end)
            """, ["advance 1", "leave 0", "advance 1"]),
    ];

    // The C# calls for a step
    static void Do(Simulation simulation, string step)
    {
        var word = step.Split(' ');
        switch (word[0])
        {
            case "advance": simulation.Advance(TimeSpan.FromSeconds(double.Parse(word[1], CultureInfo.InvariantCulture))); break;
            case "say": simulation.Say(int.Parse(word[1]), string.Join(' ', word[2..])); break;
            case "key": simulation.Key(int.Parse(word[1]), int.Parse(word[2]), word[3] == "down"); break;
            case "leave": simulation.Leave(int.Parse(word[1])); break;
            case "order":
                simulation.Order(int.Parse(word[1]), simulation.Units(int.Parse(word[1]))[0].Id, word[2], float.Parse(word[3], CultureInfo.InvariantCulture), float.Parse(word[4], CultureInfo.InvariantCulture));
                break;
            default: throw new ArgumentException(step);
        }
    }

    // The Lua calls for the same step
    static string Lua(string step)
    {
        var word = step.Split(' ');
        return word[0] switch
        {
            "advance" => $"sim:advance({word[1]})",
            "say" => $"sim:say({word[1]}, \"{string.Join(' ', word[2..])}\")",
            "key" => $"sim:key({word[1]}, {word[2]}, {(word[3] == "down" ? "true" : "false")})",
            "leave" => $"sim:leave({word[1]})",
            "order" => $"sim:order({word[1]}, sim:units({word[1]})[1].id, \"{word[2]}\", {word[3]}, {word[4]})",
            _ => throw new ArgumentException(step),
        };
    }

    [InstallFact]
    public void The_csharp_and_lua_apis_run_each_shared_case_to_the_same_state()
    {
        foreach (var testCase in Cases)
        {
            var path = Map(testCase.Main);
            using var csharp = Simulation.Load(path);
            foreach (var step in testCase.Steps) Do(csharp, step);

            var test = Path.Combine(_folder, "case.test.lua");
            var body = new StringBuilder();
            body.Append($"test(\"{testCase.Name}\", function()\n  local sim = mirror.load([[{path.Replace('\\', '/')}]])\n");
            foreach (var step in testCase.Steps) body.Append("  ").Append(Lua(step)).Append('\n');
            body.Append($"  assert_eq(sim:hash(), [[{csharp.StateHash()}]])\n");
            body.Append($"  local log = sim:log()\n  assert_eq(#log, {csharp.Log.Count})\n");
            for (var i = 0; i < csharp.Log.Count; i++) body.Append($"  assert_eq(log[{i + 1}], [[{csharp.Log[i]}]])\n");
            body.Append("end)\n");
            File.WriteAllText(test, body.ToString());

            var (exit, output, error) = CoverageTests.Mirror("test", test);

            Assert.True(exit == 0, $"{testCase.Name}: {output}{error}");
        }
    }

    [InstallFact]
    public void Mirror_load_takes_the_difficulty_of_the_computers()
    {
        var path = Map("""print("difficulty", MeleeDifficulty())""").Replace('\\', '/');
        var test = Path.Combine(_folder, "difficulty.test.lua");
        File.WriteAllText(test, $$"""
            test("insane when said", function()
              assert_contains(mirror.load([[{{path}}]], {difficulty = "insane"}):log(), "difficulty\t2")
            end)
            test("normal unless said", function()
              assert_contains(mirror.load([[{{path}}]]):log(), "difficulty\t1")
            end)
            """);

        var (exit, output, error) = CoverageTests.Mirror("test", test);

        Assert.True(exit == 0, output + error);
    }

    [InstallFact]
    public void Mirror_test_prints_each_result_and_writes_junit()
    {
        var path = Map("""TimerStart(CreateTimer(), 2, false, function() print("done") end)""").Replace('\\', '/');
        File.WriteAllText(Path.Combine(_folder, "a.test.lua"), $$"""
            test("the timer prints", function()
              local sim = mirror.load([[{{path}}]])
              assert_eq(#sim:log(), 0)
              sim:advance(3)
              assert_contains(sim:log(), "done")
            end)
            test("a failing check says why", function()
              assert_eq(1 + 1, 3)
            end)
            test("an error in the test fails it", function()
              local x = nil
              return x.field
            end)
            test("wait_until finds the moment", function()
              local sim = mirror.load([[{{path}}]])
              assert_true(sim:wait_until(function() return #sim:log() > 0 end, 5))
              assert_close(sim:time(), 2, 0.2)
            end)
            """);
        var junit = Path.Combine(_folder, "out.xml");

        var (exit, output, _) = CoverageTests.Mirror("test", _folder, "--junit", junit);

        Assert.Equal(1, exit);
        Assert.Contains("PASS a.test.lua: the timer prints", output);
        Assert.Contains("FAIL a.test.lua: a failing check says why", output);
        Assert.Contains("expected 3, got 2", output);
        Assert.Contains("FAIL a.test.lua: an error in the test fails it", output);
        Assert.True(output.Contains("PASS a.test.lua: wait_until finds the moment"), output);
        Assert.Contains("2 passed, 2 failed", output);
        var xml = File.ReadAllText(junit);
        Assert.Contains("tests=\"4\" failures=\"2\"", xml);
        Assert.Contains("<failure message=", xml);
    }
}
