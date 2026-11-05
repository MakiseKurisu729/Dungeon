# INTERRUPT-03 — 普攻淡入期可打断 + 清理怪物无效触发器

## §0 元信息

| 字段 | 值 |
|---|---|
| 任务 ID | `INTERRUPT-03` |
| 风险等级 | `常规`（1 个过渡字段 + 1 行代码；控制器属高风险资产，但改动面单点、有 SPEC01 守卫） |
| 授权 | 所有者 2026-11-05：①选 B（改过渡）；②同意清理怪物那句无效 `Interrupt` |
| 基线提交 | `4645e54d` |
| 回滚锚 | `git checkout -- Assets/_Project/Art/Animations/PlayerBase.controller Assets/_Project/Scripts/Units/MonsterBrain.cs` |
| 状态 | `Ready` |

## §1 需求

| 编号 | 需求 | 验收方式 |
|---|---|---|
| R-1 | `Locomotion → AttackMelee` 的 `InterruptionSource` 由 `None` 改为 `Destination`，使淡入期内的移动打断生效 | 读回字段 + 时序断言 |
| R-2 | 普攻指令后 0.05s、0.10s（淡入期内）下移动指令，均 0.15s 内回到 `Locomotion`，且该次普攻不结算伤害 | `Interrupt.FadeInWindow` |
| R-3 | 淡入结束、稳定在 `AttackMelee` 后下移动指令，行为与改动前一致（0.10s 级） | `Interrupt.SteadyState` |
| R-4 | 删除 `MonsterBrain.CancelAttack()` 里对不存在参数 `Interrupt` 的 `SetTrigger`；取消怪物攻击后 Console 无 “Parameter 'Interrupt' does not exist” | `Monster.NoPhantomTrigger` |
| R-5 | 怪物攻击/取消/受击行为不变；SPEC01 12/0 | `all` |

**不在范围**：怪物打断语义重构、给 `Skeleton.controller` 加 `Interrupt` 过渡、其它过渡的 `InterruptionSource`。

## §2 方案

**A. 过渡字段**：只把 `Locomotion→AttackMelee` 的 `m_InterruptionSource` 从 `0(None)` 改为 `2(Destination)`。其余字段（`HasExitTime=0`、`ExitTime=.732`、`Duration=.15`、`CanTransitionToSelf`）**一律不动**。
理由：`Destination` 表示“过渡进行中，来自目标状态的过渡可以打断当前过渡”，正好允许 `AttackMelee→Locomotion(Interrupt)` 在淡入期生效。

**B. 怪物清理**：`MonsterBrain.cs` 的 `CancelAttack()` 删除 `_animator.SetTrigger("Interrupt");`，保留 `ResetTrigger(attackTrigger)`。因为 `Skeleton.controller` 无 `Interrupt` 参数，这行只会产生警告。

**影响面**

| 项 | 内容 |
|---|---|
| 修改文件 | `PlayerBase.controller`（1 字段）、`MonsterBrain.cs`（−1 行） |
| 字符串引用 | `Interrupt`（怪物侧移除后，玩家侧仍用） |
| 场景/预制体/`.meta` | 无 |

## §3 验证

| AC | Given / When / Then | 断言 ID | 证据 |
|---|---|---|---|
| AC-1 | 读回 `Locomotion→AttackMelee` 的 `InterruptionSource == Destination`，其余字段与原值一致 | 目检-1 + 读回 | `transition.txt` |
| AC-2 | 普攻后 0.05s / 0.10s 下移动，3 次均 0.15s 内回 `Locomotion`、伤害 0 | `Interrupt.FadeInWindow` | `fadein.txt` |
| AC-3 | 稳定 `AttackMelee` 后下移动，0.15s 内回 `Locomotion`（不回归） | `Interrupt.SteadyState` | `steady.txt` |
| AC-4 | 让怪物攻击后跑出范围触发 `CancelAttack`，Console 无 `Interrupt` 参数警告；怪物仍能再次攻击 | `Monster.NoPhantomTrigger` | `monster_console.txt` |
| AC-5 | SPEC01 编辑器内 12/0；FND00B06 无新增红 | `all` | `editor_runN/…` |

**次数**：AC-2/3 各 3 次。

## §4 执行信息

| 项 | 内容 |
|---|---|
| 允许改动 | 上述 2 个文件 + `Docs/QA/Evidence/INTERRUPT-03/` + `REPORT_INTERRUPT-03.md` |
| 绝不触碰 | `OC_*`、场景、预制体、`.asset`、`ProjectSettings`、`Packages`、其它过渡、`CURRENT.md` |
| MCP | `execute_code`、`execute_menu_item`（仅 `Tools/QA/In-Editor/*`）、`read_console`、`refresh_unity` |
| 熔断 | 改完 AC-3 回归；SPEC01 由 12 变红；需要动其它过渡 |

## §5 学习产出

不适用（1 个字段 + 1 行删除），三句话版本写进 REPORT §3。

## 自包含声明

不依赖 `CURRENT.md` 即可执行。
