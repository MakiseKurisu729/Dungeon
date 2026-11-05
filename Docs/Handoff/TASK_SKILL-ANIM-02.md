# SKILL-ANIM-02 — Q/W/E/R 技能槽子状态机与攻击打断

## §0 元信息

| 字段 | 值 |
|---|---|
| 任务 ID | `SKILL-ANIM-02` |
| 风险等级 | `高风险`（Animator Controller / OverrideController / 技能资产 / 公共动画契约） |
| 授权 | 所有者 2026-11-05 定案：Q/W/E/R 子状态机、命中归一化、技能可打断普攻；ADR-0003 已记录。本 spec 待所有者审 |
| 基线提交 | `a7817b55` |
| 回滚锚 | 资产快照（`PlayerBase.controller` + 3 个 `OC_*.overrideController`）+ 代码提交 |
| 分支 | 当前分支（资产由所有者改） |
| 状态 | `Ready`（待所有者审 spec + 改资产） |

## §1 需求（Why / What）

**一句话目标**：把技能动画从“`Bash`/`Skill` 两个共享状态”改成“Q/W/E/R 四个子状态机”，技能可在普攻中**立即打断**并播放自己的动画；远程普攻收进 `AttackRange` 子状态机。

**用户故事**：作为玩家，我在普攻中按 Q/W 应立刻看到技能动作，而不是继续挥普攻；作为所有者，我要能在覆盖器里按槽位替换不同角色的技能动作，而不改通用控制器。

**必须满足**

| 编号 | 需求 | 验收方式 |
|---|---|---|
| R-1 | 控制器有 `Q/W/E/R` 四个触发器与四个子状态机；每个子状态机至少 1 段（`Q_1` 等） | 断言 + 目检 |
| R-2 | `AnyState → Q/W/E/R`：`Has Exit Time=0`、`Interruption Source=Current State`、可打断普攻 | 断言 |
| R-3 | 技能从任意状态（含 `AttackMelee`）进入对应槽位动画，≤0.2s | `Skill.InterruptAttack` |
| R-4 | 技能效果仍正常结算（伤害/耗蓝/冷却/命中帧） | `Skill.CastSmoke` |
| R-5 | 远程普攻收进 `AttackRange` 子状态机（`Attack_Draw → Attack_Release` 顺序不变） | `Clock.RangedOneShotPerCycle` |
| R-6 | 命中帧归一化：效果按当前状态的 normalizedTime 对齐，不依赖绝对秒 | 断言 + 资产时序 |
| R-7 | 覆盖器按槽位映射角色动作；Knight/Archer/Mage 的 Q/W 可使用不同 Clip | 人工 + 运行时 Clip 记录 |
| R-8 | 旧 `Bash`/`Skill` 状态与触发器在代码/测试全绿后删除 | 静态检查 |
| R-9 | 不改伤害公式、技能数值、场景/预制体 | `git status` |

**不在范围**：运行时换技能 UI（后续）、多段技能的完整内容、Archer/Mage 技能数值、新角色。

**约束**：ADR-0003；单层 Animator + Override；`UnitStats.attackSpeed` 仍为攻速单一来源；`AnyState` 仅用于 Q/W/E/R（受击/死亡保持现状）；资产由所有者改。

## §2 方案（Plan）

**备选方案**

| 方案 | 做法 | 优点 | 缺点 | 结论 |
|---|---|---|---|---|
| A | 每个槽一个子状态机 + AnyState 无 Exit Time 打断；命中用 normalizedTime | 可扩展多段；覆盖器按槽换 Clip；解决攻击中不切动画 | 控制器/覆盖器要重做，测试要同步 | **选定** |
| B | 保留 `Bash/Skill`，只给它们加 AnyState 过渡 | 改动最小 | 仍然只有两个槽，无法按 Q/W/E/R 换动作；不满足扩展性 | 弃用 |
| C | 把前后摇烘成一条 Clip，一槽一状态 | 状态图最简单 | 接缝要交叉淡化，资产返工大；多段不灵活 | 弃用 |

**选定方案与理由**：A 同时满足“多段扩展”“按槽覆盖”“攻击中立即打断”，代价是一次性重做控制器与测试。

**分阶段（关键）**

