# 子项 2（暂停边）：套件证据（red → green）

## red（实现前）

- 目录：`red/`
- `REFACTOR-M4/editor_run29`：`pass=17 fail=1`，`REFACTOR_ExitCode_run1.txt = 2`
- 失败断言：`Refactor.PauseFacade`（`setOn=False; onPause=False`）

## green（实现后）

- 目录：`green/`
- `REFACTOR-M4/editor_run30`：`pass=18 fail=0`，`code=0`
- `SPEC-01/editor_run52`：`pass=12 fail=0`，`code=0`

| 套件 | 期望 | 实测 |
|---|---|---|
| `REFACTOR` | 18/0 | 18/0 |
| `SPEC01` | 12/0 | 12/0 |
