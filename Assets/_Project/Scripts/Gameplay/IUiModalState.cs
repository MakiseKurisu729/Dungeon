namespace Dungeon.Gameplay {
    /// <summary>UI 模态契约：Gameplay 只读接口判断“背包是否打开”，不依赖 UI 层。</summary>
    public interface IUiModalState
    {
        bool IsModalOpen { get; }
    }

    /// <summary>当前注册的 UI 模态实现；UIManager 在 Awake 注册、OnDestroy 注销。</summary>
    public static class UiModalState
    {
        public static IUiModalState Current { get; set; }
    }
}
