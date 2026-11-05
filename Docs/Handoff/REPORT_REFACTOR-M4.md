# REPORT — REFACTOR-M4：技能与战斗收口（SkillCastContext + 渲染包围盒瞄准点）

> 对应 `Docs/Handoff/TASK_REFACTOR-M4.md`。节次与 `Docs/Handoff/workflow-rules.md` §2 一致，未删节。
> 执行环境：Unity 模式 B（编辑器常开 + `unityMCP`），实例 `Dungeon@4f5c34c82f31646e`，场景 `DungeonWorld`，Unity 2022.3.62f3c1。
> 基线 HEAD：`d02470c4`（全部 run 的 `Summary.head` = `d02470c47c5016b2a5cd1190cde9b76b655a50ff`）。

## 1. 结果摘要

`完成` —— 「一次技能施放」的上下文收敛为 `SkillCastContext`（槽位/触发器/状态名/目标/施法者/施法速度），三个效果类（Bash / Fireball / HeavySlash）不再自己拼 Animator 触发器名与来源；`UnitStats.skillSpeed` 默认 1（时长按 `1f/max(0,skillSpeed)` 缩放，默认完全等价于 M3）；子弹瞄准点从写死的 `position + 0.6m` 改为目标全部 `Renderer` 的合并包围盒中心（无 Renderer 回退 `transform.position`）。REFACTOR 套件 **15/15 × 3 连跑**（fail=0 / blocked=0 / infra=0 / exit=0），SPEC01 **12/12 × 2**（fail=0 / exit=0）。零场景/预制体/Animator/`.asset`/`ProjectSettings` 改动；除 TASK 清单外只多改了 1 个**必须**改的派生类 `HeavySlashEffect.cs`（记在 §4「计划外改动」）。修复轮 1 次（`attempt_run1` 红 1 条 → 修 → 15/15 连跑 3 次）。

## 2. AC 回填表

| AC | 结果 | 断言 ID | 关键数据 | 日志路径 |
|---|---|---|---|---|
| AC-1 | **通过** | 套件全部 15 条 | run1/2/3 均 **pass=15 fail=0 blocked=0 infra=0 exit=0**；15 条 id 与顺序三次完全一致（每次 JSONL 15 行、distinct=15） | `Docs/QA/Evidence/REFACTOR-M4/editor_run1..3/`（每次含 Assertions.jsonl / Summary_run1.json / ExitCode_run1.txt / Runs.jsonl / InEditorResult.txt） |
| AC-2 | **通过** | SPEC01 全 12 条 | `editor_run32`、`editor_run33` 均 **pass=12 fail=0 blocked=0 infra=0 exit=0**（`assertionLinesInFile=12`） | `Docs/QA/Evidence/SPEC-01/editor_run32/`、`editor_run33/` |
| AC-3 | **通过** | `Skill.InterruptAttack` / `Hit.InterruptSkill` / `Skill.InterruptInteract` | run1/2/3 一致：① 出招中施放 Q：cast=true、`MeleeCombat._timer` 收成 -1、Slash 命中 1→1（无第二次）、Bash 0.316/0.313/0.314s 进入（≤0.6s）、盾击 Blunt 命中 1 次；② 施放 W 后 0.1s 受 1 伤：0.053/0.055/0.053s 进入 `Hit_A`，hp 100→99（未死）；③ Bash 期间下交互指令：`HasPending=false`、0.25s 位移 0.000m，技能结束后同一条指令 `HasPending=true` | 同上 JSONL |
| AC-4 | **通过** | `Projectile.AimPoint` | 目标上两个子 MeshRenderer 都在 `+1.8m`：`ComputeEnd()`=(120, **1.8**, 120)，与合并中心距离 **0.000m**（容差 0.05），与旧式 `position+up*0.6` 距离 **1.200m**（>0.1）；无 Renderer 目标回退 `transform.position`，距离 **0.000m** | 同上 JSONL |
| AC-5 | **通过** | — | `git status --porcelain` 只含本单声明文件 + 工程脏项；`git status -- Assets/Scenes Assets/_Project/Prefabs "*.asset" "*.controller" Assets/_Project/Art ProjectSettings Packages` **输出为空**；无 `.asset`/场景/预制体/Animator 改动 | `Docs/QA/Evidence/REFACTOR-M4/git-diff-stat.txt` |
| AC-6 | **通过** | — | 测试套件 diff：`1/1` 行（BatchAcceptance 常量）+ `509/8` 行（ProbeRunner）；**删除行全集 = 8 行，全是常量与头注释**（`DefaultMilestone "M3"→"M4"` + 11 条断言标题/5~11 条列表被 15 条版本替换）；既有 11 条断言的函数体/期望/阈值**零删除** | `Docs/QA/Evidence/REFACTOR-M4/test-diff.txt` + `test-diff-u0.txt` |
| AC-7 | **通过** | 代码审查 + `Skill.QW` 时序 | `UnitStats.cs:27` 新增 `public float skillSpeed = 1f;`（追加在 attackSpeed 后，未改名/移动任何既有字段）；四处时长均为**除法**（`hitTime/ctx.Speed`、`totalTime/ctx.Speed`、`castDelay/ctx.Speed`、`flightDuration/ctx.Speed`）；`Skill.QW` 与 M3 同参数对比：Q `damageWait` 0.9317/0.9312/0.9320/0.9315s（M3=0.9317），Q 伤害 15、蓝耗 20、W 蓝耗 30、W `swordTimer=0` 全部一致 | `Docs/QA/Evidence/REFACTOR-M4/ac7-timing-compare.txt` |

