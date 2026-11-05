using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// REFACTOR 套件的结构化断言与证据写入（本套件自带，不依赖 SPEC01_Json）。
/// 证据根目录：Docs/QA/Evidence/REFACTOR-&lt;milestone&gt;/
///   milestone 由环境变量 REFACTOR_MILESTONE 决定（缺省 M0），由 REFACTOR_BatchAcceptance 注入。
///   QA_EVIDENCE_SUBDIR 存在时（编辑器内菜单入口）再套一层 editor_runN，不覆盖历史证据。
/// run&lt;=1 覆盖断言文件，run&gt;=2 追加；REFACTOR_Runs.jsonl 作为跨 run 索引。
/// 只写 Docs 证据，不修改任何生产资产。
/// </summary>
public class REFACTOR_Evidence
{
    public readonly int Run;
    public readonly string Milestone;
    public readonly DateTime StartedAt = DateTime.Now;
    public readonly List<string> Records = new List<string>();

    public int Pass;
    public int Fail;
    public int Blocked;
    public int Infra;
    public int Observed;

    readonly string _dir;
    readonly string _assertPath;

    public REFACTOR_Evidence(int run)
    {
        Run = run;
        string m = Environment.GetEnvironmentVariable("REFACTOR_MILESTONE");
        Milestone = string.IsNullOrEmpty(m) ? "M0" : m.Trim();

        string root = Directory.GetParent(Application.dataPath).FullName;
        _dir = Path.Combine(root, "Docs", "QA", "Evidence", "REFACTOR-" + Milestone);
        string subdir = Environment.GetEnvironmentVariable("QA_EVIDENCE_SUBDIR");
        if (!string.IsNullOrEmpty(subdir)) _dir = Path.Combine(_dir, subdir.Trim());
        Directory.CreateDirectory(_dir);

        _assertPath = Path.Combine(_dir, "REFACTOR_Assertions.jsonl");
        if (Run <= 1)
        {
            try { if (File.Exists(_assertPath)) File.Delete(_assertPath); }
            catch (Exception e) { Debug.LogWarning("[REFACTOR] 清空断言文件失败: " + e.Message); }
        }
    }

    public string EvidenceDir { get { return _dir; } }
    public string AssertionPath { get { return _assertPath; } }

    /// <param name="kind">production = 产品断言；infra = 测试基础设施</param>
    /// <param name="status">pass / product_fail / blocked / observed / infra_fail</param>
    public void Record(string qaItem, string assertion, object expected, object actual,
                       bool pass, string status, string kind, Dictionary<string, object> data)
    {
        string line = BuildLine(qaItem, assertion, expected, actual, pass, status, kind, data);
        Records.Add(line);

        if (status == "infra_fail") Infra++;
        else if (status == "blocked") Blocked++;
        else if (status == "observed") Observed++;
        else if (pass) Pass++;
        else Fail++;

        try { File.AppendAllText(_assertPath, line + "\n", new UTF8Encoding(false)); }
        catch (Exception e) { Debug.LogError("[REFACTOR] 断言写入失败: " + e); }

        Debug.Log("[REFACTOR][ASSERT] " + line);
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

    public string SummaryPath()
    {
        return Path.Combine(_dir, "REFACTOR_Summary_run" + Run + ".json");
    }

    public string ExitCodePath()
    {
        return Path.Combine(_dir, "REFACTOR_ExitCode_run" + Run + ".txt");
    }

    public string RunIndexPath()
    {
        return Path.Combine(_dir, "REFACTOR_Runs.jsonl");
    }

    public void WriteSummary(string scenePath, int seed, string exitReason,
                             Dictionary<string, object> extra)
    {
        var sb = new StringBuilder();
        sb.Append("{\n");
        sb.Append("  \"task\": \"REFACTOR\",\n");
        sb.Append("  \"milestone\": ").Append(REFACTOR_Json.Value(Milestone)).Append(",\n");
        sb.Append("  \"run\": ").Append(Run).Append(",\n");
        sb.Append("  \"startedAt\": ").Append(REFACTOR_Json.Value(StartedAt.ToString("yyyy-MM-dd HH:mm:ss.fff"))).Append(",\n");
        sb.Append("  \"finishedAt\": ").Append(REFACTOR_Json.Value(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))).Append(",\n");
        sb.Append("  \"unityVersion\": ").Append(REFACTOR_Json.Value(Application.unityVersion)).Append(",\n");
        sb.Append("  \"scene\": ").Append(REFACTOR_Json.Value(scenePath)).Append(",\n");
        sb.Append("  \"randomSeed\": ").Append(seed).Append(",\n");
        sb.Append("  \"assertionFile\": ").Append(REFACTOR_Json.Value(_assertPath)).Append(",\n");
        sb.Append("  \"pass\": ").Append(Pass).Append(",\n");
        sb.Append("  \"fail\": ").Append(Fail).Append(",\n");
        sb.Append("  \"blocked\": ").Append(Blocked).Append(",\n");
        sb.Append("  \"infraFail\": ").Append(Infra).Append(",\n");
        sb.Append("  \"observed\": ").Append(Observed).Append(",\n");
        if (extra != null)
        {
            foreach (var kv in extra)
                sb.Append("  ").Append(REFACTOR_Json.Value(kv.Key)).Append(": ").Append(REFACTOR_Json.Value(kv.Value)).Append(",\n");
        }
        sb.Append("  \"exitReason\": ").Append(REFACTOR_Json.Value(exitReason)).Append("\n");
        sb.Append("}\n");

        string path = SummaryPath();
        try { File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false)); }
        catch (Exception e) { Debug.LogError("[REFACTOR] 汇总写入失败: " + e); }
        Debug.Log("[REFACTOR][SUMMARY] " + sb.ToString().Replace("\n", " "));

        AppendRunIndex(path, exitReason);
    }

    void AppendRunIndex(string summaryPath, string exitReason)
    {
        try
        {
            var line = new Dictionary<string, object>
            {
                { "milestone", Milestone },
                { "run", Run },
                { "pass", Pass },
                { "fail", Fail },
                { "blocked", Blocked },
                { "infraFail", Infra },
                { "observed", Observed },
                { "summary", summaryPath },
                { "exitReason", exitReason },
                { "finishedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") }
            };
            File.AppendAllText(RunIndexPath(), REFACTOR_Json.Value(line) + "\n", new UTF8Encoding(false));
        }
        catch (Exception e) { Debug.LogWarning("[REFACTOR] run 索引写入失败: " + e.Message); }
    }

    string BuildLine(string qaItem, string assertion, object expected, object actual,
                     bool pass, string status, string kind, Dictionary<string, object> data)
    {
        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append("\"run\":").Append(Run).Append(",");
        sb.Append("\"milestone\":").Append(REFACTOR_Json.Value(Milestone)).Append(",");
        sb.Append("\"qaItem\":").Append(REFACTOR_Json.Value(qaItem)).Append(",");
        sb.Append("\"assertion\":").Append(REFACTOR_Json.Value(assertion)).Append(",");
        sb.Append("\"expected\":").Append(REFACTOR_Json.Value(expected)).Append(",");
        sb.Append("\"actual\":").Append(REFACTOR_Json.Value(actual)).Append(",");
        sb.Append("\"pass\":").Append(pass ? "true" : "false").Append(",");
        sb.Append("\"status\":").Append(REFACTOR_Json.Value(status)).Append(",");
        sb.Append("\"kind\":").Append(REFACTOR_Json.Value(kind)).Append(",");
        sb.Append("\"timestamp\":").Append(REFACTOR_Json.Value(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))).Append(",");
        sb.Append("\"data\":").Append(REFACTOR_Json.Value(data));
        sb.Append("}");
        return sb.ToString();
    }
}

