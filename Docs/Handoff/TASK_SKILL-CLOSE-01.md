# SKILL-CLOSE-01 — 技能槽位接上 + 运动锁收口 + 旧 Bash/Skill 状态清除

> 长任务 `REFACTOR-LONG-01` 第二阶段第一步。上游依据：`C:\Users\lll\.opencode\plan\refactor-plan.md` §12.2 + 所有者 2026-11-06 追加的 D 项。
> 阶段计划：`Docs/Handoff/PLAN_REFACTOR-PHASE2.md`。
> 本单自包含；执行者只读本文件 + 仓库现状即可开工。

## §0 元信息

| 字段 | 值 |
|---|---|
| 任务 ID | `SKILL-CLOSE-01` |
| 风险等级 | **高风险**（含 Animator Controller 资产改动；已获所有者批准，见下） |
| 授权 | 所有者 2026-11-06：① 授权修改 4 条断言的**期望状态名**（阈值与其它数字不动）；② 授权删除 `PlayerBase.controller` 的 `Bash`/`Skill` 状态、`Locomotion→Bash`/`Locomotion→Skill` 过渡、`Bash`/`Skill` 触发器；③ 授权清扫代码/测试/DIAG 里的旧字符串 |
| 基线提交 | `896e7450`（M4；开工时以 `git rev-parse --short HEAD` 记录实际值） |
| 回滚锚 | `896e7450`；资产回滚 = `git checkout 896e7450 -- Assets/_Project/Art/Animations/PlayerBase.controller`（仅所有者执行） |
| Unity 模式 | **模式 B（编辑器常开 + MCP）**，Unity 2022.3.62f3c1 |
| 状态 | `Ready` |

> 执行者**不得**标 Done、不得改 `CURRENT.md`、**不得**做任何 git 写操作（提交由大脑执行）。

### 资产改动批准记录（AGENTS §3.3 例外通道）

- **批准人 / 时间**：所有者 / 2026-11-06。
- **批准内容**（原话摘要）：「追加第 4 步 D（在 A 跑绿之后执行）：删除 `PlayerBase.controller` 里 `Bash`/`Skill` 两个状态、`Locomotion→Bash`/`Skill` 两条过渡、`Bash`/`Skill` 两个触发器；并清扫代码/测试/DIAG 里的旧字符串。这是资产改动，所有者已批准。」
- **附带约束**：D 必须在 A 绿之后；删前导出 states/transitions/params 清单，删后再导一份，diff 证明只删了 Bash/Skill；`CharacterBrain.InSkillState()` 同步改判 `Q_1/W_1/E_1/R_1`；`Scripts/**`、`Tests/**`、`DIAG/**` 中 `"Bash"`/`"Skill"` 字符串 = 0；不新增 `Skill.NoLegacyState` 运行时断言，用静态 grep 证据替代。
- **大脑复核结论**：改动范围仅限上述状态/过渡/触发器与引用它们的代码字面量；`SkillSpeed` 参数、`Q_1/W_1/E_1/R_1` 状态、`Hit_A`/`Locomotion`/`Attack*` 均在「不许碰」清单内，且由前后导出 diff 兜底。**复核通过。**
- **执行者仍须**：动手前把《资产改动清单》（文件 / 字段或对象 / 理由 / 回滚方式 / 影响面）写进 REPORT，并在回执与 REPORT 中抄录上面的「谁批准 / 时间 / 批准内容」。

## §1 需求（Why / What）

**一句话目标**：让技能槽位真正驱动动画（Q→`Q_1`、W→`W_1`…）、把「放技能时停下」从直接写 `NavMeshAgent` 收成可复位的运动锁，并删掉已被取代的 `Bash`/`Skill` 旧状态，使「技能」在代码与控制器里只剩一套表达。

**用户故事**：作为所有者，我要在 M4.5 里把 Q/W 的 Clip 拖进 `OC_*` 就能直接生效，而不是发现代码还在播 `Bash`/`Skill` 旧状态。

**必须满足**

