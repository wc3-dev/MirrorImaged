using MirrorImaged.Lua;

namespace MirrorImaged.Natives;

// In the game the maths natives give Lua's math.* numbers (facts, "Groups and performance"; probe strings), so they
// share its model (native/mifloat.c, decisions D151, D152, D480 to D484), but where the two differ in the game:
// SquareRoot is 0 below 0.001, and Asin, Acos and Atan2 give 0 outside their domains where math.* gives pi/2, 0 or pi.
public sealed partial class GameNatives
{
    const string NumbersFact = "facts, The number model's probes (probes numbers-sqrt, numbers-sqrt-2, numbers-squares, "
        + "numbers-trig, numbers-sin-2, numbers-fine, numbers-fine-2): every root measured, at each of the 19,195 places the "
        + "root steps too, and all 11,552 sines and cosines measured as the game's";

    /// <summary>As math.sqrt; 0 below 0.001, as the game gives.</summary>
    /// <remarks>Close: measured in part (facts, The number model's probes (probes numbers-sqrt, numbers-sqrt-2, numbers-squares, numbers-trig, numbers-sin-2, numbers-fine, numbers-fine-2): every root measured, at each of the 19,195 places the root steps too, and all 11,552 sines and cosines measured as the game's); values the probes did not reach come from the model of Lua's math.sqrt (D151, D152) and are not measured.</remarks>
    [Native(Level.Close, Fact = NumbersFact)]
    public float SquareRoot(float x) => x < 0.001f ? 0 : Native.mi_sqrtf(x);

    /// <summary>As <c>math.sin</c>, in the game's float arithmetic.</summary>
    /// <remarks>Close: measured in part (facts, The number model's probes (probes numbers-sqrt, numbers-sqrt-2, numbers-squares, numbers-trig, numbers-sin-2, numbers-fine, numbers-fine-2): every root measured, at each of the 19,195 places the root steps too, and all 11,552 sines and cosines measured as the game's); values outside the measured sines come from the model of Lua's math.sin (D151, D152) and are not measured.</remarks>
    [Native(Level.Close, Fact = NumbersFact)]
    public float Sin(float radians) => Native.mi_sinf(radians);

    /// <summary>As <c>math.cos</c>, in the game's float arithmetic.</summary>
    /// <remarks>Close: measured in part (facts, The number model's probes (probes numbers-sqrt, numbers-sqrt-2, numbers-squares, numbers-trig, numbers-sin-2, numbers-fine, numbers-fine-2): every root measured, at each of the 19,195 places the root steps too, and all 11,552 sines and cosines measured as the game's); values outside the measured cosines come from the model of Lua's math.cos (D151, D152) and are not measured.</remarks>
    [Native(Level.Close, Fact = NumbersFact)]
    public float Cos(float radians) => Native.mi_cosf(radians);

    /// <summary>Sin over Cos in the game's division (D480).</summary>
    /// <remarks>Close: sin over cos in the game's division, as the game's math.tan was measured (D480), so off where Sin or Cos is.</remarks>
    [Native(Level.Close)]
    public float Tan(float radians) => Native.mi_tanf(radians);

    /// <summary>As math.asin; 0 outside -1 to 1.</summary>
    /// <remarks>Close: the 0 outside the domain is the game's; inside it the model's values differ from the game's table, often by a float step or more (probe numbers-asin, D484).</remarks>
    [Native(Level.Close)]
    public float Asin(float y) => y is > 1 or < -1 ? 0 : Native.mi_asinf(y);

    /// <summary>As math.acos; 0 outside -1 to 1.</summary>
    /// <remarks>Close: the 0 outside the domain is the game's; inside it the model's values differ from the game's table, often by a float step or more (probe numbers-asin, D484).</remarks>
    [Native(Level.Close)]
    public float Acos(float x) => x is > 1 or < -1 ? 0 : Native.mi_acosf(x);

    const string AtanFact = "facts, The game's atan (probes numbers-atan, numbers-atan-2, numbers-functions, numbers-fine-2, "
        + "strings): all 262,144 values at every 2^-17 from 0 to 2 and every measured pair of atan(y, x), from D800's rational form";

    /// <summary>The arctangent of <paramref name="x"/>, from the game's own rational form (D800).</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, The game's atan (probes numbers-atan, numbers-atan-2, numbers-functions, numbers-fine-2, strings): all 262,144 values at every 2^-17 from 0 to 2 and every measured pair of atan(y, x), from D800's rational form).</remarks>
    [Native(Level.Exact, Fact = AtanFact)]
    public float Atan(float x) => Native.mi_atan2f(x, 1);

    /// <summary>As math.atan(y, x); Atan2(0, 0) is 0.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, The game's atan (probes numbers-atan, numbers-atan-2, numbers-functions, numbers-fine-2, strings): all 262,144 values at every 2^-17 from 0 to 2 and every measured pair of atan(y, x), from D800's rational form).</remarks>
    [Native(Level.Exact, Fact = AtanFact)]
    public float Atan2(float y, float x) => y == 0 && x == 0 ? 0 : Native.mi_atan2f(y, x);

    /// <summary>As Lua's x ^ power.</summary>
    /// <remarks>Close: the model of Lua's x ^ power (D151, D152); a power that isn't whole goes through log, and often differs from the game by a float step (probe numbers-sqrt).</remarks>
    [Native(Level.Close)]
    public float Pow(float x, float power) => Native.mi_powf(x, power);

    /// <summary>Times the float nearest pi/180, in the game's arithmetic: Deg2Rad(180) is 3.1415925.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Strings, numbers and hashtables (probes strings to strings-7)).</remarks>
    [Native(Level.Exact, Fact = StringsFact)]
    public float Deg2Rad(float degrees) => Native.mi_mul(degrees, (float)(Math.PI / 180));

    /// <summary>Times the float nearest 180/pi, in the game's arithmetic: Rad2Deg(1) is 57.2957802.</summary>
    /// <remarks>Exact: matches the game wherever measured (facts, Strings, numbers and hashtables (probes strings to strings-7)).</remarks>
    [Native(Level.Exact, Fact = StringsFact)]
    public float Rad2Deg(float radians) => Native.mi_mul(radians, (float)(180 / Math.PI));
}