/// <summary>REFACTOR 套件自带的极简 JSON 序列化（只覆盖断言所需类型）。</summary>
public static class REFACTOR_Json
{
    public static string Value(object v)
    {
        if (v == null) return "null";
        if (v is string s) return Quote(s);
        if (v is bool b) return b ? "true" : "false";
        if (v is int i) return i.ToString(CultureInfo.InvariantCulture);
        if (v is long l) return l.ToString(CultureInfo.InvariantCulture);
        if (v is short s16) return s16.ToString(CultureInfo.InvariantCulture);
        if (v is byte b8) return b8.ToString(CultureInfo.InvariantCulture);
        if (v is float f) return Number(f);
        if (v is double d) return Number(d);
        if (v is decimal m) return m.ToString(CultureInfo.InvariantCulture);
        if (v is Enum e) return Quote(e.ToString());
        if (v is Vector2 v2) return "{\"x\":" + Number(v2.x) + ",\"y\":" + Number(v2.y) + "}";
        if (v is Vector3 v3) return "{\"x\":" + Number(v3.x) + ",\"y\":" + Number(v3.y) + ",\"z\":" + Number(v3.z) + "}";
        if (v is IDictionary<string, object> dict)
        {
            var sb = new StringBuilder("{");
            bool first = true;
            foreach (var kv in dict)
            {
                if (!first) sb.Append(",");
                first = false;
                sb.Append(Quote(kv.Key)).Append(":").Append(Value(kv.Value));
            }
            sb.Append("}");
            return sb.ToString();
        }
        if (v is IEnumerable list)
        {
            var sb = new StringBuilder("[");
            bool first = true;
            foreach (var item in list)
            {
                if (!first) sb.Append(",");
                first = false;
                sb.Append(Value(item));
            }
            sb.Append("]");
            return sb.ToString();
        }
        return Quote(v.ToString());
    }

    static string Number(float f)
    {
        if (float.IsNaN(f) || float.IsInfinity(f)) return "null";
        return f.ToString("R", CultureInfo.InvariantCulture);
    }

    static string Number(double d)
    {
        if (double.IsNaN(d) || double.IsInfinity(d)) return "null";
        return d.ToString("R", CultureInfo.InvariantCulture);
    }

    static string Quote(string s)
    {
        var sb = new StringBuilder("\"");
        foreach (char c in s)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else sb.Append(c);
                    break;
            }
        }
        sb.Append("\"");
        return sb.ToString();
    }
}
