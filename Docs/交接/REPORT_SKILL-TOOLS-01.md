# REPORT_SKILL-TOOLS-01 — 调试键位与 SPEC01 动态遭遇基线

- 任务：`Docs/Handoff/TASK_SKILL-TOOLS-01.md`
- 风险等级：常规
- 执行日期：2026-11-05
- 分支/基线：当前分支；基线提交 `75de2329`
- 结论：**自动化部分完成并通过（SPEC01 10/10 ×3、exit=0）**；3 项人工目检 **未运行（待所有者目检）**

## 1. 结果摘要

| 项 | 结果 |
|---|---|
| 三闸门自证 | 开工前 `Get-Process Unity` 计数 = **0**，`Temp/UnityLockfile` 存在性 = **False**；每次 batchmode 运行前同样为 0 |
| 编译 | 第 1 轮 **red**：`DebugTestTools.cs(84,9) error CS0103: EditorSceneManager`（缺 `using UnityEditor.SceneManagement;`，3 次同报）；补齐 using 后 green，`error CS` 计数 0 |
| SPEC01 连续 3 次 | **10/10 pass，fail=0，blocked=0，infraFail=0，observed=0，退出码 0**（3 次全部） |
| 既有 10 条断言 | 全部 pass，无回退 |
| `Regression.FND02A` | 改为“场景敌人实际位置 + 相对接近”，三次 pass：敌人实测 `(-2.72, 0.083, 15.8)`（非旧写死 `-12`），起点距 15.8m → 终点距 10.8m（朝敌缩短 5m），真实寻路 `PathComplete`、2 个拐点、单帧最大位移 ≤0.0125m |
| Console 错误 | 3 次运行 `consoleErrorCount=0`；运行日志无 `error CS`、无真实 Exception（命中关键字者仅为断言文本里的“无异常”字样） |
| 资产边界 | `git status` 无 `Assets/Scenes`、`Prefabs`、`*.controller`、`*.asset`、`ProjectSettings`、`Packages` 改动 |
| 人工目检 AC-3/4/5 | **未运行（待所有者目检）**，本报告不代填 |

## 2. AC 回填

| AC | 断言 ID | 回填 | 证据 |
|---|---|---|---|
| AC-1 场景骷髅任意合法位置时 FND02A 仍有效 | `Regression.FND02A` | **pass**（三次）：基线取自场景实际位置 `z=15.8`，相对接近 5m，自动开局 `gameStarted=true`、`enemyCount=1`、`navMeshOk=true`、`PathComplete`+2 拐点、`consoleErrors=0` | `Docs/QA/Evidence/SKILL-TOOLS-01/SPEC01_run1..3_Assertions.jsonl`、`..._Summary.json`、`..._Log` |
| AC-2 连续 3 次 10/10 | `all` | **pass**：3 次均 `assertionLinesInFile=10, pass=10, fail=0`，进程退出码均 **0** | `SPEC01_run{1,2,3}_ExitCode.txt`（内容 `0`）+ `SPEC01_run{1,2,3}.log` 中 `[SPEC01] finish ... exit=0` |
| AC-3 Play 中按 F5 新增 1 只存活骷髅 | 人工 | **未运行（待所有者目检）** | 待补 |
| AC-4 Play 中按 F9 重载场景 | 人工 | **未运行（待所有者目检）** | 待补 |
| AC-5 Play 中按 F10 满血/满蓝/冷却清零 | 人工 | **未运行（待所有者目检）** | 待补 |
| AC-6 无场景/预制体/Animator/`.asset` 改动 | 静态 | **pass**：`git status --porcelain` 仅本单 3 个 `.cs` + 新增 1 `.cs`/`.meta` + 证据目录 + 两个既有 `.csproj`（开工前已是 modified，未触碰、未还原） | 见 §4.3 |

## 3. 证据锚点

主证据（本单目录，只追加）：

