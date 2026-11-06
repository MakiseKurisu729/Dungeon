# M4.5 预制体基座 — 组件归属表 + 迁移检查清单

> 用途：所有者亲自做 M4.5（`PlayerUnit_Base` 基座 → 三职业逐个迁移）时的手边材料。
> 依据：`Docs/Handoff/PLAN_REFACTOR-PHASE2.md` §3；读取自 `Assets/_Project/Prefabs/{Knight,Archer,Mage}_Unit.prefab` 与脚本 `.meta` 的 GUID 映射（2026-11-06，HEAD `f9472f4f`）。
> **资产归所有者**：本文件只是清单，AI 未改任何预制体。

## 0. 现状速览

| 项 | Knight_Unit | Archer_Unit | Mage_Unit |
|---|---|---|---|
| 预制体类型 | 普通预制体（根 `Knight_Unit`） | **Variant of `Knight_Unit`** | **Variant of `Knight_Unit`** |
| 嵌套模型 | `Knight.fbx` | 弓箭手模型（variant 覆盖） | 法师模型（variant 覆盖） |
| 嵌套武器 | `sword_1handed.fbx`、`shield_round.fbx` | 弓（variant 增删） | 法杖（variant 增删） |
| 嵌套血条 | `UnitHealthBar.prefab` | 继承 | 继承 |
| Animator Controller | `OC_Knight.overrideController`（基底 `PlayerBase.controller`） | `OC_Archer.overrideController` | `OC_Mage.overrideController` |
| 变体增删 | `m_AddedComponents` 4 / `m_AddedGameObjects` 3 / `m_RemovedComponents` 4 | 1 / 1 / 2 | 2 / 1 / 2 |
| 变体覆盖字段 | — | `m_Name`、`animator`/`unitAnimator`、`sword`/`shield`、`skills[0..1]`、`stats`、`combat`、`selectionRing` | 另加 `hotkeyIndex`、`m_Controller`（Archer 的 controller 通过 Animator 覆盖，Knight 继承基底） |

> ⚠️ `Archer`/`Mage` 直接变体自 `Knight_Unit`，所以 `Knight_Unit` 上的 `Interactable`（救援用）会被三职业**一起继承**。拆基座前必须逐组件定归属，否则「救援交互」会跟基座一起漂移。

## 1. 组件归属表

### 1.1 单位公共（所有单位都可能持有，含怪物）

| 组件 | 现挂位置 | 归属理由 | 进基座 |
|---|---|---|---|
| `Transform` / 根 `GameObject` | 根 | 必需 | ✅ |
| `CharacterController` | 根 | 移动/碰撞体（**不动尺寸**，M4 明确不做物理化） | ✅ |
| `NavMeshAgent` | 根 | 导航；`UnitMotion` 只改 `isStopped`，不改路径数据 | ✅ |
| `Animator` | 模型子物体 | 动画；控制器按职业覆盖 | ✅（引用留空/由变体覆盖） |
| `UnitHealth` | 根 | 生命 + `stats` + `team`；`UnitRegistry` 的注册根 | ✅ |
| `UnitAnimatorSpeed` | 根 | 把 `NavMeshAgent.velocity` 写进 Animator 速度参数 | ✅ |
| `UnitHealthBar`（嵌套预制体） | 子物体 | 世界血条 | ✅ |
| `Inventory` | 根 | 单位背包（宝箱/角色共用组件类型） | ⚠️ 见 1.3 |

### 1.2 玩家公共（只有三职业需要，怪物不需要）

| 组件 | 现挂位置 | 归属理由 | 进基座 |
|---|---|---|---|
| `CharacterBrain` | 根 | 玩家 Driver（指令/自动攻击/交互）；`InteractionRunner` 运行时挂它身上 | ✅ |
| `PlayerCommandController` | 根 | 右键移动/攻击输入入口 | ✅ |
| `Selectable` | 根 | 选中环 + `hotkeyIndex`（**序号是职业差异，留在变体**） | ✅（环引用留空） |
| `DownedRescue` | 根 | 倒地/救援状态机（含 `Interactable` 引用） | ✅ |
| `Interactable` | 根 | 「点击救援倒地者」的命中标记 + `hint` | ✅（见坑 4.1） |
| `UnitMana` | 根 | 蓝量（技能消耗） | ✅ |
| `SkillSystem` | 根 | 四槽技能 + `sword` 引用 + `skills[]` 资产 | ✅（`skills[]` 留空由变体填） |
| `BasicRigidBodyPush`（StarterAssets） | 根 | 历史遗留的推动组件，玩家专用 | ⚠️ 先确认是否仍在用（见坑 4.5） |

