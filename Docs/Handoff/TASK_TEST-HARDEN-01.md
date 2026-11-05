# TEST-HARDEN-01 — 探针开跑前等编译稳定 + 域重载丢失看门狗

## §0 元信息

| 字段 | 值 |
|---|---|
| 任务 ID | `TEST-HARDEN-01` |
| 风险等级 | `常规`（只改编辑器测试基础设施，不碰产品代码） |
| 授权 | 所有者 2026-11-05 批准 `REPORT_TEST-MCP-01` §7 第 5 条的两个开放子项 |
| 基线提交 | `4645e54d` |
| 回滚锚 | `git checkout -- Assets/_Project/Tests/QA/Editor/QA_InEditorMenu.cs Assets/_Project/Tests/SPEC01/Editor/SPEC01_BatchAcceptance.cs Assets/_Project/Tests/FND00B/Editor/FND00B06_BatchAcceptance.cs Assets/_Project/Tests/DIAG/Editor/DIAG_BatchAcceptance.cs` |
| 状态 | `Ready` |

## §1 需求

| 编号 | 需求 | 验收方式 |
|---|---|---|
| R-1 | 菜单开跑前先 `refresh_unity`，并**轮询到 `is_compiling=false` 连续 2 次**、`is_domain_reload_pending=false`，才进 Play；超过 120s 未稳定则放弃并写失败证据 | `Harden.Preflight` |
| R-2 | 若在 Play 中发生域重载导致 runner 丢失（`started=true && runner==null` 持续 ≥3s），看门狗自动判定本轮失败：写 `InEditorResult.txt`（code=3，reason=`runner-lost-domain-reload`）、打 token、退出 Play，**不退出编辑器** | `Harden.Watchdog` |
| R-3 | 看门狗同样覆盖“进 Play 后 10s 仍未创建 runner”的首启失败 | `Harden.WatchdogStart` |
| R-4 | 正常路径不受影响：SPEC01/FND00B06/DIAG 三套各跑一次，计数与基线一致 | `all` |

**不在范围**：自动重跑、把 runner 换成非 MonoBehaviour 驱动、改 batch 路径、改断言。

## §2 方案

**A. 开跑前 preflight（`QA_InEditorMenu.Run`）**

```text
若 EditorApplication.isCompiling || isPlayingOrWillChangePlaymode → 拒绝并日志
AssetDatabase.Refresh()
循环（最多 120s）：
   if (!EditorApplication.isCompiling && !EditorApplication.isUpdating) 连续两帧成立 → 通过
   每 200ms 重查一次（EditorApplication.update 计时或 delayCall）
超时 → 写 InEditorResult(code=3, reason=preflight-timeout) + 还原环境变量 + return
```

**B. 看门狗（三个 `*_Boot`）**

在既有 `Tick()` 里加：

```text
if (Application.isPlaying && SessionState.GetBool(StartedKey) && 找不到 runner 对象)
    累计超过 3s → WriteResult(code=3, reason="runner-lost-domain-reload") → EditorApplication.isPlaying=false
```

实现细节：`TryStartRunner` 里把创建的 GameObject 名字记到 `SessionState`，`Tick` 用 `GameObject.Find` 或静态引用判断是否存在；找不到即视为丢失（域重载会清静态引用与场景对象）。**不要**自动重建 runner（所有者要求失败要快、要留证据）。

**C. 结果写出**：复用 `QA_InEditorMenu.TryFinish(task, 3)` 的落盘路径；额外写 `InEditorResult.txt` 的 `reason` 字段。

**影响面**

| 项 | 内容 |
|---|---|
| 修改文件 | `QA_InEditorMenu.cs`、`SPEC01_BatchAcceptance.cs`、`FND00B06_BatchAcceptance.cs`、`DIAG_BatchAcceptance.cs` |
| 字符串引用 | `QA.InEditor.*`、`runner-lost-domain-reload`、`preflight-timeout` |
| 场景/预制体/`.meta` | 无 |

## §3 验证

| AC | Given / When | Then | 断言 ID | 证据 |
|---|---|---|---|---|
| AC-1 | 改一个 `.cs` 后立刻点菜单 | 菜单先等编译稳定再进 Play，日志含 `preflight ok (waited=N.Ns)` | `Harden.Preflight` | `preflight.txt` |
| AC-2 | Play 中故意触发一次重编译（改 `.cs`） | 3~5s 内自动退出 Play，`InEditorResult.txt` 有 `runner-lost-domain-reload`，Unity 仍存活 | `Harden.Watchdog` | `watchdog.txt` |
| AC-3 | 三套正常各跑一次 | SPEC01 12/0、FND00B06 12/0+2obs、DIAG 6/1 | `all` | `editor_runN/…` |

**次数**：AC-3 每套 1 次；AC-1/2 各 1 次（故障注入）。

## §4 执行信息

| 项 | 内容 |
|---|---|
| 允许改动 | 上述 4 个 `.cs` + 证据 + REPORT |
| 绝不触碰 | 产品脚本、`Assets/_Project/Art/**`、场景、预制体、`.asset`、`ProjectSettings`、`Packages`、断言方法体 |
| MCP | `execute_code`、`execute_menu_item`（仅 `Tools/QA/In-Editor/*`）、`read_console`、`refresh_unity` |
| 熔断 | 需要改产品代码；域重载后 Unity 被退出；正常三套计数变化 |
| 执行预算 | 实现 1 轮 + 修复 1 轮；故障注入最多 2 次 |

## §5 学习产出

不适用（测试基础设施健壮性），三句话写进 REPORT §3。

## 自包含声明

不依赖 `CURRENT.md` 即可执行。
