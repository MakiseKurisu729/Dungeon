using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>Summary 的输出字段（键序由 <see cref="QaEvidenceSpec.SummaryFields"/> 逐套件保留）。</summary>
public enum QaSummaryKey
{
    Task,
    Mode,
    Run,
    StartedAt,
    FinishedAt,
    UnityVersion,
    Scene,
    RandomSeed,
    AssertionFile,
    AssertionLinesInFile,
    Pass,
    Fail,
    Blocked,
    InfraFail,
    Observed,
    /// <summary>就地展开 WriteSummary 传入的 extra 字典（插入序），用于套件私有键。</summary>
    Extra,
    ExitReason
}

/// <summary>
/// 一个套件的证据契约：目录、文件名模板、tag 变量、覆盖策略、Summary 键序。
/// 纯数据，不含任何 I/O；所有落盘与计分由 <see cref="QaEvidence"/> 完成。
/// 模板占位符：<c>{prefix}</c> 解析后的前缀、<c>{tag}</c> 带下划线的 tag（无 tag 为空串）、<c>{run}</c> 轮次。
/// </summary>
public sealed class QaEvidenceSpec
{
    /// <summary>Summary 的 task 值（也是日志标签的默认值）。</summary>
    public string Task;

    /// <summary>日志前缀（如 "SPEC01"）；null 时用 Task。</summary>
    public string LogTag;

    /// <summary>证据根下的一级目录名（如 "SPEC-01"）；null 表示直接写证据根。</summary>
    public string EvidenceFolder;

    /// <summary>额外的证据子目录环境变量名（编辑器内注入 editor_runN）；默认 QA_EVIDENCE_SUBDIR。</summary>
    public string SubdirEnvVar = "QA_EVIDENCE_SUBDIR";

    /// <summary>tag 环境变量名；null 表示本套件不用 tag。</summary>
    public string TagEnvVar;

    /// <summary>无 tag 时的文件名前缀模板。</summary>
    public string Prefix = "";

    /// <summary>有 tag 时的文件名前缀模板；null 表示沿用 <see cref="Prefix"/>。</summary>
    public string PrefixWithTag;

    /// <summary>断言 JSONL 文件名模板。</summary>
    public string AssertionFileName = "{prefix}Assertions.jsonl";

    /// <summary>Summary 文件名模板；null 表示本套件不生成 Summary。</summary>
    public string SummaryFileName;

    /// <summary>有 tag 时的 Summary 文件名模板；null 表示沿用 <see cref="SummaryFileName"/>。</summary>
    public string SummaryFileNameWithTag;

    /// <summary>ExitCode 文件名模板；null 表示本套件不生成 ExitCode。</summary>
    public string ExitCodeFileName;

    /// <summary>有 tag 时的 ExitCode 文件名模板；null 表示沿用 <see cref="ExitCodeFileName"/>。</summary>
    public string ExitCodeFileNameWithTag;

    /// <summary>Runs 索引文件名模板；null 表示本套件不生成 Runs 索引。</summary>
    public string RunIndexFileName;

    /// <summary>构造时是否清空断言文件：(run, mode) → true 则删除同轮断言文件。null = 从不清空。</summary>
    public Func<int, string, bool> ShouldTruncateAssertions;

    /// <summary>Summary 键序；null = <see cref="QaEvidence.DefaultSummaryFields"/>。</summary>
    public QaSummaryKey[] SummaryFields;
}

/// <summary>
/// 四套守卫共享的证据落盘层：断言 JSONL、计分、Summary、Runs 索引。
/// 只写 Docs 证据，不修改任何生产资产；不含任何套件名/目录名硬编码。
/// 断言行字段顺序固定：run, qaItem, assertion, expected, actual, pass, status, kind, timestamp, data。
/// </summary>
public class QaEvidence : IQaSink
{
    public readonly int Run;
    public readonly string Mode;
    public readonly DateTime StartedAt = DateTime.Now;
    public readonly List<string> Records = new List<string>();

    public int Pass;
    public int Fail;
    public int Blocked;
    public int Infra;
    public int Observed;

    static readonly QaSummaryKey[] DefaultSummaryFields =
    {
        QaSummaryKey.Task, QaSummaryKey.Mode, QaSummaryKey.Run, QaSummaryKey.StartedAt,
        QaSummaryKey.FinishedAt, QaSummaryKey.UnityVersion, QaSummaryKey.Scene,
        QaSummaryKey.RandomSeed, QaSummaryKey.AssertionFile, QaSummaryKey.AssertionLinesInFile,
        QaSummaryKey.Pass, QaSummaryKey.Fail, QaSummaryKey.Blocked, QaSummaryKey.InfraFail,
        QaSummaryKey.Observed, QaSummaryKey.Extra, QaSummaryKey.ExitReason
    };

    readonly QaEvidenceSpec _spec;
    readonly string _dir;
    readonly string _assertPath;
    readonly string _prefix;
    readonly string _tag;

