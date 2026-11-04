# FND-00B — 现有行为验收记录

> 任务包：`Docs/Tasks/FND-00B_现有行为验收.md`
> 对应事实来源：`Docs/00_项目事实基线.md` 第 3.1 节
> 测试日期：2026-11-03（第 1 项实测）；补证日期：2026-11-03（第 1 项证据补拍）；批末复审：2026-11-03；补验日期：2026-11-04（第 4、6 项，FND-00B-06）
> 执行人：用户（第 1～6 项 Unity 目视/操作）；FND-00B-04 第 7～9 项由执行者以 batchmode 自动断言执行（用户完成前置关停确认）；FND-00B-05 第 11～12 项由执行者以 batchmode 自动断言执行（用户完成前置关停确认）；FND-00B-06 第 4、6 项由执行者以 batchmode 自动断言执行（用户完成前置关停确认）。人工目检 V04-1 已记录“未见遮罩”，V05-1 已记录“未能达成团灭”，V06-1 待目检；表内未填细项标“待目检”
> 当前状态：**第 1、2、4、9 项通过；第 3、5、6、7、12 项失败；第 8 项部分通过；第 11 项未测试**（第 2 项依据 FND-01 修复后人工验收在批末复审改判；第 4、6 项由 FND-00B-06 补验；第 11 项因 `OBSERVED_NO_ENTRY` 保持未测试并登记待实现差距）；另含 1 项目标差距登记（不计入测试汇总）

**重要：** 本文件中所有"用户报告"字段仅转录 `Docs/00` 中的用户描述，**不构成本次验收结果**。未填写"实际结果"的条目结果一律为 `未测试`。FND-00B-04 第 7～9 项、FND-00B-05 第 11～12 项、FND-00B-06 第 4/6 项的实际结果来自可重复运行的 batchmode 自动断言（非用户口述）；其中 FND-00B-04 的人工目检 V04-1、FND-00B-05 的人工目检 V05-1、FND-00B-06 的人工目检 V06-1 单独记录。

---

## 1. 启动与场景

| 字段 | 内容 |
|---|---|
| 测试场景 | 名称：`DungeonWorld`；路径：`Assets/Scenes/DungeonWorld.unity`；路径来源：用户确认 + AI 只读核对文件存在（**非截图证明**）；场景来源：`工程打开已有`。`03_测试场景.png` 与 `01_工程已打开.png` 为同一张截图，属复用图，不单独证明完整路径 |
| 前置状态 | 原验收提交：`e08590889577792ae449e59329f8f74fd3f28f40`；补证提交：`af07ddb1130bf23e82847c99183ef86c850f5cc0`；分支：`docs/fnd-00-baseline`；补证前工作区干净：`是`；Unity：`2022.3.62f3c1` |
| 操作步骤 | 1. 用 Unity `2022.3.62f3c1` 打开工程，等待导入与编译自然完成，未点修复建议、未改设置；2. 确认工程打开后即为 `DungeonWorld` 场景，未切换或保存场景；3. 进入 Play 模式，只观察能否进入与 Console 状态，未操作移动、攻击等玩法；观察后退出 Play，退出时无保存场景提示 |
| 预期结果 | 工程可正常打开，可进入 Play 模式，无阻塞性编译错误 |
| 实际结果 | 工程正常打开，进入编辑器界面；Play 前 Console 无红色错误行，Console 底部为一条 `Couldn't find a readme`（Log，来源 `ReadmeEditor`）；成功进入 Play，运行画面为绿色地面，右上角 HUD 显示“金币：0 / 等级：1 / 击杀：0”，场景中有 3 个角色与 1 个敌人，底部有 4 个深色图标格；Play 期间 Console 有 3 条黄色 `Failed to create agent because there is no valid NavMesh` 与 1 条 Log `0 个身体部件已挂到 PlayerBody 层`（`DebugCameraSwitcher.Start`，`Assets/_Project/Scripts/Debug/DebugCameraSwitcher.cs:102`）；无红色错误；退出 Play 无保存提示。另：退出本次 Play 之后，用户另行执行“选中角色 → 右键点击地面”时出现红色报错 `"GetRemainingDistance" can only be called on an active agent that has been placed on a NavMesh.`（`UnitBrain:get_ExecutingMove`，`Assets/_Project/Scripts/Units/UnitBrain.cs:53`；`CharacterBrain:Update`，`Assets/_Project/Scripts/Units/CharacterBrain.cs:54`），该操作属第 2 项“移动”，不计入本项结论 |
| 结果 | `通过` |
| 证据 | 原验收：`Docs/QA/Evidence/FND-00B-01/00_前置确认.txt`；`01_工程已打开.png`；`02_Play_运行.png`；`03_测试场景.png`（与 `01_工程已打开.png` 为同一张截图，复用图）。补证（2026-11-03）：`00R_前置确认.txt`；`01_Console_编译检查.png`；`02_Play_Console.png`；`04_构建与最近验收.txt`；`Unity_启动与Play.log` |
| 备注 | 可运行构建：`不存在`（AI 只读核对：全仓无 `*.exe`/`*.apk`、无构建输出目录，见 `04_构建与最近验收.txt`）；最近一次完整验收：`无书面记录；用户称今天做过测试但认为已不可靠、需重新验收`，具体时间与范围标为待用户确认；信息来源：`用户报告`。**补证记录（2026-11-03，补证提交 `af07ddb1130bf23e82847c99183ef86c850f5cc0`）：** Play 前 Console 计数 `信息 1｜警告 0｜错误 0`，仅 1 条 `Couldn't find a readme`（`ReadmeEditor.cs:90`）；进入 Play 后计数 `信息 1｜警告 3｜错误 0`，3 条 `Failed to create agent because there is no valid NavMesh`（`[15:50:55]`）原样复现，另有 1 条 `0 个身体部件已挂到 PlayerBody 层`（`[15:50:56]`，`DebugCameraSwitcher.cs:102`）；无阻塞性编译错误（无 `error CS`、无 Exception）；已成功进入 Play 并观察到 Game 视图与 HUD；退出 Play 未产生资产变更（`git status --porcelain -- Assets ProjectSettings Packages` 为空）。**原运行记录的异常予以保留，未删除、未据此宣称已修复。** 补证期间发现：Play 前那条 readme 在 Play 后不再出现（信息计数仍为 1），用户明确回复未手动清除，清除来源未确认，详见 `Unity_启动与Play.log`。用户口述推测：`自动寻路 NavMesh 烘焙可能需要重新烘焙`（仅为用户推测，未验证，本项不分析、不修复） |

