# REPORT_FND-00B-04 — 场景交互与救援批量验收

> 工单：`Docs/Handoff/TASK_FND-00B-04_场景交互与救援批量验收.md`
> 执行角色：执行者（OpenCode + DeepSeek V4.1 Flash）
> 执行日期：2026-11-03
> 结论：**自动化测试可重复运行并已产出全部断言/日志；产品断言存在失败与阻塞（宝箱场景实例 0、迷雾遮罩实例 0）；救援现状行为通过；唯一人工目检 V04-1 待用户执行。** 本单只记录，不修复。

## 1. 前置、提交与场景

| 项 | 值 |
|---|---|
| 分支 | `docs/fnd-00-baseline` |
| 完整 HEAD | `e7688025e7a00fbeb1f7c9f36950ce4e353963a5` |
| 前置工作区 | 干净（`git status --porcelain` 无输出） |
| Unity | `2022.3.62f3c1`（`ProjectVersion.txt`；可执行文件 `D:\unity\2022.3.62f3c1\Editor\Unity.exe`） |
| 目标场景 | `Assets/Scenes/DungeonWorld.unity` |
| 编辑器关停确认 | 用户于 2026-11-03 明确回复“已关闭，可以开始”后才写入测试资产并运行 Unity |
| 前置证据 | `Docs/QA/Evidence/FND-00B-04/00_前置确认.txt`（含 Git 原始输出、环境、编辑器占用检查、静态核对与 21:58 更正） |

## 2. 测试资产、入口与完整命令

### 2.1 唯一主入口

`FND00B04_BatchAcceptance.Run()`（batchmode `-executeMethod`）。未使用 UTF，未安装任何包，未新增 asmdef。

### 2.2 新增测试脚本与 `.meta`

| 文件 | 作用 | `.meta` GUID |
|---|---|---|
| `Assets/_Project/Tests/FND00B/Editor/FND00B04_BatchAcceptance.cs` | 编辑器主入口 + 域重载接续（进入 Play / 退出码） | `16eb23feda8c45343bda3c6b11674b7e` |
| `Assets/_Project/Tests/FND00B/Runtime/FND00B04_ProbeRunner.cs` | Play 模式探针：7→8→9，夹具与断言 | `47c67e41c4a94a944a5415eb800da459` |
| `Assets/_Project/Tests/FND00B/Runtime/FND00B04_Evidence.cs` | 断言 JSONL / 汇总写入 | `d6fcd7471f40e6c42b52a1d4ecf2fa9c` |
| `Assets/_Project/Tests.meta` | 目录 `.meta` | `992c4ef4776610f43bb894007d05adeb` |
| `Assets/_Project/Tests/FND00B.meta` | 目录 `.meta` | `59296ac6f1d25ba42b0ad7a0235d5c2e` |
| `Assets/_Project/Tests/FND00B/Editor.meta` | 目录 `.meta` | `0cce5f9ac83111545b4310b63627c347` |
| `Assets/_Project/Tests/FND00B/Runtime.meta` | 目录 `.meta` | `4e86e85ffa95baa4ab9f0acf1c4fde20` |

目录分工说明：`Runtime/` 中的 `FND00B04_ProbeRunner` 是 `MonoBehaviour`，必须位于非 `Editor` 文件夹才能挂载；其编辑器 API 调用以 `#if UNITY_EDITOR` 包裹。`Editor/` 只放批处理入口。

### 2.3 完整命令（两次相同，仅日志文件名与运行序号不同）

```powershell
$env:FND00B04_RUN="1"   # 第二次为 "2"
& "D:\unity\2022.3.62f3c1\Editor\Unity.exe" `
  -batchmode `
  -projectPath "D:\unity\Dungeon" `
  -executeMethod FND00B04_BatchAcceptance.Run `
  -logFile "D:\unity\Dungeon\Docs\QA\Evidence\FND-00B-04\04_Automation_run1.log"
```

