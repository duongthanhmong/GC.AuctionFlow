"""Read-only, offline decoder for the GCAE recorder container.

Stage 2, item 3: the minimum needed to CLASSIFY records and verify Trade/DOM/MBO semantics.
It opens files 'rb' only and writes nothing anywhere.

Container format, transcribed from src/GC.AuctionFlow/Recorder/FrameCodec.cs and
RecorderEnums.cs. Every constant below cites the C# line it came from, so the transcription
can be checked rather than trusted:

  container header, 8 bytes                       FrameCodec.cs:19-25
      uint32  magic  = 0x52414347  ascii "GCAR"   FrameCodec.cs:10
      uint16  containerVersion (must be 1)        RawEventRecorderVersions.cs
      uint16  flags

  frame, repeated                                 FrameCodec.cs:94-111
      uint32  magic  = 0x31464347  ascii "GCF1"   FrameCodec.cs:13
      uint16  frameVersion (must be 1)
      uint16  frameType   1=SegmentHeader 2=RawEvent 3=SegmentFooter   RecorderEnums.cs:67-72
      uint16  flags
      uint16  reserved (written 0)
      uint32  payloadLength
      byte[]  payload, UTF-8 JSON
      uint32  crc32c over (16-byte header + payload)  FrameCodec.cs:106-110

  CRC-32C, reflected Castagnoli poly 0x82F63B78, init/xorout 0xFFFFFFFF   Crc32C.cs:11,26-30

Payload discriminator RawEventPayloadKind                       RecorderEnums.cs:27-41
Stream discriminator  RecorderStreamKind                        RecorderEnums.cs:3-10

usage:
    python segment_decoder.py <segment.seg> [...]        # decode named segments
    python segment_decoder.py --samples                  # curated samples only
    python segment_decoder.py --corpus                   # every sealed .seg under .gcae
"""
import collections
import glob
import json
import os
import struct
import sys

GCAE_ROOT = r"C:\Users\LOQ\.gcae"

CONTAINER_MAGIC = 0x52414347
FRAME_MAGIC = 0x31464347
CONTAINER_HEADER = 8
FRAME_HEADER = 16
FRAME_CRC = 4
SUPPORTED_CONTAINER_VERSION = 1
SUPPORTED_FRAME_VERSION = 1

FRAME_TYPE = {1: "SegmentHeader", 2: "RawEvent", 3: "SegmentFooter"}

PAYLOAD_KIND = {
    1: "NewTrade", 2: "CumulativeTradeNew", 3: "CumulativeTradeUpdate", 4: "Depth",
    5: "BestBidAsk", 6: "DomSnapshotRequest", 7: "DomSnapshotItem",
    8: "DomSnapshotLocalEnumerationResult", 9: "Mbo", 10: "RecorderLifecycle",
    11: "RecorderIntegrity", 12: "CallbackInvocationResult",
}
STREAM_KIND = {1: "Trade", 2: "Dom", 3: "Mbo", 4: "Lifecycle", 5: "Integrity"}


def _crc32c_table():
    poly = 0x82F63B78
    table = []
    for i in range(256):
        crc = i
        for _ in range(8):
            crc = (crc >> 1) ^ poly if crc & 1 else crc >> 1
        table.append(crc)
    return table


_TABLE = _crc32c_table()


def crc32c(data):
    crc = 0xFFFFFFFF
    for b in data:
        crc = _TABLE[(crc ^ b) & 0xFF] ^ (crc >> 8)
    return crc ^ 0xFFFFFFFF


class Result:
    def __init__(self, path):
        self.path = path
        self.size = os.path.getsize(path)
        self.container_error = None
        self.crc_verified = True
        self.container_version = None
        self.frames = collections.Counter()
        self.payload_kinds = collections.Counter()
        self.stream_kinds = collections.Counter()
        self.callback_sources = collections.Counter()
        self.crc_failures = 0
        self.decode_errors = []
        self.trailing_bytes = 0
        self.header_json = None
        self.footer_json = None
        self.integrity_flags = collections.Counter()
        self.native_seq_available = collections.Counter()
        self.depth_actions = collections.Counter()
        self.mbo_samples = []
        self.seq_first = None
        self.seq_last = None
        self.seq_gaps = 0