- **运行次数**：REFACTOR **N=3**（AC-1 要求 3）＋1 次新增断言前的体检（`precheck_run1`，11/11）＋1 次修复轮现场（`attempt_run1`，14/15）；SPEC01 **N=2**（AC-2 要求 2）。三次 REFACTOR 数字完全一致。
- **修复轮**：1 轮（TASK 允许 2 轮）。`attempt_run1` 唯一红项 `Skill.InterruptAttack` 现场：`cast=True; timer=-1; bashEntered=false; slashHits=1; bluntHits=1`。根因是**测试夹具时序**而非产品代码：在 `AttackMelee` 起始帧施放时，① 大脑仍持有攻击指令，攻击动画收尾后会补一次新普攻 → 第二次 Slash 命中；② `PlayerBase.controller` 的 `Bash` 触发过渡只挂在 `Locomotion` 状态（见 §3 第 6 条），故动画要等 `AttackMelee` 收招才进 Bash。修法：在**命中窗口之后**（`normalizedTime ≥ 0.6`，第一下已结算）施放，并在 Cast 后临时禁用大脑（只观测这一次攻击），断言口径与阈值一字未改。
- 未运行项：DIAG / FND00B06（不在 M4 L1 口径内）；未用 `run_tests`、未用 batchmode、未重启 Unity。

## 3. 实现原理（面向所有者）

