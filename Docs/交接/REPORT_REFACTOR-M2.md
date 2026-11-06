# REPORT — REFACTOR-M2：生命周期拆分（状态容器 + 流程宿主 + 暂停看门人）

> 对应 `Docs/Handoff/TASK_REFACTOR-M2.md`（长任务 `REFACTOR-LONG-01` 的 M2 小节）。
> 节次与 `Docs/Handoff/workflow-rules.md` §2 一致，未删节；不适用的写“不适用”+ 理由。
> 长任务模式：执行者不标 `Done`、不改 `CURRENT.md`、未做任何 git 写操作（含 commit/push/reset/checkout/stash）。

## 1. 结果摘要

`完成` —— `GameData` 的三种状态拆成 `RunState`（金币/击杀）/ `MetaState`（等级+升级）/ `MessageFeed`（提示），`GameFlow` 的两种职责拆成 `AppFlow`（场景·暂停·重开）/ `RunFlow`（团灭判定+R 重开），三个旧类全部改为**纯转发适配层**（签名与外部可写性不变）；实测把 **20 个调用点文件 / 54 行**旧引用迁移到新 API（`Scripts/**` 排除三个适配层后旧引用计数 = **0**，基线 56 行 / 21 文件）。REFACTOR 套件新增第 10 条 `Refactor.StateFacade` 后 **10/10 连续 4 次全绿（exit 0）**，SPEC01 **12/12 连续 3 次（exit 0）**；未改动任何场景/预制体/Animator/`.asset`/`ProjectSettings`，未修改既有 9 条 REFACTOR 与 12 条 SPEC01 的断言口径。

## 2. AC 回填表

| AC | 结果 | 断言 ID | 关键数据 | 日志路径 |
|---|---|---|---|---|
| AC-1 `Scripts/**`（除三适配层）`GameData.`/`PauseManager.instance`/`GameFlow.GameStarted`/`GameFlow.instance` 计数 = 0 | **通过** | `Refactor.NoLegacyState`（grep 证据，非运行期断言） | 基线 @`0a1a45d7`：**56 行 / 21 文件**；当前全量（含适配层）：**1 行**（`Core/PauseManager.cs:4` 的 `///` 文档注释，文件属排除项）；**排除三适配层后 = 0 行 / 0 文件**；新契约使用点：`RunState.` 10、`MetaState.` 5、`MessageFeed.` 33、`AppFlow.` 17、`RunFlow.` 1 | `Docs/QA/Evidence/REFACTOR-M2/grep-legacy-state.txt` |
| AC-2 REFACTOR ×3，每次 10/10、fail=0、blocked=0、infra=0、exit=0 | **通过** | 套件全部 **10** 条（原 9 条 + 新增 `Refactor.StateFacade`） | run1 10/0/0/0/exit0；run2 10/0/0/0/exit0；run3 10/0/0/0/exit0；run4（EOL 归一化后复跑）10/0/0/0/exit0 | `Docs/QA/Evidence/REFACTOR-M2/editor_run1..4/` |
| AC-3 SPEC01 ×2，每次 12/12、fail=0、exit=0 | **通过** | SPEC01 全 12 条 | run26 12/0/0/0/exit0；run27 12/0/0/0/exit0；run28（EOL 归一化后复跑）12/0/0/0/exit0 | `Docs/QA/Evidence/SPEC-01/editor_run26..28/` |
| AC-4 新旧 API 双向一致、事件按原语义触发、暂停/timeScale 一致 | **通过** | `Refactor.StateFacade`（10 条断言中的第 10 条） | 6 步全 True：`gold`(7↔3)、`level`(4↔6)+`LevelCost`=300=`GameData.LevelCost()`、`message` 一致且 `Changed` **恰好 1 次**、退订后不再增加（1→1）、`GameStarted` 双向一致并还原、`IsWiped` 两侧均 false、`Pause`→`IsPaused=true && timeScale=0`、`Resume`→`false && 1`、收尾 `restored=True`（金币/等级/文案/开局标志还原，`timeScale==1`） | `REFACTOR-M2/editor_run1..4/REFACTOR_Assertions.jsonl` 末行 |
| AC-5 `git status` 只含本单声明文件 + 派单前既有脏项 | **通过** | — | 条目 **86** 项 = 生产脚本 **33**（23 改 + 5 新 `.cs` + 5 新 `.meta`）+ 测试 **2** + `REFACTOR-M2` 证据 **24** + `SPEC-01` 证据 **15** + 本报告 **1** + 派单前既有 **11**（2 个 `Assembly-CSharp*.csproj`、`Docs/Handoff/TASK_REFACTOR-M2.md`、`DIAG-SKILL/editor_run3/` 4 项、`FND-00B-06/editor_run4/` 4 项）；分类脚本判定 `unexpected = 0` | `REFACTOR-M2/git-diff-stat.txt` |
| AC-6 三个适配层成员签名/可写性与 M1 一致（测试引用未改仍能编译） | **通过** | 编译 + diff 审查 | `GameData` 11 个公开成员一一对应（4 字段→`get+set` 属性、`OnChanged`→`add/remove`、5 方法同签名）；`GameFlow` 4 个（`instance`/`GameStarted` 可写/`IsWiped` 外部只读——与 M1 `private set` 对外等价）；`PauseManager` 5 个（`instance`/`IsPaused` 外部只读/`Pause`/`Resume`）；`Assets/_Project/Tests` 仅改 REFACTOR 两个文件（+1/-1、+133/-2），其余测试文件零改动且编译通过 | `REFACTOR-M2/adapter-api-diff.txt` |

