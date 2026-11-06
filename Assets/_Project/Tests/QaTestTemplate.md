# Tests 断言模板（Qa 公共层）

> 目标：新人照抄本文件就能给一个套件**新增一条断言**，不用先读 1,000 行探针脚手架。
> 本文件里的示例 ID `Qa.Template.Smoke` 是**示例**，不参与任何套件运行，也不要把它加进现有套件的 `Run()`。
> 所有类名/方法名/字段名都照抄仓库里 `Assets/_Project/Tests/QA/` 与四个套件（`SPEC01` / `REFACTOR` / `FND00B06` / `DIAG`）的现状。

## 1. 三条硬规则（违反即不算完成）

| 规则 | 允许 | 禁止 |
|---|---|---|
| 证据出口唯一 | 断言体内只调 `sink.Record(...)` / `sink.BlockedAssertion(...)` / `sink.InfraFailure(...)` | 断言体内出现 `File.` `Directory.` `SessionState` `EditorApplication` `QaLauncher` `*_Boot` `_ev`/`_dir` |
| 夹具显式入参 | 夹具（`QaFixtures.PlayerProbe` / 目标 GameObject / `SkillCastFixture`）由调用方创建后作为参数传入 | 断言里 `GameObject.Find` / `FindObjectsByType` 现取“当前引导状态”，或读 runner 的引导字段决定是否执行 |
| 证据不自己拼 | 断言行、Summary 键序、计分、文件名全部由 `QaEvidence`（实现 `IQaSink`）负责 | 断言里手写 JSON、手算 pass/fail 计数、手拼证据路径 |

断言方法签名只有一种形状（`IQaSink` 首参 + 夹具入参）：

```csharp
IEnumerator Assert<Domain><Behavior>(IQaSink sink, /* 夹具/输入… */)
```

这样 M7 拆 `tests asmdef` 后，可以用 `[UnityTest]` 直接包同一个方法，把 `IQaSink` 换成内存收集器或 NUnit 结果适配器，断言源码不用改。

## 2. 完整示例（可直接复制）

下面四段构成一个最小可用套件：**证据适配器 → 断言 → runner 编排 → 菜单 + 注册表**。
新套件名假设为 `SMOKE`，证据目录 `Docs/QA/Evidence/SMOKE-01/`。

### 2.1 证据适配器：`Tests/SMOKE/Runtime/SMOKE_Evidence.cs`

只组装 `QaEvidenceSpec`（纯数据、零 I/O），上限 40 行。

```csharp
using System.Collections.Generic;

/// <summary>SMOKE 证据契约适配器：只组装 QaEvidenceSpec；落盘/计分/Summary 全部由 QaEvidence 完成。</summary>
public class SMOKE_Evidence : QaEvidence
{
    public SMOKE_Evidence(int run, string mode) : base(Spec(), run, mode) { }

    static QaEvidenceSpec Spec()
    {
        return new QaEvidenceSpec
        {
            Task = "SMOKE-01",
            LogTag = "SMOKE",
            EvidenceFolder = "SMOKE-01",
            AssertionFileName = "SMOKE_Assertions.jsonl",
            SummaryFileName = "SMOKE_Summary_run{run}.json",
            ExitCodeFileName = "SMOKE_ExitCode_run{run}.txt",
            RunIndexFileName = null,                 // null = 不生成 Runs 索引
            ShouldTruncateAssertions = (r, m) => r <= 1,
            SummaryFields = new[]                    // 键序 = Summary 输出顺序，逐字符固定
            {
                QaSummaryKey.Task, QaSummaryKey.Mode, QaSummaryKey.Run, QaSummaryKey.StartedAt,
                QaSummaryKey.FinishedAt, QaSummaryKey.UnityVersion, QaSummaryKey.Scene,
                QaSummaryKey.RandomSeed, QaSummaryKey.AssertionFile, QaSummaryKey.AssertionLinesInFile,
                QaSummaryKey.Pass, QaSummaryKey.Fail, QaSummaryKey.Blocked, QaSummaryKey.InfraFail,
                QaSummaryKey.Observed, QaSummaryKey.Extra, QaSummaryKey.ExitReason
            }
        };
    }
}
```

