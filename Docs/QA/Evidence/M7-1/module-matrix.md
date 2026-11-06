# M7-0 模块依赖矩阵（按现目录）

> 由 `audit_scripts.py` 生成；行=引用方，列=被引用方，单元格为**边数**（同一文件/行/类型去重后）。
> 逐边原始记录（含 `文件:行` 与源码片段）见 `edges.csv`。

| 引用方 \ 被引用 | Core | Content | Gameplay/Interaction | UI | Combat | Debug | Items | Player | System | Units | 合计 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| **Core** | (51) | 0 | 0 | 0 | 2 | 0 | 0 | 0 | 0 | 37 | 90 |
| **Content** | 1 | (0) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 1 |
| **Gameplay/Interaction** | 0 | 0 | (7) | 0 | 0 | 0 | 0 | 0 | 0 | 6 | 13 |
| **UI** | 19 | 0 | 0 | (18) | 1 | 0 | 8 | 12 | 11 | 10 | 79 |
| **Combat** | 13 | 9 | 0 | 0 | (39) | 0 | 0 | 0 | 0 | 24 | 85 |
| **Debug** | 5 | 0 | 0 | 0 | 0 | (0) | 0 | 0 | 0 | 3 | 8 |
| **Items** | 0 | 4 | 0 | 0 | 0 | 0 | (0) | 0 | 0 | 0 | 4 |
| **Player** | 15 | 0 | 6 | 2 | 0 | 4 | 0 | (17) | 0 | 31 | 75 |
| **System** | 6 | 2 | 10 | 1 | 0 | 1 | 7 | 0 | (2) | 9 | 38 |
| **Units** | 48 | 10 | 7 | 0 | 13 | 0 | 0 | 1 | 4 | (57) | 140 |

括号内为同组内部引用。

## 每个依赖组的来源文件

### Core → Combat（2 条，2 个来源文件）

- `Assets/_Project/Scripts/Core/GameData.cs`：MeleeCombat@23
- `Assets/_Project/Scripts/Core/MetaState.cs`：MeleeCombat@14

### Core → Units（37 条，5 个来源文件）

- `Assets/_Project/Scripts/Core/GameData.cs`：UnitHealth@23
- `Assets/_Project/Scripts/Core/MetaState.cs`：UnitHealth@14
- `Assets/_Project/Scripts/Core/RunFlow.cs`：DownedRescue@44
- `Assets/_Project/Scripts/Core/UnitRegistry.cs`：UnitHealth@28; UnitHealth@29; UnitHealth@30; UnitHealth@31; UnitHealth@34; UnitHealth@35; UnitHealth@36; UnitHealth@37 …
- `Assets/_Project/Scripts/Core/WaveSpawner.cs`：UnitHealth@24; UnitHealth@66

### Content → Core（1 条，1 个来源文件）

- `Assets/_Project/Scripts/Content/UnitStats.cs`：Team@11

### Gameplay/Interaction → Units（6 条，3 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs`：CharacterBrain@30; CharacterBrain@33; CharacterBrain@36
- `Assets/_Project/Scripts/Gameplay/Interaction/InteractionDefinition.cs`：CharacterBrain@26
- `Assets/_Project/Scripts/Gameplay/Interaction/InteractionRunner.cs`：CharacterBrain@16; CharacterBrain@20

### UI → Core（19 条，6 个来源文件）

- `Assets/_Project/Scripts/UI/CursorManager.cs`：MessageFeed@71; Team@120; Team@150
- `Assets/_Project/Scripts/UI/EscMenuPanel.cs`：AppFlow@12; AppFlow@17; AppFlow@30
- `Assets/_Project/Scripts/UI/HUD.cs`：MessageFeed@27; MessageFeed@29; MessageFeed@48; MessageFeed@50; MetaState@47
- `Assets/_Project/Scripts/UI/InventoryUI.cs`：MessageFeed@124; MessageFeed@134; RunState@133; Team@33; UnitRegistry@33
- `Assets/_Project/Scripts/UI/MainMenuPanel.cs`：AppFlow@16
- `Assets/_Project/Scripts/UI/UIManager.cs`：AppFlow@61; RunFlow@61

### UI → Combat（1 条，1 个来源文件）

- `Assets/_Project/Scripts/UI/HUD.cs`：MeleeCombat@45

### UI → Items（8 条，2 个来源文件）

