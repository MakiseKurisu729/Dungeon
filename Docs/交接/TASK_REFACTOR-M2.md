# REFACTOR-M2 — 生命周期拆分（状态容器 + 流程宿主 + 暂停看门人）

> 长任务 `REFACTOR-LONG-01` 的 M2 小节。上游依据：`C:\Users\lll\.opencode\plan\refactor-plan.md` §2/§9-M2。
> 本单自包含；执行者只读本文件 + 仓库现状即可开工。

## §0 元信息

| 字段 | 值 |
|---|---|
| 任务 ID | `REFACTOR-M2` |
| 风险等级 | `常规`（脚本改动；不碰场景/预制体/Animator/`.asset`/`ProjectSettings`） |
| 授权 | 所有者 2026-11-05 批准长任务计划书；本里程碑在预授权范围内 |
| 基线提交 | `0a1a45d7`（M1 收口；开工时以 `git rev-parse --short HEAD` 记录实际值） |
| 回滚锚 | `0a1a45d7`；回滚 = `git revert <M2 commit>` / `git reset --hard 0a1a45d7`（仅所有者可执行） |
| Unity 模式 | **模式 B（编辑器常开 + MCP）**，Unity 2022.3.62f3c1 |
| 状态 | `Ready` |

> 执行者**不得**标 Done、不得改 `CURRENT.md`、**不得**做任何 git 写操作（提交由大脑/所有者做）。

## §1 需求（Why / What）

**一句话目标**：把「局内状态 / 成长状态 / 提示消息」从一个 `GameData` 静态类拆成三个各有生命周期的容器，把「场景·暂停·重开」与「局内结果判定」从一个 `GameFlow` MonoBehaviour 拆成两个逻辑主体，并把 `PauseManager` 收编为 `AppFlow` 的时间看门人；**旧 API 全部保留为转发适配层**（测试与未迁移引用照常工作），行为逐条不变。

**用户故事**：作为重构负责人，我要状态与流程各自有明确归属和重置时机，以便后续做「预制体基座 / 多场景 / 读档」时不必再面对一堆互相污染的全静态状态。

**必须满足**

| 编号 | 需求 | 验收方式 |
|---|---|---|
| R-1 | 新增 `Scripts/Core/RunState.cs`、`MetaState.cs`、`MessageFeed.cs`、`AppFlow.cs`、`RunFlow.cs`（契约见 §2） | 代码审查 + 编译 |
| R-2 | 产品调用点迁移到新 API：`Scripts/**`（`Core/GameData.cs`、`Core/GameFlow.cs`、`Core/PauseManager.cs` 三个适配层文件除外）里 `GameData.`、`PauseManager.instance`、`GameFlow.GameStarted`、`GameFlow.instance.IsWiped` 的引用计数 = 0 | grep 计数证据 |
| R-3 | 旧 API 仍可用且语义不变：`GameData.gold/level/kills/message/AddGold/AddKill/SetMessage/LevelCost/TryLevelUp/OnChanged`、`GameFlow.instance/GameStarted/IsWiped`、`PauseManager.instance/Pause/Resume/IsPaused` | `Refactor.StateFacade` 断言 + 测试仍能编译 |
| R-4 | **不发生行为变化**：金币/等级/击杀/提示文案、团灭判定与 R 重开、Esc 暂停/恢复、timeScale 行为全部与 M1 一致；**不引入新的自动重置**（见 §2「重置语义」） | REFACTOR 10/10 + SPEC01 12/12 |
| R-5 | 现有断言口径零改动（允许新增 1 条，总数 9→10）；测试文件里对 `GameData.*` / `GameFlow.*` / `PauseManager.instance` 的引用**不得删改**（适配层就是为它们留的） | diff 审查 |

**不在范围**

- 不新增/删除任何 `MonoBehaviour` 到场景（`GameFlow`、`PauseManager` 仍是场景里的原组件，改为薄适配层）。
- 不改交互、技能、战斗（M3/M4）。
- 不改 `UnitRegistry` / `UnitId`（M1 已收口）。
- 不改存读档、UI 布局、`SimplePool`、迷雾、`EventBus`。
- 不新增 `asmdef`。

**约束**

- `Docs/01` §1.4 不变量：状态归属单一、跨场景数据不靠散落静态字段。
- 序列化字段名冻结：本单不动任何序列化字段（三个适配层保持原有 Inspector 暴露面）。
- **测试文件里的 `GameData.gold` 等引用是验收契约的一部分，禁止为了“清理”去改测试。**

## §2 方案（Plan）

**备选方案**

