# PROPOSAL — 技能动画 Q/W/E/R 化与技能槽可替换

> 状态：**待所有者决策**（阶段划分与 3 个口径需拍板）
> 关联：`TASK_SPEC-DIAG-SKILL`～`-3`（已完成的技能链修复）；本文只规划下一步，不改代码、不动资产。

## 1. 实测问题诊断（只读核查）

### 1.1 瞄准态"指针不是攻击指针" —— 已证实是代码写死

`Assets/_Project/Scripts/UI/CursorManager.cs:52-53`：

```csharp
if (SkillInputController.instance != null && SkillInputController.IsTargeting)
{ SetHover(Hover.Pointer); Apply(pointerCursor, pointerHotspot, Color.white); return; }
```

瞄准期间**直接强制普通指针并 return**，后面第 55 行开始的射线与敌人判定根本不执行。所以不是"没识别到敌人"，而是**根本没做识别**。要做攻击指针，必须在瞄准分支里继续做 hover 判定。

### 1.2 点击敌人技能不释放 —— 代码路径在，但被三件事放大成"像没反应"

1. **射程 2.5m**：`BashEffect.range=2.5`。骑士出生点 `(-2.72, 0, 0)`，骷髅在 `(-2.72, 0, -12)`，相距 **12m**。点击敌人不会立即释放，而是进入 `_chase` 追放（`SkillInputController.cs:99-107`），需要走到 2.5m 内才自动施放。若中途被挡/没注意走位，看起来就是"点了没放"。
2. **Bash 过渡带 `Has Exit Time = 1`、`Exit Time = 0.732`**（`PlayerBase.controller` 的 `Bash` 过渡）：即使 `Cast` 成功、触发器已写入，动画也要等源状态走到 73% 才切。相位不对时最多延迟一个循环，手感就是"没放出来"。
3. **攻击态没有到技能的过渡**：控制器里 `Bash`/`Skill` 只从 `Locomotion` 进入；`AttackMelee/Attack_Draw/Attack_Release` 只能回 `Locomotion`。所以"普攻中按技能"动画切不过去（详见 1.3）。

> 结论：`SkillSystem.Cast` 可能已经成功（扣蓝/进 CD/结算），但动画没切或延迟，于是被感知为"技能没释放"。
> 建议用 5 行临时日志确证"点击那一帧 `PickEnemy` 是否命中、距离/射程、Cast 返回值"，再定是否还有第四个原因（拾取失败）。本提案不臆断。

### 1.3 普攻时切不到技能态 —— 已证实是动画机结构问题

- `Bash`：`HasExitTime=1, ExitTime=0.732`，且唯一入口是 `Locomotion`。
- `Skill`：`HasExitTime=0`，但同样只从 `Locomotion` 进入。
- 攻击状态没有任何到 `Bash/Skill` 的过渡；`MeleeCombat.DoAttack` 还有 `_timer>=0` 拒绝新起手的代码闸门（`DoSkillAttack` 会先清零，代码层可打断，动画层不行）。

这与你的判断一致：**不是数值问题，是状态机结构问题**。

## 2. 目标架构（你的方案 + 必要补充）

### 2.1 控制器（`PlayerBase.controller`，所有者改）

- 删除 `Bash`、`Skill` 两个状态与同名参数。
- 新增 `Q`、`W`、`E`、`R` 四个状态，Trigger 同名（`Q/W/E/R`）。
- 过渡：
  - `AnyState → Q/W/E/R`：`Has Exit Time = 0`、`Interruption Source = Current State`、`Can Transition To Self = 0`、过渡时长 0.05。
  - `Q/W/E/R → Locomotion`：`Has Exit Time = 1`、`Exit Time = 1`（播完回待机）。
- 保留 `Locomotion / AttackMelee / Attack_Draw / Attack_Release / Hit_A / Downed / Rise / Interact`。
- 四个状态使用**占位 Clip**（建议同一段空/极短 Clip），真正的动作交给覆盖器。

### 2.2 覆盖器（`OC_Knight / OC_Archer / OC_Mage`，所有者改）

- 每个覆盖器把 Q/W/E/R 四个占位 Clip 映射到该角色的技能动作切片。
- 通用动画器不再出现任何角色专属动作；换角色只改覆盖器。
- 这套文件已经存在（`Assets/_Project/Art/Animations/OC_*.overrideController`），只是当前覆盖项里没有"技能槽"概念。

### 2.3 槽位 = 动画通道（代码）

- 第 i 槽固定触发 `Q/W/E/R[i]`，**技能资产不再硬编码 `Bash`/`Skill`**。
- `SkillEffectBase` 增加施法上下文（`slot / trigger / stateName`），`SkillSystem.Cast` 统一注入；`BashEffect` / `HeavySlashEffect` / `FireballEffect` 从中取动画名。
- 命中帧改为**归一化**：效果资产用 `hitNormalized`（0~1）而不是绝对秒；运行时读当前状态时长换算。原因：不同角色/不同技能剪辑长度不同，绝对秒会对不齐。保留 `hitTime` 作为旧资产兼容回退。

### 2.4 运行时换技能（后续，第三阶段）

- 新增 `SkillSlotAnimator.SetSlotClip(int slot, AnimationClip clip)`：
  - 取 `Animator.runtimeAnimatorController as AnimatorOverrideController`；
  - 以该槽的基准 Clip 为 key 写入 `overrideController[baseClip] = clip`。
- 换技能 = 改 `SkillData` 槽位 + 调 `SetSlotClip`；不需要重建整个控制器。
- 注意：`AnimatorOverrideController` 是资产实例，运行时修改会影响共享该 override 的所有实例；若将来同角色多单位各自不同技能，需要每个实例克隆一份 override（届时再评估）。

