# REPORT — SPEC-DIAG-SKILL-1：FND-00B-06 Animation.Hit 夹具返工（按证据修正）

- 执行者：OpenCode＋DeepSeek V4.1 Flash（【执行者】）
- 日期：2026-11-05
- 风险等级：低风险（只改测试夹具，未动生产代码与任何资产）
- 回滚锚：`7cf874f7c5db448215b33a90fcad1d209f1fb781`
- 结论：**red → green 完成，N=3 连续通过（pass=12 / fail=0 / blocked=0 / infraFail=0 / exit=0）**

---

## 1. 结果摘要（red → green）

| 阶段 | `Animation.Hit` | 套件汇总 | exit |
|---|---|---|---|
| red（既有证据，本单未重跑） | `false / product_fail`，`preState=Locomotion`、`hitEvents=1`、`hitStateObserved=false` | pass=11, fail=1, blocked=0, infraFail=0, observed=2 | 2 |
| green run1 | `true / pass`，`preState=Locomotion`、`hitState=Hit_A`、`hitEvents=1`、HP 100→95（差 5） | pass=12, fail=0, blocked=0, infraFail=0, observed=2 | 0 |
| green run2 | 同上（逐字段一致） | 同上 | 0 |
| green run3 | 同上（逐字段一致） | 同上 | 0 |

- 14 条断言逐条对比 red 基线：**只有 `Animation.Hit` 从 `false/product_fail` 变为 `true/pass`**，其余 13 条 pass/status 完全相同，无回退；`Animation.PostCombatState` 仍为 `observed`（不计入通过/失败，与基线一致）。
- 夹具内**没有**手动 `Animator.SetTrigger("Hit")`：受击触发器依然只来自生产 `CharacterBrain.OnDamaged`。
- Step 0 前置自证通过：`Unity` 进程 = 0、`Temp/UnityLockfile` = False、编辑器 exe 存在（证据 `Docs/QA/Evidence/DIAG-SKILL/06_前置确认_Hit返工.txt`）。

## 2. 改动文件清单

| 文件 | 改动 |
|---|---|
| `Assets/_Project/Tests/FND00B/Runtime/FND00B06_ProbeRunner.cs` | 6 个最小 hunk：`HitAnimationFixture` 生命周期/前置、`CreateAnimationFixture` 去除即时禁用、新增 `HasHitSubscriber` |
| `Docs/QA/Evidence/DIAG-SKILL/06_前置确认_Hit返工.txt` | 新增：Step 0 自证 |
| `Docs/QA/Evidence/DIAG-SKILL/FND00B06_green_run1..3.log` | 新增：三次运行原始日志 |
| `Docs/QA/Evidence/DIAG-SKILL/06_{Assertions,Summary,ExitCode}_green_run1..3.*` | 新增：套件自身产物逐次快照 |
| `Docs/QA/Evidence/DIAG-SKILL/05_Animation_Hit_red_green.md` | 追加 §6 green 段（§1–§5 red 原文未改写），标题下加一行指向 §6 |
| `Docs/QA/Evidence/FND-00B-06/06_Assertions.jsonl`、`06_Summary_run1.json`、`06_ExitCode_run1.txt` | 被套件正常覆盖（run=1 先清空断言文件），属测试产物更新，如实登记 |
| `Docs/Handoff/REPORT_SPEC-DIAG-SKILL-1.md` | 新增：本报告 |

未触碰：`Assets/_Project/Scripts/**`、任何 `.unity/.prefab/.asset/.controller/.anim/.mat/.meta`、`ProjectSettings/`、`Packages/`、其他测试文件、`Docs/Handoff/CURRENT.md`、既有 `REPORT_*`/`TASK_*`。无任何 git 写操作。

## 3. 本单 hunk before / after

### 3.1 `HitAnimationFixture` 前段（改前，≤30 行）

```csharp
        yield return null;
        yield return null;

        Animator anim = _animFixtureGo.GetComponentInChildren<Animator>(true);
        _animFixtureHitEvents = 0;
        _animFixturePreHitHp = _animFixtureHealth.hp;
        _animFixturePostHitHp = _animFixtureHealth.hp;
        _animFixtureHitAmount = -1f;

        System.Action<DamageInfo> onHit = info => { ... };
        _animFixtureHealth.OnHit += onHit;

        Dictionary<string, object> preSample = AnimSample(anim, "hit_pre", null);
        string preState = (string)preSample["stateName"];

        float damage = 5f;
        _animFixtureHealth.ApplyDamage(damage, ...);
```

