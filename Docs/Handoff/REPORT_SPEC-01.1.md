# REPORT_SPEC-01.1 — 飘字出生点修复（S01-2 返工）

> 状态：**待所有者目检 + 外部顾问复核**（不标 `Done`，不改 `CURRENT.md`）
> 风险：**中风险**（单系统脚本 + 测试断言；本报告附完整生产 diff）
> 执行者：【执行者】DeepSeek V4.1 Flash
> 执行时间：2026-11-05 01:39 – 01:56（本地）
> HEAD：`de92f096d674fab373ba0f32cbff160380500e52`
> 唯一证据目录：`Docs/QA/Evidence/SPEC-01/`（新文件全部带 `_fix` 后缀，原始证据未覆盖）

---

## 1. 结果摘要

| 阶段 | 断言结果 | 退出码 | 结论 |
|---|---|---|---|
| red（生产未改，仅加断言） | 7 pass + **1 product_fail**（`UI.SpawnAnchor`），blocked=0，**infra=0** | 2 | 根因复现：像素距离 **400px** |
| green run1（锚点修复后） | **8/8 pass**，blocked=0，infra=0 | 0 | `UI.SpawnAnchor` 距离 ≈ 0px |
| green run2（锚点修复后） | **8/8 pass**，blocked=0，infra=0 | 0 | 与 run1 关键数据一致 |

- 生产改动**仅 1 处、+3 行**（`DamageNumber.cs` 的 `Play()` 中设置实例 `anchorMin=anchorMax=(0.5,0.5)`；2 行注释 + 1 行赋值）。
- 样式参数（字号/颜色/曲线/扇区/`ChestHeight=1.2f`）**一律未动**。
- 未改 `CombatFeedback.cs`、`RangedCombat.cs`、任何资产（`.unity`/`.prefab`/`.controller`/`.anim`/`.overrideController`/`.meta`）。
- 未执行任何 git 写操作；`CURRENT.md` 未改；`B2`/`B6` 未回填。
- 原始 SPEC-01 证据（`SPEC01_Red_*`、`SPEC01_Assertions.jsonl`、`SPEC01_Summary_run*`、`SPEC01_Runs.jsonl` 等）mtime 全部停留在 0:27–0:56，**未被覆盖**。

### 前置闸门（Unity 进程）

开工自检发现 1 个 `Unity.exe`（PID 47052，Hub 启动的交互式编辑器）卡在关闭后残留（Editor.log 已出现完整 `Application.Shutdown.*`，`Temp/` 与 `UnityLockfile` 均不存在）。已按所有者授权 `Stop-Process -Id 47052 -Force`，仅此一个 PID；清理后 `Get-Process Unity` 为空、无锁文件。全过程见 `SPEC01_Pre_fix.txt`。

---

## 2. HEAD 与完整命令

**HEAD**：`de92f096d674fab373ba0f32cbff160380500e52`（工作区存在 SPEC-01 前序未提交改动，与本单无关）

**red（run=0）**

```powershell
$env:SPEC01_MODE='red'; $env:SPEC01_EVIDENCE_TAG='fix'; $env:SPEC01_RUN='0'
& 'D:\unity\2022.3.62f3c1\Editor\Unity.exe' -batchmode -projectPath 'D:\unity\Dungeon' `
  -executeMethod SPEC01_BatchAcceptance.Run `
  -logFile 'D:\unity\Dungeon\Docs\QA\Evidence\SPEC-01\SPEC01_Red_fix_Automation.log'
```

**green run1 / run2**

```powershell
$env:SPEC01_MODE='green'; $env:SPEC01_EVIDENCE_TAG='fix'
$unity='D:\unity\2022.3.62f3c1\Editor\Unity.exe'
foreach ($n in 1,2) {
  $env:SPEC01_RUN="$n"
  $log="D:\unity\Dungeon\Docs\QA\Evidence\SPEC-01\SPEC01_Green_fix_run${n}_Automation.log"
  Start-Process -FilePath $unity -Wait -PassThru -NoNewWindow -ArgumentList `
    '-batchmode','-projectPath','D:\unity\Dungeon', `
    '-executeMethod','SPEC01_BatchAcceptance.Run','-logFile',$log
}
```

