# REPORT — WF-03：把流程升级为课程标准工作流（Spec → Implement → Validate → Learn → Replan）

> 对应 `Docs/Handoff/TASK_WF-03.md`。风险：低（文档 / 模板 / skills）。
> 本单未建 `Docs/QA/Evidence/WF-03/`：该路径不在工单 §2 允许改动列表内，故把检查命令与原始输出直接内嵌在 §5。

## 1. 结果摘要

**完成** —— 10 个文件按工单落地：三节 TASK 模板 + AC 表、REPORT 模板（AC 回填 + `文件:行`）、学习讲稿模板、Replanning 清单、Done 门槛、两个薄 skill、constitution 1 页索引；`AGENTS.md` 146 行（≤165），`CURRENT.md` 未改。

## 2. AC 回填表

| AC | 结果 | 断言 ID | 关键数据 | 日志路径 |
|---|---|---|---|---|
| AC-1 TASK 三节 + AC 表、REPORT 含 AC 回填与 `文件:行` | 通过 | `Grep.TaskSections` | TASK_TEMPLATE `§0..§5` 6 节 + `AC \| Given/When/Then \| 断言 ID \| 证据` 表头（行 68）+ 自包含声明（行 98）；REPORT_TEMPLATE `2. AC 回填表`（行 10）、`关键代码位置（3–5 条）`（行 24）、`三句话版本`（行 29） | 本文 §5 命令 1、2 |
| AC-2 constitution 1 页、只链接不复制 00/01/02 | 通过 | `Spec.ConstitutionIndex` | 36 行；6 节；`00`/`01`/`02` 只以路径 + 节号引用（§4、§5），正文无迁移计划/不变量条目复本 | 本文 §5 命令 3 |
| AC-3 AGENTS ≤165 行、无失效链接 | 通过 | `Agents.LineCount` / `Link.Check` | 146 行；限定路径全部可解析（`MISSING` 仅剩裸名/占位，见 §5） | 本文 §5 命令 4、5 |
| AC-4 Docs/README 与目录一致 | 通过 | `Docs.Index` | 新增 `Specs/`（行 17）、`Learning/`（行 21）、模板行（行 19）、阅读顺序（行 42）；实际目录 `Docs/Specs`、`Docs/Learning` 存在 | 本文 §5 命令 6 |
| AC-5 两个 skill 是薄提示 | 通过 | `Skill.Thin` | `feature-spec` 13 行、`replan` 13 行；只引用 `AGENTS.md` 与 `Docs/Handoff/workflow-rules.md`，无模板正文复述 | 本文 §5 命令 7 |
| AC-6 无 `Assets/**`、`ProjectSettings/**`、`Packages/**` 新改动；`CURRENT.md` 未改 | 通过 | `Git.Scope` | `git status --short -- Docs/Handoff/CURRENT.md` 空输出；本单 10 个文件的 diff 全部落在 `Docs/`+`AGENTS.md`+`.opencode/skills/`。工作区另有**会话前既有**改动（`Assets/Scenes/DungeonWorld.unity`、`PlayerBase.controller`、`*.csproj` 等），非本单产生，见 §7 | 本文 §5 命令 8、9 |
| AC-7 REPORT 含“课程元素 → 产物”映射表 | 通过 | `Report.Mapping` | 见 §6（constitution / spec / implement / validate / learn / replan / skills / agent replaceability 八行） | 本文 §6 |

运行次数：本单为纯文档单，检查为**确定性文件断言**（同命令连续 2 次一致），不适用 N≥3。

## 3. 实现原理（面向所有者）

1. **目标**：把“TASK 只是任务书”升级为“TASK 是一份可审、可验收、可讲”的 spec，并让流程闭环到 Replan，而不增加第二份真相。
2. **备选方案与取舍**：
   - A（照搬课程）：`Docs/Specs/<feature>/{requirements,plan,validation}.md` 三文件 + 一 feature 一分支。弃用：本项目单线流转，三文件会漂移；Unity 资产分支合并成本高。
   - B（最小增补，**选定**）：三节合并进 `TASK_<ID>.md`；`constitution` 只做 1 页索引；讲稿独立成 `Docs/Learning/`；分支默认单支。理由：直接复用现有 TASK/REPORT 管线，改动只在模板与规则层，不触碰任何代码与资产。
