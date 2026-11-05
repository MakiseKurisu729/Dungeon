# INTERRUPT-01 — 移动指令打断普攻/交互，技能不可被移动打断

## §0 元信息

| 字段 | 值 |
|---|---|
| 任务 ID | `INTERRUPT-01` |
| 风险等级 | `高风险`（Animator 过渡 + 战斗/交互管线；改动面小但属伤害与动画契约） |
| 授权 | 所有者 2026-11-05：“我希望普攻、交互等动作都能被移动指令打断，但放技能的时候不行” |
| 基线提交 | `6872348b` + 未提交批次（Phase A / DOWNED-01 等） |
| 回滚锚 | `git checkout -- Assets/_Project/Art/Animations/PlayerBase.controller Assets/_Project/Scripts/Units/CharacterBrain.cs` |
| 分支 | 当前分支 |
| 状态 | `Ready` |

## §1 需求（Why / What）

**一句话目标**：给单位下达移动指令时，普攻与交互**立刻让位**给移动；正在释放的技能**不被打断**。

**用户故事**：作为玩家，我点错目标或想风筝走位时，希望角色立刻跑起来，而不是把这一刀挥完；但技能是我主动投入资源放的，不该被一次误点取消。

**必须满足**

| 编号 | 需求 | 验收方式 |
|---|---|---|
| R-1 | `AttackMelee` 中下达移动指令 → 0.15s 内进入 `Locomotion`，且本次普攻不再结算伤害 | `Interrupt.AttackByMove` |
| R-2 | `Interact` 中下达移动指令 → 0.15s 内进入 `Locomotion`，交互不再继续 | `Interrupt.InteractByMove` |
| R-3 | `Q/W/E/R` 或 `Bash`/`Skill` 状态中下达移动指令 → 动画**不切走**，技能结算照常完成 | `Interrupt.SkillImmuneToMove` |
| R-4 | 打断只作用于“下达移动指令的那一次”，不得残留触发器导致下一次普攻被立刻打断 | `Interrupt.NoStaleTrigger` |
| R-5 | 不改伤害公式、技能数值、场景/预制体/覆盖器；SPEC01 不回归 | `all` |

**不在范围**：受击打断（已有 `AnyState→Hit_A`）、救援读条的打断（归 `RESCUE-01`）、移动打断后的音效/VFX。

**约束**：`Interrupt` 参数已存在但**当前无任何过渡使用**；本单只新增 `AttackMelee→Locomotion` 与 `Interact→Locomotion` 两条过渡，其余状态（`Bash`/`Skill`/`Q/W/E/R`/`Attack_Draw`/`Attack_Release`/`Downed`/`Rise`）**一律不加**。

## §2 方案（Plan）

**备选方案**

| 方案 | 做法 | 优点 | 缺点 | 结论 |
|---|---|---|---|---|
| A | 给 `AttackMelee`/`Interact` 各加一条 `→Locomotion` 的过渡，条件用已有的 `Interrupt` 触发器；代码在移动指令时按当前状态决定是否发触发器 | 语义单一（“打断”就是一根触发器）；技能免疫靠“没有这条过渡”天然成立 | 需要状态判断，避免残留触发器 | **选定** |
| B | 用 `SpeedZ/SpeedX` 直接作为过渡条件（移动就切） | 不用改代码 | 技能中移动也会切走，违反 R-3；且摇杆微动会误打断 | 弃用 |
| C | 代码里强行 `Animator.Play("Locomotion")` | 简单 | 绕过过渡、破坏状态机语义，覆盖器/多段技能都会出问题 | 弃用 |

**选定方案与理由**：A 让“谁能被移动打断”完全由状态图决定——技能状态没有这条过渡，所以移动天然打不断，不需要在代码里维护白名单。

**数据流**

```
右键移动 → CharacterBrain.OnCommandMove
  ├ 若 Animator 当前状态 ∈ {AttackMelee, Interact} → animator.SetTrigger("Interrupt")
  ├ 既有：combat.CancelAttack()（停计时）
  └ CommandMove → NavMeshAgent.SetDestination
Controller：AttackMelee/Interact --(Interrupt, HasExitTime=0, Source, 0.1s)--> Locomotion
```

**过渡参数（必须逐项一致）**

| 源 | 目标 | 条件 | HasExitTime | ExitTime | Duration | InterruptionSource | OrderedInterruption | CanTransitionToSelf |
|---|---|---|---|---|---|---|---|---|
| `AttackMelee` | `Locomotion` | `Interrupt`（If） | false | 0 | 0.1 | `Source` | true | false |
| `Interact` | `Locomotion` | `Interrupt`（If） | false | 0 | 0.1 | `Source` | true | false |

**代码改动（`CharacterBrain.OnCommandMove`）**

在调用 `c.CancelAttack()` 之后追加：

