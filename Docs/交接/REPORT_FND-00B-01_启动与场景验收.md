# REPORT_FND-00B-01 — 启动与场景验收

> 工单：`Docs/Handoff/TASK_FND-00B-01_启动与场景验收.md`
> 记录日期：2026-11-03
> 记录方式：AI 逐项提问，用户提供实际运行结果；AI 未做任何推测填充

## 结果摘要

- 验收提交：`e08590889577792ae449e59329f8f74fd3f28f40`
- 分支：`docs/fnd-00-baseline`
- 测试场景：`DungeonWorld`（`Assets/Scenes/DungeonWorld.unity`），工程打开后即为该场景
- Unity：`2022.3.62f3c1`（与要求版本一致）
- 第 1 项结果：`部分通过`
- 阻塞或异常：
  - 前置确认首次检查时工作区不干净（` M AGENTS.md`、`?? Docs/Handoff/TASK_FND-00B-01_启动与场景验收.md`），按工单停止；用户自行提交后复查干净，HEAD 变为 `e0859088`，随后继续。
  - Play 期间 Console 有 3 条黄色警告 `Failed to create agent because there is no valid NavMesh`。
  - Play 期间 Console 有 1 条 Log `0 个身体部件已挂到 PlayerBody 层`（`DebugCameraSwitcher.Start`，`Assets/_Project/Scripts/Debug/DebugCameraSwitcher.cs:102`）。
  - 退出本次 Play 之后，用户另行操作“选中角色 → 右键点击地面”出现红色报错 `"GetRemainingDistance" can only be called on an active agent that has been placed on a NavMesh.`（`UnitBrain.cs:53` / `CharacterBrain.cs:54`），伴随角色不移动。该操作为第 2 项“移动”范围，本工单未测试该项，此条仅作口述记录，不构成本工单结论。

## 问答记录（按提问顺序）

| # | 提问 | 用户回答（原样要点） |
|---|---|---|
| 0 | 前置：工作区是否干净 | 首次不干净（`AGENTS.md` 已改、工单文件未跟踪）；用户自行提交后干净 |
| 0b | 是否使用 Unity `2022.3.62f3c1` | 是；实查版本 `2022.3.62f3c1` |
| 1A | Unity 实际版本号 | `2022.3.62f3c1` |
| 1B | 工程是否正常打开 | 能 |
| 1C | 是否有阻塞性编译错误 | 无 |
| 1D | 证据文件是否生成 | `01_工程已打开.png` 已截（由 AI 从其截图目录复制入证据目录）；`01_Console_编译检查.png` 未截，仅提供 Console 文本 `Couldn't find a readme`（`ReadmeEditor.cs:90`） |
| 2A | 场景名称 | `DungeonWorld` |
| 2B | 场景完整路径 | `Assets/Scenes/DungeonWorld.scene`（用户自述不确定后缀；实查仓库为 `Assets/Scenes/DungeonWorld.unity`） |
| 2C | 场景来源 | 工程打开后自动就是 |
| 2D | `03_测试场景.png` 是否已保存 | 用户答“已保存”，实查证据目录无此文件；后改为用先前的 `01_工程已打开.png` 代替（AI 复制为 `03_测试场景.png`，两者同一张图） |
| 3A | 是否成功进入 Play | 成功 |
| 3B | 进入后可见现象 | 无明显异常（画面：绿色地面、HUD“金币：0 / 等级：1 / 击杀：0”、3 角色 1 敌人、底部 4 个深色图标格） |
| 3C | Console 状态 | 3 条 `Failed to create agent because there is no valid NavMesh` + 1 条 `0 个身体部件已挂到 PlayerBody 层` 的 Log；经追问确认为**黄色**，非红色；无红色错误 |
| 3D | 退出 Play 是否弹保存提示 | 没有 |
| 3E | 截图位置 | `02_Play_运行.png` 已提供（由 AI 从截图目录复制）；`02_Play_Console.png` 未截，用户明确“不补” |
| 4A | 是否存在可运行构建 | 没有 |
| 4B | 构建路径/版本/日期 | 不适用（无可运行构建） |
| 4C | 最近一次完整验收时间 | 无记录；用户补充“今天做过测试但觉得已不可靠，需要重新验收” |
| 4D | 该次验收范围 | 用户称“现在和那次验收有差异”，未给出当时的具体范围 |
| 4E | 信息来源 | 我本人验收 |

## 判定依据

工单第 5 节规定：**通过** = 工程正常打开、成功进入 Play、无阻塞性编译错误，且三项均有证据。

实查：前两项有截图证据；第三项的 Console 截图证据缺失（`01_Console_编译检查.png`、`02_Play_Console.png` 均无），仅有用户口述“无阻塞性编译错误”。因此按规则记为 **部分通过**，未记为通过。

