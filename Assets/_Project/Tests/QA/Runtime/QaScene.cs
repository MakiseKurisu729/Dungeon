using System;
using System.IO;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 场景与仓库查询：版本号、敌人预制体来源 GUID、项目绝对路径、场景常量。
/// 只读：不打开、不保存、不修改任何场景或资产。
/// </summary>
public static class QaScene
{
    public const string ScenePath = "Assets/Scenes/DungeonWorld.unity";
    public const string EnemyPrefabGuid = "18dec5e6af31178468b146e0440bd081";

    /// <summary>
    /// 版本号。<paramref name="sessionKey"/> 为空时执行 <c>git rev-parse HEAD</c>（失败返回 "unknown"）；
    /// 给出 sessionKey 时读取启动阶段写进 SessionState 的缓存值（Play 模式下不再起进程，域重载后仍可读）。
    /// </summary>
    public static string GetHead(string sessionKey = null)
    {
        if (!string.IsNullOrEmpty(sessionKey))
        {
#if UNITY_EDITOR
            return SessionState.GetString(sessionKey, "unknown");
#else
            return "unknown";
#endif
        }

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
            Debug.LogWarning("[QA][SCENE] git head read failed: " + e.Message);
            return "unknown";
        }
    }

    /// <summary>
    /// 只读场景 YAML，返回场景里 <see cref="EnemyPrefabGuid"/> 的 <c>m_SourcePrefab</c> guid；找不到返回 "none"。
    /// <paramref name="sessionKey"/> 非空时改读启动阶段缓存值（同 <see cref="GetHead"/> 的理由）。
    /// </summary>
    public static string GetEnemySourceGuid(string scenePath, string sessionKey = null)
    {
        if (!string.IsNullOrEmpty(sessionKey))
        {
#if UNITY_EDITOR
            return SessionState.GetString(sessionKey, "none");
#else
            return "none";
#endif
        }

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

    /// <summary>把 Assets 相对路径解析成项目根下的绝对路径（已是绝对路径则原样返回）。</summary>
    public static string AbsPath(string assetPath)
    {
        if (Path.IsPathRooted(assetPath)) return assetPath;
        return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
    }
}
