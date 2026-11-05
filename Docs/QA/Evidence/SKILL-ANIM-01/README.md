# SKILL-ANIM-01 证据索引

任务：`Docs/Handoff/TASK_SKILL-ANIM-01.md`（瞄准态攻击指针与点击反馈）
基线提交：`bc382e3ee7b5df51d7a68e74792eaa0c041482f9`（见 `git_head.txt`）
Unity：`2022.3.62f3c1`（batchmode，运行前进程 0 且无 `Temp/UnityLockfile`）

## 1. 本目录文件

| 文件 | 内容 |
|---|---|
| `SPEC01_Red_skillanim01red.log` | 红阶段日志（测试已加、产品未改）：`pass=10 fail=2 exit=2` |
| `SPEC01_Green_run1.log` | 绿阶段第 1 次完整日志 |
| `SPEC01_Green_run2.log` | 绿阶段第 2 次完整日志 |
| `SPEC01_Green_run3.log` | 绿阶段第 3 次完整日志 |
| `git_status_snapshot.txt` | 本单完成时的 `git status --porcelain` 快照 |
| `git_head.txt` | `git rev-parse HEAD` |
| `git_diff_stat_assets.txt` | `git diff --stat -- Assets`（仅 3 个脚本） |

断言级 JSONL / Summary / ExitCode 由 `SPEC01_Evidence` 写入 canonical 目录
`Docs/QA/Evidence/SPEC-01/`，用 tag 区分（只追加，不覆盖既有证据）：

| 文件 | 内容 |
|---|---|
| `Docs/QA/Evidence/SPEC-01/SPEC01_Green_skillanim01red_run1_Assertions.jsonl` | 红阶段断言（12 条，10 pass / 2 product_fail） |
| `Docs/QA/Evidence/SPEC-01/SPEC01_Green_skillanim01_run{1,2,3}_Assertions.jsonl` | 绿阶段断言（各 12 条全 pass） |
| `Docs/QA/Evidence/SPEC-01/SPEC01_Green_skillanim01_run{1,2,3}_Summary.json` | 绿阶段汇总（pass=12 fail=0 blocked=0 infra=0） |
| `Docs/QA/Evidence/SPEC-01/SPEC01_Green_skillanim01_run{1,2,3}_ExitCode.txt` | 三次均为 `0` |
| `Docs/QA/Evidence/SPEC-01/SPEC01_Runs_skillanim01.jsonl` | 三次 run 的跨 run 索引 |
| `Docs/QA/Evidence/SPEC-01/SPEC01_Runs_skillanim01red.jsonl` | 红阶段 run 索引 |

## 2. 复现命令

```powershell
# 绿阶段（第 N 次），红阶段把 tag 换成 skillanim01red
$env:SPEC01_MODE="green"; $env:SPEC01_RUN="1"; $env:SPEC01_EVIDENCE_TAG="skillanim01"
& "D:\Unity\2022.3.62f3c1\Editor\Unity.exe" -batchmode `
  -projectPath "D:\unity\Dungeon" `
  -executeMethod SPEC01_BatchAcceptance.Run `
  -logFile "D:\unity\Dungeon\Docs\QA\Evidence\SKILL-ANIM-01\SPEC01_Green_run1.log"
```

红阶段做法：把 `CursorManager.cs` / `SkillInputController.cs` 换回 `HEAD` 版本（`git show HEAD:<path>` 导出），
只保留新增的 `SPEC01_ProbeRunner.cs` 断言，跑一次得到 `product_fail ×2`，随后把本单版本还原（已校验 SHA256 一致）。

## 3. 人工目检（执行者不代填）

- AC-4 目检 1：瞄准态指针（敌人→攻击指针 / 空地→普通指针）
- AC-4 目检 2：点击提示（空地“未选中敌人” / 超距“目标超出射程，正在接近” / 失败“技能释放失败”）

两项本次均 **未运行（待所有者目检）**，本目录不含目检截图/录屏。
