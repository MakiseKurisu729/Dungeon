# M7-0 模块依赖矩阵（按现目录）

> 由 `audit_scripts.py` 生成；行=引用方，列=被引用方，单元格为**边数**（同一文件/行/类型去重后）。
> 逐边原始记录（含 `文件:行` 与源码片段）见 `edges.csv`。

| 引用方 \ 被引用 | Core | Units | Combat | Player | System | UI | Items | Framework | Gameplay | Debug | 合计 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| **Core** | (42) | 46 | 2 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 90 |
| **Units** | 29 | (75) | 21 | 1 | 4 | 0 | 0 | 4 | 7 | 0 | 141 |
| **Combat** | 9 | 28 | (48) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 85 |
| **Player** | 10 | 36 | 0 | (17) | 0 | 2 | 0 | 0 | 6 | 4 | 75 |
| **System** | 4 | 11 | 0 | 0 | (2) | 1 | 9 | 0 | 10 | 1 | 38 |
| **UI** | 16 | 13 | 1 | 12 | 11 | (18) | 8 | 0 | 0 | 0 | 79 |
| **Items** | 0 | 0 | 0 | 0 | 0 | 0 | (4) | 0 | 0 | 0 | 4 |
| **Framework** | 0 | 0 | 0 | 0 | 0 | 0 | 0 | (0) | 0 | 0 | 0 |
| **Gameplay** | 0 | 6 | 0 | 0 | 0 | 0 | 0 | 0 | (7) | 0 | 13 |
| **Debug** | 2 | 6 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | (0) | 8 |

括号内为同组内部引用。

## 每个依赖组的来源文件

### Core → Units（46 条，6 个来源文件）

- `Assets/_Project/Scripts/Core/AppFlow.cs`：Team@41
- `Assets/_Project/Scripts/Core/GameData.cs`：UnitHealth@22
- `Assets/_Project/Scripts/Core/MetaState.cs`：UnitHealth@13
- `Assets/_Project/Scripts/Core/RunFlow.cs`：DownedRescue@43; Team@41
- `Assets/_Project/Scripts/Core/UnitRegistry.cs`：Team@179; Team@182; Team@188; Team@208; Team@213; Team@265; Team@270; UnitHealth@27 …
- `Assets/_Project/Scripts/Core/WaveSpawner.cs`：UnitHealth@23; UnitHealth@65

### Core → Combat（2 条，2 个来源文件）

- `Assets/_Project/Scripts/Core/GameData.cs`：MeleeCombat@22
- `Assets/_Project/Scripts/Core/MetaState.cs`：MeleeCombat@13

### Units → Core（29 条，10 个来源文件）

- `Assets/_Project/Scripts/Units/BossBrain.cs`：AppFlow@70; MessageFeed@63; MessageFeed@80; RunState@61; RunState@62; UnitRegistry@176
- `Assets/_Project/Scripts/Units/CharacterBrain.cs`：AppFlow@143
- `Assets/_Project/Scripts/Units/DownedRescue.cs`：AppFlow@208; MessageFeed@86; MessageFeed@168; MessageFeed@196; MessageFeed@288; MessageFeed@303
- `Assets/_Project/Scripts/Units/MonsterBrain.cs`：AppFlow@118; RunState@98; RunState@99
- `Assets/_Project/Scripts/Units/SkeletonWander.cs`：UnitRegistry@61
- `Assets/_Project/Scripts/Units/SkillSystem.cs`：MessageFeed@81; MessageFeed@82; MessageFeed@89
- `Assets/_Project/Scripts/Units/UnitBrain.cs`：UnitRegistry@75
- `Assets/_Project/Scripts/Units/UnitHealth.cs`：EventBus@103; EventBus@109; UnitRegistry@41; UnitRegistry@44; UnitRegistry@47; UnitRegistry@49
- `Assets/_Project/Scripts/Units/Visibility.cs`：UnitRegistry@41
- `Assets/_Project/Scripts/Units/WorldHealthBar.cs`：UnitRegistry@56

