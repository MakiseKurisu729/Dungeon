# M7-0 层依赖矩阵（按目标层）

> 由 `audit_scripts.py` 生成；行=引用方，列=被引用方，单元格为**边数**（同一文件/行/类型去重后）。
> 逐边原始记录（含 `文件:行` 与源码片段）见 `edges.csv`。

| 引用方 \ 被引用 | Core | Content | Gameplay | App | UI | Editor | 合计 |
|---|---|---|---|---|---|---|---|
| **Core** | (0) | 0 | 0 | 0 | 0 | 0 | 0 |
| **Content** | 1 | (0) | 0 | 0 | 0 | 0 | 1 |
| **Gameplay** | 80 | 25 | (292) | 8 | 3 | 0 | 408 |
| **App** | 15 | 0 | 7 | (15) | 0 | 0 | 37 |
| **UI** | 11 | 0 | 43 | 7 | (18) | 0 | 79 |
| **Editor** | 3 | 0 | 5 | 0 | 0 | (0) | 8 |

括号内为同组内部引用。

## 违规方向（下层 → 上层，M7 必须先消）

- **Gameplay → App**：8 条边
  - `Assets/_Project/Scripts/Combat/MeleeCombat.cs:147` 引用 `MetaState` → `Assets/_Project/Scripts/Core/MetaState.cs`
  - `Assets/_Project/Scripts/Player/PlayerCommandController.cs:23` 引用 `AppFlow` → `Assets/_Project/Scripts/Core/AppFlow.cs`
  - `Assets/_Project/Scripts/Player/SelectionManager.cs:89` 引用 `AppFlow` → `Assets/_Project/Scripts/Core/AppFlow.cs`
  - `Assets/_Project/Scripts/Player/SkillInputController.cs:44` 引用 `AppFlow` → `Assets/_Project/Scripts/Core/AppFlow.cs`
  - `Assets/_Project/Scripts/Units/BossBrain.cs:72` 引用 `AppFlow` → `Assets/_Project/Scripts/Core/AppFlow.cs`
  - `Assets/_Project/Scripts/Units/CharacterBrain.cs:144` 引用 `AppFlow` → `Assets/_Project/Scripts/Core/AppFlow.cs`
  - `Assets/_Project/Scripts/Units/DownedRescue.cs:209` 引用 `AppFlow` → `Assets/_Project/Scripts/Core/AppFlow.cs`
  - `Assets/_Project/Scripts/Units/MonsterBrain.cs:119` 引用 `AppFlow` → `Assets/_Project/Scripts/Core/AppFlow.cs`
- **Gameplay → UI**：3 条边
  - `Assets/_Project/Scripts/Player/PlayerCommandController.cs:24` 引用 `UIManager` → `Assets/_Project/Scripts/UI/UIManager.cs`
  - `Assets/_Project/Scripts/Player/SkillInputController.cs:45` 引用 `UIManager` → `Assets/_Project/Scripts/UI/UIManager.cs`
  - `Assets/_Project/Scripts/System/DungeonDoor.cs:47` 引用 `TransitionFader` → `Assets/_Project/Scripts/UI/TransitionFader.cs`

## 每个依赖组的来源文件

### Content → Core（1 条，1 个来源文件）

- `Assets/_Project/Scripts/Content/UnitStats.cs`：Team@11

### Gameplay → Core（80 条，24 个来源文件）

