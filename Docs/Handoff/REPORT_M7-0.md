# REPORT — M7-0：依赖审计（只读）

> 对应 `C:\Users\lll\.opencode\plan\TASK_M7-0_dependency-audit.md`。
> 本单只读：未改产品代码/资产/场景，未调用 Unity/MCP，未做任何 git 写操作。

## 1. 结果摘要

**完成** —— 77/77 个 `.cs` 全覆盖，产出 90 个类型、533 条文件级引用边、4 个文件级循环、77 个文件的分层建议与 8 步 asmdef 拆分计划；M7 的硬阻塞收敛为 **11 条上行边（9 个文件）**，估算 **3.5 个工作日**（最坏 5–6 天）。

## 2. AC 回填表

| AC | 结果 | 断言 ID | 关键数据 | 日志路径 |
|---|---|---|---|---|
| AC-1 | 通过 | `M7.AuditCoverage` | 实际 `.cs` 77；`files.csv` 数据行 77；无“零类型”文件 | `Docs/QA/Evidence/M7-0/files.csv`、`coverage.txt` |
| AC-2 | 通过 | `M7.EdgeTrace` | 边 533 条（跨目录 320）；矩阵含 10 个现目录、6 个目标层；每条边可回溯 `文件:行` | `module-matrix.md`、`layer-matrix.md`、`edges.csv` |
| AC-3 | 通过 | `M7.Cycles` | 4 个 SCC：15 / 4 / 3 / 2 个文件；每条内部边含 `文件:行` + 源码片段 + 建议解法 | `cycles.md`、`asmdef-plan.md` §4/§5 |
| AC-4 | 通过 | `M7.LayerPlan` | 77 个文件各 1 个目标层：Core 7 / Content 3 / Gameplay 50 / App 6 / UI 10 / Editor 1 | `layer-plan.md` |
| AC-5 | 通过 | `M7.AsmdefPlan` | 8 步顺序 + 每步验收 + 人时 + 最坏情况（3 类阻塞、6 个循环） | `asmdef-plan.md` |
| AC-6 | 通过 | `M7.ReadOnly` | `git status --porcelain -- Assets` 为空；新增仅在允许路径 | `git_status.txt` |

- **运行次数**：最终脚本成功运行 **1 次**（确定性输出）；此前 2 次失败均为脚本自身 bug（`sys` 未导入、哈希口径不对），已修正后重跑，不影响分析结论。未运行 Unity/测试（本单不需要）。
- **人工抽查**：5/5 条边真实，见 `edge-sampling.md`。

## 3. 实现原理（面向所有者）

1. **根因 / 目标**：M7 要加命名空间、目录和 asmdef，但此前没有一份可复核的依赖事实，工期只能拍脑袋。本单用只读静态分析把“谁引用谁、哪里成环、先建谁”变成可计算的数据。
2. **备选方案与为什么没选**：Roslyn 语义分析最准，但要装 dotnet 工具、超出“标准库 + 只读”约束；纯人工读 77 个文件易漏。选择**词边界近似分析**：先剥离注释/字符串，再按“类型名 → 声明文件”建边，保留 `文件:行` 供人工抽查；代价是可能把同名成员误判为类型引用，已在 `cycles.md` 标注需人工确认。
3. **实际数据流**：`77 个 .cs → 剥离注释/字符串 → 90 个类型声明 → 533 条文件级边（含行号）→ 模块矩阵 / 层矩阵 → Tarjan SCC → 分层与 asmdef 计划`。
4. **关键位置（3–5 条）**：
   - `Docs/QA/Evidence/M7-0/audit_scripts.py:1` —— 唯一分析脚本（标准库，可复现）。
   - `Docs/QA/Evidence/M7-0/audit_scripts.py:76` —— 目标层映射（按职责，不按现目录）。
   - `Docs/QA/Evidence/M7-0/audit_scripts.py:200` —— 类型→文件、文件→类型建边与过滤。
   - `Docs/QA/Evidence/M7-0/audit_scripts.py:360` —— 迭代版 Tarjan（文件级 SCC）。
   - `Docs/QA/Evidence/M7-0/asmdef-plan.md:1` —— 拆分顺序、阻塞边、工期与最坏情况。
