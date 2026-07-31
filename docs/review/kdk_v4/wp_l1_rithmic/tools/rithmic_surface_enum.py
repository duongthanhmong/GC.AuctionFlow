"""Enumerate every data surface the installed async_rithmic exposes. Offline, read-only.

Stage 2 addendum item 2. This inspects the INSTALLED dependency - it makes no network
connection, sends no request and reads no credential. It answers exactly one question:

    "what does the pinned API expose?"

which is the first of eight columns in the capability matrix and must not be confused with
server support, account entitlement, subscription success or actual callback delivery.

usage: python rithmic_surface_enum.py
"""
import importlib
import importlib.metadata as md
import inspect
import os
import pkgutil
import sys

MARKET_HINTS = (
    "trade", "quote", "bbo", "best", "bid", "ask", "depth", "book", "order_book",
    "market", "tick", "volume", "open_interest", "settlement", "ohlc", "high", "low",
    "close", "open", "instrument", "symbol", "search", "reference", "expiration",
    "option", "strike", "put", "call", "underlying", "session", "status", "snapshot",
    "sequence", "ssboe", "usecs", "time", "greek", "vol", "implied", "front_month",
)


def heading(t):
    print()
    print("=" * 78)
    print(t)
    print("=" * 78)


def main():
    try:
        ver = md.version("async_rithmic")
    except Exception as e:
        ver = "UNKNOWN (%s)" % e
    spec = importlib.util.find_spec("async_rithmic")
    if spec is None:
        print("async_rithmic is NOT INSTALLED - nothing to enumerate")
        return 1
    root = os.path.dirname(spec.origin)

    print("###### async_rithmic SURFACE ENUMERATION (offline) ######")
    print("  installed version : %s" % ver)
    print("  location          : %s" % root)
    print("  repo pin          : research/optionflow/requirements.txt says 'async_rithmic>=1.0'")
    print("                      -> the version is NOT pinned; 1.6.3 is what happens to be installed")
    print("  NOTE: everything below is API EXPOSURE ONLY. It is not server support, not")
    print("        account entitlement, and not evidence that any callback ever fires.")

    ar = importlib.import_module("async_rithmic")

    heading("1. top-level public names")
    names = sorted(n for n in dir(ar) if not n.startswith("_"))
    for n in names:
        obj = getattr(ar, n)
        print("   %-34s %s" % (n, type(obj).__name__))

    heading("2. enums that describe plants, data types and update bits")
    for n in names:
        obj = getattr(ar, n)
        if not inspect.isclass(obj):
            continue
        members = [m for m in dir(obj) if m.isupper() and not m.startswith("_")]
        if not members:
            continue
        print("   %s:" % n)
        for m in members:
            try:
                print("      %-40s = %r" % (m, getattr(obj, m)))
            except Exception:
                print("      %-40s = <unreadable>" % m)

    heading("3. client methods relevant to market/reference data")
    for cname in ("RithmicClient",):
        cls = getattr(ar, cname, None)
        if cls is None:
            continue
        print("   %s:" % cname)
        for mname, m in sorted(inspect.getmembers(cls, callable)):
            if mname.startswith("_"):
                continue
            low = mname.lower()
            if not any(h in low for h in MARKET_HINTS):
                continue
            try:
                sig = str(inspect.signature(m))
            except Exception:
                sig = "(...)"
            print("      %s%s" % (mname, sig))

    heading("4. event/callback handles exposed by the client")
    cls = getattr(ar, "RithmicClient", None)
    if cls is not None:
        for mname, m in sorted(inspect.getmembers(cls)):
            if mname.startswith("_"):
                continue
            t = type(m).__name__
            if "event" in mname.lower() or "Event" in t or "handler" in mname.lower():
                print("   %-38s %s" % (mname, t))

    heading("5. protocol buffer modules (request/response templates)")
    pb_root = os.path.join(root, "protocol_buffers")
    if not os.path.isdir(pb_root):
        print("   (no protocol_buffers package found)")
    else:
        mods = sorted(m.name for m in pkgutil.iter_modules([pb_root]))
        print("   %d generated modules" % len(mods))
        req = [m for m in mods if m.startswith("request_")]
        rsp = [m for m in mods if m.startswith("response_")]
        other = [m for m in mods if not m.startswith(("request_", "response_"))]
        for label, group in (("REQUEST templates", req), ("RESPONSE templates", rsp),
                             ("other", other)):
            print()
            print("   -- %s (%d) --" % (label, len(group)))
            for m in group:
                low = m.lower()
                mark = "  <-- market/reference data" if any(h in low for h in MARKET_HINTS) else ""
                print("      %s%s" % (m, mark))

    heading("6. fields on the market-data update request (what can be asked for)")
    try:
        md_pb = importlib.import_module(
            "async_rithmic.protocol_buffers.request_market_data_update_pb2")
        msg = md_pb.RequestMarketDataUpdate
        print("   RequestMarketDataUpdate fields:")
        for f in msg.DESCRIPTOR.fields:
            print("      %-34s %s" % (f.name, f.type))
        print()
        print("   UpdateBits enum members (the subscribable surfaces):")
        for v in msg.UpdateBits.DESCRIPTOR.values:
            print("      %-34s = %d" % (v.name, v.number))
    except Exception as e:
        print("   NOT DETERMINED: %s" % e)

    heading("7. last-trade / BBO / depth response fields")
    for modname, msgname in (
        ("response_market_data_update_pb2", None),
        ("last_trade_pb2", "LastTrade"),
        ("best_bid_offer_pb2", "BestBidOffer"),
        ("order_book_pb2", "OrderBook"),
        ("depth_by_order_pb2", "DepthByOrder"),
        ("instrument_pb2", None),
        ("symbol_search_pb2", None),
    ):
        try:
            mod = importlib.import_module("async_rithmic.protocol_buffers." + modname)
        except Exception:
            continue
        for attr in dir(mod):
            obj = getattr(mod, attr)
            if not hasattr(obj, "DESCRIPTOR") or not hasattr(obj.DESCRIPTOR, "fields"):
                continue
            if msgname and attr != msgname:
                continue
            print()
            print("   %s.%s" % (modname, attr))
            for f in obj.DESCRIPTOR.fields:
                print("      %s" % f.name)
            break
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
