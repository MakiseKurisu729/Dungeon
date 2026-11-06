# REPORT WF-04 — 接入 Unity MCP（保持编辑器常开）

| 字段 | 值 |
|---|---|
| 任务 | `Docs/Handoff/TASK_WF-04.md` |
| 风险等级 | 高风险（新增项目依赖 + 本机工具；未改玩法代码/资产） |
| 基线提交 | `a7817b55`（执行前后 HEAD 一致） |
| 执行时间 | 2026-11-05 |
| 执行者 | OpenCode + DeepSeek（执行者角色） |
| 结论 | 代码/配置侧完成；Unity 侧启动与 OpenCode 重启待所有者完成，**未声称已连通** |

## 1. 结果摘要

- `Packages/manifest.json` 已加入 `com.coplaydev.unity-mcp`，URL 为 `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#v10.0.0`（按工单；tag 未变更，未退化为 `#main`）。
- 本机已安装 `uv 0.13.7`（`C:\Users\lll\.local\bin\uv.exe`，同目录 `uvx`/`uvw`）。
- 根 `opencode.json` 已新增 `mcp.unity`（remote，`http://localhost:8080/mcp`，`enabled: true`），`providers` 两段原样保留。
- 未触碰 `Assets/**`、`ProjectSettings/**`、任何 `.cs`/场景/预制体/Animator/`.asset`、`Packages/packages-lock.json`、`CURRENT.md`。
- **未关闭 Unity、未运行 batchmode**（执行前后 Unity 进程 1、`Temp/UnityLockfile` 存在）。
- 包解析尚未发生：检查时 `Library/PackageCache` 无 coplay 目录、`packages-lock.json` 无 `coplaydev` 条目 —— 因 Unity 未获焦点不会自动 refresh，属所有者切回 Unity 后的正常步骤。
- 未做任何 git 写操作（全部 git 调用均为只读：`status` / `diff` / `rev-parse` / `ls-remote`）。

## 2. 证据锚点（AC 回填）

| AC | 断言 | 结果 | 证据 |
|---|---|---|---|
| AC-1 | manifest JSON 合法且含 `com.coplaydev.unity-mcp` | **PASS** | `Docs/QA/Evidence/WF-04/manifest.txt`（`ConvertFrom-Json: OK`；`com.coplaydev.unity-mcp = ...#v10.0.0`；依赖数 54；第 3 行命中） |
| AC-2 | uv 可用且版本满足 | **PASS**（改写说明见下） | `Docs/QA/Evidence/WF-04/uv.txt`（`uv 0.13.7`、`uvx 0.13.7`，路径存在） |
| AC-3 | `opencode.json` 合法、`providers` 保留、`mcp.unity` 存在 | **PASS** | `Docs/QA/Evidence/WF-04/opencode.txt` + `diff.txt`（`type=remote`、`url=http://localhost:8080/mcp`、`enabled=True`；`providers = opencode,opencode-go`，两侧 `deepseek-v4.1-flash` 均在） |
| AC-4 | OpenCode 能看到 Unity 工具并成功 `read_console` | **PENDING（未执行）** | `Docs/QA/Evidence/WF-04/mcp_smoke.txt` 标为 PENDING，未填任何“已连通”结论；需所有者启动 server + 重启会话 |
| AC-5 | 无 `Assets/**`、`ProjectSettings/**`、玩法代码改动 | **PASS** | `Docs/QA/Evidence/WF-04/git_status.txt`（禁止路径过滤结果 `NONE`） |

> AC-2 原文“版本 ≥ 3.10 对应版本可用”存在歧义：`uv` 自身版本为 0.13.7（2026-11-03 构建），而 `3.10` 指的是 uv 管理的 Python 版本线。已按“uv 已安装且可管理 Python 依赖”验证，并在此说明，不在 REPORT 里替工单改写断言。

### 附加核对（超出 AC，用于降低解析失败风险）

