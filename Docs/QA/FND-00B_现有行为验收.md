# FND-00B — 现有行为验收记录

> 任务包：`Docs/Tasks/FND-00B_现有行为验收.md`
> 对应事实来源：`Docs/00_项目事实基线.md` 第 3.1 节
> 测试日期：2026-11-03（第 1 项实测）；补证日期：2026-11-03（第 1 项证据补拍）
> 执行人：用户（第 1～6 项 Unity 目视/操作）；FND-00B-04 第 7～9 项由执行者以 batchmode 自动断言执行（用户完成前置关停确认），人工目检 V04-1 待用户执行
> 当前状态：**第 1 项通过；第 2、3、5、7 项失败；第 8 项部分通过；第 9 项通过；第 4、6、11、12 项未测试**（FND-00B-03 批量验收 2026-11-03 执行；FND-00B-04 自动验收 2026-11-03 执行；FND-00B-05 尚未执行）；另含 1 项目标差距登记（不计入测试汇总）

**重要：** 本文件中所有"用户报告"字段仅转录 `Docs/00` 中的用户描述，**不构成本次验收结果**。未填写"实际结果"的条目结果一律为 `未测试`。FND-00B-04 第 7～9 项的实际结果来自可重复运行的 batchmode 自动断言（非用户口述），其人工目检 V04-1 单独记录、尚未执行。

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
| 实际结果 | 角色无响应，位置没有变化；Console 出现红色报错 `"GetRemainingDistance" can only be called on an active agent that has been placed on a NavMesh.`（`UnitBrain:get_ExecutingMove`，`Assets/_Project/Scripts/Units/UnitBrain.cs:53`；`CharacterBrain:Update`，`Assets/_Project/Scripts/Units/CharacterBrain.cs:54`）。路径表现、目标附近停步、移动过程中的镜头跟随均未观察到（因角色未移动）。只读核对：同一文本在 `Editor.log` 中由会前 0 处增至 2,783 处（2026-11-03 17:23:27 快照），唯一文本 1 种，多为连续重复 |
| 结果 | `失败` |
| 证据 | `Docs/QA/Evidence/FND-00B-03/验收口述记录.md`；`Docs/QA/Evidence/FND-00B-03/00_前置确认.txt`（含会前日志基线 0 处） |
| 备注 | 用户报告：已经实现（`Docs/00`，不构成本次结果）。用户推测（逐字，未验证）：“没有烘培寻路，每帧检查的时候都报错”；本项不分析原因、不修复。会话未提供截图，异常以用户口述原文与只读日志统计为据 |

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
| 备注 | 用户报告（`Docs/00`）：选中角色时无法左键拖动视角的问题再次出现（回归问题）。本项只测左键拖动，未用移动时的镜头跟随替代 |

---

## 4. 普攻

| 字段 | 内容 |
|---|---|
| 测试场景 | 名称：`DungeonWorld`；路径：`Assets/Scenes/DungeonWorld.unity`；来源：`工程打开已有`（同一次 Play 会话） |
| 前置状态 | 已选中角色；因第 2 项角色不能移动，视野受迷雾限制 |
| 操作步骤 | 1. 尝试在迷雾视野中选定敌人；2. 尝试下发普攻指令 |
| 预期结果 | 角色接近目标并按攻速持续攻击，伤害正确结算 |
| 实际结果 | 因角色不能移动，很难在迷雾视野中看到敌人，无法选定敌人，未能点出普攻指令；未观察到接敌、出招、持续攻击或伤害/血条变化 |
| 结果 | `未测试` |
| 证据 | `Docs/QA/Evidence/FND-00B-03/验收口述记录.md` |
| 备注 | 已尝试但被第 2 项状态阻塞，未实际执行普攻；未静默跳过，阻塞原因按用户口述原样记录。用户报告（`Docs/00`）：已经实现，不构成本次结果 |

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
| 备注 | 覆盖不完整：仅尝试骑士 `Q` 一项，其余技能与骑士盾击未实测；未确认 `Q` 对应哪个技能。用户报告（`Docs/00`）：骑士盾击已经实现，不构成本次结果 |

---

## 6. 动画表现

