# REFACTOR-M1 — 契约层 + 单位注册（消除全场扫描）

> 长任务 `REFACTOR-LONG-01` 的 M1 小节。上游依据：`C:\Users\lll\.opencode\plan\refactor-plan.md` §2/§9。
> 本单自包含；执行者只读本文件 + 仓库现状即可开工。

## §0 元信息

| 字段 | 值 |
|---|---|
| 任务 ID | `REFACTOR-M1` |
| 风险等级 | `常规`（脚本改动；不碰场景/预制体/Animator/`.asset`/`ProjectSettings`） |
| 授权 | 所有者 2026-11-05 批准长任务计划书；本里程碑在预授权范围内 |
| 基线提交 | `ed0cdc17`（M0 收口；开工时以 `git rev-parse --short HEAD` 记录实际值） |
| 回滚锚 | `ed0cdc17`；回滚 = `git revert <M1 commit>` 或 `git reset --hard ed0cdc17`（仅所有者可执行） |
| Unity 模式 | **模式 B（编辑器常开 + MCP）**，Unity 2022.3.62f3c1 |
| 状态 | `Ready` |

> 执行者**不得**标 Done、不得改 `CURRENT.md`、**不得** `git commit`（提交由大脑/所有者做）。

## §1 需求（Why / What）

**一句话目标**：把 14 处逐帧/多次 `FindObjectsByType<...>` 全场扫描与 3 处单例查找收敛到一个**楼层级单位注册表**，为后续生命周期拆分与预制体基座提供查询契约；行为必须与替换前逐条一致。

**用户故事**：作为重构负责人，我要一个“单位注册 + 按队伍/范围查询”的契约，以便新增职业/敌人时不再靠全场扫描找单位。

**必须满足**

| 编号 | 需求 | 验收方式 |
|---|---|---|
| R-1 | 新增 `Scripts/Core/UnitId.cs`、`Scripts/Core/UnitRegistry.cs`（见 §2 契约） | 代码审查 + 编译 |
| R-2 | `Scripts/**` 下 `FindObjectsByType` / `FindObjectsOfType` / `FindObjectOfType` / `FindFirstObjectByType` / `FindAnyObjectByType` 的**调用点计数 = 0** | `grep` 计数证据 |
| R-3 | 查询语义与替换前一致：默认**排除未激活**对象；需要“含未激活”的调用点必须显式请求 includeInactive | 代码审查 + 行为断言 |
| R-4 | 不发生行为变化：无新增/删除玩法效果，无字段改名，无序列化破坏 | REFACTOR 8/8 + SPEC01 12/12 |

**不在范围**

- 不改 `GameData` / `GameFlow` 生命周期（M2）。
- 不改交互、技能、战斗（M3/M4）。
- 不新增 `asmdef`、不改命名空间式目录迁移、不动场景/预制体/Animator/`.asset`。
- 不优化 `SimplePool` / 迷雾网格 / UI。
- 不改任何现有测试断言的**口径**（允许新增断言，不允许改旧断言）。

**约束**

- `Docs/01` §1.4 不变量 8：**不恢复逐帧全场扫描**；用单位注册与低频感知。
- 序列化字段名冻结：本单不改任何现有字段名。
- 单位生命周期由 `UnitHealth` 承担（所有现有扫描都以 `UnitHealth` 为筛选根）。

## §2 方案（Plan）

**备选方案**

| 方案 | 做法 | 优点 | 缺点 | 结论 |
|---|---|---|---|---|
| A | 保留扫描，改为低频缓存（每 0.5s 一次） | 改动最小 | 仍是全场扫描，违反不变量 8；查询延迟可能改变行为 | 弃用 |
| B | 静态 `UnitRegistry` + `UnitHealth` 自注册/自注销，无新组件 | 零资产改动、零序列化影响、查询 O(n) 但 n 很小 | 身份是运行时句柄，不是稳定 GUID | **选定**：本里程碑只收敛查询，稳定身份留给后续网络里程碑 |
| C | 每个单位挂 `UnitId` MonoBehaviour（预制体加组件） | 身份更“正规” | 需要改预制体（禁止）；域重载/重实例化后 id 维护复杂 | 弃用 |

