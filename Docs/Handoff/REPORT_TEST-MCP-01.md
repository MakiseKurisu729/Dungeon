# REPORT — TEST-MCP-01：三套探针的“编辑器内运行”菜单入口

> 对应 `Docs/Handoff/TASK_TEST-MCP-01.md`。节次与 `Docs/Handoff/workflow-rules.md` §2 一致，**不得删节**；确实不适用的写“不适用”+ 一句理由。
> 禁止预填、推测或编造结果；未运行的写“未运行”。

## 1. 结果摘要

`完成` —— 新增 `Tools/QA/In-Editor/` 三个菜单并在**常开编辑器内**各跑 1 次：SPEC01 `pass=12 fail=0 exit=0`、FND00B06 `pass=12 fail=0 observed=2 exit=0`、DIAG `pass=6 fail=1 exit=2`（与各自 batch 基线逐项相等）；三套跑完 Unity 进程存活、回到 Edit 模式，canonical 证据 150 个文件 SHA256 全部未变，新证据只落 `editor_run1/`。

**追加（所有者批准并入本单，2026-11-05 晚）**：三处 launcher 的 `catch` 分支补上编辑器内钩子（先 `TryFinish("<ID>", 3)`，返回 true 即 return，batch 路径保持原样 `EditorApplication.Exit(3)`）；`QA_InEditorMenu` 的证据子目录改为 `editor_run<N>` 递增。改后重跑 SPEC01：`pass=12 fail=0 blocked=0 infraFail=0 observed=0 / ExitCode=0`，落 `editor_run3/`（第 1 次尝试因编辑器失焦 + Play 中域重载中断，`editor_run2/` 为空目录，现场见 `editor_run2_abort_note.txt`），Unity 进程存活、`is_playing=false`、`is_compiling=false`。AC-9 见 §2 追加行与 §2.1。

## 2. AC 回填表

| AC | 结果 | 断言 ID | 关键数据 | 日志路径 |
|---|---|---|---|---|
| AC-1 | 通过 | `QA.Menu.Present` | `mcpforunity://menu-items` 中 `Tools/QA` 前缀命中 3 项：`Tools/QA/In-Editor/DIAG`、`…/FND00B06`、`…/SPEC01 (green)`，无其它；3 项均由 `execute_menu_item` 实际触发成功 | `Docs/QA/Evidence/TEST-MCP-01/menu_list.txt` |
| AC-2 | 通过 | `QA.InEditor.SPEC01.Parallel` | pass=12 / fail=0 / blocked=0 / infraFail=0 / observed=0 / ExitCode=0，与基线逐项相等；`InEditorResult.txt` code=0；token `[QA][INEDITOR] done task=SPEC01 code=0` | `…/SPEC-01/editor_run1/SPEC01_Summary_run1.json`、`…/TEST-MCP-01/editor_SPEC01.log` |
| AC-3 | 通过 | `QA.InEditor.FND00B06.Parallel` | pass=12 / fail=0 / observed=2 / ExitCode=0，与基线逐项相等；token `[QA][INEDITOR] done task=FND00B06 code=0` | `…/FND-00B-06/editor_run1/06_Summary_run1.json`、`…/TEST-MCP-01/editor_FND00B06.log` |
| AC-4 | 通过 | `QA.InEditor.DIAG.Completed` | pass=6 / fail=1 / blocked=0 / observed=0 / ExitCode=2；token `[QA][INEDITOR] done task=DIAG code=2`；与 batch 基线计数及失败项完全一致，差异仅 `head`（batch `7cf874f7…` → 本次 `6872348b…`）与时间戳/路径 | `…/DIAG-SKILL/editor_run1/DIAG_Summary.json`、`…/TEST-MCP-01/editor_DIAG.log` |
| AC-5 | 通过 | `QA.InEditor.NoExit` | 三套跑完：`play_mode.is_playing=false`、`is_changing=false`、`is_compiling=false`、`is_domain_reload_pending=false`；Unity 进程 8000 存活（Responding=True，标题仍为 `Dungeon - DungeonWorld…`）；无 `EditorApplication.Exit` 触发 | `…/TEST-MCP-01/editor_state_after.json` |
| AC-6 | 通过 | `QA.Evidence.Isolated` | 跑前 canonical 三目录 150 文件、跑后 163 文件；跑前 150 条 SHA256 **逐条在跑后集合中存在且相同**（missing/modified = 0），新增 13 条全部位于 `SPEC-01/editor_run1`、`FND-00B-06/editor_run1`、`DIAG-SKILL/editor_run1` | `…/TEST-MCP-01/sha256_before.txt`、`sha256_after.txt` |
| AC-7 | 通过 | `QA.Assertions.Untouched` | 首轮 `git diff --stat -- Assets/_Project/Tests` = `6 files changed, 9 insertions(+)`；**并入 AC-9 后**为 `6 files changed, 15 insertions(+), 3 deletions(-)`（增量即三处 `catch` 各 +2/-1），变更行中含 `Assert` 的行数 = 0（断言方法体、阈值、夹具零改动）；新增未跟踪文件 1 个：`Assets/_Project/Tests/QA/`（含 `QA.meta`/`Editor.meta`/`QA_InEditorMenu.cs.meta`，Unity 自动生成） | `…/TEST-MCP-01/diff_assertions.txt`、`…/diff_catch.txt` |
| AC-8 | 通过 | `QA.Env.Clean` | 三套 `InEditorResult.txt` 的 `envAfter=` 显示 `SPEC01_MODE/SPEC01_RUN/SPEC01_EVIDENCE_TAG/FND00B06_RUN/DIAG_RUN/DIAG_MODE/QA_EVIDENCE_SUBDIR` 全为 `<null>`；`start` 行显示运行期注入值（SPEC01_MODE=green、SPEC01_RUN=1、QA_EVIDENCE_SUBDIR=editor_run1 等） | `…/TEST-MCP-01/editor_env_after.txt` |
| AC-9 | 通过 | `QA.InEditor.CatchHook` | 三处 `Run()` 的 `catch` 均在 `EditorApplication.Exit(3)` 上方插入 `if (QA_InEditorMenu.TryFinish("<ID>", 3)) return;`（SPEC01/FND00B06/DIAG 各 +2/-1 行）；编译 0 error；改后 SPEC01 重跑 12/0/exit0 → 编辑器内仍走 `TryFinish` 收尾；`git diff` 显示 catch 块内只增该两行与注释 | `…/TEST-MCP-01/diff_catch.txt`、`…/SPEC-01/editor_run3/`、`…/TEST-MCP-01/editor_run3_spec01.log` |
| AC-10 | 通过 | `QA.InEditor.RunInBackground` | `QA_InEditorMenu.Run` 在分派前设 `Application.runInBackground = true`（先记原值再置 true），start 日志同步输出 `runInBackground=True`；3 次编辑器内连跑均 `pass=12 fail=0 blocked=0 infraFail=0 / ExitCode=0`，其中 `editor_run8` 全程 353 条前台采样里 Unity 前台数 = **0**（前台钉住 OpenCode），仍跑完并写出 `InEditorResult.txt`；收尾 `is_playing=false`、`error CS` = 0。**副作用修复（2026-11-05 追加）**：编辑器里该赋值会写穿 `ProjectSettings.asset` 的 `runInBackground`（实测 0→1），故改为进 Play 前记原值（`QA.PrevRunInBackground`）、**收尾还原、不污染 ProjectSettings**（`TryFinish` 的 finally 与 `Run` 的 catch 两路都走幂等 `RestoreRunInBackground`，仅当原值为 false 时置回 false）；修复后 `editor_run11` 跑前/跑后 `ProjectSettings.asset` SHA256 完全一致（`12ADC38B…`），12/0/exit0 | `…/TEST-MCP-01/runinbackground.txt`、`…/runinbackground_restore.txt`、`…/focus_trace_run7.txt`、`…/focus_trace_run8.txt`、`…/SPEC-01/editor_run6..8/`、`…/SPEC-01/editor_run11/` |

