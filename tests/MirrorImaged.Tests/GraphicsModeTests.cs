using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>
/// The graphics mode a run is in, Reforged unless given: the C# API's option, its recording, and the game camera each mode
/// gives (decisions D680, D685; the facts' "The game runner" and "Cameras").
/// </summary>
public sealed class GraphicsModeTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    const string FarAndZ = "print(GetCameraField(CAMERA_FIELD_FARZ), GetCameraField(CAMERA_FIELD_ZOFFSET))";

    string Map(string main) => _maps.Build($"function config() end\nfunction main()\n{main}\nend\n");

    [Theory]
    [InlineData(null, "5300.0\t10.0")]
    [InlineData(GraphicsMode.Reforged, "5300.0\t10.0")]
    [InlineData(GraphicsMode.Classic, "5000.0\t0.0")]
    public void The_game_camera_is_the_modes(GraphicsMode? graphics, string expected)
    {
        using var simulation = Simulation.Load(Map(FarAndZ), "", graphics is { } mode ? new SimulationOptions { Graphics = mode } : null);

        Assert.Equal(["0.000 print: " + expected], simulation.Log);
        // and a recording of the run keeps it, so that playing it again reads the same data
        Assert.Equal(graphics ?? GraphicsMode.Reforged, Recording.FromJson(simulation.Recording.ToJson()).Graphics);
    }

    [Fact]
    public void A_recording_made_before_the_option_is_in_Reforged_graphics() =>
        Assert.Equal(GraphicsMode.Reforged, Recording.FromJson("""{"Map": "x.w3x", "Seed": 1}""").Graphics);

    [Fact]
    public void Data_read_for_one_mode_is_refused_for_a_run_in_the_other()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            Simulation.Load(Map(""), "", new SimulationOptions { Graphics = GraphicsMode.Classic }, new StandardData()));

        Assert.StartsWith("The data was read for Reforged graphics and the run is in Classic graphics", error.Message);
    }
}
