# CURRENT — 当前状态指针

> 最后更新：2026-11-06
> 新会话：读本文件 → 打开“当前 feature”的 TASK → 按“阶段”继续。**不要翻旧聊天。**
> 阶段二计划：`Docs/Handoff/PLAN_REFACTOR-PHASE2.md`。
> 规则见 `AGENTS.md`；目标/范围见 `Docs/Specs/constitution.md`。

## 基线

| 项 | 值 |
|---|---|
| 分支 | `docs/fnd-00-baseline` |
| HEAD | `896e7450`（REFACTOR-LONG-01 M4；M0–M4 已本地提交，未 push） |
| Unity | `2022.3.62f3c1`（目标 6.3，未迁移） |

## 未提交批次

- **第二轮（代码/资产/文档，待一次性提交）**：
  - 代码：`CharacterBrain.cs`、`DownedRescue.cs`、`CursorManager.cs`、`SelectionManager.cs`、`MeleeCombat.cs`、`RangedCombat.cs`、`DebugCameraSwitcher.cs`、`QA_InEditorMenu.cs`（`runInBackground` 用完还原）。
  - 资产：`PlayerBase.controller`（布局 + 2 条 `Interrupt` 过渡）、`Knight_Unit.prefab`（+`Interactable`，所有者批准）、`DungeonWorld.unity`（`interactCursor` 指定，所有者批准）。
  - 文档/证据：`AGENTS.md` §3.3 例外通道、`workflow-rules.md` §5.3 审批流程、`TASK_{DOWNED,INTERRUPT,RESCUE}-01.md`、5 份 REPORT、`Docs/Learning/RESCUE-01_讲稿.md`、`Docs/QA/Evidence/{CAM-01,ANIM-LAYOUT-01,DOWNED-01,INTERRUPT-01,RESCUE-01,SPEC-01/editor_run9..15}`。
- 两个 `Assembly-CSharp*.csproj` 是 Unity 自动生成，**永不提交**。
- `ProjectSettings/ProjectSettings.asset` 已还原为 `runInBackground: 0`，不在本批次内。
- 已提交：`9e0b7515`（TEST-MCP-01 + Phase A + WF-05 规则）。

## 当前 feature

- **`REFACTOR-LONG-01`（第二阶段）— `InProgress`（M0–M4 已收口，当前 `SKILL-CLOSE-01`）**
  - 计划/缺口/顺序：`Docs/Handoff/PLAN_REFACTOR-PHASE2.md`（来源：上游计划书 §12）。
  - 已提交：M0 `ed0cdc17`、M1 `0a1a45d7`、M2 `1961c93b`、M3 `d02470c4`、M4 `896e7450`（未 push）；护栏 = `Tools/QA/In-Editor/REFACTOR`（15 条）+ `SPEC01 (green)`（12 条）。
  - 当前任务：`SKILL-CLOSE-01`（A 槽位映射 `Q/Q_1`、`W/W_1`…；B 新增 `Scripts/Units/UnitMotion.cs` 运动锁；C 补跑 DIAG ×1 并提交 `REFACTOR-M4/editor_run4`）。TASK：`Docs/Handoff/TASK_SKILL-CLOSE-01.md`。
  - **待裁决**：`PLAN_REFACTOR-PHASE2.md` §5 —— A 项会让 4 条既有断言（`SPEC01 Skill.CastSmoke`、`REFACTOR Skill.QW / Skill.InterruptAttack / Skill.InterruptInteract`）的期望状态从 `Bash`/`Skill` 变成 `Q_1`/`W_1`，与「不改旧断言」冲突。
  - 之后：所有者做 M4.5 预制体基座（`PlayerUnit_Base` → 三职业逐个迁），再 M6（池化/事件/面板）→ M5（AI 拆分）。
- **`WF-04` — 接入 Unity MCP — `Done`（连通验证通过）**
  - 包：`com.coplaydev.unity-mcp@10.0.0`（OpenUPM）。
  - 服务：`HTTPLocal`、`http://127.0.0.1:8080`，`Session Active (Dungeon)`；`initialize` 正常。
  - 客户端：全局 `~/.config/opencode/opencode.json` 使用 V2 写法 `mcp.servers.unityMCP`；47 个工具、19 个资源可用，实例 `Dungeon@4f5c34c82f31646e`。
  - 已提交：`6872348b`（`Packages/*`、`ProjectSettings/PackageManagerSettings.asset`、WF-04 文档/证据）。
  - 安全：HTTP 8080 无鉴权，仅本机；Unity 关闭即停服务。
- **`WF-05` — MCP 常开编辑器规则入册 — `Review`（AC 全过，待所有者目检）**
  - Spec：`Docs/Handoff/TASK_WF-05.md`；报告：`Docs/Handoff/REPORT_WF-05.md`；证据：`Docs/QA/Evidence/WF-05/`。
  - 规则：§3.2 双模式；§3.2.1 九条（含第 9 条“卡在焦点时自行 `refresh_unity` 唤醒，不必询问”）；§6.1 补 MCP 熔断；`workflow-rules.md` §5.2/§7 同步。
