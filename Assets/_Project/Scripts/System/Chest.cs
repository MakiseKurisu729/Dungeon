using System.Collections;
using UnityEngine;

/// <summary>
/// 通用宝箱：由角色交互指令触发（走过去→Interact 动作→开盖+内容物现身+双面板）。
/// </summary>
public class Chest : MonoBehaviour
{
    [InspectorName("盖子")]
    public Transform lid;
    [InspectorName("开启角度")]
    public float openAngle = -110f;
    [InspectorName("开启时长")]
    public float openDuration = 0.4f;

    bool _opened;
    Inventory _inv;

    void Awake()
    {
        _inv = GetComponent<Inventory>();
        foreach (var ci in GetComponentsInChildren<ChestItem>(true))
            ci.gameObject.SetActive(false);
    }

    void Start()
    {
        if (_inv != null)
        {
            _inv.items = new System.Collections.Generic.List<ItemData>(new ItemData[_inv.capacity]);
            foreach (var ci in GetComponentsInChildren<ChestItem>(true))
                if (ci.data != null) _inv.Add(ci.data);
        }
    }

    /// <summary>角色走到交互范围后调用。</summary>
    public void OpenBy(CharacterBrain unit)
    {
        if (_opened) return;
        _opened = true;
        StartCoroutine(OpenSequence());
    }

    IEnumerator OpenSequence()
    {
        if (lid != null)
        {
            Quaternion from = lid.localRotation;
            Quaternion to = from * Quaternion.Euler(openAngle, 0f, 0f);
            float t = 0f;
            while (t < openDuration)
            {
                t += Time.deltaTime;
                lid.localRotation = Quaternion.Slerp(from, to, Mathf.Clamp01(t / openDuration));
                yield return null;
            }
            lid.localRotation = to;
        }

        foreach (var ci in GetComponentsInChildren<ChestItem>(true))
            ci.gameObject.SetActive(true);

        if (UIManager.instance != null && _inv != null)
            UIManager.instance.ShowInventory(_inv, this);
    }

    public void Take(ItemData item)
    {
        foreach (var ci in GetComponentsInChildren<ChestItem>(false))
        {
            if (ci.data == item) { Destroy(ci.gameObject); break; }
        }
        if (item.goldValue > 0) RunState.AddGold(item.goldValue);
        if (_inv != null && _inv.items.Count == 0) MessageFeed.SetMessage("宝箱已取空");
    }
}