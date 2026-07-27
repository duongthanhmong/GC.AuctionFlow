# OptionFlow output contract — `gcae-optionflow-v1`

Frozen interface between the Python sidecar (producer) and
`GC.AuctionFlow.dll` OptionFlowReader (consumer). The DLL MUST validate
`schema_version == "gcae-optionflow-v1"` and reject/treat-as-null anything else.

File: `artifacts/optionflow/<PRODUCT>/levels.json` (PRODUCT ∈ GC, ES, NQ).
Written atomically (tmp + replace); readers open shared, non-locking.

## Top level

| field | type | meaning |
|---|---|---|
| `schema_version` | string | must equal `gcae-optionflow-v1` |
| `product` | string | GC / ES / NQ |
| `underlying_symbol` | string | e.g. `GCQ6` (front-month future) |
| `spot` | number\|null | underlying future price F |
| `regime` | string\|null | `NEGATIVE_GAMMA` / `POSITIVE_GAMMA` |
| `selected_flip` | number\|null | zero-gamma flip price |
| `primary_expiry` | string\|null | ISO datetime of nearest expiry used |
| `published_at` | string | ISO-8601 UTC produced-at |
| `published_at_epoch` | integer | unix seconds — **freshness gate** |
| `data_health` | string | `LIVE` / `BLOCKED` |
| `coverage` | object | `{strike_count, oi_coverage_pct, gex_contract_coverage_pct}` |
| `levels` | array | see below |
| `analytics` | object\|null | see below |

## `levels[]` — drawable reference lines

Each: `{ level_id, level_type, scope, price, status, production_eligible, formula_version, detail }`

`status` ∈ VALID / STALE / HELD / BLOCKED / MISSING. Render only VALID; a level
that never had a VALID price is absent, never faked.

`level_type` values (v1):
`ATM_STRIKE`, `CALL_GEX_WALL`, `PUT_GEX_WALL`, `POSITIVE_NET_GEX_PEAK`,
`NEGATIVE_NET_GEX_PEAK`, `CALL_OI_WALL`, `PUT_OI_WALL`, `CALL_VOLUME_WALL`,
`PUT_VOLUME_WALL`, `GAMMA_ACTIVITY_PEAK`, `MAX_PAIN`, `TRUE_ZERO_GAMMA`.

`scope` ∈ `0DTE` / `SECONDARY_1_7_DTE`.

## `analytics` — panel/context (nullable, and each sub-block nullable)

| key | shape |
|---|---|
| `atm` | `{strike, call_price, put_price, straddle, straddle_move_to_expiry, atm_iv, dte, distance_from_spot}` |
| `vanna_wall` | `{strike, exposure}` |
| `charm_wall` | `{strike, exposure}` |
| `expected_move` | `{atm_iv, one_day_move, to_expiry_move, expiry_dte}` |
| `iv_skew` | `{put_iv, call_iv, skew, put_strike, call_strike}` |
| `term_structure` | `{ "0DTE": {net_gex,strikes,contracts}|null, "1_7DTE": {...}|null }` |
| `gamma_profile_curve` | `{curve:[{price,net_gamma}], slope_at_spot, net_gamma_at_spot}` |
| `dealer_positioning` | `{total_net_gex, total_net_dex, gross_gamma, posture}` |

## Consumer invariants (DLL)

1. `schema_version` mismatch OR `published_at_epoch` older than the freshness
   window ⇒ `GexContext = null` (+ a diagnostic), never a stale render.
2. `GexContext = null` ⇒ every existing GC.AuctionFlow decision is byte-identical
   to pre-OptionFlow (regression-guarded). GEX is never a necessary condition.
3. Any missing field ⇒ that sub-context is null; the rest still loads.
4. Units: GEX = USD per 1% underlying move; prices/strikes in index points;
   dealer sign = CALL_POSITIVE / PUT_NEGATIVE.

## Versioning

Additive fields keep `gcae-optionflow-v1`. Any breaking change (renamed/removed
field, changed units/sign) bumps to `gcae-optionflow-v2`; the DLL rejects
unknown majors rather than mis-reading them.
