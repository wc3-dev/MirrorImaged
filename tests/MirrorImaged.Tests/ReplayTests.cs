using System.IO.Compression;
using System.Text;
using MirrorImaged.Data;
using War3Net.Build.Info;

namespace MirrorImaged.Tests;

/// <summary>A fact that plays the replay in <c>MIRROR_REPLAY</c> on the map in <c>MIRROR_REPLAY_MAP</c>; skipped when either is not set or there is no install.</summary>
public sealed class ReplayFileFactAttribute : FactAttribute
{
    public ReplayFileFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("MIRROR_REPLAY") is null || Environment.GetEnvironmentVariable("MIRROR_REPLAY_MAP") is null)
            Skip = "Set MIRROR_REPLAY to a .w3g and MIRROR_REPLAY_MAP to its map to run it";
        else if (GameInstall.Find() is null) Skip = $"No Warcraft III install in {GameInstall.Place()}";
    }
}

/// <summary>Replays (.w3g): their lobby, the actions of their players, and playing them in a run.</summary>
public sealed class ReplayTests : IDisposable
{
    const int Smart = 851971, Attack = 851983, Move = 851986;

    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    // A replay in the format the game writes (header version 1): a lobby of one person and a computer, then the actions the test gives
    static byte[] Make(long durationMillis, params (int Millis, byte Player, byte[] Actions)[] slots) => Make([0x60], [], "encoded"u8.ToArray(), durationMillis, slots);

    // As Make, with a person for each of the race flags (players 1, 2, ... in slots 0, 1, ...) and then a computer, the blocks the game since
    // 1.32 puts before the lobby, and the lobby's encoded settings. A slot of player 0 is bytes that go between the time slots as they are.
    static byte[] Make(byte[] races, byte[] blocks, byte[] settings, long durationMillis, params (int Millis, byte Player, byte[] Actions)[] slots)
    {
        var body = new List<byte>();
        void Text(string text) { body.AddRange(Encoding.UTF8.GetBytes(text)); body.Add(0); }
        body.AddRange([0x10, 0x01, 0x00, 0x00]);
        body.AddRange([0x00, 0x01]); Text("Tester"); body.AddRange([0x02, 0x00, 0x00]);
        Text("Test game"); Text(""); body.AddRange(settings); body.Add(0);
        body.AddRange(new byte[12]);
        for (var person = 2; person <= races.Length; person++)
        {
            body.AddRange([0x16, (byte)person]); Text($"Tester {person}"); body.AddRange([0x02, 0x00, 0x00]);
            body.AddRange(new byte[4]);
        }
        body.AddRange(blocks);
        body.Add(0x19); body.AddRange(BitConverter.GetBytes((ushort)(1 + 9 * (races.Length + 1) + 6))); body.Add((byte)(races.Length + 1));
        for (var person = 0; person < races.Length; person++)
            body.AddRange([(byte)(person + 1), 0x64, 0x02, 0x00, (byte)person, (byte)person, races[person], 0x01, 0x64]);
        body.AddRange([0x00, 0xFF, 0x02, 0x01, 0x01, 0x01, 0x01, 0x01, 0x64]);
        body.AddRange([0x11, 0x22, 0x33, 0x44, 0x03, 0x02]);
        foreach (var kind in new byte[] { 0x1A, 0x1B, 0x1C }) body.AddRange([kind, 1, 0, 0, 0]);
        var last = 0;
        foreach (var (millis, player, actions) in slots)
        {
            if (player == 0)
            {
                body.AddRange(actions);
                continue;
            }
            var command = new List<byte> { player };
            command.AddRange(BitConverter.GetBytes((ushort)actions.Length));
            command.AddRange(actions);
            body.Add(0x1F);
            body.AddRange(BitConverter.GetBytes((ushort)(2 + command.Count)));
            body.AddRange(BitConverter.GetBytes((ushort)(millis - last)));
            body.AddRange(command);
            last = millis;
        }
        var packed = new MemoryStream();
        using (var zlib = new ZLibStream(packed, CompressionMode.Compress, true)) zlib.Write([.. body]);
        var file = new List<byte>();
        file.AddRange("Warcraft III recorded game\u001a\0"u8.ToArray());
        file.AddRange(BitConverter.GetBytes(68));
        file.AddRange(BitConverter.GetBytes(68 + 12 + (int)packed.Length));
        file.AddRange(BitConverter.GetBytes(1));
        file.AddRange(BitConverter.GetBytes(body.Count));
        file.AddRange(BitConverter.GetBytes(1));
        file.AddRange("PX3W"u8.ToArray());
        file.AddRange(BitConverter.GetBytes(10200));
        file.AddRange(BitConverter.GetBytes((ushort)7003)); file.AddRange(BitConverter.GetBytes((ushort)0));
        file.AddRange(BitConverter.GetBytes((int)durationMillis));
        file.AddRange(BitConverter.GetBytes(0));
        file.AddRange(BitConverter.GetBytes((int)packed.Length));
        file.AddRange(BitConverter.GetBytes(body.Count));
        file.AddRange(BitConverter.GetBytes(0));
        file.AddRange(packed.ToArray());
        return [.. file];
    }

