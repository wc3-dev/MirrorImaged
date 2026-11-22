using System.Text;
using System.Text.Unicode;

namespace MirrorImaged.Lua;

/// <summary>
/// A Lua string's bytes as a C# string and back, losing none (decisions D861): UTF-8 is read as its letters, and each
/// byte that isn't part of a UTF-8 letter as the lone surrogate U+DC80 + the byte, which no UTF-8 letter reads as and
/// which is written back as the byte.
/// </summary>
public static class LuaText
{
    /// <summary>The bytes as a string, a byte that isn't UTF-8 as U+DC80 + the byte.</summary>
    public static string Decode(ReadOnlySpan<byte> bytes)
    {
        if (Utf8.IsValid(bytes)) return Encoding.UTF8.GetString(bytes);
        var text = new StringBuilder(bytes.Length);
        while (!bytes.IsEmpty)
        {
            if (Rune.DecodeFromUtf8(bytes, out var rune, out var used) == System.Buffers.OperationStatus.Done)
                text.Append(rune.ToString());
            else
            {
                text.Append((char)(0xDC00 + bytes[0]));
                used = 1;
            }
            bytes = bytes[used..];
        }
        return text.ToString();
    }

    /// <summary>The string's bytes: UTF-8, with each lone U+DC80 to U+DCFF the byte it stands for.</summary>
    public static byte[] Encode(string text)
    {
        var bytes = new byte[ByteCount(text)];
        Write(text, bytes);
        return bytes;
    }

    /// <summary>How many bytes <see cref="Encode"/> gives.</summary>
    public static int ByteCount(string text) =>
        Escaped(text) ? Pieces(text).Sum(piece => piece.Byte is null ? Encoding.UTF8.GetByteCount(piece.Text) : 1) : Encoding.UTF8.GetByteCount(text);

    /// <summary>Writes <see cref="Encode"/>'s bytes into <paramref name="destination"/>; returns how many.</summary>
    public static int Write(string text, Span<byte> destination)
    {
        if (!Escaped(text)) return Encoding.UTF8.GetBytes(text, destination);
        var length = 0;
        foreach (var (piece, b) in Pieces(text))
            if (b is { } one) destination[length++] = one;
            else length += Encoding.UTF8.GetBytes(piece, destination[length..]);
        return length;
    }

    static bool Escaped(string text) => text.AsSpan().IndexOfAnyInRange('\uDC80', '\uDCFF') >= 0;

    // The string in runs of letters and single escaped bytes; a low surrogate after a high one is half a letter
    static IEnumerable<(string Text, byte? Byte)> Pieces(string text)
    {
        var start = 0;
        for (var i = 0; i < text.Length; i++)
            if (text[i] is >= '\uDC80' and <= '\uDCFF' && (i == 0 || !char.IsHighSurrogate(text[i - 1])))
            {
                if (i > start) yield return (text[start..i], null);
                yield return ("", (byte)(text[i] - 0xDC00));
                start = i + 1;
            }
        if (start < text.Length) yield return (text[start..], null);
    }
}
