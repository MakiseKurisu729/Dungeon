using UnityEngine;

[CreateAssetMenu(menuName = "Dungeon/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemName = "物品";
    public Sprite icon;          // UI 图标（拖切片或任意图）
    public int goldValue;        // 拾取时直接给的金币（金币堆用，普通物品 0）
}