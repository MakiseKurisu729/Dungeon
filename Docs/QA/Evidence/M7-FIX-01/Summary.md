# M7-FIX-01 证据索引

> 基线 `1c013dc4`；Unity `2022.3.62f3c1`；模式 B，实例 `Dungeon@4f5c34c82f31646e`。
> 资产改动清单与批准记录：`资产改动清单.md`；执行读回：`readback.txt`。

## 1. 资产 wiring

| 项 | 证据 | 结果 |
|---|---|---|
| Esc 面板接线 / 宝箱 / 门 / fader | `wiring.txt`、`wiring_check.py` | **15 PASS / 0 FAIL**，ExitCode=0 |
| 执行读回（含 NavMesh） | `readback.txt` | entry `(4,0.08,5)` `onNavMesh=True` |

## 2. 四套件回归（各连续 2 次）

| 套件 | run1 | run2 | code | 结论 |
|---|---|---|---|---|
| `REFACTOR` | `REFACTOR-M4/editor_run44` 20/0 | `editor_run45` 20/0 | 0 / 0 | 一致 |
| `SPEC01` | `SPEC-01/editor_run63` 12/0 | `editor_run64` 12/0 | 0 / 0 | 一致 |
| `FND00B06` | `FND-00B-06/editor_run13` 12/0+2obs | `editor_run14` 12/0+2obs | 0 / 0 | 一致 |
| `DIAG` | `DIAG-SKILL/editor_run15` 6/1 | `editor_run16` 6/1 | 2 / 2 | 一致；唯一红为已知 `Diag.Input.FirstExit` |

> 原始 `Summary` / `Assertions.jsonl` / `InEditorResult.txt` 在各自 canonical 目录（只追加未覆盖）。

## 3. 未覆盖（真实键鼠）

- Esc 键路径、宝箱鼠标点击、门 F 交互需要真实输入注入；MCP 无法注入键鼠事件，留 3 项目检（见 REPORT 未决项）。
- 场景无 `EventSystem`，新增 3 个按钮当前无法接收鼠标点击；补 `EventSystem` 属清单外，待批准。

## 4. 结论

- 自动化：**未回归**（四套件与基线一致；`DIAG` 已知红不变）。
- 结构：Esc/宝箱/门/fader 的 wiring 全部到位；NavMesh 落点有效。
- 人工功能项：Esc、宝箱点击、门 F **未验证**（需目检）。
