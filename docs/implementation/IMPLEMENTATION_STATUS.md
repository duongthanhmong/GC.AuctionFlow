# Implementation Status

| Field | Value |
|-------|--------|
| Current phase | **P0-07B** — Raw Event Recorder contracts/framing/segment/manifest/recovery |
| Probe version | **0.0.6** (unchanged) |
| RawEventRecorderSchemaVersion | **1.0.0** |
| RawEventContainerVersion | **1** |
| Trade / Dom / Mbo probe schemas | **1.0.1** / **1.0.0** / **1.0.1** (unchanged) |
| P0-07A | **PASS WITH LOCKED AMENDMENTS** |
| P0-07B | **CLOSEOUT AUDIT COMPLETE — pending review** |
| P0-07C | **NOT STARTED** |

## Phase checklist

| Phase | Status |
|-------|--------|
| P0-06 MBO lifecycle probe | **PASS WITH PLATFORM-SIDE OPERATIONAL LIMITATION** |
| P0-07A Raw Event Recorder architecture | **PASS WITH LOCKED AMENDMENTS** |
| P0-07B contracts, framing, segment, manifest, recovery | **CLOSEOUT AUDIT COMPLETE — READY FOR REVIEW** |
| P0-07C Trade/DOM adapters + operator settings | **NOT STARTED** |

## P0-07B locks

- Callback `RawEventDraft` vs writer `RawEventEnvelope` separation
- Writer-dequeue global ordering only (not exchange/callback total order)
- Mandatory CRC32C frames in versioned binary container
- Exact accounting model (callback / item / writer levels)
- Segments authoritative; manifest recoverable index
- MBO schema supported; primary-process recording blocked
- No ATAS callback integration in P0-07B

## Operational lock (P0-06D, retained)

**MBO subscription must not be enabled in the ATAS process used for primary GC analysis or trading.**