def decode(path, keep_mbo_samples=3, verify_crc=True):
    """Decode one segment.

    verify_crc=False skips only the CRC arithmetic; magic, version, length framing and JSON
    are still validated. It exists because CRC-32C in pure Python runs at a few MB/s and the
    local corpus is ~13 GB. Whenever it is used, the report says so explicitly rather than
    letting a reader assume every byte was checksum-verified.
    """
    r = Result(path)
    r.crc_verified = verify_crc
    with open(path, "rb") as fh:
        buf = fh.read()

    if len(buf) < CONTAINER_HEADER:
        r.container_error = "ShortContainerHeader"
        return r
    magic, ver, _flags = struct.unpack_from("<IHH", buf, 0)
    if magic != CONTAINER_MAGIC:
        r.container_error = "InvalidContainerMagic"
        return r
    if ver != SUPPORTED_CONTAINER_VERSION:
        r.container_error = "UnsupportedContainerVersion(%d)" % ver
        return r
    r.container_version = ver

    off = CONTAINER_HEADER
    while off < len(buf):
        if len(buf) - off < FRAME_HEADER:
            r.trailing_bytes = len(buf) - off
            r.decode_errors.append("NeedMoreData at %d (%d trailing bytes)" % (off, r.trailing_bytes))
            break
        fmagic, fver, ftype, _ff, _res, plen = struct.unpack_from("<IHHHHI", buf, off)
        if fmagic != FRAME_MAGIC:
            r.decode_errors.append("InvalidFrameMagic at %d" % off)
            r.trailing_bytes = len(buf) - off
            break
        if fver != SUPPORTED_FRAME_VERSION:
            r.decode_errors.append("UnsupportedFrameVersion(%d) at %d" % (fver, off))
            break
        total = FRAME_HEADER + plen + FRAME_CRC
        if len(buf) - off < total:
            r.trailing_bytes = len(buf) - off
            r.decode_errors.append("NeedMoreData at %d (want %d, have %d)"
                                   % (off, total, len(buf) - off))
            break
        covered = buf[off:off + FRAME_HEADER + plen]
        if verify_crc:
            want = struct.unpack_from("<I", buf, off + FRAME_HEADER + plen)[0]
            if crc32c(covered) != want:
                r.crc_failures += 1
                r.decode_errors.append("FrameCrcMismatch at %d" % off)
                off += total
                continue

        name = FRAME_TYPE.get(ftype, "Unknown(%d)" % ftype)
        r.frames[name] += 1
        payload = covered[FRAME_HEADER:]
        try:
            doc = json.loads(payload.decode("utf-8"))
        except Exception as e:
            r.decode_errors.append("PayloadNotJson at %d: %s" % (off, e))
            off += total
            continue

        if name == "SegmentHeader":
            r.header_json = doc
        elif name == "SegmentFooter":
            r.footer_json = doc
        else:
            _classify(r, doc, keep_mbo_samples)
        off += total
    return r


def _get(doc, *names):
    """Field lookup tolerant of camelCase / PascalCase serialisation."""
    for n in names:
        for k in (n, n[0].lower() + n[1:], n[0].upper() + n[1:]):
            if k in doc:
                return doc[k]
    return None


def _classify(r, doc, keep_mbo_samples):
    kind = _get(doc, "PayloadDiscriminator")
    kname = PAYLOAD_KIND.get(kind, str(kind)) if isinstance(kind, int) else str(kind)
    r.payload_kinds[kname] += 1

    sk = _get(doc, "StreamKind")
    r.stream_kinds[STREAM_KIND.get(sk, str(sk)) if isinstance(sk, int) else str(sk)] += 1

    cs = _get(doc, "CallbackSource")
    r.callback_sources[str(cs)] += 1

    fl = _get(doc, "IntegrityFlags")
    if fl is not None:
        r.integrity_flags[str(fl)] += 1

    ns = _get(doc, "NativeSequenceAvailable")
    if ns is not None:
        r.native_seq_available[str(ns)] += 1

    seq = _get(doc, "RecorderGlobalLocalSequence")
    if isinstance(seq, int):
        if r.seq_first is None:
            r.seq_first = seq
        elif r.seq_last is not None and seq != r.seq_last + 1:
            r.seq_gaps += 1
        r.seq_last = seq

    payload = _get(doc, "Payload") or {}
    if kname == "Depth" and isinstance(payload, dict):
        r.depth_actions[str(_get(payload, "UpdateAction"))] += 1
    if kname == "Mbo" and len(r.mbo_samples) < keep_mbo_samples and isinstance(payload, dict):
        r.mbo_samples.append(sorted(payload.keys()))


