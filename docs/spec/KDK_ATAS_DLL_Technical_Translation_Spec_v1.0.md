# KDK ATAS DLL Technical Translation Specification

**Document ID:** KDK-ATAS-TTS-001  
**Version:** 1.0  
**Status:** Implementation baseline for audit and controlled development  
**Target:** GC futures analysis in ATAS using Rithmic data  
**Primary artifact:** ATAS-loadable C#/.NET analysis DLL  
**Runtime scope:** Analysis and visualization; no broker order placement in the baseline

---

## 0. Why this document exists

This document translates the KDK theory into an implementable software contract for an ATAS DLL. It is not a new trading method, a summary that replaces the source material, or permission to invent missing rules.

The final system must preserve the KDK reasoning chain:

```text
Data integrity
→ Auction map and location
→ Auction regime
→ Reference zone
→ Auction Episode
→ Acceptance or reacceptance
→ Executed Order Flow and Effort–Result
→ FAR / AAC / Value Rotation / Unresolved
→ Structural Analysis Snapshot
```

The DLL exists to analyze GC futures and present a reproducible, point-in-time KDK interpretation inside ATAS. It must not silently turn qualitative language into arbitrary hard-coded numbers, create signals from Order Flow without location, or let Options override acceptance shown by price.

---

## 1. Source authority and conflict policy

### 1.1 Mandatory source set

Every implementation, review, refactor, and bug fix must be checked against all three source documents:

1. `KDK_KIM_DAU_KINH_CHUYEN_SAU_OPTIONS(1)(1).md`  
   Canonical theory, evidence hierarchy, application logic, limitations, and intended trading workflow.
2. `KDK_Machine_Readable_Binding_Specification_v1.1.md`  
   Machine-readable binding, state models, deterministic behavior, analysis-only clarification, coverage audit, and implementation Definition of Done.
3. `KDK_Parameter_Registry_and_Calibration_Matrix_v1.0.md`  
   Parameter inventory, calibration ranges, normalization candidates, and approval states. Its numeric values are research seeds, not KDK truth and not production defaults.

If the project contains renamed copies, the team must record their exact current paths and content hashes. A filename match alone does not prove the contents are current.

### 1.2 Authority order

Use this resolution order:

1. An explicit, approved KDK Decision Record for the current `ConfigVersion`.
2. The semantic intent and invariants of the canonical KDK theory.
3. Normative deterministic bindings in MRBS v1.1.
4. Approved-for-production parameter registry entries.
5. Approved-for-research parameter entries when running Research mode.
6. Proposed seeds only as configurable research candidates.
7. Existing code has no authority merely because it already exists.

An approved Decision Record may resolve ambiguity, but it must name the source sections, impact, owner, version, and regression tests. It must not silently redefine KDK.

### 1.3 Conflict behavior

When sources or code disagree:

- Do not choose the easiest interpretation.
- Do not preserve current code by default.
- Do not make a numeric guess.
- Record `SPEC_CONFLICT_REVIEW_REQUIRED`.
- Identify the exact source sections and existing implementation involved.
- Create a Decision Record before changing production semantics.

### 1.4 Language discipline in code and UI

Runtime labels must distinguish:

- observed data;
- calculated features;
- classified state;
- research hypothesis;
- unavailable or unknown state.

Names such as `AbsorptionCandidate`, `ExpansionCandidate`, or `Unresolved` are valid. Names such as `InstitutionalBuyerConfirmed`, `DealerMustHedgeUp`, or `GuaranteedMagnet` are prohibited because the data does not authorize those conclusions.

---

## 2. Product mission, boundary, and non-goals

### 2.1 Mission

Build a deterministic ATAS analysis DLL that consumes GC futures data, reconstructs the KDK auction state, evaluates local auction Episodes, reads executed Order Flow at the correct location, and publishes transparent FAR/AAC/Value Rotation analysis with evidence, conflicts, missing data, and reason codes.

The product must help the trader answer:

1. Where is price in the relevant auction structure?
2. Is the market in Balance, Discovery, or Transition?
3. Which reference zone is currently being tested?
4. What is the Episode attempting to do?
5. Is activity being accepted outside, reaccepted inside, or unresolved?
6. Which side is aggressive, and is that effort producing result?
7. Is the valid structural interpretation FAR, AAC, Value Rotation, or No Valid Setup?
8. What evidence supports the state, what conflicts with it, and what is still missing?

### 2.2 Baseline runtime boundary

The baseline DLL is **analysis-only**:

- analyzes GC futures;
- renders KDK zones and states inside ATAS;
- may emit alerts and immutable analysis snapshots;
- does not place, modify, or cancel broker orders;
- does not manage account equity or position size;
- does not use CFD wicks to change the GC thesis;
- does not require a CFD feed;
- does not require Options data for the AMT + Order Flow core to run.

GC-to-CFD basis mapping, order execution, stop orders, position sizing, P&L management, and broker lifecycle belong to a separate execution layer unless a later approved Decision Record explicitly moves a defined subset into the DLL.

### 2.3 Meaning of “translate the whole theory”

The entire source theory must be accounted for, but not every chapter becomes a separate runtime engine. Every chapter must be classified as one of:

