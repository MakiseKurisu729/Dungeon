# M7-1 批 2 证据摘要（Gameplay）

## 范围

| 项 | 数量 | 明细 |
|---|---|---|
| 移动 `.cs`（含 `.meta`） | 47 | `Units` 16→`Gameplay/Units`；`Core/UnitRegistry`→`Gameplay/Units`；`Combat` 14→`Gameplay/Combat`；`Player` 5→`Gameplay/Player`；`System` 6→`Gameplay/World`；`Debug` 2→`Gameplay/World`；`Core/WaveSpawner`→`Gameplay/World`；`Items` 2→`Gameplay/Items` |
| 原地加命名空间 | 3 | `Gameplay/Interaction/{IInteractionTarget,InteractionDefinition,InteractionRunner}` |
| 命名空间合计 | 50 | `Dungeon.Gameplay.{Units17,Combat14,Player5,World9,Items2,Interaction3}` |
| 补 `using` 的引用方 | 46 | 产品代码 35 + 测试 11 |
| 新增文件夹 | 5 | `Scripts/Gameplay/{Units,Combat,Player,World,Items}`（`.meta` 由 Unity 生成） |

## 验证结果

| 检查 | 结果 | 证据 |
|---|---|---|
| 编译 | 0 `error CS`；`is_compiling=false`、`is_domain_reload_pending=false` | `compile.md`、`compile-editorlog-tail.txt` |
| `verify_m7_1.py --stage 2` | 9 PASS / 0 FAIL（ExitCode=0） | `verify.txt`、`ExitCode.txt` |
| `audit_scripts.py` | `file_count=77`；`layer_file_counts=Core7/Content3/Gameplay50/App6/UI10/Editor1`；`upward_layer_edges=**11**`（不增） | `audit/stats.json`、`audit/coverage.txt`、`audit/audit-output.txt` |
| `REFACTOR` | 16 pass / 0 fail；`code=0` | `suites.md`、`suite-refactor/` |
| `SPEC01` | 12 pass / 0 fail；`code=0` | `suites.md`、`suite-spec01/` |
| Play 30 秒 | 0 `error CS` / 0 `Missing script` / 0 `NullReferenceException`；场景未脏 | `play-smoke.md` |
| `.meta` 成对 + GUID | 47 个移动文件全部 `guid_bad=[]`；无孤儿 `.cs.meta` | `verify.txt` 的 `M7-1.Structure.Meta` |
| `M7-1.NoLogicChange` | 0 problems | `verify.txt` |

## 文件计数（本批结束）

- `Assets/_Project/Scripts/**/*.cs` = 77
- 目录计数：Core 13 / Content 3 / Gameplay/Units 17 / Gameplay/Combat 14 / Gameplay/Player 5 / Gameplay/World 9 / Gameplay/Items 2 / Gameplay/Interaction 3 / Debug 1 / UI 10
- `Assets/_Project/Tests/**/*.cs` = 30（本批补 11 个文件 `using`，无 namespace）

## 备注

- 仍采用“文件系统 `.cs`+`.cs.meta` 成对移动 + `refresh_unity`”，移动前后 `.meta` 字节一致。
- `Scripts/Framework`、`Scripts/Monster` 为迁移后空目录（无 `.cs`、无 `.meta`），按所有者指示未删除、未处理。
