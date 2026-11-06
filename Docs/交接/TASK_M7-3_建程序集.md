# M7-3 — 建 Gameplay / App / UI / Editor 程序集

> 本工单自包含：执行者只读本文件 + `Docs/交接/长任务计划_M7-3.md` + `Docs/交接/M7_决策结论.md`（D15、D19、D20）。
> 前置：M7-1（目录/命名空间）、M7-2（`upward=0`）已完成。

## §0 元信息

| 字段 | 值 |
|---|---|
| 任务 ID | `M7-3` |
| 风险等级 | **高风险**（程序集边界 = 编译单元；MonoBehaviour 的 GUID 引用面） |
| 授权 | 所有者 2026-11-07：M7-2 起不需要在场；允许本地自动提交（不 push） |
| 基线提交 | `164013c3`（开工复核；不一致即停） |
| 回滚锚 | `164013c3`；每个 asmdef 一条 commit，删 asmdef 即回滚 |
| 分支 | `docs/fnd-00-baseline` |
| 提交方式 | **允许自动提交**：一步一提交，中文标题 + `为什么` + `验证` |
| 状态 | `Ready` |

基线（大脑 2026-11-07 核验）：`Scripts` 81 个 `.cs`；`Dungeon.Core`/`Dungeon.Content` asmdef 已建；`upward=0`；`REFACTOR` 20/0、`SPEC01` 12/0、`FND00B06` 12/0+2obs；Unity 模式 B `Dungeon@4f5c34c82f31646e`，场景 `DungeonWorld` 非脏。

## §1 需求（Why / What）

**一句话目标**：把 `Gameplay`/`App`/`UI`/`Editor` 四个目录各自变成独立程序集，用编译期引用方向**固定** `Core ← Content ← Gameplay ← App ← UI`（Editor 旁挂、仅编辑器），且运行行为与套件结果不变。

| 编号 | 需求 | 验收 |
|---|---|---|
| R-1 | 4 个 asmdef 存在、字段与 §2.1 完全一致 | `M7-3.Asmdef` |
| R-2 | 每建一个 asmdef 后编译 0 error；`REFACTOR` 20/0、`SPEC01` 12/0 | `M7-3.Compile` / `.REFACTOR` / `.SPEC01` |
| R-3 | 运行时程序集**不**引用 `Dungeon.Editor`；`Dungeon.Editor` 只含 Editor 平台 | `M7-3.EditorOnly` |
| R-4 | 不改任何 `.cs` 逻辑；只新增 asmdef（+ `.meta`） | `M7-3.NoAsset` |
| R-5 | 进 Play 30 秒无 Missing Script / NRE / error | `M7-3.PlaySmoke` |
| R-6 | 4 个 asmdef 各 1 条提交 + 1 条收口文档提交 | `M7-3.Commits` |

**不在范围**：M7-4 全量回归与讲稿收尾（本单只到 M7-3 收口）；拆 `Dungeon.Gameplay` 子程序集；改代码结构；改场景/预制体/`.asset`；Unity 6.3；M6/M5。

**约束**：`autoReferenced` 必须为 `true`（`Assembly-CSharp` 里的 30 个测试文件靠它引用）；`noEngineReferences=false`；`rootNamespace` 与程序集同名；`includePlatforms=[]`（Editor 除外）。

## §2 方案（Plan）

**备选方案**

| 方案 | 做法 | 优点 | 缺点 | 结论 |
|---|---|---|---|---|
| A | 一次性建 4 个 asmdef | 提交少 | 一次编译红面覆盖全仓，难定位 | 弃用 |
| B | 逐个建：Gameplay → App → UI → Editor，每个都跑套件 | 每步可编译可回滚；循环引用会在对应步暴露 | 4 次编译 + 4 次套件 | **选定**（D15） |
| C | 先建 UI/App 再建 Gameplay | — | 下层未固化，上层引用会反复改 | 弃用 |