| 项 | 方法 | 结果 |
|---|---|---|
| tag `v10.0.0` 存在 | `git ls-remote --tags`（只读） | 存在（`856d85e...`） |
| 子路径 `/MCPForUnity` 存在 | GitHub API `contents?ref=v10.0.0` | 存在 |
| 包名/版本/兼容性 | 拉取 `MCPForUnity/package.json` | `name=com.coplaydev.unity-mcp`、`version=10.0.0`、`unity=2021.3`（本机 2022.3.62f3c1 满足） |
| 额外传递依赖 | 同上 | 会引入 `com.unity.nuget.newtonsoft-json@3.0.2`（registry 包），Unity 解析时自动下载 |

## 3. 改动清单

| 文件 | 类型 | 内容 |
|---|---|---|
| `Packages/manifest.json` | 改（+1 行） | `dependencies` 首项新增 `"com.coplaydev.unity-mcp": "https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#v10.0.0"` |
| `opencode.json` | 改（+7 行） | 顶层新增 `mcp.unity`（remote / `http://localhost:8080/mcp` / enabled）；`providers` 未动 |
| `Docs/QA/Evidence/WF-04/manifest.before.json` | 新增 | manifest 改动前备份 |
| `Docs/QA/Evidence/WF-04/packages-lock.before.json` | 新增 | lock 改动前备份（仅作证据） |
| `Docs/QA/Evidence/WF-04/opencode.before.json` | 新增 | opencode 改动前备份 |
| `Docs/QA/Evidence/WF-04/manifest.txt` / `uv.txt` / `opencode.txt` / `git_status.txt` / `diff.txt` / `mcp_smoke.txt` | 新增 | 校验输出与 diff |
| `Docs/Handoff/REPORT_WF-04.md` | 新增 | 本报告 |

本机副作用（非仓库）：`C:\Users\lll\.local\bin\{uv.exe,uvx.exe,uvw.exe}`；`%USERPROFILE%\.local\bin` 未加入当前 shell 的 PATH（安装器提示重启 shell 生效）。

## 4. 测试数据

- manifest 依赖计数：54（改前 53，+1）。
- `ConvertFrom-Json`：manifest OK / opencode OK（两次独立解析）。
- `git diff --stat`（本次相关）：`Packages/manifest.json | 1 +`、`opencode.json | 7 +`。
- `git status --porcelain` 全量（执行后）：
  ```
   M Assembly-CSharp-Editor.csproj
   M Assembly-CSharp.csproj
   M Docs/Handoff/CURRENT.md
   M Packages/manifest.json
   M opencode.json
  ?? Docs/Handoff/REVIEW_ANIMATOR.md
  ?? Docs/Handoff/TASK_SKILL-ANIM-02.md
  ?? Docs/Handoff/TASK_WF-04.md
  ?? Docs/QA/Evidence/WF-04/
  ```
  → `.csproj` 两处与 `CURRENT.md`、两份 TASK/REVIEW 均为**执行前已存在**的改动（开工前快照一致），非本单产生。本单新增仅为 manifest、opencode 与证据目录。
- 禁止路径过滤：`Assets/`、`ProjectSettings/`、`*.cs`、`Packages/packages-lock.json` → `NONE`。
- Unity 状态（执行全程）：进程数 1，`Temp/UnityLockfile` 存在；未运行 batchmode。
- Unity 包解析：`Library/PackageCache` 无 `coplay` 目录，`packages-lock.json` 无 `coplaydev` 条目（16:0x 检查）→ 待所有者切回 Unity 触发。

## 5. 实现原理（3 个定位点）

1. `Packages/manifest.json:3` —— 以 git URL + `?path=` 子目录 + `#v10.0.0` tag 的方式声明包；UPM 对 git 依赖按 tag 解析，tag 一旦被重写会改变解析结果，故保留标签而非 `main`，可复现。
2. `opencode.json:23-29` —— `mcp.unity` 声明为 `remote` 传输，OpenCode 启动时按 `http://localhost:8080/mcp` 以 HTTP 连接 MCP server；`enabled: true` 使其默认加载。配置在会话启动时读取，因此改完必须重启 OpenCode 会话（这正是 AC-4 卡在所有者侧的原因）。
3. `Docs/QA/Evidence/WF-04/mcp_smoke.txt` —— 把“执行者能证实”与“所有者才能证实”分开落盘，避免出现无原始输出的连通性结论。MCP 侧链路为：Unity 编辑器内 C# 桥接包（`MCPForUnity`）↔ 由 `uv`/`uvx` 启动的本机 Python server ↔ OpenCode 的 remote MCP client；三段中执行者只完成了第三段的配置与第一段的依赖声明。

