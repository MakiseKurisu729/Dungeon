# REPORT — M7-3 建 Gameplay / App / UI / Editor 程序集

> 工单：`Docs/交接/TASK_M7-3_建程序集.md`
> 长任务计划：`Docs/交接/长任务计划_M7-3.md`
> 状态：**完成（M7-3 全范围，停在 `Review` 等终审）**。执行者不标 `Done`、不改 `当前状态.md`、不 push。

## 1. 结果摘要

- 6 个程序集全部建成：`Dungeon.Core` / `Dungeon.Content` / `Dungeon.Gameplay` / `Dungeon.App` / `Dungeon.UI` / `Dungeon.Editor`；依赖方向由 `references` 固定为 `Core ← Content ← Gameplay ← App ← UI`，Editor 旁挂且仅 Editor 平台。
- **未改任何 `.cs`**：`git diff 164013c3..HEAD -- Assets` 共 8 个路径，全部是 4 个新 `.asmdef` + 4 个 `.asmdef.meta`；`Scripts` 仍 81 个 `.cs`、`Tests` 仍 30 个。
- 每步编译 0 `error CS`；`REFACTOR` 20/0、`SPEC01` 12/0；`upward_layer_edges` 始终 0；UI 与 Editor 步各做 Play 30 秒冒烟，均 0 `error CS` / 0 `Missing script` / 0 `NullReferenceException`。
- Editor 步 `REFACTOR`/`SPEC01` 各连续 2 次一致；`Dungeon.Editor.includePlatforms==["Editor"]`；5 个运行时 asmdef 均不引用 `Dungeon.Editor`。
- 6 条提交（文档 1 + 程序集 4 + 收口 1），不 push；收口后 `git status` 干净。

**三句话版本**

1. 四个目录各自变成独立程序集，引用方向在编译期被锁死，M7-1/M7-2 的目录/依赖归一终于落成硬边界。
2. 全程只新增 asmdef、零代码改动，每步编译与两套回归都保持基线，行为不变。
3. Editor 工具被限定在 Editor 平台，运行时程序集不会反向引用它；M7-4 只需做全量回归与最终目检。

## 2. AC 回填表

| AC | 断言 ID | 结果 | 实测数据 | 证据 |
|---|---|---|---|---|
| AC-1 | `M7-3.Asmdef` | **PASS** | 6 个 asmdef 存在；`name`/`rootNamespace`/`references`/`includePlatforms`/`autoReferenced`/`noEngineReferences` 全部一致 | `收口/asmdef.md`、`收口/verify.txt` |
| AC-2 | `M7-3.Compile` | **PASS** | Gameplay/App/UI/Editor 每步 `is_compiling=false`、`is_domain_reload_pending=false`、Console 0 `error CS`；最后一次编译后 Editor.log 0 条 `error CS` | `Gameplay/compile.md`、`App/compile.md`、`UI/compile.md`、`Editor/compile.md` |
| AC-3 | `M7-3.REFACTOR` / `.SPEC01` | **PASS** | 每步 `REFACTOR` 20/0、`SPEC01` 12/0；Editor 步各连续 2 次（run40/41、run59/60）一致 | 各步 `suites.md` |
| AC-4 | `M7-3.EditorOnly` | **PASS** | `Dungeon.Editor.includePlatforms==["Editor"]`；Core/Content/Gameplay/App/UI 的 `references` 不含 `Dungeon.Editor`；`includePlatforms=[]` | `收口/editor-only.txt` |
| AC-5 | `M7-3.PlaySmoke` | **PASS** | UI 步与 Editor 步各 30 秒：0 `error CS` / 0 `Missing script` / 0 `NullReferenceException`；退出后场景 `isDirty=false` | `UI/play-smoke.md`、`Editor/play-smoke.md`、`收口/play-smoke.md` |
| AC-6 | `M7-3.NoAsset` | **PASS** | `git diff --name-only 164013c3..HEAD -- Assets` = 8（4 `.asmdef` + 4 `.asmdef.meta`）；`bad=0`；无 `.cs` 改动 | `收口/no-asset.txt` |
| AC-7 | `M7-3.Commits` | **PASS** | 6 条中文提交（开工 + 4 程序集 + 收口）；标题/正文含 `为什么：`/`验证：`；`git status` 干净；未 push | `收口/git-log.txt` |

## 3. 实现原理

### 3.1 依赖链（编译期硬边界）

```text
Dungeon.Core (refs [])
  ← Dungeon.Content (refs [Core])
    ← Dungeon.Gameplay (refs [Core, Content, UnityEngine.UI])
      ← Dungeon.App (refs [Core, Content, Gameplay])
        ← Dungeon.UI (refs [Core, Content, Gameplay, App, UnityEngine.UI])
          ← Dungeon.Editor (refs 全部运行时，includePlatforms ["Editor"])
```

- `Assets/_Project/Scripts/Gameplay/Dungeon.Gameplay.asmdef:4`：`references = ["Dungeon.Core", "Dungeon.Content", "UnityEngine.UI"]`。
- `Assets/_Project/Scripts/App/Dungeon.App.asmdef:4`：`references = ["Dungeon.Core", "Dungeon.Content", "Dungeon.Gameplay"]`。
- `Assets/_Project/Scripts/UI/Dungeon.UI.asmdef:4`：`references = ["Dungeon.Core", "Dungeon.Content", "Dungeon.Gameplay", "Dungeon.App", "UnityEngine.UI"]`。
- `Assets/_Project/Scripts/Editor/Dungeon.Editor.asmdef:5`：`includePlatforms = ["Editor"]`；`references` 覆盖全部 5 个运行时程序集。
- `Docs/QA/Evidence/M7-3/verify_m7_3.py:38-44`：`SPECS` 表逐字段比对 §2.1；`:96` 起 `EditorOnly`；`:115` 起 `NoAsset`。

