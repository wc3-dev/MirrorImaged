# The command line

`mirror <command> ...` exits 0 when it did what it was asked, 1 when a run failed or a test failed, and 2 with the usage when its arguments are wrong.

On a terminal the output is coloured, a spinner turns while a map loads or runs, `mirror coverage` draws a bar chart and `mirror check` a table. Piped or redirected,
the output is plain text with no colour, for scripts and CI to read.

| Command | What it does |
| ------- | ------------ |
| `mirror coverage` | How many natives are at each trust level, and which are Missing |
| `mirror check <map>` | The natives a map's script calls, with their trust levels; exits 1 when one is Missing |
| `mirror run <map> [--seconds n] [--seed n] [--hash n] [--record file] [--input file]` | Runs a Lua or JASS map for n simulated seconds (60) with a seed (0), then prints its log. `--hash n` prints the world's hash every n seconds. `--input` gives the inputs of a recording (or a JSON list of them) at their steps. `--record` saves the run as a recording |
| `mirror replay <recording or .w3g> [--map map] [--hash n]` | Runs a recording again, exactly, and prints the hash at its end (or every n seconds); a game replay (`.w3g`) needs the map it was played on with `--map` |
| `mirror snapshot <map or recording> [--at n] [--back n] [--seed n] [--out file]` | Saves the world after n simulated seconds with what runs it again. `--back n` runs to `--at`, steps back n seconds and saves the world there: the same world as a run forward to that step |
| `mirror compare <snapshot> [--map map]` | Runs the snapshot's run again to its step and lists the parts of the world that differ |
| `mirror desync <map> [--seconds n] [--seed n]` | Runs the map once for each person's slot as that player's own game, and reports where the runs first differ |
| `mirror bench <map> [--seconds n] [--seed n]` | Times a run: how many times faster than real time it goes |
| `mirror fuzz <map> [--seconds n] [--seed n] [--orders n]` | Gives every player's units random legal orders each second and reports script errors |
| `mirror test <file or folder> [--junit file]` | Runs the Lua tests (`*.test.lua`), prints each result, and writes JUnit XML |
| `mirror view <map or recording> [--seconds n] [--every n] [--out file]` | Runs it and saves a page that scrubs through the run in a browser |

## Help

`mirror --help` lists every command. `mirror <command> --help` (or `-h`, or `mirror help <command>`) shows one command and exits 0, wherever on the line it is asked.

## Maps

A `<map>` is one of three, and every command takes each:

| Kind | Example |
| ---- | ------- |
| A map file | `MyMap.w3x`, `MyMap.w3m` |
| A folder map: a folder of that name holding the map's files, as the game also loads | `build/MyMap.w3x/` |
| A map of the installed game, by its name in the game's storage | `"War3.w3mod:Maps/(4)Adrenaline.w3m"` |

Any command that takes a map also takes the seats: `--users 0,1` and `--computers 2,3` name the slots people and computers play, and `--melee true` seats the map's first user slot with a
person and every other with a computer. With none of them, the map's first user slot is a person and its computer slots are computers.

It also takes `--graphics reforged` or `--graphics classic`: the graphics mode the player has chosen in the game's options. The game's
data answers follow it, not the map: a unit's scale and animation speeds, the game camera, a bridge's height under `GetLocationZ` and which
sound files exist, so a script can read different numbers in each mode. Reforged unless given; a recording replays in its own mode.

## Recordings

A recording is a JSON file: the map, the seed, the seats, the inputs each at its step, and the step the run reached. See [recordings](recordings.md).
