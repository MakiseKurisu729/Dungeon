# -*- coding: utf-8 -*-
"""M7-4 全量回归结构验证器（只读；仅标准库）。

用法：
  py -3.12 -X utf8 Docs/QA/Evidence/M7-4/verify_m7_4.py --base 0c4c32e4
断言：M7-4.Asmdef / M7-4.EditorOnly / M7-4.SceneRefs / M7-4.Files / M7-4.NoAsset
有 FAIL 则 ExitCode=1。
"""
import argparse
import json
import os
import re
import subprocess
import sys

SCRIPT_ROOT = os.path.join("Assets", "_Project", "Scripts")
TEST_ROOT = os.path.join("Assets", "_Project", "Tests")
ASSETS = "Assets"
BASE_DEFAULT = "0c4c32e4"

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
RUNTIME_NAMES = {"Dungeon.Core", "Dungeon.Content", "Dungeon.Gameplay", "Dungeon.App", "Dungeon.UI"}
NS_RE = re.compile(r"^\s*namespace\s+\S+", re.M)


def read_text(path):
    with open(path, "rb") as fh:
        return fh.read().decode("utf-8-sig", errors="replace")


def git(args):
    return subprocess.run(args, capture_output=True)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default=BASE_DEFAULT)
    args = ap.parse_args()
    res = []

    def add(ok, aid, detail):
        res.append((ok, aid, detail))

    # --- Asmdef
    problems = []
    loaded = {}
    for relp, name, refs, inc in SPECS:
        p = os.path.join(SCRIPT_ROOT, relp)
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
    add(not problems and len(loaded) == 6, "M7-4.Asmdef",
        "asmdefs=%d problems=%s" % (len(loaded), problems[:8]))

    # --- EditorOnly
    eo = []
    for name, js in loaded.items():
        if name in RUNTIME_NAMES and "Dungeon.Editor" in (js.get("references") or []):
            eo.append("%s references Dungeon.Editor" % name)
        if name == "Dungeon.Editor" and js.get("includePlatforms") != ["Editor"]:
            eo.append("Dungeon.Editor includePlatforms=%s" % (js.get("includePlatforms"),))
    add(not eo, "M7-4.EditorOnly", "problems=%s" % eo[:8])

    # --- SceneRefs：所有 .unity/.prefab 的 m_Script guid 必须能解析
    # 解析范围：Assets/**/*.meta + Packages/**/*.meta + Library/PackageCache/**/*.meta
    # 例外：2 个 GUID 是基线 0c4c32e4 就已存在于第三方 PlayerCapsule.prefab 的缺失脚本
    #       （M7 未改 Assets，不归本单修；在 REPORT/目检清单列为 known issue）
    PREEXISTING = {
        "5135d2b4fb4ea6b4183453c2252e87bb",
        "9466e0370f31dfe47ae24c150b7fa083",
    }
    PREEXISTING_PREFAB = "Assets/StarterAssets/FirstPersonController/Prefabs/PlayerCapsule.prefab"

    def collect_guids(roots):
        s = set()
        n = 0
        for root in roots:
            if not os.path.isdir(root):
                continue
            for dp, _, fns in os.walk(root):
                for fn in fns:
                    if fn.endswith(".meta"):
                        n += 1
                        m = re.search(r"^guid:\s*([0-9a-f]{32})",
                                      read_text(os.path.join(dp, fn)), re.M)
                        if m:
                            s.add(m.group(1))
        return s, n

    guids, meta_count = collect_guids(["Assets", "Packages", os.path.join("Library", "PackageCache")])
    script_re = re.compile(r"m_Script:\s*\{[^}]*guid:\s*([0-9a-f]{32})")
    missing = []
    scanner_files = 0
    refs = 0
    for dp, _, fns in os.walk(ASSETS):
        for fn in fns:
            if not (fn.endswith(".unity") or fn.endswith(".prefab")):
                continue
            scanner_files += 1
            p = os.path.join(dp, fn)
            text = read_text(p)
            for m in script_re.finditer(text):
                refs += 1
                g = m.group(1)
                if g not in guids:
                    missing.append("%s:%s" % (p.replace("\\", "/"), g))
    unresolved = [x for x in missing if x.rsplit(":", 1)[-1] not in PREEXISTING]
    pre_hits = [x for x in missing if x.rsplit(":", 1)[-1] in PREEXISTING]
    # 证明确属基线既有：base 提交的同名 prefab 也含这些 GUID
    base_prefab = git(["git", "show", args.base + ":" + PREEXISTING_PREFAB]).stdout.decode("utf-8", "replace")
    pre_ok = all(g in base_prefab for g in PREEXISTING)
    add(not unresolved and pre_ok, "M7-4.SceneRefs",
        "scanned_files=%d refs=%d metas=%d unresolved=%d preexisting=%d pre_base_ok=%s %s" %
        (scanner_files, refs, meta_count, len(unresolved), len(pre_hits), pre_ok, unresolved[:8]))

    # --- Files + git status
    def count_cs(root):
        n = 0
        for dp, _, fns in os.walk(root):
            n += sum(1 for f in fns if f.endswith(".cs"))
        return n
    tests = []
    ns_tests = []
    for dp, _, fns in os.walk(TEST_ROOT):
        for fn in fns:
            if fn.endswith(".cs"):
                p = os.path.join(dp, fn)
                tests.append(p)
                if NS_RE.search(read_text(p)):
                    ns_tests.append(p)
    s_cs = count_cs(SCRIPT_ROOT)
    status = git(["git", "-c", "core.quotepath=false", "status", "--porcelain"]).stdout.decode("utf-8", "replace")
    bad_status = []
    for line in status.split("\n"):
        if not line.strip():
            continue
        path = line[3:].strip().strip('"')
        # 本单允许的 Docs 路径（其余一律 FAIL）
        ok = (path.startswith("Docs/QA/Evidence/M7-4/") or
              path in ("Docs/交接/REPORT_M7-4_全量回归.md",
                       "Docs/交接/TASK_M7-4_全量回归.md",
                       "Docs/交接/长任务计划_M7-4.md") or
              any(path.startswith(p) for p in (
                  "Docs/QA/Evidence/REFACTOR-M4/editor_run42/",
                  "Docs/QA/Evidence/REFACTOR-M4/editor_run43/",
                  "Docs/QA/Evidence/SPEC-01/editor_run61/",
                  "Docs/QA/Evidence/SPEC-01/editor_run62/",
                  "Docs/QA/Evidence/FND-00B-06/editor_run11/",
                  "Docs/QA/Evidence/FND-00B-06/editor_run12/",
                  "Docs/QA/Evidence/DIAG-SKILL/editor_run13/",
                  "Docs/QA/Evidence/DIAG-SKILL/editor_run14/",
              )))
        if not ok:
            bad_status.append(line.strip())
    add(s_cs == 81 and len(tests) == 30 and not ns_tests and not bad_status, "M7-4.Files",
        "scripts=%d tests=%d namespace_tests=%d bad_status=%s" %
        (s_cs, len(tests), len(ns_tests), bad_status[:8]))

    # --- NoAsset
    p = git(["git", "diff", "--name-only", args.base, "--", "Assets"])
    changed = set(x for x in p.stdout.decode("utf-8", "replace").split("\n") if x.strip())
    p2 = git(["git", "ls-files", "--others", "--exclude-standard", "--", "Assets"])
    changed |= set(x for x in p2.stdout.decode("utf-8", "replace").split("\n") if x.strip())
    add(not changed, "M7-4.NoAsset", "changed=%d %s" % (len(changed), sorted(changed)[:8]))

    fails = 0
    for ok, aid, detail in res:
        print(("PASS " if ok else "FAIL ") + aid + " " + detail)
        if not ok:
            fails += 1
    print("SUMMARY base=%s pass=%d fail=%d" % (args.base, len(res) - fails, fails))
    sys.exit(1 if fails else 0)


if __name__ == "__main__":
    main()
