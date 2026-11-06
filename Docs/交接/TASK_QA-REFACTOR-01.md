# QA-REFACTOR-01 — 测试代码分层重构（Qa 公共层 + 四套件迁移，不改断言）

> 本文件自包含：执行者只读本文件即可干活。信息不足时在 REPORT 写“信息不足”+ 缺项清单，**不**自行翻别的文档补全。
> 规则来源：`AGENTS.md`（硬约束）、`Docs/Handoff/workflow-rules.md`（细则）。冲突时 AGENTS 胜。

## §0 元信息

| 字段 | 值 |
|---|---|
| 任务 ID | `QA-REFACTOR-01` |
| 风险等级 | `常规`（代码只改 `Assets/_Project/Tests/**`；文档只加 `Docs/06` 与 `Docs/01` §14.1 一行；不碰 `Scripts/**`、资产、场景、预制体、Animator、`.asset`、`ProjectSettings`、`Packages`） |
| 授权 | 所有者 2026-11-06 明确批准：分层只改测试代码、不改断言；每阶段一个执行者；大脑做本地 commit（不 push） |
| 基线提交 | `df7ea0b0`（开工时以 `git rev-parse --short HEAD` 复核；不符即停） |
| 回滚锚 | 阶段提交前一律 `git stash`/`git checkout -- <file>` 可回；整单回滚 = 回到 `df7ea0b0` |
| 分支 | 当前分支 `docs/fnd-00-baseline`（不新开分支：本单只动 Tests，五阶段连续提交，开短分支反而增加合并成本） |
| 状态 | `Done`（所有者 2026-11-06 验收，大脑代录） |
| 执行方式 | **五阶段串行**，每阶段一个独立 executor 会话、干完即弃；每阶段验收后由大脑本地 commit |

> **只有所有者能标 `Done`。** 执行者不得改 `CURRENT.md`、不得做任何 git 写操作、不得标 `Done`。

## §1 需求（Why / What）

**一句话目标**：把 `Assets/_Project/Tests/`（现 23 个 `.cs`，12,326 行）里重复了 4–7 份的 **证据写入 / JSON 序列化 / 夹具 / 等待 / 场景查询 / Boot 引导** 抽成 `Tests/QA/` 公共层，四套守卫（SPEC01 / REFACTOR / FND00B06 / DIAG）只保留**断言与编排**；**断言口径零变化**。

**用户故事**：作为项目所有者，我要在改产品代码时能看懂并维护守卫套件（新增一条断言 = 写一个 `Record`，而不是先抄 80 行样板），以便 M5/M6 重构不必每次先读懂 1,300 行探针脚手架。

**为什么现在做**：`Tests/` 已 12,326 行，其中 `DIAG_Evidence` 直接调用 `SPEC01_Json`、`DIAG`/`REFACTOR` 依赖 `SPEC01` 目录下的 `SkillCastFixture` —— 套件之间已经互相耦合，M7 拆程序集时这批代码无法归属。

**必须满足**

| 编号 | 需求 | 验收方式 |
|---|---|---|
| R-1 | 新增 Qa 公共层：`QaEvidence`（含 `QaEvidenceSpec` 描述符）、`QaJson`、`QaWait`、`QaFixtures`、`QaScene`、`QaLauncher` | 文件存在 + 各套件不再自行实现同类逻辑（AC-6 计数） |
| R-2 | 四套件迁移后**断言集合逐条一致**：ID、期望、阈值、通过条件、条数、pass/fail 分布不变 | AC-1 + AC-2 |
| R-3 | 证据产物**路径与文件名、Summary 键名与键序**与迁移前完全一致 | AC-3 |
| R-4 | 改动只在 `Assets/_Project/Tests/**`（+ 本单 `Docs/`），`Scripts/**` 零改动 | AC-4 |
| R-5 | 提供 `Tests/QaTestTemplate.md`：一条**可复制**的完整示例（夹具 + 断言 + 证据），新人照抄即可写新断言 | AC-5 |
| R-6 | 消除跨套件依赖：`DIAG → SPEC01_Json`、`DIAG/REFACTOR → SPEC01 目录下的 SkillCastFixture` 均改为只依赖 `Tests/QA/` | AC-6 |
| R-7 | 分层规则**落成文档**：`Docs/06_测试架构与验收分层.md` 六节齐备；`Docs/01` §14.1 加一行指针 | AC-9 |
| R-8 | **UTF 预留**：断言只经 `IQaSink` 出证据、签名不依赖 Boot/Launcher，M7 后可被 `[UnityTest]` 直接包装 | AC-10 |

**不在范围（明确不做）**

1. **不建 asmdef / 不拆程序集**（产品代码仍在 `Assembly-CSharp`，asmdef 程序集无法引用它；程序集拆分留 M7）。
2. **不改任何产品代码**（`Assets/_Project/Scripts/**` 只读）。
3. **不改断言的 ID、期望值、阈值、通过条件、`qaItem`、`status` 判定**；不新增、不删除、不合并断言。（**允许**改断言方法的**签名**：按 §2.3 约定加 `IQaSink sink` 首参与显式夹具参数——这是唯一允许签名变动的理由，且不得改变任何判定表达式。）
4. **不动资产**：场景、预制体、Animator Controller、`.asset`、`.meta`（唯一例外：`SkillCastFixture.cs` 连同它自己的 `.meta` 一起移动，GUID 不变）。
5. **不动** `Tests/FND00B/FND00B04*`、`Tests/FND00B/FND00B05*`、`Tests/FND02A/**`（历史套件，不在四套守卫内；保持原样、不改、不删）。
6. **不顺手**改命名空间、类名（`SPEC01_Evidence` 等类名保留，只是变成薄适配器）、不批量格式化无关文件、不加新断言。
7. **不改** `QA_InEditorMenu` 的菜单项文本、证据子目录递增规则（`editor_runN`）、`runInBackground` 还原逻辑。

