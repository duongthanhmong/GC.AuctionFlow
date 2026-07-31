"""Stage 2 closure test suite - makes every structural claim in this package checkable.

Three kinds of test, and the negative ones carry the weight:

  metaschema  - each schema is itself a valid JSON Schema and its cross-file $refs resolve
  positive    - a conforming instance validates
  NEGATIVE    - a non-conforming instance is REJECTED

A fail-closed rule that only appears in prose is a promise. A fail-closed rule that rejects
an instance here is a mechanism. Every negative case below corresponds to a specific way the
recorder could silently emit something untrue.

Also re-resolves every EvidenceId against the repository and re-hashes the artifact, so a
"PROVEN" claim cannot outlive the file it points at.

usage: python test_stage2_closure.py
"""
import csv
import hashlib
import json
import os
import sys

sys.dont_write_bytecode = True

HERE = os.path.dirname(os.path.abspath(__file__))
PKG = os.path.abspath(os.path.join(HERE, ".."))
SCHEMAS = os.path.join(PKG, "schemas")
REPO = os.path.abspath(os.path.join(PKG, "..", "..", "..", "..", ".."))

FAIL, RAN = [], []


def check(name, ok, detail=""):
    RAN.append(name)
    print("  %-72s %s%s" % (name, "PASS" if ok else "FAIL", ("  " + detail) if detail else ""))
    if not ok:
        FAIL.append(name)


# --------------------------------------------------------------------------- harness

def load_registry():
    from referencing import Registry, Resource
    from referencing.jsonschema import DRAFT202012
    reg = Registry()
    for fn in sorted(os.listdir(SCHEMAS)):
        if not fn.endswith(".json"):
            continue
        doc = json.load(open(os.path.join(SCHEMAS, fn), encoding="utf-8"))
        res = Resource(contents=doc, specification=DRAFT202012)
        # register under both the absolute $id and the bare filename, because the schemas
        # reference each other by relative filename
        reg = reg.with_resource(doc["$id"], res).with_resource(fn, res)
    return reg


def validator_for(fn, reg):
    from jsonschema import Draft202012Validator
    doc = json.load(open(os.path.join(SCHEMAS, fn), encoding="utf-8"))
    return Draft202012Validator(doc, registry=reg)


VERSIONS = {"algorithmVersion": "0.1.0", "configVersion": "0.1.0", "dataSchemaVersion": "1.0.0"}
UTC = "2026-07-31T11:31:04Z"


def base_segment(**over):
    seg = {
        "segmentId": "SEG-1", "instrumentId": "GCZ6",
        "startUtc": UTC, "endUtc": "2026-07-31T11:33:04Z",
        "dataSourceId": "rithmic.ticker_plant.live",
        "sourceMix": ["rithmic.ticker_plant.live"],
        "capabilitySnapshotId": "CAP-1", "dataQuality": "Ready",
        "subscriptionScope": {"kind": "single_price", "priceCount": 1},
        "sequenceStats": {"observed": 17, "distinct": 17, "arrivalInversions": 0,
                          "sortedDiscontinuities": 16, "span": 2644, "missingWithinSpan": 2627},
        "containsUnrecoveredGap": False,
        "versions": dict(VERSIONS),
    }
    seg.update(over)
    return seg


def base_option(**over):
    c = {
        "symbol": "OGU6 C4120", "exchange": "COMEX", "productCode": "OG",
        "underlyingSymbol": "GCV6", "expirationDate": "20260826",
        "strikeTicks": 4120, "putCall": "Call", "singlePointValue": 100.0,
        "identitySource": "vendor_reference_data",
        "openInterestStatus": "CLIENT_BLOCKED", "openInterest": None,
    }
    c.update(over.pop("contract", {}))
    snap = {
        "snapshotId": "OPT-1", "snapshotTimeUtc": UTC, "optionsState": "ContextOnly",
        "capabilitySnapshotId": "CAP-1", "modelVersion": "0.1.0",
        "futuresContextSymbol": "GCZ6", "contracts": [c], "versions": dict(VERSIONS),
    }
    snap.update(over)
    return snap


def base_event(**over):
    e = {
        "instrumentId": "GCZ6", "exchange": "COMEX", "eventType": "Trade",
        "eventTimeUtc": UTC, "receiveTimeUtc": UTC, "processingTimeUtc": UTC,
        "sequence": 1, "sequenceProvenance": "recorder_assigned",
        "priceTicks": 4113, "tickSize": 1.0, "quantity": 1,
        "aggressorSide": "Buy", "aggressorProvenance": "vendor_field",
        "dataSourceId": "rithmic.ticker_plant.live",
        "capabilitySnapshotId": "CAP-1", "segmentId": "SEG-1",
        "versions": dict(VERSIONS),
    }
    e.update(over)
    return e


