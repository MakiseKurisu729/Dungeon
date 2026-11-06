# SKILL-ANIM-01 — 瞄准态攻击指针与点击反馈

## §0 元信息

| 字段 | 值 |
|---|---|
| 任务 ID | `SKILL-ANIM-01` |
| 风险等级 | `常规`（2 个生产脚本 + 测试追加；不动动画机、不动资产） |
| 授权 | 所有者 2026-11-05 已确认方向“先做瞄准指针 + 点击反馈”；本 spec 待所有者审 |
| 基线提交 | `bc382e3e` |
| 回滚锚 | `bc382e3e` / 撤销本单 3 个文件改动 |
| 分支 | 当前分支 |
| 状态 | `Ready`（待所有者审 spec） |

## §1 需求（Why / What）

**一句话目标**：瞄准技能时，指针移到敌人上显示攻击指针；点击空地/超距/失败时给出可见提示，不再静默。

**用户故事**：作为玩家，我在按 Q/W 进入瞄准后，要能从指针判断“这一下能不能打到谁”；作为所有者，我要在测试时清楚知道为什么没放出来。

**必须满足**

| 编号 | 需求 | 验收方式 |
|---|---|---|
| R-1 | 瞄准态悬停存活敌人 → 攻击指针；悬停空地/友军 → 维持现有规则（pointer/select） | 断言 + 目检 |
| R-2 | 点击空地 → 提示“未选中敌人”并退出瞄准（保留点空地取消语义） | 目检 |
| R-3 | 点击超出射程的敌人 → 提示“目标超出射程，正在接近”并进入追放 | 目检 |
| R-4 | `Cast` 返回 false 且无既有消息 → 提示“技能释放失败” | 目检 |
| R-5 | 新增两条回归断言，SPEC01 由 10 条变 12 条，连续 3 次 12/12 | `SPEC01` |
| R-6 | 不改动画机/资产；既有 10 条断言不回退 | `git status` + `SPEC01` |

**不在范围**：Q/W/E/R 子状态机、远程 `AttackRange` 子状态机、Bash 过渡 `HasExitTime`（属 `SKILL-ANIM-02`）；不改技能数值/冷却；不改场景/预制体。

**约束**：只改 `CursorManager`、`SkillInputController`、`SPEC01_ProbeRunner`；保留 `CursorManager` 四态语义与染色逻辑；不新增光标贴图；批量模式可断言。

## §2 方案（Plan）

**备选方案**

| 方案 | 做法 | 优点 | 缺点 | 结论 |
|---|---|---|---|---|
| A | `CursorManager` 瞄准态不再提前 return，继续做 hover；抽出纯函数 `ResolveHover(Ray)` | 光标逻辑仍集中在一处；可测试 | 需要一次小重构 | **选定** |
| B | 由 `SkillInputController` 直接 `Cursor.SetCursor` | 改动少 | 输入层耦合 UI，绕过四态/染色；不可测 | 弃用 |
| C | 新建 `TargetingCursor` 组件 | 隔离 | 复制一套 hover 逻辑，维护两份 | 弃用 |

**选定方案与理由**：A 只移除“瞄准就强制普通指针”的早退，并保留同一套射线分类；同时把分类抽成纯函数，batchmode 可用合成射线断言。

**数据流**：

```
按 Q/W → SkillInputController 进入 IsTargeting，记录 PendingRange
每帧 → CursorManager.Update()
        → Camera.main.ScreenPointToRay(Input.mousePosition)
        → ResolveHover(ray)：RaycastAll → 分类为 Attack / Select / Interact / Pointer
        → Apply(对应贴图)
左键 → SkillInputController.PickEnemyFromRay(ray)
        → 命中 Monster → 射程内 Cast / 超距 chase
        → 未命中 → 提示“未选中敌人” + Cancel
```

**影响面清单**

| 项 | 内容 |
|---|---|
| 新增文件 | 无（只改 3 个既有文件） |
| 修改符号 | `CursorManager.Update`、新增 `CursorManager.ResolveHover(Ray)`；`SkillInputController.PendingRange`、`PickEnemyAt(Vector3)`、`PickEnemyFromRay(Ray)`、点击分支消息；`SPEC01_ProbeRunner` 新增 2 条断言 |
| 字符串引用 | 光标状态枚举 `CursorManager.Hover.Attack`；提示文案 3 条 |
| 场景 / 预制体引用 | 无 |
| `.meta` | 无新增/移动 |

**实施顺序**：1) `CursorManager` 重构 + 移除瞄准早退；2) `SkillInputController` 暴露 `PendingRange`/`PickEnemyFromRay` + 提示；3) SPEC01 两条断言；4) 编译；5) SPEC01 ×3；6) 人工目检。

