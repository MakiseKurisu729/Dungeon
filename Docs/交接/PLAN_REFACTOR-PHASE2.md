# PLAN — REFACTOR 第二阶段（SKILL-CLOSE-01 → M4.5 预制体基座 → M5/M6）

> 来源：`C:\Users\lll\.opencode\plan\refactor-plan.md` §12（所有者 2026-11-06 追加）。
> 本文件是 §12 的仓库版本，供后续会话读取；与上游冲突时以上游计划书为准。
> 第一阶段（M0–M4）已收口：`ed0cdc17` / `0a1a45d7` / `1961c93b` / `d02470c4` / `896e7450`，未 push。

## 0. 交接状态（2026-11-06）

| 项 | 值 |
|---|---|
| 分支 | `docs/fnd-00-baseline` |
| 第一阶段 HEAD | `896e7450`（M4 技能与战斗收口） |
| Unity | `2022.3.62f3c1`（模式 B：编辑器常开 + `unityMCP`，实例 `Dungeon@4f5c34c82f31646e`） |
| 护栏套件 | `Tools/QA/In-Editor/REFACTOR`（15 条）、`Tools/QA/In-Editor/SPEC01 (green)`（12 条）、`Tools/QA/In-Editor/DIAG`（6 条，已知红 1） |
| 既有脏项（非本轮改动） | `Assembly-CSharp*.csproj`、`Docs/QA/Evidence/DIAG-SKILL/editor_run3/`、`Docs/QA/Evidence/FND-00B-06/editor_run4/`、`Docs/QA/Evidence/REFACTOR-M4/editor_run4/`（后者待随 SKILL-CLOSE-01 提交） |

所有者决定：**不做通知插件**；顺序为「派单 → 所有者做 M4.5 预制体基座 → 确认 → M5/M6」。

## 1. 当前缺口（M0–M4 审核结论）

1. **槽位没有真正接上**：`SkillEffects.cs` 的 `BashEffect` 仍返回 `Trigger="Bash"`、`StateName="Bash"`，Q 播的还是旧 `Bash` 状态，不是 `Q_1`。M4 只做了 context 管道。
2. **技能效果直接控制导航**：`SkillEffects.cs` 里 `agent.isStopped = true; agent.ResetPath();` 全仓无复位点；实测 W 收招后能移动，但「打断 / 倒地 / 复活」路径未覆盖，且直接改 `NavMeshAgent` 违反 `Docs/01` §2.2 债务表与 §1.4 不变量第 10 条。
3. **DIAG 自 2026-11-05 23:52 后未跑过**；Done 前需补 1 次，确认已知红仅 `Diag.Input.FirstExit`。
4. `REFACTOR-M4/editor_run4` 未提交。

## 2. 下一步：`SKILL-CLOSE-01`（派单给执行者）

**目标**：技能槽位真正生效 + 运动锁收口 + 收口验证。

| 项 | 改动 | 验收 |
|---|---|---|
| A 槽位映射 | `SkillSystem` 按 slot 生成 `(trigger, stateName)`：0→`Q/Q_1`、1→`W/W_1`、2→`E/E_1`、3→`R/R_1`；效果不再硬编码 `Bash`/`Skill`（保留 override 作为兜底） | Q 按下去 Animator 进入 `Q_1`；W 进入 `W_1` |
| B 运动锁 | 新增 `Scripts/Units/UnitMotion.cs`：`Begin()` 记 `wasStopped` 并 `isStopped=true`（**不 ResetPath**）；`End()` 恢复。`SkillEffects` 改为调它；`CharacterBrain` 在离开技能状态时 `End()`；`DownedRescue` 在倒地/复活/OnDisable 各调一次 | 放完技能、被打断、倒地复活后 `agent.isStopped==false` 且能移动 |
| C 收口 | 跑 DIAG ×1；提交 `REFACTOR-M4/editor_run4` | 已知红不增加 |

**验收**：REFACTOR **16/16**（新增 `Skill.MotionUnlocked`）×3、SPEC01 12/12 ×2、DIAG 1 次；不碰资产、不改旧断言（**见 §5 待裁决**）。

**停止条件**：需要改控制器/预制体；REFACTOR 修 1 轮仍红；编译连续 2 轮红。

## 3. 之后：M4.5 预制体基座（所有者亲自做，执行者不碰）

