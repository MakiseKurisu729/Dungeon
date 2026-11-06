# REPORT_SPEC-01-CLEANUP — SPEC-01 收口尾巴清理（长任务模式·三步夜间批）

> 执行者：DeepSeek V4.1 Flash｜执行日期：2026-11-05｜工单：`Docs/Handoff/TASK_SPEC-01-CLEANUP.md`
> 风险：低风险｜模式：长任务模式（范围锁死、不标 Done、不改 `CURRENT.md`）
> 回滚锚：`7cf874f7c5db448215b33a90fcad1d209f1fb781`｜实际 HEAD：`7cf874f7c5db448215b33a90fcad1d209f1fb781`（一致）
> 证据根：`Docs/QA/Evidence/CLEANUP/`

---

## 1. 结果摘要

| 步骤 | 状态 | 说明 |
|---|---|---|
| Step 0 前置与基线 | ✅ 完成 | Unity 已关闭、HEAD 一致；三套件基线各 1 轮已记录 |
| Step 1 `SwordSwing`→`MeleeCombat` | ✅ 完成（闸门按「无新增红」判定通过） | SPEC01 8/8 全绿；FND02B 红项集合与基线**逐条一致** |
| Step 2① FND00B06 硬编码 | ✅ 完成 | `mappingConsistent` 由 false→true、`mismatchCount` 2→0；红项集合与基线一致 |
| Step 2② FND02B 删过时 UI 断言 | ✅ 完成 | 断言 7→6，`UI.DamageNumberScale` 消失；**但「6/6 全绿」未达成**（见 §7 决策项 B） |
| Step 3 API 文档同步 | ✅ 完成 | 文档内 `SwordSwing` 归零；diff 仅该文件 |
| Step 4 收口产出 | ✅ 完成 | 本报告 + 醒来目检清单 + `Runs.jsonl` + diff 证据 |

**三处必须让所有者先看到的事实**

1. **FND02B 基线并非「只有 1 条红」**：实测基线 4 红，其中 3 条是「套件过时红」——`30a25171` 把骑士近战改为单段 `AttackMelee` 后，FND02B 仍按旧两段式 `Attack_Release` 与 2D 混合树断言。SPEC-01 是这些断言的继任套件且全绿。
2. 因此 **Step 2「FND02B 6/6 全绿」这个闸门在不动其他断言的前提下不可能达成**；删除 UI 断言后剩余 3 红与基线同名同因，**无新增红**。已按 P2 的「基线等价即视为有效」精神继续 Step 3，但**不宣称该闸门通过**。
3. **FND00B06 2 红与基线完全一致**（`Animation.Attack`、`Animation.Hit`），按 P2 列为醒来决策项，**未修复**。

---

## 2. Step 0 — 前置与基线

### 2.1 前置自证（`CLEANUP/step0/00_前置确认.txt`）
- `Get-Process Unity` → 0 个进程；`Temp/UnityLockfile` → 不存在。
- `git rev-parse HEAD` → `7cf874f7c5db448215b33a90fcad1d209f1fb781`（符合回滚锚）。
- `git status --porcelain -uall` 初始值：
  ```
   M Docs/Handoff/CURRENT.md
  ?? Docs/Handoff/TASK_SPEC-01-CLEANUP.md
  ```
  两者均为【大脑】本轮派单产物，非代码/资产改动，判定为**预期状态**，不构成 git 异常。
- Unity：`D:\Unity\2022.3.62f3c1\Editor\Unity.exe`（2022.3.62f3c1）。

### 2.2 既有证据保护措施
三套件证据目录 `Docs/QA/Evidence/{SPEC-01,FND-02B,FND-00B-06}` 均为**受版本控制**且属「绝不触碰」区。批处理运行会按套件自身逻辑覆盖同名证据文件，故执行前整目录镜像备份到 `CLEANUP/_backup/`，**每轮跑完立即还原**，并以 `git status -- Docs/QA/Evidence/{SPEC-01,FND-02B,FND-00B-06}` 为空核验。收口时三目录均**零差异**。

### 2.3 运行命令（三套件同一模板）
```powershell
# SPEC01
$env:SPEC01_MODE="green"; $env:SPEC01_RUN="1"
Start-Process "D:\Unity\2022.3.62f3c1\Editor\Unity.exe" -Wait -PassThru -NoNewWindow -ArgumentList @(
  "-batchmode","-projectPath","D:\unity\Dungeon",
  "-executeMethod","SPEC01_BatchAcceptance.Run",
  "-logFile","...\CLEANUP\stepN\SPEC01_Automation.log")
# FND02B：FND02B_MODE=green / FND02B_RUN=1 / FND02B_BatchAcceptance.Run
# FND00B06：FND00B06_RUN=1 / FND00B06_BatchAcceptance.Run
```

### 2.4 基线结果

| 套件 | 断言 | pass | fail | blocked | infra | observed | exit | 红项名 |
|---|---|---|---|---|---|---|---|---|
| SPEC01 | 8 | 8 | 0 | 0 | 0 | 0 | 0 | —（全绿） |
| FND02B | 7 | 3 | 4 | 0 | 0 | 0 | 2 | `Locomotion.AllPlayersMoveParam`、`Attack.DualPathUnified`、`Clock.MeleeNormalizedWindow`、`UI.DamageNumberScale` |
| FND00B06 | 14 | 10 | 2 | 0 | 0 | 2 | 2 | `Animation.Attack`、`Animation.Hit`（observed：`Animation.ConfigMapping`、`Animation.PostCombatState`） |

SPEC01 基线全绿 → 满足「SPEC01 基线有红即停」的反面条件，继续。