> 注：PowerShell 用 `&` 调用 GUI 子系统程序（Unity.exe）不会等待进程结束，首轮曾因此提前返回；改用 `Start-Process -Wait` 后获得真实退出码。此为执行环境细节，不影响产物。

**运行环境（断言数据中记录）**：Unity `2022.3.62f3c1`；batchmode 虚拟屏幕 `640×480`；共享画布 `1662.769×1247.077`（CanvasScaler ScaleWithScreenSize 1920×1080, match 0.5）；`cameraSource=Camera.main`。

---

## 3. Red 证据（逐条 product_fail，含实际像素距离）

文件：`SPEC01_Red_fix_Assertions.jsonl`（8 行）、`SPEC01_Red_fix_Summary.json`（pass=7 / fail=1 / blocked=0 / infraFail=0）、`SPEC01_Red_fix_ExitCode.txt`（2）、`SPEC01_Red_fix_Automation.log`。

**product_fail 清单（仅 1 条）**

| 断言 | expected | actual | 实际像素距离 | 判定 |
|---|---|---|---|---|
| `UI.SpawnAnchor` | 飘字屏幕位置与 `target.position + up*ChestHeight` 的相机投影距离 **≤ 50px** | `actual=(15.455, -292.973)`；`expected=(335.455, -52.973)`；`anchorMin=(0,0)`；`anchorMax=(0,0)`；`pivot=(0.5,0.5)` | **400px** | product_fail |

**其余 7 条（red 中全部 pass，无回归）**

| 断言 | status | 关键数据 |
|---|---|---|
| `Locomotion.AllPlayersMoveParam` | pass | playersPass=True; controllerPass=True; blendParameter=SpeedZ; thresholdsOk=True |
| `Attack.DualPathUnified` | pass | designatedPass=True; autoPass=True; swordTrigger=AttackMelee; rangedTriggerOk=True |
| `Clock.MeleeNormalizedWindow` | pass | hits=12; inWindow=12; window=[0.35,0.55]; attackSpeedParam=1 |
| `Clock.RangedOneShotPerCycle` | pass | rangedPass=True |
| `Animation.PostCombatAndHit` | pass | allLocomotion=True; settle=1.865s; hitState=Hit_A; hp 100→95; onHit=1 |
| `UI.DamageNumberScale` | pass | namedCanvasCount=1; smallMult=1.5; largeMult=5; smallFont=24; largeFont=80; activeAfterStress=32 |
| `Regression.FND02A` | pass | gameStarted=True; enemyFound=True; guidOk=True; navOk=True; approachOk=True; consoleErrors=0 |

**偏移量自证（根因闭环）**

- `actual - expected = (15.455-335.455, -292.973-(-52.973)) = (-320, -240)`
- 屏幕 `640×480` → `(-320, -240)` **恰为半屏**；像素距离 `√(320²+240²) = 400`
- 换算画布单位：`(831.4, 623.5) × scaleFactor 0.3849 = (320, 240)`，与 `canvasSize/2` 一致
- `ageAtSample=0`、`sampleWaitSeconds=0.1195`（即 `CombatFeedback.damageNumberDelay`）→ 采样发生在实例被激活的**同一帧**、首次 `Update()` 之前，距离不含 `_initialOffset` 运动分量，误差源唯一。

---

## 4. Green run1 / run2 逐断言表

文件：`SPEC01_Green_fix_run1_Assertions.jsonl`、`SPEC01_Green_fix_run2_Assertions.jsonl`（各 8 行）、`SPEC01_Green_fix_run{1,2}_Summary.json`、`SPEC01_Green_fix_run{1,2}_ExitCode.txt`（均为 0）、`SPEC01_Green_fix_run{1,2}_Automation.log`。

| # | 断言 | run1 | run2 |
|---|---|---|---|
| 1 | `Locomotion.AllPlayersMoveParam` | pass | pass |
| 2 | `Attack.DualPathUnified` | pass | pass |
| 3 | `Clock.MeleeNormalizedWindow` | pass | pass |
| 4 | `Clock.RangedOneShotPerCycle` | pass | pass |
| 5 | `Animation.PostCombatAndHit` | pass | pass |
| 6 | `UI.DamageNumberScale` | pass | pass |
| 7 | **`UI.SpawnAnchor`** | **pass（distancePx=7.63e-06）** | **pass（distancePx=7.63e-06）** |
| 8 | `Regression.FND02A` | pass | pass |

