# TASK — WF-03：把现有流程升级为课程标准工作流（Spec → Implement → Validate → Learn → Replan）

> **风险等级：低风险（文档 / 模板 / skills；不改代码与资产）**
> **授权**：所有者 2026-11-05 明确回复“按你说的做”，批准本工单全部交付物。
> **回滚锚**：`1bec9c93`（技能修复提交）。注意：WF-02 的文档改动可能仍未提交，本单在其上继续；提交时与 WF-02 合并或分开由所有者决定。
> **本单不改**：`Assets/**`、`ProjectSettings/**`、`Packages/**`、`Docs/Handoff/CURRENT.md`（由大脑改）、任何既有 `TASK_*` / `REPORT_*` / 证据。

## 1. 目标

把当前流程对齐 DeepLearning.AI × JetBrains《Spec-Driven Development with Coding Agents》的标准循环，同时**减少冗余**：

```
Constitution → Feature Spec（需求/方案/验证）→ Owner 审 spec → Implement
→ Validate（AC 表 + 证据）→ Learn（讲稿）→ Commit → Done → Replan → 下一个 Feature
```

关键取舍（必须写进文档）：
- 课程每个 feature 拆 `requirements/plan/validation` 三个文件 → 本项目**合并为一个 TASK 三节**，避免重复维护。
- 课程每个 feature 开独立分支 → 本项目**不强制**；Unity 场景/预制体合并成本高，仅纯代码重构用短分支。理由写进 workflow-rules。
- 课程 validation 以人工 review 为主 → 本项目**自动化断言 + 证据**更强，保留并升级为 AC 表。
- 新增“Learn（讲稿）”层：AI 初稿 + 所有者改写，解决“能验收但讲不出”的问题。

## 2. 交付物（允许改动的全部文件）

| 文件 | 动作 |
|---|---|
| `Docs/Specs/constitution.md` | 新建（1 页） |
| `Docs/Specs/README.md` | 新建（说明 constitution 与 feature spec 的映射） |
| `Docs/Handoff/TASK_TEMPLATE.md` | 重写为三节模板 |
| `Docs/Handoff/REPORT_TEMPLATE.md` | 新建 |
| `Docs/Learning/TEMPLATE_讲稿.md` | 新建 |
| `AGENTS.md` | 更新生命周期、Done 门槛、文档地图（≤165 行） |
| `Docs/Handoff/workflow-rules.md` | 增加 spec/report/讲稿/replan/Done 细则 |
| `.opencode/skills/feature-spec/SKILL.md` | 新建（薄提示） |
| `.opencode/skills/replan/SKILL.md` | 新建（薄提示） |
| `Docs/README.md` | 更新目录职责与索引 |

**允许读取**：`AGENTS.md`、`Docs/00`、`Docs/01`、`Docs/02`、`Docs/Handoff/workflow-rules.md`、`Docs/README.md`、`Docs/Handoff/TASK_TEMPLATE.md`、现有 `.opencode/skills/*`、`Docs/Decisions/ADR-0002`、`ADR-0003`。

## 3. 内容要求

### 3.1 `Docs/Specs/constitution.md`（1 页）

只写“课程 constitution 的三根柱子”，**不复制** 00/01/02：

1. **Mission**：魔石地牢是什么、为什么做（毕业设计可演示原型）、给谁看（答辩老师/玩家）、成功标准（MVP 闭环可稳定演示）。
2. **Scope / 非目标**：MVP = 走图 → 遇怪 → 普攻/技能 → 宝箱 → 团灭 → R 重来；非目标 = 联机、多楼层、大重构、Unity 6.3 迁移（本轮）。
3. **Tech stack**：Unity 2022.3.62f3c1、C#、数据驱动（SkillData/效果资产）、单层 Animator + Override、目标 Unity 6.3（未迁移）。
4. **Roadmap**：指向 `Docs/02`，只写“当前阶段 + 下一阶段”，不复制细节。
5. **不变量**：指向 `Docs/01 §1.4` 与 ADR-0003。
6. **维护规则**：constitution 只在 Replanning 更新；改架构不变量先写 ADR。

### 3.2 `TASK_TEMPLATE.md`（三节 + 执行信息）

固定结构：

- `§0 元信息`：ID、风险（常规/高风险）、授权、回滚锚、基线提交。
- `§1 需求（Why/What）`：一句话目标；用户故事；`R-1..n` 必须满足；**不在范围**；约束。
- `§2 方案（Plan）`：**至少两个备选方案 A/B + 取舍**；选定方案与理由；数据流（输入→处理→输出）；影响文件/符号/字符串/场景引用；实施顺序；风险与回滚。
- `§3 验证（Validation）`：AC 表：

  | AC | Given/When/Then | 断言 ID | 证据 |
  |---|---|---|---|
  | AC-1 | ... | `Skill.CastSmoke` | `Docs/QA/Evidence/<ID>/...` |

  以及运行次数（常规≥2、时序≥3）。
