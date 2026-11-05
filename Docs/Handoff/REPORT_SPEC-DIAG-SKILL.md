# REPORT — SPEC-DIAG-SKILL（技能施放失效诊断与修复）

- 风险等级：**中风险**
- 执行者：【执行者】DeepSeek V4.1 Flash
- 回滚锚：`7cf874f7c5db448215b33a90fcad1d209f1fb781`（与工单一致）
- 执行模式：所有者睡前授权的长任务模式；Step 0→6 连续执行，未中途请求人工确认
- 本单未执行任何 git 写操作

---

## 1. 结果摘要

**根因一句话：** 按 Q 无反应的第一个失效节点是**输入层组件缺失**——`DungeonWorld.unity` 的 `Managers` 上没有 `SkillInputController`，`Update()` 从未执行；下游 Q 施法链本身完好（探针独立跑通：Cast=true、蓝-20、CD=6、靶掉血 15、0.137s 进入 Bash），因此**不是技能数据/动画/指示器/NavMesh 问题**。

**是否修复：** 未修复（根因落在 `.unity` 资产，按工单 §6 只出清单、不代改）。
**阻塞项（两个资产缺陷，需所有者批准后执行）：**
1. **A（主因，Q）**：`DungeonWorld.unity` 的 `Managers` 缺 `SkillInputController`；精确清单见 `Docs/QA/Evidence/DIAG-SKILL/04_资产改动清单.md` §A。
2. **B（次生根因，W，本单新发现）**：`W_Slash_Effect.asset` 运行期解析为 `BashEffect`（不是 `HeavySlashEffect`），导致 `Cast(1,null)` 返回 false 且白扣 30 蓝；见 `04_资产改动清单.md` §B。工单 §2.2 对 W 效果链的结论与运行期证据不符。

**测试防线：** 已把技能路径并入 `SPEC01` 永久回归套件（断言总数 8 → 10）。当前 `SPEC01` 稳定为 **8 pass / 2 fail / 0 blocked / 0 infra，exit=2**；两条 fail 分别是 `Skill.InputWired`（已知资产缺陷 A）与 `Skill.CastSmoke`（已知资产缺陷 B）。**`exit=2` 是已知资产缺陷，不是测试失败。**

**FND-00B-06 Animation.Hit：** 按工单 §6.2 复核后**"定案不成立"**——red 基线 `preState` 已经是 `Locomotion`、Grounded=true、速度参数=0，`Hit_A` 仍不触发。真实原因是夹具在 `Start()` 前禁用 `CharacterBrain`，导致其 `OnHit` 订阅从未建立，生产唯一 `SetTrigger("Hit")` 调用源不存在。按规则**未修改** `FND00B06_ProbeRunner.cs`，未做 green N=3。

---

## 2. 证据锚点

| 结论 | 断言 ID | 证据位置 | 关键原始值 |
|---|---|---|---|
| 场景无 `SkillInputController` | `Diag.Scene.SkillInputController` | `DIAG_Assertions.jsonl`（run1/2/3）；`DIAG_probe.log` | count=0（期望 1），等待 3s 复核；`product_fail` |
| 指示器是孤儿组件 | `Diag.Scene.Indicator` | 同上 | count=1、`mat=MonsterRing`、`hasConsumer=false`；`product_fail` |
| 第一退出点 | `Diag.Input.FirstExit` | 同上 | `MISSING_CONTROLLER`（pass） |
| 槽位完好 | `Diag.Prefab.Slots` | 同上 | 4 槽 = `Q_Bash / W_HeavySlash / <empty> / <empty>`（pass） |
| Q 下游链可独立跑通 | `Diag.CastChain.Q` | 同上；`DIAG_Summary*.json` | Cast=true；mana 100→80（-20）；CD=6；靶 1000→985（=15，=GetRawDamage）；0.1376~0.1384s 进入 `Bash`；pass |
| W 下游链有独立资产缺陷 | `Diag.CastChain.W` | 同上 | `effectType=BashEffect`；Cast=false；mana 100→70（-30，白扣）；CD=0；`_timer=-1`；未进 `Skill`；product_fail |
| 空槽契约 | `Diag.CastChain.EmptySlots` | 同上 | Cast(2/3)=false；蓝不变；无异常；pass |
| 探针结论 | `DIAG_Summary.json`（`conclusion`） | 同上 | "Q 下游施法链可独立跑通；失效点在输入层，第一个未执行节点 = SkillInputController.Update()；W 另有一个独立资产缺陷（W_Slash_Effect 解析为 BashEffect），不属输入层" |
| FND red | `Animation.Hit` | `fnd00b06_red_Assertions.jsonl` / `FND00B06_red_run1.log` | `preState=Locomotion`；hitEvents=1；hp 100→95；`hitStateObserved=false`；exit=2 |