- `Assets/_Project/Scripts/UI/InventoryUI.cs`：Inventory@16; Inventory@24; Inventory@35; Inventory@42; Inventory@48; Inventory@77
- `Assets/_Project/Scripts/UI/UIManager.cs`：Inventory@116; Inventory@120

### UI → Player（12 条，4 个来源文件）

- `Assets/_Project/Scripts/UI/CursorManager.cs`：Selectable@61; Selectable@104; Selectable@108; Selectable@115; Selectable@116; SelectionManager@147
- `Assets/_Project/Scripts/UI/HUD.cs`：SelectionManager@42; SelectionManager@43
- `Assets/_Project/Scripts/UI/SkillBarUI.cs`：SelectionManager@54; SelectionManager@55
- `Assets/_Project/Scripts/UI/UIManager.cs`：SelectionManager@119; SelectionManager@120

### UI → System（11 条，3 个来源文件）

- `Assets/_Project/Scripts/UI/CursorManager.cs`：Interactable@60; Interactable@103; Interactable@108; Interactable@126; Interactable@127
- `Assets/_Project/Scripts/UI/InventoryUI.cs`：Chest@25; Chest@48
- `Assets/_Project/Scripts/UI/UIManager.cs`：Chest@37; Chest@42; Chest@46; Chest@116

### UI → Units（10 条，3 个来源文件）

- `Assets/_Project/Scripts/UI/CursorManager.cs`：DownedRescue@134; UnitHealth@59; UnitHealth@95; UnitHealth@101; UnitHealth@108; UnitHealth@119; UnitHealth@149
- `Assets/_Project/Scripts/UI/HUD.cs`：UnitHealth@44
- `Assets/_Project/Scripts/UI/SkillBarUI.cs`：SkillSystem@52; SkillSystem@55

### Combat → Core（13 条，6 个来源文件）

- `Assets/_Project/Scripts/Combat/CombatFeedback.cs`：EventBus@14; EventBus@19
- `Assets/_Project/Scripts/Combat/ExplosionFx.cs`：Team@145
- `Assets/_Project/Scripts/Combat/MeleeCombat.cs`：ICharacterCombat@11; MetaState@147; Team@365
- `Assets/_Project/Scripts/Combat/RangedCombat.cs`：ICharacterCombat@6
- `Assets/_Project/Scripts/Combat/SkillEffectFireball.cs`：MessageFeed@28; MessageFeed@30; MessageFeed@32
- `Assets/_Project/Scripts/Combat/SkillEffects.cs`：MessageFeed@30; MessageFeed@32; MessageFeed@34

### Combat → Content（9 条，6 个来源文件）

- `Assets/_Project/Scripts/Combat/DamageInfo.cs`：DamageType@15
- `Assets/_Project/Scripts/Combat/ExplosionFx.cs`：DamageType@146
- `Assets/_Project/Scripts/Combat/MeleeCombat.cs`：DamageType@339; DamageType@366; UnitStats@27; WeaponData@21
- `Assets/_Project/Scripts/Combat/Projectile.cs`：DamageType@68
- `Assets/_Project/Scripts/Combat/RangedCombat.cs`：UnitStats@13
- `Assets/_Project/Scripts/Combat/SkillEffects.cs`：DamageType@71

### Combat → Units（24 条，8 个来源文件）

- `Assets/_Project/Scripts/Combat/DamageInfo.cs`：UnitHealth@13; UnitHealth@14; UnitHealth@27
- `Assets/_Project/Scripts/Combat/ExplosionFx.cs`：UnitHealth@28; UnitHealth@144
- `Assets/_Project/Scripts/Combat/MeleeCombat.cs`：UnitHealth@29; UnitHealth@335; UnitHealth@364
- `Assets/_Project/Scripts/Combat/Projectile.cs`：UnitHealth@8; UnitHealth@9; UnitHealth@43
- `Assets/_Project/Scripts/Combat/RangedCombat.cs`：UnitHealth@15; UnitHealth@201
- `Assets/_Project/Scripts/Combat/SkillCastContext.cs`：SkillSystem@19; SkillSystem@26; UnitHealth@21; UnitHealth@26
- `Assets/_Project/Scripts/Combat/SkillEffectFireball.cs`：UnitHealth@29; UnitHealth@57
- `Assets/_Project/Scripts/Combat/SkillEffects.cs`：MonsterBrain@73; UnitHealth@31; UnitHealth@67; UnitMotion@50; UnitMotion@87

