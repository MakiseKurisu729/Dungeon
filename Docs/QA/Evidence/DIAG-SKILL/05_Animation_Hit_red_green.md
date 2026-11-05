# 05 Animation.Hit：red 基线与定案复核

生成时间：2026-11-05
结论：**工单 §6 的"夹具病（非 Locomotion 态）"定案不成立**。按 §6.2 停止本步：未修改 `FND00B06_ProbeRunner.cs`，未做 green N=3。

> 后续：`SPEC-DIAG-SKILL-1` 按本文件 §3/§4 修好夹具后已取得 green N=3，见本文件 **§6**。§1–§5 为 red 阶段原始记录，保持原样未改写。

---

## 1. red 基线（修复前，单次）

命令：
```powershell
$env:FND00B06_RUN="1"
& "D:\unity\2022.3.62f3c1\Editor\Unity.exe" -batchmode -projectPath "D:\unity\Dungeon" `
  -executeMethod FND00B06_BatchAcceptance.Run `
  -logFile "D:\unity\Dungeon\Docs\QA\Evidence\DIAG-SKILL\FND00B06_red_run1.log"
```
结果：pid=45052，exit=2，`error CS`=0。

证据文件：
- `Docs/QA/Evidence/DIAG-SKILL/FND00B06_red_run1.log`
- `Docs/QA/Evidence/DIAG-SKILL/fnd00b06_red_Assertions.jsonl`
- `Docs/QA/Evidence/DIAG-SKILL/fnd00b06_red_Summary.json`
- `Docs/QA/Evidence/DIAG-SKILL/fnd00b06_red_ExitCode.txt`

`Animation.Hit` 原始字段（原样）：

| 字段 | 值 |
|---|---|
| `pass` / `status` | `false` / `product_fail` |
| `preState` | **`Locomotion`** |
| `hitEvents` | `1` |
| `hpBefore` → `hpAfter` | `100` → `95` |
| `finalAmount` | `5` |
| `hitStateObserved` | `false` |
| `hitState` | `null` |
| 采样首帧 | `stateName=Locomotion`, `grounded=true`, `speedX=0`, `speedZ=0`, `normalizedTime=0.01255` |
| `playerHpUnaffected` | `100` |

→ 施加伤害前 Animator **已经**在 Locomotion 稳态（Grounded=true、速度参数=0）。工单预判的"非 Locomotion 态"不成立。

## 2. 与 §6.2 停止规则的对应

> "若 `preState` 已经是 `Locomotion` 但 `Hit_A` 仍未触发，说明定案与证据矛盾 → 停止本步，保留 red 证据并在 REPORT 写'定案不成立'，不得硬改夹具凑绿。"

已按此执行：
- 未修改 `Assets/_Project/Tests/FND00B/Runtime/FND00B06_ProbeRunner.cs`（工作区中该文件的既有改动全部来自 SPEC-01-CLEANUP，本单 0 hunk）。
- 未执行工单 §6.3 的"显式置 Locomotion 参数"补丁（对现象无效，见 §3）。
- 未执行 §6.4 的 green N=3。

## 3. 证据指向的真实原因（供下一张工单定案）

### 3.1 关键事实
- `Hit_A` 的进入过渡只存在于 `Locomotion` 状态（`PlayerBase.controller:259-283` 的过渡被 `Locomotion` 的过渡列表 `controller:609` 引用），条件为 `Hit` 触发器，`HasExitTime=0`。
- **生产代码中唯一调用 `Animator.SetTrigger("Hit")` 的位置**：
  - `Assets/_Project/Scripts/Units/CharacterBrain.cs:33-38`
    ```csharp
    // 受击反馈：统一伤害入口（UnitHealth.ApplyDamage/TakeHit）触发动画器 Hit → Hit_A
    void OnDamaged(DamageInfo info)
    {
        Animator a = unitAnimator != null ? unitAnimator : GetComponentInChildren<Animator>();
        if (a != null) a.SetTrigger("Hit");
    }
    ```
  - 订阅发生在 `CharacterBrain.Start()`（`CharacterBrain.cs:21-26`）：`_health.OnHit += OnDamaged;`
- FND00B06 的夹具在 **Instantiate 之后、Start 之前** 就禁用了 `CharacterBrain`：
  - `Assets/_Project/Tests/FND00B/Runtime/FND00B06_ProbeRunner.cs:1192-1193`
    ```csharp
    var brain = _animFixtureGo.GetComponent<CharacterBrain>();
    if (brain != null) brain.enabled = false;
    ```
  - 组件被禁用后，`Start()` 不会执行 → **`OnHit` 从未订阅** → 伤害发生但 `SetTrigger("Hit")` 永远不会被调用 → `Hit_A` 不进。