- **运行次数**：REFACTOR **4** 次（≥N=3；每次 10/10 完全一致）；SPEC01 **3** 次（≥2；每次 12/12）。
  - run1–run3 / run26–run27 为正式验收；run4 与 run28 是在**仅换行符归一化（新脚本与三个适配层 LF→CRLF，语义零变化）**后补跑，用于证明“交付字节就是被测字节”。
- **未运行项**：DIAG 套件**未运行** —— 派单 L1 只指定 `REFACTOR ×3` 与 `SPEC01 ×2`；本里程碑未新增/修改任何 DIAG 断言，已知红 `Diag.Input.FirstExit` 不受影响（§7）。
- 修复轮次使用：编译阶段 1 次红转绿（新增断言方法没有 `yield` 导致 `CS0161`，补一行 `yield break;` 即通过），**未动用**本里程碑的 1 轮修复预算，测试侧零修改。没有为了让测试变绿而改断言/阈值/期望值或删除/跳过断言。

### 逐次实测（来自各 run 的 Summary / ExitCode）

| run | 套件 | pass | fail | blocked | infraFail | exit |
|---|---|---|---|---|---|---|
| editor_run1 | REFACTOR | 10 | 0 | 0 | 0 | 0 |
| editor_run2 | REFACTOR | 10 | 0 | 0 | 0 | 0 |
| editor_run3 | REFACTOR | 10 | 0 | 0 | 0 | 0 |
| editor_run4（EOL 后复跑） | REFACTOR | 10 | 0 | 0 | 0 | 0 |
| editor_run26 | SPEC01 | 12 | 0 | 0 | 0 | 0 |
| editor_run27 | SPEC01 | 12 | 0 | 0 | 0 | 0 |
| editor_run28（EOL 后复跑） | SPEC01 | 12 | 0 | 0 | 0 | 0 |

## 3. 实现原理（面向所有者）

1. **根因 / 目标**：改动前 `GameData` 一个静态类同时装三种生命周期完全不同的状态（金币/击杀=局内，等级/升级=成长，提示=瞬时），`GameFlow` 一个 MonoBehaviour 同时管“场景·开局·暂停·重开”和“团灭判定”，`PauseManager` 又是另一处 `timeScale` 写入口。任何一处“重置”都要跨三个类读代码才能确认。目标是把状态与流程**按生命周期归属**拆开，并让旧 API 继续可用，从而在不改玩法的前提下把“谁负责什么时候清什么”变成可检查的契约。

2. **备选方案与为什么这样选**：
   - A（删掉 `GameData`/`GameFlow`/`PauseManager`，全仓改引用含测试）：最“干净”，但必须改测试引用，直接违反本单 R-5「测试里对旧 API 的引用不得删改」，且断点太多、无法回滚到“只拆一类状态”。
   - C（只加注释/`#region` 分区，不拆类）：没有可验证的归属边界，等于没做。
   - **B（选定）：新增 5 个类承载真实状态，旧三类退化为纯转发适配层**。好处是测试零改动、行为可证不变、可以“一次只拆一种状态”；代价是多一层转发（无运行时开销意义，只有阅读成本，已在类注释里标明“新代码用新类”）。

3. **实际数据流**：
   `MonsterBrain/BossBrain/Chest/InventoryUI → RunState.AddGold/AddKill →（HUD.Update 每帧轮询读数）`
   `HUD → MetaState.TryLevelUp → MeleeCombat.dmgMult / UnitHealth.AddMaxHp + MessageFeed.Changed`
   `各系统 → MessageFeed.SetMessage →（HUD.Update 轮询文案）`
   `EscMenuPanel → AppFlow.Pause/Resume → Time.timeScale`
   `GameFlow.Awake → instance；GameFlow.Start → RunFlow.EnterScene →（有玩家队伍）AppFlow.GameStarted=true + timeScale=1`
   `GameFlow.Update → RunFlow.Tick → UnitRegistry.OfTeam(Player) 逐个体检 → 全灭：IsWiped=true + timeScale=0 + MessageFeed.SetMessage；按 R：AppFlow.ReloadActiveScene`
   与 TASK §2 契约一致：类名/成员/语义照单实现；差异全部列在第 6 条（3 处静态化补偿 + 2 个计划外调用点 + 1 个 internal 小工具）。

