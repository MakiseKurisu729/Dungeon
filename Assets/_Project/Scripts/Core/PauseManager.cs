using UnityEngine;

/// <summary>
/// 全局暂停：Esc 切换（背包开着时 Esc 优先关背包，不抢）。timeScale=0 + 暂停面板。
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager instance;

    public GameObject pausePanel;
    public bool IsPaused { get; private set; }

    void Awake()
    {
        instance = this;
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        var ui = UIManager.instance;
        if (ui != null && ui.IsInventoryOpen) return;   // Esc 先关背包

        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
    }
}