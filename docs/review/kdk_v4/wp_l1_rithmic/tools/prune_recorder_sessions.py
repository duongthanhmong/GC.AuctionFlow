"""Prune the local recorder spool to a representative retention set.

Owner-authorised 2026-07-31. recorder/sessions/ is gitignored and therefore the ONLY copy,
so this writes a complete manifest of everything it removes BEFORE removing anything. The
bytes go; the record of what existed does not.

Retention rule, deterministic:
  * keep the 4 sessions already decoded in Stage 2 evidence, and
  * keep the smallest session for every (startUtc date x enabledStreams) group.

Everything else under recorder/sessions/ is removed, including unsealed .seg.tmp.
recorder/samples/, capability/, research/ and instrument_master_gcae.json are NEVER touched.

usage:
    python prune_recorder_sessions.py --manifest <path.csv>            # dry run
    python prune_recorder_sessions.py --manifest <path.csv> --apply    # delete
"""
import argparse
import collections
import csv
import json
import os
import shutil
import sys

ROOT = os.path.join(os.path.expanduser("~"), ".gcae", "recorder", "sessions")
PROTECTED = ("samples", "capability", "research", "instrument_master_gcae.json", "rithmic.env")

ALREADY_DECODED = {
    "7cc5b6551f794a6094aee403ec20a408",
    "4b0078a775764849acf44fc47f30fd68",
    "b46a5c0d0f6443eaa231d59f9fb9fa33",
    "27ca68e6537740838ab00eb825c185ef",
}


def scan(root):
    out = []
    for s in sorted(os.listdir(root)):
        d = os.path.join(root, s)
        if not os.path.isdir(d):
            continue
        total = 0
        segs, tmps = [], []
        for base, _, files in os.walk(d):
            for f in files:
                p = os.path.join(base, f)
                total += os.path.getsize(p)
                if f.endswith(".seg"):
                    segs.append(f)
                elif f.endswith(".seg.tmp"):
                    tmps.append(f)
        mp = os.path.join(d, "manifest.json")
        man = None
        if os.path.isfile(mp):
            try:
                man = json.load(open(mp, encoding="utf-8"))
            except Exception:
                man = None
        out.append({
            "session": s, "dir": d, "bytes": total,
            "sealed_segments": len(segs), "unsealed_tmp": len(tmps),
            "has_manifest": man is not None,
            "start_utc": (man or {}).get("startUtc", ""),
            "stop_utc": (man or {}).get("stopUtc", ""),
            "streams": ",".join((man or {}).get("enabledStreams") or []),
            "instrument": ((man or {}).get("lastInstrument") or {}).get("securityId", ""),
            "records": sum((sg.get("recordCount") or 0)
                           for sg in ((man or {}).get("segments") or [])),
            "segment_sha256": ";".join((sg.get("sha256Hex") or "")[:16]
                                       for sg in ((man or {}).get("segments") or [])),
            "abnormal": (man or {}).get("abnormalTermination", ""),
        })
    return out


def choose_keep(rows):
    keep = set(ALREADY_DECODED)
    groups = collections.defaultdict(list)
    for r in rows:
        if r["has_manifest"]:
            groups[(r["start_utc"][:10], r["streams"])].append((r["bytes"], r["session"]))
    for _, v in groups.items():
        keep.add(sorted(v)[0][1])
    return keep


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--manifest", required=True)
    ap.add_argument("--apply", action="store_true")
    a = ap.parse_args()

    if not os.path.isdir(ROOT):
        print("recorder/sessions not found at %s" % ROOT)
        return 1
    # Refuse to run if the path does not end where we expect.
    if os.path.basename(ROOT) != "sessions" or "recorder" not in ROOT:
        print("REFUSING: unexpected root %s" % ROOT)
        return 2

    rows = scan(ROOT)
    keep = choose_keep(rows)
    kept = [r for r in rows if r["session"] in keep]
    drop = [r for r in rows if r["session"] not in keep]

    os.makedirs(os.path.dirname(a.manifest), exist_ok=True)
    with open(a.manifest, "w", encoding="utf-8", newline="") as fh:
        w = csv.writer(fh, lineterminator="\n")
        w.writerow(["disposition", "session", "bytes", "records", "sealed_segments",
                    "unsealed_tmp", "has_manifest", "start_utc", "stop_utc", "streams",
                    "instrument", "abnormal_termination", "segment_sha256_prefixes"])
        for label, group in (("KEPT", kept), ("DELETED", drop)):
            for r in sorted(group, key=lambda x: x["session"]):
                w.writerow([label, r["session"], r["bytes"], r["records"],
                            r["sealed_segments"], r["unsealed_tmp"], r["has_manifest"],
                            r["start_utc"], r["stop_utc"], r["streams"], r["instrument"],
                            r["abnormal"], r["segment_sha256"]])

    print("retention rule: 4 already-decoded sessions + smallest per (date x streams)")
    print("  sessions total : %d" % len(rows))
    print("  KEEP           : %d  (%.2f GB)" % (len(kept), sum(r["bytes"] for r in kept) / 1e9))
    print("  DELETE         : %d  (%.2f GB)" % (len(drop), sum(r["bytes"] for r in drop) / 1e9))
    print("  records removed: %d" % sum(r["records"] for r in drop))
    print("  manifest       : %s" % a.manifest)
    print()
    if not a.apply:
        print("DRY RUN - nothing deleted. Re-run with --apply to delete.")
        return 0

    freed = 0
    for r in drop:
        d = r["dir"]
        if os.path.basename(os.path.dirname(d)) != "sessions":
            print("  SKIP (unexpected path): %s" % d)
            continue
        shutil.rmtree(d)
        freed += r["bytes"]
        print("  deleted %s  (%.1f MB)" % (r["session"], r["bytes"] / 1e6))
    print()
    print("freed %.2f GB" % (freed / 1e9))
    left = scan(ROOT)
    print("sessions remaining: %d  (%.2f GB)" % (len(left), sum(x["bytes"] for x in left) / 1e9))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
