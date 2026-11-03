# REPORT_FND-00B-01R — 启动与场景验收修正

> 工单：`Docs/Handoff/TASK_FND-00B-01R_启动与场景验收修正.md`
> 执行人：AI（记录与只读检查）；Unity 操作与判定由用户执行
> 执行结论：**四项缺失证据已真实补齐，第 1 项按原标准改判为 `通过`**

## 结果摘要
- 原验收提交：e08590889577792ae449e59329f8f74fd3f28f40
- 补证提交：`af07ddb1130bf23e82847c99183ef86c850f5cc0`
- 第 1 项修正后结果：`通过`
- 四项缺失证据：`01_Console_编译检查.png` 已补齐；`02_Play_Console.png` 已补齐；`04_构建与最近验收.txt` 已补齐；`Unity_启动与Play.log` 已补齐
- 补证期间异常：`NavMesh 警告复现 3 条（[15:50:55] Failed to create agent because there is no valid NavMesh）；另有 1 条 [15:50:56] 0 个身体部件已挂到 PlayerBody 层（DebugCameraSwitcher.cs:102）；无阻塞性编译错误、无 Exception；退出 Play 未产生资产变更`

## 改动文件
- `Docs/QA/Evidence/FND-00B-01/00R_前置确认.txt`（新增）
- `Docs/QA/Evidence/FND-00B-01/01_Console_编译检查.png`（新增）
- `Docs/QA/Evidence/FND-00B-01/02_Play_Console.png`（新增）
- `Docs/QA/Evidence/FND-00B-01/04_构建与最近验收.txt`（新增）
- `Docs/QA/Evidence/FND-00B-01/Unity_启动与Play.log`（新增）
- `Docs/QA/FND-00B_现有行为验收.md`（当前状态、第 1 项、汇总、统计）
- `Docs/Handoff/REPORT_FND-00B-01R_启动与场景验收修正.md`（本文件）

未改动：`Assets/`、`Packages/`、`ProjectSettings/`、任何代码、场景、预制体、Animator、`.meta`、`Docs/Handoff/CURRENT.md`。

## 检查结果
- 前置工作区：`干净`。首轮检查不干净（唯一一项为工单文件自身未跟踪），已按工单停止且未进入 Unity；用户提交后复核 `git status --porcelain` 为空，HEAD `afc70759441ed73f3ecfb0292b2b00a6de793307`，见 `00R_前置确认.txt`。
- 证据完整性：`完整`（4 项缺失证据全部补齐，另补 `00R_前置确认.txt`）。
- 代码及 Unity 资产：`未修改`（Play 退出后 `git status --porcelain -- Assets ProjectSettings Packages` 输出为空）。
- Git 变更操作：`未执行`（所有提交均由用户完成；AI 只使用只读命令 `rev-parse`、`status`、`log`、`show`）。

## 遗留问题
- NavMesh 警告在本次补证中再次出现（3 条，`[15:50:55]`），原因未分析、未修复。
- Play 前 Console 的 1 条 `Couldn't find a readme` 在 Play 后不再出现，信息计数仍为 1；用户明确回复未手动清除，清除来源未确认。
- 原报告中 `Assets/Scenes/DungeonWorld/NavMesh*` 与相关 `.meta` 的修改状态，本工单未处理、未验证，仍未确认。
- `03_测试场景.png` 与 `01_工程已打开.png` 为同一张截图（复用图），场景完整路径只有“用户确认 + 文件存在核对”两个来源，无独立截图证明。
- 最近一次完整验收的具体时间与范围仍无书面记录，标为待用户确认（见 `04_构建与最近验收.txt`）。
- 本次补证未执行移动、攻击等玩法操作，第 2 项及之后仍为 `未测试`。

---

## 执行过程记录

### 1. 前置确认（两次）

| 轮次 | HEAD | `git status --porcelain` | 处置 |
|---|---|---|---|
| 首次（约 15:40） | `25ebf775…` | 有输出，仅 `?? TASK_FND-00B-01R_….md` | 按工单停止，不进入 Unity，只写本报告 |
| 复核（15:44:25） | `afc70759…` | 空 | 允许继续 |