    // The lobby's settings as the game writes them: each seven bytes after a byte of flags, an even byte one more than it is
    static byte[] Settings(string map)
    {
        byte[] plain = [.. new byte[13], .. Encoding.UTF8.GetBytes(map), 0, .. "Tester"u8, 0];
        var encoded = new List<byte>();
        foreach (var chunk in plain.Chunk(7))
        {
            encoded.Add((byte)(1 | chunk.Select((b, i) => b % 2 == 1 ? 1 << i + 1 : 0).Sum()));
            encoded.AddRange(chunk.Select(b => (byte)(b % 2 == 1 ? b : b + 1)));
        }
        return [.. encoded];
    }

    // The two numbers the game names an object by: the first, and the second, which it counts up
    static long Object(int first, int second) => (long)second << 32 | (uint)first;

    static byte[] Select(params long[] objects) => [0x16, 0x01, .. BitConverter.GetBytes((ushort)objects.Length), .. objects.SelectMany(BitConverter.GetBytes)];
    static byte[] Deselect(params long[] objects) => [0x16, 0x02, .. BitConverter.GetBytes((ushort)objects.Length), .. objects.SelectMany(BitConverter.GetBytes)];
    static byte[] Group(byte group, params long[] objects) => [0x17, group, .. BitConverter.GetBytes((ushort)objects.Length), .. objects.SelectMany(BitConverter.GetBytes)];
    static byte[] Pick(byte group) => [0x18, group, 0x03];
    static byte[] Subgroup(string type, long lead) => [0x19, .. BitConverter.GetBytes(Ids.Of(type)), .. BitConverter.GetBytes(lead)];
    static byte[] Ability(long unit, string ability) => [0x7A, .. BitConverter.GetBytes(unit), .. BitConverter.GetBytes(Ids.Of(ability)), .. new byte[4]];
    static byte[] Alone(int order) => [0x10, 0x00, 0x00, .. BitConverter.GetBytes(order), .. new byte[8]];
    static byte[] Point(int order, float x, float y, bool queued = false) =>
        [0x11, (byte)(queued ? 1 : 0), 0x00, .. BitConverter.GetBytes(order), .. new byte[8], .. BitConverter.GetBytes(x), .. BitConverter.GetBytes(y)];
    static byte[] Target(int order, float x, float y, long target) => [.. Point(order, x, y), .. BitConverter.GetBytes(target)];
    static byte[] At(int order, float x, float y, long target) => [0x12, .. Target(order, x, y, target).Skip(1)];
    static byte[] Dequeue(byte at, string type) => [0x1E, at, .. BitConverter.GetBytes(Ids.Of(type))];
    static byte[] AtType(int order, float x, float y, string type) =>
        [0x14, .. Point(order, x, y).Skip(1), .. BitConverter.GetBytes(Ids.Of(type)), .. new byte[9], .. BitConverter.GetBytes(x), .. BitConverter.GetBytes(y)];

