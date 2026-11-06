using UnityEngine;
namespace Dungeon.App {

    /// <summary>
    /// 【薄的适配层】时间看门人：保留 PauseManager.instance / IsPaused / Pause / Resume，
    /// 实际 timeScale 与暂停标志由 AppFlow 持有。
    /// Esc 的监听在 UIManager（Esc菜单开关时经本类转发 Pause/Resume）。
    /// </summary>
    public class PauseManager : MonoBehaviour
    {
        public static PauseManager instance;

        public bool IsPaused { get { return AppFlow.IsPaused; } }

        void Awake()
        {
            instance = this;
        }

        public void Pause()
        {
            AppFlow.Pause();
        }

        public void Resume()
        {
            AppFlow.Resume();
        }
    }
}
