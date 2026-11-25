/*
** The game's float arithmetic, measured on 3.0.1.24371 (decisions, "The game's Lua numbers"). All on
** the bits with integers, or in doubles with + - * / only, so every platform gives the same bits.
*/
#define LUA_CORE

#include <stdint.h>
#include <string.h>

#include "lua.h"

static uint32_t bits (float f) { uint32_t u; memcpy(&u, &f, sizeof u); return u; }
static float flt (uint32_t u) { float f; memcpy(&f, &u, sizeof f); return f; }

/* Or-ed together with no range check, as the game packs a result: an exponent past the range carries into the
   sign bit (1e38 * 10 is -5.5e-39) and one below it borrows from it (2^-126 - (2^-126 + 2^-149) is -1.6e32). */
static float pack (uint32_t sign, int32_t e, uint32_t m) {
  return flt((sign << 31) | ((uint32_t)e << 23) | (m & 0x7fffff));
}

/* The sum or difference, cut toward zero; a number 23 or more octaves smaller, or a zero or subnormal one, is dropped.
   Of two of the same size the second is kept, so 0 + -0 is -0. The smaller number's mantissa is shifted into 24 + guard
   bits before it is added or taken away: the exact sum or difference for 23 guard bits (the add of the arithmetic
   measured so far), and with 1 the log's (decision D763). */
static float add_guarded (float x, float y, int guard) {
  uint32_t a = bits(x), b = bits(y);
  uint64_t ma, mb, m;
  int32_t ea, eb;
  if ((a & 0x7fffffff) <= (b & 0x7fffffff)) { uint32_t t = a; a = b; b = t; }
  ea = (a >> 23) & 0xff;
  eb = (b >> 23) & 0xff;
  if (eb == 0 || ea - eb >= 23) return flt(a);
  ma = (uint64_t)((a & 0x7fffff) | 0x800000) << guard;
  mb = ((uint64_t)((b & 0x7fffff) | 0x800000) << guard) >> (ea - eb);
  if ((a ^ b) >> 31) {
    m = ma - mb;
    if (m == 0) return 0.0f;
    while (m < (1ull << (23 + guard))) { m <<= 1; ea--; }
  }
  else {
    m = ma + mb;
    if (m >> (24 + guard)) { m >>= 1; ea++; }
  }
  return pack(a >> 31, ea, (uint32_t)(m >> guard));
}

float mi_add (float x, float y) { return add_guarded(x, y, 23); }

float mi_sub (float a, float b) {
  return mi_add(a, flt(bits(b) ^ 0x80000000));
}

/* The top 24 bits of the product. A zero or subnormal operand, or a biased exponent outside 1..256 before the
   mantissas' carry, gives +0 (1e20 * 1e20 is 0; 2^127 * 4 is -0 and 1.5 * 2^127 * 6 is -1.3e-38, packed). */
float mi_mul (float x, float y) {
  uint32_t a = bits(x), b = bits(y);
  int32_t ea = (a >> 23) & 0xff, eb = (b >> 23) & 0xff, e = ea + eb - 127;
  uint64_t m;
  if (ea == 0 || eb == 0 || (uint32_t)(e - 1) > 255) return 0.0f;
  m = (uint64_t)((a & 0x7fffff) | 0x800000) * ((b & 0x7fffff) | 0x800000);
  if (m >> 47) { m >>= 24; e++; }
  else m >>= 23;
  return pack((a ^ b) >> 31, e, (uint32_t)m);
}

/* The reciprocal of a divisor's mantissa m (2^23 to 2^24 - 1) in 2^-47: a table of 1024, the k-th 2^47 / (m - 1)
   cut, but the first, 1/1, which is 2^24, and one past the last 2^23; between two of them the game's straight line
   over the 8192 mantissas. */
static int64_t reciprocal_at (int64_t i) {
  return i ? (1ll << 47) / (((0x400 + i) << 13) - 1) : 1ll << 24;
}

/* The game's straight line between two table values lo and hi, at step f of the `parts` + 1 steps between them: the
   rise or fall times f is divided into `parts` (one fewer than the steps) and cut toward lo, a whole quotient too,
   which comes one short: the last step is one from hi (measured: every reciprocal, the sines by each zero). */
static int64_t between (int64_t lo, int64_t hi, int64_t f, int64_t parts) {
  int64_t n = (hi - lo) * f;
  return lo + (n < 0 ? n + 1 : n > 0 ? n - 1 : 0) / parts;
}

static uint64_t reciprocal (uint32_t m) {
  int64_t i = (m >> 13) & 0x3ff;
  return (uint64_t)between(reciprocal_at(i), reciprocal_at(i + 1), m & 0x1fff, 8191);
}

/* The dividend times the divisor's reciprocal. A number divided by the same bits is 1, so 0/0 and -0/-0 are 1;
   another zero or subnormal dividend gives +0. A zero divisor reads as 1.0 * 2^-127, so 1/0 is 2^127 and 100/0
   is past the range, 0. */
