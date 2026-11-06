# REPORT_FND-00B-06 — 普攻与动画补验

> 工单：`Docs/Handoff/TASK_FND-00B-06_普攻与动画补验.md`（风险：低风险）
> 钱包：Go 订阅；模型：opencode-go/deepseek-v4.1-flash
> 执行日期：2026-11-04
> 分支：`docs/fnd-00-baseline`；完整 HEAD：`fa6aaf78f4f7ed0ba9e929d24094aa7f1ff74616`
> 前置工作区：`git status --porcelain` 无输出（干净）
> 结论：第 4 项**通过**；第 6 项**失败**。FND-00B 是否收口交【大脑】复审，执行者未标记 `Done`。

---

## 1. 结果摘要

| 项 | 结果 | 依据 | 两次一致性 |
|---|---|---|---|
| QA 第 4 项 普攻 | `通过` | 7 条产品断言全部 pass | 两次一致 |
| QA 第 6 项 动画表现 | `失败` | 五类状态中 Idle/Move/Attack/Downed 通过、`Animation.Hit` 失败 | 两次一致 |
| 汇总 | pass=11、fail=1、blocked=0、infra=0、observed=2（每条运行） | 14 条断言 × 2 次 | 断言集合完全相同 |

- 两次运行退出码均为 `2`（0=全通过；2=存在产品失败/阻塞；3=基础设施失败）。
- 前置阻塞 0；测试基础设施失败 0。
- 另记录 2 条不计入五类判定的观察：`Animation.ConfigMapping`（映射不一致 2 处）、`Animation.PostCombatState`（战后停留在 `Attack_Draw`）。

---

## 2. 前置确认与边界

- Unity `2022.3.62f3c1`（`ProjectSettings/ProjectVersion.txt`），可执行文件 `D:\unity\2022.3.62f3c1\Editor\Unity.exe`。
- 用户已明确回复“编辑器已关闭，可以开始”；执行前未检测到 Unity 进程、`Temp\UnityLockfile` 不存在。
- 记录见 `Docs/QA/Evidence/FND-00B-06/00_前置确认.txt`（含运行期实测的玩家 Agent 导航数据、两次运行时间与退出码）。
- 过程异常（已记录、非产品问题）：第一次批处理调用时 Unity 进程被外部提前终止，留下 0 字节日志与陈旧 `Temp\UnityLockfile`；确认无 Unity 进程后删除该临时锁文件再继续。删除对象仅为 `Temp` 下临时文件。
- 未修改：生产代码、`Assets/Scenes/DungeonWorld.unity`、预制体、Animator 控制器、`ProjectSettings`、`Packages`、`CURRENT.md`；未执行任何 Git 写操作（commit/push/checkout/reset 等均无）。
- Unity 自动重生成 `Assembly-CSharp.csproj`、`Assembly-CSharp-Editor.csproj`，各新增 1～2 行 `<Compile Include=...FND00B06...>`（非人工编辑），已在变更清单与建议提交中披露。

---

## 3. 测试资产与唯一主入口

| 文件 | 说明 |
|---|---|
| `Assets/_Project/Tests/FND00B/Editor/FND00B06_BatchAcceptance.cs` | 唯一主入口 `FND00B06_BatchAcceptance.Run` + `FND00B06_Boot` 接续（沿用 04/05 的 batchmode Play 路线） |
| `Assets/_Project/Tests/FND00B/Runtime/FND00B06_ProbeRunner.cs` | 运行期探针：第 4 项 → 第 6 项，内存夹具、只读采样 |
| `Assets/_Project/Tests/FND00B/Runtime/FND00B06_Evidence.cs` | 断言 JSONL / Summary 写入（字段含 run、qaItem、assertion、expected、actual、pass、status、timestamp、data） |
| `.meta` | 由 Unity 首次导入自动生成（GUID 不可手写）：`471b76a3ac6f45043918718c01bbdf79`（BatchAcceptance）、`767c5f45f4f66854c88507b0f3f7c356`（ProbeRunner）、`887732b2abc92e5478bb2346dd254607`（Evidence） |

