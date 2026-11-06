# M7-3 App 程序集：证据摘要

## 改动

- 新增 `Assets/_Project/Scripts/App/Dungeon.App.asmdef`（+ `.meta`）：
  - `name`/`rootNamespace` = `Dungeon.App`
  - `references = ["Dungeon.Core", "Dungeon.Content", "Dungeon.Gameplay"]`
  - `includePlatforms = []`、`autoReferenced = true`、`noEngineReferences = false`
- 未改任何 `.cs`、场景、预制体、`.asset`。

## 验证

| 检查 | 实测 | 证据 |
|---|---|---|
| 编译 | 0 `error CS` | `compile.md` |
| `M7-3.Asmdef`（stage 2） | PASS（Core/Content/Gameplay/App 字段一致） | `verify.txt` |
| `M7-3.EditorOnly` | PASS | `verify.txt` |
| `M7-3.NoAsset` | PASS（仅 4 个 asmdef 路径） | `verify.txt` |
| `M7-3.Files` | PASS（81 / 30） | `verify.txt` |
| `REFACTOR`（run38） | 20/0，`code=0` | `suites.md` |
| `SPEC01`（run57） | 12/0，`code=0` | `suites.md` |
| 审计 | `upward_layer_edges=0` | `audit/stats.json` |
