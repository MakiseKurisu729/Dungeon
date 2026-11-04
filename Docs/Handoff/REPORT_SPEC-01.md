# REPORT_SPEC-01 — 动画机单段攻击（近战）与伤害飘字系统重做

> 任务：`Docs/Handoff/TASK_SPEC-01_动画机单段攻击与飘字系统.md`（阶段 2）
> 状态：**待用户目检 + 外部顾问复核 +【大脑】终审**（未标 Done、未改 `CURRENT.md`）
> 风险：高风险（命中触发路径 + 伤害入口管线）
> 执行者：DeepSeek V4.1 Flash（【执行者】）；无任何 git 写操作

---

## 1. 结果摘要

| 项 | 结果 |
|---|---|
| 阶段 0 前置闸门 | **PASS**（Unity 已关闭、无锁文件；B1 核验通过） |
| red | **pass=4 / product_fail=3 / blocked=0 / infraFail=0**，exit=2 |
| green run1 / run2 / run3 | **各 7/7 pass，fail=0，infra=0，exit=0** |
| green 关键数据一致性 | 三次 run 完全一致（见 §5.2） |
| 改动面 | 仅 4 个生产 `.cs` + 新增 `Assets/_Project/Tests/SPEC01/`（3 脚本 + 3 `.meta` + 3 目录 `.meta`）+ Docs 证据/报告；另 2 个 `.csproj` 由 Unity 自动重生成（仅 3 条 SPEC01 编译项） |
| 资产改动 | **零**（`.controller`/`.anim`/`.unity`/`.prefab`/`.asset`/`.overrideController` 均只读；mtime 均早于本会话） |
| `RangedCombat.cs` | **零改动**（B1 裁决；继续读 `attackTrigger` 序列化字段） |

- 分支：`docs/fnd-00-baseline`
- HEAD：`de92f096d674fab373ba0f32cbff160380500e52`
- Unity：`2022.3.62f3c1`（`D:\Unity\2022.3.62f3c1\Editor\Unity.exe`）

### 命令

```powershell
# red
$env:SPEC01_MODE="red"
D:\Unity\2022.3.62f3c1\Editor\Unity.exe -batchmode -projectPath D:\unity\Dungeon `
  -executeMethod SPEC01_BatchAcceptance.Run `
  -logFile D:\unity\Dungeon\Docs\QA\Evidence\SPEC-01\SPEC01_Red_Automation.log

# green runN（N=1,2,3）
$env:SPEC01_MODE="green"; $env:SPEC01_RUN="N"
D:\Unity\2022.3.62f3c1\Editor\Unity.exe -batchmode -projectPath D:\unity\Dungeon `
  -executeMethod SPEC01_BatchAcceptance.Run `
  -logFile D:\unity\Dungeon\Docs\QA\Evidence\SPEC-01\SPEC01_Automation_runN.log
```

---

## 2. 阶段 0 前置闸门

证据：`Docs/QA/Evidence/SPEC-01/00_前置确认.txt`

| 检查 | 结果 |
|---|---|
| 无 `Unity.exe` 进程 | PASS |
| 无 `Temp/UnityLockfile` | PASS |
| 控制器参数保持 `AttackRange`、无 `Attack` 触发器参数 | PASS |
| `Archer_Unit.prefab` / `Mage_Unit.prefab` 的 `RangedCombat.attackTrigger` = `AttackRange` | PASS（L392 / L323） |
| `RangedCombat.cs` 运行时只读 `attackTrigger` 字段（L72/L82），无 `SetTrigger("Attack")` 硬编码 | PASS（唯一 `"Attack"` 字面量是 L16 字段默认值，被预制体覆盖；按 B1 裁决不改） |
| 控制器 `AttackMelee` 状态、`Locomotion→AttackMelee` duration=0.25/HasExitTime=0、`AttackMelee→Locomotion` ExitTime=0.75/HasExitTime=1 | PASS（只读 YAML） |
| 混合树 1D、参数 `SpeedZ`、阈值 0/4 | PASS（只读 YAML） |
| 骑士预制体 `attackStateName: AttackMelee`（所有者手动） | PASS（只读） |

> 说明：B1 的“无 `"Attack"` 硬编码”按“运行时调用点不使用字面量”判定；字段默认值 `"Attack"` 保留（`RangedCombat.cs` 零改动），已在证据文件中显式记录。

---

## 3. 影响面清单（阶段 1 已于派单前获批；此处留档）

### 3.1 修改（生产代码，仅 `.cs`）

| 文件 | 符号 | 说明 |
|---|---|---|
| `Assets/_Project/Scripts/Combat/SwordSwing.cs` | `attackTrigger`（新，默认 `"AttackMelee"`）、`hitStateName`（新，默认 `"AttackMelee"`）、`DoAttack`、`CancelAttack`、`ReadAnimProgress`、`IsAttackAnimPlaying` | 出招触发器由硬编码 `"Attack"` 改为字段；命中窗口新增单段状态读取 |
| `Assets/_Project/Scripts/Combat/DamageInfo.cs` | `enum DamageElement`（新）、`DamageInfo.element`（新） | 方案 B：类型轴与 `WeaponData.DamageType` 正交 |
| `Assets/_Project/Scripts/Combat/DamageNumber.cs` | `Show(Vector3,float,DamageElement)`、共享画布/池/分档/曲线（重写） | 旧 `Show(Vector3,string,Color,float)` 移除 |
| `Assets/_Project/Scripts/Combat/CombatFeedback.cs` | `ShowNumber`、删除 `damageNumberScale` | 传 `finalAmount` + `element`；保留 `damageNumberDelay` |

