using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using Dungeon.Content;
using Dungeon.Core;
using Dungeon.Gameplay.Combat;
using Dungeon.Gameplay.Interaction;
using Dungeon.Gameplay.Items;
using Dungeon.Gameplay.Player;
using Dungeon.Gameplay.Units;
using Dungeon.Gameplay.World;
using Dungeon.App;
using Dungeon.UI;
#endif

/// <summary>
/// REFACTOR 护栏套件（Play 模式）。M0 建立，之后每个重构里程碑都跑它做“行为不变”兜底。
/// 只读生产状态；测试自身唯一写入面为 Docs 证据、SessionState 退出码、
/// 以及运行期内存夹具（结束前销毁）。
///
/// 16 条断言（每条都走生产入口，不复制 SPEC01 的数值口径）：
///   1. Party.PresentAndMove       三职业存在 + 生产移动命令可移动
///   2. Combat.MeleeHit            骑士近战普攻命中，命中瞬间处于 AttackMelee
///   3. Combat.RangedShot          弓/法远程普攻发射，发射瞬间处于 Attack_Release
///   4. Skill.QW                   Q/W 经 SkillSystem.Cast 生效（类型/蓝耗/动画）
///   5. Skill.InterruptAttack      普攻出招中施放 Q：Cast 成功、普攻不再命中、≤0.6s 进入 Q_1
///   6. Hit.InterruptSkill         施放 W 后受击 → ≤0.5s 进入 Hit_A 且未死
///   7. Skill.InterruptInteract    技能期间交互指令无效（无待办/不位移），技能后可下单
///   8. Projectile.AimPoint        子弹落点 = 目标渲染包围盒中心；无 Renderer 回退 position
///   9. Hit.InterruptAttack        普攻中受击 → 进入 Hit_A（打断）
///  10. Downed.Knockdown           致死 → Downed，AI/导航停用
///  11. Rescue.ChannelInterrupt    救援读条 → 受击打断 → 重新下令 → 救起
///  12. Chest.Interact             点击宝箱指令 → 走到范围 → 开盖 + 内容物现身
///  13. Refactor.RegistrySemantics 单位注册表契约（未激活语义 / Nearest 与全场扫描同口径）
///  14. Refactor.StateFacade       状态/流程门面（新旧 API 双向一致 / 事件恰好一次 / 暂停 timeScale）
///  15. Refactor.InteractionContract 交互契约（接口语义 / KeyInteractable / 开箱事件恰好一次 / Runner 复用）
///  16. Skill.MotionUnlocked       运动锁复位（技能结束 / 被打断 / 倒地复活三条路径后 isStopped==false 且能移动）
/// </summary>
public class REFACTOR_ProbeRunner : MonoBehaviour
{
    public const string ScenePath = "Assets/Scenes/DungeonWorld.unity";
    public const string FinishedKey = "REFACTOR.Finished";
    public const string ExitCodeKey = "REFACTOR.ExitCode";

    const string Locomotion = "Locomotion";
    const string AttackMelee = "AttackMelee";
    const string AttackRelease = "Attack_Release";
    const string HitA = "Hit_A";
    const string Downed = "Downed";

    static REFACTOR_Evidence _ev;
    static bool _finished;
    static string _exitReason = "";
    static FieldInfo _firedField;
    static FieldInfo _chestOpenedField;
    static FieldInfo _attackTimerField;

    int _run;
    int _seed;
    float _deadline;