### 第 1 项复审记录（2026-11-03）

- K3.1 对照 `TASK_FND-00B-01R` 复审 **通过**：证据提交为 `af07ddb1130bf23e82847c99183ef86c850f5cc0`，文档改判提交为 `28bdaced`（复审指定短 SHA）。
- 两张补证截图由执行者使用 Windows `PrintWindow` 抓取真实 Unity 编辑器窗口，取证方式已在修正报告第 6 节和 `Unity_启动与Play.log` 中披露；复审接受该证据形式，不将其描述为用户手动截图。
- Play 前 1 条 readme 日志在 Play 后消失，清除来源仍未确认；复审不归因，该事项不作为“无阻塞性编译错误”结论的反证。
- `03_测试场景.png` 为复用图；场景完整路径仅依据“用户确认 + 文件存在核对”，不声称由截图证明。该披露符合修正工单要求。

---

## 2. 移动

| 字段 | 内容 |
|---|---|
| 测试场景 | 名称：`DungeonWorld`；路径：`Assets/Scenes/DungeonWorld.unity`；来源：`工程打开已有`（同一次 Play 会话） |
| 前置状态 | 验收提交：`bf21b78dd5e41c80697c333c4f891f10f3852405`；分支：`docs/fnd-00-baseline`；工作区干净：`是`；Unity：`2022.3.62f3c1`；场景中有 3 个可操控角色，起始位置约 `(0,0,0)`，各角色 x 相差个位数 |
| 操作步骤 | 1. 依次选中三个角色；2. 对明确地面目标右键下发移动指令（用户口述“无响应，位置没变”） |
| 预期结果 | 角色按指令移动到目标点，过程中镜头跟随正常 |
| 实际结果 | 角色无响应，位置没有变化；Console 出现红色报错 `"GetRemainingDistance" can only be called on an active agent that has been placed on a NavMesh.`（`UnitBrain:get_ExecutingMove`，`Assets/_Project/Scripts/Units/UnitBrain.cs:53`；`CharacterBrain:Update`，`Assets/_Project/Scripts/Units/CharacterBrain.cs:54`）。路径表现、目标附近停步、移动过程中的镜头跟随均未观察到（因角色未移动）。只读核对：同一文本在 `Editor.log` 中由会前 0 处增至 2,783 处（2026-11-03 17:23:27 快照），唯一文本 1 种，多为连续重复。**批末复验：FND-01 修复后，自动化验证沿路径到达且新增导航异常 0；用户随后人工确认真实右键移动正常。** |
| 结果 | `通过`（2026-11-03 批末复审改判；原失败记录保留） |
| 证据 | 原实测：`Docs/QA/Evidence/FND-00B-03/验收口述记录.md`、`00_前置确认.txt`；复验：`Docs/Handoff/REPORT_FND-01.md` 第 4 节、`Docs/QA/Evidence/FND-01/自动化验证输出.txt`；人工通过结论见 `Docs/Handoff/CURRENT.md` |
| 备注 | 改判仅覆盖 FND-01 后的移动核心行为，不回写原批次历史，也不顺带判定镜头跟随、普攻或动画通过。 |

