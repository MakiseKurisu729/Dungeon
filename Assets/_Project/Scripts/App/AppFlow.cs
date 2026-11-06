using UnityEngine;
using UnityEngine.SceneManagement;
using Dungeon.Core;
using Dungeon.Gameplay.Units;
namespace Dungeon.App {

    /// <summary>应用级流程：开局标志 / 暂停 / 重开 / 队伍存在性。timeScale 的唯一写入口。
    /// 原 GameFlow.Start 的场景判定与 PauseManager 的 Pause/Resume 在此收编；GameFlow/PauseManager 只剩转发。</summary>
    public static class AppFlow
    {
        /// <summary>是否已开局（原 GameFlow 的 GameStarted 静态字段）。</summary>
        public static bool GameStarted { get; set; }

        /// <summary>是否因 Esc 菜单而暂停（原 PauseManager.IsPaused）。</summary>
        public static bool IsPaused { get; private set; }

        /// <summary>暂停：timeScale = 0（原 PauseManager.Pause）。</summary>
        public static void Pause()
        {
            IsPaused = true;
            Time.timeScale = 0f;
        }

        /// <summary>恢复：timeScale = 1（原 PauseManager.Resume）。</summary>
        public static void Resume()
        {
            IsPaused = false;
            Time.timeScale = 1f;
        }

        /// <summary>重开：清开局标志、复位暂停标志与时间、按 buildIndex 重载当前场景（原 R 重开三行）。
        /// 静态化后 IsPaused 不会随旧实例消失，因此这里必须显式复位，等价于“新场景里 IsPaused=false”。</summary>
        public static void ReloadActiveScene()
        {
            GameStarted = false;
            IsPaused = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>场景里是否存在玩家队伍（原 GameFlow.HasPlayerTeam）。</summary>
        public static bool AnyPlayerTeam()
        {
            return UnitRegistry.OfTeam(Team.Player).Count > 0;
        }
    }
}
