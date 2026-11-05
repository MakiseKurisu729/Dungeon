# REPORT_INTERRUPT-02

| 项 | 值 |
|---|---|
| 任务 ID | `INTERRUPT-02` |
| 风险等级 | 高风险（Animator 过渡 + 战斗指令入口） |
| 执行者 | OpenCode + DeepSeek（执行者角色） |
| 完成时间 | 2026-11-05 22:10 |
| Unity | 2022.3.62f3c1 / `Dungeon@4f5c34c82f31646e`（模式 B：MCP 常开编辑器） |
| 授权记录 | 所有者 2026-11-05 在 `TASK_INTERRUPT-02.md` §0 授权本单改 `PlayerBase.controller` + `CharacterBrain.cs`；MCP 写操作限定在 §2 影响面清单内 |

## 1. 结果摘要

三部分全部完成，验证全部真跑：

- **A 布局**：10 个状态 + 4 个子机 + AnyState 重排为「三列」。七条目检状态（AttackMelee / Attack_Draw / Attack_Release / Bash / Skill / Hit_A / Interact）的 `AnyState→X` 与 `X→Locomotion` 连线在估算模型下均不穿块；仍有 2 条 AnyState→技能子机的线穿块（详见 §5 未决项）。**AC-1 需所有者目检确认**（过渡线是编辑器渲染产物，无 API 可断言）。
- **B 远程打断**：新增 `Attack_Draw→Locomotion`、`Attack_Release→Locomotion` 两条 `Interrupt` 过渡；`OnCommandMove` 的可打断状态集合扩到 `{AttackMelee, Attack_Draw, Attack_Release, Interact}`。弓箭手/法师 在 draw、release 各 3 次：**0.104–0.113s 回 Locomotion，弹丸 0 个，伤害 0**；对照组（不打断）**弹丸 1 个、伤害 12/16**。
- **C 技能指令锁**：`InSkillState()` 只看当前状态名，锁住 `CommandMove / CommandAttack / CommandInteract`（只作用于玩家入口）。骑士 Q(盾击)/W(跳劈) 各 3 次：起手 0.10s 后下单移动 → `agent.destination` 变化 0.0000、`posDrift=0.000`、仍处 `Bash`/`Skill`、伤害照常（15 / 30）；技能中下单攻击/交互 → 目标不变、`_pendingInteract=null`、Animator 从未进 `Interact`；技能自然结束 0.10s 后下单移动 → destination 立即接受、0.5s 内移动约 0.95m、`CommandAttack` 也被接受（无残留）。
- **SPEC01**：最终资产状态下 `Tools/QA/In-Editor/SPEC01 (green)` = **12 通过 / 0 失败 / exit 0**（`editor_run17`）。

## 2. 证据锚点

目录 `Docs/QA/Evidence/INTERRUPT-02/`（canonical 文件只追加：失败/重跑版本以 `_attemptN_*` 后缀并存，不覆盖）

| AC | 文件 | 说明 |
|---|---|---|
| AC-1 | `positions.txt`、`transitions.txt` | 坐标读回 + 估算穿块清单 + 完整过渡表（含 isrc/ordered/exit/cond） |
| AC-2 | `ranged_draw.txt`、`ranged_final_confirm.txt` | 6 次打断 + 2 次对照；最终资产状态再确认 2 次打断 + 1 次对照 |
| AC-3 | `ranged_release.txt`、`ranged_final_confirm.txt` | 6 次打断 + 2 次对照；最终资产状态再确认 2 次打断 + 1 次对照 |
| AC-4 | `lock_move.txt` | Q×3、W×3：destination / posDrift / 技能状态 / 伤害 |
| AC-5 | `lock_other.txt` | Q×3、W×3：目标、`_pendingInteract`、Interact 状态监视 |
| AC-6 | `unlock.txt` | Q×3、W×3：解锁后移动 + 攻击控制组 |
| AC-7 | `git_diff_stat.txt`、`SPEC01_Summary_editor_run17_final.json`、`SPEC01_InEditorResult_editor_run17_final.txt` | diff + 12/0 |
| 过程留痕 | `ranged_draw_attempt1_crossfire.txt`、`lock_*_attempt1_q_exittime.txt`、`lock_*_attempt2_stale_controller.txt`、`runInBackground_prev.txt` | 见 §5 未决项 1/2 |

## 3. AC 回填