5. **边界 / 失败模式**：同名大写成员（如 `Foo.Bar` 中 `Bar` 恰好是类型名）可能误报；字符串里的类型名（`SendMessage("X")`）不会计入；`#if` 分支按全文计入（未做预处理）。因此所有循环结论都附源码片段，改代码前必须人工确认。
6. **与计划的差异及原因**：TASK §2 建议“按现有目录聚合”，实际同时产出了**按目标层**的矩阵——因为 M7 的 asmdef 边界是目标层而非现目录，只看现目录会低估“Core 目录里 8/12 个文件其实不是 Core”这一问题。
7. **三句话版本**：
   - 77 个脚本、533 条引用边里，真正挡住 asmdef 的只有 11 条“下层调上层”（Gameplay→App 8 条、Gameplay→UI 3 条）。
   - 最大的工作量不是建 asmdef，而是 60/77 个文件要按目标层挪目录，以及给 77 个文件补命名空间与 `using`。
   - 先建零依赖的 Core/Content，再消 11 条上行边，最后建 Gameplay/App/UI/Editor；基准 3.5 天，最坏 5–6 天。

## 4. 受影响模块导航

| 模块 | 职责 | 入口（类 / 方法 / 场景对象） |
|---|---|---|
| `Core`（现目录） | 事件、ID、消息、局流程、注册表混在一起 | `EventBus`、`UnitId`、`MessageFeed`、`UnitRegistry`、`AppFlow` |
| `Units` | 单位生命/AI/技能/表现 | `UnitHealth`、`CharacterBrain`、`SkillSystem` |
| `Combat` | 近战/远程/投射物/技能效果 | `MeleeCombat`、`RangedCombat`、`SkillEffectBase` |
| `Player` | 选择、指令、技能输入 | `SelectionManager`、`SkillInputController` |
| `System` | 宝箱、门、迷雾、生成器 | `Chest`、`DungeonDoor`、`FogOfWar` |
| `UI` | 面板与 HUD | `UIManager`、`InventoryUI`、`HUD` |
| `Items` / `Framework` / `Gameplay` / `Debug` | 背包、冷却、交互、调试组件 | `Inventory`、`Cooldown`、`InteractionRunner`、`DebugCameraSwitcher` |

数据流（现状）：`Player 输入 → CharacterBrain/InteractionRunner → Combat/UnitHealth → EventBus/MessageFeed → UI`；其中 `Player/System/Units` 反向直接调用 `AppFlow` 与 `UIManager`，这正是 11 条上行边的来源。

**改动文件清单**（与 TASK §4 允许改动逐项比对）

| 文件 | 改了什么 |
|---|---|
| `Docs/QA/Evidence/M7-0/audit_scripts.py` | 新增：只读分析脚本 |
| `Docs/QA/Evidence/M7-0/files.csv` | 新增：77 个文件的清单、层、行数、SHA256 |
| `Docs/QA/Evidence/M7-0/types.csv` | 新增：90 个类型 → 文件 |
| `Docs/QA/Evidence/M7-0/edges.csv` | 新增：533 条文件 → 类型边（含行号与片段） |
| `Docs/QA/Evidence/M7-0/edges.json` | 新增：同上 JSON 版 |
| `Docs/QA/Evidence/M7-0/stats.json` | 新增：统计与上行边清单 |
| `Docs/QA/Evidence/M7-0/coverage.txt` | 新增：覆盖率自检（全 PASS） |
| `Docs/QA/Evidence/M7-0/run.log` | 新增：脚本运行输出 + ExitCode=0 |
| `Docs/QA/Evidence/M7-0/module-matrix.md` | 新增：现目录依赖矩阵 + 来源文件 |
| `Docs/QA/Evidence/M7-0/layer-matrix.md` | 新增：目标层矩阵 + 11 条违规边 |
| `Docs/QA/Evidence/M7-0/cycles.md` | 新增：4 个 SCC 的逐边清单 |
| `Docs/QA/Evidence/M7-0/layer-plan.md` | 新增：77 个文件的目标层与理由 |
| `Docs/QA/Evidence/M7-0/asmdef-plan.md` | 新增：拆分顺序、阻塞边、工期、最坏情况 |
| `Docs/QA/Evidence/M7-0/graph.mmd` | 新增：Mermaid 文件级依赖图 |
| `Docs/QA/Evidence/M7-0/edge-sampling.md` | 新增：5 条边人工抽查 |
| `Docs/QA/Evidence/M7-0/git_status.txt` | 新增：只读证明 |
| `Docs/Handoff/REPORT_M7-0.md` | 新增：本报告 |

