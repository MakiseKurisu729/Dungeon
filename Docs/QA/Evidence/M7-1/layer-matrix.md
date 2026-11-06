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
  - `Assets/_Project/Scripts/Gameplay/Combat/MeleeCombat.cs:149` 引用 `MetaState` → `Assets/_Project/Scripts/Core/MetaState.cs`
  - `Assets/_Project/Scripts/Gameplay/Player/PlayerCommandController.cs:25` 引用 `AppFlow` → `Assets/_Project/Scripts/Core/AppFlow.cs`
  - `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs:93` 引用 `AppFlow` → `Assets/_Project/Scripts/Core/AppFlow.cs`
  - `Assets/_Project/Scripts/Gameplay/Player/SkillInputController.cs:46` 引用 `AppFlow` → `Assets/_Project/Scripts/Core/AppFlow.cs`
  - `Assets/_Project/Scripts/Gameplay/Units/BossBrain.cs:73` 引用 `AppFlow` → `Assets/_Project/Scripts/Core/AppFlow.cs`
  - `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs:148` 引用 `AppFlow` → `Assets/_Project/Scripts/Core/AppFlow.cs`
  - `Assets/_Project/Scripts/Gameplay/Units/DownedRescue.cs:214` 引用 `AppFlow` → `Assets/_Project/Scripts/Core/AppFlow.cs`
  - `Assets/_Project/Scripts/Gameplay/Units/MonsterBrain.cs:120` 引用 `AppFlow` → `Assets/_Project/Scripts/Core/AppFlow.cs`
- **Gameplay → UI**：3 条边
  - `Assets/_Project/Scripts/Gameplay/Player/PlayerCommandController.cs:26` 引用 `UIManager` → `Assets/_Project/Scripts/UI/UIManager.cs`
  - `Assets/_Project/Scripts/Gameplay/Player/SkillInputController.cs:47` 引用 `UIManager` → `Assets/_Project/Scripts/UI/UIManager.cs`
  - `Assets/_Project/Scripts/Gameplay/World/DungeonDoor.cs:50` 引用 `TransitionFader` → `Assets/_Project/Scripts/UI/TransitionFader.cs`

## 每个依赖组的来源文件

### Content → Core（1 条，1 个来源文件）

- `Assets/_Project/Scripts/Content/UnitStats.cs`：Team@11

### Gameplay → Core（80 条，24 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Combat/CombatFeedback.cs`：EventBus@15; EventBus@20
- `Assets/_Project/Scripts/Gameplay/Combat/ExplosionFx.cs`：Team@147
- `Assets/_Project/Scripts/Gameplay/Combat/MeleeCombat.cs`：ICharacterCombat@13; Team@367
- `Assets/_Project/Scripts/Gameplay/Combat/RangedCombat.cs`：ICharacterCombat@8
- `Assets/_Project/Scripts/Gameplay/Combat/SkillEffectFireball.cs`：MessageFeed@30; MessageFeed@32; MessageFeed@34
- `Assets/_Project/Scripts/Gameplay/Combat/SkillEffects.cs`：MessageFeed@32; MessageFeed@34; MessageFeed@36
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs`：MessageFeed@127; Team@108; Team@168; Team@231
- `Assets/_Project/Scripts/Gameplay/Player/SkillInputController.cs`：MessageFeed@102; MessageFeed@116; MessageFeed@174; MessageFeed@176; Team@195; Team@221
- `Assets/_Project/Scripts/Gameplay/Units/BossBrain.cs`：MessageFeed@66; MessageFeed@83; RunState@64; RunState@65; Team@134; Team@179
- `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`：ICharacterCombat@97; Team@27
- `Assets/_Project/Scripts/Gameplay/Units/DownedRescue.cs`：MessageFeed@92; MessageFeed@174; MessageFeed@202; MessageFeed@294; MessageFeed@309
- `Assets/_Project/Scripts/Gameplay/Units/MonsterBrain.cs`：ICharacterCombat@185; RunState@100; RunState@101; Team@53
- `Assets/_Project/Scripts/Gameplay/Units/SkeletonWander.cs`：Team@63
- `Assets/_Project/Scripts/Gameplay/Units/SkillData.cs`：Cooldown@16
- `Assets/_Project/Scripts/Gameplay/Units/SkillSystem.cs`：Cooldown@28; Cooldown@32; Cooldown@42; MessageFeed@84; MessageFeed@85; MessageFeed@92
- `Assets/_Project/Scripts/Gameplay/Units/UnitBrain.cs`：ICharacterCombat@106; Team@22; Team@76
- `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`：EventBus@107; EventBus@113; Team@14
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs`：Team@181; Team@184; Team@190; Team@210; Team@215; Team@267; Team@272; UnitId@30 …
- `Assets/_Project/Scripts/Gameplay/Units/Visibility.cs`：Team@43
- `Assets/_Project/Scripts/Gameplay/Units/WorldHealthBar.cs`：Team@41; Team@47; Team@63
- `Assets/_Project/Scripts/Gameplay/World/Chest.cs`：MessageFeed@107; RunState@106
- `Assets/_Project/Scripts/Gameplay/World/DungeonDoor.cs`：Team@53
- `Assets/_Project/Scripts/Gameplay/World/FogOfWar.cs`：Team@86
- `Assets/_Project/Scripts/Gameplay/World/WaveSpawner.cs`：MessageFeed@42; MessageFeed@72

