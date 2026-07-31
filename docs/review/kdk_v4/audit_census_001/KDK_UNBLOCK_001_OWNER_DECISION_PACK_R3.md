# KDK-UNBLOCK-001 — Owner Decision Pack R3 (self-contained)

**Supersedes R1/R2 packs (retained as historical).** R3 HEAD `e373486`. Decide `ADR-001`, `ADR-003`,
`DEC-ARCH`, `DEC-WP04` without opening any other file — full text + hashes are inline below.

---

## ADR-001 — authoritative core = trades + BBO + aggregated depth

- **Artifact:** `stage2_closure/adr/ADR-001-core.md` · **sha256** `2b5e529dc8fbe139…`
- **Decision text (verbatim):** *"The recorder's authoritative core is exactly: executed trades
  (price, size, volume, vwap) [tmpl 150]; vendor aggressor [150]; best bid/offer + sizes + order
  counts [151]; aggregated order book depth [156]; EventTime (source_*) vs ReceiveTime (ssboe/usecs).
  Everything else is support, telemetry or context. A recorder run that captures the core above with
  DataQuality = Ready is a complete run."*
- **Evidence (id · sha256 · path):**
  - `EV-PROBE-L1-TRADES` · `a4d097ace9343ddb…` · `probe/probe_20260731T102133Z_results.csv`
  - `EV-PROBE-AGG-BOOK` · `a4d097ace9343ddb…` · same session
  - `EV-REBUILD-DIVERGES` · `752b07f7f532bdc4…` · `…E5_book_rebuild.json`
- **Supporting schema tests (named, not the 1531 aggregate):** `test_stage2_closure.py` —
  *"a core trade event validates"*, *"float price is rejected"*, *"naive timestamp is rejected"*,
  *"a Trade with no aggressor fields is rejected"*.
- **Scope/consequence:** fixes what the recorder must capture; gates `T-D2-SRC`. **Deferral blocks
  ALL paths** (direct, ATAS, hybrid) — nothing can define "a complete run" without it.
- **Unresolved objections:** none identified; the core is exactly what the probe evidence proves
  obtainable.
- **Decision:** `[ APPROVE / REJECT / DEFER ]`

## ADR-003 — gap recovery absent → recorder fails closed

- **Artifact:** `stage2_closure/adr/ADR-003-gaps.md` · **sha256** `ac661eda7edf792b…`
- **Decision text (verbatim, abridged to the 5 musts):** *"The recorder owns gap detection. It must:
  (1) track last_sequence per (instrument, subscription scope) across reconnects; (2) on reconnect,
  emit a SequenceDiscontinuity record (last/first sequence, delta, outage times, recovery_attempted
  = false); (3) mark the interval DataQuality = Invalid and segment contains_unrecovered_gap = true;
  (4) never interpolate or silently join the two halves; (5) emit DATA_INVALID on any request
  spanning an unrecovered gap."*
- **Evidence:** `EV-RECONNECT-GAP` · `7eb0830fe714a6d2…` · `…E6_reconnect.json` (measured Δ3975
  sequences lost across a 12.4 s reconnect, unrecovered, no caller-visible signal).
- **Supporting schema tests (named):** `test_stage2_closure.py` — *"unrecovered gap + dataQuality
  Ready is rejected"*, *"unrecovered gap with no discontinuity record is rejected"*, *"unrecovered
  gap without DATA_INVALID reason code is rejected"*, *"claiming a recovery mechanism is rejected"*.
- **Scope/consequence:** enables the D2 fail-closed writer; gates `T-D2-SRC`. **Deferral blocks all
  paths** — without it the writer cannot mark integrity and may emit continuous-looking corrupt data.
- **Unresolved objections:** the DQ-001 gap *tolerance* is an unbound parameter (0/134 approved);
  ADR-003 does not need it (connection-coincident gap → Invalid needs no threshold), so this is not
  a blocker.
- **Decision:** `[ APPROVE / REJECT / DEFER ]`

---

## Architecture recommendation — `DEC-ARCH: C hybrid` (PROPOSED)

Given the operating model (Rithmic/ATAS for GC analysis; ATAS is the live-analysis runtime; CFD
execution is separate), **C hybrid** is recommended. It means precisely:

- **direct-Rithmic** is the **authoritative research/calibration capture leg** (native sequence 51/51);
- **ATAS callbacks** are the **authoritative live-analysis input leg** (the runtime where analysis
  actually executes);
- **only parity-proven derived features** may move between legs;
- **raw direct-only features remain unavailable in ATAS** (e.g. native sequence);
- **every output carries acquisition-path + capability provenance**;
- **cross-leg reconciliation must never silently combine incompatible inputs.**

**This is a recommendation, not an approval.**

### If `DEC-ARCH = C`, the exact WP-04 amendment (apply only on `DEC-WP04 = YES`)

```
WP-04a — direct-Rithmic research recorder   (native-sequence capture; SEC-001 gated)
WP-04b — ATAS live-analysis recorder        (binary container, AUTH-ATAS-DEPLOY gated)
WP-04c — capability/parity manifest + reconciliation  (governs which derived features port)
```
This **replaces** `BAN_THAO` v1.2's single "WP-04 = direct-Rithmic writer". **Not canonical until
`DEC-WP04 = YES`.**

## Document delivery is separate from any decision

The v1.2 control inputs and the execution brief can be **materialized into the repo without owner
approval** — that is a delivery action, not adoption. Owner *adoption* of the WBS is a separate
decision. Neither is bundled into the ADR/architecture decisions above.

## Minimum-authority reminder (stable IDs)

`T-D2-SRC` (production writer change) opens only with **`ADR-001` + `ADR-003` APPROVED + `DEC-ARCH`
chosen**. Direct leg also needs `SEC-001`; ATAS leg `AUTH-ATAS-DEPLOY`; a live run `AUTH-LIVE-SESSION`;
sign-off `DEC-D2-ACCEPT`. **No live-session or acceptance authority is requested now.**

---

## Owner response (stable IDs)

```text
ADR-001:     [ APPROVE / REJECT / DEFER ]
ADR-003:     [ APPROVE / REJECT / DEFER ]
DEC-ARCH:    [ A DIRECT / B ATAS / C HYBRID ]
DEC-WP04:    [ YES / NO / DEFER ]   # required if C
AUTH-D2-SRC: [ AUTHORIZE / HOLD ]    # effective only when ADR-001, ADR-003, DEC-ARCH are satisfied
```
