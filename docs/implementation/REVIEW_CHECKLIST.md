# Review Checklist — P0-08A Runtime Data Gate + Auction GPS Card

## Prior baseline

- [x] P0-07C3D PASS + locked (`gcae-p0-07c3d-live-trade-recorder-pass` @ `4543a79`)
- [x] Trade Recorder not reopened / redesigned
- [x] Master spec v1.2 untouched

## Implementation

- [x] ContractSnapshot + InstrumentMatch / Expiration / Roll states (evidence-gated)
- [x] RuntimeCapabilitySnapshot (conservative; MBO BLOCKED)
- [x] DataGateEngine Invalid / Degraded / Ready with stable reason codes
- [x] Immutable GcaeRuntimeSnapshot + thread-safe publisher
- [x] AuctionGpsCardViewModel separated from ATAS renderer
- [x] Visible Auction GPS Card via EnableCustomDrawing + OnRender
- [x] Transition ledger (change-only, bounded, in-memory)
- [x] Lifecycle: stop snapshot → release render → recorder → probes → base.OnDispose finally

## Explicitly not in this slice

- [x] No Classic TPO / Volume Profile / Composite Profile
- [x] No Structural Reference / Episode / Acceptance / FAR / AAC
- [x] No Entry / invalidation / targets / CFD / Telegram
- [x] No DOM/BBA/MBO recording; no MBO subscribe for GPS card
- [x] No Production Thesis or trading logic

## Verify

- [x] `dotnet clean/restore/build/test -c Release` (see report)
- [ ] Operator live acceptance on GCQ6/Rithmic (not auto-claimed)

## Recommendation

- [ ] Pending review + controlled live chart run → then tag if PASS