---

## 3. 镜头

| 字段 | 内容 |
|---|---|
| 测试场景 | 名称：`DungeonWorld`；路径：`Assets/Scenes/DungeonWorld.unity`；来源：`工程打开已有`（同一次 Play 会话） |
| 前置状态 | 已选中角色（与第 2 项同一次 Play） |
| 操作步骤 | 1. 选中角色后按住鼠标左键拖动；2. 对照：未选中状态下按住左键拖动 |
| 预期结果 | 选中角色状态下仍可按住左键拖动视角 |
| 实际结果 | 选中角色后视角不能拖动；未选中状态下可以拖动 |
| 结果 | `失败` |
| 证据 | `Docs/QA/Evidence/FND-00B-03/验收口述记录.md` |
| 备注 | 用户报告（`Docs/00`）：选中角色时无法左键拖动视角的问题再次出现（回归问题）。本项只测左键拖动，未用移动时的镜头跟随替代。**批末复审：FND-01 不含镜头修复，且无新实测证据，保留 `失败`；不属于 NavMesh 连带复验项，后续独立复验。** |

---

## 4. 普攻

| 字段 | 内容 |
|---|---|
| 测试场景 | 名称：`DungeonWorld`；路径：`Assets/Scenes/DungeonWorld.unity`；来源：batchmode 加载真实场景（目标为内存夹具，未保存场景） |
| 前置状态 | 分支：`docs/fnd-00-baseline`；HEAD：`fa6aaf78f4f7ed0ba9e929d24094aa7f1ff74616`；前置工作区：干净；Unity：`2022.3.62f3c1`；主入口：`FND00B06_BatchAcceptance.Run`；连续运行 `run=1`、`run=2`；随机种子 20261105 / 20261106；场景玩家 3、场景怪物 0；所选玩家 `Knight_Unit`（`SwordSwing` 近战，hp 100，agent 启用且在 NavMesh 上） |
| 操作步骤 | 1. 用生产 `SelectionManager.SingleSelect` 选中 `Knight_Unit`；2. 用生产 `SelectionManager.SelectEnemy` + `CharacterBrain.CommandAttack` 下发一次普攻命令（与 `SelectionManager.HandleClick` 内同一组生产方法；batchmode 无真实鼠标射线输入，按工单允许作自动等价输入）；3. 以既有敌人预制体 `Skeleton_Warrior` 在玩家 7m 外内存实例化目标夹具（关闭其 `MonsterBrain`），并临时禁用另两名玩家的 `CharacterBrain` 以避免其自动攻击污染观测；4. 由 `CharacterBrain.Update → UnitBrain.AttackLoop` 追击接敌（不使用 Warp）；5. 观察出招、持续结算与死亡；6. 同一入口连续运行 2 次 |
| 预期结果 | 命令被接受且目标正确；经现有移动链接近至攻击范围；持续出招并按攻速结算伤害；目标死亡仅一次且不重复结算 |
| 实际结果 | 两次运行 7 条断言一致（pass=7）：`Attack.Fixture` 玩家/目标/双方 Agent 状态齐备、场景怪物 0；`Attack.CommandAccepted` 选中、目标标记、命令目标均正确（true×3）；`Attack.Approach` 距离 7.000→2.398/2.398m、耗时 1.485/1.484s、配置范围 2.2m、最大单帧位移 0.015m、无 Warp；`Attack.Starts` 首次出招 0.056s、目标锁定正确；`Attack.Repeats` 结算 4 次（run1 间隔 0.998/1.001/1.001s，run2 0.999/1.001/0.999s；`attackSpeed=1` 对应期望 1.0s），非本玩家命中 0；`Attack.DamageResolved` 每次 HP 前后差值等于结算伤害、累计 60、目标 HP 60→0；`Attack.TargetDeath` 死亡事件 1 次、死亡后 1s 内无新结算。玩家 HP 全程 100 |
| 结果 | `通过` |
| 证据 | `Docs/QA/Evidence/FND-00B-06/06_Assertions.jsonl`（`Attack.Fixture`、`Attack.CommandAccepted`、`Attack.Approach`、`Attack.Starts`、`Attack.Repeats`、`Attack.DamageResolved`、`Attack.TargetDeath`）；`06_Automation_run1.log`、`06_Automation_run2.log`；`06_Summary_run1.json`、`06_Summary_run2.json`；`06_ExitCode_run1.txt`、`06_ExitCode_run2.txt` |
| 备注 | 夹具边界：目标为 `Skeleton_Warrior` 内存实例、关闭其 AI、不写入场景；另两名玩家 brain 仅运行时禁用并在第 6 项前恢复，未改资产。本项通过**不消除**“场景怪物 0、生成器 0”的已登记差距，也不代表怪物 AI/遭遇通过。不同步测移动/技能/团灭；`Animation.Hit` 的夹具伤害不计入本项。V06-1 画面核对待目检 |

