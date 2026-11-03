# REPORT_FND-01 — NavMesh 诊断与修复

> 执行者：OpenCode + DeepSeek（执行者角色）
> 执行时间：2026-11-03 20:00–20:45 +08:00
> 场景：`Assets/Scenes/DungeonWorld.unity`｜Unity `2022.3.62f3c1`
> 依据：`Docs/Handoff/TASK_FND-01_NavMesh诊断与修复.md`
> 证据：`Docs/QA/Evidence/FND-01/`

## 1. 三类候选原因逐项结论

| 候选原因 | 结论 | 证据 |
|---|---|---|
| 场景未烘焙 | **已确认（直接原因）** | 场景 `NavMeshSettings.m_NavMeshData = {fileID: 0}`；场景内 `NavMeshSurface` 数量 0；`NavMesh.CalculateTriangulation()` = 0 顶点；7 个采样点全部失败；6 条路径全部 `PathInvalid` |
| NavMeshSurface 未激活 | **已排除** | 全工程 `.unity`/`.prefab` 中 `NavMeshSurface` 出现 0 次；不存在“有对象/组件但未激活”的情况；本项目使用旧版 Navigation 工作流 |
| 烘焙数据丢失 | **部分命中：文件在、数据有效、但引用断裂** | `Assets/Scenes/DungeonWorld/NavMesh.asset` 存在（14652 字节），可加载，`sourceBounds = (0,0,0) size=(200,0,200)`，二进制含 36 个 `VAND` 瓦片（首瓦片索引 -3,-3），与 `ForestGround` 的 200×200 地面完全吻合；但该 GUID 在全工程被引用 **0 次**，场景引用为空 |

补充事实：

- `ForestGround` 为内置 Plane（`scale 20×1×20`），`m_StaticEditorFlags = 0`，`MeshCollider` 已启用。
- 场景内 3 个玩家 Agent（Knight/Archer/Mage）均 `m_Enabled: 1`，起点 `(-2.72,0,0)`、`(0.85,0,0)`、`(3.22,0,0)`。
- 修复前运行期：`Editor.log` 中 `Failed to create agent because there is no valid NavMesh` 24 行；`GetRemainingDistance` 3764 处，堆栈固定为 `UnitBrain.get_ExecutingMove()`（`UnitBrain.cs:53`）← `CharacterBrain.Update()`（`CharacterBrain.cs:54`）。

**根因链**：场景未引用烘焙数据 → 运行时无 NavMesh → Agent 未被放置到 NavMesh → `CommandMove` 中 `agent.isOnNavMesh` 为 false 而不下发 → `_hasMove` 仍为 true，`ExecutingMove` 每帧读取 `remainingDistance` → 每帧抛 `GetRemainingDistance` 并在原地不动。

## 2. NavMesh 两资产状态与历史之谜

### 2.1 当前状态

| 文件 | 存在 | 大小 | 工作区 hash | HEAD blob | 一致 |
|---|---|---|---|---|---|
| `Assets/Scenes/DungeonWorld/NavMesh.asset` | 是 | 14652 | `7d81e5e1…37dd` | `7d81e5e1…37dd` | 是 |
| `Assets/Scenes/DungeonWorld/NavMesh.asset.meta` | 是 | 197 | `c4c123c9…04cf` | `c4c123c9…04cf` | 是 |

- `git status/diff/diff --cached` 对两文件均为空；当前**不存在**“内容相同仍显示 M”的异常。
- `.meta` GUID：`fea05cc92de5b1d40b9a472821cb585a`；与场景引用核对：修复前场景为 `fileID: 0`，无引用；修复后指向该 GUID。
- 两文件 mtime/创建时间均为 2026-11-03 15:17:18，与既有报告所述“删除后恢复为 HEAD 内容”的时点吻合。

### 2.2 历史解释

- `git log --follow` 显示：`NavMesh.asset` 在 `e1e197dc`（FND-00A 基线提交）作为新文件加入，`.meta` 由 Git 识别为自 `StarterAssets/…/Playground/NavMesh.asset.meta` 复制/改名（相似度 79%），但 GUID 不同（Playground 为 `3dadeea9…`，其资产 554632 字节），**不是同一个资产**。
- `e1e197dc` 中 `DungeonWorld.unity` 的 `m_NavMeshData` 已经是 `{fileID: 0}`；`git log -S"m_NavMeshData: {fileID: 23800000"` 对该场景无任何结果 → **有效引用从未进入 Git 历史**。
- 已解释：历史“M 状态/删除后恢复”已随 15:17 的恢复消失；当前内容与 HEAD 完全一致。
- 仍未解释：该 14KB 烘焙数据由何时、以何操作产生（其 `sourceBounds` 与当前地面一致，说明确实按本场景地面烘焙过）。不影响本次修复判断。