4. **关键代码位置（5 条）**：
   - `Assets/_Project/Scripts/Core/RunState.cs:6` —— 局内状态：`Gold`/`Kills` 两个 `get;set;` 属性（默认 0，负值不裁剪）；`:12` `AddGold` 只加钱**不**触发消息事件，`MetaState` 与 `HUD` 各自读同一个值。
   - `Assets/_Project/Scripts/Core/MetaState.cs:13` —— `TryLevelUp` 与原 `GameData.TryLevelUp` 逐行等价（`Gold < LevelCost()` 返回 false；扣 `50*Level`；`Level++`；`sw.dmgMult += 0.25f`；`uh.AddMaxHp(20f)`；最后 `MessageFeed.RaiseChanged()` 等价于原 `OnChanged?.Invoke()`）。
   - `Assets/_Project/Scripts/Core/AppFlow.cs:30` —— `ReloadActiveScene()`：原 `GameFlow.Update` 的 R 三行 + `EscMenuPanel.OnClickRestart` 的三行收编，**并额外显式 `IsPaused = false`**（静态化补偿，见第 6 条）。
   - `Assets/_Project/Scripts/Core/RunFlow.cs:12` —— `EnterScene()`：`IsWiped=false`、`GameStarted=false`、有玩家队伍则开局 + `timeScale=1`（**不清**金币/等级/击杀/提示，保持跨场景重载保留）；`:27` `Tick()` 与 `GameFlow.Update` 逐行等价（含 `DownedRescue.IsDown` 不计“站立”的口径）。
   - `Assets/_Project/Tests/REFACTOR/Runtime/REFACTOR_ProbeRunner.cs:908` —— 新增第 10 条 `AssertStateFacade()`：6 步双向校验，`try/finally` 还原全部写入；`:173` 接入执行序列（在 `Refactor.RegistrySemantics` 之后、收尾之前）。

5. **边界 / 失败模式**：
   - **事件重复订阅**：`GameData.OnChanged` 改为 `add/remove` 转发到 `MessageFeed.Changed`，语义与 `event` 一致（重复 `+=` 仍会重复触发——与原实现相同）；`MessageFeed.SetMessage` 即使文案相同也触发（原语义），`GameData.message = x` 直接赋值**不**触发（原字段语义），两者都保留。
   - **`GameData.AddGold` 仍触发 `Changed`**（走 `MessageFeed.RaiseChanged()`），而新契约 `RunState.AddGold` 按 §2 明确“不触发消息事件”。当前仓库没有任何 `OnChanged` 订阅者，HUD 靠轮询，因此产品路径无可观测差异；这是刻意的语义分工，已写进注释。
   - **静态化后的生命周期**：`IsWiped`/`IsPaused` 原先是实例状态，场景重载即归零；改为静态后必须显式复位，否则“Esc 菜单重开后全队操作失灵”（`IsPaused` 残留 true）或“R 重开后立刻又被判定团灭”（`IsWiped` 残留 true）。这两处复位就是第 6 条的补偿，均有注释与 REPORT 留痕。
   - **无 PauseManager 的场合**：原先调用点写的是 `PauseManager.instance != null && ....IsPaused`，缺实例时恒为 false；现在 `AppFlow.IsPaused` 是进程级标志，只有 `Pause()` 会置 true，且 `ReloadActiveScene()` 复位，当前场景组合下等价。
   - **字段变属性的可见风险**：任何对 `GameData.gold`/`GameFlow.GameStarted` 取 `ref`/`out`/指针的写法会编译失败。全仓 grep 确认没有（测试只读写），编译零错误。

6. **与计划的差异及原因**（全部为“行为不变”所必需，无玩法改动）：
   - **① 工单 §2 的“修改文件（调用点）”表漏了 2 个文件**：`UI/MainMenuPanel.cs`（`GameFlow.GameStarted = true`）与 `UI/UIManager.cs`（`GameFlow.GameStarted` + `GameFlow.instance.IsWiped`）。AC-1 要求 `Scripts/**` 计数 = 0，不迁移这两处就无法过 AC-1；两处都是单行等价替换，已迁移并在 §4「计划外调用点」单列。表内 18 个 + 这 2 个 = **实测 20 个调用点文件**（工单 §4 写的“21 个”= 18 调用点 + 3 适配层，实际是 20 + 3 = 23 个生产脚本）。
   - **② `AppFlow.ReloadActiveScene()` 增加 `IsPaused = false`**：契约只写三行，但 `IsPaused` 静态化后不会随旧 `PauseManager` 实例消失；Esc 菜单打开时就 `Pause()`，点“重新开始”时不会走 `OnClose`，若不复位，新场景里所有“暂停即 return”的输入守卫（`PlayerCommandController`/`SelectionManager`/`CharacterBrain`/`MonsterBrain`/`BossBrain`/`SkillInputController`/`DownedRescue`）会永久失效。复位后与“新场景新实例 `IsPaused=false`”完全等价。
   - **③ `RunFlow.EnterScene()` 增加 `IsWiped = false`**：同理，`IsWiped` 静态化后 R 重开会残留 true，导致重开后 `UIManager.gameOn=false` 且 `Tick` 立刻再次进入团灭分支。放在“进入场景”处复位，等价于原实例字段默认 false。
   - **④ `EscMenuPanel.OnClickRestart` 由三行改为 `AppFlow.ReloadActiveScene()`**：这是本单唯一超出“只替换引用”的改动，原因同 ②（否则无法复位静态暂停标志）。TASK §2 契约本就提供 `ReloadActiveScene()` 作为“R 重开”的统一入口，这里复用它消除两处重复。
   - **⑤ `MessageFeed` 增加 `internal static void RaiseChanged()`**：`GameData.AddGold` 与 `MetaState.TryLevelUp` 需要“触发事件但**不**改文案”，契约只给了 `SetMessage`；用一个 `internal` 小工具保持可见性不外扩（不在公共契约里增加成员语义）。

