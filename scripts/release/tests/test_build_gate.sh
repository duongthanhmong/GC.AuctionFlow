#!/usr/bin/env bash
# Negative test for the Phase A build gate (A-BLD-001).
# Proves the gate ASSERTION library returns non-zero on: missing binary, empty/invalid
# hash, unequal hashes, and regressed test counts -- and zero on valid inputs. Directly
# exercises the same functions clean_build.sh uses, so a green build report is impossible
# when any underlying command fails. Uses only dummy data; no real build required.
set -uo pipefail
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "$HERE/../lib/gate_asserts.sh"
LOG="${1:-/dev/stdout}"
PASS=0; FAILN=0
# expect: run a gate fn, compare its rc to the expected rc (0=should pass, 1=should fail)
expect(){ local want="$1"; shift; local desc="$1"; shift; "$@" >/dev/null 2>&1; local rc=$?; \
  if [ "$rc" -eq "$want" ]; then PASS=$((PASS+1)); echo "PASS  [$desc] rc=$rc (expected $want)"; \
  else FAILN=$((FAILN+1)); echo "FAIL  [$desc] rc=$rc (expected $want)"; fi; }

tmp="$(mktemp -d)"; empty="$tmp/empty"; : > "$empty"; real="$tmp/real"; echo data > "$real"
HEX64=$(printf 'a%.0s' {1..64}); UPPER=$(printf 'A%.0s' {1..64})

{
  echo "A_BUILD_GATE_NEGATIVE_TEST - $(date -u +%Y-%m-%dT%H:%M:%SZ)"
  echo "Proves the gate fails closed. want=1 means the check MUST reject (return non-zero)."
  echo "------------------------------------------------------------------"
  # missing / empty binary  -> MUST fail
  expect 1 "missing binary rejected"        require_nonempty_file "$tmp/nope" bin
  expect 1 "empty binary rejected"          require_nonempty_file "$empty" bin
  expect 0 "real binary accepted"           require_nonempty_file "$real" bin
  # THE fail-open bug: empty hash must be rejected BEFORE any equality compare
  expect 1 "empty hash rejected"            require_sha256 "" h
  expect 1 "short hash rejected"            require_sha256 "abc123" h
  expect 1 "uppercase hash rejected"        require_sha256 "$UPPER" h
  expect 0 "valid sha256 accepted"          require_sha256 "$HEX64" h
  # equality gates
  expect 0 "equal hashes accepted"          assert_eq "$HEX64" "$HEX64" dll
  expect 1 "different hashes rejected"      assert_eq "${HEX64}" "${UPPER}" dll
  expect 1 "two empty NOT treated equal-ok" require_sha256 "" pdbA   # the bug is caught here, not by assert_eq
  # test-count regressions
  expect 1 "dotnet total 1460 rejected"     assert_min 1460 1461 dotnet_total
  expect 0 "dotnet total 1461 accepted"     assert_min 1461 1461 dotnet_total
  expect 1 "dotnet failed>0 rejected"       assert_exact 3 0 dotnet_failed
  expect 1 "python 36 rejected"             assert_exact 36 37 python_total
  expect 0 "python 37 accepted"             assert_exact 37 37 python_total
  # report freshness (A-BLD-004): a fake test that leaves no/old report must be rejected
  now="$(date +%s)"
  expect 1 "missing report rejected"        require_fresh_report "$tmp/noreport.xml" "$now" rep
  stale="$tmp/stale.xml"; echo x > "$stale"; touch -d '2020-01-01' "$stale" 2>/dev/null || true
  expect 1 "stale report rejected"          require_fresh_report "$stale" "$now" rep
  fresh="$tmp/fresh.xml"; echo x > "$fresh"
  expect 0 "fresh report accepted"          require_fresh_report "$fresh" "$((now-5))" rep
  echo "------------------------------------------------------------------"
  echo "checks passed-as-expected=$PASS  behaved-unexpectedly=$FAILN"
  echo "RESULT: $([ "$FAILN" -eq 0 ] && echo PASS || echo FAIL)"
} | tee "$LOG"
rm -rf "$tmp"
[ "$FAILN" -eq 0 ]
