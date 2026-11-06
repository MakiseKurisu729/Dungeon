# REFACTOR-M4 — 技能与战斗收口（SkillCastContext + 瞄准点）

> 长任务 `REFACTOR-LONG-01` 的 M4 小节。上游依据：`C:\Users\lll\.opencode\plan\refactor-plan.md` §2/§9-M4。
> 本单自包含；执行者只读本文件 + 仓库现状即可开工。

## §0 元信息

| 字段 | 值 |
|---|---|
| 任务 ID | `REFACTOR-M4` |
| 风险等级 | `常规`（纯代码；**不含**命中体积物理化、不动控制器/预制体/场景/`.asset`） |
| 授权 | 所有者 2026-11-05 批准长任务计划书；本里程碑在预授权范围内 |
| 基线提交 | `d02470c4`（M3 收口；开工时以 `git rev-parse --short HEAD` 记录实际值） |
| 回滚锚 | `d02470c4`；回滚 = `git revert <M4 commit>` / `git reset --hard d02470c4`（仅所有者可执行） |
| Unity 模式 | **模式 B（编辑器常开 + MCP）**，Unity 2022.3.62f3c1 |
| 状态 | `Ready` |

> 执行者**不得**标 Done、不得改 `CURRENT.md`、**不得**做任何 git 写操作（提交由大脑/所有者做）。

## §1 需求（Why / What）

**一句话目标**：把「一次技能施放」的上下文（槽位/动画触发器/状态名/目标/施法者）收敛为 `SkillCastContext` 传给效果，让效果不再自己拼动画与目标信息；同时把子弹瞄准点从写死的 `+0.6m` 改成目标渲染包围盒中心。

**用户故事**：作为重构负责人，我要新增一个技能效果时只关心「这招做什么」，不必再猜触发器名、也不必自己算瞄准高度。

**必须满足**

| 编号 | 需求 | 验收方式 |
|---|---|---|
| R-1 | 新增 `Scripts/Combat/SkillCastContext.cs`（契约见 §2）；`SkillEffectBase` 的 `CanCast/Execute` 改为接收 context | 代码审查 + 编译 |
| R-2 | `BashEffect` / `FireballEffect` 改签名，触发器/状态名由 context 提供（不得改任何 `.asset` 数值） | 代码审查 + SPEC01 技能断言 |
| R-3 | `UnitStats` 新增 `skillSpeed = 1f`（序列化兼容，默认 1 = 行为不变），效果时长按 `1f / max(ε, skillSpeed)` 缩放 | 代码审查 + REFACTOR 14/14 |
| R-4 | `Projectile.ComputeEnd` 改为瞄准目标 `Renderer.bounds` 合并包围盒中心，目标缺失/已死时仍用 `_fallbackEnd` | 新增断言 `Projectile.AimPoint` |
| R-5 | **行为不变**：技能命中、伤害、冷却、蓝耗、动画触发时序、打断关系与 M3 完全一致 | REFACTOR 15/15 + SPEC01 12/12 |
| R-6 | 现有断言口径零改动（允许新增，11 → 15）；测试里对现有产品 API 的引用不得删改 | diff 审查 |

**不在范围（M4 明确不含）**

- `HitProxy` 物理胶囊、任何命中体积物理化。
- 任何预制体/场景/Animator Controller/`.asset`/`ProjectSettings` 改动。
- 技能数值平衡、新技能、多段技能、E/R 空槽补内容。
- 不改 `MeleeCombat` 的攻击窗口/伤害公式（只允许为「技能打断普攻」的接线做最小改动，见 §2）。
- 不改输入层（M3 已收口）。

**约束**

- Animator 触发器名（`Skill`/`Bash`/`Interact`/`Interrupt`/`Hit`/`Die`/`Revive`）**不得改**；状态名同。
- `SkillSystem.Cast(int index, Transform target)` **签名不得改**（测试与 `SkillInputController` 依赖它）。
- `SkillEffectBase` 的序列化字段 `needTarget` / `range` 与各效果的字段名**一个都不能改**（`.asset` 里存着这些值）。

## §2 方案（Plan）

**备选方案**

| 方案 | 做法 | 优点 | 缺点 | 结论 |
|---|---|---|---|---|
| A | 每个效果自己从 `SkillData` 读触发器名（改数据资产） | 数据驱动最彻底 | 必须改 `.asset`（禁止） | 弃用 |
| B | 新增 context 值对象，触发器/状态名由效果以 virtual 属性声明，`SkillSystem` 组包后传入 | 零资产改动、加效果不必改系统、可断言 | 效果多两个虚属性 | **选定** |
| C | 只把方法参数从 `(SkillSystem, Transform)` 换成 `(SkillSystem, Transform, int slot)` | 改动最小 | 没有解决触发器/状态名散落的问题 | 弃用 |

