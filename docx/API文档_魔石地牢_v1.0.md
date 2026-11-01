# 魔石地牢 · 代码 API 文档 v1.0

> 数据源：`Assets/_Project/Scripts/` 全量 47 个脚本（2026-10-31 从工程压缩包提取，纯代码结构分析，未含 private 实现细节）

## 0. 总览

| 层 | 脚本数 |
|---|---|
| Core（全局流程） | 5 |
| Units（单位框架） | 10 |
| Combat（战斗执行） | 10 |
| Player（输入与选择） | 5 |
| Items（物品） | 3 |
| Systems（玩法系统） | 2 |
| UI（界面） | 6 |
| Framework（跨层结构） | 0 |
| Debug（调试） | 2 |


---

## Core（全局流程）

### `EventBus`  `Core/EventBus.cs`

全局事件总线：各系统通过它通信，不互相引用。


### `GameData`  `Core/GameData.cs`

（无类注释）


### `GameFlow`  `Core/GameFlow.cs`

*基类/接口：`MonoBehaviour`*

队伍级流程：玩家单位死亡 → 倒地退出战斗；全员死亡 → 团灭冻结，R 重开。


### `PauseManager`  `Core/PauseManager.cs`

*基类/接口：`MonoBehaviour`*

全局暂停：Esc 切换（背包开着时 Esc 优先关背包，不抢）。timeScale=0 + 暂停面板。


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| GameObject | `pausePanel` | — | — |

**公开方法**

- `void Pause()`
- `void Resume()`

### `WaveSpawner`  `Core/WaveSpawner.cs`

*基类/接口：`MonoBehaviour`*

（无类注释）


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| GameObject[] | `unitPrefabs` | — | — |
| Vector3[] | `offsets` | — | — |
| Wave[] | `waves` | — | — |
| float | `waveDelay` | 3f | — |
| float | `hpScalePerWave` | 0.3f | — |


---

## Units（单位框架）

### `CharacterBrain`  `Units/CharacterBrain.cs`

*基类/接口：`UnitBrain`*

玩家 Driver：移动指令（先取消当前攻击）> 集火 > 坚守自动攻击；交互指令（走到范围→执行）。


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| | **▸ 交互** | | |
| Animator | `unitAnimator` | — | — |
| float | `interactExtraRange` | 0.6f | — |

**公开方法**

- `void CommandAttack(Transform t)`
- `void CommandInteract(Interactable it)`

### `MonsterBrain`  `Units/MonsterBrain.cs`

*基类/接口：`UnitBrain`*

怪物 Driver：攻速缩放（时长+冷却）；Speed 驱动移动动画； 命中窗口读动画时钟（attackStateName 状态 normalizedTime）；触发器化受击/死亡/收招。


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| | **▸ 攻击** | | |
| bool | `useOwnAttackAnim` | true | — |
| string | `attackTrigger` | "AttackMelee" | — |
| string | `attackStateName` | "Attack_Melee" | 攻击状态名（动画时钟读进度用）：近战 Attack_Melee / 弩手 Shoot |
| float | `attackLen` | 0.8f | 攻击状态名（动画时钟读进度用）：近战 Attack_Melee / 弩手 Shoot |
| float | `activeStart` | 0.35f | 攻击状态名（动画时钟读进度用）：近战 Attack_Melee / 弩手 Shoot |
| float | `activeEnd` | 0.65f | — |
| float | `hitRange` | 2.2f | — |
| float | `hitHeight` | 1.5f | — |
| float | `attackCooldown` | 2f | — |
| float | `cancelAttackRangeMult` | 1.6f | — |
| | **▸ 动画参数名** | | |
| string | `speedParam` | "Speed" | — |
| string | `hitTrigger` | "Hit" | — |
| string | `deathTrigger` | "Die" | — |

**公开方法**

- `void ForceHit(Vector3 knockDir, float stagger, float knockDist, float knockDur = 0.3f)`

### `SkillData`  `Units/SkillData.cs`

*基类/接口：`ScriptableObject`*

（无类注释）


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| string | `skillName` | "技能" | — |
| Sprite | `icon` | — | — |
| float | `manaCost` | 20f | — |
| Cooldown | `cooldown` | new Cooldown { duration = 6f } | — |
| SkillEffectBase[] | `effects` | — | 效果链：按顺序执行，可多个叠加 |

