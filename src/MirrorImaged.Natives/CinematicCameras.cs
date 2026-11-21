using System.Text;
using MirrorImaged.World;

namespace MirrorImaged.Natives;

// A cinematic camera (probe units-camera-2; D794): the first camera of a model in Objects\CinematicCameras, which plays at the camera's target
// from the moment it is set. Its eye is the camera's position and its target the camera's target, each moved by the model's translation track
// (KCTR, KTTR) at the milliseconds since. In the step it is set they read as the model gives them; a step later they are turned by the rotation
// field, lifted by the z offset and put on the ground there, as the game's draw does. The fields read as they were and ResetToGameCamera ends it.
public sealed partial class GameNatives
{
    // A track's keys: the time in milliseconds, the value and, for the interpolations that have them (hermite 2, bezier 3), the tangents or control points
    sealed record Track(int Interpolation, (int Time, float[] Value, float[] In, float[] Out)[] Keys)
    {
        public float[] At(double milliseconds)
        {
            if (Keys.Length == 0) return [0, 0, 0];
            if (milliseconds <= Keys[0].Time) return Keys[0].Value;
            if (milliseconds >= Keys[^1].Time) return Keys[^1].Value;
            var first = Array.FindLastIndex(Keys, key => key.Time <= milliseconds);
            var (a, b) = (Keys[first], Keys[first + 1]);
            var s = (float)((milliseconds - a.Time) / (b.Time - a.Time));
            float[] Mix(Func<int, float> value) => [.. Enumerable.Range(0, 3).Select(value)];
            return Interpolation switch
            {
                0 => a.Value,
                1 => Mix(i => a.Value[i] + (b.Value[i] - a.Value[i]) * s),
                2 => Mix(i => (2 * s * s * s - 3 * s * s + 1) * a.Value[i] + (-2 * s * s * s + 3 * s * s) * b.Value[i] + (s * s * s - 2 * s * s + s) * a.Out[i] + (s * s * s - s * s) * b.In[i]),
                _ => Mix(i => (1 - s) * (1 - s) * (1 - s) * a.Value[i] + 3 * (1 - s) * (1 - s) * s * a.Out[i] + 3 * (1 - s) * s * s * b.In[i] + s * s * s * b.Value[i]),
            };
        }
    }

    sealed record Shot(float[] Eye, float[] Target, Track? EyeTrack, Track? TargetTrack);

    (Shot Shot, long Start, float X, float Y)? _cinematic;
    readonly Dictionary<string, Shot?> _shots = [];

    // The first camera of the model's CAMS chunk: name, eye, field of view, far, near and target after its size, then its tracks
    static Shot? ReadShot(byte[] bytes)
    {
        for (var at = 4; at + 8 <= bytes.Length;)
        {
            var size = BitConverter.ToInt32(bytes, at + 4);
            if (size < 0 || at + 8 + size > bytes.Length) return null;
            if (Encoding.ASCII.GetString(bytes, at, 4) == "CAMS" && size >= 120) return ReadCamera(bytes, at + 8, at + 8 + size);
            at += 8 + size;
        }
        return null;
    }

    static Shot ReadCamera(byte[] bytes, int start, int end)
    {
        float[] Floats(int at, int count) => [.. Enumerable.Range(0, count).Select(i => BitConverter.ToSingle(bytes, at + 4 * i))];
        Track? eye = null, target = null;
        for (var at = start + 120; at + 16 <= end;)
        {
            var (tag, count, kind) = (Encoding.ASCII.GetString(bytes, at, 4), BitConverter.ToInt32(bytes, at + 4), BitConverter.ToInt32(bytes, at + 8));
            var (width, values) = (tag == "KCRL" ? 1 : 3, kind > 1 ? 3 : 1);
            var keySize = 4 + 4 * width * values;
            at += 16;
            if (count < 0 || at + (long)count * keySize > end) break;
            var keys = Enumerable.Range(0, count).Select(i =>
            {
                var key = at + i * keySize;
                var all = Floats(key + 4, width * values);
                return (BitConverter.ToInt32(bytes, key), all[..width], values > 1 ? all[width..(2 * width)] : [], values > 1 ? all[(2 * width)..] : []);
            }).ToArray();
            at += count * keySize;
            if (tag == "KCTR") eye = new Track(kind, keys);
            else if (tag == "KTTR") target = new Track(kind, keys);
        }
        return new(Floats(start + 84, 3), Floats(start + 108, 3), eye, target);
    }

    // Where the model's eye or target is: the camera's place when it was set, the model's offsets at the milliseconds since, turned by the rotation field
    // as the step before saw it
    (float X, float Y, float Z) Cinematic(bool eye)
    {
        var (shot, start, x, y) = _cinematic!.Value;
        var (point, track) = eye ? (shot.Eye, shot.EyeTrack) : (shot.Target, shot.TargetTrack);
        var moved = _game.Step > start && track is not null ? track.At((_game.Step - start) * 1000.0 / Game.StepsPerSecond) : [0, 0, 0];
        var (px, py, pz, ground) = (point[0] + moved[0], point[1] + moved[1], point[2] + moved[2], Height(x, y));
        if (_game.Step <= start) return (x + px, y + py, ground + pz);
        var turn = (float)Radians(Field(5, _game.Step - 1));
        var (cos, sin) = ((float)Math.Cos(turn), (float)Math.Sin(turn));
        return (x + px * cos - py * sin, y + px * sin + py * cos, ground + pz + ZOffset);
    }

    /// <summary>Starts the first camera of the model file as the cinematic camera, playing at the camera's target from now; nothing for nil or a model with no camera.</summary>
    /// <remarks>Close: Only CameraZoomOut's eye was measured: its path translation as the game gave it (units-camera-2), the other models' tracks and a pan or a position set while one plays are read as the model gives them, not measured.</remarks>
    [Native(Level.Close, Fact = "probe units-camera-2 (11-08), D794", Reason = "Only CameraZoomOut's eye was measured: its path translation as the game gave it (units-camera-2), the other models' tracks and a pan or a position set while one plays are read as the model gives them, not measured")]
    public void SetCinematicCamera(string? cameraModelFile)
    {
        if (cameraModelFile is null) return;
        var file = cameraModelFile.Replace('\\', '/').ToLowerInvariant();
        if (!_shots.TryGetValue(Data.Graphics + file, out var shot))
            _shots[Data.Graphics + file] = shot = InstalledModel(file) is { } bytes ? ReadShot(bytes) : null;
        if (shot is null) return;
        _cinematic = null;
        _cinematic = (shot, _game.Step, TargetX, TargetY);
    }
}
