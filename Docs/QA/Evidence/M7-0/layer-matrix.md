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
  - `Assets/_Project/Scripts/Combat/MeleeCombat.cs:145` 引用 `MetaState` → `Assets/_Project/Scripts/Core/MetaState.cs`
  - `Assets/_Project/Scripts/Player/PlayerCommandController.cs:23` 引用 `AppFlow` → `Assets/_Project/Scripts/Core/AppFlow.cs`
  - `Assets/_Project/Scripts/Player/SelectionManager.cs:88` 引用 `AppFlow` → `Assets/_Project/Scripts/Core/AppFlow.cs`
  - `Assets/_Project/Scripts/Player/SkillInputController.cs:43` 引用 `AppFlow` → `Assets/_Project/Scripts/Core/AppFlow.cs`
  - `Assets/_Project/Scripts/Units/BossBrain.cs:70` 引用 `AppFlow` → `Assets/_Project/Scripts/Core/AppFlow.cs`
  - `Assets/_Project/Scripts/Units/CharacterBrain.cs:143` 引用 `AppFlow` → `Assets/_Project/Scripts/Core/AppFlow.cs`
  - `Assets/_Project/Scripts/Units/DownedRescue.cs:208` 引用 `AppFlow` → `Assets/_Project/Scripts/Core/AppFlow.cs`
  - `Assets/_Project/Scripts/Units/MonsterBrain.cs:118` 引用 `AppFlow` → `Assets/_Project/Scripts/Core/AppFlow.cs`
- **Gameplay → UI**：3 条边
  - `Assets/_Project/Scripts/Player/PlayerCommandController.cs:24` 引用 `UIManager` → `Assets/_Project/Scripts/UI/UIManager.cs`
  - `Assets/_Project/Scripts/Player/SkillInputController.cs:44` 引用 `UIManager` → `Assets/_Project/Scripts/UI/UIManager.cs`
  - `Assets/_Project/Scripts/System/DungeonDoor.cs:46` 引用 `TransitionFader` → `Assets/_Project/Scripts/UI/TransitionFader.cs`

## 每个依赖组的来源文件

### Content → Core（1 条，1 个来源文件）

- `Assets/_Project/Scripts/Units/UnitStats.cs`：Team@9

### Gameplay → Core（80 条，24 个来源文件）

- `Assets/_Project/Scripts/Combat/CombatFeedback.cs`：EventBus@13; EventBus@18
- `Assets/_Project/Scripts/Combat/ExplosionFx.cs`：Team@143
- `Assets/_Project/Scripts/Combat/MeleeCombat.cs`：ICharacterCombat@9; Team@363
- `Assets/_Project/Scripts/Combat/RangedCombat.cs`：ICharacterCombat@4
- `Assets/_Project/Scripts/Combat/SkillEffectFireball.cs`：MessageFeed@27; MessageFeed@29; MessageFeed@31
- `Assets/_Project/Scripts/Combat/SkillEffects.cs`：MessageFeed@28; MessageFeed@30; MessageFeed@32
- `Assets/_Project/Scripts/Core/UnitRegistry.cs`：Team@179; Team@182; Team@188; Team@208; Team@213; Team@265; Team@270; UnitId@28 …
- `Assets/_Project/Scripts/Core/WaveSpawner.cs`：MessageFeed@39; MessageFeed@69
- `Assets/_Project/Scripts/Player/SelectionManager.cs`：MessageFeed@122; Team@103; Team@163; Team@226
- `Assets/_Project/Scripts/Player/SkillInputController.cs`：MessageFeed@99; MessageFeed@113; MessageFeed@171; MessageFeed@173; Team@192; Team@218
- `Assets/_Project/Scripts/System/Chest.cs`：MessageFeed@101; RunState@100
- `Assets/_Project/Scripts/System/DungeonDoor.cs`：Team@49
- `Assets/_Project/Scripts/System/FogOfWar.cs`：Team@83
- `Assets/_Project/Scripts/Units/BossBrain.cs`：MessageFeed@63; MessageFeed@80; RunState@61; RunState@62; Team@131; Team@176
- `Assets/_Project/Scripts/Units/CharacterBrain.cs`：ICharacterCombat@92; Team@22
- `Assets/_Project/Scripts/Units/DownedRescue.cs`：MessageFeed@86; MessageFeed@168; MessageFeed@196; MessageFeed@288; MessageFeed@303
- `Assets/_Project/Scripts/Units/MonsterBrain.cs`：ICharacterCombat@183; RunState@98; RunState@99; Team@51
- `Assets/_Project/Scripts/Units/SkeletonWander.cs`：Team@61
- `Assets/_Project/Scripts/Units/SkillData.cs`：Cooldown@13
- `Assets/_Project/Scripts/Units/SkillSystem.cs`：Cooldown@25; Cooldown@29; Cooldown@39; MessageFeed@81; MessageFeed@82; MessageFeed@89
- `Assets/_Project/Scripts/Units/UnitBrain.cs`：ICharacterCombat@103; Team@19; Team@73
- `Assets/_Project/Scripts/Units/UnitHealth.cs`：EventBus@103; EventBus@109; Team@10
- `Assets/_Project/Scripts/Units/Visibility.cs`：Team@41
- `Assets/_Project/Scripts/Units/WorldHealthBar.cs`：Team@39; Team@45; Team@61

### Gameplay → Content（25 条，14 个来源文件）