1. **根因 / 目标**：「放一次技能要准备什么」原先散在每个效果类里——效果自己去 `SetTrigger("Bash"/"Skill")`、自己 `caster.GetComponent<UnitHealth>()` 当伤害来源、自己按固定秒数等待；加一个新技能要猜触发器名，还要记得把来源接对。另一处是子弹瞄准点写死 `target.position + up*0.6`：对高个子模型打脚踝上方、对矮模型打头顶，命中点与飘字位置都偏。本次把「这次施放的完整上下文」做成值对象，把瞄准点改成「看模型实际渲染体积」。
2. **备选方案与为什么没选**：A（触发器名进 `SkillData` 资产）数据驱动最彻底，但要改 `.asset`（TASK 明令禁止）；C（只加 `slot` 参数）解决不了「效果自己拼动画名」。选 B：新增 `readonly struct SkillCastContext`，触发器/状态名由效果以 `virtual` 属性声明，`SkillSystem` 组包后传入 —— **零资产改动**，加效果不必改系统，且触发器名可被断言。
3. **实际数据流**：`SkillInputController → SkillSystem.Cast(slot,target)【验冷却→验蓝→按效果组包 ctx→逐个 CanCast→扣蓝→逐个 Execute→写冷却】→ BashEffect/FireballEffect/HeavySlashEffect 用 ctx.Target/Caster/CasterUnit/Trigger/Speed → Bash：SetTrigger(ctx.Trigger) 前摇 hitTime/Speed → TakeHit(Blunt, source=ctx.CasterUnit) → 后摇 totalTime/Speed；Fireball：前摇 castDelay/Speed → 生成 Projectile（duration=flightDuration/Speed）→ 每帧 ComputeEnd() = AimPoint(target)（Renderer 合并包围盒中心）→ OnArrive 用该点当爆点`。**顺序与失败语义逐行未变**：仍然是「先验全部 CanCast，再扣蓝，再 Execute 全部，最后写冷却」。
4. **关键代码位置（6 条）**：
   - `Assets/_Project/Scripts/Combat/SkillCastContext.cs:11` —— 上下文 7 个只读字段（Slot/Trigger/StateName/Target/Caster/CasterUnit/Speed）+ `:38` `WithTarget` 派生。
   - `Assets/_Project/Scripts/Units/SkillSystem.cs:53` —— `Cast` 保持原有顺序；`:46` `ContextFor` 用 `effect.Trigger/StateName` 组包；`:36` `Speed` = `sword.stats.skillSpeed`，无资产或 `<=0` 按 1。
   - `Assets/_Project/Scripts/Combat/SkillEffectBase.cs:16`/`:19` —— `Trigger`/`StateName` 默认 `"Skill"`；`:21` 抽象入口改为 `CanCast(SkillCastContext)`。
   - `Assets/_Project/Scripts/Combat/SkillEffects.cs:25` —— `BashEffect` 覆写 `Trigger="Bash"`；`:58` `SetTrigger(ctx.Trigger)`（时机仍是前摇开始）；`:61` `hitAt = hitTime / ctx.Speed`、后摇 `totalTime / ctx.Speed`。
   - `Assets/_Project/Scripts/Combat/Projectile.cs:33` —— `ComputeEnd` 改为 `AimPoint(target)`；`:42` `AimPoint` 遍历 `GetComponentsInChildren<Renderer>()` 合并 `bounds` 取 `center`，无 Renderer 回退 `unit.transform.position`（`+0.6m` 已删除）。
   - `Assets/_Project/Scripts/Units/UnitStats.cs:27` —— 新增 `skillSpeed = 1f`（序列化兼容：既有资产缺该键时保留字段初始化值 1）。
5. **边界 / 失败模式**：
   - **施放失败仍扣蓝**？`CanCast` 仍全部先跑、`Consume` 只在其后，`ContextFor` 只是纯构造（不写状态），失败路径与 M3 一致（SPEC01 技能断言照过）。
   - **时长写成乘法**？四处全部是 `原值 / Speed`，且 `Speed` 恒 > 0（`<=0` 收敛为 1）；`Speed=1` 时逐字等值，`Skill.QW` 时序对比见 AC-7。
   - **`SetTrigger` 挪到结算帧**？没有：Bash 的 `SetTrigger` 仍在收普攻/转向之后、前摇之前；Fireball 仍在 `Execute` 首行。
   - **爆点抬高后爆炸判定跑偏**？`FireballProjectile.OnArrive` 未改，爆点=传入 `end`；SPEC01 远程/技能 12 条全绿（`Combat.RangedShot` 在 REFACTOR 里只验发射，不依赖伤害）。
   - **无 Renderer 的纯逻辑单位**：回退 `transform.position`（与旧 `position+0.6` 差 0.6m，但这类单位本来就没有可视命中点；断言第 2 步单独覆盖）。
   - **瞄准点每帧分配**：`GetComponentsInChildren<Renderer>()` 每帧产生一次数组——本项目弹道同屏数量极小，未做缓存（见 §7）。
