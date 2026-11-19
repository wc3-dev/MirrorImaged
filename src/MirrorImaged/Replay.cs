using System.IO.Compression;
using System.Text;

namespace MirrorImaged;

/// <summary>A player of a replay's lobby.</summary>
/// <param name="Slot">The slot the player sits in, from 0.</param>
/// <param name="Id">The player number the replay's actions use, from 1; 0 for a computer.</param>
/// <param name="Name">The player's name; empty for a slot with none.</param>
/// <param name="Computer">Whether a computer plays the slot.</param>
/// <param name="Team">The team, from 0.</param>
/// <param name="Color">The colour, from 0.</param>
/// <param name="Race">The race flags the lobby holds.</param>
public sealed record ReplayPlayer(int Slot, int Id, string Name, bool Computer, int Team, int Color, int Race);

/// <summary>One thing a player did in a replay.</summary>
/// <param name="Millis">The time in milliseconds of the game it was done at.</param>
/// <param name="Player">The player number it was done by.</param>
/// <param name="Kind">
/// What it was: <c>order</c>, <c>select</c> (adding to the selection or taking from it), <c>group</c> (the selection kept under a number),
/// <c>pick</c> (a kept group selected), <c>subgroup</c> (the type of the selection that takes its orders), <c>ability</c> (a button of a unit
/// pressed, which the order follows), <c>item</c> (an item given to a unit or dropped), <c>dequeue</c> (taken out of a building's queue), <c>chat</c>,
/// <c>leave</c> or <c>other</c> (an action this reader does not know).
/// </param>
/// <param name="Code">
/// The order's id; 1 for adding to the selection and 2 for taking from it; the group's number; the subgroup's unit type; the ability; the
/// result the game gave a leaving player; the type taken out of a building's queue; or the replay's own number of an action that is not known.
/// </param>
/// <param name="X">The place of an order; not a number for an order with none.</param>
/// <param name="Y">The place of an order; not a number for an order with none.</param>
/// <param name="Objects">
/// The object an order targets (-1 for the ground), or those a selection, group, subgroup or ability names. The game names an object by two
/// numbers, here the first in the low half and the second, which it counts up as it makes objects, in the high half. An item action names the
/// unit it is given to (-1 for the ground) and the item; a dequeue the place in the queue, from 0.
/// </param>
/// <param name="Text">A chat message.</param>
/// <param name="Queued">Whether an order was given to be done after the unit's others (shift held).</param>
/// <param name="TargetType">The type of the object an order targets when the replay names it by its type (action 0x14); 0 otherwise.</param>
public sealed record ReplayAction(long Millis, int Player, string Kind, int Code, float X, float Y, IReadOnlyList<long> Objects, string? Text = null, bool Queued = false, int TargetType = 0);

/// <summary>
/// A replay (<c>.w3g</c>) read: its lobby, and what each player did, in time order. A replay holds the players' input only, so a map run with
/// it plays the game again as the game does when it shows a replay, as far as MirrorImaged models the game.
/// </summary>
public sealed class Replay
{
    /// <summary>The version of the game the replay was made with, as its build number.</summary>
    public int Build { get; private set; }

    /// <summary>The length of the game in milliseconds.</summary>
    public long DurationMillis { get; private set; }

    /// <summary>The name of the game.</summary>
    public string GameName { get; private set; } = "";

    /// <summary>The path of the map the game was played on, as the lobby held it; empty when it can't be read.</summary>
    public string Map { get; private set; } = "";

    /// <summary>The lobby's slots in order.</summary>
    public List<ReplayPlayer> Players { get; } = [];

    /// <summary>What the players did, in the order it was done.</summary>
    public List<ReplayAction> Actions { get; } = [];

    /// <summary>How many actions of each of the replay's own numbers the replay held, those this reader does not read among them.</summary>
    public SortedDictionary<int, int> ActionCounts { get; } = [];

    /// <summary>Reads a replay from its bytes.</summary>
    /// <exception cref="InvalidDataException">The bytes are not a replay.</exception>
    public static Replay Read(byte[] bytes)
    {
        var magic = "Warcraft III recorded game\u001a\0"u8;
        if (bytes.Length < 68 || !bytes.AsSpan(0, magic.Length).SequenceEqual(magic)) throw new InvalidDataException("Not a Warcraft III replay");
        var replay = new Replay();
        var headerSize = BitConverter.ToInt32(bytes, 28);
        var version = BitConverter.ToInt32(bytes, 36);
        if (version != 1 || headerSize != 68) throw new InvalidDataException($"A replay of header version {version} is not read");
        replay.Build = BitConverter.ToUInt16(bytes, 56);
        replay.DurationMillis = BitConverter.ToInt32(bytes, 60);
        var data = new MemoryStream();
        for (var offset = headerSize; offset + 12 <= bytes.Length;)
        {
            var compressed = BitConverter.ToInt32(bytes, offset);
            if (compressed <= 0 || offset + 12 + compressed > bytes.Length) break;
            // A block is a zlib stream that is flushed and not ended, so it is read to its end rather than to a final block
            using var block = new ZLibStream(new MemoryStream(bytes, offset + 12, compressed), CompressionMode.Decompress);
            try { block.CopyTo(data); }
            catch (InvalidDataException) { }
            offset += 12 + compressed;
        }
        replay.Parse(data.ToArray());
        return replay;
    }

