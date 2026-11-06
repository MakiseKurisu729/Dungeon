# CURRENT — 当前状态指针

> 最后更新：2026-11-06
> 新会话：读本文件 → 打开“当前 feature”的 TASK → 按“阶段”继续。**不要翻旧聊天。**
> 规则见 `AGENTS.md`；目标/范围见 `Docs/Specs/constitution.md`。

## 基线

| 项 | 值 |
|---|---|
| 分支 | `docs/fnd-00-baseline` |
| HEAD | `8e245df4`（M7-0 依赖审计） |
| Unity | `2022.3.62f3c1`（目标 6.3，未迁移） |
| 工作区 | 干净（仅 Unity 生成的 `.csproj`，已被 ignore） |

## 当前 feature

- **`M7` — 结构收口（命名空间/目录/asmdef） — `InProgress`（所有者已授权目录迁移）**
  - 审计：`Docs/Handoff/REPORT_M7-0.md` + `Docs/QA/Evidence/M7-0/`（77/77 文件、90 类型、533 边、4 个 SCC 15/4/3/2、**11 条上行边**、60/77 文件需移动）。
  - 计划：`Docs/QA/Evidence/M7-0/asmdef-plan.md`；目标程序集 `Core/Content/Gameplay/App/UI/Editor`。
  - 批准：所有者 2026-11-06 “授权 M7 目录迁移”；迁移分 4 批提交，每批跑 `REFACTOR` + `SPEC01`。
  - 下一步：`M7-1` Step 0 —— 命名空间 + 目录归一（先不做 asmdef），预计 8h。

## 已完成（本地提交，未 push）

| 任务 | 提交 | 结果 |
|---|---|---|
| `M0–M4` 重构 | `ed0cdc17`…`896e7450` | 护栏 8→16 条、UnitRegistry、状态拆分、统一交互、技能上下文 |
| `M4.5` 预制体基座 | `7e900f4b`/`df7ea0b0` | `PlayerUnit_Base` + 三职业变体；SPEC01 12/12 |
| `SKILL-CLOSE-01` | `f9472f4f` | Q/W/E/R 接槽位；`UnitMotion`；删除旧 `Bash`/`Skill` 状态与参数 |
| `QA-REFACTOR-01` | `e7ab53f1`…`66d8410b` | `Qa` 公共层、4 套件迁移、`QaLauncher`；**所有者已标 Done** |
| `M7-0` 依赖审计 | `8e245df4` | 见上 |
| `OPS-01` / `DOC-INFRA-01` / `REBASELINE-DOC` | `ba7f48bb` | `.editorconfig`、YAMLMerge 属性、csproj/sln 停止跟踪、`Docs/07`、迁移重采基线 |

## 验证基线（当前）

| 套件 | 断言数 | 最近结果 | 已知红 |
|---|---|---|---|
| `SPEC01` | 12 | 12/0 | — |
| `REFACTOR` | 16 | 16/0 | — |
| `FND00B06` | 14 | 12/0 + observed 2 | — |
| `DIAG` | 7 | 6/1 | `Diag.Input.FirstExit`（既有，不修） |

菜单：`Tools/QA/In-Editor/{SPEC01 (green), REFACTOR, FND00B06, DIAG}`。

## 当前边界

- 场景、预制体、Animator、`.asset`、`.meta`、GUID 归所有者；AI 只出清单或经批准的编辑器操作。
- 模式 B（常开编辑器）下只读 MCP 可直接用；写操作需任务书授权 + 影响面清单 + 所有者确认。
- 同一时间只允许一个 AI 驱动 Unity。
- 不改 `Docs/01` §1.4 的 14 条不变量；要改先写 ADR。

## 未决项

1. **L2 故障注入未实测**：`QA-REFACTOR-01` 的看门狗未触发过真实域重载；要验需把 Play 中脚本重载策略改为 “Recompile And Continue Playing”。
2. **`Docs/07` 中 OPS-04 已落地**（workflow-rules §11），状态可从“待写入”更新。
3. **Addressables** 暂缓；触发条件 = 第 2/3 层内容落地且需按层异步加载。
4. `*_Unit_Legacy.prefab` 仍被 `Playground.unity` 引用，暂不删。

## 下一步（按顺序）

1. **M7-1**（8h）：命名空间 + 60 文件目录归一，分 4 批：`Core+Content` → `Gameplay` → `App` → `UI/Editor`；每批跑 `REFACTOR` + `SPEC01`，提交。
2. **M7-2**（8h）：消 11 条上行边（唯一改产品代码），逐子项提交，回归升级/暂停/开箱/门。
3. **M7-3**（7.5h）：建 6 个 asmdef（Core→Content→Gameplay→App→UI→Editor）。
4. **M7-4**（4h）：全量回归 + 测试 `using` 修补 + 场景冒烟。
5. M6（池化/事件/面板）→ M5（AI 拆分）→ Unity 6.3 迁移（含 REBASELINE）。
