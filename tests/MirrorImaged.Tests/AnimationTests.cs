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

        Assert.Equal("GetRandomInt", Assert.IsType<Natives.MissingNativeException>(simulation.Stopped!.InnerException).Native);
        Assert.Equal(["0.000 print: before"], simulation.Log);
    }
}
