# CURRENT — 当前状态指针

> 最后更新：2026-11-05
> 新会话：读本文件 → 打开“当前 feature”的 TASK → 按“阶段”继续。**不要翻旧聊天。**
> 规则见 `AGENTS.md`；目标/范围见 `Docs/Specs/constitution.md`。

## 基线

| 项 | 值 |
|---|---|
| 分支 | `docs/fnd-00-baseline` |
| HEAD | `a7817b55`（SKILL-ANIM-01 + 流程文档） |
| Unity | `2022.3.62f3c1`（目标 6.3，未迁移） |

## 未提交批次

- **MCP 接入（WF-04）**：`Packages/manifest.json`（新增 `com.coplaydev.unity-mcp@10.0.0`）、`opencode.json`（`mcp.unity` → `localhost:8080/mcp`）、`Docs/QA/Evidence/WF-04/`、`REPORT_WF-04.md`。
- **待审文档**：`Docs/Handoff/REVIEW_ANIMATOR.md`、`TASK_WF-04.md`、`TASK_SKILL-ANIM-02.md`、本文件。
- 两个 `.csproj`（Unity 自动生成，未提交、未还原）。
- 提交时把“MCP 配置/依赖”与“代码/资产”分开。

## 当前 feature

- **`WF-04` — 接入 Unity MCP — 服务已启动，待重启 OpenCode 验证**
  - 包：`com.coplaydev.unity-mcp@10.0.0`（OpenUPM，已装）。
  - 服务：`HTTPLocal`、`http://127.0.0.1:8080`，`Start Server` 已成功（`Session Active (Dungeon)`，8080 可连）。
  - 客户端：全局 `~/.config/opencode/opencode.json` 已写入 `mcp.unityMCP`；项目 `opencode.json` 的重复 `mcp.unity` 已删。
  - 待验证：重启 OpenCode 后调用 `read_console` / `execute_menu_item`；AC-4 未闭环。
  - 诊断脚本 `Assets/_Project/Editor/DIAG_McpPython.cs` 已删除；Python 用 3.12.10 + `uv` 受管 3.12.16；uv 默认源已设清华镜像。
  - 注意：HTTP 8080 无鉴权，仅本机；服务进程随 Unity 生命周期。
- **`SKILL-ANIM-02` — Q/W/E/R 子状态机与攻击打断 — `Ready`（高风险，spec 待更新）**
  - Spec：`Docs/Handoff/TASK_SKILL-ANIM-02.md`。
  - 所有者已定案：受击打断技能；新增 `SkillSpeed`；技能可打断交互；`AttackRange` 合并放结构改造阶段。
  - 下一步：MCP 接通后，用编辑器工具做 Phase A（控制器 Q/W/E/R 子状态机 + AnyState 技能/受击 + SkillSpeed）；覆盖器由所有者拖；然后 Phase B 代码/测试。

## 阶段与已知事实

- 技能修复批次 `1bec9c93`：Q 输入组件、W 拆分、扣蓝顺序、Hit 夹具；SPEC01 12/12×3（SKILL-ANIM-01 后）。
- `SKILL-TOOLS-01`：F5 补怪 / F9 重载 / F10 重置；SPEC01 动态遭遇基线（敌人实际位置，不再写死 -12）。
- `SKILL-ANIM-01`：瞄准态攻击指针 + 点击提示；攻击中放技能不切动画的问题已确认，归 `SKILL-ANIM-02`。
- 工作流已对齐 SDD：TASK 三节 + AC 表 → 审 spec → 实现 → AC 验收 → 讲稿 → 提交 → Replan。

## 已收口

- `FND-00B`、`FND-01`、`FND-02A`、`SPEC-01`、`SPEC-01.1`、`SPEC-DIAG-SKILL`、`SKILL-TOOLS-01`、`SKILL-ANIM-01`。
- `FND-02B` 仍 `In Review`（历史批次，优先级低于当前技能线）。

## 当前边界

- 场景、预制体、Animator、`.asset` 归所有者；AI 只出清单/编辑器工具。
- 除本次批准的 MCP 工具链外，不装新包、不迁移 Unity 6.3、不改任务外文件。
- 资产改动与配置/文档批次分开提交。

## 下一会话第一件事

1. 所有者在 Unity 完成 MCP 的 Configure/Start Server，重启 OpenCode；连通后把结果写进 `Docs/QA/Evidence/WF-04/mcp_smoke.txt`。
2. 我更新 `TASK_SKILL-ANIM-02` 的决策（受击打断 / SkillSpeed / 交互打断），然后用 MCP 跑 Phase A 编辑器工具。
3. 所有者拖 `OC_*` 的 Q/W 动作；我跑 Phase B 代码/测试。
