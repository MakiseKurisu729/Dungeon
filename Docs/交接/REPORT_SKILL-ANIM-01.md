# REPORT — SKILL-ANIM-01：瞄准态攻击指针与点击反馈

> 对应 `Docs/Handoff/TASK_SKILL-ANIM-01.md`。节次与 `Docs/Handoff/workflow-rules.md` §2 一致，未删节。
> 未运行项一律写“未运行”，无预填/推测结论。

## 1. 结果摘要

`完成` —— 瞄准态不再强制普通指针，指针与点击共用同一套射线分类；点击空地/超距/施放失败各有可见提示；SPEC01 由 10 条扩到 12 条，连续 3 次 12/12、exit=0，既有 10 条无回退。人工目检 2 项待所有者执行。

## 2. AC 回填表

| AC | 结果 | 断言 ID | 关键数据 | 日志路径 |
|---|---|---|---|---|
| AC-1（瞄准态悬停敌人 → 攻击指针） | 通过（自动化部分） | `Skill.CursorHover` | `hover=Attack`；`enemy=SPEC01_Target_Cursor`；`enemyIsFixture=True`；`distance=4.424`；`fixtureHits=1`（合成射线确实打在夹具碰撞体上）；三次一致 | `Docs/QA/Evidence/SPEC-01/SPEC01_Green_skillanim01_run{1,2,3}_Assertions.jsonl`；日志 `Docs/QA/Evidence/SKILL-ANIM-01/SPEC01_Green_run{1,2,3}.log` |
| AC-2（同一射线 → 返回该敌人 `UnitHealth`） | 通过（自动化部分） | `Skill.TargetPick` | `picked=SPEC01_Target_Pick`；`team=Monster`；`pickedIsFixture=True`；`distance=5.194`；`pendingRange=0`（batchmode 下未处于瞄准态，如实记录）；三次一致 | 同上 |
| AC-3（SPEC01 连续 3 次 12/12） | 通过 | `all` | run1/2/3 均 `pass=12 fail=0 blocked=0 infra=0 observed=0`，`ExitCode.txt=0`，`assertionLinesInFile=12`；既有 10 条逐条 pass | `Docs/QA/Evidence/SPEC-01/SPEC01_Green_skillanim01_run{1,2,3}_Summary.json`、`..._ExitCode.txt` |
| AC-4（指针 + 点击提示目检） | **未运行（待所有者目检）** | 人工 | — | 目检项见 §7，执行者不代填 |
| AC-5（不改动画机/场景/预制体/`.asset`；无 Console 错误） | 通过（静态 + 日志） | 静态 | `git status` 仅 3 个 `.cs` 被改；`git diff --stat -- Assets` = 3 files / +315 / −40；`Regression.FND02A` 的 `consoleErrors=0` | `Docs/QA/Evidence/SKILL-ANIM-01/git_status_snapshot.txt`、`git_diff_stat_assets.txt` |

- **运行次数**：红阶段 1 次（测试已加、产品未改 → `pass=10 fail=2 exit=2`），绿阶段连续 3 次（时序/相机相关 N≥3）。三次绿跑唯一变化是时间戳，断言数值（`distance` 4.424 / 5.194、`hover`、`picked`）完全一致。
- 红阶段证据：`Docs/QA/Evidence/SKILL-ANIM-01/SPEC01_Red_skillanim01red.log`（`apiPresent=False; hover=<missing>`，且 `fixtureHits=1` 证明夹具与射线本身没问题，失败只因产品 API 不存在）。

## 3. 实现原理（面向所有者）

1. **根因 / 目标**：`CursorManager.Update` 里有一条瞄准态早退——只要 `SkillInputController.IsTargeting` 为真就立刻把指针设成普通指针并 `return`，hover 分类根本没跑。结果玩家按 Q/W 后指针恒定，无法判断“这一下能不能打到谁”。本次移除该早退，并让指针分类可从外部用一条射线单独调用（可测）。
2. **备选方案与为什么没选**：由 `SkillInputController` 直接 `Cursor.SetCursor` 改动最小，但会把 UI 贴图/染色/热点逻辑复制到输入层，绕过 `CursorManager` 的四态与灰度染色，且 batchmode 不可断言；新建 `TargetingCursor` 组件则要维护第二份 hover 逻辑。故选 A：保留单点分类 + 抽纯函数。
3. **实际数据流**：
   `按 Q/W → SkillInputController 记 _pendingIndex、IsTargeting=true、PendingRange=射程`
   → `CursorManager.Update() → Camera.main.ScreenPointToRay(Input.mousePosition) → ResolveHover(ray) → Attack/Select/Interact/Pointer → Apply(贴图+染色)`
   → `左键 → PickEnemyAt(Input.mousePosition) → PickEnemyFromRay(ray) → 命中 Monster：射程内 CastWithFeedback / 超距提示+追放 / 未命中提示+Cancel`
   与 TASK §2 的差异：把 `PendingRange` 的赋值放在瞄准与追放两处（追放中射程仍对外可读），并在 quickCast 路径也复用 `CastWithFeedback`（同一失败提示口径）。
