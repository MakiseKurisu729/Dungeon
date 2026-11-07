# REPORT — M7-4 全量回归与 M7 收口

> 工单：`Docs/交接/TASK_M7-4_全量回归.md`
> 长任务计划：`Docs/交接/长任务计划_M7-4.md`
> 状态：**完成；2026-11-07 所有者目检通过，M7 已 `Done`（含 M7-FIX-01/01B/01C 修复）**。未 push。

## 1. 结果摘要

- 四套件各连续 2 次，全部与基线一致：
  - `REFACTOR`：20/0 ×2（`editor_run42`、`editor_run43`），`code=0`
  - `SPEC01`：12/0 ×2（`editor_run61`、`editor_run62`），`code=0`
  - `FND00B06`：12/0 + observed 2 ×2（`editor_run11`、`editor_run12`），`code=0`
  - `DIAG`：6/1 ×2（`editor_run13`、`editor_run14`），`code=2`；唯一红为已知 `Diag.Input.FirstExit`
- `DungeonWorld` Play 30 秒：0 `error CS` / 0 `Missing script` / 0 `NullReferenceException`；退出后 `isDirty=false`。
- 静态扫描 42 个 `.unity/.prefab`、199 条 `m_Script` 引用：M7 未引入新的 Missing Script（`unresolved=0`）；发现 **2 个基线已存在的第三方缺失**（`PlayerCapsule.prefab`，M7 未改 Assets），列为 known issue，未修。
- 6 个 asmdef 字段/引用/平台全部正确；`Dungeon.Editor` 仅 Editor 平台，运行时不回引。
- 本单只写 `Docs/**`：`git diff 0c4c32e4..HEAD -- Assets` 为空；2 条提交，收口后 `git status` 干净、未 push。
- M7 全量提交（`483678fb..1c013dc4`）：**24 条**（M7-1 9 + M7-2 6 + M7-3 6 + M7-4 3；M7-4 为开工/收口/状态登记三条；比旧计划写的 21 多 3，另 2 条是 M7-1 收尾的空目录清理/交接）。

**三句话版本（M7 收口版）**

1. 81 个脚本已归一为 6 个程序集（Core/Content/Gameplay/App/UI/Editor），`upward=0`，依赖方向由 asmdef 在编译期锁死。
2. 四套件 ×2 全部与基线一致，`DIAG` 仅保留已知红 `Diag.Input.FirstExit`；场景冒烟无 Missing/NRE/error，**自动化基线未回归**。
3. **人工功能项 2/4/5 未通过/未验证**（Esc 无响应、宝箱不可交互、门未测试；详见 `M7-FIX-01` 与 `醒来目检清单`），不能用“无行为回归”概括。

## 2. AC 回填表

| AC | 断言 ID | 结果 | 实测数据 | 证据 |
|---|---|---|---|---|
| AC-1 | `M7-4.Suites` | **PASS** | 四套件各 2 次：REFACTOR 20/0 ×2；SPEC01 12/0 ×2；FND00B06 12/0+2obs ×2；DIAG 6/1 ×2（已知红 `Diag.Input.FirstExit`），`code` 分别 0/0/0/2 | `suites-REFACTOR/run1..2/`、`suites-SPEC01/`、`suites-FND00B06/`、`suites-DIAG/` |
| AC-2 | `M7-4.PlaySmoke` | **PASS** | 30 秒：`error CS=0`、`Missing script=0`、`NullReferenceException=0`；退出后 `isDirty=false` | `play-smoke.md` |
| AC-3 | `M7-4.SceneRefs` | **PASS（含 2 个基线既有例外）** | 42 文件 / 199 refs / 15486 metas；`unresolved=0`；`preexisting=2`（`5135d2b4…`、`9466e037…`，仅 `StarterAssets/.../PlayerCapsule.prefab`，`pre_base_ok=True`） | `scene-refs.txt`、`verify.txt` |
| AC-4 | `M7-4.Asmdef` / `.EditorOnly` | **PASS** | 6 个 asmdef 字段一致；`Dungeon.Editor.includePlatforms==["Editor"]`；运行时 `references` 不含 Editor | `asmdef.txt`、`verify.txt` |
| AC-5 | `M7-4.Files` | **PASS** | `Scripts=81`、`Tests=30`、测试 `namespace` 文件 0；`git status` 仅本单 Docs；收口后干净 | `verify.txt` |
| AC-6 | `M7-4.Wake` | **PASS（仅证明清单文件存在）** | `醒来报告.md`、`醒来目检清单.md` 存在且含 push 前检查/已知问题/恢复步骤；**不证明 2/4/5 功能项通过**（见 `M7-FIX-01`） | `Docs/QA/Evidence/M7-4/醒来报告.md`、`醒来目检清单.md` |
| AC-7 | `M7-4.NoAsset` | **PASS** | `git diff --name-only 0c4c32e4..HEAD -- Assets` = **0** | `no-asset.txt` |
| AC-8 | `M7-4.Commits` | **PASS** | 2 条中文提交（开工文档 + 收口）；`git status` 干净；未 push | `收口/git-log.txt` |

## 3. 实现原理（验收口径）