`UI.SpawnAnchor` 两轮明细：

```
run1: distancePx=0; actual=(335.455,-52.973); expected=(335.455,-52.973);
      anchorMin=(0.5,0.5); anchorMax=(0.5,0.5); pivot=(0.5,0.5); ageAtSample=0
run2: distancePx=0; actual=(335.455,-52.973); expected=(335.455,-52.973);
      anchorMin=(0.5,0.5); anchorMax=(0.5,0.5); pivot=(0.5,0.5); ageAtSample=0
```

（JSON 中 `distancePx` 精确值 `7.62939453E-06`，即浮点噪声；断言 actual 字符串按 `0.###` 显示为 `0`。）

---

## 5. 关键数据一致性（run1 vs run2）

| 字段 | run1 | run2 | 一致 |
|---|---|---|---|
| `UI.SpawnAnchor.distancePx` | 7.62939453E-06 | 7.62939453E-06 | ✅ |
| `UI.SpawnAnchor.anchorMin/anchorMax/pivot` | 0.5,0.5 / 0.5,0.5 / 0.5,0.5 | 同 | ✅ |
| `UI.SpawnAnchor.actual/expected` | (335.455,-52.973) | (335.455,-52.973) | ✅ |
| `canvasSize` / `screen` | 1662.769×1247.077 / 640×480 | 同 | ✅ |
| `UI.DamageNumberScale` small/large 倍率 | 1.5 / 5 | 1.5 / 5 | ✅ |
| small/large 字号 | 24 / 80 | 24 / 80 | ✅ |
| `namedCanvasCount` / 压力后活跃·总实例 | 1 / 32·32 | 1 / 32·32 | ✅ |
| `Clock.MeleeNormalizedWindow` 窗口/命中数 | [0.35,0.55] / 12 | [0.35,0.55] / 12 | ✅ |
| `Clock.RangedOneShotPerCycle` 弓/法 draw→fire | 13→12 / 4→3 | 13→12 / 4→3 | ✅ |
| `Animation.PostCombatAndHit.settle` | 1.855s | 1.853s | 时序量，允许微差 |
| `Regression.FND02A` 全项 | 全 True, consoleErrors=0 | 全 True, consoleErrors=0 | ✅ |

差异仅限与帧时序相关的秒数（settle、最小发射间隔小数点后第 3 位），属正常抖动；判定字段全部一致。

---

## 6. 生产 diff（完整，仅此一处）

基线 = 本单开工时工作区（HEAD + SPEC-01 前序未提交改动）。文件 `Assets/_Project/Scripts/Combat/DamageNumber.cs`，`Play()` 内：

```diff
@@ -256,6 +256,9 @@ void Play(Vector3 worldPos, float amount, DamageElement element)
         var rt = (RectTransform)transform;
+        // 与 ScreenPointToLocalPointInRectangle 的画布中心原点对齐：实例锚点必须是中心 (0.5,0.5)，
+        // 沿用创建时的左下锚点 (0,0) 会让全部飘字系统性左下偏移半屏。
+        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
         rt.anchoredPosition = _start;
         rt.localScale = Vector3.one * PopStartScale;
     }
```

**验证“只有这一处”**：改动前文件 304 行 → 改动后 307 行，净增 3 行（2 注释 + 1 赋值）；`CreateInstance()` 中创建时的 `anchorMin/Max=zero` 与 `Update()` 运动逻辑**均未改动**。

> 说明：`git diff HEAD -- DamageNumber.cs` 显示 286 insertions / 31 deletions，那是 SPEC-01 前序“世界空间→共享 Overlay 画布”改造的**未提交存量**，不是本单改动；本单增量即上方 3 行。测试侧两个文件（`SPEC01_ProbeRunner.cs`、`SPEC01_Evidence.cs`）整个 `Assets/_Project/Tests/SPEC01/` 目录在 git 中仍为未跟踪（`??`），无法用 `git diff` 呈现；其完整内容见工作区文件，断言结果见 `_fix` 证据文件。

---

## 7. 变更文件清单

**生产（1 个）**

- `Assets/_Project/Scripts/Combat/DamageNumber.cs`（+3 行）

