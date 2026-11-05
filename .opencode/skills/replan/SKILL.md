---
name: Replan
description: 【大脑】批末收口与下一个 feature 的重新规划。触发词：收口、Replan、下一个功能、复盘。
---

## 触发

一个 feature 标 `Done` 后，用户说“收口”“Replan”“下一个功能”“复盘”。

## 步骤

1. 更新 `Docs/Handoff/CURRENT.md`（**仅大脑**）：分支、HEAD、当前任务、禁止事项、下一会话第一步。
2. 过 10 分钟清单（`Docs/Handoff/workflow-rules.md` §9）：路线图 / constitution / 规则与流程 / 下个 feature / 单独提交。
3. 输出：本轮结论（≤5 行）+ 下一 feature 一句话目标 + 建议提交（标题 + 一行“为什么”，按 §3）。
4. 若需改架构不变量 → 先写 ADR，再改 `Docs/Specs/constitution.md` 的链接指向。

## 规则

- 硬约束见 `AGENTS.md` §2、§7；清单细节见 `Docs/Handoff/workflow-rules.md` §9。
- 执行者不得改 `CURRENT.md`、不得标 `Done`；收口是大脑动作。
- 不在收口时顺手改代码或扩范围。
