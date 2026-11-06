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
using static QaFixtures;
using static QaJson;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

/// <summary>
/// SPEC-01 运行期探针（Play 模式）。
/// 只读生产状态；测试自身唯一写入面为 Docs 证据、SessionState 退出码、
/// 以及运行期内存夹具（SPEC01_Target_*，结束前销毁）。
/// 断言：Locomotion.AllPlayersMoveParam → Regression.FND02A → Attack.DualPathUnified →
///       Clock.MeleeNormalizedWindow → Clock.RangedOneShotPerCycle →
///       Animation.PostCombatAndHit → UI.DamageNumberScale → UI.SpawnAnchor。
/// 新 API（MeleeCombat.attackTrigger/hitStateName、DamageNumber 共享画布/池/尺寸字段）
/// 一律通过反射读取，保证 red 阶段（产品未实现）可编译且只产生 product_fail。
/// 不使用 Warp、不写玩家坐标、不直接 SetDestination 冒充指令。
/// </summary>
public class SPEC01_ProbeRunner : MonoBehaviour
{
    public const string ScenePath = QaScene.ScenePath;
    public const string EnemyPrefabGuid = QaScene.EnemyPrefabGuid;
    public const string ControllerPath = "Assets/_Project/Art/Animations/PlayerBase.controller";
    public const string FinishedKey = "SPEC01.Finished";
    public const string ExitCodeKey = "SPEC01.ExitCode";

    // 场景敌人的实际位置即基线（不再写死坐标）：接近点 = 敌人实际位置 + 朝骑士方向的长(起点距离 - 5m)。
    const float ApproachGain = 5f;
    const float ApproachMinStandOff = 2f;

    // 伤害飘字规格常量（断言期望值，与 DamageNumber 的默认常量对齐）
    const float UiBaselineScale = 0.006f;
    const float UiMinMultiplier = 1.5f;
    const float UiMaxMultiplier = 5f;
    const float UiMinDamage = 5f;
    const float UiMaxDamage = 60f;
    const int UiPoolCapacity = 32;
    const int UiStressCount = 40;
    const string UiOverlayNameFallback = "DamageNumberOverlay";

    static SPEC01_Evidence _ev;
    static bool _finished;
    static string _exitReason = "";
    static FieldInfo _firedField;
    static FieldInfo _rangedTimerField;
    static MethodInfo _resolveHoverWithOut;

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
    Vector3 _approachDest;
    float _approachPlanStandOff = -1f;
    int _enemyCount;

    SPEC01_Observer _observer;
    readonly List<PlayerProbe> _players = new List<PlayerProbe>();
    GameObject _target;
    UnitStats _targetStats;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    IEnumerator Start()
    {
        int parsedRun;
        string envRun = Environment.GetEnvironmentVariable("SPEC01_RUN");
        if (int.TryParse(envRun, out parsedRun)) _run = parsedRun;
        string envMode = Environment.GetEnvironmentVariable("SPEC01_MODE");
        _mode = string.IsNullOrEmpty(envMode) ? "green" : envMode.Trim().ToLowerInvariant();
        if (_mode == "red") _run = 0;
        else if (_run <= 0) _run = 1;

        _seed = 20261105 + _run;
        UnityEngine.Random.InitState(_seed);

        _ev = new SPEC01_Evidence(_run, _mode);
        _deadline = Time.realtimeSinceStartup + 300f;

        Debug.Log("[SPEC01] runner start; mode=" + _mode + "; run=" + _run +
                  "; seed=" + _seed + "; unity=" + Application.unityVersion +
                  "; evidenceDir=" + _ev.EvidenceDir + "; head=" + QaScene.GetHead("SPEC01.Head"));

        float waited = 0f;
        while (SceneManager.GetActiveScene().path != ScenePath && waited < 25f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        Debug.Log("[SPEC01] active scene=" + SceneManager.GetActiveScene().path +
                  "; waited=" + F(waited) + "s");

        float obsWaited = 0f;
        while (SPEC01_Observer.Instance == null && obsWaited < 10f)
        {
            obsWaited += Time.unscaledDeltaTime;
            yield return null;
        }
        _observer = SPEC01_Observer.Instance;
        if (_observer == null)
        {
            _ev.InfraFailure("Startup.Observer", "Startup.Observer",
                "运行期观察者未被创建（RuntimeInitializeOnLoadMethod 未执行或 SessionState 未置位）", "");
            Finish();
            yield break;
        }
        Debug.Log("[SPEC01] observer ready; baselineGameStarted=" + SPEC01_Observer.BaselineGameStarted);

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
        yield return RunSafe("UI.DamageNumberScale", AssertDamageNumber());
        yield return RunSafe("UI.SpawnAnchor", AssertSpawnAnchor(_ev));
        yield return RunSafe("Skill.CastSmoke", AssertSkillCastSmoke());
        yield return RunSafe("Skill.InputWired", AssertSkillInputWired());
        yield return RunSafe("Skill.CursorHover", AssertCursorHover());
        yield return RunSafe("Skill.TargetPick", AssertTargetPick());

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
                Debug.LogError("[SPEC01] assertion " + id + " threw: " + e);
                _ev.InfraFailure(id, id, e.Message, e.StackTrace);
                yield break;
            }
            yield return cur;
        }
    }

    // ============================================================
    // 1. Locomotion.AllPlayersMoveParam（1D / SpeedZ / 阈值 0,4）
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
            float peakSpeedZ = 0f;
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

            yield return new WaitForSeconds(1.2f);
            float finalSpeed = p.brain.agent.velocity.magnitude;
            float finalSpeedZ = p.animator.GetFloat("SpeedZ");

            bool playerPass = arrived && peakSpeed >= 2.0f && Mathf.Abs(peakSpeedZ) >= 2.0f &&
                              stateDuringMove == Locomotion && Mathf.Abs(finalSpeedZ) <= 0.15f &&
                              finalSpeed <= 0.15f && grounded;
            allMove &= playerPass;

            playerRows.Add(Dict(
                "name", p.name,
                "destination", V3(dest),
                "arrived", arrived,
                "durationSeconds", elapsed,
                "traveled", traveled,
                "maxSingleFrameDisplacement", maxStep,
                "peakAgentSpeed", peakSpeed,
                "peakAnimatorSpeedZ", peakSpeedZ,
                "stateDuringMove", stateDuringMove,
                "finalAgentSpeed", finalSpeed,
                "finalAnimatorSpeedZ", finalSpeedZ,
                "groundedDuringMove", grounded,
                "pass", playerPass));
        }

        bool stateNameExact = false;
        bool blend1D = false;
        bool usesSpeedZ = false;
        bool thresholdsOk = false;
        bool layer0Only = false;
        int layerCount = -1;
        string blendParameter = "none";
        var thresholds = new List<float>();
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
                    blend1D = bt.blendType == BlendTreeType.Simple1D;
                    blendParameter = bt.blendParameter;
                    usesSpeedZ = bt.blendParameter == "SpeedZ";
                    var children = bt.children;
                    for (int i = 0; i < children.Length; i++) thresholds.Add(children[i].threshold);
                    thresholdsOk = children.Length == 2 &&
                                   Mathf.Abs(children[0].threshold - 0f) < 0.001f &&
                                   Mathf.Abs(children[1].threshold - 4f) < 0.001f;
                }
            }
        }
