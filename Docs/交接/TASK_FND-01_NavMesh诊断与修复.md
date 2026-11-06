# TASK_FND-01 — NavMesh 诊断与修复

> 优先级：最高  
> 状态：Ready  
> 类型：诊断与修复  
> 阻塞关系：FND-00B-03 的“移动”失败，普攻、动画及后续验收被阻塞；FND-00B 暂停，04/05 待办  
> 执行环境：Unity `2022.3.62f3c1`，场景 `Assets/Scenes/DungeonWorld.unity`

## 1. 目标与适用规则

先查明 NavMesh 失效及 `Assets/Scenes/DungeonWorld/NavMesh*` 状态不明的实际原因，再实施最小修复并验收移动。

**本单是修复单，“只记录不修复”规则对本单不适用。** 允许在诊断结论形成后修改必要的 NavMesh 配置、重新烘焙并保存相关场景/资产；不得把本授权扩展到无关 Bug、玩法或重构。

目标验收结果：进入 Play 后，右键指令能使角色沿可行路径移动到目标点，且验收过程中 Console **新增 0 条** `GetRemainingDistance` 报错。

## 2. 范围与边界

允许：

- 检查导航相关场景对象、组件、引用、烘焙数据和必要日志；
- 根据诊断结果激活既有 NavMeshSurface、修正其数据引用、重新烘焙或执行等价的最小 NavMesh 修复；
- 由 Unity 保存 `DungeonWorld.unity`、`Assets/Scenes/DungeonWorld/NavMesh*` 及 Unity 自动维护的相关 `.meta`。

禁止：

- 安装新导航包、升级 Unity、开始 Unity 6.3 迁移；
- 修改移动、攻击、技能等玩法代码，重构项目或全量改写场景/YAML；
- 借本单修复镜头、技能、迷雾、救援等其他 QA 项；
- AI 执行提交、推送、恢复、清理等 Git 变更操作。资产历史核对只使用只读命令；不得用 `git restore` 代替诊断。

允许写入诊断与验收记录：`Docs/QA/Evidence/FND-01/`，以及最终 `Docs/Handoff/REPORT_FND-01_NavMesh诊断与修复.md`。

## 3. 前置确认

**豁免条款："工单文件自身与 Docs/Handoff/、Docs/QA/ 下新增文件不计入工作区脏检查"。**

- [ ] 记录 `git rev-parse --abbrev-ref HEAD`、`git rev-parse HEAD`、`git status --porcelain` 的完整输出。
- [ ] 仅豁免本工单自身及 `Docs/Handoff/`、`Docs/QA/` 下 `??` 新增文件；其他修改照常列出。已有非豁免变更不得由 AI 清理，须在报告中区分并避免覆盖。
- [ ] 将结果写入 `Docs/QA/Evidence/FND-01/00_前置确认.txt`，记录完整 HEAD 与当前资产状态。
- [ ] 以 QA 已核验事实为基线：移动失败，`GetRemainingDistance` 连续重复；不要预填三类原因中的任何一个。

## 4. 诊断步骤（必须先做，未形成结论不得修复）

### 4.1 最小复现与运行流程确认

- [ ] 打开既有场景，记录当前导航工作流：使用旧版 Navigation 烘焙、NavMeshSurface，还是两者并存；不得安装或切换工作流来“试试”。
- [ ] 进入 Play，记录启动时是否出现 `Failed to create agent because there is no valid NavMesh`。
- [ ] 仅执行一次右键移动以复现 `GetRemainingDistance`，取得最小日志样本后立即停止持续操作，避免再次产生数千条重复日志。
- [ ] 记录选中角色、NavMeshAgent 是否启用、是否报告已位于 NavMesh 上；此处只收集状态，不修改代码。

### 4.2 区分三类候选原因

逐项取得证据，结论可为其中一项、多项或“均被排除”，不得凭用户推测选择：

| 候选原因 | 必查内容 | 判定依据 |
|---|---|---|
| 场景未烘焙 | 当前导航工作流的烘焙结果、Scene 视图 NavMesh 显示、Surface 数据引用 | 没有可供当前场景加载的有效烘焙结果；须注明实际看到的空引用或缺失表现 |
| NavMeshSurface 未激活 | Surface 所在 GameObject 的 activeSelf/activeInHierarchy、组件 enabled、场景内实际实例 | 数据可能存在，但承载对象或组件未激活；记录具体对象与状态 |
| 烘焙数据丢失 | `NavMesh.asset` 是否存在且可加载、Surface 的 NavMeshData 引用、GUID 与资产对应关系、数据是否为空或引用断裂 | 文件缺失、引用丢失/GUID 不对应、资产不可加载或有效数据为空；记录实际命中的情况 |