- `ANALYSIS_CORE` — required for the GC analysis pipeline;
- `CONTEXT_OPTIONAL` — useful context that cannot break core when absent;
- `RESEARCH_DEFERRED` — valid research topic without production authority yet;
- `EXECUTION_OUTSIDE_CORE` — trading/execution policy outside the analysis DLL;
- `HUMAN_PROCESS` — education, preparation, journaling, or review workflow;
- `NOT_APPLICABLE_TO_RUNTIME` — theory that informs language/governance but has no direct executable state.

No source chapter may remain unmapped. Full coverage means traceability, not 94 independent classifiers.

### 2.4 Explicit non-goals for the baseline

- Fourteen independent setup engines.
- Automatic trade execution.
- A generic multi-market framework before GC works correctly.
- A new charting platform outside ATAS.
- A machine-learning score that can override hard KDK gates.
- Dealer-position certainty inferred from public Options OI.
- Mandatory DOM/MBO, iceberg, pulling/stacking, sweep, or stop-detection dependencies.
- Retrofitting completed-session information into earlier decisions.
- Repeated rewrites of architecture without a documented gap.

---

## 3. Theory-to-runtime coverage map

| KDK chapters | Runtime disposition | Technical translation |
|---|---|---|
| 1–6 | `ANALYSIS_CORE` | Evidence hierarchy, auction language, location-first rule, observable/calculated/inferred separation, conditional thesis logic |
| 7–12 | `ANALYSIS_CORE` with some context labels | Deterministic TPO/VP, VAH/VAL/POC, IB, reference sources; developing shape/day type remain candidate/context |
| 13–15 | `ANALYSIS_CORE` | Composite lifecycle, value/POC migration, independent intraday and multi-session regime, OTF from finalized intervals |
| 16–19 | `ANALYSIS_CORE` | Episode lifecycle, acceptance/reacceptance evidence, FAR/AAC/Unresolved, Value Rotation context |
| 20–31 | `ANALYSIS_CORE` | Executed-trade ingestion, Bid/Ask/Unknown, Delta/CVD, Footprint features, Imbalance, Attempt memory, Effort–Result |
| 32–39 | `RESEARCH_DEFERRED` or `CONTEXT_OPTIONAL` | Sweep/stops/DOM/MBO/iceberg/pulling-stacking/black-box indicators/unfinished extremes; never core dependencies |
| 40 | `ANALYSIS_CORE` | Contract identity, roll state, session participation segments, reset policies |
| 41 | `CONTEXT_OPTIONAL` | Point-in-time event metadata; no automatic blackout or risk rule in core |
| 42–43 | `CONTEXT_OPTIONAL` / `RESEARCH_DEFERRED` | Futures OI and COT with publication lag and no intraday trigger authority |
| 44–50 | `CONTEXT_OPTIONAL` | External point-in-time Options snapshot interface; IV/term/skew/expected move/flow/exposure metadata; no acceptance authority |
| 51–58 | `ANALYSIS_CORE` orchestration | Top-down order, phase-dependent authority, Episode bridge, context alignment/conflict handling |
| 59–61 | `ANALYSIS_CORE` structural outputs | Thesis evidence, structural invalidation, target/obstruction map; broker stop/R-multiple policy stays outside core |
| 62 | `EXECUTION_OUTSIDE_CORE` | Account risk, size, drawdown, correlation, behavioral controls |
| 63–65 | `ANALYSIS_CORE` | Primary result classes: FAR, AAC, Unresolved; maturity is Candidate/Developing/Confirmed |
| 66–75 | `ANALYSIS_CORE` as primary class + attributes | Rotation or FAR/AAC with location/attempt/effort/composite attributes, not separate duplicated engines |
| 76–79 | `CONTEXT_OPTIONAL` | Options alignment to a valid core state; Support/Neutral/Conflict/Unavailable cannot promote blocked setup |
| 80–81 | `ANALYSIS_CORE` | Wrong-location block and explicit No Valid Setup/Unresolved outputs |
| 82 | `EXECUTION_OUTSIDE_CORE` with analysis provenance | GC remains analysis source; optional future bridge uses separate versioned contract |
| 83–88 | `HUMAN_PROCESS` plus audit export | UI checklists, decision snapshot, journaling fields; order/position management not core |
| 89–92 | `ANALYSIS_CORE` quality infrastructure | Fixed configuration, point-in-time replay, metrics, robustness, golden cases, case library exports |
| 93–94 | `HUMAN_PROCESS` | Training and explanation standards; inform tooltips/docs, not market classification |

### 3.1 Setup normalization rule

The runtime must publish one primary structural setup at a time:

```text
FAR | AAC | VALUE_ROTATION | NONE
```

Other theory labels become attributes, for example:

```text
TWO_ATTEMPT
COMPOSITE_EDGE
SESSION_VALUE_EDGE
HIGH_EFFORT_LOW_RESULT
LOW_EFFORT_HIGH_RESULT
OPTIONS_STABILITY_CONTEXT
OPTIONS_EXPANSION_CONTEXT
NEW_VALUE_PULLBACK
RESPONSIVE_EDGE_ENTRY_RESEARCH
```

An attribute cannot create a setup. `TWO_ATTEMPT` without reacceptance is not FAR. `STACKED_IMBALANCE` without a valid zone and Episode is not AAC. `OPTIONS_EXPANSION_CONTEXT` without accepted price discovery is not AAC.

---

## 4. Target solution architecture

The current repository must be audited before any restructure. The following is the required separation of concerns, not an instruction to create duplicate projects when equivalent boundaries already exist.

### 4.1 Logical layers