## 6. 模块导航

| 关注点 | 位置 |
|---|---|
| Unity 包依赖声明 | `Packages/manifest.json`（`dependencies`，首项） |
| 解析结果（Unity 写，禁止手改） | `Packages/packages-lock.json`、`Library/PackageCache/com.coplaydev.unity-mcp@*` |
| Unity 侧入口 | 菜单 `Window → MCP for Unity`（Configure All Detected Clients / Start Server） |
| AI 侧入口 | `opencode.json` → `mcp.unity` |
| 本单证据 | `Docs/QA/Evidence/WF-04/` |

## 7. 未决项

1. **AC-4 未验证**：需所有者完成 Unity 侧启动与会话重启。执行者不代填。
2. **包解析未确认**：本次检查时 PackageCache/lock 尚无条目。若所有者切回 Unity 后出现 tag 或路径报错，按工单回退为 `#main`；若出现 `newtonsoft-json` 下载失败，属网络问题，非配置错误。
3. **端口未确认**：默认 8080 未经实机核对。若 Unity 窗口显示其它端口，需改 `opencode.json:26` 的 `url` 再重启会话。
4. **PATH**：`%USERPROFILE%\.local\bin` 未在当前 shell 的 PATH 中；Unity 侧 Configure 若要求 `uv` 在 PATH，可能需要重启 Unity/终端或手工加入 PATH（安装器已提示）。此项可能影响 AC-4，请留意。
5. **安全**：MCP HTTP 无鉴权，仅 `localhost`；不要把端口暴露到网络。
6. **讲稿不适用**（按工单 §5）：纯开发工具链接入，不改变玩家可见功能；无玩法/架构不变量变更，故未写 `Docs/Learning/WF-04_讲稿.md`。

## 8. 所有者逐步操作清单

1. 切回 Unity 窗口（触发 Package Manager 刷新）→ 等待右下角包导入/编译结束；`Window → Package Manager` 里应出现 **MCP for Unity 10.0.0**，Console 无编译错误。
   - 若报 `v10.0.0` 找不到：把 `Packages/manifest.json:3` 的 `#v10.0.0` 改成 `#main` 保存。
2. `Window → MCP for Unity` → 点 **Configure All Detected Clients**（写各客户端配置），再点 **Start Server**。
3. 看窗口里显示的 host/port（默认 `localhost:8080`）。
   - 若不是 8080：把 `opencode.json:26` 的 `url` 改为 `http://localhost:<实际端口>/mcp`。
4. **重启 OpenCode 会话**（配置只在启动时读取）。
5. 在新会话让我执行连通性测试：
   - `read_console`（读 Console 末尾若干条）
   - `execute_menu_item`（如 `Window/MCP for Unity` 或任一无害菜单）
6. 把 3/5 的实际输出贴进 `Docs/QA/Evidence/WF-04/mcp_smoke.txt` 的 `ACTUAL` 段，AC-4 才算闭环。
7. 确认 `git status` 中 `Packages/packages-lock.json` 出现 `coplaydev` 条目（由 Unity 自动写入，**不要手改**），再一起提交。

## 9. 建议提交

标题：
```
chore(mcp): 接入 Unity MCP v10.0.0 与 opencode mcp.unity 配置
```

正文：
```
为在 Unity 常开时让 AI 操作编辑器，加入 Coplay unity-mcp 包并配置本机 remote MCP 端点；同时落盘证据与回滚备份。
```

提交时注意：代码/资产类改动由所有者亲自提交；`Docs/QA/Evidence/WF-04/` 与两份 JSON 一并提交；`Assembly-CSharp*.csproj` 与 `CURRENT.md` 的 pre-existing 改动请自行决定是否包含。