- **运行次数**：每套 **1 次**（基础设施单，TASK §3 规定；batch 不变性由 AC-7 静态保证）。三套均首跑通过，无重试、无 `refuse` 记录（`read_console filter_text="refuse task="` = 0 条）。**AC-10 追加**：SPEC01 编辑器内再连跑 3 次（`editor_run6/7/8`），均 12/0/exit0，其中 `editor_run8` 为前台钉住 OpenCode 的全失焦跑。
- 未运行项：无。未跑 batchmode、未跑 Unity Test Runner（按 TASK 明确排除）。

### 2.1 AC-9 追加验证（catch 钩子 + 子目录递增）

**改法**（3 个文件，各 +2/-1 行；`git diff` 见 `Docs/QA/Evidence/TEST-MCP-01/diff_catch.txt`）

| 文件 | catch 块内新增 |
|---|---|
| `SPEC01_BatchAcceptance.cs:49-50` | `if (QA_InEditorMenu.TryFinish("SPEC01", 3)) return;` + `EditorApplication.Exit(3);` 保留 |
| `FND00B06_BatchAcceptance.cs:38-39` | 同上，`"FND00B06"` |
| `DIAG_BatchAcceptance.cs:41-42` | 同上，`"DIAG"` |

任务号已与 `QA_InEditorMenu` 内 `Run(task)`/`TryFinish(task, code)` 的取值逐一核对（`"SPEC01"`/`"FND00B06"`/`"DIAG"`，全部为 `StringComparison.Ordinal` 比较）。
`TryFinish` 内部在写结果前调用 `ClearSuiteEnvironment()`（7 个键置 null），并在 `finally` 里 `ResetFlags()`（清 `QA.InEditor.Active`/`QA.InEditor.Task`），因此**异常路径也不会残留环境变量或 SessionState**，下次 batch 不会误判；`TryFinish` 返回 false（无标记或 task 不匹配）时仍走原 `EditorApplication.Exit(3)`。

**子目录递增**：`QA_InEditorMenu.NextSubdir(task)` 扫描 `<canonical>/editor_run*` 取最大 N，返回 `editor_run<N+1>`；首次无历史目录 → `editor_run1`，扫描异常 → 回退 `DefaultSubdir`。`Run()` 内 `Environment.SetEnvironmentVariable(QA_EVIDENCE_SUBDIR, subdir)` 与 start 日志同步改用该值（改动仅限子目录命名相关行，见 `diff_subdir.txt`；`QA_InEditorMenu.cs` 为未跟踪新文件，`git diff` 不显示其内容，故单列行号摘录）。

