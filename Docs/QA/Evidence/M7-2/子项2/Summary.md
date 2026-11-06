# 子项 2 证据摘要（暂停边 → PauseState）

## 范围

| 项 | 内容 |
|---|---|
| 新增 | `Assets/_Project/Scripts/Core/PauseState.cs`（`IsPaused` + `Set(bool)`；不碰 `timeScale`） |
| 修改 | `App/AppFlow.cs`：`IsPaused` 改为转发；`Pause/Resume/ReloadActiveScene` 调 `PauseState.Set(...)`，`timeScale` 逻辑不变 |
| 修改 | 7 处 Gameplay：`PlayerCommandController` `SelectionManager` `SkillInputController` `BossBrain` `CharacterBrain` `DownedRescue` `MonsterBrain` 改读 `PauseState.IsPaused`，并删 `using Dungeon.App;`（`PlayerCommandController` 补 `using Dungeon.Core;`） |
| 测试 | `REFACTOR_ProbeRunner.cs`：新增第 18 条 `Refactor.PauseFacade`；加 `using PauseState = Dungeon.Core.PauseState;` 消除与 `UnityEditor.PauseState` 的歧义 |

## red → green

| 阶段 | REFACTOR | 说明 |
|---|---|---|
| red（`red/`，run29） | 17/1，`code=2` | 失败 ID：`Refactor.PauseFacade`（`setOn=False; onPause=False`） |
| green（`green/`，run30） | 18/0，`code=0` | — |

## 其它验证

| 检查 | 实测 | 证据 |
|---|---|---|
| 编译 | 0 `error CS`（首次刷新 8 条 `CS0104` 已用测试文件别名修复） | `compile.md` |
| `SPEC01`（run52） | 12/0，`code=0` | `green/` |
| `audit_scripts.py`（M7-2 副本） | `file_count=79`；`upward_layer_edges=**3**`（10→3） | `audit/stats.json`、`audit/coverage.txt` |
| `verify_m7_2.py --stage 2` | 6 PASS / 0 FAIL（`Files=79`、`UpwardEdges=3`） | `verify.txt`、`ExitCode.txt` |

## 语义保持

- `timeScale` 仍只由 `AppFlow.Pause/Resume/ReloadActiveScene` 与 `RunFlow.Tick`（团灭）写；`PauseState` 不碰 `timeScale`。
- 团灭 `timeScale=0` 不置 `IsPaused`（断言 `wipeNotPause=True`）。
- `AppFlow.IsPaused` 保留为转发属性（`PauseManager`/`EscMenuPanel` 继续可用）。