### 3.2 交叉验证
- SPEC01 既有断言 `Animation.PostCombatAndHit`（本单 run1）在**真实玩家骑士**（`CharacterBrain` 正常启用）上通过：
  - `entry = UnitHealth.ApplyDamage(5f,...)`，`observedState = Hit_A`，`onHitCount = 1`。
  - 同一伤害入口、同一动画器，只要 `CharacterBrain.Start` 跑过，`Hit_A` 就出现。
- 因此：**产品受击动画链路正常；FND00B06 的 Hit 断言失败是夹具把生产触发器源一起禁用导致的"夹具病"，但病因不是 Locomotion 相位，而是 OnHit 订阅缺失。**

## 4. 建议的夹具修法（下一张工单，本单未执行）
任选其一，且必须保持"经生产 `UnitHealth.ApplyDamage` 施加伤害 → 生产订阅者触发动画"的链路：
1. **让 `CharacterBrain.Start()` 先执行再禁用**：Instantiate → `yield return null`（Start 当帧执行并订阅）→ 再 `brain.enabled = false`。事件订阅与组件启用状态无关，后续 `OnDamaged` 仍会执行。
2. 保持 `CharacterBrain` 启用，把夹具放在无敌人/无指令的位置，靠"无目标"自然静止。
> 方案 1 改动最小，但仍需在报告中说明"夹具短暂启用大脑一帧"的理由。

## 5. before/after diff
- `FND00B06_ProbeRunner.cs`：**无 diff**（本单未触碰）。
- 因此不存在 green run；`Animation.Hit` 仍为 `product_fail`，等待下一张工单按 §3/§4 修复夹具后重跑 N=3。

---

## 6. green（SPEC-DIAG-SKILL-1 修复后，N=3）

修复内容：`CreateAnimationFixture` 不再在 `Instantiate` 后立刻禁用 `CharacterBrain`；改由 `HitAnimationFixture` 先让 `Awake/Start` 走完（2 帧）并只读校验 `UnitHealth.OnHit` 已有订阅（`hitSubscribedAfterStart`），之后再禁用夹具大脑；受击前把 `Grounded/SpeedX/SpeedZ` 置稳态并轮询等待 `Locomotion`（上限 2s，不成立则 `BlockedAssertion`）。
- 未在夹具里手动 `SetTrigger("Hit")`：动画触发器仍只由生产 `CharacterBrain.OnDamaged` 发出。

命令（与 red 同一条，未设 `FND00B06_RUN`，故 `run` 恒为 1）：
```powershell
& "D:\unity\2022.3.62f3c1\Editor\Unity.exe" -batchmode -projectPath "D:\unity\Dungeon" `
  -executeMethod FND00B06_BatchAcceptance.Run `
  -logFile "D:\unity\Dungeon\Docs\QA\Evidence\DIAG-SKILL\FND00B06_green_run<N>.log"
```

| 次 | Summary | Assertions | ExitCode | `Animation.Hit` |
|---|---|---|---|---|
| run1 | `06_Summary_green_run1.json` | `06_Assertions_green_run1.jsonl` | `06_ExitCode_green_run1.txt` = 0 | pass |
| run2 | `06_Summary_green_run2.json` | `06_Assertions_green_run2.jsonl` | `06_ExitCode_green_run2.txt` = 0 | pass |
| run3 | `06_Summary_green_run3.json` | `06_Assertions_green_run3.jsonl` | `06_ExitCode_green_run3.txt` = 0 | pass |

三次汇总完全一致：`pass=12, fail=0, blocked=0, infraFail=0, observed=2`，`exitReason=completed`，`error CS` 计数 = 0。

`Animation.Hit` 三次 actual 完全相同：
```json
{"hitEvents":1,"hpBefore":100,"hpAfter":95,"finalAmount":5,
 "preState":"Locomotion","hitStateObserved":true,"hitState":"Hit_A","playerHpUnaffected":100}
```
data 关键字段（三次一致）：`brainEnabledAtCreate=true`、`brainSettleFrames=2`、`hitSubscribedAfterStart=true`、`brainDisabledBeforeDamage=true`、`locomotionWaitFrames=0`、`locomotionWaitSeconds=0`。

red → green 唯一变化（逐条断言对比 14 条）：`Animation.Hit`：`false/product_fail` → `true/pass`；其余 13 条（含 `Animation.DownedOrDeath`）的 pass/status 与 red 基线逐条相同，无回退。`Animation.PostCombatState` 仍为 `observed`（不计入通过/失败，与基线一致）。

> 注意：套件固定写 `Docs/QA/Evidence/FND-00B-06/06_Assertions.jsonl`、`06_Summary_run1.json`、`06_ExitCode_run1.txt`（`run=1` 会先清空断言文件）。三次运行后该目录为本轮 green 产物；每次运行的快照已复制到 `Docs/QA/Evidence/DIAG-SKILL/06_*_green_runN.*`，red 证据（`fnd00b06_red_*`）未被覆盖。
