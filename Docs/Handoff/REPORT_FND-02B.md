# REPORT · FND-02B 玩家动画机整备与攻击时钟对齐

> 工单：`Docs/Handoff/TASK_FND-02B_玩家动画机整备与攻击时钟对齐.md`
> 执行者：DeepSeek V4.1 Flash（`opencode/deepseek-v4.1-flash-free`）
> 状态：**实现完成、自动化 7/7 连续 3 次通过；未 Done**（等待人工目检 V02B-1/2/3 + 外部顾问复核 +【大脑】终审）

---

## 1. 结果摘要

| 项 | 结果 |
|---|---|
| red（实现前） | exit **2**，7 条断言 **1 pass / 6 product_fail**，`infraFail=0`，无编译错误 |
| green run1 | exit **0**，**7/7 pass**，`infraFail=0` |
| green run2 | exit **0**，**7/7 pass**，`infraFail=0` |
| green run3 | exit **0**，**7/7 pass**，`infraFail=0` |
| 三次数据一致性 | 完全一致（关键指标见 §5） |
| 改动范围 | `PlayerBase.controller` 5 处 + 6 个脚本；未改预制体、未改场景、未改 `.meta` |
| 未覆盖项 | OverrideController 换 Clip（批准清单未给出具体 Clip/GUID 映射，信息不足） |

结论：自动化验收标准（工单 §5）已满足；人工目检 3 项仍为“待目检”，执行者不代填。

---

## 2. 完整 HEAD 与工作区基线

- 分支：`docs/fnd-00-baseline`
- 完整 HEAD：`c7b2c79bf9c06a647b40ca48da1d826f5a8eee6f`
- Unity：`2022.3.62f3c1`（`D:\Unity\2022.3.62f3c1\Editor\Unity.exe`）
- 开工前工作区（记录于 `Docs/QA/Evidence/FND-02B/00_前置确认.txt`，1708 字节）：
  - Unity 进程：`NO_UNITY_PROCESS`
  - `Temp/UnityLockfile`：`False`
  - 已存在未提交的 FND-02A 基线改动：`Assets/Scenes/DungeonWorld.unity`、`Assets/_Project/Scripts/Core/GameFlow.cs`、`Assembly-CSharp*.csproj`、`Assets/_Project/Tests/FND02A*`、`Docs/Handoff/*`、`Docs/Handoff/CURRENT.md`
- 本次新增改动（相对上述基线）：`Assets/_Project/Art/Animations/PlayerBase.controller` + 6 个脚本 + `Assets/_Project/Tests/FND02B/**` + `Docs/QA/Evidence/FND-02B/**`
- 基线未被回退：FND-02A 的 `GameFlow.cs`、场景、csproj 改动保持原样（本次未触碰）。

---

## 3. 唯一入口与完整命令

唯一入口：`FND02B_BatchAcceptance.Run`（Editor 程序集），内部进入 Play 模式运行 `FND02B_ProbeRunner` 的 7 条断言，结束后按结果 `EditorApplication.Exit(code)`。

每次运行前均确认：无 `Unity.exe` 进程、`Temp/UnityLockfile` 不存在。

red：

```powershell
$env:FND02B_MODE='red'; $env:FND02B_RUN='0'
Start-Process 'D:\Unity\2022.3.62f3c1\Editor\Unity.exe' -Wait -ArgumentList `
  '-batchmode','-projectPath','D:\unity\Dungeon',`
  '-executeMethod','FND02B_BatchAcceptance.Run',`
  '-logFile','Docs\QA\Evidence\FND-02B\02B_Red_Automation.log'
$LASTEXITCODE  # 记录到 02B_Red_ExitCode.txt
```

green（run1/run2/run3 仅 `FND02B_RUN` 与日志/证据文件名不同）：

```powershell
$env:FND02B_MODE='green'; $env:FND02B_RUN='1'   # 依次 1、2、3
Start-Process 'D:\Unity\2022.3.62f3c1\Editor\Unity.exe' -Wait -ArgumentList `
  '-batchmode','-projectPath','D:\unity\Dungeon',`
  '-executeMethod','FND02B_BatchAcceptance.Run',`
  '-logFile','Docs\QA\Evidence\FND-02B\02B_Automation_run1.log'
```

> 注：`Assembly-CSharp*.csproj` 会被 Unity 批处理重写（属既有基线现象，非本次手工修改）。

---

## 4. red 与 green 三次结果

### 4.1 red（实现前，run=0）

- 时间：`2026-11-04 18:19:34` → `18:20:42`
- exit：**2**（`02B_Red_ExitCode.txt`）
- 汇总（`02B_Red_Summary.json`）：`pass=1, fail=6, blocked=0, infraFail=0, assertionLinesInFile=7`
- 编译错误：`error CS` / `Compilation failed` 计数 **0**
- 断言明细（`02B_Red_Assertions.jsonl`）：

| 断言 ID | status | actual（原文） |
|---|---|---|
| `Locomotion.AllPlayersMoveParam` | product_fail | `playersPass=False; controllerPass=False; stateNameExact=False; blend2D=False; usesSpeedZ=False` |
| `Attack.DualPathUnified` | product_fail | `designatedPass=False; autoPass=False` |
| `Clock.MeleeNormalizedWindow` | product_fail | `hits=0; inWindow=0; releaseEntries=0; window=[0.35,0.55]; attackSpeedParam=1; stats=1` |
| `Clock.RangedOneShotPerCycle` | product_fail | `rangedPass=False` |
| `Animation.PostCombatAndHit` | product_fail | `allLocomotion=False; settle=-1s; hitState= Locomotion; hp=100→95; onHit=1` |
| `UI.DamageNumberScale` | product_fail | `instances=1; localScale=0.006 (expected 0.021); fontSize=40; multiplierField=-1; rangeAttr=False` |
| `Regression.FND02A` | pass | `gameStarted=True; enemyFound=True; posOk=True; guidOk=True; navOk=True; approachOk=True; consoleErrors=0` |

