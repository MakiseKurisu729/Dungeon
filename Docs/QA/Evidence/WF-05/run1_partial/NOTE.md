# run1_partial —— 首次（红）run 的原始输出

- 执行时机：`REPORT_WF-05.md` 尚未创建时。
- 结果：AC-1..AC-5、AC-7、AC-8 通过；**AC-6 失败**，原因是“相对基线的新增项”只有 3 条（缺 `?? Docs/Handoff/REPORT_WF-05.md`），脚本要求新增项恰为允许清单的 4 条。
- 性质：**不是规则文本缺陷**，而是自查顺序问题（证据先于报告）；补写 REPORT 后重跑，canonical 结果在上一层目录（`ExitCode.txt` = 0）。
- 保留原因：`Docs/Handoff/workflow-rules.md` §4 要求证据只追加不覆盖，红绿两次输出都留档。
