#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""M7-FIX-01 最小 wiring 断言（只读；不依赖 Unity）。

断言：Esc 面板接线 / 宝箱 prefab 组件 / 门 prefab 结构 / 场景 fader 与门实例。
用法：py -3.12 -X utf8 Docs/QA/Evidence/M7-FIX-01/wiring_check.py
"""
import os
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", ".."))
SCENE = "Assets/Scenes/DungeonWorld.unity"
CHEST_BASE = "Assets/_Project/Prefabs/chest_base.prefab"
CHEST_GOLD = "Assets/_Project/Prefabs/chest_gold.prefab"
DOOR = "Assets/_Project/Prefabs/dungeon_door.prefab"

GUID = {
    "EscMenuPanel": "0613b2488f76e6040b81ec1865742290",
    "UIManager": "5c953194cbcb38a45ba55e7b8854e168",
    "Interactable": "2f7c06b434ceff24ab2dac5d08de108e",
    "DungeonDoor": "0c8860eaa1f7c9b49b27d0ab0edfeb9f",
    "TransitionFader": "5dce597175eba0340bc05ebe925342f8",
}
CHEST_BASE_GUID = "4e0598c14f6d79c46b4b13e24ab29bbd"

results = []


def read(path):
    with open(os.path.join(ROOT, path), "r", encoding="utf-8", errors="replace") as fh:
        return fh.read()


def check(tag, cond, detail=""):
    results.append((tag, bool(cond), detail))


def main():
    scene = read(SCENE)
    base = read(CHEST_BASE)
    gold = read(CHEST_GOLD)
    door = read(DOOR)

    # A. Esc
    check("A.UIManager.escMenuPanel.nonzero",
          "escMenuPanel: {fileID: 0}" not in scene and "escMenuPanel: {fileID:" in scene,
          "escMenuPanel 已指向非 0 引用")
    check("A.PausePanel.EscMenuPanel", GUID["EscMenuPanel"] in scene and "m_Name: PausePanel" in scene,
          "场景存在 EscMenuPanel 脚本引用")
    check("A.Buttons.3", all(("m_Name: " + n) in scene for n in ("Btn_Resume", "Btn_Restart", "Btn_Quit")),
          "继续/重开/退出 三个按钮存在")
    check("A.Buttons.persistent", scene.count("Dungeon.UI.EscMenuPanel, Dungeon.UI") >= 3,
          "按钮 onClick 持久监听 >= 3")

    # B. Chest
    check("B.chest_base.BoxCollider", "--- !u!65 " in base, "chest_base 有 BoxCollider")
    check("B.chest_base.Interactable", GUID["Interactable"] in base, "chest_base 有 Interactable")
    check("B.chest_base.hint", "hint:" in base, "hint 已序列化")
    check("B.chest_gold.inherits_base", CHEST_BASE_GUID in gold, "chest_gold 变体源 = chest_base")

    # C. Door prefab
    check("C.door.DungeonDoor", GUID["DungeonDoor"] in door, "门 prefab 有 DungeonDoor")
    check("C.door.BoxCollider", "--- !u!65 " in door, "门 prefab 有 BoxCollider")
    check("C.door.model", "m_Name: DoorModel" in door, "门 prefab 有可见模型")
    check("C.door.points", "m_Name: DungeonEntryPoint" in door and "m_Name: CameraPoint" in door,
          "门 prefab 有两个点")
    check("C.door.refs", "dungeonEntryPoint: {fileID: 0}" not in door and "cameraPoint: {fileID: 0}" not in door,
          "两个点引用非 0")

    # C. Scene fader / door
    check("C.scene.TransitionFader", GUID["TransitionFader"] in scene, "场景有 TransitionFader")
    check("C.scene.door_instance", "dungeon_door" in scene, "场景有门实例")

    width = max(len(t) for t, _, _ in results)
    ok = True
    for tag, passed, detail in results:
        print(("PASS " if passed else "FAIL ") + tag.ljust(width) + "  " + detail)
        ok = ok and passed
    print("SUMMARY pass=%d fail=%d" % (sum(1 for _, p, _ in results if p), sum(1 for _, p, _ in results if not p)))
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