- `Assets/_Project/Scripts/Combat/CombatFeedback.cs`：EventBus@14; EventBus@19
- `Assets/_Project/Scripts/Combat/ExplosionFx.cs`：Team@145
- `Assets/_Project/Scripts/Combat/MeleeCombat.cs`：ICharacterCombat@11; Team@365
- `Assets/_Project/Scripts/Combat/RangedCombat.cs`：ICharacterCombat@6
- `Assets/_Project/Scripts/Combat/SkillEffectFireball.cs`：MessageFeed@28; MessageFeed@30; MessageFeed@32
- `Assets/_Project/Scripts/Combat/SkillEffects.cs`：MessageFeed@30; MessageFeed@32; MessageFeed@34
- `Assets/_Project/Scripts/Core/UnitRegistry.cs`：Team@180; Team@183; Team@189; Team@209; Team@214; Team@266; Team@271; UnitId@29 …
- `Assets/_Project/Scripts/Core/WaveSpawner.cs`：MessageFeed@40; MessageFeed@70
- `Assets/_Project/Scripts/Player/SelectionManager.cs`：MessageFeed@123; Team@104; Team@164; Team@227
- `Assets/_Project/Scripts/Player/SkillInputController.cs`：MessageFeed@100; MessageFeed@114; MessageFeed@172; MessageFeed@174; Team@193; Team@219
- `Assets/_Project/Scripts/System/Chest.cs`：MessageFeed@103; RunState@102
- `Assets/_Project/Scripts/System/DungeonDoor.cs`：Team@50
- `Assets/_Project/Scripts/System/FogOfWar.cs`：Team@84
- `Assets/_Project/Scripts/Units/BossBrain.cs`：MessageFeed@65; MessageFeed@82; RunState@63; RunState@64; Team@133; Team@178
- `Assets/_Project/Scripts/Units/CharacterBrain.cs`：ICharacterCombat@93; Team@23
- `Assets/_Project/Scripts/Units/DownedRescue.cs`：MessageFeed@87; MessageFeed@169; MessageFeed@197; MessageFeed@289; MessageFeed@304
- `Assets/_Project/Scripts/Units/MonsterBrain.cs`：ICharacterCombat@184; RunState@99; RunState@100; Team@52
- `Assets/_Project/Scripts/Units/SkeletonWander.cs`：Team@62
- `Assets/_Project/Scripts/Units/SkillData.cs`：Cooldown@14
- `Assets/_Project/Scripts/Units/SkillSystem.cs`：Cooldown@26; Cooldown@30; Cooldown@40; MessageFeed@82; MessageFeed@83; MessageFeed@90
- `Assets/_Project/Scripts/Units/UnitBrain.cs`：ICharacterCombat@105; Team@21; Team@75
- `Assets/_Project/Scripts/Units/UnitHealth.cs`：EventBus@105; EventBus@111; Team@12
- `Assets/_Project/Scripts/Units/Visibility.cs`：Team@42
- `Assets/_Project/Scripts/Units/WorldHealthBar.cs`：Team@40; Team@46; Team@62

### Gameplay → Content（25 条，14 个来源文件）

- `Assets/_Project/Scripts/Combat/DamageInfo.cs`：DamageType@15
- `Assets/_Project/Scripts/Combat/ExplosionFx.cs`：DamageType@146
- `Assets/_Project/Scripts/Combat/MeleeCombat.cs`：DamageType@339; DamageType@366; UnitStats@27; WeaponData@21
- `Assets/_Project/Scripts/Combat/Projectile.cs`：DamageType@68
- `Assets/_Project/Scripts/Combat/RangedCombat.cs`：UnitStats@13
- `Assets/_Project/Scripts/Combat/SkillEffects.cs`：DamageType@71
- `Assets/_Project/Scripts/Items/ChestItem.cs`：ItemData@8
- `Assets/_Project/Scripts/Items/Inventory.cs`：ItemData@11; ItemData@16; ItemData@20
- `Assets/_Project/Scripts/System/Chest.cs`：ItemData@42; ItemData@96
- `Assets/_Project/Scripts/Units/BossBrain.cs`：DamageType@134; DamageType@165; UnitStats@12
- `Assets/_Project/Scripts/Units/ShieldedUnitHealth.cs`：DamageType@13
- `Assets/_Project/Scripts/Units/UnitAnimatorSpeed.cs`：UnitStats@11; UnitStats@26
- `Assets/_Project/Scripts/Units/UnitBrain.cs`：UnitStats@16
- `Assets/_Project/Scripts/Units/UnitHealth.cs`：DamageType@64; DamageType@92; UnitStats@11

### Gameplay → App（8 条，8 个来源文件）

