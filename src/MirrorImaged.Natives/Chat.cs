using System.Security.Cryptography;

namespace MirrorImaged.Natives;

// Chat sent to the game (by a test) and the state hash of the run (decisions D358).
public sealed partial class GameNatives
{
    const int ChatEventId = 9096;
    readonly List<(player Player, string Text, bool Exact, @event Event)> _chat = [];
    (string Message, string Matched)? _chatNow;

    /// <summary>Registers for what the player says: a message that contains the text, or all of it when exact; an empty text matches every message.</summary>
    [Native(Level.Close, Fact = "game-facts.md, Version and launching: chat events (10-20)")]
    public @event? TriggerRegisterPlayerChatEvent(trigger? whichTrigger, player? whichPlayer, string? chatMessageToDetect, bool exactMatchOnly)
    {
        if (whichTrigger is null || whichPlayer is null) return null;
        var registered = Register(whichTrigger);
        _chat.Add((whichPlayer, chatMessageToDetect ?? "", exactMatchOnly, registered));
        return registered;
    }

    /// <summary>The whole message of the chat event.</summary>
    [Native(Level.Close, Fact = "game-facts.md, Version and launching: chat events (10-20)")]
    public string? GetEventPlayerChatString() => _chatNow?.Message ?? "";

    /// <summary>The text the registration looked for, as registered.</summary>
    [Native(Level.Close, Fact = "game-facts.md, Version and launching: chat events (10-20)")]
    public string? GetEventPlayerChatStringMatched() => _chatNow?.Matched ?? "";

    /// <summary>The player says the message: the chat registrations of the player it matches fire, in the order made.</summary>
    internal void Say(player speaker, string message)
    {
        foreach (var (player, text, exact, registered) in _chat.ToArray())
        {
            if (player != speaker || !(exact ? message == text : message.Contains(text, StringComparison.Ordinal))) continue;
            var outer = _chatNow;
            _chatNow = (message, text);
            try { Fire(registered, Convert<dialogevent>(ChatEventId), player: speaker); }
            finally { _chatNow = outer; }
        }
    }

