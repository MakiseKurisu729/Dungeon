# REPORT — RESCUE-01 救援改为「指令式」

| 字段 | 值 |
|---|---|
| 任务 | `RESCUE-01`（高风险） |
| 执行者 | OpenCode + DeepSeek（子代理） |
| 执行模式 | 模式 B｜MCP 常开编辑器（`execute_code` / `execute_menu_item` / `read_console` / `refresh_unity` / 资源读取） |
| Unity | 2022.3.62f3c1，场景 `Assets/Scenes/DungeonWorld.unity` |
| 结论 | 8 条 AC 全部达成；SPEC01 **12/0**，退出码 0 |
| 额外授权 | 所有者中途批准资产接线（见 §7-1），已按其批准范围执行并在 §3 单列 |

---

## 1. 结果摘要

1. 救援从「选中的人走进 2.5m 自动读条」改成「**悬停出救援图标 → 左键下令 → 走过去 → 循环读条 → 被打/移动打断 → 必须重新下令**」。`DownedRescue.Update()` 里的自动启动逻辑已删除，现在唯一的救援入口是 `CommandRescue(UnitHealth)`。
2. 悬停判定插在 `CursorManager.ResolveHover` 的 `Select` 之前，点击判定插在 `SelectionManager.HandleClick` 的 `sel != null` 之前；「要不要显示救援图标」的真正开关是 `DownedRescue.IsDown`（站着时同样的 `Interactable` 只会被判成 `Select`）。
3. 读条期间 `Interact` 触发器按「不在 Interact 或接近播完」重发，3s 内重进 3 次；打断后 `_channeling` 在 5～7ms 内变 false、2s 内不自动重开、且不再残留一轮交互动作（这条是 AC-5/AC-6 第一轮红→修→绿的关键）。
4. SPEC01 12 条断言全绿，普通选中/点怪攻击/技能施法链路无回归。

## 2. 证据锚点（AC 逐行回填）

证据根目录：`Docs/QA/Evidence/RESCUE-01/`

| AC | 断言 ID | 期望 | 实测 | 结论 | 证据 |
|---|---|---|---|---|---|
| AC-1 | `Rescue.HoverIcon` | 选中活人 + 指向倒地者 → `Interact`；未选中 → `Select`；指向活人 → `Select`；指向怪 → `Attack` | `Interact` / `Select` / `Select` / `Attack`（另加 case5：主选中是倒地者 → `Select`），5/5 命中 | **Pass** | `hover.txt` |
| AC-2 | `Rescue.CommandIssued` | 点击下达指令且不选中倒地者；点怪仍攻击 | 点击后 `CurrentRescuer=Knight_Unit`、`_selected` 仍是 `[Knight_Unit]`（1→1）、`_routine=running`；点怪 `_enemySelected=Skeleton` 且无救援指令；未选中时点活人是普通选中；选中倒地者点自己 `issued=0` | **Pass** | `command.txt` |
| AC-3 | `Rescue.NoAutoStart` | 站在 2.5m 内不点击，3s 内 `_channeling==false` | 距离 1.80m，3 次各 3.0s：`IsChanneling` 从未 true、Animator 从未进 `Interact`、`CurrentRescuer` 始终 null | **Pass**（3/3） | `no_auto_start.txt` |
| AC-4 | `Rescue.ChannelLoop` | 进范围 0.3s 内开始读条；3s 内 `Interact` 重触发 ≥2 次 | 3 次：进入范围→读条 **0.000s**，读条时长 **3.00s**，`Interact` 进入 **3 次**（3 次运行一致） | **Pass**（3/3） | `channel_loop.txt` |
| AC-5 | `Rescue.InterruptByHit` | 受击 0.2s 内 `_channeling=false`、`Interact` 退出、2s 不自动重开 | 3 次：掉读条 **0.005/0.006/0.005s**，+0.2s 状态 `Hit_A`，2s 内重新读条 **0 次**、`Interact` 进入 **0 次**；冷却后重新点击 0.005～0.007s 即重新读条并救起 | **Pass**（3/3） | `interrupt_hit.txt` |
| AC-6 | `Rescue.InterruptByMove` | 移动指令 0.2s 内同上 | 3 次：掉读条 **0.006/0.005/0.006s**，+0.2s 状态 `Locomotion`，2s 内重新读条 0 次、`Interact` 进入 0 次；救援者从 1.8m 走到 6.00m（移动指令确实生效） | **Pass**（3/3） | `interrupt_move.txt`、`interrupt_move_same_frame_timeline.txt` |
| AC-7 | `Rescue.Complete` | `IsDown=false`、`Revive` 走通、`Rise` 播放、1 血恢复 | `IsDown=false`、`dead=false`、`hp=1/80`；动画 `Downed → Rise(+0.102s) → Locomotion(+2.284s)`；`brain.enabled` 在起身后恢复 `true`；救援者读条停止（`Locomotion`） | **Pass** | `complete.txt` |
| AC-8 | `all` | 只改允许文件；SPEC01 12/0 | 代码 4 个 `.cs` +（所有者批准的）1 预制体 + 1 场景；SPEC01 `pass=12 fail=0 blocked=0`，退出码 0（`editor_run15`） | **Pass** | `git_diff_stat.txt`、`SPEC01_Summary.txt` |