**运行/退出码**
- DIAG：run1/run2/run3 均 `pass=4 fail=3 blocked=0 infra=0 observed=0`，**exit=2**（fail 中含 2 条已知资产缺陷 + W 的一条已知资产缺陷；均为预期复现）。
- SPEC01：run1/run2/run3 均 `pass=8 fail=2 blocked=0 infra=0 observed=0`，**exit=2**。
- FND00B06 red：`pass=11 / fail=1 / blocked=0 / infra=0 / observed=2`，**exit=2**（唯一 fail 即 `Animation.Hit`）。

**日志**（禁止整文件读取，仅用 `Select-String -Context 2` 定向提取过）：
- `Docs/QA/Evidence/DIAG-SKILL/DIAG_probe.log`（canonical，run1）
- `Docs/QA/Evidence/DIAG-SKILL/DIAG_probe_run2.log`、`DIAG_probe_run3.log`
- `Docs/QA/Evidence/DIAG-SKILL/SPEC01_green_run1.log` / `run2` / `run3`
- `Docs/QA/Evidence/DIAG-SKILL/FND00B06_red_run1.log`

---

## 3. 改动文件清单

### 3.1 新增（测试代码，含 `.meta`）

| 文件 | 说明 |
|---|---|
| `Assets/_Project/Tests/DIAG.meta` | 目录 meta |
| `Assets/_Project/Tests/DIAG/Editor.meta` | 目录 meta |
| `Assets/_Project/Tests/DIAG/Runtime.meta` | 目录 meta |
| `Assets/_Project/Tests/DIAG/Editor/DIAG_BatchAcceptance.cs` + `.meta` | batchmode 唯一入口 `DIAG_BatchAcceptance.Run`；SessionState 接续与退出码 |
| `Assets/_Project/Tests/DIAG/Runtime/DIAG_ProbeRunner.cs` + `.meta` | 一次性探针：场景普查 → 指示器消费者 → 第一退出点 → 预制体槽位 → Q/W/空槽 |
| `Assets/_Project/Tests/DIAG/Runtime/DIAG_Evidence.cs` + `.meta` | 极简 JSONL/Summary/ExitCode 证据写入（复用 `SPEC01_Json`） |
| `Assets/_Project/Tests/DIAG/Runtime/SkillCastFixture.cs` + `.meta` | 施法夹具（DIAG 与 SPEC01 共用）：非激活父物体承接实例、禁用 Agent/Brain 后激活、相位准备、逐槽施放采样 |

### 3.2 修改（测试代码，仅追加）

| 文件 | 本单改动 |
|---|---|
| `Assets/_Project/Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs` | **仅追加 2 行断言注册 + 162 行两个断言方法**；无删除、无重排。该文件同时含所有者既有的 SPEC-01-CLEANUP 未提交改动，本单未触碰那部分。 |

**FND00B06_ProbeRunner.cs：本单 0 改动。** 工作区中该文件的既有改动全部来自 SPEC-01-CLEANUP。

### 3.3 文档 / 证据（新增）

- `Docs/QA/Evidence/DIAG-SKILL/00_前置确认.txt`
- `Docs/QA/Evidence/DIAG-SKILL/01_静态路径清单.md`
- `Docs/QA/Evidence/DIAG-SKILL/02_资产核对.md`
- `Docs/QA/Evidence/DIAG-SKILL/04_资产改动清单.md`
- `Docs/QA/Evidence/DIAG-SKILL/05_Animation_Hit_red_green.md`
- `Docs/QA/Evidence/DIAG-SKILL/` 下全部 log / jsonl / json / txt（见 §2）
- `Docs/Handoff/REPORT_SPEC-DIAG-SKILL.md`（本文件）

