# REPORT_DOWNED-01 — 玩家倒地不播动作（诊断 + 修复）

| 项 | 值 |
|---|---|
| 任务书 | `Docs/Handoff/TASK_DOWNED-01.md` |
| 风险等级 | 常规 |
| 执行方式 | 模式 B（MCP 常开编辑器），`execute_code` 驱动，无 batchmode |
| Unity | 2022.3.62f3c1，场景 `Assets/Scenes/DungeonWorld.unity` |
| 运行时 HEAD | `9e0b7515`（分支 `docs/fnd-00-baseline`；与任务书 §0 写的 `6872348b` 不同，属既有未提交批次推进，本单未改动历史） |
| 结论分支 | TASK §2「结论分支」第 4 类：`Downed` 被另一条 `AnyState` 过渡抢走（实际抢走它的是 `AnyState→Hit_A`，非 `Die` 本身） |
| 状态 | 已修复并验证（待大脑验收） |

## 1. 结果摘要

**根因（一句话）**：致死一击在 `UnitHealth.ApplyDamageInfo` 里**先**同步触发 `OnHit → CharacterBrain.OnDamaged → Animator.SetTrigger("Hit")`，**后**才触发 `OnDeath → DownedRescue.OnDowned → SetTrigger("Die")`；`Die` 先被消费进入 `Downed`（正确），但残留的 `Hit` 触发器在约 0.05~0.07s 后被 `AnyState→Hit_A` 消费，把刚进入的 `Downed` 抢走，动画落到 `Hit_A → Locomotion(Idle_A)`——于是「血空、`IsDown=true`、能被救援，但角色站着不动」。

**不是**以下原因（均已被原始数据排除）：

- 不是「`OnDowned` 没执行」：`OnDeath` 唯一订阅者就是 `DownedRescue.OnDowned`，`IsDown=true`、`AllDowned=1`、`GameData.message` 变为「Knight_Unit 倒下了！」。
- 不是「`Downed` 的 motion 指错」：`Downed.motion = Death_A`（0.8s，非空，`Animator.GetCurrentAnimatorClipInfo` 权重 1.00）。
- 不是「四个状态共用同一个 clip」：`Downed/Hit_A/Interact/Rise` 用的是**同一个 FBX 的四个不同 clip**（`Rig_Medium_General.fbx` 内 `Death_A`/`Hit_A`/`Interact`，`Rise` 用另一 FBX 的 `Lie_StandUp`），guid 相同只是因为同属一个 FBX。
- 不是「覆盖器把 clip 映射成空」：`OC_Knight` 里 `Death_A → Death_A`（同一个 clip）。

**修复**：`DownedRescue.OnDowned()` 在 `SetTrigger("Die")` 之前 `animator.ResetTrigger("Hit")`，只改 1 行（+2 行注释）。

**修复后**：5 次落地验证（Knight×3、Archer×1、Mage×1）全部 `Downed` 在 **0.0201~0.1166s** 内进入、`Death_A` 权重恒 **1.00**、`Hit_A` 出现 **0 次**、`Revive → Rise → Locomotion` 链路走通；SPEC01 `editor_run12` **12/0**、exitCode=0。

## 2. 证据锚点（AC 逐行回填）

证据目录：`Docs/QA/Evidence/DOWNED-01/`

