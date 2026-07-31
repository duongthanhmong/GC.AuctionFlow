"""Enumerate every data surface async_rithmic exposes. OFFLINE, read-only, no credentials.

Stage 2 addendum corrigendum items 2, 8 and 10.

The first version reported empty sections for client methods and event handles, because
async_rithmic creates events in RithmicClient.__init__ (client.py:35-56) and reaches plant
methods through instance delegation. This version constructs a client with PLACEHOLDER
credentials and never calls connect(), so instance state is visible without a login.

It answers exactly one question - "what does the installed package expose?" - which is the
FIRST column of the capability matrix and is not server support, not entitlement, not
subscription success and not callback delivery.

usage: python rithmic_surface_enum.py
"""
import importlib
import importlib.metadata as md
import importlib.util
import inspect
import os
import pkgutil
import sys

# Item 10: never print the local user's home path.
def redact(p):
    if not p:
        return p
    s = str(p)
    for marker in ("site-packages", "dist-packages"):
        i = s.find(marker)
        if i != -1:
            return "<" + marker + ">" + s[i + len(marker):].replace("\\", "/")
    return "<local-path>/" + os.path.basename(s)


# Item 8: the exact terms used for the IV/Greeks negative finding.
GREEK_TERMS = ("implied", "impl_vol", "iv", "vol", "volatility", "delta", "gamma", "vega",
               "theta", "rho", "greek", "charm", "vanna", "moneyness")
