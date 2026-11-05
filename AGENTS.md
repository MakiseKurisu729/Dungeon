# AGENTS.md — 魔石地牢 · 唯一规则书

> 本文件是**流程规则的唯一入口**。事实看 `Docs/00_项目事实基线.md`；进度看 `Docs/Handoff/CURRENT.md`；目标与范围看 `Docs/Specs/constitution.md`；细则（模板/证据/提交格式）看 `Docs/Handoff/workflow-rules.md`。
> `Docs/Archive/` 只作背景资料，**不作为开工依据**。
> 最后更新：2026-11-05（WF-03 对齐 spec-driven 工作流）

## 0. 先读

1. `Docs/Handoff/CURRENT.md` —— 状态唯一入口：分支、HEAD、当前任务、禁止事项、下一会话第一步。
2. `Docs/00_项目事实基线.md` —— 已核验事实、待复验项、未知项。
3. `Docs/Specs/constitution.md` —— 任务是否在范围内（1 页：Mission / Scope / Tech stack）。
4. 当前任务包 —— 路径以 CURRENT 给出的为准；**不要按任务状态猜当前任务**。

效力顺序（冲突时）：当前任务书与硬约束 > 事实基线 > 已批准架构与 ADR > 仓库实际行为 > 历史文档与聊天。
本文件与仓库实际不符 → 停止并报告，先校准文档，不带矛盾继续。

## 1. 角色与权限

| 角色 | 职责 | 禁止 |
|---|---|---|
| 用户 | 产品决策、Unity 集成与验收、提交与推送、标 `Done` | 逐行实现 |
| 大脑（K3.1） | 事实校准、架构收敛、写 TASK（三节 spec）、按 AC 验收、Replan、维护 `Docs/Handoff/` | 改 `Assets/`、跑构建/测试、任何 git 写操作 |
| 执行者（OpenCode＋DeepSeek） | 按 TASK 改代码/文档、跑指定检查、写 REPORT | 越界、自行重构、任何 git 写操作 |

- **只有用户能把任务标 `Done`。**
- 执行者唯一例外：本次改动**全部在 `Docs/`** 且用户明确回复“批准提交”后可 commit。**代码与资产改动永远由用户亲自提交。**
- 模型与钱包路由见 `Docs/Handoff/workflow-rules.md` 附录 A。

## 2. 任务生命周期

```
目标 → 大脑写 TASK（三节：需求 / 方案 / 验证）→ 所有者审 spec（需求、方案取舍、AC）
→ 派单（TASK 路径 + 风险等级 + 只读边界）→ 执行（red → green → 证据 → REPORT）
→ 大脑按 AC 表验收 → 学习讲稿（AI 初稿 + 所有者改写）→ 用户目检 + GitHub Desktop 提交
→ 用户标 Done → Replan（下一个 feature）
```

- TASK 必须自包含且固定三节 + AC 表，模板 `Docs/Handoff/TASK_TEMPLATE.md`；禁止“参见 CURRENT.md”式指针。
- REPORT 固定节次，模板 `Docs/Handoff/REPORT_TEMPLATE.md`；AC 回填不得留空。
- 讲稿：feature / 高风险任务在 Done 前产出 `Docs/Learning/<ID>_讲稿.md`（模板 `Docs/Learning/TEMPLATE_讲稿.md`）。
- Replanning 清单见 `Docs/Handoff/workflow-rules.md` §9；Done 门槛见同文件 §10。
- 信息不足时，执行者在 REPORT 写“信息不足”并列出缺项，不自行翻阅别的文档补全。

## 3. 开工前置（三闸门）

1. **影响面清单**：涉及代码/资产修改，动手编辑前输出“受影响文件与符号清单”（新文件、符号、字符串引用、场景引用、`.meta`），用户确认后才编辑。纯 `Docs/` 文档单可由所有者在工单中预批，免逐项确认。
   - 新增测试 `.cs` 引起的 `.csproj` 自动改动，视为清单的隐含延伸，随资产一并确认。