### Units → Combat（21 条，9 个来源文件）

- `Assets/_Project/Scripts/Units/BossBrain.cs`：DamageType@132; DamageType@163
- `Assets/_Project/Scripts/Units/CharacterBrain.cs`：DamageInfo@50; ICharacterCombat@92
- `Assets/_Project/Scripts/Units/DownedRescue.cs`：DamageInfo@199
- `Assets/_Project/Scripts/Units/MonsterBrain.cs`：ICharacterCombat@183
- `Assets/_Project/Scripts/Units/ShieldedUnitHealth.cs`：DamageType@12
- `Assets/_Project/Scripts/Units/SkillData.cs`：SkillEffectBase@16
- `Assets/_Project/Scripts/Units/SkillSystem.cs`：MeleeCombat@12; SkillCastContext@66; SkillCastContext@72; SkillEffectBase@66
- `Assets/_Project/Scripts/Units/UnitBrain.cs`：ICharacterCombat@103
- `Assets/_Project/Scripts/Units/UnitHealth.cs`：DamageDealtEvent@103; DamageInfo@20; DamageInfo@65; DamageInfo@88; DamageInfo@95; DamageType@62; DamageType@90; UnitDiedEvent@109

### Units → Player（1 条，1 个来源文件）

- `Assets/_Project/Scripts/Units/DownedRescue.cs`：Selectable@81

### Units → System（4 条，2 个来源文件）

- `Assets/_Project/Scripts/Units/CharacterBrain.cs`：Interactable@63
- `Assets/_Project/Scripts/Units/DownedRescue.cs`：Interactable@44; Interactable@66; Interactable@67

### Units → Framework（4 条，2 个来源文件）

- `Assets/_Project/Scripts/Units/SkillData.cs`：Cooldown@13
- `Assets/_Project/Scripts/Units/SkillSystem.cs`：Cooldown@25; Cooldown@29; Cooldown@39

### Units → Gameplay（7 条，2 个来源文件）

- `Assets/_Project/Scripts/Units/CharacterBrain.cs`：IInteractionTarget@70; InteractionRunner@17; InteractionRunner@73
- `Assets/_Project/Scripts/Units/DownedRescue.cs`：IInteractionTarget@11; InteractionDefinition@106; InteractionPriority@100; InteractionRunner@135

### Combat → Core（9 条，4 个来源文件）

- `Assets/_Project/Scripts/Combat/CombatFeedback.cs`：EventBus@13; EventBus@18
- `Assets/_Project/Scripts/Combat/MeleeCombat.cs`：MetaState@145
- `Assets/_Project/Scripts/Combat/SkillEffectFireball.cs`：MessageFeed@27; MessageFeed@29; MessageFeed@31
- `Assets/_Project/Scripts/Combat/SkillEffects.cs`：MessageFeed@28; MessageFeed@30; MessageFeed@32

### Combat → Units（28 条，8 个来源文件）

- `Assets/_Project/Scripts/Combat/DamageInfo.cs`：UnitHealth@12; UnitHealth@13; UnitHealth@26
- `Assets/_Project/Scripts/Combat/ExplosionFx.cs`：Team@143; UnitHealth@26; UnitHealth@142
- `Assets/_Project/Scripts/Combat/MeleeCombat.cs`：Team@363; UnitHealth@27; UnitHealth@333; UnitHealth@362; UnitStats@25
- `Assets/_Project/Scripts/Combat/Projectile.cs`：UnitHealth@7; UnitHealth@8; UnitHealth@42
- `Assets/_Project/Scripts/Combat/RangedCombat.cs`：UnitHealth@13; UnitHealth@199; UnitStats@11
- `Assets/_Project/Scripts/Combat/SkillCastContext.cs`：SkillSystem@19; SkillSystem@26; UnitHealth@21; UnitHealth@26
- `Assets/_Project/Scripts/Combat/SkillEffectFireball.cs`：UnitHealth@28; UnitHealth@56
- `Assets/_Project/Scripts/Combat/SkillEffects.cs`：MonsterBrain@71; UnitHealth@29; UnitHealth@65; UnitMotion@48; UnitMotion@85

