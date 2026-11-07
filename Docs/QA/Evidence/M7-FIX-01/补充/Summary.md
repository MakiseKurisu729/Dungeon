# M7-FIX-01B 证据索引

> 基线 `b000ae65`；Unity `2022.3.62f3c1`；模式 B，实例 `Dungeon@4f5c34c82f31646e`。
> 清单与批准记录：`资产改动清单_01B.md`；执行读回：`readback_01B.txt`。

## 1. 资产 wiring

| 项 | 证据 | 结果 |
|---|---|---|
| EventSystem / Canvas / chest_gold ChestItem | `wiring_01B.txt`、`../wiring_check.py` | **23 PASS / 0 FAIL** |
| 执行读回 + Play 模式 `Contents` | `readback_01B.txt` | `nonNull=2`（金币袋 ×2） |

## 2. 四套件回归（各连续 2 次）

| 套件 | run1 | run2 | code | 结论 |
|---|---|---|---|---|
| `REFACTOR` | `REFACTOR-M4/editor_run46` 20/0 | `editor_run47` 20/0 | 0 / 0 | 一致 |
| `SPEC01` | `SPEC-01/editor_run65` 12/0 | `editor_run66` 12/0 | 0 / 0 | 一致 |
| `FND00B06` | `FND-00B-06/editor_run15` 12/0+2obs | `editor_run16` 12/0+2obs | 0 / 0 | 一致 |
| `DIAG` | `DIAG-SKILL/editor_run17` 6/1 | `editor_run18` 6/1 | 2 / 2 | 一致；已知红不变 |

## 3. 未覆盖（真实键鼠）

- Esc 面板按钮点击、宝箱点击开箱、门 F 过场：需真实输入，留 3 项目检。

## 4. 结论

- UI 事件系统：`EventSystem + StandaloneInputModule` 已加，无 `InputSystemUIInputModule`；Canvas 既有 `GraphicRaycaster`（no-op）。
- 宝箱：`chest_gold` 开箱非空；**实为金币袋 ×2**（嵌套 `coin_stack_medium` 原本就有 1 个，本单按清单又加 `Loot_Gold` 1 个）。
- 自动化：四套件与基线一致，未回归。
- 人工功能项仍待目检。
