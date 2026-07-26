# GC.AuctionFlow (GCAE)

Baseline repo for **GC AuctionFlow Engine v1.2**.

This is a measurement device project. Do not mix scoring, alerts, or trade recommendations unless explicitly required.

## Layout

```
GC.AuctionFlow/
├── docs/
│   ├── spec/          # Locked v1.2 final spec (source of truth)
│   ├── evidence/      # Phase 0 capability evidence
│   └── legacy/        # Frozen build fingerprints (deps.json)
├── legacy/
│   └── AosLevelEngine/  # Old source — audit only; do not merge into src/
├── src/               # New GCAE code (empty until approved plan)
├── tests/
├── artifacts/
└── README.md
```

## Baseline contents

| Path | Role |
|------|------|
| `docs/spec/GC_AuctionFlow_Engine_v1.2_Post_Cross_Review_Final_Spec_VI.md` | Spec v1.2 (locked) — **architecture, roadmap, module contracts, governance** |
| `docs/spec/GC_AuctionFlow_Engine_v1.3_Knowledge_Grounded_Spec_VI.md` | Spec v1.3 (companion) — **concept semantics, discriminators, measurement + calibration contracts, anti-pattern guards**. Source: KIM ĐẤU KINH. Precedence rules in v1.3 §0.2. GEX out of scope. |
| `docs/spec/GCAE_IMPLEMENTATION_BIBLE_SINGLE_SOURCE_FINAL.md` | Implementation Bible — **source layout, module contracts, snapshot/identity/revision lifecycle, threading, test contract, PLAN→DEPLOY workflow**. Does not override LOCKED code, `IMPLEMENTATION_STATUS.md`, v1.3 semantics, or v1.2 architecture. |
| `CLAUDE.md` | **Document authority + precedence + mandatory workflow.** Read first, every task. |
| `docs/evidence/CapabilityMatrix_ES_2026-07-20.json` | Phase 0 ES matrix |
| `docs/legacy/*.deps.json` | Legacy assembly dependency fingerprints |
| `legacy/AosLevelEngine/` | Prior Level Engine source (quarantined) |

## Working rules (Manual)

Allowed:

- Read files
- Propose plans
- Write code **after approval**
- Build / run tests
- Report errors

Not allowed without explicit approval:

- Change architecture unilaterally
- Add setup scaffolding unilaterally
- Implement all of GCAE in one shot
- Edit the spec to fit the code
- Drop requirements because an API is hard

## Git safety

Initial commit establishes the v1.2 baseline. Every subsequent change should be reviewable as a diff against this commit.