7. **三句话版本**（答辩先讲这三句）：
   - 以前“金币/等级/击杀/提示”全挤在一个 `GameData` 静态类里，“场景/暂停/重开”和“团灭判定”全挤在一个 `GameFlow` MonoBehaviour 里，谁在什么时候该清什么，只能靠读代码猜。
   - 现在三种状态各有归属（`RunState`/`MetaState`/`MessageFeed`），两套流程各有宿主（`AppFlow` 管场景与时间、`RunFlow` 管局内结果），旧 `GameData`/`GameFlow`/`PauseManager` 退化成纯转发适配层，测试引用一行没改。
   - 行为有没有变不靠肉眼：REFACTOR 新增第 10 条断言做新旧 API 双向对账 + 事件恰好一次 + `timeScale` 对齐，套件 10/10 连跑 4 次、SPEC01 12/12 连跑 3 次，`Scripts/**` 的旧 API 引用计数归零。

## 4. 受影响模块导航

| 模块 | 职责 | 入口（类 / 方法 / 场景对象） |
|---|---|---|
| 局内状态 | 金币 / 击杀（跨场景重载保留，不自动重置） | `RunState`（`Scripts/Core/RunState.cs`）：`Gold`/`Kills`/`AddGold`/`AddKill` |
| 成长状态 | 等级 / 升级费用 / 升级收益 | `MetaState`（`Scripts/Core/MetaState.cs`）：`Level`/`LevelCost`/`TryLevelUp`（调用方 `HUD.Update` 按 U） |
| 提示消息 | 单条文案 + 变更事件 | `MessageFeed`（`Scripts/Core/MessageFeed.cs`）：`Message`/`SetMessage`/`Changed` |
| 应用流程 | 开局标志 / 暂停 / 重开 / 队伍存在性；`timeScale` 唯一写入口 | `AppFlow`（`Scripts/Core/AppFlow.cs`）：`GameStarted`/`IsPaused`/`Pause`/`Resume`/`ReloadActiveScene`/`AnyPlayerTeam` |
| 局内结果 | 团灭判定 + R 重开 | `RunFlow`（`Scripts/Core/RunFlow.cs`）：`IsWiped`/`EnterScene`/`Tick`，唯一驱动方 = 场景里的 `GameFlow` 组件 |
| 适配层 1 | 旧状态 API 全量转发 | `GameData`（字段→`get;set` 属性，`OnChanged`→`add/remove`） |
| 适配层 2 | 旧流程宿主（场景组件位置不变） | `GameFlow`（`instance`/`GameStarted`/`IsWiped`，`Awake/Start/Update` 转发） |
| 适配层 3 | 旧暂停入口（场景组件位置不变） | `PauseManager`（`instance`/`IsPaused`/`Pause`/`Resume`） |
| 消费方（20 个生产文件） | HUD 读数、掉落加钱、提示文案、暂停守卫、升级、重开 | `WaveSpawner`、`MeleeCombat`、`SkillSystem`、`SkillEffects`、`SkillEffectFireball`、`MonsterBrain`、`BossBrain`、`DownedRescue`、`CharacterBrain`、`SkillInputController`、`PlayerCommandController`、`SelectionManager`、`DungeonDoor`、`Chest`、`HUD`、`CursorManager`、`InventoryUI`、`EscMenuPanel`、`MainMenuPanel`、`UIManager` |
| 行为护栏 | 9 条原有 + 第 10 条状态门面断言 | `REFACTOR_ProbeRunner`（`Tests/REFACTOR/Runtime`），菜单 `Tools/QA/In-Editor/REFACTOR` |

数据流：`怪物/Boss/宝箱/背包 → RunState.AddGold/AddKill → HUD.Update 轮询`；`HUD → MetaState.TryLevelUp → dmgMult/AddMaxHp`；`各系统 → MessageFeed.SetMessage → HUD 轮询`；`EscMenuPanel → AppFlow.Pause/Resume → Time.timeScale`；`GameFlow(Start/Update) → RunFlow.EnterScene/Tick → UnitRegistry.OfTeam(Player)`

### 影响面清单（§3.1 闸门要求，开工前已输出并获预批）

