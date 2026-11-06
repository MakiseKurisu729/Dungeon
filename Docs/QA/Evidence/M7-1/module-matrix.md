# M7-0 模块依赖矩阵（按现目录）

> 由 `audit_scripts.py` 生成；行=引用方，列=被引用方，单元格为**边数**（同一文件/行/类型去重后）。
> 逐边原始记录（含 `文件:行` 与源码片段）见 `edges.csv`。

| 引用方 \ 被引用 | Core | Content | Gameplay/Units | Gameplay/Combat | Gameplay/Interaction | Gameplay/World | Gameplay/Player | Gameplay/Items | App | UI | Debug | 合计 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| **Core** | (0) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| **Content** | 1 | (0) | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 1 |
| **Gameplay/Units** | 52 | 10 | (98) | 13 | 7 | 4 | 1 | 0 | 4 | 0 | 0 | 189 |
| **Gameplay/Combat** | 12 | 9 | 24 | (39) | 0 | 0 | 0 | 0 | 1 | 0 | 0 | 85 |
| **Gameplay/Interaction** | 0 | 0 | 6 | 0 | (7) | 0 | 0 | 0 | 0 | 0 | 0 | 13 |
| **Gameplay/World** | 6 | 2 | 13 | 0 | 10 | (3) | 0 | 7 | 0 | 1 | 0 | 42 |
| **Gameplay/Player** | 10 | 0 | 33 | 0 | 6 | 4 | (17) | 0 | 3 | 2 | 0 | 75 |
| **Gameplay/Items** | 0 | 4 | 0 | 0 | 0 | 0 | 0 | (0) | 0 | 0 | 0 | 4 |
| **App** | 15 | 0 | 5 | 2 | 0 | 0 | 0 | 0 | (15) | 0 | 0 | 37 |
| **UI** | 11 | 0 | 11 | 1 | 0 | 11 | 12 | 8 | 7 | (18) | 0 | 79 |
| **Debug** | 3 | 0 | 5 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | (0) | 8 |

括号内为同组内部引用。

## 每个依赖组的来源文件

### Content → Core（1 条，1 个来源文件）

- `Assets/_Project/Scripts/Content/UnitStats.cs`：Team@11

### Gameplay/Units → Core（52 条，12 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Units/BossBrain.cs`：MessageFeed@67; MessageFeed@84; RunState@65; RunState@66; Team@135; Team@180
- `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`：ICharacterCombat@98; Team@28
- `Assets/_Project/Scripts/Gameplay/Units/DownedRescue.cs`：MessageFeed@93; MessageFeed@175; MessageFeed@203; MessageFeed@295; MessageFeed@310
- `Assets/_Project/Scripts/Gameplay/Units/MonsterBrain.cs`：ICharacterCombat@186; RunState@101; RunState@102; Team@54
- `Assets/_Project/Scripts/Gameplay/Units/SkeletonWander.cs`：Team@63
- `Assets/_Project/Scripts/Gameplay/Units/SkillData.cs`：Cooldown@16
- `Assets/_Project/Scripts/Gameplay/Units/SkillSystem.cs`：Cooldown@28; Cooldown@32; Cooldown@42; MessageFeed@84; MessageFeed@85; MessageFeed@92
- `Assets/_Project/Scripts/Gameplay/Units/UnitBrain.cs`：ICharacterCombat@106; Team@22; Team@76
- `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`：EventBus@107; EventBus@113; Team@14
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs`：Team@181; Team@184; Team@190; Team@210; Team@215; Team@267; Team@272; UnitId@30 …
- `Assets/_Project/Scripts/Gameplay/Units/Visibility.cs`：Team@43
- `Assets/_Project/Scripts/Gameplay/Units/WorldHealthBar.cs`：Team@41; Team@47; Team@63

### Gameplay/Units → Content（10 条，5 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Units/BossBrain.cs`：DamageType@136; DamageType@167; UnitStats@14
- `Assets/_Project/Scripts/Gameplay/Units/ShieldedUnitHealth.cs`：DamageType@14
- `Assets/_Project/Scripts/Gameplay/Units/UnitAnimatorSpeed.cs`：UnitStats@12; UnitStats@27
- `Assets/_Project/Scripts/Gameplay/Units/UnitBrain.cs`：UnitStats@17
- `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`：DamageType@66; DamageType@94; UnitStats@13

### Gameplay/Units → Gameplay/Combat（13 条，5 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`：DamageInfo@56
- `Assets/_Project/Scripts/Gameplay/Units/DownedRescue.cs`：DamageInfo@206
- `Assets/_Project/Scripts/Gameplay/Units/SkillData.cs`：SkillEffectBase@19
- `Assets/_Project/Scripts/Gameplay/Units/SkillSystem.cs`：MeleeCombat@15; SkillCastContext@69; SkillCastContext@75; SkillEffectBase@69
- `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`：DamageDealtEvent@107; DamageInfo@24; DamageInfo@69; DamageInfo@92; DamageInfo@99; UnitDiedEvent@113

