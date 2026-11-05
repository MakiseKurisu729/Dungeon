using System;
using System.IO;
using System.Text;
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
    public const string ScenePath = "Assets/Scenes/DungeonWorld.unity";
    public const string EnemyPrefabGuid = "18dec5e6af31178468b146e0440bd081";
    public const string ActiveKey = "SPEC01.Active";

    public static void Run()
    {
        Debug.Log("[SPEC01] launcher start; pid=" + System.Diagnostics.Process.GetCurrentProcess().Id +
                  "; args=" + string.Join(" ", Environment.GetCommandLineArgs()));

        try
        {
            string head = GitHead();
            SessionState.SetString("SPEC01.Head", head);
            Debug.Log("[SPEC01] head=" + head);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            string enemySourceGuid = FindEnemySourceGuid(ScenePath);
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

    /// <summary>只读场景 YAML，返回敌人 m_SourcePrefab guid。</summary>
    public static string FindEnemySourceGuid(string scenePath)
    {
        string abs = AbsPath(scenePath);
        if (!File.Exists(abs)) return "none";
        string found = "none";
        foreach (string line in File.ReadAllLines(abs, Encoding.UTF8))
        {
            if (line.IndexOf("m_SourcePrefab:", StringComparison.Ordinal) < 0) continue;
            int gi = line.IndexOf("guid:", StringComparison.Ordinal);
            if (gi < 0) continue;
            string g = line.Substring(gi + 5).Trim().TrimEnd('}').Trim();
            int comma = g.IndexOf(',');
            if (comma > 0) g = g.Substring(0, comma).Trim();
            if (g == EnemyPrefabGuid) found = g;
        }
        return found;
    }

    static string AbsPath(string assetPath)
    {
        if (Path.IsPathRooted(assetPath)) return assetPath;
        return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
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
            Debug.LogWarning("[SPEC01] git head read failed: " + e.Message);
            return "unknown";
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

        if (Application.isPlaying) TryStartRunner();
    }

    static void OnPlayModeChanged(PlayModeStateChange change)
    {
        Debug.Log("[SPEC01] playModeStateChanged=" + change);
        if (change == PlayModeStateChange.EnteredPlayMode)
        {
            TryStartRunner();
        }
        else if (change == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(FinishedKey, false))
        {
            ExitWithCode();
        }
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
            Debug.Log("[SPEC01] tick fallback: isPlaying=true, starting runner");
            TryStartRunner();
        }
    }

    static void ExitWithCode()
    {
        int code = SessionState.GetInt(ExitCodeKey, 3);
        SessionState.SetBool(FinishedKey, false);
        SessionState.SetBool(SPEC01_BatchAcceptance.ActiveKey, false);
        Debug.Log("[SPEC01] batchmode exit " + code);
        if (QA_InEditorMenu.TryFinish("SPEC01", code)) return;
        EditorApplication.Exit(code);
    }

    static void TryStartRunner()
    {
        if (SessionState.GetBool(StartedKey, false)) return;
        SessionState.SetBool(StartedKey, true);

        var go = new GameObject("SPEC01_Runner");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<SPEC01_ProbeRunner>();
        Debug.Log("[SPEC01] play-mode runner created");
    }
}
