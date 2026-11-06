# REPORT_INTERRUPT-01

> 任务：`Docs/Handoff/TASK_INTERRUPT-01.md` ｜ 风险：高风险（Animator 过渡 + 战斗/交互管线）
> 执行：模式 B（MCP 常开编辑器，`Dungeon@4f5c34c82f31646e`，Unity 2022.3.62f3c1）
> 执行时间：2026-11-05 19:47–20:12（主单）＋ 20:13–20:22（收尾：AC-6 远程残留 `Interrupt`）｜ 结束状态：已退出 Play，编辑器保持打开，未做任何 git 写操作

## 1. 结果摘要

**做了什么**

| # | 改动 | 结果 |
|---|---|---|
| 1 | `PlayerBase.controller` 新增 `AttackMelee→Locomotion`、`Interact→Locomotion` 两条 `Interrupt` 过渡（参数逐项照 TASK §2 表） | 完成，读回一致 |
| 2 | `CharacterBrain.OnCommandMove`：移动指令时按“当前状态 / 入场过渡中的目标状态”决定是否发 `Interrupt` | 完成 |
| 3 | `CharacterBrain`：新增技能门 `InSkillAnim()`，技能进行中跳过 `CancelAttack()` 与 `Interrupt` | 完成（**TASK §2 原方案缺此项，会导致 R-3 失败**，见 §4.3 red→green） |
| 4 | `MeleeCombat.CancelAttack()` 删除无条件 `SetTrigger("Interrupt")` | 完成 |
| 5 | `RangedCombat.CancelAttack()` 删除无条件 `SetTrigger("Interrupt")`（收尾追补，见 AC-6） | 完成，`git diff` 仅 -1 行 |

**AC 结论（逐行回填见 §2）**

| AC | 结论 | 关键数字 |
|---|---|---|
| AC-1 `Interrupt.AttackByMove` | **部分达标（需裁决）** | 普攻已为 `AttackMelee` 时：0.104 / 0.108 s 回到 `Locomotion`、目标 HP 不变 → PASS；但 TASK 指定的“下单后 0.1s 内移动”落在**入场交叉淡入**窗口时：0.305 / 0.306 / 0.307 s 才落定（无伤害、无残留），超出 0.15 s |
| AC-2 `Interrupt.InteractByMove` | **PASS** | 0.106 / 0.107 / 0.107 s 回到 `Locomotion`，`Interact` 不再推进 |
| AC-3 `Interrupt.SkillImmuneToMove` | **PASS（含 red→green）** | 技能伤害 60→30 正常结算、`_timer` 未被清零；状态自然结束于 Skill+1.032 s（exitTime ~0.81） |
| AC-4 `Interrupt.NoStaleTrigger` | **PASS ×2** | 技能后立刻普攻：完整播放 0.649 s（≥0.40）、单次 15 伤害、HP 30→15 |
| AC-5 `all` | **PASS** | 4 个代码/资产文件（`MeleeCombat.cs`、`RangedCombat.cs` 各删 1 行）；SPEC01 编辑器内 **12 pass / 0 fail**，ExitCode 0 |
| AC-6 `Interrupt.RangedNoStale` | **PASS ×3** | `RangedCombat` 不再无条件发 `Interrupt`（`git diff` 仅 -1 行）；同一复现脚本重跑，`Interact` 生命周期 **1.158 / 1.156 / 1.155 s**（修复前 0.022 s），`maxNorm` 0.885/0.883/0.882 = 自然收招；SPEC01 `editor_run14` 12/0，`Clock.RangedOneShotPerCycle` rangedPass=True |

**残留状态（本条已在收尾单内解决）**
1. ~~`RangedCombat.CancelAttack()` 仍无条件 `SetTrigger("Interrupt")` → 弓手/法师下一次 `Interact` 在 0.022 s 被秒切~~ → 已删（AC-6），秒切消失。
2. AC-1 的“入场交叉淡入窗口”无法在 0.15 s 内落定：已在进行的 `Locomotion→AttackMelee` 淡入（0.15 s）无法从代码侧取消，最坏 = 0.15（淡入）+ 0.15（残留时长）≈ 0.30 s。**仍待裁决**。

## 2. 证据锚点（AC 回填）

