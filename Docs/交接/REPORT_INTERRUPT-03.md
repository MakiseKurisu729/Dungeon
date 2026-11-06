# REPORT — INTERRUPT-03：普攻淡入期可打断 + 清理怪物无效触发器

> 对应 `Docs/Handoff/TASK_INTERRUPT-03.md`。节次与 `Docs/Handoff/workflow-rules.md` §2 一致。
> 未运行项均显式标注；所有数字来自 `Docs/QA/Evidence/INTERRUPT-03/` 下的原始文件。

## 1. 结果摘要

`完成（带 1 项前提纠正）` —— `Locomotion→AttackMelee` 的 `InterruptionSource` 改为 `Destination` 后，普攻淡入期 0.05s/0.10s 下移动均在 0.104–0.107s 回到 `Locomotion` 且 0 伤害（6/6），稳定态 0.106–0.108s（3/3），SPEC01 12/0、FND00B06 12/0+2obs；但 R-4 的“Skeleton 没有 Interrupt 参数”这一前提与仓库实际不符，见 §7。

## 2. AC 回填表

| AC | 结果 | 断言 ID | 关键数据 | 日志路径 |
|---|---|---|---|---|
| AC-1 | 通过 | 目检-1 + 读回 | `isrc=None(0)→Destination(2)`；`dur=0.25 / exitT=0.73214287 / hasExit=False / ordered=True / self=True / cond=[If:AttackMelee]` 全部未变 | `Docs/QA/Evidence/INTERRUPT-03/transition.txt` |
| AC-2 | 通过 | `Interrupt.FadeInWindow` | 6/6 PASS：moveAt 0.050/0.051/0.053/0.103/0.101/0.103s，`inTransitionAtMove=True`（均在淡入中），settle 0.104/0.104/0.106/0.106/0.105/0.107s，hp 15070→15070（0 伤害） | `…/INTERRUPT-03/fadein.txt` |
| AC-3 | 通过 | `Interrupt.SteadyState` | 3/3 PASS：`cur=AttackMelee, inTransition=False, norm≈0.235–0.238` 时下移动，settle 0.106/0.107/0.108s，hp 0 变化 | `…/INTERRUPT-03/steady.txt` |
| AC-4 | 计数通过（前提不成立） | `Monster.NoPhantomTrigger` | 攻击→拉出 23.5m→`atkTimer -1`（CancelAttack 生效）→拉回后再次攻击；探针窗口 Console 中 `Interrupt` 相关 0 条；对照：`SetTrigger("NoSuchParamXYZ")` 立刻产出 “Parameter 'NoSuchParamXYZ' does not exist.”，证明检测有效 | `…/INTERRUPT-03/monster_console.txt`、`monster_console_raw.txt` |
| AC-5 | 通过 | `all` | SPEC01 `editor_run18`：12 pass / 0 fail / 0 blocked / 0 infraFail（code=0）；FND00B06 `editor_run2`：12 pass / 0 fail / 2 observed（code=0） | `…/INTERRUPT-03/editor_run1/`、`…/INTERRUPT-03/fnd_run1/` |

- **运行次数**：AC-2 六次（0.05s ×3 + 0.10s ×3；时序类要求 N≥3 满足）、AC-3 三次、AC-4 一次完整攻击/取消/再攻击循环、SPEC01 一次、FND00B06 一次。
- **过程性失败（已归档，不作为 AC 结果）**：
  - `fadein_attempt1_neverIdle.txt`：第一版多轮夹具在轮间未清 `_orderedTarget`，下一轮开始前大脑立即自动开打，出现 “never idle”；修正夹具后为 `fadein.txt` 6/6。
  - `diag_fadein_single.txt` / `diag_brain_single.txt`：早期夹具把 “目标残血/死亡”“自动索敌重复开打” 混入统计，仅作诊断留档；正式结论以 `fadein.txt`/`steady.txt` 为准。
- **夹具口径**：为排除“玩家自动索敌重复开打”的干扰，dummy 目标使用 `Mage_Unit`（`team=Player` 使 `UnitBrain.FindNearestEnemy` 跳过，`maxHp+10000` 防止被打死），移动指令发往 +20m 长距离，保证测量窗口内 `_moving=true`；测量期间禁用其它 `CharacterBrain`/`MonsterBrain` 与对应 `NavMeshAgent`。

## 3. 实现原理（面向所有者）