### 3.2 新增（测试与 `.meta`）

```
Assets/_Project/Tests/SPEC01.meta
Assets/_Project/Tests/SPEC01/Editor.meta
Assets/_Project/Tests/SPEC01/Runtime.meta
Assets/_Project/Tests/SPEC01/Editor/SPEC01_BatchAcceptance.cs(+.meta)
Assets/_Project/Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs(+.meta)
Assets/_Project/Tests/SPEC01/Runtime/SPEC01_Evidence.cs(+.meta)
```

> `Assembly-CSharp.csproj` / `Assembly-CSharp-Editor.csproj` 由 Unity 导入新脚本时自动重生成，diff 仅新增 3 条 SPEC01 编译项（未手改）。

### 3.3 字符串/资产引用核查

- `"Attack"`：生产代码中的 `SetTrigger/ResetTrigger` 已无字面量（`SwordSwing` 改用 `attackTrigger`）；`RangedCombat.cs` L16 仅默认值，未改。
- `"AttackMelee"`：`SwordSwing.attackTrigger` / `hitStateName` 默认值；`PlayerBase.controller` 已有同名 Trigger 与状态；`Knight_Unit.prefab` 已序列化 `attackStateName: AttackMelee`。
- `"Interrupt"`：沿用；B2（无过渡）仅登记不修。
- `"Locomotion"`：测试按无前导空格的现行状态名断言。
- `Attack_Draw` / `Attack_Release` / `AttackRange`：弓/法两段式与远程触发器保持。
- `SendMessage` / `Find`：未新增；仅 `DamageNumber.EnsureCanvas` 用 `GameObject.Find(OverlayName)` 做域重载兜底。
- 场景引用：`DungeonWorld.unity` 对 `CombatFeedback` 的引用不变（脚本 GUID 不变）；`damageNumberScale` 成为孤立序列化键（场景只读、不保存）。
- `.meta`：生产脚本 `.meta` 全部未改；仅新增 SPEC01 测试目录与脚本 `.meta`。

---

## 4. red（实施前）

证据目录：`Docs/QA/Evidence/SPEC-01/`
- `SPEC01_Red_Assertions.jsonl`（7 条）
- `SPEC01_Red_Automation.log`
- `SPEC01_Red_Summary.json`
- `SPEC01_Red_ExitCode.txt` = `2`

### 4.1 red 逐条 product_fail（含 expected / actual）

1. **`Attack.DualPathUnified`** — `product_fail`
   - expected：CommandAttack 与 Aggro 两条路径上，骑士命中瞬间 `state=AttackMelee`（单段，状态名＝触发器名）；`SwordSwing.attackTrigger=AttackMelee`、`hitStateName=AttackMelee`；弓/法 `state=Attack_Release`；`RangedCombat.attackTrigger=AttackRange`。
   - actual：`designatedPass=False; autoPass=False; swordFieldsOk=False; rangedTriggerOk=True; swordTrigger=<missing>; swordHitState=<missing>`；Knight 指定路径命中 `state=Locomotion, normalizedTime=1`，自动路径命中 `state=Locomotion`（弓/法均为 `Attack_Release`）。
2. **`Clock.MeleeNormalizedWindow`** — `product_fail`
   - expected：骑士命中由 `AttackMelee.normalizedTime` 的 `[activeStart, activeEnd]` 驱动；单周期恰 1 次；触发器/命中状态名均为 `AttackMelee`。
   - actual：`hits=11; inWindow=0; meleeEntries=0; window=[0.35,0.55]; attackTrigger=<missing>; hitStateName=<missing>; attackSpeedParam=1; stats=1`（命中全部落在 `Locomotion`）。
3. **`UI.DamageNumberScale`** — `product_fail`
   - expected：共享 Overlay 画布唯一；5 伤害→1.5x、200 伤害→5x 钳制；`Text.raycastTarget=false`；连续 40 次 Show 活跃/总实例 ≤32；不新增画布。
   - actual：`sharedOk=False; namedCanvasCount=0; overlayBeforeStress=1; overlayAfterStress=1; smallMult=-1; largeMult=-1; smallFont=-1; largeFont=-1; baseFontSize=-1; activeAfterStress=40; totalAfterStress=40; raycastOk=True; element=<missing>/<missing>`（旧实现每次 new WorldSpace Canvas，无池、无分档）。

### 4.2 red 通过项（未回归）

`Locomotion.AllPlayersMoveParam`（1D/SpeedZ/0-4 已由所有者就位）、`Clock.RangedOneShotPerCycle`、`Animation.PostCombatAndHit`、`Regression.FND02A`。

### 4.3 red 过程留痕

- `_attempts/SPEC01_Red_Automation_attempt1_compile_error.log`：首轮 red 因测试夹具直接引用尚未实现的 `DamageInfo.element` 而编译失败（测试侧问题），已改为反射读取后重跑。
- `_attempts/SPEC01_Red_*_pre_isolation.*`：第二轮 red 未做自动战斗隔离（压力采样出现 42 个实例）；补隔离后重跑得到最终 red。最终 red 与最终测试代码的唯一差异是 `Element` 读取方式（`ReadStringMember`→`InstString`），在 red 下两者都返回 `<missing>`，结论不变。

---

## 5. green ×3

唯一入口：`SPEC01_BatchAcceptance.Run`（Editor）→ `SPEC01_ProbeRunner`（Play）。断言顺序：Locomotion → Regression.FND02A → Attack.DualPathUnified（含两个 Clock 断言）→ Animation.PostCombatAndHit → UI.DamageNumberScale。

