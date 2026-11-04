# TASK_SPEC-01.1 — 飘字出生点修复（S01-2 返工）

> 状态：`Ready`（所有者口述返工单，2026-11-05）
> 风险：**中风险**（单系统脚本 + 测试断言；REPORT 须附完整 diff，由用户送外部顾问复核后终审）
> 类型：【修复】坐标系错配导致飘字系统性偏移
> 唯一报告：`Docs/Handoff/REPORT_SPEC-01.1.md`；唯一证据：`Docs/QA/Evidence/SPEC-01/`（沿用原目录，**文件名加 `_fix` 后缀**）
> 上位单：`Docs/Handoff/TASK_SPEC-01_动画机单段攻击与飘字系统.md`（整体仍未收口；本单仅闭合 `S01-2` 项）

---

## 1. 背景与根因（已由所有者定位，执行者只复核不复盘）

`S01-2` 目检未过：**飘字不源于角色、有时不可见**。

根因：`DamageNumber.Play()` 中实例 `RectTransform` 的 `anchorMin/anchorMax = (0,0)`（左下锚点），而 `RectTransformUtility.ScreenPointToLocalPointInRectangle` 返回的是**以画布中心为原点**的 local 坐标；两者坐标系不匹配 → 全体飘字系统性**左下偏移约半屏**，落在左/下象限时直接出屏。

## 2. 范围（仅 `.cs`，零资产）

### 2.1 生产修复（唯一文件：`Assets/_Project/Scripts/Combat/DamageNumber.cs`）

- 在 `Play()` 给实例赋值 `anchoredPosition` **之前**，将实例 `RectTransform` 的 `anchorMin = anchorMax = (0.5f, 0.5f)`（`pivot` 已是 `0.5`）。
- `anchoredPosition` 赋值逻辑**不变**；`Update()` 的运动叠加与 `_start/_dir` 同源坐标系，**不另改**。
- `ChestHeight` 维持 `1.2f` 常量**不动**（所有者将在 green 后开 Unity 目检试值，如需调整另行一句话指令）。
- 不得新增字段、不得改动曲线/池/配色逻辑。

### 2.2 测试（`Assets/_Project/Tests/SPEC01/`，新增断言）

新增断言 **`UI.SpawnAnchor`**：

- 造一个飘字（对测试目标 `UnitHealth` 造成一次伤害或直接 `DamageNumber.Show(target.position, 25, DamageElement.Physical)`）；
- 取该飘字实例的 `RectTransform`，屏幕位置 = `RectTransformUtility.WorldToScreenPoint(null, rt.position)`；
- 期望位置 = `Camera.main.WorldToScreenPoint(target.position + Vector3.up * DamageNumber.ChestHeight)`；
- 断言二者**距离 ≤ 50px**。
- 采样时机：`Show` 后立即取（同帧或下一帧首帧，此时 `_initialOffset` 尚未生效或位移远小于容差），并在断言数据里记录实际距离。

**修复前该断言必须为红**（半屏偏移 ≫ 50px）——这就是本单的 red 证据。

## 3. 硬性顺序（不得跳步）

1. **只读复核**：确认实例锚点当前确为 `(0,0)`、`Play()` 赋值点、`ChestHeight` 值；确认 SPEC01 套件入口与断言注册位置。若与上文不符 → 停止报告，不得自行改口径。
2. **新断言进套件**（只加断言，不动生产代码）→ 跑一轮：
   - 期望：`UI.SpawnAnchor` = `product_fail`，**其余全部 pass**，`infra=0`（无编译错误）；
   - 输出必须**逐条列出 product_fail 断言名**（含 expected/actual 与实际像素距离），不得只给计数。
3. **锚点修复**（仅 §2.1 那一处）。
4. **green ×2**：run1、run2 全部 pass 且关键数据一致（`UI.SpawnAnchor` 距离两轮均 ≤50px）。
5. 写 `Docs/Handoff/REPORT_SPEC-01.1.md`。

## 4. 证据与命名

目录 `Docs/QA/Evidence/SPEC-01/`，沿用原目录，**新文件一律加 `_fix` 后缀**：

- red：`SPEC01_Red_fix_Assertions.jsonl`、`SPEC01_Red_fix_Automation.log`、`SPEC01_Red_fix_Summary.json`、`SPEC01_Red_fix_ExitCode.txt`
- green：`SPEC01_Green_fix_runN_*.log/.json/.txt`（N=1,2）+ 断言 jsonl
- 汇总：`SPEC01_Runs_fix.jsonl`

不得覆盖 SPEC-01 原始证据文件。

## 5. 验收要点

- `UI.SpawnAnchor`：red 距离 ≫50px → green 两轮 ≤50px；
- 其余 7 条 SPEC-01 断言在 red 与 green 中均保持 pass（无回归）；
- 生产 diff 仅 `DamageNumber.cs` 一处锚点赋值（+ 必要的空值防护），无其他改动。

## 6. REPORT 要求

`Docs/Handoff/REPORT_SPEC-01.1.md` 须含：结果摘要 / HEAD / 完整命令 / red 逐条 product_fail（含实际像素距离）/ green run1~2 逐断言表 / 关键数据一致性 / 完整生产 diff（测试 diff 可外链 `_fix` 文件）/ 变更文件 / 未覆盖项 / 实现原理 / 模块导航 / 遗留问题 / 建议提交。**不设字数上限，讲清即止，禁止为压字数反复重写。**

### 人工目检（执行者只留“待目检”，严禁代填）

- `S01.1-1`：飘字从角色胸口位置冒出、全屏任意位置均可见（含左下/右下象限）。

## 7. 边界

- 禁动 `CombatFeedback.cs`、`RangedCombat.cs` 及任何资产（`.controller`/`.anim`/`.unity`/`.prefab`/`.asset`/`.overrideController` 全部只读）。
- 原神视觉风格草案**冻结**：本单只修位置，不碰样式（字号/颜色/曲线/扇区参数一律不动）。
- 不得改 `CURRENT.md`、不得标 `Done`；`B2`/`B6` 登记不回填。
- 任何 git 写操作禁止。
