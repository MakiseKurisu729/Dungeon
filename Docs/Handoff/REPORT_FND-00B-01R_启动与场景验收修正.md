# REPORT_FND-00B-01R — 启动与场景验收修正

> 工单：`Docs/Handoff/TASK_FND-00B-01R_启动与场景验收修正.md`
> 执行人：AI（执行者）；Unity 操作与判定仍由用户负责
> 执行结论：**前置条件未满足，已按工单在进入 Unity 前停止**；未补拍、未改 QA、未产生 `00R_前置确认.txt`

## 结果摘要
- 原验收提交：e08590889577792ae449e59329f8f74fd3f28f40
- 补证提交：`无（本次未产生补证提交，未进入 Unity、未生成证据文件）`
- 第 1 项修正后结果：`部分通过（未修正，保持原状）`
- 四项缺失证据：`01_Console_编译检查.png` 未补齐；`02_Play_Console.png` 未补齐；`04_构建与最近验收.txt` 未补齐；`Unity_启动与Play.log` 未补齐
- 补证期间异常：`无（未进入 Unity，未执行任何 Play）`

## 改动文件
- `Docs/Handoff/REPORT_FND-00B-01R_启动与场景验收修正.md`（本文件，新建）

其余文件一律未改：`Docs/QA/FND-00B_现有行为验收.md` 未同步、`Docs/QA/Evidence/FND-00B-01/` 未新增任何文件、`Docs/Handoff/CURRENT.md` 未修改。

## 检查结果
- 前置工作区：`不干净，已停止`。唯一变化项为工单文件自身未被 Git 跟踪，详见下节原始输出；工单第 3 节要求“`git status --porcelain` 无输出才允许继续”，故未进入 Unity。
- 证据完整性：`缺失 4 项`（`01_Console_编译检查.png`、`02_Play_Console.png`、`04_构建与最近验收.txt`、`Unity_启动与Play.log`），另 `00R_前置确认.txt` 未生成。
- 代码及 Unity 资产：`未修改`
- Git 变更操作：`未执行`（无 add / commit / restore / checkout / stash）

## 遗留问题
- 工作区存在未跟踪文件 `Docs/Handoff/TASK_FND-00B-01R_启动与场景验收修正.md`，需用户自行处理其纳入状态后重新执行本工单。
- 原工单四项缺失证据仍未补齐，第 1 项维持 `部分通过`。
- 原报告中 `Assets/Scenes/DungeonWorld/NavMesh*` 与 `.meta` 的修改状态本工单不处理、不验证，仍未确认。
- `03_测试场景.png` 与 `01_工程已打开.png` 为同一张截图，场景完整路径仍只有用户确认来源，无独立截图证明。

---

## 前置确认原始记录（只读）

执行时间：2026-11-03（本地）
工作目录：`D:\unity\Dungeon`

```text
$ git rev-parse --abbrev-ref HEAD
docs/fnd-00-baseline

$ git rev-parse HEAD
25ebf7751515fd115800f9244427d57d38767260

$ git status --porcelain
?? "Docs/Handoff/TASK_FND-00B-01R_\345\220\257\345\212\250\344\270\216\345\234\272\346\231\257\351\252\214\346\224\266\344\277\256\346\255\243.md"
```

判定：`git status --porcelain` 有输出，按工单第 3 节“否则立即停止，仅写修正执行报告，不进入 Unity”执行。

补充说明（客观事实，不含判断）：
- 当前 HEAD `25ebf775…` 与工单记录的原验收提交 `e0859088…` 不是同一提交。
- Unity 版本经 `ProjectSettings/ProjectVersion.txt` 核对为 `2022.3.62f3c1`，与工单要求一致。
- `Assets/Scenes/DungeonWorld.unity` 存在。
- `Docs/QA/Evidence/FND-00B-01/` 现有 4 个文件：`00_前置确认.txt`、`01_工程已打开.png`、`02_Play_运行.png`、`03_测试场景.png`。

---

## 任务拆分：需要用户手动在 Unity 操作的步骤

> 前提：先完成下面第 0 步，工单才允许继续。

| # | 步骤 | 产出 |
|---|---|---|
| 0 | 处理工作区：自行决定将 `Docs/Handoff/TASK_FND-00B-01R_启动与场景验收修正.md` 提交/暂存或移出，使 `git status --porcelain` 无输出。AI 不做任何 Git 变更。 | 干净的 `git status` |
| 1 | 打开 Unity `2022.3.62f3c1` 打开本工程，等待导入与编译**自然完成**；不改设置、不点修复建议 | 可截图状态 |
| 2 | **进入 Play 之前**，先 `Clear` Console 后观察，截取完整 Console 窗口（含 Unity 标题栏/菜单等上下文），保存为 `01_Console_编译检查.png` | 缺失证据 ① |
| 3 | 把这一刻 Console 的**全部文本**复制交给我（无条目就说明“无条目”），并告知当前时间 | `Unity_启动与Play.log` 的“补证：Play 前”段 |
| 4 | 进入 Play，**只观察**，不移动、不攻击、不点地面（否则会污染第 1 项结论） | — |
| 5 | 在**清除 Console 之前**截取完整 Console，保存为 `02_Play_Console.png` | 缺失证据 ② |
| 6 | 把 Play 期间 Console 的**全部文本**复制交给我（含 3 条 NavMesh 黄色警告原样文本），并告知当前时间 | `Unity_启动与Play.log` 的“补证：Play 后”段 |
| 7 | 退出 Play，**不保存**任何场景或资产变化 | — |
| 8 | 提供本次补证提交的完整 SHA（由你提交并推送后告知） | 报告与 QA 的补证提交字段 |

