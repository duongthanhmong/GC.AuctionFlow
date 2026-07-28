#!/usr/bin/env bash
# Phase A release-gate assertion library (fail-closed).
# Sourced by clean_build.sh AND by tests/test_build_gate.sh so the gate logic is
# exercised directly by the negative test. Every function returns non-zero on failure
# and prints a machine-readable line. No function ever masks a failure.

# require a file to exist and be non-empty
require_nonempty_file() {
  local path="$1" label="${2:-$1}"
  if [ ! -f "$path" ]; then echo "GATE_FAIL require_nonempty_file: MISSING $label ($path)"; return 1; fi
  if [ ! -s "$path" ]; then echo "GATE_FAIL require_nonempty_file: EMPTY $label ($path)"; return 1; fi
  echo "GATE_OK require_nonempty_file: $label"; return 0
}

# require a string to be exactly 64 lowercase hex chars (a real sha256)
require_sha256() {
  local h="$1" label="${2:-hash}"
  if printf '%s' "$h" | grep -Eq '^[0-9a-f]{64}$'; then echo "GATE_OK require_sha256: $label"; return 0; fi
  echo "GATE_FAIL require_sha256: NOT_A_SHA256 $label ('${h:0:16}...' len=${#h})"; return 1
}

# require two values to be equal
assert_eq() {
  local a="$1" b="$2" label="${3:-eq}"
  if [ "$a" = "$b" ]; then echo "GATE_OK assert_eq: $label"; return 0; fi
  echo "GATE_FAIL assert_eq: $label ('$a' != '$b')"; return 1
}

# require an integer to be >= a minimum
assert_min() {
  local val="$1" min="$2" label="${3:-min}"
  if ! printf '%s' "$val" | grep -Eq '^[0-9]+$'; then echo "GATE_FAIL assert_min: NOT_INT $label ('$val')"; return 1; fi
  if [ "$val" -ge "$min" ]; then echo "GATE_OK assert_min: $label ($val>=$min)"; return 0; fi
  echo "GATE_FAIL assert_min: $label ($val<$min)"; return 1
}

# require an integer to equal an exact expected value
assert_exact() {
  local val="$1" want="$2" label="${3:-exact}"
  if ! printf '%s' "$val" | grep -Eq '^[0-9]+$'; then echo "GATE_FAIL assert_exact: NOT_INT $label ('$val')"; return 1; fi
  if [ "$val" -eq "$want" ]; then echo "GATE_OK assert_exact: $label ($val==$want)"; return 0; fi
  echo "GATE_FAIL assert_exact: $label ($val!=$want)"; return 1
}

# sha256 of a file (fails loudly if the file is absent)
sha_file() {
  local p="$1"
  [ -f "$p" ] || { echo "" ; return 1; }
  sha256sum "$p" | cut -d' ' -f1
}

# require a report file to exist, be non-empty, AND be newer than a start epoch (freshness)
require_fresh_report() {
  local path="$1" start="$2" label="${3:-$1}"
  if [ ! -s "$path" ]; then echo "GATE_FAIL require_fresh_report: MISSING/EMPTY $label"; return 1; fi
  local mt; mt="$(stat -c %Y "$path" 2>/dev/null || echo 0)"
  if [ "$mt" -lt "$start" ]; then echo "GATE_FAIL require_fresh_report: STALE $label (mtime $mt < start $start)"; return 1; fi
  echo "GATE_OK require_fresh_report: $label"; return 0
}

# STRUCTURAL XML parse (not grep). echo "OK total failed skipped" | "ERR ..." on parse failure.
# kind = trx | junit
parse_xml_counts() {
  python - "$1" "$2" <<'PY' | tr -d '\r'
import sys, xml.etree.ElementTree as ET
f, kind = sys.argv[1], sys.argv[2]
try:
    root = ET.parse(f).getroot()
except Exception:
    print("ERR 0 1 0"); sys.exit(0)
def tagend(e, name): return e.tag.split('}')[-1] == name
if kind == "junit":
    ts = root if tagend(root, "testsuite") else next((e for e in root.iter() if tagend(e, "testsuite")), None)
    if ts is None: print("ERR 0 1 0"); sys.exit(0)
    total = int(ts.get("tests", 0)); failed = int(ts.get("failures", 0)) + int(ts.get("errors", 0)); skipped = int(ts.get("skipped", 0))
else:
    c = next((e for e in root.iter() if tagend(e, "Counters")), None)
    if c is None: print("ERR 0 1 0"); sys.exit(0)
    total = int(c.get("total", 0)); failed = int(c.get("failed", 0)); skipped = int(c.get("notExecuted", 0))
print("OK %d %d %d" % (total, failed, skipped))
PY
}
