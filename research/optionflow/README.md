# GCAE OptionFlow Sidecar

Personal, single-user research sidecar that pulls the CME/COMEX option chain
(GC, ES, NQ) **directly from Rithmic** — no CME web scraping — computes GEX/DEX
by strike with Black-76, and writes a self-describing `levels.json` per product
into the repo's `artifacts/optionflow/<PRODUCT>/`.

It is **not** installed into ATAS. Per v1.2 §49.1/§49.2 it lives under the
ResearchTools umbrella (tools/tests are never deployed to ATAS), so the single
`GC.AuctionFlow.dll` artifact rule (`D-P0-02-002`) is untouched. Whether the DLL
is allowed to *read* these artifacts is a separate, spec-gated decision
(`D-V13-002`) that is NOT taken here.

## Why Rithmic and not CME

The first reference (`ArcticfoxEngineering`) scraped `www.cmegroup.com/CmeWS/...`
for EOD settlements. That host is unreachable from this machine (TLS reset) and
adds a fragile dependency. The second reference (`SeachainsOption V9`) has zero
HTTP endpoints — it takes strike / put-call / expiry / **open interest** straight
from Rithmic's market-data feed. `open_interest` is simply one bit in Rithmic's
`RequestMarketDataUpdate.update_bits`. One Rithmic connection serves the option
chain today and (optionally) MBO order flow later.

We reuse the open-source `async_rithmic` library (its protobuf schema, login,
subscribe, reconnect). We do **not** copy any proprietary Seachains code — all
option math here (Black-76, max-pain, zero-gamma, GEX/OI/volume walls) is public.

## Data flow

```
Rithmic (your own 30-day account)
  ├─ RequestSearchSymbols   → enumerate option contracts for GC/ES/NQ
  ├─ RequestReferenceData   → strike, put_call, expiry, underlying, point value
  └─ RequestMarketDataUpdate(update_bits = OPEN_INTEREST | LAST_TRADE | BBO ...)
         → open interest (template 158) + live quote
        │
        ▼
   Black-76 per-strike gamma/delta × OI × multiplier
        │
        ▼
   walls / zero-gamma / max-pain / daily range  →  artifacts/optionflow/<P>/levels.json
```

## Layout

```
research/optionflow/
  gex/
    black76.py        # pricing + greeks + implied vol (pure python, public math)
    contracts.py      # Rithmic option-symbol parsing (OGQ6 C4080 -> GC/call/4080)
    engine.py         # per-strike GEX/DEX aggregation, walls, zero-gamma, max-pain
    levels_writer.py  # emit the levels.json contract (status/eligibility/coverage)
  rithmic/
    client.py         # async_rithmic wrapper: login, search, reference, subscribe
    collector.py      # orchestrates a snapshot cycle and calls the engine + writer
  tests/
    fixtures/         # real machine outputs copied in as golden inputs
    test_*.py
  run_snapshot.py     # CLI entry: one-shot or looped snapshot publisher
  requirements.txt
```

## Setup (you run these — classifier blocks me from running pip here)

```powershell
cd "C:\Users\LOQ\Downloads\New folder (2)\GC.AuctionFlow\research\optionflow"
py -3.12 -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
pytest -q            # offline tests, no Rithmic needed
```

## Credentials

Never in the repo. Create `C:\Users\LOQ\.gcae\rithmic.env`:

```
RITHMIC_USER=<your Rithmic user id>
RITHMIC_PASSWORD=<your Rithmic password>
RITHMIC_SYSTEM_NAME=<the System you pick in Seachains' login dropdown>
```

> Placeholders only. **Never** commit a real user id, password, token, key or
> certificate to the repo, to a scan pattern, a log, or a report (see `SEC-001`).
> The values live only in `C:\Users\LOQ\.gcae\rithmic.env`, which `.gitignore` excludes.

`app_name`/`app_version` are not separate credentials — `async_rithmic` supplies
defaults. If `RITHMIC_SYSTEM_NAME` is blank, `run_snapshot.py --list-systems`
prints every system your account can log into so you can copy the exact name.
