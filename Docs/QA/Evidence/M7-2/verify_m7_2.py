# -*- coding: utf-8 -*-
"""M7-2 结构验证器（只读；仅标准库）。

用法：
  py -3.12 -X utf8 Docs/QA/Evidence/M7-2/verify_m7_2.py --stage 1
  py -3.12 -X utf8 Docs/QA/Evidence/M7-2/verify_m7_2.py --stage 4 --diff-base 0a78111a
断言：M7-2.Structure.Files / .Namespace / .Meta / .Asmdef / M7-2.UpwardEdges / M7-2.NoAsset
有 FAIL 则 ExitCode=1。
"""
import argparse
import json
import os
import re
import subprocess
import sys

SCRIPT_ROOT = os.path.join("Assets", "_Project", "Scripts")
BASE_DEFAULT = "0a78111a"

DIR_NS = {
    "Core": "Dungeon.Core",
    "Content": "Dungeon.Content",
    "Gameplay": "Dungeon.Gameplay",
    "Gameplay/Units": "Dungeon.Gameplay.Units",
    "Gameplay/Combat": "Dungeon.Gameplay.Combat",
    "Gameplay/Player": "Dungeon.Gameplay.Player",
    "Gameplay/World": "Dungeon.Gameplay.World",
    "Gameplay/Items": "Dungeon.Gameplay.Items",
    "Gameplay/Interaction": "Dungeon.Gameplay.Interaction",
    "App": "Dungeon.App",
    "UI": "Dungeon.UI",
    "Editor": "Dungeon.Editor",
}

NS_RE = re.compile(r"^\s*namespace\s+(\S+)\s*\{\s*$", re.M)

STAGE_COUNTS = {
    1: {"Core": 8, "Content": 3, "Gameplay/Units": 17, "Gameplay/Combat": 14,
        "Gameplay/Player": 5, "Gameplay/World": 9, "Gameplay/Items": 2,
        "Gameplay/Interaction": 3, "App": 6, "UI": 10, "Editor": 1},
    2: {"Core": 9, "Content": 3, "Gameplay/Units": 17, "Gameplay/Combat": 14,
        "Gameplay/Player": 5, "Gameplay/World": 9, "Gameplay/Items": 2,
        "Gameplay/Interaction": 3, "App": 6, "UI": 10, "Editor": 1},
    3: {"Core": 9, "Content": 3, "Gameplay": 1, "Gameplay/Units": 17, "Gameplay/Combat": 14,
        "Gameplay/Player": 5, "Gameplay/World": 9, "Gameplay/Items": 2,
        "Gameplay/Interaction": 3, "App": 6, "UI": 10, "Editor": 1},
    4: {"Core": 9, "Content": 3, "Gameplay": 1, "Gameplay/Units": 17, "Gameplay/Combat": 14,
        "Gameplay/Player": 5, "Gameplay/World": 10, "Gameplay/Items": 2,
        "Gameplay/Interaction": 3, "App": 6, "UI": 10, "Editor": 1},
}
STAGE_TOTAL = {1: 78, 2: 79, 3: 80, 4: 81}
STAGE_NEW = {
    1: ["Core/ProgressionState.cs"],
    2: ["Core/ProgressionState.cs", "Core/PauseState.cs"],
    3: ["Core/ProgressionState.cs", "Core/PauseState.cs", "Gameplay/IUiModalState.cs"],
    4: ["Core/ProgressionState.cs", "Core/PauseState.cs", "Gameplay/IUiModalState.cs",
        "Gameplay/World/TransitionRequest.cs"],
}
UPWARD_EXPECT = {1: 10, 2: 3, 3: 1, 4: 0}
# 允许出现这些 using（它们只是命名空间引用，不算代码表达式）；实际是否用到由编译把关
FORBIDDEN = [r"\bAppFlow\.", r"\bUIManager\.", r"\bTransitionFader\.", r"\bMetaState\."]


def rel(p):
    return p.replace("\\", "/")


def all_cs(root):
    out = []
    for dp, _, fns in os.walk(root):
        for fn in fns:
            if fn.endswith(".cs"):
                out.append(rel(os.path.join(dp, fn)))
    return sorted(out)


def read_text(path):
    with open(path, "rb") as fh:
        return fh.read().decode("utf-8-sig")


def strip_code(text):
    out = []
    i, n = 0, len(text)
    state = "code"
    while i < n:
        c = text[i]
        nxt = text[i + 1] if i + 1 < n else ""
        if state == "code":
            if c == "/" and nxt == "/":
                state = "line_comment"; out.append("  "); i += 2; continue
            if c == "/" and nxt == "*":
                state = "block_comment"; out.append("  "); i += 2; continue
            if c == '"':
                state = "string"; out.append(" "); i += 1; continue
            if c == "'":
                state = "char"; out.append(" "); i += 1; continue
            out.append(c); i += 1
        elif state == "line_comment":
            if c == "\n":
                state = "code"; out.append("\n")
            else:
                out.append(" ")
            i += 1
        elif state == "block_comment":
            if c == "*" and nxt == "/":
                state = "code"; out.append("  "); i += 2; continue
            out.append("\n" if c == "\n" else " "); i += 1
        elif state == "string":
            if c == "\\":
                out.append("  " if nxt else " "); i += 2; continue
            if c == '"':
                state = "code"
            out.append("\n" if c == "\n" else " "); i += 1
        elif state == "char":
            if c == "\\":
                out.append("  " if nxt else " "); i += 2; continue
            if c == "'":
                state = "code"
            out.append("\n" if c == "\n" else " "); i += 1
    return "".join(out)