---

## 5. 技能

| 字段 | 内容 |
|---|---|
| 测试场景 | 名称：`DungeonWorld`；路径：`Assets/Scenes/DungeonWorld.unity`；来源：`工程打开已有`（同一次 Play 会话） |
| 前置状态 | 已选中角色；场景中实际职业清单未逐一确认 |
| 操作步骤 | 1. 选中骑士；2. 按 `Q` 尝试释放技能；其余技能未尝试 |
| 预期结果 | 当前场景中已确认存在的技能按目标设计释放，范围与效果符合目标 |
| 实际结果 | 选中骑士按 `Q` 没有弹出技能指示器；未观察到任何技能释放。场景中存在的职业与可用技能清单、其他职业技能、骑士盾击均未确认或未实测 |
| 结果 | `失败` |
| 证据 | `Docs/QA/Evidence/FND-00B-03/验收口述记录.md` |
| 备注 | 覆盖不完整：仅尝试骑士 `Q` 一项，其余技能与骑士盾击未实测；未确认 `Q` 对应哪个技能。用户报告（`Docs/00`）：骑士盾击已经实现，不构成本次结果。**批末复审：FND-01 不含技能修复，原操作未被移动阻塞，保留 `失败`；不属于 NavMesh 连带复验项。** |

---

## 6. 动画表现

| 字段 | 内容 |
|---|---|
| 测试场景 | 名称：`DungeonWorld`；路径：`Assets/Scenes/DungeonWorld.unity`；来源：batchmode 加载真实场景（受击/倒地使用内存夹具，未保存场景） |
| 前置状态 | 同第 4 项（HEAD `fa6aaf78f4f7ed0ba9e929d24094aa7f1ff74616`）；Animator：`OC_Knight`（基于 `PlayerBase.controller`），1 层，13 个参数、9 个状态（只读登记） |
| 操作步骤 | 1. 进入 Play、未下达任何指令时采样 Idle；2. 复用第 4 项追击过程采样 Move；3. 与实际出招/伤害时序对照采样 Attack；4. 用 `Knight_Unit` 内存夹具经生产 `UnitHealth.ApplyDamage` 施加一次非致命伤害（5），观察受击；5. 同一夹具经生产伤害入口施加致命伤害，观察 dead/isDown 与 Animator 状态；6. 战后 7.6s 补充观察是否回到 Locomotion；连续运行 2 次 |
| 预期结果 | 待机/移动/攻击/受击/倒地五类状态与实际逻辑一致，可由运行期状态名与参数逐项断言 |
| 实际结果 | **Animator 映射（只读）**：`controller=OC_Knight`、`overrideBase=PlayerBase`、`layerCount=1`；`SwordSwing.attackLayer=1` 超出层数、`attackStateName="Attack"` 不在状态列表（实际为 `Attack_Draw`/`Attack_Release`），不一致项 2 条。**Idle** 通过：战前状态 ` Locomotion`（状态名含前导空格）、SpeedX/SpeedZ=0、Grounded=true。**Move** 通过：最大速度 0.759/0.754 m/s，SpeedZ 同步 0.759/0.754，全程 ` Locomotion`。**Attack** 通过：命令后 1.882/1.884s 进入 `Attack_Draw`，首次伤害 1.897/1.897s（动画早于伤害 0.015/0.013s），随后 `Attack_Release`。**Hit** 失败：`ApplyDamage(5)` 生效（hp 100→95、OnHit 1 次、玩家 HP 不变），但 2s 内 Animator 始终为 ` Locomotion`，未进入 `Hit_A`。**Downed/Death** 通过：致命伤害后 dead=true、IsDown=true、死亡事件 1 次、进入 `Downed`，`IsWiped=false`、`timeScale=1`（未制造团灭）。**补充观察（不计入五类判定）**：战后 7.6s 仍停留 `Attack_Draw`（normalizedTime≈0.63、swordTimer=-1） |
| 结果 | `失败`（受击状态未进入；另登记受击无生产触发器与战后不回 Locomotion 两项表现问题） |
| 证据 | `Docs/QA/Evidence/FND-00B-06/06_Assertions.jsonl`（`Animation.ConfigMapping`、`Animation.Idle`、`Animation.Move`、`Animation.Attack`、`Animation.Hit`、`Animation.DownedOrDeath`、`Animation.PostCombatState`）；`06_Automation_run1.log`、`06_Automation_run2.log`；`06_Summary_run1.json`、`06_Summary_run2.json` |
| 备注 | 受击未进入的只读原因链：玩家侧无任何生产代码对 Animator 设置 `Hit` 触发器（仅 `MonsterBrain` 与 `DownedRescue` 救援打断使用），故 `ApplyDamage` 只触发事件与闪光；未修复。战后停留攻击状态：控制器链路 `Attack_Draw --Attack--> Attack_Release --Attack--> Locomotion` 需连续 `Attack` 触发器推进，出招中途停止后无触发器收尾，故不回待机；该项按“只记录不修复”处理。夹具边界：受击/倒地使用 `Knight_Unit` 内存实例并禁用其 brain，未改资产；受击伤害只作动画刺激，不计入第 4 项。未覆盖：技能/Bash/Jump 等非五类状态。V06-1 未填写，标“待目检” |

