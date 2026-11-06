# REFACTOR-M3 — 统一交互（IInteractionTarget + InteractionRunner）

> 长任务 `REFACTOR-LONG-01` 的 M3 小节。上游依据：`C:\Users\lll\.opencode\plan\refactor-plan.md` §2/§9-M3。
> 本单自包含；执行者只读本文件 + 仓库现状即可开工。

## §0 元信息

| 字段 | 值 |
|---|---|
| 任务 ID | `REFACTOR-M3` |
| 风险等级 | `常规`（脚本改动；不碰场景/预制体/Animator/`.asset`/`ProjectSettings`） |
| 授权 | 所有者 2026-11-05 批准长任务计划书；本里程碑在预授权范围内 |
| 基线提交 | `1961c93b`（M2 收口；开工时以 `git rev-parse --short HEAD` 记录实际值） |
| 回滚锚 | `1961c93b`；回滚 = `git revert <M3 commit>` / `git reset --hard 1961c93b`（仅所有者可执行） |
| Unity 模式 | **模式 B（编辑器常开 + MCP）**，Unity 2022.3.62f3c1 |
| 状态 | `Ready` |

> 执行者**不得**标 Done、不得改 `CURRENT.md`、**不得**做任何 git 写操作（提交由大脑/所有者做）。

## §1 需求（Why / What）

**一句话目标**：把「点一下交互物 → 走过去 → 到位执行」这条链从 `SelectionManager`/`CharacterBrain`/各交互物里各写一遍，收敛为 `IInteractionTarget`（目标侧契约）+ `InteractionRunner`（单位侧执行器）；宝箱/救援/门不再自己读 `Input` / `SelectionManager` / UI。

**用户故事**：作为重构负责人，我要新增一种交互物时只实现一个接口、不改输入层，以便 M4 与后续内容不必再为“走过去后做什么”复制粘贴一套。

**必须满足**

| 编号 | 需求 | 验收方式 |
|---|---|---|
| R-1 | 新增 `Scripts/Gameplay/Interaction/{IInteractionTarget,InteractionDefinition,InteractionRunner}.cs`（契约见 §2） | 代码审查 + 编译 |
| R-2 | `Chest` / `DownedRescue` / `DungeonDoor` 实现 `IInteractionTarget`；三者**不再**读 `Input` / `SelectionManager` / `UIManager`（宝箱开面板改走事件，见 §2） | grep 证据 + 代码审查 |
| R-3 | 交互手感与判定逐条不变：走过去→到位补播 `Interact`→执行；倒地救援的优先级、多选同时下令、打断/冷却、读条时长；门的“靠近提示 + F 进入 + 全员传送”；宝箱开盖角度/时长/内容物/面板 | REFACTOR 11/11 + SPEC01 12/12 + 目检 |
| R-4 | **不新增组件到预制体/场景**：`InteractionRunner` 只在运行时由 `CharacterBrain` 需要时 `Ensure`（`AddComponent` 或缓存引用） | 代码审查 + `git status` 无资产改动 |
| R-5 | 现有断言口径零改动（允许新增 1 条，10→11）；测试文件里对现有产品 API 的引用不得删改 | diff 审查 |

**不在范围**

- 不改交互数值（`rescueTime`/`rescueRange`/`interactExtraRange`/`openDuration`/`openAngle`/`interactRange`）。
- 不改技能、战斗、伤害管线（M4）。
- 不改 `UnitRegistry` / 状态容器（M1/M2 已收口）。
- 不新增 UI 面板、不改 `BasePanel` 体系、不改 `InventoryUI`。
- 不加“通用交互按钮/键位重映射/长按交互”等新玩法。

**约束**

