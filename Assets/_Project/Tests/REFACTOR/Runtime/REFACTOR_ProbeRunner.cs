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
#endif

/// <summary>
/// REFACTOR 护栏套件（Play 模式）。M0 建立，之后每个重构里程碑都跑它做“行为不变”兜底。
/// 只读生产状态；测试自身唯一写入面为 Docs 证据、SessionState 退出码、
/// 以及运行期内存夹具（结束前销毁）。
///
/// 9 条断言（每条都走生产入口，不复制 SPEC01 的数值口径）：
///   1. Party.PresentAndMove       三职业存在 + 生产移动命令可移动
///   2. Combat.MeleeHit            骑士近战普攻命中，命中瞬间处于 AttackMelee
///   3. Combat.RangedShot          弓/法远程普攻发射，发射瞬间处于 Attack_Release
///   4. Skill.QW                   Q/W 经 SkillSystem.Cast 生效（类型/蓝耗/动画）
///   5. Hit.InterruptAttack        普攻中受击 → 进入 Hit_A（打断）
///   6. Downed.Knockdown           致死 → Downed，AI/导航停用
///   7. Rescue.ChannelInterrupt    救援读条 → 受击打断 → 重新下令 → 救起
///   8. Chest.Interact             点击宝箱指令 → 走到范围 → 开盖 + 内容物现身
///   9. Refactor.RegistrySemantics 单位注册表契约（未激活语义 / Nearest 与全场扫描同口径）
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

    int _run;
    int _seed;
    float _deadline;

    readonly List<PlayerProbe> _players = new List<PlayerProbe>();
    readonly List<GameObject> _suppressedMonsters = new List<GameObject>();
    GameObject _target;
    UnitStats _targetStats;
    GameObject _chestGo;
    ItemData _chestItem;
    bool _monstersSuppressed;

    class PlayerProbe
    {
        public string name;
        public UnitHealth health;
        public CharacterBrain brain;
        public Animator animator;
        public MeleeCombat sword;
        public RangedCombat ranged;
        public SkillSystem skills;
        public DownedRescue rescue;
        public Vector3 home;
        public float attackSpeed = 1f;

        public Transform tx { get { return health != null ? health.transform : null; } }
    }

    class HitCollector
    {
        public UnitHealth Target;
        public int FilterSource;
        public int Count;
        public readonly List<Dictionary<string, object>> Events = new List<Dictionary<string, object>>();

        public void Subscribe(UnitHealth target, int filterSource)
        {
            Target = target;
            FilterSource = filterSource;
            if (Target != null) Target.OnHit += OnHit;
        }

        public void Unsubscribe()
        {
            if (Target != null) Target.OnHit -= OnHit;
        }

        void OnHit(DamageInfo info)
        {
            if (FilterSource != 0 && (info.source == null || info.source.GetInstanceID() != FilterSource)) return;
            Count++;
            string state = "none";
            float nt = -1f;
            int sourceId = 0;
            if (info.source != null)
            {
                sourceId = info.source.GetInstanceID();
                Animator a = info.source.GetComponentInChildren<Animator>();
                state = StateName(a);
                nt = StateNorm(a);
            }
            if (Events.Count < 200)
                Events.Add(Dict("time", Time.time, "sourceId", sourceId, "state", state,
                                "normalizedTime", nt, "amount", info.finalAmount));
        }
    }

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
                  "; evidenceDir=" + _ev.EvidenceDir + "; head=" + GetHead());

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
                "Hit.InterruptAttack", "Downed.Knockdown", "Rescue.ChannelInterrupt", "Chest.Interact",
                "Refactor.RegistrySemantics"
            };
            foreach (string id in ids)
                _ev.BlockedAssertion(id, id, "三角色齐备才能跑的护栏断言",
                    "找到玩家数=" + _players.Count, Dict("players", _players.Select(p => p.name).ToList()));
            Finish();
            yield break;
        }

        yield return RunSafe("Party.PresentAndMove", AssertPartyMove());
        SuppressMonsters();
        yield return RunSafe("Combat.MeleeHit", AssertMeleeHit());
        yield return RunSafe("Combat.RangedShot", AssertRangedShot());
        yield return RunSafe("Skill.QW", AssertSkillQW());
        yield return RunSafe("Hit.InterruptAttack", AssertHitInterrupt());
        yield return RunSafe("Downed.Knockdown", AssertKnockdown());
        yield return RunSafe("Rescue.ChannelInterrupt", AssertRescue());
        yield return RunSafe("Chest.Interact", AssertChest());
        yield return RunSafe("Refactor.RegistrySemantics", AssertRegistrySemantics());
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
    IEnumerator AssertPartyMove()
    {
        const string Id = "Party.PresentAndMove";
        var rows = new List<object>();
        bool allPass = true;

        foreach (var p in _players)
        {
            if (p.brain == null || p.brain.agent == null || p.animator == null)
            {
                allPass = false;
                rows.Add(Dict("name", p.name, "error", "missing brain/agent/animator"));
                continue;
            }

            Vector3 dest = p.home + new Vector3(0f, 0f, 3f);
            NavMeshHit hit;
            if (!NavMesh.SamplePosition(dest, out hit, 2.0f, NavMesh.AllAreas)) dest = p.home + new Vector3(1.5f, 0f, 1.5f);
            else dest = hit.position;

            Select(p);
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
                if (speed > 0.5f && stateDuringMove == "none") stateDuringMove = StateName(p.animator);
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

            rows.Add(Dict("name", p.name, "destination", V3(dest), "arrived", arrived,
                          "durationSeconds", elapsed, "traveled", traveled,
                          "peakAgentSpeed", peakSpeed, "peakAnimatorSpeedZ", peakSpeedZ,
                          "stateDuringMove", stateDuringMove,
                          "finalAgentSpeed", finalSpeed, "finalAnimatorSpeedZ", finalSpeedZ,
                          "groundedDuringMove", grounded, "pass", pass));

            // 回出生点，避免影响后续断言
            Select(p);
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

        _ev.Record(Id, Id,
            "Knight/Archer/Mage 三个生产玩家单位均存在，且经 SelectionManager.ForEachSelectedBrain → CommandMove 能移动到目标点并在 Locomotion 停稳",
            "players=" + _players.Count + "; allPass=" + allPass,
            allPass, allPass ? "pass" : "product_fail", "production",
            Dict("players", rows, "count", _players.Count,
                 "note", "移动入口与右键移动同一生产方法；SpeedZ 只要求非零变化，不复制 SPEC01 阈值口径"));
    }

    // ============================================================
    // 2. Combat.MeleeHit
    // ============================================================
    IEnumerator AssertMeleeHit()
    {
        const string Id = "Combat.MeleeHit";
        var knight = _players.FirstOrDefault(p => p.name == "Knight_Unit");
        if (knight == null || knight.brain == null || knight.health == null)
        {
            _ev.BlockedAssertion(Id, Id, "骑士近战普攻可命中", "未找到 Knight_Unit", null);
            yield break;
        }

        _target = CreateTarget("REFACTOR_Target_Melee", knight.tx.position + knight.tx.forward * 1.8f);
        if (_target == null)
        {
            _ev.InfraFailure(Id, Id, "近战靶夹具创建失败", "");
            yield break;
        }
        var targetHealth = _target.GetComponent<UnitHealth>();

        var collector = new HitCollector();
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
        yield return WaitState(knight, Locomotion, 5f);

        _ev.Record(Id, Id,
            "骑士经 CommandAttack 对近战范围内目标出手一次：目标掉血，且命中瞬间于 layer0 AttackMelee",
            "hits=" + collector.Count + "; state=" + hitState + "; amount=" + F(amount) +
            "; wait=" + F(t) + "s",
            pass, pass ? "pass" : "product_fail", "production",
            Dict("hits", collector.Count, "hitState", hitState, "hitNormalizedTime", hitNorm,
                 "amount", amount, "waitSeconds", t,
                 "fixture", "REFACTOR_Target_Melee（运行期内存 UnitHealth，无 AI）",
                 "entry", "CharacterBrain.CommandAttack → UnitBrain.Update → ICharacterCombat.AutoAttack → MeleeCombat.SingleTargetHit"));
    }

    // ============================================================
    // 3. Combat.RangedShot
    // ============================================================
    IEnumerator AssertRangedShot()
    {
        const string Id = "Combat.RangedShot";
        var rows = new List<object>();
        bool allPass = true;
        int tested = 0;

        foreach (var p in _players.Where(x => x.ranged != null))
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
                    fireState = StateName(p.animator);
                    fireNorm = StateNorm(p.animator);
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
            yield return WaitState(p, Locomotion, 5f);

            bool pass = fired && fireState == AttackRelease;
            allPass &= pass;
            rows.Add(Dict("player", p.name, "fired", fired, "fireState", fireState,
                          "fireNormalizedTime", fireNorm, "waitSeconds", t,
                          "damageObserved", damage, "attackSpeed", p.attackSpeed, "pass", pass));
            DestroyTarget();
        }

        bool anyRanged = tested > 0;
        bool passOverall = anyRanged && allPass;
        _ev.Record(Id, Id,
            "弓手/法师经 CommandAttack 出手：至少发射一次，且发射瞬间于 layer0 Attack_Release",
            "tested=" + tested + "; allPass=" + allPass + "; rows=" + rows.Count,
            passOverall, passOverall ? "pass" : "product_fail", "production",
            Dict("players", rows,
                 "note", "伤害只记录不判定：法师火球是否结算取决于预制体 explosionEffect 配置；发射本身是远程普攻的核心契约"));
    }

    // ============================================================
    // 4. Skill.QW
    // ============================================================
    IEnumerator AssertSkillQW()
    {
        const string Id = "Skill.QW";
        Vector3 FixturePos = new Vector3(80f, 0f, 80f);

        SkillCastFixture fx = null;
        try { fx = SkillCastFixture.Create(FixturePos, true); }
        catch (Exception e)
        {
            _ev.InfraFailure(Id, Id, "夹具创建抛异常: " + e.Message, e.StackTrace);
            yield break;
        }
        if (fx == null || !string.IsNullOrEmpty(fx.Error))
        {
            string err = fx != null ? fx.Error : "null";
            if (fx != null) fx.Dispose();
            _ev.InfraFailure(Id, Id, "夹具创建失败: " + err, "");
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
                _ev.BlockedAssertion(Id, Id, "Q/W 可施放",
                    "夹具未能进入 Locomotion（preState=" + prepareState + "）",
                    Dict("prepareState", prepareState));
                yield break;
            }

            yield return fx.Cast(0, fx.Target != null ? fx.Target.transform : null, "Bash", 0.5f, q);
            qPass = q.castReturn && q.effectType == "BashEffect" &&
                    Mathf.Abs(q.manaDelta - 20f) < 0.01f &&
                    q.damageObserved && q.animEntered && q.animEntrySeconds >= 0f && q.animEntrySeconds <= 0.6f;

            yield return fx.EnsureLocomotion(3f);
            yield return fx.Cast(1, null, "Skill", 0.5f, w);
            wPass = w.castReturn && w.effectType == "HeavySlashEffect" &&
                    Mathf.Abs(w.manaDelta - 30f) < 0.01f &&
                    w.swordTimer >= 0f && w.animEntered && w.animEntrySeconds >= 0f && w.animEntrySeconds <= 0.6f;
        }
        finally
        {
            if (fx != null) fx.Dispose();
        }

        bool pass = qPass && wPass;
        _ev.Record(Id, Id,
            "Q（BashEffect，-20 蓝，命中掉血，≤0.6s 进入 Bash）与 W（HeavySlashEffect，-30 蓝，起手技能斩，≤0.6s 进入 Skill）均经 SkillSystem.Cast 生效",
            "qPass=" + qPass + "; wPass=" + wPass +
            "; qType=" + q.effectType + "; wType=" + w.effectType +
            "; qManaDelta=" + F(q.manaDelta) + "; wManaDelta=" + F(w.manaDelta),
            pass, pass ? "pass" : "product_fail", "production",
            Dict("Q", q.Data(), "W", w.Data(),
                 "prepareState", prepareState, "prepareSeconds", prepareSeconds,
                 "fixture", SkillCastFixture.KnightPrefabPath,
                 "note", "只验 Q/W 两个有内容的槽；E/R 空槽契约由 SPEC01 覆盖，本套件不重复"));
    }

    // ============================================================
    // 5. Hit.InterruptAttack
    // ============================================================
    IEnumerator AssertHitInterrupt()
    {
        const string Id = "Hit.InterruptAttack";
        var knight = _players.FirstOrDefault(p => p.name == "Knight_Unit");
        if (knight == null || knight.brain == null || knight.health == null)
        {
            _ev.BlockedAssertion(Id, Id, "普攻中受击被 Hit_A 打断", "未找到 Knight_Unit", null);
            yield break;
        }
        if (knight.health.dead)
        {
            _ev.BlockedAssertion(Id, Id, "普攻中受击被 Hit_A 打断", "骑士已倒地", null);
            yield break;
        }

        _target = CreateTarget("REFACTOR_Target_HitInt", knight.tx.position + knight.tx.forward * 1.8f);
        if (_target == null)
        {
            _ev.InfraFailure(Id, Id, "受击打断靶夹具创建失败", "");
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
            if (StateName(knight.animator) == AttackMelee) { attacking = true; break; }
        }

        string observed = StateName(knight.animator);
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
                observed = StateName(knight.animator);
                if (observed == HitA) { hitWait = h; break; }
            }
        }
        float hpAfter = knight.health.hp;

        knight.brain.CommandAttack(null);
        var combat = knight.brain.combat as ICharacterCombat;
        if (combat != null) combat.CancelAttack();
        yield return WaitState(knight, Locomotion, 6f);
        DestroyTarget();

        bool pass = attacking && hitWait >= 0f && observed == HitA &&
                    Mathf.Abs(hpBefore - hpAfter - 1f) < 0.01f;
        _ev.Record(Id, Id,
            "骑士在 AttackMelee 出招中受 1 点伤害：≤1.5s 内进入 Hit_A，且伤害只结算 1 点",
            "attacking=" + attacking + "; observed=" + observed +
            "; hitWait=" + F(hitWait) + "s; hp=" + F(hpBefore) + "→" + F(hpAfter),
            pass, pass ? "pass" : "product_fail", "production",
            Dict("attackingBeforeHit", attacking, "observedState", observed, "hitWaitSeconds", hitWait,
                 "hpBefore", hpBefore, "hpAfter", hpAfter,
                 "entry", "UnitHealth.ApplyDamage(1f) → CharacterBrain.OnDamaged → Animator.SetTrigger(\"Hit\") → AnyState→Hit_A"));
    }

    // ============================================================
    // 6. Downed.Knockdown
    // ============================================================
    IEnumerator AssertKnockdown()
    {
        const string Id = "Downed.Knockdown";
        var knight = _players.FirstOrDefault(p => p.name == "Knight_Unit");
        if (knight == null || knight.health == null || knight.rescue == null)
        {
            _ev.BlockedAssertion(Id, Id, "致死进入 Downed", "骑士缺少 UnitHealth/DownedRescue", null);
            yield break;
        }
        if (knight.health.dead)
        {
            _ev.BlockedAssertion(Id, Id, "致死进入 Downed", "骑士已倒地", null);
            yield break;
        }

        knight.health.ApplyDamage(knight.health.maxHp * 4f, knight.tx.position + Vector3.up);

        float t = 0f;
        string observed = StateName(knight.animator);
        bool downedAnim = false;
        while (t < 3f)
        {
            yield return null;
            t += Time.deltaTime;
            observed = StateName(knight.animator);
            if (observed == Downed) { downedAnim = true; break; }
        }

        bool isDown = knight.rescue.IsDown;
        bool brainOff = knight.brain != null && !knight.brain.enabled;
        bool agentOff = knight.brain != null && knight.brain.agent != null && !knight.brain.agent.enabled;

        bool pass = isDown && downedAnim && brainOff && agentOff;
        _ev.Record(Id, Id,
            "骑士接受致死伤害后：DownedRescue.IsDown=true，layer0 进入 Downed，CharacterBrain 与 NavMeshAgent 停用",
            "isDown=" + isDown + "; anim=" + observed + "; wait=" + F(t) + "s" +
            "; brainEnabled=" + (knight.brain != null && knight.brain.enabled) +
            "; agentEnabled=" + (knight.brain != null && knight.brain.agent != null && knight.brain.agent.enabled),
            pass, pass ? "pass" : "product_fail", "production",
            Dict("isDown", isDown, "observedState", observed, "waitSeconds", t,
                 "brainEnabled", knight.brain != null && knight.brain.enabled,
                 "agentEnabled", knight.brain != null && knight.brain.agent != null && knight.brain.agent.enabled,
                 "entry", "UnitHealth.ApplyDamage(致死) → OnDeath → DownedRescue.OnDowned"));

        // 保持倒地状态给下一条救援断言用
        yield return null;
    }

    // ============================================================
    // 7. Rescue.ChannelInterrupt
    // ============================================================
    IEnumerator AssertRescue()
    {
        const string Id = "Rescue.ChannelInterrupt";
        var downed = _players.FirstOrDefault(p => p.name == "Knight_Unit");
        var rescuer = _players.FirstOrDefault(p => p.name == "Archer_Unit" && !p.health.dead && p.health != (downed != null ? downed.health : null));
        if (downed == null || rescuer == null || downed.rescue == null)
        {
            _ev.BlockedAssertion(Id, Id, "救援指令 → 读条 → 受击打断 → 重新下令 → 救起",
                "缺少倒地者或可用救援者", null);
            yield break;
        }
        if (!downed.rescue.IsDown)
        {
            _ev.BlockedAssertion(Id, Id, "救援指令 → 读条 → 受击打断 → 重新下令 → 救起",
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
        string stateWhileChanneling = StateName(rescuer.animator);

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

        _ev.Record(Id, Id,
            "救援指令让救援者走到范围并读条；读条中救援者受击 → 读条中断且目标仍倒地；重新下令后读完条救起（血量=reviveHp）并恢复大脑",
            "channeling=" + channeling + "; arrive=" + F(arriveSeconds) + "s; interrupted=" + interrupted +
            "; stillDown=" + stillDownAfterInterrupt + "; revived=" + revived +
            "; revive=" + F(reviveSeconds) + "s; hp=" + F(reviveHp) + "; brainBack=" + brainBack,
            pass, pass ? "pass" : "product_fail", "production",
            Dict("channeling", channeling, "arriveSeconds", arriveSeconds,
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
    IEnumerator AssertChest()
    {
        const string Id = "Chest.Interact";
        var mage = _players.FirstOrDefault(p => p.name == "Mage_Unit");
        if (mage == null || mage.brain == null || mage.brain.agent == null)
        {
            _ev.BlockedAssertion(Id, Id, "走到宝箱范围后开盖", "未找到 Mage_Unit", null);
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
        Select(mage);
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
        _ev.Record(Id, Id,
            "经生产交互入口（CharacterBrain.CommandInteract）让法师走到宝箱范围：宝箱进入已开启，盖子旋转 >30°，内容物显形且已写入宝箱背包",
            "opened=" + opened + "; wait=" + F(t) + "s; lidAngle=" + F(lidAngle) +
            "; itemVisible=" + itemVisible + "; inventoryBound=" + inventoryBound,
            pass, pass ? "pass" : "product_fail", "production",
            Dict("opened", opened, "waitSeconds", t, "lidAngle", lidAngle,
                 "itemVisible", itemVisible, "inventoryBound", inventoryBound,
                 "chestPosition", V3(chestPos), "caster", mage.name,
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
    // 9. Refactor.RegistrySemantics
    // ============================================================
    /// <summary>注册表契约（M1）：默认排除未激活、includeInactive 能取回、Nearest 与替换前的全场扫描同口径。</summary>
    IEnumerator AssertRegistrySemantics()
    {
        const string Id = "Refactor.RegistrySemantics";
        var rows = new List<object>();
        bool allPass = true;

        // ---- 1. OfTeam(Player, false) == 3 ----
        int playerCount = UnitRegistry.OfTeam(Team.Player, false).Count;
        bool countOk = playerCount == 3;
        allPass &= countOk;
        rows.Add(Dict("step", "OfTeam(Player,false)", "count", playerCount, "expected", 3, "pass", countOk));

        // ---- 2. 临时停用一个玩家：All(false) 不含 / All(true) 含；恢复后重新含入 ----
        UnitHealth probe = _players.Count > 0 ? _players[0].health : null;
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
        rows.Add(Dict("step", "SetActive(false/true)", "probe", probe != null ? probe.name : "null",
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
        rows.Add(Dict("step", "Nearest vs All(false) 3D 扫描", "range", range,
                      "registryResult", viaRegistry != null ? viaRegistry.name : "null",
                      "scanResult", viaScan != null ? viaScan.name : "null",
                      "candidatesInRange", candidates, "registryDistance", viaRegistry != null
                          ? (object)Vector3.Distance(center, viaRegistry.transform.position) : null,
                      "scanDistance", viaScan != null ? (object)Vector3.Distance(center, viaScan.transform.position) : null,
                      "nearestEqualsScan", nearestOk, "nonEmpty", nonEmpty, "pickedFixture", foundProbe,
                      "pass", nearestOk && nonEmpty));

        _ev.Record(Id, Id,
            "UnitRegistry 契约：OfTeam(Player,false)=3；SetActive(false) 后 All(false) 不含而 All(true) 含、恢复后重新含入；" +
            "Nearest(center,10,Monster) 与遍历 All(false) + 3D 距离最近的结果一致",
            "players=" + playerCount + "; toggle=" + toggleOk + "; nearestEqualsScan=" + nearestOk +
            "; candidates=" + candidates,
            allPass, allPass ? "pass" : "product_fail", "production",
            Dict("steps", rows, "count", UnitRegistry.Count,
                 "note", "只验证 M1 契约语义；不改动 M0 的 8 条断言口径"));
    }

    static bool Contains(IReadOnlyList<UnitHealth> list, UnitHealth unit)
    {
        if (list == null || unit == null) return false;
        for (int i = 0; i < list.Count; i++)
            if (list[i] == unit) return true;
        return false;
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
                Dict("head", GetHead(),
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
        foreach (string n in new[] { "Knight_Unit", "Archer_Unit", "Mage_Unit" })
        {
            PlayerProbe p = FindPlayer(n);
            if (p != null) _players.Add(p);
        }
        foreach (var p in _players)
            Debug.Log("[REFACTOR] player " + p.name + " at " + V3Text(p.home) +
                      "; sword=" + (p.sword != null) + "; ranged=" + (p.ranged != null) +
                      "; skills=" + (p.skills != null) + "; rescue=" + (p.rescue != null));
    }

    static PlayerProbe FindPlayer(string name)
    {
        foreach (var h in FindObjectsByType<UnitHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (h == null || h.team != Team.Player) continue;
            if (!(h.name == name || h.name.StartsWith(name + " ", StringComparison.Ordinal) ||
                  h.name.StartsWith(name + "(", StringComparison.Ordinal))) continue;

            var p = new PlayerProbe();
            p.name = name;
            p.health = h;
            p.brain = h.GetComponent<CharacterBrain>();
            p.animator = h.GetComponentInChildren<Animator>();
            p.sword = h.GetComponent<MeleeCombat>();
            p.ranged = h.GetComponent<RangedCombat>();
            p.skills = h.GetComponent<SkillSystem>();
            p.rescue = h.GetComponent<DownedRescue>();
            p.home = h.transform.position;
            p.attackSpeed = p.brain != null && p.brain.stats != null ? p.brain.stats.attackSpeed : 1f;
            return p;
        }
        return null;
    }

    GameObject CreateTarget(string name, Vector3 pos)
    {
        DestroyTarget();

        var go = new GameObject(name);
        go.SetActive(false);
        go.transform.position = pos;

        _targetStats = ScriptableObject.CreateInstance<UnitStats>();
        _targetStats.team = Team.Monster;
        _targetStats.maxHp = 100000f;
        _targetStats.damage = 0f;
        _targetStats.attackRange = 0f;
        _targetStats.preferredRange = 0f;
        _targetStats.aggroRange = 0f;
        _targetStats.moveSpeed = 0f;
        _targetStats.attackSpeed = 1f;

        var uh = go.AddComponent<UnitHealth>();
        uh.stats = _targetStats;
        uh.showDamageNumber = false;

        var col = go.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 0.9f, 0f);
        col.height = 1.8f;
        col.radius = 0.4f;

        go.SetActive(true);
        return go;
    }

    static void Select(PlayerProbe p)
    {
        var sm = SelectionManager.instance;
        if (sm == null || p == null || p.health == null) return;
        var sel = p.health.GetComponent<Selectable>();
        if (sel == null) return;
        var m = typeof(SelectionManager).GetMethod("SingleSelect", BindingFlags.Instance | BindingFlags.NonPublic);
        if (m != null) m.Invoke(sm, new object[] { sel });
    }

    IEnumerator WaitState(PlayerProbe p, string state, float timeout)
    {
        float t = 0f;
        while (t < timeout)
        {
            if (p.animator != null && StateName(p.animator) == state) yield break;
            yield return null;
            t += Time.deltaTime;
        }
    }

    static bool GetFired(RangedCombat r)
    {
        if (r == null) return false;
        if (_firedField == null)
            _firedField = typeof(RangedCombat).GetField("_fired", BindingFlags.Instance | BindingFlags.NonPublic);
        return _firedField != null && (bool)_firedField.GetValue(r);
    }

    static string StateName(Animator a, int layer = 0)
    {
        if (a == null) return "null";
        if (layer < 0 || layer >= a.layerCount) return "layer_out_of_range";
        var st = a.GetCurrentAnimatorStateInfo(layer);
        if (st.IsName(Locomotion)) return Locomotion;
        if (st.IsName(AttackMelee)) return AttackMelee;
        if (st.IsName("Attack_Draw")) return "Attack_Draw";
        if (st.IsName(AttackRelease)) return AttackRelease;
        if (st.IsName(HitA)) return HitA;
        if (st.IsName("Skill")) return "Skill";
        if (st.IsName("Bash")) return "Bash";
        if (st.IsName("Interact")) return "Interact";
        if (st.IsName(Downed)) return Downed;
        if (st.IsName("Rise")) return "Rise";
        return "Other:" + st.shortNameHash;
    }

    static float StateNorm(Animator a, int layer = 0)
    {
        if (a == null || layer < 0 || layer >= a.layerCount) return -1f;
        float n = a.GetCurrentAnimatorStateInfo(layer).normalizedTime;
        return n <= 1f ? n : 1f;
    }

    static string GetHead()
    {
#if UNITY_EDITOR
        return SessionState.GetString("REFACTOR.Head", "unknown");
#else
        return "n/a";
#endif
    }

    static string F(float v)
    {
        return v.ToString("0.###", CultureInfo.InvariantCulture);
    }

    static string V3Text(Vector3 v)
    {
        return "(" + F(v.x) + ", " + F(v.y) + ", " + F(v.z) + ")";
    }

    static Dictionary<string, object> Dict(params object[] kvs)
    {
        var d = new Dictionary<string, object>();
        for (int i = 0; i + 1 < kvs.Length; i += 2)
            d[(string)kvs[i]] = kvs[i + 1];
        return d;
    }

    static Dictionary<string, object> V3(Vector3 v)
    {
        return new Dictionary<string, object> { { "x", v.x }, { "y", v.y }, { "z", v.z } };
    }
}