| AC | 断言 ID | 结果 | 实测数据 | 证据 |
|---|---|---|---|---|
| AC-1 | 目检-1 | **待所有者目检**（自动化不可断言） | 位置读回逐项一致；估算穿块：七条目检状态的 `X→Locomotion` 与 `AnyState→X` **0 穿块**；残留 `AnyState→Q` 穿 Bash、`AnyState→E` 穿 Skill、`Rise/Locomotion↔Downed` 同列互穿 | `positions.txt` |
| AC-2 | `Interrupt.RangedDraw` | **PASS**（3+3 次） | Archer：0.113 / 0.107 / 0.107s 回 Locomotion，projPeak=0，hpΔ=0；Mage：0.105 / 0.107 / 0.108s，projPeak=0，hpΔ=0；对照 Archer 弹丸 1 / hpΔ=12、Mage 弹丸 1 / hpΔ=16 | `ranged_draw.txt` |
| AC-3 | `Interrupt.RangedRelease` | **PASS**（3+3 次） | Archer：0.106 / 0.108 / 0.106s，projPeak=0，hpΔ=0（下单时 release norm 0.339–0.343）；Mage：0.105 / 0.106 / 0.106s，projPeak=0，hpΔ=0（norm 0.129–0.132）；对照 弹丸 1 / hpΔ=12、16 | `ranged_release.txt` |
| AC-4 | `Skill.LockMove` | **PASS**（Q×3、W×3） | 下单时刻 state：Q=`Bash`、W=`Skill`；`destDelta=0.0000`；+1.35s 时 `posDrift=0.000`、`destNow` 仍是原值；伤害照常 Q=15 / W=30；技能自然结束于 +1.354–1.359s | `lock_move.txt` |
| AC-5 | `Skill.LockOther` | **PASS**（Q×3、W×3） | `orderedBefore=Skeleton_Warrior` → `CommandAttack(Mage)` 后仍为 `Skeleton_Warrior`；`CommandInteract(Mage)` 后 `_pendingInteract=null`；全程 `sawInteractState=False` | `lock_other.txt` |
| AC-6 | `Skill.UnlockAfter` | **PASS**（Q×3、W×3） | 技能结束 +0.10s 下单：`destDelta=0.0000`（destination 已切换）；0.5s 后距 2m 目标 1.043–1.058m（实际移动约 0.95m）；随后 `CommandAttack` 被接受；`isStopped=False` | `unlock.txt` |
| AC-7 | `all` | **PASS** | 本单只改 `PlayerBase.controller` + `CharacterBrain.cs`（`git status` 其余为开工前既有改动，见 `git_diff_stat.txt` note）；SPEC01 `editor_run17` = pass 12 / fail 0 / exit 0 | `git_diff_stat.txt`、`SPEC01_Summary_editor_run17_final.json` |

运行次数：AC-2/3/4/5/6 均 ≥3 次（时序类满足 N≥3），并各带 1 次对照组。

## 4. 改动清单

| 文件 | 改动 |
|---|---|
| `Assets/_Project/Art/Animations/PlayerBase.controller` | ① 新增 `Attack_Draw→Locomotion`：`[Interrupt If]`、`hasExitTime=false`、`duration=0.1`、`interruptionSource=Source`、`orderedInterruption=true`、`canTransitionToSelf=false`；② 新增 `Attack_Release→Locomotion`：参数同上；③ 布局：10 状态 + 4 子机坐标重排（三列）；④ `m_AnyStatePosition` → (300, 600)；⑤ `Locomotion→Bash` 的 `hasExitTime` 1→0（见 §5 未决项 2） |
| `Assets/_Project/Scripts/Units/CharacterBrain.cs` | ① 新增 `InSkillState()`（只看当前状态名）；② 新增 `IsBreakableByMove(AnimatorStateInfo)`，含 `Attack_Draw`/`Attack_Release`；③ `OnCommandMove` 顶部加技能锁，打断判定改用 `IsBreakableByMove`；④ `CommandAttack`/`CommandInteract` 顶部加技能锁；⑤ `Update` 中技能期间不做追击/普攻决策 |
| `Docs/QA/Evidence/INTERRUPT-02/**`、`Docs/Handoff/REPORT_INTERRUPT-02.md` | 本单证据与报告 |

未改：`OC_*`、场景、预制体、`.asset`、`ProjectSettings`、`Packages`、`SkillSystem`/技能效果、其他脚本、`.meta`、GUID。

## 5. 测试数据（关键摘录）