2. **Unity 必须关闭**：任何 Unity 操作（批处理、烘焙、资产写入）前先确认编辑器已关；自证 = Unity 进程数 0 且无 `Temp/UnityLockfile`。长任务允许以自证替代人工确认，但证据必须写进 REPORT。
   - 重命名/移动文件必须连 `.meta` 一起；Animator 参数名、`SendMessage`、`GameObject.Find("名字")` 等字符串引用单列核查。
3. **资产管辖权**：场景、预制体、Animator Controller、`.asset` 归用户；AI 只出“改动清单/编辑器工具”，不直接改 YAML、不批量猜改 GUID。

## 4. 验证与证据

- **AC 表先行**：每条验收写成 Given/When/Then + 断言 ID + 证据路径；TASK §3 定表，REPORT §2 逐行回填。
- **自动化优先**：验收与修复验证先写可重复运行的测试，输出断言 + 数据 + 原始日志；人工只留 ≤3 项无法自动化的目检。一次性小验证可豁免，须写明理由。
- **修复/功能：red → green**：先写失败测试，再实现到全绿。
- **时序/间歇问题 N≥3**：单次通过不算修复；常规验收同一入口连续 ≥2 次。
- **证据**：写在 `Docs/QA/Evidence/` 下按任务号命名的子目录，含运行日志、JSONL、Summary、ExitCode。canonical 套件目录**只追加不覆盖**，重跑写 tag/子目录。
- **REPORT 必含节**见 `Docs/Handoff/workflow-rules.md` §2；修复/功能单另含“实现原理”（含 3–5 个 `文件:行`）与“受影响模块导航说明”。
- **学习讲稿**：feature / 高风险任务 Done 前必有 `Docs/Learning/<ID>_讲稿.md`；规则见 `Docs/Handoff/workflow-rules.md` §2.1。
- **诊断纪律**：根因先行，禁止表面补丁；每条结论可追溯到日志/断言/diff。

## 5. 风险两级

| 等级 | 范围 | 审查 |
|---|---|---|
| 常规 | 脚本、测试、文档、孤立小改 | 大脑批末复审即可 |
| 高风险 | 场景/预制体/Animator/GUID/伤害管线/网络同步/`ProjectSettings` | 用户审阅；改架构不变量时**必须先写 ADR** |

- 不再设“中风险强制外部顾问”。
- 文件数量与风险分开判断：伤害、GUID、场景、Animator 即使只改一个文件也算高风险。
- 高风险任务：改架构不变量**先写 ADR**；Done 后必须补 `Docs/Learning/<ID>_讲稿.md`（答辩要能讲清）。
- feature 任务（无论风险等级）Done 前都必须有讲稿；纯文档 / 纯巡检小修可写“不适用”。

## 6. 停止与熔断

### 6.1 熔断（触发即停并留现场报告）

- 单步重试 ≥3 次仍红；编译错误连续 ≥2 轮。
- 计划外需求：资产改动、装包/换包、升 Unity、目录迁移。
- git 异常（冲突、非预期改动、需要回退）；配额/额度撞限；消耗明显高于同类任务。
- Unity 未关闭或状态不明；仓库与任务基线不符。
- 同一命令重复 3 次以上无进展（疑似循环）。

### 6.2 When stuck

- 信息不足 → REPORT 写“信息不足” + 缺项清单，不翻其他文档补全。
- 无法复现 → 写“无法复现” + 建议复现步骤，不编造根因。
- 需求矛盾 → 停下提问，不自行取舍。

### 6.3 效率约束

- 大脑：讲清即止，不设字数上限，禁止为压字数反复重写；改已有文件只输出改动部分；能一次读完的文件禁止分多轮小步读；大型文档（QA 表、REPORT）交执行者创建或大改。
- 执行者：禁止整文件读取日志/大文档，只用 `grep` / `Select-String -Context 2` 定向提取；结果超 50 行只留计数 + 前 10 条示例。

