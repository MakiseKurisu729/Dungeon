# WF-05 — MCP 常开编辑器使用规则入册

## §0 元信息

| 字段 | 值 |
|---|---|
| 任务 ID | `WF-05` |
| 风险等级 | `常规`（纯文档：`AGENTS.md` + `Docs/Handoff/workflow-rules.md`，无代码/资产/GUID） |
| 授权 | 所有者 2026-11-05 指示第 1 条：“把 MCP 使用规则写进 AGENTS.md §3.2/§6 与 workflow-rules” |
| 基线提交 | `6872348b`（WF-04 接入 Unity MCP） |
| 回滚锚 | `git checkout -- AGENTS.md Docs/Handoff/workflow-rules.md` |
| 分支 | 当前分支 `docs/fnd-00-baseline` |
| 状态 | `Ready`（待所有者审 spec；审过即派单） |

## §1 需求（Why / What）

**一句话目标**：让 `AGENTS.md` 成为“Unity 常开 + MCP 驱动”下唯一可执行的规则，消除 §3.2 “Unity 必须关闭”与日常 MCP 作业的正面矛盾。

**用户故事**：作为所有者，我要在一份规则里看清“哪些 MCP 调用 AI 可以直接做、哪些必须先问”，以便我敢让 Unity 一直开着干活，又不必逐次口头批准只读查询。

**必须满足**

| 编号 | 需求 | 验收方式 |
|---|---|---|
| R-1 | `AGENTS.md` §3.2 改为**双模式**：模式 A `batchmode`（编辑器关闭，默认）；模式 B `MCP 常开编辑器`（按单声明启用） | 目检 + `Select-String` 关键句 |
| R-2 | 新增 §3.2.1，给出**只读白名单**（可直接用，无需逐次批准） | 同上 |
| R-3 | §3.2.1 给出**写操作清单**：必须“任务书授权 + 影响面清单 + 用户确认”三件套 | 同上 |
| R-4 | §3.2.1 写明**单实例单驱动**：同一时刻只允许一个 AI 驱动 Unity；实例不唯一先 `set_active_instance` | 同上 |
| R-5 | §3.2.1 写明**域重载门**：等 `isCompiling=false` 且 `is_domain_reload_pending=false` 再继续 | 同上 |
| R-6 | §3.2.1 写明 **batchmode 不废止**：编辑器关闭的回归/全量/CI 仍走 batchmode | 同上 |
| R-7 | §3.2.1 写明**编辑器内测试只走 `Tools/QA/In-Editor/*` 菜单**（`TEST-MCP-01`），`run_tests` 不用于 SPEC01/FND00B06/DIAG | 同上 |
| R-8 | `AGENTS.md` §6.1 熔断补 3 条 MCP 特有触发（连接连续失败、域重载久挂、越权写调用） | 同上 |
| R-9 | `workflow-rules.md` §5.2 改为双模式细则并指向 §3.2.1；§7 熔断清单补可勾选项 | 同上 |
| R-10 | 不改其它章节、不改代码、不改资产、不改 `CURRENT.md` | `git status` |

**不在范围**：MCP 工具本身的封装/改造、`TEST-MCP-01` 的菜单实现（另一张单）、Unity 版本/包变更、`Docs/Specs/constitution.md`（只 Replan 改）。

**约束**：规则以 `AGENTS.md` 为唯一入口，`workflow-rules.md` 只承载细则，冲突时 AGENTS 胜；本次不新增架构不变量，因此**不需要 ADR**。

## §2 方案（Plan）

**备选方案**

| 方案 | 做法 | 优点 | 缺点 | 结论 |
|---|---|---|---|---|
| A | 在 `AGENTS.md` 末尾新增一章 “MCP 规则”，§3.2 原文不动 | 改动最小 | 两条规则正面冲突（§3.2 说必须关编辑器），读者按效力顺序会先撞上旧条 | 弃用 |
| B | 原地改写 §3.2 + 新增 §3.2.1 + 补 §6.1，`workflow-rules.md` 同步 | 唯一入口无矛盾；门槛和熔断都在读者必经路径上 | 需要动 2 个文件的两处主体逻辑 | **选定** |

**选定方案与理由**：B 让“该不该关编辑器”这件事只有一处答案，避免后来者按旧条停工。

**数据流**：`开工 → 任务书声明模式（A/B）→ 模式 B 时按 §3.2.1 判只读/写 → 写则过影响面闸门与用户确认 → 作业 → 域重载门 → 证据`

**影响面清单**

