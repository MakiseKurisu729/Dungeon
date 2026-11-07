# REPORT — M7-FIX-01：Esc / 宝箱 / 门 最小修复

> 任务来源：所有者 2026-11-07 派单（M7-FIX-01）。
> 基线 `1c013dc4`；Unity `2022.3.62f3c1`；模式 B，实例 `Dungeon@4f5c34c82f31646e`。
> 本单只做最小 wiring 修复；未做 TEST-03、未重构测试架构、未重做 M7。

## 1. 结果摘要

**完成（01B 补齐）**。A（Esc 接线）、B（宝箱 Collider/Interactable）、C（门 prefab + fader）的 **wiring 全部到位并读回通过**；01B 补了 `EventSystem + StandaloneInputModule`（不用含 `InputSystemUIInputModule` 的 StarterAssets prefab），并确认 `chest_gold` 开箱非空（**金币袋 ×2**：嵌套 `coin_stack_medium` 原本就带 1 个，本单按批准清单又加 `Loot_Gold` 1 个）。四套件各 2 次与基线一致。三项真实键鼠功能仍留目检。另核对出：Canvas 原本已有 `GraphicRaycaster`（01B 的 B 项为 no-op）。

## 2. AC 回填表

| AC | 断言 | 结果 | 关键数据 | 证据 |
|---|---|---|---|---|
| AC-1 | `M7-FIX-01.Wiring.A`（Esc） | **PASS（静态）** | `UIManager.escMenuPanel=PausePanel`；`PausePanel` 有 `EscMenuPanel`；3 按钮 persistent=1/1/1 | `wiring.txt`、`readback.txt` |
| AC-2 | `M7-FIX-01.Wiring.B`（宝箱） | **PASS（静态）** | `chest_base`：`BoxCollider`（合并 2 Renderer，center `(0,0.65,0.02)` size `(1.70,1.30,1.45)`）+ `Interactable`（hint 点击开启宝箱）；`chest_gold` 变体继承（3 Renderer） | `wiring.txt`、`readback.txt` |
| AC-3 | `M7-FIX-01.Wiring.C`（门） | **PASS（静态 + NavMesh）** | 门 prefab：`DungeonDoor`+`BoxCollider`+`DoorModel`+两点；场景实例 `(4,0.08,2)`；entry `(4,0.08,5)` `NavMesh.SamplePosition=True`；`TransitionFader`+CanvasGroup | `wiring.txt`、`readback.txt` |
| AC-4 | `M7-FIX-01.Suites` | **PASS** | `REFACTOR` 20/0 ×2；`SPEC01` 12/0 ×2；`FND00B06` 12/0+2obs ×2；`DIAG` 6/1 ×2（已知红） | `Summary.md` §2、各 canonical `editor_runN` |
| AC-5 | `M7-FIX-01.Assets` | **PASS** | 只改 `DungeonWorld.unity`、`chest_base.prefab`、`chest_gold.prefab`、新增 `dungeon_door.prefab`(+meta)；未提交资产 | `资产改动清单.md`、`git status` |
| AC-6 | `M7-FIX-01.Docs` | **PASS** | `REPORT_M7-4` 已改 4 处（24 条 / 81 脚本 / 去“无回归” / AC-6 口径）；`醒来目检清单` 2/4/5 已加状态 | `REPORT_M7-4_全量回归.md`、`Docs/QA/Evidence/M7-4/醒来目检清单.md` |
| AC-7 | `M7-FIX-01.Manual` | **NOT RUN** | Esc 键 / 宝箱点击 / 门 F：需真实键鼠，MCP 无法注入 | 见 §7 未决项 |
| AC-8 | `M7-FIX-01B.EventSystem` | **PASS（静态 + Play）** | 场景 `EventSystem` + `StandaloneInputModule`；无 `InputSystemUIInputModule`；Canvas 自身有 `GraphicRaycaster`（no-op） | `补充/wiring_01B.txt`、`补充/readback_01B.txt` |
| AC-9 | `M7-FIX-01B.ChestContent` | **PASS（Play 读回）** | `chest_gold` `ChestItem` ×2（均金币袋 50）；Play 模式 `Contents.nonNull=2`、`total=8` | `补充/readback_01B.txt` |
| AC-10 | `M7-FIX-01B.Suites` | **PASS** | `REFACTOR` 20/0 ×2（run46/47）；`SPEC01` 12/0 ×2（run65/66）；`FND00B06` 12/0+2obs ×2（run15/16）；`DIAG` 6/1 ×2（run17/18，已知红） | `补充/Summary.md` |

**运行次数**：四套件各连续 2 次；两次 `pass/fail/observed` 一致。

## 3. 实现原理（面向所有者）

### 3.1 根因