## 3. 改动清单

### 3.0 三句话版本（TASK §5 要求）

1. 救援的「什么时候开始」从距离自动触发改成必须由玩家点击下令，`_channeling` 只由 `CommandRescue` 打开。
2. 「能不能看到救援图标」由 `DownedRescue.IsDown` 单点门控，`CursorManager` 与 `SelectionManager` 各自把这条判定插到原有 `Select` 分支之前，所以站着的人永远不会变成救援图标、点怪仍然是攻击。
3. 打断做成「事件 + 指令通知」两条独立入口，并且打断后要清状态、清待办、清残留触发器、2s 冷却，所以「被打/被叫走」之后必须重新下令才会重新读条。

### 3.1 代码（4 个文件，均在工单允许范围内）

| 文件 | 改动 |
|---|---|
| `Assets/_Project/Scripts/Units/DownedRescue.cs` | 删 `Update()` 自动启动与 `_rescuing`；新增 `CommandRescue(UnitHealth)`（:93）、静态 `NotifyMoveCommand(GameObject)`（:120）、`CancelByMove()`（:134）；重写 `RescueRoutine`（:145，抵达 15s 超时 + 打断冷却 + 循环读条 + 结束判定）；新增 `PushInteract`（:215，含「进入 Interact 过渡中不再压触发器」）、`ResetChannelTrigger`（:232）、`Abort()`、`OnInterrupted()`（:255）、`InRange()`；只读属性 `IsChanneling`（:38）、`CurrentRescuer`；`Start()` 缓存 `Interactable` 并仅在 `hint` 为空时兜底文案。序列化字段 `rescueTime/rescueRange/reviveHp/riseDuration` 名称与默认值未动。`OnDowned` 里 `ResetTrigger("Hit")` 原样保留。 |
| `Assets/_Project/Scripts/UI/CursorManager.cs` | `ResolveHover` 在 `inter != null && selectable == null` 之前插入救援分支（:129-133）：`inter.GetComponent<DownedRescue>().IsDown` 且主选中是活着玩家 → `Interact`；新增 `PrimaryCanRescue()`（:144）。 |
| `Assets/_Project/Scripts/Player/SelectionManager.cs` | `HandleClick()` 拆成无参壳 + `HandleClick(Ray)`（:99/:107，行为不变，只为可测）；在 `sel != null` 之前插入 `if (!ctrl && TryCommandRescue(inter)) return;`（:135）；新增 `TryCommandRescue(Interactable)`（:169，按 `_selected` 内活着的玩家逐个下令）。 |
| `Assets/_Project/Scripts/Units/CharacterBrain.cs` | `OnCommandMove` 追加 `DownedRescue.NotifyMoveCommand(gameObject)`（:86，只在真正下指令时触发）；新增 `CancelPendingInteract()`（:62，供 `DownedRescue` 撤销「到位补播一次 Interact」）。 |