### Debug → Core（5 条，1 个来源文件）

- `Assets/_Project/Scripts/Debug/DebugTestTools.cs`：Team@35; Team@67; Team@95; UnitRegistry@33; UnitRegistry@93

### Debug → Units（3 条，1 个来源文件）

- `Assets/_Project/Scripts/Debug/DebugTestTools.cs`：SkillSystem@111; UnitHealth@66; UnitMana@108

### Items → Content（4 条，2 个来源文件）

- `Assets/_Project/Scripts/Items/ChestItem.cs`：ItemData@8
- `Assets/_Project/Scripts/Items/Inventory.cs`：ItemData@11; ItemData@16; ItemData@20

### Player → Core（15 条，3 个来源文件）

- `Assets/_Project/Scripts/Player/PlayerCommandController.cs`：AppFlow@23
- `Assets/_Project/Scripts/Player/SelectionManager.cs`：AppFlow@89; MessageFeed@123; Team@104; Team@164; Team@227; UnitRegistry@57
- `Assets/_Project/Scripts/Player/SkillInputController.cs`：AppFlow@44; MessageFeed@100; MessageFeed@114; MessageFeed@172; MessageFeed@174; Team@193; Team@219; UnitRegistry@219

### Player → Gameplay/Interaction（6 条，1 个来源文件）

- `Assets/_Project/Scripts/Player/SelectionManager.cs`：IInteractionTarget@152; IInteractionTarget@166; IInteractionTarget@206; IInteractionTarget@216; InteractionPriority@218; InteractionRunner@119

### Player → UI（2 条，2 个来源文件）

- `Assets/_Project/Scripts/Player/PlayerCommandController.cs`：UIManager@24
- `Assets/_Project/Scripts/Player/SkillInputController.cs`：UIManager@45

### Player → Debug（4 条，1 个来源文件）

- `Assets/_Project/Scripts/Player/SelectionManager.cs`：DebugCameraSwitcher@19; DebugCameraSwitcher@39; DebugCameraSwitcher@43; DebugCameraSwitcher@51

### Player → Units（31 条，3 个来源文件）

- `Assets/_Project/Scripts/Player/PlayerCommandController.cs`：CharacterBrain@11; CharacterBrain@16; CharacterBrain@31
- `Assets/_Project/Scripts/Player/SelectionManager.cs`：CharacterBrain@29; CharacterBrain@34; CharacterBrain@75; CharacterBrain@79; CharacterBrain@185; CharacterBrain@194; CharacterBrain@206; CharacterBrain@228 …
- `Assets/_Project/Scripts/Player/SkillInputController.cs`：CharacterBrain@71; CharacterBrain@118; SkillData@145; SkillSystem@33; SkillSystem@163; SkillSystem@166; SkillSystem@170; UnitHealth@36 …

### System → Core（6 条，3 个来源文件）

- `Assets/_Project/Scripts/System/Chest.cs`：MessageFeed@103; RunState@102
- `Assets/_Project/Scripts/System/DungeonDoor.cs`：Team@50; UnitRegistry@50
- `Assets/_Project/Scripts/System/FogOfWar.cs`：Team@84; UnitRegistry@84

### System → Content（2 条，1 个来源文件）

- `Assets/_Project/Scripts/System/Chest.cs`：ItemData@42; ItemData@96

### System → Gameplay/Interaction（10 条，3 个来源文件）

- `Assets/_Project/Scripts/System/Chest.cs`：IInteractionTarget@11; InteractionDefinition@68; InteractionPriority@62
- `Assets/_Project/Scripts/System/DungeonDoor.cs`：IInteractionTarget@9; InteractionPriority@30; InteractionRunner@20; InteractionRunner@22
- `Assets/_Project/Scripts/System/Interactable.cs`：IInteractionTarget@12; IInteractionTarget@14; IInteractionTarget@15

### System → UI（1 条，1 个来源文件）

- `Assets/_Project/Scripts/System/DungeonDoor.cs`：TransitionFader@47

### System → Debug（1 条，1 个来源文件）

- `Assets/_Project/Scripts/System/DungeonDoor.cs`：DebugCameraSwitcher@60

### System → Items（7 条，1 个来源文件）

- `Assets/_Project/Scripts/System/Chest.cs`：ChestItem@34; ChestItem@43; ChestItem@89; ChestItem@98; Inventory@24; Inventory@28; Inventory@32

### System → Units（9 条，3 个来源文件）

