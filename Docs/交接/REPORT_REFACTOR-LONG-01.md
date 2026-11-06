# REPORT — REFACTOR-LONG-01：8 小时无人值守重构（M0–M4）汇总与醒来目检

> 长任务计划书：`C:\Users\lll\.opencode\plan\refactor-plan.md`（所有者 2026-11-05 批准）。
> 本文件是收口汇总：M0–M4 各自做了什么、验收数字、依赖关系、以及给所有者的《醒来目检清单》。
> 执行者**未**做任何 git 写操作；`CURRENT.md` 未改；任务未标 `Done`（只有所有者能标）。
> 收口时 HEAD：`896e7450`（M4 已本地提交；M0–M4 五个提交齐全，未 push）。

## 1. M0–M4 总览

| 里程碑 | 提交 | 范围（计划书 §2） | 验收数字（来源） | 证据目录 |
|---|---|---|---|---|
| M0 护栏扩容 | `ed0cdc17` | 新增 `Tests/REFACTOR/**`（探针 + 编辑器入口）+ 菜单 `Tools/QA/In-Editor/REFACTOR`；不改产品脚本 | REFACTOR **8/8 ×3** ＋ SPEC01 12/12（里程碑提交信息 / `REPORT_REFACTOR-M0.md`） | `Docs/QA/Evidence/REFACTOR-M0/` |
| M1 契约层 + 单位注册 | `0a1a45d7` | 新增 `Scripts/Core/UnitRegistry.cs`、`UnitId.cs`；17 处 `FindObjects*` 收敛为注册表查询，只改调用点 | REFACTOR **9/9 ×4** ＋ SPEC01 12/12 ×2（同上） | `Docs/QA/Evidence/REFACTOR-M1/` |
| M2 生命周期拆分 | `1961c93b` | `GameData→RunState/MetaState/MessageFeed`、`GameFlow→AppFlow/RunFlow`、`PauseManager→AppFlow`；旧类型保留为转发适配层 | REFACTOR **10/10 ×4** ＋ SPEC01 12/12 ×3（同上） | `Docs/QA/Evidence/REFACTOR-M2/` |
| M3 统一交互 | `d02470c4` | 新增 `Scripts/Gameplay/Interaction/**`；宝箱/救援/门实现 `IInteractionTarget`，不再自持 `Input`/`SelectionManager`/UI；单位侧 `InteractionRunner` 运行时挂载 | REFACTOR **11/11 ×4** ＋ SPEC01 12/12 ×3（`REPORT_REFACTOR-M3.md`） | `Docs/QA/Evidence/REFACTOR-M3/`、`SPEC-01/editor_run29..31/` |
| M4 技能与战斗收口 | `896e7450` | `SkillCastContext` + 三个效果改签名 + `SkillSystem` 组包；`UnitStats.skillSpeed=1`（时长 = 原值/倍率）；`Projectile.ComputeEnd` 改为渲染包围盒中心；测试 11→15 | REFACTOR **15/15 ×3** ＋ SPEC01 12/12 ×2（`REPORT_REFACTOR-M4.md`） | `Docs/QA/Evidence/REFACTOR-M4/`、`SPEC-01/editor_run32..33/` |

> M0–M2 的数字取自各里程碑的本地提交信息与 `REPORT_REFACTOR-M<0..2>.md`（本次只做定向提取，未重跑、未改动）。M3 数字来自 `REPORT_REFACTOR-M3.md`。M4 数字为本单实测。

## 2. 总验收对照（计划书 §1）

| # | 总验收项 | 结论 | 依据 |
|---|---|---|---|
| 1 | SPEC01 编辑器内 12/12、fail=0、exit=0 | **满足**（M4 收口实测 `editor_run32`/`editor_run33`） | `Docs/QA/Evidence/SPEC-01/editor_run32..33/` |
| 2 | `QA_REFACTOR` 套件全绿 | **满足**（M0 8 条 → M1 9 条 → M2 10 条 → M3 11 条 → M4 15 条，每一代都全绿） | 各 `REFACTOR-M*/` 证据 |
| 3 | 已知红基线不增加（`Diag.Input.FirstExit` 仍为 `NO_PRIMARY`） | **未验证，待补跑 DIAG**（M4 未跑 DIAG；Done 前需补跑 1 次，确认已知红仅此一条、未新增） | `REPORT_REFACTOR-M4.md` §7（未运行项） |
| 4 | `git status` 只含本里程碑声明的文件 | **满足**（见 §4 第 4 项 git log 核对清单、第 6 项既有脏项，以及 `REFACTOR-M4/git-diff-stat.txt`） | 同上 |
| 5 | 证据齐全 `Docs/QA/Evidence/REFACTOR-M<x>/` | **满足**（M0–M4 均有断言 JSONL/Summary/ExitCode/InEditorResult；M4 另有 diff 证据） | 各目录 |

