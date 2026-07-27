# Proposal — open a minimal, optional OptionFlow/GEX read path in the DLL

> Status: **DRAFT FOR OPERATOR APPROVAL**. This document proposes edits; it does
> not itself change any authoritative spec. Nothing in `research/optionflow/` or
> the shipped `GC.AuctionFlow.dll` behaves differently until the operator accepts
> the v1.3 / IMPLEMENTATION_STATUS edits below.

## 1. Why this exists

`research/optionflow/` (the ResearchTools sidecar) now produces, per product, a
validated `artifacts/optionflow/<PRODUCT>/levels.json`:

- OI/GEX/volume walls, zero-gamma flip, regime, max-pain — **OI decode verified
  202/202 exact** against an independent EOD cache.
- Deep analytics — vanna/charm walls, term structure (0DTE vs 1-7DTE), expected
  move, IV skew, dealer gamma **profile curve** and positioning.

The sidecar is out-of-DLL by design (v1.2 §49.1/§49.2) and touches no locked
phase. The open question is only whether `GC.AuctionFlow.dll` may **read** this
file to show GEX context alongside AMT/order flow.

## 2. What currently blocks it (and must be amended to proceed)

- **`D-V13-002`**: GEX is OUT OF SCOPE; every `GexContext` field is `RESERVED =
  null`; all logic must work fully when GEX is absent.
- **v1.3 §44–50 (KDK Part V, "GEX day map")** and strategy families **Ch 76/77/79**:
  OUT OF SCOPE by operator direction.
- CLAUDE.md: do not create a GEX module/field/enum/GPS row; do not unlock `[C]`.

## 3. What this proposal DOES ask for (the minimum)

Open **one** narrow capability: a read-only `GexContext`, populated from the
sidecar file, **optional and OFF by default**, surfaced only as diagnostics.

Preserved invariants (unchanged):
- v1.3 §50 stays literally true: **GEX is never a necessary condition**; AMT +
  Order Flow alone still produce every conclusion. If the file is missing/stale,
  `GexContext = null` and behaviour is identical to today.
- No new strategy family. **Ch 76/77/79 remain OUT OF SCOPE.**
- No GPS row, no Episode/Thesis mutation, no alerts driven by GEX.
- Diagnostics-gated: nothing renders unless `ShowOptionFlowDiagnostics` is ON.

## 4. What this proposal explicitly does NOT ask for

- NOT the KDK "GEX map by day" (Ch 44–50) as a strategy.
- NOT GEX-conditioned entries, sizing, or thesis changes.
- NOT any auto-trading or GPS gating on GEX.
- NOT unlocking any `[C]` state.

These remain closed and can be separate, later proposals if ever wanted.

## 5. Proposed edits (for operator to make, in order)

1. **v1.3 `D-V13-002`** — from "GEX OUT OF SCOPE, all fields null" to:
   > `GexContext` is an **optional, read-only, diagnostics-gated** context sourced
   > from `artifacts/optionflow/<PRODUCT>/levels.json`. It is `null` whenever the
   > file is absent, stale, or diagnostics are off. It MUST NOT be a necessary
   > condition for any conclusion (v1.3 §50 unchanged). Strategy families Ch 76/77/79
   > remain OUT OF SCOPE.

2. **IMPLEMENTATION_STATUS.md** — add a new phase, e.g.
   `Phase 5A — OptionFlow Read Context (ObserveOnly, diagnostics-only)`, state
   `CODE/TEST PASS — LIVE ACCEPTANCE PENDING`, default OFF.

3. Only then build the DLL reader (§6).

## 6. DLL reader design (build target once §5 is accepted)

Namespace `GC.AuctionFlow.OptionFlow` (new folder under the existing one-DLL tree):

- `OptionFlowReader` — reads `levels.json` (+ `analytics`) with a shared,
  non-locking handle; validates `schema_version == gcae-optionflow-v1`; applies a
  freshness gate (`published_at_epoch` vs now); returns a nullable `GexContext`.
- `GexContext` (all nullable): spot, regime, flip, walls (call/put GEX+OI),
  max-pain, expected move, skew, dealer posture. Absence → `null`, never faked
  (mirrors the sidecar's "never display a value that was never VALID").
- Diagnostics rows only, behind `ShowOptionFlowDiagnostics` (default OFF). Wording
  avoids S/R / Entry / Long-Short per existing review checklist rules.
- Confluence (read-only): annotate when a GEX level sits within N ticks of an
  existing AMT/Composite level — display only, no Episode/GPS effect.

## 7. Test/acceptance plan

- Unit: reader parses a golden `levels.json`; missing/stale file → `null`; schema
  mismatch → `null` + diagnostic.
- Invariant guard (AP-style): with `GexContext = null`, every existing decision is
  byte-identical to pre-change (regression).
- Live acceptance: diagnostics rows match the sidecar file for one session before
  the phase is called FINAL.

## 8. Open decision for the operator

Approve §5 (edit v1.3 + IMPLEMENTATION_STATUS) to authorize building §6? Until
then the sidecar keeps producing data and the DLL stays unchanged.
