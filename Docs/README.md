# Docs — 文档入口与目录职责

本目录是项目文档的唯一存放位置（不放入 `Assets/`，不生成 Unity `.meta`）。

> 新会话先读根目录 `AGENTS.md`（规则书），再读事实基线、`CURRENT.md` 和当前任务包。
> 流程规则唯一入口是 `AGENTS.md`；执行细则见 `Handoff/workflow-rules.md`。

## 目录职责

| 位置 | 职责 |
|---|---|
| `00_项目事实基线.md` | **当前事实，不是规划。** 记录仓库已核验事实、用户报告待复验项、目标设计、尚未实现项和未知项。 |
| `01_总体架构与内容生产规范.md` | 目标架构、内容生产规范与**架构不变量**。 |
| `02_Unity63迁移及毕业设计实施计划.md` | Unity 6.3 迁移与毕业设计实施计划。 |
| `04_可复用资产与编辑器工具设计清单.md` | 可复用资产与编辑器工具清单。 |
| `Decisions/` | 重要且不易逆转的决策（ADR）。动画槽位化与子状态机见 `ADR-0003`。 |
| `Specs/` | spec 层：`Specs/constitution.md`（Mission / Scope / Tech stack，1 页索引）与 `Specs/README.md`（课程 spec → 本项目产物映射）。 |
| `Handoff/CURRENT.md` | 当前交接入口：分支、提交、当前任务、禁止事项、下一会话第一步。 |
| `Handoff/TASK_*.md`、`Handoff/REPORT_*.md` | 任务包与执行报告；模板 `Handoff/TASK_TEMPLATE.md`（三节 + AC 表）、`Handoff/REPORT_TEMPLATE.md`（含 AC 回填与 `文件:行`）。 |
| `Handoff/workflow-rules.md` | 工作流细则：TASK/REPORT 模板、AC 表、证据、提交与分支、前置清单、风险、停止、讲稿、Replanning、Done 门槛、模型与钱包路由。 |
| `Learning/` | 学习讲稿：`<ID>_讲稿.md`，模板 `Learning/TEMPLATE_讲稿.md`（AI 初稿 + 所有者改写）。 |
| `QA/`、`QA/Evidence/` | 真实验收记录和证据。只写真实执行结果。 |
| `Archive/` | 历史资料（含 `03`、`05`），**不作为当前事实，也不作为开工依据**。 |

## 设计文档索引

| 编号 | 文件 | 用途 |
|---|---|---|
| 01 | `01_总体架构与内容生产规范.md` | 目标架构、内容生产规范、架构不变量 |
| 02 | `02_Unity63迁移及毕业设计实施计划.md` | Unity 6.3 迁移与毕业设计实施计划 |
| 04 | `04_可复用资产与编辑器工具设计清单.md` | 可复用资产与编辑器工具清单 |

原 `03_AI协作与项目交接文档.md` 与 `05_Agent工作流人工协作与毕设基石重构指南.md` 已于 2026-11-05 归档至 `Archive/`：仍有效的流程条款并入 `AGENTS.md` 与 `Handoff/workflow-rules.md`，架构条款并入 `01` 的“架构不变量”节。

以上文档描述的"目标能力"不等于"已实现能力"。任何"是否已经可用"的判断，必须回到 `00_项目事实基线.md` 与 `QA/` 的真实记录。

## 阅读顺序建议

1. `AGENTS.md`（规则书）
2. `Docs/00_项目事实基线.md`（当前事实）
3. `Docs/Handoff/CURRENT.md`（当前进度与下一步）
4. `Docs/Specs/constitution.md`（Mission / Scope / Tech stack）
5. `Docs/Handoff/TASK_*.md`（本次任务的边界与验收）
6. 需要设计背景时再读 `01`/`02`/`04`；需要历史背景时才读 `Archive/`
