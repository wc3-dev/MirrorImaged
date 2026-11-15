using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>The settings, menu and bookkeeping natives: what a getter reads back, and the packing and name lookups.</summary>
public sealed class SettingsTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    List<string> Run(string main)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            function config() end
            function main()
            {{main}}
            end
            """), "", data: AbilityTests.Standard());
        return [.. simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..])];
    }

    [Fact]
    public void Colours_ids_and_names_convert()
    {
        var lines = Run("""
            print(BlzConvertColor(255, 1, 2, 3), BlzConvertColor(0, 255, 0, 255), AbilityId("AHhb") == FourCC("AHhb"), AbilityId("toolong"), AbilityId2String(FourCC("AHhb")))
            print(OrderId("move"), OrderId("MOVE"), OrderId("mvoe"), OrderId2String(851983), OrderId2String(1))
            print(BlzGetAnimName(ANIM_TYPE_STAND), BlzGetAnimName(ANIM_TYPE_ATTACK), BlzGetAnimName(ConvertAnimType(99)))
            """);

        Assert.Equal(["-16711165\t16711935\ttrue\t0\tAHhb", "851986\t851986\t0\tattack\tnil", "stand\tattack\t"], lines);
    }

    [Fact]
    public void Options_read_back_as_set_and_the_rest_has_defaults()
    {
        var lines = Run("""
            print(BlzIsSelectionEnabled(), BlzIsSelectionCircleEnabled(), BlzIsTargetIndicatorEnabled(), BlzGetHUDScale(), GetAllyColorFilterState(), GetCreepCampFilterState())
            BlzEnableSelections(false, false)
            BlzEnableTargetIndicator(false)
            BlzSetHUDScale(150)
            SetAllyColorFilterState(2)
            SetCreepCampFilterState(true)
            SetCustomCampaignButtonVisible(3, true)
            SetDefaultDifficulty(MAP_DIFFICULTY_HARD)
            SetCreatureDensity(MAP_DENSITY_HEAVY)
            SetResourceDensity(MAP_DENSITY_LIGHT)
            SetGameTypeSupported(GAME_TYPE_MELEE, true)
            SetPlayerTaxRate(Player(0), Player(1), PLAYER_STATE_RESOURCE_GOLD, 25)
            print(BlzIsSelectionEnabled(), BlzIsSelectionCircleEnabled(), BlzIsTargetIndicatorEnabled(), BlzGetHUDScale(), GetAllyColorFilterState(), GetCreepCampFilterState())
            print(GetCustomCampaignButtonVisible(3), GetCustomCampaignButtonVisible(4), GetHandleId(GetDefaultDifficulty()), GetHandleId(GetCreatureDensity()), GetHandleId(GetResourceDensity()))
            print(IsGameTypeSupported(GAME_TYPE_MELEE), IsGameTypeSupported(GAME_TYPE_FFA), GetPlayerTaxRate(Player(0), Player(1), PLAYER_STATE_RESOURCE_GOLD), GetPlayerTaxRate(Player(1), Player(0), PLAYER_STATE_RESOURCE_GOLD))
            print(SaveGameExists("x"), GetPlayerScore(Player(0), PLAYER_SCORE_UNITS_TRAINED), VersionSupported(VERSION_FROZEN_THRONE), GetHandleId(GetAIDifficulty(Player(1))))
            """);

        Assert.Equal([
            "true\ttrue\ttrue\t1.0\t0\tfalse",
            "false\tfalse\tfalse\t1.5\t2\ttrue",
            "true\tfalse\t2\t3\t1",
            "true\tfalse\t25\t0",
            "false\t0\ttrue\t1",
        ], lines);
    }

    [Fact]
    public void Units_are_made_by_name_and_cliffs_read_from_the_ground()
    {
        var lines = Run("""
            local u = CreateUnitAtLocByName(Player(0), "Footman", Location(10, 20), 0)
            print(GetUnitTypeId(u) == FourCC("hfoo"), GetUnitX(u), GetUnitY(u), CreateUnitAtLocByName(Player(0), "Nothing", Location(0, 0), 0))
            print(GetTerrainCliffLevel(0, 0) >= 0, GetTerrainCliffLevel(1e9, 0))
            ChangeLevel("next", false)
            """);

        Assert.Equal(["true\t10.0\t20.0\tnil", "true\t0"], lines);
    }
}