以及 `CreateAnimationFixture` 末尾（改前）：

```csharp
        var brain = _animFixtureGo.GetComponent<CharacterBrain>();
        if (brain != null) brain.enabled = false;
```

### 3.2 改后（≤30 行，节选关键部分）

```csharp
        // 夹具生命周期：先让 CharacterBrain/UnitBrain 正常走完 Awake/Start（生产侧在此订阅
        // UnitHealth.OnHit → OnDamaged），再禁用以免夹具自主移动/索敌。
        // 禁用组件不会撤销已建立的委托订阅，因此受击反馈仍走生产链路。
        CharacterBrain fixtureBrain = _animFixtureGo.GetComponent<CharacterBrain>();
        bool brainEnabledAtCreate = fixtureBrain != null && fixtureBrain.enabled;
        int brainSettleFrames = 0;
        for (int i = 0; i < 2; i++) { yield return null; brainSettleFrames++; }
        bool hitSubscribedAfterStart = HasHitSubscriber(_animFixtureHealth);
        if (fixtureBrain != null) fixtureBrain.enabled = false;

        Animator anim = _animFixtureGo.GetComponentInChildren<Animator>(true);

        // 受击前置：Animator 参数置为静止稳态（与 PlayerCommandController.Update 静止时同值）
        anim.SetBool("Grounded", true);
        anim.SetFloat("SpeedX", 0f);
        anim.SetFloat("SpeedZ", 0f);

        // 受击前置：轮询等待进入 Locomotion（上限 AnimWaitSeconds）；不成立则 blocked，不伪造通过
        while (Time.realtimeSinceStartup - locomotionWaitT0 < AnimWaitSeconds) {
            preState = ResolveState(anim);
            if (TrimState(preState) == "Locomotion") { locomotionReady = true; break; }
            locomotionWaitFrames++; yield return null;
        }
        if (!locomotionReady) { _ev.BlockedAssertion("6", "Animation.Hit", ..., "夹具未能进入 Locomotion（轮询 …s）", ...); yield break; }
```

`CreateAnimationFixture` 末尾改为注释说明 + 不再禁用；新增只读校验：

```csharp
    static bool HasHitSubscriber(UnitHealth h)
    {
        if (h == null) return false;
        FieldInfo f = PrivField(typeof(UnitHealth), "OnHit");
        if (f == null) return false;
        return f.GetValue(h) as Delegate != null;
    }
```

> 说明：`git diff` 中 `HasHitSubscriber` 所在 hunk 与 SPEC-01-CLEANUP 的 `FindSword(SwordSwing→MeleeCombat)` 相邻（同一 diff 块），但两处内容互不依赖；本单新增仅 `HasHitSubscriber` 本体。

## 4. 三次 green 的断言数据

三次 Summary（`06_Summary_green_runN.json`）完全一致：

```json
{"pass":12,"fail":0,"blocked":0,"infraFail":0,"observed":2,"exitReason":"completed","unityVersion":"2022.3.62f3c1"}
```

`Animation.Hit` actual（三次逐字段一致）：

```json
{"hitEvents":1,"hpBefore":100,"hpAfter":95,"finalAmount":5,
 "preState":"Locomotion","hitStateObserved":true,"hitState":"Hit_A","playerHpUnaffected":100}
```

`Animation.Hit` data 关键字段（三次一致）：

| 字段 | 值 | 含义 |
|---|---|---|
| `brainEnabledAtCreate` | `true` | 预制体大脑默认启用（`Knight_Unit.prefab` 该组件 `m_Enabled: 1`） |
| `brainSettleFrames` | `2` | 禁用前让 `Start()` 跑完的帧数 |
| `hitSubscribedAfterStart` | `true` | 只读反射确认 `UnitHealth.OnHit` 已挂上生产订阅者 |
| `brainDisabledBeforeDamage` | `true` | 施伤前夹具大脑已禁用（不自主移动） |
| `locomotionWaitFrames` / `locomotionWaitSeconds` | `0` / `0` | 受击前置 Locomotion 当帧即成立 |

