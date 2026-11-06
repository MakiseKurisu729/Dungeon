# AGENTS.md — 魔石地牢（AI 协作者上下文文档）

> 本文件是人与 AI 协作开发的"唯一真相源"。任何开发前必读。
> 铁律：有利拓展的方案直接用，不图省事；文件夹按"谁用"分，不按"谁先写"。

## 1. 项目概况
- Unity 3D 地牢探索 + RTS 小队操控（类 LoL 操作 + 暗黑探索），KayKit 资源包，单机起步。
- 里程碑：M1 操控层 ✅ 已验收 → M2 体验包 → M3 迷雾系统（核心乐趣验证）。
- 完整《实现方案 v1.0》见项目 docs/（设计决策的最终依据）。

## 2. 目录结构（Assets/_Project/Scripts/）
| 层 | 职责 | 文件 |
|---|---|---|
| Core | 全局流程 | EventBus, GameData, PauseManager, GameFlow, WaveSpawner |
| Units | 单位本体（数据/血蓝/脑子） | Team, UnitStats, UnitHealth, UnitMana, UnitBrain, CharacterBrain, MonsterBrain, SkillData, SkillSystem, WorldHealthBar, UnitAnimatorSpeed |
| Combat | 武器怎么打（阵营无关） | ICharacterCombat, SwordSwing, RangedCombat, Projectile, SimplePool, WeaponData, DamageNumber, PlayerStateMachine(遗留) |
| Player | 玩家的手 | PlayerCommandController, Selectable, SelectionManager, SkillInputController, SkillIndicator |
| Items | 物品 | ItemData, Inventory, ChestItem |
| Systems | 玩法系统 | Chest, Interactable |
| UI | 界面 | HUD, UIManager, InventoryUI, SlotClickHandler, CursorManager, SkillBarUI |
| Framework | 结构体 | Cooldown, DamageInfo(+DamageDealtEvent/UnitDiedEvent), SkillEffectBase(+BashEffect/HeavySlashEffect) |
| Debug | 工具 | DebugCameraSwitcher, Mirror |

## 3. 架构铁律（违反=返工）
1. **动画时钟原则**：凡"动画与代码要同步"（命中帧/放箭帧），代码读动画时钟
   （GetCurrentAnimatorStateInfo(...).normalizedTime 轮询），失败时回退代码时钟。
   判定窗口一律用比例（0~1），禁用秒数。不用 AnimationEvent 做主路径。
2. **查找模式**：逻辑脚本用 GetComponentInChildren<Animator>() 自动找动画器，禁止拖 Animator 槽。
   禁止在根节点写 [RequireComponent(typeof(Animator))]（模型子节点才持 Animator）。
3. **攻速单源**：UnitStats.attackSpeed 是唯一来源 → UnitAnimatorSpeed 写 Animator 参数 "AttackSpeed"
   （攻击状态的 Speed 勾 Parameter）+ 执行器时长/冷却 ÷S。改攻速只改资产。
4. **伤害管线**：DamageInfo 结构体打包上下文（含 source 溯源）→ UnitHealth.TakeHit
   （破防 sharpness vs hardness → 部位倍率 Weak×1.5/Critical×2）→
   C# 事件 OnHit/OnDeath（同域）+ EventBus 事件 DamageDealtEvent/UnitDiedEvent（跨域）。
   表现层（飘字/音效/震屏）只订阅事件，禁止在 UnitHealth 里直接调表现。
5. **技能策略模式**：SkillData.effects 是 SkillEffectBase[] 数组。新增技能效果 = 新建继承类资产，
   SkillSystem 零改动。禁用 switch-case 按枚举分派。
6. **配置纪律**：UnitHealth.Stats 空槽必须 Debug.LogError（防静默变敌队互殴）。
   数据优先级：代码 Awake > 资产 > 场景手改。改数值优先改资产，不改场景序列化值。
7. **UI 咽喉**：所有面板显隐统一走 UIManager，视图组件（如 InventoryUI）不管开关。

## 4. 动画体系约定
- 单位动画切入一律触发器体系；Any State 只用于受击(Hit)/死亡(Die)，
  过渡必须勾掉 Can Transition To Self。
- Skeleton.controller 全骷髅共用（近战 AttackMelee/弩手 AttackRanged 两触发器分派）；
  玩家两职业（Knight/Archer）因状态图拓扑不同而分控制器。
- 状态名与代码中的 attackStateName/drawStateName/releaseStateName 字段严格一致（逐字符）。

## 5. 常见坑（已踩过，勿再踩）
- 模型换皮后动画不播 = FBX 未设 Generic + Create From This Model（Avatar 缺失/错拖别单位的）。
- 移动后攻击失效 = Interrupt 触发器残留 → 出招前必须 ResetTrigger("Interrupt")。
- Locomotion→持械态回程线需三条件（SpeedZ<0.1 且 -0.1<SpeedX<0.1）。
- 满屏生单位 = 生成类槽（Arrow Prefab 等）错拖成单位预制体。
- 场景里 Team 对、运行变 Monster = Stats 资产里的 Team 错。
- Animator Culling Mode 必须 Always Animate（RTS 屏外单位）。
- 射线选择：Selectable 与碰撞体必须在同一支层级链上（GetComponentInParent 只向上）。
- UI 坐标映射：世界空间 UI 的 RectTransform 缩放 0.01；血条填充条轴心 X=0（左对齐扣血）。

## 6. 协作方式
- 大改动先出方案文档（改哪些文件/顺序/验收标准），批准后再动代码。
- 小步提交，每个 commit 只做一件事，信息写清做了什么。
- 全量替换脚本时保持 public 字段名稳定，避免检查器引用断裂。
- 新公开字段必须有 [Tooltip]。
