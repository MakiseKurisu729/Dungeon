# REPORT — SPEC-DIAG-SKILL-2：W 效果拆分 + 扣蓝顺序 + 夹具归位

- 工单：`Docs/Handoff/TASK_SPEC-DIAG-SKILL-2.md`（中风险）
- 回滚锚 / 运行 head：`7cf874f7c5db448215b33a90fcad1d209f1fb781`
- 环境：Unity 2022.3.62f3c1（Windows，batchmode），执行者全程未打开编辑器
- 结论：**完成**（red → 修 → green N=3，SPEC01 10/10、fail=0、exit=0）

---

## 1. 结果摘要

| 阶段 | 动作 | pass/fail/blocked/infra | exit | 证据 |
|---|---|---|---|---|
| Step 0 | 前置自证 | — | — | `Docs/QA/Evidence/DIAG-SKILL/07_前置确认_SKILL2.txt` |
| Step 1 | 只改测试（夹具迁移 + CastSmoke 新口径），生产零改动 | — | — | 见 §3 |
| Step 2 | **red**（修复前跑） | 9 / **1** / 0 / 0 | **2** | `SPEC01_skill2_red.log`、`07_Assertions_red.jsonl` |
| Step 3 | 改生产 3 文件 + 1 资产 m_Script 重指 | — | — | 见 §2 |
| Step 4 | **green N=3** | 10 / 0 / 0 / 0 ×3 | **0** ×3 | `SPEC01_skill2_green_run1..3.log`、`07_Assertions_green_run1..3.jsonl` |
| Step 4 附 | 还原 tracked 证据（无 tag 再跑一次） | 10 / 0 / 0 / 0 | 0 | `SPEC01_skill2_green_run4_restore.log` |
| Step 5 | DIAG 回归 | 6 / 1 / 0 / 0 | 2（1 项为 DIAG 陈旧期望，见 §6） | `DIAG_skill2_run.log`、`07_DIAG_Assertions_skill2.jsonl` |

red 的唯一红项正是 `Skill.CastSmoke`，且红的两条子判定与工单预期完全一致：

- `failedNoManaPass=false`、`manaDeltaFailed=20` → 失败施放白扣 20 蓝；
- `wEffectType=BashEffect` → W 被解析成盾击。

`Skill.InputWired` 在 red 即为 **pass**（`controllerCount=1`、`indicator=SkillIndicator`、`skillKeys=Q,W,E,R`），说明资产 A 已生效，未触发熔断。
`grep "error CS"` 在 6 份日志中计数均为 **0**。

## 2. 生产改动（逐段 before / after）

### 2.1 `Assets/_Project/Scripts/Units/SkillSystem.cs`

```csharp
// before
if (!_cds[index].Ready) { GameData.SetMessage($"{s.skillName} 冷却中"); return false; }
if (mana == null || !mana.Consume(s.manaCost)) { GameData.SetMessage("蓝量不足"); return false; }

foreach (var eff in s.effects)
{
    if (!eff.CanCast(this, target)) return false;
}
foreach (var eff in s.effects)
{
    eff.Execute(this, target);
}

// after
if (!_cds[index].Ready) { GameData.SetMessage($"{s.skillName} 冷却中"); return false; }
if (mana == null) { GameData.SetMessage("蓝量不足"); return false; }

// 先验效果可释放（CanCast），再扣蓝：施放失败不产生任何消耗，也不写冷却。
foreach (var eff in s.effects)
{
    if (!eff.CanCast(this, target)) return false;
}
if (!mana.Consume(s.manaCost)) { GameData.SetMessage("蓝量不足"); return false; }

foreach (var eff in s.effects)
{
    eff.Execute(this, target);
}
```

消息文案、返回值语义、冷却写入位置（仍只在成功路径）均未改。

### 2.2 `Assets/_Project/Scripts/Combat/HeavySlashEffect.cs`（新建）

```csharp
using UnityEngine;

/// <summary>强化斩：立即释放一次大威力近战攻击（数据全在 MeleeCombat，无额外参数）。</summary>
[CreateAssetMenu(menuName = "Dungeon/Skill Effects/Heavy Slash")]
public class HeavySlashEffect : SkillEffectBase
{
    public HeavySlashEffect() { needTarget = false; }

    public override bool CanCast(SkillSystem caster, Transform target) { return caster.sword != null; }

    public override void Execute(SkillSystem caster, Transform target)
    {
        caster.sword.DoSkillAttack();
    }
}
```

