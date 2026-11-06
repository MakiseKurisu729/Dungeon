namespace Dungeon.Core {
    /// <summary>提示消息：单条文案 + 变更事件，HUD 轮询显示。
    /// 生命周期 = 进程内（跨场景重载保留），与重构前 GameData 的 message 字段一致。</summary>
    public static class MessageFeed
    {
        /// <summary>当前文案。默认 ""。</summary>
        public static string Message { get; set; } = "";

        /// <summary>文案变更（原 GameData 的 OnChanged 事件）。</summary>
        public static event System.Action Changed;

        /// <summary>赋值并触发 Changed；即使文案相同也触发（保持原 SetMessage 语义）。</summary>
        public static void SetMessage(string m) { Message = m; RaiseChanged(); }

        /// <summary>仅触发变更事件，不改文案：给 GameData 适配层的 AddGold/TryLevelUp 用（原实现直接 Invoke）。</summary>
        public static void RaiseChanged() { Changed?.Invoke(); }
    }
}
