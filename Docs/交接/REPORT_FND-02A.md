# REPORT_FND-02A — MVP 产品启动与固定遭遇装配

> 执行者：【执行者】（opencode-go / deepseek-v4.1-flash）
> 工单：`Docs/Handoff/TASK_FND-02A_MVP产品启动与固定遭遇装配.md`
> 风险：高风险（公共流程状态 + 生产主场景）
> 状态：**执行完成，待用户人工目检 V02A-1 与外部顾问复核；不得由执行者标记 Done**

---

## 1. 结果摘要

- **产品启动接线已闭合**：`GameFlow.Start()` 在场景存在 `team == Team.Player` 的 `UnitHealth` 时写 `GameStarted = true; Time.timeScale = 1f;`；真实 `DungeonWorld` 进入 Play 后第 1 帧完成唯一一次 `false→true`，无主菜单阻塞。
- **固定遭遇已装配**：`DungeonWorld.unity` 新增 **1 个** `Skeleton_Warrior.prefab`（GUID `18dec5e6af31178468b146e0440bd081`，场景 PrefabInstance fileID `767899593`），位置 `(-2.72, 0, -12)`、旋转 `(0,0,0)`；来源与位置在两次独立运行中完全一致。
- **TDD**：red 有效（`Startup.ProductEntry`、`Encounter.SceneCensus` 因产品状态失败，`infra=0`，退出码 2）→ 最小实现 → 同一入口 green **连续 2 次 7/7 全通过**，退出码 0。
- **真实移动**：Knight 经生产选择/移动链从 `12.00m` 接近到 `7.00m`（`PathComplete`、2 个拐角、单帧最大位移 ≤0.18m，无 Warp、无坐标写入、无直接 `SetDestination`）。
- **回归**：FND-01 等价移动路线（出生区 → 4m 外平地）两次均到达，Console 无新增错误/异常（0/0），导航无异常。
- **未覆盖/需人工**：`V02A-1` 真实右键与肉眼确认仍为“待目检”，由用户填写；本单**不**代表普攻、伤害、动画、团灭或完整 MVP 通过。

---

## 2. 仓库状态

| 项 | 值 |
|---|---|
| 分支 | `docs/fnd-00-baseline` |
| 完整 HEAD | `85a14c854c43a6170b9d929a86771c1e8d5bf563`（2026-11-04 15:06:41 +0800，`docs(plan): FND-02补架构依据两栏与结构债清单SD-01~04`） |
| 本单未提交 | 全部改动留在工作区，由用户亲自提交（执行者未执行任何 git 写操作） |
| Unity | 2022.3.62f3c1（`D:\unity\2022.3.62f3c1\Editor\Unity.exe`） |
| 执行前状态 | `Unity.exe` 无残留、`Temp/UnityLockfile` = False（详见 `00_前置确认.txt`） |

开工前 `git status --porcelain -uall`（原样）见 `00_前置确认.txt`；当时仅有 `Docs/Handoff/` 的 3 项（`CURRENT.md` 修改 + 提案/工单未跟踪），用户已裁决豁免。

---

## 3. 唯一入口与完整命令

唯一自动化入口：`FND02A_BatchAcceptance.Run`（Editor）；运行器 `FND02A_ProbeRunner`；证据写入 `FND02A_Evidence`。场景装配为一次性入口 `FND02A_BatchAcceptance.AssembleScene`。

