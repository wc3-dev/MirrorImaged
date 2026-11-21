using MirrorImaged.Data;
using MirrorImaged.World;

namespace MirrorImaged.Natives;

public partial class player
{
    /// <summary>The player's slot, 0 to 27.</summary>
    public int Number { get; internal init; }

    internal string Name = "";
    internal int Color, Controller, SlotState, RacePreference, Race; // the values of common.j's Convert constants
    internal int Team = -1, StartLocation = -1;
    internal bool Selectable = true;
    internal bool Removed; // RemovePlayer has given it its result, and does nothing more to it
    internal float Handicap = 1, HandicapXP = 1, HandicapDamage = 1, HandicapReviveTime = 1;
    internal readonly int[] States = new int[26];
    internal readonly int[] Alliances = new int[28]; // bit n is alliancetype n towards that slot
    internal readonly Dictionary<int, int> TechMax = [], TechLevels = [];
}

// Players as a -loadfile game starts them (the players and lobby probes, facts, "Players and the game"): slots 0
// to 23 empty, unnamed, with no controller and a random race preference until the lobby seats them (Seat), the four
// neutral ones playing as creeps.
public sealed partial class GameNatives
{
    const int Empty = 0, Playing = 1, Left = 2, User = 0, Computer = 1, Creep = 4, NoController = 5, RandomRace = 32;
    const int GameResult = 0, Gold = 1, Lumber = 2, FoodCapCeiling = 6, GivesBounty = 7, Placed = 9;

    player _local;

    player[] MakePlayers()
    {
        var players = Enumerable.Range(0, 28).Select(number => Make(new player
        {
            Number = number, Name = number < 24 ? "" : $"Player {number + 1}", Color = number, RacePreference = RandomRace,
        })).ToArray();
        foreach (var p in players)
        {
            p.States[GameResult] = 3; // PLAYER_GAME_RESULT_NEUTRAL
            p.States[FoodCapCeiling] = 100;
            p.Alliances[27] = players[27].Alliances[p.Number] = 1; // neutral passive and everyone are passive to each other
            p.Alliances[p.Number] = 255; // types 0 to 7 towards itself
            (p.Controller, p.SlotState) = p.Number < 24 ? (NoController, Empty) : (Creep, Playing);
            if (p.Number >= 24) (p.Team, p.States[GivesBounty]) = (0, 1);
        }
        return players;
    }

    /// <summary>
    /// Seats the players as the lobby does once <c>config</c> has run (the players and lobby probes): people in
    /// <paramref name="users"/>, the first of them the local player, called "Local Player" when alone, and computers in
    /// <paramref name="computers"/>, each slot playing with its controller set, and the map's neutral and rescuable
    /// slots in <paramref name="others"/>, playing with the controller <c>config</c> gave them; the other slots of 0 to
    /// 23 are empty, on team -1, with their controller as <c>config</c> left it. A slot of 0 to 23 <c>config</c> named
    /// keeps its name; else a computer takes its slot's name in <paramref name="names"/> (<c>war3map.w3i</c>'s for the
    /// map's computer slots, "Player N" in another) when the map has custom forces with fixed settings and is
    /// "Computer (Normal)" otherwise, and every other slot is "Player N" (decisions D141, D210). A playing slot whose
    /// race is selectable gets the random preference, unless the map has custom forces with
    /// <paramref name="fixedSettings"/>, which keep every preference; a slot whose preference names a race has it, and
    /// the others get a random race, from a generator of their own seeded by the run's seed, drawn for every slot in
    /// order so that a slot's race and the map's own random numbers don't depend on who sits where. Custom forces with
    /// fixed settings also set <c>MAP_LOCK_ALLIANCE_CHANGES</c> and the flag 2^20, which <c>common.j</c> doesn't name.
    /// </summary>
    /// <exception cref="ArgumentException">As <see cref="CheckSeats"/>.</exception>
    public void Seat(IReadOnlyList<int> users, IReadOnlyList<int> computers, IReadOnlyList<int>? others = null,
        bool customForces = false, bool fixedSettings = false, IReadOnlyDictionary<int, string>? names = null)
    {
        others ??= [];
        CheckSeats(users, [.. computers, .. others]);
        var races = new Game(_game.RunSeed ^ 0x52616365); // 'Race'
        var fixedRaces = customForces && fixedSettings;
        foreach (var p in _players.Take(24))
        {
            var (user, computer) = (users.Contains(p.Number), computers.Contains(p.Number));
            var playing = user || computer || others.Contains(p.Number);
            if (user || computer) p.Controller = user ? User : Computer;
            p.SlotState = playing ? Playing : Empty;
            p.States[Placed] = playing ? 1 : 0;
            if (p.Name == "")
                p.Name = user && users.Count == 1 ? "Local Player"
                    : computer ? fixedRaces ? names?.GetValueOrDefault(p.Number) ?? $"Player {p.Number + 1}" : $"Computer ({(Difficulty == 0 ? "Easy" : Difficulty == 2 ? "Insane" : "Normal")})"
                    : $"Player {p.Number + 1}";
            if (!playing) p.Team = -1;
            if (playing && p.Selectable && !fixedRaces) p.RacePreference = RandomRace;
            var drawn = races.RandomInt(1, 4);
            p.Race = FixedRace(p.RacePreference) ?? drawn;
        }
        _local = _players[users.Concat(computers).Concat(others).First()];
        if (fixedRaces) _mapFlags |= 1024 | 1 << 20; // MAP_LOCK_ALLIANCE_CHANGES
    }

