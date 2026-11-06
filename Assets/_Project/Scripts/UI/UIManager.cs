using System.Collections.Generic;
using UnityEngine;
using Dungeon.Gameplay.Items;
using Dungeon.Gameplay.Player;
using Dungeon.Gameplay.World;
using Dungeon.App;
using Dungeon.Gameplay;
namespace Dungeon.UI {

    /// <summary>
    /// UI 咽喉：所有面板开关只走这里。场景里面板物体保持激活，初始隐藏由本类 Awake 统一执行。
    /// Tab=背包 I=属性 C=装备（窗口组进栈，Esc 关栈顶）；Esc 栈空=Esc菜单（接管暂停）。
    /// 开始界面与 Esc 菜单为独占面板（不进栈）；未开始游戏/团灭时 Esc 菜单不响应。
    /// </summary>
    public class UIManager : MonoBehaviour, IUiModalState
    {
        public static UIManager instance;

        [Header("面板")]
        [InspectorName("背包面板")]
        public InventoryUI inventoryPanel;
        [InspectorName("属性面板")]
        public BasePanel statsPanel;      // 第二批建，先空着
        [InspectorName("装备面板")]
        public BasePanel equipPanel;      // 第三批建，先空着
        [InspectorName("Esc菜单面板")]
        public EscMenuPanel escMenuPanel;
        [InspectorName("主菜单面板")]
        public MainMenuPanel mainMenuPanel;

        int _serial;
        readonly List<BasePanel> _stack = new List<BasePanel>();

        public bool IsInventoryOpen { get { return inventoryPanel != null && inventoryPanel.IsOpen; } }
        public bool IsAnyPanelOpen { get { return _stack.Count > 0; } }

        /// <summary>IUiModalState：严格等于 IsInventoryOpen（不扩大为“任意面板打开”）。</summary>
        public bool IsModalOpen { get { return IsInventoryOpen; } }

        void Awake()
        {
            instance = this;
            UiModalState.Current = this;
            // ★ 初始隐藏统一接管：面板物体在场景里保持激活，隐藏权 100% 归这里
            if (inventoryPanel != null) inventoryPanel.gameObject.SetActive(false);
            if (escMenuPanel != null) escMenuPanel.gameObject.SetActive(false);
            Chest.Opened += OnChestOpened;
        }

        void OnDestroy()
        {
            if (ReferenceEquals(UiModalState.Current, this)) UiModalState.Current = null;
            Chest.Opened -= OnChestOpened;
        }

        /// <summary>宝箱开箱完成 → 打开背包面板（宝箱不再直接调 UI）。</summary>
        void OnChestOpened(Chest chest)
        {
            if (chest == null) return;
            var contents = chest.Contents;
            if (contents == null) return;      // 与原实现一致：没有 Inventory 的宝箱不开面板
            ShowInventory(contents, chest);
        }

        void Start()
        {
            if (mainMenuPanel != null) OpenPanel(mainMenuPanel);
        }

        void Update()
        {
            bool gameOn = AppFlow.GameStarted && !RunFlow.IsWiped;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_stack.Count > 0)
                {
                    ClosePanel(_stack[_stack.Count - 1]);
                    return;
                }
                if (gameOn && escMenuPanel != null)
                {
                    if (escMenuPanel.IsOpen) ClosePanel(escMenuPanel);
                    else OpenPanel(escMenuPanel);
                }
                return;
            }

            if (!gameOn) return;

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (IsInventoryOpen) ClosePanel(inventoryPanel);
                else ShowInventory(null, null);
            }
            if (Input.GetKeyDown(KeyCode.I)) Toggle(statsPanel);
            if (Input.GetKeyDown(KeyCode.C)) Toggle(equipPanel);
        }

        void Toggle(BasePanel p)
        {
            if (p == null) return;
            if (p.IsOpen) ClosePanel(p);
            else OpenPanel(p);
        }

        public void OpenPanel(BasePanel panel, object userData = null)
        {
            if (panel == null) { Debug.LogError("[UIManager] 面板为空！"); return; }
            if (panel.IsOpen) return;
            _serial++;
            panel.Open(_serial, userData);
            if (!(panel is EscMenuPanel) && !(panel is MainMenuPanel))
                _stack.Add(panel);
            Debug.Log($"[UIManager] 打开 {panel.name}（序号 {_serial}）");
        }

        public void ClosePanel(BasePanel panel)
        {
            if (panel == null || !panel.IsOpen) return;
            _stack.Remove(panel);
            panel.Close();
            Debug.Log($"[UIManager] 关闭 {panel.name}");
        }

        // ===== 旧接口兼容（Chest 等调用方零改动）=====
        public void ShowInventory(Inventory chestInv, Chest chest)
        {
            if (inventoryPanel == null) { Debug.LogError("[UIManager] inventoryPanel 槽为空"); return; }
            if (chestInv == null && SelectionManager.instance != null && SelectionManager.instance.Primary != null)
                inventoryPanel.BindPlayer(SelectionManager.instance.Primary.GetComponentInParent<Inventory>());
            inventoryPanel.SetChestData(chestInv, chest);
            OpenPanel(inventoryPanel);
        }

        public void HideInventory()
        {
            ClosePanel(inventoryPanel);
        }
    }
}