float mi_div (float x, float y) {
  uint32_t a = bits(x), b = bits(y);
  int32_t e = (int32_t)((a >> 23) & 0xff) - (int32_t)((b >> 23) & 0xff) + 126;
  uint64_t r, m;
  if (a == b) return 1.0f;
  if (!(a & 0x7f800000)) return 0.0f;
  r = reciprocal((b & 0x7fffff) | 0x800000);
  if (r >> 24) { r >>= 1; e++; }
  if ((uint32_t)(e - 1) > 255) return 0.0f;
  m = ((a & 0x7fffff) | 0x800000) * r;
  if (m >> 47) { m >>= 24; e++; }
  else m >>= 23;
  return pack((a ^ b) >> 31, e, (uint32_t)m);
}

static float trunc_ (float x) {
  uint32_t a = bits(x);
  int32_t e = (int32_t)((a >> 23) & 0xff) - 127;
  if (e < 0) return 0.0f;
  if (e >= 23) return x;
  return flt(a & ~(0x7fffffu >> e));
}

float mi_floorf (float x) {
  float t = trunc_(x);
  return x < t ? t - 1.0f : t;
}

/* A zero exponent reads as a fraction (1.0 * 2^-127), so ceil(0.0) and ceil(-0.0) are 1 as in the game. */
float mi_ceilf (float x) {
  uint32_t a = bits(x);
  int32_t e = (int32_t)((a >> 23) & 0xff) - 127;
  if (e >= 23 || (e >= 0 && !(a & (0x7fffffu >> e)))) return x;
  return mi_floorf(x) + 1.0f;
}

float mi_fmodf (float a, float b) {
  return mi_sub(a, mi_mul(trunc_(mi_div(a, b)), b));
}

static uint64_t isqrt (uint64_t n) {
  uint64_t r = 0, bit = 1ull << 62;
  while (bit > n) bit >>= 2;
  while (bit) {
    if (n >= r + bit) { n -= r + bit; r = (r >> 1) + bit; }
    else r >>= 1;
    bit >>= 2;
  }
  return r;
}

/* One more than the integer root of the mantissa in 32 bits, its 23 bits after the first followed by their top 8
   again (so 46341 to 65536), made a number from 1 below 2 by 1 + 46341 (r - 46341) / 2^31, cut toward zero; for an
   odd exponent, times the float nearest sqrt(2). A zero, subnormal or negative number gives 0. */
float mi_sqrtf (float x) {
  uint32_t a = bits(x);
  int32_t e = (int32_t)((a >> 23) & 0xff) - 127;
  uint64_t r;
  float s;
  if ((a >> 31) || e == -127) return 0.0f;
  r = isqrt((uint64_t)((a & 0x7fffff) | 0x800000) << 8 | (a & 0x7fffff) >> 15) + 1;
  s = pack(0, (e - (e & 1)) / 2 + 127, (uint32_t)((46341 * r - 4633) >> 8));
  return (e & 1) ? mi_mul(s, flt(0x3fb504f3)) : s;
}

/*
** Doubles with + - * / only, the same on every platform, cut to a float toward zero as the game's arithmetic
** cuts.
*/

static const double PI = 3.14159265358979311600e+00, PIO2 = 1.57079632679489655800e+00;

/* Toward zero, after a nudge of 2^-45 so that a result that should be exact (2^10, 10^3) stays exact. A
   nan is 0: its sign differs between x64 and arm64, and the game makes none. */
static float to_float (double d) {
  uint64_t u;
  int32_t e;
  uint32_t s;
  if (d != d) return 0.0f;
  d *= 1.0 + 0x1p-45;
  memcpy(&u, &d, sizeof u);
  e = (int32_t)((u >> 52) & 0x7ff) - 1023;
  s = (uint32_t)(u >> 63) << 31;
  if (e < -126) return 0.0f;
  if (e > 127) return flt(s | 0x7f800000);
  return flt(s | (uint32_t)(e + 127) << 23 | ((uint32_t)(u >> 29) & 0x7fffff));
}

static double sin_d (double x) {  /* x from 0 to pi */
  double k, r, z, ps, pc;
  int32_t n = (int32_t)(x * (2 / PI) + 0.5);
  k = n;
  r = ((x - k * 1.57079632673412561417e+00) - k * 6.07710050650619224932e-11) - k * 2.02226624879595063154e-21;
  z = r * r;
  ps = r + r * z * (-1.0 / 6 + z * (1.0 / 120 + z * (-1.0 / 5040 + z * (1.0 / 362880 + z * (-1.0 / 39916800
       + z * (1.0 / 6227020800.0 + z * (-1.0 / 1307674368000.0 + z * (1.0 / 355687428096000.0))))))));
  pc = 1 + z * (-1.0 / 2 + z * (1.0 / 24 + z * (-1.0 / 720 + z * (1.0 / 40320 + z * (-1.0 / 3628800
       + z * (1.0 / 479001600 + z * (-1.0 / 87178291200.0 + z * (1.0 / 20922789888000.0
       + z * (-1.0 / 6402373705728000.0)))))))));
  return n == 0 ? ps : n == 1 ? pc : -ps;
}

/* sin(pi k / 2048) in 2^-31, cut toward zero, for k 0 to 2048: the game's table, whose top is 2^31 - 1. */
static int64_t sine_at (uint32_t k) {
  if (k == 0 || k == 1024 || k == 2048) return k == 1024 ? 2147483647 : 0;
  return (int64_t)(sin_d(k * (PI / 2048)) * 2147483648.0);
}

