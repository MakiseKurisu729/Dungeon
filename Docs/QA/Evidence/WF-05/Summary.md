# WF-05 证据汇总（脚本生成，数值来自本次真实执行）

执行时间: 2026-11-05 18:25:19
执行环境: Windows / PowerShell（Start-Process powershell -File），工作目录 D:\unity\Dungeon
执行方式: 全部为 `Select-String` / `git status` 静态检索；未调用任何 Unity/MCP 工具

| AC | 跑了什么命令 | 期望什么 | 实际什么 | 结论 |
|---|---|---|---|---|
| AC-1 | Select-String AGENTS.md：'模式 A｜batchmode\|模式 B｜MCP 常开编辑器'、'Unity 必须关闭'、'Unity.*必须关闭'、'### 3.2.1 MCP 使用规则' | 模式句 2 行；旧绝对表述 0 行；新标题 1 行 | 模式句 2 行；'Unity 必须关闭' 0 行；'Unity.*必须关闭' 0 行；新标题 1 行 | 通过 |
| AC-2 | Select-String AGENTS.md：'无需逐次批准'、'只读可直接用'，并逐项校验 10 个只读工具/资源名 | 措辞 ≥1 行；名单 ≥5 项命中 | 措辞 1 行、1 行；名单 10/10 项命中 | 通过 |
| AC-3 | Select-String AGENTS.md：'写操作按单授权'，逐项校验 15 个写工具名与 4 条三件套措辞 | 写工具 15/15；三件套措辞 4/4 | 标题 1 行；写工具 15/15；三件套 4/4 | 通过 |
| AC-4 | Select-String AGENTS.md：'同一时刻只允许一个 AI'、'isCompiling'、'run_tests'、'is_domain_reload_pending'、'Tools/QA/In-Editor'、'不得**用 run_tests' | 单驱动 1 行；isCompiling ≥2；run_tests 语义为禁止；域重载/菜单入口各 ≥1 | 单驱动 1；isCompiling 2；run_tests 1；reload 1；菜单 1；禁止句 1 | 通过 |
| AC-5 | Select-String workflow-rules.md：新标题、旧标题、模式 A/B 段、§7 三条勾选、两处冲突措辞、§3.2.1 指针 | 新标题 1、旧标题 0、三条勾选 3/3、冲突措辞各 1、指针 1 | 新标题 1；旧标题 0；模式B 1；模式A 1；勾选 3/3；『不得做任何 Unity 操作』 1；『模式 A 下 Unity 未关』 1；指针 1 | 通过 |
| AC-6 | git status --porcelain，与编辑前采集的 baseline_git_status.txt 求差 | 新增项 ⊆ 允许清单（4 条）且越界 0 | 新增 4 条，越界 0 条（基线自带 19 条非本单改动） | 通过 |
| AC-7 | Select-String 两份文件 '§3.2.1' + 章节编号静态校验（标题解析 + 全文 § 引用比对） | §3.2.1 引用 ≥2；断链 0 | 引用 5 处；全文 § 引用 39 处、断链 0 处 | 通过 |
| AC-8（增补） | Select-String AGENTS.md：'卡在编辑器焦点时自行推进'、'refresh_unity'、'不必询问所有者'、'不得借唤醒之名改任何资产'、'10 分钟且 §3.2.1 第 9 条'、'超 10 分钟未结束'；并向 workflow-rules.md 查 '可自行 `refresh_unity`' | 第 9 条 1 行；唤醒手段 ≥1；免询问 1；禁改资产 1；§6.1 已叠加第 9 条 1；旧无条件表述 0 | 第 9 条 1；refresh_unity 1；免询问 1；禁改资产 1；§6.1 叠加 1；旧表述 0；细则补句 1 | 通过 |

## 结论

八条 AC（含增补 AC-8）全部通过：True

## 口径说明

1. AC-1 的“不再有绝对表述”用反向断言（命中数 = 0）证明，不靠人工判断。
2. AC-6 的“只出现允许清单”用差集证明：工作区在本单开工前即有 19 条与本单无关的改动（其他任务进行中），因此不能要求 git status 输出整体等于允清单，只能要求**新增项**全部落在允许清单内；基线已原样存档。
3. AC-7 的编号口径：`AGENTS.md` §3 为列表式三闸门，列表项 1/2/3 记作 §3.1/§3.2/§3.3（与 TASK 口径一致），`### 3.2.1` 为显式标题。
