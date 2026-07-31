# KDK-UNBLOCK-001 — Owner Decision Pack R3.1 (self-contained)

**Supersedes R1/R2/R3 packs (retained as historical).** `AUDITED_SOURCE_HEAD 43d458f`,
`R3_DELIVERABLE_COMMIT e751f6e`. Decide without opening any other file — full ADR decision text and
full 64-char hashes are inline. Both ADRs remain **`AWAITING_OWNER_APPROVAL`**.

---

## ADR-001 — authoritative core = trades + BBO + aggregated depth

- **Artifact:** `stage2_closure/adr/ADR-001-core.md`
- **sha256:** `2b5e529dc8fbe1397774db0889b2760c4ebaf83ee79a45975a3bc61a0f0f53fa`
- **Status:** `AWAITING_OWNER_APPROVAL`

**Decision (complete):**
> The recorder's authoritative core is exactly: executed trades (price, size, volume, vwap) [tmpl
> 150]; vendor aggressor [150]; best bid/offer + sizes + order counts [151]; aggregated order book
> depth [156]; EventTime (source_*) vs ReceiveTime (ssboe/usecs). Everything else is support,
> telemetry or context. **A recorder run that captures the core above with DataQuality = Ready is a
> complete run**, regardless of what the non-core surfaces did.

**Consequences (complete):** core capture must not be blocked/degraded by an MBO/options/session-stat
failure (those downgrade their own surface only); aggressor is the vendor field, not inferred while
present; template 156 is incremental (BEGIN/MIDDLE/END/SOLO), a single message is a delta.

**Evidence (id · full sha256):**
- `EV-PROBE-L1-TRADES` · `a4d097ace9343ddb96b3436c2ea046f0893566e0f06d8f448a437dd112ffe645`
- `EV-PROBE-AGG-BOOK` · `a4d097ace9343ddb96b3436c2ea046f0893566e0f06d8f448a437dd112ffe645`
- `EV-REBUILD-DIVERGES` · `752b07f7f532bdc4fc5c8fabe3600f6d5426f7ad4b1a14fde1f593a68f828efc`

**Exact schema checks (named):** `test_stage2_closure.py` — *"a core trade event validates"*,
*"float price is rejected"*, *"naive timestamp is rejected"*, *"a Trade with no aggressor fields is
rejected"*.

**Applicability:** path-independent (defines what a complete run captures). Gates `T-D2-SRC` on **both**
legs. **Unresolved objections:** none.

**Decision:** `[ APPROVE / REJECT / DEFER ]`

---

## ADR-003 — gap recovery absent → recorder fails closed *(direct-Rithmic as written)*

- **Artifact:** `stage2_closure/adr/ADR-003-gaps.md`
- **sha256:** `ac661eda7edf792bf8b91d122570526e8bbcdcacc67c75adf51017b1290a1cc5`
- **Status:** `AWAITING_OWNER_APPROVAL`

**Decision (complete):**
> The recorder owns gap detection. It must: (1) track last_sequence per (instrument, subscription
> scope) across reconnects; (2) on reconnect emit a SequenceDiscontinuity record (last/first sequence,
> delta, outage times, recovery_attempted = false); (3) mark the interval DataQuality = Invalid and
> segment contains_unrecovered_gap = true; (4) never interpolate or silently join the two halves;
> (5) emit DATA_INVALID on any request spanning an unrecovered gap. **Fail closed:** when
> discontinuity state cannot be determined (e.g. restart with no last_sequence), the interval is
> Invalid, not Ready.