| 方案 | 做法 | 优点 | 缺点 | 结论 |
|---|---|---|---|---|
| A | 直接删 `GameData`/`GameFlow`/`PauseManager`，全仓改引用（含测试） | 最“干净” | 必须改测试引用 → 违反“测试只增不改”；断点多、难回滚 | 弃用 |
| B | 新增三个状态容器 + 两个流程主体，旧类保留为**薄适配层**，产品调用点全量迁移，测试引用不动 | 行为可证不变、可分批、回滚容易 | 短期多一层转发 | **选定** |
| C | 只加注释/区域划分，不拆类 | 改动最小 | 没有可验证的归属边界，等于没做 | 弃用 |

**选定方案与理由**：B —— 用适配层换取“测试零改动 + 行为零变化”，同时让新产品代码只看到新契约。

**契约（必须照此实现；命名可微调，语义与可见性不得改）**

```csharp
// Scripts/Core/RunState.cs —— 局内状态（金币/击杀）
public static class RunState
{
    public static int Gold { get; set; }        // 读写；负值不额外裁剪（保持原字段语义）
    public static int Kills { get; set; }
    public static void AddGold(int g);           // 只加钱，不触发消息事件
    public static void AddKill();                // 只加击杀
}

// Scripts/Core/MetaState.cs —— 成长状态（等级 + 升级收益）
public static class MetaState
{
    public static int Level { get; set; }        // 默认 1
    public static int LevelCost();               // 50 * Level
    public static bool TryLevelUp(MeleeCombat sw, UnitHealth uh);  // 与原实现一致
}

// Scripts/Core/MessageFeed.cs —— 提示消息
public static class MessageFeed
{
    public static string Message { get; set; }
    public static event System.Action Changed;
    public static void SetMessage(string m);     // 赋值并触发 Changed（即使文案相同也触发，保持原语义）
}

// Scripts/Core/AppFlow.cs —— 场景 / 暂停 / 重开
public static class AppFlow
{
    public static bool GameStarted { get; set; }  // 原 GameFlow.GameStarted
    public static bool IsPaused { get; }          // 原 PauseManager.instance.IsPaused
    public static void Pause();                   // timeScale = 0
    public static void Resume();                  // timeScale = 1
    public static void ReloadActiveScene();       // 原 R 重开：GameStarted=false; timeScale=1; LoadScene(buildIndex)
    public static bool AnyPlayerTeam();           // 原 GameFlow.HasPlayerTeam
}

// Scripts/Core/RunFlow.cs —— 局内结果
public static class RunFlow
{
    public static bool IsWiped { get; }           // 原 GameFlow.instance.IsWiped
    public static void EnterScene();              // 原 GameFlow.Start：GameStarted=false，有玩家队伍则开局 + timeScale=1
    public static void Tick();                    // 原 GameFlow.Update：团灭判定 + R 重开
}
```

- **行为不变判据**：`AppFlow`/`RunFlow` 的方法体必须与原 `GameFlow.Start/Update`、`PauseManager.Pause/Resume` **逐行等价**；只允许增加「适配层转发」与「空值保护不改变结果」的改动。任何一处“顺手优化”都算越界。
- **重置语义（重要）**：`RunState`/`MetaState`/`MessageFeed` 本单**不新增任何自动重置**。当前 `GameData` 的静态字段跨场景重载保留，重构后必须**保持**这一事实；`RunFlow.EnterScene()` 里**不得**清空金币/等级/击杀。若你认为“应该在重开时清空”，写进 REPORT §7 未决项交给所有者，不要自行实现。
- **`GameFlow` 适配层**：`instance`、`GameStarted`（静态可写属性，转发 `AppFlow.GameStarted`）、`IsWiped`（实例属性转发 `RunFlow.IsWiped`）三个成员必须保留原签名与可写性；`Awake/Start/Update` 改为转发。
- **`PauseManager` 适配层**：`instance`、`IsPaused`（转发 `AppFlow.IsPaused`）、`Pause()`/`Resume()`（转发）。
- **`GameData` 适配层**：字段改成转发属性（`gold`→`RunState.Gold`、`level`→`MetaState.Level`、`kills`→`RunState.Kills`、`message`→`MessageFeed.Message`），方法全部转发，`OnChanged` 用 `add/remove` 转发到 `MessageFeed.Changed`。
- **文件命名/位置**：全部放 `Assets/_Project/Scripts/Core/`，与现有 `EventBus.cs` 同层。

**数据流**

`MonsterBrain/BossBrain/Chest/InventoryUI` → `RunState.AddGold` →（`HUD.Update` 轮询读数）
`HUD` → `MetaState.TryLevelUp` → `MeleeCombat.dmgMult` / `UnitHealth.AddMaxHp`
`各系统` → `MessageFeed.SetMessage` → `HUD` 轮询显示
`EscMenuPanel` → `AppFlow.Pause/Resume` → `Time.timeScale`
`GameFlow(Awake/Start/Update)` → `AppFlow.EnterScene` / `RunFlow.Tick` → `UnitRegistry.OfTeam(Player)`

