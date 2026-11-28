# MirrorImaged

[![CI](https://github.com/wc3-dev/MirrorImaged/actions/workflows/ci.yml/badge.svg)](https://github.com/wc3-dev/MirrorImaged/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/MirrorImaged.Cli?label=MirrorImaged.Cli)](https://www.nuget.org/packages/MirrorImaged.Cli)
[![Licence: MIT](https://img.shields.io/badge/licence-MIT-blue)](LICENSE)

Headless Warcraft III: Reforged for testing maps.

MirrorImaged loads a `.w3x` map and runs its Lua or JASS script against C# copies of the game's natives, on a simulated world and clock. A test can give orders, input and chat, read what happened and check it. No game runs, and the same map, seed and input always give the same result.

The copies are built from probes run in the game: each native has a [trust level](docs/guide/trust-levels.md) that says how closely it matches.

## Quick start

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download) and Warcraft III: Reforged installed. MirrorImaged reads `blizzard.j` and the game's data from your install, and never ships or downloads game files.

```powershell
dotnet tool install --global MirrorImaged.Cli
mirror coverage
```

Run a map for a minute of game time and print its log:

```powershell
mirror run MyMap.w3x --seconds 60 --seed 1
```

Write a test next to the map, `gold.test.lua`:

```lua
test("the player is paid every 2 seconds", function()
  local sim = mirror.load("gold.w3x")
  sim:advance(7)
  sim:say(0, "-gold")
  sim:advance(0.1)
  assert_contains(sim:log(), "gold\t30")
end)
```

```powershell
mirror test .
```

```
PASS gold.test.lua: the player is paid every 2 seconds
1 passed, 0 failed
```

This map and test are in [`examples/lua/gold`](examples/lua/gold).

## Tests in C#

Reference the `MirrorImaged` package from any test project (xUnit, NUnit or another):

```csharp
using MirrorImaged;

public class GoldTests
{
    [Fact]
    public void The_player_is_paid_every_two_seconds()
    {
        using var simulation = Simulation.Load("gold.w3x", new SimulationOptions { Seed = 1 });

        simulation.Advance(TimeSpan.FromSeconds(7));
        simulation.Say(0, "-gold");
        simulation.Advance(TimeSpan.FromSeconds(0.1));

        Assert.Contains(simulation.Log, line => line.EndsWith("gold\t30"));
    }
}
```

With `MirrorImaged.TestAdapter` added too, `dotnet test`, Rider and Visual Studio also find and run your `*.test.lua` files.

## What it does

| Command | What it does |
| ------- | ------------ |
| `mirror run <map>` | Runs a map for a number of simulated seconds and prints its log; `--hash` prints the world's hash, `--record` saves the run |
| `mirror test <file or folder>` | Runs Lua tests; `--junit` writes JUnit XML |
| `mirror check <map>` | Lists the natives a map's script calls, with their trust levels |
| `mirror coverage` | How many natives are at each trust level |
| `mirror replay <recording or .w3g>` | Plays a recording again exactly, or a game replay as far as MirrorImaged models the game |
| `mirror snapshot`, `mirror compare` | Saves the world at a time and lists what differs in a later run; `snapshot --back` steps back |
| `mirror desync <map>` | Runs the map once for each person's slot, as that player's own game, and reports where the runs first differ |
| `mirror bench <map>` | How many times faster than real time a map runs |
| `mirror fuzz <map>` | Gives every player random legal orders and reports script errors |
| `mirror view <map or recording>` | Writes one HTML page that scrubs through the run in a browser |

Maps can be written in Lua, JASS, TypeScript (with TypeScriptToLua) or C# (with Moonwell). The [`examples`](examples) folder has the same gold map in each, and the test suite builds and runs every one.

## How close it is to the game

| Level | Meaning | Natives |
| ----- | ------- | ------- |
| Exact | Matches the game on every case a probe measured | 464 |
| Close | A documented model, not fully measured | 1,015 |
| Recorded | Changes nothing a map can read back; the call is logged | 356 |
| Missing | Stops the run with the native's name | 0 |

Every native in `common.j` and `common.ai` has a level. A run is as trustworthy as the lowest level among the natives it calls. The unit engine, pathing, combat timing and the computer players' AI are models, so a long game won't match the real game move for move. The [release notes](https://github.com/wc3-dev/MirrorImaged/releases) list the known differences.

## Platforms

Windows x64 is the supported platform: every test against the installed game runs there. Linux x64 and macOS on Apple silicon can run maps with `MIRROR_GAME` pointing at a copy of the install, but that isn't tested. A run's state hash is the same on all three.

MirrorImaged looks for the game in `C:\Program Files (x86)\Warcraft III`. If yours is elsewhere, set `MIRROR_GAME` to its folder.

## In CI

A GitHub Action installs `mirror` and runs your tests with JUnit output, on a self-hosted runner that has the game installed:

```yaml
- uses: wc3-dev/MirrorImaged/.github/actions/mirror-test@main
  with:
    tests: tests
```

See [Continuous integration](docs/guide/ci.md).

## Packages

| Package | What it is |
| ------- | ---------- |
| [`MirrorImaged.Cli`](https://www.nuget.org/packages/MirrorImaged.Cli) | The `mirror` tool |
| [`MirrorImaged`](https://www.nuget.org/packages/MirrorImaged) | The C# test API, `Simulation` |
| [`MirrorImaged.TestAdapter`](https://www.nuget.org/packages/MirrorImaged.TestAdapter) | Runs `*.test.lua` files from `dotnet test`, Rider and Visual Studio |
| `MirrorImaged.Natives`, `.World`, `.Data`, `.Lua` | The parts the others are built from; their APIs may change in any release |

## Documentation

- [The guide](docs/guide/index.md): getting started, Lua and C# tests, every command, recordings and replays, the viewer, trust levels and CI
- [Progress](docs/progress.md): how far each area of the game is done

## Licence

[MIT](LICENSE)
