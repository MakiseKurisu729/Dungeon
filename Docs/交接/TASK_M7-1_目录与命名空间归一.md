# M7-1 — 目录与命名空间归一

> 本工单自包含：执行者只读本文件即可执行，不需要翻 `当前状态.md` 或其他 TASK。
> 附带只读：`Docs/交接/M7_决策结论.md`（D1–D20）、`Docs/QA/Evidence/M7-0/程序集拆分计划.md`。
> 规则入口：`AGENTS.md`（§3 三闸门、§3.2.1 MCP、§4 证据、§5 风险、§6 熔断、§7 提交、§8 长任务）。

## §0 元信息

| 字段 | 值 |
|---|---|
| 任务 ID | `M7-1` |
| 风险等级 | **高风险**（60 个脚本迁移、`.meta`/GUID、场景引用面；**无逻辑改动**） |
| 授权 | 所有者 2026-11-06：① “授权 M7 目录迁移”；② 批准 M7 长任务模式；③ 批准执行者**每步本地自动提交**（不 push）；④ 批准决策 D1–D20 |
| 基线提交 | `483678fb`（`docs/fnd-00-baseline`，工作区干净）。**开工时 `git rev-parse --short HEAD` 复核，不一致即停。** |
| 回滚锚 | `483678fb`；tag `pre-m7` **未创建**，用 hash |
| 分支 | `docs/fnd-00-baseline`（不新建分支、不合并、不 push） |
| 提交方式 | **允许自动提交**：每步本地 `git add` + `git commit`，一步一提交；**不 push** |
| 状态 | `Ready` |

基线核对（2026-11-06 大脑只读核验）：

| 项 | 实测 |
|---|---|
| `Assets/_Project/Scripts/**/*.cs` | 77 个 |
| `Assets/_Project/Tests/**/*.cs` | 30 个 |
| asmdef | 0 个 |
| Unity | `2022.3.62f3c1`，**模式 B** 常开编辑器，实例 `Dungeon@4f5c34c82f31646e`，活动场景 `Assets/Scenes/DungeonWorld.unity`，`isCompiling=false` |
| 已知基线 | `REFACTOR` 16/0、`SPEC01` 12/0；`DIAG` 6/1（已知红 `Diag.Input.FirstExit`，**本单不跑、不修**） |
| 审计脚本 | `Docs/QA/Evidence/M7-1/audit_scripts.py`（已存在，`OUT` 已指向 M7-1；**不改脚本**） |
| Python | `py -3.12` = 3.12.10（`python` 是 Store 占位符，不要用） |

> **只有所有者能标 `Done`。** 执行者不得标 `Done`、不得改 `当前状态.md`、不得写 ADR。

## §1 需求（Why / What）

**一句话目标**：把 77 个脚本按目标层归一到 `Assets/_Project/Scripts/{Core,Content,Gameplay,App,UI,Editor}`，每个脚本加与目录一致的 `Dungeon.*` 命名空间；60 个移动文件 `.meta`/GUID 不变；批 3 后建成 `Dungeon.Core` + `Dungeon.Content` 两个 asmdef；**运行行为零变化**。

**用户故事**：作为项目所有者，我要“目录 = 命名空间 = 未来的 asmdef 边界”，以便 M7-2/M7-3 只靠“引用方向”就能拆程序集，而不用再动文件位置。

**必须满足**