| 项 | 内容 | 实际落地 |
|---|---|---|
| 新增文件 | `Scripts/Core/{RunState,MetaState,MessageFeed,AppFlow,RunFlow}.cs` | 已新增 5 个；Unity 自动生成 5 个 `.meta`（guid 见 `git-diff-stat.txt` 第 4 段），未手改 |
| 修改文件（适配层） | `Core/GameData.cs`、`Core/GameFlow.cs`、`Core/PauseManager.cs` | 已改薄，公开成员数与 M1 一致（11 / 4 / 5） |
| 修改文件（调用点） | 工单表列 18 个 | 已改 18 个 + 2 个计划外（`MainMenuPanel`、`UIManager`，见下） |
| 修改符号 | 只替换引用 | `GameData.X` → `RunState.X`/`MetaState.X`/`MessageFeed.X`；`PauseManager.instance.IsPaused` → `AppFlow.IsPaused`；`GameFlow.GameStarted` → `AppFlow.GameStarted`；`GameFlow.instance.IsWiped` → `RunFlow.IsWiped` |
| 字符串引用 | 无新增、无改动 | 未改 Animator 参数名 / `SendMessage` / `GameObject.Find` / 提示文案文本（逐条对比见迁移表“替换后”列） |
| 场景 / 预制体 / Animator / `.asset` / `ProjectSettings` / `Packages` | **零改动** | `git status` 无相关条目；`GameFlow`/`PauseManager` 组件仍在原物体、脚本路径不变（组件不重挂，序列化字段零变化） |
| `.meta` | 仅 5 个新脚本自带 | 仅 5 个（Unity 生成） |
| 测试文件 | `REFACTOR_BatchAcceptance.cs`（`DefaultMilestone`）+ `REFACTOR_ProbeRunner.cs`（第 10 条） | 已改；`git diff --numstat` = `1/1` 与 `133/2`，被删 2 行仅为“9 条断言”标题与 blocked id 数组行 |
| 预算 | TASK §4 “文件数 ≤ 28” | 实际 `.cs` 文件 **30** 个 = 修改 23（20 调用点 + 3 适配层）+ 新增 5 + 测试 2；超出 2 个的来源正是「计划外调用点」`MainMenuPanel`/`UIManager`（`.meta`、证据、REPORT 不计入） |

### 调用点迁移表（迁移前 `0a1a45d7` → 迁移后工作区，行号实测）

| # | 文件 | 迁移前（行:旧引用） | 迁移后（行:新引用） | 替换 |
|---|---|---|---|---|
| 1 | `Combat/MeleeCombat.cs` | `139` `GameData.level` | `139` `MetaState.Level` | 等级 |
| 2 | `Units/SkillSystem.cs`（注：不在 `Combat/`） | `40,41,48` `GameData.SetMessage` | `40,41,48` `MessageFeed.SetMessage` | 提示 |
| 3 | `Combat/SkillEffects.cs` | `27,29,31` `GameData.SetMessage` | `27,29,31` `MessageFeed.SetMessage` | 提示 |
| 4 | `Combat/SkillEffectFireball.cs` | `26,28,30` `GameData.SetMessage` | `26,28,30` `MessageFeed.SetMessage` | 提示 |
| 5 | `Units/MonsterBrain.cs` | `98` `GameData.AddGold`、`99` `AddKill`、`118` `PauseManager.instance.IsPaused` | `98` `RunState.AddGold`、`99` `RunState.AddKill`、`118` `AppFlow.IsPaused` | 金币/击杀/暂停 |
| 6 | `Units/BossBrain.cs` | `61` `AddGold`、`62` `AddKill`、`63` `SetMessage`、`70` `PauseManager`、`80` `SetMessage` | `61` `RunState.AddGold`、`62` `RunState.AddKill`、`63` `MessageFeed.SetMessage`、`70` `AppFlow.IsPaused`、`80` `MessageFeed.SetMessage` | 金币/击杀/提示/暂停 |
| 7 | `Units/DownedRescue.cs` | `84,142,170,262,276` `SetMessage`、`182` `PauseManager` | `84,142,170,262,276` `MessageFeed.SetMessage`、`182` `AppFlow.IsPaused` | 提示/暂停 |
| 8 | `Units/CharacterBrain.cs` | `127` `PauseManager` | `127` `AppFlow.IsPaused` | 暂停 |
| 9 | `Player/SkillInputController.cs` | `43` `PauseManager`、`99,113` `SetMessage`、`171` `message`、`173` `message`+`SetMessage` | `43` `AppFlow.IsPaused`、`99,113` `MessageFeed.SetMessage`、`171` `MessageFeed.Message`、`173` `MessageFeed.Message`+`SetMessage` | 暂停/提示 |
| 10 | `Player/PlayerCommandController.cs` | `23` `PauseManager` | `23` `AppFlow.IsPaused` | 暂停 |
| 11 | `Player/SelectionManager.cs` | `77` `PauseManager` | `77` `AppFlow.IsPaused` | 暂停 |
| 12 | `System/DungeonDoor.cs` | `23` `SetMessage` | `23` `MessageFeed.SetMessage` | 提示 |
| 13 | `System/Chest.cs` | `73` `AddGold`、`74` `SetMessage` | `73` `RunState.AddGold`、`74` `MessageFeed.SetMessage` | 金币/提示 |
| 14 | `Core/WaveSpawner.cs` | `39,69` `SetMessage` | `39,69` `MessageFeed.SetMessage` | 提示 |
| 15 | `UI/HUD.cs` | `20` `gold`+`LevelCost`、`22` `level`+`kills`、`26,28` `message`、`46` `TryLevelUp`、`47` `SetMessage`+`level`、`49` `SetMessage` | `20` `RunState.Gold`+`MetaState.LevelCost`、`22` `MetaState.Level`+`RunState.Kills`、`26,28` `MessageFeed.Message`、`46` `MetaState.TryLevelUp`、`47` `MessageFeed.SetMessage`+`MetaState.Level`、`49` `MessageFeed.SetMessage` | 金币/等级/击杀/提示/升级 |
| 16 | `UI/CursorManager.cs` | `70` `SetMessage` | `70` `MessageFeed.SetMessage` | 提示 |
| 17 | `UI/InventoryUI.cs` | `123,133` `SetMessage`、`132` `AddGold` | `123,133` `MessageFeed.SetMessage`、`132` `RunState.AddGold` | 提示/金币 |
| 18 | `UI/EscMenuPanel.cs` | `12` `Pause`、`17` `Resume`、`28` `GameFlow.GameStarted=false` | `12` `AppFlow.Pause()`、`17` `AppFlow.Resume()`、`30` `AppFlow.ReloadActiveScene()`（原三行合并，见 §3.6-④） | 暂停/重开 |
| 19 | `UI/MainMenuPanel.cs`（**计划外**） | `16` `GameFlow.GameStarted = true` | `16` `AppFlow.GameStarted = true` | 开局标志 |
| 20 | `UI/UIManager.cs`（**计划外**） | `46-47` `GameFlow.GameStarted` + `GameFlow.instance.IsWiped`（2 行） | `46` `AppFlow.GameStarted && !RunFlow.IsWiped`（1 行） | 开局/团灭 |
| 21 | `Core/GameFlow.cs`（适配层） | `42` `GameFlow.GameStarted=false`、`62` `GameData.SetMessage` | 已无旧引用；`Start→RunFlow.EnterScene`、`Update→RunFlow.Tick`、`GameStarted`/`IsWiped` 转发 | — |
| 22 | `Core/GameData.cs`（适配层） | `11,13,15,26` 事件/字段/方法 | 全部转发（见 `adapter-api-diff.txt`） | — |

