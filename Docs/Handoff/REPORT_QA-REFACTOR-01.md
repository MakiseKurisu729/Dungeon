# REPORT — QA-REFACTOR-01：测试代码分层重构（Qa 公共层 + 四套件迁移，不改断言）

> 对应 `Docs/Handoff/TASK_QA-REFACTOR-01.md`。节次与 `Docs/Handoff/workflow-rules.md` §2 一致，**不得删节**。
> **阅读顺序**：§2 = 整单终态 AC 表；§1 = 整单结论；§E = 终验明细与小结；§0/§3/§4/§5/§7/§8/§10（阶段 A）与 §B/§C/§D 为分阶段过程记录，仅作追溯、不再维护。

## 0. 开工前六问（TASK §4 强制）

1. **唯一主要结果**：建立 `Tests/QA/Runtime/` 公共层（`QaJson` / `QaSink`(`IQaSink`) / `QaEvidence`+`QaEvidenceSpec` / `QaWait` / `QaFixtures` / `QaScene`），把 `SkillCastFixture.cs`（连 `.meta`、GUID 不变）移入，`SPEC01_Json` 全仓改名 `QaJson`，SPEC01 改成「薄适配器 + 复用 Qa 层」，并让 **1 条示例断言**改成 `AssertXxx(IQaSink sink, …)`；**SPEC01 仍然 12 pass / 0 fail，证据文件名/键序/字段序逐字符不变**。
2. **明确不做什么**：不做阶段 B（REFACTOR）、C（FND00B06）、D（DIAG）、E（统一 Boot/Launcher、QaTestTemplate、Docs/06、Docs/01 §14.1）；不改任何断言的 ID/期望/阈值/通过条件/`qaItem`/`status`；不新增/删除/合并断言；不改产品代码 `Scripts/**`、场景、预制体、Animator、`.asset`、`ProjectSettings`、`Packages`；不改 `QA_InEditorMenu` 的菜单文本/子目录规则/`runInBackground` 还原；不动 `Tests/FND00B/FND00B04*|FND00B05*`、`Tests/FND02A/**`；不做任何 git 写操作。
3. **允许改哪些文件**：`Assets/_Project/Tests/**`（新增 6 个 `.cs`、移动 1 个 `.cs`+`.meta`、修改 4 个 `.cs`）+ `Docs/Handoff/REPORT_QA-REFACTOR-01.md` + `Docs/QA/Evidence/QA-REFACTOR-01/**`。实际改动见 §4 清单（与 TASK §2.6 逐项一致）。
4. **必须保持哪些旧行为**：证据目录与文件名；Summary 键名与键序（含 SPEC01 重复的 `mode` 键，禁止修）；断言行字段与字段序；UTF-8 无 BOM；`ExitCode` 内容；`SessionState` 键（`SPEC01.*`、`QA.InEditor.*`）；菜单项文本；`*_Runner` GameObject 名；命令行入口名 `SPEC01_BatchAcceptance.Run`；以及全部断言口径。
5. **如何证明完成**：编译绿（`error CS` = 0）；SPEC01 菜单跑 1 次得 12 行 / 12 pass / 0 fail；`(qaItem, assertion, status)` 排序后与 `before` 逐行一致（`DIFF` 为空）；文件名集合、Summary 键序（22 键含重复 `mode`）、断言行字段序、BOM 状态逐项比对；`IQaSink` 静态检查。证据：`Docs/QA/Evidence/QA-REFACTOR-01/`。
6. **失败后在哪里停**：命中 TASK §4 熔断（断言 ID/条数/`status`/阈值变化；需改 `Scripts/**` 或资产；编译连续 2 轮红；证据文件名/字段无法用 `QaEvidenceSpec` 表达；断言体内直接写盘或依赖引导且未声明）→ 立即停、保留现场（不清理半成品、不回退用户文件），在 REPORT 与回执写清现象与复现步骤。

## 1. 结果摘要

`完成`（五阶段 A→E 全部执行完毕；AC-1~AC-6、AC-8~AC-10 全过，AC-7 结构 8/8 等价、L2 域重载因编辑器脚本重载策略未能在 Play 中触发，已留现场并列入未决）。

一句话：四套守卫的 Boot/证据/JSON/夹具/等待/场景查询已收敛到 `Tests/QA/` 单份实现，四套件各连跑 2 次计数与迁移前完全一致（SPEC01 12/0、REFACTOR 16/0、FND00B06 12/0+2 observed、DIAG 6/1 已知红不变），断言 ID、`expected` 字面量与证据文件名/键序逐字符未漂移。

## 2. AC 回填表（整单终态）

> 原「阶段 A 占位行」已在阶段 E 收口替换。**本表 = 交付口径**；A–D 过程记录见文末附录，终验明细见 §E.2（AC-7 逐条见 §E.2.1）。

| AC | 结果 | 范围 / 断言 ID | 关键数据 | 证据 |
|---|---|---|---|---|
| **AC-1** 断言集合与 `expected` 不漂移 | **通过** | 四套件全部（12 / 16 / 14 / 7 行） | 四套件 `(qaItem, assertion, status)` 多重集 `IDENTICAL`；`expected` 字面量逐字符 `IDENTICAL` | `final/ac1_recheck.txt`、`<套件>/diff_AssertionIds.txt`、`<套件>/diff_Expected.txt` |
| **AC-2** 四套件各连跑 2 次 | **通过** | 同上 | run1/run2 均：SPEC01 12/0、REFACTOR 16/0、FND00B06 12/0+observed2、DIAG 6/1（红项 `Diag.Input.FirstExit`） | `final/run1/`、`final/run2/` 下各套件 |
| **AC-3** 证据文件名/键序/字段序不变 | **通过** | 四套件 | 文件名集合一致；Summary 键序一致（SPEC01 仍含重复 `mode`）；REFACTOR 断言行仍带 `milestone`；全无 BOM | `<套件>/diff_EvidenceShape.txt`、`final/run1/<套件>/counts.txt` |
| **AC-4** 改动范围 | **通过** | — | `Scripts/**` 零改动；代码仅 `Tests/**`；`Docs/01` 本单 +1 行（§14.1）；所有者 `DOC-INFRA-01` 未提交改动已隔离、未计入 | `git_scope.txt` |
| **AC-5** `QaTestTemplate.md` | **通过** | `Qa.Template.Smoke`（示例，不参与运行） | 夹具 → `IQaSink` 断言 → 证据落点 → ID 规范 → 新增套件清单 → 禁止事项；API 与仓库一致 | `Assets/_Project/Tests/QaTestTemplate.md` |
| **AC-6** 重复实现份数归零 | **通过** | — | `_Boot`=0、`_Json`=0、`PlayerProbe`=1、`HitCollector`=1、`Dict(`=1、`WaitState(`=1、`GetHead(`=1、证据根字面量=2；四适配器 32/38/33/34 行且无 I/O | `dedup_counts.txt` |
| **AC-7** Launcher 语义等价 + L2 | **通过（结构 8/8）／L2 未触发** | — | ①–⑧ 逐条等价（10s/3s 宽限、`code=3 reason=runner-lost-domain-reload`、`TryFinish` 优先、双判定 `RunnerAlive`、`DontDestroyOnLoad`）；L2 三次 Play 中注入均被 Unity 延迟到退出 Play 后编译，无域重载、无看门狗；被触碰脚本 blob sha 与 HEAD 一致（净零） | §E.2.1、`l2_domainreload/`、`netzero_diff.txt` |
| **AC-8** 编译零错误 + 无新增静态可变全局 | **通过** | — | `error CS`=0；两个程序集重建时间晚于各自最新源；可变静态字段净减 5 | `compile.log`、§E.2.2 |
| **AC-9** 分层文档六节 + §14.1 指针 | **通过** | — | `Docs/06` 六节齐备（L0/L1/L2/功能套件、套件契约与注册式边界、ID 规范含真实例子、扩展规则、证据 schema 版本化、构建隔离）；`Docs/01` §14.1 末 +1 行 | `Docs/06_测试架构与验收分层.md`、`git_scope.txt` |
| **AC-10** 断言签名 UTP 就绪 | **通过** | SPEC01 10 方法 / 12 断言、REFACTOR 16、FND00B06 14、DIAG 7 | `Assert*(IQaSink sink)` = 10/16/14/7；断言体内违禁 token=0、`_ev.`=0；`IQaSink` 唯一且恰 4 成员；`QaEvidence : IQaSink`=1 | `utp_readiness.txt` PHASE E、§E.2.3 |

- **运行次数**：基线 4 次（阶段 A 开工前）+ 每阶段本套件回归 1 次 + 终验 8 次（4 套件 × 2）；L2 注入 3 次（未触发域重载）。
- **已知红**：`Diag.Input.FirstExit` = `product_fail`，与迁移前逐字一致，未“顺手修绿”。
- **未决**：L2 需改编辑器偏好（Play 期间脚本重载策略）才能真触发，属所有者侧操作，见 §E.7。

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

---

# 阶段 B — REFACTOR 套件迁移（执行者会话，2026-11-06）

> 本节由阶段 B 执行者**追加**；上面 §1–§10 是阶段 A 的原文，未改一字。

## B.0 开工前六问（本阶段）

1. **唯一主要结果**：REFACTOR 的 16 条断言改为「只经 `IQaSink` 出证据」，`REFACTOR_Evidence` 变成只组装 `QaEvidenceSpec` 的薄适配器（`class REFACTOR_Evidence : QaEvidence`，构造函数仍是 `(int run)`），套件本地重复的夹具/等待/JSON/场景查询改用 `Tests/QA/` 公共层；**断言口径零变化**。
2. **明确不做什么**：不改任何断言 ID / `expected` 字面量 / 阈值 / 通过条件 / 条数；不删 `REFACTOR_Boot`；不碰 `Scripts/**`、场景/预制体/Animator/`.asset`/`ProjectSettings`、`CURRENT.md`、其他 `TASK_*`/`REPORT_*`、`FND00B04*`/`FND00B05*`/`FND02A/**`；不做任何 git 写操作；不跑 `run_tests`。
3. **允许改哪些文件**：`Assets/_Project/Tests/**`（本阶段实际只改 4 个）+ `Docs/Handoff/REPORT_QA-REFACTOR-01.md` + `Docs/QA/Evidence/QA-REFACTOR-01/**`（只新增/追加）。
4. **必须保持哪些旧行为**：断言行字段序 `run, milestone, qaItem, …`（REFACTOR 特有，逐字符保留）；Summary 17 键序（`task, milestone, run, …`）；`REFACTOR_Runs.jsonl` 键序；文件名 `REFACTOR_Assertions.jsonl` / `REFACTOR_Summary_run<N>.json` / `REFACTOR_ExitCode_run<N>.txt` / `REFACTOR_Runs.jsonl`；`run<=1 覆盖 / run>=2 追加`；`REFACTOR_MILESTONE` 语义（缺省 `M0`）；菜单项文本、`SessionState` 键、`REFACTOR_Runner` 名字。
5. **如何证明完成**：编译 `error CS = 0`；菜单跑 1 次 → 16 行 16 pass / 0 fail、exit 0；三份机检 diff（`(qaItem, assertion, status)` 排序比对、`expected` 逐字符、证据形状）+ AC-10 静态检查；另用脚本证明 16 个断言方法体在「机械改名归一化」后与迁移前**逐字节相同**（15/16 完全相同，1 处只差 `HitCollector` 构造参数，见 B.5）。
6. **失败后在哪里停**：断言 ID/条数/`status`/阈值/通过条件任一变化、需改产品代码或资产、编译连续 2 轮红、证据字段或文件名无法兼容、单步重试 ≥3 次仍红 → 按 TASK §4 熔断，留现场并写 REPORT，不自行取舍。

## B.1 结果摘要

| 项 | 迁移前（`REFACTOR/before`，源 `REFACTOR-M4/editor_run17`） | 迁移后（`REFACTOR/after`，源 `REFACTOR-M4/editor_run18`） |
|---|---|---|
| 断言行数 | 16 | 16 |
| pass / fail | 16 / 0 | 16 / 0 |
| blocked / infra / observed | 0 / 0 / 0 | 0 / 0 / 0 |
| ExitCode | `0` | `0` |
| Summary 键数/键序 | 17 键 | 17 键，键序逐字符一致 |
| 断言行字段序 | `run, milestone, qaItem, …` | 同左（`milestone` 保留在 `run` 之后） |
| Runs 索引键序 | `milestone, run, pass, …` | 同左 |
| 文件名集合 | 5 个 | 5 个，完全一致 |

