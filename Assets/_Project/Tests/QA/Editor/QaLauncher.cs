using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 一个套件的引导契约（纯数据，不含 I/O）：SessionState 键前缀、runner 类型与名字、日志前缀、
/// 证据一级目录（可选按里程碑分目录）、菜单驱动时要注入的环境变量、菜单调用的入口。
/// 新增套件 = 在本类注册表 <see cref="QaLauncher.Specs"/> 加一条 + 在 <see cref="QA_InEditorMenu"/>
/// 加一个 [MenuItem] 转发方法；preflight / 证据子目录递增 / TryFinish 收尾 / 看门狗一律不改。
/// </summary>
public sealed class QaLaunchSpec
{
    /// <summary>套件 ID（也是 QA_InEditorMenu.TryFinish 的 task 与 SessionState 键前缀去掉点）。</summary>
    public string SuiteId;

    /// <summary>日志前缀（输出形如 "[SPEC01] ..."）。</summary>
    public string LogTag;

    /// <summary>SessionState 键前缀，如 "SPEC01."；键名固定为 Active/Started/Finished/ExitCode/RunnerName/PlayStartedAt。</summary>
    public string KeyPrefix;

    /// <summary>Play 模式 runner 的 GameObject 名（RunnerAlive 靠 GameObject.Find(name) 找它）。</summary>
    public string RunnerName;

    /// <summary>runner 组件类型（DontDestroyOnLoad 后 AddComponent）。</summary>
    public Type RunnerType;

    /// <summary>证据一级目录名，如 "SPEC-01"（<see cref="MilestonePrefix"/> 非空时不使用）。</summary>
    public string EvidenceFolder;

    /// <summary>按里程碑分目录时的前缀（REFACTOR 的 "REFACTOR-"）；null = 目录名固定。</summary>
    public string MilestonePrefix;

    /// <summary>里程碑环境变量名；null = 本套件无里程碑。</summary>
    public string MilestoneEnvVar;

    /// <summary>里程碑缺省值；null = 无里程碑。</summary>
    public string DefaultMilestone;

    /// <summary>菜单驱动时注入的套件环境变量名（与 <see cref="EnvValues"/> 一一对应）。</summary>
    public string[] EnvKeys = new string[0];

    /// <summary>与 <see cref="EnvKeys"/> 对应的值。</summary>
    public string[] EnvValues = new string[0];

    /// <summary>菜单驱动时调用的入口（各套件 *_BatchAcceptance.Run；命令行 -executeMethod 走同一方法）。</summary>
    public Action Launch;

    public string Tag { get { return "[" + LogTag + "]"; } }
    public string ActiveKey { get { return KeyPrefix + "Active"; } }
    public string StartedKey { get { return KeyPrefix + "Started"; } }
    public string FinishedKey { get { return KeyPrefix + "Finished"; } }
    public string ExitCodeKey { get { return KeyPrefix + "ExitCode"; } }
    public string RunnerNameKey { get { return KeyPrefix + "RunnerName"; } }
    public string PlayStartedAtKey { get { return KeyPrefix + "PlayStartedAt"; } }

    /// <summary>证据一级目录名：有里程碑时按环境变量（缺省 <see cref="DefaultMilestone"/>）拼接，其余固定。</summary>
    public string EvidenceFolderName()
    {
        if (string.IsNullOrEmpty(MilestonePrefix)) return EvidenceFolder;
        string m = MilestoneEnvVar == null ? null : Environment.GetEnvironmentVariable(MilestoneEnvVar);
        if (string.IsNullOrEmpty(m)) m = DefaultMilestone;
        return MilestonePrefix + (m ?? string.Empty).Trim();
    }
}

