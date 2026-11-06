namespace Dungeon.Core {
    /// <summary>暂停标志：只由 AppFlow.Pause/Resume/ReloadActiveScene 写入；本类不碰 Time.timeScale。
    /// 团灭时 RunFlow 只写 timeScale=0，不改变本标志（团灭 ≠ Esc 暂停）。</summary>
    public static class PauseState
    {
        public static bool IsPaused { get; private set; }

        public static void Set(bool paused) { IsPaused = paused; }
    }
}
