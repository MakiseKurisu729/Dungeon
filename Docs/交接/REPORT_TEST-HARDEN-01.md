# REPORT — TEST-HARDEN-01：探针开跑前等编译稳定 + 域重载丢失看门狗

> 对应 `Docs/Handoff/TASK_TEST-HARDEN-01.md`。节次与 `Docs/Handoff/workflow-rules.md` §2 一致。
> **本单为所有者叫停后的“部分完成”报告：故障注入未执行，AC-2/AC-3 未完成验收。**

## 1. 结果摘要

`部分完成` —— R-1 的 preflight 已实现并通过 AC-1（实测等待 11.5s / 11.7s，含域重载后自动重布线）；R-2/R-3 的看门狗代码已写入四个文件但**未做故障注入验证**；AC-3 三套正常计数未完成（DIAG 一轮在 23:03 中断，且看门狗修复后未三套复跑）。中断原因：所有者叫停，避免继续消耗与 Unity 实例混乱。

## 2. AC 回填表

| AC | 结果 | 断言 ID | 关键数据 | 日志路径 |
|---|---|---|---|---|
| AC-1 | 通过 | `Harden.Preflight` | 改 `DIAG_BatchAcceptance.cs` 后立刻点 `Tools/QA/In-Editor/SPEC01 (green)`：Unity Editor.log 含 `preflight ok (waited=11.5s)` 与 `preflight ok (waited=11.7s)`；两次都含 `preflight re-armed after reload; task=SPEC01`（编译→域重载→继续等→再进 Play） | `Docs/QA/Evidence/TEST-HARDEN-01/preflight.txt` |
| AC-2 | **未运行** | `Harden.Watchdog` | 未做“Play 中触发重编译”的故障注入（所有者叫停）。看门狗路径只做过静态代码走查，无运行时证据 | — |
| AC-3 | **未完成** | `all` | 部分执行：SPEC01 `editor_run22` 12 pass / 0 fail（code=0，带 preflight）；FND00B06 `editor_run3` 12 pass / 0 fail / 2 observed（code=0，带 preflight）；DIAG `editor_run2` 只写出 5 条断言、无 `InEditorResult.txt`（23:03:21 第 5 条后 MCP 掉线、Unity 退出）。三套未在看门狗修复后统一复跑，不能按基线判定 | `Docs/QA/Evidence/SPEC-01/editor_run22/`、`Docs/QA/Evidence/FND-00B-06/editor_run3/`、`Docs/QA/Evidence/DIAG-SKILL/editor_run2/` |

- **运行次数**：AC-1 两次（均满足 waited>0）；AC-2 0 次；AC-3 每套各 1 次但 DIAG 无效，且修复后未重跑。
- **不给结论的项**：看门狗是否能在 3~5s 内退出 Play、是否不再误退编辑器 —— 均**未验证**。

## 3. 实现原理（面向所有者）

1. **R-1 preflight（已实现、已验证）**：`Run()` 不再因 `isCompiling/isUpdating` 直接拒绝，而是 `AssetDatabase.Refresh()` 后用 `EditorApplication.update` 每 200ms 采样，要求 `!isCompiling && !isUpdating && !isDomainReloadPending` **连续 2 次**成立才 `Proceed(task)`；超过 120s 写 `InEditorResult(code=3, reason=preflight-timeout)`。preflight 任务名与开始时间存 `SessionState`，域重载后 `[InitializeOnLoadMethod]` 自动重布线（实测 stayed 11.5s/11.7s，且确实“re-armed after reload”）。
2. **R-2/R-3 看门狗（已写入、未验证）**：三个 `*_Boot` 在 `Tick()` 中判断 `Active && !Finished && isPlaying`：
   - `started=true` 且 runner 找不到持续 ≥3s → 写 `code=3, reason=runner-lost-domain-reload` 并 `EditorApplication.isPlaying=false`；
   - `started=false` 且进 Play ≥10s 仍未创建 runner → 同一失败路径（首启失败）。
   - runner 存活 = 静态引用或 `SessionState` 里记的名字 `GameObject.Find` 能否找到；**不自动重建 runner**。
