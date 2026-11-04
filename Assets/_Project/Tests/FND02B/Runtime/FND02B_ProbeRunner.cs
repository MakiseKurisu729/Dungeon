using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

/// <summary>
/// FND-02B 运行期探针（Play 模式）。
/// 只读生产状态；测试自身唯一写入面为 Docs 证据、SessionState 退出码、
/// 以及运行期内存夹具（FND02B_Target_*，结束前销毁）。
/// 断言：Locomotion.AllPlayersMoveParam → Regression.FND02A → Attack.DualPathUnified →
///       Clock.MeleeNormalizedWindow → Clock.RangedOneShotPerCycle →
///       Animation.PostCombatAndHit → UI.DamageNumberScale。
/// 不使用 Warp、不写玩家坐标、不直接 SetDestination 冒充指令。
/// </summary>
public class FND02B_ProbeRunner : MonoBehaviour
{
    public const string ScenePath = "Assets/Scenes/DungeonWorld.unity";
    public const string EnemyPrefabGuid = "18dec5e6af31178468b146e0440bd081";
    public const string ControllerPath = "Assets/_Project/Art/Animations/PlayerBase.controller";
    public const string FinishedKey = "FND02B.Finished";
    public const string ExitCodeKey = "FND02B.ExitCode";

    public static readonly Vector3 EnemyPos = new Vector3(-2.72f, 0f, -12f);
    const float ApproachDestZ = -5f;
    const float PosTolXZ = 0.05f;
    const float PosTolY = 0.10f;

    const string Locomotion = "Locomotion";
    const string AttackDraw = "Attack_Draw";
    const string AttackRelease = "Attack_Release";
    const string HitA = "Hit_A";

    static FND02B_Evidence _ev;
    static bool _finished;
    static string _exitReason = "";
    static FieldInfo _firedField;
    static FieldInfo _rangedTimerField;
    static FieldInfo _swordTimerField;

    int _run;
    string _mode;
    int _seed;
    float _deadline;

    UnitHealth _enemy;
    Vector3 _enemyStartPos;
    bool _enemyFound;
    bool _gameStarted;
    bool _navOk;
    string _enemySourceGuid = "none";
    bool _approachArrived;
    float _approachStartDist = -1f;
    float _approachEndDist = -1f;
    float _approachTraveled;
    float _approachMaxStep;
    string _approachPathStatus = "none";
    int _approachCorners;

    FND02B_Observer _observer;
    readonly List<PlayerProbe> _players = new List<PlayerProbe>();
    GameObject _target;
    UnitStats _targetStats;

    class PlayerProbe
    {
        public string name;
        public UnitHealth health;
        public CharacterBrain brain;
        public Animator animator;
        public Selectable selectable;
        public SwordSwing sword;
        public RangedCombat ranged;
        public Vector3 home;
        public float attackSpeed = 1f;
        public float cycle = 1f;
        public float effectiveCycle = 1f;

        public int drawEntries;
        public int releaseEntries;
        public int fireCount;
        public string lastState = "";
        public bool lastFired;
        public float firstDrawTime = -1f;
        public float firstFireTime = -1f;
        public List<Dictionary<string, object>> fireEvents = new List<Dictionary<string, object>>();
        public List<Dictionary<string, object>> hitEvents = new List<Dictionary<string, object>>();

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
            if (Events.Count < 500)
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
        string envRun = Environment.GetEnvironmentVariable("FND02B_RUN");
        if (int.TryParse(envRun, out parsedRun)) _run = parsedRun;
        string envMode = Environment.GetEnvironmentVariable("FND02B_MODE");
        _mode = string.IsNullOrEmpty(envMode) ? "green" : envMode.Trim().ToLowerInvariant();
        if (_mode == "red") _run = 0;
        else if (_run <= 0) _run = 1;

        _seed = 20261104 + _run;
        UnityEngine.Random.InitState(_seed);

        _ev = new FND02B_Evidence(_run, _mode);
        _deadline = Time.realtimeSinceStartup + 300f;

        Debug.Log("[FND02B] runner start; mode=" + _mode + "; run=" + _run +
                  "; seed=" + _seed + "; unity=" + Application.unityVersion +
                  "; evidenceDir=" + _ev.EvidenceDir + "; head=" + GetHead());