---

## 7. 宝箱

| 字段 | 内容 |
|---|---|
| 测试场景 | 名称：`DungeonWorld`；路径：`Assets/Scenes/DungeonWorld.unity`；来源：batchmode 加载真实场景（内存夹具，未保存场景） |
| 前置状态 | 分支：`docs/fnd-00-baseline`；HEAD：`e7688025e7a00fbeb1f7c9f36950ce4e353963a5`；前置工作区：干净；Unity：`2022.3.62f3c1`；主入口：`FND00B04_BatchAcceptance.Run`；连续运行 `run=1`、`run=2` |
| 操作步骤 | 1. 加载 `DungeonWorld` 并进入 Play；2. 扫描场景内 `Chest` 实例（含 inactive），记录路径/位置/activeSelf/activeInHierarchy/交互组件状态；3. 实例数为 0 时对交互与产出输出明确的 blocked 记录，不静默跳过；4. 同一入口连续运行 2 次 |
| 预期结果 | 场景存在至少 1 个宝箱实例；可通过真实交互入口触发开箱；产出物数量/背包状态符合现有预期 |
| 实际结果 | `Chest.SceneCount`：`count=0`（run1/run2 一致），场景无宝箱实例；`Chest.Interactable`、`Chest.ProducesItem` 均为 `blocked`（无交互入口，无法开箱，产出不可断言）。只读数据：`chest_base.prefab`、`chest_gold.prefab` 资产存在，但不据此判通过 |
| 结果 | `失败` |
| 证据 | `Docs/QA/Evidence/FND-00B-04/04_Assertions.jsonl`（`Chest.SceneCount`、`Chest.Interactable`、`Chest.ProducesItem`）；`04_Automation_run1.log`、`04_Automation_run2.log`；`04_Summary_run1.json`、`04_Summary_run2.json` |
| 备注 | 用户报告“有相应代码和预制体，但尚未做到场景中”；本次运行数据与“场景集成未完成”的描述一致。**批末复审登记为待实现差距：宝箱未装配；交互与产出仍待装配后复验。** 本项只记录，不修复 |

---

## 8. 迷雾

| 字段 | 内容 |
|---|---|
| 测试场景 | 名称：`DungeonWorld`；路径：`Assets/Scenes/DungeonWorld.unity`；来源：batchmode 加载真实场景（内存夹具，未保存场景） |
| 前置状态 | 同第 7 项；场景内 3 个玩家单位（Knight 预制体变体）均存活；场景无怪物生成源（`SkeletonSpawner` 计数 0） |
| 操作步骤 | 1. 扫描场景内 `FogOfWar` 实例（含 inactive）与名称含 fog 的 `MeshRenderer`，记录组件/网格/材质数据；2. 使用生产 `Visibility` 组件 + 内存夹具敌人，分别在 30m（视野外）与 4m（视野内）采样 `IsHidden` 与 `Renderer.enabled`；3. 生产遮罩缺失时对清除表现输出 blocked；4. 另建立内存 `FogOfWar` 夹具（96×96、revealRadius=10、fogAlpha=0.82、mapSize=210），采样角色移动前后的顶点 alpha（参考探针，不计入生产结论）；5. 连续运行 2 次 |
| 预期结果 | Play 中存在启用的迷雾遮罩；视野外敌人隐藏、进入视野后显示；角色周围视野范围内遮罩被清除、范围外保持迷雾 |
| 实际结果 | `Fog.MaskState`：`fogCount=0`、`fogLikeRendererCount=0`（run1/run2 一致）→ 失败，场景无遮罩实例。`Fog.EnemyVisibility`：30m 处 `IsHidden=true`/`rendererEnabled=false`；4m 处 `IsHidden=false`/`rendererEnabled=true` → 通过（run1 参照 `Mage_Unit`，run2 参照 `Archer_Unit`，判定一致）。`Fog.VisionClearing`：`blocked`（无遮罩可采样）。夹具参考探针：初始 alpha 0.82/0.82；角色周围 0.049/0.047（<0.05）、范围外 0.82；移动 20m 后新位置 0.049/0.050、旧区域 0、范围外 0.82 → 通过（仅证明算法数据，不作为本项通过依据） |
| 结果 | `部分通过`（敌人隐藏通过；遮罩未显示、视野清除表现无法由生产数据断言） |
| 证据 | `Docs/QA/Evidence/FND-00B-04/04_Assertions.jsonl`（`Fog.MaskState`、`Fog.EnemyVisibility`、`Fog.VisionClearing`、`Fog.VisionClearing.fixtureProbe`）；`04_Automation_run1.log`、`04_Automation_run2.log`；`04_Summary_run1.json`、`04_Summary_run2.json` |
| 备注 | 用户报告：能隐藏敌人，但角色周围视野范围未正确清除迷雾遮罩；**新登记差距：生产场景迷雾遮罩缺失**。自动数据确认运行时无 `FogOfWar` 实例、无 fog 类 `MeshRenderer`；原因未分析、未修复。V04-1 的记录状态为“未见遮罩，与 `fogCount=0` 一致”；表内未填细项标“待目检”，见 `Docs/QA/Evidence/FND-00B-04/04_人工目检.md`。 |

