"""Deterministic offline self-tests for the recorder decoder. No .gcae access, no network.

Stage 2 corrigendum item 5. The decoder is the instrument every MBO and DOM figure rests on,
so it needs its own evidence rather than only agreeing with the producer once.

Frames are built here in Python and fed back through the decoder, so a transcription error in
the layout or the CRC would fail these before it could reach a report.

usage: python test_segment_decoder.py
"""
import json
import os
import struct
import sys
import tempfile

sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import segment_decoder as sd  # noqa: E402

FAILURES = []


def check(name, ok, detail=""):
    print("  %-58s %s%s" % (name, "PASS" if ok else "FAIL", ("  " + detail) if detail else ""))
    if not ok:
        FAILURES.append(name)


def frame(ftype, payload_bytes, *, crc=None, version=sd.SUPPORTED_FRAME_VERSION):
    head = struct.pack("<IHHHHI", sd.FRAME_MAGIC, version, ftype, 0, 0, len(payload_bytes))
    body = head + payload_bytes
    return body + struct.pack("<I", sd.crc32c(body) if crc is None else crc)


def container(version=sd.SUPPORTED_CONTAINER_VERSION, magic=sd.CONTAINER_MAGIC):
    return struct.pack("<IHH", magic, version, 0)


def envelope(kind="Mbo"):
    return json.dumps({
        "payloadDiscriminator": kind, "streamKind": "Mbo",
        "recorderGlobalLocalSequence": 1, "streamLocalCaptureSequence": 1,
        "callbackInvocationSequence": 1, "nativeSequenceAvailable": False,
        "integrityFlags": "None",
        "payload": {"exchangeOrderId": 7, "priority": 3, "price": 1.5, "volume": 2,
                    "rawTypeName": "New", "rawTypeNumeric": 1,
                    "rawTypeIsKnownEnumMember": True,
                    "interpretedLifecycleAction": "Unknown",
                    "snapshotCompletionKnown": False, "derivedSide": "Bid"},
    }).encode("utf-8")


def write(tmp, data):
    p = os.path.join(tmp, "t.seg")
    with open(p, "wb") as fh:
        fh.write(data)
    return p


def main():
    print("###### segment_decoder self-tests ######")
    print()

    # 1. The published CRC-32C check value. If this is wrong every integrity claim is wrong.
    v = sd.crc32c(b"123456789")
    check("CRC-32C('123456789') == 0xE3069283", v == 0xE3069283, "got 0x%08X" % v)

    check("CRC-32C(b'') == 0x00000000", sd.crc32c(b"") == 0)

    with tempfile.TemporaryDirectory() as tmp:
        # 2. A well-formed container decodes cleanly.
        good = container() + frame(1, b'{"h":1}') + frame(2, envelope()) + frame(3, b'{"f":1}')
        r = sd.decode(write(tmp, good))
        check("well-formed container: no container error", r.container_error is None)
        check("well-formed container: no decode errors", not r.decode_errors, str(r.decode_errors))
        check("well-formed container: 0 CRC failures", r.crc_failures == 0)
        check("well-formed container: header+event+footer counted",
              r.frames.get("SegmentHeader") == 1 and r.frames.get("RawEvent") == 1
              and r.frames.get("SegmentFooter") == 1, str(dict(r.frames)))

        # 3. Bad container magic is refused, not silently parsed.
        r = sd.decode(write(tmp, container(magic=0xDEADBEEF) + frame(2, envelope())))
        check("bad container magic -> InvalidContainerMagic",
              r.container_error == "InvalidContainerMagic", str(r.container_error))

        # 4. Unsupported container version is refused.
        r = sd.decode(write(tmp, container(version=99) + frame(2, envelope())))
        check("container version 99 -> UnsupportedContainerVersion",
              (r.container_error or "").startswith("UnsupportedContainerVersion"),
              str(r.container_error))

        # 5. Unsupported frame version is refused.
        r = sd.decode(write(tmp, container() + frame(2, envelope(), version=7)))
        check("frame version 7 -> UnsupportedFrameVersion",
              any("UnsupportedFrameVersion" in e for e in r.decode_errors), str(r.decode_errors))

        # 6. A truncated final frame is reported, and what came before it is kept.
        blob = container() + frame(2, envelope()) + frame(2, envelope())
        r = sd.decode(write(tmp, blob[:-9]))
        check("truncated final frame -> NeedMoreData reported",
              any("NeedMoreData" in e for e in r.decode_errors), str(r.decode_errors))
        check("truncated final frame -> earlier frame still decoded",
              r.frames.get("RawEvent") == 1, str(dict(r.frames)))

        # 7. A corrupted CRC is caught, and NOT caught when verification is off - which is
        #    exactly why the report has to state CRC scope.
        bad = container() + frame(2, envelope(), crc=0x11111111)
        r = sd.decode(write(tmp, bad))
        check("CRC mismatch -> counted as a CRC failure", r.crc_failures == 1)
        check("CRC mismatch -> frame not counted as decoded", r.frames.get("RawEvent") is None)
        r = sd.decode(write(tmp, bad), verify_crc=False)
        check("CRC mismatch with verify_crc=False -> passes (scope matters)",
              r.crc_failures == 0 and r.frames.get("RawEvent") == 1)

        # 8. A frame whose payload is not JSON is reported, not crashed on.
        r = sd.decode(write(tmp, container() + frame(2, b"not json at all")))
        check("invalid JSON payload -> PayloadNotJson reported",
              any("PayloadNotJson" in e for e in r.decode_errors), str(r.decode_errors))

        # 9. An empty file is refused rather than read as an empty success.
        r = sd.decode(write(tmp, b""))
        check("empty file -> ShortContainerHeader", r.container_error == "ShortContainerHeader")

    print()
    if FAILURES:
        print("RESULT: FAIL (%d)" % len(FAILURES))
        for f in FAILURES:
            print("   %s" % f)
        return 1
    print("RESULT: PASS - all self-tests green")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