### `SkillSystem`  `Units/SkillSystem.c.cs`

*基类/接口：`MonoBehaviour`*

技能系统：QWER 四槽（数据驱动）；验蓝+验冷却（Cooldown 结构）； 效果链执行（策略模式，新增效果不改本类）。


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| UnitMana | `mana` | — | — |
| SwordSwing | `sword` | — | — |
| SkillData[] | `skills` | new SkillData[4] | Q/W/E/R，空槽留空 |
| | **▸ 盾击节奏** | | |
| float | `bashTotalTime` | 0.6f | Q/W/E/R，空槽留空 |
| float | `bashHitTime` | 0.3f | Q/W/E/R，空槽留空 |

**公开方法**

- `SkillData GetSkill(int i)`
- `float CdRemaining(int i)`
- `float CdTotal(int i)`
- `bool Cast(int index, Transform target)`
- `void StartBash(BashEffect eff, Transform target)`

### `UnitAnimatorSpeed`  `Units/UnitAnimatorSpeed.cs`

*基类/接口：`MonoBehaviour`*

把 UnitStats.attackSpeed 写入子节点 Animator 的 "AttackSpeed" 浮点参数。 动画器自动向下查找，根上不需要 Animator 组件。


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| UnitStats | `stats` | — | — |

### `UnitBrain`  `Units/UnitBrain.cs`

*基类/接口：`MonoBehaviour`*

单位大脑基类：导航移动 + 敌对索敌 + 攻击循环。 子类（Driver）只决定"目标从哪来"：玩家=指令，怪物=AI仇恨。 OnCommandMove 为虚方法：子类可在移动前插入逻辑（如玩家单位先收招）。


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| NavMeshAgent | `agent` | — | — |
| UnitStats | `stats` | — | — |
| MonoBehaviour | `combat` | — | — |

**公开方法**

- `void CommandMove(Vector3 point)`
- `void ClearMove()`

### `UnitHealth`  `Units/UnitHealth.cs`

*基类/接口：`MonoBehaviour`*

统一生命：受击管线（破防/部位倍率）；DamageInfo 打包上下文； C# 事件（同域）+ EventBus（跨域：飘字/掉落/统计）。

- 枚举 `BodyPartType`：Normal / Weak / Hard / Critical

**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| UnitStats | `stats` | — | — |
| Team | `team` | Team.Monster | — |
| float | `maxHp` | 100f | — |
| | **▸ 部位** | | |
| BodyPartType | `partType` | BodyPartType.Normal | — |
| float | `hardness` | 5f | — |
| bool | `showDamageNumber` | true | — |

**事件**：`OnHit` / `OnDeath`


**公开方法**

- `bool TakeHit(float rawDamage, float sharpness, DamageType damageType, Vector3 hitPos, UnitHealth source = null)`
- `void ApplyDamage(float amount, Vector3 hitPos, UnitHealth source = null)`
- `void HealFull()`
- `void ScaleMaxHp(float mult)`
- `void AddMaxHp(float v)`

### `UnitMana`  `Units/UnitMana.cs`

*基类/接口：`MonoBehaviour`*

（无类注释）


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| float | `max` | 100f | — |
| float | `regenPerSec` | 10f | — |

**公开方法**

- `bool Consume(float v)`

### `UnitStats`  `Units/UnitStats.cs`

*基类/接口：`ScriptableObject`*

（无类注释）


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| string | `unitName` | "单位" | — |
| Team | `team` | Team.Monster | — |
| float | `maxHp` | 50f | — |
| float | `damage` | 10f | — |
| float | `attackRange` | 2.2f | — |
| float | `preferredRange` | 0f | — |
| float | `aggroRange` | 8f | — |
| float | `moveSpeed` | 4f | — |
| float | `attackSpeed` | 1f | 攻速倍率：1=正常，1.3=快30% |

### `WorldHealthBar`  `Units/WorldHealthBar.cs`

*基类/接口：`MonoBehaviour`*

