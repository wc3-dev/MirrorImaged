using War3Net.Build.Common;
using War3Net.Build.Info;

namespace MirrorImaged.Tests;

/// <summary>
/// A8: the desync check finds each pattern the facts' "Desyncs" section and Moonwell's analyzer rules name that this runtime can show, says
/// the step, the value in each player's game and the Lua stack of the call that made it, and does not cry wolf at what only one player's
/// screen shows. The patterns it can't show are listed in the guide (recordings.md, "What the check does not cover").
/// </summary>
[Trait("Needs", "Game")]
public sealed class DesyncSuiteTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    // At 2 s, `local` runs in player 1's game only and `everyone` in every game; `setup` runs in main
    string Map(string local = "", string everyone = "", string setup = "") => _maps.Build($$"""
        function config() SetPlayers(2) end
        function main()
            {{setup.Replace('\'', '"')}}
            TimerStart(CreateTimer(), 2, false, function()
                if GetLocalPlayer() == Player(1) then
                    {{local.Replace('\'', '"')}}
                end
                {{everyone.Replace('\'', '"')}}
            end)
            TimerStart(CreateTimer(), 4, false, function() SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, GetRandomInt(1, 1000)) end)
        end
        """, change: info =>
    {
        info.Players.Clear();
        info.Players.AddRange([Slot(0), Slot(1)]);
    });

    static PlayerData Slot(int id) => new() { Id = id, Controller = PlayerController.User, Race = PlayerRace.Human, Name = $"Player {id + 1}" };

    const string Trigger = "T = CreateTrigger()", Frame = "F = BlzCreateFrame('EscMenuBackdrop', BlzGetOriginFrame(ORIGIN_FRAME_GAME_UI, 0), 0, 0)";

    // What each pattern is, the part of the world where the games first differ, and the native whose call made them differ
    [InstallTheory]
    [InlineData("SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, 5)", "player 0", "SetPlayerState")]
    [InlineData("CreateUnit(Player(1), FourCC('hfoo'), 0, 0, 0)", "handle ids", "CreateUnit")]
    [InlineData("local n = GetRandomInt(1, 10)", "random numbers", "GetRandomInt")]
    [InlineData("local r = GetRandomReal(0, 1)", "random numbers", "GetRandomReal")]
    [InlineData("local r = math.random()", "random numbers", "math.random")]
    [InlineData("local g = CreateGroup()", "handle ids", "CreateGroup")]
    [InlineData("local g = CreateGroup() GroupEnumUnitsInRange(g, 0, 0, 500, nil) ForGroup(g, function() end)", "handle ids", "CreateGroup")]
    [InlineData("local l = Location(1, 2)", "handle ids", "Location")]
    [InlineData("local t = CreateTimer()", "handle ids", "CreateTimer")]
    [InlineData("local f = BlzCreateFrame('EscMenuBackdrop', BlzGetOriginFrame(ORIGIN_FRAME_GAME_UI, 0), 0, 0)", "handle ids", "BlzGetOriginFrame")]
    [InlineData("BlzTriggerRegisterPlayerSyncEvent(T, Player(1), 'p', false)", "handle ids", "BlzTriggerRegisterPlayerSyncEvent", Trigger)]
    [InlineData("DisableTrigger(T)", "trigger ", "DisableTrigger", Trigger)]
    [InlineData("DestroyTrigger(T)", "trigger ", "DestroyTrigger", Trigger)]
    [InlineData("BlzDestroyFrame(F)", "frame ", "BlzDestroyFrame", Frame)]
    public void A_pattern_that_changes_one_players_game_is_found_where_it_differs(string local, string part, string native, string setup = "")
    {
        var report = Desync.Check(Map(local, setup: setup), TimeSpan.FromSeconds(8));

        Assert.NotNull(report);
        Assert.StartsWith(part, report.Part);
        Assert.Equal(2 * 1024, report.Step);
        Assert.Equal([0, 1], report.Players);
        Assert.NotEqual(report.Values[0], report.Values[1]);
        // Only player 1's game made the call: its stack names the native, then the line of the map's script that called it
        Assert.Null(report.Stacks[0]);
        Assert.Contains($"'{native}'", report.Stacks[1]);
        Assert.Contains("war3map.lua:", report.Stacks[1]);
    }

    // The value in each player's game, by name
    [InstallFact]
    public void The_report_says_what_the_value_was_in_each_players_game()
    {
        var report = Desync.Check(Map("SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, 5)"), TimeSpan.FromSeconds(8))!;

        Assert.Equal(("player 0", "PLAYER_STATE_RESOURCE_GOLD"), (report.Part, report.What));
        Assert.Equal(["0", "5"], report.Values);

        report = Desync.Check(Map("DisableTrigger(T)", setup: Trigger), TimeSpan.FromSeconds(8))!;

        Assert.Equal("enabled", report.What);
        Assert.Equal(["yes", "no"], report.Values);

        report = Desync.Check(Map("BlzDestroyFrame(F)", setup: Frame), TimeSpan.FromSeconds(8))!;

        Assert.Equal(["there", null], report.Values);
    }

    // Moonwell's MW0002: what one machine reads (the local player, where its camera looks, the size of its window) differs from what another
    // reads, so a change of the game made from it differs too, with no local-player block around it
    [InstallTheory]
    [InlineData("BlzTriggerRegisterPlayerSyncEvent(T, GetLocalPlayer(), 'p', false)", "sync events", "BlzTriggerRegisterPlayerSyncEvent")]
    [InlineData("SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, GetPlayerId(GetLocalPlayer()))", "player 0", "SetPlayerState")]
    [InlineData("SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, R2I(GetCameraTargetPositionX()))", "player 0", "SetPlayerState")]
    [InlineData("SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, R2I(GetCameraEyePositionX()))", "player 0", "SetPlayerState")]
    [InlineData("local l = GetCameraTargetPositionLoc() SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, R2I(GetLocationX(l)))", "player 0", "SetPlayerState")]
    [InlineData("SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, BlzGetLocalClientWidth())", "player 0", "SetPlayerState")]
    public void A_machine_local_value_that_changes_the_game_is_found(string everyone, string part, string native)
    {
        var report = Desync.Check(Map(everyone: everyone, setup: Trigger), TimeSpan.FromSeconds(8));

        Assert.NotNull(report);
        Assert.Equal(part, report.Part);
        Assert.Equal(2 * 1024, report.Step);
        Assert.NotEqual(report.Values[0], report.Values[1]);
        // Every game made the call, each with its own value; the stack is of a game whose value it changed
        Assert.Contains(report.Stacks, stack => stack?.Contains($"'{native}'") == true);
    }

    const string Gold = "SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, ";

    // Moonwell's MW0004: pairs visits keys that are objects in the order of their addresses, which differ between machines (D740)
    [InstallTheory]
    [InlineData("local keys = {} for i = 1, 12 do keys[{}] = i end local n = 0 for _, v in pairs(keys) do n = n * 3 + v end", "")]
    [InlineData("local keys = {} for i = 1, 12 do keys[T[i]] = i end local n = 0 for _, v in pairs(keys) do n = n * 3 + v end",
        "T = {} for i = 1, 12 do T[i] = CreateTrigger() end")]
    public void A_loop_over_a_table_keyed_by_objects_is_found(string loop, string setup)
    {
        var report = Desync.Check(Map(everyone: $"{loop} {Gold}n % 100000)", setup: setup), TimeSpan.FromSeconds(8));

        Assert.NotNull(report);
        Assert.Equal("player 0", report.Part);
        Assert.Equal(2 * 1024, report.Step);
        Assert.Contains(report.Stacks, stack => stack?.Contains("'SetPlayerState'") == true);
    }

    // A loop over keys that are numbers or strings has one order on every machine
    [InstallFact]
    public void A_loop_over_a_table_keyed_by_numbers_and_strings_is_not_a_desync()
    {
        Assert.Null(Desync.Check(Map(everyone: $"local keys = {{}} for i = 1, 12 do keys[i * 7] = i keys['k' .. i] = i end local n = 0 for _, v in pairs(keys) do n = n * 3 + v end {Gold}n % 100000)"),
            TimeSpan.FromSeconds(8)));
    }

    // A machine's layout and clock make its run its own, the same every time, and the first machine's is a lone run's (D740, D741)
    [InstallFact]
    public void A_machines_layout_and_clock_do_not_change_one_players_run()
    {
        var path = Map(everyone: $"local keys = {{}} for i = 1, 12 do keys[{{}}] = i end local n = 0 for _, v in pairs(keys) do n = n * 3 + v end math.randomseed(os.time()) {Gold}n % 100000)");
        string Hash(int? machine)
        {
            using var game = Simulation.Load(path, new SimulationOptions { Users = [0, 1], Machine = machine ?? 0 });
            game.Advance(TimeSpan.FromSeconds(8));
            return game.StateHash();
        }

        Assert.Equal(Hash(null), Hash(0));
        Assert.Equal(Hash(1), Hash(1));
        Assert.NotEqual(Hash(0), Hash(1));
    }

    // Moonwell's MW0006: a blizzard.j function that compares GetLocalPlayer() and then changes the game desyncs wherever it runs
    [InstallTheory]
    [InlineData("SelectGroupForPlayerBJ(G, Player(1))", "ForGroup")]
    [InlineData("if GetLocalPlayer() == Player(1) then ForGroup(G, function() end) end", "ForGroup")]
    [InlineData("if GetLocalPlayer() == Player(1) then ForForce(bj_FORCE_ALL_PLAYERS, function() end) end", "ForForce")]
    public void A_blizzard_function_that_runs_a_loop_in_a_local_block_is_found(string everyone, string native)
    {
        var report = Desync.Check(Map(everyone: everyone,
            setup: "U = CreateUnit(Player(0), FourCC('hfoo'), 0, 0, 0) G = CreateGroup() GroupAddUnit(G, U)"), TimeSpan.FromSeconds(8));

        Assert.NotNull(report);
        Assert.Equal("group and force loops", report.Part);
        Assert.Equal(2 * 1024, report.Step);
        Assert.Contains(report.Stacks, stack => stack?.Contains($"'{native}'") == true);
    }

    // A loop every machine runs, and a selection made one unit at a time, stay the same (game-facts, Desyncs)
    [InstallTheory]
    [InlineData("ForGroup(G, function() end)")]
    [InlineData("if GetLocalPlayer() == Player(1) then SelectUnit(U, true) end")]
    public void A_loop_every_machine_runs_is_not_a_desync(string everyone)
    {
        Assert.Null(Desync.Check(Map(everyone: everyone,
            setup: "U = CreateUnit(Player(0), FourCC('hfoo'), 0, 0, 0) G = CreateGroup() GroupAddUnit(G, U)"), TimeSpan.FromSeconds(8)));
    }

    // Moonwell's MW0038: a generator seeded from the machine's clock gives each machine its own numbers, and the game's generator is shared
    [InstallTheory]
    [InlineData("math.randomseed(os.time())")]
    [InlineData("math.randomseed(os.clock() * 1000)")]
    public void A_generator_seeded_from_the_machines_clock_is_found(string everyone)
    {
        var report = Desync.Check(Map(everyone: everyone), TimeSpan.FromSeconds(8));

        Assert.NotNull(report);
        Assert.Equal("random numbers", report.Part);
        Assert.Contains(report.Stacks, stack => stack?.Contains("'math.randomseed'") == true);
    }

    // The seed every machine shares is not a desync
    [InstallFact]
    public void A_generator_seeded_the_same_on_every_machine_is_not_a_desync()
    {
        Assert.Null(Desync.Check(Map(everyone: "math.randomseed(42)"), TimeSpan.FromSeconds(8)));
    }

    // Moonwell's MW0048: a value every machine reads, changed on one machine only
    [InstallTheory]
    [InlineData("Shared = (Shared or 0) + 1")]
    [InlineData("Shared = {} Shared[1] = 5")]
    public void A_shared_value_changed_in_a_local_block_is_found_where_everyone_uses_it(string local)
    {
        var report = Desync.Check(Map(local, everyone: $"{Gold}type(Shared) == 'table' and 5 or Shared or 0)", setup: "Shared = 0"), TimeSpan.FromSeconds(8));

        Assert.NotNull(report);
        Assert.Equal("player 0", report.Part);
        Assert.Equal(2 * 1024, report.Step);
        Assert.Contains(report.Stacks, stack => stack?.Contains("'SetPlayerState'") == true);
    }

    // A change made as the map loads is found at step 0
    [InstallFact]
    public void A_change_in_main_is_found_at_the_first_step()
    {
        var report = Desync.Check(Map(setup: "if GetLocalPlayer() == Player(1) then SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_LUMBER, 7) end"), TimeSpan.FromSeconds(8))!;

        Assert.Equal((0L, "PLAYER_STATE_RESOURCE_LUMBER"), (report.Step, report.What));
        Assert.Equal(["0", "7"], report.Values);
        Assert.Contains("'SetPlayerState'", report.Stacks[1]);
    }

    // What only one player's screen shows is not a difference of the games
    [InstallTheory]
    [InlineData("DisplayTextToPlayer(Player(1), 0, 0, 'only you')")]
    [InlineData("SetCameraPosition(100, 100)")]
    [InlineData("SetMusicVolume(40)")]
    [InlineData("local n = GetPlayerName(Player(1))")]
    [InlineData("local x = GetCameraTargetPositionX()")]
    [InlineData("BlzFrameSetVisible(F, false)")]
    [InlineData("BlzFrameSetText(F, 'only you')")]
    public void A_local_change_of_what_one_player_sees_is_not_a_desync(string local)
    {
        Assert.Null(Desync.Check(Map(local, setup: Frame), TimeSpan.FromSeconds(8)));
    }

    // Reading a machine's own value and showing it on that machine changes no game
    [InstallTheory]
    [InlineData("SetCameraPosition(GetCameraTargetPositionX() + 10, GetCameraTargetPositionY())")]
    [InlineData("BlzFrameSetSize(F, BlzGetLocalClientWidth() / 4000, 0.1)")]
    [InlineData("local l = GetCameraTargetPositionLoc() RemoveLocation(l)")]
    public void A_machine_local_value_kept_to_the_screen_is_not_a_desync(string everyone)
    {
        Assert.Null(Desync.Check(Map(everyone: everyone, setup: Frame), TimeSpan.FromSeconds(8)));
    }
}
