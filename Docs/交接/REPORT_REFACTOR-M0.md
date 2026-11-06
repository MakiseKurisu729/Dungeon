# REPORT — REFACTOR-M0：护栏扩容（REFACTOR 套件建立）

> 对应 `Docs/Handoff/TASK_REFACTOR-LONG-01` 的 M0 小节（计划书 `C:\Users\lll\.opencode\plan\refactor-plan.md` §2/§9）。
> 长任务模式：执行者不标 Done、不改 `CURRENT.md`；全部进《醒来目检清单》。

## 1. 结果摘要

`完成` —— 新增 `REFACTOR` 护栏套件（8 条断言）并接入 `Tools/QA/In-Editor/REFACTOR`；**8/8 ×3 连续全绿**（exit 0），`SPEC01` 同轮 **12/12、fail=0、exit=0**；未改动任何产品脚本。

## 2. AC 回填表

| AC | 结果 | 断言 ID | 关键数据 | 日志路径 |
|---|---|---|---|---|
| AC-1 新套件 N/N（run2） | 通过 | 全部 8 条 | pass=8 / fail=0 / blocked=0 / infra=0 | `Docs/QA/Evidence/REFACTOR-M0/editor_run2/` |
| AC-1 新套件 N/N（run3） | 通过 | 全部 8 条 | pass=8 / fail=0 / blocked=0 / infra=0 | `Docs/QA/Evidence/REFACTOR-M0/editor_run3/` |
| AC-1 新套件 N/N（run4） | 通过 | 全部 8 条 | pass=8 / fail=0 / blocked=0 / infra=0 | `Docs/QA/Evidence/REFACTOR-M0/editor_run4/` |
| AC-2 SPEC01 12/12 | 通过 | SPEC01 全 12 条 | pass=12 / fail=0 / blocked=0 / infra=0 / exit=0 | `Docs/QA/Evidence/SPEC-01/editor_run23/` |
| AC-3 已知红基线不增加 | 通过 | — | 本里程碑未跑 DIAG（计划要求“红数不增加”，未新增断言、未改产品） | `REFACTOR-M0/baseline/07_DIAG_Summary_skill2.json` |
| AC-4 git status 只含声明文件 | 通过 | — | 见下“改动文件清单”；`Assembly-CSharp*.csproj` 为 Unity 生成，未纳入提交 | `REFACTOR-M0/git-diff-stat.txt` |
| AC-5 证据齐全 | 通过 | — | 见 §5 | `Docs/QA/Evidence/REFACTOR-M0/` |

- **运行次数**：时序/交互类需 N≥3，已连续 3 次（editor_run2/3/4）全绿；每次逐条数据见各自 `REFACTOR_Assertions.jsonl`。
- `editor_run1` 为首跑，暴露**测试自身**缺陷（`AssertRangedShot` 忘记回写 `_target`，infra_fail），已修，不计入 N/N 计数。

### 逐条实测（run4）

| 断言 | 实测 |
|---|---|
| `Party.PresentAndMove` | 三角色全部到达，peakAgentSpeed/peakSpeedZ=4.0，移动中状态 `Locomotion`，停稳归零，Grounded=true |
| `Combat.MeleeHit` | hits=1；命中瞬间 `AttackMelee`，normalizedTime=0.352，伤害 15，耗时 0.394s |
| `Combat.RangedShot` | Archer 发射于 `Attack_Release` nt=0.340，伤害 15；Mage 发射于 `Attack_Release` nt=0.202，伤害 28 |
| `Skill.QW` | Q=`BashEffect`、蓝 20、0.056s 进 `Bash`、掉血 15；W=`HeavySlashEffect`、蓝 30、0.055s 进 `Skill` |
| `Hit.InterruptAttack` | 出招中受 1 伤 → **0.054s** 进 `Hit_A`，hp 100→99 |
| `Downed.Knockdown` | **0.105s** 进 `Downed`，IsDown=true，brain/agent 均停用 |
| `Rescue.ChannelInterrupt` | 0.519s 进读条；受击立即中断且仍倒地；重新下令后 5.006s 救起，hp=1，大脑恢复 |
| `Chest.Interact` | 0.512s 开箱；盖角 110°；内容物显形；已写入宝箱背包 |

