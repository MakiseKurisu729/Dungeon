using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 三套既有探针（SPEC01 / FND00B06 / DIAG）的“编辑器内运行”菜单入口。
/// 只负责驱动：闸门检查 → 设置环境变量与证据子目录 → 调用既有 *_BatchAcceptance.Run()。
/// 断言、阈值、夹具、场景全部复用既有实现，本类不含任何探针逻辑。
/// 退出分叉发生在各自 *_Boot.ExitWithCode：编辑器内由 TryFinish 接管（不 Exit），
/// batch 路径没有 SessionState 标记，仍然原样执行 EditorApplication.Exit(code)。
/// </summary>
public static class QA_InEditorMenu
{
    public const string EvidenceSubdirEnv = "QA_EVIDENCE_SUBDIR";
    public const string SessionActiveKey = "QA.InEditor.Active";
    public const string SessionTaskKey = "QA.InEditor.Task";

    /// <summary>进 Play 前的 runInBackground 原值（收尾按它还原，不硬编码 false）。</summary>
    public const string SessionRunInBackgroundKey = "QA.PrevRunInBackground";

    /// <summary>已接管 runInBackground 的标记：保证还原只做一次，且回滚值确实被记录过。</summary>
    public const string SessionRunInBackgroundArmedKey = "QA.PrevRunInBackground.Armed";
    public const string DefaultSubdir = "editor_run1";

    /// <summary>preflight 的待执行任务（SessionState 跨域重载存活，重载后自动重新布线）。</summary>
    public const string PreflightTaskKey = "QA.Preflight.Task";
    public const string PreflightStartKey = "QA.Preflight.StartedAt";

    const double PreflightTimeoutSeconds = 120.0;   // R-1：超过 120s 未稳定即放弃
    const double PreflightPollSeconds = 0.2;        // 每 200ms 重查一次

    static double _lastPollAt = -1.0;
    static int _stableSamples;
    static bool _preflightArmed;
    static PropertyInfo _isDomainReloadPendingProp;

    /// <summary>需要在收尾时清空的套件环境变量（AC-8：不污染后续 batch）。</summary>
    static readonly string[] SuiteEnvKeys =
    {
        "SPEC01_MODE",
        "SPEC01_RUN",
        "SPEC01_EVIDENCE_TAG",
        "FND00B06_RUN",
        "DIAG_RUN",
        "DIAG_MODE",
        EvidenceSubdirEnv
    };

    [MenuItem("Tools/QA/In-Editor/SPEC01 (green)", false, 100)]
    public static void RunSPEC01()
    {
        Run("SPEC01");
    }

    [MenuItem("Tools/QA/In-Editor/FND00B06", false, 101)]
    public static void RunFND00B06()
    {
        Run("FND00B06");
    }

    [MenuItem("Tools/QA/In-Editor/DIAG", false, 102)]
    public static void RunDIAG()
    {
        Run("DIAG");
    }

    /// <summary>
    /// 菜单驱动入口：task ∈ SPEC01 / FND00B06 / DIAG。
    /// R-1：先刷新资产库，再非阻塞轮询到“编译/资产刷新/域重载都结束且连续 2 次成立”才进 Play；
    /// 超过 120s 未稳定则写 InEditorResult(code=3, reason=preflight-timeout) 并放弃。
    /// </summary>
    public static void Run(string task)
    {
        if (!GateForLaunch(task)) return;

        AssetDatabase.Refresh();

        SessionState.SetString(PreflightTaskKey, task);
        SessionState.SetFloat(PreflightStartKey, (float)EditorApplication.timeSinceStartup);
        _stableSamples = 0;
        _lastPollAt = -1.0;
        ArmPreflight();
    }