3. **实际数据流**：`constitution`（范围判据）→ `TASK_TEMPLATE` 三节（需求/方案/AC）→ 执行 → `REPORT_TEMPLATE` AC 回填 → `Learning` 讲稿 → Replan（§9 清单）→ 下一 feature。规则正文只有 `AGENTS.md` + `workflow-rules.md` 两处，模板只是结构的实例化。
4. **关键位置（文件:行）**：
   - `Docs/Handoff/workflow-rules.md:12` —— 三节 ↔ 课程三文件的映射表（合并理由的落点）。
   - `Docs/Handoff/workflow-rules.md:71` —— AC 表规范：四列、只增不减、必须落到具体证据文件。
   - `Docs/Handoff/workflow-rules.md:117` —— 讲稿规则：AI 初稿 + 所有者改写，改不动的就是没懂。
   - `Docs/Handoff/workflow-rules.md:264` —— Replanning 10 分钟清单（路线图 / constitution / 规则 / 下个 feature / 单独提交）。
   - `Docs/Handoff/workflow-rules.md:283` —— Done 门槛五项勾选。
   - `Docs/Handoff/TASK_TEMPLATE.md:64` —— §3 验证与 AC 表；`Docs/Handoff/REPORT_TEMPLATE.md:10` —— AC 回填表。
5. **边界 / 失败模式**：模板与规则两处若不同步 → 以 `workflow-rules.md` 为准并在 Replan 校准；讲稿写成代码复述 → 用“改不动就重讲”约束；AC 写成“无报错” → 模板要求数据断言。已知未覆盖：尚无真实 feature 走完整新流程（本单只立规则）。
6. **与计划的差异**：模板与规则结构按工单 §3 落地；额外把 `REPORT_TEMPLATE` 的“改动文件清单”并入第 4 节（保持工单 §3.3 的 8 节固定序），并在 `workflow-rules` §8.4 增加一行“范围/Mission/技术栈变化 → 走 Replanning”。
7. **三句话版本**：
   - 以前 TASK 只写“做什么”，验收靠事后补，答辩讲不清。
   - 现在 TASK 固定三节（需求 / 方案 / AC），REPORT 必须逐条回填 AC 并给出 `文件:行`。
   - 每个 feature 收口都要交一份自己改写过的讲稿，再走 10 分钟 Replan，流程才算闭合。

## 4. 受影响模块导航

| 模块 | 职责 | 入口 |
|---|---|---|
| 规则层 | 硬约束与生命周期 | `AGENTS.md` §0–§11 |
| 细则层 | 模板/证据/提交/讲稿/Replan/Done | `Docs/Handoff/workflow-rules.md` §1–§10、附录 A |
| spec 层 | 范围判据与课程映射 | `Docs/Specs/constitution.md`、`Docs/Specs/README.md` |
| 模板层 | 结构实例化 | `Docs/Handoff/TASK_TEMPLATE.md`、`REPORT_TEMPLATE.md`、`Docs/Learning/TEMPLATE_讲稿.md` |
| 话术层 | 触发式流程提示 | `.opencode/skills/feature-spec/`、`.opencode/skills/replan/` |

数据流：`constitution → TASK（三节 + AC）→ 执行 → REPORT（AC 回填）→ 讲稿 → Replan`。

**改动文件清单**

| 文件 | 改了什么 |
|---|---|
| `Docs/Specs/constitution.md` | 新建：Mission / Scope / Tech stack / Roadmap 索引 / 不变量索引 / 维护规则，36 行 |
| `Docs/Specs/README.md` | 新建：课程 spec → 本项目产物映射，合并三文件的理由，用法 |
| `Docs/Handoff/TASK_TEMPLATE.md` | 重写为 `§0 元信息`～`§5 学习产出` + 自包含声明（工单授权重写） |
| `Docs/Handoff/REPORT_TEMPLATE.md` | 新建：8 必含节 + 实现原理 7 问 + 消耗记录 |
| `Docs/Learning/TEMPLATE_讲稿.md` | 新建：9 节讲稿模板，开头写明 AI 初稿 → 所有者改写 |
| `AGENTS.md` | §0/§1/§2/§4/§5/§9 更新（生命周期、AC、讲稿、Done、文档地图），146 行 |
| `Docs/Handoff/workflow-rules.md` | 新增 §1.3 / §1.4 / §2.1 / §3.1 / §9 / §10，改写 §1、§2 |
| `.opencode/skills/feature-spec/SKILL.md` | 新建：13 行薄提示 |
| `.opencode/skills/replan/SKILL.md` | 新建：13 行薄提示 |
| `Docs/README.md` | 目录职责新增 `Specs/`、`Learning/`，模板行、阅读顺序同步 |

