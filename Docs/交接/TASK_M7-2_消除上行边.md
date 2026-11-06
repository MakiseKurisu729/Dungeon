# M7-2 — 消除 11 条上行边

> 本工单自包含：执行者只读本文件 + `Docs/交接/长任务计划_M7-2.md` + `Docs/交接/M7_决策结论.md`（D5–D7、D13、D17–D20）。
> 规则入口：`AGENTS.md`；M7-1 已完成（`Docs/交接/REPORT_M7-1_目录与命名空间归一.md`）。

## §0 元信息

| 字段 | 值 |
|---|---|
| 任务 ID | `M7-2` |
| 风险等级 | **高风险**（改运行时行为：暂停 / 升级 / 模态 / 门过场；伤害与暂停管线） |
| 授权 | 所有者 2026-11-07：“M7-2 之后的 M7 也都不需要我在场”，批准长任务模式 + 每步本地自动提交（不 push） |
| 基线提交 | `0a78111a`（开工时 `git rev-parse --short HEAD` 复核；不一致即停） |
| 回滚锚 | `0a78111a`；每个子项一条 commit，可单独 `git revert` |
| 分支 | `docs/fnd-00-baseline`（不新建分支、不 push） |
| 提交方式 | **允许自动提交**：一步一提交，中文标题 + `为什么` + `验证` |
| 状态 | `Ready` |

开工基线（大脑 2026-11-07 只读核验）：

| 项 | 实测 |
|---|---|
| `Scripts` 文件 | 77 个 `.cs`；6 个目录 `App/Content/Core/Editor/Gameplay/UI` |
| asmdef | `Dungeon.Core`、`Dungeon.Content` 已建 |
| 上行边 | **11 条**（见 §1） |
| 套件基线 | `REFACTOR` 16/0、`SPEC01` 12/0、`FND00B06` 12/0+2obs、`DIAG` 6/1（已知红 `Diag.Input.FirstExit`） |
| Unity | 模式 B，`Dungeon@4f5c34c82f31646e`，场景 `DungeonWorld`，`isDirty=false` |

## §1 需求（Why / What）

**一句话目标**：把 Gameplay 对 App/UI 的 11 条上行引用清零（`audit_scripts.py` 的 `upward_layer_edges = 0`），且暂停 / 升级 / 背包模态 / 地牢门过场的行为与现在**逐项等价**；每个子项先补一条可自动断言（红→绿）。

**11 条上行边（当前实测，`文件:行`）**

| # | 来源 | 引用 | 方向 | 子项 |
|---|---|---|---|---|
| 1 | `Gameplay/Combat/MeleeCombat.cs:150` | `MetaState.Level` | Gameplay→App | 1 升级 |
| 2 | `Gameplay/Player/PlayerCommandController.cs:27` | `AppFlow.IsPaused` | Gameplay→App | 2 暂停 |
| 3 | `Gameplay/Player/SelectionManager.cs:94` | `AppFlow.IsPaused` | Gameplay→App | 2 |
| 4 | `Gameplay/Player/SkillInputController.cs:48` | `AppFlow.IsPaused` | Gameplay→App | 2 |
| 5 | `Gameplay/Units/BossBrain.cs:74` | `AppFlow.IsPaused` | Gameplay→App | 2 |
| 6 | `Gameplay/Units/CharacterBrain.cs:149` | `AppFlow.IsPaused` | Gameplay→App | 2 |
| 7 | `Gameplay/Units/DownedRescue.cs:215` | `AppFlow.IsPaused` | Gameplay→App | 2 |
| 8 | `Gameplay/Units/MonsterBrain.cs:121` | `AppFlow.IsPaused` | Gameplay→App | 2 |
| 9 | `Gameplay/Player/PlayerCommandController.cs:28` | `UIManager.instance.IsInventoryOpen` | Gameplay→UI | 3 模态 |
| 10 | `Gameplay/Player/SkillInputController.cs:49` | `UIManager.instance.IsInventoryOpen` | Gameplay→UI | 3 |
| 11 | `Gameplay/World/DungeonDoor.cs:51` | `TransitionFader.Transition` | Gameplay→UI | 4 门 |

**必须满足**

