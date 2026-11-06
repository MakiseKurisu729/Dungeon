# RESCUE-01 — 救援改为“指令式”：悬停图标 → 点击下令 → 走进范围 → 循环读条 → 被打/移动打断

## §0 元信息

| 字段 | 值 |
|---|---|
| 任务 ID | `RESCUE-01` |
| 风险等级 | `高风险`（交互指令 + 鼠标 hover + 战斗中断语义；涉及救援成功率） |
| 授权 | 所有者 2026-11-05：“选中没有倒地的角色，把指针移到倒地角色上，指针变成救援图标，点击触发救援指令，角色走到范围内播放救援动作（交互动作，读条内循环），被打和移动都会打断，此时得重新发救援指令” |
| 图标决定 | 复用现有 `interactCursor`（不新增美术资源） |
| 基线提交 | `6872348b` + 未提交批次（Phase A / TEST-MCP-01 / DOWNED-01 / INTERRUPT-01） |
| 回滚锚 | `git checkout -- Assets/_Project/Scripts/Units/DownedRescue.cs Assets/_Project/Scripts/UI/CursorManager.cs Assets/_Project/Scripts/Player/SelectionManager.cs Assets/_Project/Scripts/Units/CharacterBrain.cs` |
| 状态 | `Ready`（待所有者确认 AC） |

## §1 需求（Why / What）

**一句话目标**：救援从“走进 2.5 米自动读条”改成“明确的点击指令 + 可打断的读条”，并且 hover 时能看出“这个倒地队友可以救”。

**用户故事**：作为玩家，我要先选一个还站着的队友，再把鼠标移到倒地的队友身上看到救援图标，点一下他才跑过去救；救到一半我被打了或我让他走开，这次救援就作废，必须重新点一次。

**必须满足**

| 编号 | 需求 | 验收方式 |
|---|---|---|
| R-1 | 已选中**未倒地**玩家单位时，鼠标悬停倒地队友 → `CursorManager` 返回 `Hover.Interact`，并使用 `interactCursor` + 现有 hint | `Rescue.HoverIcon` |
| R-2 | 该状态下左键点击 → 下达**救援指令**（不是只选中倒地者），且只对当前选中的未倒地单位生效 | `Rescue.CommandIssued` |
| R-3 | 仅“走进范围”**不再**自动开始救援；未点击而过路不会触发 | `Rescue.NoAutoStart` |
| R-4 | 抵达 `rescueRange` 后 0.3s 内开始读条，读条期间 `Interact` 动作**循环播放**（3s 读条至少重触发 2 次） | `Rescue.ChannelLoop` |
| R-5 | 读条中救援者**受击** → 立即中断，`Interact` 停止，提示“救援被打断”，且**不自动重开**（2s 内不得重新开始） | `Rescue.InterruptByHit` |
| R-6 | 读条中救援者收到**移动指令** → 立即中断，同上不自动重开 | `Rescue.InterruptByMove` |
| R-7 | 读条完成 → 倒地者 `Revive` 走通（`Die→Downed→Revive→Rise→Locomotion`），`IsDown=false` | `Rescue.Complete` |
| R-8 | 倒地者不可被选中、不可被当作救援者（现有行为保持） | `Rescue.TargetRules` |
| R-9 | 不回归：SPEC01 12/0；`INTERRUPT-01` 的普攻/交互打断仍成立 | `all` |

**不在范围**：多人同时救援、救援进度条 UI、救援音效、倒地者被怪物攻击、救援经验/奖励。

**约束**：复用现有 `Interactable` 与 `interactCursor`；不改 `PlayerBase.controller`、场景、预制体、`.asset`、`OC_*`；`rescueTime`/`rescueRange`/`reviveHp` 现有序列化字段**不改名、不改默认值**（预制体已引用）。

## §2 方案（Plan）

**备选方案**

| 方案 | 做法 | 优点 | 缺点 | 结论 |
|---|---|---|---|---|
| A | 保留 `DownedRescue`，新增 `CommandRescue(rescuer)`；hover 与点击在 `CursorManager`/`SelectionManager` 里按“倒地队友 + 已有选中”判优先级 | 改动集中、复用现有 `Interactable`/`CommandInteract` | 需要 4 个文件协同 | **选定** |
| B | 新建独立的 `RescueInteractable` 组件替换现有 | 职责更纯 | 预制体要挂新组件（资产改动，归所有者），迁移风险 | 弃用 |
| C | 保留自动读条，只加图标 | 改动最小 | 不满足“必须重新下令” | 弃用 |

