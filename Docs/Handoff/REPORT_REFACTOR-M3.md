# REPORT — REFACTOR-M3：统一交互（IInteractionTarget + InteractionRunner）

> 对应 `Docs/Handoff/TASK_REFACTOR-M3.md`。节次与 `Docs/Handoff/workflow-rules.md` §2 一致，未删节。
> 执行环境：Unity 模式 B（编辑器常开 + `unityMCP`），实例 `Dungeon@4f5c34c82f31646e`，场景 `DungeonWorld`，Unity 2022.3.62f3c1。
> 基线 HEAD：`1961c93b`（全部 run 的 `Summary.head` = `1961c93b2b2afd7315fc3a7b51899c14c03a6db9`）。

## 1. 结果摘要

`完成` —— 交互链收敛为「目标侧 `IInteractionTarget` + 单位侧 `InteractionRunner`」；宝箱/救援/门不再自己读 `Input` / 查 `SelectionManager` / 调 `UIManager`（grep 去注释后命中 0）；未动场景/预制体/Animator/`.asset`/`ProjectSettings`；REFACTOR 11/11 ×4、SPEC01 12/12 ×3，全 fail=0 / blocked=0 / infra=0 / exit=0。

## 2. AC 回填表

| AC | 结果 | 断言 ID | 关键数据 | 日志路径 |
|---|---|---|---|---|
| AC-1 | 通过 | `Refactor.InteractionNoSelfInput`（grep 证据） | 正则 `Input\.|SelectionManager|UIManager`；三文件原始命中 2、**去注释后命中 0**（2 条均为 `///` 文档注释：Chest.cs:6、DungeonDoor.cs:6）；DownedRescue.cs 0 命中 | `Docs/QA/Evidence/REFACTOR-M3/grep-interaction.txt` |
| AC-2 | 通过 | 套件全部 11 条 | run1/2/3 均 **pass=11 fail=0 blocked=0 infra=0 exit=0**；另有 run4（换行符归一化后复跑）11/11 | `Docs/QA/Evidence/REFACTOR-M3/editor_run1..4/` |
| AC-3 | 通过 | SPEC01 全 12 条 | `editor_run29`、`editor_run30` 均 **pass=12 fail=0 exit=0**；另有 `editor_run31`（归化后复跑）12/12 | `Docs/QA/Evidence/SPEC-01/editor_run29/`、`editor_run30/`、`editor_run31/` |
| AC-4 | 通过 | `Refactor.InteractionContract` | 5 个子步骤全 pass：implements 4/4；contractValues（chest/door Key=false、rescue Key=true；rescue Priority=Rescue，其余 Default）；chestContract（before=true、事件=1、after=false、二次 Interact 仍=1）；doorAvailability（true 且未 Enter）；runnerEnsure（sameInstance=true、componentCount=1） | 同上 JSONL（每次 run 末段一行） |
| AC-5 | 通过 | — | `git status` 只含本单声明文件 + 派单前既有脏项；`git status -- <场景/prefab/.asset/ProjectSettings/Packages/Art>` **输出为空**；`git diff --stat` 中两个 `Assembly-CSharp*.csproj` 为 Unity 生成（开工前已脏，仅新增 3 个新脚本条目） | `Docs/QA/Evidence/REFACTOR-M3/git-diff-stat.txt` |
| AC-6 | 通过 | — | 测试套件 diff：删除行**恰好 3 行**（`DefaultMilestone "M2"→"M3"`、头注释 10→11 条、`Start()` blocked id 数组末项），新增 135 行；**无任何既有断言的函数体/期望值/阈值被删改** | `Docs/QA/Evidence/REFACTOR-M3/test-diff.txt` + `test-diff-u0.txt` |

