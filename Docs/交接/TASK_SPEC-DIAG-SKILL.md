# TASK — SPEC-DIAG-SKILL：技能施放失效诊断与修复

> **风险等级：中风险**（测试代码新增 + 两处测试夹具修改；是否触碰生产 `.cs` 由探针结论决定）
> **所有者授权**：2026-11-05 睡前授权长任务模式。本文档落盘即视为批准：执行者**不得停下等确认**，直接执行；只有触发【熔断】或判定为**资产根因**时才停，并按本文 §6 输出精确资产改动清单。
> **回滚锚**：`7cf874f7c5db448215b33a90fcad1d209f1fb781`（HEAD，SPEC-01-CLEANUP 未提交改动的基线）
> **工作区现状**：存在所有者已知、待其手动提交的 SPEC-01-CLEANUP 未提交改动。执行者**禁止任何 git 写操作**（add/commit/stash/restore/checkout 一律禁止）；不得回滚或覆盖他人改动；本单改动须在 REPORT 单独列 diff。

---

## 1. 背景与症状

- 骑士按 `Q`（盾击 / Bash）**无技能指示器、无 Console 报错、无任何可见反应**；`W` 历史失效并案。
- 症状特征：施法流程在"进入瞄准"之前就静默结束。
- 线索：技能曾可用；所有者怀疑与 NavMesh 烘焙失效同期损坏。
- 本项目的验收与修复验证默认走自动化断言（batchmode），人工只保留 ≤3 项目检。

### 1.1 本单目标

1. 用可复现证据定位**第一个**失效节点（不是猜）。
2. 只做根因修复，禁止表面补丁。
3. 把技能路径纳入 `SPEC01` 永久回归套件（本路径此前**完全无测试覆盖**）。
4. 顺手闭合 FND-00B-06 `Animation.Hit` 已定案夹具问题。

### 1.2 非目标

- 不新增玩法、不改技能数值与手感。
- 不重构 `SkillSystem` / 效果链架构。
- 不装包、不迁移 Unity、不动 NavMesh 烘焙。
- 不修「E/R 空槽静默无提示」「开局无默认选中单位」等 UX 缺口（只在 REPORT 登记）。

---

## 2. 大脑只读核对结论（执行者必须用断言独立复核，不得直接采信）

### 2.1 施法输入链路（`Assets/_Project/Scripts/Player/SkillInputController.cs`）

按键 → 指示器的唯一路径：

```
Update() [37]
 ├─ PauseManager.IsPaused → return              [40] 静默
 ├─ UIManager.IsInventoryOpen → return          [41] 静默
 ├─ PrimarySkillSystem()                        [43]
 │    └─ SelectionManager.instance == null || Primary == null → null   [151]
 ├─ _chase 分支（追放中）                        [46-76]
 ├─ IsTargeting 分支（瞄准/点选/取消）           [78-110]
 ├─ if (ss == null) return;                     [112] ★ 静默早退 A
 └─ for i in 0..min(skillKeys.Length,4)          [114]
      ├─ !GetKeyDown(skillKeys[i]) → continue    [116]
      ├─ s == null || s.effects == null || Length==0 → continue   [118] ★ 静默早退 B
      ├─ quickCast → ss.Cast(i, NearestEnemy(...))                  [121]
      └─ else → _pendingIndex=i; IsTargeting=true; indicator.Show() [124-126]
```

- **A（空主选）**：未选中任何单位时 `ss == null`，直接 `return`，无日志。
- **B（空槽/空效果链）**：`continue`，无日志。骑士 E/R 是空槽，属设计内静默。
- **C（控制器不存在）**：若场景里根本没有本组件，`Update()` 不执行 → 无指示器、无报错、无任何反应，**比 A/B 更早**。
- 指示器 `SkillIndicator.cs` 的在地投影是**纯数学**（相机射线与 `y=0` 平面求交，`CursorGroundPoint` [61-69]），**不依赖 NavMesh，也不做 Physics.Raycast**；落空时回退到施法者位置，不会抛错、不会吞异常。因此"指示器依赖 NavMesh/射线导致静默失败"不成立。
- `skillKeys` 是**逐实例序列化数组**（`public KeyCode[] skillKeys = {Q,W,E,R}` [22]）。若场景组件被手改/丢失，按键映射不会自动恢复。

