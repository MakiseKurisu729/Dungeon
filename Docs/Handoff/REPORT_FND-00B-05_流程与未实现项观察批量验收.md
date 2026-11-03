# REPORT_FND-00B-05 — 流程与未实现项观察批量验收

> 工单：`Docs/Handoff/TASK_FND-00B-05_流程与未实现项观察批量验收.md`
> 执行角色：执行者（OpenCode + DeepSeek V4.1 Flash）
> 执行日期：2026-11-03
> 结论：**测试基础设施可用，同一入口连续 2 次运行、11/11 断言的 `pass`/`status` 完全一致。第 11 项场景无传送入口（`OBSERVED_NO_ENTRY`，保持 `未测试`）；第 12 项全员倒地可建立，但产品路径未触发游戏结束（`失败`）。怪物初始/新增均为 0，原因未确认、不作结论。唯一人工目检 V05-1 待用户执行。** 本单只记录，不修复。

## 1. 前置、提交与场景

| 项 | 值 |
|---|---|
| 分支 | `docs/fnd-00-baseline` |
| 完整 HEAD | `739f5be705643ee74f4d2221265a518d56f2f57e` |
| 前置工作区 | 干净（`git status --porcelain` 无输出） |
| Unity | `2022.3.62f3c1`（`ProjectSettings/ProjectVersion.txt`；可执行文件 `D:\unity\2022.3.62f3c1\Editor\Unity.exe`） |
| 目标场景 | `Assets/Scenes/DungeonWorld.unity` |
| 前置关停确认 | 记录时检测到 Unity 编辑器 PID 46544；执行者请求关闭并等待，用户明确回复“已关闭，可以开始”；随后复核无 Unity 进程，才写入测试资产并运行 |
| 前置证据 | `Docs/QA/Evidence/FND-00B-05/00_前置确认.txt` |

## 2. 测试资产、入口与完整命令

### 2.1 唯一主入口

`FND00B05_BatchAcceptance.Run()`（batchmode `-executeMethod`）。未使用 UTF，未安装任何包，未新增 asmdef。

### 2.2 新增测试脚本与 `.meta`

| 文件 | 作用 | `.meta` GUID |
|---|---|---|
| `Assets/_Project/Tests/FND00B/Editor/FND00B05_BatchAcceptance.cs` | 编辑器主入口 + 域重载接续（进入 Play / 退出码） | `e5a1c7d2f3b94c6a8d0e1f2b3c4d5e6f` |
| `Assets/_Project/Tests/FND00B/Runtime/FND00B05_ProbeRunner.cs` | Play 模式探针：11→12，夹具与断言 | `a1b2c3d4e5f6470891a2b3c4d5e6f708` |
| `Assets/_Project/Tests/FND00B/Runtime/FND00B05_Evidence.cs` | 断言 JSONL / 汇总 / 退出码写入 | `b2c3d4e5f6a74819a2b3c4d5e6f70819` |

`Editor/` 只放批处理入口；`Runtime/` 的 `MonoBehaviour` 以 `#if UNITY_EDITOR` 包裹编辑器 API。未新增目录，无新目录 `.meta`。

### 2.3 完整命令（两次相同，仅日志文件名与运行序号不同）

```powershell
$env:FND00B05_RUN="1"   # 第二次为 "2"
& "D:\unity\2022.3.62f3c1\Editor\Unity.exe" `
  -batchmode `
  -projectPath "D:\unity\Dungeon" `
  -executeMethod FND00B05_BatchAcceptance.Run `
  -logFile "D:\unity\Dungeon\Docs\QA\Evidence\FND-00B-05\05_Automation_run1.log"
```

实际调度使用 `Start-Process -Wait`，入口与参数完全相同；退出码由探针写入 `05_ExitCode_runN.txt`。

### 2.4 运行机制

打开场景（只读，不保存）→ `EditorApplication.EnterPlaymode()` → 域重载后由 `[InitializeOnLoadMethod]` + `playModeStateChanged` + 编辑器 update 三重兜底创建探针 → 依次执行第 11 → 12 项 → 逐行追加 JSONL → 写 `05_Summary_runN.json` / `05_ExitCode_runN.txt` → 退出 Play 并按记录退出码结束进程。

可重复性：每次运行重新加载场景；夹具名称统一前缀 `FND00B05_`，结束后销毁/恢复；随机种子 `20261103 + run`（run1=20261104、run2=20261105）由 `UnityEngine.Random.InitState` 设置并记录；断言不依赖随机数结果。

## 3. 两次运行结果

