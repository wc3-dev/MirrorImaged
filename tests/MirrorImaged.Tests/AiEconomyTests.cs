using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>The economy natives of the AI: counts, costs, producing units, upgrades and sending workers to the mine.</summary>
public sealed class AiEconomyTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static StandardData Standard() => new StandardData()
        .Add(ObjectKind.Unit, "hpea", Values("unam=Peasant;uhpm=220;umvs=190;umvt=foot;utyp=peon;ucol=31;ugol=75;ufoo=1;ubld=15"))
        .Add(ObjectKind.Unit, "htow", Values("unam=Town Hall;uhpm=1500;ubdg=1;utyp=townhall;ucol=100;umvs=0;utra=hpea;ures=Rhme"))
        .Add(ObjectKind.Unit, "ngol", Values("unam=Gold Mine;ubdg=1;uhpm=2000;uabi=Agld;umvs=0;ucol=80"))
        .Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=420;ugol=135;ufoo=2;ubld=20;umvs=270"))
        .Add(ObjectKind.Ability, "Agld", Values("anam=Gold Mine;alev=1;Gld1:1=12500"))
        .Add(ObjectKind.Upgrade, "Rhme", Values("glvl=3;gglb=100;gglm=75;glmb=50;glmm=75;gtib=60;gtim=60"))
        .Add(ObjectKind.Destructable, "LTlt", Values("bhps=50;btar=tree"));

    // A block of unwalkable cells (32 from the world's edge, -2048) at x 560 to 1040, y -400 to 400, round the first tree
    static void Cliff(War3Net.Build.Map map)
    {
        var pathing = new War3Net.Build.Environment.MapPathingMap(War3Net.Build.Environment.MapPathingMapFormatVersion.v0) { Width = 128, Height = 128 };
        for (var i = 0; i < 128 * 128; i++) pathing.Cells.Add(0);
        for (var row = 51; row <= 76; row++)
            for (var column = 81; column <= 96; column++) pathing.Cells[row * 128 + column] = War3Net.Build.Environment.PathingType.Walk;
        map.PathingMap = pathing;
    }

    [Fact]
    public void The_ai_leaves_a_tree_its_workers_stopped_short_of_and_cuts_the_next()
    {
        using var simulation = Simulation.Load(_maps.Build("""
            function config() end
            function main()
                local p = Player(0)
                SetPlayerState(p, PLAYER_STATE_RESOURCE_LUMBER, 0)
                CreateUnit(p, FourCC("htow"), 0, 0, 0)
                CreateUnit(p, FourCC("hpea"), 100, 100, 0)
                CreateDestructable(FourCC("LTlt"), 800, 0, 0, 1, 0)
                CreateDestructable(FourCC("LTlt"), 0, -1500, 0, 1, 0)
                TimerStart(CreateTimer(), 2, true, function() HarvestWood(0, 1) end)
                TimerStart(CreateTimer(), 200, false, function() print(GetPlayerState(p, PLAYER_STATE_RESOURCE_LUMBER) > 0) end)
            end
            """, map: Cliff), "", data: Standard());
        simulation.Advance(TimeSpan.FromSeconds(201));

        Assert.Equal(["true"], simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..]).ToList());
    }

    [Fact]
    public void The_ai_sends_workers_to_a_tree_however_far_it_stands_when_the_ones_near_the_town_are_cut()
    {
        using var simulation = Simulation.Load(_maps.Build("""
            function config() end
            function main()
                local p = Player(0)
                CreateUnit(p, FourCC("htow"), 0, 0, 0)
                local a = CreateUnit(p, FourCC("hpea"), 100, 100, 0)
                CreateDestructable(FourCC("LTlt"), 4000, 0, 0, 1, 0)
                HarvestWood(0, 1)
                print(GetUnitCurrentOrder(a) == 852018)
            end
            """), "", data: Standard());

        Assert.Equal(["true"], simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..]).ToList());
    }

    [Fact]
    public void The_ai_counts_costs_produces_upgrades_and_sends_workers_to_the_mine()
    {
        using var simulation = Simulation.Load(_maps.Build("""
            function config() end
            function main()
                local p = Player(0)
                SetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD, 1000)
                SetPlayerState(p, PLAYER_STATE_RESOURCE_LUMBER, 500)
                SetPlayerState(p, PLAYER_STATE_RESOURCE_FOOD_CAP, 10)
                local hall = CreateUnit(p, FourCC("htow"), 0, 0, 0)
                local mine = CreateUnit(Player(PLAYER_NEUTRAL_PASSIVE), FourCC("ngol"), 500, 0, 0)
                SetResourceAmount(mine, 100)
                local a, b = CreateUnit(p, FourCC("hpea"), 100, 100, 0), CreateUnit(p, FourCC("hpea"), 100, -100, 0)
                print(GetAiPlayer(), GetUnitCount(FourCC("hpea")), GetUnitCountDone(FourCC("hpea")), GetGoldOwned(), GetUnitGoldCost(FourCC("hfoo")), GetUnitBuildTime(FourCC("hfoo")), GetUpgradeGoldCost(FourCC("Rhme")), GetUpgradeLevel(FourCC("Rhme")))
                print(TownHasHall(0), TownHasHall(1), TownHasMine(0), GetMinesOwned(), TownWithMine(), GetBuilding(p) == hall, GetTownUnitCount(FourCC("hpea"), 0, true))
                print(SetProduce(2, FourCC("hpea"), 0), GetUnitCount(FourCC("hpea")), GetUnitCountDone(FourCC("hpea")), GetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD), SetProduce(0, FourCC("hpea"), 0), SetUpgrade(FourCC("Rhme")), GetPlayerState(p, PLAYER_STATE_RESOURCE_GOLD))
                HarvestGold(0, 5)
                print(GetUnitCurrentOrder(a) == 852018, GetUnitCurrentOrder(b) == 852018)
            end
            """), "", data: Standard());
        var lines = simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..]).ToList();

        Assert.Equal([
            "0\t2\t2\t100\t135\t20\t100\t0",
            "true\tfalse\ttrue\t1\t0\ttrue\t2",
            "true\t4\t2\t850\ttrue\ttrue\t750",
            "true\ttrue",
        ], lines);
    }
}
