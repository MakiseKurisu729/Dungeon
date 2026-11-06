# M7-0 分层建议（每个文件唯一目标层）

> 目标层来自 `Docs/01` §3.1/§3.3：Core ← Content ← Gameplay ← App ← UI；Editor 旁挂。
> 现行目录只作线索，分层按**文件实际职责**判定；`Assets/_Project/Tests` 不在本单扫描范围。

| 文件 | 现目录 | 目标层 | 理由 |
|---|---|---|---|
| `Assets/_Project/Scripts/App/AppFlow.cs` | App | **App** | 局流程/整备/成长/存档/组合根 |
| `Assets/_Project/Scripts/App/GameData.cs` | App | **App** | 局流程/整备/成长/存档/组合根 |
| `Assets/_Project/Scripts/App/GameFlow.cs` | App | **App** | 局流程/整备/成长/存档/组合根 |
| `Assets/_Project/Scripts/App/MetaState.cs` | App | **App** | 局流程/整备/成长/存档/组合根 |
| `Assets/_Project/Scripts/App/PauseManager.cs` | App | **App** | 局流程/整备/成长/存档/组合根 |
| `Assets/_Project/Scripts/App/RunFlow.cs` | App | **App** | 局流程/整备/成长/存档/组合根 |
| `Assets/_Project/Scripts/Content/ItemData.cs` | Content | **Content** | ScriptableObject 内容定义，不持有局内运行状态 |
| `Assets/_Project/Scripts/Content/UnitStats.cs` | Content | **Content** | ScriptableObject 内容定义，不持有局内运行状态 |
| `Assets/_Project/Scripts/Content/WeaponData.cs` | Content | **Content** | ScriptableObject 内容定义，不持有局内运行状态 |
| `Assets/_Project/Scripts/Core/Cooldown.cs` | Core | **Core** | 纯数据/契约/事件/时间，无场景与玩法依赖 |
| `Assets/_Project/Scripts/Core/EventBus.cs` | Core | **Core** | 纯数据/契约/事件/时间，无场景与玩法依赖 |
| `Assets/_Project/Scripts/Core/ICharacterCombat.cs` | Core | **Core** | 纯数据/契约/事件/时间，无场景与玩法依赖 |
| `Assets/_Project/Scripts/Core/MessageFeed.cs` | Core | **Core** | 纯数据/契约/事件/时间，无场景与玩法依赖 |
| `Assets/_Project/Scripts/Core/PauseState.cs` | Core | **Core** | 纯数据/契约/事件/时间，无场景与玩法依赖 |
| `Assets/_Project/Scripts/Core/ProgressionState.cs` | Core | **Core** | 纯数据/契约/事件/时间，无场景与玩法依赖 |
| `Assets/_Project/Scripts/Core/RunState.cs` | Core | **Core** | 纯数据/契约/事件/时间，无场景与玩法依赖 |
| `Assets/_Project/Scripts/Core/Team.cs` | Core | **Core** | 纯数据/契约/事件/时间，无场景与玩法依赖 |
| `Assets/_Project/Scripts/Core/UnitId.cs` | Core | **Core** | 纯数据/契约/事件/时间，无场景与玩法依赖 |
| `Assets/_Project/Scripts/Editor/DebugTestTools.cs` | Editor | **Editor** | UNITY_EDITOR 专用，运行时代码不得反向依赖 |
| `Assets/_Project/Scripts/Gameplay/Combat/CombatFeedback.cs` | Gameplay/Combat | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Combat/DamageInfo.cs` | Gameplay/Combat | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Combat/DamageNumber.cs` | Gameplay/Combat | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Combat/ExplosionFx.cs` | Gameplay/Combat | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Combat/FireballProjectile.cs` | Gameplay/Combat | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Combat/HeavySlashEffect.cs` | Gameplay/Combat | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Combat/MeleeCombat.cs` | Gameplay/Combat | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Combat/Projectile.cs` | Gameplay/Combat | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Combat/RangedCombat.cs` | Gameplay/Combat | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Combat/SimplePool.cs` | Gameplay/Combat | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Combat/SkillCastContext.cs` | Gameplay/Combat | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Combat/SkillEffectBase.cs` | Gameplay/Combat | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Combat/SkillEffectFireball.cs` | Gameplay/Combat | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Combat/SkillEffects.cs` | Gameplay/Combat | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs` | Gameplay/Interaction | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Interaction/InteractionDefinition.cs` | Gameplay/Interaction | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Interaction/InteractionRunner.cs` | Gameplay/Interaction | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Items/ChestItem.cs` | Gameplay/Items | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Items/Inventory.cs` | Gameplay/Items | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Player/PlayerCommandController.cs` | Gameplay/Player | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Player/Selectable.cs` | Gameplay/Player | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs` | Gameplay/Player | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Player/SkillIndicator.cs` | Gameplay/Player | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Player/SkillInputController.cs` | Gameplay/Player | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Units/BossBrain.cs` | Gameplay/Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs` | Gameplay/Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Units/DownedRescue.cs` | Gameplay/Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Units/MonsterBrain.cs` | Gameplay/Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Units/ShieldedUnitHealth.cs` | Gameplay/Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Units/SkeletonWander.cs` | Gameplay/Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Units/SkillData.cs` | Gameplay/Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Units/SkillSystem.cs` | Gameplay/Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Units/SleepingSkeleton.cs` | Gameplay/Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Units/UnitAnimatorSpeed.cs` | Gameplay/Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Units/UnitBrain.cs` | Gameplay/Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs` | Gameplay/Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Units/UnitMana.cs` | Gameplay/Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Units/UnitMotion.cs` | Gameplay/Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs` | Gameplay/Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Units/Visibility.cs` | Gameplay/Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Units/WorldHealthBar.cs` | Gameplay/Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/World/Chest.cs` | Gameplay/World | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/World/DebugCameraSwitcher.cs` | Gameplay/World | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/World/DungeonDoor.cs` | Gameplay/World | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/World/FogOfWar.cs` | Gameplay/World | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/World/ForestGenerator.cs` | Gameplay/World | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/World/Interactable.cs` | Gameplay/World | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/World/Mirror.cs` | Gameplay/World | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/World/SkeletonSpawner.cs` | Gameplay/World | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/World/WaveSpawner.cs` | Gameplay/World | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/UI/BasePanel.cs` | UI | **UI** | 面板与视图，只依赖下层公开契约 |
| `Assets/_Project/Scripts/UI/CursorManager.cs` | UI | **UI** | 面板与视图，只依赖下层公开契约 |
| `Assets/_Project/Scripts/UI/EscMenuPanel.cs` | UI | **UI** | 面板与视图，只依赖下层公开契约 |
| `Assets/_Project/Scripts/UI/HUD.cs` | UI | **UI** | 面板与视图，只依赖下层公开契约 |
| `Assets/_Project/Scripts/UI/InventoryUI.cs` | UI | **UI** | 面板与视图，只依赖下层公开契约 |
| `Assets/_Project/Scripts/UI/MainMenuPanel.cs` | UI | **UI** | 面板与视图，只依赖下层公开契约 |
| `Assets/_Project/Scripts/UI/SkillBarUI.cs` | UI | **UI** | 面板与视图，只依赖下层公开契约 |
| `Assets/_Project/Scripts/UI/SlotClickHandler.cs` | UI | **UI** | 面板与视图，只依赖下层公开契约 |
| `Assets/_Project/Scripts/UI/TransitionFader.cs` | UI | **UI** | 面板与视图，只依赖下层公开契约 |
| `Assets/_Project/Scripts/UI/UIManager.cs` | UI | **UI** | 面板与视图，只依赖下层公开契约 |

