# -*- coding: utf-8 -*-
"""M7-1 结构验证器（只读；仅标准库）。

用法：
  py -3.12 -X utf8 Docs/QA/Evidence/M7-1/verify_m7_1.py --stage 1
  py -3.12 -X utf8 Docs/QA/Evidence/M7-1/verify_m7_1.py --stage 5 --base 483678fb
  py -3.12 -X utf8 Docs/QA/Evidence/M7-1/verify_m7_1.py --stage 5 --diff-scan   # 提交后跑

断言 ID（与 TASK §3.1 对齐）：
  M7-1.Structure.Files / .Namespace / .Meta / .Tests / .Asmdef
  M7-1.Audit / M7-1.NoLogicChange / M7-1.RaiseChanged
有 FAIL 则 ExitCode=1；SKIP 不计失败。
"""
import argparse
import json
import os
import re
import subprocess
import sys

SCRIPT_ROOT = os.path.join("Assets", "_Project", "Scripts")
TEST_ROOT = os.path.join("Assets", "_Project", "Tests")
EVIDENCE = os.path.join("Docs", "QA", "Evidence", "M7-1")
BASE_DEFAULT = "483678fb"

DIR_NS = {
    "Core": "Dungeon.Core",
    "Content": "Dungeon.Content",
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

# 文件名 -> 最终目录（77 个，名字唯一）
FINAL_DIR = {}
for _d, _names in {
    "Core": ["EventBus", "UnitId", "MessageFeed", "RunState", "Team", "ICharacterCombat", "Cooldown"],
    "Content": ["UnitStats", "WeaponData", "ItemData"],
    "Gameplay/Units": ["BossBrain", "CharacterBrain", "DownedRescue", "MonsterBrain", "ShieldedUnitHealth",
                       "SkeletonWander", "SkillData", "SkillSystem", "SleepingSkeleton", "UnitAnimatorSpeed",
                       "UnitBrain", "UnitHealth", "UnitMana", "UnitMotion", "Visibility", "WorldHealthBar",
                       "UnitRegistry"],
    "Gameplay/Combat": ["CombatFeedback", "DamageInfo", "DamageNumber", "ExplosionFx", "FireballProjectile",
                        "HeavySlashEffect", "MeleeCombat", "Projectile", "RangedCombat", "SimplePool",
                        "SkillCastContext", "SkillEffectBase", "SkillEffectFireball", "SkillEffects"],
    "Gameplay/Player": ["PlayerCommandController", "Selectable", "SelectionManager", "SkillIndicator",
                        "SkillInputController"],
    "Gameplay/World": ["Chest", "DungeonDoor", "FogOfWar", "ForestGenerator", "Interactable", "SkeletonSpawner",
                       "DebugCameraSwitcher", "Mirror", "WaveSpawner"],
    "Gameplay/Items": ["ChestItem", "Inventory"],
    "Gameplay/Interaction": ["IInteractionTarget", "InteractionDefinition", "InteractionRunner"],
    "App": ["AppFlow", "GameData", "GameFlow", "MetaState", "PauseManager", "RunFlow"],
    "UI": ["BasePanel", "CursorManager", "EscMenuPanel", "HUD", "InventoryUI", "MainMenuPanel", "SkillBarUI",
           "SlotClickHandler", "TransitionFader", "UIManager"],
    "Editor": ["DebugTestTools"],
}.items():
    for _n in _names:
        FINAL_DIR[_n] = _d

# 原始路径（相对 Scripts/） -> (新路径, 迁移批次)
MOVE = {
    "Units/Team.cs": ("Core/Team.cs", 1),
    "Combat/ICharacterCombat.cs": ("Core/ICharacterCombat.cs", 1),
    "Framework/Cooldown.cs": ("Core/Cooldown.cs", 1),
    "Units/UnitStats.cs": ("Content/UnitStats.cs", 1),
    "Combat/WeaponData.cs": ("Content/WeaponData.cs", 1),
    "Items/ItemData.cs": ("Content/ItemData.cs", 1),
    "Units/BossBrain.cs": ("Gameplay/Units/BossBrain.cs", 2),
    "Units/CharacterBrain.cs": ("Gameplay/Units/CharacterBrain.cs", 2),
    "Units/DownedRescue.cs": ("Gameplay/Units/DownedRescue.cs", 2),
    "Units/MonsterBrain.cs": ("Gameplay/Units/MonsterBrain.cs", 2),
    "Units/ShieldedUnitHealth.cs": ("Gameplay/Units/ShieldedUnitHealth.cs", 2),
    "Units/SkeletonWander.cs": ("Gameplay/Units/SkeletonWander.cs", 2),
    "Units/SkillData.cs": ("Gameplay/Units/SkillData.cs", 2),
    "Units/SkillSystem.cs": ("Gameplay/Units/SkillSystem.cs", 2),
    "Units/SleepingSkeleton.cs": ("Gameplay/Units/SleepingSkeleton.cs", 2),
    "Units/UnitAnimatorSpeed.cs": ("Gameplay/Units/UnitAnimatorSpeed.cs", 2),
    "Units/UnitBrain.cs": ("Gameplay/Units/UnitBrain.cs", 2),
    "Units/UnitHealth.cs": ("Gameplay/Units/UnitHealth.cs", 2),
    "Units/UnitMana.cs": ("Gameplay/Units/UnitMana.cs", 2),
    "Units/UnitMotion.cs": ("Gameplay/Units/UnitMotion.cs", 2),
    "Units/Visibility.cs": ("Gameplay/Units/Visibility.cs", 2),
    "Units/WorldHealthBar.cs": ("Gameplay/Units/WorldHealthBar.cs", 2),
    "Core/UnitRegistry.cs": ("Gameplay/Units/UnitRegistry.cs", 2),
    "Combat/CombatFeedback.cs": ("Gameplay/Combat/CombatFeedback.cs", 2),
    "Combat/DamageInfo.cs": ("Gameplay/Combat/DamageInfo.cs", 2),
    "Combat/DamageNumber.cs": ("Gameplay/Combat/DamageNumber.cs", 2),
    "Combat/ExplosionFx.cs": ("Gameplay/Combat/ExplosionFx.cs", 2),
    "Combat/FireballProjectile.cs": ("Gameplay/Combat/FireballProjectile.cs", 2),
    "Combat/HeavySlashEffect.cs": ("Gameplay/Combat/HeavySlashEffect.cs", 2),
    "Combat/MeleeCombat.cs": ("Gameplay/Combat/MeleeCombat.cs", 2),
    "Combat/Projectile.cs": ("Gameplay/Combat/Projectile.cs", 2),
    "Combat/RangedCombat.cs": ("Gameplay/Combat/RangedCombat.cs", 2),
    "Combat/SimplePool.cs": ("Gameplay/Combat/SimplePool.cs", 2),
    "Combat/SkillCastContext.cs": ("Gameplay/Combat/SkillCastContext.cs", 2),
    "Combat/SkillEffectBase.cs": ("Gameplay/Combat/SkillEffectBase.cs", 2),
    "Combat/SkillEffectFireball.cs": ("Gameplay/Combat/SkillEffectFireball.cs", 2),
    "Combat/SkillEffects.cs": ("Gameplay/Combat/SkillEffects.cs", 2),
    "Player/PlayerCommandController.cs": ("Gameplay/Player/PlayerCommandController.cs", 2),
    "Player/Selectable.cs": ("Gameplay/Player/Selectable.cs", 2),
    "Player/SelectionManager.cs": ("Gameplay/Player/SelectionManager.cs", 2),
    "Player/SkillIndicator.cs": ("Gameplay/Player/SkillIndicator.cs", 2),
    "Player/SkillInputController.cs": ("Gameplay/Player/SkillInputController.cs", 2),
    "System/Chest.cs": ("Gameplay/World/Chest.cs", 2),
    "System/DungeonDoor.cs": ("Gameplay/World/DungeonDoor.cs", 2),
    "System/FogOfWar.cs": ("Gameplay/World/FogOfWar.cs", 2),
    "System/ForestGenerator.cs": ("Gameplay/World/ForestGenerator.cs", 2),
    "System/Interactable.cs": ("Gameplay/World/Interactable.cs", 2),
    "System/SkeletonSpawner.cs": ("Gameplay/World/SkeletonSpawner.cs", 2),
    "Debug/DebugCameraSwitcher.cs": ("Gameplay/World/DebugCameraSwitcher.cs", 2),
    "Debug/Mirror.cs": ("Gameplay/World/Mirror.cs", 2),
    "Core/WaveSpawner.cs": ("Gameplay/World/WaveSpawner.cs", 2),
    "Items/ChestItem.cs": ("Gameplay/Items/ChestItem.cs", 2),
    "Items/Inventory.cs": ("Gameplay/Items/Inventory.cs", 2),
    "Core/AppFlow.cs": ("App/AppFlow.cs", 3),
    "Core/GameData.cs": ("App/GameData.cs", 3),
    "Core/GameFlow.cs": ("App/GameFlow.cs", 3),
    "Core/MetaState.cs": ("App/MetaState.cs", 3),
    "Core/PauseManager.cs": ("App/PauseManager.cs", 3),
    "Core/RunFlow.cs": ("App/RunFlow.cs", 3),
    "Debug/DebugTestTools.cs": ("Editor/DebugTestTools.cs", 4),
}

STAGE_DIRS = {
    1: {"Core": 15, "Content": 3, "Units": 16, "Combat": 14, "Items": 2, "Player": 5, "System": 6,
        "Debug": 3, "UI": 10, "Gameplay/Interaction": 3},
    2: {"Core": 13, "Content": 3, "Gameplay/Units": 17, "Gameplay/Combat": 14, "Gameplay/Player": 5,
        "Gameplay/World": 9, "Gameplay/Items": 2, "Gameplay/Interaction": 3, "Debug": 1, "UI": 10},
    3: {"Core": 7, "Content": 3, "Gameplay/Units": 17, "Gameplay/Combat": 14, "Gameplay/Player": 5,
        "Gameplay/World": 9, "Gameplay/Items": 2, "Gameplay/Interaction": 3, "App": 6, "Debug": 1, "UI": 10},
    5: {"Core": 7, "Content": 3, "Gameplay/Units": 17, "Gameplay/Combat": 14, "Gameplay/Player": 5,
        "Gameplay/World": 9, "Gameplay/Items": 2, "Gameplay/Interaction": 3, "App": 6, "UI": 10, "Editor": 1},
}
STAGE_DIRS[4] = STAGE_DIRS[3]

STAGE_NS_DIRS = {
    1: {"Core", "Content"},
    2: {"Core", "Content", "Gameplay/Units", "Gameplay/Combat", "Gameplay/Player", "Gameplay/World",
        "Gameplay/Items", "Gameplay/Interaction"},
    3: {"Core", "Content", "Gameplay/Units", "Gameplay/Combat", "Gameplay/Player", "Gameplay/World",
        "Gameplay/Items", "Gameplay/Interaction", "App"},
}
STAGE_NS_DIRS[4] = STAGE_NS_DIRS[3]
STAGE_NS_DIRS[5] = set(DIR_NS)

NS_RE = re.compile(r"^\s*namespace\s+(\S+)\s*\{\s*$", re.M)
USING_RE = re.compile(r"^\s*using\s+.*;\s*$")
RAISE_RE = re.compile(r"\b(?:internal|public)\s+static\s+void\s+RaiseChanged\(\)")
RAISE_PUBLIC_RE = re.compile(r"^\s*public\s+static\s+void\s+RaiseChanged\(\)", re.M)


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


def results():
    return []


def add(res, ok, aid, detail):
    res.append((ok, aid, detail))


def git_show(base, path):
    p = subprocess.run(["git", "show", base + ":" + path], capture_output=True)
    if p.returncode != 0:
        return None
    return p.stdout.decode("utf-8-sig", errors="replace")


def normalize_body(text):
    lines = text.replace("\r\n", "\n").replace("\r", "\n").split("\n")
    out = []
    for ln in lines:
        s = ln.strip()
        if s == "":
            continue
        if NS_RE.match(ln):
            continue
        if USING_RE.match(s):
            continue
        if s in ("{", "}"):
            continue
        s = RAISE_RE.sub("RaiseChanged()", s)
        out.append(s)
    return out


def expected_paths(stage):
    out = set()
    for old, (new, move_stage) in MOVE.items():
        out.add(new if stage >= move_stage else old)
    in_place = []
    for name, d in FINAL_DIR.items():
        final_path = d + "/" + name + ".cs"
        if final_path in {v[0] for v in MOVE.values()}:
            continue
        in_place.append(final_path)
    out |= set(in_place)
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default=BASE_DEFAULT)
    ap.add_argument("--stage", type=int, default=5, choices=[1, 2, 3, 4, 5])
    ap.add_argument("--diff-scan", action="store_true")
    args = ap.parse_args()
    res = results()

    files = all_cs(SCRIPT_ROOT)
    rels = [f.split("Scripts/", 1)[-1] for f in files]
    rels_set = set(rels)
    exp = expected_paths(args.stage)

    # ---------------- M7-1.Structure.Files
    missing = sorted(exp - rels_set)
    extra = sorted(rels_set - exp)
    counts = {}
    for r in rels:
        d = os.path.dirname(r).replace("\\", "/")
        counts[d] = counts.get(d, 0) + 1
    want_counts = STAGE_DIRS[args.stage]
    ok = len(rels) == 77 and not missing and not extra and counts == want_counts
    add(res, ok, "M7-1.Structure.Files",
        "n=%d missing=%s extra=%s counts=%s want=%s" % (len(rels), missing, extra, counts, want_counts))

    # ---------------- M7-1.Structure.Namespace
    ns_dirs = STAGE_NS_DIRS[args.stage]
    bad = []
    for f, r in zip(files, rels):
        name = os.path.basename(r)[:-3]
        final_dir = FINAL_DIR.get(name)
        expected_ns = DIR_NS[final_dir] if final_dir in ns_dirs else None
        text = read_text(f)
        found = NS_RE.findall(text)
        if expected_ns is None:
            if found:
                bad.append("%s expected-none got-%s" % (r, found))
        else:
            if len(found) != 1 or found[0] != expected_ns:
                bad.append("%s expected-%s got-%s" % (r, expected_ns, found))
    add(res, not bad, "M7-1.Structure.Namespace", "bad=%d %s" % (len(bad), bad[:8]))

    # ---------------- M7-1.Structure.Meta
    missing_meta = [r for r in rels if not os.path.exists(os.path.join(SCRIPT_ROOT, r) + ".meta")]
    orphan_meta = []
    for dp, _, fns in os.walk(SCRIPT_ROOT):
        for fn in fns:
            if fn.endswith(".cs.meta"):
                cs = os.path.join(dp, fn[:-5])
                if not os.path.exists(cs):
                    orphan_meta.append(rel(cs))
    guid_bad = []
    guid_re = re.compile(r"guid:\s*([0-9a-f]{32})")
    for old, (new, move_stage) in MOVE.items():
        if args.stage < move_stage:
            continue
        old_meta = git_show(args.base, "Assets/_Project/Scripts/" + old + ".meta")
        new_meta_path = os.path.join(SCRIPT_ROOT, new + ".meta")
        if old_meta is None or not os.path.exists(new_meta_path):
            guid_bad.append("%s missing" % old)
            continue
        a = guid_re.search(old_meta)
        b = guid_re.search(read_text(new_meta_path))
        if not a or not b or a.group(1) != b.group(1):
            guid_bad.append("%s guid %s -> %s" % (old, a.group(1) if a else None, b.group(1) if b else None))
    add(res, not missing_meta and not orphan_meta and not guid_bad, "M7-1.Structure.Meta",
        "missing_meta=%s orphan_meta=%s guid_bad=%s" % (missing_meta[:8], orphan_meta[:8], guid_bad[:8]))

    # ---------------- M7-1.Structure.Tests
    tests = all_cs(TEST_ROOT)
    ns_tests = [t for t in tests if NS_RE.search(read_text(t))]
    add(res, len(tests) == 30 and not ns_tests, "M7-1.Structure.Tests",
        "n=%d namespace_files=%s" % (len(tests), ns_tests[:8]))

    # ---------------- M7-1.Structure.Asmdef
    if args.stage >= 4:
        problems = []
        spec = {
            "Core/Dungeon.Core.asmdef": ("Dungeon.Core", []),
            "Content/Dungeon.Content.asmdef": ("Dungeon.Content", ["Dungeon.Core"]),
        }
        for relp, (name, refs) in spec.items():
            p = os.path.join(SCRIPT_ROOT, relp)
            if not os.path.exists(p):
                problems.append("missing " + relp)
                continue
            try:
                js = json.loads(read_text(p))
            except Exception as e:  # noqa: BLE001
                problems.append("bad-json %s %s" % (relp, e))
                continue
            if js.get("name") != name:
                problems.append("%s name=%s" % (relp, js.get("name")))
            if js.get("rootNamespace") != name:
                problems.append("%s rootNamespace=%s" % (relp, js.get("rootNamespace")))
            if js.get("references") != refs:
                problems.append("%s references=%s" % (relp, js.get("references")))
            if js.get("autoReferenced") is not True:
                problems.append("%s autoReferenced=%s" % (relp, js.get("autoReferenced")))
            if js.get("noEngineReferences") is not False:
                problems.append("%s noEngineReferences=%s" % (relp, js.get("noEngineReferences")))
            if js.get("includePlatforms") != []:
                problems.append("%s includePlatforms=%s" % (relp, js.get("includePlatforms")))
        add(res, not problems, "M7-1.Structure.Asmdef", "problems=%s" % problems[:8])
    else:
        add(res, True, "M7-1.Structure.Asmdef", "SKIP stage<%d" % 4)

    # ---------------- M7-1.Audit
    stats_path = os.path.join(EVIDENCE, "stats.json")
    if os.path.exists(stats_path):
        stats = json.loads(read_text(stats_path))
        up = stats.get("upward_layer_edges", [])
        lfc = stats.get("layer_file_counts", {})
        want_lfc = {"Core": 7, "Content": 3, "Gameplay": 50, "App": 6, "UI": 10, "Editor": 1}
        ok = len(up) == 11 and lfc == want_lfc and stats.get("file_count") == 77
        add(res, ok, "M7-1.Audit", "upward=%d layer_file_counts=%s file_count=%s" %
            (len(up), lfc, stats.get("file_count")))
    else:
        add(res, False, "M7-1.Audit", "missing " + stats_path)

    # ---------------- M7-1.NoLogicChange（base 对象 vs 当前工作区，按迁移映射对齐）
    problems = []
    by_name = {}
    for f, r in zip(files, rels):
        by_name[os.path.basename(r)[:-3]] = r
    for name, final_dir in FINAL_DIR.items():
        final_path = final_dir + "/" + name + ".cs"
        old_path = None
        for old, (new, _st) in MOVE.items():
            if new == final_path:
                old_path = old
                break
        if old_path is None:
            old_path = final_path
        if name not in by_name:
            problems.append("missing current " + name)
            continue
        old = git_show(args.base, "Assets/_Project/Scripts/" + old_path)
        if old is None:
            problems.append("missing base " + old_path)
            continue
        new = read_text(os.path.join(SCRIPT_ROOT, by_name[name]))
        if normalize_body(old) != normalize_body(new):
            a = normalize_body(old)
            b = normalize_body(new)
            first = next((i for i, (x, y) in enumerate(zip(a, b)) if x != y), min(len(a), len(b)))
            problems.append("%s old=%r new=%r" % (name, a[first:first + 2], b[first:first + 2]))
    add(res, not problems, "M7-1.NoLogicChange", "problems=%d %s" % (len(problems), problems[:5]))

    # ---------------- M7-1.RaiseChanged
    mf = os.path.join(SCRIPT_ROOT, "Core", "MessageFeed.cs")
    if os.path.exists(mf):
        text = read_text(mf)
        ok = bool(RAISE_PUBLIC_RE.search(text))
        add(res, ok, "M7-1.RaiseChanged", "public static void RaiseChanged found=%s" % ok)
    else:
        add(res, False, "M7-1.RaiseChanged", "missing MessageFeed.cs")

    # ---------------- M7-1.NoLogicChange.DiffScan（提交后）
    if args.diff_scan:
        p = subprocess.run(["git", "diff", "-w", "--find-renames=1%", "--unified=0",
                            args.base + "..HEAD", "--", "Assets/_Project/Scripts"],
                           capture_output=True)
        if p.returncode != 0:
            add(res, False, "M7-1.NoLogicChange.DiffScan", "git diff failed: " + p.stderr.decode("utf-8", "replace"))
        else:
            bad_lines = []
            counts = {"plus": 0, "minus": 0}
            for ln in p.stdout.decode("utf-8", "replace").split("\n"):
                if ln.startswith("+++") or ln.startswith("---") or ln.startswith("@@"):
                    continue
                if ln.startswith("+"):
                    counts["plus"] += 1
                    body = ln[1:].strip()
                elif ln.startswith("-"):
                    counts["minus"] += 1
                    body = ln[1:].strip()
                else:
                    continue
                if body == "":
                    continue
                if re.match(r"^namespace\s+\S+\s*\{\s*$", body):
                    continue
                if re.match(r"^using\s+.*;\s*$", body):
                    continue
                if body in ("{", "}"):
                    continue
                if body in ("#if UNITY_EDITOR", "#endif"):
                    continue
                if re.match(r"^(public|internal)\s+static\s+void\s+RaiseChanged\(\)\s*\{\s*Changed\?\.Invoke\(\);\s*\}$", body):
                    continue
                bad_lines.append(body)
            add(res, not bad_lines, "M7-1.NoLogicChange.DiffScan",
                "plus=%d minus=%d offenders=%d %s" % (counts["plus"], counts["minus"],
                                                     len(bad_lines), bad_lines[:10]))
    else:
        add(res, True, "M7-1.NoLogicChange.DiffScan", "SKIP (pass --diff-scan after commit)")

    fails = 0
    for ok, aid, detail in res:
        print(("PASS " if ok else "FAIL ") + aid + " " + detail)
        if not ok:
            fails += 1
    print("SUMMARY stage=%d base=%s pass=%d fail=%d" % (args.stage, args.base, len(res) - fails, fails))
    sys.exit(1 if fails else 0)


if __name__ == "__main__":
    main()