### 3.4 文档 / 证据（被运行更新的既有文件）

- `Docs/QA/Evidence/SPEC-01/SPEC01_Assertions.jsonl`
- `Docs/QA/Evidence/SPEC-01/SPEC01_ExitCode_run1.txt`、`run2`、`run3`
- `Docs/QA/Evidence/SPEC-01/SPEC01_Runs.jsonl`
- `Docs/QA/Evidence/SPEC-01/SPEC01_Summary_run1.json`、`run2`、`run3`
- `Docs/QA/Evidence/FND-00B-06/06_Assertions.jsonl`
- `Docs/QA/Evidence/FND-00B-06/06_Summary_run1.json`

> 这些是测试运行产物；`SPEC-01` 原证据被 10 条断言的新一轮运行覆盖（属于本单预期）。

### 3.5 `SPEC01_ProbeRunner.cs` 本单 hunk 的 before/after

**Hunk 1（Start 断言序列，`+221,+222`）**

before：
```csharp
        yield return RunSafe("UI.DamageNumberScale", AssertDamageNumber());
        yield return RunSafe("UI.SpawnAnchor", AssertSpawnAnchor());

        RecordRegressionFinal();
```
after：
```csharp
        yield return RunSafe("UI.DamageNumberScale", AssertDamageNumber());
        yield return RunSafe("UI.SpawnAnchor", AssertSpawnAnchor());
        yield return RunSafe("Skill.CastSmoke", AssertSkillCastSmoke());
        yield return RunSafe("Skill.InputWired", AssertSkillInputWired());

        RecordRegressionFinal();
```

**Hunk 2（新方法块，`+1187,162`）**

before（原 `AssertSpawnAnchor` 尾部 + 回归汇总横幅）：
```csharp
                 "fixture", "SPEC01_Target_Anchor（运行期内存 UnitHealth，showDamageNumber=true）"));
    }

    // ============================================================
    // 回归汇总
    // ============================================================
```
after（同一位置插入 162 行）：
```csharp
                 "fixture", "SPEC01_Target_Anchor（运行期内存 UnitHealth，showDamageNumber=true）"));
    }

    // ============================================================
    // 技能施法回归（SPEC-DIAG-SKILL）
    // ============================================================
    static readonly Vector3 SkillFixturePos = new Vector3(80f, 0f, 80f);

    /// <summary>
    /// Q/W/E/R 四槽直接经生产 SkillSystem.Cast 施放（不按键盘）。
    /// 夹具在运行期实例化 Knight_Unit 并销毁，不写场景；Q 走命中帧后校验靶掉血，
    /// W 以 MeleeCombat._timer 证明效果链已执行，E/R 复核"空槽=设计内静默"契约。
    /// </summary>
    IEnumerator AssertSkillCastSmoke()
    {
        // ……（完整实现见文件 1187-1348 行；核心：Create → Prepare → Cast(0,Q,"Bash") →
        //      EnsureLocomotion → Cast(1,W,"Skill") → Cast(2/3) → 断言 → Dispose）
    }

    /// <summary>
    /// 输入层看门狗：DungeonWorld 运行期必须恰好有 1 个 SkillInputController，
    /// 且 indicator 指向场景 SkillIndicator、skillKeys = Q/W/E/R。
    /// 当前场景缺组件 → 如实记 product_fail（不得改写为 blocked/observed）。
    /// </summary>
    IEnumerator AssertSkillInputWired()
    {
        // ……（完整实现见文件 1304-1347 行）
    }

    // ============================================================
    // 回归汇总
    // ============================================================
```

> 拆分提交提示：该文件相对 HEAD 的 diff 同时包含所有者既有改动。本单新增部分可用 `git add -p` 只挑：
> - `Start()` 中两行 `RunSafe("Skill.*")`；
> - `AssertSpawnAnchor` 结束 `}` 之后到 `// 回归汇总` 之前的整块（以 `// 技能施法回归（SPEC-DIAG-SKILL）` 开头、以 `AssertSkillInputWired` 的 `}` 结尾）。