**测试（2 个，均位于未跟踪目录 `Assets/_Project/Tests/SPEC01/`）**

- `Assets/_Project/Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs`
  - 新增 `AssertSpawnAnchor()`；`Start()` 注册 `UI.SpawnAnchor`；类注释断言列表补一项。
- `Assets/_Project/Tests/SPEC01/Runtime/SPEC01_Evidence.cs`
  - 新增环境变量 `SPEC01_EVIDENCE_TAG`：置为 `fix` 时输出 `SPEC01_Red_fix_*` / `SPEC01_Green_fix_runN_*` / `SPEC01_Runs_fix.jsonl`；未设置时命名与原行为逐字节一致（不影响历史口径）。

**证据（新增，均带 `_fix` / `_Pre_fix`）**

- `Docs/QA/Evidence/SPEC-01/SPEC01_Pre_fix.txt`
- `Docs/QA/Evidence/SPEC-01/SPEC01_Red_fix_Assertions.jsonl`
- `Docs/QA/Evidence/SPEC-01/SPEC01_Red_fix_Automation.log`
- `Docs/QA/Evidence/SPEC-01/SPEC01_Red_fix_Summary.json`
- `Docs/QA/Evidence/SPEC-01/SPEC01_Red_fix_ExitCode.txt`
- `Docs/QA/Evidence/SPEC-01/SPEC01_Green_fix_run1_Assertions.jsonl`
- `Docs/QA/Evidence/SPEC-01/SPEC01_Green_fix_run1_Automation.log`
- `Docs/QA/Evidence/SPEC-01/SPEC01_Green_fix_run1_Summary.json`
- `Docs/QA/Evidence/SPEC-01/SPEC01_Green_fix_run1_ExitCode.txt`
- `Docs/QA/Evidence/SPEC-01/SPEC01_Green_fix_run2_Assertions.jsonl`
- `Docs/QA/Evidence/SPEC-01/SPEC01_Green_fix_run2_Automation.log`
- `Docs/QA/Evidence/SPEC-01/SPEC01_Green_fix_run2_Summary.json`
- `Docs/QA/Evidence/SPEC-01/SPEC01_Green_fix_run2_ExitCode.txt`
- `Docs/QA/Evidence/SPEC-01/SPEC01_Runs_fix.jsonl`

**报告**

- `Docs/Handoff/REPORT_SPEC-01.1.md`（本文件）

---

## 8. 检查结果（可复核项）

| 检查 | 方法 | 结果 |
|---|---|---|
| red 仅 1 条 product_fail 且为 `UI.SpawnAnchor` | `SPEC01_Red_fix_Summary.json` + jsonl | pass=7, fail=1, blocked=0, infraFail=0 ✅ |
| red 其余 7 条 pass | 逐行 `status` | ✅ |
| green 两轮 8/8 pass | 两份 Summary + jsonl | pass=8, fail=0, infra=0 ×2 ✅ |
| green 两轮 `UI.SpawnAnchor` ≤50px | `distancePx` | 7.63e-06px ×2 ✅ |
| 编译错误 | `Select-String "error CS|Compilation failed|Exception:"` 两份 green 日志 | 0 命中 ✅ |
| 运行期 Console 错误 | `Regression.FND02A.consoleErrors` | 0 ✅ |
| 原始证据未被覆盖 | 目录 mtime 对比 | 全部保持 0:27–0:56 ✅ |
| 无残留 Unity 进程/锁 | `Get-Process Unity` / `Test-Path Temp/UnityLockfile` | 0 / False ✅ |

未运行项：编辑器内 Game 视图人工目检（见 §11）、非 640×480 分辨率复验（见 §10）。

---

## 9. 实现原理（面向项目所有者）

**根因**：`DamageNumber` 实例的 `RectTransform` 出生时被建成左下锚点（`anchorMin=anchorMax=(0,0)`），而 `Play()` 里算出的 `_start` 来自 `RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out local)`——这个 API 返回的是**以画布 pivot（中心）为原点**的 local 坐标。`anchoredPosition` 的语义是“相对锚点的偏移”，锚点在左下时，同一个数值就被解释成“从左下角量起”，于是每个飘字都凭空多出一个 `(-画布宽/2, -画布高/2)` 的位移。本次 red 实测 `actual-expected=(-320,-240)`，正好是 640×480 屏幕的一半，与推导完全吻合；落在左/下象限时该偏移直接把文字推出屏幕，这就是“有时不可见”。

