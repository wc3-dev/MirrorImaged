using System.Text;
using MirrorImaged.Data;

namespace MirrorImaged.Natives;

// How long a unit's animations last, read from the sequences of its model in the installed game (a model file's SEQS chunk: each a name and the
// time it starts and ends, in milliseconds). Close: classic graphics read the classic model, Reforged the Definitive Edition's, else the
// high-definition one's, else the classic (D687); a unit whose model isn't in the install has no animations (D377).
public sealed partial class GameNatives
{
    static readonly int ModelFile = Code("umdl");

    readonly Dictionary<string, IReadOnlyList<(string Name, double Seconds)>> _sequences = [];

    // The model file in the install, as the game's graphics read it: the classic one, or in Reforged the Definitive Edition's, else the high-definition
    // one's, else the classic (D687); null for a file that isn't there or isn't a model
    byte[]? InstalledModel(string file)
    {
        string[] layers = Data.Graphics == GraphicsMode.Classic ? [""] : ["_DE.w3mod:", "_HD.w3mod:", ""];
        return layers.Select(layer => Data.InstallFile("War3.w3mod:" + layer + file)).FirstOrDefault(found => found is not null) is { Length: > 8 } bytes
            && Encoding.ASCII.GetString(bytes, 0, 4) == "MDLX" ? bytes : null;
    }

    IReadOnlyList<(string Name, double Seconds)> Sequences(unit u)
    {
        var path = u.Type.Text(ModelFile);
        if (string.IsNullOrEmpty(path)) return [];
        var file = path.Replace('\\', '/').ToLowerInvariant() + (path.EndsWith(".mdx", StringComparison.OrdinalIgnoreCase) ? "" : ".mdx");
        var key = Data.Graphics + file;
        if (_sequences.TryGetValue(key, out var known)) return known;
        var list = new List<(string, double)>();
        if (InstalledModel(file) is { } bytes)
            for (var at = 4; at + 8 <= bytes.Length;)
            {
                var (tag, size) = (Encoding.ASCII.GetString(bytes, at, 4), BitConverter.ToInt32(bytes, at + 4));
                if (size < 0 || at + 8 + size > bytes.Length) break;
                if (tag == "SEQS")
                    for (var entry = at + 8; entry + 132 <= at + 8 + size; entry += 132)
                    {
                        var name = Encoding.ASCII.GetString(bytes, entry, 80).TrimEnd('\0');
                        list.Add((name, (BitConverter.ToInt32(bytes, entry + 84) - BitConverter.ToInt32(bytes, entry + 80)) / 1000.0));
                    }
                at += 8 + size;
            }
        return _sequences[key] = list;
    }

    /// <summary>How long the animation of the unit's model with this name lasts, in seconds: the first whose name begins with it, ignoring case; 0 for none.</summary>
    [Native(Level.Close)]
    public float BlzGetUnitAnimationDuration(unit? whichUnit, string? whichAnimation) =>
        whichUnit is null || whichAnimation is null ? 0 : (float)(Sequences(whichUnit).FirstOrDefault(s => s.Name.StartsWith(whichAnimation, StringComparison.OrdinalIgnoreCase)).Seconds);

    /// <summary>How long the unit model's animation of this number lasts, in seconds; 0 for a number the model has no animation of.</summary>
    [Native(Level.Close)]
    public float BlzGetUnitAnimationDurationByIndex(unit? whichUnit, int index) =>
        whichUnit is not null && Sequences(whichUnit) is var list && index >= 0 && index < list.Count ? (float)list[index].Seconds : 0;
}
