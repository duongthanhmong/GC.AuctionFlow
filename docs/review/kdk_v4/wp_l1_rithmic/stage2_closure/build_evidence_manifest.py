"""Build the capability/evidence manifest: every surface -> a resolvable EvidenceId.

An EvidenceId is only minted for an artifact that EXISTS in this repository and hashes
successfully. A surface whose evidence file is missing is emitted with an empty EvidenceId
and a capability state of NOT_EVIDENCED - it is never silently upgraded.

This is the mechanism that makes "PROVEN" checkable rather than asserted: the test suite
re-resolves every EvidenceId and fails if any pointer is dangling or any hash has drifted.

usage: python build_evidence_manifest.py
"""
import csv
import hashlib
import io
import os
import sys

sys.dont_write_bytecode = True

HERE = os.path.dirname(os.path.abspath(__file__))
WP = os.path.abspath(os.path.join(HERE, ".."))
REPO = os.path.abspath(os.path.join(WP, "..", "..", "..", ".."))

# EvidenceId -> (repo-relative artifact, what it demonstrates)
# Every path is checked; nothing is minted for a file that is not there.
EVIDENCE = {
    "EV-PROBE-ENTITLEMENT": (
        "docs/review/kdk_v4/wp_l1_rithmic/probe/probe_20260731T102133Z_results.csv",
        "COMEX/NYMEX/CBOT/CME level 1 and level 2 entitlement, flag=1"),
    "EV-PROBE-L1-TRADES": (
        "docs/review/kdk_v4/wp_l1_rithmic/probe/probe_20260731T102133Z_results.csv",
        "executed trades with aggressor, vwap, volume, trade_size on template 150"),
    "EV-PROBE-AGG-BOOK": (
        "docs/review/kdk_v4/wp_l1_rithmic/probe/probe_20260731T102133Z_results.csv",
        "aggregated order book, update_type SOLO/BEGIN/END/SNAPSHOT_IMAGE"),
    "EV-EXPLOIT-OPTION-CATALOG": (
        "docs/review/kdk_v4/wp_l1_rithmic/probe/exploit_20260731T105819Z_results.csv",
        "option discovery by product_code + InstrumentType.FUTURE_OPTION"),
    "EV-EXPLOIT-OPTION-REFERENCE": (
        "docs/review/kdk_v4/wp_l1_rithmic/probe/exploit_20260731T105819Z_results.csv",
        "vendor reference data: underlying, strike, expiry, put/call, point value"),
    "EV-EXPLOIT-OPTION-QUOTES": (
        "docs/review/kdk_v4/wp_l1_rithmic/probe/exploit_20260731T105819Z_results.csv",
        "84 option-symbol ticks across 2 calls and 2 puts near ATM"),
    "EV-UNMAPPED-OI-155-157": (
        "docs/review/kdk_v4/wp_l1_rithmic/probe/UNMAPPED_TEMPLATES_exploit_20260731T105819Z.json",
        "template 158 open interest, 155 settlement, 157 market mode decoded off the wire"),
    "EV-PERBIT-ATTRIBUTION": (
        "docs/review/kdk_v4/wp_l1_rithmic/probe/exploit_20260731T110107Z_results.csv",
        "each of 14 UpdateBits subscribed alone; 12 return unparseable templates, 0 ticks"),
    "EV-PERBIT-UNMAPPED": (
        "docs/review/kdk_v4/wp_l1_rithmic/probe/UNMAPPED_TEMPLATES_exploit_20260731T110107Z.json",
        "per-bit unmapped template frames 152/153/154/155/157/158/162/163"),
    "EV-REBUILD-DIVERGES": (
        "docs/review/kdk_v4/wp_l1_rithmic/probe/exploit_20260731T113104Z_E5_book_rebuild.json",
        "21-price fan-out; 1 order seeded per snapshot; 3 of 21 levels match vendor book"),
    "EV-RECONNECT-GAP": (
        "docs/review/kdk_v4/wp_l1_rithmic/probe/exploit_20260731T113104Z_E6_reconnect.json",
        "reconnect 12.4s, subscriptions replayed, sequence delta 3975 unrecovered"),
    "EV-HIST-PARITY": (
        "docs/review/kdk_v4/wp_l1_rithmic/probe/exploit_20260731T112315Z_E7_parity.json",
        "76 of 77 live trades present in the same historical window"),
    "EV-DOC-12": (
        "docs/review/kdk_v4/wp_l1_rithmic/12_EXPLOITATION_PROBE_RESULTS.md",
        "exploitation probe report"),
    "EV-DOC-13": (
        "docs/review/kdk_v4/wp_l1_rithmic/13_RECORDER_READINESS_EVIDENCE.md",
        "recorder readiness report"),
    "EV-PARAM-REGISTRY": (
        "docs/review/kdk_v4/mrbs_param_recon_r3/KDK_PARAMETER_CANONICAL_REGISTRY_R3.csv",
        "134 canonical parameters, 0 Approved (92 Proposed / 38 UnderReview / 4 Deferred)"),
}