```
# B / Attack_Draw（弓箭手，第 2 次）
[0.159s] cur=Attack_Draw          <- 起手
[MOVE] at t=0.159s stateBefore=Attack_Draw norm=0.345 -> CommandMove(+3m z)
[0.161s] cur=Attack_Draw next=Locomotion
[0.266s] cur=Locomotion
[RESULT] moveToLoco=0.107s projPeak=0 hpDelta=0.0 verdict=PASS
对照：projPeak=1 hpDelta=12.0 verdict=PASS

# C / W(跳劈) 第 1 次
[CAST] t0 castOk=True manaBefore=100.0 manaAfter=70.0 cd=6.00
[0.056s] cur=Skill
[MOVE +0.104s] stateAtMove=Skill destBefore=(-2.72,0.08,0.00) destAfter=(-2.72,0.08,0.00) destDelta=0.0000
[+0.101s] orderedAfterCommandAttack(mage)=Skeleton_Warrior pendingInteractAfterCommandInteract=null
[+1.353s] state=Locomotion posDrift=0.000 hpNow=5030.0 hpDrop=30.0
[RESULT] verdict=PASS
```

## 6. 实现原理

1. **锁的判定口径**（`CharacterBrain.cs:104-108`）：`InSkillState()` = `IsSkillState(GetCurrentAnimatorStateInfo(0))`，状态名集合 `Skill/Bash/Q_1/W_1/E_1/R_1`（同时覆盖旧触发器版 `Bash/Skill` 与 Phase B 的 `Q_1..R_1`）。**不看 `IsInTransition`**：出口过渡开始后即视为已解锁，避免“技能结束还锁着”。
2. **移动指令锁**（`CharacterBrain.cs:66-69`）：`OnCommandMove` 第一行 `if (InSkillState()) { ClearMove(); return; }` —— 在 `base.OnCommandMove`（唯一 `agent.SetDestination` 入口，`UnitBrain.cs:41-44`）之前返回，所以不 SetDestination、不 `CancelAttack`、不发 `Interrupt`、不通知 `DownedRescue`；`ClearMove()` 顺手把基类 `_hasMove` 清掉，防止技能结束后补跑一条过期移动。
3. **可打断集合**（`CharacterBrain.cs:97-101`）：`IsBreakableByMove` 统管 `AttackMelee / Attack_Draw / Attack_Release / Interact`，`current` 与 `next` 状态都过一遍，覆盖“过渡进入攻击态”的 1–2 帧。
4. **技能起手瞬间的保护**（`CharacterBrain.cs:74`、`:111-116`）：`InSkillAnim()` 保留“过渡进入技能”的判定，只用于**不调用 `CancelAttack`**（否则 W 的 `MeleeCombat._timer` 会被刚起手的技能自己清掉）。它不参与锁判定，锁仍只认当前状态名。
5. **指令入口全覆盖**（`CharacterBrain.cs:40-50`、`:142-143`）：`CommandAttack` / `CommandInteract` 顶部同样早退；`Update` 在技能期间不做索敌/追击，避免自动追击绕过“指令无效”改 destination。怪物 AI 走 `MonsterBrain`，本单未改一行，锁不会影响怪物。
6. **远程打断靠过渡而非代码**（controller）：`RangedCombat.CancelAttack` 清 `_timer`（`RangedCombat.cs:76-84`），真正把人送回 `Locomotion` 的是控制器上的 `[Interrupt]` 过渡；两条新过渡的字段与 `INTERRUPT-01` 的 `AttackMelee→Locomotion` 完全一致。

**三句话版本**：动画机重排成三列后 `AnyState` 与 `X→Locomotion` 的线基本走空走廊；远程普攻的前摇/释放靠新增的两条 `[Interrupt]` 过渡被移动打断（0.1s 回 Locomotion、不出箭）；技能靠“当前状态名 ∈ {Bash, Skill, Q_1…R_1} 就整条指令丢弃”的玩家侧锁免疫移动/攻击/交互，状态自然退出即解锁。

## 7. 受影响模块导航

| 模块 | 入口 | 本单影响 |
|---|---|---|
| 玩家指令入口 | `CharacterBrain.CommandAttack / CommandInteract / OnCommandMove` | 新增技能锁（只影响玩家） |
| 玩家自动决策 | `CharacterBrain.Update` | 技能期间提前 return，不再追击/普攻 |
| 远程普攻 | `RangedCombat.AutoAttack / Update / CancelAttack`（未改） | 靠控制器新过渡回到 Locomotion，`CancelAttack` 照旧清计时 |
| 近战/技能 | `MeleeCombat.DoSkillAttack`、`BashEffect.BashRoutine`（未改） | 技能动画现在**立即**起手（`Locomotion→Bash hasExitTime=false`），伤害时点不变 |
| 动画机 | `PlayerBase.controller` Base Layer | 布局、2 条新过渡、AnyState 位置、1 处 hasExitTime |