实际调度使用 `Start-Process`（不等待）并轮询进程退出，随后读取 `04_ExitCode_runN.txt`；入口与参数完全相同。

### 2.4 运行机制（可复用）

打开场景（不保存）→ `EditorApplication.EnterPlaymode()` → 域重载后由 `[InitializeOnLoadMethod]` + `playModeStateChanged` + `EditorApplication.update` 兜底创建探针 → 顺序执行第 7→8→9 项 → 断言逐行追加到 JSONL → 写 `04_Summary_runN.json` / `04_ExitCode_runN.txt` → 退出 Play 并按记录退出码结束进程。

测试数据可重复性：所有夹具为内存对象（名称前缀 `FND00B04_`），生产对象只读；每次运行重新加载场景。随机种子：测试代码未调用 `UnityEngine.Random`；场景 `ForestGenerator` 的序列化 `seed=42` 与本单断言无关；场景无 `SkeletonSpawner`，不存在随机刷怪影响。

## 3. 两次运行结果

| 项 | run=1 | run=2 |
|---|---|---|
| 开始时间 | 2026-11-03 21:58:58.328 | 2026-11-03 21:59:47.458 |
| 结束时间 | 2026-11-03 21:59:03.523 | 2026-11-03 21:59:52.665 |
| Pass | 4 | 4 |
| Fail（产品断言失败） | 2 | 2 |
| Blocked（前置失败/阻塞） | 3 | 3 |
| Infra（测试基础设施失败） | 0 | 0 |
| 夹具参考探针 | 1 pass / 0 fail | 1 pass / 0 fail |
| 退出码 | 2 | 2 |
| 稳定性 | — | **10/10 断言的 `pass` 与 `status` 完全一致，无不稳定项** |

两次仅“夹具参照单位/Actor 实例 ID/毫秒级耗时”不同（不影响判定）：`Fog.EnemyVisibility` 的参照玩家 run1 为 `Mage_Unit`、run2 为 `Archer_Unit`（均为场景内同一批玩家单位，位置不同但判定一致）；`Rescue` 的倒地目标两次均为 `Knight_Unit`。

结构化数据：`Docs/QA/Evidence/FND-00B-04/04_Assertions.jsonl`（run=1 共 10 行 + run=2 共 10 行）。

## 4. 逐子项断言表

> `status`：`pass` 通过｜`product_fail` 产品断言失败｜`blocked` 前置失败/阻塞。`blocked` 为 `pass=false`，未写成通过。

### 4.1 第 7 项：宝箱

| 断言 ID | 预期 | 实际（run1 / run2） | 结果 |
|---|---|---|---|
| `Chest.SceneCount` | 场景运行期 `Chest` 实例 ≥1 | `count=0` / `count=0`；实例列表为空；资产存在 `chest_base.prefab`、`chest_gold.prefab`（仅数据，不判通过） | `product_fail` |
| `Chest.Interactable` | 通过真实交互入口（`CharacterBrain.CommandInteract`→走到范围→`Chest.OpenBy`）触发并观察前后状态 | `blocked`：场景宝箱实例 count=0，无交互入口可触发 | `blocked` |
| `Chest.ProducesItem` | 开箱产出数量/背包状态符合现有预期 | `blocked`：无法开箱，产出不可断言 | `blocked` |

### 4.2 第 8 项：迷雾

