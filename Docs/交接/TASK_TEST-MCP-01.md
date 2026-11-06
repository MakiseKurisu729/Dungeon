# TEST-MCP-01 — 三套探针的“编辑器内运行”菜单入口

## §0 元信息

| 字段 | 值 |
|---|---|
| 任务 ID | `TEST-MCP-01` |
| 风险等级 | `常规`（Editor 脚本 + 证据路径；不动 Animator/场景/预制体/`.asset`/GUID） |
| 授权 | 所有者 2026-11-05 指示第 2 条派单；**本单一次性授权** `execute_menu_item` 仅可触发 `Tools/QA/In-Editor/*` 三个菜单，其余 MCP 写工具（`manage_scene`/`manage_asset`/`execute_code`/脚本编辑等）一律禁用 |
| 基线提交 | `6872348b`（WF-04 接入 Unity MCP） |
| 回滚锚 | `git checkout -- Assets/_Project/Tests`；删除新增 `Assets/_Project/Tests/QA/` 及其 `.meta` |
| 分支 | 当前分支（不另开分支） |
| 状态 | `Review`（AC-1~8 已由大脑复核通过；launcher `catch` 已补钩子，见 AC-9） |

## §1 需求（Why / What）

**一句话目标**：让 `SPEC01`、`FND00B06`、`DIAG` 三套既有探针能在**不关闭、不退出 Unity** 的前提下从编辑器菜单跑完一轮并落证据，供 MCP `execute_menu_item` 触发。

**用户故事**：作为所有者，我要在 Unity 一直开着的状态下一句话触发回归，不必为了跑一次 SPEC01 关掉编辑器再等一次 batchmode 冷启动。

**必须满足**

| 编号 | 需求 | 验收方式 |
|---|---|---|
| R-1 | 新增 `Tools/QA/In-Editor/` 三个菜单项：`SPEC01 (green)`、`FND00B06`、`DIAG` | 菜单实际可点 + 日志 |
| R-2 | 菜单路径复用现有 batch 启动器（`*_BatchAcceptance.Run`），**不复制探针逻辑**，断言代码零改动 | `git diff` 静态检查 |
| R-3 | 编辑器内跑完**不得**调用 `EditorApplication.Exit`；跑完回到 Edit 模式，Unity 进程存活 | 运行后 `editor_state.play_mode.is_playing=false` 且进程仍在 |
| R-4 | 编辑器内证据写入 `Docs/QA/Evidence/<ID>/editor_run1/`，canonical 目录文件**字节不变** | 运行前后 SHA256 比对 |
| R-5 | 编辑器内结果可判定：写 `ExitCode.txt`（沿用现有逻辑）+ `InEditorResult.txt`（任务号/退出码/时间）+ 控制台 token `[QA][INEDITOR] done task=<ID> code=<n>` | 文件 + `read_console` |
| R-6 | 进入 Play 前有编译/播放态门：`is_compiling=false` 且 `play_mode.is_playing=false` 才继续，否则拒绝并记日志 | 日志 |
| R-7 | 不改任何断言口径、阈值、夹具、随机种子策略、场景/资产 | `git diff` 只含驱动与钩子 |
| R-8 | 三套结果与 batch 基线可比：SPEC01 `pass=12 fail=0`、FND00B06 `pass=12 fail=0 observed=2`；DIAG 记录计数并对比最近一次 batch | Summary JSON |

**不在范围**：新增测试用例、修改断言、把三套合并成一套、改 batch 入口行为（batch 必须保持原样可跑）、Unity Test Runner（`run_tests`）接入、CI。

**约束**：不新增 asmdef（沿用 `Editor/` 目录进 `Assembly-CSharp-Editor` 的现状）；运行期 Evidence 类只加“证据子目录”能力，不改统计与字段；环境变量在同一 Unity 进程内用 `Environment.SetEnvironmentVariable` 传递（进程内生效，无需重启）。

## §2 方案（Plan）

**备选方案**