| 编号 | 需求 | 验收 |
|---|---|---|
| R-1 | `Scripts/Gameplay/**` 不再出现 `AppFlow.`、`UIManager.`、`TransitionFader.`、`MetaState.` 任何一个 | `M7-2.UpwardEdges` |
| R-2 | 暂停语义：`PauseState.IsPaused` 只由 `AppFlow.Pause/Resume` 写；`Time.timeScale` 仍只由 AppFlow（与 RunFlow 团灭）写；团灭 `timeScale=0` **不算暂停**；`ReloadActiveScene` 复位 | `M7-2.Pause` + `Refactor.PauseFacade` |
| R-3 | 升级语义：`ProgressionState.Level` 默认 1、跨场景保留、不自动重置；`MetaState.TryLevelUp` 仍 `+1 级 / dmgMult +0.25 / maxHp +20 / RaiseChanged`；新单位 `Start` 仍按 `(Level-1)*0.25` 补 | `M7-2.Progression` + `Refactor.ProgressionFacade` |
| R-4 | 模态语义：`IUiModalState.IsModalOpen` **严格等于** `UIManager.IsInventoryOpen`（不扩大为任意面板）；`UIManager` 在 `Awake` 注册、`OnDestroy` 注销；未注册时 Gameplay 不阻塞 | `M7-2.Modal` + `Refactor.ModalFacade` |
| R-5 | 门过场：`DungeonDoor` 只发 `TransitionRequest { Action midAction }` 事件；`TransitionFader` 在 `OnEnable/OnDisable` 订阅/退订；黑屏→传送→亮屏行为不变 | `M7-2.Door` + `Refactor.DoorTransitionEvent` |
| R-6 | 每个子项先写失败断言（red，留日志）→ 再实现到绿（green）；提交时套件必须全绿 | `M7-2.RedGreen` |
| R-7 | `REFACTOR` 断言从 16 增到 **20**（新增 4 条），最终 20/0；`SPEC01` 12/0；`FND00B06` 12/0+2obs | `M7-2.REFACTOR` 等 |
| R-8 | 不改任何场景/预制体/Animator/`.asset`；新增 `.cs` 只允许 4 个（§2.1） | `M7-2.NoAsset` |
| R-9 | 4 个子项各 1 条提交 + 1 条收口文档提交；中文标题 + `为什么`/`验证`；不 push | `M7-2.Commits` |

**不在范围**：M7-3 的 `Gameplay/App/UI/Editor` asmdef；M7-4 全量回归；删旧适配层（`GameData`/`GameFlow`/`PauseManager` 保留）；改 `Docs/01` §1.4；改场景/预制体；M6/M5/Unity 6.3。

**约束**

1. `Time.timeScale` 的唯一写入口仍是 `AppFlow.Pause/Resume/ReloadActiveScene` 与 `RunFlow.Tick`（团灭）；`PauseState` **不碰** `timeScale`。
2. `AppFlow.IsPaused` 保留（`PauseManager`/`EscMenuPanel` 在用），改为转发 `PauseState.IsPaused`。
3. `MetaState.Level` 保留为转发属性（`GameData`/`HUD` 在用），不得改 `TryLevelUp` 的收益数值。
4. 新类型放 `Dungeon.Core` / `Dungeon.Gameplay` / `Dungeon.Gameplay.World`，不得放 `Dungeon.App`/`Dungeon.UI`。

## §2 方案（Plan）

**备选方案**

| 方案 | 做法 | 优点 | 缺点 | 结论 |
|---|---|---|---|---|
| A | 把 `AppFlow`/`UIManager`/`TransitionFader` 整体下沉 Gameplay | 改动最少 | App/UI 失去分层；UI→Gameplay→UI 成环；M7-3 必炸 | 弃用 |
| B | 在 Gameplay 定义「状态/事件/接口」下沉点：`PauseState`、`ProgressionState`、`IUiModalState`、`TransitionRequest` | 依赖方向单向；行为可逐项断言；回滚粒度细 | 4 个子项、4 次套件 | **选定**（D5–D7、D13） |
| C | 用反射/服务定位器绕开编译期引用 | 不改调用点 | 隐藏依赖，asmdef 仍无法约束；可读性差 | 弃用 |

**数据流**

- 暂停：`EscMenuPanel → AppFlow.Pause → PauseState.Set(true) + timeScale=0`；Gameplay 7 处只读 `PauseState.IsPaused`。
- 升级：`HUD → MetaState.TryLevelUp → ProgressionState.Level++ + MeleeCombat.dmgMult += 0.25`；`MeleeCombat.Start` 只读 `ProgressionState.Level`。
- 模态：`UIManager.Awake → UiModalState.Current = this`；Gameplay 2 处读 `UiModalState.Current?.IsModalOpen`。
- 门：`DungeonDoor → EventBus.Publish(TransitionRequest)`；`TransitionFader.OnEnable → Subscribe` → 播黑屏 → `midAction` 传送 → 亮屏。