**Consequences (complete):** a run with an unrecovered gap is still a valid artifact (just not Ready
over that interval); only a discontinuity **coincident with a connection event** is a gap; no
tolerance threshold is invented (DQ-001's tolerance is an unbound Registry item).

**Evidence:** `EV-RECONNECT-GAP` · `7eb0830fe714a6d28766e24613ca907b93682f384c8027d12f35198e4e18a00d`
(measured Δ3975 native sequences lost across a 12.4 s reconnect, unrecovered, no caller-visible signal).

**Exact schema checks (named):** `test_stage2_closure.py` — *"unrecovered gap + dataQuality Ready is
rejected"*, *"unrecovered gap with no discontinuity record is rejected"*, *"unrecovered gap without
DATA_INVALID reason code is rejected"*, *"claiming a recovery mechanism is rejected"*.

### Direct vs ATAS applicability — **critical**

ADR-003 as written is built on the **direct-Rithmic native sequence + subscription scope**. It cannot
be applied unchanged to the ATAS callback leg:

| | direct-Rithmic | ATAS callback |
|---|---|---|
| native sequence | present (51/51) | **absent** (`NativeSequenceAbsent`, `DepthToRawEventAdapter.cs:134`); recorder-local sequence is **writer dequeue order only** (`RawEventEnvelope.cs:8`) |
| ADR-003 mechanism | **directly implementable** | **not automatically implementable** — no native sequence to diff on reconnect |
| fail-closed principle | applies | **applies in principle**, but needs an explicit integrity addendum |

**ATAS integrity addendum required (does not yet exist).** It must define behaviour on: reconnect,
restart, unknown continuity, callback loss, queue loss, and missing native sequence — and ATAS **must
fail closed or report `Unknown/Invalid`; it must NOT fabricate exchange continuity from recorder-local
(dequeue-order) sequence.**

**Unresolved objections:** (a) native-sequence availability differs by leg (above); (b) DQ-001 gap
tolerance is an unbound parameter — ADR-003 does not need it (connection-coincident gap → Invalid needs
no threshold).

**Decision (direct path only):** `[ APPROVE / REJECT / DEFER ]`

---

## Architecture — `DEC-ARCH: C hybrid` recommended, **not approved**

Direct-Rithmic = authoritative research/calibration leg (native sequence). ATAS callbacks =
authoritative live-analysis leg (the runtime analysis actually runs in). Only parity-proven derived
features move between legs; raw direct-only features stay unavailable in ATAS; every output carries
acquisition-path + capability provenance; cross-leg reconciliation must never silently combine
incompatible inputs.

**Proposed WBS (approved by `DEC-WP04 = YES` for the *next control revision* only — it does NOT
auto-adopt any other part of the external v1.2 proposal):**
```text
WP-04a — direct-Rithmic research recorder
WP-04b — ATAS live-analysis recorder
WP-04c — capability/parity manifest and reconciliation
```

## Split production authority — **do not let direct-path approval authorize ATAS changes**

| authority ID | opens | requires |
|---|---|---|
| `AUTH-D2-DIRECT-SRC` | direct-Rithmic research writer changes | `ADR-001` + `ADR-003-DIRECT` APPROVED + `DEC-ARCH ∈ {A,C}` + `SEC-001` |
| `AUTH-D2-ATAS-SRC` | ATAS live-analysis writer changes | `ADR-001` + **an ATAS integrity addendum** + `DEC-ARCH ∈ {B,C}` + `AUTH-ATAS-DEPLOY` |

**`AUTH-D2-ATAS-SRC` stays `HOLD` until an ATAS integrity contract/addendum exists** — approving
direct-path ADR-003 does **not** silently authorize ATAS writer changes.

Document delivery (`DEC-DELIVER`: materialize v1.2 inputs + brief into the repo) is separate from all
of the above and needs no owner approval — it is a delivery, not an adoption.

---

## Owner response (stable IDs)

```text
ADR-001:            [ APPROVE / REJECT / DEFER ]
ADR-003-DIRECT:     [ APPROVE / REJECT / DEFER ]
DEC-ARCH:           [ A DIRECT / B ATAS / C HYBRID ]
DEC-WP04:           [ YES / NO / DEFER ]
AUTH-D2-DIRECT-SRC: [ AUTHORIZE / HOLD ]
AUTH-D2-ATAS-SRC:   [ HOLD ]  # until an ATAS integrity contract/addendum exists
```
No live-session or formal-acceptance authority is requested.
