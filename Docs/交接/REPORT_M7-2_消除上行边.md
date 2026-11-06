# REPORT — M7-2 消除 11 条上行边

> 工单：`Docs/交接/TASK_M7-2_消除上行边.md`
> 长任务计划：`Docs/交接/长任务计划_M7-2.md`
> 状态：**完成（M7-2 全范围，停在 `Review` 等终审）**。执行者不标 `Done`、不改 `当前状态.md`、不 push。

## 1. 结果摘要

- `audit_scripts.py` 的 `upward_layer_edges`：**11 → 0**；`Scripts/Gameplay/**` 中 `AppFlow.` / `UIManager.` / `TransitionFader.` / `MetaState.` 命中数 **0**。
- 4 个子项各先留红、再实现到绿：`REFACTOR` 17/0 → 18/0 → 19/0 → **20/0**；`SPEC01` 每步 12/0，收口连续 2 次一致；`FND00B06` 12/0 + 2 observed。
- 暂停/升级/模态/门过场行为逐项等价：`Upgrade` 收益数值未改、`timeScale` 写入口未变、`IsModalOpen` 严格等于 `IsInventoryOpen`、门过场仍是“黑屏→midAction→亮屏”。
- 新增 `.cs` 恰 4 个（+4 `.meta`），`Scripts` 77 → **81**；场景/预制体/Animator/`.asset`/`ProjectSettings`/`Packages` 零改动（`no-asset.txt`：22 个变更路径全部允许）。
- 6 条提交（1 条文档 + 4 子项 + 1 收口），不 push；`git status` 收口后干净。

**三句话版本**

1. 把“升级/暂停/模态/门过场”四个跨层耦合点下沉为 Core/Gameplay 自己的状态、接口或事件，Gameplay 不再 import App/UI，审计上行边归零。
2. 每个子项先写一条可失败的生产入口断言（红），再实现到绿；套件从 16 条扩到 20 条，行为数值与基线逐项一致。
3. 结构单向、行为等价；M7-3 现在可以直接按 `Gameplay/App/UI/Editor` 建 asmdef，而不必再改代码。

## 2. AC 回填表

| AC | 断言 ID | 结果 | 实测数据 | 证据 |
|---|---|---|---|---|
| AC-1 | `M7-2.UpwardEdges` | **PASS** | `upward_layer_edges=0`；`verify` 的 Gameplay 命中数 0；`DungeonDashboard` 无 | `收口/upward.txt`、`收口/verify.txt`、各子项 `audit/stats.json` |
| AC-2 | `M7-2.REFACTOR` | **PASS** | 子项 1/2/3/4 分别 17/0、18/0、19/0、20/0；收口连续 2 次 20/0（`editor_run35`、`editor_run36`），`code=0` | 各子项 `suites.md` + canonical REFACTOR 目录 |
| AC-3 | `M7-2.SPEC01` | **PASS** | 每子项 12/0；收口连续 2 次 12/0（`editor_run54`、`editor_run55`），`code=0` | 各子项 `suites.md` |
| AC-4 | `M7-2.FND00B06` | **PASS** | `pass=12 fail=0 observed=2`，`InEditorResult.txt` `code=0`（`FND-00B-06/editor_run10`） | `收口/suite-fnd00b06/` |
| AC-5 | `M7-2.RedGreen` | **PASS** | 4 个子项均有 `red/`（正确失败 ID）与 `green/`；红→绿套件数字见 AC-2 | `子项1..4/red/`、`子项1..4/green/` |
| AC-6 | `M7-2.Pause` | **PASS** | `Refactor.PauseFacade`：`PauseState.Set` 与 `AppFlow.IsPaused` 同步；`Pause/Resume` 写 `timeScale`；`timeScale=0` 不置 IsPaused；`ReloadActiveScene` 复位 | `子项2/green/`、`子项2/Summary.md` |
| AC-7 | `M7-2.Progression` | **PASS** | `Refactor.ProgressionFacade`：`MetaState.Level==ProgressionState.Level` 双向；`LevelCost=50*Level`；`TryLevelUp` 返回 true、等级 +1、金币归 0；默认 1 未变 | `子项1/green/`、`子项1/Summary.md` |
| AC-8 | `M7-2.Modal` | **PASS** | `Refactor.ModalFacade`：`Current==UIManager`；`IsModalOpen==IsInventoryOpen`；开/关各断言一次；结束前关闭 | `子项3/green/`、`子项3/Summary.md` |
| AC-9 | `M7-2.Door` | **PASS** | `Refactor.DoorTransitionEvent`：事件送达（signal=True）、`midAction` 由 `TransitionFader` 执行、alpha 峰值 >0.5 且最终 0；`DungeonDoor` 不再引用 UI（`verify` 命中 0） | `子项4/green/`、`子项4/play-smoke.md` |
| AC-10 | `M7-2.NoAsset` | **PASS** | `git diff --name-only 0a78111a..HEAD -- Assets` 共 22 个路径，全部为 `Scripts/**/*.cs`、`*.cs.meta` 或 `REFACTOR_ProbeRunner.cs`；新增 `.cs` 恰 4 个且 `.meta` 成对；`Scripts=81` | `收口/no-asset.txt`、`收口/verify.txt` |
| AC-11 | `M7-2.Commits` | **PASS（含 1 处格式瑕疵）** | 6 条提交：文档开工 + 子项 1–4 + 收口；标题/正文中文且含 `为什么：`/`验证：`；未 push；收口后 `git status` 干净。**注**：`91b82016` 的标题首字节带 UTF-8 BOM（PowerShell 写消息文件导致），因禁止 amend 未改，后续提交已改用无 BOM 写入 | `收口/git-log.txt` |

