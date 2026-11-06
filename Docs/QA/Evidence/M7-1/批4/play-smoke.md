# 批 4 Play 30 秒冒烟（AC-10）

- 时间：2026-11-07 00:33–00:34（本地）
- 场景：`Assets/Scenes/DungeonWorld.unity`（进入前 `isDirty=false`）
- 操作：`manage_editor(action=play)` → 等待 30 秒 → `read_console` → `manage_editor(action=stop)`
- 结果：
  - 退出后 `is_playing=false`、`is_changing=false`、`is_compiling=false`
  - 场景仍 `isDirty=false`（未保存、未改对象）；`get_loaded_scenes` 仅 `DungeonWorld`
  - Console 30 秒内共 2 条（均为普通 Log）：
    1. `[DebugTestTools] host created in DungeonWorld; keys F5/F9/F10`
    2. `0 个身体部件已挂到 PlayerBody 层`
  - `error CS`：**0**
  - `Missing script`：**0**
  - `NullReferenceException`：**0**
  - `warning CS`：**0**
