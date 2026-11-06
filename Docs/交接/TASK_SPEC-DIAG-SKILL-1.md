# TASK — SPEC-DIAG-SKILL-1：FND-00B-06 Animation.Hit 夹具返工（按证据修正）

> **风险等级：低风险**（仅修改测试夹具；不动生产代码与任何资产）
> **所有者授权**：承接 `TASK_SPEC-DIAG-SKILL` 的 Step 6，同样是睡前长任务授权：落盘即批准，不停下等确认。
> **回滚锚**：`7cf874f7c5db448215b33a90fcad1d209f1fb781`
> **前置事实**：`REPORT_SPEC-DIAG-SKILL.md` §5.7 已用 red 证据推翻"非 Locomotion 态"的旧定案；真实病因是夹具在 `Start()` 前禁用 `CharacterBrain`，`CharacterBrain.Start()`（`Assets/_Project/Scripts/Units/CharacterBrain.cs:21-26`）里对 `UnitHealth.OnHit` 的订阅从未建立，而全仓唯一玩家 `SetTrigger("Hit")` 来源就是 `CharacterBrain.OnDamaged`（`CharacterBrain.cs:33-38`）。因此 `preState=Locomotion` 时依然不会进 `Hit_A`。

---

## 1. 目标

在**不改生产代码、不改任何资产**的前提下，修正 `FND00B06_ProbeRunner` 的 Hit 夹具生命周期，让"受击 → Hit_A"走**真实生产链路**并稳定通过，red→green 证据完整。

- red 证据已存在：`Docs/QA/Evidence/DIAG-SKILL/fnd00b06_red_*`（`Animation.Hit` fail，`preState=Locomotion`、`hitEvents=1`、`hitStateObserved=false`）。本单不重跑 red。
- green 要求：连续 **N=3** 次 `FND00B06` 全跑，`Animation.Hit` 连续 pass，其余断言不回退。

## 2. 授权改动范围

只允许修改：

| 文件 | 位置 | 改动 |
|---|---|---|
| `Assets/_Project/Tests/FND00B/Runtime/FND00B06_ProbeRunner.cs` | `CreateAnimationFixture(...)` 与 `HitAnimationFixture()` | 调整 `CharacterBrain` 的禁用时机，并补受击前 Locomotion 前置 |

**绝不触碰**：`Assets/_Project/Scripts/**`（生产代码）、任何 `.unity/.prefab/.asset/.controller/.anim/.mat`、`ProjectSettings/`、`Packages/`、其他测试文件、`Docs/Handoff/CURRENT.md`、任何既有 `REPORT_*`/`TASK_*`。禁止任何 git 写操作。

> 该文件当前已含 SPEC-01-CLEANUP 的既有未提交改动。**只允许追加/调整本单所需的最小 hunk**，不得重排、不得删除他人改动。

## 3. 执行步骤

### Step 0 — 前置自证

与上一单相同：`Get-Process -Name Unity` 计数 = 0、`Test-Path Temp/UnityLockfile` = False；写 `Docs/QA/Evidence/DIAG-SKILL/06_前置确认_Hit返工.txt`。不满足即停。

### Step 1 — 最小夹具修改

1. `CreateAnimationFixture(...)`：**不要**在 `Instantiate` 后立刻 `brain.enabled = false`。
   - 改为：实例化后保持 `CharacterBrain` 启用，先让 `Awake/Start` 正常执行（协程里 `yield return null` 至少 1 帧；若需要，2 帧），使 `Start()` 完成 `UnitHealth.OnHit += OnDamaged` 订阅；
   - 之后再 `brain.enabled = false`，停止一切自主移动/索敌，但不影响事件回调。
   - 若 `CreateAnimationFixture` 目前是普通方法，允许把它改成协程或把"禁用 brain"移到各调用方；改动范围仅限本文件，且 Downed 夹具行为必须保持等价（禁用后不自主移动）。
2. `HitAnimationFixture()`：
   - 施伤前显式把 Animator 参数置为稳态：`Grounded=true`、`SpeedX=0`、`SpeedZ=0`；
   - 轮询等待（上限 2s）`TrimState(state) == "Locomotion"`，把等待帧数/耗时写入断言数据；
   - 前置不成立 → `BlockedAssertion`（理由"夹具未能进入 Locomotion"），不得伪造通过；
   - 前置成立后才 `ApplyDamage(5f, ...)`，断言语义保持不变（`Hit_A` + `hitEvents==1` + HP 差值）。
3. **不得**为了让测试变绿而在夹具里手动 `SetTrigger("Hit")` —— 那会绕过生产链路，属于作弊。

### Step 2 — green N=3

```powershell
& "D:\unity\2022.3.62f3c1\Editor\Unity.exe" -batchmode -projectPath "D:\unity\Dungeon" `
  -executeMethod FND00B06_BatchAcceptance.Run `
  -logFile "D:\unity\Dungeon\Docs\QA\Evidence\DIAG-SKILL\FND00B06_green_run<N>.log"
```

要求每次：

- `Animation.Hit` = pass，且数据中 `preState=Locomotion`、`hitState=Hit_A`、`hitEvents=1`、hp 差 = 5；
- 其余断言不回退：`fail=0`、`infra=0`（`observed` 可与基线一致）；
- 退出码 0；
- 连续 3 次一致。任一失败按熔断处理。

### Step 3 — 证据与报告

- 证据：`Docs/QA/Evidence/DIAG-SKILL/FND00B06_green_run1..3.log` + 套件自身写出的 Assertions/Summary/ExitCode；并更新 `05_Animation_Hit_red_green.md` 的 green 段（保留原 red 数据，不得改写）。
- 若 `Docs/QA/Evidence/FND-00B-06/` 被套件覆盖，如实登记（属测试产物更新）。
- 追加报告：`Docs/Handoff/REPORT_SPEC-DIAG-SKILL-1.md`，必须含：
  1. 结果摘要（red→green）；
  2. 本单 hunk 的 before/after（前后各 ≤30 行）；
  3. 三次 green 的断言数据；
  4. **实现原理**：为什么"禁用时机"会造成 Hit 不触发、为什么事件订阅在组件禁用后仍然有效、为什么不是动画/资产问题；
  5. 遗留问题（若有）；
  6. 建议提交（只含本单文件 hunk，明确与 SPEC-01-CLEANUP 拆分）。

## 4. 熔断

同一修改重试 ≥3 次仍红、编译错误连续 ≥2 轮、需要改生产代码或资产、git 异常、Unity 未关闭 —— 任一触发即停并写现场报告。

## 5. 目检清单（人类，1 项）

进入 `DungeonWorld` 后选中骑士，让敌人打一下：应播放受击动画并短暂进入 `Hit_A`，随后回到 Locomotion。