**运行：第 1 次尝试（中断，如实记录）**

`18:24` 触发 `SPEC01 (green)`，`subdir=editor_run2`，Play 进入后日志停在 `[SPEC01] observer ready`，`Editor.log` 自 18:24:17 起 3 分钟无写入；`editor/state` 显示 `is_focused=false`、`is_playing=true`、`is_paused=false`，工程 `runInBackground: 0`。按 AGENTS §3.2.1 第 9 条把 Unity 置前台后，18:27:34 Unity 处理挂起的导入/编译并在 **Play 中域重载**（`last_domain_reload_after_unix_ms=1793874454224`），运行期 runner 丢失（控制台仅剩 `[SPEC01] boot; isPlaying=True; started=True`），该轮无法收尾；`editor_run2/` 由 `SPEC01_Evidence` 构造时创建但无文件（空目录，git 不跟踪）。完整现场：`editor_run2_abort_note.txt`、`editor_run2_abort.log`。

**运行：第 2 次（重试，通过）**

`manage_editor(action="stop")` 退出卡住的 Play（只改编辑器播放状态，不写场景/资产），确认 `is_playing=false`/`is_compiling=false`/`external_changes_dirty=false` 后再次触发同一菜单；`NextSubdir` 正确递增到 `editor_run3`（证明递增逻辑生效）。结果：

| 项 | 值 | 来源 |
|---|---|---|
| pass / fail / blocked / infraFail / observed | 12 / 0 / 0 / 0 / 0 | `…/SPEC-01/editor_run3/SPEC01_Summary_run1.json` |
| ExitCode | 0 | `…/editor_run3/SPEC01_ExitCode_run1.txt`；`InEditorResult.txt` `code=0` |
| 断言行数 | 12 | `…/editor_run3/SPEC01_Assertions.jsonl` |
| 控制台 token | `[QA][INEDITOR] done task=SPEC01 code=0` | 全文：`editor_run3_spec01.log`（670 行） |
| 耗时 | 18:30:44.002 → 18:31:25.977（≈42s，与 run1 的 42s 同量级） | Summary |
| 环境变量 | `envAfter` 7 键全 `<null>` | `InEditorResult.txt` |
| 跑完状态 | `is_playing=false`、`is_changing=false`、`is_compiling=false`、`is_domain_reload_pending=false` | `editor_state_after_run3.json` |
| Unity | 进程 8000 存活、Responding=True | `unity_process_after.json` |

**未做（并说明理由）**：未人为制造 launcher 异常来“实跑”catch 分支——触发它需要改代码或注入故障（改断言/注入都超出本单边界），故 AC-9 的 catch 分支以**静态 diff + 编译通过 + 同进程内 `TryFinish` 实跑返回 true 走通编辑器收尾**取证；catch 分支调用与 `ExitWithCode` 已在用的钩子完全同一行形式、同一 task 字符串。

**MCP 调用披露**（本单全部写类/唤醒类调用，无资产写入）：
`execute_menu_item` ×2（均为 `Tools/QA/In-Editor/SPEC01 (green)`）；`refresh_unity(mode=force, scope=scripts, compile=request)` ×1（首次导入，§3.2.1 第 9 条唤醒）；`manage_editor(action="stop")` ×1（退出被冻结的 Play，按第 9 条故障处置；不改场景/资产）。其余均为只读：`read_console`、`editor/state`、`menu-items`、进程查询。

### 2.2 AC-10 追加验证（runInBackground 运行期开关，2026-11-05 追加）

**为什么用运行时开关而不是 Player Settings**：`Edit > Project Settings > Player > Run In Background` 是**工程级资产改动**（`ProjectSettings/ProjectSettings.asset` 与平台子项），一次改动会对所有 batch 验收、`-batchmode` 跑法、乃至未来打包出的播放器永久生效——而 batchmode 下问题根本不存在（无失焦概念），打包出的游戏是否后台继续跑属于产品决策，不该由“让探针别卡死”这件事顺带决定。摆在 `QA_InEditorMenu.Run` 里的一行 `Application.runInBackground = true` 只影响**编辑器内菜单驱动的这一次试跑**（Unity 重启后回到工程默认），作用域、回滚成本与可解释性都最小，且与 §3.3 资产管辖权不冲突。

**改法**（唯一文件 `Assets/_Project/Tests/QA/Editor/QA_InEditorMenu.cs`，1 行 + 1 行注释）：

```csharp
// 编辑器内无人值守跑探针：失焦时不冻结 Play 帧循环（只改运行期开关，不动 Player Settings）。
Application.runInBackground = true;
```

位置在 `PassGate` 之后、`switch (task)` 分派之前——即三套 `*_BatchAcceptance.Run()` 共用的唯一入口，早于任何一套进 Play；start 日志在同一行追加 `; runInBackground=` + 实际值（本次三次均为 `True`）。`*_BatchAcceptance` / `*_Evidence` / 断言零改动。

**三次连跑（均 `execute_menu_item` → `Tools/QA/In-Editor/SPEC01 (green)`）**

