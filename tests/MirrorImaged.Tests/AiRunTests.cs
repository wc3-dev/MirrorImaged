using War3Net.Build.Info;
using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>An AI script of the game runs for a computer player: it loads, runs its first steps and sleeps without a script error.</summary>
[Trait("Needs", "Game")]
public sealed class AiRunTests(Xunit.Abstractions.ITestOutputHelper output) : IDisposable
{
    readonly TestMaps _maps = new();
    readonly string _folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName())).FullName;

    public void Dispose()
    {
        _maps.Dispose();
        Directory.Delete(_folder, true);
    }

    [InstallFact]
    public void The_human_melee_ai_runs_for_a_computer_player_without_a_script_error()
    {
        var data = StandardData.Installed(GameInstall.Require(), null);
        using var simulation = Simulation.Load(_maps.Build("""
            function config() SetPlayers(2) end
            function main()
                SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, 500)
                CreateUnit(Player(0), FourCC("htow"), 0, 0, 0)
                CreateUnit(Player(0), FourCC("hpea"), 100, 0, 0)
                StartMeleeAI(Player(0), "human.ai")
                print("started")
                for i = 0, 11 do CreateDestructable(FourCC("LTlt"), -300 - (i % 4) * 64, 100 + (i // 4) * 64, 270, 1, 0) end
                local mine = CreateUnit(Player(PLAYER_NEUTRAL_PASSIVE), FourCC("ngol"), 600, 0, 0)
                SetResourceAmount(mine, 5000)
                TimerStart(CreateTimer(), 60, true, function()
                    local g = CreateGroup()
                    GroupEnumUnitsOfPlayer(g, Player(0), nil)
                    local names = {}
                    ForGroup(g, function() names[#names + 1] = GetUnitName(GetEnumUnit()) .. ":" .. GetUnitCurrentOrder(GetEnumUnit()) end)
                    print("status", GetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD), GetPlayerState(Player(0), PLAYER_STATE_RESOURCE_LUMBER), table.concat(names, ","))
                end)
            end
            """), "", data: data);
        simulation.Advance(TimeSpan.FromSeconds(600));

        Assert.Null(simulation.Stopped);
        Assert.DoesNotContain(simulation.Log, line => line.Contains("error", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(simulation.Log, line => line.EndsWith("started"));
        // The AI trained workers and sent them to the mine: its gold grew past what it began with
        var gold = simulation.Log.Where(line => line.Contains("status")).Select(line => int.Parse(line.Split('	')[1])).ToList();
        Assert.True(gold.Count >= 2);
        Assert.True(gold[1] > gold[0] && gold[1] > 500 || gold.Count > 2, string.Join(",", gold));
        // and built a base: an altar, a barracks and farms, then trained footmen
        var last = simulation.Log.Last(line => line.Contains("status"));
        foreach (var name in new[] { "Altar of Kings", "Barracks", "Farm", "Footman" }) Assert.Contains(name, last);
    }

    [InstallFact]
    public void The_human_melee_ai_sends_an_attack_wave_at_an_enemy_town()
    {
        var data = StandardData.Installed(GameInstall.Require(), null);
        using var simulation = Simulation.Load(_maps.Build("""
            function config() SetPlayers(2) end
            function main()
                SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, 5000)
                SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_LUMBER, 5000)
                SetPlayerAlliance(Player(0), Player(1), ALLIANCE_PASSIVE, false)
                SetPlayerAlliance(Player(1), Player(0), ALLIANCE_PASSIVE, false)
                CreateUnit(Player(0), FourCC("htow"), 0, 0, 0)
                for i = 0, 4 do CreateUnit(Player(0), FourCC("hpea"), 100, i * 40, 0) end
                CreateUnit(Player(1), FourCC("htow"), 2500, 0, 0)
                local enemy = CreateUnit(Player(1), FourCC("htow"), 2500, 1200, 0)
                StartMeleeAI(Player(0), "human.ai")
                for i = 0, 11 do CreateDestructable(FourCC("LTlt"), -300 - (i % 4) * 64, 100 + (i // 4) * 64, 270, 1, 0) end
                local mine = CreateUnit(Player(PLAYER_NEUTRAL_PASSIVE), FourCC("ngol"), 600, 0, 0)
                SetResourceAmount(mine, 50000)
                TimerStart(CreateTimer(), 120, true, function()
                    print("status", GetUnitState(enemy, UNIT_STATE_LIFE))
                end)
            end
            """), "", data: data);
        simulation.Advance(TimeSpan.FromSeconds(900));

        Assert.Null(simulation.Stopped);
        Assert.DoesNotContain(simulation.Log, line => line.Contains("error", StringComparison.OrdinalIgnoreCase));
        var life = simulation.Log.Where(line => line.Contains("status")).Select(line => float.Parse(line.Split('	')[1], System.Globalization.CultureInfo.InvariantCulture)).ToList();
        Assert.True(life[^1] < life[0], string.Join(",", life));
    }

    [InstallFact]
    public void The_human_melee_ai_has_its_hero_learn_skills_as_the_script_chooses()
    {
        var data = StandardData.Installed(GameInstall.Require(), null);
        using var simulation = Simulation.Load(_maps.Build("""
            function config() SetPlayers(2) end
            function main()
                SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, 3000)
                SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_LUMBER, 2000)
                CreateUnit(Player(0), FourCC("htow"), 0, 0, 0)
                for i = 0, 4 do CreateUnit(Player(0), FourCC("hpea"), 100, i * 40, 0) end
                StartMeleeAI(Player(0), "human.ai")
                for i = 0, 11 do CreateDestructable(FourCC("LTlt"), -300 - (i % 4) * 64, 100 + (i // 4) * 64, 270, 1, 0) end
                local mine = CreateUnit(Player(PLAYER_NEUTRAL_PASSIVE), FourCC("ngol"), 600, 0, 0)
                SetResourceAmount(mine, 50000)
                TimerStart(CreateTimer(), 240, false, function()
                    local g = CreateGroup()
                    GroupEnumUnitsOfPlayer(g, Player(0), nil)
                    ForGroup(g, function()
                        local u = GetEnumUnit()
                        if IsHeroUnitId(GetUnitTypeId(u)) then
                            local learned = 0
                            for _, a in ipairs({"AHhb", "AHds", "AHad", "AHre", "AHbz", "AHwe", "AHab", "AHmt", "AHtb", "AHtc", "AHbh", "AHav", "AHfs", "AHbn", "AHdr", "AHpx"}) do learned = learned + GetUnitAbilityLevel(u, FourCC(a)) end
                            print("hero", GetUnitName(u), GetHeroLevel(u), GetHeroSkillPoints(u), learned)
                        end
                    end)
                end)
            end
            """), "", data: data);
        simulation.Advance(TimeSpan.FromSeconds(250));

        Assert.Null(simulation.Stopped);
        var hero = Assert.Single(simulation.Log, line => line.Contains("hero"));
        var fields = hero.Split('	');
        Assert.Equal("0", fields[3]);
        Assert.True(int.Parse(fields[4]) >= 1, hero);
    }

    [InstallFact]
    public void An_ai_tells_its_allies_what_it_attacks_and_no_one_else()
    {
        const string Tells = """
            function main takes nothing returns nothing
                call SetAllianceTarget(GetEnemyBase())
                if GetAllianceTarget() == null then
                    call SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, 333)
                endif
                call Sleep(100)
            endfunction
            """;
        static string Hears(int player, string test, int gold) => $"""
            function main takes nothing returns nothing
                call Sleep(1)
                if GetAllianceTarget() {test} then
                    call SetPlayerState(Player({player}), PLAYER_STATE_RESOURCE_GOLD, {gold})
                endif
                call Sleep(100)
            endfunction
            """;
        var data = StandardData.Installed(GameInstall.Require(), null);
        using var simulation = Simulation.Load(_maps.Build("""
            function config() SetPlayers(3) end
            function main()
                SetPlayerAlliance(Player(0), Player(1), ALLIANCE_PASSIVE, true)
                SetPlayerAlliance(Player(1), Player(0), ALLIANCE_PASSIVE, true)
                for i = 0, 1 do
                    SetPlayerAlliance(Player(i), Player(2), ALLIANCE_PASSIVE, false)
                    SetPlayerAlliance(Player(2), Player(i), ALLIANCE_PASSIVE, false)
                end
                for i = 0, 2 do CreateUnit(Player(i), FourCC("htow"), -1500 + i * 1500, 0, 0) end
                StartMeleeAI(Player(0), "tells.ai")
                StartMeleeAI(Player(1), "ally.ai")
                StartMeleeAI(Player(2), "foe.ai")
                TimerStart(CreateTimer(), 3, false, function()
                    print("gold", GetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD), GetPlayerState(Player(1), PLAYER_STATE_RESOURCE_GOLD), GetPlayerState(Player(2), PLAYER_STATE_RESOURCE_GOLD))
                end)
            end
            """, files: new Dictionary<string, string> { ["tells.ai"] = Tells, ["ally.ai"] = Hears(1, "== GetEnemyBase()", 777), ["foe.ai"] = Hears(2, "== null", 555) }), "", data: data);
        simulation.Advance(TimeSpan.FromSeconds(4));

        Assert.Null(simulation.Stopped);
        // The one that tells doesn't hear itself, its ally hears the enemy's hall, the enemy hears nothing
        Assert.True(simulation.Log.Any(line => line.EndsWith("gold\t333\t777\t555")), string.Join("|", simulation.Log));
    }

    [InstallFact]
    public void A_paused_ai_script_waits_until_it_is_started_again()
    {
        const string Counts = """
            function main takes nothing returns nothing
                loop
                    call SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, GetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD) + 1)
                    call Sleep(1)
                endloop
            endfunction
            """;
        var data = StandardData.Installed(GameInstall.Require(), null);
        using var simulation = Simulation.Load(_maps.Build("""
            function config() end
            function main()
                StartMeleeAI(Player(0), "counts.ai")
                local function gold(label) print(label, GetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD)) end
                TimerStart(CreateTimer(), 3.5, false, function() PauseCompAI(Player(0), true) end)
                TimerStart(CreateTimer(), 4, false, function() gold("one") end)
                TimerStart(CreateTimer(), 8, false, function() gold("two") PauseCompAI(Player(0), false) end)
                TimerStart(CreateTimer(), 12, false, function() gold("three") end)
            end
            """, files: new Dictionary<string, string> { ["counts.ai"] = Counts }), "", data: data);
        simulation.Advance(TimeSpan.FromSeconds(13));

        Assert.Null(simulation.Stopped);
        var gold = simulation.Log.Select(line => line.Split('	')).Where(f => f.Length == 2).ToDictionary(f => f[0].Split(' ')[^1], f => int.Parse(f[1]));
        Assert.Equal(gold["one"], gold["two"]);
        Assert.True(gold["three"] >= gold["one"] + 3, string.Join("|", simulation.Log));
    }

    [InstallFact]
    public void An_obsidian_statue_of_the_game_becomes_a_destroyer()
    {
        var data = StandardData.Installed(GameInstall.Require(), null);
        using var simulation = Simulation.Load(_maps.Build("""
            function config() end
            function main()
                SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, 1000)
                SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_LUMBER, 1000)
                CreateUnit(Player(0), FourCC("uobs"), 0, 0, 0)
                print("made", ConvertUnits(1, FourCC("uobs")), GetUnitCount(FourCC("ubsp")), GetUnitCount(FourCC("uobs")))
            end
            """), "", data: data);

        Assert.True(simulation.Log.Any(line => line.EndsWith("made\ttrue\t1\t0")), string.Join("|", simulation.Log));
    }

    [InstallFact]
    public void A_reign_of_chaos_melee_map_runs_the_ai_scripts_of_its_balance_set()
    {
        const string Script = """
            function config() SetPlayers(2) end
            function main()
                CreateUnit(Player(0), FourCC("htow"), 0, 0, 0)
                StartMeleeAI(Player(0), "human.ai")
            end
            """;
        var data = StandardData.Installed(GameInstall.Require(), null);
        using var old = Simulation.Load(_maps.Build(Script, change: info => (info.FormatVersion, info.MapFlags) = (MapInfoFormatVersion.v18, info.MapFlags | MapFlags.MeleeMap)), "", data: data);
        using var frozen = Simulation.Load(_maps.Build(Script), "", data: data);
        using var own = Simulation.Load(_maps.Build(Script, files: new Dictionary<string, string> { ["human.ai"] = "function main takes nothing returns nothing\nendfunction" }), "", data: data);
        old.Advance(TimeSpan.FromSeconds(30));

        Assert.Equal("_balance/melee_v0.w3mod:Scripts/human.ai", old.AiScript("human.ai")?.From);
        Assert.Equal("Scripts/human.ai", frozen.AiScript("human.ai")?.From);
        Assert.Equal("map:human.ai", own.AiScript("human.ai")?.From);
        Assert.Null(old.Stopped);
        Assert.DoesNotContain(old.Log, line => line.Contains("error", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>A4 on one map: two computers on Normal play Echo Isles for 30 minutes; each trains an army, takes a second gold mine and loses units to the other.</summary>
    [InstallFact]
    public void Two_computers_train_expand_and_fight_on_echo_isles_for_30_minutes()
    {
        using var storage = CascStorage.Open(GameInstall.Require());
        var path = Path.Combine(_folder, "(2)EchoIsles.w3x");
        File.WriteAllBytes(path, storage.Read(storage.Names.First(name => name.EndsWith("/(2)EchoIsles.w3x", StringComparison.OrdinalIgnoreCase)))!);
        using var simulation = Simulation.Load(path, new SimulationOptions { Seed = 1, Users = [], Computers = [0, 1] });
        string[] halls = ["htow", "hkee", "hcas", "ogre", "ostr", "ofrt", "unpl", "unp1", "unp2", "etol", "etoa", "etoe"];
        int[] most = [0, 0], towns = [0, 0], lost = [0, 0];
        var creeps = simulation.Units(24).Count;
        var alive = new Dictionary<int, UnitView>();
        for (var minute = 1; minute <= 30; minute++)
        {
            simulation.Advance(TimeSpan.FromMinutes(1));
            var now = simulation.Units().Where(u => !u.Dead && u.Player < 2).ToDictionary(u => u.Id);
            foreach (var gone in alive.Values.Where(u => !now.ContainsKey(u.Id))) lost[gone.Player]++;
            alive = now;
            for (var p = 0; p < 2; p++)
            {
                most[p] = Math.Max(most[p], now.Values.Count(u => u.Player == p));
                towns[p] = Math.Max(towns[p], now.Values.Count(u => u.Player == p && halls.Contains(u.Type)));
            }
        }

        Assert.Null(simulation.Stopped);
        Assert.DoesNotContain(simulation.Log, line => line.Split(' ').ElementAtOrDefault(1) == "error:");
        var seen = $"most units {most[0]}/{most[1]}, most halls {towns[0]}/{towns[1]}, units lost {lost[0]}/{lost[1]}, creeps {creeps} to {simulation.Units(24).Count}";
        output.WriteLine(seen);
        Assert.True(most.All(n => n >= 40) && towns.All(n => n >= 2) && lost.All(n => n >= 5) && simulation.Units(24).Count < creeps, seen);
    }
}
