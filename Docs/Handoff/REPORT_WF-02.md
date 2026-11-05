# REPORT — WF-02：工作流文档收敛（唯一规则书）

- 任务：`Docs/Handoff/TASK_WF-02.md`
- 风险：低风险（文档/配置）
- 执行者：OpenCode＋DeepSeek（`opencode-go/deepseek-v4.1-flash`，Go 订阅钱包）
- 日期：2026-11-05

## 1. 结果摘要

已完成。流程规则从 8 个分散文件收敛为「`AGENTS.md` 规则书（130 行）+ `Docs/Handoff/workflow-rules.md` 细则」两层结构；`Docs/00` §8 工作流条款迁出并降为指针；`Docs/03`、`Docs/05` 移动到 `Docs/Archive/`（仅加历史注记，正文未改）；`Docs/01` 新增 §1.4「架构不变量」承接 03 §12；新建 `ADR-0003`；`Docs/README.md` 与三个 skill、`executor.md` 同步瘦身。

**旧规则未丢失**：70 条逐项映射见 §3，其中 57 条落入新文件（含 5 条部分迁移+细目归档），13 条明确标“归档，不再适用”并给出理由。

未修改任何 `Assets/**`、`ProjectSettings/**`、`Packages/**`、`Docs/Handoff/CURRENT.md`、任何 `TASK_*`/`REPORT_*`/证据；未执行任何 git 写操作。

## 2. 改动文件清单

| 文件 | 动作 | 说明 |
|---|---|---|
| `AGENTS.md` | 重写 | 130 行规则书，0–11 章 |
| `Docs/Handoff/workflow-rules.md` | 扩写 | 1–9 章 + 附录 A（TASK/REPORT/证据/提交/前置/风险/停止/模型与钱包） |
| `Docs/00_项目事实基线.md` | 局部改 | 仅替换 §8（原 37 行 → 6 行指针），§0–§7 未动 |
| `Docs/01_总体架构与内容生产规范.md` | 局部改 | 新增 §1.4「架构不变量」；§11.1 第 4 条加 ADR-0003 指针（防与新节矛盾） |
| `Docs/03_AI协作与项目交接文档.md` → `Docs/Archive/03_…` | 移动 | 文件头加历史注记；正文未改 |
| `Docs/05_Agent工作流人工协作与毕设基石重构指南.md` → `Docs/Archive/05_…` | 移动 | 同上 |
| `Docs/Decisions/ADR-0003_技能动画槽位化与子状态机.md` | 新建 | `Accepted`，含背景/决策/原因/影响/回滚/替代关系 |
| `Docs/README.md` | 更新 | 目录职责、设计文档索引、阅读顺序；删 `Tasks/`，03/05 指向 `Archive/` |
| `.opencode/skills/brain-handoff/SKILL.md` | 精简 | 26 → 24 行，只留触发/步骤/规则指针 |
| `.opencode/skills/executor-dispatch/SKILL.md` | 精简 | 29 → 21 行 |
| `.opencode/skills/acceptance-loop/SKILL.md` | 精简 | 29 → 22 行 |
| `.opencode/agents/executor.md` | 精简正文 | frontmatter 与 21 条 git deny 未动，正文 12 行 → 7 条摘要 |

## 3. 规则映射表（旧规则 → 新位置）

> 形式：`旧位置 / 然后 / 新位置`。“归档”= 保留在 `Docs/Archive/` 原文中，因阶段已过或与现状矛盾而不再具备效力。

### 3.1 旧 `AGENTS.md`