### 1.3 职业专属（不要进基座）

| 组件 / 引用 | Knight | Archer | Mage | 处理 |
|---|---|---|---|---|
| `MeleeCombat` | ✅（剑盾，`attackMode=SingleTarget`） | 变体移除/替换 | 变体移除/替换 | 留各自变体 |
| `RangedCombat` | — | 变体新增 | 变体新增 | 留各自变体 |
| `sword` / `shield` / 弓 / 法杖 引用 | 剑+盾 | 弓 | 法杖 | 留各自变体（模型/武器是嵌套预制体实例） |
| `UnitStats` 资产 | Knight 的 stats | Archer 的 stats | Mage 的 stats | 留各自变体（`stats` 字段） |
| `skills[0..3]` 技能资产 | Knight 的 Q/W 效果 | Archer 的 | Mage 的 | 留各自变体 |
| `Selectable.hotkeyIndex` | 1 | 2 | 3（变体覆盖） | 留各自变体 |
| Animator Controller | `OC_Knight` | `OC_Archer` | `OC_Mage` | 留各自变体（基底 `PlayerBase.controller`） |
| 模型 / 武器嵌套实例 | `Knight.fbx` + sword + shield | 对应模型 | 对应模型 | 留各自变体 |
| `Inventory` | 有 | 继承 | 继承 | 见坑 4.2 |

> `Inventory` 的归属要先定：宝箱（`Chest` 需要 `Inventory`）与角色共用同一组件类型。若基座带 `Inventory`，则三职业都能被当作「容器」被宝箱逻辑误用；建议**基座不带**，谁需要谁挂（当前三职业都挂了 `Inventory`，拆基座时按「玩家公共」保留即可，不要顺手删）。

## 2. 迁移检查清单（每迁一个职业跑一遍）

> 顺序：建 `PlayerUnit_Base`（只放 §1.1 + §1.2）→ `Knight_Unit` 改挂基座 → 验证 → `Archer` → 验证 → `Mage` → 验证。**一次只动一个职业。**

### 2.1 建基座前

- [ ] 复制一份 `Knight_Unit.prefab` 作对照（或先 `git status` 确认工作区干净，出错能丢弃）。
- [ ] 记录三份 `m_AddedComponents` / `m_RemovedComponents` / `m_Modifications` 现状（本文件 §0 已列数量，细节以打开预制体为准）。
- [ ] 记录三个 `OC_*` 各自覆盖了哪些 Clip（尤其 Q/W → `Q_1`/`W_1`）。

### 2.2 建 `PlayerUnit_Base`

- [ ] 只放 §1.1 + §1.2 的组件；**不放** `MeleeCombat`/`RangedCombat`、武器引用、`UnitStats`、`skills[]`、`OC_*`、模型与武器嵌套实例。
- [ ] `UnitHealth.stats`、`Selectable.selectionRing`、`DownedRescue` 的 `health/brain/agent/animator` 等**自引用留空**（运行时/子层自取），避免基座指向某个职业的资源。
- [ ] `CharacterController` 的半径/高度/中心与当前 `Knight_Unit` 一致（**不许顺手改**）。
- [ ] 保存后跑一次 `Tools/QA/In-Editor/REFACTOR` + `SPEC01 (green)`，确认基座本身没破坏任何行为（此时三职业还没挂上去）。

### 2.3 逐职业迁移（Knight → Archer → Mage，每个都走完再下一个）