编译：`error CS` = 0（整仓，含 SPEC01 / FND00B06 / DIAG —— 它们的源码本阶段**零改动**，因此必然仍可编译）。

过程记录诚实说明：第一次编译出现 8 处 `CS1503`（把本地 `WaitState(PlayerProbe,…)` 机械改名为 `QaWait.WaitState(probe,…)`，而 `QaWait` 收 `Animator`）。属本阶段 1 轮修复额度内：8 处调用改为 `QaWait.WaitState(<probe>.animator, …)`，未改任何其他内容；第二次编译起全绿。原始报错与修法见 `compile_phaseB.txt`。

## B.2 改动文件清单（行数 前→后）

| 文件 | 行数 | 改动性质 |
|---|---|---|
| `Assets/_Project/Tests/QA/Runtime/QaEvidence.cs` | 363 → 386（+29 / −6） | **纯增量**：`QaEvidenceSpec.PrefixedFields`（断言行/Runs 索引行在 `run` 之前的插入字段）、`QaSummaryKey.PrefixedFields`（Summary 里就地展开）、`BuildAssertionLine` 由 `static` 改实例并调用新增的 `AppendPrefixedFields`、`AppendRunIndex` 先写前置字段。三套既有套件不声明 `PrefixedFields` → 行为不变。 |
| `Assets/_Project/Tests/REFACTOR/Runtime/REFACTOR_Evidence.cs` | 265 → 38（−227） | 改为薄适配器 `class REFACTOR_Evidence : QaEvidence`，只组装 `QaEvidenceSpec`（含 `PrefixedFields = milestone`、17 键 Summary 序、`ShouldTruncateAssertions = (r,m) => r <= 1`）；本地 `REFACTOR_Json` 连同整个文件 I/O / 计分逻辑删除（全仓无外部引用，已 grep 确认）。38 行 ≤ TASK §2.3 的 40 行上限。 |
| `Assets/_Project/Tests/REFACTOR/Runtime/REFACTOR_ProbeRunner.cs` | 2201 → 2019（+186 / −368） | 删本地 `PlayerProbe`/`HitCollector`/`WaitState`/`StateName`/`StateNorm`/`Select`/`FindPlayer`/`GetHead`/`Dict`/`V3`，改用 `QaFixtures`/`QaWait`/`QaScene`/`QaJson`；`CreateTarget` 保留本地壳（先 `DestroyTarget()` 再委托 `QaFixtures.CreateTarget(name,pos,false)` 并把 `UnitHealth.stats` 收回 `_targetStats` 参与销毁）；`FindPlayers` 改为 `QaFixtures.FindPlayers()`；16 个 `Assert*` 全部改为 `IEnumerator AssertXxx(IQSink sink, …)`。 |
| `Assets/_Project/Tests/REFACTOR/Editor/REFACTOR_BatchAcceptance.cs` | 241 → 213（+1 / −29） | `GitHead()` → `QaScene.GetHead()`（语义等价：`sessionKey == null` 走真 `git rev-parse HEAD`，失败返回 `"unknown"`，与旧实现同）；删本地 `GitHead` 与随之无用的 `using System.IO` / `using System.Text`。**`REFACTOR_Boot` 原样保留**（留到阶段 E）。 |

合计 4 文件，全部在 `Assets/_Project/Tests/**`（`git diff --numstat` 见 B.6）。**其他套件零改动**：`Tests/SPEC01/**`、`Tests/FND00B/**`、`Tests/DIAG/**`、`Tests/FND02A/**`、`Tests/QA/{QaJson,QaSink,QaFixtures,QaWait,QaScene,SkillCastFixture,QA_InEditorMenu}` 在 `git status --porcelain -- Assets/_Project/Tests` 里都不出现。

## B.3 关键位置（`文件:行`）

1. `Assets/_Project/Tests/QA/Runtime/QaEvidence.cs:89` —— `QaEvidenceSpec.PrefixedFields` 定义（纯增量，null = 不输出）。
2. `Assets/_Project/Tests/QA/Runtime/QaEvidence.cs:296` —— `BuildAssertionLine` 在 `"run"` 之后调用 `AppendPrefixedFields(sb)` → 复现 `run, milestone, qaItem, …`；实现见 `:311`。
3. `Assets/_Project/Tests/REFACTOR/Runtime/REFACTOR_Evidence.cs:8` / `:36` —— 适配器类声明 / `PrefixedFields = [milestone]`（第 36 行同时锁定 Summary 键序里 `milestone` 的位置）。
4. `Assets/_Project/Tests/REFACTOR/Runtime/REFACTOR_ProbeRunner.cs:172` —— 第一条断言的最终形状 `AssertPartyMove(IQSink sink, IReadOnlyList<QaFixtures.PlayerProbe> players)`；`:1591` 为最后一条 `AssertMotionUnlocked`。
5. `Assets/_Project/Tests/REFACTOR/Runtime/REFACTOR_ProbeRunner.cs:1950-1957` —— `CreateTarget` 委托 `QaFixtures.CreateTarget(name, pos, false)` 并保留本地销毁簿记；`:1942` 为 `_players.AddRange(QaFixtures.FindPlayers())`；`Assets/_Project/Tests/REFACTOR/Editor/REFACTOR_BatchAcceptance.cs:30` 为 `QaScene.GetHead()`。

断言行/证据路径侧的等价性证据不靠人眼：见 B.4 的三份机检 diff 与「断言体逐字节比对」。

## B.4 AC 回填（**仅 B 阶段**，其余 AC 仍是未运行）

| AC | B 阶段结论 | 证据 |
|---|---|---|
| **AC-1** | **PASS（REFACTOR）**：`(qaItem, assertion, status)` 排序后逐行一致，`DIFF = (empty)` / `RESULT: IDENTICAL`；BEFORE = AFTER = 16 行，pass 16 / fail 0；`expected` 16 条**逐字符**一致（`RESULT: IDENTICAL (16 assertions, char-by-char)`）。另加独立验证：16 个 `Assert*` 方法体按「`_ev.`→`sink.`、`_players`→`players`、`Dict/V3/StateName/StateNorm/Select/WaitState/GetHead` 加 `QaJson.`/`QaFixtures.`/`QaWait.`/`QaScene.` 前缀」归一化后与迁移前**逐字节相同（15/16）**，第 16 条只差 `new HitCollector()` → `new QaFixtures.HitCollector(200, false)`（B.5 第 1 条） | `Docs/QA/Evidence/QA-REFACTOR-01/REFACTOR/diff_AssertionIds.txt`、`.../diff_Expected.txt` |
| **AC-3** | **PASS（REFACTOR）**：文件名集合 5 = 5 一致；Summary 17 键序 order-sensitive 一致（含 `milestone` 在 `task` 之后）；断言行字段序一致（59 个 token，含 `run, milestone, qaItem, …`）；Runs 索引 `milestone, run, pass, …` 一致；全部文件 `BOM=False`；ExitCode 内容 `0 / 0 IDENTICAL`。**唯一值级差异**：`head` 由 `df7ea0b0…`（before，阶段 A 起点）变成 `e7ab53f1…`（after，阶段 A 提交后），`assertionFile` 里的 `editor_run17`→`editor_run18`；两者都是实测值，AC-3 只比对形状 | `Docs/QA/Evidence/QA-REFACTOR-01/REFACTOR/diff_EvidenceShape.txt`、`.../shape.ps1` |
| **AC-10** | **PASS（REFACTOR）**：③ `IEnumerator Assert\w+\(IQaSink sink` = **16**（期望 16）；断言体内 `File.` / `Directory.` / `SessionState` / `EditorApplication` / `QaLauncher` / `_Boot` = **0**；体内残留 `_ev` = **none**（16 个方法逐方法花括号扫描）；② `class QaEvidence : IQaSink` = 1、`interface IQaSink` = 1、`QaSink.cs` 违禁 token = 0；⑤ 见 B.7「仍直写文件 / 仍依赖引导的断言 = 无」 | `Docs/QA/Evidence/QA-REFACTOR-01/utp_readiness.txt`（文件末尾 `PHASE B` 段，B1–B5） |
| **AC-2** | **未运行（属阶段 E）**：本阶段只按 TASK §3「阶段 A–D 各跑本套件 1 次」跑 REFACTOR 1 次；四套件连跑 2 次在阶段 E | — |
| **AC-4** | **未运行（属阶段 E 交付）**：本阶段只做局部自检（`git diff --numstat -- Assets/_Project/Tests` + `git status --porcelain`），完整 `git_scope.txt` 由阶段 E 出；已确认 `Scripts/**` 与资产零改动 | B.6 |
| **AC-5** | **未运行（阶段 E：`QaTestTemplate.md` 尚未创建）** | — |
| **AC-6** | **未运行（阶段 E 统一点数）**：本阶段相关项已可预见成立（`class PlayerProbe` 本地定义 = 0、`class HitCollector` 本地定义 = 0、`GetHead(` 本地定义 = 0、`IEnumerator WaitState(` 本地定义 = 0），但 `dedup_counts.txt` 按 TASK 在阶段 E 生成 | `utp_readiness.txt` B3 段（局部计数） |
| **AC-7** | **未运行（阶段 E：`QaLauncher` 尚未建；`REFACTOR_Boot` 按 TASK 保留）** | — |
| **AC-9** | **未运行（阶段 E：`Docs/06` 尚未创建）** | — |

## B.5 未合并项及原因（去重纪律要求的“单列”）

1. **`HitCollector` 只合并了“等价那部分”**：本地 `new HitCollector()` 的上限常量是 200 且不带 `element` 字段，`QaFixtures.HitCollector` 是「上限 + 是否带 element」双参数。故改写为 `new QaFixtures.HitCollector(200, false)` —— 逐字段等价（`time/sourceId/state/normalizedTime/amount` 同序，`FilterSource` 语义相同）。**这是 16 条断言里唯一的源码差异**，且不涉及判定表达式。
2. **宝箱夹具三份未合并**：`AssertChest`（`REFACTOR_Chest`，`openDuration=0.3f`，先 `Chest` 后 `Interactable`）、`AssertSkillInterruptInteract`（`REFACTOR_SkillInterruptChest`，`openDuration=0f`，先 `Interactable` 后 `Chest`）、`AssertInteractionContract`（`REFACTOR_ContractChest`，`openDuration=0f`，`Interactable` 先于 `Chest`）。三者在 `openDuration`、组件添加顺序、挂载位置/数量上**边界不同**，参数化后无法逐分支证明等价，故按要求**留在原处**（仍在各断言体内，不是新增复制）。
3. **`F(float)` / `V3Text(Vector3)` 保留本地**：`QaJson`/`QaFixtures`/`QaScene` 都没有等价物（`F` 是 `"0.###"` 格式、`V3Text` 是日志用坐标串，而 `QaJson.V3` 产出的是字典）。**不为凑统一把它们塞进公共层**；若阶段 E 认为应下沉，请大脑裁决。
4. **`_target` / `_chestGo` / `_targetStats` / `_chestItem` 仍是 runner 字段而非断言参数**：它们是“断言内创建、由 `DestroyTarget`/`CleanupFixtures`/`Finish` 统一销毁”的生命周期状态，改成参数需要重排销毁时序（有行为风险）。**夹具侧的“玩家集合”已按要求参数化**（12 条需要玩家的断言都显式收 `IReadOnlyList<QaFixtures.PlayerProbe> players`），其余四条（`Skill.QW`/`Projectile.AimPoint`/`StateFacade`/`Projectile`）自身不依赖玩家集合。若大脑认为目标/宝箱也必须进参数，属额外重构，建议单列一条 TASK。
5. **`SkillCastFixture.CreateTarget` 仍未合并**（沿用阶段 A 结论）：`maxHp=1000`、固定 `showDamageNumber=false`、结果进 `TargetGo/Target/TargetStats` 参与 `Dispose()`；与 `QaFixtures.CreateTarget(name,pos,showDamageNumber)`（`maxHp=100000`）边界不同。
6. **`GetHead` 的两种语义仍是同一个方法的两条分支**（沿用阶段 A 结论）：断言/编排侧传 `"REFACTOR.Head"` 走 `SessionState` 缓存（`Play` 模式不再起进程），`BatchAcceptance` 侧不传 key 走真 `git`。`QaScene.GetHead(sessionKey)` 已参数化承载，未二选一。

