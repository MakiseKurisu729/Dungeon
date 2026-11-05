# DOWNED-01 — 玩家倒地不播动作（诊断 + 修复）

## §0 元信息

| 字段 | 值 |
|---|---|
| 任务 ID | `DOWNED-01` |
| 风险等级 | `常规`（诊断 + 可能 1 处 Animator/代码修复；不新增资产） |
| 授权 | 所有者 2026-11-05 目检反馈：“角色血条清空没有倒地动作，但可以被救援” |
| 基线提交 | `6872348b` + 当前未提交批次（Phase A / TEST-MCP-01） |
| 回滚锚 | `git checkout -- <本单改动文件>` |
| 分支 | 当前分支 |
| 状态 | `Ready` |

## §1 需求（Why / What）

**一句话目标**：玩家血量归零时，屏幕上必须**看得见倒地动作**，并且仍处于“可被救援”的逻辑状态（不变成彻底死亡）。

**用户故事**：作为玩家，队友血空后我要看到他倒下，而不是“站着不动但打不了了”，否则我不知道该去救谁。

**必须满足**

| 编号 | 需求 | 验收方式 |
|---|---|---|
| R-1 | 血量归零后 0.3s 内 `Animator` 进入 `Downed` 状态，且该状态的 clip 非空、权重 > 0.9 | `Downed.AnimatorState` |
| R-2 | 倒地期间 `DownedRescue.IsDown == true`、`UnitHealth.dead == true`、`AllDowned` 含该单位 | `Downed.LogicState` |
| R-3 | 倒地后仍可被 `Revive()` 拉起（现有 `Revive` 触发 `Rise` 的链路不回归） | `Downed.RevivePath` |
| R-4 | 诊断结论有据：必须给出“到底是没进状态、还是进了但没有 clip / clip 为空 / 被覆盖器吃掉了”的判断与原始数据 | 证据 |
| R-5 | 若是缺 clip（需要美术资源），**停下报告**，不自行造 clip、不改 `OC_*` | REPORT |

**不在范围**：倒地后的救援交互流程（归 `RESCUE-01`）、鼠标图标、读条、打断；倒地镜头/UI 提示。

**约束**：不得改 `OC_*.overrideController`、场景、预制体、`.asset`；只允许改 `DownedRescue.cs` / `UnitHealth.cs` / `PlayerBase.controller`（三选一或更少，按诊断结论）。

## §2 方案（Plan）

**诊断优先（禁止先改）**。按下面顺序取数据，得出结论后再决定改哪里：

| 步骤 | 做什么 | 产出 |
|---|---|---|
| D-1 | 进 Play，取 `Knight_Unit` 的 `UnitHealth`，直接调用 `TakeHit(new DamageInfo{ amount = 9999 })` 把血打空 | 日志 |
| D-2 | 立刻读 `Animator.GetCurrentAnimatorStateInfo(0)`：`IsName("Downed")`、`normalizedTime`、`shortNameHash`；并读 `GetCurrentAnimatorClipInfo(0)` 的 clip 名与 weight | `after_death_animator.txt` |
| D-3 | 读 `DownedRescue.IsDown`、`UnitHealth.dead`、`DownedRescue.AllDowned.Count`、`animator.enabled`、`animator.GetBool/GetTrigger` 无法读触发器的部分用 `hidden` 判断（可读 `GetCurrentAnimatorStateInfo` 即可） | `after_death_logic.txt` |
| D-4 | 查控制器里 `Downed` 状态的 `motion` GUID 与 `OC_Knight.overrideController` 中该 clip 的映射；确认 `Downed/Hit_A/Interact/Rise` 是否真的共用同一 clip | `clip_mapping.txt` |
| D-5 | 对照 `DownedRescue.OnDowned()` 是否真的执行（加 1 行 `Debug.Log` 或读已有的 `GameData` 消息 / `AllDowned` 数量） | `onDowned_trace.txt` |

**结论分支**

| 诊断结果 | 修法 | 风险 |
|---|---|---|
| `OnDowned` 没执行（`IsDown=false`、`AllDowned` 为空） | 修 `UnitHealth.OnDeath` 触发或 `DownedRescue` 订阅时机 | 常规 |
| 进了 `Downed` 但 `clip weight=0` / clip 为空 | 若 `OC_*` 里该 clip 被映射成空 clip → 报告（不改覆盖器） | 停 |
| 进了 `Downed` 但画面像 `Interact`/发呆 | 控制器 `Downed` 的 `motion` 指错（四状态共用 clip）→ 只在控制器里把 `Downed` 改成正确 clip；若需要新 clip → 停 | 常规 |
| `Die` 触发器被其它过渡抢走（AnyState 顺序） | 调整 `AnyState` 顺序或 `Downed` 的 `InterruptionSource` | 常规 |