/* A whole number of 2^-31 as a float, cut toward zero. */
static float q31 (int64_t v) {
  uint32_t sign = v < 0, u = (uint32_t)(sign ? -v : v);
  int32_t top = 30;
  if (!u) return 0.0f;
  while (!(u >> top)) top--;
  return pack(sign, top - 31 + 127, top > 23 ? u >> (top - 23) : u << (23 - top));
}

/* The game's sine: the angle in 2^-20 turns, x times the float nearest 1/(2 pi) in its arithmetic and cut toward
   zero, a quarter turn more for the cosine; then the table above for half a turn, the 256 steps between two of its
   values on the game's straight line, negated for the other half. So sin(pi), a step short of the half turn, is
   2^-31. */
static float sine (float x, uint32_t quarter) {
  uint32_t u = bits(mi_mul(x, flt(0x3e22f983))), p, i, f;
  uint64_t m = (u & 0x7fffff) | 0x800000;
  int32_t e = (int32_t)((u >> 23) & 0xff) - 127 - 3;  /* |u| * 2^20 = m * 2^e */
  int64_t v;
  if (!(u & 0x7f800000) || e >= 20) p = 0;
  else p = (uint32_t)((e >= 0 ? m << e : e > -32 ? m >> -e : 0) & 0xfffff);
  if (u >> 31) p = (0x100000 - p) & 0xfffff;
  p = (p + quarter) & 0xfffff;
  i = (p >> 8) & 0x7ff;
  f = p & 0xff;
  v = between(sine_at(i), sine_at(i + 1), f, 255);
  return q31(p >> 19 ? -v : v);
}

float mi_sinf (float x) { return sine(x, 0); }
float mi_cosf (float x) { return sine(x, 0x40000); }

/* The game's tan(0.5) is its sine over its cosine. */
float mi_tanf (float x) { return mi_div(mi_sinf(x), mi_cosf(x)); }

/*
** The rest is not pinned down by the measurements (Close), but for what each says.
*/

/* exp(x) for x of 0 and up, in the game's arithmetic: the float nearest e^(1/4) to the power of the whole quarters
   in x, squaring as it goes (so exp(1) is 402df853, a step under e, and exp(89) a nan, exp(100) 0, as its products
   are packed), times a polynomial of the rest r of degree 5, 1 + r (1 + r (c2 + r (c3 + r (c4 + r c5)))). The c's are
   not 1/2 ... 1/120 (c2 is 0.4999980, c5 0.0094192): worked out from the 131,072 values at every 2^-19 from 0 to a
   quarter, which they all give (decision D890). */
static float exp_up (float x) {
  static const uint32_t coefficient[] = { 0x3effffbd, 0x3e2ab479, 0x3d296ecb, 0x3c1a5314 };  /* c2 to c5 */
  float p = 1.0f, b = flt(0x3fa45af2), r, s = flt(coefficient[3]);
  uint32_t m;
  int32_t k;
  if (!(x < 1024)) return 0.0f;  /* and nan */
  m = (uint32_t)(x * 4);
  r = x - m * 0.25f;
  for (k = 2; k >= 0; k--) s = mi_add(flt(coefficient[k]), mi_mul(s, r));
  s = mi_add(1.0f, mi_mul(s, r));
  s = mi_add(1.0f, mi_mul(s, r));
  for (; m; m >>= 1) {
    if (m & 1) p = mi_mul(p, b);
    b = mi_mul(b, b);
  }
  return mi_mul(p, s);
}

/* Below 0 the game's exp is 1 over exp(-x) in its division, so exp(-104) is 2^127, as 1/0 is. */
float mi_expf (float x) { return x < 0 ? mi_div(1.0f, exp_up(-x)) : exp_up(x); }

/* The logarithm reads a zero or subnormal number as 1.m * 2^-127, as division reads a zero divisor, so log(0)
   is -88.0296860; below 0 it is 0, as sqrt(-1) is. inf and nan are read by their exponent bits like any number (not
   measured). */
static int bad_log (float x) { return (bits(x) >> 31) && (bits(x) & 0x7f800000); }

/* The game's log (decision D760): the exponent e and the mantissa m from 1 up to 2, s = (m - 1) / (m + 1) in the game's
   division, z = s s, log2 m = s (1 + c z) / (1 + b z) 2/ln 2 in its arithmetic in that order, and the log (e + log2 m)
   ln 2, the sum with one guard bit (below 1 its difference is cut one step larger than the exact one: 0.5 + k 2^-16, all
   32,768). All 131,072 values measured at 1 + k 2^-17 are the game's, as are the other scans and the whole numbers. */
float mi_logf (float x) {
  uint32_t a = bits(x);
  int32_t e = (int32_t)((a >> 23) & 0xff) - 127;
  float m, s, z, l, t;
  if (bad_log(x)) return 0.0f;
  m = flt((a & 0x7fffff) | 0x3f800000);
  s = mi_div(mi_sub(m, 1.0f), mi_add(m, 1.0f));
  z = mi_mul(s, s);
  l = mi_mul(mi_div(mi_mul(s, mi_add(1.0f, mi_mul(flt(0xbe88d424), z))), mi_add(1.0f, mi_mul(flt(0xbf19bf59), z))), flt(0x4038aa3b));
  t = add_guarded((float)e, l, 1);
  return mi_mul(t, flt(0x3f317218));
}

/* Only in the compat build, where the game has no math.log10: its log of a base is the log over the base's. */
float mi_log10f (float x) { return mi_div(mi_logf(x), mi_logf(10.0f)); }

