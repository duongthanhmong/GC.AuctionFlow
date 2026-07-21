# History Capability Manifest — Schema (Prompt 5A)

SchemaVersion for HistoricalCapability.json: **1.3.0**  
TradingDateResolverVersion: **1.0.0**

## `sessionResolution` / `historyCapabilityManifest`

| Field | Type | Notes |
|-------|------|-------|
| `tradingDateResolverVersion` | string | Stamped on every export |
| `resolvedTradingDate` | string (yyyy-MM-dd) | From newest bar via resolver |
| `priorTradingDate` | string? | Prior among COMPLETE RTH sessions |
| `sessionRolloverHourEt` | int | From profile `TradingSessionRolloverLocalTime` |
| `earliestUsableTradingDate` | string? | Oldest COMPLETE TradingDate |
| `barsLoadedAtStart` | int | |
| `historyStartExchangeTime` / `historyEndExchangeTime` | DateTime UTC | |
| `historySpanDays` | double | |
| `completedRthSessionCount` | int | COMPLETE only |
| `completedOvernightSessionCount` | int | |
| `contractCode` / `securityId` / `expirationDate` | | |
| `contractIdentitySource` | enum | SECURITY_CODE \| REPLAY_MANIFEST \| CHART_SYMBOL_FALLBACK \| UNAVAILABLE |
| `contractIdentityConfidence` | enum | HIGH \| MEDIUM \| LOW \| NONE |
| `historyCapabilityStatus` | enum | SUFFICIENT \| PARTIAL \| INSUFFICIENT \| UNKNOWN |
| `sessionCoverageByDate` | map date→state | COMPLETE \| PARTIAL_COVERAGE \| NON_TRADING_DAY \| UNKNOWN_COVERAGE |
| `capabilities` | map | Per PDH_PDL / ONH_ONL / WeeklyHL / Composite / nPOC / RotationR |
| `periodAlignments` | array | Expected/Observed TradingDate + AlignmentStatus |

## Capability assessment

| Field | Notes |
|-------|-------|
| `status` | SUFFICIENT / INSUFFICIENT / … |
| `omissionReason` | INSUFFICIENT_HISTORY when optional level omitted |
| `required` | If true and insufficient → FailClosedEngine |
| `sourceTradingDate` | |
| `failClosedEngine` | bool |

## Profile requirements (InstrumentProfile)

- `TradingSessionIdentity.*` — FAIL STARTUP if any missing
- `HistoryCapabilities.PDH_PDL|ONH_ONL|WeeklyHL|Composite|nPOC|RotationR`
- `LevelEngine.NPocLookbackSessions`

## RotationR availability (5A supplement)

| Status | Meaning |
|--------|---------|
| `NOT_YET_AVAILABLE` | Before `Rotation.FreezeTime` (09:15 ET) on current TradingDate |
| `INSUFFICIENT` | At/after freeze but missing M1 true ranges / wrong TF — **no default R** |
| `AVAILABLE` | R computed and **frozen** (`IsFrozen=true`) |

Manifest fields: `rotationRAvailabilityStatus`, `rotationRTicksFrozen`.  
Capability: `failClosedInteraction=true` unless AVAILABLE.  
Gate: `RotationRAvailability.EnsureAvailableForInteraction` — Interaction Engine must not start with default R (`ResetTicks=max(floor,R)`, `ExitDistance=R`).