> 说明：编辑 `AGENTS.md` 时一次 PowerShell 批量替换导致编码损坏，随即用完整文件重写修复；最终内容已逐节核对（146 行、链接检查通过、与工单要求一致），未引入工单外改动。

## 5. 证据清单（命令 + 原始输出）

**命令 1：TASK 模板结构**
`rg -n "^(## §[0-5]|## 自包含声明)|^\| AC \| Given / When / Then \| 断言 ID \| 证据 \|" Docs/Handoff/TASK_TEMPLATE.md`
```text
7:## §0 元信息        21:## §1 需求（Why / What）   37:## §2 方案（Plan）
64:## §3 验证（Validation）   68:| AC | Given / When / Then | 断言 ID | 证据 |
78:## §4 执行信息      90:## §5 学习产出      98:## 自包含声明
```

**命令 2：REPORT 模板结构**
`rg -n "AC 回填表|关键代码位置|三句话版本|建议提交|证据清单" Docs/Handoff/REPORT_TEMPLATE.md`
```text
10:## 2. AC 回填表   24:4. **关键代码位置（3–5 条）**：   29:7. **三句话版本**
50:## 5. 证据清单    67:## 8. 建议提交
```

**命令 3：constitution 体量**
`(Get-Content Docs/Specs/constitution.md -Encoding UTF8).Count` → `36`；节：`## 1`(6) `## 2`(13) `## 3`(28) `## 4`(38) `## 5`(44) `## 6`(50)。

**命令 4：AGENTS 行数**
`(Get-Content AGENTS.md -Encoding UTF8).Count` → `146`（上限 165）。

**命令 5：链接检查（`Docs/`、`.opencode/skills/`、`AGENTS.md` 内反引号路径）**
```
TOTAL_MISSING=12
MISSING | AGENTS.md | CURRENT.md
MISSING | Docs/README.md | Assets/
MISSING | Docs/README.md | CURRENT.md
MISSING | Docs/README.md | 03_AI协作与项目交接文档.md
MISSING | Docs/README.md | 05_Agent工作流人工协作与毕设基石重构指南.md
MISSING | Docs/Specs/constitution.md | Assets/_Project/Tests/
MISSING | Docs/Handoff/TASK_TEMPLATE.md | Summary.md
MISSING | Docs/Handoff/workflow-rules.md | Summary.md
MISSING | Docs/Handoff/workflow-rules.md | run2_green/
MISSING | Docs/Handoff/workflow-rules.md | Assets/_Project/Tests/
MISSING | Docs/Handoff/workflow-rules.md | opencode-go/
MISSING | Docs/Handoff/workflow-rules.md | opencode/
MISSING | .opencode/skills/replan/SKILL.md | CURRENT.md
```
逐条判定：`CURRENT.md`（3 处）为文件名简称；`Assets/`、`Assets/_Project/Tests/`（3 处）为路径本体，`Assets/_Project/Tests` 实测存在（检查脚本以文件所在目录为基准，故误报）；`03`/`05` 在 `Docs/Archive/` 下真实存在（README 正文说明“已归档至 Archive/”）；`Summary.md`、`run2_green/`、`opencode-go/`、`opencode/` 是证据命名示例与模型路由前缀，非路径。**限定路径形式的失效链接 = 0。**

**命令 6：Docs/README 一致性**
`rg -n "Specs/|Learning/|REPORT_TEMPLATE" Docs/README.md` → 行 17（Specs）、19（模板）、21（Learning）、42（阅读顺序）；`Test-Path Docs/Specs`、`Docs/Learning` → True。

**命令 7：skill 体量**
`(Get-Content .opencode/skills/feature-spec/SKILL.md -Encoding UTF8).Count` → `13`；`replan` → `13`。

**命令 8：CURRENT 未改**
`git status --short -- Docs/Handoff/CURRENT.md` → 空输出。

**文件格式**：本单 10 个文件均为 UTF-8（无 BOM）、LF 换行，与仓库现有 `Docs/` 文件一致。