### 2.2 断言：`Tests/SMOKE/Runtime/SMOKE_ProbeRunner.cs`（片段）

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SMOKE_ProbeRunner : MonoBehaviour
{
    // 编排层字段：证据实例与运行序号（断言不读这些字段）
    SMOKE_Evidence _ev;
    int _run = 1;
    int _seed = 20261105 + 1;

    IEnumerator Start()
    {
        _ev = new SMOKE_Evidence(_run, "green");

        // ① 夹具：目标单位（高 HP、无 AI、静态）在内存里创建，调用方负责销毁
        GameObject target = QaFixtures.CreateTarget("SMOKE_Target", new Vector3(0f, 0f, -4f), false);

        // ② 断言：夹具作为参数传入，证据只经 IQaSink 出去
        yield return AssertSmokeTargetAlive(_ev, target);

        if (target != null) Destroy(target);
        Finish();
    }

    /// <summary>示例断言 Qa.Template.Smoke（示例，不参与套件运行）。</summary>
    IEnumerator AssertSmokeTargetAlive(IQaSink sink, GameObject target)
    {
        if (target == null)
        {
            sink.BlockedAssertion("Qa.Template.Smoke", "Qa.Template.Smoke",
                "目标单位存活且 HP 满值", "夹具创建失败", null);
            yield break;
        }

        UnitHealth uh = target.GetComponent<UnitHealth>();
        yield return QaWait.WaitUntil(() => uh != null && uh.hp > 0f, 2f);

        bool pass = uh != null && uh.hp > 0f && !uh.dead;
        sink.Record("Qa.Template.Smoke", "Qa.Template.Smoke",
            "新建目标单位 hp>0 且未死亡",                        // expected：规格描述，跨轮不变
            "hp=" + (uh != null ? uh.hp : -1f) + "; dead=" + (uh != null && uh.dead),
            pass, pass ? "pass" : "product_fail", "production",
            QaJson.Dict("hp", uh != null ? uh.hp : -1f,
                        "team", uh != null ? uh.team.ToString() : "none"));
    }

    void Finish() { /* 见 2.3 的收尾口径 */ }
}
```

要点：

- `sink` 的实现方就是 `QaEvidence`（`QaEvidence.Record(qaItem, assertion, expected, actual, pass, status, kind, data)`，9 参）；示例里以 `IQaSink` 形式出现，是为了让断言不依赖落盘、不依赖引导状态。
- `Record` 的 9 个参数固定为 `(qaItem, assertion, expected, actual, pass, status, kind, data)`；`status` 取值 `pass` / `product_fail` / `blocked` / `observed` / `infra_fail`，`kind` 取 `production`（产品断言）或 `infra`（测试基础设施）。
- 夹具创建失败用 `BlockedAssertion`（不计 fail），反射/环境异常用 `InfraFailure`（计 infra），**不要**为了让数字好看改成 pass。
- 等待只用 `QaWait`（`WaitUntil` / `WaitState` / `WaitSeconds` / `WaitFrames` / `WaitNoNumbers`），超时静默返回，是否通过由断言自己判。

### 2.3 runner 收尾（编排层，允许用 `SessionState`）

```csharp
#if UNITY_EDITOR
using UnityEditor;
#endif

