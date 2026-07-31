"""Full-population record census over decoded recorder segments. Read-only.

Stage 2 corrigendum item 2. The Stage 2 decoder kept at most three MBO field SHAPES per
segment, which proves sampled key presence and nothing more. This tool walks every decoded
record and separates four different questions that the earlier report ran together:

    1. schema/key presence      - is the key in the JSON at all
    2. non-null value presence  - does it carry a value rather than null/empty
    3. usable order identity    - is the value a plausible order id (non-zero, distinct)
    4. lifecycle completeness   - is the transition semantics actually recorded

It writes nothing and opens files 'rb' only.

usage:
    python record_census.py --sessions <id> [<id> ...]
    python record_census.py --samples
"""
import collections
import glob
import json
import os
import struct
import sys

sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import segment_decoder as sd  # noqa: E402

GCAE_ROOT = sd.GCAE_ROOT


class Census:
    def __init__(self):
        self.records = 0
        self.by_kind = collections.Counter()
        self.mbo = 0
        self.key_present = collections.Counter()
        self.non_null = collections.Counter()
        self.order_ids = set()
        self.order_id_zero = 0
        self.order_id_neg = 0
        self.priority_zero = 0
        self.priority_neg = 0
        self.priority_pos = 0
        self.price_null = self.price_zero = 0
        self.volume_null = self.volume_zero = 0
        self.raw_type_name = collections.Counter()
        self.raw_type_numeric = collections.Counter()
        self.raw_type_known = collections.Counter()
        self.lifecycle = collections.Counter()
        self.snapshot_known = collections.Counter()
        self.derived_side = collections.Counter()
        self.native_seq = collections.Counter()
        self.integrity_flags = collections.Counter()
        self.depth_action = collections.Counter()
        self.global_seq_gaps = 0
        self.global_seq_backwards = 0
        self.stream_seq_gaps = collections.Counter()
        self.cb_invocation_backwards = 0
        self._last_global = None
        self._last_stream = {}
        self._last_cb = None


def _walk_records(path):
    buf = open(path, "rb").read()
    if len(buf) < sd.CONTAINER_HEADER:
        return
    magic, ver, _f = struct.unpack_from("<IHH", buf, 0)
    if magic != sd.CONTAINER_MAGIC or ver != sd.SUPPORTED_CONTAINER_VERSION:
        return
    off = sd.CONTAINER_HEADER
    while off + sd.FRAME_HEADER <= len(buf):
        fm, fv, ft, _fl, _r, plen = struct.unpack_from("<IHHHHI", buf, off)
        if fm != sd.FRAME_MAGIC or fv != sd.SUPPORTED_FRAME_VERSION:
            return
        total = sd.FRAME_HEADER + plen + sd.FRAME_CRC
        if len(buf) - off < total:
            return
        if ft == 2:
            try:
                yield json.loads(buf[off + sd.FRAME_HEADER:off + sd.FRAME_HEADER + plen].decode("utf-8"))
            except Exception:
                pass
        off += total


def _num(v):
    return v if isinstance(v, (int, float)) else None


def _kind(v):
    """PayloadDiscriminator / StreamKind serialise as the ENUM NAME, not the number.

    Stage 2's first census assumed an int and reported every record as '?'. Accept both:
    an int is mapped through the table, a string is already the name.
    """
    if isinstance(v, int):
        return sd.PAYLOAD_KIND.get(v, str(v))
    return str(v)


def _stream(v):
    if isinstance(v, int):
        return sd.STREAM_KIND.get(v, str(v))
    return str(v)


def tally(c, doc):
    c.records += 1
    kind = _kind(sd._get(doc, "PayloadDiscriminator"))
    c.by_kind[kind] += 1

    ns = sd._get(doc, "NativeSequenceAvailable")
    c.native_seq[str(ns)] += 1
    c.integrity_flags[str(sd._get(doc, "IntegrityFlags"))] += 1

    g = sd._get(doc, "RecorderGlobalLocalSequence")
    if isinstance(g, int):
        if c._last_global is not None:
            if g < c._last_global:
                c.global_seq_backwards += 1
            elif g != c._last_global + 1:
                c.global_seq_gaps += 1
        c._last_global = g

    sk = _stream(sd._get(doc, "StreamKind"))
    s = sd._get(doc, "StreamLocalCaptureSequence")
    if isinstance(s, int):
        prev = c._last_stream.get(sk)
        if prev is not None and s != prev + 1:
            c.stream_seq_gaps[sk] += 1
        c._last_stream[sk] = s

    cb = sd._get(doc, "CallbackInvocationSequence")
    if isinstance(cb, int):
        if c._last_cb is not None and cb < c._last_cb:
            c.cb_invocation_backwards += 1
        c._last_cb = cb

    p = sd._get(doc, "Payload") or {}
    if kind == "Depth":
        c.depth_action[str(sd._get(p, "UpdateAction"))] += 1
        return
    if kind != "Mbo":
        return

    c.mbo += 1
    for key in ("exchangeOrderId", "priority", "price", "volume", "rawTypeName",
                "rawTypeNumeric", "rawTypeIsKnownEnumMember", "interpretedLifecycleAction",
                "snapshotCompletionKnown", "derivedSide", "rawSideName"):
        if key in p:
            c.key_present[key] += 1
            if p[key] is not None and p[key] != "":
                c.non_null[key] += 1

    oid = _num(p.get("exchangeOrderId"))
    if oid is not None:
        c.order_ids.add(oid)
        if oid == 0:
            c.order_id_zero += 1
        elif oid < 0:
            c.order_id_neg += 1

    pr = _num(p.get("priority"))
    if pr is not None:
        if pr > 0:
            c.priority_pos += 1
        elif pr == 0:
            c.priority_zero += 1
        else:
            c.priority_neg += 1

    pv = p.get("price")
    if pv is None:
        c.price_null += 1
    elif _num(pv) == 0:
        c.price_zero += 1
    vv = p.get("volume")
    if vv is None:
        c.volume_null += 1
    elif _num(vv) == 0:
        c.volume_zero += 1

    c.raw_type_name[str(p.get("rawTypeName"))] += 1
    c.raw_type_numeric[str(p.get("rawTypeNumeric"))] += 1
    c.raw_type_known[str(p.get("rawTypeIsKnownEnumMember"))] += 1
    c.lifecycle[str(p.get("interpretedLifecycleAction"))] += 1
    c.snapshot_known[str(p.get("snapshotCompletionKnown"))] += 1
    c.derived_side[str(p.get("derivedSide"))] += 1