**修复**：在 `Play()` 给 `anchoredPosition` 赋值**之前**，把实例锚点设为 `(0.5,0.5)`（pivot 本来就是 0.5），使“锚点原点”与“local 坐标原点”重合，`anchoredPosition=_start` 就落在正确位置。`_start` 本身不动，因此 `Update()` 里 `_start + _dir*offset + up*drift` 的运动叠加仍与原坐标系同源，无需改动。

**为什么选这个方案**：① 只改一处、零资产、零新字段，回滚就是删 3 行；② 不动 `_start` 的算法，避免把“坐标换算”和“锚点语义”两个问题混在一起；③ 不新建 RectTransform 或改 `CreateInstance` 的创建参数，池中所有实例每次 `Play()` 都会重新锚定，行为对池复用天然一致；④ 不碰样式（字号/颜色/曲线/扇区/`ChestHeight`），符合“原神视觉草案冻结”的边界。

**为什么不采用其它做法**：改 `_start` 手动补偿半屏，需要读取画布尺寸并处理分辨率/缩放变化，脆弱且把错误固化进数据；在 `Update()` 里补偿，会与运动叠加耦合，未来调曲线易再错；改 `CreateInstance` 只在首次创建生效，语义上不如在唯一赋值点 `Play()` 明确（本单也按工单限定在 `Play()`）。

**证据链**：red 400px（半屏，锚点 `(0,0)`）→ green 两轮 ≈0px（锚点 `(0.5,0.5)`，采样时 `age=0`，无运动分量），同一断言、同一采样点、同一生产路径，闭合。

---

## 10. 受影响模块导航说明

**`DamageNumber`（`Assets/_Project/Scripts/Combat/DamageNumber.cs`）**

- 职责：单个共享 `Screen Space-Overlay` 画布 + 对象池（默认 32）+ 伤害分档字号 + 出生投影 + 上飘/弹出/淡出运动。纯脚本零资产。
- 入口：`static Show(Vector3 worldPos, float amount, DamageElement element)` → `Rent()` → `Play()`（**本单改动点**：屏幕投影 → 画布 local → 设锚点 → 写 `anchoredPosition`）→ 每帧 `Update()` 叠加运动 → `ReturnToPool()`。
- 数据流：`UnitHealth.ApplyDamage` → `DamageDealtEvent` → `CombatFeedback.ShowNumber`（延迟 0.12s，取 `target.transform.position`）→ `DamageNumber.Show` → `Play()` 内部再抬 `ChestHeight=1.2m` 并投影。
- 影响面：锚点只决定 `anchoredPosition` 的参考原点，不参与字号、配色、曲线、池容量；对其它系统零影响（`DamageNumber` 无外部写入面，仅静态只读属性供测试）。

**测试与证据链**

- 入口：`SPEC01_BatchAcceptance.Run`（Editor，batchmode）→ 打开 `DungeonWorld` → EnterPlaymode → `SPEC01_ProbeRunner.Start()` 顺序跑 8 条断言 → `SPEC01_Evidence` 写 jsonl / Summary / ExitCode / Runs 索引 → `EditorApplication.Exit(code)`。
- 命名开关：`SPEC01_EVIDENCE_TAG=fix` 时全部产物带 `_fix`，未设置时保持 SPEC-01 原命名。
- 新断言：`AssertSpawnAnchor()` 用生产路径 `ApplyDamage(25f)` 造字，取实例 `RectTransform`，以 `RectTransformUtility.WorldToScreenPoint(null, rt.position)` 与 `Camera.main.WorldToScreenPoint(target.position + up*ChestHeight)` 比较，容差 50px；采样在实例激活同帧（`age=0`）。

---

## 11. 人工目检（**待目检**，执行者未代填）

- [ ] `S01.1-1`：飘字从角色胸口位置冒出、全屏任意位置均可见（含左下/右下象限）。

---

## 12. 未覆盖项