### 3.2 资产（所有者中途批准的「资产操作」，非工单原始范围）

| 文件 | 改动 | 原因 |
|---|---|---|
| `Assets/_Project/Prefabs/Knight_Unit.prefab` | 根节点加 `Interactable`（`hint="救援"`），+16 行 | 工单 §2 的 hover/点击/跑位都依赖 `Interactable`，但三个玩家预制体上都没有，且全场景 `FindObjectsOfType<Interactable>()` 为 0 —— 照原样交付 AC-1/AC-2 必挂 |
| `Assets/Scenes/DungeonWorld.unity` | `interactCursor = gauntlet_point.png`、`interactHotspot = (4,2)`，共 2 行 | `interactCursor` 槽原本是空的（其余 4 个光标都有贴图），R-1「使用 interactCursor」无法达成。选 `gauntlet_point` 是因为它是 cursor-pack 里**唯一没被占用且勾了 Read/Write**（`CursorManager.GetTinted` 需要可读贴图，其余候选会在染色时抛 `Texture is not readable`）的指针类图标，不新增美术资源 |

> `Archer_Unit.prefab` / `Mage_Unit.prefab` 是 `Knight_Unit` 的 **variant**，加基类后自动继承，两个文件 `git status` 干净（首次误加的重复组件已用 `PrefabUtility.IsAddedComponentOverride` 精确删除，最终每个预制体 1 个 `Interactable`）。

### 3.3 文档

- `Docs/QA/Evidence/RESCUE-01/**`（9 个证据文件）
- `Docs/Handoff/REPORT_RESCUE-01.md`（本文件）
- `Docs/Learning/RESCUE-01_讲稿.md`

## 4. 测试数据

| 项 | 数据 |
|---|---|
| AC-3 | 3 次 × 3.0s；距离 1.80m（rescueRange 2.5）；`IsChanneling` 曾 true = False ×3；`Interact` 曾进入 = False ×3 |
| AC-4 | 3 次；进入范围→读条 0.000s / 0.000s / 0.000s；读条 3.00s / 3.00s / 3.00s；`Interact` 进入 3 / 3 / 3 次（间隔 ≈1.16s） |
| AC-5 | 3 次；掉读条 0.006 / 0.005 / 0.005s；+0.2s 状态 `Hit_A`×3；窗口内重新读条 0；窗口内 `Interact` 进入 0；冷却后重下令 → 0.007 / 0.005 / 0.007s 重新开始并救起 |
| AC-6 | 3 次；掉读条 0.006 / 0.005 / 0.006s；+0.2s 状态 `Locomotion`×3；窗口内重新读条 0；`Interact` 进入 0；救援者位移 1.8m → 6.00m |
| AC-7 | 1 次；`Revive` 后 `hp=1/80`、`dead=false`、`IsDown=false`；动画 `Downed → Rise(+0.102s) → Locomotion(+2.284s)`；`brain.enabled` 起身后 `true` |
| SPEC01 | `editor_run15`：断言 12 条，pass 12 / fail 0，退出码 0 |
| 对抗性观察 | 「刚下令当帧就叫他走」的压力场景：读条 0.006s 停，动画因**入场 0.1s + 退场 0.1s 两条过渡串联**在 +0.208s 才切出 `Interact`（原始逐帧时间线见 `interrupt_move_same_frame_timeline.txt`）。常规路径（读条稳定 0.4s 后再打断）为 0.006s 停、+0.2s 已到 `Locomotion` |

