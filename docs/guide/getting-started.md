# Getting started

## Install

MirrorImaged is a .NET 10 tool and library. You need Warcraft III: Reforged installed on the machine that runs it: MirrorImaged reads the game's
`blizzard.j` and data from the install, and never ships or downloads them.

```bash
dotnet tool install --global MirrorImaged.Cli
mirror coverage
```

`mirror coverage` prints how many of the game's natives are at each [trust level](trust-levels.md) and which are Missing. Until the
package is published, build it from the repository with `dotnet pack src/MirrorImaged.Cli` and install it from the folder with
`--add-source`.

### Where the game is

MirrorImaged looks for the game in `C:\Program Files (x86)\Warcraft III`. If yours is somewhere else, set `MIRROR_GAME` to its folder, the one that
holds `.build.info`:

```powershell
$env:MIRROR_GAME = "D:\Games\Warcraft III"
```

### Platforms

Windows x64 is the supported platform: every test against the installed game runs there. On Linux x64 and on macOS with Apple silicon, MirrorImaged
can run maps with `MIRROR_GAME` set to a copy of the install, as it reads the game's files itself, but that is not tested. The tests that need no game
run on all three, and a run's state hash is the same on each.

MirrorImaged's Lua library is built for those three only: on Windows on ARM, Linux on ARM and Intel Macs every run fails with `DllNotFoundException`.

## Run a map

```bash
mirror run MyMap.w3x --seconds 60 --seed 1
```

The map's script runs on a clock that moves in steps of 1/1024 s. `print` and text shown to a player go to the log, which is printed at the end,
each line with its time:

```
2.000 print: gold	10
```

`--hash 10` prints a hash of the whole world every 10 seconds. Two runs with the same map, seed and input print the same hashes, on every
operating system.

`mirror check MyMap.w3x` lists the natives the script calls and their trust levels before you run it.

## Write the first test

Make a file next to the map called `gold.test.lua`:

```lua
test("the player is paid every 2 seconds", function()
  local sim = mirror.load("gold.w3x")
  sim:advance(7)
  sim:say(0, "-gold")
  sim:advance(0.1)
  assert_contains(sim:log(), "gold\t30")
end)
```

```bash
mirror test .
```

```
PASS gold.test.lua: the player is paid every 2 seconds
1 passed, 0 failed
```

The map for this example is in `examples/lua/gold`, with this test: `mirror test examples/lua/gold` runs it. The map there, `gold.w3x`, is a folder map:
a folder named as a map that holds the map's files, here only `war3map.lua`. MirrorImaged reads a folder map as it reads a `.w3x` file, and a map with no
`war3map.w3i` has one person, in slot 0. The game itself needs the rest of a map's files to open it.

The [Lua tests](lua-tests.md) page lists every call; [C# tests](csharp-tests.md) does the same for xUnit.
