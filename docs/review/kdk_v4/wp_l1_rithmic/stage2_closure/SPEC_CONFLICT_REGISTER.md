# SPEC_CONFLICT register

MRBS v1.1 control rule (line 62): *"Khi phần v1.0 và Addendum v1.1 khác nhau, **không tự chọn
một bên**. Hệ thống phải trả về `SPEC_CONFLICT_REVIEW_REQUIRED` cho đến khi có decision
record."*

**Therefore this document does not resolve anything.** It records each conflict, states what
evidence bears on it, and marks it awaiting an owner decision. Resolving one of these
unilaterally would be exactly the failure the rule exists to prevent — and several of them
are domain-semantic, where KDK outranks both me and the implementation.

Four conflicts were already registered in `mrbs_adoption_r1/MRBS_V1_1_CONFLICT_REGISTER.csv`
(CONF-001..004). The five below were raised in the 2026-07-31 ruling and are **additional**;
none is a duplicate.

---

| id | topic | positions | evidence available | status |
|---|---|---|---|---|
| **SC-005** | Rearm condition | MRBS v1.0: `ANY` condition. Registry: at least one geometric **plus** one temporal/structural | none — no captured episode dataset | `SPEC_CONFLICT_REVIEW_REQUIRED` |
| **SC-006** | Acceptance criteria | MRBS: three M1 bars **and** local POC. Registry: minimum evidence groups, local POC optional | none — requires M1 aggregation over a real session | `SPEC_CONFLICT_REVIEW_REQUIRED` |
| **SC-007** | `DataQuality` taxonomy | Three taxonomies in circulation: `Ready/Degraded/Invalid` (MRBS §18), `READY/LIMITED/CAUTION/UNUSABLE/RECOVERING`, and an Options-specific one | **acquisition side settled** — see note | `PARTIALLY_CONSTRAINED` |
| **SC-008** | Confidence field name | JSON v1.0 uses `score`; addendum requires `ResearchConfidence` | none — naming decision | `SPEC_CONFLICT_REVIEW_REQUIRED` |
| **SC-009** | Output surface | v1.0 emits Allow/Restricted + risk + execution; addendum proposes Analysis-Only | none — scope decision, Product Owner authority | `SPEC_CONFLICT_REVIEW_REQUIRED` |

## Note on SC-007 — the only one this package touches

This closure package uses **MRBS §18 `Ready | Degraded | Invalid` verbatim** for
`DataQuality`, and **MRBS §18 `CapabilityState`** verbatim for capability, because §18 is the
clause that governs the acquisition layer and it is unambiguous there.

Two extension values were added to `CapabilityState` and are **declared as extensions** in
`common.defs.schema.json` rather than smuggled in:

- **`CLIENT_BLOCKED`** — the vendor delivers the surface and this client library discards it.
  MRBS has no term for this. `Unavailable` would be factually wrong: the data is entitled,
  present on the wire, and was decoded (`EV-UNMAPPED-OI-155-157`). The distinction determines
  whether the fix is a vendor conversation or a client change.
- **`NOT_EVIDENCED`** — no resolvable EvidenceId. Never upgraded silently; the schema forbids
  pairing it with `AUTHORITATIVE_CORE`.

**This does not resolve SC-007.** The competing five-value and Options-specific taxonomies
remain unadjudicated, and if the owner adopts one of them, the two extensions above must be
re-mapped. Recorded here so that re-mapping is a known task rather than a discovery.

## What would close each of these

SC-005, SC-006 need a point-in-time dataset with provenance — which per `DATASET_CONTRACTS.md`
does not exist and cannot be substituted with fixtures. SC-007 needs an owner decision on the
canonical taxonomy. SC-008 is a naming decision. SC-009 is Product Owner scope authority.

**None of the five is closable by evidence produced in this work package.**
