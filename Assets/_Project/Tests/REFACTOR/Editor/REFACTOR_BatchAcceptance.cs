using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// REFACTOR 护栏套件的编辑器入口（batch 与编辑器内菜单共用同一套装配）。
/// 命令行：
///   Unity.exe -batchmode -projectPath &lt;repo&gt;
///     -executeMethod REFACTOR_BatchAcceptance.Run
///     -logFile &lt;evidence&gt;/REFACTOR_Automation_runN.log
///   环境变量 REFACTOR_RUN=1|2|3；REFACTOR_MILESTONE=M0..M4（缺省用 DefaultMilestone）。
/// 本入口不装配场景、不写生产资产；测试夹具全部在 Play 模式内存中创建并在结束前销毁。
/// </summary>
public static class REFACTOR_BatchAcceptance
{
    public const string ScenePath = "Assets/Scenes/DungeonWorld.unity";
    public const string ActiveKey = "REFACTOR.Active";

    /// <summary>当前里程碑（证据目录 REFACTOR-M&lt;x&gt; 的 x）。每进一个里程碑改一次。</summary>
    public const string DefaultMilestone = "M4";

    public static void Run()
    {
        Debug.Log("[REFACTOR] launcher start; pid=" + System.Diagnostics.Process.GetCurrentProcess().Id +
                  "; args=" + string.Join(" ", Environment.GetCommandLineArgs()));

        try
        {
            string head = QaScene.GetHead();
            SessionState.SetString("REFACTOR.Head", head);
            Debug.Log("[REFACTOR] head=" + head);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("[REFACTOR] opened scene " + scene.path + "; loaded=" + scene.isLoaded +
                      "; dirty=" + scene.isDirty +
                      "; milestone=" + (Environment.GetEnvironmentVariable("REFACTOR_MILESTONE") ?? DefaultMilestone));

            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(REFACTOR_Boot.StartedKey, false);
            SessionState.SetBool(REFACTOR_Boot.FinishedKey, false);

            EditorApplication.EnterPlaymode();
            Debug.Log("[REFACTOR] EnterPlaymode requested");
        }
        catch (Exception e)
        {
            Debug.LogError("[REFACTOR] launcher failed: " + e);
            if (QA_InEditorMenu.TryFinish("REFACTOR", 3)) return;   // 编辑器内：只记失败，不退 Unity
            EditorApplication.Exit(3);                              // batch：原样
        }
    }
}

/// <summary>
/// 域重载后的接续点：进入 Play 时创建运行期探针；退出 Play 后按记录退出码结束。
/// 编辑器内由 QA_InEditorMenu.TryFinish 接管（不退出编辑器）。
/// </summary>
public static class REFACTOR_Boot
{
    public const string StartedKey = "REFACTOR.Started";
    public const string FinishedKey = "REFACTOR.Finished";
    public const string ExitCodeKey = "REFACTOR.ExitCode";
    public const string RunnerNameKey = "REFACTOR.RunnerName";
    public const string PlayStartedAtKey = "REFACTOR.PlayStartedAt";

    const float RunnerLostGraceSeconds = 3f;
    const float FirstStartGraceSeconds = 10f;

    static GameObject _runner;
    static float _missingSince = -1f;

    [InitializeOnLoadMethod]
    static void Boot()
    {
        if (!SessionState.GetBool(REFACTOR_BatchAcceptance.ActiveKey, false)) return;

        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;

        Debug.Log("[REFACTOR] boot; isPlaying=" + Application.isPlaying +
                  "; started=" + SessionState.GetBool(StartedKey, false) +
                  "; finished=" + SessionState.GetBool(FinishedKey, false));

        _missingSince = -1f;
        if (Application.isPlaying) TryStartRunner();
    }

    static void OnPlayModeChanged(PlayModeStateChange change)
    {
        Debug.Log("[REFACTOR] playModeStateChanged=" + change);
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

    static bool ClearStaleRunFlags()
    {
        if (!SessionState.GetBool(StartedKey, false)) return false;
        if (RunnerAlive()) return false;

        Debug.LogWarning("[REFACTOR] stale run flags cleared (previous play session had no runner)");
        SessionState.SetBool(StartedKey, false);
        SessionState.SetBool(FinishedKey, false);
        SessionState.SetBool(REFACTOR_BatchAcceptance.ActiveKey, false);
        SessionState.EraseString(RunnerNameKey);
        _runner = null;
        _missingSince = -1f;
        return true;
    }

    static void Tick()
    {
        if (!SessionState.GetBool(REFACTOR_BatchAcceptance.ActiveKey, false)) return;

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
            Debug.Log("[REFACTOR] tick fallback: isPlaying=true, starting runner");
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
            Debug.LogWarning("[REFACTOR][WATCHDOG] runner missing; counting down " + RunnerLostGraceSeconds + "s");
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
        Debug.LogError("[REFACTOR][WATCHDOG] " + why + " -> code=3 reason=runner-lost-domain-reload");
        if (QA_InEditorMenu.TryFinish("REFACTOR", 3, "runner-lost-domain-reload"))
        {
            EditorApplication.isPlaying = false;
            Debug.Log("[REFACTOR][WATCHDOG] left play mode; editor kept alive");
            return;
        }
        EditorApplication.Exit(3);
    }

    static void ExitWithCode()
    {
        int code = SessionState.GetInt(ExitCodeKey, 3);
        SessionState.SetBool(FinishedKey, false);
        SessionState.SetBool(StartedKey, false);
        SessionState.SetBool(REFACTOR_BatchAcceptance.ActiveKey, false);
        SessionState.EraseString(RunnerNameKey);
        _runner = null;
        _missingSince = -1f;
        Debug.Log("[REFACTOR] exit " + code);
        if (QA_InEditorMenu.TryFinish("REFACTOR", code)) return;
        EditorApplication.Exit(code);
    }

    static void TryStartRunner()
    {
        if (SessionState.GetBool(StartedKey, false)) return;
        SessionState.SetBool(StartedKey, true);
        SessionState.SetFloat(PlayStartedAtKey, Time.realtimeSinceStartup);

        var go = new GameObject("REFACTOR_Runner");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<REFACTOR_ProbeRunner>();
        _runner = go;
        SessionState.SetString(RunnerNameKey, go.name);
        _missingSince = -1f;
        Debug.Log("[REFACTOR] play-mode runner created");
    }
}