### 4.2 green run1 / run2 / run3

| 项 | run1 | run2 | run3 |
|---|---|---|---|
| 时间窗 | 18:27:04→18:27:45 | 18:28:26→18:29:07 | 18:29:36→18:30:17 |
| exit code | 0 | 0 | 0 |
| pass / fail | 7 / 0 | 7 / 0 | 7 / 0 |
| infraFail | 0 | 0 | 0 |
| randomSeed | 20261105 | 20261106 | 20261107 |
| 编译错误计数 | 0 | 0 | 0 |
| `02B_Assertions.jsonl` 行数 | 7 | 14（追加） | 21（追加） |

（`02B_Assertions.jsonl` 通过 `run` 字段区分三次运行；run≤1 覆盖写，run2/3 追加。）

---

## 5. 逐项断言表（green 三次数据）

### 5.1 `Locomotion.AllPlayersMoveParam` — pass ×3

路径：`SelectionManager.SingleSelect(Selectable)` → `ForEachSelectedBrain(b => b.CommandMove(Vector3))`（与右键移动同一条生产链路）。

| 角色 | traveled | peakAgentSpeed | peakAnimatorMoveParam | stateDuringMove | finalAgentSpeed | finalAnimatorMoveParam |
|---|---|---|---|---|---|---|
| Knight_Unit | 2.5517 | 4.0001 | 4.0001 | Locomotion | 0 | 0 |
| Archer_Unit | 2.5519 | 4.0002 | 4.0002 | Locomotion | 0 | 0 |
| Mage_Unit | 2.5513 | 4.0001 | 4.0001 | Locomotion | 0 | 0 |

控制器结构断言：`stateNameExact=true`、`blendTreeIs2D=true`、`blendParameterY="SpeedZ"`、`layerCount=1`。
（red 时状态名为带前导空格的 `" Locomotion"`、混合树为 `m_BlendType=0`（Simple1D，仅 `SpeedX`），故前向移动不驱动动画。）

### 5.2 `Attack.DualPathUnified` — pass ×3

| 路径 | 角色 | 事件 | 状态 | normalizedTime | AttackSpeed 参数 / stats |
|---|---|---|---|---|---|
| CommandAttack | Knight | hit | Attack_Release | 0.3501 | 1 / 1 |
| CommandAttack | Archer | fire | Attack_Release | 0.3384 | 3 / 3 |
| CommandAttack | Mage | fire | Attack_Release | 0.2002 | 0.8 / 0.8 |
| AggroAuto | Knight | hit | Attack_Release | 0.3503 | 1 / 1 |
| AggroAuto | Archer | fire | Attack_Release | 0.3403 | 3 / 3 |
| AggroAuto | Mage | fire | Attack_Release | 0.2000 | 0.8 / 0.8 |

结论：两条路径出伤/出射瞬间均处于 `Attack_Release`，不存在“停在 Locomotion/Idle 却出伤”。

### 5.3 `Clock.MeleeNormalizedWindow` — pass ×3

- 配置窗口：`[0.35, 0.55]`（`SwordSwing.activeStart/activeEnd`）
- 命中记录：`hits=4, inWindow=4`，每次 `state=Attack_Release`，`normalizedTime` 依次 0.35026 / 0.35045 / 0.35055 / 0.35035，`amount=15`
- `releaseEntries=5`、`hits=4`：观察窗内进入释放状态 5 次、结算 4 次；有 1 次进入释放状态未产生命中，本次证据未记录该次的具体原因（不影响断言：4 次命中全部落在窗口内，且单周期仅结算 1 次）
- `attackSpeedParam=1` = `attackSpeedStats=1`（Knight）
- 探针字段说明：`effectiveLayer=1` 实为预制体原始 `attackLayer` 值（字段命名遗留）；控制器 `layerCount=1`，运行时由 `SwordSwing.AnimLayer` clamp 到 0。`drawStateName` 字段同为探针里 `attackStateName` 的原始值（`"Attack"`）。

### 5.4 `Clock.RangedOneShotPerCycle` — pass ×3

观察窗 10.978s。

| 角色 | drawEntries | fireCount | releaseEntries | 窗口 | 最小发射间隔 | 期望周期 | 首次 draw→fire | 延迟上限 |
|---|---|---|---|---|---|---|---|---|
| Archer | 13 | 12 | 12 | [0.2, 0.6] | 0.8720s | 0.8887s | 0.3622s | 0.7888s |
| Mage | 4 | 3 | 4 | [0.2, 0.6] | 2.8594s | 3.7913s | 1.5377s | 2.3852s |

- 每次发射均 `state=Attack_Release`、`normalizedTime∈[0.2,0.6]`（弓手 ≈0.338、法师 ≈0.200）
- `fireCount ≤ releaseEntries`（单周期至多 1 发，一弓三箭消失）
- `attackSpeedParam` = `attackSpeedStats`（弓 3、法 0.8）

### 5.5 `Animation.PostCombatAndHit` — pass ×3

- 战后：`targetDestroyed=true`、`allPlayersLocomotion=true`、`settleSeconds` = 2.246 / 2.241 / 2.246（均 < 3s）
- 受击：入口 `UnitHealth.ApplyDamage(5f, hitPos, null)`；`hp 100→95`、`onHitCount=1`、观察状态 `Hit_A`、`normalizedTime=0.0753`、`dead=false`

### 5.6 `UI.DamageNumberScale` — pass ×3

- 基线 `localScale=0.006`，期望倍率 3.5 → 期望 `0.021`
- 运行时实例：`localScale=(0.021,0.021,0.021)`、`fontSize=40`
- `CombatFeedback.damageNumberScale` 字段存在、值 3.5、带 `[Range(1f,6f)]`（`rangeAttr=true`）