    [Fact]
    public void A_replay_gives_its_lobby_and_what_its_players_did_in_order()
    {
        var bytes = Make(5000,
            (1000, 1, [.. Select(0x100005, 0x100006), .. Point(Move, 300, -40)]),
            (4000, 1, [0x01]));

        var replay = Replay.Read(bytes);

        Assert.Equal(7003, replay.Build);
        Assert.Equal("Test game", replay.GameName);
        Assert.Equal("", replay.Map);
        Assert.Equal([("Tester", false), ("", true)], replay.Players.Select(p => (p.Name, p.Computer)));
        Assert.Equal(["select", "order"], replay.Actions.Select(a => a.Kind));
        Assert.Equal([0x100005, 0x100006], replay.Actions[0].Objects);
        Assert.Equal((Move, 300f, -40f, 1000, false), (replay.Actions[1].Code, replay.Actions[1].X, replay.Actions[1].Y, (int)replay.Actions[1].Millis, replay.Actions[1].Queued));
        Assert.Equal(3, replay.ActionCounts.Count);
    }

    // What the game since 1.32 and 2.0 adds: blocks before the lobby, and the action of a unit's button and a ladder's data among the others
    [Fact]
    public void A_replay_of_the_game_today_is_read_whole()
    {
        var (a, b) = (Object(0x7471, 0x7477), Object(0x7267, 0x87F8));
        byte[] blocks = [0x39, 0x03, 2, 0, 0, 0, 0xAA, 0xBB, 0x38, 0x01, 1, 0, 0, 0, 0xCC];
        var bytes = Make([0x08, 0x41], blocks, Settings("maps\\test\\(2)Small.w3x"), 9000,
            (100, 1, [.. Select(a, b), .. Group(3, a, b), .. Subgroup("hpea", b), 0x1A, .. Deselect(a), .. Pick(3)]),
            (200, 2, [0x77, (byte)'T', 0, (byte)'A', (byte)'B', 0, 0, 0, 0, 0, .. Ability(a, "Aque"), .. Alone(Ids.Of("hpea"))]),
            (300, 1, [.. Point(Move, 10, 20, queued: true), .. At(Smart, 30, 40, b), .. At(Smart, 50, 60, -1)]),
            (0, 0, [0x17, 1, 0, 0, 0, 2, 9, 0, 0, 0, 0, 0, 0, 0]));

        var replay = Replay.Read(bytes);

        Assert.Equal("maps\\test\\(2)Small.w3x", replay.Map);
        Assert.Equal([(1, "Tester", 0x08), (2, "Tester 2", 0x41), (0, "", 0x01)], replay.Players.Select(p => (p.Id, p.Name, p.Race)));
        Assert.DoesNotContain(replay.Actions, action => action.Kind == "other");
        Assert.Equal(["select", "group", "subgroup", "select", "pick", "ability", "order", "order", "order", "order", "leave"], replay.Actions.Select(action => action.Kind));
        Assert.Equal([3, Ids.Of("hpea"), 2, 3], replay.Actions.Skip(1).Take(4).Select(action => action.Code));
        Assert.Equal([a, b], replay.Actions[1].Objects);
        Assert.Equal([b], replay.Actions[2].Objects);
        Assert.Equal([a], replay.Actions[3].Objects);
        Assert.Equal((2, Ids.Of("Aque")), (replay.Actions[5].Player, replay.Actions[5].Code));
        Assert.Equal([a], replay.Actions[5].Objects);
        Assert.True(float.IsNaN(replay.Actions[6].X) && replay.Actions[6].Code == Ids.Of("hpea"));
        Assert.Equal((10f, 20f, true), (replay.Actions[7].X, replay.Actions[7].Y, replay.Actions[7].Queued));
        Assert.Equal((30f, false), (replay.Actions[8].X, replay.Actions[8].Queued));
        Assert.Equal([b], replay.Actions[8].Objects);
        Assert.Equal([-1L], replay.Actions[9].Objects);
        Assert.Equal((2, 9), (replay.Actions[10].Player, replay.Actions[10].Code));
    }

