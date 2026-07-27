"""Credential + connection loading. Secrets never live in the repo.

Reads C:\\Users\\LOQ\\.gcae\\rithmic.env (overridable via GCAE_RITHMIC_ENV).
Format is plain KEY=VALUE lines; '#' comments and blanks ignored.
"""

from __future__ import annotations

import os
from dataclasses import dataclass

DEFAULT_ENV_PATH = os.path.join(os.path.expanduser("~"), ".gcae", "rithmic.env")


# Standard gateway/system for Rithmic 30-day paper / eval accounts — the same
# pairing Seachains uses (System dropdown = "Rithmic Paper Trading"). One gateway
# hosts several systems; the app lists them via RequestRithmicSystemInfo. Override
# in rithmic.env if your broker gave you a different gateway.
# Discovered from the running Seachains: it connects account fin_... on system
# "Rithmic Paper Trading" via the Japan gateway rsc-jp.rithmic.com (18.183.109.168).
# That is the gateway/system pairing that actually serves GC/ES/NQ option data.
DEFAULT_SYSTEM_NAME = "Rithmic Paper Trading"
DEFAULT_GATEWAY_URL = "rsc-jp.rithmic.com:443"


@dataclass
class RithmicConfig:
    user: str
    password: str
    system_name: str = DEFAULT_SYSTEM_NAME
    url: str = DEFAULT_GATEWAY_URL
    app_name: str = "GCAE_OptionFlow"
    app_version: str = "1.0.0"

    @property
    def is_complete(self) -> bool:
        return all([self.user, self.password, self.system_name, self.url])


def _parse_env(path: str) -> dict[str, str]:
    out: dict[str, str] = {}
    if not os.path.exists(path):
        return out
    with open(path, encoding="utf-8") as fh:
        for line in fh:
            line = line.strip()
            if not line or line.startswith("#") or "=" not in line:
                continue
            k, _, v = line.partition("=")
            out[k.strip()] = v.strip()
    return out


def load_config(path: str | None = None) -> RithmicConfig:
    path = path or os.environ.get("GCAE_RITHMIC_ENV", DEFAULT_ENV_PATH)
    env = _parse_env(path)
    # Environment variables win over the file, so nothing forces secrets to disk.
    # A blank value (e.g. "RITHMIC_SYSTEM_NAME=") counts as absent so defaults apply.
    def g(key, default=""):
        v = os.environ.get(key) or env.get(key)
        return v if v else default

    return RithmicConfig(
        user=g("RITHMIC_USER"),
        password=g("RITHMIC_PASSWORD"),
        system_name=g("RITHMIC_SYSTEM_NAME", DEFAULT_SYSTEM_NAME),
        url=g("RITHMIC_GATEWAY_URL", DEFAULT_GATEWAY_URL),
        app_name=g("RITHMIC_APP_NAME", "GCAE_OptionFlow"),
        app_version=g("RITHMIC_APP_VERSION", "1.0.0"),
    )