        float waited = 0f;
        while (SceneManager.GetActiveScene().path != ScenePath && waited < 25f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        Debug.Log("[FND02B] active scene=" + SceneManager.GetActiveScene().path +
                  "; waited=" + F(waited) + "s");

        float obsWaited = 0f;
        while (FND02B_Observer.Instance == null && obsWaited < 10f)
        {
            obsWaited += Time.unscaledDeltaTime;
            yield return null;
        }
        _observer = FND02B_Observer.Instance;
        if (_observer == null)
        {
            _ev.InfraFailure("Startup.Observer", "Startup.Observer",
                "运行期观察者未被创建（RuntimeInitializeOnLoadMethod 未执行或 SessionState 未置位）", "");
            Finish();
            yield break;
        }
        Debug.Log("[FND02B] observer ready; baselineGameStarted=" + FND02B_Observer.BaselineGameStarted);

        yield return new WaitForSecondsRealtime(1.0f);

        FindPlayers();
        if (_players.Count != 3)
        {
            _ev.BlockedAssertion("All", "Players.Present",
                "Knight_Unit / Archer_Unit / Mage_Unit 三个生产玩家单位均存在",
                "找到玩家数=" + _players.Count, Dict("players", _players.Select(p => p.name).ToList()));
        }

        yield return RunSafe("Locomotion.AllPlayersMoveParam", AssertLocomotion());
        yield return RunSafe("Regression.FND02A", AssertRegressionFND02A());
        yield return RunSafe("Attack.DualPathUnified", AssertDualPathAndClocks());
        yield return RunSafe("Animation.PostCombatAndHit", AssertPostCombatAndHit());
        yield return RunSafe("UI.DamageNumberScale", AssertDamageNumberScale());

        RecordRegressionFinal();
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
                Debug.LogError("[FND02B] assertion " + id + " threw: " + e);
                _ev.InfraFailure(id, id, e.Message, e.StackTrace);
                yield break;
            }
            yield return cur;
        }
    }

    // ============================================================
    // 1. Locomotion.AllPlayersMoveParam
    // ============================================================
    IEnumerator AssertLocomotion()
    {
        var data = Dict(
            "entry", "SelectionManager.SingleSelect(Selectable) → ForEachSelectedBrain(b => b.CommandMove(Vector3))（与右键移动同一生产方法）",
            "players", new List<object>(),
            "controllerPath", ControllerPath);
        var playerRows = new List<object>();
        bool allPlayers = _players.Count == 3;
        bool allMove = true;

        foreach (var p in _players)
        {
            if (p.brain == null || p.brain.agent == null || p.animator == null)
            {
                allMove = false;
                playerRows.Add(Dict("name", p.name, "error", "missing brain/agent/animator"));
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
            float peakParam = 0f;
            string stateDuringMove = "none";
            bool grounded = true;
            bool arrived = false;
            Vector3 prev = p.tx.position;
            float traveled = 0f;
            float maxStep = 0f;

            while (elapsed < 8f)
            {
                yield return null;
                elapsed += Time.deltaTime;
                Vector3 pos = p.tx.position;
                float step = Vector3.Distance(prev, pos);
                if (step > maxStep) maxStep = step;
                traveled += step;
                prev = pos;

                float speed = p.brain.agent.velocity.magnitude;
                float sx = p.animator.GetFloat("SpeedX");
                float sz = p.animator.GetFloat("SpeedZ");
                float param = Mathf.Sqrt(sx * sx + sz * sz);
                if (speed > 0.5f && stateDuringMove == "none") stateDuringMove = StateName(p.animator);
                if (speed > peakSpeed) peakSpeed = speed;
                if (param > peakParam) peakParam = param;
                grounded &= p.animator.GetBool("Grounded");

                if (!p.brain.agent.pathPending && p.brain.agent.hasPath &&
                    p.brain.agent.remainingDistance <= p.brain.agent.stoppingDistance + 0.15f)
                {
                    arrived = true;
                    break;
                }
            }

            yield return new WaitForSeconds(1.2f);
            float finalSpeed = p.brain.agent.velocity.magnitude;
            float finalSx = p.animator.GetFloat("SpeedX");
            float finalSz = p.animator.GetFloat("SpeedZ");
            float finalParam = Mathf.Sqrt(finalSx * finalSx + finalSz * finalSz);

            bool playerPass = arrived && peakSpeed >= 2.0f && peakParam >= 2.0f &&
                              stateDuringMove == Locomotion && finalParam <= 0.15f && finalSpeed <= 0.15f && grounded;
            allMove &= playerPass;

            playerRows.Add(Dict(
                "name", p.name,
                "destination", V3(dest),
                "arrived", arrived,
                "durationSeconds", elapsed,
                "traveled", traveled,
                "maxSingleFrameDisplacement", maxStep,
                "peakAgentSpeed", peakSpeed,
                "peakAnimatorMoveParam", peakParam,
                "stateDuringMove", stateDuringMove,
                "finalAgentSpeed", finalSpeed,
                "finalAnimatorMoveParam", finalParam,
                "groundedDuringMove", grounded,
                "pass", playerPass));
        }

        bool stateNameExact = false;
        bool blend2D = false;
        bool usesSpeedZ = false;
        bool layer0Only = false;
        int layerCount = -1;
#if UNITY_EDITOR
        var ac = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(ControllerPath);
        if (ac != null)
        {
            layerCount = ac.layers.Length;
            layer0Only = layerCount == 1;
            var sm = ac.layers[0].stateMachine;
            AnimatorState loco = null;
            foreach (var cs in sm.states)
            {
                if (cs.state != null && cs.state.name == Locomotion) { loco = cs.state; break; }
            }
            stateNameExact = loco != null;
            if (loco != null)
            {
                var bt = loco.motion as BlendTree;
                if (bt != null)
                {
                    blend2D = bt.blendType != BlendTreeType.Simple1D;
                    usesSpeedZ = bt.blendParameterY == "SpeedZ";
                }
            }
        }
#endif
        bool controllerPass = stateNameExact && blend2D && usesSpeedZ && layer0Only;
        data["controller"] = Dict(
            "stateNameExact", stateNameExact,
            "blendTreeIs2D", blend2D,
            "blendParameterY", usesSpeedZ ? "SpeedZ" : "other",
            "layerCount", layerCount,
            "layer0Only", layer0Only,
            "note", "red 基线：状态名为带前导空格的 \" Locomotion\"，混合树 m_BlendType=0（Simple1D，仅 SpeedX 驱动）");
        data["players"] = playerRows;

        bool pass = allPlayers && allMove && controllerPass;
        _ev.Record("Locomotion.AllPlayersMoveParam", "Locomotion.AllPlayersMoveParam",
            "三角色经生产移动命令移动时处于 Locomotion，SpeedX/SpeedZ 随实际速度非零变化，停稳归零；控制器 Locomotion 状态名精确且 2D 混合树使用 SpeedZ",
            "playersPass=" + allMove + "; controllerPass=" + controllerPass +
            "; stateNameExact=" + stateNameExact + "; blend2D=" + blend2D + "; usesSpeedZ=" + usesSpeedZ,
            pass, pass ? "pass" : "product_fail", "production", data);

        // 回到出生点，避免影响后续接近/战斗
        foreach (var p in _players)
        {
            if (p.brain == null || p.brain.agent == null) continue;
            Select(p);
            var sm = SelectionManager.instance;
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
    }

    // ============================================================
    // 2. Regression.FND02A
    // ============================================================
    IEnumerator AssertRegressionFND02A()
    {
        _gameStarted = GameFlow.GameStarted;
        _enemy = FindRealEnemy();
        _enemyFound = _enemy != null;
        _enemySourceGuid = GetEnemySourceGuid();

        if (_enemy != null)
        {
            _enemyStartPos = _enemy.transform.position;
            NavMeshAgent agent = _enemy.GetComponent<NavMeshAgent>();
            NavMeshHit hit;
            bool sampled = NavMesh.SamplePosition(_enemy.transform.position, out hit, 1.0f, NavMesh.AllAreas);
            _navOk = agent != null && agent.enabled && agent.isOnNavMesh && sampled;
        }

        var knight = _players.FirstOrDefault(p => p.name == "Knight_Unit");
        if (knight != null && knight.brain != null && knight.brain.agent != null && _enemy != null)
        {
            Vector3 dest = new Vector3(EnemyPos.x, knight.tx.position.y, ApproachDestZ);
            NavMeshHit destHit;
            if (NavMesh.SamplePosition(dest, out destHit, 2.0f, NavMesh.AllAreas)) dest = destHit.position;

            Vector3 start = knight.tx.position;
            _approachStartDist = XZ(start, EnemyPos);

            NavMeshPath path = new NavMeshPath();
            bool pathOk = NavMesh.CalculatePath(start, dest, NavMesh.AllAreas, path);
            _approachPathStatus = path.status.ToString();
            _approachCorners = path.corners != null ? path.corners.Length : 0;

            Select(knight);
            var sm = SelectionManager.instance;
            if (sm != null) sm.ForEachSelectedBrain(b => b.CommandMove(dest));
            else knight.brain.CommandMove(dest);

            float elapsed = 0f;
            Vector3 prev = start;
            while (elapsed < 15f)
            {
                yield return null;
                elapsed += Time.deltaTime;
                Vector3 pos = knight.tx.position;
                float step = Vector3.Distance(prev, pos);
                if (step > _approachMaxStep) _approachMaxStep = step;
                _approachTraveled += step;
                prev = pos;
                if (!knight.brain.agent.pathPending && knight.brain.agent.hasPath &&
                    knight.brain.agent.remainingDistance <= knight.brain.agent.stoppingDistance + 0.2f)
                {
                    _approachArrived = true;
                    break;
                }
            }
            if (_approachArrived)
            {
                float settle = 0f;
                while (settle < 1.0f && knight.brain.agent.velocity.sqrMagnitude > 0.0025f)
                {
                    yield return null;
                    settle += Time.deltaTime;
                }
            }
            _approachEndDist = XZ(knight.tx.position, EnemyPos);
        }

        // 回到出生点并冻结真实敌人，隔离后续战斗夹具
        if (knight != null && knight.brain != null && knight.brain.agent != null)
        {
            Select(knight);
            var sm = SelectionManager.instance;
            if (sm != null) sm.ForEachSelectedBrain(b => b.CommandMove(knight.home));
            else knight.brain.CommandMove(knight.home);
            float w = 0f;
            while (w < 10f)
            {
                yield return null;
                w += Time.deltaTime;
                if (!knight.brain.agent.pathPending && knight.brain.agent.hasPath &&
                    knight.brain.agent.remainingDistance <= knight.brain.agent.stoppingDistance + 0.2f) break;
            }
        }
        if (_enemy != null) _enemy.gameObject.SetActive(false);
    }

    // ============================================================
    // 3+4+5. Attack.DualPathUnified / Clock.*
    // ============================================================
    IEnumerator AssertDualPathAndClocks()
    {
        if (_players.Count != 3)
        {
            _ev.BlockedAssertion("Attack.DualPathUnified", "Attack.DualPathUnified",
                "三角色均能进入攻击状态", "玩家数量不足", null);
            yield break;
        }

        _target = CreateTarget("FND02B_Target_Combat", new Vector3(0f, 0f, -4f), false);
        if (_target == null)
        {
            _ev.InfraFailure("Attack.DualPathUnified", "Attack.DualPathUnified", "夹具创建失败", "");
            yield break;
        }
        var targetHealth = _target.GetComponent<UnitHealth>();

        // ---- 指定攻击路径（CommandAttack） ----
        var designatedRows = new List<object>();
        bool designatedPass = true;
        foreach (var p in _players)
        {
            if (p.brain == null || p.animator == null) { designatedPass = false; continue; }

            var combat = p.brain.combat as ICharacterCombat;
            if (combat != null) combat.CancelAttack();
            yield return WaitState(p, Locomotion, 4.0f);

            var collector = new HitCollector();
            collector.Subscribe(targetHealth, p.health != null ? p.health.GetInstanceID() : 0);
            p.lastFired = GetFired(p.ranged);

            p.brain.enabled = false;
            p.brain.CommandAttack(_target.transform);
            p.brain.enabled = true;

            float t = 0f;
            Dictionary<string, object> ev = null;
            string evType = "missing";
            while (t < 10f && ev == null)
            {
                yield return null;
                t += Time.deltaTime;
                if (p.ranged != null && PollFire(p))
                {
                    evType = "fire";
                    ev = Dict("path", "CommandAttack", "player", p.name, "event", evType,
                              "time", Time.time, "state", StateName(p.animator),
                              "normalizedTime", StateNorm(p.animator),
                              "attackSpeedParam", p.animator.GetFloat("AttackSpeed"),
                              "attackSpeedStats", p.attackSpeed);
                }
                else if (p.sword != null && collector.Count > 0)
                {
                    evType = "hit";
                    var h = collector.Events[0];
                    ev = Dict("path", "CommandAttack", "player", p.name, "event", evType,
                              "time", h["time"], "state", h["state"],
                              "normalizedTime", h["normalizedTime"],
                              "attackSpeedParam", p.animator.GetFloat("AttackSpeed"),
                              "attackSpeedStats", p.attackSpeed);
                }
            }
            collector.Unsubscribe();

            bool evOk = ev != null && (string)ev["state"] == AttackRelease;
            designatedPass &= evOk;
            if (ev == null) ev = Dict("path", "CommandAttack", "player", p.name, "event", "missing", "state", "none");
            ev["pass"] = evOk;
            designatedRows.Add(ev);

            p.brain.CommandAttack(null);
            var c2 = p.brain.combat as ICharacterCombat;
            if (c2 != null) c2.CancelAttack();
            yield return WaitState(p, Locomotion, 4.0f);
        }

        // ---- 自动攻击路径（Aggro 自动索敌） ----
        foreach (var p in _players)
        {
            p.brain.CommandAttack(null);
            var c = p.brain.combat as ICharacterCombat;
            if (c != null) c.CancelAttack();
        }
        yield return WaitAllStates(Locomotion, 5.0f);

        var autoCollector = new HitCollector();
        autoCollector.Subscribe(targetHealth, 0);
        foreach (var p in _players)
        {
            p.drawEntries = 0;
            p.releaseEntries = 0;
            p.fireCount = 0;
            p.lastState = StateName(p.animator);
            p.lastFired = GetFired(p.ranged);
            p.firstDrawTime = -1f;
            p.firstFireTime = -1f;
            p.fireEvents.Clear();
        }

        // Knight 距离夹具 4m > 2.2m，先经生产移动命令进入近战距离（目标点 = 夹具位置正前方 2m）
        var knight = _players.FirstOrDefault(p => p.name == "Knight_Unit");
        if (knight != null && knight.brain != null && knight.brain.agent != null)
        {
            Vector3 tp = _target.transform.position;
            Vector3 kdest = new Vector3(tp.x, knight.tx.position.y, tp.z + 2.0f);
            NavMeshHit khit;
            if (NavMesh.SamplePosition(kdest, out khit, 2.0f, NavMesh.AllAreas)) kdest = khit.position;
            Select(knight);
            var sm = SelectionManager.instance;
            if (sm != null) sm.ForEachSelectedBrain(b => b.CommandMove(kdest));
            else knight.brain.CommandMove(kdest);
        }

        float duration = 2.5f * _players.Max(p => p.effectiveCycle) + 1.5f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            yield return null;
            elapsed += Time.deltaTime;
            foreach (var p in _players)
            {
                TrackState(p);
                if (p.ranged != null && PollFire(p))
                {
                    p.fireCount++;
                    var fe = Dict("time", Time.time, "state", StateName(p.animator),
                                  "normalizedTime", StateNorm(p.animator),
                                  "attackSpeedParam", p.animator.GetFloat("AttackSpeed"),
                                  "attackSpeedStats", p.attackSpeed);
                    p.fireEvents.Add(fe);
                    if (p.firstFireTime < 0f) p.firstFireTime = Time.time;
                }
            }
        }
        autoCollector.Unsubscribe();

        foreach (var p in _players)
        {
            p.hitEvents = autoCollector.Events
                .Where(e => p.health != null && (int)e["sourceId"] == p.health.GetInstanceID())
                .ToList();
        }

        var autoRows = new List<object>();
        bool autoPass = true;
        foreach (var p in _players)
        {
            Dictionary<string, object> ev = null;
            if (p.sword != null && p.hitEvents.Count > 0) ev = p.hitEvents[0];
            else if (p.ranged != null && p.fireEvents.Count > 0) ev = p.fireEvents[0];

            bool evOk = ev != null && (string)ev["state"] == AttackRelease;
            autoPass &= evOk;
            var row = Dict("path", "AggroAuto", "player", p.name,
                           "event", p.sword != null ? "hit" : "fire",
                           "state", ev != null ? ev["state"] : "missing",
                           "normalizedTime", ev != null ? ev["normalizedTime"] : (object)-1f,
                           "attackSpeedParam", p.animator != null ? (object)p.animator.GetFloat("AttackSpeed") : -1f,
                           "attackSpeedStats", p.attackSpeed,
                           "pass", evOk);
            autoRows.Add(row);
        }

        bool dualPass = designatedPass && autoPass;
        _ev.Record("Attack.DualPathUnified", "Attack.DualPathUnified",
            "CommandAttack 与 Aggro 自动索敌两条路径的命中/发射瞬间均处于 Attack_Release，且不存在 Locomotion 出伤/出射",
            "designatedPass=" + designatedPass + "; autoPass=" + autoPass,
            dualPass, dualPass ? "pass" : "product_fail", "production",
            Dict("designatedPath", designatedRows, "autoPath", autoRows,
                 "fixture", "FND02B_Target_Combat（运行期内存 UnitHealth，高 HP，无 AI）",
                 "note", "指定攻击通过 CharacterBrain.CommandAttack（与 SelectionManager 点敌同一生产方法）；自动攻击通过 CharacterBrain.Update → UnitBrain.AttackLoop → ICharacterCombat.AutoAttack"));

        // ---- Clock.MeleeNormalizedWindow ----
        var melee = _players.FirstOrDefault(p => p.sword != null);
        bool meleePass = false;
        var meleeHits = new List<object>();
        float activeStart = -1f, activeEnd = -1f;
        if (melee != null)
        {
            activeStart = melee.sword.activeStart;
            activeEnd = melee.sword.activeEnd;
            int inWindow = 0;
            foreach (var h in melee.hitEvents)
            {
                string state = (string)h["state"];
                float nt = (float)h["normalizedTime"];
                bool ok = state == AttackRelease && nt >= activeStart - 0.06f && nt <= activeEnd + 0.06f;
                if (ok) inWindow++;
                meleeHits.Add(Dict("time", h["time"], "state", state, "normalizedTime", nt,
                                   "inWindow", ok, "amount", h["amount"]));
            }
            float attackSpeedParam = melee.animator != null ? melee.animator.GetFloat("AttackSpeed") : -1f;
            bool speedOk = Mathf.Abs(attackSpeedParam - melee.attackSpeed) < 0.01f;
            bool countOk = melee.hitEvents.Count >= 2 && inWindow == melee.hitEvents.Count &&
                           Mathf.Abs(melee.hitEvents.Count - melee.releaseEntries) <= 1;
            meleePass = countOk && speedOk;
            _ev.Record("Clock.MeleeNormalizedWindow", "Clock.MeleeNormalizedWindow",
                "骑士命中由 layer 0 Attack_Release.normalizedTime 的 [activeStart, activeEnd] 窗口驱动；单周期恰 1 次；AttackSpeed 参数等于 stats.attackSpeed",
                "hits=" + melee.hitEvents.Count + "; inWindow=" + inWindow +
                "; releaseEntries=" + melee.releaseEntries +
                "; window=[" + F(activeStart) + "," + F(activeEnd) + "]" +
                "; attackSpeedParam=" + F(attackSpeedParam) + "; stats=" + F(melee.attackSpeed),
                meleePass, meleePass ? "pass" : "product_fail", "production",
                Dict("windowStart", activeStart, "windowEnd", activeEnd,
                     "hits", meleeHits, "releaseEntries", melee.releaseEntries,
                     "attackSpeedParam", attackSpeedParam, "attackSpeedStats", melee.attackSpeed,
                     "effectiveLayer", melee.sword.attackLayer,
                     "drawStateName", melee.sword.attackStateName,
                     "singleTargetMode", melee.sword.attackMode.ToString()));
        }
        else
        {
            _ev.BlockedAssertion("Clock.MeleeNormalizedWindow", "Clock.MeleeNormalizedWindow",
                "骑士近战时钟", "未找到 SwordSwing 玩家", null);
        }

        // ---- Clock.RangedOneShotPerCycle ----
        var rangedRows = new List<object>();
        bool rangedPass = true;
        foreach (var p in _players.Where(x => x.ranged != null))
        {
            float ws = -1f, we = -1f;
            var f1 = typeof(RangedCombat).GetField("releaseWindowStart");
            var f2 = typeof(RangedCombat).GetField("releaseWindowEnd");
            if (f1 != null && f2 != null)
            {
                ws = (float)f1.GetValue(p.ranged);
                we = (float)f2.GetValue(p.ranged);
            }

            bool windowOk = ws >= 0f && we >= ws;
            bool fireStateOk = true;
            bool fireWindowOk = true;
            var fireRows = new List<object>();
            float prevFire = -1f;
            float minInterval = float.MaxValue;
            foreach (var fe in p.fireEvents)
            {
                string state = (string)fe["state"];
                float nt = (float)fe["normalizedTime"];
                bool sOk = state == AttackRelease;
                bool wOk = windowOk && nt >= ws - 0.06f && nt <= we + 0.06f;
                fireStateOk &= sOk;
                fireWindowOk &= wOk;
                float ft = (float)fe["time"];
                if (prevFire >= 0f)
                {
                    float iv = ft - prevFire;
                    if (iv < minInterval) minInterval = iv;
                }
                prevFire = ft;
                fireRows.Add(Dict("time", ft, "state", state, "normalizedTime", nt,
                                  "stateOk", sOk, "windowOk", wOk,
                                  "attackSpeedParam", fe["attackSpeedParam"]));
            }
            bool countOk = p.drawEntries >= 2 && p.fireCount >= 2 &&
                           Mathf.Abs(p.fireCount - p.drawEntries) <= 1;
            bool intervalOk = p.fireCount < 2 || minInterval >= 0.75f * p.cycle;
            bool latencyOk = true;
            float latency = -1f;
            if (p.firstDrawTime >= 0f && p.firstFireTime >= 0f)
            {
                latency = p.firstFireTime - p.firstDrawTime;
                latencyOk = latency <= 0.55f * p.cycle + 0.30f;
            }
            float attackSpeedParam = p.animator != null ? p.animator.GetFloat("AttackSpeed") : -1f;
            bool speedOk = Mathf.Abs(attackSpeedParam - p.attackSpeed) < 0.01f;
            bool pass = countOk && fireStateOk && fireWindowOk && intervalOk && latencyOk && speedOk && windowOk;
            rangedPass &= pass;
            rangedRows.Add(Dict(
                "player", p.name,
                "drawEntries", p.drawEntries,
                "fireCount", p.fireCount,
                "releaseEntries", p.releaseEntries,
                "windowStart", ws, "windowEnd", we,
                "fires", fireRows,
                "minFireInterval", p.fireCount >= 2 ? minInterval : -1f,
                "expectedCycle", p.cycle,
                "firstDrawToFirstFireLatency", latency,
                "latencyLimit", 0.55f * p.cycle + 0.30f,
                "attackSpeedParam", attackSpeedParam,
                "attackSpeedStats", p.attackSpeed,
                "countOk", countOk, "fireStateOk", fireStateOk, "fireWindowOk", fireWindowOk,
                "intervalOk", intervalOk, "latencyOk", latencyOk, "speedOk", speedOk, "pass", pass));
        }
        _ev.Record("Clock.RangedOneShotPerCycle", "Clock.RangedOneShotPerCycle",
            "弓手/法师单个攻击动画周期恰发射 1 次；发射瞬间处于 Attack_Release 的可调 normalizedTime 窗口；法师下令到首次出手符合攻速周期",
            "rangedPass=" + rangedPass,
            rangedPass, rangedPass ? "pass" : "product_fail", "production",
            Dict("players", rangedRows,
                 "note", "观测窗 " + F(duration) + "s；以 Attack_Draw 进入次数 vs 发射次数、发射瞬间状态/归一化时间、最小发射间隔与首次出手延迟共同判定"));
    }

    // ============================================================
    // 6. Animation.PostCombatAndHit
    // ============================================================
    IEnumerator AssertPostCombatAndHit()
    {
        DestroyTarget();
        float t = 0f;
        bool allLoco = false;
        float settleTime = -1f;
        while (t < 7f)
        {
            yield return null;
            t += Time.deltaTime;
            if (_players.All(p => p.animator != null && StateName(p.animator) == Locomotion))
            {
                allLoco = true;
                settleTime = t;
                break;
            }
        }

        var knight = _players.FirstOrDefault(p => p.name == "Knight_Unit");
        bool hitPass = false;
        float hpBefore = -1f, hpAfter = -1f;
        int onHitCount = 0;
        string hitState = "none";
        float hitNorm = -1f;
        if (knight != null && knight.health != null && !knight.health.dead)
        {
            hpBefore = knight.health.hp;
            Action<DamageInfo> cb = info => { onHitCount++; };
            knight.health.OnHit += cb;
            knight.health.ApplyDamage(5f, knight.tx.position + Vector3.up * 1f, null);
            hpAfter = knight.health.hp;
            float ht = 0f;
            while (ht < 0.8f)
            {
                yield return null;
                ht += Time.deltaTime;
                string s = StateName(knight.animator);
                if (s == HitA)
                {
                    hitState = s;
                    hitNorm = StateNorm(knight.animator);
                    hitPass = true;
                    break;
                }
                if (hitState == "none") hitState = s;
            }
            knight.health.OnHit -= cb;
        }

        bool pass = allLoco && hitPass && hpBefore >= 0f && Mathf.Abs(hpAfter - (hpBefore - 5f)) < 0.01f && onHitCount == 1;
        _ev.Record("Animation.PostCombatAndHit", "Animation.PostCombatAndHit",
            "目标销毁后限定时间内三角色回到 Locomotion；玩家经生产 UnitHealth.ApplyDamage 受非致命伤害后进入 Hit_A",
            "allLocomotion=" + allLoco + "; settle=" + F(settleTime) + "s" +
            "; hitState=" + hitState + "; hp=" + F(hpBefore) + "→" + F(hpAfter) +
            "; onHit=" + onHitCount,
            pass, pass ? "pass" : "product_fail", "production",
            Dict("postCombat", Dict(
                    "targetDestroyed", true,
                    "allPlayersLocomotion", allLoco,
                    "settleSeconds", settleTime,
                    "states", _players.Select(p => Dict("name", p.name, "state", StateName(p.animator))).ToList()),
                 "hit", Dict(
                    "entry", "UnitHealth.ApplyDamage(5f, hitPos, null)（生产统一伤害入口）",
                    "hpBefore", hpBefore, "hpAfter", hpAfter,
                    "onHitCount", onHitCount,
                    "observedState", hitState,
                    "observedNormalizedTime", hitNorm,
                    "dead", knight != null && knight.health != null && knight.health.dead)));

        yield return new WaitForSeconds(1.0f);
    }

    // ============================================================
    // 7. UI.DamageNumberScale
    // ============================================================
    IEnumerator AssertDamageNumberScale()
    {
        var knight = _players.FirstOrDefault(p => p.name == "Knight_Unit");
        if (knight == null || knight.health == null)
        {
            _ev.BlockedAssertion("UI.DamageNumberScale", "UI.DamageNumberScale",
                "伤害飘字放大 3.5x", "未找到 Knight", null);
            yield break;
        }

        knight.health.ApplyDamage(3f, knight.tx.position + Vector3.up * 1.5f, null);
        float t = 0f;
        DamageNumber[] found = null;
        while (t < 1.0f)
        {
            yield return null;
            t += Time.deltaTime;
            found = FindObjectsByType<DamageNumber>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (found != null && found.Length > 0) break;
        }

        float fontSize = -1f;
        Vector3 localScale = Vector3.zero;
        int count = found != null ? found.Length : 0;
        if (count > 0)
        {
            DamageNumber chosen = found.OrderByDescending(d => d.transform.localScale.x).First();
            Text text = chosen.GetComponentInChildren<Text>();
            fontSize = text != null ? text.fontSize : -1f;
            localScale = chosen.transform.localScale;
        }

        const float baselineScale = 0.006f;
        const float expectedMultiplier = 3.5f;
        float expectedScale = baselineScale * expectedMultiplier;
        bool scaleOk = Mathf.Abs(localScale.x - expectedScale) <= expectedScale * 0.15f;

        CombatFeedback cf = FindFirstObjectByType<CombatFeedback>();
        FieldInfo field = typeof(CombatFeedback).GetField("damageNumberScale");
        float multiplier = -1f;
        bool rangeAttr = false;
        if (field != null && cf != null)
        {
            multiplier = (float)field.GetValue(cf);
            rangeAttr = field.GetCustomAttributes(typeof(RangeAttribute), true).Length > 0;
        }
        bool fieldOk = field != null && Mathf.Abs(multiplier - expectedMultiplier) < 0.01f && rangeAttr;

        bool pass = count > 0 && scaleOk && fieldOk;
        _ev.Record("UI.DamageNumberScale", "UI.DamageNumberScale",
            "战斗结算生成的 DamageNumber 视觉尺寸为基线 3.5x（默认 3.5，Inspector 可调）",
            "instances=" + count + "; localScale=" + F(localScale.x) + " (expected " + F(expectedScale) + ")" +
            "; fontSize=" + F(fontSize) + "; multiplierField=" + F(multiplier) + "; rangeAttr=" + rangeAttr,
            pass, pass ? "pass" : "product_fail", "production",
            Dict("baselineLocalScale", baselineScale,
                 "expectedMultiplier", expectedMultiplier,
                 "expectedLocalScale", expectedScale,
                 "instances", count,
                 "observedFontSize", fontSize,
                 "observedLocalScale", V3(localScale),
                 "combatFeedbackField", field != null,
                 "fieldValue", multiplier,
                 "rangeAttribute", rangeAttr,
                 "scaleOk", scaleOk,
                 "fieldOk", fieldOk,
                 "note", "基线：fontSize=40，localScale=0.006；本单按 Transform 缩放放大，字体大小保持基线"));
    }

    // ============================================================
    // 回归汇总
    // ============================================================
    void RecordRegressionFinal()
    {
        int errors = _observer != null ? _observer.ErrorCount : -1;
        var errList = _observer != null ? _observer.ErrorList() : new List<object>();
        bool posOk = _enemyFound &&
                     Mathf.Abs(_enemyStartPos.x - EnemyPos.x) <= PosTolXZ &&
                     Mathf.Abs(_enemyStartPos.y - EnemyPos.y) <= PosTolY &&
                     Mathf.Abs(_enemyStartPos.z - EnemyPos.z) <= PosTolXZ;
        bool guidOk = _enemySourceGuid == EnemyPrefabGuid;
        bool approachOk = _approachArrived &&
                          _approachStartDist >= 11.8f &&
                          _approachEndDist <= 7.35f && _approachEndDist >= 6.6f &&
                          _approachTraveled >= 4.0f && _approachMaxStep <= 0.6f &&
                          _approachPathStatus == "PathComplete" && _approachCorners > 1;
        bool pass = _gameStarted && _enemyFound && posOk && guidOk && _navOk && approachOk && errors == 0;

        _ev.Record("Regression.FND02A", "Regression.FND02A",
            "FND-02A 自动开局、固定遭遇、真实移动接近、零新增 Console 错误/异常继续通过",
            "gameStarted=" + _gameStarted + "; enemyFound=" + _enemyFound + "; posOk=" + posOk +
            "; guidOk=" + guidOk + "; navOk=" + _navOk + "; approachOk=" + approachOk +
            "; consoleErrors=" + errors,
            pass, pass ? "pass" : "product_fail", "production",
            Dict("gameStarted", _gameStarted,
                 "enemyFound", _enemyFound,
                 "enemyName", _enemy != null ? _enemy.name : "none",
                 "enemyStartPosition", V3(_enemyStartPos),
                 "expectedPosition", V3(EnemyPos),
                 "enemySourceGuid", _enemySourceGuid,
                 "expectedPrefabGuid", EnemyPrefabGuid,
                 "navMeshOk", _navOk,
                 "approachArrived", _approachArrived,
                 "approachStartDistance", _approachStartDist,
                 "approachEndDistance", _approachEndDist,
                 "approachTraveled", _approachTraveled,
                 "approachMaxSingleFrameDisplacement", _approachMaxStep,
                 "approachPathStatus", _approachPathStatus,
                 "approachPathCorners", _approachCorners,
                 "consoleErrorCount", errors,
                 "consoleErrors", errList,
                 "errorWindow", "RuntimeInitializeOnLoadMethod(AfterSceneLoad) 起至本断言",
                 "testWroteGameStarted", false,
                 "testDisabledRealEnemyAfterRegression", true));
    }

    // ============================================================
    // 收尾
    // ============================================================
    void DestroyTarget()
    {
        if (_target != null)
        {
            Destroy(_target);
            _target = null;
        }
        if (_targetStats != null)
        {
            Destroy(_targetStats);
            _targetStats = null;
        }
    }

    void CleanupFixtures()
    {
        DestroyTarget();
        foreach (var p in _players)
        {
            if (p.brain != null) p.brain.enabled = true;
        }
    }

    void Finish()
    {
        if (_finished) return;
        _finished = true;

        CleanupFixtures();

        if (_ev != null)
        {
            _ev.WriteSummary(ScenePath, _seed, string.IsNullOrEmpty(_exitReason) ? "completed" : _exitReason,
                Dict("mode", _mode,
                     "head", GetHead(),
                     "enemySourceGuid", _enemySourceGuid,
                     "enemyPosition", V3(EnemyPos),
                     "fixtureNote", "FND02B_Target_Combat 为运行期内存 UnitHealth（高 HP、无 AI、showDamageNumber=false），结束前销毁；不写入场景",
                     "testWroteGameStarted", false));
        }

        int code;
        if (_ev == null || _ev.Infra > 0) code = 3;
        else if (_ev.Fail > 0 || _ev.Blocked > 0) code = 2;
        else code = 0;

        Debug.Log("[FND02B] finish; mode=" + _mode + "; run=" + _run + "; exit=" + code +
                  "; pass=" + (_ev != null ? _ev.Pass : -1) +
                  "; fail=" + (_ev != null ? _ev.Fail : -1) +
                  "; blocked=" + (_ev != null ? _ev.Blocked : -1) +
                  "; infra=" + (_ev != null ? _ev.Infra : -1) +
                  "; observed=" + (_ev != null ? _ev.Observed : -1));

#if UNITY_EDITOR
        SessionState.SetBool(FinishedKey, true);
        SessionState.SetInt(ExitCodeKey, code);
        try
        {
            if (_ev != null)
                System.IO.File.WriteAllText(_ev.ExitCodePath(), code.ToString() + Environment.NewLine,
                    new System.Text.UTF8Encoding(false));
        }
        catch (Exception e) { Debug.LogWarning("[FND02B] exit code file write failed: " + e.Message); }

        if (FND02B_Observer.Instance != null)
            Destroy(FND02B_Observer.Instance.gameObject);
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
        foreach (var p in _players) Debug.Log("[FND02B] player " + p.name + " at " + V3Text(p.home) +
                                              "; sword=" + (p.sword != null) + "; ranged=" + (p.ranged != null) +
                                              "; attackSpeed=" + p.attackSpeed);
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
            p.selectable = h.GetComponent<Selectable>();
            p.sword = h.GetComponent<SwordSwing>();
            p.ranged = h.GetComponent<RangedCombat>();
            p.home = h.transform.position;
            p.attackSpeed = p.brain != null && p.brain.stats != null ? p.brain.stats.attackSpeed : 1f;
            float len = p.sword != null ? p.sword.attackLen : (p.ranged != null ? p.ranged.attackLen : 1f);
            float spd = p.attackSpeed > 0f ? p.attackSpeed : 1f;
            p.cycle = len / spd;
            p.effectiveCycle = Mathf.Max(p.cycle, 2.8f / spd);
            return p;
        }
        return null;
    }

    static UnitHealth FindRealEnemy()
    {
        foreach (var h in FindObjectsByType<UnitHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (h == null || h.team != Team.Monster) continue;
            if (h.name.StartsWith("Skeleton_Warrior", StringComparison.Ordinal)) return h;
        }
        return null;
    }

    static void Select(PlayerProbe p)
    {
        var sm = SelectionManager.instance;
        if (sm == null || p.selectable == null) return;
        var m = typeof(SelectionManager).GetMethod("SingleSelect", BindingFlags.Instance | BindingFlags.NonPublic);
        if (m != null) m.Invoke(sm, new object[] { p.selectable });
    }

    GameObject CreateTarget(string name, Vector3 pos, bool showDamageNumber)
    {
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
        uh.showDamageNumber = showDamageNumber;

        var col = go.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 0.9f, 0f);
        col.height = 1.8f;
        col.radius = 0.4f;

        go.SetActive(true);
        Debug.Log("[FND02B] fixture created " + name + " at " + V3Text(pos) + "; showDamageNumber=" + showDamageNumber);
        return go;
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

    IEnumerator WaitAllStates(string state, float timeout)
    {
        float t = 0f;
        while (t < timeout)
        {
            if (_players.All(p => p.animator != null && StateName(p.animator) == state)) yield break;
            yield return null;
            t += Time.deltaTime;
        }
    }

    static void TrackState(PlayerProbe p)
    {
        if (p.animator == null) return;
        string s = StateName(p.animator);
        if (s != p.lastState)
        {
            if (s == AttackDraw)
            {
                p.drawEntries++;
                if (p.firstDrawTime < 0f) p.firstDrawTime = Time.time;
            }
            if (s == AttackRelease) p.releaseEntries++;
            p.lastState = s;
        }
    }

    static bool GetFired(RangedCombat r)
    {
        if (r == null) return false;
        if (_firedField == null)
            _firedField = typeof(RangedCombat).GetField("_fired", BindingFlags.Instance | BindingFlags.NonPublic);
        return _firedField != null && (bool)_firedField.GetValue(r);
    }

    static bool PollFire(PlayerProbe p)
    {
        if (p.ranged == null) return false;
        bool f = GetFired(p.ranged);
        bool edge = f && !p.lastFired;
        p.lastFired = f;
        return edge;
    }

    static string StateName(Animator a, int layer = 0)
    {
        if (a == null) return "null";
        if (layer < 0 || layer >= a.layerCount) return "layer_out_of_range";
        var st = a.GetCurrentAnimatorStateInfo(layer);
        if (st.IsName(Locomotion)) return Locomotion;
        if (st.IsName(" Locomotion")) return " Locomotion";
        if (st.IsName(AttackDraw)) return AttackDraw;
        if (st.IsName(AttackRelease)) return AttackRelease;
        if (st.IsName(HitA)) return HitA;
        if (st.IsName("Skill")) return "Skill";
        if (st.IsName("Bash")) return "Bash";
        if (st.IsName("Interact")) return "Interact";
        if (st.IsName("Downed")) return "Downed";
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
        return SessionState.GetString("FND02B.Head", "unknown");
#else
        return "n/a";
#endif
    }

    static string GetEnemySourceGuid()
    {
#if UNITY_EDITOR
        return SessionState.GetString("FND02B.EnemySourceGuid", "none");
#else
        return "n/a";
#endif
    }

    static float XZ(Vector3 a, Vector3 b)
    {
        Vector3 d = a - b; d.y = 0f;
        return d.magnitude;
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

/// <summary>
/// 首帧观察者：由 RuntimeInitializeOnLoadMethod(AfterSceneLoad) 创建（早于所有 Start），
/// 只读 Console，不写任何产品状态。
/// </summary>
public class FND02B_Observer : MonoBehaviour
{
    public static FND02B_Observer Instance;
    public static bool BaselineGameStarted;

    public int ErrorCount;
    readonly List<string> _errors = new List<string>();

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Application.logMessageReceived += OnLog;
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= OnLog;
        if (Instance == this) Instance = null;
    }

    void OnLog(string message, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        ErrorCount++;
        if (_errors.Count < 40)
            _errors.Add(type + ": " + message + (string.IsNullOrEmpty(stack) ? "" : " | " + FirstLine(stack)));
    }

    static string FirstLine(string s)
    {
        int i = s.IndexOf('\n');
        return i < 0 ? s : s.Substring(0, i);
    }

    public List<object> ErrorList()
    {
        var list = new List<object>();
        for (int i = 0; i < _errors.Count; i++) list.Add(_errors[i]);
        return list;
    }
}

/// <summary>进入 Play 时的自举钩子：仅在 FND02B.Active 置位时创建观察者。</summary>
public static class FND02B_BootHook
{
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AfterSceneLoadHook()
    {
        if (!SessionState.GetBool("FND02B.Active", false)) return;
        if (FND02B_Observer.Instance != null) return;

        FND02B_Observer.BaselineGameStarted = GameFlow.GameStarted;

        var go = new GameObject("FND02B_Observer");
        go.AddComponent<FND02B_Observer>();
        Debug.Log("[FND02B] observer created; baselineGameStarted=" + FND02B_Observer.BaselineGameStarted +
                  "; frame=" + Time.frameCount);
    }
#endif
}