    /// <summary>Refuses a lineup with nobody playing, a slot outside 0 to 23, or a slot given twice.</summary>
    /// <param name="users">The slots people play.</param>
    /// <param name="others">The other slots that play.</param>
    /// <exception cref="ArgumentException">The lineup can't be seated; the message says why.</exception>
    public static void CheckSeats(IReadOnlyList<int> users, IReadOnlyList<int> others)
    {
        if (users.Count == 0 && others.Count == 0) throw new ArgumentException("A run needs someone playing, and no slot is given");
        var seated = users.Concat(others).ToList();
        foreach (var slot in seated)
            if ((uint)slot >= 24) throw new ArgumentException($"Slot {slot} can't be played: people and computers play slots 0 to 23");
        if (seated.GroupBy(slot => slot).FirstOrDefault(slot => slot.Count() > 1) is { } twice)
            throw new ArgumentException($"Slot {twice.Key} is given twice");
    }

    // RACE_PREF_HUMAN, ORC, NIGHTELF, UNDEAD and DEMON as RACE_*; null for random or none.
    static int? FixedRace(int preference) =>
        (preference & -preference) switch { 1 => 1, 2 => 2, 4 => 4, 8 => 3, 16 => 5, _ => null };

    /// <summary>The player in slot <paramref name="number"/>, 0 to 27; null for any other number, not measured.</summary>
    /// <remarks>Close: A number outside 0 to 27 giving null isn't measured in the game.</remarks>
    [Native(Level.Close)]
    public player? Player(int number) => (uint)number < _players.Length ? _players[number] : null;

    /// <summary>The player the run is played as: slot 0 until <see cref="Seat"/> says otherwise.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, C# through CSharp.lua: in single player, GetLocalPlayer inside Local.For is slot 0 (10-19)).</remarks>
    [Native(Level.Exact, Fact = "game-facts.md, C# through CSharp.lua: in single player, GetLocalPlayer inside Local.For is slot 0 (10-19)")]
    public player? GetLocalPlayer() => _local;

    /// <summary>The player's slot; 0 for null, not measured.</summary>
    /// <remarks>Close: A null player giving 0 isn't measured in the game.</remarks>
    [Native(Level.Close)]
    public int GetPlayerId(player? whichPlayer) => whichPlayer?.Number ?? 0;

    // The slots common.j and blizzard.j name: users and computers 0 to 23, then neutral aggressive, victim, extra and
    // passive. The four were read on the probe map (probe natives-test-run, 24 24 25 26 27); the game gave 12 for GetBJMaxPlayers on a map whose
    // info names editor version 6051 and 24 on one naming 6060 (facts, Version and launching), where the neutral slots of the old maps weren't
    // read, so a map of an older editor reads 24 and these slots here.
    const string SlotsReason = "The slots of a map built by editor version 6060 or later, as the probe map's; the game gave 12 for GetBJMaxPlayers on a map naming 6051 (where the neutral slots weren't read), and MirrorImaged answers these for every map (D791)";