**约束**

- 产品代码无 `Tests` 依赖（已核验：`Scripts/` 只有一处注释提到 `SPEC01`），因此 Tests → Scripts 单向引用合法，不需要 asmdef。
- 证据目录**只追加不覆盖**（`AGENTS.md` §4）：迁移不许删除、改写任何既有 `Docs/QA/Evidence/**`。
- `Assembly-CSharp*.csproj` 由 Unity 自动生成，**永不提交**；新增/删 `.cs` 会改它，属已知脏项。
- 不得新增静态全局状态（`AGENTS.md` §10 工程三性）：Qa 层只允许无状态的 `static` 纯函数 + 实例化对象（`QaEvidence`/`QaFixtures` 的使用者持有实例）。`QaLauncher` 的静态字段是既有 `*_Boot` 静态字段的等价迁移，不新增语义。

## §2 方案（Plan）

### 2.1 备选方案

| 方案 | 做法 | 优点 | 缺点 | 结论 |
|---|---|---|---|---|
| A | 一次性重写四套件（重排断言、统一命名、顺手建 asmdef） | 一步到位最干净 | 12k 行同时变，断言口径几乎必然被动到；无法用“迁移前后逐行比对”证伪；asmdef 无法引用 `Assembly-CSharp`，直接编译不过 | **弃用** |
| B | 只抽 `QaEvidence`+`QaJson`（收益最大的两块），夹具/等待/Boot 留在套件里 | 风险最低 | 只削掉约 900 行，`PlayerProbe`/`WaitState`/`CreateTarget` 仍各 2–3 份；跨套件依赖（DIAG→SPEC01）依然存在；M7 仍无法归属 | **弃用**：不解决 R-6 |
| C | **分层 + 五阶段串行迁移**：先建 Qa 层（零行为变化），再一套件一阶段搬，最后统一 Boot；每阶段跑该套件并用“断言 ID 多重集”比对前后 | 每阶段可独立编译、独立验收、独立回滚；断言口径靠机检保证；跨套件依赖逐阶段消失 | 阶段多、总耗时长（约 16 次套件运行） | **选定** |

**选定理由**：本单唯一真正的风险是“悄悄改了断言”，而阶段化 + 断言 ID 多重集比对是唯一能机检这条风险的做法；把 Boot 统一放到最后，是因为它一改就四个套件同时不可用，必须等前面全部绿了再动。

### 2.2 目标结构

```
Assets/_Project/Tests/
  QA/                                  ← 公共层（唯一共享层；见 2.3 命名说明）
    Runtime/
      QaJson.cs                        ← 极简 JSON 序列化（从 SPEC01_Json 原样迁移）
      QaSink.cs                        ← IQaSink：断言唯一的证据出口（R-8 / AC-10）
      QaEvidence.cs                    ← 证据落盘 + 计分 + Summary + Runs 索引 + QaEvidenceSpec（实现 IQaSink）
      QaWait.cs                        ← 协程等待/轮询（WaitState / WaitUntil / WaitFrames ...）
      QaFixtures.cs                    ← PlayerProbe / HitCollector / CreateTarget / Select / FindPlayers
      QaScene.cs                       ← 场景与仓库查询（GetHead / GetEnemySourceGuid / AbsPath / 场景常量）
      SkillCastFixture.cs              ← 从 Tests/SPEC01/Runtime/ 移入（连 .meta，类名不变）
    Editor/
      QaLauncher.cs                    ← 四个 *_Boot 合并：SessionState 引导 + 看门狗 + 退出分叉
    QA_InEditorMenu.cs                 ← 现有编辑器菜单驱动（只做必要适配）
  QaTestTemplate.md                    ← 新人模板（R-5）
  SPEC01/{Runtime,Editor}/             ← 只留 12 条断言 + 编排 + 薄适配器
  REFACTOR/{Runtime,Editor}/           ← 只留 16 条断言 + 编排 + 薄适配器
  FND00B/...FND00B06*                  ← 只留 14 条断言 + 编排 + 薄适配器（FND00B04/05 不动）
  DIAG/{Runtime,Editor}/               ← 只留 7 条断言 + 编排 + 薄适配器
```

**命名说明（与口述任务的唯一偏差，必须照此执行）**：口述写 `Tests/Qa/`，但仓库已有 `Assets/_Project/Tests/QA/`（存放 `QA_InEditorMenu.cs`）。Windows/NTFS 大小写不敏感，`Qa` 与 `QA` 是**同一个目录**，另建会退化成 git 大小写重命名（有丢 `.meta` 与 GUID 的风险）。故公共层落在现有 `Tests/QA/` 下，**不改目录大小写**。

### 2.3 类职责与硬性 API 契约

