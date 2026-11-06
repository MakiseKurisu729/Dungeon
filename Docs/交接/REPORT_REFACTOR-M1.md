# REPORT — REFACTOR-M1：契约层 + 单位注册（消除全场扫描）

> 对应 `Docs/Handoff/TASK_REFACTOR-M1.md`（长任务 `REFACTOR-LONG-01` 的 M1 小节）。
> 节次与 `Docs/Handoff/workflow-rules.md` §2 一致，未删节；不适用的写“不适用”+ 理由。
> 长任务模式：执行者不标 `Done`、不改 `CURRENT.md`、未做任何 git 写操作。

## 1. 结果摘要

`完成` —— 新增楼层级 `UnitRegistry`（+ `UnitId`），`UnitHealth` 自注册/注销，替换 `Scripts/**` 下**实测 17 个** `FindObjects*` 调用点（调用点计数 = **0**）；REFACTOR 套件新增第 9 条 `Refactor.RegistrySemantics` 后 **9/9 ×4 连续全绿**（exit 0），SPEC01 **12/12 ×2**（exit 0）；未改动任何场景/预制体/Animator/`.asset`/`ProjectSettings`，未修改任何既有断言口径。

## 2. AC 回填表

| AC | 结果 | 断言 ID | 关键数据 | 日志路径 |
|---|---|---|---|---|
| AC-1 `Scripts/**` 五类 `FindObjects*` 调用点 = 0 | **通过** | `Refactor.NoGlobalScan`（grep 证据，非运行期断言） | 基线 17 行（12 个文件）→ 当前原始命中 4 行且**全部为 `///` 文档注释**；过滤行内 `//` 后调用点 = **0** | `Docs/QA/Evidence/REFACTOR-M1/grep-findobjects.txt` |
| AC-2 REFACTOR ×3，每次 8/8→**9/9**、fail=0、infra=0、exit=0 | **通过** | 套件全部 **9** 条（原 8 条 + 新增 `Refactor.RegistrySemantics`） | run1 pass=9/fail=0/blocked=0/infra=0/exit=0；run2 同；run3 同；run4（EOL 归一化后复跑）同 | `Docs/QA/Evidence/REFACTOR-M1/editor_run1..3/`（run4 为附加复跑） |
| AC-3 SPEC01 ×2，每次 12/12、fail=0、exit=0 | **通过** | SPEC01 全 12 条 | run24 pass=12/fail=0/blocked=0/infra=0/exit=0；run25 同 | `Docs/QA/Evidence/SPEC-01/editor_run24/`、`editor_run25/` |
| AC-4 契约语义：`All(false)` 不含 `SetActive(false)` 单位、`All(true)` 含；`OfTeam`/`Nearest` 与替换前扫描一致 | **通过** | `Refactor.RegistrySemantics` | `OfTeam(Player,false).Count=3`；停用 `Knight_Unit` 后 `All(false)` 3→2、`All(true)` 保持 4、`Count` 保持 4，恢复后回到 3 且 `Find(id)==probe`；`Nearest(knight.pos,10,Monster)` 与「遍历 `All(false)` + 3D 距离最近」同结果（`REFACTOR_Registry_Probe`，距离 2.0，候选 1） | `REFACTOR-M1/editor_run1..4/REFACTOR_Assertions.jsonl`（末行） |
| AC-5 `git status` 只含本单声明文件 | **通过** | — | 见 §4“改动文件清单”；另有开工前既有脏项 3 项 + 派单文件本身（非本里程碑改动，见 §7） | `Docs/QA/Evidence/REFACTOR-M1/git-diff-stat.txt` |

- **运行次数**：REFACTOR 连续 **4** 次（≥N=3 要求；每次 9/9，逐次一致）；SPEC01 **2** 次（每次 12/12）。
  - run1–run3 为正式验收；run4 是在**仅换行符归一化（LF→CRLF，语义零变化）**后补跑的一次，用于证明交付字节就是被测字节。
- **未运行项**：DIAG 套件**未运行**——本次派单的 L1 只指定 `REFACTOR ×3` 与 `SPEC01 ×2`；本里程碑未新增/未修改任何 DIAG 断言，已知红 `Diag.Input.FirstExit` 不受影响（见 §7）。
- 修复轮次使用：**0 轮**（首跑即全绿，无红）。