### 5.1 逐断言表（run1 / run2 / run3）

| # | 断言 | 新口径 | run1 | run2 | run3 |
|---|---|---|---|---|---|
| 1 | `Locomotion.AllPlayersMoveParam` | 混合树 1D、参数 `SpeedZ`、阈值 0/4；三角色经生产移动命令移动/停稳 | pass | pass | pass |
| 2 | `Regression.FND02A` | 自动开局、固定遭遇、真实移动接近、Console 零错误 | pass | pass | pass |
| 3 | `Attack.DualPathUnified` | 骑士命中 `AttackMelee`；弓/法 `Attack_Release`；远程触发器 `AttackRange` | pass | pass | pass |
| 4 | `Clock.MeleeNormalizedWindow` | 读 `AttackMelee` 态窗口；触发器/状态名均为 `AttackMelee` | pass | pass | pass |
| 5 | `Clock.RangedOneShotPerCycle` | 弓/法单周期 1 次、释放窗口 | pass | pass | pass |
| 6 | `Animation.PostCombatAndHit` | 收招回 Locomotion；受击进 `Hit_A` | pass | pass | pass |
| 7 | `UI.DamageNumberScale` | 共享 Overlay 唯一 + 1.5x/5x 钳制 + 池 ≤32 + 不新增画布 + raycastTarget=false | pass | pass | pass |

### 5.2 关键数据一致性（三次 run）

| 数据 | run1 | run2 | run3 |
|---|---|---|---|
| Locomotion `blend1D / blendParameter / thresholds` | True / SpeedZ / [0,4] | 同 | 同 |
| 骑士 `attackTrigger / hitStateName` | AttackMelee / AttackMelee | 同 | 同 |
| Melee `hits / inWindow / meleeEntries` | 12 / 12 / 12 | 12 / 12 / 12 | 12 / 12 / 12 |
| Melee `window` | [0.35, 0.55] | 同 | 同 |
| UI `namedCanvasCount` | 1 | 1 | 1 |
| UI `smallMult / largeMult` | 1.5 / 5 | 1.5 / 5 | 1.5 / 5 |
| UI `smallFont / largeFont` | 24 / 80 | 24 / 80 | 24 / 80 |
| UI `activeAfterStress / totalAfterStress` | 32 / 32 | 32 / 32 | 32 / 32 |
| UI `overlayBefore/AfterStress` | 2 / 2 | 2 / 2 | 2 / 2 |
| UI `element` | Physical / Physical | 同 | 同 |
| `Regression` consoleErrors | 0 | 0 | 0 |
| PostCombat settle | 1.855s | 1.856s | 1.853s |

### 5.3 green 证据文件

- `SPEC01_Assertions.jsonl`（run1 覆盖，run2/3 追加，共 21 条，`run` 字段区分）
- `SPEC01_Automation_run1.log` / `run2.log` / `run3.log`
- `SPEC01_Summary_run1.json` / `run2.json` / `run3.json`
- `SPEC01_ExitCode_run1.txt` / `run2.txt` / `run3.txt`（均为 `0`）
- `SPEC01_Runs.jsonl`（跨 run 汇总索引；含 pre-isolation red 尝试、最终 red 与 green×3）

---

## 6. 完整 diff（相对本单实施前工作树快照）

