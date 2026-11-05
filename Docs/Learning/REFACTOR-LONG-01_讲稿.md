# REFACTOR-LONG-01 讲稿：把「一处改、三处散」的旧代码收敛成四层契约（M0–M4）

> **AI 出初稿，所有者用自己的话改写。改不动的地方就是没懂 —— 让 AI 再讲一遍。**
> 覆盖长任务全部 5 个里程碑（M0 护栏 / M1 注册表 / M2 状态 / M3 交互 / M4 技能）。答辩先讲 §1 的三句话。

## 1. 一句话

这次没有加任何新玩法：先把「怎么证明行为没变」做成套件（M0），再把单位查找、局内状态、交互、技能四条散落的链各自收敛到一个契约（M1–M4），全程零场景/预制体/Animator/`.asset` 改动，每一层都用「断言只增不改口径 + SPEC01 12 条」验收。

## 2. 原来的问题 / 为什么需要

- **改之前的现象**：
  - 想找一个单位就 `FindObjectsByType` 全场景扫一遍（M1 前共 17 处），楼层/波次一变就出现「同一份查询写在不同系统里」；
  - 局内金币/等级/提示、场景流程/暂停/团灭各有一个静态类和单例互相读写（`GameData`/`GameFlow`/`PauseManager`），跨场景重载容易串数据；
  - 「点一下交互物 → 走过去 → 到位执行」在输入层、角色大脑、每个交互物里各写一遍，加一种交互物要改三处（M3 前门自己读 `Input`、宝箱直接调 `UIManager`）；
  - 加一个技能效果要自己猜 Animator 触发器名（`"Bash"`/`"Skill"` 散在方法体里）、自己 `GetComponent<UnitHealth>()` 当伤害来源；子弹固定打 `position + 0.6m`，高矮模型都会偏。
- **不解决会怎样**：下一个里程碑（预制体基座 + Unity 6.3 迁移）要在这些散点上逐处改，任何一处漏改都是「迁移后手感变了」的隐性回归，而且没有自动化判据。
- **触发条件**：所有者批准 8 小时长任务计划书，允许「先立尺子、再逐步搬家具」，并给出「不改行为、不动资产」的硬边界。

## 3. 方案对比

| 方案 | 做法 | 为什么不选 / 为什么选 |
|---|---|---|
| A：先迁移引擎/模板，再回头收拾代码 | 直接上 Unity 6.3 + 预制体基座 | 不行：迁移会把「行为变了」和「代码搬错了」混在一起，无法归因 |
| B：先立护栏（M0），再按依赖顺序逐层收敛（M1→M4），每层跑同一套断言 | 每层只动本层文件，断言只增不改口径 | **选定**：任何一步红了都能立刻定位在当层；零资产改动让回滚成本 = 一次 `git reset` |
| C：一次性大重构（全仓重排 + 换目录 + 改命名） | 一个提交做完所有层 | 不行：违反「不同时做实现 + 重构 + 文档」纪律，且无法分段验收 |

## 4. 数据流（M0–M4 依赖关系，文字版）

```text
M0 REFACTOR 套件（断言 = 尺子）
 ├── M1 单位注册表：所有“找单位”从 FindObjects* 改为 UnitRegistry 查询
 │     └── M2 状态门面拆分时，门面层不再引入新的全场扫描
 │           └── M3 交互 Runner 的“目标登记/最近 F 目标”建立在注册表之上
 │                 └── M4 技能上下文里 CasterUnit/Target 直接由契约给出
 └── 每一代套件只加断言（8→9→10→11→15），旧断言口径不动 → 行为不变可逐层证明
```

运行时数据流（M4 后）：`SkillInputController → SkillSystem.Cast(slot,target) → 组 SkillCastContext（槽位/触发器/状态名/目标/施法者/速度）→ 效果.CanCast(全部) → 扣蓝 → 效果.Execute(全部) → SetTrigger(ctx.Trigger) + 结算（Bash 直接伤害；Fireball 生成投射物 → ComputeEnd=AimPoint → 命中/爆炸）`。