### 2.2 场景与预制体核对（只读）

- **`SkillInputController`（脚本 GUID `1838f28ef8538c44cbd56e6e42391d1c`）在全部 3 个场景与 15 个预制体中引用数 = 0。**
  - `Assets/Scenes/DungeonWorld.unity`（MVP 实际游玩场景）只有 `SkillIndicator` 对象（GameObject `582970960` / 组件 `582970962`），`mat` 已赋值。
  - `Managers` 对象（GameObject `6932195` / Transform `6932200`）当前组件：`CombatFeedback(6932196)`、`CursorManager(6932197)`、`PlayerCommandController(6932198)`、`SelectionManager(6932199)`，**没有 `SkillInputController`**。
  - 全仓 `.cs` 中**没有** `AddComponent<SkillInputController>()` 之类的运行期补挂。→ 运行期 `SkillInputController.instance == null`。
  - `CursorManager`/`SelectionManager`/`PlayerCommandController` 均对 `instance` 做了 null 防护，所以**不会产生 Console 报错**——与症状完全吻合。
- **骑士预制体槽位正常**：`Assets/_Project/Prefabs/Knight_Unit.prefab` 的 `SkillSystem`（`&1142945820008185855`）：
  - `skills[0] = 4264a152333311e418432a24f68fd8cf`（`Date/Skills/Q_Bash.asset`，盾击）
  - `skills[1] = 390bc8731f7ab274199ccd7e06552b2a`（`Date/Skills/W_HeavySlash.asset`，跳劈）
  - `skills[2] = 0`、`skills[3] = 0`（E/R 空槽）
  - `mana`、`sword`（`MeleeCombat`，GUID `9ec9f7a6099e5204fb03ca3a12b8afdd`）均已赋值。
- **效果链完整**：`Q_Bash.asset` → `Q_Bash_Effect.asset`（脚本 `b87811a1…` = `BashEffect`）；`W_HeavySlash.asset` → `W_Slash_Effect.asset`（同脚本 = `HeavySlashEffect`）。`needTarget`/`range`/时序字段齐全。
- **动画侧**：`PlayerBase.controller` 存在 `Bash` 触发器与 `Bash` 状态、`AttackMelee` 状态、`Hit` 触发器与 `Hit_A` 状态。
- `SkillSystem.Cast`（`Units/SkillSystem.cs:30-49`）顺序：验槽/验效果 → 验冷却 → **扣蓝** → `CanCast` 全部通过才结算。注意：扣蓝在 `CanCast` 之前，`CanCast` 失败会**白扣蓝**（本单不改，登记为未决项）。

**结论（待探针复核）**：技能数据链、动画链、指示器资产均完好；失效点在**输入层**——`DungeonWorld` 场景丢失 `SkillInputController` 组件，`Update()` 根本没跑。若探针证实，则根因属**场景资产**，按 §6 只出清单、不代改。

---

## 3. 授权改动范围（受影响文件与符号清单）

> 本清单即"影响面闸门"清单，已随计划批准。超出此清单即越界，须停下报告。

### 3.1 允许新增（仅测试）