**计数对账（口径留痕）**：
- 基线 `git grep -E 'GameData\.|PauseManager\.instance|GameFlow\.GameStarted|GameFlow\.instance' 0a1a45d7 -- Assets/_Project/Scripts` = **56 行 / 21 文件**。
- 其中 `Core/GameFlow.cs` 自身 2 行（AGENTS/AC-1 明确排除适配层），**生产调用点 = 54 行 / 20 文件**。
- 迁移后生产调用点 = **53 行 / 20 文件**：少 1 行是 `UIManager` 的两行表达式被合成一行；**引用条数**未减少（`UIManager` 那行同时含 `AppFlow.GameStarted` 与 `RunFlow.IsWiped`）。
- 工单 §4 写“21 个调用点/核心脚本”= §2 表 18 个调用点文件 + 3 个适配层；实测是 **20 个调用点文件 + 3 个适配层 = 23 个生产脚本**。差异来源 = 表里漏列 `MainMenuPanel` 与 `UIManager`（未逐行核对），本单**没有**为凑数字拆/并任何调用点。

### 计划外调用点（不在 §2 表内，实际已迁移）

| 文件 | 处数 | 原引用 | 为什么必须迁移 | 风险 |
|---|---|---|---|---|
| `Assets/_Project/Scripts/UI/MainMenuPanel.cs` | 1（`:16`） | `GameFlow.GameStarted = true` | AC-1 要求 `Scripts/**` 旧引用 = 0；不迁移则 AC-1 必红。语义与 `EscMenuPanel` 完全同类 | 单行等价替换 |
| `Assets/_Project/Scripts/UI/UIManager.cs` | 2 行（`:46-47`） | `GameFlow.GameStarted`、`GameFlow.instance == null || !GameFlow.instance.IsWiped` | 同上；且 `GameFlow.instance` 判空在新模型下恒真（`RunFlow.IsWiped` 是静态值） | 表达式合并为 `AppFlow.GameStarted && !RunFlow.IsWiped`，真值表等价（GameFlow 实例在 DungeonWorld 恒存在） |

- 另有 **测试侧**大量旧引用（`SPEC01`/`FND00B`/`FND02A` 等探针），按 R-5 与派单要求**一律未改**（它们就是适配层的存在理由）。

### 改动文件清单（与 TASK §4 允许改动逐项比对）

