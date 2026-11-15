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
        return System.Convert.ToHexString(hash.GetHashAndReset())[..16];
    }

    /// <summary>
    /// What the world is now in parts, each named and hashed: the time, each player and each unit. Two runs that differ differ first in a part, which
    /// says where (the desync check, D368).
    /// </summary>
    internal IEnumerable<(string Name, string Hash)> HashParts()
    {
        string Of(Action<Action<long>, Action<float>> add)
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            add(value => hash.AppendData(BitConverter.GetBytes(value)), value => hash.AppendData(BitConverter.GetBytes(value)));
            return System.Convert.ToHexString(hash.GetHashAndReset())[..12];
        }
        yield return ("time", Of((add, _) => add(_game.Step)));
        // The random numbers drawn and the ids given out: in the game a draw in one player's game only, or a handle made there, makes the games differ
        yield return ("random numbers", Of((add, _) => add((long)_game.Counters.Random)));
        yield return ("handle ids", Of((add, _) => { add(_game.Counters.NextId); add(_game.Counters.NextKey); }));
        for (var n = 0; n < 28; n++)
            if (Player(n) is { } p)
                yield return ($"player {n}", Of((add, _) =>
                {
                    foreach (var (state, value) in p.States.Select((value, state) => (state, value))) { add(state); add(value); }
                    foreach (var (tech, level) in p.TechLevels.OrderBy(pair => pair.Key)) { add(tech); add(level); }
                }));
        foreach (var u in _units.Values)
            yield return ($"unit {u.Id} ({GetUnitName(u)})", Of((add, addF) =>
            {
                add(u.Type.Id); add(GetPlayerId(u.Owner)); add(u.Order); add(u.Dead ? 1 : 0); add(u.Buffs.Count); add(u.Train.Count);
                addF(u.X); addF(u.Y); addF(u.Life); addF(u.Mana);
            }));
    }
}