### 2.1 子项与文件（预批改动清单，清单外一律停）

#### 子项 1｜升级边（1 条）

| 文件 | 动作 |
|---|---|
| `Assets/_Project/Scripts/Core/ProgressionState.cs` | **新增**：`public static class ProgressionState { public static int Level { get; set; } = 1; }` |
| `Assets/_Project/Scripts/App/MetaState.cs` | `Level` 改为转发 `ProgressionState.Level`；`TryLevelUp` 其余不变 |
| `Assets/_Project/Scripts/Gameplay/Combat/MeleeCombat.cs` | `MeleeCombat.cs:150` 改 `ProgressionState.Level`（`using Dungeon.Core;` 已有） |
| `Assets/_Project/Tests/REFACTOR/Runtime/REFACTOR_ProbeRunner.cs` | 新增断言 `Refactor.ProgressionFacade`（见 §2.2） |

#### 子项 2｜暂停边（7 条）

| 文件 | 动作 |
|---|---|
| `Assets/_Project/Scripts/Core/PauseState.cs` | **新增**：`IsPaused { get; private set; }` + `Set(bool)` |
| `Assets/_Project/Scripts/App/AppFlow.cs` | `IsPaused => PauseState.IsPaused`；`Pause/Resume/ReloadActiveScene` 调 `PauseState.Set(...)`，`timeScale` 逻辑不变 |
| `Gameplay/Player/PlayerCommandController.cs` `SelectionManager.cs` `SkillInputController.cs` | `AppFlow.IsPaused` → `PauseState.IsPaused`；删 `using Dungeon.App;`（若不再用） |
| `Gameplay/Units/BossBrain.cs` `CharacterBrain.cs` `DownedRescue.cs` `MonsterBrain.cs` | 同上 |
| `Assets/_Project/Tests/REFACTOR/Runtime/REFACTOR_ProbeRunner.cs` | 新增断言 `Refactor.PauseFacade` |

#### 子项 3｜模态边（2 条）

| 文件 | 动作 |
|---|---|
| `Assets/_Project/Scripts/Gameplay/IUiModalState.cs` | **新增**：`namespace Dungeon.Gameplay { public interface IUiModalState { bool IsModalOpen { get; } } public static class UiModalState { public static IUiModalState Current { get; set; } } }` |
| `Assets/_Project/Scripts/UI/UIManager.cs` | `class UIManager : MonoBehaviour, IUiModalState`；`IsModalOpen => IsInventoryOpen`；`Awake` 注册、`OnDestroy` 注销 |
| `Gameplay/Player/PlayerCommandController.cs` `SkillInputController.cs` | 改为 `if (UiModalState.Current != null && UiModalState.Current.IsModalOpen) return;`；删 `using Dungeon.UI;` |
| `Assets/_Project/Tests/REFACTOR/Runtime/REFACTOR_ProbeRunner.cs` | 新增断言 `Refactor.ModalFacade` |

#### 子项 4｜门过场边（1 条）

| 文件 | 动作 |
|---|---|
| `Assets/_Project/Scripts/Gameplay/World/TransitionRequest.cs` | **新增**：`namespace Dungeon.Gameplay.World { public struct TransitionRequest { public System.Action midAction; } }` |
| `Assets/_Project/Scripts/Gameplay/World/DungeonDoor.cs` | `TransitionFader.Transition(...)` → `EventBus.Publish(new TransitionRequest { midAction = ... })`；删 `using Dungeon.UI;` |
| `Assets/_Project/Scripts/UI/TransitionFader.cs` | `OnEnable` 订阅 `EventBus.Subscribe<TransitionRequest>`、`OnDisable` 退订；handler 调 `Transition(req.midAction)` |
| `Assets/_Project/Tests/REFACTOR/Runtime/REFACTOR_ProbeRunner.cs` | 新增断言 `Refactor.DoorTransitionEvent` |

**新增 `.cs` 共 4 个**（每个配 `.meta`，由 Unity 生成）：Core 2、Gameplay 根 1、Gameplay/World 1 → `Scripts` 总文件 **77 → 81**。

### 2.2 新增的 4 条自动断言（写在 REFACTOR 第 17–20 条）

统一要求：放在现有 16 条**之后**、用 `RunSafe("<ID>", ...)`；**进入前保存、结束前还原**（`RunState.Gold`、`ProgressionState.Level`、`PauseState`、`Time.timeScale`），不得污染已有断言。

