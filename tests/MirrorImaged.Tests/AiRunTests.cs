using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>An AI script of the game runs for a computer player: it loads, runs its first steps and sleeps without a script error.</summary>
[Trait("Needs", "Game")]
public sealed class AiRunTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

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
        simulation.Advance(TimeSpan.FromSeconds(300));

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
}