6. **与计划的差异及原因**：
   - TASK §4 允许改动清单里没有 `HeavySlashEffect.cs`，但它是 `SkillEffectBase` 的第三个派生类（`Skill.QW` 的 W 用它），抽象签名一改**必须**跟着改，否则编译红。改动只有 3 行（`caster`→`ctx.Caster`），语义等价，已记入 §4「计划外改动」。
   - 新增第 5~8 条断言的**分节注释编号**用 `M4-A..M4-D`（未重排既有 1~11 的注释编号），目的是把测试 diff 压到最小（AC-6）。
   - `Skill.InterruptAttack` 的施放时点取「`AttackMelee` 且 `normalizedTime ≥ 0.6`」（第一下已结算、仍在收招），而不是进入状态的第 0 帧。原因是从控制器读到的既有事实：`Assets/_Project/Art/Animations/PlayerBase.controller:78-102` 的 `Bash` 过渡（条件 `Bash`、`m_HasExitTime: 0`）被 `:1050` 挂在 **`Locomotion` 状态**的过渡表里（`Locomotion` 起于 `:1043`），所以从 `AttackMelee` 起手施放时动画必然等到收招才切 Bash（≈0.7s），那不是 M4 代码能改的（改控制器属禁止项）。在命中窗口之后施放既满足「出招中」也满足「≤0.6s 进入 Bash」（实测 0.31s），并正好对上 TASK 里「不再产生**第二次**命中」的措辞。
   - 测试侧新增 4 个工具方法（`GetAttackTimer`/`RefillSkills`/`RestoreAgent`/`ComputeEndOf`）：`RefillSkills` 走生产调试入口（`UnitMana.Refill` / `SkillSystem.ResetCooldowns`）让三条技能断言互不依赖；`RestoreAgent` 在收尾把 `NavMeshAgent.isStopped` 恢复为 false（BashEffect 只置 true 不复位，属既有实现，本单不改产品行为）。
7. **三句话版本**：
   - 以前加一个技能效果要自己猜 Animator 触发器名、自己找施法者当伤害来源；现在 `SkillSystem.Cast` 把「槽位/触发器/状态名/目标/施法者/施法速度」打成一个 `SkillCastContext` 传下去，效果只写「这招做什么」。
   - 子弹不再固定瞄「脚底 +0.6m」，而是取目标所有 Renderer 的合并包围盒中心，高矮模型都打在身体上；没有 Renderer 的单位回退到 `transform.position`。
   - 行为不变靠三件事兜底：改动只碰代码不碰任何 `.asset`/预制体/控制器、`skillSpeed` 默认 1 且时长全是除法（Q/W 时序与 M3 差 <1ms）、既有 11 条断言口径零改动 + 新增 4 条（打断链 3 条 + 瞄准点 1 条）连跑三次 15/15。

## 4. 受影响模块导航

| 模块 | 职责 | 入口（类 / 方法 / 场景对象） |
|---|---|---|
| 施放上下文 | 一次施放的不可变值对象（槽位/触发器/状态名/目标/施法者/速度） | `SkillCastContext`（`Scripts/Combat/SkillCastContext.cs`）、`WithTarget` |
| 技能系统 | 验冷却/验蓝/组包/效果链/写冷却；不认识具体技能 | `SkillSystem.Cast`、`SkillSystem.ContextFor`、`SkillSystem.Speed`（`Scripts/Units/SkillSystem.cs`） |
| 效果基类 | 效果契约 + 默认触发器等 | `SkillEffectBase.CanCast/Execute/Trigger/StateName` |
| 盾击 | 单目标伤害 + 击退 + 僵直；触发器 `Bash` | `BashEffect`（`Scripts/Combat/SkillEffects.cs`）、`BashRoutine` |
| 火球 | 前摇 → 生成追踪弹 → 爆炸；触发器 `Skill` | `FireballEffect`（`Scripts/Combat/SkillEffectFireball.cs`） |
| 强化斩 | 立即释放一次技能斩（无额外参数） | `HeavySlashEffect`（`Scripts/Combat/HeavySlashEffect.cs`） |
| 抛射物 | 追踪 + 抛物线 + 到达结算 + 池化；瞄准点=渲染包围盒中心 | `Projectile.ComputeEnd/AimPoint`（`Scripts/Combat/Projectile.cs`）、`FireballProjectile.OnArrive`（未改） |
| 单位数值 | 新增施法速度倍率（默认 1） | `UnitStats.skillSpeed`（`Scripts/Units/UnitStats.cs:27`） |

