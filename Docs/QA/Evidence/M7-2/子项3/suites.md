# 子项 3（模态边）：套件证据（red → green）

## red（实现前）

- 目录：`red/`
- `REFACTOR-M4/editor_run31`：`pass=18 fail=1`，`REFACTOR_ExitCode_run1.txt = 2`
- 失败断言：`Refactor.ModalFacade`（`registered=False`）

## green（实现后）

- 目录：`green/`
- `REFACTOR-M4/editor_run32`：`pass=19 fail=0`，`code=0`
- `SPEC-01/editor_run53`：`pass=12 fail=0`，`code=0`

| 套件 | 期望 | 实测 |
|---|---|---|
| `REFACTOR` | 19/0 | 19/0 |
| `SPEC01` | 12/0 | 12/0 |