```diff
### diff: Assets/_Project/Scripts/Combat/SwordSwing.cs
diff --git "a/Assets/_Project/Scripts/Combat/SwordSwing.cs" "b/Assets/_Project/Scripts/Combat/SwordSwing.cs"
index 4976d938..3ecfb094 100644
--- "a/Assets/_Project/Scripts/Combat/SwordSwing.cs"
+++ "b/Assets/_Project/Scripts/Combat/SwordSwing.cs"
@@ -37,6 +37,9 @@ public class SwordSwing : MonoBehaviour, ICharacterCombat
     [Header("攻击动画")]
     [InspectorName("攻击时长")]
     public float attackLen = 0.8f;
+    [InspectorName("攻击触发器名")]
+    [Tooltip("出招时写入 Animator 的 Trigger；近战单段用 AttackMelee（与状态名一致）")]
+    public string attackTrigger = "AttackMelee";
     [InspectorName("判定窗口起")]
     [Range(0f, 1f)] public float activeStart = 0.35f;
     [InspectorName("判定窗口止")]
@@ -46,6 +49,9 @@ public class SwordSwing : MonoBehaviour, ICharacterCombat
     [InspectorName("攻击状态名（旧配置）")]
     [Tooltip("旧配置的攻击状态名；新控制器以“前摇/释放状态名”为准")]
     public string attackStateName = "Attack_Draw";
+    [InspectorName("命中状态名")]
+    [Tooltip("命中窗口按此状态的 normalizedTime 判定；近战单段 AttackMelee（状态名＝触发器名，不复用两段式 releaseStateName）")]
+    public string hitStateName = "AttackMelee";
     [InspectorName("前摇状态名")]
     [Tooltip("攻击前摇状态名：只播放动画，不结算命中")]
     public string drawStateName = "Attack_Draw";
@@ -198,6 +204,12 @@ public class SwordSwing : MonoBehaviour, ICharacterCombat
             playing = true;
             return st.normalizedTime % 1f;
         }
+        // 单段近战：状态名＝触发器名（AttackMelee），命中窗口直接读该状态
+        if (!string.IsNullOrEmpty(hitStateName) && st.IsName(hitStateName))
+        {
+            playing = true;
+            return st.normalizedTime % 1f;
+        }
         if (!string.IsNullOrEmpty(releaseStateName) && st.IsName(releaseStateName))
         {
             playing = true;
@@ -220,6 +232,7 @@ public class SwordSwing : MonoBehaviour, ICharacterCombat
         if (_animator == null) return false;
         var st = _animator.GetCurrentAnimatorStateInfo(AnimLayer);
         if (!string.IsNullOrEmpty(drawStateName) && st.IsName(drawStateName)) return true;
+        if (!string.IsNullOrEmpty(hitStateName) && st.IsName(hitStateName)) return true;
         if (!string.IsNullOrEmpty(releaseStateName) && st.IsName(releaseStateName)) return true;
         if (!string.IsNullOrEmpty(skillStateName) && st.IsName(skillStateName)) return true;
         if (!string.IsNullOrEmpty(attackStateName) && st.IsName(attackStateName)) return true;
@@ -235,7 +248,7 @@ public class SwordSwing : MonoBehaviour, ICharacterCombat
         _timer = -1f;
         if (_animator != null)
         {
-            _animator.ResetTrigger("Attack");
+            _animator.ResetTrigger(attackTrigger);
             _animator.ResetTrigger("Skill");
             _animator.SetTrigger("Interrupt");
         }
@@ -264,8 +277,8 @@ public class SwordSwing : MonoBehaviour, ICharacterCombat
         if (_animator != null)
         {
             _animator.ResetTrigger("Interrupt");
-            _animator.ResetTrigger("Attack");
-            _animator.SetTrigger(skill ? "Skill" : "Attack");
+            _animator.ResetTrigger(attackTrigger);
+            _animator.SetTrigger(skill ? "Skill" : attackTrigger);
         }
 
         _prevHilt = sword.position;

### diff: Assets/_Project/Scripts/Combat/DamageInfo.cs
diff --git "a/Assets/_Project/Scripts/Combat/DamageInfo.cs" "b/Assets/_Project/Scripts/Combat/DamageInfo.cs"
index b74d21d1..55c89028 100644
--- "a/Assets/_Project/Scripts/Combat/DamageInfo.cs"
+++ "b/Assets/_Project/Scripts/Combat/DamageInfo.cs"
@@ -1,11 +1,18 @@
 using UnityEngine;
 
+/// <summary>
+/// 伤害元素类型（飘字配色/后续抗性轴）。
+/// 与武器类别轴 <see cref="DamageType"/>（Slash/Thrust/Blunt）正交；后续类型在此追加。
+/// </summary>
+public enum DamageElement { Physical, Fire, Ice, Lightning, Poison, Holy, Shadow }
+
 /// <summary>伤害数据包：一次伤害结算的全部上下文（暴击/增伤/飘字/音效/统计的统一挂点）。</summary>
 public struct DamageInfo
 {
     public UnitHealth source;      // 来源（可为空）
     public UnitHealth target;
     public DamageType damageType;
+    public DamageElement element;  // 伤害元素；枚举 0 值即默认 Physical（结构体字段不可写初始化器）
     public float rawAmount;
     public float finalAmount;
     public bool brokeDefense;     // false = 弹刀未破防

### diff: Assets/_Project/Scripts/Combat/CombatFeedback.cs
diff --git "a/Assets/_Project/Scripts/Combat/CombatFeedback.cs" "b/Assets/_Project/Scripts/Combat/CombatFeedback.cs"
index 52d531b4..314ad437 100644
--- "a/Assets/_Project/Scripts/Combat/CombatFeedback.cs"
+++ "b/Assets/_Project/Scripts/Combat/CombatFeedback.cs"
@@ -8,9 +8,6 @@ public class CombatFeedback : MonoBehaviour
     [Tooltip("飘字延迟（秒）：数字跳出来对齐爆炸涨开的时刻")]
     public float damageNumberDelay = 0.12f;
 
-    [Tooltip("伤害飘字视觉缩放倍率（1 = 原基线 0.006；默认 3.5x）")]
-    [Range(1f, 6f)] public float damageNumberScale = 3.5f;
-
     void Awake()
     {
         EventBus.Subscribe<DamageDealtEvent>(OnDamage);
@@ -32,6 +29,8 @@ public class CombatFeedback : MonoBehaviour
     {
         float t = 0f;
         while (t < damageNumberDelay) { t += Time.deltaTime; yield return null; }
-        DamageNumber.Show(info.hitPos, info.finalAmount.ToString("F0"), Color.yellow, damageNumberScale);
+        // 出生点取敌人本体位置（DamageNumber 内部再抬到胸口高度）；无目标时退回命中点
+        Vector3 world = info.target != null ? info.target.transform.position : info.hitPos;
+        DamageNumber.Show(world, info.finalAmount, info.element);
     }
 }
\ No newline at end of file

### diff: Assets/_Project/Scripts/Combat/DamageNumber.cs
diff --git "a/Assets/_Project/Scripts/Combat/DamageNumber.cs" "b/Assets/_Project/Scripts/Combat/DamageNumber.cs"
index 85a35ae8..0e4137b5 100644
--- "a/Assets/_Project/Scripts/Combat/DamageNumber.cs"
+++ "b/Assets/_Project/Scripts/Combat/DamageNumber.cs"
@@ -1,53 +1,304 @@
+using System.Collections.Generic;
 using UnityEngine;
 using UnityEngine.UI;
 
 /// <summary>
-/// 伤害数字：世界空间 Canvas + Text，向上飘、面向相机、渐隐销毁。
-/// 用法：DamageNumber.Show(世界坐标, "25", Color.yellow);
+/// 伤害飘字（恶意不息风格）：单个共享 Screen Space-Overlay 画布 + 对象池（默认 32）。
+/// 出生瞬间取世界点（敌人胸口 +1.2m）投影到屏幕，之后只做自身运动、不再跟随。
+/// 尺寸随伤害分档：字号 = BaseFontSize × Clamp(Lerp(1.5, 5, (amount-5)/(60-5)), 1.5, 5)；
+/// 弹出 0.12s 内 0.6→1.15→缓慢缩至 0.8，全程上飘 + 淡出，生命周期 0.9s。
+/// 纯脚本、零资产：颜色用静态字典（可 SetElementColor 覆盖），不建 SO。
 /// </summary>
 public class DamageNumber : MonoBehaviour
 {
-    public static void Show(Vector3 worldPos, string content, Color color, float scaleMultiplier = 1f)
+    // ---- 共享画布 ----
+    public const string OverlayName = "DamageNumberOverlay";
+
+    // ---- 对象池 ----
+    public const int DefaultPoolCapacity = 32;
+    public static int PoolCapacity = DefaultPoolCapacity;
+
+    // ---- 尺寸-伤害挂钩 ----
+    public const float BaselineScale = 0.006f;   // 旧世界空间基线（仅作倍率语义参考；Overlay 下用字号实现）
+    public const float BaseFontSize = 16f;
+    public const float MinScaleMultiplier = 1.5f;
+    public const float MaxScaleMultiplier = 5f;
+    public const float MinDamage = 5f;
+    public const float MaxDamage = 60f;
+
+    // ---- 运动/曲线 ----
+    public const float Lifetime = 0.9f;
+    public const float PopDuration = 0.12f;
+    public const float PopStartScale = 0.6f;
+    public const float PopPeakScale = 1.15f;
+    public const float PopEndScale = 0.8f;
+    public const float FadeDuration = 0.35f;
+    public const float ChestHeight = 1.2f;
+    public const float MinAngle = 60f;
+    public const float MaxAngle = 120f;
+    public const float MinInitialOffset = 40f;
+    public const float MaxInitialOffset = 70f;
+    public const float DriftSpeed = 28f;
+
+    static readonly Dictionary<DamageElement, Color> ElementColors = new Dictionary<DamageElement, Color>
+    {
+        { DamageElement.Physical, new Color(1f, 0.92f, 0.35f) },
+        { DamageElement.Fire, new Color(1f, 0.45f, 0.15f) },
+        { DamageElement.Ice, new Color(0.45f, 0.85f, 1f) },
+        { DamageElement.Lightning, new Color(0.75f, 0.7f, 1f) },
+        { DamageElement.Poison, new Color(0.55f, 0.95f, 0.35f) },
+        { DamageElement.Holy, new Color(1f, 0.97f, 0.8f) },
+        { DamageElement.Shadow, new Color(0.7f, 0.45f, 0.95f) }
+    };
+
+    static Canvas _sharedCanvas;
+    static readonly List<DamageNumber> _pool = new List<DamageNumber>();    // 未激活
+    static readonly List<DamageNumber> _active = new List<DamageNumber>();  // 活跃
+
+    Text _text;
+    CanvasGroup _group;
+    float _age;
+    float _appliedMultiplier = 1f;
+    float _appliedFontSize = BaseFontSize;
+    DamageElement _element = DamageElement.Physical;
+    Vector2 _start;
+    Vector2 _dir = Vector2.up;
+    float _initialOffset;
+
+    // ---- 测试/调试只读面 ----
+    public static Canvas SharedCanvas { get { return _sharedCanvas; } }
+    public static int ActiveCount { get { return _active.Count; } }
+    public static int PooledCount { get { return _pool.Count; } }
+    public float AppliedMultiplier { get { return _appliedMultiplier; } }
+    public float AppliedFontSize { get { return _appliedFontSize; } }
+    public DamageElement Element { get { return _element; } }
+    public float Age { get { return _age; } }
+
+    /// <summary>入口：世界坐标（敌人本体位置）+ 伤害数值 + 伤害元素。</summary>
+    public static void Show(Vector3 worldPos, float amount, DamageElement element)
+    {
+        Canvas canvas = EnsureCanvas();
+        if (canvas == null) return;
+        DamageNumber dn = Rent();
+        if (dn == null) return;
+        dn.Play(worldPos, amount, element);
+    }
+
+    public static float ComputeMultiplier(float amount)
+    {
+        float t = Mathf.InverseLerp(MinDamage, MaxDamage, amount);
+        return Mathf.Clamp(Mathf.Lerp(MinScaleMultiplier, MaxScaleMultiplier, t),
+                           MinScaleMultiplier, MaxScaleMultiplier);
+    }
+
+    public static Color ColorFor(DamageElement element)
+    {
+        Color c;
+        return ElementColors.TryGetValue(element, out c) ? c : Color.white;
+    }
+
+    /// <summary>覆盖某元素配色（替代 SO 资产；默认表见 ElementColors）。</summary>
+    public static void SetElementColor(DamageElement element, Color color)
     {
-        var go = new GameObject("DamageNumber");
-        go.transform.position = worldPos + Vector3.up * 0.25f;
+        ElementColors[element] = color;
+    }
+
+    // ============================================================
+    // 画布 / 池
+    // ============================================================
+    static Canvas EnsureCanvas()
+    {
+        if (_sharedCanvas != null) return _sharedCanvas;
+
+        // 域重载关闭等场景下复用已存在的画布，避免出现第二个 Overlay 画布
+        var existing = GameObject.Find(OverlayName);
+        if (existing != null)
+        {
+            var c = existing.GetComponent<Canvas>();
+            if (c != null)
+            {
+                _sharedCanvas = c;
+                if (_pool.Count == 0 && _active.Count == 0)
+                {
+                    foreach (var dn in c.GetComponentsInChildren<DamageNumber>(true))
+                    {
+                        if (dn == null) continue;
+                        dn.gameObject.SetActive(false);
+                        _pool.Add(dn);
+                    }
+                }
+                return _sharedCanvas;
+            }
+        }
+
+        var go = new GameObject(OverlayName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
+        var canvas = go.GetComponent<Canvas>();
+        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
+        canvas.sortingOrder = 5000;
+        var scaler = go.GetComponent<CanvasScaler>();
+        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
+        scaler.referenceResolution = new Vector2(1920f, 1080f);
+        scaler.matchWidthOrHeight = 0.5f;
+        Object.DontDestroyOnLoad(go);
+
+        _sharedCanvas = canvas;
+        return _sharedCanvas;
+    }
+
+    static DamageNumber Rent()
+    {
+        for (int i = _pool.Count - 1; i >= 0; i--)
+        {
+            var d = _pool[i];
+            _pool.RemoveAt(i);
+            if (d == null) continue;
+            _active.Add(d);
+            return d;
+        }
+
+        if (_active.Count >= PoolCapacity && _active.Count > 0)
+        {
+            // 池已满且无空闲：抢占最旧活跃实例，保证同时存在实例数 ≤ 容量
+            var oldest = _active[0];
+            _active.RemoveAt(0);
+            if (oldest != null)
+            {
+                _active.Add(oldest);
+                return oldest;
+            }
+        }
+
+        var created = CreateInstance(EnsureCanvas());
+        if (created != null) _active.Add(created);
+        return created;
+    }
+
+    static DamageNumber CreateInstance(Canvas canvas)
+    {
+        if (canvas == null) return null;
+
+        var go = new GameObject("DamageNumber", typeof(RectTransform), typeof(CanvasGroup));
+        go.transform.SetParent(canvas.transform, false);
 
-        var canvas = go.AddComponent<Canvas>();
-        canvas.renderMode = RenderMode.WorldSpace;
         var rt = (RectTransform)go.transform;
-        rt.sizeDelta = new Vector2(200f, 60f);
-        // 基线世界宽度约 1.2 米（0.006）；倍率由 CombatFeedback.damageNumberScale 传入（默认 3.5x）
-        rt.localScale = Vector3.one * (0.006f * Mathf.Max(0.01f, scaleMultiplier));
-
-        var tgo = new GameObject("txt");
-        tgo.transform.SetParent(go.transform, false);
-        var text = tgo.AddComponent<Text>();
-        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");   // ★ 脚本创建的 Text 不会自动带字体（编辑器菜单创建才触发 Reset）
-        text.rectTransform.sizeDelta = new Vector2(200f, 60f);
-        text.text = content;
-        text.fontSize = 40;
+        rt.anchorMin = Vector2.zero;
+        rt.anchorMax = Vector2.zero;
+        rt.pivot = new Vector2(0.5f, 0.5f);
+        rt.sizeDelta = new Vector2(240f, 80f);
+        rt.localScale = Vector3.one;
+
+        var textGo = new GameObject("txt", typeof(RectTransform));
+        textGo.transform.SetParent(go.transform, false);
+        var text = textGo.AddComponent<Text>();
+        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
         text.alignment = TextAnchor.MiddleCenter;
-        text.color = color;
         text.raycastTarget = false;
+        text.horizontalOverflow = HorizontalWrapMode.Overflow;
+        text.verticalOverflow = VerticalWrapMode.Overflow;
+        var tRT = text.rectTransform;
+        tRT.anchorMin = Vector2.zero;
+        tRT.anchorMax = Vector2.one;
+        tRT.offsetMin = Vector2.zero;
+        tRT.offsetMax = Vector2.zero;
 
-        go.AddComponent<DamageNumber>()._text = text;
-    }
+        var group = go.GetComponent<CanvasGroup>();
+        group.alpha = 1f;
+        group.interactable = false;
+        group.blocksRaycasts = false;
 
-    Text _text;
-    float _life = 0.8f;
+        var dn = go.AddComponent<DamageNumber>();
+        dn._text = text;
+        dn._group = group;
+        go.SetActive(false);
+        return dn;
+    }
 
-    void Update()
+    void Play(Vector3 worldPos, float amount, DamageElement element)
     {
-        transform.position += Vector3.up * (Time.deltaTime * 0.9f);
-        if (Camera.main != null) transform.rotation = Camera.main.transform.rotation;  // 面向相机
+        gameObject.SetActive(true);
+        _age = 0f;
+        _element = element;
+        _appliedMultiplier = ComputeMultiplier(amount);
+        _appliedFontSize = BaseFontSize * _appliedMultiplier;
 
-        _life -= Time.deltaTime;
         if (_text != null)
         {
-            var c = _text.color;
-            c.a = Mathf.Clamp01(_life / 0.35f);
-            _text.color = c;
+            _text.text = Mathf.RoundToInt(amount).ToString();
+            _text.fontSize = Mathf.RoundToInt(_appliedFontSize);
+            _text.color = ColorFor(element);
+        }
+        if (_group != null) _group.alpha = 1f;
+
+        Canvas canvas = _sharedCanvas != null ? _sharedCanvas : EnsureCanvas();
+        Vector2 screen = ScreenCenter();
+        Camera cam = ResolveCamera();
+        if (cam != null)
+        {
+            Vector3 sp = cam.WorldToScreenPoint(worldPos + Vector3.up * ChestHeight);
+            screen = new Vector2(sp.x, sp.y);
         }
-        if (_life <= 0f) Destroy(gameObject);
+
+        Vector2 local;
+        if (canvas != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
+                (RectTransform)canvas.transform, screen, null, out local))
+        {
+            _start = local;
+        }
+        else
+        {
+            _start = Vector2.zero;
+        }
+
+        float angle = Random.Range(MinAngle, MaxAngle) * Mathf.Deg2Rad;
+        _dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
+        _initialOffset = Random.Range(MinInitialOffset, MaxInitialOffset);
+
+        var rt = (RectTransform)transform;
+        rt.anchoredPosition = _start;
+        rt.localScale = Vector3.one * PopStartScale;
+    }
+
+    static Vector2 ScreenCenter()
+    {
+        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
+    }
+
+    static Camera ResolveCamera()
+    {
+        Camera cam = Camera.main;
+        if (cam != null) return cam;
+        var cams = Camera.allCameras;
+        return cams.Length > 0 ? cams[0] : null;
+    }
+
+    void Update()
+    {
+        _age += Time.deltaTime;
+
+        float jump = Mathf.Clamp01(_age / PopDuration);
+        float ease = 1f - (1f - jump) * (1f - jump);
+        float drift = Mathf.Max(0f, _age - PopDuration) * DriftSpeed;
+
+        var rt = (RectTransform)transform;
+        rt.anchoredPosition = _start + _dir * (_initialOffset * ease) + Vector2.up * drift;
+        rt.localScale = Vector3.one * PopScale(_age);
+
+        if (_group != null)
+            _group.alpha = Mathf.Clamp01((Lifetime - _age) / FadeDuration);
+
+        if (_age >= Lifetime) ReturnToPool();
+    }
+
+    static float PopScale(float age)
+    {
+        if (age < PopDuration)
+            return Mathf.Lerp(PopStartScale, PopPeakScale, age / PopDuration);
+        return Mathf.Lerp(PopPeakScale, PopEndScale, (age - PopDuration) / (Lifetime - PopDuration));
+    }
+
+    void ReturnToPool()
+    {
+        gameObject.SetActive(false);
+        if (_active.Remove(this)) _pool.Add(this);
     }
-}
\ No newline at end of file
+}
```