## 8. 未决项 / 已知残余

1. **AnyState 坐标偏离工单建议**：工单要求 `(520, -160)`，实测会让 `AnyState→Hit_A` 成为沿 x=520 的竖直长线，穿过 5 个状态块；改为 `(300, 600)`（列间空走廊）。`anyStatePosition` 实际是公开可写属性，未使用 `SerializedObject`。
2. **`Locomotion→Bash` 的 `hasExitTime` 由 1 改为 0（计划外但必要）**：原值为 `true` + `exitTime=0.732`，导致 Q 起手后要等 Locomotion 循环到 73% 才进 `Bash`（实测延迟 0.37–0.82s），这段窗口里移动指令会被正常接受、角色边跑边出伤，`Skill.LockMove` 无法达成。改为 `false` 后与 `Locomotion→Interact/Attack_Draw/AttackMelee/Hit_A` 一致（同一文件内唯一一处 `m_HasExitTime` 改动：HEAD 13 处 → 现在 12 处，`git diff` 可核）。**若所有者认为该改动属于计划外，请指示回滚，则 AC-4 的 Q 分支只能在“已进入 Bash 之后”才成立。**
3. **残留穿块连线（未用改过渡条件/时长去迁就）**：`AnyState→Q` 穿 `Bash`、`AnyState→E` 穿 `Skill`、`Rise↔Locomotion` 与 `Downed` 同列互穿。全部为估算（节点尺寸按默认 170×48 / 子机 210×64），最终以所有者目检为准。
4. **技能锁的过渡窗口（≤0.05s）**：锁只看当前状态名，`Locomotion→技能` 的 0.05s 过渡期间移动指令仍会被接受（只是不会 `CancelAttack`、不会取消技能）。这是按工单「判定用当前状态名、别用 IsInTransition」的直接后果；0.1s 后测试不受影响。若要补，需要引入“施法意图”标志，属新决策。
5. **测试隔离口径（不是缺陷，但需知情）**：运行期驱动测试里把三名玩家的 `CharacterBrain.enabled=false`、把骷髅 `MonsterBrain`+NavMeshAgent 停用、把骷髅当固定靶（`maxHp+5000`），以排除自动索敌/同场其他角色抢靶的噪声（第一轮就出现过法师给弓箭手的靶子补了一发火球，见 `ranged_draw_attempt1_crossfire.txt`）。锁本身在 `CommandMove/CommandAttack/CommandInteract` 三个入口，停用 `Update` 不影响被测路径；SPEC01 走生产入口（`SelectionManager`）12/0 另作交叉验证。
6. **第二次 C 轮跑在旧控制器上**：改完 `hasExitTime` 未重进 Play 就重跑，运行中的 Animator 仍是旧控制器，结果与修改前一致（`lock_*_attempt2_stale_controller.txt`）。停 Play→重进→重跑才是 `lock_*.txt` 的最终数据。
7. **编辑器焦点/帧循环**：编辑器不聚焦时 Play 帧循环冻结（`Time.time` 卡在 0.02s），按工单允许的方式临时 `Application.runInBackground=true` 驱动，收尾已还原为 `false`（`runInBackground_prev.txt`）；`ProjectSettings` 的 git 状态为空。

## 9. 建议提交

代码与资产由所有者自行提交；建议拆两条（或一条也行）：

```
标题：INTERRUPT-02 远程前后摇可被打断 + 技能期间玩家指令锁定 + 动画机三列布局
正文：给 Attack_Draw/Attack_Release 补 Interrupt 过渡并在玩家指令入口加技能状态锁，同时重排 PlayerBase.controller 节点坐标减少连线穿块。
```

```
标题：INTERRUPT-02 修正 Locomotion→Bash 的 hasExitTime，技能动画即时起手
正文：Bash 过渡等 Locomotion 循环点才触发，导致 Q 起手 0.3~0.8s 内仍可被移动指令打断并照常出伤。
```

`Docs/`（本 REPORT + 证据）可与上面同批提交；提交前请先目检 AC-1 的过渡线。
