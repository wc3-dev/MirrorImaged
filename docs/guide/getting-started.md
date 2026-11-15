# Getting started

## Install

MirrorImaged is a .NET 10 tool and library. You need Warcraft III: Reforged installed on the machine that runs it.

```bash
dotnet tool install --global MirrorImaged.Cli
mirror coverage
```

`mirror coverage` prints how many of the game's natives are at each [trust level](trust-levels.md) and which are Missing. Until the
package is published, build it from the repository with `dotnet pack src/MirrorImaged.Cli` and install it from the folder with
`--add-source`.

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

The map for this example is `examples/lua/gold`. The [Lua tests](lua-tests.md) page lists every call; [C# tests](csharp-tests.md) does the same for xUnit.
