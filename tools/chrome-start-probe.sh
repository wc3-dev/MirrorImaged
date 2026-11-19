#!/usr/bin/env bash
# Temporary: starts headless Chrome as tests/MirrorImaged.Tests/Browser.cs does, 40 times, and says how long each took to write its debugging port.
# LOAD=suite runs the test suite beside it (the browser tests left out), as CI's test step does. EXTRA holds switches to try.
set -u
chrome=$(command -v google-chrome || command -v chromium)
echo "browser: $chrome, $("$chrome" --version)"
echo "cores: $(nproc); DBUS_SESSION_BUS_ADDRESS='${DBUS_SESSION_BUS_ADDRESS-<unset>}'; DISPLAY='${DISPLAY-<unset>}'; XDG_RUNTIME_DIR='${XDG_RUNTIME_DIR-<unset>}'"
echo "extra switches: '${EXTRA:-}'"
if [ "${LOAD:-idle}" = suite ]; then
  dotnet test MirrorImaged.slnx --no-build --filter "Needs!=Game&FullyQualifiedName!~ViewerBrowser" > suite.log 2>&1 &
  suite=$!
  sleep 8
fi
times=()
slow=0
for i in $(seq 1 40); do
  profile=$(mktemp -d)
  start=$(date +%s%N)
  # shellcheck disable=SC2086
  "$chrome" --headless=new --disable-gpu --no-sandbox --no-first-run --disable-dev-shm-usage --remote-debugging-port=0 \
    --enable-logging=stderr --v=1 ${EXTRA:-} --user-data-dir="$profile" about:blank > "chrome-$i.log" 2>&1 &
  pid=$!
  took=-1
  while [ $(( ($(date +%s%N) - start) / 1000000 )) -lt 60000 ]; do
    if [ -s "$profile/DevToolsActivePort" ]; then took=$(( ($(date +%s%N) - start) / 1000000 )); break; fi
    sleep 0.05
  done
  kill -9 "$pid" 2> /dev/null; pkill -9 -f -- "--user-data-dir=$profile" 2> /dev/null; wait "$pid" 2> /dev/null
  times+=("$took")
  echo "start $i: $( [ "$took" -lt 0 ] && echo 'no port in 60 s' || echo "$took ms")$( [ "${suite-}" ] && kill -0 "$suite" 2> /dev/null && echo ' (suite running)')"
  if [ "$took" -lt 0 ] || [ "$took" -gt 5000 ]; then
    slow=$((slow + 1))
    echo "  --- its log, the lines with a gap of a second or more before them, and its last lines:"
    awk -F'[/:]' '/^\[[0-9]+:[0-9]+:[0-9]+\/[0-9]+\.[0-9]+/ { t = substr($4, 1, 2) * 3600 + substr($4, 3, 2) * 60 + substr($4, 5); if (last && t - last >= 1) printf "  +%.1fs %s\n", t - last, $0; last = t }' "chrome-$i.log" | cut -c1-260 | head -20
    tail -n 6 "chrome-$i.log" | cut -c1-260 | sed 's/^/  last: /'
  fi
  rm -rf "$profile"
done
sorted=$(printf '%s\n' "${times[@]}" | sort -n)
echo "starts: ${#times[@]}; over 5 s or none: $slow; least $(echo "$sorted" | head -1) ms, median $(echo "$sorted" | sed -n 20p) ms, most $(echo "$sorted" | tail -1) ms"
if [ "${suite-}" ]; then kill "$suite" 2> /dev/null; grep -E 'Passed!|Failed!' suite.log; fi
