# 子项 1（升级边）：套件证据（red → green）

## red（实现前）

- 目录：`red/`
- `REFACTOR-M4/editor_run27`：`pass=16 fail=1 blocked=0`，`REFACTOR_ExitCode_run1.txt = 2`
- 失败断言：`Refactor.ProgressionFacade`（`forward=False; backward=False; cost=False; upgrade=False`）
- 说明：`ProgressionState` 已存在但 `MetaState.Level` 尚未转发，断言按预期失败。

## green（实现后）

- 目录：`green/`
- `REFACTOR-M4/editor_run28`：`pass=17 fail=0 blocked=0`，`InEditorResult.txt` `task=REFACTOR code=0`
- `SPEC-01/editor_run51`：`pass=12 fail=0 blocked=0`，`code=0`

## 与验收口径

| 套件 | 期望 | 实测 |
|---|---|---|
| `REFACTOR` | 17/0 | 17/0 |
| `SPEC01` | 12/0 | 12/0 |
