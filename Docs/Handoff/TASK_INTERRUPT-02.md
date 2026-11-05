# INTERRUPT-02 — 过渡线布局、远程前后摇打断、技能期间指令锁定

## §0 元信息

| 字段 | 值 |
|---|---|
| 任务 ID | `INTERRUPT-02` |
| 风险等级 | `高风险`（Animator 过渡 + 战斗指令入口；改动面小） |
| 授权 | 所有者 2026-11-05 目检反馈：①过渡线穿状态块；②法师前摇没打断；③W 技能被移动打断且没出伤，要求“技能释放期间其他指令无效” |
| 基线提交 | `9e0b7515` + 未提交的第二轮批次 |
| 回滚锚 | `git checkout -- Assets/_Project/Art/Animations/PlayerBase.controller Assets/_Project/Scripts/Units/CharacterBrain.cs` |
| 状态 | `Ready` |

## §1 需求（Why / What）

**一句话目标**：动画机看得懂、远程普攻的前后摇能被移动打断、技能一旦起手就“锁住”其它指令。

| 编号 | 需求 | 验收方式 |
|---|---|---|
| R-1 | 动画机节点重排后，`AnyState→X` 与 `X→Locomotion` 的连线尽量不穿过其它状态块（直线走廊优先） | 目检 + 位置读回 |
| R-2 | `Attack_Draw`、`Attack_Release` 中下达移动指令 → 0.15s 内回 `Locomotion`，远程不再出箭/出火球 | `Interrupt.RangedDraw` |
| R-3 | 技能起手后（Animator 在 `Q_1/W_1/E_1/R_1/Skill/Bash`）**移动指令无效**：不 SetDestination、不切动画、不取消技能计时 | `Skill.LockMove` |
| R-4 | 技能期间**攻击/交互指令同样无效**（不切换目标、不进入交互），技能自然结束后恢复 | `Skill.LockOther` |
| R-5 | 技能结束时指令锁解除，下一次移动/攻击正常响应（无残留） | `Skill.UnlockAfter` |
| R-6 | 不改伤害公式、技能数值、场景/预制体/覆盖器；SPEC01 12/0 | `all` |

**不在范围**：Q 技能改为播 `Q_1`（归 `SKILL-ANIM-02` Phase B）、被动/持续施法、多段技能内容。

**约束**：`Interrupt` 触发器仍只用于“移动打断普攻/交互”；技能免疫靠“指令锁”实现，**不要**给技能状态加 `Interrupt` 过渡。

## §2 方案（Plan）

**A. 布局（只改坐标）**

Unity 的过渡线是自动路由的，不能手动加折点。能做到“不穿块”的唯一手段是**摆位**：把 `Locomotion` 放最左，动作状态在中间排成一列（同 x、等距 y），`AnyState` 放在列顶上方，`Q/W/E/R` 子机放最右列。这样 `AnyState→X` 的线沿列外侧下行、`X→Locomotion` 的线向左穿过空走廊。

目标坐标（若与现有节点冲突可微调，但必须保持“三列 + 列间留白 ≥ 200”）：

| 节点 | 位置 |
|---|---|
| `Locomotion` | (80, 620) |
| `AttackMelee` | (520, 20) |
| `Attack_Draw` | (520, 170) |
| `Attack_Release` | (520, 320) |
| `Bash` | (520, 470) |
| `Skill` | (520, 620) |
| `Hit_A` | (520, 770) |
| `Interact` | (520, 920) |
| `Downed` | (80, 900) |
| `Rise` | (80, 1050) |
| `Q` | (1000, 120) |
| `W` | (1000, 380) |
| `E` | (1000, 640) |
| `R` | (1000, 900) |
| 子机内 `Q_1` 等 | (300, 40)（各自子机内） |
| `AnyState` | 通过 `SerializedObject` 设 `m_AnyStatePosition` 到 (520, -160) |

**B. 远程打断**

1. 控制器：新增 `Attack_Draw → Locomotion`、`Attack_Release → Locomotion` 两条过渡，参数同 `INTERRUPT-01`：条件 `Interrupt`（`If`）、`HasExitTime=false`、`duration=0.1`、`InterruptionSource=Source`、`OrderedInterruption=true`、`CanTransitionToSelf=false`。
2. `CharacterBrain.OnCommandMove` 的状态门从 `{AttackMelee, Interact}` 扩成 `{AttackMelee, Attack_Draw, Attack_Release, Interact}`。

