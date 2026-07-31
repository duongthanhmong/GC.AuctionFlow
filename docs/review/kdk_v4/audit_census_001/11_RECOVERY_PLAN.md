# 11 — Recovery Plan

**Audited commit:** `43d458f`. This plan proposes; it authorises nothing.

## Critical path

```
Q2 approve ADRs ─┐
Q3 resolve SC   ─┼─→ D2 writer proof (code already present+wired: PROVE under load)
Q5 ratify 02B   ─┘        │
                          ▼
                    D1 sustained-load capture ──→ Stage 2 acceptance (Q4)
                          │
                          ▼
              LONG-HORIZON POINT-IN-TIME DATASET  ← the true bottleneck
                          │
                          ▼
              parameter approval (0/134) ──→ WP-05 Acceptance/FAR/AAC calibration
```

The critical path is **not** code. It is: **5 owner/reviewer signatures (Q2, Q3, Q4, Q5/Q9,
Q6/Q10) and 1 dataset.** Every downstream calibration is blocked on the dataset, and the dataset
is blocked on D1/D2 producing a `Ready` window longer than 160 s.

## Can proceed in parallel (no new authorisation)

- D2 writer **proof** — the code exists (`SegmentWriter`, `RecoveryScanner`); wire `RecoveryScanner`
  into the DLL path and evidence the fail-closed state machine under a real session. Read-only
  audit is done; this is the next build work, and it needs only the existing ADRs to be approved.
- Wiring `RecoveryScanner` to a production caller (GAP-06).
- Fixing the `IMPLEMENTATION_STATUS.md` staleness and the closeout distribution (CTR-02/03/07) —
  documentation, no code risk.

## Requires owner authorisation

Q2 (ADRs), Q3 (SPEC_CONFLICTs), Q4 (Stage 2 accept), Q5/Q9 (ratify 02A/02B/02D),
Q6/Q10 (authorise 04A), Q7 (vendor `.proto`), Q8 (Ch76/77/79 spec), plus GOV-Q1 (deliver brief).

## Dataset dependencies

WP-05 (Acceptance/FAR/AAC calibration), WP-07 (Options), and every parameter promotion depend on
a point-in-time dataset meeting all 6 `DATASET_CONTRACTS.md` requirements. **None exists.** This is
the single largest schedule risk.

## Acceptance gates

1. Stage 2 (D1–D4) → reviewer.
2. Each parameter → Decision Record (MRBS §44).
3. Each requirement → code+test+live evidence in 02B.

## The next smallest executable work package after this audit

**`WP-D2-PROVE`**: with ADR-003/007 approved, wire `RecoveryScanner` into the recorder path and
produce ONE real recorded session whose segments validate against the 6 schemas and whose
fail-closed state machine (`READY→GAP_DETECTED→RECOVERING→READY/DEGRADED`) is exercised by a real
reconnect. Scope: existing code + one live session. Output: the first schema-valid dataset segment.
This is the smallest step that converts D2 from `CODE_PRESENT` to `LIVE_EVIDENCED` and produces the
seed of the D3 dataset.

## Must NOT begin yet

- Parameter calibration / walk-forward (no dataset; 0/134 approved).
- Options analytics rebuild beyond spec (needs Q6/Q10 + vendor `.proto`).
- Ch76/77/79 strategy families (`AWAITING_DOMAIN_SPEC`).
- Any resolution of a `SPEC_CONFLICT` (MRBS line 62 forbids unilateral choice).
- CFD/execution bridge build (Phase 4 under `CANDIDATE-DEPRECATION`, pending Q3).