## 3. 实现原理

### 3.1 升级：`ProgressionState` 下沉 Core

- `Core/ProgressionState.cs:5`：`public static int Level { get; set; } = 1;`
- `App/MetaState.cs:10-13`：`Level` 改为转发 `ProgressionState.Level`（get/set）；`LevelCost()` 与 `TryLevelUp` 的数值逻辑不动（`+1 级 / dmgMult +0.25 / maxHp +20 / RaiseChanged`）。
- `Gameplay/Combat/MeleeCombat.cs:149`：新单位伤害倍率改为读 `ProgressionState.Level`；删 `using Dungeon.App;`。
- 语义：默认 1、跨场景保留、不自动重置；新单位仍按 `(Level-1)*0.25` 补。

### 3.2 暂停：`PauseState` 下沉 Core（`timeScale` 解耦）

- `Core/PauseState.cs:6-8`：`IsPaused { get; private set; }` + `Set(bool)`，**不碰 `timeScale`**。
- `App/AppFlow.cs:15`：`IsPaused` 只读转发；`Pause`/`Resume`/`ReloadActiveScene` 先 `PauseState.Set(...)` 再写 `Time.timeScale`（`AppFlow.cs:20/27/36`）。
- 7 处 Gameplay（`PlayerCommandController`/`SelectionManager`/`SkillInputController`/`BossBrain`/`CharacterBrain`/`DownedRescue`/`MonsterBrain`）改读 `PauseState.IsPaused`。
- `RunFlow.Tick` 团灭仍只写 `Time.timeScale=0`，不置 `IsPaused` → “团灭 ≠ Esc 暂停”。

### 3.3 模态：`IUiModalState` + 注册表

- `Gameplay/IUiModalState.cs:3-11`：`IUiModalState { bool IsModalOpen { get; } }` + `UiModalState.Current`。
- `UI/UIManager.cs:15/38/43/52`：实现接口（`IsModalOpen => IsInventoryOpen`），`Awake` 注册、`OnDestroy` 注销。
- Gameplay 两处改为 `UiModalState.Current != null && UiModalState.Current.IsModalOpen`；未注册时不阻塞。
- 语义严格等于 `IsInventoryOpen`，未扩成 `IsAnyPanelOpen`。

### 3.4 门过场：`TransitionRequest` 事件

- `Gameplay/World/TransitionRequest.cs:3-5`：`struct TransitionRequest { System.Action midAction; }`。
- `Gameplay/World/DungeonDoor.cs:50`：`EventBus.Publish(new TransitionRequest { midAction = ... })`，不再 `using Dungeon.UI`。
- `UI/TransitionFader.cs:22-26`：`OnEnable` 订阅、`OnDisable` 退订；handler 调 `Transition(req.midAction)`，黑屏/传送/亮屏逻辑不变。

### 3.5 断言（第 17–20 条）

- `REFACTOR_ProbeRunner.cs:1858` `AssertProgressionFacade`、`:1912` `AssertPauseFacade`、`:1971` `AssertModalFacade`、`:2035` `AssertDoorTransitionEvent`。
- 每条都保存/还原 `RunState.Gold`、等级、`PauseState`、`Time.timeScale`、背包开合；门断言因 DungeonWorld 场景没有门/过场对象，使用运行期 fader 夹具（结束销毁），与既有 `REFACTOR_Target_*` 同惯例。

## 4. 受影响模块导航