| 文件 | 说明 |
|---|---|
| `Assets/_Project/Tests/DIAG/Editor/DIAG_BatchAcceptance.cs` | batchmode 唯一入口 `DIAG_BatchAcceptance.Run`，仿 `SPEC01_BatchAcceptance` |
| `Assets/_Project/Tests/DIAG/Runtime/DIAG_ProbeRunner.cs` | 一次性探针：场景普查 + 施法链逐节点断言 |
| `Assets/_Project/Tests/DIAG/Runtime/DIAG_Evidence.cs` | 极简 JSONL 证据写入（写 `Docs/QA/Evidence/DIAG-SKILL/`） |
| `Assets/_Project/Tests/DIAG/Runtime/SkillCastFixture.cs` | 施法夹具（实例化 Knight → 造靶 → 逐槽施放），供 DIAG 与 SPEC01 共用 |
| 上述目录/文件对应 `.meta` | 由 Unity 生成或手写标准 `MonoImporter` 文本，**不得改其他 `.meta`** |

### 3.2 允许修改（仅测试）

| 文件 | 符号 / 位置 | 改动 |
|---|---|---|
| `Assets/_Project/Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs` | `Start()` 断言序列；新增 `AssertSkillCastSmoke()`、`AssertSkillInputWired()` | 追加 `Skill.CastSmoke`、`Skill.InputWired` 两条断言 |
| `Assets/_Project/Tests/FND00B/Runtime/FND00B06_ProbeRunner.cs` | `HitAnimationFixture()`（约 1019-1096 行） | 仅补"受击前确保 Locomotion 态"的前置，不改断言语义 |

> 该文件当前已在 SPEC-01-CLEANUP 未提交改动中。**只允许追加本单的最小 hunk**，REPORT 必须单独列出本单 hunk 的 before/after，便于所有者拆分提交。

### 3.3 允许新增（文档）

- `Docs/QA/Evidence/DIAG-SKILL/**`（日志、JSONL、Summary、前置确认）
- `Docs/Handoff/REPORT_SPEC-DIAG-SKILL.md`

### 3.4 绝不触碰（硬禁止）

- 任何 `.unity` / `.prefab` / `.controller` / `.anim` / `.asset` / `.mat` / `.meta`（除 §3.1 新增测试的 `.meta`）
- `Assets/_Project/Date/Skills/*`、`Assets/_Project/Prefabs/Knight_Unit.prefab`、`Assets/Scenes/DungeonWorld.unity`
- `ProjectSettings/`、`Packages/`、`Assets/Scenes/SampleScene.unity`、`Assets/Scenes/Playground.unity`
- 所有生产运行时代码（`Assets/_Project/Scripts/**`）：**本单默认零改动**；仅当探针证明存在独立代码缺陷、且属最小根因修复时才允许，并须在 REPORT 单列理由与 diff
- `AGENTS.md`、`Docs/00_项目事实基线.md`、`Docs/Handoff/CURRENT.md`、任何 `TASK_*`、任何既存 `REPORT_*`
- 不做任何 git 写操作；不删除、不重命名、不移动既存文件

---

## 4. 执行步骤（按顺序，逐步留证）

### Step 0 — 前置自证（替代人工确认，写证据）

1. 确认编辑器已关闭，结果写入 `Docs/QA/Evidence/DIAG-SKILL/00_前置确认.txt`：
   - `Get-Process -Name Unity` 计数必须 = 0（`Unity Hub` 不算编辑器，允许存在）；
   - `Test-Path .\Temp\UnityLockfile` 必须 = False；
   - 记录 Unity 可执行路径 `D:\unity\2022.3.62f3c1\Editor\Unity.exe` 存在、`ProjectSettings/ProjectVersion.txt` 版本。
2. **若任一条件不满足 → 立即停止，写现场报告（熔断）**，不得启动 Unity。
3. 记录 `git rev-parse HEAD`（只读）与 `git status --porcelain` 快照（只读），声明本单不写 git。

### Step 1 — 静态路径复核（只读）

- 按 §2.1 复读 `SkillInputController.Update()`，把**当前代码**的全部静默分支按执行顺序列成表（分支 / 行号 / 触发条件 / 有无日志 / 是否可能命中）。
- 重点回答：
  1. 空技能槽（E/R）行为 = `continue`，无任何反馈；
  2. 指示器地面投影 = 相机射线与 `y=0` 求交，**不依赖 NavMesh / Physics**，落空回退，不吞异常。