## 3. 实现原理（面向所有者）

1. **目标**：M1–M4 要改生产代码，必须先有一条**独立于 SPEC01 的行为护栏**。SPEC01 锁的是“具体数值口径”（阈值、窗口、归一化时间），重构时容易被合理的实现变化打红；本套件只锁“行为还在不在”。
2. **为什么另建一套而不是扩 SPEC01**：计划 §9-M0 明确“复制结构但不复制断言数值”。REFACTOR 与 SPEC01 共用 Play 模式夹具思路，但断言口径独立，避免两套互相污染，也避免改 SPEC01 触发“改现有断言口径”的红线。
3. **数据流**：菜单 `Tools/QA/In-Editor/REFACTOR` → `QA_InEditorMenu.Run("REFACTOR")` → preflight（等编译/刷新/域重载稳定）→ 注入 `REFACTOR_RUN`/`REFACTOR_MILESTONE`/`QA_EVIDENCE_SUBDIR` → `REFACTOR_BatchAcceptance.Run()` 打开 DungeonWorld 并进 Play → `REFACTOR_Boot` 建 `REFACTOR_Runner` → 8 条断言 → 写证据 → 退出 Play → `TryFinish` 落 `InEditorResult.txt`。
4. **关键代码位置**：
   - `Assets/_Project/Tests/REFACTOR/Runtime/REFACTOR_ProbeRunner.cs:206` —— `Party.PresentAndMove`：走 `SelectionManager.ForEachSelectedBrain → CharacterBrain.CommandMove`（与右键同一生产入口）。
   - `Assets/_Project/Tests/REFACTOR/Runtime/REFACTOR_ProbeRunner.cs:362` —— `Combat.RangedShot`：用反射读 `RangedCombat._fired` 的上升沿，只在“发射瞬间”采样动画状态。
   - `Assets/_Project/Tests/REFACTOR/Runtime/REFACTOR_ProbeRunner.cs:627` —— `Rescue.ChannelInterrupt`：`DownedRescue.CommandRescue`（选择器点击救援同一入口）→ 读条 → 受击 → 重新下令 → 救起。
   - `Assets/_Project/Tests/REFACTOR/Runtime/REFACTOR_ProbeRunner.cs:721` —— `Chest.Interact`：运行期内存宝箱（`Chest`+`Interactable`+`Inventory`+`ChestItem`），走 `CharacterBrain.CommandInteract`。
   - `Assets/_Project/Tests/QA/Editor/QA_InEditorMenu.cs:73` / `:474` —— 菜单入口与 REFACTOR 证据根目录（`REFACTOR-M<x>`）接线。
5. **边界 / 失败模式**：
   - 套件会**临时停用场景内 MonsterBrain 单位**（`SuppressMonsters`，结束恢复），否则自动索敌会把“近战/远程/救援”三条断言搅乱；这是测试隔离，不改资产。
   - 战士/法师的主动普攻会真实开宝箱 UI，结束前 `HideInventory` 关闭，避免污染后续。
   - 若某条前置断言失败（如骑士没倒地），后续救援断言按 `blocked` 记录并给原因，不静默跳过。
6. **与计划的差异**：无功能差异。仅一处实现选择：`Skill.QW` 复用了现有 `SkillCastFixture`（同一测试程序集内的夹具），没有为了“独立”再复制一份 400 行装配代码。

## 4. 受影响模块导航

| 模块 | 职责 | 入口 |
|---|---|---|
| REFACTOR 运行期探针 | 8 条行为护栏 | `REFACTOR_ProbeRunner`（MonoBehaviour，Play 模式） |
| REFACTOR 证据 | 断言 JSONL / Summary / ExitCode | `REFACTOR_Evidence`、`REFACTOR_Json` |
| REFACTOR 编辑器入口 | 打开场景 → 进 Play → 收尾 | `REFACTOR_BatchAcceptance.Run()`、`REFACTOR_Boot` |
| 编辑器菜单 | 统一驱动四套探针 | `QA_InEditorMenu.Run("REFACTOR")` |