## 7. 提交

- AI **不** `commit` / `push` / 发布 / 删除或改写分支。
- 用户用 GitHub Desktop 一键提交；代码与资产永远由用户提交。
- 每次交付给“标题 + 正文（一行为什么）”，格式见 `Docs/Handoff/workflow-rules.md` §3。

## 8. 长任务模式

所有者声明“进入长任务模式”后：

1. 执行前【大脑】产出长任务计划书（步骤/改动/验收/回滚锚/绝不触碰清单），所有者批准并落盘即生效。
2. 自主运行期**范围锁死**，遇计划外需求立即停。
3. 熔断按 §6 执行。
4. 执行者**不得**标 `Done`、不得改 `CURRENT.md`；所有目检项进《醒来目检清单》。
5. 收口必须等所有者目检 + 终审。

## 9. 文档地图

| 位置 | 用途 |
|---|---|
| `AGENTS.md` | 本文件：规则书 |
| `Docs/Handoff/CURRENT.md` | 状态唯一入口 |
| `Docs/00_项目事实基线.md` | 事实、未知项 |
| `Docs/01_总体架构与内容生产规范.md` | 目标架构 + 架构不变量 |
| `Docs/02_Unity63迁移及毕业设计实施计划.md` | 迁移与实施计划（里程碑 / Roadmap 唯一来源） |
| `Docs/04_可复用资产与编辑器工具设计清单.md` | 资产与工具清单 |
| `Docs/Specs/constitution.md` | 1 页索引：Mission / Scope / Tech stack；只在 Replan 更新 |
| `Docs/Decisions/ADR-*.md` | 决策留痕（改不变量必写） |
| `Docs/Handoff/workflow-rules.md` | 细则：TASK/REPORT/证据/提交/前置/风险/讲稿/Replan/Done |
| `Docs/Handoff/TASK_TEMPLATE.md`、`Docs/Handoff/REPORT_TEMPLATE.md` | 三节 TASK 模板、REPORT 模板（含 AC 回填） |
| `Docs/Handoff/TASK_*.md`、`REPORT_*.md` | 任务包与报告 |
| `Docs/Learning/` | 学习讲稿：`<ID>_讲稿.md`，模板 `Docs/Learning/TEMPLATE_讲稿.md` |
| `Docs/QA/`、`Docs/QA/Evidence/` | 验收记录与证据 |
| `.opencode/skills/`、`.opencode/agents/executor.md` | 话术 Skill 与执行者子代理（只索引本文件） |
| `Docs/Archive/` | 历史资料（含 03/05），不作开工依据 |

## 10. 工程三性

- 每次写代码默认考虑**可读性、可维护性、可拓展性**；为省事牺牲三性的写法须说明理由并报所有者批准。
- 命名遵循可拓展约定：触发器 `Attack<风格>`，近战 `AttackMelee`／远程 `AttackRange`。
- 新增内容优先“数据 + 组合”，不复制核心类、不新增静态全局状态。

## 11. Do not 汇总

- 把规划写成已实现；根据旧聊天或归档文档猜现状。
- 无边界全仓扫描；整文件重写已有文件（含代码、文档、Unity YAML）；批量格式化无关文件。
- 安装新包、升级引擎、替换框架；修改任务外文件。
- 擅自修改场景、预制体、Animator、`ProjectSettings`、`.asset`、`.meta`、GUID。
- 声称运行过未运行的构建/测试/验证；预填、推测或编造验收结果、目检结论、证据。
- 越权写文件：大脑只写 `Docs/Handoff/`（任务书特批除外）；执行者禁止一切 git 写操作。
- 同时做实现 + 无关重构 + 全面文档更新；为“顺便优化”扩大文件范围。