| 项 | run=1 | run=2 |
|---|---|---|
| 开始时间 | 2026-11-03 22:35:28.938 | 2026-11-03 22:36:52.334 |
| 结束时间 | 2026-11-03 22:36:17.584 | 2026-11-03 22:37:40.976 |
| 断言行数 | 11 | 11 |
| Pass（产品通过） | 1 | 1 |
| Fail（产品断言失败） | 2 | 2 |
| Blocked（前置失败/阻塞） | 2 | 2 |
| Infra（测试基础设施失败） | 0 | 0 |
| Observed（观察完成，不判通过/失败） | 4 | 4 |
| 夹具参考探针 | 2 pass / 0 fail | 2 pass / 0 fail |
| 退出码 | 2 | 2 |
| 稳定性 | — | **11/11 断言的 `assertion`/`pass`/`status` 完全一致，无不稳定项** |

两次差异仅为位置、实例顺序、耗时与随机种子（如传送夹具中玩家起始位置不同，移动距离 16.43～22.44m / 18.89～22.37m），不影响任何判定。结构化数据：`Docs/QA/Evidence/FND-00B-05/05_Assertions.jsonl`（run1 11 行 + run2 11 行，共 22 行）。

## 4. 逐子项断言表

> `status`：`pass` 产品通过｜`product_fail` 产品断言失败｜`blocked` 前置失败/阻塞｜`observed` / `observed_no_entry` / `observed_unconfirmed` 观察完成但不判通过/失败｜`fixture_pass` 夹具参考。`observed*` 与 `blocked` 均**未写成通过**。

### 4.1 第 11 项：传送

| 断言 ID | 预期 | 实际（run1 / run2） | 结果 |
|---|---|---|---|
| `Teleport.EntryCensus` | 枚举场景内可辨认的传送入口/组件（含 inactive），输出数量、路径、激活与交互状态 | `entryCount=0` / `0`；`dungeonDoorInstances=0` / `0`；`transitionFaderInstances=0` / `0`；已加载类型清单含 `DungeonDoor @ Assembly-CSharp`、`TransitionFader @ Assembly-CSharp`（类型存在，不等于场景集成） | `observed_no_entry` |
| `Teleport.TargetData` | 存在入口时输出源点、配置目标坐标、目标场景/对象标识及有效性 | `blocked`：`OBSERVED_NO_ENTRY`，场景无入口实例，无源点/目标配置可读 | `blocked` |
| `Teleport.ReachesTarget` | 通过实际交互入口触发，断言角色到达配置目标容差内 | `blocked`：`OBSERVED_NO_ENTRY`，无生产入口可触发 | `blocked` |
| `Teleport.ReachesTarget.fixtureProbe` | 夹具内调用生产 `DungeonDoor.Enter()`，玩家落在 `dungeonEntryPoint +` 生产偏移（容差 0.5m），过场完成 | 3/3 玩家落点误差 `0.000m`；目标 `(20.85, 0.08333, 0)`；移动 16.43～22.44m / 18.89～22.37m；`faderCompleted=true`；相机焦点已设置；`cameraRigMoved=false`（见备注） | `fixture_pass`（**不作为第 11 项通过依据**） |

> 夹具说明：`Input.GetKeyDown(KeyCode.F)` 按键门控未模拟；`cameraRigMoved=false` 是因为 `DebugCameraSwitcher` 在过场结束后的帧里按焦点重算相机位置，不代表 `cameraPoint` 接线失败，仅记录。夹具证明 `Enter()` 的落点与过场机制可用，不证明场景已集成。

### 4.2 第 12 项：团灭与游戏结束（含怪物遭遇前置数据）