- 输出：`Docs/QA/Evidence/DIAG-SKILL/01_静态路径清单.md`，并记录"第一个不可达节点 = `Update()` 本身未执行（待 Step 3 证实）"。

### Step 2 — 资产核对（只读，不得改）

- 复核 §2.2 全部条目，逐条给出 `文件:GUID:字段` 证据。
- 输出：`Docs/QA/Evidence/DIAG-SKILL/02_资产核对.md`。
- **此步一旦发现槽位/效果链/动画缺失，仍不得修改**：转入 §6，只写清单，同时继续 Step 5/6 的测试工作。

### Step 3 — 一次性 batch 探针（定位第一个退出点）

新增 `Assets/_Project/Tests/DIAG/`（含 `.meta`），唯一入口 `DIAG_BatchAcceptance.Run`。命令行与 SPEC01 同构：

```powershell
$env:DIAG_MODE="probe"
& "D:\unity\2022.3.62f3c1\Editor\Unity.exe" -batchmode -projectPath "D:\unity\Dungeon" `
  -executeMethod DIAG_BatchAcceptance.Run `
  -logFile "D:\unity\Dungeon\Docs\QA\Evidence\DIAG-SKILL\DIAG_probe.log"
```

探针必须输出以下断言（pass/fail 均记录实际值）：

| 断言 ID | 期望 | 说明 |
|---|---|---|
| `Diag.Scene.SkillInputController` | 计数 1 | 打开 `DungeonWorld` 并进入 Play 后 `FindObjectsByType<SkillInputController>` 计数。**当前预期 0 = 复现** |
| `Diag.Scene.Indicator` | 计数 1，`mat != null`，且存在消费者 | 指示器存在但无输入消费者 = 孤儿组件 |
| `Diag.Input.FirstExit` | `MISSING_CONTROLLER` | 枚举退出点：`MISSING_CONTROLLER` / `NO_PRIMARY` / `EMPTY_SLOT` / `NO_EFFECTS` / `NONE` |
| `Diag.Prefab.Slots` | Q/W 非空、E/R 空 | 读 `Knight_Unit` 的 `SkillSystem.skills` 长度=4 与各资产名 |
| `Diag.CastChain.Q` | 全绿 | 用 `SkillCastFixture` 直接 `SkillSystem.Cast(0, enemy)`：返回 true、蓝 -20、CD>0、靶掉血（命中帧后）、Animator 进入 `Bash` |
| `Diag.CastChain.W` | 全绿 | `Cast(1, null)`：返回 true、蓝 -30、CD>0、`MeleeCombat` 计时启动（反射读 `_timer >= 0`）、Animator 进入 `Skill` |
| `Diag.CastChain.EmptySlots` | 契约成立 | `Cast(2/3)` 返回 false、蓝不变、无异常（记录为设计内静默） |

结论必须明确写成一句："下游施法链 `Q/W` 可独立跑通；失效点在输入层，第一个未执行节点 = `SkillInputController.Update()`"。
证据：`Docs/QA/Evidence/DIAG-SKILL/DIAG_probe.log` + `DIAG_Assertions.jsonl` + `DIAG_Summary.json` + `DIAG_ExitCode.txt`。

### Step 4 — 根因修复（受 §6 约束）

- **若 Step 3 证实根因为"场景缺少 `SkillInputController` 组件"**：属资产根因。
  - **禁止**修改 `DungeonWorld.unity`、禁止用编辑器脚本写入场景。
  - 停止该分支，按 §6 写精确改动清单到 REPORT 与 `04_资产改动清单.md`；**继续执行 Step 5、Step 6**（测试防线不依赖资产修复）。
  - 可以补的最小代码改动：**无**。不要为了"让按键工作"而加运行期自挂组件——那是掩盖资产丢失的表面补丁，明确禁止。