---

## 9. 救援（现状行为）

| 字段 | 内容 |
|---|---|
| 测试场景 | 名称：`DungeonWorld`；路径：`Assets/Scenes/DungeonWorld.unity`；来源：batchmode 加载真实场景（内存夹具，未保存场景） |
| 前置状态 | 同第 7 项；场景 3 个玩家单位（`Knight_Unit` 预制体变体：`Knight_Unit`/`Archer_Unit`/`Mage_Unit`）均带 `DownedRescue`+`Selectable`+`CharacterBrain`，team=Player；无怪物生成源 |
| 操作步骤 | 1. 用真实伤害管线击倒场景内 `Knight_Unit`（hp 100 → -10；`dead=true`、`isDown=true`，AI/移动被禁用）；2. 实例化同一 `Knight_Unit` 预制体作为救援者（内存夹具），置于 6.32m 外；3. 通过 `SelectionManager.SingleSelect` 选中救援者（等价左键单选，**未使用点击目标流程**，未调用任何救援完成方法）；4. 通过 `NavMeshAgent.Warp` 靠近至 1.2m（`rescueRange=2.5`）；5. 观察自动触发与完成；连续运行 2 次 |
| 预期结果 | 现状：选中可行动角色并靠近倒地角色后自动开始救援，并在限定时间内完成（本项不断言“悬停图标→点击指令”的目标需求） |
| 实际结果 | `Rescue.Fixture`／`Rescue.AutoTrigger`／`Rescue.Completes` 两次运行一致：倒地 1、可行动 1；选中后自动触发延迟约 `0.00001s`、触发距离 `1.2m`；救援耗时 `2.997s`（rescueTime=3）完成；最终 `hp=1`、`dead=false`、`isDown=false`、`OnRevived` 1 次；事件链 `OnDeath → OnRevived`；brain/agent 恢复启用；未触发团灭（`IsWiped=false`、`timeScale=1`） |
| 结果 | `通过` |
| 证据 | `Docs/QA/Evidence/FND-00B-04/04_Assertions.jsonl`（`Rescue.Fixture`、`Rescue.AutoTrigger`、`Rescue.Completes`）；`04_Automation_run1.log`、`04_Automation_run2.log`；`04_Summary_run1.json`、`04_Summary_run2.json` |
| 备注 | 只验当前自动救援；未断言第 10 项目标交互，未让全员倒地。用户报告“靠近后自动救援、救援可完成”与本次数据一致；当前交互方式不符合目标需求的结论仍以第 10 项登记为准 |

---

## 10. 救援（目标操作）— 目标差距登记（不在本轮执行，不计入测试汇总）

| 字段 | 内容 |
|---|---|
| 测试场景 | 待填写 |
| 前置状态 | 场上有倒地角色，另有可行动角色 |
| 操作步骤 | 目标流程（供未来验收使用）：选中救援角色 → 指针移到倒地角色上显示救援图标 → 点击下发救援指令 |
| 预期结果 | 按上述目标交互完成救援 |
| 实际结果 | —（本轮不执行，仅登记目标差距） |
| 结果 | `—（目标登记）` |
| 证据 | 无 |
| 备注 | **这是目标需求，不是现有能力**；不在本轮执行，不计入测试汇总；FND-00B 不实现它 |

---

## 11. 传送

