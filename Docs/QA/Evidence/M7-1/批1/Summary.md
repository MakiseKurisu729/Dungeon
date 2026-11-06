# M7-1 批 1 证据摘要（Core + Content）

> 所有者 2026-11-06 指示：本轮**只做批 1，提交后立即停止**。本目录仅覆盖批 1。

## 范围

| 项 | 数量 | 明细 |
|---|---|---|
| 移动 `.cs`（含 `.meta`） | 6 | `Units/Team.cs→Core/`、`Combat/ICharacterCombat.cs→Core/`、`Framework/Cooldown.cs→Core/`、`Units/UnitStats.cs→Content/`、`Combat/WeaponData.cs→Content/`、`Items/ItemData.cs→Content/` |
| 原地加命名空间 | 4 | `Core/EventBus.cs`、`Core/UnitId.cs`、`Core/MessageFeed.cs`、`Core/RunState.cs`（均原地） |
| 命名空间合计 | 10 | `Dungeon.Core` ×7、`Dungeon.Content` ×3 |
| 补 `using` 的引用方 | 48 | 产品代码 39 + 测试 9（按 `usings` 扫描 + 编译 0 error 校验） |
| 可见性改动 | 1 | `MessageFeed.RaiseChanged`：`internal static` → `public static`（签名其余不变） |
| 新增文件夹 | 1 | `Assets/_Project/Scripts/Content/`（`.meta` 由 Unity 生成） |

## 验证结果

| 检查 | 结果 | 证据 |
|---|---|---|
| 编译 | 0 `error CS`；`is_compiling=false`、`is_domain_reload_pending=false` | `compile.md`、`compile-editorlog-tail.txt` |
| `verify_m7_1.py --stage 1` | 9 PASS / 0 FAIL（ExitCode=0） | `verify.txt`、`ExitCode.txt` |
| `audit_scripts.py` | `file_count=77`；`layer_file_counts=Core7/Content3/Gameplay50/App6/UI10/Editor1`；`upward_layer_edges=11`（不增） | `audit/stats.json`、`audit/coverage.txt`、`audit/audit-output.txt` |
| `REFACTOR` | 16 pass / 0 fail；`code=0` | `suites.md`、`suite-refactor/` |
| `SPEC01` | 12 pass / 0 fail；`code=0` | `suites.md`、`suite-spec01/` |
| `.meta` 成对 + GUID | 6 个移动文件全部 `guid_bad=[]`；无孤儿 `.cs.meta` | `verify.txt` 的 `M7-1.Structure.Meta` |
| `M7-1.NoLogicChange` | 0 problems（按 base 对象逐文件比对归一化正文） | `verify.txt` |
| `M7-1.RaiseChanged` | `public static void RaiseChanged()` found=True | `verify.txt` |

## 红 → 绿

- 红基线：`verify-red-baseline.txt`（3 FAIL：Files/Namespace/RaiseChanged，ExitCode=1）
- 绿：`verify.txt`（9 PASS，ExitCode=0）

## 文件计数（本批结束）

- `Assets/_Project/Scripts/**/*.cs` = 77
- 目录计数：Core 15 / Content 3 / Units 16 / Combat 14 / Items 2 / Player 5 / System 6 / Debug 3 / UI 10 / Gameplay/Interaction 3
- `Assets/_Project/Tests/**/*.cs` = 30（本批仅补 9 个文件的 `using`，无 namespace）

## 备注

- MCP `manage_asset(action="move")` 首次调用实际成功但返回失败文案；随后按 D16 退化为“文件系统 `.cs`+`.cs.meta` 成对移动 + `refresh_unity`”，移动前后 `.meta` 字节比对一致（GUID 不变）。
- 本批未跑 Play 冒烟（按所有者“做完批 1 即停”的指令，且 Play 冒烟原定批 2/批 4）。