### Gameplay/Units → Gameplay/Interaction（7 条，2 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`：IInteractionTarget@76; InteractionRunner@23; InteractionRunner@79
- `Assets/_Project/Scripts/Gameplay/Units/DownedRescue.cs`：IInteractionTarget@18; InteractionDefinition@113; InteractionPriority@107; InteractionRunner@142

### Gameplay/Units → Gameplay/World（4 条，2 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`：Interactable@69
- `Assets/_Project/Scripts/Gameplay/Units/DownedRescue.cs`：Interactable@51; Interactable@73; Interactable@74

### Gameplay/Units → Gameplay/Player（1 条，1 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Units/DownedRescue.cs`：Selectable@88

### Gameplay/Units → App（4 条，4 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Units/BossBrain.cs`：AppFlow@74
- `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`：AppFlow@149
- `Assets/_Project/Scripts/Gameplay/Units/DownedRescue.cs`：AppFlow@215
- `Assets/_Project/Scripts/Gameplay/Units/MonsterBrain.cs`：AppFlow@121

### Gameplay/Combat → Core（12 条，6 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Combat/CombatFeedback.cs`：EventBus@15; EventBus@20
- `Assets/_Project/Scripts/Gameplay/Combat/ExplosionFx.cs`：Team@147
- `Assets/_Project/Scripts/Gameplay/Combat/MeleeCombat.cs`：ICharacterCombat@14; Team@368
- `Assets/_Project/Scripts/Gameplay/Combat/RangedCombat.cs`：ICharacterCombat@8
- `Assets/_Project/Scripts/Gameplay/Combat/SkillEffectFireball.cs`：MessageFeed@30; MessageFeed@32; MessageFeed@34
- `Assets/_Project/Scripts/Gameplay/Combat/SkillEffects.cs`：MessageFeed@32; MessageFeed@34; MessageFeed@36

### Gameplay/Combat → Content（9 条，6 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Combat/DamageInfo.cs`：DamageType@17
- `Assets/_Project/Scripts/Gameplay/Combat/ExplosionFx.cs`：DamageType@148
- `Assets/_Project/Scripts/Gameplay/Combat/MeleeCombat.cs`：DamageType@342; DamageType@369; UnitStats@30; WeaponData@24
- `Assets/_Project/Scripts/Gameplay/Combat/Projectile.cs`：DamageType@70
- `Assets/_Project/Scripts/Gameplay/Combat/RangedCombat.cs`：UnitStats@15
- `Assets/_Project/Scripts/Gameplay/Combat/SkillEffects.cs`：DamageType@73

### Gameplay/Combat → Gameplay/Units（24 条，8 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Combat/DamageInfo.cs`：UnitHealth@15; UnitHealth@16; UnitHealth@29
- `Assets/_Project/Scripts/Gameplay/Combat/ExplosionFx.cs`：UnitHealth@30; UnitHealth@146
- `Assets/_Project/Scripts/Gameplay/Combat/MeleeCombat.cs`：UnitHealth@32; UnitHealth@338; UnitHealth@367
- `Assets/_Project/Scripts/Gameplay/Combat/Projectile.cs`：UnitHealth@10; UnitHealth@11; UnitHealth@45
- `Assets/_Project/Scripts/Gameplay/Combat/RangedCombat.cs`：UnitHealth@17; UnitHealth@203
- `Assets/_Project/Scripts/Gameplay/Combat/SkillCastContext.cs`：SkillSystem@21; SkillSystem@28; UnitHealth@23; UnitHealth@28
- `Assets/_Project/Scripts/Gameplay/Combat/SkillEffectFireball.cs`：UnitHealth@31; UnitHealth@59
- `Assets/_Project/Scripts/Gameplay/Combat/SkillEffects.cs`：MonsterBrain@75; UnitHealth@33; UnitHealth@69; UnitMotion@52; UnitMotion@89

### Gameplay/Combat → App（1 条，1 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Combat/MeleeCombat.cs`：MetaState@150

### Gameplay/Interaction → Gameplay/Units（6 条，3 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Interaction/IInteractionTarget.cs`：CharacterBrain@32; CharacterBrain@35; CharacterBrain@38
- `Assets/_Project/Scripts/Gameplay/Interaction/InteractionDefinition.cs`：CharacterBrain@28
- `Assets/_Project/Scripts/Gameplay/Interaction/InteractionRunner.cs`：CharacterBrain@18; CharacterBrain@22