- **运行次数**：REFACTOR **N=4**（要求 N≥3）、SPEC01 **N=3**（要求 2）；各次数字完全一致（11/11、12/12，fail=0）。第 4 次 REFACTOR 与第 3 次 SPEC01 是「新脚本 LF→CRLF 归一化」之后的复跑，用于证明交付字节可编译可跑（`Library/ScriptAssemblies/Assembly-CSharp.dll` 重建于文件写入之后，控制台 0 error）。
- 未运行项：无（DIAG / FND00B06 不在本单 L1 口径内，未跑，也未用 `run_tests`、未用 batchmode）。

## 3. 实现原理（面向所有者）

1. **根因 / 目标**：「点一下交互物 → 走过去 → 到位执行」这条链原先散落三处——输入层 `SelectionManager` 认 `Interactable`/`DownedRescue` 具体类型、单位 `CharacterBrain` 内联算落点并在 `Update` 里 `GetComponent<Chest>().OpenBy()`、交互物自己 `Update` 读 `Input`/查 `SelectionManager`/写提示（门），宝箱还直接调 `UIManager`。新增一种交互物必须同时改输入层与单位层。本次把「目标做什么」与「单位怎么走过去」拆成接口 + 执行器。
2. **备选方案与为什么没选**：A（每个交互物自带 Runner 组件）必须改预制体、动序列化，禁止；C（只把 `GetComponent<Chest>()` 换成 `switch(type)`）加新交互物仍要改输入层，等于没统一。选 B：接口 + 单位侧运行时 Runner，**零资产改动**，输入层只剩一处分发。
3. **实际数据流**：`SelectionManager（唯一读 Input 的地方：左键点击 / F 键 / 提示轮询）→ 命中物取 IInteractionTarget，按 Priority + IsAvailable 择优 → CharacterBrain.CommandInteract(target) → InteractionRunner.Order（算落点 + CommandMove + 记待办）→ 到位时 CharacterBrain.Update 判 wasMoving && !isMoving → Runner.OnMovedTick（补播 Interact 触发器 + target.Interact(unit)）→ 目标自执行（开箱 / 下达救援指令 / 传送）→ Chest.Opened 事件 → UIManager 开面板`。与 TASK §2 的差异见本节第 6 条。
4. **关键代码位置**：
   - `Assets/_Project/Scripts/Gameplay/Interaction/InteractionRunner.cs:48` —— `Order`：原 `CommandInteract` 的落点算法（方向 × 半径）与 `CommandMove` + 待办记录，技能门禁走 `CharacterBrain.CanAcceptInteract`。
   - `Assets/_Project/Scripts/Gameplay/Interaction/InteractionRunner.cs:84` —— `OnMovedTick`：`wasMoving && !isMoving && 有待办` 时补播 `Interact` 并 `target.Interact(brain)`；先清待办再执行，避免目标内部再次 `Order`（救援就是这种）被覆盖。
   - `Assets/_Project/Scripts/Gameplay/Interaction/InteractionRunner.cs:96` —— `NearestKeyTarget`：只遍历 `KeyInteractable` 目标登记表，用 **3D 距离 ≤ Radius** 判定（与原 `DungeonDoor.Update` 的 `Vector3.Distance` 口径一致），输入层不按具体类型分支。
   - `Assets/_Project/Scripts/Player/SelectionManager.cs:205`/`:215` —— `BestTarget`（按 Priority 择高、跳过不可交互）+ `TryCommandRescue`（对每个选中存活玩家逐个 `target.Interact(brain)`，等价原 `TryCommandRescue`，不再按 `DownedRescue` 类型分支）。
   - `Assets/_Project/Scripts/System/Chest.cs:90`（`Opened` 事件）/ `Assets/_Project/Scripts/UI/UIManager.cs:46`（`OnChestOpened`）—— 宝箱侧零 UI 引用；UIManager 在 `Awake` 订阅、`OnDestroy` 退订，`Contents == null` 时不开面板（与原 `_inv != null` 判据一致）。
   - `Assets/_Project/Scripts/Gameplay/Interaction/InteractionDefinition.cs:26` —— `RadiusFor`：`max(collider.bounds.extents.x, .z) + unit.interactExtraRange`，无碰撞体时兜底 1.5m；三个交互物共用同一份算法。
