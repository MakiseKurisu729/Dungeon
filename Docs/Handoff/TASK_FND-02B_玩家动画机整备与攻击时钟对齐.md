# TASK_FND-02B — 玩家动画机整备与攻击时钟对齐

> 状态：`Ready`（提案已批准；阶段 1 最终影响面、`.controller` 改动必要性与 Unity 关闭尚未确认）
> 风险：**高风险**（触及玩家普攻/远程执行器、伤害触发时机与公共动画状态契约；须外部顾问＋【大脑】双重终审）
> 类型：【重构】玩家动画驱动修复、双攻击路径统一与动画时钟对齐（含伤害飘字放大顺手项）
> 架构依据：
> - **事实基线决策 #4（动画时钟原则）**（`Docs/01_总体架构与内容生产规范.md` §11.1.6）：凡“动画与代码要同步”（命中帧/放箭帧），代码轮询 `Animator.GetCurrentAnimatorStateInfo(0).normalizedTime` 可调比例窗口（0~1），失败时回退代码时钟；**不用 `AnimationEvent` 作主命中路径**。
> - **事实基线决策 #5（攻速系统）**（`Docs/01_总体架构与内容生产规范.md` §11.1.7）：`UnitStats.attackSpeed` 为唯一攻速来源 → `UnitAnimatorSpeed` 写 Animator 参数 `"AttackSpeed"`＋执行器时长/冷却同源缩放，禁止维护两套互相矛盾的计时时钟。
> - `Docs/01_总体架构与内容生产规范.md` §5.3 统一伤害入口、§6.1 指令执行与动画反馈、§11.1～11.3 单层动画与动作时序。
> 对应结构债：SD-02；前置依赖 FND-02A（`Done`）已提供自动开局与 `(-2.72, 0, -12)` 真实 `Skeleton_Warrior` 固定遭遇
> 唯一报告：`Docs/Handoff/REPORT_FND-02B.md`

---

## 1. 唯一结果与边界

在真实 `Assets/Scenes/DungeonWorld.unity` 场景中，三名玩家角色（Knight / Archer / Mage）的移动与普攻表现层—逻辑层契约完全对齐：
1. **三角色移动动画参数正常驱动**（移动时混合树参数随实际速度非零更新，停下归零，消除 Idle 滑步）；
2. **统一“指定攻击”与“自动攻击”两条路径的动画触发**（无论手动 `CommandAttack` 还是 Aggro 自动索敌攻击，均触发进入 `Attack_Draw / Attack_Release`，消除自动攻击 Idle 出箭）；
3. **近战命中与远程发射对齐动画时钟（决策 #4 + #5）**：`SwordSwing` 与 `RangedCombat` 在 `layer 0` 轮询攻击状态的 `normalizedTime` 比例窗口（Inspector 暴露 `[Range(0f, 1f)]` 可调字段），单次动画周期严格结算 **1 次** 命中/发射，消除骑士早出伤与弓手“一弓三箭”，法师从下令到首次出球节奏符合 `attackSpeed` 预期；
4. **战后收尾与受击对齐**：目标死亡或打断后 Animator 正常回落 `Locomotion`（不卡 `Attack_Draw`）；玩家受非致命伤害时触发进入 `Hit_A`；
5. **伤害飘字放大（Q-01 已批准并入本单）**：`DamageNumber` 视觉尺寸放大为原基线的 **3～4 倍（默认 3.5x，Inspector 可调）**。

**严格边界（非目标）**：
- 敌方 `Skeleton_Warrior` 的移动/出伤/动作已对齐，**不得修改敌方 AI/动画**；
- 严禁使用 `AnimationEvent` 作主命中/发射路径；不新建 ScriptableObject 动作资产框架；
- 不修镜头左键拖动、Q 技能、迷雾遮罩、宝箱、救援、传送或团灭重来；不升级 Unity、不安装新包。

---

## 2. 已知事实与六条表现层缺陷（自包含输入）

1. **缺陷 1（移动无动画）**：V02A-1 实测三名玩家移动时均保持 Idle 姿态滑步；需核查 `UnitAnimatorSpeed` / `UnitBrain` / `CharacterBrain` / `Animator` 参数驱动（注意 `PlayerBase.controller` 中状态名 `" Locomotion"` 含前导空格，参数 `SpeedX`/`SpeedZ`/`Grounded`/`AttackSpeed`）。
2. **缺陷 2（骑士出伤早于动作完成）**：FND-00B-06 实测 `SwordSwing.attackLayer=1` 越界（控制器仅 1 层 `layer=0`）、`attackStateName="Attack"` 不在状态列表（实际为 `Attack_Draw`/`Attack_Release`），导致动画时钟读取失败而走独立计时提前出伤。
3. **缺陷 3（弓手一弓多箭）**：V02A-1 实测弓手一次拉弓周期（前摇+后摇）结算 3 箭，代码冷却时钟（约动画周期 1/3）与 Animator 播放周期各跑各的。
4. **缺陷 4（法师前摇过长）**：V02A-1 实测法师从下令到出手存在秒级拖沓。
5. **缺陷 5（【关键线索】自动攻击不播动画）**：V02A-1 实测指定攻击出箭瞬间处于拉弓动画，但自动攻击（Aggro 范围自动索敌开火）出箭瞬间仍处 Idle——存在两条攻击路径，自动攻击路径未触发攻击动画状态。
6. **缺陷 6（伤害飘字过小）**：所有者要求放大 3～4 倍（已批准默认 3.5x 并入本单）。
7. **FND-00B-06 遗留**：玩家 `ApplyDamage` 非致命受击未进入 `Hit_A`；战斗结束后 7.6s 仍停留 `Attack_Draw`（`normalizedTime≈0.63`）。

