# MirrorImaged guide

MirrorImaged runs a Warcraft III map's script on a simulated clock, with no game open, so a test can give the map orders, input and chat,
read what happened, and check it. It needs the game installed (for `blizzard.j` and the standard unit, ability and item data) and never ships or
downloads game files.

| Page | What it covers |
| ---- | -------------- |
| [Getting started](getting-started.md) | Install, run a map, write the first test |
| [Lua tests](lua-tests.md) | `mirror test`, the `mirror` API and the checks, with the example map |
| [C# tests](csharp-tests.md) | The `Simulation` API from xUnit or NUnit |
| [JASS and TypeScript maps](jass-and-typescript.md) | The same tests for maps written in JASS or TypeScriptToLua |
| [The command line](cli.md) | Every `mirror` command and option |
| [Recordings, replays and stepping back](recordings.md) | Recording a run, replaying it, snapshots, the desync check |
| [The viewer](viewer.md) | `mirror view`: scrub through a run in a browser |
| [Trust levels](trust-levels.md) | What Exact, Close, Recorded and Missing mean, and how to read the coverage |
| [Continuous integration](ci.md) | A GitHub Action and JUnit output |

The worked examples are in the `examples` folder of the repository. Each is built into a map by the test suite and run, so they stay true.
