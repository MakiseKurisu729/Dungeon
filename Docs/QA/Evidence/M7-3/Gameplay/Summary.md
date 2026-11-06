# M7-3 Gameplay 程序集：证据摘要

## 改动

- 新增 `Assets/_Project/Scripts/Gameplay/Dungeon.Gameplay.asmdef`（+ Unity 生成的 `.asmdef.meta`）：
  - `name`/`rootNamespace` = `Dungeon.Gameplay`
  - `references = ["Dungeon.Core", "Dungeon.Content", "UnityEngine.UI"]`
  - `includePlatforms = []`、`autoReferenced = true`、`noEngineReferences = false`
- 未改任何 `.cs`、场景、预制体、`.asset`。

## 验证

| 检查 | 实测 | 证据 |
|---|---|---|
| 编译 | 0 `error CS`；`is_compiling=false`、`is_domain_reload_pending=false` | `compile.md` |
| `M7-3.Asmdef`（stage 1） | PASS（Core/Content/Gameplay 字段一致） | `verify.txt` |
| `M7-3.EditorOnly` | PASS（运行时 asmdef 未引用 Editor） | `verify.txt` |
| `M7-3.NoAsset` | PASS（仅 `Gameplay/Dungeon.Gameplay.asmdef` + `.meta`） | `verify.txt` |
| `M7-3.Files` | PASS（Scripts 81、Tests 30） | `verify.txt` |
| `REFACTOR`（run37） | 20/0，`code=0` | `suites.md` |
| `SPEC01`（run56） | 12/0，`code=0` | `suites.md` |
| 审计 | `upward_layer_edges=0` | `audit/stats.json` |