- `Assets/_Project/Scripts/Combat/MeleeCombat.cs`：MetaState@147
- `Assets/_Project/Scripts/Player/PlayerCommandController.cs`：AppFlow@23
- `Assets/_Project/Scripts/Player/SelectionManager.cs`：AppFlow@89
- `Assets/_Project/Scripts/Player/SkillInputController.cs`：AppFlow@44
- `Assets/_Project/Scripts/Units/BossBrain.cs`：AppFlow@72
- `Assets/_Project/Scripts/Units/CharacterBrain.cs`：AppFlow@144
- `Assets/_Project/Scripts/Units/DownedRescue.cs`：AppFlow@209
- `Assets/_Project/Scripts/Units/MonsterBrain.cs`：AppFlow@119

### Gameplay → UI（3 条，3 个来源文件）

- `Assets/_Project/Scripts/Player/PlayerCommandController.cs`：UIManager@24
- `Assets/_Project/Scripts/Player/SkillInputController.cs`：UIManager@45
- `Assets/_Project/Scripts/System/DungeonDoor.cs`：TransitionFader@47

### App → Core（15 条，4 个来源文件）

- `Assets/_Project/Scripts/Core/AppFlow.cs`：Team@42
- `Assets/_Project/Scripts/Core/GameData.cs`：MessageFeed@9; MessageFeed@13; MessageFeed@14; MessageFeed@17; MessageFeed@19; RunState@6; RunState@8; RunState@17 …
- `Assets/_Project/Scripts/Core/MetaState.cs`：MessageFeed@21; RunState@16; RunState@17
- `Assets/_Project/Scripts/Core/RunFlow.cs`：MessageFeed@52; Team@42

### App → Gameplay（7 条，4 个来源文件）

- `Assets/_Project/Scripts/Core/AppFlow.cs`：UnitRegistry@42
- `Assets/_Project/Scripts/Core/GameData.cs`：MeleeCombat@23; UnitHealth@23
- `Assets/_Project/Scripts/Core/MetaState.cs`：MeleeCombat@14; UnitHealth@14
- `Assets/_Project/Scripts/Core/RunFlow.cs`：DownedRescue@44; UnitRegistry@42

### UI → Core（11 条，3 个来源文件）

- `Assets/_Project/Scripts/UI/CursorManager.cs`：MessageFeed@71; Team@120; Team@150
- `Assets/_Project/Scripts/UI/HUD.cs`：MessageFeed@27; MessageFeed@29; MessageFeed@48; MessageFeed@50
- `Assets/_Project/Scripts/UI/InventoryUI.cs`：MessageFeed@124; MessageFeed@134; RunState@133; Team@33

### UI → Gameplay（43 条，5 个来源文件）

- `Assets/_Project/Scripts/UI/CursorManager.cs`：DownedRescue@134; Interactable@60; Interactable@103; Interactable@108; Interactable@126; Interactable@127; Selectable@61; Selectable@104 …
- `Assets/_Project/Scripts/UI/HUD.cs`：MeleeCombat@45; SelectionManager@42; SelectionManager@43; UnitHealth@44
- `Assets/_Project/Scripts/UI/InventoryUI.cs`：Chest@25; Chest@48; Inventory@16; Inventory@24; Inventory@35; Inventory@42; Inventory@48; Inventory@77 …
- `Assets/_Project/Scripts/UI/SkillBarUI.cs`：SelectionManager@54; SelectionManager@55; SkillSystem@52; SkillSystem@55
- `Assets/_Project/Scripts/UI/UIManager.cs`：Chest@37; Chest@42; Chest@46; Chest@116; Inventory@116; Inventory@120; SelectionManager@119; SelectionManager@120

### UI → App（7 条，4 个来源文件）

- `Assets/_Project/Scripts/UI/EscMenuPanel.cs`：AppFlow@12; AppFlow@17; AppFlow@30
- `Assets/_Project/Scripts/UI/HUD.cs`：MetaState@47
- `Assets/_Project/Scripts/UI/MainMenuPanel.cs`：AppFlow@16
- `Assets/_Project/Scripts/UI/UIManager.cs`：AppFlow@61; RunFlow@61

### Editor → Core（3 条，1 个来源文件）

- `Assets/_Project/Scripts/Debug/DebugTestTools.cs`：Team@35; Team@67; Team@95

### Editor → Gameplay（5 条，1 个来源文件）

- `Assets/_Project/Scripts/Debug/DebugTestTools.cs`：SkillSystem@111; UnitHealth@66; UnitMana@108; UnitRegistry@33; UnitRegistry@93