```csharp
// 只有可被移动打断的状态才发 Interrupt；技能状态不发，避免残留触发器误伤下一次普攻
if (unitAnimator != null)
{
    var st = unitAnimator.GetCurrentAnimatorStateInfo(0);
    if (st.IsName("AttackMelee") || st.IsName("Interact"))
        unitAnimator.SetTrigger("Interrupt");
}
```

若 `OnCommandMove` 里没有 `unitAnimator` 字段，用 `GetComponentInChildren<Animator>()` 缓存到字段（只加字段，不改别的）。

**影响面清单**

| 项 | 内容 |
|---|---|
| 新增文件 | `Docs/QA/Evidence/INTERRUPT-01/**`、`REPORT_INTERRUPT-01.md` |
| 修改文件 | `PlayerBase.controller`（2 条过渡）、`CharacterBrain.cs`（+≤8 行） |
| 字符串引用 | `Interrupt`、`AttackMelee`、`Interact`、`Locomotion` |
| 场景/预制体 | 不改 |
| `.meta` | 无 |

**实施顺序**：控制器加 2 条过渡 → 代码加状态判断 → 编译 → 定点验证（三段） → SPEC01 回归 → 证据。

**风险与回滚**：最大风险是“残留 `Interrupt` 触发器”导致下一次普攻秒断（与 `DOWNED-01` 的 `Hit` 残留同类）。R-4 专门验这个：连续做「技能 → 移动 → 立刻普攻」，普攻必须正常播完。回滚 = 还原 2 个文件。

## §3 验证（Validation）

| AC | Given / When / Then | 断言 ID | 证据 |
|---|---|---|---|
| AC-1 | 给定 Play，当对 `Knight_Unit` 下普攻指令后 0.1s 内下移动指令，则 0.15s 内状态为 `Locomotion`，且该次普攻的伤害未结算（目标 HP 不变） | `Interrupt.AttackByMove` | `Docs/QA/Evidence/INTERRUPT-01/attack_by_move.txt` |
| AC-2 | 给定 Play，当交互中（`Interact` 状态）下移动指令，则 0.15s 内进入 `Locomotion`，交互进度不再推进 | `Interrupt.InteractByMove` | `interact_by_move.txt` |
| AC-3 | 给定 Play，当技能（Q）播放中下移动指令，则 1s 内状态始终为 `Q_1`（或 `Bash`，取决于当前代码路径），技能伤害照常结算 | `Interrupt.SkillImmuneToMove` | `skill_immune.txt` |
| AC-4 | 给定「技能中移动 → 技能结束 → 立刻普攻」，则普攻完整播放 ≥ 0.4s 且正常结算一次伤害（无残留触发器） | `Interrupt.NoStaleTrigger` | `no_stale_trigger.txt` |
| AC-5 | 给定最终状态，当 `git status`，则只改 §2 允许的 2 个文件；SPEC01 编辑器内 12/0 | `all` | `git diff --stat`、`editor_runN/…` |

**运行次数**：AC-1/2/3 各 3 次（时序类 N≥3），AC-4 连续 2 次。

**自动化优先**：全部用 `execute_code` 在 Play 中驱动（`SetTrigger`/`CommandMove`/读 `GetCurrentAnimatorStateInfo`），无目检项。

**必须保存的证据**：三段定点验证的原始状态序列 + 伤害数值、`git diff`、SPEC01 一次。

## §4 执行信息

| 项 | 内容 |
|---|---|
| 前置 | Unity 打开、`is_compiling=false`；`DOWNED-01` 已合入（`ResetTrigger("Hit")` 在 `OnDowned` 里） |
| 允许改动 | `PlayerBase.controller`（仅 2 条过渡）、`CharacterBrain.cs`（仅 §2 那段）+ 证据 + REPORT |
| 允许读取 | 本工单、`CharacterBrain.cs`、`MeleeCombat.cs`、`PlayerBase.controller` |
| 绝不触碰 | `OC_*`、场景、预制体、`.asset`、`ProjectSettings`、`Packages`、`SkillSystem`/技能效果（归 Phase B）、`CURRENT.md` |
| MCP 权限 | `execute_code`、`execute_menu_item`（仅 `Tools/QA/In-Editor/*`）、`read_console`、`refresh_unity`、资源读取；其余写工具禁用 |
| 熔断 | 需要改技能状态/技能代码；AC-4 出现残留触发器且一次修复仍失败；SPEC01 由 12 变红 |
| 执行预算 | 实现 1 轮 + 修复 1 轮；不跑 batch |

## §5 学习产出

| 项 | 要求 |
|---|---|
| 讲稿 | **不适用**：过渡配置 + 一处状态判断，讲解点并入 REPORT §3 |
| 三句话版本 | 写进 REPORT §3：“为什么用触发器而不是速度参数 / 为什么技能不加这条过渡就天然免疫 / 残留触发器为什么会误伤下一次攻击” |

## 自包含声明

本工单不依赖 `CURRENT.md` 或其他任务书即可执行。信息不足时在 REPORT 写“信息不足”并列缺项。
