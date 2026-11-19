# The viewer

```bash
mirror view MyMap.w3x --seconds 120 --every 0.5 --out run.html
```

runs the map, samples its units every half second, and writes one HTML file that holds everything: open it in any browser, with no server and no network.
`mirror view run.json` does the same for a [recording](recordings.md).

The page shows:

- the map from above, with its pathing grid (cells nothing walks on are dark) and the regions the map made, each outlined and tinted;
- every unit as a circle in its player's colour, with a life bar when it is hurt; click a unit to inspect it (name, id, type, player, place, life and order);
- every missile while it flies, as a dot with a tail in its player's colour;
- a timeline to scrub, with play and pause, one frame back and forward, and speeds from 1x to 16x; units glide between frames;
- the log, with the lines up to the time shown; click a line to go to its time.

A missile is kept as when and where it left and landed, whatever `--every` is, and drawn on the straight line between: one that turned after a walking
target is drawn a little off its real way. Most flights are shorter than half a second, so a frame you stop on shows few of them: play, or click a log
line, to see them fly. Only attacks with a missile and Storm Bolt fly; the other spells' missiles are not modelled.

The ground's height is not drawn.