数据流：`SkillInputController → SkillSystem.Cast(slot,target) → SkillCastContext → 效果.CanCast(全部) → 扣蓝 → 效果.Execute(全部) → Animator.SetTrigger(ctx.Trigger) + 结算（Bash 直接伤害 / Fireball 生成 Projectile → ComputeEnd=AimPoint → 命中/爆炸）`

### 4.1 影响面清单（开工前输出，已验证与实际改动一致）

| 项 | 内容 |
|---|---|
| 新增文件 | `Assets/_Project/Scripts/Combat/SkillCastContext.cs`（+ Unity 自动生成 `SkillCastContext.cs.meta`，guid `90b8a4789da599142955290980a81d7e`） |
| 新增符号 | `SkillCastContext`（struct + 7 字段 + 构造 + `WithTarget`）、`SkillEffectBase.Trigger/StateName`、`SkillSystem.Speed`（私有属性）、`SkillSystem.ContextFor`、`Projectile.AimPoint`、`UnitStats.skillSpeed` |
| 修改符号 | `SkillEffectBase.CanCast/Execute`（签名 `(SkillSystem,Transform)` → `(SkillCastContext)`）、`BashEffect/FireballEffect/HeavySlashEffect` 的同名覆写、`BashEffect.Trigger/StateName`、`Projectile.ComputeEnd`、`SkillSystem.Cast` 内部两处调用 |
| 字符串引用核查 | 触发器字面量 `"Bash"`/`"Skill"` **语义不变**，只是从方法体移到 `Trigger` 属性（`SkillEffects.cs:25`、效果基类默认值）；`ResetTrigger("Interrupt")`、`SetTrigger("Interact"/"Hit"/"Die"/"Revive")` 全部未动；无新增 `SendMessage`；无新增 `GameObject.Find` |
| 场景 / 预制体 / Animator / `.asset` | **零改动**（`git status` 对 `Assets/Scenes`、`Assets/_Project/Prefabs`、`*.asset`、`*.controller`、`Assets/_Project/Art`、`ProjectSettings`、`Packages` 输出为空） |
| `.meta` | 仅新增脚本自带 1 个（Unity 生成，非手写） |
| 测试文件 | `Tests/REFACTOR/Editor/REFACTOR_BatchAcceptance.cs`（`DefaultMilestone` → `"M4"`）；`Tests/REFACTOR/Runtime/REFACTOR_ProbeRunner.cs`（新增 4 条断言 + 头注释 + blocked id + 4 个工具方法） |

**改动文件清单**（与 TASK §4 允许改动逐项比对）