- [ ] 模型：确认嵌套模型预制体实例仍在、缩放/朝向/根节点位置与迁移前一致。
- [ ] `UnitStats`：引用指向该职业的 stats 资产（不是基座或别人的）。
- [ ] `OC_*.overrideController`：Animator 的 controller 仍是该职业的 OC，基底仍是 `PlayerBase.controller`。
- [ ] 武器引用：`MeleeCombat.sword/shield`、`RangedCombat` 的弹丸/发射点全部指向本职业对象。
- [ ] `Interactable`：确认仍存在且 `hint` 未被清空（救援提示）；`DownedRescue` 能在 `Start` 里找到它。
- [ ] `DownedRescue`：`dieTrigger/reviveTrigger` 仍为 `Die`/`Revive`；`rescueTime/rescueRange/reviveHp/riseDuration` 与迁移前一致。
- [ ] 动画参数：Animator 里 `Q/W/E/R` 触发器、`SkillSpeed`(Float)、`Hit`/`Interrupt`/`Die`/`Revive`/`Interact` 参数仍在（M4.5 只拖 Clip，不删参数）。
- [ ] `Selectable.hotkeyIndex`：Knight=1、Archer=2、Mage=3（与 `SelectionManager` 的 1~4 键对应）。
- [ ] `SkillSystem.sword`：骑士指向 `MeleeCombat`；弓/法若走 `RangedCombat`，确认 `HeavySlashEffect`（W）与 `SkillSystem.sword` 的关系仍成立（W 依赖 `sword.DoSkillAttack`，若该职业没有 `MeleeCombat` 会 `CanCast=false`——这是现状，迁移时不要改变）。
- [ ] 迁移完立刻跑：`Tools/QA/In-Editor/REFACTOR`（16/16）+ `SPEC01 (green)`（12/12）；任一红 → 停，回滚该职业。

### 2.4 迁移后目检（一次性）

- [ ] 三职业各按一次 Q / W：Animator 进入 `Q_1` / `W_1`（不是 `Locomotion`）。
- [ ] 三职业各走一次：移动、普攻、受击打断、倒地、救援读条、救起。
- [ ] 放技能时单位停住；技能结束/受击打断/倒地复活后**还能走**（`UnitMotion` 已覆盖，但要实机确认）。
- [ ] 选中环、血条、蓝量、金币/等级 HUD、开宝箱面板只弹一次。
- [ ] Console 无 `Missing Script` / `Missing Reference`（基座最容易在这里翻车）。

## 3. 已知坑

1. **`Interactable` 会被三职业一起继承**：它现在是「点击救援」的命中标记，不是「单位一定可被救援」。若将来出现「NPC/召唤物也用变体」的情况，需要在变体上移除该组件。
2. **`Inventory` 双语义**：宝箱与角色共用；迁移时不要把角色上的 `Inventory` 当成「宝箱专用」删掉（`InventoryUI` 绑玩家背包要用）。
3. **`CharacterController` 与 `NavMeshAgent` 都在根**：基座化后两者的半径/高度/速度参数**按原值冻结**，改尺寸等于改玩法。
4. **`SkillSystem.sword` 是 `MeleeCombat` 类型**：弓/法把 `MeleeCombat` 移除后，`HeavySlashEffect.CanCast` 返回 false（W 对这些职业本来就不可用）。迁移时**不要**为了「让 W 能放」给弓/法补 `MeleeCombat`。
5. **`BasicRigidBodyPush`（StarterAssets）来源可疑**：属第三方 StarterAssets，功能是推刚体。迁移前先确认场景里是否还需要它；不需要就**单独一次提交**移除，不要混进基座重构。
6. **Variant → 基座的覆盖重放**：`Archer`/`Mage` 现在是 `Knight_Unit` 的变体。改挂 `PlayerUnit_Base` 时，Knight 上已有的覆盖（模型、武器、stats、skills、OC）必须显式重放到各自变体上，否则会静默回落到基座默认值（表现为「技能没了/武器没了/血条数值不对」）。
7. **Q/W Clip 拖拽**：`OC_Knight/Archer/Mage` 里的 `Q_1`/`W_1` 目前是占位 Clip；拖完再跑一次 REFACTOR + SPEC01（M4.5 的验收口径不改成「Clip 对不对」，只要求行为断言仍绿）。

## 4. 一句话交接

基座只装「所有玩家都一样的骨头与器官」（`CharacterController`/`NavMeshAgent`/`Animator`/`UnitHealth`/`UnitAnimatorSpeed`/`CharacterBrain`/`PlayerCommandController`/`Selectable`/`DownedRescue`/`Interactable`/`UnitMana`/`SkillSystem`/血条），**职业差异（模型、武器、stats、skills、OC、hotkey）留在变体**；一次只迁一个职业，迁完立刻跑 REFACTOR + SPEC01。
