# 09 — Gaps, Debt & Risks Register

**Audited commit:** `43d458f`.

## Structural gaps

| id | gap | evidence | consequence |
|---|---|---|---|
| GAP-01 | 591/679 requirements `NOT_IMPLEMENTED`; only **1** `CODE_TESTED` | 02B | the domain is specified, barely built |
| GAP-02 | 0/134 parameters approved; 132 not even present in code | Registry R3 | nothing calibrates; chain cannot reach `Ready` |
| GAP-03 | No calibration dataset; longest capture 160 s | `07_DATA_EVIDENCE` | D3 blocked; WP-05 blocked |
| GAP-04 | D1 sustained-load `NOT_STARTED` | no soak artifact | recorder production-readiness unproven |
| GAP-05 | DLL ingress is via ATAS, not direct Rithmic; ATAS path loses native sequence | `04_RUNTIME_WIRING` note A | the field-loss the whole WP-L1 arc documented |
| GAP-06 | `RecoveryScanner` (546 lines) has no production caller | `04_RUNTIME_WIRING` | recovery logic is tested but never runs in the DLL |
| GAP-07 | `StructuralAnalysisSnapshot` requirement symbol absent; impl is `GcaeRuntimeSnapshot` | CTR-06 | requirement-to-symbol traceability broken by rename |

## Risk — things that could be mistaken for working behaviour

| id | risk | evidence | why it's a risk |
|---|---|---|---|
| RISK-01 | 383 `NotCalibrated` guards | grep `src/` | a green build + 1531 passing tests can read as "it works"; it emits observations, not conclusions |
| RISK-02 | writer code present + wired | CTR-01 | reads as "D2 done"; it is `CODE_PRESENT`, not `LIVE_EVIDENCED` under load |
| RISK-03 | `IMPLEMENTATION_STATUS.md` stale (stops 2026-07-28) | CTR-07 | anyone reading it as current gets a false picture; 8 revisions of alignment matrix and the whole L1 arc are invisible |
| RISK-04 | rejected R1 alignment matrix still in repo | `mrbs_adoption_r1/` | future readers may cite void line numbers (already happened once) |
| RISK-05 | Options analytics `IMPLEMENTED_BUT_INVALIDATED` but code present | `OPT-001..006` | GEX/flip/EM values exist and are wrong; must never be read as truth |
| RISK-06 | 23 untracked legacy `Oac.*` files on disk | inventory | `D-P0-02-002` forbids building them; presence invites accidental build |

## Governance debt (awaiting owner/reviewer)

| id | item | recorded in |
|---|---|---|
| GOV-Q1 | Execution brief `26ad6e23…` never reached workspace (409 zips hashed, no match) | this audit; `stage2_closure/README.md` |
| GOV-Q2 | 7 ADRs `AWAITING_OWNER_APPROVAL` | `stage2_closure/adr/*` |
| GOV-Q3 | `SC-005..009` + `CONF-001` + `RECL-01/02/03` unresolved | `SPEC_CONFLICT_REGISTER.md`, `CONFLICT_REGISTER_R2.csv` |
| GOV-Q4 | Stage 2 acceptance (D1–D4) | `STAGE2_CHECKLIST.md` |
| GOV-Q5 | 02A/02B/02D ratification (02B provisional) | `SUPERSESSION_REGISTER.md:144`; `04A:58` |
| GOV-Q6 | 04A authorisation as binding spec | `04A:3` |
| GOV-Q7 | Vendor `.proto` for OI/settlement templates | ADR-004 / E1 |
| GOV-Q8 | Ch76/77/79 domain spec (28 reqs `AWAITING_DOMAIN_SPEC`) | 02B |
| GOV-Q9 | `SEC-001` credential rotation | `IMPLEMENTATION_STATUS.md:1778` |