- **Phase A（所有者改资产，代码不动）**
  1. `PlayerBase.controller`：新增 Trigger `Q/W/E/R`；新增四个子状态机 `Q/W/E/R`，各放一个状态 `Q_1/W_1/E_1/R_1`（先用占位 Clip，例如旧的 `Bash`/`Skill` Clip）。
  2. `AnyState → 每个子状态机`：`Has Exit Time = 否`、`Interruption Source = Current State`、`Ordered Interruption = 是`、`Can Transition To Self = 否`、过渡 0.05。
  3. 每个 `*_1 → Locomotion`：`Has Exit Time = 是`、`Exit Time = 1`、过渡 0.1。
  4. 新建子状态机 `AttackRange`，把 `Attack_Draw`、`Attack_Release` 移进去（入口 `Attack_Draw`，内部 Draw→Release 不变；外层从 `Locomotion → AttackRange` 的过渡条件保持原样）。
  5. 暂时**保留** `Bash`/`Skill` 状态与触发器（Phase C 再删），保证代码切换前测试仍绿。
  6. 三个覆盖器：`OC_Knight` 把 `Q_1/W_1` 映射到盾击/跳劈；`OC_Archer`、`OC_Mage` 各映射自己的 Q/W 动作（单段；多段留到后续）。
- **Phase B（执行者改代码 + 测试）**
  - `SkillSystem` 构造 `SkillCastContext { slot, trigger, stateName, target, caster }`，传给效果。
  - `SkillEffectBase/CanCast/Execute` 改为接收 context；三个效果改用 `ctx.trigger`/`ctx.stateName`。
  - `BashEffect`：`SetTrigger(ctx.trigger)`；命中帧 = `hitTime/totalTime` 比例 × 当前状态长度（不再用绝对秒）。
  - `HeavySlashEffect`：`caster.sword.DoSkillAttack(ctx.trigger, ctx.stateName)`。
  - `FireballEffect`：改用 `ctx.trigger`。
  - `MeleeCombat`：`DoSkillAttack(trigger, stateName)`；`ReadAnimProgress` 读本次技能状态名；`CancelAttack` 重置本次技能触发器。
  - 测试：`Skill.CastSmoke` 状态名改 `Q_1/W_1`；新增 `Skill.InterruptAttack`；`FND00B06` 已知状态表加 Q/W/E/R；`DIAG` 探针同步（若运行）。
- **Phase C（所有者清理）**
  - 删除 `Bash`/`Skill` 状态与参数；重跑 SPEC01 + 人工 4 键。

**数据流**：

```
按 Q → SkillInputController（瞄准/点选）
→ SkillSystem.Cast(slot, target)
→ SkillCastContext{slot=0, trigger="Q", stateName="Q_1"}
→ BashEffect.Execute：CancelAttack → Animator.SetTrigger("Q")
→ Controller：AnyState → Q 子状态机 → Q_1（无 Exit Time，立即切）
→ 命中：BashEffect 按 Q_1.normalizedTime 到 hitTime/totalTime 比例时结算
→ Q_1 播完 → Locomotion
```

**影响面清单（高风险：完整）**

| 项 | 内容 |
|---|---|
| 资产（所有者） | `PlayerBase.controller`；`OC_Knight/Archer/Mage.overrideController`；必要时 `Q_Bash_Effect.asset`（只调时序比例，不加字段） |
| 代码（执行者） | `SkillEffectBase.cs`、`SkillEffects.cs`、`SkillEffectFireball.cs`、`SkillSystem.cs`、`MeleeCombat.cs`、`SPEC01_ProbeRunner.cs`、`FND00B06_ProbeRunner.cs` |
| 字符串引用 | Trigger `Q/W/E/R`、状态 `Q_1/W_1/E_1/R_1`、旧 `Bash/Skill` 的所有引用（代码/测试/DIAG） |
| 场景/预制体 | 不新增；覆盖器引用由 Unity GUID 保持 |
| `.meta` | 不移动/重命名脚本；仅资产内部修改 |
| 测试 | `Skill.CastSmoke` 状态名、`Skill.InterruptAttack` 新增、FND00B06 状态表 |

**实施顺序**：Phase A → 编译 → Phase B（red：测试先改；green）→ SPEC01 ×3 → Phase C → 再跑 ×1 → 人工 4 键。

