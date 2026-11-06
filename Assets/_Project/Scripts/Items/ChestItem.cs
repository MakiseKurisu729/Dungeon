using UnityEngine;
using Dungeon.Content;

/// <summary>宝箱内容物：挂在展示模型上，声明它是什么物品。</summary>
public class ChestItem : MonoBehaviour
{
    [InspectorName("物品数据")]
    public ItemData data;
}