| AC | 断言 ID | 结果 | 原始证据 | 关键数据 |
|---|---|---|---|---|
| AC-1 | `Downed.AnimatorState` | **pass** | `after_death_animator.txt`（5 次逐帧采样）、原始 `_run1.._run5*.txt` | Downed 首次出现：0.0461 / 0.1166 / 0.1105 / 0.0201 / 0.1136 s（均 <0.3s）；窗口内 clip=`Death_A`，weight min=max=**1.00**（>0.9）；`Hit_A` 出现 0 次 |
| AC-2 | `Downed.LogicState` | **pass** | `after_death_logic.txt` | el≈0.2s 与 el≈1.3s 两次采样均为 `IsDown=True dead=True AllDowned=1 anim=True agent=False brain=False`；复活后 `AllDowned=0` |
| AC-3 | `Downed.RevivePath` | **pass** | `revive_trace.txt` | `Revive()`→`IsDown=False dead=False hp=1`→`Rise(Lie_StandUp)`→`Locomotion(Idle_A)`；5 次一致（Rise 起始 1.6129~1.6156s，Locomotion 3.7935~3.8005s） |
| AC-4 | `Downed.RootCause` | **pass** | `clip_mapping.txt`、`onDowned_trace.txt`、`before_fix_red_run.txt`、`before_fix_ab_reset_hit.txt` | 修复前同一击杀：`Downed@Death_A w=1.00` → 9 帧后 `Hit_A w=1.00` → `Idle_A`；击杀后同帧 `ResetTrigger("Hit")` 的 A/B 对照里 Downed 保持 120 帧不被抢 |
| AC-5 | `all` | **pass** | `git_diff_stat.txt`、`Docs/QA/Evidence/SPEC-01/editor_run12/SPEC01_Summary_run1.json` | 本单只改 `DownedRescue.cs`（+3/−1）；SPEC01 pass=12 fail=0 blocked=0 exitCode=0 |

诊断步骤 D-1~D-5 与产出对照：

| 步骤 | 产出文件 | 说明 |
|---|---|---|
| D-1 打空血 | `_run*.txt` 的 `KILL_CALL/KILL_DONE` 行 | 用 `UnitHealth.ApplyDamage(9999f, pos)`（`TakeHit` 无 `DamageInfo` 重载，且会被 `hardness/锋利度` 弹刀门挡住，故改用 `ApplyDamage`；已在报告中注明，未改产品代码） |
| D-2 读 Animator | `after_death_animator.txt` | `GetCurrentAnimatorStateInfo(0).IsName(...)` + `GetCurrentAnimatorClipInfo(0)` 的 clip.name/weight，逐帧落盘 |
| D-3 读逻辑 | `after_death_logic.txt` | `IsDown / dead / AllDowned.Count / animator.enabled / agent.enabled / brain.enabled` |
| D-4 clip 映射 | `clip_mapping.txt` | 控制器 10 个状态 → clip 名/instanceID/长度；6 条 AnyState 过渡（顺序、条件、interruptionSource）；`OC_Knight` 全部 original→override 对 |
| D-5 OnDowned 是否执行 | `onDowned_trace.txt` | 反射枚举 `OnHit/OnDeath/OnRevived` 订阅者 + 击杀后同步读 `GameData.message` |

## 3. 改动清单

| 文件 | 改动 | 行数 |
|---|---|---|
| `Assets/_Project/Scripts/Units/DownedRescue.cs` | `OnDowned()`：`SetTrigger(Die)` 前先 `ResetTrigger("Hit")`，并加 2 行注释说明原因 | +3 / −1（净 +2） |
| `Docs/QA/Evidence/DOWNED-01/**` | 新增证据 | 新增 |
| `Docs/Handoff/REPORT_DOWNED-01.md` | 本报告 | 新增 |

未改动：`PlayerBase.controller`、`OC_Knight/OC_Archer/OC_Mage.overrideController`、场景、预制体、`.asset`、`UnitHealth.cs`、`ProjectSettings`。
（`git diff --stat` 里另外出现的 `PlayerBase.controller`（仅 `m_Position` 节点坐标）、`SelectionManager.cs`、`DebugCameraSwitcher.cs`、`QA_InEditorMenu.cs`、`ProjectSettings.asset`、两个 `.csproj` 均为**本单开工前已存在的未提交批次**，本单未触碰。）

## 4. 测试数据