## 5. 关键代码 7 处

| 位置 | 这段在干嘛 |
|---|---|
| `Assets/_Project/Scripts/Core/UnitRegistry.cs:208` | `Nearest(center,range,team,predicate)`：替换前的「全场景扫 + 3D 距离最近」换成注册表查询，语义与旧实现同口径（由 `Refactor.RegistrySemantics` 断言比对） |
| `Assets/_Project/Scripts/Core/AppFlow.cs:12` | `IsPaused`（原 `PauseManager.IsPaused` 的静态视图）+ `:15` `Pause()` 同时改状态与 `timeScale`，旧类保留为转发适配层 |
| `Assets/_Project/Scripts/Gameplay/Interaction/InteractionRunner.cs:48` | `Order(target)`：算落点（方向 × 半径）+ `CommandMove` + 记待办；技能期间被 `CharacterBrain.CanAcceptInteract` 挡住（M4 第 7 条断言直接打这条门禁） |
| `Assets/_Project/Scripts/Combat/SkillCastContext.cs:11` | 施放上下文的 7 个只读字段：槽位/触发器/状态名/目标/施法者/施法者单位/速度 |
| `Assets/_Project/Scripts/Units/SkillSystem.cs:53` | `Cast`：验冷却 → 按效果 `ContextFor` 组包 → 全部 `CanCast` → 扣蓝 → 全部 `Execute` → 写冷却（顺序与失败语义逐行未变） |
| `Assets/_Project/Scripts/Combat/Projectile.cs:42` | `AimPoint`：遍历目标全部 `Renderer` 合并 `bounds` 取 `center`；无 Renderer 回退 `transform.position`（`+0.6m` 已删除）；`:33` `ComputeEnd` 调它 |
| `Assets/_Project/Scripts/Units/UnitStats.cs:27` | 新增 `skillSpeed = 1f`：时长一律 `原值 / max(skillSpeed,>0)`，默认 1 时与原值逐字相同 |

## 6. 为什么这样写（设计取舍）

- **选的写法与理由**：每层先定义「契约」（接口 / 值对象 / 静态门面），调用点只依赖契约，不依赖具体类型；契约层允许零资产改动（`SkillCastContext` 是 struct、`InteractionRunner` 运行时 `AddComponent`、门面是静态转发类），因此不需要碰场景/预制体/`.asset`。
- **放弃的写法与代价**：
  - 把触发器名/交互半径搬进 `.asset`（数据驱动最彻底）→ 必须改资产，长任务明确禁止，且会让「代码对不对」与「资产填没填」纠缠；
  - 给交互物各挂一个 Runner 组件 → 要改预制体；
  - 技能上下文用 `class` + 传可变引用 → 效果可能改到共享状态，值对象（`readonly struct`）把「只读上下文」写进类型。
- **与架构不变量（`Docs/01` §1.4）的关系**：没有新增全局可变静态状态（M2 的门面是既有静态状态的唯一入口）；没有复制核心类，新能力都是「数据 + 组合」（效果链仍是策略模式，新增效果只需继承 + 声明 `Trigger`）；伤害管线入口（`UnitHealth.TakeHit`）未改。
- **测试侧的取舍**：断言只走生产入口（`SkillSystem.Cast` / `TakeHit` / `CommandInteract`），夹具全部运行期内存创建并销毁；为让三条技能断言互不依赖，用生产调试入口 `UnitMana.Refill` / `SkillSystem.ResetCooldowns` 复位（不是改私有字段造状态）。

## 7. 边界 / 失败模式

- **什么情况会坏**：
  - 把时长缩放写成乘法 → `speed=1` 时也变成 2 倍；本次四处全是除法，并用 M3/M4 的 Q/W 时序对比（差 <1ms）兜底。
  - `SetTrigger` 从「前摇开始」挪到「结算帧」→ 动画与判定错位；本次 `SetTrigger(ctx.Trigger)` 位置未动。
  - `ComputeEnd` 抬高爆点 → 爆炸判定的命中集合变化；`FireballProjectile.OnArrive` 未改、SPEC01 12 条全绿。
  - 交互 Runner 抢在技能期间下单 → 技能被打断；`CanAcceptInteract` 门禁 + 第 7 条断言（`HasPending=false`）覆盖。
