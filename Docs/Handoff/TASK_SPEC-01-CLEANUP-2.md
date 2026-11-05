# TASK_SPEC-01-CLEANUP-2 — SPEC-01 收尾：FND02B 退役 + FND00B06 单段口径对齐

> 状态：`Ready`（所有者 2026-11-05 批准收尾单）
> 风险：**低风险**（纯 `.cs` 删除/对齐 + 证据整理；零资产）
> 模式：长任务模式收尾（`AGENTS.md` §4）——熔断即停、不标 Done、不改 `CURRENT.md`
> 前置单：`Docs/Handoff/TASK_SPEC-01-CLEANUP.md`（已执行，报告 `REPORT_SPEC-01-CLEANUP.md`）
> 回滚锚：`7cf874f7c5db448215b33a90fcad1d209f1fb781`（git 已有；工作区改动可由所有者一次 Revert）
> 唯一报告：`Docs/Handoff/REPORT_SPEC-01-CLEANUP.md`（**追加 §收尾**，不新建文件）
> 证据根：`Docs/QA/Evidence/CLEANUP/step5/`

---

## 0. 所有者裁决（原文）

| 编号 | 裁决 |
|---|---|
| **D1** | **整体删除 FND02B 套件**。删除前须出「剩余 3 条断言 × SPEC01 8 条」逐条对照表；**若发现 FND02B 独有覆盖，授权执行者「保留独有、删其余」，无需二次停批**；对照表照常进 REPORT 留档 |
| **D2-a** | `FND00B06 Animation.Attack` **改夹具口径对齐**：骑士按单段 `AttackMelee`（状态名＝触发器名），**断言保留不删** |
| **D2-b** | `FND00B06 Animation.Hit` **冻结**：所有者将先编辑器手动验证（打一次伤害看 `Hit_A` 是否播放）后再定修夹具或立缺陷单。**本单一行不得触碰该断言**（含其取样时机、期望值、日志） |
| **D3** | `CLEANUP/_backup/` 快照**本单执行删除**；其余证据保留 |
| **D4** | `.csproj` 属 Unity 自动重生成噪音：**提交时取消勾选**，归档说明注一句即可 |

---

## 1. 步骤（严格顺序）

### Step A — 覆盖对照（只读，先出表）

对象：FND02B 剩余 3 条断言
`Locomotion.AllPlayersMoveParam`、`Attack.DualPathUnified`、`Clock.MeleeNormalizedWindow`
对照对象：SPEC01 8 条
`Locomotion.AllPlayersMoveParam`、`Regression.FND02A`、`Attack.DualPathUnified`、`Clock.MeleeNormalizedWindow`、`Clock.RangedOneShotPerCycle`、`Animation.PostCombatAndHit`、`UI.DamageNumberScale`、`UI.SpawnAnchor`

逐条列：断言名 / 判定字段 / 判定条件 / 对应 SPEC01 条目 / 覆盖结论（完全覆盖 / 部分覆盖 / 独有）。

产出 `Docs/QA/Evidence/CLEANUP/step5/coverage_matrix.md`。

- 全部完全覆盖 → 进 Step B。
- 存在**部分覆盖或独有** → **保留该断言所在的独有部分，删除其余被覆盖部分**（授权范围内，不停批），并在对照表与 REPORT 中写明保留理由与保留后的套件形态。

### Step B — 退役 FND02B 套件

按 Step A 结论删除：
- 若整体退役：删除 `Assets/_Project/Tests/FND02B/` 整目录（`Editor/`、`Runtime/`、全部 `.cs` 与 `.cs.meta`、目录 `.meta`）。
- 若部分保留：仅删除被覆盖的断言与方法，保留独有断言及套件骨架。

**不动**：`Docs/QA/Evidence/FND-02B/`（历史证据）、`Docs/Handoff/REPORT_FND-02B.md`、`Docs/Handoff/TASK_FND-02B_*.md`、`PROPOSAL_FND-02B.md`。
`.csproj` 由 Unity 自动重生成，**不手改**。

### Step C — FND00B06 `Animation.Attack` 口径对齐