## 3. M0–M4 依赖关系（文字版）

```text
M0 行为护栏（REFACTOR 套件 + 菜单）
 └─ 给 M1–M4 每一步提供“行为不变”的判据（每加一个里程碑，断言条数 +1，只增不改口径）

M1 单位注册表/契约（UnitRegistry + UnitId）
 ├─→ M2 状态拆分：拆分时用它替代全场扫描（避免门面层再引入 FindObjects）
 ├─→ M3 交互：Runner 的目标登记表、优先级的“按单位取 brain”都建立在“能按契约找到单位”之上
 └─→ M4 技能：施法者/目标解析不再靠场景扫描（SkillCastContext.CasterUnit 直接给）

M2 状态/流程门面（RunState/MetaState/MessageFeed/AppFlow/RunFlow）
 ├─→ M3 交互：暂停门禁（AppFlow.IsPaused）与提示（MessageFeed）走门面
 └─→ M4 技能：施放失败提示（MessageFeed）不变；暂停时不推进技能协程（Time.timeScale=0 的既有语义）

M3 统一交互（IInteractionTarget + InteractionRunner）
 └─→ M4 技能：技能期间“交互指令无效”的门禁 `CharacterBrain.CanAcceptInteract` 被第 7 条断言直接覆盖

M4 技能上下文 + 瞄准点（SkillCastContext / AimPoint / skillSpeed）
 └─ 依赖上面全部：施法者与目标来自注册表体系、上下文零资产改动、断言建立在 M0 夹具与 M3 交互门禁之上
```

一句话：**M0 是尺子，M1 是名册，M2 是账本，M3 是手脚，M4 是招式**；后一层只使用前一层的公开契约，没有回改上层内部实现。

## 4. 醒来目检清单（计划书 §8 模板 + 本任务补充）

1. **打开 Unity**：确认 Console 无 `Missing Script` / `Missing Reference`；`Assets/_Project/Scripts/Combat/SkillCastContext.cs.meta` 已被 Unity 识别（Project 窗口能看到该脚本，不是 “No MonoBehaviour scripts”）。
2. **跑一次 `Tools/QA/In-Editor/SPEC01 (green)`**：确认 12/12、fail=0、exit=0（证据目录会是 `SPEC-01/editor_run34/`）。
3. **手动试**（顺序建议，覆盖 M0–M4 全部行为）：
   - 选中角色移动（M0/M1 注册表）、右键普攻一名怪物（M0）；
   - 骑士按 Q（盾击）：Animator 应进入 `Q_1`（若仍进 `Bash`，说明槽位映射未生效）；按 W 应进入 `W_1`；伤害/击退/僵直正常；
   - 骑士放技能时点宝箱：不应走过去、不应开箱；技能结束后再点：正常走过去开箱（M3/M4 门禁）；
   - 骑士普攻中被打 → `Hit_A`；骑士放技能中被打 → `Hit_A`（M4 第 6 条）；
   - 法师/弓手普攻：弹道应打在怪物模型身体中心（M4 瞄准点）；
   - 倒地 → 救援读条 → 受击打断 → 重新下令救起（M0/M3）；
   - 暂停（Esc）后恢复：提示与移动都正常（M2）。
4. **看 `git log --oneline -8`**：M0–M3 四个提交（`ed0cdc17` / `0a1a45d7` / `1961c93b` / `d02470c4`）应各只含该里程碑声明文件；M4 提交后同样核对（清单见 `REPORT_REFACTOR-M4.md` §4）。
5. **确认 `Docs/Handoff/CURRENT.md` 未被本任务修改**（执行者禁止），且 `Docs/Handoff/TASK_REFACTOR-M4.md`、本报告、`REPORT_REFACTOR-M4.md`、`Docs/Learning/REFACTOR-LONG-01_讲稿.md` 都在。
6. **开工前既有脏项**（不是本次改动，别误删）：`Assembly-CSharp*.csproj`、`Docs/QA/Evidence/DIAG-SKILL/editor_run3/`、`Docs/QA/Evidence/FND-00B-06/editor_run4/`、`Docs/Handoff/TASK_REFACTOR-M4.md`。