逐字迁移自 `SkillEffects.cs` 原 77–89 行，未改任何行为。配套 `.meta` 为新 GUID `303ec946df86449195e8131db3bceb2b`（`[guid]::NewGuid()` 生成，已在 `Assets/`、`ProjectSettings/`、`Packages/` 下全量 grep，冲突 0）。

### 2.3 `Assets/_Project/Scripts/Combat/SkillEffects.cs`

只删除了文件末尾的 `HeavySlashEffect` 类（原 77–89 行及其上方的空行）；`BashEffect` 一字未动，文件结尾仍为 `BashEffect` 的闭合 `}`（无尾随空行，与改前一致）。

### 2.4 `Assets/_Project/Date/Skills/W_Slash_Effect.asset`

仅 1 行 diff：

```yaml
-  m_Script: {fileID: 11500000, guid: b87811a1f9107cc43b6bc8cb79537544, type: 3}
+  m_Script: {fileID: 11500000, guid: 303ec946df86449195e8131db3bceb2b, type: 3}
```

`needTarget/range/totalTime/hitTime/stagger/knockDist/knockDuration/m_EditorClassIdentifier` 全部保持原值；`Q_Bash_Effect.asset`、`Q_Bash.asset`、`W_HeavySlash.asset`、`Knight_Unit.prefab`、场景、Animator、ProjectSettings、Packages 未触碰。

## 3. 测试改动

1. **夹具归位**：`Assets/_Project/Tests/DIAG/Runtime/SkillCastFixture.cs(.meta)` → `Assets/_Project/Tests/SPEC01/Runtime/`，GUID 保持 `4409a5b02ede476d82d3489a9689a8bb`，仅把类头注释「DIAG 与 SPEC01 共用」改为「SPEC01 常驻共用」。两个目录都没有 asmdef（同属 Assembly-CSharp、全局命名空间），`DIAG_ProbeRunner.cs` 未改且迁移后仍可编译、仍能跑（Step 5 已证）。
2. **`SPEC01_ProbeRunner.AssertSkillCastSmoke()` 新口径**：由 3 段（Q/W/E-R）扩为 4 段（失败施放 → Q → W → E/R），同一方法内顺序执行，全部并入同一条断言；`pass = failedNoManaPass && qPass && wPass && emptyPass`。新增子判定：
   - ① `Cast(0, null)`：`castReturn==false`、`manaDelta==0`、`CdRemaining(0)==0`、无异常；
   - ② Q 追加 `qEffectType=="BashEffect"`（原阈值未放宽：蓝 -20、CD≈6、伤害=GetRawDamage、≤0.5s 进 Bash）；
   - ③ W 追加 `wEffectType=="HeavySlashEffect"`（沿用蓝 -30、CD>0、`_timer>=0`、≤0.5s 进 Skill）；
   - ④ E/R 契约原样保留。
   - 新数据字段：`failedCastReturn / manaDeltaFailed / cdFailed / qEffectType / wEffectType / failedNoManaPass`（另保留原有全部字段与 `failed/Q/W/E-R` 明细）。
   - 未放宽阈值、未把 W 的动画等待改成 observed、未删除子判定、未在夹具里手动 `SetTrigger`。
3. 未改 `DIAG_ProbeRunner.cs`。

## 4. 实现原理（面向项目所有者）

**为什么一个 `.cs` 里放两个类，W 会解析成 `BashEffect`。**
Unity 里 1 个 `.cs` 文件只导入出 1 个 `MonoScript`（由 `.meta` 的 GUID 标识）。脚本资产的 `m_Script` 引用的是这个 `MonoScript`，运行期 `ScriptableObject` 实例的类型由 `MonoScript.GetClass()` 决定。当文件里有多个继承 `ScriptableObject`/`MonoBehaviour` 的类时，Unity 只能挑一个作为该 `MonoScript` 的主类——挑的是**文件里第一个**。`SkillEffects.cs` 里 `BashEffect` 在前、`HeavySlashEffect` 在后，于是 W 资产的 `m_Script` 实际拿到的是 `BashEffect`。资产里那行 `m_EditorClassIdentifier: Assembly-CSharp::HeavySlashEffect` 只是编辑器 Inspector 的显示提示，不参与运行期实例化，所以它"看起来是对的，跑起来是错的"。这也解释了 red 的两个现象同源：类型是 `BashEffect` → `CanCast(null)` 要求目标 → 返回 false。

