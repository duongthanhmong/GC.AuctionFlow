"""Build a self-contained handover ZIP the reviewer can unzip and run without this repo.

The ZIP reproduces the minimum repository tree the test suite resolves against: the closure
package itself, every artifact named in EVIDENCE_INDEX.csv at its exact repo-relative path,
and the canonical parameter registry. Nothing else, so the ZIP stays small and every file in
it is a file the tests actually read.

Also embeds the git bundle so branch history travels with it.

usage: python verification/build_handover_zip.py
"""
import csv
import hashlib
import os
import sys
import zipfile

sys.dont_write_bytecode = True

HERE = os.path.dirname(os.path.abspath(__file__))
PKG = os.path.abspath(os.path.join(HERE, ".."))
REPO = os.path.abspath(os.path.join(PKG, "..", "..", "..", "..", ".."))
PKG_REL = "docs/review/kdk_v4/wp_l1_rithmic/stage2_closure"
HANDOVER = os.path.join(REPO, "docs", "review", "kdk_v4", "wp_l1_rithmic", "handover")
REGISTRY_REL = "docs/review/kdk_v4/mrbs_param_recon_r3/KDK_PARAMETER_CANONICAL_REGISTRY_R3.csv"

RUN_ME = """# GCAE Stage 2 Closure - independent verification

Unzip anywhere. This archive reproduces the exact repository sub-tree the test suite reads,
so no clone is required to check the result.

## Run the tests

```
python docs/review/kdk_v4/wp_l1_rithmic/stage2_closure/tests/test_stage2_closure.py
```

Expected final line: `RESULT: PASS - 47/47 checks`

## Reproduce the full transcript

```
python docs/review/kdk_v4/wp_l1_rithmic/stage2_closure/verification/run_verification.py
```

This re-hashes every artifact, recounts every figure from the files rather than the prose,
and re-resolves all 15 EvidenceIds. Expected final line: `RESULT: ALL CONFIRMED`.
(The git-state section will differ - there is no .git directory inside this archive.)

## Requirements

Python 3.11+, `jsonschema` and `referencing`:

```
pip install jsonschema referencing
```

Verified against Python 3.11.9, jsonschema 4.26.0, referencing 0.37.0.

## Git history

`GCAE_STAGE2_CLOSURE_full.bundle` is a self-contained bundle of branch
`wp-l1-rithmic-data-surface-discovery`, including commits `34f204c` and `8160f2c`. It
requires no external ref:

```
git clone GCAE_STAGE2_CLOSURE_full.bundle -b wp-l1-rithmic-data-surface-discovery gcae-check
```

The branch is also pushed to `origin`.

**On Windows, clone into a SHORT path** (e.g. `C:\gcae-check`). The repository contains
paths long enough to hit the 260-character `MAX_PATH` limit when cloned into an already-deep
directory - this was reproduced, and it fails with
`error: unable to create file ...: Filename too long` followed by
`fatal: unable to checkout working tree`. The commits clone correctly; only the working-tree
checkout fails. If a deep path is unavoidable:

```
git config --global core.longpaths true
```

The ADR filenames in this package were shortened for the same reason; the longest path in
the package is now 88 characters.

## What is in here

| path | what |
|---|---|
| `.../stage2_closure/` | the full package - 7 ADRs, 6 schemas, manifest, index, specs, tests |
| `.../wp_l1_rithmic/probe/` | the evidence artifacts every EvidenceId resolves to |
| `.../wp_l1_rithmic/12_*.md`, `13_*.md` | the probe reports the evidence came from |
| `.../mrbs_param_recon_r3/...REGISTRY_R3.csv` | the 134-parameter canonical registry |
| `SHA256SUMS.txt` | every file above, hashed |

## Status of the contents

All seven ADRs are `AWAITING_OWNER_APPROVAL` and are **not in force**. SC-005..009 in the
conflict register are **unresolved** by design - MRBS v1.1 line 62 forbids picking a side
without a decision record. Stage 2 is **not** declared accepted.
"""


def main():
    os.makedirs(HANDOVER, exist_ok=True)
    members = []

    for dp, dns, fns in os.walk(PKG):
        dns[:] = [d for d in dns if d != "__pycache__"]
        for fn in sorted(fns):
            p = os.path.join(dp, fn)
            rel = PKG_REL + "/" + os.path.relpath(p, PKG).replace(os.sep, "/")
            members.append((p, rel))

    idx = list(csv.DictReader(open(os.path.join(PKG, "EVIDENCE_INDEX.csv"), encoding="utf-8")))
    missing = []
    for r in idx:
        rel = r["artifact"]
        p = os.path.join(REPO, rel.replace("/", os.sep))
        if os.path.isfile(p):
            if not any(m[1] == rel for m in members):
                members.append((p, rel))
        else:
            missing.append(r["evidence_id"])

    # The registry is ALSO an evidence artifact (EV-PARAM-REGISTRY), so it may already be
    # in members - adding it unconditionally produced a duplicate ZIP entry.
    p = os.path.join(REPO, REGISTRY_REL.replace("/", os.sep))
    if os.path.isfile(p):
        if not any(m[1] == REGISTRY_REL for m in members):
            members.append((p, REGISTRY_REL))
    else:
        missing.append("REGISTRY")

    seen, deduped = set(), []
    for src, rel in members:
        if rel in seen:
            continue
        seen.add(rel)
        deduped.append((src, rel))
    members = deduped

    bundle = os.path.join(HANDOVER, "GCAE_STAGE2_CLOSURE_full.bundle")
    if os.path.isfile(bundle):
        members.append((bundle, "GCAE_STAGE2_CLOSURE_full.bundle"))
    else:
        missing.append("BUNDLE")

    if missing:
        print("REFUSING to build - missing inputs: %s" % missing)
        return 2

    lines = []
    for src, rel in sorted(members, key=lambda m: m[1]):
        h = hashlib.sha256(open(src, "rb").read()).hexdigest()
        lines.append("%s  %s" % (h, rel))
    sums = "\n".join(lines) + "\n"

    out = os.path.join(HANDOVER, "GCAE_STAGE2_CLOSURE_HANDOVER.zip")
    with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED) as z:
        for src, rel in sorted(members, key=lambda m: m[1]):
            z.write(src, rel)
        z.writestr("RUN_ME.md", RUN_ME)
        z.writestr("SHA256SUMS.txt", sums)

    zh = hashlib.sha256(open(out, "rb").read()).hexdigest()
    print("members            : %d files + RUN_ME.md + SHA256SUMS.txt" % len(members))
    print("zip                : %s" % os.path.relpath(out, REPO).replace(os.sep, "/"))
    print("zip bytes          : %d" % os.path.getsize(out))
    print("zip sha256         : %s" % zh)

    with zipfile.ZipFile(out) as z:
        bad = z.testzip()
        names = z.namelist()
    print("archive integrity  : %s" % ("OK" if bad is None else "CORRUPT at " + str(bad)))
    print("contains tests     : %s" % (PKG_REL + "/tests/test_stage2_closure.py" in names))
    print("contains bundle    : %s" % ("GCAE_STAGE2_CLOSURE_full.bundle" in names))
    print("contains registry  : %s" % (REGISTRY_REL in names))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