数据流：`菜单 → preflight → EnterPlaymode → Boot 建 Runner → 断言 → 证据 → ExitPlay → TryFinish`

**改动文件清单**

| 文件 | 改了什么 |
|---|---|
| `Assets/_Project/Tests/REFACTOR/Runtime/REFACTOR_ProbeRunner.cs` | 新增：8 条断言 + 夹具 + 收尾 |
| `Assets/_Project/Tests/REFACTOR/Runtime/REFACTOR_Evidence.cs` | 新增：断言与证据写入（自带 JSON 序列化） |
| `Assets/_Project/Tests/REFACTOR/Editor/REFACTOR_BatchAcceptance.cs` | 新增：编辑器/batch 入口 + Boot + `DefaultMilestone` |
| `Assets/_Project/Tests/REFACTOR/**/*.meta`、`Tests/REFACTOR.meta` | Unity 自动生成的 `.meta`（新增脚本自带，属计划允许） |
| `Assets/_Project/Tests/QA/Editor/QA_InEditorMenu.cs` | 仅加 1 个菜单项 + REFACTOR 环境变量/证据目录分派（+26 行） |
| `Docs/QA/Evidence/REFACTOR-M0/**` | 新增证据（baseline + editor_run1..4 + diff-stat） |
| `Docs/QA/Evidence/SPEC-01/editor_run23/**` | 本里程碑 SPEC01 验收证据 |

**未改动**：任何 `Assets/_Project/Scripts/**`、场景、预制体、Animator、`.asset`、`ProjectSettings`、`Packages`。

## 5. 证据清单

| 文件 | 内容 |
|---|---|
| `Docs/QA/Evidence/REFACTOR-M0/editor_run2..4/REFACTOR_Assertions.jsonl` | 逐条断言（含 expected/actual/data） |
| `Docs/QA/Evidence/REFACTOR-M0/editor_run2..4/REFACTOR_Summary_run1.json` | 汇总计数 |
| `Docs/QA/Evidence/REFACTOR-M0/editor_run2..4/REFACTOR_ExitCode_run1.txt` | 退出码（0） |
| `Docs/QA/Evidence/REFACTOR-M0/editor_run2..4/InEditorResult.txt` | 编辑器内收尾回执 |
| `Docs/QA/Evidence/REFACTOR-M0/baseline/` | 开工前三套 canonical Summary 副本（对照用） |
| `Docs/QA/Evidence/REFACTOR-M0/git-diff-stat.txt` | 本次 diff 与 `git status` 快照 |
| `Docs/QA/Evidence/SPEC-01/editor_run23/` | SPEC01 12/12 证据 |

## 6. 学习讲稿

不适用（M0 是测试护栏，非 feature / 非高风险；M4 收口后统一写重构讲稿）。

## 7. 未决项

- `editor_run1` 的 infra_fail 保留在案（测试缺陷已修），若要“零瑕疵证据”可由所有者决定是否删除该目录。
- 后续 M1–M4 每进一个里程碑需把 `REFACTOR_BatchAcceptance.DefaultMilestone` 改为对应 `M<x>`，否则证据会写进 `REFACTOR-M0`。
- 目检：由所有者在 Unity 里点一次 `Tools/QA/In-Editor/REFACTOR`，确认菜单可见、跑完不退出编辑器。

## 8. 建议提交

```text
重构：M0 建立 REFACTOR 行为护栏套件（8/8 ×3 + SPEC01 12/12）
为什么：M1–M4 要改生产代码，先把“行为还在不在”锁成可重复回归，避免重构期靠肉眼验收。
```

## 9. 消耗记录

- 钱包：`Go 订阅`
- 模型完整 ID：`opencode-go/deepseek-v4.1-flash`
