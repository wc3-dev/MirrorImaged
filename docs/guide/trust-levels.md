# Trust levels

Every native in the game's `common.j` and `common.ai` has a trust level, and none is left out. `mirror coverage` prints the counts and the Missing ones.

| Level | Meaning |
| ----- | ------- |
| Exact | Matches the game on every case a probe measured |
| Close | A documented model, not fully measured |
| Recorded | Changes nothing a map can read back; the call is logged |
| Missing | Stops the run with the native's name |

No native of `common.j` and `common.ai` is Missing now, but the level stays: a native a later game patch adds is Missing until it is written, and `SimulationOptions.TreatAsMissing` (or the `MIRROR_MISSING` environment variable, names separated by commas) runs any native as Missing to see how a map copes. A Missing native stops the run: `Advance` throws a `LuaException` and `Simulation.Stopped` says which native it was, so a test never passes by
doing nothing. A run's trust is the lowest level of the natives it calls; `mirror check Map.w3x` lists them.

The facts a level rests on are in `docs/facts.md` and `docs/game-facts.md`; each native's attribute names its fact, and each decision made along the
way is in `docs/decisions.md` with a D-number.

## What Close means for combat, orders and the AI

The unit engine ticks 32 times a second: units walk along paths found on a grid of 32 (buildings block, trees and other units do not), attack at their
damage point, acquire targets, build, train, research, harvest and cast. The computer players run the game's own AI scripts (`common.ai`, `human.ai`...)
against these natives. These are models: they pass the facts that were measured, and a long game will not match the real game move for move.