- **四套件**：每套都从 `Tools/QA/In-Editor/<suite>` 菜单进 Play 跑生产入口，证据由 `QaEvidence` 写 `Summary(pass/fail/blocked/observed)` 与 `InEditorResult(code)`；连续两次用同一入口，检查 `pass/fail/observed` 一致。
- **场景引用扫描**（`Docs/QA/Evidence/M7-4/verify_m7_4.py:126` 起）：
  1. 收集 `Assets/**/*.meta` + `Packages/**/*.meta` + `Library/PackageCache/**/*.meta` 的 GUID（15486 个）；
  2. 正则抓取 `.unity`/`.prefab` 的 `m_Script: {..., guid: X, type: 3}`（`script_re`，`:146`）；
  3. 缺失集合减去 `PREEXISTING` 白名单后必须为空；
  4. `PREEXISTING` 两个 GUID 还通过 `git show 0c4c32e4:<prefab>` 验证确实基线已存在（`pre_base_ok=True`）。
- **asmdef 校验**（`:38-44`）：`SPECS` 表集中定义 6 个程序集的 `references`/`includePlatforms`，逐字段比对。
- **Files/NoAsset**（`:170` 起 / `:200` 起）：81/30 文件、测试无 `namespace`、`git status` 只允许本单 Docs；`git diff base..HEAD -- Assets` 必须为空。

## 4. 受影响模块导航

- 本单**零产品改动**：`Assets/**`、`ProjectSettings`、`Packages` 均未改。
- 仅新增/更新 `Docs/**`：`Docs/QA/Evidence/M7-4/**`、`Docs/交接/REPORT_M7-4_全量回归.md`，并提交现成的 `当前状态.md` 与工单/计划。

## 5. 证据清单

| 证据 | 路径 |
|---|---|
| 套件 ×8 | `Docs/QA/Evidence/M7-4/suites-{REFACTOR,SPEC01,FND00B06,DIAG}/run{1,2}/Summary.md` |
| 场景引用扫描 | `Docs/QA/Evidence/M7-4/scene-refs.txt` |
| asmdef | `Docs/QA/Evidence/M7-4/asmdef.txt` |
| Play 冒烟 | `Docs/QA/Evidence/M7-4/play-smoke.md` |
| 结构验证器 | `Docs/QA/Evidence/M7-4/verify_m7_4.py`、`verify.txt`、`ExitCode.txt` |
| 无资产改动 | `Docs/QA/Evidence/M7-4/no-asset.txt` |
| M7 全量提交 | `Docs/QA/Evidence/M7-4/收口/git-log.txt` |
| 醒来文档 | `Docs/QA/Evidence/M7-4/醒来报告.md`、`醒来目检清单.md` |
| canonical 套件 | `REFACTOR-M4/editor_run42/43`、`SPEC-01/editor_run61/62`、`FND-00B-06/editor_run11/12`、`DIAG-SKILL/editor_run13/14` |

## 6. 学习讲稿

- **不适用**：本单是纯验收 + 文档，无新设计/无产品改动；M7 的设计取舍已由 `Docs/学习/M7-1_讲稿.md`、`M7-2_讲稿.md`、`M7-3_讲稿.md` 覆盖。

## 7. 未决项

1. **2 个基线既有 Missing Script（不归 M7）**：`Assets/StarterAssets/FirstPersonController/Prefabs/PlayerCapsule.prefab` 行 275/345 的 GUID `5135d2b4fb4ea6b4183453c2252e87bb`、`9466e0370f31dfe47ae24c150b7fa083` 在 `Assets/Packages/PackageCache` 均无定义；基线 `0c4c32e4` 同样存在，M7 未改 Assets。未修（超出本单范围），已列入《醒来目检清单》。
2. `DIAG` 已知红 `Diag.Input.FirstExit` 保持 6/1，未修、未改阈值。
3. Player 构建/真机未做（TASK 范围外）；`Dungeon.Editor` 的 Player 平台排除仅做 `includePlatforms` 静态校验。
4. `FND00B06` 的 2 条 `observed` 是既有观察项，非失败。
5. M7 全量提交实际 **24 条**（`483678fb..1c013dc4`；M7-4 为 3 条：开工/收口/状态登记；另 2 条为 M7-1 收尾的空目录清理 `a939310e` 与交接 `0a78111a`）。
6. **人工功能项 2/4/5 未通过/未验证**（所有者 2026-11-07 目检：Esc 无响应、宝箱不可交互、门未测试）；已由 `M7-FIX-01` 做最小 wiring 修复，真实键鼠路径仍需目检。详见 `REPORT_M7-FIX-01`。

## 8. 建议提交

| # | hash | 标题 |
|---|---|---|
| 0 | `5ba1b1a8` | `文档：M7-4 工单与长任务计划（M7-4/开工）` |
| 1 | 见 `收口/git-log.txt` | `测试：M7-4 全量回归与场景冒烟（M7-4/收口）` |

- 均**未 push**；不合并分支、不改写历史。

## 9. 消耗记录

- 墙钟：2026-11-07 10:25–10:50（约 25 分钟）。
- 套件运行：8 次（REFACTOR/SPEC01/FND00B06/DIAG 各 2 次）；Play 冒烟 1 次（30 秒）。
- MCP 写调用：0；`execute_menu_item` 8 次；`manage_editor` play/stop 各 1 次。
- Token/额度：执行环境未提供计数。