    public QaEvidence(QaEvidenceSpec spec, int run, string mode)
    {
        if (spec == null) throw new ArgumentNullException("spec");
        _spec = spec;
        Run = run;
        Mode = mode;
        LogTag = spec.LogTag ?? spec.Task ?? "";

        string root = Directory.GetParent(Application.dataPath).FullName;
        string evidenceRoot = Path.Combine(root, "Docs", "QA", "Evidence");
        _dir = string.IsNullOrEmpty(spec.EvidenceFolder) ? evidenceRoot : Path.Combine(evidenceRoot, spec.EvidenceFolder);
        string subdir = ReadEnv(spec.SubdirEnvVar);
        if (subdir.Length > 0) _dir = Path.Combine(_dir, subdir);
        Directory.CreateDirectory(_dir);

        string tag = ReadEnv(spec.TagEnvVar);
        _tag = tag.Length == 0 ? "" : "_" + tag;

        string prefixTemplate = tag.Length > 0 && spec.PrefixWithTag != null ? spec.PrefixWithTag : spec.Prefix;
        _prefix = Resolve(prefixTemplate, "");

        _assertPath = Path.Combine(_dir, Resolve(spec.AssertionFileName, _prefix));

        if (spec.ShouldTruncateAssertions != null && spec.ShouldTruncateAssertions(run, mode))
        {
            try { if (File.Exists(_assertPath)) File.Delete(_assertPath); }
            catch (Exception e) { Debug.LogWarning("[" + LogTag + "] 清空断言文件失败: " + e.Message); }
        }
    }

    /// <summary>日志前缀（"SPEC01" 等）。</summary>
    public string LogTag { get; private set; }

    public string EvidenceDir { get { return _dir; } }
    public string AssertionPath { get { return _assertPath; } }
    public string Prefix { get { return _prefix; } }

    /// <param name="kind">production = 产品断言；infra = 测试基础设施</param>
    /// <param name="status">pass / product_fail / blocked / observed / infra_fail</param>
    public void Record(string qaItem, string assertion, object expected, object actual,
                       bool pass, string status, string kind, Dictionary<string, object> data)
    {
        string line = BuildAssertionLine(Run, qaItem, assertion, expected, actual, pass, status, kind, data);
        Records.Add(line);

        if (status == "infra_fail") Infra++;
        else if (status == "blocked") Blocked++;
        else if (status == "observed") Observed++;
        else if (pass) Pass++;
        else Fail++;

        try { File.AppendAllText(_assertPath, line + "\n", new UTF8Encoding(false)); }
        catch (Exception e) { Debug.LogError("[" + LogTag + "] 断言写入失败: " + e); }

        Debug.Log("[" + LogTag + "][ASSERT] " + line);
    }

    public void BlockedAssertion(string qaItem, string assertion, object expected, string reason,
                                 Dictionary<string, object> data)
    {
        var d = data ?? new Dictionary<string, object>();
        d["blockedReason"] = reason;
        Record(qaItem, assertion, expected, new Dictionary<string, object> { { "blocked", reason } },
               false, "blocked", "production", d);
    }

    public void InfraFailure(string qaItem, string assertion, string message, string stack)
    {
        Record(qaItem, assertion, "可执行", new Dictionary<string, object> { { "error", message } },
               false, "infra_fail", "infra",
               new Dictionary<string, object> { { "stack", stack ?? "" } });
    }

    public string LineCount()
    {
        try
        {
            if (!File.Exists(_assertPath)) return "0";
            return File.ReadAllLines(_assertPath).Length.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        catch { return "unknown"; }
    }

    /// <summary>Summary 落盘路径；本套件不生成 Summary 时返回 null。</summary>
    public string SummaryPath()
    {
        string name = ChooseName(_spec.SummaryFileName, _spec.SummaryFileNameWithTag);
        return name == null ? null : Path.Combine(_dir, Resolve(name, _prefix));
    }

    /// <summary>ExitCode 落盘路径；本套件不生成 ExitCode 时返回 null（调用方需判空）。</summary>
    public string ExitCodePath()
    {
        string name = ChooseName(_spec.ExitCodeFileName, _spec.ExitCodeFileNameWithTag);
        return name == null ? null : Path.Combine(_dir, Resolve(name, _prefix));
    }

    /// <summary>Runs 索引路径；本套件不生成索引时返回 null。</summary>
    public string RunIndexPath()
    {
        return _spec.RunIndexFileName == null ? null : Path.Combine(_dir, Resolve(_spec.RunIndexFileName, _prefix));
    }

    public void WriteSummary(string scenePath, int seed, string exitReason,
                             Dictionary<string, object> extra)
    {
        string path = SummaryPath();
        if (path == null) return;

        var pairs = new List<KeyValuePair<string, object>>();
        foreach (var key in _spec.SummaryFields ?? DefaultSummaryFields)
        {
            if (key == QaSummaryKey.Extra)
            {
                if (extra == null) continue;
                foreach (var kv in extra) pairs.Add(new KeyValuePair<string, object>(kv.Key, kv.Value));
                continue;
            }
            pairs.Add(new KeyValuePair<string, object>(KeyName(key), ValueOf(key, scenePath, seed, exitReason)));
        }

        var sb = new StringBuilder();
        sb.Append("{\n");
        for (int i = 0; i < pairs.Count; i++)
        {
            sb.Append("  ").Append(QaJson.Value(pairs[i].Key)).Append(": ").Append(QaJson.Value(pairs[i].Value));
            sb.Append(i == pairs.Count - 1 ? "\n" : ",\n");
        }
        sb.Append("}\n");

        try { File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false)); }
        catch (Exception e) { Debug.LogError("[" + LogTag + "] 汇总写入失败: " + e); }
        Debug.Log("[" + LogTag + "][SUMMARY] " + sb.ToString().Replace("\n", " "));

