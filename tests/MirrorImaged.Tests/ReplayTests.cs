using System.IO.Compression;
using System.Text;
using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>Replays (.w3g): their lobby, the actions of their players, and playing them in a run.</summary>
public sealed class ReplayTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    // A replay in the format the game writes (header version 1): a lobby of one person and a computer, then the actions the test gives
    static byte[] Make(long durationMillis, params (int Millis, byte Player, byte[] Actions)[] slots)
    {
        var body = new List<byte>();
        void Text(string text) { body.AddRange(Encoding.UTF8.GetBytes(text)); body.Add(0); }
        body.AddRange([0x10, 0x01, 0x00, 0x00]);
        body.AddRange([0x00, 0x01]); Text("Tester"); body.AddRange([0x02, 0x00, 0x00]);
        Text("Test game"); Text(""); Text("encoded");
        body.AddRange(new byte[12]);
        body.Add(0x19); body.AddRange(BitConverter.GetBytes((ushort)(1 + 18 + 6))); body.Add(2);
        body.AddRange([0x01, 0x64, 0x02, 0x00, 0x00, 0x00, 0x60, 0x01, 0x64]);
        body.AddRange([0x00, 0xFF, 0x02, 0x01, 0x01, 0x01, 0x01, 0x01, 0x64]);
        body.AddRange([0x11, 0x22, 0x33, 0x44, 0x03, 0x02]);
        foreach (var kind in new byte[] { 0x1A, 0x1B, 0x1C }) body.AddRange([kind, 1, 0, 0, 0]);
        var last = 0;
        foreach (var (millis, player, actions) in slots)
        {
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

    static byte[] Select(params int[] objects) =>
        [0x16, 0x01, .. BitConverter.GetBytes((ushort)objects.Length), .. objects.SelectMany(id => BitConverter.GetBytes(id).Concat(BitConverter.GetBytes(id)))];

    static byte[] Point(int order, float x, float y) =>
        [0x11, 0x00, 0x00, .. BitConverter.GetBytes(order), .. new byte[8], .. BitConverter.GetBytes(x), .. BitConverter.GetBytes(y)];

    [Fact]
    public void A_replay_gives_its_lobby_and_what_its_players_did_in_order()
    {
        var bytes = Make(5000,
            (1000, 1, [.. Select(0x100005, 0x100006), .. Point(851986, 300, -40)]),
            (4000, 1, [0x01]));

        var replay = Replay.Read(bytes);

        Assert.Equal(7003, replay.Build);
        Assert.Equal("Test game", replay.GameName);
        Assert.Equal([("Tester", false), ("", true)], replay.Players.Select(p => (p.Name, p.Computer)));
        Assert.Equal(["select", "order"], replay.Actions.Select(a => a.Kind));
        Assert.Equal([0x100005, 0x100006], replay.Actions[0].Objects);
        Assert.Equal((851986, 300f, -40f, 1000), (replay.Actions[1].Code, replay.Actions[1].X, replay.Actions[1].Y, (int)replay.Actions[1].Millis));
        Assert.Equal(3, replay.ActionCounts.Count);
    }

    [Fact]
    public void Bytes_that_are_not_a_replay_are_refused()
    {
        Assert.Throws<InvalidDataException>(() => Replay.Read(new byte[200]));
    }

    static Dictionary<string, string> Values(string pairs) =>
        pairs.Split(';', StringSplitOptions.TrimEntries).Select(pair => pair.Split('=', 2)).ToDictionary(pair => pair[0], pair => pair[1]);

    [Fact]
    public void A_replay_run_on_its_map_gives_the_players_orders_to_their_units()
    {
        var path = _maps.Build("""
            function config() end
            function main() CreateUnit(Player(0), FourCC("hfoo"), 0, 0, 0) end
            """);
        var data = new StandardData().Add(ObjectKind.Unit, "hfoo", Values("unam=Footman;uhpm=100;umvs=270;umvt=foot;uacq=500;uaen=1;ua1w=normal;ua1t=normal;ua1g=ground;ua1b=5;ua1d=2;ua1s=3;ua1c=1;ua1r=90;udp1=0.5;ucol=31;udef=0;udty=normal"));
        using var probe = Simulation.Load(path, "", data: data);
        var footman = probe.Units(0)[0].Id;
        var replay = Replay.Read(Make(6000, (1000, 1, [.. Select(footman), .. Point(851986, 500, 0)]), (5000, 1, [0x01])));

        using var simulation = Simulation.Load(path, "", data: data);
        simulation.Follow(replay.ToRecording(path));

        Assert.Equal(6000 * 1024 / 1000, simulation.Step);
        Assert.InRange(simulation.Unit(footman)!.X, 490, 510);
    }
}