| 编号 | 需求 | 验收方式 |
|---|---|---|
| R-1 | 77 个 `.cs` 全部落到 §2.1 的 6 个目标目录；文件名不变（不重命名类型） | `verify_m7_1.py` 断言 `M7-1.Structure.Files` |
| R-2 | 每个脚本恰好 1 条 `namespace`，且等于所在目录的映射（§2.2） | `M7-1.Structure.Namespace` |
| R-3 | 60 个移动文件的 `.cs` 与 `.meta` **成对**移动，GUID 不变；场景/预制体/Animator/`.asset` **零改动** | `M7-1.Structure.Meta` + `git status` |
| R-4 | 每批结束：编译 0 error，`REFACTOR` 16/0，`SPEC01` 12/0 | AC-1…AC-3 |
| R-5 | `MessageFeed.RaiseChanged` 由 `internal` 改 `public`（批 1，D17） | 源码 + `M7-1.NoLogicChange` |
| R-6 | 批 3 后建 `Dungeon.Core.asmdef`（`references: []`）与 `Dungeon.Content.asmdef`（`references: ["Dungeon.Core"]`） | `M7-1.Structure.Asmdef` |
| R-7 | 30 个测试 `.cs` **保持全局命名空间**，只补 `using` | `M7-1.Structure.Tests` |
| R-8 | **无行为改动**：产品代码除 `namespace`/`using`/`RaiseChanged` 可见性外，语句与改动前一致 | `M7-1.NoLogicChange`（`git diff -w`） |
| R-9 | 每步一条中文提交（标题 + `为什么` + `验证`），证据已落盘；**不 push** | AC-9 |

**不在范围**（不得顺手做）

- M7-2 的 11 条上行边（`ProgressionState` / `PauseState` / `IUiModalState` / `TransitionRequest`）——一行都不改。
- M7-3 的 `Gameplay`/`App`/`UI`/`Editor` asmdef；M7-4 全量回归与场景冒烟。
- 删除或改写 `GameData`/`GameFlow`/`PauseManager` 旧适配层（D9 保留）。
- 改场景、预制体、Animator、`.asset`、`.meta` 内容、`ProjectSettings`、`Packages`。
- 改 `Docs/01` §1.4 的 14 条不变量、写 ADR、拆 Gameplay 子程序集、M6/M5/Unity 6.3。
- 批量重命名类型、批量格式化无关文件、`git mv`、`push`、`reset --hard`/`rebase`/`amend`/force push/`git add -A`。

**约束**

1. asmdef 边界 = 目录边界 → 目录必须与目标层严格一致，不留“临时层”。
2. 序列化兼容：ScriptableObject（`UnitStats`/`WeaponData`/`ItemData`）靠 GUID 引用，移动安全；`Cooldown` 是普通 struct 字段，命名空间变更不影响字段名序列化；不得改任何字段名。
3. 字符串引用（`[CreateAssetMenu]` 的 menuName、Animator 参数名、`GameObject.Find("名字")`）一律**不改**。
4. 只允许“插入/包裹”式代码改动：文件内容整体右移 4 空格以匹配 namespace 块；`git diff -w` 必须只剩 namespace/using/可见性差异。

## §2 方案（Plan）

**备选方案**

| 方案 | 做法 | 优点 | 缺点 | 结论 |
|---|---|---|---|---|
| A | 一次性移动 77 个文件 + 一次性加命名空间，最后统一修编译 | 提交少 | 一次编译红面覆盖全仓，出错无法定位；回滚粒度 = 全部 | 弃用 |
| B | **4 批按目标层**：Core+Content → Gameplay → App → UI/Editor；批 3 后建 Core/Content asmdef | 每批独立可编译、可回滚；叶层先固化，上层后续不再反复 | 提交 5+1 次，测试 `using` 要按批补 | **选定**：风险与工期最匹配（决策 D1） |
| C | 只加命名空间不动目录，asmdef 用 `name` 硬指目录 | 迁移量小 | 违反“asmdef 边界 = 目录边界”；M7-3 必然返工 | 弃用 |

**选定方案与理由**：先固化的批次只被上层引用，后做的批次只改自己，编译红面始终限制在一层内；每批一条提交即一条回滚锚。

**数据流**：源码磁盘路径 → Unity 资产库（GUID 不变）→ 编译器按目录内的 asmdef（M7-1 只有 Core/Content 两个）切程序集 → 运行时行为不变（无逻辑改动）。测试仍在预定义程序集（无 asmdef），依靠 `using` 解析类型。

**影响面清单**