- `Docs/01` §1.4 不变量：交互入口单一、目标不自持输入。
- 序列化字段名冻结：三个交互物上的 Inspector 字段（`hint`/`lid`/`openAngle`/`openDuration`/`rescueTime`/`rescueRange`/`reviveHp`/`riseDuration`/`dieTrigger`/`reviveTrigger`/`interactRange`/`dungeonEntryPoint`/`cameraPoint`）**一个都不能改名或删除**。
- Animator 触发器名 `Interact`/`Interrupt`/`Die`/`Revive` 是字符串引用，**不得改**。

## §2 方案（Plan）

**备选方案**

| 方案 | 做法 | 优点 | 缺点 | 结论 |
|---|---|---|---|---|
| A | 每个交互物自带 `InteractionRunner` 组件（预制体加组件） | 语义直观 | 必须改预制体（禁止）+ 序列化影响 | 弃用 |
| B | 接口 + 单位侧运行时 Runner（`CharacterBrain` 持有/按需 `AddComponent`） | 零资产改动、输入层只剩一处分发、可加断言 | 多一层间接 | **选定** |
| C | 只把三处 `GetComponent<Chest>()` 换成 `switch(type)` | 改动最小 | 加新交互物仍要改输入层，等于没统一 | 弃用 |

**选定方案与理由**：B —— 用接口把“目标做什么”与“单位怎么走过去”拆开，且完全不触碰资产。

**契约（必须照此实现；命名可微调，语义与可见性不得改）**

```csharp
// Scripts/Gameplay/Interaction/IInteractionTarget.cs
public enum InteractionPriority { Default = 0, Rescue = 10 }

public interface IInteractionTarget
{
    Transform Anchor { get; }                       // 落点基准（原 it.transform）
    string Hint { get; }                            // 提示文案（原 Interactable.hint；可空）
    InteractionPriority Priority { get; }           // 点击优先级：救援=Rescue，其余=Default
    bool KeyInteractable { get; }                   // 是否支持“靠近按 F 瞬发”（只有门为 true）
    bool IsAvailable(CharacterBrain unit);          // 当前是否还能交互（已开宝箱/未倒地/已用门 = false）
    float Radius(CharacterBrain unit);              // 到位半径（原 CommandInteract 的 r 计算迁到这里）
    void Interact(CharacterBrain unit);             // 执行（宝箱 OpenBy 的等价物 / 救援 CommandRescue / 门 Enter）
}

// Scripts/Gameplay/Interaction/InteractionDefinition.cs
// 交互参数值对象（非资产、不序列化到场景）：把原来散在 CharacterBrain 的常量集中
public readonly struct InteractionDefinition
{
    public static readonly InteractionDefinition Default;   // DefaultRadius = 1.5f, RangePadding = 0.6f
    public readonly float DefaultRadius;
    public readonly float RangePadding;
}

// Scripts/Gameplay/Interaction/InteractionRunner.cs —— 与 CharacterBrain 同物体，运行时 Ensure
public sealed class InteractionRunner : MonoBehaviour
{
    public static InteractionRunner Ensure(CharacterBrain brain);   // 已存在则复用，否则 AddComponent（不改资产）
    public void Order(IInteractionTarget target);      // 原 CharacterBrain.CommandInteract：算落点 + CommandMove + 记待办
    public void CancelPending();                       // 原 CharacterBrain.CancelPendingInteract
    public bool TryKeyInteract();                      // 供输入层 F 键：范围内最近的可 KeyInteractable 目标 → Interact
    public string PollKeyHint();                       // 供输入层：范围内最近的可 KeyInteractable 目标 Hint（无则 null）
    // 到达检测由 CharacterBrain 每帧调用 OnMovedTick(bool wasMoving, bool isMoving)，等价原 Update 内联逻辑
}
```

