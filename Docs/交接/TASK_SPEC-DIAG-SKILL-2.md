# TASK — SPEC-DIAG-SKILL-2：W 效果拆分 + 扣蓝顺序 + 夹具归位（逐项获批）

> **风险等级：中风险**（含 1 处生产代码 + 2 处资产：新 `.cs` 与 `W_Slash_Effect.asset` 的 `m_Script` 重指；均为所有者逐项批准）
> **所有者授权（2026-11-05 裁决）**：
> ① 资产 A（场景 `SkillInputController`）由所有者手动完成，AI 不动——**已完成并核实**（`DungeonWorld.unity` `&6932201`，`indicator` 指向 `582970962`，`skillKeys=71/77/65/72`）。
> ② 批准：拆 `HeavySlashEffect` 为独立 `.cs` 并重指 `W_Slash_Effect.asset`；**仅限该 `.asset` 的 `m_Script` 重指 + 新 `.cs`**。要求 Bash 行为不得变。
> ③ 同单修 `SkillSystem.Cast`：`CanCast` 先于扣蓝，并断言"失败施放不扣蓝"。
> ④ 同单把 DIAG 共用夹具迁到 `Tests/SPEC01/Runtime/`。
> 其余登记项不动。
> **验收**：`SPEC01` **10/10、exit 0**（N=3），REPORT 更新。
> **回滚锚**：`7cf874f7c5db448215b33a90fcad1d209f1fb781`

---

## 1. 背景（已证实，勿重复诊断）

- Q：输入组件缺失（资产 A），所有者已手动补好。
- W：`W_Slash_Effect.asset` 的 `m_Script` 指向 `SkillEffects.cs`（GUID `b87811a1f9107cc43b6bc8cb79537544`），该文件同时定义 `BashEffect` 与 `HeavySlashEffect`；Unity 一个 `.cs` 只对应一个 MonoScript，运行期解析为 `BashEffect`。证据：`Diag.CastChain.W` 的 `effectType=BashEffect`、`Cast(1,null)=false`、白扣 30 蓝（N=3）。`W_Slash_Effect.asset` 的 `m_EditorClassIdentifier: Assembly-CSharp::HeavySlashEffect` 只影响编辑器显示，不参与运行期解析。
- 代码缺陷：`SkillSystem.Cast`（`Assets/_Project/Scripts/Units/SkillSystem.cs:30-49`）先 `mana.Consume` 再 `CanCast`，失败会白扣蓝。
- 夹具组织：`Assets/_Project/Tests/DIAG/Runtime/SkillCastFixture.cs` 被 `SPEC01_ProbeRunner` 永久依赖，位置不当。

## 2. 授权改动清单（超出即越界）

### 2.1 生产代码（精确）

| 文件 | 改动 |
|---|---|
| `Assets/_Project/Scripts/Combat/HeavySlashEffect.cs`（**新建** + `.meta` 新 GUID） | 仅含 `HeavySlashEffect : SkillEffectBase`：`[CreateAssetMenu(menuName = "Dungeon/Skill Effects/Heavy Slash")]`、构造 `needTarget=false`、`CanCast => caster.sword != null`、`Execute => caster.sword.DoSkillAttack()`。逐字迁移，不改行为。 |
| `Assets/_Project/Scripts/Combat/SkillEffects.cs` | **仅删除** `HeavySlashEffect` 类（原 77-89 行）；`BashEffect` 一字不动。 |
| `Assets/_Project/Scripts/Units/SkillSystem.cs` | `Cast` 顺序改为：取槽/效果校验 → 冷却 → **`CanCast` 全部通过** → `mana.Consume` → `Execute` → 冷却写入。只移动扣蓝那一步，消息与返回值语义保持（失败时不扣蓝、不写冷却）。 |

### 2.2 资产（仅限）

| 文件 | 改动 |
|---|---|
| `Assets/_Project/Date/Skills/W_Slash_Effect.asset` | **只改 `m_Script` 的 guid** 为新建 `HeavySlashEffect.cs.meta` 的 GUID。**不得**改 `needTarget/range/时序` 等任何字段，不得改 `m_EditorClassIdentifier`。 |
| 新 `.cs.meta` | 全新、全仓唯一 GUID（PowerShell `[guid]::NewGuid().ToString("N")` 生成）。 |

**禁止**：改 `Q_Bash_Effect.asset`、`Q_Bash.asset`、`W_HeavySlash.asset`、`Knight_Unit.prefab`、任何场景、Animator、`ProjectSettings/`、`Packages/`；改其他资产或生产代码。

### 2.3 测试（精确）

| 文件 | 改动 |
|---|---|
| `SkillCastFixture.cs` | 从 `Assets/_Project/Tests/DIAG/Runtime/` **移动**到 `Assets/_Project/Tests/SPEC01/Runtime/`（`.meta` 随行、GUID 不变）；仅更新头部注释"DIAG 与 SPEC01 共用"→"SPEC01 常驻共用"。 |
| `SPEC01_ProbeRunner.cs` | 仅改 `AssertSkillCastSmoke()` 的判定与数据字段（见 §3），其余既有 hunk 不动。 |

> 不动 `DIAG_ProbeRunner.cs`（同程序集、全局命名空间，移动后自动可用）。

## 3. `Skill.CastSmoke` 新口径（必须全部满足）

顺序执行，全部记入同一条断言：

1. **失败施放不扣蓝**：`Cast(0, null)`（`BashEffect.CanCast` 需要目标）→ 返回 **false**；`manaDelta == 0`；`CdRemaining(0) == 0`；无异常。
   - 该条直接守护 ③：修复前应为白扣 20，修复后为 0（执行者必须先用**修复前**代码跑出 red 证据，再改 `SkillSystem`，见 §4）。
