# Live FIN/Rithmic acquisition probe — result

**Outcome: `AUTHENTICATION_DENIED`. No new market data was acquired.**

> **Corrigendum applied.** Two statements in the first version of this document were wrong and
> are corrected below: the claim that a re-run would not leak (§3), and the system/gateway
> hypothesis (§4.1). Both were caught by the reviewer.

Per the instruction — *"If authentication fails, report the sanitized error and stop. Do not describe that
outcome as completion of the requested acquisition"* — the probe stopped at P0 and I am not presenting this
as the requested acquisition. **Stage 2 still has no newly acquired FIN/Rithmic data.**

`ODR-L1-05` was withdrawn by the owner as a gate and I did not stop to re-ask. The probe ran. The server
refused the login.

---

## 1. What happened

| | |
|---|---|
| session id | `probe_20260731T052809Z` |
| UTC start / stop | `2026-07-31T05:28:09Z` → `2026-07-31T05:28:11Z` |
| local start | `2026-07-31T12:28:09+07:00` |
| elapsed | 2.2 s |
| command | `python tools/rithmic_probe.py --out <quarantine> --window 25` |
| plants requested | `TICKER_PLANT` only |
| **authenticated** | **NO** |
| request accepted | n/a — never reached |
| subscription accepted | n/a |
| callbacks observed | **0** |
| bytes captured | **0** market-data bytes |
| clean teardown | n/a — no session established |

**Sanitized server response:**

```
template_id 11 (login response)
rp_code    : ['13', 'permission denied']
for request: template_id 10 (login), infra_type 1 (TICKER_PLANT)
             system_name '<REDACTED>', app_name 'GCAE_OptionFlow', app_version '1.0.0'
```

The client then raised `AttributeError: 'NoneType' object has no attribute 'heartbeat_interval'`
(`async_rithmic/plants/base.py:252`) because it reads the login response without checking that the login
succeeded. That secondary exception is a **library defect masking the real cause**; the real cause is
`rp_code 13 permission denied`.

## 2. Every required field, recorded

`probe/probe_20260731T052809Z_results.csv` — machine-readable, one row per probe step. Only P0 has a row,
because the sequence stopped there.

Distinguished as required:

| state | result |
|---|---|
| authenticated successfully | **NO** |
| request accepted | not reached |
| subscription accepted | not reached |
| callback observed | **NO** |
| non-empty values observed | **NO** |
| capture persisted successfully | transcript + results only; **zero market-data records** |

P1–P8 and P10 were never attempted. Nothing is claimed about entitlement for individual surfaces:
**a login denial is not evidence about `SETTLEMENT`, MBO, options or anything else.** Those rows stay
`NOT_PROBED` in `08_ACQUISITION_CAPABILITY_MATRIX.csv`.

## 3. A security finding that must not wait

**`async_rithmic` 1.6.3 writes the full login request — including the cleartext password — into its ERROR
log when the server denies the login.**

`plants/base.py:550` raises `RithmicErrorResponse(f"... for the request={request}")` where `request`
is the login message containing `user` and `password`. The record reaches the root logger and was printed
to the console on this run.

Containment, verified:

| check | result |
|---|---|
| password present in any repo file | **0 matches** |
| password present in any pushed commit | **0 matches** (`git grep` over both pushed branches) |
| password present in quarantine capture | **0 matches** |
| username present in repo | 1 match, `docs/review/phaseA/A_PHASE_A.patch`, **untracked — never committed, never pushed** |

So nothing leaked into git. The exposure was to the local console during this run.

**Fix applied to the probe — first attempt was WRONG and is corrected.**

My first fix installed a `logging.Filter` on the root logger and on the loggers that existed at install
time, and I claimed *"re-running the probe will not reproduce the console leak"*. **That claim was false**
and the reviewer rejected it correctly. Demonstrated in `raw/12_probe_redaction_selftest.txt`:

