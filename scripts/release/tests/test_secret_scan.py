#!/usr/bin/env python3
"""Tests for scripts/release/secret_scan.py (A-SEC-002/003/004).
No real credential. Code fixtures are built by concatenation so this file contains no
credential-key assignment for the AST scanner to flag.
Run: python scripts/release/tests/test_secret_scan.py [logpath]  -> exit 0 all pass.
"""
import os, sys, tempfile, zipfile, importlib.util

HERE = os.path.dirname(os.path.abspath(__file__))
SPEC = importlib.util.spec_from_file_location("secret_scan", os.path.join(HERE, "..", "secret_scan.py"))
ss = importlib.util.module_from_spec(SPEC); SPEC.loader.exec_module(ss)

LIT="hunter"+"2plain"; SPACED="synthetic "+"secret value"; Q='"'; EQ="="; CO=":"
n_pass=0; n_fail=0; lines=[]
def check(d,c):
    global n_pass,n_fail
    if c: n_pass+=1; lines.append("PASS  "+d)
    else: n_fail+=1; lines.append("FAIL  "+d)
def fpy(code): f=[]; ss.scan_python("t.py",code,f); return len(f)>0
def fcs(code): f=[]; ss.scan_csharp("t.cs",code,f); return len(f)>0
def ftx(code): f=[]; ss.scan_text("t.env",code,f); return len(f)>0

# ---- text: only markers + whole-RHS env accessors are safe ----
for ph in ["", "<your password>", "${RITHMIC_PASSWORD}", "%TOKEN%", "$env:RITHMIC_USER"]:
    check("text placeholder safe "+repr(ph), not ftx("RITHMIC_PASSWORD"+EQ+ph))
check("text os.getenv safe", not ftx("PASSWORD"+EQ+"os.getenv("+Q+"PASSWORD"+Q+")"))
check("text Environment accessor safe", not ftx("PASSWORD"+EQ+"Environment.GetEnvironmentVariable("+Q+"PASSWORD"+Q+")"))
check("A-SEC-004 substring getenv NOT safe", ftx("PASSWORD"+EQ+"literal_getenv_secret"))
check("text helper call flagged", ftx("PASSWORD"+EQ+"g("+Q+"RITHMIC_PASSWORD"+Q+")"))
check("text literal-with-space flagged", ftx("RITHMIC_PASSWORD"+EQ+SPACED))
check("text lowercase key flagged", ftx("password"+EQ+LIT))
check("text non-cred key ignored", not ftx("cancellation_delay"+EQ+LIT))

# ---- Python AST ----
check("py generic literal flagged", fpy("password "+EQ+" "+Q+SPACED+Q))
check("A-SEC-004 py vault() flagged", fpy("password "+EQ+" vault("+Q+"SYNTHETIC_SECRET"+Q+")"))
check("A-SEC-004 py .strip() flagged", fpy("password "+EQ+" "+Q+"synthetic secret"+Q+".strip()"))
check("A-SEC-004 py f-string flagged", fpy("password "+EQ+" f"+Q+"synthetic-{123}"+Q))
check("py concat flagged", fpy("password "+EQ+" "+Q+"a"+Q+" + "+Q+"b"+Q))
check("py os.getenv safe", not fpy("password "+EQ+" os.getenv("+Q+"RITHMIC_PASSWORD"+Q+")"))
check("py os.environ subscript safe", not fpy("password "+EQ+" os.environ["+Q+"RITHMIC_PASSWORD"+Q+"]"))
check("py helper call flagged (not env accessor)", fpy("password "+EQ+" g("+Q+"RITHMIC_PASSWORD"+Q+")"))
check("py annotation NOT flagged", not fpy("password"+CO+" str"))
check("py variable ref NOT flagged", not fpy("password "+EQ+" some_var"))
check("py kwarg NOT flagged", not fpy("cfg "+EQ+" Config(password"+EQ+"g("+Q+"X"+Q+"))"))
check("py unparseable fails closed", fpy("def (((("))
# ---- A-SEC-005 ----
check("A-SEC-005 getenv literal default flagged", fpy("password "+EQ+" os.getenv("+Q+"PASSWORD"+Q+", "+Q+"synthetic-default"+Q+")"))
check("py getenv variable default NOT flagged", not fpy("password "+EQ+" os.getenv("+Q+"PASSWORD"+Q+", fallback_var)"))
check("A-SEC-005 credential kwarg literal flagged", fpy("Config(password"+EQ+Q+"synthetic-value"+Q+")"))
check("py credential kwarg helper NOT flagged", not fpy("Config(password"+EQ+"g("+Q+"RITHMIC_PASSWORD"+Q+"))"))