### 5.7 `Regression.FND02A` — pass ×3

- `gameStarted=true`；固定敌人 `Skeleton_Warrior`，GUID `18dec5e6af31178468b146e0440bd081`，起点 `(-2.72, 0.0833, -12)`
- `navMeshOk=true`、`approachArrived=true`、`approachStartDistance=12` → `approachEndDistance=7`、`approachTraveled=4.5014`、`approachPathStatus=PathComplete`、`approachPathCorners=2`
- `consoleErrorCount=0`（`errorWindow` = `RuntimeInitializeOnLoadMethod(AfterSceneLoad)` 至运行结束）
- `testWroteGameStarted=false`；回归完成后 `testDisabledRealEnemyAfterRegression=true`（避免真实敌人干扰后续夹具断言）

---

## 6. 原始日志说明

| 文件 | 内容 |
|---|---|
| `00_前置确认.txt` | 开工前 Unity 进程 / Lockfile / `git status --porcelain -uall` 原始输出 |
| `02B_Red_Assertions.jsonl` | red 7 行断言（`run=0`，字段：`run, qaItem, assertion, expected, actual, pass, status, kind, timestamp, data`） |
| `02B_Red_Summary.json` | red 汇总（pass/fail/infra、HEAD、seed、夹具说明、`exitReason`） |
| `02B_Red_ExitCode.txt` | `2` |
| `02B_Red_Automation.log` | red 完整 Unity 批处理日志（152412 字节） |
| `02B_Assertions.jsonl` | green 汇总 21 行（3×7，`run=1/2/3`） |
| `02B_Summary_run1/2/3.json` | 各次汇总 |
| `02B_ExitCode_run1/2/3.txt` | 均为 `0` |
| `02B_Automation_run1/2/3.log` | 各次完整 Unity 批处理日志（151246 / 144473 / 144398 字节） |
| `02B_人工目检.md` | 仅 V02B-1/2/3 三项“待目检” |

日志中 `error CS` / `Compilation failed` / `Scripts have compiler errors` 计数：red 与三次 green 均为 **0**。
说明：证据 JSON 内的中文在 Windows 控制台按 GBK 显示会呈乱码，但文件本身为 UTF-8；断言字段、数值与英文不受影响。

---

## 7. 夹具边界

- `FND02B_Target_Combat`：**Play 模式运行期内存创建**的 `UnitHealth` 目标（高 HP、无 AI、`showDamageNumber=false`），不写入场景，结束前 `Destroy`。
- 真实敌人 `Skeleton_Warrior` 仅用于 `Regression.FND02A`（自动开局 / 固定遭遇 / NavMesh 接近），回归后禁用，避免其攻击干扰后续断言。
- 测试**不写** `GameStarted`（`testWroteGameStarted=false`），不改场景、不改预制体、不改 `.meta`。
- 近战自动路径目标点取夹具正前方 2m（`(tp.x, y, tp.z+2.0f)`），保证在 `SwordSwing` 单体 2.2m 判定半径内。

---

## 8. 完整 diff（相对 HEAD `c7b2c79`，仅本次改动文件）