## 分层统计

- Core: 9 个文件
- Content: 3 个文件
- Gameplay: 50 个文件
- App: 6 个文件
- UI: 10 个文件
- Editor: 1 个文件

## 已知的“目标层与当前职责冲突”文件（M7 前置重构候选）

| 文件 | 现状 | 冲突 | 处理建议 |
|---|---|---|---|
| `Assets/_Project/Scripts/Combat/DamageInfo.cs` | 伤害结果包，引用 `UnitHealth` | 理想 Core，但 Core 不能引用 Gameplay | M7 先留在 Gameplay；后续把 `UnitHealth` 换成 `UnitId`/接口后再下沉 |
| `Assets/_Project/Scripts/Combat/SkillCastContext.cs` | 施法上下文，引用 `SkillSystem`/`UnitHealth` | 同上 | 留在 Gameplay |
| `Assets/_Project/Scripts/Units/SkillData.cs` | ScriptableObject，但持有 `SkillEffectBase[]` | Content 引用 Gameplay | 留在 Gameplay；若要入 Content，先把效果链改成 ID 列表 |
| `Assets/_Project/Scripts/Units/UnitHealth.cs` | 引用 `UnitStats`(Content) 与 `DamageInfo`(Gameplay) | Gameplay 内正常 | 留在 Gameplay |
| `Assets/_Project/Scripts/Core/GameData.cs` | 适配层，转发 RunState/MetaState/MessageFeed/MeleeCombat/UnitHealth | Core 目录里却依赖玩法 | 归 App；M7 后删除或保留为 App 内兼容层 |
| `Assets/_Project/Scripts/Core/UnitRegistry.cs` | 全局单位注册表 | Core 目录里却依赖 `UnitHealth` | 归 Gameplay |
| `Assets/_Project/Scripts/Core/WaveSpawner.cs` | 波次生成 MonoBehaviour | Core 目录里却是玩法组件 | 归 Gameplay |
| `Assets/_Project/Scripts/Debug/DebugCameraSwitcher.cs` | 运行期相机控制 MonoBehaviour | 名为 Debug，实为场景运行组件 | 归 Gameplay；不能进 Editor asmdef |
| `Assets/_Project/Scripts/Debug/Mirror.cs` | 运行期镜面渲染 | 名为 Debug，实为场景表现 | 归 Gameplay |
