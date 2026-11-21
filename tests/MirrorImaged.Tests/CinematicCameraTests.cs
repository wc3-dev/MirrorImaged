using System.Text;
using MirrorImaged.Data;

namespace MirrorImaged.Tests;

/// <summary>
/// SetCinematicCamera, against the probe units-camera-2 (facts, "Cameras"): the target and the eye read the model's
/// camera at once, then its path as it plays, turned by the rotation field and lifted by the z offset, until
/// ResetToGameCamera. The model here is a camera at (200, 0, 100) looking at (0, 0, 50) that moves 600 along x and 300 up
/// in 60 s on a Bezier track, as CameraZoomOut's (a third and two thirds of the way between the keys).
/// </summary>
public sealed class CinematicCameraTests : IDisposable
{
    readonly TestMaps _maps = new();

    public void Dispose() => _maps.Dispose();

    static byte[] Floats(params float[] values) => values.SelectMany(BitConverter.GetBytes).ToArray();

    static byte[] Track(string tag, int interpolation, params (int Time, float[] Value)[] keys) =>
    [
        .. Encoding.ASCII.GetBytes(tag), .. BitConverter.GetBytes(keys.Length), .. BitConverter.GetBytes(interpolation), .. BitConverter.GetBytes(-1),
        .. keys.SelectMany(key => BitConverter.GetBytes(key.Time).Concat(Floats(key.Value))),
    ];

    // MDLX, a version chunk and the camera chunk of one camera: name, eye, field of view, far, near and target, then its tracks
    static byte[] Model(float[] eye, float[] target, params byte[][] tracks)
    {
        var name = new byte[80];
        Encoding.ASCII.GetBytes("Camera02").CopyTo(name, 0);
        var camera = new List<byte>();
        camera.AddRange(name);
        camera.AddRange(Floats([.. eye, 1.4f, 6000, 8, .. target]));
        foreach (var track in tracks) camera.AddRange(track);
        var chunk = BitConverter.GetBytes(camera.Count + 4).Concat(camera).ToArray();
        return [.. "MDLX"u8, .. "VERS"u8, .. BitConverter.GetBytes(4), .. BitConverter.GetBytes(800), .. "CAMS"u8, .. BitConverter.GetBytes(chunk.Length), .. chunk];
    }

    // The key at 0 holds (0, 0, 0) with the control point a third of the way, the key at 60 s the whole way and the control point two thirds
    static byte[] Zoom(float x, float z) =>
        Track("KCTR", 3, (0, [0, 0, 0, 0, 0, 0, x / 3, 0, z / 3]), (60000, [x, 0, z, 2 * x / 3, 0, 2 * z / 3, 0, 0, 0]));