| 项 | 内容 |
|---|---|
| 新增文件 | 2 个 asmdef + 各自 `.meta`；`Scripts/Content`、`Scripts/App`、`Scripts/Editor` 三个文件夹 + `.meta`；`Docs/QA/Evidence/M7-1/**`、REPORT、讲稿 |
| 移动文件 | **60 个** `.cs` + 60 个 `.cs.meta`（§2.1 逐一点名） |
| 原地加命名空间 | 17 个（Core 4 + Gameplay/Interaction 3 + UI 10） |
| 修改符号 | 仅：77 个文件加 `namespace` 包裹、引用方加 `using`、`MessageFeed.RaiseChanged` 改 `public` |
| 字符串引用 | **无改动**（menuName / Animator 参数 / Find 名字都不动） |
| 场景 / 预制体引用 | **不编辑**；靠 `AssetDatabase.MoveAsset` 保 GUID；`git status` 不得出现 `.meta` 删除+新增配对 |
| `.meta` | 随 `.cs` 成对移动；空目录与其 `.meta` 一并消失（预期） |

### 2.1 批次与文件映射（**预批改动清单**，清单外一律先停）

路径前缀统一为 `Assets/_Project/Scripts/`（表中省略）。

#### 批 1｜Core + Content（10 个文件，其中移动 6 个）

| # | 现路径 | 新路径 | 命名空间 | 动作 |
|---|---|---|---|---|
| 1 | `Core/EventBus.cs` | 同 | `Dungeon.Core` | 原地加 ns |
| 2 | `Core/UnitId.cs` | 同 | `Dungeon.Core` | 原地加 ns |
| 3 | `Core/MessageFeed.cs` | 同 | `Dungeon.Core` | 原地加 ns + `RaiseChanged` → `public` |
| 4 | `Core/RunState.cs` | 同 | `Dungeon.Core` | 原地加 ns |
| 5 | `Units/Team.cs` | `Core/Team.cs` | `Dungeon.Core` | 移动 + ns |
| 6 | `Combat/ICharacterCombat.cs` | `Core/ICharacterCombat.cs` | `Dungeon.Core` | 移动 + ns |
| 7 | `Framework/Cooldown.cs` | `Core/Cooldown.cs` | `Dungeon.Core` | 移动 + ns |
| 8 | `Units/UnitStats.cs` | `Content/UnitStats.cs` | `Dungeon.Content` | 移动 + ns |
| 9 | `Combat/WeaponData.cs` | `Content/WeaponData.cs` | `Dungeon.Content` | 移动 + ns |
| 10 | `Items/ItemData.cs` | `Content/ItemData.cs` | `Dungeon.Content` | 移动 + ns |

> 批 1 **不移动** `Core/` 里最终属于 App/Gameplay 的 8 个文件（`AppFlow`/`GameData`/`GameFlow`/`MetaState`/`PauseManager`/`RunFlow`/`UnitRegistry`/`WaveSpawner`）：它们**保持全局命名空间**，到批 2/3 移动时再加 ns，避免同一文件改两次命名空间。

#### 批 2｜Gameplay（移动 47 个 + 原地 3 个 = 50 个文件）

| 目标目录 | 命名空间 | 来自 | 文件（逐个点名） |
|---|---|---|---|
| `Gameplay/Units` | `Dungeon.Gameplay.Units` | `Units/`（16） | `BossBrain` `CharacterBrain` `DownedRescue` `MonsterBrain` `ShieldedUnitHealth` `SkeletonWander` `SkillData` `SkillSystem` `SleepingSkeleton` `UnitAnimatorSpeed` `UnitBrain` `UnitHealth` `UnitMana` `UnitMotion` `Visibility` `WorldHealthBar` |
| `Gameplay/Units` | 同上 | `Core/`（1） | `UnitRegistry` |
| `Gameplay/Combat` | `Dungeon.Gameplay.Combat` | `Combat/`（14） | `CombatFeedback` `DamageInfo` `DamageNumber` `ExplosionFx` `FireballProjectile` `HeavySlashEffect` `MeleeCombat` `Projectile` `RangedCombat` `SimplePool` `SkillCastContext` `SkillEffectBase` `SkillEffectFireball` `SkillEffects` |
| `Gameplay/Player` | `Dungeon.Gameplay.Player` | `Player/`（5） | `PlayerCommandController` `Selectable` `SelectionManager` `SkillIndicator` `SkillInputController` |
| `Gameplay/World` | `Dungeon.Gameplay.World` | `System/`（6） | `Chest` `DungeonDoor` `FogOfWar` `ForestGenerator` `Interactable` `SkeletonSpawner` |
| `Gameplay/World` | 同上 | `Debug/`（2） | `DebugCameraSwitcher` `Mirror` |
| `Gameplay/World` | 同上 | `Core/`（1） | `WaveSpawner` |
| `Gameplay/Items` | `Dungeon.Gameplay.Items` | `Items/`（2） | `ChestItem` `Inventory` |
| `Gameplay/Interaction` | `Dungeon.Gameplay.Interaction` | 原地（3） | `IInteractionTarget` `InteractionDefinition` `InteractionRunner` |