- [ ] 若三类均被排除，停止修复并报告证据；只可继续记录 NavMesh 数据加载和 Agent 放置状态，不得直接改玩法代码。
- [ ] 将检查过程与结论写入 `Docs/QA/Evidence/FND-01/诊断记录.md`，明确“已确认 / 已排除 / 未确认”。

### 4.3 查清 NavMesh* 资产修改状态之谜

针对以下两个文件逐一核对：

- `Assets/Scenes/DungeonWorld/NavMesh.asset`
- `Assets/Scenes/DungeonWorld/NavMesh.asset.meta`

必须完成：

- [ ] 记录当前文件存在性、大小、修改时间，以及 `git status --porcelain -- <两文件>`、`git diff -- <两文件>`、`git diff --cached -- <两文件>` 的结果。
- [ ] 用只读方式比较工作区文件与 HEAD：记录 `git hash-object <文件>` 和对应 HEAD blob 的哈希；不得只看时间戳或仅凭 `git status` 推断内容差异。
- [ ] 记录 `.meta` GUID，并与场景/Surface 的实际引用核对，区分“文件内容不同”“引用不同”“仅状态显示异常”。
- [ ] 对照既有报告所述历史：曾出现删除状态，随后恢复为 HEAD 内容，且 `.meta` 一度内容相同仍显示 `M`。确认当前是否仍存在该状态，不把历史描述当作当前结果。
- [ ] 给出可追溯结论：当前实际状态、与 HEAD 是否一致、历史遗留中已解释部分、仍未解释部分。若内容一致但状态仍异常，只记录，不执行 `git add`、`git update-index`、restore 或清理。

## 5. 最小修复

只有第 4 节诊断完成后才执行：

- [ ] 按已确认原因选择最小动作：未烘焙则使用现有工作流烘焙；Surface 未激活则激活既有对象/组件；数据丢失则修复现有引用或由 Unity 重新生成数据。
- [ ] 多项原因同时存在时仍只修复必要项，不顺手调整角色速度、寻路代码、动画、镜头或其他系统。
- [ ] 优先通过 Unity Editor 操作并保留既有 GUID/引用；不得手工全量重写 Unity YAML 或 `.meta`。
- [ ] 每次修改记录“修改前状态—实际动作—修改后文件—对应诊断结论”，写入 `诊断记录.md`。
- [ ] 若修复需要超出第 2 节范围，停止并提交新阻塞说明，不擅自扩大工单。

## 6. 验收标准

修复后在同一场景重新进入 Play：

- [ ] 选中一个可操控角色，记录起点和一个需要沿路径到达的明确目标点。
- [ ] 右键下发移动指令；角色必须实际沿可行路径移动，最终停在目标点或合理可达范围内，不得仅改变目标变量、瞬移或原地无响应。
- [ ] 从本次进入 Play、下发指令到角色停步并结束观察，Console 新增 `GetRemainingDistance` 报错为 **0 条**，不得用“减少”代替通过。
- [ ] 同时记录 `Failed to create agent because there is no valid NavMesh` 是否仍出现及其他 Console 现象。
- [ ] 将用户操作与结果原样记录到 `Docs/QA/Evidence/FND-01/验收记录.md`。仅当文字无法说明或复杂异常需追溯时补截图/日志。
- [ ] 验收后列出所有变更文件，并确认没有任务外代码或 Unity 资产修改。

两条核心标准（沿路径到达、GetRemainingDistance 新增 0）必须同时满足才判修复通过；否则判未通过并保留诊断与实测结果。

## 7. 报告与完成判定

生成一份 `Docs/Handoff/REPORT_FND-01_NavMesh诊断与修复.md`，包含：

1. 三类候选原因的逐项结论及证据；
2. NavMesh 两个资产当前状态、HEAD 对比、历史状态之谜的解释与剩余未知；
3. 实际修复动作和全部变更文件；
4. 移动验收的起点、目标、路径表现、停步结果、Console 新增错误计数；
5. 未解决问题与是否建议恢复 FND-00B-03 复验。

诊断结论不明、资产状态未核对、修复越界或核心验收任一条未满足，均不得报完成。AI 不提交、不推送；后续由【大脑】复审。
