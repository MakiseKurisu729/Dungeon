# TASK — SKILL-ANIM-01：瞄准指针与点击反馈（阶段 1，低风险）

> **风险等级：低风险**（2 个生产脚本 + 测试追加；不动动画机、不动资产）
> **前置**：所有者先提交当前技能修复批次（提交清单见 `REPORT_SPEC-DIAG-SKILL-2.md` §12）。**未提交前不得开工**，以免把本单改动混进待提交批次。
> **回滚锚**：以所有者的技能修复提交为锚（开工时记录 `git rev-parse HEAD`）。

## 1. 背景（已证实）

- 瞄准态没有攻击指针：`CursorManager.Update` 在 `IsTargeting` 时直接强制 pointer 并 `return`（`CursorManager.cs:52-53`），后面的 hover 射线根本不执行。
- 点击敌人"放不出"的三条放大因素：技能射程 2.5m（出生点到骷髅 12m）、Bash 过渡 `HasExitTime=1/ExitTime=0.732`、攻击态无到技能的过渡。后两条属阶段 2（动画机 QWER 化），本单只解决**瞄准指针**与**点击反馈**，并为"点击能否命中"补一条可重复断言。

## 2. 授权改动

| 文件 | 改动 |
|---|---|
| `Assets/_Project/Scripts/UI/CursorManager.cs` | 瞄准态不再无条件 pointer：继续做 hover 判定；悬停存活敌人 → 攻击指针；其余 → 普通指针。把判定抽成纯方法便于断言（见 §3） |
| `Assets/_Project/Scripts/Player/SkillInputController.cs` | ① 暴露 `PendingRange`（当前待施放技能射程）；② 抽出 `PickEnemyAt(Vector3 screenPos)`（`PickEnemy` 调它，逻辑不变）；③ 点击反馈：未点中敌人 → "未选中敌人"；敌人超出射程 → "目标超出射程，正在接近"；`Cast` 返回 false 且无消息时 → "技能释放失败" |
| `Assets/_Project/Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs` | 追加 `Skill.TargetPick` 与 `Skill.CursorHover` 两条断言（见 §4） |

**禁止**：改动画机/控制器/覆盖器/任何 `.asset`/场景/预制体；改 `SkillSystem`、效果类、`MeleeCombat`；改既有断言口径；git 写操作。

## 3. 实现要求

### 3.1 CursorManager

- 保留现有四态语义与贴图染色逻辑。
- `Update()`：库存打开 → pointer（不变）；**瞄准中不再提前 return**，继续走同一套 hover 判定；悬停存活敌人 → `Attack`，悬停友军/可交互/空地 → 现有规则。
- 抽纯函数：`public Hover ResolveHover(Ray ray)`（内部复用同一套 `Physics.RaycastAll`），`Update()` 用 `Camera.main.ScreenPointToRay(Input.mousePosition)` 调它。这样 batchmode 可用合成射线断言。
- 不新增光标贴图；不要求"射程外换色"（登记为后续可选）。

### 3.2 SkillInputController

- `public float PendingRange { get; private set; }`：进入瞄准时写入 `GetSkillRange(s)`，`Cancel()` 归零；供光标/UI 使用（本单光标不强依赖它，但先暴露）。
- `Transform PickEnemyAt(Vector3 screenPos)`：`ray = Camera.main.ScreenPointToRay(screenPos)`，其余逻辑与现 `PickEnemy` 完全一致（排序、`team==Monster`、`!dead`）。
- 瞄准点击分支：
  - `PickEnemyAt` 返回 null → `GameData.SetMessage("未选中敌人")`，再 `Cancel()`（保持"点空地取消"语义）；
  - 命中但超距 → `GameData.SetMessage("目标超出射程，正在接近")`，进入 `_chase`（现有逻辑）；
  - 命中且在射程内 → `Cast`；若 `Cast` 返回 false 且没有可用消息（空槽等）→ `GameData.SetMessage("技能释放失败")`。
- 不改 chase/取消/追放行为，不改鼠标键位。

## 4. 新增断言（并入 SPEC01，保持在 `UI.SpawnAnchor` 之后）

1. `Skill.TargetPick`（production）：
   - 用场景内真实敌人，取其胸口世界点 `enemy.position + up*1.2` → `Camera.main.WorldToScreenPoint` → 通过反射调 `PickEnemyAt(screenPoint)`；
   - 期望返回该敌人的 `UnitHealth`；同时记录 `screenPoint`、`hitName`、`cameraName`、`distanceToPlayer`、`pendingRange`、`inRange`。
   - 若敌人不在相机视锥内（`z<0`）→ `BlockedAssertion`，不伪造通过。
2. `Skill.CursorHover`（production）：
   - 用同一条合成射线调 `CursorManager.ResolveHover(ray)`；
   - 期望 `Hover.Attack`；另断言"瞄准中不再被强制 pointer"这一行为（即 `ResolveHover` 在瞄准态可返回 Attack）。
3. 两条都必须 `kind=production`、默认期望 pass；**不得为了让套件绿而放宽**。

> 断言总数将从 10 → 12；验收口径相应为 **12/12、exit 0、N=3**。

## 5. 验收

1. `SPEC01` 连续 3 次：**12/12 pass、fail=0、blocked=0、infra=0、exit=0**，既有 10 条不回退。
2. 人工 2 项：
   - 选中骑士按 Q/W，指针移到敌人上 → **攻击指针**；移到空地 → 普通指针；
   - 点空地 → 提示"未选中敌人"并退出瞄准；点 2.5m 外敌人 → 提示"目标超出射程，正在接近"并开始追放。
3. 不改动画机行为；已知的"普攻中切不到技能态 / Bash 延迟"留待阶段 2。

## 6. 证据与报告

- 证据：`Docs/QA/Evidence/SKILL-ANIM-01/`（3 次 SPEC01 日志、ExitCode/Summary、断言快照）。
- 报告：`Docs/Handoff/REPORT_SKILL-ANIM-01.md`，含结果摘要、diff（两文件）、断言数据、实现原理、模块导航、建议提交。

## 7. 熔断

单步重试 ≥3 次、编译错连续 ≥2 轮、工作区存在未提交的技能修复批次、需要改动画机/资产、git 异常 —— 任一触发即停并报告。