/// <summary>
/// 四套守卫统一的 boot/launcher（阶段 E 由 SPEC01/REFACTOR/FND00B06/DIAG 四个 *_Boot 等价合并而来）：
/// SessionState 引导标记、Play 状态机、看门狗、退出分叉。
/// 行为逐条保留：①ClearStaleRunFlags ②首启宽限 10s ③runner 丢失宽限 3s
/// ④看门狗落 code=3 + reason=runner-lost-domain-reload 且编辑器内不杀 Unity
/// ⑤EnteredEditMode + Finished → ExitWithCode ⑥TryFinish 优先于 EditorApplication.Exit
/// ⑦RunnerAlive = 静态引用 + GameObject.Find(name) 双判定 ⑧runner DontDestroyOnLoad。
/// 不改菜单、不改证据子目录规则、不新增退出码。
/// </summary>
public static class QaLauncher
{
    /// <summary>看门狗：started=true 但 runner 丢失持续 3s → 判定本轮失败（域重载典型症状）。</summary>
    public const float RunnerLostGraceSeconds = 3f;

    /// <summary>看门狗：进 Play 后 10s 仍未创建 runner → 首启失败。</summary>
    public const float FirstStartGraceSeconds = 10f;

    static readonly QaLaunchSpec[] Registry =
    {
        new QaLaunchSpec
        {
            SuiteId = "SPEC01",
            LogTag = "SPEC01",
            KeyPrefix = "SPEC01.",
            RunnerName = "SPEC01_Runner",
            RunnerType = typeof(SPEC01_ProbeRunner),
            EvidenceFolder = "SPEC-01",
            EnvKeys = new[] { "SPEC01_MODE", "SPEC01_RUN" },
            EnvValues = new[] { "green", "1" },
            Launch = SPEC01_BatchAcceptance.Run
        },
        new QaLaunchSpec
        {
            SuiteId = "FND00B06",
            LogTag = "FND00B06",
            KeyPrefix = "FND00B06.",
            RunnerName = "FND00B06_Runner",
            RunnerType = typeof(FND00B06_ProbeRunner),
            EvidenceFolder = "FND-00B-06",
            EnvKeys = new[] { "FND00B06_RUN" },
            EnvValues = new[] { "1" },
            Launch = FND00B06_BatchAcceptance.Run
        },
        new QaLaunchSpec
        {
            SuiteId = "DIAG",
            LogTag = "DIAG",
            KeyPrefix = "DIAG.",
            RunnerName = "DIAG_Runner",
            RunnerType = typeof(DIAG_ProbeRunner),
            EvidenceFolder = "DIAG-SKILL",
            EnvKeys = new[] { "DIAG_MODE", "DIAG_RUN" },
            EnvValues = new[] { "probe", "1" },
            Launch = DIAG_BatchAcceptance.Run
        },
        new QaLaunchSpec
        {
            SuiteId = "REFACTOR",
            LogTag = "REFACTOR",
            KeyPrefix = "REFACTOR.",
            RunnerName = "REFACTOR_Runner",
            RunnerType = typeof(REFACTOR_ProbeRunner),
            MilestonePrefix = "REFACTOR-",
            MilestoneEnvVar = "REFACTOR_MILESTONE",
            DefaultMilestone = REFACTOR_BatchAcceptance.DefaultMilestone,
            EnvKeys = new[] { "REFACTOR_RUN" },
            EnvValues = new[] { "1" },
            Launch = REFACTOR_BatchAcceptance.Run
        }
    };

    /// <summary>本轮已解析出的套件（域重载后由 <see cref="Boot"/> 重新解析）。</summary>
    static QaLaunchSpec _current;

    // 下面两个字段是四个 *_Boot 各自 _runner/_missingSince 的等价迁移（原 4×2 个 → 现在 2 个）。
    static GameObject _runner;
    static float _missingSince = -1f;

    /// <summary>注册表（只读视图；顺序 = 菜单顺序）。</summary>
    public static QaLaunchSpec[] Specs { get { return Registry; } }

