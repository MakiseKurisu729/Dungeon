# REPORT — M7-1 目录与命名空间归一（批 1 中断版）

> 工单：`Docs/交接/TASK_M7-1_目录与命名空间归一.md`
> 长任务计划：`Docs/交接/长任务计划_M7-1.md`
> 状态：**部分完成（按所有者 2026-11-06 要求停在批 1 后）**。执行者不标 `Done`、不改 `当前状态.md`、不写 ADR、不 push。

## 1. 结果摘要

- 本轮只完成**批 1（Core + Content）**并已提交：`84f7555e`。
- 批 1 内容：6 个文件成对移动（`.cs`+`.cs.meta`，GUID 不变）；10 个文件加块级命名空间（`Dungeon.Core` ×7、`Dungeon.Content` ×3）；48 个引用方补 `using`（产品代码 39 + 测试 9）；`MessageFeed.RaiseChanged` 由 `internal` 改 `public`。
- 验证全绿：编译 0 `error CS`；`REFACTOR` 16/0（code=0）；`SPEC01` 12/0（code=0）；`audit_scripts.py` 的 `upward_layer_edges` 恒等于 **11**（基线 11，不增）；`verify_m7_1.py --stage 1` **9 PASS / 0 FAIL**。
- 所有者要求批 1 提交后立即停止：批 2、批 3、Core/Content asmdef、批 4 均**未开始**；Play 冒烟未跑；学习讲稿未写（等全部批次完成）。Unity 未关闭，场景未保存/未切换。

**三句话版本（亦见 §3.7）**

1. 本轮把最底层的 Core/Content 从旧目录收口到 `Scripts/Core`、`Scripts/Content`，并加上 `Dungeon.Core` / `Dungeon.Content` 命名空间，`.meta` 与 GUID 原样保留。
2. 所有引用方按编译错误与静态扫描补 `using`，产品代码除命名空间、`using` 和 `RaiseChanged` 可见性外零改动，`REFACTOR`/`SPEC01` 与基线一致。
3. 按所有者要求只做批 1 即停，后续 Gameplay/App/UI/Editor 迁移与 asmdef 留给下一次会话。

## 2. AC 回填表

| AC | 断言 ID | 结果 | 实测数据 | 证据 |
|---|---|---|---|---|
| AC-1 | `M7-1.Compile` | **PASS（批 1）** | `is_compiling=false`、`is_domain_reload_pending=false`；Console 0 条 `error CS`；Editor.log 最后 `error CS` 行 220962 早于本次编译（最后 `Finished compiling graph` 行 231601） | `Docs/QA/Evidence/M7-1/批1/compile.md`、`compile-editorlog-tail.txt` |
| AC-2 | `M7-1.REFACTOR` | **PASS（批 1）** | `pass=16 fail=0 blocked=0 infraFail=0`；`InEditorResult.txt` `code=0`（`finishedAt=2026-11-06 19:42:23.560`） | `批1/suites.md`、`Docs/QA/Evidence/REFACTOR-M4/editor_run22/` |
| AC-3 | `M7-1.SPEC01` | **PASS（批 1）** | `pass=12 fail=0 blocked=0 infraFail=0`；`code=0`（`finishedAt=2026-11-06 19:43:55.538`） | `批1/suites.md`、`Docs/QA/Evidence/SPEC-01/editor_run46/` |
| AC-4 | `M7-1.Structure.Files/.Namespace/.Tests` | **未运行（所有者要求暂停）** | 终态断言要求 77 文件落到 6 个目标目录、30 测试无 namespace；批 1 只完成 Core/Content，`verify --stage 1` 已 PASS 但不代表 AC-4 终态 | `批1/verify.txt`（stage 1，部分口径） |
| AC-5 | `M7-1.Structure.Meta` | **未运行（所有者要求暂停）** | AC-5 要求 60 个 `.cs` 全为 rename；批 1 的 6 个移动已由 `M7-1.Structure.Meta` 断言覆盖：`missing_meta=[] orphan_meta=[] guid_bad=[]`；全量 60 未完成 | `批1/verify.txt` 的 `M7-1.Structure.Meta` |
| AC-6 | `M7-1.Audit` | **PASS（批 1）** | `file_count=77`；`layer_file_counts=Core7/Content3/Gameplay50/App6/UI10/Editor1`；`upward_layer_edges=11`（与基线一致，不增） | `批1/audit/stats.json`、`coverage.txt`、`audit-output.txt` |
| AC-7 | `M7-1.Structure.Asmdef` | **未运行（所有者要求暂停）** | 程序集步骤（批 3 后建 `Dungeon.Core`/`Dungeon.Content` asmdef）未开始 | `批1/verify.txt` 中 `SKIP stage<4` |
| AC-8 | `M7-1.NoLogicChange` | **未运行（所有者要求暂停）** | 批 1 范围内已做提交后 diff 扫描：`plus=70 minus=3 offenders=0`；全量 AC-8 要求覆盖 77 文件最终态，未完成 | `批1/verify-after-commit.txt`（仅批 1） |
| AC-9 | `M7-1.Commits` | **未运行（所有者要求暂停）** | 全任务要求 6 条提交；本轮按所有者指示只提交批 1 + 交接文档（≤2 条） | `批1/git_show_stat.txt`、`git log` |
| AC-10 | `M7-1.PlaySmoke` | **未运行（所有者要求暂停）** | Play 冒烟原定批 2/批 4；所有者明确本轮“不跑 Play 冒烟”，且不进入 Play、不保存/切换场景 | — |
| AC-11 | `M7-1.RaiseChanged` | **PASS（批 1）** | `public static void RaiseChanged()` found=True；签名其余不变 | `批1/verify.txt`、`Core/MessageFeed.cs:16` |