```powershell
$unity = "D:\unity\2022.3.62f3c1\Editor\Unity.exe"
$repo  = "D:\unity\Dungeon"
$ev    = "D:\unity\Dungeon\Docs\QA\Evidence\FND-02A"

# red（生产代码/场景未改，仅新增测试资产）
$env:FND02A_MODE = "red"; $env:FND02A_RUN = "0"
Start-Process -FilePath $unity -Wait -PassThru -ArgumentList @(
  '-batchmode','-projectPath',$repo,
  '-executeMethod','FND02A_BatchAcceptance.Run',
  '-logFile',"$ev\02A_Red_Automation.log")

# 一次性场景装配（编辑器 API，不手改 YAML）
Start-Process -FilePath $unity -Wait -PassThru -ArgumentList @(
  '-batchmode','-projectPath',$repo,
  '-executeMethod','FND02A_BatchAcceptance.AssembleScene',
  '-logFile',"$ev\02A_Assemble.log")

# green run1 / run2（同一入口，连续 2 次）
$env:FND02A_MODE = "green"; $env:FND02A_RUN = "1"
Start-Process -FilePath $unity -Wait -PassThru -ArgumentList @(
  '-batchmode','-projectPath',$repo,
  '-executeMethod','FND02A_BatchAcceptance.Run',
  '-logFile',"$ev\02A_Automation_run1.log")
$env:FND02A_RUN = "2"
Start-Process -FilePath $unity -Wait -PassThru -ArgumentList @(
  '-batchmode','-projectPath',$repo,
  '-executeMethod','FND02A_BatchAcceptance.Run',
  '-logFile',"$ev\02A_Automation_run2.log")
```

退出码由运行器写入 `02A_*_ExitCode*.txt`，并与 `Start-Process -PassThru` 的 `ExitCode` 一致（red=2、run1=0、run2=0）。

---

## 4. red（实施前，生产代码与场景保持未改）

- 证据：`02A_Red_Assertions.jsonl`、`02A_Red_Summary.json`、`02A_Red_ExitCode.txt`（=2）、`02A_Red_Automation.log`
- 汇总：`pass=1 / fail=3 / blocked=3 / infraFail=0`；seed `20261104`；15:58:14 → 15:58:18
- 有效性判定：`infraFail=0`、无编译错误（日志 `error CS` 命中 0、`Exception` 命中 0），**两项必需断言因当前产品状态失败**：

| 断言 | red 实测 | 判定 |
|---|---|---|
| `Startup.ProductEntry` | `gameStarted=False; timeScale=1; mainMenuPanelNull=True; mainMenuInstances=0`；首帧观测 `firstObservedValue=false`、`firstTrueRealtime=-1` | `product_fail`（有效 red） |
| `Startup.OnceAndStable` | `risingEdges=0`（1s 时 0，2s 窗后 0）；`players=3`；`monsters=0` | `product_fail` |
| `Encounter.SceneCensus` | `monsters=0; spawners=0; testUnitHealth=0` | `product_fail`（有效 red） |
| `Encounter.FixedIdentity` / `NavMesh` / `RealApproach` | 场景无敌人 → `blocked`（不计通过） | `blocked` |
| `Regression.NoNewErrors` | `arrived=True`（3→0.497m）、`consoleErrors=0` | `pass`（仅作基线） |

---

## 5. green（同一入口连续 2 次）

| 项 | run1 | run2 |
|---|---|---|
| 证据日志 | `02A_Automation_run1.log`（144,773 B） | `02A_Automation_run2.log`（138,070 B） |
| 汇总 | `02A_Summary_run1.json` | `02A_Summary_run2.json` |
| 退出码 | `02A_ExitCode_run1.txt` = **0** | `02A_ExitCode_run2.txt` = **0** |
| 断言 | 7/7 pass，fail 0、blocked 0、infra 0 | 7/7 pass，fail 0、blocked 0、infra 0 |
| seed | 20261105 | 20261106 |
| 时间 | 16:15:41 → 16:15:47 | 16:16:20 → 16:16:26 |
| `error CS` / `Exception` 日志命中 | 0 / 0 | 0 / 0 |

两次结果**一致**（同一 GUID、同一 fileID、同一坐标、同一接近起止距离）；未出现间歇性/时序抖动，故未触发 N≥3 升级。

---

## 6. 逐项断言表（green run1 / run2 实测）

