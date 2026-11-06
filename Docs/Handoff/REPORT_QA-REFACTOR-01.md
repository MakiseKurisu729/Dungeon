# REPORT — QA-REFACTOR-01：测试代码分层重构（Qa 公共层 + 四套件迁移，不改断言）

> 对应 `Docs/Handoff/TASK_QA-REFACTOR-01.md`。节次与 `Docs/Handoff/workflow-rules.md` §2 一致，**不得删节**。
> **本文件当前只覆盖「阶段 A」**（五阶段串行，每阶段一个执行者会话）。阶段 B/C/D/E 一律写“阶段 X 待做”。

## 0. 开工前六问（TASK §4 强制）

1. **唯一主要结果**：建立 `Tests/QA/Runtime/` 公共层（`QaJson` / `QaSink`(`IQaSink`) / `QaEvidence`+`QaEvidenceSpec` / `QaWait` / `QaFixtures` / `QaScene`），把 `SkillCastFixture.cs`（连 `.meta`、GUID 不变）移入，`SPEC01_Json` 全仓改名 `QaJson`，SPEC01 改成「薄适配器 + 复用 Qa 层」，并让 **1 条示例断言**改成 `AssertXxx(IQaSink sink, …)`；**SPEC01 仍然 12 pass / 0 fail，证据文件名/键序/字段序逐字符不变**。
2. **明确不做什么**：不做阶段 B（REFACTOR）、C（FND00B06）、D（DIAG）、E（统一 Boot/Launcher、QaTestTemplate、Docs/06、Docs/01 §14.1）；不改任何断言的 ID/期望/阈值/通过条件/`qaItem`/`status`；不新增/删除/合并断言；不改产品代码 `Scripts/**`、场景、预制体、Animator、`.asset`、`ProjectSettings`、`Packages`；不改 `QA_InEditorMenu` 的菜单文本/子目录规则/`runInBackground` 还原；不动 `Tests/FND00B/FND00B04*|FND00B05*`、`Tests/FND02A/**`；不做任何 git 写操作。
3. **允许改哪些文件**：`Assets/_Project/Tests/**`（新增 6 个 `.cs`、移动 1 个 `.cs`+`.meta`、修改 4 个 `.cs`）+ `Docs/Handoff/REPORT_QA-REFACTOR-01.md` + `Docs/QA/Evidence/QA-REFACTOR-01/**`。实际改动见 §4 清单（与 TASK §2.6 逐项一致）。
4. **必须保持哪些旧行为**：证据目录与文件名；Summary 键名与键序（含 SPEC01 重复的 `mode` 键，禁止修）；断言行字段与字段序；UTF-8 无 BOM；`ExitCode` 内容；`SessionState` 键（`SPEC01.*`、`QA.InEditor.*`）；菜单项文本；`*_Runner` GameObject 名；命令行入口名 `SPEC01_BatchAcceptance.Run`；以及全部断言口径。
5. **如何证明完成**：编译绿（`error CS` = 0）；SPEC01 菜单跑 1 次得 12 行 / 12 pass / 0 fail；`(qaItem, assertion, status)` 排序后与 `before` 逐行一致（`DIFF` 为空）；文件名集合、Summary 键序（22 键含重复 `mode`）、断言行字段序、BOM 状态逐项比对；`IQaSink` 静态检查。证据：`Docs/QA/Evidence/QA-REFACTOR-01/`。
6. **失败后在哪里停**：命中 TASK §4 熔断（断言 ID/条数/`status`/阈值变化；需改 `Scripts/**` 或资产；编译连续 2 轮红；证据文件名/字段无法用 `QaEvidenceSpec` 表达；断言体内直接写盘或依赖引导且未声明）→ 立即停、保留现场（不清理半成品、不回退用户文件），在 REPORT 与回执写清现象与复现步骤。

## 1. 结果摘要

`部分完成`（= 阶段 A 完成；本单为五阶段串行，B/C/D/E 尚未执行）。

一句话：Qa 公共层已建立且 SPEC01 已迁移到它上面，编译绿、SPEC01 12/12、断言三重集与证据形状与迁移前逐字符一致；`IQaSink` 已定义、`QaEvidence` 已实现、1 条示例断言（`UI.SpawnAnchor`）已改新签名。

