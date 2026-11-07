# M7-4 — 全量回归与 M7 收口

> 本工单自包含：执行者只读本文件 + `Docs/交接/长任务计划_M7-4.md`。
> 前置：M7-1（目录/命名空间）、M7-2（`upward=0`）、M7-3（6 个 asmdef）已完成。

## §0 元信息

| 字段 | 值 |
|---|---|
| 任务 ID | `M7-4` |
| 风险等级 | **常规**（纯验收 + 文档；**不改代码/资产**） |
| 授权 | 所有者 2026-11-07：M7 全程不需要在场；允许本地自动提交（不 push） |
| 基线提交 | `0c4c32e4`（开工复核；不一致即停） |
| 回滚锚 | `0c4c32e4`（本单无代码改动，回滚 = revert 文档提交） |
| 分支 | `docs/fnd-00-baseline` |
| 提交方式 | **允许自动提交**：2 条中文提交（开工文档 + 收口证据） |
| 状态 | `Ready` |

基线（大脑 2026-11-07 核验）：`Scripts` 81 个 `.cs`；6 个 asmdef；`upward=0`；`REFACTOR` 20/0、`SPEC01` 12/0、`FND00B06` 12/0+2obs、`DIAG` 6/1（已知红）；Unity 模式 B `Dungeon@4f5c34c82f31646e`，场景 `DungeonWorld` 非脏。

## §1 需求（Why / What）

**一句话目标**：用**全量四套件 ×2 + 场景冒烟 + 静态引用扫描**证明 M7 结构收口没有破坏行为，并产出 M7 级《醒来报告》《醒来目检清单》，把 M7 停在 `Review` 交给所有者 push + 终审。

| 编号 | 需求 | 验收 |
|---|---|---|
| R-1 | 四套件各连续 2 次，结果与基线一致：`REFACTOR` 20/0、`SPEC01` 12/0、`FND00B06` 12/0+2obs、`DIAG` 6/1（已知红不变） | `M7-4.Suites` |
| R-2 | 主场景 `DungeonWorld` 进 Play 30 秒：无 `Missing script`/`NullReferenceException`/`error CS`；退出后场景非脏 | `M7-4.PlaySmoke` |
| R-3 | 静态扫描所有 `.unity`/`.prefab` 的 `m_Script` GUID，全部能在 `Assets/**/*.meta` 中找到；无 Missing Script | `M7-4.SceneRefs` |
| R-4 | 6 个 asmdef 存在且字段正确；`Dungeon.Editor` 仅 Editor 平台；运行时程序集不回引 Editor | `M7-4.Asmdef` |
| R-5 | 测试 30 个文件仍无 `namespace`；`Scripts` 81 文件；`git status` 干净 | `M7-4.Files` |
| R-6 | 产出 M7 级《醒来报告》《醒来目检清单》（覆盖 M7-1～M7-4 + 已知问题 + push 前检查） | `M7-4.Wake` |
| R-7 | 本单不改任何 `.cs`/场景/预制体/资产；只改 `Docs/**` | `M7-4.NoAsset` |
| R-8 | 2 条中文提交；未 push | `M7-4.Commits` |

**不在范围**：修 `DIAG` 已知红；真机/player 构建；M6/M5/Unity 6.3；改 `当前状态.md`；改 `Docs/01`。

**约束**：`DIAG` 的 `Diag.Input.FirstExit` 是**已知红**，两次都应 6/1；**不得**顺手修它、不得改测试阈值。

## §2 方案（Plan）

**备选方案**

| 方案 | 做法 | 优点 | 缺点 | 结论 |
|---|---|---|---|---|
| A | 只跑一次四套件 | 快 | 无法排除间歇 | 弃用 |
| B | 四套件各连续 2 次 + Play 冒烟 + 静态引用扫描 + 醒来清单 | 覆盖行为/引用/结构 | 约 8–10 分钟 | **选定** |
| C | 加 batchmode 全量 | 最接近 CI | 需关闭编辑器，超长任务范围 | 不在本单（留给 CI/封装） |

**数据流**：`菜单触发 → 套件进 Play → Summary(pass/fail) + InEditorResult(code) → 证据`；静态扫描 `meta guid ↔ scene/prefab m_Script guid`。

**实施顺序**

1. 复核基线；提交开工文档（`当前状态.md` 现成内容 + 本 TASK + 计划）→ `文档：M7-4 工单与长任务计划（M7-4/开工）`。
2. `REFACTOR` ×2 → `SPEC01` ×2 → `FND00B06` ×2 → `DIAG` ×2；每套记 `pass/fail/observed` 与 `code`。
3. Play 30 秒冒烟（`DungeonWorld`）→ 记录 Console。
4. 写 `verify_m7_4.py`：asmdef 字段、Editor-only、场景/预制体 `m_Script` GUID、81/30 文件、`git status`；跑绿。
5. 写 `REPORT_M7-4` + `Docs/QA/Evidence/M7-4/醒来报告.md` + `醒来目检清单.md` + `收口/git-log.txt`。
6. 提交 `测试：M7-4 全量回归与场景冒烟（M7-4/收口）`。