### 逐次实测（`pass` 计数，来自各 run 的 Summary）

| run | 套件 | pass | fail | blocked | infraFail | exit |
|---|---|---|---|---|---|---|
| editor_run1 | REFACTOR | 9 | 0 | 0 | 0 | 0 |
| editor_run2 | REFACTOR | 9 | 0 | 0 | 0 | 0 |
| editor_run3 | REFACTOR | 9 | 0 | 0 | 0 | 0 |
| editor_run4（EOL 后复跑） | REFACTOR | 9 | 0 | 0 | 0 | 0 |
| editor_run24 | SPEC01 | 12 | 0 | 0 | 0 | 0 |
| editor_run25 | SPEC01 | 12 | 0 | 0 | 0 | 0 |

## 3. 实现原理（面向所有者）

1. **目标**：把「场上谁算单位」从 17 处各自调用的全场扫描，收敛为**单一注册源**（`Docs/01` §1.4 不变量 8）。重构后新增职业/敌人不需要再写扫描；查询语义（是否含未激活、距离口径）由契约统一表达。

2. **备选方案与为什么这样选**：
   - A（保留扫描 + 每 0.5s 缓存）：改动最小，但仍是全场扫描，违反不变量 8，且引入新的查询延迟。
   - C（每个单位挂 `UnitId` MonoBehaviour）：身份更“正规”，但必须改预制体（本里程碑禁止），且域重载/重实例化后 id 维护复杂。
   - **B（选定）**：静态 `UnitRegistry` + `UnitHealth` 自注册/自注销，**零资产改动、零序列化影响**；身份是运行时句柄（`UnitId`），稳定 GUID 留给后续网络里程碑。

3. **实际数据流**：
   `UnitHealth.Awake`（team 赋值之后）→ `UnitRegistry.Register` →
   `OnEnable/OnDisable` → `SetActiveState`（只维护“激活”标志，**不**注销）→
   调用方 `All/OfTeam/InRadius/Nearest/Find` → 返回与替换前同口径的集合 →
   `OnDestroy` → `Unregister`。
   与 TASK §2 的一致性：契约方法名/签名照单实现；差异只有一处（见第 6 条）。

4. **关键代码位置（5 条）**：
   - `Assets/_Project/Scripts/Core/UnitRegistry.cs:112` —— `Register/Unregister/SetActiveState`：注册顺序即枚举顺序（稳定），激活标志单独维护，`_byId` 支持 `Find(id)`。
   - `Assets/_Project/Scripts/Core/UnitRegistry.cs:247` —— `EnsureViews()`：懒重建 6 张只读视图（全量/激活 × 全队/Player/Monster），查询零分配；`unit == null` 的瞬时残留被跳过。
   - `Assets/_Project/Scripts/Core/UnitRegistry.cs:208` —— `Nearest(center,maxRange,team,filter)`：**3D 距离**、严格 `d < bestD`（与 `UnitBrain/BossBrain/SkeletonWander` 原 `d < bestD` 口径一致），**不隐式跳过 dead**，由调用方传 `h => !h.dead`。
   - `Assets/_Project/Scripts/Units/UnitHealth.cs:41` —— `Awake` 末尾注册（保证 `team` 已由 `stats` 赋值）；`:44-49` `OnEnable/OnDisable/OnDestroy`（`OnDisable` 用 `gameObject.activeInHierarchy` 区分“对象停用”和“组件停用”，因此 `SetActive(false→true)` 不丢身份）。
   - `Assets/_Project/Tests/REFACTOR/Runtime/REFACTOR_ProbeRunner.cs:811` —— 新增第 9 条断言 `Refactor.RegistrySemantics`（`:171` 接入执行序列）：先 `OfTeam(Player)=3`，再临时停用骑士验证未激活语义，最后造一只运行期内存怪物夹具，比对 `Nearest` 与 `All(false)` 手算扫描。

5. **边界 / 失败模式**：
   - **未激活且从未激活过**的单位不会注册——Unity 不跑 `Awake`。当前仓库无此用例（§7 记录为已知限制）。已激活后停用的单位仍保留登记，只是不进 `All(false)`。
   - `unit.team` 若在运行时改动，视图要等下一次注册/激活变更（`_dirty`）才刷新；当前无任何代码改 `team`（`UnitHealth.team` 只在 `Awake` 由 `stats` 赋值）。
   - 场景卸载：`sceneUnloaded` 只剔除**已销毁**项，不清整表——单场景重载时新场景 `Awake` 可能早于旧场景卸载，整表 `Clear()` 会误伤新单位（`GameFlow.R` 重开路径）。
   - 返回的列表是**注册表内部视图**（只读、就地刷新），调用方不得长期持有并修改、不要向下转型成 `List`。