## 2. AC 回填表

> 只回填阶段 A 覆盖到的 AC 行；其余行写“未运行”+ 原因，不预填结论。

| AC | 结果 | 断言 ID | 关键数据 | 日志路径 |
|---|---|---|---|---|
| **AC-1**（仅 SPEC01 部分） | **通过** | 12 条（见下） | before 12 行 / pass=12 fail=0；after 12 行 / pass=12 fail=0；`(qaItem, assertion, status)` 排序后 **DIFF 为空** | `Docs/QA/Evidence/QA-REFACTOR-01/SPEC01/diff_AssertionIds.txt` |
| **AC-3**（仅 SPEC01 部分） | **通过** | — | 文件名集合 5=5 一致；Summary **22 键逐键同序**（含重复 `mode`）；断言行首行 69 个 `"key"` 出现次序一致；5 个文件全部 `BOM=False`；`ExitCode` 内容 `0=0` | `Docs/QA/Evidence/QA-REFACTOR-01/SPEC01/diff_EvidenceShape.txt` |
| **AC-4**（仅阶段 A 部分） | **通过（部分）** | — | `Assets/_Project/Scripts/**` 零改动；代码改动全在 `Assets/_Project/Tests/**`；Docs 侧只有本 REPORT 与 `Docs/QA/Evidence/QA-REFACTOR-01/**`（+ TASK 本体，非我所改）；`Assembly-CSharp*.csproj` 脏但未提交 | `Docs/QA/Evidence/QA-REFACTOR-01/git_scope_phaseA.txt` |
| **AC-8**（仅阶段 A 部分） | **通过（部分）** | — | `error CS` = 0（两次刷新后均为 0）；`Tests/QA/**` 无 `static` 可变字段（`QaJson`/`QaWait`/`QaFixtures`/`QaScene` 全为 static 纯函数，`QaEvidence`/`QaEvidenceSpec` 为实例对象） | `Docs/QA/Evidence/QA-REFACTOR-01/compile_phaseA.txt` |
| **AC-10**（仅阶段 A 部分） | **通过** | `UI.SpawnAnchor`（示例断言沿用原 ID，未改） | `interface IQaSink` = 1；`QaSink.cs` 违禁 token = 0；成员恰为 `Record`/`BlockedAssertion`/`InfraFailure`/`EvidenceDir`；`class QaEvidence : IQaSink` = 1；`Assert\w+\(IQaSink sink` = 1（SPEC01）；四套件 `Assert*` 体内 `File.\|Directory.\|SessionState\|EditorApplication\|QaLauncher\|_Boot` = 0 | `Docs/QA/Evidence/QA-REFACTOR-01/utp_readiness.txt` |
| AC-2 | 未运行 | — | 阶段 E 才要求四套件各连续 2 次 | — |
| AC-5 | 未运行 | — | `Tests/QaTestTemplate.md` 属阶段 E | — |
| AC-6 | 未运行 | — | 阶段 E 才做去重计数（当前 `class PlayerProbe` = 2、`class HitCollector` = 2，REFACTOR 本地实现待阶段 B 迁移） | — |
| AC-7 | 未运行 | — | `QaLauncher`/L2 域重载属阶段 E | — |
| AC-9 | 未运行 | — | `Docs/06_测试架构与验收分层.md` 属阶段 E | — |

- **运行次数**：基线 4 次（SPEC01 / REFACTOR / FND00B06 / DIAG 各 1）+ 迁移后 SPEC01 回归 2 次 = **6 次 ≤ 20**。两次 SPEC01 回归（`editor_run41` 中间快照、`editor_run42` 最终代码）均为 12 pass / 0 fail；`after/` 用最终代码的 `editor_run42`。
- 基线各套件数字：SPEC01 `12 pass / 0 fail`（12 行）；REFACTOR `16 / 0`（16 行）；FND00B06 `12 / 0 + observed 2`（14 行）；DIAG `6 / 1`（7 行，唯一红 `Diag.Input.FirstExit`，`code=2`）。

## 3. 实现原理（面向所有者）

