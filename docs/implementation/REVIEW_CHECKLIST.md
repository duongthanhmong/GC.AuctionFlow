# Review Checklist — P0-07B Closeout Audit

## Prior

- [x] P0-06 PASS WITH PLATFORM-SIDE OPERATIONAL LIMITATION
- [x] Tag `gcae-p0-06-mbo-lifecycle-pass` at clean baseline
- [x] P0-07A PASS WITH LOCKED AMENDMENTS
- [x] P0-07B implementation present (uncommitted)

## Closeout audit must pass

- [x] Diff scope: recorder + recorder tests + governance only
- [x] Versions locked (recorder 1.0.0 / container 1; probes unchanged)
- [x] Draft vs envelope separation; writer sequence = dequeue only
- [x] Closed payloads; constituents/provider completion false
- [x] Frame layout + mandatory CRC32C coverage documented/tested
- [x] Footer reserved in byte rotation; RecordsWritten = RawEvent only
- [x] Durable flush(true) before rename; hash then manifest
- [x] Accounting equations + terminal outcomes
- [x] Identity lifecycle first event of new epoch segment
- [x] Recovery stop/continue rules + quarantine failure reported
- [x] MBO lock; no subscribe; no adapters; no P0-07C
- [x] Master specification untouched
- [x] `dotnet clean/restore/build/test -c Release` — 0 errors, 0 warnings
