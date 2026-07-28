#!/usr/bin/env python3
"""Negative tests for scripts/release/package_source.py (A-PKG-002).
Proves the packager FAILS CLOSED (non-zero, no false PASS) when run outside a Git
repository, in an empty repository, or in a repository missing mandatory members.
Run: python scripts/release/tests/test_package_source.py [logpath]
"""
import os, sys, subprocess, tempfile, shutil, zipfile

HERE = os.path.dirname(os.path.abspath(__file__))
SCRIPT = os.path.join(HERE, "..", "package_source.py")
REALROOT = os.path.abspath(os.path.join(HERE, "..", "..", ".."))   # real repo root (for real secret_scan.py)
PY = sys.executable
MANDATORY = ["global.json","GC.AuctionFlow.sln","CLAUDE.md",
             "docs/spec/KDK_KIM_DAU_KINH_CHUYEN_SAU_OPTIONS.md",
             "src/GC.AuctionFlow/GC.AuctionFlow.csproj",
             "tests/GC.AuctionFlow.Tests/GC.AuctionFlow.Tests.csproj",
             "scripts/release/clean_build.sh","scripts/release/deploy.ps1",
             "scripts/release/secret_scan.py","scripts/release/package_source.py"]
n_pass = 0; n_fail = 0; lines = []
def check(desc, cond):
    global n_pass, n_fail
    if cond: n_pass += 1; lines.append("PASS  " + desc)
    else:    n_fail += 1; lines.append("FAIL  " + desc)

def run_in(cwd, mode=None):
    env = os.environ.copy()
    if mode is not None: env["GCAE_PACKAGE_MODE"] = mode
    else: env.pop("GCAE_PACKAGE_MODE", None)
    r = subprocess.run([PY, SCRIPT], cwd=cwd, capture_output=True, text=True, env=env)
    return r.returncode

def build_full_repo(base, name):
    d = os.path.join(base, name); os.makedirs(d)
    git(d, "init", "-q"); git(d, "config", "user.email", "t@t"); git(d, "config", "user.name", "t")
    for m in MANDATORY:
        fp = os.path.join(d, m); os.makedirs(os.path.dirname(fp), exist_ok=True)
        if m == "scripts/release/secret_scan.py":
            shutil.copy(os.path.join(REALROOT, "scripts", "release", "secret_scan.py"), fp)
        else:
            open(fp, "w").write("stub\n")
    git(d, "add", "-A"); git(d, "commit", "-q", "-m", "init")
    os.makedirs(os.path.join(d, "docs", "review", "phaseA"), exist_ok=True)
    open(os.path.join(d, "docs", "review", "phaseA", "A_PHASE_A.patch"), "w").write("")
    return d

def git(cwd, *a):
    subprocess.run(["git", *a], cwd=cwd, capture_output=True, text=True)

base = tempfile.mkdtemp()
try:
    # 1) not a git repository -> fail closed
    d1 = os.path.join(base, "nogit"); os.makedirs(d1)
    check("no git repo -> non-zero", run_in(d1) != 0)

    # 2) empty git repo (no commit / no HEAD) -> fail closed
    d2 = os.path.join(base, "emptygit"); os.makedirs(d2)
    git(d2, "init", "-q")
    check("empty repo (no HEAD) -> non-zero", run_in(d2) != 0)

    # 3) git repo with a commit but missing mandatory members -> fail closed
    d3 = os.path.join(base, "partial"); os.makedirs(d3)
    git(d3, "init", "-q"); git(d3, "config", "user.email", "t@t"); git(d3, "config", "user.name", "t")
    open(os.path.join(d3, "README.txt"), "w").write("hello")
    git(d3, "add", "-A"); git(d3, "commit", "-q", "-m", "init")
    check("missing mandatory members -> non-zero", run_in(d3) != 0)

    # 4-10) A-PKG-005: full repo with mandatory members committed.
    d = build_full_repo(base, "full")
    gov = os.path.join(d, "docs", "governance"); os.makedirs(gov, exist_ok=True)
    for g in ("AUTHORITY_ORDER.md", "SUPERSESSION_REGISTER.md", "KDK_CHANGE_CONTROL.md"):
        open(os.path.join(gov, g), "w").write("gov\n")
    zp = os.path.join(d, "docs", "review", "phaseA", "GCAE_PHASE_A_SOURCE_EVIDENCE.zip")

    # review mode: exactly the 3 accepted Round 1B governance files are allowed
    check("review: 3 exact baseline governance accepted", run_in(d, "review") == 0)
    check("review: baseline governance file in archive",
          os.path.exists(zp) and "docs/governance/AUTHORITY_ORDER.md" in zipfile.ZipFile(zp).namelist())

    # an UNAUTHORIZED governance file must be rejected and must not enter the archive
    if os.path.exists(zp): os.remove(zp)
    open(os.path.join(gov, "UNAUTHORIZED.md"), "w").write("nope\n")
    check("review: unauthorized governance file rejected", run_in(d, "review") != 0)
    check("review: unauthorized governance absent from archive",
          (not os.path.exists(zp)) or ("docs/governance/UNAUTHORIZED.md" not in zipfile.ZipFile(zp).namelist()))

    # package mode fails closed
    check("mode 'relase' (typo) rejected", run_in(d, "relase") != 0)
    check("mode 'unknown' rejected", run_in(d, "unknown") != 0)
    check("default mode is release -> rejects untracked", run_in(d, None) != 0)

    # unauthorized untracked under a source root is rejected in review mode too
    open(os.path.join(d, "src", "GC.AuctionFlow", "UNAUTHORIZED_UNTRACKED.txt"), "w").write("x\n")
    check("review: unauthorized src untracked rejected", run_in(d, "review") != 0)
finally:
    shutil.rmtree(base, ignore_errors=True)

lines.append("-"*56); lines.append("passed=%d failed=%d" % (n_pass, n_fail))
lines.append("RESULT: " + ("PASS" if n_fail == 0 else "FAIL"))
out = "\n".join(["A_SOURCE_PACKAGE_TEST - packager fail-closed negative tests", "-"*56] + lines)
print(out)
if len(sys.argv) > 1: open(sys.argv[1], "w", encoding="utf-8").write(out + "\n")
sys.exit(0 if n_fail == 0 else 1)