## B.6 证据清单（本阶段新增，全部为新增文件/追加）

| 文件 | 内容 |
|---|---|
| `Docs/QA/Evidence/QA-REFACTOR-01/REFACTOR/after/` | 迁移后快照（源 `REFACTOR-M4/editor_run18`，5 文件，复制而非移动，canonical 目录未改） |
| `Docs/QA/Evidence/QA-REFACTOR-01/REFACTOR/diff_AssertionIds.txt` | BEFORE / AFTER / DIFF 三段，`DIFF = (empty)`，`RESULT: IDENTICAL` |
| `Docs/QA/Evidence/QA-REFACTOR-01/REFACTOR/diff_Expected.txt` | 16 条 `expected` 逐字符比对，`RESULT: IDENTICAL (16 assertions, char-by-char)` |
| `Docs/QA/Evidence/QA-REFACTOR-01/REFACTOR/diff_EvidenceShape.txt` | 文件名集合、Summary 17 键序、断言行字段序（含 `milestone`）、Runs 索引序、BOM/CRLF、ExitCode |
| `Docs/QA/Evidence/QA-REFACTOR-01/REFACTOR/{compare,expected,shape}.ps1` | 上述三份 diff 的可重跑脚本 |
| `Docs/QA/Evidence/QA-REFACTOR-01/compile_phaseB.txt` | 首轮 8 条 `CS1503`（现场）+ 修法 + 修后 `error CS = 0` |
| `Docs/QA/Evidence/QA-REFACTOR-01/editor_state_phaseB.txt` | 开工/编译后/跑前/收工 四次 `editor_state`（`is_compiling`/`is_domain_reload_pending`/`playMode`）与 `editor_run18` 结果 |
| `Docs/QA/Evidence/QA-REFACTOR-01/utp_readiness.txt` | **追加** `PHASE B` 段（B1–B5 计数），原有 `PHASE A` 段一字未改 |
| `git diff --numstat -- Assets/_Project/Tests`（本阶段实测） | `29/6 QaEvidence.cs`、`1/29 REFACTOR_BatchAcceptance.cs`、`30/257 REFACTOR_Evidence.cs`、`186/368 REFACTOR_ProbeRunner.cs`；`git status --porcelain -- Assets/_Project/Tests` 只有这 4 个 `M`，无新增/删除、无 `.meta` 变化 |

## B.7 §10 更新（REFACTOR）：仍直写文件 / 仍依赖引导的断言

**结论：无。** REFACTOR 16 条断言全部满足 AC-10 ③：首参 `IQaSink sink`（12 条另收 `IReadOnlyList<QaFixtures.PlayerProbe> players`），体内只经 `sink.Record` / `sink.BlockedAssertion` / `sink.InfraFailure` 出证据；逐方法扫描 `File.` / `Directory.` / `SessionState` / `EditorApplication` / `QaLauncher` / `_Boot` = 0，残留 `_ev` = none。落盘、计分、Summary、Runs 索引全部在 `QaEvidence`（`IQaSink` 实现方），编排层（`Start` / `Update` 看门狗 / `RunSafe` / `Finish`）照旧使用 `SessionState` 与 `EditorApplication`，位置未被搬进断言。

## B.8 目检项（≤3，供所有者）

1. 打开 `Docs/QA/Evidence/QA-REFACTOR-01/REFACTOR/diff_EvidenceShape.txt`，确认 Summary 第 2 键是 `milestone`、断言行前两键是 `run, milestone`。
2. `git diff -- Assets/_Project/Tests/REFACTOR/Runtime/REFACTOR_Evidence.cs` 目视确认适配器只剩 spec 组装（无 I/O）；`REFACTOR_ProbeRunner.cs` 里 `REFACTOR_Boot` 仍在 `REFACTOR_BatchAcceptance.cs` 中。
3. Unity Console 只应有 `[REFACTOR]`/`[REFACTOR][ASSERT]` 日志，无 `error CS`；`Docs/QA/Evidence/REFACTOR-M4/editor_run18/` 为本次只追加产物。

## B.9 建议提交（阶段 B）

```text
test(qa): REFACTOR 套件迁移到 Qa 公共层（阶段 B，断言零变化）

为什么：把 REFACTOR 里重复的夹具/等待/JSON/场景查询换成唯一共享层、让 16 条断言只经 IQaSink 出证据，
并用“断言三重集 + expected 逐字符 + 证据形状 + 断言体逐字节比对”机检证明迁移没动断言口径。
```

（由所有者用 GitHub Desktop 提交；本执行者未做任何 git 写操作。）

## B.10 消耗记录（阶段 B）

- 钱包：`Go 订阅`
- 模型完整 ID：`opencode-go/deepseek-v4.1-flash`
- 轮次：1 轮实现 + 1 轮修复（8 处 `CS1503`，额度内）；套件运行 1 次（REFACTOR）。

---

# 阶段 C — FND00B06 套件迁移（执行者会话，2026-11-06）

## C.0 开工前六问（本阶段）

| 问 | 答 |
|---|---|
| 唯一主要结果 | FND00B06 的 14 条断言变成 `IEnumerator AssertXxx(IQSink sink, …)`，断言口径（ID / `qaItem` / `expected` / 阈值 / `status` / 条数）零变化；证据文件名与 Summary 键序零变化 |
| 明确不做什么 | 阶段 D（DIAG）/ 阶段 E（统一 Boot、删 `*_Boot`、模板、`Docs/06`）；不动 `Scripts/**`、资产、`ProjectSettings`、`CURRENT.md`；不删其他三套件的 `*_Boot`；不碰 `FND00B04/05`、`FND02A` |
| 允许改哪些文件 | `Assets/_Project/Tests/**`（本阶段只改 `FND00B06_Evidence.cs`、`FND00B06_ProbeRunner.cs`）+ `Docs/Handoff/REPORT_QA-REFACTOR-01.md` + `Docs/QA/Evidence/QA-REFACTOR-01/**` |
| 必须保持哪些旧行为 | 证据目录 `FND-00B-06/[<subdir>]`、`06_Assertions.jsonl`、`06_Summary_run<N>.json`、`06_ExitCode_run<N>.txt`（内容 `0`）、**无 Runs 索引**；Summary 16 键键序；断言行 10 字段字段序；UTF-8 无 BOM；菜单项文本；`FND00B06.*` SessionState 键；`FND00B06_Runner` 名字 |
| 如何证明完成 | 编译 `error CS`=0 + 菜单跑 1 次得 12 pass / 0 fail / observed 2（14 行）+ 三份 diff（triple / expected / shape）+ `utp_readiness.txt` PHASE C 段静态检查全 PASS |
| 失败后在哪里停 | 任一断言 ID/条数/status/阈值变化、需要改 `Scripts/**` 或资产、编译连续 2 轮红、`QaEvidenceSpec` 表达不了现状 → 停并留现场（TASK §4 熔断） |

## C.1 结果摘要

- 编译：`error CS` = 0（`Assembly-CSharp.dll` 重编于 14:41:49，晚于最后一次编辑 14:41:08）。
- 套件：`Tools/QA/In-Editor/FND00B06` 跑 1 次 → **12 pass / 0 fail / blocked 0 / infraFail 0 / observed 2，共 14 行**，`code=0`，`evidenceSubdir=editor_run6`。
- 断言 ID 三重集：BEFORE 14 / AFTER 14，**DIFF 空**（`RESULT: IDENTICAL`）。
- `expected` 字面量：14/14 **逐字符一致**（`IDENTICAL`）。
- 证据形状：文件名集合一致（4 个）、Summary 16 键键序一致、断言行字段序一致（136 个键序位一致）、两侧均无 BOM、`06_ExitCode_run1.txt` 仍在且内容 `0`、两侧均无 Runs 索引。
- AC-10：FND00B06 14 条 `Assert*(IQaSink sink)`；断言体内 `File./Directory./SessionState/EditorApplication/QaLauncher/_Boot` = 0；残留 `_ev` = none。
- **口径说明（与派单文字的一处偏差，必须记录）**：派单写“4 项攻击 6 条 + 6 项动画 6 条 + 2 observed”，实测 `before/06_Assertions.jsonl` 是 **7 条 item-4 + 7 条 item-6**；2 条 observed 分别是 `Animation.ConfigMapping` 与 `Animation.PostCombatState`（本轮 `Attack.TargetDeath` 因目标死亡走 pass 分支；其 `observed` 分支代码原样保留）。本阶段按实测 7+7 拆分，未增删任何断言。

## C.2 改动清单（含行数）

| 文件 | 改法 | 行数 |
|---|---|---|
| `Tests/FND00B/Runtime/FND00B06_Evidence.cs` | 改为 `class FND00B06_Evidence : QaEvidence` 薄适配器（只组装 `QaEvidenceSpec`）；删 `FND00B06_Json`（改用 `QaJson`） | 240 → **33** |
| `Tests/FND00B/Runtime/FND00B06_ProbeRunner.cs` | 14 条断言抽成 `AssertXxx(IQSink sink, …)`；删本地 `Dict`/`V3`（改用 `QaJson.Dict`/`QaJson.V3`）；`Finish()` 的 ExitCode 改用 `QaEvidence.ExitCodePath()`；`WriteSummary` 增加 `qaItems` extra | 1514 → **1593** |
| `Tests/FND00B/Editor/FND00B06_BatchAcceptance.cs` | **未改**（本套件无 `GitHead`，已是 `SessionState` + `QaScene` 形态；`FND00B06_Boot` 按 TASK §2.5 留到阶段 E） | 213（不变） |

## C.3 关键位置（`文件:行`）

1. `Assets/_Project/Tests/FND00B/Runtime/FND00B06_Evidence.cs:6` — `public class FND00B06_Evidence : QaEvidence`（33 行适配器；`EvidenceFolder="FND-00B-06"`、`ExitCodeFileName="06_ExitCode_run{run}.txt"`、`RunIndexFileName=null`、`ShouldTruncateAssertions=(r,m)=>r==1`）。
2. `Assets/_Project/Tests/FND00B/Runtime/FND00B06_ProbeRunner.cs:23-31`（`SummaryFields` 见适配器）与 `:204` — `WriteSummary(…, QaJson.Dict("qaItems","4,6"))`，保住 `run, task, qaItems, …` 的 16 键键序（`QaSummaryKey.Run, Task, Extra, StartedAt, …`）。
3. `Assets/_Project/Tests/FND00B/Runtime/FND00B06_ProbeRunner.cs:223` — ExitCode 不再在 runner 里拼路径：`string exitPath = _ev != null ? _ev.ExitCodePath() : null;`（文件名逐字符不变）。
4. `Assets/_Project/Tests/FND00B/Runtime/FND00B06_ProbeRunner.cs:438-445` / `:521-524` / `:909-933` — 编排层改成 `yield return AssertXxx(sink, …)`，bail-out 级联仍留在编排层（用 `_ev.BlockedAssertion`）。
5. `Assets/_Project/Tests/FND00B/Runtime/FND00B06_ProbeRunner.cs:533` / `:714` / `:1119` — 三条代表性断言方法（夹具型 / 观察窗口型 / 夹具生命周期型）。

## C.4 AC 回填（仅 C 阶段范围）

| AC | C 阶段结论 | 证据 |
|---|---|---|
| **AC-1** | 通过：FND00B06 的 `(qaItem, assertion, status)` 排序后逐行一致，`DIFF` 空；14 条 `expected` 字面量逐字符一致；计数 14 = 14（pass 13 / observed 2 两侧同） | `Docs/QA/Evidence/QA-REFACTOR-01/FND00B06/diff_AssertionIds.txt`（`RESULT: IDENTICAL`）、`…/diff_Expected.txt`（`IDENTICAL (14 assertions …)`） |
| **AC-3** | 通过：文件名集合 `{06_Assertions.jsonl, 06_ExitCode_run1.txt, 06_Summary_run1.json, InEditorResult.txt}` 两侧一致；Summary 16 键键序一致；断言行字段序一致；无 BOM；ExitCode 内容 `0`；无 Runs 索引 | `…/diff_EvidenceShape.txt` |
| **AC-10** | 通过（FND00B06 部分）：14 条 `Assert\w+\(IQaSink sink`；逐方法花括号扫描禁用 token = 0，`_ev.` 残留 = none；`FND00B06_Json` 全树 0；适配器 33 行、无 I/O | `Docs/QA/Evidence/QA-REFACTOR-01/utp_readiness.txt` 的 `## PHASE C` 段（`ALL APPLICABLE CHECKS PASS`） |
| AC-2 / AC-8 / AC-4 等 | 不在本阶段验收（AC-2 连跑 2 次属阶段 E；AC-8 全仓编译属阶段 E；AC-4 的 git 范围由大脑在阶段末核） | — |