---

## 4. 测试 / 检查结果

### 4.1 前置（Step 0）

`Docs/QA/Evidence/DIAG-SKILL/00_前置确认.txt`：
- `Get-Process -Name Unity` count = 0（PASS）
- `Test-Path Temp/UnityLockfile` = False（PASS）
- `D:\unity\2022.3.62f3c1\Editor\Unity.exe` 存在；`ProjectVersion.txt` = `2022.3.62f3c1 (1623fc0bbb97)`
- `git rev-parse HEAD` = `7cf874f7c5db448215b33a90fcad1d209f1fb781`
- 全程未执行 git 写操作。

**执行偏差（如实登记）**：第二次启动 DIAG 前的复查中 `Temp/UnityLockfile` 曾被检测为 True（上一次因编译错误 exit=1 留下的陈旧锁），但 `Get-Process -Name Unity` 计数为 0；Unity 正常启动并完成运行。此后每次运行前锁文件均为 False。此后的启动前置条件全部满足。

### 4.2 DIAG 探针（N=3）

| run | 断言 | pass | fail | blocked | infra | exit | error CS |
|---|---|---|---|---|---|---|---|
| 1 | 7 | 4 | 3 | 0 | 0 | 2 | 0 |
| 2 | 7 | 4 | 3 | 0 | 0 | 2 | 0 |
| 3 | 7 | 4 | 3 | 0 | 0 | 2 | 0 |

Q 链数值（三次一致）：
- Cast=true；`manaDelta=20`；`cdRemaining≈6`；`targetHp 1000→985`；`damageApplied=15=expectedDamage`；进入 `Bash` 用时 **0.1376 / 0.1381 / 0.1376 s**。
- `effectType=BashEffect`；夹具 Animator 配置：`enabled=true, cullingMode=AlwaysAnimate, updateMode=Normal, layerCount=1, controller=OC_Knight`（排除"剔除/未启用"假设）。

W（三次一致）：`effectType=BashEffect`，Cast=false，manaDelta=30，CD=0，`_timer=-1`，未进 Skill。

### 4.3 SPEC01 永久回归（N=3）

| run | 断言总数 | pass | fail | blocked | infra | exit | error CS |
|---|---|---|---|---|---|---|---|
| 1 | 10 | 8 | 2 | 0 | 0 | 2 | 0 |
| 2 | 10 | 8 | 2 | 0 | 0 | 2 | 0 |
| 3 | 10 | 8 | 2 | 0 | 0 | 2 | 0 |

- `Skill.CastSmoke` 三次均 **qPass=true / wPass=false / emptyPass=true**：
  - Q：Cast=true、蓝 100→80、CD=6、靶 1000→985（15=GetRawDamage）、Bash 用时 0.1375/0.1378/0.1378s；
  - W：`effectType=BashEffect`、Cast=false、白扣 30 蓝；
  - E/R：false、蓝不变、无异常。
- `Skill.InputWired` 三次均 `controllerCount=0` → product_fail。
- 其余 8 条（Locomotion / Regression.FND02A / Attack.DualPathUnified / Clock.MeleeNormalizedWindow / Clock.RangedOneShotPerCycle / Animation.PostCombatAndHit / UI.DamageNumberScale / UI.SpawnAnchor）三次全 pass。
- `Regression.FND02A` 的 Console 错误计数为 0：说明新夹具没有污染零错误断言。

**绿口径说明（与工单 §5 的差异）：**
- 工单预期"10 条仅 `Skill.InputWired` fail（fail=1）"。实测 **fail=2**，多出的是 `Skill.CastSmoke` 的 **W 资产缺陷 B**（详见 §1/§7）。
- 未删断言、未改写 status、未放宽阈值。`exit=2 是已知资产缺陷，不是测试失败。`
- 修好 A+B 后应达到 10/10、exit=0、Infra=0；本单未执行资产修改，故未出现该状态。

### 4.4 FND00B06 Animation.Hit（red）