    /// <summary>
    /// RECOVERY ONLY（MCP 桥未自动连接时用 -executeMethod 手动拉起；不参与探针逻辑）：
    /// 设置 AutoStartOnLoad 并反射调用 MCPForUnity 的 TransportManager.StartAsync(Http)。
    /// </summary>
    public static void RecoverMcpBridge()
    {
        try
        {
            EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", true);

            System.Reflection.Assembly asm = null;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (a.GetName().Name == "MCPForUnity.Editor") { asm = a; break; }
            }
            if (asm == null) { Debug.LogError("[QA][RECOVERY] MCPForUnity.Editor not loaded"); return; }

            var locator = asm.GetType("MCPForUnity.Editor.Services.MCPServiceLocator");
            var tm = locator.GetProperty("TransportManager").GetValue(null, null);
            var modeType = asm.GetType("MCPForUnity.Editor.Services.Transport.TransportMode");
            object http = Enum.Parse(modeType, "Http");
            var start = tm.GetType().GetMethod("StartAsync");
            var task = (System.Threading.Tasks.Task)start.Invoke(tm, new object[] { http });

            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (!task.IsCompleted && sw.ElapsedMilliseconds < 20000)
            {
                System.Threading.Thread.Sleep(50);
            }
            bool ok = task.IsCompleted && !task.IsFaulted && !task.IsCanceled && (bool)task.GetType().GetProperty("Result").GetValue(task, null);
            Debug.Log("[QA][RECOVERY] mcp bridge start completed=" + task.IsCompleted + " success=" + ok);
        }
        catch (Exception e)
        {
            Debug.LogError("[QA][RECOVERY] " + e);
        }
    }

    /// <summary>
    /// 域重载后自动重新布线 preflight：静态字段会丢，但 SessionState 里的 task 还在。
    /// </summary>
    [InitializeOnLoadMethod]
    static void ReArmPreflightAfterReload()
    {
        string task = SessionState.GetString(PreflightTaskKey, string.Empty);
        if (string.IsNullOrEmpty(task)) return;

        _stableSamples = 0;
        _lastPollAt = -1.0;
        ArmPreflight();
        Debug.Log("[QA][INEDITOR] preflight re-armed after reload; task=" + task);
    }

    static void ArmPreflight()
    {
        if (_preflightArmed) return;
        _preflightArmed = true;
        EditorApplication.update -= PreflightTick;
        EditorApplication.update += PreflightTick;
    }

    static void DisarmPreflight()
    {
        if (_preflightArmed)
        {
            EditorApplication.update -= PreflightTick;
            _preflightArmed = false;
        }
        SessionState.EraseString(PreflightTaskKey);
        SessionState.EraseFloat(PreflightStartKey);
    }

    static void PreflightTick()
    {
        string task = SessionState.GetString(PreflightTaskKey, string.Empty);
        if (string.IsNullOrEmpty(task))
        {
            DisarmPreflight();
            return;
        }

        double startAt = SessionState.GetFloat(PreflightStartKey, (float)EditorApplication.timeSinceStartup);
        double elapsed = EditorApplication.timeSinceStartup - startAt;

        if (elapsed > PreflightTimeoutSeconds)
        {
            DisarmPreflight();
            ClearSuiteEnvironment();
            string subdir = NextSubdir(task);
            WriteResultFile(task, 3, subdir, "preflight-timeout");
            Debug.LogError("[QA][INEDITOR] preflight timeout task=" + task +
                           "; waited=" + elapsed.ToString("0.0") + "s" +
                           "; isCompiling=" + EditorApplication.isCompiling +
                           "; isUpdating=" + EditorApplication.isUpdating);
            return;
        }

        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            DisarmPreflight();
            Debug.LogWarning("[QA][INEDITOR] preflight aborted task=" + task + " reason=enteredPlaymode");
            return;
        }

        // 每 200ms 才采样一次，避免一帧内连点 2 次造成“连续 2 次”失真。
        double sincePoll = _lastPollAt < 0.0 ? double.MaxValue : EditorApplication.timeSinceStartup - _lastPollAt;
        if (sincePoll < PreflightPollSeconds) return;
        _lastPollAt = EditorApplication.timeSinceStartup;

        if (IsEditorStable()) _stableSamples++;
        else _stableSamples = 0;

        if (_stableSamples < 2) return;

        DisarmPreflight();
        Debug.Log("[QA][INEDITOR] preflight ok (waited=" + elapsed.ToString("0.0") + "s)");
        Proceed(task);
    }

    /// <summary>编译中 / 资产刷新中 / 域重载挂起，三者任一都视为未稳定。</summary>
    static bool IsEditorStable()
    {
        return !EditorApplication.isCompiling
            && !EditorApplication.isUpdating
            && !IsDomainReloadPending();
    }

    /// <summary>Unity 内部属性；不同版本可能不存在，取不到按 false 处理（真正的重载会体现为 isCompiling）。</summary>
    static bool IsDomainReloadPending()
    {
        if (_isDomainReloadPendingProp == null)
        {
            _isDomainReloadPendingProp = typeof(EditorApplication).GetProperty(
                "isDomainReloadPending", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }
        if (_isDomainReloadPendingProp == null) return false;
        try { return (bool)_isDomainReloadPendingProp.GetValue(null, null); }
        catch { return false; }
    }

    static void Proceed(string task)
    {
        if (!GateForLaunch(task)) return;

        SessionState.SetBool(SessionActiveKey, true);
        SessionState.SetString(SessionTaskKey, task);

        // 先清掉上一次可能残留的套件变量，再按套件注入，避免跨套件污染。
        ClearSuiteEnvironment();
        string subdir = NextSubdir(task);   // editor_run1 / editor_run2 / … 递增，不覆盖历史证据
        Environment.SetEnvironmentVariable(EvidenceSubdirEnv, subdir);

        if (task == "SPEC01")
        {
            Environment.SetEnvironmentVariable("SPEC01_MODE", "green");
            Environment.SetEnvironmentVariable("SPEC01_RUN", "1");
        }
        else if (task == "FND00B06")
        {
            Environment.SetEnvironmentVariable("FND00B06_RUN", "1");
        }
        else if (task == "DIAG")
        {
            Environment.SetEnvironmentVariable("DIAG_MODE", "probe");
            Environment.SetEnvironmentVariable("DIAG_RUN", "1");
        }
        else
        {
            Debug.LogError("[QA][INEDITOR] refuse task=" + task + " reason=unknownTask");
            ResetFlags();
            ClearSuiteEnvironment();
            return;
        }

        // 编辑器内无人值守跑探针：失焦时不冻结 Play 帧循环。
        // 注意：编辑器里的 Application.runInBackground 会写穿到 PlayerSettings（ProjectSettings.asset），
        // 故先记录原值、收尾还原，避免把“切后台继续跑”带进打包。
        ApplyRunInBackground();

        Debug.Log("[QA][INEDITOR] start task=" + task +
                  "; subdir=" + subdir +
                  "; unity=" + Application.unityVersion +
                  "; runInBackground=" + Application.runInBackground +
                  "; envAtStart=" + EnvironmentSnapshot());

        try
        {
            switch (task)
            {
                case "SPEC01":
                    SPEC01_BatchAcceptance.Run();
                    break;
                case "FND00B06":
                    FND00B06_BatchAcceptance.Run();
                    break;
                default:
                    DIAG_BatchAcceptance.Run();
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[QA][INEDITOR] launcher threw task=" + task + "; err=" + e);
            ResetFlags();
            ClearSuiteEnvironment();
            // 异常可能绕过 TryFinish，这里兜底还原（RestoreRunInBackground 幂等）。
            RestoreRunInBackground();
        }
    }

    /// <summary>
    /// 编辑器内的收尾钩子：由各套 *_Boot.ExitWithCode 在 EditorApplication.Exit 之前调用。
    /// 返回 true = 编辑器内运行，已写结果文件、打 token、清标记与环境变量，调用方直接 return。
    /// 返回 false = 不是编辑器内运行（batch 路径），调用方保持原有 Exit 行为。
    /// </summary>
    public static bool TryFinish(string task, int code)
    {
        return TryFinish(task, code, null);
    }

    /// <summary>
    /// 同上，额外把失败原因写进 InEditorResult.txt 的 reason 字段（看门狗 / preflight 用）。
    /// </summary>
    public static bool TryFinish(string task, int code, string reason)
    {
        if (!SessionState.GetBool(SessionActiveKey, false)) return false;
        string active = SessionState.GetString(SessionTaskKey, string.Empty);
        if (!string.Equals(active, task, StringComparison.Ordinal)) return false;

        string subdir = Environment.GetEnvironmentVariable(EvidenceSubdirEnv);
        if (string.IsNullOrEmpty(subdir)) subdir = DefaultSubdir;
        subdir = subdir.Trim();

        WriteResultFile(task, code, subdir, reason);

        ResetFlags();
        // 收尾还原 runInBackground：只回滚到“原本为 false”的情形，不关掉用户自己打开的设置。
        RestoreRunInBackground();

        Debug.Log("[QA][INEDITOR] done task=" + task + " code=" + code +
                  (string.IsNullOrEmpty(reason) ? string.Empty : " reason=" + reason));
        return true;
    }

    /// <summary>
    /// 唯一的落盘实现：Code / reason / 环境快照 / 证据子目录。preflight 失败与看门狗都走这里。
    /// </summary>
    static void WriteResultFile(string task, int code, string subdir, string reason)
    {
        try
        {
            string dir = Path.Combine(EvidenceRoot(task), subdir);
            Directory.CreateDirectory(dir);

            ClearSuiteEnvironment();
            string envAfter = EnvironmentSnapshot();

            var sb = new StringBuilder();
            sb.Append("task=").Append(task).Append('\n');
            sb.Append("code=").Append(code).Append('\n');
            if (!string.IsNullOrEmpty(reason)) sb.Append("reason=").Append(reason).Append('\n');
            sb.Append("finishedAt=").Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")).Append('\n');
            sb.Append("unityVersion=").Append(Application.unityVersion).Append('\n');
            sb.Append("scenario=in-editor").Append('\n');
            sb.Append("evidenceSubdir=").Append(subdir).Append('\n');
            sb.Append("envAfter=").Append(envAfter).Append('\n');

            string path = Path.Combine(dir, "InEditorResult.txt");
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            Debug.Log("[QA][INEDITOR] result " + path.Replace('\\', '/') + "; " + sb.ToString().Replace("\n", " "));
        }
        catch (Exception e)
        {
            Debug.LogError("[QA][INEDITOR] write result failed task=" + task + "; err=" + e);
            ClearSuiteEnvironment();
        }
    }

    /// <summary>
    /// 进 Play 前的硬闸门：播放态与脏场景仍然直接拒绝（不代替所有者保存）。
    /// 编译中/资产刷新中**不再拒绝**——由 preflight 轮询等待（R-1 / AC-1）。
    /// </summary>
    static bool GateForLaunch(string task)
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Refuse(task, "playmode:isPlaying=" + EditorApplication.isPlaying +
                         ";isPlayingOrWillChange=" + EditorApplication.isPlayingOrWillChangePlaymode);
            return false;
        }

        // 打开场景（OpenSceneMode.Single）遇到脏场景会弹保存对话框卡住，故在此拒绝，不代替所有者保存。
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.isDirty)
        {
            Refuse(task, "dirtyScene=" + (string.IsNullOrEmpty(scene.path) ? scene.name : scene.path));
            return false;
        }

        return true;
    }

    static void Refuse(string task, string reason)
    {
        Debug.LogWarning("[QA][INEDITOR] refuse task=" + task + " reason=" + reason);
    }

    static void ResetFlags()
    {
        SessionState.SetBool(SessionActiveKey, false);
        SessionState.SetString(SessionTaskKey, string.Empty);
    }

    /// <summary>
    /// 进 Play 前记录 runInBackground 原值（SessionState 跨域重载存活），再打开后台运行。
    /// 编辑器里的赋值会写穿到 PlayerSettings，原值必须留到收尾还原。
    /// </summary>
    static void ApplyRunInBackground()
    {
        bool prevRunInBackground = Application.runInBackground;
        SessionState.SetBool(SessionRunInBackgroundKey, prevRunInBackground);
        SessionState.SetBool(SessionRunInBackgroundArmedKey, true);
        Application.runInBackground = true;
    }

    /// <summary>
    /// 还原 PlayerSettings 的 runInBackground：只有原本为 false 时才置回 false，
    /// 避免把用户本来打开的设置关掉。靠 Armed 标记保证幂等（重复调用不二次回滚）。
    /// </summary>
    static void RestoreRunInBackground()
    {
        if (!SessionState.GetBool(SessionRunInBackgroundArmedKey, false)) return;

        if (SessionState.GetBool(SessionRunInBackgroundKey, false) == false)
            Application.runInBackground = false;

        SessionState.EraseBool(SessionRunInBackgroundKey);
        SessionState.EraseBool(SessionRunInBackgroundArmedKey);
    }

    static void ClearSuiteEnvironment()
    {
        foreach (string key in SuiteEnvKeys)
        {
            Environment.SetEnvironmentVariable(key, null);
        }
    }

    /// <summary>7 个套件环境变量的当前值快照（null 记为 &lt;null&gt;）。</summary>
    static string EnvironmentSnapshot()
    {
        var sb = new StringBuilder("{");
        for (int i = 0; i < SuiteEnvKeys.Length; i++)
        {
            if (i > 0) sb.Append(";");
            string v = Environment.GetEnvironmentVariable(SuiteEnvKeys[i]);
            sb.Append(SuiteEnvKeys[i]).Append("=").Append(string.IsNullOrEmpty(v) ? "<null>" : v);
        }
        sb.Append("}");
        return sb.ToString();
    }

    static string EvidenceRoot(string task)
    {
        string folder;
        if (task == "SPEC01") folder = "SPEC-01";
        else if (task == "FND00B06") folder = "FND-00B-06";
        else folder = "DIAG-SKILL";

        string root = Directory.GetParent(Application.dataPath).FullName;
        return Path.Combine(root, "Docs", "QA", "Evidence", folder);
    }

    /// <summary>
    /// 取下一个证据子目录名：扫描已有 editor_run&lt;N&gt;，返回 editor_run&lt;N+1&gt;（只读扫描，不动历史证据）。
    /// 首次运行无历史目录时返回 editor_run1；扫描失败回退 DefaultSubdir。
    /// </summary>
    public static string NextSubdir(string task)
    {
        int next = 1;
        try
        {
            string root = EvidenceRoot(task);
            if (Directory.Exists(root))
            {
                foreach (string dir in Directory.GetDirectories(root, "editor_run*"))
                {
                    int n;
                    if (int.TryParse(Path.GetFileName(dir).Substring("editor_run".Length), out n) && n >= next)
                    {
                        next = n + 1;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[QA][INEDITOR] subdir scan failed; fallback=" + DefaultSubdir + "; err=" + e.Message);
            return DefaultSubdir;
        }
        return "editor_run" + next;
    }
}