#### 批 3｜App（移动 6 个）

| 现路径 | 新路径 | 命名空间 |
|---|---|---|
| `Core/AppFlow.cs` | `App/AppFlow.cs` | `Dungeon.App` |
| `Core/GameData.cs` | `App/GameData.cs` | `Dungeon.App` |
| `Core/GameFlow.cs` | `App/GameFlow.cs` | `Dungeon.App` |
| `Core/MetaState.cs` | `App/MetaState.cs` | `Dungeon.App` |
| `Core/PauseManager.cs` | `App/PauseManager.cs` | `Dungeon.App` |
| `Core/RunFlow.cs` | `App/RunFlow.cs` | `Dungeon.App` |

> 批 3 完成后 `Scripts/Core/` 恰好剩 7 个纯 Core 文件 → 才能建 asmdef。

#### 批 3 后｜建 Core/Content 程序集（2 个新文件）

| 文件 | 内容要求 |
|---|---|
| `Scripts/Core/Dungeon.Core.asmdef` | `name=Dungeon.Core`、`rootNamespace=Dungeon.Core`、`references=[]`、`autoReferenced=true`、`includePlatforms=[]`、`noEngineReferences=false` |
| `Scripts/Content/Dungeon.Content.asmdef` | `name=Dungeon.Content`、`rootNamespace=Dungeon.Content`、`references=["Dungeon.Core"]`、`autoReferenced=true`、`includePlatforms=[]`、`noEngineReferences=false` |

> 用文本写 `.asmdef`（JSON）后 `refresh_unity`；`.meta` 由 Unity 生成，必须一并提交。**不得**手写 `.meta`。

#### 批 4｜UI + Editor + 测试 using（11 个文件 + 30 个测试）

| 目标目录 | 命名空间 | 文件 |
|---|---|---|
| `UI`（原地 10） | `Dungeon.UI` | `BasePanel` `CursorManager` `EscMenuPanel` `HUD` `InventoryUI` `MainMenuPanel` `SkillBarUI` `SlotClickHandler` `TransitionFader` `UIManager` |
| `Editor`（移动 1） | `Dungeon.Editor` | `Debug/DebugTestTools.cs` → `Editor/DebugTestTools.cs` |
| `Assets/_Project/Tests/**`（30） | **不加 namespace** | 只补 `using`，按编译错误逐个修到 0 error |

> `Scripts/Editor/` 是 Unity 特殊目录，会进 `Assembly-CSharp-Editor`；这正是目标（`#if UNITY_EDITOR` 工具）。移动后仍保留 `#if`/`#endif` 包裹。

### 2.2 目录 → 命名空间映射（唯一口径）

