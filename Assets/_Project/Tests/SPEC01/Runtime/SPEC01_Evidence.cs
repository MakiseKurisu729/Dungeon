using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// SPEC-01 结构化断言与证据写入。
/// red：Docs/QA/Evidence/SPEC-01/SPEC01_Red_Assertions.jsonl（每次覆盖）
/// green run1：SPEC01_Assertions.jsonl（覆盖）；run2/run3：追加，靠 run 字段区分
/// 另追加 SPEC01_Runs.jsonl 作为跨 run 汇总索引。
/// 只写 Docs 证据，不修改任何生产资产。
/// 环境变量 SPEC01_EVIDENCE_TAG（如 fix）：文件名插入 _fix 后缀，避免覆盖原始证据；
/// 未设置时命名与上述完全一致。
/// </summary>
public class SPEC01_Evidence
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

    readonly string _dir;
    readonly string _assertPath;
    readonly string _prefix;
    readonly string _tag;

    public SPEC01_Evidence(int run, string mode)
    {
        Run = run;
        Mode = mode;
        string root = Directory.GetParent(Application.dataPath).FullName;
        _dir = Path.Combine(root, "Docs", "QA", "Evidence", "SPEC-01");
        string subdir = Environment.GetEnvironmentVariable("QA_EVIDENCE_SUBDIR");
        if (!string.IsNullOrEmpty(subdir)) _dir = Path.Combine(_dir, subdir.Trim());
        Directory.CreateDirectory(_dir);

        string tag = Environment.GetEnvironmentVariable("SPEC01_EVIDENCE_TAG");
        _tag = string.IsNullOrEmpty(tag) ? "" : "_" + tag.Trim();

        if (mode == "red")
        {
            _prefix = "SPEC01_Red" + _tag + "_";
            _assertPath = Path.Combine(_dir, _prefix + "Assertions.jsonl");
        }
        else if (_tag.Length > 0)
        {
            _prefix = "SPEC01_Green" + _tag + "_run" + run + "_";
            _assertPath = Path.Combine(_dir, _prefix + "Assertions.jsonl");
        }
        else
        {
            _prefix = "SPEC01_";
            _assertPath = Path.Combine(_dir, "SPEC01_Assertions.jsonl");
        }

        if (mode == "red" || Run <= 1)
        {
            try { if (File.Exists(_assertPath)) File.Delete(_assertPath); }
            catch (Exception e) { Debug.LogWarning("[SPEC01] 清空断言文件失败: " + e.Message); }
        }
    }

    public string EvidenceDir { get { return _dir; } }
    public string AssertionPath { get { return _assertPath; } }
    public string Prefix { get { return _prefix; } }

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
        catch (Exception e) { Debug.LogError("[SPEC01] 断言写入失败: " + e); }

        Debug.Log("[SPEC01][ASSERT] " + line);
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
            return File.ReadAllLines(_assertPath).Length.ToString(CultureInfo.InvariantCulture);
        }
        catch { return "unknown"; }
    }

    public string SummaryPath(string scenePath)
    {
        if (Mode == "red" || _tag.Length > 0)
            return Path.Combine(_dir, _prefix + "Summary.json");
        return Path.Combine(_dir, "SPEC01_Summary_run" + Run + ".json");
    }

    public string ExitCodePath()
    {
        if (Mode == "red" || _tag.Length > 0)
            return Path.Combine(_dir, _prefix + "ExitCode.txt");
        return Path.Combine(_dir, "SPEC01_ExitCode_run" + Run + ".txt");
    }

    public string RunIndexPath()
    {
        return Path.Combine(_dir, "SPEC01_Runs" + _tag + ".jsonl");
    }

    public void WriteSummary(string scenePath, int seed, string exitReason,
                             Dictionary<string, object> extra)
    {
        var sb = new StringBuilder();
        sb.Append("{\n");
        sb.Append("  \"task\": \"SPEC-01\",\n");
        sb.Append("  \"mode\": ").Append(SPEC01_Json.Value(Mode)).Append(",\n");
        sb.Append("  \"run\": ").Append(Run).Append(",\n");
        sb.Append("  \"startedAt\": ").Append(SPEC01_Json.Value(StartedAt.ToString("yyyy-MM-dd HH:mm:ss.fff"))).Append(",\n");
        sb.Append("  \"finishedAt\": ").Append(SPEC01_Json.Value(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))).Append(",\n");
        sb.Append("  \"unityVersion\": ").Append(SPEC01_Json.Value(Application.unityVersion)).Append(",\n");
        sb.Append("  \"scene\": ").Append(SPEC01_Json.Value(scenePath)).Append(",\n");
        sb.Append("  \"randomSeed\": ").Append(seed).Append(",\n");
        sb.Append("  \"assertionFile\": ").Append(SPEC01_Json.Value(_assertPath)).Append(",\n");
        sb.Append("  \"assertionLinesInFile\": ").Append(SPEC01_Json.Value(LineCount())).Append(",\n");
        sb.Append("  \"pass\": ").Append(Pass).Append(",\n");
        sb.Append("  \"fail\": ").Append(Fail).Append(",\n");
        sb.Append("  \"blocked\": ").Append(Blocked).Append(",\n");
        sb.Append("  \"infraFail\": ").Append(Infra).Append(",\n");
        sb.Append("  \"observed\": ").Append(Observed).Append(",\n");
        if (extra != null)
        {
            foreach (var kv in extra)
                sb.Append("  ").Append(SPEC01_Json.Value(kv.Key)).Append(": ").Append(SPEC01_Json.Value(kv.Value)).Append(",\n");
        }
        sb.Append("  \"exitReason\": ").Append(SPEC01_Json.Value(exitReason)).Append("\n");
        sb.Append("}\n");

        string path = SummaryPath(scenePath);
        try { File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false)); }
        catch (Exception e) { Debug.LogError("[SPEC01] 汇总写入失败: " + e); }
        Debug.Log("[SPEC01][SUMMARY] " + sb.ToString().Replace("\n", " "));

        AppendRunIndex(path, exitReason);
    }

    void AppendRunIndex(string summaryPath, string exitReason)
    {
        try
        {
            var line = new Dictionary<string, object>
            {
                { "mode", Mode },
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
            File.AppendAllText(RunIndexPath(), SPEC01_Json.Value(line) + "\n", new UTF8Encoding(false));
        }
        catch (Exception e) { Debug.LogWarning("[SPEC01] run 索引写入失败: " + e.Message); }
    }

    string BuildLine(string qaItem, string assertion, object expected, object actual,
                     bool pass, string status, string kind, Dictionary<string, object> data)
    {
        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append("\"run\":").Append(Run).Append(",");
        sb.Append("\"qaItem\":").Append(SPEC01_Json.Value(qaItem)).Append(",");
        sb.Append("\"assertion\":").Append(SPEC01_Json.Value(assertion)).Append(",");
        sb.Append("\"expected\":").Append(SPEC01_Json.Value(expected)).Append(",");
        sb.Append("\"actual\":").Append(SPEC01_Json.Value(actual)).Append(",");
        sb.Append("\"pass\":").Append(pass ? "true" : "false").Append(",");
        sb.Append("\"status\":").Append(SPEC01_Json.Value(status)).Append(",");
        sb.Append("\"kind\":").Append(SPEC01_Json.Value(kind)).Append(",");
        sb.Append("\"timestamp\":").Append(SPEC01_Json.Value(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))).Append(",");
        sb.Append("\"data\":").Append(SPEC01_Json.Value(data));
        sb.Append("}");
        return sb.ToString();
    }
}

/// <summary>极简 JSON 序列化（仅覆盖断言所需类型），避免依赖第三方库。</summary>
public static class SPEC01_Json
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