**选定方案与理由**：A 不改预制体接线（`DownedRescue` 已经挂在玩家单位上），只改判定与流程控制，回滚成本最低。

**数据流**

```
hover: CursorManager.ResolveHover
  ├ 命中 Interactable + 其 DownedRescue.IsDown
  └ 且 SelectionManager.Primary 是未倒地玩家 → Hover.Interact（interactCursor + hint）

click: SelectionManager.HandleClick
  ├ 若 hover 目标是“可救援倒地者”且当前有未倒地选中 → DownedRescue.CommandRescue(rescuer)
  └ 否则维持原有 Select / Attack / Interact 判定

command: DownedRescue.CommandRescue(rescuer)
  ├ 记录 _rescuer，禁用自动逻辑
  └ CharacterBrain.CommandInteract(this) → 走到范围内

arrive: DownedRescue.Update 检测 _rescuer 进入 rescueRange
  └ StartChannel()：设 Interact 触发器 → 读条 3s
       ├ 循环：当 Animator 状态不在 Interact 或 normalizedTime ≥ 0.95 → 再 SetTrigger("Interact")
       ├ 打断：rescuer.OnHit / CharacterBrain.OnCommandMove → StopChannel(不自动重开)
       └ 完成：Revive()
```

**具体改动点**

1. `DownedRescue.cs`
   - 删除 `Update()` 里的“距离内自动 `StartCoroutine(RescueRoutine)`”。
   - 新增 `public void CommandRescue(UnitHealth rescuer)`：校验（`IsDown`、`_channeling==false`、rescuer 活着且非本人）→ 记 `_rescuer` → 调用 `CharacterBrain.CommandInteract(GetComponent<Interactable>())`。
   - `RescueRoutine` 改为：等待进入 `rescueRange`（超时 15s 或 `_rescuer` 失效则放弃）→ 循环 `Interact` 触发器 → 读条 → 结束判定。
   - 打断源：`rescuer.OnHit`（已有）+ 新增 `UnitBrain`/`CharacterBrain` 的移动通知（见第 4 点）。
   - 打断后：`_channeling=false`、`_rescuer=null`、发 `GameData.SetMessage("救援被打断，请重新下达救援指令")`，**不**自动重启。
2. `CursorManager.cs`
   - `ResolveHover(...)` 判定顺序改为：若 `inter != null` 且 `inter.GetComponent<DownedRescue>()?.IsDown == true` 且 `SelectionManager.Primary` 是活着的玩家单位 → 返回 `Hover.Interact`；否则维持原顺序。
   - hint 文案由 `Interactable.hint` 提供（倒地单位上配置“救援”即可，若为空则在 `DownedRescue` 里兜底设置）。
3. `SelectionManager.cs`
   - `HandleClick()` 在 `if (sel != null)` 之前插入“可救援判定”：命中可救援倒地者且 `_selected` 中有活着的玩家 → 对每个选中单位调 `CommandRescue`，**不**选中倒地者。
4. `CharacterBrain.cs`
   - `OnCommandMove(Vector3)` 里追加：`DownedRescue.NotifyMoveCommand(gameObject)`（静态方法，遍历 `AllDowned` 里 `_rescuer == this` 的实例并中断读条）。只在**真正下达移动指令**时触发，不因受击位移中断。

**影响面清单**

| 项 | 内容 |
|---|---|
| 新增文件 | `Docs/QA/Evidence/RESCUE-01/**`、`REPORT_RESCUE-01.md` |
| 修改文件 | `DownedRescue.cs`、`CursorManager.cs`、`SelectionManager.cs`、`CharacterBrain.cs` |
| 字符串引用 | `Interact`（触发器）、`Hover.Interact`、`rescueRange`、`rescueTime`、`AllDowned` |
| 场景/预制体 | 不改；`DownedRescue` 与 `Interactable` 的挂载关系沿用现状 |
| `.meta` | 无 |

**实施顺序**：DownedRescue 指令化 → CursorManager 判定 → SelectionManager 点击优先级 → CharacterBrain 移动通知 → 编译 → 六段定点验证 → SPEC01。

