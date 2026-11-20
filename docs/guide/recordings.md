# Recordings, replays and stepping back

A run is a function of its map, seed, seats and input, so it can be saved and repeated exactly.

## Recording

Everything a person does through the API (`Order`, `Select`, `Key`, `Mouse`, `Say`, `Leave`) is kept with the step it was given at. `simulation.Recording` is
the map, seed, seats, those inputs and the step the run reached; `ToJson()` writes it and `Recording.FromJson` reads it.

```bash
mirror run MyMap.w3x --seconds 60 --seed 4 --input inputs.json --record run.json
mirror replay run.json --hash 10
```

`inputs.json` is a list such as `[{"Step": 2048, "Kind": "say", "Player": 0, "Text": "-hi"}]`. The kinds are `say`, `order`, `select`, `key`, `mouse` and
`leave`. `replay` prints the same hashes every time, on every operating system.

## Snapshots

A snapshot is the step, the hash of the world and a hash of each part of it (the time, each player, each unit), with the recording that reaches it.

```bash
mirror snapshot MyMap.w3x --at 30 --seed 4 --out at30.json
mirror compare at30.json
mirror compare at30.json --map MyMap-v2.w3x
```

`compare` runs the snapshot's run again and lists what differs: `unit 1048612 (Footman) differs`. It is how a change to a map, or to MirrorImaged, shows
up in a test: keep the snapshot of a good run and compare later runs against it. In C#, `snapshot.Differences(simulation)` does the same for a simulation at the
snapshot's step.

## Stepping back

`simulation.StepBack(steps)` returns a new simulation at the step the run had that many steps ago, made by running the map again with the same inputs, so its
state is the state the run had then. The simulation you called it on goes on as it is. Stepping back costs the run to that step.

## The desync check

In the game each player runs the map on their own machine, and the games must stay the same. A map that changes the game from something only one machine
knows makes the games differ, and the players disconnect. `mirror desync MyMap.w3x --seconds 120` runs the map once for each slot a person plays, as that
player's own game, and compares the worlds each second. When they differ it runs the games again to find the step, and once more to find the call:

```
Desync at step 2048 (2 s): player 0 differs
  player 0's game: PLAYER_STATE_RESOURCE_GOLD is 0 (state 3B81FEE14CC1DCC1)
  player 1's game: PLAYER_STATE_RESOURCE_GOLD is 5 (state BD3393C8B19E6C80)
    [C]: in function 'SetPlayerState'
    war3map.lua:5: in function <war3map.lua:3>
```

- **The step** is the one the games first differ in, 1024 to the second, not the second they were compared at.
- **The part** says where: the time, a player, a unit, `random numbers`, `handle ids`, a trigger, a frame or `sync events`.
- **The value** is what differs in that part and what it is in each player's game. `not there` is a game that has no such thing, such as a frame it
  destroyed.
- **The Lua stack** is of the call that made the value differ, the native first and then the lines of the map's script that led to it. A game that made
  no such call in the step has none. A JASS map's lines are those of the Lua its script is made into.

The first line of the log that differs is printed too. A map with one user slot has nothing to compare. `Desync.Check(map, time)` is the same check from
C#: its `DesyncReport` has `Step`, `Part`, `What`, and `Values`, `Stacks` and `Hashes` with a place for each of `Players`. A report costs three runs of the
map, a clean check one.

### What it finds

The patterns are those `docs/game-facts.md` ("Desyncs") and Moonwell's analyzer rules (MW0001 to MW0004, MW0006, MW0038, MW0048) name, as far as a simulator can show them. Each is a
test in `DesyncSuiteTests`.

| The map does this in one player's game only | The part reported |
|---|---|
| Changes a player's state, a unit, or anything else in the world | The player or the unit |
| Draws a random number (`GetRandomInt`, `GetRandomReal`, `math.random`) | `random numbers` |
| Makes a handle (`CreateUnit`, `CreateGroup`, `Location`, `CreateTimer`, a frame, an event registered) | `handle ids` |
| Disables, enables or destroys a trigger | The trigger |
| Destroys a frame | The frame |
| Registers a sync event for the local player (`BlzTriggerRegisterPlayerSyncEvent(t, GetLocalPlayer(), ...)`) | `sync events` |
| Runs `ForGroup` or `ForForce` (also through `SelectGroupForPlayerBJ`) | `group and force loops` |

A value only one machine has, used outside a local-player block, is found the same way, by what it changes: each game is run as its own machine. In each
game `GetLocalPlayer()` is that game's player, the camera's target and eye (`GetCameraTargetPositionX`, `GetCameraEyePositionY`, the `Loc` forms) read 64
further along x and y for each next player, and the window (`BlzGetLocalClientWidth`, `BlzGetLocalClientHeight`) is 16 by 9 larger. So gold set from the
camera's x differs between the games and is reported.

Two more things differ per machine, so the rules about them are shown too. Each game's memory is laid out its own way (the first machine's is the arena's own), so
`pairs` over a table keyed by tables or handles visits them in another order in each game (MW0004). Each game's clock runs a second ahead of the one
before, so `math.randomseed(os.time())` or `os.clock()` seeds the shared generator differently (MW0038). A change one game makes to a global
that every game then reads (MW0048) shows in what it changes. A lone run is the first machine's, so it is unchanged.

What only one player's screen shows is not a difference: text, the camera, music, a frame hidden or given text for one player.

### What it does not cover

- **The game's own bugs**, which no script decides, so a simulator that runs the script right cannot show them: `GetLocationZ` under a terrain deformation
  or on an animated walkable destructable; an upgrade with attack dice above 14; a timer of 0.01 s or less started at the map's start; frames made while a
  player is alt-tabbed, or in `main`; a frame destroyed for everyone that desyncs at random; a location made at the Lua root whose handle id drifts with
  the collector.
- **How the game breaks on `ForGroup` or `ForForce` in a local-player block** is a claim (Hive thread 317486), not measured. Here each call counts as
  a step every game must make alike, so a game that runs one the others don't differs, whatever the callback does.
- **A real `pairs` order and clock.** The layout and clock are made-up differences between the games, not the game's: they show that a map's result
  depends on them, and a table of only a few keys may still come out in the same order. Loops over numbers and strings are the same in every game.
- **Other values only one machine has**: a name or tooltip in the player's language, the keys and mouse, a sound's length, what a frame reads back, the
  camera's fields. They read the same in every game here, so a change made from one is missed.
- **Sync data, a false alarm.** Each game gets only its own player's `BlzSendSyncData`, so a map whose sync event changes the game is reported as a
  desync a step after the data is sent, though the game sends it to everyone.
- **What the players do.** The check gives no orders, chat or keys, so a desync that needs one is not reached.