| 文件 | 改了什么 | 在允许范围 |
|---|---|---|
| `Scripts/Core/RunState.cs` | 新增：`Gold`/`Kills`/`AddGold`/`AddKill` | ✅ 新增允许 |
| `Scripts/Core/MetaState.cs` | 新增：`Level=1`/`LevelCost`/`TryLevelUp`（逐行等价原实现） | ✅ |
| `Scripts/Core/MessageFeed.cs` | 新增：`Message`/`Changed`/`SetMessage`/`internal RaiseChanged` | ✅ |
| `Scripts/Core/AppFlow.cs` | 新增：`GameStarted`/`IsPaused`/`Pause`/`Resume`/`ReloadActiveScene`/`AnyPlayerTeam` | ✅ |
| `Scripts/Core/RunFlow.cs` | 新增：`IsWiped`/`EnterScene`/`Tick` | ✅ |
| 上述 5 个 `.cs.meta` | Unity 自动生成 | ✅ 新增脚本自带 |
| `Scripts/Core/GameData.cs` | 改薄：4 字段→转发属性、`OnChanged`→`add/remove`、6 方法转发（17+/23-） | ✅ |
| `Scripts/Core/GameFlow.cs` | 改薄：保留 `instance`/`GameStarted`/`IsWiped`，`Awake/Start/Update` 转发（8+/46-） | ✅ |
| `Scripts/Core/PauseManager.cs` | 改薄：`IsPaused`/`Pause`/`Resume` 转发（7+/8-） | ✅ |
| `Scripts/Core/WaveSpawner.cs` | 2 处 `SetMessage` → `MessageFeed` | ✅ 表内 |
| `Scripts/Combat/MeleeCombat.cs` | 1 处 `GameData.level` → `MetaState.Level` | ✅ 表内 |
| `Scripts/Combat/SkillEffects.cs`、`SkillEffectFireball.cs` | 各 3 处 `SetMessage` | ✅ 表内 |
| `Scripts/Units/SkillSystem.cs` | 3 处 `SetMessage` | ✅ 表内 |
| `Scripts/Units/MonsterBrain.cs`、`BossBrain.cs`、`DownedRescue.cs`、`CharacterBrain.cs` | 金币/击杀/提示/暂停守卫 | ✅ 表内 |
| `Scripts/Player/SkillInputController.cs`、`PlayerCommandController.cs`、`SelectionManager.cs` | 暂停守卫 + 提示 + 文案读取 | ✅ 表内 |
| `Scripts/System/DungeonDoor.cs`、`Chest.cs` | 提示 / 金币 | ✅ 表内 |
| `Scripts/UI/HUD.cs`、`CursorManager.cs`、`InventoryUI.cs`、`EscMenuPanel.cs` | 读数/提示/升级/暂停/重开 | ✅ 表内 |
| `Scripts/UI/MainMenuPanel.cs`、`UIManager.cs` | 开局标志 / 团灭判定 | ⚠️ **计划外**（见上表，为过 AC-1 必需） |
| `Tests/REFACTOR/Editor/REFACTOR_BatchAcceptance.cs` | `DefaultMilestone` `"M1"`→`"M2"`（1/1） | ✅ |
| `Tests/REFACTOR/Runtime/REFACTOR_ProbeRunner.cs` | 新增第 10 条 `AssertStateFacade` + 接入 + 头注释 9→10 条 + blocked id 加 1（133+/2-；**既有 9 条零改动**） | ✅ |
| `Docs/Handoff/REPORT_REFACTOR-M2.md` | 本报告 | ✅ |
| `Docs/QA/Evidence/REFACTOR-M2/**` | 3 份证据 + 4 次 run + 迁移清单 | ✅ |
| `Docs/QA/Evidence/SPEC-01/editor_run26..28/**` | SPEC01 证据 | ✅ |

**未改动**：`Assets/_Project/Art/**`、`Assets/Scenes/**`、`Assets/_Project/Prefabs/**`、`*.asset`、`ProjectSettings/**`、`Packages/**`、`AGENTS.md`、`Docs/Handoff/CURRENT.md`、其他 `TASK_*`/`REPORT_*`、`Tests/**` 里除 REFACTOR 套件外的任何文件、现有 9 条 REFACTOR 与 12 条 SPEC01 断言口径。

## 5. 证据清单

| 文件 | 内容 |
|---|---|
| `Docs/QA/Evidence/REFACTOR-M2/editor_run1..4/REFACTOR_Assertions.jsonl` | 逐条断言（每次 10 行，末行 `Refactor.StateFacade`，含 `expected/actual/data.steps`） |
| `Docs/QA/Evidence/REFACTOR-M2/editor_run1..4/REFACTOR_Summary_run1.json` | 每次汇总（pass=10 / fail=0 / blocked=0 / infraFail=0 / milestone=M2 / head=0a1a45d7） |
| `Docs/QA/Evidence/REFACTOR-M2/editor_run1..4/REFACTOR_ExitCode_run1.txt` | 每次退出码（`0`） |
| `Docs/QA/Evidence/REFACTOR-M2/editor_run1..4/InEditorResult.txt` | 编辑器内收尾回执（`code=0`，含 env 清理快照） |
| `Docs/QA/Evidence/REFACTOR-M2/editor_run1..4/REFACTOR_Runs.jsonl` | 跨 run 索引 |
| `Docs/QA/Evidence/REFACTOR-M2/grep-legacy-state.txt` | AC-1：正则、范围、排除文件、基线 56 行/21 文件（A）、当前全量 1 行注释（B）、排除适配层后 **0**（C）、新契约使用点计数（D） |
| `Docs/QA/Evidence/REFACTOR-M2/git-diff-stat.txt` | AC-5：`git status --porcelain -uall`、`git diff --stat`、`--numstat`、5 个新脚本 `.meta` guid、期望/实际分类（unexpected=0） |
| `Docs/QA/Evidence/REFACTOR-M2/adapter-api-diff.txt` | AC-6：三个适配层 `0a1a45d7` vs 工作区公开成员逐行对照 + 成员级可写性判定 + 测试目录 diff 仅 REFACTOR 两文件 |
| `Docs/QA/Evidence/REFACTOR-M2/migration-lines.txt` | 迁移后新 API 使用点逐行清单（53 行，供迁移表复核） |
| `Docs/QA/Evidence/SPEC-01/editor_run26..28/` | SPEC01 12/12 证据（Summary/Assertions/ExitCode/InEditorResult/Runs） |