### 3.2 为什么 Gameplay/UI 要引 `UnityEngine.UI`

- Gameplay 的 `DamageNumber` 用 `Text/CanvasScaler`、`WorldHealthBar` 用 `Image`；
- UI 的 `HUD`/`InventoryUI`/`SkillBarUI`/`SlotClickHandler` 用 uGUI + `EventSystems`；
- `UnityEngine.UI` 是 uGUI 的程序集名（`UnityEngine.EventSystems` 也在其中），不引会 CS0246。

### 3.3 Editor 平台隔离

- `Scripts/Editor/` 本身是 Unity 特殊目录，asmdef 只是把它显式命名；`includePlatforms:["Editor"]` 保证它只参与编辑器编译，不进 player build。
- 5 个运行时 asmdef 的 `references` 都不含 `Dungeon.Editor`，反向依赖被编译期挡住。

### 3.4 `autoReferenced` 与测试

- 30 个测试仍在预定义程序集（无 asmdef）。6 个 asmdef 全部 `autoReferenced=true`，因此 `Assembly-CSharp`/`Assembly-CSharp-Editor` 能自动引用它们，测试与 `Scripts/Editor` 无需改代码。
- `noEngineReferences=false` 保证 UnityEngine 模块可用。

## 4. 受影响模块导航

| 程序集 | 目录 | 文件数 | 备注 |
|---|---|---|---|
| `Dungeon.Core` | `Scripts/Core` | 9 | 含 M7-2 的 `ProgressionState`/`PauseState` |
| `Dungeon.Content` | `Scripts/Content` | 3 | 只引 Core |
| `Dungeon.Gameplay` | `Scripts/Gameplay/**` | 52 | 含根 `IUiModalState`；引 uGUI |
| `Dungeon.App` | `Scripts/App` | 6 | 流程/成长/适配层 |
| `Dungeon.UI` | `Scripts/UI` | 10 | 面板/HUD；引 uGUI |
| `Dungeon.Editor` | `Scripts/Editor` | 1 | `DebugTestTools`，仅 Editor |
| （测试） | `Assets/_Project/Tests` | 30 | 仍在预定义程序集，靠 `autoReferenced` |

## 5. 证据清单

| 证据 | 路径 |
|---|---|
| Gameplay | `Docs/QA/Evidence/M7-3/Gameplay/` |
| App | `Docs/QA/Evidence/M7-3/App/` |
| UI | `Docs/QA/Evidence/M7-3/UI/`（含 `play-smoke.md`） |
| Editor | `Docs/QA/Evidence/M7-3/Editor/`（含 `editor-only.txt`、`play-smoke.md`） |
| 收口 | `Docs/QA/Evidence/M7-3/收口/`（`asmdef.md`、`editor-only.txt`、`no-asset.txt`、`play-smoke.md`、`git-log.txt`、`verify.txt`） |
| 审计脚本（M7-3 副本） | `Docs/QA/Evidence/M7-3/audit_scripts.py`（`OUT` 指向 M7-3） |
| 检查器 | `Docs/QA/Evidence/M7-3/verify_m7_3.py` |

## 6. 学习讲稿

- `Docs/学习/M7-3_讲稿.md`（已完成）：6 个程序集边界、uGUI 引用原因、Editor 平台隔离、`autoReferenced` 与测试、Q&A 5 条。

## 7. 未决项

1. **M7-4 未做**：全量四套件（含 `FND00B06`/`DIAG`）、最终场景冒烟与《醒来目检清单》终审留给 M7-4。
2. **人工目检（留给所有者）**：Project 窗口选中脚本显示正确 Assembly；Play 30 秒正常；Player 平台引用列表无 `Dungeon.Editor`（本次仅用 `includePlatforms` 静态校验，未真机构建）。
3. 测试仍是预定义程序集（`Assembly-CSharp`/`Assembly-CSharp-Editor`），未建测试 asmdef（按 D8 留到 TEST-03）。
4. `Editor.log` 中 63 条历史 `error CS` 均为 M7-2 期间已修复的测试记录（最后一条行 6627，早于最后一次编译行 20088）；当前 Console 0 条。

## 8. 建议提交

| # | hash | 标题 |
|---|---|---|
| 0 | `9a0c9c65` | `文档：M7-3 工单与长任务计划（M7-3/开工）` |
| 1 | `9619f54c` | `重构：M7-3 建 Gameplay 程序集（M7-3/Gameplay）` |
| 2 | `623e9544` | `重构：M7-3 建 App 程序集（M7-3/App）` |
| 3 | `c7e24d4f` | `重构：M7-3 建 UI 程序集（M7-3/UI）` |
| 4 | `62dade15` | `重构：M7-3 建 Editor 程序集（M7-3/Editor）` |
| 5 | 见 `收口/git-log.txt` | `文档：M7-3 交付报告与讲稿（M7-3/收口）` |

- 全部**未 push**；不合并分支、不改写历史。

## 9. 消耗记录

- 墙钟：2026-11-07 02:20–03:10（约 50 分钟，含一次断网恢复）。
- 套件运行：`REFACTOR` 6 次（20/0 ×6）、`SPEC01` 6 次（12/0 ×6）；Play 冒烟 2 次（UI/Editor 各 30 秒）。
- MCP 写调用：0；`execute_menu_item` 12 次；`manage_editor` play/stop 各 2 次。
- Token/额度：执行环境未提供计数。
