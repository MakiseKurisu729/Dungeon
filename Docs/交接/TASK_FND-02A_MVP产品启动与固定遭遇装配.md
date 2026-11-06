# TASK_FND-02A — MVP 产品启动与固定遭遇装配

> 状态：`Done`（2026-11-04 自动断言 2 次 7/7 通过 + V02A-1 人工目检通过 + 外部顾问复核与【大脑】终审收口）
> 风险：**高风险**（公共流程状态＋生产主场景；须外部顾问＋【大脑】双重终审）
> 类型：【重构＋新增】产品启动接线与固定遭遇装配
> 架构依据：`Docs/01_总体架构与内容生产规范.md` §3.1 组合根显式接线、§4.1 局内/楼层生命周期、§7.2 场景根模板
> 对应结构债：SD-01；后续 FND-02B 依赖本单真实固定遭遇
> 前置事实：FND-00B 已复审收口；FND-01 真实右键移动已由用户人工验收通过
> 唯一报告：`Docs/Handoff/REPORT_FND-02A.md`

## 1. 唯一结果与边界

进入真实 `Assets/Scenes/DungeonWorld.unity` Play 后，由产品自身直接自动开局；正式场景保存 1 个可复现的既有 `Skeleton_Warrior` 固定遭遇，玩家可经现有移动指令接近，为后续普攻/动画单提供真实前置。

- 已批准业务决策：自动开局，无主菜单点击步骤；只新增 1 个 `Skeleton_Warrior`；启用既有生产 AI/生命/导航组件；由唯一现有产品入口负责开局；不顺带改造主菜单。
- 候选初始位置：`(-2.72, 0.08333, 7)`。执行者只读核对后须确认该坐标；如需调整，必须在影响面清单给出新坐标与 NavMesh 依据并由用户确认，不得实施时自行改点。
- 只修启动接线与固定遭遇装配；不修普攻、伤害、动画、镜头、技能、迷雾、宝箱、救援、传送或团灭/重来。
- 不开发通用刷怪器、随机生成、Boot/Town、新主菜单流程或 `UnitRegistry`；不升级 Unity、不安装包、不重新烘焙导航、不做全场景整理。
- 本单通过不代表战斗、团灭或完整 MVP 通过。

## 2. 已知事实与禁止误判

- `GameFlow.Start()` 当前将静态 `GameStarted` 置为 `false`，`Update()` 在未开局时返回。
- 既有 `MainMenuPanel.OnClose()` 会恢复 `timeScale=1` 并置 `GameStarted=true`；此前实测主菜单未接线，产品路径未开局。
- 既有实测场景怪物 0、生成器 0、20s 新增 0；原因未确认，不得编造根因。
- `Assets/_Project/Prefabs/Skeleton_Warrior.prefab` 存在。FND-00B-06 曾以其内存实例在候选点附近验证生产移动链可接近；这只是选址依据，不是本单产品证据。
- 旧 FND-00B 的“场景怪物 0”是历史前置；不得修改旧 QA/证据适配本单新场景。

## 3. 阶段闸门与 Unity 安全

### 阶段 1：只读影响面确认

执行者只读本工单，禁止翻阅其他背景文档补全要求。可只读检查代码、场景/预制体引用和现有测试入口，但在任何文件编辑或 Unity 调用前必须：

1. 记录分支、完整 HEAD、`git status --porcelain` 原始输出。工单自身及本单新增 `Docs/Handoff/`、`Docs/QA/` 文档可豁免；其他脏项全部列出并停止交用户处理，尤其不得混入旧测试资产或 `.csproj` 改动。
2. 输出“受影响文件与符号清单”：唯一自动开局组件及调用链、场景实例与预制体 GUID、字符串引用、最终坐标、测试脚本/`.meta`、可能自动生成的 `.csproj`、Evidence/REPORT。
3. 核查 `GameStarted`、`MainMenuPanel.OnClose`、`UIManager.Start/mainMenuPanel`，确保不多头开局；列出 `SelectionManager`/`CharacterBrain` 的真实移动命令签名。
4. **输出清单后停止，等待用户明确确认。** 未确认不得编辑、写测试资产、保存场景或调用 Unity。