| 断言 ID | 预期 | 实际（run1 / run2） | 结果 |
|---|---|---|---|
| `Monster.Population` | 团灭前输出初始怪物数、20s 窗口新增、怪物 ID/位置、生成器数量/状态 | 初始 `0` / `0`；新增 `0` / `0`；最终 `0` / `0`；生成器 `0` / `0`；窗口 20.00017s / 20.00043s | `observed`（数据采集完成，**不代表怪物功能通过**） |
| `Monster.NavMeshState` | 逐只输出 `NavMeshAgent` enabled / `isOnNavMesh` / `pathPending` / `hasPath` / 位置与窗口位移；数据不足标未确认 | 怪物 `0` → `未确认`；参照 3 名玩家 `agentEnabled=true`、`isOnNavMesh=true`、`pathPending=false`、`hasPath=false`；NavMesh 三角顶点 2558 | `observed_unconfirmed` |
| `Monster.EncounterProbe` | 固定路线/窗口记录遭遇事件数、最近距离、怪物位移，断言数据完整采集 | 遭遇事件 `0` / `0`；最近距离 `null` / `null`；位移记录 `0` / `0`；路线采样 6、到达 4；移动 13.60627m / 13.60537m；窗口 18.00016s / 18.00023s；探针演员 `Archer_Unit` | `observed` |
| `Party.AllDown` | 通过真实伤害/倒地入口建立全员倒地，逐角色输出 ID/倒地时间/状态/存活计数 | `playerCount=3`、`allDown=true`；存活计数 `3→0` / `3→0`；3 人 `hp=100→-10`、`dead=true`、`isDown=true`、brain/agent 禁用；`DownedRescue.AllDowned=3` | `pass` |
| `GameEnd.TriggerChain` | 全员倒地 → `aliveCount=0` → 游戏结束状态 → UI，限定时间内触发且仅一次 | **未触发**：等待 8.00029s / 8.00012s，`risingEdges=0`、`IsWiped=false`、`timeScale=1`、`GameStarted=false`、HUD 文本为空；`aliveStanding=0` | `product_fail` |
| `GameEnd.FinalState` | 输出最终游戏状态、相关 UI active 状态与输入锁定状态 | `IsWiped=false`、`timeScale=1`、`paused=false`；`UIManager` 五个面板槽位全部未接线；无 `GameOver/GameEnd/Wipe/Defeat` 类对象；HUD 激活但 `messageText` 为空；输入未锁定 | `product_fail` |
| `GameEnd.TriggerChain.fixtureControl` | 夹具内仅补 `GameStarted=true`，验证团灭链在限定时间内触发且一次 | 触发 true，0.00183s / 0.00164s；`risingEdges=1`、`IsWiped=true`、`timeScale=0`；消息与 HUD 文本均为“全队覆没！按 R 重来”；2s 内无二次触发 | `fixture_pass`（**不作为第 12 项通过依据**） |

**两类结果分开标注：**
- **遭遇探针结果**：场景怪物 0、生成器 0、20s 新增 0、18s 遭遇 0、最近距离 `null`。只记录数据，**不把“未生成或怪物无法移动”任一种可能写成原因结论**。
- **团灭链结果**：全员倒地可建立；产品路径未触发团灭（`失败`）。夹具控制证明 `GameStarted=true` 时链本身可触发，但该前置只能由场景中缺席的 `MainMenuPanel` 写入，产品路径是否可达由 `GameEnd.TriggerChain` 单独记录，二者不相互掩盖。

## 5. 结构化数据与原始日志路径

| 内容 | 路径 |
|---|---|
| 结构化断言（主数据） | `Docs/QA/Evidence/FND-00B-05/05_Assertions.jsonl`（22 行，273,755 字节） |
| run1 汇总 / 退出码 | `Docs/QA/Evidence/FND-00B-05/05_Summary_run1.json` / `05_ExitCode_run1.txt`（2） |
| run2 汇总 / 退出码 | `Docs/QA/Evidence/FND-00B-05/05_Summary_run2.json` / `05_ExitCode_run2.txt`（2） |
| **run1 原始日志（完整）** | `D:\unity\Dungeon\Docs\QA\Evidence\FND-00B-05\05_Automation_run1.log`（279,246 字节；含 11 条 `[FND00B05][ASSERT]`、`teleport fixture cleaned`、`finish; exit=2`、`batchmode exit 2`） |
| **run2 原始日志（完整）** | `D:\unity\Dungeon\Docs\QA\Evidence\FND-00B-05\05_Automation_run2.log`（275,855 字节；含 11 条 `[FND00B05][ASSERT]`、`finish; exit=2`、`batchmode exit 2`） |
| 前置确认 | `Docs/QA/Evidence/FND-00B-05/00_前置确认.txt` |
| 人工目检 | `Docs/QA/Evidence/FND-00B-05/05_人工目检.md`（V05-1，**待用户执行**） |
| 预运行留档（测试侧修复前） | `05_Assertions_preliminary_run1.jsonl`、`05_Automation_preliminary_run1.log`、`05_Summary_preliminary_run1.json`、`05_ExitCode_preliminary_run1.txt` |
| 编译失败留档 | `05_Automation_failed_attempt1_compile.log` |

原始日志未经筛选或改写，保留 Unity 完整启动、编译、导入、Play 进入、`Debug.Log` 与退出内存统计。

## 6. 四类结果必须分开看

### 6.1 测试基础设施失败（不属于产品结论）

