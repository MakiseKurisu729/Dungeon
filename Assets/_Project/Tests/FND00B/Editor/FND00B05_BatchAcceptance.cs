using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// FND-00B-05 batchmode 主入口（唯一）。
/// 命令行：
///   Unity.exe -batchmode -projectPath &lt;repo&gt;
///     -executeMethod FND00B05_BatchAcceptance.Run
///     -logFile &lt;evidence&gt;/05_Automation_runN.log
///   环境变量 FND00B05_RUN=N 标记运行序号。
/// 流程：打开 DungeonWorld（只读，不保存）→ 进入 Play → FND00B05_ProbeRunner 执行 11→12 → 退出并返回退出码。
/// 不安装包、不修改生产代码/场景/预制体/ProjectSettings。
/// </summary>
public static class FND00B05_BatchAcceptance
{
    public const string ScenePath = "Assets/Scenes/DungeonWorld.unity";

    public static void Run()
    {
        Debug.Log("[FND00B05] launcher start; pid=" + System.Diagnostics.Process.GetCurrentProcess().Id +
                  "; args=" + string.Join(" ", Environment.GetCommandLineArgs()));

        try
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("[FND00B05] opened scene " + scene.path + "; loaded=" + scene.isLoaded +
                      "; dirty=" + scene.isDirty);

            SessionState.SetBool(FND00B05_Boot.ActiveKey, true);
            SessionState.SetBool(FND00B05_Boot.StartedKey, false);
            SessionState.SetBool(FND00B05_Boot.FinishedKey, false);

            EditorApplication.EnterPlaymode();
            Debug.Log("[FND00B05] EnterPlaymode requested");
        }
        catch (Exception e)
        {
            Debug.LogError("[FND00B05] launcher failed: " + e);
            EditorApplication.Exit(3);
        }
    }
}

/// <summary>
/// 域重载后的接续点：进入 Play 时创建运行期探针；退出 Play 后按记录退出码结束 batchmode。
/// BatchMode 下 EnterPlaymode 的域重载可能早于 Application.isPlaying 变 true，
/// 因此除 InitializeOnLoadMethod 外还订阅 playModeStateChanged，并在 update 中兜底创建。
/// </summary>
public static class FND00B05_Boot
{
    public const string ActiveKey = "FND00B05.Active";
    public const string StartedKey = "FND00B05.Started";
    public const string FinishedKey = "FND00B05.Finished";
    public const string ExitCodeKey = "FND00B05.ExitCode";

    [InitializeOnLoadMethod]
    static void Boot()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;

        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;

        Debug.Log("[FND00B05] boot; isPlaying=" + Application.isPlaying +
                  "; started=" + SessionState.GetBool(StartedKey, false) +
                  "; finished=" + SessionState.GetBool(FinishedKey, false));

        if (Application.isPlaying) TryStartRunner();
    }

    static void OnPlayModeChanged(PlayModeStateChange change)
    {
        Debug.Log("[FND00B05] playModeStateChanged=" + change);
        if (change == PlayModeStateChange.EnteredPlayMode)
        {
            TryStartRunner();
        }
        else if (change == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(FinishedKey, false))
        {
            ExitWithCode();
        }
    }

    /// <summary>BatchMode 下 playModeStateChanged 可能不可靠，用编辑器 update 兜底。</summary>
    static void Tick()
    {
        if (SessionState.GetBool(FinishedKey, false))
        {
            if (!Application.isPlaying) ExitWithCode();
            return;
        }
        if (Application.isPlaying && !SessionState.GetBool(StartedKey, false))
        {
            Debug.Log("[FND00B05] tick fallback: isPlaying=true, starting runner");
            TryStartRunner();
        }
    }

    static void ExitWithCode()
    {
        int code = SessionState.GetInt(ExitCodeKey, 3);
        SessionState.SetBool(FinishedKey, false);
        Debug.Log("[FND00B05] batchmode exit " + code);
        EditorApplication.Exit(code);
    }

    static void TryStartRunner()
    {
        if (SessionState.GetBool(StartedKey, false)) return;
        SessionState.SetBool(StartedKey, true);

        var go = new GameObject("FND00B05_Runner");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<FND00B05_ProbeRunner>();
        Debug.Log("[FND00B05] play-mode runner created");
    }
}
