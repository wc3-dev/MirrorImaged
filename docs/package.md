# MirrorImaged

MirrorImaged is a headless Warcraft III runtime for map makers who test their maps. It loads a `.w3x`, runs its Lua or
JASS script against C# copies of the game's natives on a simulated world and clock, and lets a test give orders, input
and chat, read what happened and check it. No game runs. The same map, seed and input give the same result.

It reads `blizzard.j` and the game's data from your Warcraft III: Reforged install, and never ships or downloads game
files. It looks in `C:\Program Files (x86)\Warcraft III`; set `MIRROR_GAME` to the game's folder if it is elsewhere.

## The packages

- `MirrorImaged.Cli`, the `mirror` tool: `dotnet tool install --global MirrorImaged.Cli`.
- `MirrorImaged`, the C# test API (`Simulation`), for xUnit, NUnit or any other test framework.
- `MirrorImaged.TestAdapter`, which lets `dotnet test`, Rider and Visual Studio find and run `*.test.lua` files.
- `MirrorImaged.Data`, `.Lua`, `.Natives` and `.World`, the parts the others are built from. Their API may change in
  any release: reference `MirrorImaged` instead.

The Lua library ships for win-x64, linux-x64 and osx-arm64.

## A first test

```lua
test("the player is paid every 2 seconds", function()
  local sim = mirror.load("gold.w3x")
  sim:advance(7)
  sim:say(0, "-gold")
  sim:advance(0.1)
  assert_contains(sim:log(), "gold\t30")
end)
```

Run it with `mirror test gold.test.lua`.

## More

The guide, with every command, the C# API and how far each native matches the game, is at
https://github.com/wc3-dev/MirrorImaged/tree/main/docs/guide.