| 文件 | 说明 |
|---|---|
| `Docs/QA/Evidence/SKILL-TOOLS-01/SPEC01_compile_fail1.log` | 第 1 轮编译 red 原始日志（CS0103 ×3） |
| `.../SPEC01_run1.log` / `run2.log` / `run3.log` | 最终代码的 3 次 batchmode 原始日志（含 `[SPEC01][ASSERT]`、`[SPEC01][SUMMARY]`、`finish ... exit=0`） |
| `.../SPEC01_run{1,2,3}_Assertions.jsonl` | 10 条断言的 JSONL |
| `.../SPEC01_run{1,2,3}_Summary.json` | pass/fail/blocked/infra 汇总 |
| `.../SPEC01_run{1,2,3}_ExitCode.txt` | 内容 `0` |
| `.../SPEC01_run1_prefix.log`、`..._prefix_Assertions.jsonl`、`..._prefix_Summary.json`、`..._prefix_ExitCode.txt` | 测量修正前的首跑（green 但 `traveled=4.5009`），保留以便追溯，不作为最终证据 |

canonical 目录（`Docs/QA/Evidence/SPEC-01/`，**只追加**，用 `SPEC01_EVIDENCE_TAG=skilltools01` 打标签，未覆盖任何既有文件）：
`SPEC01_Green_skilltools01_run{1,2,3}_{Assertions.jsonl,Summary.json,ExitCode.txt}`、`SPEC01_Runs_skilltools01.jsonl`（追加）。

## 4. 测试数据

### 4.1 三次运行汇总

| Run | 断言条数 | pass | fail | blocked | infraFail | observed | 进程退出码 | summary run 字段 |
|---|---|---|---|---|---|---|---|---|
| 1 | 10 | 10 | 0 | 0 | 0 | 0 | 0 | 1 |
| 2 | 10 | 10 | 0 | 0 | 0 | 0 | 0 | 2 |
| 3 | 10 | 10 | 0 | 0 | 0 | 0 | 0 | 3 |

断言清单（三次均 pass）：`Locomotion.AllPlayersMoveParam`、`Regression.FND02A`、`Attack.DualPathUnified`、`Clock.MeleeNormalizedWindow`、`Clock.RangedOneShotPerCycle`、`Animation.PostCombatAndHit`、`UI.DamageNumberScale`、`UI.SpawnAnchor`、`Skill.CastSmoke`、`Skill.InputWired`。

### 4.2 `Regression.FND02A` 动态基线实测值

| 字段 | run1 | run2 | run3 |
|---|---|---|---|
| `enemyCount` | 1 | 1 | 1 |
| `enemyStartPosition` | (-2.72, 0.0833334, 15.8) | 同 | 同 |
| `approachStartDistance` | 15.8 | 15.8 | 15.8 |
| `approachPlannedStandOff` | 10.8 | 10.8 | 10.8 |
| `approachEndDistance` | 10.8 | 10.8 | 10.8 |
| `approachTraveled` | 5 | 5 | 5 |
| `approachMaxSingleFrameDisplacement` | 0.00708079 | 0.01247120 | 0.01226485 |
| `approachPathStatus` / `approachPathCorners` | PathComplete / 2 | 同 | 同 |
| `navMeshOk` / `gameStarted` | true / true | 同 | 同 |
| `consoleErrorCount` | 0 | 0 | 0 |

（旧版本断言要求 `enemyStartPosition ≈ (-2.72, 0, -12)`、`start≥11.8`、`end∈[6.6,7.35]`、`traveled≥4`；本次全部改为由场景实际位置推导，见 §5。）

### 4.3 资产边界快照（`git status --porcelain`）