## C.5 14 条断言拆分对照表（旧位置 → 新方法）

> 旧行号 = 迁移前 `FND00B06_ProbeRunner.cs`（1514 行）的 `_ev.Record/BlockedAssertion` 行；新行号 = 本次 1593 行版本的方法声明行。判定表达式逐字搬入，未改阈值/期望/`status`。

| # | 断言 ID | 旧位置 | 新方法（行） |
|---|---|---|---|
| 1 | `4 / Attack.Fixture` | `GroupAttack` 内联 `:448` | `AssertAttackFixture(IQSink sink, UnitHealth player, UnitHealth target, NavMeshAgent playerAgent, int playerCount, int monsterCount, List<object> playerScan, string spawnMethod, List<object> spawnTrials)`（`:533`） |
| 2 | `4 / Attack.CommandAccepted` | `GroupAttack` 内联 `:529` | `AssertAttackCommandAccepted(IQSink sink, Selectable playerSelectable, bool selected, bool primaryOk, bool enemyMarked, bool orderedOk, string entryError, bool selectionManagerPresent)`（`:588`） |
| 3 | `4 / Attack.Approach` | `GroupAttack` 内联 `:587`（含 18s 接敌循环） | `AssertAttackApproach(IQSink sink, UnitHealth player, UnitHealth target, CharacterBrain brain)`（`:612`） |
| 4 | `4 / Attack.Starts` | `GroupAttack` 内联 `:640`（含 4s 出招等待循环） | `AssertAttackStarts(IQSink sink, UnitHealth player, UnitHealth target, MeleeCombat sword, CharacterBrain brain)`（`:669`） |
| 5 | `4 / Attack.Repeats` | `GroupAttack` 内联 `:703`（含 8s 伤害观察窗口） | `AssertAttackRepeats(IQSink sink, UnitHealth target, MeleeCombat sword, CharacterBrain brain)`（`:714`） |
| 6 | `4 / Attack.DamageResolved` | `GroupAttack` 内联 `:733` | `AssertAttackDamageResolved(IQSink sink, UnitHealth target)`（`:778`） |
| 7 | `4 / Attack.TargetDeath` | `GroupAttack` 内联 `:753`（died）/ `:767`（observed 分支） | `AssertAttackTargetDeath(IQSink sink, UnitHealth target)`（`:811`，两分支原样保留） |
| 8 | `6 / Animation.ConfigMapping` | `GroupAnimation` 内联 `:857` | `AssertAnimationConfigMapping(IQSink sink, Animator refAnim, MeleeCombat sword)`（`:937`，`status="observed"` 不变） |
| 9 | `6 / Animation.Idle` | `GroupAnimation` 内联 `:879`（blocked）/ `:893`（record） | `AssertAnimationIdle(IQSink sink)`（`:974`） |
| 10 | `6 / Animation.PostCombatState` | `GroupAnimation` 内联 `:925`（含 7.6s 观察 + 4 采样） | `AssertAnimationPostCombatState(IQSink sink, Animator animator, NavMeshAgent agent)`（`:1006`，`status="observed"`、`pass=false` 不变） |
| 11 | `6 / Animation.Move` | `GroupAnimation` 内联 `:944`（blocked）/ `:961`（record） | `AssertAnimationMove(IQSink sink)`（`:1041`） |
| 12 | `6 / Animation.Attack` | `GroupAnimation` 内联 `:976`（blocked）/ `:992`（record） | `AssertAnimationAttack(IQSink sink, CharacterBrain brain, MeleeCombat sword)`（`:1075`） |
| 13 | `6 / Animation.Hit` | `HitAnimationFixture` `:1025`（blocked）/ `:1120`（record），方法体 `:1019-1144` | `AssertAnimationHit(IQSink sink, UnitHealth player)`（`:1119`；`CreateAnimationFixture` 上移到编排层） |
| 14 | `6 / Animation.DownedOrDeath` | `DownedAnimationFixture` `:1150`（blocked）/ `:1189`（record），方法体 `:1146-1210` | `AssertAnimationDownedOrDeath(IQSink sink)`（`:1237`） |

**编排层保留项**：`Start` / `Update` 看门狗 / `Finish`（`SessionState` + 退出码）/ 夹具建立与清理（`CleanupAttackFixture`、`CreateAnimationFixture`、`Destroy`）/ 各级 blocked 级联 / `RestoreOtherBrains` 仍留在 runner；断言体内只调 `sink.*`。

## C.6 未合并项及原因（去重纪律）

| 本地实现 | 处置 | 原因（等价性判断） |
|---|---|---|
| `static Dictionary<string,object> Dict(params object[])` | **删除**，全部改 `QaJson.Dict(` | 与 `QaJson.Dict` 逐行等价（同为按 `kvs` 插入序建字典） |
| `static Dictionary<string,object> V3(Vector3)` | **删除**，全部改 `QaJson.V3(` | 与 `QaJson.V3` 等价（`{x,y,z}` 且字段序相同，序列化结果一致） |
| `static string GetPath(Transform)` | **留原地** | `QaFixtures`/`QaScene`/`QaWait` 均无等价物（`QaFixtures.StateName` 是动画状态名，不是层级路径）；套件内补注释说明 |
| `static float XZDistance(Vector3,Vector3)` | **留原地** | Qa 层无等价物；且它是 `Attack.Approach` 判定口径（XZ 平面距离）的直接实现，参数化会引入口径风险 |
| `string ResolveState(Animator)` | **留原地** | **不等价**：本套件先用 `DumpAnimator` 收集的 `_stateNames`（含 `" Locomotion"` 前导空格的真实状态名）做哈希查表，未命中再按 `KnownStates` 逐名比对，最后返回 `unknown#<hash>`；`QaFixtures.StateName` 是固定白名单且返回 `Other:<hash>`。换掉会改变证据里的 `stateName` 字面量，进而可能改变 `TrimState(state)=="Locomotion"` 之类的判定 |
| `AnimSample` / `DumpAnimator` / `TryPickSpawn` / `CreateAnimationFixture` | **留套件内** | 套件专有（FND00B06 的状态名表、NavMesh 落点策略、Knight 动画夹具生命周期），Qa 层无对应抽象 |

## C.7 证据清单（本阶段新增，全部为新增文件或追加）

| 路径 | 内容 |
|---|---|
| `Docs/QA/Evidence/QA-REFACTOR-01/FND00B06/before/` | 阶段 A 采集的迁移前基线（4 文件，本阶段只读） |
| `Docs/QA/Evidence/QA-REFACTOR-01/FND00B06/after/` | 本次 `FND-00B-06/editor_run6` 的复制快照（4 文件） |
| `Docs/QA/Evidence/QA-REFACTOR-01/FND00B06/diff_AssertionIds.txt` | 三重集比对（`RESULT: IDENTICAL`，diffRows=0） |
| `Docs/QA/Evidence/QA-REFACTOR-01/FND00B06/diff_Expected.txt` | 14 条 `expected` 逐字符比对（`IDENTICAL`） |
| `Docs/QA/Evidence/QA-REFACTOR-01/FND00B06/diff_EvidenceShape.txt` | 文件名/Summary 键序/断言行字段序/BOM/Runs/ExitCode |
| `Docs/QA/Evidence/QA-REFACTOR-01/FND00B06/{compare,expected,shape}.ps1` | 上述三份 diff 的可复跑脚本 |
| `Docs/QA/Evidence/QA-REFACTOR-01/utp_readiness.txt` | 追加 `## PHASE C` 段（C1–C5，`ALL APPLICABLE CHECKS PASS`） |
| `Docs/QA/Evidence/QA-REFACTOR-01/utp_readiness_phaseC.ps1` | PHASE C 静态检查脚本（含逐方法花括号扫描） |
| `Docs/QA/Evidence/QA-REFACTOR-01/compile_phaseC.txt` | `error CS`=0 + 重编时间 + 菜单项存在 + 改动行数 |
| `Docs/QA/Evidence/QA-REFACTOR-01/editor_state_phaseC.txt` | 模式 B 开工/编译/跑套件/收工四段 `editor_state` |
| `Docs/QA/Evidence/FND-00B-06/editor_run6/` | canonical 套件证据（只追加；菜单自动递增） |

## C.8 §10 更新（本阶段）：仍直写文件 / 仍依赖引导的断言

**结论：无。** FND00B06 的 14 条断言全部满足 AC-10 ③：首参 `IQaSink sink`（分别另收 0–8 个夹具/观测参数），体内只经 `sink.Record` / `sink.BlockedAssertion`；逐方法花括号扫描 `File.` / `Directory.` / `SessionState` / `EditorApplication` / `QaLauncher` / `_Boot` = 0，残留 `_ev.` = none。落盘、计分、Summary、ExitCode 路径全部在 `QaEvidence` / `IQaSink` 侧；`SessionState`、`EditorApplication.isPlaying`、`Time.timeScale`、退出码仍只在编排层（`Start`/`Update`/`Finish`）。

**已知取舍（供阶段 E 参考）**：断言体仍读取 runner 的**观测字段**（`_hitEvents`、`_moveSamples`、`_stateTimeline`、`_idleSamples`、`_animFixture*`、`_attackWindowStart` 等）与**测点快照**（`_playerAgentOk/_playerAgentWhy/_monsterBrainDisabled/_prevSwordTimer/_targetDeathAt`）。这些是“测量结果”，不是引导状态；AC-10 表只禁止读引导字段与在断言内现取当前场景对象。若阶段 E 要把断言搬出 runner，这些字段需随夹具结构一起打包。

## C.9 目检项（≤3，供所有者）

1. 打开 `Docs/QA/Evidence/QA-REFACTOR-01/FND00B06/diff_EvidenceShape.txt`，确认 Summary 第 1 键是 `run`、第 3 键是 `qaItems`，且 `06_ExitCode_run1.txt` 存在、内容 `0`。
2. `git diff -- Assets/_Project/Tests/FND00B/Runtime/FND00B06_Evidence.cs` 目视确认适配器只剩 spec 组装（33 行、无 I/O）；`FND00B06_Boot` 仍在 `FND00B06_BatchAcceptance.cs`（本阶段未动该文件）。
3. Unity Console 只应有 `[FND00B06][ASSERT]` 日志、无 `error CS`；`Docs/QA/Evidence/FND-00B-06/editor_run6/` 为本次只追加产物（`editor_run5` 未被覆盖）。

## C.10 建议提交（阶段 C）

```text
test(qa): FND00B06 套件迁移到 Qa 公共层（阶段 C，断言零变化）

为什么：FND00B06 的 14 条断言原本内联在 GroupAttack/GroupAnimation/两个夹具协程里，
拆成 AssertXxx(IQSink sink, …) 后断言只经 IQaSink 出证据、ExitCode 路径归还 QaEvidence，
并用“三重集 + expected 逐字符 + 证据形状”三份机检 diff 证明断言口径与证据契约没动。
```

（由所有者用 GitHub Desktop 提交；本执行者未做任何 git 写操作。）

## C.11 消耗记录（阶段 C）

- 钱包：`Go 订阅`
- 模型完整 ID：`opencode-go/deepseek-v4.1-flash`
- 轮次：1 轮实现 + 1 轮修复（1 处类型名拼写 `IQSink`→`IQaSink` 共 14 处、1 处脚本 UTF-8 BOM 导致的解析失败；均未触及断言口径）；套件运行 1 次（FND00B06，`editor_run6`）。

---

## D. 阶段 D —— DIAG 套件迁移（6 pass / 1 fail，已知红保持）

> 本节只覆盖阶段 D。阶段 A 见 §0–§4，阶段 B/C 见各自后缀节（`B.*` / `C.*`）；**阶段 E 仍未做**（`QaLauncher`、四套件统一 Boot、`QaTestTemplate.md`、`Docs/06`、`Docs/01 §14.1`）。