**影响面清单**

| 项 | 内容 |
|---|---|
| 新增文件 | `Core/RunState.cs`、`Core/MetaState.cs`、`Core/MessageFeed.cs`、`Core/AppFlow.cs`、`Core/RunFlow.cs`（+ Unity 生成 `.meta`） |
| 修改文件（核心） | `Core/GameData.cs`、`Core/GameFlow.cs`、`Core/PauseManager.cs`（改薄适配层） |
| 修改文件（调用点） | `Core/WaveSpawner.cs`；`Combat/MeleeCombat.cs`、`Combat/SkillSystem.cs`、`Combat/SkillEffects.cs`、`Combat/SkillEffectFireball.cs`；`Units/MonsterBrain.cs`、`Units/BossBrain.cs`、`Units/DownedRescue.cs`、`Units/CharacterBrain.cs`；`Player/SkillInputController.cs`、`Player/PlayerCommandController.cs`、`Player/SelectionManager.cs`；`System/DungeonDoor.cs`、`System/Chest.cs`；`UI/HUD.cs`、`UI/CursorManager.cs`、`UI/InventoryUI.cs`、`UI/EscMenuPanel.cs` |
| 修改符号 | 只替换引用：`GameData.X` → `RunState.X` / `MetaState.X` / `MessageFeed.X`；`PauseManager.instance.IsPaused` → `AppFlow.IsPaused`；`GameFlow.GameStarted` → `AppFlow.GameStarted`；`GameFlow.instance.IsWiped` → `RunFlow.IsWiped` |
| 字符串引用 | 无新增；**不得**改 Animator 参数名、`SendMessage`、`GameObject.Find`、提示文案文本 |
| 场景 / 预制体 / `.asset` | **零改动**（`GameFlow`、`PauseManager` 组件仍在原物体上，脚本路径不变） |
| `.meta` | 仅 5 个新脚本自带；不移动/重命名任何已有资产 |
| 测试文件 | `Tests/REFACTOR/Editor/REFACTOR_BatchAcceptance.cs`（`DefaultMilestone` → `"M2"`）；`Tests/REFACTOR/Runtime/REFACTOR_ProbeRunner.cs`（新增第 10 条 `Refactor.StateFacade`） |

**实施顺序**

1. 先写 5 个新类（编译通过，旧类暂不动）。
2. 三个旧类改薄适配层（编译通过）。
3. 按上面文件表逐文件迁移调用点（每 4–5 个文件编译一次）；**用 grep 复核**，不留漏网。
4. `DefaultMilestone = "M2"`，新增第 10 条断言。
5. 跑 L1：REFACTOR ×3 + SPEC01 ×2（编辑器内菜单）。
6. 写证据与 REPORT。

**风险与回滚**

- 最可能错：① `GameStarted` 变成属性后有人取地址/反射写字段；② `GameData.OnChanged` 从 `event` 改 `add/remove` 后语义差异（重复订阅/退订）；③ `message` 相同文案时是否仍触发事件；④ `HUD` 轮询与 `MessageFeed` 引用类型（string 为不可变，无别名问题）。
  - 检测：`Refactor.StateFacade` 第 2/4 条 + SPEC01 的 HUD/提示相关断言 + REFACTOR 全 10 条。
  - 处理：以“原实现逐行等价”为准，不做“更合理”的改写。
- 回滚锚：`0a1a45d7`。

## §3 验证（Validation）

**AC 表**

| AC | Given / When / Then | 断言 ID | 证据 |
|---|---|---|---|
| AC-1 | 给定 `Assets/_Project/Scripts/**/*.cs`，当 grep `GameData\.` / `PauseManager\.instance` / `GameFlow\.GameStarted` / `GameFlow\.instance`，则**除 `Core/GameData.cs`、`Core/PauseManager.cs`、`Core/GameFlow.cs` 自身外计数 = 0** | `Refactor.NoLegacyState`（grep 证据） | `Docs/QA/Evidence/REFACTOR-M2/grep-legacy-state.txt` |
| AC-2 | 给定 REFACTOR 套件，连续跑 3 次，则每次 **10/10、fail=0、blocked=0、infra=0、exit=0** | 套件全部 10 条 | `Docs/QA/Evidence/REFACTOR-M2/editor_run1..3/` |
| AC-3 | 给定 SPEC01 套件，跑 2 次，则每次 **12/12、fail=0、exit=0** | SPEC01 全 12 条 | `Docs/QA/Evidence/SPEC-01/editor_run<新>/` |
| AC-4 | 给定新旧 API，当读写它们，则两侧视图一致且事件只按原语义触发（见下「新增断言」） | `Refactor.StateFacade` | 同上 JSONL 末行 |
| AC-5 | 给定 `git status`，则只含本单声明文件 + 派单前既有脏项 | — | `REFACTOR-M2/git-diff-stat.txt` |
| AC-6 | 给定三个适配层，则 `GameFlow.instance/GameStarted/IsWiped`、`PauseManager.instance/IsPaused/Pause/Resume`、`GameData` 全部成员**签名与可写性与 M1 完全一致**（测试引用未改仍能编译） | 编译 + diff 审查 | `REFACTOR-M2/adapter-api-diff.txt` |