---

## 3. Step 1 — `SwordSwing` → `MeleeCombat`

### 3.1 改动
- `Assets/_Project/Scripts/Combat/SwordSwing.cs` → `MeleeCombat.cs`：文件改名；`public class SwordSwing` → `public class MeleeCombat`（**唯一内容差异**）。
- `Assets/_Project/Scripts/Combat/SwordSwing.cs.meta` → `MeleeCombat.cs.meta`：**内容零改动**（SHA256 前后均为 `32410D1196880381BAAF147609AB210F57B0B230799EBBC8492947FC058071B2`），`guid: 9ec9f7a6099e5204fb03ca3a12b8afdd` 原样。
- 引用同步 26 处：`SkillEffects.cs`(1，注释)、`GameData.cs`(1)、`HUD.cs`(1)、`SkillSystem.cs`(1)、`FND00B06_ProbeRunner.cs`(14)、`FND02B_ProbeRunner.cs`(3)、`SPEC01_ProbeRunner.cs`(5)。含 `typeof(SwordSwing)`、`GetComponent<SwordSwing>()`、字段类型与字符串键 `hasSwordSwing`→`hasMeleeCombat`。

> 工单写「19 处」为估算；实测 `SwordSwing` 总出现 27 次 = 类声明 1 + 引用 26。全部同步后 `.cs` 内归零。

### 3.2 逻辑零改动证明
`HEAD:Assets/_Project/Scripts/Combat/SwordSwing.cs` 与当前 `MeleeCombat.cs` 逐行比对：**375 行 vs 375 行，差异仅 1 行**（类名）。补丁见 `CLEANUP/diff/rename.patch`。

### 3.3 验收
| 项 | 结果 |
|---|---|
| `Assets/**/*.cs` 中 `SwordSwing` 计数 | **0** |
| `.cs.meta` 中 `SwordSwing` 计数 | **0**（文件名随迁） |
| `MeleeCombat.cs.meta` guid | `9ec9f7a6099e5204fb03ca3a12b8afdd`（未变） |
| `Knight_Unit.prefab:185` / `PlayerCapsule.prefab:287` 的 `m_Script` guid | 未变（只读核对） |
| `.prefab/.controller/.unity/.asset/.overrideController` 改动 | **无** |
| SPEC01 1 轮 | 8/8 pass，exit 0 |
| FND02B 1 轮 | 3 pass / 4 fail，红项名与基线**逐条一致** → 无新增红 |

---

## 4. Step 2 — 夹具双清

### 4.1 2① FND00B06 硬编码（只改两个字符串）
`FND00B06_ProbeRunner.cs`
```diff
-    static readonly string[] RequiredStates = { " Locomotion", "Attack_Draw", "Hit_A", "Downed", "Rise" };
-    static readonly string[] RequiredParams = { "SpeedX", "SpeedZ", "Grounded", "AttackSpeed", "Attack", "Hit", "Die", "Revive" };
+    static readonly string[] RequiredStates = { "Locomotion", "Attack_Draw", "Hit_A", "Downed", "Rise" };
+    static readonly string[] RequiredParams = { "SpeedX", "SpeedZ", "Grounded", "AttackSpeed", "AttackRange", "Hit", "Die", "Revive" };
```
依据（只读核对）：`PlayerBase.controller` 的参数为 `AttackMelee`、`AttackRange`（无 `Attack`），状态名无前导空格。

**效果证据**：`Animation.ConfigMapping` 的 `actual` 由
`{"mappingConsistent":false,"mismatchCount":2}` → `{"mappingConsistent":true,"mismatchCount":0}`。

### 4.2 2② FND02B 删除过时 UI 断言（方案 A）
- 删除注册行 `yield return RunSafe("UI.DamageNumberScale", AssertDamageNumberScale());`
- 删除 `// 7. UI.DamageNumberScale` 段落 + `IEnumerator AssertDamageNumberScale()` 整个方法（共 83 行）
- 文件头注释：断言清单去掉 `→ UI.DamageNumberScale`，新增一行
  `/// UI.DamageNumberScale 已由 SPEC-01 套件覆盖（SPEC-01 收口，2026-11-05）。`
- **其他断言零改动**。

### 4.3 验收结果

**FND02B（6 条）**

| 断言 | 结果 |
|---|---|
| `Regression.FND02A` | pass |
| `Clock.RangedOneShotPerCycle` | pass |
| `Animation.PostCombatAndHit` | pass |
| `Locomotion.AllPlayersMoveParam` | product_fail（`blend2D=False`） |
| `Attack.DualPathUnified` | product_fail（骑士 hit 状态为 `Other:579103364`＝`AttackMelee`） |
| `Clock.MeleeNormalizedWindow` | product_fail（`releaseEntries=0`） |

- 断言数 7→6，`UI.DamageNumberScale` 已消失；剩余 3 红与基线**同名同因**。
- **「6/6 全绿」闸门未达成**（详见 §7 决策项 B），未做任何越权修改。

**FND00B06（14 条）**

| 断言 | 结果 |
|---|---|
| `Attack.Fixture` / `Attack.CommandAccepted` / `Attack.Approach` / `Attack.Starts` / `Attack.Repeats` / `Attack.DamageResolved` / `Attack.TargetDeath` | pass ×7 |
| `Animation.Idle` / `Animation.Move` / `Animation.DownedOrDeath` | pass ×3 |
| `Animation.ConfigMapping` / `Animation.PostCombatState` | observed ×2（设计如此，不计红） |
| `Animation.Attack` / `Animation.Hit` | product_fail ×2 |