| 类 | 必须提供 | 必须不做 |
|---|---|---|
| `QaJson` | `static string Value(object)`，行为与现 `SPEC01_Json.Value` **逐字段一致**（含 `float` 的 `"R"` 格式、NaN/Inf→`null`、`Enum→字符串`、`IDictionary<string,object>`/`IEnumerable` 递归、控制字符转义） | 不引入第三方库；不改变数字格式（改了就破 AC-3） |
| `QaEvidenceSpec` | 描述一个套件的证据契约：`Task`、`EvidenceFolder`、断言文件名、`Summary` 文件名、`ExitCode` 文件名（可为 `null`=不生成）、`Runs` 索引文件名（可为 `null`=不生成）、`TagEnvVar`（可为 `null`） | 不含任何 I/O |
| `QaEvidence` | `EvidenceDir`/`AssertionPath`/`Prefix`/`Records`/`Pass`/`Fail`/`Blocked`/`Infra`/`Observed`；`Record(qaItem, assertion, expected, actual, pass, status, kind, data)`（9 参，签名不变）；`BlockedAssertion`；`InfraFailure`；`LineCount`；`WriteSummary(...)`；`ExitCodePath`；`RunIndexPath`；**实现 `IQaSink`** | 不硬编码任何套件名/目录名；`"Docs","QA","Evidence"` 字面量全仓只在此类出现一次 |
| `IQaSink`（新） | 断言方法**唯一的证据出口**，成员就是 `QaEvidence` 现有的记录面：`Record(9 参)`、`BlockedAssertion`、`InfraFailure`、`string EvidenceDir { get; }`。**不加**任何落盘/引导/计分成员 | 不得引用 `File`/`Directory`/`SessionState`/`EditorApplication`/`QaLauncher`（接口要做到 M7 能换成内存收集器或 `[UnityTest]` 的 NUnit 结果） |
| `QaWait` | 协程等待：`WaitState(Animator, stateName, timeout)`、`WaitUntil(Func<bool>, timeout)`、`WaitSeconds`、`WaitFrames`；全部以 `Time.deltaTime` 累加超时（与现实现同口径），超时**静默返回**（不抛异常、不断言） | 不做断言、不写证据 |
| `QaFixtures` | `PlayerProbe`（**超集**：合并现 SPEC01 版全部字段 + REFACTOR 版全部字段）、`HitCollector`、`CreateTarget(name, pos, bool showDamageNumber = true)`、`Select(PlayerProbe)`、`FindPlayers()` | 不持有跨调用静态状态；不销毁不属于自己创建的对象 |
| `QaScene` | `GetHead(string sessionKey = null)`：`sessionKey == null` 时真跑 `git rev-parse HEAD`（失败返回 `"unknown"`），否则读 `SessionState.GetString(sessionKey)`；`GetEnemySourceGuid(scenePath)`（只读 YAML 扫 `m_SourcePrefab`）、`AbsPath(assetPath)`、`ScenePath`/`EnemyPrefabGuid` 常量 | 不写场景、不保存场景 |
| `QaLauncher` | `Begin(QaLaunchSpec)`：写 SessionState 标记并 `EnterPlaymode`；`[InitializeOnLoadMethod]` 单一引导；Play 状态机 + 看门狗；退出分叉 `QA_InEditorMenu.TryFinish` 优先、batch 落 `EditorApplication.Exit(code)` | 不改菜单、不改证据子目录规则、不新增退出码 |

**每套件的 `*_Evidence.cs` 变成薄适配器**：保留原类名与构造函数签名，`class SPEC01_Evidence : QaEvidence`，构造函数只组装 `QaEvidenceSpec`。**适配器上限 40 行**（含注释与 using），不得含文件 I/O、JSON 拼装、计分逻辑。若发现无法在 40 行内表达 → 说明 `QaEvidenceSpec` 抽象不足，在 REPORT 写设计缺口，**不硬写**。

**去重纪律（本单最容易犯的错）**：只合并**可证明等价**的实现。若两处同名方法体实际不同（不同参数、不同默认值、不同边界），则用参数化表达；**参数化后语义必须逐分支等价**。无法等价的一律留在原套件内，并在 REPORT 单列“未合并及原因”。禁止为了让方法长得一样而改动行为。

**断言签名约定（R-8 / AC-10，M7 后要能直接包 `[UnityTest]`）**

```csharp
// 唯一允许的形状：证据出口显式入参，夹具显式入参，不碰引导状态
IEnumerator Assert<Domain><Behavior>(IQaSink sink, <夹具/输入…>)
```

| 允许 | 禁止 |
|---|---|
| 断言体内只经 `sink.Record/BlockedAssertion/InfraFailure` 出证据 | 断言体内出现 `File.`、`Directory.`、`_ev`/`_dir`、`SessionState`、`EditorApplication`、`QaLauncher`、`*_Boot` |
| 夹具（`PlayerProbe`/目标/箱子）作为参数传入 | 在断言内 `GameObject.Find`/`FindObjectsByType` 现取“当前引导状态”，或读 runner 的引导字段来决定断言是否执行 |
| 断言方法可以留在 runner 类上（本单不搬类） | 断言方法签名依赖 Boot/Launcher 的存在 |
| 编排层（`Start`/`Finish`/看门狗）继续用 `SessionState`、写 `ExitCode` | 把这些搬进断言 |

- 阶段 A 只要求：`IQaSink` + `QaEvidence` 实现它 + **1 条示例断言**改成新签名（选 SPEC01 里最简单的一条），其余断言可在阶段 B–D 逐套件改。
- **违反者不算完成**：阶段 B/C/D 的套件迁移必须让该套件所有 `Assert*` 满足上表；若某条实在做不到（例如依赖 MonoBehaviour 协程生命周期），留在原地并在 REPORT 单列“仍直写文件的断言/仍依赖引导的断言”及原因。


### 2.4 证据产物契约（AC-3 的比对基准，**逐字符照此保留**）