| 跑次 | 时间 | 前台焦点条件 | 结果 | 证据 |
|---|---|---|---|---|
| run6 | 18:57:37 → 18:58:19 | 混合（对照） | 12/0/0/0/0，ExitCode 0 | `…/SPEC-01/editor_run6/` |
| run7 | 18:59:33 → 19:00:15 | 触发前 Win32 前台=OpenCode 且 `is_focused=false`；中途 24.5s 非 Unity 前台 | 同上 | `…/SPEC-01/editor_run7/`、`…/focus_trace_run7.txt` |
| run8 | 19:01:47 → 19:02:29 | **全程钉住**：250ms 一次把前台压回 OpenCode，353 条采样中 Unity 前台 = 0 | 同上 | `…/SPEC-01/editor_run8/`、`…/focus_trace_run8.txt` |

- 前台判定用 Win32 `GetForegroundWindow` + `GetWindowThreadProcessId`（PID 8000 = Unity，15784 = OpenCode），不依赖 `editor/state.is_focused`（后者在本次观测中出现过陈旧/自相矛盾的快照）。
- run7 的 24.5s 失焦窗口内落下 `Attack.DualPathUnified`（19:00:08.675）与 `Clock.*`、`Animation.PostCombatAndHit`（19:00:09.241）——帧驱动断言在失焦期间照常产出，这是“Play 帧循环没被冻结”的直接证据。
- run8 的 12 条断言时间戳 19:01:54.776 → 19:02:29.471 整段落在非 Unity 前台期间，且 `InEditorResult.txt` 正常写出（`code=0`）、`[QA][INEDITOR] done task=SPEC01 code=0`。
- 收尾态：`is_playing=false`、`is_changing=false`、`is_compiling=false`、`is_domain_reload_pending=false`、`is_focused=false`；`read_console filter_text="error CS"` = 0 条；Unity 进程 8000 存活，未触发 `EditorApplication.Exit`。
- **对照**：前一版在 `runInBackground: 0` 下的同类情形（`editor_run2`）失焦后帧循环停摆、Play 中域重载丢失 runner，只能人工 stop（见 `editor_run2_abort_note.txt`）。

**MCP 调用披露（AC-10 段）**：`execute_menu_item` ×3（均 `Tools/QA/In-Editor/SPEC01 (green)`）、`refresh_unity` ×1（改后触发脚本刷新/编译）、`read_console` / `editor/state` / `windows` / `tests` 等只读若干。无任何写工具、无资产改动、无 git 写操作，Unity 未关闭。

**更正（2026-11-05 追加，副作用修复）**：上文"只改运行期开关，不动 Player Settings / Unity 重启后回到工程默认"的判断**不成立**——编辑器内 `Application.runInBackground = true` 会写穿到 `ProjectSettings/ProjectSettings.asset` 的 `PlayerSettings.runInBackground`（实测 0→1，`editor_run9/10` 各写一次）。已改为"进 Play 前记原值 → 收尾还原（仅当原值为 false 时置回 false）"，`TryFinish` 的 `finally` 与 `Run` 的 `catch` 两路都调幂等的 `RestoreRunInBackground()`；修复后 `editor_run11` 跑前/跑后 `ProjectSettings.asset` SHA256 一致（`12ADC38B…`），即不再新增污染。**遗留**：`editor_run9/10` 造成的既有 `runInBackground: 0→1` 仍留在工作区，按指令未 `git checkout`，待所有者还原后补跑一次以取证"原值为 false"分支；详见 `Docs/QA/Evidence/TEST-MCP-01/runinbackground_restore.txt`。

## 3. 实现原理（面向所有者）

1. **根因 / 目标**：三套探针此前只有 batchmode 入口——`*_Boot.ExitWithCode` 结尾固定 `EditorApplication.Exit(code)`，编辑器里跑一次就会把常开的 Unity 杀掉；且证据固定写 canonical 目录，会覆盖历史基线。本单建立的能力是：**同一份探针、两套出口**——batch 出口照旧退出进程，编辑器出口不退出、证据分流、结果落文件+控制台 token。
2. **备选方案与为什么没选**：方案 A（三份 `*_InEditorAcceptance.cs`，各复制一遍进 Play/场景/收尾逻辑）被弃用——三处重复的驱动逻辑，改一次要改三处，且容易与 batch 入口分叉；方案 B（一份共享驱动 `QA_InEditorMenu` + 三个 `ExitWithCode` 各插一行钩子 + 三个 Evidence 类加子目录能力）被选中——复用现有唯一入口，batch 行为**零行改动**（钩子返回 false 即走原路径）。
3. **实际数据流**：
   `execute_menu_item("Tools/QA/In-Editor/SPEC01 (green)") → QA_InEditorMenu.Run("SPEC01")`：
   - 闸门：`isCompiling` / `isUpdating` / `isPlaying||isPlayingOrWillChangePlaymode` / 当前场景 `isDirty` → 任一命中即 `[QA][INEDITOR] refuse task=… reason=…` 并返回；
   - 置 `SessionState: QA.InEditor.Active=true, QA.InEditor.Task=<ID>`，清空 7 个套件环境变量后注入 `SPEC01_MODE=green / SPEC01_RUN=1 / QA_EVIDENCE_SUBDIR=editor_run1`（FND00B06 注入 `FND00B06_RUN=1`；DIAG 注入 `DIAG_MODE=probe / DIAG_RUN=1`）；
   - 调既有 `*_BatchAcceptance.Run()`（打开 `DungeonWorld` → `EnterPlaymode`）→ 探针断言（代码零改动）→ `*_Evidence` 因 `QA_EVIDENCE_SUBDIR` 把 `_dir` 改到 `<canonical>/editor_run1` → 写 Summary/Assertions/ExitCode → `EditorApplication.isPlaying=false`；
   - `EnteredEditMode → *\\*_Boot.ExitWithCode` → `if (QA_InEditorMenu.TryFinish("<ID>", code)) return;` → `TryFinish` 写 `InEditorResult.txt`（task/code/finishedAt/unityVersion/scenario/evidenceSubdir/envAfter）、打 `[QA][INEDITOR] done task=<ID> code=<n>`、清环境变量与 SessionState，返回 true → 调用方 return，**不执行** `EditorApplication.Exit`；batch 路径无 SessionState 标记 → 返回 false → 原样 `EditorApplication.Exit(code)`。
   与 TASK §2 数据流一致。