| 字段 | 内容 |
|---|---|
| 测试场景 | 名称：`DungeonWorld`；路径：`Assets/Scenes/DungeonWorld.unity`；来源：`工程打开已有`（同一次 Play 会话） |
| 前置状态 | 场景中同时存在角色与怪物；第 2 项角色不能移动 |
| 操作步骤 | 按工单汇总本会话可观察的待机/移动/攻击/受击/死亡；因移动与普攻均未成功，未能在末尾单独触发缺失状态 |
| 预期结果 | 角色的各动作按单位状态正常触发 |
| 实际结果 | 本会话未汇总任何动画状态的播放情况；待机、移动、攻击、受击、死亡均未观察记录 |
| 结果 | `未测试` |
| 证据 | `Docs/QA/Evidence/FND-00B-03/验收口述记录.md` |
| 备注 | 用户口述“同3”，即因不能移动未测试；未制造团灭。用户报告（`Docs/00`）：角色的大部分动作没有正常触发，需逐项列出具体状态，本次未取得 |

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
| 备注 | 用户报告“有相应代码和预制体，但尚未做到场景中”；本次运行数据与“场景集成未完成”的描述一致。本项只记录，不修复 |

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
| 备注 | 用户报告：能隐藏敌人，但角色周围视野范围未正确清除迷雾遮罩；**2026-11-03 FND-01 复验新差距：Play 中迷雾遮罩未显示**。本次自动数据确认：场景运行时无 `FogOfWar` 实例、无 fog 类 `MeshRenderer`；原因未分析、未修复。画面是否真实可见见 `Docs/QA/Evidence/FND-00B-04/04_人工目检.md`（V04-1，**待用户执行**） |

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
| 测试场景 | 待填写 |
| 前置状态 | 待填写 |
| 操作步骤 | 待填写 |
| 预期结果 | 待目标设计明确 |
| 实际结果 | 未执行 |
| 结果 | `未测试` |
| 证据 | 无 |
| 备注 | 用户报告：尚未实现 |

---

## 12. 团灭与游戏结束

| 字段 | 内容 |
|---|---|
| 测试场景 | 待填写 |
| 前置状态 | 队伍中所有角色可被击倒 |
| 操作步骤 | 待填写（建议：让全部角色倒地，观察是否触发游戏结束流程） |
| 预期结果 | 全员倒地后进入游戏结束流程 |
| 实际结果 | 未执行 |
| 结果 | `未测试` |
| 证据 | 无 |
| 备注 | 用户报告：所有角色可以全部倒地，但目前不会结束游戏；流程未完成。**2026-11-03 FND-01 复验新差距（用户报告）：角色移动正常，但长时间未遭遇任何怪物；原因未确认，“未生成或怪物无法移动”仅为待排查可能，不作为结论。只记录不修复，归入 FND-00B-05 待验；本备注不改变本项“未测试”状态** |

---

## 汇总

| # | 条目 | 结果 |
|---|---|---|
| 1 | 启动与场景 | `通过` |
| 2 | 移动 | `失败` |
| 3 | 镜头 | `失败` |
| 4 | 普攻 | `未测试` |
| 5 | 技能 | `失败` |
| 6 | 动画表现 | `未测试` |
| 7 | 宝箱 | `失败` |
| 8 | 迷雾 | `部分通过` |
| 9 | 救援（现状） | `通过` |
| 10 | 救援（目标） | `—（目标登记）` |
| 11 | 传送 | `未测试` |
| 12 | 团灭与游戏结束 | `未测试` |

第 10 项为**目标差距登记**，不在本轮执行，不计入下列统计。

现有行为验收共 **11 项**：通过 2 ｜ 部分通过 1 ｜ 失败 4 ｜ 未测试 4。目标差距登记 1 项，不计入通过率与未测试总数。

> 本汇总为 2026-11-03 实测结果：第 1 项经同日补证后为 `通过`（补证提交 `af07ddb1130bf23e82847c99183ef86c850f5cc0`）；第 2、3、5 项为 FND-00B-03 批量会话实测结果；第 4、6 项因移动失效被阻塞未测试；第 7～9 项为 FND-00B-04 batchmode 自动断言结果（HEAD `e7688025e7a00fbeb1f7c9f36950ce4e353963a5`，连续运行 2 次一致）；第 11、12 项尚未执行。AI 不得预填或推测结果。
> 第 8 项的 `部分通过` = 敌人隐藏（`Fog.EnemyVisibility`）通过，但生产场景无 `FogOfWar` 实例（`Fog.MaskState` 失败、`Fog.VisionClearing` 阻塞）；画面可见性属人工目检 V04-1（`Docs/QA/Evidence/FND-00B-04/04_人工目检.md`，待用户执行），不以自动数据替代。
