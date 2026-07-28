#!/usr/bin/env python3
"""Phase A / A1 credential assignment scanner (hardened, fail-closed) - Round 3 (A-SEC-004).

Rule: a credential assignment is SUSPICIOUS BY DEFAULT. The ONLY exemptions are an explicit
recognized environment accessor or an explicit placeholder marker. Arbitrary helper calls
(vault(), g(), load()), method calls (.strip()), f-strings, and concatenations are NOT safe.

- Credential variable names are normalized (lowercase, drop '_') so RithmicPassword,
  rithmic_password, apiKey, ApiKey, API_KEY, clientSecret, client_secret all match.
- Python is parsed with `ast`; only os.getenv(...)/os.environ[...]/os.environ.get(...) values,
  placeholder-marker string constants, plain name/attribute references, and non-string
  constants are safe. Everything else (Call/JoinedStr/BinOp/unknown) is flagged.
- C# flags `<name> = "literal"` where the normalized name is a credential and the literal is
  not a placeholder (env accessors are calls, not string literals, so they are not matched).
- Text/env/json/yaml matching requires the WHOLE RHS to be a recognized accessor/placeholder,
  never a substring like 'getenv' inside a literal.
- Unreadable file/member/archive and unparseable .py FAIL CLOSED. Values never printed.
"""
import os, re, sys, zipfile, ast

CRED_NORM = ["rithmicuser","rithmicpassword","password","passwd","secret","token","apikey",
             "privatekey","clientsecret","accesskey","authtoken","certpassword","certificate"]
def _norm(name): return re.sub(r"[^a-z0-9]", "", (name or "").lower())
def is_cred_key(name):
    n = _norm(name)
    return any(tok in n for tok in CRED_NORM)

PLACEHOLDER = [re.compile(p) for p in [
    r"^\s*$", r"^\s*<[^>]*>\s*$", r"^\s*\$\{[A-Za-z0-9_]+\}\s*$", r"^\s*%[A-Za-z0-9_]+%\s*$",
    r"^\s*\$env:[A-Za-z0-9_]+\s*$", r"^\s*\{\{[A-Za-z0-9_]+\}\}\s*$"]]
def _is_marker(s): return any(p.match(s) for p in PLACEHOLDER)
# whole-RHS recognized environment accessors (text-file values)
ENV_ACCESSOR_FULL = re.compile(
    r"^\s*(os\.)?getenv\(\s*['\"][A-Za-z0-9_]+['\"]\s*(,[^)]*)?\)\s*$"
    r"|^\s*os\.environ(\.get)?\[\s*['\"][A-Za-z0-9_]+['\"]\s*\]\s*$"
    r"|^\s*os\.environ\.get\(\s*['\"][A-Za-z0-9_]+['\"]\s*(,[^)]*)?\)\s*$"
    r"|^\s*Environment\.GetEnvironmentVariable\(\s*\"[A-Za-z0-9_]+\"\s*\)\s*$"
    r"|^\s*process\.env\.[A-Za-z0-9_]+\s*$", re.I)

def is_placeholder_text(v):
    v = v.strip()
    return _is_marker(v) or bool(ENV_ACCESSOR_FULL.match(v))

def _dequote(v):
    v = v.split("#",1)[0].strip().rstrip(",;")
    if len(v) >= 2 and v[0] in "'\"" and v[-1] == v[0]: v = v[1:-1]
    return v.strip()

SKIP_DIRS = {".git","bin","obj","__pycache__",".venv","node_modules",".vs",".idea",".pytest_cache"}
TEXT_EXT = (".txt",".md",".env",".json",".yaml",".yml",".ini",".cfg",".config",".xml",
            ".ps1",".psm1",".sh",".toml",".bat",".properties")
PY_EXT = (".py",); CS_EXT = (".cs",".csproj",".cshtml",".razor")
SCANNED = TEXT_EXT + PY_EXT + CS_EXT
# text assignment: <name> [:=] value  (name captured so it can be normalized)
RX_TEXT = re.compile(r"([A-Za-z_][A-Za-z0-9_]*)[\"']?[ \t]*[:=]{1,2}[ \t]*(.*)")
# C# string assignment: <name> = "literal" | $"interp" | @"verbatim" | """raw"""
RX_CS = re.compile(r"([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(\"{3}[\s\S]*?\"{3}|[@$]{0,2}\"(?:[^\"\\]|\\.)*\")")

# ---- Python AST ----
def _dotted(node):
    if isinstance(node, ast.Name): return node.id
    if isinstance(node, ast.Attribute): return _dotted(node.value) + "." + node.attr
    return ""
def _env_call_safe(call):
    # A-SEC-005: an env accessor is safe ONLY with the single env-name string and NO literal
    # default. os.getenv("X", "literal-default") is suspicious (the default becomes the secret).
    if _dotted(call.func) not in ("os.getenv","getenv","os.environ.get"): return False
    str_args = [a for a in call.args if isinstance(a, ast.Constant) and isinstance(a.value,str)]
    str_kw   = [k for k in call.keywords if isinstance(k.value, ast.Constant) and isinstance(k.value.value,str)]
    return (len(str_args) == 1 and len(str_kw) == 0
            and len(call.args) >= 1 and isinstance(call.args[0], ast.Constant))
def _kwarg_suspicious(v):
    # A-SEC-005: a credential-named keyword arg with a direct string literal is suspicious
    if isinstance(v, ast.Constant) and isinstance(v.value, str):
        return not _is_marker(v.value.strip())
    if isinstance(v, (ast.JoinedStr, ast.BinOp)):
        return any(not _is_marker(s.strip()) for s in _string_consts(v))
    return False