### 阶段 2：red 与实施前置

- 写入测试资产或进行任何 Unity 调用前，必须请求用户关闭编辑器并等待明确确认；未确认不得动手。隔离副本须事先告知代价并获同意。
- 仅允许通过 Unity 编辑器 API 装配并保存本单场景变化；禁止整文件重写 Unity YAML。保存前后必须核对局部 diff 与 GUID 引用。
- 不执行任何 Git 写操作；代码与资产由用户亲自提交。

## 4. 最大影响面（最终授权以用户确认清单为准）

- 生产场景：`Assets/Scenes/DungeonWorld.unity`，仅新增批准的敌人实例及必要接线。
- 启动代码：只选择 `GameFlow.cs` 或既有产品入口中的最小单一接线点；`MainMenuPanel.cs`/`UIManager.cs` 非默认修改，确有必需须单列并获确认。
- 只读引用：`Skeleton_Warrior.prefab`、既有选择/移动/AI/Animator 组件；预计不编辑预制体、Animator 或既有 `.meta`。
- 测试资产：仅新增 `Assets/_Project/Tests/FND02A/{Editor,Runtime}/` 下 2～3 个本单 `.cs` 及自动 `.meta`；新增测试 `.cs` 导致的 `.csproj` 自动 Compile 引用变化视为随资产批准。
- 文档证据：仅新增 `Docs/QA/Evidence/FND-02A/`、`Docs/Handoff/REPORT_FND-02A.md`。不得修改 `CURRENT.md`、旧 QA 或其他生产文件。
- 禁止修改 `ProjectSettings`、`Packages`、导航烘焙数据及任务外场景/预制体。

## 5. TDD：先 red，再最小实现到 green

### 5.1 固定顺序

1. 完成两项用户确认后，只新增测试资产，在生产场景/脚本尚未修改时运行 red。
2. 有效 red 至少要求 `Startup.ProductEntry` 与 `Encounter.SceneCensus` 因当前产品状态失败；编译错误、入口不存在或测试基础设施异常不算有效 red，须先修测试基础设施。
3. 保存 red 断言、Summary、ExitCode 和原始日志后，才允许按确认清单最小实施。
4. 使用同一主入口连续运行 green **2 次**；判定不一致标“不稳定”，不得挑选通过的一次。若出现间歇性/时序问题，相关验证改为 **N≥3**。
5. 保留 FND-01 移动回归；不得通过修改旧 FND-00B 测试或历史结论制造兼容。

建议唯一入口 `FND02A_BatchAcceptance.Run()`。每次重新加载真实场景，测试对象统一前缀 `FND02A_`，结束后清理运行时测试对象；产品敌人不是测试夹具，不得删除或改写成内存实例。

## 6. 自动化验收标准