- **`TEST-MCP-01` — 三套探针的编辑器内运行菜单 — `Review`（AC-1~9 全过，待所有者目检）**
  - Spec：`Docs/Handoff/TASK_TEST-MCP-01.md`；报告：`Docs/Handoff/REPORT_TEST-MCP-01.md`；证据：`Docs/QA/Evidence/TEST-MCP-01/`。
  - 新增 `Tools/QA/In-Editor/{SPEC01 (green), FND00B06, DIAG}`，跑完不退出编辑器；证据写各自 `editor_runN/`，canonical 未变；三个 launcher 的 `catch` 也已加钩子（异常不再杀 Unity）。
  - 编辑器内入口已设 `Application.runInBackground = true`（不改 `ProjectSettings`）：失焦也能跑完，实测全程 Unity 非前台仍 12/12。
  - 编辑器内 vs batch：SPEC01 12/0/exit0、FND00B06 12/0/observed2/exit0、DIAG 6/1/exit2（与 batch 基线逐项一致）。
- **`CAM-01` — 选中后可拖视角 — `Review`（`SelectionManager.cs:94` 改为常开平移，8px 阈值区分点击/拖动；SPEC01 12/0）**
- **`ANIM-LAYOUT-01` — 动画机节点布局整理 — `Review`（18 个节点坐标重排；diff 仅 `m_Position`/`m_AnyStatePosition`）**
- **`DOWNED-01` — 倒地不播动作 — `Review`（根因：致死那一击的 `Hit` 触发器在 64ms 后抢走 `Downed`；修法 `OnDowned` 里先 `ResetTrigger("Hit")`；3 次复现 + Archer/Mage 通过）**
- **`INTERRUPT-01` — 移动打断普攻/交互、技能免疫 — `Review`（新增 `AttackMelee/Interact → Locomotion` 的 `Interrupt` 过渡；`CancelAttack` 与 `RangedCombat` 的无条件触发器删除；AC-1 入场淡入窗口最坏 0.30s 待裁决）**
- **`RESCUE-01` — 救援指令化 — `Review`（hover 救援图标 → 点击下令 → 走进范围 → 循环读条 → 受击/移动打断且需重新下令；AC-1~8 全过；资产改动经所有者批准）**
  - 讲稿：`Docs/Learning/RESCUE-01_讲稿.md`（AI 完整稿，所有者按需批注，不需改写）。
  - 未决：`interactCursor` 观感、倒地者仍可被选中（R-8 要求保持）、同帧打断动画尾 0.208s。
- **`INTERRUPT-02` — 过渡布局 / 远程前后摇打断 / 技能指令锁 — `Review`**
  - A 布局：控制器三列重排（80/520/1000），`AnyState` 移到 (300,600)；七条目检状态进出线 0 穿块，仍有 3 条估算穿块（`AnyState→Q` 穿 `Bash`、`AnyState→E` 穿 `Skill`、`Rise↔Locomotion` 同列互穿）待目检。
  - B 远程：新增 `Attack_Draw/Attack_Release → Locomotion`（`Interrupt`），弓箭手/法师各 3 次实测 0.104–0.113s 回 `Locomotion`、弹丸 0、伤害 0。
  - C 技能锁：`CharacterBrain.InSkillState()` 覆盖 `Bash/Skill/Q_1..R_1`，锁 `CommandMove/CommandAttack/CommandInteract`；Q/W 各 3 次 `destDelta=0`、伤害照常、结束后恢复。
  - **计划外改动（待所有者裁决）**：`Locomotion→Bash` 的 `hasExitTime` 由 1 改 0（否则 Q 起手要等 73% 循环，0.3–0.8s 内移动仍生效）。全文件唯一一处 `m_HasExitTime` 改动。
  - 证据：`Docs/QA/Evidence/INTERRUPT-02/`；SPEC01 `editor_run17` 12/0。
- **`SKILL-ANIM-02` — Q/W/E/R 子状态机与攻击打断 — `InProgress`（Phase A 完成，Phase B 未开）**
  - Spec：`Docs/Handoff/TASK_SKILL-ANIM-02.md`（四条决策已并入）。
  - Phase A 已完成：`PlayerBase.controller` 新增 `Q/W/E/R` Trigger + `SkillSpeed`(Float,默认1)、四个子状态机（`Q_1/W_1/E_1/R_1`，占位 Clip=`Bash`/`Skill`）、`AnyState→Hit_A/Q/W/E/R`（`If`、`HasExitTime=0`、`Source`、`Ordered`、`CanSelf=0`、0.05s）；`Bash`/`Skill` 保留，`AttackRange` 未动。
  - 定点验证：未按 Q 时三角色常驻 `Locomotion`；`Knight.SetTrigger("Q")` → 0.058s 进 `Q_1`，1.172s 回 `Locomotion`；SPEC01 编辑器内仍 12/0。
  - 证据：`Docs/QA/Evidence/SKILL-ANIM-02/phaseA/`。
  - 下一步：**Phase B 待所有者批**（代码 8 文件 + 测试）；之后所有者拖 `OC_*` 的 Q/W Clip。

