---
name: Executor Dispatch
description: 向【执行者】派单并执行指定工单。触发词：派单、执行、执行工单、开工。
---

## 触发

`Docs/Handoff/TASK_XXX.md` 已就绪，用户说“派单”“执行”“执行工单”“开工”（可交 `executor` 子代理）。

## 步骤

1. 派单只给三样：工单路径 + 风险等级 + “只读该工单文件，禁翻其他文档”。
2. 工单必须自包含（要求见 `Docs/Handoff/workflow-rules.md` §1）；信息不足由执行者在 REPORT 声明，不自行补读其他文档。
3. 执行者按 `AGENTS.md` 走三闸门：影响面清单 → Unity 必须关闭（进程 0 + 无 `Temp/UnityLockfile`）→ 资产归用户；修复/功能 red → green，时序类 N≥3。
4. 交付 `Docs/Handoff/REPORT_XXX.md`，节次按 `workflow-rules.md` §2；建议提交格式按 §3。

## 规则

- 硬约束见 `AGENTS.md`，执行者人格见 `.opencode/agents/executor.md`。
- 无法复现 / 需求矛盾 / 越界 / 编辑器未关 → 停下报告，不自行取舍。
