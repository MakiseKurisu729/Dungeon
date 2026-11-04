using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// FND-02B 结构化断言与证据写入。
/// red：Docs/QA/Evidence/FND-02B/02B_Red_Assertions.jsonl（每次覆盖）
/// green run1：02B_Assertions.jsonl（覆盖）；run2/run3：追加，靠 run 字段区分
/// 只写 Docs 证据，不修改任何生产资产。
/// </summary>
public class FND02B_Evidence
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

    public FND02B_Evidence(int run, string mode)
    {
        Run = run;
        Mode = mode;
        string root = Directory.GetParent(Application.dataPath).FullName;
        _dir = Path.Combine(root, "Docs", "QA", "Evidence", "FND-02B");
        Directory.CreateDirectory(_dir);

        if (mode == "red")
        {
            _prefix = "02B_Red_";
            _assertPath = Path.Combine(_dir, "02B_Red_Assertions.jsonl");
        }
        else
        {
            _prefix = "02B_";
            _assertPath = Path.Combine(_dir, "02B_Assertions.jsonl");
        }

        if (mode == "red" || Run <= 1)
        {
            try { if (File.Exists(_assertPath)) File.Delete(_assertPath); }
            catch (Exception e) { Debug.LogWarning("[FND02B] 清空断言文件失败: " + e.Message); }
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
        catch (Exception e) { Debug.LogError("[FND02B] 断言写入失败: " + e); }

        Debug.Log("[FND02B][ASSERT] " + line);
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
        return Path.Combine(_dir, _prefix + (Mode == "red" ? "Summary.json" : "Summary_run" + Run + ".json"));
    }

    public string ExitCodePath()
    {
        return Path.Combine(_dir, _prefix + (Mode == "red" ? "ExitCode.txt" : "ExitCode_run" + Run + ".txt"));
    }

    public void WriteSummary(string scenePath, int seed, string exitReason,
                             Dictionary<string, object> extra)
    {
        var sb = new StringBuilder();
        sb.Append("{\n");
        sb.Append("  \"task\": \"FND-02B\",\n");
        sb.Append("  \"mode\": ").Append(FND02B_Json.Value(Mode)).Append(",\n");
        sb.Append("  \"run\": ").Append(Run).Append(",\n");
        sb.Append("  \"startedAt\": ").Append(FND02B_Json.Value(StartedAt.ToString("yyyy-MM-dd HH:mm:ss.fff"))).Append(",\n");
        sb.Append("  \"finishedAt\": ").Append(FND02B_Json.Value(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))).Append(",\n");
        sb.Append("  \"unityVersion\": ").Append(FND02B_Json.Value(Application.unityVersion)).Append(",\n");
        sb.Append("  \"scene\": ").Append(FND02B_Json.Value(scenePath)).Append(",\n");
        sb.Append("  \"randomSeed\": ").Append(seed).Append(",\n");
        sb.Append("  \"assertionFile\": ").Append(FND02B_Json.Value(_assertPath)).Append(",\n");
        sb.Append("  \"assertionLinesInFile\": ").Append(FND02B_Json.Value(LineCount())).Append(",\n");
        sb.Append("  \"pass\": ").Append(Pass).Append(",\n");
        sb.Append("  \"fail\": ").Append(Fail).Append(",\n");
        sb.Append("  \"blocked\": ").Append(Blocked).Append(",\n");
        sb.Append("  \"infraFail\": ").Append(Infra).Append(",\n");
        sb.Append("  \"observed\": ").Append(Observed).Append(",\n");
        if (extra != null)
        {
            foreach (var kv in extra)
                sb.Append("  ").Append(FND02B_Json.Value(kv.Key)).Append(": ").Append(FND02B_Json.Value(kv.Value)).Append(",\n");
        }
        sb.Append("  \"exitReason\": ").Append(FND02B_Json.Value(exitReason)).Append("\n");
        sb.Append("}\n");

        string path = SummaryPath(scenePath);
        try { File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false)); }
        catch (Exception e) { Debug.LogError("[FND02B] 汇总写入失败: " + e); }
        Debug.Log("[FND02B][SUMMARY] " + sb.ToString().Replace("\n", " "));
    }

    string BuildLine(string qaItem, string assertion, object expected, object actual,
                     bool pass, string status, string kind, Dictionary<string, object> data)
    {
        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append("\"run\":").Append(Run).Append(",");
        sb.Append("\"qaItem\":").Append(FND02B_Json.Value(qaItem)).Append(",");
        sb.Append("\"assertion\":").Append(FND02B_Json.Value(assertion)).Append(",");
        sb.Append("\"expected\":").Append(FND02B_Json.Value(expected)).Append(",");
        sb.Append("\"actual\":").Append(FND02B_Json.Value(actual)).Append(",");
        sb.Append("\"pass\":").Append(pass ? "true" : "false").Append(",");
        sb.Append("\"status\":").Append(FND02B_Json.Value(status)).Append(",");
        sb.Append("\"kind\":").Append(FND02B_Json.Value(kind)).Append(",");
        sb.Append("\"timestamp\":").Append(FND02B_Json.Value(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))).Append(",");
        sb.Append("\"data\":").Append(FND02B_Json.Value(data));
        sb.Append("}");
        return sb.ToString();
    }
}

/// <summary>极简 JSON 序列化（仅覆盖断言所需类型），避免依赖第三方库。</summary>
public static class FND02B_Json
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