- `Assets/_Project/Scripts/System/Chest.cs`：CharacterBrain@49; CharacterBrain@66; CharacterBrain@68; CharacterBrain@71
- `Assets/_Project/Scripts/System/DungeonDoor.cs`：CharacterBrain@34; CharacterBrain@36; CharacterBrain@38
- `Assets/_Project/Scripts/System/SkeletonSpawner.cs`：SkeletonWander@40; Visibility@41

### Units → Core（48 条，11 个来源文件）

- `Assets/_Project/Scripts/Units/BossBrain.cs`：AppFlow@72; MessageFeed@65; MessageFeed@82; RunState@63; RunState@64; Team@133; Team@178; UnitRegistry@178
- `Assets/_Project/Scripts/Units/CharacterBrain.cs`：AppFlow@144; ICharacterCombat@93; Team@23
- `Assets/_Project/Scripts/Units/DownedRescue.cs`：AppFlow@209; MessageFeed@87; MessageFeed@169; MessageFeed@197; MessageFeed@289; MessageFeed@304
- `Assets/_Project/Scripts/Units/MonsterBrain.cs`：AppFlow@119; ICharacterCombat@184; RunState@99; RunState@100; Team@52
- `Assets/_Project/Scripts/Units/SkeletonWander.cs`：Team@62; UnitRegistry@62
- `Assets/_Project/Scripts/Units/SkillData.cs`：Cooldown@14
- `Assets/_Project/Scripts/Units/SkillSystem.cs`：Cooldown@26; Cooldown@30; Cooldown@40; MessageFeed@82; MessageFeed@83; MessageFeed@90
- `Assets/_Project/Scripts/Units/UnitBrain.cs`：ICharacterCombat@105; Team@21; Team@75; UnitRegistry@77
- `Assets/_Project/Scripts/Units/UnitHealth.cs`：EventBus@105; EventBus@111; Team@12; UnitRegistry@43; UnitRegistry@46; UnitRegistry@49; UnitRegistry@51
- `Assets/_Project/Scripts/Units/Visibility.cs`：Team@42; UnitRegistry@42
- `Assets/_Project/Scripts/Units/WorldHealthBar.cs`：Team@40; Team@46; Team@62; UnitRegistry@57

### Units → Content（10 条，5 个来源文件）

- `Assets/_Project/Scripts/Units/BossBrain.cs`：DamageType@134; DamageType@165; UnitStats@12
- `Assets/_Project/Scripts/Units/ShieldedUnitHealth.cs`：DamageType@13
- `Assets/_Project/Scripts/Units/UnitAnimatorSpeed.cs`：UnitStats@11; UnitStats@26
- `Assets/_Project/Scripts/Units/UnitBrain.cs`：UnitStats@16
- `Assets/_Project/Scripts/Units/UnitHealth.cs`：DamageType@64; DamageType@92; UnitStats@11

### Units → Gameplay/Interaction（7 条，2 个来源文件）

- `Assets/_Project/Scripts/Units/CharacterBrain.cs`：IInteractionTarget@71; InteractionRunner@18; InteractionRunner@74
- `Assets/_Project/Scripts/Units/DownedRescue.cs`：IInteractionTarget@12; InteractionDefinition@107; InteractionPriority@101; InteractionRunner@136

### Units → Combat（13 条，5 个来源文件）

- `Assets/_Project/Scripts/Units/CharacterBrain.cs`：DamageInfo@51
- `Assets/_Project/Scripts/Units/DownedRescue.cs`：DamageInfo@200
- `Assets/_Project/Scripts/Units/SkillData.cs`：SkillEffectBase@17
- `Assets/_Project/Scripts/Units/SkillSystem.cs`：MeleeCombat@13; SkillCastContext@67; SkillCastContext@73; SkillEffectBase@67
- `Assets/_Project/Scripts/Units/UnitHealth.cs`：DamageDealtEvent@105; DamageInfo@22; DamageInfo@67; DamageInfo@90; DamageInfo@97; UnitDiedEvent@111

### Units → Player（1 条，1 个来源文件）

- `Assets/_Project/Scripts/Units/DownedRescue.cs`：Selectable@82

### Units → System（4 条，2 个来源文件）

- `Assets/_Project/Scripts/Units/CharacterBrain.cs`：Interactable@64
- `Assets/_Project/Scripts/Units/DownedRescue.cs`：Interactable@45; Interactable@67; Interactable@68