    readonly List<QaFixtures.PlayerProbe> _players = new List<QaFixtures.PlayerProbe>();
    readonly List<GameObject> _suppressedMonsters = new List<GameObject>();
    GameObject _target;
    UnitStats _targetStats;
    GameObject _chestGo;
    ItemData _chestItem;
    bool _monstersSuppressed;


    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    IEnumerator Start()
    {
        int parsedRun;
        string envRun = Environment.GetEnvironmentVariable("REFACTOR_RUN");
        if (!int.TryParse(envRun, out parsedRun) || parsedRun <= 0) parsedRun = 1;
        _run = parsedRun;
        _seed = 20261106 + _run;
        UnityEngine.Random.InitState(_seed);

        _ev = new REFACTOR_Evidence(_run);
        _deadline = Time.realtimeSinceStartup + 300f;

        Debug.Log("[REFACTOR] runner start; milestone=" + _ev.Milestone + "; run=" + _run +
                  "; seed=" + _seed + "; unity=" + Application.unityVersion +
                  "; evidenceDir=" + _ev.EvidenceDir + "; head=" + QaScene.GetHead("REFACTOR.Head"));

        float waited = 0f;
        while (SceneManager.GetActiveScene().path != ScenePath && waited < 25f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        Debug.Log("[REFACTOR] active scene=" + SceneManager.GetActiveScene().path + "; waited=" + F(waited) + "s");

        yield return new WaitForSecondsRealtime(1.0f);

        FindPlayers();
        if (_players.Count != 3)
        {
            string[] ids =
            {
                "Party.PresentAndMove", "Combat.MeleeHit", "Combat.RangedShot", "Skill.QW",
                "Skill.InterruptAttack", "Hit.InterruptSkill", "Skill.InterruptInteract", "Projectile.AimPoint",
                "Hit.InterruptAttack", "Downed.Knockdown", "Rescue.ChannelInterrupt", "Chest.Interact",
                "Refactor.RegistrySemantics", "Refactor.StateFacade", "Refactor.InteractionContract",
                "Skill.MotionUnlocked"
            };
            foreach (string id in ids)
                _ev.BlockedAssertion(id, id, "三角色齐备才能跑的护栏断言",
                    "找到玩家数=" + _players.Count, QaJson.Dict("players", _players.Select(p => p.name).ToList()));
            Finish();
            yield break;
        }

        yield return RunSafe("Party.PresentAndMove", AssertPartyMove(_ev, _players));
        SuppressMonsters();
        yield return RunSafe("Combat.MeleeHit", AssertMeleeHit(_ev, _players));
        yield return RunSafe("Combat.RangedShot", AssertRangedShot(_ev, _players));
        yield return RunSafe("Skill.QW", AssertSkillQW(_ev));
        yield return RunSafe("Skill.InterruptAttack", AssertSkillInterruptAttack(_ev, _players));
        yield return RunSafe("Hit.InterruptSkill", AssertHitInterruptSkill(_ev, _players));
        yield return RunSafe("Skill.InterruptInteract", AssertSkillInterruptInteract(_ev, _players));
        yield return RunSafe("Projectile.AimPoint", AssertProjectileAimPoint(_ev));
        yield return RunSafe("Hit.InterruptAttack", AssertHitInterrupt(_ev, _players));
        yield return RunSafe("Downed.Knockdown", AssertKnockdown(_ev, _players));
        yield return RunSafe("Rescue.ChannelInterrupt", AssertRescue(_ev, _players));
        yield return RunSafe("Chest.Interact", AssertChest(_ev, _players));
        yield return RunSafe("Refactor.InteractionContract", AssertInteractionContract(_ev, _players));
        yield return RunSafe("Refactor.RegistrySemantics", AssertRegistrySemantics(_ev, _players));
        yield return RunSafe("Refactor.StateFacade", AssertStateFacade(_ev));
        yield return RunSafe("Skill.MotionUnlocked", AssertMotionUnlocked(_ev, _players));
        RestoreMonstersAndUi();
        CleanupFixtures();
        Finish();
    }

    void Update()
    {
        if (_finished || _ev == null) return;
        if (Time.realtimeSinceStartup <= _deadline) return;
        _ev.InfraFailure("all", "Runner.Watchdog", "运行超时（300 秒）被强制结束", "");
        _exitReason = "watchdog timeout";
        Finish();
    }

    IEnumerator RunSafe(string id, IEnumerator routine)
    {
        while (true)
        {
            object cur;
            try
            {
                if (!routine.MoveNext()) break;
                cur = routine.Current;
            }
            catch (Exception e)
            {
                Debug.LogError("[REFACTOR] assertion " + id + " threw: " + e);
                _ev.InfraFailure(id, id, e.Message, e.StackTrace);
                yield break;
            }
            yield return cur;
        }
    }

    // ============================================================
    // 1. Party.PresentAndMove
    // ============================================================
    IEnumerator AssertPartyMove(IQaSink sink, IReadOnlyList<QaFixtures.PlayerProbe> players)
    {
        const string Id = "Party.PresentAndMove";
        var rows = new List<object>();
        bool allPass = true;

        foreach (var p in players)
        {
            if (p.brain == null || p.brain.agent == null || p.animator == null)
            {
                allPass = false;
                rows.Add(QaJson.Dict("name", p.name, "error", "missing brain/agent/animator"));
                continue;
            }

            Vector3 dest = p.home + new Vector3(0f, 0f, 3f);
            NavMeshHit hit;
            if (!NavMesh.SamplePosition(dest, out hit, 2.0f, NavMesh.AllAreas)) dest = p.home + new Vector3(1.5f, 0f, 1.5f);
            else dest = hit.position;

            QaFixtures.Select(p);
            var sm = SelectionManager.instance;
            if (sm != null) sm.ForEachSelectedBrain(b => b.CommandMove(dest));
            else p.brain.CommandMove(dest);

            float elapsed = 0f;
            float peakSpeed = 0f;
            float peakSpeedZ = 0f;
            bool grounded = true;
            bool arrived = false;
            string stateDuringMove = "none";
            float traveled = 0f;
            Vector3 prev = p.tx.position;

            while (elapsed < 8f)
            {
                yield return null;
                elapsed += Time.deltaTime;
                Vector3 pos = p.tx.position;
                traveled += Vector3.Distance(prev, pos);
                prev = pos;

                float speed = p.brain.agent.velocity.magnitude;
                float sz = p.animator.GetFloat("SpeedZ");
                if (speed > 0.5f && stateDuringMove == "none") stateDuringMove = QaFixtures.StateName(p.animator);
                if (speed > peakSpeed) peakSpeed = speed;
                if (Mathf.Abs(sz) > Mathf.Abs(peakSpeedZ)) peakSpeedZ = sz;
                grounded &= p.animator.GetBool("Grounded");

                if (!p.brain.agent.pathPending && p.brain.agent.hasPath &&
                    p.brain.agent.remainingDistance <= p.brain.agent.stoppingDistance + 0.15f)
                {
                    arrived = true;
                    break;
                }
            }

            yield return new WaitForSeconds(1.0f);
            float finalSpeed = p.brain.agent.velocity.magnitude;
            float finalSpeedZ = p.animator.GetFloat("SpeedZ");

            bool pass = arrived && peakSpeed >= 2.0f && Mathf.Abs(peakSpeedZ) >= 2.0f &&
                        (stateDuringMove == Locomotion || stateDuringMove == "none") &&
                        Mathf.Abs(finalSpeedZ) <= 0.15f && finalSpeed <= 0.15f && grounded;
            allPass &= pass;

            rows.Add(QaJson.Dict("name", p.name, "destination", QaJson.V3(dest), "arrived", arrived,
                          "durationSeconds", elapsed, "traveled", traveled,
                          "peakAgentSpeed", peakSpeed, "peakAnimatorSpeedZ", peakSpeedZ,
                          "stateDuringMove", stateDuringMove,
                          "finalAgentSpeed", finalSpeed, "finalAnimatorSpeedZ", finalSpeedZ,
                          "groundedDuringMove", grounded, "pass", pass));

            // 回出生点，避免影响后续断言
            QaFixtures.Select(p);
            if (sm != null) sm.ForEachSelectedBrain(b => b.CommandMove(p.home));
            else p.brain.CommandMove(p.home);
            float w = 0f;
            while (w < 8f)
            {
                yield return null;
                w += Time.deltaTime;
                if (!p.brain.agent.pathPending && p.brain.agent.hasPath &&
                    p.brain.agent.remainingDistance <= p.brain.agent.stoppingDistance + 0.2f) break;
            }
        }

        sink.Record(Id, Id,
            "Knight/Archer/Mage 三个生产玩家单位均存在，且经 SelectionManager.ForEachSelectedBrain → CommandMove 能移动到目标点并在 Locomotion 停稳",
            "players=" + players.Count + "; allPass=" + allPass,
            allPass, allPass ? "pass" : "product_fail", "production",
            QaJson.Dict("players", rows, "count", players.Count,
                 "note", "移动入口与右键移动同一生产方法；SpeedZ 只要求非零变化，不复制 SPEC01 阈值口径"));
    }
    // ============================================================
    // 2. Combat.MeleeHit
    // ============================================================
    IEnumerator AssertMeleeHit(IQaSink sink, IReadOnlyList<QaFixtures.PlayerProbe> players)
    {
        const string Id = "Combat.MeleeHit";
        var knight = players.FirstOrDefault(p => p.name == "Knight_Unit");
        if (knight == null || knight.brain == null || knight.health == null)
        {
            sink.BlockedAssertion(Id, Id, "骑士近战普攻可命中", "未找到 Knight_Unit", null);
            yield break;
        }

        _target = CreateTarget("REFACTOR_Target_Melee", knight.tx.position + knight.tx.forward * 1.8f);
        if (_target == null)
        {
            sink.InfraFailure(Id, Id, "近战靶夹具创建失败", "");
            yield break;
        }
        var targetHealth = _target.GetComponent<UnitHealth>();

        var collector = new QaFixtures.HitCollector(200, false);
        collector.Subscribe(targetHealth, knight.health.GetInstanceID());

        knight.brain.enabled = false;
        knight.brain.CommandAttack(_target.transform);
        knight.brain.enabled = true;

        float t = 0f;
        while (t < 8f && collector.Count == 0)
        {
            yield return null;
            t += Time.deltaTime;
        }
        collector.Unsubscribe();

        Dictionary<string, object> ev = collector.Count > 0 ? collector.Events[0] : null;
        string hitState = ev != null ? (string)ev["state"] : "none";
        float hitNorm = ev != null ? (float)ev["normalizedTime"] : -1f;
        float amount = ev != null ? (float)ev["amount"] : 0f;
        bool pass = ev != null && hitState == AttackMelee && amount > 0f;

        knight.brain.CommandAttack(null);
        var combat = knight.brain.combat as ICharacterCombat;
        if (combat != null) combat.CancelAttack();
        yield return QaWait.WaitState(knight.animator, Locomotion, 5f);

        sink.Record(Id, Id,
            "骑士经 CommandAttack 对近战范围内目标出手一次：目标掉血，且命中瞬间于 layer0 AttackMelee",
            "hits=" + collector.Count + "; state=" + hitState + "; amount=" + F(amount) +
            "; wait=" + F(t) + "s",
            pass, pass ? "pass" : "product_fail", "production",
            QaJson.Dict("hits", collector.Count, "hitState", hitState, "hitNormalizedTime", hitNorm,
                 "amount", amount, "waitSeconds", t,
                 "fixture", "REFACTOR_Target_Melee（运行期内存 UnitHealth，无 AI）",
                 "entry", "CharacterBrain.CommandAttack → UnitBrain.Update → ICharacterCombat.AutoAttack → MeleeCombat.SingleTargetHit"));
    }
    // ============================================================
    // 3. Combat.RangedShot
    // ============================================================
    IEnumerator AssertRangedShot(IQaSink sink, IReadOnlyList<QaFixtures.PlayerProbe> players)
    {
        const string Id = "Combat.RangedShot";
        var rows = new List<object>();
        bool allPass = true;
        int tested = 0;

        foreach (var p in players.Where(x => x.ranged != null))
        {
            tested++;
            _target = CreateTarget("REFACTOR_Target_Ranged_" + p.name, p.tx.position + p.tx.forward * 3.0f);
            var targetHealth = _target.GetComponent<UnitHealth>();

            bool wasFired = GetFired(p.ranged);
            string fireState = "none";
            float fireNorm = -1f;
            bool fired = false;
            float t = 0f;

            p.brain.enabled = false;
            p.brain.CommandAttack(_target.transform);
            p.brain.enabled = true;

            while (t < 10f)
            {
                yield return null;
                t += Time.deltaTime;
                bool f = GetFired(p.ranged);
                if (f && !wasFired)
                {
                    fired = true;
                    fireState = QaFixtures.StateName(p.animator);
                    fireNorm = QaFixtures.StateNorm(p.animator);
                    break;
                }
                wasFired = f;
            }

            // 等弹道落地，读一次目标血量（仅记录，不参与通过判定：法师火球依赖预制体配置）
            float hpBefore = targetHealth.hp;
            yield return new WaitForSeconds(1.2f);
            float damage = hpBefore - targetHealth.hp;

            p.brain.CommandAttack(null);
            var combat = p.brain.combat as ICharacterCombat;
            if (combat != null) combat.CancelAttack();
            yield return QaWait.WaitState(p.animator, Locomotion, 5f);

            bool pass = fired && fireState == AttackRelease;
            allPass &= pass;
            rows.Add(QaJson.Dict("player", p.name, "fired", fired, "fireState", fireState,
                          "fireNormalizedTime", fireNorm, "waitSeconds", t,
                          "damageObserved", damage, "attackSpeed", p.attackSpeed, "pass", pass));
            DestroyTarget();
        }

        bool anyRanged = tested > 0;
        bool passOverall = anyRanged && allPass;
        sink.Record(Id, Id,
            "弓手/法师经 CommandAttack 出手：至少发射一次，且发射瞬间于 layer0 Attack_Release",
            "tested=" + tested + "; allPass=" + allPass + "; rows=" + rows.Count,
            passOverall, passOverall ? "pass" : "product_fail", "production",
            QaJson.Dict("players", rows,
                 "note", "伤害只记录不判定：法师火球是否结算取决于预制体 explosionEffect 配置；发射本身是远程普攻的核心契约"));
    }
    // ============================================================
    // 4. Skill.QW
    // ============================================================
    IEnumerator AssertSkillQW(IQaSink sink)
    {
        const string Id = "Skill.QW";
        Vector3 FixturePos = new Vector3(80f, 0f, 80f);

        SkillCastFixture fx = null;
        try { fx = SkillCastFixture.Create(FixturePos, true); }
        catch (Exception e)
        {
            sink.InfraFailure(Id, Id, "夹具创建抛异常: " + e.Message, e.StackTrace);
            yield break;
        }
        if (fx == null || !string.IsNullOrEmpty(fx.Error))
        {
            string err = fx != null ? fx.Error : "null";
            if (fx != null) fx.Dispose();
            sink.InfraFailure(Id, Id, "夹具创建失败: " + err, "");
            yield break;
        }

        var q = new CastProbeResult();
        var w = new CastProbeResult();
        bool qPass = false, wPass = false;
        string prepareState = "none";
        float prepareSeconds = -1f;

        try
        {
            yield return fx.Prepare(6f);
            prepareState = fx.PrepareState;
            prepareSeconds = fx.PrepareSeconds;
            if (!fx.PrepareLocomotion)
            {
                sink.BlockedAssertion(Id, Id, "Q/W 可施放",
                    "夹具未能进入 Locomotion（preState=" + prepareState + "）",
                    QaJson.Dict("prepareState", prepareState));
                yield break;
            }

            yield return fx.Cast(0, fx.Target != null ? fx.Target.transform : null, "Q_1", 0.5f, q);
            qPass = q.castReturn && q.effectType == "BashEffect" &&
                    Mathf.Abs(q.manaDelta - 20f) < 0.01f &&
                    q.damageObserved && q.animEntered && q.animEntrySeconds >= 0f && q.animEntrySeconds <= 0.6f;

            yield return fx.EnsureLocomotion(3f);
            yield return fx.Cast(1, null, "W_1", 0.5f, w);
            wPass = w.castReturn && w.effectType == "HeavySlashEffect" &&
                    Mathf.Abs(w.manaDelta - 30f) < 0.01f &&
                    w.swordTimer >= 0f && w.animEntered && w.animEntrySeconds >= 0f && w.animEntrySeconds <= 0.6f;
        }
        finally
        {
            if (fx != null) fx.Dispose();
        }

        bool pass = qPass && wPass;
        sink.Record(Id, Id,
            "Q（BashEffect，-20 蓝，命中掉血，≤0.6s 进入 Q_1）与 W（HeavySlashEffect，-30 蓝，起手技能斩，≤0.6s 进入 W_1）均经 SkillSystem.Cast 生效",
            "qPass=" + qPass + "; wPass=" + wPass +
            "; qType=" + q.effectType + "; wType=" + w.effectType +
            "; qManaDelta=" + F(q.manaDelta) + "; wManaDelta=" + F(w.manaDelta),
            pass, pass ? "pass" : "product_fail", "production",
            QaJson.Dict("Q", q.Data(), "W", w.Data(),
                 "prepareState", prepareState, "prepareSeconds", prepareSeconds,
                 "fixture", SkillCastFixture.KnightPrefabPath,
                 "note", "只验 Q/W 两个有内容的槽；E/R 空槽契约由 SPEC01 覆盖，本套件不重复"));
    }
    // ============================================================
    // M4-A. Skill.InterruptAttack（M4 新增）
    // ============================================================
    /// <summary>普攻出招中施放 Q（BashEffect）：Cast 返回 true、普攻判定窗口被收掉（不再产生 Slash 命中）、
    /// ≤0.6s 进入 Bash。只统计骑士自己发出的伤害，并用伤害类型区分普攻(Slash)与盾击(Blunt)。</summary>
    IEnumerator AssertSkillInterruptAttack(IQaSink sink, IReadOnlyList<QaFixtures.PlayerProbe> players)
    {
        const string Id = "Skill.InterruptAttack";
        var knight = players.FirstOrDefault(p => p.name == "Knight_Unit");
        if (knight == null || knight.brain == null || knight.health == null ||
            knight.skills == null || knight.sword == null)
        {
            sink.BlockedAssertion(Id, Id, "普攻出招中施放 Q：Cast 成功、普攻不再命中、≤0.6s 进入 Q_1",
                "缺少 Knight_Unit 或 SkillSystem/MeleeCombat", null);
            yield break;
        }
        if (knight.health.dead)
        {
            sink.BlockedAssertion(Id, Id, "普攻出招中施放 Q：Cast 成功、普攻不再命中、≤0.6s 进入 Q_1",
                "骑士已倒地", null);
            yield break;
        }

        _target = CreateTarget("REFACTOR_Target_SkillIntAttack", knight.tx.position + knight.tx.forward * 1.8f);
        if (_target == null)
        {
            sink.InfraFailure(Id, Id, "技能打断普攻靶夹具创建失败", "");
            yield break;
        }
        var targetHealth = _target.GetComponent<UnitHealth>();

        int slashHits = 0;
        int bluntHits = 0;
        System.Action<DamageInfo> onHit = info =>
        {
            if (info.source == null || info.source != knight.health) return;
            if (info.damageType == DamageType.Slash) slashHits++;
            else if (info.damageType == DamageType.Blunt) bluntHits++;
        };
        targetHealth.OnHit += onHit;

        bool attacking = false;
        bool castOk = false;
        float attackTimerAfterCast = -2f;
        bool bashEntered = false;
        float bashWait = -1f;
        float attackNormAtCast = -1f;
        int slashHitsBeforeCast = -1;
        bool bashEnteredLater = false;
        float bashActualWait = -1f;

        try
        {
            RefillSkills(knight);

            knight.brain.enabled = false;
            knight.brain.CommandAttack(_target.transform);
            knight.brain.enabled = true;

            // 等到 AttackMelee 的命中窗口之后（normalizedTime ≥ 0.6）：第一次普攻已结算、仍在出招
            float t = 0f;
            while (t < 8f)
            {
                yield return null;
                t += Time.deltaTime;
                if (QaFixtures.StateName(knight.animator) != AttackMelee) continue;
                attacking = true;
                if (QaFixtures.StateNorm(knight.animator) >= 0.6f) break;
            }
            attackNormAtCast = attacking ? QaFixtures.StateNorm(knight.animator) : -1f;
            slashHitsBeforeCast = slashHits;

            if (attacking)
            {
                castOk = knight.skills.Cast(0, _target.transform);       // 生产入口
                attackTimerAfterCast = GetAttackTimer(knight.sword);     // CancelAttack 后应为 -1
                // 隔离：Cast 后不让大脑再自动补普攻（否则观测窗口里会出现“新一次攻击”的命中，混入计次）
                knight.brain.enabled = false;

                float w = 0f;
                while (w < 0.6f)
                {
                    if (QaFixtures.StateName(knight.animator) == "Q_1") { bashEntered = true; bashWait = w; break; }
                    yield return null;
                    w += Time.deltaTime;
                }

                // 未在 0.6s 进入的，再宽限观察 1.5s 记录真实耗时（只记录，不参与通过判定）
                if (!bashEntered)
                {
                    float w2 = 0f;
                    while (w2 < 1.5f)
                    {
                        if (QaFixtures.StateName(knight.animator) == "Q_1")
                        { bashEnteredLater = true; bashActualWait = w + w2; break; }
                        yield return null;
                        w2 += Time.deltaTime;
                    }
                }

                // 覆盖完整普攻判定窗口：窗口若未被收掉，这里应看到第二次 Slash 命中
                float hold = 0f;
                while (hold < 0.8f) { yield return null; hold += Time.deltaTime; }
            }
        }
        finally
        {
            targetHealth.OnHit -= onHit;
            knight.brain.enabled = true;
        }

        knight.brain.CommandAttack(null);
        var combat = knight.brain.combat as ICharacterCombat;
        if (combat != null) combat.CancelAttack();
        RestoreAgent(knight);
        yield return QaWait.WaitState(knight.animator, Locomotion, 6f);
        DestroyTarget();

        bool noSecondHit = slashHitsBeforeCast >= 0 && slashHits == slashHitsBeforeCast;
        bool pass = attacking && castOk && attackTimerAfterCast < 0f &&
                    bashEntered && bashWait >= 0f && bashWait <= 0.6f && noSecondHit;
        sink.Record(Id, Id,
            "骑士在 AttackMelee 出招中（窗口已过、仍在收招）立即 SkillSystem.Cast(0,Q)：Cast 返回 true，" +
            "MeleeCombat 攻击计时被收(-1)，普攻不再产生第二次 Slash 命中，且 ≤0.6s 进入 Q_1（盾击 Blunt 命中照常结算）",
            "attacking=" + attacking + "; normAtCast=" + F(attackNormAtCast) + "; cast=" + castOk +
            "; attackTimerAfterCast=" + F(attackTimerAfterCast) +
            "; bash=" + bashEntered + "(" + F(bashWait) + "s)" +
            (bashEnteredLater ? "; bashLater=" + F(bashActualWait) + "s" : "") +
            "; slash=" + slashHitsBeforeCast + "→" + slashHits + "; bluntHits=" + bluntHits,
            pass, pass ? "pass" : "product_fail", "production",
            QaJson.Dict("attackingBeforeCast", attacking, "attackNormalizedAtCast", attackNormAtCast,
                 "castReturn", castOk, "attackTimerAfterCast", attackTimerAfterCast,
                 "bashEnteredWithin0_6s", bashEntered, "bashWaitSeconds", bashWait,
                 "bashEnteredLater", bashEnteredLater, "bashActualWaitSeconds", bashActualWait,
                 "slashHitsBeforeCast", slashHitsBeforeCast, "slashHitsAfterCast", slashHits,
                 "bluntHitsAfterCast", bluntHits,
                 "note", "PlayerBase.controller 的 Q 触发过渡只挂在 Locomotion 状态（Locomotion→Q），" +
                         "故出招中施放时 Q_1 会在 AttackMelee 收招后进入；本断言在命中窗口之后立即施放，覆盖“第一下已结算、不许有第二下”"));
    }
    // ============================================================
    // M4-B. Hit.InterruptSkill（M4 新增）
    // ============================================================
    /// <summary>施放 W（HeavySlashEffect → Skill 动画）后 0.1s 内对施法者造成 1 点伤害：
    /// ≤0.5s 进入 Hit_A，且单位未死（hp > 0）。</summary>
    IEnumerator AssertHitInterruptSkill(IQaSink sink, IReadOnlyList<QaFixtures.PlayerProbe> players)
    {
        const string Id = "Hit.InterruptSkill";
        var knight = players.FirstOrDefault(p => p.name == "Knight_Unit");
        if (knight == null || knight.brain == null || knight.health == null || knight.skills == null)
        {
            sink.BlockedAssertion(Id, Id, "施放 W 后受击 → ≤0.5s 进入 Hit_A 且未死",
                "缺少 Knight_Unit 或 SkillSystem/UnitHealth", null);
            yield break;
        }
        if (knight.health.dead)
        {
            sink.BlockedAssertion(Id, Id, "施放 W 后受击 → ≤0.5s 进入 Hit_A 且未死", "骑士已倒地", null);
            yield break;
        }

        RefillSkills(knight);
        bool castOk = knight.skills.Cast(1, null);       // 生产入口：W = HeavySlashEffect（Skill 动画）

        float pre = 0f;
        while (pre < 0.1f) { yield return null; pre += Time.deltaTime; }

        float hpBefore = knight.health.hp;
        knight.health.TakeHit(1f, 999f, DamageType.Blunt, knight.tx.position + Vector3.up, null);
        float hpAfter = knight.health.hp;
        bool hitApplied = Mathf.Abs(hpBefore - hpAfter - 1f) < 0.01f;

        string observed = QaFixtures.StateName(knight.animator);
        bool hitEntered = false;
        float hitWait = -1f;
        float h = 0f;
        while (h < 0.5f)
        {
            yield return null;
            h += Time.deltaTime;
            observed = QaFixtures.StateName(knight.animator);
            if (observed == HitA) { hitEntered = true; hitWait = h; break; }
        }
        if (!hitEntered) observed = QaFixtures.StateName(knight.animator);

        knight.brain.CommandAttack(null);
        var combat = knight.brain.combat as ICharacterCombat;
        if (combat != null) combat.CancelAttack();
        RestoreAgent(knight);
        yield return QaWait.WaitState(knight.animator, Locomotion, 6f);

        bool pass = castOk && hitApplied && hitEntered && hitWait >= 0f && hitWait <= 0.5f && hpAfter > 0f;
        sink.Record(Id, Id,
            "骑士经 SkillSystem.Cast(1,W) 进入技能后 0.1s 受 1 点伤害：≤0.5s 进入 Hit_A，伤害只结算 1 点且单位未死",
            "cast=" + castOk + "; observed=" + observed + "; hitWait=" + F(hitWait) + "s" +
            "; hp=" + F(hpBefore) + "→" + F(hpAfter),
            pass, pass ? "pass" : "product_fail", "production",
            QaJson.Dict("castReturn", castOk, "stateAfterHit", observed, "hitWaitSeconds", hitWait,
                 "hpBefore", hpBefore, "hpAfter", hpAfter,
                 "entry", "SkillSystem.Cast(1,null) → HeavySlashEffect；UnitHealth.TakeHit(1f) → CharacterBrain.OnDamaged → Animator.SetTrigger(\"Hit\") → AnyState→Hit_A"));
    }
    // ============================================================
    // M4-C. Skill.InterruptInteract（M4 新增）
    // ============================================================
    /// <summary>技能（Bash）进行中对同一单位下交互指令：InteractionRunner.HasPending=false 且不朝宝箱位移；
    /// 技能结束后同一条指令能正常产生待办。宝箱为运行期内存夹具，结束前销毁。</summary>
    IEnumerator AssertSkillInterruptInteract(IQaSink sink, IReadOnlyList<QaFixtures.PlayerProbe> players)
    {
        const string Id = "Skill.InterruptInteract";
        var knight = players.FirstOrDefault(p => p.name == "Knight_Unit");
        if (knight == null || knight.brain == null || knight.health == null || knight.skills == null)
        {
            sink.BlockedAssertion(Id, Id, "技能期间交互指令无效（无待办/不位移），技能后可下单",
                "缺少 Knight_Unit 或 SkillSystem", null);
            yield break;
        }
        if (knight.health.dead)
        {
            sink.BlockedAssertion(Id, Id, "技能期间交互指令无效（无待办/不位移），技能后可下单", "骑士已倒地", null);
            yield break;
        }

        GameObject chestGo = null;
        ItemData chestItem = null;
        var rows = new List<object>();
        bool castOk = false, inBash = false, skillEnded = false;
        bool pendingDuringSkill = true, pendingAfterSkill = false;
        float movedDuringSkill = -1f;
        float bashWait = -1f;

        try
        {
            // ---- 夹具：宝箱（结构同 Refactor.InteractionContract；锚点在骑士前方 6m，走过去会明显位移） ----
            Vector3 chestPos = knight.tx.position + knight.tx.forward * 6f;
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(chestPos, out navHit, 3f, NavMesh.AllAreas)) chestPos = navHit.position;

            chestGo = new GameObject("REFACTOR_SkillInterruptChest");
            chestGo.transform.position = chestPos;
            var body = chestGo.AddComponent<BoxCollider>();
            body.center = new Vector3(0f, 0.4f, 0f);
            body.size = new Vector3(0.8f, 0.8f, 0.8f);

            var lid = new GameObject("lid");
            lid.transform.SetParent(chestGo.transform, false);
            lid.transform.localPosition = new Vector3(0f, 0.8f, 0f);

            var itemGo = new GameObject("item");
            itemGo.transform.SetParent(chestGo.transform, false);
            var itemComp = itemGo.AddComponent<ChestItem>();
            chestItem = ScriptableObject.CreateInstance<ItemData>();
            chestItem.itemName = "REFACTOR_技能打断交互物品";
            chestItem.goldValue = 0;
            itemComp.data = chestItem;

            var inv = chestGo.AddComponent<Inventory>();
            inv.capacity = 4;
            var marker = chestGo.AddComponent<Interactable>();     // 先于 Chest：Chest.Awake 会取它做 Hint
            marker.hint = "点击开启宝箱";
            var chest = chestGo.AddComponent<Chest>();
            chest.lid = lid.transform;
            chest.openDuration = 0f;

            _target = CreateTarget("REFACTOR_Target_SkillIntInteract",
                                   knight.tx.position + knight.tx.forward * 1.8f);
            if (_target == null)
            {
                sink.InfraFailure(Id, Id, "技能打断交互靶夹具创建失败", "");
                yield break;
            }

            var runner = InteractionRunner.Ensure(knight.brain);
            knight.brain.CancelPendingInteract();

            // ---- 1. 进入 Q_1 ----
            RefillSkills(knight);
            castOk = knight.skills.Cast(0, _target.transform);     // 生产入口
            float t = 0f;
            while (t < 1.5f)
            {
                yield return null;
                t += Time.deltaTime;
                if (QaFixtures.StateName(knight.animator) == "Q_1") { inBash = true; bashWait = t; break; }
            }

            // ---- 2. 技能期间下交互指令：不应产生待办，也不应朝宝箱位移 ----
            Vector3 posBefore = knight.tx.position;
            knight.brain.CommandInteract(marker);                  // 生产入口（与点击宝箱同一条）
            pendingDuringSkill = runner != null && runner.HasPending;
            float hold = 0f;
            while (hold < 0.25f) { yield return null; hold += Time.deltaTime; }
            movedDuringSkill = Vector3.Distance(posBefore, knight.tx.position);

            // ---- 3. 等技能收尾，再下一次同样的指令：应正常产生待办 ----
            float e = 0f;
            while (e < 4f)
            {
                yield return null;
                e += Time.deltaTime;
                string st = QaFixtures.StateName(knight.animator);
                if (!IsSkillStateName(st)) { skillEnded = true; break; }
            }

            knight.brain.CommandInteract(marker);
            pendingAfterSkill = runner != null && runner.HasPending;

            // ---- 收尾：撤销待办、恢复导航、回原位 ----
            knight.brain.CancelPendingInteract();
            RestoreAgent(knight);
            knight.brain.CommandMove(posBefore);
            float back = 0f;
            while (back < 3f && Vector3.Distance(knight.tx.position, posBefore) > 0.25f)
            { yield return null; back += Time.deltaTime; }
            float returnMoveDistance = Vector3.Distance(knight.tx.position, posBefore);

            bool pass = castOk && inBash && !pendingDuringSkill &&
                        movedDuringSkill >= 0f && movedDuringSkill < 0.05f &&
                        skillEnded && pendingAfterSkill;

            rows.Add(QaJson.Dict("step", "cast", "castReturn", castOk, "bashEntered", inBash,
                          "bashWaitSeconds", bashWait, "pass", castOk && inBash));
            rows.Add(QaJson.Dict("step", "interactDuringSkill", "hasPending", pendingDuringSkill,
                          "movedMeters", movedDuringSkill,
                          "pass", !pendingDuringSkill && movedDuringSkill < 0.05f));
            rows.Add(QaJson.Dict("step", "interactAfterSkill", "skillEnded", skillEnded,
                          "hasPending", pendingAfterSkill, "pass", skillEnded && pendingAfterSkill));
            rows.Add(QaJson.Dict("step", "cleanup", "returnMoveMeters", returnMoveDistance,
                          "note", "技能期间的运动锁由 UnitMotion 负责（施法即停），收尾恢复可动并回原位"));

            sink.Record(Id, Id,
                "SkillSystem.Cast(0,Q) 进入 Q_1 后对同一单位 CommandInteract(宝箱夹具)：InteractionRunner.HasPending=false 且 0.25s 内不位移；" +
                "技能结束后再次 CommandInteract 能产生 HasPending=true",
                "cast=" + castOk + "; bash=" + inBash + "; pendingDuring=" + pendingDuringSkill +
                "; moved=" + F(movedDuringSkill) + "m; skillEnded=" + skillEnded +
                "; pendingAfter=" + pendingAfterSkill,
                pass, pass ? "pass" : "product_fail", "production",
                QaJson.Dict("steps", rows,
                     "fixture", "REFACTOR_SkillInterruptChest（运行期内存，结束前销毁；不写场景）",
                     "entry", "SkillSystem.Cast(0,target) → BashEffect；CharacterBrain.CommandInteract(Interactable) → InteractionRunner.Order（技能门禁 CanAcceptInteract）"));
        }
        finally
        {
            if (chestGo != null) Destroy(chestGo);
            if (chestItem != null) Destroy(chestItem);
            DestroyTarget();
        }
    }
    // ============================================================
    // M4-D. Projectile.AimPoint（M4 新增）
    // ============================================================
    /// <summary>子弹落点 = 目标所有 Renderer 的合并包围盒中心（容差 0.05m），且不再是 position+0.6；
    /// 无 Renderer 的目标回退 transform.position。夹具为运行期内存对象，结束前销毁。</summary>
    IEnumerator AssertProjectileAimPoint(IQaSink sink)
    {
        const string Id = "Projectile.AimPoint";
        GameObject projGo = null;
        GameObject bareGo = null;
        UnitStats bareStats = null;
        var rows = new List<object>();

        try
        {
            // ---- 1. 带 Renderer 的目标：两个同中心、不同大小的 Renderer 合并后中心仍是 +1.8m ----
            _target = CreateTarget("REFACTOR_AimTarget", new Vector3(120f, 0f, 120f));
            if (_target == null)
            {
                sink.InfraFailure(Id, Id, "瞄准点靶夹具创建失败", "");
                yield break;
            }
            var uh = _target.GetComponent<UnitHealth>();
            var cubeA = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeA.name = "aim_renderer_a";
            cubeA.transform.SetParent(_target.transform, false);
            cubeA.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            var cubeB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeB.name = "aim_renderer_b";
            cubeB.transform.SetParent(_target.transform, false);
            cubeB.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            cubeB.transform.localScale = Vector3.one * 0.4f;

            Vector3 basePos = _target.transform.position;
            Vector3 expected = basePos + Vector3.up * 1.8f;
            Vector3 legacy = basePos + Vector3.up * 0.6f;

            projGo = new GameObject("REFACTOR_AimProjectile");
            projGo.transform.position = basePos + new Vector3(0f, 1.1f, -3f);
            var proj = projGo.AddComponent<Projectile>();
            proj.target = uh;
            proj.duration = 1000f;               // 断言期间不飞行/不回收
            proj.Launch(null);

            Vector3 end = ComputeEndOf(proj);
            int rendererCount = uh.GetComponentsInChildren<Renderer>().Length;
            float dExpected = Vector3.Distance(end, expected);
            float dLegacy = Vector3.Distance(end, legacy);
            bool aimOk = rendererCount >= 2 && dExpected <= 0.05f && dLegacy > 0.1f;
            rows.Add(QaJson.Dict("step", "rendererAim", "rendererCount", rendererCount,
                          "computeEnd", QaJson.V3(end), "mergedBoundsCenter", QaJson.V3(expected),
                          "legacyPositionPlus06", QaJson.V3(legacy),
                          "distanceToCenter", dExpected, "distanceToLegacy", dLegacy, "pass", aimOk));

            // ---- 2. 无 Renderer 的目标：回退 transform.position（不再 +0.6m） ----
            Destroy(projGo);
            projGo = null;
            DestroyTarget();

            bareGo = new GameObject("REFACTOR_AimBare");
            bareGo.transform.position = new Vector3(140f, 0f, 140f);
            bareStats = ScriptableObject.CreateInstance<UnitStats>();
            bareStats.team = Team.Monster;
            bareStats.maxHp = 100f;
            bareStats.damage = 0f;
            bareStats.aggroRange = 0f;
            bareStats.moveSpeed = 0f;
            var buh = bareGo.AddComponent<UnitHealth>();
            buh.stats = bareStats;
            buh.showDamageNumber = false;

            var proj2Go = new GameObject("REFACTOR_AimProjectile_Bare");
            proj2Go.transform.position = bareGo.transform.position + new Vector3(0f, 1f, -3f);
            var proj2 = proj2Go.AddComponent<Projectile>();
            proj2.target = buh;
            proj2.duration = 1000f;
            proj2.Launch(null);
            Vector3 end2 = ComputeEndOf(proj2);
            float dFallback = Vector3.Distance(end2, bareGo.transform.position);
            bool fallbackOk = buh.GetComponentsInChildren<Renderer>().Length == 0 && dFallback <= 0.001f;
            Destroy(proj2Go);
            rows.Add(QaJson.Dict("step", "fallbackNoRenderer", "rendererCount", buh.GetComponentsInChildren<Renderer>().Length,
                          "computeEnd", QaJson.V3(end2), "targetPosition", QaJson.V3(bareGo.transform.position),
                          "distance", dFallback, "pass", fallbackOk));

            bool pass = aimOk && fallbackOk;
            sink.Record(Id, Id,
                "Projectile.ComputeEnd：有 Renderer 的目标取合并包围盒中心（容差 0.05m，距 position+0.6 超过 0.1m）；" +
                "无 Renderer 的目标回退 transform.position",
                "aimOk=" + aimOk + "; fallbackOk=" + fallbackOk +
                "; distanceToCenter=" + F(dExpected) + "m; distanceToLegacy=" + F(dLegacy) + "m" +
                "; fallbackDistance=" + F(dFallback) + "m",
                pass, pass ? "pass" : "product_fail", "production",
                QaJson.Dict("steps", rows,
                     "fixture", "REFACTOR_AimTarget（含 2 个 MeshRenderer 子物体）/ REFACTOR_AimBare（无 Renderer），结束前销毁",
                     "entry", "Projectile.Launch → 反射调用 protected ComputeEnd()"));
        }
        finally
        {
            if (projGo != null) Destroy(projGo);
            if (bareGo != null) Destroy(bareGo);
            if (bareStats != null) Destroy(bareStats);
            DestroyTarget();
        }
    }
    // ============================================================
    // 5. Hit.InterruptAttack
    // ============================================================
    IEnumerator AssertHitInterrupt(IQaSink sink, IReadOnlyList<QaFixtures.PlayerProbe> players)
    {
        const string Id = "Hit.InterruptAttack";
        var knight = players.FirstOrDefault(p => p.name == "Knight_Unit");
        if (knight == null || knight.brain == null || knight.health == null)
        {
            sink.BlockedAssertion(Id, Id, "普攻中受击被 Hit_A 打断", "未找到 Knight_Unit", null);
            yield break;
        }
        if (knight.health.dead)
        {
            sink.BlockedAssertion(Id, Id, "普攻中受击被 Hit_A 打断", "骑士已倒地", null);
            yield break;
        }

        _target = CreateTarget("REFACTOR_Target_HitInt", knight.tx.position + knight.tx.forward * 1.8f);
        if (_target == null)
        {
            sink.InfraFailure(Id, Id, "受击打断靶夹具创建失败", "");
            yield break;
        }

        knight.brain.enabled = false;
        knight.brain.CommandAttack(_target.transform);
        knight.brain.enabled = true;

        // 等进入 AttackMelee（正在出招）
        float t = 0f;
        bool attacking = false;
        while (t < 6f)
        {
            yield return null;
            t += Time.deltaTime;
            if (QaFixtures.StateName(knight.animator) == AttackMelee) { attacking = true; break; }
        }

        string observed = QaFixtures.StateName(knight.animator);
        float hitWait = -1f;
        float hpBefore = knight.health.hp;
        if (attacking)
        {
            knight.health.ApplyDamage(1f, knight.tx.position + Vector3.up);
            float h = 0f;
            while (h < 1.5f)
            {
                yield return null;
                h += Time.deltaTime;
                observed = QaFixtures.StateName(knight.animator);
                if (observed == HitA) { hitWait = h; break; }
            }
        }
        float hpAfter = knight.health.hp;

        knight.brain.CommandAttack(null);
        var combat = knight.brain.combat as ICharacterCombat;
        if (combat != null) combat.CancelAttack();
        yield return QaWait.WaitState(knight.animator, Locomotion, 6f);
        DestroyTarget();

        bool pass = attacking && hitWait >= 0f && observed == HitA &&
                    Mathf.Abs(hpBefore - hpAfter - 1f) < 0.01f;
        sink.Record(Id, Id,
            "骑士在 AttackMelee 出招中受 1 点伤害：≤1.5s 内进入 Hit_A，且伤害只结算 1 点",
            "attacking=" + attacking + "; observed=" + observed +
            "; hitWait=" + F(hitWait) + "s; hp=" + F(hpBefore) + "→" + F(hpAfter),
            pass, pass ? "pass" : "product_fail", "production",
            QaJson.Dict("attackingBeforeHit", attacking, "observedState", observed, "hitWaitSeconds", hitWait,
                 "hpBefore", hpBefore, "hpAfter", hpAfter,
                 "entry", "UnitHealth.ApplyDamage(1f) → CharacterBrain.OnDamaged → Animator.SetTrigger(\"Hit\") → AnyState→Hit_A"));
    }
    // ============================================================
    // 6. Downed.Knockdown
    // ============================================================
    IEnumerator AssertKnockdown(IQaSink sink, IReadOnlyList<QaFixtures.PlayerProbe> players)
    {
        const string Id = "Downed.Knockdown";
        var knight = players.FirstOrDefault(p => p.name == "Knight_Unit");
        if (knight == null || knight.health == null || knight.rescue == null)
        {
            sink.BlockedAssertion(Id, Id, "致死进入 Downed", "骑士缺少 UnitHealth/DownedRescue", null);
            yield break;
        }
        if (knight.health.dead)
        {
            sink.BlockedAssertion(Id, Id, "致死进入 Downed", "骑士已倒地", null);
            yield break;
        }

        knight.health.ApplyDamage(knight.health.maxHp * 4f, knight.tx.position + Vector3.up);

        float t = 0f;
        string observed = QaFixtures.StateName(knight.animator);
        bool downedAnim = false;
        while (t < 3f)
        {
            yield return null;
            t += Time.deltaTime;
            observed = QaFixtures.StateName(knight.animator);
            if (observed == Downed) { downedAnim = true; break; }
        }

        bool isDown = knight.rescue.IsDown;
        bool brainOff = knight.brain != null && !knight.brain.enabled;
        bool agentOff = knight.brain != null && knight.brain.agent != null && !knight.brain.agent.enabled;

        bool pass = isDown && downedAnim && brainOff && agentOff;
        sink.Record(Id, Id,
            "骑士接受致死伤害后：DownedRescue.IsDown=true，layer0 进入 Downed，CharacterBrain 与 NavMeshAgent 停用",
            "isDown=" + isDown + "; anim=" + observed + "; wait=" + F(t) + "s" +
            "; brainEnabled=" + (knight.brain != null && knight.brain.enabled) +
            "; agentEnabled=" + (knight.brain != null && knight.brain.agent != null && knight.brain.agent.enabled),
            pass, pass ? "pass" : "product_fail", "production",
            QaJson.Dict("isDown", isDown, "observedState", observed, "waitSeconds", t,
                 "brainEnabled", knight.brain != null && knight.brain.enabled,
                 "agentEnabled", knight.brain != null && knight.brain.agent != null && knight.brain.agent.enabled,
                 "entry", "UnitHealth.ApplyDamage(致死) → OnDeath → DownedRescue.OnDowned"));

        // 保持倒地状态给下一条救援断言用
        yield return null;
    }
    // ============================================================
    // 7. Rescue.ChannelInterrupt
    // ============================================================
    IEnumerator AssertRescue(IQaSink sink, IReadOnlyList<QaFixtures.PlayerProbe> players)
    {
        const string Id = "Rescue.ChannelInterrupt";
        var downed = players.FirstOrDefault(p => p.name == "Knight_Unit");
        var rescuer = players.FirstOrDefault(p => p.name == "Archer_Unit" && !p.health.dead && p.health != (downed != null ? downed.health : null));
        if (downed == null || rescuer == null || downed.rescue == null)
        {
            sink.BlockedAssertion(Id, Id, "救援指令 → 读条 → 受击打断 → 重新下令 → 救起",
                "缺少倒地者或可用救援者", null);
            yield break;
        }
        if (!downed.rescue.IsDown)
        {
            sink.BlockedAssertion(Id, Id, "救援指令 → 读条 → 受击打断 → 重新下令 → 救起",
                "骑士未处于倒地状态（上一条 Downed.Knockdown 未成立）", null);
            yield break;
        }

        // 下达救援指令：走生产入口 DownedRescue.CommandRescue（选择器点击救援时调用的同一条）
        downed.rescue.CommandRescue(rescuer.health);

        float t = 0f;
        bool channeling = false;
        while (t < 20f)
        {
            yield return null;
            t += Time.deltaTime;
            if (downed.rescue.IsChanneling) { channeling = true; break; }
        }
        float arriveSeconds = t;
        string stateWhileChanneling = QaFixtures.StateName(rescuer.animator);

        // 受击打断：救援者受 1 点伤害
        bool interrupted = false;
        float it = 0f;
        if (channeling)
        {
            rescuer.health.ApplyDamage(1f, rescuer.tx.position + Vector3.up);
            while (it < 2.5f)
            {
                yield return null;
                it += Time.deltaTime;
                if (!downed.rescue.IsChanneling) { interrupted = true; break; }
            }
        }
        bool stillDownAfterInterrupt = downed.rescue.IsDown;

        // 重新下令 → 读完条救起
        float reviveSeconds = -1f;
        bool revived = false;
        bool brainBack = false;
        if (interrupted && stillDownAfterInterrupt)
        {
            downed.rescue.CommandRescue(rescuer.health);
            float r = 0f;
            while (r < 25f)
            {
                yield return null;
                r += Time.deltaTime;
                if (!downed.rescue.IsDown) { revived = true; reviveSeconds = r; break; }
            }
            float b = 0f;
            while (b < 4f)
            {
                yield return null;
                b += Time.deltaTime;
                if (downed.brain != null && downed.brain.enabled) { brainBack = true; break; }
            }
        }

        float reviveHp = downed.health.hp;
        bool hpOk = revived && Mathf.Abs(reviveHp - downed.rescue.reviveHp) < 0.01f;
        bool pass = channeling && interrupted && stillDownAfterInterrupt && revived && hpOk && brainBack;

        sink.Record(Id, Id,
            "救援指令让救援者走到范围并读条；读条中救援者受击 → 读条中断且目标仍倒地；重新下令后读完条救起（血量=reviveHp）并恢复大脑",
            "channeling=" + channeling + "; arrive=" + F(arriveSeconds) + "s; interrupted=" + interrupted +
            "; stillDown=" + stillDownAfterInterrupt + "; revived=" + revived +
            "; revive=" + F(reviveSeconds) + "s; hp=" + F(reviveHp) + "; brainBack=" + brainBack,
            pass, pass ? "pass" : "product_fail", "production",
            QaJson.Dict("channeling", channeling, "arriveSeconds", arriveSeconds,
                 "stateWhileChanneling", stateWhileChanneling,
                 "interrupted", interrupted, "interruptDetectSeconds", it,
                 "stillDownAfterInterrupt", stillDownAfterInterrupt,
                 "revived", revived, "reviveSeconds", reviveSeconds,
                 "hpAfterRevive", reviveHp, "expectedReviveHp", downed.rescue.reviveHp,
                 "brainBack", brainBack,
                 "downed", downed.name, "rescuer", rescuer.name,
                 "entry", "DownedRescue.CommandRescue(UnitHealth)（选择器点击救援同一入口）→ CharacterBrain.CommandInteract 跑位 → RescueRoutine 读条"));
    }
    // ============================================================
    // 8. Chest.Interact
    // ============================================================
    IEnumerator AssertChest(IQaSink sink, IReadOnlyList<QaFixtures.PlayerProbe> players)
    {
        const string Id = "Chest.Interact";
        var mage = players.FirstOrDefault(p => p.name == "Mage_Unit");
        if (mage == null || mage.brain == null || mage.brain.agent == null)
        {
            sink.BlockedAssertion(Id, Id, "走到宝箱范围后开盖", "未找到 Mage_Unit", null);
            yield break;
        }

        Vector3 chestPos = mage.tx.position + mage.tx.forward * 2.4f;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(chestPos, out hit, 2.0f, NavMesh.AllAreas)) chestPos = hit.position;