头顶血蓝条（LoL 式）：世界空间 Canvas，面向相机； 玩家=绿血+蓝条常驻；敌人=红血，仅"可见"时显示（当前逻辑：12 米内有存活玩家单位，迷雾接入后换判定）。


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| UnitHealth | `health` | — | — |
| UnitMana | `mana` | — | — |
| Canvas | `canvas` | — | — |
| Image | `hpFill` | — | — |
| Image | `manaFill` | — | — |
| | **▸ 可见性** | | |
| float | `visibleRange` | 12f | — |
| Color | `playerColor` | new Color(0.2f, 0.9f, 0.3f) | — |
| Color | `enemyColor` | new Color(0.9f, 0.2f, 0.2f) | — |


---

## Combat（战斗执行）

### `CombatFeedback`  `Combat/CombatFeedback.cs`

*基类/接口：`MonoBehaviour`*

（无类注释）


### `DamageNumber`  `Combat/DamageNumber.cs`

*基类/接口：`MonoBehaviour`*

伤害数字：世界空间 Canvas + Text，向上飘、面向相机、渐隐销毁。 用法：DamageNumber.Show(世界坐标, "25", Color.yellow);


### `PlayerStateMachine`  `Combat/PlayerStateMachine.cs`

*基类/接口：`MonoBehaviour`*

（无类注释）

- 枚举 `PlayerState`：Sheathed / // 未拔剑（探索）
    Combat / // 拔剑（战斗）
    Charging / // 蓄力
    Attacking   // 攻击中

**公开方法**

- `void ChangeState(PlayerState next)`

### `Projectile`  `Combat/Projectile.cs`

*基类/接口：`MonoBehaviour`*

（无类注释）


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| UnitHealth | `target` | — | — |
| UnitHealth | `source` | — | — |
| float | `damage` | — | — |
| float | `sharpness` | — | — |
| float | `duration` | 0.7f | — |
| float | `arc` | 1.2f | — |

**公开方法**

- `void Launch(GameObject prefab)`

### `RangedCombat`  `Combat/RangedCombat.cs`

*基类/接口：`MonoBehaviour, ICharacterCombat`*

（无类注释）


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| Transform | `bow` | — | — |
| GameObject | `arrowPrefab` | — | — |
| UnitStats | `stats` | — | — |
| UnitHealth | `health` | — | — |
| string | `attackTrigger` | "Attack" | 与动画器参数一致：玩家弓手填 Attack，骷髅弓手填 AttackRanged |
| | **▸ 动画状态名（用于读取播放进度）** | | |
| string | `drawStateName` | "Attack_Draw" | 与动画器参数一致：玩家弓手填 Attack，骷髅弓手填 AttackRanged |
| string | `releaseStateName` | "Attack_Release" | 与动画器参数一致：玩家弓手填 Attack，骷髅弓手填 AttackRanged |
| float | `attackLen` | 1.0f | — |
| float | `releasePoint` | 0.45f | — |
| float | `damage` | 12f | — |
| float | `sharpness` | 8f | — |
| float | `flightDuration` | 0.7f | — |
| float | `arcHeight` | 1.2f | — |

**公开方法**

- `void AutoAttack(Transform target)`
- `void CancelAttack()`
- `void OnReleaseEvent()`

### `SimplePool`  `Combat/SimplePool.cs`

极简对象池：按预制体分池复用。Get 取（空了自动实例化），Release 还。 消除运行时 Instantiate/Destroy 的卡顿来源——所有频繁生成物（箭/火球/伤害数字）都该走池。


### `SkillEffectBase`  `Combat/SkillEffectBase.cs`

*基类/接口：`ScriptableObject`*

技能效果策略：技能 = 效果组件列表（可叠加/组合）。 新增效果 = 建一个继承类资产，SkillSystem 零改动。


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| bool | `needTarget` | true | 是否需要选定敌方目标 |
| float | `range` | 2.5f | 是否需要选定敌方目标 |

### `BashEffect`  `Combat/SkillEffects.cs`

*基类/接口：`SkillEffectBase`*

（无类注释）


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| float | `stagger` | 0.5f | — |
| float | `knockDist` | 2f | — |
| float | `knockDuration` | 0.3f | — |
| float | `bashLen` | 0.6f | — |

### `SwordSwing`  `Combat/SwordSwing.cs`

*基类/接口：`MonoBehaviour, ICharacterCombat`*