> 新增测试文件（9 个，含 `.meta`）为全新文件，其完整 diff 见 `Docs/QA/Evidence/SPEC-01/SPEC01_NewTestFiles.diff`；以上为 4 个生产代码文件的完整 diff。

---

## 7. 测试夹具边界

- `SPEC01_Target_Combat`：运行期内存 `UnitHealth`（高 HP、无 AI、`showDamageNumber=false`），用于攻击路径/时钟；结束前销毁。
- `SPEC01_Target_UI`：运行期内存 `UnitHealth`（高 HP、无 AI、`showDamageNumber=true`），用于飘字采样；结束前销毁。
- 采样隔离：UI 断言期间临时禁用玩家 `CharacterBrain` 并关闭其它单位 `showDamageNumber`，结束后恢复；避免自动战斗在采样窗内产生额外飘字。
- 真实敌人 `Skeleton_Warrior` 在 Regression 后 `SetActive(false)` 隔离；不写入场景。
- 移动一律走生产 `SelectionManager`/`CharacterBrain.CommandMove`，不使用 Warp/`SetDestination`/写坐标。
- 不装配场景、不保存场景；仅写 `Docs/QA/Evidence/SPEC-01/` 与 SessionState 退出码。

---

## 8. 未覆盖项

1. 飘字“随机 60°–120° 扇区、40–70px 初位移、不遮挡”的**视觉观感**未做像素级断言（仅代码常量 + 人工目检 S01-2）。
2. `DamageElement` 配色映射仅验证 `Physical` 生产路径；其余元素颜色未逐项采样。
3. “单周期恰 1 次命中”沿用 FND-02B 口径（`hits` 与 `AttackMelee` 进入次数容差 ≤1），未做逐周期精确配对。
4. 法师施法剪辑选型属所有者资产管辖，本单未验证。
5. `FND00B06` 未运行（范围外，见 §10.1）。
6. 旧 `FND02B` 测试中的 `UI.DamageNumberScale` 断言未修改（不在本单允许改动面）；它按旧 3.5x 口径编写，已被 SPEC-01 套件取代。