```diff
diff --git a/Assets/_Project/Art/Animations/PlayerBase.controller b/Assets/_Project/Art/Animations/PlayerBase.controller
index 2426d206..dd8df416 100644
--- a/Assets/_Project/Art/Animations/PlayerBase.controller
+++ b/Assets/_Project/Art/Animations/PlayerBase.controller
@@ -491,7 +491,7 @@ AnimatorState:
   m_CorrespondingSourceObject: {fileID: 0}
   m_PrefabInstance: {fileID: 0}
   m_PrefabAsset: {fileID: 0}
-  m_Name: ' Locomotion'
+  m_Name: Locomotion
   m_Speed: 1
   m_CycleOffset: 0
   m_Transitions:
@@ -534,7 +534,7 @@ AnimatorStateTransition:
   m_TransitionDuration: 0.15
   m_TransitionOffset: 0
   m_ExitTime: 0.73214287
-  m_HasExitTime: 1
+  m_HasExitTime: 0
   m_HasFixedDuration: 1
   m_InterruptionSource: 0
   m_OrderedInterruption: 1
@@ -596,7 +596,7 @@ BlendTree:
   m_MaxThreshold: 4
   m_UseAutomaticThresholds: 0
   m_NormalizedBlendValues: 0
-  m_BlendType: 0
+  m_BlendType: 2
 --- !u!1102 &5907960694754117835
 AnimatorState:
   serializedVersion: 6
@@ -657,10 +657,7 @@ AnimatorStateTransition:
   m_PrefabInstance: {fileID: 0}
   m_PrefabAsset: {fileID: 0}
   m_Name: 
-  m_Conditions:
-  - m_ConditionMode: 1
-    m_ConditionEvent: Attack
-    m_EventTreshold: 0
+  m_Conditions: []
   m_DstStateMachine: {fileID: 0}
   m_DstState: {fileID: 4538305570025381470}
   m_Solo: 0
@@ -704,10 +701,7 @@ AnimatorStateTransition:
   m_PrefabInstance: {fileID: 0}
   m_PrefabAsset: {fileID: 0}
   m_Name: 
-  m_Conditions:
-  - m_ConditionMode: 1
-    m_ConditionEvent: Attack
-    m_EventTreshold: 0
+  m_Conditions: []
   m_DstStateMachine: {fileID: 0}
   m_DstState: {fileID: 5907960694754117835}
   m_Solo: 0
diff --git a/Assets/_Project/Scripts/Combat/CombatFeedback.cs b/Assets/_Project/Scripts/Combat/CombatFeedback.cs
index a653d6a8..52d531b4 100644
--- a/Assets/_Project/Scripts/Combat/CombatFeedback.cs
+++ b/Assets/_Project/Scripts/Combat/CombatFeedback.cs
@@ -8,6 +8,9 @@ public class CombatFeedback : MonoBehaviour
     [Tooltip("飘字延迟（秒）：数字跳出来对齐爆炸涨开的时刻")]
     public float damageNumberDelay = 0.12f;
 
+    [Tooltip("伤害飘字视觉缩放倍率（1 = 原基线 0.006；默认 3.5x）")]
+    [Range(1f, 6f)] public float damageNumberScale = 3.5f;
+
     void Awake()
     {
         EventBus.Subscribe<DamageDealtEvent>(OnDamage);
@@ -29,6 +32,6 @@ public class CombatFeedback : MonoBehaviour
     {
         float t = 0f;
         while (t < damageNumberDelay) { t += Time.deltaTime; yield return null; }
-        DamageNumber.Show(info.hitPos, info.finalAmount.ToString("F0"), Color.yellow);
+        DamageNumber.Show(info.hitPos, info.finalAmount.ToString("F0"), Color.yellow, damageNumberScale);
     }
 }
\ No newline at end of file
diff --git a/Assets/_Project/Scripts/Combat/DamageNumber.cs b/Assets/_Project/Scripts/Combat/DamageNumber.cs
index e437927c..85a35ae8 100644
--- a/Assets/_Project/Scripts/Combat/DamageNumber.cs
+++ b/Assets/_Project/Scripts/Combat/DamageNumber.cs
@@ -7,7 +7,7 @@ using UnityEngine.UI;
 /// </summary>
 public class DamageNumber : MonoBehaviour
 {
-    public static void Show(Vector3 worldPos, string content, Color color)
+    public static void Show(Vector3 worldPos, string content, Color color, float scaleMultiplier = 1f)
     {
         var go = new GameObject("DamageNumber");
         go.transform.position = worldPos + Vector3.up * 0.25f;
@@ -16,7 +16,8 @@ public class DamageNumber : MonoBehaviour
         canvas.renderMode = RenderMode.WorldSpace;
         var rt = (RectTransform)go.transform;
         rt.sizeDelta = new Vector2(200f, 60f);
-        rt.localScale = Vector3.one * 0.006f;   // 世界宽度约 1.2 米
+        // 基线世界宽度约 1.2 米（0.006）；倍率由 CombatFeedback.damageNumberScale 传入（默认 3.5x）
+        rt.localScale = Vector3.one * (0.006f * Mathf.Max(0.01f, scaleMultiplier));
 
         var tgo = new GameObject("txt");
         tgo.transform.SetParent(go.transform, false);
diff --git a/Assets/_Project/Scripts/Combat/RangedCombat.cs b/Assets/_Project/Scripts/Combat/RangedCombat.cs
index 8981ecf4..80f5305a 100644
--- a/Assets/_Project/Scripts/Combat/RangedCombat.cs
+++ b/Assets/_Project/Scripts/Combat/RangedCombat.cs
@@ -25,6 +25,14 @@ public class RangedCombat : MonoBehaviour, ICharacterCombat
     public float attackLen = 1.0f;
     [InspectorName("释放帧比例")]
     [Range(0f, 1f)] public float releasePoint = 0.45f;
+
+    [Header("动画释放窗口（按释放状态 normalizedTime）")]
+    [InspectorName("发射窗口起")]
+    [Tooltip("释放状态 normalizedTime 的发射窗口起点（0~1）；留空前摇状态名时退回旧 releasePoint 逻辑")]
+    [Range(0f, 1f)] public float releaseWindowStart = 0.2f;
+    [InspectorName("发射窗口止")]
+    [Tooltip("释放状态 normalizedTime 的发射窗口终点（0~1）")]
+    [Range(0f, 1f)] public float releaseWindowEnd = 0.6f;
     [InspectorName("伤害")]
     public float damage = 12f;
     [InspectorName("锋利度")]
@@ -38,6 +46,7 @@ public class RangedCombat : MonoBehaviour, ICharacterCombat
     float _timer = -1f;
     float _curLen;
     bool _fired;
+    bool _sawAttackAnim;
     Transform _target;
 
     float SpeedMult { get { return stats != null && stats.attackSpeed > 0f ? stats.attackSpeed : 1f; } }
@@ -50,10 +59,13 @@ public class RangedCombat : MonoBehaviour, ICharacterCombat
     public void AutoAttack(Transform target)
     {
         if (_timer >= 0f || target == null) return;
+        // 上一段攻击动画尚未回落时不抢起手，避免用旧释放进度提前发射
+        if (IsAttackAnimPlaying()) return;
         _target = target;
         _curLen = attackLen / SpeedMult;
         _timer = 0f;
         _fired = false;
+        _sawAttackAnim = false;
         if (_animator != null)
         {
             _animator.ResetTrigger("Interrupt");
@@ -86,28 +98,102 @@ public class RangedCombat : MonoBehaviour, ICharacterCombat
         if (_timer < 0f) return;
         _timer += Time.deltaTime;
 
-        float t01 = ReadAnimProgress();
-        if (t01 < 0f) t01 = _timer / _curLen;
+        bool animPlaying;
+        bool inRelease;
+        float t01 = ReadAnimProgress(out animPlaying, out inRelease);
+
+        // 动画读取失败（无 Animator/不在攻击状态）：回退代码时钟
+        if (!animPlaying) t01 = _timer / _curLen;
 
-        if (!_fired && t01 >= releasePoint)
+        if (!_fired)
         {
-            _fired = true;
-            Fire();
+            bool inWindow;
+            if (animPlaying)
+                inWindow = inRelease && t01 >= releaseWindowStart && t01 <= releaseWindowEnd;
+            else
+                inWindow = t01 >= releaseWindowStart && t01 <= releaseWindowEnd;
+
+            if (inWindow)
+            {
+                _fired = true;
+                Fire();
+            }
         }
-        if (_timer >= _curLen) _timer = -1f;
+
+        // 动画仍在攻击状态时延长代码计时（动画周期可能长于 attackLen/SpeedMult）；动画结束即收招
+        if (animPlaying)
+        {
+            _sawAttackAnim = true;
+            if (_timer > MaxAttackSeconds) _timer = -1f;
+        }
+        else if (_sawAttackAnim)
+        {
+            _timer = -1f;
+            _sawAttackAnim = false;
+        }
+        else if (_timer >= _curLen) _timer = -1f;
+    }
+
+    // 攻击动画兜底上限：正常由动画状态驱动收招，此值只防动画卡死导致 _timer 永不结束
+    const float MaxAttackSeconds = 10f;
+
+    bool TwoStateMode
+    {
+        get { return !string.IsNullOrEmpty(drawStateName) && !string.IsNullOrEmpty(releaseStateName); }
     }
 
-    float ReadAnimProgress()
+    /// <summary>
+    /// 读取攻击动画：animPlaying=处于攻击动画（前摇或释放）；inRelease=处于释放状态；
+    /// 返回释放状态的 normalizedTime（前摇返回 -1）。
+    /// 旧配置（前摇状态名为空，例如骷髅弓手 Shoot 单状态）沿用 releasePoint 比例映射。
+    /// </summary>
+    float ReadAnimProgress(out bool animPlaying, out bool inRelease)
     {
+        animPlaying = false;
+        inRelease = false;
         if (_animator == null) return -1f;
+
         var st = _animator.GetCurrentAnimatorStateInfo(0);
+        if (TwoStateMode)
+        {
+            if (st.IsName(releaseStateName))
+            {
+                animPlaying = true;
+                inRelease = true;
+                return st.normalizedTime % 1f;
+            }
+            if (st.IsName(drawStateName))
+            {
+                animPlaying = true;
+                return -1f;
+            }
+            return -1f;
+        }
+
+        // 旧配置：单状态映射到 releasePoint 之后
         if (!string.IsNullOrEmpty(releaseStateName) && st.IsName(releaseStateName))
+        {
+            animPlaying = true;
+            inRelease = true;
             return releasePoint + (1f - releasePoint) * (st.normalizedTime % 1f);
+        }
         if (!string.IsNullOrEmpty(drawStateName) && st.IsName(drawStateName))
+        {
+            animPlaying = true;
             return releasePoint * Mathf.Clamp01(st.normalizedTime % 1f);
+        }
         return -1f;
     }
 
+    bool IsAttackAnimPlaying()
+    {
+        if (_animator == null) return false;
+        var st = _animator.GetCurrentAnimatorStateInfo(0);
+        if (!string.IsNullOrEmpty(drawStateName) && st.IsName(drawStateName)) return true;
+        if (!string.IsNullOrEmpty(releaseStateName) && st.IsName(releaseStateName)) return true;
+        return false;
+    }
+
     void Fire()
     {
         if (arrowPrefab == null || bow == null || _target == null) return;
diff --git a/Assets/_Project/Scripts/Combat/SwordSwing.cs b/Assets/_Project/Scripts/Combat/SwordSwing.cs
index 80e2d694..4976d938 100644
--- a/Assets/_Project/Scripts/Combat/SwordSwing.cs
+++ b/Assets/_Project/Scripts/Combat/SwordSwing.cs
@@ -42,12 +42,20 @@ public class SwordSwing : MonoBehaviour, ICharacterCombat
     [InspectorName("判定窗口止")]
     [Range(0f, 1f)] public float activeEnd = 0.55f;
 
-    [Header("动画状态名（UpperBody 层）")]
-    [InspectorName("攻击状态名")]
+    [Header("动画状态名（单层动画 layer 0）")]
+    [InspectorName("攻击状态名（旧配置）")]
+    [Tooltip("旧配置的攻击状态名；新控制器以“前摇/释放状态名”为准")]
     public string attackStateName = "Attack_Draw";
+    [InspectorName("前摇状态名")]
+    [Tooltip("攻击前摇状态名：只播放动画，不结算命中")]
+    public string drawStateName = "Attack_Draw";
+    [InspectorName("释放状态名")]
+    [Tooltip("攻击释放状态名：命中窗口按此状态的 normalizedTime 判定")]
+    public string releaseStateName = "Attack_Release";
     [InspectorName("技能状态名")]
     public string skillStateName = "Skill";
     [InspectorName("攻击动画层")]
+    [Tooltip("攻击动画所在层；越界时运行时收敛到合法层")]
     public int attackLayer = 0;
 
     [Header("命中")]
@@ -84,6 +92,10 @@ public class SwordSwing : MonoBehaviour, ICharacterCombat
     bool _isSkill;
     float _sharpness, _radiusMult = 1f;
     Transform _target;               // ★ 单体模式：发起攻击时锁定的目标
+    bool _sawAttackAnim;             // 本次攻击是否已进入过攻击动画（用于动画结束收招）
+
+    // 攻击动画兜底上限：正常由动画状态驱动收招，此值只防动画卡死导致 _timer 永不结束
+    const float MaxAttackSeconds = 10f;
 
     Vector3 _prevHilt, _prevTip;
     readonly HashSet<int> _hitIds = new HashSet<int>();
@@ -134,8 +146,9 @@ public class SwordSwing : MonoBehaviour, ICharacterCombat
         if (_timer < 0f) return;
         _timer += Time.deltaTime;
 
-        float t01 = ReadAnimProgress();
-        if (t01 < 0f) t01 = _timer / _curLen;      // 回退代码时钟
+        bool animPlaying;
+        float t01 = ReadAnimProgress(out animPlaying);
+        if (!animPlaying) t01 = _timer / _curLen;      // 动画读取失败：回退代码时钟
 
         float wS = _isSkill ? skillActiveStart : activeStart;
         float wE = _isSkill ? skillActiveEnd : activeEnd;
@@ -147,19 +160,72 @@ public class SwordSwing : MonoBehaviour, ICharacterCombat
             _hiltPts.Add(sword.position);
         }
 
-        if (_timer >= _curLen) _timer = -1f;
+        // 动画仍在攻击状态时延长代码计时（动画可能长于 attackLen/SpeedMult）；动画结束即收招
+        if (animPlaying)
+        {
+            _sawAttackAnim = true;
+            if (_timer > MaxAttackSeconds) _timer = -1f;
+        }
+        else if (_sawAttackAnim)
+        {
+            _timer = -1f;
+            _sawAttackAnim = false;
+        }
+        else if (_timer >= _curLen) _timer = -1f;
         UpdateDebug();
     }
 
-    float ReadAnimProgress()
+    int AnimLayer
     {
-        if (_animator == null || attackLayer >= _animator.layerCount) return -1f;
-        var st = _animator.GetCurrentAnimatorStateInfo(attackLayer);
-        if (st.IsName(attackStateName) || st.IsName(skillStateName))
+        get
+        {
+            if (_animator == null) return 0;
+            return Mathf.Clamp(attackLayer, 0, Mathf.Max(0, _animator.layerCount - 1));
+        }
+    }
+
+    /// <summary>
+    /// 读取攻击动画进度：返回命中窗口状态的 normalizedTime；
+    /// playing=是否仍处于攻击/技能动画（前摇也算，用于延长代码计时）；返回 -1 表示尚未进入释放状态。
+    /// </summary>
+    float ReadAnimProgress(out bool playing)
+    {
+        playing = false;
+        if (_animator == null) return -1f;
+        var st = _animator.GetCurrentAnimatorStateInfo(AnimLayer);
+        if (!string.IsNullOrEmpty(skillStateName) && st.IsName(skillStateName))
+        {
+            playing = true;
             return st.normalizedTime % 1f;
+        }
+        if (!string.IsNullOrEmpty(releaseStateName) && st.IsName(releaseStateName))
+        {
+            playing = true;
+            return st.normalizedTime % 1f;
+        }
+        // 兼容旧配置：attackStateName 既非前摇名又命中当前状态时，按释放状态处理
+        if (!string.IsNullOrEmpty(attackStateName) && attackStateName != drawStateName &&
+            st.IsName(attackStateName))
+        {
+            playing = true;
+            return st.normalizedTime % 1f;
+        }
+        if (!string.IsNullOrEmpty(drawStateName) && st.IsName(drawStateName))
+            playing = true;      // 前摇：只延长计时，不结算命中
         return -1f;
     }
 
+    bool IsAttackAnimPlaying()
+    {
+        if (_animator == null) return false;
+        var st = _animator.GetCurrentAnimatorStateInfo(AnimLayer);
+        if (!string.IsNullOrEmpty(drawStateName) && st.IsName(drawStateName)) return true;
+        if (!string.IsNullOrEmpty(releaseStateName) && st.IsName(releaseStateName)) return true;
+        if (!string.IsNullOrEmpty(skillStateName) && st.IsName(skillStateName)) return true;
+        if (!string.IsNullOrEmpty(attackStateName) && st.IsName(attackStateName)) return true;
+        return false;
+    }
+
     // 单体：锁定大脑/玩家指定的目标；扫掠：窗口内群体（原逻辑）
     public void AutoAttack(Transform target) { DoAttack(false, target); }
 
@@ -187,6 +253,8 @@ public class SwordSwing : MonoBehaviour, ICharacterCombat
     void DoAttack(bool skill, Transform target)
     {
         if (_timer >= 0f) return;
+        // 上一段攻击动画尚未回落时不抢起手，避免用旧释放进度提前结算
+        if (!skill && IsAttackAnimPlaying()) return;
         _isSkill = skill;
         _target = target;
         _sharpness = (weapon != null ? weapon.sharpness : 8f) + (skill ? 5f : 0f);
@@ -206,6 +274,7 @@ public class SwordSwing : MonoBehaviour, ICharacterCombat
         _tipPts.Clear();
         _hiltPts.Clear();
         _trailLife = trailPersistTime + _curLen;
+        _sawAttackAnim = false;
         _timer = 0f;
     }
 
diff --git a/Assets/_Project/Scripts/Units/CharacterBrain.cs b/Assets/_Project/Scripts/Units/CharacterBrain.cs
index b3fa6b72..e70bd52d 100644
--- a/Assets/_Project/Scripts/Units/CharacterBrain.cs
+++ b/Assets/_Project/Scripts/Units/CharacterBrain.cs
@@ -14,9 +14,29 @@ public class CharacterBrain : UnitBrain
     Transform _orderedTarget;
     Interactable _pendingInteract;
     bool _moving;
+    UnitHealth _health;
 
     protected override Team MyTeam { get { return Team.Player; } }
 
+    protected override void Start()
+    {
+        base.Start();
+        _health = GetComponent<UnitHealth>();
+        if (_health != null) _health.OnHit += OnDamaged;
+    }
+
+    void OnDestroy()
+    {
+        if (_health != null) _health.OnHit -= OnDamaged;
+    }
+
+    // 受击反馈：统一伤害入口（UnitHealth.ApplyDamage/TakeHit）触发动画器 Hit → Hit_A
+    void OnDamaged(DamageInfo info)
+    {
+        Animator a = unitAnimator != null ? unitAnimator : GetComponentInChildren<Animator>();
+        if (a != null) a.SetTrigger("Hit");
+    }
+
     public void CommandAttack(Transform t)
     {
         _orderedTarget = t;
diff --git a/Assets/_Project/Scripts/Units/UnitAnimatorSpeed.cs b/Assets/_Project/Scripts/Units/UnitAnimatorSpeed.cs
index e976da83..326fca62 100644
--- a/Assets/_Project/Scripts/Units/UnitAnimatorSpeed.cs
+++ b/Assets/_Project/Scripts/Units/UnitAnimatorSpeed.cs
@@ -9,17 +9,22 @@ public class UnitAnimatorSpeed : MonoBehaviour
     [InspectorName("数值资产")]
     public UnitStats stats;
     Animator _animator;
+    UnitBrain _brain;
 
     void Awake()
     {
         _animator = GetComponentInChildren<Animator>();
+        _brain = GetComponent<UnitBrain>();
     }
 
     void Update()
     {
         if (_animator == null) return;
-        float s = stats != null ? stats.attackSpeed : 1f;
-        if (!Mathf.Approximately(_animator.GetFloat("AttackSpeed"), s))
-            _animator.SetFloat("AttackSpeed", s);
+        // ★ 优先取同一单位 Driver 的 stats：预制体变体（弓/法）会继承基础预制体的 stats 槽，
+        //   导致 AttackSpeed 参数与角色真实攻速不同源。
+        UnitStats s = _brain != null && _brain.stats != null ? _brain.stats : stats;
+        float v = s != null ? s.attackSpeed : 1f;
+        if (!Mathf.Approximately(_animator.GetFloat("AttackSpeed"), v))
+            _animator.SetFloat("AttackSpeed", v);
     }
 }
\ No newline at end of file
```