**新增断言（REFACTOR 套件内加第 10 条，不得改前 9 条口径）**

`Refactor.StateFacade`（Play 模式，全部步骤结束后恢复原值，不得污染后续断言）：

1. `RunState.Gold = 7` → `GameData.gold == 7`；反向 `GameData.gold = 3` → `RunState.Gold == 3`；
2. `MetaState.Level` 与 `GameData.level` 双向一致；`MetaState.LevelCost() == 50 * MetaState.Level` 且等于 `GameData.LevelCost()`；
3. `MessageFeed.SetMessage("REFACTOR_STATE_PROBE")` → `GameData.message == "REFACTOR_STATE_PROBE"`，且订阅 `MessageFeed.Changed` 计数 = 1（**恰好一次**）；退订后再 `SetMessage` 计数不增；
4. `AppFlow.GameStarted` 与 `GameFlow.GameStarted` 双向一致（写入后立刻还原，**不得**影响 `RunFlow.Tick`）；
5. `RunFlow.IsWiped == GameFlow.instance.IsWiped`；
6. `AppFlow.Pause()` → `PauseManager.instance.IsPaused == true` 且 `Time.timeScale == 0`；`AppFlow.Resume()` → `false` 且 `1`（结束时保证 `timeScale == 1`）。

**运行次数**：REFACTOR **N=3**；SPEC01 **2 次**。

**自动化优先**：全部通过 `Tools/QA/In-Editor/{REFACTOR, SPEC01 (green)}` 菜单执行；**不得**用 `run_tests`，不得 batchmode。

**必须保存的证据**：`REFACTOR_Assertions.jsonl` / `REFACTOR_Summary_run*.json` / `REFACTOR_ExitCode_run*.txt` / `InEditorResult.txt`（两套同结构）+ `grep-legacy-state.txt`、`git-diff-stat.txt`、`adapter-api-diff.txt`。

## §4 执行信息

| 项 | 内容 |
|---|---|
| 前置 | Unity 已开（模式 B），MCP 单实例；开工前 `editor_state.is_compiling=false`；当前场景 `DungeonWorld` 且非脏 |
| 允许改动 | 上表「修改文件」（21 个）+ 新增 5 个 `Core/*.cs`；`Tests/REFACTOR/**`（仅新增第 10 条 + 改 `DefaultMilestone`）；`Docs/Handoff/REPORT_REFACTOR-M2.md`；`Docs/QA/Evidence/REFACTOR-M2/**` |
| 允许读取 | 本工单 + 被改文件现状（只读） |
| 绝不触碰 | `Assets/_Project/Art/**`、`Assets/Scenes/**`、`Assets/_Project/Prefabs/**`、`*.asset`、`ProjectSettings/**`、`Packages/**`、`AGENTS.md`、`Docs/Handoff/CURRENT.md`、其他 `TASK_*`/`REPORT_*`、`Tests/**` 里**除 REFACTOR 套件外**的任何文件、现有断言口径 |
| 证据目录 | `Docs/QA/Evidence/REFACTOR-M2/`（套件自动建）；SPEC01 进 `Docs/QA/Evidence/SPEC-01/editor_run<N>/` |
| 熔断 | `AGENTS.md` §6 + 计划书 §6；修复轮次 **1 轮** |
| 执行预算 | 文件数 ≤ 28；不跑全量 batch 验收；不重启 Unity |

**停止条件（照计划书 §6）**：需要改预制体/场景/`.asset`/`ProjectSettings` 才能继续；需要改 `Docs/01` 不变量或新增 ADR；编译错误连续 2 轮；修复轮用尽仍红；`git status` 对不上；为了“更合理”需要改变玩法行为（交所有者）。

## §5 学习产出

| 项 | 要求 |
|---|---|
| 讲稿 | 不适用（M4 收口后统一写重构讲稿） |
| 三句话版本 | 写进 REPORT §3 第 7 条 |

## 自包含声明

本工单不依赖 `CURRENT.md` 或其他任务书即可执行；信息不足时在 REPORT 写“信息不足”并列缺项，**不**自行翻阅其他文档补全。
