**MirrorImaged** is headless Warcraft III: Reforged for automated testing.

MirrorImaged loads `.w3x` archives (aka. maps), runs its script against C# copies of the game natives on a simulated clock, and lets tests give orders, input, read the state and check results. The copied game are built from in-game probing.

## Progress `███████████████████░` 97%

| #                             | Description                                                                                        | Progress                               |
| ----------------------------- | -------------------------------------------------------------------------------------------------- | -------------------------------------- |
| 0. Foundation                 | Repository, CI, natives gen, Lua, loading maps, main runner                                        | `████████████████████` 100% (6 of 6)   |
| 1. Core                       | Triggers, threads, timers, hashtables, strings, players, files, cache, handles, JASS               | `████████████████████` 100% (10 of 10) |
| 2. Data and units             | Map files, game data, units, heroes, items, destructables, damage, groups, regions, unit abilities | `████████████████████` 100% (11 of 11) |
| 3. Orders, movement, combat   | Orders, pathfinding, combat, buildings, training, research, harvesting                             | `████████████████████` 100% (7 of 7)   |
| 4. Abilities and buffs        | Casting, ability effects, buffs and auras                                                          | `███████████████░░░░░` 75% (2 of 3)    |
| 5. Presentation and interface | Cameras, sounds, visual effects, text, frames, dialogs, multiboards                                | `████████████████████` 100% (4 of 4)   |
| 6. Players, input, visibility | Chat, keys, mouse, selection, sync, leaving, fow, desyncs                                          | `████████████████████` 100% (8 of 8)   |
| 7. AI                         | `common.ai`, melee/campaign AI, creeps                                                             | `████████████████████` 100%            |
| 8. Test tools                 | C# API, Lua API, commands, snapshots, recordings                                                   | `████████████████████` 100% (5 of 5)   |
| 9. Viewer                     | Webapp for watching/scrubbing recordings                                                           | `████████████████████` 100%            |
| 10. Accuracy and speed        | More probes, whole-corpus runs, benchmarks, linux/macOS                                            | `████████████████████` 100%            |
| 11. Docs and release          | docs, guides, examples, 1.0 release                                                                | `██████████████████░░` 90%             |

### Natives `████████████████████` 100%

| Level    | Meaning                                                 | Count | Share                      |
| -------- | ------------------------------------------------------- | ----- | -------------------------- |
| Exact    | Matches the game on every case a probe measured         | 463   | `█████░░░░░░░░░░░░░░░` 25% |
| Close    | A documented model, not fully measured                  | 1,016 | `███████████░░░░░░░░░` 55% |
| Recorded | Changes nothing a map can read back; the call is logged | 356   | `████░░░░░░░░░░░░░░░░` 19% |
| Missing  | Stops the run with the native's name                    | 0     | `░░░░░░░░░░░░░░░░░░░░` 0%  |