| AC | 证据文件 | 断言数据（原始） | 结论 |
|---|---|---|---|
| AC-1 | `Docs/QA/Evidence/INTERRUPT-01/attack_by_move.txt` | 5 run：①AC 窗口(下单+0.052~0.054s 移动) → `latSettled` 0.307 / 0.306 / 0.305 s，`hp 60→60`；②对照(AttackMelee 已 current) → `latSettled` 0.108 / 0.104 s，`hp 60→60`；全 run `timer` 立刻变 -1 | 部分达标（见 §7-2） |
| AC-1 旁证 | `attack_by_move_attempt1_moveAt0.358s.txt`、`attack_by_move_attempt2_moveAt0.253s.txt` | 第一/二轮探测（触发点由“状态入场”判定，落点 0.358 s / 0.253 s），保留不覆盖 | 参考 |
| AC-2 | `interact_by_move.txt` | 3 run：`Interact` 已 current 后 +0.02 s 下移动 → `latSettled` 0.106 / 0.107 / 0.107 s；+0.05/0.10 s 处 `next=Locomotion`，+0.15 s 起 `cur=Locomotion` | PASS ×3 |
| AC-3 | `skill_immune.txt` | 3 run：Skill+0.10 s 移动 → `timerAfterMove` 0.169 / 0.158 / 0.161（red 为 -1）；`hp 60→30`（跳劈 30 伤害）；`Locomotion` 首次出现在 Skill+1.032 s（自然 exitTime） | PASS ×3 |
| AC-3 red | `skill_immune_RED_before_skillGate.txt` | 未加技能门时：3 run 全部 `timerAfterMove=-1`、`hp 60→60`（技能伤害被 `CancelAttack` 杀掉） | red 已复现 |
| AC-4 | `no_stale_trigger.txt` | 2 run：技能中移动 → 技能自然结束(Skill+1.183 s) → 立刻普攻 → `AttackMelee` 持续 0.649 / 0.648 s、`attackDmg=15`、`hp 60→15` | PASS ×2 |
| AC-5 | `git_diff_stat.txt`、`SPEC01_Summary.json`、`SPEC01_Runs.jsonl`、`SPEC01_ExitCode_run1.txt` | `pass:12 fail:0 blocked:0 infraFail:0`，`exitReason=completed`，ExitCode `0`；`git status` 仅 3 个本单文件（+ 已存在的他单批次改动） | PASS（`git_diff_stat.txt` 为收尾前的快照；收尾后实际 diff：`RangedCombat.cs` -1 行，见 AC-6 行） |
| 控制器参数读回 | `controller_transitions.txt` | 两条新过渡：`-> Locomotion hasExitTime=False exitTime=0 duration=0.1 interruptionSource=Source orderedInterruption=True canTransitionToSelf=False conds=[Interrupt.If:0]`；既有两条 exitTime 过渡与 6 条 AnyState 未变 | 与 TASK §2 表逐项一致 |
| 残留诊断（非 AC） | `residual_ranged_interrupt.txt` | 弓手攻击中移动 → 回 Locomotion → 仅 `SetTrigger("Interact")` → `interactLifetime=0.022 s`，`staleInterruptConfirmed=True` | 修复前基线（见 §7-1） |
| AC-6 修复后重跑 | `residual_ranged_after_fix.txt` | 同一复现 3 run：`lifeBeforeFix=0.022s` → `lifeAfterFix=1.158 / 1.156 / 1.155 s`，`interactMaxNorm=0.885/0.883/0.882`，`pass=True ×3`；`moveToSettle=0.866/0.873/0.872 s`，`timerAtMove` 移动后立刻 `-1` | PASS ×3 |
| AC-6 远程回归 | `Docs/QA/Evidence/SPEC-01/editor_run14/SPEC01_Summary_run1.json`、`SPEC01_Assertions.jsonl` | `pass:12 fail:0`，`InEditorResult code=0`；`Clock.RangedOneShotPerCycle` `actual=rangedPass=True`（Archer `drawEntries=13 fireCount=12`，每次发射 `state=Attack_Release`、`normalizedTime≈0.34` 落在 0.2~0.6 窗口；Mage 同样 `pass=true`） | PASS |