/* A whole power from 1 up is multiplied out in the game's arithmetic, squaring as it goes (1000^10 is 7149f2c9,
   one by one would give 7149f2c8), so (-8)^3 is -512; any other is exp(y log|x|), so (-8)^(1/3) is 1.99999964 and
   (-8)^-1 is 0.125. 0^0 is 1, and 0 to any other power 0. */
float mi_powf (float x, float y) {
  float r = 1.0f, b = x;
  uint32_t n;
  if (y == 0) return 1.0f;
  if (!(bits(x) & 0x7f800000)) return 0.0f;
  if (y >= 1 && y < 0x1p31f && mi_floorf(y) == y) {
    for (n = (uint32_t)y; n; n >>= 1) {
      if (n & 1) r = mi_mul(r, b);
      b = mi_mul(b, b);
    }
    return r;
  }
  return mi_expf(mi_mul(y, mi_logf(flt(bits(x) & 0x7fffffff))));
}

/* The game's atan from 0 to 1 (decision D800): x (K + a z) / (1 + b z) with z = x x, and above 2 - sqrt 3 pi/6 plus that of
   (x - 1/sqrt 3) / (1 + x / sqrt 3), every step in the game's arithmetic in this order (all 131,072 values at every 2^-17). */
static float atan_rational (float x) {
  float z = mi_mul(x, x);
  return mi_div(mi_mul(x, mi_add(flt(0x3f7ffff0), mi_mul(flt(0x3e8415a6), z))), mi_add(1.0f, mi_mul(flt(0x3f17592e), z)));
}

static float atan_01 (float t) {
  if (t < flt(0x3e8930a3)) return atan_rational(t);
  return mi_add(flt(0x3f060a92), atan_rational(mi_div(mi_sub(t, flt(0x3f13cd3a)), mi_add(1.0f, mi_mul(t, flt(0x3f13cd3a))))));
}

/* |y| over |x| in the game's division, and above 1 pi/2 less the atan of its inverse (1 over it, a second division); then pi
   less it for x below 0, and its sign from y. */
static float atan2_game (float y, float x) {
  float ay = flt(bits(y) & 0x7fffffff), ax = flt(bits(x) & 0x7fffffff), a;
  float q = mi_div(ay, ax);
  if (q <= 1.0f) a = atan_01(q);
  else a = mi_sub(flt(0x3fc90fdb), atan_01(mi_div(1.0f, q)));
  if (x < 0) a = mi_sub(flt(0x40490fdb), a);
  return y < 0 ? flt(bits(a) | 0x80000000u) : a;
}

static double atan_d (double t) {  /* t >= 0 */
  int inv = t > 1, off;
  double z, p, s = 0;
  int32_t i;
  if (inv) t = 1 / t;
  off = t > 0.26794919243112270;
  if (off) t = (t * 1.73205080756887719318 - 1) / (t + 1.73205080756887719318);
  z = t * t;
  p = t;
  for (i = 1; i <= 33; i += 2) { s += (i & 2 ? -p : p) / i; p *= z; }
  if (off) s += PI / 6;
  return inv ? PIO2 - s : s;
}

static double atan2_d (double y, double x) {
  double ay = y < 0 ? -y : y, ax = x < 0 ? -x : x, a;
  if (ax == 0 && ay == 0) a = 0;
  else if (ax >= ay) a = atan_d(ay / ax);
  else a = PIO2 - atan_d(ax / ay);
  if (x < 0) a = PI - a;
  return y < 0 ? -a : a;
}

/* On an axis the game gives the floats nearest pi/2 and pi, and atan(0, 0) is pi/2. */
float mi_atan2f (float y, float x) {
  if (y == 0) return x < 0 ? flt(0x40490fdb) : x > 0 ? 0.0f : flt(0x3fc90fdb);
  if (x == 0) return flt(y < 0 ? 0xbfc90fdb : 0x3fc90fdb);
  return atan2_game(y, x);
}

/* The game's asin, from x = 0 to 1018/1024 (Close, decisions D712, D850): a table of 1025 whole numbers in 2^-29 radians, at
   k / 1024. The entries are the game's where its floats show every unit (k below 32: floor(asin(k / 1024) 2^29 + 1.45), and 1
   at 0) and up to 1018 the nearest whole numbers the asin and acos scans allow, within a few units of the curve; 1019 to 1023
   are not read (acos_near_1) and 1024 is acos's top. Between two
   entries lo and hi, at the fraction fr of the step, the game gives lo + floor((D + 1/2) fr + 1/2) for D = hi - lo, cut to a
   float toward zero (measured at every 1/1024 step for k below 31: 30,720 of 30,720). */