- 红项集合 `{Animation.Attack, Animation.Hit}` **== 基线** → 按 P2 判定 Step 2① 有效，Step 3 照常执行，**不修复**。

---

## 5. Step 3 — API 文档同步

文件：`docx/API文档_魔石地牢_v1.0.md`（diff 15 insertions / 11 deletions，**仅此一个文件**）

1. `SwordSwing` → `MeleeCombat` 共 5 处（`:161` 字段表、`:422` 类标题+路径、`:850` 接口说明、`:898` 待清理表、`:900` 合规确认）。
2. 攻击触发器/状态口径落文（`:850` 所在接口段）：
   > 攻击触发器/状态口径：近战 `AttackMelee`（状态名＝触发器名，单段）；远程 `AttackRange`（两段式 `Attack_Draw → Attack_Release`）。
3. `MeleeCombat` 字段表按代码事实校正：新增 `attackTrigger`(="AttackMelee")、`hitStateName`(="AttackMelee")、`drawStateName`(="Attack_Draw")、`releaseStateName`(="Attack_Release")；`attackStateName` 默认 `"Attack"`→`"Attack_Draw"`；`attackLayer` `1`→`0`；标题 `UpperBody 层`→`单层动画 layer 0`。
4. `RangedCombat.attackTrigger` 说明更正为「玩家弓手/法师预制体覆写为 AttackRange；骷髅弓手填 AttackRanged」（脚本默认值仍为 `"Attack"`，属生产现状，未改代码）。
5. 顺带把该文件 3 个游离 `CR` 归一为 `LF`（与文件其余部分一致，git 归一化后 diff 不变）。

**验收**：文档内 `SwordSwing` = **0**；`git diff --stat -- docx` 仅该文件。

> 说明：`docx/编辑规范清单.md:45-46` 另有 `SwordSwing` 字样，属编辑规范文档，不在 Step 3 指定范围，**未触碰**。

---

## 6. 改动文件清单

| # | 文件 | 性质 |
|---|---|---|
| 1 | `Assets/_Project/Scripts/Combat/MeleeCombat.cs` | 新增（由 `SwordSwing.cs` 改名，1 行内容差异） |
| 2 | `Assets/_Project/Scripts/Combat/MeleeCombat.cs.meta` | 新增（由 `SwordSwing.cs.meta` 改名，字节不变） |
| 3 | `Assets/_Project/Scripts/Combat/SwordSwing.cs` / `.meta` | 删除（改名另一端） |
| 4 | `Assets/_Project/Scripts/Combat/SkillEffects.cs` | 注释 1 处 |
| 5 | `Assets/_Project/Scripts/Core/GameData.cs` | 参数类型 1 处 |
| 6 | `Assets/_Project/Scripts/UI/HUD.cs` | 泛型实参 1 处 |
| 7 | `Assets/_Project/Scripts/Units/SkillSystem.cs` | 字段类型 1 处 |
| 8 | `Assets/_Project/Tests/FND00B/Runtime/FND00B06_ProbeRunner.cs` | 14 处改名 + 2 个字符串 |
| 9 | `Assets/_Project/Tests/FND02B/Runtime/FND02B_ProbeRunner.cs` | 3 处改名 + 删除过时断言 |
| 10 | `Assets/_Project/Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs` | 5 处改名 |
| 11 | `docx/API文档_魔石地牢_v1.0.md` | 文档同步 |
| 12 | `Assembly-CSharp.csproj` / `Assembly-CSharp-Editor.csproj` | **Unity 自动重生成**（非手改）：`MeleeCombat.cs` 替 `SwordSwing.cs`，并补入 SPEC-01 测试文件条目（既有缺口） |

完整 diff：
- `CLEANUP/diff/rename.patch`（重命名 + 1 行）
- `CLEANUP/diff/modified.patch`（其余 8 个受控文件，391 行）

---

## 7. 醒来决策项（**未修复，等所有者裁决**）

### 决策项 A — FND00B06 2 红（按 P2 保留）

| 断言 | 期望 | 实测 | 判定 |
|---|---|---|---|
| `Animation.Attack` | 骑士攻击进入 `Attack_Draw/Attack_Release`，`AttackSpeed` 与 stats 一致 | 首次攻击状态 `Locomotion`，`enteredAttackState=false`；时间线里攻击状态为 `unknown#579103364`（即 `AttackMelee`，因 `KnownStates` 未收录该名而显示为哈希） | **套件过时**：SPEC-01 已将骑士近战改为单段 `AttackMelee`，SPEC01 的 `Attack.DualPathUnified` 全绿 |
| `Animation.Hit` | 对玩家 `UnitHealth.ApplyDamage` 后进入 `Hit_A` | `hitEvents=1`、`hp 100→95`，但玩家 Animator 全程 `Locomotion`，`hitStateObserved=false` | **待定**：同一路径在 SPEC01 `Animation.PostCombatAndHit` 中通过；怀疑 FND00B06 在 `OnHit` 回调当帧取样（早于 Animator 状态切换），属夹具时序缺陷 |

**修复建议（供所有者选择，均未执行）**
1. `Animation.Attack`：把 `KnownStates` 加入 `"AttackMelee"`、`AttackStates` 改为单段口径（或直接删除该断言，由 SPEC01 覆盖）。
2. `Animation.Hit`：先人工在编辑器里对骑士造成一次伤害，确认是否播 `Hit_A`；若播，则比对 SPEC01 的等待逻辑（等待 1–2 帧再取状态）修正 FND00B06 取样点；若不播，则是真实产品缺陷，另开工单。
3. B2（`Interrupt` 无过渡）/ B6（FND00B06 硬编码历史）按工单要求**未回填**。