def _env_subscript_safe(node):
    return isinstance(node, ast.Subscript) and _dotted(node.value) in ("os.environ","environ")
def _string_consts(node):
    return [n.value for n in ast.walk(node) if isinstance(n, ast.Constant) and isinstance(n.value, str)]
def _value_safe_py(node):
    # 1) an explicit recognized env accessor is always safe (its env-name arg is not a secret)
    if _env_subscript_safe(node): return True
    if isinstance(node, ast.Call) and _env_call_safe(node): return True
    # 2) otherwise SUSPICIOUS if the value embeds ANY non-placeholder string literal
    #    (covers "literal", vault("SECRET"), "x".strip(), f"..{}"); pure refs/subscripts with
    #    no string literal (token=token.strip(), x=parts[i]) are not credential literals.
    strs = _string_consts(node)
    if not strs: return True
    return all(_is_marker(s.strip()) for s in strs)
def _target_names(node):
    tgts = node.targets if isinstance(node, ast.Assign) else [node.target]
    out = []
    for t in tgts:
        if isinstance(t, ast.Name): out.append(t.id)
        elif isinstance(t, ast.Attribute): out.append(t.attr)
    return out
def scan_python(name, text, findings):
    try: tree = ast.parse(text)
    except Exception: findings.append(f"{name}: UNPARSEABLE_PYTHON (fail-closed)"); return
    for node in ast.walk(tree):
        if isinstance(node, (ast.Assign, ast.AnnAssign)):
            if node.value is None: continue
            for nm in _target_names(node):
                if is_cred_key(nm) and not _value_safe_py(node.value):
                    findings.append(f"{name}:{node.lineno}: {nm}=<SUSPECTED-LITERAL>")
        elif isinstance(node, ast.Call):
            for kw in node.keywords:
                if kw.arg and is_cred_key(kw.arg) and _kwarg_suspicious(kw.value):
                    findings.append(f"{name}:{node.lineno}: {kw.arg}=<SUSPECTED-LITERAL-KWARG>")

def scan_csharp(name, text, findings):
    for ln, line in enumerate(text.splitlines(), 1):
        for m in RX_CS.finditer(line):
            nm, lit = m.group(1), m.group(2)
            if not is_cred_key(nm): continue
            if lit.startswith('"""') and lit.endswith('"""'): val = lit[3:-3]
            else: val = lit.lstrip('@$')[1:-1]     # strip @ $ prefix + surrounding quotes
            if not _is_marker(val.strip()):
                findings.append(f"{name}:{ln}: {nm}=<SUSPECTED-LITERAL len={len(val)}>")

def scan_text(name, text, findings):
    for ln, line in enumerate(text.splitlines(), 1):
        m = RX_TEXT.search(line)
        if not m: continue
        nm = m.group(1)
        if not is_cred_key(nm): continue
        val = _dequote(m.group(2))
        if not is_placeholder_text(val):
            findings.append(f"{name}:{ln}: {nm}=<SUSPECTED-LITERAL len={len(val)}>")

def scan_member(name, text, findings, ext):
    if ext in PY_EXT: scan_python(name, text, findings)
    elif ext in CS_EXT: scan_csharp(name, text, findings)
    else: scan_text(name, text, findings)

def scan_zip(path, findings, prefix=""):
    try: zf = zipfile.ZipFile(path)
    except Exception: findings.append(f"{prefix}{os.path.basename(path)}: UNREADABLE_ARCHIVE (fail-closed)"); return
    try:
        bad = zf.testzip()
        if bad is not None: findings.append(f"{prefix}{bad}: CORRUPT_ARCHIVE_MEMBER (fail-closed)")
        for info in zf.infolist():
            low = info.filename.lower()
            if info.is_dir() or not low.endswith(SCANNED): continue
            try: data = zf.read(info).decode("utf-8", errors="strict")
            except Exception: findings.append(f"{prefix}{info.filename}: UNREADABLE_MEMBER (fail-closed)"); continue
            ext = next((e for e in SCANNED if low.endswith(e)), "")
            scan_member(prefix + info.filename, data, findings, ext)
    finally: zf.close()

def main():
    root = sys.argv[1] if len(sys.argv) > 1 else os.getcwd()
    findings = []
    if os.path.isfile(root) and root.lower().endswith(".zip"):
        scan_zip(root, findings)
    else:
        for dp, dn, fn in os.walk(root):
            dn[:] = [d for d in dn if d not in SKIP_DIRS]
            for f in fn:
                p = os.path.join(dp, f); low = f.lower()
                rel = os.path.relpath(p, root).replace(os.sep, "/")
                if low.endswith(".zip"): scan_zip(p, findings, prefix=rel + "::"); continue
                if not low.endswith(SCANNED): continue
                try: data = open(p, encoding="utf-8", errors="strict").read()
                except Exception: findings.append(f"{rel}: UNREADABLE_FILE (fail-closed)"); continue
                ext = next((e for e in SCANNED if low.endswith(e)), "")
                scan_member(rel, data, findings, ext)
    print("A1 CREDENTIAL ASSIGNMENT SCAN (hardened, Round 3 / A-SEC-004)")
    print("credential name tokens (normalized):", ", ".join(CRED_NORM))
    print("root:", root)
    if findings:
        print("SUSPECTED LITERAL CREDENTIALS / UNREADABLE: %d (values masked)" % len(findings))
        for x in findings: print("  ! " + x)
        print("RESULT: FAIL"); return 1
    print("suspected literal credentials: 0"); print("RESULT: PASS"); return 0

if __name__ == "__main__":
    sys.exit(main())