        _chestGo = new GameObject("REFACTOR_Chest");
        _chestGo.transform.position = chestPos;
        var body = _chestGo.AddComponent<BoxCollider>();
        body.center = new Vector3(0f, 0.4f, 0f);
        body.size = new Vector3(0.8f, 0.8f, 0.8f);

        var lid = new GameObject("lid");
        lid.transform.SetParent(_chestGo.transform, false);
        lid.transform.localPosition = new Vector3(0f, 0.8f, 0f);

        var itemGo = new GameObject("item");
        itemGo.transform.SetParent(_chestGo.transform, false);
        itemGo.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        var chestItem = itemGo.AddComponent<ChestItem>();
        _chestItem = ScriptableObject.CreateInstance<ItemData>();
        _chestItem.itemName = "REFACTOR_测试物品";
        _chestItem.goldValue = 0;
        chestItem.data = _chestItem;

        var inv = _chestGo.AddComponent<Inventory>();
        inv.capacity = 4;

        var chest = _chestGo.AddComponent<Chest>();   // Awake 会隐藏 ChestItem 子物体
        chest.lid = lid.transform;
        chest.openDuration = 0.3f;

        var interact = _chestGo.AddComponent<Interactable>();
        interact.hint = "点击开启宝箱";

        Quaternion lidBefore = lid.transform.localRotation;
        QaFixtures.Select(mage);
        mage.brain.CommandInteract(interact);

