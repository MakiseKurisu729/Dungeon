using UnityEngine;

/// <summary>可选中单位：注册选择系统，管理选中圈；hotkeyIndex=1~4 数字键选人。</summary>
public class Selectable : MonoBehaviour
{
    [InspectorName("选中圈")]
    public GameObject selectionRing;
    [InspectorName("热键序号")]
    [Tooltip("1~4 数字键快速选中（0=不参与）")]
    public int hotkeyIndex = 0;

    void OnEnable()
    {
        // ★ 初始/重激活时隐藏选中圈：选中态由 SelectionManager 点亮，默认不亮
        SetSelected(false);
        if (SelectionManager.instance != null)
            SelectionManager.instance.Register(this);
    }

    public void SetSelected(bool on)
    {
        if (selectionRing != null) selectionRing.SetActive(on);
    }
}