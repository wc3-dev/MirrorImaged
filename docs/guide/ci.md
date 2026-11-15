# Continuous integration

MirrorImaged needs the game's files to run a map, and never ships or downloads them, so tests run on a machine that has Warcraft III installed: a
self-hosted runner.

```yaml
jobs:
  test:
    runs-on: [self-hosted, windows]
    steps:
      - uses: actions/checkout@v4
      - uses: wc3-dev/mirror-imaged/.github/actions/mirror-test@main
        with:
          tests: tests
          junit: mirror-junit.xml
```

The action installs the .NET 10 SDK and `MirrorImaged.Cli`, then runs `mirror test tests --junit mirror-junit.xml`. Most CI services and editors read the
JUnit file. The exit code of the step is 1 when a test fails.

MirrorImaged's own CI runs the tests that need no game on Windows, Linux and macOS, among them one that checks that the state hash of a run is the same
on all three.