**为什么拆文件 + 重指 `m_Script` 就够了。**
把 `HeavySlashEffect` 搬到同名文件后，该文件的 `MonoScript` 只有一个候选类，`GetClass()` 无歧义。再让 `W_Slash_Effect.asset` 的 `m_Script` 指向新文件的 GUID（`type: 3` 表示脚本资产），运行期实例化的就是 `HeavySlashEffect`。因为是把类**逐字搬家**（构造里 `needTarget=false`、`CanCast`、`Execute` 全同），只有"解析到哪个类"这一件事变了——这正是 W 需要变的那一件事，也是最不容易波及 Q 的最小改动面。

**为什么先 `CanCast` 再扣蓝。**
`Cast` 里两次 `foreach` 之间必须插一次状态变更（扣蓝）。旧顺序是"扣蓝 → `CanCast`"，一旦 `CanCast` 返回 false，`Cast` 返回 false 但蓝已经被扣走——调用方看到的是"施放失败"，实际却付出了资源，属于**非原子操作**。技能施放的正确语义是"先全部校验、后一次性生效"：校验阶段只读（`_cds[index].Ready`、`mana`、每个效果的 `CanCast`），生效阶段才写（扣蓝、`Execute`、写冷却）。改成"校验全通过 → 扣蓝 → 执行 → 写冷却"后，失败路径天然不留下任何副作用（不扣蓝、不写冷却），也不需要额外回滚代码。

**为什么"失败不扣蓝"必须由测试守护。**
这个缺陷不会抛异常、不会报错、也不影响成功路径的任何数字，肉眼与手动试玩几乎不可能发现（只是每次误触技能悄悄少 20 蓝）。它唯一的可见面就是"失败施放前后的蓝量差"，而这条只能被断言抓住。工单要求的顺序也保证了这条断言**不是恒真**：先让它在修复前的代码上跑红（实测 `manaDeltaFailed=20`），再改实现转绿。以后任何人把扣蓝往前挪，这条断言会立刻变红。

## 5. Bash 不回归的证据（Q 四项逐项对齐）

| 指标 | red（修复前） | green run1 | green run2 | green run3 |
|---|---|---|---|---|
| Q 蓝量变化 | 20 | 20 | 20 | 20 |
| Q CD 剩余 | 6 | 6 | 6 | 6 |
| Q 靶掉血 / 期望 | 15 / 15 | 15 / 15 | 15 / 15 | 15 / 15 |
| Q 进入 Bash 用时 | 0.1364 s | 0.1364 s | 0.1366 s | 0.1361 s |
| `qEffectType` | BashEffect | BashEffect | BashEffect | BashEffect |

DIAG 侧同样一致：`Diag.CastChain.Q` 在修复前 pass、修复后仍 pass（伤害 15/15、Bash 0.138 s；`Diag.CastChain.W` 由 product_fail 转 pass）。

## 6. 检查结果明细

- **SPEC01 green N=3**：三次均 `pass=10 fail=0 blocked=0 infra=0 observed=0`、`exit=0`；
  `qEffectType=BashEffect`、`wEffectType=HeavySlashEffect`、`failedNoManaPass=true` 三次一致；
  W 均 `castReturn=true`、`manaDelta=30`、`cd=6`、`swordTimer=0`、进入 Skill 0.0519/0.0520/0.0521 s。
- **DIAG**：`Diag.CastChain.W` → `castReturn=true、effectType=HeavySlashEffect、manaDelta=30、cd=6、anim=Skill`；
  `Diag.CastChain.EmptySlots`、`Diag.Scene.SkillInputController`、`Diag.Scene.Indicator`、`Diag.Prefab.Slots` 全 pass；
  **唯一红项 `Diag.Input.FirstExit`**：该探针把"场景缺 `SkillInputController`"写成了期望值（`expected={"expected":"MISSING_CONTROLLER"}`），资产 A 补齐后实际 `controllerCount=1`，于是红。这是 DIAG 探针期望陈旧，**不是产品回归**；工单禁止改 `DIAG_ProbeRunner.cs`，按实际记录，列入遗留项。