### Gameplay → Content（25 条，14 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Combat/DamageInfo.cs`：DamageType@17
- `Assets/_Project/Scripts/Gameplay/Combat/ExplosionFx.cs`：DamageType@148
- `Assets/_Project/Scripts/Gameplay/Combat/MeleeCombat.cs`：DamageType@341; DamageType@368; UnitStats@29; WeaponData@23
- `Assets/_Project/Scripts/Gameplay/Combat/Projectile.cs`：DamageType@70
- `Assets/_Project/Scripts/Gameplay/Combat/RangedCombat.cs`：UnitStats@15
- `Assets/_Project/Scripts/Gameplay/Combat/SkillEffects.cs`：DamageType@73
- `Assets/_Project/Scripts/Gameplay/Items/ChestItem.cs`：ItemData@9
- `Assets/_Project/Scripts/Gameplay/Items/Inventory.cs`：ItemData@12; ItemData@17; ItemData@21
- `Assets/_Project/Scripts/Gameplay/Units/BossBrain.cs`：DamageType@135; DamageType@166; UnitStats@13
- `Assets/_Project/Scripts/Gameplay/Units/ShieldedUnitHealth.cs`：DamageType@14
- `Assets/_Project/Scripts/Gameplay/Units/UnitAnimatorSpeed.cs`：UnitStats@12; UnitStats@27
- `Assets/_Project/Scripts/Gameplay/Units/UnitBrain.cs`：UnitStats@17
- `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`：DamageType@66; DamageType@94; UnitStats@13
- `Assets/_Project/Scripts/Gameplay/World/Chest.cs`：ItemData@46; ItemData@100

### Gameplay → App（8 条，8 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Combat/MeleeCombat.cs`：MetaState@149
- `Assets/_Project/Scripts/Gameplay/Player/PlayerCommandController.cs`：AppFlow@25
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs`：AppFlow@93
- `Assets/_Project/Scripts/Gameplay/Player/SkillInputController.cs`：AppFlow@46
- `Assets/_Project/Scripts/Gameplay/Units/BossBrain.cs`：AppFlow@73
- `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`：AppFlow@148
- `Assets/_Project/Scripts/Gameplay/Units/DownedRescue.cs`：AppFlow@214
- `Assets/_Project/Scripts/Gameplay/Units/MonsterBrain.cs`：AppFlow@120

### Gameplay → UI（3 条，3 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Player/PlayerCommandController.cs`：UIManager@26
- `Assets/_Project/Scripts/Gameplay/Player/SkillInputController.cs`：UIManager@47
- `Assets/_Project/Scripts/Gameplay/World/DungeonDoor.cs`：TransitionFader@50

### App → Core（15 条，4 个来源文件）

- `Assets/_Project/Scripts/Core/AppFlow.cs`：Team@43
- `Assets/_Project/Scripts/Core/GameData.cs`：MessageFeed@11; MessageFeed@15; MessageFeed@16; MessageFeed@19; MessageFeed@21; RunState@8; RunState@10; RunState@19 …
- `Assets/_Project/Scripts/Core/MetaState.cs`：MessageFeed@23; RunState@18; RunState@19
- `Assets/_Project/Scripts/Core/RunFlow.cs`：MessageFeed@53; Team@43

### App → Gameplay（7 条，4 个来源文件）

- `Assets/_Project/Scripts/Core/AppFlow.cs`：UnitRegistry@43
- `Assets/_Project/Scripts/Core/GameData.cs`：MeleeCombat@25; UnitHealth@25
- `Assets/_Project/Scripts/Core/MetaState.cs`：MeleeCombat@16; UnitHealth@16
- `Assets/_Project/Scripts/Core/RunFlow.cs`：DownedRescue@45; UnitRegistry@43

### UI → Core（11 条，3 个来源文件）

- `Assets/_Project/Scripts/UI/CursorManager.cs`：MessageFeed@74; Team@123; Team@153
- `Assets/_Project/Scripts/UI/HUD.cs`：MessageFeed@30; MessageFeed@32; MessageFeed@51; MessageFeed@53
- `Assets/_Project/Scripts/UI/InventoryUI.cs`：MessageFeed@127; MessageFeed@137; RunState@136; Team@36

### UI → Gameplay（43 条，5 个来源文件）

- `Assets/_Project/Scripts/UI/CursorManager.cs`：DownedRescue@137; Interactable@63; Interactable@106; Interactable@111; Interactable@129; Interactable@130; Selectable@64; Selectable@107 …
- `Assets/_Project/Scripts/UI/HUD.cs`：MeleeCombat@48; SelectionManager@45; SelectionManager@46; UnitHealth@47
- `Assets/_Project/Scripts/UI/InventoryUI.cs`：Chest@28; Chest@51; Inventory@19; Inventory@27; Inventory@38; Inventory@45; Inventory@51; Inventory@80 …
- `Assets/_Project/Scripts/UI/SkillBarUI.cs`：SelectionManager@56; SelectionManager@57; SkillSystem@54; SkillSystem@57
- `Assets/_Project/Scripts/UI/UIManager.cs`：Chest@40; Chest@45; Chest@49; Chest@119; Inventory@119; Inventory@123; SelectionManager@122; SelectionManager@123

### UI → App（7 条，4 个来源文件）

- `Assets/_Project/Scripts/UI/EscMenuPanel.cs`：AppFlow@12; AppFlow@17; AppFlow@30
- `Assets/_Project/Scripts/UI/HUD.cs`：MetaState@50
- `Assets/_Project/Scripts/UI/MainMenuPanel.cs`：AppFlow@16
- `Assets/_Project/Scripts/UI/UIManager.cs`：AppFlow@64; RunFlow@64

### Editor → Core（3 条，1 个来源文件）

- `Assets/_Project/Scripts/Debug/DebugTestTools.cs`：Team@36; Team@68; Team@96

### Editor → Gameplay（5 条，1 个来源文件）

- `Assets/_Project/Scripts/Debug/DebugTestTools.cs`：SkillSystem@112; UnitHealth@67; UnitMana@109; UnitRegistry@34; UnitRegistry@94