- `§4 执行`：前置、证据目录、绝不触碰、熔断。
- `§5 学习产出`：feature/高风险任务必须产出 `Docs/Learning/<ID>_讲稿.md`；小修可写“不适用”。
- 末尾：**自包含声明**（禁止“参见 CURRENT.md”式指针）。

### 3.3 `REPORT_TEMPLATE.md`

固定结构：

1. 结果摘要；
2. **AC 回填表**：`AC | 结果 | 断言 ID | 关键数据 | 日志路径`；
3. **实现原理（面向所有者）**，必须回答：
   - 根因/目标；
   - 备选方案与为什么没选；
   - 实际数据流；
   - **3–5 个关键代码位置（`文件:行`）**；
   - 边界/失败模式；
   - 与计划的差异及原因；
   - **三句话版本**（答辩时先讲这三句）；
4. 受影响模块导航；
5. 证据清单；
6. 学习讲稿链接（feature 任务）；
7. 未决项；
8. 建议提交（标题 + 正文“为什么”）。

### 3.4 `Docs/Learning/TEMPLATE_讲稿.md`

1–2 页，结构：

1. 一句话；
2. 原来的问题/为什么需要；
3. 方案对比（A 为什么不行、B 为什么选）；
4. 数据流图（Mermaid 或文字箭头）；
5. 关键代码 3–5 处（`文件:行` + 这段在干嘛）；
6. 为什么这样写（设计取舍）；
7. 边界/失败模式；
8. 答辩 Q&A 5 条；
9. 名词表（可选）。

开头写清楚：**AI 出初稿，所有者用自己的话改写；改不动的地方就是没懂，让 AI 再讲**。

### 3.5 `AGENTS.md` 更新（保留规则书定位，≤165 行）

- §2 生命周期改为：目标 → 大脑写 TASK（三节）→ **所有者审 spec（需求/方案/AC）** → 执行 → 大脑按 AC 验收 → **学习讲稿** → 提交 → Done → Replan。
- §4 验证：增加“AC 表 + 学习讲稿”要求（feature 任务）。
- §5 风险：高风险必须先 ADR；feature 任务 Done 前必须有讲稿。
- §9 文档地图：加入 `Docs/Specs/`、`Docs/Learning/`。
- 不复制 workflow-rules 的模板细节。

### 3.6 `workflow-rules.md` 增补

- Feature spec 模板（三节）与 AC 表。
- Report 模板与实现原理问答清单。
- 学习讲稿模板与“AI 初稿→所有者改写”规则。
- Replanning 10 分钟清单：路线图、constitution、规则/流程、下个 feature、单独提交。
- Done 门槛：AC 全过 + 证据 + 讲稿（feature 任务）+ 用户确认。
- 分支策略：默认单分支；纯代码重构可用短分支；资产改动由所有者提交，不强制分支。
- 提交格式：标题 + 正文“为什么”；GitHub Desktop 一键；不要求分 hunk。

### 3.7 两个新 skill（薄提示）

- `feature-spec`：触发“写 spec/开新功能”；步骤 = 读 constitution + roadmap → 列歧义/缺项 → 写 TASK 三节（含 AC 表）→ 等所有者审。
- `replan`：触发“收口/下一个”；步骤 = 更新 CURRENT（大脑）→ 路线图/constitution/规则 → 10 分钟清单 → 提交标题/正文。

## 4. 验收

1. 模板/文档与 §3 结构一致；`TASK_TEMPLATE` 含 AC 表与“学习产出”节；`REPORT_TEMPLATE` 含 AC 回填与 `文件:行`；
2. constitution 1 页，且只链接不复制 00/01/02；
3. `AGENTS.md` ≤165 行，无失效相对链接；
4. `Docs/README.md` 与实际目录一致（新增 Specs/Learning）；
5. 两个新 skill 只引用 AGENTS/workflow-rules，不重复长规则；
6. `git status` 无 `Assets/**`、`ProjectSettings/**`、`Packages/**`；`CURRENT.md` 未改；
7. `REPORT_WF-03.md` 含“课程元素 → 本项目产物”映射表（constitution / spec / implement / validate / learn / replan / skills / agent replaceability）。

## 5. 报告

`Docs/Handoff/REPORT_WF-03.md`：结果摘要、改动清单、映射表、链接检查、`git diff --stat`、遗留问题、建议提交（标题+正文）。

## 6. 熔断

需要改代码/资产、需要删除既有任务/报告、链接无法保持、AGENTS 超行数且无法压缩、工作区出现他人改动 —— 停止并报告。

## 7. 建议提交（预览）

标题：`docs(workflow): align process with spec-driven development`

正文：`为什么：现有 TASK 缺少需求/方案/验收记分卡与学习产出，无法支撑答辩时讲清实现逻辑`
