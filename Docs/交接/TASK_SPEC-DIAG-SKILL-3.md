# TASK — SPEC-DIAG-SKILL-3：SPEC01 规范 N=3 证据补齐（零代码改动）

> **风险等级：低风险**（只跑测试与写 Docs 证据；不改任何代码/资产）
> **授权**：承接 `TASK_SPEC-DIAG-SKILL-2` 的验收收尾；落盘即批准，不停下等确认。
> **回滚锚**：`7cf874f7c5db448215b33a90fcad1d209f1fb781`

## 1. 问题

`Docs/QA/Evidence/SPEC-01/` 当前不一致：
- `SPEC01_Summary_run1.json` = 10/10、exit 0（今天）；`SPEC01_Assertions.jsonl` 只有今天 10 行；
- `SPEC01_Summary_run2.json` / `run3.json`、`SPEC01_ExitCode_run2.txt` / `run3.txt` 仍是**昨晚 8 pass / 2 fail** 的旧产物（exit=2）。

原因：上一轮 3 次 green 都用了默认 `SPEC01_RUN`（每次 run=1，互相覆盖），最后一次又只还原了 run1。日志虽能证明三次 10/10，但**目录内证据自相矛盾**，不能作为规范 N=3 验收材料。

## 2. 目标

用套件原生 N=3 机制重跑，使 `Docs/QA/Evidence/SPEC-01/` 三份 run 证据全部为 **10/10、exit 0**，且三份数据一致。

## 3. 执行步骤（零代码改动）

### Step 0
`Get-Process -Name Unity` = 0、`Test-Path Temp/UnityLockfile` = False；写 `Docs/QA/Evidence/DIAG-SKILL/08_前置确认_SPEC01_N3.txt`。

### Step 1 — 三次运行（必须设置 `SPEC01_RUN`，且**不设** `SPEC01_EVIDENCE_TAG`）

```powershell
$env:SPEC01_MODE="green"
& "D:\unity\2022.3.62f3c1\Editor\Unity.exe" -batchmode -projectPath "D:\unity\Dungeon" `
  -executeMethod SPEC01_BatchAcceptance.Run `
  -logFile "D:\unity\Dungeon\Docs\QA\Evidence\DIAG-SKILL\SPEC01_skill2_final_run1.log"
```
- run1：`$env:SPEC01_RUN="1"`；run2：`"2"`；run3：`"3"`。
- 每次启动前：轮询 Unity 进程归零、锁文件消失；Windows 上 `Unity.exe` 是启动器会立即返回，**必须等进程结束再取证据**。
- 禁止使用 `SPEC01_EVIDENCE_TAG`（否则不会写默认路径）。

### Step 2 — 验收检查（逐条留证）

1. `SPEC01_Summary_run1/2/3.json`：均 `pass=10, fail=0, blocked=0, infraFail=0, exitReason=completed`。
2. `SPEC01_ExitCode_run1/2/3.txt`：均 `0`。
3. `SPEC01_Assertions.jsonl`：**30 行**（`run` 字段覆盖 1/2/3，各 10 条），无 `product_fail`/`blocked`/`infra_fail`。
4. 三份 `Skill.CastSmoke` 数据一致：`failedNoManaPass=true`、`manaDeltaFailed=0`、`qEffectType=BashEffect`、`wEffectType=HeavySlashEffect`、`qPass=wPass=emptyPass=true`；Q 蓝 -20 / CD 6 / 伤害 15；`Skill.InputWired` `controllerCount=1`。
5. `SPEC01_Runs.jsonl` 追加 3 行；`error CS` 计数 0。

### Step 3 — 报告

在 `Docs/Handoff/REPORT_SPEC-DIAG-SKILL-2.md` 追加「§ 终验证据补正（SPEC-DIAG-SKILL-3）」：
- 说明旧 run2/3 为昨晚残留、本次已由原生 N=3 覆盖；
- 贴三份 Summary 的 pass/fail/exit 与实际文件路径；
- 贴 `Skill.CastSmoke`/`Skill.InputWired` 三条 run 的关键字段；
- 明确结论：`SPEC01 = 10/10 × 3、exit 0`。

## 4. 熔断

任一 run 非 10/10、exit≠0、进程未归零、需要改代码/资产 —— 停止并写现场报告，不得手工改写 JSON 结果。

## 5. 禁止

- 不改任何 `.cs` / `.asset` / `.unity` / `.prefab` / `.controller` / `.meta`。
- 不伪造、不手工编辑 Assertions/Summary/ExitCode；只允许套件自己写。
- 不做 git 写操作。
