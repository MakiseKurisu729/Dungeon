# 子项 4 证据摘要（门过场边 → TransitionRequest 事件）

## 范围

| 项 | 内容 |
|---|---|
| 新增 | `Assets/_Project/Scripts/Gameplay/World/TransitionRequest.cs`（`midAction`，`Dungeon.Gameplay.World`） |
| 修改 | `Gameplay/World/DungeonDoor.cs`：`TransitionFader.Transition(...)` → `EventBus.Publish(new TransitionRequest { midAction = ... })`；删 `using Dungeon.UI;` |
| 修改 | `UI/TransitionFader.cs`：`OnEnable` 订阅、`OnDisable` 退订 `TransitionRequest`；handler 调 `Transition(req.midAction)` |
| 测试 | `REFACTOR_ProbeRunner.cs`：新增第 20 条 `Refactor.DoorTransitionEvent`（运行期 fader 夹具，结束销毁）；头注释 16→20 条 |

## red → green

| 阶段 | 套件 | 结果 |
|---|---|---|
| red（run34，`red/`） | `REFACTOR` | 19/1，`code=2`；失败 ID `Refactor.DoorTransitionEvent` |
| green（run35） | `REFACTOR` | 20/0，`code=0` |
| green（run36） | `REFACTOR` | 20/0，`code=0` |
| green（run54） | `SPEC01` | 12/0，`code=0` |
| green（run55） | `SPEC01` | 12/0，`code=0` |
| green（run10） | `FND00B06` | 12/0 + 2 observed，`code=0` |
| Play 30s | — | 0 error/Missing/NRE |

## 其它验证

| 检查 | 实测 | 证据 |
|---|---|---|
| 编译 | 0 `error CS` | `compile.md` |
| `audit_scripts.py`（M7-2 副本） | `file_count=81`；`upward_layer_edges=**0**`（1→0） | `audit/stats.json`、`audit/coverage.txt` |
| `verify_m7_2.py --stage 4` | 6 PASS / 0 FAIL（`Files=81`、`UpwardEdges=0`） | `verify.txt`、`ExitCode.txt` |

## 语义保持

- `DungeonDoor` 只发事件，不再引用 UI；`TransitionFader` 在 `OnEnable/OnDisable` 成对订阅/退订。
- 过场仍是“黑屏 → midAction（传送/相机）→ 亮屏”，`fadeTime` 默认 0.9s 未改。
- 场景本身没有门（门在其他场景/副本），本子项的自动断言使用运行期 fader 夹具验证事件链路；门内传送逻辑未改，留给目检。
