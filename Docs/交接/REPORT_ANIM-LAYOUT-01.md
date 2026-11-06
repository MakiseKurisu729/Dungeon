# REPORT_ANIM-LAYOUT-01 — 动画机节点布局整理

- 任务 ID：`ANIM-LAYOUT-01`（无独立 TASK 文件，以派单正文为工单）
- 分支 / HEAD：`docs/fnd-00-baseline` / `9e0b7515`
- Unity：2022.3.62f3c1，模式 B（MCP 常开编辑器，实例 `Dungeon@4f5c34c82f31646e`）
- 风险等级：高风险（Animator Controller 资产），但改动面被工单限定为**节点坐标**；不涉及场景、预制体、`.asset`、`ProjectSettings`、GUID

## 1. 结果摘要

`Assets/_Project/Art/Animations/PlayerBase.controller` 的 10 个平级状态、4 个子状态机、4 个子机内状态，共 18 个节点坐标已按工单表逐项写入并双次读回核对，`mismatch=0`。资产已 `SaveAssets`（`dirty=0`）。

- 读回：18/18 与目标表一致（`positions.txt`）。
- `git diff`：`19 insertions(+), 19 deletions(-)`，全部落在 `m_Position` / `m_AnyStatePosition`（`diff_guard.txt`）。
- 守卫 grep：`m_Conditions` / `m_TransitionDuration` / `m_SpeedParameter` / `m_Name` 命中 **0**；非 position 行 **0**。
- `error CS`：0 条。SPEC01 复跑 `pass=12 fail=0 exit=0`。
- 未改任何状态名、过渡、条件、参数、时长、clip、GUID/fileID。

## 2. 证据锚点

| 证据 | 路径 |
|---|---|
| 坐标读回（before / apply 内校验 / 独立二次读回） | `Docs/QA/Evidence/ANIM-LAYOUT-01/positions.txt` |
| diff 守卫（机械分类 + 4 个守卫 grep） | `Docs/QA/Evidence/ANIM-LAYOUT-01/diff_guard.txt` |
| 控制器完整原始 diff（112 行） | `Docs/QA/Evidence/ANIM-LAYOUT-01/controller_diff_raw.txt` |
| 控制台（error CS=0 + SPEC01 复跑终止行） | `Docs/QA/Evidence/ANIM-LAYOUT-01/console.txt` |
| SPEC01 汇总（原样复制自 `SPEC-01/editor_run10`） | `Docs/QA/Evidence/ANIM-LAYOUT-01/spec01_summary.json` |
| SPEC01 完整原始证据 | `Docs/QA/Evidence/SPEC-01/editor_run10/`（canonical 目录，只追加） |

## 3. 改动清单

仅一个资产文件，仅坐标字段：

| 节点 | 原坐标 | 新坐标 |
|---|---|---|
| `Locomotion` | (310, 40) | (260, 220) |
| `AttackMelee` | (50, -30) | (620, 40) |
| `Attack_Draw` | (140, -180) | (620, 160) |
| `Attack_Release` | (460, -180) | (900, 160) |
| `Bash` | (150, 470) | (620, 280) |
| `Skill` | (420, 470) | (620, 400) |
| `Hit_A` | (490, 280) | (260, 380) |
| `Downed` | (640, 220) | (260, 500) |
| `Rise` | (610, 40) | (620, 520) |
| `Interact` | (40, 270) | (260, 620) |
| 子状态机 `Q` | (530, -90) | (1000, 300) |
| 子状态机 `W` | (560, 140) | (1200, 300) |
| 子状态机 `E` | (820, 140) | (1400, 300) |
| 子状态机 `R` | (1080, 140) | (1600, 300) |
| `Q_1` / `W_1` / `E_1` / `R_1`（各自子机内） | (320, 20) | (300, 40) |
| **Any State 节点**（非工单表项） | (660, 410) | (670, 460) |

实现方式：单次 `execute_code`，按**名字**（不按索引）匹配后写绝对坐标，随后 `EditorUtility.SetDirty(ac); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();`，并在同一段代码里重新 `LoadAssetAtPath` 逐项比对。写操作是幂等的：重复执行只会写入同一批字面量。

未新增/删除状态，未新增文件，无 `.meta` 变更，无 `.csproj` 变更。

## 4. 测试数据

