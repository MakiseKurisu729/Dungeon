# M7-3 UI 程序集：证据摘要

## 改动

- 新增 `Assets/_Project/Scripts/UI/Dungeon.UI.asmdef`（+ Unity 生成的 `.meta`）：
  - `name`/`rootNamespace` = `Dungeon.UI`
  - `references = ["Dungeon.Core", "Dungeon.Content", "Dungeon.Gameplay", "Dungeon.App", "UnityEngine.UI"]`
  - `includePlatforms = []`、`autoReferenced = true`、`noEngineReferences = false`
- 未改任何 `.cs`、场景、预制体、`.asset`。

## 验证

| 检查 | 实测 | 证据 |
|---|---|---|
| 编译 | 0 `error CS`（最后一次编译后 0 条；63 条历史错误均早于本次编译） | `compile.md` |
| `M7-3.Asmdef`（stage 3） | PASS（Core/Content/Gameplay/App/UI 字段一致） | `verify.txt` |
| `M7-3.EditorOnly` | PASS（运行时 asmdef 未引用 Editor） | `verify.txt` |
| `M7-3.NoAsset` | PASS（仅 6 个 asmdef 路径） | `verify.txt` |
| `M7-3.Files` | PASS（81 / 30） | `verify.txt` |
| `REFACTOR`（run39） | 20/0，`code=0` | `suites.md` |
| `SPEC01`（run58） | 12/0，`code=0` | `suites.md` |
| 审计 | `upward_layer_edges=0`、`file_count=81` | `audit/stats.json` |
| Play 30s | 0 error / 0 Missing / 0 NRE；场景未脏 | `play-smoke.md` |