**选定方案与理由**：B —— 只动代码就能把「这次施放的完整上下文」传到位，且不碰任何资产。

**契约（必须照此实现；命名可微调，语义与可见性不得改）**

```csharp
// Scripts/Combat/SkillCastContext.cs
public readonly struct SkillCastContext
{
    public readonly int Slot;              // 0..3 = Q/W/E/R
    public readonly string Trigger;        // Animator 触发器（"Skill" / "Bash"）
    public readonly string StateName;      // 对应动画状态名（断言/等待用）
    public readonly Transform Target;      // 目标（可能为 null）
    public readonly SkillSystem Caster;    // 施法系统（原 CanCast/Execute 的第一个参数）
    public readonly UnitHealth CasterUnit; // 施法者单位（可能为 null）
    public readonly float Speed;           // = max(0.01f, stats.skillSpeed)，默认 1

    public SkillCastContext(int slot, string trigger, string stateName, Transform target,
                            SkillSystem caster, UnitHealth casterUnit, float speed);
    public SkillCastContext WithTarget(Transform target);   // 便捷：只换目标
}

// Scripts/Combat/SkillEffectBase.cs（改）
public abstract bool CanCast(SkillCastContext ctx);
public abstract void Execute(SkillCastContext ctx);
public virtual string Trigger { get { return "Skill"; } }
public virtual string StateName { get { return "Skill"; } }
```

- `BashEffect`：`Trigger = "Bash"`、`StateName = "Bash"`；`FireballEffect`：用基类默认 `"Skill"`。
- `SkillSystem.Cast(index, target)`：组包 `new SkillCastContext(index, eff.Trigger, eff.StateName, target, this, GetComponent<UnitHealth>(), Speed)` 后先 `CanCast` 全部、再扣蓝、再 `Execute` 全部（**顺序与失败语义逐行不变**）。`Speed` 从 `sword != null && sword.stats != null ? sword.stats.skillSpeed : 1f` 取，`<=0` 时按 1。
- 效果内部时长：`waitSeconds / ctx.Speed`（`BashEffect.hitTime/totalTime`、`FireballEffect.castDelay/flightDuration`）；`Speed==1` 时与原值完全相同。**动画 `SetTrigger` 的时机不变**（仍在前摇开始处）。
- `Projectile.ComputeEnd`：
  ```csharp
  protected virtual Vector3 ComputeEnd()
  {
      if (target != null && !target.dead) return AimPoint(target);
      return _fallbackEnd;
  }
  static Vector3 AimPoint(UnitHealth t)   // 合并所有子 Renderer 的 bounds → center
  ```
  - 无 `Renderer` 时回退 `target.transform.position`（**不得**再 `+ Vector3.up * 0.6f`）。
  - `_fallbackEnd` 的产生逻辑不变（`Launch` 时按 `target.transform.position`）。
  - `FireballProjectile.OnArrive` 不改（仍用传入的 `end` 作为爆点）。
- `MeleeCombat`：只允许为 R-5 的「技能打断普攻」做接线（若 `BashRoutine` 里 `caster.sword.CancelAttack()` 已足够，则**不改** `MeleeCombat`；确需改时只允许改 `CancelAttack` 的幂等性，不得改伤害/窗口公式）。

**数据流**

`SkillInputController → SkillSystem.Cast(slot, target) → 组 SkillCastContext（槽位/触发器/状态名/目标/施法者/速度）→ 各效果 CanCast(ctx) → 扣蓝 → Execute(ctx) → 播放动画 + 结算（Bash 直接伤害；Fireball 生成 Projectile → 瞄准 Renderer 包围盒中心 → 命中/爆炸）`

**影响面清单**