| 断言 ID | 客观预期 | run1 实测 | run2 实测 |
|---|---|---|---|
| `Startup.ProductEntry` | 产品自身令 `GameStarted=true`、`timeScale=1`，无主菜单阻塞，测试未写入 | `gameStarted=True; timeScale=1; mainMenuPanelNull=True; mainMenuInstances=0; gameFlowInstances=1`；首帧 `firstObservedValue=true`、`firstTrueFrame=1`、`firstTrueRealtime=3.488`；`testWroteGameStarted=false` | 同（`firstTrueRealtime=3.304`、`firstTrueFrame=1`） |
| `Startup.OnceAndStable` | 观察窗内仅 1 次 `false→true`；玩家仍 3；敌人不重复生成 | `risingEdges=1`（1s 时 1、2s 窗后 1）；`players=3`（Archer/Knight/Mage，hp 80/100/70）；`monsters=1`（两次采样同 1）；`UnitHealth.OnDeath` 订阅总数 4 | 同 |
| `Encounter.SceneCensus` | 恰 1 个普通敌人（非测试内存实例），无新增刷怪来源 | `monsters=1`（`Skeleton_Warrior`，team=Monster，hp=60，active=true，`scenePath=Assets/Scenes/DungeonWorld.unity`）；`spawners=0`；`testUnitHealth=0`；`forestGeneratorCount=1`（装饰用，非刷怪器） | 同 |
| `Encounter.FixedIdentity` | 来源为 `Skeleton_Warrior.prefab`，位置/旋转等于批准值 | `sceneSourceGuid=18dec5e6af31178468b146e0440bd081`；fileID `767899593`；pos `(-2.72, 0.08333334, -12)`；`rotDelta=0°`；`monsterStatsGuid=520f29cc…`（SkeletonWarrior.asset） | 完全一致（pos 位逐位相同） |
| `Encounter.NavMesh` | Agent 启用、`isOnNavMesh=true`、生产 AI 未禁用 | `agentEnabled=true; isOnNavMesh=true; sampled=true`（`SamplePosition` 距离 0）；`agentTypeID=0`、radius 0.5、height 2、speed 2.2、baseOffset 0；`brainEnabled=true`、`brain.agent/stats` 均赋值 | 同 |
| `Encounter.RealApproach` | 经现有选择/移动命令从出生区接近固定遭遇 | `start=12.00m → end=7.00m`；`moved=4.503m`；`maxStep=0.176m`；`PathComplete`、corners=2；耗时 1.384s；`usedWarp=false`、`wrotePlayerPosition=false`、`calledAgentSetDestinationDirectly=false` | `start=12 → end=7`；`moved=4.500m`；`maxStep=0.0098m`；其余同 |
| `Regression.NoNewErrors` | 无新增导航异常/阻塞错误，FND-01 移动路线可用 | `arrived=True`（3.00 → 0.497m）；`consoleErrorCount=0`；`navMeshErrors=[]` | `arrived=True`（3.00 → 0.499m）；`consoleErrorCount=0` |

**坐标一致性说明（重要）**：批准值 `y=0`，运行时 `y=0.08333334`，`deltaY=0.0833` 在容差 0.10 内。该偏差不是摆放误差：同一时刻 `NavMesh.SamplePosition` 采样点 y 恰为 `0.08333334`（`navMeshSampleDistance=0`），即 **Agent 落到 NavMesh 表面**的偏移；x/z 与批准值逐位一致，旋转 delta 0°。这也解释了阶段 1 候选值里出现的 `0.08333`。

---

## 7. 原始证据与日志

目录：`Docs/QA/Evidence/FND-02A/`

| 文件 | 说明 |
|---|---|
| `00_前置确认.txt` | 阶段 1 只读结论、用户裁决、Unity 退出核实 |
| `02A_Red_Assertions.jsonl` / `02A_Red_Summary.json` / `02A_Red_ExitCode.txt` / `02A_Red_Automation.log` | red |
| `02A_Assemble.log` | 场景装配（含装配前后实例数、fileID、GUID） |
| `02A_Assertions.jsonl` | green run1（截断重建 7 行）+ run2（追加，共 14 行） |
| `02A_Automation_run1.log` / `02A_Automation_run2.log` | green 原始 Unity 日志 |
| `02A_Summary_run1.json` / `02A_Summary_run2.json` | green 汇总 |
| `02A_ExitCode_run1.txt` / `02A_ExitCode_run2.txt` | green 退出码（0 / 0） |
| `02A_Assertions_failed_attempt1..3.jsonl`、`02A_Automation_failed_attempt1..3.log`、`02A_Automation_failed_attempt4_compile.log`、对应 `02A_Summary/ExitCode_failed_attempt*` | **测试基础设施迭代记录（全部保留，未删除、未挑选）**，见 §13 |
| `02A_人工目检.md` | `V02A-1` 仅“待目检” |

