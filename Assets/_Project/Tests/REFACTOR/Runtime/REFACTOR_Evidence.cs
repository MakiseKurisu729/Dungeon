using System.Collections.Generic;

/// <summary>
/// REFACTOR 证据契约适配器：只组装 <see cref="QaEvidenceSpec"/>；落盘/计分/Summary/Runs 索引全部由 QaEvidence 完成。
/// 证据目录 Docs/QA/Evidence/REFACTOR-&lt;milestone&gt;/（milestone 取 REFACTOR_MILESTONE，缺省 M0）；run&lt;=1 覆盖断言文件；
/// 断言行与 Summary 在 run 之后多一个 milestone 字段，靠 <c>PrefixedFields</c> 原样保留（键序逐字符一致）。
/// </summary>
public class REFACTOR_Evidence : QaEvidence
{
    public readonly string Milestone;
    public REFACTOR_Evidence(int run) : this(run, MilestoneOf()) { }
    REFACTOR_Evidence(int run, string milestone) : base(Spec(milestone), run, null) { Milestone = milestone; }
    static string MilestoneOf()
    {
        string m = System.Environment.GetEnvironmentVariable("REFACTOR_MILESTONE");
        return string.IsNullOrEmpty(m) ? "M0" : m.Trim();
    }
    static QaEvidenceSpec Spec(string milestone) { return new QaEvidenceSpec
    {
        Task = "REFACTOR",
        LogTag = "REFACTOR",
        EvidenceFolder = "REFACTOR-" + milestone,
        Prefix = "REFACTOR_",
        AssertionFileName = "{prefix}Assertions.jsonl",
        SummaryFileName = "REFACTOR_Summary_run{run}.json",
        ExitCodeFileName = "REFACTOR_ExitCode_run{run}.txt",
        RunIndexFileName = "REFACTOR_Runs.jsonl",
        ShouldTruncateAssertions = (r, m) => r <= 1,
        SummaryFields = new[]
        {
            QaSummaryKey.Task, QaSummaryKey.PrefixedFields, QaSummaryKey.Run, QaSummaryKey.StartedAt,
            QaSummaryKey.FinishedAt, QaSummaryKey.UnityVersion, QaSummaryKey.Scene, QaSummaryKey.RandomSeed,
            QaSummaryKey.AssertionFile, QaSummaryKey.Pass, QaSummaryKey.Fail, QaSummaryKey.Blocked,
            QaSummaryKey.InfraFail, QaSummaryKey.Observed, QaSummaryKey.Extra, QaSummaryKey.ExitReason
        },
        PrefixedFields = new List<KeyValuePair<string, object>> { new KeyValuePair<string, object>("milestone", milestone) }
    }; }
}