| # | 旧规则 | 新位置 |
|---|---|---|
| A1 | §1 当前阶段（FND-00B 状态） | 移除状态块；状态唯一入口改为 `Docs/Handoff/CURRENT.md`（新 §0） |
| A2 | §2 开工必读顺序 | 新 §0 先读 |
| A3 | §3 角色表 | 新 §1 角色与权限 |
| A4 | §4 硬约束：不把规划写成已实现 / 不猜现状 / 基线不符停止 / 不无边界扫描 / 不整文件重写 / 不装包 / 不改任务外文件 | 新 §11 Do not + §0 效力顺序 |
| A5 | §4 不擅自改场景/预制体/Animator/ProjectSettings/`.meta` | 新 §3 第 3 条 + §11 |
| A6 | §4 AI 不自行提交/推送/发布/删分支 | 新 §1 + §7 |
| A7 | §4 工程三性 + 触发器 `Attack<风格>` 命名 | 新 §10 |
| A8 | §4 长任务模式 ①–⑤ | 新 §8 |
| A9 | §4.1 Do not 汇总（含越权写文件） | 新 §11 |
| A10 | §4.2 When stuck 4 条 | 新 §6.2 |
| A11 | §4.3 诊断纪律（根因先行 / N≥3 / 证据链） | 新 §4 + workflow-rules §4.4 |
| A12 | §4.4 效率约束（大脑 / 执行者） | 新 §6.3 |
| A13 | §4.5 工单自包含 | 新 §2 + workflow-rules §1 |
| A14 | §5 文档入口 | 新 §9 文档地图 |
| A15 | §6 风险三级（低/中/高 + 中风险外部顾问） | 新 §5 两级（常规/高风险）；“中风险强制外部顾问”取消；审查清单 workflow-rules §6 |
| A16 | 会话角色协议·大脑 | 新 §1 + `.opencode/skills/brain-handoff/SKILL.md` |
| A17 | 会话角色协议·执行者（默认验证 / 影响面闸门 / Unity 前置 / 隔离副本 / 实现原理 / 建议提交 / 模块导航 / 提交权限） | 新 §1、§3、§4、§7 + `executor.md` + workflow-rules §2/§3/§5.2 |

### 3.2 旧 `Docs/00` §8

| # | 旧规则 | 新位置 |
|---|---|---|
| B1 | 自动化验收标准 1–4（固定节、目检 ≤3、自动化优先、≥2 / N≥3） | workflow-rules §4（第 6 条）“自动化验收标准”+ AGENTS §4 |
| B2 | 文档命名与单一真相源 1–3 | workflow-rules §8.1 |
| B3 | 影响面清单确认闸门 + `.csproj` 隐含延伸 | AGENTS §3 第 1 条 + workflow-rules §5.1 |
| B4 | Unity 专属警告（`.meta` / 字符串引用 / GUID） | AGENTS §3 第 2 条 + workflow-rules §5.1 |
| B5 | 提案闸门 | workflow-rules §8.3 |
| B6 | TDD 前置 | AGENTS §4 + workflow-rules §8.2 |
| B7 | 模型配置（temperature 0、关思考回滚） | workflow-rules 附录 A |
| B8 | 钱包路由（Go 前缀 / Zen 前缀 / REPORT 标注 / Luna 暂不启用） | workflow-rules 附录 A |

### 3.3 旧 `Docs/03`（已归档）