```text
ATAS/Rithmic input
  → ATAS adapter and capability discovery
  → canonical market-event normalizer
  → data quality and event ordering
  → deterministic KDK analysis engine
  → immutable AnalysisSnapshot
  → ATAS renderer / alerts / audit export
```

### 4.2 Recommended code boundaries

1. **Domain contracts**  
   Pure records, enums, state transitions, reason codes, units, and identifiers. No ATAS UI dependency.
2. **Deterministic engine**  
   Session, Profile, Composite/Regime, Zone, Episode, Acceptance, Order Flow, Effort–Result, Setup, and Snapshot orchestration.
3. **ATAS adapter**  
   Converts the exact installed ATAS API and Rithmic-delivered data into canonical events; discovers capabilities; handles lifecycle callbacks.
4. **Presentation**  
   Draws zones, labels, panels, and alerts from snapshots. It must not contain hidden classification logic.
5. **Verification**  
   Unit tests, golden replay fixtures, determinism tests, and build/deployment validation. Test code must map to a requirement or defect and must not leak into production output.

### 4.3 Dependency direction

- Domain and engine must not reference ATAS.
- ATAS adapter may reference domain/engine.
- Renderer consumes snapshots; it must not mutate engine state.
- Options adapter is optional and may reference only published Options contracts.
- No core engine may depend on CFD, broker, account, or position state.

### 4.4 Build artifact

The release must produce the minimum set of files required by the installed ATAS version. The audit must determine:

- exact target framework;
- exact ATAS assembly references and versions;
- architecture/platform target;
- dependency copying rules;
- output DLL name;
- ATAS indicator discovery/deployment location;
- whether a single assembly or adjacent dependency assemblies are required.

Do not guess ATAS API class names, callback signatures, or deployment paths in the engine. Bind them only in the adapter after inspecting the current project and installed references.

---

## 5. Input-first contract

Input truth is the first implementation gate. No classifier is valid until the adapter proves which historical and live fields are actually available in the current ATAS/Rithmic environment.

### 5.1 Minimum viable core input

The baseline AMT + Order Flow core requires:

- instrument and exact GC contract identity;
- exchange tick size and price-step validity;
- exchange/session timezone and a versioned session template;
- executed trades with event timestamp, price, quantity, and stable identity or deduplication key;
- aggressor side from a documented source, with `Unknown` preserved when classification is not reliable;
- historical trade/tick data sufficient to rebuild profiles and M1 Order Flow;
- live trade/tick updates;
- deterministic historical-to-live handoff;
- bar or interval finalization times for TPO brackets and completed M1 confirmation;
- source/capability status and data-gap information.

If ATAS provides only aggregated candle values for a period, the system must not claim it reconstructed tick-level Footprint, Bid/Ask, trade count, or Episode-local distribution unless the required underlying data is actually present.

### 5.2 Canonical event model

The exact C# shape may adapt to the current codebase, but semantics must be equivalent to:

```csharp
public readonly record struct CanonicalMarketEvent(
    string InstrumentId,
    string ContractId,
    DateTime EventTimeUtc,
    DateTime ReceiveTimeUtc,
    long SequenceOrStableKey,
    MarketEventKind Kind,
    long PriceTicks,
    long Quantity,
    AggressorSide Side,
    DataPhase Phase,
    string SourceId,
    string DataSchemaVersion);
```

Required enums include:

```text
MarketEventKind = Trade | Quote | Depth | SessionMarker | Correction
AggressorSide   = Buy | Sell | Unknown
DataPhase       = Historical | Replay | Live
```

Price must be converted once at the adapter boundary:

```text
price_ticks = deterministic_round(price / tick_size)
display_price = price_ticks × tick_size
```

Floating-point chart prices must never be used as identity keys or state boundaries inside the engine.

### 5.3 Time semantics

Every event must preserve:

- `EventTimeUtc`: market/source event time used for market ordering;
- `ReceiveTimeUtc`: time received by the platform/adapter;
- optional processing time for latency metrics only;
- exchange-local time derived from a versioned timezone rule;
- trading date assigned solely by `SessionEngine`.

Machine-local clock and local calendar date must not define the trading session.

### 5.4 Historical/live merge

When the DLL is attached, restarted, or reconnected:

1. enter `HistoricalWarmup` or `Rebuilding`;
2. load the configured history window;
3. normalize and order events;
4. deduplicate historical/live overlap by stable event identity or a documented deterministic composite key;
5. rebuild all dependent states in pipeline order;
6. record the source cutoff and live handoff point;
7. publish confirmed states only after every required dependency is ready.

The engine must not publish a confirmed setup while profiles, baselines, session mapping, or Episode history are only partially rebuilt.

### 5.5 Ordering, finalization, and late data

At an M1 decision cutoff `T`:

```text
accept events eligible for the bar by EventTime
→ apply configured late-event grace policy
→ finalize a specific bar revision
→ update profiles
→ update zones and Episode
→ update acceptance
→ update Order Flow and Effort–Result
→ classify setup
→ publish immutable snapshot tied to that revision
```

Late events after cutoff create a revision event. They must not silently rewrite a previously published decision. Intrabar calculations may publish `Candidate` or `Developing`; `Confirmed` uses finalized M1 evidence under the active config.

### 5.6 Input capability matrix