完整命令（Windows PowerShell 可直接复现；两次仅 `$env:FND00B06_RUN` 与日志文件名不同）：

run1：
```powershell
$env:FND00B06_RUN="1"
& "D:\unity\2022.3.62f3c1\Editor\Unity.exe" -batchmode -projectPath "D:\unity\Dungeon" -executeMethod FND00B06_BatchAcceptance.Run -logFile "D:\unity\Dungeon\Docs\QA\Evidence\FND-00B-06\06_Automation_run1.log"
"EXITCODE=$LASTEXITCODE"
```

run2：
```powershell
$env:FND00B06_RUN="2"
& "D:\unity\2022.3.62f3c1\Editor\Unity.exe" -batchmode -projectPath "D:\unity\Dungeon" -executeMethod FND00B06_BatchAcceptance.Run -logFile "D:\unity\Dungeon\Docs\QA\Evidence\FND-00B-06\06_Automation_run2.log"
"EXITCODE=$LASTEXITCODE"
```

说明：`&` 调用 GUI 子系统程序时 PowerShell 可能不等退出即返回；本单实际执行使用 `Start-Process -Wait -PassThru` 等待并取得 `exitCode=2`（运行结果与日志路径一致）。

运行参数：run1 seed=`20261105`，14:27:28.219→14:27:43.964；run2 seed=`20261106`，14:28:23.239→14:28:38.977；时长均约 15.7s。

---

## 4. 逐项断言表（14 条 × 2 次）

| # | qaItem | assertion | run1 | run2 | 关键数据（run1 / run2） |
|---|---|---|---|---|---|
| 1 | 4 | `Attack.Fixture` | pass | pass | 玩家 `Knight_Unit` hp100、agentOnNavMesh=true；目标 `Skeleton_Warrior` 内存实例 hp60、7m；场景怪物 0、玩家 3 |
| 2 | 4 | `Attack.CommandAccepted` | pass | pass | playerSelected/primaryIsPlayer/enemyMarked/orderedTargetIsTarget 全 true |
| 3 | 4 | `Attack.Approach` | pass | pass | 7.000→2.398/2.398m；1.485/1.484s；maxFrameStep 0.015/0.015m；teleport=false；PathComplete |
| 4 | 4 | `Attack.Starts` | pass | pass | 首次出招 0.056s；SwordSwing 锁定目标=true |
| 5 | 4 | `Attack.Repeats` | pass | pass | 结算 4 次；非本玩家命中 0；间隔 0.998/1.001/1.001 与 0.999/1.001/0.999（期望 1.0s） |
| 6 | 4 | `Attack.DamageResolved` | pass | pass | 4 次、累计 60 伤害、HP 60→0、每次 HP 差值=结算值 |
| 7 | 4 | `Attack.TargetDeath` | pass | pass | 死亡事件 1 次；死亡时刻 3.408/3.406s；之后 1s 无新结算 |
| 8 | 6 | `Animation.ConfigMapping` | observed（映射不一致） | observed（同） | `OC_Knight`/`PlayerBase`、layerCount=1；`attackLayer=1` 越界；`attackStateName="Attack"` 不存在（实际 `Attack_Draw`/`Attack_Release`） |
| 9 | 6 | `Animation.Idle` | pass | pass | 战前 ` Locomotion`、SpeedX/SpeedZ=0、Grounded=true |
| 10 | 6 | `Animation.PostCombatState` | observed（未回待机） | observed（同） | 战后 7.6s 仍 `Attack_Draw`，normalizedTime 0.634/0.631，swordTimer=-1 |
| 11 | 6 | `Animation.Move` | pass | pass | 最大 agent 速度 0.759/0.754m/s，SpeedZ 同步，全程 ` Locomotion`，24 采样 |
| 12 | 6 | `Animation.Attack` | pass | pass | 进入 `Attack_Draw` 于命令后 1.882/1.884s，首次伤害 1.897/1.897s（动画早于伤害 0.015/0.013s），进入攻击状态 3 次 |
| 13 | 6 | `Animation.Hit` | product_fail | product_fail | `ApplyDamage(5)`：hp100→95、OnHit 1 次；2s 内状态仍 ` Locomotion`，未进入 `Hit_A` |
| 14 | 6 | `Animation.DownedOrDeath` | pass | pass | dead=true、IsDown=true、死亡事件 1 次、状态 `Downed`；IsWiped=false、timeScale=1 |