未改：`Assets/**`（`git status --porcelain -- Assets` 为空）、`CURRENT.md`、其他 TASK/REPORT。

## 5. 证据清单

| 文件 | 内容 |
|---|---|
| `Docs/QA/Evidence/M7-0/coverage.txt` | 77/77 覆盖、类型/边计数、自检全 PASS |
| `Docs/QA/Evidence/M7-0/run.log` | 脚本 stdout + `ExitCode=0` |
| `Docs/QA/Evidence/M7-0/files.csv` | 77 行文件清单（含 SHA256，可复核未变） |
| `Docs/QA/Evidence/M7-0/types.csv` | 90 行类型表 |
| `Docs/QA/Evidence/M7-0/edges.csv` / `edges.json` | 533 条边，含 `文件:行` 与源码片段 |
| `Docs/QA/Evidence/M7-0/module-matrix.md` / `layer-matrix.md` | 模块/层矩阵，含违规方向逐边定位 |
| `Docs/QA/Evidence/M7-0/cycles.md` | 4 个 SCC 的完整内部边 |
| `Docs/QA/Evidence/M7-0/layer-plan.md` | 77 个文件的唯一目标层与理由 |
| `Docs/QA/Evidence/M7-0/asmdef-plan.md` | 8 步拆分、验收、人时、最坏情况 |
| `Docs/QA/Evidence/M7-0/graph.mmd` | Mermaid 依赖图 |
| `Docs/QA/Evidence/M7-0/edge-sampling.md` | 5/5 抽查真实 |
| `Docs/QA/Evidence/M7-0/git_status.txt` | 只读证明 + 基线/HEAD 变化说明 |

## 6. 学习讲稿

不适用（分析类小单，TASK §5 明确只写三句话版本，见 §3.7）。

## 7. 未决项

- **仓库 HEAD 在本单执行期间变化**：开工 `5df13b1a` → 完成 `66d8410b`（所有者提交 2 次）。已核验 `git diff 5df13b1a..66d8410b -- Assets/_Project/Scripts` 为 0 行，审计结果仍对应同一份代码；详见 `git_status.txt`。
- **近似分析的残余误报面**：与类型同名的大写成员访问、`#if` 分支未预处理。改代码前需按 `cycles.md` 的源码片段人工确认。
- **`Assembly-CSharp*.csproj` / `Dungeon.sln` 当前为已暂存删除**（`D `），非本单操作；提交前请所有者确认这是预期的生成物清理。
- **待目检（≤3）**：① `layer-plan.md` 中 6 个“目标层与职责冲突”文件的处理意见；② `asmdef-plan.md` §4 的 4 个解法是否符合预期架构；③ 是否把 `RunState` 放 Core（放 App 会多 5 条上行边）。

## 8. 建议提交

```text
docs(m7-0): 产出脚本依赖审计与 asmdef 拆分计划
为什么：M7 动手前需要可复核的依赖图、循环清单与工期，避免拍脑袋拆程序集
```

## 9. 消耗记录

- 钱包：`Go 订阅`
- 模型完整 ID：`opencode-go/deepseek-v4.1-flash`