6. **与计划的差异及原因**：
   - **场景卸载清理用“剔除已销毁项”**而不是直接 `Clear()`（TASK §2 允许“或等价”）：避免单场景重载误清新场景单位（上面第 5 条）。
   - **`SelectionManager` 启动收集**用 `All()`（默认排除未激活）+ `GetComponentInChildren<Selectable>()`，而不是表中建议的 `All(true)`：原 `FindObjectsByType<Selectable>` 默认**排除未激活**，用 `All(true)` 会把未激活的 Selectable 也收进来，属语义放宽。选择与替换前严格同口径。
   - **`DebugCameraSwitcher` 取实例加了惰性回退**：`SelectionManager.Awake` 与 `DebugCameraSwitcher.Awake` 的执行顺序不保证；若只写 `_cam = DebugCameraSwitcher.instance`，另一种顺序下 `_cam` 会永久为 null（原 `FindObjectOfType` 不受顺序影响）。因此在 `Update` 侧用 `Cam` 属性惰性重取一次。
   - **`InRadius` 采用“先清空再填充”**：契约未规定，按“查询进缓冲区”的常见语义实现并在 XML 注释写明（当前无调用方，不影响行为）。
   - 数量差异（16 / 17 / “11 个文件”）见 §4“调用点数量差异说明”。

7. **三句话版本**（答辩先讲这三句）：
   - 以前“场上有哪些单位”要靠 17 处各自调 `FindObjectsByType` 全场扫描，每加一种查询就多一次扫描，且“含不含未激活”全凭调用点自觉。
   - 现在单位在 `UnitHealth.Awake` 向 `UnitRegistry` 登记，`OnDestroy` 注销，`OnEnable/OnDisable` 只维护激活标志；查询统一走 `All/OfTeam/Nearest`，默认排除未激活、距离用 3D，与替换前逐条一致。
   - 行为是否没变不靠肉眼：REFACTOR 套件 9 条（含新增的注册表语义断言）连续 4 次全绿，SPEC01 12/12 两次，`Scripts/**` 的 `FindObjects*` 调用点计数归零。

## 4. 受影响模块导航

| 模块 | 职责 | 入口（类 / 方法 / 场景对象） |
|---|---|---|
| 单位契约层 | 单位身份句柄 | `UnitId`（`Scripts/Core/UnitId.cs`） |
| 单位注册表 | 登记/注销/激活标志 + 队伍/范围/最近查询 | `UnitRegistry`（`Scripts/Core/UnitRegistry.cs`），静态、楼层级 |
| 单位生命周期宿主 | 注册与注销的唯一触发点 | `UnitHealth.Awake/OnEnable/OnDisable/OnDestroy` |
| 查询消费方（11 个生产文件） | 索敌、迷雾、可见性、血条、背包绑定、传送、团灭、调试工具、选择收集 | `UnitBrain/BossBrain/SkeletonWander/FogOfWar/Visibility/WorldHealthBar/InventoryUI/DungeonDoor/GameFlow/SkillInputController/DebugTestTools/SelectionManager` |
| 单例替代 | 相机与调试宿主不再靠 `FindObject*` | `DebugCameraSwitcher.instance`、`DebugTestToolsHost.instance` |
| 行为护栏 | 8 条原有 + 1 条新增注册表语义断言 | `REFACTOR_ProbeRunner`（`Tests/REFACTOR/Runtime`） |

数据流：`UnitHealth.Awake → UnitRegistry.Register →（OnEnable/OnDisable 维护激活）→ 调用方 All/OfTeam/InRadius/Nearest/Find → UnitHealth.OnDestroy → UnitRegistry.Unregister`

### 影响面清单（§3.1 闸门要求）

