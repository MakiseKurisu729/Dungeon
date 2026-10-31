using UnityEngine;

/// <summary>
/// UI 管理器（唯一咽喉）：所有面板的显示/隐藏只允许经过这里。
/// 每次切换打印调用栈——如果面板被"幽灵"点亮，日志会指认凶手。
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    [Tooltip("背包/箱子面板的总根")]
    public GameObject inventoryRoot;

    public bool IsInventoryOpen
    {
        get { return inventoryRoot != null && inventoryRoot.activeSelf; }
    }

    void Awake()
    {
        instance = this;
        SetVisible(inventoryRoot, false, "Awake 初始隐藏");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && IsInventoryOpen)
            HideInventory();

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (IsInventoryOpen) HideInventory();
            else ShowInventory(null, null);   // Tab = 只开背包
        }
    }

    /// <summary>打开背包/箱子面板。chestInv 为空 = 仅背包模式。</summary>
    public void ShowInventory(Inventory chestInv, Chest chest)
    {
        if (InventoryUI.instance != null)
            InventoryUI.instance.SetData(chestInv, chest);
        SetVisible(inventoryRoot, true, "ShowInventory");
    }

    public void HideInventory()
    {
        SetVisible(inventoryRoot, false, "HideInventory");
        if (InventoryUI.instance != null)
            InventoryUI.instance.SetData(null, null);
    }

    // 唯一的 SetActive 咽喉
    void SetVisible(GameObject go, bool visible, string reason)
    {
        if (go == null) { Debug.LogError("[UIManager] 目标面板为空！检查 inventoryRoot 槽"); return; }
        if (go.activeSelf == visible) return;
        go.SetActive(visible);
        Debug.Log($"[UIManager] {go.name} → {(visible ? "显示" : "隐藏")}（{reason}）\n{System.Environment.StackTrace}");
    }
}