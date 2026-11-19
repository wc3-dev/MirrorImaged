# JASS and TypeScript maps

The tests do not care which language the map is written in. A map whose script is `war3map.j` is turned into Lua with War3Net's transpiler, as the
game's own `blizzard.j` is, and runs the same way. A TypeScript map is built with TypeScriptToLua into a `war3map.lua` first.

## JASS

`examples/jass/gold/war3map.j` is the gold map in JASS:

```jass
function PayPlayer takes nothing returns nothing
    call SetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD, GetPlayerState(Player(0), PLAYER_STATE_RESOURCE_GOLD) + 10)
endfunction
```

Its test is a Lua test like any other; the only change is how `BJDebugMsg` shows text (`gold 30`, the words joined by a space, where `print`
separates them with a tab):

```lua
test("the player is paid every 2 seconds, in JASS", function()
  local sim = mirror.load("gold.w3x")
  sim:advance(7)
  sim:say(0, "-gold")
  sim:advance(0.1)
  assert_contains(sim:log(), "gold 30")
end)
```

A JASS map that calls a native or a `blizzard.j` function MirrorImaged cannot run says so by name; `mirror check Map.w3x` lists them before a run.

## TypeScript

`examples/typescript/gold` is the gold map in TypeScript: `src/main.ts`, the natives it calls declared in `src/natives.d.ts`, and a
`package.json` that pins TypeScriptToLua and TypeScript. `npm ci` and then `npm run build` bundle it into `war3map.lua`:

```json
{
  "compilerOptions": { "target": "ESNext", "lib": ["ESNext"], "strict": true, "types": [] },
  "tstl": { "luaTarget": "5.3", "luaBundle": "war3map.lua", "luaBundleEntry": "src/main.ts", "noImplicitSelf": true, "noHeader": true },
  "include": ["src"]
}
```

`noImplicitSelf` keeps the calls to natives as they are written, with no `self` argument added. `config` and `main` are plain functions in
`main.ts`, so the bundle makes them globals, where the game looks for them. A larger map takes the natives' types from a package for them instead of
declaring its own.

Build the `.w3x` from `war3map.lua` as you always do (War3Net, a map builder or the World Editor); its test is the Lua test of the Lua example. The
suite compiles the example this way and runs the test; without Node.js (`npm` on the path) that test is skipped.
