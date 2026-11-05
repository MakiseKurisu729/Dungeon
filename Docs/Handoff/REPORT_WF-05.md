# REPORT — WF-05：MCP 常开编辑器使用规则入册

> 对应 `Docs/Handoff/TASK_WF-05.md`。节次与 `Docs/Handoff/workflow-rules.md` §2 一致，不得删节。

## 1. 结果摘要

`完成` —— `AGENTS.md` §3.2 改为双模式（A batchmode / B MCP 常开），新增 §3.2.1 九条 MCP 细则，§6.1 补三条 MCP 熔断；`workflow-rules.md` §5.2 改双模式细则、§7 补三条勾选；七条 AC 全通过，另按所有者临时指示增补 AC-8（焦点唤醒规则）并同样通过。全程未调用任何 Unity/MCP 工具，未做任何 git 写操作。

## 2. AC 回填表

| AC | 结果 | 断言 ID | 关键数据 | 日志路径 |
|---|---|---|---|---|
| AC-1 | 通过 | 目检-1 + grep | “模式 A｜batchmode”与“模式 B｜MCP 常开编辑器”各 1 行；`Unity 必须关闭` 0 行、`Unity.*必须关闭` 0 行；`### 3.2.1 MCP 使用规则` 1 行 | `Docs/QA/Evidence/WF-05/ac1_grep.txt` |
| AC-2 | 通过 | 目检-2 | “无需逐次批准” 1 行、“只读可直接用” 1 行；只读名单 10/10 项命中（`read_console`、6 个 `mcpforunity://…` 资源、`find_gameobjects`、`get_info`、`manage_editor`） | `Docs/QA/Evidence/WF-05/ac2_grep.txt` |
| AC-3 | 通过 | 目检-3 | “写操作按单授权” 1 行；写工具名单 15/15 命中；三件套措辞 4/4 命中（任务书写明授权范围 / 影响面清单（§3.1） / 用户确认 / 缺任一项即越界） | `Docs/QA/Evidence/WF-05/ac3_grep.txt` |
| AC-4 | 通过 | 目检-4 | “同一时刻只允许一个 AI” 1；`isCompiling` 2（§3.2.1 第 5 条 + §6.1）；`run_tests` 1（“**不得**用 `run_tests` 跑这三套”）；`is_domain_reload_pending` 1；`Tools/QA/In-Editor` 1 | `Docs/QA/Evidence/WF-05/ac4_grep.txt` |
| AC-5 | 通过 | 目检-5 | 新标题“### 5.2 Unity 访问模式自证” 1、旧标题“Unity 关闭自证” 0；模式 A/B 段各 1；§7 三条 MCP 勾选 3/3；“不得**做任何 Unity 操作”仅 1 处且落在模式 A 段；“模式 A 下 Unity 未关” 1；指向 `AGENTS.md` §3.2 / §3.2.1 1 处 | `Docs/QA/Evidence/WF-05/ac5_grep.txt` |
| AC-6 | 通过（口径收窄，见 §7-1） | 目检-6 | 相对编辑前基线新增 4 项：`M AGENTS.md`、`M Docs/Handoff/workflow-rules.md`、`?? Docs/Handoff/REPORT_WF-05.md`、`?? Docs/QA/Evidence/WF-05/`；越界项 0；基线自带 19 条与本单无关的脏改动原样未动 | `Docs/QA/Evidence/WF-05/ac6_git_status.txt` |
| AC-7 | 通过 | 目检-7 | `§3.2.1` 全文引用 5 处（≥2）；全文 `§` 引用 39 处，未命中已知编号 0 处 | `Docs/QA/Evidence/WF-05/ac7_refs.txt` |
| AC-8（增补） | 通过 | 目检-8 | “卡在编辑器焦点时自行推进” 1 行（`AGENTS.md:66`）；`refresh_unity` 1；`不必询问所有者` 1；`不得借唤醒之名改任何资产` 1；§6.1 已改为“10 分钟且 §3.2.1 第 9 条的唤醒手段已用过仍无进展” 1；旧无条件表述“超 10 分钟未结束” 0；`workflow-rules.md:203` 补句 1 | `Docs/QA/Evidence/WF-05/ac8_grep.txt` |

- **运行次数**：纯文档单，按 TASK §3 不适用 N≥3；同一套自查命令完整跑 2 次（首次因 `REPORT_WF-05.md` 尚未生成导致 AC-6 新增项只有 3 条、判失败；补写 REPORT 后重跑全绿）。两次原始输出都保留：`Docs/QA/Evidence/WF-05/run1_partial/`（红）与 `Docs/QA/Evidence/WF-05/`（绿，canonical）。
- 未运行项：无。
- 证据脚本：`C:\Users\lll\AppData\Local\Temp\opencode\wf05_evidence.ps1`（一次性辅助脚本，不入库；所有命令与期望/实际都回显在对应 `.txt` 内）。

## 3. 实现原理（面向所有者）