# ---- C# (normalized names) ----
check("cs token literal flagged", fcs("string token "+EQ+" "+Q+LIT+Q+";"))
check("A-SEC-004 cs RithmicPassword flagged", fcs("const string RithmicPassword "+EQ+" "+Q+"synthetic secret"+Q+";"))
check("cs ApiKey flagged", fcs("const string ApiKey "+EQ+" "+Q+LIT+Q+";"))
check("cs clientSecret flagged", fcs("string clientSecret "+EQ+" "+Q+LIT+Q+";"))
check("cs call RHS NOT flagged", not fcs("var password "+EQ+" GetPassword();"))
check("cs interpolated NOT flagged", not fcs("return $"+Q+"token:0x{t:X8}"+Q+";"))
check("cs env accessor NOT flagged", not fcs("string password "+EQ+" Environment.GetEnvironmentVariable("+Q+"X"+Q+");"))
check("cs non-cred var ignored", not fcs("string message "+EQ+" "+Q+LIT+Q+";"))
check("A-SEC-005 cs interpolated Password flagged", fcs("string Password "+EQ+" $"+Q+"synthetic-{suffix}"+Q+";"))
check("A-SEC-005 cs raw Password flagged", fcs("string Password "+EQ+" "+Q+Q+Q+"synthetic-value"+Q+Q+Q+";"))

# ---- ZIP + fail-closed ----
tmp = tempfile.mkdtemp()
zb=os.path.join(tmp,"b.zip"); zipfile.ZipFile(zb,"w").writestr("c.env","RITHMIC_PASSWORD"+EQ+LIT+"\n")
zg=os.path.join(tmp,"g.zip"); zipfile.ZipFile(zg,"w").writestr("c.env","RITHMIC_PASSWORD"+EQ+"<your password>\n")
fb=[]; ss.scan_zip(zb,fb); check("zip literal flagged", len(fb)>0)
fg=[]; ss.scan_zip(zg,fg); check("zip placeholder clean", len(fg)==0)
zc=os.path.join(tmp,"c.zip"); open(zc,"wb").write(b"PK\x03\x04 not a zip"); fc=[]; ss.scan_zip(zc,fc); check("corrupt zip fails closed", len(fc)>0)
zx=os.path.join(tmp,"x.zip"); zipfile.ZipFile(zx,"w").writestr("x.env",b"\xff\xfe\x00bad"); fx=[]; ss.scan_zip(zx,fx); check("undecodable member fails closed", len(fx)>0)
fv=[]; ss.scan_text("t.env","PASSWORD"+EQ+LIT,fv); check("finding masks value", all(LIT not in x for x in fv))

lines+=["-"*56, "passed=%d failed=%d"%(n_pass,n_fail), "RESULT: "+("PASS" if n_fail==0 else "FAIL")]
out="\n".join(["A_SECRET_SCAN_TEST - scanner unit tests (no real credential)","-"*56]+lines)
print(out)
if len(sys.argv)>1: open(sys.argv[1],"w",encoding="utf-8").write(out+"\n")
sys.exit(0 if n_fail==0 else 1)