---

## 9. 实现原理（面向项目所有者，≤500 字）

**根因**：控制器已改为 `AttackMelee`/`AttackRange`，但 `SwordSwing` 仍硬编码 `SetTrigger("Attack")`，骑士进不了攻击状态，命中退化到代码时钟、出伤停在 `Locomotion`；旧飘字每伤害新建 WorldSpace Canvas，无池、无分档。

**方案**：① `SwordSwing` 新增 `attackTrigger`/`hitStateName`（默认 `AttackMelee`），出招/取消用字段，命中窗口读该状态 `normalizedTime`；不复用 `releaseStateName`，零资产改动。② `DamageNumber` 改共享 Overlay 画布单例+32 池；出生取胸口 +1.2m 投影，随机 60°–120°、40–70px 跳出；伤害→倍率 `Clamp(Lerp(1.5,5,t),1.5,5)` 映射字号。③ `CombatFeedback` 传 `finalAmount`+`element`。④ `DamageInfo` 加 `DamageElement`。

**为什么**：不改资产即落地；Overlay+池消除每伤害画布。

---

## 10. 受影响模块导航（≤200 字）

- `SwordSwing`：执行器；入口 `AutoAttack`/`DoSkillAttack`；`UnitHealth.TakeHit`。
- `CombatFeedback`：订阅伤害事件 → `DamageNumber.Show`。
- `DamageNumber`：Overlay 画布+池+投影/跳出/分档/淡出；入口 `Show`。
- `DamageInfo`：伤害数据包，新增 `element`。