SPEC01 原始目录：`Docs/QA/Evidence/SPEC-01/editor_run13/`（主单触发）、`Docs/QA/Evidence/SPEC-01/editor_run14/`（AC-6 收尾重跑，只追加未覆盖）。

## 3. 改动清单

| 文件 | 改动 | 行/锚点 |
|---|---|---|
| `Assets/_Project/Art/Animations/PlayerBase.controller` | 新增 2 个 `AnimatorStateTransition`（fileID `-6417422852915030786` = AttackMelee→Locomotion；`7751934566210493718` = Interact→Locomotion），并在两个状态的 `m_Transitions` 各追加 1 个引用 | 新块 `:147`、`:1250`；引用 `:344`、`:184` |
| `Assets/_Project/Scripts/Units/CharacterBrain.cs` | `OnCommandMove` 加技能门 + 按状态发 `Interrupt`；新增 `InSkillAnim()`、`IsSkillState()` | `:59-95` |
| `Assets/_Project/Scripts/Combat/MeleeCombat.cs` | `CancelAttack()` 删掉 `_animator.SetTrigger("Interrupt");`（改由 `CharacterBrain` 按状态发） | `:245-254` |
| `Assets/_Project/Scripts/Combat/RangedCombat.cs` | `CancelAttack()` 删掉同一句 `_animator.SetTrigger("Interrupt");`（收尾 AC-6，保留 `ResetTrigger(attackTrigger)`） | `:76-85`，`git diff` = -1 行 |
| 新增（只读产物） | `Docs/QA/Evidence/INTERRUPT-01/**`（9 个文件，含 `residual_ranged_after_fix.txt`）、本 REPORT | — |

未触碰：`OC_*`、场景、预制体、`.asset`、`ProjectSettings`、`Packages`、`SkillSystem`/技能效果、`CURRENT.md`、`MonsterBrain.cs`（其 `:112` 的无条件 `Interrupt` 属怪物 `Skeleton.controller` 自有过渡，且不在授权文件内）。
（`ProjectSettings/ProjectSettings.asset` 的 `runInBackground 0→1` 为更早会话遗留的未提交改动，`LastWriteTime 19:16:26` 早于本单收尾，本单未触碰。）

**与 TASK §2 的偏差（已按需求优先级取舍，理由见 §4）**
1. §2 的代码片段只判断 `GetCurrentAnimatorStateInfo`；本实现同时判断“入场过渡中的 `GetNextAnimatorStateInfo`”（否则按下移动后这一刀会在移动中补播）。→ +4 行。
2. §2 未提技能门，但 `OnCommandMove` 里既有的无条件 `CancelAttack()` 会清零 `_timer`，直接违反 R-3（已用 red 证据固定）。→ +12 行 `InSkillAnim/IsSkillState`，并把它包住原有的 `CancelAttack` 调用。
3. 影响面清单里 CharacterBrain 预估“+≤8 行”，实际 +35/-1；多出的部分全部是上述两项需求的实现，无其它重构。

## 4. 测试数据

### 4.1 协议
- 全部用 `execute_code` 在 Play 中驱动：`CharacterBrain.CommandAttack/CommandMove`、`SkillSystem.Cast`、读 `Animator.GetCurrentAnimatorStateInfo/GetNextAnimatorStateInfo/IsInTransition`、反射读 `MeleeCombat._timer`，采样器挂在 `EditorApplication.update`（≈200 fps，采样间隔 4–7 ms）。
- 目标为运行期 `Instantiate(Assets/_Project/Prefabs/Skeleton_Warrior.prefab)` 的实例（禁用 `MonsterBrain`、每次重置 HP），不改任何资产；退出 Play 后自动消失。
- 时序类全部 N≥3（AC-1/2/3 各 3 次，AC-4 2 次）。

### 4.2 AC-1（`attack_by_move.txt`）

| run | 模式 | 移动时 `_timer` | 移动时 norm | 落定耗时 | 目标 HP | 结论 |
|---|---|---|---|---|---|---|
| 1 | AC 窗口（下单+0.05s） | 0.054 s | 37.63* | 0.307 s | 60→60 | BOUNDARY |
| 2 | AC 窗口 | 0.053 s | 0.815 | 0.306 s | 60→60 | BOUNDARY |
| 3 | AC 窗口 | 0.052 s | 0.810 | 0.305 s | 60→60 | BOUNDARY |
| 4 | 对照（AttackMelee 已 current） | 0.252 s | 0.237 | 0.108 s | 60→60 | PASS |
| 5 | 对照 | 0.250 s | 0.235 | 0.104 s | 60→60 | PASS |