**选定方案与理由**：B —— 在不动任何资产的前提下，把“谁在场上”这一事实收敛为单一注册源，查询语义可控且可测。

**契约（必须照此实现，命名可微调但语义与可见性不得改）**

```csharp
// Scripts/Core/UnitId.cs
public readonly struct UnitId : System.IEquatable<UnitId>
{
    public int Value { get; }              // 运行时句柄；同一进程内唯一、递增
    public bool IsValid { get; }           // Value != 0
}

// Scripts/Core/UnitRegistry.cs —— 静态、楼层级
public static class UnitRegistry
{
    public static int Count { get; }                                   // 已注册总数（含未激活）
    public static IReadOnlyList<UnitHealth> All(bool includeInactive = false);
    public static IReadOnlyList<UnitHealth> OfTeam(Team team, bool includeInactive = false);
    public static void InRadius(Vector3 center, float radius, List<UnitHealth> results,
                                Team? team = null, bool includeInactive = false);
    public static UnitHealth Nearest(Vector3 center, float maxRange, Team team,
                                     System.Func<UnitHealth, bool> filter = null);
    public static UnitHealth Find(UnitId id);                          // 无效/已注销返回 null
    public static bool TryGetId(UnitHealth unit, out UnitId id);
    public static void Clear();                                        // 场景卸载/重开时清空
}
```

- `UnitHealth.Awake` 末尾注册；`OnDestroy` 注销；`OnEnable`/`OnDisable` 维护“激活”标志（**不得**在 `OnDisable` 时注销，否则 `SetActive(false/true)` 会丢身份）。
- `UnitRegistry` 用 `RuntimeInitializeOnLoadMethod` 订阅 `SceneManager.sceneUnloaded`（或等价）做 `Clear()`；同一进程内 `UnitId.Value` 只增不减。
- 查询实现不得在运行时退回 `FindObjects*`。
- **未激活且从未激活过**的对象不会注册（Unity `Awake` 不跑）。当前仓库无此用例；在 REPORT §7 记一条已知限制即可。

**必须替换的调用点（开工时按实际行号核对，逐个写进 REPORT）**

| # | 文件:行 | 当前用途 | 替换为 |
|---|---|---|---|
| 1 | `Player/SelectionManager.cs:34` | 启动时收集 `Selectable` | `UnitRegistry.All(true)` 后取 `Selectable`（`Selectable` 不是单位，见下方说明） |
| 2 | `Player/SkillInputController.cs:218` | 敌人缓存刷新 | `UnitRegistry.OfTeam(Team.Monster)` |
| 3 | `System/FogOfWar.cs:83` | 迷雾可见性 | `UnitRegistry.All()` / `OfTeam(Team.Player)` |
| 4 | `Debug/DebugTestTools.cs:32` | 调试：含未激活单位 | `All(true)` |
| 5 | `Debug/DebugTestTools.cs:92` | 调试：存活单位 | `All()` |
| 6 | `Core/GameFlow.cs:33` | 是否有玩家队伍 | `OfTeam(Team.Player)` |
| 7 | `Core/GameFlow.cs:56` | 团灭判定 | `OfTeam(Team.Player)` |
| 8 | `System/DungeonDoor.cs:33` | 传送全部玩家 | `OfTeam(Team.Player)` |
| 9 | `Units/BossBrain.cs:176` | 索敌/统计 | `All()` 或 `OfTeam` |
| 10 | `Units/SkeletonWander.cs:62` | 游荡索敌 | `All()` / `Nearest` |
| 11 | `Units/UnitBrain.cs:75` | `FindNearestEnemy` | `UnitRegistry.Nearest(pos, range, 敌方队伍, filter)` |
| 12 | `UI/InventoryUI.cs:32` | 背包绑定玩家 | `OfTeam(Team.Player)` |
| 13 | `Units/Visibility.cs:41` | 可见性 | `All()` |
| 14 | `Units/WorldHealthBar.cs:55` | 血条目标缓存 | `All()`（保留原有缓存刷新节奏） |
| 15 | `Player/SelectionManager.cs:29` | `FindObjectOfType<DebugCameraSwitcher>` | `DebugCameraSwitcher.instance`（新增静态实例，Awake/OnDestroy 维护） |
| 16 | `System/DungeonDoor.cs:43` | 同上 | 同上 |
| 17 | `Debug/DebugTestTools.cs:167` | `FindFirstObjectByType<DebugTestToolsHost>` | `DebugTestToolsHost.instance`（新增静态实例） |