| Input/capability | Core authority | Missing behavior |
|---|---|---|
| Executed GC trades | Mandatory | Core invalid if unavailable |
| Price and quantity by trade | Mandatory | Affected profiles/flow invalid |
| Reliable contract/tick size | Mandatory | Block analysis |
| Session template/calendar | Mandatory | Block session-dependent outputs |
| Native aggressor side | Preferred | Use documented fallback; otherwise preserve Unknown |
| Bid/Ask quote stream | Conditional fallback/context | Degrade side classification if missing |
| Historical tick/trade stream | Mandatory for full rebuild | Warm-up incomplete; do not fake parity |
| DOM/depth | Optional research | Module unavailable; core continues |
| MBO/order identifiers | Optional research | MBO/iceberg modules unavailable |
| Futures OI | Optional context | `Unavailable` with availability timestamp |
| Event calendar | Optional context | `UnknownCalendar`; no fabricated event state |
| Options chain/snapshots | Optional context | `OptionsUnavailable`; AMT + OF continues |
| CFD quotes | Outside baseline | No effect on analysis core |

### 5.7 Data-quality snapshot

Every published analysis must reference a capability and quality snapshot containing at least:

```text
source identity
contract identity
session template version
tick size
historical/live availability
classified-volume ratio
unknown volume
duplicate count
out-of-order count
gap status
late-event count
last valid event time
required module readiness
optional module readiness
quality state and reason codes
```

Use distinct quality states such as:

```text
Ready | Limited | Caution | Unusable | Recovering
```

Data invalidity has the highest blocking authority.

---

## 6. Deterministic engine modules

Each module must have a documented input schema, unit semantics, state/output, missing-data behavior, reason codes, KDK references, unit tests, and golden replay case.

### 6.1 DataQualityEngine

**Inputs:** canonical events, source status, session mapping, capability discovery.  
**Outputs:** `DataQualitySnapshot`, module readiness, hard blocks.  
**Responsibilities:** duplicate/out-of-order/gap/latency/Unknown-side monitoring; historical/live capability distinction; recovery state.  
**Hard rule:** `Unusable` blocks all setup confirmation. Optional module failure must not automatically invalidate unrelated core modules.

### 6.2 SessionEngine

**Inputs:** UTC events, exchange timezone, versioned GC session/calendar template.  
**Outputs:** trading date, ETH/primary-session boundaries, session segment, IB window, holiday/maintenance state.  
**Hard rules:** no machine-local session logic; DST handled by exchange timezone; missing open invalidates IB-dependent output; ETH and primary-session levels remain provenance-distinct.

### 6.3 TPOEngine

**Inputs:** price presence over deterministic completed/developing brackets, row size, session.  
**Outputs:** TPO counts, TPO POC, VAH/VAL, developing/final state, structural candidates.  
**Hard rules:** bracket alignment, row origin, POC tie-break, and value-area expansion are deterministic and versioned. Final profiles are immutable; corrections create revisions.

Candidate-only context such as profile shape, day type, Single Prints, Excess, Poor High/Low must not become FAR/AAC dependencies unless later approved.

### 6.4 VolumeProfileEngine

**Inputs:** executed trade price/volume, profile scope and anchor reason.  
**Profile types:** Session, Composite, Episode; Event only when a point-in-time event anchor exists.  
**Outputs:** VP POC, VAH/VAL, price rows, HVN/LVN candidates, provenance.  
**Hard rules:** every profile has `ProfileId`, scope, row configuration, start/end reason, revision, and config version. No fixed-range profile selected after observing the result is valid input.

### 6.5 CompositeEngine

**Inputs:** finalized session profiles and value/POC relationship features.  
**Outputs:** composite lifecycle, composite profile, separation/reentry evidence.  
**Lifecycle:** `Candidate → Active → Separating → Closed/Invalid`.  
**Hard rules:** structural lifecycle, not fixed N-day grouping; raw profiles are not merged across contract roll without an approved adjustment/reset policy; all thresholds remain configuration with approval metadata.

### 6.6 AuctionRegimeEngine

Maintain separate outputs:

```text
IntradayAuctionRegime
MultiSessionAuctionRegime
```

Allowed semantic states:

```text
Balance
DiscoveryUp
DiscoveryDown
TransitionUp
TransitionDown
Mixed
Unknown
InsufficientData
```

Required evidence includes value overlap, POC/value migration, time and volume outside previous value, range extension, reentry count, close location, and new-value development. A price breakout alone cannot produce `Discovery`. Price outside old value without new value or reacceptance remains `Transition`.

### 6.7 ReferenceZoneEngine

Each zone must contain:

```text
ZoneId
SourceType and source IDs
LowerTicks / UpperTicks / CenterTicks
Role and timeframe class
CreatedAt and source decision time
Age and interaction history
Status / memory state
ConfigVersion
```

Core source candidates include session/composite VAH/VAL/POC, IB high/low, overnight high/low, HVN/LVN edge, and versioned event/manual zones created before interaction.

Roles are distinct:

```text
UpperBoundary | LowerBoundary | Center | Extreme | Corridor
```

The engine must treat a zone as a place to open a question, not a support/resistance prediction. Zone merge retains all provenance. Touch count is recorded but never hard-coded as “more touches means weaker.”

### 6.8 AuctionEpisodeEngine

An Episode begins when executed price actually intersects an eligible zone. Approach is preparation, not Episode start.

Minimum state namespace:

```text
Dormant
Approaching
Interacting
TestingOutside
Reentered
DevelopingOutside
AcceptedOutside
ReacceptedInside
RetestingBoundary
ResolvedFAR
ResolvedAAC
Unresolved
Expired
InvalidData
```