两次记录见 `Docs/QA/Evidence/FND-00B-01/00R_前置确认.txt`；原 `00_前置确认.txt` 未被覆盖。

### 2. 构建产物核对（AI 只读）

- 根目录下级目录：`.vscode`、`Assets`、`Docs`、`docx`、`Library`、`Logs`、`Packages`、`ProjectSettings`、`Temp`、`UserSettings`，**无构建输出目录**。
- 全仓 `*.exe` / `*.apk` 搜索（排除 `Library\`、`Temp\`、`.git\`）：**结果为空**。
- `Logs\` 仅 `Packages-Update.log` 与 0 字节 `shadercompiler-UnityShaderCompiler.exe0.log`，**无 Player 日志**。
- 结论：无可运行构建，与 `REPORT_FND-00B-01` 第 4 组问答一致；细节见 `04_构建与最近验收.txt`。

### 3. Play 前

- Unity `2022.3.62f3c1`，打开工程后层级根节点为 `DungeonWorld`，未切换场景。
- Console 计数 `信息 1｜警告 0｜错误 0`，唯一条目为 `[15:46:53] Couldn't find a readme`（`ReadmeEditor.cs:90`）。
- 截图 `01_Console_编译检查.png`：15:50:05，2560×1368。

### 4. Play 中（仅观察）

- 用户进入 Play，未执行移动、攻击或点击地面等操作。
- Console 计数 `信息 1｜警告 3｜错误 0`：
  - `[15:50:55] Failed to create agent because there is no valid NavMesh` ×3
  - `[15:50:56] 0 个身体部件已挂到 PlayerBody 层`（`DebugCameraSwitcher.cs:102`）
- Game 视图可见绿色地面与 HUD（金币 0／等级 1／击杀 0）。
- 截图 `02_Play_Console.png`：15:51:25，2560×1368。
- Console 文本与 `Editor.log` 行 481–497 逐条核对一致。

### 5. Play 后

- 用户手动退出 Play，未保存任何场景或资产。
- `git status --porcelain -- Assets ProjectSettings Packages` 输出为空。

### 6. 关于截图的取证方式（必须留痕）

用户以对话图片形式提供画面，但对话附件未落盘为文件，无法直接另存。因此两张证据截图由 AI 调用 Windows `PrintWindow` 抓取**当时真实的 Unity 编辑器窗口**（2560×1368），并已逐张复核画面内容与用户描述一致。抓取 `01_Console_编译检查.png` 前，Unity 窗口处于最小化状态，AI 执行了一次窗口还原（`ShowWindow`），**未改变**工程、场景或 Console 内容。该说明同时写入 `Unity_启动与Play.log`，以免日后被误认为“用户手动截图”或“抓取时刻与描述不符”。

### 7. 证据时间线

| 时刻（本地） | 事件 |
|---|---|
| 15:40 前后 | 首次前置确认失败 → 停止 |
| 15:44:25 | 复核通过，HEAD `afc70759…` |
| 15:46:53 | Unity 打开，`Couldn't find a readme` |
| 15:50:05 | 截图 `01_Console_编译检查.png` |
| 15:50:55–56 | 进入 Play，3 条 NavMesh 警告 + 1 条 PlayerBody 日志 |
| 15:51:25 | 截图 `02_Play_Console.png` |
| 15:5x | 退出 Play |
| 提交 `af07ddb1…` | 证据文件入库（5 文件 182 行） |

## 判定说明

按原工单标准“工程正常打开、成功进入 Play、无阻塞性编译错误且证据齐全”，四项均满足，故第 1 项由 `部分通过` 改为 `通过`。NavMesh 警告属已记录异常，按工单要求原样保留、不分析、不修复，且不作为本项否决依据；其余 10 项未执行，仍为 `未测试`。统计：通过 1｜部分通过 0｜失败 0｜未测试 10。
