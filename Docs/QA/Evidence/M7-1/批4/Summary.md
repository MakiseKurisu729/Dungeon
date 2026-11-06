# M7-1 批 4 证据摘要（UI / Editor / 测试 using）

## 范围

| 项 | 数量 | 明细 |
|---|---|---|
| UI 原地加命名空间 | 10 | `Dungeon.UI`：`BasePanel` `CursorManager` `EscMenuPanel` `HUD` `InventoryUI` `MainMenuPanel` `SkillBarUI` `SlotClickHandler` `TransitionFader` `UIManager` |
| Editor 移动 + 命名空间 | 1 | `Debug/DebugTestTools.cs` → `Editor/DebugTestTools.cs`，`Dungeon.Editor`（保留 `#if UNITY_EDITOR` 包裹） |
| 补 `using` 的引用方 | 7 | 产品代码 3（PlayerCommandController、SkillInputController、DungeonDoor）+ 测试 4 |
| 测试 `.cs` | 30 | 目录结构与命名空间不变；本批补齐剩余 `using`，编译 0 error |

## 验证结果

| 检查 | 结果 | 证据 |
|---|---|---|
| 编译 | 0 `error CS`；`is_compiling=false`、`is_domain_reload_pending=false` | `compile.md`、`compile-editorlog-tail.txt` |
| `verify_m7_1.py --stage 5`（提交前） | 9 PASS / 0 FAIL（ExitCode=0） | `verify.txt`、`ExitCode.txt` |
| `verify_m7_1.py --stage 5 --diff-scan`（提交后） | 见收口证据 | `verify-after-commit.txt` |
| `audit_scripts.py` | `file_count=77`；`layer_file_counts=Core7/Content3/Gameplay50/App6/UI10/Editor1`；`upward_layer_edges=**11**` | `audit/stats.json`、`audit/coverage.txt` |
| `REFACTOR` ×2 | 均 16 pass / 0 fail；`code=0` | `suites.md`、`suite-refactor-1/`、`suite-refactor-2/` |
| `SPEC01` ×2 | 均 12 pass / 0 fail；`code=0` | `suites.md`、`suite-spec01-1/`、`suite-spec01-2/` |
| Play 30 秒 | 0 `error CS` / 0 `Missing script` / 0 `NullReferenceException`；场景未脏 | `play-smoke.md` |
| `.meta` 成对 + GUID | `Editor/DebugTestTools.cs` 移动 `guid_bad=[]`；无孤儿 `.cs.meta` | `verify.txt` |
| `M7-1.NoLogicChange` | 0 problems | `verify.txt` |

## 文件计数（M7-1 终态）

- `Assets/_Project/Scripts/**/*.cs` = 77
- Core 7 / Content 3 / Gameplay 50（Units17/Combat14/Player5/World9/Items2/Interaction3）/ App 6 / UI 10 / Editor 1
- `Assets/_Project/Tests/**/*.cs` = 30（无 namespace）