```
 M Assembly-CSharp-Editor.csproj          ← 开工前即 modified（自动生成，未触碰、未还原）
 M Assembly-CSharp.csproj                 ← 同上
 M Assets/_Project/Scripts/Units/SkillSystem.cs
 M Assets/_Project/Scripts/Units/UnitMana.cs
 M Assets/_Project/Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs
 M Docs/Handoff/TASK_SKILL-TOOLS-01.md    ← 工单本身（派单前已 modified）
?? Assets/_Project/Scripts/Debug/DebugTestTools.cs
?? Assets/_Project/Scripts/Debug/DebugTestTools.cs.meta
?? Docs/QA/Evidence/SKILL-TOOLS-01/
?? Docs/QA/Evidence/SPEC-01/SPEC01_Green_skilltools01_run{1,2,3}_*  / SPEC01_Runs_skilltools01.jsonl
```

无 `Assets/Scenes/**`、`Assets/_Project/Prefabs/**`、`*.controller`、`*.asset`、`ProjectSettings/**`、`Packages/**`、`CURRENT.md` 改动。新增 `.cs` 导致的 `.csproj` 变更属工单预批的隐含延伸；两个 `.csproj` 由用户决定是否提交，本单既不提交也不还原。

## 5. 实现原理

1. `Assets/_Project/Scripts/Debug/DebugTestTools.cs:162` — `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` 在进入 Play 后判断 `Application.isBatchMode`、活动场景名 == `DungeonWorld`，才创建 `DebugTestTools` 宿主；不改场景、不进包（整文件 `#if UNITY_EDITOR`）。
2. `DebugTestTools.cs:131` — `DebugTestToolsHost` 用 `DontDestroyOnLoad` 存活于场景重载，`OnSceneLoaded` 收到重载事件后重新记录生成点（因此 F9 之后 F5 仍落在重载后场景的真实位置）；`Update` 里轮询 `F5/F9/F10`。
3. `DebugTestTools.cs:29` — 生成点来源是**场景里实际的那只** `Skeleton_Warrior`（team=Monster + 名字前缀），`F5`（`:45`）用 `AssetDatabase.LoadAssetAtPath` 加载 `Assets/_Project/Prefabs/Skeleton_Warrior.prefab` 后在该点 `Instantiate`，并自检“存活 + team=Monster”。
4. `DebugTestTools.cs:76` / `:89` — `F9` 用 `EditorSceneManager.LoadSceneInPlayMode(activeScene.path, LoadSceneParameters(Single))`；`F10` 遍历 team=Player 的 `UnitHealth`：`dead → Revive(maxHp)` 否则 `HealFull()`，再 `GetComponentInParent<UnitMana>().Refill()`、`GetComponentInParent<SkillSystem>().ResetCooldowns()`。两个小 API：`Assets/_Project/Scripts/Units/UnitMana.cs:21`（`current = max`）、`Assets/_Project/Scripts/Units/SkillSystem.cs:30`（四槽 `_cds[i] = default(Cooldown)`，不改技能数据）。
5. `Assets/_Project/Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs:451-466` — 动态基线：`start = 骑士当前站位`、`enemyPos = 场景敌人实际位置`，方向 `(start-enemyPos)` 归一化后取 `dest = enemyPos + dir * (起点距离 - 5m)`（下限 2m 站位），再 `NavMesh.SamplePosition` 落到 NavMesh；`:503-508` 把停稳阶段位移也累加进 `traveled`，避免终点距离在 settle 后继续缩短而少记；`:1406-1412` 判定改为“读到实际位置 + 终点比起点近 ≥ 计划增益一半 + `traveled ≥ 直线缩短量 - 0.5` + 单帧位移 ≤0.6 + `PathComplete` + 拐点>1”，仍走 `CharacterBrain.CommandMove` 生产指令，无 Warp、无坐标直写。

## 6. 受影响模块导航

