# OPS-01 证据 Summary（阶段 0 工程治理，4 项）

- 任务：OPS-01/.editorconfig、OPS-02/.gitattributes、OPS-03/.gitignore、OPS-04/workflow-rules 上下文与预算
- 日期：2026-11-06
- 执行者：OpenCode 执行者（模型 `opencode-go/deepseek-v4.1-flash`）
- 边界：只写 4 个文件 + 本证据目录；未碰代码/资产/场景/预制体；未执行任何 git 写操作（无 commit/push/rm/config）。
- 证据文件：`grep-four-sections.txt`、`git-status.txt`、`diff.patch`、`OWNER-COMMANDS.txt`

## 验收对照

| # | 验收项 | 结论 | 证据 |
|---|---|---|---|
| AC-1 | 仓库根存在 `.editorconfig`，含 root=true、`[*]` 六项（charset utf-8 / end_of_line lf / insert_final_newline / trim_trailing_whitespace / indent_style space / indent_size 4）、`[*.{json,asmdef}]` indent_size=2、`[*.md]` trim_trailing_whitespace=false | 通过 | grep-four-sections.txt §[1]；`Test-Path .editorconfig = True`；`git status` 显示 `?? .editorconfig` |
| AC-2 | `.gitattributes` 追加 `*.unity/*.prefab/*.asset merge=unityyamlmerge` | 通过 | grep-four-sections.txt §[2]；`git check-attr merge` 三条均为 `unityyamlmerge`，对照 `.json` 为 `unspecified` |
| AC-3 | `.gitignore` 追加 `*.csproj`、`*.sln` | 通过 | grep-four-sections.txt §[3]；`git check-ignore --no-index -v` 命中三个文件（行号 36/37） |
| AC-4 | `Docs/Handoff/workflow-rules.md` 新增「上下文与预算」一节，含 70%/60%/禁止中途停/修复轮次 1-2 轮四条 | 通过 | grep-four-sections.txt §[4]；diff.patch 仅新增 7 行，原内容未改 |
| AC-5 | 改动范围仅 4 个文件（+ 本证据目录）；未触碰他会话改动 | 通过（附注） | git-status.txt；diffstat：`.gitattributes +5`、`.gitignore +6/-1`、`workflow-rules.md +7`、新文件 `.editorconfig` |
| AC-6 | 证据目录 `Docs/QA/Evidence/OPS-01/` 含四段 grep、git status、所有者两条命令 | 通过 | 本目录 4 个证据文件 |

ExitCode：本条为纯文档/配置文件任务，无测试套件运行（不适用自动化套件 ExitCode），故以只读校验命令输出为准。

## 附注（重要，非本单改动）

执行期间 `git status` 里存在**不属于本单**的改动，均为既有或他会话产生，执行者全程只读、未修改、未提交：

1. 开工前即存在：`M AGENTS.md`、`M Docs/01_总体架构与内容生产规范.md`、`?? Docs/07_工程规范与基础设施.md`、`?? Docs/QA/Evidence/DOC-INFRA-01/`。
2. 开工后由他会话新增：`M Assets/_Project/Tests/FND00B/Runtime/FND00B06_Evidence.cs`、`M .../FND00B06_ProbeRunner.cs`（Tests/** 属用户明示“不要动”的范围）。
3. `M Assembly-CSharp.csproj`、`M Assembly-CSharp-Editor.csproj` 属既有状态（Unity 重新生成）。

因此实际 `git status` 不是“只多这四个文件”，多出的是上列既有/他会话改动；本单自身净增为 4 个目标文件 + 1 个证据目录。

## 未决项

1. 所有者需执行 OWNER-COMMANDS.txt 的两条命令（合并驱动配置、`git rm --cached`）；`Dungeon.sln` 已被跟踪，故 `git rm --cached` 已包含它。
2. `.gitignore` 忽略对已跟踪文件无效——命令 2 未执行前，csproj/sln 仍会进 diff。
3. `core.autocrlf=true` 与 `.editorconfig` 的 `end_of_line=lf` 存在潜在冲突（git 会在检出的工作区转 CRLF）；本单未改该配置，留待所有者决策。