    public static QaLaunchSpec SPEC01 { get { return Registry[0]; } }
    public static QaLaunchSpec FND00B06 { get { return Registry[1]; } }
    public static QaLaunchSpec DIAG { get { return Registry[2]; } }
    public static QaLaunchSpec REFACTOR { get { return Registry[3]; } }

    /// <summary>按套件 ID 找 spec；未知 ID 返回 null。</summary>
    public static QaLaunchSpec Find(string suiteId)
    {
        for (int i = 0; i < Registry.Length; i++)
        {
            if (string.Equals(Registry[i].SuiteId, suiteId, StringComparison.Ordinal)) return Registry[i];
        }
        return null;
    }

    /// <summary>lancer 起始日志（原各 *_Boot 的 "[TAG] launcher start; pid=…; args=…"）。</summary>
    public static void LogStart(QaLaunchSpec spec)
    {
        Debug.Log(spec.Tag + " launcher start; pid=" + System.Diagnostics.Process.GetCurrentProcess().Id +
                  "; args=" + string.Join(" ", Environment.GetCommandLineArgs()));
    }

    /// <summary>
    /// 装配完成后的统一入口：写 Active/Started/Finished 标记并请求进入 Play。
    /// 场景打开、head/GUID 缓存等套件私有装配仍留在各 *_BatchAcceptance.Run()。
    /// </summary>
    public static void Begin(QaLaunchSpec spec)
    {
        if (spec == null) throw new ArgumentNullException("spec");

        SessionState.SetBool(spec.ActiveKey, true);
        SessionState.SetBool(spec.StartedKey, false);
        SessionState.SetBool(spec.FinishedKey, false);

        EditorApplication.EnterPlaymode();
        Debug.Log(spec.Tag + " EnterPlaymode requested");
    }

    /// <summary>启动失败的统一收场：编辑器内只记失败不退 Unity，batch 保持原有 Exit(3)。</summary>
    public static void Fail(QaLaunchSpec spec, Exception e)
    {
        Debug.LogError(spec.Tag + " launcher failed: " + e);
        if (QA_InEditorMenu.TryFinish(spec.SuiteId, 3)) return;
        EditorApplication.Exit(3);
    }

    /// <summary>当前 Active 标记成立的套件；没有则 null（等价于原各 *_Boot 的提前 return）。</summary>
    static QaLaunchSpec ActiveSpec()
    {
        for (int i = 0; i < Registry.Length; i++)
        {
            if (SessionState.GetBool(Registry[i].ActiveKey, false)) return Registry[i];
        }
        return null;
    }

    /// <summary>域重载后的唯一接续点：按 Active 标记重新布线（各 *_Boot 原为各自一个 InitializeOnLoadMethod）。</summary>
    [InitializeOnLoadMethod]
    static void Boot()
    {
        QaLaunchSpec spec = ActiveSpec();
        if (spec == null) return;

        _current = spec;

        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;

        Debug.Log(spec.Tag + " boot; isPlaying=" + Application.isPlaying +
                  "; started=" + SessionState.GetBool(spec.StartedKey, false) +
                  "; finished=" + SessionState.GetBool(spec.FinishedKey, false));

        _missingSince = -1f;
        if (Application.isPlaying) TryStartRunner();
    }

    static void OnPlayModeChanged(PlayModeStateChange change)
    {
        QaLaunchSpec spec = ActiveSpec();
        if (spec == null) return;
        _current = spec;

        Debug.Log(spec.Tag + " playModeStateChanged=" + change);
        if (change == PlayModeStateChange.EnteredPlayMode)
        {
            SessionState.SetFloat(spec.PlayStartedAtKey, Time.realtimeSinceStartup);
            if (ClearStaleRunFlags()) return;
            TryStartRunner();
        }
        else if (change == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(spec.FinishedKey, false))
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
        QaLaunchSpec spec = _current;
        if (spec == null) return false;

        if (!SessionState.GetBool(spec.StartedKey, false)) return false;
        if (RunnerAlive()) return false;

        Debug.LogWarning(spec.Tag + " stale run flags cleared (previous play session had no runner)");
        SessionState.SetBool(spec.StartedKey, false);
        SessionState.SetBool(spec.FinishedKey, false);
        SessionState.SetBool(spec.ActiveKey, false);
        SessionState.EraseString(spec.RunnerNameKey);
        _runner = null;
        _missingSince = -1f;
        return true;
    }