## 7. 改动文件清单

生产：
- `Assets/_Project/Scripts/Units/SkillSystem.cs`（改）
- `Assets/_Project/Scripts/Combat/HeavySlashEffect.cs`（新建）+ `HeavySlashEffect.cs.meta`（新建）

资产：
- `Assets/_Project/Date/Skills/W_Slash_Effect.asset`（仅 `m_Script` guid 一行）

测试：
- `Assets/_Project/Tests/DIAG/Runtime/SkillCastFixture.cs` + `.meta`：**移动**到 `Assets/_Project/Tests/SPEC01/Runtime/`（GUID 不变 `4409a5b0...`）
- `Assets/_Project/Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs`（仅 `AssertSkillCastSmoke` 一段：+44 / −14）

文档 / 证据：
- `Docs/QA/Evidence/DIAG-SKILL/07_*`（前置确认、red/green run1–3、run4 还原、DIAG、red→green 汇总表）
- `Docs/QA/Evidence/SPEC-01/SPEC01_Assertions.jsonl`、`SPEC01_Summary_run1.json`、`SPEC01_ExitCode_run1.txt`、`SPEC01_Runs.jsonl`：被 red 轮覆盖后已用无 tag 的 green run4 还原为 green 内容（10/10、exit 0），`SPEC01_Runs.jsonl` 追加了索引行
- `Docs/Handoff/REPORT_SPEC-DIAG-SKILL-2.md`（本文件）
- `Docs/Handoff/REPORT_SPEC-DIAG-SKILL.md`（末尾追加 §10，前文未改）

## 8. 受影响模块导航说明（技能链现状）

```
输入层   SkillInputController（场景组件；Q/W/E/R → 槽位号 0..3；indicator 指向场景 SkillIndicator）
   ↓ Cast(slot, target)
调度层   SkillSystem（MonoBehaviour，挂在 Knight_Unit 等玩家单位上）
         字段：mana(UnitMana) / sword(MeleeCombat) / skills[4](SkillData)
         流程：取槽 → 效果数组非空 → 冷却 Ready → mana 存在 → 每个效果 CanCast 全过
               → mana.Consume(manaCost) → 每个效果 Execute → 写冷却并 Trigger
         失败路径：任何一步 false 都不扣蓝、不写冷却，只 SetMessage
   ↓ 数据
数据层   SkillData（ScriptableObject，资产在 Assets/_Project/Date/Skills/）
         字段：skillName / manaCost / cooldown(Cooldown) / effects(SkillEffectBase[])
   ↓ 策略
效果层   SkillEffectBase（抽象）：needTarget / range / CanCast / Execute
         ├─ BashEffect（Assets/_Project/Scripts/Combat/SkillEffects.cs）→ Q；资产 Q_Bash_Effect.asset
         │    协程：停 NavMesh → CancelAttack → 面向目标 → Animator.SetTrigger("Bash")
         │          → 等 hitTime 结算（TakeHit + ForceHit 击退）→ 等 totalTime
         └─ HeavySlashEffect（Assets/_Project/Scripts/Combat/HeavySlashEffect.cs）→ W；资产 W_Slash_Effect.asset
              Execute → MeleeCombat.DoSkillAttack()（复用近战时钟，动画走 Skill 状态）
   ↓ 执行
战斗层   MeleeCombat（普攻/技能共用计时与命中窗） → UnitHealth.TakeHit → DamageDealtEvent → CombatFeedback/DamageNumber
   ↓ 表现
动画层   PlayerBase.controller（Locomotion / AttackMelee / Bash / Skill …）
```

关键点：**效果层现在是"一文件一效果类"**，新增技能只需新建效果类 + 资产，`SkillSystem` 零改动；`W_Slash_Effect.asset` 与 `Q_Bash_Effect.asset` 自此指向两个不同的 `MonoScript`，互不影响。