5. **边界 / 失败模式**：
   - 门提示漏帧 → 输入层每帧 `PollKeyHint()` 非空即 `MessageFeed.SetMessage`（保持“持续显示”观感）；无目标时不写、不清空（同原实现）。
   - 救援多选被削成“只对主选生效” → `TryCommandRescue` 遍历 `_selected` 并对每个存活玩家下令，判据 `IsDown`/`dead` 与原来逐条一致。
   - 到位补播时序被改 → `CharacterBrain.Update` 仍是 `wasMoving && !_moving && 有待办` 才进分支，且本帧 `return` 跳过追击/普攻；`Interact` 触发器名未改。
   - 宝箱面板不开/开两次 → 事件在 `OpenSequence` 末尾**只抛一次**（`_opened` 守卫），UIManager 用 `Awake` 订阅 + `OnDestroy` 退订；第 11 条断言实测“事件恰好 1 次 + 二次 Interact 不再触发”。
   - 打断后白播一轮 Interact → `DownedRescue.CommandRescue` 仍在下单后立刻 `CancelPendingInteract()`，`_channelBlockUntil` 冷却/`ResetChannelTrigger` 一行未改；`Rescue.ChannelInterrupt` 断言原样通过。
6. **与计划的差异及原因**（全部为“语义不变、符号微调”）：
   - `OnMovedTick` 保持 `void`；由 `CharacterBrain.Update` 在 `_runner != null && HasPending && wasMoving && !_moving` 时调用。原因：本帧必须 `return` 才能跳过追击/普攻，若让 Runner 返回布尔就改了契约形状；这样调用点判断与原来内联判断逐字对应（Runner 内再判一次是幂等保护）。
   - 新增 `InteractionRunner.HasPending`（只读）与 `CharacterBrain.CanAcceptInteract` / `Health`（只读属性，不新增序列化字段）：前者给 Update 判断，后者把“技能期间交互无效”的门禁放到 `Order` 一处，兼容入口与新入口共用。
   - 新增 `CommandInteract(IInteractionTarget)` 重载：输入层手上是目标契约而非 `Interactable` 标记；原 `CommandInteract(Interactable)` 保留为兼容入口（`Interactable.Target()` → 新重载）。
   - `InteractionRunner` 内新增 `RegisterKeyTarget/UnregisterKeyTarget` 静态登记表：`PollKeyHint` 需要“范围内最近的可 F 交互目标”，若每帧全场景扫 `MonoBehaviour` 太重、若直接查 `DungeonDoor` 又回到类型分支；改成 `KeyInteractable=true` 的目标自行在 `OnEnable/OnDisable` 登记（当前只有门）。销毁残留由 `as Component == null` 兜底清理。
   - `DownedRescue.CommandRescue` 改用 `InteractionRunner.Ensure(rBrain).Order(this)`，并**去掉**原来隐含的 `_interact != null` 前置条件（原来没有 `Interactable` 就不跑位）。场景内三名玩家都挂 `Interactable`，实际行为不变；这是接口化后的正确语义。
   - 点击分发按 TASK 要求**跳过 `IsAvailable == false` 的目标**：因此点击“已开过的宝箱”不再走过去（原实现会走过去但 `OpenBy` 空转）。属 TASK §2 明列的语义，不是回归。
   - 门提示的刷新时点从“门自己的 Update”移到 `SelectionManager.Update`，后者在 `AppFlow.IsPaused` 时提前 return —— 即**暂停期间提示不再被重写**（原实现暂停时仍会写同一句）。文案与持续显示观感在正常运行下不变。
