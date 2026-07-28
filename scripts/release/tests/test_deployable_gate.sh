#!/usr/bin/env bash
# Integration test for A-PROV-002: prove the gate CAN emit a deployable PASS.
# Builds a temporary CLEAN git repo containing the real GCAE source, runs the real
# clean_build.sh with GCAE_EVIDENCE_OUT pointing OUTSIDE that repo, and requires
# gate_verdict=PASS with clean_before/after=true and the temp repo still clean.
# Then dirties the repo and requires the verdict to fall back to non-deployable.
# Skips cleanly if no .NET SDK is present (e.g. reviewer environment).
set -uo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
LOG="${1:-/dev/stdout}"
PASS=0; FAIL=0; OUTL=()
P(){ PASS=$((PASS+1)); OUTL+=("PASS  $1"); }
F(){ FAIL=$((FAIL+1)); OUTL+=("FAIL  $1"); }

if ! command -v dotnet >/dev/null 2>&1; then
  printf 'A_DEPLOYABLE_GATE_TEST\nSKIP: no .NET SDK on this host (cannot build)\nRESULT: SKIP\n' | tee "$LOG"; exit 0
fi

T="$(mktemp -d)"; repo="$T/repo"; mkdir -p "$repo"
for f in global.json GC.AuctionFlow.sln CLAUDE.md .gitignore; do cp "$ROOT/$f" "$repo/"; done
for d in src/GC.AuctionFlow tests/GC.AuctionFlow.Tests research/optionflow scripts/release; do
  mkdir -p "$repo/$(dirname "$d")"; cp -r "$ROOT/$d" "$repo/$d"; done
mkdir -p "$repo/docs/spec"; cp "$ROOT/docs/spec/KDK_KIM_DAU_KINH_CHUYEN_SAU_OPTIONS.md" "$repo/docs/spec/"
find "$repo" -type d \( -name bin -o -name obj -o -name __pycache__ -o -name .pytest_cache \) -prune -exec rm -rf {} + 2>/dev/null || true
( cd "$repo" && git init -q && git config user.email t@t && git config user.name t && git add -A && git commit -q -m init )

# 1) DEPLOYABLE run: evidence OUTSIDE the repo
ev="$T/evidence"
( GCAE_EVIDENCE_OUT="$ev" bash "$repo/scripts/release/clean_build.sh" ) > "$T/run1.log" 2>&1; rc1=$?
man="$ev/A_BUILD_MANIFEST.json"
v1="$(grep -oE '"gate_verdict": "[A-Z_]+"' "$man" 2>/dev/null | grep -oE 'PASS[A-Z_]*' | head -1)"
[ "$rc1" -eq 0 ] && [ "$v1" = "PASS" ] && P "clean repo + external evidence -> gate_verdict=PASS" || F "clean repo + external evidence -> gate_verdict=PASS (rc=$rc1 v=$v1)"
grep -q '"working_tree_clean_before": true' "$man" 2>/dev/null && P "clean_before=true" || F "clean_before=true"
grep -q '"working_tree_clean_after": true'  "$man" 2>/dev/null && P "clean_after=true"  || F "clean_after=true"
grep -q '"evidence_out_of_repo": true'       "$man" 2>/dev/null && P "evidence_out_of_repo=true" || F "evidence_out_of_repo=true"
[ -z "$(git -C "$repo" status --porcelain)" ] && P "temp repo still clean after deployable build" || F "temp repo still clean after deployable build"

# 2) mutated/dirty tree -> must NOT be deployable
printf '\n<!-- mutate -->\n' >> "$repo/src/GC.AuctionFlow/GC.AuctionFlow.csproj"
ev2="$T/evidence2"
( GCAE_EVIDENCE_OUT="$ev2" bash "$repo/scripts/release/clean_build.sh" ) > "$T/run2.log" 2>&1
v2="$(grep -oE '"gate_verdict": "[A-Z_]+"' "$ev2/A_BUILD_MANIFEST.json" 2>/dev/null | grep -oE 'PASS[A-Z_]*' | head -1)"
[ "$v2" != "PASS" ] && P "dirty tree -> NOT deployable (got '${v2:-none}')" || F "dirty tree -> NOT deployable (got PASS)"

rm -rf "$T"
{ echo "A_DEPLOYABLE_GATE_TEST - integration (temp clean git repo, real build)"; echo "------------------------------------------------------------";
  for l in "${OUTL[@]}"; do echo "$l"; done; echo "------------------------------------------------------------";
  echo "passed=$PASS failed=$FAIL"; echo "RESULT: $([ "$FAIL" -eq 0 ] && echo PASS || echo FAIL)"; } | tee "$LOG"
[ "$FAIL" -eq 0 ]