说明：NavMesh 警告只做原样转录，不分析、不修复、不据此断言原异常已消失或已修复。

## 任务拆分：AI 可直接完成（无需 Unity）的步骤

| # | 步骤 | 状态 |
|---|---|---|
| A | 读工单、拆分步骤、核对前置条件 | 已完成 |
| B | 只读核对 Unity 版本、场景文件、现有证据清单 | 已完成 |
| C | 只读检查构建产物是否存在 | 已完成，结论见下节 |
| D | 只读提取 `Editor.log` / `Editor-prev.log` 中 NavMesh 警告（仅供参考，不作为证据） | 已完成，结论见下节 |
| E | 生成 `04_构建与最近验收.txt`（工单给定文本，无推测） | **未执行**，待前置条件满足 |
| F | 生成 `00R_前置确认.txt` | **未执行**，待前置条件满足 |
| G | 写入 `Unity_启动与Play.log`（用你提供的 Console 文本） | **未执行**，缺步骤 3/6 的输入 |
| H | 同步 QA 第 1 项与汇总 | **未执行**，待证据齐全后按实际判定 |
| I | 回填本报告的结果摘要与提交 SHA | 待补证完成后更新 |

---

## 只读参考信息：构建产物检查

检查方式：仓库根目录下级目录枚举；全仓（排除 `Library/`、`Temp/`、`.git/`）搜索 `*.exe` / `*.apk`；搜索形如 `Build` / `Builds` / `Output` / `构建` 的目录。

结论：
- 未发现任何 `*.exe` / `*.apk` 产物。
- 仓库根目录仅含 `.vscode`、`Assets`、`Docs`、`docx`、`Library`、`Logs`、`Packages`、`ProjectSettings`、`Temp`、`UserSettings`，无构建输出目录。
- 与 `REPORT_FND-00B-01` 第 4 组问答“无可运行构建”一致，无新信息可追加。
- `Logs/` 下仅有 `Packages-Update.log` 与空的 `shadercompiler-UnityShaderCompiler.exe0.log`，无 Player 日志。

## 只读参考信息：Editor.log 中的 NavMesh 警告（不作为补证证据）

保留的日志只有两份，均产生于原验收截图之后：

| 日志 | 覆盖时段（本地） | `Failed to create agent because there is no valid NavMesh` | `GetRemainingDistance` 报错 | `error CS` |
|---|---|---|---|---|
| `Editor-prev.log` | 15:05:53 起 → 15:17:12 | 6 条（行 365–367、12089–12091，即两轮 Play 各 3 条） | 2360 条 | 0 |
| `Editor.log` | 15:18:59 起 → 15:29:36 | 6 条（行 492–494、14399–14401，即两轮 Play 各 3 条） | 4058 条 | 0 |

原始文本（两轮 Play 中每次均为 3 条连续相同行，位置紧邻 `[PhysX] Initialized MultithreadedTaskDispatcher` 与 `Loaded scene 'Temp/__Backscenes…'` 之前）：

```text
Failed to create agent because there is no valid NavMesh
Failed to create agent because there is no valid NavMesh
Failed to create agent because there is no valid NavMesh
```

紧邻的上下文样本（`Editor.log` 行 490–496）：

```text
[PhysX] Initialized MultithreadedTaskDispatcher with 16 workers.
[PhysX] Initialized MultithreadedTaskDispatcher with 16 workers.
Failed to create agent because there is no valid NavMesh
Failed to create agent because there is no valid NavMesh
Failed to create agent because there is no valid NavMesh
Loaded scene 'Temp/__Backupscenes/0.backup'
	Deserialize:            4.869 ms
```

需要注意的事实边界：
- 原验收截图时间为 14:45 与 15:02，而现存两份日志分别从 15:05:53 与 15:18:59 开始，**没有任何一份覆盖截图当时的那一轮运行**。
- 因此以上条目只能说明“NavMesh 警告在之后的多轮 Play 中重复出现”，**不能**代替工单要求的新一轮 Play 截图与 Console 全文，也不能据此判定原异常是否消失或修复。
- 同一日志中另有大量 `"GetRemainingDistance" can only be called on an active agent that has been placed on a NavMesh.` 报错（合计 6418 条），属于第 2 项“移动”范畴，本工单只记录数量，不分析、不修复。
