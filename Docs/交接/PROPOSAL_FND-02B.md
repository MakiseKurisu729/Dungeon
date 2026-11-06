# PROPOSAL_FND-02B — 玩家动画机整备与攻击—伤害时钟对齐

> 状态：`Approved`（2026-11-04 用户明确回复“批准”，Q-01～Q-03 默认值全部采纳）
> 对应规划：`Docs/Handoff/TASK_FND-02_重构与最小可玩原型规划.md` §7、结构债 SD-02
> 输入来源：`Docs/QA/Evidence/FND-02A/02A_人工目检.md` 第二～四节（表现层六条缺陷）、FND-00B-06 补验报告
> 类型：【重构】玩家动画驱动修复、双攻击路径统一与动画时钟对齐（含伤害飘字顺手项）
> 风险：**高风险**（触及玩家普攻/远程执行器、伤害触发时机与公共动画状态契约；实施须外部顾问＋【大脑】双重终审）
> 架构依据：
> - **事实基线决策 #4（动画时钟原则）**（`Docs/01_总体架构与内容生产规范.md` §11.1.6 / 基线架构铁律）：凡“动画与代码要同步”（命中帧/放箭帧），代码轮询 `Animator.GetCurrentAnimatorStateInfo(0).normalizedTime` 比例窗口（0~1）并暴露为 Inspector 可调参数，读取失败时回退代码时钟；**不用 `AnimationEvent` 作主命中路径**。
> - **事实基线决策 #5（攻速系统）**（`Docs/01_总体架构与内容生产规范.md` §11.1.7 / 基线架构铁律）：`UnitStats.attackSpeed` 为唯一攻速来源 → `UnitAnimatorSpeed` 写入 Animator 参数 `"AttackSpeed"`（攻击状态 Speed 乘子绑定该参数）＋执行器时长/冷却按攻速同源缩放，禁止维护两套独立互相矛盾的计时时钟。
> - `Docs/01_总体架构与内容生产规范.md` §5.3 统一伤害入口、§6.1 指令执行与动画反馈、§11.1～11.3 单层动画与动作时序。
> **本提案不是正式 TASK，不授权修改代码/资产或调用 Unity；未经用户明确回复“批准”不得生成正式 TASK 文件。**

---

## 1. 提案目标与事实边界

### 1.1 唯一结果

在 FND-02A 真实固定遭遇场景下，三名玩家角色（Knight / Archer / Mage）的**移动动画参数正常驱动**、**指定攻击与自动攻击两条路径统一触发攻击动画状态**、**近战命中与远程出箭/出球严格对齐动画时钟（`normalizedTime` 可调比例窗口，单周期单次结算，消除骑士早出伤、弓手一弓三箭与法师秒级拖沓前摇）**、**战后正常回落 `Locomotion` 且玩家受击可进入 `Hit_A`**，同时完成**伤害飘字放大 3～4 倍**。

### 1.2 已知事实与六条表现层缺陷映射

对照事实：敌方 `Skeleton_Warrior` 的移动/出伤/动作完全对齐，病灶集中于**玩家侧**。

| 缺陷 # | V02A-1 实测现象 | 根因线索与 FND-00B-06 互证 | 本单整备目标 |
|---|---|---|---|
| 1 | **移动无动画**：三角色均保持 Idle 姿态滑步 | 玩家侧移动动画参数（`SpeedX`/`SpeedZ` 或 `Speed`）在真实移动/不同角色上未正确持续驱动 | 修复全玩家角色移动动画参数驱动，移动时混合树参数随实际速度非零变化，停下归零 |
| 2 | **攻击动作与伤害不同步**：骑士出伤早于动作完成 | `SwordSwing.attackLayer=1` 越界（控制器仅 1 层 `layer=0`）、`attackStateName="Attack"` 不存在（实际为 `Attack_Draw`/`Attack_Release`），导致动画时钟读取失效而回退独立计时器提前出伤 | 修正层索引（`0`）与状态契约，按 `Attack_Release` 的 `normalizedTime` 可调比例窗口触发唯一一次命中 |
| 3 | **弓手一弓多箭**：一次拉弓周期（前摇+后摇）结算 3 箭 | 远程结算走独立代码冷却时钟（约动画周期 1/3），与 Animator 播放周期脱节 | 远程出箭对齐 `Attack_Release`（或对应释放态）的 `normalizedTime` 可调比例窗口，单次动画周期只发射 1 支箭，冷却与 `AttackSpeed` 单源同步 |
| 4 | **法师前摇过长**：从下令到出手明显拖沓（秒级） | 攻击状态链（`Attack_Draw → Attack_Release`）过渡/触发节奏或法师执行器等待参数异常 | 诊断并收敛法师 `Draw → Release` 节奏与释放窗口，使下令到出手响应符合 `attackSpeed` 预期 |
| 5 | **【关键线索】自动攻击不播动画**：指定攻击出箭处于拉弓动画，自动攻击出箭仍 Idle | `CharacterBrain` / `UnitBrain` 存在“指定攻击（`CommandAttack`）”与“自动攻击（Aggro 索敌 / `RangedCombat.AutoAttack`）”两条路径，自动攻击路径绕过了攻击动画状态触发 | 统一两条攻击路径的出招入口与 Animator 触发契约，无论手动指定还是自动索敌攻击均进入完整攻击动画状态链 |
| 6 | **伤害飘字过小**：难以辨认 | `DamageNumber` 世界空间字体/缩放偏小 | 将伤害飘字尺寸放大 **3～4 倍**（保持清晰可读与生命周期稳定） |
| 06 遗留 | 玩家受击未进 `Hit_A`；战后停留 `Attack_Draw` | 玩家受击未触发 Animator `Hit`；战斗结束/目标死亡时未清理攻击状态回 `Locomotion` | 补齐玩家受击 `Hit` 触发与目标死亡/取消攻击时的动画收尾回落 |

