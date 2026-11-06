using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// DIAG-SKILL batchmode 唯一入口。
/// 命令行：
///   Unity.exe -batchmode -projectPath &lt;repo&gt;
///     -executeMethod DIAG_BatchAcceptance.Run
///     -logFile &lt;evidence&gt;/DIAG_probe.log
///   （可选环境变量 DIAG_MODE=probe）
/// 流程：打开 DungeonWorld（只读，不保存）→ 进入 Play → DIAG_ProbeRunner 断言 → 退出并返回退出码。
/// 不装配场景、不写任何生产资产；夹具全部在 Play 模式内存中创建并销毁。
/// </summary>
public static class DIAG_BatchAcceptance
{
    public const string ScenePath = "Assets/Scenes/DungeonWorld.unity";

    public static void Run()
    {
        Debug.Log("[DIAG] launcher start; pid=" + System.Diagnostics.Process.GetCurrentProcess().Id +
                  "; args=" + string.Join(" ", Environment.GetCommandLineArgs()));

        try
        {
            string head = QaScene.GetHead();
            SessionState.SetString("DIAG.Head", head);
            Debug.Log("[DIAG] head=" + head + "; mode=" + (Environment.GetEnvironmentVariable("DIAG_MODE") ?? "probe"));

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("[DIAG] opened scene " + scene.path + "; loaded=" + scene.isLoaded + "; dirty=" + scene.isDirty);

            SessionState.SetBool(DIAG_Boot.ActiveKey, true);
            SessionState.SetBool(DIAG_Boot.StartedKey, false);
            SessionState.SetBool(DIAG_Boot.FinishedKey, false);

            EditorApplication.EnterPlaymode();
            Debug.Log("[DIAG] EnterPlaymode requested");
        }
        catch (Exception e)
        {
            Debug.LogError("[DIAG] launcher failed: " + e);
            if (QA_InEditorMenu.TryFinish("DIAG", 3)) return;   // 编辑器内：只记失败，不退 Unity
            EditorApplication.Exit(3);                          // batch：原样
        }
    }
}

/// <summary>域重载后的接续点：进入 Play 时创建运行期探针；退出 Play 后按记录退出码结束 batchmode。</summary>
public static class DIAG_Boot
{
    public const string ActiveKey = "DIAG.Active";
    public const string StartedKey = "DIAG.Started";
    public const string FinishedKey = "DIAG.Finished";
    public const string ExitCodeKey = "DIAG.ExitCode";
    public const string RunnerNameKey = "DIAG.RunnerName";
    public const string PlayStartedAtKey = "DIAG.PlayStartedAt";

    const float RunnerLostGraceSeconds = 3f;
    const float FirstStartGraceSeconds = 10f;

    static GameObject _runner;
    static float _missingSince = -1f;

    [InitializeOnLoadMethod]
    static void Boot()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;

        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;

        Debug.Log("[DIAG] boot; isPlaying=" + Application.isPlaying +
                  "; started=" + SessionState.GetBool(StartedKey, false) +
                  "; finished=" + SessionState.GetBool(FinishedKey, false));

        _missingSince = -1f;
        if (Application.isPlaying) TryStartRunner();
    }

    static void OnPlayModeChanged(PlayModeStateChange change)
    {
        Debug.Log("[DIAG] playModeStateChanged=" + change);
        if (change == PlayModeStateChange.EnteredPlayMode)
        {
            SessionState.SetFloat(PlayStartedAtKey, Time.realtimeSinceStartup);
            if (ClearStaleRunFlags()) return;
            TryStartRunner();
        }
        else if (change == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(FinishedKey, false))
        {
            ExitWithCode();
        }
    }

    /// <summary>上一轮中断留下的 Started/Active + 已销毁 runner：新一轮进 Play 时清掉，避免看门狗误判。</summary>
    static bool ClearStaleRunFlags()
    {
        if (!SessionState.GetBool(StartedKey, false)) return false;
        if (RunnerAlive()) return false;

        Debug.LogWarning("[DIAG] stale run flags cleared (previous play session had no runner)");
        SessionState.SetBool(StartedKey, false);
        SessionState.SetBool(FinishedKey, false);
        SessionState.SetBool(ActiveKey, false);
        SessionState.EraseString(RunnerNameKey);
        _runner = null;
        _missingSince = -1f;
        return true;
    }

    static void Tick()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;

        if (SessionState.GetBool(FinishedKey, false))
        {
            if (!Application.isPlaying) ExitWithCode();
            return;
        }

        if (!Application.isPlaying) return;

        float now = Time.realtimeSinceStartup;

        if (!SessionState.GetBool(StartedKey, false))
        {
            float playStartedAt = SessionState.GetFloat(PlayStartedAtKey, now);
            if (now - playStartedAt >= FirstStartGraceSeconds)
            {
                WatchdogFail("runner never created within " + FirstStartGraceSeconds + "s after entering play");
                return;
            }
            Debug.Log("[DIAG] tick fallback: isPlaying=true, starting runner");
            TryStartRunner();
            return;
        }

        if (RunnerAlive())
        {
            _missingSince = -1f;
            return;
        }

        if (_missingSince < 0f)
        {
            _missingSince = now;
            Debug.LogWarning("[DIAG][WATCHDOG] runner missing; counting down " + RunnerLostGraceSeconds + "s");
        }
        else if (now - _missingSince >= RunnerLostGraceSeconds)
        {
            WatchdogFail("runner missing >= " + RunnerLostGraceSeconds + "s");
        }
    }

    static bool RunnerAlive()
    {
        if (_runner != null) return true;
        string name = SessionState.GetString(RunnerNameKey, string.Empty);
        if (string.IsNullOrEmpty(name)) return true;
        return GameObject.Find(name) != null;
    }

    static void WatchdogFail(string why)
    {
        Debug.LogError("[DIAG][WATCHDOG] " + why + " -> code=3 reason=runner-lost-domain-reload");
        if (QA_InEditorMenu.TryFinish("DIAG", 3, "runner-lost-domain-reload"))
        {
            EditorApplication.isPlaying = false;
            Debug.Log("[DIAG][WATCHDOG] left play mode; editor kept alive");
            return;
        }
        EditorApplication.Exit(3);
    }

    static void ExitWithCode()
    {
        int code = SessionState.GetInt(ExitCodeKey, 3);
        SessionState.SetBool(FinishedKey, false);
        SessionState.SetBool(StartedKey, false);
        SessionState.SetBool(ActiveKey, false);
        SessionState.EraseString(RunnerNameKey);
        _runner = null;
        _missingSince = -1f;
        Debug.Log("[DIAG] batchmode exit " + code);
        if (QA_InEditorMenu.TryFinish("DIAG", code)) return;
        EditorApplication.Exit(code);
    }

    static void TryStartRunner()
    {
        if (SessionState.GetBool(StartedKey, false)) return;
        SessionState.SetBool(StartedKey, true);
        SessionState.SetFloat(PlayStartedAtKey, Time.realtimeSinceStartup);

        var go = new GameObject("DIAG_Runner");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<DIAG_ProbeRunner>();
        _runner = go;
        SessionState.SetString(RunnerNameKey, go.name);
        _missingSince = -1f;
        Debug.Log("[DIAG] play-mode runner created");
    }
}
