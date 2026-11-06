# SKILL-TOOLS-01 — 调试键位与 SPEC01 动态遭遇基线

## §0 元信息

| 字段 | 值 |
|---|---|
| 任务 ID | `SKILL-TOOLS-01` |
| 风险等级 | `常规`（代码 + 测试；无资产改动） |
| 授权 | 所有者 2026-11-05 确认“2A”：测试基线 A + 调试键位 F5/F9/F10 |
| 基线提交 | `75de2329`（文档/流程批次；场景与控制器测试改动已随该提交入库） |
| 回滚锚 | `75de2329` / 撤销本单新增或修改的文件；不动资产 |
| 分支 | 当前分支 |
| 状态 | `Ready`（待所有者审 spec） |

## §1 需求（Why / What）

**一句话目标**：手动测试时不用退出 Play 就能补怪/重开/重置；同时让 SPEC01 的固定遭遇回归不再写死骷髅坐标。

**用户故事**：作为所有者，我要在 Play 中按一个键补一只骷髅、按一个键重载场景、按一个键重置队伍，以便连续测试技能；作为大脑，我要让自动化回归在你随意摆放测试骷髅时仍然有效。

**必须满足**

| 编号 | 需求 | 验收方式 |
|---|---|---|
| R-1 | `F5` 在启动时记录的骷髅位置生成 1 只 `Skeleton_Warrior`（存活、team=Monster） | 人工目检 + 日志 |
| R-2 | `F9` 在 Play 中重载当前场景（等价重新进 Play） | 人工目检 |
| R-3 | `F10` 重置所有玩家：满血/复活、满蓝、技能冷却清零 | 人工目检 |
| R-4 | SPEC01 `Regression.FND02A` 使用场景敌人的**实际位置**与相对接近路线，不再写死 `-12` | `SPEC01` 断言 |
| R-5 | 既有 SPEC01 10 条断言不回退 | `SPEC01` 10/10 ×3 |
| R-6 | 不产生 Console 错误；不修改场景/预制体/Animator/`.asset` | 日志 + `git status` |

**不在范围**：Q/W/E/R 动画机、瞄准指针/点击反馈（阶段 1）、任何玩法改动、场景布局改动。

**约束**：调试工具仅编辑器内生效（`#if UNITY_EDITOR`）；不引入新包；不改动既有资产；沿用 `UnitHealth.HealFull/Revive`、`UnitMana`、`SkillSystem` 现有职责。

## §2 方案（Plan）

**备选方案**

| 方案 | 做法 | 优点 | 缺点 | 结论 |
|---|---|---|---|---|
| A | 新建 `DebugTestTools`，`RuntimeInitializeOnLoadMethod` 自动挂载（仅编辑器） | 不碰场景；随时可用；随场景重载存活 | 代码里多一个调试入口 | **选定** |
| B | 把组件挂到 `Managers` 上，Inspector 拖预制体 | 直观 | 需要改场景 + 拖引用，属资产改动；重载后引用维护麻烦 | 弃用 |
| C | 用编辑器菜单/快捷键静态类，不进 Play | 零运行时 | 不能在 Play 中补怪/重置 | 弃用 |

**选定方案与理由**：A 不需要任何场景/预制体改动，符合“资产归所有者”的边界，且能在 Play 中直接工作。

**数据流**：

```
F5  → DebugTestTools.SpawnSkeleton()
      → AssetDatabase.LoadAssetAtPath("Assets/_Project/Prefabs/Skeleton_Warrior.prefab")
      → Instantiate 到 _spawnPoint（启动时记录的场景骷髅位置）
F9  → EditorSceneManager.LoadSceneInPlayMode(activeScene.path)
      → sceneLoaded 回调重新记录 _spawnPoint
F10 → DebugTestTools.ResetPlayers()
      → UnitHealth.HealFull()/Revive(maxHp) + UnitMana.Refill() + SkillSystem.ResetCooldowns()
SPEC01 Regression.FND02A
      → 读 _enemyStartPos（场景实际位置）
      → 计算朝敌人方向的 5m 站位点 → 采样 NavMesh → 下达移动 → 断言接近
```

**影响面清单**