def main():
    print("###### KDK Stage 2 closure tests ######")
    print()

    # ------------------------------------------------------------------ metaschema
    from jsonschema import Draft202012Validator
    reg = load_registry()
    files = sorted(f for f in os.listdir(SCHEMAS) if f.endswith(".json"))
    print("-- schemas are valid JSON Schema and their $refs resolve --")
    for fn in files:
        doc = json.load(open(os.path.join(SCHEMAS, fn), encoding="utf-8"))
        errs = list(Draft202012Validator.check_schema(doc) or [])
        check("%s is a valid draft-2020-12 schema" % fn, not errs)
    for fn in [f for f in files if f != "common.defs.schema.json"]:
        v = validator_for(fn, reg)
        try:
            list(v.iter_errors({}))          # forces $ref resolution
            ok = True
        except Exception as e:
            ok, _ = False, e
        check("%s cross-file $refs resolve" % fn, ok)

    # ------------------------------------------------------------------ positive
    print()
    print("-- conforming instances validate --")
    ev = validator_for("market_event.schema.json", reg)
    seg = validator_for("recorder_segment.schema.json", reg)
    opt = validator_for("options_snapshot.schema.json", reg)
    raw = validator_for("raw_frame.schema.json", reg)
    cap = validator_for("capability_snapshot.schema.json", reg)

    check("a core trade event validates", not list(ev.iter_errors(base_event())))
    check("a clean segment validates", not list(seg.iter_errors(base_segment())))
    check("a ContextOnly options snapshot with blocked OI validates",
          not list(opt.iter_errors(base_option())))
    check("an uninterpreted raw frame validates", not list(raw.iter_errors({
        "templateId": 158, "vendorNameHypothesis": "open_interest (vendor numbering)",
        "capturedUtc": UTC, "segmentId": "SEG-1", "framedBytes": 49,
        "payloadBase64": "CJ4B", "decodeMethod": "generic_wireformat",
        "semanticStatus": "FIELD_NUMBERS_ONLY",
        "decodedFields": {"100064": [{"wire": "varint", "value": 174}]},
        "versions": dict(VERSIONS)})))
    check("a capability snapshot validates", not list(cap.iter_errors({
        "capabilitySnapshotId": "CAP-1", "createdUtc": UTC,
        "acquisitionPath": "rithmic_direct_live",
        "clientLibrary": {"name": "async_rithmic", "version": "1.6.3",
                          "mappedStreamTemplates": [150, 151, 156, 160, 161],
                          "observedUnmappedTemplates": [152, 153, 154, 155, 157, 158, 162, 163]},
        "surfaces": [{"surface": "Level 1 executed trades", "capabilityState": "Ready",
                      "dataQualityRole": "AUTHORITATIVE_CORE",
                      "evidenceIds": ["EV-PROBE-L1-TRADES"]}],
        "versions": dict(VERSIONS)})))

    # ------------------------------------------------------------------ NEGATIVE
    print()
    print("-- fail-closed rules REJECT non-conforming instances --")

    check("naive timestamp (no UTC offset) is rejected",
          bool(list(ev.iter_errors(base_event(eventTimeUtc="2026-07-31T11:31:04")))),
          "base.py:567-577 trap")
    check("float price is rejected (integer ticks only, MRBS 2.1)",
          bool(list(ev.iter_errors(base_event(priceTicks=4113.5)))))
    check("missing version block is rejected (VER-001)",
          bool(list(ev.iter_errors({k: v for k, v in base_event().items() if k != "versions"}))))
    check("aggressorProvenance=absent with a concrete side is rejected",
          bool(list(ev.iter_errors(base_event(aggressorProvenance="absent",
                                              aggressorSide="Buy")))),
          "ADR-001")
    check("a Trade with no aggressor fields is rejected",
          bool(list(ev.iter_errors({k: v for k, v in base_event().items()
                                    if k not in ("aggressorSide", "aggressorProvenance")}))))

    check("unrecovered gap + dataQuality Ready is rejected",
          bool(list(seg.iter_errors(base_segment(containsUnrecoveredGap=True,
                                                 dataQuality="Ready")))),
          "ADR-003")
    check("unrecovered gap with no discontinuity record is rejected",
          bool(list(seg.iter_errors(base_segment(containsUnrecoveredGap=True,
                                                 dataQuality="Invalid",
                                                 reasonCodes=["DATA_INVALID"])))))
    check("unrecovered gap without DATA_INVALID reason code is rejected",
          bool(list(seg.iter_errors(base_segment(
              containsUnrecoveredGap=True, dataQuality="Invalid", reasonCodes=[],
              discontinuities=[{"lastSequenceBefore": 115664320,
                                "firstSequenceAfter": 115668295, "delta": 3975,
                                "outageStartUtc": UTC, "outageEndUtc": UTC,
                                "reconnected": True, "recoveryAttempted": False,
                                "recoveryMechanism": "none_available"}])))))
    check("a real measured gap segment validates when marked correctly",
          not list(seg.iter_errors(base_segment(
              containsUnrecoveredGap=True, dataQuality="Invalid",
              reasonCodes=["DATA_INVALID"],
              discontinuities=[{"lastSequenceBefore": 115664320,
                                "firstSequenceAfter": 115668295, "delta": 3975,
                                "outageStartUtc": UTC, "outageEndUtc": UTC,
                                "reconnected": True, "subscriptionsReplayed": True,
                                "recoveryAttempted": False,
                                "recoveryMechanism": "none_available"}]))),
          "the E6 measurement, expressed")
    check("claiming a recovery mechanism is rejected",
          bool(list(seg.iter_errors(base_segment(
              containsUnrecoveredGap=True, dataQuality="Invalid",
              reasonCodes=["DATA_INVALID"],
              discontinuities=[{"lastSequenceBefore": 1, "firstSequenceAfter": 9,
                                "delta": 8, "outageStartUtc": UTC, "outageEndUtc": UTC,
                                "reconnected": True, "recoveryAttempted": True,
                                "recoveryMechanism": "none_available"}])))))

    check("OptionsState=Ready while OI is CLIENT_BLOCKED is rejected",
          bool(list(opt.iter_errors(base_option(optionsState="Ready")))),
          "ADR-004")
    check("an OI value present while status is not SCHEMA_BOUND is rejected",
          bool(list(opt.iter_errors(base_option(
              contract={"openInterestStatus": "FIELD_NUMBERS_ONLY", "openInterest": 174})))),
          "ADR-007")
    check("InvalidMapping without CONTRACT_MAPPING_INVALID is rejected",
          bool(list(opt.iter_errors(base_option(rollState="InvalidMapping",
                                                optionsState="Invalid",
                                                reasonCodes=["DATA_INVALID"])))),
          "ROLL-004")
    check("identity parsed from the symbol string cannot be expressed",
          bool(list(opt.iter_errors(base_option(
              contract={"identitySource": "symbol_string_parse"})))))
    check("an option with no underlyingSymbol is rejected",
          bool(list(opt.iter_errors(base_option(contract={"underlyingSymbol": ""})))))

    check("generic wire decode cannot be SCHEMA_BOUND",
          bool(list(raw.iter_errors({
              "templateId": 158, "capturedUtc": UTC, "segmentId": "S", "framedBytes": 49,
              "payloadBase64": "CJ4B", "decodeMethod": "generic_wireformat",
              "semanticStatus": "SCHEMA_BOUND", "versions": dict(VERSIONS)}))),
          "ADR-007: field numbers are not meanings")
    check("a decoded field carrying a NAME is rejected",
          bool(list(raw.iter_errors({
              "templateId": 158, "capturedUtc": UTC, "segmentId": "S", "framedBytes": 49,
              "payloadBase64": "CJ4B", "decodeMethod": "generic_wireformat",
              "semanticStatus": "FIELD_NUMBERS_ONLY",
              "decodedFields": {"open_interest": [{"wire": "varint", "value": 174}]},
              "versions": dict(VERSIONS)}))))

    check("NOT_EVIDENCED surface cannot be AUTHORITATIVE_CORE",
          bool(list(cap.iter_errors({
              "capabilitySnapshotId": "C", "createdUtc": UTC,
              "acquisitionPath": "rithmic_direct_live",
              "clientLibrary": {"name": "async_rithmic", "version": "1.6.3",
                                "mappedStreamTemplates": [150]},
              "surfaces": [{"surface": "x", "capabilityState": "NOT_EVIDENCED",
                            "dataQualityRole": "AUTHORITATIVE_CORE",
                            "evidenceIds": ["EV-PROBE-L1-TRADES"]}],
              "versions": dict(VERSIONS)}))))
    check("a surface with no EvidenceId is rejected",
          bool(list(cap.iter_errors({
              "capabilitySnapshotId": "C", "createdUtc": UTC,
              "acquisitionPath": "rithmic_direct_live",
              "clientLibrary": {"name": "async_rithmic", "version": "1.6.3",
                                "mappedStreamTemplates": [150]},
              "surfaces": [{"surface": "x", "capabilityState": "Ready",
                            "dataQualityRole": "core-support", "evidenceIds": []}],
              "versions": dict(VERSIONS)}))))

    # parameterRef: unapproved parameters must not carry a value
    defs = json.load(open(os.path.join(SCHEMAS, "common.defs.schema.json"), encoding="utf-8"))
    pref = Draft202012Validator(
        {"$schema": defs["$schema"], "$defs": defs["$defs"],
         "$ref": "#/$defs/parameterRef"}, registry=reg)
    check("an unapproved parameter carrying a bound value is rejected",
          bool(list(pref.iter_errors({"canonicalId": "gap_tolerance",
                                      "approvalStatus": "Proposed", "boundValue": 3}))),
          "ADR-005")
    check("an unapproved parameter with no value validates",
          not list(pref.iter_errors({"canonicalId": "gap_tolerance",
                                     "approvalStatus": "Proposed", "boundValue": None})))

    # ------------------------------------------------------------------ evidence
    print()
    print("-- every EvidenceId resolves and re-hashes --")
    idx = list(csv.DictReader(open(os.path.join(PKG, "EVIDENCE_INDEX.csv"), encoding="utf-8")))
    dangling, drifted = [], []
    for r in idx:
        p = os.path.join(REPO, r["artifact"].replace("/", os.sep))
        if not os.path.isfile(p):
            dangling.append(r["evidence_id"])
            continue
        h = hashlib.sha256(open(p, "rb").read()).hexdigest()
        if h != r["sha256"]:
            drifted.append(r["evidence_id"])
    check("all %d EvidenceIds resolve to a file" % len(idx), not dangling, str(dangling))
    check("all EvidenceId hashes still match", not drifted, str(drifted))

    man = list(csv.DictReader(open(os.path.join(PKG, "CAPABILITY_EVIDENCE_MANIFEST.csv"),
                                   encoding="utf-8")))
    known = {r["evidence_id"] for r in idx}
    bad = [m["surface"] for m in man
           if any(e and e not in known for e in m["evidence_ids"].split(";"))]
    check("manifest covers 32 surfaces", len(man) == 32, "got %d" % len(man))
    check("every manifest EvidenceId exists in the index", not bad, str(bad))
    check("no manifest surface is left unresolved",
          all(m["evidence_resolved"] == "True" for m in man))
    core = [m for m in man if m["data_quality_role"] == "AUTHORITATIVE_CORE"]
    check("every AUTHORITATIVE_CORE surface is Ready", core and
          all(m["capability_state"] == "Ready" for m in core), "%d core surfaces" % len(core))
    mbo = [m for m in man if m["data_quality_role"] == "RESEARCH_TELEMETRY"]
    check("no MBO surface is AUTHORITATIVE_CORE (MRBS DQ-002)", mbo and
          all(m["capability_state"] != "Ready" for m in mbo), "%d telemetry surfaces" % len(mbo))

    # ------------------------------------------------------------------ registry
    print()
    print("-- no seed is hard-coded (ADR-005) --")
    reg_csv = os.path.join(REPO, "docs", "review", "kdk_v4", "mrbs_param_recon_r3",
                           "KDK_PARAMETER_CANONICAL_REGISTRY_R3.csv")
    rows = list(csv.DictReader(open(reg_csv, encoding="utf-8")))
    approved = [r for r in rows if r["approval_status"] == "Approved"]
    check("canonical registry has 134 parameters", len(rows) == 134, "got %d" % len(rows))
    check("zero parameters are Approved", not approved, "%d approved" % len(approved))
    inlined = []
    for fn in files:
        doc = json.load(open(os.path.join(SCHEMAS, fn), encoding="utf-8"))
        txt = json.dumps(doc)
        for r in rows:
            cid = r["canonical_id"]
            if '"%s"' % cid in txt and '"default"' in txt:
                inlined.append((fn, cid))
    check("no schema inlines a default for a registry parameter", not inlined, str(inlined[:3]))

    print()
    if FAIL:
        print("RESULT: FAIL (%d of %d)" % (len(FAIL), len(RAN)))
        for f in FAIL:
            print("   %s" % f)
        return 1
    print("RESULT: PASS - %d/%d checks" % (len(RAN), len(RAN)))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
