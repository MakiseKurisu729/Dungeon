# M7-3 六个程序集定义汇总（AC-1）

| 程序集 | 文件 | GUID | references | includePlatforms |
|---|---|---|---|---|
| `Dungeon.Core` | `Core/Dungeon.Core.asmdef` | `b6b796b44e6748740b0730f5418cc4aa` | `[]` | `[]` |
| `Dungeon.Content` | `Content/Dungeon.Content.asmdef` | `ee1c130b68616264f82b9dfc7457222e` | `["Dungeon.Core"]` | `[]` |
| `Dungeon.Gameplay` | `Gameplay/Dungeon.Gameplay.asmdef` | `088b351bf9fa1984f89efd688ede7e13` | `["Dungeon.Core","Dungeon.Content","UnityEngine.UI"]` | `[]` |
| `Dungeon.App` | `App/Dungeon.App.asmdef` | `c65dc3be932cead46b4241887b56864c` | `["Dungeon.Core","Dungeon.Content","Dungeon.Gameplay"]` | `[]` |
| `Dungeon.UI` | `UI/Dungeon.UI.asmdef` | `2dd491abae0b09b4abc545b4447bc0bd` | `["Dungeon.Core","Dungeon.Content","Dungeon.Gameplay","Dungeon.App","UnityEngine.UI"]` | `[]` |
| `Dungeon.Editor` | `Editor/Dungeon.Editor.asmdef` | `63198e110f2853843938542843fcfea4` | `["Dungeon.Core","Dungeon.Content","Dungeon.Gameplay","Dungeon.App","Dungeon.UI"]` | `["Editor"]` |

统一字段（全部 6 个）：`rootNamespace` = `name`、`autoReferenced=true`、`noEngineReferences=false`、`excludePlatforms=[]`、`allowUnsafeCode=false`、`overrideReferences=false`、`precompiledReferences=[]`、`defineConstraints=[]`、`versionDefines=[]`。

`verify_m7_3.py --stage 4` 结果：`M7-3.Asmdef` PASS / `M7-3.EditorOnly` PASS / `M7-3.NoAsset` PASS / `M7-3.Files` PASS（4 PASS / 0 FAIL）。
