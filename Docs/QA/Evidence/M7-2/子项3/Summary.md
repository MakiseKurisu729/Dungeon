# 子项 3 证据摘要（模态边 → IUiModalState）

## 范围

| 项 | 内容 |
|---|---|
| 新增 | `Assets/_Project/Scripts/Gameplay/IUiModalState.cs`：`IUiModalState` 接口 + `UiModalState.Current` 注册点（`Dungeon.Gameplay`） |
| 修改 | `UI/UIManager.cs`：实现 `IUiModalState`（`IsModalOpen => IsInventoryOpen`）；`Awake` 注册、`OnDestroy` 注销 |
| 修改 | `Gameplay/Player/PlayerCommandController.cs`、`SkillInputController.cs`：改读 `UiModalState.Current?.IsModalOpen`，删 `using Dungeon.UI;` |
| 测试 | `REFACTOR_ProbeRunner.cs`：新增第 19 条 `Refactor.ModalFacade`；补 `using Dungeon.Gameplay;` |

## red → green

| 阶段 | REFACTOR | 说明 |
|---|---|---|
| red（`red/`，run31） | 18/1，`code=2` | 失败 ID：`Refactor.ModalFacade`（`registered=False`） |
| green（`green/`，run32） | 19/0，`code=0` | — |

## 其它验证

| 检查 | 实测 | 证据 |
|---|---|---|
| 编译 | 0 `error CS`（首次 8 条 `CS0103` 已补测试 using 修复） | `compile.md` |
| `SPEC01`（run53） | 12/0，`code=0` | `green/` |
| `audit_scripts.py`（M7-2 副本） | `file_count=80`；`upward_layer_edges=**1**`（3→1，只剩门过场） | `audit/stats.json`、`audit/coverage.txt` |
| `verify_m7_2.py --stage 3` | 6 PASS / 0 FAIL（`Files=80`、`UpwardEdges=1`） | `verify.txt`、`ExitCode.txt` |

## 语义保持

- `IUiModalState.IsModalOpen` 严格等于 `UIManager.IsInventoryOpen`，未扩大为 `IsAnyPanelOpen`。
- `UIManager.Awake` 注册、`OnDestroy` 注销；未注册时 Gameplay 的读法为 `Current != null && Current.IsModalOpen`，不阻塞。
