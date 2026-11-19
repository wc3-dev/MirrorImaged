# Decisions

Each decision has a number, the choice and the reason. D1 to D11 are the plan's (section 2); those marked *(Sven)* are
his, the rest the agents' and open to his review.

## From the plan

- **D1.** MirrorImaged is a new repository in `wc3-dev`, using War3Net as a package to read map files. *(Sven)*
- **D2.** The name is MirrorImaged. *(Sven)*
- **D3.** Tests are written in C# and Lua, both in 1.0. *(Sven)*
- **D4.** JASS maps run, turned into Lua by War3Net's transpiler at load time. *(Sven)*
- **D5.** Everything in the plan is 1.0; nothing is held back for a later version. *(Sven)*
- **D6.** The approach is a copy of the game's rules, not the real game run hidden as warcraftsim does. *(Sven, 11-08)*
- **D7.** It is built autonomously, by subagents, as `docs/tasks/how-tasks-run.md` sets out. *(Sven)*
- **D8.** The command-line tool is `mirror`. Short, and the word the name starts with.
- **D9.** The licence is MIT, as Moonwell's, so code can move between the two.
- **D10.** The library and the command line run on Windows, Linux and macOS; the comparison against the game runs on
  Windows, the only platform the game runs on here.
- **D11.** .NET is net10.0, the version Moonwell uses.

## Made by the tasks

- **D12.** The game-files check's test fixtures are git repositories the tests make in a temporary folder, not tracked
  files: a tracked fixture with a game extension or signature would fail the check on this repository. (W0-1)
- **D13.** The game-files check also fails on a file starting with `HM3W`, the header a map puts before its MPQ
  archive, so a map renamed to another extension is caught too. (W0-1)
- **D14.** The game-files check also fails on the sound extensions `.wav`, `.mp3`, `.flac` and `.ogg`, since the rules
  keep the game's sounds out of git and a sound has no signature the check looks for. (W0-1)
- **D15.** The game-files check reads each file's content from git's index, not the disk, so it checks what a commit
  holds. A path git can't give the content of (a submodule, a missing object) fails, and so does a Git LFS pointer,
  whose real content git doesn't hold: the repository uses neither. (W0-1)
- **D16.** The natives are parsed with the upstream `War3Net.CodeAnalysis.Jass` 6.0.2 from nuget.org: it parses the
  installed 3.0.1.24371 `common.j` and `common.ai` whole, so no fork is needed. (W0-2)
- **D17.** The counts in 3.0.1.24371: `common.j` declares 139 handle types and 1,712 natives, `common.ai` no types and
  123 natives, none of them in both. With `handle` that makes 140 classes and 1,835 natives, all Missing after W0-2.
  The plan's 1,243 natives were 1.32.9's. (W0-2)
- **D18.** The natives are instance methods of one class, `GameNatives`, so each simulation has its own. It is not
  called `Natives`, which would clash with its namespace `MirrorImaged.Natives`. (W0-2)
- **D19.** The handle classes keep `common.j`'s names (`unit`, `@event`, `equipmentType`), so a signature reads as
  `common.j` declares it and the Lua layer and the checks match by name. Their file turns off CS8981, the warning for
  lower-case type names. They are `partial`, so later tasks add members (ids, the world) in their own files. (W0-2)
- **D20.** JASS types in C#: `integer` is `int`, `real` is `float` (the game's single floats), `boolean` is `bool`, and
  `string`, the handles and `code` are nullable, since JASS and Lua pass null for any of them. `code` is `Action?`,
  except where the game reads what the function returns: `Func<bool>?` for `Condition` and `Filter`, and `Func<int>?`
  for `common.ai`'s `SetHeroLevels`. In 3.0.1.24371's storage, `War3.w3mod:Scripts/human.ai`, `orc.ai`, `undead.ai`
  and `elf.ai` each call `StandardAI(function SkillArrays, ...)`; `common.ai` declares `SkillArrays takes nothing
  returns integer` and `StandardAI` passes that `code` to `SetHeroLevels`, as `CampaignAI` passes its own. (W0-2)
- **D21.** The generator writes `Types.g.cs`, `CommonJNatives.g.cs` and `CommonAiNatives.g.cs`, each sorted by name
  (ordinal), with `\n` line endings whatever the generator's own checkout has, and a header naming the game version,
  so a re-run on the same install gives the same files. `.gitattributes` keeps `*.g.cs` at LF in every checkout. A
  `Needs=Game` test checks the committed files are what the generator writes. `* text=auto` was left out: it changes
  nothing where `core.autocrlf` is set, and it would reach every other task's files. (W0-2)
- **D22.** A native is filled in by writing it by hand in `MirrorImaged.Natives`, outside `Generated`, in the partial
  `GameNatives`, and re-running the generator: it leaves out every method marked `[Native(...)]` right before its
  `public` in the project's other files, passing over comments and strings. The one-to-one test reads all the natives
  by reflection, so it holds as they move out of the stubs. (W0-2)
- **D23.** The Exact native's probe or fact is the attribute's `Fact`. That a Recorded native names a `Reason` and an
  Exact one a `Fact` is a test over every native, since an attribute can't require a named argument. (W0-2)
- **D24.** The CASC reader is Moonwell's, reading a file by its root path (`War3.w3mod:scripts/common.j`), without
  Moonwell's graphics tiers and locales, which the scripts don't need, and without its lock: nothing here reads from
  several threads. A failed open closes what it opened; an I/O error comes out as it is, anything else wrapped in an
  `IOException` naming the folder. The install is the folder given, else `MIRROR_GAME`, else
  `C:\Program Files (x86)\Warcraft III`. (W0-2)
- **D25.** `mirror coverage` prints the count at each level and in all, then every Missing native, one a line, sorted.
  (W0-2)
- **D26.** The melee AI scripts are stored three times in 3.0.1.24371: `War3.w3mod:Scripts/` (`human.ai` 22,453 bytes),
  `War3.w3mod:_Balance/Melee_V0.w3mod:Scripts/` (17,331) and `War3.w3mod:_Balance/Custom_V0.w3mod:Scripts/` (17,267),
  each with `orc.ai`, `undead.ai` and `elf.ai`; `common.ai` and `common.j` are only in `War3.w3mod:Scripts/`. Which
  copy a melee game loads is not known: wave 7 (the AI) must find out, by a probe, before it runs one. (W0-2)

### The game's Lua numbers (W0-3)

The measurements are the facts' "Lua runtime" section and the two probe outputs behind it (`docs/facts.md`, "Lua
numbers"). Each model was found by trying candidates against every measured case in Python first; `native/mifloat.c`
is the result. A float is read in the IEEE layout throughout: the game's `string.pack('<f')` gives those bits.

- **D27. Addition and subtraction** align the smaller number with one guard bit and cut the result toward zero; a number
  23 or more octaves smaller (under two float steps) is dropped, a zero or subnormal operand too. Matches every measured
  case: the N1 and N7 sums and ties, the ten "adding k float steps" rows, the ten loops, the twenty sums of up to a
  million additions to the last bit, the four stalls with their exact counts, and `1 - cos(0)` = 2^-24. Rejected:
  plain truncation (`2^24 + 3` gives 2^24 + 2), pre-shifting both operands (`33000.01 + 0.004` loses a step), aligning
  the smaller to two steps (adding 3 steps gives 2), no guard bit (`1 - cos(0)` gives 2^-23; for additions one guard bit
  and none always agree). Not measured: subtractions that shift out two or more bits, sums past the range. (Measured
  since, and the subtraction replaced: D151.)
- **D28. Multiplication** keeps the top 24 bits of the 48-bit product (toward zero); a zero or subnormal operand gives +0.
  A result's biased exponent is kept from 1 to 256 and packed as it is, so 255 and 256 carry into the inf, nan and sign
  bits; anything else is +0. Measured: 256 kept (`1e38 * 10` = -5.5e-39, `(1 / 0) * 4` = -0), 259 and 260 zero
  (`1e20 * 1e20`, `100 / 0`); 257 and 258 aren't, and fall on the zero side. Matches all of N7's products, nine r*r
  products, the `10 * 0.7` family and the limits. (Replaced in part by D151: the range is checked before the carry.)
- **D29. Division** is the dividend times a reciprocal, R = ⌊(2^47 + 0xCC0000) / m⌋ with m the divisor's 24-bit mantissa
  without its lowest bit, cut toward zero, with no zero check: zero reads as 1.0 × 2^-127, so `0/0` is 1, `1/0` is
  2^127 and `100/0` is past the range, 0. Matches the twelve measured quotients, `3/5`, `7.2 % 0.1` = 0, 21 of
  the 23 roots after a correction step, and the 140 wrong casts of 4,200 in `n * (k / 100)`. Rejected: exact
  truncation (`1/3`), rounding up (`1/5`), Newton-Raphson reciprocals in the game's arithmetic from 2- to 12-bit tables
  (no quotient set matched), a table with one exact Newton step plus a bias. **Close:** `(int)(n / d)` for n 1-2000, d
  2-100 is wrong 268 times in the game and 102 in the model, `Ceiling` 893 and 956; the correction step after the roots
  of 3 and 1,000,000 is one step lower than the game's. (Replaced by D151.)
- **D30. floor, ceil and fmod.** floor is exact. ceil reads a zero exponent as a fraction, so `math.ceil(0.0)` and
  `math.ceil(-0.0)` are 1 as in the game. fmod is `a - trunc(a / b) * b` in the game's arithmetic (exact fmod gives
  0.0999997 for 7.2 and 0.1; the game 0), and Lua's float `%` uses it.
- **D31. sqrt** is the exact root of the mantissa reduced to [1, 4) (the error repeats every two octaves), times
  1 + E(m), with E linear between the errors measured at ten mantissas (seventeen roots) and 0 at 1 and 4. Every
  measured root is bit for bit the game's, powers of 4 are exact and a larger value never gives a smaller root.
  **Close** between the measured mantissas: the game's worst is 1.93e-5 high in both octaves and 0.09e-5 low, the
  model's 1.93e-5 (at 2.25) and 1.34e-5 below 2, never low, and `(int)Sqrt(n * n)` for n 1-4096 is wrong 64 times in
  the game and never here. The algorithm wasn't found: the error jumps between near mantissas (3 gives 0.2e-5, 3.0625
  gives 1.67e-5), which neither a Newton step from straight lines over [1, 2], [2, 3], [3, 4] nor a table and a Newton
  step reproduce. (Replaced by D152: found, exact.)