1. **Esc 无响应**：`DungeonWorld` 的 `UIManager.escMenuPanel` 序列化为 `{fileID: 0}`；`UIManager.cs:81` 的 `if (gameOn && escMenuPanel != null)` 因此永不进入。场景里虽有 `PausePanel`，但没有 `EscMenuPanel` 组件。
2. **宝箱点不动**：`SelectionManager.cs:151` 用 `Physics.RaycastAll` 找 `IInteractionTarget`；两个宝箱 prefab 根只有 `Chest`+`Inventory`，**没有 Collider**，射线不命中。`Chest.cs:37` 还依赖同物体的 `Interactable` 提供 hint，也没有。
3. **门没得测**：`DungeonWorld` 没有 `DungeonDoor`，也没有 `TransitionFader`；M7-2 的 `Refactor.DoorTransitionEvent` 用的是测试运行期假 fader，不代表场景可用。

### 3.2 修法（最小 wiring，无代码改动）

- A：给 `PausePanel` 加 `EscMenuPanel`，加 3 个 `Button`（`UnityEventTools.AddPersistentListener` 接 `OnClickResume/OnClickRestart/OnClickQuitToMenu`），把 `UIManager.escMenuPanel` 指向它。
- B：在挂 `Chest` 的物体上，按**所有子 Renderer 合并 bounds** 加 `BoxCollider`，加 `Interactable`（hint「点击开启宝箱」）。`chest_gold` 是 `chest_base` 变体，自动继承。
- C：新建 `dungeon_door.prefab`（`DungeonDoor` + `BoxCollider` + `DoorModel` + `DungeonEntryPoint`/`CameraPoint`），场景实例放在 `(4,0.08,2)`，入口 `(4,0.08,5)` 经 `NavMesh.SamplePosition` 校验；新增 `TransitionCanvas`（CanvasGroup+黑 Image+`TransitionFader`）。

### 3.3 关键位置（`文件:行`）