| ID | Given | When | Then（断言） | 证据 | 结果 |
|---|---|---|---|---|---|
| AC-B1 | 控制器结构与基线一致（1 layer；根 10 states / 4 stateMachines；每个子机 1 个状态） | 执行布局代码 | 18 个节点坐标 = 工单表；`mismatch=0`；`dirty=0` | `positions.txt` READBACK 1 | PASS |
| AC-B2 | 资产已保存 | 另起一次 `execute_code` 重新 `LoadAssetAtPath` 读坐标 | 18/18 与目标表一致（证明落盘，非内存态） | `positions.txt` READBACK 2 | PASS |
| AC-B3 | 布局已保存 | `git diff --stat -- PlayerBase.controller` | 只有 position 行变化：`19 insertions / 19 deletions`，非 position 行 0 条 | `diff_guard.txt`、`controller_diff_raw.txt` | PASS |
| AC-B4 | 同上 | diff 上过滤 `m_Conditions`/`m_TransitionDuration`/`m_SpeedParameter`/`m_Name` | 命中 0 条 | `diff_guard.txt`（`guard_hits=0`） | PASS |
| AC-B5 | 布局已保存、编辑器空闲未编译 | `read_console` 搜 `error CS` | 0 条；`is_compiling=false`、`is_domain_reload_pending=false` | `console.txt` | PASS |
| AC-B6 | 同上 | `execute_menu_item("Tools/QA/In-Editor/SPEC01 (green)")` 再跑一次 | `pass=12 fail=0 exit=0`；JSONL `"pass":true`=12、`"pass":false`=0 | `spec01_summary.json`、`console.txt`、`SPEC-01/editor_run10/` | PASS（12/0/0） |
| AC-B7 | 布局已保存 | 复跑结束后再取一次 `git diff --stat` | 仍为 `19/19`，未被 Play 模式或重新保存改写 | 本文 §1 末尾复核（命令输出见 `diff_guard.txt` 头部） | PASS |

## 5. 实现原理

1. `Assets/_Project/Art/Animations/PlayerBase.controller:1253-1303` — 根状态机的 `m_ChildStates[*].m_Position` 与 `m_ChildStateMachines[*].m_Position` 即为画布坐标；Unity 序列化的是 `AnimatorStateMachine.ChildState.position`。必须走 `arr[i].position = ...; root.states = arr;`（struct 数组回写），否则改动丢失。
2. 同上 `:64-66`、`:367-369`、`:650-652`、`:719-721` — 四个子状态机内各自的 `m_ChildStates[0].m_Position`（`Q_1/W_1/E_1/R_1`），同样需要 `sub.states = inner` 回写。
3. `Assets/_Project/Art/Animations/PlayerBase.controller:1304-1312` — `m_AnyStatePosition`（Any State 节点坐标）被 Unity 自动挪动 660,410→670,460：`Skill` 新位置 (620,400) 与旧 Any State 节点重叠，Unity 的布局会把内置节点让开。属坐标字段，不影响任何逻辑。
4. `Assets/_Project/Tests/SPEC01` 的 `Locomotion.AllPlayersMoveParam` 断言直接读 `PlayerBase.controller` 的 state 名/混合树/阈值，本次复跑仍 `controllerPass=True`，说明布局改动未触碰状态拓扑。

## 6. 模块导航

- 控制器资产：`Assets/_Project/Art/Animations/PlayerBase.controller`（`PlayerBase`，单层 `Base Layer`）。
- 结构：根层 10 个状态（Locomotion / AttackMelee / Attack_Draw / Attack_Release / Bash / Skill / Hit_A / Downed / Rise / Interact）+ 4 个子状态机（Q/W/E/R，各含 `*_1` 单状态）。
- 消费方：角色 Animator（`CharacterBrain`/`UnitBrain` 侧通过参数与状态名驱动）；回归守卫 `Assets/_Project/Tests/SPEC01/`，编辑器入口 `Assets/_Project/Tests/QA/Editor/QA_InEditorMenu.cs`。

## 7. 未决项

1. **Any State 节点被 Unity 挪了位置**（660,410 → 670,460），不在工单表里但属 position 行，已在 `diff_guard.txt` 单列说明。若所有者希望 Any State 固定在原处，需要在 Animator 窗口手工拖动（本执行者未用 SerializedObject 强改，避免越界写字段）。
2. 工单未定义排版规则（例如过渡箭头交叉是否可接受）；当前按表执行，未做额外避让。若打开 Animator 后发现箭头密集交叉，需要在**新工单**里给新坐标表，本任务不再自行调整。
3. `controller_diff_raw.txt` 头部带一条 git 的 `LF will be replaced by CRLF` 提示（仓库既有行尾转换策略），不是内容变更。
4. 工作区存在与本次无关的既有改动：`Assembly-CSharp-Editor.csproj`、`Assembly-CSharp.csproj`（开工前即已 modified，未触碰）。
5. **需所有者处理（意外工作区改动，非本任务编辑）**：跑编辑器内 SPEC01 后 `ProjectSettings/ProjectSettings.asset` 被写成 `runInBackground: 0 → 1`。来源是既有测试入口 `Assets/_Project/Tests/QA/Editor/QA_InEditorMenu.cs:88` 的 `Application.runInBackground = true;`（注释称“不动 Player Settings”，实测会写盘）。我未用任何工具写 `ProjectSettings`，也未做 git 写操作。建议提交前 `git checkout -- ProjectSettings/ProjectSettings.asset`，并另立工单修正该测试入口。

## 8. 建议提交

标题：`整理：PlayerBase 控制器节点布局（ANIM-LAYOUT-01）`

正文：`仅改 18 个节点坐标（10 状态 / 4 子状态机 / 4 子机内状态），Any State 节点随 Skill 让位 660,410→670,460；无状态/过渡/参数/clip 变化，SPEC01 12/0 守卫通过。`
