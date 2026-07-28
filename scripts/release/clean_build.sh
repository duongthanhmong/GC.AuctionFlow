#!/usr/bin/env bash
# Phase A / A3 - reproducible clean build + regression, FAIL-CLOSED (Round 2).
# Fixes: A-BLD-003 (SDK pin enforced INSIDE each isolated build via a copied global.json),
# A-BLD-004 (test reports deleted before, required fresh after, parsed by an XML parser,
# strict counts), A-PROV-001 (pre/post input manifests incl. ATAS reference hashes; the
# deployable verdict is PASS only from a clean tree, else PASS_FOR_REVIEW_NOT_DEPLOYABLE).
set -Eeuo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
source "$ROOT/scripts/release/lib/gate_asserts.sh"
# A-PROV-002: a DEPLOYABLE build writes ALL evidence OUTSIDE the repo (GCAE_EVIDENCE_OUT),
# so a clean checkout stays clean. Without it, evidence lands in-repo (review mode) and the
# verdict can only be PASS_FOR_REVIEW_NOT_DEPLOYABLE.
OUT="${GCAE_EVIDENCE_OUT:-$ROOT/docs/review/phaseA}"; mkdir -p "$OUT"
OUT_ABS="$(cd "$OUT" && pwd)"
case "$OUT_ABS/" in "$ROOT"/*) IN_REPO_OUT=1;; *) IN_REPO_OUT=0;; esac
CLEAN_BEFORE=$( [ -z "$(git -C "$ROOT" status --porcelain)" ] && echo true || echo false )
PROJ="src/GC.AuctionFlow/GC.AuctionFlow.csproj"
RESULT="$OUT/A_BUILD_GATE_RESULT.txt"; : > "$RESULT"
FAILS=0; fail(){ FAILS=$((FAILS+1)); }
log(){ echo "$*" | tee -a "$RESULT"; }
trap 'echo "UNEXPECTED_ERROR line $LINENO rc=$?" | tee -a "$RESULT"; exit 2' ERR

BUILD_START_UTC="$(date -u +%Y-%m-%dT%H:%M:%SZ)"; START_EPOCH="$(date +%s)"
PIN="$(grep -oE '[0-9]+\.[0-9]+\.[0-9]+' "$ROOT/global.json" | head -1)"
SDK="$(dotnet --version)"
log "SDK pinned=$PIN root-resolved=$SDK"
assert_eq "$SDK" "$PIN" "sdk_pin_root" || fail

# ---- ATAS reference assembly hashes (the three referenced DLLs actually used) ----
AH="${ATAS_HOME:-C:\\Program Files (x86)\\ATAS Platform}"
AHB="$(cygpath "$AH" 2>/dev/null || echo "$AH")"
atas_hash(){ local f="$AHB/$1"; if [ -f "$f" ]; then sha_file "$f"; else echo "ABSENT"; fi; }
ATAS_IND="$(atas_hash ATAS.Indicators.dll)"; ATAS_DF="$(atas_hash ATAS.DataFeedsCore.dll)"; ATAS_OFT="$(atas_hash OFT.Rendering.dll)"
log "ATAS refs: Indicators=$ATAS_IND DataFeeds=$ATAS_DF OFT=$ATAS_OFT"

# ---- input manifest (BEFORE build) ----
make_input_manifest(){
  local out="$1"
  { echo "# gcae input manifest";
    ( cd "$ROOT" && find global.json GC.AuctionFlow.sln CLAUDE.md \
        src/GC.AuctionFlow tests/GC.AuctionFlow.Tests research/optionflow scripts/release \
        docs/spec/KDK_KIM_DAU_KINH_CHUYEN_SAU_OPTIONS.md -type f \
        ! -path '*/bin/*' ! -path '*/obj/*' ! -path '*/__pycache__/*' ! -name '*.pyc' 2>/dev/null \
        ! -path '*/.pytest_cache/*' \
      | sort | xargs sha256sum );
    echo "ATAS.Indicators.dll  $ATAS_IND";
    echo "ATAS.DataFeedsCore.dll  $ATAS_DF";
    echo "OFT.Rendering.dll  $ATAS_OFT";
  } > "$out"
}
make_input_manifest "$OUT/A_INPUT_MANIFEST.sha256"
PRE_INPUT_SHA="$(sha_file "$OUT/A_INPUT_MANIFEST.sha256")"

# ---- provenance env ----
{ echo "build_start_utc=$BUILD_START_UTC"; echo "git_HEAD=$(git -C "$ROOT" rev-parse HEAD)";
  echo "git_branch=$(git -C "$ROOT" rev-parse --abbrev-ref HEAD)"; echo "sdk_pinned=$PIN sdk_root=$SDK";
  echo "os=$(uname -s -r)"; echo "--- git status --short ---"; git -C "$ROOT" status --short; } > "$OUT/A_BUILD_ENV.txt"

# ---- Part 1: two ISOLATED builds, each a repo-shaped root WITH global.json (A-BLD-003) ----
declare -A DLLH PDBH ISDK
for TAG in A B; do
  WD="$(mktemp -d)"
  cp -r "$ROOT/src/GC.AuctionFlow/." "$WD/"; rm -rf "$WD/bin" "$WD/obj"
  cp "$ROOT/global.json" "$WD/global.json"          # pin the SDK inside the isolated root
  ISDK[$TAG]="$( cd "$WD" && dotnet --version )"
  assert_eq "${ISDK[$TAG]}" "$PIN" "sdk_pin_isolated_$TAG" || fail
  rc=0; ( cd "$WD" && dotnet build GC.AuctionFlow.csproj -c Release --nologo ) > "$OUT/A_BUILD_$TAG.log" 2>&1 || rc=$?
  assert_exact "$rc" 0 "build_${TAG}_rc" || fail
  DLL="$WD/bin/Release/net10.0-windows/GC.AuctionFlow.dll"; PDB="${DLL%.dll}.pdb"
  require_nonempty_file "$DLL" "build_${TAG}_dll" || fail; require_nonempty_file "$PDB" "build_${TAG}_pdb" || fail
  DLLH[$TAG]="$(sha_file "$DLL" || echo MISSING)"; PDBH[$TAG]="$(sha_file "$PDB" || echo MISSING)"
  require_sha256 "${DLLH[$TAG]}" "build_${TAG}_dll_sha" || fail; require_sha256 "${PDBH[$TAG]}" "build_${TAG}_pdb_sha" || fail
  # preserve physical outputs for A-EVID-003
  mkdir -p "$OUT/binaries/build-$TAG"; cp "$DLL" "$OUT/binaries/build-$TAG/"; cp "$PDB" "$OUT/binaries/build-$TAG/"
  rm -rf "$WD"
done
log "isolated SDKs: A=${ISDK[A]} B=${ISDK[B]}"

# ---- Part 2: canonical in-repo build ----
rm -rf "$ROOT/src/GC.AuctionFlow/bin" "$ROOT/src/GC.AuctionFlow/obj"
rc=0; ( cd "$ROOT" && dotnet build "$PROJ" -c Release --nologo ) > "$OUT/A_BUILD_CANONICAL.log" 2>&1 || rc=$?
assert_exact "$rc" 0 "canonical_build_rc" || fail
CDLL="$ROOT/src/GC.AuctionFlow/bin/Release/net10.0-windows/GC.AuctionFlow.dll"; CPDB="${CDLL%.dll}.pdb"
require_nonempty_file "$CDLL" "canonical_dll" || fail; require_nonempty_file "$CPDB" "canonical_pdb" || fail
CDLLH="$(sha_file "$CDLL" || echo MISSING)"; CPDBH="$(sha_file "$CPDB" || echo MISSING)"
require_sha256 "$CDLLH" "canonical_dll_sha" || fail
mkdir -p "$OUT/binaries/canonical"; cp "$CDLL" "$OUT/binaries/canonical/"; cp "$CPDB" "$OUT/binaries/canonical/"
WARN="$(grep -oE '[0-9]+ Warning' "$OUT/A_BUILD_CANONICAL.log" | grep -oE '^[0-9]+' | head -1 || echo 999)"
ERRC="$(grep -oE '[0-9]+ Error'   "$OUT/A_BUILD_CANONICAL.log" | grep -oE '^[0-9]+' | head -1 || echo 999)"
assert_exact "${WARN:-999}" 0 "canonical_warnings" || fail
assert_exact "${ERRC:-999}" 0 "canonical_errors" || fail

# ---- tests: delete stale reports, record start, require fresh, parse XML structurally (A-BLD-004) ----
TRX="$OUT/A_DOTNET_TEST.trx"; JUNIT="$OUT/A_PYTHON_TEST.junit.xml"
rm -f "$TRX" "$JUNIT"; TEST_START="$(date +%s)"
rc=0; ( cd "$ROOT" && dotnet test tests/GC.AuctionFlow.Tests/GC.AuctionFlow.Tests.csproj -c Release --nologo \
        --logger "trx;LogFileName=A_DOTNET_TEST.trx" --results-directory "$OUT" ) > "$OUT/A_DOTNET_TEST.log" 2>&1 || rc=$?
assert_exact "$rc" 0 "dotnet_test_rc" || fail
require_fresh_report "$TRX" "$TEST_START" "trx" || fail
read -r ok TT TF TS < <(parse_xml_counts "$TRX" trx)
log "dotnet TRX(xml): total=$TT failed=$TF skipped=$TS parse=$ok"
assert_eq "$ok" "OK" "trx_parse" || fail
assert_exact "${TF:-1}" 0 "dotnet_failed_zero" || fail
assert_exact "${TS:-1}" 0 "dotnet_skipped_zero" || fail
assert_min "${TT:-0}" 1461 "dotnet_total_min" || fail

rc=0; ( cd "$ROOT/research/optionflow" && python -m pytest -q --junitxml="$JUNIT" ) > "$OUT/A_PYTHON_TEST.log" 2>&1 || rc=$?
assert_exact "$rc" 0 "python_test_rc" || fail
require_fresh_report "$JUNIT" "$TEST_START" "junit" || fail
read -r ok PT PF PS < <(parse_xml_counts "$JUNIT" junit)
log "pytest JUnit(xml): total=$PT failed=$PF skipped=$PS parse=$ok"
assert_eq "$ok" "OK" "junit_parse" || fail
assert_exact "${PF:-1}" 0 "python_failed_zero" || fail
assert_exact "${PS:-1}" 0 "python_skipped_zero" || fail
assert_exact "${PT:-0}" 37 "python_total_exact" || fail

# ---- reproducibility equality gates ----
assert_eq "${DLLH[A]}" "${DLLH[B]}" "AB_dll_equal" || fail
assert_eq "${PDBH[A]}" "${PDBH[B]}" "AB_pdb_equal" || fail
assert_eq "$CDLLH" "${DLLH[A]}" "canonical_eq_A_dll" || fail
assert_eq "$CPDBH" "${PDBH[A]}" "canonical_eq_A_pdb" || fail

# ---- post-input manifest must equal pre (source unchanged during build) ----
make_input_manifest "$OUT/A_INPUT_MANIFEST_POST.sha256"
POST_INPUT_SHA="$(sha_file "$OUT/A_INPUT_MANIFEST_POST.sha256")"
assert_eq "$PRE_INPUT_SHA" "$POST_INPUT_SHA" "input_manifest_stable" || fail

log "GATE_FAILS=$FAILS"
if [ "$FAILS" -ne 0 ]; then log "RESULT: FAIL ($FAILS gate(s))"; exit 1; fi

# ---- build manifest (deployable ONLY from a clean tree, before AND after, evidence out-of-repo) ----
BUILD_END_UTC="$(date -u +%Y-%m-%dT%H:%M:%SZ)"
COMMIT="$(git -C "$ROOT" rev-parse HEAD)"
CLEAN_AFTER=$( [ -z "$(git -C "$ROOT" status --porcelain)" ] && echo true || echo false )
if [ "$IN_REPO_OUT" = "0" ] && [ "$CLEAN_BEFORE" = "true" ] && [ "$CLEAN_AFTER" = "true" ]; then
  CLEAN=true; VERDICT="PASS"
else
  CLEAN=false; VERDICT="PASS_FOR_REVIEW_NOT_DEPLOYABLE"
fi
log "clean_before=$CLEAN_BEFORE clean_after=$CLEAN_AFTER evidence_in_repo=$IN_REPO_OUT verdict=$VERDICT"
TRX_SHA="$(sha_file "$TRX")"; JUNIT_SHA="$(sha_file "$JUNIT")"
cat > "$OUT/A_BUILD_MANIFEST.json" <<JSON
{
  "schema": "gcae-build-manifest-v2",
  "commit": "$COMMIT",
  "working_tree_clean": $CLEAN,
  "working_tree_clean_before": $CLEAN_BEFORE,
  "working_tree_clean_after": $CLEAN_AFTER,
  "evidence_out_of_repo": $( [ "$IN_REPO_OUT" = "0" ] && echo true || echo false ),
  "gate_verdict": "$VERDICT",
  "build_start_utc": "$BUILD_START_UTC",
  "build_end_utc": "$BUILD_END_UTC",
  "sdk_pinned": "$PIN",
  "sdk_resolved_root": "$SDK",
  "sdk_resolved_build_A": "${ISDK[A]}",
  "sdk_resolved_build_B": "${ISDK[B]}",
  "configuration": "Release",
  "target_framework": "net10.0-windows",
  "dll_path": "src/GC.AuctionFlow/bin/Release/net10.0-windows/GC.AuctionFlow.dll",
  "dll_sha256": "$CDLLH",
  "pdb_sha256": "$CPDBH",
  "reproducible_dll_sha256": "${DLLH[A]}",
  "reproducible_pdb_sha256": "${PDBH[A]}",
  "pre_input_manifest_sha256": "$PRE_INPUT_SHA",
  "post_input_manifest_sha256": "$POST_INPUT_SHA",
  "atas_indicators_sha256": "$ATAS_IND",
  "atas_datafeeds_sha256": "$ATAS_DF",
  "atas_oft_rendering_sha256": "$ATAS_OFT",
  "dotnet_test_trx_sha256": "$TRX_SHA",
  "python_test_junit_sha256": "$JUNIT_SHA",
  "dotnet_total": ${TT:-0}, "dotnet_failed": ${TF:-0}, "dotnet_skipped": ${TS:-0},
  "python_total": ${PT:-0}, "python_failed": ${PF:-0}, "python_skipped": ${PS:-0}
}
JSON
log "build manifest: commit=$COMMIT clean=$CLEAN verdict=$VERDICT dll=$CDLLH"
log "RESULT: PASS (all gates)"
exit 0
