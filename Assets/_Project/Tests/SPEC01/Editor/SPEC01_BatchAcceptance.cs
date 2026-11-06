using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// SPEC-01 batchmode 主入口（唯一）。
/// 命令行：
///   Unity.exe -batchmode -projectPath &lt;repo&gt;
///     -executeMethod SPEC01_BatchAcceptance.Run
///     -logFile &lt;evidence&gt;/SPEC01_Automation_runN.log
///   环境变量 SPEC01_MODE=red|green；SPEC01_RUN=1|2|3。
/// Run：打开 DungeonWorld（不保存）→ 进入 Play → SPEC01_ProbeRunner 断言 → 退出并返回退出码。
/// 本入口不装配场景、不写生产资产；测试夹具全部在 Play 模式内存中创建并在结束前销毁。
/// </summary>
public static class SPEC01_BatchAcceptance
{
    public const string ScenePath = QaScene.ScenePath;
    public const string EnemyPrefabGuid = QaScene.EnemyPrefabGuid;
    public const string ActiveKey = "SPEC01.Active";

    public static void Run()
    {
        Debug.Log("[SPEC01] launcher start; pid=" + System.Diagnostics.Process.GetCurrentProcess().Id +
                  "; args=" + string.Join(" ", Environment.GetCommandLineArgs()));

        try
        {
            string head = QaScene.GetHead();
            SessionState.SetString("SPEC01.Head", head);
            Debug.Log("[SPEC01] head=" + head);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            string enemySourceGuid = QaScene.GetEnemySourceGuid(ScenePath);
            SessionState.SetString("SPEC01.EnemySourceGuid", enemySourceGuid);
            Debug.Log("[SPEC01] opened scene " + scene.path + "; loaded=" + scene.isLoaded +
                      "; dirty=" + scene.isDirty +
                      "; enemySourceGuid=" + enemySourceGuid +
                      "; mode=" + (Environment.GetEnvironmentVariable("SPEC01_MODE") ?? "green"));

            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(SPEC01_Boot.StartedKey, false);
            SessionState.SetBool(SPEC01_Boot.FinishedKey, false);

            EditorApplication.EnterPlaymode();
            Debug.Log("[SPEC01] EnterPlaymode requested");
        }
        catch (Exception e)
        {
            Debug.LogError("[SPEC01] launcher failed: " + e);
            if (QA_InEditorMenu.TryFinish("SPEC01", 3)) return;   // 编辑器内：只记失败，不退 Unity
            EditorApplication.Exit(3);                            // batch：原样
        }
    }
}

/// <summary>
/// 域重载后的接续点：进入 Play 时创建运行期探针；退出 Play 后按记录退出码结束 batchmode。
/// </summary>
public static class SPEC01_Boot
{
    public const string StartedKey = "SPEC01.Started";
    public const string FinishedKey = "SPEC01.Finished";
    public const string ExitCodeKey = "SPEC01.ExitCode";
    public const string RunnerNameKey = "SPEC01.RunnerName";
    public const string PlayStartedAtKey = "SPEC01.PlayStartedAt";

    /// <summary>看门狗：started=true 但 runner 丢失持续 3s → 判定本轮失败（域重载典型症状）。</summary>
    const float RunnerLostGraceSeconds = 3f;

    /// <summary>看门狗：进 Play 后 10s 仍未创建 runner → 首启失败。</summary>
    const float FirstStartGraceSeconds = 10f;

    static GameObject _runner;
    static float _missingSince = -1f;

    [InitializeOnLoadMethod]
    static void Boot()
    {
        if (!SessionState.GetBool(SPEC01_BatchAcceptance.ActiveKey, false)) return;

        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;

        Debug.Log("[SPEC01] boot; isPlaying=" + Application.isPlaying +
                  "; started=" + SessionState.GetBool(StartedKey, false) +
                  "; finished=" + SessionState.GetBool(FinishedKey, false));

        _missingSince = -1f;
        if (Application.isPlaying) TryStartRunner();
    }

