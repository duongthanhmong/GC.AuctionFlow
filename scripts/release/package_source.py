#!/usr/bin/env python3
"""Phase A / A-PKG-003 source packaging (fail-closed, intended-source-only).
The package is the INTENDED GCAE source set = files tracked at the named baseline PLUS the
authorized Phase A additions (global.json + scripts/release/**, which must appear in
A_PHASE_A.patch). It does NOT sweep in arbitrary untracked files, so OAC source/tests,
pre-existing unrelated untracked docs, historical logs and audit packages are excluded.
Every packaged file must be tracked at baseline OR present in A_PHASE_A.patch. Fails closed
outside git / empty repo / missing mandatory members / unreadable archive.

Outputs (under docs/review/phaseA/): GCAE_PHASE_A_SOURCE_EVIDENCE.zip,
A_SOURCE_PACKAGE_MANIFEST.sha256, A_SOURCE_PACKAGE_SCAN.txt (incl. scope report).
"""
import os, sys, re, subprocess, zipfile, hashlib, importlib.util

FORBIDDEN = re.compile(r"(^|/)(bin|obj|__pycache__|\.venv|\.vs|\.idea|node_modules|\.pytest_cache)/"
                       r"|\.(pyc|pdb|dll|exe|zip|pfx|db|db-wal|db-shm|db-journal|spool)$"
                       r"|(^|/)\.env$|rithmic.*\.env$|(^|/)artifacts/optionflow/", re.I)
# A-PKG-004: explicit INCLUDE-root allowlist (not a broad tracked sweep). Only these roots are
# the intended KDK-governed, one-solution GCAE source. Everything else (artifacts/build, legacy,
# docs/legacy, OAC, docs/review, reference, caches) is excluded by omission.
INCLUDE_EXACT = {"CLAUDE.md","global.json","GC.AuctionFlow.sln",".gitignore",".gitattributes"}
INCLUDE_ROOTS = ("docs/governance/","docs/spec/","docs/implementation/",
                 "src/GC.AuctionFlow/","tests/GC.AuctionFlow.Tests/",
                 "research/optionflow/","scripts/release/","config/")
def included(f): return (f in INCLUDE_EXACT) or any(f.startswith(p) for p in INCLUDE_ROOTS)
MANDATORY = ["global.json","GC.AuctionFlow.sln","CLAUDE.md",
             "docs/spec/KDK_KIM_DAU_KINH_CHUYEN_SAU_OPTIONS.md",
             "src/GC.AuctionFlow/GC.AuctionFlow.csproj",
             "tests/GC.AuctionFlow.Tests/GC.AuctionFlow.Tests.csproj",
             "scripts/release/clean_build.sh","scripts/release/deploy.ps1",
             "scripts/release/secret_scan.py","scripts/release/package_source.py"]

def run(a): return subprocess.run(a, capture_output=True, text=True)

r = run(["git", "rev-parse", "--show-toplevel"])
if r.returncode != 0 or not r.stdout.strip():
    print("A_SOURCE_PACKAGE_SCAN\nFATAL: not inside a Git repository\nA-ACC-02 source archive acceptance: FAIL")
    sys.exit(1)
ROOT = r.stdout.strip()
OUT = os.path.join(ROOT, "docs", "review", "phaseA")
ZIP = os.path.join(OUT, "GCAE_PHASE_A_SOURCE_EVIDENCE.zip")
MAN = os.path.join(OUT, "A_SOURCE_PACKAGE_MANIFEST.sha256")
# A-PKG-005: mode is fail-closed. Default = release (safest); review must be requested explicitly.
MODE = os.environ.get("GCAE_PACKAGE_MODE", "release").lower()
_VALID_MODES = ("release", "review")
SCAN = os.path.join(OUT, {"review": "A_SOURCE_PACKAGE_REVIEW_SCAN.txt",
                          "release": "A_SOURCE_PACKAGE_RELEASE_SCAN.txt"}.get(MODE, "A_SOURCE_PACKAGE_SCAN.txt"))
PATCH = os.path.join(OUT, "A_PHASE_A.patch")

def die(msg):
    os.makedirs(OUT, exist_ok=True)
    open(SCAN, "w", encoding="utf-8").write("FATAL: " + msg + "\nA-ACC-02: FAIL\n")
    print("A_SOURCE_PACKAGE_SCAN\nFATAL: " + msg + "\nA-ACC-02 source archive acceptance: FAIL")
    sys.exit(1)