| 套件 | 证据目录 | 断言 JSONL | Summary | ExitCode | Runs 索引 |
|---|---|---|---|---|---|
| SPEC01 | `Docs/QA/Evidence/SPEC-01/[<subdir>]` | `SPEC01_Assertions.jsonl`；有 tag：`SPEC01_Green_<tag>_run<N>_Assertions.jsonl`；red：`SPEC01_Red[_<tag>]_Assertions.jsonl` | 默认 `SPEC01_Summary_run<N>.json`；red 或有 tag：`<prefix>Summary.json` | 默认 `SPEC01_ExitCode_run<N>.txt`；red/有 tag：`<prefix>ExitCode.txt` | `SPEC01_Runs[ _<tag>].jsonl` |
| REFACTOR | `Docs/QA/Evidence/REFACTOR-<M>/[<subdir>]` | `REFACTOR_Assertions.jsonl` | `REFACTOR_Summary_run<N>.json` | `REFACTOR_ExitCode_run<N>.txt` | `REFACTOR_Runs.jsonl` |
| FND00B06 | `Docs/QA/Evidence/FND-00B-06/[<subdir>]` | `06_Assertions.jsonl` | `06_Summary_run<N>.json` | `06_ExitCode_run<N>.txt`（由 runner 在 `Finish()` 内写，非 `*_Evidence` 提供路径） | **不生成** |
| DIAG | `Docs/QA/Evidence/DIAG-SKILL/[<subdir>]` | `DIAG_Assertions.jsonl` | `DIAG_Summary.json`（固定名，无 run 后缀） | `DIAG_ExitCode.txt`（固定名） | **不生成** |

- `<subdir>` 来自环境变量 `QA_EVIDENCE_SUBDIR`（编辑器内由 `QA_InEditorMenu` 注入 `editor_runN`）；未设置时写到证据目录根部。
- 断言行字段顺序固定：`run, qaItem, assertion, expected, actual, pass, status, kind, timestamp, data`，UTF-8 无 BOM，行尾 `\n`。
  - **REFACTOR 例外（2026-11-06 大脑按实测修正）**：REFACTOR 的断言行在 `run` 之后多一个 `milestone`（`run, milestone, qaItem, …`）。这是现状，必须保留 → `QaEvidence` 需支持“断言行可选前置字段”（纯增量），REFACTOR 传 `milestone`，其余三套件不传。§2.3 的 `QaEvidenceSpec` 相应增加该可选项。
- **Summary 的键名与键序逐套件保留**（迁移后必须与迁移前一致）：
  - SPEC01：`task, mode, run, startedAt, finishedAt, unityVersion, scene, randomSeed, assertionFile, assertionLinesInFile, pass, fail, blocked, infraFail, observed,` **`mode`（重复键，现状，禁止修）**`, head, enemySourceGuid, enemyPosition, fixtureNote, testWroteGameStarted, exitReason`
  - REFACTOR：`task, milestone, run, startedAt, finishedAt, unityVersion, scene, randomSeed, assertionFile, pass, fail, blocked, infraFail, observed, head, fixtureNote, exitReason`
  - FND00B06：`run, task, qaItems, startedAt, finishedAt, unityVersion, scene, randomSeed, assertionFile, assertionLinesInFile, pass, fail, blocked, infraFail, observed, exitReason`
  - DIAG：`task, mode, run, startedAt, finishedAt, unityVersion, scene, head, assertionFile, pass, fail, blocked, infraFail, observed, conclusion, exitReason`
- 计分口径不变：`infra_fail→Infra`、`blocked→Blocked`、`observed→Observed`、其余 pass 真值分 `Pass`/`Fail`。

### 2.5 实施顺序（每阶段一次派单）

| 阶段 | 内容 | 涉及文件 | 该阶段验收 |
|---|---|---|---|
| **A** | ① 先跑 4 套件各 1 次，保存 `before` 基线快照；② 建 Qa 核心（`QaJson`/`QaSink`/`QaEvidence`+`QaEvidenceSpec`/`QaWait`/`QaFixtures`/`QaScene`）；③ 移动 `SkillCastFixture.cs`（连 `.meta`）；④ `SPEC01_Json` 全仓改名 `QaJson` 并删原类；⑤ 迁移 SPEC01（Evidence→适配器并实现 `IQaSink`，ProbeRunner 用 Qa 层，BatchAcceptance 的 `GitHead`/`AbsPath`/`FindEnemySourceGuid` 转 `QaScene`）；⑥ **1 条示例断言**改成 `AssertXxx(IQSink sink, …)` | 新增 6 + 移动 1(+meta) + 改 4（`SPEC01_Evidence`、`SPEC01_ProbeRunner`、`SPEC01_BatchAcceptance`、`DIAG_Evidence` 仅类名替换） | 编译绿；SPEC01 12/12；SPEC01 断言 ID 多重集与 before 一致；`IQaSink` 定义 1 份、`QaEvidence` 实现它、≥1 条断言用新签名 |
| **B** | 迁移 REFACTOR（16 条）；该套件所有 `Assert*` 加 `IQaSink` 首参 | 改 `REFACTOR_Evidence`、`REFACTOR_ProbeRunner`、`REFACTOR_BatchAcceptance` | 编译绿；REFACTOR 16/16；多重集一致；AC-10 静态检查过 |
| **C** | 迁移 FND00B06（12+2 observed）；该套件所有 `Assert*` 加 `IQaSink` 首参。**注意（2026-11-06 实测）**：FND00B06 现有断言不是独立方法（逻辑在 `Start`/`SampleInitialIdle`/`GroupAttack`/`GroupAnimation` 内），允许**拆出/改名**为 `AssertXxx(IQSink sink, …)`，判定表达式原样搬入，不改阈值 | 改 `FND00B06_Evidence`、`FND00B06_ProbeRunner`、`FND00B06_BatchAcceptance` | 编译绿；12 pass + 2 observed；多重集 + `expected` 一致；AC-10 静态检查过 |
| **D** | 迁移 DIAG（6/1）；该套件所有 `Assert*` 加 `IQaSink` 首参 | 改 `DIAG_Evidence`、`DIAG_ProbeRunner`、`DIAG_BatchAcceptance` | 编译绿；6 pass + 1 `product_fail`（`Diag.Input.FirstExit`，已知红不变）；多重集一致；AC-10 静态检查过 |
| **E** | 统一 Boot/Launcher + 删重复 + 写模板 + 写分层文档 + 补齐签名 | 新增 `QaLauncher.cs`、`QaTestTemplate.md`、`Docs/06_测试架构与验收分层.md`；改 4 个 `*_BatchAcceptance.cs`（删 `*_Boot` 类）；**补齐 SPEC01 剩余 11 条断言的 `IQaSink` 首参**（阶段 A 只做了 1 条）；改 `Docs/01_总体架构与内容生产规范.md`（**仅 §14.1 加一行指针**）；必要时微调 `QA_InEditorMenu.cs`（仅适配，不改菜单与子目录规则） | 四套件全绿且**各连跑 2 次**；L2 域重载一轮；AC-6 计数全达标；AC-10 四套件断言全达标；AC-9 六节齐备 |

