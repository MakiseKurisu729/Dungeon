using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Esc 菜单（独占面板）：打开=全局暂停，关闭=恢复。
/// 继续游戏 / 重新开始（回主菜单） / 退出到主菜单。
/// </summary>
public class EscMenuPanel : BasePanel
{
    protected override void OnOpen(object userData)
    {
        if (PauseManager.instance != null) PauseManager.instance.Pause();
    }

    protected override void OnClose()
    {
        if (PauseManager.instance != null) PauseManager.instance.Resume();
    }

    // 按钮 OnClick 挂这些
    public void OnClickResume()
    {
        if (UIManager.instance != null) UIManager.instance.ClosePanel(this);
    }

    public void OnClickRestart()
    {
        GameFlow.GameStarted = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnClickQuitToMenu()
    {
        OnClickRestart();
    }
}