若用户后续补交上述两张 Console 截图，且截图内容与口述一致，可按工单规则改为 `通过`，并同步汇总为 通过 1 ｜ 部分通过 0 ｜ 未测试 10。

## 改动文件

新增：

- `Docs/QA/Evidence/FND-00B-01/00_前置确认.txt`
- `Docs/QA/Evidence/FND-00B-01/01_工程已打开.png`（来源：`C:\Users\lll\Pictures\Screenshots\屏幕截图 2026-11-03 144555.png`）
- `Docs/QA/Evidence/FND-00B-01/02_Play_运行.png`（来源：`C:\Users\lll\Pictures\Screenshots\屏幕截图 2026-11-03 150250.png`）
- `Docs/QA/Evidence/FND-00B-01/03_测试场景.png`（由 `01_工程已打开.png` 复制，非独立新截图）
- `Docs/Handoff/REPORT_FND-00B-01_启动与场景验收.md`（本文件）

修改：

- `Docs/QA/FND-00B_现有行为验收.md`：测试日期、当前状态、第 1 项内容、汇总第 1 行与总数、末期说明。其余 10 项与第 10 项目标登记未改动。

未创建：`Docs/QA/Evidence/FND-00B-01/Unity_启动与Play.log`（用户报告无红色错误）。

经用户授权后恢复（非本工单原定范围）：

- `Assets/Scenes/DungeonWorld/NavMesh.asset`（恢复为 HEAD 版本）
- `Assets/Scenes/DungeonWorld/NavMesh.asset.meta`（恢复为 HEAD 版本，`git status` 仍显示 ` M`，内容无差异）

## 检查结果

- 前置工作区：`首次不干净 → 按工单停止 → 用户自行提交 → 复查干净（git status --porcelain 无输出）`
- 验收提交：`e08590889577792ae449e59329f8f74fd3f28f40`（实查，未沿用文档旧值）
- 证据完整性：`缺失 01_Console_编译检查.png、02_Play_Console.png；03_测试场景.png 为复用图，未单独证明场景资产路径`
- 代码及 Unity 资产：`验收过程未修改；经用户授权后仅恢复 2 个 NavMesh 文件至 HEAD 版本`
- Git 操作：`AI 未执行任何提交、推送、分支或工作区清理操作`

## 遗留问题

- Console 编译状态无截图证据，仅有用户口述“无阻塞性编译错误”，导致第 1 项只能记 `部分通过`。
- `03_测试场景.png` 与 `01_工程已打开.png` 为同一张图；场景名称依据标题栏与层级根节点 `DungeonWorld` 辨认，未显示 `Assets/Scenes/DungeonWorld.unity` 路径。
- Play 期间存在 3 条 `Failed to create agent because there is no valid NavMesh` 黄色警告，现象已记录，未分析原因。
- 用户口述：在本次 Play 之后，“选中角色 → 右键点击地面”触发红色报错 `"GetRemainingDistance" can only be called on an active agent that has been placed on a NavMesh.`，角色不移动。该现象属第 2 项“移动”，本轮未按工单测试，仅记录口述，未验证。
- 用户口述推测“自动寻路 NavMesh 烘焙可能需要重新烘焙”，仅为用户推测，未验证，不作为结论。
- 最近一次完整验收无书面记录；用户称今天做过测试但认为不可靠、需重新验收，具体时间与范围均未确认。
- 是否存在可运行构建：用户答“没有”。
- **记录完成后的工作区状态（客观记录）**：写文档后执行 `git status --porcelain` 曾得到
  ` D Assets/Scenes/DungeonWorld/NavMesh.asset`、` D Assets/Scenes/DungeonWorld/NavMesh.asset.meta`，
  即这两个 NavMesh 资产在 14:43 前置确认（当时干净）之后变为“已删除”状态；删除发生的具体时刻与执行者未确认。
  AI 未删除任何 Unity 资产。
- **恢复操作（经用户明确授权后执行）**：2026-11-03 15:17，AI 执行
  `git restore --source=HEAD -- "Assets/Scenes/DungeonWorld/NavMesh.asset" "Assets/Scenes/DungeonWorld/NavMesh.asset.meta"`，
  仅这两个文件。结果：`NavMesh.asset` 恢复为 14,652 字节，与 HEAD blob `7d81e5e1` 一致；
  `NavMesh.asset.meta` 恢复后磁盘为 197 字节，`git hash-object` 与索引 blob `c4c123c9` 一致，
  `git diff` 无内容差异，但 `git status` 仍显示 ` M`（换行符/索引状态缓存差异），AI 未执行 `git add`、未提交。
- 恢复动作超出本工单原定范围（工单禁止修改 `Assets/`），已获用户口头授权；恢复的是 2026-11-01 提交版本，
  是否与当前场景/烘焙状态一致、是否能消除 NavMesh 警告与移动报错，均**未验证**。