- **`CharacterBrain` 改动最小化**：保留 `CommandInteract(Interactable)` 作为**兼容入口**（内部转 `Runner.Order(ResolveTarget(it))`），`CancelPendingInteract()` 转发 `Runner.CancelPending()`；`Update` 里“到位补播 Interact + 执行”的那段**逻辑不变**，只是执行体变成 `target.Interact(this)`（不再 `GetComponent<Chest>()`）。
- **`SelectionManager`（输入层）**：仍是唯一读 `Input` 的地方。点击分发改为：命中物上取 `IInteractionTarget`，按 `Priority` 选最高且 `IsAvailable` 的；`Rescue` 优先级 → 对所有选中玩家逐个 `target.Interact(brain)`（等价原 `TryCommandRescue`，**不再按 `DownedRescue` 具体类型分支**）；其余 → 对选中玩家 `brain.CommandInteract`（等价原 `CommandInteract`）。另加 F 键：`Primary` 的 `InteractionRunner.Ensure(...).TryKeyInteract()`，并在 Update 里 `PollKeyHint()` 非空时 `MessageFeed.SetMessage(hint)`（**必须是每帧写**，保持原门提示“持续显示”的观感）。
- **`DungeonDoor`**：删掉 `Update` 里的 `SelectionManager`/`Input`/`MessageFeed` 逻辑，只保留 `IInteractionTarget` 实现（`KeyInteractable=true`、`Hint="按 F 进入地牢"`、`Radius=interactRange`、`IsAvailable=!_used`、`Interact=Enter()`），`Enter()` 内容**逐行不变**。
- **`Chest`**：`OpenBy` 保留为 `Interact` 的实现体；**移除** `UIManager.instance.ShowInventory(...)` 直调，改为在 `OpenSequence` 末尾触发 `public static event System.Action<Chest> Opened;`（并暴露 `public Inventory Contents { get; }` 供 UI 取用）；`UIManager` 在 `Awake` 订阅、`OnDestroy` 退订，回调里 `ShowInventory(chest.Contents, chest)`。
- **`DownedRescue`**：实现 `IInteractionTarget`（`Priority=Rescue`、`KeyInteractable=false`、`IsAvailable=IsDown`、`Interact(unit) = CommandRescue(unit.health)`、`Radius` 与宝箱同算法），`CommandRescue` 内部的 `rBrain.CommandInteract(_interact)` 改为 `rBrain.CommandInteract(_interact)` 之外的新入口 **或** 直接 `InteractionRunner.Ensure(rBrain).Order(this)`（等价；`CancelPendingInteract` 同步改）。读条/打断/冷却/动画段**一行不改**。
- **`Interactable`**：保留为“点击命中标记 + 悬停 hint”（Chest/DownedRescue 身上已有，不能删组件）；不再承担交互语义。可加一个便捷方法 `IInteractionTarget Target()`（`GetComponentInParent` 缓存），不得改 `hint` 字段名。
- **`InteractionDefinition`**：只做参数集中，`CharacterBrain.interactExtraRange` 字段**必须保留并继续作为默认值来源**（Inspector 可调），`InteractionDefinition.Default` 作为兜底常量。

**数据流**

`输入层 SelectionManager（点/按 F）→ 命中 IInteractionTarget → CharacterBrain.CommandInteract → InteractionRunner.Order（CommandMove）→ 到位 → 播 Interact + target.Interact(unit) → 目标自执行（开箱/读条/传送）→ 事件（Chest.Opened → UIManager 开面板）`

**影响面清单**