**第一轮红 → 修 → 绿（AC-5/6）**：第一版打断只做「清状态 + 提示」，实测 AC-5 出现「2s 窗口内 `Interact` 进入 1 次」、AC-6 出现「+0.2s 状态仍为 `Interact`」。逐帧时间线定位到两个独立残留：
（a）`CharacterBrain` 在跑到交互落点时会补播一次 `Interact`（`_pendingInteract` 的到达分支），打断时没有撤销 → 打断后白播一轮；
（b）读条循环在「进入 `Interact` 的过渡期」又压了一次触发器，留下一个没人消费的 `Interact`，等 `Hit_A`/`Locomotion` 结束后被消费。
修法：`CharacterBrain.CancelPendingInteract()` + `DownedRescue.ResetChannelTrigger()` + `PushInteract` 的过渡期守卫。修后 AC-5/6 各 3 次全绿，且 2s 窗口内 `Interact` 进入次数恒为 0。

## 5. 实现原理（关键 5 处）

1. `Assets/_Project/Scripts/Units/DownedRescue.cs:93` `CommandRescue` —— 唯一的救援入口：校验 `IsDown`/救援者在活/非本人 → 停掉上一条协程 → **先** `brain.CommandInteract(_interact)` 再记 `_rescuer`。顺序很关键：`CommandInteract` 内部会走一次 `CommandMove` → `CharacterBrain.OnCommandMove` → `NotifyMoveCommand`，此时 `_rescuer` 还是 null，所以不会把自己这条刚下的指令当成「移动打断」。
2. `Assets/_Project/Scripts/Units/DownedRescue.cs:145` `RescueRoutine` —— 三段式：等抵达（15s 超时/救援者阵亡/目标已复活则静默 `Abort`）→ 等打断冷却（`_channelBlockUntil`，指令不丢、冷却到期再开）→ 读条循环（`t += Time.deltaTime` 累到 `rescueTime`，每帧 `PushInteract`；`OnHit` 事件与出界都会 `break`）。协程用 `try/finally` 保证 `OnHit` 一定退订、`_channeling` 一定回落。
3. `Assets/_Project/Scripts/Units/DownedRescue.cs:215` `PushInteract` —— 循环的实现：仅当「不在 `Interact` 或 `normalizedTime ≥ 0.95`」才重发触发器，且 0.2s 节流、且过渡期不压。之所以不改 clip 的 Loop：`PlayerBase.controller` 的 `Interact` 是「播到 0.8077 就回 `Locomotion`」的一次性状态，读条时长（3s）与 clip 长度无关，靠重新压触发器才能把「读条 3s」和「动作循环」解耦，也才不用动资产。
4. `Assets/_Project/Scripts/Units/DownedRescue.cs:120` `NotifyMoveCommand` + `:134` `CancelByMove` —— 移动打断从**指令侧**进（`CharacterBrain.OnCommandMove` 是唯一「真正下指令」的地方，受击位移不经过它），遍历 `AllDowned` 找到 `_rescuer == this` 的实例取消，同时置 2s 冷却；受击打断仍走 `UnitHealth.OnHit` 事件，因为它与「有没有下指令」无关。
5. `Assets/_Project/Scripts/UI/CursorManager.cs:129` + `Assets/_Project/Scripts/Player/SelectionManager.cs:135` —— 优先级插在**渲染层之外**：`CursorManager` 只决定「画哪个光标」，`SelectionManager` 只决定「点了之后干什么」，两者共用同一个 `IsDown && 主选中是活着的玩家` 条件，避免「光标说能救、点下去却在选人」的不一致。

## 6. 模块导航