| 断言 ID | 客观预期 | 必留数据 |
|---|---|---|
| `Startup.ProductEntry` | 真实场景进入 Play、无任何测试状态写入或测试调用开局入口后，产品自身令 `GameStarted=true`、`timeScale=1`，无主菜单阻塞 | 产品入口与调用链、首帧/稳定采样、开局时间、测试写入标志=false |
| `Startup.OnceAndStable` | 观察窗内仅 1 次 `false→true`；既有玩家仍为 3，敌人不重复生成，状态稳定 | 上升沿次数、单位 ID 清单、观察时长、可取得的事件/订阅计数 |
| `Encounter.SceneCensus` | 生产场景恰有本单新增的 1 个普通敌人，非测试内存实例；未新增随机刷怪来源 | 敌人 ID、场景路径、team、hp、active、场景怪物/生成器计数 |
| `Encounter.FixedIdentity` | 来源为批准的 `Skeleton_Warrior.prefab`，初始位置/旋转与最终批准值一致，两次运行一致 | 预制体 GUID、场景文件实例 ID、坐标、容差、逐次采样 |
| `Encounter.NavMesh` | 敌人 Agent 启用且 `isOnNavMesh=true`，既有生产 AI 未被测试禁用 | Agent/Brain 状态、NavMesh 采样、异常计数 |
| `Encounter.RealApproach` | 通过现有选择/移动命令从玩家出生区接近固定遭遇；不使用 Warp、不写玩家坐标、不直接驱动 Agent 冒充指令 | 方法签名、起止距离、路径状态、耗时、最大单帧位移、目标位置 |
| `Regression.NoNewErrors` | 无新增导航异常或阻塞性错误，FND-01 既有移动路线仍可用 | Console 分类计数、异常原文、回归路线与到达数据 |

- batchmode 无法产生真实鼠标时，允许使用与生产点击链一致的选择/移动方法作自动等价输入，必须记录调用链；不得以直接 `SetDestination` 代替玩家指令。
- 断言 JSONL 字段至少含 `run`、`assertion`、`expected`、`actual`、`pass`、`status`、`timestamp`、`data`。`blocked`、基础设施失败和产品失败不得写成通过。
- 每次记录随机种子、开始/结束时间、退出码；原始日志完整保留。

## 7. 证据与人工目检

证据目录：`Docs/QA/Evidence/FND-02A/`，至少包含：

- `00_前置确认.txt`
- `02A_Red_Assertions.jsonl`、`02A_Red_Summary.json`、`02A_Red_ExitCode.txt`、`02A_Red_Automation.log`
- `02A_Assertions.jsonl`
- `02A_Automation_run1.log`、`02A_Automation_run2.log`
- `02A_Summary_run1.json`、`02A_Summary_run2.json`
- `02A_ExitCode_run1.txt`、`02A_ExitCode_run2.txt`
- `02A_人工目检.md`

人工目检仅 1 项：`V02A-1` 在真实 Game 视图确认无菜单阻挡、能看到固定敌人，并用真实右键完成一次接近。执行者只留“待目检”，由用户填写，不得代填；目检不替代自动断言。

## 8. 实现约束与回滚

- 自动开局只能有一个产品所有者；测试不得写 `GameStarted`、调用测试后门或主动调用开局方法制造通过。
- 固定敌人必须保存在生产场景，使用既有预制体和生产参数；不得测试时禁用其 AI 后仍声称真实遭遇通过。
- 若启动接线异常，仅撤销本单确认的代码 hunk；若场景装配失败，仅移除本单实例与接线，不整文件重写或清理其他 YAML。
- 需要版本恢复时由用户亲自执行 Git 恢复；AI 不执行 `reset/checkout/restore/clean`。执行者提供精确反向 diff 与 GUID 清单。
- 回滚后核对生产 diff 回到本单前状态并复验 FND-01 移动；red/green 证据保留并标记“已回滚”，不得删除失败记录。

## 9. REPORT 与完成判定

REPORT 必须包含：结果摘要、完整 HEAD、唯一入口与完整命令、red/green 两次结果、逐项断言表、原始日志、夹具边界（本单产品断言不得使用夹具）、完整 diff、变更文件、未覆盖项、实现原理（500 字内）、受影响模块导航说明（200 字内）、Go 订阅及 `opencode-go/deepseek-v4.1-flash` 完整模型 ID、遗留问题和“建议提交”精确清单。

完成条件：有效 red 已保留；同一入口 green 连续 2 次全部必需断言通过；场景/代码 diff 未越权；V02A-1 已单列；REPORT 完整。**高风险批次须经用户送外部顾问复核，再由【大脑】终审后才可标记 Done；执行者不得修改 CURRENT 或自行宣称 FND-02A 完成。**
