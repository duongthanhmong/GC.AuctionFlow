"""Audit of research/episode-dataset.jsonl. Read-only.

This is the tool that produced raw/02_episode_dataset_audit.txt. Stage 1 ran it inline and
did not commit it, so the figure could not be re-derived. It is committed now.

Classification is by EpisodeId SHAPE, which is the only reliable discriminator here:
production ids are a pipe-delimited composite identity; fixture ids are `EP-<digits>`.

usage: python episode_dataset_audit.py [path]
"""
import collections
import datetime
import hashlib
import json
import os
import re
import sys

DEFAULT = r"C:\Users\LOQ\.gcae\research\episode-dataset.jsonl"
FIXTURE_ID = re.compile(r"^EP-\d+$")


def main():
    p = sys.argv[1] if len(sys.argv) > 1 else DEFAULT
    raw = open(p, "rb").read()
    print("###### episode-dataset.jsonl AUDIT ######")
    print("  path        : %s" % p)
    print("  size        : %d bytes" % len(raw))
    print("  mtime       : %s" % datetime.datetime.fromtimestamp(
        os.path.getmtime(p)).isoformat(timespec="seconds"))
    print("  sha256      : %s" % hashlib.sha256(raw).hexdigest())
    print("  sha256 (LF) : %s   <- compare this one against a git blob; the checkout is CRLF"
          % hashlib.sha256(raw.replace(b"\r\n", b"\n")).hexdigest())
    print()

    total = blank = malformed = 0
    malformed_lines = []
    ids = collections.Counter()
    shape = collections.Counter()
    combo = collections.Counter()
    fieldsets = collections.Counter()
    with open(p, encoding="utf-8", errors="replace") as fh:
        for n, line in enumerate(fh, 1):
            if not line.strip():
                blank += 1
                continue
            total += 1
            try:
                r = json.loads(line)
            except Exception:
                malformed += 1
                malformed_lines.append(n)
                continue
            eid = r.get("EpisodeId", "")
            ids[eid] += 1
            fieldsets[tuple(sorted(r.keys()))] += 1
            kind = ("fixture (EP-<n>)" if FIXTURE_ID.match(eid)
                    else "production (composite identity)" if "|" in eid
                    else "other")
            shape[kind] += 1
            combo[(kind, r.get("EpisodePolicyVersion"), r.get("PrimaryAuctionId"))] += 1

    print("=== counts ===")
    print("  non-blank lines   : %d" % total)
    print("  blank lines       : %d" % blank)
    print("  MALFORMED lines   : %d   %s" % (malformed, malformed_lines or ""))
    print("  distinct EpisodeId: %d" % len(ids))
    dups = {k: v for k, v in ids.items() if v > 1}
    print("  duplicate ids     : %d  (surplus rows: %d)"
          % (len(dups), sum(dups.values()) - len(dups)))
    print("  distinct field-sets: %d" % len(fieldsets))
    print()

    print("=== rows by EpisodeId shape ===")
    for k, n in shape.most_common():
        print("  %6d  %s" % (n, k))
    print()

    print("=== shape x policy x auction ===")
    for (kind, pol, auc), n in combo.most_common(20):
        print("  %6d  %-32s %-28s %s" % (n, kind, pol, auc))
    print()

    print("=== most duplicated ids ===")
    for k, v in sorted(dups.items(), key=lambda x: -x[1])[:10]:
        print("  x%-4d %s" % (v, k[:110]))


if __name__ == "__main__":
    main()