| 方案 | 做法 | 优点 | 缺点 | 结论 |
|---|---|---|---|---|
| A | 为三套各写一份 `*_InEditorAcceptance.cs`，复制 Run/Boot 逻辑，去掉 Exit | 三个文件互不影响 | 三份重复的进 Play/场景/退出逻辑，改一次要改三处；断言调用点也重复 | 弃用 |
| B | 一份共享驱动 `QA_InEditorMenu.cs`：设置环境变量与证据子目录 → 调既有 `*_BatchAcceptance.Run()`；在三处 `Boot.ExitWithCode` 加一行“编辑器内不 Exit”钩子 | 复用现有唯一入口；batch 行为一行不改即可保留；新增/修改面最小 | 需要改 3 个 batch 文件各 1 处 + 3 个 Evidence 类各 1 处 | **选定** |

**选定方案与理由**：B 的“编辑器内不退出”只发生在共享钩子里，batch 路径解析后仍然 `EditorApplication.Exit(code)`，两条路径共用同一套探针与断言。

**数据流**

```text
execute_menu_item("Tools/QA/In-Editor/SPEC01 (green)")
→ QA_InEditorMenu.Run("SPEC01")
   ├ 闸门：is_compiling / is_playing 检查
   ├ Environment: SPEC01_MODE=green, SPEC01_RUN=1, QA_EVIDENCE_SUBDIR=editor_run1
   ├ SessionState: QA.InEditor.Active=true, QA.InEditor.Task=SPEC01
   └ 调 SPEC01_BatchAcceptance.Run()（打开场景 → EnterPlaymode）
→ SPEC01_ProbeRunner 跑断言（不变）→ SPEC01_Evidence 写 editor_run1/
→ 探针 Finish：写 Summary/ExitCode，EditorApplication.isPlaying=false
→ EnteredEditMode → SPEC01_Boot.ExitWithCode
   ├ QA_InEditor.TryFinish("SPEC01", code) == true → 写 InEditorResult.txt，打 token，清标记，return
   └ 否则（batch 路径）→ EditorApplication.Exit(code)（原样）
```

**影响面清单**

| 项 | 内容 |
|---|---|
| 新增文件 | `Assets/_Project/Tests/QA/Editor/QA_InEditorMenu.cs`（菜单 + 驱动 + 结果钩子） |
| 修改文件 | `Assets/_Project/Tests/SPEC01/Editor/SPEC01_BatchAcceptance.cs`（`ExitWithCode` 加钩子）、`Assets/_Project/Tests/FND00B/Editor/FND00B06_BatchAcceptance.cs`（同）、`Assets/_Project/Tests/DIAG/Editor/DIAG_BatchAcceptance.cs`（同） |
| 修改文件（证据子目录） | `Assets/_Project/Tests/SPEC01/Runtime/SPEC01_Evidence.cs`、`Assets/_Project/Tests/FND00B/Runtime/FND00B06_Evidence.cs`、`Assets/_Project/Tests/DIAG/Runtime/DIAG_Evidence.cs`（各加 `QA_EVIDENCE_SUBDIR` 读取，1 行分支） |
| 修改符号 | 新增 `QA_InEditorMenu`（静态类）；三个 `Boot.ExitWithCode` 内的退出分支；三个 `*_Evidence` 构造函数的目录计算 |
| 字符串引用 | 菜单路径 `Tools/QA/In-Editor/*`；环境变量 `QA_EVIDENCE_SUBDIR`；SessionState 键 `QA.InEditor.Active`/`QA.InEditor.Task`；日志 token `[QA][INEDITOR]` |
| 场景/预制体引用 | 无新增；仍由 `*_BatchAcceptance.ScenePath` 打开 `Assets/Scenes/DungeonWorld.unity` |
| `.meta` | 新增文件/文件夹由 Unity 自动生成 `.meta`，**随代码一起交所有者提交**；不手动改 `.meta`、不改 GUID |
| 选项/包 | 不改 `ProjectSettings`、`Packages`、无 asmdef 变更 |

**实施顺序**