### Gameplay/World → Core（6 条，4 个来源文件）

- `Assets/_Project/Scripts/Gameplay/World/Chest.cs`：MessageFeed@107; RunState@106
- `Assets/_Project/Scripts/Gameplay/World/DungeonDoor.cs`：Team@53
- `Assets/_Project/Scripts/Gameplay/World/FogOfWar.cs`：Team@86
- `Assets/_Project/Scripts/Gameplay/World/WaveSpawner.cs`：MessageFeed@42; MessageFeed@72

### Gameplay/World → Content（2 条，1 个来源文件）

- `Assets/_Project/Scripts/Gameplay/World/Chest.cs`：ItemData@46; ItemData@100

### Gameplay/World → Gameplay/Units（13 条，5 个来源文件）

- `Assets/_Project/Scripts/Gameplay/World/Chest.cs`：CharacterBrain@53; CharacterBrain@70; CharacterBrain@72; CharacterBrain@75
- `Assets/_Project/Scripts/Gameplay/World/DungeonDoor.cs`：CharacterBrain@37; CharacterBrain@39; CharacterBrain@41; UnitRegistry@53
- `Assets/_Project/Scripts/Gameplay/World/FogOfWar.cs`：UnitRegistry@86
- `Assets/_Project/Scripts/Gameplay/World/SkeletonSpawner.cs`：SkeletonWander@42; Visibility@43
- `Assets/_Project/Scripts/Gameplay/World/WaveSpawner.cs`：UnitHealth@26; UnitHealth@68

### Gameplay/World → Gameplay/Interaction（10 条，3 个来源文件）

- `Assets/_Project/Scripts/Gameplay/World/Chest.cs`：IInteractionTarget@15; InteractionDefinition@72; InteractionPriority@66
- `Assets/_Project/Scripts/Gameplay/World/DungeonDoor.cs`：IInteractionTarget@12; InteractionPriority@33; InteractionRunner@23; InteractionRunner@25
- `Assets/_Project/Scripts/Gameplay/World/Interactable.cs`：IInteractionTarget@14; IInteractionTarget@16; IInteractionTarget@17

### Gameplay/World → Gameplay/Items（7 条，1 个来源文件）

- `Assets/_Project/Scripts/Gameplay/World/Chest.cs`：ChestItem@38; ChestItem@47; ChestItem@93; ChestItem@102; Inventory@28; Inventory@32; Inventory@36

### Gameplay/World → UI（1 条，1 个来源文件）

- `Assets/_Project/Scripts/Gameplay/World/DungeonDoor.cs`：TransitionFader@50

### Gameplay/Player → Core（10 条，2 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs`：MessageFeed@128; Team@109; Team@169; Team@232
- `Assets/_Project/Scripts/Gameplay/Player/SkillInputController.cs`：MessageFeed@103; MessageFeed@117; MessageFeed@175; MessageFeed@177; Team@196; Team@222

### Gameplay/Player → Gameplay/Units（33 条，3 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Player/PlayerCommandController.cs`：CharacterBrain@14; CharacterBrain@19; CharacterBrain@34
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs`：CharacterBrain@34; CharacterBrain@39; CharacterBrain@80; CharacterBrain@84; CharacterBrain@190; CharacterBrain@199; CharacterBrain@211; CharacterBrain@233 …
- `Assets/_Project/Scripts/Gameplay/Player/SkillInputController.cs`：CharacterBrain@74; CharacterBrain@121; SkillData@148; SkillSystem@36; SkillSystem@166; SkillSystem@169; SkillSystem@173; UnitHealth@39 …

### Gameplay/Player → Gameplay/Interaction（6 条，1 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs`：IInteractionTarget@157; IInteractionTarget@171; IInteractionTarget@211; IInteractionTarget@221; InteractionPriority@223; InteractionRunner@124

### Gameplay/Player → Gameplay/World（4 条，1 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs`：DebugCameraSwitcher@24; DebugCameraSwitcher@44; DebugCameraSwitcher@48; DebugCameraSwitcher@56

### Gameplay/Player → App（3 条，3 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Player/PlayerCommandController.cs`：AppFlow@26
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs`：AppFlow@94
- `Assets/_Project/Scripts/Gameplay/Player/SkillInputController.cs`：AppFlow@47

### Gameplay/Player → UI（2 条，2 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Player/PlayerCommandController.cs`：UIManager@27
- `Assets/_Project/Scripts/Gameplay/Player/SkillInputController.cs`：UIManager@48

### Gameplay/Items → Content（4 条，2 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Items/ChestItem.cs`：ItemData@9
- `Assets/_Project/Scripts/Gameplay/Items/Inventory.cs`：ItemData@12; ItemData@17; ItemData@21

### App → Core（15 条，4 个来源文件）

