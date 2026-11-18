# C# tests

Reference the `MirrorImaged` package from a test project. `Simulation` is the whole API; it works with xUnit, NUnit or anything else, as it only
needs a path to a map.

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

## Loading

`Simulation.Load(path, options)` reads a `.w3x` or `.w3m` whose script is `war3map.lua` or `war3map.j` (JASS is turned into Lua first) and runs its
`config` and `main`. `SimulationOptions` takes the `Seed`, the `Users` (the slots people play, the first being the local player) and `Computers`
(the slots computers play), the `Difficulty` the computers play at (Normal unless given) and the `Documents` folder the map's preload files go
to. With `Users` empty and computers seated, the computers play each other and the run is seen from the first computer's slot.

## Time

| Member | What it does |
| ------ | ------------ |
| `Advance(TimeSpan)` | Moves the clock on, rounded to the step (1/1024 s) nearest all the time asked so far |
| `AdvanceSteps(long)` | Moves it by whole steps, exactly |
| `Until(check, within, every)` | Advances until the check holds; returns whether it did |
| `Step`, `Paused`, `Ended` | Where the clock is, and whether the map paused or ended the game |
| `Stopped` | The `LuaException` that stopped the run (a script error, or a Missing native) |

A script error goes in the log and the run goes on; a native that is [Missing](trust-levels.md) stops the run and `Advance` throws.

## Reading

`Log` is every line the run showed. `Units(player)` and `Unit(id)` return `UnitView` records (id, type, name, player, place, life, mana, order, dead).
`StateHash()` is a hash of the world that is equal for runs that are the same.

## What a person does

`Order`, `Select`, `Key`, `Mouse`, `Say` and `Leave` are the input of a player, each recorded at the step it was given. Only a player's own units obey
its orders and selection.

```csharp
simulation.Order(player: 0, unit: footman.Id, order: "attack", x: 300, y: 0);
var hurt = simulation.Until(() => simulation.Unit(footman.Id)!.Life < footman.MaxLife, TimeSpan.FromSeconds(20));
```

## Recording, snapshots and stepping back

`Recording` is what the run was given; `Simulation.Replay(recording)` and `Follow(recording)` run it again to the same hash. `Snapshot()`,
`Snapshot.Differences(simulation)`, `Simulation.Restore(snapshot)` and `StepBack(steps)` are in [recordings](recordings.md), and
`Desync.Check(map, time)` is the [desync check](recordings.md#the-desync-check).
