"""Recursive inventory of the .gcae tree. Read-only; opens nothing for writing.

Every number in section 1-2 of the discovery report comes from this script. It prints the
exact predicates it uses so a reviewer can re-run them by hand.

usage: python inventory.py [root]
"""
import collections
import os
import subprocess
import sys

DEFAULT_ROOT = r"C:\Users\LOQ\.gcae"

# The extensions searched for source/config. Counted explicitly so the report cannot
# misstate how many were used.
SOURCE_EXT = (".py", ".cs", ".ps1", ".sh", ".bat", ".cmd", ".ipynb", ".sql",
              ".yaml", ".yml", ".toml", ".ini", ".cfg", ".proto", ".js", ".ts")


def walk_non_git(root):
    """Every file under root excluding anything inside a .git directory."""
    for base, dirs, files in os.walk(root):
        dirs[:] = [d for d in dirs if d != ".git"]
        for f in files:
            yield os.path.join(base, f)


def main():
    root = sys.argv[1] if len(sys.argv) > 1 else DEFAULT_ROOT
    print("###### .gcae RECURSIVE INVENTORY (recomputed) ######")
    print("root: %s" % root)
    print()

    all_files = [p for p in walk_non_git(root)]
    with_git = sum(len(fs) for _, _, fs in os.walk(root))
    print("=== 1. file counts ===")
    print("  predicate: os.walk(root), pruning any directory named '.git'")
    print("  files including .git : %d" % with_git)
    print("  files excluding .git : %d   <-- the figure used throughout the report" % len(all_files))
    print()

    print("=== 2. extension histogram (basename suffix, excluding .git) ===")
    ext = collections.Counter(os.path.splitext(p)[1].lower() or "(no extension)" for p in all_files)
    total = 0
    for e, n in ext.most_common():
        print("   %6d  %s" % (n, e))
        total += n
    print("   ------")
    print("   %6d  TOTAL  (must equal the excluding-.git count above: %s)"
          % (total, "OK" if total == len(all_files) else "MISMATCH"))
    print()

    print("=== 3. directory census ===")
    for rel in ("recorder/sessions", "recorder/samples"):
        d = os.path.join(root, *rel.split("/"))
        if not os.path.isdir(d):
            print("   %-20s <absent>" % rel)
            continue
        subs = sorted(x for x in os.listdir(d) if os.path.isdir(os.path.join(d, x)))
        mans = [s for s in subs if os.path.isfile(os.path.join(d, s, "manifest.json"))]
        print("   %-20s %3d directories, %3d with manifest.json, %d without"
              % (rel, len(subs), len(mans), len(subs) - len(mans)))
        for s in subs:
            if s not in mans:
                print("        NO MANIFEST: %s" % s)
    print()

    print("=== 4. source/config search ===")
    print("  extensions searched (%d): %s" % (len(SOURCE_EXT), " ".join(SOURCE_EXT)))
    hits = [p for p in all_files if os.path.splitext(p)[1].lower() in SOURCE_EXT]
    print("  matches: %d" % len(hits))
    for h in hits:
        print("     %s" % os.path.relpath(h, root))
    print()
    print("  equivalent shell predicate:")
    print("    find <root> -type f -not -path '*/.git/*' \\")
    print("      \\( %s \\)" % " -o ".join("-name '*%s'" % e for e in SOURCE_EXT))
    print()

    print("=== 5. same search across the DATA_rithmic GIT HISTORY (all commits) ===")
    try:
        revs = subprocess.run(["git", "rev-list", "--all"], cwd=root,
                              capture_output=True, text=True, check=True).stdout.split()
        seen = set()
        for r in revs:
            out = subprocess.run(["git", "ls-tree", "-r", "--name-only", r], cwd=root,
                                 capture_output=True, text=True).stdout.splitlines()
            for path in out:
                if os.path.splitext(path)[1].lower() in SOURCE_EXT:
                    seen.add(path)
        print("  commits examined     : %d" % len(revs))
        print("  source paths in history: %d" % len(seen))
        for p in sorted(seen):
            print("     %s" % p)
        if not seen:
            print("     (none)")
        print("  command: git rev-list --all | xargs -n1 git ls-tree -r --name-only")
    except Exception as e:
        print("  NOT DETERMINED: %s" % e)
    print()

    print("=== 6. size by top-level entry ===")
    for name in sorted(os.listdir(root)):
        if name == ".git":
            continue
        p = os.path.join(root, name)
        if os.path.isfile(p):
            print("   %12d bytes  %s" % (os.path.getsize(p), name))
        else:
            n = 0
            for b, d, fs in os.walk(p):
                d[:] = [x for x in d if x != ".git"]
                n += sum(os.path.getsize(os.path.join(b, f)) for f in fs)
            print("   %12d bytes  %s/" % (n, name))


if __name__ == "__main__":
    main()
