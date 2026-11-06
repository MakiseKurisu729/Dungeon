namespace Dungeon.Core {
    /// <summary>成长等级：跨场景保留的纯静态状态；默认 1，不自动重置。</summary>
    public static class ProgressionState
    {
        public static int Level { get; set; } = 1;
    }
}