---

## 11. 遗留问题与登记（不在本单修复）

1. **FND00B06（范围外，B6 只登记）**：`FND00B06_ProbeRunner` 的 `RequiredParams` 含 `"Attack"`、`RequiredStates` 含带前导空格的 `" Locomotion"`，与所有者现行控制器（`AttackMelee`/`AttackRange`、`Locomotion` 无前导空格）不一致。该不一致在阶段 0（本单任何代码改动前）就已存在；本单未运行 `FND00B06`（唯一入口只有 `SPEC01_BatchAcceptance.Run`），也未修改其测试文件。若后续运行它，预期相关断言会红，需单独开单。
2. **B2（范围外）**：`Interrupt` 无过渡，仅登记。
3. **旧 FND02B 套件**：`UI.DamageNumberScale` 仍按旧 3.5x 口径；本单未改（允许改动面之外），后续由 SPEC-01 取代或单独收口。
4. **`damageNumberScale` 移除**：`CombatFeedback` 上该 Inspector 字段已删除（改为伤害驱动），场景中成为孤立序列化键（未保存场景，无资产改动）。
5. **`DamageInfo.element` 写入方**：当前所有生产 `TakeHit/ApplyDamage` 调用未显式设置元素，故均为 `Physical`；后续元素伤害需在伤害入口赋值（本单按方案 B 只建轴）。
6. **元素配色“Inspector 可覆盖”**：实现为静态字典 + `SetElementColor` API（无 SO 资产、无资产改动）；如需 Inspector 表，需另行批准在场景组件上加配置表。
7. **`hitStateName` 选型理由（已定案登记）**：默认 `"AttackMelee"`，不复用 `releaseStateName`——单段语义清晰、避免两段式误读，且新字段缺省即生效、无需改预制体。
8. **首轮 red 编译失败**：测试夹具问题已修正；保留日志于 `_attempts/`，非产品失败。