4. **关键代码位置（3–5 条）**：
   - `Assets/_Project/Scripts/UI/CursorManager.cs:52` —— 注释 + 删除瞄准态早退；`:54-56` 相机空值兜底；`:62` 每帧一次 `ResolveHover`。
   - `Assets/_Project/Scripts/UI/CursorManager.cs:92/100/107` —— `ResolveHover` 三层重载：纯分类入口、带 `enemy/distance` 输出、内部 5-out 核心（`RaycastAll` → 分类）。
   - `Assets/_Project/Scripts/Player/SkillInputController.cs:96-119` —— 点击分支：`PickEnemyAt` → 未命中提示“未选中敌人”；超距提示“目标超出射程，正在接近”并进入追放。
   - `Assets/_Project/Scripts/Player/SkillInputController.cs:168-174` —— `CastWithFeedback`：`Cast` 返回 false 时，只有 `GameData.message` 未变才补“技能释放失败”，不覆盖“冷却中/蓝量不足”。
   - `Assets/_Project/Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs:1402-1620` —— 两条新断言：合成射线（相机 → 敌人胸口）+ 反射调用，`Camera.main`/`SkillInputController.instance` 为空走 `InfraFailure`。
5. **边界 / 失败模式**：
   - 相机为空：`CursorManager.Update` 直接返回（不再每帧 NRE）；断言侧 `Camera.main == null` → `InfraFailure`，不会伪造 pass。
   - 敌人死亡：`ResolveHover` 与 `PickEnemyFromRay` 都要求 `!uh.dead`；追放中目标死亡仍走 `Cancel()`。
   - UI 遮挡：原代码只按 `clickableLayers` 做物理射线，背包打开时才是早退；本次未改遮挡语义（UI 面板不参与物理射线，属既有行为）。
   - 瞄准态点空地仍保留“取消”语义，只是多了一句原因提示，不改变控制流。
6. **与计划的差异及原因**：无功能偏离；新增的 `PickEnemyAt(Vector3)` 在 TASK 中只列名未定语义，本次明确为“屏幕像素坐标 → `Camera.main.ScreenPointToRay` → `PickEnemyFromRay`”，与 `Input.mousePosition` 直接对接。
7. **三句话版本**（答辩先讲这三句）：
   - 瞄准态也做 hover，不再提前 return 成普通指针。
   - 指针落在敌人上给攻击指针，落在空地上给普通指针（友军仍按选择指针）。
   - 点击给出原因：空地“未选中敌人”、超距“目标超出射程，正在接近”并追放、`Cast` 静默失败补“技能释放失败”。

## 4. 受影响模块导航

| 模块 | 职责 | 入口（类 / 方法 / 场景对象） |
|---|---|---|
| 光标呈现 | 四态光标 + 灰度染色 + 交互 hint | `CursorManager.Update` / `CursorManager.ResolveHover` |
| 技能输入 | 瞄准、点选、追放、取消 | `SkillInputController.Update` / `PickEnemyAt` / `PickEnemyFromRay` / `PendingRange` |
| 技能执行 | 验蓝/验冷却/效果链 | `SkillSystem.Cast`（未改） |
| 全局提示 | 单行消息 | `GameData.SetMessage`（未改） |
| 验收 | SPEC01 运行期探针 | `SPEC01_ProbeRunner`（新增两条断言） |

数据流：`Input(Q/W/左键) → SkillInputController → SkillSystem.Cast`；`Input(mouse) → CursorManager.ResolveHover → Cursor.SetCursor`；`提示 → GameData.message → UIManager`。

**改动文件清单**（与 TASK §4 允许改动逐项比对）

