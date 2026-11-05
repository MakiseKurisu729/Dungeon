# REVIEW — 通用动画机结构评审（PlayerBase + Override）

> 范围：`Assets/_Project/Art/Animations/PlayerBase.controller`、`OC_Knight/Archer/Mage.overrideController`、代码/测试中的动画字符串引用。
> 性质：**只读评审**，不改资产；修改清单见 §6，需所有者批准。
> 基线：`a7817b55`（2026-11-05）。

## 1. 现状结构（实测）

**状态（10 个，单层 Base Layer）**

| 状态 | 速度参数 | 出口 |
|---|---|---|
| `Locomotion`（默认） | 无（BlendTree 自己处理） | 6 条 |
| `AttackMelee` | `AttackSpeed` 生效 | → Locomotion |
| `Attack_Draw` | `AttackSpeed` 生效 | → `Attack_Release` |
| `Attack_Release` | `AttackSpeed` 生效 | → Locomotion |
| `Bash` | **未生效**（`SpeedParameterActive=0`） | → Locomotion |
| `Skill` | **未生效**（`=0`，参数指向 `AttackSpeed`） | → Locomotion |
| `Hit_A` | 未生效 | → Locomotion |
| `Downed` | — | → `Rise`（Revive） |
| `Rise` | — | → Locomotion |
| `Interact` | — | → Locomotion |

**参数**：`SpeedZ`、`SpeedX`、`Grounded`、`AttackSpeed`、`AttackRange`、`Skill`、`Bash`、`Interrupt`、`Die`、`Revive`、`Hit`。

**过渡（实测）**

| 源 | 条件 | 目标 | HasExitTime / ExitTime | Interruption | CanSelf |
|---|---|---|---|---|---|
| Locomotion | `AttackRange` | Attack_Draw | 0 / .732 | None | 1 |
| Locomotion | `AttackMelee` | AttackMelee | 0 / .732 | None | 1 |
| Locomotion | `Bash` | Bash | **1 / .732** | None | 1 |
| Locomotion | `Skill` | Skill | 0 / .732 | None | 1 |
| Locomotion | `Hit` | Hit_A | 0 / .732 | None | 1 |
| Locomotion | `Interact` | Interact | 0 / .732 | None | 1 |
| Attack_Draw | — | Attack_Release | 1 / .8125 | None | 1 |
| Attack_Release | — | Locomotion | 1 / .8125 | None | 1 |
| AttackMelee | — | Locomotion | 1 / .75 | None | 1 |
| Bash | — | Locomotion | 1 / .7656 | None | 1 |
| Skill | — | Locomotion | 1 / .8125 | None | 1 |
| Hit_A | — | Locomotion | 1 / .625 | None | 1 |
| Interact | — | Locomotion | 1 / .8077 | None | 1 |
| Downed | `Revive` | Rise | 0 / .6875 | None | 0 |
| Rise | — | Locomotion | 1 / .8929 | None | 1 |
| **AnyState** | `Die` | Downed | 0 / .75 | None | 0 |

> `InterruptionSource` 全部为 `None`；`AnyState` 只有 `Die` 一条。

## 2. 总体判断

**能跑 MVP，但不是“合理且边界闭合”的通用动画机。**

- 合理之处：单层 + Override 的方向正确；受击/死亡/起身/交互都有独立状态；攻击/技能的出口都回 `Locomotion`，不会永久卡死。
- 不合理之处：**技能只有 2 个共享状态（Bash/Skill），却要服务 4 个槽**；**所有打断策略都是 None，AnyState 只有 Die**；**受击只能从 Locomotion 进入**；**命中时序一半用绝对秒、一半用归一化**；**技能速度参数被关掉**。这些不是“调参能解决”的，是结构问题。

## 3. 问题清单（按严重度）

### P0 — 会直接违反可玩性/边界

**P0-1 技能不能打断普攻**
- 证据：`Bash`/`Skill` 只有 `Locomotion → X` 一条入口；攻击状态没有到技能的过渡；所有过渡 `InterruptionSource=None`。
- 后果：普攻中按 Q/W，技能效果结算但动画不切（所有者 2026-11-05 实测复现）。
- 目标：`AnyState → Q/W/E/R`，`HasExitTime=0`、`InterruptionSource=Source`、`OrderedInterruption=1`、`CanSelf=0`。

**P0-2 受击不能打断攻击/技能**
- 证据：`Hit` 只有 `Locomotion → Hit_A`；`AnyState` 只有 `Die`。
- 后果：攻击/技能中被打，`Hit_A` 不播；受击反馈只在待机/移动出现。这与“受击应能打断”的常见预期不符，也影响打击感。
- 目标：`AnyState → Hit_A`（`Hit`），优先级 **Die > Hit > 技能**；或者明确“技能霸体”策略并写进 ADR。