| # | 旧规则 | 新位置 |
|---|---|---|
| C1 | §1 总体协作方案、§1.1 为何不单模型包办、§1.2 单订阅 | 归档，不再适用：选型结论已由 ADR-0002 与 workflow-rules 附录 A 承载 |
| C2 | §2 角色分工表、§2.2 角色非永久绑定 | AGENTS §1 |
| C3 | §3 任务分级 XS/S/M/L | workflow-rules §1（≤3 文件、L 必拆）+ AGENTS §5（风险与文件数分开） |
| C4 | §4.1 执行预算 7 条 | workflow-rules §1.1 |
| C5 | §4.2 默认禁止事项 8 条 | AGENTS §11 |
| C6 | §4.3 异常消耗处理（6 条） | AGENTS §6.1（含“消耗明显高于同类任务”）+ workflow-rules §7 |
| C7 | §5 成本控制（小上下文 / 订阅边界 / 记录真实成本表） | workflow-rules 附录 A + REPORT 节次 §2.9「消耗记录」；成本表细目归档 |
| C8 | §7 仓库文档体系图 | AGENTS §9 + `Docs/README.md` |
| C9 | §7.1 三类信息分开（事实/历史/待验证） | AGENTS §0 效力顺序 + workflow-rules §6 文档审查项 |
| C10 | §8 标准任务包模板 | workflow-rules §1 |
| C11 | §8.1 任务包质量规则 5 条 | workflow-rules §1 |
| C12 | §9 提示模板 9.1–9.6 | 归档，不再适用：等价能力由三个 skill 与 `executor.md` 承载 |
| C13 | §10 完成定义 DoD 8 条 | AGENTS §2 生命周期 + §4/§5 + workflow-rules §2、§6 |
| C14 | §11.1 分支纪律（main / migration / feature / refactor / docs） | 归档，不再适用：现为单分支 + 用户 GitHub Desktop 提交；“AI 不操作分支”保留在 AGENTS §7 |
| C15 | §11.2 提交纪律（一提交一事、不存密钥） | workflow-rules §3（第 5 条密钥禁令）+ AGENTS §7 |
| C16 | §11.3 Unity 资产纪律 | AGENTS §3 第 3 条 + workflow-rules §5.3 |
| C17 | §12 防回退硬约束 14 条 | `Docs/01` §1.4 架构不变量（14 条全量迁入）；动画例外 → ADR-0003 |
| C18 | §13 推荐任务序列 T000–T018 | 归档，不再适用：已由 FND/SKILL/SPEC 实际批次取代 |
| C19 | §14 交接快照模板 | `CURRENT.md` 现行格式；模板文本留档 |
| C20 | §15 文档同步触发器 | workflow-rules §8.4 |
| C21 | §16 审查清单（代码/资产/协作/文档） | workflow-rules §6（常规 + 高风险追加） |
| C22 | §17 安全与边界 | 密钥/生成目录禁令 → workflow-rules §3；公开仓库与打包范围归档 |
| C23 | §18 下一会话启动指令 | AGENTS §0 |
| C24 | §19 价格与产品信息核验 | 归档，不再适用：易变信息不入项目文档 |

### 3.4 旧 `Docs/05`（已归档）

| # | 旧规则 | 新位置 |
|---|---|---|
| D1 | §0 一句话原则 / 阶段四项追求 | 归档（背景）；可执行部分 = AGENTS §2、§4 |
| D2 | §1 对过去方式的六点校正 | 归档（诊断性背景） |
| D3 | §2 角色分工、§2.1/§2.2 大脑与执行者输入输出、§2.3 用户不再承担的工作 | AGENTS §1 + §9；输入输出分工由 skills 与 `executor.md` 承载 |
| D4 | §3 用户必须亲自完成的六类工作 | 归档（背景）；验收权集中在 AGENTS §1 |
| D5 | §4 标准开发循环 | AGENTS §2 任务生命周期 |
| D6 | §4.1 开工前六问 | workflow-rules §1.1 |
| D7 | §4.2 结构化问题反馈模板 | workflow-rules §1.2 |
| D8 | §4.3 每轮成本记录 | workflow-rules §2.9 |
| D9 | §5 毕设基石定义 12 条 / 非目标 / 三项扩展实验 | 归档（阶段目标，已由 FND 批次推进） |
| D10 | §6 FND-00～FND-08 重构顺序 | 归档，不再适用：已被实际批次取代 |
| D11 | §7.1–7.4 编辑器工具优先级与工具任务要求 | AGENTS §3 第 3 条 + workflow-rules §5.3；“只读 → 干运行 → 可撤销写入”细目归档 |
| D12 | §8 当前阶段工单模板 | workflow-rules §1 |
| D13 | §9 提示词（规划 / 编码 / 审查 / 失败定位） | 三个 skill + `executor.md` |
| D14 | §10 人工审查清单 | workflow-rules §6 |
| D15 | §11.1 完成定义 / §11.2 立即停止的情况 | AGENTS §2、§6 + workflow-rules §7 |
| D16 | §12 每周节奏与防拖延 | 归档（背景） |
| D17 | §13 现在就可以开始的第一轮 | 归档（已完成） |
| D18 | §14 最终自我检查 | 归档（背景） |

### 3.5 旧 `workflow-rules.md` 与 skills