1. **分辨率/宽高比**：仅验证 batchmode `640×480`。数学上偏移恒为 `(-屏宽/2, -屏高/2)`，修复后与分辨率无关，但未在 1920×1080 等分辨率实测。
2. **编辑器 Game 视图目检**：未做（`S01.1-1` 待所有者）。
3. **`ChestHeight` 试值**：按所有者指令保持 `1.2f`，未调参。
4. **`ScreenPointToLocalPointInRectangle` 返回 false 的分支**：未构造（该分支 `_start=0`，修复后居中，行为仍自洽）。
5. **多画布/嵌套 Canvas**：当前工程仅一个共享 Overlay 画布，未覆盖其它宿主形态。
6. **首帧运动容差边界**：采样依赖 `age=0`（本次两轮均命中）。若未来 batchmode 帧率极低（单帧 >0.056s），首帧 `_initialOffset` 位移理论上可接近 50px 容差上限；本次实测 `age=0`，未触发。

---

## 13. 遗留问题

1. **测试文件未纳入 git**：`Assets/_Project/Tests/SPEC01/` 整体仍是未跟踪状态（SPEC-01 前序遗留），提交时需一并 `git add`，否则断言与命名开关不会进版本库。
2. **`overlayCanvasBeforeStress=2`**：这是 SPEC-01 既有状态（UI 画布 + 飘字画布各一），非本单引入，`canvasStable` 判定仍为 pass，不在本单范围。
3. **本单未闭合上位单**：`TASK_SPEC-01` 整体仍未收口，本单仅闭合 `S01-2` 的“出生点”部分；样式类目检仍按原计划另行处理。
4. **中风险复核**：按工单要求，本报告需所有者送外部顾问复核生产 diff 后再终审。

---

## 14. 建议提交

> 执行者不执行任何 git 写操作；以下供所有者参考。代码/资产改动由所有者亲自提交。

**建议 `git add` 清单**

```
Assets/_Project/Scripts/Combat/DamageNumber.cs
Assets/_Project/Tests/SPEC01/                      # 含 .meta（若尚未提交）
Docs/QA/Evidence/SPEC-01/SPEC01_Pre_fix.txt
Docs/QA/Evidence/SPEC-01/SPEC01_Red_fix_Assertions.jsonl
Docs/QA/Evidence/SPEC-01/SPEC01_Red_fix_Automation.log
Docs/QA/Evidence/SPEC-01/SPEC01_Red_fix_Summary.json
Docs/QA/Evidence/SPEC-01/SPEC01_Red_fix_ExitCode.txt
Docs/QA/Evidence/SPEC-01/SPEC01_Green_fix_run1_Assertions.jsonl
Docs/QA/Evidence/SPEC-01/SPEC01_Green_fix_run1_Automation.log
Docs/QA/Evidence/SPEC-01/SPEC01_Green_fix_run1_Summary.json
Docs/QA/Evidence/SPEC-01/SPEC01_Green_fix_run1_ExitCode.txt
Docs/QA/Evidence/SPEC-01/SPEC01_Green_fix_run2_Assertions.jsonl
Docs/QA/Evidence/SPEC-01/SPEC01_Green_fix_run2_Automation.log
Docs/QA/Evidence/SPEC-01/SPEC01_Green_fix_run2_Summary.json
Docs/QA/Evidence/SPEC-01/SPEC01_Green_fix_run2_ExitCode.txt
Docs/QA/Evidence/SPEC-01/SPEC01_Runs_fix.jsonl
Docs/Handoff/REPORT_SPEC-01.1.md
```

**建议提交信息（混合改动，建议拆两个 commit）**

commit 1（生产修复）：

```
fix(combat): 修正飘字出生点锚点坐标系错配

为什么: DamageNumber 实例沿用左下锚点(0,0)，而 ScreenPointToLocalPointInRectangle 返回画布中心原点坐标，导致全体飘字系统性左下偏移半屏、落在左/下象限时出屏不可见；在 Play() 赋值 anchoredPosition 前将实例锚点设为 (0.5,0.5)，red 400px → green ≈0px。
```

commit 2（测试 + 证据 + 报告）：

```
test(spec01): 新增 UI.SpawnAnchor 断言并归档 SPEC-01.1 修复证据

为什么: 为“飘字出生点”提供可重复的像素级回归防线，并以 _fix 后缀隔离本次返工证据、不覆盖 SPEC-01 原始证据。
```