- **若 Step 3 证明另有代码根因**（如 `skillKeys` 逻辑、`Cast` 顺序、某处异常被吞）：允许最小 `.cs` diff，要求：
  - 先写 red 断言复现，再改到 green；
  - N≥3 次重复运行；
  - diff 必须能一句话说清"为什么这就是根因"。
- 任何"顺手优化"一律不做。

### Step 5 — `Skill.CastSmoke` 并入 SPEC01（永久回归防线）

在 `SPEC01_ProbeRunner.Start()` 的断言序列中追加（位置在 `UI.SpawnAnchor` 之后、`RecordRegressionFinal()` 之前）：

```csharp
yield return RunSafe("Skill.CastSmoke", AssertSkillCastSmoke());
yield return RunSafe("Skill.InputWired", AssertSkillInputWired());
```

**`Skill.CastSmoke`（期望 green）**：用 `SkillCastFixture`（预制体在运行期实例化、结束前销毁，不写场景）：

1. Q（slot 0）：`Cast(0, enemyInRange)` 返回 true；施法后同帧蓝量减少 20（误差 <0.01，避开回蓝干扰）；`CdRemaining(0) > 0` 且 ≈ 6s；等待 `hitTime + 余量` 后靶 HP 下降且等于 `MeleeCombat.GetRawDamage()`（误差 <0.01）；Animator 在 0.5s 内进入 `Bash`。
2. W（slot 1）：`Cast(1, null)` 返回 true；蓝减少 30；`CdRemaining(1) > 0`；`MeleeCombat` 的 `_timer >= 0`（反射，证明效果已执行）；Animator 在 0.5s 内进入 `Skill`。
3. E/R（slot 2/3）：`Cast` 返回 false、蓝量不变、无异常；数据里标注"空槽=设计内静默，非缺陷"。
4. 数据字段：`manaBefore/After`、`cdRemaining0/1`、`targetHpBefore/After`、`expectedDamage`、`animState0/1`、`slotNames`、`fixturePath`。
5. 断言名固定 `Skill.CastSmoke`，`kind=production`。

**`Skill.InputWired`（当前预期 `product_fail`，是根因的永久看门狗）**：

- 在 `DungeonWorld` 运行期普查：`FindObjectsByType<SkillInputController>` 计数 == 1；该组件 `indicator != null` 且指向场景中的 `SkillIndicator`；`skillKeys.Length == 4` 且依次为 `Q,W,E,R`。
- 当前实际 0 → **必须如实记 `product_fail`，不得改写成 blocked/observed，不得为了让套件变绿而删断言**。
- 所有者补组件后，本条与 `Skill.CastSmoke` 应同跑 `run1..run3` 全绿（10/10，exit 0）。

**green 口径（本单）**：`SPEC01` 断言总数 8 → 10；允许且仅允许 `Skill.InputWired` 为 `product_fail`（fail=1，exit=2），其余全 pass；`Infra=0`。REPORT 必须原样写明"exit=2 是已知资产缺陷，不是测试失败"。

### Step 6 — 闭合 FND-00B-06 `Animation.Hit`（已定案：夹具病）

**定案**：受击动画（`Hit_A`）由所有者按"仅待机/移动（Locomotion）态可触发"设计；夹具在非 Locomotion 态施加伤害，因此观察不到 `Hit_A`，不是产品缺陷。

执行顺序：