| 项 | 内容 |
|---|---|
| 新增文件 | 无 |
| 修改文件 | `AGENTS.md`（§0 日期行、§3.2、新增 §3.2.1、§6.1）；`Docs/Handoff/workflow-rules.md`（§0 日期行、§5.2、§7） |
| 修改符号 | 无（纯 Markdown） |
| 字符串引用 | 无（文档里出现的工具名仅作规则引用，不参与编译） |
| 场景/预制体引用 | 无 |
| `.meta` | 无 |

**必须写入的规则文本要点**（执行者按此落笔，措辞可润色，条款不得增删）

1. `AGENTS.md` §3.2 改为：

   ```text
   2. **Unity 访问模式（二选一，任务书声明）**
      - **模式 A｜batchmode（默认）**：编辑器必须关闭；自证 = Unity 进程数 0 且无 `Temp/UnityLockfile`。
        封装、烘焙、全量验收、CI 用此模式。
      - **模式 B｜MCP 常开编辑器**：编辑器保持打开，经 `unityMCP` 访问，仅限 §3.2.1 白名单与任务书授权范围。
      - 重命名/移动文件必须连 `.meta` 一起；Animator 参数名、`SendMessage`、`GameObject.Find("名字")`
        等字符串引用单列核查。
   ```

2. 新增 `### 3.2.1 MCP 使用规则（模式 B）`，八条：
   1. **只读可直接用**：`read_console`；MCP 资源（`mcpforunity://editor/state`、`project/info`、`instances`、`custom-tools`、`tests` 等）；`find_gameobjects` 与 `get_*`/`get_info` 查询；`manage_editor` 的查询类 action。无需逐次批准。
   2. **写操作按单授权**：`manage_asset`、`manage_scene`、`manage_components`、`manage_gameobject` 的写 action、`manage_prefabs`、`manage_animation` 写、脚本编辑（`create_script`/`manage_script`/`apply_text_edits`/`script_apply_edits`）、`execute_code`、`manage_scriptable_object`、`manage_material`/`manage_texture`/`manage_shader` 写。必须：① 任务书写明授权范围；② 动手前输出影响面清单（§3.1）；③ 用户确认。缺一项即越界。
   3. **资产管辖权不变**：MCP 写工具不放宽 §3.3；场景/预制体/Animator/`.asset` 的最终形态由所有者确认并提交。
   4. **单实例单驱动**：同一时刻只允许一个 AI 驱动 Unity；其他会话/子代理只能只读，需要写时排队。实例不唯一且未 `set_active_instance` 时先 pin 再操作。
   5. **域重载门**：写操作后等 `editor_state.compilation.is_compiling=false` 且 `is_domain_reload_pending=false` 再继续；编译错连续 2 轮按 §6.1 熔断。
   6. **batchmode 不废止**：编辑器关闭的回归/全量/CI/封装仍走 batchmode，不得用常开编辑器替代全量验收。
   7. **编辑器内测试入口**：`SPEC01`/`FND00B06`/`DIAG` 的编辑器内执行只允许走 `Tools/QA/In-Editor/*` 菜单（`TEST-MCP-01`），由 `execute_menu_item` 触发；不得用 `run_tests` 跑这三套。
   8. **不并行**：Unity 打开时不得两个 AI 同时操作，也不得在 MCP 作业期间手工改同一资产。

3. `AGENTS.md` §6.1 末尾补：

   ```text
   - MCP 连接/工具调用连续 3 次失败；域重载后 `isCompiling` 单次超 10 分钟未结束。
   - 发现第二个 AI/会话正在写同一 Unity 实例（单实例单驱动被破坏）。
   - 未经授权调用了 §3.2.1 第 2 条列出的写工具：立即停、保留现场、按回滚锚还原。
   ```

4. `workflow-rules.md` §5.2 标题改 `5.2 Unity 访问模式自证`，正文二分：模式 A 保留原文自证；模式 B 写明“任务书已声明 + 影响面清单已确认 + `editor_state` 记录（instance_id / is_compiling / play_mode）写进 REPORT”。§7 清单追加：

   ```text
   - [ ] 模式 B 下调用了未授权写工具
   - [ ] 域重载未完成继续操作（isCompiling 仍为 true）
   - [ ] 同一 Unity 实例被两个会话同时驱动
   ```

5. `AGENTS.md` §0 的“最后更新”改为 `2026-11-05（WF-05 MCP 常开编辑器规则）`；`workflow-rules.md` 顶部日期行同步。

**实施顺序**：1. 改 `AGENTS.md`（§0/§3.2/§3.2.1/§6.1）→ 2. 改 `workflow-rules.md`（§0/§5.2/§7）→ 3. 自查交叉引用（§3.2.1 编号在全文唯一、§3.3 与 §5 引用仍成立）→ 4. 出证据。