| 断言 ID | 预期 | 实际（run1 / run2） | 结果 |
|---|---|---|---|
| `Fog.MaskState` | Play 中存在启用的 `FogOfWar`，`MeshRenderer` 有效 | `fogCount=0` / `0`；`fogLikeRendererCount=0` / `0`；无材质/网格可采样 | `product_fail` |
| `Fog.EnemyVisibility` | 视野外隐藏、进入 10m 视野后显示 | 30m：`IsHidden=true`、`renderer=false`；4m：`IsHidden=false`、`renderer=true`（两次一致） | `pass` |
| `Fog.VisionClearing` | 角色周围遮罩清除、范围外保持 | `blocked`：场景 FogOfWar 实例 count=0，无生产遮罩可采样 | `blocked` |
| `Fog.VisionClearing.fixtureProbe` | 夹具内算法：初始全迷雾→周围清除→移动后新位置清除且旧区域保持 | 初始 0.82/0.82；清除后近点 0.04921 / 0.04685，远点 0.82；移动后新点 0.04920 / 0.04966、旧点 0、远点 0.82 | `pass`（夹具参考，不计入第 8 项通过依据） |

### 4.3 第 9 项：救援（现状）

| 断言 ID | 预期 | 实际（run1 / run2） | 结果 |
|---|---|---|---|
| `Rescue.Fixture` | 1 个倒地角色 + 1 个可行动角色 | 倒地 1、可行动 1；`Knight_Unit` hp 100→-10，`dead=true`、`isDown=true`；救援者 `FND00B04_Rescuer` 为 Knight_Unit 内存实例，初始距离 6.31998m | `pass` |
| `Rescue.AutoTrigger` | 选中并进入 `rescueRange` 后无需点击目标自动开始 | 选中成功且 `Primary` 为救援者；`Warp` 至 1.2m；触发延迟 ≈0.00001s；`usedCommandInteract=false`、`usedTargetClickFlow=false` | `pass` |
| `Rescue.Completes` | 限定时间内完成，恢复符合 `reviveHp` | 2.99766s / 2.99689s 完成；`hp=1`、`dead=false`、`isDown=false`、`OnRevived=1`；事件链 `OnDeath→OnRevived`；brain/agent 恢复；`IsWiped=false`、`timeScale=1` | `pass` |

补充事实（运行期扫描）：场景 3 个玩家单位（`Knight_Unit`/`Archer_Unit`/`Mage_Unit`）均为 `Knight_Unit.prefab` 的**预制体变体**，运行时都带 `DownedRescue`、`Selectable`、`CharacterBrain`，team=Player；场景无 `SkeletonSpawner`，`sceneMonsterCount=0`（两次一致）。该事实已用于更正 `00_前置确认.txt` 第 5 节的静态推测。

## 5. 结构化数据与原始日志路径

| 内容 | 路径 |
|---|---|
| 结构化断言（主数据） | `Docs/QA/Evidence/FND-00B-04/04_Assertions.jsonl` |
| run1 汇总 | `Docs/QA/Evidence/FND-00B-04/04_Summary_run1.json` |
| run2 汇总 | `Docs/QA/Evidence/FND-00B-04/04_Summary_run2.json` |
| run1 退出码 | `Docs/QA/Evidence/FND-00B-04/04_ExitCode_run1.txt`（2） |
| run2 退出码 | `Docs/QA/Evidence/FND-00B-04/04_ExitCode_run2.txt`（2） |
| **run1 原始日志（完整）** | `D:\unity\Dungeon\Docs\QA\Evidence\FND-00B-04\04_Automation_run1.log`（147,520 字节；含 10 条 `[FND00B04][ASSERT]`、`finish`、`batchmode exit 2`） |
| **run2 原始日志（完整）** | `D:\unity\Dungeon\Docs\QA\Evidence\FND-00B-04\04_Automation_run2.log`（140,494 字节；含 10 条 `[FND00B04][ASSERT]`、`finish`、`batchmode exit 2`） |
| 前置确认 | `Docs/QA/Evidence/FND-00B-04/00_前置确认.txt` |
| 人工目检 | `Docs/QA/Evidence/FND-00B-04/04_人工目检.md`（V04-1，**待用户执行**） |

原始日志未经筛选或改写，包含 Unity 完整启动、编译、Play 进入、`Debug.Log` 与退出内存统计。

## 6. 三类结果必须分开看

### 6.1 测试基础设施失败（不属于产品结论）

