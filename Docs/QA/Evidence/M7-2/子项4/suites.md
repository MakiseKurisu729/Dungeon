# 子项 4（门过场边）：套件证据（red → green）

## red

- 目录：`red/`（来自 `REFACTOR-M4/editor_run34`）
- `pass=19 fail=1 blocked=0`，`REFACTOR_ExitCode_run1.txt = 2`
- 失败断言：`Refactor.DoorTransitionEvent`（`signal=True; midActionRan=False; peakAlpha=0`）
- 早先的 run33 因场景无 fader 实例被 `blocked`，改用运行期夹具后 run34 才是有效红；run33 保留在 canonical 目录备查。

## green

| 套件 | 运行 | 结果 | code |
|---|---|---|---|
| `REFACTOR` | `editor_run35` | pass=20 fail=0 | 0 |
| `REFACTOR` | `editor_run36` | pass=20 fail=0 | 0 |
| `SPEC01` | `editor_run54` | pass=12 fail=0 | 0 |
| `SPEC01` | `editor_run55` | pass=12 fail=0 | 0 |
| `FND00B06` | `FND-00B-06/editor_run10` | pass=12 fail=0 **observed=2** | 0 |

- 本目录副本：`green/`（含两次 REFACTOR/SPEC01 与 FND 副本）
- 两次 `REFACTOR`、两次 `SPEC01` 的 `pass/fail` 一致。