1. **根因 / 目标**：`Tests/` 里「证据写入 / JSON 序列化 / 夹具 / 等待 / 场景查询」各重复 2–7 份，且 `DIAG → SPEC01_Json`、`DIAG/REFACTOR → SPEC01 目录下的 SkillCastFixture` 让套件互相耦合。本阶段目标是先建**唯一共享层**并让 SPEC01 成为第一个消费者，且**证据字节级不变**（否则 AC-1/AC-3 无法机检）。
2. **备选方案与为什么没选**：不做「一次性重写四套件」（12k 行同时变、断言口径无法证伪、asmdef 引不到 `Assembly-CSharp`）；也不做「只抽 Evidence+Json」（`PlayerProbe`/`WaitState`/`CreateTarget` 仍分散、跨套件依赖不消失）。选**分层 + 五阶段串行**，每阶段用「断言三重集比对 + 证据形状比对」做机检门。
3. **实际数据流**：`Tools/QA/In-Editor/*` 菜单 → `QA_InEditorMenu`（注入 `QA_EVIDENCE_SUBDIR=editor_runN`）→ `SPEC01_BatchAcceptance.Run()`（`QaScene.GetHead()` / 打开场景 / `QaScene.GetEnemySourceGuid`）→ `SPEC01_Boot` 进 Play → `SPEC01_ProbeRunner`（`QaFixtures` 找玩家/造夹具、`QaWait` 等待）→ 断言 → `SPEC01_Evidence`（= `QaEvidence` 薄适配器，经 `QaJson` 序列化落盘）→ `SPEC01_Boot.ExitWithCode` → `QA_InEditorMenu.TryFinish`。
4. **关键代码位置（5 条）**：
   - `Assets/_Project/Tests/QA/Runtime/QaSink.cs:7` —— `interface IQaSink`：断言唯一证据出口（`Record`/`BlockedAssertion`/`InfraFailure`/`EvidenceDir`），零落盘零引导。
   - `Assets/_Project/Tests/QA/Runtime/QaEvidence.cs:116` —— 证据引擎构造函数：目录/子目录、`{tag}`/`{run}`/`{prefix}` 模板解析、`ShouldTruncateAssertions` 策略；`:155` `Record`（9 参，计分口径不变）；`:219` `WriteSummary`（按 `SummaryFields` 键序写，含 `Extra` 锚点，复现 SPEC01 重复 `mode`）。
   - `Assets/_Project/Tests/QA/Runtime/QaFixtures.cs:24` `PlayerProbe`（SPEC01 字段 + REFACTOR 的 `skills`/`rescue` 超集）、`:61` `HitCollector(int maxEvents=500, bool includeElement=true)`（等价参数化）、`:114` `FindPlayers()`、`:169` `CreateTarget(name,pos,showDamageNumber=true)`。
   - `Assets/_Project/Tests/SPEC01/Runtime/SPEC01_Evidence.cs:10` —— 32 行适配器：`class SPEC01_Evidence : QaEvidence`，构造函数只组装 `QaEvidenceSpec`（red/green × tag 的文件名模板 + 截断策略）。
   - `Assets/_Project/Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs:1021` —— 示例断言 `IEnumerator AssertSpawnAnchor(IQaSink sink)`（体内 3 处 `_ev.` → `sink.`，判定表达式一字未改）；调用点 `:150` `RunSafe("UI.SpawnAnchor", AssertSpawnAnchor(_ev))`。