3. **与 TASK §2 的差异**：§2 A 的伪代码写“若 `isCompiling || isPlayingOrWillChangePlaymode` → 拒绝”，与 R-1/AC-1“改完 .cs 立刻点菜单要先等编译”自相矛盾。本实现按 AC 为准：Play 态仍拒绝，编译中改为等待。需大脑确认此取舍。
4. **已发现并修复、但同样未验证的严重缺陷（23:03 事故根因）**：原 FND `ExitWithCode` 不清 `FND00B06.Active`、也不清 `Started/RunnerName`；FND 一轮跑完后这些标记残留。下一轮 DIAG 进 Play 时，FND 的看门狗看到 `started=true` 而 runner 已被销毁，3s 后触发 `WatchdogFail` → `QA_InEditorMenu.TryFinish("FND00B06", …)` 因当前任务不是 FND 而返回 false → 走到 `EditorApplication.Exit(3)`，**把整个 Unity 编辑器关掉**（23:03:23 左右，`Editor.log` 可见 `[FND00B06][WATCHDOG] runner missing >= 3s` 后进程 8000 退出）。
   修复：三个 `ExitWithCode` 统一清 `Started/Finished/Active/RunnerName`；三个 `EnterPlayMode` 增加 `ClearStaleRunFlags()`——上一轮残留标记 + runner 不存在时清掉，不再拿历史标记驱动本轮看门狗。**该修复未经 AC-2 验证。**
5. **关键代码位置（行号取停止令时的磁盘状态）**：
   - `Assets/_Project/Tests/QA/Editor/QA_InEditorMenu.cs:82` —— `Run()` 写 preflight SessionState 并布线。
   - `…/QA_InEditorMenu.cs:161` —— `PreflightTick()` 200ms 采样 / 连续 2 次 / 120s 超时。
   - `…/QA_InEditorMenu.cs:209` —— `IsEditorStable()`（含 `isDomainReloadPending` 反射）。
   - `…/QA_InEditorMenu.cs:312` —— `TryFinish(task, code, reason)` 重载；`:336` `WriteResultFile` 统一落盘（新增 `reason=` 行）。
   - `…/SPEC01/Editor/SPEC01_BatchAcceptance.cs:182`（Tick）/ `:239`（WatchdogFail）/ `:167`（ClearStaleRunFlags）。
   - `…/FND00B/Editor/FND00B06_BatchAcceptance.cs:121` / `:173` / `:105`；`…/DIAG/Editor/DIAG_BatchAcceptance.cs:142` / `:194` / `:127`。
   - **临时代码**：`…/QA_InEditorMenu.cs:93 RecoverMcpBridge()` —— 恢复 MCP 桥用的 `-executeMethod` 入口，**不属于本单需求**，建议复核后删除。

## 4. 受影响模块导航

| 模块 | 职责 | 入口（类 / 方法） |
|---|---|---|
| 编辑器菜单驱动 | 闸门、preflight、环境变量、调用三套 Run | `QA_InEditorMenu.Run / PreflightTick / Proceed / TryFinish` |
| 结果落盘 | `InEditorResult.txt`（task/code/reason/envAfter） | `QA_InEditorMenu.WriteResultFile` |
| SPEC01 接续 | runner 创建 / 退出 Play / 看门狗 | `SPEC01_Boot.Tick/TryStartRunner/WatchdogFail` |
| FND00B06 接续 | 同上 | `FND00B06_Boot.*` |
| DIAG 接续 | 同上 | `DIAG_Boot.*` |

数据流：`菜单点击 → GateForLaunch → AssetDatabase.Refresh → PreflightTick(200ms×连续2次) → Proceed(设 env/SessionState) → *_BatchAcceptance.Run → EnterPlaymode → *_Boot.Tick(看门狗) → ExitWithCode → TryFinish(写 reason) → 退出 Play`。

**改动文件清单**（与 TASK §4 允许改动比对）

| 文件 | 改了什么 | 验证状态 |
|---|---|---|
| `Assets/_Project/Tests/QA/Editor/QA_InEditorMenu.cs` | R-1 preflight 全套；`TryFinish` 增加 `reason`；抽 `WriteResultFile`；`PassGate→GateForLaunch`（编译中改为等待）；**额外** `RecoverMcpBridge()` 恢复入口 | preflight 已验证；`RecoverMcpBridge` 仅为恢复环境，未验证 |
| `Assets/_Project/Tests/SPEC01/Editor/SPEC01_BatchAcceptance.cs` | runner 名/进 Play 时间入 SessionState；3s 丢失看门狗 + 10s 首启看门狗；`WatchdogFail`；`ClearStaleRunFlags`；退出清理 | **未验证** |
| `Assets/_Project/Tests/FND00B/Editor/FND00B06_BatchAcceptance.cs` | 同上（含补清 `ActiveKey`） | **未验证** |
| `Assets/_Project/Tests/DIAG/Editor/DIAG_BatchAcceptance.cs` | 同上 | **未验证** |