    /// <summary>The number of slots for users and computers, 24.</summary>
    /// <remarks>Close: The slots of a map built by editor version 6060 or later, as the probe map's; the game gave 12 for GetBJMaxPlayers on a map naming 6051 (where the neutral slots weren't read), and MirrorImaged answers these for every map (D791).</remarks>
    [Native(Level.Close, Fact = TestRunProbe, Reason = SlotsReason)]
    public int GetBJMaxPlayers() => 24;

    /// <summary>The slot of neutral aggressive, 24.</summary>
    /// <remarks>Close: The slots of a map built by editor version 6060 or later, as the probe map's; the game gave 12 for GetBJMaxPlayers on a map naming 6051 (where the neutral slots weren't read), and MirrorImaged answers these for every map (D791).</remarks>
    [Native(Level.Close, Fact = TestRunProbe, Reason = SlotsReason)]
    public int GetPlayerNeutralAggressive() => 24;

    /// <summary>The slot of neutral victim, 25.</summary>
    /// <remarks>Close: The slots of a map built by editor version 6060 or later, as the probe map's; the game gave 12 for GetBJMaxPlayers on a map naming 6051 (where the neutral slots weren't read), and MirrorImaged answers these for every map (D791).</remarks>
    [Native(Level.Close, Fact = TestRunProbe, Reason = SlotsReason)]
    public int GetBJPlayerNeutralVictim() => 25;

    /// <summary>The slot of neutral extra, 26.</summary>
    /// <remarks>Close: The slots of a map built by editor version 6060 or later, as the probe map's; the game gave 12 for GetBJMaxPlayers on a map naming 6051 (where the neutral slots weren't read), and MirrorImaged answers these for every map (D791).</remarks>
    [Native(Level.Close, Fact = TestRunProbe, Reason = SlotsReason)]
    public int GetBJPlayerNeutralExtra() => 26;

    /// <summary>The slot of neutral passive, 27.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Orders and events: a new item belongs to neutral passive (slot 27) (10-21)).</remarks>
    [Native(Level.Exact, Fact = "game-facts.md, Orders and events: a new item belongs to neutral passive (slot 27) (10-21)")]
    public int GetPlayerNeutralPassive() => 27;

    /// <summary>The number of player slots, 28, the neutral ones included.</summary>
    /// <remarks>Close: The 28 slots are the ones MirrorImaged makes; not measured in the game.</remarks>
    [Native(Level.Close)]
    public int GetBJMaxPlayerSlots() => _players.Length;

    /// <summary>Text for <paramref name="toPlayer"/>, which goes in the run's log: <c>2.000 player 0: text</c>.</summary>
    /// <remarks>Recorded: Shows text on the player's screen; the run's log records it with the player.</remarks>
    [Native(Level.Recorded, Reason = "Shows text on the player's screen; the run's log records it with the player")]
    public void DisplayTextToPlayer(player? toPlayer, float x, float y, string? message) => Show(toPlayer, message);

    /// <summary>As <see cref="DisplayTextToPlayer"/>; the duration isn't kept.</summary>
    /// <remarks>Recorded: Shows text on the player's screen for a time; the run's log records it with the player.</remarks>
    [Native(Level.Recorded, Reason = "Shows text on the player's screen for a time; the run's log records it with the player")]
    public void DisplayTimedTextToPlayer(player? toPlayer, float x, float y, float duration, string? message) => Show(toPlayer, message);

    void Show(player? toPlayer, string? message)
    {
        if (toPlayer is not null) _game.Write($"player {toPlayer.Number}", message ?? "");
    }

    /// <summary>
    /// "" for slots 0 to 23 until the lobby seats them, then "Player N" for slot N - 1, "Local Player" for the one person
    /// of a single-player game, and for a computer its name in <c>war3map.w3i</c> with fixed player settings and
    /// "Computer (Normal)" without, as the players, lobby and data probes read them; a name <c>config</c> set is kept (not
    /// measured). Close: several people's names, their accounts', aren't known.
    /// </summary>
    /// <remarks>Close: A name <c>config</c> set being kept isn't measured, and with several people their accounts' names aren't known.</remarks>
    [Native(Level.Close)]
    public string? GetPlayerName(player? whichPlayer) => whichPlayer?.Name;