    static void Tick()
    {
        QaLaunchSpec spec = ActiveSpec();
        if (spec == null) return;
        _current = spec;

        if (SessionState.GetBool(spec.FinishedKey, false))
        {
            if (!Application.isPlaying) ExitWithCode();
            return;
        }

        if (!Application.isPlaying) return;

        float now = Time.realtimeSinceStartup;

        if (!SessionState.GetBool(spec.StartedKey, false))
        {
            float playStartedAt = SessionState.GetFloat(spec.PlayStartedAtKey, now);
            if (now - playStartedAt >= FirstStartGraceSeconds)
            {
                WatchdogFail("runner never created within " + FirstStartGraceSeconds + "s after entering play");
                return;
            }
            Debug.Log(spec.Tag + " tick fallback: isPlaying=true, starting runner");
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
            Debug.LogWarning(spec.Tag + "[WATCHDOG] runner missing; counting down " + RunnerLostGraceSeconds + "s");
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
        QaLaunchSpec spec = _current;
        if (spec == null) return true;
        string name = SessionState.GetString(spec.RunnerNameKey, string.Empty);
        if (string.IsNullOrEmpty(name)) return true;   // 还没记录名字，不误判
        return GameObject.Find(name) != null;
    }

    /// <summary>
    /// 写 code=3 + reason=runner-lost-domain-reload，然后退出 Play；编辑器内**不退出编辑器**。
    /// batch 路径（没有 QA.InEditor 标记）保持原有 Exit(3) 行为。
    /// </summary>
    static void WatchdogFail(string why)
    {
        QaLaunchSpec spec = _current;
        if (spec == null) return;

        Debug.LogError(spec.Tag + "[WATCHDOG] " + why + " -> code=3 reason=runner-lost-domain-reload");
        if (QA_InEditorMenu.TryFinish(spec.SuiteId, 3, "runner-lost-domain-reload"))
        {
            EditorApplication.isPlaying = false;
            Debug.Log(spec.Tag + "[WATCHDOG] left play mode; editor kept alive");
            return;
        }
        EditorApplication.Exit(3);
    }

    static void ExitWithCode()
    {
        QaLaunchSpec spec = _current;
        if (spec == null) return;

        int code = SessionState.GetInt(spec.ExitCodeKey, 3);
        SessionState.SetBool(spec.FinishedKey, false);
        SessionState.SetBool(spec.StartedKey, false);
        SessionState.SetBool(spec.ActiveKey, false);
        SessionState.EraseString(spec.RunnerNameKey);
        _runner = null;
        _missingSince = -1f;
        Debug.Log(spec.Tag + " batchmode exit " + code);
        if (QA_InEditorMenu.TryFinish(spec.SuiteId, code)) return;
        EditorApplication.Exit(code);
    }

    static void TryStartRunner()
    {
        QaLaunchSpec spec = _current;
        if (spec == null) return;
        if (SessionState.GetBool(spec.StartedKey, false)) return;
        SessionState.SetBool(spec.StartedKey, true);
        SessionState.SetFloat(spec.PlayStartedAtKey, Time.realtimeSinceStartup);

        var go = new GameObject(spec.RunnerName);
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent(spec.RunnerType);
        _runner = go;
        SessionState.SetString(spec.RunnerNameKey, go.name);
        _missingSince = -1f;
        Debug.Log(spec.Tag + " play-mode runner created");
    }
}