测试代码（新增、未跟踪，非生产代码，完整内容见仓库文件，不在此展开 3 个 `.cs`）：

- `Assets/_Project/Tests/FND02B/Editor/FND02B_BatchAcceptance.cs`（入口 `Run`，含 `.meta`）
- `Assets/_Project/Tests/FND02B/Runtime/FND02B_ProbeRunner.cs`（7 条断言与夹具，含 `.meta`）
- `Assets/_Project/Tests/FND02B/Runtime/FND02B_Evidence.cs`（JSONL/Summary/ExitCode 写入，含 `.meta`）

---

## 9. 变更文件

生产（本次改动，未提交）：

| 文件 | 改动要点 |
|---|---|
| `Assets/_Project/Art/Animations/PlayerBase.controller` | 状态名去前导空格；混合树 Simple1D→FreeformDirectional2D；`Locomotion→Attack_Draw` 去 ExitTime；`Attack_Draw→Attack_Release`、`Attack_Release→Locomotion` 去 `Attack` 条件 |
| `Assets/_Project/Scripts/Units/UnitAnimatorSpeed.cs` | `AttackSpeed` 优先取 `UnitBrain.stats` |
| `Assets/_Project/Scripts/Units/CharacterBrain.cs` | 订阅 `UnitHealth.OnHit` → `Animator.SetTrigger("Hit")` |
| `Assets/_Project/Scripts/Combat/SwordSwing.cs` | 前摇/释放双状态名、层 clamp、动画结束收招、10s 兜底 |
| `Assets/_Project/Scripts/Combat/RangedCombat.cs` | 释放窗口 `[releaseWindowStart, releaseWindowEnd]`、双状态模式 + 旧单状态兼容、动画结束收招 |
| `Assets/_Project/Scripts/Combat/CombatFeedback.cs` | `damageNumberScale=3.5`（`[Range(1,6)]`） |
| `Assets/_Project/Scripts/Combat/DamageNumber.cs` | `Show(..., scaleMultiplier=1f)`，`localScale = 0.006 * max(0.01, mult)` |