    /// <summary>Not measured: the game crashes lobby creation when it is called in <c>config</c> (facts), which this doesn't.</summary>
    /// <remarks>Close: The game crashes lobby creation when it is called in <c>config</c>, which this doesn't; elsewhere it isn't measured.</remarks>
    [Native(Level.Close)]
    public void SetPlayerName(player? whichPlayer, string? name)
    {
        if (whichPlayer is not null) whichPlayer.Name = name ?? "";
    }

    /// <summary>The player's colour; null for a null player.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Version and launching: GetPlayerColor compares equal to ConvertPlayerColor(n) (10-20)).</remarks>
    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: GetPlayerColor compares equal to ConvertPlayerColor(n) (10-20)")]
    public playercolor? GetPlayerColor(player? whichPlayer) => whichPlayer is null ? null : Convert<playercolor>(whichPlayer.Color);

    /// <summary>Close until units: the game leaves the player's units in their old colour (facts).</summary>
    /// <remarks>Close: Only the player's colour is kept; the game leaves the player's units in their old colour (facts), which isn't modelled yet.</remarks>
    [Native(Level.Close)]
    public void SetPlayerColor(player? whichPlayer, playercolor? color)
    {
        if (whichPlayer is not null && color is not null) whichPlayer.Color = color.Id;
    }

    /// <summary>The race the lobby gave the player (<see cref="Seat"/>).</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Version and launching: GetPlayerRace compares equal to RACE_* (10-20)).</remarks>
    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: GetPlayerRace compares equal to RACE_* (10-20)")]
    public race? GetPlayerRace(player? whichPlayer) => whichPlayer is null ? null : Convert<race>(whichPlayer.Race);

    /// <summary>Sets the player's race preference bits, which IsPlayerRacePrefSet reads; a null player or preference changes nothing.</summary>
    /// <remarks>Close: Not measured in the game; the race GetPlayerRace gives isn't changed by it.</remarks>
    [Native(Level.Close)]
    public void SetPlayerRacePreference(player? whichPlayer, racepreference? whichRacePreference)
    {
        if (whichPlayer is not null && whichRacePreference is not null) whichPlayer.RacePreference = whichRacePreference.Id;
    }

    /// <summary>Whether the player's race preference has that bit set; false for a null player or preference.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Version and launching: a selectable race reads RACE_PREF_RANDOM set (10-12)).</remarks>
    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: a selectable race reads RACE_PREF_RANDOM set (10-12)")]
    public bool IsPlayerRacePrefSet(player? whichPlayer, racepreference? pref) =>
        whichPlayer is not null && pref is not null && (whichPlayer.RacePreference & pref.Id) != 0;

    /// <summary>Sets whether the player's race is selectable, which Seat reads when it gives the preference; a null player changes nothing.</summary>
    /// <remarks>Close: Not measured in the game; only GetPlayerSelectable and Seat read it.</remarks>
    [Native(Level.Close)]
    public void SetPlayerRaceSelectable(player? whichPlayer, bool value)
    {
        if (whichPlayer is not null) whichPlayer.Selectable = value;
    }

    /// <summary>Whether the player's race is selectable; true until set (the players probe).</summary>
    /// <remarks>Close: A null player giving false isn't measured in the game.</remarks>
    [Native(Level.Close)]
    public bool GetPlayerSelectable(player? whichPlayer) => whichPlayer?.Selectable ?? false;

    /// <summary>Changes nothing; no race look is changed.</summary>
    /// <remarks>Recorded: Picks the look of the player's race; no native reads it back.</remarks>
    [Native(Level.Recorded, Reason = "Picks the look of the player's race; no native reads it back")]
    public void SetPlayerRaceSkin(player? whichPlayer, racepreference? whichRacePreference) { }

    /// <summary>The controller the lobby or SetPlayerController gave the player; null for a null player.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Input and sync: in single player player 1 is a user (10-20); facts, Players and the game).</remarks>
    [Native(Level.Exact, Fact = "game-facts.md, Input and sync: in single player player 1 is a user (10-20); facts, Players and the game")]
    public mapcontrol? GetPlayerController(player? whichPlayer) => whichPlayer is null ? null : Convert<mapcontrol>(whichPlayer.Controller);