Every transition records timestamp, triggering event/revision, raw evidence snapshot, reason codes, and config version. Start anchor is immutable. A new Episode is not created for every candle. Episode close/expiry reasons are explicit.

### 6.9 Attempt and Rearm

An attempt is an independent outside test within an Episode. A prolonged test is not repeatedly counted.

Rearm must require a configured combination of:

- at least one geometric condition, such as reentry depth or center traversal;
- at least one time or structural reset condition;
- optional delta/local-profile reset only when formally bound.

Each attempt stores its own raw effort and result vectors. Attempt number never creates setup authority. `TWO_ATTEMPT` is an attribute only after valid comparison and the required FAR/AAC state.

### 6.10 AcceptanceEngine

Acceptance is an evidence classifier, not a candle boolean.

Required raw evidence groups:

1. **Time** — outside/inside duration and finalized interval counts.
2. **Activity** — volume, trade count, and their shares.
3. **Structure** — local POC and local value position/migration.
4. **Geometry** — excursion, reentry depth, boundary interaction, close side.
5. **Persistence** — retest behavior, hold time, reclaim failure, repeated flips.

Output states:

```text
Unknown
DevelopingInside
DevelopingOutside
RejectedOutside
AcceptedOutside
ReacceptedInside
Conflicted
Unresolved
InvalidData
```

The classifier must publish raw evidence, active thresholds with approval states, reasons, and missing fields. One threshold or one close cannot confirm acceptance. Strong Order Flow cannot promote `Unknown` acceptance to confirmed. Conflicted or unresolved acceptance blocks confirmed FAR/AAC.

### 6.11 OrderFlowEngine

**Primary aggregation:** tick-driven canonical trades into deterministic M1 bars, plus Episode-local windows.  
**Outputs:** Bid/Ask/Unknown volume, Delta, trade count, average trade size, Footprint rows, Imbalance features, CVD families, classified-volume ratio.  
**CVD families:** ETH, primary session, Episode, and optional rolling window with separate reset policies.

Hard rules:

- `Delta = AskVolume - BidVolume` only for classified volume;
- Unknown remains separate;
- data quality reduces the authority of Delta;
- Imbalance formula, neighbor rule, minimum volumes, zero denominator, stack rule, price step, and finalized-bar policy are explicit configuration;
- intrabar output is preview/candidate;
- Order Flow at the wrong location returns `OF_AT_WRONG_LOCATION` and cannot create setup.

### 6.12 EffortResultEngine

The engine stores raw vectors before classifying.

Effort candidates:

```text
directional volume
absolute delta
total volume
trade count
average trade size
execution rate
repeated tests
```

Result candidates:

```text
directional excursion
net/close progress
hold at extreme
reentry or outside persistence
local POC/value migration
next state progress available at decision time
```

Normalization must be conditioned on instrument, contract, session segment, bar duration, and volatility regime. Future follow-through may label research outcomes later; it cannot be consumed by an earlier live decision.

Allowed broad states:

```text
HighEffortHighResult
HighEffortLowResult
LowEffortHighResult
LowEffortLowResult
InsufficientBaseline
Unknown
```

Effort–Result away from a valid location/Episode is context only.

### 6.13 SetupEngine

Production classification uses hard gates, evidence vectors, and reason codes. A numeric score is research-only unless formally approved and can never override a hard block.

#### FAR hard gates

- valid pre-existing boundary zone;
- valid Episode and outside test;
- no accepted-outside state opposing FAR;
- geometric reentry followed by evidence of reacceptance inside;
- failed attempt/retest to restore the outside auction, according to maturity policy;
- Order Flow/Effort–Result supports actual progress toward the old auction or at least does not conflict at confirmation;
- data and required dependencies valid.

Maturity:

```text
FAR_CANDIDATE → FAR_DEVELOPING → FAR_CONFIRMED
```

Any conflict, expiry, or invalid data must be explicit.

#### AAC hard gates

- valid pre-existing boundary zone and outside test;
- accepted-outside evidence;
- local activity/POC/value organized outside according to the configured maturity level;
- reclaim of the old value fails or outside retest holds;
- completed M1 evidence restores the accepted direction for confirmation;
- no reaccepted-inside state;
- data and required dependencies valid.

Maturity:

```text
AAC_CANDIDATE → AAC_DEVELOPING → AAC_CONFIRMED
```

#### Value Rotation hard gates

- Balance regime at the relevant horizon;
- stable value/POC with no accepted break;
- valid edge/deviation location, not middle-of-value chasing;
- responsive behavior produces result toward center;
- target path to POC/center is structurally meaningful;
- any opposite-edge extension remains conditional.

#### Blocking precedence

```text
Invalid data
→ invalid session/contract/config
→ no valid zone/location
→ unresolved/conflicted Episode or acceptance
→ opposing acceptance
→ insufficient confirmed evidence
→ no valid primary setup
```

Options and setup attributes cannot turn a block into allow.

### 6.14 OptionsContextEngine

Options is an optional point-in-time context interface, not a hidden dependency.

If implemented, every snapshot requires:

- exact underlying futures contract;
- expiry/DTE/horizon;
- observed timestamp and availability timestamp;
- quote-quality state;
- OI availability policy;
- model/formula/version;
- expected-move method;
- exposure sign assumptions or explicit unsigned concentration;
- sensitive zones with source, age, horizon, invalidation, and confidence.