### 1.3 非目标

- 不改敌方 `Skeleton_Warrior` 动画与 AI（已验证正常）。
- 不采用 `AnimationEvent` 作为主命中/出箭路径（严守决策 #4）。
- 不新建复杂 ScriptableObject 动作资产框架或重写全套状态机，只做契约对齐与 Inspector 可调窗口暴露。
- 不修镜头左键拖动、Q 技能、迷雾遮罩、宝箱、救援、传送或团灭重来。

---

## 2. 业务规则（提案）

| ID | 规则 |
|---|---|
| BR-01 | **移动参数全员驱动**：Knight、Archer、Mage 在 NavMesh 移动期间持续更新 Animator 移动参数（`SpeedX`/`SpeedZ`），停止移动后归零并稳定处于 `Locomotion`。 |
| BR-02 | **双攻击路径统一**：手动指定攻击（`CommandAttack`）与自动索敌攻击（Aggro 范围自动开火）必须走同一套出招状态与 Animator 触发逻辑，严禁自动攻击走“只扣血/只发投射物不播动画”的旁路。 |
| BR-03 | **动画时钟原则（决策 #4）**：近战命中（`SwordSwing`）与远程发射（`RangedCombat`：弓箭/法球）统一在 `layer 0` 轮询攻击释放状态的 `normalizedTime`，在 `[hitWindowStart, hitWindowEnd]`（Inspector 暴露 `[Range(0f, 1f)]` 可调字段）内且本周期尚未结算时触发 **1 次** 伤害/投射物；仅当 Animator 缺失或超时异常时走回退时钟。 |
| BR-04 | **攻速单源（决策 #5）**：`UnitStats.attackSpeed` 作为唯一攻速数据源驱动 Animator `"AttackSpeed"` 参数与攻击周期节奏，消除“一弓三箭”与多套计时器冲突。 |
| BR-05 | **收尾与受击对齐**：目标死亡或移动打断攻击后，Animator 必须在限定窗口内回到 `Locomotion`，不再卡死 `Attack_Draw`；玩家经 `UnitHealth.ApplyDamage` 受到非致命伤害时触发 `Hit` 进入 `Hit_A`。 |
| BR-06 | **伤害飘字放大**：`DamageNumber` 视觉尺寸放大至原尺寸的 3～4 倍（默认取 **3.5 倍**，Inspector 可调），不改变伤害数值与事件订阅链路。 |

---

## 3. 未决问题清单（随提案决策）

| ID | 未决问题 | 提案默认值（用户批准即采用；否则请指出） |
|---|---|---|
| Q-01 | 伤害飘字放大 3～4 倍放入本单还是拆独立小单？ | **并入本单（FND-02B）顺手完成**，默认放大倍数为原尺寸的 **3.5x**（在 `DamageNumber` 暴露可调倍率/字号）。 |
| Q-02 | 可调比例窗口字段的暴露位置？ | 直接暴露在现有攻击执行组件（`SwordSwing` 与 `RangedCombat`）Inspector 上（如 `hitNormalizedTime` / `fireNormalizedTime` `[Range(0f,1f)]` 及状态名字段），便于所有者在 Inspector 手动微调，不额外新建 SO 资产。 |
| Q-03 | 是否允许必要时微调玩家 OverrideController / BaseController 的过渡参数（如法师前摇过渡或战后回 Locomotion 条件）？ | **仅在阶段 1 只读核查证实纯代码无法消除法师秒级前摇或 `Attack_Draw` 卡死时**，才将对应 `.controller` 列入阶段 1 影响面清单并报用户确认；否则优先只改脚本契约。 |

