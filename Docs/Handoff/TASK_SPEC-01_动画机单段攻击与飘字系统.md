# TASK_SPEC-01 — 动画机单段攻击（近战）与伤害飘字系统重做

> 状态：`Ready`（依据所有者定案文档 `C:\Users\lll\Desktop\SPEC-01_动画机单段攻击与飘字系统.md` 生成；开工前置见 §2）
> 风险：**高风险**（触及命中触发路径与伤害入口管线；须外部顾问＋【大脑】双重终审）
> 类型：【重构】近战单段攻击状态接入＋恶意不息风格飘字系统
> 唯一报告：`Docs/Handoff/REPORT_SPEC-01.md`；唯一证据：`Docs/QA/Evidence/SPEC-01/`
> 裁决来源：SPEC-01 与事实基线/FND-02B 冲突处以 SPEC-01 为最新决策（混合树 2D→1D、骑士期望状态改为 `AttackMelee` 单段，触发器名＝状态名）。

---

## 1. 唯一目标与边界

1. **近战单段攻击**：骑士出招走新触发器 `AttackMelee` → 单段状态 `AttackMelee`（触发器名＝状态名，基座态），命中仍由该状态 `normalizedTime` 的 `activeStart/activeEnd` 窗口结算 1 次；弓/法保持 `Attack_Draw → Attack_Release` + `AttackRange` 触发器，行为与 FND-02B green 完全一致。
2. **飘字系统重做**：共享 Overlay 画布单例＋对象池（容量 32）＋出生随机上扇区跳出＋尺寸随伤害分档（min 1.5x/max 5x，钳制）＋回弹/淡出曲线（生命周期 ≈0.9s）＋`DamageElement` 枚举与颜色映射（先静态字典，不建 SO 资产）。
3. **能力确认新规（决策三，即刻生效）**：执行者对本单涉及的任何编辑器资产（`.controller`/.anim/.unity/.prefab/.asset/.overrideController）**只读**；上述资产管辖权归所有者手动维护（`PlayerBase.controller`、`OC_*` 系列、玩家动画剪辑）。执行者只写 `.cs`＋新增测试 `.cs`/`.meta`。若声称任何资产"必须改"，先输出精确文件路径/GUID/字段并列清单单独请批，未批不动。

**严格边界**：不修改敌方 `Skeleton_Warrior`/`Skeleton_Rogue`；`RangedCombat.cs` 生产逻辑不变；不用 `AnimationEvent`；不迁移目录、不装包、不升 Unity。

### 2. 开工前置（未全部满足不得派单）

1. **FND-02B 仍未收口**（V02B-1/2/3 目检＋外部顾问复核＋大脑终审待完成）；按 SPEC-01 执行顺序，本单与 FND-02B 一并收口，本单报告无须等待 FND-02B 先收口即可开工。
2. **所有者已完成基座/OC 手动改动（资产）**：
   - 增触发器 `AttackMelee`（Trigger）；
   - 增单段状态 `AttackMelee`，`Locomotion→AttackMelee`（HasExitTime=0，duration=**0.25**）、`AttackMelee→Locomotion`（HasExitTime=1，ExitTime=**0.75** 无条件）、打断回流沿用 `Interrupt` 方式；
   - 混合树改 **1D，参数 `SpeedZ`，阈值 0/4**（断言同步清单要求；基线冲突处以 SPEC-01 为准）；
   - `OC_Knight`：`AttackMelee ← Melee_1H_Attack_Chop`；`OC_Mage`：施法剪辑定夺并已记入 CURRENT；`OC_Archer`：确认 Draw/Release 映射无误。
3. **所有者确认三小项**（默认按 SPEC-01 值执行，改则开工前告知执行者）：触发器名 `AttackMelee`；尺寸 1.5x/5x 与池容量 32；法师施法 Clip 选型。
4. 阶段 1 影响面清单获用户批准；Unity 已关闭并自证（无 `Unity.exe`、无 `Temp/UnityLockfile`），结果写入 `Docs/QA/Evidence/SPEC-01/00_前置确认.txt`。

---

## 3. 阶段 1：只读影响面确认（执行者首步）

只读本工单与指定资产 YAML，禁止翻阅其他背景文档补全。输出以下清单后**立即停止**等待用户批准：