### 决策项 B — FND02B「6/6 全绿」闸门未达成（**计划假设与现状不符**）

| 断言 | 基线 | Step2 后 | 根因（只读核对） |
|---|---|---|---|
| `Locomotion.AllPlayersMoveParam` | fail | fail | 断言要求 **2D 混合树**（`blendType != Simple1D`、`blendParameterY=="SpeedZ"`）；实际 `PlayerBase.controller` 为 **Simple1D + SpeedZ**。SPEC01 已改为 1D 口径并通过 |
| `Attack.DualPathUnified` | fail | fail | 断言要求骑士命中时处于 `Attack_Release`；实际为 `AttackMelee`（单段） |
| `Clock.MeleeNormalizedWindow` | fail | fail | 断言在 `Attack_Release` 上取 normalizedTime，实际 `releaseEntries=0`（近战不再进该状态） |
| `UI.DamageNumberScale` | fail | **已删除** | 过时断言（读已删除的 `CombatFeedback.damageNumberScale`），按 P3 方案 A 删除 |

**结论**：FND02B 是 SPEC-01 之前的旧套件，其近战/移动断言已被 SPEC-01 取代（SPEC01 同名断言全绿）。删除 UI 断言后剩余 3 红与基线同名同因，**无新增红**。

**修复建议（未执行）**
- 方案 1（推荐）：将 FND02B 标记为「被 SPEC-01 取代」，在工单/文档登记后停止维护，不再作为闸门。
- 方案 2：把 3 条断言改写为单段 `AttackMelee` + 1D `SpeedZ` 口径（等价于把 SPEC01 的实现搬过来），但属「改动其他断言」，超出本单授权。
- 方案 3：整体删除 FND02B 套件（需所有者批准，涉及证据归档策略）。

---

## 8. 实现原理（面向项目所有者）

**为什么改类名**：Unity 要求「脚本文件名 = 主类名」，否则 Inspector 组件绑定会失效。`SwordSwing` 这个名字描述的是「挥剑」这一表现，而 `RangedCombat` 描述的是「远程战斗」这一职责；同一层的两个执行器命名口径不一致，后续加「长柄/双持」等近战风格时无法扩展。改名后：`MeleeCombat`（近战）/ `RangedCombat`（远程）成对，新增风格可走 `Attack<风格>` 触发器约定。

**为什么能安全改名**：Unity 的序列化引用走 `.meta` 的 GUID，不走类名/文件名。因此只要 `.meta` 随文件一起改名且 GUID 不变，`Knight_Unit.prefab`、`PlayerCapsule.prefab` 里的组件引用就自动继续有效——本单实测 meta 字节哈希前后一致、两处预制体 GUID 未动，即证明这一点。代价是 `Assembly-CSharp*.csproj` 由 Unity 重生成（已发生，非手改）。

**为什么 FND00B06 只改两个字符串**：那两个字符串是「测试夹具对动画机的期望清单」。控制器真实参数是 `AttackRange`、真实状态名是 `Locomotion`（无前导空格），旧字符串导致 `mappingConsistent=false` 的假红。改后 `mismatchCount` 2→0，说明夹具与动画机重新对齐；但两条真正过时的断言（`Animation.Attack`/`Animation.Hit`）按 P2 保留，因为它们不是「夹具写错」，而是「产品语义已变，断言该重写」——这需要所有者决策，不能在清理单里顺手改。

**为什么删而不是改 FND02B 的 UI 断言**：该断言读的 `CombatFeedback.damageNumberScale` 字段已被 SPEC-01 的飘字系统删除，断言本身没有可修的对象；而 SPEC-01 的 `UI.DamageNumberScale` 已经用新口径覆盖同一需求。保留只会制造永久红，删除是唯一不产生假信号的选择。

---

## 9. 受影响模块导航说明

| 模块 | 职责 | 入口 | 数据流 |
|---|---|---|---|
| `Combat/MeleeCombat.cs` | 近战执行器：起手 → 动画时钟命中窗口 → 结算 | `ICharacterCombat.AutoAttack/DoSkillAttack` | `UnitBrain/CharacterBrain.CommandAttack` → `MeleeCombat.DoAttack` → `Animator.SetTrigger(attackTrigger)` → `Update` 读 `hitStateName` 的 normalizedTime → `UnitHealth.TakeHit` |
| `Combat/RangedCombat.cs` | 远程执行器：两段式 `Attack_Draw → Attack_Release` | 同上 | `SetTrigger(attackTrigger=AttackRange)` → 前摇/释放状态时钟 → 箭矢/法术生成 |
| `Units/SkillSystem.cs` | 技能输入与冷却，持有近战引用 | `SkillSystem.sword` | 技能斩 → `MeleeCombat.DoSkillAttack()`（扫掠） |
| `Core/GameData.cs` | 升级数值 | `GameData.TryLevelUp(MeleeCombat, UnitHealth)` | 升级 → 提升近战 `dmgMult` |
| `UI/HUD.cs` | HUD 读取近战状态 | `GetComponent<MeleeCombat>()` | 显示技能/武器信息 |
| `Tests/SPEC01` | 当前权威验收套件（8 条） | `SPEC01_BatchAcceptance.Run` | 场景 Play 模式 → 内存夹具 → 证据 JSONL |
| `Tests/FND02B` | 旧验收套件（6 条，已被 SPEC01 取代） | `FND02B_BatchAcceptance.Run` | 同上 |
| `Tests/FND00B` | 第 4/6 项行为验收（14 条） | `FND00B06_BatchAcceptance.Run` | 同上 |

