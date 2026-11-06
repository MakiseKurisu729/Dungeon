# QA-REFACTOR-01 讲稿：把四套守卫的重复脚手架抽成 Qa 公共层

> AI 完整稿（已可独立讲），所有者按需批注，不必重写。数据均来自 `Docs/Handoff/REPORT_QA-REFACTOR-01.md` 与 `Docs/QA/Evidence/QA-REFACTOR-01/`。

## 1. 一句话

四套守卫（SPEC01 / REFACTOR / FND00B06 / DIAG）原本各自复制一份“证据写入 + JSON 序列化 + 夹具 + 等待 + Boot 引导”，我把它收敛成 `Tests/QA/` **单份实现**，并让每个套件只剩“断言 + 编排”，同时用机器比对证明**没有改任何一条断言的口径**。

## 2. 原来的问题 / 为什么需要

- **改之前的现象**：`Tests/` 共 23 个 `.cs`、12,326 行，其中至少 1,100 行是 5 份几乎一样的 `*_Evidence`（含 5 份 JSON 序列化）、约 600 行是 4 份几乎一样的 `*_Boot`（域重载看门狗 + SessionState 引导）、`PlayerProbe`/`HitCollector`/`WaitState`/`CreateTarget` 各 2 份。
- **跨套件耦合**：`DIAG_Evidence` 直接调用 `SPEC01_Json`；`DIAG` 与 `REFACTOR` 依赖 `SPEC01/Runtime/` 目录下的 `SkillCastFixture`。这不是“重复”，是**依赖方向错乱**——M7 拆程序集时这批代码根本没法归属。
- **不解决会怎样**：加一条断言要先抄 80 行样板；改看门狗要改 4 处且极易漏掉退出分叉；证据格式一旦分叉，历史证据就无法用同一套脚本比对。
- **触发条件**：M5/M6 要继续改产品代码，每次都要先读懂 1,900 行的探针脚手架；蒸馏到“读断言就知道在测什么”是继续重构的前置条件。

## 3. 方案对比

| 方案 | 做法 | 为什么不选 / 为什么选 |
|---|---|---|
| A 一次性重写四套件 | 重排断言、统一命名、顺手建 asmdef | 不行。12k 行同时变，断言口径无法证伪；且 asmdef 程序集**引用不到** `Assembly-CSharp`（产品代码还没拆），直接编译不过 |
| B 只抽 Evidence + Json | 只合并收益最大的两块 | 不行。`PlayerProbe`/`WaitState`/`CreateTarget` 仍各 2 份，跨套件依赖（DIAG→SPEC01）不消失，M7 归属问题没解决 |
| **C 分层 + 五阶段串行** | 先建 Qa 层（零行为变化）→ SPEC01 → REFACTOR → FND00B06 → DIAG → 最后统一 Boot | **选定**。每阶段独立编译、独立验收、独立回滚；断言口径靠“多重集 + `expected` 字面量 + 证据形状”三重机检保证；把 Boot 放最后，因为它一改就四套件同时不可用 |

## 4. 数据流

```
[菜单 Tools/QA/In-Editor/<套件>]
   → QA_InEditorMenu（preflight 等编辑器稳定 + 分配 editor_runN 子目录）
   → QaLauncher.Begin(spec)（写 SessionState 标记 → EnterPlaymode）
   → 进 Play：QaLauncher 创建 <套件>_Runner（DontDestroyOnLoad）→ ProbeRunner
   → ProbeRunner：QaFixtures 建夹具 → AssertXxx(IQSink sink, …) 断言
   → QaEvidence（实现 IQaSink）：JSONL 逐行落盘 + 计分 + Summary + Runs 索引
   → 退 Play：QaLauncher.ExitWithCode → QA_InEditorMenu.TryFinish（写 InEditorResult.txt，不杀编辑器）
                                    ↘ batch 路径：EditorApplication.Exit(code)
```

## 5. 关键代码 3–5 处

| 位置 | 这段在干嘛 |
|---|---|
| `Assets/_Project/Tests/QA/Runtime/QaSink.cs:7` | `IQaSink`：断言**唯一**的证据出口，4 个成员（`Record`/`BlockedAssertion`/`InfraFailure`/`EvidenceDir`）。M7 后可换成内存收集器或 NUnit 适配器，断言源码不用动 |
| `Assets/_Project/Tests/QA/Runtime/QaEvidence.cs:39` | `QaEvidenceSpec`：把“目录名 / 断言文件名 / Summary 文件名 / ExitCode 文件名 / Runs 索引 / tag 环境变量 / 断言行前置字段”做成数据，四套件的差异全靠它表达，逻辑只写一遍 |
| `Assets/_Project/Tests/QA/Runtime/QaEvidence.cs:167` | `Record(...)`：唯一落盘路径。字段序固定 `run, [前置字段], qaItem, assertion, expected, actual, pass, status, kind, timestamp, data` |
| `Assets/_Project/Tests/QA/Editor/QaLauncher.cs:267` | `Tick()`：域重载看门狗。首启 10s 不建 runner → 首启失败；runner 丢失 3s → 判 `runner-lost-domain-reload` |
| `Assets/_Project/Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs:1021` | 迁移后的断言形状：`AssertSpawnAnchor(IQSink sink)`——显式拿证据出口，不再读 runner 的 `_ev` 字段 |