static const int32_t ASIN_TABLE[1025] = {
  1, 524289, 1048578, 1572867, 2097158, 2621451, 3145747, 3670046,
  4194348, 4718654, 5242964, 5767280, 6291601, 6815928, 7340262, 7864602,
  8388950, 8913306, 9437671, 9962045, 10486428, 11010821, 11535224, 12059639,
  12584065, 13108503, 13632954, 14157418, 14681895, 15206386, 15730892, 16255413,
  16779949, 17304501, 17829070, 18353656, 18878259, 19402881, 19927520, 20452179,
  20976858, 21501557, 22026276, 22551016, 23075778, 23600561, 24125368, 24650197,
  25175050, 25699927, 26224829, 26749756, 27274708, 27799686, 28324691, 28849724,
  29374783, 29899871, 30424988, 30950134, 31475309, 32000514, 32525750, 33051018,
  33576317, 34101648, 34627012, 35152409, 35677840, 36203305, 36728805, 37254340,
  37779910, 38305517, 38831161, 39356842, 39882561, 40408319, 40934115, 41459950,
  41985825, 42511741, 43037697, 43563695, 44089735, 44615817, 45141943, 45668111,
  46194324, 46720581, 47246883, 47773231, 48299624, 48826065, 49352552, 49879087,
  50405670, 50932302, 51458983, 51985713, 52512494, 53039325, 53566208, 54093143,
  54620129, 55147169, 55674262, 56201409, 56728610, 57255866, 57783178, 58310545,
  58837969, 59365450, 59892989, 60420585, 60948241, 61475955, 62003729, 62531563,
  63059458, 63587414, 64115432, 64643513, 65171656, 65699863, 66228133, 66756468,
  67284868, 67813333, 68341865, 68870463, 69399128, 69927861, 70456663, 70985533,
  71514472, 72043481, 72572560, 73101711, 73630933, 74160227, 74689593, 75219033,
  75748546, 76278133, 76807795, 77337533, 77867346, 78397235, 78927202, 79457246,
  79987368, 80517569, 81047848, 81578208, 82108647, 82639168, 83169770, 83700454,
  84231220, 84762069, 85293002, 85824019, 86355120, 86886307, 87417580, 87948939,
  88480385, 89011919, 89543540, 90075251, 90607050, 91138939, 91670919, 92202989,
  92735151, 93267405, 93799752, 94332192, 94864726, 95397353, 95930076, 96462895,
  96995809, 97528820, 98061928, 98595135, 99128439, 99661842, 100195345, 100728948,
  101262652, 101796457, 102330364, 102864374, 103398486, 103932702, 104467023, 105001448,
  105535979, 106070615, 106605358, 107140209, 107675167, 108210234, 108745409, 109280694,
  109816090, 110351596, 110887213, 111422943, 111958785, 112494740, 113030809, 113566993,
  114103291, 114639706, 115176236, 115712883, 116249648, 116786531, 117323532, 117860653,
  118397894, 118935255, 119472738, 120010342, 120548069, 121085918, 121623892, 122161989,
  122700212, 123238560, 123777034, 124315635, 124854364, 125393220, 125932206, 126471320,
  127010565, 127549940, 128089447, 128629085, 129168856, 129708760, 130248798, 130788970,
  131329278, 131869721, 132410301, 132951017, 133491872, 134032865, 134573996, 135115268,
  135656680, 136198233, 136739928, 137281764, 137823744, 138365868, 138908136, 139450549,
  139993108, 140535813, 141078664, 141621664, 142164812, 142708109, 143251556, 143795153,
  144338902, 144882802, 145426854, 145971060, 146515420, 147059934, 147604603, 148149428,
  148694410, 149239549, 149784846, 150330302, 150875918, 151421693, 151967629, 152513727,
  153059987, 153606410, 154152996, 154699747, 155246663, 155793745, 156340994, 156888409,
  157435993, 157983745, 158531667, 159079759, 159628021, 160176456, 160725062, 161273842,
  161822796, 162371924, 162921227, 163470707, 164020363, 164570197, 165120209, 165670400,
  166220771, 166771323, 167322056, 167872971, 168424068, 168975350, 169526815, 170078466,
  170630303, 171182326, 171734537, 172286936, 172839524, 173392302, 173945270, 174498430,
  175051781, 175605326, 176159065, 176712997, 177267126, 177821450, 178375971, 178930690,
  179485607, 180040724, 180596041, 181151559, 181707278, 182263200, 182819325, 183375655,
  183932190, 184488930, 185045877, 185603032, 186160395, 186717967, 187275749, 187833742,
  188391947, 188950364, 189508994, 190067839, 190626899, 191186175, 191745668, 192305378,
  192865307, 193425455, 193985823, 194546413, 195107225, 195668259, 196229517, 196791000,
  197352709, 197914643, 198476806, 199039196, 199601815, 200164665, 200727745, 201291057,
  201854602, 202418380, 202982393, 203546642, 204111127, 204675849, 205240809, 205806008,
  206371448, 206937128, 207503051, 208069216, 208635625, 209202279, 209769179, 210336325,
  210903719, 211471362, 212039254, 212607396, 213175790, 213744437, 214313337, 214882491,
  215451901, 216021567, 216591491, 217161673, 217732114, 218302816, 218873779, 219445004,
  220016493, 220588246, 221160265, 221732550, 222305102, 222877923, 223451013, 224024374,
  224598007, 225171912, 225746091, 226320545, 226895274, 227470280, 228045564, 228621127,
  229196970, 229773094, 230349501, 230926190, 231503164, 232080423, 232657969, 233235803,
  233813925, 234392337, 234971040, 235550035, 236129323, 236708905, 237288783, 237868957,
  238449429, 239030200, 239611271, 240192643, 240774317, 241356295, 241938577, 242521164,
  243104059, 243687262, 244270774, 244854596, 245438731, 246023177, 246607938, 247193015,
  247778406, 248364117, 248950146, 249536495, 250123166, 250710159, 251297475, 251885117,
  252473085, 253061380, 253650004, 254238959, 254828244, 255417862, 256007814, 256598101,
  257188724, 257779685, 258370985, 258962625, 259554607, 260146932, 260739601, 261332615,
  261925976, 262519686, 263113745, 263708155, 264302917, 264898033, 265493504, 266089331,
  266685516, 267282060, 267878965, 268476231, 269073861, 269671855, 270270215, 270868943,
  271468040, 272067508, 272667347, 273267559, 273868146, 274469109, 275070450, 275672170,
  276274270, 276876753, 277479619, 278082870, 278686507, 279290533, 279894948, 280499754,
  281104953, 281710546, 282316535, 282922921, 283529705, 284136890, 284744477, 285352468,
  285960863, 286569665, 287178876, 287788496, 288398528, 289008973, 289619833, 290231109,
  290842804, 291454918, 292067453, 292680411, 293293794, 293907604, 294521841, 295136509,
  295751608, 296367140, 296983107, 297599511, 298216353, 298833635, 299451360, 300069527,
  300688141, 301307202, 301926712, 302546673, 303167087, 303787955, 304409279, 305031062,
  305653305, 306276009, 306899178, 307522812, 308146913, 308771484, 309396527, 310022042,
  310648033, 311274501, 311901449, 312528877, 313156788, 313785184, 314414068, 315043440,
  315673303, 316303659, 316934510, 317565859, 318197706, 318830055, 319462907, 320096264,
  320730129, 321364503, 321999389, 322634789, 323270704, 323907138, 324544092, 325181569,
  325819570, 326458098, 327097154, 327736744, 328376865, 329017523, 329658719, 330300455,
  330942733, 331585557, 332228927, 332872845, 333517319, 334162346, 334807928, 335454070,
  336100773, 336748040, 337395873, 338044275, 338693248, 339342794, 339992916, 340643617,
  341294899, 341946764, 342599215, 343252255, 343905886, 344560111, 345214932, 345870352,
  346526374, 347183000, 347840233, 348498075, 349156530, 349815599, 350475286, 351135594,
  351796525, 352458081, 353120267, 353783084, 354446535, 355110624, 355775352, 356440724,
  357106741, 357773407, 358440725, 359108697, 359777327, 360446618, 361116572, 361787191,
  362458483, 363130446, 363803085, 364476402, 365150402, 365825087, 366500460, 367176525,
  367853284, 368530741, 369208900, 369887763, 370567333, 371247615, 371928611, 372610325,
  373292760, 373975919, 374659806, 375344425, 376029779, 376715870, 377402704, 378090283,
  378778611, 379467692, 380157528, 380848125, 381539485, 382231612, 382924510, 383618182,
  384312633, 385007866, 385703885, 386400694, 387098296, 387796697, 388495897, 389195905,
  389896721, 390598351, 391300799, 392004068, 392708163, 393413087, 394118845, 394825442,
  395532880, 396241167, 396950302, 397660295, 398371146, 399082862, 399795445, 400508901,
  401223236, 401938451, 402654553, 403371546, 404089435, 404808224, 405527917, 406248520,
  406970038, 407692475, 408415836, 409140125, 409865349, 410591511, 411318617, 412046672,
  412775681, 413505648, 414236580, 414968481, 415701356, 416435212, 417170051, 417905883,
  418642709, 419380538, 420119372, 420859219, 421600085, 422341973, 423084891, 423828845,
  424573839, 425319879, 426066972, 426815123, 427564337, 428314624, 429065985, 429818430,
  430571964, 431326592, 432082322, 432839158, 433597110, 434356181, 435116380, 435877711,
  436640183, 437403802, 438168574, 438934507, 439701607, 440469881, 441239335, 442009979,
  442781818, 443554859, 444329109, 445104577, 445881268, 446659193, 447438357, 448218767,
  449000432, 449783359, 450567557, 451353032, 452139794, 452927850, 453717208, 454507876,
  455299863, 456093177, 456887826, 457683819, 458481165, 459279872, 460079949, 460881403,
  461684248, 462488489, 463294135, 464101197, 464909683, 465719603, 466530966, 467343783,
  468158062, 468973814, 469791048, 470609775, 471430005, 472251747, 473075012, 473899812,
  474726155, 475554052, 476383517, 477214557, 478047185, 478881412, 479717248, 480554707,
  481393797, 482234532, 483076925, 483920985, 484766726, 485614159, 486463297, 487314152,
  488166736, 489021065, 489877148, 490735000, 491594634, 492456063, 493319301, 494184361,
  495051257, 495920004, 496790615, 497663106, 498537489, 499413781, 500291995, 501172148,
  502054254, 502938328, 503824388, 504712448, 505602524, 506494632, 507388790, 508285013,
  509183319, 510083725, 510986248, 511890906, 512797717, 513706697, 514617867, 515531245,
  516446848, 517364695, 518284809, 519207207, 520131908, 521058933, 521988301, 522920037,
  523854157, 524790686, 525729642, 526671051, 527614932, 528561307, 529510202, 530461640,
  531415642, 532372233, 533331437, 534293279, 535257784, 536224978, 537194886, 538167533,
  539142948, 540121157, 541102187, 542086066, 543072822, 544062486, 545055083, 546050648,
  547049206, 548050790, 549055433, 550063164, 551074016, 552088022, 553105215, 554125630,
  555149300, 556176262, 557206550, 558240200, 559277251, 560317739, 561361705, 562409185,
  563460219, 564514850, 565573116, 566635062, 567700729, 568770162, 569843404, 570920501,
  572001499, 573086447, 574175389, 575268378, 576365462, 577466694, 578572121, 579681803,
  580795789, 581914138, 583036904, 584164145, 585295922, 586432292, 587573320, 588719066,
  589869598, 591024976, 592185272, 593350554, 594520892, 595696357, 596877026, 598062970,
  599254268, 600451000, 601653246, 602861093, 604074618, 605293915, 606519071, 607750178,
  608987331, 610230627, 611480162, 612736040, 613998368, 615267249, 616542796, 617825120,
  619114342, 620410579, 621713954, 623024594, 624342632, 625668201, 627001441, 628342495,
  629691509, 631048637, 632414034, 633787865, 635170294, 636561497, 637961650, 639370939,
  640789555, 642217695, 643655566, 645103377, 646561352, 648029714, 649508704, 650998564,
  652499553, 654011932, 655535979, 657071980, 658620234, 660181052, 661754759, 663341693,
  664942208, 666556673, 668185473, 669829014, 671487721, 673162032, 674852420, 676559369,
  678283395, 680025040, 681784875, 683563501, 685361550, 687179699, 689018654, 690879175,
  692762055, 694668148, 696598357, 698553650, 700535056, 702543679, 704580701, 706647394,
  708745123, 710875365, 713039719, 715239904, 717477812, 719755489, 722075173, 724439329,
  726850664, 729312174, 731827183, 734399390, 737032961, 739732569, 742503517, 745351870,
  748284579, 751309713, 754436718, 757676739, 761043141, 764552123, 768223673, 772082918,
  776162222, 780504446, 785168462, 790239031, 795846264, 802209199, 809754962, 819586360,
  843314857,
};