> 未运行项原因统一为：**所有者 2026-11-06 要求“只完成批 1，提交后立即停止”，不开始批 2/3/程序集/批 4、不跑 Play 冒烟**。不是执行失败。

## 3. 实现原理（批 1）

### 3.1 命名空间包裹方式

- 采用**块级** `namespace X { … }`（Unity 2022.3 = C# 9，不能用文件级 namespace）。
- 插入点：最后一条 `using`（或顶层 `#if`）之后、类型注释之前；整段正文右移 4 空格；文件末尾补 `}`；`DebugTestTools` 这类顶层 `#if` 文件保持 `#if/#endif` 在列 0，namespace 在 `#if` 内、闭括号在 `#endif` 前。
  - `Assets/_Project/Scripts/Core/EventBus.cs:3`
  - `Assets/_Project/Scripts/Core/Team.cs:1`
  - `Assets/_Project/Scripts/Content/UnitStats.cs:3`
- 换行风格按文件原样保留：CRLF 文件仍 CRLF，原本两个 LF 文件（`DamageNumber.cs`、`UnitMotion.cs`）仍 LF。发现还原时误用 git 对象（LF）覆盖 10 个 CRLF 文件后，已在提交前恢复 CRLF 并重新 `git add`。

### 3.2 `.meta` / GUID

- 6 个移动文件使用“文件系统 `.cs` + `.cs.meta` 成对移动 + `refresh_unity`”（D16 允许的退化路径；`manage_asset(action="move")` 首次实际成功但返回失败文案）。
- 移动前后 `.meta` 字节比对一致；`verify` 的 `M7-1.Structure.Meta` 对每个移动文件比对 `guid:` 与基线，`guid_bad=[]`。

### 3.3 引用方 `using`

- 按“已包裹文件的类型 → 命名空间”静态扫描 + 编译错误双保险，共补 48 个文件：
  - `Assets/_Project/Scripts/Content/UnitStats.cs:2`（`using Dungeon.Core;`，引用 `Team`）
  - `Assets/_Project/Scripts/Combat/MeleeCombat.cs:3-4`（`using Dungeon.Content; using Dungeon.Core;`）
  - 测试 9 个文件同步补（测试仍保持全局命名空间，只补 `using`）。

### 3.4 唯一可见性改动

- `Assets/_Project/Scripts/Core/MessageFeed.cs:16`：`internal static void RaiseChanged()` → `public static void RaiseChanged()`；方法体、签名参数、返回类型均未变（D17）。

### 3.5 验证器

- `Docs/QA/Evidence/M7-1/verify_m7_1.py`：分阶段断言（`--stage 1..5`），覆盖文件集/目录计数、每文件 namespace、`.cs`↔`.meta` 与 GUID、30 个测试无 namespace、asmdef、audit、逐文件正文归一化比对（`normalize_body` 行 191）、`RaiseChanged`、提交后 `git diff -w` 扫描（排除 New folder `.meta` 与未识别重命名的误报）。
- 红 → 绿：`批1/verify-red-baseline.txt`（3 FAIL，ExitCode=1）→ `批1/verify.txt`（9 PASS，ExitCode=0）。

## 4. 受影响模块导航

