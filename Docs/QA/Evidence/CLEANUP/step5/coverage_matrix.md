# Step A — FND02B 剩余断言 × SPEC01 覆盖对照

> 工单：`Docs/Handoff/TASK_SPEC-01-CLEANUP-2.md` Step A
> 日期：2026-11-05
> 方法：只读。对 `FND02B_ProbeRunner.cs` 与 `SPEC01_ProbeRunner.cs` 的对应方法体做逐行比对（`Compare-Object` + `git diff --no-index -U0/-U1`），并对照 `Docs/QA/Evidence/CLEANUP/step2` 的断言记录。
> 判定字段一律取自源码实际变量名。

---

## 1. 剩余 3 条（step2 红项）

### 1.1 `Locomotion.AllPlayersMoveParam`

| 项 | FND02B | SPEC01 |
|---|---|---|
| 判定字段 | `players[].pass`、`controllerPass` | `players[].pass`、`controllerPass` |
| 玩家行为条件 | `arrived` ∧ `peakAgentSpeed ≥ 2.0` ∧ `peakMoveParam = √(SpeedX²+SpeedZ²) ≥ 2.0` ∧ `stateDuringMove == "Locomotion"` ∧ `finalMoveParam ≤ 0.15` ∧ `finalAgentSpeed ≤ 0.15` ∧ `grounded` | `arrived` ∧ `peakAgentSpeed ≥ 2.0` ∧ `|peakSpeedZ| ≥ 2.0` ∧ `stateDuringMove == "Locomotion"` ∧ `|finalSpeedZ| ≤ 0.15` ∧ `finalAgentSpeed ≤ 0.15` ∧ `grounded` |
| 控制器结构条件 | `stateNameExact` ∧ `blendTreeIs2D`（`blendType != Simple1D`）∧ `blendParameterY == "SpeedZ"` ∧ `layer0Only` | `stateNameExact` ∧ `blend1D`（`blendType == Simple1D`）∧ `blendParameter == "SpeedZ"` ∧ `thresholdsOk`（2 子节点，阈值 0 / 4）∧ `layer0Only` |
| 夹具 | 生产玩家 + `SelectionManager.SingleSelect → CommandMove` | 同一生产入口 |
| 覆盖结论 | — | **完全覆盖（新口径超集）** |

说明：FND02B 版断言的 `blendTreeIs2D` 是 SPEC-01 之前的**旧契约**（2D 混合树、`SpeedZ` 作 Y 轴）；现控制器已改为 1D 单参数 `SpeedZ`、阈值 0/4。因此该条的红是**期望过期**，不是覆盖缺口。SPEC01 在玩家行为维度等价、在控制器结构维度更严（多出 `blendType` 与 `thresholds` 双向精确校验）。

### 1.2 `Attack.DualPathUnified`

| 项 | FND02B | SPEC01 |
|---|---|---|
| 判定字段 | `designatedPass`、`autoPass` | `designatedPass`、`autoPass`、`swordFieldsOk`、`rangedTriggerOk` |
| 指定路径（`CommandAttack`） | 命中/发射瞬间 `ev.state == "Attack_Release"`（近战同样期望 `Attack_Release`） | 近战期望 `AttackMelee`、远程期望 `Attack_Release` |
| 自动路径（`Aggro`） | 命中/发射瞬间 `ev.state == "Attack_Release"` | 同上（按武器分支） |
| 序列化字段核验 | 无 | `MeleeCombat.attackTrigger == "AttackMelee"` ∧ `MeleeCombat.hitStateName == "AttackMelee"`；`RangedCombat.attackTrigger == "AttackRange"` |
| 夹具 | `FND02B_Target_Combat`（内存 `UnitHealth`，高 HP，无 AI） | `SPEC01_Target_Combat`（同构） |
| 覆盖结论 | — | **完全覆盖（严格超集）** |

说明：SPEC01 保留 FND02B 的两条路径判定，并把近战期望改为单段 `AttackMelee`，额外核验两个序列化触发器字段。FND02B 版无任何 SPEC01 缺失的判定。

### 1.3 `Clock.MeleeNormalizedWindow`

| 项 | FND02B | SPEC01 |
|---|---|---|
| 判定字段 | `countOk`、`speedOk` | `countOk`、`speedOk`、`fieldOk` |
| 计数条件 | `hitEvents.Count ≥ 2` ∧ `inWindow == hitEvents.Count` ∧ `|hitEvents.Count − releaseEntries| ≤ 1` | `hitEvents.Count ≥ 2` ∧ `inWindow == hitEvents.Count` ∧ `|hitEvents.Count − meleeEntries| ≤ 1` |
| 状态/窗口 | `state == "Attack_Release"` ∧ `normalizedTime ∈ [activeStart−0.06, activeEnd+0.06]` | `state == "AttackMelee"` ∧ 同一窗口容差 |
| 攻速比对 | `speedOk = |Animator.AttackSpeed − stats.attackSpeed| < 0.01` | 同一逻辑 |
| 字段核验 | 无 | `fieldOk = attackTrigger == "AttackMelee"` ∧ `hitStateName == "AttackMelee"` |
| 覆盖结论 | — | **完全覆盖（超集）** |

说明：`releaseEntries`（进入 `Attack_Release` 次数）在单段口径下由 `meleeEntries`（进入 `AttackMelee` 次数）承接；`speedOk` 逐字相同，即「`AttackSpeed` 参数比对」被完整保留。SPEC01 额外多 `fieldOk`。

---

## 2. 同套件其余 3 条（step2 绿项，整体退役需一并核）

| # | 断言 | 方法体比对结果 | 覆盖结论 |
|---|---|---|---|
| 2.1 | `Regression.FND02A` | 与 SPEC01 同名方法逐行等价，仅夹具命名字面量 `FND02B_Target_Combat` → `SPEC01_Target_Combat` | 完全覆盖 |
| 2.2 | `Animation.PostCombatAndHit` | 与 SPEC01 同名方法逐行等价 | 完全覆盖 |
| 2.3 | `Clock.RangedOneShotPerCycle` | 与 SPEC01 同名方法逐行等价，仅局部变量名 `rangedRows` → `rangedCycleRows` | 完全覆盖 |

---

## 3. 结论

| 结论项 | 值 |
|---|---|
| 独有覆盖条数 | **0** |
| 部分覆盖条数 | **0** |
| 完全覆盖条数 | **6 / 6**（3 红 + 3 绿） |
| 保留决策 | **不保留任何断言**；按 D1 整体退役 `Assets/_Project/Tests/FND02B/` |

理由：3 条红项均为「旧口径期望过期」，其判定维度已被 SPEC01 同名断言以更严格的新口径完整承接；3 条绿项与 SPEC01 同名方法等价。退役后套件形态为：**FND02B 目录整体消失，无残留骨架**；原 6 条断言的职责全部由 `SPEC01` 的 8 条承接（SPEC01 另有 `UI.DamageNumberScale`、`UI.SpawnAnchor` 两条，FND02B 已在上一单删除对应过时断言）。

覆盖对照的原始依据（只读命令）：

```powershell
# 方法体逐行比对（示例）
git diff --no-index --no-color -U0 A_reg.cs B_reg.cs
Compare-Object $fnd02b[490..786] $spec01[514..839]
```