## 5. 证据清单

| 文件 | 内容 |
|---|---|
| `Docs/QA/Evidence/TEST-HARDEN-01/preflight.txt` | AC-1：Editor.log 摘录，`preflight ok waited=11.5s/11.7s` + 两次 `re-armed after reload` |
| `Docs/QA/Evidence/SPEC-01/editor_run22/` | SPEC01 带 preflight 的完整一轮（12/0，code=0） |
| `Docs/QA/Evidence/FND-00B-06/editor_run3/` | FND00B06 带 preflight 的完整一轮（12/0/2obs，code=0） |
| `Docs/QA/Evidence/DIAG-SKILL/editor_run2/` | DIAG 中断现场：仅 5 条断言，无 InEditorResult（其中 `Diag.Input.FirstExit` 为 product_fail，属既有口径） |
| `Docs/QA/Evidence/SPEC-01/editor_run18..21/` | 实现过程中的 preflight 运行（18/20 为 AC-1 注入编译的两次，19/21 为普通轮） |

## 6. 学习讲稿

不适用（测试基础设施健壮性）。三句话：
- 菜单开跑前必须先等编译/域重载稳定，否则会拿旧程序集进 Play。
- 域重载会清掉 runner，看门狗用“3s 找不到 runner”判失败并留 `reason`，不自动重建。
- 看门狗必须只对本轮 run 生效，历史 SessionState 标记会把下一套测试误杀。

## 7. 未决项

- **AC-2 故障注入未执行**：看门狗“3~5s 内退出 Play、写 `runner-lost-domain-reload`、不退出编辑器”无运行时证据；**不能合并为已验证**。
- **AC-3 未完成**：DIAG 缺一轮有效结果；三套未在看门狗修复后统一复跑。
- **23:03 事故**：FND 残留标记 + 看门狗误判导致 `EditorApplication.Exit(3)`，Unity 进程 8000 退出。已改代码修复（`ClearStaleRunFlags` + 退出清理），但**未验证**。事故现场见 `DIAG-SKILL/editor_run2/`。
- **Unity 实例**：本会话 23:17:36 启动过 PID 44968（恢复用）、23:24:42 启动 PID 15396（`-executeMethod QA_InEditorMenu.RecoverMcpBridge` 恢复 MCP）；停止令后未再操作、未杀进程。按所有者指令，实例交由所有者处理。
- **临时代码** `QA_InEditorMenu.RecoverMcpBridge()`（:`93`）建议删除或明确保留理由。
- **取舍待确认**：TASK §2 A 伪代码与 AC-1 冲突，本实现按 AC 等待编译，需大脑复核。

## 8. 建议提交

**暂不提交。** 保留工作区现场供大脑/所有者决定“回滚 watchdog 或继续验证”。若决定继续，建议在 AC-2/AC-3 补齐后使用：

```text
test(qa): 编辑器内探针开跑前等编译稳定，并为域重载丢失 runner 加看门狗
为什么：菜单在编译/域重载窗口点击会拿旧程序集进 Play，且 play 中域重载会静默丢 runner
```

## 9. 消耗记录

- 钱包：`Go 订阅`
- 模型完整 ID：`opencode-go/deepseek-v4.1-flash`

## 10. 回执（≤6 行）

1. 我还持有 Unity 进程：`PID 15396`（23:24:42 由本会话启动，用于恢复 MCP），停止令后未杀。
2. 最后成功的 Unity 操作：23:26:53 `SPEC01` 编辑器内跑完（`editor_run22`，code=0，12/0）。
3. 最后一次调用 MCP 是同一轮 SPEC01 菜单触发；之后只有 shell 读文件，无 Unity 操作。
4. Play 状态：按该轮 `TryFinish` 路径应已退出 Play；停止令后**未再验证**，不再调用 MCP。
5. 未做：故障注入、三套复跑、回滚、git、删除证据。