## 9. 遗留项

1. `Diag.Input.FirstExit` 期望陈旧（写死 `MISSING_CONTROLLER`），需另立 DIAG 单更新口径；本单按工单禁止改 DIAG 探针。
2. `SkillSystem.Cast` 的 XML 注释仍是旧的「验蓝验冷却 → 效果链」，与新顺序不完全一致。工单只授权移动扣蓝那一步，未授权改注释，故保留（建议后续小额文档修正）。
3. `W_Slash_Effect.asset` 里仍留有 `BashEffect` 的序列化字段（`totalTime/hitTime/stagger/knockDist/knockDuration`、`needTarget:1`）。`HeavySlashEffect` 不读这些字段，Unity 下次保存该资产时才会自然丢弃；工单明令禁止改动这些字段，本次未动。
4. `SkillCastFixture` 内部对象名仍带 `DIAG_` 前缀（如 `DIAG_SkillFixtureHolder`）。工单只授权改类头注释，未改这些字符串（避免超出授权面）。
5. 工作树另有大量与本单无关的既有未提交改动（`SwordSwing.cs` 删除、`FND02B/` 删除、`MeleeCombat.cs` 新增等），提交时务必按显式路径 `git add`。

## 10. 人类目检（工单 §7，2 项，待所有者执行）

1. `DungeonWorld`：选中骑士按 W 点敌人 → 应播 `Skill`（跳劈）动作并造成范围伤害，不再是盾击动作。
2. 按 Q 点 2.5m 内敌人 → 仍应播 `Bash`、掉血、击退（与修复前一致）。

## 11. 终验证据补正（SPEC-DIAG-SKILL-3）

> 工单：`Docs/Handoff/TASK_SPEC-DIAG-SKILL-3.md`（低风险，零代码 / 零资产改动）。
> 执行窗口：2026-11-05 12:48–12:55；HEAD `7cf874f7c5db448215b33a90fcad1d209f1fb781`（= 工单回滚锚）；Unity 2022.3.62f3c1。

### 11.1 旧 run2/run3 自相矛盾的成因与本轮处置

`Docs/QA/Evidence/SPEC-01/` 里的 `SPEC01_Summary_run2.json`、`run3.json` 与对应 `ExitCode_run2/3.txt` 是 11-05 凌晨 04:39/04:40 的**旧产物**（当时 8 pass / 2 fail、exit=2）。上一轮三次 green 全部落在默认 `SPEC01_RUN`（每次都等价 run1），互相覆盖同一批 run1 文件，最后又只还原了 run1 —— 于是目录内出现「run1 = 10/10、run2/3 = 8 pass 2 fail」的自相矛盾组合。日志能证明三次绿，但目录本身不能当 N=3 验收材料。

本轮改用套件**原生 N=3 机制**重跑：`SPEC01_MODE=green` + `SPEC01_RUN=1/2/3`，且**不设** `SPEC01_EVIDENCE_TAG`，路径完全由 `SPEC01_Evidence` 决定（run1 先删旧 `SPEC01_Assertions.jsonl` 再写，run2/3 追加、靠 `run` 字段区分）。旧的 run2/run3 文件已被本次运行原地覆盖，不再存在任何 8/2 残留。

取证据的时序：每次启动前确认 `Get-Process -Name Unity` = 0 且 `Temp/UnityLockfile` 不存在；启动后**轮询**直到进程归零 + 锁文件消失（连续 2 次采样成立）才读文件。Windows 上 `Unity.exe` 是启动器、`Start-Process` 会立即返回，其退出码不能代表 batchmode 结束，因此不采用。

### 11.2 三份 Summary / ExitCode（全部由套件写出，未手工编辑）

| run | startedAt → finishedAt | pass | fail | blocked | infraFail | observed | exitReason | ExitCode |
|---|---|---|---|---|---|---|---|---|
| 1 | 12:49:34.352 → 12:50:19.295 | 10 | 0 | 0 | 0 | 0 | completed | 0 |
| 2 | 12:51:05.818 → 12:51:50.758 | 10 | 0 | 0 | 0 | 0 | completed | 0 |
| 3 | 12:52:28.969 → 12:53:13.912 | 10 | 0 | 0 | 0 | 0 | completed | 0 |