### Player → Core（10 条，3 个来源文件）

- `Assets/_Project/Scripts/Player/PlayerCommandController.cs`：AppFlow@23
- `Assets/_Project/Scripts/Player/SelectionManager.cs`：AppFlow@88; MessageFeed@122; UnitRegistry@56
- `Assets/_Project/Scripts/Player/SkillInputController.cs`：AppFlow@43; MessageFeed@99; MessageFeed@113; MessageFeed@171; MessageFeed@173; UnitRegistry@218

### Player → Units（36 条，3 个来源文件）

- `Assets/_Project/Scripts/Player/PlayerCommandController.cs`：CharacterBrain@11; CharacterBrain@16; CharacterBrain@31
- `Assets/_Project/Scripts/Player/SelectionManager.cs`：CharacterBrain@28; CharacterBrain@33; CharacterBrain@74; CharacterBrain@78; CharacterBrain@184; CharacterBrain@193; CharacterBrain@205; CharacterBrain@227 …
- `Assets/_Project/Scripts/Player/SkillInputController.cs`：CharacterBrain@70; CharacterBrain@117; SkillData@144; SkillSystem@32; SkillSystem@162; SkillSystem@165; SkillSystem@169; Team@192 …

### Player → UI（2 条，2 个来源文件）

- `Assets/_Project/Scripts/Player/PlayerCommandController.cs`：UIManager@24
- `Assets/_Project/Scripts/Player/SkillInputController.cs`：UIManager@44

### Player → Gameplay（6 条，1 个来源文件）

- `Assets/_Project/Scripts/Player/SelectionManager.cs`：IInteractionTarget@151; IInteractionTarget@165; IInteractionTarget@205; IInteractionTarget@215; InteractionPriority@217; InteractionRunner@118

### Player → Debug（4 条，1 个来源文件）

- `Assets/_Project/Scripts/Player/SelectionManager.cs`：DebugCameraSwitcher@18; DebugCameraSwitcher@38; DebugCameraSwitcher@42; DebugCameraSwitcher@50

### System → Core（4 条，3 个来源文件）

- `Assets/_Project/Scripts/System/Chest.cs`：MessageFeed@101; RunState@100
- `Assets/_Project/Scripts/System/DungeonDoor.cs`：UnitRegistry@49
- `Assets/_Project/Scripts/System/FogOfWar.cs`：UnitRegistry@83

### System → Units（11 条，4 个来源文件）

- `Assets/_Project/Scripts/System/Chest.cs`：CharacterBrain@47; CharacterBrain@64; CharacterBrain@66; CharacterBrain@69
- `Assets/_Project/Scripts/System/DungeonDoor.cs`：CharacterBrain@33; CharacterBrain@35; CharacterBrain@37; Team@49
- `Assets/_Project/Scripts/System/FogOfWar.cs`：Team@83
- `Assets/_Project/Scripts/System/SkeletonSpawner.cs`：SkeletonWander@40; Visibility@41

### System → UI（1 条，1 个来源文件）

- `Assets/_Project/Scripts/System/DungeonDoor.cs`：TransitionFader@46

### System → Items（9 条，1 个来源文件）

- `Assets/_Project/Scripts/System/Chest.cs`：ChestItem@32; ChestItem@41; ChestItem@87; ChestItem@96; Inventory@22; Inventory@26; Inventory@30; ItemData@40 …

### System → Gameplay（10 条，3 个来源文件）

- `Assets/_Project/Scripts/System/Chest.cs`：IInteractionTarget@9; InteractionDefinition@66; InteractionPriority@60
- `Assets/_Project/Scripts/System/DungeonDoor.cs`：IInteractionTarget@8; InteractionPriority@29; InteractionRunner@19; InteractionRunner@21
- `Assets/_Project/Scripts/System/Interactable.cs`：IInteractionTarget@12; IInteractionTarget@14; IInteractionTarget@15