### D.1 结果摘要

**阶段 D 完成**。

一句话：`DIAG_Evidence` 变成 34 行适配器（`class DIAG_Evidence : QaEvidence`，构造函数 `(int run, string mode)` 不变、无 I/O），`DIAG_ProbeRunner` 的 7 条 `Assert*` 全部改成 `IEnumerator AssertXxx(IQSink sink, …)` 且删掉本地 `Dict`/`V3`/`GetHead`，`DIAG_BatchAcceptance` 的 `GitHead` 改用 `QaScene.GetHead()`；编译绿、DIAG 菜单跑 1 次得 **6 pass / 1 fail / 7 行 / 退出码 2**，唯一红仍是已知的 `Diag.Input.FirstExit`（`status=product_fail`），三份机检 diff 全部 `IDENTICAL`。

### D.2 AC 回填（本阶段部分）

| AC | 结果 | 断言 ID | 关键数据 | 日志路径 |
|---|---|---|---|---|
| **AC-1**（DIAG 部分） | **通过** | 7 条 | before 7 行 / pass=6 fail=1；after 7 行 / pass=6 fail=1；`(qaItem, assertion, status)` 排序后 **DIFF 为空**；7 条 `expected` 逐字符 **IDENTICAL** | `Docs/QA/Evidence/QA-REFACTOR-01/DIAG/diff_AssertionIds.txt`、`.../DIAG/diff_Expected.txt` |
| **AC-3**（DIAG 部分） | **通过** | — | 文件名集合 4=4（`DIAG_Assertions.jsonl` / `DIAG_Summary.json` / `DIAG_ExitCode.txt` / `InEditorResult.txt`）；**两侧都无 Runs 索引**；Summary **16 键逐键同序**（`task…exitReason`，`head` 第 8、`assertionFile` 第 9、`conclusion` 第 15）；断言行首行 20 个键的 `"key"` 出现次序一致；4 个文件全 `BOM=False`；`ExitCode` 内容 `2=2`；`conclusion` 值逐字符一致 | `Docs/QA/Evidence/QA-REFACTOR-01/DIAG/diff_EvidenceShape.txt` |
| **AC-10**（DIAG 部分） | **通过** | — | `Assert\w+\(IQaSink sink` = **7**；每条 `Assert*` 体内 `File.\|Directory.\|SessionState\|EditorApplication\|QaLauncher\|_Boot` = **0**、`_ev.` = **0**、每条都有 `sink.Record/BlockedAssertion/InfraFailure`；`DIAG_Evidence.cs` **34 行 ≤ 40**、I/O token = 0、无本地 `WriteSummary/Record/BuildLine`；`QaSink.cs` 不变、`class QaEvidence : IQaSink` 仍 = 1；`Tests/DIAG/**` 内 `"Docs", "QA", "Evidence"` 字面量 = 0 | `Docs/QA/Evidence/QA-REFACTOR-01/utp_readiness.txt`（`## PHASE D`，`ALL APPLICABLE CHECKS PASS`） |
| **AC-4**（本阶段快照，供阶段 E 汇总） | 通过（局部） | — | 代码侧只有 4 个文件：`Tests/DIAG/Runtime/DIAG_Evidence.cs`、`Tests/DIAG/Runtime/DIAG_ProbeRunner.cs`、`Tests/DIAG/Editor/DIAG_BatchAcceptance.cs`、`Tests/QA/Runtime/QaEvidence.cs`（纯增量）；`Assets/_Project/Scripts/**` 零改动；Docs 侧只有本 REPORT、`Docs/QA/Evidence/QA-REFACTOR-01/**`；canonical 只新增 `Docs/QA/Evidence/DIAG-SKILL/editor_run6/`；所有者的未提交改动（`.gitattributes`/`.gitignore`/`.editorconfig`/`AGENTS.md`/`Docs/01`/`Docs/07`/`workflow-rules.md`/`Docs/QA/Evidence/{DOC-INFRA-01,OPS-01}`）未被触碰 | `Docs/QA/Evidence/QA-REFACTOR-01/compile_phaseD.txt` §4 |
| AC-2 | 未运行 | — | 阶段 E 才要求四套件各连跑 2 次 | — |
| AC-5/6/7/8/9 | 未运行 | — | 属阶段 E（模板、全仓去重计数、`QaLauncher`+L2、全仓编译结论、`Docs/06`） | — |

### D.3 改动清单

| 文件 | 现行数 | `git diff --numstat`（vs `e3ad251c`） | 关键改动 |
|---|---|---|---|
| `Assets/_Project/Tests/DIAG/Runtime/DIAG_Evidence.cs` | **34** | +27 −128 | 全量换成适配器：只组装 `QaEvidenceSpec`；`Task="DIAG-SKILL"`、`LogTag="DIAG"`、`EvidenceFolder="DIAG-SKILL"`、`DIAG_Assertions.jsonl` / 固定名 `DIAG_Summary.json`、固定名 `DIAG_ExitCode.txt`、`RunIndexFileName=null`、`ShouldTruncateAssertions=(r,m)=>true`、16 键 `SummaryFields` |
| `Assets/_Project/Tests/DIAG/Runtime/DIAG_ProbeRunner.cs` | **543** | +124 −143 | 7 条断言改 `IEnumerator AssertXxx(IQSink sink[, ProbeState probe])`；新增私有 `ProbeState` 测点对象；删本地 `Dict`/`V3`/`GetHead`（→ `QaJson.Dict`/`QaJson.V3`/`QaScene.GetHead("DIAG.Head")`）；场景等待与控制器轮询改 `QaWait.WaitUntil`；`ScenePath` 常量改指 `QaScene.ScenePath`；`Finish()` 改调 `QaEvidence.WriteSummary(..., extra, extraTail)` |
| `Assets/_Project/Tests/DIAG/Editor/DIAG_BatchAcceptance.cs` | **207** | +1 −28 | 删本地 `GitHead()`，改 `QaScene.GetHead()`；删无用 `using System.IO;`；**`DIAG_Boot` 完整保留**（阶段 E 才统一） |
| `Assets/_Project/Tests/QA/Runtime/QaEvidence.cs` | **391** | +9 −4 | **纯增量**：新增 `QaSummaryKey.ExtraTail` + `WriteSummary(..., Dictionary<string,object> extraTail = null)` 的第二展开槽（其余三套件不传该参数，行为不变） |
| `Docs/QA/Evidence/QA-REFACTOR-01/DIAG/{compare,expected,shape}.ps1` | — | 新增 | 三份 diff 的可复跑脚本 |
| `Docs/QA/Evidence/QA-REFACTOR-01/DIAG/{diff_AssertionIds,diff_Expected,diff_EvidenceShape}.txt` | — | 新增 | 三份机检 diff |
| `Docs/QA/Evidence/QA-REFACTOR-01/DIAG/after/`（4 文件） | — | 新增 | `DIAG-SKILL/editor_run6/` 的复制快照 |
| `Docs/QA/Evidence/QA-REFACTOR-01/DIAG/console_phaseD.txt` | — | 新增 | 本次运行的 Console 摘录（`CONCLUSION` / `SUMMARY` / `finish` / `INEDITOR`） |
| `Docs/QA/Evidence/QA-REFACTOR-01/{utp_readiness.txt,utp_readiness_phaseD.ps1}` | — | 追加 / 新增 | `## PHASE D` 段（D1–D6） |
| `Docs/QA/Evidence/QA-REFACTOR-01/{compile_phaseD.txt,editor_state_phaseD.txt}` | — | 新增 | 编译与模式 B 四段 state |
| `Docs/QA/Evidence/DIAG-SKILL/editor_run6/` | — | 新增（canonical，只追加） | 菜单自动递增产物 |

- 未改：菜单文本、证据子目录递增规则、`*_Runner` GameObject 名、`SessionState` 键（`DIAG.*`）、命令行入口 `DIAG_BatchAcceptance.Run`、`DIAG_Boot`。
- 行尾：三个被重写的 DIAG 文件在工作树是 LF（仓库本来就两种并存：`FND00B06_Evidence.cs` 为 LF，`REFACTOR_Evidence.cs`/`QaEvidence.cs` 为 CRLF）。`git diff --numstat` 显示并非整文件噪声，纯属换行符，无逻辑影响。

### D.4 实现原理（含 5 个 `文件:行`）

1. `Assets/_Project/Tests/DIAG/Runtime/DIAG_Evidence.cs:7-34` —— 适配器只做 spec 组装。证据契约按"现状"逐项保留：目录 `DIAG-SKILL/[QA_EVIDENCE_SUBDIR]`、断言文件名固定、Summary 固定名（无 run 后缀）、ExitCode 固定名、**不生成 Runs 索引**、**构造即覆盖断言文件**（`ShouldTruncateAssertions = (r, m) => true`）。
2. `Assets/_Project/Tests/QA/Runtime/QaEvidence.cs:28` + `:231-246` —— 新增 `QaSummaryKey.ExtraTail` 与 `WriteSummary(..., extraTail = null)`：DIAG 的 Summary 需要把 `head` 放在第 8 位、`conclusion` 放在第 15 位（两个**不相邻**的私有键块），原有单 `Extra` 槽只能展开一次，故加第二个可选槽。为纯增量，其余三套件不传 → 键序不变。
3. `Assets/_Project/Tests/DIAG/Runtime/DIAG_ProbeRunner.cs:39-47` + `:129` —— 断言间共享的测点（控制器/指示器数组、`controllerCount`、`qPass/wPass/emptyPass`）收进私有 `ProbeState` 实例并由 `Start` 创建后**显式传参**；runner 上的同类可变字段全部删除，断言不再读写 runner 字段。
4. `Assets/_Project/Tests/DIAG/Runtime/DIAG_ProbeRunner.cs:72` + `:131-135` + `:492-495` —— 场景就绪与控制器轮询改 `QaWait.WaitUntil(..., timeout)`（超时改由 `Time.deltaTime` 累加，正常 `timeScale=1` 下与原 `unscaledDeltaTime` 口径一致，仍为 25s/3s 上限），实测耗时改用 `Time.realtimeSinceStartup` 差值；收尾把 `head`/`conclusion` 分别经 `QaJson.Dict` 传给 `WriteSummary` 的 `extra`/`extraTail`。
5. `Assets/_Project/Tests/DIAG/Editor/DIAG_BatchAcceptance.cs:27` —— `GitHead()`（本地 25 行进程调用）改为 `QaScene.GetHead()`（同样跑 `git rev-parse HEAD`，失败返回 `"unknown"`），`SessionState.SetString("DIAG.Head", head)` 与下游 `QaScene.GetHead("DIAG.Head")` 的语义完全不变。

### D.5 证据锚点

| 路径 | 内容 | 结论 |
|---|---|---|
| `Docs/QA/Evidence/QA-REFACTOR-01/DIAG/before/` | 阶段 A 采集的迁移前基线（4 文件，只读） | 本轮未改 |
| `Docs/QA/Evidence/QA-REFACTOR-01/DIAG/after/` | `DIAG-SKILL/editor_run6/` 的复制快照（4 文件） | 本轮产物 |
| `.../DIAG/diff_AssertionIds.txt` | 三重集排序比对 | `BEFORE count=7 pass=6 fail=1` / `AFTER count=7 pass=6 fail=1` / `RESULT: IDENTICAL` |
| `.../DIAG/diff_Expected.txt` | 7 条 `expected` 逐字符比对 | `RESULT: IDENTICAL (7 assertions, char-by-char)` |
| `.../DIAG/diff_EvidenceShape.txt` | 文件名集合 / Runs 索引缺席 / Summary 键序 / 断言行字段序 / BOM / ExitCode / Summary 常量值 | 全项 `IDENTICAL` |
| `.../DIAG/console_phaseD.txt` | 本次运行 Console 摘录 | `finish; run=1; exit=2; pass=6; fail=1` |
| `.../utp_readiness.txt` | `## PHASE D`（D1–D6） | `ALL APPLICABLE CHECKS PASS` |
| `.../compile_phaseD.txt` | `error CS`=0、四套件 sink 断言计数、`git status --porcelain` | 编译绿、范围干净 |
| `.../editor_state_phaseD.txt` | 模式 B 四段 `editor_state` | 未关编辑器、无 batchmode |
| `Docs/QA/Evidence/DIAG-SKILL/editor_run6/` | canonical（菜单自动递增，只追加） | `editor_run1..5` 未被覆盖 |

