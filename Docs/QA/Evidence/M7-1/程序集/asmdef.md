# 程序集步骤 evidence（AC-7 / M7-1.Structure.Asmdef）

## asmdef 内容与 GUID

### `Assets/_Project/Scripts/Core/Dungeon.Core.asmdef`

```json
{
    "name": "Dungeon.Core",
    "rootNamespace": "Dungeon.Core",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- `.meta` GUID：`b6b796b44e6748740b0730f5418cc4aa`（Unity 生成，未手写）

### `Assets/_Project/Scripts/Content/Dungeon.Content.asmdef`

```json
{
    "name": "Dungeon.Content",
    "rootNamespace": "Dungeon.Content",
    "references": [
        "Dungeon.Core"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- `.meta` GUID：`ee1c130b68616264f82b9dfc7457222e`（Unity 生成，未手写）

## 依赖边界实测（audit edges.csv）

| 检查 | 结果 |
|---|---|
| `src_layer=Core` 且 `dst_layer≠Core` 的边 | **0** |
| `src_layer=Content` 且 `dst_layer∉{Core,Content}` 的边 | **0** |
| `Dungeon.Core` 文件数 | 7 |
| `Dungeon.Content` 文件数 | 3 |

## 编译

- `refresh_unity(compile=request)` 后：`is_compiling=false`、`is_domain_reload_pending=false`、`error CS` 0 条。
- `verify --stage 4`：`M7-1.Structure.Asmdef` PASS（`name`/`rootNamespace`/`references`/`autoReferenced`/`noEngineReferences`/`includePlatforms` 全部符合）。
- Unity 自动生成的 `.asmdef.meta` 已随本次提交一并提交。
