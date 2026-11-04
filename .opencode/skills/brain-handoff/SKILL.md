---
name: Brain Handoff
description: 【大脑】新会话开场、读档与汇报进度。触发词：开场、汇报进度、新会话、交接。
---

## When to use

用户声明你是【大脑】并说“开场”“汇报进度”，或新会话需要接手当前进度时。

## 步骤

1. 一次性按顺序读完：`AGENTS.md`（已注入则跳过）→ `Docs/00_项目事实基线.md` → `Docs/Handoff/CURRENT.md` → CURRENT 指定的当前任务包/最近 REPORT。不分多轮小步读，不做全仓扫描。
2. 用 `git rev-parse --abbrev-ref HEAD` 与 `git rev-parse HEAD` 只读核对分支/HEAD 是否与 CURRENT 基线一致（【大脑】禁止 git 写操作）。
3. 汇报（≤300 字）：当前分支与 HEAD、任务状态、未决风险、“下一会话第一件事”、你建议的最小下一步。
4. 等用户确认后再生成工单；工单写入 `Docs/Handoff/TASK_XXX.md`，遵守工单自包含原则，并标注风险等级。

## 失败路径

- 基线与仓库不一致 → 停止，报告差异，不带矛盾继续。
- CURRENT 未给出准确任务路径 → 声明“信息不足”，向用户询问，不按状态猜测。
- 文档间需求矛盾 → 停下提问，不自行取舍。

## 验证

- 汇报中每条状态都能对应到 CURRENT/REPORT 的具体行；未经实测的内容不写成“已实现”。
- 未修改 `Docs/Handoff/` 以外任何文件（任务书特批除外）。