| 尝试 | 现象 | 处理 |
|---|---|---|
| 第 1 次启动 | 新探针在 `try/catch` 内 `yield return`，编译错误 `CS1626`，Unity 以退出码 1 结束 | 日志保留为 `05_Automation_failed_attempt1_compile.log`；把等待循环移出 `try` 后重跑 |
| 预运行（成功但被取代） | 传送夹具的过场结束判定存在一帧竞态（`blocksRaycasts` 尚未复位），导致 `fixture_fail` | 保留 `05_Assertions_preliminary_run1.jsonl` 等；改为只按 `alpha<=0.01` 判定后，正式 run1/run2 均 `fixture_pass` |

正式结论只取第 3 节的 run1/run2（同一份最终代码、同一入口）。

### 6.2 产品断言失败

- `GameEnd.TriggerChain`：全员倒地后等待 8s，`risingEdges=0`、`IsWiped=false`、`timeScale=1`，未触发游戏结束。
- `GameEnd.FinalState`：`IsWiped=false`、无结束 UI、HUD 文本为空、输入未锁定。
- 运行期观察到的相关状态：`GameStarted=false`；`UIManager.mainMenuPanel` 槽位未接线；场景无 `MainMenuPanel` 实例。本单只记录状态，不做根因结论、不修复。

### 6.3 观察完成但目标未明确 / 数据不足

- 第 11 项：`OBSERVED_NO_ENTRY`（`count=0`），目标设计未明确，无法形成通过/失败判定；QA 保持 `未测试`。已输出类型清单，避免被误读为“代码不存在”。
- `Monster.NavMeshState`：怪物 0，逐只数据无法采集，标 `未确认`；不把“未生成或无法移动”写成原因。
- `Monster.EncounterProbe`：数据采集完成（事件 0、最近距离 `null`、位移记录 0），不据此推断怪物功能。
- 探针路径覆盖有限：18s 内 6 个采样路点中 4 个槽位到时；移动 13.61m。该数据仅为遭遇探针服务，**不构成第 2 项“移动”结论**。

### 6.4 人工目检差距

- V05-1 尚未执行：`05_人工目检.md` 已单列（仅此 1 项），等待用户在 Unity 中 Play 后记录“团灭后游戏结束 UI 是否真实可见；未触发则原样记录未见”。本报告不以目检替代任何自动断言。

## 7. 人工目检清单（仅 1 项）

| 项 | 人工核对内容 | 记录 |
|---|---|---|
| V05-1 游戏结束画面 | 自动断言之外，团灭后游戏结束 UI 是否真实可见；未触发则原样记录未见 | `Docs/QA/Evidence/FND-00B-05/05_人工目检.md`（**待执行**） |

## 8. 实现原理（面向项目所有者）

项目没有 asmdef，UTF 程序集无法引用 `Assembly-CSharp`；运行期 `MonoBehaviour` 也不能放在 `Editor` 目录。因此沿用 FND-00B-04 的 `-executeMethod` 路线：打开真实场景（只读）进入 Play，域重载后由三重兜底创建探针。探针先做运行时实例/类型枚举，无传送入口即输出 `OBSERVED_NO_ENTRY`；再用内存 `DungeonDoor`+`TransitionFader` 夹具调用生产 `Enter()` 验证落点。团灭用真实伤害入口 `ApplyDamage` 令 3 人倒地，等待 8s 观察 `GameFlow` 链，并另用 `GameStarted=true` 夹具隔离链本身。所有断言逐行写 JSONL，同一入口两次复跑，便于回归。

## 9. 变更文件

### 9.1 新增测试资产

- `Assets/_Project/Tests/FND00B/Editor/FND00B05_BatchAcceptance.cs` + `.meta`
- `Assets/_Project/Tests/FND00B/Runtime/FND00B05_ProbeRunner.cs` + `.meta`
- `Assets/_Project/Tests/FND00B/Runtime/FND00B05_Evidence.cs` + `.meta`
- 未新增目录；`Assets/_Project/Tests/FND00B/Editor` 与 `Runtime` 目录 `.meta` 沿用已有资产。

### 9.2 新增文档/证据