| 目录（相对 `Assets/_Project/Scripts/`） | 命名空间 | 文件数 |
|---|---|---|
| `Core/` | `Dungeon.Core` | 7 |
| `Content/` | `Dungeon.Content` | 3 |
| `Gameplay/Units/` | `Dungeon.Gameplay.Units` | 17 |
| `Gameplay/Combat/` | `Dungeon.Gameplay.Combat` | 14 |
| `Gameplay/Player/` | `Dungeon.Gameplay.Player` | 5 |
| `Gameplay/World/` | `Dungeon.Gameplay.World` | 9 |
| `Gameplay/Items/` | `Dungeon.Gameplay.Items` | 2 |
| `Gameplay/Interaction/` | `Dungeon.Gameplay.Interaction` | 3 |
| `App/` | `Dungeon.App` | 6 |
| `UI/` | `Dungeon.UI` | 10 |
| `Editor/` | `Dungeon.Editor` | 1 |
| 合计 | — | **77** |

### 2.3 实施顺序

1. **前置自检**：`git rev-parse --short HEAD` = `483678fb`；`git status --porcelain` 为空；`editor_state` 可读且 `is_compiling=false`；活动场景非脏（脏→停，见 §4 熔断）。
2. **批 1** → 编译 0 error → `verify_m7_1.py` → `REFACTOR`/`SPEC01` → 证据 → 提交。
3. **批 2** → 同上 + Play 30 秒冒烟 → 提交。
4. **批 3** → 同上（Core 目录最终化）→ 提交。
5. **程序集**（Core/Content asmdef）→ 编译 0 error → 提交。
6. **批 4**（UI/Editor + 测试 using）→ 编译 0 error → `verify` 全 PASS → `REFACTOR`/`SPEC01` **连续各 2 次** → Play 冒烟 → 提交。
7. **收口**：REPORT + 讲稿 + 醒来目检清单 + 最终 `git log` → 提交（`文档：`）。

**每批固定动作**（顺序不要变）：

1. 建目标文件夹（`manage_asset action=create_folder`，或移动前由 Unity 自动创建）。
2. **先试 1 个文件**：`manage_asset(action="move", path="Assets/_Project/Scripts/...", destination="Assets/_Project/Scripts/...")`，用 `git status` 确认源 `.cs`+`.meta` 消失、目标 `.cs`+`.meta` 出现、`git status` 无 `D`+`??` 配对；**确认后才批量**。
3. 批量移动本批全部文件。
4. 加命名空间（把整段内容包进 `namespace X { ... }` 并整体右移 4 空格；同时补 `using`）。
5. 修引用方 `using`：从 `read_console` 的 `error CS0246`/`CS0234` 逐条修；测试文件同步补。**禁止**为了让编译过而改逻辑、改类型名、改字段名。
6. `refresh_unity` → 轮询 `editor_state` 到 `is_compiling=false` 且 `is_domain_reload_pending=false` → `read_console` 过滤 error。
7. 跑 `verify_m7_1.py`（断言见 §3）。
8. 跑 `REFACTOR`、`SPEC01` 菜单，记录 `Summary` 的 pass/fail + `InEditorResult.txt` 的 code。
9. 写本批证据到 `Docs/QA/Evidence/M7-1/批N/`。
10. `git status` 只含本批声明文件 → `git add -- Assets/_Project/Scripts` + 显式 Docs 路径 → `git commit` → `git show --stat` 落盘。

**移动方式**（D16）：优先 `manage_asset action=move`；失败/停滞 → 文件系统成对移动 `.cs` + `.cs.meta` 后 `refresh_unity`。**禁止 `git mv`**。任何情况下不得让 `.meta` 与 `.cs` 分开。

### 2.4 风险与回滚

| 风险 | 信号 | 检测 | 回滚 |
|---|---|---|---|
| GUID 断 / `.meta` 丢 | 场景 Missing Script、`git status` 出现 `.meta` 删除+新增配对 | `M7-1.Structure.Meta` + Play 冒烟 | `git revert` 本批提交，重做本批 |
| 命名空间漏加/错层 | 编译 CS0246、`verify` namespace 断言红 | `M7-1.Structure.Namespace` | 修到绿；≥3 轮仍红 → revert |
| 顺手改逻辑 | `git diff -w` 出现非 namespace/using 行 | `M7-1.NoLogicChange` | revert 本批，重新只做搬迁 |
| Core 不纯（引用 Gameplay） | `Dungeon.Core` 编译红 | 程序集步骤编译 | 删除 asmdef 回到上一提交，报告 |
| 测试没跟上 | `error CS` 出现在 `Tests/**` | 编译 | 补 `using`，不改断言 |
| 暂停/升级等行为变化 | 套件红 | `REFACTOR`/`SPEC01` | 本批不允许行为变化 → revert 并报告 |

