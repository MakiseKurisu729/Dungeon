# 子项 1 证据摘要（升级边 → ProgressionState）

## 范围

| 项 | 内容 |
|---|---|
| 新增 | `Assets/_Project/Scripts/Core/ProgressionState.cs`（`Dungeon.Core`，`Level` 默认 1） |
| 修改 | `App/MetaState.cs`：`Level` 改为转发 `ProgressionState.Level`（`TryLevelUp`/`LevelCost` 未改数值） |
| 修改 | `Gameplay/Combat/MeleeCombat.cs`：`MetaState.Level` → `ProgressionState.Level`；删 `using Dungeon.App;` |
| 测试 | `REFACTOR_ProbeRunner.cs`：新增第 17 条 `Refactor.ProgressionFacade` |

## red → green

| 阶段 | REFACTOR | 说明 |
|---|---|---|
| red（`red/`，run27） | 16/1，`code=2` | 失败 ID：`Refactor.ProgressionFacade` |
| green（`green/`，run28） | 17/0，`code=0` | — |

## 其它验证

| 检查 | 实测 | 证据 |
|---|---|---|
| 编译 | 0 `error CS` | `compile.md` |
| `SPEC01`（run51） | 12/0，`code=0` | `green/` |
| `audit_scripts.py`（M7-2 副本） | `file_count=78`；`upward_layer_edges=**10**`（11→10，符合子项 1 预期） | `audit/stats.json`、`audit/coverage.txt` |
| `verify_m7_2.py --stage 1` | 6 PASS / 0 FAIL（`Files=78`、`UpwardEdges=10`、`NoAsset` 白名单含 REFACTOR runner） | `verify.txt`、`ExitCode.txt` |

## 语义保持

- `Level` 默认 1（`ProgressionState.Level` 初始化器）→ 跨场景保留、不自动重置。
- `MetaState.TryLevelUp` 仍 `+1 级 / 扣 50*Level / dmgMult +0.25 / maxHp +20 / MessageFeed.RaiseChanged`。
- `MeleeCombat` 新单位 `dmgMult` 仍按 `(Level-1)*0.25` 补。