\* run1 的 `normalizedTime` 是循环状态的累计值（37.63），不影响结论；实际打断点在 norm≈0.16 附近。

状态序列（run 1，节选）：`Locomotion(next=AttackMelee) +0.007s → +0.15s → AttackMelee +0.200s → next=Locomotion +0.205s → Locomotion +0.307s`。
即：入场淡入无法从代码侧取消，`Interrupt` 被保留到 `AttackMelee` 成为 current 的那一刻才被消费（`Interrupt` 触发器语义），随后 0.1 s 混合回 `Locomotion`。

### 4.3 AC-3 red→green（技能门）

| 版本 | run | 移动后 `_timer` | 技能伤害 | 结论 |
|---|---|---|---|---|
| red（无技能门，TASK §2 原样） | 1/2/3 | -1 / -1 / -1 | 60→60（未结算） | FAIL ×3 |
| green（加 `InSkillAnim`） | 1/2/3 | 0.169 / 0.158 / 0.161 且持续增长 | 60→30 | PASS ×3 |

### 4.4 AC-2 / AC-4 摘要

- AC-2：`Interact` norm≈0.095 时下移动 → `next=Locomotion` 在 +0.004~0.007 s、`cur=Locomotion` 在 +0.104~0.110 s（3/3 ≤0.15 s）。
- AC-4：技能中移动 → 技能自然结束 +1.183 s → `CommandAttack` → `AttackMelee` 0.649 s 后正常 exit（exitTime 0.75），期间 `ATK +0.551s` 处才出现 `next=Locomotion`，**没有**在入场瞬间被切；伤害 15（技能 30 + 普攻 15 = HP 60→15）。

### 4.5 AC-6（`residual_ranged_after_fix.txt`，修复后同一复现 ×3）

| run | 移动时 `_timer` | 移动→落定 Locomotion | `Interact` 生命周期 | 修复前 | `interactMaxNorm` | 结论 |
|---|---|---|---|---|---|---|
| 1 | 0.333 s（已放箭） | 0.866 s | **1.158 s** | 0.022 s | 0.885 | PASS |
| 2 | 0.017 s（未放箭） | 0.873 s | **1.156 s** | 0.022 s | 0.883 | PASS |
| 3 | 0.016 s（未放箭） | 0.872 s | **1.155 s** | 0.022 s | 0.882 | PASS |

- 判定方式：`Interact` 离场时的 `maxNorm` 落在 0.88（> 状态自身 `exitTime=0.8077`），且离场前经过 `cur=Interact next=Locomotion` 的**正常混合**，而不是修复前那种 `cur=Interact next=Locomotion norm≈0.08` 的 1 帧闪断。
- 说明：本次编辑器未获焦点，Play 帧率约 3 fps，逐帧采样偏粗；秒切与自然收招的差别体现在 `maxNorm`（0.08 vs 0.88），不依赖亚帧计时，结论不受帧率影响。
- 远程普攻回归由 SPEC01 覆盖（见 §2 AC-6 远程回归行）：`Attack_Draw → Attack_Release` 仍在释放窗口内每次周期各放 1 箭。

## 5. 实现原理