# surface -> (capability_state, data_quality_role, EvidenceIds, note)
# capability_state uses MRBS v1.1 section 18 vocabulary ONLY:
#   Unavailable | HistoricalOnly | LiveOnly | ReplayCompatible | Ready | Degraded
# plus CLIENT_BLOCKED and NOT_EVIDENCED, which are declared in the schema as extensions
# because MRBS has no term for "vendor sends it and the client library discards it".
SURFACES = [
    ("Exchange permissions / entitlement", "Ready", "core-support",
     ["EV-PROBE-ENTITLEMENT"], "L1+L2 on four exchanges"),
    ("Front-month contract", "Ready", "core-support",
     ["EV-PROBE-ENTITLEMENT"], "GCZ6"),
    ("Symbol search / option chain discovery", "Ready", "core-support",
     ["EV-EXPLOIT-OPTION-CATALOG"],
     "usable only with server-side product_code + instrument_type filter"),
    ("Reference data (instrument metadata)", "Ready", "core-support",
     ["EV-EXPLOIT-OPTION-REFERENCE"], "tick size and point value read, not assumed"),
    ("Level 1 executed trades", "Ready", "AUTHORITATIVE_CORE",
     ["EV-PROBE-L1-TRADES"], "template 150"),
    ("Trade aggressor (vendor field)", "Ready", "AUTHORITATIVE_CORE",
     ["EV-PROBE-L1-TRADES"], "vendor field on template 150; no inference required"),
    ("Trade size / volume / vwap", "Ready", "AUTHORITATIVE_CORE",
     ["EV-PROBE-L1-TRADES"], "template 150"),
    ("Best bid offer (BBO)", "Ready", "AUTHORITATIVE_CORE",
     ["EV-PROBE-L1-TRADES"], "template 151, with sizes and order counts"),
    ("Aggregated order book depth", "Ready", "AUTHORITATIVE_CORE",
     ["EV-PROBE-AGG-BOOK", "EV-REBUILD-DIVERGES"],
     "template 156, incremental with BEGIN/MIDDLE/END/SOLO framing"),
    ("Exchange timestamp (EventTime)", "Ready", "AUTHORITATIVE_CORE",
     ["EV-REBUILD-DIVERGES"], "source_ssboe/source_usecs/source_nsecs"),
    ("Receive timestamp", "Ready", "AUTHORITATIVE_CORE",
     ["EV-REBUILD-DIVERGES"], "ssboe/usecs; jop_* is a third domain"),
    ("OrderBook.update_type semantics", "Ready", "core-support",
     ["EV-PROBE-AGG-BOOK"], "batch framing, not per-level NCD"),
    ("Market-by-order (DepthByOrder)", "Degraded", "RESEARCH_TELEMETRY",
     ["EV-REBUILD-DIVERGES"], "per-price subscription only; not a core dependency (DQ-002)"),
    ("MBO per-level New/Change/Delete", "Degraded", "RESEARCH_TELEMETRY",
     ["EV-REBUILD-DIVERGES"], "NEW/CHANGE/DELETE observed"),
    ("MBO native sequence_number", "Degraded", "RESEARCH_TELEMETRY",
     ["EV-REBUILD-DIVERGES", "EV-RECONNECT-GAP"],
     "present on every event; discontinuity across reconnect is unrecoverable"),
    ("MBO order identity and priority", "Degraded", "RESEARCH_TELEMETRY",
     ["EV-REBUILD-DIVERGES"], "exchange_order_id, depth_order_priority"),
    ("MBO transaction side", "Degraded", "RESEARCH_TELEMETRY",
     ["EV-REBUILD-DIVERGES"], "transaction_type on updates, depth_side on snapshot"),
    ("Full-book bootstrap", "Unavailable", "BLOCKED",
     ["EV-REBUILD-DIVERGES"],
     "template 116 returns exactly 1 order per price; deterministic rebuild impossible"),
    ("Option to expiry-specific futures mapping", "Ready", "OPTIONS_CONTEXT",
     ["EV-EXPLOIT-OPTION-REFERENCE"],
     "vendor underlying_symbol per series; OGU6 -> GCV6 while front month is GCZ6 (ROLL-004)"),
    ("Option strike / expiry / put-call", "Ready", "OPTIONS_CONTEXT",
     ["EV-EXPLOIT-OPTION-REFERENCE"], "vendor reference data"),
    ("Option quotes", "Ready", "OPTIONS_CONTEXT",
     ["EV-EXPLOIT-OPTION-QUOTES"], "bid/ask/sizes/order counts on option symbols"),
    ("Option open interest (point-in-time)", "CLIENT_BLOCKED", "OPTIONS_CONTEXT",
     ["EV-UNMAPPED-OI-155-157", "EV-PERBIT-UNMAPPED"],
     "template 158 on the wire and decoded; async_rithmic 1.6.3 has no message class"),
    ("Settlement", "CLIENT_BLOCKED", "OPTIONS_CONTEXT",
     ["EV-UNMAPPED-OI-155-157", "EV-PERBIT-UNMAPPED"], "template 155"),
    ("Market mode", "CLIENT_BLOCKED", "core-support",
     ["EV-UNMAPPED-OI-155-157", "EV-PERBIT-UNMAPPED"], "template 157"),
    ("Session OHLC", "CLIENT_BLOCKED", "core-support",
     ["EV-PERBIT-ATTRIBUTION", "EV-PERBIT-UNMAPPED"], "OPEN and HIGH_LOW both return 152"),
    ("Opening / closing indicator", "CLIENT_BLOCKED", "core-support",
     ["EV-PERBIT-ATTRIBUTION"],
     "OPENING_INDICATOR -> 154; CLOSING_INDICATOR produced no frames in the window"),
    ("Price limits / margin rate / adjusted close", "CLIENT_BLOCKED", "core-support",
     ["EV-PERBIT-ATTRIBUTION"],
     "limits -> 163, margin -> 162; ADJUSTED_CLOSE produced no frames"),
    ("Historical time bars", "ReplayCompatible", "core-support",
     ["EV-HIST-PARITY"], "1-minute OHLCV; aware-UTC required"),
    ("Historical tick replay", "ReplayCompatible", "core-support",
     ["EV-HIST-PARITY"],
     "template 207 bars, num_trades=1 and O==H==L==C; parity 76 of 77"),
    ("Reconnection and resubscription", "Degraded", "core-support",
     ["EV-RECONNECT-GAP"],
     "reconnect and replay work; gap recovery absent and not surfaced to the caller"),
    ("Implied volatility (vendor)", "Unavailable", "NOT_APPLICABLE",
     ["EV-DOC-12"], "absent from the protocol; must be computed locally"),
    ("Greeks (vendor)", "Unavailable", "NOT_APPLICABLE",
     ["EV-DOC-12"], "absent from the protocol; must be computed locally"),
]