| # | 单位 | 控制器 | 击杀→Downed | Downed 窗口 clip 权重 | Hit_A | Revive→Rise | Rise→Locomotion | 结果 |
|---|---|---|---|---|---|---|---|---|
| A | Knight_Unit | OC_Knight | 0.0461s | 1.00 | 0 | 1.6140s | 3.7964s | pass |
| B | Knight_Unit | OC_Knight | 0.1166s | 1.00 | 0 | 1.6156s | 3.7935s | pass |
| C | Knight_Unit | OC_Knight | 0.1105s | 1.00 | 0 | 1.6129s | 3.7946s | pass |
| D | Archer_Unit | OC_Archer | 0.0201s | 1.00 | 0 | 1.6138s | 3.7969s | pass |
| E | Mage_Unit | OC_Mage | 0.1136s | 1.00 | 0 | 1.6153s | 3.8005s | pass |
| red | Knight_Unit（修复前） | OC_Knight | 0.113s 进入 Downed（正确） | 进入后 0.064s（9 帧）权重从 1.00 掉到 0.06 并离开 | **1**（随后 Idle_A） | red 现场未跑到复活；控制器里 `Revive` 只挂在 `Downed→Rise`，状态已被抢到 Locomotion 时 `Revive` 无出处过渡 | — | fail（复现） |

- A/B/C 为同一 Play 会话内连续 3 次「死亡→复活」，D/E 为另一轮跨角色验证；均为 N≥3 的时序类要求。为省 play 进出开销未在每轮之间重进 Play，但每轮都有独立的 PRE 行、`KILL_CALL` 与 `t=` 时间戳，可独立复核。
- 取证过程备忘（不影响产物）：中途有一次在已退出 Play 后误执行了击杀脚本，仅改到运行期属性（`hp/dead/IsDown` 均非序列化），随后经 `Revive` 复原；核对 `scene.isDirty=False`、无 `.asset`/场景/预制体改动，之后重进 Play 正式取证。
- `Revive` 走 `DownedRescue.Revive()`（`RescueRoutine` 完成时调用的同一私有方法），内部调用 `UnitHealth.Revive(reviveHp=1f)`；救援交互流程本身归 `RESCUE-01`，本单不验证读条/打断。
- SPEC01：`Docs/QA/Evidence/SPEC-01/editor_run12`，`pass=12 fail=0 blocked=0 exitCode=0`，`head=9e0b7515`。

## 5. 实现原理（含定位）

1. `Assets/_Project/Scripts/Units/UnitHealth.cs:92` — `OnHit?.Invoke(info)` 在死亡判定**之前**执行，所以「致死一击」也是一次正常的受击事件。
2. `Assets/_Project/Scripts/Units/UnitHealth.cs:95-99` — `hp<=0` 才置 `dead=true` 并 `OnDeath?.Invoke()`。两者同一帧顺序发生：`Hit` 触发器先入队、`Die` 后入队。
3. `Assets/_Project/Scripts/Units/DownedRescue.cs:57`（本单改动）— `OnDowned` 里 `animator.ResetTrigger("Hit"); animator.SetTrigger(dieTrigger);`。触发器是「按住不放」的标记，只有被某条过渡消费才清除；不清掉 `Hit`，它会在下一帧/下一段过渡完成后继续生效。
4. `Assets/_Project/Art/Animations/PlayerBase.controller:1297-1303` — AnyState 过渡顺序为 `Die(0) → Hit(1) → Q/W/E/R`。`Die` 排第一，所以进入 `Downed` 是对的；问题在**残留的 `Hit` 触发器仍会命中第 2 条过渡**（`m_InterruptionSource: 1 = Source`），把 `Downed` 打断。
5. `Assets/_Project/Art/Animations/PlayerBase.controller:182-197` — `Hit_A` 状态运动是 `Hit_A`(0.667s)，其出口回到 `Locomotion`；这就是玩家看到的「站着不动」。
6. `Assets/_Project/Scripts/Units/CharacterBrain.cs:37` — `SetTrigger("Hit")` 的调用点（grep 命中；该文件不在本单允许读取清单内，故仅引用命中行，未展开阅读）。

排查路径（可复用）：**先分清「没进状态 / 进了没 clip / 进了被抢走」**——逐帧同时打印 `AnimatorStateInfo.IsName()` + `AnimatorClipInfo.clip/weight` + `IsDown/dead`，只要出现「曾正确进入 + 权重 1.0 + 若干帧后跳到无关状态」，就直接怀疑**触发器残留 + AnyState 优先级**，而不是先怀疑 clip 资源；再用「同帧 ResetTrigger 做 A/B」把根因锁定到单一触发器。

## 6. 模块导航

