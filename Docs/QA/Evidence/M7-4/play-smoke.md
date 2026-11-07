# M7-4 Play 30 秒冒烟

- 时间：2026-11-07 10:3x（本地）
- 场景：`Assets/Scenes/DungeonWorld.unity`（进入前 `isDirty=false`）
- 操作：`manage_editor(action=play)` → 30 秒 → `read_console` → `manage_editor(action=stop)`
- 结果：
  - Console 2 条普通 Log：`[DebugTestTools] host created...`、`0 个身体部件已挂到 PlayerBody 层`
  - `error CS` **0**、`Missing script` **0**、`NullReferenceException` **0**、`warning CS` **0**
  - 退出后 `is_playing=false`、`is_compiling=false`、场景 `isDirty=false`
