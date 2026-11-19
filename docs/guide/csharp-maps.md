# C# maps

A map written in C# with [Moonwell](https://github.com/wc3-dev/moonwell) is tested like any other: Moonwell compiles the C# into `war3map.lua`
and packs the map, and MirrorImaged runs what it packed. The tests can be in Lua, or in C# with the `Simulation` API ([C# tests](csharp-tests.md)).

## The example

`examples/csharp/gold` is the gold map as a Moonwell project. `Pay.cs` pays the first player 10 gold every 2 seconds and shows their gold when they
type `-purse`:

```csharp
public sealed class Pay : GameComponent
{
    public override void OnGameStart()
    {
        Every(2f, () => Player.One.Gold += 10);
        Command("purse", (player, rest) => Game.Print("gold " + player.Gold));
    }
}
```

The chat command is `-purse`, not `-gold`: in a development build, `-gold` is also one of Moonwell's debug commands.

Its folder has no `map.w3x/`, the terrain World Editor saves: put one there (any map saved as a folder, or `moonwell new`'s), then build:

```
dotnet tool restore
dotnet moonwell build
mirror test .
```

`dotnet tool restore` installs the Moonwell version the folder's `.config/dotnet-tools.json` names. The build writes `build/Gold.w3x`, which is a
folder map MirrorImaged reads as it is. The test loads it from there:

```lua
test("the player is paid every 2 seconds, in C#", function()
  local sim = mirror.load("build/Gold.w3x")
  sim:advance(7)
  sim:say(0, "-purse")
  sim:advance(0.1)
  assert_contains(sim:log(), "gold 30")
end)
```

`Game.Print` shows the text to every player, so the log's line is `player 0: gold 30`.

## In the test suite

The suite copies the example, gives it a blank 32 by 32 `map.w3x/`, restores the tool, builds it and runs its test. It needs the game installed
(Moonwell reads the game's `common.j` and `Blizzard.j`, and MirrorImaged its data), and is skipped without it, as every test that reads the install
is.