        float t = 0f;
        bool opened = false;
        while (t < 15f)
        {
            yield return null;
            t += Time.deltaTime;
            if (ChestOpened(chest)) { opened = true; break; }
        }

        // 开盖动画 0.3s，等它走完再读角度与内容物
        yield return new WaitForSeconds(0.8f);
        float lidAngle = Quaternion.Angle(lidBefore, lid.transform.localRotation);
        bool itemVisible = itemGo.activeSelf;
        bool inventoryBound = inv.items != null && inv.items.Count > 0 && inv.items.Exists(i => i == _chestItem);

        bool pass = opened && lidAngle > 30f && itemVisible && inventoryBound;
        sink.Record(Id, Id,
            "经生产交互入口（CharacterBrain.CommandInteract）让法师走到宝箱范围：宝箱进入已开启，盖子旋转 >30°，内容物显形且已写入宝箱背包",
            "opened=" + opened + "; wait=" + F(t) + "s; lidAngle=" + F(lidAngle) +
            "; itemVisible=" + itemVisible + "; inventoryBound=" + inventoryBound,
            pass, pass ? "pass" : "product_fail", "production",
            QaJson.Dict("opened", opened, "waitSeconds", t, "lidAngle", lidAngle,
                 "itemVisible", itemVisible, "inventoryBound", inventoryBound,
                 "chestPosition", QaJson.V3(chestPos), "caster", mage.name,
                 "fixture", "REFACTOR_Chest（运行期内存，含 Interactable/Chest/Inventory/ChestItem，结束前销毁）",
                 "entry", "CharacterBrain.CommandInteract(Interactable) → 走到范围 → CharacterBrain.Update → Chest.OpenBy"));
    }
    static bool ChestOpened(Chest chest)
    {
        if (chest == null) return false;
        if (_chestOpenedField == null)
            _chestOpenedField = typeof(Chest).GetField("_opened", BindingFlags.Instance | BindingFlags.NonPublic);
        return _chestOpenedField != null && (bool)_chestOpenedField.GetValue(chest);
    }

    // ============================================================
    // 11. Refactor.InteractionContract（M3 新增）
    // ============================================================
    /// <summary>交互契约：三个交互目标都实现 IInteractionTarget；KeyInteractable/Priority/IsAvailable 取值成立；
    /// 宝箱 Opened 事件恰好一次且重复 Interact 幂等；InteractionRunner.Ensure 不重复挂组件。
    /// 夹具全部运行期内存创建并在断言结束前销毁（不写场景）。</summary>
    IEnumerator AssertInteractionContract(IQaSink sink, IReadOnlyList<QaFixtures.PlayerProbe> players)
    {
        const string Id = "Refactor.InteractionContract";
        var rows = new List<object>();
        bool allPass = true;

        var unit = players.FirstOrDefault(p => p.brain != null && p.health != null && !p.health.dead);
        var downed = players.FirstOrDefault(p => p.rescue != null);
        if (unit == null || downed == null)
        {
            sink.BlockedAssertion(Id, Id, "三个交互目标实现 IInteractionTarget / 契约取值 / 开箱事件恰好一次 / Runner 复用",
                "缺少可用玩家或 DownedRescue", null);
            yield break;
        }

        // 场景里没有 Chest / DungeonDoor 实例（全工程 .unity/.prefab 无这两个脚本的 GUID），
        // 因此“各取一个实例”= 倒地救援用场景真实组件，宝箱与门用运行期内存夹具（结束前销毁，不写场景）。
        GameObject chestGo = null;
        GameObject doorGo = null;
        ItemData chestItem = null;
        Chest chest = null;
        Interactable chestMarker = null;
        int openedCount = 0;
        System.Action<Chest> onOpened = c => { if (c == chest) openedCount++; };

        try
        {
            // ---- 夹具：宝箱（Collider + lid + 内容物 + Inventory + Interactable + Chest） ----
            chestGo = new GameObject("REFACTOR_ContractChest");
            var body = chestGo.AddComponent<BoxCollider>();
            body.center = new Vector3(0f, 0.4f, 0f);
            body.size = new Vector3(0.8f, 0.8f, 0.8f);

            var lid = new GameObject("lid");
            lid.transform.SetParent(chestGo.transform, false);
            lid.transform.localPosition = new Vector3(0f, 0.8f, 0f);

            var itemGo = new GameObject("item");
            itemGo.transform.SetParent(chestGo.transform, false);
            var chestItemComp = itemGo.AddComponent<ChestItem>();
            chestItem = ScriptableObject.CreateInstance<ItemData>();
            chestItem.itemName = "REFACTOR_契约测试物品";
            chestItem.goldValue = 0;
            chestItemComp.data = chestItem;

            var inv = chestGo.AddComponent<Inventory>();
            inv.capacity = 4;
            chestMarker = chestGo.AddComponent<Interactable>();   // 先于 Chest：Chest.Awake 会取它做 Hint
            chestMarker.hint = "点击开启宝箱";
            chest = chestGo.AddComponent<Chest>();
            chest.lid = lid.transform;
            chest.openDuration = 0f;                              // 事件在 Interact 内同步抛，判定可重复

            // ---- 夹具：门（不调 Interact，避免传送干扰后续断言；放到远处避免影响提示轮询） ----
            doorGo = new GameObject("REFACTOR_ContractDoor");
            doorGo.transform.position = unit.tx.position + new Vector3(200f, 0f, 200f);
            var door = doorGo.AddComponent<DungeonDoor>();
            door.interactRange = 3.5f;

            // ---- 1. 三个交互目标都实现接口；Interactable 标记能解析到目标 ----
            bool chestIsTarget = chest is IInteractionTarget;
            bool rescueIsTarget = downed.rescue is IInteractionTarget;
            bool doorIsTarget = door is IInteractionTarget;
            bool markerResolves = chestMarker.Target() != null && (object)chestMarker.Target() == chest;
            bool implementsOk = chestIsTarget && rescueIsTarget && doorIsTarget && markerResolves;
            allPass &= implementsOk;
            rows.Add(QaJson.Dict("step", "implements", "chest", chestIsTarget, "downedRescue", rescueIsTarget,
                          "dungeonDoor", doorIsTarget, "interactableMarkerResolves", markerResolves,
                          "chestSource", "运行期夹具（场景内无 Chest 实例）",
                          "doorSource", "运行期夹具（场景内无 DungeonDoor 实例）",
                          "rescueSource", downed.name + " 场景实例",
                          "pass", implementsOk));

            // ---- 2. 契约取值：KeyInteractable / Priority ----
            bool keyOk = !chest.KeyInteractable && !downed.rescue.KeyInteractable && door.KeyInteractable;
            bool priorityOk = downed.rescue.Priority == InteractionPriority.Rescue &&
                              chest.Priority == InteractionPriority.Default &&
                              door.Priority == InteractionPriority.Default;
            allPass &= keyOk && priorityOk;
            rows.Add(QaJson.Dict("step", "contractValues",
                          "chestKeyInteractable", chest.KeyInteractable,
                          "rescueKeyInteractable", downed.rescue.KeyInteractable,
                          "doorKeyInteractable", door.KeyInteractable,
                          "chestPriority", chest.Priority, "rescuePriority", downed.rescue.Priority,
                          "doorPriority", door.Priority, "pass", keyOk && priorityOk));

            // ---- 3. 宝箱：开箱前可交互 → Interact → Opened 恰好一次 → 不可交互；再 Interact 不再触发 ----
            Chest.Opened += onOpened;
            bool availableBefore = chest.IsAvailable(unit.brain);
            chest.Interact(unit.brain);
            float waited = 0f;
            while (openedCount == 0 && waited < 5f) { yield return null; waited += Time.deltaTime; }
            int countAfterFirst = openedCount;
            bool availableAfter = chest.IsAvailable(unit.brain);
            chest.Interact(unit.brain);                           // 幂等：已开箱应无副作用
            float waited2 = 0f;
            while (waited2 < 0.5f) { yield return null; waited2 += Time.deltaTime; }
            bool idempotent = openedCount == countAfterFirst;
            bool chestOk = availableBefore && countAfterFirst == 1 && !availableAfter && idempotent;
            allPass &= chestOk;
            rows.Add(QaJson.Dict("step", "chestContract", "availableBefore", availableBefore,
                          "openedEventsAfterFirstInteract", countAfterFirst,
                          "availableAfter", availableAfter, "openedEventsAfterSecondInteract", openedCount,
                          "idempotent", idempotent, "waitSeconds", waited, "pass", chestOk));

            // ---- 4. 门：初始可交互，且本断言不触发 Enter ----
            bool doorAvailable = door.IsAvailable(unit.brain);
            allPass &= doorAvailable;
            rows.Add(QaJson.Dict("step", "doorAvailability", "isAvailable", doorAvailable,
                          "enteredByTest", false,
                          "note", "只读 IsAvailable，不调用 Interact，避免黑屏过场与全员传送干扰后续断言",
                          "pass", doorAvailable));

            // ---- 5. Runner.Ensure 复用：两次同一实例，且组件只有一个 ----
            var runner1 = InteractionRunner.Ensure(unit.brain);
            var runner2 = InteractionRunner.Ensure(unit.brain);
            int runnerCount = unit.brain.GetComponents<InteractionRunner>().Length;
            bool runnerOk = runner1 != null && runner1 == runner2 && runnerCount == 1;
            allPass &= runnerOk;
            rows.Add(QaJson.Dict("step", "runnerEnsure", "sameInstance", runner1 != null && runner1 == runner2,
                          "componentCount", runnerCount, "runtimeOnly", true, "pass", runnerOk));
        }
        finally
        {
            Chest.Opened -= onOpened;
            if (chestGo != null) Destroy(chestGo);
            if (doorGo != null) Destroy(doorGo);
            if (chestItem != null) Destroy(chestItem);
        }

        sink.Record(Id, Id,
            "Chest/DownedRescue/DungeonDoor 都实现 IInteractionTarget；KeyInteractable 只有门为 true、救援 Priority=Rescue；" +
            "宝箱开箱前 IsAvailable=true、Interact 后 Opened 恰好 1 次且 IsAvailable=false、重复 Interact 不再触发；" +
            "门初始 IsAvailable=true 且不被本断言触发；InteractionRunner.Ensure 两次返回同一实例且只挂 1 个组件",
            "allPass=" + allPass,
            allPass, allPass ? "pass" : "product_fail", "production",
            QaJson.Dict("steps", rows,
                 "fixture", "REFACTOR_ContractChest / REFACTOR_ContractDoor 为运行期内存对象，断言结束前销毁；不写入场景",
                 "note", "M3 新增第 11 条；M0–M2 的 10 条断言口径未改动"));

        yield break;
    }
    // ============================================================
    // 9. Refactor.RegistrySemantics
    // ============================================================
    /// <summary>注册表契约（M1）：默认排除未激活、includeInactive 能取回、Nearest 与替换前的全场扫描同口径。</summary>
    IEnumerator AssertRegistrySemantics(IQaSink sink, IReadOnlyList<QaFixtures.PlayerProbe> players)
    {
        const string Id = "Refactor.RegistrySemantics";
        var rows = new List<object>();
        bool allPass = true;

        // ---- 1. OfTeam(Player, false) == 3 ----
        int playerCount = UnitRegistry.OfTeam(Team.Player, false).Count;
        bool countOk = playerCount == 3;
        allPass &= countOk;
        rows.Add(QaJson.Dict("step", "OfTeam(Player,false)", "count", playerCount, "expected", 3, "pass", countOk));

        // ---- 2. 临时停用一个玩家：All(false) 不含 / All(true) 含；恢复后重新含入 ----
        UnitHealth probe = players.Count > 0 ? players[0].health : null;
        int activeBefore = UnitRegistry.All(false).Count;
        int anyBefore = UnitRegistry.All(true).Count;
        int registeredBefore = UnitRegistry.Count;
        bool hideOk = false, showOk = false, idOk = false;
        UnitId probeId = UnitId.None;

        if (probe != null && probe.gameObject.activeSelf)
        {
            idOk = UnitRegistry.TryGetId(probe, out probeId) && probeId.IsValid;
            probe.gameObject.SetActive(false);
            yield return null;   // 等 OnDisable 落地（引擎同帧内同步，这里多等一帧保证观测稳定）

            bool hiddenFromActive = !Contains(UnitRegistry.All(false), probe);
            bool presentInAny = Contains(UnitRegistry.All(true), probe);
            hideOk = hiddenFromActive && presentInAny
                     && UnitRegistry.All(false).Count == activeBefore - 1
                     && UnitRegistry.All(true).Count == anyBefore
                     && UnitRegistry.Count == registeredBefore;

            probe.gameObject.SetActive(true);
            yield return null;

            showOk = Contains(UnitRegistry.All(false), probe)
                     && UnitRegistry.All(false).Count == activeBefore
                     && UnitRegistry.Find(probeId) == probe;
        }
        bool toggleOk = hideOk && showOk;
        allPass &= toggleOk;
        rows.Add(QaJson.Dict("step", "SetActive(false/true)", "probe", probe != null ? probe.name : "null",
                      "activeBefore", activeBefore, "anyBefore", anyBefore, "registeredBefore", registeredBefore,
                      "hiddenFromAll(false)", hideOk, "backInAll(false)", showOk,
                      "unitId", probeId.IsValid ? (object)probeId.Value : "None", "idResolved", idOk,
                      "pass", toggleOk));

        // ---- 3. Nearest 与「遍历 All(false) + 3D 距离最近」一致 ----
        // 造一个运行期内存怪物夹具，保证候选集非空（其它怪物此刻被 SuppressMonsters 停用）
        Vector3 center = probe != null ? probe.transform.position : Vector3.zero;
        var probeMonster = CreateTarget("REFACTOR_Registry_Probe", center + new Vector3(2f, 0f, 0f));
        yield return null;

        const float range = 10f;
        UnitHealth viaRegistry = UnitRegistry.Nearest(center, range, Team.Monster);
        UnitHealth viaScan = null;
        int candidates = 0;
        float bestD = range;
        foreach (var h in UnitRegistry.All(false))
        {
            if (h == null || h.team != Team.Monster) continue;
            float d = Vector3.Distance(center, h.transform.position);
            if (d < bestD) { bestD = d; viaScan = h; }
            if (d < range) candidates++;
        }

        bool nearestOk = viaRegistry == viaScan;
        bool nonEmpty = candidates >= 1;
        bool foundProbe = viaRegistry != null && viaRegistry.gameObject == probeMonster;
        allPass &= nearestOk && nonEmpty;
        rows.Add(QaJson.Dict("step", "Nearest vs All(false) 3D 扫描", "range", range,
                      "registryResult", viaRegistry != null ? viaRegistry.name : "null",
                      "scanResult", viaScan != null ? viaScan.name : "null",
                      "candidatesInRange", candidates, "registryDistance", viaRegistry != null
                          ? (object)Vector3.Distance(center, viaRegistry.transform.position) : null,
                      "scanDistance", viaScan != null ? (object)Vector3.Distance(center, viaScan.transform.position) : null,
                      "nearestEqualsScan", nearestOk, "nonEmpty", nonEmpty, "pickedFixture", foundProbe,
                      "pass", nearestOk && nonEmpty));

        sink.Record(Id, Id,
            "UnitRegistry 契约：OfTeam(Player,false)=3；SetActive(false) 后 All(false) 不含而 All(true) 含、恢复后重新含入；" +
            "Nearest(center,10,Monster) 与遍历 All(false) + 3D 距离最近的结果一致",
            "players=" + playerCount + "; toggle=" + toggleOk + "; nearestEqualsScan=" + nearestOk +
            "; candidates=" + candidates,
            allPass, allPass ? "pass" : "product_fail", "production",
            QaJson.Dict("steps", rows, "count", UnitRegistry.Count,
                 "note", "只验证 M1 契约语义；不改动 M0 的 8 条断言口径"));
    }
    // ============================================================
    // 10. Refactor.StateFacade
    // ============================================================
    /// <summary>状态/流程门面（M2）：RunState/MetaState/MessageFeed/AppFlow/RunFlow 与旧 GameData/GameFlow/PauseManager
    /// 双向一致；Changed 恰好触发一次且退订后不再触发；Pause/Resume 与 timeScale 对齐。所有写入在结束前还原。</summary>
    IEnumerator AssertStateFacade(IQaSink sink)
    {
        const string Id = "Refactor.StateFacade";
        var rows = new List<object>();
        bool allPass = true;

        // 原值（结束前全部还原，避免污染收尾与后续观察）
        int goldBefore = RunState.Gold;
        int levelBefore = MetaState.Level;
        string messageBefore = MessageFeed.Message;
        bool gameStartedBefore = AppFlow.GameStarted;
        bool pausedBefore = AppFlow.IsPaused;

        bool goldOk = false, levelOk = false, messageEventOk = false;
        bool startedOk = false, wipedOk = false, pauseOk = false;
        bool restoredOk = false;
        int changed = 0;

        try
        {
            // ---- 1. 金币：新 → 旧；旧 → 新 ----
            RunState.Gold = 7;
            bool goldForward = GameData.gold == 7;
            GameData.gold = 3;
            bool goldBackward = RunState.Gold == 3;
            goldOk = goldForward && goldBackward;
            allPass &= goldOk;
            rows.Add(QaJson.Dict("step", "RunState.Gold <-> GameData.gold",
                          "newToOld", goldForward, "oldToNew", goldBackward, "pass", goldOk));

            // ---- 2. 等级 + 升级费用 ----
            MetaState.Level = 4;
            bool levelForward = GameData.level == 4;
            GameData.level = 6;
            bool levelBackward = MetaState.Level == 6;
            int metaCost = MetaState.LevelCost();
            int legacyCost = GameData.LevelCost();
            bool costOk = metaCost == 50 * MetaState.Level && metaCost == legacyCost;
            levelOk = levelForward && levelBackward && costOk;
            allPass &= levelOk;
            rows.Add(QaJson.Dict("step", "MetaState.Level <-> GameData.level; LevelCost",
                          "newToOld", levelForward, "oldToNew", levelBackward,
                          "level", MetaState.Level, "metaCost", metaCost, "legacyCost", legacyCost,
                          "costOk", costOk, "pass", levelOk));

            // ---- 3. 消息：旧读新写；订阅后恰好触发一次；退订后不再增加 ----
            System.Action onChanged = () => { changed++; };
            MessageFeed.Changed += onChanged;
            MessageFeed.SetMessage("REFACTOR_STATE_PROBE");
            bool messageOk = GameData.message == "REFACTOR_STATE_PROBE";
            int afterFirst = changed;
            MessageFeed.Changed -= onChanged;
            MessageFeed.SetMessage("REFACTOR_STATE_PROBE");
            int afterUnsubscribe = changed;
            messageEventOk = messageOk && afterFirst == 1 && afterUnsubscribe == afterFirst;
            allPass &= messageEventOk;
            rows.Add(QaJson.Dict("step", "MessageFeed.SetMessage -> GameData.message + Changed",
                          "messageOk", messageOk, "changedAfterFirst", afterFirst,
                          "changedAfterUnsubscribe", afterUnsubscribe, "pass", messageEventOk));

            // ---- 4. 开局标志：双向一致（写入后立刻还原，且全程同帧，不影响 RunFlow.Tick） ----
            AppFlow.GameStarted = true;
            bool startedForward = GameFlow.GameStarted;
            GameFlow.GameStarted = false;
            bool startedBackward = !AppFlow.GameStarted;
            AppFlow.GameStarted = gameStartedBefore;
            startedOk = startedForward && startedBackward && AppFlow.GameStarted == gameStartedBefore;
            allPass &= startedOk;
            rows.Add(QaJson.Dict("step", "AppFlow.GameStarted <-> GameFlow.GameStarted",
                          "newToOld", startedForward, "oldToNew", startedBackward,
                          "restoredTo", gameStartedBefore, "pass", startedOk));

            // ---- 5. 团灭标志：实例视图与静态视图一致 ----
            bool instanceExists = GameFlow.instance != null;
            wipedOk = instanceExists && RunFlow.IsWiped == GameFlow.instance.IsWiped;
            allPass &= wipedOk;
            rows.Add(QaJson.Dict("step", "RunFlow.IsWiped == GameFlow.instance.IsWiped",
                          "instanceExists", instanceExists, "runFlow", RunFlow.IsWiped,
                          "legacy", instanceExists ? (object)GameFlow.instance.IsWiped : null, "pass", wipedOk));

            // ---- 6. 暂停/恢复：IsPaused 与 timeScale 同步（同帧完成，结束后 timeScale==1） ----
            AppFlow.Pause();
            bool pauseEntered = PauseManager.instance != null && PauseManager.instance.IsPaused
                                && Time.timeScale == 0f;
            AppFlow.Resume();
            bool pauseLeft = PauseManager.instance != null && !PauseManager.instance.IsPaused
                             && Time.timeScale == 1f;
            pauseOk = pauseEntered && pauseLeft;
            allPass &= pauseOk;
            rows.Add(QaJson.Dict("step", "AppFlow.Pause/Resume -> PauseManager.IsPaused + Time.timeScale",
                          "onPause", pauseEntered, "onResume", pauseLeft, "timeScale", Time.timeScale,
                          "pass", pauseOk));
        }
        finally
        {
            RunState.Gold = goldBefore;
            MetaState.Level = levelBefore;
            MessageFeed.Message = messageBefore;
            AppFlow.GameStarted = gameStartedBefore;
            AppFlow.Resume();   // IsPaused=false; 结束时保证 Time.timeScale == 1

            restoredOk = RunState.Gold == goldBefore && MetaState.Level == levelBefore &&
                         MessageFeed.Message == messageBefore && AppFlow.GameStarted == gameStartedBefore &&
                         Time.timeScale == 1f;
            allPass &= restoredOk;
            rows.Add(QaJson.Dict("step", "restore", "gold", RunState.Gold, "level", MetaState.Level,
                          "message", MessageFeed.Message, "gameStarted", AppFlow.GameStarted,
                          "isPaused", AppFlow.IsPaused, "timeScale", Time.timeScale,
                          "pausedBefore", pausedBefore, "pass", restoredOk));
        }

        sink.Record(Id, Id,
            "状态/流程门面：RunState/MetaState/MessageFeed/AppFlow/RunFlow 与旧 GameData/GameFlow/PauseManager 双向一致；" +
            "Changed 订阅后恰好触发一次、退订后不再触发；Pause -> IsPaused=true 且 timeScale=0，Resume -> false 且 1（结束 timeScale=1）",
            "gold=" + goldOk + "; level=" + levelOk + "; messageEvent=" + messageEventOk +
            "; gameStarted=" + startedOk + "; wiped=" + wipedOk + "; pause=" + pauseOk +
            "; restored=" + restoredOk,
            allPass, allPass ? "pass" : "product_fail", "production",
            QaJson.Dict("steps", rows, "changedEvents", changed,
                 "note", "M2 新增第 10 条；M0/M1 的 9 条断言口径未改动；所有写入已在 finally 还原"));

        yield break;
    }
    // ============================================================
    // SKILL-CLOSE-01. Skill.MotionUnlocked（第 16 条）
    // ============================================================
    /// <summary>运动锁（UnitMotion）必须从三条路径复位：
    /// ① 技能正常收尾（Q → 离开技能状态回 Locomotion）；
    /// ② 技能被受击打断（W 后 0.1s 受 1 点伤害；另补 Q —— 只有 BashEffect 会加锁，W 不加，故 Q 才是「锁着被打断」）；
    /// ③ 倒地 → 复活。
    /// 每条都断言 agent.isStopped==false 且 UnitMotion.IsHeld==false；最后下一次移动指令证明真的能走。
    /// 复活走产品私有入口 DownedRescue.Revive（反射调用，与既有套件读私有字段同一惯例），不重跑 3s 读条。</summary>
    IEnumerator AssertMotionUnlocked(IQaSink sink, IReadOnlyList<QaFixtures.PlayerProbe> players)
    {
        const string Id = "Skill.MotionUnlocked";
        var knight = players.FirstOrDefault(p => p.name == "Knight_Unit");
        if (knight == null || knight.brain == null || knight.health == null ||
            knight.skills == null || knight.brain.agent == null || knight.rescue == null)
        {
            sink.BlockedAssertion(Id, Id, "技能结束/被打断/倒地复活后 agent.isStopped==false 且能移动",
                "缺少 Knight_Unit 或 SkillSystem/NavMeshAgent/DownedRescue", null);
            yield break;
        }
        if (knight.health.dead)
        {
            sink.BlockedAssertion(Id, Id, "技能结束/被打断/倒地复活后 agent.isStopped==false 且能移动",
                "骑士已倒地", null);
            yield break;
        }

        var agent = knight.brain.agent;
        MethodInfo reviveMethod = typeof(DownedRescue).GetMethod("Revive",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (reviveMethod == null)
        {
            sink.InfraFailure(Id, Id, "取不到 DownedRescue.Revive（产品私有入口被改名？）", "");
            yield break;
        }

        var rows = new List<object>();
        bool qCast = false, qLeftSkill = false, qUnlocked = false, qNotHeld = false, qLockObserved = false;
        float qSkillSeconds = -1f;
        bool wCast = false, wHitApplied = false, wHitEntered = false, wUnlocked = false;
        float wUnlockSeconds = -1f;
        bool qbCast = false, qbLockObserved = false, qbUnlocked = false;
        float qbUnlockSeconds = -1f;
        bool downed = false, revived = false, reviveUnlocked = false, reviveNotHeld = false;
        bool lockClearedOnDown = false;
        bool moveOk = false, moveNotHeld = false;
        float moveDistance = -1f, moveSeconds = -1f;

        try
        {
            // 前置复位：上一条（救援）把骑士留在 1 血，先用生产 API 回满，避免受击直接致死
            knight.brain.enabled = true;
            knight.brain.CancelPendingInteract();
            knight.brain.CommandAttack(null);
            var combat0 = knight.brain.combat as ICharacterCombat;
            if (combat0 != null) combat0.CancelAttack();
            knight.health.HealFull();
            yield return QaWait.WaitState(knight.animator, Locomotion, 6f);

            _target = CreateTarget("REFACTOR_Target_MotionUnlocked",
                                   knight.tx.position + knight.tx.forward * 1.8f);
            if (_target == null)
            {
                sink.InfraFailure(Id, Id, "运动锁靶夹具创建失败", "");
                yield break;
            }

            // ---- 1. Q 正常收尾：离开技能状态 → 锁必须解掉 ----
            RefillSkills(knight);
            qCast = knight.skills.Cast(0, _target.transform);
            {
                float t = 0f;
                bool entered = false;
                while (t < 6f)
                {
                    yield return null;
                    t += Time.deltaTime;
                    string s = QaFixtures.StateName(knight.animator);
                    if (IsSkillStateName(s))
                    {
                        entered = true;
                        if (UnitMotion.IsHeld(agent)) qLockObserved = true;
                    }
                    else if (entered && s == Locomotion)
                    {
                        qLeftSkill = true;
                        qSkillSeconds = t;
                        break;
                    }
                }
            }
            yield return null;
            yield return null;
            qUnlocked = !agent.isStopped;
            qNotHeld = !UnitMotion.IsHeld(agent);
            rows.Add(QaJson.Dict("step", "1-normalEnd", "castReturn", qCast, "leftSkill", qLeftSkill,
                          "skillSeconds", qSkillSeconds, "lockObservedDuringSkill", qLockObserved,
                          "agentIsStopped", agent.isStopped, "isHeld", UnitMotion.IsHeld(agent),
                          "pass", qCast && qLeftSkill && qUnlocked && qNotHeld));

            // ---- 2. W + 受击打断（工单口径；W 走 HeavySlashEffect，不加锁，验证「不残留锁」） ----
            RefillSkills(knight);
            wCast = knight.skills.Cast(1, null);
            float pre = 0f;
            while (pre < 0.1f) { yield return null; pre += Time.deltaTime; }
            float wHpBefore = knight.health.hp;
            knight.health.TakeHit(1f, 999f, DamageType.Blunt, knight.tx.position + Vector3.up, null);
            wHitApplied = Mathf.Abs(wHpBefore - knight.health.hp - 1f) < 0.01f;
            {
                float h = 0f;
                while (h < 0.5f)
                {
                    yield return null;
                    h += Time.deltaTime;
                    if (QaFixtures.StateName(knight.animator) == HitA) { wHitEntered = true; break; }
                }
                float u = 0f;
                while (u < 2f)
                {
                    if (!agent.isStopped && !UnitMotion.IsHeld(agent)) { wUnlocked = true; wUnlockSeconds = u; break; }
                    yield return null;
                    u += Time.deltaTime;
                }
            }
            rows.Add(QaJson.Dict("step", "2a-w-then-hit", "castReturn", wCast, "hitApplied", wHitApplied,
                          "enteredHit_A", wHitEntered, "unlockedWithin2s", wUnlocked,
                          "unlockSeconds", wUnlockSeconds,
                          "agentIsStopped", agent.isStopped, "isHeld", UnitMotion.IsHeld(agent),
                          "pass", wCast && wHitApplied && wUnlocked));

            // ---- 2b. Q（唯一加锁的技能）+ 受击打断：锁着的时候被打断也必须解锁 ----
            yield return QaWait.WaitState(knight.animator, Locomotion, 6f);
            RefillSkills(knight);
            qbCast = knight.skills.Cast(0, _target.transform);
            {
                float l = 0f;
                while (l < 0.4f)
                {
                    if (UnitMotion.IsHeld(agent)) { qbLockObserved = true; break; }
                    yield return null;
                    l += Time.deltaTime;
                }
                knight.health.TakeHit(1f, 999f, DamageType.Blunt, knight.tx.position + Vector3.up, null);
                float u = 0f;
                while (u < 2f)
                {
                    if (!agent.isStopped && !UnitMotion.IsHeld(agent)) { qbUnlocked = true; qbUnlockSeconds = u; break; }
                    yield return null;
                    u += Time.deltaTime;
                }
            }
            rows.Add(QaJson.Dict("step", "2b-q-then-hit", "castReturn", qbCast, "lockObserved", qbLockObserved,
                          "unlockedWithin2s", qbUnlocked, "unlockSeconds", qbUnlockSeconds,
                          "agentIsStopped", agent.isStopped, "isHeld", UnitMotion.IsHeld(agent),
                          "pass", qbCast && qbLockObserved && qbUnlocked));

            // ---- 3. 倒地 → 复活：倒地清锁，复活后必须可动 ----
            yield return QaWait.WaitState(knight.animator, Locomotion, 6f);
            knight.health.ApplyDamage(knight.health.maxHp * 4f, knight.tx.position + Vector3.up);
            {
                float d = 0f;
                while (d < 3f)
                {
                    yield return null;
                    d += Time.deltaTime;
                    if (knight.rescue.IsDown) { downed = true; break; }
                }
                lockClearedOnDown = downed && !UnitMotion.IsHeld(agent);
                if (downed) reviveMethod.Invoke(knight.rescue, null);
                float r = 0f;
                while (r < 5f)
                {
                    yield return null;
                    r += Time.deltaTime;
                    if (!knight.rescue.IsDown && agent.enabled && agent.isOnNavMesh) { revived = true; break; }
                }
                yield return null;
                reviveUnlocked = revived && !agent.isStopped;
                reviveNotHeld = !UnitMotion.IsHeld(agent);
            }
            rows.Add(QaJson.Dict("step", "3-down-then-revive", "downed", downed, "lockClearedOnDown", lockClearedOnDown,
                          "revived", revived, "agentIsStopped", agent.isStopped,
                          "isHeld", UnitMotion.IsHeld(agent),
                          "pass", downed && lockClearedOnDown && revived && reviveUnlocked && reviveNotHeld));

            // ---- 4. 真的能走：下一次移动指令必须产生位移 ----
            {
                float b = 0f;
                while (b < 5f && (knight.brain == null || !knight.brain.enabled))
                { yield return null; b += Time.deltaTime; }

                Vector3 from = knight.tx.position;
                Vector3 dest = from + knight.tx.forward * 3f;
                NavMeshHit navHit;
                if (NavMesh.SamplePosition(dest, out navHit, 3f, NavMesh.AllAreas)) dest = navHit.position;
                knight.brain.CommandMove(dest);

                float mt = 0f;
                float best = 0f;
                while (mt < 4f)
                {
                    yield return null;
                    mt += Time.deltaTime;
                    float dd = Vector3.Distance(from, knight.tx.position);
                    if (dd > best) best = dd;
                    if (dd >= 0.5f) break;
                }
                moveDistance = best;
                moveSeconds = mt;
                moveOk = best > 0.1f;
                moveNotHeld = !UnitMotion.IsHeld(agent);
            }
            rows.Add(QaJson.Dict("step", "4-canMove", "movedMeters", moveDistance, "seconds", moveSeconds,
                          "isHeld", UnitMotion.IsHeld(agent),
                          "pass", moveOk && moveNotHeld));
        }
        finally
        {
            knight.brain.CommandAttack(null);
            var combat = knight.brain.combat as ICharacterCombat;
            if (combat != null) combat.CancelAttack();
            knight.brain.CancelPendingInteract();
            RestoreAgent(knight);
            knight.health.HealFull();      // 夹具复位：复活后只有 reviveHp(1)，给后续/重跑留干净状态
            DestroyTarget();
        }

        bool pass = qCast && qLeftSkill && qUnlocked && qNotHeld &&
                    wCast && wHitApplied && wUnlocked &&
                    qbCast && qbLockObserved && qbUnlocked &&
                    downed && lockClearedOnDown && revived && reviveUnlocked && reviveNotHeld &&
                    moveOk && moveNotHeld;
        sink.Record(Id, Id,
            "运动锁复位：① Q 正常收尾（离开技能状态回 Locomotion）② W/Q 施放后受击打断 ③ 倒地 → 复活 —— " +
            "三条路径后 agent.isStopped==false 且 UnitMotion.IsHeld==false；最后下一次移动指令位移 > 0.1m",
            "qCast=" + qCast + "; qLeftSkill=" + qLeftSkill + "(" + F(qSkillSeconds) + "s)" +
            "; qUnlocked=" + qUnlocked + "; qNotHeld=" + qNotHeld + "; qLockSeen=" + qLockObserved +
            "; wCast=" + wCast + "; wHit=" + wHitApplied + "; wUnlocked=" + wUnlocked +
            "; qbLockSeen=" + qbLockObserved + "; qbUnlocked=" + qbUnlocked +
            "; downed=" + downed + "; revived=" + revived + "; reviveUnlocked=" + reviveUnlocked +
            "; moveMeters=" + F(moveDistance),
            pass, pass ? "pass" : "product_fail", "production",
            QaJson.Dict("steps", rows,
                 "entry", "SkillSystem.Cast(0/1) → BashEffect/HeavySlashEffect → UnitMotion.Begin/End；" +
                          "UnitHealth.TakeHit/ApplyDamage → DownedRescue.OnDowned；DownedRescue.Revive（反射私有入口）",
                 "note", "W 走 HeavySlashEffect 不加运动锁，故 2a 验证「不残留锁」，2b 用唯一加锁的 Q 验证「锁着被打断也能解锁」；" +
                         "复活不重跑 3s 读条以免拉长套件（本套件既有惯例是反射读产品私有成员）"));

        yield break;
    }
    static bool Contains(IReadOnlyList<UnitHealth> list, UnitHealth unit)
    {
        if (list == null || unit == null) return false;
        for (int i = 0; i < list.Count; i++)
            if (list[i] == unit) return true;
        return false;
    }

    /// <summary>技能状态名判定（与本套件 StateName 的映射一致）。</summary>
    static bool IsSkillStateName(string s)
    {
        return s == "Q_1" || s == "W_1" || s == "E_1" || s == "R_1";
    }

    // ============================================================
    // 夹具与收尾
    // ============================================================
    void SuppressMonsters()
    {
        _monstersSuppressed = true;
        foreach (var h in FindObjectsByType<UnitHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (h == null || h.team != Team.Monster) continue;
            var brain = h.GetComponent<MonsterBrain>();
            if (brain == null) continue;
            _suppressedMonsters.Add(h.gameObject);
            h.gameObject.SetActive(false);
        }
        Debug.Log("[REFACTOR] monsters suppressed=" + _suppressedMonsters.Count);
    }

    void RestoreMonstersAndUi()
    {
        if (_monstersSuppressed)
        {
            foreach (var go in _suppressedMonsters)
                if (go != null) go.SetActive(true);
            _monstersSuppressed = false;
        }
        if (UIManager.instance != null && UIManager.instance.IsInventoryOpen)
            UIManager.instance.HideInventory();
    }

    void DestroyTarget()
    {
        if (_target != null) { Destroy(_target); _target = null; }
        if (_targetStats != null) { Destroy(_targetStats); _targetStats = null; }
    }

    void CleanupFixtures()
    {
        DestroyTarget();
        if (_chestGo != null) { Destroy(_chestGo); _chestGo = null; }
        if (_chestItem != null) { Destroy(_chestItem); _chestItem = null; }
        foreach (var p in _players)
            if (p.brain != null) p.brain.enabled = true;
    }

    void Finish()
    {
        if (_finished) return;
        _finished = true;

        RestoreMonstersAndUi();
        CleanupFixtures();

        if (_ev != null)
        {
            _ev.WriteSummary(ScenePath, _seed,
                string.IsNullOrEmpty(_exitReason) ? "completed" : _exitReason,
                QaJson.Dict("head", QaScene.GetHead("REFACTOR.Head"),
                     "fixtureNote", "REFACTOR_Target_* / REFACTOR_Chest 为运行期内存对象，结束前销毁；不写入场景"));
        }

        int code;
        if (_ev == null || _ev.Infra > 0) code = 3;
        else if (_ev.Fail > 0 || _ev.Blocked > 0) code = 2;
        else code = 0;

        Debug.Log("[REFACTOR] finish; run=" + _run + "; exit=" + code +
                  "; pass=" + (_ev != null ? _ev.Pass : -1) +
                  "; fail=" + (_ev != null ? _ev.Fail : -1) +
                  "; blocked=" + (_ev != null ? _ev.Blocked : -1) +
                  "; infra=" + (_ev != null ? _ev.Infra : -1));

#if UNITY_EDITOR
        SessionState.SetBool(FinishedKey, true);
        SessionState.SetInt(ExitCodeKey, code);
        try
        {
            if (_ev != null)
                System.IO.File.WriteAllText(_ev.ExitCodePath(), code.ToString() + Environment.NewLine,
                    new System.Text.UTF8Encoding(false));
        }
        catch (Exception e) { Debug.LogWarning("[REFACTOR] exit code file write failed: " + e.Message); }

        Destroy(gameObject);
        Time.timeScale = 1f;
        EditorApplication.isPlaying = false;
#else
        Time.timeScale = 1f;
#endif
    }

    // ============================================================
    // 工具
    // ============================================================
    void FindPlayers()
    {
        _players.Clear();
        _players.AddRange(QaFixtures.FindPlayers());
        foreach (var p in _players)
            Debug.Log("[REFACTOR] player " + p.name + " at " + V3Text(p.home) +
                      "; sword=" + (p.sword != null) + "; ranged=" + (p.ranged != null) +
                      "; skills=" + (p.skills != null) + "; rescue=" + (p.rescue != null));
    }

    /// <summary>Fixture: QaFixtures.CreateTarget(showDamageNumber=false) plus the local destroy bookkeeping.</summary>
    GameObject CreateTarget(string name, Vector3 pos)
    {
        DestroyTarget();
        _target = QaFixtures.CreateTarget(name, pos, false);
        var uh = _target.GetComponent<UnitHealth>();
        _targetStats = uh != null ? uh.stats : null;
        return _target;
    }



    static bool GetFired(RangedCombat r)
    {
        if (r == null) return false;
        if (_firedField == null)
            _firedField = typeof(RangedCombat).GetField("_fired", BindingFlags.Instance | BindingFlags.NonPublic);
        return _firedField != null && (bool)_firedField.GetValue(r);
    }

    /// <summary>只读观测：MeleeCombat 的攻击计时（-1 = 当前没有进行中的攻击）。</summary>
    static float GetAttackTimer(MeleeCombat m)
    {
        if (m == null) return -2f;
        if (_attackTimerField == null)
            _attackTimerField = typeof(MeleeCombat).GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic);
        if (_attackTimerField == null) return -2f;
        object v = _attackTimerField.GetValue(m);
        return v is float f ? f : -2f;
    }

    /// <summary>夹具复位：蓝量与冷却走生产调试入口，保证每条断言独立（不依赖上一条的剩余蓝量/冷却）。</summary>
    static void RefillSkills(QaFixtures.PlayerProbe p)
    {
        if (p == null || p.skills == null) return;
        if (p.skills.mana != null) p.skills.mana.Refill();
        p.skills.ResetCooldowns();
    }

    /// <summary>测试收尾：BashEffect 会把 NavMeshAgent.isStopped 置 true（既有产品行为，M4 未改）；
    /// 这里恢复可动，避免影响后续断言对同一单位的移动观测。</summary>
    static void RestoreAgent(QaFixtures.PlayerProbe p)
    {
        if (p == null || p.brain == null || p.brain.agent == null) return;
        if (p.brain.agent.isOnNavMesh) p.brain.agent.isStopped = false;
    }

    /// <summary>反射读取 protected ComputeEnd()（落点只由该虚方法决定）。</summary>
    static Vector3 ComputeEndOf(Projectile p)
    {
        var m = typeof(Projectile).GetMethod("ComputeEnd", BindingFlags.Instance | BindingFlags.NonPublic);
        if (m == null || p == null) return new Vector3(float.NaN, float.NaN, float.NaN);
        object v = m.Invoke(p, null);
        return v is Vector3 end ? end : new Vector3(float.NaN, float.NaN, float.NaN);
    }




    static string F(float v)
    {
        return v.ToString("0.###", CultureInfo.InvariantCulture);
    }

    static string V3Text(Vector3 v)
    {
        return "(" + F(v.x) + ", " + F(v.y) + ", " + F(v.z) + ")";
    }


}