近战执行器：命中窗口读【动画时钟】（UpperBody 层 normalizedTime 轮询）， 与攻速/过渡天然同步；失败回退代码时钟。


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| | **▸ 引用** | | |
| Transform | `sword` | — | — |
| Transform | `shield` | — | — |
| WeaponData | `weapon` | — | — |
| Material | `ringMaterial` | — | — |
| Material | `trailMaterial` | — | — |
| UnitStats | `stats` | — | — |
| UnitHealth | `health` | — | — |
| | **▸ 攻击动画** | | |
| float | `attackLen` | 0.8f | — |
| float | `activeStart` | 0.35f | — |
| float | `activeEnd` | 0.55f | — |
| | **▸ 动画状态名（UpperBody 层）** | | |
| string | `attackStateName` | "Attack" | — |
| string | `skillStateName` | "Skill" | — |
| int | `attackLayer` | 1 | — |
| | **▸ 命中** | | |
| float | `bladeLength` | 1.05f | — |
| float | `hitRadius` | 0.28f | — |
| | **▸ 强化斩（技能用）** | | |
| float | `skillLen` | 1.3f | — |
| float | `skillActiveStart` | 0.5f | — |
| float | `skillActiveEnd` | 0.7f | — |
| | **▸ 伤害** | | |
| float | `strengthBase` | 1f | — |
| float | `strengthDamageFactor` | 0.5f | — |
| float | `dmgMult` | 1f | — |
| | **▸ 调试可视化** | | |
| bool | `showDebug` | true | — |
| float | `trailPersistTime` | 0.25f | — |

**公开方法**

- `void AutoAttack(Transform target)`
- `void CancelAttack()`
- `void DoSkillAttack()`
- `float GetRawDamage()`

### `WeaponData`  `Combat/WeaponData.cs`

*基类/接口：`ScriptableObject`*

（无类注释）

- 枚举 `DamageType`：Slash / Thrust / Blunt
- 枚举 `GripType`：OneHandOnly / TwoHandOnly

**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| | **▸ 基础** | | |
| string | `weaponName` | "铁剑" | — |
| DamageType | `damageType` | DamageType.Slash | — |
| GripType | `grip` | GripType.TwoHandOnly | — |
| | **▸ 数值** | | |
| float | `baseDamage` | 10f | 基础伤害 |
| float | `sharpness` | 8f | 基础伤害 |


---

## Player（输入与选择）

### `PlayerCommandController`  `Player/PlayerCommandController.cs csharp.cs`

*基类/接口：`MonoBehaviour`*

右键指令 + 动画驱动。Animator 自动查找（不做拖线，杜绝槽位错配）。 持械常态层在移动时淡出（手臂自然摆动）。


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| float | `turnSpeed` | 12f | — |
| LayerMask | `groundMask` | ~0 | — |
| | **▸ 持械常态层** | | |
| int | `stanceLayer` | 1 | — |
| float | `stanceBlendSpeed` | 8f | — |

### `Selectable`  `Player/Selectable.cs`

*基类/接口：`MonoBehaviour`*

（无类注释）


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| GameObject | `selectionRing` | — | — |
| int | `hotkeyIndex` | 0 | 1~4 数字键快速选中（0=不参与） |

**公开方法**

- `void SetSelected(bool on)`

### `SelectionManager`  `Player/SelectionManager.cs`

*基类/接口：`MonoBehaviour`*

选择系统：左键=选角色/点敌集火/点交互物下达交互指令/点空地取消； Ctrl+左键加选；Ctrl+A 全选；1~4 按热键选人；反引号清空；瞄准技能时左键让给施法。


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| LayerMask | `clickableLayers` | ~0 | — |

**公开方法**

- `void Register(Selectable s)`
- `void ClearSelection()`
- `void ForEachSelectedBrain(System.Action<CharacterBrain> action)`

### `SkillIndicator`  `Player/SkillIndicator.cs`

*基类/接口：`MonoBehaviour`*

（无类注释）


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| Material | `mat` | — | — |

**公开方法**

- `void Show()`
- `void Hide()`
- `void UpdateVisual(Vector3 center, float range)`

### `SkillInputController`  `Player/SkillInputController.cs`

*基类/接口：`MonoBehaviour`*

