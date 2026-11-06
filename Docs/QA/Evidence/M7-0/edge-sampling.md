# M7-0 人工抽查（5 条边）

> 抽查方法：从 `edges.csv` 随机取 5 条边，回到源码对应 `文件:行` 核对是否为真实类型引用。
> 抽查时间：2026-11-06；执行者：OpenCode 执行者（只读）。

| # | 边（来源 `文件:行`） | 引用类型 | 目标文件 | 源码原文 | 判定 |
|---|---|---|---|---|---|
| 1 | `Assets/_Project/Scripts/Combat/MeleeCombat.cs:333` | `UnitHealth` | `Assets/_Project/Scripts/Units/UnitHealth.cs` | `var uh = _target.GetComponent<UnitHealth>();` | 真实 |
| 2 | `Assets/_Project/Scripts/UI/UIManager.cs:37` | `Chest` | `Assets/_Project/Scripts/System/Chest.cs` | `Chest.Opened += OnChestOpened;` | 真实（静态事件） |
| 3 | `Assets/_Project/Scripts/Core/UnitRegistry.cs:179` | `UnitHealth` / `Team` | `Units/UnitHealth.cs` / `Units/Team.cs` | `public static IReadOnlyList<UnitHealth> OfTeam(Team team, bool includeInactive = false)` | 真实（同一行两条边） |
| 4 | `Assets/_Project/Scripts/System/DungeonDoor.cs:46` | `TransitionFader` | `Assets/_Project/Scripts/UI/TransitionFader.cs` | `TransitionFader.Transition(() =>` | 真实（跨层：Gameplay → UI） |
| 5 | `Assets/_Project/Scripts/Units/SkillSystem.cs:16` | `SkillData` | `Assets/_Project/Scripts/Units/SkillData.cs` | `public SkillData[] skills = new SkillData[4];` | 真实 |

## 结论

- 5/5 抽查边均为真实类型引用，未发现误报。
- 误报防线：脚本已剥离注释与字符串；`小写变量.类型名` 的实例成员访问不计边；同文件自引用不计边。
- 已知残余误报面：与类型同名的**大写**属性/字段访问（如 `Foo.Bar` 中 `Bar` 恰好是类型名）无法完全区分，故 `cycles.md` 要求人工确认。

## 数据来源

- 边表：`Docs/QA/Evidence/M7-0/edges.csv`
- 脚本：`Docs/QA/Evidence/M7-0/audit_scripts.py`
- 复现：`py -3.12 -X utf8 Docs/QA/Evidence/M7-0/audit_scripts.py`