| 断言 ID | 断言内容 |
|---|---|
| `Refactor.ProgressionFacade` | ① `MetaState.Level == ProgressionState.Level`；② 设 `RunState.Gold = 50*Level` 后 `MetaState.TryLevelUp(null,null)==true`，`Level` +1、金币归 0；③ 还原 |
| `Refactor.PauseFacade` | ① `PauseState.Set(true)` → `PauseState.IsPaused && AppFlow.IsPaused`；② `Set(false)` → 两者 false；③ `AppFlow.Pause()` → `timeScale==0 && IsPaused`；`Resume()` → `timeScale==1 && !IsPaused`；④ 还原 |
| `Refactor.ModalFacade` | ① `UiModalState.Current != null`（UIManager 已注册）；② `UiModalState.Current.IsModalOpen == UIManager.instance.IsInventoryOpen`；③ 打开/关闭背包各断言一次 |
| `Refactor.DoorTransitionEvent` | ① 订阅一个临时 `TransitionRequest` handler；② `EventBus.Publish(new TransitionRequest { midAction = () => flag = true })`；③ 等 `2 * fadeTime + 0.5s`（realtime），`flag==true` 且 `canvasGroup.alpha==0`；④ 退订 |

> 上线顺序：每条断言先在**未实现**时跑一次留红日志（`red/`），实现后跑绿；提交时只提交绿。

### 2.3 实施顺序

1. 复核基线（HEAD `0a78111a`、工作区干净、`is_compiling=false`、场景非脏）。
2. 提交待入库文档：`当前状态.md`（空目录已清理）+ 本 TASK + 长任务计划（`文档：` 一条）。
3. 子项 1：写断言 → 跑 `REFACTOR` 留红 → 实现 → 编译 0 error → `REFACTOR`（17/0）→ `SPEC01`（12/0）→ 审计 → 证据 → 提交。
4. 子项 2：同上（18/0）。
5. 子项 3：同上（19/0）。
6. 子项 4：同上（20/0）+ `FND00B06`（12/0+2obs）+ Play 30 秒冒烟。
7. 收口：REPORT + 讲稿 + 醒来报告/目检清单 + `git log` → 提交。

## §3 验证（Validation）

### 3.1 AC 表

| AC | Given / When / Then | 断言 ID | 证据 |
|---|---|---|---|
| AC-1 | 四个子项完成后，`Scripts/Gameplay/**` 无 `AppFlow.`/`UIManager.`/`TransitionFader.`/`MetaState.`，且 `audit_scripts.py` 的 `upward_layer_edges` 长度 = **0** | `M7-2.UpwardEdges` | `Docs/QA/Evidence/M7-2/收口/upward.txt` + `audit/stats.json` |
| AC-2 | 每子项后跑 `Tools/QA/In-Editor/REFACTOR`，最终 `pass=20 fail=0`；子项 1/2/3 分别为 17/18/19 | `M7-2.REFACTOR` | 各子项 `suites.md` + `REFACTOR-M4/editor_runN/*Summary*.json` |
| AC-3 | 每子项后 `SPEC01` = 12/0；收口连跑 2 次一致 | `M7-2.SPEC01` | 同上（`SPEC-01/`） |
| AC-4 | 收口跑 `FND00B06` = 12/0 + 2 observed；`code=0` | `M7-2.FND00B06` | `收口/suite-fnd00b06/` |
| AC-5 | 每条新断言都有“实现前红、实现后绿”两组日志，且红日志的失败 ID 正确 | `M7-2.RedGreen` | 各子项 `red/`、`green/` |
| AC-6 | 暂停：`PauseState` 标志与 `timeScale` 解耦；团灭 `timeScale=0` 时 `IsPaused==false`；`ReloadActiveScene` 复位 | `M7-2.Pause` | `Refactor.PauseFacade` + 子项 2 `Summary` |
| AC-7 | 升级：默认 1、跨场景保留、`TryLevelUp` 收益与原来一致 | `M7-2.Progression` | `Refactor.ProgressionFacade` |
| AC-8 | 模态：`IsModalOpen == IsInventoryOpen`；注册/注销成对；未注册不阻塞 | `M7-2.Modal` | `Refactor.ModalFacade` |
| AC-9 | 门：事件路径生效、黑屏后传送、亮屏归零；`DungeonDoor` 不再引用 UI | `M7-2.Door` | `Refactor.DoorTransitionEvent` + `play-smoke.md` |
| AC-10 | 无场景/预制体/Animator/`.asset` 改动；新增 `.cs` 恰 4 个且 `.meta` 成对；`Scripts` 81 文件 | `M7-2.NoAsset` | `收口/no-asset.txt` + `verify_m7_2.py` |
| AC-11 | 4 个子项提交 + 1 条收口文档提交；中文标题 + `为什么`/`验证`；`git status` 干净；未 push | `M7-2.Commits` | `收口/git-log.txt` |