| 项 | 内容 |
|---|---|
| 新增文件 | `Scripts/Combat/SkillCastContext.cs`（+ Unity 生成 `.meta`） |
| 修改文件 | `Scripts/Combat/SkillEffectBase.cs`、`SkillEffects.cs`、`SkillEffectFireball.cs`、`Projectile.cs`、`Scripts/Units/SkillSystem.cs`、`Scripts/Units/UnitStats.cs`；**仅在必要且证明必需时**才动 `Scripts/Combat/MeleeCombat.cs` / `FireballProjectile.cs` |
| 修改符号 | `SkillEffectBase.CanCast/Execute`（签名）、新增 `Trigger/StateName`；`BashEffect`/`FireballEffect` 的 `CanCast/Execute` 覆写；`Projectile.ComputeEnd` + 新增 `AimPoint`；`SkillSystem.Cast` 内部；`UnitStats.skillSpeed` |
| 字符串引用 | `SetTrigger("Bash"/"Skill")` 改为读 `ctx.Trigger`，**字面量不得变**；其余 Animator 参数名不动 |
| 场景 / 预制体 / Animator / `.asset` | **零改动**（`.asset` 里的效果数值一个都不改；新增的 `skillSpeed` 字段由代码默认值兜底） |
| `.meta` | 仅 1 个新脚本自带 |
| 测试文件 | `Tests/REFACTOR/Editor/REFACTOR_BatchAcceptance.cs`（`DefaultMilestone` → `"M4"`）；`Tests/REFACTOR/Runtime/REFACTOR_ProbeRunner.cs`（新增 4 条，11 → 15） |

**实施顺序**

1. 写 `SkillCastContext` + 改 `SkillEffectBase`/两个效果 + `SkillSystem`（编译通过，先跑一次 REFACTOR 确认技能两条不红）。
2. `UnitStats.skillSpeed` + 时长缩放（默认 1，行为不变）。
3. `Projectile.ComputeEnd` 改瞄准点。
4. `DefaultMilestone="M4"` + 新增 4 条断言。
5. 跑 L1：REFACTOR ×3 + SPEC01 ×2。
6. 证据 + REPORT（含重构讲稿，见 §5）。

**风险与回滚**

- 最可能错：① 效果时长缩放写成乘法（会把 1 变 2）；② `SetTrigger` 时机从「前摇开始」挪到「结算帧」；③ 包围盒中心把爆点抬到模型胶囊中心后，`ExplosionFx` 的伤害半径判定对象集合变化（SPEC01 `RangedShot` 兜底）；④ `SkillSystem.Cast` 的「先验全部 CanCast 再扣蓝」顺序被打乱（会产生「失败也扣蓝」的回归）。
  - 检测：REFACTOR `Skill.QW` / `Combat.RangedShot` / `Combat.MeleeHit` + 新增 4 条 + SPEC01 12 条。
- 回滚锚：`d02470c4`。

## §3 验证（Validation）

**AC 表**

| AC | Given / When / Then | 断言 ID | 证据 |
|---|---|---|---|
| AC-1 | 给定 REFACTOR 套件，连续跑 3 次，则每次 **15/15、fail=0、blocked=0、infra=0、exit=0** | 套件全部 15 条 | `Docs/QA/Evidence/REFACTOR-M4/editor_run1..3/` |
| AC-2 | 给定 SPEC01 套件，跑 2 次，则每次 **12/12、fail=0、exit=0** | SPEC01 全 12 条 | `Docs/QA/Evidence/SPEC-01/editor_run<新>/` |
| AC-3 | 给定技能链路，则上下文字段全部到位、技能仍能打断普攻、受击仍能打断技能、技能期间交互指令仍无效 | `Skill.InterruptAttack` / `Hit.InterruptSkill` / `Skill.InterruptInteract` | 同上 JSONL |
| AC-4 | 给定带 Renderer 的目标，则子弹落点 = 渲染包围盒中心（容差 0.05m），不再是 `position + 0.6` | `Projectile.AimPoint` | 同上 JSONL |
| AC-5 | 给定 `git status`，则只含本单声明文件 + 派单前既有脏项；**无 `.asset`/场景/预制体/Animator 改动** | — | `REFACTOR-M4/git-diff-stat.txt` |
| AC-6 | 给定既有 11 条断言，则其**口径与代码零改动**（只允许新增 4 条与头注释/blocked 列表） | diff 审查 | `REFACTOR-M4/test-diff.txt` |
| AC-7 | 给定 `UnitStats`，则新增 `skillSpeed` 默认 1，且 `Speed==1` 时效果时长与 M3 完全一致（同参数对比） | 代码审查 + `Skill.QW` 时序数据 | REPORT §2 |

**新增断言（REFACTOR 套件加 4 条，不得改前 11 条）**