Context states may include:

```text
DataUnusable
Neutral
StabilityCandidate
ExpansionCandidate
EventPremium
ExpiryDominant
FlowShift
Mixed
Unavailable
```

Alignment is separate:

```text
Support | Neutral | Conflict | Unusable | Unavailable
```

Options may enrich UI, add a conflict reason, or later affect execution policy after testing. It cannot confirm acceptance, create FAR/AAC, or stop the core from running when absent.

### 6.15 AnalysisSnapshotPublisher

The publisher emits immutable, revision-aware snapshots only after the engine pipeline has processed the same atomic input cutoff.

---

## 7. Analysis output contract

The exact serialization may follow the current project, but every decision snapshot must preserve equivalent information:

```json
{
  "snapshot_id": "stable-id",
  "analysis_time_utc": "point-in-time",
  "bar_revision": 0,
  "instrument": "GC",
  "contract": "exact-contract",
  "session_id": "versioned-session-id",
  "algorithm_version": "semver",
  "config_version": "semver",
  "data_schema_version": "semver",
  "capability_snapshot_id": "stable-id",
  "data_quality": "Ready|Limited|Caution|Unusable|Recovering",
  "engine_state": "AnalysisReady|...",
  "auction_regime": {
    "intraday": "...",
    "multi_session": "...",
    "relationship": "Aligned|Mixed|Unknown"
  },
  "reference_zone": {
    "zone_id": "...",
    "source": "...",
    "role": "...",
    "lower_ticks": 0,
    "upper_ticks": 0,
    "status": "..."
  },
  "episode": {
    "episode_id": "...",
    "state": "...",
    "attempt_number": 0,
    "raw_metrics_ref": "..."
  },
  "acceptance": {
    "state": "...",
    "evidence_groups": [],
    "missing_evidence": [],
    "raw_metrics_ref": "..."
  },
  "order_flow": {
    "classified_volume_ratio": 0.0,
    "effort_result": "...",
    "raw_metrics_ref": "..."
  },
  "options_context": {
    "state": "Unavailable",
    "alignment": "Unavailable",
    "snapshot_id": null
  },
  "setup": {
    "primary": "FAR|AAC|VALUE_ROTATION|NONE",
    "maturity": "Candidate|Developing|Confirmed|Conflicted|Expired|None",
    "direction": "...",
    "attributes": [],
    "reason_codes": [],
    "conflicts": [],
    "invalid_when": [],
    "missing_evidence": []
  },
  "structural_map": {
    "nodes": [],
    "obstructions": []
  }
}
```

Baseline output does not require entry price, CFD price, stop order, position size, account value, P&L, broker permission, or trade management state.

### 7.1 Required reason-code behavior

Every `Blocked`, `Restricted`, `Conflicted`, `Unknown`, `Unresolved`, or `Invalid` state requires machine-readable reasons. Free text may explain but cannot replace reason codes.

Minimum families:

- data/config/session/contract invalid;
- profile provenance missing;
- no valid zone/location;
- Episode unresolved/expired/invalid;
- acceptance developing/conflicted/opposes setup;
- Order Flow wrong location/degraded;
- insufficient baseline/warm-up;
- Options unusable/unavailable/conflict;
- no valid setup;
- specification conflict/review required.

---

## 8. ATAS visualization and operator behavior

Presentation must show what the engine knows now, not decorate hindsight.

### 8.1 Required visual layers

1. **Structural zones** — session/composite value edges, POC/center, IB/overnight reference where enabled, with source and lifecycle.
2. **Active Episode** — current zone, state, attempt number, outside excursion, and elapsed time.
3. **Acceptance state** — Developing/Accepted/Reaccepted/Conflicted/Unresolved with missing evidence.
4. **Order Flow state** — classified volume quality, Effort–Result, and supporting/conflicting features.
5. **Primary KDK result** — FAR/AAC/Value Rotation/None plus maturity.
6. **Reason panel** — supporting reasons, blocks, conflicts, and data status.
7. **Structural destinations** — nearest center/POC/edge/obstruction as analysis nodes, not promised targets.

### 8.2 Visual truth rules

- Candidate and Confirmed must be visually distinct.
- Developing profiles/zones must be visually distinct from finalized values.
- Expired or invalid states must remain auditable but not look active.
- Historical decisions must not visually move without a recorded revision.
- Rendering cannot recompute or reinterpret market logic.
- Alerts should fire on meaningful state transition IDs, not every tick repaint.
- A missing optional module must display `Unavailable`, not zero or neutral evidence.

### 8.3 Operator-facing compact summary

The default summary should answer, in order:

```text
DATA → REGIME → LOCATION → EPISODE → ACCEPTANCE → ORDER FLOW → KDK STATE → WHY / WHAT IS MISSING
```

Avoid an indicator dashboard that gives unrelated numbers equal visual authority.

---

## 9. Configuration and parameter governance

### 9.1 No proposed seed in hidden code

All thresholds, windows, row sizes, equality tolerances, timeouts, minimum sample sizes, and normalizations must be externalized into a versioned config with metadata.

Each parameter requires:

```text
ParameterId
Name
Module
Value and unit
SourceType: KDK_CONVENTION | PROPOSED_SEED | DATA_DERIVED
ApprovalStatus
KDK chapter references
MRBS references
Owner/proposer
Rationale
Effective ConfigVersion
Sensitivity test reference
```