- **D32. sin, cos, tan, asin, acos, atan, exp, log and pow are Close.** They are computed in doubles with + - × ÷ only
  (no C library), then cut to a float toward zero after a nudge of 2^-45, so that exact results (2^10, 10^3) stay exact.
  sin and cos go through a 31-bit fraction as the game's do (cos 0 = 1 - 2^-24, sin 90° the same), and tan is sin over
  cos, as the game's `tan(0.5)` is. Exact on `log(10)`, `asin(0.5)`, `acos(0.5)`, `2^10`, `10^2`, `10^3`, `1.5^2`,
  `2^0.5`, cos 0, sin 90°, `1 - cos(0)`. Off: sin, cos and tan of 0.5 by up to 7.4e-6 (the game's angle is about
  5.6e-6 rad off there), `exp(1)`, `log(2)` and `atan(1)` by one or two float steps, `log10(1000)` by 8.1e-6, `x^0.5`
  by the game's own up to 1.35e-5 low, sin π (2^-31 in the game, -8.7e-8 here), and the 720 whole-degree sines and
  cosines by up to 5.3e-6 (18 the same). Tried for sin: an angle in 2^14 to 2^20 units a turn, floored or rounded, into
  an exact sine; at most 19 of the 720 matched. `math.log(x, 2)` is `log(x) / log(2)` in the game's arithmetic (D33),
  so `math.log(8, 2)` is 3.00000024. Outside their domains the functions give 0, as `math.sqrt(-1)` does; `log(0)` is
  -inf; a nan result is 0. (Replaced in part by D152: sin, cos, the domains' ends, exp below 0, log(0) and the powers.)
- **D33. One configuration on every platform: Windows's.** Lua's `luaconf.h` gives a Windows build `LUA_USE_C89` and
  others C99 paths, which differ in what a script sees: `math.log(x, 2)`, hex numerals, `%a` and `os.date`'s options.
  Every platform is built as a stock Windows build is (`-DLUA_USE_C89` and Windows's `os.date` options), chosen to
  match the likely Windows build of the game; to be settled by a probe of `math.log(8, 2)` (here 3.00000024),
  `math.log(3, 2)` (1.58496273), `0x1.0000011p0` (1: Lua's own reader adds the digits in floats), `string.format('%a',
  0.1)` (`0xc.ccccdp-7`, Lua's own writer) and `os.date('%F')` (refused; `%#x` is taken). `LUA_USE_POSIX` stays on
  Linux and macOS, where it only makes the time functions thread-safe.
- **D34. Number text and parsing.** `tostring` is `%.7g` (Lua's default for floats; the game's `1/0` reads
  1.701412e+38). inf and nan are written by the library, as the C library writes them (sign or `+` or space, capitals
  for `%G` and the like, padded to the width) but the same on every platform; the game writes `nan` for `MaxValue * 2`.
  Decimal numerals are parsed with `strtod` and cast to a float, since `strtof` differs between C libraries. An integer
  becomes a float by the C cast (to nearest); not measured.
- **D35. The Lua build.** Lua 5.3.6 without `LUA_COMPAT_5_2`, so no `math.pow`, `bit32` or `unpack`: no fact shows them
  in the game, and a probe can settle it. Changed in the vendored sources, and nowhere else: `luaconf.h` (`LUA_32BITS`,
  the include of `mifloat.h`, Windows's `os.date` options, a fixed `table.sort` pivot instead of the clock, pointers
  written as arena offsets), `lstate.c` (the string hash seed is 0 instead of the time and addresses; the game's seed
  is unknown; replaced by D154: the game's), `lmathlib.c` (`math.log` with a base, `math.modf`, `math.rad` and `math.deg` use the game's arithmetic
  instead of C operators), `ldo.c` (binary chunks refused, D36), `lapi.c` and `ltable.c` (a C function is hashed and
  written by its place in the library, D39), `loslib.c` (the time functions on the simulated clock, D44).
- **D36. The sandbox** leaves out `math.random` and `math.randomseed`: the game's are its own generator, shared with
  `GetRandomInt` (fact), which the world provides (W0-5). `os.clock`, `os.time` and `os.date` read the real clock, as the
  game's do, until the world replaces them (done in W0-5, D44). Binary chunks are refused by every `load`, whatever its mode: Lua 5.3 never
  checks bytecode, so a map could crash the process with one. Whether the game loads them isn't measured;
  `string.dump` stays, harmless without them (replaced by D126: the game has no `string.dump`, so it is dropped, and
  seems to read binary chunks, which stay refused).
- **D37. The native build** uses zig 0.17.0 (`native/build.ps1`) for win-x64 (MinGW), linux-x64 (glibc 2.17) and
  osx-arm64, with `-DLUA_USE_C89` (D33), `-ffp-contract=off` (arm64 would fuse multiply-adds and x64 not) and `-s`
  (without it the DLL holds a hash of the folder it was built in). The output is the same bytes from any folder and
  with CRLF sources (checked), so CI rebuilds it on Windows and fails if it differs from the committed libraries. A
  determinism test hashes every number operation over 20,000 float pairs, nan and infinity patterns, hex numerals, `%a`
  and `os.date` included, and runs on all three platforms.
- **D38. LuaState** passes nil, booleans, integers (`int`), floats (`float`) and strings; a Lua string is read as UTF-8,
  so bytes that aren't UTF-8 come back as U+FFFD; reading a table or a function throws until a task needs them.
  Anything that can raise a Lua error runs in C under `lua_pcall` (`sandbox.c`): getting and setting globals, calling
  one, registering a function, and pushing a C# function's results, which a C trampoline does after C# returns them in
  a buffer. So Lua's `longjmp` never crosses a managed frame, even when a `__gc` fails or `_G` has a strict metatable.
  The values C# writes are pushed with the collector held, before anything else runs: a finalizer calling C# would
  otherwise write over them mid-push. To keep the collector's pace, the hold is Lua's own flag (`gcrunning`), not
  `lua_gc`'s stop and restart, which forget the collector's debt; what the push allocated is added to the debt, and the
  collector takes its step once the values are in. A loop that only calls a C# function returning a 100 KB string
  commits 1 MB, as the same loop in Lua does, and a C# call per step gives 717 collection cycles where Lua's own
  function gives 647. The other choice, a fresh buffer for each nested call, was left: it needs C to tell C# when a
  push is over. Errors carry Lua's traceback, and a chunk's name is given with `@`, so messages
  read `war3map.lua:3:`. The state is a `SafeHandle`, counted in use for the whole of every call, so neither `Dispose`
  (even from inside a callback) nor the finalizer frees it under a running call. A function is added to the state's list
  before its global is set, since a `__newindex` on `_G` can call it then, and stays there if the setting fails, since
  the `__newindex` may have kept it: an id is never given to two functions.
- **D39. Addresses are the same every run.** Each state allocates from its own arena of 64 GB of address space,
  aligned to its size and filled the same way every run (size classes, freed blocks reused last in first out), so an
  object's offset in it is the same every run and, as CI checks with a pinned workload, on every platform. Lua hashes
  an object by the low 32 bits of its address, which are the offset's, so `pairs` visits table, function and coroutine
  keys in the same order, and `tostring` writes the offset (16 hex digits, as Windows writes `%p`). The exception is a C
  function from the library (`print`, `math.sqrt`): it is hashed and written by its place in the library, the same
  every run but not between platforms or builds, so `tostring(print)` and `pairs` over such keys are left out of the
  cross-platform check. The game's order isn't reproduced: the facts say it differs between machines. Memory: each state
  reserves 64 GB of address space (128 GB for a moment while it is placed), commits memory as it is used and gives none
  back until it is closed, so about 2,000 states fit at once (2,008 measured on Windows 11, whose processes have 128 TB
  of address space).
- **D40. Facts not tested here.** The "Lua runtime" facts that measure Moonwell's or CSharp.lua's own Lua code on top of
  these numbers (`Round(2.567, 2)`, `F2` and `F0` text, `Normalize`, `Wrap`, `Lerp`, vectors, rays, slides, bounces,
  circle and wall hits, straight lines, distances away from the origin, easing ends but `InSine(0)`, `-7 / 2` and
  `-7 % 2` as .NET, CoreSystem's integer forms): those libraries aren't part of this repository; the arithmetic under
  them is tested, and their probes can be compared when a C# map runs here. The clock facts (`os.clock`'s steps,
  `os.time` as the PC's seconds, `os.date` in local time, long calls not stopped, slow ticks made up): they are about
  the real clock and the game loop, which belong to the world (W0-5); only `os.time` being an integer and `os.date`'s
  options are tested. `math.random` sharing the game's generator: W0-5.

### Running a Lua map (W0-5)

- **D41. A handle in Lua is a full userdata** holding its id and declared type, with one metatable per type whose
  `__name` is the type, so `tostring` writes `timer: <address>`: the facts measure that it names the type the
  returning native declares. While Lua holds it, the same handle as the same type is the same userdata (a weak table
  per type), so `==` and table keys work (`GetLocalPlayer() == Player(0)`); as another type it is another userdata, as
  the facts measure for `unit` and `widget`. One Lua let go of is made again at a new address next time, as the game
  does (measured since: D112). Its place, so `pairs` and `tostring`, is the same every run (D39). An argument's
  declared type must be the parameter's or below it, else Lua's `bad argument #1 to 'X' (timer expected, got
  player)`, as the facts measure for `GetUnitTypeId` (the message replaced by D113: no `got`); a destroyed handle
  reads as null to the natives (but a destroyed `Condition` or `Filter`, D115). `==` is D72's, then D112's.
- **D42. A Lua function given to C#** becomes a reference in the registry, made by `sandbox.c`'s trampoline before C#
  runs, since C# must not allocate in Lua there. It is let go of when the C# function returns, unless C# keeps it
  (`LuaFunction.Keep`, then `Dispose`). A native's `code` (D20) is a delegate whose target is the kept function's
  wrapper; a native letting go of a callback disposes the target if it can (`TimerStart` again, `DestroyTimer`, and
  `TimerStart` with no timer, which keeps nothing), so a map starting timers with new closures doesn't grow. An
  argument C# can't read (a table, a coroutine) comes to C# as its type (`LuaOpaque`) rather than failing the read,
  so the native refuses it with D49's message and every function given with it is let go of.
- **D43. A Missing native halts the run** (`LuaState.Halts`): its error is raised at the line of the nearest Lua
  function calling in (in its thread, else the main thread, so `pcall(CreateUnit)` and
  `coroutine.wrap(CreateUnit)()` name a line too), and from then on every Lua instruction in every thread raises it
  again (a count hook on every thread; a new thread takes its creator's), so no `pcall`, `xpcall` handler,
  `coroutine.resume`, `load` reader, `string.gsub` callback or C# that swallows the error carries on. No C# function
  runs once the state has halted, since a finalizer or an `xpcall` handler can call one without a Lua instruction.
  The run reports its first error, whatever `coroutine.wrap` adds to it on the way out. Replacing `pcall`, `xpcall`
  and `coroutine.resume` instead misses the others, and in Lua it would move `pcall(error, "", 2)`'s position
  (facts). A stop leaves the simulation stopped rather than throwing it away, so its log is kept: `Simulation.Load`
  returns it with `Stopped`, a `LuaException` starting with the line (`war3map.lua:4: The native CreateUnit is
  Missing...`) whose inner exception is the `MissingNativeException`, and `Advance` throws that, also when the stop
  happens in it. `mirror run` prints the log, then the stop.
- **D44. os.clock, os.time, os.date and os.difftime read the simulated clock** (`LuaState.Clock`, whole milliseconds,
  in the main thread's extra space so every coroutine reads it). `os.clock` is the run's time, divided by 1000 in the
  game's arithmetic as Lua's `/` divides (`mi_div`); the game's is the process's real time, which runs with game time
  in single player. `os.time` and `os.date` add it to 2026-01-01 00:00:00 UTC, the same every run, and local time is
  UTC (`isdst` false, `%z` +0000, `%Z` UTC). The conversions are written out in `native/mitime.c` (dates to days and
  back, `mktime`'s carrying over, Windows's list of conversions in the "C" locale as its C library writes them, `%#c`
  and `%#x` from its documentation), not the C library's, which reads the machine's zone and differs between
  platforms; `loslib.c` calls them in place of `clock`, `time`, `mktime`, `strftime` and `difftime`. As Windows's C
  library (D33) gives no date before 1970, `os.date` of a time below 0 and `os.time` of a date before 1970 raise Lua's
  "time result cannot be represented in this installation"; `os.time` writes the table's fields back before it raises,
  so they are put right then too, the same every run. Not measured in the game: `%c`, `%x`, `%#c`, `%Z`, and times
  before 1970.
- **D45. The clock's step is 1/1024 s.** The facts measure timers in game time down to 1/256 s: a periodic 1/256 s
  timer and a one-shot started again every 1/256 s each run 256 times a game second. With 1 ms steps the one-shot
  would restart every 4 ms (250 a second); with 1/1024 s, 1/256 s is 4 steps. A timer's expiries are at its start
  plus whole timeouts, exactly in seconds, each run in the step nearest it, so it doesn't drift; the clock reads the
  step during its callback, and a 0 s timer started there runs in the same step (fact). Timers due in one step run in
  the order their expiries were queued: when the timer was started or resumed, or for a periodic timer's next
  expiry, when the one before ran, before its callback. Not measured in the game. (Replaced by D153.) A periodic timer runs at most once a
  step, so a periodic 0 s one runs every step (not measured). A timer paused in the step it was due, before it ran,
  has 0 left and runs as soon as it is resumed (not measured). An expiry past the clock's range (2^60 steps, as for a
  timeout of 1e30 or `math.huge`) never comes. The remaining time is the next expiry less the clock, and elapsed the
  timeout less that, each cut toward zero to a single float as the game's arithmetic cuts (D27). Cut toward zero, the
  604800 s timer the facts measure reads ahead by up to 0.0596 s over 100 reads 0.01 s apart, and 84 of 99 reads are
  unchanged; the game drifted up to 0.05 s with 83 of 100 unchanged, and rounding to nearest never drifts more than
  half of 0.0625 s, so it can't. At 3600 s the game drifted 0.0002 s, but here the clock's steps make the remaining
  time exact in a float, so it reads exact. Not matched: the game ran timers due at 0.5, 2 and 3.25 s 0.0001 s early.
  `Game.Advance` jumps to what falls due instead of visiting every step: the same run, faster. `Simulation.Advance`
  moves to the step nearest all the time asked for so far, so 600 advances of 1/60 s reach 10 s.
- **D46. The random generator is SplitMix64** from the run's seed: the game's isn't known, and .NET doesn't promise
  `System.Random`'s seeded numbers across versions. `GetRandomInt`, `GetRandomReal`, `SetRandomSeed`, `math.random` and
  `math.randomseed` share it, as the game's do (fact). `math.random(m, n)` is `GetRandomInt(m, n)` and `math.random()`
  a float from 0 below 1, with Lua 5.3's argument checks ("interval is empty", "interval too large");
  `math.randomseed(x)` seeds with `x` cut to an integer. `GetRandomInt` takes its bounds in either order (replaced by
  D86: from the first bound up by the bounds' distance).
- **D47. Handle ids** count up from 0x100000, the first agent id the game is said to give (claim), and are never
  reused: the game's reuse isn't measured (facts, "Handles and identity"). The 28 players, slots 0 to 27 (fact), are
  made first. `GetHandleId` waits for wave 1, with the rest of the core. (Measured since, and kept: D110, D111.)
- **D48. The run's log** has an entry for each `print`, each text shown to a player, whichever player, and each Lua
  error, as `<seconds to 3 places> <source>: <text>`, the source `print`, `player <n>` or `error`; text of several
  lines stays one entry with that before each line, so every line printed reads on its own. An error's traceback is
  left out. The game shows text only to the local player and loses what is printed before `main` (facts); the log
  keeps it all, as the test's record. A Lua error in the script, `config`, `main` or a timer goes in the log and the
  run carries on, as the game drops errors in callbacks (fact); a map without `config` or `main` logs "attempt to call
  a nil value (global 'main')".
- **D49. The natives become Lua globals** by reflection over `GameNatives`'s `[Native]` methods, sorted by name, so the
  global table fills the same way every run; a Missing native throws before its arguments are converted, whatever
  they are. An integer parameter takes a whole float (fact) and refuses another with Lua's "number has no integer
  representation"; a real takes an integer; a boolean is Lua's truth; a string, handle or code takes nil as null (for
  a string, replaced by D80: nil is refused);
  anything else, a table or a coroutine too, is Lua's `bad argument #n to 'Name' (<type> expected, got <type>)`
  (replaced by D113 for handles, functions, numeral strings and missing arguments). A
  handle is returned as the native's declared type. The game's own Lua functions, Close: `print`, joining `tostring`
  of each argument with tabs, as Lua's does; `FourCC` (facts), a string's bytes big-endian (an object id for 4; other
  lengths not measured); and `__jarray(default)`, which World Editor's scripts make each array variable with
  (`udg_x = __jarray(0)`, with `0`, `""` or `false`): a table whose missing keys read the default through its
  metatable's `__index`, storing nothing. `FourCC` and `__jarray` are Lua, so `FourCC` reads the bytes, which a Lua
  string read in C# loses when it isn't UTF-8. The four maps in `Maps\lua-reference` load their scripts and stop in
  `config` at `SetMapName`. After W0-5: 1 Exact (`GetLocalPlayer`), 14 Close, 2 Recorded, 1,818 Missing.
- **D50.** Probe maps are built with the upstream `War3Net.Build.Core` 6.1.2 from nuget.org, the package of
  `War3Net.Build` that holds `Map`, `MapBuilder` and `MapFactory`, all the builder uses. `War3Net.Build` itself adds
  only the JASS script builder and the transpiler, and their packages. No fork is needed. (W0-4)
- **D51.** A probe map is War3Net's `MapFactory` map at 32 by 32 (info format 28 as World Editor 1.31 wrote it, flat
  ground, the melee flag cleared, one force holding player 1), with `config` and `main` as World Editor writes them for
  one user player, and a black 256x256 `war3mapMap.tga` made in code, since the game closes a map with no minimap image
  (facts) and no game file may be in git. No pathing, shadow or doodad file: the game loads it without. (W0-4)
- **D52.** A probe is plain Lua that calls `out(text)` per line and `done()` at the end. It runs in the first
  zero-second timer, through `guard(f)`, which runs `f` in `pcall` and on an error adds the line
  `error: <message>` (line breaks made spaces, cut to fit as below) and calls `done()`; a probe passes its own timer
  functions through `guard` too. These rules are tested by running a probe map's script in MirrorImaged's Lua with stand-ins for the natives.
  `out` refuses, with a Lua error, a line over 253 bytes or with a line break, the most one `Preload` call keeps whole
  as one line of the file (facts "Files and Preload"); a backslash counts two, as the game writes it doubled (whether
  the game's cut counts it once or twice is not measured, so this is the safe side). `done()` writes the lines to
  `CustomMapData\MirrorImaged\<name>.pld`, each as `")\n` + line + `\n//` (as `docs/tools/gate.ps1` reads them), then
  ends the game. A name is letters, digits and dashes, as it is a file name and a Lua string. (W0-4)
- **D53.** The runner is in `tests/MirrorImaged.GameTests`, with its tests, since what uses it is tests that compare
  with the game; the builder and the probes are in `probes/`. (W0-4)
- **D54.** The runner always puts the game on a hidden desktop: there it runs at full speed and shows nothing (facts).
  Minimised is ruled out (game time stops), and a normal window, which also runs, is not needed, so there is no choice
  to make. The desktop is asked for `DESKTOP_READOBJECTS`, to list its windows, and `DESKTOP_CREATEWINDOW`, without
  which `CreateDesktop` fails with error 8 (seen 11-08). (W0-4)
- **D55.** Every probe map writes `CustomMapData\MirrorImaged\loaded.pld` at the end of `main` and `started.pld` in its
  first zero-second timer, before the probe. While `loaded.pld` is there and `started.pld` isn't, the runner posts a
  space key to the game's window every 3 s (facts: the loading screen waits; a posted key passes it). These marks, not
  `War3Log.txt`, say the map loaded, because the game holds the log back while it runs and loses it when killed
  (facts). A run with no lines is `NoOutput` when the map loaded (`Closed` when the game exited by itself after
  `started.pld` came, D122); `AtLogin` when the game still ran but never logged in
  (it rewrites `War3Preferences.txt` once it has, facts), with its detail starting "needs Sven: ..." and saying what is
  known: it was seen every time the runner ran under Claude Code's Bash tool and never from PowerShell (facts); and
  `Queued` otherwise. What the game shows can't be captured on the hidden desktop (facts), so `AtLogin` is told from the
  preferences file, not from a picture. (W0-4)
- **D56.** The game starts suspended in a job object of its own with `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE`, and is
  resumed once in it, so everything it starts is in the job (measured: no breakaway). The runner ends the job, never
  closing the game's window, which opens the quit dialog; when the test host ends, however it ends, Windows closes the
  job and the game goes with it. It stops nothing outside the job. (W0-4)
- **D57.** Runs are one at a time on a computer: a run holds the named mutex `MirrorImaged.GameRunner` for its whole
  length, a second waits for it up to 5 minutes (well inside the 10 after which the test host's blame collector ends a
  run) and then fails saying so, and a run refuses to start while any Warcraft III is running, as it couldn't tell that
  game's files from its own. Two runs at once made one fail (facts).
  A test that starts a fake `Warcraft III.exe` holds the mutex while it runs. The run's detail names the installed
  build and warns when it isn't 3.0.1.24371, the version the facts were measured on. (W0-4)
- **D58.** Built maps go to `%LOCALAPPDATA%\MirrorImaged\maps`, as `<name>-<8 random hex digits>.w3x`, new each run and
  deleted after it, since the game loads no map whose path is longer than 173 characters (facts) and the test assembly's
  folder is long already; a longer path is refused before the run. (W0-4)
- **D59.** The runner never writes over any of Sven's files and puts nothing back. The one file of his whose content a
  run changes is the account's `LastReplay.w3g`, which the probe's game replaces (facts). Every launch also rewrites
  `Documents\Warcraft III\Logs\War3Log.txt` (the game does it), which the workspace's `docs/tools/gate.ps1` reads to
  confirm Sven's own test-map runs, so a run between his test map and that read loses it. Before a run each account's
  `LastReplay.w3g` whose SHA-256 isn't one a probe run made is copied to `Replays\Saved by MirrorImaged\LastReplay
  <its write time, yyyy-MM-dd HHmmss>.w3g`, or with the first 8 digits of its hash added when another replay holds that
  name; never over a file, and not again when a copy with its content is there. After the run the hash of each
  `LastReplay.w3g` that changed is added to `%LOCALAPPDATA%\MirrorImaged\probe-replays.txt` (hexadecimal, one a line),
  unless a Warcraft III outside the run's job was seen during it (Sven's own game, or World Editor's Test Map), in which
  case the replay may be his and the next run saves it. This replaced a keep-and-restore of five files, which could put
  a stale copy over a newer file. (W0-4)
- **D60.** A `Needs=Game` test whose probe gives no lines fails with the run's detail, rather than passing or being
  skipped: xUnit 2 can't skip once a test runs, and a pass would hide that nothing was checked. (W0-4)
- **D61.** The hub (one game kept open, handed each probe through a Preload request and `ChangeLevel`, each probe going
  back to a hub built for the next request) was built, tried and measured not to work: the hub read its request, but
  `ChangeLevel` from a `-loadfile` game does nothing or crashes the game (facts). It is removed; with no login needed, a
  launch per probe costs about 15 s. (W0-4)

### The game's globals and blizzard.j (W1-1)

- **D62. common.j's globals are generated with the natives,** as `GameNatives.Globals()` in `Generated/Globals.g.cs`:
  each global's name and its value as C#, sorted by name. A value is an integer (any of JASS's forms, and the products
  such as `MAP_LOCK_SPEED`'s `8192*2`, written out), a boolean, or the call of the native common.j calls, with integer
  arguments (`ConvertPlayerColor(0)`, `GetPlayerNeutralPassive()`), so each handle is made by its native as the game
  makes it, and no JASS is committed. Any other form, and an integer past 32 bits (`2147483648`, `$100000000`), stops
  the generator, naming the global. 3.0.1.24371's common.j has 1,871 globals, all constants: 1,857 made by 82 of the 88
  `Convert*` natives, 10 integers, 2 booleans and the two neutral players. A run sets them as Lua globals in that order,
  after the natives and before the game's Lua functions and blizzard.j. Not measured: whether the game's are plain
  globals a script can change, as they are here.
- **D63. Every handle has a key, and Lua's handles hold it.** A handle's key is its own in its game, given when it is
  made, whatever its type, and never given twice; it is not its id, the number the game shows (`GetHandleId`, W1-6). The
  userdata Lua holds is made from the key and the type the handle is seen as (`LuaHandle(Type, Key)`; D41's "its id" is
  now its key; D112 adds the id for `==`): the same key as the same type is the same userdata, as another type another,
  and `tostring` and the metatable follow the type. A handle passed back is found by its key, so one seen as a parent
  type, as `GetTriggerEventId`'s `eventid` will give `gameevent`, `limitop` and the other event types, is still found,
  and `gameevent` 0 and `limitop` 0, both eventids standing for 0, are two handles; a handle that isn't of the
  parameter's type is refused with D49's bad argument, as the type Lua sees is. Keying by id and type, as the first
  version did, missed a handle seen as its parent, and an id alone is ambiguous between a converted value and an agent.
- **D64. A Convert native gives the handle standing for its value:** one object per type and value, made the first
  time and kept, whose id is the value, with a key of its own and no game id, so the agents' ids are as before (D47).
  So the same value gives the same userdata and `==` holds, and the same value as another type is another handle
  (`ConvertRace(0) ~= ConvertPlayerColor(0)`; since D112 `==` compares ids, so they are equal, as in the game, and
  `rawequal` tells them apart). All 88 are Close. `ConvertPlayerColor` and `ConvertRace` (facts:
  `GetPlayerColor` equals `ConvertPlayerColor(n)` and `GetPlayerRace` `RACE_*` with `==`), and `ConvertAttackType`,
  `ConvertDamageType` and `ConvertWeaponType` (the Moonwell.Damage gate's types, made once and compared with `==`)
  become Exact in the tasks that add those getters, which the facts compare them with. Not measured: `GetHandleId` of
  one (said to be the value; W1-6), and a value common.j doesn't name, which gives a handle like any other. (Since
  measured, probe `handles`: the id is the value, a value common.j doesn't name too, and none is taken, D110; and
  `ConvertGameEvent` and `ConvertLimitOp` are Exact, D115.)
- **D65. blizzard.j is transpiled at run time** from the install's `War3.w3mod:scripts/blizzard.j` (the root spells it
  `Scripts/Blizzard.j`; its lookup ignores case, D24), with `common.j` registered first for the natives' return types,
  by the upstream `War3Net.CodeAnalysis.Transpilers` 6.1.1 from nuget.org, rendered by its CSharp.lua with the default
  settings. 6.1.1, not 6.0.2: 6.0.2 brings `War3Net.CSharpLua` 2.0.3, whose `NuGet.Packaging` 5.11.6 and
  `Newtonsoft.Json` 9.0.1 fail the build on their advisories; 6.1.1 brings 2.0.4, which doesn't. No fork: two changes
  are made to the JASS before transpiling, by War3Net's own `JassSyntaxRewriter`. A name that is a Lua keyword and not
  a JASS one gets `_lua` after it, since War3Net writes it as it is and Lua can't load it (`SubStringBJ`'s parameter
  `end`; `docs/upstream.md`), and JASS refuses `end_`. A `debug` statement is left out, Close: War3Net transpiles it as
  if it had no `debug`, and the JASS manual says the game never runs one (claim, not measured; blizzard.j's
  `PlayDialogueFromSpeakerEx` has one). The same two changes are to apply to a map's JASS (W1-7). The Lua runs as the
  chunk `blizzard.lua`, after common.j's globals and the game's Lua functions (its globals use `FourCC` and
  `__jarray`), before `war3map.lua`; an error in a BJ function reads `blizzard.lua:N:`, N the line in the cached file
  (D66); the game's name for it isn't measured. Its globals make 4 timers and 3 groups, the handles after the 28
  players, and call the slot natives (D67); none of its functions runs until a map calls one. They call the natives as
  globals, looked up at each call, so a native a script replaces is what they call (facts). A blizzard.lua that
  doesn't load or run stops the run before the map: `Simulation.Load` throws its `LuaException`
  (`blizzard.lua:N: ...`), or, for a Missing native, comes back `Stopped` (D43). Without an install, `Simulation.Load`
  throws a `DirectoryNotFoundException` ("A run needs blizzard.j from an installed Warcraft III, and there is none in
  ..."), after reading the map, so a missing map is still reported as such. The tests of the runtime itself load their
  maps through an internal seam, `Simulation.Load(path, blizzard, options)`, with `""` in place of blizzard.j, so CI
  runs them on every platform; only the tests that need blizzard.j itself are `Needs=Game`.
- **D66. The transpiled Lua is cached:** a transpile takes about 1.6 s in a new process (0.35 s opening the storage, the
  rest parsing and transpiling; Release build). It is kept once a process, and between processes in
  `%LOCALAPPDATA%\MirrorImaged\cache\blizzard-<hash>.lua`, the hash (SHA-256, its first 32 hexadecimal digits) of the
  game's build key and of the bytes of the assemblies that make the Lua: MirrorImaged's own, War3Net's JASS parser and
  transpiler, and CSharp.lua's renderer. So a change to any of them makes a new file, and nothing else does. A file is
  written whole under another name and moved in place, so no process reads half of one. Each read marks the file used
  (its write time is set to now), and writing one removes the `blizzard-*` files there not used for 7 days, so lanes
  running other builds at once keep theirs. A cache that can't be read or written is passed over, and there is none
  when the user's local application data folder is unknown or an assembly has no file (a single-file app). A
  `mirror run` of a small map then takes about 0.2 s.
- **D67. The slot natives:** `GetBJMaxPlayers` and `GetPlayerNeutralAggressive` 24, `GetBJPlayerNeutralVictim` 25,
  `GetBJPlayerNeutralExtra` 26, `GetPlayerNeutralPassive` 27 (Exact: facts, a new item belongs to neutral passive, slot
  27) and `GetBJMaxPlayerSlots` 28; the rest Close, the values the game has given since it had 24 players, not
  measured here. `CreateGroup` makes a group with the next id, Close; what groups hold comes with the units (wave 2).
- **D68. Left for W1-7,** whose JASS rules reach blizzard.j too, as it goes through the same transpiler: a whole number
  given to a real stays a Lua integer (`bj_MELEE_MINE_SEARCH_RADIUS` is the integer 2000; `docs/upstream.md`), and
  JASS's integer division is Lua's `//`, which floors where JASS cuts toward zero. What the game's own Lua gives for
  these is not measured (measured since: D131 to D133). After W1-1: 2 Exact, 108 Close, 2 Recorded, 1,723 Missing.

### Triggers and threads (W1-2)

The measurements are four probes, `probes/triggers*.lua`, in `docs/facts.md`, "Triggers and threads", with their lines
in `probes/expected`.

- **D70. The script's callbacks run in Lua threads of their own,** as the game's do (not the main thread,
  `coroutine.isyieldable()` false): a timer's callback, a condition or filter, a trigger's action and `ExecuteFunc`'s
  function each start a thread (`LuaFunction.Start`, sandbox.c's `mirror_start` and `mirror_resume`). The script can't
  yield one: there sandbox.c's `coroutine.yield` raises "attempt to yield across a C-call boundary" and
  `coroutine.isyieldable` is false, while a coroutine the script makes yields as usual. Nor can it run one on:
  `coroutine.resume` of one answers false, "cannot resume non-suspended coroutine" (not measured; the game's threads
  aren't Lua's to resume). A wait is the only yield: a C# function asks for it (`LuaState.Yield`), and the trampoline
  yields the calling thread once C# has returned, so `pcall` and `xpcall` across it keep working (facts). A trigger's
  actions and `ExecuteFunc`'s function are run on after their wait; a timer's callback or a condition that waits is let
  go of, so it never goes on, as measured. The natives know no Lua: a function the script gave them is the target of
  their delegate, an `IScriptFunction` that starts it in a thread; `IScriptThreads` starts a global by name and makes
  the caller wait; an `IScriptThread` waiting is run on or let go of. Not matched, as the game's threads are stacks of
  their own rather than Lua coroutines: a wait past a C function (a `table.sort` comparator in an action) raises here
  and waits in the game, and a wait in a coroutine the script made inside an action yields that coroutine here and ends
  the action in the game. A wait in `main` raises Lua's "attempt to yield from outside a coroutine" (not measured). The
  state now makes a table for its threads and wraps `coroutine.resume`, which moves the addresses `LuaDeterminismTests`
  pins. Left to wave 2 (W2-6, `docs/tasks/wave-2.md`): `ForGroup`, `ForForce`, `Enum*` and filter callbacks, measured
  only as far as timers, conditions, actions and `ExecuteFunc` go; and to wave 7 (`docs/tasks/later.md`): `common.ai`'s
  threads.
- **D71. A wait ends at the first tenth of a second of game time after its timeout has passed,** counted in steps from
  the start; a timeout of 0 or less takes 0.1 s, and one past the clock's range never ends. Close: the game's waits end
  mostly on that grid, at or one tenth after the timeout, but differ between runs (facts). Of the 28 waits measured in
  `triggers` and `triggers-2`'s first run, as call + timeout = the game's end, the rule matches 19: 0.6 + 1 = 1.7, 1.7 +
  0 = 1.8, 2.0 + 0.33 = 2.4, 2.4 + (-1) = 2.5, 2.5 + 0.5 = 3.1, 2.0 + 0.1 = 2.2, 2.2 + 0.1 = 2.4, 2.8 + 0.2 = 3.1, 3.1 +
  0.25 = 3.4, 3.4 + 0.5 = 4.0, 4.0 + 0.55 = 4.6, 4.6 + 0.01 = 4.7, 2.033 + 0.1 = 2.2, 2.2 + 0.2 = 2.5, 2.5 + 0 = 2.6,
  2.6 + 0.5 = 3.2, 2.4 + 0.3 = 2.8, 2.8 + 1 = 3.9, 0.8 + 0.2 = 1.1; and misses 9, the rule's end in brackets: 1.8 + 0.05
  = 2.0 (1.9), 0.8 + 0.5 = 1.3 (1.4), 0.9 + 0.25 = 1.3 (1.2), 2.4 + 0.1 = 2.5 (2.6), 2.5 + 0.15 = 2.8 (2.7), 4.7 + 0.09
  = 4.9 (4.8), 4.9 + 0.11 = 5.125 (5.1), 2.25 + 0 = 2.4 (2.3), 0.6 + 0.5 = 1.1 (1.2). Rounding the timeout to tenths and
  adding one matched 20; the rule is the simplest to state. A wait never ends in the step it started in, so
  `PolledWait`'s loop always moves the clock; `PolledWait(1)` takes 1.1 s, as twice in the game. As wait lengths vary,
  `probes/expected/<probe>.txt` marks the lines they reach: a line starting `# ` is a note; one starting `~ ` varies
  from run to run, each `{t}` in it standing for a number and the rest exact, and a run gives it once, anywhere, as its
  time decides its place; every other line comes in order, exactly; a `~ ` line without a `{t}` is refused. W1-8's
  comparison reads the same format (`docs/tasks/wave-1.md`); `ExpectedLines` in `GameProbeTests.cs` reads it now
  (updated by W1-8: `ProbeComparison.Compare` reads it, D120, and D124 adds `{w}` and lets both stand in a line that
  keeps its place).
- **D72. `==` holds for the same handle seen as two types:** each handle type's metatable has an `__eq` comparing the
  handles' keys (D63), so `GetTriggerEventId() == EVENT_GAME_TIMER_EXPIRED` and a widget equal to its unit, as the game
  measures; `rawequal` stays false (D41). Called straight from the metatable with anything but two handles, it answers
  false. The game's `==` also holds between different handles with the same id (`GetTriggerEventId()` with id 4 `==`
  `ConvertPlayerColor(4)`; `Condition(nil) == Filter(nil)`), which needs the id in the userdata: left to W1-6
  (`docs/tasks/wave-1.md`), so `GetTriggerEventId`, `Condition` and `Filter` are Close until then. (Replaced by D112.)
- **D73. Boolexprs:** `And` and `Or` run their second operand only when needed; a nil operand, `Not(nil)` and a nil
  condition are false; a destroyed `Condition` or `Filter` fails without running, while a destroyed `And`, `Or` or `Not`
  still runs. `Condition(nil)` and `Filter(nil)` each give one handle every call, which passes as a trigger's own
  condition and is false in an `And`, `Or` or `Not`: this matches every measured case but one, `And(true,
  Condition(nil))`, true in the game. In the game the two are one handle; here, as C# types, two. In `triggers` a
  condition destroyed while a trigger had it, then a new one added and removed, still ran the new one through the old
  one's place (`ran removed`; handle id reuse inferred), which isn't modelled: `TriggerAddCondition`,
  `TriggerRemoveCondition`, `DestroyCondition` and `TriggerEvaluate` are Close for it, left to W1-6 with the ids.
  (Measured since: not id reuse but the condition's place, D115; the nil handle is D114.)
- **D74. A run takes the trigger's actions as it starts** (facts), so an action removed while a run goes on, by
  `TriggerRemoveAction`, `TriggerClearActions` or `DestroyTrigger`, is let go of only once the trigger's last running
  thread has ended. `DestroyTrigger` lets go of its events too and lets a waiting run go on: the claim that it breaks
  the game's handle stack isn't modelled.
- **D75. The event responses read the running thread's firing:** its trigger, its event id and its game state, kept
  across its waits. An event sets all three; `TriggerExecute` keeps the caller's event and names its trigger;
  `TriggerEvaluate` names the trigger for its conditions; `ExecuteFunc` keeps the caller's (after its wait the game read
  only the trigger: the event kept there too is the model). Outside an event the event id is `ConvertGameEvent(0)` seen
  as `eventid` (the game's has id 0). Units, players and the other responses join it in their tasks.
- **D76. Events here:** timer events schedule themselves, not through a timer handle, so `GetExpiredTimer` is nil in
  them, as measured; a timeout under 0.01 s is 0.01 s. Among what falls due in one step, a timer event keeps the place
  it got when registered, every time (`Game.At` with a place), which both measured ties fit: a periodic event registered
  before a timer started later ran first when both fell due (`triggers`, at 3.0), and a periodic 0 s event fired 10
  times in 0.1 s, before the 0.1 s timer started after it (`triggers-2`); so it is Exact. Whether timers keep such a
  place too, against D45's queue order, is W1-8's to probe. Variable events never fire, as in the game for a Lua global.
  Integer game states (divine intervention, disconnected; W1-4's `GameState.cs`) fire their events on a change as
  measured, with the event id the comparison's value less 255 (`ConvertGameEvent(-253)` for `EQUAL`). The time of day's
  events are W1-9's, with W1-4's clock: their registrations are kept in `_stateEvents`, for the clock to fire with
  `Fire` and the id 3; until then `TriggerRegisterGameStateEvent` is Close. `TriggerRegisterGameEvent` is W1-9's too.
- **D77. `BlzTriggerIsRunning` and `BlzTriggerInterrupt`** (probe `triggers-4`): a trigger is running while a run of its
  actions hasn't ended, waiting or not; `TriggerEvaluate`'s conditions are no run. An interrupt stops the runs started
  before it: the action running goes on, but no other action runs, and a run that waits stops at its wait's end, so it
  reads running until then. When the action running interrupts its own trigger and then waits, the wait wins: the run
  stops at that wait's end, and the rest of the action never runs (not measured; the probe's action ended without a
  wait). A run started after the interrupt runs as usual (not measured). After W1-2, with W1-3 and W1-4 merged first: 93
  Exact, 265 Close, 7 Recorded, 1,470 Missing.

### Hashtables, strings, maths and randomness (W1-3)

The facts are `docs/facts.md`, "Strings, numbers and hashtables": four probes in `probes/`, kept so W1-8 can compare
them, as every measured line is a test. Each model was fitted against those lines with the Lua library's own float
functions before it was written.

- **D80. A string parameter is read as `luaL_checklstring` reads it,** for every native: nil and booleans are refused
  with Lua's `bad argument #n to 'X' (string expected, got nil)`, a number is written as `tostring` writes it (Lua's own
  `%.7g` through the library, and `.0` after a whole number), and the native gets the string up to its first NUL. This
  replaces D49's "a string takes nil as null". Measured on seven natives (nil), three (numbers) and three (NUL); that
  the game binds every native alike is inferred. (A JASS map's natives take null strings: D136.)
- **D81. The maths natives call the Lua library's float functions** (`native/mifloat.c`, through `MirrorImaged.Lua`'s
  `Native`, which `MirrorImaged.Natives` now references), so they give `math.*`'s bits here as the game's natives give
  the game's `math.*`'s: Close where that model is (D31, D32), the game's value beside the model's in the tests (34 of
  the 91 measured cases are a step or more off). Where the game's natives differ from its `math.*`, they follow the
  game: `SquareRoot` is 0 below 0.001, and `Asin`, `Acos` and `Atan2(0, 0)` give 0, as the model's functions already do.
  `Deg2Rad` and `Rad2Deg` multiply by the floats nearest π/180 and 180/π in the game's arithmetic: Exact on the ten
  measured. The new `math.*` measurements (`math.rad`, `math.deg`, `math.asin(2)`, `math.atan(0, 0)`, `^` of a negative
  base, sines of 1 and 1e6) are left to W1-8, which owns the model.
- **D82. StringHash** is lookup2 of the string's UTF-16 letters, folded to upper case and written as one or two bytes,
  with `""` 0 (all 43 measured strings and 19 pairs). Measured folded: a, b, c, h, w, ä, é, ö, þ, ÷, œ, а, ё and я;
  measured left: ÿ, ß, ł, ą, ž, š, ő, ı, α, β, ա, ａ, ǆ and µ. The folding of the ranges a-z, à-þ, а-я and ѐ-џ is
  inferred from those; every letter outside them and œ is left as it is, measured or not.
  Close: a string that isn't UTF-8, which the game decodes byte by byte without checks, reaches C# with U+FFFD in its
  place (D38), so its hash differs; so does `SubString` cutting inside a letter, where the game gives the lone byte.
  Keeping Lua's bytes across the binding would fix both; it is left until a map needs it.
- **D83. S2R** sums the digits as an integer while they fit (then in the game's float arithmetic), cuts toward zero to a
  float and divides by ten to the decimals in the game's arithmetic: 67 of the 77 measured strings exact, the other 10
  (most with five or more decimals) a float step off, so Close. Tried and dropped: summing in floats from the first
  digit (`123456789` lost its last digit, as the game's addition drops it), in doubles (`4294967296` came out exact,
  not the game's 4294967040), dividing by ten once per decimal, multiplying by the game's 1/10, and dividing in steps of
  10^4 (70 of 77, but two more rules for three strings). Past the float range the model is extrapolated: its sum wraps
  as the game's products do (D28), and of the 13 strings of 39 to 48 characters measured (strings-4) three agree,
  eight are 9 to 14 float steps off, and two are far: 40 nines (the game 0, the model 99) and 40 decimals (the game
  0.123456791, as for 9 decimals; the model -1.814). How the game keeps a long fraction sane wasn't found.
- **D84. R2S and R2SW** write the integer part as an integer and the decimals as the fraction times ten to the
  precision, plus 0.5, cut, in the game's arithmetic, with the game's padding after the sign: 130 of the 141 measured
  cases exact. A precision below 0 writes 6 decimals with the zeros at the end dropped but one, and above 9 writes 9;
  the text is at most 255 bytes, cut there, and `""` when the length the width asks for passes a 32-bit integer, the
  pad and the length wrapping as 32-bit integers do (strings-4 to strings-6 and four probes of one call: 29 cases,
  all exact). Close from 2^31, where the game writes about ten digits of its own and then zeros and the largest
  float as `inf`: there MirrorImaged writes the float's exact digits, unpadded, as the game's algorithm wasn't found
  (dividing by ten repeatedly or by a power of ten made by multiplying, then taking digits, matched none). Close too
  for a number below 0 whose text passes 255 bytes: the game closes (0xC0000409, twice), which a run can't copy, so
  the text is cut as above 0 (replaced by D122: the run stops there, as the game closes).
- **D85. The hashtable natives** keep each key's integer, real, boolean, string and handle apart, as measured; a
  `Save*Handle` saves nothing and gives false for a null or destroyed handle (but a destroyed `Condition` or `Filter`,
  which stays in the game, so it is saved and loads, D115), and saves a Convert native's value but
  gives false, as for `FOG_OF_WAR_MASKED` (a handle made by a native that makes objects gave true); a handle destroyed
  after its save loads as null; a save to a destroyed table saves nothing and gives false (not measured: from Lua a
  destroyed table reads as null, D41, which was). `FlushParentHashtable` destroys the table and gives its place back
  under the limit of 256. Exact are the natives measured and tested: `InitHashtable`, the integer, real, boolean and
  string saves and loads, the five `HaveSaved*`, `RemoveSavedInteger`, `RemoveSavedHandle`, both flushes, the saves
  of timer, group, agent, player and fogstate, and the loads of timer, group, widget, player and fogstate. Close: the
  location and text tag saves (measured true, but `Location` and `CreateTextTag` are Missing, so no test makes the
  handle), the other handle types' saves and loads (the same code, types not measured), and `RemoveSavedReal`,
  `RemoveSavedBoolean` and `RemoveSavedString`.
- **D86. The random natives go from the first bound up by the bounds' distance,** as measured (`GetRandomInt(5, 1)`
  5 to 9, `GetRandomReal(2, 1)` 2 to 3); this replaces D46's "GetRandomInt takes its bounds in either order", and
  `math.random`, which refuses an empty interval first, is unchanged. `GetRandomInt` counts its numbers in 32 bits that
  wrap, as the four widest pairs measured show (always -2^31 for (-2^31, 2^31-1), whose count wraps to 0; below 0 for
  (2^31-1, 0)). `GetRandomReal` works in doubles and holds at the float range's ends, so finite bounds never give inf
  or nan: the game's spread over (-3e38, 3e38) and (0, 3e38) is matched; its (3e38, -3e38), some numbers from about 0
  to 3.4e38 and a third tiny and below 0, is not (here 3e38 to the largest float). The generator stays SplitMix64,
  Close: the game's numbers for seed 42 are in the facts for whoever finds its algorithm.
- **D87. I2R, R2I, MathRound, I2S, S2I, StringLength, StringCase and the bit natives are Exact:** C#'s own operations
  give every measured case (`R2I` and `MathRound`'s `long` hold at the ends as .NET 9 and later do on every platform),
  with `I2R` cut toward zero as `TimerGetElapsed` cuts (D45). After W1-3, with W1-4 merged first: 65 Exact, 255
  Close, 7 Recorded, 1,508 Missing.

### Players, forces and the game (W1-4)

The players probe (`probes/players.lua`, its game lines in `probes/expected/players.txt`, `docs/facts.md` "Players and
the game") measured what a one-player `-loadfile` game starts with; the choices below follow it where it measured and
say so where not. What W1-4 left is in W1-9 (`docs/tasks/wave-1.md`), W2-1, W2-6 and W2-7 (`wave-2.md`) and
`docs/tasks/later.md`.

- **D90. A player's colour, race, controller and slot state are kept as values** on the `player` handle and given out
  through the Convert handles (D64), so `GetPlayerColor(p) == ConvertPlayerColor(n)` and `GetPlayerRace(p) ==
  RACE_HUMAN` hold with Lua's `==` (facts); `ConvertPlayerColor` and `ConvertRace` are Exact with them. A player's data
  (name, team, start location, alliances, states, handicaps, tech) lives on its handle, as later waves' unit data will.
- **D91. Who plays is seated after `config`, before `main`,** as the lobby does once `config` has run
  (`GameNatives.Seat`). The test chooses with `SimulationOptions.Users` (the slots people play, the first the local
  player) and `Computers`; without them it is a single-player game in the map's first user slot of `war3map.w3i`, with
  the map's computer slots. The options are checked against `war3map.w3i` before any of the map's Lua runs, each
  refusal an `ArgumentException` saying which slot and why: a person only in a slot the map gives a user, a computer in
  one it gives a user or a computer (the lobby lets a computer take an open slot), never a slot the map doesn't have,
  and no person, a slot given twice or one outside 0 to 23 neither. The map's neutral and rescuable slots always play,
  with the controller `config` gave them (inferred: the lobby offers them to no one). The chosen slots play with their
  controller set; the other slots of 0 to 23 are Empty and keep the controller `config` gave them. The one user of a
  single-player game is "Local Player" (facts); with several users each keeps "Player N", as the game's account names
  aren't known. Before seating, in the script's top level and in `config`, the local player is slot 0, the game type
  melee and no map flag set, while the players probe saw `InitGenericPlayerSlots` read melee inside `config`; the lobby
  before `config` is W1-9's. Two options, not three: the first user is the local player, so a test choosing whose game
  the run is orders `Users`. Choosing a selectable slot's race or a computer's difficulty is in `later.md`.
- **D92. Races come from the lobby:** a playing slot whose race is fixed (its preference names a race, and it isn't
  selectable) has that race; a selectable one gets `RACE_PREF_RANDOM` (replaced by D141: not with custom forces and
  fixed player settings, which keep every preference) and, like a random preference, a random race, as do the unused
  slots of 0 to 23 (probe; replaced by D141: an unused slot whose preference names a race has it); the neutral slots
  have race 0. The random races come from a generator of their own, seeded from the run's seed (`Game.RunSeed`), with a
  draw for every slot from 0 to 23 in order, used or not, so a slot's race doesn't depend on who else plays and the
  map's own `GetRandomInt` and `math.random` don't depend on the seating at all. Whether the game's lobby draws from the
  generator the script shares is not known (D46).
- **D93. The starting state is the probe's:** names "Player N" (replaced by D141: "" until the lobby seats the players),
  slots 1 to 23 Empty with `MAP_CONTROL_NONE`, team -1 and start location -1, the neutral slots Playing as
  `MAP_CONTROL_CREEP` on team 0, colour the slot, races selectable; each player giving itself alliance types 0 to 7,
  neutral passive and every player passive to each other (26 to 27 inferred), nothing else; game result 3, food cap
  ceiling 100, bounty from the neutral slots (26 inferred), `PLACED` 1 for a seated slot; handicaps 1; speed normal,
  difficulty easy, placement the map's settings, game type melee. A seated computer's and a named slot's defaults are
  inferred from these, to be settled by W1-9's probe (settled by D141: a computer's are a user's, and it has its
  `war3map.w3i` name). Gold below 0 reads 0 (measured) and so does lumber (inferred); other states keep any value. A
  tech maximum below 0 reads 2147483647; `SetPlayerTechResearched` sets the level, also lower, but not below 0
  (inferred), and an addition stops at 2147483647. An upgrade's level count, which the game's `AddPlayerTechResearched`
  stops at and its maximum reads, needs the game's data (W2-1), so those natives are Close; so is
  `SetPlayerTechResearched` (setting above that count isn't measured) and `GetPlayerTechCount` (a unit type counts the
  player's units, W2-2, and `specificonly` is ignored until then). `SetGameSpeed` refuses `MAP_SPEED_FASTEST`
  (measured); the speed changes nothing else, as game time is simulated. `GetGameTypeSelected` is the map's settings for
  a map with custom forces (inferred from the lobby, which offers no other there; replaced by D141: melee), and custom
  forces with fixed player settings set `MAP_LOCK_ALLIANCE_CHANGES` (facts); `SetMapFlag` is Close, as only that flag
  set false was measured. `war3map.w3i` is read with War3Net's `ReadMapInfo` from `War3Net.Build.Core`, now a package of
  `MirrorImaged.Data` too. Player names from `war3map.w3i` aren't used yet (replaced by D141: a computer's is; and by
  D210: only with fixed player settings, its `TRIGSTR_n` read from the strings file), so `GetPlayerName` is Close.
- **D94. Forces are a mask of slots,** so `ForForce` goes in slot order (measured). It runs over the players the force
  holds as it starts: one the callback adds isn't visited and one it removes still is (not measured; the simplest
  reading of a mask copied at the start). The enumerations clear the force first and take the playing players of slots 0
  to 23, not the neutral ones (measured with one user; computers inferred). `ForceEnumAllies` takes the players the
  given player is passive to, itself among them, and `ForceEnumEnemies` the others (the direction isn't measured). A
  filter stops the run as the native being Missing until the boolexprs can be evaluated (W1-9), so a map never gets a
  wrong answer; `GetFilterPlayer` waits with them (replaced by D146: filters run). `ForForce` lets go of its callback
  (D42) and gives the outer enum player back after a nested one. It is Close: besides the copy, the facts' wait that
  never ends after a force was looped with `ForForce` and destroyed isn't modelled (W1-9; replaced by D148: modelled).
- **D95. Pausing stops the clock** (`Game.Paused`): what is due in the step the game paused in still runs, so a pause
  from one callback doesn't hold back the others of its frame, and then the clock stays; `Simulation.Advance` counts no
  paused time, so once the game goes on the clock moves from where it stopped. `Simulation.Paused` shows it; only the
  map can go on, as in the game, so the API has no way to resume. Not measured; the facts say only that the victory and
  defeat boxes pause the game. Until dialogs and input exist (waves 5 and 6), only a callback in the same step can go
  on.
- **D96. The time of day is worked out from the clock,** from where it was last set, at 0.05 hours a second times the
  scale, kept from 0 below 24 (by `%` on doubles, which is exact, so even a huge scale gives an hour of the day) and
  read cut toward zero to a float, so it reads under 24 (from 23.9, 2 s later 23.999998, where the game read
  23.9999962). An infinite or NaN scale is refused and the scale stays (not measured). Setting reads back as set; below
  0 and above 23.99 read 0 (measured: 23.99 kept, 23.995 not). Close: the game adds each frame's share and loses a
  little (0.0999975 for 0.1 after 2 s), reads a set 0.1 a float step lower, and its event at 12 reads a float step under
  12; the scale and suspending are Exact. The workspace's facts saw a 7 read 6.999999 (11-05, a Moonwell check map)
  where the probe read 7 back exactly; the facts' read may have come later than the set, and W1-9's probe is to separate
  them (answered by D149: neither read gives 6.999999). A day is the standard 24 hours of 480 s until the game's data
  (W2-1) gives a map's own (the facts' `DayHours=30` cases; done in D208).
- **D97. Events wait for the triggers (W1-9):** `TriggerRegisterPlayerEvent` (victory, defeat, leave),
  `TriggerRegisterPlayerStateEvent`, `TriggerRegisterPlayerAllianceChange` and `TriggerRegisterGameStateEvent` stay
  Missing, and with them the events the facts measure inside `RemovePlayer`, `SetPlayerAlliance` and `SetPlayerState`
  and as the time of day passes an hour, so those three natives are Close (replaced by D142 to D144: the events fire,
  and the three natives are Exact). `RemovePlayer` sets the slot, Left for defeat and Playing for any other result
  (victory measured; replaced by D142: Left for all but victory, once), and the player's `PLAYER_STATE_GAME_RESULT` to
  the result given (inferred: the state starts at the neutral result, 3, and is named for it). `EndGame`, which ends the
  game for `CustomVictorySkipBJ`, stays Missing (W1-9; replaced by D147: it ends the run); `CustomVictoryBJ` and
  `CustomDefeatBJ` also need dialogs (wave 5). `DefineStartLocationLoc` and `GetStartLocationLoc` wait for locations
  (W2-6), `SetPlayerAbilityAvailable` for abilities (W2-7).
- **D98. What only the lobby reads is Recorded:** `SetMapName`, `SetMapDescription`, `SetEnemyStartLocPrio`,
  `SetEnemyStartLocPrioCount` and `SetPlayerRaceSkin` change nothing a native reads back, so they are Recorded, each
  with its reason. Their calls aren't written to the run's log: D48's log is what the map shows, and none of these
  shows anything during a run; how tests read Recorded calls in general is the test tools' (wave 8).
  `SetStartLocPrioCount` stays Close: whether `GetStartLocPrio` and `GetStartLocPrioSlot` read the slots past the count
  differently isn't measured, and here they read what was set. The lobby's placement of players on start locations
  isn't simulated, so a player's start location is the one `config` set, or -1.
- **D99. After W1-4:** 25 Exact, 169 Close, 7 Recorded, 1,634 Missing. `config` of the four maps in
  `Maps\lua-reference` runs through, and each stops at the first line of `main`, `GetCameraMargin` (wave 5); `main`'s
  player setup (`InitBlizzard`) comes after the cameras, sounds, regions and units, and its first player function,
  `InitBlizzardGlobals`, needs `Filter` (W1-2) and sounds first.

### Files and game caches (W1-5)

The files probe (`probes/files.lua`, its lines in `probes/expected/files.txt`) and its one-off runs measured what the
workspace's facts left open (`docs/facts.md`, "Files and game cache"); the choices below follow them where they
measured and say so where not.

- **D100. The documents are an object the test holds,** `Documents` (in `MirrorImaged.World`): `CustomMapData`, the
  files by their path in it, `\` between folders, found in any case, which a test reads, fills and empties; and the
  saved game caches, internal. A run takes it from `SimulationOptions.Documents` (a new, empty one by default) and
  shows it as `Simulation.Documents`; the same one given to the next run keeps both. It is in memory, not a folder: a
  run stays the same on every platform and a map can reach nothing outside it; a test that wants a real file copies it
  in.
- **D101. Paths are the game's, as the probe measured them.** A file is written and read only with `.pld` or `.txt` in
  lower case: `.PLD`, `.dat`, `.json`, `.ini`, `.log`, `.w3v` and none were written nowhere, and an outside program's
  `.dat`, `.json` and extensionless files weren't read. `/` is `\`, and `.` and empty folders are left out. `..` makes a
  write go nowhere, while a read goes through it (an outside file read through `sub\..`), never above `CustomMapData`
  (not measured). An absolute path (`C:\...`, measured) is neither written nor read, and neither is one starting with
  a slash (`\x.pld`, `/x.pld`, `\\srv\x.pld`, `//srv/x.pld`) or holding a colon (`C:x.pld`) (not measured). At most
  201 characters: 201 was written and read and 202 not, on a PC whose `CustomMapData` path has 58, so the game's limit
  is likely the whole path's 259 and moves with the player's folder; reads have it too (not measured: the longer files
  weren't there to read). Folders and names are found in any case, and so is a path `Preloader` has read (probe:
  `MIRRORIMAGED\FILES-PROBE\own.pld` gave `own.pld`'s first content). Windows's other refused characters (`<>"|?*`)
  aren't modelled.
- **D102. Preload and Preloader.** Every `Preload` text is kept, its first 259 bytes of UTF-8, until `PreloadGenClear`,
  whether or not `PreloadGenStart` came first and also after `PreloadGenEnd`, which writes them all and keeps them
  (probe). A cut can split a letter (not measured); `Preloader` reads the file as UTF-8, so the split letter comes back
  as U+FFFD, and a Lua string that isn't UTF-8 is already U+FFFD where it isn't when C# reads it (D38). A backslash is
  doubled as the file is written, after the cut: whether the game's cut counts it once or twice isn't measured, and
  D52's lines take the safe side. `PreloadEnd`'s number is written 0.0: 304 of the 311 Preload files on this PC have
  it, those known to be written in one call among them; 7 have 0.1, and a file ended 2.5 game seconds after its
  `PreloadGenStart` had 1.9, 2.0, 2.0 and 1.9 in four runs, so it counts something other than game time, perhaps real
  time, and `PreloadGenStart` and `PreloadGenEnd` are Close.
  `Preloader` reads a path once a game and keeps that content, also after the map writes the path again (probe); a
  path that was missing is read again (not measured). It runs the file at once: the JASS goes through War3Net's
  transpiler and D65's rewriter as blizzard.j does, against declarations of common.j's natives and globals made from
  `GameNatives` by reflection (the transpiler needs a function's declaration to call it inside an expression, and no
  game file is needed). The rewriter now also writes a rawcode as its value: War3Net writes it as `FourCC("...")` with
  its text as it is, so `'ab"c'` breaks out of the string and `'\abc'` reads `\a` as one byte; the value is War3Net's
  reading of the four characters, a negative one in parentheses (`-'éab'` would read as a Lua comment). War3Net reads a
  byte from 0x80 up after the first as signed: `'éab'` (UTF-8 `C3 A9 61 62`) is -1029086878, where its bytes, and the
  old `FourCC`, give -1012309662. Neither that nor the game's reading of a backslash in a rawcode is measured: W1-7's
  JASS probes are to settle rawcodes with high bytes (settled by D137: War3Net's reading is the game's). This reaches
  blizzard.j's rawcodes too, with the same values for its rawcodes, which are all ASCII. The file runs in a Lua thread of its own, as W1-2 runs a callback; a wait in it
  (`TriggerSleepAction`) lets the rest of the file go (not measured). The Lua is loaded as the chunk named by the
  file's path, so an error in it reads `MyMap\save.pld:N:` (N the transpiled Lua's line, as D65's are), and runs with
  those natives and globals as they were before blizzard.j and the map ran, and `__jarray`, which War3Net makes a JASS
  array with. So a native the map replaced isn't what a file calls, and a file's `PreloadFiles` isn't the map's
  (inferred: the game runs a Preload file as JASS, without blizzard.j or Lua, facts). A `Preload` call the file makes
  isn't kept for the map's next file, while what the map kept before and after the `Preloader` call is (probe).
  The game closed on a file with a cut piece and on one naming a blizzard.j global (facts), on a line that isn't JASS,
  and on a file that ends `PreloadFiles` and goes on in Lua, as some maps' save files do (probe); so a file that
  doesn't parse or names anything but those natives and globals stops the run with a `GameClosedException`, as for a
  Missing native (D43), at the line of the map's `Preloader` call, the file's error in the message. An error as the
  file runs (a missing argument) does the same; that one is not measured. The facts' "Lua cannot be run from a
  Preload file (`//!beginusercode` does nothing)" (10-01) didn't record the file: a `//!beginusercode` block of JASS
  comments runs nothing here, and Lua after `endfunction` closes the game as measured. A Missing native the file calls
  stops the run as D43 does, at the file's line (`MyMap\save.pld:N: The native ... is Missing`). JASS's own type
  checks aren't made (a call with an argument too many runs), and neither is its operation limit, so a file that loops
  forever hangs the run; the limit is W1-7's, for transpiled JASS. The game's own Preload files
  (`scripts\HumanMelee.pld`, which the melee BJ functions read) aren't in `CustomMapData` and read as missing; they
  only name files to load. `PreloadStart`, `PreloadEnd`, `PreloadEndEx` and `PreloadRefresh` are Recorded and not in
  the log (D98). The ability-tooltip carrier waits for W2-7 (`BlzSetAbilityTooltip` and its kin, with the facts'
  tooltip cases): until then a file calling it stops the run as Missing. The game-cache and `SetPlayerName` carriers
  work now (probe: the probe reads its files back through a game cache). Comparing the game closing with
  `GameClosedException` is W1-8's.
- **D103. Game caches, as the probe measured them:** one cache a name, every `InitGameCache` of it a new handle to it;
  each type's values apart; missions and keys found in any case (measured with ASCII letters; `ToUpperInvariant`
  here); a missing value reads 0, 0.0, false and an empty string; an empty string, false and 0 are stored values, and
  `StoreString` gives true. `FlushGameCache` removes the cache: its handles then read and store nothing, also once
  `InitGameCache` makes the name again, which gives a new, empty cache. `FlushStoredInteger` takes the integer only;
  the real, boolean and string ones take theirs (Close, not measured). A name is found in any case, as a file is (not
  measured).
- **D104. Saved caches.** A game starts with the caches saved in its documents, taken as the documents are given to
  it, so a save another game makes later doesn't reach it (a value saved in one game read 1 in the next, one stored
  after the save 0); `SaveGameCache` writes every cache, not only the one given (the game's file then held caches the
  probe never saved by name), and gives true, also for a name without `.w3v`; `ReloadGameCachesFromDisk` gives true,
  as in a live game, puts the saved caches back in place, so their handles read them, and drops the caches never
  saved, whose handles then read nothing. These two are Close: they were measured in one run, which no probe may
  repeat, since `SaveGameCache` rewrites the account's `BattleNet\<id>\Campaigns\Classic\Campaigns.w3v` (here
  `Documents` holds them). So caches go from one map of a test to the next through `SaveGameCache`, as from one game
  to the next. Left for later (`docs/tasks/later.md`): a level change in one game (`ChangeLevel`), which may keep
  caches never saved; replays, where `ReloadGameCachesFromDisk` gives false (facts) and Preload files are written
  again; and keeping `Documents` between processes or seeding its saved caches from outside. `SaveGameCache(null)`
  gives false (not measured). The sync natives do nothing: a run is one machine, whose caches are every player's. The
  unit natives (`StoreUnit`, `RestoreUnit`, `HaveStoredUnit`, `FlushStoredUnit`, `SyncStoredUnit`) wait for W2-3;
  the 255-cache limit (a claim) isn't modelled. `Documents` isn't thread-safe: one run at a time uses it.
- **D105. After W1-5,** with W1-2, W1-3 and W1-4 merged first: 112 Exact, 276 Close, 11 Recorded, 1,436 Missing.

### Handles and errors (W1-6)

The measurements are four probes, `probes/handles.lua`, `handles-2.lua`, `handles-3.lua` and `arguments.lua`, in
`docs/facts.md`, "Handles and errors", with their lines in `probes/expected`.

- **D110. Ids as the game gives them, but for where they start and their reuse.** Every handle a native makes takes the
  next id, whatever its type, a trigger's actions, conditions and events too; a Convert handle's id is its value and it
  takes none; `GetHandleId` of nil or of nothing is 0, and of a destroyed handle its id, which the Lua handle holds (the
  binding gives `GetHandleId`, the one native taking any `handle`, a stand-in with the id, as a destroyed handle reads
  as null to the natives, D41). All measured. Not matched, so `GetHandleId` is Close: where the ids start. The game
  makes one handle before blizzard.j's globals (0x100001 on) and each player when first asked for, it seems
  (`Player(0)` in `config` at 0x100008, `Player(25)` next, then 1 to 24, 26 and 27 later); here the 28 players are made
  first (D47), as other natives read them before the script asks, and the game's own handles aren't known; left to
  wave 6 (`docs/tasks/later.md`). Ids are `int`, from 0x100000 up, which a map making a thousand handles a second would
  pass in 24 days. The probe lines with raw ids differ from MirrorImaged by this decision (W1-8's list,
  `docs/tasks/wave-1.md`).
- **D111. A destroyed handle's id is never given again.** The game gives it again once the handle is destroyed and Lua
  has collected every userdata of it, 0.1 to 0.2 s later, the last freed first, with another id, not found, freed with
  some batches (facts). Not modelled: when Lua collects can't match the game's collector (the allocator and the
  sizes differ, D39), so a model would only move the ids by a guess, and it needs the userdata counted from `__gc` into
  C#. What a map can see of it doesn't change: as long as Lua holds a destroyed handle, its id isn't given again, here
  or in the game, so a stale handle never equals a new one, and `==` by id (D112) is safe. A map that keeps data under
  `GetHandleId` and forgets to remove it doesn't get it back for a new handle here, as it may in the game. Left to
  wave 10 (`docs/tasks/later.md`); the probe lines it changes are in W1-8's list (`docs/tasks/wave-1.md`).
- **D112. `==` compares the game's ids.** A Lua handle holds its key (D63), its game id and whether its declared type is
  an agent (`LuaHandle(Type, Key, Id, Agent)`, sandbox.c's `Handle`); every type's `__eq` compares the ids, whatever
  the types (`GetTriggerEventId()` of id 4 `==` `ConvertPlayerColor(4)`, `ConvertRace(1) == ConvertPlayerColor(1)`,
  `ConvertPlayerColor(n) == t`), and an agent's refuses a second operand that isn't an agent with Lua's `bad argument #2
  to '__eq' (agent expected)` at the comparing line (`t == ConvertPlayerColor(n)`, `Player(0) == RACE_HUMAN`), as
  measured; Lua calls the first operand's. The metatables read as `false` through `getmetatable` (`__metatable`), as
  the game's, so `__eq` can't be called with anything else; it still checks its operands. Keys stay the identity: the
  same key as the same type is the same userdata, so a table key, and an equal handle of another type is another key,
  as measured; a handle Lua collected comes back as a new userdata, as measured. This replaces D72; the userdata grew,
  which moved no address `LuaDeterminismTests` pins.
- **D113. Arguments are checked as the game reports them** (probe `arguments`): a handle or function parameter takes nil
  and a missing argument as null and refuses anything else with `bad argument #n to 'X' (<type> expected)`, with no
  `got`, the type named as `common.j` spells it (`equipmentType`) and `function` for code; a number parameter reads a
  string as Lua's `luaL_checkinteger` and `luaL_checknumber` do, with Lua's own reader (`LuaState.ToNumber`, sandbox.c's
  `mirror_tonumber` over `luaO_str2num`), so `"0x10"` is 16 and `"1.5"` has no integer representation; a missing
  argument to a number or string parameter is `got no value`; a boolean is Lua's truth, a missing one false. Lua's own
  `got <type>` names a handle's declared type (`got timer`). Measured on `TimerGetTimeout`, `GetHandleId`,
  `TriggerRegisterVariableEvent`, `I2S`, `R2S`, `StringLength`, `TriggerWaitOnSleeps`, `TriggerAddAction`, `Condition`
  and `TimerStart`; that every native binds alike is inferred, as in D80. `math.random` takes numeral strings too, as
  Lua's does; `math.randomseed` still refuses them (Lua's takes them). Not matched, not measured: Lua's own `got` names
  any value whose metatable has a string `__name`, so the game's `I2S(setmetatable({}, {__name = "x"}))` would say
  `got x`; here a table is `got table`.
- **D114. `Condition(nil)` and `Filter(nil)` are one handle in the game:** here two, `conditionfunc` and `filterfunc`,
  made by the first call of either with one id, so `==` holds, `rawequal` doesn't and the second takes no id, as
  measured; C#'s types can't make one object both. Their value in an `And`, `Or` or `Not` stays D73's (false; alone as a
  trigger's condition, true), which matches 15 of the 21 cases measured: the game's depends on what ran before (`And(t,
  cn)` passes, `And(t, And(u, cn))` passes, `And(t, Not(cn))` fails, `And(Not(cn), t)` fails), and no rule tried fits
  all 21: the last condition's result, globally or per evaluation, and a value per depth that a condition writes for its
  depth and below and an `And` or `Or` clears for its operands (19 of 21, missing `And(t, Not(Not(cn)))` and
  `And(Not(cn), t)`), which mispredicted two of the seven cases the last probe was written to check. It likely reads
  memory the game left. `TriggerTests` lists the 21, the game's result beside the model's. So `Condition`, `Filter`,
  `And`, `Or` and `Not` are Close, and `TriggerEvaluate`, which gives those results, too, until the nil condition fits
  every measured case (wave 10, `docs/tasks/later.md`; the probe lines in W1-8's list). As they are one handle in the
  game, destroying either destroys both here, also the other when its first call comes after (not measured).
- **D115. A destroyed condition's place:** a `Condition` or `Filter` keeps its function in a place of its own;
  destroyed, the place is freed, and the next `Condition` or `Filter` made takes the first freed, so what still holds
  the destroyed one (a trigger's condition, an `And`, `Or` or `Not`) runs the new one, as the seven cases measured. A
  timer and a trigger made between don't take it (measured); `Condition(nil)` and a new `And`, `Or` or `Not` don't
  either (not measured, the model's). `And`, `Or` and `Not` have no place, so a destroyed one still runs its operands
  (measured: an `Or` made after it didn't take it). The new one is a new handle with its own id. A destroyed condition
  stays its place's, so given to a native it runs what the place holds: nothing while it is free, the new function once
  taken (W1-9's probe `events`: a destroyed `Filter` given to `ForceEnumPlayers` after a `Condition` had taken its place
  ran the new condition for each player). This is the one kind of destroyed handle that doesn't read as null to the
  natives (D41). Not measured, the model's: destroying it again after its place was taken frees the new function too
  (a place taken twice, c1 destroyed and taken by c2, c2 destroyed and taken by c3: c1, c2 and c3 all run c3's
  function, and destroying the stale c1 or c2 frees c3's place; W1-10 is to probe it), and a hashtable saves and
  loads it (D85's check is that the handle is still in the game). Known limit: a destroyed `Condition` or `Filter`
  stays in the game for good, about 80 bytes each, which blizzard.j's matching functions, destroying a filter a call,
  pile up; it can be let go of once Lua has collected its userdata, which needs wave 10's collector-driven id reuse
  (`docs/tasks/later.md`; the `shortcut:` in `Triggers.cs`'s `Destroy`). No cheaper release is safe: a userdata Lua
  still holds finds it by its key, and nothing tells C# when the last one is collected. With it, `TriggerAddCondition`,
  `TriggerRemoveCondition` and `DestroyCondition` are Exact, and so are `GetTriggerEventId` (D112), `ConvertGameEvent`
  and `ConvertLimitOp` (probe `triggers-2`: equal to the event of id 4; probe `handles`: `ConvertGameEvent(4)` has id
  4). After W1-6, with W1-5 merged first: 116 Exact, 273 Close, 11 Recorded, 1,435 Missing.

### Player and game events (W1-9)

The measurements are three probes, `probes/lobby.lua`, `events.lua` and `forforce-wait.lua`, in `docs/facts.md`,
"Players and the game" and "Player and game events", with their lines in `probes/expected`. What they left is in
`docs/tasks/later.md` (wave 6), W2-1 and W1-8 (`docs/tasks/wave-1.md`, `wave-2.md`).

- **D140. A probe map with a computer** (`ProbeMap.Build(..., withComputer: true)`, and the runner's `Run` likewise),
  for the lobby and events probes: a user in slot 0, a computer in 1 and a user slot 2 no one plays, named "Probe User",
  "Probe Computer" and "Probe Empty" in `war3map.w3i`, human with a selectable race, in custom forces with fixed player
  settings (0 and 2 in one force, 1 in the other, not allied). Its `config` first adds what it reads (the game type,
  the lock flag, the local player, the counts, and slots 0 to 2's name, slot state, controller, race and random
  preference) to the global list `ProbeConfig`, one entry a call: so a probe sees what the lobby set before `config`,
  and how often it ran (once, in the map's Lua state). One fixed layout, not options: both probes need the same, and
  the other probes' map stays as it was.
- **D141. The lobby before and after config** (the lobby probe). At the start of `config`, before it sets anything,
  slots 0 to 2 are empty with no controller, named "" and with the random race preference (slots 3 to 23 inferred the
  same, as only 0 to 2 were read), the local player is slot 0, the game type melee, `MAP_LOCK_ALLIANCE_CHANGES` not set
  (read only there, not after `config`'s own calls), 0 players and 24 teams. The lobby seats the players after
  `config` (D91), and then names a computer in a slot the map gives a computer as `war3map.w3i` does and every other
  slot of 0 to 23 "Player N"; puts a slot no one plays on team -1, with the controller `config` gave it; keeps every
  race preference when the map has custom forces with fixed player settings, selectable or not (the same with and
  without `war3map.w3i`'s selectable-race flag), and otherwise makes a playing selectable slot random, as before; gives
  a slot whose preference names a race that race, played or not; and, for custom forces with fixed settings, sets
  `MAP_LOCK_ALLIANCE_CHANGES` and the flag 2^20, which `common.j` doesn't name. So the lock isn't set before `config`,
  against the task's premise, and the seating stays after it. The game type is melee with custom forces too; custom
  forces without fixed settings aren't measured. A computer's alliances, states, handicaps and placed state are a
  user's, as D93 inferred. Not measured, and chosen: a name `config` set is kept (the game crashes the lobby when
  `config` calls `SetPlayerName`, facts), and a computer seated in a slot the map gives a user is "Player N", not that
  slot's `war3map.w3i` name. With `MAP_PLACEMENT_TEAMS_TOGETHER` the lobby put the players on start locations at
  random, different in each run; that isn't simulated (D98, `later.md`), so `GetPlayerStartLocation` stays Close.
  World Editor writes the names as `TRIGSTR_n`, read through the strings file in W2-1; until then a computer's name is
  `war3map.w3i`'s as it is.
- **D142. RemovePlayer takes a player out once** (the events probe and the facts): the result is stored, victory keeps
  the slot Playing and any other result leaves it Left (neutral measured, tie inferred), and a player that isn't a user
  fires its victory or defeat event inside the call, after the change, with itself the trigger player. A user fires
  neither, inside the call or a second later (victory measured here, defeat by the facts' `CustomDefeatBJ`); neutral
  and tie fire nothing (neutral measured); `EVENT_PLAYER_LEAVE` never fires for it. A second call on the same player
  does nothing, whatever the result. Who counts as a user is the controller: another person in a multiplayer game
  isn't measured. Exact.
- **D143. Alliance and player state events** (the events probe). `SetPlayerAlliance` fires only on a change, on the
  giving player's registrations: its `EVENT_PLAYER_ALLIANCE_CHANGED` first, for `ALLIANCE_PASSIVE` only (alliance types
  1 to 9 fire none), then the alliance type's own (`TriggerRegisterPlayerAllianceChange`), each with the event id
  `EVENT_PLAYER_ALLIANCE_CHANGED` and no trigger player, reading the new alliance. The workspace's facts say it fires
  "each time it is set"; setting the same value again fired nothing here, so that means each change. A player's
  alliance with itself doesn't change (passive measured). `SetPlayerState` fires the player's events for that state on
  a change to a value that meets the comparison, the value stored (gold set to -1 fires as 0), with `NOT_EQUAL` a real
  comparison, unlike the integer game states' (any change, D76); the event id is `EVENT_PLAYER_STATE_LIMIT` whatever
  the comparison, `GetTriggerPlayer` the player and `GetEventPlayerState` the state. Outside an event
  `GetEventPlayerState` is the state with id 0 (`PLAYER_STATE_GAME_RESULT`), not nil. `TriggerRegisterPlayerEvent`
  with `EVENT_PLAYER_STATE_LIMIT` never fires; its input events (leave, chat, cinematic, arrows, mouse, keys) register
  and never fire until wave 6 (`later.md`). The three registrations, `GetTriggerPlayer`, `GetEventPlayerState`,
  `SetPlayerAlliance` and `SetPlayerState` are Exact.
- **D144. The time of day's events** (the events probe; triggers-3's reads fit too, so D76's inference holds). A set
  fires inside `SetFloatGameState`, reading the value set, by where the time goes: `GREATER_THAN` and
  `GREATER_THAN_OR_EQUAL` when it goes up from the limit or below to the limit or above, `LESS_THAN` and
  `LESS_THAN_OR_EQUAL` when it goes down from above the limit to it or below (from the limit itself, nothing), `EQUAL`
  when it lands on the limit, `NOT_EQUAL` never. The workspace's facts say a set on from exactly the hour (6 to 18, 18
  to 17) fires the old hour's event too; here 12 to 13 and 12 to 11 fired no `EQUAL` inside the set, so that was the
  frames after (below). As the day runs, it fires a limit's `EQUAL`, `GREATER_THAN_OR_EQUAL` and `GREATER_THAN` events
  as it passes it forward, and its `EQUAL`, `LESS_THAN_OR_EQUAL` and `LESS_THAN` ones backward, the time reading the
  limit exactly; here at the first step at or past it, with the time set to the limit there (2.001 s from 11.9 at
  scale 1; the game's frame came at 1.9999), so the day loses up to a step's share at each pass, and again a day later.
  A set that lands on a limit while the day runs passes it at the next step, also when something else reschedules the
  day in the set's step. Only the nearest pass is queued, so a map setting the time every frame doesn't fill the
  queue. Not modelled, so `TriggerRegisterGameStateEvent` stays Close (W1-8 is to settle or model them): with the day
  stopped, the frame after a set to exactly the limit fired all five again (the facts' "again within the next
  second"); 12 to 13 with the day stopped, after frames at 12, fired nothing, where the same set within one instant
  fired the two upward comparisons; and with the day running, a set that lands on the limit fired `EQUAL` only at the
  next frame, which the model follows by firing `EQUAL` in a set only with the day stopped. The game's order among one
  moment's events (GT, GE, EQ forward; EQ, LE, LT backward; EQ, GT, GE in a set) isn't registration order and isn't
  followed.
- **D145. TriggerRegisterGameEvent never fires in a run:** none of its 16 events fired through the events probe, not
  the timer event for its timers, the state event for its state sets nor the victory event for its victories; that the
  end-level event doesn't fire for `EndGame` is inferred from a file its action would have written and didn't. Close:
  the game fires `EVENT_GAME_LOADED` after a saved game loads (facts, "Initialisation"), which a run doesn't do.
  `GetWinningPlayer` stays Missing, with no event to read it in (`later.md`).
- **D146. Filters** (the events probe). An enumeration's filter runs for each playing player of slots 0 to 23 in slot
  order, with `GetFilterPlayer` that player (nil after), and passes as a trigger's condition does: 1 passes; 0, `"x"`,
  nil, a table and a raising filter fail; `Filter(nil)` passes without running; a `Condition` serves too; `And` runs
  both operands for each player. `ForceEnumAllies` and `ForceEnumEnemies` run it for every playing player, then test
  the alliance; `ForceEnumPlayersCounted` stops once the force holds the count, without running the filter for the
  rest. Inside the filter `GetEnumPlayer` and `GetTriggerPlayer` were nil when the enumeration ran in a timer's
  callback, outside any event or `ForForce`; a filter run inside an event's action or a `ForForce` callback isn't
  measured, and here it reads the outer thread's trigger player and enum player. A destroyed filter given again ran
  the `Condition` that had taken the filter's place, for each player; with W1-6's places (D115) a destroyed filter
  runs what its place holds, so this matches, and the four `ForceEnum*` are Exact (the direction of
  `ForceEnumAllies`, D94, still isn't measured). A filter runs in a thread of its own, as a condition (D70); W2-6
  probes that.
- **D147. EndGame ends the run** (`Simulation.Ended`): the rest of its step runs, the code after the call and a 0 s
  timer started before it among it, then the clock stops for good, a pause the map can't lift, and `Advance` moves
  nothing. No event fires (D145; inferred from a missing file). The evidence is the probe's side files
  (`events-after.pld` and `events-timer.pld` written, `events-end.pld` not), which the events game test now deletes
  before the run and checks after, and W1-8's comparison is to check too. `doScoreScreen` changes nothing: a
  `-loadfile` game skips the score screen (facts). Exact.
- **D148. The lost wait after ForForce** (the forforce-wait probe, which reproduced the facts' three): a trigger's
  wait started right after a force `ForForce` had run over was destroyed never ended, whether the force held a player
  or not; `ForForce` alone, or a force made and destroyed, didn't; the next trigger's wait, 1.5 s later, and a
  `PolledWait` after it ended. Its reach, probed after the code review: a wait started 1 s after the destroy ended, and
  so did a wait already running across it, while a new one started beside it in the same instant was lost. Modelled
  so: destroying a force `ForForce` has run over loses the next wait started in the same step; a later wait, or one
  already running, ends. Not measured: a second new wait in that step. Why the game loses it isn't known (the trigger
  and timer made after the force took new ids, so it isn't an id reused at once). `ForForce` stays Close for the
  force's changes during it (D94).
- **D149. The time of day read back** (the lobby probe, D96's question): with the day stopped, 7, 12, 18, 6 and 7.5
  read back as set at once and in the same frame, and after a frame 7 read 7.00000477 (`40e0000a`), the others the
  same; 0.1 read `3dccccc0` and 23.9 `41bf3332` already at once, where the players probe read `3dcccccc` and `41bf3333`.
  So neither read gives the 11-05 map's 6.999999; the read drifts with the frames by a rule not found (times that are
  binary fractions of 24 hours stay exact), left to W1-8. D96's model stays, reading back as set, Close. After W1-9,
  with W1-5 and W1-6 merged first: 131 Exact, 266 Close, 11 Recorded, 1,427 Missing.

### Probes compared with the game (W1-8)

- **D120. The comparison** is `ProbeComparison` in `tests/MirrorImaged.GameTests`. Its test without the game runs every
  probe in `probes/` in MirrorImaged and compares its lines with the game's in `probes/expected/<probe>.txt` (D71's
  format), so CI does it on all three platforms. A probe's map runs through the seam without blizzard.j (D65), as rule
  5 of `docs/tasks/how-tasks-run.md` asks, with the three blizzard.j functions the map's `config` and `main` call
  (`SetPlayerSlotAvailable`, `InitGenericPlayerSlots`, `InitBlizzard`) given as functions that do nothing (replaced by
  D150). With the
  installed blizzard.j it would be no closer: `InitBlizzard` stops at `IsFogEnabled`, Missing, and the rest of it needs
  sounds, rects and units (later waves). So a probe reading what those three set (players) or calling another blizzard.j
  function (`PolledWait` in triggers and triggers-2, `GetPlayersAll` in players) differs there, recorded as below. The
  run is advanced a second at a time, for at most the 2 minutes the game runner gives a probe (its hidden desktop runs
  game time as real time), until the probe's Preload file is in the run's documents; the run stopping after it
  (`EndGame` is Missing until W1-9) is the game ending there. The lines that differ are found by the longest common run
  of lines, and those between the same two common lines are paired in order, so each difference names the expected
  file's line and both values. The test with the game (`Needs=Game`) runs each probe in the game first and fails when the
  game no longer gives the expected lines (a stale record), then runs it in MirrorImaged as above; one probe: `dotnet
  test tests/MirrorImaged.GameTests --filter 'Needs=Game&DisplayName~name: \"triggers\"'` from PowerShell. It replaces
  the four trigger probes' tests and `hello`'s, now that every probe has an expected file: `hello` and `game-time` were
  recorded (11-08), `game-time`'s real seconds as `{t}`.
- **D121. MirrorImaged's differences are recorded per probe** in `probes/differences/<probe>.txt`, in the comparison's
  order: the game's line after `- `, MirrorImaged's after `+ `, or `! ` and the first line of the message when its run
  stopped without lines, with notes (`# `) giving why and the decision that makes the native Close. The test holds the
  file to the differences exactly, both ways, so a new difference fails and so does one that has gone; its message
  lists every difference with its line and gives the file's lines without notes, to paste. A stop is one entry, not
  every line, as a Missing native in a probe loses all its lines: `CreateUnit` stops triggers-2 and `Location`
  strings-2 (wave 2); the lane that brings the native updates the file. One file a probe, so lanes changing different
  natives seldom touch the same file. With W1-6 and W1-9 merged, of the 31 probes 2 stop so, 3 close the game as the
  game does, and of the 26 that give lines 12 give the game's and 14 differ: strings, strings-4, game-time, lua-config,
  timers and events where the native or the model is Close; handles-2 and handles-3 on ids given again (D111) and
  `Condition(nil)` (D114); and players, lobby, triggers, triggers-3, handles and forforce-wait also where blizzard.j's
  functions, which the comparison runs without, would act (ids, teams, the neutral victim, `PolledWait`,
  `GetPlayersAll`), which W1-10 is to settle. The lobby and events probes run on W1-9's map with a computer (D140) in
  MirrorImaged as in the game; the events probe's side files (D147) stay a game test of their own.
- **D122. A game that closes is a line.** A probe whose game exits by itself after its map loaded, before the lines
  came, ends `Closed` in the runner (new), and gives the one line `! the game closes`; so does a MirrorImaged run that
  stops with a `GameClosedException` before its lines, and an expected file holds it as any line. `Closed` needs the
  probe's `started.pld`, written in the probe's first timer before the probe runs, so a game that exits as its map
  loads isn't a probe closing it: the three closing probes run again (11-08) each wrote it before the game closed.
  `R2SW` of a number below 0 whose text passes 255 bytes now throws it, replacing D84's cut, as the game closed on
  -1.5 at widths 254 and 1000; the check sees the length as it wraps, so a width whose length passes a 32-bit integer
  still gives `""` below 0, as above 0 (not measured below 0; a test pins it). W1-3's six one-call `R2SW` probes are committed as `probes/r2sw-*.lua`, and W1-5's `files-text` (`Preloader` of
  a file that isn't JASS) as `files-text.lua`; each was run again (11-08) and gave what the facts say, the two at -1.5
  and files-text closing the game.
- **D123. The runner copies the account's campaign files too:** before a run, each account's
  `Campaigns\*\Campaigns.w3v` and each campaign's profile, named after its folder (`Classic\Classic.w3p`,
  `Reforged\Reforged.w3p`, `ForsakenKingdom\ForsakenKingdom.w3p`), as D59 copies `LastReplay.w3g`, to `Saved by
  MirrorImaged\<name> <write time><extension>` beside it, never over a file, and not when its hash is one a probe run
  made; after the run the hashes of those that changed join the same list, still `probe-replays.txt`, so the hashes
  noted before stay. Every game rewrites the profiles with the same bytes, so their write time changes each run: a
  file is not copied when a copy in its folder has its content, whatever the copy's name (before, only a copy of the same
  name counted, and this task's first runs left 34 copies of one `Classic.w3p`, the 33 extra ones since deleted).
- **D124. A line can vary in a word too, and keep its place:** in D71's format, `{w}` stands for a word (ASCII
  letters, digits and underscores) as `{t}` stands for a number, and either can stand in any line: a line without
  `~ ` keeps its place and matches by its pattern, and `~ ` is only for a line that moves. A `~ ` line needs a token
  and some fixed text, so it can't match any line at all. The players probe run again gave other races for slots 0
  to 23, as the lobby draws them at random (D92), so those 24 lines have `RACE_{w}` for the race, in order; the two
  runs' races are in the file's notes. `game-time`'s real seconds and `hashtables`' `SaveFogStateHandle` answer
  (D125) are in-order lines with `{t}` and `{w}` too.
- **D125. `SaveFogStateHandle` is Close:** its answer for `FOG_OF_WAR_MASKED`, a Convert native's value, was false in
  W1-3's run of the hashtables probe and true in W1-8's, every other line the same; the value was saved and loaded back
  both times. So the game's answer there isn't fixed (perhaps whatever its return register held; not known), and the
  native still gives false (D85), with the line given `{w}` there.
- **D126. The Lua build as the game's, probe lua-config** (two runs, the same lines): D33's Windows configuration holds
  (`math.log(8, 2)` 3.00000024 and `math.log(1024, 2)` 10.000001 as here, `0x1.0000011p0` 1, `%a` of 0.1
  `0xc.ccccdp-7`, `os.date` refusing `%F`, `%e` and `%Ey` and taking `%#x`), and so does D35's build without
  `LUA_COMPAT_5_2` (no `math.pow`, `math.ldexp`, `math.log10`, `bit32`, `unpack`, `loadstring`, `table.maxn` or the
  rest; `utf8`, `table.move`, `math.ult` and `rawlen` as here). D34's integer made a float by the C cast is now
  measured: `i + 0.0` and `i / 1` round to nearest, ties to even (16777217 to 16777216, 16777219 and 16777221 to
  16777220, 2147483647 to 2^31), and match here, where `I2R` cuts toward zero. Two differences are fixed or kept:
  the game has no `string.dump`, so the sandbox drops it now (`sandbox.c`; the arena's addresses moved, so
  `LuaDeterminismTests`' pins were taken again); and the game's `load` seems to read a binary chunk (inferred: the
  start of one gave "truncated precompiled chunk", with mode `bt` and `b`, so it reached the bytecode reader), which
  stays refused here, D36's reason holding: without `string.dump` a map can still write bytecode by hand. That keeps
  `lua-config` different from the game for good, an exception to A6 that `docs/tasks/later.md` lists for Sven's
  review. Left as differences, recorded: `math.log(3, 2)` and the base-10 logs (D32's log), and `pairs` over 20 string
  keys, which visits them in the same order in both of the game's runs but not in this one's; inferred from that, the
  game's string hash seed is fixed and isn't 0 (D35), though another hash function or table growth would change the
  order too. Finding it is W1-10's.
- **D127. Timers due at the same moment, probe timers** (two runs, the same lines): six timers due at 1.0 ran in the
  game as resumed, first, periodic, second, late, restarted, which is neither the order they were made (late,
  restarted, periodic, resumed, second, first), nor started, nor D45's order of queueing (first, second, resumed, late,
  restarted, periodic, as here). A 3600 s timer read every 0.01 s by a periodic timer gave elapsed times as if read at
  the hundredths themselves and cut toward zero as D45 cuts (0.01001, 0.02002, ...), but 0.25024 and 0.50024 where
  0.25 and 0.5 would read whole, and whole at 0.75 and 1.0 (so the periodic timer's expiries are a hair past 0.25 and
  0.5, perhaps summed timeouts); here the expiries run in the nearest 1/1024 s step, which the clock reads, so 0.00977,
  0.01953, ... Both are recorded as differences for the timers' follow-up: D45's step and order are Close until a
  model matches.
- **D128. After W1-8,** with W1-6 and W1-9 merged first: 130 Exact, 267 Close, 11 Recorded, 1,427 Missing
  (`SaveFogStateHandle` went to Close, D125).

### Map files and the game's standard data (W2-1)

The probes `data`, `data-objects`, `data-tags`, `data-names`, `data-sets-melee`, `data-sets-tft` and `data-sets-fk`
(`docs/facts.md`, "The game's data") measured what the facts left open; the choices below follow them, the facts'
"Object data", and Moonwell.GameData's reading of the tables, ported. What W2-1 leaves is in its tasks: W2-2, W2-3,
W2-4, W2-6 and W2-8 (`docs/tasks/wave-2.md`) and `docs/tasks/later.md`.

- **D200. One data model, `GameData`:** the standard data (`StandardData`) with the map's changes, and the map's files
  (`MapArchive`). An object is found by its kind and id (`Object(ObjectKind.Unit, id)`; ids are the game's, `FourCC`'s
  order), since two kinds may share one; a field by its id (`uhpm`, `Hhb1`) and level, as text (`Text`), or read as
  C's `atoi` and `atof` read it (`Integer`, `Real`: the longest number at the start, else 0), and null or 0 for none.
  A field without levels is read at level 0 whatever level is given; one with levels from 1 to the object's count
  (`alev`, `glvl`, `bvar`, `dvar`; 0 for the other kinds), and past the count it has none, as the standard Storm Bolt
  reads 0 past its third level (game-facts). A table column's level past the 4th with no value of its own, in the map
  or the tables, takes the level before's (game-facts: a 6-level Storm Bolt reads its 4th column at 4 to 6; the patch
  note). A level of a field in the text files is its list's entry, and past the list's end there is none. The natives
  decide what none reads as: tooltips past the last level read the last one (game-facts), which is W2-7's.
- **D201. The standard data is read from the install's storage** through one seam, a function giving a file's bytes by
  its path in `War3.w3mod` (`units/unitdata.slk`, `_balance/custom_v1.w3mod/units/unitdata.slk`,
  `_locales/enus.w3mod/units/humanunitstrings.txt`), null when there is none, which is passed over. Each kind's
  metadata, tables, func and skin files and locale strings are Moonwell.GameData's, merged in its order (a later
  value replaces an earlier one), with the files named one by one (the race files in alphabetical order) instead of
  listed, as the storage has no folders: the first table names the objects and another table's row of any other id
  is dropped; a text section is the object's in any case, its exact id first (`[Boac]` is `BOac`'s), and its name is
  what is between `[` and the first `]`, as the install has sections with words after the bracket (`[Hleo] leonid`,
  `[AHw2] - T2 - Spell Blade`), whose keys would otherwise land in the section before (fapo took Leonid's icon); a
  key is found in any case, and a later file's `UberTip` replaces `Ubertip`. The `*Addons.txt` files of 3.0 aren't
  read, as Moonwell doesn't. The text is the installed locale's (the build's tags, `enUS` text). The SLK and profile
  readers are written here (`Tables`), keeping each cell's text, as Moonwell's keep-text fork of War3Net.IO.Slk does
  (Moonwell's package); unlike the SYLK format, a value's `;;` and `""` aren't read as a semicolon and a quote, as no
  game file has them. Reading takes about 1.2 s (0.5 s opening the storage, 0.25 s `AbilityData.slk`); it is done
  once a process for each install folder, build (its locale too) and pair of balance sets (`StandardData.Installed`);
  a cache between processes is `later.md`'s (wave 10).
- **D202. Which balance set a map reads** (`StandardData.Sets`): the facts' table (Forsaken Kingdom the base tables;
  Custom `custom_v0` or `custom_v1`; Melee with Reign of Chaos `melee_v0`, with The Frozen Throne the base values and
  `custom_v1`'s objects), and, from the probes, the Default data set read as Melee on a map with the melee flag and as
  Custom on another, and an info file of format 28, which has no data version, read as The Frozen Throne. For Melee
  with The Frozen Throne, measured: units (fapo's life 0), items (`CreateItem` of ckdk nil) and abilities
  (`UnitAddAbility` of AA12 false) that `custom_v1` leaves out don't exist, while Forsaken Kingdom has all three; and
  the constants are the base file's (`DamageBonusPierce`: a footman took 80.36 of 100 pierce, as on Forsaken
  Kingdom, where a `custom_v1` map's took 89.29). Inferred: the same for buffs, upgrades, destructables and doodads,
  for the func and skin files, and for the 14 other constants `custom_v1`'s `MiscGame.txt` changes; formats 25 to 30
  read as The Frozen Throne and older ones as Reign of Chaos (25 is World Editor's first for The Frozen Throne). A map
  without an info file reads `custom_v1`. 257 of the install's 273 maps read the base values with `custom_v1`'s
  objects. Which AI scripts a set holds is `later.md`'s (wave 7).
- **D203. A value kept per graphics mode is read as Definitive Edition graphics read it** (`:de`, else `:hd`, else
  the plain key, never `:sd`; game-facts, graphics mode 2), as Moonwell does: a run shows nothing, the facts measured
  that mode, and it is the one Sven plays. Another mode isn't offered.
- **D204. The map's changes:** each object file and then its skin file (the data probe: hkni read the skin file's
  name), a change to a standard object changing it, a new object starting from the standard object it is based on and
  not the map's change to it (game-facts; the data probe for an upgrade's levels), and a change to an object the
  balance set doesn't have dropped (not measured). War3Net keeps an id's first letter in its lowest byte, so its ids
  are reversed to the game's (`docs/upstream.md`). A string value's `TRIGSTR_n` is resolved as the map is read; a
  number is kept as its invariant text (D214). A doodad's variation is taken as the file's number, as a level is;
  whether the file counts variations from 0 is W2-4's (from 1: D257).
- **D205. A field's place comes from its metadata row,** as Moonwell.GameData's `Fields.ColumnOf` finds it: the
  column is `field` with the data pointer's letter (`DataA`); a field of each level in a table has a column a level
  (`DataA1`), in the text files a comma list, or with `appendIndex` a key a level (`Requires`, `Requires1`); a field
  sharing its `field` with others, or of the text files with an index, is that entry of a comma list (`Buttonpos`'s 0
  and 1, a unit's `Name`), an entry in quotes keeping its commas; a blank cell, `-` and `_` are none. Which fields
  apply to which object isn't checked: any field reads its column, so an ability's field read on another ability
  reads the other's column of that place. Not measured: units' (`useUnit`, `useHero`, `useBuilding`) are W2-2's
  (answered by D220: the natives don't read them), abilities' (`useSpecific`) and changes stored by column W2-8's.
- **D206. A `TRIGSTR_n` given to a native that reads it is the map's string n,** resolved where its string argument is
  read (D80), n read as `atoi` reads it after the prefix in capitals (`TRIGSTR_` and `TRIGSTR_abc` are string 0,
  `TRIGSTR_-1` none); a string the file doesn't have stays as it is. The data probe measured which natives read it:
  `GetLocalizedString`, `SetPlayerName`, `StringCase` and `StringLength` do; `StringHash`, `S2I`, `S2R`, `SubString`
  and `SaveStr` don't, and `ParseTags` doesn't (data-tags), which its task (W2-8) keeps so. `DisplayTextToPlayer` and
  `DisplayTimedTextToPlayer` resolve it too, inferred: World Editor writes the texts a map shows as `TRIGSTR_n`, and
  the game shows the strings. The list is `ReadsTrigstr` in `Simulation.Lua.cs`; a task bringing a native that shows
  or keeps text adds it when it measures it.
- **D207. Runtime tests give their own data** (rule 5): `StandardData` is empty when made with `new`, and a test adds
  objects to it with their values by field and level (`Add(ObjectKind.Upgrade, "Rhme", {["glvl"] = "3"})`, `"Hhb1:2"`
  for a level) and strings, hotkeys and constants to its dictionaries; `Simulation.Load(path, blizzard, options,
  data)` runs with it, and without data with an empty one, where every object is missing and the day is the standard
  24 hours of 480 s. A test object has no metadata, so its fields read exactly the level given, with no carry past
  level 4. The map's own files are always read, in tests too.
- **D208. The day's hours and length are the constants `DayHours` and `DayLength`,** the map's own over the game's: the
  time moves `DayHours / DayLength` hours a second, comes round at `DayHours`, and reads no more than 24 (game-facts:
  `DayHours=30` gives an hour per 16 s; set to 26 or 29 it reads 24). A set above `DayHours` less 0.01 is 0, which for
  24 hours is the measured 23.99 kept and 23.995 not; Close: with 30 hours the facts' 29.999 read 24, here 0, and the
  game's 29.9 still read 24 3 s later, where this comes round.
- **D209. A terrain, doodad or placed-unit file War3Net can't read is left out** (null), named in `MapArchive.Unread`
  and `Simulation.Unread`, and written as the run's first log entries (`0.000 map: left out, as it can't be read:
  war3map.w3e: ...`), rather than stopping the run: the game loads the install's three such maps (`docs/upstream.md`),
  and no wave reads those files yet. Any other file that can't be read stops the read, naming it. Fixing the readers
  is W2-6's (DustwallowKeys' `war3map.w3e`) and W2-4's (SilverpineSprint's `war3map.doo`, read: D258); Roundabout's
  `war3mapUnits.doo`, World Editor's file, which the game makes no units from, stays left out.
- **D210. Player names, with D141:** the one person of a single-player game is "Local Player", whatever its name in
  `war3map.w3i` (the players, lobby and data probes); a seated computer is its `war3map.w3i` name, its `TRIGSTR_n` read
  from the strings file, when the map has custom forces with fixed player settings (the lobby probe, "Probe Computer";
  data-names, `TRIGSTR_001` read "The Horde"), and "Computer (Normal)" otherwise (the data probe, a map without custom
  forces whose computer was named `TRIGSTR_002`), a computer seated in a slot the map gives a user too (with fixed
  settings D141's "Player N" stays there; neither measured); every other slot is "Player N". Not separated: custom
  forces without fixed settings; that the names follow the fixed settings is inferred, as the lobby shows a map's own
  names only with them. Several people's names, their accounts', stay unknown, so `GetPlayerName` stays Close. The
  names are written in English, as before, not looked up in the strings (`LOCAL_PLAYER`, `COMPUTER_NORMAL`).
- **D211. `GetLocalizedString` and `GetLocalizedHotkey` are Exact:** the strings are the `StringList`s of the locale's
  `GlobalStrings.fdf`, `InfoPanelStrings.fdf`, `NetworkStrings.fdf` and `DateStrings.fdf`, FrameDef.toc's string files,
  each key in its case, the first of a key kept; a missing key gives the text back. The hotkeys are `UI\MiscUI.txt`'s
  `[Hotkeys]`, any case, a line for the installed language (`KEY:deDE='W'`, `KEY:esES,itIT='A'`) before the plain one;
  the letter's code, 0 for none. A map's own string lists (`BlzLoadTOCFile`) are `later.md`'s (wave 5).
  `GetPlayerTechMaxAllowed` is Exact with the upgrades' level counts, and `AddPlayerTechResearched` stops at them
  (Close: a count already past one, or a limit set below it, isn't measured).
- **D212. Read but not used yet:** placed units, doodads, regions, cameras, sounds and the import list are read as
  War3Net gives them, for the waves that make them, and the map's other files by name (`MapArchive.File`, the FDF and
  TOC files of wave 5). The terrain's heights (`Terrain`, the game-facts' rules for `GetLocationZ`, each corner rule
  tested) wait for W2-6's `GetLocationZ`, which also measures the off-map edge (inferred: a point off the map reads
  its nearest edge); bridges and walkable destructables are W2-4's (D255). `GetObjectName` is W2-2's (it reads a name
  even for an object the set leaves out: fapo's under Melee with The Frozen Throne; done, D229), a kind's ordered
  objects for the random item and creep natives W2-3's, `ParseTags` W2-8's, and the sound files' lengths `later.md`'s
  (wave 5).
- **D213. The data probes are compared as every probe is (D120),** each on its own map: `ProbeComparison.Change` gives
  a probe's change (`DataProbeMaps`), in the game and in MirrorImaged alike, with one probe file a map. In MirrorImaged
  they run with the install's standard data for their map (`ProbeComparison.ReadsData`), so their comparison is
  `Needs=Game`, skipped without an install, and CI compares the other probes as before. `data` reads only natives
  MirrorImaged has and gives every one of the game's 49 lines; `data-names` gives its 3; `data-objects` and the three
  `data-sets` stop at `GetObjectName` and `CreateUnit` (wave 2) and `data-tags` at `ParseTags` (W2-8), recorded in
  `probes/differences`, so their data are tested by `GameDataTests` and `InstalledDataTests`.
- **D214. A real the map changes is kept as decimal text without an exponent** (`0.00001`), as `Integer` reads text
  as `atoi` does and would read `1E-05` as 1; shortcut: a real under 1e-28 is kept as 0 (replaced by D233: every
  digit, as the decimal text cut digits from about 1e-21). A whole number past the
  integers' range reads as the range's end, as the C library's `strtol` holds it (`Integer`), not 0.
- **D215. A day of no length or no hours is the standard one:** a map's `DayLength` or `DayHours` of 0 or below (or
  not a number) is passed over for 480 s and 24 hours, rather than a day that never moves or divides by 0. Not
  measured.
- **D216. After W2-1,** with W1-6, W1-8 and W1-9 merged first: 133 Exact, 266 Close, 11 Recorded, 1,425 Missing.

### The Lua number model fitted to the game (W1-10)

The measurements are eleven probes in `probes/` (`numbers-*`, `string-order`, `timers-2`, `condition-places`), each run
twice with the same lines by the game runner, 11-08, their lines in `probes/expected` and `docs/facts.md`, "The number model's probes". Each
model was found in Python against those lines first, then written into `native/mifloat.c`.

- **D150. The probe comparison's stand-in for blizzard.j** (`ProbeComparison.Frame`) does what blizzard.j does that the
  probe maps and the probes read, written in Lua as blizzard.j has it: its seven handle globals (4 timers, 3 groups, in
  blizzard.j's order), `SetPlayerSlotAvailable` noting the slot, `InitGenericPlayerSlots` with `TeamInitPlayerSlots`
  (`SetTeams` and a team per noted slot), and of `InitBlizzard` `ConfigureNeutralVictim` and `bj_FORCE_ALL_PLAYERS`,
  `GetPlayersAll` and `PolledWait`. This replaces D120's three functions that did nothing: the installed blizzard.j's
  `InitBlizzard` still stops at `IsFogEnabled`, then needs sounds (`CreateSoundFromLabel`), rects and units, which are
  later waves', so running it under `Needs=Game` would compare nothing more. The players, lobby, triggers and
  forforce-wait probes now run to their ends, and their lines on Exact natives (`ForceEnum*`, `GetGameTypeSelected`,
  `IsMapFlagSet`, `GetGameSpeed`, `GetTeams`, the alliances and bounty `ConfigureNeutralVictim` sets, `PolledWait`'s
  natives) all match the game; what still differs is the time of day (D96, D149), an upgrade's level count (W2-1) and
  raw ids (D110), whose stand-in handles are made after the players and miss `InitBlizzard`'s sounds, rects and units.
  Left out, of `InitBlizzard`: `InitBlizzardGlobals`' seven filters, `bj_FORCE_PLAYER`'s 28 forces and the rest of its
  globals, `InitQueuedTriggers`, `InitRescuableBehaviorBJ`, the sounds (`InitDNCSounds` and the others), `InitMapRects`,
  `InitSummonableCaps`, `InitNeutralBuildings` and `DetectGameStarted`'s timer; no probe reads what they make or set but
  as raw ids, the handles they make shifting the ids after them. It is not blizzard.j: a probe calling another BJ
  function gets nil, as before.
- **D151. The game's arithmetic, measured further** (probes `numbers-ops`, `numbers-div`, `numbers-fine`). A
  subtraction is the exact difference cut toward zero, not D27's single guard bit (`numbers-ops`: all 72 shifting 1 to
  22 octaves; the guard bit missed 31); a number 23 or more octaves smaller is still dropped, and of two of the same
  size the second is kept, so `0 + -0`, `0 - 0` and `-0 - 0` are -0. A result is packed with its exponent or-ed in, never
  range-checked: `2^-126 - (2^-126 + 2^-149)` is `f5000000`, its exponent borrowing into the sign, and `-2^127 * 4` is
  `80000000` (D28's addition gave 0). A product is +0 when its exponent is outside 1 to 256 before the mantissas'
  carry: `1.5 * 2^127 * 6` (carry to 257) is `80900000`, `2^127 * 8` is 0. Division is the dividend times a reciprocal
  of the divisor's mantissa. The game's reciprocal at the 1024 divisors 1 + k/1024 is measured: `2^47 / (m - 1)` of
  the mantissa m, cut, but for the first knot, 1/1, which is `2^24` (1.0; the formula would pass it). Between the knots
  the model is a fit, not the game's algorithm, which wasn't found: straight lines to `(2^47 - 2^16) / m` at the next
  knot (`2^23` at the last), the interpolated value cut and one added. On divisors with every mantissa bit varied it
  misses about 1 in 5 by a step (the 312 random 1/d 55 times, the 192 random n/d 41 times); over all 2,887 quotients
  measured, knots and grids included, 2,753 (D29's model 1,597). `(int)(n / d)` for n 1-2000, d 2-100 now goes wrong
  268 times and `Ceiling` 893, as in the game (102 and 956 before). So division stays Close. Also measured: a number
  divided by the same bits is 1 (`0/0` and `-0/-0` too), and any other zero or subnormal dividend gives +0 (`-0/0`
  and `0/-0` are 0). `1 - 0.001` is `3f7fbe76`, as W1-7's JASS probe measured (D27's guard bit gave `3f7fbe77`).
- **D152. The game's maths** (probes `numbers-sqrt`, `numbers-trig`, `numbers-functions`, `numbers-fine-2`).
  `math.sqrt` is one more than the integer root of the mantissa times 2^8 (46341 to 65536), made a number as 1 + 46341
  (r - 46341) / 2^31, times the float nearest sqrt(2) for an odd exponent; it repeats every octave, not D31's two. It
  gives 6,156 of the 6,162 roots measured (`numbers-sqrt`, `numbers-fine`, and `numbers-squares`' n * n for n 1 to
  4096): for six squares the game's integer root is a step higher, its m * 2^8 just below a square (by 49 and 68),
  while two others 25 and 145 below aren't, so no offset fits; (int)Sqrt(n * n) goes wrong 64 times in the game, in plain
  Lua too, and 70 here. So Close. `math.sin` and `math.cos` fit all but three of the measured: the angle x times the float nearest
  1/(2 pi) in the game's arithmetic, cut toward zero to 2^-20 turns (a quarter turn more for the cosine), then a table
  of sin(pi k / 2048) for half a turn, in 2^-31 and cut (its top 2^31 - 1, its last 1), each 256 steps between two
  values on a straight line divided into 255, negated for the other half: 2,333 of 2,336 (the three a 2^-31 step near pi
  and its multiples) and 716 of the 720 whole degrees (sin 19 and 199 degrees, cos 109 and 289, a float step off; their
  angles were made in C#, which may differ). A rounded table or a float one fits fewer. So Close. The table is worked out in doubles when read, the same on every platform, rather than
  held. `tan` is sin over cos, as D32 had it, so Close with the division. `math.rad(x)` is `x * pi / 180` and
  `math.deg(x)` `x * 180 / pi` in the game's arithmetic (all 130 measured). From 1 and past it `asin` is the float
  nearest pi/2 and `acos` 0, from -1 down -pi/2 and pi (`asin(2)` is pi/2, D81's question); `atan(y, x)` on an axis is
  0, pi/2 or pi, those floats, and `atan(0, 0)` pi/2; the natives keep their own 0 outside the domains and for
  `Atan2(0, 0)` (`Maths.cs`). `exp` below 0 is 1 over `exp(-x)` in the game's division (135 of 160 pairs; `exp(-104)` is
  2^127, as 1/0 is), past the range packed as a product (`exp(89)` a nan, `exp(100)` 0). `log` reads a zero or
  subnormal number as 1.m * 2^-127 (`log(0)` -88.0296860) and is 0 below 0. A whole power from 1 up is multiplied
  out, squaring as it goes (1000^10 is `7149f2c9`; one by one would be `7149f2c8`), so `(-8)^3` is -512; any other is
  `exp(y log|x|)`, so `(-8)^(1/3)` is 1.99999964 and `(-8)^-1` 0.125; 0 to any power but 0 is 0. Still Close and
  D32's doubles: `exp` itself (the game's moves by a constant slope over runs of inputs, a table on straight lines),
  `log` (right near 1, off from 1.5 up: `log(1000)` by 41 float steps, `log(1.5)` by 5e-7), `atan` (the game's is x
  (1 - 2^-20) near 0), `asin` and `acos` inside the domain, and the powers that go through them. `log` and `log10` read inf
  and nan through the double's exponent (`log(math.huge)` 709.78), not measured. `SquareRoot`, `Sin`, `Cos` and `Tan`
  match every case the strings probe measured, but not every case of the number probes, so all stay Close.
  `S2R`, which divides, now matches 74 of its 77 measured strings (D83's 67).
- **D153. Timers due at the same moment** (probes `timers`, `timers-2`): they run in the order they were started or
  resumed, a periodic timer keeping the place of its start for every later expiry (D45 queued each expiry when the
  one before ran; measured: `timers-2`'s a2 and `timers`' periodic timer ran in the place of their start). That a timer
  paused and resumed in one instant is queued again is inferred from one case (`timers-2`'s b2 ran after b4, which was
  started before the resume). This matches every order measured but one: in `timers` the timer paused and resumed at
  0.25 for 0.75 ran first, before those started at 0. Another rule fits all of them: ends in the game's float clock,
  ties in the order started or resumed, if that timer's end fell a hair before 1.0; it needs the clock below, so the
  model takes the order rule. The clock reads are not modelled (inferred from three probes): a timer read in the
  instant it was started reads one float step of its timeout past 0 (2^-23 for 2 s, 2^-12 for 3600 s, 2^-7 for
  100000 s), and the 3600 and 100000 s timers read a float step past each whole second up to 6 (`game-time`'s 100000 s
  timer up to 7, whole from 8). That fits a float clock starting a hair past 0, between 4.8e-7 and 9.5e-7 s, which it
  keeps while its value is small enough to hold it, and a timer's end made as start plus timeout in floats, which drops
  it. The 0.01 s periodic timer's reads in `timers-2` aren't that simple: the 2 s timer's elapsed time drifts from 0.01
  times the count by up to 2.2e-7 and is exact at the 25th, where the 3600 s timer reads a step past 0.25 (`timers`: at
  0.25 and 0.5). Here the clock is the 1/1024 s step (D45), so `TimerGetElapsed` and `TimerGetRemaining` stay Close and
  the timers probes' read lines are recorded differences.
- **D154. The string hash seed is `0x1c46318e`** (probes `lua-config` and `string-order`): the 20 keys of `lua-config`
  fit 49,152 seeds, whose low 20 bits are one of 12 patterns, and of those the six sets of 24 keys of 8 to 60 letters
  in `string-order` fit one; Lua 5.3.6's own hash and table code reproduce every order with it. So `pairs` over string
  keys visits them as the game's does (the same in three games over the day; whether it changes between machines or
  installs isn't measured). This replaces D35's seed 0 (`native/lua/lstate.c`); D126's inference holds.
- **D155. A condition's place taken twice** (probe `condition-places`): as D115 modelled it. c1 destroyed and taken by
  c2, c2 destroyed and taken by c3: the triggers of c1, c2 and c3 all ran c3; destroying the stale c1 freed c3's place
  (all three ran nothing), the next condition took it (all three ran it), and destroying the stale c2 did the same.
  `And(nil, nil)`, `Or(nil, nil)`, `Not(nil)`, `Condition(nil)` and `Filter(nil)` made after a destroy took no place,
  and the next `Condition` did. After W1-10, with W2-1 merged first: 133 Exact, 266 Close, 11 Recorded, 1,425
  Missing, as after W2-1 (D216).

### JASS maps (W1-7)

The measurements are thirteen JASS probes, `probes/jass-*.j`, run in the game's JASS machine (D130), and
`probes/blizzard-numbers.lua`, in `docs/facts.md`, "JASS maps", with their lines in `probes/expected`. W1-8's
comparison (D120) runs them as it runs the Lua probes: `ProbeComparison` takes `probes/*.j` too, and builds and runs
each with `ProbeMap.BuildJass`; where MirrorImaged differs by the decisions below, `probes/differences` says so.

- **D130. A JASS map runs as Lua.** `Simulation.Load` reads `war3map.lua`, else `war3map.j` (or `scripts\war3map.j`,
  where older maps keep it); a map with both runs the one `war3map.w3i`'s script language names (not measured). A JASS
  script is made into Lua by War3Net's transpiler against common.j's natives and
  globals (declarations made from `GameNatives`, as the Preloader's, D102) and the installed blizzard.j, with JASS's
  rules (D132 to D137), and runs as the chunk `war3map.lua`, its `config` and `main` as a Lua map's, so an error names
  the Lua's line. JASS that doesn't parse, or calls, reads or sets a function or variable no file declares, stops the
  load with an `InvalidDataException` ("The game wouldn't load ..."): the game didn't load a map whose JASS it refused
  (a rawcode `'a\bc'`, and a first jass-numbers with exponent literals); that an undeclared name does the same is
  inferred; Lua would call nil. Any other failure to make the Lua is MirrorImaged's, a `NotSupportedException` that says
  so. A probe can be JASS: `ProbeMap.BuildJass` puts it in a `war3map.j` beside `out` and
  `done` written in JASS, and calls no blizzard.j function, so the tests' seam (`Load(path, "")`, which transpiles
  against common.j only) runs the JASS probes' maps on every platform; JASS has no `pcall`, so a probe runs what may
  stop a thread in one of its own (`ExecuteFunc`), and a timer ends the probe 30 s in. The map with `'a\bc'`, which
  gives no lines, is a game test's inline probe instead. The rewriting is `Jass.cs`'s, the JASS map's run
  `Simulation.Jass.cs`'s.
- **D131. blizzard.j two ways.** A Lua map's blizzard.j is the game's own Lua: there `ModuloInteger(7, -3)` is -5,
  flooring as Lua's `//` does (probe blizzard-numbers), so the integer `/` stays War3Net's `//`; and a real global given
  a whole number is a float (`bj_MELEE_MINE_SEARCH_RADIUS` 2000.0), so a whole number written where a real goes is
  written as a real, in every place a real is taken: globals measured; locals, sets, arguments and returns inferred.
  This settles D68's first part for Lua maps (`docs/upstream.md`). A JASS map's blizzard.j is transpiled with JASS's
  rules, as the game's JASS machine runs it: there `ModuloInteger(7, -3)` is 1, measured on a copy of its body in probe
  jass-numbers, inferred for blizzard.j's own. The two are cached apart: D66's hash takes the rules. A Preload file stays as D102 has it, without JASS's rules.
- **D132. Integers.** +, - and * wrap at 32 bits, as Lua's integers here do (probe jass-numbers). `/` cuts toward zero
  (`-7 / 2` is -3), where Lua's `//` floors, so it calls `MirrorImaged_Divide`. Dividing by zero ends the thread (probe
  jass-divide-integer), and -2147483648 by -1 closes the game (probe jass-divide-overflow: exit code 1, `BlizzardError`),
  here a `GameClosedException` stopping the run (D43): `war3map.lua:N: The game closes: ...`. A decimal integer past
  32 bits wraps (probe jass-integers: 4294967295 is -1, 99999999999 is 1215752191), where Lua would read it as a float,
  so it is written wrapped, and -2147483648 in hexadecimal, which Lua wraps too.
- **D133. Integers given to reals** are cut toward zero, as `I2R` (D87): 16777219 is 16777218 set to a real, given to a
  real parameter, passed to `I2R`, added to 0.0 and written out, where Lua's conversion rounds (16777220). A whole
  number written out is converted as the JASS is rewritten; another passes through `MirrorImaged_Real` (Lua's
  conversion where it is exact, else `I2R`), also as the integer side of a sum, product, quotient or comparison with a
  real, and as a native's real argument (not measured for natives). A function returning real that returns an integer gives the integer's bits as a float, unconverted (16777219 gives
  0x01000003; probes jass-numbers and jass-reals): `MirrorImaged_Bits`.
- **D134. Reals.** The JASS machine's arithmetic is taken to be the game's Lua's (D27 to D29): every measured sum,
  product and quotient agrees once its operands' bits do (1/3, 2/3, 10 × 0.7, 1 - 0.9, 0.1 + 0.2, 3 × 0.1, and the
  drops of 1 - 2^-30 and the like, and since W1-10's model `1 - 0.001` and `1000 - 0.001`), inferred, not proved. One
  pair doesn't: `-1 + 0.001` and `-1 - 0.001` (probe jass-reals' ladder at -1) come out a float step from the game's,
  which look rounded toward minus infinity where the model cuts toward zero; W1-10's model's to settle. A real literal is read as the
  game's JASS reads it: its whole part and its decimals each as `S2R` reads them, added in the game's arithmetic, and
  written into the Lua as that float's exact digits (probe jass-literals: 0.1 is 0x3dccccce, a step above the nearest,
  as `S2R`'s; 1.001 is 0x3f8020c4, a step below `S2R`'s; all 14 fit where D83's `S2R` does, and since W1-10 one
  differs by its Close step). A real divided by zero ends the thread (probe jass-divide-real), where the game's Lua gives 2^127 (D29): a
  division whose divisor isn't a nonzero number written out calls `MirrorImaged_DivideReal`. `==` on reals holds when
  |a - b| < 0.001 or is under two float steps of the larger magnitude (probe jass-equal: the largest equal step on
  either side at 0, ±1, 0.25, 4, ±1000, 2^20, and 2^12, 2^20 and 2^23 times 1 to 1.875, all fit, swapped too; probe
  jass-reals agrees), `MirrorImaged_Equal`, also between an integer and a real; `!=` compares exactly (1.0 == 1.0001 and
  1.0 != 1.0001 both hold), as do `<`, `>`, `<=` and `>=`. Why `==` has two margins isn't known.
- **D135. The operation limit.** The game ends a JASS thread after 3,000,000 of its operations: loops of eleven
  shapes ran 428571, 300000, 300000, 333333, 230769, and with an integer's `/`, a real's `==`, a string's `+`, an
  integer added to a real, a read that may come before a set and one set on both sides of an if, 230769, 250000,
  230769, 214286, 187499 and 187499 times, 7 to 16 operations each; a thread `ExecuteFunc` starts counts its own, and
  its caller's count goes on after; a wait (`TriggerSleepAction`) starts the count again (probe jass-threads). A JASS
  map's Lua state counts Lua instructions instead (`LuaState.OperationLimit`, a count hook set by `sandbox.c`'s
  `mirror_limit`), from a thread's start and from each wait's end, and for the script's top, `config` and `main` each;
  past it the thread raises "the thread passed JASS's operation limit", logged as an error (the game says nothing),
  naming the script's line, and stops as the game's does. JASS's rules are C functions (D138), so only the
  instructions calling them count. The limit is 2,100,100 instructions, the fewest by which no measured shape stops
  before the game's does (the call of an empty function or a native, 7 instructions to 10 operations, sets it): the
  eleven loops then run 1.00 to 2.10 times as often as in the game (525025, 300014, 300014, 700033, 300014, 233345,
  262513, 233345, 233345, 190918, 262512), a local's arithmetic, cheapest in Lua, the most. Close: counting weighted
  operations would need Lua's own loop changed. A Lua map has no limit, as in the game.
- **D136. Variables never set, and null strings.** Reading a variable declared without a value before it is set ends
  the thread (probe jass-threads: integers, reals, strings, booleans and handles, locals and globals, in arithmetic,
  joins, conditions, comparisons with null and as arguments; set first, it reads fine). Such a variable starts as
  `MirrorImaged_Unset`, and a read that may come before a set, by the order of its function's statements, goes
  through `MirrorImaged_Read`, whose error names the variable: a set inside a loop counts only there, and inside an
  `if` only there too, but for one made on every side of an if with an else (a side that returns not counting), and
  one inside a `debug` statement, which never runs, not at all; a global is checked in every function until set
  there. An array is never unset: an integer array's elements read 0, a string
  array's null (here a plain table, where War3Net's `__jarray("")` would read ""), a handle array's null. A string's `+`
  takes null as "", and null + null is null (`MirrorImaged_Join`), `null` written out too. In a JASS map natives take
  null strings (this replaces D80 there; a Lua map, blizzard.j in it included, still refuses nil, as the game's Lua
  binding does): `StringLength`, `S2I`, `S2R` and `StringHash` give 0, `SubString` and `StringCase` null, `SaveStr`
  saves null and gives true, `LoadStr` loads it as null and a key never saved as null too (in Lua "", W1-3), and
  `ExecuteFunc(null)` runs nothing, all measured (probes jass-threads and jass-executefunc-null); the other natives get
  C#'s null, not measured, and what string natives give in a JASS map besides for a null argument isn't measured
  apart from Lua's (the natives give the same). `ExecuteFunc` of a name no function has ran nothing in the game too;
  here its thread raises, which the log shows, and the run goes on as the game's.
- **D137. Rawcodes and debug statements.** The game reads a rawcode's bytes as War3Net does, each after the first from
  0x80 up as signed (probe jass-rawcodes: 'éab' -1029086878, 'aéb' 1623370082, 'abé' 1633796777), so D102's values
  are the game's; a quote in one is a byte as any other ('ab"c' 1633821283). The game refused a map with `'a\bc'`;
  here War3Net reads the backslash as an escape and the map runs, not modelled. A `debug` statement never runs (probe
  jass-debug), as D65 claimed.
- **D138. The rules' helpers** are C functions in `sandbox.c` (`mirror_jass`, through `LuaState.AddJassFunctions`),
  Lua globals named `MirrorImaged_*` set before blizzard.j in a JASS map's state only, so the operation limit counts
  only their calls (D135), and declared to the transpiler, whose syntax factory takes only names JASS allows; a map
  declaring one of these names would replace it, which no map does. An error they raise, a native's error and the
  operation limit name the script's line, passing over MirrorImaged's own Lua (the chunk `mirror`), as a halt did
  already. The rewriter knows each name's type from the files
  as the transpiler does; `MirrorImaged_Read` is declared real to it, since what JASS's types change (an integer's `/`
  and a string's `+`) is rewritten before the transpiler sees them.
- **D139. The JASS maps in `Documents\Warcraft III\Maps`,** read-only: `mirror run` of each, 60 s, after W2-1's merge.
  Of the 299 JASS maps (and 82 Lua ones, which stop as before), 292 run their script and `config` and stop at the first
  line of `main`, `GetCameraMargin` (Missing, wave 5), as the Lua maps do (D99); five stop earlier at another Missing
  native (`DialogCreate` twice and `CreateItemPool` among the globals or `main`'s setup, `Location`, and
  `TriggerRegisterPlayerUnitEvent` through a BJ function); two don't load as Lua: a function with more than 200
  locals, Lua's limit (Gaia's ORPG, Survival Chaos Reborn), left for later (JASS has no such limit; moving the extra
  locals into a table would need the transpiler's output rewritten). The first run found two more, now handled: seven
  maps declare a native themselves (`native UnitAlive takes unit id returns boolean`, `GetUnitGoldCost`), which the
  transpiler took twice and refused, so a native the map declares is taken from the map alone; and one (WotLK RPG)
  has statements after a `return`, which War3Net writes where Lua can't load them (`docs/upstream.md`), so they are
  left out. After W1-7, with W1-8, W2-1 and W1-10 merged first, the natives' levels are unchanged.

### Destructables, trees and doodads (W2-4)

The measurements are the probes `probes/destructables.lua` (six runs as it grew, the lines they shared the same),
`destructables-remove.lua` and `destructables-remove-jass.j`, in `docs/facts.md`, "Destructables", with their lines in
`probes/expected`, and the facts' "Units" and "World Editor and map files". What it leaves is in `docs/tasks/later.md`
(D255, D257) and W2-6's task.

- **D250. A destructable is its type's data with its own life, most life, place, scale, occluder height and
  invulnerability** (`Destructables.cs`). It is dead while its life is 0: `KillDestructable` fires nothing on one whose
  most life was set to 0, though it never died, and fires again on a dead one whose life was set to 20 (probe), so no
  flag of its own is kept. Its death event fires inside `KillDestructable`, a set of its life below 0.405 and
  `RemoveDestructable` (dead or not, with the life it had), with `GetTriggerDestructable` and `GetTriggerWidget` the
  destructable (`==` holds), `GetTriggerUnit` and `GetDyingUnit` nil, and the event id `EVENT_WIDGET_DEATH`; a firing
  carries the destructable beside W2-2's unit (`Firing.Destructable`, `Firing.Unit`). A `RemoveDestructable` inside its
  own death event fires the event again there, nested, each time (probe: three deep, the probe stopping it); a tree
  brought back a second after it died stands again. Without a guard, as World Editor's "Destructible dies -> Remove
  (Dying destructible)" writes it, the game's Lua map fired the event 98 times, killed or removed, and 65 with a pcall
  around the removal, which caught nothing, and went on after the call (probe destructables-remove): its callback
  threads nest in the thread starting them, two of Lua's 200 C calls a level and one more for the pcall, and the next
  doesn't start, silently. So a thread a native starts or runs on is now resumed from the Lua thread calling that native
  (`LuaState`'s calling thread; before, from the main thread, which let 196 nest), which matches all three counts; the
  failed start is logged here (D48). A JASS map doing the same closed the game (probe destructables-remove-jass), so in
  a JASS map a thread too deep to start stops the run with a `GameClosedException` (D122), at the same depth: how deep
  the JASS machine went before it closed isn't measured. Invulnerability is a flag that stops neither native's death
  (damage is W2-5's). The life rules are the probe's: a set keeps its fraction and is held to the most life, and 0.405
  lives; `SetDestructableMaxLife` rounds halves away from 0 (80.5 is 81, 80.4 80), keeps 0 and below, and the life keeps
  its share, at least 0 (20 of 50 is 40 of 100), killing nothing; `DestructableRestoreLife` on a dead one gives the life
  from 0.5 up to the most, 0 the most, and on a living one nothing. One made dead reads 0 of its most. The 28
  `BlzCreate*Destructable*` natives make one as `CreateDestructable` does and are Close: their skin, colour, pitch and
  roll aren't read back, and no probe made one. After a removal every native reads it as nil (its handle is gone).
- **D251. Where a destructable is put** (probe: eight types at twelve places, a free type at fifteen facings; the
  facts' LTlt at 1000,1000 and YT16 at 2600,-1200): a place is cut down to a quarter (floor: -10.1 is -10.25); then a
  type with a pathing texture is put on the 64 grid toward 0 (-96 is -64), or 32 off it away from 0 (0 is 32, -10 is
  -32) when half the texture's cells that way is odd. The texture's sides swap when the type is turned exactly 0 or 180
  degrees: by its fixed rotation (`bfxr`), else by the facing given (0 and 180 swapped; 0.5, 179.9, 360, 540, -180 and
  -90 didn't). A type without a texture is only cut to the quarter. The deck probe's 162 placements at facing 270 fit
  too. A texture's size is its TGA header's, read with the standard data (`StandardData.PathingSizes`), a map's own file
  of that path first (`GameData.PathingSize`), one reader for both. A zero place is +0, as `GetWidgetX` read it. As
  textures are read from the install now, `.tga` joins the game-files check's extensions (A15).
- **D252. One set of widget natives for units and destructables** (`Widgets.cs`): `GetWidgetLife`, `SetWidgetLife`,
  `GetWidgetX`, `GetWidgetY`, `TriggerRegisterDeathEvent` and `GetTriggerWidget` take a unit as W2-2 measured (D220 on)
  and a destructable as the probe here did (`SetWidgetLife` on one is `SetDestructableLife`); an item waits for W2-3 and
  stops the run as the native being Missing, so a map gets no wrong answer, and the five that take one are Close until
  then (`GetTriggerWidget`, which takes none, is Exact). W2-2 and W2-4 were written side by side; W2-4, merged second,
  joined them: one death-registration store, W2-2's lists per widget (D225), so a destructable's death reads only its
  own registrations, and `DestroyTrigger` and the removals let go of them as a unit's are; one id helper (`Ids.Of`,
  public in `MirrorImaged.Data` now, behind W2-2's `Code`); one `EVENT_WIDGET_DEATH`.
- **D253. EnumDestructablesInRect** finds a destructable when the 32 cell holding its place lies in the rect, its west
  and south edges past the rect's: a centred rect finds it from 32 around, a rect whose west edge is its place doesn't,
  one whose east edge is 31 past it doesn't (probe: two types on the 32 grid at 24 rects each; a portal at -1000.5 at
  five, where its place alone would answer otherwise). So a rect of 32 around a destructable never finds it, as the
  facts saw. The order is by squares of 256 world units, in rows from the south, west to east, the last made first in a
  square whatever its place in it: five made in one square came last made first; sixteen in a grid across x = 0 and
  y = 0, made out of order, came by square, the tree at 0, 0 made long before after the g11 it shares a square with;
  squares of 128 or 512 would give other orders (probe). Each is given to the filter, then the callback, with
  `GetFilterDestructable` and `GetEnumDestructable` both it in either (one field here); nil after. A hidden
  destructable is listed (probe), so `ShowDestructable` is Recorded; one removed by an earlier callback is passed over
  (not measured). Whether the filter and callback run in threads of their own is W2-6's to probe with the other
  enumerations (D70); here they run as `ForForce`'s do.
- **D254. A destructable's name is the World Editor string its type names** (`bnam`, `WESTRING_DEST_BARREL`), read from
  the locale's `UI\WorldEditGameStrings.txt` (`StandardData.EditorStrings`, its quotes dropped; `WorldEditStrings.txt`,
  which the task named, doesn't hold them), without the editor suffix: "Summer Tree Wall", "Barrel", "Long Stone
  Bridge", "Shimmering Portal" (probe); a name that isn't a key is itself, and nil's is "". Other languages aren't
  measured (the facts' claim: names are localised per client).
- **D255. Bridges and walkable destructables in GetLocationZ** read the deck shapes measured in the game, Moonwell's
  `Decks.txt` (copied into `MirrorImaged.Data`, MIT, the workspace's own measurement): each standard walkable type's
  variation 0 at facing 270 and scale 1, its height above the ground every 32, in whole units (`Deck`). A deck stands on
  the ground beneath any water at the destructable's place (`Terrain.Ground`; game-facts: the deck on the river's bed),
  turns with the facing unless the rotation is fixed, scales with the scale, and between its points blends the four
  around, or takes the nearest where one has none, as Moonwell reads it; `GetLocationZ` is the higher of the terrain
  and the standing decks within reach of the point (each deck's turn and reach are worked out when it is made). A deck
  follows the life a step late: a killed bridge's still reads in that instant, a restored one's not yet (probe,
  facts). Close: the whole units put most points up to 0.5 off (YT16's middle 287 against 286.5); a dying deck falls
  over 2 s and leaves its ends in the game, where here it is gone at the next step; a restored one comes back over
  0.5 s (7 s with birth); a map's own walkable type reads its base type's deck; and the walkable types without a
  measured deck have none here: the ice floes ITf1 to ITf4, whose decks moved, and DTep and RW00, where no deck read.
  Exact would need the decks measured to a tenth (`later.md`).
- **D256. The map's placed destructables are made as it loads, before blizzard.j** (`GameNatives.Place`), from
  `war3map.doo`: one whose state has bit 0 or 1 set (game-facts: the game skips those of state 0 and 4, which World
  Editor's script makes), unlisted by the enumeration when bit 0 is (game-facts: state 3 isn't listed; that states 1, 5
  and 7 aren't either, from the same bit, is inferred; whether the game makes them at all is open, and here they are
  made, so their decks stand), put on the grid as a made one is (game-facts: a placed YT16 at x 0 stood at 32, its
  facing ignored), at its life share of the file's percentage (not measured). Their handle ids come before blizzard.j's
  (not measured). A probe of placed destructables couldn't be run: the probe map with a `war3map.doo` written by
  War3Net, even an empty one, closed the game as it loaded (facts).
- **D257. A doodad's variations are numbered from 1 in `war3map.w3d`:** the install's maps change the per-variation
  fields (`dvr1`, `dvg1`, `dvb1`) at variations 1 to 10 and never 0, so D204's reading of the file's variation as the
  field's level holds, and a test pins it. `SetDoodadAnimation`, `SetDoodadAnimationRect`, `SetDoodadColor`,
  `SetDoodadColorRect` and the destructable natives that change only how it is drawn (`SetDestructableAnimation`,
  `QueueDestructableAnimation`, `SetDestructableAnimationSpeed`, `SetDestructableColor`, `SetDestructableVertexColor`,
  `ShowDestructable`) are Recorded: no native reads them back. The doodad natives by index (`BlzGetDoodad*`,
  `BlzGetNumDoodads`, `BlzSetSingleDoodad*`) stay Missing: reading placed doodads needs a probe map with a
  `war3map.doo` the game loads, which War3Net's from scratch wasn't (D256); `later.md`, wave 10.
- **D258. A `war3map.doo` whose doodad states War3Net refuses is read here** (`MapArchive.Doodads`), as War3Net reads
  every format it knows but with each state kept as written: `(1)SilverpineSprint.w3x`'s, format 13 with states 10
  and 14 (bit 3, which War3Net doesn't define), now reads its 13,098 doodads, and every map of the install reads its
  doodads. What follows the doodads in the file (the terrain's own doodads, which nothing reads) is left out then.
  `MapArchive.Unread` still names a doodad file that can't be read this way either. This replaces D209 for
  `war3map.doo`; DustwallowKeys' `war3map.w3e` stays W2-6's and Roundabout's `war3mapUnits.doo` stays left out.
- **D259. The smallest locations and rects:** `Location`, `RemoveLocation`, `MoveLocation`, `GetLocationX`,
  `GetLocationY`, `GetLocationZ`, `Rect` and `RemoveRect` (`Locations.cs`), Close, which `GetLocationZ` on decks and
  `EnumDestructablesInRect` need, as the task asked, for W2-6 to take over with its rects, regions and the off-map edge
  (its task says so). `GetLocationZ` is W2-1's `Terrain` with the decks (D255). The strings-2 probe, which stopped at
  `Location`, now stops at `CreateTextTag`. After W2-4, with W2-2 and W2-9 merged first: 221 Exact, 329 Close, 25
  Recorded, 1,260 Missing.

### Every shipped map loads (W2-9)

The install's 466 maps, `War3.w3mod:Maps/` (273) and `War3.w3mod:Campaign/` (193: `Classic` and `Reforged`, each
`ROC` and `TFT`), read 11-08 from 3.0.1.24371's storage. The install has no `.w3n`: its campaigns are folders of maps
(`Campaign/Classic/ROC/Human01.w3m`). Of the 193, War3Net 6.1.2 refused 131: 130 for D300 (three of them, Human03,
Undead05 and OrcX01_02, also have D301's sounds) and Human09 for D301 alone. Neither is an enum value War3Net doesn't
know, so W2-1's three files (D209) are another fix, left to W2-4 and W2-6 as before: a doodad state with bit 3 set, a
cliff type 0 and a random unit mode 641, each in a reader D300 and D301 don't touch.

- **D300. A format-39 `war3map.w3i` of World Editor before 6140 has no race HUD in its players.** War3Net reads format
  39 as 3.0 writes it, with a race HUD after each player's race, and so took the player's flags from the first four
  bytes of its name (`TRIG` of `TRIGSTR_`, the `PlayerFlags` value 0x47495254; for an empty name its 0 and three bytes
  of its start, 0x34000000 and 0x3C000000). World Editor 6117 (one map) and 6127 (129; both 2.0.4.23839) wrote format
  39 without it; the install's maps of 6140 (2.0.4.24127), 7000 and 7003 have it. A format-39 file of an editor before
  6140 that War3Net can't read whole as it is (it throws, or stops before the last byte) is given a race HUD of none in
  each player and read again (`MapArchive`), the map's own race HUD, which 6127 wrote too, kept; one it reads whole,
  as a tool writing the field with an older editor version may make, is kept as it is. Then all 466 info files read to
  their last byte. The 130 are 37 of Classic's Reign of Chaos campaign, 42 of Reforged's and Reforged's 51 of The
  Frozen Throne. Inferred: the line is the editor version, at 6140; the install has no map of 6128 to 6139.
- **D301. A sound whose name differs where `war3map.w3s` repeats it takes the name as repeated.** War3Net refuses such
  a sound. 15 sounds in 4 maps have one, each `g_snd_X` repeated `gg_snd_X`: Reforged's Human03 (1), Human09 (1),
  Undead05 (12) and OrcX01_02 (1); their files repeat every path as it is. The repeat is put in the first name's place
  before War3Net reads the file, as for D300 only when War3Net can't read the file whole as it is; all 223 of the
  install's sound files then read to their last byte. Inferred, not probed: that the game keeps the repeated name.
  Each of the 15 is the variable the map's script makes the sound in (`CreateSound`, as the game makes a map's
  sounds), and none of the first names is in a script; nothing reads the sound file yet (D212).
- **D302. Where the shipped maps stop.** `ShippedMapTests` reads each of the 466 from the install's storage into a
  temporary file and runs it to its first Missing native or through 10 minutes; a map that doesn't read or load, stops
  otherwise, or logs a script error fails it (A5). On `release/1.0.0` with W1-7 merged (11-08), every one runs, JASS as
  well as Lua: 465 stop at `GetCameraMargin` (wave 5), the first line of `main`, and
  `ForsakenKingdom/Scenario/(1)ForgottenHollow.w3x` (Lua) at `Location` before it; none logs a script error, and the
  run takes 70 to 90 s. So every shipped map runs its script and `config`, and where they stop after `main`'s first line
  shows only once `GetCameraMargin` exists. (Where they stop now: D507.)

### Units (W2-2)

The probes `units`, `units-decay`, `units-attribute` and `units-primary` (`docs/facts.md`, "Units") measured what the
workspace's facts left open; the choices below follow them where they measured and say so where not. What W2-2 leaves
is in W2-3, W2-5, W2-6 and W2-7 (`docs/tasks/wave-2.md`) and `docs/tasks/later.md`.

- **D220. A unit is its handle's data** (`unit` in `MirrorImaged.Natives`, as D90 keeps a player's): its type (the
  data model's object, D200), owner, place, life and mana and their most, flags, and a table of what the unit's own
  sets changed, by field id (a value of another field type there is passed over, so a `Convert` of a map's own can't
  break a read). A unit field reads that table, else its type's data: a number, a word read as the
  constant `common.j` gives it, or a list of words read as flags (armour, defence, regeneration and move types, the
  primary attribute, targeted as, classification, placement prevented by); where the field is the unit's state (life,
  mana, move speed, turn rate, acquisition range, fly height, armour, sight) it reads and sets that, as its native
  does. D205's question is answered: the fields' `useUnit`, `useHero` and `useBuilding` aren't read, as the game set a
  barracks' formation rank and level (both not for buildings by the metadata) and a footman's hero-only death message
  flag; what acts as a hero's alone is the data only a hero has: its attributes and their gains per level, its proper
  names (`upro`) and its primary attribute (`upra`) (D227). The units the probes measured are the tests' data; every
  field the probe read and set is in `probes/expected/units.txt`, compared with the install's data (D232).
- **D221. Units take ids as any handle** (D110): one each, a Paladin too (probe units), from `CreateUnit` and
  `BlzCreateUnitWithSkin`, which World Editor's `war3map.lua` calls for the placed units; `war3mapUnits.doo` is
  never read for them (D212). `CreateUnit` is nil for an id that is no unit's (an item's, a destructable's, an
  ability's, 0, one the balance set leaves out) and for no player. A hero (its id starting with a capital, as
  `IsHeroUnitId` reads) starts at level 1 with 25 life a strength and 15 mana an intelligence (the constants'
  `StrHitPointBonus` and `IntManaBonus`), as the facts and the probe read; the rest of heroes is W2-3's. No list of
  the game's units is kept yet: the enumerations bring one (W2-6).
- **D222. Life is the most life less the damage, in floats** (probe units): a set keeps `max - (max - value)`, so
  0.41 reads 0.4100037 on a footman of 420 and 0.4099731 on a Paladin of 650, and a unit whose life then reads below
  0.405 dies inside the call (0.405 kills the footman, not the Paladin). The most life changes it only when it falls
  below the life. A dead unit's life can be set, from 0 to its most, and it stays dead. Mana is kept as set, from 0 to
  its most. Both read Exact on every measured case.
- **D223. A unit stands where it is put** (Close): `CreateUnit` and `SetUnitX`/`SetUnitY` keep the place given, as the
  game does on open ground (probe units, facts), but the game moves a unit made where others stand off them and one
  made off the map onto it ((5000, -5000) to (2032, -2000) on the 32 by 32 probe map), which needs the pathing and the
  map's bounds: wave 3 and W2-6. `SetUnitPosition` waits for them too. The probe makes each unit on a spot of its own
  (a grid 200 apart) so its other lines don't depend on it. `BlzGetUnitZ` and `BlzGetLocalUnitZ` read the ground
  (W2-1's `Terrain`), not the unit's height, as the facts measure; the local one is Close (not measured).
- **D224. Facing and turning are Close.** A facing is kept from 0 below 360 by `%` in degrees, which matches every
  facing from -360 to 720 the probe made but 400 (40.00002 in the game, a float step off); the game's reduction of
  larger angles (1e9 reads 0, 100000 280.0085) wasn't found among a dozen float and double models tried. `SetUnitFacing`
  reads the old facing at once and turns the short way round (half a turn going up), modelled as a steady pace from
  1/32 s after the call, at 16 x the type's turn rate less 3.2 radians a second, at least 0.5: the footman's, knight's
  and gryphon rider's steady pace (6.4, 4.8 and 3.2 for 0.6, 0.5 and 0.4) fit that line exactly; the game eases into
  the end (the knight's last tenth of a turn takes 0.3 s) and its frames don't fall on the clock's steps. The type's
  rate, not the unit's: a footman set to 0.2 turned as at 0.6. `SetUnitFacingTimed` turns over the time given (not
  measured). `BlzSetUnitFacingEx` turns at once (Exact). The turn rate itself is kept as an angle from 0.001 below 2
  pi, as the probe read it (-1 reads 5.283185, 7 0.7168145, 7.5 1.216815, 0 0.001); an angle more than a turn away
  comes round by a float modulo (Close: not measured).
- **D225. Unit events fire in the call, by kind** (probe units): a death fires the unit's widget death events
  (`TriggerRegisterDeathEvent`), then its owner's player-unit events, then its own unit events, each kind in the order
  registered (the probe registered unit, player, widget, unit and saw widget, player, unit, unit); a decay, an owner
  change and an order the player-unit events then the unit's. The registrations are kept per unit and per player in the
  order made, so an event reads only its unit's and its owner's, and each registration knows its list, so
  `DestroyTrigger` reads only its own; an empty list goes, and a unit's goes with it (Release: 8,000 deaths took 24 to
  42 ms with no registration, 31 to 38 with one each, 46 to 54 with each trigger destroyed in its own death action;
  destroying 8,000 one-registration triggers 5 to 11 ms). `TriggerRegisterPlayerUnitEvent`'s filter never runs and
  doesn't hold the event back, as measured, so `GetFilterUnit` waits for the enumerations (W2-6). The responses read the
  firing (D75): `GetTriggerUnit` in every unit event, `GetTriggerWidget` only in the widget event, `GetDyingUnit` only
  in the unit and player-unit deaths, `GetTriggerPlayer` the owner but in the widget event, `GetKillingUnit` nil for
  every death this wave makes, `GetDecayingUnit`, `GetChangingUnit`, `GetChangingUnitPrevOwner`, `GetOrderedUnit` and
  `GetIssuedOrderId` in their own events, nil or 0 elsewhere (not measured outside them). The other unit events register
  and never fire until their waves; `TriggerRegisterUnitStateEvent` and `TriggerRegisterFilterUnitEvent` stay Missing
  (W2-10).
- **D226. SetUnitOwner** (probe units, the facts' "Orders and events"): a living unit gets a stop (851972) whose order
  events read the old owner, then changes hands, then fires its owner change on the new owner's registrations
  (`GetTriggerPlayer` the new owner, `GetChangingUnitPrevOwner` the old); given to the owner it has, nothing happens; a
  dead unit gets no stop. The stop only fires its events: orders are wave 3's. When a stop's event gives the unit away
  first, the call changes nothing more (not measured; the model's, so a map can't fire a second change). Not here:
  the `undefend` (852056) a unit with Defend gets after the change, in the game also for a dead one on its old owner's
  registration, which needs abilities (W2-7, W2-10; recorded in `probes/differences/units.txt`), so `SetUnitOwner` is
  Close; so are `KillUnit` and `RemoveUnit` (`undefend` before a death, twice on a removal) and `PauseUnit` (the
  stunned order and `undefend` on a pause, the earlier move given back on unpausing; game-facts, "Orders and events"),
  whose order events are W2-10's (replaced by D310: done, and a dead unit given away gets no `undefend`; the one seen
  was its death's, so `SetUnitOwner`, `KillUnit` and `RemoveUnit` are Exact). The colour becomes the new owner's when asked, kept
  with `SetUnitColor`'s, `SetUnitScale`'s x and `SetUnitVertexColor`'s parts (kept from 0 to 255), all Recorded: no
  native reads them back (the scale and tint fields keep the type's and 255, probe units).
- **D227. The hero attributes and the field quirks** (probe units, units-attribute, units-primary): reading a hero
  attribute field (`UNIT_IF_AGILITY` and the eight like it, which have no metadata rows) on a unit that isn't a hero
  closes the game, and so does setting its primary attribute to an attribute (3 measured; 1 and 2 inferred), so they
  stop the run with `GameClosedException` (D122); setting a hero attribute there does the same here, inferred from the
  read (not measured). Its gains per level read 0 and refuse a set, its proper names read "" and keep none. On a hero
  the attributes read the type's, the ones with a bonus refuse a set and, with no bonuses yet, read the attribute, as
  the permanent ones do here (the game read a permanent one 1 after its own set: W2-3), so `BlzGetUnitIntegerField` is
  Close; a hero's level set to n reads n + 1 (7 to 8, 3 to 4), inferred: both from level 1, where adding n fits as
  well (W2-3 to probe from level 5). The primary attribute and the tint fields take a set (true) but keep
  their value; the shadow's size, the sight and hiding on the minimap the same; the most pitch and roll read 0 and
  refuse every set; the least attack range refuses a unit without an attack; a ground unit's fly height field takes no
  set, a flier's does, and a flier's most fly height keeps its height between its least (`moveFloor`) and the most.
  Left to W2-3, recorded as differences: an attribute set changes only the field here (the game's strength 30 gave the
  Paladin 850 life), the permanent attributes (read 1 after a set of 7 and of 0) aren't modelled, and a hero's proper
  name is the first of its type's (the game draws one and numbers it). So `BlzSetUnitIntegerField` and
  `BlzGetUnitStringField` are Close.
- **D228. A death and its body** (probe units-decay, game-facts): a unit dies once; its body decays its death time
  later (`UNIT_RF_DEATH_TIME`, the type's `death` unless set), firing the decay events, and is gone `DecayTime` plus
  `BoneDecayTime` after that (2 + 88: the facts' "about 90 s after decay"; inferred from the constants, as the probe
  stopped at 45 s within the 2 minutes the runner gives one, D120; W2-10 is to probe it, and `UnitTests`' 93.25 s is
  this inference); a building fires no decay and is gone its death time plus `StructureDecayTime` later (2.34 + 30,
  measured); a unit whose death type doesn't decay fires none and is gone at its death time (the gryphon rider); a hero
  stays (W2-3 revives it); an exploded unit is gone after the step. `UnitSuspendDecay` keeps the body, but its decay
  event still comes, as measured; a body whose time came while suspended goes in the step its decay is let go (not
  measured); a corpse from `CreateCorpse` never decays (none in 45 s) (replaced by D311: the bones' 90 s is measured, a
  corpse goes 90 s after it is made, and `UnitSuspendDecay` keeps nothing). `RemoveUnit` leaves the unit
  as it was for the rest of its step, alive or dead, and gone after it: every getter then reads 0 or nil, as Lua's
  handle reads as null (D41); a unit removed and then killed in its step stays as a body, as the probe saw. Gone is
  when the step's queue reaches it (`Game.At` in the step), so a 0 s timer started after the removal reads it gone, as
  the facts' does. The body's times are Exact but the bone decay's length; `KillUnit` and `RemoveUnit` are Close for
  their `undefend` (D226).
- **D229. GetObjectName** reads the name of a unit, item, ability or upgrade (its first level's), in that order, the
  map's changes and `TRIGSTR_n` read, and "Default string" for anything else, buffs and destructables too (probe units;
  game-facts: a new buff's and destructable's). It names an object the balance set leaves out too (fapo "Apothecary"
  under Melee with The Frozen Throne, D212), so `StandardData` keeps those objects apart (`Absent`) rather than dropping
  them: they don't exist for any other native. Exact.
- **D230. IsUnitType** reads the kinds the probe measured on twelve units: hero, dead, structure, flying, ground (not
  flying and not a structure), attacks ground and attacks flying from its enabled weapons' targets, melee (its first
  weapon of type `normal`) and ranged (another type), and its classification's giant, summoned, undead, mechanical,
  peon, sapper, townhall, ancient and tauren. Close: stunned, plagued, snared, poisoned, polymorphed, sleeping,
  resistant, ethereal and magic immune need buffs and abilities (wave 4) and read false.
- **D231. Speeds and ranges** (probe units): move speed kept from the constants' `MinUnitSpeed` to `MaxUnitSpeed`, a
  unit that can't move keeping 0; an acquisition range of 50 or less not taken, nor any by a unit without an attack,
  which reads 0 (Close: 51 to 54 not measured); where a map's least move speed passes its most, the most (not
  measured); a most mana below 0 keeps the mana at 0 (not measured); a type's default acquisition range its day sight
  where the data has
  none (a barracks' 900); the propulsion window read in radians as `Deg2Rad` makes them, its default in degrees; the
  fly height read at once, whatever the rate (the game draws the rise). `BlzPauseUnitEx` is a count of its own that
  `IsUnitPaused` doesn't read; what a pause holds back is wave 3's.
- **D232. The units probes read the install's data** in MirrorImaged, as the data probes do (D213):
  `ProbeComparison.ReadsData` takes the names starting `units` too. A probe that closes the game on one call is a
  probe of its own (`units-attribute`, `units-primary`), as D122's closing probes are, since the game's lines are
  written only at the end. `triggers-2`, which runs without data, now runs past its `CreateUnit` (nil without a
  footman), so the differences its stop hid are recorded: ids (D110), the clock's steps (D45, D71), the time of day's
  events (D144), and its unit line.
- **D233. W2-1's small items:** a real the map changes is kept as its shortest float digits written without an
  exponent, every digit (`0.000000000000000000001234567`), replacing D214's decimal text, which cut digits from about
  1e-21; `DisplayTextToPlayer` and `DisplayTimedTextToPlayer` read a `TRIGSTR_n` (D206, tested); and
  `Simulation.Load` lists its `InvalidDataException`.
- **D234. After W2-2,** with W1-10 merged first: 204 Exact, 287 Close, 15 Recorded, 1,329 Missing. Left, with where
  (`docs/tasks/wave-2.md`): heroes' attribute state, levels and proper names (W2-3); a widget for destructable deaths in
  the unit events (W2-4); the weapon fields (`BlzGetUnitWeapon*Field`), armour, killers and damage (W2-5); groups and
  the game's list of units, `GetFilterUnit`, `SetUnitPosition`, locations, the map's bounds and the move hook for
  regions (W2-6); `undefend` and the abilities' natives (W2-7); the order events of `KillUnit`, `RemoveUnit`,
  `PauseUnit` and `SetUnitOwner`, the bone decay's length, `CreateCorpse` past 45 s, timed life, summons, sleep, unit
  state and filter events, unit counts, `CreateUnitByName` and `UnitId`, and the rest of the unit natives (W2-10);
  orders, the pathing and moving units off each other (wave 3); animation, time scale and the other natives that only
  show (wave 5).

### Cameras, and the rest of what main needs first (W5-1)

The measurements are six probes, `probes/cameras.lua`, `cameras-2.lua`, `cameras-3.lua`, `cameras-4.lua`,
`units-camera.lua` and `units-camera-2.lua`, in `docs/facts.md`, "Cameras", with their lines in `probes/expected`. The
natives are in `Cameras.cs` and `Presentation.cs`.

- **D500. One camera, the local player's,** as the task says (multiplayer: wave 6). Its fields are kept as set, angles
  in degrees, and read at once, the angles in radians; its target and eye read as the step before left them, as the
  game reads them as it last drew (the eye kept its place in the instant a rotation was set, and in a 0 s timer, and
  moved by 0.05 s). A change over a time goes in a straight line from where the value reads, an angle the shorter way
  round, however many turns it has made (an exact half turn goes clockwise: not measured); `Motion` keeps what the
  step before saw when a value is set again in the same step, as a probe sets a field several times in one instant.
  The levels: Exact where a probe reads the native back to the digit (the margins, the bounds, the setups' fields,
  places and labels, the flags, the depth of field natives), Close where the reads hang on the game's frames or a
  model (the fields, target and eye, the pans, the applies, following a unit, the rotate mode, the noise, the cinematic
  camera, the blockers), and Recorded for what only the view shows (the quick position, the smoothing factor). The
  probes that make a footman are named `units-camera`, so `ProbeComparison.ReadsData`'s prefix rule (D232) runs them
  with the install's data, without which `CreateUnit` gives nil.
- **D501. The fields' limits** are the measured ones (distance 0.5 to 10000, far z from 100, field of view 5 to 120
  degrees after coming round, z offset ±4096, near z 16 to 9500, depth of field from 0, z absolute as set), and an
  angle comes round to a turn with the game's whole turns: 2π above 0 and -0 at 0 and below (`Turn`), which also
  gives the start's roll and local angles as -0. Near z's 9500 is a constant: the same at a distance of 3000 and a far
  z of 2000. A setup keeps its fields as given, the lengths below 0 as 0 and the angles not turned; the camera holds
  them when the setup is applied. `CameraSetFocalDistance` and `CameraSetDepthOfFieldScale` set the two depth of
  field fields at once (measured), held as those fields are (inferred).
- **D502. The target.** The bounds start at 0, not the map's `war3map.w3i` bounds, as measured; every shipped map sets
  them in `main`. Their box holds the target, at every rotation and distance measured, and for a diamond. One case
  reads otherwise in every run (all three of the cameras probe): after pans with a z made while the bounds were 0, then
  bounds of 200 each way and a position at (1000, 1000) at rotation 120, the game held the target 200 along the view
  past the corner, (100, 373.2051); cameras-2 and cameras-3 didn't repeat it without the earlier pans. A rule, then,
  but not found; here the box holds it (`probes/differences/cameras.txt`, `later.md`). A position lands in the next
  step: mid-game the game's lands within 0.05 s, but as the game starts 0.2 to 0.25 s later, which isn't modelled
  (`probes/differences/cameras-2.txt`). `PanCameraTo` and a setup's pan that isn't timed go at 3000 a second (the
  game's 2970 to 3060 measured at 1100 and 2000 away). A pan with a z moves the target's height to it in place of the
  z offset, until the target moves again, and from the next step after `StopCamera` or `ResetToGameCamera`; following
  a unit keeps it (all measured). A move that leaves the target where it is changes nothing, as `PanCameraToWithZ`
  inside bounds of 0 left the height. `CameraSetupApplyWithZ` and `CameraSetupApplyForceDurationWithZ` over 0 s leave
  the height (measured), and the latter over a time takes it (measured). The height is the ground's, with the bridges'
  decks (`Height`, as `GetLocationZ` reads it; inferred: measured only on flat ground), and the z offset. A camera
  blocker keeps the target out of its rect: a timed pan stopped where its path entered it, and a position set inside it
  landed past it along the move (from (0, 0) to (400, 0) in a blocker from 200 to 600, at 600); here each move is cut
  so, in the order the blockers were added, with the rect as it is then (the rest the model's). Turned off, the target
  goes in (measured).
- **D503. The eye is worked out in floats,** as the game does: the distance times the cosine of the angle of attack
  times that of the rotation, each a float of the double's, gives 5 of the 6 eyes the probes read to the last digit,
  where doubles gave none. The sixth is at an angle of attack of 300 degrees, whose cosine the game reads 0.5000004
  (`probes/differences/cameras.txt`). The cosines are the doubles' rounded to floats, so a platform's last bit of a
  double doesn't show but at a tie (inferred). The eye ignores roll, the local angles and the noise.
- **D504. Following and turning.** `SetCameraTargetController` and `SetCameraOrientController` make the target follow
  the unit at the offsets from the next step, also once it is removed, from where it was, until a position, a pan, a
  setup's pan or `ResetToGameCamera` (measured, but for the setup's pan); the game follows a move a frame later
  (`probes/differences/units-camera.txt`), and neither the orient controller nor `inheritOrientation` changed the
  rotation field. A controller given nil ends a follow (the model's). `SetCameraRotateMode` takes the target round the
  point by the sweep over the time, the view turning with it while the rotation field reads as it was, without the z
  offset. The mode holds the target (units-camera-2): a position set, during the sweep or after it, and `StopCamera`
  only stop the sweep where it is, and a unit controller is dropped, the sweep going on; a timed pan ends the mode (the
  target moved, at the z offset again), and so does `ResetToGameCamera` (cameras-4). Inferred: `PanCameraTo` and the
  pans with a z end it as the timed pan does; a setup's pan ends it (the target moved, but the setup's z offset of 0
  can't tell the mode's height from the end of the mode); a new rotate mode in one starts from where the target is;
  and a controller given in the mode is dropped rather than kept for its end (the target hadn't followed 1 s after the
  sweep).
- **D505. The view's own natives.** The noise natives are Close, a model without the shake: at a magnitude of 50 the
  game's target read 0.1 to 0.3 off in one run and up to 2.8 in the other two, its eye 0.3 to 0.6 and up to
  5.6 (cameras-4), random as the game's, which a seeded copy couldn't match. `SetCinematicCamera` is Close too: while its
  camera plays, the game's target and eye read the model's camera (CameraZoomOut: the target 110 to 120 up, the eye
  200 to 250 from it, moving), here the game camera's, as the models' cameras aren't read
  (`probes/differences/units-camera-2.txt`, `later.md`); the fields read as they were, and `ResetToGameCamera` ends it
  in both. The smoothing factor stays Recorded: at 1 a pan read the same as without it. So does the quick position,
  which changed nothing read (cameras-4). `BlzCameraSetupSetCameraType` is Recorded: the game's getter reads a number
  of its own each run, whatever was set, and here `BlzCameraSetupGetCameraType` reads 0 (Close). The three location
  natives (`GetCameraTargetPositionLoc`, `GetCameraEyePositionLoc`, `CameraSetupGetDestPositionLoc`) are W2-6's, with
  the locations it takes over from W2-4 (`docs/tasks/wave-2.md`).
- **D506. What main sets for the view** (`Presentation.cs`): `SetDayNightModels`, the terrain fog natives
  (`SetTerrainFog`, `SetTerrainFogEx`, `SetTerrainFogExV`, `ResetTerrainFog`, `SetUnitFog`, the `BlzSetTerrainFog*`
  ones, `EnableWorldFogBoundary`), the water natives (`SetWaterBaseColor`, `SetWaterDeforms`, `SetHDWaterParams`,
  `SetHDWaterParamsEx`, the `BlzSetHDWater*` ones), `SetSkyModel`, `BlzShowSkyBox` and `BlzShowTerrain` are Recorded:
  no native reads them back. Each family is done whole, as the maps reach one of each. The fog flags are kept for
  `IsFogEnabled` and `IsFogMaskEnabled` (Exact: true at the start, then what was set); `FogEnable` and
  `FogMaskEnable` are Close, as what they do to what a player sees is wave 6's. The sound and music natives on the
  way (`NewSoundEnvironment`, the ambient sounds, `SetMapMusic`, `InitSounds`' `CreateSound` and `SetSound*`, and the
  dialogue keys of a sound) are W5-2's, which runs beside this task, and `SetResourceAmount`, whose gold mine's start
  amount comes from the data, is the units' (W2-10 or wave 3's harvesting).
- **D507. Where the shipped maps stop after W5-1** (`ShippedMapTests`, with W2-2, W2-4 and W2-9 merged first): 411 at
  `NewSoundEnvironment` (W5-2), the line after `SetDayNightModels` and the terrain fog and water lines, ForgottenHollow
  among them now that W2-4's `Location` is in; and 55 at `AddWeatherEffect` (W5-3), the map-wide weather their `main`
  makes before its sounds (Classic's Night Elf X05 and X06, Reforged's prologue and others). None logs a script error.
  With every sound, music and dialogue native stubbed out in a file never committed (so not reproducible from the
  repository until W5-2's pull request merges, when the test is to be run again for real), the maps went on to 206 at
  `SetResourceAmount` (gold mines; units, W2-10 or wave 3), 138 at `AddWeatherEffect` (W5-3), 46 at
  `BlzCreateItemWithSkin` (W2-3), 30 at `SetPlayerAbilityAvailable` (W2-7), 26 at `SetAllItemTypeSlots`
  (`InitBlizzard`'s `InitNeutralBuildings`, W2-3), 10 at `SetHeroLevel` (W2-3), 5 at `GetRectCenterX` (W2-6), 2 at
  `IssueImmediateOrder` (wave 3), 2 at `UnitAddItemToSlotById` (W2-3) and 1 at `CreateBlightedGoldmine` (Reforged's
  Undead07, W2-11), none at a native of this task's area. So no map reaches the World Editor's camera setups
  (`CreateCameras`) yet; the probes and `CameraNativesTests` test those. This replaces D302's `GetCameraMargin`. After
  W5-1: 245 Exact, 363 Close, 63 Recorded, 1,164 Missing (from W2-4's 221, 329, 25 and 1,260).
### The rest of the units (W2-10)

The probes `units-more`, `units-more-2`, `units-types`, `units-bones` and `units-gold` (`docs/facts.md`, "Units", "The
rest of the units") measured what the facts left open; the choices below follow them where they measured and say so
where not.

- **D310. The order events the unit natives give without orders** (wave 3's): undefend (852056) once for each Defend
  the unit has (an ability made from `Adef`, a map's copy too), on its owner's and its own order events: at a death,
  after it reads dead with life 0 and gives its food back and before the death events; as a removed unit goes after its
  step, twice if it lived (it then reads dead, life 0, unpaused) and once if dead; as an exploded unit or a body goes;
  inside `CreateCorpse`; after a pause's stunned order. `PauseUnit(true)` on a living unpaused unit fires the stunned
  order (851973) as a point order (`EVENT_*_ISSUED_POINT_ORDER`, which `GetOrderedUnit` and `GetIssuedOrderId` now read);
  `PauseUnit(false)` gives back, 0.01 s later (`Frame`, measured twice), undefend for each Defend, or a stop if the unit
  changed hands while paused; paused again before that, the given-back order fires at once and no stunned order. A
  paused unit given away gets no stop or undefend; a death unpauses. A dead unit given away gets nothing: the undefend
  W2-2 read as its give-away's was its death's (the probe killed it after the player's registration), so D226's
  sentence on it is replaced and `docs/facts.md` corrected. Which abilities a unit has is its type's `uabi` (shortcut,
  `Abilities` in `UnitOrders.cs`) until W2-7 gives units their own. `KillUnit`, `RemoveUnit`, `SetUnitOwner`, `CreateCorpse`
  and `GetOrderedUnit`/`GetIssuedOrderId` are Exact; `PauseUnit` stays Close: the game also gives back the order the
  unit had (game-facts: the move 0.016 s later), which needs orders.
- **D311. Bodies** (probe units-bones): a body goes `DecayTime` plus `BoneDecayTime` (2 + 88) after its decay, as D228
  inferred (footman 93.04, rifleman 91.7), and a `CreateCorpse` corpse that long after it is made, with no decay event
  (a hero's corpse stays, inferred from a dead hero); D228's "a corpse never decays" was the 45 s probe's horizon.
  `UnitSuspendDecay` right after the death changed nothing, also let go after the body's time, so it does nothing here,
  replacing D228's suspension (Close: suspending in the decay or the bones isn't measured). A body goes, and a timed
  life ends, a step after its time, as the game ran a timer due at the same time first (a corpse whose time was 90.00
  still read in a 90.00 timer; a 1.40 timer ran before the 1.40 deaths).
- **D312. Timed life** (probes units-more, units-more-2): `UnitApplyTimedLife` on a living unit adds summoned to its
  classification (as `UnitAddType`, also with a duration of 0 or less, which never kills) and kills it the duration
  later, the sooner end holding when applied again; a dead unit gets nothing; any buff id. `UnitPauseTimedLife` stops
  and resumes its clock, `BlzUnitCancelTimedLife` kills at once; both Exact. `UnitApplyTimedLife` is Close: with the
  Water Elemental's buff `BHwe` the game gave the body no decay event and took it at its decay time, which isn't
  modelled (`probes/differences/units-more.txt`). Summon events and `GetSummonedUnit` come with the abilities that
  summon (wave 4).
- **D313. Sleep is settled when it changes, and when read after the day turned** (probes units-more, units-more-2): a
  unit keeps whether it can sleep (its type's `usle`, `UnitAddSleep`) and whether it sleeps; made, it sleeps when it is
  neutral hostile's, can sleep and it is night, dead too; `UnitWakeUp` wakes it until more than 2 s later; `UnitAddSleep`
  wakes it or puts it to sleep at once. Read in another half of the day than it was settled in, it is settled again:
  at night it sleeps if it could fall asleep; by day the living unpaused ones wake. Before a change to what that reads
  (a death or removal, a pause or unpause, an owner change, the owner's `PLAYER_STATE_NO_CREEP_SLEEP`) it is settled
  first, so dawn and dusk find the unit as it was then. So nothing runs for creeps between reads, and
  `IsUnitType(UNIT_TYPE_SLEEPING)` reads the same. `PLAYER_STATE_NO_CREEP_SLEEP` wakes no one (measured)
  and keeps them from falling asleep (inferred from its name and `EnableCreepSleepBJ`). `UnitIsSleeping` is Close: the
  game sleeps and wakes them 0.2 to 0.25 s after dusk and dawn, here at the hour; also, a read after a whole day reads
  as one after none, and an owner change at night doesn't put a creep to sleep (neither measured). `UnitCanSleepPerm` is
  false and `UnitAddSleepPerm` does nothing, as measured.
- **D314. Rescue and what waits for later waves:** `SetUnitRescuable` and `SetUnitRescueRange` keep their values on the
  unit and `GetRescuer` is nil, all Close: the game rescued a unit 0.5 s after a rescuer stood 100 away, firing the
  rescue events on its owner's and its own registrations without giving it away, and that needs the units in range
  (W2-6, with `GroupEnumUnitsInRange`'s collision rule; `docs/tasks/wave-2.md`). `SetUnitCreepGuard` does nothing until
  the creep camps' AI (wave 7) keeps and reads it, Close. `UnitSetUsesAltIcon` and `BlzShowUnitTeamGlow` only show
  (Recorded).
  `SetPlayerUnitsOwner` gave nothing away and fired nothing, and what the game keeps it for isn't known: Recorded.
  `IsUnitIllusion` is false, Close until abilities and items make illusions (wave 4; game-facts: they read true), as
  `IsUnitType`'s wave-4 kinds are (D230); `BlzIsUnitSelectable` is alive, shown and not removed, and a unit with Locust
  is for W2-7 to probe (game-facts measured only `SelectUnit` on one): done in D288.
- **D315. Unit state events** fire inside the set or death that brings the life or mana from failing the comparison to
  passing it, kept in the unit's event list with their comparison (`UnitLimit`), so `DestroyTrigger` lets go of them as
  of any unit event. `EQUAL`, `NOT_EQUAL` and the most life and mana never fire, nor a registration that passes when
  made. Mana is looked at again a frame later: a registration whose comparison passes and didn't at the last look
  fires once more (the game's at 0.0049 s, here the next step), which fits every mana case measured. Close: the frame's
  time, and regeneration, which can bring a value across a limit, comes with combat (wave 3). `GetEventUnitState` is
  the event's state, and outside one the state with id 0, Exact. `TriggerRegisterFilterUnitEvent` registers as
  `TriggerRegisterUnitEvent` with its filter run first, as a trigger's condition is (in the firing, so `GetTriggerUnit`
  reads the unit; `GetFilterUnit` stays W2-6's), and not for a destroyed or disabled trigger; a death registered so
  never fires, as measured; Exact.
- **D316. Counts, food and names.** Each player keeps its living units' count by type, from creation to death or, for a
  removed unit, until it is gone; an owner change moves it. The counts read it (structures by the type's `ubdg`, the
  upgrades by `uupt`, followed to the end), and `GetPlayerTechCount` of a unit type reads it too, so it is Exact. A
  living unit's food used and made go to its owner's `PLAYER_STATE_RESOURCE_FOOD_USED` and `FOOD_CAP` at the same
  moments (a corpse adds and takes back), firing the state events inside the call, and `SetUnitUseFood` moves only the
  food used. The counts and food are kept before any of those events fires, and a death's before its life state
  event, so a handler that gives the unit away finds them whole (from the code review). Not measured, the model's (from
  the re-review): an old owner's food event that kills the unit or gives it away ends `SetUnitOwner` there, with no
  owner change event, and one that stops its food use leaves the new owner's food used unannounced; a corpse's
  count and food are taken back from the owner they were counted for, also when its food event gives it away. `UnitId` reads `unitui.slk`'s `name`, which the text files' `Name` would
  replace in the data's rows: `StandardData` keeps it as the unit field `name` (a test gives it so too), and
  `GameData.Objects` lists a kind's objects in the order of its first table (unitdata.slk for units; each object keeps
  its row), then the map's new ones by id, so a name two types share is the first's (paladin is `Hpal`; archmage
  `Hamg`, inferred). Not measured: a map's own unit type reads
  its base's name (the game may name it otherwise), and `UnitId2String` gives null in a JASS map, as `LoadStr` does.
  `IsUnitIdType` reads a type as `IsUnitType` reads a unit, never ground or dead; `UnitAddType` and `UnitRemoveType`
  change the classification field of the unit for its nine kinds and refuse the rest, as measured.
- **D317. A gold mine's gold** (probe units-gold, asked of W2-10 by the coordinator, as 173 melee maps call
  `SetResourceAmount`): `GetResourceAmount` is what was set, else the Max Gold (`Gld1`, level 1) of the unit's ability
  made from `Agld`, else 0; `SetResourceAmount` keeps any amount, 0 and below with no death, on a unit with an ability
  made from `Agld`, `Abgm` or `Aegm` (a haunted and an entangled mine keep one though they start at 0), and nothing on
  any other; `AddResourceAmount` adds in 32 bits that wrap. All Exact; the type's abilities stand for the unit's until
  W2-7 (D310). Harvesting is wave 3's.
- **D318. The probes** keep players 0 to 15 passive to each other where units of several players stand near each other,
  as footmen given to Player 1 fought and died in `units-more-2`'s first runs; they print times to hundredths, as the
  game's frames don't fall on the clock's steps (D45). `units-bones` ends 1 s after the footman's bones go, at about 94
  s, within the runner's 2 minutes from the launch.
- **D319. After W2-10,** with W2-4 merged first: 253 Exact, 333 Close, 28 Recorded, 1,221 Missing. Left: rescue
  itself (W2-6), the units' own abilities for undefend and gold, and a probe of Locust (W2-7), the order a pause gives
  back and regeneration across a state event's limit (wave 3), summons and illusions (wave 4), the creep camps and
  their sleep 0.2 to 0.25 s after dusk and dawn (wave 7).
### Sounds and music (W5-2)

The measurements are two probes, `probes/sounds.lua` and `sounds-2.lua`, in `docs/facts.md`, "Sounds", with their lines
in `probes/expected`, and the workspace's facts' "Presentation" and "Files" sound bullets.

- **D520. A sound file's length is the install's classic file's, read at run time.** `GetSoundFileDuration`, and a
  sound's length, look in the map's own files, then in the install's storage: the classic file under the language's
  folder (`_Locales/enUS.w3mod:`), then the base, then the Definitive Edition's (its language's, then its own), then the
  HD one's; in each, the path as given, then with `.wav`, `.flac`, `.ogg` and `.mp3` in place of its extension or of
  none, `/` and `\` alike, in any case. Measured: `HeroPaladinWhat1` asked for five ways read the classic ogg's 1288,
  not the DE flac's 1788, though the game shows DE graphics (D203); a file stored only for DE and HD read the DE one's.
  The map's own file first, and a name with a letter outside ASCII not found there, are the workspace's facts'. DE
  before HD is inferred from one file. A length is read from the file's headers, never its sound, in whole milliseconds
  cut toward zero: a WAV's data over its bytes a second, a FLAC's STREAMINFO samples, an Ogg Vorbis's last granule
  position over its rate, an MP3's Layer III frames counted (`SoundFiles`). Nothing is committed (A15): the install's
  storage stays open in the process for the files a run reads (`StandardData.Storage`), one a folder, its tables about
  58 MB, a new build of the install closing the old one's; a run reads a path once. Exact on every ogg measured (no wav
  or flac is the classic file of a measured path; their headers are as simple); an mp3 is Close: its frames and one
  more, which the game reads for 27 of the 61 measured; of the rest it reads 21 shorter, by 2 to 32 frames against this
  count (1 to 31 fewer than the file holds; up to 836 ms, Orc1 and Undead2), and 1 a frame longer (LichKingTheme). The
  frames alone matched 12, a length from the first frames' average size no more than 12; the game's rule wasn't found.
- **D521. A sound is its file and its play state.** It takes the next handle id (probe: a timer made before and one
  after are 1 and 1 off). Its length is its file's from when it is made: `CreateSound`'s file, also
  `CreateSoundFilenameWithLabel`'s (not the label's), the label's for `CreateSoundFromLabel` and `CreateMIDISound`; a
  label the tables don't have still makes a sound, of length 0. `SetSoundDuration` and `SetSoundParamsFromLabel` change
  neither its length (3000 and -5 still read 1288) nor how long it plays (sounds-2). Nothing is played: the natives
  setting what a sound sounds like (volume, channel, distances, cone, position, velocity, its unit, stacking, the
  environment, the label's parameters) keep nothing, as no native reads them back, and are Recorded; what wave 8's
  record of Recorded calls keeps is its own. The facial animation sets give true for a sound and false for nil, and the
  dialogue keys read back as set, "" before and for nil: Exact. The natives whose effects the getters read (making a
  sound, `StartSound`, `StopSound`, `KillSoundWhenDone`, the play position, the pitch) are Close, the play model's
  (D522). World Editor's `InitSounds` calls only these.
- **D522. A sound reads playing from StartSound for its length over its pitch** (`GetSoundIsPlaying`, Close): a looping
  one until stopped, one of length 0 never; a second `StartSound` while it plays leaves its end (read so in every run,
  where the workspace's facts heard it cut the sound off, 09-29, by ear), one after it starts it again;
  `SetSoundPlayPosition` while it plays ends it that much sooner, and before `StartSound` does nothing; `StopSound`
  stops it at once and destroys nothing, but with a fade it reads playing to the end of its start's instant if stopped
  in it, else on to its end (fitted to two cases: stopped with a fade in `StartSound`'s instant it read playing then and
  not 0.6 s later, in every run of `sounds`; 0.5 s in, it read playing to within 0.05 s of a sound left alone, in all
  seven runs of `sounds-2`); `KillSoundWhenDone` stops a sound playing at once and keeps it (its length reads 4.5 s
  later), and destroys one not playing. Each is measured as the probes saw it. Not matched: the game's start waits a
  varying while (0.8 to 1.4 s in `sounds`, 3.6 to 4.0 s in `sounds-2`, from when the sounds stopped), so its sounds read
  playing that much longer; and a 3D sound at the origin with no distances set never read playing, where here it plays
  as a 2D one, as who hears isn't simulated. A pitch of 0 or below plays at its own speed (not measured).
  `GetSoundIsLoading` is false, Exact: every read was.
- **D523. A label is its first file.** `CreateSoundFromLabel` and `CreateMIDISound` take the label's `FileNames` from
  the install's sound tables in `UI\SoundInfo` (the ability, ambience, anim, cinematic, dialog, UI, unit ack and unit
  combat sounds, `AmbientMusic` and `Music`, the first naming a label kept), read with the standard data, and its first
  file. Close: the game takes one of a label's files at random (`HeroPaladinWhat` 1184 or 1288, the same six draws in
  all five runs, so its own generator, seeded the same, not the map's), and a unit's acknowledgement labels name their
  files without a folder, which the game finds and MirrorImaged doesn't: they read 0. A map's own sound tables aren't
  read.
- **D524. TriggerWaitForSound** waits as `TriggerSleepAction` (D71) for the time the sound has left less the offset, and
  0.15 s more: a sound not playing, or a negative offset, 0.15 s, so 0.2 s; nil 0, so 0.1 s. Fitted to the 75 waits
  read, 61 match: 49 of `sounds-2`'s 56 in seven runs (the first three times 1.4 s, here 1.5; offset 0.5 three times 0.9
  s, here 1.0; nil once 0.2 s, here 0.1) and 12 of `sounds`' 19 in seven (the first twice 1.4 s; a sound never started
  0.3 s in three runs, here 0.2; offset 1.0 once 0.6 s and once 0.4 s, here 0.5). The wait follows the sound's length,
  not when it stops reading playing (D522): a 1.288 s sound waited 1.5 s in both probes (1.4 s in two runs of `sounds`
  and three of `sounds-2`), and one with `SetSoundDuration(s, 3000)` too. A looping sound's wait never ends (not
  measured). Close.
- **D525. Music and the volume groups change nothing a native reads back,** so their natives keep nothing and are
  Recorded, `PlayMusic` and `PlayMusicEx` too: where the game starts in a list isn't steady (workspace facts,
  "Presentation"), but no native reads it, so there is no model to be Close (the code review's call, where the task
  named music lists Close). The sound probes mute every volume group and stop the music first, so a probe makes no sound
  on Sven's PC; muted sounds still read playing.
- **D526. The sound probes are compared with the install's data** and sound files, as the data probes are (D213):
  `ProbeComparison.ReadsData` takes `sounds` and `sounds-2`, so their comparison in MirrorImaged is `Needs=Game`. Only
  their lines that varied from run to run read `{t}` or `{w}`: in `sounds` the waits and, at 2.0 s, the sounds played
  from 1000 ms and at pitch 2; in `sounds-2` three waits (the first, offset 0.5's and nil's) and when each sound stopped
  reading playing, as lines that move (`~`), with a line of its own checking that the sound stopped with a fade stopped
  with the one left alone. The reads the same in every run stay as the game gave them, so where the play model differs
  (a sound read playing longer, D522) the difference is recorded.
- **D527. The shipped maps get through their sounds.** With the natives `main` calls before `InitSounds` stood in for in
  a local run only (11-08, not committed; W5-1 brings them), `ShippedMapTests` took 432 of the 466 maps through their
  `NewSoundEnvironment`, `SetAmbientDaySound` and `SetAmbientNightSound` (blizzard.j: `CreateMIDISound`, `StartSound`),
  `SetMapMusic` and `InitSounds`, to stops after it: `BlzCreateUnitWithSkin` 261, `CreateCameraSetup` 166,
  `BlzCreateItemWithSkin` 3, `BlzCreateDestructableWithSkinPitchRoll` 2; the other 34 stop before their sounds
  (`SetTerrainFogExV` 32, `IsFogEnabled` 1, `Location` 1). None stopped at a sound or music native or logged a script
  error. As committed, the maps still stop at `GetCameraMargin` (D302). blizzard.j's sound functions run on the natives
  (`InstalledSoundTests`: `InitBlizzardGlobals`' quest sounds made as it makes them, as the rest of it needs
  `IsFogEnabled`; `InitDNCSounds`; the ambient, volume group and music BJ functions; `PlaySound`). `GetAbilitySound` and
  `GetAbilitySoundById`, which read an ability's sound labels, stay Missing for W2-7 (`wave-2.md`). After W5-2, with
  W2-2 and W2-4 merged first: 229 Exact, 343 Close, 56 Recorded, 1,207 Missing.

- **D240. A hero keeps its attributes as floats and reads the whole part.** `unit.Str`, `Agi` and `Int` start as the
  type's and gain the level's `STRplus`, `AGIplus` and `INTplus` for each level (the unit's own field if it was set), so
  a Paladin's 22 and 2.7 read 24 at level 2 and 46 at 10 (probe heroes). A set (`SetHeroStr`, the field set) is a whole
  number and drops the fraction. A hair of 1e-4 is added before the floor: 17 + 9 x 1.8 - 9 x 1.8 in floats is a
  hair under 17, and stripping levels must give the start back.
- **D241. An attribute change moves the most life or mana, and the life or mana now, by the same.** Each whole point
  adds `StrHitPointBonus` (25) or `IntManaBonus` (15); the life is kept to its most, the mana from 0 to it (probe
  heroes: 325 of 650 at level 4 reads 525 of 850; strength 12 left 75 of 400, intelligence 7 mana 0 of 105; strength -5
  reads life -25 of -25 and doesn't kill). Armour reads the whole agility: `AgiDefenseBase` + agility x
  `AgiDefenseBonus`. The permanent flag of `SetHeroStr/Agi/Int` changes nothing and the "with bonus" reads equal the
  plain ones: bonuses come with the abilities (W2-7). Close.
- **D242. The experience table is the constants'.** Levels 2 up come from `NeedHeroXP`'s list as totals, then each
  level is the one before's times `NeedHeroXPFormulaA` plus its number times `B` plus `C`, kept as a double and cut
  when read, to `MaxHeroLevel` (defaults 200 / 1, 100, 0 / 10: game-facts "Heroes"). The level is kept apart from the
  experience: `SetHeroXP` and `AddHeroXP` only raise the level, `AddHeroXP` can lower the experience (not under 0) and
  nothing passes the last level's. A raise fires one level event inside the call, the player's first, reading the final
  level, experience and skill points. A suspended hero ignores `SetHeroXP`, `AddHeroXP` and `SetHeroLevel`.
- **D243. `UNIT_IF_LEVEL` set is a level change without an event.** From a higher level it goes to the value (level 5,
  set 3, reads 3), from a lower one to the value plus one (level 1, set 3, reads 4; the old rule of adding one to every
  set, D227, only held from level 1), with that level's experience. The permanent attribute fields set the attribute to
  1 whatever the value (7 and 0 both: probe heroes). Inferred between: a set equal to the level.
- **D244. Skills: a point, the type's list and the required level.** `SelectHeroSkill` learns the next level of an
  ability in the type's `uhab` list when the hero has a point and its level is the ability's `arlv` plus 2 for each
  level after the first (`alsk`, the data's level skip, reads 0 for Holy Light though the game needs hero level 3 for
  its second, so 2 is used where it is 0). The skill event fires before the point is spent (the points read the same
  inside it). The learned level is kept on the hero, and the ability is on it too (D348). `UnitModifySkillPoints` keeps from 0 up; `BlzResetUnitTalents` does
  nothing (no talents in the Paladin).
- **D245. Revival.** `ReviveHero` on a dead hero fires the start event with the hero dead where it died, then sets it
  alive at the place with its most life and its type's `umpi` mana (100, not the full 495), counts its food again, and
  fires the finish event, all inside the call. False for a living hero and for a unit that isn't one. The revivable
  event the game fires long after (with a unit that isn't the hero) is never fired here; the probe's last line is a
  recorded difference.
- **D246. Units that aren't heroes read 0 and refuse every hero native, and nothing closes the game** (probes
  heroes-nonhero-level, -str, -setstr and heroes-nonhero): `GetHeroStr` and the rest read 0, `SetHeroStr` and
  `SetHeroLevel` change nothing, `ReviveHero` and `UnitStripHeroLevel` are false. Only `IsSuspendedXP` reads true on a
  footman before any suspend, as the game does. D227's guess that a hero attribute set there closes the game was wrong
  for the natives; the field natives still close it (probe units-attribute).
- **D247. Proper names are drawn with a generator of their own** (seeded, not the map's: D523) from the type's list,
  numbered with a roman numeral by how many times that name was drawn. The game's numbering isn't that count: its 15th
  and 16th Paladin read "II" after about 35 had been made. Close; recorded in `probes/differences/heroes.txt`.
- **D248. An item is a widget of its own that reads nothing the moment it is removed** (probe items: type id 0, place 0,
  life 0, no charges, no player in the call), not after the step as a unit. It reads its type's data (`icla`, `ilev`,
  `iuse`, `ihtp`, the flag codes) unless set. The item field ids are the data's four-letter codes (`ITEM_BF_*`
  1769304929 is `iusa`...), so a field native reads the data with that code, `ipaw` (pawnable) being "can be sold to
  merchants" and `ipow` (powerup) "use automatically". The class words give 0 Permanent, 1 Charged, 2 PowerUp, 3
  Artifact, 4 Purchasable, 5 Campaign and 8 for the rest (the data's Miscellaneous read 8, not 6). The Blz setters of
  booleans and reals answer false but keep the value; the integer ones answer true.
- **D249. A hero's six slots, with the events inside the calls.** A pickup fires with the item in its slot, the
  player's event before the unit's; a drop (also from a use that spends the last charge of a perishable item) fires
  before the item leaves its slot; an item taken out lands 24 south of its carrier, visible and not owned; a powerup is
  picked up and dropped at once and gone two steps on. An item doesn't stack. `UnitAddItemById` with no room leaves the
  item at the unit and answers nil. `UnitDropItemSlot` swaps. `UnitDropItemPoint` and `UnitDropItemTarget` answer true
  and do nothing (the walk is wave 3's). What carried items give (attribute bonuses), a potion's heal and an ankh's
  reincarnation are the abilities' (W2-7); the probe's lines for them are recorded differences.
- **D330. The item widget natives.** `GetWidgetLife`, `SetWidgetLife`, `GetWidgetX`, `GetWidgetY`,
  `TriggerRegisterDeathEvent` and `GetTriggerWidget` take items (D252's Missing stop is gone): an item's life is kept to
  its most, below 0.405 it dies once (life 0, the widget death event inside the call) and stays, an invulnerable one too.
- **D331. `EnumItemsInRect` is `EnumDestructablesInRect`'s rule** on the items on the ground (a hidden one counts, a
  carried one doesn't), the last made first in a square of 256 (probe items). Close: the destructables' 32-cell test
  is used and the game's was not probed apart from them.
- **D332. The random item picks and the pools use the map's random numbers.** `ChooseRandomItem` and its kin pick, in
  the data's order, from the items marked `pickRandom` of the level and class (the level -1 and class ANY (9) all); the
  one with includes takes the others when told. `ChooseRandomCreep` and `ChooseRandomNPBuilding` give 0 (the creep data
  and camps come later). A pool picks by weight (a weight of 0 or less never), `PlaceRandomItem` and `PlaceRandomUnit`
  make what they pick, nil for an empty pool. All Close: the game's draws aren't known (D46).
- **D333. The stock natives are Recorded.** No shop sells or restocks until orders and the clock of wave 3, and no native
  reads a stock back, so `AddItemToStock` and the others keep nothing, and `InitBlizzard`'s `SetAllItemTypeSlots` and
  `SetAllUnitTypeSlots` get through.
- **D334. A hero's inventory is six slots; any other unit has none.** `UnitInventorySize` is 6 for a hero and 0 for any
  other unit (probe items). Close: a unit with the inventory ability has the size its data gives (Backpack, game-facts).
- **D335. The hero-revivable event never fires** (D245): `GetRevivableUnit` reads nil.
- **D336. The proper-name numbering is a count** (D247).
- **D337. (Replaced by D347: the unit natives are done.)** The game cache's unit natives were left Missing: `StoreUnit`, `RestoreUnit`, `HaveStoredUnit`, `FlushStoredUnit`
  and `SyncStoredUnit` stay Missing: restoring needs the whole unit's state (the attributes, levels, items and abilities
  of D240 to D249) written to the cache and rebuilt, and the attack speed that survived the Paladin's store came from
  an ability of wave 3. Left in `docs/tasks/later.md`.
- **D338. Random picks are Close** (D332).
- **D339. The hero and item probes change the world only with `CreateUnit`, `CreateItem` and the natives under test,**
  each hero made on a spot of its own (the game moves a unit made on another), so the probes hold in the game and in
  MirrorImaged. They read the installed data (`ProbeComparison.ReadsData`: names starting `heroes` and `items`).
- **D340. The extended tooltip's tags stay as written** (`<AIh1,DataA1>`); the game resolves them from the ability's
  data (`ParseTags`, W2-8). Close.
- **D341. An item set far off stays where it is set;** the game keeps it inside the map (5000, -5000 read 2016, -2048).
  The map's bounds aren't kept for items. Close.
- **D342. (Replaced by D343 to D346 and D348.)** Item abilities were left to W2-7: the tome's +1 agility, the carried items' attribute bonuses and the
  resulting life, mana and armour, the potion's heal and the ankh. `probes/differences/items.txt` records each line.
- **D343. A carried item puts its abilities on its carrier and its bonuses count while it is in a slot** (probe
  items-carried, `ItemBonuses.cs`). Each of the item's `iabi` abilities is given to the unit first in its list (one ability
  more for each; `GetUnitAbilityLevel` reads 1) and goes when the item leaves the slot, after the drop event. The
  bonuses are read from the ability's own data fields where it keeps them (`Istr`, `Iagi`, `Iint`, `Idef`, `Ilif`, `Iman`,
  a field another ability keeps in that column reads 0), so `AIs3` is +3 strength and `AIx5` +5 of each: `GetHeroStr`,
  `Agi` and `Int` with `true`, and the `_WITH_BONUS` fields, add them; the base, `_PERMANENT` and the plain field don't.
  Armour counts the agility with the bonus and `Idef` (`rde1` +2). The pickup event reads the bonus already in, the
  drop event still has it. Close: with two items of one kind the game's ability count wasn't probed.
- **D344. A change of what the hero carries keeps the life and mana's share of the most** (probe items-carried: 300 of
  650 with +3 strength reads 335 of 725, with +150 life 369 of 800, mana 50 of 255 with +250 reads 99 of 505, and
  taking the item out gives 300 of 650 again; rounded to the nearest). A hero at full life stays full. A level's gain
  and a set of the attribute add to the life as before, and a tome of strength keeps the share too (400 of 650
  reads 415 of 675); a manual of health adds its 50 to both (450 of 700), a tome of experience gives its experience (100,
  none while the hero's is suspended). A set of an attribute under a bonus sets the base, whether permanent or not.
- **D345. Using a potion heals before the use event and is refused when it can't** (probe items-carried). `phea` gives
  `Ihpg` 250 life, `pman` `Impg` mana, inside the call and capped at the most; a use at full life (or mana) or by a dead
  hero answers false and keeps the charge, with no event. Close: the refusal was measured for the two potions and a
  dead hero's `phea`.
- **D346. A hero dying with an ankh uses it, and comes back** (probe items-ankh, `ItemBonuses.Reincarnate`). The last
  slot's item with a charge and the reincarnation ability is used as the hero dies: the use event with the hero dead,
  the drop event and the item gone inside the call when it was the last charge, and no death event. 7 s later (`Ircd`) the
  hero lives where it died with 500 life (`irc2`) and the mana it had, with no revive event. An ankh with no charge isn't
  used: the hero dies with its death event. Close: a hero that regenerates (wave 3) keeps its 500, so the game's later
  lines differ, and the game's revivable event for an ordinary dead hero never fires here (D335).
- **D347. The game cache's unit natives** (probe heroes-store, `UnitCache.cs`; replaces D337). `StoreUnit` keeps the unit
  in the cache under the mission and key and answers false, whatever the unit (the game does, a dead hero, a building and
  a unit removed in the same step too; one already gone isn't kept); `HaveStoredUnit` reads true then, `FlushStoredUnit`
  and `FlushStoredMission` take it out (other types of the same key stay: the types are apart), and `SyncStoredUnit`
  does nothing. `RestoreUnit` makes a new unit of the stored type for the player at the place, alive with its most life
  and mana; a hero comes back with its level, experience, skill points, learned skills (with the ability), base
  attributes and items in their slots with their charges (the pickup events fire for each, and one hero level event for a
  level above 1); nothing else is kept: not the life, name, user data, flags, scale, abilities added or the proper name
  (a new one is drawn). Nil where nothing is stored and for no player. Close: a restored facing reads 89.99999 for 90, a
  restored footman reads scale 1.0, and the game moves a building made on others (`probes/differences/heroes-store.txt`).
- **D348. A learned skill is an ability on the hero** (probe heroes-store: after `SelectHeroSkill` of `AHhb`, `GetUnitAbilityLevel`
  reads 1 and the hero has one ability more); changes D244's "the ability itself is W2-7's".
- **D349. Not done:** equipment and the extended inventory, items with other abilities (`AItc` damage, `AIva` life steal,
  `AIsx` attack speed and the rest give their ability to the carrier and nothing more), regeneration (the potion probe's
  `350.6748`), and the `AIrc` ankh's first-found choice between two (the last slot is used, as the probe read).
- **D280. A unit's abilities are its own list** (`unit.Abilities`, `Abilities.cs`), made when the unit is, in the order
  `BlzGetUnitAbilityByIndex` gives them (probe abilities-lists, 60 kinds of unit): what the game gives it for what it
  is (Aatk, Amov, a building's Aalr and the race's Afi*, the build, upgrade and train commands), around the type's
  `uabi` held last first. One added goes first. A unit's end takes its abilities' handles. Ids the data lacks are
  made as bare objects only for the game's own commands (`Data.Bare`); a type's listed ability must be in the data.
- **D281. An ability's handle is made when a native first gives it out** and kept while the ability is on the unit
  (probe abilities: the id reads 0 after the unit or the ability goes, a re-added ability is a new handle).
- **D282. Extended tooltips keep their `<id,column>` tags as written.** The game fills them (`<AHhb,DataA1>` reads
  200); W2-8's `ParseTags` does it, so `probes/differences/abilities-*.txt` records the difference (Close).
- **D283. An upgrade raising an ability's level** (effect `rlev`) is applied by the change of its bonus since the
  ability last counted it (`Research`), so a level set in between moves with it. An ability given to a new owner only
  gains from the new owner's research; what the old owner had researched stays (probe abilities-upgrades).
- **D284. Probes whose names start `abilities`, and `files`, read the install's standard data** (`ReadsData`).
- **D285. `Defends` and a mine's gold read the unit's own abilities** (D310's shortcut, the type's `uabi`, is gone): an
  added `Adef` gives undefend and a removed one none.
- **D286. A cooldown started without a cast is the clock's, not the game's frames'.** The game's reads carry the float
  error of its frames (`1.192093e-07` after a start of 4, `0.2500001`); MirrorImaged's steps don't (Close, recorded in
  `probes/differences/abilities-fields.txt` and `abilities-sets.txt`).
- **D287. `GetAbilitySoundById`** reads the ability's or buff's `Effectsound` (`Effectsoundlooped` for
  `SOUND_TYPE_EFFECT_LOOPED`), "" without. `GetAbilitySound` reads "" for every string measured (an id, a name, a
  buff's id): Close, since what it reads wasn't found (probe abilities-types).
- **D288. Locust stays.** A unit that has had `Aloc` (the ability's id or one whose `code` is `Aloc`, given by add or by
  its type) is left out of `GroupEnumUnitsInRange` and `GroupEnumUnitsInRect`, and `BlzIsUnitSelectable` reads false,
  from the add on and after `UnitRemoveAbility` too, as the game gave (probe units-locust, facts "Locust"); the unit
  keeps a flag the add sets and nothing clears. `GroupEnumUnitsOfPlayer`, `OfType` and the range tests still find it.
  Exact. The game's selection natives are D278's, Missing.
- **D270. Terrain the readers refuse** (`(10)DustwallowKeys.w3m`'s `war3map.w3e`, a cliff type 0 War3Net doesn't
  define): `MapArchive` reads the file itself and keeps the type as written, so the map has the ground the game loads,
  and a melee map no longer lacks terrain. A file that can't be read either is left out, as before (W2-6).
- **D271. A group keeps its units in the order made.** The game's order is the order made while its memory is fresh and
  otherwise its memory's, which isn't modelled; every enumeration, `FirstOfGroup` and `ForGroup` go in the order made,
  with a removed unit dropped on the next read. Close (W2-6).
- **D272. The enumerations' filters.** `GetFilterUnit` reads the unit being filtered inside the filter and is nil
  outside; a filter runs in a thread of its own (D277) and a player-unit event's filter never runs (D223).
- **D273. Every unit is held in the world as a flier is.** A unit made or moved goes from the world's west and south
  edges to 32 short of its east and north ones; the game also moves a ground unit off the map's edge, off other units
  and buildings and onto a building's grid by its pathing, which is wave 3. The comparison files name these as Close.
- **D274. Rects, regions and locations** keep their corners in order as probe units-bounds measured, hold them in the
  world as units are, and a removed rect, location or nil reads 0. A region is the rect cells it was given, with the
  game's 32-cell rule; `GetLocationZ` reads the terrain and keeps `GameNatives.Height`'s bridge decks as W2-4 left them.
- **D275. Enter and leave events** fire for every way a unit's place changes: made, `SetUnitX`/`SetUnitY`,
  `SetUnitPosition`, `SetUnitPositionLoc`, removal and death with decay. They fire inside the call that moved it.
- **D276. The camera's location natives** (`GetCameraTargetPositionLoc`, `GetCameraEyePositionLoc`,
  `CameraSetupGetDestPositionLoc`) make a new location from their X and Y natives' values each call, nil for a nil setup.
  Close, as the X and Y natives are.
- **D277. Nesting.** A callback started inside a native runs in a thread nested in the calling one. A Lua map goes
  98 to 99 deep where the game went 99 (the deepest callback here ends at its call; Close); a JASS map's threads nest
  from the main thread (`LuaState.NestsInMain`), which gives 120 deep and closes the game from about 190, where the
  game's JASS ran 900 and closed by 1000. Close; the game's closing is kept as `GameClosedException`.
- **D278. `GroupEnumUnitsSelected`** stays Missing: selection comes with the player's input (a later wave).
- **D260. Damage from code.** `UnitDamageTarget` fires DAMAGING with the amount as given, cuts it by the armour (damage type
  normal only; the attack type's `DamageBonus` row for the target's defence type, then `1 / (1 + 0.06 x armour)`, or `2 - 0.94^n` for
  a negative armour, n its whole part to 20), fires DAMAGED with the result and takes the life after its actions, a killing
  blow's death inside the call naming the source (`Kill` and `SetLife` carry the killer; `GetKillingUnit` reads it). The
  same `Hit` object goes through both events, so a type changed in DAMAGING reaches the armour and one set in DAMAGED
  does nothing. A nil source answers false and deals nothing (the game's answer is not measured: Close).
- **D261. Hits abilities change** (Hardened Skin, Mana Shield, evasion, Defend and the rest of the facts' list) wait for wave 4;
  here only invulnerability (reads 0 in DAMAGED) and spell immunity by `ACmi` (the damage and attack types the facts list
  get DAMAGING and no DAMAGED, no life lost) act. `BlzGetEventIsAttack` reads false until real attacks (wave 3).
- **D262. `UnitDamagePoint`** runs after its delay (at least one step) on each living unit whose origin is within the radius,
  in the units' grid order, through `UnitDamageTarget`; Close.
- **D290. Tags** (`ParseTags`, and the extended, learn and turned-on tooltips of abilities and an item's extended tooltip) fill each
  `<id,column>` as the facts of 10-29 and 11-08 read them: the id exact in case (an ability, a unit, an item or an upgrade),
  the column in any case with its trailing digits as the level, the value through `GameData.Own` so a column shared by
  ability ids (`Hbz1` on `AHhb`) reads the object's own and its map change, whole by default (cut toward zero), `,%` times 100,
  `,.` one decimal. Anything unfillable shows 0. Close: a unit reads only `HP`, `realHP`, `mindmg1/2` and `maxdmg1/2` (the facts:
  `spd`, `dmgplus1` and `cool1` read 0, with no rule found), and a standard buff's comma cut is not modelled.
- **D320. Pathing and blight** live in one byte a 32 cell, read from the map's `war3map.wpm` (none: every cell open, and a map
  with no ground reads nothing). `IsTerrainPathable` is true when the kind's bit is set (true means not pathable), kinds 1 to 7
  in `PATHING_TYPE_*`'s order and 0 (any) any bit; a point off the grid reads blocked; `SetTerrainPathable` sets or clears one
  cell for one kind. Close: a map without a pathing file reads open where the game works its own out from the ground.
- **D321. Blight** is kept on 64 cells (four pathing cells) whose centre lies within 0.8 of the radius (radius 300 reached 240
  along a line, taking off with 150 cleared to 112: both match), `SetBlightPoint` is a radius of 200 (the game reached about
  160: Close), a rect takes the cells whose centre is inside it, and the player changes nothing. Blight touches no other
  pathing bit. A Necropolis's blight comes with buildings (wave 3 and 4); `CreateBlightedGoldmine` makes an `ugol` and a point of
  blight under it.
- **D322. Painted tiles** are kept by corner: `SetTerrainType` paints the corners within area - 1 of the nearest one, a circle
  for shape 0 and a square for shape 1, a variation of -1 as 0; `GetTerrainType` and `GetTerrainVariance` read the nearest
  corner, 0 off the map. A tile the map's list lacks is read back as set, as in the game.
- **D323. Craters** (`TerrainDeformCrater`) move the 128 corners by -depth x cos(90 degrees x distance / radius) inside the
  radius, from the step after they are made, linearly over their duration (Close: the game's ramp starts slower), adding
  up, until `TerrainDeformStop` (gone from the step after the stop, so a read in the same instant still sees it; with a
  duration, faded over it). `GetLocationZ`, `BlzGetUnitZ` and a bridge's ground read them. Ripple, random and wave are
  Recorded: `GetLocationZ` ignores them.
- **D560. Boards and dialogs** keep what their getters read: a leaderboard's rows (a second row for a player who has one, the index of the
  first, sorted by label, value or player as asked) and flags, a multiboard's row and column counts (cells cut off with them, one
  handle for a cell until released, nil outside the board), title and its own displayed and minimised flags (two boards can both
  read displayed, as the game's did), a timer dialog's displayed flag, a quest's flags and its items' completion, a dialog's buttons.
  Colours, styles, texts of cells and the like are Recorded. A leaderboard with two rows for a player, set as the player's, closes
  the game (twice seen). A dialog's click, and so `GetClickedButton`, waits for input (wave 6); the quest defaults are not measured.
- **D540. Effects** are handles with their place, kept as the facts of 11-03 read it: an effect made at a point stands on the
  ground there (a crater's raised z included), `BlzSetSpecialEffectX`/`Y` keep its z, `BlzSetSpecialEffectHeight` reads as the z
  until a place is set, and one made on a widget reads x, y and z 0 and scale 1 wherever the widget goes. Colour, turn,
  animation and time are Recorded (nothing reads them back); `DestroyEffect` and `BlzRemoveEffect` leave the place readable.
- **D541. Lightning** is made for any non-empty code (the game draws the ones it knows: Close) and reads the colour back as
  `SetLightningColor` set it, also after a move; a destroyed bolt answers false to destroy, move and colour. Floating text,
  weather, images, ubersplats, minimap icons, the cinematic filter and scenes, unit animations, indicators and command
  button effects are Recorded handles that nothing reads back; `IsCineFilterDisplayed` and the hero glow flag read what was set.
- **D340. The bag and equipment slots** (3.0) are a unit's 30-place bag (a unit with `AIni`; `UnitExtendedInventorySize` reads 30 or 0)
  and nine equipment slots (head, chest, gloves, boots, ring, second ring, primary, offhand, trinket; a unit with `AEqu`). An
  item whose `iequ` names an equipment type goes to the bag of a unit that has one when given, and any item does when the six
  slots are full; a bagged item is owned and hidden, `UnitHasItem` false, `IsItemInBag` true. `UnitEquipItem` puts it in its
  type's slot with the pick-up and equip events (`GetEquippedItem` the item), a third ring going to the bag; the item's abilities
  work from the slot, not the bag. `UnitUnequipItem` fires the drop, the pick-up into the bag and the unequip event.
  Close: `UnitCanEquipItemOfEquipmentType` reads false as in the game for gloves, the item tags read 0, and an item's ability
  objects (`BlzGetItemAbility`) are nil.
- **D263. Weapon fields** take the weapon index (0 or 1, any other the first) as the digit of the data code: `ua1b` for weapon
  0, `ua2b` for weapon 1, `umh1` to `umh2`; a set is kept per unit and weapon. The attack type reads JASS's numbers (spells 0,
  normal 1, pierce 2, siege 3, magic 4, chaos 5, hero 6: a footman's normal reads 1), the targets allowed the flags of their words
  (the footman's 314, `air,enemies` 65540), the weapon sound `WEAPON_TYPE_*`'s number, and attacks enabled the bit of the weapon in
  `uaen`. Close: an index past 1 is read as 0, which is not measured.
- **D264. Base damage, dice and cooldown** are the weapon's `ua1b`, `ua1d`, `ua1s` and `ua1c`; a hero's base damage adds its primary
  attribute (22 strength reads 23 with +1), and setting it takes that off first. The cooldown is the base one, not the attack
  speed bonus's. `BlzUnitHeal` answers the amount and changes the life (a negative amount takes it), nothing for a nil source.
- **D265. Doodads** the map places are those of the doodad kind in `war3map.doo`, in the file's order; an index outside gives 0.
  Buffs and cooldowns come with wave 4: `UnitRemoveBuffs*`, `UnitHasBuffsEx` and `UnitResetCooldown` have nothing to act on.
- **D600. Settings and menus.** The natives of the game's options and menus keep what their getters read (selection, HUD scale, the
  ally and creep filters, difficulty, densities, game types, tax rates, campaign buttons); the screens, saves, campaign menus,
  the cinematic panels and the automation harness are Recorded. `ChangeLevel`, `RestartGame`, `ReloadGame` and `LoadGame` end
  this run as `EndGame` does (the next level is another map's run). `OrderId` knows only the orders the facts name (wave 3 brings
  the rest); `AbilityId` is the four-character code; `GetAbilityEffect*` reads the ability's art column by effect type;
  `CreateUnitAtLocByName` finds a unit by its data name; `GetTerrainCliffLevel` reads the nearest corner. Close: defaults not
  measured (HUD scale 1, selection on, difficulty Normal, a computer's difficulty Normal, no saves).
- **D350. Orders from code.** `Issue*Order` (immediate, point, target, build, neutral, instant, by name or id, by location) answer false for an
  order the unit cannot take (an unknown name, a removed unit, a dead one for anything but attack) and otherwise make it the
  unit's current order at once ('stop' makes it 0) and fire its order event inside the call, the unit's player-unit registrations
  first as for any unit event, with `GetIssuedOrderId`, `GetOrderPointX/Y` (a target's place for a target order) and
  `GetOrderTarget*` in the event; a nested order fires nested. A build order carries the building's type id and needs it in the
  unit's build list. `BlzQueue*` on an idle unit gives the order at once, on a busy one queues it; `BlzGetUnitOrderCount` counts the
  current order and the queue, `BlzUnitClearOrders` and `BlzUnitForceStopOrder` clear them. Group orders answer true when every
  unit took the order. A `setrally` order keeps the rally point and target; waygates keep their destination and flag. What an order
  does (walking, fighting, casting, building) comes with waves 3 and 4: an order stays current until another replaces it. Close:
  `OrderId` knows only the orders the facts name until the order table is probed.
- **D561. Frames.** A frame is a handle with its name, type, parent and children, text, alpha, visibility (a frame inside a hidden one
  is not visible), enable flag, value range and size; a frame without a size takes the size of the one it fills (`BlzFrameSetAllPoints`).
  `BlzCreateFrame` finds a template the map's loaded TOC lists (its FDF's `Frame "TYPE" "Name"` lines) or one of a few game templates, else nil;
  `BlzCreateFrameByType` always makes a frame; the origin frames are made on the first ask, one for each type and index. Destroying a
  frame destroys its children. `BlzFrameClick` fires the frame's `FRAMEEVENT_CONTROL_CLICK` registrations with it as the trigger frame;
  the mouse and key events are wave 6's. Textures, models, fonts, colours and anchors are Recorded. Close: the game's own TOC files
  and its frame tree are not read, and layout is the size given. The missing-native tests now use `BlzGetUnitAnimationDurationByIndex`, the last native to be
  written (wave 7).
- **D601. Vision.** A place is visible to a player within the sight (day or night field) of a living, shown unit the player owns, shares's
  or sees through by a shared-vision alliance or `UnitShareVision`; else a running fog modifier's state at the place, the later
  started winning; else fogged where the player has seen (kept in 64-unit cells, marked by every read and every move of
  a unit that has gone a cell) and masked elsewhere. `FogEnable(false)` shows fogged ground as visible, `FogMaskEnable(false)` shows
  masked as fogged, both off everything visible (facts, 10-09 and 11-06). A stopped modifier leaves its area explored. Selection is
  a set for each player; `SelectUnit` acts on the local player. Close: invisibility, detection, transports and the in-range event
  come with abilities and movement (waves 3 and 4), and the game's later recompute of overlapping modifiers (the oldest wins) isn't modelled.
- **D602. Events whose sources come later.** The spell, training, building, shop, summon and attack responses read nil or 0 outside
  their events, as every response does outside one; the registrations of the command, upgrade-command, chat, trackable and key events
  answer an event that never fires; the key and mouse state reads false and 0. Sync data (`BlzSendSyncData`) is real: it reaches
  the registrations for its prefix for the local player a step later, refused over 255 characters. Using an item on a place or a
  target needs its ability (wave 4) and is refused.
- **D352. Movement and fighting.** A 1/32 s step (woken by an order or a unit made, and running while a living unit could walk or fight)
  moves each unit with a walking order toward its place or target at its speed, in a straight line: only unwalkable cells (for
  walkers and riders) and the edge of the world stop it; an order ends on arrival and the next queued one starts, firing its event.
  An attack order, a smart order on an enemy, an attack-move and an idle or holding unit fight: the unit walks into the weapon's
  range (plus the target's collision size), swings when its cooldown is over (the base cooldown), and the damage lands at the damage
  point through the damage events as a real attack (`BlzGetEventIsAttack` true), base damage and a die roll from the game's generator,
  damage type normal and the weapon's attack and weapon types. A unit with no order, and a holding one, takes the nearest enemy
  it may attack within its acquisition range (hold: its weapon range) every fourth step, its current order staying 0 as the facts
  say; neutral passive players and allies are never attacked. Close: no pathfinding, collision or turning, no missile flight, no
  damage loss or splash, and the order of units acting in a step is the game's units order, not measured.
- **D351. The order table** is the 373 names the game gives (probe `orders-table`), kept as `OrderNames` and, by name in any case, `OrderIds`;
  `OrderId` and `OrderId2String` read it (Exact: the probe's expected lines are the table itself), and the Issue natives find a named order in
  it. A name the game doesn't give is 0 and an unnamed id ""; the stunned order (851973) is the pause's own and has no name.
- **D353. Buildings, training, research and harvesting.** A worker given a build order walks to within 100 of the site, pays the building's gold and
  lumber (the order ends with nothing made when the player can't), and makes the building at 10% life with its construct start event; the
  building finishes after its build time (life full, the food it makes counted then, the finish event) and the worker, busy until then, goes
  idle. Cancelling it (order 851976) refunds 75% and fires the cancel event. A finished building given a unit or an upgrade it lists (`utra`,
  `ures`) as an immediate order queues it (seven at most), takes the cost at once and holds the unit's food; the first starts at once with
  its start event and ends with the finish event and the unit made below the building (sent to the rally point if one is set); cancel
  takes the newest back with all of its cost. An upgrade's level, cost and time follow its base and per-level fields. `GetTrainedUnit`,
  `GetTrainedUnitType`, `GetResearched`, `GetResearchingUnit` and the construct responses read these events. A worker ordered to
  harvest a mine or a tree walks to it, works a second, takes 10 gold or 10 lumber (a tree loses 10 life) and carries it to the nearest
  finished town hall of its owner, then goes back until the mine or tree is spent. Close: refunds, rally behaviour, loads and trip times
  are the common ones, not measured; the building's life while it grows isn't modelled; no upkeep, transports, repairs, upgrades of a building
  into another, or merchants yet.
- **D354. Attack events.** Each swing fires `EVENT_UNIT_ATTACKED` (player 18) for the unit attacked, `GetAttacker` naming the attacker; a
  unit that takes a target of its own accord fires `EVENT_UNIT_ACQUIRED_TARGET`, `GetEventTargetUnit` the target, its order staying 0
  (facts, 11-14); a unit with no order and nothing to fight that is hit takes its attacker as its target, however far (facts, 11-14).
  The unit-level construct start event common.j lacks (`EVENT_UNIT_CONSTRUCT_START`) is kept under an id nothing registers: 63 is
  `EVENT_UNIT_RESCUED`. Close: the target-in-range event and the call for help of allies are not fired.
- **D355. The cast flow.** An order whose name is the `Order` of an ability the unit has (and not hidden or disabled) is a spell order: refused
  (false, no order) when the unit lacks the mana or the ability is cooling down, else the unit's current order. It walks into the
  ability's range (plus the target's collision), fires the channel and cast events, waits the cast time, spends the mana, starts the cooldown, fires
  the effect event and does the effect, then fires finish and end cast and is idle. `GetSpellAbilityId`, `GetSpellAbility`,
  `GetSpellAbilityUnit` and the target responses read these events. Effects are the data-driven ones of Holy Light (heals `DataA`), Storm Bolt
  (a hit of 0 of unknown type, then `DataA` of damage type sonic: facts) and Thunder Clap (`DataA` to enemy ground units in the area); every
  other ability fires its events and does nothing yet (Close, wave 4 goes on with each base ability and its buffs).
- **D356. Buffs.** A buff is a type id, its giver, a sign (positive or negative), an end and what it changes: the unit's move speed (a factor, held
  to `MaxUnitSpeed` when it raises), its attack rate (the cooldown divided by 1 plus the bonuses), its armour, or the unit doing nothing (a
  stun). Giving one a unit has refreshes it; it ends at its time (a hero's own duration for a hero), with the unit's death or when dispelled
  (`UnitRemoveBuffs*` take the ones the sign and magic flags pick; `UnitCountBuffsEx` and `UnitHasBuffsEx` count them), and
  `GetUnitAbilityLevel` of its id reads 1 while it is on. Storm Bolt stuns after its hit, Slow (`Aslo`: `DataA` the speed lost, `DataB` the attack
  rate lost) and Bloodlust (`Ablo`: `DataA` attack rate, `DataB` speed gained) give theirs. Close: no other stats, no buff objects of
  their own (`BlzGetUnitAbility` of a buff, tooltips) and no auras yet.
- **D357. The AI's economy natives** (counts, costs, `SetProduce`, `SetUpgrade`, `HarvestGold`, towns and mines) act for `AiFor`, the player an AI script runs
  for (the local player where none does), on the buildings, queues and workers of wave 3: a count includes units in training, a town is a finished
  town hall (the first made is town 0), a mine belongs to the town it stands within 1,000 of, `SetProduce` queues what is missing at buildings
  that list the unit, and `HarvestGold` sends idle workers to the mine nearest the hall. Wood, the captains and assaults, the expansion natives
  and running the scripts themselves (a Lua state of their own for each AI player, with `Sleep` as the wait) are still to do (wave 7).
- **D358. State hash, chat and `mirror check`.** `Simulation.StateHash()` hashes the time, each player's states and tech, and each unit's place, life,
  mana, owner, order, buffs and queue in the order of their ids (SHA-256, 16 hex digits): two runs of a map with one seed and the same input give
  the same hash at every step, a fight with dice included (the determinism suite's first case). `Simulation.Say(player, text)` fires the chat
  registrations of the player the text matches (a part of the message, or all of it when exact) with `GetEventPlayerChatString` and
  `GetEventPlayerChatStringMatched`. `mirror check <map>` lists the natives a map's script names with their trust levels and exits 1 when one is
  Missing; `mirror run --hash <n>` prints the hash every n seconds.
- **D359. Pathfinding.** A walker goes straight until a step is blocked (an unwalkable cell of the pathing file, the world's edge, or a building it is
  not working on and not standing in), then finds a path by A* over the 32 cells (eight neighbours, no cutting a corner, at most 6,000 nodes)
  to the place, or to the cell nearest it that can be reached, follows its waypoints and finds it again if one is blocked. Fliers ignore
  buildings and cells. Close: buildings block as circles of their collision size, trees and other units not at all, and the game's own
  search is not copied; a place that can't be reached ends the order at the nearest cell, as a stuck unit would stop.
- **D360. Kill rewards.** A kill by a unit whose owner is an enemy of the victim's gives the killer's owner the bounty of the victim's type (`ubba` plus
  `ubdi` rolls of `ubsi`) when the victim's owner gives bounty (the neutral players by default), and the experience of the constants'
  `GrantNormalXP` or, for a hero, `GrantHeroXP` list by the victim's level, shared equally by the killer's owner's living heroes within
  `HeroExpRange` of the victim. Close: the field codes of the bounty, the shares and the range follow the data and the common values.
- **D361. Running the AI scripts.** `StartMeleeAI` and `StartCampaignAI` read the script (the map's `Scripts\<name>`, else `War3.w3mod:Scripts/<name>` of the
  install; which of the install's three copies a game loads is still open, D26) with `common.ai` in front, make both into Lua with the JASS rules and
  run them in an environment of their own, so two players' scripts keep their globals apart, in a thread whose natives act for that player
  (`AiFor`): `Sleep` waits on a game timer and `StartThread` starts a function in a thread of its own. A function argument that gives an
  integer (`SetHeroLevels`) is accepted. Kill the AI commands that are command-card orders (`harvest`, `build`, `load`...) are not spell orders.
  Seen: the human melee AI, given a town hall, a worker, a mine and 500 gold, trains workers and mines gold for two minutes with no script
  error. Close: the buildings, the captains and attack waves, expansion and the heroes are the AI natives still to do.

- **D362 (The AI builds and gathers):** `SetProduce(qty, id, town)` makes qty more: a building is built by a worker at a site found in rings round
  the hall (buildable and walkable ground when the map has a pathing map, clear of buildings, ordered sites and mines), a unit is queued where
  it is trained. `HarvestGold` and `HarvestWood` ask for worker slots in order within a round (`ClearHarvestAI` empties them), and workers are
  assigned by id to the slots, so some go to trees. Seen: in five minutes the human melee AI builds an altar, barracks, farms and a blacksmith
  and trains footmen and a hero. Close: not measured against the game.

- **D363 (Exploring is per unit):** a unit that moves a fog cell explores only what it sees, not what every unit of the player sees.
  A campaign map of 300 units took 1 s of real time per game second; it now takes a tenth of that. Not measured against the game.

- **D364 (Path search is cheaper):** a search for a path lists the buildings once a tick and asks each cell once, and an idle unit that
  looks for a target skips what is out of range before it asks who is an enemy. A campaign map that took 8 s of real time per game second
  (Human01) takes a quarter of one. Not measured against the game.

- **D365 (A blocked unit searches less):** a unit blocked again on its way searches for a path at most once in half a second, and one whose
  search found no way to a place does not search for that place again for two seconds, so it ends the order. A campaign map (NightElf01)
  that ran at a third of real time after 150 s now runs seven times faster. Not measured against the game.

- **D366 (The AI wages war):** the wave the AI asks for (`InitAssault`, `AddAssault`) is made at the buildings that train it and forms the
  attack captain, a group the natives send (`CaptainAttack`, `AttackMoveXY`, `AttackMoveKill`) as an attack-move and bring home, and answer
  for (`CaptainIsFull`, `CaptainAtGoal`, `CaptainInCombat`, `CaptainReadiness`). `GetEnemyBase` is the nearest enemy building, a town hall first,
  found at once (`WaitGetEnemyBase` is false: the AI loops while it is true). Expansion is a gold mine no town hall stands within reach of
  (`GetNextExpansion` is -1 with none), with its creeps, a worker sent to build there, and the camps of creeps. Seen: the human melee AI, five
  minutes in, sends eight footmen, four riflemen and a hero at an enemy second town hall and takes its life down. Close: the AI never attacks
  a main base without siege, as its script has it.

- **D367 (A person at a player):** a test or a recording gives a player keys, the mouse, a selection, orders and leaving (`Simulation.Key`,
  `Mouse`, `Select`, `Order`, `Leave`, `Say`), each at the step it was given in, and each fires the events a map registered and sets what it
  reads back (`BlzGetTriggerPlayerKey`, `IsUnitSelected`, `GetPlayerSlotState`). Only a player's own units obey its orders and selection. A
  recording is the map, seed, seats and the inputs at their steps, and `Follow` or `mirror replay` runs it again to the same hash. Close: the
  game's own arrow-key and key-event ids are not mapped beyond the registrations of `BlzTriggerRegisterPlayerKeyEvent`.
- **D368 (The desync check):** `mirror desync` and `Desync.Check` run the map once for each person's slot, all seated, with that slot as the
  local player, and compare the state after each second; a difference is reported with its step, the part of the world (time, a player,
  a unit) and the first log line that differs. `mirror bench` times a run, and `mirror fuzz` gives every player's units random legal orders
  each second and reports script errors.

- **D369 (Tests in Lua):** `mirror test <file or folder>` runs `*.test.lua` files in a Lua state of their own, apart from the games they
  load. A file calls `test(name, function)`; inside, `mirror.load(map, {seed = n})` gives a simulation with the calls of the C# API (`advance`,
  `units`, `order`, `select`, `key`, `mouse`, `leave`, `say`, `log`, `hash`, `wait_until`, `recording`), and `assert_eq`, `assert_true`,
  `assert_close` and `assert_contains` check. Each result is printed and `--junit <file>` writes JUnit XML. A shared case list (a map script and
  steps) runs through both APIs and must end at the same state hash and log (A9). Not done: finding the Lua tests from `dotnet test`.

- **D371 (Buildings in a grid):** the buildings that block a walk are listed once a tick in a grid of squares of 256, and a search for a
  path reads the move type once, so a cell costs a look at the few buildings near it. A campaign map that took 1.3 s of real time per game
  second (HumanX05) takes a fifteenth of that. Not measured against the game.
- **D370 (Snapshots and stepping back):** a snapshot is the step, the state hash, the hash of each part (time, a player, a unit) and the
  recording to that step. `Snapshot.Differences` lists the parts that differ from a simulation at the same step, `Simulation.Restore` runs the
  snapshot again, and `Simulation.StepBack` runs the map again to an earlier step, which lands on the state the run had then (A11). The
  world is not copied: a run repeats exactly from its recording, so going back costs the run to that step. `mirror snapshot` saves one and
  `mirror compare` runs it again and lists what differs.

- **D373 (The viewer):** `mirror view <map or recording>` runs it, samples the units every half second (id, type, player, place, life, order, name)
  with the log and its times, and writes one page that opens in a browser with the trace inside it: the map from above with its pathing
  grid and regions, the units as circles in the colours of their players with a life bar, a timeline to scrub with play, step and speed, the
  log (a line seeks to its time) and a panel for the selected unit. No missiles are drawn: attacks land at their damage point here. Heights
  are not drawn. Seen in the in-app browser on a shipped campaign map; a headless browser opens it in the tests where one starts.
- **D372 (Determinism across systems):** a test runs a map of dice, trigonometry, a fight, timers and many units for 20 seconds and compares
  the state hash at 1, 5, 10, 15 and 20 seconds with the hashes made on Windows. CI runs it on Windows, Linux and macOS (A7).

- **D374 (Docs):** the public API of every project but the natives has XML docs, and the compiler refuses one without (`CS1591` as an error);
  the natives are documented by common.j and common.ai and by the trust level each carries (`mirror coverage`). `docs/guide` has a page for
  each part (getting started, Lua tests, C# tests, JASS and TypeScript, the command line, recordings and the desync check, the viewer, trust
  levels, CI) and tests check that its links go somewhere, the index lists every page and the command line page names every command. The
  `examples` folder has the gold map in Lua, JASS and TypeScript, built into maps and run by the suite. `.github/actions/mirror-test` runs a
  map's Lua tests on a self-hosted runner. A Lua test reads its maps from its own folder.

- **D375 (Replays):** `Replay.Read` reads a `.w3g` of header version 1 (Reforged): the zlib blocks (each flushed and not ended), the lobby (host,
  players, game start record with its slots) and the time slots with the actions of each player: selection, orders (no target, a place, a
  unit), chat and leaving; other actions are counted and skipped, a player's actions of a slot stopping at one it cannot measure. `ToRecording(map)`
  makes a recording of the replay's input at the steps of its times (1024 to the second), which `Simulation.Follow` or `mirror replay game.w3g --map
  map.w3x` runs. A unit is named by the id the replay gives it, taken as the id the unit has here (ids count up from 0x100000 as the game's do), so
  a replay of a game whose units were made in another order than here gives orders to units that are not there, and they are left out. Read
  from the format as documented and one replay of this machine (a probe, with no orders of players); the orders are checked on replays the
  tests make. Close.

- **D376 (Upgrades and revival):** a building given the id of a unit type it upgrades to (`uupt`) pays that type's price and, after its build time,
  becomes it: the life and mana keep their share, the abilities are the new type's and the food it makes changes; it trains nothing while it
  upgrades, and cancelling gives the price back. A building given the id of a hero it revives (`urev`) when that hero is dead pays a share of the
  hero's price (0.4 and 0.1 for each level above the first, ReviveBaseFactor and ReviveLevelFactor) and revives it at its door when the time
  (0.65 of the hero's build time) is out; a hero a player already has, alive or in training, can't be trained again. Close: the revive price
  and time are the constants' names as I know them, not measured.

- **D377 (Animation lengths, and none Missing):** `BlzGetUnitAnimationDuration` and `BlzGetUnitAnimationDurationByIndex` read the sequences of the
  unit's model (its `umdl` file with `.mdx`) in the installed game: each is a name and the time it starts and ends in milliseconds; the first
  whose name begins with the one asked for wins, and a model that is not in the install gives 0. The game may read the high-definition model
  where the classic one is read here. With this no native of `common.j` and `common.ai` is Missing (A1). To keep the Missing machinery
  tested, `SimulationOptions.TreatAsMissing` (and `MIRROR_MISSING` for the command line) runs named natives as Missing.

- **D378 (The desync suite):** the state hash and its parts now hold the random generator's state and the next ids to be given (`random numbers`,
  `handle ids`), so a draw from the shared generator or a handle made in one player's game only, which the facts name as desyncs, make the games
  differ as they do in the game. A suite of patterns (a change to a player or unit, `GetRandomInt` and `GetRandomReal`, `CreateGroup` with `ForGroup`,
  `Location`, `CreateTimer`, `BlzCreateFrame`, each in a local-player block) must be found by `mirror desync` with the part and the step, and what one
  screen shows (text, camera, music, a name read) must not (A8). Not shown: the timer of 0.01 s and the upgrade dice, which the game does by chance.

- **D379 (Melee from the command line, and its speed):** every command that takes a map takes `--users` and `--computers` (slots) or `--melee true` (the
  first user slot a person, every other user or computer slot a computer), and a map named `War3.w3mod:Maps/...` is read from the installed game.
  `MeleeCorpusTests` runs the install's melee maps for 30 game minutes that way (set `MIRROR_MELEE` to a part of a name for some) and checks no script
  error and that the computers trained units; and a 4-player map (`(4)Adrenaline`) goes through 30 game minutes in under 60 seconds (A10): 23 s on
  this machine, 78 times real time. What made it so: a worker taking its load home may walk up to its own town hall (it went round it, a search
  each time), the town halls are listed once a tick, a search for a path leans to the goal (the estimate counts twice, 3000 cells at most), and a
  unit whose way stopped short of its place waits 2 seconds before it searches again. The AI's ghouls cut the wood its wood slots ask for.

- **D380 (More spells and the auras):** the spells of the melee heroes that deal with numbers: Death Coil (hurts a foe, heals a friend), Chain Lightning and
  Healing Wave (jump to the nearest not yet reached within the area, for less each time), Frost Nova (damage to the target, less round it, and a slow),
  War Stomp (damage and a stun round the caster), Fan of Knives, Mana Burn (mana taken, and as much damage), Carrion Swarm and Shockwave (a line from
  the caster), Entangling Roots (the unit cannot walk and takes damage each second), Sleep, Divine Shield and Big Bad Voodoo (nothing hurts the unit,
  and no unit takes it as an enemy), Frost Armor; and the auras Devotion (armour), Endurance (speed and attack rate), Unholy (speed and life each
  second), Vampiric (a share of a melee attack's damage back) and Trueshot (more damage for ranged attacks), which each unit that has one gives the
  allies within its area once a second for a little longer than a second. The numbers are the ability's Data A to E by level, as its tooltip names them.
  Close: channels (Blizzard, Flame Strike), summons, transformations, and the passives of an attack's chance (bash, critical strike, evasion) are not here.

- **D381 (Summons):** Water Elemental, Force of Nature, Serpent Ward, Scout and Vengeance (DataA units) and Feral Spirit (DataB) call units of the type their
  UnitID field lists for the level, in a ring round the target place or the caster, for the caster's owner, each a summoned unit that ends with the
  ability's duration, and fire the summon event (`GetSummonedUnit`, `GetSummoningUnit`). Close: Animate Dead (it raises corpses), Carrion Beetles and
  the summons that need more than a type and a number are not here.

- **D382 (Shops):** an order a player gives a building that lists the item (`usei`) or the unit (`useu`) with `IssueNeutralImmediateOrderById` is a purchase: the
  player's unit nearest the shop within 900 pays the price and takes the item (or the shop makes the unit at its door, the food counted), and the sell events
  fire for the buyer's player (`GetSoldItem`, `GetSoldUnit`, `GetSellingUnit`, `GetBuyingUnit`). Without a unit of the player near, or the price, nothing
  happens. Close: the stock a shop keeps and its wait are not (it always has the thing), and selling an item to a shop (pawn) is not.

- **D383 (The AI's heroes learn skills):** `SetHeroLevels(function)` gives, for the AI player, the function that names the ability a hero learns: for a hero with a skill
  point it is asked (`GetHeroId` the hero's type, `GetHeroLevelAI` its level), and the hero learns what it names (`SelectHeroSkill`), when the hero is made, when it
  reaches a level, and for the heroes the player has when the function is given. A point that is not spent stops the asking.

- **D384 (Bash, Critical Strike, Evasion):** a unit with Critical Strike (AOcr) multiplies the damage of an attack by DataB when a draw of 1 to 100 is within DataA; one with
  Bash (AHbh) adds DataC and stuns for the ability's duration (a hero's own for a hero) on a melee attack with the chance DataA; one with Evasion (AEev) turns an
  attack aside when a draw is within DataA times 100. Each draw is the game's shared generator's. Close: the abilities of other names that do the same with their
  own data are not here.

- **D385 (Pawning):** `UnitDropItemTarget` with a shop that has the pawn ability (Apit) and an item that can be pawned, from a unit within 900, gives the unit's player
  the share of the item's price that PawnItemRate names (half), fires the pawn event (`GetSoldItem`, `GetSellingUnit` the shop, `GetBuyingUnit` the seller) and takes
  the item away. Any other target leaves the item as it was.

- **D386 (Transports):** a unit with a cargo hold (Cargo Hold, whose DataA is the room, one a unit) takes a friendly unit near it (within 700) on the load order, or a unit
  sent to it by its smart order once it is in reach; the loaded unit is hidden, moves with the transport, fires the loaded event (`GetLoadedUnit`, `GetTransportUnit`;
  `IsUnitLoaded`, `IsUnitInTransport`), and comes out round the place of the unload or unload-all order; a transport that dies kills what it holds. Close: units
  count one each (the game counts by size), a unit does not walk to a transport that loads it, and there is no unloaded event (common.j has none).

- **D387 (Repair):** a worker given the repair order on a hurt finished building of its owner or an ally walks to it and restores its life at the rate its build time gives
  (a full repair takes the build time times RepairTimeRatio, 1.5), paying that share of the price times RepairCostRatio (0.35) in whole points as it builds up, until the
  building is whole, the player cannot pay or the order changes. Close: the ratios are the constants' names as I know them (not measured), and mechanical units are not
  repaired.

- **D388 (The corpus of shipped maps, A4 and A5):** all 466 maps of the installed game's Maps and Campaign folders load and run 10 game minutes with no Missing native; two
  community maps of the Forsaken Kingdom scenarios fail in their own scripts and are named in the corpus test with the reason (ForgottenHollow indexes a table with nil
  in its handle library at the start; SilverpineSprint's car physics finds no terrain family at about 13 s). The one fault in the runtime that the corpus found: JASS has a
  unary plus (`+x`) that the transpiler wrote as it was and Lua refuses, in an AI script of a campaign; the operand is written alone now. `MIRROR_DUMP_AI` (a folder) writes
  the Lua of each AI script, and `MIRROR_TRACEBACK` puts the Lua traceback of a script error in the log.

- **D389 (The melee corpus, A4):** the install's classic melee maps ((2) to (12) players: 42 maps) each run 30 game minutes with a person in the first slot and computers in the rest, with
  no script error, and the computers' units grow (a 2-player map from 6 to about 18 units, a 4-player from 18 to about 75, an 8-player from 42 to about 160): 9 to 230 s a map
  with eight runs at once, a 4-player map alone 23 s (A10). The test counts a melee map as one with two or more user slots, and the computers' own units only.

- **D390 (The game's own constants, Blizzard and Animate Dead):** a hero's revival at an altar uses the install's constants: the share of its price is ReviveBaseFactor (.40) plus ReviveLevelFactor
  (.10) for each level above the first, at most ReviveMaxFactor (4.0), the gold at most HeroMaxReviveCostGold (700) and the lumber at most HeroMaxReviveCostLumber; the time is
  ReviveTimeFactor (0.65) of its build time, at most HeroMaxReviveTime (150). Pawning reaches PawnItemRange (300) from the shop's edge. These replace the guesses of D376 and D385.
  Not read from the install: repair's two ratios, which it has no constant for (the unit data holds its repair time, `urtm`); a probe in the game would give them, and the game would
  not log in for a probe from here. Blizzard does DataB damage in each of DataA waves, a second apart, to the ground units round the place; Animate Dead raises up to DataA
  corpses of units that are not heroes or buildings near the place as units of the caster's owner for the ability's time. The melee corpus test takes maps that have the melee flag.

- **D391 (Tile ids as the game gives them):** the ground of a map's file gave each tile's id as the four letters read as a number in the order the file has them, the reverse of FourCC; `GetTerrainType`
  gives FourCC now, as the game does and as the scripts that look tiles up by `FourCC("Ldrt")` need. It was why SilverpineSprint's car physics found no terrain; that map now runs its 10
  minutes without error. Left: ForgottenHollow's handle library, which indexes a table with nil at the start in a trigger of the map; the Lua here has no `debug` library to name the call
  (the game's has none), and no native was found that answers nil here and a handle in the game.

- **D392 (Lua tests from dotnet test):** `MirrorImaged.TestAdapter` is a test adapter of the test platform: a test project that references it and copies its `*.test.lua` files to its output folder
  has them found (each `test(name, ...)` a test) and run by `dotnet test` and the editors, with the failure's message; checked end to end on a project made for it. It finds the tests without
  running a map (the file is run only to the calls of `test`).

- **D393 (What is still open after the first release):** (1) Repair's two ratios (D387) are unmeasured: `probes/pending/repair.lua` is the probe, which the game runner could not run from the agent's session (the
  game stopped at its Battle.net login) and needs a run on Sven's side. (2) ForgottenHollow's handle library indexes a table with nil in a trigger at the start; no native was found to blame, as the
  sandbox's Lua has no `debug` (D391). (3) Replays are read as the format is documented and checked on replays the tests make: no replay of this machine holds a player's orders. (4) Not modelled:
  channelled spells other than Blizzard (Flame Strike, Starfall, Tranquility, Earthquake), the summons that need more than a type and a number (Carrion Beetles, Locust Swarm), transformations
  (Metamorphosis, Hex, Avatar), Mirror Image, Wind Walk, and the passives of other names. (5) The AI natives that stay as they were (guard posts, zeppelins, merging units) change nothing a melee game reads.

- **D394 (Hex, Metamorphosis, Avatar):** Hex turns the target into the ability's creature (its UnitID) and Metamorphosis the caster into its form, for the ability's time, then they are as they were with the mana they had;
  a change of type keeps the abilities a script made permanent (UnitMakeAbilityPermanent) and drops the ones it gave otherwise, as the game does. Avatar gives DataA armour (a buff) and DataB life for
  its time. Close: the form is the type only (a hero's hero-ness and the form's own attack changes are not carried), and the base armour `BlzGetUnitArmor` reads leaves the buff out as the probes measured.

- **D395 (Auras that pulse; Resurrection, Inferno, Brilliance Aura):** the auras of D380 did nothing in a game whose units had nothing to do (the unit engine rests then, and the pulse was one of its steps, which the
  first test did not check closely): the pulse is a timer of its own now, a second apart, kept going while any unit has an aura ability. Resurrection stands up to DataA corpses of friends
  near the caster (not heroes), Inferno hurts the units round the place for DataA and lands an infernal (the ability's UnitID) for its time, and Brilliance Aura gives its allies near DataA
  mana a second. Tests check the numbers (Endurance and Unholy together: 270 x 1.1 x 1.1 = 326.7 speed; Unholy 5 life a second).

- **D396 (Flame Strike, Starfall):** Flame Strike hurts the ground units round the place for DataA and then DataB a second for the ability's time; Starfall hurts the units round the caster for DataA a second.
  Both are from the data's names and not yet measured against the game, so their numbers are marked Close.

- **D397 (Siphon Mana, Wind Walk, Carrion Beetles):** Siphon Mana moves DataA mana a second from the target to the caster, Wind Walk gives DataB more speed for its time, and Carrion Beetles summon DataA units. The data fields are read from the names and are Close, not measured.

- **D398 (the game UI's named frames):** `BlzGetFrameByName` finds a list of the game's own UI frames (ConsoleUIBackdrop and the like) as it finds the origin frames, made on the first ask. ForgottenHollow parents its fade filter to ConsoleUIBackdrop and failed at the start without it.

  (The frames of the map's FDF also bring the named frames nested in them, so `BlzGetFrameByName("MinimapTexture")` finds the child of a MinimapTextureFrame, as the game does.)
  The named frames nested in an FDF frame are made along with it, so `BlzGetFrameByName("MinimapTexture")` finds the child of a MinimapTextureFrame, as the game does.
  A button frame made by type (GLUETEXTBUTTON and the like) has three backdrop children (normal, pushed, disabled), as the game's buttons do: ForgottenHollow's level-up panel reads them with `BlzFrameGetChild(button, 0)` to 2 and, without them, fails at its start. Close, from the maps that use it.

- **D399 (Immolation, Mirror Image, Thorns):** Immolation hurts the foes round the caster for DataA a second for the ability's time (ten seconds when it has none, the toggle off is not modelled), Mirror Image stands DataA timed copies of the caster beside it, and Thorns returns DataA of a melee hit's damage to the attacker. Close.

- **D400 (Earthquake):** Earthquake hurts the buildings round the place for DataA a second for the ability's time; units are left alone. Close.

- **D401 (Styled output on a terminal):** on a terminal `mirror` writes through Spectre.Console: colour, a spinner while a map loads or runs, `coverage` as a bar chart, `check` as a table and the usage as a list.
  Piped or redirected, every command writes the plain text it wrote before, to the byte, as scripts, CI and the tests read it: Spectre.Console wraps redirected output at 80 columns and ends lines as the
  platform does. The usage is the exception: its plain form is now a command a line with what it does under it.

- **D401 (repair price, measured):** the game repaired a Barracks from 300 to 1500 life in 74 seconds, 33.3 life every 2 seconds (build time x 1.5), for 45 gold and 17 lumber (35% of 160 and 60 for 80% of its life, rounded up), so D387's ratios are right and the price is now taken a point ahead (rounded up). The game starts about 0.4 s later than MirrorImaged (probes/differences/abilities-repair.txt).

- **D402 (the spells corrected by the game's numbers):** with the probe cleaned (foes paused, the caster's attack at 0, Starfall run apart because its stars reach 2000 away or more) the game gave: Starfall about 50 a wave, a wave about every 1.5 s; Mirror Image one image more than the caster (DataA); Silence is ANsi (not Siphon Mana); Carrion Beetles needs a corpse as target (refused on a probe corpse); and an immediate order must not walk the caster to the point 0, 0 (it did, for any spell with a range). All fixed in the models; they stay Close.

- **D403 (Banish, measured):** Banish puts `BHbn` on its target, a foe or a friend, for the ability's time (a hero's own on a hero): the unit is ethereal (`IsUnitType`) and slower by DataA, and takes a
  hit of 0 as the buff comes and as its time ends (a buff has an `Ended` action for that, which a dispel does not run). An ethereal unit attacks nothing and its order to attack ends; a unit's attack
  reaches one only with a weapon of attack type magic; a hit reaches one by attack type magic, or by spells with a damage type other than normal, times the constants' `EtherealDamageBonus`, and every
  other hit fires DAMAGING alone. The bonus is read from the fourth and sixth of the constant's seven entries (spells and magic), which is this lane's reading of `0,0,0,1.66,0,1.66,0`, both being
  1.66; without the constant it is 1.66. MirrorImaged gives probe abilities-banish's lines with no difference. Not measured, and left as it was: a swing under way when the buff comes still lands.

- **D404 (three corrections the Banish probe measured):** (1) the attack type's row of the damage table now cuts every damage type, where it cut only normal: 100 of magic at a hero by attack type
  spells takes 75, and by attack type magic at a footman 200; the armour still cuts normal alone. (2) A unit a buff slows keeps the constants' `MinUnitSpeed` (half of 270 reads 150). (3) A unit whose
  Attack (`Aatk`) `UnitRemoveAbility` took off attacks nothing, and `UnitAddAbility` gives it back.

- **D405 (Carrion Beetles, measured):** the four orders are handled before the spell's own: `carrionscarabs` goes on only as a target order on a corpse, `carrionscarabsinstant` with no target becomes
  that order on the nearest corpse within the ability's area, and the autocast orders set a flag on the ability that casts on the nearest corpse whenever it is ready, looked at every 0.25 s (the game
  cast at once, and in the second its cooldown ended). A body is a corpse once its death time is over, or at once when `CreateCorpse` made it. The beetle's type is DataC's (the game's data has no
  UnitID for it), DataA of them, with no timed life, and one that would be over DataE of its caster's dies as it comes. MirrorImaged gives probe abilities-beetles's lines with no difference. This
  lane's choices, not measured: the nearest corpse, and that a hero's or a building's body is no corpse.

- **D406 (Locust Swarm, Close):** DataA locusts of the ability's unit, one every DataB seconds from the effect, hit foes within the ability's area of the caster with their own attack, bring DataD of
  what four hits took back to the caster, bring what they carry as the ability's time ends, and are removed 1.6 s after it (the game: 1.5 to 2.0). All of that is measured. The rest is a model, as
  the game's locusts fly about: a locust hits every 3 s (the game's average was 2.8 to 3.3), the first 2 s after it comes out, and is back 1 s after its fourth hit; the foes are taken nearest the
  caster first, DataC locusts each and the rest of the swarm on the last, which uses DataC by its name (the most swarm units a target has) and was not measured; the locusts stand at their foe or at
  the caster, and the swarm ends when its caster dies. Four hits is what 120 of 124 of the game's locusts came back with; the 3 with five and the 1 with three are not modelled, and DataE does
  nothing here. The unit engine leaves a locust alone (`Swarming`).

- **D407 (the three probes):** each runs every case in one map, the cases far enough apart, and prints numbers the game gives the same every run; where the game's own numbers vary (which locust hits
  whom, and when) the expected lines hold `{t}` and `{w}`, so MirrorImaged is compared there only in form. The casters' mana is filled before each order and the damage tables are read inside one
  instant, as a caster's mana does not come back in MirrorImaged as it does in the game. Probe abilities-spells's differences file changed with the three models and is recorded again; its casters outside the map
  (docs/facts.md) are left as they are.

- **D408 (probe abilities-spells, written again):** every caster and foe is inside the map: nine places 1300 apart, a spell each, cast at once, and Starfall alone afterwards from the middle of the map, as it
  reaches 1000 and left foes alone when cast near the map's edge. The foes are not paused, as a paused unit takes a spell's first hit only and its buffs never end, and each is held by `SetUnitPropWindow(u, 0)`,
  as a unit that is hurt runs; one paused foe by each spell keeps that on record. Each foe's hits are printed as runs of one amount with the first and the last time, the buffs by when they came (to 0.1 s) and
  how long they were on, as the probe looks every 0.05 s and MirrorImaged's cast comes a moment later than the game's; the orders come at 1.02 s, off the looks' instants. What changes from run to run is `{t}`
  or `{w}`: where Mirror Image puts the caster and its image and which summon event has the image, when the image and the Wind Walk hero hit, and Starfall's hits. The mana a caster has spent is counted against
  a hero that casts nothing, and the hero that runs out of mana has intelligence 0, as mana comes back to a hero in the game and not in MirrorImaged. Banish, Locust Swarm and Carrion Beetles are no longer in
  it: they have probes of their own (D407).

- **D409 (a spell's area counts the foe's edge):** the foes a spell finds round a place are those whose collision circle reaches into the area (`Foes`), for every spell that looks for them this way. Measured
  for Flame Strike, Immolation, Silence and Starfall; for the others (Frost Nova, War Stomp, Fan of Knives, Blizzard, Inferno, the lines of Carrion Swarm and Shockwave, Earthquake, the jumps of the chains)
  it is taken to be the same. Thunder Clap, which has its own search, is left as it was.

- **D410 (Flame Strike, measured):** from the ability's casting time after the effect, DataA every DataB seconds for the hero duration, then DataC every DataD seconds until the duration is over, of damage type
  fire, to the ground foes at the place; a paused foe takes the first hit alone. MirrorImaged gives the probe's hit lines with no difference. Left as it was, not measured: friends, the caster and buildings,
  which the ability's targets name, are not burned, and a dead caster's fire stops.

- **D411 (channelled spells: Starfall and Earthquake):** a spell that channels keeps its order after the effect and its caster does nothing else (`unit.Channel`); another order, or the caster's death, ends it,
  and so does its time, taken as before from the hero duration for a hero. The end-cast event fires then, and the finish event only when the time ran out, which is the game-facts line that finish fires only
  for a spell that ran to its end; the probe's stop order gave end cast and no finish. Starfall: DataA of universal damage to the foes in the area every DataB seconds. The game drops each foe's star at a
  time of its own (4 to 7 hits in 10 s), which is not modelled: the waves here are a model, Close. Earthquake: from DataA seconds in and every second, the buildings at the place take DataB of universal damage
  and the other foes there get `BOeq` for 2 s, slower by DataC; the 2 s is the measured time and is in no field. That it slows friends too is not measured and not done.

- **D412 (Silence takes only what has mana):** the game gave a Paladin and Priests the buff and a footman among them none. Silence here silences the foes whose mana maximum is above 0. That mana is what
  decides it is this lane's reading: the footman also has no spell to cast, and the ability's targets (air, ground, enemy, organic, neutral) don't tell them apart.

- **D413 (Wind Walk, measured; cast at once):** the buff makes the unit DataB faster, hides it from foes from DataA seconds in (`IsUnitInvisible` true and `IsUnitVisible` false for a player that is neither its
  owner nor an ally), keeps it from taking a target by itself, and ends as its next attack lands: a hit of 0 of attack and damage type 0, then the attack with DataC more. Wind Walk and Immolation are cast
  inside the order's call, all five cast events with it, as the game gave them; the other spells keep MirrorImaged's cast flow. Not modelled: detection, and the attack the game's hero made in the 0.6 s
  before it was hidden, in two runs of three.

- **D414 (Immolation is a switch):** the `immolation` order puts `BEim` on the caster until `unimmolation`, which answers true and takes it off, or until the mana is out. DataA of fire comes 0.01 s after the
  effect and every duration (1 s) after, to the ground foes in the area, a paused one only the first time. The mana goes DataB a second in 128 steps a second, as the game's went evenly (35.64 at 1.52 s), and it
  goes off when two steps can't be paid, which puts the end where the game's was (2.85 s on 20 mana, 0.10 left). This lane's choices, not measured: `unimmolation` on a unit that isn't burning answers
  false, and an `immolation` order while it burns starts it again.

- **D415 (Mirror Image, measured):** DataD seconds after the effect DataA images come: units of the caster's type with its life and mana and the type's own attack, `IsUnitIllusion` true, whose hits take
  DataB of their damage (the damage events still fire, with 0) and that lose DataC times a hit (`unit.Deals`, `unit.Takes`), dying when the ability's time is over. The cast fires no finish event. Left as a
  difference: the game fires a second summon event 0.18 s after the first, one of the two without a unit, and moves the caster and the image to two places near (which of them where changes from run to
  run); here one summon event fires, with the image, and the caster stays.

- **D430 (A game of computers only, and their difficulty):** `SimulationOptions.Users` may be empty when a computer is seated: the run is then seen from
  the first computer's slot (`GetLocalPlayer`), and a lineup with nobody is still refused. The game itself needs a person, as a player or an observer;
  observers are not modelled, so this is a test's way to let computers play each other, not something measured. `SimulationOptions.Difficulty` (Easy, Normal,
  Insane; `mirror.load(map, {difficulty = "easy"})` in a Lua test) is what `GetAIDifficulty` and `MeleeDifficulty` answer for every computer, and names them
  ("Computer (Easy)"); Normal stays the default (D141). One difficulty for all computers: no test needs them to differ yet. A recording keeps it.

- **D431 (The AI natives that stay shells):** counted over `common.ai` and the four race scripts, by what `main` of each race script can reach. On the melee
  path and modelled: D432, D433. On the melee path and left as they are: `GetMegaTarget` (nil: the game's rule for a player weak enough for an all-out
  attack is not known), `CommandsWaiting` (0: no melee map sends its computers commands; the order `CommandAI`'s commands come out in is not measured, so
  the queue waits for a probe), `StartGetEnemyBase` and `WaitGetEnemyBase` (the search is done at once), `PurchaseZeppelin` (only on a map the script
  finds no way across), `RemoveInjuries` and `RemoveSiege` (the wave is the units the script asked for). The settings of how the AI fights and works
  (`SetTargetHeroes`, `SetUnitsFlee`, `SetGroupsFlee`, `SetHeroesFlee`, `SetPeonsRepair`, `SetHeroesBuyItems`, `SetHeroesTakeItems`, `SetIgnoreInjured`,
  `SetSmartArtillery`, `SetDefendPlayer`, `SetWatchMegaTargets` and the campaign's like them) and the texts for the script's developer (`DisplayText*`,
  `Debug*`) are Recorded: nothing reads them back, and what they would change (fleeing, repairing, shopping) is not simulated. The campaign's natives
  (guard posts, defenders, suicide waves, `IgnoredUnits`, `Unsummon`, zeppelin loading) stay Close shells, off the melee path.

- **D432 (What the melee script asks before it attacks; none measured):** `TownThreatened` is true while an armed unit of an enemy player (not a creep,
  not a building) stands within 1,000 of one of the AI player's town halls; the script then keeps its wave home. Nothing more is done about the threat:
  the units at home fight what comes in range, and a wave that is out is not called back. `IsTowered` is true while a finished enemy building with an
  attack stands within 900 of the target. `UnitInvis` is whether a buff hides the unit (as `IsUnitInvisible`, without the viewer). `CreepsOnMap` is
  whether a creep lives. `SetAllianceTarget` keeps the unit for the AI player and `GetAllianceTarget` gives what an allied AI player set, while it
  lives, the lowest slot's first, never its own: the script joins an ally's attack and goes on with its own choice otherwise.

- **D433 (What kept the melee AI from playing on, found on Echo Isles):** (1) `SetProduce` of a building another turns into (Keep, Stronghold) queues
  the upgrade there; it used to send a worker to build one, which no worker can, and the script's build list stops at the first request that fails.
  (2) `SetProduce` of what nothing of the player's trains answers true: `common.ai`'s `StartUnit` says the native "may not be able to right now but
  that doesn't stop us from trying other units", having set the price aside itself. What the game's `SetProduce` answers false for is not measured.
  (3) A building whose data says `urev=1` (every altar of the game) revives its owner's dead heroes; the check read the field as a list of heroes, which
  only a test's data had, so a computer never revived its hero and its build list stopped at it. (4) The AI leaves 96 between its buildings (it left
  24, less than a worker is wide, and walled its own workers in). (5) `SetExpansion` sends no second worker while one is on its way to build the hall.
  (6) `MergeUnits` and `ConvertUnits` make the unit at once where the mount or the statue stands, for the price of the type made (the game's takes the
  ability's time; its price is not measured); the other form is the ability's `Emeu`.

- **D434 (The AI scripts of the balance set):** a race script is read from the map, else from the balance set the map's values come from
  (`_balance/<set>.w3mod:Scripts/`, which Reign of Chaos's `melee_v0` and `custom_v0` have), else from the game's `Scripts`. `common.ai` is always the
  game's: no set has one. That the game reads them this way is taken from where the files are, not measured.

- **D435 (Creeps guard their post; not measured):** a unit of the neutral aggressive player has a post, the place it stands the first step it is idle.
  Idle and more than 64 off it with nothing to fight, it walks back; fighting further than the game's `GuardReturnDistance` (1,000 where the data has
  none) from it, it lets its target go and walks back, fighting again only once it is home. `SetUnitCreepGuard(unit, false)` and `RemoveGuardPosition`
  stop that, `SetUnitCreepGuard(unit, true)` on another player's unit and `RecycleGuardPosition` make where the unit stands its post, and
  `RemoveAllGuardPositions` does it for a player's units. `GuardDistance` and `GuardReturnTime` are not used. The AI players' own units have no posts.

- **D436 (A step that clips what the way goes round):** a unit on a found way whose next step is stopped takes the part of the step along one axis
  (x first) when that is free. The way is found over the centres of 32 cells and buildings stop a unit as circles, so a unit at a circle's edge could
  have a way and no first step, and searched again for good: five workers of a computer stood so with a town hall to build.

- **D420 (Replays of the game today):** read from one replay of a ladder game of 2.0 (header version 10200, build 7003), not from a document.
  Before the lobby's slots come blocks 0x38 and 0x39 (a kind, a byte, a length of four bytes, that many bytes), which are skipped. Action 0x7A
  is 17 bytes: the two numbers of a unit, an ability and an order, sent when a button of the unit is pressed, before the order itself. Action
  0x77 is two strings and four bytes, a ladder's own data. The reader now also gives groups (0x17), a group picked (0x18), the subgroup (0x19),
  whether an order was queued, the result of a leaving player, and the map's path from the lobby's encoded settings. With these the replay has no
  action that is not read. A unit is named by two numbers; the second counts up as the game makes objects and the first is used again.

- **D421 (A replay's seats):** `Simulation.Load(map, replay)` seats the lobby's people whose slots the map has as users (a watcher has none), no
  computers, each with the race the lobby holds (a player who took random gets a random one: the replay does not say what the game drew). A
  replay does not say where the game put the players either, so each gets the start place nearest the first place it gave an order at. This
  lane's choice; it held for the one replay (both players' first orders were at their own gold mines). `SimulationOptions.Races` and
  `StartLocations` carry it.

- **D422 (Which unit a replay's numbers name):** the game's numbers for a unit are its own and a run here cannot have them, so D375's "the id
  the unit has here" never held for a played game. `ReplayRun` finds the unit when an order first needs it: among the player's living units no
  numbers have yet, made after the units of lower second numbers and before those of higher ones, the first of the type the subgroup action
  told of that unit; else a building or not, as a pressed button told (`Aque`, a build menu); else, in a selection of several, the first that
  is not a building; else the first. An order's target is the unit with no numbers yet within 64 of the place the order gives, else the tree
  there, else the place. A unit the game made and this run did not is never found, and its orders are counted (`Unowned`). This lane's
  model, Close: a wrong match is possible and is not detected. `ToRecording` is gone; the run's own recording (`act` inputs, with the run's
  unit ids) plays it again exactly.

- **D423 (Who takes a replay's order):** smart, stop, attack, move, patrol and hold position go to every unit of the selection. Any other order
  is a button of the card and goes to one unit: the first that takes it, those of the shown subgroup's type first. The game's own rule (the
  building with the shortest queue, the nearest worker) is not measured. An order of a hero ability's code is the skill learned. Read and not
  played, and counted (`NotPlayed`): giving and dropping items (0x13, 0x14), taking from a queue (0x1E, 0x1F) and the escape key (0x61).

- **D424 (Smart at a mine or a tree):** a replay's smart order of a unit with a harvest ability (`Ahar`, `Aaha`, `Awha` for gold; `Ahar`,
  `Ahrl`, `Awha` for a tree) at a mine with gold or at a tree is given as `harvest`, as the game turns a person's right-click into it.
  `IssueTargetOrder(smart)` from a script is left as it was. Acolytes and wisps then carry gold in trips as workers do, which the game's do not.

### The corpus runs (L-mirror-corpus-runs)

- **D440 (the corpus tests run in parts):** `ShippedMapTests` and `MeleeCorpusTests` each took longer than the 10 minutes the test run gives a test (35 min 17 s for the 466 shipped maps on 11-19, and
  about 3 h 13 min for the 258 melee maps, worked out from the 136 the old test got through in 5,016 s), so nobody ran them to the end. Each is now a theory of 8 parts (`Corpus.Shards`), a part taking
  every 8th map of the sorted list, so the long maps, which sort together by their players, are spread over the parts. A part is one test, so the 10 minutes count for each part: the longest took 2 min 2 s of the shipped maps' 7 min 35 s and 4 min 33 s of the melee maps'
  31 min 26 s (with 4 parts the longest melee part took 8 min 57 s, too near the limit). The two classes are one
  xUnit collection, so they never run at once.

- **D441 (a part runs its maps side by side):** on half the PC's processors (`Environment.ProcessorCount / 2`, 6 on the 12 this was measured on), each map its own simulation and file. A map's result
  doesn't depend on what runs beside it: the 136 melee maps the old test finished had the same unit counts in the new one, and seven maps' state hashes every 150 s are the same before and after.

- **D442 (the tests use the server garbage collector):** `ServerGarbageCollector` in the test project. With the workstation collector six maps in one process each waited on the others' garbage: the
  46 two-player melee maps took 103 to 138 s, and 88 s with the server collector, while six processes ran a map as fast as one. It changes no result.

- **D443 (JASS is made into Lua one map at a time):** `Jass.ToLua` holds a lock. Once in some 1,500 loads side by side, a map's `war3map.j` failed to become Lua (`NotSupportedException`, on
  `Campaign/Classic/TFT/OrcX02_09.w3x`, which loads alone and loaded in every run since). The inner error wasn't caught, so the cause is not known: it is in the parser, War3Net's transpiler or the
  renderer, none of which says it may be used from two threads. The lock costs little, as a map's script takes about 0.2 s. This lane's call; the failure has not been seen again in the 1,448 loads of the two full runs
  with it, which is not proof.

- **D444 (the speed-ups, none of which changes a state hash):** a standard object keeps each field it has read (`StandardObject.Cell`: three strings joined and three look-ups a read before); the AI's
  nearest tree is found without sorting every tree, a type's being a tree is kept, and the towns are listed once in a round of worker slots (`AiEconomy.cs`); and a unit's sight is marked explored again
  only when it moved, its sight changed or cells were taken back since (`Vision.cs`: every `IsLocationVisibleToPlayer` marked every unit's circle, 67% of `HumanX06`, which went from 69 s to 16 s).
  The 4-player benchmark (A10, `mirror bench "(4)Adrenaline" --seconds 1800 --melee true`) went from 16.0 to 17.1 s to 11.2 s. Tried and taken back: the explored cells as one number each, whose
  hash collides and made `HumanX06` 195 s.

- **D445 (what is left, not done here):** the nearest enemy is looked for among every unit by every unit (`Nearest` in `Combat.cs`, 41% of `Orc01`'s 877 units), and the path search keeps its cells in
  dictionaries (`FindPath`, 35% of `ExtremeCandyWar2004`, the longest shipped map at 94 s). Both want a grid, which is more than a small change in another lane's file.

## Engine timing (lane engine-timing, 2026-11-19)

Probes `abilities-cast-timing`, `units-attack-timing` and `heroes-mana-regeneration`, each run in the game (3.0.1.24371); what they gave is in `docs/facts.md`, "Engine timing". The numbers D450 to D469
are this lane's; D458 to D469 are not used.

- **D450 (the cast's timing, measured):** a spell order's channel and cast events fire inside the order's call when the unit is in range, as the game's did. The effect event, with the mana and the
  cooldown spent, comes the caster's cast point later (the unit's own `ucpt`, which `BlzSetUnitRealField` sets; inside the call when it is 0), and the finish and end-cast events the caster's backswing
  (`ucbs`) after that, the order staying the unit's until then. Another order ends a cast that has begun with its end-cast event and no finish; before the cast point nothing is spent and no cooldown
  starts. Wind Walk and Immolation stay cast whole inside the call and Mirror Image's cast ends with its effect, as measured (D413, D415). The steps are scheduled on the clock (1/1024 s), not looked
  at each engine step, so the times are the game's to 0.01 s. Casts whose steps fall due in one instant go on in the order their units were made, as probe `abilities-beetles` has it (of two heroes
  ordered in one instant, the one made first had its effect first, though ordered second), and after whatever else is due then, which is not measured. This lane's choices, not measured: a spell order the unit can't take (no mana, a cooldown) leaves a cast under way as it is, where it
  silently ended one before; a caster that dies in its cast gets the end-cast event; a paused caster's cast waits until it is free. Left as a difference: a unit turns at once, where the game's Archmage
  took 0.21 s to turn round before its cast began.

- **D451 (a casting time comes after the effect event):** for an ability with a casting time, what the spell does comes that long after the effect event, with the finish and end-cast events, and the
  backswing doesn't count; an order before then ends the cast and the spell does nothing, the mana staying spent. Measured for Flame Strike (effect +0.30, finish and first fire +1.63 with a backswing
  of 0.5, 2.4 and 3; no fire when stopped 1 s in). That every ability with a casting time goes so is this lane's reading, as only Flame Strike was measured; before, the casting time was waited before
  the effect event. Flame Strike's model no longer waits its casting time itself.

- **D452 (a hero's agility and the attack rate, measured):** the attack rate is 1 + the hero's agility (with its bonuses) x `AgiAttackSpeedBonus` (0.02) + the buffs' share, kept between 0.2 and 5,
  and both the cooldown and the damage point are divided by it: a Paladin's 2.20 s between hits was 1.833, 1.746, 1.467, 1.100, 0.733, 0.550 at agility 10, 13, 25, 50, 100, 150, and 0.440 at 200, 250
  and 400, and its hit came 0.43 s after its swing began at agility 0, 0.22 at 50, 0.09 at 200. The 5 is the measured cap, in no constant that was found. Not measured, this lane's reading: that
  the buffs' share adds to the agility's before the cap, and that agility from items counts.

- **D453 (a missile's flight, measured):** a weapon of type missile, mbounce, msplash, mline, artillery, aline or alsplash lands its hit a flight after the damage point: the way between the two units,
  less the attacker's `launchY` (`ulpy`: how far ahead of it its missiles start; an archer's 62, a headhunter's -34, an Archmage's 0), over the weapon's missile speed (`ua1z`). The game's archer, at
  200 to 500, and its Archmage, at 100 to 600, landed within 0.01 s of that, with the speed set to 300, 450 and 1800 too, and the arc changed nothing. A way under 94 lands at once: an archer's 88
  did and an Archmage's 100 flew, and 94 is the middle, this lane's choice. Storm Bolt's hits land the same flight after its effect, at the ability's missile speed (`amsp`). The flight is worked out
  as the missile leaves, so a target that walks is hit as if it stood (a shortcut, named in the code). Not modelled: heights (a flier, a cliff), the other spells' missiles, which were not measured,
  and the 0.03 s more that a Priest's and a Sorceress's missiles took, for which no reason was found.

- **D454 (attacks come a cooldown apart to the clock's step):** a unit whose cooldown is running is looked at again the step the cooldown ends, not at the engine's next 1/32 s step, and a swing
  that was waited for counts its cooldown from when it was due: the game's hits came exactly a cooldown apart (1.350, 2.200, 1.500 s over 24 s), where the engine's step gave 1.375 for 1.35. The
  first swing after an order still starts on the engine's step; the game's started 0.03 to 0.27 s after the order, differently each run, which is not modelled.

- **D455 (mana comes back, measured):** a hero regains `IntRegenBonus` (0.05) a second for each point of intelligence (with its bonuses, which is this lane's reading), and its type's own
  regeneration doesn't count (an Archmage of intelligence 0 got none in 30 s; of 1, 0.50 every 10 s); any other unit regains its type's regeneration (`umpr`, or what `BlzSetUnitRealField` set). A
  paused or dead unit regains none, a hidden one does, and none goes past the most. It is worked out when the mana is read or set and before what decides it changes (`unit.Mana` is a property
  now), in the engine's steps of 1/32 s, so an idle game runs nothing for it and the mana doesn't move inside a step: the second look of a mana state event a frame after a set (probe
  units-more) still sees the set value. Not done: a unit state event doesn't fire as regeneration passes its limit (a shortcut, named in the code); life doesn't come back; the game's amounts fall
  about 0.013% short of rate x time (449.94 of 450), which is left as a difference.

- **D456 (a paused unit's buff, measured):** a buff's time stands still while its unit is paused and goes on when it is free: 16 s of Silence ended 36 s in on a Priest paused for the first 20, and
  32 s in on one paused from 4 to 20. `PauseUnit` keeps each timed buff's steps left and starts them again. What a buff's model schedules beside its end (a root's damage each second) still runs.

- **D457 (Mirror Image's summon events, measured):** the game fires a summon event for each image and one more whose summoned unit is none: 2, 3 and 4 events at levels 1 to 3, the first DataD (0.5 s)
  after the effect and the last 0.16 to 0.18 s later, which of them is the one without a unit changing from run to run. Here the images' events come at DataD and the one without a unit 0.18 s later,
  `GetSummoningUnit` the caster in each; the order and the 0.18 are this lane's choice, so the times are Close. This closes the difference D415 left.

- **D490 (an AI's town is a place):** a town was a standing town hall, so a computer whose hall died had no town: `TownHasMine(0)` read false, the race scripts' `MeleeTownHall` never asked for a new
  hall, and it lived on with its other buildings and no income. A town is now a place, kept per AI player: each of its town halls, and each mine that is its own (an undead's haunted one), makes a town
  where it stands more than 1,000 from the towns the player has, halls before mines and the first made first; a town is never dropped. `TownHasHall` asks for a finished hall of the player's within
  1,000 of the place, `TownHasMine` for a mine with gold there that is nobody's or the player's, and a building asked for in a town is sited round the place. `GetMinesOwned`, `TownWithMine` and
  `GetGoldOwned` count a town's mine only while the player has it: its own mine, or one with a finished hall of the player's beside it. `TownThreatened` stays on the standing halls, as before: counted
  from the places, a computer whose expansion had been destroyed read threatened for as long as the winner's army stood there and never attacked again (seen on Echo Isles). Close: the game's own rule
  for a town is not measured; this is the least that lets the scripts' `MeleeTownHall( 1, NECROPOLIS_1 )` (a mine with no hall) and their hall rebuilding work.

- **D491 (`GetGoldOwned` is the gold in the mines, not the player's):** it answered the player's own gold. The race scripts read it as the gold left to mine ("if we have low gold in our mines then we need
  to expand": `c_gold_owned < 2000`, and a third mine under 10,000), and read the player's gold through `GetGold`. It is now the gold left in the mines of the player's towns that it has (D490). Not
  measured in the game: read from the scripts.

- **D492 (a haunted mine is made of the gold mine):** the undead script expands with `SetExpansion(peon, UNDEAD_MINE)`, and the model built the Haunted Gold Mine beside the gold mine as it does a town
  hall: the gold mine stayed free, the new building held no gold, and the computer paid 225 gold and 210 lumber for another every two minutes for the rest of the game. This was why no undead computer
  ever had a second mine, and every two-player map's computer is undead with the corpus's one seed. A building whose type has the haunted mine's ability (`Abgm`) is now made of the nearest gold mine
  that is nobody's and has gold, within 300 of the site: it stands where the mine stood and keeps its gold, and the gold mine is removed, as `BlightGoldMineForPlayerBJ` makes the one a player starts
  with. Where no such mine stands nothing is made or paid for. This is in `Place`, so a person's or a replay's build order gets it too. Not modelled (a shortcut, named in the code): the gold mine
  doesn't come back when the haunted one dies or is cancelled; the 300 is this lane's choice (the AI's site is 160 from the mine).

- **D493 (`BlzGetPlayerTownHallCount`):** the player's living units with the town hall classification, of any race and tier, one under construction too. It answered 0, so the game's melee script
  called every player crippled: "You will be revealed ..." at 2 s and "Revealing ..." at 122 s on every map. Close: that a hall being built counts is this lane's reading of the function it replaced
  in `blizzard.j` (`GetPlayerTypedUnitCount` with incomplete ones included), not a measurement.

- **D494 (what the melee corpus asserts, and its parts):** `Every_melee_map_runs_30_minutes_with_computers_without_a_script_error` looked only at the computers' units added together. It now looks at the
  game each minute and fails a map where a computer never had more units than it began with, where no computer had two town halls at once, or where no unit was seen dead (a unit that is gone with no
  body was taken away, as a defeated player's are, and is not counted). It is not loosened for the maps that fail; they are listed in the pull request. The corpus theories run in 24 parts, not 8: a
  part took 6 to 15 minutes with other work running, against the 10 minutes the test run gives a test, and a third of that fits.
