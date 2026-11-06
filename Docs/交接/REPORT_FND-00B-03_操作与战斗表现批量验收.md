# REPORT_FND-00B-03 — 操作与战斗表现批量验收

> 工单：`Docs/Handoff/TASK_FND-00B-03_操作与战斗表现批量验收.md`（含 2026-11-03 修订：取消逐步截图，改为口述记录）
> 记录日期：2026-11-03
> 记录方式：用户口述，记录员原样转录；记录员未补全、未归因、未修复

## 1. 会话与提交

| 项 | 值 |
|---|---|
| 验收提交 | 前置确认时：`bf21b78dd5e41c80697c333c4f891f10f3852405`；会话期间用户提交工单修订后：`e12c9e56`（仅 `Docs/Handoff/` 三份工单与 `00_前置确认.txt`，无 `Assets/` 变更） |
| 分支 | `docs/fnd-00-baseline` |
| 场景 | `DungeonWorld`（`Assets/Scenes/DungeonWorld.unity`） |
| Unity | `2022.3.62f3c1` |
| 会话开始时间 | 未提供（用户未记录）；衔接证据：`Editor.log` 本次写入起始 `2026-11-03 16:46:51` |
| 会话结束时间 | 未提供（用户未记录）；记录员最后只读快照 `2026-11-03 17:23:27` |
| Play 次数 | 用户按工单在一次 Play 内完成已执行部分（口述未明确，记录为“同一次会话”） |
| 前置确认 | 通过：工作区干净（`git status --porcelain` 无输出），豁免条款未使用 |

## 2. 五个子项结果

| QA 项 | 结果 | 关键实测结果 | 口述记录/补充证据 | 遗留问题 |
|---|---|---|---|---|
| 2 移动 | `失败` | 三个角色均选中，起始位置约 `(0,0,0)`（x 相差个位数）；右键下达移动后无响应、位置没变；Console 红色报错 `"GetRemainingDistance" can only be called on an active agent that has been placed on a NavMesh.`（`UnitBrain.cs:53` / `CharacterBrain.cs:54`） | `验收口述记录.md`；`00_前置确认.txt`（会前日志基线 0 处） | 路径、停步、镜头跟随未观察到（角色未移动）；无截图，异常以口述原文＋只读日志统计为据 |
| 3 镜头 | `失败` | 选中角色后左键不能拖动视角；未选中状态下可以拖动 | `验收口述记录.md` | 仅测左键拖动，未记录拖动前后的角度数值 |
| 4 普攻 | `未测试` | 因角色不能移动，迷雾视野中难以看到敌人，无法选定敌人，未能下发普攻指令 | `验收口述记录.md` | 被第 2 项状态阻塞，未实际执行；非静默跳过，阻塞原因已记录 |
| 5 技能 | `失败` | 选中骑士按 `Q` 没有弹出技能指示器，未观察到技能释放 | `验收口述记录.md` | 覆盖不完整：职业清单未逐一确认，其他职业技能与骑士盾击未实测；`Q` 对应技能未确认 |
| 6 动画表现 | `未测试` | 本会话未汇总待机/移动/攻击/受击/死亡的播放情况 | `验收口述记录.md` | 因移动与普攻未成功，未能在末尾单独触发缺失状态；未制造团灭 |

## 3. Console 异常（客观记录，不分析原因）

- 进入 Play 前，用户提供的 Console 文本：
  - `Failed to create agent because there is no valid NavMesh` ×3
  - `0 个身体部件已挂到 PlayerBody 层`（`DebugCameraSwitcher:Start`，`DebugCameraSwitcher.cs:102`）
- Play 中移动操作触发红色报错，堆栈同第 2 项。
- 只读核对 `C:\Users\lll\AppData\Local\Unity\Editor\Editor.log`：

| 时点 | 文件大小 | `GetRemainingDistance` 出现次数 |
|---|---|---|
| 会前基线 16:46:51 | 32,697 字节 | 0 |
| 17:15:59 | 648,486 字节 | 1,598 |
| 17:23:27 | 1,136,695 字节 | 2,783 |

  - 2,783 处匹配的**唯一文本只有 1 种**，堆栈固定为 `UnitBrain:get_ExecutingMove ()`（`UnitBrain.cs:53`）、`CharacterBrain:Update ()`（`CharacterBrain.cs:54`）。
  - 相邻两次出现多为间隔 7 行（2,780 次），即同一条报错连续重复；另有 2 处间隔（349 行、1,055 行）为夹入的其他日志。
  - 这些行不带时间戳，日志未记录每次出现的具体时刻。
