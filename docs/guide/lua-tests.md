# Lua tests

`mirror test <file or folder>` runs the `*.test.lua` files it finds, in a Lua state of their own, apart from the games they load. Each result is
printed; `--junit results.xml` also writes JUnit XML. The exit code is 1 when a test fails.

A test file calls `test(name, function)`. Inside, `mirror.load` opens a map. A map path that is not rooted is read from the test file's folder.
Every simulation a test opens is closed when the test ends.

```lua
test("two footmen fight", function()
  local sim = mirror.load("fight.w3x", {seed = 7})
  sim:advance(2)
  local mine = sim:units(0)[1]
  sim:order(0, mine.id, "attack", 300, 0)
  assert_true(sim:wait_until(function() return sim:unit(mine.id).life < mine.max_life end, 20))
end)
```

## The simulation

| Call | What it does |
| ---- | ------------ |
| `mirror.load(path, {seed = n, difficulty = "easy", graphics = "classic"})` | Opens a Lua or JASS map; the seed starts the random numbers (0 by default), the difficulty is the computers' (`easy`, `normal` by default, `insane`), the graphics mode the player's (`reforged` by default, `classic`), whose data the run reads |
| `sim:advance(seconds)` | Moves the clock on |
| `sim:advance_steps(n)` | Moves the clock on by whole steps of 1/1024 s, exactly |
| `sim:wait_until(check, within, every)` | Advances by `every` (0.1 s) until `check()` is true or `within` seconds have passed; returns whether it held |
| `sim:time()`, `sim:step()` | The clock now, in seconds and in steps |
| `sim:hash()` | A hash of the world: equal for runs that are the same |
| `sim:log()` | The lines the run printed or showed, as a list of strings: `time print: text` |
| `sim:stopped()` | The error that stopped the run, or nil |
| `sim:recording()` | The run so far as JSON, to [replay](recordings.md) |
| `sim:recorded()` | Every call of a [Recorded](trust-levels.md) native so far, in order: `{step = n, native = "Name", args = {...}}`, a handle as its id and a function as `"function"` |
| `sim:close()` | Ends the run early |

## Units

`sim:units(player)` lists the living units, of one player or all when the player is nil, as tables with `id`, `type` (the four-character code),
`name`, `player`, `x`, `y`, `life`, `max_life`, `mana`, `order` (the order's id), `order_name` (its string, `""` for none) and `dead`. `sim:unit(id)` is one of them, or nil.

## What a person does

These are a player's own input. Only a player's own units obey its orders and selection, as in the game.

| Call | What it does |
| ---- | ------------ |
| `sim:order(player, unit, order, x, y, target)` | Orders the unit, by the game's order string (`move`, `attack`, `smart`, `stop`...), to a place, to another unit, or alone |
| `sim:select(player, unit, ...)` | Selects exactly these units; fires the selected and deselected events |
| `sim:key(player, key, down, meta)` | Presses or releases an OS key (65 is A) with the meta keys held (1 shift, 2 control, 4 alt, 8 meta) |
| `sim:mouse(player, kind, button, x, y)` | Presses (305), releases (306) or moves (307) the mouse over the map; the button is 1 left, 2 middle, 3 right |
| `sim:say(player, text)` | Types a chat message |
| `sim:leave(player)` | The player leaves the game |

## Checks

`assert_eq(actual, expected)`, `assert_true(value)`, `assert_close(actual, expected, tolerance)`, `assert_contains(list or text, wanted)` and
`fail(message)`. A failing check says what it expected, and the line of the test.

## From `dotnet test` and the test explorers

A test project that references `MirrorImaged.TestAdapter` and copies its `*.test.lua` files to its output folder has them found and run by `dotnet test`, Rider and Visual Studio, one test each, with
the failure's message shown:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
  <PackageReference Include="MirrorImaged.TestAdapter" Version="1.0.0" />
  <None Include="**\*.test.lua" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

The files are the same ones `mirror test` runs; a map path in them is read from the test file's folder, so the maps go to the output folder too.

## The example

`examples/lua/gold` is a map that pays its player 10 gold every 2 seconds. Its test checks the gold after 7 seconds and that two runs of
one seed end in the same hash. The same test runs for the [JASS](jass-and-typescript.md), TypeScript and [C#](csharp-maps.md) versions of the map.