| 编号 | 需求 | 验收方式 |
|---|---|---|
| A-1 | `SkillSystem` 按 slot 生成 `(trigger, stateName)`：0→`Q`/`Q_1`、1→`W`/`W_1`、2→`E`/`E_1`、3→`R`/`R_1`；效果不再硬编码 `Bash`/`Skill` | 代码审查 + 断言 |
| A-2 | `CharacterBrain.InSkillState()` 改判 `Q_1/W_1/E_1/R_1`（技能期间的移动/攻击/交互锁不得静默失效） | 代码审查 + `Skill.InterruptInteract` |
| B-1 | 新增 `Scripts/Units/UnitMotion.cs`：`Begin(agent)` 记 `wasStopped` 后 `isStopped=true`（**不得 ResetPath**）；`End(agent)` 恢复原值（agent 为空/未上导航/已禁用时安全返回） | 代码审查 + `Skill.MotionUnlocked` |
| B-2 | `SkillEffects`（盾击）改调 `UnitMotion`；`CharacterBrain` 在离开技能状态时 `End()`；`DownedRescue` 在倒地/复活/`OnDisable` 各调一次 | 代码审查 + `Skill.MotionUnlocked` |
| C-1 | 补跑 DIAG ×1；确认已知红仅 `Diag.Input.FirstExit` | DIAG 证据 |
| C-2 | 把 `Docs/QA/Evidence/REFACTOR-M4/editor_run4/`（既有未提交）纳入本单提交范围 | `git status` |
| D-1 | 控制器删除 `Bash`/`Skill` 两个状态、`Locomotion→Bash`/`Locomotion→Skill` 两条过渡、`Bash`/`Skill` 两个触发器；`SkillSpeed` 与 `Q_1/W_1/E_1/R_1` 不动 | 前后导出 diff |
| D-2 | `Scripts/**`、`Tests/**`、`DIAG/**` 里字符串字面量 `"Bash"`、`"Skill"` 计数 = **0** | 静态 grep 证据 |
| D-3 | 4 条既有断言只改**期望状态名**（Bash→Q_1、Skill→W_1），阈值/时长/伤害/蓝耗等数字一字不动；测试里的状态名映射表同步补齐 `Q_1..R_1` | diff 审查 + REPORT 抄录改前/改后 |

**不在范围**

- 不改任何 `OC_*.overrideController`、不改 Q/W/E/R 的子状态机结构、不拖任何 Clip（M4.5 所有者做）。
- 不改技能数值（`range`/`hitTime`/`totalTime`/`damage`/`manaCost`/`cooldown`/`castDelay`/`flightDuration`）。
- 不改 `SkillSystem.Cast(int, Transform)` 签名；不改 `SkillEffectBase`/各效果的既有序列化字段名。
- 不新增运行时断言 `Skill.NoLegacyState`（用静态 grep 替代）；除 `Skill.MotionUnlocked` 外不新增断言。
- 不改输入层、不改交互（M3 已收口）、不改 UI。

**约束**

- 不得手改 `.controller`/`.prefab`/`.unity`/`.asset` 的 YAML（`AGENTS.md` §3.3）。控制器改动必须经 Unity 编辑器（MCP 或临时 Editor 工具），并留下前后导出。
- 若必须写临时 Editor 工具：允许新增 1 个 `Assets/_Project/Tests/QA/Editor/` 下的一次性菜单脚本；**运行成功后删除该脚本与其 `.meta`**，只保留导出证据，不得把一次性工具留在仓库里。
- Animator 字符串引用（`Interact`/`Interrupt`/`Hit`/`Die`/`Revive`/`Die`/`Hit_A`/`Locomotion`/`AttackMelee`/`Attack_Draw`/`Attack_Release`/`Downed`/`Rise`）**不得改**。
- `"Bash"`/`"Skill"` 的**类名/文件名/资产菜单名**（`BashEffect` 类、`SkillSystem` 类、`Dungeon/Skill Effects/Bash`）不算「字符串字面量」，不在 D-2 范围内；只清 `"Bash"` / `"Skill"` 这种引号字面量。

## §2 方案（Plan）

**备选方案**

| 方案 | 做法 | 优点 | 缺点 | 结论 |
|---|---|---|---|---|
| A1 | 效果各自声明触发器（现状） | 零改动 | Q 播 `Bash`，槽位与动画脱节 | 弃用 |
| A2 | `SkillSystem` 按 slot 查表生成 `(trigger, stateName)` | 新槽位天然对齐；效果不再关心动画名 | 表要维护 | **选定** |
| B1 | 每个技能自己写 `agent.isStopped` | 改动小 | 无复位点（现状缺陷） | 弃用 |
| B2 | 静态 `UnitMotion` 记原值 + 引用计数 | 任意路径都能 `End()`，不改预制体 | 多一个静态类 | **选定** |
| D1 | 保留旧 `Bash`/`Skill` 状态做兼容 | 最保守 | 两套表达长期并存，M4.5 拖 Clip 时容易拖错 | 弃用 |
| D2 | 删除旧状态与触发器，代码/测试同步 | 只剩一套 | 需资产改动（已批准） | **选定** |

**契约**

