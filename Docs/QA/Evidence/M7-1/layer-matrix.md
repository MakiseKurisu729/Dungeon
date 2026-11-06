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
  - `Assets/_Project/Scripts/Gameplay/Combat/MeleeCombat.cs:150` 引用 `MetaState` → `Assets/_Project/Scripts/App/MetaState.cs`
  - `Assets/_Project/Scripts/Gameplay/Player/PlayerCommandController.cs:26` 引用 `AppFlow` → `Assets/_Project/Scripts/App/AppFlow.cs`
  - `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs:94` 引用 `AppFlow` → `Assets/_Project/Scripts/App/AppFlow.cs`
  - `Assets/_Project/Scripts/Gameplay/Player/SkillInputController.cs:47` 引用 `AppFlow` → `Assets/_Project/Scripts/App/AppFlow.cs`
  - `Assets/_Project/Scripts/Gameplay/Units/BossBrain.cs:74` 引用 `AppFlow` → `Assets/_Project/Scripts/App/AppFlow.cs`
  - `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs:149` 引用 `AppFlow` → `Assets/_Project/Scripts/App/AppFlow.cs`
  - `Assets/_Project/Scripts/Gameplay/Units/DownedRescue.cs:215` 引用 `AppFlow` → `Assets/_Project/Scripts/App/AppFlow.cs`
  - `Assets/_Project/Scripts/Gameplay/Units/MonsterBrain.cs:121` 引用 `AppFlow` → `Assets/_Project/Scripts/App/AppFlow.cs`
- **Gameplay → UI**：3 条边
  - `Assets/_Project/Scripts/Gameplay/Player/PlayerCommandController.cs:27` 引用 `UIManager` → `Assets/_Project/Scripts/UI/UIManager.cs`
  - `Assets/_Project/Scripts/Gameplay/Player/SkillInputController.cs:48` 引用 `UIManager` → `Assets/_Project/Scripts/UI/UIManager.cs`
  - `Assets/_Project/Scripts/Gameplay/World/DungeonDoor.cs:50` 引用 `TransitionFader` → `Assets/_Project/Scripts/UI/TransitionFader.cs`

## 每个依赖组的来源文件

### Content → Core（1 条，1 个来源文件）

- `Assets/_Project/Scripts/Content/UnitStats.cs`：Team@11

### Gameplay → Core（80 条，24 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Combat/CombatFeedback.cs`：EventBus@15; EventBus@20
- `Assets/_Project/Scripts/Gameplay/Combat/ExplosionFx.cs`：Team@147
- `Assets/_Project/Scripts/Gameplay/Combat/MeleeCombat.cs`：ICharacterCombat@14; Team@368
- `Assets/_Project/Scripts/Gameplay/Combat/RangedCombat.cs`：ICharacterCombat@8
- `Assets/_Project/Scripts/Gameplay/Combat/SkillEffectFireball.cs`：MessageFeed@30; MessageFeed@32; MessageFeed@34
- `Assets/_Project/Scripts/Gameplay/Combat/SkillEffects.cs`：MessageFeed@32; MessageFeed@34; MessageFeed@36
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs`：MessageFeed@128; Team@109; Team@169; Team@232
- `Assets/_Project/Scripts/Gameplay/Player/SkillInputController.cs`：MessageFeed@103; MessageFeed@117; MessageFeed@175; MessageFeed@177; Team@196; Team@222
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
- `Assets/_Project/Scripts/Gameplay/World/Chest.cs`：MessageFeed@107; RunState@106
- `Assets/_Project/Scripts/Gameplay/World/DungeonDoor.cs`：Team@53
- `Assets/_Project/Scripts/Gameplay/World/FogOfWar.cs`：Team@86
- `Assets/_Project/Scripts/Gameplay/World/WaveSpawner.cs`：MessageFeed@42; MessageFeed@72

### Gameplay → Content（25 条，14 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Combat/DamageInfo.cs`：DamageType@17
- `Assets/_Project/Scripts/Gameplay/Combat/ExplosionFx.cs`：DamageType@148
- `Assets/_Project/Scripts/Gameplay/Combat/MeleeCombat.cs`：DamageType@342; DamageType@369; UnitStats@30; WeaponData@24
- `Assets/_Project/Scripts/Gameplay/Combat/Projectile.cs`：DamageType@70
- `Assets/_Project/Scripts/Gameplay/Combat/RangedCombat.cs`：UnitStats@15
- `Assets/_Project/Scripts/Gameplay/Combat/SkillEffects.cs`：DamageType@73
- `Assets/_Project/Scripts/Gameplay/Items/ChestItem.cs`：ItemData@9
- `Assets/_Project/Scripts/Gameplay/Items/Inventory.cs`：ItemData@12; ItemData@17; ItemData@21
- `Assets/_Project/Scripts/Gameplay/Units/BossBrain.cs`：DamageType@136; DamageType@167; UnitStats@14
- `Assets/_Project/Scripts/Gameplay/Units/ShieldedUnitHealth.cs`：DamageType@14
- `Assets/_Project/Scripts/Gameplay/Units/UnitAnimatorSpeed.cs`：UnitStats@12; UnitStats@27
- `Assets/_Project/Scripts/Gameplay/Units/UnitBrain.cs`：UnitStats@17
- `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`：DamageType@66; DamageType@94; UnitStats@13
- `Assets/_Project/Scripts/Gameplay/World/Chest.cs`：ItemData@46; ItemData@100