7. **三句话版本**：
   - 以前“走过去再做什么”这条链在输入层、角色大脑、每个交互物里各写一遍，加一个交互物要改三处；现在目标只实现 `IInteractionTarget`（落点、提示、优先级、还能不能交互、执行），单位侧 `InteractionRunner` 统一负责走过去与到位补播。
   - 输入层只剩 `SelectionManager` 一处：点击按优先级分发（救援先于选中）、F 键与提示轮询都走 Runner，门不再自己读 `Input`、宝箱不再自己调 UI（改抛 `Opened` 事件由 `UIManager` 订阅）。
   - 手感与判定靠“既有 10 条 ASSERT 口径零改动 + 新增第 11 条契约断言 + SPEC01 12 条”兜底，且全程零场景/预制体/Animator/`.asset` 改动（Runner 只在运行时 `AddComponent`）。

## 4. 受影响模块导航

| 模块 | 职责 | 入口（类 / 方法 / 场景对象） |
|---|---|---|
| 交互契约 | 目标侧回答“在哪/能不能/多近/做什么”，点击优先级 | `IInteractionTarget`、`InteractionPriority`（`Scripts/Gameplay/Interaction/IInteractionTarget.cs`） |
| 交互参数 | 落点半径与额外距离的单一算法 + 兜底常量 | `InteractionDefinition.Default` / `InteractionDefinition.RadiusFor`（`InteractionDefinition.cs`） |
| 单位侧执行器 | 算落点、下达移动、记待办、到位补播并执行；F 键与提示查询 | `InteractionRunner.Ensure/Order/CancelPending/OnMovedTick/TryKeyInteract/PollKeyHint`（与 `CharacterBrain` 同物体，运行时挂载） |
| 输入层（唯一读 Input） | 点击判定与优先级分发、救援多选下令、F 键、每帧提示 | `SelectionManager.HandleClick/BestTarget/TryCommandRescue/Update` |
| 角色大脑 | 到位检测时点；保留两个兼容入口 | `CharacterBrain.CommandInteract(Interactable/IInteractionTarget)`、`CancelPendingInteract`、`Update` |
| 宝箱 | 开盖 + 显形 + 抛 `Opened`；不再碰 UI | `Chest`（`Scripts/System/Chest.cs`）、`Chest.Opened` 事件、`Chest.Contents` |
| 倒地救援 | `Priority=Rescue` 的目标；读条/打断/冷却逻辑一行未改 | `DownedRescue`（`Scripts/Units/DownedRescue.cs`）、`CommandRescue` |
| 地牢门 | 唯一 `KeyInteractable` 目标；只保留 `Enter()` | `DungeonDoor`（`Scripts/System/DungeonDoor.cs`） |
| UI | 订阅开箱事件开背包面板 | `UIManager.Awake/OnDestroy/OnChestOpened` |
| 点击命中标记 | 只留 `hint` + `Target()` 解析 | `Interactable`（`Scripts/System/Interactable.cs`） |

数据流：`SelectionManager（点/F）→ IInteractionTarget（Priority/IsAvailable 择优）→ CharacterBrain.CommandInteract → InteractionRunner.Order → CommandMove → CharacterBrain.Update 到位 → Runner.OnMovedTick（Interact 触发器 + target.Interact）→ 目标自执行 → Chest.Opened → UIManager 开面板`

### 4.1 影响面清单（开工前输出，已验证与实际改动一致）