4. **关键代码位置（3–5 条）**：
   - `Assets/_Project/Tests/QA/Editor/QA_InEditorMenu.cs:34-47` —— 三个 `[MenuItem]`（`Tools/QA/In-Editor/SPEC01 (green)` / `FND00B06` / `DIAG`）。
   - `Assets/_Project/Tests/QA/Editor/QA_InEditorMenu.cs:53-117` —— `Run(task)`：先闸门，再置 SessionState、清残留环境变量、按套件注入变量，最后调既有 `*_BatchAcceptance.Run()`。
   - `Assets/_Project/Tests/QA/Editor/QA_InEditorMenu.cs:119-158` —— `TryFinish(task, code)`：编辑器内出口（写 `InEditorResult.txt` + token + 清标记/环境变量），返回 false 时 batch 行为不变。
   - `Assets/_Project/Tests/QA/Editor/QA_InEditorMenu.cs:165-200` —— `PassGate`：编译中/刷新中/播放态/脏场景四道拒绝，避免 EnterPlaymode 与 OpenScene 保存对话框。
   - `SPEC01_BatchAcceptance.cs:168` / `FND00B06_BatchAcceptance.cs:109` / `DIAG_BatchAcceptance.cs:128` —— 三处一行钩子，插在 `EditorApplication.Exit(code)` 正上方。
   - `SPEC01_BatchAcceptance.cs:49` / `FND00B06_BatchAcceptance.cs:38` / `DIAG_BatchAcceptance.cs:41` —— **追加**：三处 `Run()` 的 `catch` 里同一个钩子（固定码 3），编辑器内只写失败结果不退 Unity。
   - `QA_InEditorMenu.cs:244-268` —— **追加**：`NextSubdir(task)` 扫描 `editor_run*` 取下一个序号；`:62` 用它作为 `QA_EVIDENCE_SUBDIR`。
   - `SPEC01_Evidence.cs:42` / `FND00B06_Evidence.cs:36` / `DIAG_Evidence.cs:37` —— `QA_EVIDENCE_SUBDIR` 非空时把 `_dir` 指向子目录**之后**才 `CreateDirectory`，canonical 不被写入或删除（关键：FND00B06 `run==1` 与 DIAG 构造时会删断言文件，若不先改 `_dir` 会删掉 canonical 文件）。
5. **边界 / 失败模式**：
   - 编译中/进入 Play 中触发菜单 → 拒绝并记 warning，不会进半态；
   - 当前场景有未保存修改 → 拒绝（否则 `OpenScene(Single)` 弹保存框会把编辑器挂死）；
   - 套件变量跨轮残留 → `Run` 开头与 `TryFinish` 结尾各清一次，7 个键全置 null；
   - 跨套件误触发 → `TryFinish` 校验 `SessionState[QA.InEditor.Task] == task`，不匹配返回 false，避免 A 套件收尾误吞 B 套件的退出；
   - 首套跑完后旧探针的 `EditorApplication.update` 订阅仍在（既有代码），但 `Finished=false` 且 `Started=true`，在后续套件的 Play 中均为 no-op（本次 FND00B06、DIAG 连续运行未出现异常）。
6. **与计划的差异及原因**：
   - **闸门多了 `dirtyScene` 一项**：TASK R-6 只要求编译/播放态；`OpenScene(Single)` 遇脏场景会弹保存对话框（TASK §2 风险 2 已点名），故在编辑器侧多一道拒绝，未触碰 batch 路径。
   - **导入触发方式**：写文件后 Unity 处于未聚焦状态，`editor/state.external_changes_dirty=true` 但未自动导入（Editor.log 17:24 后无 refresh 记录；`SetForegroundWindow` 被前台锁拒绝，返回 False）。按 §6.2 停下询问，所有者选择“手动点一下 Unity 窗口”，聚焦后 Unity Auto Refresh 导入并编译（域重载完成、0 编译错误），随后才触发菜单。**未调用 `refresh_unity`**，本单全部 MCP 写类调用仅 3 次 `execute_menu_item`。
   - 其余与 TASK §2 一致：断言/阈值/夹具零改动（`git diff` 仅 +9 行）。
