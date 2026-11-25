using System.Text.RegularExpressions;
using static MirrorImaged.Tests.CoverageTests;

namespace MirrorImaged.Tests;

/// <summary>
/// Every command of the command line: its help, and each kind of map name it takes (a map file, a folder map and a map
/// named from the install), with <c>mirror check</c>, <c>mirror view</c> and stepping back (decisions D510 to D513).
/// </summary>
public sealed class CliCommandsTests : IDisposable
{
    const string Script = "function config() end\nfunction main() TimerStart(CreateTimer(), 1, true, function() print(GetRandomInt(1, 9)) end) end\n";

    readonly TestMaps _maps = new();
    readonly string _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;

    public void Dispose()
    {
        _maps.Dispose();
        Directory.Delete(_folder, true);
    }

    string Out(string name) => Path.Combine(_folder, name);

    // The commands the usage lists, each with its line: "run" and "mirror run <map> [--seconds <n>] ..."
    static Dictionary<string, string> Commands()
    {
        var (exit, output, error) = Mirror("--help");
        Assert.Equal((0, ""), (exit, error));
        return Regex.Matches(output, @"^  (mirror (\w+).*)$", RegexOptions.Multiline).ToDictionary(line => line.Groups[2].Value, line => line.Groups[1].Value.TrimEnd());
    }

    [Fact]
    public void Every_command_has_help_wherever_it_is_asked_for()
    {
        var commands = Commands();

        Assert.Equal(["bench", "check", "compare", "coverage", "desync", "fuzz", "replay", "run", "snapshot", "test", "view"], commands.Keys.Order(StringComparer.Ordinal));
        foreach (var (command, line) in commands)
            foreach (string[] asked in new string[][] { [command, "--help"], [command, "-h"], ["help", command], [command, "map.w3x", "--seconds", "1", "--help"] })
            {
                var (exit, output, error) = Mirror(asked);

                Assert.True(exit == 0 && error == "", $"mirror {string.Join(' ', asked)}: exit {exit}, {error}");
                Assert.StartsWith($"usage:\n  {line}\n      ", output);
                Assert.Single(Regex.Matches(output, @"^  mirror ", RegexOptions.Multiline));
            }
    }

    [Fact]
    public void Help_for_a_command_there_isnt_is_the_usage_as_an_error()
    {
        var (exit, output, error) = Mirror("conquer", "--help");

        Assert.Equal((2, ""), (exit, output));
        Assert.Contains("mirror coverage", error);
    }

    [Fact]
    public void Check_lists_the_natives_a_maps_script_calls_with_their_levels()
    {
        var (exit, output, error) = Mirror("check", _maps.Build(Script));

        Assert.Equal((0, ""), (exit, error));
        Assert.Matches(@"(^|\n)(Exact|Close|Recorded) +CreateTimer x1\n", output);
        Assert.Contains("GetRandomInt x1\n", output);
        Assert.Contains("TimerStart x1\n", output);
        Assert.EndsWith("3 natives called, 0 Missing\n", output);
    }

    [InstallFact]
    [Trait("Needs", "Game")]
    public void View_saves_a_page_of_the_run()
    {
        var page = Out("run.html");

        var (exit, output, error) = Mirror("view", _maps.Build(Script), "--seconds", "3", "--every", "1", "--out", page);

        Assert.Equal((0, ""), (exit, error));
        Assert.Matches(@"^\d+ frames of 3 s and 3 log lines saved to ", output);
        Assert.Contains("<html", File.ReadAllText(page));
    }

    // The plan's acceptance line A11: stepping back lands on the same state as running forward
    [InstallFact]
    [Trait("Needs", "Game")]
    public void Stepping_back_lands_on_the_state_running_forward_had()
    {
        var map = _maps.Build(Script);

        var forward = Mirror("snapshot", map, "--at", "7", "--seed", "5", "--out", Out("forward.json"));
        var back = Mirror("snapshot", map, "--at", "10", "--back", "3", "--seed", "5", "--out", Out("back.json"));
        var same = Mirror("compare", Out("back.json"));

        Assert.Equal((0, 0, 0), (forward.Exit, back.Exit, same.Exit));
        Assert.Matches(@"^Snapshot at step 7168 \(7 s\): [0-9A-F]{16},", forward.Output);
        Assert.Equal(forward.Output.Split(", saved to")[0], back.Output.Split(", saved to")[0]);
        Assert.Contains("Same world at step 7168", same.Output);
    }

