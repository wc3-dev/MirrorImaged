using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>
/// ChooseRandomCreep and ChooseRandomNPBuilding, against the probe units-random-creeps (facts, "Items and pools"): of the units of
/// the creeps race that aren't buildings, special or heroes and are for the map's tileset (or all of them), those of the level asked
/// (-1 any, a unit with no level is level 0); of the neutral buildings the data marks random and for the tileset.
/// </summary>
public sealed class RandomCreepTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static StandardData Standard() => new StandardData()
        .Add(ObjectKind.Unit, "ngna", Values("unam=Gnoll Poacher;urac=creeps;ulev=1;util=L,F,W"))
        .Add(ObjectKind.Unit, "ngno", Values("unam=Gnoll;urac=creeps;ulev=1;util=L,F,W"))
        .Add(ObjectKind.Unit, "nmrl", Values("unam=Murloc Tiderunner;urac=creeps;ulev=1;util=F,W"))
        .Add(ObjectKind.Unit, "nogm", Values("unam=Ogre Mauler;urac=creeps;ulev=5;util=L,F,W"))
        .Add(ObjectKind.Unit, "nglm", Values("unam=Goblin Land Mine;urac=creeps;util=*"))
        .Add(ObjectKind.Unit, "nsw1", Values("unam=Lesser Spirit Beast;urac=creeps;ulev=2;uspe=1;util=*"))
        .Add(ObjectKind.Unit, "Nfir", Values("unam=Firelord;urac=creeps;ulev=5;util=*"))
        .Add(ObjectKind.Unit, "nban", Values("unam=Bandit;urac=creeps;ulev=1;util=A"))
        .Add(ObjectKind.Unit, "ndrr", Values("unam=Red Dragon Roost;urac=other;ubdg=1;unbr=1;util=L"))
        .Add(ObjectKind.Unit, "ndrk", Values("unam=Black Dragon Roost;urac=other;ubdg=1;unbr=1;util=F"))
        .Add(ObjectKind.Unit, "ngme", Values("unam=Goblin Merchant;urac=other;ubdg=1;unbr=1;util=*"))
        .Add(ObjectKind.Unit, "hbar", Values("unam=Barracks;urac=human;ubdg=1;util=L"));

    // The names of every type a pick gave in 200 draws, sorted
    List<string> Seen(string pick)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            function config() end
            function main()
                local seen = {}
                for _ = 1, 200 do
                    local n = {{pick}}
                    if n ~= 0 then seen[string.char(n >> 24, n >> 16 & 255, n >> 8 & 255, n & 255)] = true end
                end
                local names = {}
                for name in pairs(seen) do names[#names + 1] = name end
                table.sort(names)
                print(table.concat(names, " "))
            end
            """), "", data: Standard());
        return [.. simulation.Log[0][(simulation.Log[0].IndexOf("print: ") + 7)..].Split(' ', StringSplitOptions.RemoveEmptyEntries)];
    }

    [Theory]
    [InlineData(1, "ngna ngno")]
    [InlineData(0, "nglm")]
    [InlineData(5, "nogm")]
    [InlineData(2, "")]
    [InlineData(-2, "")]
    [InlineData(12, "")]
    [InlineData(-1, "nglm ngna ngno nogm")]
    public void A_creep_is_a_plain_creep_of_the_level_that_the_maps_tileset_has(int level, string expected)
    {
        Assert.Equal(expected.Split(' ', StringSplitOptions.RemoveEmptyEntries), Seen($"ChooseRandomCreep({level})"));
    }

    [Fact]
    public void A_neutral_building_is_one_the_data_marks_random_that_the_maps_tileset_has()
    {
        Assert.Equal(["ndrr", "ngme"], Seen("ChooseRandomNPBuilding()"));
    }
}