复跑方式：编辑器内菜单 `Tools/QA/In-Editor/REFACTOR`、`Tools/QA/In-Editor/SPEC01 (green)`（模式 B / `unityMCP`，**未**使用 `run_tests`、**未**使用 batchmode）；证据目录由 `QA_InEditorMenu.NextSubdir` 自动递增，只追加不覆盖。grep 证据可用 `grep-legacy-state.txt` 第 5–9 行给出的正则/范围/排除项原样复现。

## 6. 学习讲稿

不适用 —— M2 是纯重构里程碑（非 feature、非高风险：不碰场景/预制体/Animator/`.asset`/`ProjectSettings`）；TASK §5 规定 M4 收口后统一写重构讲稿。

## 7. 未决项

- **需大脑/所有者确认（1 项）**：工单 §2 调用点表漏列 `UI/MainMenuPanel.cs`、`UI/UIManager.cs`，本单为满足 AC-1（旧引用 = 0）已迁移这 2 个文件（各 1 处 / 2 行）并单列在 §4「计划外调用点」。若判定越界，回滚方式 = 把这 2 处改回旧 API（会立刻使 AC-1 变红）。
- **刻意留给所有者的语义变更（未实现）**：`RunState`/`MetaState`/`MessageFeed` **没有**新增任何自动重置，金币/等级/击杀/提示仍**跨场景重载保留**（与重构前一致）。若希望“R 重开清空局内金币/击杀”，需所有者决策（TASK §2 明确要求本单不实现）。
- **可观测性副作用**：`GameData.AddGold` 仍触发 `Changed`（保持旧语义），`RunState.AddGold` 不触发；当前无 `OnChanged` 订阅者，产品路径无差异。若未来给 `MessageFeed.Changed` 加订阅者并期望“金币变化也收到通知”，要显式改用 `GameData.AddGold` 或自行 `RaiseChanged`。
- **两处静态化补偿**（`AppFlow.ReloadActiveScene` 复位 `IsPaused`、`RunFlow.EnterScene` 复位 `IsWiped`）不在 TASK §2 的契约文本里，是“行为不变”的必要条件，理由见 §3.3/§3.6-②③；请大脑在验收时确认接受。
- **DIAG 套件未运行**：派单 L1 只要求 REFACTOR×3 + SPEC01×2；已知红 `Diag.Input.FirstExit` 预期不变，是否补跑由大脑按计划 §1 第 3 条决定。
- **换行符归一化**：5 个新脚本与 3 个适配层初次写入为 LF，收尾前统一为 CRLF（`.gitattributes` `* text=auto`）；归一化后已复跑 REFACTOR（run4，10/10）与 SPEC01（run28，12/12）确认交付字节可跑。
- **需所有者目检（3 项）**：
  1. 在 Unity 里点一次 `Tools/QA/In-Editor/REFACTOR`，确认证据写进 `REFACTOR-M2/editor_run5`（而非 M1）且不退出编辑器；
  2. 手动试一遍：打怪加金币/击杀数、按 U 升级（等级+1、费用 50×等级、金币不足提示）、Esc 暂停/继续、Esc→“重新开始”后**能正常操作**（这是 `IsPaused` 复位的关键目检点）、全灭后按 R 重开（不应重开即再次团灭）；
  3. 看 GitHub Desktop 改动列表，确认只含 §4 清单文件（`Assembly-CSharp*.csproj`、`Docs/Handoff/TASK_REFACTOR-M2.md`、`Docs/QA/Evidence/DIAG-SKILL/editor_run3/`、`Docs/QA/Evidence/FND-00B-06/editor_run4/` 为**开工前既有脏项**，非本里程碑改动）。

## 8. 建议提交

```text
重构：M2 生命周期拆分（GameData→RunState/MetaState/MessageFeed，GameFlow→AppFlow/RunFlow，PauseManager→AppFlow 看门人；REFACTOR 10/10×4 + SPEC01 12/12×3）
为什么：把局内/成长/提示三类状态与「场景·暂停·重开」「团灭判定」拆到各自有明确归属与重置时机的容器，旧 API 全部保留为转发适配层，脚本侧 54 行旧引用清零而测试引用一行未改；行为由新增的 Refactor.StateFacade（新旧 API 双向对账 + 事件恰好一次 + timeScale 对齐）与既有 12 条 SPEC01 兜底。
```

## 9. 消耗记录

- 钱包：`Go 订阅`
- 模型完整 ID：`opencode-go/deepseek-v4.1-flash`