### Gameplay → App（8 条，8 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Combat/MeleeCombat.cs`：MetaState@150
- `Assets/_Project/Scripts/Gameplay/Player/PlayerCommandController.cs`：AppFlow@26
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs`：AppFlow@94
- `Assets/_Project/Scripts/Gameplay/Player/SkillInputController.cs`：AppFlow@47
- `Assets/_Project/Scripts/Gameplay/Units/BossBrain.cs`：AppFlow@74
- `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`：AppFlow@149
- `Assets/_Project/Scripts/Gameplay/Units/DownedRescue.cs`：AppFlow@215
- `Assets/_Project/Scripts/Gameplay/Units/MonsterBrain.cs`：AppFlow@121

### Gameplay → UI（3 条，3 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Player/PlayerCommandController.cs`：UIManager@27
- `Assets/_Project/Scripts/Gameplay/Player/SkillInputController.cs`：UIManager@48
- `Assets/_Project/Scripts/Gameplay/World/DungeonDoor.cs`：TransitionFader@50

### App → Core（15 条，4 个来源文件）

- `Assets/_Project/Scripts/App/AppFlow.cs`：Team@44
- `Assets/_Project/Scripts/App/GameData.cs`：MessageFeed@12; MessageFeed@16; MessageFeed@17; MessageFeed@20; MessageFeed@22; RunState@9; RunState@11; RunState@20 …
- `Assets/_Project/Scripts/App/MetaState.cs`：MessageFeed@24; RunState@19; RunState@20
- `Assets/_Project/Scripts/App/RunFlow.cs`：MessageFeed@54; Team@44

### App → Gameplay（7 条，4 个来源文件）

- `Assets/_Project/Scripts/App/AppFlow.cs`：UnitRegistry@44
- `Assets/_Project/Scripts/App/GameData.cs`：MeleeCombat@26; UnitHealth@26
- `Assets/_Project/Scripts/App/MetaState.cs`：MeleeCombat@17; UnitHealth@17
- `Assets/_Project/Scripts/App/RunFlow.cs`：DownedRescue@46; UnitRegistry@44

### UI → Core（11 条，3 个来源文件）

- `Assets/_Project/Scripts/UI/CursorManager.cs`：MessageFeed@74; Team@123; Team@153
- `Assets/_Project/Scripts/UI/HUD.cs`：MessageFeed@31; MessageFeed@33; MessageFeed@52; MessageFeed@54
- `Assets/_Project/Scripts/UI/InventoryUI.cs`：MessageFeed@127; MessageFeed@137; RunState@136; Team@36

### UI → Gameplay（43 条，5 个来源文件）

- `Assets/_Project/Scripts/UI/CursorManager.cs`：DownedRescue@137; Interactable@63; Interactable@106; Interactable@111; Interactable@129; Interactable@130; Selectable@64; Selectable@107 …
- `Assets/_Project/Scripts/UI/HUD.cs`：MeleeCombat@49; SelectionManager@46; SelectionManager@47; UnitHealth@48
- `Assets/_Project/Scripts/UI/InventoryUI.cs`：Chest@28; Chest@51; Inventory@19; Inventory@27; Inventory@38; Inventory@45; Inventory@51; Inventory@80 …
- `Assets/_Project/Scripts/UI/SkillBarUI.cs`：SelectionManager@56; SelectionManager@57; SkillSystem@54; SkillSystem@57
- `Assets/_Project/Scripts/UI/UIManager.cs`：Chest@41; Chest@46; Chest@50; Chest@120; Inventory@120; Inventory@124; SelectionManager@123; SelectionManager@124

### UI → App（7 条，4 个来源文件）

- `Assets/_Project/Scripts/UI/EscMenuPanel.cs`：AppFlow@13; AppFlow@18; AppFlow@31
- `Assets/_Project/Scripts/UI/HUD.cs`：MetaState@51
- `Assets/_Project/Scripts/UI/MainMenuPanel.cs`：AppFlow@17
- `Assets/_Project/Scripts/UI/UIManager.cs`：AppFlow@65; RunFlow@65

### Editor → Core（3 条，1 个来源文件）

- `Assets/_Project/Scripts/Debug/DebugTestTools.cs`：Team@36; Team@68; Team@96

### Editor → Gameplay（5 条，1 个来源文件）

- `Assets/_Project/Scripts/Debug/DebugTestTools.cs`：SkillSystem@112; UnitHealth@67; UnitMana@109; UnitRegistry@34; UnitRegistry@94