| 模块 | 位置 | 与本单关系 |
|---|---|---|
| 调试工具 | `Assets/_Project/Scripts/Debug/`（与 `DebugCameraSwitcher`、`Mirror` 同目录） | 新增 `DebugTestTools.cs`；目录与 `.meta` 约定与既有文件一致 |
| 单位数值 | `Assets/_Project/Scripts/Units/UnitMana.cs`、`SkillSystem.cs` | 各新增 1 个重置 API，职责不变，未改施放/回复流程 |
| 生命重置 | `Assets/_Project/Scripts/Units/UnitHealth.cs` | 未改动；复用既有 `HealFull()/Revive(float)`（`Revive` 仅在 dead 时生效） |
| 规格回归 | `Assets/_Project/Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs` | 仅改 `AssertRegressionFND02A` 接近段、`RecordRegressionFinal` 判定与字段、`Finish` 的 summary 字段、新增 `CountSceneEnemies` 诊断 |
| 自动化入口 | `Docs/QA/Evidence/...` + `Assets/_Project/Tests/SPEC01/Editor/SPEC01_BatchAcceptance.cs` | 未改动入口；用 `SPEC01_EVIDENCE_TAG=skilltools01` 打标签避免覆盖 canonical 证据 |

## 7. 未决项

1. **AC-3/4/5 待所有者目检**（本报告按要求不填写目检结论）。建议步骤：打开 `DungeonWorld` → Play → 控制台应出现 `[DebugTestTools] host created in DungeonWorld; keys F5/F9/F10` 与 `spawn point recorded at (-2.72, 0.083, 15.8)`；按 F5 应出现 `F5 spawned Skeleton_Warrior_Debug1 ...; team=Monster; hp=…`；按 F9 场景重载且日志再次出现 `spawn point recorded`；掉血/耗蓝/进 CD 后按 F10 应出现 `F10 reset players; healed=…; revived=…; manaRefilled=3; cooldownReset=3`。
2. **F5 生成点依赖场景中已有骷髅**：若骷髅由运行期 spawner 生成，进入 Play 瞬间尚未存在，则宿主只在按 F5 时重试一次记录；仍找不到会 `LogWarning` 并放弃（不猜坐标）。如所有者希望“骷髅被清完后仍能 F5”，需要改成本单范围外的持久化记录（例如场景标记点），建议另开任务。
3. **batchmode 下不挂载调试宿主**（`Application.isBatchMode` 直接 return）为本单的刻意隔离：避免调试入口出现在 SPEC01 自动化观测面里。若后续有“自动化里也用 F5 补怪”的需求，需另开任务并显式开关。
4. **发现但未改**：`AssertRegressionFND02A` 内 `bool pathOk` 赋值后未使用（本单之前即如此），属既有代码，未在范围内顺手改。
5. **`git status` 中两个 `.csproj`** 为 Unity 自动生成、开工前即 modified，按工单不提交、不还原。

## 8. 建议提交（由用户执行）

**Title**：`SKILL-TOOLS-01: 调试键位 F5/F9/F10 + SPEC01 动态遭遇基线`

**Body**：
新增编辑器内调试工具（F5 补骷髅 / F9 重载场景 / F10 重置队伍，不改场景、不进包），并让 SPEC01 的 Fixed-Regression 用场景敌人实际位置与相对接近路线替代写死坐标，三次连续 10/10。

**三句话版本（工单 §5 要求）**
1. 这个工具解决“手动测技能时非得退出 Play 才能补怪/重开/重置”的问题：Play 中按 F5 补一只骷髅、F9 重载场景、F10 把队伍补满血满蓝并清冷却。
2. 用法：进 Play 后自动挂载在 `DungeonWorld`（Console 有 `[DebugTestTools] host created`），直接按 F5/F9/F10 即可；不需要往场景里拖任何组件。
3. SPEC01 不再写死坐标：`Regression.FND02A` 现在读场景敌人的实际位置，算出“朝敌人方向缩短 5m”的站位点，再用生产移动指令走过去，用“终点比起点更近 + 单帧位移不超 0.6m + 寻路 PathComplete”判 pass，所以所有者随便挪骷髅都不会让回归失效。

**讲稿**：**不适用** —— 本单是开发工具 + 测试基线，不改变玩家可见功能与玩法（工单 §5 已如此约定）。