- `KnownStates` 增加 `AttackMelee`；
- 断言期望改为：骑士出招进入 **`AttackMelee`**（单段，状态名＝触发器名），不再等 `Attack_Draw/Attack_Release`；
- `AttackSpeed` 参数比对逻辑**保留**；
- **断言保留**，仅改期望口径；
- `Animation.Hit` 及其取样代码、期望、日志字段：**一行不动**。

### Step D — 证据整理

- 删除 `Docs/QA/Evidence/CLEANUP/_backup/`（整目录）。
- 保留 `step0~step5/`、`diff/`、`Runs.jsonl`。
- 在 `CLEANUP/Runs.jsonl` 追加本单各轮记录。

### Step E — REPORT 追加 §收尾

`Docs/Handoff/REPORT_SPEC-01-CLEANUP.md` 追加：
1. 覆盖对照表全文 + 覆盖结论；
2. 删除清单（精确路径）与保留清单（若有）；
3. FND00B06 前后红项对照（`Animation.Attack` 转绿、`Animation.Hit` 保持红并标注**冻结**）；
4. `Animation.Hit` 冻结声明（所有者手动验证中，执行者未触碰）；
5. **证据文件处置说明**：`_backup/` 已删、其余保留、总量；
6. 提交拆分建议（见 §4，含 `.csproj` 取消勾选说明）。

---

## 2. 验收

| 项 | 标准 |
|---|---|
| 编译 | 全轮 `error CS` = 0，`infraFail=0` |
| SPEC01 | 1 轮 **8/8 pass**（确认删除未伤编译与运行） |
| FND00B06 | 1 轮：`Animation.Attack` **转绿**；仅剩 `Animation.Hit` 红（冻结）；**无新增红** |
| 代码引用 | `Assets/**/*.cs` 中 `FND02B` 归零（若部分保留，则剩余引用仅为独有断言） |
| 对照表 | 3 条 × 8 条齐全，结论明确，已进 REPORT |
| 证据 | `_backup/` 不存在；`step5/` 存在 |

任一轮出现基线外新红 → 熔断停止。

---

## 3. 边界（绝不触碰）

- `.prefab` / `.controller` / `.unity` / `.asset` / `.overrideController`：只读；任何 `.meta` 的 `guid` 绝不改。
- `FND00B06 Animation.Hit` 相关的一切代码与日志字段：**禁改**。
- 生产脚本 `MeleeCombat.cs` / `DamageNumber.cs` / `CombatFeedback.cs` / `RangedCombat.cs`：**零改动**。
- `CURRENT.md`、`AGENTS.md`、`Docs/00`、`Docs/01`、`Docs/03`、`Docs/Archive`、历史 `REPORT_*`、`Docs/QA/Evidence/{SPEC-01,FND-02B,FND-00B-06}/`：不动。
- B2（`Interrupt` 无过渡）/ B6：不回填。
- **不标任何单 Done**；**无任何 git 写操作**（仅允许只读 `git rev-parse` / `git status`）。

## 4. 提交拆分建议（写入 REPORT）

1. `refactor(combat): SwordSwing → MeleeCombat`
   脚本改名 + 4 个生产引用 + 2 个测试引用；**`.csproj` 为 Unity 自动重生成噪音，提交时取消勾选**。
2. `test: 对齐 FND00B06 单段口径并退役 FND02B 套件`
3. `docs: API 文档类名与攻击触发器口径同步 + SPEC-01-CLEANUP 报告与证据`
4. `chore: 删除 CLEANUP 临时快照`（可并入 3）

## 5. 熔断

单步重试 ≥3 仍红／编译错连续 ≥2 轮／计划外硬阻断／git 异常／配额撞限 → 立即停并留现场报告（最后命令、`git status --porcelain -uall`、日志尾 20 行、已完成/未完成步骤）。

## 6. 效率与报告纪律

- 禁止整文件读取日志/大文档，用 `grep`/`Select-String` 定向提取；结果超 50 行只留计数 + 前 10 条示例。
- 报告不设字数上限，讲清即止，禁止为压字数反复重写。