**风险与回滚**：最大风险是改出“§3.2 与 §3.2.1 互相矛盾”或编号断链；自查项写入 AC。回滚 = `git checkout` 两个文件。

## §3 验证（Validation）

| AC | Given / When / Then | 断言 ID | 证据 |
|---|---|---|---|
| AC-1 | 给定 `AGENTS.md`，当读 §3.2，则同时存在“模式 A｜batchmode”与“模式 B｜MCP 常开编辑器”两段，且不再有“任何 Unity 操作前编辑器必须关闭”的绝对表述 | 目检-1 + grep | `Docs/QA/Evidence/WF-05/ac1_grep.txt` |
| AC-2 | 给定 §3.2.1，当读第 1 条，则列出 ≥5 个只读工具/资源名并写明“无需逐次批准” | 目检-2 | `Docs/QA/Evidence/WF-05/ac2_grep.txt` |
| AC-3 | 给定 §3.2.1，当读第 2 条，则列出写工具清单并含“任务书授权 / 影响面清单 / 用户确认”三项 | 目检-3 | `Docs/QA/Evidence/WF-05/ac3_grep.txt` |
| AC-4 | 给定 §3.2.1/§6.1，当搜“同一时刻只允许一个 AI”“isCompiling”“`run_tests`”，则三处均命中且语义与需求 R-4/R-5/R-7 一致 | 目检-4 | `Docs/QA/Evidence/WF-05/ac4_grep.txt` |
| AC-5 | 给定 `workflow-rules.md`，当读 §5.2 与 §7，则 §5.2 为双模式、§7 含 3 条 MCP 勾选项，且与 `AGENTS.md` 无冲突 | 目检-5 | `Docs/QA/Evidence/WF-05/ac5_grep.txt` |
| AC-6 | 给定跑完，当 `git status --porcelain`，则只出现 `AGENTS.md`、`Docs/Handoff/workflow-rules.md`、`Docs/Handoff/TASK_WF-05.md`、`Docs/Handoff/REPORT_WF-05.md`、`Docs/QA/Evidence/WF-05/` | 目检-6 | `Docs/QA/Evidence/WF-05/ac6_git_status.txt` |
| AC-7 | 给定全文，当搜 `§3.2.1`，则引用次数 ≥2 且无指向不存在章节的编号 | 目检-7 | `Docs/QA/Evidence/WF-05/ac7_refs.txt` |

**运行次数**：纯文档单，不适用 N≥3；同一自查命令跑 1 次即可，但每条 grep 输出须**原样**保存。

**自动化优先**：本单无法自动化（纯规则文本），全部为静态检索，属“一次性小验证”豁免，已在 AC 表写明断言 ID 与证据路径。

**必须保存的证据**：7 份 grep/status 输出（`.txt`）+ `Summary.md` + `ExitCode.txt`（写 0）。

## §4 执行信息

| 项 | 内容 |
|---|---|
| 前置 | 工作区仅 `CURRENT.md` + 两个 `.csproj` 未提交；Unity 开或关均可（本单不操作 Unity，**不得**调用任何 MCP 工具） |
| 允许改动 | `AGENTS.md`（修改）、`Docs/Handoff/workflow-rules.md`（修改）、`Docs/Handoff/REPORT_WF-05.md`（新增）、`Docs/QA/Evidence/WF-05/**`（新增） |
| 允许读取 | 本工单；`Docs/Handoff/CURRENT.md` 仅用于确认边界（不改） |
| 绝不触碰 | `Assets/**`、`Packages/**`、`ProjectSettings/**`、`CURRENT.md`、其他 `TASK_*`/`REPORT_*`、`.meta`、任何 Unity/MCP 调用、任何 git 写操作 |
| 证据目录 | `Docs/QA/Evidence/WF-05/` |
| 熔断 | 需要改到 §4/§5/§7 之外章节；需要新增 ADR；发现 `AGENTS.md` 与仓库实际冲突无法靠本单收敛 |
| 执行预算 | 1 轮实现 + 1 轮修复；文件数 2 改 2 增 |

## §5 学习产出

| 项 | 要求 |
|---|---|
| 讲稿 | **不适用**：纯流程规则文档，不涉及新机制或代码路径 |
| 三句话版本 | 仍须在 REPORT §3 写：“为什么要把 Unity 访问拆成两种模式 / 只读与写的分界在哪 / 单驱动与域重载门挡的是什么事故” |

## 自包含声明

本工单不依赖 `CURRENT.md` 或其他任务书即可执行；执行者只读本文件。信息不足时在 REPORT 写“信息不足”并列缺项，不自行翻阅其他文档补全。