**风险与回滚**

| 风险 | 信号 | 应对 |
|---|---|---|
| 套件红（非已知红） | `fail>0` | 停，留日志；不自行修代码（本单不改代码），写 REPORT 阻塞 |
| `DIAG` 变成 6/0 或 6/2 | 与基线不符 | 记录差异，不修；在 REPORT 未决项标出 |
| 场景 Missing Script | 静态扫描有缺失 | 停，列缺失 GUID/文件，交给所有者 |
| 场景变脏导致拒跑 | `isDirty=true` | 不保存、不切场景，停并报告 |

## §3 验证（Validation）

| AC | Given / When / Then | 断言 ID | 证据 |
|---|---|---|---|
| AC-1 | 四套件各连续 2 次，结果为 `REFACTOR` 20/0、`SPEC01` 12/0、`FND00B06` 12/0+2obs、`DIAG` 6/1 | `M7-4.Suites` | `Docs/QA/Evidence/M7-4/suites-*/run1..2/` |
| AC-2 | Play 30 秒无 `Missing script`/NRE/`error CS`；退出后 `isDirty=false` | `M7-4.PlaySmoke` | `play-smoke.md` |
| AC-3 | 所有 `.unity`/`.prefab` 的 `m_Script` GUID 都能在 `.meta` 中找到 | `M7-4.SceneRefs` | `scene-refs.txt` |
| AC-4 | 6 个 asmdef 字段正确；Editor 仅 Editor；运行时不回引 Editor | `M7-4.Asmdef` | `asmdef.txt` |
| AC-5 | `Scripts` 81、`Tests` 30 且无 `namespace`；`git status` 干净 | `M7-4.Files` | `verify.txt` |
| AC-6 | 《醒来报告》《醒来目检清单》存在且含“push 前检查/已知问题/恢复步骤” | `M7-4.Wake` | `醒来报告.md`、`醒来目检清单.md` |
| AC-7 | `git diff 0c4c32e4..HEAD -- Assets` 为空（本单不改资产） | `M7-4.NoAsset` | `no-asset.txt` |
| AC-8 | 2 条中文提交，`git status` 干净，未 push | `M7-4.Commits` | `git-log.txt` |

**运行次数**：四套件各 **连续 2 次**（同一入口）；两次 `pass/fail/observed` 必须一致。

**检查器** `Docs/QA/Evidence/M7-4/verify_m7_4.py`：

| 断言 ID | 内容 |
|---|---|
| `M7-4.Asmdef` | 6 个 asmdef + 字段/引用/平台 |
| `M7-4.SceneRefs` | 扫 `Assets/**/*.unity`、`**/*.prefab` 的 `m_Script` GUID → 缺失列表为空 |
| `M7-4.Files` | 81 脚本 / 30 测试 / 测试无 namespace / `git status` 空（或只含本单 Docs） |
| `M7-4.NoAsset` | `git diff --name-only 0c4c32e4..HEAD -- Assets` 为空 |

**人工目检**：见《醒来目检清单》（≤8 项，所有者执行）。

## §4 执行信息

| 项 | 内容 |
|---|---|
| 前置 | HEAD `0c4c32e4`、工作区干净；模式 B；场景非脏；`py -3.12` |
| 允许改动 | **只改 `Docs/**`**：`Docs/QA/Evidence/M7-4/**`、`Docs/交接/REPORT_M7-4_全量回归.md`、`当前状态.md`（只提交现成内容） |
| 绝不触碰 | `Assets/**`（任何文件）、`ProjectSettings`、`Packages`、`Docs/01`、其他 TASK/REPORT/证据 |
| MCP 授权 | 只读 `editor_state`/`read_console`；`execute_menu_item`（四套件）；`manage_editor` play/stop（仅冒烟）；`refresh_unity`。禁止任何资产写 |
| 提交策略 | 允许自动提交：开工文档 1 条 + 收口 1 条；不 push |
| 熔断 | 非已知红；场景 Missing；资产需要改；MCP 连续 3 次失败；发现第二个 AI 写同一实例 |

## §5 学习产出

| 项 | 要求 |
|---|---|
| 讲稿 | **不适用**：本单是纯验收 + 文档，无新设计；M7 的设计取舍已由 `M7-1/2/3_讲稿.md` 覆盖。REPORT §6 写此理由 |
| 三句话版本 | REPORT §3.7（M7 收口版） |

## 自包含声明

本工单不依赖 `当前状态.md` 或其他任务书即可执行。信息不足时在 REPORT 写“信息不足”并列缺项，不自行取舍。
