# TASK_SPEC-01-CLEANUP — SPEC-01 收口尾巴清理（长任务模式·三步夜间批）

> 状态：`Ready`（所有者 2026-11-05 批准计划书，含 P1/P2/P3 裁决）
> 风险：**低风险**（全 `.cs` / `.md`，零资产改动）
> 模式：**长任务模式**（`AGENTS.md` §4）——自主运行、范围锁死、熔断即停、不标 Done、不改 `CURRENT.md`
> 回滚锚：`7cf874f7c5db448215b33a90fcad1d209f1fb781`（工作区已核实 clean）
> 唯一报告：`Docs/Handoff/REPORT_SPEC-01-CLEANUP.md`
> 唯一证据根：`Docs/QA/Evidence/CLEANUP/`
> 醒来目检：`Docs/Handoff/醒来目检清单_SPEC-01-CLEANUP.md`

---

## 0. 所有者裁决（覆盖任何歧义）

| 编号 | 裁决 |
|---|---|
| **P1** | Step 1 验收口径＝**SPEC01 全绿 + FND02B「除已知 1 条 `UI.DamageNumberScale` 外全绿、无新增红」**；Step 2② 完成后再要求 FND02B 全绿 |
| **P2（修正）** | FND00B06 若红项**与基线红项集合一致** → Step 2① 改动视为有效、**Step 3 照常执行**；FND00B06 作为**「醒来决策项」**单列进报告（红项名／基线对照／修复建议），**不得自行修复**。若出现**基线之外的新红** → 立即熔断停止 |
| **P3** | 选**方案 A（删除）**：删除 `FND02B_ProbeRunner.AssertDamageNumberScale()` 与注册行，文件头注释登记「UI 尺寸断言已由 SPEC-01 套件覆盖」 |

---

## 1. 目标

清掉 SPEC-01 收口清单里的三条代码侧尾巴：① `SwordSwing` 类名对齐 `RangedCombat`；② FND00B06 硬编码与 FND02B 过时 UI 断言；③ API 文档类名与攻击触发器口径同步。

## 2. 已核实现状（执行者不必重复大范围扫描）

| 项 | 事实 |
|---|---|
| 脚本 GUID | `SwordSwing.cs.meta` = `9ec9f7a6099e5204fb03ca3a12b8afdd` |
| 按 GUID 引用该脚本的预制体 | `Assets/_Project/Prefabs/Knight_Unit.prefab:185`、`Assets/StarterAssets/FirstPersonController/Prefabs/PlayerCapsule.prefab:287` |
| 生产引用点 | `Units/SkillSystem.cs:12`、`Core/GameData.cs:19`、`UI/HUD.cs:44`；`Combat/SkillEffects.cs:77` 仅注释 |
| 测试引用点 | `Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs`（约 5 处）、`Tests/FND00B/Runtime/FND00B06_ProbeRunner.cs`（约 11 处）、`Tests/FND02B/Runtime/FND02B_ProbeRunner.cs`（约 3 处） |
| FND00B06 硬编码 | `FND00B06_ProbeRunner.cs:43` `" Locomotion"`；`:44` `"Attack"` |
| FND02B 过时断言 | `FND02B_ProbeRunner.cs:204` 注册；`:863-929` 实现（读已删除的 `CombatFeedback.damageNumberScale`） |
| API 文档 | `docx/API文档_魔石地牢_v1.0.md`（`:161`、`:422`、`:850`、`:898`、`:900`） |
| 套件入口 | `SPEC01_BatchAcceptance.Run`、`FND02B_BatchAcceptance.Run`、`FND00B06_BatchAcceptance.Run` |

---

## 3. 步骤（严格顺序，每步 green 才进下一步）

### Step 0 — 前置与基线

1. 自证 Unity 关闭：无 `Unity.exe` 进程、无 `Temp/UnityLockfile`；写 `CLEANUP/step0/00_前置确认.txt`。
2. 记录 `git rev-parse HEAD`（应为 `7cf874f7c5db448215b33a90fcad1d209f1fb781`）与 `git status --porcelain -uall`（应为空）；不符 → 停止报告。
3. 基线跑三套件各 1 轮，**逐条记录红项名**：`SPEC01`、`FND02B`、`FND00B06`。
   证据：`CLEANUP/step0/{suite}_{Assertions.jsonl,Automation.log,Summary.json,ExitCode.txt}`。
4. `SPEC01` 基线若出现任何红 → **立即停止**（说明收口后环境已变）。

### Step 1 — `SwordSwing` → `MeleeCombat`

**改动**：
- 文件改名 `Assets/_Project/Scripts/Combat/SwordSwing.cs` → `MeleeCombat.cs`；`.meta` **同名随迁、GUID 字段原样**（`9ec9f7a6…`）。这是本单**唯一允许触碰的 `.meta`**。
- 类名 `public class SwordSwing` → `public class MeleeCombat`。
- 引用同步：上表生产 3 处 + 注释 1 处 + 3 个测试文件全部 `SwordSwing` → `MeleeCombat`（含字段类型 `public SwordSwing sword;`）。
- `Assembly-CSharp.csproj` / `Assembly-CSharp-Editor.csproj` 由 Unity 自动重生成，**不手改**。
- **严禁改逻辑**：方法体、字段、默认值、特性一律原样，只换标识符与文件名。