执行者在 `SKILL-CLOSE-01` 绿后，大脑产出两份材料：

1. **组件归属表**（单位公共 / 玩家公共 / 职业专属）。注意 `Archer_Unit`/`Mage_Unit` 是 `Knight_Unit` 的 Variant，`Knight_Unit` 上的 `Interactable`（救援用）会被三职业一起继承；拆基座前必须逐组件定归属。
2. **迁移检查清单**：每迁一个职业，核对模型、`UnitStats`、`OC_*.overrideController`、武器引用、`Interactable`、`DownedRescue`、动画参数；迁完立刻跑 REFACTOR + SPEC01。

顺序：建 `PlayerUnit_Base`（只放公共组件）→ `Knight_Unit` 改挂基座 → 验证 → `Archer` → 验证 → `Mage` → 验证。一次只动一个职业。

## 4. 再之后：M5 / M6

- **M6（推荐先做，可无人值守）**：池化契约、事件订阅、面板注册。
- **M5（所有者在线时做）**：AI 感知/决策/执行拆分；当前 AI 行为缺断言，夜间跑风险高，建议先补 AI 断言再上。

## 5. 已知口径冲突（必须先裁决，否则 `SKILL-CLOSE-01` 无法同时满足两条验收）

§2-A 要求「Q 进入 `Q_1`、W 进入 `W_1`」，但现有断言写死了旧状态：

| 套件 | 断言 | 现口径 | A 项生效后实际 |
|---|---|---|---|
| SPEC01 | `Skill.CastSmoke` | Q `≤0.5s` 进入 `Bash`；W 进入 `Skill` | 进入 `Q_1` / `W_1` |
| REFACTOR | `Skill.QW` | Q 进入 `Bash`、W 进入 `Skill` | 进入 `Q_1` / `W_1` |
| REFACTOR | `Skill.InterruptAttack` | `≤0.6s` 进入 `Bash` | 进入 `Q_1` |
| REFACTOR | `Skill.InterruptInteract` | 技能中 = 处于 `Bash` | 处于 `Q_1` |

`REPORT_REFACTOR-LONG-01.md` 的验收要求「REFACTOR 16/16、SPEC01 12/12」，与「不改旧断言」直接冲突。

可选项：

- **A（推荐）**：所有者授权本次单内把这 4 条断言的**期望状态名**改为 `Q_1`/`W_1`（断言体其余口径、阈值、数据字段不动），并在 TASK/REPORT 明确记录「口径变更已获批」。
- **B**：本单只做 B/C 两项（运动锁 + DIAG + 提交），A 项等到 M4.5 所有者拖完 `OC_*` 的 Q/W Clip 后再单独一单。
- **C**：A 项照做，接受这 4 条断言变红并列入已知红（违反第一阶段「已知红不增加」的基线约定）。

> 未裁决前不得派单执行 A 项。

## 6. 新会话接手步骤（§12.5）

1. 读上游计划书 §12 + 本文件。
2. （已完成）§12 整理为 `Docs/Handoff/PLAN_REFACTOR-PHASE2.md`，并同步 `CURRENT.md` 的「当前 feature」。
3. 写 `Docs/Handoff/TASK_SKILL-CLOSE-01.md`（三节 + AC 表，自包含）。
4. 派一个 executor 执行；主会话只验收 REPORT、做本地 commit。
5. 绿后产出 §3 的两份材料，交给所有者做 M4.5。

## 7. 预授权（与第一阶段 §7 相同）

| 事项 | 授权 |
|---|---|
| 编辑 `Scripts/` 下本阶段列出的文件 | ✅ 预批 |
| 新增脚本与目录（含 `.meta`） | ✅ 预批 |
| 运行编辑器内测试菜单（`Tools/QA/In-Editor/*`） | ✅ 预批 |
| 本地 commit（不 push） | ✅ 预批（由大脑执行） |
| 重启 Unity（关唯一实例→重开→等 MCP） | ✅ 预批，禁止开第二个实例 |
| 修改 `CURRENT.md` / 标 Done | ❌ 禁止（执行者） |
| 改预制体/场景/`.asset`/`ProjectSettings`/`Packages`/Animator Controller | ❌ 禁止，遇到即停 |
| 改现有断言口径（未获 §5 裁决时） | ❌ 禁止 |
| 绕过失败（关断言、降级通过、删测试） | ❌ 禁止 |