| 字段 | 内容 |
|---|---|
| 测试场景 | 名称：`DungeonWorld`；路径：`Assets/Scenes/DungeonWorld.unity`；来源：batchmode 加载真实场景（内存夹具，未保存场景） |
| 前置状态 | 分支：`docs/fnd-00-baseline`；HEAD：`739f5be705643ee74f4d2221265a518d56f2f57e`；前置工作区：干净；Unity：`2022.3.62f3c1`；主入口：`FND00B05_BatchAcceptance.Run`；连续运行 `run=1`、`run=2`；随机种子 20261104 / 20261105 |
| 操作步骤 | 1. 运行时枚举场景内可辨认的传送入口/组件（`DungeonDoor`、类型名含 `Teleport`/`Portal`，含 inactive），并输出已加载类型清单；2. 无入口时输出 `OBSERVED_NO_ENTRY`，`TargetData` 与 `ReachesTarget` 明确 `blocked`；3. 另建内存夹具（`DungeonDoor` + `TransitionFader`）调用生产 `Enter()`，核对 3 名玩家落点；4. 同一入口连续运行 2 次 |
| 预期结果 | 目标设计尚未明确；若存在入口，应能从源点经交互触发到达配置目标 |
| 实际结果 | `Teleport.EntryCensus`：`count=0`、`dungeonDoorInstances=0`、`transitionFaderInstances=0`（run1/run2 一致）；已加载类型清单含 `DungeonDoor @ Assembly-CSharp`、`TransitionFader @ Assembly-CSharp`（说明类型/源码存在，不等于场景集成）。`Teleport.TargetData`、`Teleport.ReachesTarget`：两次均 `blocked`（无生产入口可读/可触发）。夹具探针 `Teleport.ReachesTarget.fixtureProbe`：调用生产 `Enter()` 后 3 名玩家落点与 `dungeonEntryPoint +` 生产偏移的误差均为 `0.000m`（容差 0.5m），移动距离 16.43～22.44m，过场完成、相机焦点已设置；该夹具仅证明实现机制，**不作为本项通过依据** |
| 结果 | `未测试`（`OBSERVED_NO_ENTRY`：场景无入口且目标设计未明确，无法形成通过/失败判定） |
| 证据 | `Docs/QA/Evidence/FND-00B-05/05_Assertions.jsonl`（`Teleport.EntryCensus`、`Teleport.TargetData`、`Teleport.ReachesTarget`、`Teleport.ReachesTarget.fixtureProbe`）；`05_Automation_run1.log`、`05_Automation_run2.log`；`05_Summary_run1.json`、`05_Summary_run2.json` |
| 备注 | 用户报告"尚未实现"与运行期场景无入口一致。**批末复审登记为待实现差距：传送无场景入口；本项仍以 `未测试` 保留，不把差距登记写成通过或失败。** 本项不推断"代码不存在"（类型清单已单独输出），也不把夹具探针写成功能通过。目标设计仍待明确。 |

---

## 12. 团灭与游戏结束