COLUMNS = ["surface", "capability_state", "data_quality_role", "evidence_ids",
           "evidence_resolved", "evidence_sha256", "note"]


def sha256_of(rel):
    p = os.path.join(REPO, rel.replace("/", os.sep))
    if not os.path.isfile(p):
        return None
    with open(p, "rb") as fh:
        return hashlib.sha256(fh.read()).hexdigest()


def main():
    resolved, dangling = {}, []
    for eid, (rel, _desc) in sorted(EVIDENCE.items()):
        h = sha256_of(rel)
        if h is None:
            dangling.append((eid, rel))
        else:
            resolved[eid] = h

    ev_rows = []
    for eid, (rel, desc) in sorted(EVIDENCE.items()):
        ev_rows.append({
            "evidence_id": eid, "artifact": rel, "demonstrates": desc,
            "exists": eid in resolved,
            "sha256": resolved.get(eid, ""),
        })
    out = io.StringIO()
    w = csv.DictWriter(out, fieldnames=["evidence_id", "artifact", "demonstrates",
                                        "exists", "sha256"], lineterminator="\n")
    w.writeheader()
    w.writerows(ev_rows)
    with open(os.path.join(HERE, "EVIDENCE_INDEX.csv"), "w",
              encoding="utf-8", newline="") as fh:
        fh.write(out.getvalue())

    rows = []
    for surface, state, role, eids, note in SURFACES:
        missing = [e for e in eids if e not in resolved]
        if missing:
            state, note = "NOT_EVIDENCED", note + " | UNRESOLVED: " + ",".join(missing)
        rows.append({
            "surface": surface, "capability_state": state, "data_quality_role": role,
            "evidence_ids": ";".join(eids),
            "evidence_resolved": not missing,
            "evidence_sha256": ";".join(resolved.get(e, "MISSING")[:16] for e in eids),
            "note": note,
        })
    out = io.StringIO()
    w = csv.DictWriter(out, fieldnames=COLUMNS, lineterminator="\n")
    w.writeheader()
    w.writerows(rows)
    with open(os.path.join(HERE, "CAPABILITY_EVIDENCE_MANIFEST.csv"), "w",
              encoding="utf-8", newline="") as fh:
        fh.write(out.getvalue())

    print("surfaces            : %d" % len(rows))
    print("evidence ids         : %d  (resolved %d, dangling %d)"
          % (len(EVIDENCE), len(resolved), len(dangling)))
    for eid, rel in dangling:
        print("   DANGLING %s -> %s" % (eid, rel))
    import collections
    print("capability states    : %s"
          % dict(collections.Counter(r["capability_state"] for r in rows).most_common()))
    print("roles                : %s"
          % dict(collections.Counter(r["data_quality_role"] for r in rows).most_common()))
    unresolved = [r["surface"] for r in rows if not r["evidence_resolved"]]
    print("surfaces unresolved  : %s" % (unresolved or "none"))
    return 1 if dangling else 0


if __name__ == "__main__":
    raise SystemExit(main())