两次断言集合比对：14 个 ID 完全一致，`pass`/`status` 逐条相同（脚本比对输出 `SETS_IDENTICAL`）。

---

## 5. 第 4 项：普攻实测要点

- **输入等价性**：batchmode 无真实鼠标射线，按工单允许使用生产方法作自动等价输入——`SelectionManager.SingleSelect(Selectable)`（左键单选）、`SelectionManager.SelectEnemy(UnitHealth)`（点敌人）+ `CharacterBrain.CommandAttack(Transform)`；与 `SelectionManager.HandleClick` 内调用的方法一致。未逐帧调用执行器、未直接写 HP。
- **接敌**：由 `CharacterBrain.Update → UnitBrain.AttackLoop → agent.SetDestination` 完成，7m→2.398m；`teleportDetected=false`、最大单帧位移 0.015m，未使用 Warp / transform 直写。
- **持续攻击**：4 次结算、间隔 1.0s，与 `SwordSwing.attackLen=1 / UnitStats.attackSpeed=1` 一致；同时记录 `swingCount`（1，受私有 `_timer` 采样抖动影响）仅作对照，判定以 `OnHit` 次数为准。
- **夹具边界**：目标为 `Skeleton_Warrior` 预制体内存实例（关闭 `MonsterBrain`，不写场景）；另两名玩家 `CharacterBrain` 仅运行时禁用、第 6 项前恢复（首轮试跑发现其“坚守自动攻击”会命中夹具目标并污染结算，已隔离并在断言中单列 `unattributedHitCount`，两次正式运行均为 0）。
- 本项通过**不消除**“场景怪物 0 / 生成器 0”的已登记差距，也不代表怪物 AI 或遭遇通过。

---

## 6. 第 6 项：动画实测要点

- **只读映射登记**：`OC_Knight`（基于 `PlayerBase.controller`）1 层、13 个参数、9 个状态；控制器链路为
  ` Locomotion --Attack(exit 0.732)--> Attack_Draw --Attack(exit 0.8125)--> Attack_Release --Attack(exit 0.8125)--> Locomotion`。
- **Two mismatches（只记录、未修复）**：
  1. `SwordSwing.attackLayer = 1`，而 Animator 只有 1 层（索引 0）→ `ReadAnimProgress()` 恒返回 -1，命中窗口实际回落到代码时钟 `_timer/_curLen`；
  2. `SwordSwing.attackStateName = "Attack"` 不存在，实际状态名为 `Attack_Draw` / `Attack_Release`。
- **Idle / Move / Attack / Downed**：均通过；Attack 的状态进入发生在首次伤害前 ~0.014s，说明“动画与伤害时序一致”。
- **Hit 失败（产品断言）**：`UnitHealth.ApplyDamage(5)` 走生产伤害入口，事件与血量变化正确（100→95、OnHit 1 次），但 2s 内 Animator 始终停留在 ` Locomotion`。只读原因链：**玩家侧没有任何生产代码对 Animator 设置 `Hit` 触发器**——`Hit` 仅由 `MonsterBrain`（怪物受击）与 `DownedRescue`（救援者被打断）使用；`UnitHealth.Flash` 只做材质闪红。因此玩家受击没有动画表现。按工单只记录、不修复。
- **补充观察（不计入五类判定）**：目标死亡、出招停止后，动画停在 `Attack_Draw` 至少 7.6s 未回 `Locomotion`。原因是上述链路每一步都需要新的 `Attack` 触发器，且离开 `Attack_Draw`/`Attack_Release` 没有“仅退出时间”的转换；出招中途停止后没有触发器收尾。
- **夹具边界**：Hit/Downed 使用 `Knight_Unit` 内存实例（禁用 brain、不改资产）；受击伤害仅作动画刺激，不计入第 4 项；致命伤害只作用于该夹具，未制造团灭（`IsWiped=false`、`timeScale=1`）。