| 字段 | 内容 |
|---|---|
| 测试场景 | 名称：`DungeonWorld`；路径：`Assets/Scenes/DungeonWorld.unity`；来源：batchmode 加载真实场景（内存夹具，未保存场景） |
| 前置状态 | 同第 11 项（HEAD `739f5be705643ee74f4d2221265a518d56f2f57e`）；场景 3 个玩家单位（`Knight_Unit`/`Archer_Unit`/`Mage_Unit`）均存活且带 `DownedRescue`；场景怪物 0、生成器 0；`GameStarted=false`、`UIManager.mainMenuPanel` 未接线 |
| 操作步骤 | 1. 团灭夹具前输出初始怪物数、20s 固定窗口新增、怪物 ID/位置、生成器数量与状态；2. 逐只输出 `NavMeshAgent` 状态（无怪物则标未确认，并输出玩家 agent 参照）；3. 沿固定采样路线 + 18s 固定窗口记录遭遇事件数、最近距离、怪物位移；4. 用真实伤害入口 `UnitHealth.ApplyDamage` 令 3 名玩家全部倒地；5. 等待 8s 观察 `GameFlow` 团灭链与 UI；6. 夹具内仅补 `GameStarted=true` 隔离验证团灭链本身；连续运行 2 次 |
| 预期结果 | 全员倒地后进入游戏结束流程，且仅触发一次 |
| 实际结果 | **怪物前置数据（单独列示，不归因）**：`Monster.Population` 初始 0、20s 新增 0、最终 0、生成器 0（两次一致）；`Monster.NavMeshState` 怪物 0 → `未确认`（参照 3 名玩家 `agentEnabled=true`/`isOnNavMesh=true`/`pathPending=false`，NavMesh 三角顶点 2558）；`Monster.EncounterProbe` 遭遇事件 0、最近距离 `null`、怪物位移记录 0，路线采样 6/到达 4、移动 13.61m（两次一致）。**团灭链**：`Party.AllDown` 3 名玩家 hp 100→-10、`dead=true`、`isDown=true`、brain/agent 禁用，存活计数 3→0。`GameEnd.TriggerChain` 等待 8.000s 未触发（`risingEdges=0`、`IsWiped=false`、`timeScale=1`、`GameStarted=false`、HUD 消息为空）；`GameEnd.FinalState` 同上，无 GameOver 类 UI，输入未锁定。**夹具控制**（仅补 `GameStarted=true`）：0.0018s 触发，`IsWiped=true`、`timeScale=0`、消息"全队覆没！按 R 重来"显示于 HUD `messageText`，2s 内仅 1 次 |
| 结果 | `失败`（产品路径：全员倒地后未进入游戏结束流程） |
| 证据 | `Docs/QA/Evidence/FND-00B-05/05_Assertions.jsonl`（`Monster.Population`、`Monster.NavMeshState`、`Monster.EncounterProbe`、`Party.AllDown`、`GameEnd.TriggerChain`、`GameEnd.FinalState`、`GameEnd.TriggerChain.fixtureControl`）；`05_Automation_run1.log`、`05_Automation_run2.log`；`05_Summary_run1.json`、`05_Summary_run2.json` |
| 备注 | 与用户报告"所有角色可以全部倒地，但目前不会结束游戏；流程未完成"一致。**新登记差距：怪物零生成/零遭遇（场景怪物 0、生成器 0、20s 新增 0），原因未确认；团灭产品链未触发。** 不把"未生成或怪物无法移动"写成原因结论。夹具控制只隔离验证团灭链本身，不作为本项通过依据。V05-1 记录状态为“因为没有生成敌人，角色没办法受伤，没办法测试团灭”；未填画面细项标“待目检”，见 `Docs/QA/Evidence/FND-00B-05/05_人工目检.md`。 |

---

## 汇总

| # | 条目 | 结果 |
|---|---|---|
| 1 | 启动与场景 | `通过` |
| 2 | 移动 | `通过`（FND-01 后批末改判） |
| 3 | 镜头 | `失败` |
| 4 | 普攻 | `通过` |
| 5 | 技能 | `失败` |
| 6 | 动画表现 | `失败` |
| 7 | 宝箱 | `失败` |
| 8 | 迷雾 | `部分通过` |
| 9 | 救援（现状） | `通过` |
| 10 | 救援（目标） | `—（目标登记）` |
| 11 | 传送 | `未测试` |
| 12 | 团灭与游戏结束 | `失败` |

第 10 项为**目标差距登记**，不在本轮执行，不计入下列统计。

现有行为验收共 **11 项**：通过 4 ｜ 部分通过 1 ｜ 失败 5 ｜ 未测试 1。目标差距登记 1 项，不计入通过率与未测试总数；宝箱、传送、怪物遭遇等待实现差距已登记，但不篡改对应 QA 结果。

> 本汇总为 2026-11-03 实测与批末复审结果：第 1 项经补证通过；第 2 项原实测失败，依据 FND-01 自动化证据及用户人工验收改判通过；第 3、5 项保持原失败；第 4、6 项由 FND-00B-06（2026-11-04）连续两次自动断言补验：第 4 项 7/7 断言通过改判 `通过`，第 6 项因受击状态未进入判 `失败`；第 7～9 项采用 FND-00B-04 连续两次自动断言；第 11～12 项采用 FND-00B-05 连续两次、11/11 一致的自动断言。AI 不得预填或推测结果。
> 第 8 项的 `部分通过` = 敌人隐藏通过，但生产遮罩缺失、视野清除阻塞；V04-1 已记录“未见遮罩”，未填细项标“待目检”（`Docs/QA/Evidence/FND-00B-04/04_人工目检.md`）。
> 第 11 项的 `未测试` = 场景无传送入口，已登记待实现差距；夹具探针只证明实现机制。第 12 项的 `失败` = 全员倒地后产品路径未触发游戏结束；怪物零生成/零遭遇单独登记、不归因。V05-1 已记录“无法测试团灭”，未填画面细项标“待目检”（`Docs/QA/Evidence/FND-00B-05/05_人工目检.md`）。
> **批末收口判断：2026-11-04【大脑】复审通过并收口（FND-00B-06）。** 第 4 项通过、第 6 项失败均为有效验收结论；第 8、11 项均已有行为结论或差距登记，不单独阻塞；V06-1 仍待目检（`Docs/QA/Evidence/FND-00B-06/06_人工目检.md`），不作为通过依据。
