# Continuous integration

MirrorImaged needs the game's files to run a map, and never ships or downloads them, so tests run on a machine that has Warcraft III installed: a
self-hosted runner.

```yaml
jobs:
  test:
    runs-on: [self-hosted, windows]
    steps:
      - uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1 # v7.0.1
      - uses: wc3-dev/MirrorImaged/.github/actions/mirror-test@main
        with:
          tests: tests
          junit: mirror-junit.xml
```

The action installs the .NET 10 SDK and `MirrorImaged.Cli`, then runs `mirror test tests --junit mirror-junit.xml`. Most CI services and editors read the
JUnit file. The exit code of the step is 1 when a test fails.

| Input | What it is | Default |
|---|---|---|
| `tests` | The Lua test file, or the folder whose `*.test.lua` files are run | `tests` |
| `junit` | Where the JUnit XML of the results is written | `mirror-junit.xml` |
| `version` | The version of `MirrorImaged.Cli` to install | the latest |

`@main` is the action as it is on MirrorImaged's `main` branch, so it changes when that does. To keep one version of it, put a commit's SHA in place
of `main`, and give the tool's `version` too. If the game is not in `C:\Program Files (x86)\Warcraft III` on the runner, set `MIRROR_GAME` in the job's
`env` ([getting started](getting-started.md#where-the-game-is)).

MirrorImaged's own CI runs the tests that need no game on Windows, Linux and macOS, among them one that checks that the state hash of a run is the same
on all three.