5. **边界 / 失败模式**：① 文件名/键序写错 → AC-3 形状比对立刻暴露（本次 5 文件 + 22 键 + 69 个字段出现次序全一致）；② 去重时改了行为（如 `CreateTarget` 的 `showDamageNumber` 默认值、`HitCollector` 的 500/200 上限、`WaitState` 的超时口径）→ 对应断言转红，本阶段用「参数化而非统一」规避；③ 移动 `SkillCastFixture.cs` 丢 `.meta` → DIAG/SPEC01 编译红，本次 `.meta` 同步移动、GUID `4409a5b02ede476d82d3489a9689a8bb` 不变，`Skill.CastSmoke` 仍 pass。
6. **与计划的差异及原因**：
   - `QaScene.GetHead(string sessionKey = null)` / `GetEnemySourceGuid(string scenePath, string sessionKey = null)`：TASK 只写 `GetHead()` 走 `git rev-parse HEAD`，但 SPEC01 断言层原来读的是**启动期缓存**（`SessionState["SPEC01.Head"]`）。若直接把断言层改成现场起 `git` 进程，属于「改了行为」（Play 中起进程、值取收口时刻而非启动时刻）。故保留两种来源在同一个方法上，断言层传 `sessionKey`，`BatchAcceptance` 走真 `git`——行为逐字节等价，且 `GetHead()` 定义数仍为 1（满足 AC-6）。
   - `QaEvidenceSpec` 用**文件名模板 + Summary 键序枚举**（`QaSummaryKey[]`）而不是硬编码四套件：四套套件的 Summary 键序本就不同（§2.4），只有数据驱动才能既「不硬编码套件名」又「逐字符保留」。
   - `QaJson` 额外承载 `Dict(params object[])` / `V3(Vector3)`（原为各 runner 私有）：两者是证据 `data` 字形的唯一构造方式、四处实现字节相同，属可证明等价合并，并为 AC-6「`Dict(` 定义 = 1」铺路。
   - `QaFixtures` 额外承载 `StateName`/`StateNorm`/`DamageElementName`：`HitCollector` 迁出后必须能算命中瞬间状态名；三者与 SPEC01 原实现逐分支相同（含 `"Other:"+shortNameHash`、`>1` 钳到 1、`<missing>`）。
7. **三句话版本**：
   - 证据/夹具/等待/场景查询现在只有一份实现（`Tests/QA/Runtime`），SPEC01 只剩断言与编排。
   - 迁移不靠“看起来一样”，靠机检：断言三重集排序逐行相同、Summary 22 键同序（含那个重复的 `mode`）、断言行字段序与 BOM 状态全部一致。
   - 断言与落盘已经解耦：断言只认 `IQaSink`，所以 M7 换成 `[UnityTest]` 时改的是实现方，不是断言。

## 4. 受影响模块导航

| 模块 | 职责 | 入口（类 / 方法 / 场景对象） |
|---|---|---|
| `Tests/QA/Runtime/QaSink.cs` | 断言唯一证据出口（新） | `IQaSink.{Record,BlockedAssertion,InfraFailure,EvidenceDir}` |
| `Tests/QA/Runtime/QaEvidence.cs` | 证据落盘 + 计分 + Summary + Runs 索引 + 套件契约（新） | `QaEvidence`, `QaEvidenceSpec`, `QaSummaryKey` |
| `Tests/QA/Runtime/QaJson.cs` | 极简 JSON 序列化 + `Dict`/`V3`（由 `SPEC01_Json` 迁移） | `QaJson.Value/Dict/V3` |
| `Tests/QA/Runtime/QaFixtures.cs` | 玩家探针 / 命中收集器 / 内存目标 / 选中 / 找玩家 / 状态名（新） | `QaFixtures.*` |
| `Tests/QA/Runtime/QaWait.cs` | 协程等待（新） | `QaWait.WaitState/WaitAllStates/WaitUntil/WaitSeconds/WaitFrames/WaitNoNumbers` |
| `Tests/QA/Runtime/QaScene.cs` | 版本号 / 敌人来源 GUID / 绝对路径 / 场景常量（新） | `QaScene.GetHead/GetEnemySourceGuid/AbsPath/ScenePath/EnemyPrefabGuid` |
| `Tests/QA/Runtime/SkillCastFixture.cs` | 施法夹具（移动，类名/GUID/行为不变） | `SkillCastFixture.Create/Prepare/Cast` |
| `Tests/SPEC01/Runtime/SPEC01_Evidence.cs` | SPEC01 证据契约适配器（薄） | `SPEC01_Evidence(int run, string mode)` |
| `Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs` | SPEC01 断言与编排 | `AssertSpawnAnchor(IQaSink)` + 其余 9 个 `Assert*` |
| `Tests/SPEC01/Editor/SPEC01_BatchAcceptance.cs` | 启动器（`*_Boot` 保留到阶段 E） | `Run()`, `SPEC01_Boot` |
| `Tests/DIAG/Runtime/DIAG_Evidence.cs` | DIAG 证据（仅类名替换） | `DIAG_Evidence`（用 `QaJson`） |