1. **谁能被移动打断，由状态图决定**：控制器只给 `AttackMelee`、`Interact` 各一条 `Interrupt`（If）→ `Locomotion` 的过渡（`PlayerBase.controller:147`、`:1250`，`HasExitTime=False, Duration=0.1, InterruptionSource=Source, CanTransitionToSelf=False`）。`Skill/Bash/Q_1/W_1/E_1/R_1` 没有这条过渡，所以按移动天然切不走 —— 代码里不需要维护“技能白名单”，只需要不要把它们误判成可打断状态。
2. **代码只决定“发不发”，不决定“怎么切”**：`CharacterBrain.cs:59-81` 在 `CancelAttack()` 之后判断 `GetCurrentAnimatorStateInfo` 与（过渡中）`GetNextAnimatorStateInfo`，命中 `AttackMelee/Interact` 才 `SetTrigger("Interrupt")`。这样技能状态不会被置位，**不会有残留触发器**。
3. **技能门**：`CharacterBrain.cs:84-95` 的 `InSkillAnim()` 在技能状态（含入场过渡）时整体跳过 `CancelAttack()`；否则 `MeleeCombat.CancelAttack()` 的 `_timer = -1f`（`MeleeCombat.cs:245-248`）会把技能命中窗口一起废掉——这就是 §4.3 的 red。
4. **不留残留的两道保险**：① 只有可打断状态才置位（§5.2）；② `MeleeCombat.DoAttack` 每次起手前 `ResetTrigger("Interrupt")`（`MeleeCombat.cs:278`，既有代码），即使有历史残留也会在下一刀起手前被清掉。AC-4 实测 0.649 s 完整挥砍即验证这两道。
5. **删除 `CancelAttack` 里的 `SetTrigger("Interrupt")`（`MeleeCombat.cs:245-254`、`RangedCombat.cs:76-85`）**：那是“任何取消都打断”的旧语义，与新过渡叠加会误伤技能路径（技能被移动取消时会把 `Interrupt` 留着，等下一次 Interact 被秒切）。
6. **远程为什么可以直接删、不需要替代**：`RangedCombat` 的取消路径只做两件事——停自己的计时（`_timer = -1f`）和清 `AttackRange` 触发器；动画侧 `Attack_Draw → Attack_Release → Locomotion` 全由 `exitTime` 过渡驱动（`PlayerBase.controller` 中两状态各 1 条 exitTime 边），**没有任何一条边以 `Interrupt` 为条件**。所以旧代码里那句 `Interrupt` 对远程自己毫无作用，唯一效果是把触发器留到下一次进入 `Interact` 时被新过渡消费——正是 AC-6 修掉的那个秒切。删掉后 `CharacterBrain.OnCommandMove` 仍照常判断 `AttackMelee/Interact` 才发 `Interrupt`，远程状态不在名单内 → 不再产生残留。

**三句话版本（TASK §5 要求）**
- **为什么用触发器而不是速度参数**：`SpeedZ/SpeedX` 描述的是“我正以多快速度移动”这个持续状态，摇杆微动、风筝走位、寻路抖动都会触发它，误打断率高；`Interrupt` 是“玩家刚刚下达了移动指令”这个一次性意图，只在 `OnCommandMove` 里发一次，语义单一、可控。
- **为什么技能不加这条过渡就天然免疫**：Animator 里“能不能被打断”是边的存在性，不是运行期的 if；`Skill` 等状态没有 `--(Interrupt)--> Locomotion` 这条边，于是无论移动指令怎么发，状态机都没有可走的路径，动画自然不切。
- **残留触发器为什么会误伤下一次攻击**：Trigger 是“置位后等待某条过渡消费”的一次性开关，如果当时的源状态没有消费它的过渡（例如技能状态），它就一直挂着；等到下一次进入带该条件的源状态（如 `Interact`），过渡在第一帧就成立 → 动画刚进就被切走（本次在弓手上实测 0.022 s 被切，见 `residual_ranged_interrupt.txt`）。

## 6. 受影响模块导航

| 模块 | 位置 | 本次影响 |
|---|---|---|
| 玩家指令入口 | `CharacterBrain.CommandMove/CommandAttack/CommandInteract` → `UnitBrain.OnCommandMove` | 移动指令现在会先判定技能/普攻/交互，再决定是否打断 |
| 近战执行器 | `MeleeCombat.CancelAttack/DoAttack` | `CancelAttack` 不再发 `Interrupt`；技能仍由 `IsSkill` 路径驱动 |
| 动画状态机 | `PlayerBase.controller` Base Layer | 仅 2 条新边；6 条 AnyState 与既有 exitTime 过渡未动（`controller_transitions.txt`） |
| 交互 | `CharacterBrain.Update`（到达后 `SetTrigger("Interact")` + `Chest.OpenBy`） | 交互中移动会被切走且 `_pendingInteract` 保持为 null（原有逻辑） |
| 技能 | `SkillSystem.Cast` → `SkillEffectBase.Execute` | 未改代码；移动不再清零技能计时 |

## 7. 未决项（需所有者/大脑裁决，我未自行取舍）

