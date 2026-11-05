using System;
using System.IO;
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
            string head = GitHead();
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
            EditorApplication.Exit(3);
        }
    }

    static string GitHead()
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("git", "rev-parse HEAD")
            {
                WorkingDirectory = Directory.GetParent(Application.dataPath).FullName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var p = System.Diagnostics.Process.Start(psi))
            {
                string outp = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit(5000);
                return string.IsNullOrEmpty(outp) ? "unknown" : outp;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[DIAG] git head read failed: " + e.Message);
            return "unknown";
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

        if (Application.isPlaying) TryStartRunner();
    }

    static void OnPlayModeChanged(PlayModeStateChange change)
    {
        Debug.Log("[DIAG] playModeStateChanged=" + change);
        if (change == PlayModeStateChange.EnteredPlayMode) TryStartRunner();
        else if (change == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(FinishedKey, false)) ExitWithCode();
    }

    static void Tick()
    {
        if (SessionState.GetBool(FinishedKey, false))
        {
            if (!Application.isPlaying) ExitWithCode();
            return;
        }
        if (Application.isPlaying && !SessionState.GetBool(StartedKey, false))
        {
            Debug.Log("[DIAG] tick fallback: isPlaying=true, starting runner");
            TryStartRunner();
        }
    }

    static void ExitWithCode()
    {
        int code = SessionState.GetInt(ExitCodeKey, 3);
        SessionState.SetBool(FinishedKey, false);
        SessionState.SetBool(ActiveKey, false);
        Debug.Log("[DIAG] batchmode exit " + code);
        EditorApplication.Exit(code);
    }

    static void TryStartRunner()
    {
        if (SessionState.GetBool(StartedKey, false)) return;
        SessionState.SetBool(StartedKey, true);

        var go = new GameObject("DIAG_Runner");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<DIAG_ProbeRunner>();
        Debug.Log("[DIAG] play-mode runner created");
    }
}