    [Fact]
    public void Bytes_that_are_not_a_replay_are_refused()
    {
        Assert.Throws<InvalidDataException>(() => Replay.Read(new byte[200]));
    }

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    static StandardData Standard() => new StandardData()
        .Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=100;umvs=270;umvt=foot;uacq=500;uaen=1;ua1w=normal;ua1t=normal;ua1g=ground;ua1b=5;ua1d=2;ua1s=3;ua1c=1;ua1r=90;udp1=0.5;ucol=31;udef=0;udty=normal;ugol=135;ufoo=2;ubld=20"))
        .Add(ObjectKind.Unit, "hpea", Values("unam=Peasant;uhpm=220;umvs=190;umvt=foot;utyp=peon;ucol=31;uabi=Ahar;uaen=0"))
        .Add(ObjectKind.Unit, "hbar", Values("unam=Barracks;uhpm=1500;ubdg=1;utra=hfoo;ucol=80;umvs=0"))
        .Add(ObjectKind.Unit, "htow", Values("unam=Town Hall;uhpm=1500;ubdg=1;utyp=townhall;ucol=100;umvs=0"))
        .Add(ObjectKind.Unit, "ngol", Values("unam=Gold Mine;ubdg=1;uhpm=2000;uabi=Agld;umvs=0;ucol=80"))
        .Add(ObjectKind.Ability, "Agld", Values("anam=Gold Mine;alev=1;Gld1:1=12500"))
        .Add(ObjectKind.Ability, "Ahar", Values("anam=Harvest;alev=1"));

    Simulation Map(string main, Action<MapInfo>? change = null, SimulationOptions? options = null) => Simulation.Load(_maps.Build($$"""
        function config() end
        function main()
        {{main}}
        end
        """, change: change), "", options, Standard());

    [Fact]
    public void A_replay_run_on_its_map_gives_the_players_orders_to_their_units()
    {
        using var simulation = Map("""CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)""");
        var footman = simulation.Units(0)[0].Id;
        var replay = Replay.Read(Make(6000, (1000, 1, [.. Select(footman), .. Point(Move, 500, 0)]), (5000, 1, [0x61])));

        var run = simulation.Follow(replay);

        Assert.Equal(6000 * 1024 / 1000, simulation.Step);
        Assert.InRange(simulation.Unit(footman)!.X, 490, 510);
        Assert.Equal((1, 1, 0, 1, 1), (run.Orders, run.Taken, run.Unowned, run.Named, run.Found));
        Assert.Equal([new(0x61, 1)], run.NotPlayed);
    }

    // The game's numbers for a unit are not the run's: the second of them says the order the units were made in
    [Fact]
    public void Units_are_found_by_the_order_they_were_made_in_whatever_numbers_the_game_gave_them()
    {
        using var simulation = Map("""
            CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
            CreateUnit(Player(0), FourCC("hfoo"), 0, 300, 0)
            """);
        var (first, second, third) = (Object(0x900, 0x7001), Object(0x100, 0x7005), Object(0x500, 0x7009));
        var replay = Replay.Read(Make(8000,
            (100, 1, [.. Select(second, first), .. Point(Move, 400, 0)]),
            (200, 1, [.. Deselect(first), .. Point(Move, -400, 300)]),
            (300, 1, [.. Deselect(second), .. Select(third), .. Point(Move, 0, -400)])));

        var run = simulation.Follow(replay);

        Assert.Equal([(400, 0), (-400, 300)], simulation.Units(0).Select(u => ((int)MathF.Round(u.X / 10) * 10, (int)MathF.Round(u.Y / 10) * 10)));
        Assert.Equal((3, 2, 1, 3, 2), (run.Orders, run.Taken, run.Unowned, run.Named, run.Found));
    }

