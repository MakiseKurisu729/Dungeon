using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// FND-00B-05 结构化断言与证据写入。
/// 每条断言输出一行 JSON 到 Docs/QA/Evidence/FND-00B-05/05_Assertions.jsonl，
/// 字段：run / qaItem / assertion / expected / actual / pass / status / kind / timestamp / data。
/// run=1 时清空正式断言文件，run=2 追加；两次运行共用同一文件，靠 run 字段区分。
/// 只写 Docs 证据，不修改任何生产资产。
/// </summary>
public class FND00B05_Evidence
{
    public readonly int Run;
    public readonly DateTime StartedAt = DateTime.Now;
    public readonly List<string> Records = new List<string>();

    public int Pass;
    public int Fail;
    public int Blocked;
    public int Infra;
    public int FixturePass;
    public int FixtureFail;
    public int Observed;

    readonly string _dir;
    readonly string _assertPath;

    public FND00B05_Evidence(int run)
    {
        Run = run;
        string root = Directory.GetParent(Application.dataPath).FullName;
        _dir = Path.Combine(root, "Docs", "QA", "Evidence", "FND-00B-05");
        Directory.CreateDirectory(_dir);
        _assertPath = Path.Combine(_dir, "05_Assertions.jsonl");

        if (Run == 1)
        {
            try { if (File.Exists(_assertPath)) File.Delete(_assertPath); }
            catch (Exception e) { Debug.LogWarning("[FND00B05] 清空断言文件失败: " + e.Message); }
        }
    }

    public string EvidenceDir { get { return _dir; } }
    public string AssertionPath { get { return _assertPath; } }

    /// <param name="kind">production = 产品断言；fixture_probe = 夹具参考探针；infra = 测试基础设施</param>
    /// <param name="status">pass / product_fail / blocked / observed / observed_no_entry / observed_unconfirmed / fixture_pass / fixture_fail / infra_fail</param>
    public void Record(string qaItem, string assertion, object expected, object actual,
                       bool pass, string status, string kind, Dictionary<string, object> data)
    {
        string line = BuildLine(qaItem, assertion, expected, actual, pass, status, kind, data);
        Records.Add(line);

        if (kind == "fixture_probe")
        {
            if (pass) FixturePass++; else FixtureFail++;
        }
        else if (status == "infra_fail")
        {
            Infra++;
        }
        else if (status == "blocked")
        {
            Blocked++;
        }
        else if (status == "observed" || status == "observed_no_entry" || status == "observed_unconfirmed")
        {
            Observed++;
        }
        else if (pass)
        {
            Pass++;
        }
        else
        {
            Fail++;
        }

        try
        {
            File.AppendAllText(_assertPath, line + "\n", new UTF8Encoding(false));
        }
        catch (Exception e)
        {
            Debug.LogError("[FND00B05] 断言写入失败: " + e);
        }
        Debug.Log("[FND00B05][ASSERT] " + line);
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
               new Dictionary<string, object> { { "stack", stack } });
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

    public void WriteSummary(string scenePath, int seed, string exitReason)
    {
        var sb = new StringBuilder();
        sb.Append("{\n");
        sb.Append("  \"run\": ").Append(Run).Append(",\n");
        sb.Append("  \"task\": \"FND-00B-05\",\n");
        sb.Append("  \"startedAt\": ").Append(FND00B05_Json.Value(StartedAt.ToString("yyyy-MM-dd HH:mm:ss.fff"))).Append(",\n");
        sb.Append("  \"finishedAt\": ").Append(FND00B05_Json.Value(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))).Append(",\n");
        sb.Append("  \"unityVersion\": ").Append(FND00B05_Json.Value(Application.unityVersion)).Append(",\n");
        sb.Append("  \"scene\": ").Append(FND00B05_Json.Value(scenePath)).Append(",\n");
        sb.Append("  \"randomSeed\": ").Append(seed).Append(",\n");
        sb.Append("  \"assertionFile\": ").Append(FND00B05_Json.Value(_assertPath)).Append(",\n");
        sb.Append("  \"assertionLinesInFile\": ").Append(FND00B05_Json.Value(LineCount())).Append(",\n");
        sb.Append("  \"pass\": ").Append(Pass).Append(",\n");
        sb.Append("  \"fail\": ").Append(Fail).Append(",\n");
        sb.Append("  \"blocked\": ").Append(Blocked).Append(",\n");
        sb.Append("  \"infraFail\": ").Append(Infra).Append(",\n");
        sb.Append("  \"observed\": ").Append(Observed).Append(",\n");
        sb.Append("  \"fixtureProbePass\": ").Append(FixturePass).Append(",\n");
        sb.Append("  \"fixtureProbeFail\": ").Append(FixtureFail).Append(",\n");
        sb.Append("  \"exitReason\": ").Append(FND00B05_Json.Value(exitReason)).Append("\n");
        sb.Append("}\n");

        string path = Path.Combine(_dir, "05_Summary_run" + Run + ".json");
        try { File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false)); }
        catch (Exception e) { Debug.LogError("[FND00B05] 汇总写入失败: " + e); }
        Debug.Log("[FND00B05][SUMMARY] " + sb.ToString().Replace("\n", " "));
    }

    string BuildLine(string qaItem, string assertion, object expected, object actual,
                     bool pass, string status, string kind, Dictionary<string, object> data)
    {
        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append("\"run\":").Append(Run).Append(",");
        sb.Append("\"qaItem\":").Append(FND00B05_Json.Value(qaItem)).Append(",");
        sb.Append("\"assertion\":").Append(FND00B05_Json.Value(assertion)).Append(",");
        sb.Append("\"expected\":").Append(FND00B05_Json.Value(expected)).Append(",");
        sb.Append("\"actual\":").Append(FND00B05_Json.Value(actual)).Append(",");
        sb.Append("\"pass\":").Append(pass ? "true" : "false").Append(",");
        sb.Append("\"status\":").Append(FND00B05_Json.Value(status)).Append(",");
        sb.Append("\"kind\":").Append(FND00B05_Json.Value(kind)).Append(",");
        sb.Append("\"timestamp\":").Append(FND00B05_Json.Value(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))).Append(",");
        sb.Append("\"data\":").Append(FND00B05_Json.Value(data));
        sb.Append("}");
        return sb.ToString();
    }
}

/// <summary>极简 JSON 序列化（仅覆盖断言所需类型），避免依赖第三方库。</summary>
public static class FND00B05_Json
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

    static string Number(double d)
    {
        if (double.IsNaN(d) || double.IsInfinity(d)) return Quote(d.ToString(CultureInfo.InvariantCulture));
        return Math.Round(d, 5).ToString("0.#####", CultureInfo.InvariantCulture);
    }

    static string Quote(string s)
    {
        var sb = new StringBuilder();
        sb.Append('"');
        foreach (char c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 32) sb.Append("\\u").Append(((int)c).ToString("x4"));
                    else sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
        return sb.ToString();
    }
}