    // Prints the computers' difficulty, then each thing player 0 says
    const string Chat = """
        function config() end
        function main()
            print("difficulty", GetHandleId(GetAIDifficulty(Player(0))))
            local t = CreateTrigger()
            TriggerRegisterPlayerChatEvent(t, Player(0), "", false)
            TriggerAddAction(t, function() print("said", GetEventPlayerChatString()) end)
        end
        """;

    [InstallFact]
    [Trait("Needs", "Game")]
    public void Replay_and_view_run_a_recording_at_its_difficulty_with_every_input()
    {
        var recording = Out("insane.json");
        File.WriteAllText(recording, new Recording
        {
            Map = _maps.Build(Chat), Difficulty = Difficulty.Insane, Steps = 2048,
            Inputs = [new(0, "say", 0, Text: "-a"), new(1024, "say", 0, Text: "-b"), new(1024, "say", 0, Text: "-c")],
        }.ToJson());
        var page = Out("insane.html");

        var replay = Mirror("replay", recording);
        var view = Mirror("view", recording, "--out", page);

        Assert.Equal((0, "", 0, ""), (replay.Exit, replay.Error, view.Exit, view.Error));
        Assert.Contains("print: difficulty\t2\n", replay.Output);
        Assert.Equal(["-a", "-b", "-c"], Regex.Matches(replay.Output, @"print: said\t(\S+)").Select(match => match.Groups[1].Value));
        // The page keeps the log as JSON, a tab written \t
        var html = File.ReadAllText(page);
        Assert.Contains(@"difficulty\t2", html);
        Assert.Equal(["-a", "-b", "-c"], Regex.Matches(html, @"said\\t(-\w)").Select(match => match.Groups[1].Value));
    }

    [InstallFact]
    [Trait("Needs", "Game")]
    public void Run_with_inputs_prints_the_hash_every_period_and_replay_prints_the_same()
    {
        var map = _maps.Build(Chat);
        var inputs = Out("inputs.json");
        File.WriteAllText(inputs, """[{"Step": 0, "Kind": "say", "Player": 0, "Text": "-a"}, {"Step": 1500, "Kind": "say", "Player": 0, "Text": "-b"}]""");

        var run = Mirror("run", map, "--seconds", "3", "--input", inputs, "--hash", "1", "--record", Out("run.json"));
        var replay = Mirror("replay", Out("run.json"), "--hash", "1");

        static string[] Hashes(string output) => [.. Regex.Matches(output, @"^hash .*$", RegexOptions.Multiline).Select(match => match.Value)];
        Assert.Equal((0, "", 0, ""), (run.Exit, run.Error, replay.Exit, replay.Error));
        Assert.Equal(["hash 1", "hash 2", "hash 3"], Hashes(run.Output).Select(line => line[..6]));
        Assert.Equal(Hashes(run.Output), Hashes(replay.Output));
        Assert.Equal(2, Regex.Matches(run.Output, "print: said").Count);
    }

    [InstallTheory]
    [Trait("Needs", "Game")]
    [InlineData("file")]
    [InlineData("folder")]
    [InlineData("install")]
    public void Every_command_that_takes_a_map_takes_each_kind_of_map_name(string kind)
    {
        var map = kind switch { "file" => _maps.Build(Script), "folder" => _maps.Unpack(_maps.Build(Script)), _ => "War3.w3mod:Maps/(4)Adrenaline.w3m" };
        string[][] lines =
        [
            ["check", map],
            ["run", map, "--seconds", "1", "--record", Out("run.json")],
            ["replay", Out("run.json")],
            ["replay", Out("run.json"), "--map", map],
            ["desync", map, "--seconds", "1"],
            ["snapshot", map, "--at", "1", "--out", Out("snapshot.json")],
            ["compare", Out("snapshot.json")],
            ["compare", Out("snapshot.json"), "--map", map],
            ["view", map, "--seconds", "1", "--out", Out("view.html")],
            ["bench", map, "--seconds", "1"],
            ["fuzz", map, "--seconds", "1"],
        ];

        Assert.Equal(Commands().Where(command => command.Value.Contains("<map")).Select(command => command.Key).Order(StringComparer.Ordinal), lines.Select(line => line[0]).Distinct().Order(StringComparer.Ordinal));
        foreach (var line in lines)
        {
            var (exit, _, error) = Mirror(line);

            Assert.True(exit == 0 && error == "", $"mirror {string.Join(' ', line)}: exit {exit}, {error}");
        }
    }
}