- `Assets/_Project/Scripts/App/AppFlow.cs`：Team@44
- `Assets/_Project/Scripts/App/GameData.cs`：MessageFeed@12; MessageFeed@16; MessageFeed@17; MessageFeed@20; MessageFeed@22; RunState@9; RunState@11; RunState@20 …
- `Assets/_Project/Scripts/App/MetaState.cs`：MessageFeed@24; RunState@19; RunState@20
- `Assets/_Project/Scripts/App/RunFlow.cs`：MessageFeed@54; Team@44

### App → Gameplay/Units（5 条，4 个来源文件）

- `Assets/_Project/Scripts/App/AppFlow.cs`：UnitRegistry@44
- `Assets/_Project/Scripts/App/GameData.cs`：UnitHealth@26
- `Assets/_Project/Scripts/App/MetaState.cs`：UnitHealth@17
- `Assets/_Project/Scripts/App/RunFlow.cs`：DownedRescue@46; UnitRegistry@44

### App → Gameplay/Combat（2 条，2 个来源文件）

- `Assets/_Project/Scripts/App/GameData.cs`：MeleeCombat@26
- `Assets/_Project/Scripts/App/MetaState.cs`：MeleeCombat@17

### UI → Core（11 条，3 个来源文件）

- `Assets/_Project/Scripts/UI/CursorManager.cs`：MessageFeed@74; Team@123; Team@153
- `Assets/_Project/Scripts/UI/HUD.cs`：MessageFeed@31; MessageFeed@33; MessageFeed@52; MessageFeed@54
- `Assets/_Project/Scripts/UI/InventoryUI.cs`：MessageFeed@127; MessageFeed@137; RunState@136; Team@36

### UI → Gameplay/Units（11 条，4 个来源文件）

- `Assets/_Project/Scripts/UI/CursorManager.cs`：DownedRescue@137; UnitHealth@62; UnitHealth@98; UnitHealth@104; UnitHealth@111; UnitHealth@122; UnitHealth@152
- `Assets/_Project/Scripts/UI/HUD.cs`：UnitHealth@48
- `Assets/_Project/Scripts/UI/InventoryUI.cs`：UnitRegistry@36
- `Assets/_Project/Scripts/UI/SkillBarUI.cs`：SkillSystem@54; SkillSystem@57

### UI → Gameplay/Combat（1 条，1 个来源文件）

- `Assets/_Project/Scripts/UI/HUD.cs`：MeleeCombat@49

### UI → Gameplay/World（11 条，3 个来源文件）

- `Assets/_Project/Scripts/UI/CursorManager.cs`：Interactable@63; Interactable@106; Interactable@111; Interactable@129; Interactable@130
- `Assets/_Project/Scripts/UI/InventoryUI.cs`：Chest@28; Chest@51
- `Assets/_Project/Scripts/UI/UIManager.cs`：Chest@41; Chest@46; Chest@50; Chest@120

### UI → Gameplay/Player（12 条，4 个来源文件）

- `Assets/_Project/Scripts/UI/CursorManager.cs`：Selectable@64; Selectable@107; Selectable@111; Selectable@118; Selectable@119; SelectionManager@150
- `Assets/_Project/Scripts/UI/HUD.cs`：SelectionManager@46; SelectionManager@47
- `Assets/_Project/Scripts/UI/SkillBarUI.cs`：SelectionManager@56; SelectionManager@57
- `Assets/_Project/Scripts/UI/UIManager.cs`：SelectionManager@123; SelectionManager@124

### UI → Gameplay/Items（8 条，2 个来源文件）

- `Assets/_Project/Scripts/UI/InventoryUI.cs`：Inventory@19; Inventory@27; Inventory@38; Inventory@45; Inventory@51; Inventory@80
- `Assets/_Project/Scripts/UI/UIManager.cs`：Inventory@120; Inventory@124

### UI → App（7 条，4 个来源文件）

- `Assets/_Project/Scripts/UI/EscMenuPanel.cs`：AppFlow@13; AppFlow@18; AppFlow@31
- `Assets/_Project/Scripts/UI/HUD.cs`：MetaState@51
- `Assets/_Project/Scripts/UI/MainMenuPanel.cs`：AppFlow@17
- `Assets/_Project/Scripts/UI/UIManager.cs`：AppFlow@65; RunFlow@65

### Debug → Core（3 条，1 个来源文件）

- `Assets/_Project/Scripts/Debug/DebugTestTools.cs`：Team@36; Team@68; Team@96

### Debug → Gameplay/Units（5 条，1 个来源文件）

- `Assets/_Project/Scripts/Debug/DebugTestTools.cs`：SkillSystem@112; UnitHealth@67; UnitMana@109; UnitRegistry@34; UnitRegistry@94