| 字段 | 值 |
|---|---|
| preState | Locomotion |
| grounded / speedX / speedZ | true / 0 / 0 |
| hitEvents | 1 |
| hpBefore → hpAfter | 100 → 95 |
| finalAmount | 5 |
| hitStateObserved | false |
| pass | false（product_fail） |

本次 FND00B06 整轮：`pass=11 / fail=1 / blocked=0 / infra=0 / observed=2`，exit=2（`fnd00b06_red_Summary.json` / `fnd00b06_red_ExitCode.txt`）。

按 §6.2 判定"定案不成立"，停止 Step 6；未做 green N=3。

### 4.5 静态检查

- `error CS`：DIAG 与 SPEC01 各次运行均 0；无编译错误。
- 搜索 `Assets/Scenes/*.unity`、`Assets/_Project/Prefabs/*.prefab` 对 GUID `1838f28ef8538c44cbd56e6e42391d1c` 引用数 = 0。
- 未修改任何 `.unity/.prefab/.asset/.controller/.anim/.mat`（新增测试 `.meta` 除外）。

---

## 5. 实现原理（面向项目所有者）

### 5.1 为什么"组件丢失"会造成零报错、零反馈
`SkillInputController.Update()` 是唯一把 QWER 按键翻译成"进入瞄准 / 直接施法"的地方。组件不在场景里时：
- `Update()` 根本不会执行，所以 L40/L41/L112/L118 的所有早退分支都不会走到；
- `Awake()` 也不会执行，`SkillInputController.instance` 保持 null；
- 其余系统（`PlayerCommandController.cs:28`、`CursorManager.cs:52`）对 `instance` 只做 null 判断，不抛异常；
- `SkillIndicator` 只是被动组件，没有人调用 `Show()/UpdateVisual()`，所以屏幕上什么都不出现。
→ 结果是"按键无反应 + Console 干净"，与症状完全吻合。它不是某个逻辑分支静默失败，而是**入口整段代码不存在**。

### 5.2 为什么不用"运行期自挂组件"来修
运行期 `AddComponent<SkillInputController>()` 能让按键"看起来好了"，但会：
1. 掩盖资产丢失这一真实事实，下次场景再丢组件仍然无人知道；
2. 需要额外猜 `indicator` 引用与 `skillKeys` 数组，等于在代码里复制一份场景配置；
3. 资产修好后会变成双实例（静态 `instance` 被覆盖），引入更难查的输入竞争。
本单因此只加"看门狗断言"（`Skill.InputWired`），让资产缺陷一直可见，等资产侧修复。

### 5.3 为什么指示器与 NavMesh 无关
`SkillIndicator.UpdateVisual` 的落点是"相机射线与 `y=0` 平面求交"（`SkillIndicator.cs:61-69`），纯数学；求交失败回退到施法者位置。它不调用 `Physics.Raycast`、不查询 `NavMesh`、也没有吞异常的 try/catch。因此"NavMesh 烘焙失效导致指示器静默"这个假设不成立；本单用 `Diag.CastChain.Q` 也证明了施法链不依赖导航就能跑通（夹具显式禁用 NavMeshAgent）。

### 5.4 为什么测试用夹具而不是"直接按键"
- batchmode 下 `Input.GetKeyDown` 不可靠（没有真实输入设备/焦点），无法稳定复现"按 Q"；
- 回归测试要测的是**生产方法**：`SkillSystem.Cast`、`BashEffect`、`MeleeCombat`、`UnitHealth`、Animator 状态机，而不是 Unity 的输入系统；
- 夹具从 `Knight_Unit.prefab` 在内存实例化，结束后销毁，不写场景；同时用 `Skill.InputWired` 单独守"输入层是否接好"，把"输入接线"和"下游执行"拆成两条断言，失败时能一眼定位层级。

