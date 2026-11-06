# 批 1 编译证据（AC-1 / M7-1.Compile）

- 时间：2026-11-06 批 1 代码改动完成后（REFACTOR 启动于 19:41:44 之前）
- 动作：`refresh_unity(wait_for_completion=true, compile=request)` → 轮询 `mcpforunity://editor/state`
- 结果：
  - `is_compiling=false`
  - `is_domain_reload_pending=false`
  - `active_scene.path=Assets/Scenes/DungeonWorld.unity`（`manage_scene.get_active`：`isDirty=false`）
  - `read_console(action=get, types=["error"], count="200")` 于刷新后即时查询：**0 条**
- Unity Editor.log 佐证（全量日志）：
  - 最后一条 `error CS` 出现在行 `220962`（早于本次编译段）
  - 最后一次 `Finished compiling graph` 出现在行 `231601`，其后无任何 `error CS`
  - 原始行摘录：`compile-editorlog-tail.txt`
- 说明：随后进入 Play 跑套件时 Unity 清空了 Console，因此编译期 Console 计数以“刷新后即时查询”为准；套件能完整加载并跑完 16/12 条断言本身也证明程序集编译成功。

## 编译完成时的 Console 全量快照（套件运行前）

- `read_console(types=["all"], count="60")`：仅 1 条 MCP WebSocket 提示（`[WebSocket] Unexpected receive error: WebSocket is not initialised`），与本批代码无关。
- 无 `error CS`、无 `NullReferenceException`、无 `Missing script`。