**编译安全规则（阶段 A 必读）**：`DIAG_Evidence` 目前直接调用 `SPEC01_Json`。阶段 A 删除 `SPEC01_Json` 时，必须同批把全仓 `SPEC01_Json` 替换为 `QaJson`（纯类名替换，零逻辑改动），否则编译红。同理阶段 B/C 删除 `REFACTOR_Json`/`FND00B06_Json` 时先确认无外部引用（当前仅在各自 `*_Evidence.cs` 内），再替换为 `QaJson`。
**`*_Boot` 一律留到阶段 E**：阶段 A–D 只改 `*_BatchAcceptance.Run()` 内部对 `QaScene`/`QaLauncher` 的调用，**不得**在中间阶段删 `*_Boot`（否则四套件同时不可用）。

### 2.6 影响面清单

| 项 | 内容 |
|---|---|
| 新增文件 | `Tests/QA/Runtime/{QaJson,QaSink,QaEvidence,QaWait,QaFixtures,QaScene}.cs`、`Tests/QA/Editor/QaLauncher.cs`、`Tests/QaTestTemplate.md`、`Docs/06_测试架构与验收分层.md`（Unity 为新 `.cs` 自动生成 `.meta`；`.md` 作为 TextAsset 生成 `.meta`） |
| 修改文件（文档） | `Docs/01_总体架构与内容生产规范.md` —— **仅 §14.1「自动测试」末尾加一行**指向 `Docs/06`；其余章节一字不动 |
| 移动文件 | `Tests/SPEC01/Runtime/SkillCastFixture.cs` → `Tests/QA/Runtime/SkillCastFixture.cs`，**连 `.meta` 一起移动，GUID 不变，类名不变** |
| 修改符号 | `SPEC01_Evidence`/`REFACTOR_Evidence`/`FND00B06_Evidence`/`DIAG_Evidence`（改为适配器，公开成员不变）；四个 `*_ProbeRunner`（删除本地重复的 `PlayerProbe`/`HitCollector`/`WaitState`/`StateName`/`Dict`/`V3`/`CreateTarget`/`Select`/`FindPlayers`/`GetHead` 等）；四个 `*_BatchAcceptance`（`GitHead`→`QaScene.GetHead`；阶段 E 删 `*_Boot`）；`SPEC01_Json`/`REFACTOR_Json`/`FND00B06_Json`（删除，改用 `QaJson`） |
| 字符串引用 | `-executeMethod SPEC01_BatchAcceptance.Run` 等**命令行入口名必须不变**；菜单项 `Tools/QA/In-Editor/{SPEC01 (green), FND00B06, DIAG, REFACTOR}` 必须不变；`SessionState` 键前缀 `SPEC01.` / `REFACTOR.` / `FND00B06.` / `DIAG.` / `QA.InEditor.*` 必须不变（跨域重载存活依赖它们）；`GameObject` 名字 `SPEC01_Runner`/`REFACTOR_Runner`/`FND00B06_Runner`/`DIAG_Runner` 必须不变（`RunnerAlive` 靠 `GameObject.Find(name)`） |
| 场景 / 预制体引用 | **无**。本单不读改任何 `.unity`/`.prefab`；`QaScene.GetEnemySourceGuid` 只读文本扫 YAML，与现实现一致 |
| `.meta` | 仅 `SkillCastFixture.cs.meta` 随文件移动；新增文件由 Unity 生成；不手改任何既有 `.meta`、不猜 GUID |

### 2.7 风险与回滚

| 风险 | 检测 | 处置 |
|---|---|---|
| 断言被悄悄改动（最严重） | AC-1 断言 ID 多重集比对 + AC-2 计数 | 立即停，按回滚锚回退该阶段提交，重做 |
| `Summary` 键序/文件名漂移（AC-3） | 阶段内 before/after 目录文件清单 + Summary 键序提取比对 | 同上 |
| 去重时改了行为（如 `CreateTarget` 的 `showDamageNumber` 默认值、`WaitState` 超时口径） | 该套件断言转红 | 撤销该处合并，保留套件本地实现，REPORT 记“未合并” |
| 移动 `SkillCastFixture.cs` 丢 `.meta` | Unity 生成新 GUID；REFACTOR/DIAG 编译报找不到类型 | 用 git 恢复 `.meta`，重做 `git mv` 等价操作 |
| 阶段 E 删 Boot 后卡在 Play / 域重载丢 runner | 看门狗应写 `code=3 reason=runner-lost-domain-reload` 且不杀编辑器 | 回退阶段 E，Boot 恢复；单列未决 |
| 编译连续 2 轮红 | Console | 熔断（`AGENTS.md` §6.1） |