**改名影响面**：只有 C# 编译期符号；场景/预制体/动画机零影响（GUID 不变）。回滚方式：把两个文件改回原名、类名改回 `SwordSwing`，其余引用同量反向替换即可。

---

## 10. 未覆盖项 / 遗留问题

1. **未跑 N≥3 轮**：本单为确定性改名 + 字符串修正，非间歇性/时序问题；按工单每步各 1 轮。FND02B/FND00B06 的红项在两轮独立运行中逐条复现（step0 与 step1/step2），已具备稳定性证据。
2. **未做 Unity 编辑器内目检**（Inspector 组件名、手感、动画）——已全部列入《醒来目检清单》。
3. `Assets/_Project/Scripts.zip` 内仍含 `SwordSwing` 字样：历史压缩包，非 `.cs`，不在范围，未触碰。
4. `docx/编辑规范清单.md` 仍含 `SwordSwing`：非 Step 3 指定文件，未触碰。
5. `Assembly-CSharp*.csproj` 的 SPEC-01 测试文件条目属**既有缺口**（`e7535e4c` 入库时未重生成），本次由 Unity 顺带补上，建议随改名提交一起入库。
6. `CLEANUP/_backup/` 是既有证据的还原快照，仅为自证用，可在所有者确认后删除，避免仓库体积膨胀。

---

## 11. 建议提交

> 全部改动均为本地工作区改动；**本会话未执行任何 git 写操作**。以下为建议，提交由所有者执行。
> `Docs/Handoff/CURRENT.md` 的修改来自【大脑】派单，不列入本次提交。

**① 近战执行器改名（代码 + 自动生成工程文件）**
```bash
git add Assets/_Project/Scripts/Combat/MeleeCombat.cs Assets/_Project/Scripts/Combat/MeleeCombat.cs.meta \
        Assets/_Project/Scripts/Combat/SwordSwing.cs Assets/_Project/Scripts/Combat/SwordSwing.cs.meta \
        Assets/_Project/Scripts/Combat/SkillEffects.cs Assets/_Project/Scripts/Core/GameData.cs \
        Assets/_Project/Scripts/UI/HUD.cs Assets/_Project/Scripts/Units/SkillSystem.cs \
        Assets/_Project/Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs \
        Assembly-CSharp.csproj Assembly-CSharp-Editor.csproj
```
```
refactor(combat): SwordSwing 更名 MeleeCombat 对齐 RangedCombat 命名口径

为什么：类名与职责口径统一，避免后续近战风格扩展时命名分叉；GUID 随 meta 保留，预制体引用零改动。
```

**② 测试夹具双清**
```bash
git add Assets/_Project/Tests/FND00B/Runtime/FND00B06_ProbeRunner.cs \
        Assets/_Project/Tests/FND02B/Runtime/FND02B_ProbeRunner.cs
```
```
test(fnd): FND00B06 夹具参数名对齐动画机并删除 FND02B 过时 UI 断言

为什么：消除 " Locomotion"/"Attack" 造成的假红，并把已被 SPEC-01 覆盖的 UI 尺寸断言移出旧套件。
```

**③ API 文档同步**
```bash
git add "docx/API文档_魔石地牢_v1.0.md"
```
```
docs(api): API 文档同步 MeleeCombat 类名与近战/远程攻击触发器口径

为什么：文档仍写 SwordSwing 与旧两段式近战口径，与当前代码和动画机不符。
```

**④ 收口证据与报告（纯 Docs）**
```bash
git add Docs/Handoff/REPORT_SPEC-01-CLEANUP.md Docs/Handoff/醒来目检清单_SPEC-01-CLEANUP.md \
        Docs/QA/Evidence/CLEANUP
```
```
docs(handoff): SPEC-01 收口三步批报告、醒来目检清单与证据归档

为什么：留存改名/夹具清理的可复现证据与两个待裁决红项，便于后续单独立项。
```

---

## 12. 现场（收口时）

```
$ git status --porcelain -uall     # 已剔除 CLEANUP 未跟踪证据
 M Assembly-CSharp-Editor.csproj
 M Assembly-CSharp.csproj
 M Assets/_Project/Scripts/Combat/SkillEffects.cs
 D Assets/_Project/Scripts/Combat/SwordSwing.cs
 D Assets/_Project/Scripts/Combat/SwordSwing.cs.meta
 M Assets/_Project/Scripts/Core/GameData.cs
 M Assets/_Project/Scripts/UI/HUD.cs
 M Assets/_Project/Scripts/Units/SkillSystem.cs
 M Assets/_Project/Tests/FND00B/Runtime/FND00B06_ProbeRunner.cs
 M Assets/_Project/Tests/FND02B/Runtime/FND02B_ProbeRunner.cs
 M Assets/_Project/Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs
 M Docs/Handoff/CURRENT.md          # 【大脑】产物，未动
 M docx/API文档_魔石地牢_v1.0.md
?? Assets/_Project/Scripts/Combat/MeleeCombat.cs
?? Assets/_Project/Scripts/Combat/MeleeCombat.cs.meta
```

- Unity 进程：0；`Temp/UnityLockfile`：不存在。
- 三套件既有证据目录：`git status` 为空（已逐字节还原）。
- 证据索引：`Docs/QA/Evidence/CLEANUP/Runs.jsonl`（7 条运行记录，JSON 校验通过）。

---

## 13. 收尾 — FND02B 退役 + FND00B06 单段口径对齐（`TASK_SPEC-01-CLEANUP-2`）

> 执行时间：2026-11-05；HEAD `7cf874f7c5db448215b33a90fcad1d209f1fb781`（与回滚锚一致）。
> 本单**未执行任何 git 写操作**；以下改动全部留在工作区，由所有者提交。
> 证据根：`Docs/QA/Evidence/CLEANUP/step5/`。