| 项 | 内容 |
|---|---|
| 新增文件 | `Scripts/Gameplay/Interaction/IInteractionTarget.cs`、`InteractionDefinition.cs`、`InteractionRunner.cs`（+ Unity 生成 `.meta`；`Gameplay/` 与 `Gameplay/Interaction/` 目录 `.meta` 由 Unity 生成） |
| 修改文件 | `System/Interactable.cs`、`System/Chest.cs`、`System/DungeonDoor.cs`、`Units/DownedRescue.cs`、`Units/CharacterBrain.cs`、`Player/SelectionManager.cs`、`UI/UIManager.cs` |
| 修改符号 | 新增接口/枚举/Runner；`Chest.OpenBy`（保留签名）、`Chest.Opened`（新增事件）；`DungeonDoor.Update` 删除、`Enter` 保留；`DownedRescue.CommandRescue` 内部入口调整；`CharacterBrain.CommandInteract/CancelPendingInteract/Update`；`SelectionManager.HandleClick/TryCommandRescue/Update`；`UIManager.Awake/OnDestroy` |
| 字符串引用 | Animator：`Interact`/`Interrupt`/`Die`/`Revive` **不改**；`SendMessage`/`GameObject.Find`：无新增 |
| 场景 / 预制体 / `.asset` | **零改动**（`Interactable`、`Chest`、`DungeonDoor`、`DownedRescue` 组件位置与序列化字段不变；Runner 运行时创建、不落盘） |
| `.meta` | 仅 3 个新脚本 + 2 个新目录自带；不移动/重命名任何已有资产 |
| 测试文件 | `Tests/REFACTOR/Editor/REFACTOR_BatchAcceptance.cs`（`DefaultMilestone` → `"M3"`）；`Tests/REFACTOR/Runtime/REFACTOR_ProbeRunner.cs`（新增第 11 条 `Refactor.InteractionContract`） |

**实施顺序**

1. 写 3 个新文件（编译通过）。
2. `CharacterBrain` 接 `InteractionRunner`（保持行为），编译。
3. `Chest` / `DownedRescue` / `DungeonDoor` 实现接口并去掉自持输入/UI；`UIManager` 订阅 `Chest.Opened`；编译。
4. `SelectionManager` 改分发 + F 键 + 提示轮询；编译。
5. `DefaultMilestone="M3"` + 新增第 11 条断言。
6. 跑 L1：REFACTOR ×3 + SPEC01 ×2。
7. 证据 + REPORT。

**风险与回滚**

- 最可能错：① 门的提示每帧写入被漏掉（观感变化）；② 救援多选下令被改成只对主选中生效；③ 到位补播 `Interact` 的时序（`wasMoving && !isMoving`）被改；④ 宝箱面板改事件后未订阅/重复订阅导致不开面板或开两次；⑤ `DownedRescue` 的 `CancelPendingInteract` 与 `NotifyMoveCommand` 的配合被打断（打断后白播一轮 Interact）。
  - 检测：REFACTOR `Chest.Interact` / `Rescue.ChannelInterrupt` / `Hit.InterruptAttack` 三条既有断言（**必须原样通过、口径不变**）+ 新第 11 条 + SPEC01 12 条。
- 回滚锚：`1961c93b`。

## §3 验证（Validation）

**AC 表**

| AC | Given / When / Then | 断言 ID | 证据 |
|---|---|---|---|
| AC-1 | 给定 `Chest.cs`/`DownedRescue.cs`/`DungeonDoor.cs`，当 grep `Input\.`、`SelectionManager`、`UIManager`，则三者命中数 = 0（注释不计，需说明过滤方式） | `Refactor.InteractionNoSelfInput`（grep 证据） | `Docs/QA/Evidence/REFACTOR-M3/grep-interaction.txt` |
| AC-2 | 给定 REFACTOR 套件，连续跑 3 次，则每次 **11/11、fail=0、blocked=0、infra=0、exit=0** | 套件全部 11 条 | `Docs/QA/Evidence/REFACTOR-M3/editor_run1..3/` |
| AC-3 | 给定 SPEC01 套件，跑 2 次，则每次 **12/12、fail=0、exit=0** | SPEC01 全 12 条 | `Docs/QA/Evidence/SPEC-01/editor_run<新>/` |
| AC-4 | 给定三个交互目标，当按契约查询/执行，则接口语义成立且宝箱开箱事件恰好一次（见「新增断言」） | `Refactor.InteractionContract` | 同上 JSONL |
| AC-5 | 给定 `git status`，则只含本单声明文件 + 派单前既有脏项；**无场景/预制体/`.asset` 改动** | — | `REFACTOR-M3/git-diff-stat.txt` |
| AC-6 | 给定既有 10 条断言，则其**口径与代码零改动**（只允许新增第 11 条与头注释/blocked 列表） | diff 审查 | `REFACTOR-M3/test-diff.txt` |