Allowed approval states:

```text
Proposed
UnderReview
ApprovedForResearch
ApprovedForProduction
Rejected
Replaced
Deferred
```

`ApprovedForResearch` is not `ApprovedForProduction`.

### 9.2 Research and production modes

- **Research mode:** may run explicitly selected `ApprovedForResearch` or named `Proposed` configurations; output must visibly carry the status.
- **Production analysis mode:** may use only `ApprovedForProduction` values for decisions labeled production-ready.
- Missing required approved configuration must produce a configuration block, not a fallback guess.

### 9.3 Normalization

Absolute volume, Delta, trade count, range, and speed must not be compared across all sessions and regimes without normalization.

Candidate methods include percentile, robust Z-score, and session-segment baseline. The chosen method must specify:

- training/baseline window;
- minimum sample size;
- session segment/time bucket;
- volatility regime;
- online update policy;
- outlier policy;
- warm-up behavior;
- contract-roll behavior.

### 9.4 Versioning

Keep separate:

```text
AlgorithmVersion
ConfigVersion
DataSchemaVersion
```

Every snapshot and event must reference all applicable versions. Threshold changes never reuse the same `ConfigVersion`.

---

## 10. Lifecycle, recovery, and concurrency

### 10.1 Lifecycle states

```text
ColdStart
HistoricalWarmup
Rebuilding
AnalysisReady
Degraded
Recovering
Invalid
Stopped
```

The DLL must handle attachment mid-session, ATAS restart, Rithmic reconnect, historical/live overlap, missing segments, contract change, config change, and late corrections.

### 10.2 Recovery rules

- Restored state must identify its source snapshot and config.
- If input history and config are available, replay rebuild is the authority.
- No stale confirmed setup survives a contract/config/session incompatibility.
- Reconnect must not double-count overlapping trades.
- Recovery cannot publish until dependent state is coherent.

### 10.3 Concurrency rules

- One deterministic ordering controls engine mutations.
- Profile, Episode, Acceptance, and Order Flow read the same atomic cutoff.
- Thread scheduling must not change output.
- UI rendering operates on immutable snapshots.
- Same event stream + same versions must produce the same domain-event sequence and snapshot hash.

---

## 11. Verification strategy without file sprawl

Testing is mandatory, but throwaway infrastructure is not a deliverable.

### 11.1 Rules for test artifacts

- Every persistent test maps to a KDK/MRBS requirement or a confirmed defect.
- One-off probes must not be committed as unexplained production files.
- Existing test infrastructure must be reused before creating a new project or script.
- Generated `bin/obj`, logs, dumps, screenshots, and temporary exports are not source deliverables.
- A task may create a focused fixture only when it becomes a named golden case or is removed before completion.
- Do not repeatedly build after cosmetic edits; implement the bounded task, run the smallest relevant tests, then perform one full build/review gate.

### 11.2 Mandatory unit-test areas

- price-to-tick conversion and tick-grid boundaries;
- exchange timezone, DST, session boundary, holiday, missing open;
- TPO bracket alignment, POC tie-break, value-area expansion ties;
- VP provenance and late-trade revision;
- Composite lifecycle and roll behavior;
- zone merge with provenance;
- valid and invalid Episode transitions;
- Attempt rearm boundary conditions;
- Acceptance confirmed/conflicted/unresolved cases;
- Unknown-side volume and classification quality;
- Imbalance zero denominator/missing neighbor/minimum volume;
- completed-M1 confirmation and no look-ahead;
- Effort–Result baseline/warm-up;
- wrong-location Order Flow block;
- Options unavailable while core remains functional;
- deterministic historical/live handoff and reconnect deduplication.

### 11.3 Golden replay cases

At minimum:

1. Standard FAR: outside test → reentry → reacceptance → failed outside retest → progress inside.
2. Standard AAC: accepted outside → pullback → failed reclaim → restored progress outside.
3. False FAR: geometric reentry while local value/POC remains outside.
4. False AAC: strong breakout/Delta followed by sustained reacceptance of old value.
5. Unresolved: repeated flips and mixed local value.
6. Strong Order Flow in the middle of value blocked by location.
7. Data gap during an Episode transitions to invalid and never confirms.
8. Restart/reconnect rebuild produces identical event and snapshot hashes.

Golden fixtures must be point-in-time event streams, not screenshots after the outcome.

### 11.4 Build/deployment gate

A release candidate is valid only when:

- solution builds with the documented command and installed ATAS references;
- no unapproved warnings indicate runtime incompatibility;
- automated tests pass;
- the DLL loads in the target ATAS version;
- historical and live paths both run;
- one controlled replay matches live-equivalent output for the same input;
- no extra production files exist without documented purpose;
- release artifact names and versions are recorded.

---

## 12. Implementation milestones

These are end-to-end milestones, not invitations to fragment work into dozens of prompts. Each Claude Code task should complete one bounded milestone, run its relevant verification, and review the resulting diff before reporting.

### Milestone 0 — Truthful repository and capability audit

Inventory the existing project without code changes. Establish actual build state, ATAS bindings, data paths, current module coverage, hard-coded seeds, dead/test-only code, and the project’s exact position on this specification.

### Milestone 1 — Deterministic input foundation

Complete the real ATAS/Rithmic adapter path through canonical events, contract/tick/session identity, historical/live handoff, deduplication, event ordering, lifecycle, capability snapshot, and data-quality blocking. Verify with deterministic input tests and one baseline build.