技能输入：QWER → 需目标的技能进入瞄准模式（范围圈+指向线）； 左键选定释放 / 右键·Esc 取消；quickCast 快捷施法（后续接设置界面）。


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| bool | `quickCast` | false | 快捷施法：跳过目标指示，直接对最近敌人释放 |
| SkillIndicator | `indicator` | — | 快捷施法：跳过目标指示，直接对最近敌人释放 |
| KeyCode[] | `skillKeys` | { KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R } | 快捷施法：跳过目标指示，直接对最近敌人释放 |


---

## Items（物品）

### `ChestItem`  `Items/ChestItem.cs`

*基类/接口：`MonoBehaviour`*

（无类注释）


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| ItemData | `data` | — | — |

### `Inventory`  `Items/Inventory.cs`

*基类/接口：`MonoBehaviour`*

（无类注释）


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| int | `capacity` | 12 | — |
| List<ItemData> | `items` | — | — |

**事件**：`OnChanged`


**公开方法**

- `int Add(ItemData item)`
- `void RemoveAt(int index)`

### `ItemData`  `Items/ItemData.cs`

*基类/接口：`ScriptableObject`*

（无类注释）


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| string | `itemName` | "物品" | — |
| Sprite | `icon` | — | — |
| int | `goldValue` | — | — |


---

## Systems（玩法系统）

### `Chest`  `System/Chest.cs`

*基类/接口：`MonoBehaviour`*

通用宝箱：由角色交互指令触发（走过去→Interact 动作→开盖+内容物现身+双面板）。


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| Transform | `lid` | — | — |
| float | `openAngle` | -110f | — |
| float | `openDuration` | 0.4f | — |

**公开方法**

- `void OpenBy(CharacterBrain unit)`
- `void Take(ItemData item)`

### `Interactable`  `System/Interactable.cs`

*基类/接口：`MonoBehaviour`*

（无类注释）



---

## UI（界面）

### `CursorManager`  `UI/CursorManager.cs`

*基类/接口：`MonoBehaviour`*

（无类注释）


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| Texture2D | `defaultCursor` | — | — |
| Texture2D | `selectCursor` | — | — |
| Texture2D | `attackCursor` | — | — |
| Texture2D | `interactCursor` | — | — |
| Texture2D | `grabCursor` | — | — |
| Vector2 | `defaultHotspot` | new Vector2(4, 2) | — |
| Vector2 | `selectHotspot` | new Vector2(56, 8) | — |
| Vector2 | `attackHotspot` | new Vector2(56, 8) | — |
| Vector2 | `interactHotspot` | new Vector2(32, 4) | — |
| Vector2 | `grabHotspot` | new Vector2(32, 32) | — |

### `HUD`  `UI/HUD.cs`

*基类/接口：`MonoBehaviour`*

（无类注释）


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| Text | `goldText` | — | — |
| Text | `levelText` | — | — |
| Text | `messageText` | — | — |

### `InventoryUI`  `UI/InventoryUI.cs`

*基类/接口：`MonoBehaviour`*

背包/箱子双面板视图（纯视图：不管显隐，UIManager 负责开关）。 Player Inv 留空时自动找第一个玩家单位（按 Team，不靠物体名）。


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| Inventory | `playerInv` | — | — |
| Transform | `chestGrid` | — | — |
| Transform | `playerGrid` | — | — |
| Sprite | `slotBg` | — | — |

**公开方法**

- `void SetData(Inventory chestInv, Chest chest)`

### `SkillBarUI`  `UI/SkillBarUI.cs`

*基类/接口：`MonoBehaviour`*

（无类注释）


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| Image[] | `slotIcons` | new Image[4] | — |
| Image[] | `cdMasks` | new Image[4] | — |
| Text[] | `keyLabels` | new Text[4] | — |
| Text[] | `costLabels` | new Text[4] | — |
| Color | `enoughMana` | Color.white | — |
| Color | `notEnoughMana` | new Color(1f, 1f, 1f, 0.35f) | — |

### `SlotClickHandler`  `UI/SlotClickHandler.cs`

*基类/接口：`MonoBehaviour, IPointerClickHandler`*

（无类注释）


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| System.Action<PointerEventData> | `onClick` | — | — |

**公开方法**

- `void OnPointerClick(PointerEventData eventData)`

### `UIManager`  `UI/UIManager.cs`

*基类/接口：`MonoBehaviour`*

UI 管理器（唯一咽喉）：所有面板的显示/隐藏只允许经过这里。 每次切换打印调用栈——如果面板被"幽灵"点亮，日志会指认凶手。


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| GameObject | `inventoryRoot` | — | 背包/箱子面板的总根 |