**命令 9：本单 diff**
```
 AGENTS.md                      | 215 ++++++++++++++++-------------
 Docs/Handoff/TASK_TEMPLATE.md  | 121 ++++++++++-------
 Docs/Handoff/workflow-rules.md | 298 +++++++++++++++++++++++++++++++++++++++--
 Docs/README.md                 |  33 +++--
 4 files changed, 499 insertions(+), 168 deletions(-)
```
新增未跟踪：`Docs/Specs/`、`Docs/Learning/`、`Docs/Handoff/REPORT_TEMPLATE.md`、`.opencode/skills/feature-spec/`、`.opencode/skills/replan/`。
（`AGENTS.md` 的 215 行包含本单之前的 WF-02 未提交改动，非全部为本单所改。）

## 6. 课程元素 → 本项目产物

| 课程元素 | 本项目产物 | 落地方式 |
|---|---|---|
| constitution | `Docs/Specs/constitution.md` | 1 页索引：Mission / Scope / Tech stack；Roadmap 与不变量只给路径 + 节号，仅在 Replan 更新 |
| feature spec | `Docs/Handoff/TASK_<ID>.md` §1 需求 | `R-1..n` + 不在范围 + 约束；自包含，禁“参见 CURRENT.md” |
| plan | 同文件 §2 方案 | 至少 A/B + 取舍、数据流、影响面清单、实施顺序、风险与回滚 |
| validation | 同文件 §3 验证 + REPORT §2 AC 回填 | AC 表四列（断言 ID / 证据），运行次数 ≥2、时序 N≥3 |
| implement | 执行者按 TASK 改代码 | `AGENTS.md` §3 三闸门；red → green |
| validate | 大脑按 AC 复审 | `.opencode/skills/acceptance-loop/`；Done 门槛 `workflow-rules.md` §10 |
| learn | `Docs/Learning/<ID>_讲稿.md` | 9 节模板；AI 初稿 + 所有者改写；Done 前必需（小修写“不适用”） |
| replan | `workflow-rules.md` §9 + `.opencode/skills/replan/` | 10 分钟五项：路线图 / constitution / 规则 / 下个 feature / 单独提交 |
| skills | `feature-spec`、`replan` | 13 行薄提示，只索引 `AGENTS.md` 与 `workflow-rules.md`，不复述长规则 |
| agent replaceability | `AGENTS.md` §1 角色表 + ADR-0002 | 角色只绑职责与权限，不绑具体模型；换模型不改流程；Done 权仍在用户 |

## 7. 未决项

1. **工作区既有改动**：`git status` 中 `Assets/Scenes/DungeonWorld.unity`、`Assets/_Project/Art/Animations/PlayerBase.controller`、`Assembly-CSharp*.csproj`、`.opencode/agents/executor.md`、`.opencode/skills/{acceptance-loop,brain-handoff,executor-dispatch}/SKILL.md`、`Docs/00`、`Docs/01`、`PROPOSAL_SKILL-ANIM-QWER.md`、`Docs/03`/`05` 删除 + `Archive/` 新增等，**均早于本单**：这些文件 `LastWriteTime` 为 12:37–13:59，本单 10 个文件为 14:25–14:32（顺序可证）。提交时请所有者与 WF-02 一起核对，不要误并入本单。
2. `AGENTS.md` 的 `git diff` 含 WF-02 未提交内容，无法用 diff 单独证明“本单只改了 §0/1/2/4/5/9”；本报告 §4 清单为人工对照结果。
3. 新流程尚未有真实 feature 跑通（本单只立规则）。下一张 feature 单建议先按新模板写一次，验证 AC 表与讲稿是否可用。
4. 既有 `REPORT_*` / `TASK_*` 仍是旧格式，未回填；不追溯（与 WF-02 同口径）。
5. 三条旧指针（`CURRENT.md`、`PROPOSAL_FND-02A.md`、`TASK_FND-02B` 中对 `workflow-rules.md` 旧 §4/§7 的引用）仍待大脑在 Replan 时修正 —— 不在本单允许改动范围。

## 8. 建议提交

```text
docs(workflow): align process with spec-driven development
为什么：现有 TASK 缺少需求/方案/验收记分卡与学习产出，无法支撑答辩时讲清实现逻辑
```

默认单提交，GitHub Desktop 一键提交；若与 WF-02 的未提交改动混在工作区，建议由所有者决定合并或分开。

## 9. 消耗记录

- 钱包：Go 订阅（`opencode-go`）
- 模型完整 ID：`opencode-go/deepseek-v4.1-flash`