- `Assets/_Project/Scripts/UI/UIManager.cs:81` —— Esc 分支的空引用守卫；`escMenuPanel` 为 0 即整段跳过。
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs:151` —— 点击判定用 `Physics.RaycastAll`，Collider 是必要条件。
- `Assets/_Project/Scripts/Gameplay/World/Chest.cs:37` —— `Chest.Awake` 缓存同物体 `Interactable`；`Chest.cs:64` 的 `Hint` 依赖它。
- `Assets/_Project/Scripts/Gameplay/World/DungeonDoor.cs:50` —— M7-2 已改为 `EventBus.Publish(TransitionRequest)`；本单补场景真实订阅者。
- `Assets/_Project/Scripts/UI/TransitionFader.cs:22` —— `OnEnable` 订阅 `TransitionRequest`，`OnDisable` 退订。

### 3.4 边界 / 失败模式

- 3 个按钮**当前点不动**：`DungeonWorld` 全项目没有 `EventSystem`；Esc 键盘路径不受影响。补 `EventSystem` 属清单外，未做。
- 宝箱能开但**物品为空**：两个 prefab 都没有 `ChestItem`，`Contents` 返回空 `Inventory`。加什么物品是内容决策。
- 门是**一次性**（`_used`）；传送/相机切换的真实观感需目检。

### 3.5 三句话版本

1. Esc 不响应的根因是场景里 `UIManager.escMenuPanel` 为 0，不是暂停逻辑坏了；本单把 `PausePanel` 接上 `EscMenuPanel` 并补了 3 个按钮。
2. 宝箱点不动是因为 prefab 没有 Collider；本单按子 Renderer 合并 bounds 补了 `BoxCollider` 和 `Interactable`，`chest_gold` 作为变体自动继承。
3. 门和 fader 原来只存在于测试假对象里；本单新建了带可见模型的门 prefab 与场景 fader，入口点已用 `NavMesh.SamplePosition` 验证在导航网格上。

### 3.6 与计划的差异

- 未改任何 `.cs`：根因都在资产 wiring，代码逻辑（M7-2 下沉点）已验证正确。
- 追加了 `TransitionCanvas` 的 3 个按钮持久监听；清单内 A2 已覆盖。
- **01B 补齐**：新增 `EventSystem + StandaloneInputModule`（未用含 `InputSystemUIInputModule` 的 StarterAssets prefab）；`chest_gold` 加 `Loot_Gold`。核对发现 `coin_stack_medium` 原本已有 `ChestItem`，故现在金币袋 ×2。

## 4. 受影响模块导航

| 模块 | 职责 | 入口 |
|---|---|---|
| UI / Esc | `UIManager.Update` 捕获 Esc → `OpenPanel(EscMenuPanel)` → `AppFlow.Pause` → `PauseState` + `timeScale=0` | `Canvas/PausePanel`、`UIManager.escMenuPanel` |
| 宝箱 | 鼠标点击 → `SelectionManager` 射线 → `Interactable`/`Chest` → 走到范围 → `Chest.OpenBy` → `Chest.Opened` → `UIManager.ShowInventory` | `chest_gold` 场景实例、`chest_base` prefab |
| 门 | F 键 → `DungeonDoor.Enter` → `EventBus.Publish(TransitionRequest)` → `TransitionFader` 黑屏 → 传送/相机 → 亮屏 | `dungeon_door` 场景实例、`TransitionCanvas` |

**改动资产清单**（与批准清单逐项比对）

| 文件 | 动作 |
|---|---|
| `Assets/Scenes/DungeonWorld.unity` | 改：PausePanel 组件/按钮、UIManager 引用、TransitionCanvas、door 实例、EventSystem（01B） |
| `Assets/_Project/Prefabs/chest_base.prefab` | 改：BoxCollider + Interactable |
| `Assets/_Project/Prefabs/chest_gold.prefab` | 改：变体重存（继承）+ Loot_Gold（ChestItem + GoldPile，01B） |
| `Assets/_Project/Prefabs/dungeon_door.prefab` | 新增 |
| `Assets/_Project/Prefabs/dungeon_door.prefab.meta` | 新增 |

未改：其他场景/预制体/Animator/`.asset`/`ProjectSettings`/`Packages`/任何 `.cs`。

## 5. 证据清单

| 文件 | 内容 |
|---|---|
| `Docs/QA/Evidence/M7-FIX-01/资产改动清单.md` | 资产清单 + 批准记录 + 执行回执 |
| `Docs/QA/Evidence/M7-FIX-01/readback.txt` | execute_code 日志、组件读回、NavMesh 读回、偏差 |
| `Docs/QA/Evidence/M7-FIX-01/wiring_check.py` / `wiring.txt` | 01 wiring 断言（15 PASS；01B 后扩为 23 条） |
| `Docs/QA/Evidence/M7-FIX-01/Summary.md` | 证据索引与结论 |
| `Docs/QA/Evidence/M7-FIX-01/补充/` | 01B：清单/批准记录、执行读回、wiring 23 PASS、Summary |
| `REFACTOR-M4/editor_run44..45` | 01：20/0 ×2 |
| `SPEC-01/editor_run63..64` | 01：12/0 ×2 |
| `FND-00B-06/editor_run13..14` | 01：12/0+2obs ×2 |
| `DIAG-SKILL/editor_run15..16` | 01：6/1 ×2（已知红） |
| `REFACTOR-M4/editor_run46..47` | 01B：20/0 ×2 |
| `SPEC-01/editor_run65..66` | 01B：12/0 ×2 |
| `FND-00B-06/editor_run15..16` | 01B：12/0+2obs ×2 |
| `DIAG-SKILL/editor_run17..18` | 01B：6/1 ×2（已知红） |

## 6. 学习讲稿

**不适用**：本单是最小 wiring 修复，没有新设计取舍；Esc/宝箱/门的架构决策已在 `Docs/学习/M7-2_讲稿.md` 覆盖（暂停下沉、模态接口、门事件）。

## 7. 未决项

1. **真实键鼠 3 项目检**：Esc 打开后点继续/重开/退出、宝箱点击开箱、门 F 过场（MCP 无法注入输入）。
2. **宝箱内容为金币袋 ×2**：`coin_stack_medium` 原本已有 1 个 `ChestItem`，本单按清单又加 `Loot_Gold` 1 个；若只要 1 个，删除 `Loot_Gold` 即可。
3. **资产未提交**：`DungeonWorld.unity`、`chest_base.prefab`、`chest_gold.prefab`、`dungeon_door.prefab`(+meta) 由所有者提交。
4. **EventSystem**：已补 `EventSystem + StandaloneInputModule`，无 `InputSystemUIInputModule`；按钮可点性待目检。

## 8. 建议提交

| 组 | 文件 | 提交信息 |
|---|---|---|
| 证据/文档（本单自动提交） | `Docs/QA/Evidence/M7-FIX-01/**`、`REPORT_M7-FIX-01`、`REPORT_M7-4`、`醒来目检清单.md` | `修复：M7-FIX-01B 补 EventSystem 与宝箱内容（M7-FIX-01B/收口）` + `为什么` / `验证` |
| 资产（所有者提交） | `DungeonWorld.unity`、`chest_base.prefab`、`chest_gold.prefab`、`dungeon_door.prefab`(+meta) | `修复：M7-FIX-01 场景与预制体接线（资产）` |

## 9. 消耗记录

- 钱包：`Go 订阅`
- 模型完整 ID：`opencode-go/deepseek-v4.1-flash`