### 5.5 夹具的两个关键技巧（也是为了测试稳定）
1. **非激活父物体承接实例**：先 `holder.SetActive(false)` → `Instantiate(prefab, pos, rot, holder)` → 禁用 `CharacterBrain`/`NavMeshAgent` → 再激活。这样 NavMeshAgent 的 native Awake 不会在无 NavMesh 环境下刷 Console 错误（`Regression.FND02A` 的零错误口径不会被污染）。
2. **Bash 相位准备**：`Bash` 进入过渡是 `HasExitTime=1, ExitTime=0.732`，且实测要求状态"跨过"出口时刻才触发。夹具在起手前把 Locomotion 相位等到 `[0.65, 0.72]`，因此"0.5s 内进入 Bash"测的是产品状态机，而不是动画相位运气。实测用时 ~0.137s，三次稳定。

### 5.6 W 的独立根因（本单新发现，非输入层）
`SkillEffects.cs` 一个文件里定义了两个 ScriptableObject：`BashEffect`（第一个）与 `HeavySlashEffect`（第二个）。两个效果资产的 `m_Script` 都指向该文件的**主 MonoScript**（`fileID: 11500000`），运行期只能解析为第一个类 `BashEffect`。`m_EditorClassIdentifier: Assembly-CSharp::HeavySlashEffect` 只影响编辑器显示，不参与运行期类型解析。于是 W 槽实际执行的是"盾击"，`Cast(1,null)` 因 `BashEffect.CanCast` 要求目标而返回 false。修法需要"拆类到独立文件 + 重指资产"，属代码+资产联合改动，超出本单授权，已出清单。

### 5.7 FND00B06 "定案不成立"的原理
`Hit` 触发器不是伤害系统自动发的，而是 `CharacterBrain` 在 `Start()` 里订阅 `UnitHealth.OnHit` 后、由 `OnDamaged` 发出的（全仓唯一 `SetTrigger("Hit")`）。夹具在 Instantiate 后、`Start()` 前就 `brain.enabled = false`，订阅永远不会建立；所以无论 Animator 是否已经在 Locomotion，都不会进 `Hit_A`。red 证据里 `preState=Locomotion`、`grounded=true` 正好排除了工单假设的"非 Locomotion 态"。真正的修法是让 `Start()` 先跑一帧再禁用（或保持启用），不是改动画参数。

---

## 6. 受影响模块导航说明

**生产数据流（技能路径）：**

```
[键盘 QWER]
  → SkillInputController.Update()            // Assets/_Project/Scripts/Player/SkillInputController.cs
       · PrimarySkillSystem(): SelectionManager.instance.Primary → GetComponentInParent<SkillSystem>()
       · 非 quickCast: IsTargeting=true + SkillIndicator.Show()/UpdateVisual()
       · 左键点敌 → SkillSystem.Cast(index, target)
  → SkillSystem.Cast(index, target)          // Assets/_Project/Scripts/Units/SkillSystem.cs:30-49
       · GetSkill / effects 校验 → Cooldown.Ready → UnitMana.Consume → effect.CanCast → effect.Execute
  → SkillEffectBase 子类                     // Assets/_Project/Scripts/Combat/SkillEffects.cs
       · BashEffect: 协程 → 停 NavMeshAgent → CancelAttack → SetTrigger("Bash") → hitTime 后 TakeHit + 击退
       · HeavySlashEffect: sword.DoSkillAttack()
  → MeleeCombat                              // Assets/_Project/Scripts/Combat/MeleeCombat.cs
       · DoSkillAttack/DoAttack → 设置 _timer、SetTrigger("Skill"/"AttackMelee") → 命中窗口读动画 normalizedTime → TakeHit
  → UnitHealth.TakeHit/ApplyDamage          // Assets/_Project/Scripts/Units/UnitHealth.cs
       · 破防/部位倍率 → hp 扣减 → OnHit 事件 + EventBus
  → Animator（PlayerBase.controller）        // Assets/_Project/Art/Animations/PlayerBase.controller
       · Locomotion ↔ Bash / Skill / AttackMelee / Hit_A；参数 Grounded/SpeedX/SpeedZ/AttackSpeed
  → CharacterBrain.OnDamaged                 // Assets/_Project/Scripts/Units/CharacterBrain.cs:33-38
       · 全仓唯一 SetTrigger("Hit") 源（受击动画入口）
```