### 13.1 覆盖对照（Step A）

完整对照表见 `Docs/QA/Evidence/CLEANUP/step5/coverage_matrix.md`，下表为全文摘要（判定字段取自源码实际变量名）。

**A. 剩余 3 条（step2 红项）**

| FND02B 断言 | FND02B 判定字段 / 条件 | SPEC01 对应条目 | SPEC01 判定字段 / 条件 | 覆盖结论 |
|---|---|---|---|---|
| `Locomotion.AllPlayersMoveParam` | `players[].pass`：`arrived ∧ peakAgentSpeed≥2.0 ∧ √(SpeedX²+SpeedZ²)≥2.0 ∧ stateDuringMove=="Locomotion" ∧ finalMoveParam≤0.15 ∧ finalAgentSpeed≤0.15 ∧ grounded`；`controllerPass`：`stateNameExact ∧ blendTreeIs2D ∧ blendParameterY=="SpeedZ" ∧ layer0Only` | `Locomotion.AllPlayersMoveParam` | `players[].pass`：同构，移动参数改为 `\|peakSpeedZ\|≥2.0`、`\|finalSpeedZ\|≤0.15`；`controllerPass`：`stateNameExact ∧ blend1D ∧ blendParameter=="SpeedZ" ∧ thresholdsOk(2 子节点 @0/@4) ∧ layer0Only` | **完全覆盖（新口径超集）** |
| `Attack.DualPathUnified` | `designatedPass ∧ autoPass`，两条路径一律要求 `ev.state=="Attack_Release"`（近战同）；无序列化字段核验 | `Attack.DualPathUnified` | `designatedPass ∧ autoPass ∧ swordFieldsOk ∧ rangedTriggerOk`；近战期望 `AttackMelee`、远程 `Attack_Release`；额外核验 `MeleeCombat.attackTrigger/hitStateName=="AttackMelee"`、`RangedCombat.attackTrigger=="AttackRange"` | **完全覆盖（严格超集）** |
| `Clock.MeleeNormalizedWindow` | `countOk`（`hitEvents≥2 ∧ inWindow==hitEvents.Count ∧ \|hitEvents−releaseEntries\|≤1`）`∧ speedOk`（`\|AttackSpeed−stats.attackSpeed\|<0.01`），状态 `Attack_Release` | `Clock.MeleeNormalizedWindow` | 同一 `countOk`（以 `meleeEntries` 承接 `releaseEntries`）`∧ speedOk ∧ fieldOk`，状态 `AttackMelee` | **完全覆盖（超集）** |

**B. 同套件其余 3 条（step2 绿项，整体退役需一并核）**

| 断言 | 方法体比对 | 覆盖结论 |
|---|---|---|
| `Regression.FND02A` | 与 SPEC01 同名方法逐行等价，仅夹具命名字面量 `FND02B_Target_Combat` → `SPEC01_Target_Combat` | 完全覆盖 |
| `Animation.PostCombatAndHit` | 与 SPEC01 同名方法逐行等价 | 完全覆盖 |
| `Clock.RangedOneShotPerCycle` | 与 SPEC01 同名方法逐行等价，仅局部变量名 `rangedRows` → `rangedCycleRows` | 完全覆盖 |

**结论：独有覆盖 0 条、部分覆盖 0 条、完全覆盖 6/6 → 不保留任何断言，按 D1 整体退役。**

3 条红项的红因均为「旧口径期望过期」（2D 混合树 / `Attack_Release` 两段式），而非覆盖缺口；其判定维度已被 SPEC01 以更严格的新口径完整承接。退役后套件形态：`FND02B` 目录整体消失、无残留骨架，原 6 条断言职责全部由 `SPEC01` 8 条承接。

### 13.2 删除清单（精确路径，Step B / Step D）

```
Assets/_Project/Tests/FND02B/Editor/FND02B_BatchAcceptance.cs
Assets/_Project/Tests/FND02B/Editor/FND02B_BatchAcceptance.cs.meta
Assets/_Project/Tests/FND02B/Runtime/FND02B_Evidence.cs
Assets/_Project/Tests/FND02B/Runtime/FND02B_Evidence.cs.meta
Assets/_Project/Tests/FND02B/Runtime/FND02B_ProbeRunner.cs
Assets/_Project/Tests/FND02B/Runtime/FND02B_ProbeRunner.cs.meta
Assets/_Project/Tests/FND02B/Editor.meta
Assets/_Project/Tests/FND02B/Runtime.meta
Assets/_Project/Tests/FND02B.meta
Docs/QA/Evidence/CLEANUP/_backup/            # 整目录，71 文件 / 3.21 MB
```

保留清单：`Assets/_Project/Tests/{FND00B,FND02A,SPEC01}/` 全部原样；`Docs/QA/Evidence/CLEANUP/{step0,step1,step2,step5,diff}/` 与 `Runs.jsonl`；`Docs/QA/Evidence/{SPEC-01,FND-02B,FND-00B-06}/` 原样（见 13.5）。

引用核查：`Assets/**/*.cs` 中 `FND02B` 由 90 处（3 文件）→ **0 处**；`Assembly-CSharp*.csproj` 中 3 处由 Unity 在 batchmode 启动时自动重生成清除，**未手改**。

### 13.3 FND00B06 前后红项对照（Step C）