## 5. 遗留风险 / 未决项（跨里程碑汇总）

| 项 | 来源 | 状态 / 需要谁决定 |
|---|---|---|
| `Bash` 触发过渡只挂在 `Locomotion` 状态：出招中放 Q 的动画要等普攻收招才切 Bash（代码侧判定已即时收掉） | M4 静态审查（`PlayerBase.controller`） | 既有资产设计；要改成“出招中直接切”必须动控制器，**待所有者决定** |
| `BashEffect` 仅置 `NavMeshAgent.isStopped=true`，全仓无复位点 | M4 静态审查（`SkillEffects.cs:51`，`grep isStopped` 唯一命中） | 既有实现，本单未改；需实机确认“盾击后单位是否还能移动”，如不能则是既有缺陷 |
| 场景内没有 `Chest` / `DungeonDoor` 实例 → 这两类交互只能用运行期夹具断言，门的 F 交互无法人工目检 | M3 | 待所有者在需要地牢入口的场景/预制体上挂 `DungeonDoor` 后目检（属资产改动） |
| 点击分发跳过 `IsAvailable == false` 的目标（点已开宝箱不再走过去） | M3 | 已按 TASK 语义实现；若想保留旧观感需所有者决定 |
| 暂停期间不再重写门提示（原实现在暂停时也写同一句） | M3 | 正常游玩无差异，记录备查 |
| `Projectile.AimPoint` 每帧 `GetComponentsInChildren<Renderer>()` 有数组分配 | M4 | 同屏弹道少，暂不优化；量大时改缓冲复用 |
| `skillSpeed ≠ 1` 无资产/无用例 | M4 | 本单禁改 `.asset`；由代码审查（除法）保证，未来加“急速”属性时补用例 |

## 6. 建议提交（M4，单提交）

```text
重构：M4 技能与战斗收口（SkillCastContext + 效果接口化 + 子弹瞄准渲染包围盒中心；skillSpeed 默认 1；REFACTOR 15/15 x3 + SPEC01 12/12 x2）
为什么：把"一次施放要准备什么"收敛成 SkillCastContext 传给效果，新增技能不必再猜 Animator 名与来源；子弹落点从写死的 position+0.6m 改为目标渲染包围盒中心；零资产改动，行为由 11+4 条 REFACTOR 与 12 条 SPEC01 兜底。
```

属于本单的工作区条目（GitHub Desktop 应只看到这些 + §4 第 6 项的既有脏项）：

- 修改：`Assets/_Project/Scripts/Combat/{SkillCastContext.cs(新),SkillEffectBase.cs,SkillEffects.cs,SkillEffectFireball.cs,HeavySlashEffect.cs,Projectile.cs}`、`Assets/_Project/Scripts/Units/{SkillSystem.cs,UnitStats.cs}`、`Assets/_Project/Tests/REFACTOR/{Editor/REFACTOR_BatchAcceptance.cs,Runtime/REFACTOR_ProbeRunner.cs}`
- 新增：`Assets/_Project/Scripts/Combat/SkillCastContext.cs.meta`、`Docs/QA/Evidence/REFACTOR-M4/**`、`Docs/QA/Evidence/SPEC-01/editor_run32..33/**`、`Docs/Handoff/REPORT_REFACTOR-M4.md`、`Docs/Handoff/REPORT_REFACTOR-LONG-01.md`、`Docs/Learning/REFACTOR-LONG-01_讲稿.md`

## 7. 运行参数与消耗

- Unity 模式 B（编辑器常开 + `unityMCP`），实例 `Dungeon@4f5c34c82f31646e`，Unity 2022.3.62f3c1，场景 `DungeonWorld`。
- 未使用 `run_tests`、未使用 batchmode、未重启 Unity、未执行任何 git 写操作。
- 钱包：`Go 订阅`；模型完整 ID：`opencode-go/deepseek-v4.1-flash`。
- M4 编辑器内菜单执行 7 次：REFACTOR ×5（体检 1 + 修复轮 1 + 验收 3）、SPEC01 ×2。
