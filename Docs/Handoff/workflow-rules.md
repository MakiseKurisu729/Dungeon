# 工作流细则（WF-01）

> 由 `AGENTS.md` 索引；本文件承载 REPORT 格式与提交规范等细节，硬约束仍以 `AGENTS.md` 为准。

## 1. REPORT「建议提交」节格式

1. 标题行使用 Conventional Commits：`<type>(<scope>): <摘要>`，type 取 `feat` / `fix` / `test` / `docs` / `refactor` / `chore`。
2. body 一行写**为什么**（动机/根因），不写“做了什么”。
3. 附 `git add` 文件清单。
4. 混合改动（如代码 + 测试 + 文档）必须给出拆分方案：每组列出文件清单与各自的提交信息。

示例：

```text
组 1：
git add Assets/_Project/Scripts/Foo.cs Assets/_Project/Scripts/Foo.cs.meta
fix(combat): 修正普攻伤害重复结算
原因：OnHit 在动画事件与协程中各调用一次，导致单次攻击结算两次。

组 2：
git add Docs/Handoff/REPORT_XXX.md Docs/QA/YYY.md
docs(qa): 登记 XXX 修复验收证据
原因：修复结论需可追溯到断言与日志。
```

## 2. REPORT「受影响模块导航说明」节

- 适用：修复类、功能类工单（纯验收/纯文档单可写“不适用”）。
- 篇幅 ≤200 字，包含：模块职责、入口（类/方法/场景对象）、数据流（输入 → 处理 → 输出）。
- 用途：用户定期归档至 `Docs/Learning/`，作为代码库地图与学习笔记。