| 想知道… | 去哪看 |
|---|---|
| 救援现在怎么被触发 | `SelectionManager.TryCommandRescue` → `DownedRescue.CommandRescue` |
| 为什么站着的人不会显示救援图标 | `CursorManager.ResolveHover` 的救援分支要求 `DownedRescue.IsDown`；`SelectionManager` 侧同理 |
| 读条为什么是循环的 | `DownedRescue.PushInteract` + `PlayerBase.controller` 的 `Interact`/`Locomotion` 互转（不改资产） |
| 被打断后为什么必须重新下令 | `RescueRoutine` 是单发协程；`OnInterrupted`/`CancelByMove` 清 `_rescuer` 并置 `_channelBlockUntil`，没有任何自动重启路径 |
| 移动打断从哪里进 | `CharacterBrain.OnCommandMove` → `DownedRescue.NotifyMoveCommand` |
| 谁负责「走到旁边」 | `CharacterBrain.CommandInteract`（落点 = 碰撞体半径 + `interactExtraRange`），随后 `CancelPendingInteract()` 撤掉到达补播 |
| 要看实测数据 | `Docs/QA/Evidence/RESCUE-01/`（hover / command / no_auto_start / channel_loop / interrupt_hit / interrupt_move / complete / git_diff_stat / SPEC01_Summary） |

## 7. 未决项

1. **资产操作授权记录**：本任务属高风险且工单写「绝不触碰预制体/场景」。执行中核对到工单前提与仓库不符（玩家预制体无 `Interactable`、`interactCursor` 为空），按 §6.1 停下询问；所有者确认「只要图标只在对地状态出现就批准资产操作」后执行了 §3.2 的改动。请复核 `Knight_Unit.prefab` 与 `DungeonWorld.unity` 的 diff 是否接受。
2. **预制体 diff 含本批其它任务的序列化字段**：保存 `Knight_Unit.prefab` 时 Unity 顺带把当前批次（`MeleeCombat`/`RangedCombat` 已改）里新增的序列化字段落盘（`attackTrigger: AttackMelee`、`hitStateName: AttackMelee`）。不是本任务行为改动，但会出现在同一个提交里，建议提交前看一眼。
3. **倒地者仍可被「选中」**：`SelectionManager.SingleSelect` 不拦 `dead` 单位，`OnDowned` 只是把选中圈关掉（DOWNED-01 现状）。工单 R-8 要求「现有行为保持」，本次未改；若后续要求「倒地者不可进入选中集合」，需改 `SingleSelect`。
4. **同帧打断的动画尾**：AC-6 压力场景下 `Interact` 状态在 +0.208s 才退出（两条 0.1s 过渡串联），读条状态本身 5～7ms 就停了。常规操作（读条已经播起来再打断）不受影响，未额外加 `Animator.Play` 硬切以免破坏过渡手感。
5. **怪物夹具**：场景原本的 `Skeleton_Warrior` 在早前一轮 AC-2 里被骑士击杀并销毁，AC-1 的 `Attack` 用例改用运行期实例化的 `Skeleton_Warrior.prefab` 夹具（用完销毁，未写入场景）。
6. `Application.runInBackground` 在 Play 期间置 true，结束时已还原为 false；`git status` 无 `ProjectSettings` 变更。

## 8. 建议提交

**标题**：`功能/交互：救援改为点击指令式（悬停图标→下令→循环读条→受击/移动打断）`

**正文（一行）**：`把「走进 2.5m 自动读条」改成必须由玩家对倒地队友下令，读条循环 Interact、受击或移动立即打断且 2s 内不自动重开，并补上玩家预制体的 Interactable 与交互光标接线。`

**提交范围建议**：`DownedRescue.cs`、`CursorManager.cs`、`SelectionManager.cs`、`CharacterBrain.cs`、`Knight_Unit.prefab`、`DungeonWorld.unity`、`Docs/QA/Evidence/RESCUE-01/`、`REPORT_RESCUE-01.md`、`TASK_RESCUE-01.md`、`Docs/Learning/RESCUE-01_讲稿.md`、`Docs/QA/Evidence/SPEC-01/editor_run15/`。