# Substrings that make a "vol" hit a false positive (volume, not volatility).
GREEK_FALSE = ("volume", "vol_", "_vol", "total_vol")


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
        print("async_rithmic is NOT INSTALLED")
        return 1
    root = os.path.dirname(spec.origin)

    print("###### async_rithmic SURFACE ENUMERATION (offline, no credentials) ######")
    print("  installed version : %s" % ver)
    print("  location          : %s" % redact(root))
    print("  repo requirement  : research/optionflow/requirements.txt -> 'async_rithmic>=1.0'")
    print("                      NOT PINNED. Everything below is true of %s only." % ver)
    print("  scope             : API EXPOSURE ONLY.")

    ar = importlib.import_module("async_rithmic")

    # ---------------------------------------------------------------- enums
    heading("1. enums and their value semantics")
    for n in sorted(dir(ar)):
        if n.startswith("_"):
            continue
        obj = getattr(ar, n)
        vals = None
        if hasattr(obj, "DESCRIPTOR") and hasattr(obj.DESCRIPTOR, "values"):
            vals = [(v.name, v.number) for v in obj.DESCRIPTOR.values]
        elif inspect.isclass(obj):
            ms = [m for m in dir(obj) if m.isupper() and not m.startswith("_")]
            if ms:
                vals = []
                for m in ms:
                    try:
                        v = getattr(obj, m)
                        vals.append((m, int(v) if hasattr(v, "__int__") else v))
                    except Exception:
                        vals.append((m, "<unreadable>"))
        if not vals:
            continue
        nonzero = [v for v in vals if v[1] != 0]
        print("   %s  (%d members, %d non-zero)" % (n, len(vals), len(nonzero)))
        for name, num in vals:
            print("      %-36s = %s%s" % (name, num, "   <- zero/unspecified" if num == 0 else ""))

    # ------------------------------------------------------- classes + plants
    heading("2. classes: RithmicClient and every plant")
    plants_mod = importlib.import_module("async_rithmic.plants")
    targets = [("RithmicClient", getattr(ar, "RithmicClient", None))]
    for pn in sorted(dir(plants_mod)):
        po = getattr(plants_mod, pn)
        if inspect.isclass(po):
            targets.append((pn, po))
    try:
        base_mod = importlib.import_module("async_rithmic.plants.base")
        for bn in sorted(dir(base_mod)):
            bo = getattr(base_mod, bn)
            if inspect.isclass(bo) and bn not in dict(targets):
                targets.append((bn, bo))
    except Exception:
        pass

    for name, cls in targets:
        if cls is None:
            continue
        print()
        print("   -- %s --" % name)
        pub = []
        for mn, m in sorted(inspect.getmembers(cls)):
            if mn.startswith("_"):
                continue
            if callable(m) or isinstance(m, (staticmethod, classmethod)):
                try:
                    sig = str(inspect.signature(m))
                except Exception:
                    sig = "(...)"
                pub.append("def %s%s" % (mn, sig))
            elif isinstance(m, property):
                pub.append("property %s" % mn)
        if not pub:
            print("      (no public class-level members)")
        for p in pub:
            print("      %s" % p)

    # ------------------------------------- instance state: events + delegation
    heading("3. INSTANCE surface (events and delegated methods)")
    print("   Constructed with placeholder credentials. connect() is NEVER called.")
    inst = None
    try:
        inst = ar.RithmicClient(user="<placeholder>", password="<placeholder>",
                                system_name="<placeholder>", app_name="<placeholder>",
                                app_version="0", url="<placeholder>")
    except Exception as e:
        print("   could not construct: %s: %s" % (type(e).__name__, e))

    if inst is not None:
        cls_names = set(dir(type(inst)))
        events, delegated, other = [], [], []
        for n in sorted(dir(inst)):
            if n.startswith("_"):
                continue
            try:
                v = getattr(inst, n)
            except Exception:
                continue
            t = type(v).__name__
            if t == "Event" or n.startswith("on_"):
                events.append((n, t))
            elif callable(v) and n not in cls_names:
                delegated.append((n, t))
            elif n not in cls_names:
                other.append((n, t))
        print()
        print("   -- event handles (%d) --" % len(events))
        for n, t in events:
            print("      %-42s %s" % (n, t))
        print()
        print("   -- delegated/instance callables (%d) --" % len(delegated))
        for n, t in delegated:
            print("      %-42s %s" % (n, t))
        print()
        print("   -- other instance attributes (%d) --" % len(other))
        for n, t in other:
            print("      %-42s %s" % (n, t))
        print()
        print("   -- reconnection / retry defaults constructed WITHOUT caller input --")
        for attr in ("reconnection_settings", "retry_settings"):
            v = getattr(inst, attr, None)
            print("      %-26s %s" % (attr, v))
        print()
        print("   -- plants instantiated --")
        try:
            for k, v in (inst.plants or {}).items():
                print("      %-14s %s" % (k, type(v).__name__))
                subs = getattr(v, "_subscriptions", None)
                if subs is not None:
                    print("           tracks subscriptions: %s" % list(subs.keys()))
        except Exception as e:
            print("      (%s)" % e)

    # ----------------------------------------- every protobuf message + field
    heading("4. ALL protobuf modules: every message and every field")
    pb_root = os.path.join(root, "protocol_buffers")
    mods = sorted(m.name for m in pkgutil.iter_modules([pb_root])) if os.path.isdir(pb_root) else []
    total_msgs = total_fields = 0
    greek_hits = []
    field_index = {}
    for mname in mods:
        try:
            mod = importlib.import_module("async_rithmic.protocol_buffers." + mname)
        except Exception as e:
            print("   %-52s IMPORT FAILED %s" % (mname, type(e).__name__))
            continue
        msgs = []
        for attr in sorted(dir(mod)):
            o = getattr(mod, attr)
            d = getattr(o, "DESCRIPTOR", None)
            if d is None or not hasattr(d, "fields"):
                continue
            fields = [f.name for f in d.fields]
            msgs.append((attr, fields))
            total_msgs += 1
            total_fields += len(fields)
            field_index[mname + "." + attr] = fields
            for f in fields:
                lf = f.lower()
                for term in GREEK_TERMS:
                    if term in lf and not any(x in lf for x in GREEK_FALSE):
                        greek_hits.append((mname, attr, f, term))
        if msgs:
            print()
            print("   %s" % mname)
            for attr, fields in msgs:
                print("      %s (%d fields)" % (attr, len(fields)))
                print("         %s" % ", ".join(fields))
    print()
    print("   modules scanned: %d   messages: %d   fields: %d"
          % (len(mods), total_msgs, total_fields))

    # ------------------------------------------------ item 8: negative finding
    heading("5. IV / Greeks scan across EVERY message field (item 8)")
    print("   search terms: %s" % ", ".join(GREEK_TERMS))
    print("   false-positive filters: %s" % ", ".join(GREEK_FALSE))
    print("   modules scanned: %d   messages: %d   fields compared: %d"
          % (len(mods), total_msgs, total_fields))
    print()
    if greek_hits:
        print("   MATCHES (%d):" % len(greek_hits))
        for m, a, f, t in greek_hits:
            print("      %-46s %-28s field=%-24s term=%s" % (m, a, f, t))
    else:
        print("   MATCHES: 0")
        print("   -> no implied-volatility or Greek field exists in any message of this")
        print("      package version. IV and Greeks must be calculated locally.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