1. **根因 / 目标**：`AGENTS.md` §3.2 原文是“Unity 必须关闭：任何 Unity 操作前先确认编辑器已关”，而日常作业已改为 MCP 直连常开编辑器，规则与行为正面矛盾，读者按效力顺序会先撞上旧条而停工。目标是把“该不该关编辑器”收敛成**唯一一处答案**：两种模式二选一，由任务书声明。
2. **备选方案与为什么没选**：方案 A（在文末新增“MCP 规则”一章、§3.2 原文不动）改动最小，但保留冲突条款，属“规则书自相矛盾”，弃用；方案 B（原地改写 §3.2 + 新增 §3.2.1 + 补 §6.1，`workflow-rules.md` 同步）把门槛与熔断放在读者必经路径上，选定。未新增架构不变量，故不写 ADR。
3. **实际数据流**：`开工 → 任务书声明模式（未声明按 A）→ 模式 B 时查 §3.2.1 判只读/写 → 写操作过 §3.1 影响面清单 + 用户确认三件套 → 作业 → 域重载门（is_compiling / is_domain_reload_pending 为 false）→ 证据`。与 TASK §2 的差异只有两处排版取舍，见第 6 条。
4. **关键位置（4 条）**：
   - `AGENTS.md:48` —— §3.2 双模式本体：A batchmode 保留原来的“进程数 0 + 无 `Temp/UnityLockfile`”自证，B 明确“经 `unityMCP` 访问，仅限 §3.2.1 与任务书授权”。
   - `AGENTS.md:54-66` —— `### 3.2.1 MCP 使用规则（模式 B）` 九条：只读白名单、写操作三件套、资产管辖权不放宽、单实例单驱动、域重载门、batchmode 不废止、编辑器内测试只走 `Tools/QA/In-Editor/*`、不并行、焦点唤醒。
   - `AGENTS.md:100-102` —— §6.1 三条 MCP 熔断；第 100 条已按所有者补充改为“10 分钟**且**§3.2.1 第 9 条唤醒手段用过仍无进展”，与第 9 条不再打架。
   - `Docs/Handoff/workflow-rules.md:186-203` —— §5.2 双模式自证细则与唤醒补句（203）；§7 三条勾选在 `251-253`。
5. **边界 / 失败模式**：最容易坏的是“§3.2 与 §3.2.1 互相矛盾”和“编号断链”。现在用两道静态检查挡：`ac5_grep.txt` 反向断言旧标题为 0，`ac7_refs.txt` 解析两份文件标题建已知编号集、再校验全文 39 处 `§` 引用；`ac1_grep.txt` 反证“Unity 必须关闭”已不存在。另一风险是模式 B 变成绕过资产管辖的后门，因此在 §3.2.1 第 3 条与 §5.2 各写一次“不放宽”。
6. **与计划的差异及原因**：
   - `### 3.2.1` 放在 §3 列表**末尾**（第 3 条之后），而不是夹在第 2、3 条之间——插在列表中间会把有序列表截成两段、第二段从“1.”重新编号，属排版损坏。
   - §3.2.1 第 5 条补了“资源 JSON 字段 `isCompiling`”的括注，因为 AC-4 的检索词是 `isCompiling`，而 TASK 要点写的是 `is_compiling`，两者是同一字段的不同拼写，写明可避免后来者以为是笔误。
   - `AGENTS.md:98` 与 `workflow-rules.md:245` 的“Unity 未关闭或状态不明”分别加上“模式 A 下”限定——不加则与模式 B 直接冲突（AC-5 要求“与 AGENTS.md 无冲突”），属工单点名章节内的最小必要措辞修正。
   - 按所有者临时指示增补第 9 条与 AC-8（含 §6.1 措辞回改、§5.2 补句）。
7. **三句话版本**：
   - **为什么拆两种模式**：封装/全量/CI 需要独占干净环境，MCP 日常作业需要编辑器活着，一条“必须关闭”管两种场景必然有一边被迫违规。
   - **只读与写的分界**：查状态、读控制台、查资源与 `get_*` 随时可用；任何会落盘的 action 必须“任务书授权 + 影响面清单 + 用户确认”三件套齐全，缺一项即越界。
   - **单驱动与域重载门挡什么**：挡两个会话同时写同一实例导致的资产互踩，以及编译/域重载未完成就继续操作导致的“改了但没生效”式假结论。

## 4. 受影响模块导航

| 模块 | 职责 | 入口（章节 / 位置） |
|---|---|---|
| 规则书 | 唯一流程入口，模式与闸门 | `AGENTS.md` §3（§3.2 双模式、§3.2.1 九条），§6.1 熔断 |
| 细则 | 模板/证据/自证/可勾选清单 | `Docs/Handoff/workflow-rules.md` §5.2（自证）、§7（熔断勾选） |
| 验收证据 | 本单静态检索输出 | `Docs/QA/Evidence/WF-05/` |

数据流：`任务书声明模式 → AGENTS §3.2/§3.2.1 判权 → 作业 → workflow-rules §5.2 自证写 REPORT → 触发 §6.1/§7 则停`