| 项 | 内容 | 实际落地 |
|---|---|---|
| 新增文件 | `Scripts/Core/UnitId.cs`、`Scripts/Core/UnitRegistry.cs` | 已新增，Unity 自动生成 2 个 `.meta`（`guid: 7b5053ab…` / `002a52aa…`） |
| 修改文件（生产） | 12 个调用点文件 + `Units/UnitHealth.cs` + `Debug/DebugCameraSwitcher.cs`（仅新增 `static instance`） | 共 **14** 个已修改；生产文件合计 **16**（2 新 + 14 改） |
| 修改符号 | `UnitHealth.Awake/OnEnable/OnDisable/OnDestroy`；`DebugCameraSwitcher.instance`（+Awake/OnDestroy）；`DebugTestToolsHost.instance`（+Awake/OnDestroy）；各调用点内部实现 | 与清单一致；**无字段改名、无序列化字段增删** |
| 字符串引用 | 无新增、无改动 | 未动 Animator 参数名 / `SendMessage` / `GameObject.Find` / 层级名 |
| 场景 / 预制体 / Animator / `.asset` | 零改动 | 零改动（`git status` 无相关条目） |
| `.meta` | 仅两个新脚本自带 | 仅 2 个（Unity 生成，未手改） |
| 测试文件 | `Tests/REFACTOR/Editor/REFACTOR_BatchAcceptance.cs`（`DefaultMilestone`） + `Tests/REFACTOR/Runtime/REFACTOR_ProbeRunner.cs`（新增第 9 条） | 共 2 个已修改 |

> 预算说明：TASK §4 “文件数 ≤ 16”。生产文件正好 16；另有 2 个测试文件是 AC-4/`DefaultMilestone` 直接要求，故实际改动 18 个文件（不含证据与 REPORT）。已尽量不再扩散（`Units/DownedRescue.cs` 等未被触碰）。

### 17 个调用点：基线 → 替换后（文件:行号）

| # | 基线（`ed0cdc17`） | 用途 | 替换后实现（当前行号） | 未激活语义 |
|---|---|---|---|---|
| 1 | `Player/SelectionManager.cs:34` | 启动收集 `Selectable` | `SelectionManager.cs:45` `UnitRegistry.All()` + `GetComponentInChildren<Selectable>()`（`:47`） | 排除（同原默认） |
| 2 | `Player/SkillInputController.cs:218` | 敌人缓存刷新 | `SkillInputController.cs:218` `UnitRegistry.OfTeam(Team.Monster)` | 排除 |
| 3 | `System/FogOfWar.cs:83` | 迷雾可见性 | `FogOfWar.cs:83` `UnitRegistry.OfTeam(Team.Player)` | 排除 |
| 4 | `Debug/DebugTestTools.cs:32` | 调试：含未激活单位 | `DebugTestTools.cs:32` `UnitRegistry.All(true)` | **含**（显式） |
| 5 | `Debug/DebugTestTools.cs:92` | 调试：存活单位 | `DebugTestTools.cs:92` `UnitRegistry.All()` | 排除 |
| 6 | `Core/GameFlow.cs:33` | 是否有玩家队伍 | `GameFlow.cs:33` `UnitRegistry.OfTeam(Team.Player).Count > 0` | 排除 |
| 7 | `Core/GameFlow.cs:56` | 团灭判定 | `GameFlow.cs:52` `UnitRegistry.OfTeam(Team.Player)` | 排除 |
| 8 | `System/DungeonDoor.cs:33` | 传送全部玩家 | `DungeonDoor.cs:33` `UnitRegistry.OfTeam(Team.Player)` | 排除 |
| 9 | `Units/BossBrain.cs:176` | 索敌/统计 | `BossBrain.cs:176` `UnitRegistry.Nearest(pos, aggroRange, Team.Player, !dead)` | 排除 |
| 10 | `Units/SkeletonWander.cs:62` | 游荡索敌 | `SkeletonWander.cs:61` `UnitRegistry.Nearest(pos, aggroRange, Team.Player, !dead)` | 排除 |
| 11 | `Units/UnitBrain.cs:75` | `FindNearestEnemy` | `UnitBrain.cs:75` `UnitRegistry.Nearest(pos, range, 敌方队伍, !dead)` | 排除 |
| 12 | `UI/InventoryUI.cs:32` | 背包绑定玩家 | `InventoryUI.cs:32` `UnitRegistry.OfTeam(Team.Player)` | 排除 |
| 13 | `Units/Visibility.cs:41` | 可见性 | `Visibility.cs:41` `UnitRegistry.OfTeam(Team.Player)` | 排除 |
| 14 | `Units/WorldHealthBar.cs:55` | 血条目标缓存 | `WorldHealthBar.cs:56` `UnitRegistry.All()`（`:52` 保留 0.25s 刷新节奏；字段类型 `UnitHealth[]`→`IReadOnlyList<UnitHealth>`） | 排除 |
| 15 | `Player/SelectionManager.cs:29` | 相机单例查找 | `SelectionManager.cs:39` `DebugCameraSwitcher.instance`（`:31` 惰性回退） | — |
| 16 | `System/DungeonDoor.cs:43` | 相机单例查找 | `DungeonDoor.cs:43` `DebugCameraSwitcher.instance` | — |
| 17 | `Debug/DebugTestTools.cs:167` | 调试宿主判重 | `DebugTestTools.cs:172` `DebugTestToolsHost.instance` | — |