**运行次数**：本轮 DIAG 跑 **1 次**（阶段 D 要求）；整单累计 7 次（基线 4 + 阶段 A 2 + 阶段 C 1，本轮 1）≤ 20。三套件本轮**未重跑**（只验证"仍能编译"）。

### D.6 已知红保持的证据

- `Diag.Input.FirstExit`：`pass=false`、`status=product_fail`、`kind=production`；`expected={"expected":"MISSING_CONTROLLER","note":"按 Q 首个失效节点"}` 与 before **逐字符一致**（`diff_Expected.txt` 的该行未出现在 DIFF 段）；`actual.firstExit="NO_PRIMARY"`（与迁移前同因）。
- 7 行里唯一红仍是它：Summary `pass=6 fail=1 blocked=0 infraFail=0 observed=0`，`InEditorResult.txt` `code=2`，`DIAG_ExitCode.txt` 内容 `2`（与 before `2` 一致）。
- **未做任何"顺手修绿"**：没改 `expected`、没改判定表达式（`bool pass = firstExit == "MISSING_CONTROLLER";` 原样搬入），没删该断言。

### D.7 未合并项及原因（去重纪律）

| 本地实现 / 位置 | 处置 | 原因（等价性判断） |
|---|---|---|
| `Dictionary<string,object> Dict(params object[])` | **删除**，全改 `QaJson.Dict(` | 与 `QaJson.Dict` 逐行等价（按 `kvs` 插入序建字典） |
| `Dictionary<string,object> V3(Vector3)` | **删除**，全改 `QaJson.V3(` | 与 `QaJson.V3` 等价（`{x,y,z}` 同序） |
| `static string GetHead()` | **删除**，改 `QaScene.GetHead("DIAG.Head")` | 与 `QaScene.GetHead(sessionKey)` 的 `SessionState.GetString(key,"unknown")` 分支等价 |
| `const string ScenePath` | **删除**，改 `QaScene.ScenePath` | 字面量完全相同（`Assets/Scenes/DungeonWorld.unity`） |
| `IEnumerator AssertPrefabSlots` 内的 `AssetDatabase.LoadAssetAtPath` | **留在断言内** | Qa 层无资产加载封装；它是**只读**取预制体（`#if UNITY_EDITOR` 内），AC-10 禁止 token 不含 `AssetDatabase`。阶段 E 若要搬，应连同"只读资产加载"一起进 Qa 层 |
| `static List<object> NamesOf(System.Array)` | **留在套件内** | `QaFixtures` 无等价物（`QaFixtures.StateName` 是动画状态名，不是对象名列表）；且它只服务 DIAG 的两条场景断言 |
| `IEnumerator RunSafe(string, IEnumerator)` | **留在套件内（编排层）** | 属编排（异常→`InfraFailure` 兜底），不是断言体；阶段 E 统一 Boot 时可考虑上移 |
| `yield return new WaitForSecondsRealtime(1.0f);` | **保留未改** | 与 `QaWait.WaitSeconds` **不等价**（后者按 `Time.deltaTime` 累加，`timeScale=0` 时不会推进；此处是"进场景后静置 1 秒"的同步点，保持原实时语义）。与阶段 B/C 的取舍一致 |
| `QaEvidence` 的 Summary 槽 | **加 `ExtraTail`（设计缺口修复）** | `QaEvidenceSpec` 原有的单个 `Extra` 槽表达不了 DIAG"两个不相邻私有键块"的键序；若阶段 E 判定应改为 spec 字段（如 `HeadField`），可再收，不影响断言与证据形状 |

### D.8 受影响模块导航

- `Assets/_Project/Tests/DIAG/Runtime/DIAG_Evidence.cs` —— 只剩"证据契约"（目录/文件名/键序/覆盖策略）。要看 DIAG 证据长什么样，看这一个文件。
- `Assets/_Project/Tests/DIAG/Runtime/DIAG_ProbeRunner.cs` —— 编排（`Start` / `Update` 看门狗 / `Finish`）+ 7 条断言 + `ProbeState`。断言只经 `IQaSink` 出证据，落盘在 `QaEvidence`。
- `Assets/_Project/Tests/DIAG/Editor/DIAG_BatchAcceptance.cs` —— batch/菜单入口 + `DIAG_Boot`（域重载接续、看门狗）。阶段 E 会把 `DIAG_Boot` 并入 `QaLauncher`。
- `Assets/_Project/Tests/QA/Runtime/QaEvidence.cs` —— 四套件共享的落盘/计分/Summary/Runs；本轮仅新增 `ExtraTail` 槽。
- 依赖方向：`DIAG/** → QA/**`（单向）；DIAG 不再引用 `SPEC01_Json`（阶段 A 已改名 `QaJson`），`SkillCastFixture` 已属 `QA/Runtime`。

### D.9 三句话版本（TASK §5）

1. DIAG 的证据契约被压成 34 行适配器，断言行/Summary/ExitCode 的落盘与计分全部归 `QaEvidence`，所以"改套件"不再需要先读 300 行样板。
2. 7 条断言改成 `AssertXxx(IQSink sink, …)` 并把测点收进 `ProbeState` 显式传参，断言不再认识 runner 字段、`SessionState` 或文件系统，M7 可以直接用 `[UnityTest]` 包一条跑一条。
3. 断言口径没动的证据不是"我看着一样"，而是三份机检：三重集 DIFF 为空、7 条 `expected` 逐字符一致、证据形状（文件名/键序/字段序/无 Runs/BOM）逐项一致，且已知红 `Diag.Input.FirstExit` 仍是 `product_fail`。

### D.10 仍直写文件 / 仍依赖引导的断言（AC-10 ⑤）

**无**。DIAG 的 7 条断言全部满足：首参 `IQaSink sink`（其中 6 条另带 `ProbeState probe` 测点参数），体内只经 `sink.Record` / `sink.BlockedAssertion` / `sink.InfraFailure`；逐方法花括号扫描 `File.` / `Directory.` / `SessionState` / `EditorApplication` / `QaLauncher` / `_Boot` = 0，`_ev.` = 0。`SessionState`、`EditorApplication.isPlaying`、`File.WriteAllText(ExitCodePath)`、`Time.timeScale` 仍只在编排层（`Start` / `Update` / `Finish`）与 `DIAG_Boot`。

**已知取舍**：`AssertPrefabSlots` 用 `AssetDatabase.LoadAssetAtPath` 只读取预制体（编辑器内），属"读资产"而非"写文件"；`AssertFirstExit` 读生产 `SelectionManager.instance`（与迁移前一致，它测的就是这条早退链）。

### D.11 目检项（≤3，供所有者）

1. 打开 `Docs/QA/Evidence/QA-REFACTOR-01/DIAG/diff_EvidenceShape.txt`：确认 Summary 16 键依次为 `task, mode, run, …, scene, head, assertionFile, pass, … observed, conclusion, exitReason`，且"`RUNS INDEX: (absent both sides) IDENTICAL`"。
2. `git diff -- Assets/_Project/Tests/DIAG/Runtime/DIAG_Evidence.cs` 目视确认适配器只有 spec 组装、无 I/O；`DIAG_Boot` 仍在 `Assets/_Project/Tests/DIAG/Editor/DIAG_BatchAcceptance.cs:51`。
3. Unity Console 只应有 `[DIAG][ASSERT]` 日志、无 `error CS`；`Docs/QA/Evidence/DIAG-SKILL/editor_run6/` 是本次新增（`editor_run1..5` 未被覆盖）。

### D.12 建议提交（阶段 D）

```text
test(qa): DIAG 套件迁移到 Qa 公共层（阶段 D，断言零变化）

为什么：DIAG_Evidence 从 131 行降到 34 行、只保留证据契约，7 条断言改成
AssertXxx(IQSink sink, …) 并把测点显式传参；"断言没被动过"由三重集 DIFF 为空、
7 条 expected 逐字符一致、证据形状逐项一致三份机检证明，已知红 Diag.Input.FirstExit
仍是 product_fail 未修。
```

（由所有者用 GitHub Desktop 提交；本执行者未做任何 git 写操作。）

### D.13 消耗记录（阶段 D）

- 钱包：`Go 订阅`
- 模型完整 ID：`opencode-go/deepseek-v4.1-flash`
- 轮次：1 轮实现 + 1 轮修复（修复内容：① 7 处断言签名误写成 `IQSink` → `IQaSink`，与阶段 C 同类笔误；② `DIAG_BatchAcceptance` 删 `GitHead` 时误删类闭合大括号，已补齐；③ `utp_readiness_phaseD.ps1` 把适配器行数期望从 31 改为实测 34）。均未触及断言口径。
- 套件运行：DIAG 1 次（`editor_run6`）。

# 阶段 E — 统一 Launcher/Boot + 补齐断言签名 + 两份文档 + 四套件终验（执行者会话，2026-11-06）

> 任务书：`Docs/Handoff/TASK_QA-REFACTOR-01.md`（§2.3 / §2.4 / §2.5 阶段 E 行 / §3 AC-1..AC-10 / §4）
> 开工 HEAD = `5df13b1a`（阶段 D 提交），模式 B（MCP 常开编辑器，单实例 `Dungeon@4f5c34c82f31646e`），测试只经 `Tools/QA/In-Editor/*` 菜单，未使用 `run_tests`。

## E.0 开工前六问（本阶段）

| # | 问题 | 回答 |
|---|---|---|
| 1 | 唯一主要结果 | 四个守卫套件的引导状态机合并为单一 `QaLauncher`（删 4 个 `*_Boot`），SPEC01 剩余断言全部改为 `Assert*(IQaSink sink, …)`，补 `Tests/QaTestTemplate.md`、`Docs/06_测试架构与验收分层.md`、`Docs/01` §14.1 一行指针，四套件各连跑 2 次全绿 |
| 2 | 明确不做什么 | 不改任何断言的 ID / `qaItem` / `expected` / 阈值 / 通过条件 / 条数 / `status`；不碰 `Scripts/**`、资产、`ProjectSettings`、`Packages`、`CURRENT.md`、其他 `TASK_*`/`REPORT_*`、既有 canonical 证据；不做任何 git 写操作；不建 asmdef |
| 3 | 允许改哪些文件 | `Assets/_Project/Tests/**`、本 REPORT、`Docs/QA/Evidence/QA-REFACTOR-01/**`、新增 `Docs/06_*.md`、`Docs/01` §14.1 一行 |
| 4 | 必须保持哪些旧行为 | AC-7 的 8 条引导语义；`SessionState` 键（`<Suite>.Active/Started/Finished/ExitCode/RunnerName/PlayStartedAt`）；runner 名（`<Suite>_Runner`）；日志前缀；`-executeMethod *.Run` 入口名；菜单文本与顺序；`editor_runN` 递增；`runInBackground` 记录/还原；preflight 120s 超时；证据文件名 / Summary 键序 / 断言行字段序 |
| 5 | 如何证明完成 | 8 次运行的后备计数（Summary + JSONL）+ AC-1 的 phase-E 复比对 + AC-6/AC-10 静态检查（含命令与期望值）+ AC-4 原始 git 输出 + AC-8 编译时间戳与静态字段清单 |
| 6 | 失败后在哪里停 | 工单 §4 熔断七条；本轮唯一“未达成”的是 L2 域重载注入不可执行，按要求记录而不伪造（见 E.2 与 `l2_domainreload/README.txt`） |

## E.1 结果摘要（阶段 E + 整单终态）

**阶段 E 交付完成。AC-1/2/3/4/5/6/8/9/10 全部满足；AC-7 的 8 条语义等价逐条回填并有结构校验，L2「Play 中触发域重载」在本机不可执行（3 次注入均被 Unity 延迟到退出 Play 后编译），已留现场与原因。**

- 四套件终验（各连续 2 次，同一入口，中间未改代码）：
  - SPEC01：`12 pass / 0 fail`，`code=0`（run1=`editor_run43`，run2=`editor_run44`）
  - REFACTOR：`16 / 0`，`code=0`（`editor_run19` / `editor_run20`）
  - FND00B06：`12 / 0` + `observed 2`（14 行），`code=0`（`editor_run7` / `editor_run8`）
  - DIAG：`6 / 1`（唯一红 `Diag.Input.FirstExit`，`status=product_fail`），`code=2`（`editor_run10` / `editor_run11`）