| 文件 | 改了什么 |
|---|---|
| `Assets/_Project/Scripts/UI/CursorManager.cs` | 删瞄准态早退；抽 `ResolveHover` 三层重载；相机空值兜底 |
| `Assets/_Project/Scripts/Player/SkillInputController.cs` | 新增 `PendingRange`；`PickEnemyAt`/`PickEnemyFromRay` 取代私有 `PickEnemy()`；3 条点击提示；`CastWithFeedback` |
| `Assets/_Project/Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs` | 注册并实现 `Skill.CursorHover`、`Skill.TargetPick`（反射调用，红阶段可编译） |
| `Docs/QA/Evidence/SKILL-ANIM-01/` | 红/绿日志、git 快照、README 索引 |
| `Docs/Handoff/REPORT_SKILL-ANIM-01.md` | 本报告 |
| `Docs/Learning/SKILL-ANIM-01_讲稿.md` | AI 初稿讲稿 |

未触碰：场景、预制体、`*.controller`、`*.asset`、`ProjectSettings`、`Packages`、`.meta`、两个 `.csproj`、`CURRENT.md`、`workflow-rules.md`。

## 5. 证据清单

| 文件 | 内容 |
|---|---|
| `Docs/QA/Evidence/SKILL-ANIM-01/SPEC01_Red_skillanim01red.log` | 红阶段完整日志（fail=2） |
| `Docs/QA/Evidence/SKILL-ANIM-01/SPEC01_Green_run{1,2,3}.log` | 三次绿跑完整日志 |
| `Docs/QA/Evidence/SKILL-ANIM-01/README.md` | 证据索引 + 复现命令 + 红阶段做法 |
| `Docs/QA/Evidence/SKILL-ANIM-01/git_status_snapshot.txt` | `git status --porcelain` 快照 |
| `Docs/QA/Evidence/SKILL-ANIM-01/git_head.txt` | `bc382e3ee7b5df51d7a68e74792eaa0c041482f9` |
| `Docs/QA/Evidence/SKILL-ANIM-01/git_diff_stat_assets.txt` | Assets 下仅 3 文件改动 |
| `Docs/QA/Evidence/SPEC-01/SPEC01_Green_skillanim01_run{1,2,3}_Assertions.jsonl` | 断言级 JSONL（canonical 目录，tag 追加） |
| `Docs/QA/Evidence/SPEC-01/SPEC01_Green_skillanim01_run{1,2,3}_Summary.json` | pass=12 / fail=0 / blocked=0 / infra=0 |
| `Docs/QA/Evidence/SPEC-01/SPEC01_Green_skillanim01_run{1,2,3}_ExitCode.txt` | 均为 `0` |
| `Docs/QA/Evidence/SPEC-01/SPEC01_Green_skillanim01red_run1_Assertions.jsonl` | 红阶段断言（10 pass / 2 product_fail） |

## 6. 学习讲稿

`Docs/Learning/SKILL-ANIM-01_讲稿.md`（AI 初稿，待所有者用自己的话改写）。

## 7. 未决项

1. **未运行（待所有者目检）**：AC-4 目检 1 —— 瞄准态下指针移到敌人显示攻击指针、移到空地显示普通指针（batchmode 无法验证屏幕像素指针）。
2. **未运行（待所有者目检）**：AC-4 目检 2 —— 点空地“未选中敌人”、点超距敌人“目标超出射程，正在接近”并追放、`Cast` 失败“技能释放失败”。追放链路与 `Cast` 失败消息口径已用生产代码实现，但未经人工点击确认。
3. `Skill.TargetPick` 的 `pendingRange=0`：batchmode 无键盘输入、无法稳定保持 `IsTargeting`（每帧会因 `_pendingIndex=-1` 取消），故该断言只验证纯查询函数；`PendingRange` 的瞄准态取值由目检 2 的追放观察覆盖。若后续需要自动化覆盖 `PendingRange`，需额外暴露测试专用的瞄准态注入点（会侵入产品 API，本次未做）。

其余：无信息不足项；未触发熔断（三次绿跑均一次通过）。

**说明（`.csproj`）**：本单未编辑两个 `.csproj`；但 Unity batchmode 启动时会重写自动生成的项目文件（`LastWriteTime` 落在本次运行窗口）。其 `git diff` 内容为既有差异（移除 `FND02B`、加入 `DIAG`/`SPEC01` 条目），与本单改动无关，按工单要求保持未提交、未还原。

## 8. 建议提交

```text
功能：瞄准态攻击指针与点击反馈（SKILL-ANIM-01）
为什么：瞄准时指针被强制为普通指针，玩家无法判断能否命中；点空地/超距/施放失败静默无反馈，现统一走 ResolveHover 分类并补 3 条提示，SPEC01 扩到 12 条并连续 3 次全绿。
```

`Docs/` 证据与报告可由主会话在用户“批准提交”后处理；代码与资产一律由用户提交。

## 9. 消耗记录

- 钱包：`Go 订阅`
- 模型完整 ID：`opencode-go/deepseek-v4.1-flash`