- `Docs/QA/Evidence/SPEC-01/SPEC01_Summary_run1.json`
- `Docs/QA/Evidence/SPEC-01/SPEC01_Summary_run2.json`
- `Docs/QA/Evidence/SPEC-01/SPEC01_Summary_run3.json`
- `Docs/QA/Evidence/SPEC-01/SPEC01_ExitCode_run1.txt` / `run2.txt` / `run3.txt`（内容均为 `0`）

三次 `mode=green`、`scene=Assets/Scenes/DungeonWorld.unity`、`unityVersion=2022.3.62f3c1`、`head` 均为回滚锚提交；`randomSeed` 依次 20261106 / 20261107 / 20261108（按 run 派生）。

### 11.3 Assertions

`Docs/QA/Evidence/SPEC-01/SPEC01_Assertions.jsonl` = **30 行**，`run` 字段 1/2/3 各 10 条；`status` 全 `pass`（30/30），`kind` 全 `production`，**无** `product_fail` / `blocked` / `infra_fail`，`pass` 字段无 false。三次 run 的断言名集合完全一致（10 条）：

`Locomotion.AllPlayersMoveParam`、`Attack.DualPathUnified`、`Clock.MeleeNormalizedWindow`、`Clock.RangedOneShotPerCycle`、`Animation.PostCombatAndHit`、`UI.DamageNumberScale`、`UI.SpawnAnchor`、`Skill.CastSmoke`、`Skill.InputWired`、`Regression.FND02A`。

### 11.4 Skill.CastSmoke / Skill.InputWired 三 run 关键字段

`Skill.CastSmoke`（三次 run 数值逐项一致）：

| run | failedNoManaPass | manaDeltaFailed | cdFailed | failedCastReturn | qEffectType | wEffectType | qPass / wPass / emptyPass |
|---|---|---|---|---|---|---|---|
| 1 | true | 0 | 0 | false | BashEffect | HeavySlashEffect | true / true / true |
| 2 | true | 0 | 0 | false | BashEffect | HeavySlashEffect | true / true / true |
| 3 | true | 0 | 0 | false | BashEffect | HeavySlashEffect | true / true / true |

Q（Bash）三 run 均为：蓝 100 → 80（-20）、`cdRemaining=6/6`、靶 HP 1000 → 985、`damageApplied=15`=`expectedDamage=15`、`animStateAfter=Bash`（进入耗时 0.135–0.136s）；W（HeavySlash）三 run 均为：蓝 -30、`cdRemaining=6/6`、`animStateAfter=Skill`、`swordTimer=0`；失败施放 `failedException=null`、蓝量不变。

`Skill.InputWired` 三 run 均为：`pass=true`、**`controllerCount=1`**、`indicatorCount=1`、`indicator=SkillIndicator`、`skillKeys=Q,W,E,R`、`skillKeysOk=true`。

### 11.5 其它核验

- `SPEC01_Runs.jsonl` 末尾追加 3 行（run1/2/3，均 pass=10 / fail=0 / exitReason=completed），文件总 13 行。
- 三份运行日志 `Docs/QA/Evidence/DIAG-SKILL/SPEC01_skill2_final_run{1,2,3}.log`：`error CS` 计数 **0**（157548 / 157495 / 157505 字节）。
- `Assets/`、`ProjectSettings/`、`Packages/` 下 12:48 之后被写入的文件数 = **0**：本轮未改任何 `.cs` / `.asset` / `.unity` / `.prefab` / `.controller` / `.meta`（`git status` 里那些改动全部早于本次执行，最后写入时间 ≤ 12:37）。
- 自动核验脚本 `C:\Users\lll\AppData\Local\Temp\opencode\verify_spec01_n3.ps1`（只读，可重跑），输出 `Docs/QA/Evidence/DIAG-SKILL/09_验收_SPEC01_N3.txt`：24 条检查全 PASS，失败项计数 0。
- 前置确认：`Docs/QA/Evidence/DIAG-SKILL/08_前置确认_SPEC01_N3.txt`（含每 run 启动前的 procs=0 / lock=False 复核）。

### 11.6 结论