static float q29 (int64_t v) {
  uint32_t u = (uint32_t)v;
  int32_t top = 30;
  if (v <= 0) return 0.0f;
  while (!(u >> top)) top--;
  return pack(0, top - 29 + 127, top > 23 ? u >> (top - 23) : u << (23 - top));
}

/* x from 0 to 1 less 2^-13: the asin in 2^-29 radians, and with `from_top` what the acos is from the top (see below). */
static int64_t asin_units (double x, int from_top) {
  double xi = x * 1024, f, d;
  int32_t k = (int32_t)xi;
  int64_t lo = ASIN_TABLE[k];
  f = xi - k;
  d = (double)(ASIN_TABLE[k + 1] - lo);
  if (from_top) return lo + (int64_t)__builtin_floor((2 * d + 1) * f / 2);
  return k == 0 && f == 0 ? 0 : lo + (int64_t)__builtin_floor(((2 * d + 1) * f + 1) / 2);
}

/* Near 1 (x above 1018/1024) acos is a straight line between the floats nearest acos(1 - w) at 8 points an octave of w,
   w = 2^e (1 + i/8) for e from -22 (decision D850). The game's w is 1 - x and a step of x more (2^-24), so x = 1 - 4 2^-24
   and above give 0 (w at most 2^-22); between two points lo and hi it is lo + (hi - lo) f + c worked out exactly and cut
   toward zero to a float, f the low 20 bits of w's mantissa over 2^20 and c a constant from 3 2^-36 to 4 2^-36 (D880, the
   middle taken): the game's line carries more bits than a float and sits that much above the floats' line, by the same
   amount in every octave. All 2,378 values of asin and acos above 1018/1024 in the scans are that. */