**运行次数**：每子项 `REFACTOR` ≥1（含 red 1 次 + green ≥1）、`SPEC01` ≥1；收口 `REFACTOR`/`SPEC01` 各连续 2 次。

**自动化优先**：结构调整全部由 `Docs/QA/Evidence/M7-2/verify_m7_2.py` 断言（可重复运行，`--diff-base 0a78111a`）：

| 断言 ID | 内容 |
|---|---|
| `M7-2.Structure.Files` | `Scripts/**/*.cs` = 81；`Core 9 / Content 3 / Gameplay根1 / Units17 / Combat14 / Player5 / World10 / Items2 / Interaction3 / App6 / UI10 / Editor1` |
| `M7-2.Structure.Namespace` | 每个 `.cs` 的 namespace 与目录映射一致（新增 `Gameplay/` → `Dungeon.Gameplay`） |
| `M7-2.UpwardEdges` | 扫 `Scripts/Gameplay/**`，`AppFlow.`/`UIManager.`/`TransitionFader.`/`MetaState.` 命中数 = 0 |
| `M7-2.NoAsset` | `git diff --name-only 0a78111a..HEAD -- Assets` 只含 `Scripts/**/*.cs` 与 `.cs.meta` |
| `M7-2.Asmdef` | Core/Content asmdef 仍在且引用不变 |

**人工目检（≤3，留给所有者）**：① 进 Play 后 Esc 暂停/恢复（AI 停手、恢复后行动）；② 开背包时右键/技能被屏蔽，关掉恢复；③ 地牢门黑屏过场 + 传送 + 相机正常。

### 3.2 证据目录

`Docs/QA/Evidence/M7-2/`：

```
子项1..4/Summary.md, compile.md, suites.md, audit/, red/, green/, git_show_stat.txt
收口/upward.txt, no-asset.txt, suite-fnd00b06/, git-log.txt, verify.txt
```

## §4 执行信息

| 项 | 内容 |
|---|---|
| 前置 | HEAD `0a78111a`、工作区干净；模式 B，实例 `Dungeon@4f5c34c82f31646e`；场景非脏；`py -3.12` |
| 允许改动 | §2.1 列出的 4 个新 `.cs` + 12 个修改 `.cs` + `REFACTOR_ProbeRunner.cs` + 证据 + REPORT + 讲稿 |
| 允许新增 | 4 个 `.cs` 的 `.meta`（Unity 生成）；`Docs/QA/Evidence/M7-2/**`；`Docs/学习/M7-2_讲稿.md` |
| 绝不触碰 | 场景/预制体/Animator/`.asset`/`ProjectSettings`/`Packages`/`.meta` 内容；`Tests` 其他文件；`Docs/01`；`当前状态.md`（大脑维护） |
| MCP 授权 | 只读 `editor_state`/`read_console`/`find_gameobjects`；`refresh_unity`；`execute_menu_item`（REFACTOR/SPEC01/FND00B06）；`manage_editor` play/stop（仅 30 秒冒烟，不保存）。**写操作一律用执行者自己的文件编辑工具**；不启用 `execute_code`/`manage_scene` 写 |
| 提交策略 | 允许自动提交：子项 1–4 各 1 条 + 收口 1 条 + 文档 1 条；不 push |
| 熔断 | 见长任务计划 §6；额外：任一子项 3 轮仍红 → revert 该子项提交并停 |

## §5 学习产出

| 项 | 要求 |
|---|---|
| 讲稿 | **必须**：`Docs/学习/M7-2_讲稿.md`（高风险 + 改行为）。含 4 个下沉点的取舍、`timeScale` 与 `IsPaused` 解耦、模态注册时序、事件订阅生命周期、答辩 Q&A 5 条 |
| 三句话版本 | REPORT §3.7 |

## 自包含声明

本工单不依赖 `当前状态.md` 或其他任务书即可执行。信息不足时在 REPORT 写“信息不足”并列缺项，不自行取舍。
