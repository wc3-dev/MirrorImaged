using System.Text;
using MirrorImaged.Data;

namespace MirrorImaged.Natives;

// The <id,column> tags of tooltips and ParseTags, filled as the facts' "Spells and abilities" and the probe data-tags read them.
public sealed partial class GameNatives
{
    const string TagFacts = "game-facts.md, Spells and abilities: tooltip tags and ParseTags (10-29, 11-08); MirrorImaged probe data-tags";

    /// <summary>
    /// Fills each <c>&lt;id,column&gt;</c> with the object's value in that column, the map's changes included, as a whole
    /// number cut toward zero; <c>,%</c> after the column shows it times 100 and <c>,.</c> one decimal, anything else
    /// there is ignored. An unknown object, an id not in the right case, a text column, a level the object lacks and a
    /// <c>-</c> cell give 0; <c>&lt;AHhb&gt;</c> gives "" and an unclosed tag drops the rest of the text. A unit reads only
    /// <c>HP</c>, <c>realHP</c> and the two attacks' <c>mindmg</c> and <c>maxdmg</c> (the facts: <c>spd</c>, <c>dmgplus1</c> and
    /// <c>cool1</c> read 0; why isn't separated, so Close).
    /// </summary>
    [Native(Level.Close, Fact = TagFacts)]
    public string? ParseTags(string? taggedString)
    {
        if (taggedString is null) return null;
        var text = new StringBuilder();
        for (var at = 0; at < taggedString.Length;)
        {
            var open = taggedString.IndexOf('<', at);
            if (open < 0)
            {
                text.Append(taggedString, at, taggedString.Length - at);
                break;
            }
            text.Append(taggedString, at, open - at);
            var close = taggedString.IndexOf('>', open + 1);
            if (close < 0) break;
            text.Append(Tag(taggedString[(open + 1)..close]));
            at = close + 1;
        }
        return text.ToString();
    }

    string Tag(string tag)
    {
        var parts = tag.Split(',', 3);
        if (parts.Length < 2) return "";
        var (id, rest) = (parts[0], parts.Length > 2 ? parts[2] : "");
        if (id.Length != 4 || Tagged(Code(id)) is not { } type) return "0";
        return Shown(TagValue(type, parts[1]), rest);
    }

    // The column's value for the object, 0 where it has none; a column ends in the level it reads
    float TagValue(GameObject type, string column)
    {
        var name = column.TrimEnd("0123456789".ToCharArray());
        var level = name.Length < column.Length && int.TryParse(column[name.Length..], out var digits) ? digits : 0;
        if (type.Kind == ObjectKind.Unit)
        {
            var low = name.ToLowerInvariant();
            if (low is "hp" or "realhp" && level == 0) return type.Real(Code("uhpm"));
            if (low is "mindmg" or "maxdmg" && level is 1 or 2)
            {
                float @base = type.Real(Code($"ua{level}b")), dice = type.Real(Code($"ua{level}d")), sides = type.Real(Code($"ua{level}s"));
                return low == "mindmg" ? @base + dice : @base + dice * sides;
            }
            return 0;
        }
        if (type.Kind == ObjectKind.Item && name.Equals("uses", StringComparison.OrdinalIgnoreCase)) return type.Real(Code("iuse"));
        return (Data.FieldAt(type.Kind, name) ?? Data.FieldAt(type.Kind, column)) is { } field ? type.Real(Data.Own(type, field), level) : 0;
    }

    GameObject? Tagged(int id) =>
        Data.Object(ObjectKind.Ability, id) ?? Data.Object(ObjectKind.Unit, id) ?? Data.Object(ObjectKind.Item, id) ?? Data.Object(ObjectKind.Upgrade, id);

    // A value as the tags show it: whole by default, times 100 for ",%", one decimal for ",." (whole values without one)
    static string Shown(float value, string rest)
    {
        if (rest.StartsWith('%')) return ((long)MathF.Truncate(value * 100)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (rest.StartsWith('.'))
        {
            var tenths = MathF.Truncate(value * 10) / 10;
            return tenths % 1 == 0 ? ((long)tenths).ToString(System.Globalization.CultureInfo.InvariantCulture) : tenths.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        }
        return ((long)MathF.Truncate(value)).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
