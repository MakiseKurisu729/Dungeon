# REPORT — M7-1 目录与命名空间归一

> 工单：`Docs/交接/TASK_M7-1_目录与命名空间归一.md`
> 长任务计划：`Docs/交接/长任务计划_M7-1.md`
> 状态：**完成（M7-1 全范围：批 1 → 批 2 → 批 3 → 程序集 → 批 4 → 收口）**。执行者不标 `Done`、不改 `当前状态.md`、不写 ADR、不 push。

## 1. 结果摘要

- 77 个脚本全部落到 `Assets/_Project/Scripts/{Core,Content,Gameplay/{Units,Combat,Player,World,Items,Interaction},App,UI,Editor}`；每个脚本恰好 1 条与目录一致的 `Dungeon.*` 块级命名空间。
- 60 个移动文件全部 `.cs` + `.cs.meta` 成对移动，60 个 `.cs.meta` 为 100% rename，GUID 与基线一致（`guid_bad=[]`）；场景/预制体/Animator/`.asset` 零改动。
- 批 3 后建成 `Dungeon.Core`（`references=[]`）与 `Dungeon.Content`（`references=["Dungeon.Core"]`）两个 asmdef，`.meta` 由 Unity 生成并提交。
- 每批验收：编译 0 `error CS`；`REFACTOR` 16/0；`SPEC01` 12/0；`audit_scripts.py` 的 `upward_layer_edges` **恒等于 11**（基线 11，不增）；`verify_m7_1.py` 各 stage 全 PASS。
- 批 4 套件**连续各 2 次**一致；批 2/批 4 Play 30 秒冒烟均 0 `error CS` / 0 `Missing script` / 0 `NullReferenceException`；场景未保存、未脏。
- 本轮 5 条提交（批 2/批 3/程序集/批 4/收口），加上上一轮的批 1 与中断交接提交，共 7 条；**全部未 push**。

**三句话版本**

1. 77 个脚本按目标层完成“目录 = 命名空间”归一，60 个移动文件靠 `.cs.meta` 成对搬迁保住 GUID，Core/Content 两个 asmdef 把依赖方向钉死在编译期。
2. 每批都以编译 0 error + REFACTOR 16/0 + SPEC01 12/0 + audit `upward=11` 为门槛，批 4 套件连续 2 次、Play 冒烟 2 次均无异常，行为与基线一致。
3. 结构改动完成、无逻辑改动；M7-2 的 11 条上行边原样保留，留给下一任务按 D5–D7 消解。

## 2. AC 回填表

