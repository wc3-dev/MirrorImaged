using System.Buffers.Binary;

namespace MirrorImaged.Data;

/// <summary>
/// A pathing texture as a building stamps it: a cell of 32 a pixel, <see cref="Across"/> by <see cref="Up"/>, and which cells
/// no unit walks on (the pixel's red; green is flying and blue building).
/// </summary>
public sealed class Footprint(int across, int up, bool[] unwalkable)
{
    /// <summary>The cells across.</summary>
    public int Across { get; } = across;

    /// <summary>The cells up.</summary>
    public int Up { get; } = up;

    /// <summary>Whether the cell <paramref name="column"/> from the left and <paramref name="row"/> from the bottom is unwalkable.</summary>
    public bool Unwalkable(int column, int row) => column >= 0 && row >= 0 && column < Across && row < Up && unwalkable[row * Across + column];

    /// <summary>A TGA's pixels (uncompressed or run-length, 24 or 32 bits); null for none or one that can't be read.</summary>
    public static Footprint? Read(byte[]? tga)
    {
        if (tga is not { Length: >= 18 } || tga[2] is not (2 or 10) || tga[16] is not (24 or 32)) return null;
        int across = BinaryPrimitives.ReadInt16LittleEndian(tga.AsSpan(12)), up = BinaryPrimitives.ReadInt16LittleEndian(tga.AsSpan(14)), size = tga[16] / 8;
        if (across <= 0 || up <= 0) return null;
        var pixels = new byte[across * up * size];
        var at = 18 + tga[0];
        if (tga[2] == 2)
        {
            if (tga.Length < at + pixels.Length) return null;
            tga.AsSpan(at, pixels.Length).CopyTo(pixels);
        }
        else
            for (var o = 0; o < pixels.Length;)
            {
                if (at >= tga.Length) return null;
                int head = tga[at++], n = Math.Min((head & 127) + 1, (pixels.Length - o) / size);
                var length = (head & 128) != 0 ? size : n * size;
                if (at + length > tga.Length) return null;
                for (var k = 0; k < ((head & 128) != 0 ? n : 1); k++) tga.AsSpan(at, length).CopyTo(pixels.AsSpan(o + k * size));
                at += length;
                o += n * size;
            }
        // Rows go up from the bottom unless the descriptor's bit 5 says the top comes first
        var top = (tga[17] & 32) != 0;
        var unwalkable = new bool[across * up];
        for (var row = 0; row < up; row++)
            for (var column = 0; column < across; column++)
                unwalkable[row * across + column] = pixels[((top ? up - 1 - row : row) * across + column) * size + 2] > 127;
        return new Footprint(across, up, unwalkable);
    }
}