### System → Debug（1 条，1 个来源文件）

- `Assets/_Project/Scripts/System/DungeonDoor.cs`：DebugCameraSwitcher@59

### UI → Core（16 条，6 个来源文件）

- `Assets/_Project/Scripts/UI/CursorManager.cs`：MessageFeed@70
- `Assets/_Project/Scripts/UI/EscMenuPanel.cs`：AppFlow@12; AppFlow@17; AppFlow@30
- `Assets/_Project/Scripts/UI/HUD.cs`：MessageFeed@26; MessageFeed@28; MessageFeed@47; MessageFeed@49; MetaState@46
- `Assets/_Project/Scripts/UI/InventoryUI.cs`：MessageFeed@123; MessageFeed@133; RunState@132; UnitRegistry@32
- `Assets/_Project/Scripts/UI/MainMenuPanel.cs`：AppFlow@16
- `Assets/_Project/Scripts/UI/UIManager.cs`：AppFlow@61; RunFlow@61

### UI → Units（13 条，4 个来源文件）

- `Assets/_Project/Scripts/UI/CursorManager.cs`：DownedRescue@133; Team@119; Team@149; UnitHealth@58; UnitHealth@94; UnitHealth@100; UnitHealth@107; UnitHealth@118 …
- `Assets/_Project/Scripts/UI/HUD.cs`：UnitHealth@43
- `Assets/_Project/Scripts/UI/InventoryUI.cs`：Team@32
- `Assets/_Project/Scripts/UI/SkillBarUI.cs`：SkillSystem@52; SkillSystem@55

### UI → Combat（1 条，1 个来源文件）

- `Assets/_Project/Scripts/UI/HUD.cs`：MeleeCombat@44

### UI → Player（12 条，4 个来源文件）

- `Assets/_Project/Scripts/UI/CursorManager.cs`：Selectable@60; Selectable@103; Selectable@107; Selectable@114; Selectable@115; SelectionManager@146
- `Assets/_Project/Scripts/UI/HUD.cs`：SelectionManager@41; SelectionManager@42
- `Assets/_Project/Scripts/UI/SkillBarUI.cs`：SelectionManager@54; SelectionManager@55
- `Assets/_Project/Scripts/UI/UIManager.cs`：SelectionManager@119; SelectionManager@120

### UI → System（11 条，3 个来源文件）

- `Assets/_Project/Scripts/UI/CursorManager.cs`：Interactable@59; Interactable@102; Interactable@107; Interactable@125; Interactable@126
- `Assets/_Project/Scripts/UI/InventoryUI.cs`：Chest@24; Chest@47
- `Assets/_Project/Scripts/UI/UIManager.cs`：Chest@37; Chest@42; Chest@46; Chest@116

### UI → Items（8 条，2 个来源文件）

- `Assets/_Project/Scripts/UI/InventoryUI.cs`：Inventory@15; Inventory@23; Inventory@34; Inventory@41; Inventory@47; Inventory@76
- `Assets/_Project/Scripts/UI/UIManager.cs`：Inventory@116; Inventory@120

### Gameplay → Units（6 条，3 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs`：CharacterBrain@30; CharacterBrain@33; CharacterBrain@36
- `Assets/_Project/Scripts/Gameplay/Interaction/InteractionDefinition.cs`：CharacterBrain@26
- `Assets/_Project/Scripts/Gameplay/Interaction/InteractionRunner.cs`：CharacterBrain@16; CharacterBrain@20

### Debug → Core（2 条，1 个来源文件）

- `Assets/_Project/Scripts/Debug/DebugTestTools.cs`：UnitRegistry@32; UnitRegistry@92

### Debug → Units（6 条，1 个来源文件）

- `Assets/_Project/Scripts/Debug/DebugTestTools.cs`：SkillSystem@110; Team@34; Team@66; Team@94; UnitHealth@65; UnitMana@107
