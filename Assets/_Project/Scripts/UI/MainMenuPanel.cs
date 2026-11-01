using UnityEngine;

/// <summary>
/// 开始界面（门户面板）：打开时 timeScale=0 冻结全场；点"进入地牢"→ 解冻 + GameStarted=true。
/// </summary>
public class MainMenuPanel : BasePanel
{
    protected override void OnOpen(object userData)
    {
        Time.timeScale = 0f;
    }

    protected override void OnClose()
    {
        Time.timeScale = 1f;
        GameFlow.GameStarted = true;
    }

    // 按钮 OnClick 挂这个
    public void OnClickStart()
    {
        if (UIManager.instance != null) UIManager.instance.ClosePanel(this);
    }
}