def sha256(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for b in iter(lambda: f.read(65536), b""): h.update(b)
    return h.hexdigest()

def load_scanner():
    spec = importlib.util.spec_from_file_location("secret_scan", os.path.join(ROOT, "scripts", "release", "secret_scan.py"))
    m = importlib.util.module_from_spec(spec); spec.loader.exec_module(m); return m

def main():
    os.makedirs(OUT, exist_ok=True)
    if run(["git", "-C", ROOT, "rev-parse", "HEAD"]).returncode != 0: die("no valid HEAD")
    tr = run(["git", "-C", ROOT, "ls-files"])                       # TRACKED ONLY (baseline)
    if tr.returncode != 0: die("git ls-files failed")
    tracked = set(p.strip() for p in tr.stdout.splitlines() if p.strip())
    ot = run(["git", "-C", ROOT, "ls-files", "-o", "--exclude-standard"])
    untracked = set(p.strip() for p in ot.stdout.splitlines() if p.strip())
    # A-PKG-005: every packaged file must be AUTHORIZED, not merely inside an allowed root.
    # release mode: tracked-only, fail if ANY untracked file sits under an included source root.
    # review  mode (pre-commit, explicitly requested): untracked allowed ONLY if it is in A_PHASE_A.patch or
    #          under the accepted Round 1B baseline root (docs/governance/).
    if MODE not in _VALID_MODES:
        die("invalid GCAE_PACKAGE_MODE '%s' (allowed exactly: release, review)" % MODE)
    patch_files = set()
    if os.path.exists(PATCH):
        for ln in open(PATCH, encoding="utf-8", errors="replace"):
            m = re.match(r"^\+\+\+ b/(.+)$", ln.strip())
            if m: patch_files.add(m.group(1))
    # EXACT accepted Round 1B baseline file set (not a directory prefix)
    R1B_BASELINE_FILES = {"docs/governance/AUTHORITY_ORDER.md",
                          "docs/governance/SUPERSESSION_REGISTER.md",
                          "docs/governance/KDK_CHANGE_CONTROL.md"}
    def review_authorized(f): return (f in patch_files) or (f in R1B_BASELINE_FILES)
    inc = lambda f: included(f) and not FORBIDDEN.search(f) and os.path.isfile(os.path.join(ROOT, f))
    cand_tracked   = sorted(f for f in tracked   if inc(f))
    cand_untracked = sorted(f for f in untracked if inc(f))
    if MODE == "release":
        if cand_untracked:
            die("release mode: untracked file(s) under included source roots: " + ", ".join(cand_untracked[:8]))
        intended = cand_tracked
    else:
        unauthorized = [f for f in cand_untracked if not review_authorized(f)]
        if unauthorized:
            die("review mode: unauthorized untracked file(s) (not in A_PHASE_A.patch / Round1B baseline): " + ", ".join(unauthorized[:8]))
        intended = sorted(set(cand_tracked) | set(cand_untracked))
    if not intended: die("zero intended source files")
    missing = [m for m in MANDATORY if m not in intended]
    if missing: die("missing mandatory members: " + ", ".join(missing))

    if os.path.exists(ZIP): os.remove(ZIP)
    with zipfile.ZipFile(ZIP, "w", zipfile.ZIP_DEFLATED) as z:
        for f in intended: z.write(os.path.join(ROOT, f), arcname=f)
    try:
        with zipfile.ZipFile(ZIP) as z:
            if z.testzip() is not None: die("produced archive corrupt")
            names = z.namelist()
            with open(MAN, "w", encoding="utf-8") as mf:
                mf.write("# GCAE Phase A source package manifest\n")
                for n in names: mf.write(hashlib.sha256(z.read(n)).hexdigest() + "  " + n + "\n")
    except zipfile.BadZipFile:
        die("produced archive not a valid zip")

    bad = [n for n in names if FORBIDDEN.search(n) or (not included(n)) or "\\" in n]
    ss = load_scanner(); findings = []; ss.scan_zip(ZIP, findings)
    ok = (not bad) and (not findings) and all(m in names for m in MANDATORY)
    roots = sorted(set((f.split("/", 1)[0] if "/" in f else f) for f in intended))
    n_oac = sum(1 for n in names if "Oac." in n or n.startswith(("src/Oac","tests/Oac.Core.Tests")))
    n_legacy = sum(1 for n in names if n.startswith(("legacy/","docs/legacy/")))
    n_artlog = sum(1 for n in names if n.startswith("artifacts/build/"))
    n_gov = sum(1 for n in names if n.startswith("docs/governance/"))
    lines = ["A_SOURCE_PACKAGE_SCAN (intended-source-only, explicit include roots)",
             f"package_mode: {MODE}  (release=tracked-only, fails on any untracked source; review=tracked + A_PHASE_A.patch + EXACT Round1B baseline files)",
             f"archive: docs/review/phaseA/{os.path.basename(ZIP)}",
             f"archive_sha256: {sha256(ZIP)}",
             f"entries: {len(names)}",
             f"include roots: {', '.join(INCLUDE_ROOTS)} + {sorted(INCLUDE_EXACT)}",
             f"included top-level: {', '.join(roots)}",
             f"mandatory members present: {sum(1 for m in MANDATORY if m in names)}/{len(MANDATORY)}",
             f"OAC entries: {n_oac}   legacy entries: {n_legacy}   artifacts/build logs: {n_artlog}",
             f"governance docs: {n_gov}",
             f"forbidden/not-included/backslash entries: {len(bad)} {bad[:6]}",
             f"credential/unreadable findings: {len(findings)}"]
    for x in findings: lines.append("  ! " + x)
    lines.append("A-ACC-02 source archive acceptance: " + ("PASS" if ok else "FAIL"))
    report = "\n".join(lines); print(report); open(SCAN, "w", encoding="utf-8").write(report + "\n")
    return 0 if ok else 1

if __name__ == "__main__":
    sys.exit(main())