| 项 | 内容 |
|---|---|
| 新增文件 | `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs`、`InteractionDefinition.cs`、`InteractionRunner.cs` |
| 新增符号 | `InteractionPriority`（enum，Default=0/Rescue=10）、`IInteractionTarget`、`InteractionDefinition`（+`RadiusFor`）、`InteractionRunner`（`Ensure/Order/CancelPending/OnMovedTick/TryKeyInteract/PollKeyHint/RegisterKeyTarget/UnregisterKeyTarget/HasPending`）、`Chest.Opened`、`Chest.Contents`、`Interactable.Target()`、`CharacterBrain.Health`、`CharacterBrain.CanAcceptInteract`、`CharacterBrain.CommandInteract(IInteractionTarget)` |
| 修改符号 | `Chest`（实现接口，`OpenBy` 签名保留、移除 `UIManager` 直调）；`DungeonDoor`（删 `Update`，加接口成员 + `OnEnable/OnDisable` 登记）；`DownedRescue`（实现接口，`CommandRescue` 内部改走 `Runner.Order`）；`CharacterBrain`（`_pendingInteract`→`_runner`，`Update` 到位分支改调 `OnMovedTick`）；`SelectionManager`（`HandleClick`/`TryCommandRescue` 改目标契约、`BestTarget`、Update 内 F 键与提示轮询、`PrimaryBrain`）；`UIManager`（`Awake/OnDestroy` 订阅退订 + `OnChestOpened`） |
| 字符串引用核查 | Animator 触发器 `Interact`/`Interrupt`/`Die`/`Revive` **零改动**（`SetTrigger("Interact")` 只是从 `CharacterBrain.Update` 移到 `InteractionRunner.OnMovedTick`）；`SendMessage` 无新增；`GameObject.Find` 无新增；门提示文案 `"按 F 进入地牢"` 原样迁到 `DungeonDoor.Hint` |
| 场景 / 预制体 / `.asset` | **零改动**（`git status -- *.unity *.prefab *.asset ProjectSettings/* Packages/* Art/*` 输出为空；`InteractionRunner` 只在运行时 `AddComponent`，不落盘；三个交互物的 Inspector 序列化字段一个未改名/未删） |
| `.meta` | Unity 自动生成：`Scripts/Gameplay.meta`、`Scripts/Gameplay/Interaction.meta` + 3 个 `.cs.meta`；未移动/重命名任何已有资产 |
| 测试文件 | `Tests/REFACTOR/Editor/REFACTOR_BatchAcceptance.cs`（`DefaultMilestone` → `"M3"`）；`Tests/REFACTOR/Runtime/REFACTOR_ProbeRunner.cs`（新增第 11 条 + 头注释 + blocked id 数组一项） |

**改动文件清单**（与 TASK §4 允许改动逐项比对）

| 文件 | 改了什么 |
|---|---|
| `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs`（新） | 契约 + 优先级枚举 |
| `Assets/_Project/Scripts/Gameplay/Interaction/InteractionDefinition.cs`（新） | 参数值对象 + 共用半径算法 |
| `Assets/_Project/Scripts/Gameplay/Interaction/InteractionRunner.cs`（新） | 单位侧执行器 + F 交互登记表 |
| `Assets/_Project/Scripts/System/Interactable.cs` | 文档注释更新 + `Target()` 解析目标（`hint` 字段名未动） |
| `Assets/_Project/Scripts/System/Chest.cs` | 实现接口、`Opened` 事件、`Contents`、移除 `UIManager` 直调 |
| `Assets/_Project/Scripts/System/DungeonDoor.cs` | 删自持 `Update`（Input/SelectionManager/提示），实现接口，`Enter()` 逐行未改 |
| `Assets/_Project/Scripts/Units/DownedRescue.cs` | 实现接口；`CommandRescue` 改走 `Runner.Order`；读条/打断/冷却/动画段未改 |
| `Assets/_Project/Scripts/Units/CharacterBrain.cs` | `_pendingInteract`→`InteractionRunner`；两个 `CommandInteract` 入口；到位分支改调 `OnMovedTick`；`Health`/`CanAcceptInteract` |
| `Assets/_Project/Scripts/Player/SelectionManager.cs` | 点击按 Priority/IsAvailable 分发；救援多选逐人下令；F 键；每帧提示轮询 |
| `Assets/_Project/Scripts/UI/UIManager.cs` | 订阅/退订 `Chest.Opened`，回调开背包面板 |
| `Assets/_Project/Tests/REFACTOR/Editor/REFACTOR_BatchAcceptance.cs` | `DefaultMilestone = "M3"` |
| `Assets/_Project/Tests/REFACTOR/Runtime/REFACTOR_ProbeRunner.cs` | 第 11 条 `Refactor.InteractionContract` + 头注释 11 条 + blocked id 一项 |
| `Assembly-CSharp.csproj` / `Assembly-CSharp-Editor.csproj` | Unity 自动重生成（含 3 个新脚本条目）；**开工前已是脏项**，未手改 |