**验收**：
1. `Select-String` 全仓 `Assets/**/*.cs` 中 `SwordSwing` 归零。
2. `MeleeCombat.cs.meta` 的 `guid:` 仍为 `9ec9f7a6099e5204fb03ca3a12b8afdd`；两处预制体 `m_Script` guid 未变（**只读**核对，不写资产）。
3. `SPEC01` 全绿 1 轮；`FND02B` 按 **P1** 口径（仅 `UI.DamageNumberScale` 红、无新增红）。
4. 报告写三行摘要。

### Step 2 — 夹具双清

**2①** `FND00B06_ProbeRunner.cs:43` `" Locomotion"` → `"Locomotion"`；`:44` `"Attack"` → `"AttackRange"`。**只改这两个字符串**。

**2②（方案 A）** `FND02B_ProbeRunner.cs`：删除 `AssertDamageNumberScale()` 方法体、删除 `:204` 的 `yield return RunSafe("UI.DamageNumberScale", ...)` 注册行；文件头注释增加一行「`UI.DamageNumberScale` 已由 SPEC-01 套件覆盖（SPEC-01 收口，2026-11-05）」。**不得改动其他断言。**

**验收**：
- 跑 `FND00B06` 1 轮 + `FND02B` 1 轮。
- `FND02B` 必须**全绿**（删除后 6/6）。
- `FND00B06` 按 **P2（修正）**判定：
  - 红项集合 == 基线 → 记录为「醒来决策项」，**继续 Step 3**，不修；
  - 出现基线外新红 → **熔断停止**。
- 证据：`CLEANUP/step2/`。

### Step 3 — 文档同步（无 Unity 运行）

- `docx/API文档_魔石地牢_v1.0.md`：
  - 全文 `SwordSwing` → `MeleeCombat`（`:161`、`:422`、`:850`、`:898`、`:900`）。
  - 攻击触发器/状态口径落文：**近战 `AttackMelee`（状态名＝触发器名，单段）／远程 `AttackRange`（两段式 `Attack_Draw → Attack_Release`）**。
  - `:900` 合规确认行同步类名。
- **不碰** `Docs/01`、`Docs/03`、`Docs/04`、`Docs/Archive`、任何 `REPORT_*`、`CURRENT.md`、`AGENTS.md`。

**验收**：该文档内 `SwordSwing` 归零；diff 仅此一文件。

### Step 4 — 收口产出

- `Docs/Handoff/REPORT_SPEC-01-CLEANUP.md`：分步结果、每步命令、逐断言表、完整 diff、变更文件、未覆盖项、实现原理、模块导航、遗留问题、**醒来决策项**（FND00B06 红项／基线对照／修复建议）、建议提交（拆分 commit）。
- `Docs/Handoff/醒来目检清单_SPEC-01-CLEANUP.md`：列出所有待所有者目检项（至少含：骑士普攻手感、弓/法两段、飘字、改名后 Inspector 组件显示名、API 文档可读性）。
- 证据索引：`CLEANUP/Runs.jsonl`。

---

## 4. 硬性边界（绝不触碰）

- `.prefab` / `.controller` / `.unity` / `.asset` / `.overrideController`：**只读**。
- 除 `MeleeCombat.cs.meta` 随迁外的一切 `.meta`：**不改**；任何 `.meta` 的 `guid`：**绝不改**。
- 三个生产脚本逻辑：`MeleeCombat`（仅改名）、`DamageNumber.cs`、`CombatFeedback.cs`、`RangedCombat.cs` —— **逻辑零改动**。
- `CURRENT.md`、`AGENTS.md`、`Docs/00`、`Docs/01`、`Docs/03`、`Docs/Archive`、历史 `REPORT_*`、`Docs/QA/Evidence/{SPEC-01,FND-02B,FND-00B-06}/` 既有证据。
- B2（`Interrupt` 无过渡）/ B6（FND00B06 硬编码历史）**不回填、不修复**。
- **不标任何单 Done**；**无任何 git 写操作**（commit/add/stash/checkout/branch）。

## 5. 熔断（任一触发即停并留现场报告）

1. 单步重试 ≥3 次仍红；
2. 编译错连续 ≥2 轮；
3. 计划外硬阻断（如需改资产、装包、升 Unity、目录迁移）；
4. git 异常（HEAD 不符、工作区非预期脏）；
5. 配额撞限。

**停时现场报告须含**：最后一条命令、`git status --porcelain -uall`、最后日志尾 20 行、已完成/未完成步骤、已绿步骤是否仍有效。

## 6. 效率与报告纪律

- 禁止整文件读取日志/大文档，用 `grep`/`Select-String` 定向提取（`-Context 2`）；结果超 50 行只留计数 + 前 10 条示例。
- 报告**不设字数上限**，讲清即止，**禁止为压字数反复重写**。
