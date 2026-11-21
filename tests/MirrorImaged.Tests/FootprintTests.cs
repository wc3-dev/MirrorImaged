using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>A pathing texture read as a footprint: the red of each pixel is a cell no unit walks on.</summary>
public sealed class FootprintTests
{
    // A 2 by 2 TGA, rows from the bottom: red at the bottom left, blue (unbuildable only) at the top right
    static byte[] Header(byte type, byte bits, byte descriptor) => [0, 0, type, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 2, 0, bits, descriptor];

    [Fact]
    public void An_uncompressed_texture_reads_its_red_cells_from_the_bottom()
    {
        byte[] pixels = [0, 0, 255, 0, 0, 0, 0, 0, 0, 255, 0, 0];
        var footprint = Footprint.Read([.. Header(2, 24, 0), .. pixels])!;

        Assert.Equal((2, 2), (footprint.Across, footprint.Up));
        Assert.Equal([true, false, false, false], new[] { footprint.Unwalkable(0, 0), footprint.Unwalkable(1, 0), footprint.Unwalkable(0, 1), footprint.Unwalkable(1, 1) });
        Assert.False(footprint.Unwalkable(-1, 0));
    }

    [Fact]
    public void A_run_length_texture_with_the_top_row_first_reads_the_same_cells()
    {
        // The top row (two blue-less blacks, then blue) first: a raw packet of the top row, then a run of the red pixel, then black
        byte[] packets = [1, 0, 0, 0, 255, 0, 0, 128, 0, 0, 255, 0, 0, 0, 0];
        var footprint = Footprint.Read([.. Header(10, 24, 32), .. packets])!;

        Assert.Equal([true, false, false, false], new[] { footprint.Unwalkable(0, 0), footprint.Unwalkable(1, 0), footprint.Unwalkable(0, 1), footprint.Unwalkable(1, 1) });
    }

    [Fact]
    public void A_file_that_is_no_texture_reads_none() => Assert.Null(Footprint.Read([1, 2, 3]));
}