| # | 旧规则 | 新位置 |
|---|---|---|
| E1 | §1 建议提交格式（Conventional Commits / 一行为什么 / `git add` 清单 / 混合拆分） | workflow-rules §3（保留；默认单提交，拆分改为“按需”） |
| E2 | §2 模块导航说明（≤200 字、归档 `Docs/Learning/`） | workflow-rules §2 第 6 条 |
| F1 | 三个 skill 中与 AGENTS 重复的规则长文 | 精简为“触发 + 步骤 + 指针”，规则本体统一回 `AGENTS.md` |

**条目统计：A 17 + B 8 + C 24 + D 18 + E 2 + F 1 = 70 条**（其中“归档，不再适用” 13 条，均附理由）。

## 4. 链接检查

- 范围：本单改动的 12 个文件，正则提取所有 `` `Docs/...` ``、`` `.opencode/...` ``、`` `AGENTS.md` `` 引用，用 `Test-Path` 逐个核对。
- 结果：**91 处引用，`AGENTS.md` 内 23 处（15 个唯一路径）0 失效**。
- 全部 16 处未命中均为以下三类，非失效链接：
  1. 占位符模板：`TASK_XXX`、`REPORT_XXX`、`<TASK-ID>`、`<ID>`（workflow-rules 与 skill 的模板写法）；
  2. 预存事实/历史描述：`Docs/00` 的 `Docs/_Incoming`、`Docs/01`～`05` 简写；归档文档正文内的旧路径（归档文本按工单要求未改）；
  3. 按需创建目录：`Docs/Learning/`（已在文中注明“按需创建”）。

## 5. `git diff --stat`（本单文件）

```text
 .opencode/agents/executor.md                       |  19 +-
 .opencode/skills/acceptance-loop/SKILL.md          |  28 ++-
 .opencode/skills/brain-handoff/SKILL.md            |  24 +--
 .opencode/skills/executor-dispatch/SKILL.md        |  27 +--
 AGENTS.md                                          | 193 ++++++++++---------
 Docs/00_项目事实基线.md                            |  40 +---
 Docs/01_总体架构与内容生产规范.md                  |  28 ++-
 Docs/Handoff/workflow-rules.md                     | 206 +++++++++++++++++++--
 Docs/README.md                                     |  30 +--
 9 files changed, 384 insertions(+), 211 deletions(-)

未跟踪（新建/移动目标）：
 Docs/Archive/03_AI协作与项目交接文档.md
 Docs/Archive/05_Agent工作流人工协作与毕设基石重构指南.md
 Docs/Decisions/ADR-0003_技能动画槽位化与子状态机.md
```

移动在 `git status` 中表现为 `D Docs/03_…` + `?? Docs/Archive/03_…`（未 `git add`，故暂未显示为 rename）；提交时 git 会自动识别为重命名。

## 6. 实现原理（文档结构，面向项目所有者）

**问题**：同一批规则同时活在 8+ 个文件里（AGENTS §4、事实基线 §8、03 全文、05 全文、workflow-rules、三个 skill、executor.md），且互相冲突——例如风险三级里的“中风险强制外部顾问”在 03 与 AGENTS 中写法不同；Any State 限制在 AGENTS、事实基线、03 §12、01 §11.1 各有一份，且都禁止技能打断普攻。

**方案原理**：按“规则 / 细则 / 事实 / 历史 / 决策”五类拆开，让每类只有一个家：

- **规则**（谁不能做什么、何时停、谁有权标 Done）→ `AGENTS.md`，每会话必注入，必须短（130 行）。
- **细则**（可复制的模板与格式）→ `workflow-rules.md`，需要时才读，可以长。
- **事实**（当前提交、行为、未知项）→ `Docs/00`，只写已验证或已标注待复验的内容。
- **架构不变量**（不易逆转、改动必须写 ADR）→ `Docs/01` §1.4。
- **历史**（阶段方案、成本讨论、旧模板）→ `Docs/Archive/`，保留可追溯但不参与决策。

**为什么这样分**：模型上下文有限，入口文件越长越容易被截断或忽略，所以强制 ≤150 行；冲突的根因是“同一规则多处副本”，因此每条规则只保留一个权威位置，其余位置只放指针。这样以后改规则只改一处，不会再次分叉。