1. **red 基线**：先跑一次现状 `FND00B06_BatchAcceptance.Run`，把 `Animation.Hit` 的 `preState / hitStateObserved / hitState` 原样留证到 `Docs/QA/Evidence/DIAG-SKILL/fnd00b06_red.*`。
2. **验证定案**：若 `preState` 已经是 `Locomotion` 但 `Hit_A` 仍未触发，说明定案与证据矛盾 → **停止本步，保留 red 证据并在 REPORT 写"定案不成立"**，不得硬改夹具凑绿。
3. **夹具修复**（仅 `HitAnimationFixture()`）：
   - 创建夹具并禁用 `CharacterBrain` 后，显式把 Animator 参数置为 Locomotion 稳态：`Grounded=true`、`SpeedX=0`、`SpeedZ=0`、`AttackSpeed` 保持现有值；每帧 `Animator.Update` 由引擎驱动即可；
   - 轮询等待（上限 ~2s）`TrimState(state) == "Locomotion"`，把"等待耗时/轮次"写入数据；
   - **前置不成立时用 `BlockedAssertion`**（理由"夹具未能进入 Locomotion"），不要伪造伤害结果；
   - 前置成立后才 `ApplyDamage(5f, ...)`，仍按原口径断言 `Hit_A` + `hitEvents==1` + HP 差值。
4. **green N=3**：连续 3 次 `FND00B06` 全跑，要求 `Animation.Hit` 连续 pass 且数据一致；任一失败按熔断处理。
5. 证据：`fnd00b06_red_*`、`fnd00b06_green_run1..3_*`，汇总写入 `05_Animation_Hit_red_green.md`（含 before/after diff）。

---

## 5. 命令模板与证据命名

```powershell
# SPEC01（环境变量：SPEC01_MODE=green|red，SPEC01_RUN=1..3，SPEC01_EVIDENCE_TAG 可选）
& "D:\unity\2022.3.62f3c1\Editor\Unity.exe" -batchmode -projectPath "D:\unity\Dungeon" `
  -executeMethod SPEC01_BatchAcceptance.Run `
  -logFile "D:\unity\Dungeon\Docs\QA\Evidence\DIAG-SKILL\SPEC01_<tag>_run<N>.log"

# FND00B06
& "D:\unity\2022.3.62f3c1\Editor\Unity.exe" -batchmode -projectPath "D:\unity\Dungeon" `
  -executeMethod FND00B06_BatchAcceptance.Run `
  -logFile "D:\unity\Dungeon\Docs\QA\Evidence\DIAG-SKILL\FND00B06_<tag>_run<N>.log"
```

- 每次运行记录 `pid`、退出码、pass/fail/blocked/infra 计数；**禁止只贴"绿了"不贴数据**。
- 运行前必须再次确认无 `Unity.exe` 与 `Temp/UnityLockfile`（上一次 batch 可能残留）。
- 日志提取只用 `Select-String -Context 2` 定向抓 `[ASSERT]`、`error CS`、`Exception`；**禁止整文件读取日志**。

---

## 6. 资产根因停止规则与预置改动清单

**触发条件**：任何根因落在 `.unity` / `.prefab` / `.controller` / `.asset` / 序列化字段。

**动作**：

1. 不写资产、不用编辑器脚本改资产、不用 `git checkout` 回滚资产。
2. 在 `Docs/QA/Evidence/DIAG-SKILL/04_资产改动清单.md` 与本单 REPORT 中给出**可直接执行**的清单（文件 / GUID / 字段 / 期望值 / 验收命令）。
3. 继续 Step 5、Step 6（代码与测试侧工作不受阻），最后统一报告。

### 6.1 预置清单（若 Step 3 证实"场景缺组件"）

**文件**：`Assets/Scenes/DungeonWorld.unity`

**目标对象**：`Managers`（GameObject `fileID: 6932195`，Transform `6932200`）

**缺失脚本**：`SkillInputController`，GUID `1838f28ef8538c44cbd56e6e42391d1c`

**推荐修法（所有者 30 秒）**：
1. Unity 打开 `DungeonWorld`，选中 `Managers`；
2. Add Component → `SkillInputController`；
3. 把子物体 `SkillIndicator` 拖到 `indicator` 槽（或直接引用已有组件 `fileID: 582970962`）；
4. 确认 `skillKeys` 显示为 `Q W E R`、`quickCast` 未勾选、`enemyCacheInterval=0.25`；
5. 保存场景 → 跑一次 `SPEC01` 验证 `Skill.InputWired` 转绿。