        AppendRunIndex(path, exitReason);
    }

    void AppendRunIndex(string summaryPath, string exitReason)
    {
        string path = RunIndexPath();
        if (path == null) return;

        try
        {
            var line = new Dictionary<string, object>();
            if (Mode != null) line["mode"] = Mode;
            line["run"] = Run;
            line["pass"] = Pass;
            line["fail"] = Fail;
            line["blocked"] = Blocked;
            line["infraFail"] = Infra;
            line["observed"] = Observed;
            line["summary"] = summaryPath;
            line["exitReason"] = exitReason;
            line["finishedAt"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            File.AppendAllText(path, QaJson.Value(line) + "\n", new UTF8Encoding(false));
        }
        catch (Exception e) { Debug.LogWarning("[" + LogTag + "] run 索引写入失败: " + e.Message); }
    }

    static string BuildAssertionLine(int run, string qaItem, string assertion, object expected, object actual,
                                     bool pass, string status, string kind, Dictionary<string, object> data)
    {
        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append("\"run\":").Append(run).Append(",");
        sb.Append("\"qaItem\":").Append(QaJson.Value(qaItem)).Append(",");
        sb.Append("\"assertion\":").Append(QaJson.Value(assertion)).Append(",");
        sb.Append("\"expected\":").Append(QaJson.Value(expected)).Append(",");
        sb.Append("\"actual\":").Append(QaJson.Value(actual)).Append(",");
        sb.Append("\"pass\":").Append(pass ? "true" : "false").Append(",");
        sb.Append("\"status\":").Append(QaJson.Value(status)).Append(",");
        sb.Append("\"kind\":").Append(QaJson.Value(kind)).Append(",");
        sb.Append("\"timestamp\":").Append(QaJson.Value(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))).Append(",");
        sb.Append("\"data\":").Append(QaJson.Value(data));
        sb.Append("}");
        return sb.ToString();
    }

    object ValueOf(QaSummaryKey key, string scenePath, int seed, string exitReason)
    {
        switch (key)
        {
            case QaSummaryKey.Task: return _spec.Task;
            case QaSummaryKey.Mode: return Mode;
            case QaSummaryKey.Run: return Run;
            case QaSummaryKey.StartedAt: return StartedAt.ToString("yyyy-MM-dd HH:mm:ss.fff");
            case QaSummaryKey.FinishedAt: return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            case QaSummaryKey.UnityVersion: return Application.unityVersion;
            case QaSummaryKey.Scene: return scenePath;
            case QaSummaryKey.RandomSeed: return seed;
            case QaSummaryKey.AssertionFile: return _assertPath;
            case QaSummaryKey.AssertionLinesInFile: return LineCount();
            case QaSummaryKey.Pass: return Pass;
            case QaSummaryKey.Fail: return Fail;
            case QaSummaryKey.Blocked: return Blocked;
            case QaSummaryKey.InfraFail: return Infra;
            case QaSummaryKey.Observed: return Observed;
            case QaSummaryKey.ExitReason: return exitReason;
        }
        return null;
    }

    static string KeyName(QaSummaryKey key)
    {
        switch (key)
        {
            case QaSummaryKey.Task: return "task";
            case QaSummaryKey.Mode: return "mode";
            case QaSummaryKey.Run: return "run";
            case QaSummaryKey.StartedAt: return "startedAt";
            case QaSummaryKey.FinishedAt: return "finishedAt";
            case QaSummaryKey.UnityVersion: return "unityVersion";
            case QaSummaryKey.Scene: return "scene";
            case QaSummaryKey.RandomSeed: return "randomSeed";
            case QaSummaryKey.AssertionFile: return "assertionFile";
            case QaSummaryKey.AssertionLinesInFile: return "assertionLinesInFile";
            case QaSummaryKey.Pass: return "pass";
            case QaSummaryKey.Fail: return "fail";
            case QaSummaryKey.Blocked: return "blocked";
            case QaSummaryKey.InfraFail: return "infraFail";
            case QaSummaryKey.Observed: return "observed";
            case QaSummaryKey.ExitReason: return "exitReason";
        }
        return null;
    }

    static string ReadEnv(string key)
    {
        if (key == null) return "";
        string v = Environment.GetEnvironmentVariable(key);
        return v == null ? "" : v.Trim();
    }

    string ChooseName(string plain, string withTag)
    {
        return _tag.Length > 0 && withTag != null ? withTag : plain;
    }

    string Resolve(string template, string prefix)
    {
        if (template == null) return null;
        return template
            .Replace("{prefix}", prefix ?? "")
            .Replace("{tag}", _tag)
            .Replace("{run}", Run.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}