## §3 验证（Validation）

### 3.1 AC 表

| AC | Given / When / Then | 断言 ID | 证据 |
|---|---|---|---|
| AC-1 | 给定任一批次改完并刷新，当 Unity 编译结束，则 `is_compiling=false`、`is_domain_reload_pending=false`，Console 无 `error CS` | `M7-1.Compile` | `Docs/QA/Evidence/M7-1/批N/compile.md`（含 read_console 原文摘录与条数） |
| AC-2 | 给定批次完成，当跑 `Tools/QA/In-Editor/REFACTOR`，则 `pass=16 fail=0` 且 `InEditorResult.txt` 为 `code=0` | `M7-1.REFACTOR` | `批N/suites.md` + `Docs/QA/Evidence/REFACTOR-*/editor_runN/*Summary*.json` |
| AC-3 | 同上，`SPEC01` 则 `pass=12 fail=0`、`code=0` | `M7-1.SPEC01` | 同上（`SPEC-01/editor_runN/`） |
| AC-4 | 给定全部完成，当跑 `verify_m7_1.py`，则 77 文件、目录计数 `7/3/17/14/5/9/2/3/6/10/1`、每个脚本的 `namespace` 等于 §2.2 映射、测试 30 文件且无 `namespace` | `M7-1.Structure.Files` / `.Namespace` / `.Tests` | `批4/verify.txt`（ExitCode=0） |
| AC-5 | 给定全部完成，当查 `.cs`↔`.meta` 与 `git diff --summary`，则 60 个 `.cs` 全为 `rename`，脚本目录内无孤儿 `.cs.meta`，无 `.meta` 删除+新增配对 | `M7-1.Structure.Meta` | `批4/meta-check.txt` + `git show --summary --find-renames` |
| AC-6 | 给定每批完成，当重跑 `audit_scripts.py`，则 `layer_file_counts` 逐批收敛到 §2.2，且 `len(upward_layer_edges)` **恒等于 11（不增）** | `M7-1.Audit` | `批N/audit/stats.json` + `coverage.txt` |
| AC-7 | 给定程序集步骤完成，当读两个 asmdef 并编译，则 `Dungeon.Core.references=[]`、`Dungeon.Content.references=["Dungeon.Core"]`，Core 7 文件不引用项目内其他类型 | `M7-1.Structure.Asmdef` | `程序集/asmdef.md` + `verify.txt` |
| AC-8 | 给定全部完成，当跑 `git diff -w 483678fb..HEAD -- Assets/_Project/Scripts`，则增删行只匹配 `namespace`、`using`、`public static void RaiseChanged`、大括号；**无其他语句变化** | `M7-1.NoLogicChange` | `批4/verify.txt` 的 `diff-scan` 段 |
| AC-9 | 给定全部完成，当 `git log --oneline 483678fb..HEAD`，则正好 6 条中文提交（5 步 + 1 收口），标题含 `M7-1`、正文含 `为什么：`/`验证：`；`git status` 干净 | `M7-1.Commits` | `收口/git-log.txt` + `收口/git-show-stat.txt` |
| AC-10 | 给定批 2 与批 4 完成，当进 Play 30 秒再退出，则 Console 无 `Missing script`、无 `NullReferenceException`、无 `error CS` | `M7-1.PlaySmoke` | `批N/play-smoke.md`（时间、场景、error 计数） |
| AC-11 | 给定批 1 完成，当读源码，则 `MessageFeed.RaiseChanged` 为 `public static`，签名其余不变 | `M7-1.RaiseChanged` | `批1/compile.md` 附 grep 行 + `M7-1.NoLogicChange` |

