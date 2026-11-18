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
| `mirror snapshot <map or recording> [--at n] [--seed n] [--out file]` | Saves the world after n simulated seconds with what runs it again |
| `mirror compare <snapshot> [--map map]` | Runs the snapshot's run again to its step and lists the parts of the world that differ |
| `mirror desync <map> [--seconds n] [--seed n]` | Runs the map once for each person's slot as that player's own game, and reports where the runs first differ |
| `mirror bench <map> [--seconds n] [--seed n]` | Times a run: how many times faster than real time it goes |
| `mirror fuzz <map> [--seconds n] [--seed n] [--orders n]` | Gives every player's units random legal orders each second and reports script errors |
| `mirror test <file or folder> [--junit file]` | Runs the Lua tests (`*.test.lua`), prints each result, and writes JUnit XML |
| `mirror view <map or recording> [--seconds n] [--every n] [--out file]` | Runs it and saves a page that scrubs through the run in a browser |

A recording is a JSON file: the map, the seed, the seats, the inputs each at its step, and the step the run reached. See [recordings](recordings.md).
