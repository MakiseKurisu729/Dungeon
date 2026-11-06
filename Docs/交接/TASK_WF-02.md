# TASK — WF-02：工作流文档收敛（唯一规则书）

> **风险等级：低风险（文档/配置；不改代码与资产）**
> **授权**：所有者 2026-11-05 明确回复“按你说的做”，批准本工单 §2 的文档结构调整。
> **前置**：当前代码修复批次已提交（`1bec9c93`）；工作区只剩两个自动生成的 `.csproj`。
> **本单不改**：`Assets/**`、`ProjectSettings/**`、`Packages/**`、`Docs/Handoff/CURRENT.md`、任何 `TASK_*` / `REPORT_*` / 证据。

## 1. 目标

把现在分散在 8+ 个文件里的流程规则收敛成：
- **一个规则书**：`AGENTS.md`（只保留角色、闸门、风险、停止、提交、长任务、文档地图）；
- **一个细则文件**：`Docs/Handoff/workflow-rules.md`（REPORT/提交/证据/前置清单模板）；
- **事实归事实**：`Docs/00` 只留事实，流程条款迁出；
- **历史归档**：`Docs/03`、`Docs/05` 移入 `Docs/Archive/`，不再作为开工依据；
- **决策留痕**：新增 `ADR-0003`（技能动画槽位化/子状态机，替代 03 §12 中冲突条款）。

**硬要求：不丢规则。** 每条旧规则必须在 REPORT 的“规则映射表”里有去处（新文件+章节，或“归档，不再适用”+理由）。

## 2. 允许改动的文件（超出即越界）

| 文件 | 动作 |
|---|---|
| `AGENTS.md` | 重写为精简规则书（≤150 行） |
| `Docs/Handoff/workflow-rules.md` | 扩为细则文件（REPORT/提交/证据/前置/风险/停止模板） |
| `Docs/00_项目事实基线.md` | 删除 §8 工作流约定，替换为指针；其余事实不动 |
| `Docs/03_AI协作与项目交接文档.md` | 移动到 `Docs/Archive/`，文件头加“历史文档”注记 |
| `Docs/05_Agent工作流人工协作与毕设基石重构指南.md` | 移动到 `Docs/Archive/`，文件头加“历史文档”注记 |
| `Docs/01_总体架构与内容生产规范.md` | 新增“架构不变量”节，承接 03 §12 中仍有效且与动画无关的条目；动画相关条目指向 ADR-0003 |
| `Docs/Decisions/ADR-0003_技能动画槽位化与子状态机.md` | 新建 |
| `Docs/README.md` | 更新目录职责与索引（删除 `Tasks/`，归档 03/05，加入 workflow-rules/ADR） |
| `.opencode/skills/brain-handoff/SKILL.md` | 精简为薄提示，只引用 AGENTS |
| `.opencode/skills/executor-dispatch/SKILL.md` | 同上 |
| `.opencode/skills/acceptance-loop/SKILL.md` | 同上 |
| `.opencode/agents/executor.md` | 精简为薄提示，保留 git deny 列表 |

**允许读取**（本单特批，突破“只读工单”）：`AGENTS.md`、`Docs/00`、`Docs/03`、`Docs/05`、`Docs/Handoff/workflow-rules.md`、`Docs/README.md`、三个 skill、`executor.md`、`Docs/Decisions/ADR-0002`。

## 3. 目标内容要求

### 3.1 `AGENTS.md`（规则书，≤150 行）

必须包含且只包含这些章：

1. **0 先读**：`CURRENT.md` 是状态唯一入口；事实看 `Docs/00`；任务路径以 CURRENT 为准。
2. **1 角色与权限**：用户 / 大脑 / 执行者；大脑只写 `Docs/Handoff/`；执行者禁一切 git 写；Done 只能用户确认。
3. **2 任务生命周期**：目标 → 大脑写 TASK（自包含）→ 派单（只给 TASK+风险）→ 执行（red→green→证据）→ 大脑验收 → 用户目检 + GitHub Desktop 提交 → 用户标 Done。
4. **3 开工前置（三闸门）**：影响面清单（新文件/符号/字符串/场景引用/`.meta`）；Unity 必须关闭（自证进程=0、无 `Temp/UnityLockfile`；长任务可自证替代人工确认）；资产管辖权（场景/预制体/Animator/`.asset` 归用户，AI 只出清单）。
5. **4 验证与证据**：自动化优先；修复/功能 red→green；时序类 N≥3；证据目录与命名、**只追加不覆盖**（细则见 workflow-rules）；REPORT 必含节指向 workflow-rules。
6. **5 风险两级**：**常规**（脚本/测试/文档，大脑验收即可）与**高风险**（场景/预制体/Animator/GUID/伤害/网络/ProjectSettings）；高风险必须 ADR（若改架构不变量）+ 用户审阅；取消“中风险强制外部顾问”。
7. **6 停止与熔断**：合并旧 4.2/4.3/长任务五条：单步重试≥3、编译错连续≥2、计划外资产/装包/迁移、git 异常、配额、Unity 未关、仓库与基线不符。
8. **7 提交**：AI 不 commit/push/删分支；用户 GitHub Desktop 一键提交；大脑每次给“标题 + 正文（为什么）”。
9. **8 长任务模式**：计划书落盘即批准；范围锁死；熔断；执行者不标 Done/不改 CURRENT；醒来目检清单。
10. **9 文档地图**：CURRENT / Handoff / 00 / 01 / 02 / 04 / ADR / workflow-rules / skills / Archive。
11. **10 工程三性**与命名约定（近战 `AttackMelee`、远程 `AttackRange`、触发器 `Attack<风格>`）。
12. **11 Do not 汇总**（保留：不把规划写成已实现、不猜现状、不全仓扫描、不整文件重写、不装包、不改任务外文件、不擅自改资产、不声称未运行的验证、不编造验收）。

