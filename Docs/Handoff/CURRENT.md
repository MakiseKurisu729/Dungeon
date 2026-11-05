# CURRENT — 当前状态指针

> 最后更新：2026-11-05
> 新会话：读本文件 → 打开“当前 feature”的 TASK → 按“阶段”继续。**不要翻旧聊天。**
> 规则见 `AGENTS.md`；目标/范围见 `Docs/Specs/constitution.md`。

## 基线

| 项 | 值 |
|---|---|
| 分支 | `docs/fnd-00-baseline` |
| HEAD | `6872348b`（WF-04 接入 Unity MCP） |
| Unity | `2022.3.62f3c1`（目标 6.3，未迁移） |

## 未提交批次

- **测试基础设施（代码）**：`Assets/_Project/Tests/QA/Editor/QA_InEditorMenu.cs`（新增，含 `.meta`）、6 个既有测试 `.cs`（launcher 钩子 + 证据子目录）、`Docs/QA/Evidence/TEST-MCP-01/`、三套 `editor_runN/`、`REPORT_TEST-MCP-01.md`。
- **Phase A（资产）**：`Assets/_Project/Art/Animations/PlayerBase.controller`（+461 −1）、`Docs/QA/Evidence/SKILL-ANIM-02/phaseA/`。
- **本轮文档**：`AGENTS.md`、`Docs/Handoff/workflow-rules.md`、`CURRENT.md`、`TASK_WF-05.md`、`REPORT_WF-05.md`、`Docs/QA/Evidence/WF-05/`、`TASK_TEST-MCP-01.md`、`TASK_SKILL-ANIM-02.md`。
- 两个 `Assembly-CSharp*.csproj` 是 Unity 自动生成，**永不提交**。
- 提交建议分三组：①测试基础设施代码 ②Phase A 控制器 ③文档（规则+任务+报告+证据）。

## 当前 feature

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
- **`SKILL-ANIM-02` — Q/W/E/R 子状态机与攻击打断 — `InProgress`（高风险；Phase A 已完成）**
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

1. 所有者对 `WF-05` / `TEST-MCP-01` 目检（Unity 里点一次 `Tools/QA/In-Editor/SPEC01 (green)`，确认跑完停在 Edit 模式）。
2. 所有者回复 **“批准 Phase B”** → 执行者改 `SkillSystem`/`SkillEffectBase`/`SkillEffects`/`SkillEffectFireball`/`MeleeCombat`/`UnitStats` + 测试，用 `Tools/QA/In-Editor/*` 跑 SPEC01 ×3 + FND00B06。
3. 所有者拖 `OC_Knight/Archer/Mage` 的 Q/W Clip（`Q_1`/`W_1`），拖完再补一次目检。
4. 待定：`REPORT_TEST-MCP-01` §7 第 5/6 条（`runInBackground=0` + 编辑器失焦会冻结 Play；Play 中域重载丢 runner）建议另立小单。
