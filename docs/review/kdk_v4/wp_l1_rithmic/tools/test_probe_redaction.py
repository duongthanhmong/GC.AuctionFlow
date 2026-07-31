"""Offline proof that credential redaction covers loggers created AFTER install.

The reviewer rejected the first redaction because it filtered the root logger and the
loggers existing at install time, which leaves later loggers - notably the HISTORY_PLANT
logger - unprotected. This test reproduces that failure mode and proves the replacement
closes it.

No socket, no credential load, no async_rithmic import.

usage: python test_probe_redaction.py
"""
import io
import logging
import os
import sys

sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from rithmic_probe import install_log_redaction  # noqa: E402

USER = "probe_user_placeholder"
PASSWORD = "probe_password_placeholder"
FAILURES = []


def check(name, ok, detail=""):
    print("  %-64s %s%s" % (name, "PASS" if ok else "FAIL", ("  " + detail) if detail else ""))
    if not ok:
        FAILURES.append(name)


def fresh_root():
    root = logging.getLogger()
    for h in list(root.handlers):
        root.removeHandler(h)
    buf = io.StringIO()
    h = logging.StreamHandler(buf)
    h.setFormatter(logging.Formatter("%(message)s"))
    root.addHandler(h)
    root.setLevel(logging.DEBUG)
    return buf


def main():
    print("###### probe credential-redaction self-tests ######")
    print()

    # 1. Baseline: without redaction the secret reaches the handler. If this ever fails the
    #    rest of the suite proves nothing.
    buf = fresh_root()
    logging.getLogger("baseline.demo").error("login request password=%s", PASSWORD)
    check("baseline (no redaction): secret DOES reach the handler",
          PASSWORD in buf.getvalue())

    original = install_log_redaction(USER, PASSWORD)
    try:
        # 2. The case that broke the first attempt: a logger created after install.
        buf = fresh_root()
        logging.getLogger("rithmic.plant.history").error(
            "Rithmic returned an error for request={'user': '%s', 'password': '%s'}",
            USER, PASSWORD)
        out = buf.getvalue()
        check("logger created AFTER install: password redacted", PASSWORD not in out)
        check("logger created AFTER install: user redacted", USER not in out)
        check("logger created AFTER install: record still emitted", "<REDACTED>" in out)

        # 3. A handler added after install must also be covered.
        buf2 = io.StringIO()
        h2 = logging.StreamHandler(buf2)
        h2.setFormatter(logging.Formatter("%(message)s"))
        logging.getLogger().addHandler(h2)
        logging.getLogger("rithmic.plant.ticker").error("pw=%s", PASSWORD)
        check("handler added AFTER install: password redacted",
              PASSWORD not in buf2.getvalue())
        logging.getLogger().removeHandler(h2)

        # 4. A traceback carrying the credential must be suppressed, not emitted.
        buf = fresh_root()
        try:
            raise RuntimeError("request={'password': '%s'}" % PASSWORD)
        except RuntimeError:
            logging.getLogger("rithmic.plant.order").exception("failed")
        out = buf.getvalue()
        check("exception traceback containing credential is suppressed", PASSWORD not in out)
        check("suppression is announced rather than silent",
              "traceback suppressed" in out, out.strip()[:60])

        # 5. Ordinary records must pass through untouched.
        buf = fresh_root()
        logging.getLogger("rithmic.plant.ticker").info("subscribed GCZ6 COMEX bits=3")
        check("unrelated record passes through unchanged",
              "subscribed GCZ6 COMEX bits=3" in buf.getvalue())

        # 6. Deep child loggers.
        buf = fresh_root()
        logging.getLogger("a.b.c.d.e").error("x %s y", PASSWORD)
        check("deeply nested child logger: password redacted", PASSWORD not in buf.getvalue())

    finally:
        if original is not None:
            logging.Handler.handle = original
        for h in list(logging.getLogger().handlers):
            logging.getLogger().removeHandler(h)

    print()
    if FAILURES:
        print("RESULT: FAIL (%d)" % len(FAILURES))
        for f in FAILURES:
            print("   %s" % f)
        return 1
    print("RESULT: PASS - redaction holds for loggers and handlers created after install")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
