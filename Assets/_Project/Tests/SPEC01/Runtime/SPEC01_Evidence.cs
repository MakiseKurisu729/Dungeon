using System;

/// <summary>
/// SPEC-01 证据契约适配器：只组装 <see cref="QaEvidenceSpec"/>；落盘、计分、Summary、Runs 索引全部由 QaEvidence 完成。
/// 证据目录 Docs/QA/Evidence/SPEC-01/；red 或首轮覆盖断言文件；环境变量 SPEC01_EVIDENCE_TAG 插入 _tag 后缀。
/// Summary 键序与迁移前逐字符一致（含重复的 mode 键，由调用方 extra 提供）。
/// </summary>
public class SPEC01_Evidence : QaEvidence
{
    public SPEC01_Evidence(int run, string mode) : base(Spec(run, mode), run, mode) { }

    static QaEvidenceSpec Spec(int run, string mode)
    {
        bool red = mode == "red";
        return new QaEvidenceSpec
        {
            Task = "SPEC-01",
            LogTag = "SPEC01",
            EvidenceFolder = "SPEC-01",
            TagEnvVar = "SPEC01_EVIDENCE_TAG",
            Prefix = red ? "SPEC01_Red_" : "SPEC01_",
            PrefixWithTag = red ? "SPEC01_Red{tag}_" : "SPEC01_Green{tag}_run{run}_",
            AssertionFileName = "{prefix}Assertions.jsonl",
            SummaryFileName = red ? "SPEC01_Red_Summary.json" : "SPEC01_Summary_run{run}.json",
            SummaryFileNameWithTag = red ? "SPEC01_Red{tag}_Summary.json" : "SPEC01_Green{tag}_run{run}_Summary.json",
            ExitCodeFileName = red ? "SPEC01_Red_ExitCode.txt" : "SPEC01_ExitCode_run{run}.txt",
            ExitCodeFileNameWithTag = red ? "SPEC01_Red{tag}_ExitCode.txt" : "SPEC01_Green{tag}_run{run}_ExitCode.txt",
            RunIndexFileName = "SPEC01_Runs{tag}.jsonl",
            ShouldTruncateAssertions = (r, m) => m == "red" || r <= 1
        };
    }
}
