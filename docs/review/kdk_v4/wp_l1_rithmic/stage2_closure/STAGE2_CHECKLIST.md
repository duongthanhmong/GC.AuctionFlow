# Stage 2 closure checklist

**Every `PROVEN` / `VERIFIED` / `ENFORCED` below cites an EvidenceId that resolves to a file
in this repository and re-hashes, or a named test that passes.** Re-run
`tests/test_stage2_closure.py` to check all of it — 47/47 at the time of writing.

Nothing here declares Stage 2 accepted. That is the reviewer's call, and four items are open.

---

## A. Acquisition facts — closed

| # | item | verdict | evidence |
|---|---|---|---|
| A1 | COMEX/NYMEX/CBOT/CME L1+L2 entitlement | **PROVEN** | `EV-PROBE-ENTITLEMENT` |
| A2 | Executed trades with vendor `aggressor` | **PROVEN** | `EV-PROBE-L1-TRADES` |
| A3 | BBO with sizes and order counts | **PROVEN** | `EV-PROBE-L1-TRADES` |
| A4 | Aggregated depth, incremental framing | **PROVEN** | `EV-PROBE-AGG-BOOK` |
| A5 | EventTime vs ReceiveTime, three clock domains | **PROVEN** | `EV-REBUILD-DIVERGES` |
| A6 | Option identity: underlying, strike, expiry, put/call, point value | **VERIFIED** | `EV-EXPLOIT-OPTION-REFERENCE` |
| A7 | Option quotes acquired | **VERIFIED** | `EV-EXPLOIT-OPTION-QUOTES` |
| A8 | Option→futures mapping is **not** the identity (`OGU6`→`GCV6`) | **VERIFIED** | `EV-EXPLOIT-OPTION-REFERENCE` |
| A9 | OI / settlement / market mode on the wire, client discards | **PROVEN** | `EV-UNMAPPED-OI-155-157`, `EV-PERBIT-UNMAPPED` |
| A10 | 12 of 14 `UpdateBits` return only unparseable templates, 0 ticks | **PROVEN** | `EV-PERBIT-ATTRIBUTION` |
| A11 | Reconnect + subscription replay | **PROVEN** | `EV-RECONNECT-GAP` |
| A12 | Gap recovery **absent**, 3,975 sequences unrecovered | **PROVEN** | `EV-RECONNECT-GAP` |
| A13 | Deterministic book rebuild **not possible** from 116+160 | **PROVEN** | `EV-REBUILD-DIVERGES` |
| A14 | Historical/live parity 76 of 77 | **PROVEN** | `EV-HIST-PARITY` |

## B. Architecture decisions — proposed, NOT in force

No owner approval has been recorded for any ADR. Each states a decision the evidence
supports; none is binding until the owner approves it.

| # | ADR | status |
|---|---|---|
| B1 | ADR-001 authoritative core = trades + BBO + aggregated depth | AWAITING_OWNER_APPROVAL |
| B2 | ADR-002 MBO is research telemetry, not a reconstructable book | AWAITING_OWNER_APPROVAL |
| B3 | ADR-003 gap recovery absent; recorder fails closed on its own | AWAITING_OWNER_APPROVAL |
| B4 | ADR-004 options identity verified; OI/settlement client-blocked | AWAITING_OWNER_APPROVAL |
| B5 | ADR-005 no seed hard-coded; registry is the single source | AWAITING_OWNER_APPROVAL |
| B6 | ADR-006 historical and live publish capability separately | AWAITING_OWNER_APPROVAL |
| B7 | ADR-007 preserve unparseable frames; preservation ≠ interpretation | AWAITING_OWNER_APPROVAL |

## C. Executable artifacts — built and tested

| # | item | verdict | test |
|---|---|---|---|
| C1 | 6 JSON Schemas, draft 2020-12, cross-file `$ref` resolving | **ENFORCED** | 11 metaschema/ref checks |
| C2 | Integer ticks; float price rejected | **ENFORCED** | *float price is rejected* |
| C3 | Aware-UTC only; naive timestamp rejected | **ENFORCED** | *naive timestamp is rejected* |
| C4 | Three independent version fields (VER-001) | **ENFORCED** | *missing version block is rejected* |
| C5 | Unrecovered gap ⇒ `Invalid` + `DATA_INVALID` + discontinuity record | **ENFORCED** | 4 negative cases |
| C6 | Recovery mechanism cannot be claimed | **ENFORCED** | *claiming a recovery mechanism is rejected* |
| C7 | `optionsState = Ready` unsatisfiable while OI is blocked | **ENFORCED** | *OptionsState=Ready while OI is CLIENT_BLOCKED is rejected* |
| C8 | OI value only when frame is `SCHEMA_BOUND` | **ENFORCED** | 2 negative cases |
| C9 | Generic wire decode can never be `SCHEMA_BOUND`; named fields rejected | **ENFORCED** | 2 negative cases |
| C10 | Symbol-string identity parsing is unrepresentable | **ENFORCED** | *identity parsed from the symbol string cannot be expressed* |
| C11 | `NOT_EVIDENCED` cannot be `AUTHORITATIVE_CORE`; no surface without an EvidenceId | **ENFORCED** | 2 negative cases |
| C12 | Unapproved parameter cannot carry a value | **ENFORCED** | *an unapproved parameter carrying a bound value is rejected* |
| C13 | 32-surface capability/evidence manifest, all EvidenceIds resolve and re-hash | **ENFORCED** | 7 manifest checks |
| C14 | Registry: 134 parameters, **0 Approved**; no schema inlines a default | **ENFORCED** | 3 registry checks |

## D. Still open — Stage 2 is not closable on these

| # | item | why it is open |
|---|---|---|
| **D1** | **Sustained-load stability** | longest measured window 90 s; no throughput, no soak, no back-pressure behaviour |
| **D2** | **Writer path** | no persistence implementation exists; the schemas define the contract, nothing writes to it |
| **D3** | **Calibration dataset** | requirements 1, 3, 5, 6 in `DATASET_CONTRACTS.md` unmet; no point-in-time dataset exists |
| **D4** | **SPEC_CONFLICTs SC-005..009** | MRBS forbids unilateral resolution; 4 of 5 need an owner decision, 2 need a dataset that does not exist |

## E. Items outside this package

| # | item | note |
|---|---|---|
| E1 | Vendor `.proto` for templates 152/153/154/155/157/158/162/163 | would unblock OI and settlement; **separate authorised change** |
| E2 | Whole-book snapshot request, if the vendor protocol has one | would reopen ADR-002; not in current evidence |
| E3 | KDK Ch 76/77/79 | `AWAITING_DOMAIN_SPEC`, unchanged |
| E4 | Task D (porting) | not started, not authorised |
| E5 | `SEC-001` credential rotation | still unresolved |
| E6 | `OBS-R8` — library logs cleartext password on failed login | unfixed upstream; probe-side redaction holds |

---

## Verdict this package supports

| scope | verdict |
|---|---|
| Futures acquisition feasibility | **PROVEN** — A1–A5, A11, A14 |
| Futures recorder production-ready | **NOT PROVEN** — D1, D2; and A12/A13 are structural blockers, not gaps in testing |
| Options acquisition identity + quotes | **VERIFIED** — A6, A7, A8 |
| Options OI / settlement / exposure | **CLIENT_BLOCKED** — A9; not `UNVERIFIED`, and not `Unavailable` |
| MBO full-book reconstruction | **PROVEN NOT POSSIBLE** on this path — A13 |
| Parameter calibration | **CANNOT START** — D3 |
| Stage 2 | **NOT ACCEPTED** — reviewer's call; D1–D4 open |