回滚：每阶段一次本地 commit（由大脑做）；出问题 `git revert <阶段提交>` 或 `git checkout df7ea0b0 -- Assets/_Project/Tests`（仅在所有者同意时）。

## §3 验证（Validation）

**Unity 访问模式：模式 B（MCP 常开编辑器）**。理由：四套件的唯一入口是 `Tools/QA/In-Editor/*` 菜单（`TEST-MCP-01` 定案，`AGENTS.md` §3.2.1 第 7 条：这三套**不得**用 `run_tests`）。授权范围见 §4。

### AC 表

| AC | Given / When / Then | 断言 ID | 证据 |
|---|---|---|---|
| **AC-1** | 给定每套件迁移**前**的一轮完整 JSONL（`before`），当该套件迁移**后**再跑一轮（`after`），则两份 JSONL 提取的 `(qaItem, assertion, status)` 行**排序后逐行一致**，且总行数、pass/fail/blocked/infra/observed 计数一致；**且每条断言的 `expected` 字面量逐字符一致**（`expected` 是规格描述，跨轮不应变化；`actual`/`timestamp`/`run`/`data` 为实测值，跨轮本就允许波动，不参与比对，也不得因此改断言） | 每套件全部断言（SPEC01 12、REFACTOR 16、FND00B06 14、DIAG 7） | `Docs/QA/Evidence/QA-REFACTOR-01/<套件>/diff_AssertionIds.txt`（含 `BEFORE`/`AFTER`/`DIFF` 三段，`DIFF` 必须为空）+ `<套件>/diff_Expected.txt`（`expected` 逐条比对，`IDENTICAL` 或差异清单） |
| **AC-2** | 给定阶段 E 的四套件连续 2 次运行，则每次均为：SPEC01 `12 pass / 0 fail`（12 行）、REFACTOR `16 / 0`（16 行）、FND00B06 `12 / 0` + `observed 2`（14 行）、DIAG `6 / 1`（7 行，唯一红为 `Diag.Input.FirstExit`，`status=product_fail`） | 同上 | `Docs/QA/Evidence/QA-REFACTOR-01/final/run{1,2}/`（各套件 Summary + JSONL + InEditorResult 快照） |
| **AC-3** | 给定 before/after 两轮证据目录，则**文件名集合完全一致**，且 `Summary` 的键名与键序逐字符一致（含 SPEC01 的重复 `mode` 键）、断言行字段顺序一致、UTF-8 无 BOM | — | `Docs/QA/Evidence/QA-REFACTOR-01/<套件>/diff_EvidenceShape.txt` |
| **AC-4** | 给定阶段 E 交付时的 `git status --porcelain` 与 `git diff --stat`，则：`Assets/_Project/Scripts/**` 零改动；代码侧改动全部落在 `Assets/_Project/Tests/**`；`Docs/` 侧只允许四类——`Docs/Handoff/{TASK,REPORT}_QA-REFACTOR-01.md`、`Docs/QA/Evidence/QA-REFACTOR-01/**`、`Docs/06_测试架构与验收分层.md`（新增）、`Docs/01_总体架构与内容生产规范.md`（**本单只加 §14.1 一行**，`+1/−0`；**基准 = 阶段 E 开工瞬间该文件的内容**——所有者同期在做 `DOC-INFRA-01`，对该文件另有已存在的未提交改动，不属本单、不得计入、更不得回退或顺带提交）；另有四套件 canonical 证据目录下**新增**的 `editor_runN/` 子目录（菜单自动递增、只追加，属预期，不算越界）；`Assembly-CSharp*.csproj` 属 Unity 自动生成、**未提交** | — | `Docs/QA/Evidence/QA-REFACTOR-01/git_scope.txt`（贴原始命令输出） |
| **AC-5** | 给定 `Assets/_Project/Tests/QaTestTemplate.md`，则包含 ≥1 条**完整可复制**示例：夹具创建 + 一条 `QaEvidence.Record` 断言 + 证据落盘位置说明 + 断言 ID 命名约定；示例中的类名/方法名必须与 `Tests/QA/` 实际一致（不得写不存在的 API） | 示例断言 ID（如 `Qa.Template.Smoke`） | 文件本身 + `git_scope.txt` 中的路径 |
| **AC-6** | 给定阶段 E 的代码，则以下 grep 计数成立（**统计范围 = `Tests/QA/**` + 四套件；`Tests/FND00B/FND00B04*`、`FND00B05*`、`Tests/FND02A/**` 属范围外不计数**）：`class .*_Boot` = 0、`class .*_Json` = 0、`class PlayerProbe` = 1、`class HitCollector` = 1、`static Dictionary<string, object> Dict(` 定义 = 1、`IEnumerator WaitState(` 定义 = 1、`GetHead(` 定义 = 1；`"Docs", "QA", "Evidence"` 字面量**只允许出现在 `QaEvidence.cs` 与 `QA_InEditorMenu.cs`**（其余位置 = 0）；四个 `*_Evidence.cs` 每个 ≤40 行且不含 `File.`/`Directory.`。**行数不作为通过判据**（抽公共层会把内联逻辑变成带签名的方法，单文件允许变长；实测阶段 A–D 后范围内总行数 8,209→8,929，属预期成本）：`before`/`after` 总行数只登记进 `dedup_counts.txt` 作参考，**不得为压行数删注释、合并断言或去掉 `expected` 描述** | — | `Docs/QA/Evidence/QA-REFACTOR-01/dedup_counts.txt`（每条：命令 + 实际计数 + 期望；附行数前后对照） |
| **AC-7** | 给定阶段 E 的 `QaLauncher` 与四个已删 `*_Boot`，则逐条对照语义等价并回填 REPORT：①`ClearStaleRunFlags` 防看门狗误判 ②首启宽限 10s ③runner 丢失宽限 3s ④看门狗落 `code=3 + reason=runner-lost-domain-reload` 且编辑器内不杀 Unity ⑤`EnteredEditMode` + `Finished` → `ExitWithCode` ⑥`TryFinish` 优先于 `Exit` ⑦`RunnerAlive` 静态引用 + `GameObject.Find(name)` 双判定 ⑧runner `DontDestroyOnLoad`。另需 L2 故障注入 1 轮：Play 中触发域重载，期望看门狗按 ④ 收场且 Unity 不退出 | `Qa.Launcher.DomainReload`（L2 轮次标记，非产品断言） | `Docs/QA/Evidence/QA-REFACTOR-01/l2_domainreload/`（Console 摘录 + `InEditorResult.txt`） |
| **AC-8** | 给定阶段 E 的全仓编译，则 Unity Console 无 `error CS`；`Tests/QA/**` 无新增 `static` 可变全局字段（`QaLauncher` 的迁移字段除外，需在 REPORT 列出并对照旧实现） | — | `Docs/QA/Evidence/QA-REFACTOR-01/compile.log`（编译日志摘录 + `error CS` 计数=0） |
| **AC-9**<br>（所有者 2026-11-06 追加；口述编号为“AC-6”，但 §3 已有 AC-6，按“AC 只增不减”顺延为 AC-9） | 给定 `Docs/06_测试架构与验收分层.md`，则**六节齐备且各自有实质内容**（非占位）：①**分层** L0 冒烟 / L1 回归 / L2 故障注入 / 功能套件；②**套件契约** 夹具 + 断言 + 证据，且菜单为注册式（**由大脑 2026-11-06 修正为可执行口径**：Unity 的 `[MenuItem]` 不能在运行时动态增删，故“注册式”= 新增套件只需①在注册表加一条 `QaLaunchSpec`②加一个 `[MenuItem]` 转发方法，**不改**驱动逻辑（preflight / 证据子目录递增 / `TryFinish` 收尾 / 看门狗都不动）；文档须写明这条边界，不得写成“完全无需改代码”）；③**断言 ID 规范** `<Domain>.<Behavior>` 并与 TASK §3 AC 表逐条对应（举 ≥2 个真实例子，如 `Skill.CastSmoke`、`Diag.Input.FirstExit`）；④**扩展规则** 新功能新套件、行为变更须留痕（旧断言不许静默改）；⑤**证据 schema 版本化**（`schemaVersion` 字段与兼容策略）；⑥**构建隔离** M7 起 `tests asmdef` + `UNITY_INCLUDE_TESTS`。另：`Docs/01_总体架构与内容生产规范.md` §14.1 内出现一行指向 `Docs/06` 的指针 | 文档自身（示例中的断言 ID 必须与现有套件真实 ID 一致，不得杜撰） | `Docs/06_测试架构与验收分层.md` + `git_scope.txt` 中的 §14.1 diff 摘录 |
| **AC-10**<br>（所有者 2026-11-06 追加；口述编号为“AC-7”，但 §3 已有 AC-7，按“AC 只增不减”顺延为 AC-10） | 给定交付代码，则：①`Tests/QA/Runtime/QaSink.cs` 定义 `interface IQaSink`，成员恰为 `Record(9 参)`/`BlockedAssertion`/`InfraFailure`/`EvidenceDir`，文件内 `File`/`Directory`/`SessionState`/`EditorApplication`/`QaLauncher` 计数 = 0；②`QaEvidence` 声明实现 `IQaSink`；③已迁移套件的 `Assert*` 首参均为 `IQaSink`（形如 `IEnumerator Assert\w+\(IQaSink sink`；原本没有独立断言方法的套件允许拆出/改名为 `Assert*`，判定表达式原样搬入），断言体内 `File.`/`Directory.`/`SessionState`/`EditorApplication`/`QaLauncher`/`_Boot` 计数 = 0；④阶段 A 至少 1 条断言已用新签名，阶段 E 时四套件全部满足或在 REPORT 显式列例外与原因；⑤REPORT 必有「仍直写文件 / 仍依赖引导的断言」小节（无则写“无”，不得缺节） | `Qa.Sink`（静态检查，非产品断言）；阶段 A 示例断言沿用其原 ID（不得改 ID） | `Docs/QA/Evidence/QA-REFACTOR-01/utp_readiness.txt`（每条：grep 命令 + 实际计数 + 期望）+ REPORT 对应小节 |