| 层/目录 | 本批后内容 | 说明 |
|---|---|---|
| `Scripts/Core/`（15 个临时） | 7 个纯 Core：`EventBus` `UnitId` `MessageFeed` `RunState` `Team` `ICharacterCombat` `Cooldown`；另有 8 个待批 2/3 迁出的 App/Gameplay 文件（`AppFlow` `GameData` `GameFlow` `MetaState` `PauseManager` `RunFlow` `UnitRegistry` `WaveSpawner`） | 这 8 个**保持全局命名空间**，批 2/3 移动时再加 ns（按工单 §2.1 批 1 注） |
| `Scripts/Content/`（3） | `UnitStats` `WeaponData` `ItemData`（`Dungeon.Content`） | 新增目录；`UnitStats` 依赖 `Dungeon.Core.Team` |
| `Scripts/Units`（16） `Scripts/Combat`（14） `Scripts/Items`（2） `Scripts/Framework`（0） | 批 2 继续迁移 | 引用 Core/Content 类型处已补 `using` |
| `Scripts/Gameplay/Interaction`（3） | 未动 | 批 2 原地加 `Dungeon.Gameplay.Interaction` |
| `Scripts/UI`（10） | 未动 | 批 4 原地加 `Dungeon.UI` |
| `Assets/_Project/Tests/**`（30） | 结构未动；9 个文件补 `using` | 测试保持全局命名空间（R-7） |

## 5. 证据清单

| 证据 | 路径 |
|---|---|
| 批 1 摘要 | `Docs/QA/Evidence/M7-1/批1/Summary.md` |
| 编译 | `Docs/QA/Evidence/M7-1/批1/compile.md`、`compile-editorlog-tail.txt` |
| verify（green） | `Docs/QA/Evidence/M7-1/批1/verify.txt`、`ExitCode.txt`、`verify-after-commit.txt` |
| verify（red 基线） | `Docs/QA/Evidence/M7-1/批1/verify-red-baseline.txt`、`ExitCode-red-baseline.txt` |
| 套件 | `Docs/QA/Evidence/M7-1/批1/suites.md`、`suite-refactor/`、`suite-spec01/`；canonical：`REFACTOR-M4/editor_run22/`、`SPEC-01/editor_run46/` |
| 审计 | `Docs/QA/Evidence/M7-1/批1/audit/`；基线：`Docs/QA/Evidence/M7-1/批0/audit/` |
| git | `Docs/QA/Evidence/M7-1/批1/git_status.txt`、`git_show_stat.txt` |
| 结构检查器 | `Docs/QA/Evidence/M7-1/verify_m7_1.py` |

## 6. 学习讲稿

- **暂未写**：`Docs/学习/M7-1_讲稿.md` 等全部批次完成后再产出（所有者明确本轮不写）。
- 高风险 + feature 级结构改动，Done 前必须补；已列入未决项。

## 7. 未决项

1. **批 2 未开始**：Gameplay 47 移 + 3 原地（`Gameplay/{Units,Combat,Player,World,Items,Interaction}`）。
2. **批 3 未开始**：`Core` 6 个 App 文件 → `App/`；Core 目录最终化。
3. **程序集步骤未开始**：`Dungeon.Core.asmdef`、`Dungeon.Content.asmdef`。
4. **批 4 未开始**：UI 10 原地加 ns；`DebugTestTools` → `Editor/`；测试 using 收尾；`REFACTOR`/`SPEC01` 各连续 2 次。
5. **Play 冒烟未跑**（批 2/批 4 原定）。
6. **学习讲稿未写**（按所有者要求延后）。
7. **工作区仍有 2 个未跟踪派单文档**：`Docs/交接/TASK_M7-1_目录与命名空间归一.md`、`Docs/交接/长任务计划_M7-1.md`。二者开工前即存在、非本轮改动；因不在工单允许提交清单内，未纳入提交。除这 2 个文件外，`git status` 干净。
8. `verify_m7_1.py` 的 diff-scan 口径在批 1 提交后修正（排除新文件夹 `.meta`、容忍小文件重命名未识别），修正版在交接文档提交中。

## 8. 建议提交

| # | hash | 标题 |
|---|---|---|
| 1 | `84f7555e` | `重构：M7-1 批1 Core+Content 目录与命名空间归一（M7-1/批1）` |
| 2 | 见 `Docs/QA/Evidence/M7-1/醒来报告.md` | `文档：M7-1 批1 交接报告（M7-1/中断）` |

- 均**未 push**；下一步由所有者目检 + 决定是否继续批 2。

## 9. 消耗记录

- 墙钟：2026-11-06 19:31–19:52（约 20 分钟；含读工单、批 1 迁移、两套件、证据与交接）。
- 工具调用：MCP 只读查询数次；`manage_asset` 写调用 2 次（create_folder 1、move 1，均在本工单授权清单内）；菜单套件 2 次（REFACTOR 1、SPEC01 1）。
- Token/额度：执行环境未提供计数，无法给出精确值。