**公开方法**

- `void ShowInventory(Inventory chestInv, Chest chest)`
- `void HideInventory()`


---

## Debug（调试）

### `DebugCameraSwitcher`  `Debug/DebugCameraSwitcher.cs`

*基类/接口：`MonoBehaviour`*

相机系统（RTS 化）：俯视=自由锚点相机 Z/X 旋转、左键拖动平移（allowPan 开关，后续由选择系统接管）、空格跳到焦点、Y 锁定跟随、Esc 暂停在 PauseManager。 V 仍可在四种模式间循环。


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| Transform | `cam` | — | — |
| Transform | `lookTarget` | — | — |
| | **▸ 俯视（默认，RTS 自由相机）** | | |
| Vector3 | `topOffset` | new Vector3(0f, 9f, -5f) | — |
| float | `topPitch` | 55f | — |
| float | `topFov` | 50f | — |
| float | `rotateSpeed` | 120f | Z/X 旋转速度（度/秒，暂停时也可用） |
| float | `panScale` | 0.05f | Z/X 旋转速度（度/秒，暂停时也可用） |
| bool | `allowPan` | true | Z/X 旋转速度（度/秒，暂停时也可用） |
| Transform | `focusTarget` | — | 允许左键拖动平移——第3步选择系统接入后会改为「有选择时自动禁用」 |
| Behaviour | `aimConstraint` | — | 允许左键拖动平移——第3步选择系统接入后会改为「有选择时自动禁用」 |
| | **▸ 第一人称（实验性）** | | |
| Vector3 | `fpLocalPos` | new Vector3(0f, 0.02f, -0.12f) | 空格回中 / Y 锁定跟随的目标（暂时拖骑士，之后由选择系统设置） |
| float | `fpFov` | 72f | — |
| | **▸ 第三人称·后上方** | | |
| Vector3 | `backLocalPos` | new Vector3(0f, 0.55f, -3.2f) | — |
| | **▸ 第三人称·正面** | | |
| Vector3 | `frontLocalPos` | new Vector3(0f, 0.55f, 3f) | — |
| float | `lookHeight` | 0.8f | — |
| | **▸ 第一人称隐藏（PlayerBody 层）** | | |
| string | `bodyLayerName` | "PlayerBody" | — |
| string[] | `hideKeywords` | { "head", "helmet", "visor", "body", "cape", "belt", "torso", "beard" } | — |

**公开方法**

- `void Shake(float duration, float strength)`
- `void SetFocus(Transform t)`

### `Mirror`  `Debug/Mirror.cs`

*基类/接口：`MonoBehaviour`*

简易镜子：把主相机关于镜面的镜像位置/朝向算出来，赋给镜像相机， 镜像相机渲染进 RenderTexture，贴在镜面上。


**检查器字段**

| 类型 | 字段 | 默认 | 说明 |
|---|---|---|---|
| Transform | `playerCam` | — | — |
| Transform | `mirrorCam` | — | — |


---

### `ICharacterCombat`  `Combat/ICharacterCombat.cs`（接口）

角色战斗执行器接口：自动攻击 + 指令打断。近战(SwordSwing)与远程(RangedCombat)均实现。

- void AutoAttack(Transform target) — 大脑/玩家指令触发一次攻击
- void CancelAttack() — 打断当前攻击（移动指令等调用）

---

### `DamageInfo`  `Combat/DamageInfo.cs`（结构体）

伤害数据包：一次伤害结算的全部上下文。

- UnitHealth source — 伤害来源（可为空，箭矢溯源用）
- UnitHealth target — 目标
- DamageType damageType — 伤害类型（Slash/Blunt/Thrust）
- float rawAmount / finalAmount — 原始值 / 结算值
- bool brokeDefense — false=弹刀未破防
- Vector3 hitPos — 命中点（飘字位置）
- 同文件事件：DamageDealtEvent{info}（跨域）、UnitDiedEvent{unit}（跨域）

---

### `Cooldown`  `Framework/Cooldown.cs`（结构体）

冷却计时结构：一处定义，UI/逻辑共用同一数据源。

- float duration — 冷却总时长（从 SkillData 拷贝）
- bool Ready — 是否冷却完毕
- float Remaining — 剩余秒数（技能栏遮罩读它）
- void Trigger() — 开始计时