数据流：`菜单 → QA_InEditorMenu（editor_runN）→ *_BatchAcceptance.Run（QaScene）→ *_Boot → *_ProbeRunner（QaFixtures/QaWait）→ 断言（IQaSink）→ *_Evidence(QaEvidence) + QaJson → .jsonl/.json/.txt`

**改动文件清单**（与 TASK §4 允许改动逐项比对）

| 文件 | 类型 | 行数（前 → 后） | 改了什么 |
|---|---|---|---|
| `Tests/QA/Runtime/QaSink.cs` | 新增 | — → 21 | `interface IQaSink`（AC-10） |
| `Tests/QA/Runtime/QaJson.cs` | 新增 | — → 108 | `SPEC01_Json` 原样迁移 + `Dict`/`V3` |
| `Tests/QA/Runtime/QaEvidence.cs` | 新增 | — → 363 | 证据引擎 + `QaEvidenceSpec` + Summary 键序枚举；实现 `IQaSink` |
| `Tests/QA/Runtime/QaWait.cs` | 新增 | — → 78 | 协程等待 |
| `Tests/QA/Runtime/QaFixtures.cs` | 新增 | — → 235 | 探针/收集器/夹具/状态名 |
| `Tests/QA/Runtime/QaScene.cs` | 新增 | — → 94 | git HEAD / enemy GUID / AbsPath / 常量 |
| `Tests/SPEC01/Runtime/SkillCastFixture.cs` | **移动**（+`.meta`） | 413 → 413 | 位置 `SPEC01/Runtime/` → `QA/Runtime/`；类名/GUID 不变 |
| `Tests/SPEC01/Runtime/SPEC01_Evidence.cs` | 修改 | 299 → 32（−267） | 变 32 行适配器，`SPEC01_Json` 类随之外迁删除 |
| `Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs` | 修改 | 2153 → 1921（−232） | 删本地 `PlayerProbe`/`HitCollector`/`FindPlayer(s)`/`Select`/`CreateTarget` 实现/`WaitState`/`WaitAllStates`/`WaitNoNumbers`/`StateName`/`StateNorm`/`DamageElementName`/`GetHead`/`GetEnemySourceGuid`/`Dict`/`V3`；`ScenePath`/`EnemyPrefabGuid` 指向 `QaScene`；1 条断言改 `IQaSink` 签名 |
| `Tests/SPEC01/Editor/SPEC01_BatchAcceptance.cs` | 修改 | 279 → 226（−53） | `GitHead`/`AbsPath`/`FindEnemySourceGuid` 三处转 `QaScene`；常量指向 `QaScene` |
| `Tests/DIAG/Runtime/DIAG_Evidence.cs` | 修改 | 135 → 135 | 仅 `SPEC01_Json` → `QaJson` 机械替换（18/18 行，零逻辑改动） |

未改动（符合预期）：`Scripts/**`、全部资产/场景/预制体/Animator/`ProjectSettings`/`Packages`、`REFACTOR/**`、`FND00B/FND00B06*`、`FND00B04/05`、`FND02A`、`QA_InEditorMenu.cs`、`CURRENT.md`、其他 TASK/REPORT、既有 `Docs/QA/Evidence/**`（只在其中**新增**了 `editor_run40/41/42`、`editor_run17`(REFACTOR-M4)、`editor_run5`(FND-00B-06/DIAG-SKILL) 五个由菜单自动创建的子目录）。

## 5. 证据清单