**运行次数**：AC-2 要求四套件**各连续 2 次**（同一入口，数据一致）；阶段 A–D 各跑本套件 1 次做即时回归；AC-1 的 `before` 每套件 1 次（阶段 A 开始前统一采）。AC-7 的 L2 故障注入 1 轮。

> **AC 修订记录（大脑，2026-11-06）**：AC-6 原含「`Tests/` 总行数较 12,326 下降 ≥15%」。阶段 A–D 实测后该判据不成立（范围内 8,209→8,929，+8.8%）：抽公共层把内联逻辑变成带签名的方法，`QaEvidenceSpec`/`IQaSink`/`Assert*` 拆分会新增脚手架，行数本就不是这次重构的收益。**判据改为「重复实现份数归零」（grep 计数）**，行数只登记作参考。此修订不影响 AC-1/2/3 的严格性，属“判据纠错”，非放宽验收。

**AC 只增不减**：执行中发现漏项 → 追加 `AC-9…` 并在 REPORT 说明，不得改旧条。

**必须保存的证据**：每个验收动作的原始输出（JSONL、Summary、InEditorResult、日志摘录、命令输出）落在 `Docs/QA/Evidence/QA-REFACTOR-01/`；**canonical 套件证据目录只读**（不删、不改、不覆盖），快照用**复制**。

