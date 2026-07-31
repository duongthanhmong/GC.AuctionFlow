# ADR-004 — Options identity is vendor-verified; OI and settlement are client-blocked

**Status:** ACCEPTED
**Date:** 2026-07-31
**Evidence:** `EV-EXPLOIT-OPTION-REFERENCE`, `EV-EXPLOIT-OPTION-QUOTES`,
`EV-UNMAPPED-OI-155-157`, `EV-PERBIT-ATTRIBUTION`, `EV-PERBIT-UNMAPPED`
**MRBS anchor:** §20 ROLL-004; §21 OPT-001, OPT-002, OPT-004; §23.2 `OPTIONS_UNUSABLE`
**KDK anchor:** §50 — Options is optional layer-7 context, never a necessary condition; GEX
is a module inside the Options pillar, not the pillar

## Context — two separate facts that must not be merged

**Fact 1 — identity and quotes are verified.** Filtering by vendor `product_code` and
`InstrumentType.FUTURE_OPTION`, then confirming every field against vendor reference data
before subscribing:

| symbol | put/call | strike | underlying | expiry | point value |
|---|---|---|---|---|---|
| `OGU6 C4120` | Call | 4120.0 | **`GCV6`** | 20260826 | 100.0 |
| `OGU6 C4035` | Call | 4035.0 | `GCV6` | 20260826 | 100.0 |
| `OGU6 P4120` | Put | 4120.0 | `GCV6` | 20260826 | 100.0 |
| `OGU6 P4035` | Put | 4035.0 | `GCV6` | 20260826 | 100.0 |

84 option-symbol ticks were acquired across all four.

**The option→futures mapping is not the identity.** The nearest OG expiry references
`GCV6` while the futures front month is `GCZ6`. MRBS ROLL-004 makes wrong mapping a hard
failure → `Options Invalid`.

**Fact 2 — open interest and settlement are on the wire and unreachable.**
`async_rithmic` 1.6.3 ships protobuf definitions for five streaming templates only
(150, 151, 156, 160, 161). Subscribing each of the 14 `UpdateBits` alone: **12 return
frames, all on templates the library cannot parse, and not one produces a tick.**

Decoded off the socket:

```
template 158  open interest  ->  OGU6 C4120 = 174   OGU6 C4035 = 42
template 155  settlement     ->  1299.4 / 20260730 / "final"
template 157  market mode    ->  "Open" / "Group Schedule"
```

## Decision

1. **Options identity, strike, expiry, put/call, point value and quotes are `Ready`.** They
   are read from vendor reference data and never inferred from the symbol string.
2. **`underlying_symbol` is always read per series.** No component may assume options belong
   to the futures front month. A mismatch between an option's declared underlying and the
   contract in use is `CONTRACT_MAPPING_INVALID` → `OptionsState = Invalid` (ROLL-004).
3. **Open interest, settlement and market mode are `CLIENT_BLOCKED`**, not `Unavailable`.
   The distinction is load-bearing: the data is entitled and delivered; the client is the
   blocker, and the blocker is fixable.
4. Until a parser exists, the recorder **preserves the raw frames** (ADR-007 / recorder
   spec §4) rather than discarding them. Preservation is not interpretation.
5. Any exposure, GEX, dealer-positioning or regime output that would need OI or settlement
   is `OptionsState = ContextOnly` at best, and `Unusable` if it needs a point-in-time OI
   value (OPT-002: missing snapshot/timestamp/model version → `Unusable`).

## Consequences

- KDK §50 invariant is untouched: with `GexContext = null`, phases 1–4 remain
  byte-identical. Nothing in this ADR gives Options a veto.
- OPT-004 requires dealer/GEX labels to carry formula, sign and assumption. With OI blocked,
  those labels cannot be produced honestly, so they are not produced.
- Fixing the blocker means obtaining or authoring protobuf definitions for templates
  152/153/154/155/157/158/162/163. **That is a separate authorised change**, not part of
  Stage 2 closure.

## What is NOT claimed

That GEX, flip, regime or dealer positioning can now be computed. Options analytics remain
`IMPLEMENTED_BUT_INVALIDATED` per the standing project status; this ADR changes the
*acquisition* facts only.