def report(results, title):
    print("=" * 78)
    print(title)
    print("=" * 78)
    tot = collections.Counter()
    kinds = collections.Counter()
    streams = collections.Counter()
    depth = collections.Counter()
    crc = errs = 0
    bad_container = []
    mbo_shapes = []
    for r in results:
        tot.update(r.frames)
        kinds.update(r.payload_kinds)
        streams.update(r.stream_kinds)
        depth.update(r.depth_actions)
        crc += r.crc_failures
        errs += len(r.decode_errors)
        if r.container_error:
            bad_container.append((r.path, r.container_error))
        mbo_shapes.extend(r.mbo_samples)
    verified = sum(1 for r in results if getattr(r, "crc_verified", True))
    print("  segments decoded      : %d" % len(results))
    print("  CRC-32C verified      : %d of %d segments%s"
          % (verified, len(results),
             "" if verified == len(results) else
             "   <-- the rest had CRC skipped for cost; framing and JSON still validated"))
    print("  container errors      : %d" % len(bad_container))
    for p, e in bad_container[:10]:
        print("      %s: %s" % (os.path.basename(p), e))
    print("  CRC failures          : %d" % crc)
    print("  decode errors         : %d" % errs)
    print()
    print("  frames by type:")
    for k, n in tot.most_common():
        print("      %-22s %d" % (k, n))
    print()
    print("  RawEvent payload kinds:")
    for k, n in kinds.most_common():
        print("      %-36s %d" % (k, n))
    print()
    print("  stream kinds:")
    for k, n in streams.most_common():
        print("      %-22s %d" % (k, n))
    if depth:
        print()
        print("  Depth UpdateAction:")
        for k, n in depth.most_common():
            print("      %-22s %d" % (k, n))
    print()
    print("  MBO payload field shapes observed: %d" % len(mbo_shapes))
    for s in mbo_shapes[:3]:
        print("      %s" % ", ".join(s))
    if not mbo_shapes:
        print("      (none — no Mbo payload decoded in this set)")
    print()


def sealed_segments(root, area):
    return sorted(glob.glob(os.path.join(root, "recorder", area, "*", "segments", "*.seg")))


def main():
    args = sys.argv[1:]
    if not args:
        print(__doc__)
        return 0
    if args[0] == "--samples":
        paths = sealed_segments(GCAE_ROOT, "samples")
        title = "CURATED SAMPLES — %d sealed segments" % len(paths)
    elif args[0] == "--corpus":
        paths = sealed_segments(GCAE_ROOT, "sessions")
        title = "LOCAL CORPUS — %d sealed segments (read-only)" % len(paths)
    elif args[0] == "--sessions":
        want = set(args[1:])
        paths = [p for p in sealed_segments(GCAE_ROOT, "sessions")
                 if os.path.basename(os.path.dirname(os.path.dirname(p))) in want]
        title = "SELECTED SESSIONS — %d sealed segments from %d sessions" % (len(paths), len(want))
    else:
        paths = args
        title = "NAMED SEGMENTS — %d" % len(paths)
    no_crc = "--no-crc" in args
    paths = [p for p in paths if p != "--no-crc"]
    results = []
    for i, p in enumerate(paths, 1):
        results.append(decode(p, verify_crc=not no_crc))
        if len(paths) > 8:
            print("  [%d/%d] %s" % (i, len(paths), os.path.basename(p)), flush=True)
    report(results, title)
    for r in results:
        if r.header_json is not None and len(results) <= 4:
            print("  header of %s:" % os.path.basename(r.path))
            print("     %s" % json.dumps(r.header_json, indent=None)[:600])
        if r.footer_json is not None and len(results) <= 4:
            print("  footer of %s:" % os.path.basename(r.path))
            print("     %s" % json.dumps(r.footer_json, indent=None)[:600])
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
