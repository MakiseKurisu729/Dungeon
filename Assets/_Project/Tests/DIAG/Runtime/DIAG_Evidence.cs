using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// DIAG-SKILL 一次性探针的结构化证据写入（极简 JSONL）。
/// 输出目录：Docs/QA/Evidence/DIAG-SKILL/
///   - DIAG_Assertions.jsonl（每条断言一行；启动时清空）
///   - DIAG_Summary.json / DIAG_ExitCode.txt（收尾写）
/// 只写 Docs 证据，不修改任何生产资产。
/// JSON 序列化复用 SPEC01_Json（同程序集，纯工具类）。
/// </summary>
public class DIAG_Evidence
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

    public DIAG_Evidence(int run, string mode)
    {
        Run = run;
        Mode = mode;
        string root = Directory.GetParent(Application.dataPath).FullName;
        _dir = Path.Combine(root, "Docs", "QA", "Evidence", "DIAG-SKILL");
        string subdir = Environment.GetEnvironmentVariable("QA_EVIDENCE_SUBDIR");
        if (!string.IsNullOrEmpty(subdir)) _dir = Path.Combine(_dir, subdir.Trim());
        Directory.CreateDirectory(_dir);
        _assertPath = Path.Combine(_dir, "DIAG_Assertions.jsonl");
        try { if (File.Exists(_assertPath)) File.Delete(_assertPath); }
        catch (Exception e) { Debug.LogWarning("[DIAG] 清空断言文件失败: " + e.Message); }
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
        catch (Exception e) { Debug.LogError("[DIAG] 断言写入失败: " + e); }

        Debug.Log("[DIAG][ASSERT] " + line);
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

    public void WriteSummary(string scenePath, string exitReason, string conclusion, string head)
    {
        var sb = new StringBuilder();
        sb.Append("{\n");
        sb.Append("  \"task\": \"DIAG-SKILL\",\n");
        sb.Append("  \"mode\": ").Append(SPEC01_Json.Value(Mode)).Append(",\n");
        sb.Append("  \"run\": ").Append(Run).Append(",\n");
        sb.Append("  \"startedAt\": ").Append(SPEC01_Json.Value(StartedAt.ToString("yyyy-MM-dd HH:mm:ss.fff"))).Append(",\n");
        sb.Append("  \"finishedAt\": ").Append(SPEC01_Json.Value(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"))).Append(",\n");
        sb.Append("  \"unityVersion\": ").Append(SPEC01_Json.Value(Application.unityVersion)).Append(",\n");
        sb.Append("  \"scene\": ").Append(SPEC01_Json.Value(scenePath)).Append(",\n");
        sb.Append("  \"head\": ").Append(SPEC01_Json.Value(head)).Append(",\n");
        sb.Append("  \"assertionFile\": ").Append(SPEC01_Json.Value(_assertPath)).Append(",\n");
        sb.Append("  \"pass\": ").Append(Pass).Append(",\n");
        sb.Append("  \"fail\": ").Append(Fail).Append(",\n");
        sb.Append("  \"blocked\": ").Append(Blocked).Append(",\n");
        sb.Append("  \"infraFail\": ").Append(Infra).Append(",\n");
        sb.Append("  \"observed\": ").Append(Observed).Append(",\n");
        sb.Append("  \"conclusion\": ").Append(SPEC01_Json.Value(conclusion ?? "")).Append(",\n");
        sb.Append("  \"exitReason\": ").Append(SPEC01_Json.Value(exitReason)).Append("\n");
        sb.Append("}\n");

        string path = Path.Combine(_dir, "DIAG_Summary.json");
        try { File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false)); }
        catch (Exception e) { Debug.LogError("[DIAG] 汇总写入失败: " + e); }
        Debug.Log("[DIAG][SUMMARY] " + sb.ToString().Replace("\n", " "));
    }

    public string ExitCodePath()
    {
        return Path.Combine(_dir, "DIAG_ExitCode.txt");
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
