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

In the game each player runs the map on their own machine. A map that changes the game from something only one player's game knows (`GetLocalPlayer()`)
makes the games differ, and the players disconnect. `mirror desync MyMap.w3x --seconds 120` runs the map once for each slot a person plays, with that slot as
the local player, and compares the worlds each second:

```
Desync at step 4096 (4 s): player 0 differs
  player 0's game: 9F2C...
  player 1's game: 51AB...
```

The part says where the games differ first: the time, a player, or a unit. The first line of the log that differs is printed too. A map with one user slot
has nothing to compare. `Desync.Check(map, time)` is the same check from C#.
