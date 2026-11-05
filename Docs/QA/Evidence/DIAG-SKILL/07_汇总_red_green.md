# SPEC-DIAG-SKILL-2 · 证据汇总（red → green）

> 工单：`Docs/Handoff/TASK_SPEC-DIAG-SKILL-2.md`
> 回滚锚：`7cf874f7c5db448215b33a90fcad1d209f1fb781`（每次都记录为 head）
> 采集环境：Unity 2022.3.62f3c1 / Windows / batchmode
> 说明：`SPEC01_Evidence` 的证据根目录是 `Docs/QA/Evidence/SPEC-01/`，本目录中的 `07_*` 文件是运行产物复制件（文件名已改为本单序号，内容未改）。

## 1. 运行清单

| 轮次 | 用途 | 命令 | 日志 | 退出码 |
|---|---|---|---|---|
| red | 改生产代码前（只改测试） | `SPEC01_BatchAcceptance.Run` | `SPEC01_skill2_red.log` | 2 |
| green run1 | 改后验收 | 同上（`SPEC01_EVIDENCE_TAG=skill2run1`） | `SPEC01_skill2_green_run1.log` | 0 |
| green run2 | 改后验收 | 同上（`skill2run2`） | `SPEC01_skill2_green_run2.log` | 0 |
| green run3 | 改后验收 | 同上（`skill2run3`） | `SPEC01_skill2_green_run3.log` | 0 |
| green run4（还原） | 无 tag 再跑一次，把 `Docs/QA/Evidence/SPEC-01/SPEC01_Assertions.jsonl`、`SPEC01_Summary_run1.json`、`SPEC01_ExitCode_run1.txt` 还原为 green 内容（red 轮曾覆盖这三个 tracked 文件） | 同上去 tag | `SPEC01_skill2_green_run4_restore.log` | 0 |
| DIAG | 回归确认（Step 5） | `DIAG_BatchAcceptance.Run` | `DIAG_skill2_run.log` | 2 |

每次运行后均轮询到 Unity 进程归零才取证据（实测单轮 36–91 s）。

## 2. 汇总计数

| 轮次 | pass | fail | blocked | infra | observed | exit |
|---|---|---|---|---|---|---|
| red | 9 | 1 | 0 | 0 | 0 | 2 |
| green run1 | 10 | 0 | 0 | 0 | 0 | 0 |
| green run2 | 10 | 0 | 0 | 0 | 0 | 0 |
| green run3 | 10 | 0 | 0 | 0 | 0 | 0 |
| green run4（还原） | 10 | 0 | 0 | 0 | 0 | 0 |

red 的唯一红项是 `Skill.CastSmoke`；`Skill.InputWired` 在 red 即为 pass（资产 A 已生效）。
六份日志 `error CS` 计数均为 0。

## 3. `Skill.CastSmoke` 关键数据

| 字段 | red（修复前） | green run1 | green run2 | green run3 | green run4（还原） |
|---|---|---|---|---|---|
| `failedNoManaPass` | **false** | true | true | true | true |
| `failedCastReturn` | false | false | false | false | false |
| `manaDeltaFailed` | **20** | 0 | 0 | 0 | 0 |
| `cdFailed` | 0 | 0 | 0 | 0 | 0 |
| `qEffectType` | BashEffect | BashEffect | BashEffect | BashEffect | BashEffect |
| `wEffectType` | **BashEffect** | **HeavySlashEffect** | HeavySlashEffect | HeavySlashEffect | HeavySlashEffect |
| `qPass` / `wPass` / `emptyPass` | true / **false** / true | true/true/true | true/true/true | true/true/true | true/true/true |
| Q 蓝量变化 `manaDelta` | 20 | 20 | 20 | 20 | 20 |
| Q `cdRemaining` | 6 | 6 | 6 | 6 | 6 |
| Q 靶掉血 / 期望伤害 | 15 / 15 | 15 / 15 | 15 / 15 | 15 / 15 | 15 / 15 |
| Q 进入 Bash 用时 | 0.1364 s | 0.1364 s | 0.1366 s | 0.1361 s | 0.1361 s |
| W 蓝量变化 `manaDelta` | 30（白扣，castReturn=false） | 30 | 30 | 30 | 30 |
| W `cdRemaining` / `swordTimer` | 0 / -1（效果未执行） | 6 / 0 | 6 / 0 | 6 / 0 | 6 / 0 |
| W 进入 Skill 用时 | 未进入 | 0.0519 s | 0.0520 s | 0.0521 s | 0.0511 s |
| E/R 返回 / 蓝量 | false / 0 | false / 0 | false / 0 | false / 0 | false / 0 |

结论：Q 的蓝量、CD、伤害、Bash 用时四项在 red 与 4 次 green 中逐项一致（20 / 6 / 15 / ≈0.136 s）；W 由"解析成 BashEffect → 失败白扣 30 蓝"变为"HeavySlashEffect → 成功扣 30 蓝并进入 Skill"。

## 4. DIAG 回归（Step 5）

| 断言 | 结果 | 关键数据 |
|---|---|---|
| `Diag.CastChain.Q` | pass | castReturn=true；effectType=BashEffect；manaDelta=20；cd=6；伤害 15/15；Bash 0.138 s |
| `Diag.CastChain.W` | **pass**（red 阶段为 product_fail） | castReturn=true；**effectType=HeavySlashEffect**；manaDelta=30；cd=6；swordTimer=0；Skill 0.051 s |
| `Diag.CastChain.EmptySlots` | pass | — |
| `Diag.Scene.SkillInputController` / `Diag.Scene.Indicator` / `Diag.Prefab.Slots` | pass | 资产 A 生效后由 fail 转 pass |
| `Diag.Input.FirstExit` | **product_fail** | 该探针把"场景缺控制器"写成了期望值：`expected={"expected":"MISSING_CONTROLLER"}`，实际 `controllerCount=1`。属 DIAG 探针期望陈旧，不是产品回归；本单禁止改 `DIAG_ProbeRunner.cs`，按实际记录 |

DIAG 汇总：pass=6 / fail=1 / blocked=0 / infra=0，exit=2（该 1 项失败来自上述陈旧期望）。

## 5. 文件清单（本目录）

- `07_前置确认_SKILL2.txt`：Step 0 前置自证（无 Unity 进程、无 UnityLockfile、head 对齐回滚锚）
- `SPEC01_skill2_red.log` + `07_Assertions_red.jsonl` + `07_Summary_red.json` + `07_ExitCode_red.txt`
- `SPEC01_skill2_green_run1..3.log` + `07_Assertions_green_run1..3.jsonl` + `07_Summary_green_run1..3.json` + `07_ExitCode_green_run1..3.txt`
- `SPEC01_skill2_green_run4_restore.log` + `07_Assertions_green_run4_restore.jsonl` + `07_Summary_green_run4_restore.json` + `07_ExitCode_green_run4_restore.txt`
- `DIAG_skill2_run.log` + `07_DIAG_Assertions_skill2.jsonl` + `07_DIAG_Summary_skill2.json` + `07_DIAG_ExitCode.txt`
- `07_DIAG_prev_*`：本单 DIAG 运行前 `DIAG_Assertions.jsonl` / `DIAG_Summary.json` / `DIAG_ExitCode.txt` 的备份（SKILL-1 产物，被 DIAG 入口固定路径覆盖前留存）