def git(args):
    return subprocess.run(args, capture_output=True)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--diff-base", default=BASE_DEFAULT)
    ap.add_argument("--stage", type=int, default=4, choices=[1, 2, 3, 4])
    args = ap.parse_args()
    res = []

    def add(ok, aid, detail):
        res.append((ok, aid, detail))

    files = all_cs(SCRIPT_ROOT)
    rels = [f.split("Scripts/", 1)[-1] for f in files]
    counts = {}
    for r in rels:
        d = os.path.dirname(r).replace("\\", "/")
        counts[d] = counts.get(d, 0) + 1

    # --- Files
    want_counts = STAGE_COUNTS[args.stage]
    missing_new = [p for p in STAGE_NEW[args.stage] if p not in rels]
    ok = len(rels) == STAGE_TOTAL[args.stage] and counts == want_counts and not missing_new
    add(ok, "M7-2.Structure.Files",
        "n=%d want=%d counts=%s want_counts=%s missing_new=%s" %
        (len(rels), STAGE_TOTAL[args.stage], counts, want_counts, missing_new))

    # --- Namespace
    bad = []
    for r in rels:
        d = os.path.dirname(r).replace("\\", "/")
        expected = DIR_NS.get(d)
        found = NS_RE.findall(read_text(os.path.join(SCRIPT_ROOT, r)))
        if expected is None:
            bad.append("%s unknown-dir" % r)
        elif len(found) != 1 or found[0] != expected:
            bad.append("%s expected-%s got-%s" % (r, expected, found))
    add(not bad, "M7-2.Structure.Namespace", "bad=%d %s" % (len(bad), bad[:8]))

    # --- Meta（每个 .cs 有同名 .cs.meta；无孤儿）
    missing_meta = [r for r in rels if not os.path.exists(os.path.join(SCRIPT_ROOT, r) + ".meta")]
    orphan = []
    for dp, _, fns in os.walk(SCRIPT_ROOT):
        for fn in fns:
            if fn.endswith(".cs.meta"):
                cs = os.path.join(dp, fn[:-5])
                if not os.path.exists(cs):
                    orphan.append(rel(cs))
    add(not missing_meta and not orphan, "M7-2.Structure.Meta",
        "missing=%s orphan=%s" % (missing_meta[:8], orphan[:8]))

    # --- Asmdef
    problems = []
    for relp, refs in [("Core/Dungeon.Core.asmdef", []),
                       ("Content/Dungeon.Content.asmdef", ["Dungeon.Core"])]:
        p = os.path.join(SCRIPT_ROOT, relp)
        if not os.path.exists(p):
            problems.append("missing " + relp)
            continue
        try:
            js = json.loads(read_text(p))
        except Exception as e:  # noqa: BLE001
            problems.append("bad-json %s %s" % (relp, e))
            continue
        if js.get("references") != refs:
            problems.append("%s references=%s" % (relp, js.get("references")))
        if js.get("autoReferenced") is not True:
            problems.append("%s autoReferenced=%s" % (relp, js.get("autoReferenced")))
    add(not problems, "M7-2.Structure.Asmdef", "problems=%s" % problems[:8])

    # --- UpwardEdges（只扫 Gameplay/**，先剥离注释与字符串；按 (文件,行,引用) 去重，与 audit 边口径一致）
    hits = set()
    for f in files:
        if "/Gameplay/" not in f.replace("\\", "/"):
            continue
        code = strip_code(read_text(f))
        for pat in FORBIDDEN:
            for m in re.finditer(pat, code):
                line = code.count("\n", 0, m.start()) + 1
                hits.add("%s:%d:%s" % (rel(f).split("Scripts/", 1)[-1], line, m.group(0)))
    hits = sorted(hits)
    add(len(hits) == UPWARD_EXPECT[args.stage], "M7-2.UpwardEdges",
        "hits=%d want=%d sample=%s" % (len(hits), UPWARD_EXPECT[args.stage], hits[:8]))

    # --- NoAsset（工作区相对 base：只允许 Scripts/**/*.cs 与 .cs.meta）
    changed = set()
    p = git(["git", "diff", "--name-only", args.diff_base, "--", "Assets"])
    if p.returncode != 0:
        add(False, "M7-2.NoAsset", "git diff failed: " + p.stderr.decode("utf-8", "replace"))
    else:
        changed |= set(x for x in p.stdout.decode("utf-8", "replace").split("\n") if x.strip())
        p2 = git(["git", "ls-files", "--others", "--exclude-standard", "--", "Assets"])
        changed |= set(x for x in p2.stdout.decode("utf-8", "replace").split("\n") if x.strip())
        bad_paths = [c for c in changed
                     if not ((c.startswith("Assets/_Project/Scripts/") and
                              (c.endswith(".cs") or c.endswith(".cs.meta"))) or
                             c == "Assets/_Project/Tests/REFACTOR/Runtime/REFACTOR_ProbeRunner.cs")]
        add(not bad_paths, "M7-2.NoAsset",
            "changed=%d bad=%s" % (len(changed), bad_paths[:8]))

    fails = 0
    for ok, aid, detail in res:
        print(("PASS " if ok else "FAIL ") + aid + " " + detail)
        if not ok:
            fails += 1
    print("SUMMARY stage=%d base=%s pass=%d fail=%d" % (args.stage, args.diff_base, len(res) - fails, fails))
    sys.exit(1 if fails else 0)


if __name__ == "__main__":
    main()
