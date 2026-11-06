# 醒来目检清单 — SPEC-01 收口三步批

> 对应报告：`Docs/Handoff/REPORT_SPEC-01-CLEANUP.md`
> 说明：自动化已覆盖编译、引用完整性、三套件断言与证据（见 `Docs/QA/Evidence/CLEANUP/`）。下列为**只能人工判断**的项。
> ⚠️ 目检时**不要保存**场景/预制体（避免产生资产 diff）；只看不改。

---

## A. 必检（5 项）

### A1. 改名后 Inspector 组件显示名与引用完整性
1. 打开 `Assets/Scenes/DungeonWorld.unity`。
2. 选中 `Knight_Unit`（Hierarchy 中玩家单位），看 Inspector 里原「Sword Swing」组件：
   - [ ] 标题显示为 **Melee Combat**（Unity 自动在类名中加空格）；
   - [ ] **没有** `Missing (Script)`；
   - [ ] `sword` / `shield` / `weapon` / `stats` / `health` 槽位仍有引用（不是 None）。
3. 对 `PlayerCapsule` 预制体实例做同样检查（该预制体也挂了此组件）。
4. 若出现 Missing Script → **立即停止并回报**（说明 GUID 链路断了）。

### A2. 骑士普攻手感（单段 `AttackMelee`）
1. Play，右键点选 `Skeleton_Warrior`。
2. 观察骑士接近 → 挥砍：
   - [ ] 只挥 **一段**，命中窗口内目标掉血一次；
   - [ ] 收招后回到 `Locomotion`，不卡在攻击姿势；
   - [ ] 连续点右键可连续攻击，无「挥空但掉血」或「贴脸不中」。

### A3. 弓/法两段式（`AttackRange` → `Attack_Draw → Attack_Release`）
1. 分别选中 `Archer_Unit`、`Mage_Unit` 攻击同一目标。
2. - [ ] 先播前摇（拉弓/蓄力），再播释放；
   - [ ] 箭矢/法术在**释放段**生成（不是前摇就飞出去）；
   - [ ] 两段之间无 T-pose / 跳帧。

### A4. 伤害飘字
1. 对敌人造成一次普攻与一次技能伤害。
2. - [ ] 飘字出现、上浮、消失；
   - [ ] 尺寸明显大于旧版（基线 3.5x），不糊屏、不遮挡血条；
   - [ ] 连续多段伤害时飘字不重叠成一片（池上限 32）。

### A5. API 文档可读性
打开 `docx/API文档_魔石地牢_v1.0.md`（或渲染后的 docx）：
- [ ] `### MeleeCombat` 段落标题与路径正确（`Combat/MeleeCombat.cs`）；
- [ ] 检查器字段表**表格未错行**（新增的 `attackTrigger/hitStateName/drawStateName/releaseStateName` 四行在表内）；
- [ ] `ICharacterCombat` 段落的「攻击触发器/状态口径」一句完整可读；
- [ ] 全文搜索 `SwordSwing` 无结果。

---

## B. 决策项裁决（2 项，需所有者拍板）

### B1. FND00B06 `Animation.Hit` 是产品缺陷还是夹具时序缺陷？
1. 编辑器 Play，对 `Knight_Unit` 手动造成一次伤害（例如让骷髅攻击一次，或用调试手段调 `UnitHealth.ApplyDamage`）。
2. - [ ] 骑士是否播放 `Hit_A` 受击动画？
   - 播 → 判定为 FND00B06 取样时序缺陷，按报告 §7-A 建议 2 修夹具；
   - 不播 → 判定为真实产品缺陷，另开修复单。
3. 同时确认 `Animation.Attack` 的处置：改为 `AttackMelee` 单段口径，还是直接删除（由 SPEC-01 覆盖）。

### B2. FND02B 旧套件如何处置？
- 方案 1（推荐）：登记「被 SPEC-01 取代」，停止维护；
- 方案 2：把 3 条断言改写为单段 `AttackMelee` + 1D `SpeedZ` 口径；
- 方案 3：整体删除套件并归档证据。
> 现状：该套件 3 红与基线同名同因，不影响本次清理的有效性。

---

## C. 顺带确认（低优先）

- [ ] `Assembly-CSharp.csproj` / `Assembly-CSharp-Editor.csproj` 的改动是 Unity 自动重生成（`MeleeCombat.cs` 替 `SwordSwing.cs` + SPEC-01 测试文件条目），确认可随改名一起提交。
- [ ] 若不需要保留还原快照，可删除 `Docs/QA/Evidence/CLEANUP/_backup/`（仅自证用）。
- [ ] 技能斩（Q/W/E/R）仍能打断普攻并群体扫掠（本单未改逻辑，抽查即可）。
