# M7-0 层依赖矩阵（按目标层）

> 由 `audit_scripts.py` 生成；行=引用方，列=被引用方，单元格为**边数**（同一文件/行/类型去重后）。
> 逐边原始记录（含 `文件:行` 与源码片段）见 `edges.csv`。

| 引用方 \ 被引用 | Core | Content | Gameplay | App | UI | Editor | 合计 |
|---|---|---|---|---|---|---|---|
| **Core** | (0) | 0 | 0 | 0 | 0 | 0 | 0 |
| **Content** | 1 | (0) | 0 | 0 | 0 | 0 | 1 |
| **Gameplay** | 88 | 25 | (294) | 0 | 1 | 0 | 408 |
| **App** | 21 | 0 | 7 | (15) | 0 | 0 | 43 |
| **UI** | 11 | 0 | 46 | 7 | (18) | 0 | 82 |
| **Editor** | 3 | 0 | 5 | 0 | 0 | (0) | 8 |

括号内为同组内部引用。

## 违规方向（下层 → 上层，M7 必须先消）

- **Gameplay → UI**：1 条边
  - `Assets/_Project/Scripts/Gameplay/World/DungeonDoor.cs:51` 引用 `TransitionFader` → `Assets/_Project/Scripts/UI/TransitionFader.cs`

## 每个依赖组的来源文件

### Content → Core（1 条，1 个来源文件）

- `Assets/_Project/Scripts/Content/UnitStats.cs`：Team@11

### Gameplay → Core（88 条，25 个来源文件）

- `Assets/_Project/Scripts/Gameplay/Combat/CombatFeedback.cs`：EventBus@15; EventBus@20
- `Assets/_Project/Scripts/Gameplay/Combat/ExplosionFx.cs`：Team@147
- `Assets/_Project/Scripts/Gameplay/Combat/MeleeCombat.cs`：ICharacterCombat@13; ProgressionState@149; Team@367
- `Assets/_Project/Scripts/Gameplay/Combat/RangedCombat.cs`：ICharacterCombat@8
- `Assets/_Project/Scripts/Gameplay/Combat/SkillEffectFireball.cs`：MessageFeed@30; MessageFeed@32; MessageFeed@34
- `Assets/_Project/Scripts/Gameplay/Combat/SkillEffects.cs`：MessageFeed@32; MessageFeed@34; MessageFeed@36
- `Assets/_Project/Scripts/Gameplay/Player/PlayerCommandController.cs`：PauseState@26
- `Assets/_Project/Scripts/Gameplay/Player/SelectionManager.cs`：MessageFeed@127; PauseState@93; Team@108; Team@168; Team@231
- `Assets/_Project/Scripts/Gameplay/Player/SkillInputController.cs`：MessageFeed@102; MessageFeed@116; MessageFeed@174; MessageFeed@176; PauseState@46; Team@195; Team@221
- `Assets/_Project/Scripts/Gameplay/Units/BossBrain.cs`：MessageFeed@66; MessageFeed@83; PauseState@73; RunState@64; RunState@65; Team@134; Team@179
- `Assets/_Project/Scripts/Gameplay/Units/CharacterBrain.cs`：ICharacterCombat@97; PauseState@148; Team@27
- `Assets/_Project/Scripts/Gameplay/Units/DownedRescue.cs`：MessageFeed@92; MessageFeed@174; MessageFeed@202; MessageFeed@294; MessageFeed@309; PauseState@214
- `Assets/_Project/Scripts/Gameplay/Units/MonsterBrain.cs`：ICharacterCombat@185; PauseState@120; RunState@100; RunState@101; Team@53
- `Assets/_Project/Scripts/Gameplay/Units/SkeletonWander.cs`：Team@63
- `Assets/_Project/Scripts/Gameplay/Units/SkillData.cs`：Cooldown@16
- `Assets/_Project/Scripts/Gameplay/Units/SkillSystem.cs`：Cooldown@28; Cooldown@32; Cooldown@42; MessageFeed@84; MessageFeed@85; MessageFeed@92
- `Assets/_Project/Scripts/Gameplay/Units/UnitBrain.cs`：ICharacterCombat@106; Team@22; Team@76
- `Assets/_Project/Scripts/Gameplay/Units/UnitHealth.cs`：EventBus@107; EventBus@113; Team@14
- `Assets/_Project/Scripts/Gameplay/Units/UnitRegistry.cs`：Team@181; Team@184; Team@190; Team@210; Team@215; Team@267; Team@272; UnitId@30 …
- `Assets/_Project/Scripts/Gameplay/Units/Visibility.cs`：Team@43
- `Assets/_Project/Scripts/Gameplay/Units/WorldHealthBar.cs`：Team@41; Team@47; Team@63
- `Assets/_Project/Scripts/Gameplay/World/Chest.cs`：MessageFeed@107; RunState@106
- `Assets/_Project/Scripts/Gameplay/World/DungeonDoor.cs`：Team@54
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