**风险与回滚**：AnyState 过渡若配置错会循环/卡死；用 `Can Transition To Self=否` + 单段回 Locomotion 兜底。命中比例沿用 `hitTime/totalTime`，若某角色 Clip 长度差异大，需调该技能资产比例。回滚 = 还原 4 个资产 + 代码提交。

## §3 验证（Validation）

| AC | Given / When / Then | 断言 ID | 证据 |
|---|---|---|---|
| AC-1 | 给定普攻进行中，当 `Cast(0, enemy)`，则 0.2s 内 Animator 进入 `Q_1`，普攻计时被取消，伤害在归一化命中帧结算 | `Skill.InterruptAttack` | `Docs/QA/Evidence/SKILL-ANIM-02/` |
| AC-2 | 给定 `Cast(0, enemy)`/`Cast(1, null)`，则蓝量/CD/伤害/效果类型与状态 `Q_1/W_1` 均正确；失败施放不扣蓝 | `Skill.CastSmoke` | 同上 |
| AC-3 | 给定远程普攻，则 `AttackRange` 子状态机内 `Attack_Draw → Attack_Release`、单周期发射 1 次，原有窗口不回归 | `Clock.RangedOneShotPerCycle` | 同上 |
| AC-4 | 给定不同长度的 Q/W Clip，则命中发生在 `hitTime/totalTime` 比例处（不再固定绝对秒） | `Skill.CastSmoke` 数据 + 目检 | 同上 |
| AC-5 | 给定三个角色的覆盖器，则 Q/W 槽使用各自 Clip（运行时记录 Clip 名，允许人工确认） | 人工 + 记录 | 目检记录 |
| AC-6 | 给定 SPEC01 连续 3 次，则 13/13 pass、fail=0、exit=0；FND00B06 无新增红 | `all` | 同上 |
| AC-7 | 给定最终 diff，则只改批准资产/代码；旧 `Bash/Skill` 字符串清零 | 静态 | `git status`、grep |

**运行次数**：SPEC01 连续 **3 次**；FND00B06 至少 1 次；人工 3 项（四键、攻击打断、远程普攻）。

**必须保存的证据**：3 次 SPEC01 日志/JSONL/Summary/ExitCode；FND00B06 一次；`git status`；控制器/覆盖器 diff 清单（所有者提供或截图）。

## §4 执行信息

| 项 | 内容 |
|---|---|
| 前置 | Phase A 资产已由所有者完成并保存；Unity 关闭（进程 0 + 无锁文件）；工作区仅剩两个 `.csproj` |
| 允许改动 | 代码 6 个文件 + 测试 2 个文件 + `Docs/QA/Evidence/SKILL-ANIM-02/` + `REPORT_SKILL-ANIM-02.md` + `Docs/Learning/SKILL-ANIM-02_讲稿.md`；**资产只读** |
| 允许读取 | 本工单 + 上述代码/测试 + 控制器/覆盖器（只读） |
| 绝不触碰 | `PlayerBase.controller`、`OC_*.overrideController`、场景、预制体、`.asset`、`ProjectSettings`、`Packages`、`CURRENT.md`、两个 `.csproj` |
| 证据目录 | `Docs/QA/Evidence/SKILL-ANIM-02/`；canonical `SPEC-01/` 用 tag 追加 |
| 熔断 | 需要改资产、SPEC01 连续 3 次仍红、编译错连续 2 轮、AnyState 循环/卡死、Unity 未关 |
| 执行预算 | Phase B 实现 1 轮 + 修复 1 轮；只跑 SPEC01/FND00B06 |

## §5 学习产出

| 项 | 要求 |
|---|---|
| 讲稿 | **必须**：`Docs/Learning/SKILL-ANIM-02_讲稿.md`；AI 写完整讲解（所有者阅读，可选批注） |
| 必含 | 为什么一个 State 装不下两条 Clip；子状态机/AnyState/Interruption Source 各是什么；命中归一化公式；覆盖器按槽替换的原理；多段技能怎么加 |
| 三句话版本 | 写进 REPORT §3 |

## 自包含声明

本工单不依赖 `CURRENT.md` 或其他任务书即可执行；Phase A 资产步骤必须由所有者完成，执行者不得代改资产。