---

## 3. 阶段闸门与 Unity 安全

### 阶段 1：只读影响面确认（当前阶段）

执行者只读本工单，禁止翻阅其他背景文档补全要求。可只读检查 `Assets/_Project/Scripts/`、场景、预制体与 `.controller/.overrideController` YAML，但在**任何文件编辑或 Unity 调用前**必须输出以下完整清单并立即停止：

1. **仓库状态**：分支、完整 HEAD、`git status --porcelain -uall` 原始输出。标注 FND-02A 尚未提交的工作区改动及 `Docs/Handoff/` 豁免项，确认是否存在意外脏项。
2. **六条缺陷＋06 遗留的只读根因定位**：
   - 移动参数驱动：哪个脚本在写 `SpeedX`/`SpeedZ`？为何真实移动时三角色滑步？
   - 双攻击路径：`CharacterBrain` / `UnitBrain` / `SwordSwing` / `RangedCombat` 中“指定攻击”与“自动攻击”各自的完整调用链是什么？自动攻击在哪里绕过了 Animator 触发？
   - 近战/远程时钟与法师前摇：`SwordSwing` 与 `RangedCombat` 当前如何计时、如何触发 `Attack` / `Attack_Draw` / `Attack_Release`？法师秒级前摇的具体成因（代码计时 vs Controller 过渡/Clip 时长）是什么？
   - 战后卡 `Attack_Draw` 与玩家受击 `Hit_A`：为何战后停在 `Attack_Draw`？为何玩家受击不进 `Hit_A`？
   - 伤害飘字：`DamageNumber` 的字体/缩放定义在哪个脚本或预制体？
3. **受影响文件与符号清单**：
   - 计划修改的脚本路径、类名、方法签名、新增/修改的 Inspector 可调字段（含 `[Tooltip]` 与 `[Range(0f,1f)]`）；
   - **`.controller` / `.overrideController` / `.prefab` / `.unity` 是否需要修改？**（按用户裁决：默认只改脚本；若阶段 1 证实必须改 `.controller` 或预制体，必须单列精确文件路径、GUID、状态/过渡字段及理由，交用户二次确认后才可动）；
   - 字符串引用核查清单（Animator 参数名 `SpeedX`/`SpeedZ`/`Grounded`/`AttackSpeed`/`Attack`/`Hit`/`Interrupt`、状态名 `" Locomotion"`/`Attack_Draw`/`Attack_Release`/`Hit_A`/`Downed`、`SendMessage`/`Find` 等）；
   - 计划新增的测试脚本/`.meta`（`Assets/_Project/Tests/FND02B/`）、随测自动更新的 `.csproj`、证据目录 `Docs/QA/Evidence/FND-02B/` 与 `Docs/Handoff/REPORT_FND-02B.md`；
   - 阶段 2 计划执行的完整 Unity 命令行。
4. **输出清单后立即停止，等待用户明确确认（及 `.controller` 二次确认、Unity 关闭确认）。**

### 阶段 2：red 与实施前置

- 获用户确认清单并确认关闭 Unity（且核实无 `Unity.exe` 残留、`Temp/UnityLockfile` 不存在）后，先写 `Docs/QA/Evidence/FND-02B/00_前置确认.txt`。
- **先写测试跑 red**：生产代码与资产保持阶段 1 状态，运行 `FND02B_BatchAcceptance.Run` 产出有效 red（至少证明移动参数、自动攻击动画、一弓多箭/时钟窗口、受击/战后收尾、飘字倍率中的当前缺陷因产品状态失败，`infra=0`、无编译错误）。
- **最小实现到 green**：因本单涉及动画时钟与周期时序，按诊断纪律（AGENTS.md §4.3 / 事实基线 §8）同一入口连续运行 **green N=3 次（run1、run2、run3）** 全部通过且数据一致。
- 严禁整文件重写代码或 YAML；严禁执行任何 git 写操作。

---

## 4. Unity 专属警告

1. 重命名类或移动文件必须连同 `.meta` 一起（本单原则上不重命名/不移动现有脚本）。
2. Animator 参数名、状态名（尤注意 `" Locomotion"` 前导空格、`Attack_Draw`、`Attack_Release`、`Hit_A`）、`SendMessage`、`GameObject.Find` 等字符串引用必须在阶段 1 单列核查。
3. 场景/预制体/Controller 中的组件与资产引用走 GUID，修改后必须验证引用未断裂，已有 Inspector 公开字段名保持稳定以避免序列化丢失，新增公开字段必须带 `[Tooltip]`。