1. 仓库状态：分支、完整 HEAD、`git status --porcelain -uall` 原始输出；区分 FND-02A/02B 尚未提交基线与本单。
2. 只读根因定位：
   - `SwordSwing` 当前 `SetTrigger("Attack")` 的具体位置与调用链；骑士预制体 `SwordSwing` 上 `releaseStateName`/`attackTrigger` 是否已序列化（决定旧字段值是否会被实例覆盖）。
   - `DamageNumber`/`CombatFeedback`/`DamageInfo`（伤害入口所在文件）当前结构：现有调用点、`DamageInfo` 字段、旧 `.Show` 签名的全部引用（含 FND02A/FND02B 测试夹具中的引用，一并列入同步清单）。
   - 所有者手动改动后的 `PlayerBase.controller`/`OC_*` YAML：确认 `AttackMelee` 触发器、`AttackMelee` 单段状态、远程触发参数 `AttackRange`、两段式原样保留、混合树 1D+`SpeedZ` 实际都已就位（只读核对，如缺项即停止并报告，不得代改）。
3. 受影响文件与符号清单：
   - 计划修改/新增的 `.cs` 与 `.meta`；字符串引用核查（`"Attack"`/`"AttackMelee"`/`"Interrupt"`、状态名 `" Locomotion"`（FND-02B 已去前导空格，确认现行状态名）、`Attack_Draw`/`Attack_Release`/`Attack`、`SendMessage`/`Find`）；
   - 伤害入口 `DamageInfo` 加 `element`（`DamageElement`）字段的影响面（序列化兼容、Lint 调用点）；
   - 断言测试将如何取共享画布/池实例做不遮挡验证。
4. 阶段 2 完整 Unity 命令行。

## 4. 阶段 2：断言更新 → red → 实施 → green ×3

顺序硬性：

0. **前置闸门**（证据 `Docs/QA/Evidence/SPEC-01/00_前置确认.txt`）：
   - Unity 已关闭自证（无 `Unity.exe` 进程、无 `Temp/UnityLockfile`）；
   - **B1 反向裁决核验**：控制器保持参数 `AttackRange`；`Archer_Unit.prefab`/`Mage_Unit.prefab` 的 `RangedCombat.attackTrigger` 序列化值已由所有者改为 `AttackRange`；只读确认 `RangedCombat.cs` 无 `"Attack"` 硬编码、确实读取 `attackTrigger` 字段。任一项不符 → 停止并报告，不得进 red，不得改资产。

1. **先改 FND-02B 断言进 SPEC-01 口径，再跑 red**（期望此时因产品状态失败，`infra=0` 无编译错误）。**red 输出必须逐条列出 product_fail 的断言名（含 expected/actual），不得只给汇总计数**：

   | 断言 | 新期望 |
   |---|---|
   | Locomotion.AllPlayersMoveParam | 混合树 **1D**、参数 **SpeedZ**、阈值 0/4（Idle/Running） |
   | Attack.DualPathUnified | 骑士出伤时状态=**AttackMelee**（单段，状态名＝触发器名）；弓/法仍 Attack_Release，远程触发器为 **AttackRange** |
   | Clock.MeleeNormalizedWindow | 读 **AttackMelee** 态窗口，并断言触发器为 **AttackMelee** |
   | UI.DamageNumberScale | 共享 **Overlay 画布唯一**＋字号随伤害缩放（小、大各采样一次验证 min/max 钳制）＋池复用（连续 Show 不产生新画布、活跃实例数 ≤ 池容量） |
   | 其余（Ranged/PostCombat/Regression.FND02A） | 不变 |

2. **最小实施（完整规格，自包含）**，改动仅限：

**决策一-C（近战代码，仅 `SwordSwing.cs`）**：
- 新增字段 `public string attackTrigger = "AttackMelee";`，出招时 `SetTrigger(attackTrigger)` 替代原 `"Attack"`；
- 命中窗口读取的状态名：**新增 `public string hitStateName = "AttackMelee";`**（已定案，零资产改动即可生效；不复用 `releaseStateName`，避免两段式语义混淆与预制体静默覆盖），`ReadAnimProgress`/`IsAttackAnimPlaying` 纳入该字段；
- 命中窗口 `activeStart/activeEnd` 语义不变（仍为该状态 normalizedTime 的 [0,1] 比例窗口）；
- 字符串核查点：状态 `" Locomotion"` 为历史遗留（FND-02B 已去空格）；`RangedCombat.cs` 生产逻辑**不变**（继续两段式窗口），但其 `attackTrigger` 默认值与弓/法预制体序列化值均为 **`AttackRange`**（预制体值由所有者手动改，执行者只读）；`CharacterBrain/UnitBrain` 无需分支（触发器选择由各 combat 组件自理）。