| 模块 | 位置 | 职责 / 与本单关系 |
|---|---|---|
| 生命与受击管线 | `Assets/_Project/Scripts/Units/UnitHealth.cs` | `OnHit/OnDeath/OnRevived` 三个事件；`ApplyDamageInfo` 的调用顺序是本 bug 的触发条件 |
| 倒地与救援 | `Assets/_Project/Scripts/Units/DownedRescue.cs` | `OnDowned()` 禁用 Brain/Agent、播 `Die`、维护 `AllDowned`；`Revive()` 走 `Revive→Rise`。本单唯一改动处 |
| 玩家动画控制器 | `Assets/_Project/Art/Animations/PlayerBase.controller` | 单层 Base Layer；`AnyState` 6 条过渡决定「谁先抢到状态」；`Downed` 只接受 `Revive` 离开 |
| 角色皮肤覆盖 | `OC_Knight/OC_Archer/OC_Mage.overrideController` | 只做 clip 换皮；三者都映射到玩家共用控制器（只读核对，未改） |
| 受击表现 | `Assets/_Project/Scripts/.../CharacterBrain.cs`（`OnDamaged`） | 受击时 `SetTrigger("Hit")`；致死一击的残留触发器来自这里 |

## 7. 未决项

1. **结论分支与修法不完全对应（需大脑确认）**：TASK §2 第 4 条给的两种修法是「调整 AnyState 顺序」或「改 `Downed` 的 `InterruptionSource`」。经核对，`Die` 已经排在 AnyState 第 0 位，**顺序无需调整**；`InterruptionSource` 只约束「过渡进行中的打断」，而本例是过渡**结束后**的残留触发器再次命中，改它也无效。真正的最小修法是清掉残留 `Hit` 触发器（落在允许文件 `DownedRescue.cs`，1 行）。因此本单按「等价最小修复」执行，请验收时确认该判断；若判定越界，回滚锚为 `git checkout -- Assets/_Project/Scripts/Units/DownedRescue.cs`。
2. **更彻底的结构性方案（未实施，超出本单范围）**：给控制器加 `Downed`（Bool）参数并让它门控 `AnyState→Hit`，从结构上禁止「已倒地还能被受击动画打断」；这需要改控制器参数 + 代码设置参数，属高风险且超范围。
3. **目检 1 项（留给所有者）**：`Death_A` 0.8s 播完后停在末帧（`normalizedTime>1` 保持）。请在 Game 视图确认该末帧观感是「趴/躺在地上」而非穿模或悬空——这是本单唯一无法自动化的验收点。
4. `_run1.._run5*.txt`、`_trace_tmp.txt`、`_ab_test.txt`、`_revive_tmp.txt` 为原始工作文件，保留在证据目录以便复核；`before_fix_*.txt` 是修复前的 red 现场副本。
5. 未验证：真实点击救援（走 `RescueRoutine` 3s 读条 + 被打断分支）——归 `RESCUE-01`。

## 8. 建议提交

- 标题：`fix(downed): 倒地前清掉致死一击残留的 Hit 触发器，避免 AnyState→Hit_A 抢走 Downed`
- 正文（一行）：`致死伤害会先排入 Hit 触发器，Downed 进入后又被 AnyState→Hit_A 抢走，角色血空却站着不动；OnDowned 里先 ResetTrigger("Hit") 再 SetTrigger(Die)。`

## 9. 讲稿

**不适用**——修复类小单（1 行代码），按 TASK §5 要求不单独出讲稿；排查路径已并入本报告 §5，Done 后如需答辩用 §5「排查路径」一段即可。

**三句话版本**：

1. 倒地动画其实进过一次，但被「致死一击顺带排队的受击触发器」在 0.05s 后顶掉，动画回到待机，所以看起来站着不动。
2. 定位方法：逐帧同时打印状态名、clip 名与权重、`IsDown/dead`；只要「先正确进入 → 权重 1.0 → 几帧后跳到无关状态」，就怀疑触发器残留而不是 clip 资源。
3. 修法：倒地瞬间先清掉 `Hit` 触发器，再播 `Die`——1 行改动，5 次跨角色验证 + SPEC01 12/0 全绿。