| 文件 | 改了什么 |
|---|---|
| `Assets/_Project/Scripts/Combat/SkillCastContext.cs`（新） | 施放上下文值对象（7 字段 + 构造 + `WithTarget`） |
| `Assets/_Project/Scripts/Combat/SkillEffectBase.cs` | `CanCast/Execute` 改收 `SkillCastContext`；新增 `virtual Trigger/StateName`（默认 `"Skill"`） |
| `Assets/_Project/Scripts/Combat/SkillEffects.cs` | `BashEffect`：`Trigger/StateName="Bash"`、`CanCast/Execute/BashRoutine` 改 ctx、`SetTrigger(ctx.Trigger)`、新增 `hitAt/totalAt = 原值/Speed`、伤害来源用 `ctx.CasterUnit` |
| `Assets/_Project/Scripts/Combat/SkillEffectFireball.cs` | `CanCast/Execute/FireRoutine` 改 ctx、`SetTrigger(ctx.Trigger)`、`delay=castDelay/Speed`、`p.duration=flightDuration/Speed`、来源用 `ctx.CasterUnit` |
| `Assets/_Project/Scripts/Combat/HeavySlashEffect.cs`（**计划外，必需**） | 仅签名跟随基类：`caster.sword` → `ctx.Caster.sword`，3 行 |
| `Assets/_Project/Scripts/Combat/Projectile.cs` | `ComputeEnd` → `AimPoint(target)`；新增 `AimPoint`（Renderer 合并包围盒中心 / 无 Renderer 回退 `position`） |
| `Assets/_Project/Scripts/Units/SkillSystem.cs` | 新增 `Speed`/`ContextFor`；`Cast` 内两处调用改用 ctx（顺序与失败语义不变） |
| `Assets/_Project/Scripts/Units/UnitStats.cs` | 新增 `skillSpeed = 1f`（追加在 `attackSpeed` 之后） |
| `Assets/_Project/Tests/REFACTOR/Editor/REFACTOR_BatchAcceptance.cs` | `DefaultMilestone = "M4"` |
| `Assets/_Project/Tests/REFACTOR/Runtime/REFACTOR_ProbeRunner.cs` | 新增 4 条断言 + 4 个工具方法 + 头注释/blocked id；既有 11 条一字未动 |
| `Assembly-CSharp.csproj` / `Assembly-CSharp-Editor.csproj` | Unity 自动重生成（新增 1 个脚本条目）；**开工前已是脏项**，未手改 |

**计划外改动**：`Assets/_Project/Scripts/Combat/HeavySlashEffect.cs`（`SkillEffectBase` 的第三个派生类，抽象签名变更必须跟随；只改 3 行、语义等价，见 §3 第 6 条）。除此之外未触碰 `MeleeCombat.cs` / `FireballProjectile.cs`（证明不需要：REFACTOR 15/15 已覆盖「技能打断普攻」与弹道，`MeleeCombat.CancelAttack` 的幂等性满足需要）。

## 5. 证据清单

| 文件 | 内容 |
|---|---|
| `Docs/QA/Evidence/REFACTOR-M4/precheck_run1/*` | 新增断言前的一次性体检（11/11、exit 0），用于证明上下文改造没先弄红 Skills/Combat |
| `Docs/QA/Evidence/REFACTOR-M4/attempt_run1/*` | 修复轮现场（14/15、fail=1、exit=2；红项 `Skill.InterruptAttack` 的 `slashHits=1 / bashEntered=false`） |
| `Docs/QA/Evidence/REFACTOR-M4/editor_run1..3/*` | AC-1/AC-3/AC-4：每次 15 条断言 JSONL、Summary（milestone=M4、head=d02470c4）、ExitCode（0）、Runs、InEditorResult |
| `Docs/QA/Evidence/REFACTOR-M4/runs-summary.txt` | 5 个 REFACTOR run + 2 个 SPEC01 run 的时间/数字/退出码/未运行项 |
| `Docs/QA/Evidence/REFACTOR-M4/git-diff-stat.txt` | AC-5：HEAD、`git status --porcelain`、`git diff --stat`、禁止路径空改动核查 |
| `Docs/QA/Evidence/REFACTOR-M4/test-diff.txt`、`test-diff-u0.txt` | AC-6：删除行全集（8 行，全为常量/注释）+ 原始 `git diff -U0` |
| `Docs/QA/Evidence/REFACTOR-M4/ac7-timing-compare.txt` | AC-7：M3 vs M4 的 Q/W 时序/蓝耗/伤害对比 |
| `Docs/QA/Evidence/SPEC-01/editor_run32..33/*` | AC-2：SPEC01 12/12 的 JSONL / Summary / ExitCode / InEditorResult |

复跑方式：编辑器内菜单 `Tools/QA/In-Editor/REFACTOR`、`Tools/QA/In-Editor/SPEC01 (green)`（模式 B / `unityMCP`，**未**用 `run_tests`、**未**用 batchmode）；证据子目录由 `QA_InEditorMenu.NextSubdir` 自动递增，只追加不覆盖。

