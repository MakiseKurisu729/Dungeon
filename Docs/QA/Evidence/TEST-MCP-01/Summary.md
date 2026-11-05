# TEST-MCP-01 证据汇总

- 任务：`Docs/Handoff/TASK_TEST-MCP-01.md`
- Unity 实例：`Dungeon@4f5c34c82f31646e`，Unity `2022.3.62f3c1`，分支 `docs/fnd-00-baseline`，HEAD `6872348b`
- 运行方式：**编辑器内**（不关 Unity、不开 batchmode），逐个点击 `Tools/QA/In-Editor/*` 菜单，每套 1 次
- 导入方式：写入 `.cs` 后 Unity 处于未聚焦状态未自动导入；所有者手动切入 Unity 窗口触发 Auto Refresh 后编译完成（`editor/state` 域重载完成、控制台 0 error），**未调用 `refresh_unity`**，全程唯一的 MCP 写类调用是 3 次 `execute_menu_item`

## 三套结果

| 套件 | pass | fail | blocked | infraFail | observed | ExitCode | batch 基线 | 一致性 |
|---|---|---|---|---|---|---|---|---|
| SPEC01 (green, run1) | 12 | 0 | 0 | 0 | 0 | 0 | 12/0/0/0/0，exit 0 | 逐项相等 |
| FND00B06 (run1) | 12 | 0 | 0 | 0 | 2 | 0 | 12/0/0/0/2，exit 0 | 逐项相等 |
| DIAG (probe, run1) | 6 | 1 | 0 | 0 | 0 | 2 | 6/1/0/0/0，exit 2 | 逐项相等，失败项同为 `Diag.Input.FirstExit`（expected `MISSING_CONTROLLER` / actual `NO_PRIMARY`） |

DIAG 差异（如实记录，不判失败）：canonical `DIAG-SKILL/DIAG_Summary.json` 的 `head` 为 `7cf874f7…`（2026-11-05 12:42 的 batch），本次编辑器内为 `6872348b…`；除 `head`、时间戳与 `assertionFile` 路径外，计数与失败项完全一致。

## AC 对照

| AC | 结果 | 证据 |
|---|---|---|
| AC-1 菜单存在且三项 | 通过（`Tools/QA/In-Editor/{SPEC01 (green),FND00B06,DIAG}`，matchedCount=3，无其它 Tools/QA 项） | `menu_list.txt` |
| AC-2 SPEC01 12/0/exit0 | 通过 | `../SPEC-01/editor_run1/SPEC01_Summary_run1.json`、`editor_SPEC01.log` |
| AC-3 FND00B06 12/0/observed2/exit0 | 通过 | `../FND-00B-06/editor_run1/06_Summary_run1.json`、`editor_FND00B06.log` |
| AC-4 DIAG 出 Summary+ExitCode+token | 通过（token：`[QA][INEDITOR] done task=DIAG code=2`） | `../DIAG-SKILL/editor_run1/DIAG_Summary.json`、`editor_DIAG.log` |
| AC-5 不退出编辑器 | 通过（三套跑完 Unity 进程 8000 存活、Responding=True、`is_playing=false`、`is_compiling=false`、`is_domain_reload_pending=false`） | `editor_state_after.json` |
| AC-6 canonical 未变 | 通过（跑前 150 个文件 SHA256，跑后全部逐条相同；新增 13 个文件只在各自 `editor_run1/`） | `sha256_before.txt`、`sha256_after.txt` |
| AC-7 断言零改动 | 通过（6 个文件 +9/-0，无任何行含 `Assert`；新增 1 个未跟踪文件 `Assets/_Project/Tests/QA/`） | `diff_assertions.txt` |
| AC-8 环境变量清空 | 通过（三套 `InEditorResult.txt` 的 `envAfter=` 7 个键全为 `<null>`） | `editor_env_after.txt` |

## 文件清单

- `menu_list.txt` — AC-1 菜单枚举
- `editor_SPEC01.log` / `editor_FND00B06.log` / `editor_DIAG.log` — 从 Unity `Editor.log` 按 `[QA][INEDITOR] start…done` 区间抽取的原始运行日志（控制台同源；控制台 token 已用 `read_console` 独立核对）
- `sha256_before.txt` / `sha256_after.txt` — 三个 canonical 目录全量 SHA256
- `editor_state_after.json` — 跑完时刻 `editor/state` 原样响应
- `diff_assertions.txt` — `git status --short` + `git diff --stat` + `git diff` + 断言行核查
- `editor_env_after.txt` — 环境变量清空证据
- `ExitCode.txt` — 三套退出码汇总

运行时证据（各套件 `editor_run1/`，非本目录）：

- `../SPEC-01/editor_run1/`：`SPEC01_Summary_run1.json`、`SPEC01_Assertions.jsonl`(12)、`SPEC01_ExitCode_run1.txt`(0)、`SPEC01_Runs.jsonl`、`InEditorResult.txt`
- `../FND-00B-06/editor_run1/`：`06_Summary_run1.json`、`06_Assertions.jsonl`(14 行 / 12 pass + 2 observed)、`06_ExitCode_run1.txt`(0)、`InEditorResult.txt`
- `../DIAG-SKILL/editor_run1/`：`DIAG_Summary.json`、`DIAG_Assertions.jsonl`(7)、`DIAG_ExitCode.txt`(2)、`InEditorResult.txt`

## AC-9 追加验证（launcher catch 钩子 + 递增子目录，2026-11-05 18:2x）

| 项 | 结果 | 证据 |
|---|---|---|
| 三处 launcher catch 先调 QA_InEditorMenu.TryFinish("<ID>", 3) | 已改（SPEC01 / FND00B06 / DIAG 各 +2 行），编译 0 error | `diff_catch.txt` |
| 编辑器内不再被 catch 分支 Exit | 静态证据：钩子位于 `EditorApplication.Exit(3)` 正上方；未人为制造异常（边界禁止改断言/代码去触发） | `diff_catch.txt` |
| 证据子目录递增 | `NextSubdir(task)` 扫描既有 `editor_run<N>` 取 N+1 | `diff_subdir.txt` |
| SPEC01 二次运行 | pass=12 fail=0 blocked=0 infraFail=0 observed=0，ExitCode=0，12 条断言，`code=0` | `../SPEC-01/editor_run3/` |
| Unity 存活 | 进程 8000 存活、Responding=True；`is_playing=false`、`is_compiling=false` | `editor_state_after_run3.json`、`unity_process_after.json` |

中断与重试（如实记录，不删不改原始文件）：
- 第 1 次尝试写入 `editor_run2`（18:24 起）后因编辑器失焦 + Play 中域重载无法收尾，`editor_run2/` 为空目录；
- 第 2 次（重试）自动落 `editor_run3` 并通过；现场细节见 `editor_run2_abort_note.txt`、`editor_run2_abort.log`、`editor_run3_spec01.log`。