| AC | 断言 ID | 结果 | 实测数据 | 证据 |
|---|---|---|---|---|
| AC-1 | `M7-1.Compile` | **PASS** | 批 1/2/3/程序集/批 4 每次 `refresh_unity` 后均 `is_compiling=false`、`is_domain_reload_pending=false`、`error CS` 0 条；Editor.log 本次会话 `error CS` 0 条 | `批1/compile.md`、`批2/compile.md`、`批3/compile.md`、`程序集/compile.md`、`批4/compile.md` |
| AC-2 | `M7-1.REFACTOR` | **PASS** | 批 1 16/0；批 2 16/0；批 3 16/0；批 4 第 1 次 16/0、第 2 次 16/0；`InEditorResult.txt` 全部 `code=0` | 各批 `suites.md` + `Docs/QA/Evidence/REFACTOR-M4/editor_run22/23/24/25/26/` |
| AC-3 | `M7-1.SPEC01` | **PASS** | 批 1 12/0；批 2 12/0；批 3 12/0；批 4 第 1 次 12/0、第 2 次 12/0；`code=0` | 各批 `suites.md` + `Docs/QA/Evidence/SPEC-01/editor_run46/47/48/49/50/` |
| AC-4 | `M7-1.Structure.Files` / `.Namespace` / `.Tests` | **PASS** | 77 文件；目录计数 `Core7/Content3/Gameplay-Units17/Combat14/Player5/World9/Items2/Interaction3/App6/UI10/Editor1`；每文件 namespace = 目录映射；测试 30 文件无 namespace | `批4/verify-after-commit.txt`（9 PASS，ExitCode=0） |
| AC-5 | `M7-1.Structure.Meta` | **PASS** | 60 个 `.cs.meta` 100% rename；删除 `.cs.meta` 0、新建 `.cs.meta` 0；`missing_meta=[]`、`orphan_meta=[]`、`guid_bad=[]` | `批4/meta-check.txt`、`批4/verify-after-commit.txt` |
| AC-6 | `M7-1.Audit` | **PASS** | 每批 `file_count=77`；`layer_file_counts=Core7/Content3/Gameplay50/App6/UI10/Editor1`；`upward_layer_edges` 批 1/2/3/程序集/批 4 均为 **11** | 各批 `audit/stats.json` + `coverage.txt` |
| AC-7 | `M7-1.Structure.Asmdef` | **PASS** | `Dungeon.Core.references=[]`；`Dungeon.Content.references=["Dungeon.Core"]`；`Core→非Core` 边 0、`Content→{非Core,非Content}` 边 0；编译 0 error | `程序集/asmdef.md`、`程序集/verify.txt` |
| AC-8 | `M7-1.NoLogicChange` | **PASS** | 逐文件正文归一化比对 0 差异；提交后 `git diff -w` 扫描 `plus=3345 minus=3073 offenders=0` | `批4/verify-after-commit.txt` |
| AC-9 | `M7-1.Commits` | **PASS（口径说明）** | 5 个改造步各 1 条 + 收口 1 条 = 6 条工作提交；另有 1 条所有者要求的中断交接提交 `a85acc0a`（批 1 后暂停），合计 7 条；标题/正文全部中文且含 `为什么：`/`验证：`；`git status` 仅剩 3 个大脑-owned 文档 | `收口/git-log.txt`、`收口/git-show-stat.txt` |
| AC-10 | `M7-1.PlaySmoke` | **PASS** | 批 2 与批 4 各 30 秒：`error CS=0`、`Missing script=0`、`NullReferenceException=0`；退出后场景 `isDirty=false` | `批2/play-smoke.md`、`批4/play-smoke.md` |
| AC-11 | `M7-1.RaiseChanged` | **PASS** | `MessageFeed.RaiseChanged` 为 `public static`，签名其余不变 | `批1/verify.txt`、`Core/MessageFeed.cs:16` |

## 3. 实现原理

### 3.1 块级命名空间与换行风格

- 统一用块级 `namespace X { … }`（Unity 2022.3 = C# 9，不能文件级）。插入点为最后一条 `using`（或顶层 `#if`）之后、类型注释之前；正文整体右移 4 空格；末尾补 `}`。`Editor/DebugTestTools.cs` 保留 `#if UNITY_EDITOR` 在列 0，namespace 在 `#if` 内、闭括号在 `#endif` 前。
  - `Assets/_Project/Scripts/Core/MessageFeed.cs:1`
  - `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs:5`
  - `Assets/_Project/Scripts/UI/UIManager.cs:7`
  - `Assets/_Project/Scripts/Editor/DebugTestTools.cs:8`
- 每个文件保持原换行风格：CRLF 仍 CRLF，两个原本 LF 的 `.cs`（`DamageNumber.cs`、`UnitMotion.cs`）仍 LF。

### 3.2 `.meta` / GUID

- 60 个移动文件全部“`.cs` + `.cs.meta` 成对移动 + `refresh_unity`”（D16 允许的退化路径）。移动前后 `.meta` 字节一致，`verify` 逐一比对 `guid:` 与基线，`guid_bad=[]`。
- 60 个 `.cs.meta` 在 `git diff --summary` 中为 100% rename，0 个删除/新建配对。

### 3.3 引用方 `using`

- 按“已包裹类型 → 命名空间”静态扫描 + 编译错误双保险，四批共补约 100 个引用方文件；跨子域引用显式可见：
  - `Assets/_Project/Scripts/Gameplay/Combat/MeleeCombat.cs:3-6`（Content/Core/Units/App）
  - `Assets/_Project/Scripts/Content/UnitStats.cs:2`（Core：`Team`）
  - 测试保持全局命名空间，只补 `using`（30 个文件全部仍无 `namespace`）。