1. **`RangedCombat.CancelAttack()` 的残留 `Interrupt` — 已修复（AC-6）**
   - 原现象：弓手攻击中按移动 → 该行置位 `Interrupt`，无过渡消费；下一次 `Interact` 只因新加的 `Interact→Locomotion(Interrupt)` 过渡而**在 0.022 s 被秒切**（`residual_ranged_interrupt.txt`；法师同构）。
   - 处理：删掉 `RangedCombat.cs:83` 的 `_animator.SetTrigger("Interrupt");`，打断统一由 `CharacterBrain` 按状态发。复现重跑 ×3 → `Interact` 生命周期 1.158/1.156/1.155 s（`residual_ranged_after_fix.txt`），远程普攻窗口由 SPEC01 覆盖（12/0，rangedPass=True）。
   - 留给后续的同类隐患：`MonsterBrain.cs:112` 仍无条件发 `Interrupt`，但怪物用 `Skeleton.controller`（自有 `Interrupt` 过渡），且该文件不在本单授权内，未改。
2. **AC-1 的入场淡入窗口（0.05–0.10 s）落定 0.305–0.307 s**
   - 原因：`Locomotion→AttackMelee` 的既有过渡 `Duration=0.15`、`InterruptionSource=None`，一开始就无法从代码侧取消；只能在 `AttackMelee` 成为 current 后消费 `Interrupt`（0.15 s 淡入 + 0.1 s 混合）。
   - 现状：该窗口内**无伤害**（`_timer` 立刻 -1）、**无残留**（AC-4 PASS），只是动画会“闪一刀”。
   - 选项 A：接受，把 AC-1 的 0.15 s 改成“普攻已进入 `AttackMelee` 后 ≤0.15 s；入场窗口 ≤0.35 s”。
   - 选项 B：授权把既有 `Locomotion→AttackMelee` 过渡的 `InterruptionSource` 改为 `Destination`（或把该过渡 `Duration` 从 0.15 缩到 0.05），即可在窗口内也 ≤0.15 s。
3. **技能门用状态名白名单**（`Skill/Bash/Q_1/W_1/E_1/R_1`，`CharacterBrain.cs:91-95`）
   - 理由：`MeleeCombat._isSkill` 是私有字段，`ICharacterCombat` 无查询接口；本单只允许改 `MeleeCombat` 删一行。
   - 代价：将来新增技能状态需同步这个名字列表。可选替代（需评估调用方）：在 `MeleeCombat.CancelAttack()` 里 `if (_isSkill) return;`。
4. **AC-3 文案与技能状态自身 exitTime 的冲突**
   - `Skill` 状态自带 `exitTime≈0.81`（×1.3 s ≈ 1.05 s），所以“技能播放中 1 s 内状态始终为 Skill”在自然收招边界会不成立；我按“自然结束前不被切走 + 伤害结算”判定 PASS。若要严格 1 s，需改技能状态 exitTime（不在本单）。

## 8. 讲稿

**不适用**。理由：本任务是 2 条状态机过渡 + 一处状态判断（+ 一处技能门），没有新架构、无新增数据资产；讲解点已按 TASK §5 要求并入本文 §5（实现原理 + 三句话版本）。

## 9. 建议提交

- 标题：`INTERRUPT-01: 移动指令打断普攻/交互，技能免疫`
- 正文：
  `PlayerBase.controller 新增 AttackMelee/Interact → Locomotion 的 Interrupt 过渡；CharacterBrain.OnCommandMove 按当前状态决定是否发触发器、技能进行中跳过 CancelAttack 以保证技能伤害结算；MeleeCombat/RangedCombat 的 CancelAttack 不再无条件发 Interrupt（远程因此不再把残留触发器带到下一次 Interact）。SPEC01 12/0。`
- 提交范围：`Assets/_Project/Art/Animations/PlayerBase.controller`、`Assets/_Project/Scripts/Units/CharacterBrain.cs`、`Assets/_Project/Scripts/Combat/MeleeCombat.cs`、`Assets/_Project/Scripts/Combat/RangedCombat.cs`、`Docs/QA/Evidence/INTERRUPT-01/`、本 REPORT。
- §7-1 已在本批解决，`RangedCombat.cs` 的一行改动与上述文件同批提交；§7-2（入场淡入窗口）仍需所有者裁决。