    /// <summary>
    /// A hash of the world now: the time, each player's states and tech, and each unit's place, life, mana, owner, order and buffs, in the
    /// order of their ids, so two runs of one map with one seed and the same input give the same hash at every step.
    /// </summary>
    internal string Hash()
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        void Add(long value) => hash.AppendData(BitConverter.GetBytes(value));
        void AddF(float value) => hash.AppendData(BitConverter.GetBytes(value));
        Add(_game.Step);
        Add((long)_game.Counters.Random); Add(_game.Counters.NextId); Add(_game.Counters.NextKey);
        for (var n = 0; n < 28; n++)
            if (Player(n) is { } p)
            {
                foreach (var (state, value) in p.States.Select((value, state) => (state, value))) { Add(state); Add(value); }
                foreach (var (tech, level) in p.TechLevels.OrderBy(pair => pair.Key)) { Add(tech); Add(level); }
            }
        foreach (var u in _units.Values)
        {
            Add(u.Id); Add(u.Type.Id); Add(GetPlayerId(u.Owner)); Add(u.Order); Add(u.Dead ? 1 : 0); Add(u.Buffs.Count); Add(u.Train.Count);
            AddF(u.X); AddF(u.Y); AddF(u.Life); AddF(u.Mana);
        }
        // What the game also keeps in step between machines (game-facts, Desyncs): which triggers there are and are on, who the sync events
        // are registered for, and which frames there are. A run with none of them adds nothing, so its hash is as it was.
        foreach (var t in _triggers) { Add(t.Id); Add(t.Enabled ? 1 : 0); }
        foreach (var (player, prefix, _) in _syncEvents) { Add(player.Number); hash.AppendData(System.Text.Encoding.UTF8.GetBytes(prefix)); }
        foreach (var frame in _frames)
            if (!frame.Destroyed) Add(frame.Id);
        if (_loops > 0) Add(_loops);
        return System.Convert.ToHexString(hash.GetHashAndReset())[..16];
    }

    Dictionary<int, string>? _stateNames;

    // ForGroup and ForForce calls so far: the game runs them in step between machines, whatever the callback does (D742)
    long _loops;

    /// <summary>
    /// What <see cref="Hash"/> is made of, a value at a time: the part of the world it is in (the time, a player, a unit), what it is, and the value
    /// as text. Two runs whose hashes differ differ in one of these, which is what the desync check reports (D368, D508).
    /// </summary>
    internal IEnumerable<(string Part, string What, string Value)> Fields()
    {
        // Written the same whatever the machine's language: a Swedish one writes -5 with another minus sign
        static string Whole(long value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        static string Text(float value) => value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        static string Code(int id) => new([(char)(id >> 24 & 255), (char)(id >> 16 & 255), (char)(id >> 8 & 255), (char)(id & 255)]);
        yield return ("time", "step", $"{_game.Step}");
        // The random numbers drawn and the ids given out: in the game a draw in one player's game only, or a handle made there, makes the games differ
        yield return ("random numbers", "numbers drawn", $"{_game.Draws}");
        yield return ("random numbers", "generator", $"{_game.Counters.Random:X16}");
        yield return ("handle ids", "next handle id", $"{_game.Counters.NextId}");
        yield return ("handle ids", "handles made", $"{_game.Counters.NextKey}");
        _stateNames ??= Globals().Where(global => global.Value is playerstate).ToDictionary(global => ((handle)global.Value!).Id, global => global.Name);
        for (var n = 0; n < 28; n++)
            if (Player(n) is { } p)
            {
                for (var state = 0; state < p.States.Length; state++)
                    yield return ($"player {n}", _stateNames.GetValueOrDefault(state) ?? $"player state {state}", Whole(p.States[state]));
                foreach (var (tech, level) in p.TechLevels.OrderBy(pair => pair.Key)) yield return ($"player {n}", $"level of {Code(tech)}", Whole(level));
            }
        foreach (var u in _units.Values)
        {
            var part = $"unit {u.Id} ({GetUnitName(u)})";
            yield return (part, "type", Code(u.Type.Id));
            yield return (part, "owner", $"player {GetPlayerId(u.Owner)}");
            yield return (part, "order", Whole(u.Order));
            yield return (part, "dead", u.Dead ? "yes" : "no");
            yield return (part, "buffs", $"{u.Buffs.Count}");
            yield return (part, "units in training", $"{u.Train.Count}");
            yield return (part, "x", Text(u.X));
            yield return (part, "y", Text(u.Y));
            yield return (part, "life", Text(u.Life));
            yield return (part, "mana", Text(u.Mana));
        }
        foreach (var t in _triggers) yield return ($"trigger {t.Id}", "enabled", t.Enabled ? "yes" : "no");
        foreach (var ((player, prefix, _), n) in _syncEvents.Select((registered, n) => (registered, n)))
            yield return ("sync events", $"registration {n + 1}", $"player {player.Number}, prefix \"{prefix}\"");
        foreach (var frame in _frames)
            if (!frame.Destroyed) yield return ($"frame {frame.Id} ({frame.Name})", "frame", "there");
        if (_loops > 0) yield return ("group and force loops", "loops run", Whole(_loops));
    }

    /// <summary>
    /// What the world is now in parts, each named and hashed: the time, each player, each unit and the rest of <see cref="Fields"/>. Two runs that
    /// differ differ first in a part, which says where.
    /// </summary>
    internal IEnumerable<(string Name, string Hash)> HashParts() =>
        Fields().GroupBy(field => field.Part).Select(part =>
            (part.Key, System.Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join('\n', part.Select(field => $"{field.What}={field.Value}")))))[..12]));
}
