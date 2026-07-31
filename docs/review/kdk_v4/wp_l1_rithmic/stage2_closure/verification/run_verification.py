"""Produce a self-contained, independently checkable verification transcript.

Records the environment (Python, dependency versions, platform), the git working-tree state,
the full test output, and the counts the reviewer asked to have confirmed. Writes the
transcript to disk so it travels with the handover ZIP rather than living only in a terminal.

Counts are RECOUNTED here from the artifacts, not copied from prose - if a number in the
documentation drifts from the files, this disagrees with it.

usage: python verification/run_verification.py
"""
import csv
import hashlib
import io
import json
import os
import platform
import subprocess
import sys

sys.dont_write_bytecode = True

HERE = os.path.dirname(os.path.abspath(__file__))
PKG = os.path.abspath(os.path.join(HERE, ".."))
REPO = os.path.abspath(os.path.join(PKG, "..", "..", "..", "..", ".."))
OUT = []


def say(*a):
    s = " ".join(str(x) for x in a)
    print(s, flush=True)
    OUT.append(s)


def run(cmd, cwd=None):
    p = subprocess.run(cmd, cwd=cwd or REPO, capture_output=True, text=True, shell=False)
    return (p.stdout or "") + (p.stderr or ""), p.returncode


def dep_version(name):
    try:
        from importlib.metadata import version
        return version(name)
    except Exception as e:
        return "UNAVAILABLE (%s)" % type(e).__name__