static const uint32_t ACOS_NEAR_1[121] = {
  0x3a3504f3, 0x3a400000, 0x3a4a62c2, 0x3a544395, 0x3a5db3d8, 0x3a66c15b, 0x3a6f7751, 0x3a77def6,
  0x3a800000, 0x3a87c3b7, 0x3a8f1bbd, 0x3a9617e3, 0x3a9cc471, 0x3aa32b2c, 0x3aa953fe, 0x3aaf456f,
  0x3ab504f4, 0x3ac00001, 0x3aca62c3, 0x3ad44396, 0x3addb3d9, 0x3ae6c15c, 0x3aef7753, 0x3af7def8,
  0x3b000001, 0x3b07c3b8, 0x3b0f1bbf, 0x3b1617e5, 0x3b1cc473, 0x3b232b2e, 0x3b295400, 0x3b2f4572,
  0x3b3504f7, 0x3b400005, 0x3b4a62c7, 0x3b54439b, 0x3b5db3de, 0x3b66c162, 0x3b6f7759, 0x3b77deff,
  0x3b800005, 0x3b87c3bd, 0x3b8f1bc4, 0x3b9617eb, 0x3b9cc47a, 0x3ba32b36, 0x3ba9540a, 0x3baf457c,
  0x3bb50502, 0x3bc00012, 0x3bca62d7, 0x3bd443ad, 0x3bddb3f3, 0x3be6c179, 0x3bef7773, 0x3bf7df1c,
  0x3c000015, 0x3c07c3d0, 0x3c0f1bdb, 0x3c161805, 0x3c1cc498, 0x3c232b57, 0x3c29542f, 0x3c2f45a5,
  0x3c350530, 0x3c400048, 0x3c4a6316, 0x3c5443f6, 0x3c5db446, 0x3c66c1d7, 0x3c6f77dc, 0x3c77df90,
  0x3c800055, 0x3c87c41c, 0x3c8f1c34, 0x3c96186c, 0x3c9cc50d, 0x3ca32bdc, 0x3ca954c3, 0x3caf464a,
  0x3cb505e5, 0x3cc00120, 0x3cca6413, 0x3cd4451a, 0x3cddb593, 0x3ce6c34e, 0x3cef797f, 0x3cf7e161,
  0x3d000155, 0x3d07c54e, 0x3d0f1d9a, 0x3d161a09, 0x3d1cc6e4, 0x3d232dee, 0x3d295714, 0x3d2f48db,
  0x3d3508b9, 0x3d400480, 0x3d4a6807, 0x3d5449aa, 0x3d5dbac5, 0x3d66c92b, 0x3d6f800c, 0x3d77e8a5,
  0x3d800556, 0x3d87ca14, 0x3d8f2332, 0x3d96207d, 0x3d9cce3f, 0x3da33639, 0x3da96059, 0x3daf5323,
  0x3db5140c, 0x3dc01205, 0x3dca77dd, 0x3dd45bef, 0x3dddcf97, 0x3de6e0a5, 0x3def9a4a, 0x3df805c1,
  0x3e00155f,
};