1. 新增 `QA_InEditorMenu.cs`（菜单 3 项 + 闸门 + 环境变量 + `TryFinish`）。
2. 三个 `*_BatchAcceptance.cs` 的 `ExitWithCode` 加钩子（放在清理 SessionState 之前，`EditorApplication.Exit` 之前）。
3. 三个 `*_Evidence.cs` 支持 `QA_EVIDENCE_SUBDIR`。
4. 回 Unity 等编译完成（`is_compiling=false`），确认菜单出现。
5. 依次跑三套（每套之间等 `is_playing=false`）：SPEC01 → FND00B06 → DIAG。
6. 采集：`read_console` 过滤 `[SPEC01]/[FND00B06]/[DIAG]/[QA][INEDITOR]`，写日志文件；SHA256 比对 canonical 目录。

**风险与回滚**

- 风险 1：`Environment.SetEnvironmentVariable` 残留污染后续 batch（在 IDE 进程内）。缓解：`TryFinish` 里把三个套件的环境变量清空（置 null）。
- 风险 2：编辑器内跑 SPEC01 会打开场景；若当前场景有未保存修改，Unity 可能弹保存对话框卡住。缓解：R-6 前置检查 + 若 30s 内未出现 `EnterPlaymode requested` 日志，立即收手并在 REPORT 记录“无法复现/被对话框阻塞”，不强行点。
- 风险 3：Play 模式下异常卡死，需手工退 Play。缓解：只跑 1 轮，不连跑。
- 回滚：删 `Assets/_Project/Tests/QA/`，`git checkout --` 三个 batch 文件与三个 Evidence 文件。

## §3 验证（Validation）

**batch 基线（对照用，来自既有证据，不重跑）**

| 套件 | 基线来源 | pass | fail | observed | exit |
|---|---|---|---|---|---|
| SPEC01 | `Docs/QA/Evidence/SPEC-01/SPEC01_Green_skillanim01_run3_Summary.json` | 12 | 0 | 0 | 0 |
| FND00B06 | `Docs/QA/Evidence/FND-00B-06/06_Summary_run1.json` | 12 | 0 | 2 | 0 |
| DIAG | `Docs/QA/Evidence/DIAG-SKILL/DIAG_Summary.json` | 记录实际值 | 记录实际值 | 记录实际值 | 记录实际值 |

| AC | Given / When / Then | 断言 ID | 证据 |
|---|---|---|---|
| AC-1 | 给定编译完成，当查菜单资源/目检，则 `Tools/QA/In-Editor/` 下恰有 SPEC01 / FND00B06 / DIAG 三项，`execute_menu_item` 能列出并可触发 | `QA.Menu.Present` | `Docs/QA/Evidence/TEST-MCP-01/menu_list.txt` |
| AC-2 | 给定编辑器内跑 SPEC01，当探针结束，则 `pass=12`、`fail=0`、`exit=0`，与 batch 基线逐项相等 | `QA.InEditor.SPEC01.Parallel` | `.../editor_run1/SPEC01_*_Summary.json`、`editor_SPEC01.log` |
| AC-3 | 给定编辑器内跑 FND00B06，当探针结束，则 `pass=12`、`fail=0`、`observed=2`、`exit=0`，与 batch 基线逐项相等 | `QA.InEditor.FND00B06.Parallel` | `.../editor_run1/06_Summary*.json`、`editor_FND00B06.log` |
| AC-4 | 给定编辑器内跑 DIAG，当探针结束，则出 Summary 与 ExitCode、控制台有 `[QA][INEDITOR] done task=DIAG code=<n>`，计数与 DIAG 最近一次 batch 的差异如实记录（差异不判失败，但必须写原因） | `QA.InEditor.DIAG.Completed` | `.../editor_run1/DIAG_Summary.json`、`editor_DIAG.log` |
| AC-5 | 给定三套跑完，当查 Unity，则进程仍存活、`editor_state.play_mode.is_playing=false`、`is_compiling=false`（**没有** Exit 退出编辑器） | `QA.InEditor.NoExit` | `.../editor_state_after.json` |
| AC-6 | 给定跑完，当比 SHA256，则 `Docs/QA/Evidence/SPEC-01/`、`FND-00B-06/`、`DIAG-SKILL/` 的 canonical 文件全部未变，新证据只在各自 `editor_run1/` | `QA.Evidence.Isolated` | `.../sha256_before.txt`、`sha256_after.txt` |
| AC-7 | 给定最终 diff，当 `git diff -- Assets/_Project/Tests`，则只含 6 个文件的驱动/钩子改动 + 1 个新文件，`Assert*` 方法体与阈值零改动 | `QA.Assertions.Untouched` | `.../diff_assertions.txt` |
| AC-8 | 给定跑完，当查环境变量，则 `SPEC01_MODE/SPEC01_RUN/SPEC01_EVIDENCE_TAG/FND00B06_RUN/DIAG_RUN/DIAG_MODE/QA_EVIDENCE_SUBDIR` 均已被清空（不污染后续 batch） | `QA.Env.Clean` | `.../editor_env_after.txt` |