### Milestone 2 — Auction map foundation

Complete deterministic TPO/VP, finalized/developing semantics, Composite lifecycle, dual-horizon Auction Regime, and provenance-rich Reference Zones. Render the structural map in ATAS and verify historical/replay consistency.

### Milestone 3 — Episode and acceptance

Complete Episode state machine, zone interaction memory, independent Attempt/Rearm, local Episode profile, Acceptance evidence vector, conflicts, expiry, and UI state. Add FAR/AAC/unresolved golden cases without setup promotion yet where evidence is incomplete.

### Milestone 4 — Order Flow and primary KDK classification

Complete M1 aggregation, Bid/Ask/Unknown, Delta/CVD, Imbalance configuration, Effort–Result normalization, wrong-location blocks, and hard-gated FAR/AAC/Value Rotation classification. Publish full immutable snapshots with reason codes.

### Milestone 5 — ATAS operator surface and release gate

Complete non-repainting visualization, transition alerts, compact reason/missing-evidence panel, audit export, restart/reconnect behavior, full regression, deployment documentation, and final DLL artifact.

### Milestone 6 — Optional context, only after core is proven

Add point-in-time event, OI/COT, Options snapshots, or research microstructure modules through capability-gated interfaces. Each module must prove incremental value and must not make core unavailable when missing.

---

## 13. Mandatory unknowns for the initial audit

The following must be discovered from the current repository and environment. They must not be guessed in implementation prompts:

1. Current solution/project layout and which files are production, test, experiment, or dead.
2. Installed/target ATAS version and API assemblies.
3. Target .NET framework/runtime and platform architecture.
4. Actual ATAS callbacks or subscriptions currently used for historical and live data.
5. Whether historical tick trades, Bid/Ask classification, trade count, and price-level volume are truly available.
6. Historical/live overlap semantics and event identity fields available from ATAS/Rithmic.
7. Current session/trading-date implementation and GC session template source.
8. Current profile implementation and whether it uses raw trades or chart aggregates.
9. Current contract-roll behavior.
10. Current config sources and all hard-coded parameters.
11. Existing state machines and whether UI code contains hidden business logic.
12. Current build command, DLL output, and deployment procedure.
13. Current tests and whether they validate production logic or only prototypes.
14. Existing Options or CFD code and whether it belongs in the analysis core.
15. Uncommitted work and user-owned changes that must be preserved.

---

## 14. Definition of Done for a module

A module is not “done” because a class exists or a test compiles. It is done only when:

1. its source KDK chapters and MRBS requirements are named;
2. input schema, units, timestamp semantics, and capabilities are explicit;
3. output enums/states and transition rules are explicit;
4. missing/invalid/degraded behavior is explicit;
5. no-look-ahead behavior is explicit;
6. every parameter has source and approval status;
7. reason codes and raw evidence are emitted;
8. deterministic unit tests pass;
9. at least one golden replay or end-to-end fixture covers the module;
10. historical/live behavior is equivalent where the declared capability permits;
11. recovery/restart behavior is defined;
12. the output is visible or consumable through the AnalysisSnapshot;
13. no unrelated framework, duplicate engine, or throwaway file was introduced;
14. the implementation diff was reviewed against all three KDK source documents;
15. same input + same versions produces the same state/event/snapshot result.

---

## 15. Workflow lock for future implementation tasks

Every Claude Code implementation prompt must include:

1. **Current context** — repository state and the last verified milestone.
2. **Final goal** — deterministic GC KDK analysis DLL for ATAS/Rithmic.
3. **Source authority** — exact paths of all three KDK documents plus this translation specification.
4. **Task scope** — one bounded end-to-end milestone or defect, including what is out of scope.
5. **Input truth** — the exact available data/capabilities the task may use.
6. **Target behavior** — inputs, states, outputs, reason codes, and theory references.
7. **Acceptance criteria** — build/tests/replay/UI evidence and no-look-ahead requirements.
8. **Anti-sprawl constraints** — reuse existing structure; no speculative framework; no unrelated files.
9. **Review requirement** — inspect the final diff, map it back to KDK/MRBS, report residual gaps and assumptions.

Claude Code must receive a complete task and finish it before review. Do not split a coherent module across many conversational micro-prompts unless a real blocker is discovered.

---

## 16. Final acceptance of the DLL baseline

The baseline KDK DLL is acceptable when it can, from real supported ATAS/Rithmic GC inputs:

- rebuild a deterministic structural auction map;
- show developing versus finalized profiles without hidden hindsight;
- identify and track reference-zone Episodes;
- classify acceptance, reacceptance, conflict, and unresolved states from auditable evidence;
- aggregate reliable Order Flow and explicitly preserve Unknown volume;
- compare Effort with Result at valid locations;
- publish hard-gated FAR, AAC, Value Rotation, or None with maturity and reasons;
- remain functional when Options, DOM/MBO, OI, COT, and CFD data are absent;
- recover across restart/reconnect without double counting;
- reproduce the same snapshots from the same event stream and versions;
- render the result in ATAS as a usable analysis tool rather than a collection of disconnected indicators;
- identify every unimplemented theory chapter by an explicit coverage status instead of silently ignoring it.

The goal is not maximum code volume. The goal is faithful, observable, testable translation of KDK into a focused ATAS analysis instrument for GC.