### 调用点数量差异说明（口径分歧留痕）

- **实测基线 = 17 处 / 12 个文件**（`git grep` 于 `ed0cdc17`，见 `grep-findobjects.txt` A 段）；TASK §2 表同样列 17 行，但表注写“共 11 个文件”——实际是 **12** 个（`SelectionManager`、`DebugTestTools`、`DungeonDoor`、`GameFlow` 各 2 处）。
- 计划书 §9-M1 与派单补充写的“16 处”与实测差 1。差异来源是**上游计数口径**（未逐行核对），不是本单拆/并调用：本单**没有**新增、拆分或合并任何调用点，17 处逐行对号替换，替换后计数 0。
- 结论以 `grep-findobjects.txt` 的 A/C 两段为准（可复跑）。

### 计划外调用点（不在本次 17 条清单内，**未处理**）

| 文件 | 处数 | 用途 | 为何不处理 |
|---|---|---|---|
| `Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs` | 13 | 测试探针自建/查找夹具单位 | `Tests/**` 不在 AC-1 范围（AC-1 明确 `Scripts/**`）；且测试必须能观测**原始场景状态**，若改走注册表，注册表自身出问题时测试会跟着一起错，护栏失效 |
| `Tests/FND00B/Runtime/FND00B05_ProbeRunner.cs` | 13 | 同上 | 同上 |
| `Tests/FND00B/Runtime/FND00B04_ProbeRunner.cs` | 11 | 同上 | 同上 |
| `Tests/FND02A/Runtime/FND02A_ProbeRunner.cs` | 10 | 同上 | 同上 |
| `Tests/DIAG/Runtime/DIAG_ProbeRunner.cs` | 4 | 同上 | 同上 |
| `Tests/FND00B/Runtime/FND00B06_ProbeRunner.cs` | 3 | 同上 | 同上 |
| `Tests/REFACTOR/Runtime/REFACTOR_ProbeRunner.cs` | 2（`:915` `SuppressMonsters`、`:1018` `FindPlayer`） | 停用怪物夹具 / 定位三职业 | 同上（本单只在该文件**新增**第 9 条断言，未改其探针的查找方式） |
| `Tests/FND02A/Editor/FND02A_BatchAcceptance.cs` | 1 | 编辑器入口兜底查找 | 同上 |
| 合计 | **57** | — | 全部在 `Assets/_Project/Tests/**`，属后续里程碑（如需）统一处理 |

### 改动文件清单（与 TASK §4 允许改动逐项比对）