## 3. 修复动作与变更文件

| 修改前 | 实际动作 | 修改后 |
|---|---|---|
| `m_NavMeshData: {fileID: 0}` | 将场景 NavMeshSettings 指向既有 `NavMesh.asset`（`fileID: 23800000, guid: fea05cc9…585a, type: 2`） | `m_NavMeshData: {fileID: 23800000, guid: fea05cc92de5b1d40b9a472821cb585a, type: 2}` |

- 未重新烘焙：既有数据经实测有效且正好覆盖 200×200 地面，重新烘焙不是最小动作。
- 未改动 `NavMesh.asset` / `.meta`、任何 `.cs`、预制体、Animator、`ProjectSettings`、包依赖。
- 修复通过 Unity 批处理在隔离副本中完成并验证；随后对真实场景做同值单行修改，避免 Unity 重存场景时顺带改写 3 个与导航无关的 `CursorManager` 旧字段（`info*` → `interact*`）。隔离副本场景与真实修复后场景 SHA256 完全一致（`7C44B9EB…816B`）。

**全部变更文件**：

```
M  Assets/Scenes/DungeonWorld.unity        （+1 / -1，仅 m_NavMeshData 一行）
?? Docs/QA/Evidence/FND-01/                 （新增证据目录）
?? Docs/Handoff/REPORT_FND-01*.md           （本报告）
```

## 4. 移动验收

### 4.1 自动化等价验证（已执行，通过）

环境：隔离副本，场景与真实修复后文件逐字节一致；Unity 批处理 + PlayMode。

| 项 | 结果 |
|---|---|
| 起点 | Knight `(-2.72, 0.08, 0.00)` |
| 目标 | `(0.00, 0.00, -30.00)`（营地南侧通道，无树障碍） |
| 路径表现 | 3 个 Agent 全部 `isOnNavMesh=True`；`SetDestination` 后沿 NavMesh 实际移动 |
| 停步结果 | 7.69s 到达 `(-0.04, 0.08, -29.60)`，距目标 0.40m（`stoppingDistance=0.3`），`arrived=True`；非瞬移、非原地无响应 |
| Console 新增 `GetRemainingDistance` | **0 条** |
| Console 新增 `Failed to create agent…` | **0 条** |

EditMode 辅助验证：三角化 144 顶点/216 索引，包围盒约 `(-99.33,0.08,-99.33)`–`(99.33,0.08,99.33)`；营地及 6 个方向的 `SamplePosition` 全部命中、`CalculatePath` 全部 `PathComplete`。

原始输出：`Docs/QA/Evidence/FND-01/自动化验证输出.txt`。

### 4.2 真实右键复验（待用户执行）

批处理无法模拟真实鼠标右键，因此工单字面要求的“右键点击”尚未执行。步骤与待填表格见 `Docs/QA/Evidence/FND-01/验收记录.md` 第 3 节。按两条核心标准（沿路径到达、新增报错 0），自动化等价验证均满足，但**在用户完成真实右键复验前，不宣布“修复通过”**。

## 5. 未解决问题与建议

1. **真实右键复验待执行**（见 4.2）。完成后建议恢复 FND-00B-03，从“移动”继续，再补普攻/动画。
2. `ForestGround` 的 `NavigationStatic` 位仍为 0。当前不影响运行；但将来若重新烘焙，必须先勾选该位，否则会再次得到空数据。本次为保持最小改动未修改。
3. 重新打开/刷新场景后方可生效：Unity 若提示场景在磁盘上变更，应选择重新加载，避免用编辑器内旧场景覆盖保存。
4. 镜头、技能等失败项不属于 FND-01 范围，未处理。

## 6. 完成判定

- 诊断结论明确、两资产状态已核对、修复未越界。
- 自动化等价验收两条核心标准均满足。
- 真实右键复验未执行 → 本报告结论为：**修复已实施并自动化验证通过，真实右键验收待用户确认**；不自行标记 FND-01 为 Done。
- AI 未执行任何 Git 提交/推送/恢复/清理。