**风险与回滚**：`ResolveHover` 重构可能改变非瞄准态行为 → 用 SPEC01 既有 10 条与人工目检兜底；`Camera.main` 为空 → 断言走 infra/blocked，不伪造；回滚 = 撤销 3 个文件。

## §3 验证（Validation）

| AC | Given / When / Then | 断言 ID | 证据 |
|---|---|---|---|
| AC-1 | 给定瞄准态与一条从相机指向敌人胸口的射线，当调用 `ResolveHover(ray)`，则返回 `Hover.Attack` | `Skill.CursorHover` | `Docs/QA/Evidence/SKILL-ANIM-01/` |
| AC-2 | 给定同一条射线，当调用 `PickEnemyFromRay(ray)`，则返回该敌人的 `UnitHealth`（team=Monster） | `Skill.TargetPick` | 同上 |
| AC-3 | 给定 SPEC01 连续 3 次，则 12/12 pass、fail=0、blocked=0、infra=0、exit=0，既有 10 条不回退 | `all` | 同上 |
| AC-4 | 给定瞄准态，指针移到敌人 → 攻击指针；移到空地 → 普通指针；点空地 → “未选中敌人”；点超距敌人 → “目标超出射程，正在接近”并追放 | 人工 | 目检记录 |
| AC-5 | 给定本单 diff，则不改动画机/场景/预制体/`.asset`；无 Console 错误 | 静态 + 日志 | `git status`、运行日志 |

**运行次数**：SPEC01 连续 **3 次**（时序/相机相关）；人工目检 2 项（指针 + 提示）。

**必须保存的证据**：3 次 SPEC01 日志、Assertions JSONL、Summary、ExitCode；`git status` 快照；人工目检记录。

**测试实现要点**：
- `Skill.CursorHover`：`ray = new Ray(Camera.main.transform.position, (enemyChest - cameraPos).normalized)`；调用 `CursorManager.instance.ResolveHover(ray)`；记录 `hover`、`enemy`、`distance`、`camera`。
- `Skill.TargetPick`：同一条射线调用 `SkillInputController.instance.PickEnemyFromRay(ray)`；断言返回的 `UnitHealth.team == Monster`；记录命中名、距离、`PendingRange`、`inRange`。
- 两条都 `kind=production`；`Camera.main`/`SkillInputController.instance` 为空 → `InfraFailure`，不得伪造通过。
- **不使用** `WorldToScreenPoint` 作为主路径（避免敌人不在视锥时 blocked）；屏幕坐标路径只做人工目检。

## §4 执行信息

| 项 | 内容 |
|---|---|
| 前置 | Unity 必须关闭（进程 0 + 无 `Temp/UnityLockfile`）；工作区还有 3 个未提交文档（`CURRENT.md`、`workflow-rules.md`、本 TASK）与两个 `.csproj`——**不要提交、不要还原、不要修改它们** |
| 允许改动 | `Assets/_Project/Scripts/UI/CursorManager.cs`、`Assets/_Project/Scripts/Player/SkillInputController.cs`、`Assets/_Project/Tests/SPEC01/Runtime/SPEC01_ProbeRunner.cs`、`Docs/QA/Evidence/SKILL-ANIM-01/`、`Docs/Handoff/REPORT_SKILL-ANIM-01.md`、`Docs/Learning/SKILL-ANIM-01_讲稿.md` |
| 允许读取 | 本工单 + 上述 3 个源文件 |
| 绝不触碰 | `Assets/Scenes/**`、`Assets/_Project/Prefabs/**`、`*.controller`、`*.asset`、`ProjectSettings/**`、`Packages/**`、`CURRENT.md`、其他 `TASK_*`/`REPORT_*`/证据 |
| 证据目录 | `Docs/QA/Evidence/SKILL-ANIM-01/`；canonical `SPEC-01/` 只追加，用 tag |
| 熔断 | 需要改动画机/资产、SPEC01 连续 3 次仍红、编译错连续 2 轮、Unity 未关、git 异常 |
| 执行预算 | 实现 1 轮 + 修复 1 轮；只跑 SPEC01 |

## §5 学习产出

| 项 | 要求 |
|---|---|
| 讲稿 | **必须**：`Docs/Learning/SKILL-ANIM-01_讲稿.md`，套模板；AI 初稿 + 所有者改写 |
| 必含 | 为什么瞄准时强制普通指针是错的；`ResolveHover` 的数据流；为什么不在 `SkillInputController` 里直接设光标；关键代码 3–5 处；边界（相机为空、敌人死亡、UI 遮挡） |
| 三句话版本 | 写进 REPORT §3：① 瞄准态也做 hover；② 敌人→攻击指针、空地→普通指针；③ 点击给出原因并保留追放 |

## 自包含声明

本工单不依赖 `CURRENT.md` 或其他任务书即可执行；信息不足时在 REPORT 写“信息不足”并列缺项，不自行翻阅其他文档补全。
