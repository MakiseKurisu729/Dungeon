/// <summary>
/// DIAG-SKILL 证据契约适配器：只组装 <see cref="QaEvidenceSpec"/>；断言行/计分/Summary/ExitCode 全部由 QaEvidence 完成。
/// 证据目录 Docs/QA/Evidence/DIAG-SKILL/[QA_EVIDENCE_SUBDIR]；每次构造清空断言文件（现状：每轮覆盖）；
/// 无 Runs 索引；Summary 固定名 DIAG_Summary.json（无 run 后缀），键序与迁移前逐字符一致
/// （head 与 conclusion 是两个不相邻的套件私有键块，分别走 Extra 与 ExtraTail 槽）。
/// </summary>
public class DIAG_Evidence : QaEvidence
{
    public DIAG_Evidence(int run, string mode) : base(Spec(), run, mode) { }

    static QaEvidenceSpec Spec()
    {
        return new QaEvidenceSpec
        {
            Task = "DIAG-SKILL",
            LogTag = "DIAG",
            EvidenceFolder = "DIAG-SKILL",
            AssertionFileName = "DIAG_Assertions.jsonl",
            SummaryFileName = "DIAG_Summary.json",
            ExitCodeFileName = "DIAG_ExitCode.txt",
            RunIndexFileName = null,
            ShouldTruncateAssertions = (r, m) => true,
            SummaryFields = new[]
            {
                QaSummaryKey.Task, QaSummaryKey.Mode, QaSummaryKey.Run, QaSummaryKey.StartedAt,
                QaSummaryKey.FinishedAt, QaSummaryKey.UnityVersion, QaSummaryKey.Scene,
                QaSummaryKey.Extra, QaSummaryKey.AssertionFile,
                QaSummaryKey.Pass, QaSummaryKey.Fail, QaSummaryKey.Blocked,
                QaSummaryKey.InfraFail, QaSummaryKey.Observed,
                QaSummaryKey.ExtraTail, QaSummaryKey.ExitReason
            }
        };
    }
}