| 断言 | 改前（step2 run1） | 改后（step5 run1） | 结论 |
|---|---|---|---|
| `Animation.Attack` | `pass=false`；`enteredAttackState=false`；`attackStateEntryCount=0`；时间轴 4 次采样为 `unknown#579103364`；`attackSpeedParam=null` | `pass=true`；`enteredAttackState=true`；`attackStateEntryCount=4`；4 次采样均为 `AttackMelee`；`attackSpeedParam=1`（= `stats.attackSpeed` 1）；`animationLagBehindDamageSeconds=-0.125` | **转绿** |
| `Animation.Hit` | `pass=false`（`product_fail`）；`hitEvents=1`、`hp 100→95`、`preState=Locomotion`、`hitStateObserved=false` | **逐字段相同**，仍 `pass=false`（`product_fail`） | **保持红（冻结）** |
| 套件汇总 | 14 断言 / pass 10 / fail 2 / blocked 0 / infra 0 / observed 2 / exit 2 | 14 断言 / pass 11 / fail 1 / blocked 0 / infra 0 / observed 2 / exit 2 | 无新增红 |

根因（`Animation.Attack`）：`KnownStates` 缺 `AttackMelee`，运行期 `ResolveState` 在 `_stateNames` 尚未填充时把该状态解析成 `unknown#579103364`，`IsAttackState` 判否 → `enteredAttackState=false`。`swordConfigured.attackStateName` 在改前就已读出 `AttackMelee`，可证状态确实进入、只是测试侧不认识该名字。

改动（`Assets/_Project/Tests/FND00B/Runtime/FND00B06_ProbeRunner.cs`，共 5 处）：

1. `KnownStates` 增 `"AttackMelee"`；
2. `AttackStates` → `{ "AttackMelee" }`；
3. 新增 `IsMeleeAttackState(string)`（仅 `AttackMelee` 为真）；
4. `Animation.Attack` 的 `attackEntries` 过滤与 `enteredAttackState` 改用 `IsMeleeAttackState`；requirement 文案改为「进入 AttackMelee（单段，状态名=触发器名）」；
5. `IsAttackState` 改为并集（`AttackMelee ∨ Attack_Draw ∨ Attack_Release`）——保留旧两段是为了**不弱化** `Animation.Idle` 的「非攻击态」判定。

`AttackSpeed` 比对（`attackSpeedParam` vs `attackSpeedExpected`/`stats.attackSpeed`）原样保留。

### 13.4 `Animation.Hit` 冻结声明

`Animation.Hit` 的取样时机、夹具、期望值、日志字段（`hitEvents/hpBefore/hpAfter/finalAmount/preState/hitStateObserved/hitState/playerHpUnaffected`）与 `HitAnimationFixture()` 方法体：**本单一行未触碰**，`git diff` 中该区块无任何增删行。所有者将先在编辑器内手动验证（打一次伤害看 `Hit_A` 是否播放）后再定「修夹具 / 立缺陷单」。执行者不对该红项作任何结论。

### 13.5 证据文件处置说明

| 项 | 结果 |
|---|---|
| `CLEANUP/_backup/` | **已删除**（71 文件 / 3.21 MB） |
| `CLEANUP/step0`、`step1`、`step2` | 保留（14 / 9 / 8 文件） |
| `CLEANUP/diff/` | 保留（2 文件） |
| `CLEANUP/step5/` | **新建**，10 文件 / 0.40 MB |
| `CLEANUP/Runs.jsonl` | 追加 step5 两行（SPEC01、FND00B06），现 9 行，逐行 JSON 校验通过 |
| `CLEANUP` 合计 | 44 文件 / 1.81 MB |
| `Docs/QA/Evidence/{SPEC-01,FND-00B-06}` | 跑批前镜像备份到仓库外临时目录，跑完逐字节还原；`git status -- Docs/QA/Evidence/SPEC-01 Docs/QA/Evidence/FND-00B-06` = **0 行** |

`step5/` 内容：`coverage_matrix.md`、`SPEC01_{Assertions.jsonl, Automation.log, Runs.jsonl, Summary_run1.json, ExitCode.txt}`、`FND00B06_{Assertions.jsonl, Automation.log, Summary_run1.json, ExitCode.txt}`。

### 13.6 验收结果

| 项 | 标准 | 实测 | 判定 |
|---|---|---|---|
| 编译 | 全轮 `error CS`=0、`infraFail`=0 | SPEC01 0、FND00B06 0；两轮 `infraFail=0` | ✅ |
| SPEC01 | 1 轮 8/8 pass | 8/8 pass，fail 0，exit 0 | ✅ |
| FND00B06 | `Animation.Attack` 转绿、仅剩 `Animation.Hit` 红、无新增红 | pass 11 / fail 1（仅 `Animation.Hit`）/ observed 2 不变 | ✅ |
| 代码引用 | `Assets/**/*.cs` 中 `FND02B` 归零 | 0 处 | ✅ |
| 对照表 | 3×8 齐全、结论明确、已进 REPORT | 见 13.1 与 `step5/coverage_matrix.md` | ✅ |
| 证据 | `_backup/` 不存在、`step5/` 存在 | 均满足 | ✅ |

### 13.7 实现原理（面向项目所有者）

**根因**：FND02B 是 SPEC-01 之前的旧套件，它的 3 条红项并不是「产品坏了」，而是**测试还在按旧契约提问**——旧契约里 `Locomotion` 是 2D 混合树（`SpeedX`+`SpeedZ`）、近战是 `Attack_Draw→Attack_Release` 两段式。SPEC-01 已把契约改成「1D 混合树只吃 `SpeedZ`（阈值 0/4）」+「近战单段 `AttackMelee`，状态名＝触发器名」。所以旧断言必然红，而新断言全绿。