原始 Unity 日志体积较大（每个 130–150 KB），按工单效率约束不整文件转贴；关键判定均已在 §6 表格与 JSONL 中结构化留证。

---

## 8. 夹具边界（本单产品断言不使用夹具）

- 未创建任何内存敌人/玩家夹具；`Encounter.*` 的判定对象是**场景文件中保存的产品实例**（`testFixtureUnitHealthCount=0`）。
- 唯一的测试侧对象是 `FND02A_EdgeObserver`（只在 `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` 创建、**只读**采样 `GameStarted`/`timeScale`/Console）与 `FND02A_Runner`；两者都不写 `GameStarted`、不调用开局入口、不持有 `UnitHealth`，结束时销毁。
- 移动输入使用与生产点击链**同一方法**：`SelectionManager.SingleSelect`（private，反射，`HandleClick` 内同款）→ `SelectionManager.ForEachSelectedBrain(b => b.CommandMove(Vector3))`（与 `PlayerCommandController` 右键同款）；未使用 `NavMeshAgent.Warp`，未写玩家坐标，未直接 `SetDestination`。
- 场景装配只经编辑器 API（`PrefabUtility.InstantiatePrefab` + `EditorSceneManager.SaveScene`），未新增代码路径或运行时生成。

---

## 9. 变更文件与完整 diff

变更文件（相对本单前）：

| 文件 | 类型 | 说明 |
|---|---|---|
| `Assets/_Project/Scripts/Core/GameFlow.cs` | 修改（+17/-0） | 产品自动开局唯一所有者 |
| `Assets/Scenes/DungeonWorld.unity` | 修改（+69/-0） | 仅新增 1 个 PrefabInstance + 1 条 SceneRoots |
| `Assembly-CSharp.csproj` | 修改（+2/-0） | 新增 2 条 Runtime 测试 Compile（用户裁决 3 批准随测纳入） |
| `Assembly-CSharp-Editor.csproj` | 修改（+1/-0） | 新增 1 条 Editor 测试 Compile |
| `Assets/_Project/Tests/FND02A/**`（3 个 `.cs` + 3 个 `.cs.meta` + 3 个目录 `.meta`） | 新增 | 测试入口/运行器/证据写入 |
| `Docs/QA/Evidence/FND-02A/**` | 新增 | 本单证据 |
| `Docs/Handoff/REPORT_FND-02A.md` | 新增 | 本报告 |

未修改：`Skeleton_Warrior.prefab`、Animator/Controller、`ProjectSettings`、`Packages`、`NavMesh.asset`、旧 FND-00B 测试、旧 QA 证据、`CURRENT.md`、`Playground.unity`、`SampleScene.unity`。

### 9.1 GameFlow.cs（完整）

```diff
diff --git a/Assets/_Project/Scripts/Core/GameFlow.cs b/Assets/_Project/Scripts/Core/GameFlow.cs
index dfed3ebe..93c2014f 100644
--- a/Assets/_Project/Scripts/Core/GameFlow.cs
+++ b/Assets/_Project/Scripts/Core/GameFlow.cs
@@ -18,6 +18,23 @@ public class GameFlow : MonoBehaviour
     void Start()
     {
         GameStarted = false;
+
+        // ★ 产品自动开局：本场景存在可操作队伍时直接进入局内（无主菜单步骤）。
+        // 只有存在 team==Player 的 UnitHealth 才开局，避免无队伍场景（如 Playground）被误判团灭。
+        if (HasPlayerTeam())
+        {
+            GameStarted = true;
+            Time.timeScale = 1f;
+        }
+    }
+
+    static bool HasPlayerTeam()
+    {
+        foreach (var h in FindObjectsByType<UnitHealth>(FindObjectsSortMode.None))
+        {
+            if (h != null && h.team == Team.Player) return true;
+        }
+        return false;
     }
 
     void Update()
```