    static StandardData Install(GraphicsMode graphics)
    {
        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["War3.w3mod:Objects/CinematicCameras/Zoom.mdx"] = Model([200, 0, 100], [0, 0, 50], Zoom(600, 300)),
            ["War3.w3mod:Objects/CinematicCameras/Still.mdx"] = Model([10, 20, 30], [1, 2, 3]),
            ["War3.w3mod:Objects/CinematicCameras/NotAModel.mdx"] = [1, 2, 3],
        };
        return new StandardData { Storage = files.GetValueOrDefault, Graphics = graphics };
    }

    IReadOnlyList<string> Run(string main, GraphicsMode graphics = GraphicsMode.Classic)
    {
        using var simulation = Simulation.Load(_maps.Build($$"""
            function config() end
            function main()
                SetCameraBounds(-2000, -2000, 2000, 2000, -2000, 2000, 2000, -2000)
                local function F(x) return string.format("%.3f", x) end
                local function target() return F(GetCameraTargetPositionX()) .. " " .. F(GetCameraTargetPositionY()) .. " " .. F(GetCameraTargetPositionZ()) end
                local function eye() return F(GetCameraEyePositionX()) .. " " .. F(GetCameraEyePositionY()) .. " " .. F(GetCameraEyePositionZ()) end
                {{main}}
            end
            """), "", new SimulationOptions { Graphics = graphics }, Install(graphics));
        simulation.Advance(TimeSpan.FromSeconds(5));
        return [.. simulation.Log.Select(line => line[(line.IndexOf("print: ") + 7)..])];
    }

    // The game printed -0.000 for an x it worked out as a float cosine of a half turn's half
    static string[] Fixed(IEnumerable<string> lines) => [.. lines.Select(line => line.Replace("-0.000", "0.000"))];

    [Fact]
    public void The_target_and_the_eye_read_the_models_camera_at_once_and_its_path_turned_by_the_rotation_a_moment_later()
    {
        var lines = Run("""
            local function show(label) print(label .. " " .. target() .. " eye " .. eye()) end
            SetCameraPosition(0, 0)
            TimerStart(CreateTimer(), 0.5, false, function()
                SetCinematicCamera("Objects\\CinematicCameras\\Zoom.mdx")
                show("now")
                TimerStart(CreateTimer(), 0.5, false, function() show("0.5 s") end)
                TimerStart(CreateTimer(), 2.5, false, function() show("2.5 s") end)
            end)
            """);

        // 0.5 s: the eye (200, 0, 100) moved 600 * 0.5 / 60 = 5 along x and 2.5 up, then turned a quarter (rotation 90): (0, 205); the target (0, 0, 50) turned is (0, 0)
        Assert.Equal(["now 0.000 0.000 50.000 eye 200.000 0.000 100.000", "0.5 s 0.000 0.000 50.000 eye 0.000 205.000 102.500", "2.5 s 0.000 0.000 50.000 eye 0.000 225.000 112.500"], Fixed(lines));
    }

    [Fact]
    public void The_path_plays_at_the_cameras_target_and_the_z_offset_lifts_it()
    {
        var lines = Run("""
            SetCameraPosition(500, -300)
            SetCameraField(CAMERA_FIELD_ZOFFSET, 40, 0)
            TimerStart(CreateTimer(), 0.5, false, function()
                SetCinematicCamera("Objects\\CinematicCameras\\Still.mdx")
                print("now " .. target() .. " eye " .. eye())
                TimerStart(CreateTimer(), 0.25, false, function() print("later " .. target() .. " eye " .. eye()) end)
            end)
            """);

        // The still camera at (10, 20, 30) looking at (1, 2, 3): at once about (500, -300), then turned a quarter: (x, y) to (-y, x), 40 higher
        Assert.Equal(["now 501.000 -298.000 3.000 eye 510.000 -280.000 30.000", "later 498.000 -299.000 43.000 eye 480.000 -290.000 70.000"], Fixed(lines));
    }

    [Fact]
    public void The_fields_read_as_they_were_and_ResetToGameCamera_ends_it()
    {
        var lines = Run("""
            SetCameraPosition(0, 0)
            TimerStart(CreateTimer(), 0.5, false, function()
                local distance = GetCameraField(CAMERA_FIELD_TARGET_DISTANCE)
                SetCinematicCamera("Objects\\CinematicCameras\\Zoom.mdx")
                TimerStart(CreateTimer(), 0.5, false, function()
                    print("fields " .. tostring(distance == GetCameraField(CAMERA_FIELD_TARGET_DISTANCE)) .. " " .. F(GetCameraField(CAMERA_FIELD_ROTATION)))
                    ResetToGameCamera(0)
                    TimerStart(CreateTimer(), 0.25, false, function() print("reset " .. target() .. " eye " .. eye()) end)
                end)
            end)
            """);

        Assert.Equal("fields true 1.571", lines[0]);
        Assert.Equal("reset 0.000 0.000 0.000 eye 0.000 -1118.386 1658.075", Fixed(lines)[1]);
    }

    [Theory]
    [InlineData("\"Objects\\\\CinematicCameras\\\\Missing.mdx\"")]
    [InlineData("\"Objects\\\\CinematicCameras\\\\NotAModel.mdx\"")]
    public void A_missing_or_unreadable_model_leaves_the_game_camera(string model)
    {
        var lines = Run($$"""
            SetCameraPosition(0, 0)
            TimerStart(CreateTimer(), 0.5, false, function()
                SetCinematicCamera({{model}})
                TimerStart(CreateTimer(), 0.25, false, function() print(target() .. " eye " .. eye()) end)
            end)
            """);

        Assert.Equal(["0.000 0.000 0.000 eye 0.000 -1118.386 1658.075"], Fixed(lines));
    }
}