**C. 技能指令锁**

在 `CharacterBrain`（或 `PlayerCommandController` 统一入口）加：

```csharp
static readonly string[] SkillStates = { "Q_1","W_1","E_1","R_1","Bash","Skill" };
bool InSkillState() => SkillStates.Any(n => anim.GetCurrentAnimatorStateInfo(0).IsName(n));

public void CommandMove(Vector3 p)
{
    if (InSkillState()) return;      // 技能期间移动无效
    ...
}
public void CommandAttack(Transform t) { if (InSkillState()) return; ... }
public void CommandInteract(Interactable i) { if (InSkillState()) return; ... }
```

注意：
- 判定用**当前状态名**，不用 `IsInTransition`（过渡中允许结束）；
- 技能自然结束（`Skill` 状态退出）后自动解除，不需要计时器；
- 不得影响怪物 AI（本单只改玩家指令入口）。

**影响面清单**

| 项 | 内容 |
|---|---|
| 新增文件 | `Docs/QA/Evidence/INTERRUPT-02/**`、`REPORT_INTERRUPT-02.md` |
| 修改文件 | `PlayerBase.controller`（2 条过渡 + 坐标）、`CharacterBrain.cs`（状态门 + 指令锁） |
| 字符串引用 | `Attack_Draw`、`Attack_Release`、`Q_1`…`R_1`、`Bash`、`Skill`、`Interrupt` |
| 场景/预制体 | 不改 |
| `.meta` | 无 |

## §3 验证（Validation）

| AC | Given / When / Then | 断言 ID | 证据 |
|---|---|---|---|
| AC-1 | 给定动画机窗口，当观察三列布局，则 `AnyState→X` 与 `X→Locomotion` 连线不穿过任一状态矩形（至少 AttackMelee/Draw/Release/Bash/Skill/Hit_A/Interact 七条目视通过） | 目检-1 | 截图或位置读回 + 所有者确认 |
| AC-2 | 给定 Archer/Mage 在 `Attack_Draw` 中下单移动，则 0.15s 内 `Locomotion`，本周期箭/火球不生成 | `Interrupt.RangedDraw` | `ranged_draw.txt` |
| AC-3 | 同上但在 `Attack_Release`，则 0.15s 内 `Locomotion`，伤害不结算 | `Interrupt.RangedRelease` | `ranged_release.txt` |
| AC-4 | 给定技能（Q/W）起手后 0.1s 下单移动，则 `agent.destination` 不变、Animator 仍在该技能状态、技能伤害照常结算 | `Skill.LockMove` | `lock_move.txt` |
| AC-5 | 技能中下单攻击/交互，则目标不变、不进入 `Interact` | `Skill.LockOther` | `lock_other.txt` |
| AC-6 | 技能结束后 0.1s 下单移动，则正常移动（锁已解除） | `Skill.UnlockAfter` | `unlock.txt` |
| AC-7 | 最终 diff 只含 2 个文件；SPEC01 12/0 | `all` | `git diff --stat`、`editor_runN/…` |

**运行次数**：AC-2/3/4/5/6 各 3 次（时序类）。

**自动化优先**：`execute_code` 在 Play 中驱动；AC-1 只能目检（≤1 项，已写明理由：过渡线是编辑器渲染产物，无 API 可断言）。

## §4 执行信息

| 项 | 内容 |
|---|---|
| 前置 | Unity 打开；`INTERRUPT-01` / `RESCUE-01` 已合入 |
| 允许改动 | `PlayerBase.controller`、`CharacterBrain.cs` + 证据 + REPORT |
| 绝不触碰 | `OC_*`、场景、预制体、`.asset`、`ProjectSettings`、`Packages`、`SkillSystem`/技能效果、`CURRENT.md` |
| MCP 权限 | `execute_code`、`execute_menu_item`（仅 `Tools/QA/In-Editor/*`）、`read_console`、`refresh_unity`、资源读取 |
| 熔断 | 需要改技能代码或预制体；技能锁导致技能无法自然结束；SPEC01 由 12 变红 |
| 执行预算 | 实现 1 轮 + 修复 1 轮 |

## §5 学习产出

| 项 | 要求 |
|---|---|
| 讲稿 | **不适用**：过渡与指令门的小改，讲解点并入 REPORT §3 |
| 三句话版本 | 写进 REPORT §3 |

## 自包含声明

本工单不依赖 `CURRENT.md` 或其他任务书即可执行。