- 断言口径：阶段 A/D 的 `after` 与阶段 E `final/run1` 逐行比对，四套件 `(qaItem, assertion, status, pass)` 多重集与 `expected` 字面量**全部 IDENTICAL**（`final/ac1_recheck.txt`）。
- 代码规模：范围内总行数 `8209`（阶段 A 基线）→ `8929`（阶段 D）→ **`8582`（阶段 E）**；删 4 个 `*_Boot` 类（约 600 行）+ 新增 `QaLauncher.cs`（324 行）。
- 静态检查：`class *_Boot`=0、`class *_Json`=0、`PlayerProbe`=1、`HitCollector`=1、`Dict(`=1、`WaitState(`=1、`GetHead(`=1、证据根字面量只在两处；四套件 `Assert*(IQaSink sink)`=10/16/14/7，断言体内违禁 token=0。
- 编译：`error CS` = 0；两个程序集重建时间均晚于各自最新源文件（`compile.log`）。

## E.2 AC 回填（整单视角，阶段 E 终态）

| AC | 判定 | 证据 / 实测 |
|---|---|---|
| **AC-1** 断言集合与 `expected` 逐条一致 | **PASS** | `final/ac1_recheck.txt`：SPEC01 12 行、REFACTOR 16 行、FND00B06 14 行、DIAG 7 行；`tuple multiset: IDENTICAL` ×4、`expected literals: IDENTICAL` ×4（对照阶段 A/D 的 `<suite>/after/*_Assertions.jsonl` 与 `final/run1/*`）。阶段 A–D 的 `diff_AssertionIds.txt` / `diff_Expected.txt` 未被覆盖 |
| **AC-2** 四套件各连跑 2 次 | **PASS** | `final/run1|run2/<suite>/`（每次含 Summary + JSONL + ExitCode + InEditorResult + `counts.txt`）：SPEC01 12/0、12/0；REFACTOR 16/0、16/0；FND00B06 12/0+observed2、12/0+observed2；DIAG 6/1、6/1（红项均为 `Diag.Input.FirstExit`，`product_fail`） |
| **AC-3** 证据形状（文件名/键序/字段序） | **PASS（未回退）** | 阶段 E 未改 `QaEvidenceSpec`/`QaEvidence`/`QA_InEditorMenu` 的文件名与键序逻辑；`final/*/<suite>/` 文件集合与阶段 A–D 的 `<suite>/after/` 一致（例：SPEC01 = InEditorResult + Assertions + ExitCode_run1 + Runs + Summary_run1）；SPEC01 Summary 仍含重复 `mode` 键（`final/run1/SPEC01/counts.txt` 的逐键输出） |
| **AC-4** 改动范围 | **PASS（所有者既有改动已逐项区分）** | `git_scope.txt`：`git diff --stat -- Assets/_Project/Scripts` = 空；代码侧改动仅 `Assets/_Project/Tests/**`（6 个 `.cs`）；`Docs/01` numstat = `5 0`，其中**本单 = 1 行**（hunk `@@ -511,0 +512 @@`，§14.1 指针）、另 4 行是所有者 `DOC-INFRA-01` 的既有未提交改动（hunk `@@ -520,0 +522,4 @@`，§14.4），本单未触碰；新证据只追加（`editor_run7/8/9/10/11`、`editor_run19/20`、`editor_run43/44` 均为新增目录）；`Assembly-CSharp*.csproj` 为 Unity 自动生成、未提交 |
| **AC-5** `QaTestTemplate.md` | **PASS** | `Assets/_Project/Tests/QaTestTemplate.md`：完整可复制示例（适配器 → `IQaSink` 断言 `Qa.Template.Smoke` → runner 收尾 → 注册表 + MenuItem），API 全部照抄现状（`QaEvidence`/`QaEvidenceSpec`/`QaSummaryKey`/`QaFixtures.CreateTarget`/`QaWait.WaitUntil`/`QaJson.Dict`/`QaScene.ScenePath`）；含“证据落到哪（目录/文件名/字段序）”“断言 ID 命名约定”“新增套件清单”“禁止事项”；已注明示例不参与套件运行 |
| **AC-6** 重复实现份数归零 | **PASS** | `dedup_counts.txt`：`class \w+_Boot\b`=0、`class \w+_Json\b`=0、`class PlayerProbe`=1、`class HitCollector`=1、`static Dictionary<string, object> Dict(`=1、`IEnumerator WaitState(`=1、`static string GetHead(`=1、证据根字面量=2（`QaEvidence.cs` + `QA_InEditorMenu.cs`）、四适配器 32/38/33/34 行且 `File.|Directory.`=0；行数 `8209→8929→8582` 仅登记参考 |
| **AC-7** Launcher 语义等价 + L2 | **PASS（结构）/ 未触发（L2）** | ①–⑧ 逐条见 E.2.1；结构校验见 `utp_readiness.txt` 的 `E5a..E5m`；L2 见 `l2_domainreload/`（3 次注入、均未在 Play 中触发域重载、三次运行 `code=2` 正常收场、`netzero_diff.txt` 证明被触碰脚本与 HEAD blob sha 一致） |
| **AC-8** 编译零错误 + 无新增静态可变全局 | **PASS** | `compile.log`：Console `error CS`=0、Editor.log 末 4000 行 `error CS`=0、12 个编译/重载标记；`Assembly-CSharp.dll`(17:12:34) > 最新 runtime 源(17:11:10)，`Assembly-CSharp-Editor.dll`(16:28:28) > 最新 editor 源(16:22:38)；静态字段清单与“−5 净减”对照见 E.2.2 |
| **AC-9** 分层文档六节 + §14.1 指针 | **PASS** | `Docs/06_测试架构与验收分层.md`：①分层 L0/L1/L2/功能套件（含 L2 三条注入法）②套件契约五件 + 注册式边界（明确“MenuItem 不能运行时注册，至少改注册表 + 转发方法两处”）③ID 规范 + 真实例子（`Skill.CastSmoke` / `Diag.Input.FirstExit` / `Party.PresentAndMove` / `Locomotion.AllPlayersMoveParam` / `Clock.MeleeNormalizedWindow` / `UI.SpawnAnchor`）+ 与 TASK AC 表对应关系 ④扩展规则（新功能新套件、行为变更留痕：不得静默改）⑤证据 schema 版本化（如实写明**现在还没有 `schemaVersion`**，当前为隐式 v1，并给出引入策略）⑥构建隔离（现状无 asmdef、asmdef 不能引用 `Assembly-CSharp`，M7 目标态与前置条件）；`Docs/01` §14.1 末追加 1 行指针（`git_scope.txt` hunk `@@ -511,0 +512 @@`） |
| **AC-10** 断言签名 UTP 就绪 | **PASS（SPEC01 以 10 方法覆盖 12 断言，见 E.2.3）** | `utp_readiness.txt` PHASE E：`Assert\w+\(IQaSink sink` = SPEC01 **10** / REFACTOR 16 / FND00B06 14 / DIAG 7；断言体内 `File.|Directory.|SessionState|EditorApplication|QaLauncher|_Boot`=0、`_ev.`=0（四套件）；`interface IQaSink` 唯一、成员恰 4 个、文件内违禁 token=0；`class QaEvidence : IQaSink`=1；E.10 如实写“无” |

### E.2.1 AC-7 ①–⑧ 逐条回填（`Assets/_Project/Tests/QA/Editor/QaLauncher.cs`）

| # | 旧行为（4 份 `*_Boot` 各一份） | 新实现 | 等价性说明 |
|---|---|---|---|
| ① `ClearStaleRunFlags` 防看门狗误判 | `:249`；`OnPlayModeChanged` 的 `EnteredPlayMode` 分支 `if (ClearStaleRunFlags()) return;`（`:233`） | 逐行等价：`Started` 未置位 → false；`RunnerAlive()` → false；否则清 `Started/Finished/Active`、`EraseString(RunnerName)`、`_runner=null`、`_missingSince=-1`，并打同格式 warning |
| ② 首启宽限 10s | `FirstStartGraceSeconds = 10f`（`:82`），`Tick` 中与 `PlayStartedAt` 比 | 常量名、数值、判定式与日志文案一致（`runner never created within 10s after entering play`） |
| ③ runner 丢失宽限 3s | `RunnerLostGraceSeconds = 3f`（`:79`） | 同上，`_missingSince` 首帧记录 + 倒计时 warning 文案一致 |
| ④ 看门狗落 `code=3 + reason=runner-lost-domain-reload`，编辑器内不杀 Unity | `WatchdogFail`（`:328`） | `LogError("[TAG][WATCHDOG] … -> code=3 reason=runner-lost-domain-reload")` → `TryFinish(spec.SuiteId, 3, "runner-lost-domain-reload")` 为真则 `EditorApplication.isPlaying=false` 并 return；否则 `EditorApplication.Exit(3)` |
| ⑤ `EnteredEditMode` + `Finished` → `ExitWithCode` | `:239` | 条件式与旧实现逐字一致（`change == EnteredEditMode && SessionState.GetBool(spec.FinishedKey, false)`） |
| ⑥ `TryFinish` 优先于 `EditorApplication.Exit` | `WatchdogFail` / `ExitWithCode`（`:343`） | 两处均先 `TryFinish` 再 `Exit`；`utp_readiness` 的 `E5i/E5j` 用方法体 `IndexOf` 校验先后 |
| ⑦ `RunnerAlive` = 静态引用 + `GameObject.Find(name)` | `:314` | `_runner != null` → true；否则按 `SessionState.GetString(RunnerNameKey)` 找；名字为空 → true（沿用“不误判”分支） |
| ⑧ runner `DontDestroyOnLoad` | `TryStartRunner`（`:360`，`DontDestroyOnLoad` 在 `:369`） | `new GameObject(spec.RunnerName)` → `DontDestroyOnLoad` → `AddComponent(spec.RunnerType)` → 记 `RunnerName`；名字与旧实现逐字相同 |

### E.2.2 AC-8 静态字段对照（净减 5）

| 旧 | 新 | 说明 |
|---|---|---|
| `SPEC01_Boot._runner` / `_missingSince` | `QaLauncher._runner` / `_missingSince` | 四个 `*_Boot` 各自 2 个可变静态 → 合并为共享 2 个（同一时刻只有一个套件 Active，语义等价） |
| `REFACTOR_Boot._runner` / `_missingSince` | 同上 | 同上 |
| `FND00B06_Boot._runner` / `_missingSince` | 同上 | 同上 |
| `DIAG_Boot._runner` / `_missingSince` | 同上 | 同上 |
| —（无） | `QaLauncher._current` | 新增 1 个：域重载后“本轮是哪个套件”的解析结果；替代旧的“每个类各自编译期绑定” |
| —（无） | `QaLauncher.Registry`（`static readonly`） | 任务要求的数据注册表，非可变状态 |

其余静态字段均为阶段 A–E 之前就存在：`QA_InEditorMenu` 的 preflight 四个字段、`QaEvidence.DefaultSummaryFields`、`QaFixtures.PlayerNames`、`QA_InEditorMenu.SuiteEnvKeys`、`SkillCastFixture._swordTimerField`（随文件移动，内容未改）。

### E.2.3 SPEC01 10 方法 / 12 断言说明

`AssertDualPathAndClocks(IQSink sink)` 一个协程内 emit 三条断言（`Attack.DualPathUnified` / `Clock.MeleeNormalizedWindow` / `Clock.RangedOneShotPerCycle`），因此 SPEC01 是 12 条断言行、10 个 `IEnumerator Assert*` 方法。AC-10 的口径是“已迁移套件的 `Assert*` 首参均为 `IQaSink`”，10/10 方法成立；若要求方法数也等于 12，需把该协程拆成 3 个（会移动判定代码块），本单按“只允许改签名”的约束未拆，已在此与 E.8 目检项说明。

## E.3 实现原理

**三句话版本**：①把四个 `*_Boot` 里逐字重复的“SessionState 标记 → EnteredPlayMode 建 runner → 看门狗 → EnteredEditMode 收场 → TryFinish/Exit 分叉”提升成唯一状态机 `QaLauncher`，套件差异收敛为一张 `QaLaunchSpec` 注册表（键前缀 / runner 类型与名字 / 证据目录 / 注入的环境变量 / 入口方法）；②`QA_InEditorMenu` 的 preflight、`editor_runN` 递增、`TryFinish`、`runInBackground` 原样保留，只把“按 task 字符串 switch”换成查注册表并调 `spec.Launch()`；③SPEC01 剩余 9 个断言方法统一加 `IQaSink sink` 首参、体内 `_ev.` → `sink.`，判定表达式一字未改。

