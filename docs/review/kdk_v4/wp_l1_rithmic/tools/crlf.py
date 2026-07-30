import hashlib
import os
import subprocess

ROOT = r"C:\Users\LOQ\.gcae"
REL = "research/episode-dataset.jsonl"
CRLF = b"\r\n"
LF = b"\n"

work = open(os.path.join(ROOT, "research", "episode-dataset.jsonl"), "rb").read()
blob = subprocess.run(["git", "show", "HEAD:" + REL], cwd=ROOT, capture_output=True).stdout

for label, data in (("working copy", work), ("HEAD blob   ", blob)):
    crlf = data.count(CRLF)
    print("  %s: %9d bytes  CRLF=%d  bare-LF=%d  sha256=%s"
          % (label, len(data), crlf, data.count(LF) - crlf, hashlib.sha256(data).hexdigest()[:16]))

same = work.replace(CRLF, LF) == blob.replace(CRLF, LF)
print("  identical after LF normalisation: %s" % same)
print("  => the raw-byte difference is git's autocrlf filter, NOT a content change.")