## 6. 为什么这样写（设计取舍）

- **保留同名薄适配器（`SPEC01_Evidence : QaEvidence`，32–38 行）而不是删类**：探针里几百处 `_ev.Record(...)` 一行不用改，爆炸半径最小；适配器只组装 spec，不含 I/O 与计分，逻辑仍然只有一份。
- **只合并“可证明等价”的实现**：`SkillCastFixture.CreateTarget`（maxHp 1000、固定 `showDamageNumber=false`、带宿主字段）与 `QaFixtures.CreateTarget`（100000、参数化）边界不同，**没有合并**；`ResolveState`（带前导空格的真实状态名 + `unknown#hash`）与 `QaFixtures.StateName` 不等价，也没合并。去重不等于强统一——为了“长得一样”改行为是最贵的 bug。
- **行数不是判据**：抽公共层会把内联逻辑变成带签名的方法（`Assert*` 拆分、`QaEvidenceSpec`、`IQaSink`），单文件允许变长。实测范围内 8,209 → 8,582 行（+4.5%），收益是“同一逻辑只有一份”，不是行数。我一开始把 AC 写成“总行数降 15%”，阶段 A–D 实测后自己改掉了这条判据。
- **与 `Docs/01` 不变量的关系**：Tests → Scripts 单向依赖不变（产品代码零改动、零新增测试钩子）；没有新增静态全局状态（4 个 Boot 的 8 个可变静态合并为 2 个，净减 5）。

## 7. 边界 / 失败模式

- **断言被悄悄改**：三重机检挡——`(qaItem, assertion, status)` 多重集排序逐行比对、`expected` 字面量逐字符比对、证据文件名/键序比对。任何一项非空即判失败。
- **证据格式漂移**：`QaEvidenceSpec` 里把每个套件的文件名与 Summary 键序（含 SPEC01 那个重复的 `mode` 键、REFACTOR 断言行里的 `milestone`）全部显式保留，漂移即红。
- **合并把行为改了**：判定“等价”才合并；不等价一律留原处并在 REPORT 单列。
- **域重载看门狗**：8 条语义逐条对照旧实现（宽限值、`code=3 + reason`、`TryFinish` 优先、双判定 `RunnerAlive`、`DontDestroyOnLoad`），有结构校验脚本。
- **已知未覆盖**：L2“Play 中真实触发域重载”在本机**没做成**——Unity 2022.3 默认把 Play 期间的脚本重编译延迟到退出 Play 之后，三次注入都没有产生域重载。要真触发需改编辑器偏好（Play 期间脚本重载策略），属所有者侧操作；已留现场，未伪造。

## 8. 答辩 Q&A 5 条

1. **为什么不用“一次性重写四套件”？**
   因为这次唯一真正的风险是“悄悄改了断言”，而重写无法用迁移前后的机器比对证伪；分层 + 五阶段串行让每个阶段都能用同一套 diff 门禁，出问题只回退一个阶段。
2. **这个改动影响哪些已有系统？**
   只影响 `Assets/_Project/Tests/**` 与 `Docs/`。产品代码（`Scripts/**`）零改动、无新增测试钩子；四套件菜单名、命令行入口名、SessionState 键、证据目录与文件名全部保持逐字符不变。
3. **新增一条断言 / 一个新套件要改哪里？**
   新断言：在对应 `ProbeRunner` 写一个 `AssertXxx(IQSink sink, …)`，用 `sink.Record(...)` 出证据。新套件：加一条 `QaLaunchSpec` + 一个 `[MenuItem]` 转发方法 + 一个 ≤40 行的 `*_Evidence` 适配器；preflight、证据子目录递增、收尾逻辑都不用动。模板见 `Assets/_Project/Tests/QaTestTemplate.md`。
4. **怎么证明它是对的？证据在哪？**
   四套件各连跑 2 次：SPEC01 12/0、REFACTOR 16/0、FND00B06 12/0+2 observed、DIAG 6/1（唯一红 `Diag.Input.FirstExit` 与迁移前一致）；`Docs/QA/Evidence/QA-REFACTOR-01/final/` 有 8 次原始产物，`<套件>/diff_*.txt` 有逐条比对，`dedup_counts.txt` 有去重计数。
5. **如果重做一次，会怎么改？**
   两处：①先写“断言口径不得变化”的比对脚本再动代码（这次是阶段 A 才补上，顺序反了）；②不把“总行数下降”写进 AC——抽层一定会加脚手架，行数只会涨。