**测试入口（均为 batchmode `-executeMethod`）：**
| 入口 | 用途 | 证据目录 |
|---|---|---|
| `SPEC01_BatchAcceptance.Run` | 永久回归（10 条，含 `Skill.CastSmoke` / `Skill.InputWired`） | `Docs/QA/Evidence/SPEC-01/` |
| `DIAG_BatchAcceptance.Run` | 一次性诊断探针（场景普查 + 施法链逐节点） | `Docs/QA/Evidence/DIAG-SKILL/` |
| `FND00B06_BatchAcceptance.Run` | 普攻 + 角色动画（含 `Animation.Hit`） | `Docs/QA/Evidence/FND-00B-06/` |

**测试夹具**：`Assets/_Project/Tests/DIAG/Runtime/SkillCastFixture.cs`（DIAG 与 SPEC01 共用）；`SPEC01_ProbeRunner` 通过 `FindObjectsByType` 读场景；不写场景、不写生产资产。

---

## 7. 未决项

1. **资产 A（Q 主因，待所有者执行）**：`DungeonWorld.unity` 的 `Managers` 补 `SkillInputController` + `indicator` 引用 + `skillKeys=Q/W/E/R`。清单见 `04_资产改动清单.md` §A；修好后 `Skill.InputWired` 转绿。
2. **资产 B（W 次生根因，新发现，待所有者批准）**：`W_Slash_Effect.asset` 运行期被解析为 `BashEffect`；需要"拆 `HeavySlashEffect` 到独立 .cs + 重指资产"（代码+资产）。清单见 `04_资产改动清单.md` §B；修好后 `Skill.CastSmoke` 的 wPass 转绿，SPEC01 才会 10/10。
3. **`SkillSystem.Cast` 扣蓝早于 `CanCast`（代码缺陷，本单未改）**：`Assets/_Project/Scripts/Units/SkillSystem.cs:35-40`，`mana.Consume` 在 `effect.CanCast` 之前。W 的实测即证据：`castReturn=false` 但 `manaDelta=30`。建议后续工单把扣蓝移到 `CanCast` 之后（或失败回滚），本单只登记。
4. **开局无默认选中单位**：`SelectionManager.instance.Primary == null` 时 `SkillInputController` 走 `NO_PRIMARY` 静默返回；按 Q 依旧无反应。属 UX 缺口，本单不修。
5. **E/R 空槽静默无提示**：`SkillInputController.cs:117-118` 的 `continue` 无任何反馈；设计内静默，本单不修（`Skill.CastSmoke` 已固化为契约）。
6. **FND-00B-06 `Animation.Hit` 尚未修复**：工单 §6 的"夹具病（非 Locomotion 态）"定案不成立；真实原因是夹具在 `Start()` 前禁用 `CharacterBrain`，`OnHit` 订阅缺失。需下一张工单按 `05_Animation_Hit_red_green.md` §4 的两种方案之一修复夹具，并重跑 N=3。**本单未改该文件。**
7. **工单前提与仓库不符（信息性）**：仓库实际只有 `DungeonWorld.unity`、`SampleScene.unity` 两个场景（工单 §2.2 写 3 个）；W 的效果类与工单 §2.2 结论不符（已用运行期证据纠正）。
8. **`Docs/QA/Evidence/SPEC-01/` 与 `FND-00B-06/` 的既有证据被本单运行覆盖**：属测试产物更新；如需保留旧证据，请从版本库取回。

---

## 8. 醒来目检清单（人类，≤3 项，全部依赖 A 修复后）

1. 打开 `DungeonWorld`，确认 `Managers` 上已有 `SkillInputController`，`indicator` 指向场景 `SkillIndicator`，`skillKeys` 显示 `Q W E R`、`quickCast` 未勾选。
2. 进入 Play：左键点骑士（或按 `1`）先选中单位，按 `Q` 应出现脚下范围圈 + 指向线；左键点 2.5m 内敌人 → 骑士播 `Bash`、敌人掉血并被击退；点空地取消。
3. 按 `W`：修好资产 B 之前仍会表现异常（无 `Skill` 动画、扣蓝不生效/无伤害），这是已登记缺陷，不是新问题。

---

## 9. 建议提交

