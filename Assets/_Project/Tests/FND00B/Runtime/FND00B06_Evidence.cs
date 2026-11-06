/// <summary>
/// FND-00B-06 证据契约适配器：只组装 <see cref="QaEvidenceSpec"/>；断言行/计分/Summary 全部由 QaEvidence 完成。
/// 证据目录 Docs/QA/Evidence/FND-00B-06/[QA_EVIDENCE_SUBDIR]；run=1 清空断言文件，run=2 追加；
/// 无 Runs 索引；Summary 键序与迁移前逐字符一致（run 在最前，qaItems 紧随 task）。
/// </summary>
public class FND00B06_Evidence : QaEvidence
{
    public FND00B06_Evidence(int run) : base(Spec(), run, null) { }

    static QaEvidenceSpec Spec()
    {
        return new QaEvidenceSpec
        {
            Task = "FND-00B-06",
            LogTag = "FND00B06",
            EvidenceFolder = "FND-00B-06",
            Prefix = "06_",
            AssertionFileName = "{prefix}Assertions.jsonl",
            SummaryFileName = "06_Summary_run{run}.json",
            ExitCodeFileName = "06_ExitCode_run{run}.txt",
            RunIndexFileName = null,
            ShouldTruncateAssertions = (r, m) => r == 1,
            SummaryFields = new[]
            {
                QaSummaryKey.Run, QaSummaryKey.Task, QaSummaryKey.Extra, QaSummaryKey.StartedAt,
                QaSummaryKey.FinishedAt, QaSummaryKey.UnityVersion, QaSummaryKey.Scene,
                QaSummaryKey.RandomSeed, QaSummaryKey.AssertionFile, QaSummaryKey.AssertionLinesInFile,
                QaSummaryKey.Pass, QaSummaryKey.Fail, QaSummaryKey.Blocked, QaSummaryKey.InfraFail,
                QaSummaryKey.Observed, QaSummaryKey.ExitReason
            }
        };
    }
}
