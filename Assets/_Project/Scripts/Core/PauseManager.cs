using UnityEngine;

/// <summary>
/// 时间看门人：只管 timeScale 与 IsPaused。
/// Esc 的监听已移交 UIManager（Esc菜单开关时调用 Pause/Resume）。
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager instance;

    public bool IsPaused { get; private set; }

    void Awake()
    {
        instance = this;
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
    }
}