"""Manifest census and de-duplication for the .gcae recorder tree. Read-only.

Stage 1 counted every physical manifest, including the two curated copies under
recorder/samples/ that carry the same sessionId as a directory under recorder/sessions/.
That double-counted their segments and records. This script separates:

  * physical manifest paths  (what is on disk)
  * unique sessionId         (what was actually recorded)
  * curated copies           (reported separately, never added in)

and it does NOT assume a same-ID pair is a duplicate. It compares the manifest bytes and
every segment hash, and says so either way.

usage: python manifest_census.py [root]
"""
import collections
import hashlib
import json
import os
import sys

DEFAULT_ROOT = r"C:\Users\LOQ\.gcae"


def sha256_file(p):
    h = hashlib.sha256()
    with open(p, "rb") as fh:
        for chunk in iter(lambda: fh.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def load_manifests(root):
    out = []
    for sub in ("sessions", "samples"):
        d = os.path.join(root, "recorder", sub)
        if not os.path.isdir(d):
            continue
        for s in sorted(os.listdir(d)):
            p = os.path.join(d, s, "manifest.json")
            if not os.path.isfile(p):
                continue
            try:
                m = json.load(open(p, encoding="utf-8"))
                err = None
            except Exception as e:
                m, err = None, str(e)
            out.append({"area": sub, "dir": s, "path": p, "manifest": m, "error": err})
    return out


def totals(entries):
    seg = rec = mkt = inv = life = 0
    for e in entries:
        for sg in (e["manifest"].get("segments") or []):
            seg += 1
            rec += sg.get("recordCount") or 0
            mkt += sg.get("marketEventRecordCount") or 0
            inv += sg.get("invocationResultRecordCount") or 0
            life += sg.get("lifecycleIntegrityRecordCount") or 0
    return seg, rec, mkt, inv, life


def main():
    root = sys.argv[1] if len(sys.argv) > 1 else DEFAULT_ROOT
    ents = load_manifests(root)
    print("###### RECORDER MANIFEST CENSUS AND DE-DUPLICATION ######")
    print("root: %s" % root)
    print()

    print("=== 1. every physical manifest path ===")
    for e in ents:
        rel = os.path.relpath(e["path"], root).replace("\\", "/")
        sid = (e["manifest"] or {}).get("sessionId", "<unparseable>")
        print("   %-62s sessionId=%s%s" % (rel, sid, "  PARSE ERROR: " + e["error"] if e["error"] else ""))
    print("   physical manifests: %d" % len(ents))
    print()

    good = [e for e in ents if e["manifest"] is not None]
    by_sid = collections.defaultdict(list)
    for e in good:
        by_sid[e["manifest"].get("sessionId")].append(e)

    print("=== 2. physical count vs unique sessionId ===")
    print("   physical manifests   : %d" % len(ents))
    print("   unparseable          : %d" % (len(ents) - len(good)))
    print("   unique sessionId     : %d" % len(by_sid))
    dupes = {k: v for k, v in by_sid.items() if len(v) > 1}
    print("   sessionIds appearing more than once: %d" % len(dupes))
    print()

    print("=== 3. same-ID pairs compared by HASH (not assumed duplicate) ===")
    identical = []
    for sid, group in sorted(dupes.items()):
        print("   sessionId %s appears %d times:" % (sid, len(group)))
        sigs = []
        for e in group:
            man_hash = sha256_file(e["path"])
            segdir = os.path.join(os.path.dirname(e["path"]), "segments")
            segs = {}
            if os.path.isdir(segdir):
                for f in sorted(os.listdir(segdir)):
                    if f.endswith(".seg"):
                        segs[f] = sha256_file(os.path.join(segdir, f))
            rel = os.path.relpath(e["path"], root).replace("\\", "/")
            print("      %-58s manifest sha256=%s" % (rel, man_hash[:16]))
            for f, h in segs.items():
                print("           segment %-40s %s" % (f, h[:16]))
            sigs.append((man_hash, tuple(sorted(segs.items()))))
        same_manifest = len({s[0] for s in sigs}) == 1
        same_segments = len({s[1] for s in sigs}) == 1
        verdict = ("IDENTICAL — manifest and every segment hash match"
                   if same_manifest and same_segments else
                   "NOT IDENTICAL — manifest match=%s, segment match=%s"
                   % (same_manifest, same_segments))
        print("      -> %s" % verdict)
        if same_manifest and same_segments:
            identical.append(sid)
        print()

    # FAIL CLOSED. A sessionId that appears more than once is only safe to collapse if every
    # copy is byte-identical. If they diverge, one of them is a different recording wearing the
    # same id, and silently electing a canonical copy would discard real data and publish a
    # total that is wrong in an invisible way. Stop instead.
    divergent = [sid for sid in dupes if sid not in identical]
    if divergent:
        print("=== FATAL: duplicate sessionId with NON-IDENTICAL content ===")
        for sid in divergent:
            print("   %s" % sid)
            for e in by_sid[sid]:
                print("      %s" % os.path.relpath(e["path"], root).replace("\\", "/"))
        print()
        print("   Refusing to elect a canonical copy. These are not duplicates: they are")
        print("   distinct recordings sharing an id, and collapsing them would silently drop")
        print("   data. Resolve the collision before any total is quoted.")
        raise SystemExit(2)

    print("=== 4. totals, de-duplicated ===")
    print("   (safe to collapse: every repeated sessionId was proved byte-identical above)")
    canonical = []
    curated = []
    for sid, group in by_sid.items():
        sess = [e for e in group if e["area"] == "sessions"]
        samp = [e for e in group if e["area"] == "samples"]
        canonical.append(sess[0] if sess else group[0])
        curated.extend(samp if sess else samp[1:])
    for label, entries in (("UNIQUE SESSIONS (canonical)", canonical),
                           ("CURATED SAMPLE COPIES (reported separately, NOT added)", curated)):
        seg, rec, mkt, inv, life = totals(entries)
        print("   %s: %d manifests" % (label, len(entries)))
        print("      segments                     : %d" % seg)
        print("      records                      : %d" % rec)
        print("        marketEventRecordCount     : %d" % mkt)
        print("        invocationResultRecordCount: %d" % inv)
        print("        lifecycleIntegrityRecords  : %d" % life)
    print()

    print("=== 5. stream posture and instrument, UNIQUE SESSIONS ONLY ===")
    for label, fn in (("enabledStreams", lambda m: ",".join(m.get("enabledStreams") or ["<none>"])),
                      ("disabledStreams", lambda m: ";".join(sorted(
                          d.get("stream", "?") for d in (m.get("disabledStreams") or []))) or "<none>"),
                      ("MBO posture", lambda m: "schema=%s recording=%s isolation=%s"
                       % (m.get("mboSchemaSupported"), m.get("mboRecordingEnabled"),
                          m.get("mboIsolationRequirement"))),
                      ("instrument", lambda m: (m.get("lastInstrument") or {}).get("securityId")),
                      ("start date UTC", lambda m: (m.get("startUtc") or "")[:10]),
                      ("abnormalTermination", lambda m: str(m.get("abnormalTermination")))):
        c = collections.Counter(fn(e["manifest"]) for e in canonical)
        print("   -- %s --" % label)
        for k, n in c.most_common():
            print("      %4d  %s" % (n, k))
    print()

    print("=== 6. session directories WITHOUT a valid manifest ===")
    d = os.path.join(root, "recorder", "sessions")
    missing = []
    for s in sorted(os.listdir(d)):
        sd = os.path.join(d, s)
        if not os.path.isdir(sd):
            continue
        if not os.path.isfile(os.path.join(sd, "manifest.json")):
            missing.append(s)
    for s in missing:
        sd = os.path.join(d, s)
        inv_ = []
        for sub in ("segments", "recovery"):
            p = os.path.join(sd, sub)
            if os.path.isdir(p):
                fs = sorted(os.listdir(p))
                size = sum(os.path.getsize(os.path.join(p, f)) for f in fs)
                inv_.append("%s: %d files, %d bytes" % (sub, len(fs), size))
        loose = [f for f in sorted(os.listdir(sd)) if os.path.isfile(os.path.join(sd, f))]
        print("   %s" % s)
        for i in inv_:
            print("      %s" % i)
        print("      loose files: %s" % (", ".join(loose) or "(none)"))
    print("   total without manifest: %d" % len(missing))
    print()

    print("=== 7. .tmp files — classified, NOT decoded and NOT modified ===")
    tmps = []
    for base, dirs, files in os.walk(root):
        dirs[:] = [x for x in dirs if x != ".git"]
        for f in files:
            if f.endswith(".tmp"):
                tmps.append(os.path.join(base, f))
    rows = []
    for p in sorted(tmps):
        rel = os.path.relpath(p, root).replace("\\", "/")
        parts = rel.split("/")
        session = parts[2] if len(parts) > 2 and parts[0] == "recorder" else "(outside recorder)"
        size = os.path.getsize(p)
        stem = p[: -len(".tmp")]
        has_final = os.path.isfile(stem)
        in_manifest = False
        mp = os.path.join(os.path.dirname(os.path.dirname(p)), "manifest.json")
        if os.path.isfile(mp):
            try:
                m = json.load(open(mp, encoding="utf-8"))
                names = {sg.get("fileName") for sg in (m.get("segments") or [])}
                in_manifest = os.path.basename(stem) in names
            except Exception:
                pass
        rows.append((rel, session, size, has_final, in_manifest))
    print("   count: %d" % len(rows))
    print("   %-64s %10s %12s %12s" % ("path", "bytes", "final-exists", "in-manifest"))
    for rel, sess, size, hf, im in rows:
        print("   %-64s %10d %12s %12s" % (rel, size, hf, im))
    zero = sum(1 for r in rows if r[2] == 0)
    orphan = sum(1 for r in rows if not r[3])
    print()
    print("   zero-byte                       : %d" % zero)
    print("   without a corresponding final file: %d" % orphan)
    print("   claimed by a manifest segment list: %d" % sum(1 for r in rows if r[4]))
    print("   -> recoverability NOT asserted: deciding whether these hold usable records")
    print("      requires decoding .seg payloads, which Stage 1 does not do.")
    by_sess = collections.Counter(r[1] for r in rows)
    print()
    print("   by session:")
    for s, n in by_sess.most_common():
        tot = sum(r[2] for r in rows if r[1] == s)
        print("      %-40s %3d files  %12d bytes" % (s, n, tot))


if __name__ == "__main__":
    main()