    /// <summary>Reads the replay in the file.</summary>
    public static Replay Read(string path) => Read(File.ReadAllBytes(path));

    static string Text(byte[] data, ref int at)
    {
        var end = Array.IndexOf(data, (byte)0, at);
        if (end < 0) end = data.Length;
        var text = Encoding.UTF8.GetString(data, at, end - at);
        at = Math.Min(end + 1, data.Length);
        return text;
    }

    void Parse(byte[] data)
    {
        var at = 4;
        // The host, then the name of the game, an empty string, the encoded settings, and the counts
        PlayerRecord(data, ref at);
        GameName = Text(data, ref at);
        Text(data, ref at);
        Map = MapOf(data, ref at);
        at += 4 + 1 + 1 + 2 + 4;
        while (at < data.Length && data[at] == 0x16)
        {
            PlayerRecord(data, ref at);
            at += 4;
        }
        // The game since 1.32 puts blocks of its own here (the players' accounts and skins), each a kind, a length and that many bytes
        while (at + 6 <= data.Length && data[at] is 0x38 or 0x39) at += 6 + BitConverter.ToInt32(data, at + 2);
        if (at >= data.Length || data[at] != 0x19) throw new InvalidDataException($"The replay's lobby could not be read: block 0x{(at < data.Length ? data[at] : 0):X2} at {at}");
        var length = BitConverter.ToUInt16(data, at + 1);
        var slots = data[at + 3];
        for (var slot = 0; slot < slots; slot++)
        {
            var record = at + 4 + slot * 9;
            var id = data[record];
            var named = _names.GetValueOrDefault(id, "");
            Players.Add(new(slot, id, named, data[record + 3] != 0, data[record + 4], data[record + 5], data[record + 6]));
        }
        at += 3 + length;
        // Three small blocks, then the time slots
        while (at < data.Length && data[at] is 0x1A or 0x1B or 0x1C) at += 5;
        Blocks(data, at);
    }

    // The lobby's settings are written so that no byte is 0: a byte of flags, then seven bytes, each one more than it is unless its flag is set.
    // Decoded, the map's path follows 13 bytes of settings.
    static string MapOf(byte[] data, ref int at)
    {
        var decoded = new List<byte>();
        var mask = 0;
        for (var i = 0; at < data.Length && data[at] != 0; at++, i++)
        {
            if (i % 8 == 0) mask = data[at];
            else decoded.Add((byte)((mask >> i % 8 & 1) != 0 ? data[at] : data[at] - 1));
        }
        at = Math.Min(at + 1, data.Length);
        var end = decoded.Count > 13 ? decoded.IndexOf(0, 13) : -1;
        return end < 0 ? "" : Encoding.UTF8.GetString([.. decoded], 13, end - 13);
    }

    readonly Dictionary<int, string> _names = [];

    void PlayerRecord(byte[] data, ref int at)
    {
        var id = data[at + 1];
        at += 2;
        var name = Text(data, ref at);
        _names[id] = name;
        at += 1 + data[at];
    }

    void Blocks(byte[] data, int at)
    {
        long millis = 0;
        while (at < data.Length)
        {
            var kind = data[at];
            if (kind is 0x1E or 0x1F)
            {
                var length = BitConverter.ToUInt16(data, at + 1);
                if (at + 3 + length > data.Length) break;
                millis += BitConverter.ToUInt16(data, at + 3);
                Slot(data, at + 5, at + 3 + length, millis);
                at += 3 + length;
            }
            else if (kind == 0x20)
            {
                var length = BitConverter.ToUInt16(data, at + 2);
                if (at + 4 + length > data.Length) break;
                var player = data[at + 1];
                var flags = data[at + 4];
                var start = at + 5 + (flags == 0x20 ? 4 : 0);
                var text = Encoding.UTF8.GetString(data, start, Math.Max(Array.IndexOf(data, (byte)0, start, at + 4 + length - start) is var end and >= 0 ? end - start : at + 4 + length - start, 0));
                Actions.Add(new(millis, player, "chat", 0x20, 0, 0, [], text));
                at += 4 + length;
            }
            else if (kind == 0x17)
            {
                if (at + 14 > data.Length) break;
                Actions.Add(new(millis, data[at + 5], "leave", BitConverter.ToInt32(data, at + 6), 0, 0, []));
                at += 14;
            }
            else if (kind == 0x22) at += 2 + data[at + 1];
            else if (kind == 0x23) at += 11;
            else if (kind == 0x2F) at += 9;
            else if (kind == 0) break;
            else at += 1 + (at + 1 < data.Length ? data[at + 1] : 0);
        }
        DurationMillis = Math.Max(DurationMillis, millis);
    }