| 文件 | 改动 | 说明 |
|---|---|---|
| `Core/ProgressionState.cs`（新） | Level 默认 1 | 升级状态从 App 下沉 |
| `Core/PauseState.cs`（新） | IsPaused + Set | 暂停标志与 timeScale 解耦 |
| `Gameplay/IUiModalState.cs`（新） | 接口 + Current | Gameplay 只读 UI 模态契约 |
| `Gameplay/World/TransitionRequest.cs`（新） | midAction | 门过场事件负载 |
| `App/MetaState.cs` | Level 转发 | 旧 API 保留（GameData/HUD 用） |
| `App/AppFlow.cs` | IsPaused 转发 + 写 PauseState | timeScale 写入口不变 |
| `Gameplay/+7` | 改读 PauseState | 不再引用 App |
| `UI/UIManager.cs` | 实现+注册/注销 | IsModalOpen == IsInventoryOpen |
| `UI/TransitionFader.cs` | 订阅/退订事件 | 过场逻辑不变 |
| `Gameplay/World/DungeonDoor.cs` | 发事件 | 不再引用 UI |
| `Tests/REFACTOR/Runtime/REFACTOR_ProbeRunner.cs` | +4 条断言 | 16 → 20 条 |

## 5. 证据清单

| 证据 | 路径 |
|---|---|
| 子项 1–4 | `Docs/QA/Evidence/M7-2/子项1..4/`（`Summary.md`、`compile.md`、`suites.md`、`audit/`、`red/`、`green/`、`verify.txt`、`git_status.txt`） |
| 收口 | `Docs/QA/Evidence/M7-2/收口/`（`upward.txt`、`no-asset.txt`、`suite-fnd00b06/`、`git-log.txt`、`verify.txt`） |
| 审计脚本（M7-2 副本） | `Docs/QA/Evidence/M7-2/audit_scripts.py`（`OUT` 已改为 M7-2；M7-1 原脚本未改） |
| 结构检查器 | `Docs/QA/Evidence/M7-2/verify_m7_2.py`（`--diff-base 0a78111a --stage 4`） |
| canonical 套件 | `REFACTOR-M4/editor_run27..36`、`SPEC-01/editor_run51..55`、`FND-00B-06/editor_run10` |

## 6. 学习讲稿

- `Docs/学习/M7-2_讲稿.md`（已完成）：4 个下沉点取舍、`timeScale`/`IsPaused` 解耦、模态注册时序、事件订阅生命周期、答辩 Q&A 5 条。

## 7. 未决项

1. **人形/场景目检（留给所有者）**：Esc 暂停/恢复、背包屏蔽右键/技能、地牢门黑屏过场（DungeonWorld 场景本身没有门，门在副本/其他场景，自动断言用运行期 fader 夹具覆盖事件链路）。
2. **M7-3/M7-4 未做**：`Gameplay`/`App`/`UI`/`Editor` asmdef 与全量回归留给后续任务；`DIAG` 未跑（已知红不变）。
3. **`91b82016` 提交标题带 UTF-8 BOM**（不可 amend；后续提交已修正写法）。若必须清理，只能由所有者决定是否重写历史。
4. 门过场的“传送/相机切换”只在 `midAction` 回调里保持原逻辑，未做端到端自动断言（场景无门对象）；需要目检。
5. `verify_m7_2.py` 的 `NoAsset` 白名单显式允许 `Tests/REFACTOR/Runtime/REFACTOR_ProbeRunner.cs`（工单 §2.1 明确允许），其余 Tests 文件不改。

## 8. 建议提交

| # | hash | 标题 |
|---|---|---|
| 0 | `14183c83` | `文档：M7-2 工单与长任务计划（M7-2/开工）` |
| 1 | `91b82016` | `重构：M7-2 升级边下沉 ProgressionState（M7-2/子项1）`（标题首字节含 BOM） |
| 2 | `a28af7b3` | `重构：M7-2 暂停边下沉 PauseState（M7-2/子项2）` |
| 3 | `4c05c1d0` | `重构：M7-2 模态边改 IUiModalState（M7-2/子项3）` |
| 4 | `62479bb7` | `重构：M7-2 门过场改 TransitionRequest 事件（M7-2/子项4）` |
| 5 | 见 `收口/git-log.txt` | `文档：M7-2 交付报告与讲稿（M7-2/收口）` |

- 全部**未 push**；不合并分支、不改写历史。

## 9. 消耗记录

- 墙钟：2026-11-07 01:20–02:10（约 50 分钟）。
- 套件运行：`REFACTOR` 10 次（含 4 次红/阻塞 + 6 次绿）、`SPEC01` 6 次、`FND00B06` 1 次、Play 冒烟 1 次（30 秒）。
- MCP 写调用：0（所有文本改动走执行者编辑工具）；`execute_menu_item` 17 次；`manage_editor` play/stop 各 1 次。
- Token/额度：执行环境未提供计数。