---

## 5. 自动化验收标准

唯一入口：`FND02B_BatchAcceptance.Run`（连续运行 **N=3 次**：`run1`、`run2`、`run3`）。

| 断言 ID | 客观预期 | 必留数据 |
|---|---|---|
| `Locomotion.AllPlayersMoveParam` | Knight、Archer、Mage 经生产移动命令在 NavMesh 移动期间处于 `Locomotion` 且驱动移动的 Animator 参数随实际速度非零变化（匹配混合树阈值），停下后归零 | 三角色名称、移动速度峰值、Animator 移动参数峰值与停稳值、状态名采样 |
| `Attack.DualPathUnified` | **指定攻击**（`CommandAttack`）与**自动攻击**（Aggro 自动索敌）两条路径下，出招角色均触发进入 `Attack_Draw / Attack_Release`，不存在停留 `Locomotion`/Idle 却产生伤害或发射投射物 | 两条路径各自的触发帧、出伤/出射瞬间的 Animator 状态名与 `normalizedTime`、调用链 |
| `Clock.MeleeNormalizedWindow` | 骑士近战命中由 `layer 0` 攻击释放状态的 `normalizedTime` 可调窗口驱动；出伤瞬间处于配置窗口内，单次挥砍动画周期恰结算 1 次伤害；`"AttackSpeed"` 参数等于 `UnitStats.attackSpeed` | 配置窗口 `[start, end]`、实际出伤时的状态名与 `normalizedTime`、单周期伤害次数、`AttackSpeed` 参数值 |
| `Clock.RangedOneShotPerCycle` | 弓手与法师在单个攻击动画周期内恰发射 **1 次** 投射物（消除一弓三箭）；发射瞬间处于配置的可调 `normalizedTime` 窗口内；法师从下令（已在射程内）到首次进入释放/发射耗时符合攻速周期（无秒级异常卡顿） | 弓手/法师单周期动画进入次数 vs 投射物生成次数、发射瞬间状态名与 `normalizedTime`、法师下令至首次出手耗时 |
| `Animation.PostCombatAndHit` | 战斗结束（目标死亡或停止攻击）后限定时间内三角色均回到 `Locomotion`（不卡 `Attack_Draw`）；玩家经生产 `UnitHealth.ApplyDamage` 受非致命伤害后进入 `Hit_A` | 战后状态名与回落耗时、受击前后 HP、`OnHit` 次数、受击后观察到的 `Hit_A` 状态哈希/`normalizedTime` |
| `UI.DamageNumberScale` | 战斗结算生成的 `DamageNumber` 实例视觉尺寸（字体大小或 Transform 缩放）为原基线的 3～4 倍（目标 3.5x），且暴露可调字段 | 原基线值、当前配置倍率、运行时实例实际 `fontSize`/`localScale` |
| `Regression.FND02A` | FND-02A 的自动开局（`GameStarted=true`）、固定遭遇存在、真实移动接近与零新增 Console 错误/异常继续全部通过 | `GameStarted`、固定敌人 ID/坐标、Console 错误/异常计数 |

---

## 6. 证据与人工目检

证据目录：`Docs/QA/Evidence/FND-02B/`，至少包含：
- `00_前置确认.txt`
- `02B_Red_Assertions.jsonl`、`02B_Red_Summary.json`、`02B_Red_ExitCode.txt`、`02B_Red_Automation.log`
- `02B_Assertions.jsonl`（汇总 green run1～run3）
- `02B_Automation_run1.log`、`02B_Automation_run2.log`、`02B_Automation_run3.log`
- `02B_Summary_run1.json`、`02B_Summary_run2.json`、`02B_Summary_run3.json`
- `02B_ExitCode_run1.txt`、`02B_ExitCode_run2.txt`、`02B_ExitCode_run3.txt`
- `02B_人工目检.md`（执行者只留“待目检”，严禁代填）

人工目检（3 项）：
- `V02B-1`：三角色移动不再 Idle 滑步；手动指定攻击与自动索敌攻击均播放攻击动作。
- `V02B-2`：骑士挥剑出伤与动作同步、弓手一弓一箭、法师出手无秒级拖沓，且 Inspector 可调比例窗口可见可调。
- `V02B-3`：伤害飘字放大 3～4 倍后清晰易读。

---

## 7. REPORT 与完成判定

`Docs/Handoff/REPORT_FND-02B.md` 必须包含：结果摘要、完整 HEAD 与工作区基线、唯一入口与完整命令、red 与 green 三次（run1～run3）结果、逐项断言表、原始日志说明、夹具边界、完整 diff、变更文件、未覆盖项、实现原理（≤500 字）、受影响模块导航说明（≤200 字）、Go 订阅及 `opencode-go/deepseek-v4.1-flash` 完整模型 ID、遗留问题与“建议提交”精确清单。

**高风险批次须经用户人工目检 + 外部顾问复核 +【大脑】终审后才可标记 Done；执行者不得修改 `CURRENT.md` 或自行宣称完成。**