static float acos_near_1 (float x) {
  float w = mi_add(mi_sub(1.0f, x), 0x1p-24f);
  uint32_t b = bits(w), i;
  float lo, hi;
  if (w <= 0x1p-22f) return 0.0f;
  i = ((b >> 23) - (127 - 22)) * 8 + ((b >> 20) & 7);
  lo = flt(ACOS_NEAR_1[i]);
  hi = flt(ACOS_NEAR_1[i + 1]);
  {
    /* exact in a double: lo and hi have 24 bits, f 20, and the sum spans at most 52 */
    double d = (double)lo + ((double)hi - (double)lo) * ((double)(b & 0xfffff) * 0x1p-20) + 0x7p-37;
    float r = (float)d;
    return (double)r > d ? flt(bits(r) - 1) : r;
  }
}

/* From 1 and past it asin is the float nearest pi/2 (asin(2) too) and acos 0; from -1 down, -pi/2 and pi. A number below 0
   gives a unit less than the table's (the asin of -x is minus it, the acos of -x pi/2 and it: measured at k / 64, all 63 of
   63), and the acos of x from 0 is pi/2 less the line with its rounding half a unit lower (64 of 64). Above 1018/1024 both
   are acos_near_1's: asin pi/2 less it in the game's subtraction. Below -1018/1024 they are taken to be minus the asin and
   pi less the acos (not measured). */
float mi_asinf (float x) {
  float a = x < 0 ? -x : x;
  if (x != x) return 0.0f;  /* as before D850, never into acos_near_1 (its index would be off the table) */
  if (x >= 1 || x <= -1) return flt(x < 0 ? 0xbfc90fdb : 0x3fc90fdb);
  if (a <= 1018.0f / 1024) {
    int64_t g = asin_units(a, 0);
    float r;
    if (x < 0 && g) g--;
    r = q29(g);
    return x < 0 ? flt(bits(r) | 0x80000000u) : r;
  }
  {
    float r = mi_sub(flt(0x3fc90fdb), acos_near_1(a));
    return x < 0 ? flt(bits(r) | 0x80000000u) : r;
  }
}

float mi_acosf (float x) {
  float a = x < 0 ? -x : x;
  if (x != x) return 0.0f;
  if (x >= 1 || x <= -1) return x < 0 ? flt(0x40490fdb) : 0.0f;
  if (a <= 1018.0f / 1024) {
    int64_t g = x < 0 ? asin_units(a, 0) : asin_units(a, 1);
    if (x < 0 && g) g--;
    return q29(x < 0 ? ASIN_TABLE[1024] + g : ASIN_TABLE[1024] - g);
  }
  return x < 0 ? mi_sub(flt(0x40490fdb), acos_near_1(a)) : acos_near_1(a);
}

/* inf and nan are written here, as the C library writes them but the same on every platform: the sign, or + or
   a space for those flags, in capitals for %E %F %G %A, padded with spaces to the width */
int mi_snprintf (char *s, size_t sz, const char *f, double n) {
  uint64_t u;
  const char *flags = f + 1, *p = flags;
  char text[8];
  int width = 0, upper;
  memcpy(&u, &n, sizeof u);
  if (((u >> 52) & 0x7ff) != 0x7ff) return snprintf(s, sz, f, n);
  while (*p && strchr("-+ #0", *p)) p++;
  while (*p >= '0' && *p <= '9') width = width * 10 + (*p++ - '0');
  while (*p == '.' || (*p >= '0' && *p <= '9')) p++;
  upper = *p >= 'A' && *p <= 'Z';
  snprintf(text, sizeof text, "%s%s", (u >> 63) ? "-" : memchr(flags, '+', (size_t)(p - flags)) ? "+"
           : memchr(flags, ' ', (size_t)(p - flags)) ? " " : "",
           (u << 12) ? (upper ? "NAN" : "nan") : (upper ? "INF" : "inf"));
  return snprintf(s, sz, memchr(flags, '-', (size_t)(p - flags)) ? "%-*s" : "%*s", width, text);
}