关键位置：

- `Assets/_Project/Tests/QA/Editor/QaLauncher.cs:84` —— `static readonly QaLaunchSpec[] Registry`，四个套件的唯一事实源；`SPEC01/FND00B06/DIAG/REFACTOR` 属性按索引暴露给 `*_BatchAcceptance.Run()`。
- `Assets/_Project/Tests/QA/Editor/QaLauncher.cs:206` —— 唯一 `[InitializeOnLoadMethod] Boot()`：域重载后按 `ActiveSpec()` 重新布线 `playModeStateChanged` + `update`，并写回 `_current`。
- `Assets/_Project/Tests/QA/Editor/QaLauncher.cs:328` —— `WatchdogFail`：`code=3 + reason=runner-lost-domain-reload`、编辑器内只退 Play，`TryFinish` 优先于 `Exit`。
- `Assets/_Project/Tests/QA/Editor/QA_InEditorMenu.cs:242` / `:280` —— 菜单驱动改为 `QaLauncher.Find(task)` + `spec.Launch()`；`EvidenceRoot` 用 `spec.EvidenceFolderName()`（`:446`）。
- `Assets/_Project/Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs:193` / `:332` —— `AssertLocomotion(IQaSink sink)` 与 `sink.Record(...)`：签名迁移 + 证据出口改造，判定表达式与 `expected` 未动。

## E.4 受影响模块导航

| 模块 | 文件 | 变化性质 | 下次改动时先看什么 |
|---|---|---|---|
| 引导状态机 | `Tests/QA/Editor/QaLauncher.cs` | **新增**（`QaLaunchSpec` + `QaLauncher`） | E.2.1 的 8 条语义；改 `Begin`/`Tick`/`WatchdogFail` 会同时影响四套件 |
| 菜单驱动 | `Tests/QA/Editor/QA_InEditorMenu.cs` | 改（`Proceed`/`EvidenceRoot` 走注册表；私有 `RefactorMilestone()` 删除） | preflight / `editor_runN` / `TryFinish` / `runInBackground` 属“驱动逻辑”，新增套件不许动 |
| 四套件入口 | `Tests/{SPEC01,REFACTOR,DIAG}/Editor/*_BatchAcceptance.cs`、`Tests/FND00B/Editor/FND00B06_BatchAcceptance.cs` | 改（删 `*_Boot`，`Run()` 变装配 + `QaLauncher.Begin/Fail`） | `ScenePath` / `EnemyPrefabGuid` / `DefaultMilestone` 保留；`-executeMethod *.Run` 入口名不变 |
| SPEC01 断言 | `Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs` | 改（9 个方法签名 + 22 处 `_ev.`→`sink.`） | `AssertDualPathAndClocks` 承载 3 条断言（12 条断言 / 10 个方法，见 E.2.3） |
| 证据层 | `Tests/QA/Runtime/{QaSink,QaEvidence,QaJson}.cs` | **未改** | 文件名 / 键序 / 字段序是 AC-3 的判据，改前先看 TASK §2.4 |
| 新人模板 | `Tests/QaTestTemplate.md` | 新增 | 模板里的 API 必须与 `Tests/QA/**` 同步；示例 ID 不参与运行 |
| 分层文档 | `Docs/06_测试架构与验收分层.md` | 新增 | §6 构建隔离描述的是现状 + M7 目标，别写成已实现 |
| 架构索引 | `Docs/01_总体架构与内容生产规范.md` §14.1 | 加 1 行指针 | 其余章节一字未动 |

## E.5 证据清单（阶段 E 新增，均为新增文件或追加）

| 文件 | 内容 |
|---|---|
| `Docs/QA/Evidence/QA-REFACTOR-01/final/run1|run2/<suite>/` | 8 次运行的 Summary + JSONL + ExitCode + InEditorResult + `counts.txt`（后备计数） |
| `Docs/QA/Evidence/QA-REFACTOR-01/final/ac1_recheck.txt` | AC-1 phase-E 复比对（tuple 多重集 + `expected` 字面量） |
| `Docs/QA/Evidence/QA-REFACTOR-01/dedup_counts.txt`（+ `dedup_counts_phaseE.ps1`） | AC-6 每条 grep 命令 + 实际 + 期望 + 行数对照 |
| `Docs/QA/Evidence/QA-REFACTOR-01/utp_readiness.txt`（+ `utp_readiness_phaseE.ps1`） | AC-10 PHASE E + AC-7 结构校验（`E5a..E5m`、`E6a..E6f`） |
| `Docs/QA/Evidence/QA-REFACTOR-01/compile.log`（+ `collect_compile.ps1`） | AC-8：`error CS`=0、程序集时间戳、静态字段清单 |
| `Docs/QA/Evidence/QA-REFACTOR-01/git_scope.txt`（+ `collect_gitscope.ps1`） | AC-4：status / diff --stat / numstat / `-U0` hunk / Scripts 空 / 只追加的 `editor_runN` |
| `Docs/QA/Evidence/QA-REFACTOR-01/l2_domainreload/`（+ `collect_l2.ps1`） | AC-7 L2：`README.txt`（3 次尝试表 + 原因）、`l2_watch*.txt`（注入时间）、`InEditorResult_editor_run{7,8,9}.txt`、`console_excerpt.txt`、`netzero_diff.txt`（blob sha 相同） |
| `Docs/QA/Evidence/QA-REFACTOR-01/capture_final.ps1` | 8 次运行的快照与计数脚本（可重跑） |

## E.6 测试数据（8 次运行 + L2 + 静态检查数字）

| 套件 | run1 源目录 | run1 计数 | run2 源目录 | run2 计数 |
|---|---|---|---|---|
| SPEC01 | `SPEC-01/editor_run43` | 12 pass / 0 fail，`code=0` | `SPEC-01/editor_run44` | 12 / 0，`code=0` |
| REFACTOR | `REFACTOR-M4/editor_run19` | 16 / 0，`code=0` | `REFACTOR-M4/editor_run20` | 16 / 0，`code=0` |
| FND00B06 | `FND-00B-06/editor_run7` | 12 / 0 + observed 2（14 行），`code=0` | `FND-00B-06/editor_run8` | 12 / 0 + observed 2，`code=0` |
| DIAG | `DIAG-SKILL/editor_run10` | 6 / 1（`Diag.Input.FirstExit` = `product_fail`），`code=2` | `DIAG-SKILL/editor_run11` | 6 / 1，`code=2` |

L2：3 次注入（16:48:05.938 / 16:55:09.786 / 17:08:13.733），对应 `editor_run7/8/9` 均 `code=2` 正常收场，**未出现** `[DIAG][WATCHDOG]`、**未**在 Play 中发生域重载；原因：Unity 2022.3 在 Play 期间延迟脚本重编译（`Recompile After Finished Playing`），重编译全部发生在退出 Play 之后。改变该行为需要改编辑器偏好或 `ProjectSettings`（超范围）或 `execute_code`（禁用）。

静态检查数字：`Assert*(IQaSink sink)` = 10/16/14/7；断言体内违禁 token = 0；`class *_Boot` = 0；`class *_Json` = 0；适配器行数 32/38/33/34（≤40）；范围内总行数 8582。

## E.7 未决项（≤3 目检）

1. **L2 域重载未被真实触发**（`l2_domainreload/README.txt`）。若要在真实域重载下验证看门狗：把编辑器偏好 `Script Changes While Playing` 改为 `Recompile And Continue Playing`（所有者环境设置，本单未改）→ `Tools/QA/In-Editor/DIAG` 进 Play → 加注释保存 → 预期落 `code=3 reason=runner-lost-domain-reload`。
2. **SPEC01 断言方法数 10 vs 断言行数 12**（`AssertDualPathAndClocks` 承载 3 条）。若要求方法数也等于 12，需拆协程并移动判定代码块，超出“只改签名”的约束。
3. **`Docs/01` numstat 是 `5 0` 而非 `1 0`**：同文件还有所有者 `DOC-INFRA-01` 的 4 行未提交改动（§14.4）。本单贡献已用 hunk 分离（`@@ -511,0 +512 @@`）；所有者提交时请注意分开 stage。

## E.8 建议提交（阶段 E）

```text
test(qa): 统一四套件 Launcher/Boot、SPEC01 断言走 IQaSink、补分层文档（阶段 E）

为什么：四个守卫套件的引导状态机重复四份，M7 拆程序集时无法归属；本次合并为单一
QaLauncher（SessionState 键/runner 名/日志/退出分叉逐条等价），SPEC01 剩余断言统一
走 IQaSink，并补新人模板与分层文档；四套件各连跑 2 次，AC-1 复比对 IDENTICAL 证明
断言口径零变化，已知红 Diag.Input.FirstExit 仍是 product_fail。
```

建议提交范围（不含所有者 `DOC-INFRA-01` 的既有改动）：
- `Assets/_Project/Tests/QA/Editor/{QaLauncher.cs(+meta),QA_InEditorMenu.cs}`
- `Assets/_Project/Tests/{SPEC01,REFACTOR,DIAG}/Editor/*_BatchAcceptance.cs`、`Assets/_Project/Tests/FND00B/Editor/FND00B06_BatchAcceptance.cs`
- `Assets/_Project/Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs`
- `Assets/_Project/Tests/QaTestTemplate.md(+meta)`
- `Docs/01_总体架构与内容生产规范.md`（仅 §14.1 那 1 行；与 `DOC-INFRA-01` 的 4 行分开 stage）
- `Docs/06_测试架构与验收分层.md`（Docs/ 目录不在 Assets 下，无 `.meta`）
- `Docs/QA/Evidence/QA-REFACTOR-01/**`（含 `final/`、`l2_domainreload/`、`dedup_counts.txt`、`compile.log`、`git_scope.txt`、`utp_readiness*.ps1`、`capture_final.ps1`、`collect_*.ps1`）
- 新增 canonical 证据子目录：`SPEC-01/editor_run{43,44}`、`REFACTOR-M4/editor_run{19,20}`、`FND-00B-06/editor_run{7,8}`、`DIAG-SKILL/editor_run{7,8,9,10,11}`
- `Assembly-CSharp*.csproj` 为 Unity 自动生成，按规则**不提交**

（由所有者用 GitHub Desktop 提交；本执行者未做任何 git 写操作。）

## E.9 消耗记录（阶段 E）

- 钱包：`Go 订阅`
- 模型完整 ID：`opencode-go/deepseek-v4.1-flash`
- 轮次：1 轮实现 + 1 轮修复（修复内容：① `utp_readiness_phaseE.ps1` 三处期望值写成“只算代码”却按“含注释”计数，改为先剥注释；② `docs/01` diff 采集脚本因 PowerShell 以 ANSI 读脚本导致中文路径乱码，改用通配路径；③ 误把累积式 REPORT 整文件覆盖，已按 HEAD 字节还原并改为追加阶段 E 章节）。均未触及断言口径。
- 套件运行：11 次（DIAG×5 = L2 三次 + AC-2 两次；SPEC01/REFACTOR/FND00B06 各 2 次），上限 20。
- 编译：3 次有效重编译，无连续红。
- 时间：约 70 分钟。
- 熔断：未触发。

## E.10 仍直写文件 / 仍依赖引导的断言（AC-10 ⑤）

**无。** 四套件全部 `Assert*` 已满足：证据只经 `IQaSink`（`sink.Record` / `sink.BlockedAssertion` / `sink.InfraFailure`），夹具显式入参，断言体内 `File.` / `Directory.` / `SessionState` / `EditorApplication` / `QaLauncher` / `*_Boot` / `_ev.` 计数均为 0（`utp_readiness.txt` PHASE E 的 `E2/E3` 逐套件扫描）。

例外说明（不是违规，属编排层）：`Start()` / `Finish()` / `Update()`（看门狗）与 `RecordRegressionFinal()` 仍在 runner 类上使用 `_ev` 与 `SessionState` 写入 `Finished`/`ExitCode`，这是 TASK §2.3 明确允许的“编排层继续用 SessionState、写 ExitCode”。