def main():
    say("=" * 78)
    say("KDK Stage 2 closure - independent verification transcript")
    say("=" * 78)

    say("")
    say("-- environment --")
    say("  python            : %s" % sys.version.replace("\n", " "))
    say("  executable        : %s" % sys.executable)
    say("  platform          : %s %s" % (platform.system(), platform.release()))
    say("  machine           : %s" % platform.machine())
    for dep in ("jsonschema", "referencing", "attrs", "rpds-py"):
        say("  %-17s : %s" % (dep, dep_version(dep)))

    say("")
    say("-- git state --")
    for label, cmd in (("branch", ["git", "rev-parse", "--abbrev-ref", "HEAD"]),
                       ("head", ["git", "rev-parse", "HEAD"]),
                       ("head short", ["git", "log", "--oneline", "-1"])):
        o, _ = run(cmd)
        say("  %-17s : %s" % (label, o.strip()))
    o, _ = run(["git", "status", "--porcelain", "--", PKG])
    dirty = [l for l in o.splitlines() if l.strip()]
    say("  package tree      : %s" % ("CLEAN" if not dirty else "%d modified" % len(dirty)))
    for l in dirty[:12]:
        say("      %s" % l)

    say("")
    say("-- package inventory --")
    files = []
    for dp, dns, fns in os.walk(PKG):
        dns[:] = [d for d in dns if d != "__pycache__"]
        for fn in sorted(fns):
            p = os.path.join(dp, fn)
            rel = os.path.relpath(p, PKG).replace(os.sep, "/")
            if rel.startswith("verification/") and rel.endswith(".txt"):
                continue          # the transcript being written now
            h = hashlib.sha256(open(p, "rb").read()).hexdigest()
            files.append((rel, os.path.getsize(p), h))
    say("  artifacts         : %d" % len(files))
    for rel, size, h in files:
        say("    %-58s %8d  %s" % (rel, size, h[:16]))

    say("")
    say("-- recounted from the artifacts (not copied from prose) --")
    idx = list(csv.DictReader(open(os.path.join(PKG, "EVIDENCE_INDEX.csv"), encoding="utf-8")))
    man = list(csv.DictReader(open(os.path.join(PKG, "CAPABILITY_EVIDENCE_MANIFEST.csv"),
                                   encoding="utf-8")))
    adrs = sorted(f for f in os.listdir(os.path.join(PKG, "adr")) if f.endswith(".md"))
    schemas = sorted(f for f in os.listdir(os.path.join(PKG, "schemas")) if f.endswith(".json"))

    dangling, drifted = [], []
    for r in idx:
        p = os.path.join(REPO, r["artifact"].replace("/", os.sep))
        if not os.path.isfile(p):
            dangling.append(r["evidence_id"])
        elif hashlib.sha256(open(p, "rb").read()).hexdigest() != r["sha256"]:
            drifted.append(r["evidence_id"])

    approved_adrs = []
    for a in adrs:
        t = open(os.path.join(PKG, "adr", a), encoding="utf-8").read()
        if "**Status:** ACCEPTED" in t:
            approved_adrs.append(a)

    reg = os.path.join(REPO, "docs", "review", "kdk_v4", "mrbs_param_recon_r3",
                       "KDK_PARAMETER_CANONICAL_REGISTRY_R3.csv")
    prows = list(csv.DictReader(open(reg, encoding="utf-8")))
    approved_params = [r for r in prows if r["approval_status"] == "Approved"]

    counts = [
        ("ADRs", len(adrs), 7),
        ("ADRs still marked ACCEPTED (must be 0)", len(approved_adrs), 0),
        ("JSON schemas", len(schemas), 6),
        ("capability surfaces", len(man), 32),
        ("EvidenceIds", len(idx), 15),
        ("dangling evidence pointers", len(dangling), 0),
        ("evidence hash mismatches", len(drifted), 0),
        ("registry parameters", len(prows), 134),
        ("registry parameters Approved (must be 0)", len(approved_params), 0),
    ]
    bad = []
    for label, got, want in counts:
        ok = got == want
        if not ok:
            bad.append(label)
        say("  %-42s %6d   expected %-6d %s" % (label, got, want, "OK" if ok else "MISMATCH"))

    say("")
    say("-- test suite --")
    test = os.path.join(PKG, "tests", "test_stage2_closure.py")
    env = dict(os.environ, PYTHONIOENCODING="utf-8", PYTHONDONTWRITEBYTECODE="1")
    p = subprocess.run([sys.executable, test], capture_output=True, text=True, env=env)
    out = (p.stdout or "") + (p.stderr or "")
    for line in out.splitlines():
        say("  " + line)
    say("  exit code: %d" % p.returncode)

    # Count only per-check result lines. A naive out.count(" PASS") also matches the
    # summary line "RESULT: PASS - 47/47 checks" and reported 48.
    def is_result_line(line):
        return line.startswith("  ") and (line.rstrip().endswith("PASS")
                                          or " PASS " in line
                                          or line.rstrip().endswith("FAIL")
                                          or " FAIL " in line)

    passes = sum(1 for l in out.splitlines()
                 if is_result_line(l) and ("PASS" in l and "FAIL" not in l))
    fails = sum(1 for l in out.splitlines() if is_result_line(l) and "FAIL" in l)
    summary = [l for l in out.splitlines() if l.startswith("RESULT:")]
    negatives = 0
    in_neg = False
    for line in out.splitlines():
        if line.startswith("-- fail-closed rules REJECT"):
            in_neg = True
            continue
        if in_neg and line.startswith("--"):
            in_neg = False
        if in_neg and is_result_line(line):
            negatives += 1

    say("")
    say("-- confirmations requested --")
    conf = [
        ("47/47 tests pass", passes == 47 and fails == 0 and p.returncode == 0
         and summary == ["RESULT: PASS - 47/47 checks"],
         "%d pass / %d fail / exit %d / summary %s"
         % (passes, fails, p.returncode, summary)),
        ("21 negative fail-closed cases", negatives == 21, "counted %d" % negatives),
        ("32 capability surfaces", len(man) == 32, str(len(man))),
        ("15 EvidenceIds", len(idx) == 15, str(len(idx))),
        ("0 dangling evidence pointers", not dangling, str(dangling)),
        ("evidence hashes match", not drifted, str(drifted)),
        ("no ADR claims ACCEPTED", not approved_adrs, str(approved_adrs)),
        ("0 registry parameters Approved", not approved_params, str(len(approved_params))),
    ]
    allok = True
    for label, ok, detail in conf:
        allok = allok and ok
        say("  %-36s %s   %s" % (label, "CONFIRMED" if ok else "NOT CONFIRMED", detail))

    say("")
    say("RESULT: %s" % ("ALL CONFIRMED" if allok and not bad else "NOT FULLY CONFIRMED"))

    tp = os.path.join(HERE, "VERIFICATION_TRANSCRIPT.txt")
    with open(tp, "w", encoding="utf-8", newline="\n") as fh:
        fh.write("\n".join(OUT) + "\n")
    print()
    print("transcript written: %s" % tp)
    return 0 if (allok and not bad) else 1


if __name__ == "__main__":
    raise SystemExit(main())