- `Assets/_Project/Scripts/Combat/DamageInfo.cs`：DamageType@14
- `Assets/_Project/Scripts/Combat/ExplosionFx.cs`：DamageType@144
- `Assets/_Project/Scripts/Combat/MeleeCombat.cs`：DamageType@337; DamageType@364; UnitStats@25; WeaponData@19
- `Assets/_Project/Scripts/Combat/Projectile.cs`：DamageType@67
- `Assets/_Project/Scripts/Combat/RangedCombat.cs`：UnitStats@11
- `Assets/_Project/Scripts/Combat/SkillEffects.cs`：DamageType@69
- `Assets/_Project/Scripts/Items/ChestItem.cs`：ItemData@7
- `Assets/_Project/Scripts/Items/Inventory.cs`：ItemData@10; ItemData@15; ItemData@19
- `Assets/_Project/Scripts/System/Chest.cs`：ItemData@40; ItemData@94
- `Assets/_Project/Scripts/Units/BossBrain.cs`：DamageType@132; DamageType@163; UnitStats@10
- `Assets/_Project/Scripts/Units/ShieldedUnitHealth.cs`：DamageType@12
- `Assets/_Project/Scripts/Units/UnitAnimatorSpeed.cs`：UnitStats@10; UnitStats@25
- `Assets/_Project/Scripts/Units/UnitBrain.cs`：UnitStats@14
- `Assets/_Project/Scripts/Units/UnitHealth.cs`：DamageType@62; DamageType@90; UnitStats@9

### Gameplay → App（8 条，8 个来源文件）

- `Assets/_Project/Scripts/Combat/MeleeCombat.cs`：MetaState@145
- `Assets/_Project/Scripts/Player/PlayerCommandController.cs`：AppFlow@23
- `Assets/_Project/Scripts/Player/SelectionManager.cs`：AppFlow@88
- `Assets/_Project/Scripts/Player/SkillInputController.cs`：AppFlow@43
- `Assets/_Project/Scripts/Units/BossBrain.cs`：AppFlow@70
- `Assets/_Project/Scripts/Units/CharacterBrain.cs`：AppFlow@143
- `Assets/_Project/Scripts/Units/DownedRescue.cs`：AppFlow@208
- `Assets/_Project/Scripts/Units/MonsterBrain.cs`：AppFlow@118

### Gameplay → UI（3 条，3 个来源文件）

- `Assets/_Project/Scripts/Player/PlayerCommandController.cs`：UIManager@24
- `Assets/_Project/Scripts/Player/SkillInputController.cs`：UIManager@44
- `Assets/_Project/Scripts/System/DungeonDoor.cs`：TransitionFader@46

### App → Core（15 条，4 个来源文件）

- `Assets/_Project/Scripts/Core/AppFlow.cs`：Team@41
- `Assets/_Project/Scripts/Core/GameData.cs`：MessageFeed@8; MessageFeed@12; MessageFeed@13; MessageFeed@16; MessageFeed@18; RunState@5; RunState@7; RunState@16 …
- `Assets/_Project/Scripts/Core/MetaState.cs`：MessageFeed@20; RunState@15; RunState@16
- `Assets/_Project/Scripts/Core/RunFlow.cs`：MessageFeed@51; Team@41

### App → Gameplay（7 条，4 个来源文件）

- `Assets/_Project/Scripts/Core/AppFlow.cs`：UnitRegistry@41
- `Assets/_Project/Scripts/Core/GameData.cs`：MeleeCombat@22; UnitHealth@22
- `Assets/_Project/Scripts/Core/MetaState.cs`：MeleeCombat@13; UnitHealth@13
- `Assets/_Project/Scripts/Core/RunFlow.cs`：DownedRescue@43; UnitRegistry@41

### UI → Core（11 条，3 个来源文件）

- `Assets/_Project/Scripts/UI/CursorManager.cs`：MessageFeed@70; Team@119; Team@149
- `Assets/_Project/Scripts/UI/HUD.cs`：MessageFeed@26; MessageFeed@28; MessageFeed@47; MessageFeed@49
- `Assets/_Project/Scripts/UI/InventoryUI.cs`：MessageFeed@123; MessageFeed@133; RunState@132; Team@32

### UI → Gameplay（43 条，5 个来源文件）

- `Assets/_Project/Scripts/UI/CursorManager.cs`：DownedRescue@133; Interactable@59; Interactable@102; Interactable@107; Interactable@125; Interactable@126; Selectable@60; Selectable@103 …
- `Assets/_Project/Scripts/UI/HUD.cs`：MeleeCombat@44; SelectionManager@41; SelectionManager@42; UnitHealth@43
- `Assets/_Project/Scripts/UI/InventoryUI.cs`：Chest@24; Chest@47; Inventory@15; Inventory@23; Inventory@34; Inventory@41; Inventory@47; Inventory@76 …
- `Assets/_Project/Scripts/UI/SkillBarUI.cs`：SelectionManager@54; SelectionManager@55; SkillSystem@52; SkillSystem@55
- `Assets/_Project/Scripts/UI/UIManager.cs`：Chest@37; Chest@42; Chest@46; Chest@116; Inventory@116; Inventory@120; SelectionManager@119; SelectionManager@120

### UI → App（7 条，4 个来源文件）

- `Assets/_Project/Scripts/UI/EscMenuPanel.cs`：AppFlow@12; AppFlow@17; AppFlow@30
- `Assets/_Project/Scripts/UI/HUD.cs`：MetaState@46
- `Assets/_Project/Scripts/UI/MainMenuPanel.cs`：AppFlow@16
- `Assets/_Project/Scripts/UI/UIManager.cs`：AppFlow@61; RunFlow@61

### Editor → Core（3 条，1 个来源文件）

- `Assets/_Project/Scripts/Debug/DebugTestTools.cs`：Team@34; Team@66; Team@94

### Editor → Gameplay（5 条，1 个来源文件）

- `Assets/_Project/Scripts/Debug/DebugTestTools.cs`：SkillSystem@110; UnitHealth@65; UnitMana@107; UnitRegistry@32; UnitRegistry@92