2. **Q 行为不变（防 Bash 回归）**：`Cast(0, enemyInRange)` → true；`effectType == "BashEffect"`；蓝 -20；CD≈6；靶掉血 = `MeleeCombat.GetRawDamage()`；≤0.5s 进入 `Bash`。
3. **W 类型正确**：`Cast(1, null)` → true；`effectType == "HeavySlashEffect"`；蓝 -30；CD>0；`MeleeCombat._timer >= 0`；≤0.5s 进入 `Skill`。
4. **E/R 空槽契约**：`Cast(2/3)` false；蓝不变；无异常。
5. 数据字段至少含：`failedCastReturn/manaDeltaFailed/cdFailed`、`qEffectType/wEffectType`、`qPass/wPass/emptyPass`、`failedNoManaPass`。
6. 判定：`pass = failedNoManaPass && qPass && wPass && emptyPass`。

**禁止**：放宽阈值、把 W 的动画等待改成 `observed`、删除任何子判定、在夹具里手动 `SetTrigger`。

## 4. 执行顺序（red → 改代码 → green）

1. **Step 0**：自证无 `Unity.exe`、无 `Temp/UnityLockfile`（资产 A 已由所有者完成，编辑器必须关闭）；写 `Docs/QA/Evidence/DIAG-SKILL/07_前置确认_SKILL2.txt`。
2. **Step 1（只改测试）**：迁移夹具（④）+ 按 §3 加断言（含"失败不扣蓝"），**不改生产代码**。
3. **Step 2（red）**：跑一次 `SPEC01`，预期：
   - `Skill.CastSmoke` fail，其中 `failedNoManaPass=false`（白扣蓝）且 `wEffectType=BashEffect`（类型错配）；
   - `Skill.InputWired` **应已 pass**（资产 A 已补），若仍 fail 立即停下报告（说明场景改动未生效）。
   证据：`Docs/QA/Evidence/DIAG-SKILL/SPEC01_skill2_red.log` + 断言快照。
4. **Step 3（改生产代码 + 资产）**：做 §2.1、§2.2。只做这些，不做"顺手优化"。
5. **Step 4（green N=3）**：连续跑 3 次 `SPEC01`，要求每次 **10/10 pass、fail=0、blocked=0、infra=0、exit=0**；三次 `qEffectType=BashEffect`、`wEffectType=HeavySlashEffect`、`failedNoManaPass=true`、Q 蓝量/CD/伤害/Bash 用时与修复前一致（-20 / ≈6 / 15 / ~0.14s）。
   - 命令：
     ```powershell
     & "D:\unity\2022.3.62f3c1\Editor\Unity.exe" -batchmode -projectPath "D:\unity\Dungeon" `
       -executeMethod SPEC01_BatchAcceptance.Run `
       -logFile "D:\unity\Dungeon\Docs\QA\Evidence\DIAG-SKILL\SPEC01_skill2_green_run<N>.log"
     ```
   - Windows 上 `Unity.exe` 是启动器，命令会立即返回：必须轮询进程归零后再取证据，禁止误拷上一轮产物。
6. **Step 5（回归确认）**：额外跑一次 `DIAG_BatchAcceptance.Run`，只看 `Diag.CastChain.W` 与 Q：应 `W castReturn=true、effectType=HeavySlashEffect、manaDelta=30、cd>0、anim=Skill`，Q 与前次一致。若 DIAG 未同步更新口径导致红，按实际记录、不得改生产代码迎合测试。

## 5. 证据与报告

- 证据目录：`Docs/QA/Evidence/DIAG-SKILL/`（red 1 次、green 3 次、DIAG 1 次、ExitCode/Summary 齐备）。
- 报告：新建 `Docs/Handoff/REPORT_SPEC-DIAG-SKILL-2.md`，必含：
  1. 结果摘要（red→green，10/10×3、exit 0）；
  2. 生产 diff（`SkillSystem.cs`、`HeavySlashEffect.cs`、`SkillEffects.cs`、`W_Slash_Effect.asset` 逐段 before/after）；
  3. 测试改动（夹具移动 + CastSmoke 新口径）；
  4. **实现原理**：为什么一个 `.cs` 两个类会解析成第一个；为什么拆文件+重指即可；为什么先 `CanCast` 再扣蓝；为什么"失败不扣蓝"必须由测试守护；
  5. **Bash 不回归的证据**：Q 的四项数值与修复前逐项一致；
  6. 模块导航（技能链现状）；
  7. 遗留项；
  8. 建议提交（正交拆分：生产修复 / 测试 / 文档）。
- 同时在 `Docs/Handoff/REPORT_SPEC-DIAG-SKILL.md` 末尾追加一节"§10 后续：SPEC-DIAG-SKILL-2 已闭合"，只写状态与证据指针，不改前文结论。

## 6. 熔断

单步重试 ≥3 次仍红；编译错误连续 ≥2 轮；`Skill.InputWired` 在 red 阶段仍 fail；需要改本清单外资产/代码；git 异常；Unity 未关闭 —— 任一触发即停并写现场报告。

## 7. 目检（人类，2 项）

1. `DungeonWorld`：选中骑士按 `W` 点敌人 → 应播 `Skill`（跳劈）动作并造成范围伤害，不再是盾击动作。
2. 按 `Q` 点 2.5m 内敌人 → 仍应播 `Bash`、掉血、击退（与修复前一致）。