- **现在怎么挡住**：REFACTOR 15 条（含 4 条 M4 新增：技能打断普攻 / 受击打断技能 / 技能期间交互无效 / 瞄准点）+ SPEC01 12 条；每次改动后跑，红了先分「测试夹具时序」还是「产品行为」。
- **已知未覆盖**：
  - `Bash` 动画过渡只挂在 `Locomotion`，出招中放 Q 会「收招后切 Bash」（资产设计，改它要动控制器）；
  - `BashEffect` 把 `NavMeshAgent.isStopped` 置 true 后无复位点（既有实现，待实机确认影响）；
  - `skillSpeed ≠ 1` 的加速路径没有资产/用例；
  - 场景里没有 `Chest`/`DungeonDoor` 实例，门的 F 交互无法自动目检。

## 8. 答辩 Q&A 5 条

1. **为什么不用另一种做法（数据驱动调触发器/半径）？** 因为那要改 `.asset`，而本任务硬边界是「零资产改动」；用效果的 `virtual Trigger/StateName` 既把字面量集中到一处，又不动序列化数据。
2. **这个改动影响哪些已有系统？** 技能输入（`SkillInputController` 仍调 `Cast(int,Transform)`，签名未变）、近战/远程普攻（`MeleeCombat` 未改）、交互（M3 门禁被 M4 断言复用）、UI（技能冷却读 `CdRemaining` 未变）。改的是「效果内部的准备方式」和「弹道落点」，不是伤害公式与输入层。
3. **新增一个技能 / 敌人 / 楼层要改哪里？** 新技能 = 新建一个 `SkillEffectBase` 派生类（声明 `Trigger/StateName`）+ 建效果资产 + 填 `SkillData`，`SkillSystem` 零改动；新敌人 = 建 `UnitStats` 资产 + 复用 `MonsterBrain`；新楼层 = 走 `RunState`/`AppFlow` 门面，不再直接摸旧静态类。
4. **怎么证明它是对的？证据在哪？** 每层都用同一套 REFACTOR 断言（8→15 条，只增不改口径）+ SPEC01 12 条；M4 证据：`Docs/QA/Evidence/REFACTOR-M4/editor_run1..3/`（15/15 ×3）、`SPEC-01/editor_run32..33/`（12/12 ×2）、`ac7-timing-compare.txt`（与 M3 时序对比）、`git-diff-stat.txt`（零资产改动）。
5. **如果重做一次，会怎么改？** ① 先写「差值断言」（例如「同参数下 Q 的伤害等待时间与基线差 <10ms」）而不是复用同一套绝对值断言；② 技能触发器的过渡关系（`Locomotion→Bash`）在 M4 前先用只读手段列成「动画契约表」写进 TASK，避免断言时点靠猜；③ 测试夹具的「复位/收尾」抽成公共 helper，减少每条断言各自处理 `NavMeshAgent.isStopped` 这类产品遗留状态。

## 9. 名词表

| 名词 | 含义 |
|---|---|
| REFACTOR 套件 | Play 模式下的行为护栏（`Tests/REFACTOR`），每个里程碑只加断言不改口径，M4 后 15 条 |
| SPEC01 | 既有的 12 条规格验收套件，所有里程碑都必须保持 12/12 |
| `SkillCastContext` | 一次技能施放的不可变上下文（槽位/触发器/状态名/目标/施法者/施法者单位/速度） |
| 合并包围盒中心 | 目标全部 `Renderer.bounds` 的并集中心，作为子弹瞄准点/爆点 |
| 门面（facade） | M2 的 `RunState`/`MetaState`/`MessageFeed`/`AppFlow`/`RunFlow`，旧静态类转发到它们，调用点逐步迁移 |