    // The command data of a time slot: for each player, a length and the actions
    void Slot(byte[] data, int at, int end, long millis)
    {
        while (at + 3 <= end)
        {
            var player = data[at];
            var length = BitConverter.ToUInt16(data, at + 1);
            var stop = Math.Min(at + 3 + length, end);
            Actions_(data, at + 3, stop, millis, player);
            at = stop;
        }
    }

    void Actions_(byte[] data, int at, int end, long millis, int player)
    {
        float Float(int i) => BitConverter.ToSingle(data, i);
        int Int(int i) => BitConverter.ToInt32(data, i);
        long Long(int i) => BitConverter.ToInt64(data, i);
        while (at < end)
        {
            var code = data[at];
            ActionCounts[code] = ActionCounts.GetValueOrDefault(code) + 1;
            int size;
            switch (code)
            {
                // An order: flags (1 is shift held), the order, 8 bytes, then its place and its target as its kind has them
                case 0x10 when at + 15 <= end:
                    Actions.Add(new(millis, player, "order", Int(at + 3), float.NaN, float.NaN, [], Queued: (data[at + 1] & 1) != 0));
                    size = 15;
                    break;
                case 0x11 when at + 23 <= end:
                    Actions.Add(new(millis, player, "order", Int(at + 3), Float(at + 15), Float(at + 19), [], Queued: (data[at + 1] & 1) != 0));
                    size = 23;
                    break;
                case 0x12 when at + 31 <= end:
                    Actions.Add(new(millis, player, "order", Int(at + 3), Float(at + 15), Float(at + 19), [Long(at + 23)], Queued: (data[at + 1] & 1) != 0));
                    size = 31;
                    break;
                // An item given to a unit or dropped: the order, its place, the unit it is given to (-1 for the ground) and the item
                case 0x13 when at + 39 <= end:
                    Actions.Add(new(millis, player, "item", Int(at + 3), Float(at + 15), Float(at + 19), [Long(at + 23), Long(at + 31)], Queued: (data[at + 1] & 1) != 0));
                    size = 39;
                    break;
                // An order at an object the replay names by its type, not its numbers (a tree, a mine or a shop under the fog): the order, its
                // place and the type
                case 0x14 when at + 44 <= end:
                    Actions.Add(new(millis, player, "order", Int(at + 3), Float(at + 15), Float(at + 19), [], Queued: (data[at + 1] & 1) != 0, TargetType: Int(at + 23)));
                    size = 44;
                    break;
                case 0x16 or 0x17 when at + 4 <= end:
                {
                    var count = BitConverter.ToUInt16(data, at + 2);
                    size = 4 + count * 8;
                    if (at + size > end) return;
                    Actions.Add(new(millis, player, code == 0x16 ? "select" : "group", data[at + 1], 0, 0, [.. Enumerable.Range(0, count).Select(i => Long(at + 4 + i * 8))]));
                    break;
                }
                case 0x18 when at + 3 <= end:
                    Actions.Add(new(millis, player, "pick", data[at + 1], 0, 0, []));
                    size = 3;
                    break;
                case 0x19 when at + 13 <= end:
                    Actions.Add(new(millis, player, "subgroup", Int(at + 1), 0, 0, [Long(at + 5)]));
                    size = 13;
                    break;
                case 0x1A: size = 1; break;
                case 0x1B or 0x1C: size = 10; break;
                case 0x1D: size = 9; break;
                // A unit or an upgrade taken out of a building's queue: its place in the queue and its type
                case 0x1E when at + 6 <= end:
                    Actions.Add(new(millis, player, "dequeue", Int(at + 2), 0, 0, [data[at + 1]]));
                    size = 6;
                    break;
                case 0x01 or 0x02 or 0x04 or 0x05 or 0x07 or 0x61 or 0x66 or 0x67 or 0x29 or 0x2A or 0x2B or 0x2C: size = 1; break;
                case 0x03: size = 2; break;
                case 0x06: size = 1 + Array.IndexOf(data, (byte)0, at + 1) - at; break;
                case 0x21: size = 9; break;
                case 0x50: size = 6; break;
                case 0x51: size = 10; break;
                case 0x62: size = 13; break;
                case 0x68: size = 13; break;
                case 0x69 or 0x6A or 0x7B: size = 17; break;
                // Since 2.0: a button of a unit pressed (the unit, the ability, the order), before the order it gives
                case 0x7A when at + 17 <= end:
                    Actions.Add(new(millis, player, "ability", Int(at + 9), 0, 0, [Long(at + 1)]));
                    size = 17;
                    break;
                // Since 2.0: data a ladder sends (two strings and four bytes)
                case 0x77: size = Array.IndexOf(data, (byte)0, Array.IndexOf(data, (byte)0, at + 1) + 1) + 5 - at; break;
                default:
                    // An action this reader cannot measure: the rest of the player's actions of this slot are left
                    Actions.Add(new(millis, player, "other", code, 0, 0, []));
                    return;
            }
            if (size <= 0 || at + size > end) return;
            at += size;
        }
    }
}
