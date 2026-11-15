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
                local mine = CreateUnit(Player(PLAYER_NEUTRAL_PASSIVE), FourCC("ngol"), 600, 0, 0)
                SetResourceAmount(mine, 5000)
                TimerStart(CreateTimer(), 60, true, function()
                    local g = CreateGroup()
                    GroupEnumUnitsOfPlayer(g, Player(0), nil)
                    local names = {}
                    ForGroup(g, function() names[#names + 1] = GetUnitName(GetEnumUnit()) .. ":" .. GetUnitCurrentOrder(GetEnumUnit()) end)
                    print("status", GetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD), table.concat(names, ","))
                end)
            end
            """), "", data: data);
        simulation.Advance(TimeSpan.FromSeconds(125));

        Assert.Null(simulation.Stopped);
        Assert.DoesNotContain(simulation.Log, line => line.Contains("error", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(simulation.Log, line => line.EndsWith("started"));
        // The AI trained workers and sent them to the mine: its gold grew past what it began with
        var gold = simulation.Log.Where(line => line.Contains("status")).Select(line => int.Parse(line.Split('	')[1])).ToList();
        Assert.Equal(2, gold.Count);
        Assert.True(gold[1] > gold[0] && gold[1] > 500, string.Join(",", gold));
    }
}
