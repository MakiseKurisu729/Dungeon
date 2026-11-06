# FND-00A — 建立文档与事实基线

## 头部

| 字段 | 值 |
|---|---|
| 任务 ID | `FND-00A` |
| 级别 | `M` |
| 优先级 | `P0` |
| 风险 | 低（纯文档；无代码、无资产、无设置改动） |
| 状态 | `Done` |
| 负责人 | OpenCode＋DeepSeek 执行；用户审查 |
| 分支 | `docs/fnd-00-baseline` |
| 起始提交 | `e1e197dca373636a130c161b73246d8a373acc7f` |

> 状态 `Done`：文档实现已完成，并由 K3.1 独立审查通过。本任务为纯文档任务，不涉及 Unity 验收，也未填写尚不存在的最终提交哈希。

## 1. 唯一主要结果

仓库具备一个可供 K3.1、OpenCode 和其他新会话使用的文档入口：五份设计文档进入正式 `Docs/` 目录，根 `AGENTS.md` 变为简短协作入口，旧 `AGENTS.md` 归档，并建立事实基线、任务、QA、ADR 与当前交接入口。

## 2. 非目标

- 不修复 `Docs/00` 中记录的任何 Bug。
- 不产出原因分析或修复方案。
- 不开始 Unity 6.3 迁移。
- 不修改任何玩法代码、场景、预制体、动画、包、`ProjectSettings` 或 `.meta`。
- 不运行 Unity，不构建，不执行玩法测试。
- 不读取 `Assets/_Project/Scripts`，不扫描整仓，不读取根目录 `docx`。
- 不修改 `.gitignore`。
- 不执行 `git commit`、`git push` 或删除分支。
- 不改写 01～05 号设计文档正文。

## 3. 读写权限

| 类别 | 内容 |
|---|---|
| 允许读取 | 根 `AGENTS.md`；`Docs/_Incoming/` 下五份 Markdown；`ProjectSettings/ProjectVersion.txt`；本任务相关的 Git 状态与差异 |
| 允许修改 | 根 `AGENTS.md`；`Docs/` 下的 Markdown 文件 |
| 禁止修改 | `Assets/`、`Packages/`、`ProjectSettings/`、`.gitignore`、任何 `.meta`、任何 Unity 资产或场景 |

## 4. 架构与兼容约束

- 事实与规划必须分离（`Docs/Decisions/ADR-0001_事实与规划分离.md`）。
- 用户报告一律标注为"待复验"，不得写成已验收。
- 归档文档不得作为当前事实来源。
- 文档不放入 `Assets/`，不生成 `.meta`。

## 5. Unity 编辑器影响

- 不需要打开 Unity。
- 不改变 `.meta`、场景、预制体、Animator。
- 不需要用户手动导入或保存场景。
- 不需要重新构建。

## 6. 执行预算

- 一轮文档整理 + 一轮必要修正。
- 不安装任何包或插件。
- 只检查本任务要求的 Git 输出。

## 7. 验收方式

由用户审查本任务的文档差异（`git status --short`、`git diff`）：

1. 五份设计文档位于 `Docs/01`～`05`，正文与原始文件一致，`Docs/_Incoming/` 不再保留副本。
2. 旧 `AGENTS.md` 完整存在于 `Docs/Archive/2026-11-01_旧AGENTS.md`，内容未删改。
3. 新 `AGENTS.md` 简短，只承担入口职责，不含旧进度。
4. `Docs/00_项目事实基线.md` 中所有用户报告均为"待复验"，且区分了已核验事实 / 用户报告 / 目标设计 / 尚未实现 / 未知项。
5. `git diff -- Assets Packages ProjectSettings` 无输出。
6. 变更只涉及 `AGENTS.md` 与 `Docs/`。

通过后由用户将状态改为 `Done` 并提交。

## 8. 实际修改文件

**新建：**

- `Docs/README.md`
- `Docs/00_项目事实基线.md`
- `Docs/Tasks/_TEMPLATE.md`
- `Docs/Tasks/FND-00A_建立文档与事实基线.md`
- `Docs/Tasks/FND-00B_现有行为验收.md`
- `Docs/QA/README.md`
- `Docs/QA/FND-00B_现有行为验收.md`
- `Docs/QA/Evidence/README.md`
- `Docs/Decisions/README.md`
- `Docs/Decisions/ADR-0001_事实与规划分离.md`
- `Docs/Decisions/ADR-0002_当前AI协作路由.md`
- `Docs/Handoff/CURRENT.md`
- `Docs/Archive/README.md`
- `Docs/Archive/2026-11-01_旧AGENTS.md`（旧 `AGENTS.md` 的完整副本）

**重写：**

- `AGENTS.md`（改为简短协作入口）

**移动（正文未改写）：**

- `Docs/_Incoming/01_总体架构与内容生产规范.md` → `Docs/01_总体架构与内容生产规范.md`
- `Docs/_Incoming/02_Unity63迁移及毕业设计实施计划.md` → `Docs/02_Unity63迁移及毕业设计实施计划.md`
- `Docs/_Incoming/03_AI协作与项目交接文档.md` → `Docs/03_AI协作与项目交接文档.md`
- `Docs/_Incoming/04_可复用资产与编辑器工具设计清单.md` → `Docs/04_可复用资产与编辑器工具设计清单.md`
- `Docs/_Incoming/05_Agent工作流人工协作与毕设基石重构指南.md` → `Docs/05_Agent工作流人工协作与毕设基石重构指南.md`
- `Docs/_Incoming/` 目录已删除，不保留重复副本

**未修改：** 任何代码、场景、预制体、动画、包、`ProjectSettings`、`.gitignore`、`.meta`。

## 9. 真实检查与未执行项目

**已执行：**

- `git rev-parse --abbrev-ref HEAD` → `docs/fnd-00-baseline`
- `git rev-parse HEAD` → `e1e197dca373636a130c161b73246d8a373acc7f`
- 读取 `ProjectSettings/ProjectVersion.txt` → `2022.3.62f3c1`
- 五份文档按字节移动（文件大小与原件一致：31344 / 20269 / 22728 / 18585 / 25006 字节）
- `git status --short`、`git diff --stat`、`git diff --name-only`、`git diff -- Assets Packages ProjectSettings`
- K3.1 独立审查提出的七项修正已完成，文档边界与事实表述通过审查。

**未执行：**

- 未运行 Unity，未构建，未做任何玩法测试。
- 未读取 `Assets/_Project/Scripts`。
- 未读取、未移动根目录 `docx`。
- 未安装或更新任何包。
- 未执行 `git commit` / `git push` / 删除分支。

## 10. 遗留问题与下一步

- 遗留：`Docs/00` 第 3.1 节的 11 项用户报告全部尚未经 Unity 复验。
- 遗留：仓库根目录旧 `docx` 尚未梳理（已在 `Docs/Archive/README.md` 记录）。
- 下一步：FND-00A 文档已通过审查；由用户提交本次收尾更新后，开始 **FND-00B 现有行为验收**。