| 尝试 | 现象 | 处理 |
|---|---|---|
| 第 1 次启动 | 入口已打开场景并请求进入 Play，但 `[InitializeOnLoadMethod]` 执行时 `Application.isPlaying` 仍为 false，探针未创建，进程停留在 Play 模式 | 人工终止；日志保留为 `04_Automation_failed_attempt1_hang.log`；改用 `playModeStateChanged` + 更新兜底 |
| 第 2 次启动 | `FND00B04_ProbeRunner` 位于 `Editor/` 文件夹，`AddComponent` 被 Unity 拒绝：`Can't add script behaviour ... because it is an editor script` | 日志保留为 `04_Automation_failed_attempt2_editor_script.log`；将运行期脚本移至 `Runtime/`（加 `#if UNITY_EDITOR` 守卫） |
| 预运行（成功但被取代） | 调整断言数据字段与夹具说明文字前的一次完整成功运行 | 保留为 `04_Assertions_preliminary_run1.jsonl`、`04_Automation_preliminary_run1.log`、`04_Summary_preliminary_run1.json`、`04_ExitCode_preliminary_run1.txt`；**不参与正式两次结论** |

正式结论只取第 3 节的 run1/run2（同一份最终代码、同一入口）。

### 6.2 产品断言失败 / 阻塞

- 第 7 项：`Chest.SceneCount=0`（失败）；交互与产出 `blocked`。代码/预制体存在不构成通过。
- 第 8 项：`Fog.MaskState` 失败（`fogCount=0`、无 fog 类 Renderer）；`Fog.VisionClearing` `blocked`。`Fog.EnemyVisibility` 通过；夹具算法探针通过（仅参考）。
- 第 9 项：三项全部通过。

### 6.3 人工目检差距

- V04-1 尚未执行：`04_人工目检.md` 已单列（仅此 1 项），自动数据背景已填入，等待用户在 Unity 中 Play 后记录“遮罩是否真实可见、角色周围清除是否与数据一致”。本报告不以其替代任何自动断言。

## 7. 人工目检清单（仅 1 项）

| 项 | 人工核对内容 | 记录 |
|---|---|---|
| V04-1 迷雾实际画面 | 自动化数据之外，遮罩是否真实可见、角色周围清除表现是否与数据一致；若未显示，原样记录 | `Docs/QA/Evidence/FND-00B-04/04_人工目检.md`（**待执行**） |

## 8. 实现原理（面向项目所有者）

项目没有 asmdef，UTF 测试程序集无法引用 `Assembly-CSharp`；`MonoBehaviour` 也不能放在 `Editor` 文件夹，且 batchmode 的域重载早于 `isPlaying` 变 true。因此本单使用 `-executeMethod` 打开真实场景并进入 Play，再由 `[InitializeOnLoadMethod]`、`playModeStateChanged` 与编辑器 update 三重兜底创建运行期探针；探针按 7→8→9 顺序执行，用真实交互/伤害/距离条件触发，用内存夹具隔离，逐条写 JSONL 并记录退出码。这样无需改工程结构或装包，同时保留完整可复跑日志。

## 9. 变更文件

### 9.1 新增测试资产

- `Assets/_Project/Tests/FND00B/Editor/FND00B04_BatchAcceptance.cs` + `.meta`
- `Assets/_Project/Tests/FND00B/Runtime/FND00B04_ProbeRunner.cs` + `.meta`
- `Assets/_Project/Tests/FND00B/Runtime/FND00B04_Evidence.cs` + `.meta`
- 目录 `.meta`：`Assets/_Project/Tests.meta`、`Assets/_Project/Tests/FND00B.meta`、`.../Editor.meta`、`.../Runtime.meta`

### 9.2 新增文档/证据