## §4 执行信息

| 项 | 内容 |
|---|---|
| 前置 | 基线 HEAD = `df7ea0b0`（不符即停）；Unity 编辑器常开且 MCP 可连（模式 B）；多实例时先 `set_active_instance`；开工/收工各记一次 `editor_state`（`instance_id`/`is_compiling`/`is_domain_reload_pending`/`playMode`） |
| 允许改动 | `Assets/_Project/Tests/**`（新增/修改/移动）+ 本单 `Docs/Handoff/REPORT_QA-REFACTOR-01.md` + `Docs/QA/Evidence/QA-REFACTOR-01/**` + `Docs/06_测试架构与验收分层.md`（新增，仅阶段 E）+ `Docs/01_总体架构与内容生产规范.md`（**仅阶段 E、仅 §14.1 一行指针，+1/−0**） |
| 允许读取 | 本工单；`Docs/QA/Evidence/{SPEC-01,REFACTOR-M*,FND-00B-06,DIAG-SKILL}/**`（**只读**，用 `Select-String` 定向提取，禁止整文件读大 JSONL）；`Assets/_Project/Tests/**` |
| 绝不触碰 | `Assets/_Project/Scripts/**`、`Assets/Scenes/**`、`Assets/_Project/Prefabs/**`、任何 `.controller`/`.asset`/`.unity`/`.prefab`、`ProjectSettings/**`、`Packages/**`、`Docs/Handoff/CURRENT.md`、`Docs/Handoff/TASK_*.md`（含本文件）、其他 `REPORT_*.md`、既有 `Docs/QA/Evidence/**`、`Docs/01` **§14.1 以外**的任何章节、`Docs/02`/`Docs/04`/`Docs/00`/`Docs/Specs/**`/`Docs/Decisions/**`/`Docs/Learning/**`；`Tests/FND00B/FND00B04*|FND00B05*`、`Tests/FND02A/**` |
| 编辑器写权限 | 只允许**脚本文件编辑**（执行者自己的文件编辑工具或 MCP `create_script`/`apply_text_edits`）；**禁止** `manage_scene`/`manage_prefab`/`manage_asset`/`manage_gameobject`/`manage_components`/`manage_animation`/`manage_scriptable_object`/`execute_code` 等资产写工具；测试只允许经 `Tools/QA/In-Editor/*` 菜单（`execute_menu_item`）触发，**禁止** `run_tests` |
| 证据目录 | `Docs/QA/Evidence/QA-REFACTOR-01/`（子目录：`<套件>/before/`、`<套件>/after/`、`final/run{1,2}/`、`l2_domainreload/`、`git_scope.txt`、`dedup_counts.txt`、`compile.log`） |
| 熔断（本单特有，命中即停并留现场） | ① 任一断言 ID/条数/`status`/阈值/通过条件变化 ② 需要改 `Scripts/**` 或任何资产才能过 ③ 编译连续 2 轮红 ④ 证据字段/文件名无法兼容（`QaEvidenceSpec` 表达不了） ⑤ 域重载后 `isCompiling` 单次 >10 分钟且 §3.2.1 第 9 条唤醒已试 3 次 ⑥ 单步重试 ≥3 次仍红 ⑦ 断言体内出现直接写盘或 `SessionState`/`EditorApplication`/`QaLauncher` 依赖且未在 REPORT 声明 |
| 执行预算 | 每阶段 **1 轮实现 + 最多 1 轮修复**；总文件改动上限 20（含新增）；时间上限 每阶段 ≤25 分钟；四套件运行总次数 ≤20 |
| 回滚锚 | 阶段提交前不提交；大脑在每阶段验收后 commit；出问题回退到上一阶段提交 |

**开工前六问（执行者必须在 REPORT §0 逐条作答）**：唯一主要结果 / 明确不做什么 / 允许改哪些文件 / 必须保持哪些旧行为 / 如何证明完成 / 失败后在哪里停。

## §5 学习产出

| 项 | 要求 |
|---|---|
| 讲稿 | **需要**（架构类改动，所有者答辩要能讲清“为什么分层、如何保证断言没变”）：`Docs/Learning/QA-REFACTOR-01_讲稿.md`，**由大脑在阶段 E 后撰写**，执行者不写 |
| 三句话版本 | 执行者在 REPORT §3 给三句；大脑收口时与讲稿对齐 |
| 不适用理由 | 不适用本条 |

## 自包含声明

本工单不依赖 `CURRENT.md` 或其他任务书即可执行。执行者只读本文件 + `AGENTS.md`。信息不足时在 REPORT 写“信息不足”并列缺项，**不**自行翻阅别的文档补全。
