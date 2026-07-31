# 13 — Audit Bundle Manifest

Audited commit: 43d458fab164c35055bd5378c78c8be7c14cd369 (43d458f)
R1 correction HEAD: 323e36a → this commit.

**Integrity design (corrected, R1 defect D4):** a manifest cannot safely embed its own hash — the
census `13_MANIFEST.md` recorded `2412557a…`/1395B for itself while the delivered file was
`4dbc8bc3…`/1838B, an unavoidable self-reference error. Integrity is now carried by a **detached
`SHA256SUMS.txt`** that hashes ALL bundle files — including this manifest — after finalization.

## Verify

```
cd docs/review/kdk_v4/audit_census_001
sha256sum -c SHA256SUMS.txt
```

This file lists WHAT is in the bundle; `SHA256SUMS.txt` lists the hashes. This file does NOT
contain its own hash by design.

## Bundle contents (census + R1)

Census artifacts 01–13, `raw/` transcripts, and the R1 correction set:
`KDK_CENSUS_001_R1_REVIEW.md`, `r1/{01,03,04,06,10}_*_R1.md`,
`KDK_UNBLOCK_001_OWNER_DECISION_PACK.md`, `KDK_D2_CONTRACT_FIT_001.md`, `SHA256SUMS.txt`.