def dist(label, counter, limit=8):
    print("   %s" % label)
    if not counter:
        print("      (none)")
        return
    for k, n in counter.most_common(limit):
        print("      %-44s %d" % (k, n))


def report(name, c):
    print("=" * 78)
    print(name)
    print("=" * 78)
    print("   records decoded                    : %d" % c.records)
    dist("payload kinds:", c.by_kind, 20)
    print()
    print("   -- envelope ordering and integrity --")
    print("   RecorderGlobalLocalSequence gaps   : %d" % c.global_seq_gaps)
    print("   RecorderGlobalLocalSequence back   : %d" % c.global_seq_backwards)
    print("   CallbackInvocationSequence back    : %d" % c.cb_invocation_backwards)
    dist("StreamLocalCaptureSequence gaps by stream:", c.stream_seq_gaps)
    dist("NativeSequenceAvailable:", c.native_seq)
    dist("IntegrityFlags:", c.integrity_flags)
    if c.depth_action:
        dist("Depth UpdateAction:", c.depth_action)
    print()
    if not c.mbo:
        print("   -- no MBO records in this set --")
        print()
        return
    print("   -- MBO, FULL POPULATION (n=%d) --" % c.mbo)
    print("   1. SCHEMA / KEY PRESENCE")
    for k in ("exchangeOrderId", "priority", "price", "volume", "rawTypeName",
              "interpretedLifecycleAction", "snapshotCompletionKnown"):
        print("      %-30s key present in %d/%d" % (k, c.key_present[k], c.mbo))
    print()
    print("   2. NON-NULL VALUE PRESENCE")
    for k in ("exchangeOrderId", "priority", "price", "volume"):
        print("      %-30s non-null in   %d/%d" % (k, c.non_null[k], c.mbo))
    print()
    print("   3. USABLE ORDER-LEVEL IDENTITY")
    print("      distinct exchangeOrderId values : %d" % len(c.order_ids))
    print("      exchangeOrderId == 0            : %d" % c.order_id_zero)
    print("      exchangeOrderId  < 0            : %d" % c.order_id_neg)
    print("      priority > 0                    : %d" % c.priority_pos)
    print("      priority == 0                   : %d" % c.priority_zero)
    print("      priority  < 0                   : %d" % c.priority_neg)
    print("      price null / zero               : %d / %d" % (c.price_null, c.price_zero))
    print("      volume null / zero              : %d / %d" % (c.volume_null, c.volume_zero))
    print()
    print("   4. LIFECYCLE COMPLETENESS")
    dist("rawTypeName:", c.raw_type_name)
    dist("rawTypeNumeric:", c.raw_type_numeric)
    dist("rawTypeIsKnownEnumMember:", c.raw_type_known)
    dist("interpretedLifecycleAction:", c.lifecycle)
    dist("snapshotCompletionKnown:", c.snapshot_known)
    dist("derivedSide:", c.derived_side)
    print()


def main():
    args = sys.argv[1:]
    if not args:
        print(__doc__)
        return 0
    if args[0] == "--samples":
        groups = {"CURATED SAMPLES": sd.sealed_segments(GCAE_ROOT, "samples")}
    elif args[0] == "--sessions":
        groups = {}
        for sid in args[1:]:
            groups[sid] = sorted(glob.glob(os.path.join(
                GCAE_ROOT, "recorder", "sessions", sid, "segments", "*.seg")))
    else:
        groups = {"NAMED": args}

    overall = Census()
    for name, paths in groups.items():
        c = Census()
        for p in paths:
            for doc in _walk_records(p):
                tally(c, doc)
                tally(overall, doc)
        report("%s  (%d sealed segment(s))" % (name, len(paths)), c)
    if len(groups) > 1:
        report("AGGREGATE ACROSS ALL SELECTED SESSIONS", overall)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