**`SPEC01 = 10/10 × 3、exit 0`** —— 三份 Summary/ExitCode 互相自洽，30 行断言无 fail/blocked/infra，`Skill.CastSmoke` 与 `Skill.InputWired` 三次逐项一致，可作为规范 N=3 验收材料。

若所有者决定本轮把刷新的 SPEC-01 证据一并入库，请显式暂存这 7 个文件（勿 `git add -A`）：`Docs/QA/Evidence/SPEC-01/SPEC01_Assertions.jsonl`、`SPEC01_Runs.jsonl`、`SPEC01_Summary_run1/2/3.json`、`SPEC01_ExitCode_run1/2/3.txt`。

## 12. 建议提交

> 工作树里已有大量与本案无关的未提交改动，**禁止 `git add -A`**；以下每条只按列出的路径暂存。三个代码/资产提交建议按 1→2→3 顺序落库，使每一步都可独立编译。

### 提交 1（生产：W 效果拆分 + 资产重指）

```powershell
git add Assets/_Project/Scripts/Combat/HeavySlashEffect.cs `
        Assets/_Project/Scripts/Combat/HeavySlashEffect.cs.meta `
        Assets/_Project/Scripts/Combat/SkillEffects.cs `
        Assets/_Project/Date/Skills/W_Slash_Effect.asset
```

```
fix(skill): 把 HeavySlashEffect 拆到独立脚本并重指 W 效果资产

一个 .cs 只对应一个 MonoScript，SkillEffects.cs 里的第一个类 BashEffect 抢占了解析，
导致 W 运行期实为盾击（effectType=BashEffect、Cast(1,null)=false）。
拆文件后 W_Slash_Effect.asset 的 m_Script 指向新脚本 GUID，行为逐字迁移、其余字段不动。
```

### 提交 2（生产：扣蓝顺序）

```powershell
git add Assets/_Project/Scripts/Units/SkillSystem.cs
```

```
fix(skill): 施放先验收再扣蓝，失败施放不再白扣蓝量

旧顺序先 mana.Consume 再逐效果 CanCast，CanCast 失败时蓝已被扣走且无回滚；
改为 CanCast 全通过后扣蓝，失败路径不扣蓝也不写冷却。
```

### 提交 3（测试：夹具归位 + CastSmoke 新口径）

```powershell
git add Assets/_Project/Tests/SPEC01/Runtime/SkillCastFixture.cs `
        Assets/_Project/Tests/SPEC01/Runtime/SkillCastFixture.cs.meta `
        Assets/_Project/Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs
```

```
test(skill): 夹具迁入 SPEC01 常驻目录并为 CastSmoke 增加失败施放与效果类型断言

SkillCastFixture 被 SPEC01 永久依赖却留在 DIAG 目录；断言补 failedNoManaPass 与
qEffectType/wEffectType，使"失败不扣蓝"和"W 解析为 HeavySlashEffect"可长期防回归。
```

### 提交 4（文档 / 证据）

```powershell
git add Docs/Handoff/REPORT_SPEC-DIAG-SKILL-2.md `
        Docs/Handoff/REPORT_SPEC-DIAG-SKILL.md `
        Docs/QA/Evidence/DIAG-SKILL
```

```
docs(qa): 记录 SPEC-DIAG-SKILL-2 的 red→green 证据与收口状态

补齐 07_* red/green/DIAG 证据与 REPORT，并在 SKILL 报告末尾追加 §10 指向本单。
```

### 不要提交

- `Assembly-CSharp*.csproj`（Unity 自动生成）
- `Assets/_Project/Scripts/Combat/MeleeCombat.cs`、`SwordSwing.cs`、`FND02B/`、`GameData.cs`、`HUD.cs`、`FND00B06_ProbeRunner.cs`、`Assets/Scenes/DungeonWorld.unity`（资产 A 及前序批次改动，属所有者/前序工单）
- `Docs/QA/Evidence/SPEC-01/` 下 `SPEC01_Assertions.jsonl`、`SPEC01_Summary_run1.json`、`SPEC01_ExitCode_run1.txt`、`SPEC01_Runs.jsonl`：本单 red 轮覆盖后已由 run4 还原为 green，属证据刷新，由所有者决定是否单独提交（若提交，请与前序 SPEC-01 证据一起）