**改动文件清单**（与 TASK §4 允许改动逐项比对）

| 文件 | 改了什么 |
|---|---|
| `AGENTS.md` | §0 日期行 → WF-05；§3.2 改双模式；新增 §3.2.1（九条）；§6.1 改 1 条限定 + 补 3 条 MCP 熔断 |
| `Docs/Handoff/workflow-rules.md` | §0 日期行 → WF-05；§5.2 标题改“Unity 访问模式自证”并二分正文 + 唤醒补句；§7 改 1 条限定 + 补 3 条勾选 |
| `Docs/Handoff/REPORT_WF-05.md` | 本报告（新增） |
| `Docs/QA/Evidence/WF-05/` | 8 份检索输出 + `Summary.md` + `ExitCode.txt` + `baseline_git_status.txt` + `run1_partial/`（新增） |

未触碰：`Assets/**`、`Packages/**`、`ProjectSettings/**`、`CURRENT.md`、其他 `TASK_*`/`REPORT_*`、任何 `.meta`。

## 5. 证据清单

| 文件 | 内容 |
|---|---|
| `Docs/QA/Evidence/WF-05/ac1_grep.txt` | §3.2 双模式 + 反向断言旧表述为 0 + 新标题 |
| `Docs/QA/Evidence/WF-05/ac2_grep.txt` | 只读白名单 10 项逐条命中 |
| `Docs/QA/Evidence/WF-05/ac3_grep.txt` | 写工具 15 项 + 三件套 4 条逐条命中 |
| `Docs/QA/Evidence/WF-05/ac4_grep.txt` | 单驱动 / `isCompiling` / `run_tests` / 域重载 / 菜单入口 |
| `Docs/QA/Evidence/WF-05/ac5_grep.txt` | workflow-rules §5.2 双模式 + §7 三条勾选 + 两处冲突措辞 |
| `Docs/QA/Evidence/WF-05/ac6_git_status.txt` | `git status --porcelain` 全量 + 与基线差集 + 越界项（空） |
| `Docs/QA/Evidence/WF-05/ac7_refs.txt` | `§3.2.1` 引用 5 处 + 全文 39 处 `§` 引用的断链校验 |
| `Docs/QA/Evidence/WF-05/ac8_grep.txt` | 第 9 条焦点唤醒 + §6.1 措辞 + §5.2 补句 |
| `Docs/QA/Evidence/WF-05/Summary.md` | 每条 AC 的“跑了什么 / 期望什么 / 实际什么 / 结论” |
| `Docs/QA/Evidence/WF-05/ExitCode.txt` | `0` |
| `Docs/QA/Evidence/WF-05/baseline_git_status.txt` | 编辑前 `git status --porcelain` 基线（19 行） |
| `Docs/QA/Evidence/WF-05/run1_partial/` | 首次（红）run：AC-6 因 REPORT 未生成只新增 3 项；其余 AC 已通过 |
| `Docs/QA/Evidence/WF-05/run1_partial/NOTE.md` | 红 run 的判读说明（顺序问题，非规则缺陷） |

## 6. 学习讲稿

**不适用**。理由：本单只改两份流程规则 Markdown，不引入代码路径、运行时机制或资产结构，也没有新的架构不变量（TASK §5 亦判“不适用”）；三句话版本已写在 §3-7，足够答辩口述。

## 7. 未决项

1. **AC-6 字面口径**：AC 原文“只出现 `AGENTS.md`、`workflow-rules.md`、`TASK_WF-05.md`、`REPORT_WF-05.md`、`Evidence/WF-05/`”——实际工作区在本单开工前就有 19 条与本单无关的脏改动（`TEST-MCP-01`、`DIAG`/`SPEC01`/`FND00B06` 的 `.cs`、两个 `.csproj`、`CURRENT.md` 等，非本单产生且未触碰）。因此本单以“相对基线的**新增项** ⊆ 允许清单且越界 0”证明，基线原样存档于 `baseline_git_status.txt`，请验收时按此口径判。
2. **`refresh_unity` 工具名未经本单验证**：本单禁止调用 MCP，第 9 条按所有者口径直书该名；建议 `TEST-MCP-01` 收口时顺手确认工具名与 `editor_state` 字段拼写（`isCompiling` / `is_compiling` 两种写法已同时写入，避免读者误判笔误）。
3. **待目检（≤3）**：(a) 读 `AGENTS.md:48-66` 判断九条是否可直接执行；(b) 读 `workflow-rules.md:186-203` 确认模式 B 自证三条是否够用；(c) 确认 §7 三条勾选与 §6.1 三条熔断语义一一对应。

## 8. 建议提交

```text
docs(agents): Unity 访问拆成 batchmode 与 MCP 常开两种模式
为什么：§3.2 旧条“必须关闭编辑器”与 MCP 日常作业正面冲突，规则书出现自相矛盾条款
```

## 9. 消耗记录

- 钱包：`Go 订阅`
- 模型完整 ID：`opencode-go/deepseek-v4.1-flash`
