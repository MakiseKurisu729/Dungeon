using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Dungeon.Core;
using Dungeon.Gameplay.Units;

/// <summary>
/// FND-02A batchmode 主入口（唯一）。
/// 命令行：
///   Unity.exe -batchmode -projectPath &lt;repo&gt;
///     -executeMethod FND02A_BatchAcceptance.Run              （red / green）
///     -executeMethod FND02A_BatchAcceptance.AssembleScene    （一次性场景装配）
///     -logFile &lt;evidence&gt;/02A_*.log
///   环境变量 FND02A_MODE=red|green；FND02A_RUN=1|2。
/// Run：打开 DungeonWorld（不保存）→ 进入 Play → FND02A_ProbeRunner 断言 → 退出并返回退出码。
/// AssembleScene：仅用编辑器 API 新增 1 个 Skeleton_Warrior 实例并保存场景，不手改 YAML。
/// </summary>
public static class FND02A_BatchAcceptance
{
    public const string ScenePath = "Assets/Scenes/DungeonWorld.unity";
    public const string EnemyPrefabPath = "Assets/_Project/Prefabs/Skeleton_Warrior.prefab";
    public const string EnemyPrefabGuid = "18dec5e6af31178468b146e0440bd081";
    public const string ActiveKey = "FND02A.Active";

    public static void Run()
    {
        Debug.Log("[FND02A] launcher start; pid=" + System.Diagnostics.Process.GetCurrentProcess().Id +
                  "; args=" + string.Join(" ", Environment.GetCommandLineArgs()));

        try
        {
            string head = GitHead();
            SessionState.SetString("FND02A.Head", head);
            Debug.Log("[FND02A] head=" + head);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            string instanceId = FindPrefabInstanceFileId(ScenePath, EnemyPrefabGuid, out int count);
            string enemySourceGuid = FindEnemySourceGuid(ScenePath);
            SessionState.SetString("FND02A.SceneInstanceFileID", instanceId);
            SessionState.SetString("FND02A.EnemySourceGuid", enemySourceGuid);
            Debug.Log("[FND02A] opened scene " + scene.path + "; loaded=" + scene.isLoaded +
                      "; dirty=" + scene.isDirty +
                      "; enemyPrefabInstances=" + count +
                      "; sceneInstanceFileID=" + instanceId +
                      "; enemySourceGuid=" + enemySourceGuid +
                      "; mode=" + (Environment.GetEnvironmentVariable("FND02A_MODE") ?? "green"));

            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(FND02A_Boot.StartedKey, false);
            SessionState.SetBool(FND02A_Boot.FinishedKey, false);

            EditorApplication.EnterPlaymode();
            Debug.Log("[FND02A] EnterPlaymode requested");
        }
        catch (Exception e)
        {
            Debug.LogError("[FND02A] launcher failed: " + e);
            EditorApplication.Exit(3);
        }
    }

