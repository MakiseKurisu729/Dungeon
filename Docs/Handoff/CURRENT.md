# CURRENT — 当前状态指针

> 最后更新：2026-11-05
> 新会话：读本文件 → 打开“当前 feature”的 TASK → 按“阶段”继续。**不要翻旧聊天。**
> 规则见 `AGENTS.md`；目标/范围见 `Docs/Specs/constitution.md`。

## 基线

| 项 | 值 |
|---|---|
| 分支 | `docs/fnd-00-baseline` |
| HEAD | `1bec9c93`（技能修复批次） |
| Unity | `2022.3.62f3c1`（目标 6.3，未迁移） |

## 未提交批次

- **文档批次（WF-02 + WF-03）**：`AGENTS.md`、`workflow-rules.md`、`Docs/Specs/`、`Docs/Learning/`、`TASK/REPORT_TEMPLATE`、两个新 skill、`Docs/README`、`03/05` 归档、`ADR-0003`。待所有者 GitHub Desktop 提交。
- **所有者测试改动**：`DungeonWorld.unity`（骷髅移到 z=15.8、旋转 180°）、`PlayerBase.controller`（Bash/Skill 速度参数）、两个 `.csproj`。待所有者决定提交或保留。
- 提交时把文档批次与 `Assets/` 改动分开。

## 当前 feature

- **无进行中 feature。**
- **下一步**：`SKILL-TOOLS-01` — 调试键位（F5 生成骷髅 / F9 重载 Play / F10 重置玩家）+ SPEC01 动态遭遇基线。状态 `Ready`。
- **再下一步**：`SKILL-ANIM-01` — 瞄准态攻击指针 + 点击反馈（SPEC01 12/12）。
- **再下一步**：`SKILL-ANIM-02` — Q/W/E/R 子状态机 + 远程 `AttackRange` 子状态机（高风险，资产由所有者改，ADR-0003 已记录）。

## 阶段与已知事实

- 技能修复批次已提交：Q 输入组件、W 拆分、扣蓝顺序、Hit 夹具；SPEC01 10/10×3、FND00B06 12 pass/0 fail。
- 场景骷髅被所有者移到 `z=15.8` 用于手动测试；`SPEC01 Regression.FND02A` 仍写死 `(-2.72,0,-12)`，因此 `SKILL-TOOLS-01` 要改成动态位置 + 相对接近。
- 工作流已对齐 SDD：TASK 三节（需求/方案/验证 AC 表）→ 审 spec → 实现 → AC 验收 → 讲稿 → 提交 → Replan。

## 已收口

- `FND-00B`、`FND-01`、`FND-02A`、`SPEC-01`、`SPEC-01.1`、`SPEC-DIAG-SKILL`（技能输入 / W 拆分 / 扣蓝 / Hit 夹具）。
- `FND-02B` 仍 `In Review`（历史批次，优先级低于当前技能线；待 V02B 目检 + 顾问 + 终审）。

## 当前边界

- 场景、预制体、Animator、`.asset` 归所有者；AI 只出清单。
- 不装包、不迁移 Unity 6.3、不修改任务外文件。
- 资产改动与文档批次分开提交。

## 下一会话第一件事

1. 提交文档批次（标题/正文由大脑给）。
2. 派 `SKILL-TOOLS-01`；完成后跑 `SKILL-ANIM-01`。