**P0-3 技能效果与动画时序脱节**
- 证据：`BashEffect` 用绝对 `hitTime` 等待；`MeleeCombat` 用 `normalizedTime` 窗口；技能动画因 P0-1 不切时，伤害仍按旧时钟结算。
- 后果：动画和伤害不同步；换角色 Clip 长度后必须重标秒数。
- 目标：所有技能按当前状态的 `normalizedTime` 比例结算。

### P1 — 扩展性/一致性

**P1-1 两个技能状态服务四个槽**
- 证据：控制器只有 `Bash`/`Skill`；`SkillSystem.skills` 有 4 槽；`BashEffect` 硬编码 `"Bash"`，`FireballEffect` 硬编码 `"Skill"`。
- 后果：Q/W/E/R 无法各自换动作；覆盖器没有“槽”概念；运行时换技能不可行。

**P1-2 没有子状态机，多段技能无法表达**
- 证据：`Attack_Draw/Release` 是两个平级状态；技能各 1 个状态。
- 后果：拉弓→放箭、蓄力→释放、连段只能继续加平级状态和过渡，状态图会膨胀。

**P1-3 命中时序口径不统一**
- 证据：`BashEffect.cs` 用 `hitTime` 秒；`MeleeCombat.ReadAnimProgress` 用 `normalizedTime`；`RangedCombat` 用释放窗口。
- 后果：同一套技能系统里三种时钟；换 Clip/换角色容易错位。

**P1-4 技能速度参数失效**
- 证据：`Bash`/`Skill` 的 `SpeedParameterActive=0`；`Skill` 的 `SpeedParameter` 指向 `AttackSpeed` 但未启用；攻击状态是启用的。
- 后果：技能动画不受攻速影响；如果以后加“释放速度”，现在没有承载点。

**P1-5 `Bash` 的 `HasExitTime=1` 与其他技能不一致**
- 证据：`Bash` 1 / .732；`Skill` 0 / .732；`AttackMelee` 0 / .732。
- 后果：Q 有最多 73% 循环的延迟，W 没有；手感不一致，也让“按了没反应”更难排查。

**P1-6 没有明确的打断优先级**
- 证据：所有过渡 `InterruptionSource=None`；AnyState 只有 Die。
- 后果：受击/死亡/技能/攻击同时发生时，谁能打断谁没有文档，行为靠偶然的过渡顺序。

### P2 — 可维护性/契约

**P2-1 `CanTransitionToSelf=1` 大面积存在**
- 证据：除 Downed→Rise、AnyState→Downed 外，几乎都是 1。
- 风险：同类状态重复进入/自过渡；虽然当前源/目标不同不一定触发，但属于该关掉的配置。

**P2-2 动画字符串散落在代码/测试**
- 证据：`BashEffect`（"Bash"）、`FireballEffect`（"Skill"）、`MeleeCombat`（"Skill"）、`FND00B06` 的已知状态表、`SPEC01` 的 CastSmoke。
- 风险：改状态名必须全仓搜字符串；漏一处就静默失效。

**P2-3 近战/远程结构不对称（已定案，不是遗留问题）**
- 证据：近战 `AttackMelee` 单状态；远程 `Attack_Draw → Attack_Release` 两状态。
- 定案：远程两状态收进 `AttackRange` 子状态机；后续技能同样用子状态机装多段。此项在“结构改造阶段”执行，不再作为待决问题。

**P2-4 命名/职责不统一**
- 证据：`Bash`/`Skill` 是“具体技能”，`AttackMelee`/`AttackRange` 是“攻击类型”，`Hit_A`/`Downed`/`Rise` 是“受击流程”。
- 风险：状态名不能表达“槽位/段”，覆盖器和代码都难对齐。

**P2-5 施法者死亡/倒地的协程未收口**
- 证据：`BashEffect.BashRoutine` 等待 hitTime/totalTime，没有检查 `caster` 是否死亡/倒地；`FireballEffect` 类似。
- 风险：死亡后仍可能结算伤害/生成火球。

**P2-6 覆盖器缺 Clip 时回退到基础占位**
- 证据：`AnimatorOverrideController` 未覆盖的 Clip 会用基础控制器里的占位 Clip。
- 风险：角色会“借用”别人的动作，且没有日志。

## 4. 边界矩阵（现状 vs 目标）

| 场景 | 现状 | 目标 |
|---|---|---|
| 普攻中按技能 | ❌ 动画不切，效果照算 | ✅ 立即切槽位动画，普攻计时取消 |
| 技能中被打 | ❌ Hit_A 不播 | ✅ Hit 打断（或明确霸体，写 ADR） |
| 技能中死亡 | ✅ AnyState Die 生效 | ✅ Die 优先；技能协程停止 |
| 倒地中按技能 | ❌ 无入口 | ❌ 明确禁止（或复活后） |
| 交互中按技能 | ❌ 无入口 | 明确策略（建议禁止） |
| 远程普攻中按技能 | ❌ 无入口 | ✅ 同近战 |
| 多角色 | 共享控制器 + 覆盖器 | 槽位级覆盖，缺 Clip 有日志/回退 |
| 换技能 | 无 | 槽位 → Clip 组，运行时替换 |
| 暂停/TimeScale | 未验证 | 技能协程/动画一致 |
| 根运动/Culling | 未验证 | 统一关闭 Root Motion、Always Animate |