    /// <summary>The controller a script reads; who plays is the lobby's (<see cref="Seat"/>), which sets it again for the seated slots.</summary>
    /// <remarks>Close: Not measured in the game; it only changes what GetPlayerController reads.</remarks>
    [Native(Level.Close)]
    public void SetPlayerController(player? whichPlayer, mapcontrol? controlType)
    {
        if (whichPlayer is not null && controlType is not null) whichPlayer.Controller = controlType.Id;
    }

    /// <summary>The slot's state, empty, playing or left; null for a null player.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Version and launching: RemovePlayer and CustomDefeatBJ leave the slot Left, victory Playing (11-06); facts, Players and the game).</remarks>
    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: RemovePlayer and CustomDefeatBJ leave the slot Left, victory Playing (11-06); facts, Players and the game")]
    public playerslotstate? GetPlayerSlotState(player? whichPlayer) => whichPlayer is null ? null : Convert<playerslotstate>(whichPlayer.SlotState);

    /// <summary>The player's team number; -1 for an unused slot, 0 for a null player.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Version and launching: an unused slot's team is -1 (10-20); facts, Players and the game).</remarks>
    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: an unused slot's team is -1 (10-20); facts, Players and the game")]
    public int GetPlayerTeam(player? whichPlayer) => whichPlayer?.Team ?? 0;

    /// <summary>Sets the team number and nothing else; a null player changes nothing.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Version and launching: SetPlayerTeam changes the team number only (11-06)).</remarks>
    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: SetPlayerTeam changes the team number only (11-06)")]
    public void SetPlayerTeam(player? whichPlayer, int whichTeam)
    {
        if (whichPlayer is not null) whichPlayer.Team = whichTeam;
    }

    /// <summary>The start location set; -1 until then. The lobby doesn't place players (not measured).</summary>
    /// <remarks>Close: The lobby's placement isn't simulated and a null player giving -1 isn't measured in the game.</remarks>
    [Native(Level.Close)]
    public int GetPlayerStartLocation(player? whichPlayer) => whichPlayer?.StartLocation ?? -1;

    /// <summary>Sets the start location that GetPlayerStartLocation reads; nothing is placed.</summary>
    /// <remarks>Close: Not measured in the game; only the number is kept.</remarks>
    [Native(Level.Close)]
    public void SetPlayerStartLocation(player? whichPlayer, int startLocIndex)
    {
        if (whichPlayer is not null) whichPlayer.StartLocation = startLocIndex;
    }

    /// <summary>As <see cref="SetPlayerStartLocation"/>; the lobby's placement, which it forces, isn't simulated.</summary>
    /// <remarks>Close: The lobby's forced placement isn't simulated, and what the game does isn't measured.</remarks>
    [Native(Level.Close)]
    public void ForcePlayerStartLocation(player? whichPlayer, int startLocIndex) => SetPlayerStartLocation(whichPlayer, startLocIndex);

    /// <summary>Whether the source player has that alliance setting towards the other; false for a null player or a setting outside 0 to 31.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Version and launching: alliances go one way (10-20); facts, Players and the game: the starting alliances).</remarks>
    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: alliances go one way (10-20); facts, Players and the game: the starting alliances")]
    public bool GetPlayerAlliance(player? sourcePlayer, player? otherPlayer, alliancetype? whichAllianceSetting) =>
        sourcePlayer is not null && otherPlayer is not null && whichAllianceSetting is { Id: >= 0 and < 32 }
        && (sourcePlayer.Alliances[otherPlayer.Number] & (1 << whichAllianceSetting.Id)) != 0;

    /// <summary>
    /// One way: the other player's alliance back is left as it was. A change fires the source player's alliance events
    /// inside the call (<see cref="AllianceChanged"/>); a player's alliance with itself doesn't change (measured for
    /// passive).
    /// </summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Version and launching: alliances go one way (10-20); facts, Player and game events: probe events (11-08)).</remarks>
    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: alliances go one way (10-20); " + Events)]
    public void SetPlayerAlliance(player? sourcePlayer, player? otherPlayer, alliancetype? whichAllianceSetting, bool value)
    {
        if (sourcePlayer is null || otherPlayer is null || sourcePlayer == otherPlayer || whichAllianceSetting is not { Id: >= 0 and < 32 }) return;
        var (was, bit) = (sourcePlayer.Alliances[otherPlayer.Number], 1 << whichAllianceSetting.Id);
        var now = value ? was | bit : was & ~bit;
        if (now == was) return;
        sourcePlayer.Alliances[otherPlayer.Number] = now;
        AllianceChanged(sourcePlayer, whichAllianceSetting.Id);
    }