| 文件 | 改了什么 | 是否在允许范围 |
|---|---|---|
| `Assets/_Project/Scripts/Core/UnitId.cs` | 新增：`UnitId` 只读结构（`Value`/`IsValid`/`None`/`Equals`/`==`/`ToString`） | ✅ 新增允许 |
| `Assets/_Project/Scripts/Core/UnitRegistry.cs` | 新增：登记/注销/激活、6 张懒重建视图、`All/OfTeam/InRadius/Nearest/Find/TryGetId/Clear`、`RuntimeInitializeOnLoadMethod` + `sceneUnloaded` 清理 | ✅ 新增允许 |
| `.../Core/UnitId.cs.meta`、`UnitRegistry.cs.meta` | Unity 自动生成 | ✅ 新增脚本自带 |
| `Assets/_Project/Scripts/Units/UnitHealth.cs` | `Awake` 末尾注册；新增 `OnEnable/OnDisable/OnDestroy`（+10 行，无字段改动） | ✅ |
| `Assets/_Project/Scripts/Core/GameFlow.cs` | 2 处扫描 → `OfTeam(Player)` | ✅ |
| `Assets/_Project/Scripts/Player/SelectionManager.cs` | 相机单例 + `Selectable` 收集改造；新增 `Cam` 惰性属性 | ✅ |
| `Assets/_Project/Scripts/Player/SkillInputController.cs` | 敌人缓存 → `OfTeam(Monster)` | ✅ |
| `Assets/_Project/Scripts/System/DungeonDoor.cs` | 传送 → `OfTeam(Player)`；相机 → `instance` | ✅ |
| `Assets/_Project/Scripts/System/FogOfWar.cs` | 迷雾 → `OfTeam(Player)` | ✅ |
| `Assets/_Project/Scripts/UI/InventoryUI.cs` | 背包绑定 → `OfTeam(Player)` | ✅ |
| `Assets/_Project/Scripts/Units/BossBrain.cs` | 索敌 → `Nearest(...)` | ✅ |
| `Assets/_Project/Scripts/Units/SkeletonWander.cs` | 游荡索敌 → `Nearest(...)` | ✅ |
| `Assets/_Project/Scripts/Units/UnitBrain.cs` | `FindNearestEnemy` → `Nearest(...)` | ✅ |
| `Assets/_Project/Scripts/Units/Visibility.cs` | 可见性 → `OfTeam(Player)` | ✅ |
| `Assets/_Project/Scripts/Units/WorldHealthBar.cs` | 血条缓存 → `All()`（字段类型改 `IReadOnlyList`） | ✅ |
| `Assets/_Project/Scripts/Debug/DebugTestTools.cs` | `All(true)`/`All()`/`DebugTestToolsHost.instance`；宿主加 `static instance` | ✅（TASK §2 明列 `DebugTestToolsHost` 新增 `static instance`） |
| `Assets/_Project/Scripts/Debug/DebugCameraSwitcher.cs` | 仅新增 `static instance` + `Awake`/`OnDestroy`（+13 行，无其他改动） | ✅（TASK §2 明列 `DebugCameraSwitcher` 新增 `static instance`） |
| `Assets/_Project/Tests/REFACTOR/Editor/REFACTOR_BatchAcceptance.cs` | `DefaultMilestone` `"M0"`→`"M1"`（+1/-1） | ✅ |
| `Assets/_Project/Tests/REFACTOR/Runtime/REFACTOR_ProbeRunner.cs` | 新增第 9 条断言 + 接入 + 文档注释 8→9 条 + blocked 列表加 1 个 id（+107/-2；**既有 8 条断言零改动**） | ✅ |
| `Docs/Handoff/REPORT_REFACTOR-M1.md` | 本报告 | ✅ |
| `Docs/QA/Evidence/REFACTOR-M1/**` | 证据（4 次 run + grep + git 快照） | ✅ |
| `Docs/QA/Evidence/SPEC-01/editor_run24..25/**` | 本轮 SPEC01 证据 | ✅ |

**未改动**：`Art/**`、`Assets/Scenes/**`、`Prefabs/**`、`*.asset`、`ProjectSettings/**`、`Packages/**`、`AGENTS.md`、`Docs/Handoff/CURRENT.md`、其他 `TASK_*`/`REPORT_*`、既有 8 条 REFACTOR 与 SPEC01 的任何断言口径。

## 5. 证据清单

| 文件 | 内容 |
|---|---|
| `Docs/QA/Evidence/REFACTOR-M1/editor_run1..4/REFACTOR_Assertions.jsonl` | 逐条断言（含 `expected/actual/data`；每次 9 行，末行为 `Refactor.RegistrySemantics`） |
| `Docs/QA/Evidence/REFACTOR-M1/editor_run1..4/REFACTOR_Summary_run1.json` | 每次汇总计数（pass=9 / fail=0 / blocked=0 / infraFail=0 / milestone=M1） |
| `Docs/QA/Evidence/REFACTOR-M1/editor_run1..4/REFACTOR_ExitCode_run1.txt` | 每次退出码（`0`） |
| `Docs/QA/Evidence/REFACTOR-M1/editor_run1..4/InEditorResult.txt` | 编辑器内收尾回执（`code=0`，含 env 清理快照） |
| `Docs/QA/Evidence/REFACTOR-M1/editor_run1..4/REFACTOR_Runs.jsonl` | 跨 run 索引 |
| `Docs/QA/Evidence/REFACTOR-M1/grep-findobjects.txt` | AC-1：基线 17 行（A 段）、当前原始 4 行全为注释（B 段）、过滤后调用点 0（C 段）、替换后 `UnitRegistry` 使用点 18 处（D 段），并写明正则与过滤方式 |
| `Docs/QA/Evidence/REFACTOR-M1/git-diff-stat.txt` | AC-5：`git status --porcelain`、`git diff --stat`、`--numstat`、新建脚本的 `.meta` guid |
| `Docs/QA/Evidence/SPEC-01/editor_run24/`、`editor_run25/` | SPEC01 12/12 证据（Summary/Assertions/ExitCode/InEditorResult/Runs） |