**方案原理**：
1. 先做**方法体级逐行比对**（不是只看断言名），确认 SPEC01 的同名断言是旧断言的超集，才敢整体删；否则按所有者授权保留独有部分。本次比对结论是「无独有」，故整体退役。
2. FND00B06 的 `Animation.Attack` 红因不是产品问题，而是测试的 `KnownStates` 白名单里没有 `AttackMelee`，导致运行期把真实状态解析成 `unknown#<hash>`。修法只需**把新状态名登记进白名单**，并把该断言的期望从「进入任一攻击状态」收紧为「进入 `AttackMelee`」——这正是「口径对齐」而非「放宽断言」。
3. `IsAttackState` 保留旧两段，是因为它还被 `Animation.Idle` 复用（判定「当前不在攻击态」）。若直接改成只认 `AttackMelee`，`Idle` 的判定会被悄悄放宽；因此拆成「宽判定 `IsAttackState`（并集）+ 严判定 `IsMeleeAttackState`（仅单段）」两个函数，各用其位。

**为什么这样选**：删除比保留更彻底，但前提是有等价的更严覆盖；收紧期望比放宽断言更可信；新增一个只读的小函数，比改动共用函数语义风险更低。

### 13.8 受影响模块导航说明

- `Assets/_Project/Tests/FND00B/Runtime/FND00B06_ProbeRunner.cs` — 第 4 项（攻击链路）+ 第 6 项（动画状态）验收探针。入口：`FND00B06_BatchAcceptance.Run` → Play 模式 `FND00B06_ProbeRunner.Start()`；状态解析经 `ResolveState`（先查控制器状态字典 `_stateNames`，未命中回退 `KnownStates` 白名单），判定经 `IsAttackState` / `IsMeleeAttackState`。
- `Assets/_Project/Tests/SPEC01/` — 现役唯一行为验收套件（8 条），本次未改动，仅作为承接方。
- `Assets/_Project/Tests/FND02B/` — **已删除**；历史证据仍在 `Docs/QA/Evidence/FND-02B/`，但该套件不再可复跑。
- 数据流：`CharacterBrain.CommandAttack/AutoAttack → MeleeCombat.DoAttack → Animator(AttackMelee) → UnitHealth.ApplyDamage`；测试只读观察，不写生产资产。

### 13.9 建议提交

> 全部为工作区改动；**本会话未执行任何 git 写操作**。`Docs/Handoff/CURRENT.md` 为【大脑】产物，不列入。

**① 近战执行器改名（代码 + 自动生成工程文件）** — 与上一单同批
```bash
git add Assets/_Project/Scripts/Combat/MeleeCombat.cs Assets/_Project/Scripts/Combat/MeleeCombat.cs.meta \
        Assets/_Project/Scripts/Combat/SwordSwing.cs Assets/_Project/Scripts/Combat/SwordSwing.cs.meta \
        Assets/_Project/Scripts/Combat/SkillEffects.cs Assets/_Project/Scripts/Core/GameData.cs \
        Assets/_Project/Scripts/UI/HUD.cs Assets/_Project/Scripts/Units/SkillSystem.cs \
        Assets/_Project/Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs
```
```
refactor(combat): SwordSwing 更名 MeleeCombat 对齐 RangedCombat 命名口径

为什么：类名与职责口径统一，避免后续近战风格扩展时命名分叉；GUID 随 meta 保留，预制体引用零改动。
```
> `Assembly-CSharp.csproj` / `Assembly-CSharp-Editor.csproj` 属 Unity 自动重生成噪音，**提交时取消勾选**（本单 batchmode 已顺带清除 FND02B 条目，diff 中只剩此类噪音）。

**② 测试口径对齐 + 退役 FND02B**
```bash
git add Assets/_Project/Tests/FND00B/Runtime/FND00B06_ProbeRunner.cs
git add -A Assets/_Project/Tests/FND02B Assets/_Project/Tests/FND02B.meta   # 目录已删，-A 用于登记删除
```
```
test: 对齐 FND00B06 单段口径并退役 FND02B 套件

为什么：近战已改单段 AttackMelee，旧套件断言的 2D 混合树与 Attack_Release 期望全部过期，其覆盖已被 SPEC01 严格超集承接。
```

**③ 报告与证据（纯 Docs）**
```bash
git add Docs/Handoff/REPORT_SPEC-01-CLEANUP.md Docs/Handoff/醒来目检清单_SPEC-01-CLEANUP.md \
        Docs/QA/Evidence/CLEANUP
```
```
docs(handoff): SPEC-01 收尾报告、覆盖对照表与 step5 证据归档

为什么：留存 FND02B 退役的覆盖依据、FND00B06 前后红项对照与 Animation.Hit 冻结现场，便于后续单独立项。
```
> `Docs/QA/Evidence/CLEANUP/_backup/` 为未跟踪快照，已删除，无需 `git rm`；若提交时仍出现在列表中，直接忽略。

### 13.10 遗留问题

1. `Animation.Hit` 仍红，**冻结中**，等所有者编辑器手动验证后另立单。
2. FND02B 退役后，`Docs/QA/Evidence/FND-02B/` 的历史证据**不再可复跑**（套件已删）；如需追溯只能读日志。
3. `Animation.ConfigMapping` 的 `RequiredStates` 仍含 `Attack_Draw`（`observed` 级，未计入红绿）；当前控制器仍保留该状态，故未报 mismatch，但该清单已不是最新口径——留给后续清理，本单不动。
4. 未跑 N≥3 轮：本单为确定性删除 + 白名单补名，非间歇性/时序问题；两套件各 1 轮，且红绿分布与基线逐条对得上。
