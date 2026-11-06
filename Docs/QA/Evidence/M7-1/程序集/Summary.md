# M7-1 程序集步骤证据摘要（Core / Content asmdef）

## 范围

| 项 | 数量 | 明细 |
|---|---|---|
| 新增 asmdef | 2 | `Core/Dungeon.Core.asmdef`（`references=[]`）、`Content/Dungeon.Content.asmdef`（`references=["Dungeon.Core"]`） |
| 新增 `.meta` | 2 | 由 Unity 生成（未手写） |
| 代码改动 | 0 | 本步只加程序集边界 |

## 验证结果

| 检查 | 结果 | 证据 |
|---|---|---|
| 编译 | 0 `error CS` | `compile.md`、`compile-editorlog-tail.txt` |
| `verify_m7_1.py --stage 4` | 9 PASS / 0 FAIL（ExitCode=0） | `verify.txt`、`ExitCode.txt` |
| `M7-1.Structure.Asmdef` | PASS（name/rootNamespace/references/autoReferenced/noEngineReferences/includePlatforms） | `verify.txt`、`asmdef.md` |
| Core 越界引用 | `Core → 非 Core` 边 **0** | `asmdef.md`（基于 `edges.csv`） |
| Content 越界引用 | `Content → {非 Core,非 Content}` 边 **0** | `asmdef.md`（基于 `edges.csv`） |
| `audit upward_layer_edges` | **11**（不增） | `audit/stats.json` |