### 3.4 程序集边界

- `Assets/_Project/Scripts/Core/Dungeon.Core.asmdef:2-4`：`name=Dungeon.Core`、`rootNamespace=Dungeon.Core`、`references=[]`、`autoReferenced=true`、`noEngineReferences=false`。
- `Assets/_Project/Scripts/Content/Dungeon.Content.asmdef:2-5`：`references=["Dungeon.Core"]`。
- 编译后 `Core → 非Core` 边 0、`Content → 外部` 边 0。

### 3.5 唯一可见性改动

- `Assets/_Project/Scripts/Core/MessageFeed.cs:16`：`internal static void RaiseChanged()` → `public static void RaiseChanged()`（D17），方法体与签名其余部分不变。

## 4. 受影响模块导航

| 层/目录 | 文件数 | 命名空间 | 说明 |
|---|---|---|---|
| `Scripts/Core/` | 7 | `Dungeon.Core` | `EventBus` `UnitId` `MessageFeed` `RunState` `Team` `ICharacterCombat` `Cooldown`；asmdef `Dungeon.Core` |
| `Scripts/Content/` | 3 | `Dungeon.Content` | `UnitStats` `WeaponData` `ItemData`；asmdef 只引 Core |
| `Scripts/Gameplay/Units/` | 17 | `Dungeon.Gameplay.Units` | 含 `UnitRegistry`；AI/生命/移动/技能数据 |
| `Scripts/Gameplay/Combat/` | 14 | `Dungeon.Gameplay.Combat` | 近战/远程/伤害/技能效果/投射物 |
| `Scripts/Gameplay/Player/` | 5 | `Dungeon.Gameplay.Player` | 选择/指令/技能输入/指示器 |
| `Scripts/Gameplay/World/` | 9 | `Dungeon.Gameplay.World` | 宝箱/门/迷雾/生成/交互 + Debug 运行期相机/镜面 + `WaveSpawner` |
| `Scripts/Gameplay/Items/` | 2 | `Dungeon.Gameplay.Items` | `ChestItem` `Inventory` |
| `Scripts/Gameplay/Interaction/` | 3 | `Dungeon.Gameplay.Interaction` | 交互接口/定义/执行器（原地） |
| `Scripts/App/` | 6 | `Dungeon.App` | `AppFlow` `GameData` `GameFlow` `MetaState` `PauseManager` `RunFlow` |
| `Scripts/UI/` | 10 | `Dungeon.UI` | 面板/HUD/背包/技能栏/过场（原地） |
| `Scripts/Editor/` | 1 | `Dungeon.Editor` | `DebugTestTools`（`#if UNITY_EDITOR`，进 `Assembly-CSharp-Editor`） |
| `Assets/_Project/Tests/**` | 30 | 全局 | 只补 `using`；无 asmdef、无 namespace |

> 旧目录 `Units/Combat/Player/System/Items/Framework/Debug` 已清空（0 个 `.cs`），目录本身与其 `.meta` 仍留在磁盘上；未删除（不在工单授权文件清单；`Framework`/`Monster` 由大脑明确“不要删”）。

## 5. 证据清单

| 证据 | 路径 |
|---|---|
| 批 1 | `Docs/QA/Evidence/M7-1/批1/` |
| 批 2 | `Docs/QA/Evidence/M7-1/批2/`（含 `play-smoke.md`） |
| 批 3 | `Docs/QA/Evidence/M7-1/批3/` |
| 程序集 | `Docs/QA/Evidence/M7-1/程序集/`（含 `asmdef.md`） |
| 批 4 | `Docs/QA/Evidence/M7-1/批4/`（含 `meta-check.txt`、`verify-after-commit.txt`、`play-smoke.md`、4 份套件副本） |
| 收口 | `Docs/QA/Evidence/M7-1/收口/git-log.txt`、`git-show-stat.txt` |
| 基线审计 | `Docs/QA/Evidence/M7-1/批0/audit/` |
| 结构检查器 | `Docs/QA/Evidence/M7-1/verify_m7_1.py` |
| 套件 canonical | `Docs/QA/Evidence/REFACTOR-M4/editor_run22..26/`、`Docs/QA/Evidence/SPEC-01/editor_run46..50/` |