```csharp
// Scripts/Units/UnitMotion.cs —— 运动锁（不新增组件、不落盘）
public static class UnitMotion
{
    public static void Begin(UnityEngine.AI.NavMeshAgent agent);   // 记 wasStopped → isStopped = true（不 ResetPath）
    public static void End(UnityEngine.AI.NavMeshAgent agent);     // 恢复 Begin 前的 isStopped；未 Begin 过则 isStopped = false（安全复位）
    public static bool IsHeld(UnityEngine.AI.NavMeshAgent agent);  // 断言/排查用
}
```

- `SkillSystem` 内部：`static readonly (string trigger, string stateName)[] SlotAnim = { ("Q","Q_1"), ("W","W_1"), ("E","E_1"), ("R","R_1") };`；`ContextFor` 用 slot 查表；越界 slot 用 `(null, null)` 并由效果兜底（当前无越界用例）。
- `SkillEffectBase.Trigger`/`StateName` 保留为 `virtual`，默认返回 `null`（表示「用槽位表」）；`BashEffect` 的两个 override **删除**。
- `MeleeCombat.DoSkillAttack()` 增加 `DoSkillAttack(string trigger, string stateName)` 重载；`HeavySlashEffect` 改为传 `ctx.Trigger`/`ctx.StateName`；无参重载保留（字段 `skillStateName` 兜底，默认值改为 `"W_1"`），且 `ResetTrigger` 必须重置「本次实际用过的触发器」而不是写死 `"Skill"`。
- `BashEffect` 的执行体：`agent.isStopped = true; agent.ResetPath();` → `UnitMotion.Begin(agent)`（**不再 ResetPath**）；协程结束（含提前 return 路径）`UnitMotion.End(agent)`。
- `CharacterBrain.Update`：每个单位在「上一帧在技能状态、这一帧不在」时 `UnitMotion.End(agent)`。
- `DownedRescue`：`OnDowned()`、`Revive()`、`OnDisable()` 各 `UnitMotion.End(agent)`。

**数据流**

`按 Q → SkillSystem.Cast(0,target) → SlotAnim[0]=(Q,Q_1) → SkillCastContext{Trigger=Q,StateName=Q_1} → BashEffect.Execute → UnitMotion.Begin(agent) + SetTrigger("Q") → 结算 → UnitMotion.End(agent)`
`受击/倒地/复活/禁用 → UnitMotion.End(agent)`（任一路径都复位）

**影响面清单**

| 项 | 内容 |
|---|---|
| 新增文件 | `Assets/_Project/Scripts/Units/UnitMotion.cs`（+`.meta`）；**临时** `Assets/_Project/Tests/QA/Editor/*`（跑完删除，不入库） |
| 修改脚本 | `Units/SkillSystem.cs`、`Combat/SkillEffectBase.cs`、`Combat/SkillEffects.cs`、`Combat/HeavySlashEffect.cs`、`Combat/SkillEffectFireball.cs`（仅跟随签名/清理字面量）、`Combat/MeleeCombat.cs`、`Units/CharacterBrain.cs`、`Units/DownedRescue.cs` |
| 修改测试 | `Tests/REFACTOR/**`（4 条期望状态名 + 状态名映射补 `Q_1..R_1` + 新增第 16 条 `Skill.MotionUnlocked`）、`Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs`（仅 2 处期望状态 + 映射表）、`Tests/**/SkillCastFixture.cs`（known 状态列表）、`Tests/FND00B/Runtime/FND00B06_ProbeRunner.cs`（状态列表）、`Tests/DIAG/Runtime/DIAG_ProbeRunner.cs`（2 处 `expectedState`） |
| 资产（已批准） | `Assets/_Project/Art/Animations/PlayerBase.controller`：删 2 状态 / 2 过渡 / 2 参数；**不动** `SkillSpeed`、`Q_1..R_1`、其余过渡与参数 |
| 字符串引用 | 清 `"Bash"`/`"Skill"`；`Interact`/`Interrupt`/`Hit`/`Die`/`Revive` 等**不动** |
| 场景 / 预制体 / `.asset` | **零改动**（`.prefab`/`.unity`/`.asset` 一律不碰） |
| `.meta` | 新增脚本自带；临时 Editor 工具删除时连 `.meta` 一起删 |

**实施顺序（严格两段，D 必须在 A 全绿之后）**