#endif
        bool controllerPass = stateNameExact && blend1D && usesSpeedZ && thresholdsOk && layer0Only;
        data["controller"] = Dict(
            "stateNameExact", stateNameExact,
            "blendTreeIs1D", blend1D,
            "blendParameter", blendParameter,
            "thresholds", thresholds,
            "layerCount", layerCount,
            "layer0Only", layer0Only,
            "note", "SPEC-01 口径：Locomotion 混合树 1D，参数 SpeedZ，阈值 0(Idle)/4(Running)");
        data["players"] = playerRows;

        bool pass = allPlayers && allMove && controllerPass;
        _ev.Record("Locomotion.AllPlayersMoveParam", "Locomotion.AllPlayersMoveParam",
            "三角色经生产移动命令移动时处于 Locomotion，SpeedZ 随实际速度非零变化，停稳归零；控制器 Locomotion 状态名精确、混合树为 1D、参数 SpeedZ、阈值 0/4",
            "playersPass=" + allMove + "; controllerPass=" + controllerPass +
            "; stateNameExact=" + stateNameExact + "; blend1D=" + blend1D +
            "; blendParameter=" + blendParameter + "; thresholdsOk=" + thresholdsOk,
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
    // 2. Regression.FND02A（场景实际位置基线 + 相对接近）
    // ============================================================
    IEnumerator AssertRegressionFND02A()
    {
        _gameStarted = GameFlow.GameStarted;
        _enemy = FindRealEnemy();
        _enemyFound = _enemy != null;
        _enemySourceGuid = QaScene.GetEnemySourceGuid(ScenePath, "SPEC01.EnemySourceGuid");
        _enemyCount = CountSceneEnemies();

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
            // 基线全部来自场景实际位置：起点 = 骑士当前站位，终点 = 朝敌人方向缩短 ApproachGain 米。
            Vector3 start = knight.tx.position;
            Vector3 enemyPos = _enemyStartPos;
            Vector3 away = start - enemyPos;
            away.y = 0f;
            Vector3 dir = away.sqrMagnitude > 0.0001f ? away.normalized : Vector3.back;

            _approachStartDist = XZ(start, enemyPos);
            float gain = Mathf.Min(ApproachGain, Mathf.Max(0.5f, _approachStartDist - ApproachMinStandOff));
            _approachPlanStandOff = Mathf.Max(ApproachMinStandOff, _approachStartDist - gain);

            Vector3 dest = enemyPos + dir * _approachPlanStandOff;
            dest.y = start.y;
            NavMeshHit destHit;
            if (NavMesh.SamplePosition(dest, out destHit, 2.0f, NavMesh.AllAreas)) dest = destHit.position;
            _approachDest = dest;

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
                    // 停稳阶段的位移也计入行程，避免 end 距离在 settle 后继续缩短而 traveled 少记
                    Vector3 pos = knight.tx.position;
                    float step = Vector3.Distance(prev, pos);
                    if (step > _approachMaxStep) _approachMaxStep = step;
                    _approachTraveled += step;
                    prev = pos;
                }
            }
            _approachEndDist = XZ(knight.tx.position, enemyPos);
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

        _target = CreateTarget("SPEC01_Target_Combat", new Vector3(0f, 0f, -4f), false);
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
            yield return QaWait.WaitState(p.animator, Locomotion, 4.0f);

            var collector = new HitCollector();
            collector.Subscribe(targetHealth, p.health != null ? p.health.GetInstanceID() : 0);
            p.lastFired = GetFired(p.ranged);

            p.brain.enabled = false;
            p.brain.CommandAttack(_target.transform);
            p.brain.enabled = true;

            string expectedState = p.sword != null ? AttackMelee : AttackRelease;
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
                              "expectedState", expectedState,
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
                              "expectedState", expectedState,
                              "attackSpeedParam", p.animator.GetFloat("AttackSpeed"),
                              "attackSpeedStats", p.attackSpeed);
                }
            }
            collector.Unsubscribe();

            bool evOk = ev != null && (string)ev["state"] == expectedState;
            designatedPass &= evOk;
            if (ev == null) ev = Dict("path", "CommandAttack", "player", p.name, "event", "missing",
                                      "state", "none", "expectedState", expectedState);
            ev["pass"] = evOk;
            designatedRows.Add(ev);

            p.brain.CommandAttack(null);
            var c2 = p.brain.combat as ICharacterCombat;
            if (c2 != null) c2.CancelAttack();
            yield return QaWait.WaitState(p.animator, Locomotion, 4.0f);
        }

        // ---- 自动攻击路径（Aggro 自动索敌） ----
        foreach (var p in _players)
        {
            p.brain.CommandAttack(null);
            var c = p.brain.combat as ICharacterCombat;
            if (c != null) c.CancelAttack();
        }
        yield return QaWait.WaitAllStates(_players.Select(p => p.animator), Locomotion, 5.0f);

        var autoCollector = new HitCollector();
        autoCollector.Subscribe(targetHealth, 0);
        foreach (var p in _players)
        {
            p.drawEntries = 0;
            p.releaseEntries = 0;
            p.meleeEntries = 0;
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

        // ---- 触发器名核验（MeleeCombat / RangedCombat 序列化字段） ----
        var meleePlayer = _players.FirstOrDefault(p => p.sword != null);
        string swordTrigger = meleePlayer != null ? ReadStringMember(meleePlayer.sword, "attackTrigger") : "<no-sword>";
        string swordHitState = meleePlayer != null ? ReadStringMember(meleePlayer.sword, "hitStateName") : "<no-sword>";
        bool swordFieldsOk = swordTrigger == AttackMelee && swordHitState == AttackMelee;

        var rangedRows = new List<object>();
        bool rangedTriggerOk = true;
        foreach (var p in _players.Where(x => x.ranged != null))
        {
            bool ok = p.ranged.attackTrigger == "AttackRange";
            rangedTriggerOk &= ok;
            rangedRows.Add(Dict("player", p.name, "attackTrigger", p.ranged.attackTrigger,
                                "expected", "AttackRange", "pass", ok));
        }

        var autoRows = new List<object>();
        bool autoPass = true;
        foreach (var p in _players)
        {
            string expectedState = p.sword != null ? AttackMelee : AttackRelease;
            Dictionary<string, object> ev = null;
            if (p.sword != null && p.hitEvents.Count > 0) ev = p.hitEvents[0];
            else if (p.ranged != null && p.fireEvents.Count > 0) ev = p.fireEvents[0];

            bool evOk = ev != null && (string)ev["state"] == expectedState;
            autoPass &= evOk;
            autoRows.Add(Dict("path", "AggroAuto", "player", p.name,
                              "event", p.sword != null ? "hit" : "fire",
                              "state", ev != null ? ev["state"] : "missing",
                              "expectedState", expectedState,
                              "normalizedTime", ev != null ? ev["normalizedTime"] : (object)-1f,
                              "attackSpeedParam", p.animator != null ? (object)p.animator.GetFloat("AttackSpeed") : -1f,
                              "attackSpeedStats", p.attackSpeed,
                              "pass", evOk));
        }

        bool dualPass = designatedPass && autoPass && swordFieldsOk && rangedTriggerOk;
        _ev.Record("Attack.DualPathUnified", "Attack.DualPathUnified",
            "CommandAttack 与 Aggro 自动索敌两条路径：骑士命中瞬间处于 AttackMelee（单段，状态名=触发器名），弓/法发射瞬间处于 Attack_Release；远程触发器为 AttackRange；不存在 Locomotion 出伤/出射",
            "designatedPass=" + designatedPass + "; autoPass=" + autoPass +
            "; swordFieldsOk=" + swordFieldsOk + "; rangedTriggerOk=" + rangedTriggerOk +
            "; swordTrigger=" + swordTrigger + "; swordHitState=" + swordHitState,
            dualPass, dualPass ? "pass" : "product_fail", "production",
            Dict("designatedPath", designatedRows, "autoPath", autoRows,
                 "swordTriggerField", swordTrigger,
                 "swordHitStateField", swordHitState,
                 "rangedTriggers", rangedRows,
                 "fixture", "SPEC01_Target_Combat（运行期内存 UnitHealth，高 HP，无 AI）",
                 "note", "指定攻击通过 CharacterBrain.CommandAttack（与 SelectionManager 点敌同一生产方法）；自动攻击通过 CharacterBrain.Update → UnitBrain.AttackLoop → ICharacterCombat.AutoAttack"));

        // ---- Clock.MeleeNormalizedWindow ----
        var melee = meleePlayer;
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
                bool ok = state == AttackMelee && nt >= activeStart - 0.06f && nt <= activeEnd + 0.06f;
                if (ok) inWindow++;
                meleeHits.Add(Dict("time", h["time"], "state", state, "normalizedTime", nt,
                                   "inWindow", ok, "amount", h["amount"]));
            }
            float attackSpeedParam = melee.animator != null ? melee.animator.GetFloat("AttackSpeed") : -1f;
            bool speedOk = Mathf.Abs(attackSpeedParam - melee.attackSpeed) < 0.01f;
            bool countOk = melee.hitEvents.Count >= 2 && inWindow == melee.hitEvents.Count &&
                           Mathf.Abs(melee.hitEvents.Count - melee.meleeEntries) <= 1;
            bool fieldOk = swordTrigger == AttackMelee && swordHitState == AttackMelee;
            meleePass = countOk && speedOk && fieldOk;
            _ev.Record("Clock.MeleeNormalizedWindow", "Clock.MeleeNormalizedWindow",
                "骑士命中由 layer 0 AttackMelee.normalizedTime 的 [activeStart, activeEnd] 窗口驱动；单周期恰 1 次；触发器/命中状态名均为 AttackMelee；AttackSpeed 参数等于 stats.attackSpeed",
                "hits=" + melee.hitEvents.Count + "; inWindow=" + inWindow +
                "; meleeEntries=" + melee.meleeEntries +
                "; window=[" + F(activeStart) + "," + F(activeEnd) + "]" +
                "; attackTrigger=" + swordTrigger + "; hitStateName=" + swordHitState +
                "; attackSpeedParam=" + F(attackSpeedParam) + "; stats=" + F(melee.attackSpeed),
                meleePass, meleePass ? "pass" : "product_fail", "production",
                Dict("windowStart", activeStart, "windowEnd", activeEnd,
                     "hits", meleeHits, "meleeEntries", melee.meleeEntries,
                     "attackTrigger", swordTrigger, "hitStateName", swordHitState,
                     "attackSpeedParam", attackSpeedParam, "attackSpeedStats", melee.attackSpeed,
                     "effectiveLayer", melee.sword.attackLayer,
                     "singleTargetMode", melee.sword.attackMode.ToString()));
        }
        else
        {
            _ev.BlockedAssertion("Clock.MeleeNormalizedWindow", "Clock.MeleeNormalizedWindow",
                "骑士近战时钟", "未找到 MeleeCombat 玩家", null);
        }

        // ---- Clock.RangedOneShotPerCycle ----
        var rangedCycleRows = new List<object>();
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
            rangedCycleRows.Add(Dict(
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
            Dict("players", rangedCycleRows,
                 "note", "观测窗 " + F(duration) + "s；以 Attack_Draw 进入次数 vs 发射次数、发射瞬间状态/归一化时间、最小发射间隔与首次出手延迟共同判定"));
    }

    // ============================================================
    // 6. Animation.PostCombatAndHit（不变）
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
    // 7. UI.DamageNumberScale（共享 Overlay 画布 + 伤害分档字号 + 池复用）
    // ============================================================
    IEnumerator AssertDamageNumber()
    {
        var cf = FindFirstObjectByType<CombatFeedback>();
        if (cf == null)
        {
            _ev.BlockedAssertion("UI.DamageNumberScale", "UI.DamageNumberScale",
                "伤害飘字共享 Overlay 画布与伤害分档", "场景中未找到 CombatFeedback", null);
            yield break;
        }

        var fixture = CreateTarget("SPEC01_Target_UI", new Vector3(0f, 0f, -4f), true);
        if (fixture == null)
        {
            _ev.InfraFailure("UI.DamageNumberScale", "UI.DamageNumberScale", "飘字夹具创建失败", "");
            yield break;
        }
        var fh = fixture.GetComponent<UnitHealth>();
        Vector3 spawn = fh.transform.position + Vector3.up * 1.5f;

        // 隔离：暂停玩家大脑并临时关闭其它单位的飘字，避免自动战斗在采样窗内产生额外飘字
        foreach (var p in _players) if (p.brain != null) p.brain.enabled = false;
        var suppressed = new List<UnitHealth>();
        foreach (var h in FindObjectsByType<UnitHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (h == null || h == fh || !h.showDamageNumber) continue;
            suppressed.Add(h);
            h.showDamageNumber = false;
        }

        // 清掉前序断言可能残留的飘字，避免污染采样
        yield return QaWait.WaitNoNumbers(2.5f);

        // ---- 小伤害采样（期望 min 钳制 1.5x） ----
        var beforeSmall = ActiveNumberIds();
        fh.ApplyDamage(UiMinDamage, spawn, null);
        DamageNumber small = null;
        float waitSmall = 0f;
        while (waitSmall < 1.2f && small == null)
        {
            yield return null;
            waitSmall += Time.deltaTime;
            small = FirstNewNumber(beforeSmall);
        }
        float smallMult = small != null ? InstFloat(small, "AppliedMultiplier") : -1f;
        float smallFont = small != null ? InstFloat(small, "AppliedFontSize") : -1f;
        string smallElement = InstString(small, "Element");
        Text smallText = small != null ? small.GetComponentInChildren<Text>(true) : null;
        bool smallRaycastOff = smallText != null && !smallText.raycastTarget;
        bool smallHasGroup = small != null && small.GetComponent<CanvasGroup>() != null;

        yield return QaWait.WaitNoNumbers(2.5f);

        // ---- 大伤害采样（期望 max 钳制 5x） ----
        var beforeLarge = ActiveNumberIds();
        fh.ApplyDamage(200f, spawn, null);
        DamageNumber large = null;
        float waitLarge = 0f;
        while (waitLarge < 1.2f && large == null)
        {
            yield return null;
            waitLarge += Time.deltaTime;
            large = FirstNewNumber(beforeLarge);
        }
        float largeMult = large != null ? InstFloat(large, "AppliedMultiplier") : -1f;
        float largeFont = large != null ? InstFloat(large, "AppliedFontSize") : -1f;
        string largeElement = InstString(large, "Element");

        yield return QaWait.WaitNoNumbers(2.5f);

        // ---- 连续 Show 压力：池复用、不新增画布 ----
        int overlayBeforeStress = CountOverlayCanvases();
        int totalBeforeStress = CountNumbers(true);
        for (int i = 0; i < UiStressCount; i++)
            fh.ApplyDamage(10f + i, spawn, null);
        yield return new WaitForSeconds(0.5f);
        int activeAfterStress = CountNumbers(false);
        int totalAfterStress = CountNumbers(true);
        int overlayAfterStress = CountOverlayCanvases();

        // ---- 共享画布唯一性与元素映射 ----
        string overlayName = StaticString(typeof(DamageNumber), "OverlayName");
        if (string.IsNullOrEmpty(overlayName)) overlayName = UiOverlayNameFallback;
        Canvas shared = StaticMember(typeof(DamageNumber), "SharedCanvas") as Canvas;
        bool sharedOk = shared != null && shared.renderMode == RenderMode.ScreenSpaceOverlay;
        int namedCanvasCount = CountCanvasesByName(overlayName);
        bool uniqueCanvasOk = namedCanvasCount == 1;
        bool canvasStable = overlayAfterStress == overlayBeforeStress;
        bool poolOk = activeAfterStress <= UiPoolCapacity && totalAfterStress <= UiPoolCapacity;

        float baseFontSize = StaticFloat(typeof(DamageNumber), "BaseFontSize");
        bool smallClampOk = small != null && Mathf.Abs(smallMult - UiMinMultiplier) <= 0.01f;
        bool largeClampOk = large != null && Mathf.Abs(largeMult - UiMaxMultiplier) <= 0.01f;
        bool fontOk = baseFontSize > 0f &&
                      small != null && large != null &&
                      Mathf.Abs(smallFont - baseFontSize * UiMinMultiplier) <= 0.51f &&
                      Mathf.Abs(largeFont - baseFontSize * UiMaxMultiplier) <= 0.51f;
        bool raycastOk = smallRaycastOff;
        bool groupOk = smallHasGroup;
        bool elementOk = smallElement == "Physical" && largeElement == "Physical";

        bool pass = sharedOk && uniqueCanvasOk && canvasStable && poolOk &&
                    smallClampOk && largeClampOk && fontOk && raycastOk && groupOk && elementOk;

        // 恢复隔离
        foreach (var h in suppressed) if (h != null) h.showDamageNumber = true;
        foreach (var p in _players) if (p.brain != null) p.brain.enabled = true;
        if (fixture != null) Destroy(fixture);

        _ev.Record("UI.DamageNumberScale", "UI.DamageNumberScale",
            "单个共享 Screen Space-Overlay 画布（唯一、不随伤害新增）；字号随伤害分档（5→1.5x、200→5x 钳制）；Text raycastTarget=false；连续 40 次 Show 活跃/总实例 ≤ 32",
            "sharedOk=" + sharedOk + "; namedCanvasCount=" + namedCanvasCount +
            "; overlayBeforeStress=" + overlayBeforeStress + "; overlayAfterStress=" + overlayAfterStress +
            "; smallMult=" + F(smallMult) + "; largeMult=" + F(largeMult) +
            "; smallFont=" + F(smallFont) + "; largeFont=" + F(largeFont) +
            "; baseFontSize=" + F(baseFontSize) +
            "; activeAfterStress=" + activeAfterStress + "; totalAfterStress=" + totalAfterStress +
            "; raycastOk=" + raycastOk + "; element=" + smallElement + "/" + largeElement,
            pass, pass ? "pass" : "product_fail", "production",
            Dict("overlayName", overlayName,
                 "sharedCanvasRenderMode", shared != null ? shared.renderMode.ToString() : "missing",
                 "namedCanvasCount", namedCanvasCount,
                 "overlayCanvasBeforeStress", overlayBeforeStress,
                 "overlayCanvasAfterStress", overlayAfterStress,
                 "totalInstancesBeforeStress", totalBeforeStress,
                 "activeAfterStress", activeAfterStress,
                 "totalAfterStress", totalAfterStress,
                 "poolCapacity", UiPoolCapacity,
                 "smallAmount", UiMinDamage,
                 "smallAppliedMultiplier", smallMult,
                 "smallAppliedFontSize", smallFont,
                 "largeAmount", 200f,
                 "largeAppliedMultiplier", largeMult,
                 "largeAppliedFontSize", largeFont,
                 "baseFontSize", baseFontSize,
                 "minMultiplier", UiMinMultiplier,
                 "maxMultiplier", UiMaxMultiplier,
                 "minDamage", UiMinDamage,
                 "maxDamage", UiMaxDamage,
                 "baselineScaleReference", UiBaselineScale,
                 "raycastTargetFalse", smallRaycastOff,
                 "canvasGroupPresent", smallHasGroup,
                 "smallElement", smallElement,
                 "largeElement", largeElement,
                 "sharedOk", sharedOk, "uniqueCanvasOk", uniqueCanvasOk,
                 "canvasStable", canvasStable, "poolOk", poolOk,
                 "smallClampOk", smallClampOk, "largeClampOk", largeClampOk,
                 "fontOk", fontOk, "raycastOk", raycastOk, "groupOk", groupOk, "elementOk", elementOk,
                 "fixture", "SPEC01_Target_UI（运行期内存 UnitHealth，高 HP，showDamageNumber=true）",
                 "entry", "UnitHealth.ApplyDamage → DamageDealtEvent → CombatFeedback（生产路径）"));
    }

    // ============================================================
    // 8. UI.SpawnAnchor（出生点锚点：飘字屏幕位置对齐目标胸口投影）
    // ============================================================
    IEnumerator AssertSpawnAnchor(IQaSink sink)
    {
        const string AssertionId = "UI.SpawnAnchor";
        const float TolerancePx = 50f;

        // 与 DamageNumber.ResolveCamera 同口径：Camera.main 优先，否则 allCameras[0]
        Camera cam = Camera.main;
        string camSource = "Camera.main";
        if (cam == null)
        {
            var cams = Camera.allCameras;
            if (cams.Length > 0) { cam = cams[0]; camSource = "Camera.allCameras[0]"; }
        }
        if (cam == null)
        {
            sink.BlockedAssertion(AssertionId, AssertionId,
                "飘字出生瞬间屏幕位置与目标胸口相机投影距离 ≤ " + F(TolerancePx) + "px",
                "场景中未找到可用相机（Camera.main 与 Camera.allCameras 均为空）", null);
            yield break;
        }

        var fixture = CreateTarget("SPEC01_Target_Anchor", new Vector3(0f, 0f, -4f), true);
        if (fixture == null)
        {
            sink.InfraFailure(AssertionId, AssertionId, "飘字锚点夹具创建失败", "");
            yield break;
        }
        var fh = fixture.GetComponent<UnitHealth>();
        Vector3 targetPos = fh.transform.position;
        Vector3 chestPoint = targetPos + Vector3.up * DamageNumber.ChestHeight;

        // 隔离：暂停玩家大脑并临时关闭其它单位的飘字，避免采样窗内出现额外飘字
        foreach (var p in _players) if (p.brain != null) p.brain.enabled = false;
        var suppressed = new List<UnitHealth>();
        foreach (var h in FindObjectsByType<UnitHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (h == null || h == fh || !h.showDamageNumber) continue;
            suppressed.Add(h);
            h.showDamageNumber = false;
        }

        yield return QaWait.WaitNoNumbers(2.5f);

        var before = ActiveNumberIds();
        fh.ApplyDamage(25f, targetPos, null);

        DamageNumber dn = null;
        float waited = 0f;
        while (dn == null && waited < 2.0f)
        {
            dn = FirstNewNumber(before);
            if (dn != null) break;
            yield return null;
            waited += Time.deltaTime;
        }

        Vector2 actualScreen = Vector2.zero;
        Vector2 expectedScreen = Vector2.zero;
        float distancePx = -1f;
        string anchorMin = "n/a";
        string anchorMax = "n/a";
        string pivot = "n/a";
        string canvasSize = "n/a";
        float ageAtSample = -1f;
        if (dn != null)
        {
            var rt = (RectTransform)dn.transform;
            actualScreen = RectTransformUtility.WorldToScreenPoint(null, rt.position);
            Vector3 ep = cam.WorldToScreenPoint(chestPoint);
            expectedScreen = new Vector2(ep.x, ep.y);
            distancePx = Vector2.Distance(actualScreen, expectedScreen);
            anchorMin = F(rt.anchorMin.x) + "," + F(rt.anchorMin.y);
            anchorMax = F(rt.anchorMax.x) + "," + F(rt.anchorMax.y);
            pivot = F(rt.pivot.x) + "," + F(rt.pivot.y);
            ageAtSample = dn.Age;
            var canvasRt = dn.transform.parent as RectTransform;
            if (canvasRt != null) canvasSize = F(canvasRt.rect.width) + "x" + F(canvasRt.rect.height);
        }

        bool pass = dn != null && distancePx >= 0f && distancePx <= TolerancePx;

        // 恢复隔离并销毁夹具
        foreach (var h in suppressed) if (h != null) h.showDamageNumber = true;
        foreach (var p in _players) if (p.brain != null) p.brain.enabled = true;
        if (fixture != null) Destroy(fixture);
        DestroyTarget();

        sink.Record(AssertionId, AssertionId,
            "飘字出生瞬间的屏幕位置与目标胸口（target.position + up*ChestHeight）相机投影距离 ≤ " + F(TolerancePx) + "px",
            "distancePx=" + F(distancePx) +
            "; actual=(" + F(actualScreen.x) + "," + F(actualScreen.y) + ")" +
            "; expected=(" + F(expectedScreen.x) + "," + F(expectedScreen.y) + ")" +
            "; anchorMin=(" + anchorMin + "); anchorMax=(" + anchorMax + "); pivot=(" + pivot + ")",
            pass, pass ? "pass" : "product_fail", "production",
            Dict("entry", "UnitHealth.ApplyDamage(25f) → DamageDealtEvent → CombatFeedback → DamageNumber.Show（生产路径）",
                 "cameraSource", camSource,
                 "targetPosition", V3(targetPos),
                 "chestHeight", DamageNumber.ChestHeight,
                 "worldChestPoint", V3(chestPoint),
                 "actualScreen", Dict("x", actualScreen.x, "y", actualScreen.y),
                 "expectedScreen", Dict("x", expectedScreen.x, "y", expectedScreen.y),
                 "distancePx", distancePx,
                 "tolerancePx", TolerancePx,
                 "anchorMin", anchorMin,
                 "anchorMax", anchorMax,
                 "pivot", pivot,
                 "canvasSize", canvasSize,
                 "screenWidth", Screen.width,
                 "screenHeight", Screen.height,
                 "ageAtSample", ageAtSample,
                 "sampleWaitSeconds", waited,
                 "instanceFound", dn != null,
                 "fixture", "SPEC01_Target_Anchor（运行期内存 UnitHealth，showDamageNumber=true）"));
    }

    // ============================================================
    // 技能施法回归（SPEC-DIAG-SKILL）
    // ============================================================
    static readonly Vector3 SkillFixturePos = new Vector3(80f, 0f, 80f);

    /// <summary>
    /// Q/W/E/R 四槽直接经生产 SkillSystem.Cast 施放（不按键盘）。
    /// 夹具在运行期实例化 Knight_Unit 并销毁，不写场景。
    /// 口径（SPEC-DIAG-SKILL-2）：①失败施放不扣蓝/不写冷却 ②Q 行为不变（防 Bash 回归）
    /// ③W 效果类型为 HeavySlashEffect ④E/R 复核"空槽=设计内静默"契约。四条全过于算 pass。
    /// </summary>
    IEnumerator AssertSkillCastSmoke()
    {
        SkillCastFixture fx = null;
        try { fx = SkillCastFixture.Create(SkillFixturePos, true); }
        catch (Exception e)
        {
            _ev.InfraFailure("Skill.CastSmoke", "Skill.CastSmoke", "夹具创建抛异常: " + e.Message, e.StackTrace);
            yield break;
        }
        if (fx == null || !string.IsNullOrEmpty(fx.Error))
        {
            string err = fx != null ? fx.Error : "null";
            if (fx != null) fx.Dispose();
            _ev.InfraFailure("Skill.CastSmoke", "Skill.CastSmoke", "夹具创建失败: " + err, "");
            yield break;
        }

        var failed = new CastProbeResult();
        var q = new CastProbeResult();
        var w = new CastProbeResult();
        var e2 = new CastProbeResult();
        var r2 = new CastProbeResult();
        bool failedNoManaPass = false, qPass = false, wPass = false, emptyPass = false;
        float prepSeconds = -1f;
        string prepState = "none";
        string fixturePath = fx.CasterPath();

        try
        {
            yield return fx.Prepare(6f);
            prepSeconds = fx.PrepareSeconds;
            prepState = fx.PrepareState;
            if (!fx.PrepareLocomotion)
            {
                _ev.BlockedAssertion("Skill.CastSmoke", "Skill.CastSmoke",
                    Dict("expected", "失败施放不扣蓝 + Q/W 效果链执行 + E/R 空槽静默"),
                    "夹具未能进入 Locomotion（preState=" + prepState + "）",
                    Dict("prepareState", prepState, "preparePhase", fx.PreparePhase));
                yield break;
            }

            // ① 失败施放不扣蓝：Q 需要目标，传 null → BashEffect.CanCast 失败。
            //    若实现先扣蓝再 CanCast，这里会白扣 20 蓝，并连带污染后续 Q 的蓝量读数。
            yield return fx.Cast(0, null, null, 0f, failed);
            failedNoManaPass = !failed.castReturn &&
                               Mathf.Abs(failed.manaDelta) < 0.0001f &&
                               Mathf.Abs(failed.cdRemaining) < 0.0001f &&
                               string.IsNullOrEmpty(failed.exception);

            // ② Q 行为不变（防技能槽回归）
            yield return fx.Cast(0, fx.Target != null ? fx.Target.transform : null, "Q_1", 0.5f, q);
            bool qType = q.effectType == "BashEffect";
            bool qMana = Mathf.Abs(q.manaDelta - 20f) < 0.01f;
            bool qCd = q.cdRemaining > 0f && Mathf.Abs(q.cdRemaining - 6f) <= 0.05f;
            bool qDmg = q.damageObserved && Mathf.Abs(q.damageApplied - q.expectedDamage) < 0.01f;
            bool qAnim = q.animEntered && q.animEntrySeconds >= 0f && q.animEntrySeconds <= 0.5f;
            qPass = q.castReturn && qType && qMana && qCd && qDmg && qAnim;

            // ③ W 类型正确（W_Slash_Effect 必须解析为 HeavySlashEffect，而不是同文件里的 BashEffect）
            yield return fx.EnsureLocomotion(3f);
            yield return fx.Cast(1, null, "W_1", 0.5f, w);
            bool wType = w.effectType == "HeavySlashEffect";
            bool wMana = Mathf.Abs(w.manaDelta - 30f) < 0.01f;
            bool wCd = w.cdRemaining > 0f;
            bool wTimer = w.swordTimer >= 0f;
            bool wAnim = w.animEntered && w.animEntrySeconds >= 0f && w.animEntrySeconds <= 0.5f;
            wPass = w.castReturn && wType && wMana && wCd && wTimer && wAnim;

            // ④ E/R 空槽契约
            yield return fx.Cast(2, null, null, 0f, e2);
            yield return fx.Cast(3, null, null, 0f, r2);
            emptyPass = !e2.castReturn && !r2.castReturn &&
                        Mathf.Abs(e2.manaDelta) < 0.0001f && Mathf.Abs(r2.manaDelta) < 0.0001f &&
                        string.IsNullOrEmpty(e2.exception) && string.IsNullOrEmpty(r2.exception);
        }
        finally
        {
            if (fx != null) fx.Dispose();
        }

        bool pass = failedNoManaPass && qPass && wPass && emptyPass;
        var slotNames = new List<object>
        {
            failed.slotName, q.slotName, w.slotName, e2.slotName, r2.slotName
        };

        _ev.Record("Skill.CastSmoke", "Skill.CastSmoke",
            Dict("失败施放", "Cast(0,null) false；manaDelta=0；CdRemaining=0；无异常",
                 "Q", "Cast(0,enemy) true；effectType=BashEffect；蓝-20；CD>0≈6；命中帧后靶掉血=GetRawDamage；≤0.5s 进入 Q_1",
                 "W", "Cast(1,null) true；effectType=HeavySlashEffect；蓝-30；CD>0；_timer>=0；≤0.5s 进入 W_1",
                 "E/R", "Cast(2/3) false；蓝不变；无异常"),
            Dict("failedCastReturn", failed.castReturn,
                 "manaDeltaFailed", failed.manaDelta,
                 "cdFailed", failed.cdRemaining,
                 "failedException", failed.exception,
                 "qEffectType", q.effectType,
                 "wEffectType", w.effectType,
                 "qPass", qPass, "wPass", wPass, "emptyPass", emptyPass,
                 "failedNoManaPass", failedNoManaPass),
            pass, pass ? "pass" : "product_fail", "production",
            Dict("failed", failed.Data(),
                 "Q", q.Data(), "W", w.Data(), "E/R", new List<object> { e2.Data(), r2.Data() },
                 "manaBefore0", q.manaBefore,
                 "manaAfter0", q.manaAfter,
                 "manaBefore1", w.manaBefore,
                 "manaAfter1", w.manaAfter,
                 "cdRemaining0", q.cdRemaining,
                 "cdRemaining1", w.cdRemaining,
                 "targetHpBefore", q.targetHpBefore,
                 "targetHpAfter", q.targetHpAfter,
                 "expectedDamage", q.expectedDamage,
                 "animState0", q.animStateAfter,
                 "animState1", w.animStateAfter,
                 "slotNames", slotNames,
                 "fixturePath", fixturePath,
                 "fixturePos", V3(SkillFixturePos),
                 "prepareState", prepState,
                 "prepareSeconds", prepSeconds,
                 "failedCastReturn", failed.castReturn,
                 "manaDeltaFailed", failed.manaDelta,
                 "cdFailed", failed.cdRemaining,
                 "qEffectType", q.effectType,
                 "wEffectType", w.effectType,
                 "qPass", qPass,
                 "wPass", wPass,
                 "emptyPass", emptyPass,
                 "failedNoManaPass", failedNoManaPass,
                 "note", "全部数据来自生产 SkillSystem.Cast；E/R 空槽=设计内静默，非缺陷（不修）"));
        yield return null;
    }

    /// <summary>
    /// 输入层看门狗：DungeonWorld 运行期必须恰好有 1 个 SkillInputController，
    /// 且 indicator 指向场景 SkillIndicator、skillKeys = Q/W/E/R。
    /// 当前场景缺组件 → 如实记 product_fail（不得改写为 blocked/observed）。
    /// </summary>
    IEnumerator AssertSkillInputWired()
    {
        yield return null;
        var ctrls = FindObjectsByType<SkillInputController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        var inds = FindObjectsByType<SkillIndicator>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        bool refOk = false;
        bool keysOk = false;
        string keys = "none";
        string indicatorName = "none";
        int controllerCount = ctrls.Length;
        if (controllerCount == 1)
        {
            var c = ctrls[0];
            if (c.indicator != null)
            {
                indicatorName = c.indicator.name;
                refOk = System.Array.IndexOf(inds, c.indicator) >= 0;
            }
            if (c.skillKeys != null)
            {
                var parts = new List<string>();
                for (int i = 0; i < c.skillKeys.Length; i++) parts.Add(c.skillKeys[i].ToString());
                keys = string.Join(",", parts.ToArray());
                keysOk = c.skillKeys.Length == 4 &&
                         c.skillKeys[0] == KeyCode.Q && c.skillKeys[1] == KeyCode.W &&
                         c.skillKeys[2] == KeyCode.E && c.skillKeys[3] == KeyCode.R;
            }
        }

        bool pass = controllerCount == 1 && refOk && keysOk;
        _ev.Record("Skill.InputWired", "Skill.InputWired",
            Dict("controllerCount", 1, "indicatorPointsToSceneIndicator", true, "skillKeys", "Q,W,E,R"),
            Dict("controllerCount", controllerCount, "indicatorPointsToSceneIndicator", refOk,
                 "indicator", indicatorName, "skillKeys", keys, "indicatorCount", inds.Length),
            pass, pass ? "pass" : "product_fail", "production",
            Dict("controllerCount", controllerCount,
                 "indicatorCount", inds.Length,
                 "indicator", indicatorName,
                 "indicatorPointsToSceneIndicator", refOk,
                 "skillKeys", keys,
                 "skillKeysOk", keysOk,
                 "note", "DungeonWorld 场景缺失该组件时本条必为 product_fail；这是资产缺陷的永久看门狗，不得删除"));
    }

    // ============================================================
    // 9. Skill.CursorHover（瞄准态 hover 分类：合成射线 → 攻击指针）
    // ============================================================
    /// <summary>反射取 ResolveHover(Ray, out UnitHealth, out float)：产品未实现时返回 null，红阶段仍可编译。</summary>
    static MethodInfo ResolveHoverWithOut
    {
        get
        {
            if (_resolveHoverWithOut == null)
                _resolveHoverWithOut = typeof(CursorManager).GetMethod("ResolveHover",
                    new[] { typeof(Ray), typeof(UnitHealth).MakeByRefType(), typeof(float).MakeByRefType() });
            return _resolveHoverWithOut;
        }
    }

    /// <summary>
    /// 口径：瞄准态下指针分类不再强制 Pointer，而是与常态共用 ResolveHover。
    /// 这里用合成射线（相机 → 敌人胸口）直接验证分类函数，不使用 WorldToScreenPoint（敌人可能不在视锥）。
    /// 瞄准态早退的移除属视觉行为，由 AC-4 人工目检覆盖；本断言只认纯函数返回值。
    /// </summary>
    IEnumerator AssertCursorHover()
    {
        const string AssertionId = "Skill.CursorHover";
        Camera cam = Camera.main;
        if (cam == null)
        {
            _ev.InfraFailure(AssertionId, AssertionId, "Camera.main 为空，无法构造合成射线", "");
            yield break;
        }
        var manager = CursorManager.instance;
        if (manager == null)
        {
            _ev.InfraFailure(AssertionId, AssertionId, "CursorManager.instance 为空（场景未装配光标管理器）", "");
            yield break;
        }

        var fixture = CreateTarget("SPEC01_Target_Cursor",
            cam.transform.position + cam.transform.forward * 6f, false);
        if (fixture == null)
        {
            _ev.InfraFailure(AssertionId, AssertionId, "瞄准指针夹具创建失败", "");
            yield break;
        }
        var fh = fixture.GetComponent<UnitHealth>();
        Vector3 chest = fixture.transform.position + Vector3.up * DamageNumber.ChestHeight;
        Physics.SyncTransforms();   // 新建夹具的碰撞体需同步后才会被本帧射线看到
        Ray ray = new Ray(cam.transform.position, (chest - cam.transform.position).normalized);

        int fixtureHits = 0;
        float fixtureDistance = -1f;
        foreach (var h in Physics.RaycastAll(ray, 300f))
        {
            if (h.collider == null || h.collider.GetComponentInParent<UnitHealth>() != fh) continue;
            fixtureHits++;
            if (fixtureDistance < 0f) fixtureDistance = h.distance;
        }

        MethodInfo method = ResolveHoverWithOut;
        bool apiPresent = method != null;
        object result = null;
        UnitHealth hoverEnemy = null;
        float distance = -1f;
        if (apiPresent)
        {
            object[] args = { ray, null, 0f };
            try
            {
                result = method.Invoke(manager, args);
                hoverEnemy = args[1] as UnitHealth;
                if (args[2] is float f) distance = f;
            }
            catch (Exception e)
            {
                _ev.InfraFailure(AssertionId, AssertionId,
                    "ResolveHover 反射调用抛异常: " + e.Message, e.StackTrace);
                Destroy(fixture);
                DestroyTarget();
                yield break;
            }
        }

        string hover = result != null ? result.ToString() : "<missing>";
        bool enemyIsFixture = hoverEnemy != null && hoverEnemy == fh;
        bool pass = apiPresent && hover == "Attack" && enemyIsFixture;

        _ev.Record(AssertionId, AssertionId,
            "给定瞄准态与一条从相机指向敌人胸口的合成射线，ResolveHover(ray) 返回 Hover.Attack 且命中敌人就是该敌人",
            "apiPresent=" + apiPresent + "; hover=" + hover +
            "; enemy=" + (hoverEnemy != null ? hoverEnemy.name : "none") +
            "; enemyIsFixture=" + enemyIsFixture +
            "; distance=" + F(distance) + "; fixtureHits=" + fixtureHits,
            pass, pass ? "pass" : "product_fail", "production",
            Dict("entry", "CursorManager.ResolveHover(Ray, out UnitHealth, out float)（瞄准态 hover 分类的纯查询入口）",
                 "apiPresent", apiPresent,
                 "hover", hover,
                 "expectedHover", "Attack",
                 "enemyName", hoverEnemy != null ? hoverEnemy.name : "none",
                 "enemyIsFixture", enemyIsFixture,
                 "enemyTeam", hoverEnemy != null ? hoverEnemy.team.ToString() : "none",
                 "enemyDead", hoverEnemy != null && hoverEnemy.dead,
                 "distance", distance,
                 "fixtureHits", fixtureHits,
                 "fixtureDistance", fixtureDistance,
                 "fixturePosition", V3(fixture.transform.position),
                 "chestHeight", DamageNumber.ChestHeight,
                 "chestPoint", V3(chest),
                 "cameraPosition", V3(cam.transform.position),
                 "rayOrigin", V3(ray.origin),
                 "rayDirection", V3(ray.direction),
                 "raycastDistance", 300f,
                 "clickableLayers", manager.clickableLayers.value,
                 "isTargeting", SkillInputController.IsTargeting,
                 "cameraSource", "Camera.main",
                 "note", "合成射线（相机 → 敌人胸口），不使用 WorldToScreenPoint；瞄准态早退的移除由 AC-4 人工目检覆盖"));

        Destroy(fixture);
        DestroyTarget();
        yield return null;
    }

    // ============================================================
    // 10. Skill.TargetPick（点击选敌：同一射线口径 → 敌人 UnitHealth）
    // ============================================================
    /// <summary>
    /// 口径：点击选敌与指针分类共用同一射线口径，PickEnemyFromRay 直接返回命中敌人的 UnitHealth。
    /// 瞄准态在 batchmode 下无法稳定保持（无键盘输入、每帧会因空槽取消），
    /// 故只验证纯查询函数本身；PendingRange 如实记录（非瞄准态为 0）。
    /// </summary>
    IEnumerator AssertTargetPick()
    {
        const string AssertionId = "Skill.TargetPick";
        Camera cam = Camera.main;
        if (cam == null)
        {
            _ev.InfraFailure(AssertionId, AssertionId, "Camera.main 为空，无法构造合成射线", "");
            yield break;
        }
        var input = SkillInputController.instance;
        if (input == null)
        {
            _ev.InfraFailure(AssertionId, AssertionId, "SkillInputController.instance 为空（场景未装配技能输入）", "");
            yield break;
        }

        var fixture = CreateTarget("SPEC01_Target_Pick",
            cam.transform.position + cam.transform.forward * 6f + cam.transform.right * 1.5f, false);
        if (fixture == null)
        {
            _ev.InfraFailure(AssertionId, AssertionId, "点击选敌夹具创建失败", "");
            yield break;
        }
        var fh = fixture.GetComponent<UnitHealth>();
        Vector3 chest = fixture.transform.position + Vector3.up * DamageNumber.ChestHeight;
        Physics.SyncTransforms();
        Ray ray = new Ray(cam.transform.position, (chest - cam.transform.position).normalized);
        float rayDistance = Vector3.Distance(cam.transform.position, chest);

        MethodInfo method = typeof(SkillInputController).GetMethod("PickEnemyFromRay", new[] { typeof(Ray) });
        bool apiPresent = method != null;
        UnitHealth picked = null;
        Exception invokeError = null;
        if (apiPresent)
        {
            try { picked = method.Invoke(input, new object[] { ray }) as UnitHealth; }
            catch (Exception e) { invokeError = e; }
        }
        if (invokeError != null)
        {
            _ev.InfraFailure(AssertionId, AssertionId,
                "PickEnemyFromRay 反射调用抛异常: " + invokeError.Message, invokeError.StackTrace);
            Destroy(fixture);
            DestroyTarget();
            yield break;
        }

        object rangeValue = ReadMember(input, "PendingRange");
        float pendingRange = rangeValue is float pf ? pf : -1f;
        bool inRange = pendingRange > 0f && rayDistance <= pendingRange;
        bool pickedIsFixture = picked != null && picked == fh;
        bool pass = apiPresent && pickedIsFixture && picked.team == Team.Monster && !picked.dead;

        _ev.Record(AssertionId, AssertionId,
            "给定同一条指向敌人胸口的合成射线，PickEnemyFromRay(ray) 返回该敌人的 UnitHealth（team=Monster、存活）",
            "apiPresent=" + apiPresent +
            "; picked=" + (picked != null ? picked.name : "none") +
            "; pickedIsFixture=" + pickedIsFixture +
            "; team=" + (picked != null ? picked.team.ToString() : "none") +
            "; distance=" + F(rayDistance) + "; pendingRange=" + F(pendingRange) + "; inRange=" + inRange,
            pass, pass ? "pass" : "product_fail", "production",
            Dict("entry", "SkillInputController.PickEnemyFromRay(Ray)（点击选敌与指针分类共用射线口径）",
                 "apiPresent", apiPresent,
                 "pickedName", picked != null ? picked.name : "none",
                 "pickedIsFixture", pickedIsFixture,
                 "pickedTeam", picked != null ? picked.team.ToString() : "none",
                 "pickedDead", picked != null && picked.dead,
                 "distance", rayDistance,
                 "pendingRange", pendingRange,
                 "pendingRangePresent", rangeValue != null,
                 "inRange", inRange,
                 "fixturePosition", V3(fixture.transform.position),
                 "chestPoint", V3(chest),
                 "cameraPosition", V3(cam.transform.position),
                 "rayDirection", V3(ray.direction),
                 "isTargeting", SkillInputController.IsTargeting,
                 "screenPathTested", false,
                 "note", "不使用 WorldToScreenPoint（敌人可能不在视锥）；屏幕坐标点击路径由 AC-4 人工目检覆盖"));

        Destroy(fixture);
        DestroyTarget();
        yield return null;
    }

    // ============================================================
    // 回归汇总
    // ============================================================
    void RecordRegressionFinal()
    {
        int errors = _observer != null ? _observer.ErrorCount : -1;
        var errList = _observer != null ? _observer.ErrorList() : new List<object>();
        // 位置基线 = 场景实际位置（动态）：不再与写死坐标比较，只要求确实读到了场景敌人。
        bool posOk = _enemyFound && _approachStartDist > 0f;
        bool guidOk = _enemySourceGuid == EnemyPrefabGuid;
        float plannedGain = _approachStartDist - _approachPlanStandOff;
        bool approachOk = _approachArrived &&
                          _approachPlanStandOff >= ApproachMinStandOff - 0.01f &&
                          _approachEndDist >= ApproachMinStandOff - 0.5f &&
                          _approachEndDist <= _approachStartDist - Mathf.Max(0.5f, plannedGain * 0.5f) &&
                          _approachTraveled >= Mathf.Max(0.5f, _approachStartDist - _approachEndDist - 0.5f) &&
                          _approachMaxStep <= 0.6f &&
                          _approachPathStatus == "PathComplete" && _approachCorners > 1;
        bool pass = _gameStarted && _enemyFound && posOk && guidOk && _navOk && approachOk && errors == 0;

        _ev.Record("Regression.FND02A", "Regression.FND02A",
            "FND-02A 自动开局、场景实际遭遇位置为基线、真实寻路相对接近、零新增 Console 错误/异常继续通过",
            "gameStarted=" + _gameStarted + "; enemyFound=" + _enemyFound + "; enemyCount=" + _enemyCount +
            "; posOk=" + posOk + "; guidOk=" + guidOk + "; navOk=" + _navOk + "; approachOk=" + approachOk +
            "; consoleErrors=" + errors,
            pass, pass ? "pass" : "product_fail", "production",
            Dict("gameStarted", _gameStarted,
                 "enemyFound", _enemyFound,
                 "enemyCount", _enemyCount,
                 "enemyName", _enemy != null ? _enemy.name : "none",
                 "enemyStartPosition", V3(_enemyStartPos),
                 "positionBaseline", "scene-actual（动态读取场景敌人位置，不再写死坐标）",
                 "enemySourceGuid", _enemySourceGuid,
                 "expectedPrefabGuid", EnemyPrefabGuid,
                 "navMeshOk", _navOk,
                 "approachArrived", _approachArrived,
                 "approachGainTarget", ApproachGain,
                 "approachPlannedStandOff", _approachPlanStandOff,
                 "approachDestination", V3(_approachDest),
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
                     "head", QaScene.GetHead("SPEC01.Head"),
                     "enemySourceGuid", _enemySourceGuid,
                     "enemyPosition", V3(_enemyStartPos),
                     "fixtureNote", "SPEC01_Target_Combat / SPEC01_Target_UI 为运行期内存 UnitHealth，结束前销毁；不写入场景",
                     "testWroteGameStarted", false));
        }

        int code;
        if (_ev == null || _ev.Infra > 0) code = 3;
        else if (_ev.Fail > 0 || _ev.Blocked > 0) code = 2;
        else code = 0;

        Debug.Log("[SPEC01] finish; mode=" + _mode + "; run=" + _run + "; exit=" + code +
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
        catch (Exception e) { Debug.LogWarning("[SPEC01] exit code file write failed: " + e.Message); }

        if (SPEC01_Observer.Instance != null)
            Destroy(SPEC01_Observer.Instance.gameObject);
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
        foreach (var p in _players) Debug.Log("[SPEC01] player " + p.name + " at " + V3Text(p.home) +
                                              "; sword=" + (p.sword != null) + "; ranged=" + (p.ranged != null) +
                                              "; attackSpeed=" + p.attackSpeed);
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

    /// <summary>场景里 Skeleton_Warrior 数量（诊断字段：遭遇应恰 1 只）。</summary>
    static int CountSceneEnemies()
    {
        int n = 0;
        foreach (var h in FindObjectsByType<UnitHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (h == null || h.team != Team.Monster) continue;
            if (h.name.StartsWith("Skeleton_Warrior", StringComparison.Ordinal)) n++;
        }
        return n;
    }

    /// <summary>夹具薄包装：统一走 QaFixtures.CreateTarget，并保留本套件对 _targetStats 的销毁责任。</summary>
    GameObject CreateTarget(string name, Vector3 pos, bool showDamageNumber)
    {
        var go = QaFixtures.CreateTarget(name, pos, showDamageNumber);
        var uh = go != null ? go.GetComponent<UnitHealth>() : null;
        _targetStats = uh != null ? uh.stats : null;
        Debug.Log("[SPEC01] fixture created " + name + " at " + V3Text(pos) + "; showDamageNumber=" + showDamageNumber);
        return go;
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
            if (s == AttackMelee) p.meleeEntries++;
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

    // ---- 反射读取：新 API 在 red 阶段不存在时返回 <missing> / -1，不抛异常 ----
    static object ReadMember(object target, string name)
    {
        if (target == null) return null;
        var t = target.GetType();
        var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null) return f.GetValue(target);
        var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (p != null) return p.GetValue(target);
        return null;
    }

    static object StaticMember(Type t, string name)
    {
        if (t == null) return null;
        var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (f != null) return f.GetValue(null);
        var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        if (p != null) return p.GetValue(null);
        return null;
    }

    static float InstFloat(object target, string name)
    {
        object v = ReadMember(target, name);
        return v is float f ? f : -1f;
    }

    static float StaticFloat(Type t, string name)
    {
        object v = StaticMember(t, name);
        return v is float f ? f : -1f;
    }

    static string StaticString(Type t, string name)
    {
        return StaticMember(t, name) as string;
    }

    static string ReadStringMember(object target, string name)
    {
        object v = ReadMember(target, name);
        return v as string ?? "<missing>";
    }

    static string InstString(object target, string name)
    {
        object v = ReadMember(target, name);
        return v != null ? v.ToString() : "<missing>";
    }

    // ---- 飘字实例统计 ----
    static int CountNumbers(bool includeInactive)
    {
        var found = FindObjectsByType<DamageNumber>(
            includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        return found.Length;
    }

    static HashSet<int> ActiveNumberIds()
    {
        var set = new HashSet<int>();
        foreach (var d in FindObjectsByType<DamageNumber>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (d != null) set.Add(d.GetInstanceID());
        return set;
    }

    static DamageNumber FirstNewNumber(HashSet<int> before)
    {
        foreach (var d in FindObjectsByType<DamageNumber>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (d != null && !before.Contains(d.GetInstanceID())) return d;
        return null;
    }

    static int CountOverlayCanvases()
    {
        int n = 0;
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c != null && c.renderMode == RenderMode.ScreenSpaceOverlay) n++;
        return n;
    }

    static int CountCanvasesByName(string name)
    {
        int n = 0;
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c != null && c.gameObject.name == name) n++;
        return n;
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
}

/// <summary>
/// 首帧观察者：由 RuntimeInitializeOnLoadMethod(AfterSceneLoad) 创建（早于所有 Start），
/// 只读 Console，不写任何产品状态。
/// </summary>
public class SPEC01_Observer : MonoBehaviour
{
    public static SPEC01_Observer Instance;
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

/// <summary>进入 Play 时的自举钩子：仅在 SPEC01.Active 置位时创建观察者。</summary>
public static class SPEC01_BootHook
{
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AfterSceneLoadHook()
    {
        if (!SessionState.GetBool("SPEC01.Active", false)) return;
        if (SPEC01_Observer.Instance != null) return;

        SPEC01_Observer.BaselineGameStarted = GameFlow.GameStarted;

        var go = new GameObject("SPEC01_Observer");
        go.AddComponent<SPEC01_Observer>();
        Debug.Log("[SPEC01] observer created; baselineGameStarted=" + SPEC01_Observer.BaselineGameStarted +
                  "; frame=" + Time.frameCount);
    }
#endif
}
