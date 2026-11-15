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
/// <param name="Kind">What it was: <c>order</c>, <c>select</c>, <c>chat</c>, <c>leave</c> or <c>other</c>.</param>
/// <param name="Code">The replay's own number for the action, or the order's id.</param>
/// <param name="X">The place of an order, or 0.</param>
/// <param name="Y">The place of an order, or 0.</param>
/// <param name="Objects">The ids of the units an order targets, or a selection holds.</param>
/// <param name="Text">A chat message.</param>
public sealed record ReplayAction(long Millis, int Player, string Kind, int Code, float X, float Y, IReadOnlyList<int> Objects, string? Text = null);

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
        Text(data, ref at);
        at += 4 + 1 + 1 + 2 + 4;
        while (at < data.Length && data[at] == 0x16)
        {
            PlayerRecord(data, ref at);
            at += 4;
        }
        if (at >= data.Length || data[at] != 0x19) throw new InvalidDataException("The replay's lobby could not be read");
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
                Actions.Add(new(millis, data[at + 5], "leave", 0x17, 0, 0, []));
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
        while (at < end)
        {
            var code = data[at];
            ActionCounts[code] = ActionCounts.GetValueOrDefault(code) + 1;
            int size;
            switch (code)
            {
                case 0x10 when at + 15 <= end:
                    Actions.Add(new(millis, player, "order", Int(at + 3), 0, 0, []));
                    size = 15;
                    break;
                case 0x11 when at + 23 <= end:
                    Actions.Add(new(millis, player, "order", Int(at + 3), Float(at + 15), Float(at + 19), []));
                    size = 23;
                    break;
                case 0x12 when at + 31 <= end:
                    Actions.Add(new(millis, player, "order", Int(at + 3), Float(at + 15), Float(at + 19), [Int(at + 23)]));
                    size = 31;
                    break;
                case 0x13 when at + 39 <= end: size = 39; break;
                case 0x14 when at + 44 <= end: size = 44; break;
                case 0x16 or 0x17 when at + 4 <= end:
                {
                    var count = BitConverter.ToUInt16(data, at + 2);
                    size = 4 + count * 8;
                    if (at + size > end) return;
                    if (code == 0x16) Actions.Add(new(millis, player, "select", data[at + 1], 0, 0, [.. Enumerable.Range(0, count).Select(i => Int(at + 4 + i * 8))]));
                    break;
                }
                case 0x18: size = 3; break;
                case 0x19: size = 13; break;
                case 0x1A: size = 1; break;
                case 0x1B or 0x1C: size = 10; break;
                case 0x1D: size = 9; break;
                case 0x1E: size = 6; break;
                case 0x01 or 0x02 or 0x04 or 0x05 or 0x07 or 0x61 or 0x66 or 0x67 or 0x29 or 0x2A or 0x2B or 0x2C: size = 1; break;
                case 0x03: size = 2; break;
                case 0x06: size = 1 + Array.IndexOf(data, (byte)0, at + 1) - at; break;
                case 0x21: size = 9; break;
                case 0x50: size = 6; break;
                case 0x51: size = 10; break;
                case 0x62: size = 13; break;
                case 0x68: size = 13; break;
                case 0x69 or 0x6A: size = 17; break;
                default:
                    // An action this reader cannot measure: the rest of the player's actions of this slot are left
                    Actions.Add(new(millis, player, "other", code, 0, 0, []));
                    return;
            }
            if (size <= 0 || at + size > end) return;
            at += size;
        }
    }

    /// <summary>
    /// The replay as a <see cref="Recording"/> of the map: each player's selection, orders, chat and leaving at the step of its time, which
    /// <see cref="Simulation.Follow"/> runs. A unit is named by the id the replay gives it, which is the id a unit has here when the map made its
    /// units as the game did; an order of a unit that is not there, or of an order the game has no name for, is left out.
    /// </summary>
    public Recording ToRecording(string map, int seed = 0)
    {
        var recording = new Recording { Map = map, Seed = seed, Steps = (long)Math.Round(DurationMillis * 1024.0 / 1000) };
        var slotOf = Players.Where(player => player.Id > 0).ToDictionary(player => player.Id, player => player.Slot);
        var selected = new Dictionary<int, List<int>>();
        foreach (var action in Actions)
        {
            if (!slotOf.TryGetValue(action.Player, out var player)) continue;
            var step = (long)Math.Round(action.Millis * 1024.0 / 1000);
            switch (action.Kind)
            {
                case "select":
                    var set = selected.TryGetValue(player, out var current) ? current : selected[player] = [];
                    if (action.Code == 2) set.RemoveAll(action.Objects.Contains);
                    else set.AddRange(action.Objects.Where(id => !set.Contains(id)));
                    recording.Inputs.Add(new(step, "select", player, [.. set]));
                    break;
                case "order" when GameNaturalName(action.Code) is { } name:
                    foreach (var unit in selected.GetValueOrDefault(player) ?? [])
                        recording.Inputs.Add(new(step, "order", player, action.Objects.Count > 0 ? [unit, action.Objects[0]] : [unit], action.X != 0 || action.Y != 0 ? [action.X, action.Y] : null, name));
                    break;
                case "chat":
                    recording.Inputs.Add(new(step, "say", player, Text: action.Text));
                    break;
                case "leave":
                    recording.Inputs.Add(new(step, "leave", player));
                    break;
            }
        }
        return recording;
    }

    static string? GameNaturalName(int order) => Natives.GameNatives.NameOfOrder(order);
}
