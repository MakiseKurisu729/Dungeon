using UnityEngine;
namespace Dungeon.Content {

    [CreateAssetMenu(menuName = "Dungeon/Item Data")]
    public class ItemData : ScriptableObject
    {
        [InspectorName("物品名称")]
        public string itemName = "物品";
        [InspectorName("图标")]
        public Sprite icon;          // UI 图标（拖切片或任意图）
        [InspectorName("金币价值")]
        public int goldValue;        // 拾取时直接给的金币（金币堆用，普通物品 0）
    }
}