> **`Selectable` 说明**：`Selectable` 与 `UnitHealth` 同物体（玩家/倒地者）。第 1 项只要求“不再全场扫描”，可改为遍历 `OfTeam(Team.Player)` + `GetComponent<Selectable>()`，并保留 `Register()` 语义与原顺序无关性。

**数据流**：`UnitHealth.Awake/OnEnable` → `UnitRegistry.Register/SetActive` → 调用方 `All/OfTeam/InRadius/Nearest` → 返回与替换前相同的集合

**影响面清单**

| 项 | 内容 |
|---|---|
| 新增文件 | `Assets/_Project/Scripts/Core/UnitId.cs`、`Assets/_Project/Scripts/Core/UnitRegistry.cs`（+ 自带 `.meta`，Unity 生成，可提交） |
| 修改文件 | 上表 17 个调用点所在文件（共 11 个文件）+ `Units/UnitHealth.cs`（注册/注销） |
| 修改符号 | `UnitHealth.Awake/OnDestroy/OnEnable/OnDisable`；`DebugCameraSwitcher`、`DebugTestToolsHost` 新增 `static instance`；各调用点内部实现 |
| 字符串引用 | 无新增；**不得**改 Animator 参数名、`SendMessage`、`GameObject.Find` 调用 |
| 场景 / 预制体引用 | **零改动**；不新增组件到预制体 |
| `.meta` | 仅新增两个脚本自带的 `.meta`；不移动/重命名任何资产 |
| 测试文件 | `Assets/_Project/Tests/REFACTOR/Editor/REFACTOR_BatchAcceptance.cs` 的 `DefaultMilestone` 改为 `"M1"` |

**实施顺序**

1. 写 `UnitId` / `UnitRegistry`（先编译通过）。
2. 改 `UnitHealth` 注册/注销。
3. 按表逐项替换（每完成 3–4 项编译一次）。
4. 改 `DefaultMilestone = "M1"`。
5. 跑 L1：REFACTOR ×3 + SPEC01 ×2（编辑器内菜单）。
6. 写 REPORT + 证据。

**风险与回滚**

- 最可能错：查询集合与扫描不一致（尤其“未激活”语义、`Selectable` 收集时机、`Nearest` 的距离口径用 3D 还是 XZ）。
  - 检测：REFACTOR `Party.PresentAndMove` / `Combat.*` / `Rescue.*` + SPEC01 全 12 条。
  - 注意：`UnitBrain.FindNearestEnemy` 原来用 `Vector3.Distance`（含 Y）；`AttackLoop` 用 XZ。`Nearest` 必须按调用方需要分别支持，**默认 3D**，需要 XZ 时由调用方传 filter 或自行比较——**不得**改变现有判定口径。
- 回滚锚：`ed0cdc17`。

## §3 验证（Validation）

**AC 表**

| AC | Given / When / Then | 断言 ID | 证据 |
|---|---|---|---|
| AC-1 | 给定 `Scripts/**`，当 grep `FindObjectsByType\|FindObjectsOfType\|FindObjectOfType\|FindFirstObjectByType\|FindAnyObjectByType`，则**调用点 = 0**（注释与文档字符串不计，需在 REPORT 说明过滤方式） | `Refactor.NoGlobalScan` | `Docs/QA/Evidence/REFACTOR-M1/grep-findobjects.txt` |
| AC-2 | 给定 REFACTOR 套件，连续跑 3 次，则每次 **8/8、fail=0、infra=0、exit=0** | 套件全部 8 条 | `Docs/QA/Evidence/REFACTOR-M1/editor_run1..3/` |
| AC-3 | 给定 SPEC01 套件，跑 2 次，则每次 **12/12、fail=0、exit=0** | SPEC01 全 12 条 | `Docs/QA/Evidence/SPEC-01/editor_run<新>/` |
| AC-4 | 给定注册表契约，则 `All(false)` 不含 `SetActive(false)` 的单位、`All(true)` 含；`OfTeam` 与 `Nearest` 结果与替换前扫描一致 | `Refactor.RegistrySemantics` | 见下“新增断言” |
| AC-5 | 给定 `git status`，则只含本单声明文件（新增 2 脚本 + 11 个调用点文件 + REFACTOR 默认里程碑 + 证据 + REPORT） | — | `REFACTOR-M1/git-diff-stat.txt` |