- 用户推测（逐字，未验证，不作为结论）：“没有烘培寻路，每帧检查的时候都报错”。
- 未清除 Console、未修改或删除日志。

## 4. 口述记录与证据清单

实际存在：

- `Docs/QA/Evidence/FND-00B-03/00_前置确认.txt`（含 Git 原始输出、Editor.log 基线 0 处、上一会话 `Editor-prev.log` 4,058 处的背景登记）
- `Docs/QA/Evidence/FND-00B-03/验收口述记录.md`（五个子项用户口述原文、Console 文本、只读统计与用户推测）

未创建（按修订后工单非必交）：

- 各子项截图（`02_移动_*.png`、`03_镜头_拖动后.png`、`04_普攻_结果.png`、`05_技能_结果.png`、`06_动画_*.png`、`01_会话开始_Console.png`、`99_会话结束_Console.png`）：用户未提供，工单修订后不要求逐步截图。
- `Unity_操作与战斗.log`：未创建；异常原文保存在 `验收口述记录.md`，日志数据以只读统计形式记录。

## 5. 改动文件

新增：

- `Docs/QA/Evidence/FND-00B-03/00_前置确认.txt`
- `Docs/QA/Evidence/FND-00B-03/验收口述记录.md`
- `Docs/Handoff/REPORT_FND-00B-03_操作与战斗表现批量验收.md`（本文件）

修改：

- `Docs/QA/FND-00B_现有行为验收.md`：顶部当前状态、第 2～6 项、汇总第 2～6 行、11 项统计与末期说明。

未修改：`Assets/`、`Packages/`、`ProjectSettings/`、代码、场景、预制体、Animator、`.meta`；未重新烘焙；未安装包；未构建；未升级 Unity；未修改 `CURRENT.md`。

## 6. 工作区检查

- 前置：`git rev-parse --abbrev-ref HEAD` = `docs/fnd-00-baseline`；`git rev-parse HEAD` = `bf21b78dd5e41c80697c333c4f891f10f3852405`；`git status --porcelain` 无输出（干净）。
- 会话期间（`2026-11-03 17:12:44`）用户自行提交 `e12c9e56`：“三个批量工单证据标准改为文字描述优先”，含 3 份 `Docs/Handoff/` 工单与 `00_前置确认.txt`，`git show --stat` 未见 `Assets/`、`Packages/`、`ProjectSettings/` 变更；该提交由用户执行，非记录员。
- 记录员在收尾时只读核对：`git status --porcelain -- Assets Packages ProjectSettings` 无输出，即本次 Unity 会话未产生资产/设置变更。
- Git 变更操作：**未执行**（记录员无 add/commit/push/分支操作）。

## 7. 未覆盖项

- 第 4 项普攻：未实际执行（被移动失效阻塞）。
- 第 6 项动画表现：未观察记录任何状态。
- 第 5 项：仅尝试骑士 `Q` 一项，未覆盖其他职业技能与骑士盾击。
- 第 7～9、11、12 项：不属于本工单，保持原状（未测试）；第 10 项目标登记不变。
- 未提供 Console 起止计数与场景保存提示之外的收尾细节（用户仅答“没有弹出”）。

## 8. 遗留问题

- 移动完全无响应且持续输出同一 `GetRemainingDistance` 报错（本次会话 2,783 处，会前基线 0 处），导致后续普攻、技能、动画三项无法完整验收；现象已记录，未分析原因，未修复。
- 选中角色后左键无法拖动视角，未选中时可拖动。
- 骑士按 `Q` 无技能指示器，职业技能体系整体未确认。
- 上一会话遗留 `Editor-prev.log` 中同一报错 4,058 处，仅作背景登记。
- 用户推测“NavMesh 未烘焙导致每帧报错”，未验证；本报告不将其作为结论。
- 会话开始/结束时间、Console 计数、退出 Play 的具体方式未记录。
