namespace Dungeon.Gameplay.World {
    /// <summary>门过场请求：DungeonDoor 只发事件；UI 层 TransitionFader 订阅后播黑屏 → 执行 midAction → 亮屏。</summary>
    public struct TransitionRequest
    {
        public System.Action midAction;
    }
}