7. **三句话版本**（答辩时先讲这三句）：
   - batch 与编辑器内共用同一入口，只在退出点分叉（`TryFinish` 返回 true 就 return，不 `EditorApplication.Exit`）。
   - 证据靠环境变量 `QA_EVIDENCE_SUBDIR` 分流到 `editor_run1/`，canonical 目录一个字节都不动。
   - 这样 MCP 才能在常开编辑器里跑回归而不杀掉编辑器，跑完还能继续干活。

## 4. 受影响模块导航

| 模块 | 职责 | 入口（类 / 方法 / 场景对象） |
|---|---|---|
| 编辑器内菜单驱动 | 闸门 + 环境变量 + SessionState + 调既有入口 | `QA_InEditorMenu.Run` / `RunSPEC01` / `RunFND00B06` / `RunDIAG` |
| 编辑器内收尾钩子 | 写 `InEditorResult.txt`、打 token、清标记与环境变量 | `QA_InEditorMenu.TryFinish` |
| SPEC-01 batchmode 入口 | 打开场景 → EnterPlaymode → 探针；退出点分叉 | `SPEC01_BatchAcceptance.Run` / `SPEC01_Boot.ExitWithCode` |
| FND-00B-06 batchmode 入口 | 同上 | `FND00B06_BatchAcceptance.Run` / `FND00B06_Boot.ExitWithCode` |
| DIAG-SKILL batchmode 入口 | 同上 | `DIAG_BatchAcceptance.Run` / `DIAG_Boot.ExitWithCode` |
| 证据写入（三套） | 目录计算（canonical 或 `<canonical>/<subdir>`）、Summary/Assertions/ExitCode | `SPEC01_Evidence` / `FND00B06_Evidence` / `DIAG_Evidence` 构造函数 |

数据流：`execute_menu_item → QA_InEditorMenu.Run（闸门/环境变量/SessionState）→ *_BatchAcceptance.Run（OpenScene → EnterPlaymode）→ *_ProbeRunner（断言不变）→ *_Evidence（写 editor_run1/）→ EnteredEditMode → *_Boot.ExitWithCode → QA_InEditorMenu.TryFinish（写结果/token/清理）→ return（不 Exit）`

**改动文件清单**（与 TASK §4 允许改动逐项比对）

| 文件 | 改了什么 |
|---|---|
| `Assets/_Project/Tests/QA/Editor/QA_InEditorMenu.cs`（新增） | 3 个菜单项、`Run` 闸门与注入、`TryFinish` 收尾钩子、环境变量快照/清理、证据目录映射；**追加**：`NextSubdir(task)` 生成递增 `editor_run<N>`（只读扫描已有目录），`Run` 内改用它 |
| `Assets/_Project/Tests/SPEC01/Editor/SPEC01_BatchAcceptance.cs` | `SPEC01_Boot.ExitWithCode` 内 `EditorApplication.Exit(code)` 前 +1 行 `if (QA_InEditorMenu.TryFinish("SPEC01", code)) return;`；**追加**：`Run()` 的 `catch` 内同款钩子（固定码 3） |
| `Assets/_Project/Tests/FND00B/Editor/FND00B06_BatchAcceptance.cs` | 同上，`"FND00B06"` |
| `Assets/_Project/Tests/DIAG/Editor/DIAG_BatchAcceptance.cs` | 同上，`"DIAG"` |
| `Assets/_Project/Tests/SPEC01/Runtime/SPEC01_Evidence.cs` | 构造函数 +2 行：读 `QA_EVIDENCE_SUBDIR`，非空则 `_dir` 追加子目录（在 `CreateDirectory` 之前） |
| `Assets/_Project/Tests/FND00B/Runtime/FND00B06_Evidence.cs` | 同上 |
| `Assets/_Project/Tests/DIAG/Runtime/DIAG_Evidence.cs` | 同上 |
| `Docs/QA/Evidence/TEST-MCP-01/**` | 本单证据（11 个文件） |
| `Docs/Handoff/REPORT_TEST-MCP-01.md` | 本报告 |
| `Assembly-CSharp-Editor.csproj`（Unity 自动生成，18:07:54） | 新增 `.cs` 引起的工程文件重生成：加入 `QA_InEditorMenu.cs`，并把 `D:\Unity\…` 归一为 `D:\unity\…`。按 AGENTS §3.1 视为影响面清单的隐含延伸；非我手改 |

未触碰：场景、预制体、Animator Controller、`.asset`、`ProjectSettings/**`、`Packages/**`、`CURRENT.md`、其它 `TASK_*`/`REPORT_*`、既有 canonical 证据（SHA256 已证）。无任何 git 写操作。

## 5. 证据清单