    [Fact]
    public void The_type_a_subgroup_names_finds_the_unit()
    {
        using var simulation = Map("""
            CreateUnit(Player(0), FourCC("hpea"), 0, 0, 0)
            CreateUnit(Player(0), FourCC("hfoo"), 0, 300, 0)
            """);
        var footman = Object(0x100, 0x7005);
        var replay = Replay.Read(Make(5000, (100, 1, [.. Select(footman), .. Subgroup("hfoo", footman), .. Point(Move, 400, 300)])));

        simulation.Follow(replay);

        Assert.Equal([("hpea", 0), ("hfoo", 400)], simulation.Units(0).Select(u => (u.Type, (int)MathF.Round(u.X / 10) * 10)));
    }

    [Fact]
    public void A_kept_group_picked_again_takes_the_orders()
    {
        using var simulation = Map("""CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)""");
        var footman = Object(0x100, 0x7005);
        var replay = Replay.Read(Make(5000,
            (100, 1, [.. Select(footman), .. Group(2, footman), .. Deselect(footman), .. Point(Move, -400, 0)]),
            (200, 1, [.. Pick(2), .. Point(Move, 400, 0)])));

        var run = simulation.Follow(replay);

        Assert.InRange(simulation.Units(0)[0].X, 390, 410);
        Assert.Equal((2, 1, 1), (run.Orders, run.Taken, run.Unowned));
    }

    // Two barracks selected and the footman's button pressed once: one footman, as in the game
    [Fact]
    public void A_button_of_the_card_goes_to_one_unit_of_the_selection()
    {
        using var simulation = Map("""
            SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, 1000)
            SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_FOOD_CAP, 10)
            CreateUnit(Player(0), FourCC("hfoo"), -600, 0, 0)
            CreateUnit(Player(0), FourCC("hbar"), 0, 0, 0)
            CreateUnit(Player(0), FourCC("hbar"), 600, 0, 0)
            """);
        var (one, other) = (Object(0x100, 0x7005), Object(0x200, 0x7009));
        var replay = Replay.Read(Make(30000, (100, 1, [.. Select(one, other), .. Ability(one, "Aque"), .. Ability(other, "Aque"), .. Alone(Ids.Of("hfoo"))])));

        var run = simulation.Follow(replay);

        Assert.Equal(2, simulation.Units(0).Count(u => u.Type == "hfoo"));
        Assert.Equal((1, 1), (run.Orders, run.Taken));
    }

    [Fact]
    public void An_order_at_a_unit_finds_it_where_the_order_says_it_stands()
    {
        using var simulation = Map("""
            CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
            CreateUnit(Player(1), FourCC("hfoo"), 300, 0, 0)
            CreateUnit(Player(1), FourCC("hfoo"), 300, 600, 0)
            """);
        var (mine, foe) = (Object(0x100, 0x7005), Object(0x300, 0x7020));
        var replay = Replay.Read(Make(20000, (100, 1, [.. Select(mine), .. At(Attack, 300, 0, foe)])));

        simulation.Follow(replay);

        Assert.Equal([600f], simulation.Units(1).Select(u => u.Y));
    }

    [Fact]
    public void A_workers_smart_order_at_a_mine_is_the_harvest_order()
    {
        using var simulation = Map("""
            CreateUnit(Player(0), FourCC("htow"), 0, 0, 0)
            CreateUnit(Player(0), FourCC("hpea"), 200, 0, 0)
            CreateUnit(Player(0), FourCC("hfoo"), 200, 200, 0)
            CreateUnit(Player(PLAYER_NEUTRAL_PASSIVE), FourCC("ngol"), 600, 0, 0)
            """);
        var (peasant, footman, mine) = (Object(0x100, 0x7005), Object(0x200, 0x7009), Object(0x50, 0x6000));
        var replay = Replay.Read(Make(20000, (100, 1, [.. Select(peasant, footman), .. At(Smart, 600, 0, mine)])));

        simulation.Follow(replay);

        Assert.Equal([852018, Smart], simulation.Units(0).Where(u => u.Type != "htow").Select(u => u.Order is 852018 or 852020 ? 852018 : u.Order));
        Assert.True(simulation.Natives.GetPlayerState(simulation.Natives.Player(0), simulation.Natives.ConvertPlayerState(1)) > 0);
    }