---

### `Team`  `Units/Team.cs`（枚举）

队伍：索敌、光标、仇恨全部以此为区分。

- Player / Monster

---

## 附录 A · 铁律合规审查（2026-10-31）

| 级别 | 问题 | 位置 | 处理建议 |
|---|---|---|---|
| 🔴 | **文件名与类名不一致**：`SkillSystem.c.cs`（类名 SkillSystem）。Unity 强制脚本文件名=主类名，否则组件绑定失效 | Units/SkillSystem.c.cs | 改名 `SkillSystem.cs` |
| 🔴 | **文件名与类名不一致**：`PlayerCommandController.cs csharp.cs` | Player/ | 改名 `PlayerCommandController.cs` |
| 🟡 | Update() 内每帧 FindObjectsByType（NearestEnemy） | Player/SkillInputController.cs | 加缓存或事件驱动目标列表 |
| ⚪ | Monster/ 空文件夹残留 | Monster/ | 删除 |
| ⚪ | PlayerStateMachine.cs 已无任何消费者（SwordSwing 摘除状态机后） | Combat/ | 删除（将来做脱战回鞘再建） |

**合规确认（已达标项）**：动画时钟原则三执行器全部落实（SwordSwing/RangedCombat/MonsterBrain 均有 ReadAnimProgress + 代码时钟回退）✓；RequireComponent(Animator) 已清除 ✓；技能策略模式无 switch 残留 ✓；对象池（箭矢走 SimplePool）✓；UnitHealth Stats 空槽报错 ✓；无硬编码物体名查找 ✓。

---

## 附录 B · 2026-11-01 修复批注（M3 迷雾期，根因见 git commit）

**新增资产**
- `Art/Shaders/FogOfWarUnlit.shader`：Shader 名 `"Dungeon/FogOfWar"`——URP 最简 Unlit（顶点色直出 + SrcAlpha 混合 + ZWrite Off + Cull Off）。存在理由：URP Particles/Unlit 切透明需 _Surface/_Blend/_SrcBlend/_DstBlend/_ZWrite/关键字整组配对，运行时拼属性不可靠。

**新增脚本（接线清单未覆盖的新系统）**
- `System/FogOfWar.cs`（挂场景"Fog"物体）：LoL 式战争迷雾。Start 时 BuildMesh 生成 resolution×resolution 网格板（顶点色=揭开程度），Update 每 0.1s 对玩家单位 RevealAround（只减不增=永久揭开），材质首选 Dungeon/FogOfWar shader，找不到回退 Particles/Unlit（补全透明参数）。公开字段：mapSize/resolution/revealRadius/edgeSoftness/fogAlpha/revealSpeed。
- `Units/Visibility.cs`（SkeletonSpawner 运行时 AddComponent 到骷髅根）：视野外/被遮挡 → 关 Renderer + Canvas + 冻结 UnitBrain。**公开只读属性 `bool IsHidden`**：本帧隐身状态，WorldHealthBar 读取后不再把隐身单位血条重新点亮（双系统抢 canvas.enabled 的根因修复）。
- `Combat/ExplosionFx.cs`：两段式爆炸（核心闪现→碎片迸出落地消散）。碎片生成即 `Destroy(d, debrisMaxLife)` 引擎级自毁，不依赖本组件存活。

**行为变更（对外可见）**
- `Units/WorldHealthBar.LateUpdate`：visible 计算追加一条——单位带 Visibility 且 IsHidden 时强制隐藏血条（血条与模型严格同生同灭）。
- `Combat/FireballProjectile.OnArrive`：fx 兜底 Destroy 3s→5s（覆盖碎片 4s 寿命），任意爆炸物体 ≤5s 必清。
- `Combat/DamageNumber.Show`：Text 显式指定内置字体 LegacyRuntime.ttf（脚本 AddComponent<Text> 不会触发 Reset 赋字体，原因此字不渲染）。
- `Player/SelectionManager` + `Debug/DebugCameraSwitcher`：选中单位后左键拖动>8px 进入视角平移（`DebugCameraSwitcher.LeftDragPan`，公开 set 属性，SelectionManager 注入），松开不作点选；快速单击仍是点选；技能瞄准中不抢左键。