测试（新增，未跟踪）：`Assets/_Project/Tests/FND02B/**`（含 3 个 `.cs` 与全部 `.meta`）

证据（新增，未跟踪）：`Docs/QA/Evidence/FND-02B/**`（§6 全部文件）

**未修改**：`Knight_Unit.prefab` / `Archer_Unit.prefab` / `Mage_Unit.prefab` / `DungeonWorld.unity` / `ProjectSettings/**` / `Docs/Handoff/CURRENT.md` / 任何已有 `.meta`。

---

## 10. 未覆盖项

1. **OverrideController 换 Clip（工单阶段 1 批准清单第 2 项）未执行**：批准清单未给出具体 Clip 资产与 GUID 映射（现有 `PlayerBaseOverride` 三色共用 CombatRanged 攻击 Clip），属于**信息不足**；且 7 条自动化断言均不依赖 Clip 身份（断言的是状态名与 `normalizedTime` 窗口）。若需按角色换用专属攻击 Clip，请补充“角色 → Clip 资产路径/GUID”映射后再单独派单。
2. `RangedCombat` 旧单状态兼容分支（`drawStateName` 为空，如 `Skeleton_Rogue`）仅做静态兼容，未纳入自动化断言；本次未改动该敌人行为。
3. 人工目检 V02B-1/2/3 未执行（按工单要求由用户完成）。