逐条断言（三次一致）：
`Attack.*` 7 条全 pass；`Animation.ConfigMapping` observed；`Animation.Idle` pass；`Animation.PostCombatState` observed（pass=false，基线同）；`Animation.Move`、`Animation.Attack`、`Animation.Hit`、`Animation.DownedOrDeath` 全 pass。
`error CS` 计数：三次日志均为 0。

退出码：`06_ExitCode_green_runN.txt` 均为 `0`。

## 5. 实现原理（面向项目所有者）

**根因**：Unity 里组件被 `enabled = false` 时不会调用 `Start()`（`Awake` 已在 `Instantiate` 瞬间执行）。生产链路的受击动画完全依赖一条订阅：`CharacterBrain.Start()` 里 `_health.OnHit += OnDamaged`，而 `OnDamaged` 是**全仓唯一**调用 `Animator.SetTrigger("Hit")` 的地方。旧夹具在 `Instantiate` 后同一帧内就禁用了 `CharacterBrain`，`Start()` 永不执行 → `OnHit` 从未订阅 → 伤害照常结算（`hitEvents=1`、HP 100→95）、但动画触发器永不发出 → `Hit_A` 不进。red 证据里 `preState` 本来就是 `Locomotion`，所以旧工单"非 Locomotion 相位"的定案是错的。

**方案**：把"禁用时机"从 `Instantiate` 后立即，改为先让 `Awake/Start` 正常执行（等 2 帧），再禁用。禁用只停止 `Update/FixedUpdate` 等引擎消息，不会撤销 C# 委托订阅（`OnHit` 是 `UnitHealth` 上的委托字段，调用方直接走委托链；`OnDamaged` 内部也没有 `enabled` 判断），所以"禁用后受击动画仍正常、同时不自主索敌/移动"二者可以同时成立。

**为什么不是动画/资产问题**：red 阶段已逐条确认控制器存在 `Locomotion → Hit_A` 过渡（条件 `Hit` 触发器、`HasExitTime=0`）；同一 `UnitHealth.ApplyDamage` 入口在真实玩家（大脑启用）上能进 `Hit_A`；本单未改任何动画资产，仅改夹具启用时机，同一资产即转绿——唯一变量就是订阅建立时机。

**为什么加"证据化"字段**：等 2 帧是时序假设，容易再审时变成"猜"。因此额外用反射只读校验 `UnitHealth.OnHit` 是否已挂订阅（`hitSubscribedAfterStart`），并记录 `brainSettleFrames`、`brainDisabledBeforeDamage`，把"我以为 Start 跑了"变成可复核数据；受击前再把 `Grounded/SpeedX/SpeedZ` 置稳态并轮询 `Locomotion`，前置不成立时明确 `BlockedAssertion`，不伪造通过。

**为什么是 2 帧**：`Instantiate` 发生在协程续体（该帧 `Update` 之后），`Start` 在下一帧开头执行，1 帧理论上已够；取 2 帧留余量，代价仅为夹具多存活 2 帧（实测该窗口内 `agentVelocity=0`、无出招）。

## 6. 受影响模块导航说明

- **被测生产模块**：`UnitHealth`（伤害/事件入口，`OnHit`/`OnDeath`）→ `CharacterBrain`（玩家 Driver，`Start` 订阅受击、`OnDamaged` 发 `Hit` 触发器）→ Animator Controller（`Locomotion → Hit_A`）。数据流：`UnitHealth.ApplyDamage/TakeHit` → `OnHit` → `CharacterBrain.OnDamaged` → `Animator.SetTrigger("Hit")` → 状态机进 `Hit_A`。
- **测试侧入口**：`FND00B06_BatchAcceptance.Run`（Editor 批处理入口）→ `FND00B06_Boot`（域重载接续）→ `FND00B06_ProbeRunner`（Play 模式探针，第 4 项普攻 → 第 6 项动画）。本单只动 `GroupAnimation` 调用的两个夹具方法及其工具函数。
- **证据出口**：`FND00B06_Evidence` → `Docs/QA/Evidence/FND-00B-06/06_Assertions.jsonl` + `06_Summary_runN.json` + `06_ExitCode_runN.txt`；本次逐次快照到 `Docs/QA/Evidence/DIAG-SKILL/06_*_green_runN.*`。

## 7. 测试 / 检查结果