## 6. 学习讲稿

`Docs/Learning/REFACTOR-LONG-01_讲稿.md` —— 已写（M4 收口统一讲稿，覆盖 M0–M4：各自改了什么/为什么/怎么证明没变；含 7 个 `文件:行` 与 M0–M4 依赖关系图）。

## 7. 未决项

- **`Bash` 动画过渡只挂在 `Locomotion`（资产事实，非本单造成）**：`PlayerBase.controller` 里条件为 `Bash` 的过渡属于 `Locomotion` 状态（退出时间 0.732、`m_HasExitTime: 0`）。因此「普攻出招中放 Q」的动画观感是「收招后立刻切 Bash」；代码侧已即时收掉普攻判定（`_timer=-1`）。若希望出招中**直接**切 Bash 动画，需要改 Animator Controller（本单禁止），需所有者决定。
- **`BashEffect` 只置 `agent.isStopped = true` 未复位（静态审查发现，既有实现）**：没有其他地方把它设回 false（`grep isStopped` 只有 `SkillEffects.cs:51` 一处）。本单未改产品行为，测试收尾用 `RestoreAgent` 恢复；是否会让「施放过盾击的单位之后不再移动」需实机确认（本单未做该验证）。
- **`Projectile.AimPoint` 每帧 `GetComponentsInChildren<Renderer>()` 会分配一个数组**：同屏弹道很少，暂不优化；若后续出现大量投射物，改成静态缓冲或 `List<Renderer>` 复用即可（不改语义）。
- **`skillSpeed ≠ 1` 的加速路径无资产/无用例覆盖**：本单禁止改 `.asset`，只由代码审查（除法）保证。
- **需所有者目检（3 项）**：
  1. 实机试：骑士普攻中按 Q（看是否收招后立刻盾击、伤害/击退正常）→ W 技能中被打（看是否进 `Hit_A`）→ 放技能时点宝箱（不应走过去/不应开箱）→ 法师/弓手普攻弹道是否打在模型身体中心（对比改动前后飘字高度）。
  2. 打开 `Knight_Unit` 预制体与任一 `UnitStats` 资产，确认 Inspector 里 `attackSpeed` 之后多出「施法速度倍率 = 1」，其余字段无改名/丢失；技能效果资产（Bash/Fireball/Heavy Slash）数值未变。
  3. 看 GitHub Desktop 改动列表，确认只含 §4 清单文件；`Assembly-CSharp*.csproj`、`Docs/Handoff/TASK_REFACTOR-M4.md`、`Docs/QA/Evidence/DIAG-SKILL/editor_run3/`、`Docs/QA/Evidence/FND-00B-06/editor_run4/` 为**开工前既有脏项/派单文件**，非本里程碑改动。

## 8. 建议提交

```text
refactor(skill): M4 技能上下文与瞄准点收口（SkillCastContext + 渲染包围盒 AimPoint；skillSpeed 默认 1；REFACTOR 15/15×3 + SPEC01 12/12×2）
为什么：把「一次施放要准备什么」（槽位/触发器/状态名/目标/施法者/速度）收敛成 SkillCastContext 传给效果，新增技能不必再猜 Animator 名与来源；子弹落点从写死的 position+0.6m 改为目标渲染包围盒中心，高矮模型都命中身体；零资产改动，行为由既有 11 条 + 新增 4 条断言与 SPEC01 12 条兜底。
```

## 9. 消耗记录

- 钱包：`Go 订阅`
- 模型完整 ID：`opencode-go/deepseek-v4.1-flash`
- 运行配额相关：编辑器内菜单执行 7 次（REFACTOR ×5：precheck/attempt/3 次验收；SPEC01 ×2），均单实例、未重启 Unity、未跑 batchmode；编译/域重载 3 次（新建候选脚本 → 上下文改造 → 断言新增），控制台 0 error。
