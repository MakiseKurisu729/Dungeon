using UnityEngine;
using UnityEngine.SceneManagement;
using Dungeon.App;
namespace Dungeon.UI {

    /// <summary>
    /// Esc 菜单（独占面板）：打开=全局暂停，关闭=恢复。
    /// 继续游戏 / 重新开始（回主菜单） / 退出到主菜单。
    /// </summary>
    public class EscMenuPanel : BasePanel
    {
        protected override void OnOpen(object userData)
        {
            AppFlow.Pause();
        }

        protected override void OnClose()
        {
            AppFlow.Resume();
        }

        // 按钮 OnClick 挂这些
        public void OnClickResume()
        {
            if (UIManager.instance != null) UIManager.instance.ClosePanel(this);
        }

        public void OnClickRestart()
        {
            // 原三行（GameStarted=false / timeScale=1 / LoadScene）收编到 AppFlow，
            // 并额外复位静态的 IsPaused（场景重载后旧 PauseManager 实例消失，等价于新场景 IsPaused=false）。
            AppFlow.ReloadActiveScene();
        }

        public void OnClickQuitToMenu()
        {
            OnClickRestart();
        }
    }
}