    [Fact]
    public void A_queued_order_waits_for_the_one_before_it()
    {
        using var simulation = Map("""CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)""");
        var footman = Object(0x100, 0x7005);
        var replay = Replay.Read(Make(8000, (0, 1, [.. Select(footman), .. Point(Move, 400, 0), .. Point(Move, 400, 400, queued: true)])));
        var run = simulation.Play(replay);

        run.To(1024);
        var early = simulation.Units(0)[0];
        run.To(run.Steps);

        Assert.True(early.X > 100 && Math.Abs(early.Y) < 20, $"{early.X}, {early.Y}");
        Assert.Equal((400, 400), ((int)MathF.Round(simulation.Units(0)[0].X / 10) * 10, (int)MathF.Round(simulation.Units(0)[0].Y / 10) * 10));
    }

    // The lobby's people in the map's slots, with its races, each at the start place its first order is nearest; the watcher has no slot
    [Fact]
    public void The_replays_lobby_seats_the_map()
    {
        static PlayerData Slot(int id, float x) => new() { Id = id, Controller = PlayerController.User, Race = PlayerRace.Selectable, Name = $"Player {id + 1}", StartPosition = new(x, 0) };
        var path = _maps.Build("""
            function config()
                DefineStartLocation(0, -1000, 0)
                DefineStartLocation(1, 1000, 0)
                SetPlayerStartLocation(Player(0), 0)
                SetPlayerStartLocation(Player(1), 1)
            end
            function main()
                for i = 0, 1 do print(i, GetHandleId(GetPlayerRace(Player(i))), GetPlayerStartLocation(Player(i)), GetPlayerSlotState(Player(i)) == PLAYER_SLOT_STATE_PLAYING) end
                print(GetPlayerSlotState(Player(2)) == PLAYER_SLOT_STATE_PLAYING)
            end
            """, change: info => info.Players.AddRange([Slot(0, -1000), Slot(1, 1000)]));
        var replay = Replay.Read(Make([0x08, 0x41, 0x60], [], Settings("map"), 1000,
            (100, 1, Point(Move, 900, 50)), (100, 2, Point(Move, -900, 50)), (100, 3, Point(Move, 0, 0))));

        var options = Simulation.Seats(path, replay);
        using var simulation = Simulation.Load(path, "", options, Standard());

        Assert.Equal([0, 1], options.Users);
        Assert.Equal(["0\t3\t1\ttrue", "1\t1\t0\ttrue", "false"], simulation.Log.Select(line => line[(line.IndexOf(": ", StringComparison.Ordinal) + 2)..]));
    }

    [Fact]
    public void A_run_played_from_a_replay_runs_again_from_its_recording()
    {
        const string script = """
            CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)
            CreateUnit(Player(1), FourCC("hfoo"), 300, 0, 0)
            """;
        using var simulation = Map(script);
        var (mine, foe) = (Object(0x100, 0x7005), Object(0x300, 0x7020));
        simulation.Follow(Replay.Read(Make(6000, (100, 1, [.. Select(mine), .. At(Attack, 300, 0, foe), .. Point(Move, -300, 0, queued: true)]))));

        using var again = Map(script);
        again.Follow(simulation.Recording);

        Assert.Equal(simulation.StateHash(), again.StateHash());
        Assert.Contains(simulation.Recording.Inputs, input => input.Kind == "act");
    }