---

## 11. 实现原理（面向项目所有者）

**根因一：动画机自身有三处断点。**
- `Locomotion` 状态名实际存的是 `" Locomotion"`（前导空格），任何 `IsName("Locomotion")`/状态判定都匹配不上，表现为“移动时看着像 Idle”。
- 移动混合树是 `Simple1D`（`m_BlendType=0`），只读 `SpeedX`；而角色前向移动把速度写进 `SpeedZ`，于是速度再大也推不动动画。
- 攻击链 `Locomotion→Attack_Draw→Attack_Release→Locomotion` 的后续两段过渡仍要求再触发一次 `Attack` 且带 ExitTime，单次 trigger 只够进第一段，于是卡在 `Attack_Draw` 直到代码超时。

对应改法：改状态名、混合树改 `FreeformDirectional2D`（同时吃 X/Z）、`Locomotion→Attack_Draw` 去掉 ExitTime、两段后续过渡去掉条件让动画按自身 ExitTime 自动流转。这样“一次 trigger 走完整套招式”，代码只负责起手与收招。

**根因二：代码时钟与动画时钟不同源。**
原 `SwordSwing`/`RangedCombat` 用 `_timer/_curLen` 的代码时间判断出伤/发射，动画只是装饰；一旦动画比代码慢（或卡在 Draw），就会出现“动作没到位却已结算”或“一弓三箭”。改法是：以动画状态的 `normalizedTime` 为准，在配置窗口内结算一次（`_fired` 单次闸门），代码计时只做兜底（动画结束即收招，10s 硬上限防卡死）。近战窗口 `[0.35,0.55]` 为 `SwordSwing.activeStart/activeEnd` 的现有配置；远程窗口取 `[0.2,0.6]`，因为 Draw→Release 过渡 0.15s 内释放状态已推进到约 0.34，若按原建议 `[0,0.2]` 会漏发射。