void Finish()
{
    _ev.WriteSummary(QaScene.ScenePath, _seed, "completed", null);
    int code = _ev.Infra > 0 ? 3 : (_ev.Fail > 0 || _ev.Blocked > 0 ? 2 : 0);

#if UNITY_EDITOR
    SessionState.SetBool("SMOKE.Finished", true);
    SessionState.SetInt("SMOKE.ExitCode", code);
    string exitPath = _ev.ExitCodePath();          // null = 本套件不生成 ExitCode
    if (exitPath != null)
        System.IO.File.WriteAllText(exitPath, code + System.Environment.NewLine,
            new System.Text.UTF8Encoding(false));
    EditorApplication.isPlaying = false;            // 触发 EnteredEditMode → QaLauncher 落结果/退出
#endif
}
```

> 注意：上面 `_seed` 是编排层字段（`int _seed = 20261105 + _run;`，照抄现有 runner）。
> 收尾状态机（`SMOKE.Finished` / `SMOKE.ExitCode` + `isPlaying=false`）是 `QaLauncher` 的契约，
> 键名前缀必须与注册表里的 `KeyPrefix` 一致；`ScenePath` 取 `QaScene.ScenePath`，不要自己拼路径。

### 2.4 菜单入口与注册表（一次加两处）

`Tests/SMOKE/Editor/SMOKE_BatchAcceptance.cs`：

```csharp
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SMOKE_BatchAcceptance
{
    public const string ScenePath = QaScene.ScenePath;

    public static void Run()
    {
        QaLaunchSpec spec = QaLauncher.SMOKE;
        QaLauncher.LogStart(spec);
        try
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            QaLauncher.Begin(spec);              // 写 SMOKE.Active/Started/Finished 并进 Play
        }
        catch (Exception e) { QaLauncher.Fail(spec, e); }
    }
}
```

`QaLauncher` 注册表加一条（`Tests/QA/Editor/QaLauncher.cs`）：

```csharp
new QaLaunchSpec
{
    SuiteId = "SMOKE",
    LogTag = "SMOKE",
    KeyPrefix = "SMOKE.",
    RunnerName = "SMOKE_Runner",                 // 必须与 GameObject 名一致（RunnerAlive 靠找它）
    RunnerType = typeof(SMOKE_ProbeRunner),
    EvidenceFolder = "SMOKE-01",
    EnvKeys = new[] { "SMOKE_RUN" },
    EnvValues = new[] { "1" },
    Launch = SMOKE_BatchAcceptance.Run
}
```

`QA_InEditorMenu` 加一个转发方法（菜单项文本 = 路径，必须唯一）：

```csharp
[MenuItem("Tools/QA/In-Editor/SMOKE", false, 104)]
public static void RunSMOKE()
{
    Run("SMOKE");
}
```

**边界（写文档/评审时不要写成“完全无需改代码”）**：Unity 的 `[MenuItem]` 不能在运行时动态注册，所以新增套件至少要改两处——注册表一条 `QaLaunchSpec` + 一个 `[MenuItem]` 转发。
preflight（编译/资产刷新/域重载等待）、证据子目录递增（`editor_runN`）、`TryFinish` 收尾、`runInBackground` 记录还原、看门狗**都不用改**。

## 3. 证据落到哪

以示例套件为例（`QA_EVIDENCE_SUBDIR` 在编辑器内由 `QA_InEditorMenu` 注入 `editor_runN`，batch 未设置时写证据目录根部）：

| 产物 | 路径 | 说明 |
|---|---|---|
| 断言 JSONL | `Docs/QA/Evidence/SMOKE-01/[editor_runN/]SMOKE_Assertions.jsonl` | 每条 `Record` 立即追加一行；UTF-8 无 BOM、行尾 `\n` |
| Summary | `Docs/QA/Evidence/SMOKE-01/[editor_runN/]SMOKE_Summary_run1.json` | 键名与键序由 `QaEvidenceSpec.SummaryFields` 固定 |
| ExitCode | `Docs/QA/Evidence/SMOKE-01/[editor_runN/]SMOKE_ExitCode_run1.txt` | `ExitCodeFileName` 为 null 时由 runner 判空跳过 |
| Runs 索引 | 不生成（`RunIndexFileName = null`） | REFACTOR 才用 `REFACTOR_Runs.jsonl` |
| 编辑器内结果 | `Docs/QA/Evidence/SMOKE-01/[editor_runN/]InEditorResult.txt` | 由 `QA_InEditorMenu.WriteResultFile` 写，不由断言写 |

断言行字段顺序固定（`QaEvidence.BuildAssertionLine`）：

```
run, [PrefixedFields…], qaItem, assertion, expected, actual, pass, status, kind, timestamp, data
```

- `PrefixedFields` 只有声明了才出现（REFACTOR 用它保留 `run, milestone, qaItem, …` 的现状）。
- 证据目录**只追加不覆盖**：历史 `editor_runN/` 不删不改；`ShouldTruncateAssertions` 只作用于“本轮自己的断言文件”。

## 4. 断言 ID 命名约定

`<Domain>.<Behavior>`，与 `TASK_*.md` §3 AC 表里的断言 ID 一一对应（AC 表写什么 ID，`Record` 的第一个参数就写什么）：

| 真实例子 | Domain | Behavior | AC 归属 |
|---|---|---|---|
| `Skill.CastSmoke` | `Skill` | `CastSmoke` | SPEC-01 技能施放冒烟 |
| `Diag.Input.FirstExit` | `Diag` | `Input.FirstExit` | DIAG 输入链首个失效节点（已知红） |
| `Party.PresentAndMove` | `Party` | `PresentAndMove` | REFACTOR 队伍存在与移动 |
| `Locomotion.AllPlayersMoveParam` | `Locomotion` | `AllPlayersMoveParam` | SPEC-01 移动参数 |

规则：

1. 一个 `Domain` 只做一件事（`Skill` / `Clock` / `Animation` / `UI` / `Locomotion` / `Diag` / `Party`…），不按文件、不按人分。
2. `Behavior` 写“被观察的行为”，不写“期望结果”（不写 `ShouldPass`）。
3. 断言 ID 一旦进过 `Docs/QA/Evidence/**` 就**不改**；行为变更只能新增 ID 或显式改口径并在 REPORT 留痕。
4. `qaItem` 与 `assertion` 通常同值；分档套件（FND00B06 的 `4`/`6`）把档位写进 `qaItem`。

## 5. 新增一条断言的最短清单

1. 在当前套件的 runner 里写 `IEnumerator Assert<Domain><Behavior>(IQaSink sink, …)`，判定表达式照业务写清楚。
2. 在 `Start()` 的编排里 `yield return RunSafe("Domain.Behavior", AssertDomainBehavior(_ev, …));`（`RunSafe` 负责把异常记成 `infra_fail`）。
3. 跑 `Tools/QA/In-Editor/<套件>` 菜单，确认断言 ID 出现在该套件 `*_Assertions.jsonl`，计数与 Summary 的 `pass/fail` 对得上。
4. 不改别人的 ID、不改 `expected` 字面量、不动阈值——只新增。

## 6. 禁止事项（评审直接打回）

- 改判定表达式、改 `expected` 字面量、改阈值、改 `status` 归属让红灯变绿。
- 在断言体内写盘 / 读 `SessionState` / 碰 `EditorApplication` / 依赖 `QaLauncher`、`*_Boot`。
- 断言里现找“当前引导状态”（`GameObject.Find("xxx_Runner")`、读 runner 字段）来决定是否执行。
- 把夹具创建、清理、`Destroy` 放进断言（夹具归编排层）。
- 新增静态可变全局状态（`AGENTS.md` §10）；Qa 层只允许无状态 `static` 纯函数 + 使用者持有的实例。
- 为了压行数删注释、合并断言、去掉 `expected` 描述。