**ADR-0003 的作用**：动画槽位化与 03 §12 的“Any State 仅限受击/死亡”“普攻固定两段状态”直接冲突。按“改不变量必须写 ADR”的新规则，先把新决定固化成 ADR，再让 `Docs/01` §1.4 引用它，避免用改正文的方式悄悄改掉冻结条款。

## 7. 受影响模块导航说明

不适用（纯文档/配置单，无代码模块）。

## 8. 现场差异与未决项

### 8.1 现场差异（工单前置假设不成立，已如实记录）

工单写“工作区只剩两个自动生成的 `.csproj`”，实际开工时 `git status` 已有他人改动：

| 文件 | 最后修改时间 |
|---|---|
| `Assets/Scenes/DungeonWorld.unity` | 2026-11-05 13:54:12 |
| `Assets/_Project/Art/Animations/PlayerBase.controller` | 2026-11-05 13:54:11 |
| `Docs/Handoff/PROPOSAL_SKILL-ANIM-QWER.md` | 2026-11-05 13:52:56 |
| `Assembly-CSharp.csproj` / `Assembly-CSharp-Editor.csproj` | 12:37 / 04:28 |

这些改动均早于本单开工（14:00 前），本单**未读写**它们（场景/控制器 last-write 时间未变）。因此验收项 4“`git status` 无 `Assets/**`”无法按字面满足：`Assets/` 的 2 处改动来自开工前的工作区，不是本单产物。请提交时把它们与本单拆开。

### 8.2 未决项

1. **过期指针未修**（不在本单允许改动的文件内）：`CURRENT.md` 第 36/50/51/70 行、`PROPOSAL_FND-02A.md` 第 59 行、`TASK_FND-02B` 第 68 行仍引用“事实基线 §8”“AGENTS.md §4.3”，应改为 `workflow-rules.md` §4 / §7 / AGENTS §4。
2. `Docs/00` §7 维护规则第 4 条仍写“规划写在 `Docs/01`～`05`”，因 03/05 已归档而略失效；按工单“其余事实不动”未改，建议下次文档批次改为 `01`/`02`/`04`。
3. `Docs/01` §11.2 “玩家公共状态”列表（`Skill`、`Bash`）仍是 ADR-0003 之前的表述，建议随动画资产迁移一并刷新。
4. `ADR-0002` 第 54 行仍指向已不存在的 `Docs/Tasks/_TEMPLATE.md`（应为 `Docs/Handoff/TASK_TEMPLATE.md`），未改（不在允许范围）。
5. `Docs/Learning/` 尚不存在，条款已注明“按需创建”。

## 9. 建议提交

标题：

```text
docs(workflow): consolidate agent rules into AGENTS.md
```

正文：

```text
为什么：流程规则散落在 8+ 个文件中且互相冲突，入口文件与当前状态不一致
```

`git add` 清单（仅本单文件）：

```text
git add AGENTS.md
git add Docs/README.md
git add "Docs/00_项目事实基线.md"
git add "Docs/01_总体架构与内容生产规范.md"
git add Docs/Handoff/workflow-rules.md
git add "Docs/Decisions/ADR-0003_技能动画槽位化与子状态机.md"
git add "Docs/Archive/03_AI协作与项目交接文档.md"
git add "Docs/Archive/05_Agent工作流人工协作与毕设基石重构指南.md"
git rm --cached "Docs/03_AI协作与项目交接文档.md" "Docs/05_Agent工作流人工协作与毕设基石重构指南.md"
git add .opencode/agents/executor.md
git add .opencode/skills/brain-handoff/SKILL.md
git add .opencode/skills/executor-dispatch/SKILL.md
git add .opencode/skills/acceptance-loop/SKILL.md
```

（用 GitHub Desktop 时可直接勾选上述文件；`Docs/03`、`Docs/05` 的删除会自动随 `Docs/Archive/` 下的同名新文件组成 rename。）

**不要**把 `Assets/Scenes/DungeonWorld.unity`、`Assets/_Project/Art/Animations/PlayerBase.controller`、`Docs/Handoff/PROPOSAL_SKILL-ANIM-QWER.md`、两个 `.csproj` 混入本提交。

## 10. 消耗记录

- 钱包：Go 订阅（`opencode-go/`）
- 模型：`opencode-go/deepseek-v4.1-flash`