| 项 | 内容 |
|---|---|
| 新增文件 | `Assets/_Project/Scripts/Debug/DebugTestTools.cs` + `.meta` |
| 修改符号 | `UnitMana.Refill()`（新增）；`SkillSystem.ResetCooldowns()`（新增）；`SPEC01_ProbeRunner` 的 `EnemyPos/ApproachDestZ/AssertRegressionFND02A` |
| 字符串引用 | 场景名 `DungeonWorld`；预制体路径 `Assets/_Project/Prefabs/Skeleton_Warrior.prefab`；按键 `F5/F9/F10` |
| 场景 / 预制体引用 | 无新增；不修改场景与预制体 |
| `.meta` | 新增 `.cs` 需配套 `.meta`；不移动既有文件 |

**实施顺序**：1) 调试工具与两个小 API；2) SPEC01 动态化；3) 编译；4) SPEC01 跑 3 次；5) 人工按键验证。

**风险与回滚**：F9 用编辑器 API，打包后不存在（`#if UNITY_EDITOR` 隔离）；动态接近逻辑要保留“真实寻路”语义，不能退化成瞬移/Warp；回滚 = 撤销上述文件改动。

## §3 验证（Validation）

| AC | Given / When / Then | 断言 ID | 证据 |
|---|---|---|---|
| AC-1 | 给定场景骷髅在任意合法位置，当 SPEC01 跑 `Regression.FND02A`，则自动开局、恰 1 只怪、真实寻路接近、零 Console 错误 | `Regression.FND02A` | `Docs/QA/Evidence/SKILL-TOOLS-01/SPEC01_run1..3.*` |
| AC-2 | 给定 SPEC01 连续跑 3 次，则 10/10 pass、fail=0、exit=0，既有 10 条不回退 | `all` | 同上 |
| AC-3 | 给定 Play 中按 `F5`，则在记录点新增 1 只存活骷髅 | 人工 | 目检记录 |
| AC-4 | 给定 Play 中按 `F9`，则场景重载、队伍/敌人复位 | 人工 | 目检记录 |
| AC-5 | 给定玩家掉血/耗蓝/技能进 CD 后按 `F10`，则满血/满蓝/冷却清零 | 人工 | 目检记录 |
| AC-6 | 给定本单 diff，则不出现场景/预制体/Animator/`.asset` 改动 | 静态 | `git status` 快照 |

**运行次数**：SPEC01 连续 **3 次**（时序/场景相关）；人工目检 3 项。

**必须保存的证据**：3 次 SPEC01 日志、Assertions JSONL、Summary、ExitCode；人工目检记录；`git status` 快照。

## §4 执行信息

| 项 | 内容 |
|---|---|
| 前置 | Unity 必须关闭（进程 0 + 无 `Temp/UnityLockfile`）；工作区仅剩两个自动生成的 `.csproj`，**不要提交、不要还原**；场景骷髅在 `z=15.8`，本单用动态位置适配 |
| 允许改动 | 上表 5 个文件（新增 1 + 修改 4） |
| 允许读取 | 本工单 + 上述文件 + `Docs/00` 的当前事实（如需要） |
| 绝不触碰 | `Assets/Scenes/**`、`Assets/_Project/Prefabs/**`、`*.controller`、`*.asset`、`ProjectSettings/**`、`Packages/**`、`CURRENT.md`、其他 `TASK_*`/`REPORT_*`/证据 |
| 证据目录 | `Docs/QA/Evidence/SKILL-TOOLS-01/`；canonical `SPEC-01/` 只追加，不覆盖 |
| 熔断 | 需要改资产、SPEC01 连续 3 次仍红、编译错连续 2 轮、Unity 未关、git 异常 |
| 执行预算 | 实现 1 轮 + 修复 1 轮；只跑 SPEC01 与必要编译 |

## §5 学习产出

| 项 | 要求 |
|---|---|
| 讲稿 | **不适用**：开发工具 + 测试基线，不改变玩家可见功能；在 REPORT 说明理由 |
| 三句话版本 | 交付前给出“这个工具解决什么、怎么用、为什么 SPEC01 不再写死坐标”三句 |

## 自包含声明

本工单不依赖 `CURRENT.md` 或其他任务书即可执行；信息不足时在 REPORT 写“信息不足”并列缺项，不自行翻阅其他文档补全。