    /// <summary>Whether the player is passive to the other.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Version and launching: IsPlayerAlly is true with ALLIANCE_PASSIVE alone, false with ALLIANCE_SHARED_VISION alone (10-20)).</remarks>
    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: IsPlayerAlly is true with ALLIANCE_PASSIVE alone, false with ALLIANCE_SHARED_VISION alone (10-20)")]
    public bool IsPlayerAlly(player? whichPlayer, player? otherPlayer) => GetPlayerAlliance(whichPlayer, otherPlayer, Convert<alliancetype>(0));

    /// <summary>True when the first player is not passive to the other; false for a null player.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Version and launching: IsPlayerEnemy is IsPlayerAlly's opposite (10-20)).</remarks>
    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: IsPlayerEnemy is IsPlayerAlly's opposite (10-20)")]
    public bool IsPlayerEnemy(player? whichPlayer, player? otherPlayer) =>
        whichPlayer is not null && otherPlayer is not null && !IsPlayerAlly(whichPlayer, otherPlayer);

    /// <summary>
    /// The state as the game starts it (the players probe: game result 3, food cap ceiling 100, placed 1 for a seated
    /// slot, bounty from neutral hostile and passive) or as set; the food used and made add the living units' (probe
    /// units-more, UnitCounts.cs).
    /// </summary>
    /// <remarks>Close: A state outside 0 to 25 and a null player giving 0 aren't measured in the game.</remarks>
    [Native(Level.Close)]
    public int GetPlayerState(player? whichPlayer, playerstate? whichPlayerState) =>
        whichPlayer is not null && whichPlayerState is { Id: >= 0 and < 26 } ? whichPlayer.States[whichPlayerState.Id] : 0;

    /// <summary>
    /// Gold below 0 is 0 (the players and events probes), and lumber too (inferred); the rest is kept as set. A change
    /// fires the player's state events whose comparison the new value meets, inside the call
    /// (<see cref="PlayerStateChanged"/>).
    /// </summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Players and the game: gold below 0 reads 0; facts, Player and game events: probe events (11-08)).</remarks>
    [Native(Level.Exact, Fact = "facts, Players and the game: gold below 0 reads 0; " + Events)]
    public void SetPlayerState(player? whichPlayer, playerstate? whichPlayerState, int value)
    {
        if (whichPlayer is null || whichPlayerState is not { Id: >= 0 and < 26 }) return;
        if (whichPlayerState.Id is Gold or Lumber) value = Math.Max(value, 0);
        if (whichPlayer.States[whichPlayerState.Id] == value) return;
        if (whichPlayerState.Id == NoCreepSleep) SettleSleep(whichPlayer);
        whichPlayer.States[whichPlayerState.Id] = value;
        PlayerStateChanged(whichPlayer, whichPlayerState, value);
    }

    /// <summary>Whether the player's observer state is above 0; false for a null player.</summary>
    /// <remarks>Close: Not measured in the game; read from the observer state that SetPlayerState keeps.</remarks>
    [Native(Level.Close)]
    public bool IsPlayerObserver(player? whichPlayer) => whichPlayer?.States[11] is > 0; // PLAYER_STATE_OBSERVER

