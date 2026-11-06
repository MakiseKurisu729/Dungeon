# 批 1 套件证据（AC-2 / AC-3）

## REFACTOR

- 触发：`execute_menu_item("Tools/QA/In-Editor/REFACTOR")`
- 证据目录：`Docs/QA/Evidence/REFACTOR-M4/editor_run22/`
- `REFACTOR_Summary_run1.json`：`pass=16 fail=0 blocked=0 infraFail=0 observed=0`
- `InEditorResult.txt`：`task=REFACTOR code=0 finishedAt=2026-11-06 19:42:23.560`
- `REFACTOR_ExitCode_run1.txt`：`0`
- 本目录副本：`suite-refactor/`

## SPEC01

- 触发：`execute_menu_item("Tools/QA/In-Editor/SPEC01 (green)")`
  - 注：菜单实际名带 ` (green)`；最初按工单字面 `SPEC01` 调用返回“invalid/disabled”，改按 menu-items 实查名称后成功。
- 证据目录：`Docs/QA/Evidence/SPEC-01/editor_run46/`
- `SPEC01_Summary_run1.json`：`pass=12 fail=0 blocked=0 infraFail=0 observed=0`
- `InEditorResult.txt`：`task=SPEC01 code=0 finishedAt=2026-11-06 19:43:55.538`
- `SPEC01_ExitCode_run1.txt`：`0`
- 本目录副本：`suite-spec01/`

## 与基线对比

| 套件 | 基线 | 批 1 实测 | 结论 |
|---|---|---|---|
| `REFACTOR` | 16/0 | 16/0 | 一致 |
| `SPEC01` | 12/0 | 12/0 | 一致 |
