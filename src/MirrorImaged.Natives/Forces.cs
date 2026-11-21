namespace MirrorImaged.Natives;

public partial class force
{
    internal int Players; // bit n: the player in slot n
    internal bool Looped; // ForForce has run over it
}

// Forces as the players and events probes measure them (facts, "Players and the game", "Player and game
// events"): ForForce goes in slot order, and the enumerations clear the force first and take the playing players of
// slots 0 to 23 that their filter passes, not the neutral ones, though those play too.
public sealed partial class GameNatives
{
    player? _enumPlayer, _filterPlayer;
    long _waitLost = -1; // the step a force ForForce ran over was destroyed in, until a wait is lost to it

    /// <summary>A new, empty force.</summary>
    /// <remarks>Close: read from common.j's docs, not measured; a force holds the players by number.</remarks>
    [Native(Level.Close)]
    public force? CreateForce() => Make(new force());

    /// <summary>
    /// Destroying a force that ForForce has run over makes the next wait started in the same step never end, as the
    /// forforce-wait probe and the facts' three probes saw; a wait started 1 s later, or one already running, ends.
    /// Why isn't known.
    /// </summary>
    /// <remarks>Close: the lost wait is as the probes saw it, and why the game does it isn't known.</remarks>
    [Native(Level.Close)]
    public void DestroyForce(force? whichForce)
    {
        if (whichForce is null) return;
        if (whichForce.Looped) _waitLost = _game.Step;
        _game.Remove(whichForce.Key);
    }

    /// <summary>Adds the player to the force; nothing for a null force or player.</summary>
    /// <remarks>Close: a null force or player is ignored, not measured in the game.</remarks>
    [Native(Level.Close)]
    public void ForceAddPlayer(force? whichForce, player? whichPlayer)
    {
        if (whichForce is not null && whichPlayer is not null) whichForce.Players |= 1 << whichPlayer.Number;
    }

    /// <summary>Takes the player out of the force; nothing for a null force or player.</summary>
    /// <remarks>Close: a null force or player is ignored, not measured in the game.</remarks>
    [Native(Level.Close)]
    public void ForceRemovePlayer(force? whichForce, player? whichPlayer)
    {
        if (whichForce is not null && whichPlayer is not null) whichForce.Players &= ~(1 << whichPlayer.Number);
    }

    /// <summary>Empties the force; nothing for a null force.</summary>
    /// <remarks>Close: a null force is ignored, not measured in the game.</remarks>
    [Native(Level.Close)]
    public void ForceClear(force? whichForce)
    {
        if (whichForce is not null) whichForce.Players = 0;
    }

    /// <summary>True when the player is in the force; false for a null player or force.</summary>
    /// <remarks>Close: a null player or force gives false, not measured in the game.</remarks>
    [Native(Level.Close)]
    public bool IsPlayerInForce(player? whichPlayer, force? whichForce) =>
        whichPlayer is not null && whichForce is not null && (whichForce.Players & (1 << whichPlayer.Number)) != 0;

    /// <summary>As IsPlayerInForce, with the force first.</summary>
    /// <remarks>Close: as IsPlayerInForce, not measured in the game.</remarks>
    [Native(Level.Close)]
    public bool BlzForceHasPlayer(force? whichForce, player? whichPlayer) => IsPlayerInForce(whichPlayer, whichForce);

    /// <summary>
    /// The playing players of slots 0 to 23 that the filter passes, as a trigger's condition passes, called for each in
    /// slot order with GetFilterPlayer set; a destroyed filter runs what its place holds (decisions D115).
    /// </summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Players and the game: the players probe; facts, Player and game events: probe events (11-08)).</remarks>
    [Native(Level.Exact, Fact = "facts, Players and the game: the players probe; " + Events)]
    public void ForceEnumPlayers(force? whichForce, boolexpr? filter) => Enumerate(whichForce, filter, _ => true);

    /// <summary>As ForceEnumPlayers, until it holds <paramref name="countLimit"/> players: the filter isn't called after.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Players and the game: the players probe; facts, Player and game events: probe events (11-08)).</remarks>
    [Native(Level.Exact, Fact = "facts, Players and the game: the players probe; " + Events)]
    public void ForceEnumPlayersCounted(force? whichForce, boolexpr? filter, int countLimit) => Enumerate(whichForce, filter, _ => true, countLimit);

    /// <summary>
    /// The players <paramref name="whichPlayer"/> is passive to, itself among them (not measured beyond itself), that the
    /// filter passes: it is called for every playing player, ally or not.
    /// </summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Players and the game: the players and lobby probes; facts, Player and game events: probe events (11-08)).</remarks>
    [Native(Level.Exact, Fact = "facts, Players and the game: the players and lobby probes; " + Events)]
    public void ForceEnumAllies(force? whichForce, player? whichPlayer, boolexpr? filter) =>
        Enumerate(whichForce, filter, p => IsPlayerAlly(whichPlayer, p));

    /// <summary>As ForceEnumAllies, for the players it isn't passive to.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Players and the game: the players and lobby probes; facts, Player and game events: probe events (11-08)).</remarks>
    [Native(Level.Exact, Fact = "facts, Players and the game: the players and lobby probes; " + Events)]
    public void ForceEnumEnemies(force? whichForce, player? whichPlayer, boolexpr? filter) =>
        Enumerate(whichForce, filter, p => IsPlayerEnemy(whichPlayer, p));

    void Enumerate(force? whichForce, boolexpr? filter, Func<player, bool> take, int count = int.MaxValue)
    {
        if (whichForce is null) return;
        whichForce.Players = 0;
        foreach (var p in _players.Take(24))
        {
            if (count <= 0) break;
            if (p.SlotState != Playing || !Filtered(p, filter) || !take(p)) continue;
            whichForce.Players |= 1 << p.Number;
            count--;
        }
    }

    bool Filtered(player p, boolexpr? filter)
    {
        if (filter is null) return true;
        var outer = _filterPlayer;
        _filterPlayer = p;
        try { return Holds(filter, alone: true); }
        finally { _filterPlayer = outer; }
    }

    /// <summary>The player an enumeration's filter is called for; nil outside one.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Player and game events: probe events (11-08)).</remarks>
    [Native(Level.Exact, Fact = Events)]
    public player? GetFilterPlayer() => _filterPlayer;

    /// <summary>
    /// Runs <paramref name="callback"/> for each player the force holds as it starts, in slot order (the players probe),
    /// with the enum player set; then lets go of it. Close: players added or removed by the callback don't change the
    /// run (not measured). Destroying the force afterwards loses the next wait (DestroyForce).
    /// </summary>
    /// <remarks>Close: players added or removed by the callback don't change the run, not measured in the game.</remarks>
    [Native(Level.Close)]
    public void ForForce(force? whichForce, Action? callback)
    {
        if (whichForce is not null) whichForce.Looped = true;
        var outer = _enumPlayer;
        try
        {
            for (int slot = 0, players = whichForce?.Players ?? 0; slot < _players.Length; slot++)
            {
                if ((players & (1 << slot)) == 0) continue;
                _enumPlayer = _players[slot];
                callback?.Invoke();
            }
        }
        finally
        {
            _enumPlayer = outer;
            _loops++;
            Release(callback);
        }
    }

    /// <summary>The player ForForce is running for; null outside it.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Players and the game: nil after ForForce).</remarks>
    [Native(Level.Exact, Fact = "facts, Players and the game: nil after ForForce")]
    public player? GetEnumPlayer() => _enumPlayer;
}