### Gameplay → UI（1 条，1 个来源文件）

- `Assets/_Project/Scripts/Gameplay/World/DungeonDoor.cs`：TransitionFader@51

### App → Core（21 条，4 个来源文件）

- `Assets/_Project/Scripts/App/AppFlow.cs`：PauseState@15; PauseState@20; PauseState@27; PauseState@36; Team@44
- `Assets/_Project/Scripts/App/GameData.cs`：MessageFeed@12; MessageFeed@16; MessageFeed@17; MessageFeed@20; MessageFeed@22; RunState@9; RunState@11; RunState@20 …
- `Assets/_Project/Scripts/App/MetaState.cs`：MessageFeed@28; ProgressionState@12; ProgressionState@13; RunState@23; RunState@24
- `Assets/_Project/Scripts/App/RunFlow.cs`：MessageFeed@54; Team@44

### App → Gameplay（7 条，4 个来源文件）

- `Assets/_Project/Scripts/App/AppFlow.cs`：UnitRegistry@44
- `Assets/_Project/Scripts/App/GameData.cs`：MeleeCombat@26; UnitHealth@26
- `Assets/_Project/Scripts/App/MetaState.cs`：MeleeCombat@21; UnitHealth@21
- `Assets/_Project/Scripts/App/RunFlow.cs`：DownedRescue@46; UnitRegistry@44

### UI → Core（11 条，3 个来源文件）

- `Assets/_Project/Scripts/UI/CursorManager.cs`：MessageFeed@75; Team@124; Team@154
- `Assets/_Project/Scripts/UI/HUD.cs`：MessageFeed@32; MessageFeed@34; MessageFeed@53; MessageFeed@55
- `Assets/_Project/Scripts/UI/InventoryUI.cs`：MessageFeed@128; MessageFeed@138; RunState@137; Team@37

### UI → Gameplay（46 条，5 个来源文件）

- `Assets/_Project/Scripts/UI/CursorManager.cs`：DownedRescue@138; Interactable@64; Interactable@107; Interactable@112; Interactable@130; Interactable@131; Selectable@65; Selectable@108 …
- `Assets/_Project/Scripts/UI/HUD.cs`：MeleeCombat@50; SelectionManager@47; SelectionManager@48; UnitHealth@49
- `Assets/_Project/Scripts/UI/InventoryUI.cs`：Chest@29; Chest@52; Inventory@20; Inventory@28; Inventory@39; Inventory@46; Inventory@52; Inventory@81 …
- `Assets/_Project/Scripts/UI/SkillBarUI.cs`：SelectionManager@57; SelectionManager@58; SkillSystem@55; SkillSystem@58
- `Assets/_Project/Scripts/UI/UIManager.cs`：Chest@47; Chest@53; Chest@57; Chest@127; IUiModalState@15; Inventory@127; Inventory@131; SelectionManager@130 …

### UI → App（7 条，4 个来源文件）

- `Assets/_Project/Scripts/UI/EscMenuPanel.cs`：AppFlow@14; AppFlow@19; AppFlow@32
- `Assets/_Project/Scripts/UI/HUD.cs`：MetaState@52
- `Assets/_Project/Scripts/UI/MainMenuPanel.cs`：AppFlow@18
- `Assets/_Project/Scripts/UI/UIManager.cs`：AppFlow@72; RunFlow@72

### Editor → Core（3 条，1 个来源文件）

- `Assets/_Project/Scripts/Editor/DebugTestTools.cs`：Team@37; Team@69; Team@97

### Editor → Gameplay（5 条，1 个来源文件）

- `Assets/_Project/Scripts/Editor/DebugTestTools.cs`：SkillSystem@113; UnitHealth@68; UnitMana@110; UnitRegistry@35; UnitRegistry@95
