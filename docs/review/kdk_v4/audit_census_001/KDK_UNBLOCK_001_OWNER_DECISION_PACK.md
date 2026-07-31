# KDK-UNBLOCK-001 — Owner Decision Pack

**One place for every decision now blocking production.** Prepared at HEAD `323e36a`. Nothing here
is self-approved. Answer the numbered template at the end; no prose rewriting needed.

---

## 1. ADR-001…007 — recommend / consequence / evidence

All seven are `AWAITING_OWNER_APPROVAL` (verified: 7/7 in `stage2_closure/adr/`).

| ADR | subject | recommend | consequence if approved | evidence |
|---|---|---|---|---|
| 001 | authoritative core = trades+BBO+aggregated depth | **APPROVE** | fixes the recorder's core scope | `EV-PROBE-*`, 1531 tests |
| 002 | MBO = research telemetry, not reconstructable book | **APPROVE** | MBO never gates core; matches measured 3/21 rebuild | `EV-REBUILD-DIVERGES` |
| 003 | gap recovery absent → recorder fails closed | **APPROVE** | enables D2 fail-closed writer | `EV-RECONNECT-GAP` |
| 004 | options identity verified, OI client-blocked | **APPROVE** | keeps `optionsState=Ready` unsatisfiable until `.proto` | `EV-EXPLOIT-*`, `EV-UNMAPPED-OI` |
| 005 | no seed hard-coded; registry single source | **APPROVE** | locks G-CAL-001; 0/134 stays honest | `EV-PARAM-REGISTRY`, r1/06 |
| 006 | historical & live publish capability separately | **APPROVE** | prevents parity over-claim | `EV-HIST-PARITY` (76/77) |
| 007 | preserve unparseable frames | **APPROVE** | keeps OI/settlement recoverable later | `EV-UNMAPPED-OI-155-157` |

All seven are evidence-backed and internally consistent; the recommendation is **approve all seven**
so D2 can proceed. The owner may DEFER 004/007 without blocking D2 (they concern Options, not the
core recorder).

## 2. SPEC_CONFLICTs — owner-ruling vs data-blocked

From `SPEC_CONFLICT_REGISTER.md` + `CONFLICT_REGISTER_R2.csv` (CONF-002/003/004 are **retired** →
RECL-01/02/03; do not action them):

| id | needs | note |
|---|---|---|
| SC-005 (Rearm ANY vs geometric+temporal) | **owner ruling + data** | can't resolve without episode dataset |
| SC-006 (Acceptance 3×M1+POC vs evidence-groups) | **owner ruling + data** | same |
| SC-007 (DataQuality taxonomy) | **owner ruling** | pick canonical of 3 taxonomies |
| SC-008 (`score` vs `ResearchConfidence`) | **owner ruling** | naming only |
| SC-009 (Allow/Restricted vs Analysis-Only output) | **owner ruling** | ties to §41 CFD-split decision (§5 below) |
| CONF-001 (Session/timezone) | **owner ruling** | single 24h window is a CONFLICT |
| RECL-01/02/03 | reviewer reclassification | already downgraded; confirm |

MRBS line 62 forbids me resolving any of these. **4 need only a signature (SC-007/008/009, CONF-001);
2 also need a dataset (SC-005/006).**

## 3. Ratification / authorization

| item | status | blocks |
|---|---|---|
| 02A ratification | catalog PASS, adopted | — |
| **02B ratification** | **provisional** (`SUPERSESSION_REGISTER.md:144`) | `04A:58` makes it a hard dependency of Chặng 1–3 |
| 02D | coverage report, 93 rows | — |
| **04A authorization** | *"Nothing here is authorized for implementation"* (`04A:3`) | Chặng 4 / Phase C |

## 4. D1 sustained-load acceptance criteria — all **PROPOSED**, none approved

| metric | PROPOSED value | rationale |
|---|---|---|
| soak duration | **≥ 4 h continuous** RTH | vs current longest evidence = 160 s |
| throughput sustained | **≥ 2× peak observed** without backlog | headroom over live GC |
| drop policy | **0 silent drops**; every drop → `DATA_INVALID` segment | ADR-003 fail-closed |
| queue depth | bounded; **max < configured cap**, logged | back-pressure visible |
| writer latency | **p99 < 50 ms** enqueue→durable | PROPOSED |
| CPU / RAM | **CPU < 25%** one core; **RAM flat** (no unbounded growth) | leak guard |
| disk growth | **linear, predictable**; rotation verified | — |
| reconnect | **≥ 3 forced** reconnects survived; each → discontinuity record | ADR-003 |
| crash/restart | **1 kill -9**; recovery scan classifies partial segment | RecoveryScanner wired |
| discontinuity | **100%** of connection-coincident gaps flagged `containsUnrecoveredGap` | schema-enforced |
| schema validation | **100%** emitted segments validate vs the 6 schemas | quality gate |

**Every value above is PROPOSED for the owner to set, not approved.**

## 5. Recorder architecture — ONE decision required

The DLL currently ingests via **ATAS callbacks**; the Stage 2 schemas were written for the
**direct-Rithmic** path. `SegmentWriter` (binary container) serves the ATAS path; the schema's
fail-closed metadata contract serves the direct path. These are different layers (see
`KDK_D2_CONTRACT_FIT_001.md`). The owner must choose the **authoritative** recorder path:

| option | native sequence | runtime parity w/ ATAS | dataset suitability | op complexity | features available in ATAS runtime |
|---|---|---|---|---|---|
| **A. Direct-Rithmic recorder** | **YES** (proven 51/51) | separate process | **best** (full fields) | higher (own conn, entitlement, SEC-001) | **NO** — data not in the ATAS analysis runtime |
| **B. ATAS-callback recorder** | **NO** (`NativeSequenceAvailable=false`) | **native** | weaker (lost fields) | lowest (already wired) | **YES** — same runtime as analysis |
| **C. Hybrid capability-split** | direct for research, ATAS for live | both, labelled | **best of both** | highest (two paths + capability manifest) | **partial** — features tagged by source |

**Recommendation (not an approval): Option C (hybrid).** Rationale: research/calibration needs the
native sequence only the direct path gives (Option A), but production analysis runs in the ATAS
runtime where only Option B's data exists — so a single path forces a false choice. The Stage 2
`CapabilitySnapshot` already models per-source capability, which is exactly Option C's requirement.
**A parameter calibrated from direct-Rithmic data may only be used in the ATAS runtime if the
capability manifest proves the field exists on the ATAS path too** — this is the load-bearing rule
Option C makes explicit and A/B leave implicit. **The owner must decide; I do not approve C.**

---

## Owner response template (answer these, nothing else needed)

```
1. ADRs — approve all 7?           [ YES / NO / list exceptions: ______ ]
2. SC-007 DataQuality taxonomy:    [ Ready/Degraded/Invalid  /  5-state  /  other: ___ ]
3. SC-008 field name:              [ score  /  ResearchConfidence ]
4. SC-009 output surface:          [ Allow/Restricted+risk  /  Analysis-Only (§41) ]
5. CONF-001 session window:        [ keep single 24h  /  session template — spec: ___ ]
6. Ratify 02B provisional catalog? [ YES / NO ]
7. Authorize 04A as binding spec?  [ YES / NO ]
8. D1 acceptance values:           [ accept PROPOSED as-is  /  amend: ______ ]
9. Recorder architecture:          [ A direct / B ATAS / C hybrid ]
10. Execution brief 26ad6e23:      [ will commit to repo  /  delivered elsewhere — where: ___ ]
```

Answering 1 + 9 alone unblocks `KDK-D2-001` (the first live writer proof).