> 本单未执行任何 git 写操作。以下是给所有者的建议；`Assets/` 与 `Docs/` 混合改动请按下面拆分。

### 9.1 前置：先处理所有者既有的 SPEC-01-CLEANUP 待提交改动
`SPEC01_ProbeRunner.cs`、`FND00B06_ProbeRunner.cs`、`Assembly-CSharp*.csproj`、`CURRENT.md` 等文件中已有 SPEC-01-CLEANUP 的未提交改动。请先按该任务自己的清单提交/处理，避免与本单 hunk 混在一个 commit。

### 9.2 提交 1（测试代码 + 新 meta）
```bash
git add Assets/_Project/Tests/DIAG Assets/_Project/Tests/DIAG.meta
git add -p Assets/_Project/Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs
#   只挑：RunSafe("Skill.CastSmoke"/"Skill.InputWired") 两行 + "技能施法回归（SPEC-DIAG-SKILL）"整块
```
建议信息：
```
test(skill): add SPEC01 cast smoke and input-wiring watchdog

为什么：技能施放路径此前完全没有回归覆盖，Q 失效时无自动看门狗、只能靠人工发现
```
（若 `git add -p` 不便，可先把该文件的本单 hunk 单独摘出到临时 patch，再与 CLEANUP 的改动分开暂存。）

### 9.3 提交 2（文档与证据）
```bash
git add Docs/QA/Evidence/DIAG-SKILL
git add Docs/QA/Evidence/SPEC-01
git add Docs/QA/Evidence/FND-00B-06
git add Docs/Handoff/REPORT_SPEC-DIAG-SKILL.md
```
建议信息：
```
docs(diag): record skill Q input-layer root cause and W effect asset defect

为什么：需要把"零报错零反馈"的定位证据、资产改动清单与 FND-00B-06 定案复核固化，供后续修复与验收
```

### 9.4 不要提交
- `Assembly-CSharp.csproj` / `Assembly-CSharp-Editor.csproj`（Unity 自动生成，本单未改）
- `Assets/_Project/Tests/FND00B/Runtime/FND00B06_ProbeRunner.cs`（本单未改；属 CLEANUP）
- `Docs/Handoff/CURRENT.md`、任何 `TASK_*`、任何既有 `REPORT_*`（除本文件）
- `Docs/QA/Evidence/CLEANUP/`（属 CLEANUP）
- 本单未修改任何 `.unity/.prefab/.asset/.controller`，无需也不应把资产放进以上 commit。

---

## §10 后续：SPEC-DIAG-SKILL-2 已闭合

- 状态：**已完成**（2026-11-05）。red → 修 → green，`SPEC01` **10/10、fail=0、blocked=0、infra=0、exit=0**（green 连跑 3 次，另 1 次无 tag 运行用于还原 tracked 证据，同样 10/10、exit 0）。
- 本单关闭了本文 §5.6 记录的 W 缺陷：`HeavySlashEffect` 拆到独立脚本 `Assets/_Project/Scripts/Combat/HeavySlashEffect.cs`，并把 `W_Slash_Effect.asset` 的 `m_Script` 重指到新脚本 GUID（其余字段未动）；DIAG 侧 `Diag.CastChain.W` 由 product_fail 转 pass（`effectType=HeavySlashEffect`、蓝 30、CD 6、anim=Skill）。
- 同单关闭 `SkillSystem.Cast` 的「失败施放白扣蓝」：改为 `CanCast` 全通过后才 `mana.Consume`；red 阶段实测 `manaDeltaFailed=20`，green 阶段为 0，Q 的蓝量/CD/伤害/Bash 用时与修复前逐项一致。
- 资产 A（场景 `SkillInputController`）由所有者补齐后，`Skill.InputWired` 在 red 与 green 中均为 pass。
- 证据：`Docs/QA/Evidence/DIAG-SKILL/07_*`（`07_前置确认_SKILL2.txt`、red 1 次、green run1–3、run4 还原、DIAG 1 次、`07_汇总_red_green.md`）。
- 详细报告：`Docs/Handoff/REPORT_SPEC-DIAG-SKILL-2.md`。
- 本节只记录状态与证据指针，未改动本文任何既有结论。
