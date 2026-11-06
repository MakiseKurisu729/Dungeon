# -*- coding: utf-8 -*-
"""M7-3 结构验证器（只读；仅标准库）。

用法：
  py -3.12 -X utf8 Docs/QA/Evidence/M7-3/verify_m7_3.py --stage 1
  py -3.12 -X utf8 Docs/QA/Evidence/M7-3/verify_m7_3.py --stage 4 --diff-base 164013c3
断言：M7-3.Asmdef / M7-3.EditorOnly / M7-3.NoAsset / M7-3.Files
有 FAIL 则 ExitCode=1。
"""
import argparse
import json
import os
import subprocess
import sys

SCRIPT_ROOT = os.path.join("Assets", "_Project", "Scripts")
TEST_ROOT = os.path.join("Assets", "_Project", "Tests")
BASE_DEFAULT = "164013c3"

# (相对 Scripts 的路径, name, references, includePlatforms)
SPECS = [
    ("Core/Dungeon.Core.asmdef", "Dungeon.Core", [], []),
    ("Content/Dungeon.Content.asmdef", "Dungeon.Content", ["Dungeon.Core"], []),
    ("Gameplay/Dungeon.Gameplay.asmdef", "Dungeon.Gameplay",
     ["Dungeon.Core", "Dungeon.Content", "UnityEngine.UI"], []),
    ("App/Dungeon.App.asmdef", "Dungeon.App",
     ["Dungeon.Core", "Dungeon.Content", "Dungeon.Gameplay"], []),
    ("UI/Dungeon.UI.asmdef", "Dungeon.UI",
     ["Dungeon.Core", "Dungeon.Content", "Dungeon.Gameplay", "Dungeon.App", "UnityEngine.UI"], []),
    ("Editor/Dungeon.Editor.asmdef", "Dungeon.Editor",
     ["Dungeon.Core", "Dungeon.Content", "Dungeon.Gameplay", "Dungeon.App", "Dungeon.UI"], ["Editor"]),
]
STAGE_KEYS = {1: ["Dungeon.Core", "Dungeon.Content", "Dungeon.Gameplay"],
              2: ["Dungeon.Core", "Dungeon.Content", "Dungeon.Gameplay", "Dungeon.App"],
              3: ["Dungeon.Core", "Dungeon.Content", "Dungeon.Gameplay", "Dungeon.App", "Dungeon.UI"],
              4: [s[1] for s in SPECS]}
RUNTIME_NAMES = {"Dungeon.Core", "Dungeon.Content", "Dungeon.Gameplay", "Dungeon.App", "Dungeon.UI"}


def read_text(path):
    with open(path, "rb") as fh:
        return fh.read().decode("utf-8-sig")


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

    expected_names = set(STAGE_KEYS[args.stage])
    loaded = {}
    problems = []
    for relp, name, refs, inc in SPECS:
        p = os.path.join(SCRIPT_ROOT, relp)
        if name not in expected_names:
            continue
        if not os.path.exists(p):
            problems.append("missing " + relp)
            continue
        if not os.path.exists(p + ".meta"):
            problems.append("missing meta " + relp + ".meta")
        try:
            js = json.loads(read_text(p))
        except Exception as e:  # noqa: BLE001
            problems.append("bad-json %s %s" % (relp, e))
            continue
        loaded[name] = js
        if js.get("name") != name:
            problems.append("%s name=%s" % (relp, js.get("name")))
        if js.get("rootNamespace") != name:
            problems.append("%s rootNamespace=%s" % (relp, js.get("rootNamespace")))
        if js.get("references") != refs:
            problems.append("%s references=%s" % (relp, js.get("references")))
        if js.get("includePlatforms") != inc:
            problems.append("%s includePlatforms=%s" % (relp, js.get("includePlatforms")))
        if js.get("autoReferenced") is not True:
            problems.append("%s autoReferenced=%s" % (relp, js.get("autoReferenced")))
        if js.get("noEngineReferences") is not False:
            problems.append("%s noEngineReferences=%s" % (relp, js.get("noEngineReferences")))
    add(not problems, "M7-3.Asmdef", "stage=%d checked=%d problems=%s" %
        (args.stage, len(loaded), problems[:8]))

    # EditorOnly
    eo = []
    for name, js in loaded.items():
        refs = js.get("references") or []
        if name in RUNTIME_NAMES and "Dungeon.Editor" in refs:
            eo.append("%s references Dungeon.Editor" % name)
        if name == "Dungeon.Editor" and js.get("includePlatforms") != ["Editor"]:
            eo.append("Dungeon.Editor includePlatforms=%s" % (js.get("includePlatforms"),))
    add(not eo, "M7-3.EditorOnly",
        "editor_included=%s problems=%s" % ("Dungeon.Editor" in loaded, eo[:8]))

    # Files
    def count_cs(root):
        n = 0
        for dp, _, fns in os.walk(root):
            n += sum(1 for f in fns if f.endswith(".cs"))
        return n
    s_cs, t_cs = count_cs(SCRIPT_ROOT), count_cs(TEST_ROOT)
    add(s_cs == 81 and t_cs == 30, "M7-3.Files", "scripts=%d want=81 tests=%d want=30" % (s_cs, t_cs))

    # NoAsset：工作区相对 base，只允许 Scripts 下的 .asmdef/.asmdef.meta
    changed = set()
    p = git(["git", "diff", "--name-only", args.diff_base, "--", "Assets"])
    if p.returncode != 0:
        add(False, "M7-3.NoAsset", "git diff failed: " + p.stderr.decode("utf-8", "replace"))
    else:
        changed |= set(x for x in p.stdout.decode("utf-8", "replace").split("\n") if x.strip())
        p2 = git(["git", "ls-files", "--others", "--exclude-standard", "--", "Assets"])
        changed |= set(x for x in p2.stdout.decode("utf-8", "replace").split("\n") if x.strip())
        bad = [c for c in changed
               if not (c.startswith("Assets/_Project/Scripts/") and
                       (c.endswith(".asmdef") or c.endswith(".asmdef.meta")))]
        add(not bad, "M7-3.NoAsset", "changed=%d bad=%s" % (len(changed), bad[:8]))

    fails = 0
    for ok, aid, detail in res:
        print(("PASS " if ok else "FAIL ") + aid + " " + detail)
        if not ok:
            fails += 1
    print("SUMMARY stage=%d base=%s pass=%d fail=%d" % (args.stage, args.diff_base, len(res) - fails, fails))
    sys.exit(1 if fails else 0)


if __name__ == "__main__":
    main()