    /// <summary>The handicap kept, 1 until set; 0 for a null player.</summary>
    /// <remarks>Close: Kept as set and not measured in the game; 0 for a null player isn't measured either.</remarks>
    [Native(Level.Close)] public float GetPlayerHandicap(player? whichPlayer) => whichPlayer?.Handicap ?? 0;
    /// <summary>The experience handicap kept, 1 until set; 0 for a null player.</summary>
    /// <remarks>Close: Kept as set and not measured in the game; 0 for a null player isn't measured either.</remarks>
    [Native(Level.Close)] public float GetPlayerHandicapXP(player? whichPlayer) => whichPlayer?.HandicapXP ?? 0;
    /// <summary>The damage handicap kept, 1 until set; 0 for a null player.</summary>
    /// <remarks>Close: Kept as set and not measured in the game; 0 for a null player isn't measured either.</remarks>
    [Native(Level.Close)] public float GetPlayerHandicapDamage(player? whichPlayer) => whichPlayer?.HandicapDamage ?? 0;
    /// <summary>The revive time handicap kept, 1 until set; 0 for a null player.</summary>
    /// <remarks>Close: Kept as set and not measured in the game; 0 for a null player isn't measured either.</remarks>
    [Native(Level.Close)] public float GetPlayerHandicapReviveTime(player? whichPlayer) => whichPlayer?.HandicapReviveTime ?? 0;

    /// <summary>Keeps the handicap for GetPlayerHandicap; nothing else uses it.</summary>
    /// <remarks>Close: Only kept for the getter; what the handicap does to the game isn't modelled or measured.</remarks>
    [Native(Level.Close)]
    public void SetPlayerHandicap(player? whichPlayer, float handicap)
    {
        if (whichPlayer is not null) whichPlayer.Handicap = handicap;
    }

    /// <summary>Keeps the experience handicap for GetPlayerHandicapXP; nothing else uses it.</summary>
    /// <remarks>Close: Only kept for the getter; what the handicap does to the game isn't modelled or measured.</remarks>
    [Native(Level.Close)]
    public void SetPlayerHandicapXP(player? whichPlayer, float handicap)
    {
        if (whichPlayer is not null) whichPlayer.HandicapXP = handicap;
    }

    /// <summary>Keeps the damage handicap for GetPlayerHandicapDamage; nothing else uses it.</summary>
    /// <remarks>Close: Only kept for the getter; what the handicap does to the game isn't modelled or measured.</remarks>
    [Native(Level.Close)]
    public void SetPlayerHandicapDamage(player? whichPlayer, float handicap)
    {
        if (whichPlayer is not null) whichPlayer.HandicapDamage = handicap;
    }

    /// <summary>Keeps the revive time handicap for GetPlayerHandicapReviveTime; nothing else uses it.</summary>
    /// <remarks>Close: Only kept for the getter; what the handicap does to the game isn't modelled or measured.</remarks>
    [Native(Level.Close)]
    public void SetPlayerHandicapReviveTime(player? whichPlayer, float handicap)
    {
        if (whichPlayer is not null) whichPlayer.HandicapReviveTime = handicap;
    }

    /// <summary>
    /// The limit set; for none or a negative one, an upgrade's level count, with the map's change (a new upgrade's is
    /// its standard base's), and 2147483647 for anything else.
    /// </summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Players and the game (Rhme 3, hfoo and zzzz 2147483647, -5 set) and The game's data (Rhme changed to 5, R000 made from it 3)).</remarks>
    [Native(Level.Exact, Fact = "facts, Players and the game (Rhme 3, hfoo and zzzz 2147483647, -5 set) and The game's data (Rhme changed to 5, R000 made from it 3)")]
    public int GetPlayerTechMaxAllowed(player? whichPlayer, int techid) =>
        whichPlayer is not null && whichPlayer.TechMax.TryGetValue(techid, out var max) && max >= 0 ? max : Levels(techid) ?? int.MaxValue;

    // An upgrade's level count; null for anything else.
    int? Levels(int techid) => Data.Object(ObjectKind.Upgrade, techid)?.Levels;

    /// <summary>Keeps the limit for the tech, which GetPlayerTechMaxAllowed reads; a negative one reads back as none set.</summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Version and launching: a limit of 0 and of 2147483647 read back (10-20); facts, Players and the game: a negative one reads 2147483647).</remarks>
    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: a limit of 0 and of 2147483647 read back (10-20); facts, Players and the game: a negative one reads 2147483647")]
    public void SetPlayerTechMaxAllowed(player? whichPlayer, int techid, int maximum)
    {
        if (whichPlayer is not null) whichPlayer.TechMax[techid] = maximum;
    }