### 9.2 DungeonWorld.unity（完整，+69/-0）

```diff
diff --git a/Assets/Scenes/DungeonWorld.unity b/Assets/Scenes/DungeonWorld.unity
index 3f2a3c6a..51d5ae92 100644
--- a/Assets/Scenes/DungeonWorld.unity
+++ b/Assets/Scenes/DungeonWorld.unity
@@ -1116,6 +1116,74 @@ MonoBehaviour:
   m_Name: 
   m_EditorClassIdentifier: 
   mat: {fileID: 2100000, guid: 6101c243c1057e5438a31ce91a7c454e, type: 2}
+--- !u!1001 &767899593
+PrefabInstance:
+  m_ObjectHideFlags: 0
+  serializedVersion: 2
+  m_Modification:
+    serializedVersion: 3
+    m_TransformParent: {fileID: 0}
+    m_Modifications:
+    - target: {fileID: 4704138966014272613, guid: 18dec5e6af31178468b146e0440bd081,
+        type: 3}
+      propertyPath: m_LocalPosition.x
+      value: -2.72
+      objectReference: {fileID: 0}
+    - target: {fileID: 4704138966014272613, guid: 18dec5e6af31178468b146e0440bd081,
+        type: 3}
+      propertyPath: m_LocalPosition.y
+      value: 0
+      objectReference: {fileID: 0}
+    - target: {fileID: 4704138966014272613, guid: 18dec5e6af31178468b146e0440bd081,
+        type: 3}
+      propertyPath: m_LocalPosition.z
+      value: -12
+      objectReference: {fileID: 0}
+    - target: {fileID: 4704138966014272613, guid: 18dec5e6af31178468b146e0440bd081,
+        type: 3}
+      propertyPath: m_LocalRotation.w
+      value: 1
+      objectReference: {fileID: 0}
+    - target: {fileID: 4704138966014272613, guid: 18dec5e6af31178468b146e0440bd081,
+        type: 3}
+      propertyPath: m_LocalRotation.x
+      value: 0
+      objectReference: {fileID: 0}
+    - target: {fileID: 4704138966014272613, guid: 18dec5e6af31178468b146e0440bd081,
+        type: 3}
+      propertyPath: m_LocalRotation.y
+      value: 0
+      objectReference: {fileID: 0}
+    - target: {fileID: 4704138966014272613, guid: 18dec5e6af31178468b146e0440bd081,
+        type: 3}
+      propertyPath: m_LocalRotation.z
+      value: 0
+      objectReference: {fileID: 0}
+    - target: {fileID: 4704138966014272613, guid: 18dec5e6af31178468b146e0440bd081,
+        type: 3}
+      propertyPath: m_LocalEulerAnglesHint.x
+      value: 0
+      objectReference: {fileID: 0}
+    - target: {fileID: 4704138966014272613, guid: 18dec5e6af31178468b146e0440bd081,
+        type: 3}
+      propertyPath: m_LocalEulerAnglesHint.y
+      value: 0
+      objectReference: {fileID: 0}
+    - target: {fileID: 4704138966014272613, guid: 18dec5e6af31178468b146e0440bd081,
+        type: 3}
+      propertyPath: m_LocalEulerAnglesHint.z
+      value: 0
+      objectReference: {fileID: 0}
+    - target: {fileID: 5333288747275058911, guid: 18dec5e6af31178468b146e0440bd081,
+        type: 3}
+      propertyPath: m_Name
+      value: Skeleton_Warrior
+      objectReference: {fileID: 0}
+    m_RemovedComponents: []
+    m_RemovedGameObjects: []
+    m_AddedGameObjects: []
+    m_AddedComponents: []
+  m_SourcePrefab: {fileID: 100100000, guid: 18dec5e6af31178468b146e0440bd081, type: 3}
 --- !u!1 &806595974
 GameObject:
   m_ObjectHideFlags: 0
@@ -3691,3 +3759,4 @@ SceneRoots:
   - {fileID: 1287161071}
   - {fileID: 2127526991}
   - {fileID: 1850084002}
+  - {fileID: 767899593}
```