**风险与回滚**：最大风险是点击优先级改坏普通选择/攻击（例如点怪变成点队友）。AC 里专门验“未选中任何人时点倒地者不触发救援”“点怪仍下达攻击”。回滚 = 还原 4 个文件。

## §3 验证（Validation）

| AC | Given / When / Then | 断言 ID | 证据 |
|---|---|---|---|
| AC-1 | 给定选中 `Knight_Unit` 活体，当鼠标射线命中倒地 `Archer_Unit`，则 `CursorManager.ResolveHover` 返回 `Interact`；未选中任何单位时返回 `Select` | `Rescue.HoverIcon` | `Docs/QA/Evidence/RESCUE-01/hover.txt` |
| AC-2 | 给定同上，当左键点击，则 `DownedRescue.CommandRescue` 被调用 1 次、目标不是被选中而是收到指令；点击怪物仍走攻击 | `Rescue.CommandIssued` | `command.txt` |
| AC-3 | 给定选中单位只是路过倒地者 2.5m 内但未点击，则 3s 内 `_channeling==false` | `Rescue.NoAutoStart` | `no_auto_start.txt` |
| AC-4 | 给定下达指令，当走到范围内，则 0.3s 内开始读条，3s 内 `Interact` 触发器被重发 ≥2 次 | `Rescue.ChannelLoop` | `channel_loop.txt` |
| AC-5 | 给定读条中，当救援者受击，则 0.2s 内 `_channeling==false`、`Interact` 状态退出，且 2s 内不自动重开 | `Rescue.InterruptByHit` | `interrupt_hit.txt` |
| AC-6 | 给定读条中，当对救援者下达移动指令，则同上立即中断且不自动重开 | `Rescue.InterruptByMove` | `interrupt_move.txt` |
| AC-7 | 给定读条完成，则倒地者 `IsDown=false`、`Revive` 走通、`Rise` 播放、1 血恢复 | `Rescue.Complete` | `complete.txt` |
| AC-8 | 给定最终状态，当 `git status`，则只改 4 个允许文件；SPEC01 12/0 | `all` | `git diff --stat`、`editor_runN/…` |

**运行次数**：AC-3/4/5/6 各 3 次（时序类 N≥3）；AC-1/2/7 各 1 次（确定性）。

**自动化优先**：`ResolveHover` 是纯函数（传 Ray），可在 Play 中直接以屏幕坐标构造 Ray 断言；读条/打断用 `execute_code` 驱动时间与受击。目检 1 项：救援图标观感（黄圈/十字是否清晰）。

**必须保存的证据**：hover 判定表、指令调用计数、读条重触发次数、打断前后状态序列、完成链路、`git diff`、SPEC01 一次。

## §4 执行信息

| 项 | 内容 |
|---|---|
| 前置 | Unity 打开；`DOWNED-01`/`INTERRUPT-01` 已合入；无其它 AI 驱动 |
| 允许改动 | 上述 4 个 `.cs` + 证据 + REPORT |
| 允许读取 | 本工单、4 个目标文件、`Interactable.cs`、`UnitBrain.cs`、`MeleeCombat.cs` |
| 绝不触碰 | `PlayerBase.controller`、`OC_*`、场景、预制体、`.asset`、`ProjectSettings`、`Packages`、`SkillSystem`/技能效果、`CURRENT.md` |
| MCP 权限 | `execute_code`、`execute_menu_item`（仅 `Tools/QA/In-Editor/*`）、`read_console`、`refresh_unity`、资源读取；其余写工具禁用 |
| 熔断 | 需要改预制体接线；AC-5/6 出现“打断后自动重开”；SPEC01 由 12 变红；点击优先级改坏普通选择 |
| 执行预算 | 实现 1 轮 + 修复 1 轮；不跑 batch |

## §5 学习产出

| 项 | 要求 |
|---|---|
| 讲稿 | **必须**：`Docs/Learning/RESCUE-01_讲稿.md`（AI 初稿，所有者用自己的话改写） |
| 必含 | 为什么“自动读条”要改成“指令式”；hover 判定的优先级为什么放在 `CursorManager` 而不是 `SelectionManager`；读条循环为什么要重发触发器而不是改 clip 循环；两种打断的入口为什么不同（受击走事件、移动走指令通知） |
| 三句话版本 | 写进 REPORT §3 |

## 自包含声明

本工单不依赖 `CURRENT.md` 或其他任务书即可执行。信息不足时在 REPORT 写“信息不足”并列缺项。