---

## 7. 人工目检

- V06-1 状态：**待目检**，见 `Docs/QA/Evidence/FND-00B-06/06_人工目检.md`；执行者未填写任何画面结论。

---

## 8. 分类区分

| 类别 | 内容 |
|---|---|
| 产品断言失败 | `Animation.Hit`（受击状态未进入）；补充观察 `Animation.PostCombatState`（战后不回待机，不计入五类判定） |
| 前置阻塞 | 无 |
| 测试基础设施失败 | 无（首轮试跑的进程中断与陈旧锁已披露；不计为产品结果，试跑数据另存 `*_preliminary_run*`） |
| 夹具 / 观测参考 | 敌人目标夹具 `Skeleton_Warrior`、动画夹具 `Knight_Unit`；`Animation.ConfigMapping` 为只读登记 |

---

## 9. 实现原理（面向项目所有者）

本单不修任何产品逻辑，只把“普攻能不能真的打、伤害按不按攻速结算、角色动作是否跟着逻辑走”变成可重复的自动断言。

攻击侧：命令入口是 `SelectionManager.HandleClick` 内的 `SelectEnemy + CharacterBrain.CommandAttack`；batchmode 无法产生鼠标射线，因此直接调用这两个生产方法（等价输入），之后全部交给 `CharacterBrain.Update → UnitBrain.AttackLoop → SwordSwing` 真实追击、出招、`UnitHealth.TakeHit` 结算。为避免场景中另两名角色的“坚守自动攻击”误伤夹具目标，测试在运行时临时关闭其大脑并单列非本玩家命中数，从而把“普攻命中次数”和“别人打的”彻底分开。

动画侧：直接采样 Animator 层 0 的状态哈希与参数，不调用 `Animator.Play`，用状态时间轴对照伤害时间。由此定位到三件事：① `SwordSwing` 配的 `attackLayer=1`/`attackStateName="Attack"` 与只有 1 层的 `PlayerBase` 不符，命中窗口一直走代码时钟；② 玩家侧没有任何代码设置 `Hit` 触发器，所以受击只有闪红没有动作；③ 攻击状态链靠连续 `Attack` 触发器推进，出招中途停止（如击杀目标）后动画会卡在攻击状态。三项均只记录、未修复。

---

## 10. 受影响模块导航说明

新增 `Tests/FND00B/` 启动器与探针；只读链路 `SelectionManager→CharacterBrain→UnitBrain→SwordSwing→UnitHealth`；动画 `OC_Knight/PlayerBase`；倒地 `DownedRescue`；产物 `06_Assertions.jsonl`→`Summary/ExitCode`→QA 与报告。

---

## 11. 变更文件清单

新增（测试资产，含 Unity 自动生成的 `.meta`）：
- `Assets/_Project/Tests/FND00B/Editor/FND00B06_BatchAcceptance.cs` / `.cs.meta`
- `Assets/_Project/Tests/FND00B/Runtime/FND00B06_ProbeRunner.cs` / `.cs.meta`
- `Assets/_Project/Tests/FND00B/Runtime/FND00B06_Evidence.cs` / `.cs.meta`

新增（证据与报告）：
- `Docs/QA/Evidence/FND-00B-06/00_前置确认.txt`
- `Docs/QA/Evidence/FND-00B-06/06_Assertions.jsonl`
- `Docs/QA/Evidence/FND-00B-06/06_Automation_run1.log`、`06_Automation_run2.log`
- `Docs/QA/Evidence/FND-00B-06/06_Summary_run1.json`、`06_Summary_run2.json`
- `Docs/QA/Evidence/FND-00B-06/06_ExitCode_run1.txt`、`06_ExitCode_run2.txt`
- `Docs/QA/Evidence/FND-00B-06/06_人工目检.md`（内容仅“待目检”）
- `Docs/QA/Evidence/FND-00B-06/06_Automation_failed_attempt1_compile.log`、`06_Automation_preliminary_run1.log`、`06_Automation_preliminary_run2.log`、`06_Assertions_preliminary_run1.jsonl`、`06_Assertions_preliminary_run2.jsonl`、`06_Summary_preliminary_run1.json`、`06_Summary_preliminary_run2.json`、`06_ExitCode_preliminary_run1.txt`、`06_ExitCode_preliminary_run2.txt`
- `Docs/Handoff/REPORT_FND-00B-06_普攻与动画补验.md`（本文件）

