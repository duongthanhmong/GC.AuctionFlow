"""Survey every recorder manifest and capability artifact under .gcae."""
import collections
import json
import os

ROOT = r"C:\Users\LOQ\.gcae"


def manifests():
    out = []
    for sub in ("sessions", "samples"):
        d = os.path.join(ROOT, "recorder", sub)
        if not os.path.isdir(d):
            continue
        for s in sorted(os.listdir(d)):
            p = os.path.join(d, s, "manifest.json")
            if os.path.isfile(p):
                try:
                    out.append((sub, s, json.load(open(p, encoding="utf-8"))))
                except Exception as e:
                    out.append((sub, s, {"__parse_error__": str(e)}))
    return out


def main():
    ms = manifests()
    print("###### RECORDER SURVEY ######")
    print("  manifests found: %d" % len(ms))
    bad = [m for m in ms if "__parse_error__" in m[2]]
    print("  unparseable    : %d" % len(bad))
    for sub, s, m in bad:
        print("     %s/%s: %s" % (sub, s, m["__parse_error__"]))
    ok = [m for m in ms if "__parse_error__" not in m[2]]
    print()

    keys = collections.Counter()
    for _, _, m in ok:
        keys.update(m.keys())
    print("=== manifest fields (count / %d manifests) ===" % len(ok))
    for k, n in keys.most_common():
        print("   %-34s %d" % (k, n))
    print()

    def tally(fn, label):
        c = collections.Counter()
        for _, _, m in ok:
            try:
                v = fn(m)
            except Exception:
                v = "<error>"
            c[v] += 1
        print("=== %s ===" % label)
        for k, n in c.most_common():
            print("   %5d  %s" % (n, k))
        print()

    tally(lambda m: m.get("recorderSchemaVersion"), "recorderSchemaVersion")
    tally(lambda m: m.get("declaredDataSourceMode"), "declaredDataSourceMode")
    tally(lambda m: m.get("declaredProvider"), "declaredProvider")
    tally(lambda m: ",".join(m.get("enabledStreams", []) or ["<none>"]), "enabledStreams")
    tally(lambda m: ";".join(sorted(d.get("stream", "?") for d in m.get("disabledStreams", []))) or "<none>",
          "disabledStreams")
    tally(lambda m: "schema=%s recording=%s isolation=%s"
                    % (m.get("mboSchemaSupported"), m.get("mboRecordingEnabled"),
                       m.get("mboIsolationRequirement")), "MBO posture")
    tally(lambda m: (m.get("lastInstrument") or {}).get("securityId"), "instrument")
    tally(lambda m: (m.get("startUtc") or "")[:10], "session start date (UTC)")

    segs = recs = mkt = inv = life = 0
    seq_gaps = []
    for _, s, m in ok:
        prev_last = 0
        for sg in m.get("segments", []) or []:
            segs += 1
            recs += sg.get("recordCount", 0) or 0
            mkt += sg.get("marketEventRecordCount", 0) or 0
            inv += sg.get("invocationResultRecordCount", 0) or 0
            life += sg.get("lifecycleIntegrityRecordCount", 0) or 0
            fw = sg.get("firstWriterSequence")
            if prev_last and fw is not None and fw != prev_last + 1:
                seq_gaps.append((s, prev_last, fw))
            prev_last = sg.get("lastWriterSequence") or prev_last
    print("=== totals across all manifests ===")
    print("   segments                     : %d" % segs)
    print("   records                      : %d" % recs)
    print("     marketEventRecordCount     : %d" % mkt)
    print("     invocationResultRecordCount: %d" % inv)
    print("     lifecycleIntegrityRecords  : %d" % life)
    print("   writer-sequence discontinuities between consecutive segments: %d" % len(seq_gaps))
    for s, a, b in seq_gaps[:10]:
        print("      session %s: %d -> %d" % (s[:8], a, b))
    print()

    extra = collections.Counter()
    for _, _, m in ok:
        for sg in (m.get("segments") or [])[:1]:
            extra.update(sg.keys())
    print("=== segment record fields ===")
    for k, n in extra.most_common():
        print("   %-34s %d" % (k, n))
    print()

    cap = os.path.join(ROOT, "capability")
    probes = collections.Counter()
    fields = collections.defaultdict(collections.Counter)
    for f in sorted(os.listdir(cap)):
        if not f.endswith(".json"):
            continue
        kind = f.split("_")[0]
        probes[kind] += 1
        try:
            d = json.load(open(os.path.join(cap, f), encoding="utf-8"))
            fields[kind].update(d.keys())
        except Exception:
            fields[kind]["<unparseable>"] += 1
    print("=== capability probe artifacts ===")
    for k, n in probes.most_common():
        print("   %-20s %d artifacts" % (k, n))
        for fk, fn in fields[k].most_common(40):
            print("        %-42s %d" % (fk, fn))
        print()


if __name__ == "__main__":
    main()