## 阶段与已知事实

- 技能修复批次 `1bec9c93`：Q 输入组件、W 拆分、扣蓝顺序、Hit 夹具；SPEC01 12/12×3（SKILL-ANIM-01 后）。
- `SKILL-TOOLS-01`：F5 补怪 / F9 重载 / F10 重置；SPEC01 动态遭遇基线（敌人实际位置，不再写死 -12）。
- `SKILL-ANIM-01`：瞄准态攻击指针 + 点击提示；攻击中放技能不切动画的问题已确认，归 `SKILL-ANIM-02`。
- 工作流已对齐 SDD：TASK 三节 + AC 表 → 审 spec → 实现 → AC 验收 → 讲稿 → 提交 → Replan。
- `TEST-MCP-01`：`Tools/QA/In-Editor/*` 三菜单已通；编辑器内与 batch 计数一致（SPEC01 12/0、FND00B06 12/0+2obs、DIAG 6/1）；canonical 证据未被覆盖。

## 已收口

- `FND-00B`、`FND-01`、`FND-02A`、`SPEC-01`、`SPEC-01.1`、`SPEC-DIAG-SKILL`、`SKILL-TOOLS-01`、`SKILL-ANIM-01`。
- `FND-02B` 仍 `In Review`（历史批次，优先级低于当前技能线）。

## 当前边界

- 场景、预制体、Animator、`.asset` 归所有者；AI 只出清单/编辑器工具。
- 除本次批准的 MCP 工具链外，不装新包、不迁移 Unity 6.3、不改任务外文件。
- 资产改动与配置/文档批次分开提交。

## 下一会话第一件事

1. 所有者在 Unity 里目检本轮：`Tools/QA/In-Editor/SPEC01 (green)` 跑一次、拖视角、倒地/救援走一遍、看 `interactCursor` 图标观感。
2. 所有者回复 **“批准 Phase B”** → 执行者改 `SkillSystem`/`SkillEffectBase`/`SkillEffects`/`SkillEffectFireball`/`MeleeCombat`/`UnitStats` + 测试，用 `Tools/QA/In-Editor/*` 跑 SPEC01 ×3 + FND00B06。
3. 所有者拖 `OC_Knight/Archer/Mage` 的 Q/W Clip（`Q_1`/`W_1`），拖完再补一次目检。
4. 待裁决：
   - `INTERRUPT-01` AC-1 入场淡入窗口最坏 0.30s（是否放宽 AC 到 0.35s，或改既有 `Locomotion→AttackMelee` 的 `InterruptionSource`）。
   - `MonsterBrain.cs:112` 有与 `RangedCombat` 同类的无条件 `Interrupt`（怪物控制器用），本轮未在授权内。
   - `REPORT_TEST-MCP-01` §7 第 5/6 条（Play 中域重载丢 runner）建议另立小单。

## 待规划（未排期，所有者要求先讨论不动手）

- **选中拾取（PICK 系列）**：目标“指针落在模型上就选中”，不再只认脚底圈。已定方向：用**当前姿态的包围盒**做 OBB 射线检测 + 障碍遮挡射线 + 最近优先；将来若要“视觉=命中”，升级为跟随姿态的 `HitProxy` 胶囊，让选中/近战/子弹共用一份体积。
- **命中体积一致性（HITBOX 系列，高风险）**：现状是子弹**无碰撞检测**、直接朝“根节点+0.6m”飞行到点结算；近战与选中打的是**静止胶囊** `CharacterController`。若要“看着打空就不该掉血”，需要 `HitProxy` + 改近战掩码 + 改子弹瞄准点，并写 ADR。
- **选中交互规则（已定，待实现）**：左键点已选中角色 = 取消选中；左键点地板 = **保留角色选中**、仅清空敌方目标；未来 UI 角色图标：左键选、右键取消。
- 所有者计划先处理完当前任务，再单独讨论代码库重构。

### 重构约束（2026-11-05 已定，待 Plan 展开）

1. **顺序**：先重构（2022.3 上完成并冻结），后迁 Unity 6.3。
2. **不做一次性全量重写**：先划逻辑边界、再按依赖加程序集（`Docs/01` §1.2/§3.3）。
3. **验收口径**：`Docs/01` §1.3 五条扩展演练 + 目录/命名空间与职责对齐；每道闸门都要能回答“新增第四个职业要碰几个文件”。
4. **护栏扩大**：新建覆盖交互/救援/选中/技能打断的回归套件，`SPEC01` 保持不动。
5. **`Date → Data`**：所有者自行改名（Unity 内改，GUID 不变；代码内 0 处硬编码路径）。
6. **预制体基座**：G1 契约冻结 → G2 结构重构 → G3 `PlayerUnit_Base` → G4 三职业变体迁移；每次只迁一个职业。
7. **序列化字段名冻结**（选 A）：重构期只改内部实现，字段名不改；改名单独一次做并加 `[FormerlySerializedAs]`。
8. **网络**：重构要为 P2P 预留“指令/状态可序列化”的形状，但不实现。
9. **冻结新功能**：里程碑内锁死，里程碑之间允许插入。
