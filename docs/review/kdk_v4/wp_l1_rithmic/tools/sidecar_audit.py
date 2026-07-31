"""Verify every .sha256 sidecar under .gcae against the file it claims to describe."""
import hashlib
import os
import re

ROOT = r"C:\Users\LOQ\.gcae"
HEX = re.compile(r"\b([0-9a-fA-F]{64})\b")
CRLF = b"\r\n"
LF = b"\n"


def main():
    sidecars = []
    for base, dirs, files in os.walk(ROOT):
        dirs[:] = [d for d in dirs if d != ".git"]
        for f in files:
            if f.endswith(".sha256"):
                sidecars.append(os.path.join(base, f))

    ok = mismatch = missing = unreadable = lf_ok = 0
    bad = []
    for s in sorted(sidecars):
        target = s[: -len(".sha256")]
        rel = os.path.relpath(s, ROOT).replace("\\", "/")
        if not os.path.isfile(target):
            missing += 1
            bad.append(("TARGET MISSING", rel, "", ""))
            continue
        m = HEX.search(open(s, encoding="utf-8", errors="replace").read())
        if not m:
            unreadable += 1
            bad.append(("NO HEX IN SIDECAR", rel, "", ""))
            continue
        claimed = m.group(1).lower()
        data = open(target, "rb").read()
        actual = hashlib.sha256(data).hexdigest()
        if actual == claimed:
            ok += 1
            continue
        # Does it match once CRLF is normalised back to LF? That would mean git's
        # autocrlf filter rewrote the bytes after the hash was taken.
        norm = hashlib.sha256(data.replace(CRLF, LF)).hexdigest()
        if norm == claimed:
            lf_ok += 1
            bad.append(("CRLF-MANGLED", rel, claimed[:16], actual[:16]))
        else:
            mismatch += 1
            bad.append(("MISMATCH", rel, claimed[:16], actual[:16]))

    print("###### .gcae SIDECAR HASH AUDIT ######")
    print("  sidecars found        : %d" % len(sidecars))
    print("  verified OK           : %d" % ok)
    print("  match only after LF   : %d" % lf_ok)
    # Conditional on purpose. A zero here means no file needed LF-normalisation to verify,
    # i.e. autocrlf did NOT rewrite any hashed byte. The old unconditional note read as though
    # it had, which is the opposite of what a zero shows.
    print("      %s" % ("-> autocrlf rewrote the bytes of these files on checkout; the hash was "
                        "taken before that happened" if lf_ok else
                        "-> zero: no file required LF-normalisation, so autocrlf did not rewrite "
                        "any hashed byte"))
    print("  genuine MISMATCH      : %d" % mismatch)
    print("  target file missing   : %d" % missing)
    print("  sidecar unparseable   : %d" % unreadable)
    print()
    by_kind = {}
    for kind, rel, c, a in bad:
        by_kind.setdefault(kind, []).append((rel, c, a))
    for kind, items in by_kind.items():
        print("=== %s (%d) ===" % (kind, len(items)))
        for rel, c, a in items[:15]:
            print("   %s" % rel + (("\n      claimed %s...  actual %s..." % (c, a)) if c else ""))
        if len(items) > 15:
            print("   ... and %d more" % (len(items) - 15))
        print()

    # Which file types are affected?
    print("=== affected target extensions ===")
    ext = {}
    for kind, rel, _, _ in bad:
        e = os.path.splitext(rel[: -len(".sha256")])[1] or "(none)"
        ext[(kind, e)] = ext.get((kind, e), 0) + 1
    for (kind, e), n in sorted(ext.items(), key=lambda x: -x[1]):
        print("   %-18s %-8s %d" % (kind, e, n))


if __name__ == "__main__":
    main()
