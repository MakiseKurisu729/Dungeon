# WF-04 — 接入 Unity MCP（保持编辑器常开）

## §0 元信息

| 字段 | 值 |
|---|---|
| 任务 ID | `WF-04` |
| 风险等级 | `高风险`（新增项目依赖 + 本机工具；不改玩法代码/资产） |
| 授权 | 所有者 2026-11-05 明确：“如果 MCP 能解决我的问题，那就用这个方案”；“如果你能做就你直接做” |
| 基线提交 | `a7817b55` |
| 回滚锚 | 删除 `Packages/manifest.json` 依赖 + `opencode.json` 的 `mcp` 段；`uv` 可保留 |
| 前置 | Unity **保持打开**（当前进程 1、锁文件存在）；本单不运行 batchmode、不关闭 Unity |
| 状态 | `Ready` |

## §1 需求（Why / What）

**一句话目标**：让 AI 能通过 MCP 控制已打开的 Unity 编辑器（执行菜单、跑测试、读 Console、改资产），不再需要“关 Unity → batch → 开 Unity”。

**用户故事**：作为所有者，我要在 Unity 常开的情况下让 AI 执行工单和验收；作为 AI，我要能触发编辑器工具并读取结果。

**必须满足**

| 编号 | 需求 | 验收方式 |
|---|---|---|
| R-1 | Unity 项目加入 `com.coplaydev.unity-mcp`（v10.0.0） | `Packages/manifest.json` + PackageCache |
| R-2 | 本机安装 `uv`（Python 3.10+ 由 uv 管理） | `uv --version` |
| R-3 | OpenCode 配置 `mcp.unity` 指向本机 MCP 服务 | `opencode.json` 合法 |
| R-4 | 所有者完成 Unity 侧“Configure/Start Server”后，OpenCode 能列出 Unity 工具 | 会话重启后调用 `read_console` |
| R-5 | 不改 `Assets/**`、`ProjectSettings/**`、玩法代码 | `git status` |

**不在范围**：玩法代码、动画机、场景/预制体、CI、远程访问、MCP 工具二次开发。

**约束**：Coplay MCP 明确支持 Unity 2021.3 LTS–6.x（本项目 2022.3.62f3c1）；MIT；默认 HTTP `localhost:8080`，**无鉴权**，只在本机使用。

## §2 方案（Plan）

**备选方案**

| 方案 | 做法 | 优点 | 缺点 | 结论 |
|---|---|---|---|---|
| A | Coplay MCP（UPM 包 + 本机 server + OpenCode MCP） | 支持 2022.3；工具全；Unity 常开 | 新依赖、需 uv、HTTP 无鉴权 | **选定** |
| B | 官方 `com.unity.ai.assistant` MCP | 官方 | 已弃用、偏 Unity 6、仍需包 | 弃用 |
| C | 菜单工具 + 人工点击 | 零依赖 | AI 不能自己触发 | 备选 |

**实施步骤**

1. **备份**：把 `Packages/manifest.json`、`Packages/packages-lock.json` 复制到 `Docs/QA/Evidence/WF-04/`（只作证据，不入库）。
2. **加包**：在 `Packages/manifest.json` 的 `dependencies` 加：
   `"com.coplaydev.unity-mcp": "https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#v10.0.0"`
   > 若 Unity 报 tag 不存在，改用 `#main` 并在 REPORT 记录。
3. **装 uv**：PowerShell 执行
   `irm https://astral.sh/uv/install.ps1 | iex`
   然后用完整路径验证 `%USERPROFILE%\.local\bin\uv.exe --version`。
4. **配 OpenCode**：在根 `opencode.json` 增加：
   ```json
   "mcp": {
     "unity": {
       "type": "remote",
       "url": "http://localhost:8080/mcp",
       "enabled": true
     }
   }
   ```
   保留现有 `providers` 段不动。
5. **所有者动作**（AI 做不了，MCP 尚未接通）：
   - 切回 Unity，等待包导入/编译；
   - `Window → MCP for Unity`，点 `Configure All Detected Clients` / `Start Server`；
   - 确认窗口显示的地址/端口（默认 `localhost:8080`）；
   - 重启 OpenCode 会话。
6. **验证**：会话重启后调用 Unity MCP 的 `read_console` / `execute_menu_item` 做连通性测试；若端口不是 8080，改 `opencode.json` 后重启。

**回滚**：

- 删 `Packages/manifest.json` 的依赖 → Unity 重新解析；
- 删 `opencode.json` 的 `mcp` 段；
- `uv` 可保留（其他项目可用）或按官方方式卸载。

## §3 验证（Validation）

| AC | Given / When / Then | 断言 / 证据 | 证据 |
|---|---|---|---|
| AC-1 | 给定 manifest，则 JSON 合法且包含 `com.coplaydev.unity-mcp` | `ConvertFrom-Json` + grep | `Docs/QA/Evidence/WF-04/manifest.txt` |
| AC-2 | 给定安装后的 uv，则版本 ≥ 3.10 对应版本可用 | `uv --version` | `.../uv.txt` |
| AC-3 | 给定 `opencode.json`，则 JSON 合法、`providers` 保留、`mcp.unity` 存在 | `ConvertFrom-Json` + diff | `.../opencode.txt` |
| AC-4 | 给定 Unity 已 Configure/Start，则 OpenCode 会话能看到 Unity 工具并成功 `read_console` | 所有者 + 工具输出 | `.../mcp_smoke.txt` |
| AC-5 | 给定本单 diff，则无 `Assets/**`、`ProjectSettings/**`、玩法代码改动 | `git status` | `.../git_status.txt` |

> AC-4 依赖所有者点 Unity 窗口与重启会话；执行者只准备到“配置完成”，不能伪造“已连通”。

## §4 执行信息

| 项 | 内容 |
|---|---|
| 前置 | Unity 保持打开；**不要**运行 batchmode、不要关闭 Unity；网络可访问 GitHub 与 astral.sh |
| 允许改动 | `Packages/manifest.json`、`opencode.json`、`Docs/QA/Evidence/WF-04/`、`Docs/Handoff/REPORT_WF-04.md`；允许安装本机 `uv` |
| 允许读取 | 本工单 + 上述两个 JSON + Unity 的 `Library/PackageCache`（只读） |
| 绝不触碰 | `Assets/**`、`ProjectSettings/**`、任何 `.cs`/场景/预制体/Animator/`.asset`、`Packages/packages-lock.json`（让 Unity 自己写）、`CURRENT.md`、其他 TASK/REPORT |
| 证据目录 | `Docs/QA/Evidence/WF-04/` |
| 熔断 | 网络失败、包解析报错、Unity 崩溃/锁死、需要改资产/代码、需要关闭 Unity |
| 执行预算 | 1 轮；不反复重装 |

## §5 学习产出

| 项 | 要求 |
|---|---|
| 讲稿 | **不适用**：开发工具链，不改变玩家可见功能；REPORT 说明理由 |
| 三句话版本 | ① MCP 让 AI 控制已打开的 Unity；② 项目加了一个 MIT 桥接包 + 本机 uv；③ 默认 localhost:8080，无鉴权，只在本机用 |

## 自包含声明

本工单不依赖 `CURRENT.md` 或其他任务书即可执行；Unity 侧最后一步必须由所有者点击，执行者不得代填“已连通”。