### 9.3 两个 .csproj（完整，Compile 引用随测纳入）

```diff
diff --git a/Assembly-CSharp-Editor.csproj b/Assembly-CSharp-Editor.csproj
index 9ed5635b..46919e69 100644
--- a/Assembly-CSharp-Editor.csproj
+++ b/Assembly-CSharp-Editor.csproj
@@ -67,6 +67,7 @@
     <Compile Include="Assets\_Project\Tests\FND00B\Editor\FND00B04_BatchAcceptance.cs" />
     <Compile Include="Assets\TutorialInfo\Scripts\Editor\ReadmeEditor.cs" />
     <Compile Include="Assets\_Project\Tests\FND00B\Editor\FND00B05_BatchAcceptance.cs" />
+    <Compile Include="Assets\_Project\Tests\FND02A\Editor\FND02A_BatchAcceptance.cs" />
     <Compile Include="Assets\_Project\Tests\FND00B\Editor\FND00B06_BatchAcceptance.cs" />
   </ItemGroup>
   <ItemGroup>
diff --git a/Assembly-CSharp.csproj b/Assembly-CSharp.csproj
index a103dd28..3b89b584 100644
--- a/Assembly-CSharp.csproj
+++ b/Assembly-CSharp.csproj
@@ -115,6 +115,7 @@
     <Compile Include="Assets\_Project\Scripts\Units\MonsterBrain.cs" />
     <Compile Include="Assets\_Project\Scripts\Units\Visibility.cs" />
     <Compile Include="Assets\_Project\Scripts\Combat\SwordSwing.cs" />
+    <Compile Include="Assets\_Project\Tests\FND02A\Runtime\FND02A_Evidence.cs" />
     <Compile Include="Assets\_Project\Scripts\Combat\ICharacterCombat.cs" />
     <Compile Include="Assets\_Project\Scripts\Player\SkillIndicator.cs" />
     <Compile Include="Assets\_Project\Scripts\Core\PauseManager.cs" />
@@ -138,6 +139,7 @@
     <Compile Include="Assets\StarterAssets\FirstPersonController\Scripts\FirstPersonController.cs" />
     <Compile Include="Assets\StarterAssets\InputSystem\StarterAssetsInputs.cs" />
     <Compile Include="Assets\_Project\Scripts\Combat\DamageNumber.cs" />
+    <Compile Include="Assets\_Project\Tests\FND02A\Runtime\FND02A_ProbeRunner.cs" />
     <Compile Include="Assets\_Project\Scripts\Units\SkillSystem.cs" />
     <Compile Include="Assets\_Project\Scripts\UI\TransitionFader.cs" />
     <Compile Include="Assets\_Project\Scripts\Combat\SimplePool.cs" />
```

---

## 10. 未覆盖项

1. `V02A-1` 真实鼠标右键 + 肉眼确认（自动断言只覆盖等价输入与状态）。
2. 普攻/伤害/动画/团灭/宝箱/技能/镜头等战斗与流程行为（属 FND-02B 及之后）。
3. `Playground.unity`、`SampleScene.unity` 的实际 Play 行为未跑；只做了静态核查（无 `UnitHealth` → 不自动开局）并加门控，未做场景级运行验证。
4. 帧率上限/多分辨率/输入设备差异对开局时机的绝对时间无意义（断言用状态与上升沿，不用固定秒数）。
5. NavMesh 覆盖范围只验证了敌人点、玩家出生点与 12m 走廊（`SamplePosition`/`isOnNavMesh`），未做全图覆盖统计。