| 文件 | 内容 |
|---|---|
| `Docs/QA/Evidence/QA-REFACTOR-01/SPEC01/before/` | 迁移前快照（源：`SPEC-01/editor_run40`，5 文件） |
| `Docs/QA/Evidence/QA-REFACTOR-01/SPEC01/after/` | 迁移后快照（源：`SPEC-01/editor_run42`，最终代码，5 文件） |
| `Docs/QA/Evidence/QA-REFACTOR-01/SPEC01/after_editor_run41_pre_IQaSink/` | AC-10 变更前的中间快照（保留，未删） |
| `Docs/QA/Evidence/QA-REFACTOR-01/SPEC01/diff_AssertionIds.txt` | BEFORE / AFTER / DIFF 三段，`DIFF = (empty)`，`RESULT: IDENTICAL` |
| `Docs/QA/Evidence/QA-REFACTOR-01/SPEC01/diff_EvidenceShape.txt` | 文件名集合、Summary 22 键序、断言行字段序、Runs 索引字段序、BOM/CRLF、ExitCode 内容 |
| `Docs/QA/Evidence/QA-REFACTOR-01/SPEC01/compare.ps1`、`shape.ps1` | 上述两份 diff 的生成脚本（可重跑） |
| `Docs/QA/Evidence/QA-REFACTOR-01/{REFACTOR,FND00B06,DIAG}/before/` | 另三套件基线快照（源：`REFACTOR-M4/editor_run17`、`FND-00B-06/editor_run5`、`DIAG-SKILL/editor_run5`） |
| `Docs/QA/Evidence/QA-REFACTOR-01/utp_readiness.txt` + `.ps1` | AC-10 静态检查（命令 → 实际计数 → 期望 → PASS/N/A） |
| `Docs/QA/Evidence/QA-REFACTOR-01/compile_phaseA.txt` | 两次 `refresh_unity` + `read_console` 原始结果，`error CS` = 0 |
| `Docs/QA/Evidence/QA-REFACTOR-01/git_scope_phaseA.txt` | `git status --porcelain`、`git diff --numstat -- Assets/_Project/Tests`、`Scripts/**` = none |
| `Docs/QA/Evidence/QA-REFACTOR-01/editor_state_phaseA.txt` | 开工/收工 `editor_state` 快照 + 6 次运行的子目录名与计数 |

## 6. 学习讲稿

阶段 E 由【大脑】撰写 `Docs/Learning/QA-REFACTOR-01_讲稿.md`。本阶段不写（阶段 A 不适用）。

## 7. 未决项

1. **未合并项及原因**（去重纪律要求的“单列”）
   - `SkillCastFixture.CreateTarget(Vector3)` 未并入 `QaFixtures.CreateTarget(name,pos,showDamageNumber)`：前者 `maxHp=1000`、固定 `showDamageNumber=false`、并把结果存进自己的 `TargetGo`/`Target`/`TargetStats` 字段参与 `Dispose()`；后者 `maxHp=100000`、按参数、无宿主字段。**不同边界，不可证明等价**，故留在 `SkillCastFixture` 内（该文件只是被移动）。
   - `SPEC01_ProbeRunner.GetHead()`（读 `SessionState` 缓存）与 `SPEC01_BatchAcceptance.GitHead()`（真 `git` 进程）未合并成“一个语义”：两者数据来源与时刻不同。现以 `QaScene.GetHead(sessionKey)` 参数化承载，默认（`sessionKey=null`）保留 `git` 语义，断言层传 key 保留缓存语义（见 §3.6）。**若大脑认为应二选一，请在阶段 B 前裁决**，我不自行取舍。
   - `REFACTOR_Evidence` 的断言行有 `milestone` 字段（`run, milestone, qaItem, …`），当前 `QaEvidence` 的断言行字段序按 §2.4 固定为 `run, qaItem, …`。**这是 TASK §2.4 与实际文件的矛盾**：阶段 B 若要保 AC-3（字段序一致），需要给 `QaEvidence` 增加断言行的可配置字段序（纯增量）。本阶段未动，避免替阶段 B 决定。
2. **FND00B06 的断言不在 `Assert*` 方法里**（`FND00B06_ProbeRunner` 只有 `Start`/`SampleInitialIdle`/`GroupAttack`/`GroupAnimation`/`*Fixture`），所以 AC-10 的第 5 项对它 N/A。阶段 C 必须先拆出/命名 `Assert*` 方法，才能满足“首参 `IQaSink`”。
3. **TASK §2.4 与现状不一致（信息不足，未自行取舍）**：§2.4 写 FND00B06「ExitCode **不生成**」，但基线 `editor_run5` 实际生成了 `06_ExitCode_run1.txt`。`QaEvidenceSpec` 已支持 `ExitCodeFileName = null`（不生成），阶段 C 可二选一；请大脑裁决“保现状 or 按 §2.4 停用”。
4. **本阶段只有 1 条断言走 `IQaSink`**（TASK 允许阶段 A 只做 1 条）：其余 11 条 SPEC01 断言行仍经 runner 字段 `_ev` 出证据——见 §10。
5. 目检项（≤3，供所有者）：① 打开 `Docs/QA/Evidence/QA-REFACTOR-01/SPEC01/diff_EvidenceShape.txt` 确认“Summary 键序含两个 `mode`”；② 在 Unity Project 窗口确认 `Tests/QA/Runtime/` 下 6 个新脚本与 `SkillCastFixture.cs` 的 `.meta` 均存在、无 `Missing`；③ 目视 `git status` 里没有 `Scripts/**` 与资产改动。
6. Canonical 证据目录新增了 5 个 `editor_runN` 子目录（菜单自动递增的产物，属“只追加”）；阶段 E 的 AC-4 描述里“Docs 侧只有 …”需要把这批新增目录算进去。

