# M7-1 批 3 证据摘要（App）

## 范围

| 项 | 数量 | 明细 |
|---|---|---|
| 移动 `.cs`（含 `.meta`） | 6 | `Core/{AppFlow,GameData,GameFlow,MetaState,PauseManager,RunFlow}.cs` → `App/` |
| 命名空间 | 6 | `Dungeon.App` |
| 补 `using` 的引用方 | 18 | 产品代码 12 + 测试 6 |
| 新增文件夹 | 1 | `Scripts/App/`（`.meta` 由 Unity 生成） |
| Core 最终化 | 7 | `EventBus` `UnitId` `MessageFeed` `RunState` `Team` `ICharacterCombat` `Cooldown`（全部 `Dungeon.Core`） |

## 验证结果

| 检查 | 结果 | 证据 |
|---|---|---|
| 编译 | 0 `error CS`；`is_compiling=false`、`is_domain_reload_pending=false` | `compile.md`、`compile-editorlog-tail.txt` |
| `verify_m7_1.py --stage 3` | 9 PASS / 0 FAIL（ExitCode=0） | `verify.txt`、`ExitCode.txt` |
| `audit_scripts.py` | `file_count=77`；`layer_file_counts=Core7/Content3/Gameplay50/App6/UI10/Editor1`；`upward_layer_edges=**11**` | `audit/stats.json`、`audit/coverage.txt` |
| `REFACTOR` | 16 pass / 0 fail；`code=0` | `suites.md`、`suite-refactor/` |
| `SPEC01` | 12 pass / 0 fail；`code=0` | `suites.md`、`suite-spec01/` |
| `.meta` 成对 + GUID | 6 个移动文件 `guid_bad=[]`；无孤儿 `.cs.meta` | `verify.txt` |
| `M7-1.NoLogicChange` | 0 problems | `verify.txt` |

## 文件计数（本批结束）

- `Assets/_Project/Scripts/**/*.cs` = 77
- 目录计数：Core 7 / Content 3 / Gameplay 50（Units17/Combat14/Player5/World9/Items2/Interaction3）/ App 6 / Debug 1 / UI 10
- Core 目录已最终化，满足建 `Dungeon.Core.asmdef` 的前置。