- `Docs/QA/Evidence/FND-00B-05/00_前置确认.txt`
- `Docs/QA/Evidence/FND-00B-05/05_Assertions.jsonl`
- `Docs/QA/Evidence/FND-00B-05/05_Automation_run1.log`
- `Docs/QA/Evidence/FND-00B-05/05_Automation_run2.log`
- `Docs/QA/Evidence/FND-00B-05/05_Summary_run1.json`
- `Docs/QA/Evidence/FND-00B-05/05_Summary_run2.json`
- `Docs/QA/Evidence/FND-00B-05/05_ExitCode_run1.txt`
- `Docs/QA/Evidence/FND-00B-05/05_ExitCode_run2.txt`
- `Docs/QA/Evidence/FND-00B-05/05_人工目检.md`
- 留档：`05_Automation_failed_attempt1_compile.log`、`05_Assertions_preliminary_run1.jsonl`、`05_Automation_preliminary_run1.log`、`05_Summary_preliminary_run1.json`、`05_ExitCode_preliminary_run1.txt`
- `Docs/Handoff/REPORT_FND-00B-05_流程与未实现项观察批量验收.md`（本文件）

### 9.3 修改文档

- `Docs/QA/FND-00B_现有行为验收.md`：顶部当前状态与执行人；第 11、12 项；汇总第 11、12 行；11 项统计与末期说明。

### 9.4 由 Unity 自动重写的跟踪文件（需用户决定）

- `Assembly-CSharp.csproj`（+2 行 `<Compile Include=...>`）
- `Assembly-CSharp-Editor.csproj`（+1 行 `<Compile Include=...>`）

原因：Unity 导入新增脚本时自动重新生成工程文件；内容仅为把 3 个测试脚本加入编译列表。**不是生产代码**，但属于受跟踪文件的工作区改动，故单独列出交用户决定提交或还原。执行者未执行任何 Git 写操作（只读执行过 `git status` / `rev-parse` / `diff --stat`）。

### 9.5 未修改确认

- 未修改生产代码、场景、预制体、Animator、`ProjectSettings`、`Packages`、导航数据；未保存场景；未重新烘焙。
- 未安装新包；未升级 Unity；未执行构建。
- 未修改 `CURRENT.md`。
- 自动化夹具全部在内存中创建并销毁；玩家位置在探针结束后恢复，未写回场景。

## 10. 遗留问题

1. **传送（第 11 项）**：场景无 `DungeonDoor`/`TransitionFader` 实例，目标设计未明确，保持 `未测试`；夹具探针只证明 `Enter()` 落点与过场机制可用。是否集成场景、目标点如何配置需后续另立任务。
2. **游戏结束（第 12 项）**：全员倒地可建立，但产品路径 8s 内未触发团灭；运行期观察到 `GameStarted=false`、`UIManager.mainMenuPanel` 未接线、场景无 `MainMenuPanel`。根因分析与修复不在本单范围。
3. **怪物遭遇**：初始 0、20s 新增 0、生成器 0、遭遇 0；原因未确认，“未生成或无法移动”均未证实，后续复验前不得写成原因结论。
4. **V05-1 人工目检待用户执行**（唯一目检项），结果需回填 `05_人工目检.md`。
5. **探针路径覆盖有限**：18s 内 4 个路点槽位到时、移动 13.61m；仅影响遭遇探针覆盖，不代表第 2 项移动结论。
6. **夹具边界已披露**：传送夹具未模拟 F 键门控；团灭夹具仅补 `GameStarted=true`，均不作为产品通过依据。
7. `Assembly-CSharp*.csproj` 两处自动生成改动待用户处理。
8. 第 10 项（救援目标交互）仍为目标差距登记，本单未执行、未断言。

## 11. 建议提交

```powershell
git add Assets/_Project/Tests/FND00B/Editor/FND00B05_BatchAcceptance.cs `
        Assets/_Project/Tests/FND00B/Editor/FND00B05_BatchAcceptance.cs.meta `
        Assets/_Project/Tests/FND00B/Runtime/FND00B05_ProbeRunner.cs `
        Assets/_Project/Tests/FND00B/Runtime/FND00B05_ProbeRunner.cs.meta `
        Assets/_Project/Tests/FND00B/Runtime/FND00B05_Evidence.cs `
        Assets/_Project/Tests/FND00B/Runtime/FND00B05_Evidence.cs.meta `
        Docs/QA/Evidence/FND-00B-05 `
        "Docs/QA/FND-00B_现有行为验收.md" `
        "Docs/Handoff/REPORT_FND-00B-05_流程与未实现项观察批量验收.md" `
        Assembly-CSharp.csproj `
        Assembly-CSharp-Editor.csproj
```

建议提交信息：

```
test: FND-00B-05 流程与未实现项自动验收（传送无入口、团灭链未触发）
```

> 是否包含两个 `.csproj` 由用户决定（Unity 自动生成）；若不需要，可从 `git add` 清单中移除并还原。执行者未自行提交、未推送。