### 3.2 `workflow-rules.md` 必须新增/保留

- TASK 模板要求（自包含、边界、验收、证据路径、回滚锚、绝不触碰、影响面清单）。
- REPORT 模板（摘要、证据锚点、改动清单、测试数据、实现原理、模块导航、未决项、建议提交）。
- 提交规范：标题 Conventional Commits + 正文一行“为什么”；默认单提交，GitHub Desktop；不再要求分 hunk。
- 证据规范：`Docs/QA/Evidence/<TASK-ID>/`；运行日志/JSONL/Summary/ExitCode；**canonical 套件目录只追加不覆盖**，重跑用 tag/子目录；N≥3 口径。
- 前置清单：影响面闸门、Unity 关闭自证、资产改动清单格式。
- 风险两级表与对应审查。
- 停止/熔断清单。

### 3.3 `Docs/00`

- 删除 §8（工作流约定、模型配置、钱包路由），替换为 3 行指针：流程看 `AGENTS.md`，细则看 `Docs/Handoff/workflow-rules.md`，模型/钱包配置看 `workflow-rules.md` 附录。
- 其余事实、未知项、维护规则不动。

### 3.4 `Docs/03` / `Docs/05` 归档

- 移动到 `Docs/Archive/`，文件名不变；文件头加：
  `> 历史文档（2026-11-05 归档）。流程以 AGENTS.md 为准；本文只作背景，不作为开工依据。`
- 03 §12 的架构不变量：仍有效且与动画无关的（单层动画、Override、攻速单一来源、命中 normalizedTime、统一伤害入口等）迁到 `Docs/01` 的“架构不变量”节；`Attack_Draw→Attack_Release` 保留但注明“远程普攻在 `AttackRange` 子状态机内保持该顺序”；AnyState 限制改为“技能槽允许 AnyState 打断，其余仍限受击/死亡”，并指向 ADR-0003。

### 3.5 `ADR-0003`（Accepted）

记录 2026-11-05 的决定：
- 技能槽 Q/W/E/R = 子状态机（每段一状态一 Clip，支持多段技能）；
- 普攻远程 `AttackRange` = 子状态机（`Draw → Release`），不烘 Clip；
- 技能槽允许 `AnyState → 槽`、`Has Exit Time=0`、可打断普攻；
- 命中帧归一化；
- 覆盖器按“槽的段”覆盖 Clip，运行时换技能按槽换整组 Clip；
- 影响与回滚；标注替代 `Docs/03` §12.3 的一部分。

### 3.6 Skills / executor

- 三个 skill 只保留：触发词、最简步骤、指向 AGENTS/workflow-rules；删除与 AGENTS 重复的规则长文。
- `executor.md` 保留 git deny 列表与“只读工单/影响面/Unity 前置/REPORT”摘要，细节指向 AGENTS。

## 4. 验收

1. **规则映射表**（REPORT 必含）：逐条列出旧规则 → 新位置；至少覆盖：影响面闸门、Unity 关闭、资产管辖、N≥3、证据只追加、REPORT 节、提交禁令、Done 权限、风险分级、长任务、熔断、工程三性、Do not、When stuck、诊断纪律、效率约束、工单自包含。
2. `AGENTS.md` ≤150 行；无失效相对链接（逐个检查 `Docs/...` 路径存在）。
3. `Docs/README.md` 与实际目录一致（无 `Tasks/`，03/05 指向 Archive）。
4. `git status` 只出现本单允许的 Docs/.opencode 文件；**无 `Assets/**`、`ProjectSettings/**`、`Packages/**`**。
5. `Docs/Handoff/CURRENT.md` 未被修改。
6. 不删除任何旧文件（只移动/归档）。

## 5. 报告

`Docs/Handoff/REPORT_WF-02.md`：结果摘要、改动清单、规则映射表、链接检查、`git diff --stat`、遗留问题、建议提交（标题+正文）。

## 6. 熔断

需要改代码/资产、需要删除旧文档、发现规则无法归类、链接无法保持、工作区出现他人改动 —— 停止并报告。

## 7. 建议提交（预览）

标题：`docs(workflow): consolidate agent rules into AGENTS.md`

正文：`为什么：流程规则散落在 8+ 个文件中且互相冲突，入口文件与当前状态不一致`