**等价 YAML（仅在所有者明确批准、编辑器已关闭时使用）**：
```yaml
--- !u!114 &6932201
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 6932195}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 1838f28ef8538c44cbd56e6e42391d1c, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  quickCast: 0
  indicator: {fileID: 582970962}
  skillKeys: 71000000770000006500000072000000
  enemyCacheInterval: 0.25
```
并同步在 `&6932195` 的 `m_Component` 列表末尾追加 `- component: {fileID: 6932201}`。
（`skillKeys` 为 `KeyCode` 枚举 int32 序列：Q=113=0x71、W=119=0x77、E=101=0x65、R=114=0x72。）

**验收命令**：`SPEC01_BatchAcceptance.Run` → `Skill.InputWired` 与 `Skill.CastSmoke` 全 pass，10/10、exit 0。

---

## 7. 熔断（任一触发即停，保留现场并写 REPORT）

1. 单步重试 ≥3 次仍红；
2. 编译错误连续 ≥2 轮（`error CS` 出现即记一轮）；
3. 计划外硬阻断（需改资产 / 装包 / 升 Unity / 目录迁移 / 编辑器未关）；
4. git 异常（工作区被他人改动、需要回滚、出现冲突）；
5. 配额或工具撞限。
6. 额外：`FND00B06` red 证据与"夹具病"定案矛盾（见 Step 6.2）。

停时报告必须含：停在哪一步、最后一条原始日志（≤20 行）、已改文件清单、工作区是否干净、下一步建议。

---

## 8. 醒来目检清单（人类，≤3 项）

1. **先选中单位**：进入 `DungeonWorld` 后左键点骑士（或按 `1`）——本作开局**不会自动选中**，未选中时按 Q 依旧无反应（已登记为 UX 缺口，不在本单修）。
2. 按 `Q`：应出现脚下范围圈 + 指向线；左键点 2.5m 内敌人 → 骑士播 `Bash`、敌人掉血并被击退；点空地 → 取消。
3. 按 `W`：应出现指示器；左键点敌人（或空地）→ 骑士播 `Skill` 状态并造成范围伤害。
（若所有者选择"AI 代改 YAML"，则需先人工在编辑器里打开场景确认组件与引用正确，再保存。）

---

## 9. REPORT 要求（`Docs/Handoff/REPORT_SPEC-DIAG-SKILL.md`）

必含：

1. **结果摘要**：根因一句话 + 是否修复 + 阻塞项。
2. **证据锚点**：每结论对应的断言 ID、JSONL 行、日志路径、退出码。
3. **改动文件清单**：新增/修改分列；`SPEC-01-CLEANUP` 已脏文件单独标注"本单仅追加 hunk"，并给出该 hunk 的 before/after。
4. **测试/检查结果**：red → green 的完整数据（pass/fail/infra 计数、关键字段数值），N≥3 的每次结果。
5. **实现原理（面向项目所有者）**：为什么"组件丢失"会造成零报错零反馈；为什么不用运行期自挂组件补；为什么指示器与 NavMesh 无关；为什么测试用夹具而非直接按键盘。
6. **受影响模块导航说明**：`SkillInputController → SkillSystem → SkillEffectBase/BashEffect → MeleeCombat → Animator` 的职责、入口、数据流；测试入口 `SPEC01_BatchAcceptance.Run` / `DIAG_BatchAcceptance.Run` / `FND00B06_BatchAcceptance.Run`。
7. **未决项**：至少登记：①`SkillSystem.Cast` 扣蓝早于 `CanCast`（失败白扣蓝）；②开局无默认选中单位；③E/R 空槽静默无提示；④若适用，资产改动待批。
8. **建议提交**：`git add` 清单 + Conventional Commits 信息（body 一行"为什么"）；明确与 `SPEC-01-CLEANUP` 待提交改动的拆分方案。
9. **不设字数上限**，讲清即止；禁止为压字数反复重写。