    static void OnPlayModeChanged(PlayModeStateChange change)
    {
        Debug.Log("[SPEC01] playModeStateChanged=" + change);
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

    /// <summary>
    /// 上一轮被中断（手动退 Play / 编辑器重开）会留下 Started/Active 标记，但 runner 已随场景销毁。
    /// 新一轮 EnterPlayMode 时识别出来并清掉，绝不能拿上一轮的标记去触发看门狗。
    /// </summary>
    static bool ClearStaleRunFlags()
    {
        if (!SessionState.GetBool(StartedKey, false)) return false;
        if (RunnerAlive()) return false;

        Debug.LogWarning("[SPEC01] stale run flags cleared (previous play session had no runner)");
        SessionState.SetBool(StartedKey, false);
        SessionState.SetBool(FinishedKey, false);
        SessionState.SetBool(SPEC01_BatchAcceptance.ActiveKey, false);
        SessionState.EraseString(RunnerNameKey);
        _runner = null;
        _missingSince = -1f;
        return true;
    }

    static void Tick()
    {
        if (!SessionState.GetBool(SPEC01_BatchAcceptance.ActiveKey, false)) return;

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
            Debug.Log("[SPEC01] tick fallback: isPlaying=true, starting runner");
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
            Debug.LogWarning("[SPEC01][WATCHDOG] runner missing; counting down " + RunnerLostGraceSeconds + "s");
        }
        else if (now - _missingSince >= RunnerLostGraceSeconds)
        {
            WatchdogFail("runner missing >= " + RunnerLostGraceSeconds + "s");
        }
    }

    /// <summary>runner 存活判断：静态引用优先，其次按 SessionState 记住的名字在场景里找（域重载后静态引用会丢）。</summary>
    static bool RunnerAlive()
    {
        if (_runner != null) return true;
        string name = SessionState.GetString(RunnerNameKey, string.Empty);
        if (string.IsNullOrEmpty(name)) return true;   // 还没记录名字，不误判
        return GameObject.Find(name) != null;
    }

    /// <summary>
    /// 写 code=3 + reason=runner-lost-domain-reload，然后退出 Play；编辑器内**不退出编辑器**。
    /// batch 路径（没有 QA.InEditor 标记）保持原有 Exit(3) 行为。
    /// </summary>
    static void WatchdogFail(string why)
    {
        Debug.LogError("[SPEC01][WATCHDOG] " + why + " -> code=3 reason=runner-lost-domain-reload");
        if (QA_InEditorMenu.TryFinish("SPEC01", 3, "runner-lost-domain-reload"))
        {
            EditorApplication.isPlaying = false;
            Debug.Log("[SPEC01][WATCHDOG] left play mode; editor kept alive");
            return;
        }
        EditorApplication.Exit(3);
    }

    static void ExitWithCode()
    {
        int code = SessionState.GetInt(ExitCodeKey, 3);
        SessionState.SetBool(FinishedKey, false);
        SessionState.SetBool(StartedKey, false);
        SessionState.SetBool(SPEC01_BatchAcceptance.ActiveKey, false);
        SessionState.EraseString(RunnerNameKey);
        _runner = null;
        _missingSince = -1f;
        Debug.Log("[SPEC01] batchmode exit " + code);
        if (QA_InEditorMenu.TryFinish("SPEC01", code)) return;
        EditorApplication.Exit(code);
    }

    static void TryStartRunner()
    {
        if (SessionState.GetBool(StartedKey, false)) return;
        SessionState.SetBool(StartedKey, true);
        SessionState.SetFloat(PlayStartedAtKey, Time.realtimeSinceStartup);

        var go = new GameObject("SPEC01_Runner");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<SPEC01_ProbeRunner>();
        _runner = go;
        SessionState.SetString(RunnerNameKey, go.name);
        _missingSince = -1f;
        Debug.Log("[SPEC01] play-mode runner created");
    }
}
