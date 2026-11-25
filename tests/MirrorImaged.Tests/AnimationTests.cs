using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>The length of a unit's animations, read from its model in the install, and the hook that runs a native as Missing.</summary>
public sealed class AnimationTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    [InstallFact]
    [Trait("Needs", "Game")]
    public void A_footmans_animations_are_the_sequences_of_its_model()
    {
        using var simulation = Simulation.Load(_maps.Build("""
            function config() end
            function main()
                local u = CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
                print(BlzGetUnitAnimationDurationByIndex(u, 0), BlzGetUnitAnimationDuration(u, "attack"), BlzGetUnitAnimationDuration(u, "no such"), BlzGetUnitAnimationDurationByIndex(u, 9999))
            end
            """));

        Assert.EndsWith("print: 1.5\t1.0\t0.0\t0.0", simulation.Log[0]);
    }

    [InstallFact]
    [Trait("Needs", "Game")]
    public void Reforged_graphics_read_the_Definitive_Edition_model() => Footman(GraphicsMode.Reforged, "2.467	4.667	27");

    [InstallFact]
    [Trait("Needs", "Game")]
    public void Classic_graphics_read_the_classic_model() => Footman(GraphicsMode.Classic, "2.5	0.0	13");

    void Footman(GraphicsMode mode, string expected)
    {
        using var simulation = Simulation.Load(_maps.Build("""
            function config() end
            function main()
                local u = CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
                local count = 0
                while BlzGetUnitAnimationDurationByIndex(u, count) > 0 do count = count + 1 end
                print(BlzGetUnitAnimationDurationByIndex(u, 1), BlzGetUnitAnimationDurationByIndex(u, 13), count)
            end
            """), new SimulationOptions { Graphics = mode });

        Assert.EndsWith("print: " + expected, simulation.Log[0]);
    }

    [Fact]
    public void A_native_can_be_run_as_Missing_to_see_how_a_map_copes()
    {
        using var simulation = Simulation.Load(_maps.Build("""
            function config() end
            function main()
                print("before")
                GetRandomInt(1, 2)
                print("after")
            end
            """), "", new SimulationOptions { TreatAsMissing = ["GetRandomInt"] });

        Assert.Equal("GetRandomInt", Assert.IsType<MissingNativeException>(simulation.Stopped!.InnerException).Native);
        Assert.Equal(["0.000 print: before"], simulation.Log);
    }
}