**新增断言（REFACTOR 套件内加 1 条，不得改旧 8 条口径）**

- `Refactor.RegistrySemantics`：在 Play 模式下
  1. `UnitRegistry.OfTeam(Team.Player, false).Count == 3`；
  2. 临时 `SetActive(false)` 其中一个玩家 → `All(false)` 不含它、`All(true)` 含它，恢复后重新含入；
  3. `UnitRegistry.Nearest(knight.pos, 10f, Team.Monster)` 与“遍历 `All(false)` + 3D 距离最近”结果一致。
- 该断言加在 8 条之后；总断言数变为 9。

**运行次数**：REFACTOR **N=3**（交互/时序）；SPEC01 **2 次**。

**自动化优先**：全部通过 `Tools/QA/In-Editor/{REFACTOR, SPEC01 (green)}` 菜单执行；**不得**用 `run_tests`，不得 batchmode。

**必须保存的证据**：菜单生成的 `REFACTOR_Assertions.jsonl` / `REFACTOR_Summary_run*.json` / `REFACTOR_ExitCode_run*.txt` / `InEditorResult.txt`，SPEC01 同结构；外加 `grep-findobjects.txt`、`git-diff-stat.txt`。

## §4 执行信息

| 项 | 内容 |
|---|---|
| 前置 | Unity 已开（模式 B），MCP 单实例；开工前 `editor_state` 的 `is_compiling=false`；当前场景 `DungeonWorld` 且非脏 |
| 允许改动 | 上表 11 个脚本文件 + `Units/UnitHealth.cs`；新增 `Scripts/Core/UnitId.cs`、`Scripts/Core/UnitRegistry.cs`；`Tests/REFACTOR/**`（新增 1 条断言 + 改 `DefaultMilestone`）；`Docs/Handoff/REPORT_REFACTOR-M1.md`；`Docs/QA/Evidence/REFACTOR-M1/**` |
| 允许读取 | 本工单 + 被改文件的现状（只读） |
| 绝不触碰 | `Assets/_Project/Art/**`、`Assets/Scenes/**`、`Assets/_Project/Prefabs/**`、`*.asset`（除新增脚本自带 `.meta`）、`ProjectSettings/**`、`Packages/**`、`AGENTS.md`、`Docs/Handoff/CURRENT.md`、其他 `TASK_*`/`REPORT_*`、现有断言口径 |
| 证据目录 | `Docs/QA/Evidence/REFACTOR-M1/`（由套件自动建）；SPEC01 证据进 `Docs/QA/Evidence/SPEC-01/editor_run<N>/` |
| 熔断 | `AGENTS.md` §6 + 计划书 §6；修复轮次 **1 轮**（编译器/测试红 → 修 1 次仍红即停并写现场） |
| 执行预算 | 文件数 ≤ 16；不改无关文件；不跑全量 batch 验收 |

**停止条件（照计划书 §6）**：需要改预制体/场景/`.asset`/`ProjectSettings` 才能继续；需要改 `Docs/01` 不变量或新增 ADR；编译错误连续 2 轮；修复轮用尽仍红；`git status` 对不上。

## §5 学习产出

| 项 | 要求 |
|---|---|
| 讲稿 | 不适用（M4 收口后统一写重构讲稿） |
| 三句话版本 | 写进 REPORT §3 第 7 条 |

## 自包含声明

本工单不依赖 `CURRENT.md` 或其他任务书即可执行；信息不足时在 REPORT 写“信息不足”并列缺项，**不**自行翻阅其他文档补全。