**根因三：参数与表现不同源。**
`UnitAnimatorSpeed` 读的是组件上的 `stats` 槽，而弓/法预制体变体继承了基础预制体的 `stats`（攻速 1），导致动画速度与真实攻速不符 → 改为优先取同一单位 `UnitBrain.stats`。受击动画原先无人触发 → `CharacterBrain` 订阅 `UnitHealth.OnHit` 统一入口打 `Hit` trigger。飘字过小 → 倍率做成 `CombatFeedback` 上的可调字段（默认 3.5），由 `DamageNumber.Show` 施加到 `localScale`。

---

## 12. 受影响模块导航说明

- **动画机**：`Assets/_Project/Art/Animations/PlayerBase.controller`（三色玩家共用基座，变体经 OverrideController 换 Clip）。入口是 Animator 参数 `SpeedX/SpeedZ/AttackSpeed/Attack/Hit`。
- **移动**：`UnitBrain`/`CharacterBrain.CommandMove` → NavMeshAgent 速度 → `UnitAnimatorSpeed` 写 `AttackSpeed`、移动驱动写 `SpeedX/SpeedZ` → 混合树。
- **攻击**：`CharacterBrain.CommandAttack`（指定）与 `UnitBrain.AttackLoop`→`ICharacterCombat.AutoAttack`（自动）→ `SwordSwing`/`RangedCombat` 读 `Attack_Release.normalizedTime` 窗口 → `UnitHealth.ApplyDamage`/生成投射物。
- **反馈**：`DamageDealtEvent` → `CombatFeedback` → `DamageNumber.Show`。

---

## 13. 执行环境（Go 订阅与模型 ID）

- 执行者运行时模型（本次会话）：provider `opencode`，model `deepseek-v4.1-flash-free` → 完整 ID `opencode/deepseek-v4.1-flash-free`。
- 仓库 `.opencode/opencode.json` 中登记的模型 ID：
  - Go 订阅：`opencode-go/deepseek-v4.1-flash`（`temperature: 0`）
  - 非 Go：`opencode/deepseek-v4.1-flash`（`temperature: 0`）
- 说明：订阅类型与额度属账户侧信息，执行者无法从仓库读取，仅登记配置中可见的 ID 事实。

---

## 14. 遗留问题

1. OverrideController 换 Clip 未做（见 §10.1），需补 Clip/GUID 映射。
2. `PlayerBase.controller` 的 `Attack_Draw` 与 `Attack_Release` 现在无条件自动流转，意味着**任何**进入 `Attack_Draw` 的 trigger 都会完整走完一套；目前只有玩家使用该控制器，若后续敌人复用需重新评估。
3. `MaxAttackSeconds=10f` 为兜底常量，正常路径不会触发；若后续出现 10s 卡招，应视为动画机断链的告警信号。
4. 探针 JSON 中 `effectiveLayer`/`drawStateName` 字段命名与实际语义不符（见 §5.3 说明），属测试代码遗留，不影响断言结论；如需可后续重命名。
5. 真实敌人 `Skeleton_Rogue` 的 `RangedCombat` 走旧单状态兼容分支，未做行为回归（本次未改其预制体与配置）。

---

## 15. 建议提交（仅建议，执行者未执行任何 git 写操作）

组 1（生产代码 + 动画机）：

```text
git add Assets/_Project/Art/Animations/PlayerBase.controller Assets/_Project/Scripts/Units/UnitAnimatorSpeed.cs Assets/_Project/Scripts/Units/CharacterBrain.cs Assets/_Project/Scripts/Combat/SwordSwing.cs Assets/_Project/Scripts/Combat/RangedCombat.cs Assets/_Project/Scripts/Combat/CombatFeedback.cs Assets/_Project/Scripts/Combat/DamageNumber.cs
fix(animation): 修正玩家动画机断链并让攻击结算跟随动画时钟
原因：Locomotion 状态名带前导空格、混合树仅读 SpeedX、攻击链需二次 trigger，导致移动滑步、卡 Attack_Draw 与一弓三箭；出伤改由 Attack_Release.normalizedTime 窗口驱动。
```

组 2（自动化验收测试）：

```text
git add Assets/_Project/Tests/FND02B.meta Assets/_Project/Tests/FND02B/
test(fnd-02b): 新增玩家动画与攻击时钟批处理验收
原因：7 条断言需可重复运行以证明动画机与攻击窗口修复，且保留 red/green 对照。
```

组 3（证据与报告）：

```text
git add Docs/QA/Evidence/FND-02B/ Docs/Handoff/REPORT_FND-02B.md
docs(qa): 登记 FND-02B red/green 证据与验收报告
原因：修复结论需可追溯到断言数据与原始日志。
```

> `Docs/Handoff/CURRENT.md` 的改动属【大脑】职责，不在本报告建议范围内。