复跑方式：编辑器内菜单 `Tools/QA/In-Editor/REFACTOR`、`Tools/QA/In-Editor/SPEC01 (green)`（模式 B / `unityMCP`）；`grep` 证据由 `grep-findobjects.txt` 内“filter”两行所述规则可完全复现。

## 6. 学习讲稿

不适用（M1 是纯重构里程碑、非 feature、非高风险；TASK §5 规定 M4 收口后统一写重构讲稿）。

## 7. 未决项

- **已知限制（TASK §2 已预告）**：单位若在场景里**初始就是未激活**、且从未激活过，`Awake` 不跑 → 不会注册，`All(true)` 也查不到。当前仓库无此用例；后续若出现“默认隐藏、按需启用”的单位，需要在启用处补 `UnitRegistry.Register` 或改由生成器注册。
- **`team` 运行时变更不会即时反映**：视图仅在注册/注销/激活变更时重建；当前无代码改 `UnitHealth.team`，若未来出现“易主/中立化”需加 `UnitRegistry` 失效入口。
- **DIAG 套件本里程碑未运行**：派单 L1 只要求 REFACTOR×3 + SPEC01×2；已知红 `Diag.Input.FirstExit`（`MISSING_CONTROLLER` vs `NO_PRIMARY`）预期不变，需大脑在批末按计划 §1 第 3 条决定是否补跑。
- **换行符归一化**：`DebugTestTools.cs`、`REFACTOR_BatchAcceptance.cs`、`REFACTOR_ProbeRunner.cs` 与两个新脚本在编辑过程中被写成 LF，已在收尾前统一为 CRLF（`.gitattributes` `* text=auto` + `core.autocrlf=true`），`git diff --numstat` 在归一化前后一致；归一化后已复跑 REFACTOR 一次（run4，9/9）并确认编译零错误。
- **需所有者目检（≤3 项）**：
  1. 在 Unity 里点一次 `Tools/QA/In-Editor/REFACTOR`，确认证据写进 `REFACTOR-M1`（而非 M0）、跑完不退出编辑器；
  2. 手动试一遍：选人（1~4 键、Ctrl+A）、右键移动、点敌人攻击、Q/W、迷雾推进、敌人血条显隐、F5/F9/F10 调试键（`DebugTestTools` 走的是 `All(true)`，停用的骷髅也能被记录生成点）；
  3. 看 GitHub Desktop 的改动列表，确认只包含 §4 清单文件（`Assembly-CSharp*.csproj` 为 Unity 重新生成、`Docs/Handoff/TASK_REFACTOR-M1.md`、`Docs/QA/Evidence/DIAG-SKILL/editor_run3/`、`Docs/QA/Evidence/FND-00B-06/editor_run4/` 是**开工前既有脏项**，非本里程碑改动）。

## 8. 建议提交

```text
重构：M1 契约层 + 单位注册（17 处全场扫描收敛为 UnitRegistry，REFACTOR 9/9×4 + SPEC01 12/12×2）
为什么：把“场上谁算单位”收敛为单一注册源，消除逐帧全场扫描（违反 Docs/01 §1.4 不变量 8），并为后续生命周期拆分与预制体基座提供稳定查询契约；行为由 REFACTOR 新增的注册表语义断言兜底，Scripts/** 的 FindObjects* 调用点归零。
```

## 9. 消耗记录

- 钱包：`Go 订阅`
- 模型完整 ID：`opencode-go/deepseek-v4.1-flash`