**数据流**：`UnitHealth.hp → 0 → OnDeath → DownedRescue.OnDowned → brain/agent 禁用 → Animator.SetTrigger("Die") → AnyState → Downed`

**影响面清单**

| 项 | 内容 |
|---|---|
| 新增文件 | `Docs/QA/Evidence/DOWNED-01/**`、`Docs/Handoff/REPORT_DOWNED-01.md` |
| 修改文件 | 诊断后确定；上限 2 个，必须是 `DownedRescue.cs` / `UnitHealth.cs` / `PlayerBase.controller` 之一或之二 |
| 字符串引用 | `Die` / `Downed` / `Revive` / `Rise` |
| 场景/预制体 | 不改；预制体只读检查 |
| `.meta` | 无 |

**实施顺序**：D-1 → D-5 取证 → 结论 → 最小修复 → 复跑验证 → 证据落盘。

**风险与回滚**：最大风险是“看着像修好了其实只是把状态强制切过去”。必须用 D-2 的 clip 名 + weight 证明画面真的在播倒地动画。回滚 = 还原改动文件。

## §3 验证（Validation）

| AC | Given / When / Then | 断言 ID | 证据 |
|---|---|---|---|
| AC-1 | 给定 Play 中，当 `Knight_Unit` 血量打到 0，则 0.3s 内 `Animator` 当前状态为 `Downed`、clip 非空、weight > 0.9 | `Downed.AnimatorState` | `Docs/QA/Evidence/DOWNED-01/after_death_animator.txt` |
| AC-2 | 同一次死亡后，`DownedRescue.IsDown=true`、`UnitHealth.dead=true`、`AllDowned.Count` 增加 1 | `Downed.LogicState` | `after_death_logic.txt` |
| AC-3 | 给定倒地单位，当调用 `Revive(1f)`，则 `Die→Downed→Revive→Rise→Locomotion` 链路走通、`IsDown=false` | `Downed.RevivePath` | `revive_trace.txt` |
| AC-4 | 给定诊断步骤 D-1~D-5，则每个步骤都有原始输出（不是结论转述），且能定位到唯一根因 | `Downed.RootCause` | `onDowned_trace.txt`、`clip_mapping.txt` |
| AC-5 | 给定最终 diff，则只改 §2 允许的文件，且 SPEC01 编辑器内仍 12/0 | `all` | `git diff --stat`、`editor_runN/…Summary.json` |

**运行次数**：倒地/复活各 3 次（时序类，N≥3），每次记录状态名与时间戳。

**自动化优先**：全部用 `execute_code` 在 Play 中驱动，无需人工点击；目检 1 项（画面是不是“像倒下”而不是穿模/僵住）。

**必须保存的证据**：D-1~D-5 原始输出、3 次死亡/复活记录、`git diff`、SPEC01 一次。

## §4 执行信息

| 项 | 内容 |
|---|---|
| 前置 | Unity 打开、`is_compiling=false`；无其它 AI 驱动 |
| 允许改动 | 诊断后确定（≤2 文件，限 `DownedRescue.cs`/`UnitHealth.cs`/`PlayerBase.controller`）+ 证据 + REPORT |
| 允许读取 | 本工单、`DownedRescue.cs`、`UnitHealth.cs`、`UnitBrain.cs`、`PlayerBase.controller`、`OC_*.overrideController`（只读） |
| 绝不触碰 | `OC_*`、场景、预制体、`.asset`、`ProjectSettings`、`Packages`、`CURRENT.md`、其他 TASK/REPORT、canonical 证据 |
| MCP 权限 | `execute_code`、`execute_menu_item`（仅 `Tools/QA/In-Editor/*`）、`read_console`、`refresh_unity`、资源读取；其余写工具禁用 |
| 熔断 | 需要改覆盖器/场景/预制体；需要新 clip 资源；3 次复现都拿不到一致结论（写“无法复现”+ 建议步骤） |
| 执行预算 | 诊断 1 轮 + 修复 1 轮；不跑 batch |

## §5 学习产出

| 项 | 要求 |
|---|---|
| 讲稿 | **不适用**：修复类小单，讲解点并入 REPORT §3（“为什么会进状态却不播动画”这一类问题的排查路径） |
| 三句话版本 | 写进 REPORT §3 |

## 自包含声明

本工单不依赖 `CURRENT.md` 或其他任务书即可执行。信息不足时在 REPORT 写“信息不足”并列缺项。