---

## 11. 实现原理（面向项目所有者，≤500 字）

**根因**：局内启动无所有者。`GameFlow.Start()` 置 `GameStarted=false`；唯一置 true 的 `MainMenuPanel.OnClose()` 因场景无该面板、槽位 null 不可达，`Update` 永远早退，UI 闸门关闭，场景也没有敌人。

**方案**：`GameFlow` 独占开局——`Start()` 先清零，再扫描是否存在 `team==Player` 的 `UnitHealth`，有则 `GameStarted=true; timeScale=1`。以“有玩家单位”而非场景名判断，避免无队伍场景误判团灭。

**遭遇**：经编辑器 API 实例化 `Skeleton_Warrior.prefab` 到 `(-2.72,0,-12)`：位于 ForestGen 的 -Z 清空走廊（无树石挖洞），距三玩家 12.0/12.5/13.3m，均超玩家仇恨/射程 10，开局不会被 Archer 射杀（原 +7 两秒内被射死）。y 场面 0，运行期吸附 NavMesh 面变 0.0833（SamplePosition 距离 0 可证）。

---

## 12. 受影响模块导航说明（≤200 字）

`GameFlow`(Core)：局内流程所有者，负责开局、团灭、重开；入口 `Start`/`Update`。`UIManager`(UI)：面板中枢，只读 `GameStarted` 控制 Tab/I/C/Esc，`Start` 仅在主菜单槽非空时开菜单。`DungeonWorld`：组合根，3 玩家 + 1 骷髅。链路：加载→Awake→Start→右键→CommandMove→Agent。

---

## 13. 测试基础设施迭代（保留全部失败记录）

| 尝试 | 结果 | 原因 | 修正 |
|---|---|---|---|
| red | 有效失败（exit 2） | 产品未开局、场景 0 敌人 | — |
| green 尝试 1 | exit 2 | ①`Encounter.FixedIdentity` 取预制体路径返回空串；②`Encounter.RealApproach` 在自动减速帧采样，终点 7.498m 越界 | 改为“到点后等速度收敛再量终点”；GUID 改用多种来源 |
| green 尝试 2 | exit 2 | GUID 仍取不到（`GetCorrespondingObjectFromSource` 对该预制体变体返回 null） | 改为以场景 YAML `m_SourcePrefab` GUID（编辑期只读解析）为来源证据 |
| green 尝试 3 | exit 2 | 判定额外依赖 `PrefabUtility.IsPartOfPrefabInstance/GetPrefabInstanceStatus`，该预制体变体根是嵌套 FBX 对象，运行期返回 `NotAPrefab` | 诊断为 Unity 变体限制，改以“场景 YAML GUID + 运行时内容指纹（stats 资产 GUID + 名称）”判定，API 值仅留诊断 |
| green 尝试 4 | exit 1 | 测试代码笔误 `PosTolZ` 编译失败（无断言产生） | 修正拼写 |
| **green run1 / run2** | **exit 0 / 0，7/7** | — | — |

以上迭代只改测试资产，**未放宽产品断言语义**；生产代码与场景在 red 之后仅发生本单批准的 2 处改动；red 的失败项（`ProductEntry`/`OnceAndStable`/`SceneCensus`）与这些测试侧修正无关。

---

## 14. 历史测试 FND00B 04/05/06 的行为演进（预期，不判回归失败）

`GameFlow.Start()` 现在会在存在玩家单位的场景自动开局，因此**重跑历史 FND00B 04/05/06 批次时，`GameStarted` 的初始值将由基线期的 `false` 变为 `true`**：

- 这属于本单**预期演进**（SD-01 局内启动接线闭合），不是回归缺陷；**不修改**旧 FND-00B 测试、旧 QA 表或旧证据。
- 影响面：旧批次中依赖“未开局”前置的观察项（如 `GameEnd.TriggerChain` 的产品路径前置、`UIManager` 闸门状态）在新基线下可能直接成立；其历史结论仍按当时 HEAD 与证据解读，不可与本次结果混用。
- 旧批次中唯一写 `GameStarted=true` 的“夹具控制”段落仍会执行赋值/还原，但新基线下其隔离意义下降，需在后续复用前重新评估（本单不改）。