修改：
- `Docs/QA/FND-00B_现有行为验收.md`（顶部状态与执行人、第 4 项、第 6 项、汇总第 4/6 行、11 项统计、收口说明；其余项与第 10 项目标登记未动）
- `Assembly-CSharp.csproj`、`Assembly-CSharp-Editor.csproj`（Unity 自动重生成，各新增 Compile 行）

未改动：`Assets/_Project/Scripts/**`、`Assets/Scenes/DungeonWorld.unity`、`Assets/_Project/Prefabs/**`、`Assets/_Project/Art/Animations/**`、`ProjectSettings/**`、`Packages/**`、`Docs/Handoff/CURRENT.md`、既有 04/05 测试与证据。

---

## 12. 未覆盖项与遗留问题

1. `Animation.Hit` 失败：玩家受击无 `Hit` 触发器；是否要补玩家受击动作属产品决策，本单不修。
2. 攻击状态链不可自收尾：击杀/停止攻击后停在 `Attack_Draw`（`Animation.PostCombatState`）；与 ④ 同一控制器配置问题。
3. `SwordSwing` 动画映射与控制器不一致（`attackLayer=1`、状态名 `Attack`）：当前靠代码时钟兜底，命中窗口与动画并非同一时基。
4. 五类状态之外的 `Skill`/`Bash`/`Jump`/`Interact` 未覆盖（工单限定五类）。
5. 场景仍为“怪物 0、生成器 0”，普攻与动画结论均来自内存夹具，不代表真实遭遇链路；该差距未消除。
6. V06-1 画面核对待用户完成；未填写前不得视为通过。
7. 首次批处理被外部提前终止、首轮试跑发现其他玩家自动攻击污染，均已保留原始产物，未删除、未挑选通过的一次。

---

## 13. 建议提交

建议拆成两笔（测试代码/资产 与 文档分离）：

**提交 1（测试资产）**
```
git add "Assets/_Project/Tests/FND00B/Editor/FND00B06_BatchAcceptance.cs" "Assets/_Project/Tests/FND00B/Editor/FND00B06_BatchAcceptance.cs.meta" "Assets/_Project/Tests/FND00B/Runtime/FND00B06_ProbeRunner.cs" "Assets/_Project/Tests/FND00B/Runtime/FND00B06_ProbeRunner.cs.meta" "Assets/_Project/Tests/FND00B/Runtime/FND00B06_Evidence.cs" "Assets/_Project/Tests/FND00B/Runtime/FND00B06_Evidence.cs.meta" "Assembly-CSharp.csproj" "Assembly-CSharp-Editor.csproj"
```
```
test(FND-00B-06): 新增普攻与动画 batchmode 验收脚本

为什么：第 4/6 项缺少可重复的自动证据，用生产选择/命令入口与真实 Animator 采样替代口述观察。
```

**提交 2（证据与文档）**
```
git add "Docs/QA/Evidence/FND-00B-06" "Docs/QA/FND-00B_现有行为验收.md" "Docs/Handoff/REPORT_FND-00B-06_普攻与动画补验.md" "Docs/Handoff/TASK_FND-00B-06_普攻与动画补验.md"
```
```
docs(FND-00B-06): 回填第 4 项通过、第 6 项失败与证据

为什么：补验结果需进入 QA 与复审材料，供【大脑】判断 FND-00B 是否收口。
```

> 以上命令仅供用户执行；执行者未做任何 Git 写操作。`Docs/Handoff/TASK_FND-00B-06_普攻与动画补验.md` 未修改，如不需要随本次提交可自行移除该路径。