---

## 12. 人工目检（执行者只列待目检，不代填）

- `S01-1`：骑士单段出招与“刀落窗口”手感（Inspector 可调 `activeStart/activeEnd`）。
- `S01-2`：飘字方向随机/大小分档/多段伤害不堆叠不遮挡。
- `S01-3`：弓/法原两段表现无回归。

---

## 13. 建议提交

> 本单改动为混合改动（生产代码 + 新增测试 + 证据/报告）。建议拆分如下；`Assets/_Project/Art/**`、`Assets/_Project/Prefabs/**`、`RangedCombat.cs`、`CharacterBrain.cs`、`UnitAnimatorSpeed.cs`、`CURRENT.md`、`AGENTS.md` 等为**会话开始前既有/系统级改动**，不属于本单，请由所有者另行处理。

**提交 1（生产代码）**

```
feat(combat): 近战单段攻击接入 AttackMelee 并重做伤害飘字

why: 旧代码硬编码 Attack 触发器且每次伤害新建 WorldSpace 画布，导致骑士无法进入新单段状态、飘字无池且尺寸与伤害脱钩。
```

```powershell
git add Assets/_Project/Scripts/Combat/SwordSwing.cs `
        Assets/_Project/Scripts/Combat/DamageInfo.cs `
        Assets/_Project/Scripts/Combat/CombatFeedback.cs `
        Assets/_Project/Scripts/Combat/DamageNumber.cs
```

**提交 2（测试）**

```
test(spec01): 新增 SPEC-01 批处理验收与证据入口

why: 用可重复的 red/green 套件锁定单段攻击窗口与飘字共享画布/池/分档口径。
```

```powershell
git add Assets/_Project/Tests/SPEC01.meta `
        Assets/_Project/Tests/SPEC01/Editor.meta `
        Assets/_Project/Tests/SPEC01/Runtime.meta `
        Assets/_Project/Tests/SPEC01/Editor/SPEC01_BatchAcceptance.cs `
        Assets/_Project/Tests/SPEC01/Editor/SPEC01_BatchAcceptance.cs.meta `
        Assets/_Project/Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs `
        Assets/_Project/Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs.meta `
        Assets/_Project/Tests/SPEC01/Runtime/SPEC01_Evidence.cs `
        Assets/_Project/Tests/SPEC01/Runtime/SPEC01_Evidence.cs.meta `
        Assembly-CSharp.csproj `
        Assembly-CSharp-Editor.csproj
```

> `.csproj` 为 Unity 自动重生成（仅新增 3 条 SPEC01 编译项），随测试一起提交以保持工程可编译。

**提交 3（文档与证据）**

```
docs(spec01): 记录 SPEC-01 red/green 证据与验收报告

why: 保留逐断言原始数据与完整 diff，供目检、外部顾问复核与终审追溯。
```

```powershell
git add Docs/QA/Evidence/SPEC-01 `
        Docs/Handoff/REPORT_SPEC-01.md
```
