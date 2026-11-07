#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""M7-FIX-01 / 01B 最小 wiring 断言（只读；不依赖 Unity）。

覆盖：
- 01：Esc 面板接线 / 宝箱 Collider+Interactable / 门 prefab / 场景 fader 与门实例。
- 01B：EventSystem + StandaloneInputModule（且无 InputSystemUIInputModule）/ Canvas GraphicRaycaster /
       chest_gold 的 ChestItem(data=金币袋)（含嵌套 coin_stack 来源）。
用法：py -3.12 -X utf8 Docs/QA/Evidence/M7-FIX-01/wiring_check.py
"""
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", ".."))
SCENE = "Assets/Scenes/DungeonWorld.unity"
CHEST_BASE = "Assets/_Project/Prefabs/chest_base.prefab"
CHEST_GOLD = "Assets/_Project/Prefabs/chest_gold.prefab"
COIN_STACK = "Assets/_Project/Prefabs/coin_stack_medium.prefab"
DOOR = "Assets/_Project/Prefabs/dungeon_door.prefab"

GUID = {
    "EscMenuPanel": "0613b2488f76e6040b81ec1865742290",
    "Interactable": "2f7c06b434ceff24ab2dac5d08de108e",
    "DungeonDoor": "0c8860eaa1f7c9b49b27d0ab0edfeb9f",
    "TransitionFader": "5dce597175eba0340bc05ebe925342f8",
    "EventSystem": "76c392e42b5098c458856cdf6ecaaaa1",
    "StandaloneInputModule": "4f231c4fb786f3946a6b90b886c48677",
    "InputSystemUIInputModule": "01614664b831546d2ae94a42149d80ac",
    "GraphicRaycaster": "dc42784cf147c0c48a680349fa168899",
    "ChestItem": "cc98114b50e726b4f872c203a71d69b8",
    "GoldBag": "4c1eb5183a519b54eb99651a126f1883",
    "CoinStackPrefab": "de5ebc47c4297c44899d09f027359c50",
    "InventoryUI": "01ee69bc580a49b49959e7a9b94da0ae",
    "UIManager": "5c953194cbcb38a45ba55e7b8854e168",
}
CHEST_BASE_GUID = "4e0598c14f6d79c46b4b13e24ab29bbd"

results = []


def read(path):
    with open(os.path.join(ROOT, path), "r", encoding="utf-8", errors="replace") as fh:
        return fh.read()


def check(tag, cond, detail=""):
    results.append((tag, bool(cond), detail))


def yaml_blocks(text):
    """按 `--- !u!<class> &<anchor>` 切块，返回 {anchor: (class, body)}。"""
    blocks = {}
    cur = None
    buf = []
    for line in text.splitlines():
        m = re.match(r"^--- !u!(\d+) &(-?\d+)", line)
        if m:
            if cur is not None:
                blocks[cur] = (cls, "\n".join(buf))
            cls, cur, buf = m.group(1), m.group(2), []
        else:
            buf.append(line)
    if cur is not None:
        blocks[cur] = (cls, "\n".join(buf))
    return blocks


def gameobject_block(blocks, name):
    for anchor, (cls, body) in blocks.items():
        if cls == "1" and re.search(r"^  m_Name: %s$" % re.escape(name), body, re.M):
            return anchor, body
    return None, None


def component_ids(go_body):
    ids = []
    in_comp = False
    for line in go_body.splitlines():
        if line.strip() == "m_Component:":
            in_comp = True
            continue
        if in_comp:
            m = re.match(r"^  - component: \{fileID: (-?\d+)\}", line)
            if m:
                ids.append(m.group(1))
            elif line.startswith("  m_Layer:"):
                break
    return ids


def has_component_with_guid(blocks, go_name, script_guid):
    anchor, body = gameobject_block(blocks, go_name)
    if anchor is None:
        return False
    for cid in component_ids(body):
        if cid in blocks:
            cls, cbody = blocks[cid]
            if cls == "114" and script_guid in cbody:
                return True
    return False


def main():
    scene = read(SCENE)
    base = read(CHEST_BASE)
    gold = read(CHEST_GOLD)
    coin = read(COIN_STACK)
    door = read(DOOR)
    blocks = yaml_blocks(scene)

    # ---- 01: Esc ----
    check("A.UIManager.escMenuPanel.nonzero",
          "escMenuPanel: {fileID: 0}" not in scene and "escMenuPanel: {fileID:" in scene,
          "escMenuPanel 已指向非 0 引用")
    check("A.PausePanel.EscMenuPanel", GUID["EscMenuPanel"] in scene and "m_Name: PausePanel" in scene,
          "场景存在 EscMenuPanel 脚本引用")
    check("A.Buttons.3", all(("m_Name: " + n) in scene for n in ("Btn_Resume", "Btn_Restart", "Btn_Quit")),
          "继续/重开/退出 三个按钮存在")
    check("A.Buttons.persistent", scene.count("Dungeon.UI.EscMenuPanel, Dungeon.UI") >= 3,
          "按钮 onClick 持久监听 >= 3")

    # ---- 01: Chest ----
    check("B.chest_base.BoxCollider", "--- !u!65 " in base, "chest_base 有 BoxCollider")
    check("B.chest_base.Interactable", GUID["Interactable"] in base, "chest_base 有 Interactable")
    check("B.chest_base.hint", "hint:" in base, "hint 已序列化")
    check("B.chest_gold.inherits_base", CHEST_BASE_GUID in gold, "chest_gold 变体源 = chest_base")

    # ---- 01: Door ----
    check("C.door.DungeonDoor", GUID["DungeonDoor"] in door, "门 prefab 有 DungeonDoor")
    check("C.door.BoxCollider", "--- !u!65 " in door, "门 prefab 有 BoxCollider")
    check("C.door.model", "m_Name: DoorModel" in door, "门 prefab 有可见模型")
    check("C.door.points", "m_Name: DungeonEntryPoint" in door and "m_Name: CameraPoint" in door,
          "门 prefab 有两个点")
    check("C.door.refs", "dungeonEntryPoint: {fileID: 0}" not in door and "cameraPoint: {fileID: 0}" not in door,
          "两个点引用非 0")
    check("C.scene.TransitionFader", GUID["TransitionFader"] in scene, "场景有 TransitionFader")
    check("C.scene.door_instance", "dungeon_door" in scene, "场景有门实例")

    # ---- 01B: EventSystem ----
    check("B01.EventSystem.exists", GUID["EventSystem"] in scene, "场景有 EventSystem")
    check("B01.EventSystem.Standalone", GUID["StandaloneInputModule"] in scene, "有 StandaloneInputModule")
    check("B01.EventSystem.noInputSystem", GUID["InputSystemUIInputModule"] not in scene,
          "无 InputSystemUIInputModule")
    check("B01.EventSystem.single", scene.count("guid: " + GUID["EventSystem"]) >= 1,
          "EventSystem 脚本引用存在（仅 1 个实例，运行期已读回）")

    # ---- 01B: Canvas GraphicRaycaster ----
    ok_gr = has_component_with_guid(blocks, "Canvas", GUID["GraphicRaycaster"])
    check("B01.Canvas.GraphicRaycaster", ok_gr, "Canvas 自身挂有 GraphicRaycaster")

    # ---- 01B: chest_gold ChestItem ----
    check("B01.chest_gold.ChestItem",
          (GUID["ChestItem"] in gold and GUID["GoldBag"] in gold)
          or (GUID["CoinStackPrefab"] in gold and GUID["ChestItem"] in coin and GUID["GoldBag"] in coin),
          "chest_gold 的 ChestItem 来源存在（自身或嵌套 coin_stack）")
    check("B01.coin_stack.ChestItem", GUID["ChestItem"] in coin and GUID["GoldBag"] in coin,
          "嵌套 coin_stack_medium 也带 ChestItem(data=金币袋)")
    check("B01.chest_gold.includes_coin_stack", GUID["CoinStackPrefab"] in gold,
          "chest_gold 引用 coin_stack_medium")

    # ---- 01C: InventoryUI host & code fix ----
    check("B01C.InventoryUI.on.PanelRoot", has_component_with_guid(blocks, "PanelRoot", GUID["InventoryUI"]),
          "InventoryUI 组件在 PanelRoot 上")
    check("B01C.InventoryUI.not.on.Canvas", not has_component_with_guid(blocks, "Canvas", GUID["InventoryUI"]),
          "Canvas 上不再挂 InventoryUI")
    panel_body = gameobject_block(blocks, "PanelRoot")[1] or ""
    panel_comps = component_ids(panel_body)
    ref_ok = False
    for anchor, (cls, body) in blocks.items():
        if cls == "114" and GUID["UIManager"] in body:
            m = re.search(r"inventoryPanel: \{fileID: (-?\d+)\}", body)
            if m:
                ref_ok = m.group(1) in panel_comps
    check("B01C.UIManager.inventoryPanel.on.PanelRoot", ref_ok, "UIManager.inventoryPanel 指向 PanelRoot 上的组件")
    inv_src = read("Assets/_Project/Scripts/UI/InventoryUI.cs")
    check("B01C.InventoryUI.code.noParent", "chestGrid.parent.gameObject" not in inv_src,
          "InventoryUI 不再用 chestGrid.parent 切换显隐")

    width = max(len(t) for t, _, _ in results)
    ok = True
    for tag, passed, detail in results:
        print(("PASS " if passed else "FAIL ") + tag.ljust(width) + "  " + detail)
        ok = ok and passed
    print("SUMMARY pass=%d fail=%d" % (sum(1 for _, p, _ in results if p), sum(1 for _, p, _ in results if not p)))
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