| 文件 | 内容 |
|---|---|
| `Docs/QA/Evidence/TEST-MCP-01/menu_list.txt` | AC-1：`menu-items` 过滤 `Tools/QA` 的 3 项 |
| `Docs/QA/Evidence/TEST-MCP-01/editor_SPEC01.log` | AC-2：SPEC01 编辑器内运行原始日志（638 行，12 条 ASSERT、1 条 SUMMARY） |
| `Docs/QA/Evidence/TEST-MCP-01/editor_FND00B06.log` | AC-3：FND00B06 原始日志（578 行） |
| `Docs/QA/Evidence/TEST-MCP-01/editor_DIAG.log` | AC-4：DIAG 原始日志（519 行） |
| `Docs/QA/Evidence/TEST-MCP-01/sha256_before.txt` / `sha256_after.txt` | AC-6：canonical 三目录全量 SHA256（150 → 163） |
| `Docs/QA/Evidence/TEST-MCP-01/editor_state_after.json` | AC-5：跑完时刻 `editor/state` 原样响应 |
| `Docs/QA/Evidence/TEST-MCP-01/diff_assertions.txt` | AC-7：`git status`/`diff --stat`/`diff`/断言行核查 |
| `Docs/QA/Evidence/TEST-MCP-01/editor_env_after.txt` | AC-8：三套 `envAfter` 全 `<null>` |
| `Docs/QA/Evidence/TEST-MCP-01/Summary.md` | 三套结果、AC 对照、文件说明 |
| `Docs/QA/Evidence/TEST-MCP-01/ExitCode.txt` | 三套退出码汇总（0/0/2，聚合 2） |
| `Docs/QA/Evidence/SPEC-01/editor_run1/` | `SPEC01_Summary_run1.json`、`SPEC01_Assertions.jsonl`、`SPEC01_ExitCode_run1.txt`、`SPEC01_Runs.jsonl`、`InEditorResult.txt` |
| `Docs/QA/Evidence/FND-00B-06/editor_run1/` | `06_Summary_run1.json`、`06_Assertions.jsonl`、`06_ExitCode_run1.txt`、`InEditorResult.txt` |
| `Docs/QA/Evidence/DIAG-SKILL/editor_run1/` | `DIAG_Summary.json`、`DIAG_Assertions.jsonl`、`DIAG_ExitCode.txt`、`InEditorResult.txt` |
| `Docs/QA/Evidence/TEST-MCP-01/diff_catch.txt` | AC-9：三处 `catch` 的 `git diff`（含 `ExitWithCode` 原有钩子对照） |
| `Docs/QA/Evidence/TEST-MCP-01/diff_subdir.txt` | AC-9：`QA_InEditorMenu.NextSubdir` 与 `Run` 子目录赋值行摘录（该文件未跟踪，`git diff` 不含） |
| `Docs/QA/Evidence/TEST-MCP-01/editor_run3_spec01.log` | AC-9：SPEC01 重试运行的原始日志（670 行，含 `start … done task=SPEC01 code=0`） |
| `Docs/QA/Evidence/TEST-MCP-01/editor_run2_abort.log` | AC-9：第 1 次尝试中断现场（596 行，至 `observer ready` + 域重载/传输层日志） |
| `Docs/QA/Evidence/TEST-MCP-01/editor_run2_abort_note.txt` | AC-9：中断-唤醒-重试时间线与原因分析（含 `runInBackground: 0`、`last_domain_reload_after`） |
| `Docs/QA/Evidence/TEST-MCP-01/editor_state_after_run3.json` | AC-9：重试跑完时刻 `editor/state` 原样响应 |
| `Docs/QA/Evidence/TEST-MCP-01/unity_process_after.json` | AC-9：跑完 Unity 进程（8000，Responding=True） |
| `Docs/QA/Evidence/TEST-MCP-01/git_status_after.txt` | AC-9：改动后 `git status --porcelain`（确认只多本单改动 + `editor_run3/`） |
| `Docs/QA/Evidence/SPEC-01/editor_run3/` | AC-9：`SPEC01_Summary_run1.json`（12/0）、`SPEC01_Assertions.jsonl`（12）、`SPEC01_ExitCode_run1.txt`（0）、`SPEC01_Runs.jsonl`、`InEditorResult.txt`（code=0） |
| `Docs/QA/Evidence/TEST-MCP-01/runinbackground.txt` | AC-10：改动行号、编译闸门、3 次连跑计数、失焦区间与断言时间戳对照、收尾态（含 `runInBackground=True` 的 start 行摘录） |
| `Docs/QA/Evidence/TEST-MCP-01/focus_trace_run7.txt` | AC-10：250ms 前台采样（`PID\|标题`），含 24.5s 非 Unity 前台区间 |
| `Docs/QA/Evidence/TEST-MCP-01/focus_trace_run8.txt` | AC-10：前台钉住跑采样，353 条中 Unity 前台 0 条 |
| `Docs/QA/Evidence/SPEC-01/editor_run6/`、`editor_run7/`、`editor_run8/` | AC-10：三次连跑各自的 `SPEC01_Summary_run1.json`（均 12/0）、`SPEC01_Assertions.jsonl`（12）、`SPEC01_ExitCode_run1.txt`（0）、`InEditorResult.txt`（code=0） |

说明：`editor_*.log` 由 Unity `Editor.log` 按 `[QA][INEDITOR] start <task> … done <task>` 区间抽取（与控制台同源；域重载会触发控制台 Clear on Recompile，`read_console` 只保留本次运行尾部）。三个 token 均已用 `read_console` 独立核对存在。