**数据流**：`asmdef（目录）→ 编译器切程序集 → 引用方向由 references 强制`。运行时无变化（无代码改动）。

### 2.1 asmdef 精确内容

顺序：**Gameplay → App → UI → Editor**。

`Assets/_Project/Scripts/Gameplay/Dungeon.Gameplay.asmdef`：

```json
{
    "name": "Dungeon.Gameplay",
    "rootNamespace": "Dungeon.Gameplay",
    "references": ["Dungeon.Core", "Dungeon.Content", "UnityEngine.UI"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

`Assets/_Project/Scripts/App/Dungeon.App.asmdef`：`references = ["Dungeon.Core","Dungeon.Content","Dungeon.Gameplay"]`，其余同上。

`Assets/_Project/Scripts/UI/Dungeon.UI.asmdef`：`references = ["Dungeon.Core","Dungeon.Content","Dungeon.Gameplay","Dungeon.App","UnityEngine.UI"]`，其余同上。

`Assets/_Project/Scripts/Editor/Dungeon.Editor.asmdef`：`references = ["Dungeon.Core","Dungeon.Content","Dungeon.Gameplay","Dungeon.App","Dungeon.UI"]`、`includePlatforms = ["Editor"]`，其余同上。

> `UnityEngine.UI` 是 uGUI 程序集名（含 `UnityEngine.EventSystems`）：`DamageNumber`/`WorldHealthBar`（Gameplay）与 `HUD`/`InventoryUI`/`SkillBarUI`/`SlotClickHandler`（UI）需要。
> `Dungeon.Editor` 的 `includePlatforms:["Editor"]` 保证不进 player build；`Scripts/Editor/` 已是 Unity 特殊目录，asmdef 只是把它显式命名。

**实施顺序**

1. 复核基线；提交待入库文档（`当前状态.md` + 本 TASK + 计划）→ `文档：M7-3 工单与长任务计划（M7-3/开工）`。
2. 建 `Dungeon.Gameplay.asmdef` → `refresh_unity` → 编译 0 error → `REFACTOR` 20/0 → `SPEC01` 12/0 → 审计 upward=0 → 证据 → 提交。
3. 建 `Dungeon.App.asmdef` → 同上 → 提交。
4. 建 `Dungeon.UI.asmdef` → 同上 + Play 30 秒冒烟 → 提交。
5. 建 `Dungeon.Editor.asmdef` → 编译 0 error → 编辑器平台校验 → `REFACTOR` 20/0 → 提交。
6. 收口：REPORT + 讲稿 + 醒来报告/目检清单 → 提交。

**风险与回滚**

| 风险 | 信号 | 应对 |
|---|---|---|
| 缺引用（uGUI/EventSystems） | CS0246/CS0234 | 只补 `references`，不改代码；仍红→停 |
| 循环引用 | CS0234/编辑器报 circular | 回退该 asmdef，停并报告（说明 M7-2 有漏边） |
| 场景 Missing Script | Play 冒烟 error | 不该发生（GUID 不变）；出现即 revert 该 asmdef 并停 |
| 测试编译失败 | `Tests/**` CS0246 | 确认 `autoReferenced=true`；仍红→停 |
| Editor 进 player build | asmdef 字段错 | 修正 `includePlatforms` |

## §3 验证（Validation）

| AC | Given / When / Then | 断言 ID | 证据 |
|---|---|---|---|
| AC-1 | 4 个 asmdef 建成后，字段与 §2.1 完全一致（`name`/`rootNamespace`/`references`/`includePlatforms`/`autoReferenced`） | `M7-3.Asmdef` | `Docs/QA/Evidence/M7-3/收口/asmdef.md` + `verify_m7_3.py` |
| AC-2 | 每建一个 asmdef 后，`is_compiling=false` 且 Console 0 条 `error CS` | `M7-3.Compile` | 各步 `compile.md` |
| AC-3 | 每步 `REFACTOR` 20/0、`SPEC01` 12/0；收口各连续 2 次 | `M7-3.REFACTOR` / `.SPEC01` | 各步 `suites.md` |
| AC-4 | 运行时程序集（Core/Content/Gameplay/App/UI）都不引用 `Dungeon.Editor`；`Dungeon.Editor.includePlatforms == ["Editor"]` | `M7-3.EditorOnly` | `收口/editor-only.txt` |
| AC-5 | 进 Play 30 秒无 `Missing script`/`NullReferenceException`/`error CS`，退出后场景非脏 | `M7-3.PlaySmoke` | `收口/play-smoke.md` |
| AC-6 | `git diff 164013c3..HEAD -- Assets` 只含 4 个 `.asmdef` + 4 个 `.asmdef.meta`；无 `.cs` 改动 | `M7-3.NoAsset` | `收口/no-asset.txt` |
| AC-7 | 4 个 asmdef 提交 + 1 条收口提交；中文标题 + `为什么`/`验证`；`git status` 干净；未 push | `M7-3.Commits` | `收口/git-log.txt` |

**运行次数**：每步 `REFACTOR`/`SPEC01` 各 ≥1；收口各连续 2 次。

**检查器** `Docs/QA/Evidence/M7-3/verify_m7_3.py`：

| 断言 ID | 内容 |
|---|---|
| `M7-3.Asmdef` | 6 个 asmdef（Core/Content/Gameplay/App/UI/Editor）存在；逐字段比对 §2.1 |
| `M7-3.EditorOnly` | `Dungeon.Editor.includePlatforms == ["Editor"]`；其余 5 个 `includePlatforms == []`；运行时 asmdef 的 `references` 不含 `Dungeon.Editor` |
| `M7-3.NoAsset` | `git diff --name-only 164013c3..HEAD -- Assets` 只匹配 `Scripts/**/*.asmdef(.meta)` |
| `M7-3.Files` | `Scripts/**/*.cs` 仍 81；`Tests/**/*.cs` 仍 30 |

**人工目检（≤3，留给所有者）**：① Project 窗口选中脚本显示正确 Assembly（Core/Content/Gameplay/App/UI/Editor）；② Play 30 秒正常；③ 打包设置里 `Dungeon.Editor` 不出现在 player 平台（或 Owner 后续真机构建确认）。

## §4 执行信息

| 项 | 内容 |
|---|---|
| 前置 | HEAD `164013c3`、工作区干净；模式 B；场景非脏；`py -3.12` |
| 允许改动 | 4 个新 `.asmdef` + 4 个 `.asmdef.meta`；`Docs/QA/Evidence/M7-3/**`；`REPORT_M7-3`；`Docs/学习/M7-3_讲稿.md` |
| 绝不触碰 | `Assets/**/*.cs`（本单不改代码）、场景、预制体、Animator、`.asset`、`ProjectSettings`、`Packages`、`Docs/01`、`当前状态.md` |
| MCP 授权 | 只读 `editor_state`/`read_console`；`refresh_unity`；`execute_menu_item`（REFACTOR/SPEC01）；`manage_editor` play/stop（仅 30 秒冒烟）；`manage_asset(action="get_info"/"search")`。**禁止** `execute_code`/`manage_scene` 写/任何资产写 |
| 提交策略 | 允许自动提交：文档 1 + asmdef 4 + 收口 1 = 6 条；不 push |
| 熔断 | 见计划 §6；额外：asmdef 循环引用 2 轮内无法解决 → 回退该 asmdef 并停 |

## §5 学习产出

| 项 | 要求 |
|---|---|
| 讲稿 | **必须**：`Docs/学习/M7-3_讲稿.md`（6 个程序集边界、为什么 Gameplay 要引 uGUI、Editor 平台隔离、autoReferenced 与测试的关系、答辩 Q&A 5 条） |
| 三句话版本 | REPORT §3.7 |

## 自包含声明

本工单不依赖 `当前状态.md` 或其他任务书即可执行。信息不足时在 REPORT 写“信息不足”并列缺项，不自行取舍。