1. **根因 / 目标**：`Locomotion→AttackMelee` 过渡默认 `InterruptionSource=None`，淡入期间 Unity 不会评估“目标状态出发的过渡”，因此 `AttackMelee→Locomotion(Interrupt)` 在淡入期无法接管；表现是移动指令已把 `MeleeCombat._timer=-1`（不会结算伤害），但动画仍播到自然退出（实测 ~0.55s），不符合“0.15s 内回 Locomotion”。
2. **备选方案与为什么没选**：A（在代码里 `CrossFade`/`Play("Locomotion")` 硬切）会绕开过渡资产、破坏 Animator 语义并影响其它打断路径；B（只改一个枚举字段为 `Destination`）最小、可回滚、由 SPEC01 守卫，故选 B。
3. **实际数据流**：`CommandMove` → `CharacterBrain.OnCommandMove`（`CancelAttack()` 把 `_timer=-1`，并在 current/next 为可打断状态时 `SetTrigger("Interrupt")`）→ 因为淡入过渡 `isrc=Destination`，`AttackMelee→Locomotion(0.1s)` 可立即接管 → 当前状态在 ~0.105s 变回 `Locomotion` 且 `_timer=-1` 使 `MeleeCombat.Update` 提前 return，命中窗口不结算。
4. **关键代码位置**：
   - `Assets/_Project/Art/Animations/PlayerBase.controller:581`（transition `-571037474482483329`）—— `m_InterruptionSource: 0 → 2`，唯一功能改动。
   - `Assets/_Project/Scripts/Units/CharacterBrain.cs:71-89` —— 移动指令先 `CancelAttack`，再按 current/next 状态决定是否发 `Interrupt`。
   - `Assets/_Project/Scripts/Combat/MeleeCombat.cs:245-254` —— `CancelAttack` 置 `_timer=-1` 并复位 `attackTrigger/Skill`。
   - `Assets/_Project/Scripts/Combat/MeleeCombat.cs:144-182` —— `_timer<0` 直接 return，所以打断后本段普攻不可能再结算。
   - `Assets/_Project/Scripts/Units/MonsterBrain.cs:106-113` —— `CancelAttack` 现在只 `ResetTrigger(attackTrigger)`。
5. **边界 / 失败模式**：只有在 `current|next` 命中 `AttackMelee/Attack_Draw/Attack_Release/Interact` 时才会发 `Interrupt`；技能状态（`Skill/Bash/Q_1...`）不发，保持 INTERRUPT-01 的技能免疫。淡入期若玩家没有真正进入移动（无路径），本段攻击已取消但动画会播完——这是现有逻辑，不在本单范围。
6. **与计划的差异及原因**：
   - 过渡实际 `m_TransitionDuration=0.25`（工单/dispatch 写 0.15）；本单只改 `InterruptionSource`，时长未动，AC 的 0.15s 断言按“打断后回 Locomotion 的耗时”验证，仍全部通过。
   - Unity 在 `AssetDatabase.SaveAssets()` 时把 `Rise` 状态节点 `y=1050 → 1020` 重新序列化；尝试用 API 写回 1050 仍被归一为 1020。属动画机画布布局，不影响任何字段；已在 `transition.txt` 留痕。
   - 任务基线写 `4645e54d`，实际 HEAD 为 `db732792`（INTERRUPT-01/02 之后的提交）；工单/dispatch 给出的文件级事实与 `db732792` 一致，故按现场执行，差异在此登记。
7. **三句话版本**：
   - 淡入期的“进攻击”过渡默认不允许被目标状态的过渡打断，所以移动慢半拍。
   - 把该过渡的 `InterruptionSource` 改成 `Destination`，`AttackMelee→Locomotion(Interrupt)` 就能在 0.1s 内接管。
   - 伤害没变多也没变少：`_timer=-1` 本来就会让本段普攻不结算，这次只是让动画也立刻退出。

## 4. 受影响模块导航

| 模块 | 职责 | 入口（类 / 方法 / 场景对象） |
|---|---|---|
| 输入 → 指令 | 右键/移动指令、集火 | `CharacterBrain.CommandMove` → `OnCommandMove`（`Knight_Unit` 等） |
| 近战执行 | 计时、命中窗口、取消 | `MeleeCombat.CancelAttack` / `Update` / `SingleTargetHit` |
| 动画机 | 状态过渡与打断策略 | `PlayerBase.controller`（Base Layer，`Locomotion↔AttackMelee`） |
| 怪物大脑 | 攻击/取消/受击 | `MonsterBrain.CancelAttack`（`Skeleton_Warrior`） |