**决策二（飘字重做，均纯脚本、零资产改动）**：

| 项 | 规格 |
|---|---|
| 涉及文件 | `DamageNumber.cs`（重写主体）、`CombatFeedback.cs`（传参）、`DamageInfo`（伤害入口所在文件加字段） |
| 渲染 | 单个共享 **Screen Space - Overlay** 画布（懒加载单例，全场景唯一）；飘字 = `Text`＋`CanvasGroup`，`raycastTarget = false`（永不遮挡、不挡点击） |
| 跟随语义 | **出生瞬间**取敌人位置（胸口高度 +1.2m）`WorldToScreenPoint` 定初始屏幕点；此后**不再跟随**，仅自身运动 |
| 跳出方向 | 出生时 **60°–120° 上扇区内随机角度**、随机初位移（40–70px 量级），同敌人连吃多段伤害不堆叠、互不遮挡 |
| 尺寸-伤害挂钩 | `scale = Clamp(Lerp(minScale, maxScale, (amount-minDmg)/(maxDmg-minDmg)), minScale, maxScale)`；默认 minScale=1.5x / maxScale=5x（相对基线 0.006）、minDmg=5 / maxDmg=60；常量集中且可调 |
| 动画曲线 | 弹出：0.12s 内 0.6 → 1.15（回弹感）→ 缓慢缩至 0.8；全程上飘＋`CanvasGroup.alpha` 淡出；总生命周期 ≈ 0.9s |
| 性能 | **对象池**：默认容量 32；`Show` 取池、结束回池（`SetActive(false)`）；池生命周期与共享画布一致；禁止每伤害 new Canvas |
| 类型扩展 | **方案 B**：新建 `enum DamageElement { Physical, Fire, Ice, Lightning, ... }`（先建枚举，后续类型陆续加）；`DamageInfo` 新增 `public DamageElement element = DamageElement.Physical;`；`WeaponData.DamageType`（Slash/Thrust/Blunt）保持武器类别轴**不动**；飘字颜色由 `DamageElement → Color` 映射表决定（先静态字典＋Inspector 可覆盖，不建 SO 资产） |
| 入口签名 | `DamageNumber.Show(Vector3 worldPos, float amount, DamageElement element)`；`CombatFeedback` 传 `info.finalAmount` 与 `info.element`，保留现有 `damageNumberDelay`（结算对齐感） |

3. **green N=3**：唯一入口 `SPEC01_BatchAcceptance.Run`（沿 FND-02B 的 Editor/Runtime 测试结构，新目录 `Assets/_Project/Tests/SPEC01/`，含 `.meta`），run1~run3 全部通过且关键数据一致；证据入 `Docs/QA/Evidence/SPEC-01/`（red 4 件＋green 3×4 件＋汇总 jsonl）。**green 阶段若 `FND00B06` 断言转红 → 立即中止、只报告，不得修改任何生产代码**（登记为范围外问题，待用户裁决）。

### 验收要点（客观预览）

- 骑士出招：出伤瞬间处于 `AttackMelee` 态、窗口内、单周期恰 1 次命中；
- 弓/法：`Attack_Draw→Attack_Release` 两段行为与 FND-02B green 数据一致；
- 飘字：1 个共享 Overlay Canvas；同一敌人连吃多段伤害不堆叠（随机 60°–120° 扇区、初始位移 40–70px）；大伤害 5x 钳制、小伤害 1.5x 钳制；连续 Show 实例数 ≤32。

## 5. REPORT 与收口

`Docs/Handoff/REPORT_SPEC-01.md` 须含：结果摘要/HEAD/命令/red 与 green run1~3/逐断言表/夹具边界/完整 diff/变更文件/未覆盖项/实现原理（讲清即止，不设字数上限）/模块导航（讲清即止，不设字数上限）/遗留问题/建议提交。明确登记 `hitStateName`（默认 `AttackMelee`）选型理由（已定案：不复用 `releaseStateName`）。执行者不得改 `CURRENT.md`、不得标 Done；本单与 FND-02B 一并经用户目检＋外部顾问复核＋大脑终审后收口，本 SPEC 随报告归档。

### 人工目检（执行者只留"待目检"项，严禁代填）

- `S01-1`：骑士单段出招与"刀落窗口"手感（Inspector 可调 `activeStart/activeEnd` 可视可调）；
- `S01-2`：飘字方向随机/大小分档/多段伤害不堆叠不遮挡；
- `S01-3`：弓/法原两段表现无回归。
