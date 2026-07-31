"""Pre-login gateway diagnostic: which Rithmic systems does this gateway actually offer?

async_rithmic's BasePlant._connect() opens the websocket and sends RequestRithmicSystemInfo
(template 16) BEFORE _login() (base.py:200-216). So the system list is obtainable without a
valid credential - which is exactly what is needed after an rp_code 13 'permission denied'
login, because it separates "wrong system name" from "account/entitlement problem".

No login is performed. No market data is requested. Credentials are loaded only because the
client constructor requires them, and redaction is installed before anything runs.

usage: python rithmic_system_probe.py --out <quarantine_dir>
"""
import argparse
import asyncio
import datetime as dt
import json
import os
import sys

sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from rithmic_probe import install_log_redaction, pb  # noqa: E402


async def run(outdir):
    here = os.path.dirname(os.path.abspath(__file__))
    sys.path.insert(0, os.path.abspath(os.path.join(
        here, "..", "..", "..", "..", "..", "research", "optionflow")))
    from rithmic.config import load_config
    from async_rithmic import RithmicClient, SysInfraType

    cfg = load_config()
    install_log_redaction(cfg.user, cfg.password)

    out = {"utc": dt.datetime.now(dt.timezone.utc).isoformat(timespec="seconds")}
    client = RithmicClient(user=cfg.user, password=cfg.password,
                           system_name=cfg.system_name, app_name=cfg.app_name,
                           app_version=cfg.app_version, url=cfg.url)
    plant = client.plants["ticker"]

    print("connecting websocket only (NO login)")
    try:
        await plant._connect()
        out["websocket_connected"] = True
        print("  websocket: CONNECTED")
    except Exception as e:
        out["websocket_connected"] = False
        out["error"] = "%s: %s" % (type(e).__name__, str(e)[:200])
        print("  websocket FAILED: %s" % out["error"])
        return out

    try:
        info = pb(await plant.get_system_info())
        out["system_info"] = info
        names = info.get("system_name") if isinstance(info, dict) else None
        if isinstance(names, str):
            names = [names]
        names = names or []
        out["systems_offered"] = list(names)
        print("  gateway offers %d system(s):" % len(names))
        for n in names:
            print("     %r" % n)
        configured = cfg.system_name
        out["configured_system_matches_gateway_list"] = configured in names
        print()
        print("  configured system present in that list: %s"
              % out["configured_system_matches_gateway_list"])
        if not out["configured_system_matches_gateway_list"]:
            print("  -> the configured system name is NOT one this gateway serves")
        else:
            print("  -> the system name is valid; the denial is about the account, not the system")
    except Exception as e:
        out["system_info_error"] = "%s: %s" % (type(e).__name__, str(e)[:200])
        print("  system-info request failed: %s" % out["system_info_error"])

    try:
        await plant._disconnect()
    except Exception:
        try:
            await client.disconnect()
        except Exception:
            pass
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    a = ap.parse_args()
    stamp = dt.datetime.now(dt.timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    d = os.path.join(a.out, "systeminfo_" + stamp)
    os.makedirs(d, exist_ok=True)
    res = asyncio.run(run(d))
    p = os.path.join(d, "system_info.json")
    with open(p, "w", encoding="utf-8", newline="\n") as fh:
        json.dump(res, fh, indent=1, default=str)
    print()
    print("capture: %s" % p)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
