using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 背包/箱子双面板视图（纯视图：不管显隐，UIManager 负责开关）。
/// Player Inv 留空时自动找第一个玩家单位（按 Team，不靠物体名）。
/// </summary>
public class InventoryUI : MonoBehaviour
{
    public static InventoryUI instance;

    public Inventory playerInv;
    public Transform chestGrid;
    public Transform playerGrid;
    public Sprite slotBg;

    Inventory _chestInv;
    Chest _chest;
    readonly List<GameObject> _slots = new List<GameObject>();

    void Awake()
    {
        instance = this;
        if (playerInv == null)
        {
            foreach (var uh in FindObjectsByType<UnitHealth>(FindObjectsSortMode.None))
            {
                if (uh.team != Team.Player) continue;
                playerInv = uh.GetComponent<Inventory>();
                if (playerInv != null) break;
            }
        }
    }

    /// <summary>由 UIManager 调用：设置当前数据并刷新。</summary>
    public void SetData(Inventory chestInv, Chest chest)
    {
        _chestInv = chestInv;
        _chest = chest;
        if (chestGrid != null && chestGrid.parent != null)
            chestGrid.parent.gameObject.SetActive(chestInv != null);
        Refresh();
    }

    void Refresh()
    {
        foreach (var s in _slots) Destroy(s);
        _slots.Clear();
        if (_chestInv != null) BuildGrid(chestGrid, _chestInv, true);
        if (playerInv != null) BuildGrid(playerGrid, playerInv, false);
    }

    void BuildGrid(Transform grid, Inventory inv, bool isChest)
    {
        if (grid == null) { Debug.LogError("[InvUI] Grid 槽为空"); return; }
        for (int i = 0; i < inv.capacity; i++)
        {
            var item = i < inv.items.Count ? inv.items[i] : null;

            var slot = new GameObject("slot", typeof(Image));
            slot.transform.SetParent(grid, false);
            var img = slot.GetComponent<Image>();
            img.sprite = slotBg;
            img.raycastTarget = true;

            if (item != null && item.icon != null)
            {
                var icon = new GameObject("icon", typeof(Image));
                icon.transform.SetParent(slot.transform, false);
                var iimg = icon.GetComponent<Image>();
                iimg.sprite = item.icon;
                iimg.raycastTarget = false;
                var rt = icon.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector3.one;
                rt.offsetMin = new Vector2(5, 5); rt.offsetMax = new Vector2(-5, -5);
            }

            var clicker = slot.AddComponent<SlotClickHandler>();
            int index = i;
            bool chestSide = isChest;
            clicker.onClick = (e) => OnSlotClick(index, chestSide, e.button);
            _slots.Add(slot);
        }
    }

    void OnSlotClick(int index, bool isChest, PointerEventData.InputButton button)
    {
        if (isChest)
        {
            if (_chestInv == null || playerInv == null) return;
            if (button != PointerEventData.InputButton.Left) return;
            var item = _chestInv.items[index];
            if (item == null) return;
            if (playerInv.Add(item) >= 0)
            {
                _chestInv.RemoveAt(index);
                if (_chest != null) _chest.Take(item);
                Refresh();
            }
            else GameData.SetMessage("背包已满！");
        }
        else
        {
            if (playerInv == null) return;
            if (button != PointerEventData.InputButton.Right) return;
            var item = playerInv.items[index];
            if (item == null || item.goldValue <= 0) return;
            playerInv.RemoveAt(index);
            GameData.AddGold(item.goldValue);
            GameData.SetMessage($"使用{item.itemName}，+{item.goldValue} 金币");
            Refresh();
        }
    }
}