---

## 4. 影响面预估（非最终编辑清单，阶段 1 只读确认后冻结）

| 候选路径/符号 | 预估变化 | 边界 |
|---|---|---|
| `Assets/_Project/Scripts/Units/UnitAnimatorSpeed.cs` / `UnitBrain.cs` / `CharacterBrain.cs` | 修复三角色移动参数驱动、统一手动/自动攻击触发路径、补受击与战后收尾触发 | 核心候选；阶段 1 须列出精确方法签名与调用链 |
| `Assets/_Project/Scripts/Combat/SwordSwing.cs` | `attackLayer` 修正为 `0`，状态名对齐 `Attack_Draw`/`Attack_Release`，命中对齐 `normalizedTime` 可调窗口 | 核心候选；保持现有 `DamageInfo` 入口不变 |
| `Assets/_Project/Scripts/Combat/RangedCombat.cs` | 统一自动攻击与手动攻击动画触发，发射时机对齐 `normalizedTime` 可调窗口（一周期一箭/一球），收敛法师前摇节奏 | 核心候选 |
| `Assets/_Project/Scripts/Combat/DamageNumber.cs`（或对应预制体） | 伤害飘字放大 3.5 倍（3～4 倍区间） | 顺手项；阶段 1 核实是改脚本默认缩放/字号还是预制体 |
| 玩家 Animator / OverrideController | 默认只读核查状态名、参数名与过渡条件 | 若需改 `.controller` 必须在阶段 1 单列并获用户确认 |
| `Assets/_Project/Tests/FND02B/{Editor,Runtime}/` | 新增本单 TDD 自动化验收脚本与 `.meta`（`.csproj` 随测纳入） | 唯一入口 `FND02B_BatchAcceptance.Run()` |
| `Docs/QA/Evidence/FND-02B/`、`Docs/Handoff/REPORT_FND-02B.md` | red/green 证据、人工目检表与报告 | 不修改旧 FND-00B / FND-02A 证据 |

**Unity 专属警告核查项（阶段 1 必查）**：
- Animator 参数名/状态名字符串（`" locmotion"` 前导空格、`Attack_Draw`、`Attack_Release`、`Hit_A`、`SpeedX`/`SpeedZ`/`AttackSpeed`/`Attack`/`Hit`/`Interrupt`）；
- 场景与预制体 GUID 引用完整性。

---

## 5. TDD 与自动化验收标准（预览）

1. **顺序**：阶段 1 只读影响面确认 → 用户确认并关闭 Unity → 仅写测试跑有效 red → 最小实施 → 同一入口连续跑 green（涉及动画时钟与周期同步，按诊断纪律连续运行 **N=3 次** 一致通过）。
2. **核心断言**：
   - `Locomotion.AllPlayersMoveParam`：Knight / Archer / Mage 移动期间 `SpeedX/SpeedZ` 随速度非零更新，停下后归零。
   - `Attack.DualPathUnified`：**指定攻击**与**自动攻击**两条路径均触发进入 `Attack_Draw / Attack_Release`，不存在 Idle 态直接出伤/发箭。
   - `Clock.MeleeNormalizedWindow`：骑士命中发生在 `Attack_Release` 的可调 `normalizedTime` 窗口内，单次挥砍周期仅结算 1 次伤害。
   - `Clock.RangedOneShotPerCycle`：弓手与法师在单个拉弓/施法动画周期内仅发射 1 支投射物（消除一弓三箭），出射点位于配置的 `normalizedTime` 窗口内，法师从下令到首次出射耗时处于合理攻速周期内。
   - `Animation.PostCombatAndHit`：战斗结束后回落 `Locomotion`（不卡 `Attack_Draw`）；玩家受非致命伤害进入 `Hit_A`。
   - `UI.DamageNumberScale`：生成的 `DamageNumber` 实际字号/缩放为原基线的 3～4 倍。
   - `Regression.FND02A`：自动开局、固定遭遇、真实接近与零新增 Console 错误保持通过。
3. **人工目检（≤3 项）**：
   - `V02B-1`：肉眼观察三角色移动不再滑步、自动攻击与手动攻击均播动作。
   - `V02B-2`：观察骑士挥砍出伤、弓手一弓一箭、法师出手节奏自然，且 Inspector 可调比例窗口生效。
   - `V02B-3`：确认伤害飘字放大 3～4 倍后清晰易读。

---

## 6. 批准闸门

请审阅本提案及 Q-01～Q-03 默认值。回复**“批准”**后，【大脑】将生成 `Docs/Handoff/TASK_FND-02B_玩家动画机整备与攻击时钟对齐.md` 并派发阶段 1 只读影响面确认。