    /// <summary>
    /// 一次性装配：把批准的 Skeleton_Warrior 实例放到 (-2.72, 0, -12)，旋转 identity，并保存场景。
    /// 幂等：已存在 1 个正确实例时不重复新增。全程只通过编辑器 API，不手改 YAML。
    /// </summary>
    public static void AssembleScene()
    {
        try
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[FND02A] prefab not found: " + EnemyPrefabPath);
                EditorApplication.Exit(4);
                return;
            }
            string prefabGuid = AssetDatabase.AssetPathToGUID(EnemyPrefabPath);
            Debug.Log("[FND02A] assemble: prefab=" + EnemyPrefabPath + "; guid=" + prefabGuid);
            if (prefabGuid != EnemyPrefabGuid)
            {
                Debug.LogError("[FND02A] prefab GUID mismatch: expected " + EnemyPrefabGuid + " actual " + prefabGuid);
                EditorApplication.Exit(4);
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            int beforeGUID;
            string beforeId = FindPrefabInstanceFileId(ScenePath, EnemyPrefabGuid, out beforeGUID);
            int prefabInstanceLinesBefore = CountPrefabInstanceLines(ScenePath);
            Debug.Log("[FND02A] assemble: opened " + scene.path + "; dirty=" + scene.isDirty +
                      "; existingEnemyInstances=" + beforeGUID + "; prefabInstanceCount=" + prefabInstanceLinesBefore +
                      "; existingFileID=" + beforeId);

            if (beforeGUID == 1)
            {
                var existing = FindEnemyInstance();
                if (existing != null && Near(existing.transform.position, FND02A_ProbeRunner.EnemyPos, 0.001f))
                {
                    Debug.Log("[FND02A] assemble: already assembled at " + Fmt(existing.transform.position) +
                              "; fileID=" + beforeId + "; no change");
                    EditorApplication.Exit(0);
                    return;
                }
                Debug.LogError("[FND02A] assemble aborted: 已存在 1 个 Skeleton_Warrior 实例但位置不是批准值（" +
                               (existing != null ? Fmt(existing.transform.position) : "null") + "）");
                EditorApplication.Exit(4);
                return;
            }
            if (beforeGUID > 1)
            {
                Debug.LogError("[FND02A] assemble aborted: Skeleton_Warrior 实例数 = " + beforeGUID + "（应为 0 或 1）");
                EditorApplication.Exit(4);
                return;
            }

            GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            if (go == null)
            {
                Debug.LogError("[FND02A] assemble failed: InstantiatePrefab returned null");
                EditorApplication.Exit(4);
                return;
            }
            go.transform.position = FND02A_ProbeRunner.EnemyPos;
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[FND02A] assemble: instantiated name=" + go.name +
                      "; position=" + Fmt(go.transform.position) +
                      "; rotation=" + Fmt(go.transform.rotation.eulerAngles) +
                      "; prefabPath=" + PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go) +
                      "; saved=" + saved);

            int afterGUID;
            string afterId = FindPrefabInstanceFileId(ScenePath, EnemyPrefabGuid, out afterGUID);
            int prefabInstanceLinesAfter = CountPrefabInstanceLines(ScenePath);
            Debug.Log("[FND02A] assemble: after save enemyInstances=" + afterGUID +
                      "; fileID=" + afterId +
                      "; prefabInstanceCount=" + prefabInstanceLinesAfter +
                      "; delta=" + (prefabInstanceLinesAfter - prefabInstanceLinesBefore));

            bool ok = saved && afterGUID == 1 &&
                      (prefabInstanceLinesAfter - prefabInstanceLinesBefore) == 1 &&
                      go.transform.position == FND02A_ProbeRunner.EnemyPos;
            Debug.Log("[FND02A] assemble result=" + (ok ? "ok" : "fail"));
            EditorApplication.Exit(ok ? 0 : 4);
        }
        catch (Exception e)
        {
            Debug.LogError("[FND02A] assemble failed: " + e);
            EditorApplication.Exit(4);
        }
    }

    static GameObject FindEnemyInstance()
    {
        foreach (var h in UnityEngine.Object.FindObjectsByType<UnitHealth>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (h == null || h.team != Team.Monster) continue;
            return h.gameObject;
        }
        return null;
    }

    static bool Near(Vector3 a, Vector3 b, float tol)
    {
        return Mathf.Abs(a.x - b.x) <= tol && Mathf.Abs(a.y - b.y) <= tol && Mathf.Abs(a.z - b.z) <= tol;
    }

    static string Fmt(Vector3 v)
    {
        return "(" + v.x.ToString("0.###") + ", " + v.y.ToString("0.###") + ", " + v.z.ToString("0.###") + ")";
    }

    /// <summary>读取场景 YAML（只读），返回指定预制体 GUID 的 PrefabInstance fileID；count 为实例数。</summary>
    public static string FindPrefabInstanceFileId(string scenePath, string prefabGuid, out int count)
    {
        count = 0;
        string found = "none";
        string abs = AbsPath(scenePath);
        if (!File.Exists(abs)) return found;
        string[] lines = File.ReadAllLines(abs, Encoding.UTF8);
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].IndexOf("m_SourcePrefab:", StringComparison.Ordinal) < 0) continue;
            if (lines[i].IndexOf(prefabGuid, StringComparison.Ordinal) < 0) continue;
            count++;
            for (int j = i; j >= 0; j--)
            {
                string l = lines[j].Trim();
                if (l.StartsWith("--- !u!1001 &", StringComparison.Ordinal))
                {
                    found = l.Substring("--- !u!1001 &".Length).Trim();
                    break;
                }
            }
        }
        return found;
    }

    /// <summary>只读场景 YAML，返回敌人的 m_SourcePrefab guid（从文件解析，不假定常量）。</summary>
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

    static int CountPrefabInstanceLines(string scenePath)
    {        string abs = AbsPath(scenePath);
        if (!File.Exists(abs)) return -1;
        int n = 0;
        foreach (string line in File.ReadAllLines(abs, Encoding.UTF8))
            if (line.IndexOf("m_SourcePrefab:", StringComparison.Ordinal) >= 0) n++;
        return n;
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
            Debug.LogWarning("[FND02A] git head read failed: " + e.Message);
            return "unknown";
        }
    }
}

/// <summary>
/// 域重载后的接续点：进入 Play 时创建运行期探针；退出 Play 后按记录退出码结束 batchmode。
/// BatchMode 下 EnterPlaymode 的域重载可能早于 Application.isPlaying 变 true，
/// 因此除 InitializeOnLoadMethod 外还订阅 playModeStateChanged，并在 update 中兜底创建。
/// </summary>
public static class FND02A_Boot
{
    public const string StartedKey = "FND02A.Started";
    public const string FinishedKey = "FND02A.Finished";
    public const string ExitCodeKey = "FND02A.ExitCode";

    [InitializeOnLoadMethod]
    static void Boot()
    {
        if (!SessionState.GetBool(FND02A_BatchAcceptance.ActiveKey, false)) return;

        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;

        Debug.Log("[FND02A] boot; isPlaying=" + Application.isPlaying +
                  "; started=" + SessionState.GetBool(StartedKey, false) +
                  "; finished=" + SessionState.GetBool(FinishedKey, false));

        if (Application.isPlaying) TryStartRunner();
    }

    static void OnPlayModeChanged(PlayModeStateChange change)
    {
        Debug.Log("[FND02A] playModeStateChanged=" + change);
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
            Debug.Log("[FND02A] tick fallback: isPlaying=true, starting runner");
            TryStartRunner();
        }
    }

    static void ExitWithCode()
    {
        int code = SessionState.GetInt(ExitCodeKey, 3);
        SessionState.SetBool(FinishedKey, false);
        SessionState.SetBool(FND02A_BatchAcceptance.ActiveKey, false);
        Debug.Log("[FND02A] batchmode exit " + code);
        EditorApplication.Exit(code);
    }

    static void TryStartRunner()
    {
        if (SessionState.GetBool(StartedKey, false)) return;
        SessionState.SetBool(StartedKey, true);

        var go = new GameObject("FND02A_Runner");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<FND02A_ProbeRunner>();
        Debug.Log("[FND02A] play-mode runner created");
    }
}