1. **A**：`SkillSystem` 槽位表 + `SkillEffectBase`/`BashEffect` 去硬编码 + `CharacterBrain.InSkillState()` 改判 + `MeleeCombat` 技能触发器/状态名改为传入。
2. 同步把 4 条既有断言的期望状态名改为 `Q_1`/`W_1`，补齐 `StateName()` 映射（`Q_1/W_1/E_1/R_1`），`SkillCastFixture.known` 补 `Q_1..R_1`（此步只允许改状态名与列表，不改阈值）。
3. **B**：`UnitMotion` + 接线（`SkillEffects`/`CharacterBrain`/`DownedRescue`）；新增第 16 条 `Skill.MotionUnlocked`。
4. 跑第一轮：REFACTOR ×3（16/16）+ SPEC01 ×2（12/12）。**必须全绿才能进 D。**
5. **D**：导出控制器现状 → 删除 `Bash`/`Skill` 状态、`Locomotion→Bash`/`Skill` 过渡、`Bash`/`Skill` 触发器 → 再导出 → diff 证明只删这两套。
6. 清扫 `Scripts/**`、`Tests/**`、`DIAG/**` 的 `"Bash"`/`"Skill"` 字面量（来源已由前几步覆盖，这一步是兜底与核对）。
7. 跑第二轮：REFACTOR ×3（16/16）+ SPEC01 ×2（12/12）。
8. **C**：DIAG ×1；整理证据 + REPORT。

**风险与回滚**

- 最可能错：① 删状态时误删 `Q_1..R_1` 或 `SkillSpeed`；② `AnyState→Q` 的过渡 `m_DstState` 指向 Q_1（应保留）而被误删；③ `MeleeCombat` 的 `ResetTrigger` 仍写死旧名导致动画残留；④ `UnitMotion` 引用计数与 `DownedRescue.OnDisable` 双 End 造成提前解锁；⑤ 测试状态映射表改漏导致 `StateName()` 返回未知字符串。
  - 检测：控制器前后导出 diff + `Skill.QW` / `Skill.InterruptAttack` / `Skill.InterruptInteract` / `Skill.MotionUnlocked` + SPEC01 12 条 + DIAG 已知红。
- 修复轮次：**2 轮**（A/B 一轮、D 一轮各 1 次；用尽仍红按熔断停）。
- 回滚锚：`896e7450`（代码 + 控制器一起回滚）。

## §3 验证（Validation）

**AC 表**

| AC | Given / When / Then | 断言 ID / 证据 | 证据路径 |
|---|---|---|---|
| AC-1 | 给定骑士按 Q，则 `≤0.6s` 内进入 `Q_1`；按 W 则进入 `W_1` | `Skill.QW`（期望值已按批准改为 `Q_1`/`W_1`） | `Docs/QA/Evidence/SKILL-CLOSE-01/runA_*/` |
| AC-2 | 给定 A+B 完成后，REFACTOR 连跑 3 次，则每次 **16/16、fail=0、blocked=0、infra=0、exit=0** | 套件 16 条 | `SKILL-CLOSE-01/runA_1..3/` |
| AC-3 | 给定 A+B 完成后，SPEC01 跑 2 次，则每次 **12/12、fail=0、exit=0** | SPEC01 12 条 | `Docs/QA/Evidence/SPEC-01/editor_run<N>/` |
| AC-4 | 给定技能结束 / 技能被受击打断 / 倒地后复活三种路径，则 `agent.isStopped == false` 且单位能移动 | `Skill.MotionUnlocked` | `SKILL-CLOSE-01/runA_1..3/` JSONL |
| AC-5 | 给定 D 完成后，REFACTOR 连跑 3 次 **16/16**、SPEC01 跑 2 次 **12/12**，全 `fail=0` | 两套全量 | `SKILL-CLOSE-01/runD_1..3/`、`SPEC-01/editor_run<N>/` |
| AC-6 | 给定控制器改动，则前后导出的 states/transitions/params diff **只含** Bash/Skill 两套删除，`Q_1..R_1`、`SkillSpeed` 与其它节点完全不变 | `controller-states-before.txt` / `-after.txt` / `-diff.txt` | `SKILL-CLOSE-01/` |
| AC-7 | 给定 `Scripts/**`、`Tests/**`、`DIAG/**`，当 grep 字符串字面量 `"Bash"`、`"Skill"`，则计数 = **0**（注释不计；说明过滤方式） | `grep-legacy-strings.txt` | `SKILL-CLOSE-01/` |
| AC-8 | 给定 DIAG 跑 1 次，则通过数 = 基线、已知红仅 `Diag.Input.FirstExit`，无新增红 | DIAG Summary | `Docs/QA/Evidence/SKILL-CLOSE-01/diag_run1/` |
| AC-9 | 给定 `git status`，则只含本单声明文件 + 既有脏项（`Assembly-CSharp*.csproj`、`DIAG-SKILL/editor_run3/`、`FND-00B-06/editor_run4/`），且 `REFACTOR-M4/editor_run4/` 已纳入；无 `.prefab`/`.unity`/`.asset` 改动 | `git-diff-stat.txt` | `SKILL-CLOSE-01/` |
| AC-10 | 给定 4 条被改断言，则 REPORT 抄录**改前原文 / 改后原文 / 只改状态名**，并注明「所有者 2026-11-06 批准」 | REPORT §2 + `assertion-diff.txt` | `SKILL-CLOSE-01/` |