1. `Skill.InterruptAttack`：骑士普攻出招中（进入 `AttackMelee`）立即 `SkillSystem.Cast(0, enemy)` → （a）`Cast` 返回 true；（b）普攻不再产生第二次命中（以 `MeleeCombat` 的命中次数/`_fired` 观测，窗口内计数不增）；（c）`≤0.6s` 内进入 `Bash`。
2. `Hit.InterruptSkill`：施放 W（`Skill`/`HeavySlash` 动画）后 0.1s 内对施法者 `TakeHit(1f)` → `≤0.5s` 进入 `Hit_A`，且单位未死（hp > 0）。
3. `Skill.InterruptInteract`：`SkillSystem.Cast` 进行中（进入 `Bash` 后）对同一单位调用 `CharacterBrain.CommandInteract(chest 夹具)` → 断言 `InteractionRunner.HasPending == false` 且单位未朝宝箱位移（技能期间交互指令无效）。技能结束后再下一次指令能正常产生 `HasPending == true`。
4. `Projectile.AimPoint`：造一个在 `transform.position + (0, 1.8, 0)` 处有 `Renderer` 的内存目标（`GameObject.CreatePrimitive` 或 `AddComponent<MeshRenderer>`），挂 `Projectile` 后 `Launch`，用反射取 `ComputeEnd()` → 断言与合并包围盒中心的距离 `≤0.05m`，且与 `transform.position + up*0.6` 的距离 `>0.1m`。夹具结束销毁。

> 断言必须走生产入口（`SkillSystem.Cast` / `TakeHit` / `CommandInteract`），不得直接改私有字段造状态；不得修改既有断言口径。

**运行次数**：REFACTOR **N=3**；SPEC01 **2 次**。

**自动化优先**：全部通过 `Tools/QA/In-Editor/{REFACTOR, SPEC01 (green)}` 菜单执行；**不得** `run_tests`，不得 batchmode。

**必须保存的证据**：两套的 `Assertions.jsonl` / `Summary_run*.json` / `ExitCode_run*.txt` / `InEditorResult.txt` + `git-diff-stat.txt`、`test-diff.txt`。

## §4 执行信息

| 项 | 内容 |
|---|---|
| 前置 | Unity 已开（模式 B），MCP 单实例；`is_compiling=false`；场景 `DungeonWorld` 且非脏 |
| 允许改动 | 上述 6 个脚本 + 新增 `Combat/SkillCastContext.cs`（必改）；`Combat/MeleeCombat.cs`、`Combat/FireballProjectile.cs`（**仅在证明必需时**）；`Tests/REFACTOR/**`（仅 4 条新断言 + `DefaultMilestone`）；`Docs/Handoff/REPORT_REFACTOR-M4.md`；`Docs/QA/Evidence/REFACTOR-M4/**` |
| 允许读取 | 本工单 + 被改文件现状（只读） |
| 绝不触碰 | `Art/**`、`Assets/Scenes/**`、`Prefabs/**`、`*.asset`（**尤其技能效果资产与 UnitStats 资产**）、Animator Controller、`ProjectSettings/**`、`Packages/**`、`AGENTS.md`、`Docs/Handoff/CURRENT.md`、其他 `TASK_*`/`REPORT_*`、`Tests/**` 里除 REFACTOR 套件的任何文件、现有 11 条 REFACTOR 与 12 条 SPEC01 断言口径 |
| 证据目录 | `Docs/QA/Evidence/REFACTOR-M4/`；SPEC01 进 `Docs/QA/Evidence/SPEC-01/editor_run<N>/` |
| 熔断 | `AGENTS.md` §6 + 计划书 §6；修复轮次 **2 轮**（M4 专属） |
| 执行预算 | `.cs` 文件数 ≤ 10；不重启 Unity；不跑全量 batch |

**停止条件（照计划书 §6）**：需要改 `.asset`/预制体/场景/Animator 才能继续；需要改技能数值；编译错误连续 2 轮；2 轮修复用尽仍红；`git status` 对不上。

## §5 学习产出

| 项 | 要求 |
|---|---|
| 讲稿 | **必须**写 `Docs/Learning/REFACTOR-LONG-01_讲稿.md`（M4 收口统一讲稿）：覆盖 M0–M4 各自「改了什么/为什么/怎么证明没变」，模板 `Docs/Learning/TEMPLATE_讲稿.md`；至少含 5 个 `文件:行` 与一张 M0–M4 依赖关系图（文字版即可） |
| 三句话版本 | 写进 REPORT §3 第 7 条 |
| 长任务汇总 | 另写 `Docs/Handoff/REPORT_REFACTOR-LONG-01.md`：M0–M4 汇总 + 《醒来目检清单》（模板见计划书 §8） |

## 自包含声明

本工单不依赖 `CURRENT.md` 或其他任务书即可执行；信息不足时在 REPORT 写“信息不足”并列缺项，**不**自行翻阅其他文档补全。