数据流：`玩家移动指令 → CharacterBrain.OnCommandMove → MeleeCombat.CancelAttack(_timer=-1) + Animator.SetTrigger("Interrupt") → AttackMelee→Locomotion 过渡 → 本段不结算伤害`。

**改动文件清单**

| 文件 | 改了什么 |
|---|---|
| `Assets/_Project/Art/Animations/PlayerBase.controller` | `Locomotion→AttackMelee` 的 `m_InterruptionSource` 0→2；另有 Unity 自动归一化的 Rise 节点 y 坐标（见 §2 AC-1 说明） |
| `Assets/_Project/Scripts/Units/MonsterBrain.cs` | `CancelAttack()` 删除 `_animator.SetTrigger("Interrupt")`，保留 `ResetTrigger(attackTrigger)` |
| `Docs/QA/Evidence/INTERRUPT-03/**` | 本单证据 |
| `Docs/Handoff/REPORT_INTERRUPT-03.md` | 本报告 |

## 5. 证据清单

| 文件 | 内容 |
|---|---|
| `Docs/QA/Evidence/INTERRUPT-03/transition.txt` | AC-1 读回、全量 Locomotion 过渡、git diff、Skeleton 参数审计 |
| `Docs/QA/Evidence/INTERRUPT-03/fadein.txt` | AC-2 正式 6 轮（0.05s×3 / 0.10s×3），逐轮 move/settle/hp |
| `Docs/QA/Evidence/INTERRUPT-03/steady.txt` | AC-3 正式 3 轮 |
| `Docs/QA/Evidence/INTERRUPT-03/monster_console.txt` | AC-4 时间线、Console 检查、缺参对照、前提纠正 |
| `Docs/QA/Evidence/INTERRUPT-03/monster_console_raw.txt` | AC-4 逐帧原始日志 |
| `Docs/QA/Evidence/INTERRUPT-03/anim_isolated.txt` | 控制器隔离探针（禁用 brain/melee，直接驱动 Animator）6/6，settle 0.103–0.108s |
| `Docs/QA/Evidence/INTERRUPT-03/editor_run1/` | SPEC01 `editor_run18` 原样拷贝（InEditorResult/Summary/Assertions/Runs/ExitCode） |
| `Docs/QA/Evidence/INTERRUPT-03/fnd_run1/` | FND00B06 `editor_run2` 原样拷贝 |
| `Docs/QA/Evidence/INTERRUPT-03/fadein_attempt1_neverIdle.txt`、`diag_*.txt` | 过程性失败与诊断留档 |

## 6. 学习讲稿

不适用（1 个字段 + 1 行删除）。三句话版本见 §3.7。

## 7. 未决项

- **R-4 前提与仓库实际不符（需所有者/大脑裁定）**：`Skeleton.controller` 实际含 `Interrupt` 参数（参数表：Speed/AttackMelee/AttackRanged/Hit/Interrupt/Die/AttackSpeed），且有两条 `Interrupt` 条件过渡：`Attack_Melee→Locomotion`、`Shoot→Locomotion`。因此原 `SetTrigger("Interrupt")` **不会**产生“参数不存在”警告，并且**确实会**在怪物收招时把攻击动画打断回 Locomotion。按工单明确指令已删除该行，但删除后怪物 `CancelAttack` 只复位触发器、不再打断动画（R-5“行为不变”存在偏差）。建议二选一：① 恢复该行（回滚锚一行即可）；② 明确接受“怪物收招动画自然播完”的新行为并补验收。回滚命令：`git checkout -- Assets/_Project/Scripts/Units/MonsterBrain.cs`（会同时回滚该文件全部改动）。
- Unity 保存控制器时把 `Rise` 节点 y 由 1050 归一化为 1020；无 API 可稳定写回。请在 Animator 窗口目检决定是否接受该 hunk（纯布局）。
- 工单基线 `4645e54d` 与现场 `db732792` 不一致，已按现场事实执行（见 §3.6）。

## 8. 建议提交

```text
修复(anim): 普攻淡入期允许被移动打断，清理怪物 CancelAttack 的 Interrupt 触发
为什么：进攻击过渡 InterruptionSource=None 导致淡入期移动需等动画自然退出，改为 Destination 后 0.1s 内回到 Locomotion 且不结算伤害
```

> 说明：`Assembly-CSharp*.csproj` 的改动由 Unity 自动生成，与会话无关；`Rise` 节点坐标 hunk 属 Unity 保存副作用。

## 9. 消耗记录

- 钱包：`Go 订阅`
- 模型完整 ID：`opencode-go/deepseek-v4.1-flash`