    /// <summary>
    /// The level set, as the facts and the players probe read it; for a unit type, the player's living units of it, as
    /// GetPlayerTypedUnitCount counts them, with the types it upgrades to unless <paramref name="specificonly"/> (probe
    /// units-more: a town hall 1 and 3 with a keep and a castle).
    /// </summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Players and the game: probe players; facts, Units: probes units-more and units-more-2 (11-08)).</remarks>
    [Native(Level.Exact, Fact = "facts, Players and the game: probe players; " + MoreProbe)]
    public int GetPlayerTechCount(player? whichPlayer, int techid, bool specificonly) =>
        Data.Object(ObjectKind.Unit, techid) is not null ? Typed(whichPlayer, techid, !specificonly) : whichPlayer?.TechLevels.GetValueOrDefault(techid) ?? 0;

    /// <summary>Whether the player's tech count is above 0, counted as GetPlayerTechCount does.</summary>
    /// <remarks>Close: Not measured in the game; modelled as the tech count being above 0.</remarks>
    [Native(Level.Close)]
    public bool GetPlayerTechResearched(player? whichPlayer, int techid, bool specificonly) => GetPlayerTechCount(whichPlayer, techid, specificonly) > 0;

    /// <summary>
    /// Sets the level, also lower (the players probe), and not below 0 (not measured). Close: setting an upgrade above
    /// its level count isn't measured.
    /// </summary>
    /// <remarks>Close: A level above an upgrade's level count and a level below 0 held at 0 aren't measured in the game.</remarks>
    [Native(Level.Close)]
    public void SetPlayerTechResearched(player? whichPlayer, int techid, int setToLevel)
    {
        if (whichPlayer is null) return;
        whichPlayer.TechLevels[techid] = Math.Max(setToLevel, 0);
        Research(whichPlayer);
    }

    /// <summary>
    /// Adds levels, stopping an upgrade at its level count (the players and data probes) and anything else at
    /// 2147483647. Close: a count that already passes the level count isn't measured, nor a limit set below it.
    /// </summary>
    /// <remarks>Close: A count that already passes the level count isn't measured, nor a limit set below it.</remarks>
    [Native(Level.Close)]
    public void AddPlayerTechResearched(player? whichPlayer, int techid, int levels) =>
        SetPlayerTechResearched(whichPlayer, techid, (int)Math.Min((long)GetPlayerTechCount(whichPlayer, techid, true) + levels, Levels(techid) ?? int.MaxValue));

    /// <summary>Takes levels off the tech's count, as AddPlayerTechResearched with the number negated.</summary>
    /// <remarks>Close: Not measured in the game; modelled as adding the levels negated.</remarks>
    [Native(Level.Close)]
    public void BlzDecPlayerTechResearched(player? whichPlayer, int techid, int levels) => AddPlayerTechResearched(whichPlayer, techid, -levels);

    /// <summary>
    /// Takes the player out of the game, once: its <c>PLAYER_STATE_GAME_RESULT</c> becomes the result, victory keeps its
    /// slot Playing and any other result leaves it Left (tie not measured), its alliances stay, and a player that isn't a
    /// user fires its <c>EVENT_PLAYER_VICTORY</c> or <c>EVENT_PLAYER_DEFEAT</c> inside the call, after the change; a
    /// user fires neither, a neutral result nothing. A player already taken out is left as it is.
    /// </summary>
    /// <remarks>Exact: matches the game wherever measured (game-facts.md, Version and launching: RemovePlayer, CustomVictoryBJ and CustomDefeatBJ (11-06); facts, Player and game events: probe events (11-08)).</remarks>
    [Native(Level.Exact, Fact = "game-facts.md, Version and launching: RemovePlayer, CustomVictoryBJ and CustomDefeatBJ (11-06); " + Events)]
    public void RemovePlayer(player? whichPlayer, playergameresult? gameResult)
    {
        if (whichPlayer is not { Removed: false } || gameResult is null) return;
        whichPlayer.Removed = true;
        whichPlayer.States[GameResult] = gameResult.Id;
        if (gameResult.Id != 0) whichPlayer.SlotState = Left; // all but PLAYER_GAME_RESULT_VICTORY
        if (whichPlayer.Controller != User && gameResult.Id is 0 or 1) FirePlayerEvent(whichPlayer, gameResult.Id == 0 ? PlayerVictory : PlayerDefeat, whichPlayer);
    }
}