| 检查 | 结果 |
|---|---|
| 编译（三次运行日志 `error CS`） | 0 / 0 / 0 |
| `Animation.Hit` | pass / pass / pass |
| 套件 fail / blocked / infra | 0 / 0 / 0（三次） |
| 退出码 | 0 / 0 / 0 |
| 回退检查 | 与 red 基线逐条比对，仅 `Animation.Hit` 变化 |
| 作弊检查 | 夹具内无 `SetTrigger("Hit")`；触发器仅由生产 `OnDamaged` 发出（`hitSubscribedAfterStart=true` 佐证链路存在） |

自动化说明：本单验证方式为**可重复运行的批处理套件**（14 条断言 + JSON 证据 + 退出码），人工目检只保留工单 §5 的 1 项。

运维注意：Windows 上 `Unity.exe` 是启动器，`&` 调用会**立刻返回**而编辑器进程仍在后台跑。必须轮询 `Get-Process Unity` 归零后再取证据，否则会读到上一轮旧文件（本次首轮曾因此误拷贝一次旧 red 产物，已重新拷贝并核对时间戳与 `startedAt`）。

## 8. 目检清单（人类，1 项）

进入 `DungeonWorld` 后选中骑士，让敌人打一下：应播放受击动画并短暂进入 `Hit_A`，随后回到 Locomotion。

## 9. 遗留问题

1. 套件固定写 `run=1`（未设 `FND00B06_RUN`）：每次运行会清空 `06_Assertions.jsonl`、覆盖 `06_Summary_run1.json` / `06_ExitCode_run1.txt`。本单已逐次快照到 `DIAG-SKILL`，但若要套件自身保留 N 次原始数据，需改测试基础设施（超出本单授权）。
2. `Animation.PostCombatState` 仍为 `observed`（战后不回 Locomotion 的既有产品观测项），本单按"不修 Bug"未处理。
3. 夹具大脑在创建后短暂启用 2 帧；理论上有自主索敌可能，实测该窗口 `agentVelocity=0`、无出招副作用。若将来夹具落点靠近敌人导致污染，可考虑延后禁用前先 `CommandMove` 到安全点（本单未做，避免扩大改动）。
4. `FND00B06_ProbeRunner.cs` 仍同时含 `SPEC-01-CLEANUP` 的未提交改动，提交时必须拆分（见 §10）。

## 10. 建议提交

> 本单文件同时承载 SPEC-01-CLEANUP 的既有改动，**不可整文件一次提交**。建议两笔：

**提交 1（SPEC-01-CLEANUP 既有改动，非本单，仅提示拆分顺序）**

```bash
git add -p Assets/_Project/Tests/FND00B/Runtime/FND00B06_ProbeRunner.cs  # 先暂存 SPEC-01 的 hunk
```

**提交 2（本单，代码 hunk）**

```bash
git add -p Assets/_Project/Tests/FND00B/Runtime/FND00B06_ProbeRunner.cs
#  选中这 6 个 hunk：
#  @@ -1031,2 +1031,9 @@（settle 2 帧 + 订阅校验 + 禁用）
#  @@ -1034,0 +1042,39 @@（参数稳态 + Locomotion 前置 + blocked）
#  @@ -1049 +1095 @@（preState 复用）
#  @@ -1089,0 +1136,6 @@（证据字段）
#  @@ -1192,2 +1244,4 @@（CreateAnimationFixture 不再即时禁用）
#  @@ -1409 +1469,13 @@（HasHitSubscriber；注意此块还含 SPEC-01 的 FindSword 改名，勿重复暂存）
git commit -m "test(fnd00b06): 修复 Animation.Hit 夹具生命周期，恢复生产受击动画链路" \
           -m "为什么：夹具在 Start 前禁用 CharacterBrain，OnHit 订阅从未建立，Hit_A 永不触发"
```

**提交 3（文档与证据，纯 `Docs/`）**

```bash
git add Docs/QA/Evidence/DIAG-SKILL/ Docs/QA/Evidence/FND-00B-06/ Docs/Handoff/REPORT_SPEC-DIAG-SKILL-1.md
git commit -m "docs(qa): 登记 DIAG-SKILL-1 Animation.Hit red→green N=3 证据" \
           -m "为什么：red 定案被推翻后需要用可复核证据固化修复结论"
```

（本会话为子代理，未执行任何 git 写操作；以上命令供所有者执行。）
