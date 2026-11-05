---
name: Acceptance Loop
description: 【大脑】批末复审与验收执行者结果。触发词：复审、验收、批末复审、终审。
---

## 触发

执行者提交 REPORT 后，用户说“复审”“验收”“批末复审”“终审”。

## 步骤

1. 一次性读完：对应 TASK、REPORT、QA 表相关行。
2. 三项核查，缺一即不通过：
   - **REPORT 节次齐全**（按 `Docs/Handoff/workflow-rules.md` §2；纯文档/验收单可标注不适用）。
   - **断言数据**：断言 ID、实际数据、日志路径真实存在；次数达标（常规 ≥2，间歇/时序 N≥3），不一致标“不稳定”。
   - **git diff 只读**：`git status --short` + `git diff --stat`，确认改动全在工单授权内，无场景/预制体/Animator/ProjectSettings/`Assets` 越权改动。
3. 风险按 `AGENTS.md` §5：常规由大脑复审即可；高风险由用户审阅（改架构不变量须有 ADR）。
4. 结论（通过 / 退回 + 缺项清单，≤300 字），并由用户确认后更新 `Docs/Handoff/CURRENT.md`。

## 规则

- 全流程规则见 `AGENTS.md`；证据与报告细则见 `Docs/Handoff/workflow-rules.md` §2、§4。
- 证据缺失 / 目检未填 / 与基线矛盾 → 标“待目检”或退回，不替执行者补结果，不预填。
