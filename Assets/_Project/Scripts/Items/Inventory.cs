using System.Collections.Generic;
using UnityEngine;

/// <summary>通用背包（按槽位存储）：items 长度恒等于 capacity，空槽为 null。</summary>
public class Inventory : MonoBehaviour
{
    [InspectorName("容量")]
    public int capacity = 12;
    [InspectorName("物品列表")]
    public List<ItemData> items;
    public event System.Action OnChanged;

    void Awake()
    {
        items = new List<ItemData>(new ItemData[capacity]);
    }

    /// <summary>放入第一个空槽，成功返回槽位索引，满了返回 -1。</summary>
    public int Add(ItemData item)
    {
        int i = items.IndexOf(null);
        if (i < 0) return -1;
        items[i] = item;
        OnChanged?.Invoke();
        return i;
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= items.Count) return;
        items[index] = null;
        OnChanged?.Invoke();
    }
}