    // Action 0x1E: the third footman of the queue taken out, its price back
    [Fact]
    public void An_item_taken_out_of_a_buildings_queue_gives_its_price_back()
    {
        using var simulation = Map("""
            SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, 1000)
            SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_FOOD_CAP, 10)
            CreateUnit(Player(0), FourCC("hbar"), 0, 0, 0)
            """);
        var barracks = Object(0x100, 0x7005);
        var replay = Replay.Read(Make(3000,
            (100, 1, [.. Select(barracks), .. Alone(Ids.Of("hfoo")), .. Alone(Ids.Of("hfoo")), .. Alone(Ids.Of("hfoo"))]),
            (500, 1, Dequeue(2, "hfoo"))));

        var run = simulation.Follow(replay);

        Assert.Equal(1000 - 2 * 135, simulation.Natives.GetPlayerState(simulation.Natives.Player(0), simulation.Natives.ConvertPlayerState(1)));
        Assert.Equal(1, run.Dequeued);
        Assert.Empty(run.NotPlayed);
    }

    // Action 0x14 names the mine by its type: the worker harvests the mine of that type at the place
    [Fact]
    public void An_order_at_a_type_goes_to_the_object_of_that_type_at_the_place()
    {
        using var simulation = Map("""
            CreateUnit(Player(0), FourCC("hpea"), 200, 0, 0)
            CreateUnit(Player(0), FourCC("htow"), 0, 0, 0)
            CreateUnit(Player(PLAYER_NEUTRAL_PASSIVE), FourCC("ngol"), 600, 0, 0)
            """);
        var peasant = Object(0x100, 0x7005);
        var replay = Replay.Read(Make(20000, (100, 1, [.. Select(peasant), .. AtType(Smart, 580, 20, "ngol")])));

        var run = simulation.Follow(replay);

        Assert.Equal((1, 1), (run.Orders, run.Taken));
        Assert.True(simulation.Natives.GetPlayerState(simulation.Natives.Player(0), simulation.Natives.ConvertPlayerState(1)) > 0);
    }

    [Fact]
    public void The_run_counts_its_orders_by_game_minute_and_names_the_first_that_failed()
    {
        using var simulation = Map("""CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0)""");
        var (footman, gone) = (Object(0x100, 0x7005), Object(0x200, 0x7009));
        var replay = Replay.Read(Make(62000,
            (1000, 1, [.. Select(footman), .. Point(Move, 300, 0)]),
            (61000, 1, [.. Deselect(footman), .. Select(gone), .. Point(Move, 0, 0)])));

        var run = simulation.Follow(replay);

        Assert.Equal([new(0, 1, 1, 1, 1), new(1, 1, 0, 2, 1)], run.Minutes);
        Assert.Equal("1:01, player 0, move: no unit of the selection is in this run", run.FirstMiss);
    }

    // The replay of a game someone played, which is not in the repository: it is read whole and runs to its end on its map
    [ReplayFileFact]
    [Trait("Needs", "Game")]
    public void A_replay_of_a_played_game_runs_to_its_end()
    {
        var replay = Replay.Read(Environment.GetEnvironmentVariable("MIRROR_REPLAY")!);
        using var simulation = Simulation.Load(Environment.GetEnvironmentVariable("MIRROR_REPLAY_MAP")!, replay);

        var run = simulation.Follow(replay);

        Assert.DoesNotContain(replay.Actions, action => action.Kind == "other");
        Assert.Null(simulation.Stopped);
        Assert.Equal(run.Steps, simulation.Step);
        Assert.DoesNotContain(simulation.Log, line => line.Contains(" error: ", StringComparison.Ordinal));
        Assert.True(run.Taken > 0 && run.Found > 0);
        // The share of orders a unit took may not fall: 32.8% on the 31-minute ladder game this test was first run with (decisions D645); a
        // run of another replay needs its own floor
        Assert.True(run.Taken >= ShareFloor * run.Orders, $"{run.Taken} of {run.Orders} orders taken, under {ShareFloor:P0}; first not taken: {run.FirstMiss}");
    }

    const double ShareFloor = 0.32;
}