**运行次数**：REFACTOR 两轮各 **N=3**；SPEC01 两轮各 **2 次**；DIAG **1 次**。同一入口连续执行，不得用 `run_tests`、不得 batchmode。

**新增断言（仅 1 条，15 → 16）**

`Skill.MotionUnlocked`（Play 模式，夹具用完销毁，不得污染后续断言）：

1. 骑士对靶施放 Q → 等技能结束（离开 `Q_1` 且回 `Locomotion`）→ `agent.isStopped == false`，`UnitMotion.IsHeld(agent) == false`；
2. 施放 W 后 0.1s 内对施法者 `TakeHit(1f)` → 进入 `Hit_A` 后 `≤2s` 内 `agent.isStopped == false`；
3. 让骑士倒地（`UnitHealth` 致死入口）→ 复活 → `agent.isStopped == false`；
4. 最后再下一次 `CommandMove` 断言位移 > 0.1m 且 `UnitMotion.IsHeld == false`（证明真的能走）。

**必须保存的证据**：两轮 REFACTOR/SPEC01 的 JSONL/Summary/ExitCode/InEditorResult、DIAG 同上；`controller-states-{before,after,diff}.txt`、`grep-legacy-strings.txt`、`assertion-diff.txt`、`git-diff-stat.txt`、`asset-change-list.txt`（《资产改动清单》）。

## §4 执行信息

| 项 | 内容 |
|---|---|
| 前置 | Unity 已开（模式 B）、MCP 单实例、`is_compiling=false`、场景 `DungeonWorld` 且非脏 |
| 允许改动 | 上表「修改脚本」8 个 + 新增 `Units/UnitMotion.cs`；上表「修改测试」5 个文件（仅状态名/列表/新增断言）；`PlayerBase.controller`（已批准，仅删两套旧节点）；`Docs/Handoff/REPORT_SKILL-CLOSE-01.md`；`Docs/QA/Evidence/SKILL-CLOSE-01/**`、新增 SPEC-01 与 DIAG 证据目录 |
| 允许读取 | 本工单 + 被改文件现状 + 控制器 YAML（只读导出用） |
| 绝不触碰 | `Assets/_Project/Art/**` 除 `PlayerBase.controller` 外的任何文件、`Assets/Scenes/**`、`Prefabs/**`、`*.asset`、`*.overrideController`、`ProjectSettings/**`、`Packages/**`、`AGENTS.md`、`Docs/Handoff/CURRENT.md`、其他 `TASK_*`/`REPORT_*`、既有断言中除上述 4 条**期望状态名**以外的任何内容 |
| 证据目录 | `Docs/QA/Evidence/SKILL-CLOSE-01/`（两轮分 `runA_*` / `runD_*`） |
| 熔断 | `AGENTS.md` §6 + 计划书 §6；修复轮次 **2 轮**（A/B 一次、D 一次） |
| 执行预算 | `.cs` 文件数 ≤ 12（不含临时 Editor 工具）；不重启 Unity；不跑全量 batch |

**停止条件**：需要改 `OC_*`/预制体/场景/其它 `.asset` 才能继续；删除旧状态会连带破坏 `Q_1..R_1` 或 `SkillSpeed`（此时停并写现场，不自行重建控制器）；编译连续 2 轮红；修复轮用尽仍红；`git status` 对不上。

## §5 学习产出

| 项 | 要求 |
|---|---|
| 讲稿 | 不单独写：本单是 `REFACTOR-LONG-01` 讲稿的一节，执行者在 REPORT §3 给出「三句话版本」，并给 `Docs/Learning/REFACTOR-LONG-01_讲稿.md` 补一段「阶段二：槽位接上与运动锁」的建议内容（不直接改该文件，交给大脑合并） |
| 三句话版本 | 写进 REPORT §3 第 7 条 |

## 自包含声明

本工单不依赖 `CURRENT.md` 或其他任务书即可执行；信息不足时在 REPORT 写“信息不足”并列缺项，**不**自行翻阅其他文档补全。
