# M7-3 Editor 程序集：证据摘要

## 改动

- 新增 `Assets/_Project/Scripts/Editor/Dungeon.Editor.asmdef`（+ Unity 生成的 `.meta`）：
  - `name`/`rootNamespace` = `Dungeon.Editor`
  - `references = ["Dungeon.Core", "Dungeon.Content", "Dungeon.Gameplay", "Dungeon.App", "Dungeon.UI"]`
  - `includePlatforms = ["Editor"]`、`autoReferenced = true`、`noEngineReferences = false`
- 未改任何 `.cs`、场景、预制体、`.asset`。

## 验证

| 检查 | 实测 | 证据 |
|---|---|---|
| 编译 | 0 `error CS`（最后一次编译后 0 条） | `compile.md` |
| `M7-3.Asmdef`（stage 4） | PASS（6 个 asmdef 字段全部一致） | `verify.txt` |
| `M7-3.EditorOnly` | PASS（`Dungeon.Editor.includePlatforms==["Editor"]`；运行时 asmdef 不引用 Editor） | `editor-only.txt` |
| `M7-3.NoAsset` | PASS（仅 8 个 asmdef 路径） | `verify.txt` |
| `M7-3.Files` | PASS（81 / 30） | `verify.txt` |
| `REFACTOR` ×2 | 20/0、20/0，`code=0` | `suites.md` |
| `SPEC01` ×2 | 12/0、12/0，`code=0` | `suites.md` |
| 审计 | `upward_layer_edges=0`、`file_count=81` | `audit/stats.json` |
| Play 30s | 0 error / 0 Missing / 0 NRE；场景未脏 | `play-smoke.md` |

## Editor-only 校验明细（`editor-only.txt`）

- `Core` / `Content` / `Gameplay` / `App` / `UI`：`includePlatforms=[]`，`references` 均不含 `Dungeon.Editor`
- `Editor`：`includePlatforms=["Editor"]`，`references` 覆盖全部 5 个运行时程序集
- `EDITOR_ONLY=True`、`RUNTIME_NO_EDITOR_REF=True`