## 6. 学习讲稿

`不适用`（TASK §5 已判定）：测试基础设施改造，不新增产品机制、不改公共契约；未创建 `Docs/Learning/` 文件。讲解点已并入本报告 §3（“为什么用一行钩子而不是复制一份入口”），三句话版本见 §3.7。

## 7. 未决项

1. **DIAG 仍有 1 条 product_fail**：`Diag.Input.FirstExit`（expected `MISSING_CONTROLLER`，actual `NO_PRIMARY`）。该失败在 canonical batch 基线里同样存在（`DIAG-SKILL/DIAG_Assertions.jsonl` 2026-11-05 12:42 那次），**非本单引入**；本单按 AC-4 只记录不判失败，修复需另立单。
2. ~~**launcher 兜底路径仍会退编辑器**：三个 `*_BatchAcceptance.Run` 的 `catch` 里保留既有 `EditorApplication.Exit(3)`（例：`OpenScene` 抛异常）。本单按硬边界只改 `ExitWithCode` 一处，未动 catch；编辑器内若触发该路径仍会退出 Unity。建议后续单点把“是否编辑器内运行”的判断下沉到一处。~~ —— **已关闭（2026-11-05 追加，所有者批准并入本单）**：三处 `catch` 已补 `if (QA_InEditorMenu.TryFinish("<ID>", 3)) return;`，batch 路径仍原样 `EditorApplication.Exit(3)`；证据见 §2.1 与 `Docs/QA/Evidence/TEST-MCP-01/diff_catch.txt`。
3. **控制台分类噪音**：`read_console(types=error)` 命中 8 条 DIAG 断言/汇总文本（Unity 按内容把它们标成 Assert/Exception 图标，file=`DIAG_Evidence.cs:65/109`，实为 `Debug.Log`）；已用 `filter_text="error CS"` 与 `"Compilation failed"` 复查 = 0 条，无编译错误。仅作记录，不影响结论。
4. **工作区另有非本单改动**（提交时请注意只勾本单文件）：`Assembly-CSharp.csproj`（16:22:04 修改，早于本单写入）、`Docs/Handoff/CURRENT.md`（17:38:55）、`Docs/Handoff/TASK_SKILL-ANIM-02.md`（17:46:44）、`AGENTS.md` 与 `Docs/Handoff/workflow-rules.md`（§3.2.1 第 5/9 条更新，非本单）、未跟踪的 `Docs/Handoff/TASK_WF-05.md` / `REPORT_WF-05.md` / `TASK_TEST-MCP-01.md`、`Docs/QA/Evidence/WF-05/`。这些我在本单内**没有**读写。
5. **~~（追加，新）编辑器失焦会冻结 Play 帧循环~~** —— **已关闭（2026-11-05 追加，AC-10）**：改为在 `QA_InEditorMenu.Run` 分派前设 `Application.runInBackground = true`（运行期开关，不动 Player Settings）；3 次连跑含 1 次前台钉住的全失焦跑均 12/0/exit0，证据见 §2.2 与 `Docs/QA/Evidence/TEST-MCP-01/runinbackground.txt`、`focus_trace_run7/8.txt`。原文留档：工程 `runInBackground: 0`，`is_focused=false` 时探针协程不推进；若此时 Unity 补做挂起的导入/编译并在 Play 中域重载，运行期 runner 丢失且 `Boot` 见 `started=true` 不再重建，该轮只能人工 stop（`editor_run2` 即此情形）。**仍开放的子项**：① 菜单 `Run` 前对 `external_changes_dirty` 增加显式 `refresh_unity` + 等编译完成；② 探针 `Boot` 在 `started=true && runner==null` 时允许重启一轮。二者属探针健壮性改进，不在本单范围。
6. **（追加，记录）`Docs/QA/Evidence/SPEC-01/editor_run2/` 为空目录**：第 1 次中断尝试由 `SPEC01_Evidence` 构造时创建，未写入文件；git 不跟踪空目录，未删除、未补写。重试结果在 `editor_run3/`。

## 8. 建议提交

```text
test(qa): 新增 Tools/QA/In-Editor 三套探针的编辑器内菜单入口，并让 launcher 兜底路径不再退编辑器
为什么：常开 Unity 里可一句话跑 SPEC01/FND00B06/DIAG 回归，证据按 editor_run<N> 递增分流，异常路径也只记失败不杀编辑器
```

提交范围：上述 7 个 `.cs` + Unity 生成的 3 个 `.meta` + `Docs/QA/Evidence/TEST-MCP-01/**` + 本 REPORT + 三套 `editor_run1/` 目录 + `SPEC-01/editor_run3/`（`editor_run2/` 为空目录，git 不跟踪）。**AC-10 增量**：`QA_InEditorMenu.cs` 内 1 行代码 + 1 行注释 + start 日志追加一段；证据新增 `SPEC-01/editor_run6..8/`、`TEST-MCP-01/focus_trace_run7/8.txt`、`TEST-MCP-01/runinbackground.txt`。代码与资产由所有者用 GitHub Desktop 提交。

## 9. 消耗记录

- 钱包：未由子代理暴露（`opencode-go` 路由）
- 模型完整 ID：`opencode-go/deepseek-v4.1-flash`
