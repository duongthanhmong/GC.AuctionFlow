# r1/01 — Executive Truth, corrected

**Supersedes the flagged claims in census artifact 01.** R1 HEAD `323e36a`, base `43d458f`.

## Corrected one-paragraph truth

The DLL **builds clean (0/0) and passes 1531 unit tests**, and the chain
`ATAS → recorder → AMT → runtime snapshot → ATAS` is **wired end to end**. The analysis core is
**largely built at module granularity** — the R3E matrix maps **23 of 25 modules to real code
symbols** — but it is **almost entirely untraced at requirement granularity** (02B is provisional;
its 591 `NOT_IMPLEMENTED` rows carry no source mapping and are `UNMAPPED_IN_02B`, not proven absent)
and **entirely uncalibrated** (0/134 parameters approved, 383 not-calibrated occurrences hold
classifiers to observations). It is a well-disciplined, largely-built skeleton that **refuses to
conclude** because it is uncalibrated by rule — not an empty one.

**Retracted from census 01:** "the domain is almost entirely unbuilt."

## The 10 Uncomfortable Truths — corrected

1. **Requirement traceability is near-zero, not code.** 1/679 requirements is `CODE_TESTED` in 02B,
   but that measures **02B population**, not code existence: 591 rows are `UNMAPPED_IN_02B` with no
   source column. The code exists (23/25 modules, 1531 tests); the requirement→symbol map does not.
2. **0/134 parameters approved; 132 not in code.** Unchanged — the calibration gate is real.
3. **"Nothing writes" is false** — writer exists (992 lines) and is wired. Unchanged.
4. **…but not D2-proven** — no soak run; `RecoveryScanner` un-wired. Unchanged.
5. **DLL ingress is ATAS, not direct Rithmic.** Unchanged — this is why the recorder-architecture
   decision (Owner Pack §5) is unavoidable.
6. **The progress tracker is 4 days stale** (says 1458; real 1531). Unchanged.
7. **The closeout distribution doesn't match its CSV** (589/2/1 vs 591/0/0/1). Unchanged.
8. **No owner-signed acceptance exists** — but WP-L1-02 carries a `TECHNICAL_REVIEW_VERDICT`
   (accepted/closed in the review narrative). "Nothing accepted" is now scoped to **formal
   owner-signed** acceptance only.
9. **The execution brief is a `SOURCE_AVAILABILITY_CONFLICT`, not proven-missing.** No repo doc
   claimed RECEIVED; the owner asserts it; this workspace has never held it. Both stand recorded.
10. **The bottleneck is a dataset + ~5 signatures, not a keyboard.** Unchanged, and now with a
    concrete unblock path (Owner Decision Pack).

## Acceptance taxonomy (Defect 8 resolution)

| level | what exists |
|---|---|
| `TECHNICAL_REVIEW_VERDICT` | WP-L1-02 (accepted/closed in review narrative) |
| `HANDOVER_COMPLETE` | Stage 2 Closure Package |
| `OWNER_APPROVED` | **none** — 7 ADRs `AWAITING_OWNER_APPROVAL` |
| `FORMALLY_ACCEPTED` | **none** |
| `NOT_ACCEPTED` | Stage 2 overall (D1–D4 open) |

"No acceptance exists" is true **only for `OWNER_APPROVED`/`FORMALLY_ACCEPTED`**; a technical
verdict and a handover do exist.
