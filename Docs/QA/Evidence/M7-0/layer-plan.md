# M7-0 分层建议（每个文件唯一目标层）

> 目标层来自 `Docs/01` §3.1/§3.3：Core ← Content ← Gameplay ← App ← UI；Editor 旁挂。
> 现行目录只作线索，分层按**文件实际职责**判定；`Assets/_Project/Tests` 不在本单扫描范围。

| 文件 | 现目录 | 目标层 | 理由 |
|---|---|---|---|
| `Assets/_Project/Scripts/Combat/CombatFeedback.cs` | Combat | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Combat/DamageInfo.cs` | Combat | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Combat/DamageNumber.cs` | Combat | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Combat/ExplosionFx.cs` | Combat | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Combat/FireballProjectile.cs` | Combat | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Combat/HeavySlashEffect.cs` | Combat | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Combat/ICharacterCombat.cs` | Combat | **Core** | 纯数据/契约/事件/时间，无场景与玩法依赖 |
| `Assets/_Project/Scripts/Combat/MeleeCombat.cs` | Combat | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Combat/Projectile.cs` | Combat | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Combat/RangedCombat.cs` | Combat | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Combat/SimplePool.cs` | Combat | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Combat/SkillCastContext.cs` | Combat | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Combat/SkillEffectBase.cs` | Combat | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Combat/SkillEffectFireball.cs` | Combat | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Combat/SkillEffects.cs` | Combat | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Combat/WeaponData.cs` | Combat | **Content** | ScriptableObject 内容定义，不持有局内运行状态 |
| `Assets/_Project/Scripts/Core/AppFlow.cs` | Core | **App** | 局流程/整备/成长/存档/组合根 |
| `Assets/_Project/Scripts/Core/EventBus.cs` | Core | **Core** | 纯数据/契约/事件/时间，无场景与玩法依赖 |
| `Assets/_Project/Scripts/Core/GameData.cs` | Core | **App** | 局流程/整备/成长/存档/组合根 |
| `Assets/_Project/Scripts/Core/GameFlow.cs` | Core | **App** | 局流程/整备/成长/存档/组合根 |
| `Assets/_Project/Scripts/Core/MessageFeed.cs` | Core | **Core** | 纯数据/契约/事件/时间，无场景与玩法依赖 |
| `Assets/_Project/Scripts/Core/MetaState.cs` | Core | **App** | 局流程/整备/成长/存档/组合根 |
| `Assets/_Project/Scripts/Core/PauseManager.cs` | Core | **App** | 局流程/整备/成长/存档/组合根 |
| `Assets/_Project/Scripts/Core/RunFlow.cs` | Core | **App** | 局流程/整备/成长/存档/组合根 |
| `Assets/_Project/Scripts/Core/RunState.cs` | Core | **Core** | 纯数据/契约/事件/时间，无场景与玩法依赖 |
| `Assets/_Project/Scripts/Core/UnitId.cs` | Core | **Core** | 纯数据/契约/事件/时间，无场景与玩法依赖 |
| `Assets/_Project/Scripts/Core/UnitRegistry.cs` | Core | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Core/WaveSpawner.cs` | Core | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Debug/DebugCameraSwitcher.cs` | Debug | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Debug/DebugTestTools.cs` | Debug | **Editor** | UNITY_EDITOR 专用，运行时代码不得反向依赖 |
| `Assets/_Project/Scripts/Debug/Mirror.cs` | Debug | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Framework/Cooldown.cs` | Framework | **Core** | 纯数据/契约/事件/时间，无场景与玩法依赖 |
| `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs` | Gameplay | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Interaction/InteractionDefinition.cs` | Gameplay | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Gameplay/Interaction/InteractionRunner.cs` | Gameplay | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Items/ChestItem.cs` | Items | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Items/Inventory.cs` | Items | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Items/ItemData.cs` | Items | **Content** | ScriptableObject 内容定义，不持有局内运行状态 |
| `Assets/_Project/Scripts/Player/PlayerCommandController.cs` | Player | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Player/Selectable.cs` | Player | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Player/SelectionManager.cs` | Player | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Player/SkillIndicator.cs` | Player | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Player/SkillInputController.cs` | Player | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/System/Chest.cs` | System | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/System/DungeonDoor.cs` | System | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/System/FogOfWar.cs` | System | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/System/ForestGenerator.cs` | System | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/System/Interactable.cs` | System | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/System/SkeletonSpawner.cs` | System | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
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
| `Assets/_Project/Scripts/Units/BossBrain.cs` | Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Units/CharacterBrain.cs` | Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Units/DownedRescue.cs` | Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Units/MonsterBrain.cs` | Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Units/ShieldedUnitHealth.cs` | Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Units/SkeletonWander.cs` | Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Units/SkillData.cs` | Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Units/SkillSystem.cs` | Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Units/SleepingSkeleton.cs` | Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Units/Team.cs` | Units | **Core** | 纯数据/契约/事件/时间，无场景与玩法依赖 |
| `Assets/_Project/Scripts/Units/UnitAnimatorSpeed.cs` | Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Units/UnitBrain.cs` | Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Units/UnitHealth.cs` | Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Units/UnitMana.cs` | Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Units/UnitMotion.cs` | Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Units/UnitStats.cs` | Units | **Content** | ScriptableObject 内容定义，不持有局内运行状态 |
| `Assets/_Project/Scripts/Units/Visibility.cs` | Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |
| `Assets/_Project/Scripts/Units/WorldHealthBar.cs` | Units | **Gameplay** | 运行时玩法（单位/战斗/交互/AI/世界） |

## 分层统计

- Core: 7 个文件
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