## 5. 目标结构（建议）

```
Base Layer
├─ Locomotion (default, BlendTree)
├─ AttackMelee            (近战普攻，单状态)
├─ AttackRange (子状态机)  (远程普攻：Draw → Release)
├─ Q (子状态机)            Q_1 [→ Q_2 ...]
├─ W (子状态机)            W_1 [→ W_2 ...]
├─ E (子状态机)            E_1 ...
├─ R (子状态机)            R_1 ...
├─ Hit_A
├─ Downed → Rise
└─ Interact

AnyState 优先级（从上到下）：
  Die    → Downed      (HasExitTime=0, CanSelf=0)
  Hit    → Hit_A       (HasExitTime=0, InterruptionSource=Source, CanSelf=0)
  Q/W/E/R→ 槽子状态机   (HasExitTime=0, InterruptionSource=Source, CanSelf=0)
```

- 参数：`Q/W/E/R`、`Hit`、`Die`、`Revive`、`AttackSpeed`、`SkillSpeed`（新增）、`Grounded`、`SpeedX`、`SpeedZ`。
- 命中：统一 `normalizedTime` 比例；技能效果从 `SkillCastContext` 取 `stateName`，不再硬编码。
- 覆盖器：按槽位映射；缺 Clip 时记录日志并回退到基础 Clip。
- 旧 `Bash`/`Skill`：迁移完成后删除。

## 6. 修改清单（需所有者批准）

### 6.1 资产（所有者）

1. `PlayerBase.controller`
   - 新增 `Q/W/E/R` Trigger、四个子状态机（本轮各 1 段）。
   - `AnyState → Q/W/E/R`：无 Exit Time、Interruption Source=Source、Ordered、CanSelf=0。
   - `AnyState → Hit_A`（Hit）：同上；放在 Die 之后、技能之前。
   - `AttackRange` 子状态机包住 `Attack_Draw/Release`。
   - 技能状态启用 `SpeedParameterActive`，参数用新增 `SkillSpeed`（默认 1）；或明确技能不吃攻速。
   - 删除 `Bash`/`Skill`（迁移完成后）。
2. `OC_Knight/Archer/Mage`
   - 按 Q/W 槽映射 Clip；多段时加 `Q_2` 等。
   - 缺 Clip 时留空并在报告登记。

### 6.2 代码（执行者）

1. `SkillSystem`：构造 `SkillCastContext { slot, trigger, stateName, target }`，传给效果。
2. `SkillEffectBase/SkillEffects/SkillEffectFireball`：改用 context；去掉 `"Bash"`/`"Skill"` 硬编码；命中改归一化。
3. `MeleeCombat`：`DoSkillAttack(trigger, stateName)`；`ReadAnimProgress` 读本次技能状态名；`CancelAttack` 重置本次触发器。
4. `BashEffect`/`FireballEffect`：施法者死亡/倒地时停止协程。
5. `FND00B06`/`SPEC01`/`DIAG`：状态名改 `Q_1/W_1`，新增 `Skill.InterruptAttack`、`Hit.InterruptSkill` 断言。
6. 新增 `AnimatorParams` 常量类（集中字符串），避免散落。

### 6.3 测试/文档

- `Skill.InterruptAttack`：普攻中 Q，0.2s 内进 `Q_1`，普攻计时取消。
- `Hit.InterruptSkill`：技能中受击，进 `Hit_A`（若采用霸体则改为“不打断”断言）。
- `Skill.CastSmoke`：Q/W 状态名/伤害/蓝/CD。
- `Animation.RangedSubState`：`AttackRange` 子状态机后远程单周期仍只发 1 次。
- 更新 ADR-0003 或新增 ADR-0004：受击/技能打断优先级、技能速度参数。

## 7. 结论

- **现在这套结构适合“两个技能 + 单角色”的 MVP，不适合“四槽 + 多段 + 多角色覆盖 + 换技能”的目标。**
- 最该先修的不是状态图美观，而是 **P0 的三条边界**：技能打断普攻、受击打断、命中时序统一。
- 建议顺序：A+B（QWER 子状态机 + AnyState）→ 受击 AnyState + 归一化 → `AttackRange` 子状态机 → 清理旧状态/字符串。

## 8. 所有者已定案（2026-11-05）

1. **受击打断技能**：**打断**。`AnyState → Hit_A`，优先级 **Die > Hit > 技能**。
2. **技能速度**：**新增 `SkillSpeed`**；Q/W/E/R 状态启用 `SpeedParameterActive`，用 `SkillSpeed`。
3. **交互中放技能**：**允许**；放技能先打断当前交互/移动，再播技能。
4. **`AttackRange` 合并**：**结构改造阶段做**，不在 A+B 这一步。
5. 近战单状态 / 远程双状态不是新问题；远程按 `AttackRange` 子状态机收口，技能按槽位子状态机装多段。