## 8. 建议提交

```text
test(qa): 建立 Qa 公共层并迁移 SPEC01（阶段 A，断言零变化）

为什么：证据/夹具/等待/场景查询各重复 2–7 份且跨套件耦合，先把唯一共享层立起来并让 SPEC01 成为第一个消费者，用“断言三重集 + 证据形状”机检证明迁移没动断言口径。
```

（由所有者用 GitHub Desktop 提交；本执行者未做任何 git 写操作。）

## 9. 消耗记录

- 钱包：`Go 订阅`
- 模型完整 ID：`opencode-go/deepseek-v4.1-flash`

## 10. 仍直写文件 / 仍依赖引导的断言（AC-10 ⑤，不得缺节）

**结论：直写文件的断言 = 无；依赖引导状态（`SessionState`/`EditorApplication`/`*_Boot`/`QaLauncher`）的断言 = 无**——四套件所有 `Assert*` 方法体内这组 token 的计数为 0（逐方法花括号扫描，见 `utp_readiness.txt` 第 5 项）。它们的问题不是“直写/依赖引导”，而是**尚未改签名**：仍通过 runner 字段 `_ev` 出证据（`_ev` 是 `QaEvidence` 实例，写盘发生在实现方，不在断言里）。

### 10.1 SPEC01：已迁移 1 条，其余 11 条待阶段 B 前补

| 断言 ID | 方法 | 现状 | 原因 |
|---|---|---|---|
| `UI.SpawnAnchor` | `AssertSpawnAnchor(IQaSink sink)` | **已迁移** | 阶段 A 示例 |
| `Locomotion.AllPlayersMoveParam` | `AssertLocomotion()` | 经 `_ev` | 阶段 A 只做 1 条样例 |
| `Regression.FND02A` | `AssertRegressionFND02A()` + `RecordRegressionFinal()` | 经 `_ev`（记录在编排方法里） | 同上；该方法还汇总 `SPEC01_Observer` 的全局状态 |
| `Attack.DualPathUnified` | `AssertDualPathAndClocks()` | 经 `_ev` | 同上（同方法一次出 3 条断言） |
| `Clock.MeleeNormalizedWindow` | `AssertDualPathAndClocks()` | 经 `_ev` | 同上 |
| `Clock.RangedOneShotPerCycle` | `AssertDualPathAndClocks()` | 经 `_ev` | 同上 |
| `Animation.PostCombatAndHit` | `AssertPostCombatAndHit()` | 经 `_ev` | 同上 |
| `UI.DamageNumberScale` | `AssertDamageNumber()` | 经 `_ev` | 同上 |
| `Skill.CastSmoke` | `AssertSkillCastSmoke()` | 经 `_ev` | 同上 |
| `Skill.InputWired` | `AssertSkillInputWired()` | 经 `_ev` | 同上 |
| `Skill.CursorHover` | `AssertCursorHover()` | 经 `_ev` | 同上 |
| `Skill.TargetPick` | `AssertTargetPick()` | 经 `_ev` | 同上 |

### 10.2 其他套件（本阶段未触碰，整批属各自阶段）

| 套件 | `Assert*` 方法数 | 现状 | 归属 |
|---|---|---|---|
| REFACTOR | 16 | 全部经本地 `_ev`（`REFACTOR_Evidence`），违禁 token = 0 | 阶段 B |
| DIAG | 7 | 全部经本地 `_ev`（`DIAG_Evidence`），违禁 token = 0 | 阶段 D |
| FND00B06 | 0（断言内联在 `Start` 等非 `Assert*` 方法里） | 无 `Assert*` 可改，需先拆分/命名 | 阶段 C |