**计划外改动**：无。除上表与证据/报告外，未触碰任何文件（`Art/**`、`Scenes/**`、`Prefabs/**`、`*.asset`、`ProjectSettings/**`、`Packages/**`、`AGENTS.md`、`CURRENT.md`、其他 TASK/REPORT、`Tests/**` 中 REFACTOR 之外的文件均零改动）。交互数值（`rescueTime`/`rescueRange`/`interactExtraRange`/`openAngle`/`openDuration`/`interactRange`）与 `UnitRegistry`/状态容器未动（`git diff` 中无这些字段的增删改）。

## 5. 证据清单

| 文件 | 内容 |
|---|---|
| `Docs/QA/Evidence/REFACTOR-M3/grep-interaction.txt` | AC-1：正则/范围/排除规则/逐文件命中/复现命令（去注释后 0 命中） |
| `Docs/QA/Evidence/REFACTOR-M3/git-diff-stat.txt` | AC-5：HEAD、`git status --porcelain`、`git diff --stat`、场景/预制体/`.asset`/ProjectSettings 空改动核查 |
| `Docs/QA/Evidence/REFACTOR-M3/test-diff.txt` | AC-6：删除行全集（3 行）+ 性质说明 + 复现命令 |
| `Docs/QA/Evidence/REFACTOR-M3/test-diff-u0.txt` | AC-6：原始 `git diff -U0 -- Assets/_Project/Tests/REFACTOR`（165 行） |
| `Docs/QA/Evidence/REFACTOR-M3/runs-summary.txt` | 7 个 run 的 pass/fail/blocked/infra/exit/时间 + 第 11 条实测键值 |
| `Docs/QA/Evidence/REFACTOR-M3/editor_run1..4/REFACTOR_Assertions.jsonl` | 断言级数据（每次 11 行，含第 11 条的 5 个子步骤） |
| `Docs/QA/Evidence/REFACTOR-M3/editor_run1..4/REFACTOR_Summary_run1.json` | 汇总（`milestone=M3`、pass/fail/blocked/infraFail、head、seed） |
| `Docs/QA/Evidence/REFACTOR-M3/editor_run1..4/REFACTOR_ExitCode_run1.txt` | 退出码（4 个均为 `0`） |
| `Docs/QA/Evidence/REFACTOR-M3/editor_run1..4/InEditorResult.txt` | 编辑器内收口记录（task/code/finishedAt/subdir） |
| `Docs/QA/Evidence/SPEC-01/editor_run29|30|31/*` | SPEC01 断言 JSONL / Summary / ExitCode / InEditorResult（均 12/12、exit 0） |

复跑方式：编辑器内菜单 `Tools/QA/In-Editor/REFACTOR`、`Tools/QA/In-Editor/SPEC01 (green)`（模式 B / `unityMCP`，**未**使用 `run_tests`、**未**使用 batchmode）；证据子目录由 `QA_InEditorMenu.NextSubdir` 自动递增，只追加不覆盖。

## 6. 学习讲稿

不适用 —— M3 是纯重构里程碑（非 feature、非高风险：不碰场景/预制体/Animator/`.asset`/`ProjectSettings`）；TASK §5 规定 M4 收口后统一写重构讲稿（三句话版本已写入 §3 第 7 条）。