- `Docs/QA/Evidence/FND-00B-04/00_前置确认.txt`
- `Docs/QA/Evidence/FND-00B-04/04_Assertions.jsonl`
- `Docs/QA/Evidence/FND-00B-04/04_Automation_run1.log`
- `Docs/QA/Evidence/FND-00B-04/04_Automation_run2.log`
- `Docs/QA/Evidence/FND-00B-04/04_Summary_run1.json`
- `Docs/QA/Evidence/FND-00B-04/04_Summary_run2.json`
- `Docs/QA/Evidence/FND-00B-04/04_ExitCode_run1.txt`
- `Docs/QA/Evidence/FND-00B-04/04_ExitCode_run2.txt`
- `Docs/QA/Evidence/FND-00B-04/04_人工目检.md`
- 失败/预运行留档：`04_Automation_failed_attempt1_hang.log`、`04_Automation_failed_attempt2_editor_script.log`、`04_Assertions_preliminary_run1.jsonl`、`04_Automation_preliminary_run1.log`、`04_Summary_preliminary_run1.json`、`04_ExitCode_preliminary_run1.txt`
- `Docs/Handoff/REPORT_FND-00B-04_场景交互与救援批量验收.md`（本文件）

### 9.3 修改文档

- `Docs/QA/FND-00B_现有行为验收.md`：顶部当前状态；第 7、8、9 项；汇总第 7～9 行；11 项统计与末期说明。

### 9.4 由 Unity 自动重写的跟踪文件（需用户决定）

- `Assembly-CSharp.csproj`（+2 行 `<Compile Include=...>`）
- `Assembly-CSharp-Editor.csproj`（+1 行 `<Compile Include=...>`）

原因：Unity 导入新增脚本时自动重新生成工程文件；内容仅为把 3 个测试脚本加入编译列表。**不是生产代码**，但属于受跟踪文件的工作区改动，故单独列出交用户决定提交或还原。执行者未执行任何 Git 写操作。

### 9.5 未修改确认

- 未修改生产代码、场景、预制体、Animator、`ProjectSettings`、`Packages`、导航数据；未保存场景；未重新烘焙。
- 未安装新包；未升级 Unity；未执行构建。
- 未修改 `CURRENT.md`。
- `git status --porcelain -- Assets` 仅显示新增测试目录（`?? Assets/_Project/Tests.meta`、`?? Assets/_Project/Tests/`），无已跟踪生产资产改动。

## 10. 遗留问题

1. **宝箱**：场景无 `Chest` 实例（两次 `count=0`），交互与产出无法验证；需要后续（另立任务）把宝箱集成到场景后再复验。
2. **迷雾**：场景无 `FogOfWar` 实例、无 fog 类 `MeshRenderer`（两次一致），遮罩未显示的自动化数据已确认；视野清除表现无法由生产数据断言。原因未分析、未修复。
3. **人工目检 V04-1 待用户执行**（唯一目检项），结果需回填 `04_人工目检.md`。
4. 场景无 `SkeletonSpawner`，`sceneMonsterCount=0`；因此敌人隐藏仅能用生产 `Visibility` + 内存夹具验证，无真实怪物样本。
5. `Assembly-CSharp*.csproj` 两处自动生成改动待用户处理。
6. 第 10 项（救援目标交互）仍为目标差距登记，本单未执行、未断言。

## 11. 建议提交

```powershell
git add Assets/_Project/Tests.meta Assets/_Project/Tests Docs/QA/Evidence/FND-00B-04 Docs/QA/FND-00B_现有行为验收.md Docs/Handoff/REPORT_FND-00B-04_场景交互与救援批量验收.md Assembly-CSharp.csproj Assembly-CSharp-Editor.csproj
```

建议提交信息：

```
test: FND-00B-04 场景交互与救援自动验收（宝箱阻塞/迷雾遮罩缺失/救援现状通过）
```

> 是否包含两个 `.csproj` 由用户决定（Unity 自动生成）；若不需要，可从 `git add` 清单中移除并还原。执行者未自行提交、未推送。