**运行次数**：批 1/2/3、程序集：`REFACTOR` 与 `SPEC01` 各 **1 次**；批 4（收口批）：各 **连续 2 次**，两次 `pass/fail` 一致才算过（时序/间歇口径 N≥3 不适用：本单无时序改动，按常规 ≥2 次）。

**自动化优先**：结构类断言全部由 `Docs/QA/Evidence/M7-1/verify_m7_1.py`（执行者新增）覆盖，**必须可重复运行**，参数 `--base 483678fb`，输出 `PASS/FAIL <断言ID> <数据>`，有 FAIL 则 ExitCode=1。人工目检 ≤3 项，放在 REPORT §7。

`verify_m7_1.py` 必须实现：

| 断言 ID | 内容 |
|---|---|
| `M7-1.Structure.Files` | `Scripts/**/*.cs` = 77；各目录计数与 §2.2 一致 |
| `M7-1.Structure.Namespace` | 每个 `.cs` 恰 1 条 `^\s*namespace\s+(\S+)\s*\{\s*$`，值 = 目录映射；多/少/错都 FAIL |
| `M7-1.Structure.Meta` | 每个 `.cs` 有同名 `.cs.meta`；`Scripts` 下无不存在对应文件的 `.cs.meta` |
| `M7-1.Structure.Tests` | `Tests/**/*.cs` = 30；无 `namespace` 行 |
| `M7-1.Structure.Asmdef` | 两个 asmdef 存在且 `name`/`references`/`rootNamespace`/`autoReferenced` 正确 |
| `M7-1.Audit` | `Docs/QA/Evidence/M7-1/stats.json` 的 `upward_layer_edges` 长度 = 11，`layer_file_counts` = §2.2 |
| `M7-1.NoLogicChange` | 解析 `git diff -w --unified=0 <base>..HEAD -- Assets/_Project/Scripts`，每个 `+`/`-` 行（去空白）必须匹配白名单：`namespace …{`、`using …;`、`}`、`#if UNITY_EDITOR`/`#endif`（DebugTestTools 包裹不变，理论上不出现）、`public static void RaiseChanged()`/`internal static void RaiseChanged()` |

**必须保存的证据**（每批一份，只追加不覆盖）：`Summary.md`、`verify.txt`、`ExitCode.txt`、`compile.md`、`suites.md`、`audit/`（脚本输出副本）、`git_status.txt`、`git_show_stat.txt`。收口另加 `git-log.txt`。

### 3.2 目检（≤3，留给所有者）

1. Unity 里 `Scripts` 目录结构与 §2.2 一致，拖动/双击脚本无 Missing。
2. 主场景 `DungeonWorld` 进 Play 30 秒：玩家能走、敌人能追、HUD 有数字。
3. Project 窗口选中任一脚本，Inspector 顶部显示 `Dungeon.*` 命名空间对应的程序集（Core/Content）或 `Assembly-CSharp`。

## §4 执行信息

| 项 | 内容 |
|---|---|
| 前置 | HEAD=`483678fb`、工作区干净；**模式 B**：编辑器保持打开，实例 `Dungeon@4f5c34c82f31646e`（多实例先 `set_active_instance`）；活动场景非脏；`py -3.12` 可用 |
| 允许改动 | §2.1 列出的 77 个 `.cs` + 60 个 `.cs.meta`；`Scripts/Content`/`App`/`Editor` 文件夹及 `.meta`；2 个 `.asmdef` + `.meta`；`Docs/QA/Evidence/M7-1/**`；`Docs/交接/REPORT_M7-1_目录与命名空间归一.md`；`Docs/学习/M7-1_讲稿.md` |
| 允许读取 | 本工单、`Docs/交接/M7_决策结论.md`、`Docs/QA/Evidence/M7-0/程序集拆分计划.md`、`Docs/QA/Evidence/M7-1/audit_scripts.py` |
| 绝不触碰 | 场景/预制体/Animator/`.asset`/`ProjectSettings`/`Packages`/`.meta` 内容；`Assets/_Project/Tests/**` **目录结构**（只许改 `.cs` 里的 `using`）；`Docs/01`；`当前状态.md`；其他 `TASK_*`/`REPORT_*`；`Docs/归档/**`；M7-0 证据 |
| 证据目录 | `Docs/QA/Evidence/M7-1/批1…批4/`、`Docs/QA/Evidence/M7-1/程序集/`、`Docs/QA/Evidence/M7-1/收口/`（重跑写 `run2/` 子目录，不覆盖） |
| 提交策略 | **允许自动提交**：一步一提交，共 6 条；不 push；不改写历史 |
| 熔断 | 见下 |
| 执行预算 | 5 个工作步；单步重试 ≤3 轮；总时长 ≤ 8 人时；文件上限 = 本工单清单 |