## 6. 学习讲稿

- `Docs/学习/M7-1_讲稿.md`（已完成）：一句话、原问题、方案对比、数据流、5 处 `文件:行`、设计取舍、边界/失败模式、答辩 Q&A 5 条。

## 7. 未决项

1. **M7-2 的 11 条上行边未动**（本任务明确不做）：`audit upward=11` 原样保留，M7-2 按 D5–D7 消解。
2. **旧目录空壳残留**：`Scripts/{Units,Combat,Player,System,Items,Framework,Debug}` 为空目录（0 个 `.cs`），未删除；`Framework`/`Monster` 由大脑明确不要删。若所有者要求视觉上的“无旧目录”，可单独授权删除空目录 + `.meta`。
3. **M7-3/M7-4 未做**：`Gameplay`/`App`/`UI`/`Editor` asmdef、全量四套件回归（`FND00B06`/`DIAG`）留给后续任务；`DIAG` 已知红仍未处理（本任务未跑）。
4. **Play 冒烟只做 30 秒**（AC-10 口径），不是长时间 soak；M7-4 再做完整场景冒烟。
5. `git diff` 对 `.cs` 的 rename 配对不可靠（全文件缩进变化导致相似度低），已用 60 个 `.cs.meta` 100% rename + `verify` 路径/GUID 断言替代证明，详见 `批4/meta-check.txt`。
6. 工作区仍有 3 个大脑-owned 文档：`TASK_M7-1…`、`长任务计划_M7-1.md`（未跟踪）、`当前状态.md`（已修改）；按所有者要求**未提交、未改**。
7. `verify_m7_1.py` 在程序集步骤修正过一次 stage 映射（`DebugTestTools` 属批 4 而非程序集步骤），修正版已随程序集提交。

## 8. 建议提交

| # | hash | 标题 |
|---|---|---|
| 0 | `483678fb` | 任务起点（回滚锚） |
| 1 | `84f7555e` | `重构：M7-1 批1 Core+Content 目录与命名空间归一（M7-1/批1）` |
| 2 | `a85acc0a` | `文档：M7-1 批1 交接报告（M7-1/中断）` |
| 3 | `c0ac2f4d` | `重构：M7-1 批2 Gameplay 目录与命名空间归一（M7-1/批2）` |
| 4 | `d2ef93e3` | `重构：M7-1 批3 App 目录与命名空间归一（M7-1/批3）` |
| 5 | `d35d43da` | `重构：M7-1 建 Core/Content 程序集（M7-1/程序集）` |
| 6 | `7116b3d0` | `重构：M7-1 批4 UI/Editor 目录与命名空间归一（M7-1/批4）` |
| 7 | 见 `收口/git-log.txt` | `文档：M7-1 交付报告与讲稿（M7-1/收口）` |

- 全部**未 push**；不合并分支、不改写历史。

## 9. 消耗记录

- 墙钟：2026-11-06 19:31–19:52（批 1）+ 2026-11-07 00:15–00:50（批 2/3/程序集/批 4/收口），合计约 55 分钟。
- 套件运行：`REFACTOR` 5 次（16/0 ×5）、`SPEC01` 5 次（12/0 ×5）；Play 冒烟 2 次（各 30 秒）。
- MCP 写调用：`manage_asset` create_folder 8 次（Content/App/Editor/Gameplay 子目录 5 个 + 复用已有）、move 1 次（误报但成功），其余移动走文件系统成对搬迁 + `refresh_unity`；`execute_menu_item` 10 次；`manage_editor` play/stop 2 次。
- Token/额度：执行环境未提供计数，无法给出精确值。