## 7. 未决项

- **场景内没有 Chest / DungeonDoor 实例（如实说明，非本单造成）**：全工程 `.unity`/`.prefab` 均无 `Chest.cs`（guid `7afbbf731cf09f4478d22ebd9c294352`）与 `DungeonDoor.cs`（guid `0c8860eaa1f7c9b49b27d0ab0edfeb9f`）的引用。因此 TASK §3 里“场景内各取一个实例”的宝箱与门只能用**运行期内存夹具**（`REFACTOR_ContractChest` / `REFACTOR_ContractDoor`，断言结束前 `Destroy`，不写场景），倒地救援取场景真实组件（`Knight_Unit`）。第 11 条断言里已逐项标注 `chestSource`/`doorSource`/`rescueSource`，未伪造“场景实例”。
- **门的 F 交互无法人工目检（需所有者决策）**：当前 `DungeonWorld` 没有 `DungeonDoor` 组件实例，`Tools/QA/In-Editor/*` 也无法覆盖“靠近 → 提示 → 按 F → 黑屏 → 全员传送”。要目检需在场景/预制体挂一个 `DungeonDoor`（含 `dungeonEntryPoint`/`cameraPoint` 引用）——属资产改动，本单禁止。建议由所有者在下一个需要地牢入口的场景里挂上后目检一次。
- **点击分发跳过不可交互目标**：点“已开过的宝箱”不再走过去（TASK §2 明列的语义）。若希望保留“走过去但空转”的旧观感，需要所有者决定。
- **暂停期间的提示刷新**：提示改由 `SelectionManager.Update` 轮询，而它在 `AppFlow.IsPaused` 时提前 return，因此暂停时不再重写同一句提示（正常游玩无差异）。
- **换行符**：3 个新脚本与重写过的 `Interactable.cs` 初次写入为 LF，收尾统一为 CRLF（与仓库内其余 `.cs` 一致）；归一化后已复跑 REFACTOR（editor_run4，11/11）与 SPEC01（editor_run31，12/12）。
- **需所有者目检（3 项）**：
  1. 手动试：选中角色 → 点倒地队友（救援优先、多选同时下令）→ 走位 → 读条 → 受击/移动打断 → 重新下令救起；点宝箱 → 开盖 + 内容物 + 背包面板只开一次；
  2. 打开一个玩家预制体/场景对象，确认 `CharacterBrain` 的 Inspector 字段与三个交互物的字段**没有改名/丢失**（Runner 只应出现在 Play 模式下的临时组件列表里，退出 Play 后消失）；
  3. 看 GitHub Desktop 改动列表，确认只含 §4 清单文件；`Assembly-CSharp*.csproj`、`Docs/QA/Evidence/DIAG-SKILL/editor_run3/`、`Docs/QA/Evidence/FND-00B-06/editor_run4/` 为**开工前既有脏项**，非本里程碑改动。

## 8. 建议提交

```text
refactor(interaction): M3 统一交互（IInteractionTarget + InteractionRunner；三交互物不再自持输入/UI；REFACTOR 11/11×4 + SPEC01 12/12×3）
为什么：把「点一下交互物→走过去→到位补播 Interact→执行」从输入层/角色大脑/各交互物三处各写一遍，收敛为目标侧接口 + 单位侧运行时执行器，新增交互物只实现一个接口、不再改输入层；宝箱面板改由 Opened 事件驱动，行为由既有 10 条 + 新增 Refactor.InteractionContract + 12 条 SPEC01 兜底，全程零场景/预制体/Animator/.asset 改动。
```

## 9. 消耗记录

- 钱包：`Go 订阅`
- 模型完整 ID：`opencode-go/deepseek-v4.1-flash`
- 运行配额相关：编辑器内菜单执行 7 次（REFACTOR ×4、SPEC01 ×3），均单实例、未重启 Unity、未跑 batchmode。
