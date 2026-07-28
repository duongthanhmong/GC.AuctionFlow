#!/usr/bin/env python3
"""Phase A / A-EVID-004 evidence consistency validator (fail-closed).
Checks that the final Phase A evidence is internally coherent - no stale PASS artifact
beside a non-deployable final manifest, report counts equal the machine-readable logs,
and any deploy manifest is bound to the final build manifest.

Checks:
  1. A_BUILD_MANIFEST.json exists; gate_verdict in {PASS, PASS_FOR_REVIEW_NOT_DEPLOYABLE}.
  2. TRX counts == manifest dotnet_total/failed; JUnit counts == manifest python_total/failed.
  3. A_IMPLEMENTATION_REPORT.md contains no obsolete tokens (523 entries, 15/15, 30/30, 9/9)
     and states the final source-archive entry count from A_SOURCE_PACKAGE_SCAN.txt.
  4. If A_DEPLOY_MANIFEST.json exists: it must NOT claim gate_verdict=PASS while the build
     manifest is non-deployable, and its build_manifest_sha256 must equal sha256(build manifest).
Writes A_EVIDENCE_CONSISTENCY.txt; exits non-zero on any failure.
"""
import os, sys, re, json, hashlib, subprocess
import xml.etree.ElementTree as ET

r = subprocess.run(["git","rev-parse","--show-toplevel"], capture_output=True, text=True)
ROOT = r.stdout.strip() or os.getcwd()
P = os.path.join(ROOT, "docs", "review", "phaseA")
def path(n): return os.path.join(P, n)
def sha(f):
    h=hashlib.sha256()
    with open(f,"rb") as fh:
        for b in iter(lambda: fh.read(65536), b""): h.update(b)
    return h.hexdigest()

problems=[]; notes=[]
def need(c,msg):
    if not c: problems.append(msg)

bm_path = path("A_BUILD_MANIFEST.json")
need(os.path.exists(bm_path), "A_BUILD_MANIFEST.json missing")
bm = json.load(open(bm_path, encoding="utf-8")) if os.path.exists(bm_path) else {}
need(bm.get("gate_verdict") in ("PASS","PASS_FOR_REVIEW_NOT_DEPLOYABLE"), f"unexpected gate_verdict {bm.get('gate_verdict')}")
notes.append("build manifest verdict: " + str(bm.get("gate_verdict")))

def tagend(e,n): return e.tag.split('}')[-1]==n
def trx_counts(f):
    root=ET.parse(f).getroot(); c=next((e for e in root.iter() if tagend(e,"Counters")),None)
    return (int(c.get("total",0)), int(c.get("failed",0))) if c is not None else (None,None)
def junit_counts(f):
    root=ET.parse(f).getroot(); ts=root if tagend(root,"testsuite") else next((e for e in root.iter() if tagend(e,"testsuite")),None)
    return (int(ts.get("tests",0)), int(ts.get("failures",0))+int(ts.get("errors",0))) if ts is not None else (None,None)

if os.path.exists(path("A_DOTNET_TEST.trx")):
    tt,tf = trx_counts(path("A_DOTNET_TEST.trx"))
    need(tt==bm.get("dotnet_total") and tf==bm.get("dotnet_failed"),
         f"TRX ({tt}/{tf}) != manifest ({bm.get('dotnet_total')}/{bm.get('dotnet_failed')})")
    notes.append(f"TRX total/failed = {tt}/{tf}")
else: problems.append("A_DOTNET_TEST.trx missing")
if os.path.exists(path("A_PYTHON_TEST.junit.xml")):
    pt,pf = junit_counts(path("A_PYTHON_TEST.junit.xml"))
    need(pt==bm.get("python_total") and pf==bm.get("python_failed"),
         f"JUnit ({pt}/{pf}) != manifest ({bm.get('python_total')}/{bm.get('python_failed')})")
    notes.append(f"JUnit tests/failed = {pt}/{pf}")
else: problems.append("A_PYTHON_TEST.junit.xml missing")

# source archive entry count (review-mode scan is the authoritative pre-commit PASS)
src_entries=None
scan_file = next((f for f in ("A_SOURCE_PACKAGE_REVIEW_SCAN.txt","A_SOURCE_PACKAGE_SCAN.txt") if os.path.exists(path(f))), None)
if scan_file:
    m=re.search(r"entries:\s*(\d+)", open(path(scan_file),encoding="utf-8").read())
    if m: src_entries=int(m.group(1))
notes.append("source archive entries: " + str(src_entries))

# report coherence
rp=path("A_IMPLEMENTATION_REPORT.md")
if os.path.exists(rp):
    rt=open(rp,encoding="utf-8").read()
    for stale in ["523","15/15","30/30","9/9"]:
        need(stale not in rt, f"report contains obsolete token '{stale}'")
    need("1461/1461" in rt or "1461" in rt, "report missing final .NET count 1461")
    if src_entries is not None:
        need(str(src_entries) in rt, f"report does not state final source entry count {src_entries}")
else: problems.append("A_IMPLEMENTATION_REPORT.md missing")

# deploy manifest coherence
dm=path("A_DEPLOY_MANIFEST.json")
if os.path.exists(dm):
    d=json.load(open(dm,encoding="utf-8"))
    if bm.get("gate_verdict")!="PASS":
        need(d.get("gate_verdict")!="PASS", "stale A_DEPLOY_MANIFEST.json claims PASS beside a non-deployable build manifest")
    need(d.get("build_manifest_sha256")==sha(bm_path), "deploy manifest build_manifest_sha256 != current build manifest sha")
else:
    notes.append("no A_DEPLOY_MANIFEST.json present (expected for a non-deployable review build)")

out=["A_EVIDENCE_CONSISTENCY - final-state coherence","-"*50]+["note: "+n for n in notes]+["-"*50]
if problems:
    out += ["PROBLEM: "+p for p in problems] + ["RESULT: FAIL"]
    open(path("A_EVIDENCE_CONSISTENCY.txt"),"w",encoding="utf-8").write("\n".join(out)+"\n")
    print("\n".join(out)); sys.exit(1)
out += ["RESULT: PASS"]
open(path("A_EVIDENCE_CONSISTENCY.txt"),"w",encoding="utf-8").write("\n".join(out)+"\n")
print("\n".join(out)); sys.exit(0)