**运行次数**：每套编辑器内 **1 次**（基础设施单；batch 结果不变性由 AC-7 静态保证）。若首次因对话框/编译卡住失败，允许 1 次重试，第二次仍失败即停并报“无法复现/被阻塞”。

**自动化优先**：全部 AC 由真实运行 + 文件比对得出，无目检项。

**必须保存的证据**：三套的 `editor_run1/`（Summary、Assertions.jsonl、ExitCode.txt、InEditorResult.txt）、三份控制台日志、`menu_list.txt`、SHA256 前后、`editor_state_after.json`、`diff_assertions.txt`、`Summary.md`、`ExitCode.txt`（汇总）。

## §4 执行信息

| 项 | 内容 |
|---|---|
| 前置 | Unity 已打开（`Dungeon@4f5c34c82f31646e`，Unity 2022.3.62f3c1）；**无未保存的场景修改**（有则先让所有者保存，不得代替保存）；无其它 AI 正在操作 Unity |
| 允许改动 | 上表 7 个 `.cs`（含 1 新增）+ 其自动生成的 `.meta` + `Docs/QA/Evidence/TEST-MCP-01/**` + `Docs/Handoff/REPORT_TEST-MCP-01.md` + `Docs/Learning/TEST-MCP-01_讲稿.md`（不适用说明写这里不建文件，见 §5） |
| 允许读取 | 本工单；三套现有批处理入口与 Evidence 类（只读对照） |
| 允许的 MCP 调用 | **只读**：`read_console`、资源 `mcpforunity://editor/state`/`tests`/`menu-items`、`manage_editor` 查询。**唯一写类**：`execute_menu_item` 触发 `Tools/QA/In-Editor/*` 三项。**禁用**：`manage_scene`、`manage_asset`、`manage_components`、`manage_gameobject` 写、`manage_animation` 写、`create_script`/`manage_script`/`apply_text_edits`/`script_apply_edits`、`execute_code`、`run_tests`、`set_active_instance`（除非实例不唯一） |
| 绝不触碰 | 场景、预制体、Animator Controller、`.asset`、`ProjectSettings/**`、`Packages/**`、`CURRENT.md`、其他 `TASK_*`/`REPORT_*`、既有 canonical 证据、任何 git 写操作 |
| 证据目录 | `Docs/QA/Evidence/TEST-MCP-01/` |
| 熔断 | 需要改断言或阈值；需要关 Unity；需要改场景/资产；对话框阻塞 1 次重试后仍阻塞；`ExitCode` 与基线不一致（先停，报差异，不自行“修到一样”）；编译错连续 2 轮；发现第二个 AI 在驱动 Unity |
| 执行预算 | 1 轮实现 + 1 轮修复；三套各跑 1 次（DIAG 崩溃重试算在内）；不跑 batch、不跑 Unity Test Runner |

## §5 学习产出

| 项 | 要求 |
|---|---|
| 讲稿 | **不适用**：测试基础设施改造，不新增产品机制、不改公共契约；讲解点已并入 REPORT §3（“为什么用一行钩子而不是复制一份入口”） |
| 三句话版本 | 必须写进 REPORT §3：“batch 与编辑器内共用同一入口，只在退出点分叉 / 证据靠环境变量分流到 `editor_run1/`，canonical 不动 / 这样 MCP 才能在常开编辑器里跑回归而不杀掉编辑器” |

## 自包含声明

本工单不依赖 `CURRENT.md` 或其他任务书即可执行；执行者只读本文件。信息不足时在 REPORT 写“信息不足”并列缺项，不自行翻阅其他文档补全。