**MCP 授权（模式 B，缺一即越界）**

| 类别 | 允许 | 说明 |
|---|---|---|
| 只读 | `mcpforunity://editor/state`、`instances`、`project/info`、`read_console`、`manage_editor` 查询、`find_gameobjects`、`manage_asset(action="get_info"/"search")` | 随时可用 |
| 资产移动 | `manage_asset(action="move")`、`action="create_folder"` | **仅限** §2.1 的 60 个 `.cs` 与 3 个新文件夹；本工单即影响面清单，所有者 2026-11-06 已授权目录迁移 |
| 编辑唤醒 | `refresh_unity` | 只触发刷新，不改资产 |
| 套件 | `execute_menu_item` → `Tools/QA/In-Editor/REFACTOR`、`Tools/QA/In-Editor/SPEC01` | 禁止 `run_tests` |
| Play 冒烟 | `manage_editor` 的 play/stop | 仅 30 秒冒烟；**不得保存场景、不得改对象** |
| **禁止** | `execute_code`、`create_script`、`manage_script`、`apply_text_edits`、`script_apply_edits`、`manage_scene` 写、`manage_gameobject`/`manage_components` 写、`manage_prefabs`、`manage_animation` 写、`manage_scriptable_object`、`manage_material/texture/shader` 写、`manage_build` | 文本改动用执行者自己的文件编辑工具 |

**熔断（触发即停 + 现场报告）**

1. 单步重试 ≥3 次仍红；编译错误连续 ≥2 轮。
2. `.meta` 出现删除+新增配对、场景 Missing、GUID 断裂。
3. 出现 §2.1 清单外的文件（含资产）需要改动。
4. 活动场景为脏（`GateForLaunch` 拒跑 `dirtyScene`）——**不得保存、不得切场景**，停下报告。
5. MCP 连续 3 次失败；`is_compiling` 单次超 10 分钟且 `refresh_unity`/前台唤醒无效。
6. 测试连续 3 次红；`git status` 出现未声明文件；发现第二个 AI 写同一 Unity 实例。
7. 任何 push/reset/rebase/amend/提交未声明文件的冲动 → 停。

**提交信息模板（照抄，替换 N/数字）**

```text
重构：M7-1 批1 Core+Content 目录与命名空间归一（M7-1/批1）
为什么：先收口最底层，后续批次只改上层，避免跨层引用反复变动。
验证：编译 0 error；REFACTOR 16/0；SPEC01 12/0；audit upward=11；verify 全 PASS；证据 Docs/QA/Evidence/M7-1/批1/
```

批 2/批 3/批 4/程序集同格式；收口提交用 `文档：M7-1 交付报告与讲稿（M7-1/收口）`。

## §5 学习产出

| 项 | 要求 |
|---|---|
| 讲稿 | **必须**：`Docs/学习/M7-1_讲稿.md`（高风险 + feature 级结构改动）。按 `Docs/学习/讲稿模板.md`，含一句话、原问题、方案对比、数据流、3–5 处 `文件:行`、设计取舍、边界/失败模式、答辩 Q&A 5 条 |
| 三句话版本 | 写进 REPORT §3.7，并附在交付回复里 |

## 自包含声明

本工单不依赖 `当前状态.md` 或其他任务书即可执行。信息不足时在 REPORT 写“信息不足”并列缺项，**不**自行翻阅别的文档补全。