### 2.5 远程攻击单状态化（新决策：合并 `Attack_Draw` + `Attack_Release`）

**结论：可以合并，推荐在阶段 2 与 QWER 一起做。**

- 依据：`RangedCombat.Update` 本来就是按动画 `normalizedTime` 轮询发射窗口（`releaseWindowStart/End`），两个状态只是历史拆分；前摇/后摇是**同一个 Clip 内的两个阶段**，不需要两个状态。
- 单状态做法：`AttackRange` 一个状态 + 一个完整"拉弓→放箭→收招"Clip；发射时刻 = 该状态 `normalizedTime ∈ [releaseWindowStart, releaseWindowEnd]`。
- 代码改动：`RangedCombat` 增加显式单状态模式——`drawStateName` 留空时，直接读 `releaseStateName` 的**原始** `normalizedTime` 与窗口比较（不再走 `releasePoint` 映射）；`IsAttackAnimPlaying` 同步。
- 技能远程（弓箭手技能）：同理，槽位状态 `Q` 用完整"拉弓→放箭"Clip；新增 `RangedSkillEffect`（或复用 Fireball 的时序），在归一化窗口生成箭矢，不再需要两个状态。
- 影响：`SPEC01` 的 `Clock.RangedOneShotPerCycle`、`Attack.DualPathUnified` 要从"draw→release"口径改为"单状态窗口"；`FND00B06` 的 `RequiredStates` 去掉 `Attack_Draw`。这类测试改写放在阶段 2 同批完成。
- 风险：两个远程角色（弓手/法师）的发射窗口要重新标定一次；箭矢/法术的命中时序仍按窗口，不受各角色剪辑长度影响。

## 3. 改动清单

### 3.1 资产（所有者，AI 只读）

| 文件 | 改动 |
|---|---|
| `Assets/_Project/Art/Animations/PlayerBase.controller` | 删 `Bash/Skill`，加 `Q/W/E/R` 状态与 Trigger、AnyState 过渡、回 Locomotion 过渡、占位 Clip |
| `OC_Knight/OC_Archer/OC_Mage.overrideController` | Q/W/E/R 四槽映射到各角色技能动作 |
| `Date/Skills/Q_Bash_Effect.asset`、`W_Slash_Effect.asset` | 若采用归一化：补 `hitNormalized`；否则保持绝对秒 |

### 3.2 代码（AI，中/高风险）

| 文件 | 改动 |
|---|---|
| `Scripts/Combat/SkillEffectBase.cs` | 增加施法上下文（slot/trigger/state）与归一化命中字段 |
| `Scripts/Combat/SkillEffects.cs` | `BashEffect` 用上下文触发 `Q` 等；命中帧改归一化 |
| `Scripts/Combat/HeavySlashEffect.cs` | 传入槽位触发器/状态名，不再用 `"Skill"` |
| `Scripts/Combat/SkillEffectFireball.cs` | 同上（远程技能将来也要槽位化） |
| `Scripts/Combat/MeleeCombat.cs` | `DoSkillAttack(trigger, stateName)`；`ReadAnimProgress` 用本次状态名；技能可打断普攻 |
| `Scripts/Units/SkillSystem.cs` | `Cast` 构造并注入上下文 |
| `Scripts/Player/SkillInputController.cs` | 暴露 `PendingSlot/PendingRange`；点击失败/超距给可见反馈 |
| `Scripts/UI/CursorManager.cs` | 瞄准态继续做 hover：敌人→攻击指针（射程外用不同色/提示） |
| `Scripts/Player/SkillSlotAnimator.cs`（新） | 运行时覆盖 Clip API（第三阶段） |

### 3.3 测试与文档（AI）

- `SPEC01` 的 `Skill.CastSmoke`：`Bash`/`Skill` 断言改为 `Q`/`W` 状态；新增"攻击中按技能可打断"断言。
- `DIAG` 探针：状态名同步。
- `FND00B06` 的 `KnownStates` 增加 `Q/W/E/R`。
- QA/REPORT 文档更新（说明状态改名与兼容影响）。

## 4. 分阶段（建议）

| 阶段 | 内容 | 风险 | 谁做 | 验收 |
|---|---|---|---|---|
| **0** | 提交当前已完成的技能修复批次（含场景组件、W 拆分、扣蓝顺序、测试） | — | 所有者 | 工作区干净、可回滚 |
| **1** | 光标与点击反馈：瞄准态攻击指针、超距提示、失败提示（不改动画机） | 低 | AI 派单 | 人工 1 项 + 现有 SPEC01 不回退 |
| **2** | Q/W/E/R 动画机 + 代码槽位化 + 测试同步 | 高 | 所有者改资产 → AI 改代码 | SPEC01 10/10×3 + 四键人工 |
| **3** | 运行时换技能 API/UI | 中 | AI（先出设计） | 换技能后动画/命中/冷却正确 |

## 5. 风险与回滚

- 高风险点：控制器改名会让现有测试、报告、QA 中的 `Bash/Skill` 字符串失效；必须同步改，否则红。
- 动画时长/命中窗口：归一化后要按各角色剪辑重新标定 `hitNormalized`。
- 回滚：阶段 1 单文件；阶段 2 以控制器/覆盖器资产快照 + 代码提交回滚（阶段 0 的提交就是回滚锚）。
- 资产管辖权不变：控制器、覆盖器、`.asset` 由所有者改；AI 只读。

## 6. 需要拍板的 3 个口径

1. **命中帧归一化**（推荐）还是继续绝对秒？
2. **技能可打断普攻**（推荐 AnyState + 无 Exit Time）还是必须回 Locomotion 才能放？
3. **阶段 1 是否现在就做**（光标/反馈小修，不等动画机）？