**新增断言（REFACTOR 套件加第 11 条，不得改前 10 条）**

`Refactor.InteractionContract`（Play 模式，用运行期内存夹具，结束销毁，不写场景）：

1. 场景内 `Chest` / `DownedRescue` / `DungeonDoor`（各取一个实例）都 `is IInteractionTarget`；
2. 契约取值：`Chest.KeyInteractable == false`、`DownedRescue.KeyInteractable == false`、`DungeonDoor.KeyInteractable == true`；`DownedRescue.Priority == InteractionPriority.Rescue`，其余 `Default`；
3. `Chest.IsAvailable(unit)` 开箱前 true；`Interact(unit)` 后 `Chest.Opened` 事件**恰好触发 1 次**，且 `IsAvailable == false`；再次 `Interact` 不再触发（幂等）；
4. `DungeonDoor.IsAvailable(unit)` 初始 true（**不要真的触发 Enter**，避免传送干扰后续断言）；
5. `InteractionRunner.Ensure(brain)` 两次返回同一实例（不重复挂组件）。

**运行次数**：REFACTOR **N=3**；SPEC01 **2 次**。

**自动化优先**：全部通过 `Tools/QA/In-Editor/{REFACTOR, SPEC01 (green)}` 菜单执行；**不得** `run_tests`，不得 batchmode。

**必须保存的证据**：两套的 `Assertions.jsonl` / `Summary_run*.json` / `ExitCode_run*.txt` / `InEditorResult.txt` + `grep-interaction.txt`、`git-diff-stat.txt`、`test-diff.txt`（既有 10 条的 diff 证明）。

## §4 执行信息

| 项 | 内容 |
|---|---|
| 前置 | Unity 已开（模式 B），MCP 单实例；`is_compiling=false`；场景 `DungeonWorld` 且非脏 |
| 允许改动 | 上述 7 个脚本 + 新增 `Scripts/Gameplay/Interaction/**`（3 个 `.cs`）+ `Tests/REFACTOR/**`（仅第 11 条 + `DefaultMilestone`）+ `Docs/Handoff/REPORT_REFACTOR-M3.md` + `Docs/QA/Evidence/REFACTOR-M3/**` |
| 允许读取 | 本工单 + 被改文件现状（只读） |
| 绝不触碰 | `Art/**`、`Assets/Scenes/**`、`Prefabs/**`、`*.asset`、`ProjectSettings/**`、`Packages/**`、`AGENTS.md`、`Docs/Handoff/CURRENT.md`、其他 `TASK_*`/`REPORT_*`、`Tests/**` 里除 REFACTOR 套件的任何文件、Animator 参数名、现有断言口径 |
| 证据目录 | `Docs/QA/Evidence/REFACTOR-M3/`；SPEC01 进 `Docs/QA/Evidence/SPEC-01/editor_run<N>/` |
| 熔断 | `AGENTS.md` §6 + 计划书 §6；修复轮次 **1 轮** |
| 执行预算 | `.cs` 文件数 ≤ 12；不重启 Unity；不跑全量 batch |

**停止条件（照计划书 §6）**：需要改预制体/场景/`.asset`/`ProjectSettings` 才能继续；需要改 Animator 参数名；需要改 `Docs/01` 不变量；编译错误连续 2 轮；修复轮用尽仍红；`git status` 对不上。

## §5 学习产出

| 项 | 要求 |
|---|---|
| 讲稿 | 不适用（M4 收口后统一写重构讲稿） |
| 三句话版本 | 写进 REPORT §3 第 7 条 |

## 自包含声明

本工单不依赖 `CURRENT.md` 或其他任务书即可执行；信息不足时在 REPORT 写“信息不足”并列缺项，**不**自行翻阅其他文档补全。