---

## 15. 执行环境（模型 / 订阅）

- 执行者模型完整 ID：**`opencode-go/deepseek-v4.1-flash`**
- 订阅通道：**Go**（provider `opencode-go`）
- 工具能力：只读代码/场景/日志 + 编辑器批处理调用 + 证据/文档写入；未执行任何 `git add/commit/checkout/reset/restore/clean`。

---

## 16. 遗留问题与需复核事项

1. **场景 YAML 手工回退（必须复核）**：`AssembleScene` 保存场景时，Unity 顺带把 `CursorManager` 的 3 个**陈旧**序列化字段名（`infoCursor/infoHotspot/infoColor`）重写成现行字段（`interactCursor/interactHotspot/interactColor`，值为 `null/(0,0)/默认色`，运行时语义等价）。为遵守“不清理任务外 YAML、diff 只含本单变更”，我随后把这 3 行**恢复为原文本**，最终场景 diff 为 `+69/-0`（纯新增）。这是**一次手改 YAML 的例外**，特此披露；恢复后两次 green 均正常加载并 7/7 通过（YAML 合法）。若顾问认为应保留 Unity 的重写，可重新以编辑器保存一次（会再次出现那 3 行改动）。
2. **固定身份判定的 API 限制**：该预制体是“根为嵌套 FBX 对象”的变体，运行期 `PrefabUtility` 链接查询返回 `NotAPrefab`；本单改用“场景 YAML GUID + stats 资产 GUID + 名称”判定，证据强度足够但依赖编辑期解析，后续若预制体结构变化需同步调整。
3. **敌人 aggro 与后续战斗单**：敌人 `aggroRange=10`、玩家 Archer `attackRange=10`；`(-2.72,-12)` 留有 2m 余量。FND-02B 若让玩家更早接敌或改动射程/仇恨，需重新核算该坐标，否则固定遭遇可能被远处自动射杀。
4. **`EscMenuPanel.OnClickRestart`/`GameFlow` 团灭 `LoadScene(buildIndex)`**：`DungeonWorld` 不在 `EditorBuildSettings`（仅 `SampleScene`），重开会加载错误场景。属 FND-02 后续单范围，本单未改。
5. `V02A-1` 仍待用户目检；本单在高风险批次下**不得**由执行者标记 Done。

---

## 17. 建议提交

建议拆 2 个提交（代码/资产与文档分离），均由用户亲自执行：

**提交 1（生产接线 + 固定遭遇 + 测试资产）**

```
git add Assets/_Project/Scripts/Core/GameFlow.cs
git add Assets/Scenes/DungeonWorld.unity
git add Assets/_Project/Tests/FND02A
git add Assembly-CSharp.csproj Assembly-CSharp-Editor.csproj
git commit -m "feat(fnd-02a): 产品自动开局并装配固定骷髅遭遇" -m "GameFlow 在存在玩家队伍时自动开局，DungeonWorld 保存 1 个 Skeleton_Warrior 于 (-2.72,0,-12)，使 MVP 从真实场景 Play 即可进入遭遇。"
```

**提交 2（证据与报告）**

```
git add Docs/QA/Evidence/FND-02A Docs/Handoff/REPORT_FND-02A.md
git commit -m "docs(fnd-02a): 记录启动与固定遭遇的 red/green 证据及报告" -m "保留 red、两次 green 与测试基础设施迭代记录，供外部顾问复核。"
```

**不要提交**（非本单/非 AI 改动）：`Docs/Handoff/CURRENT.md`（【大脑】维护）、`Docs/Handoff/PROPOSAL_FND-02A.md` 与工单（由【大脑】/用户按流程处理）。

提交说明可选补充：外部顾问复核结论、`git rev-parse HEAD` 实际提交哈希、`V02A-1` 目检结果。