* `Logger.addFilter` only filters records logged **directly** on that logger; a record emitted on
  `rithmic.plant.history` propagates to root's **handlers** without passing root's logger-level filters;
* loggers created after install — exactly when the `HISTORY_PLANT` logger appears — received no filter;
* the handler line was guarded by `logging.getLogger().handlers and [...]`, so with no handler yet it
  silently installed nothing.

The replacement redacts at `logging.Handler.handle`, the one choke point every record passes regardless of
which logger produced it or when that logger was created; patching the class covers handlers that do not
exist yet. Proven by `tools/test_probe_redaction.py` — 9/9 offline, including a baseline that shows the
secret leaking without it, a logger created after install, a handler added after install, and a traceback
carrying the credential.

**This does not fix the library.** Any other process using `async_rithmic` with these credentials — the
existing `research/optionflow` collector included — will still log the password on a failed login.

Recorded as **`OBS-R8`**. It also makes `SEC-001` rotation materially more urgent than when it was filed:
the password has now been rendered to a console, and the account it belongs to is denied anyway.

## 4. Why the login was denied — hypotheses, not conclusions

I did **not** retry, because the instruction says to stop on authentication failure and because repeated
failed logins risk account lockout, which it also says to stop on. So the following are untested:

1. ~~**System/gateway pairing.**~~ **WITHDRAWN — I misread the source.** `rithmic/config.py:15-23`
   says the opposite of what I wrote: *"it connects account fin_… on system \"Rithmic Paper Trading\" via
   the Japan gateway rsc-jp.rithmic.com … That is the gateway/system pairing that actually serves GC/ES/NQ
   option data."* The configured pairing **is** the documented working one. My suggestion to try a
   "non-paper" system would have spent a login attempt on a hypothesis the repository already contradicts.
2. **Entitlement lapse.** The account last produced data on 2026-07-28. A market-data entitlement can
   expire or be withdrawn independently of the login being valid.
3. **Credential state.** `SEC-001` records the password as exposed and unrotated; if it was rotated
   out-of-band, the stored value is stale. I did not inspect the stored values.
4. **App identity.** `app_name='GCAE_OptionFlow'` may need registration with the vendor.

Each costs one login attempt, and with hypothesis 1 withdrawn none of the remaining three is
something I can settle from the repository. **A second login should only run after the operator or the
vendor confirms which pairing is valid, or confirms the credential / app-identity state.** Guessing is how
an account gets locked out, and the reviewer's ruling stands: the gate withdrawal covered the first run,
and the stop-on-auth-failure condition is now active.

## 5. What this does and does not change

**Unchanged and still true:** everything in `07_ACQUISITION_ADDENDUM_CORRIGENDUM.md`. The schema findings
stand on their own evidence — 17 non-zero `UpdateBits`, 14 unrequested; `DepthByOrder.sequence_number`
exists; per-level New/Change/Delete lives only on `DepthByOrder`; reference-data, bar-replay and permission
schemas exist; IV/Greeks absent across 104 modules; the ATAS path loses fields the wire carries.

**Not established, and not claimed:** that FIN can obtain any of it. The whole point of the probe was to
promote "schema exists" to "FIN can obtain it", and **that promotion has not happened for a single
surface.**

## 6. Deliverables

| file | what |
|---|---|
| `probe/probe_20260731T052809Z_transcript.txt` | sanitized transcript, quarantine path replaced |
| `probe/probe_20260731T052809Z_results.csv` | machine-readable per-step result, all required columns |
| `probe/probe_20260731T052809Z_capture_manifest.csv` | file, bytes, sha256, location |
| `tools/rithmic_probe.py` | the probe, now with credential log redaction |

Raw capture stays in `<quarantine>/probe_20260731T052809Z` outside `.gcae` and outside the repository. It
contains **no market data** — only the two files above. No credential, account identifier or unsanitized
transcript is committed.

`.gcae` and `DATA_rithmic` were not touched by the probe.
