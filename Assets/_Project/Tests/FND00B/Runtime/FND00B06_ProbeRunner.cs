using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Dungeon.Core;
using Dungeon.Gameplay.Combat;
using Dungeon.Gameplay.Player;
using Dungeon.Gameplay.Units;
using Dungeon.App;

/// <summary>
/// FND-00B-06 运行期探针（Play 模式）。
/// 只读取生产对象 + 在内存中建立夹具，不保存、不改生产资产。
/// 顺序：第 4 项（普攻）→ 第 6 项（角色动画）。
/// 攻击输入使用生产方法等价输入：SelectionManager.SingleSelect / SelectEnemy + CharacterBrain.CommandAttack
/// （与 SelectionManager.HandleClick 内同一组生产方法），不逐帧调用执行器、不直接写 HP。
/// </summary>
public class FND00B06_ProbeRunner : MonoBehaviour
{
    const string ScenePath = "Assets/Scenes/DungeonWorld.unity";
    const string KnightPrefabPath = "Assets/_Project/Prefabs/Knight_Unit.prefab";
    const string EnemyPrefabPath = "Assets/_Project/Prefabs/Skeleton_Warrior.prefab";
    const string FixturePrefix = "FND00B06_";

    const string FinishedKey = "FND00B06.Finished";
    const string ExitCodeKey = "FND00B06.ExitCode";

    const float ApproachTimeoutSeconds = 18f;
    const float StartTimeoutSeconds = 4f;
    const float DamageWindowSeconds = 8f;
    const float IdleWaitSeconds = 1.6f;
    const float AnimWaitSeconds = 2f;
    const float FixtureOffsetMeters = 13f;

    static readonly string[] KnownStates =
    {
        " Locomotion", "Locomotion", "AttackMelee", "Attack_Draw", "Attack_Release", "Hit_A", "Downed", "Rise", "Interact", "Q_1", "W_1", "E_1", "R_1"
    };
    static readonly string[] AttackStates = { "AttackMelee" };
    static readonly string[] RequiredStates = { "Locomotion", "Attack_Draw", "Hit_A", "Downed", "Rise" };
    static readonly string[] RequiredParams = { "SpeedX", "SpeedZ", "Grounded", "AttackSpeed", "AttackRange", "Hit", "Die", "Revive" };

    static readonly string[] AttackAssertionIds =
    {
        "Attack.Fixture", "Attack.CommandAccepted", "Attack.Approach", "Attack.Starts",
        "Attack.Repeats", "Attack.DamageResolved", "Attack.TargetDeath"
    };

    FND00B06_Evidence _ev;
    int _run = 1;
    int _seed = 20261104;
    float _deadline;
    bool _finished;
    string _exitReason = "";

    // ---- 第 4 项产物（供第 6 项复用）----
    UnitHealth _player;
    CharacterBrain _brain;
    Selectable _playerSel;
    MeleeCombat _sword;
    Animator _pAnim;
    NavMeshAgent _pAgent;

    UnitHealth _target;
    GameObject _targetGo;
    int _targetDeathEvents;
    int _hitEventCount;
    float _targetHpTracked;
    readonly List<object> _hitEvents = new List<object>();
    float _targetSpawnSeconds = -1f;

    float _attackWindowStart;
    float _approachStartDist = -1f;
    float _approachEndDist = -1f;
    float _approachSeconds = -1f;
    float _approachConfiguredRange = -1f;
    float _approachMaxStep;
    bool _approachTeleported;
    string _approachEndReason = "";
    readonly List<object> _moveSamples = new List<object>();
    readonly List<object> _attackSamples = new List<object>();
    int _swingCount;
    readonly List<object> _swingTimes = new List<object>();
    float _firstAttackAt = -1f;
    string _firstAttackState = null;
    bool _firstAttackTargetOk;

    float _commandAt = -1f;
    readonly List<object> _stateTimeline = new List<object>();
    string _lastStateName = null;
    readonly List<UnitHealth> _othersDisabled = new List<UnitHealth>();
    readonly List<bool> _othersBrainEnabled = new List<bool>();
    FieldInfo _swordCurLenField;

    readonly List<object> _idleSamples = new List<object>();
    bool _idleCaptured;
    string _idlePlayerName = "none";

    // ---- 第 6 项产物 ----
    readonly Dictionary<int, string> _stateNames = new Dictionary<int, string>();
    readonly List<object> _stateList = new List<object>();
    readonly List<object> _paramDump = new List<object>();
    readonly List<string> _paramNames = new List<string>();
    string _controllerName = "";
    string _overrideBaseName = "";
    int _layerCount = -1;
    int _runtimeLayerCount = -1;
    string _animatorOwner = "";
    UnitHealth _animFixtureHealth;
    GameObject _animFixtureGo;
    DownedRescue _animFixtureDowned;
    int _animFixtureHitEvents;
    int _animFixtureDeathEvents;
    float _animFixturePreHitHp = -1f;
    float _animFixturePostHitHp = -1f;
    float _animFixtureHitAmount = -1f;

    FieldInfo _swordTimerField;
    FieldInfo _swordTargetField;
    FieldInfo _orderedTargetField;
    FieldInfo _enemySelectedField;

    // ---- 断言判定快照：编排层在测点写入，断言方法只读（不引入引导状态依赖）----
    bool _playerAgentOk;
    string _playerAgentWhy = "";
    bool _monsterBrainDisabled;
    bool _aFixturePass;
    bool _aCommandPass;
    bool _aApproachPass;
    float _prevSwordTimer = -1f;
    float _targetDeathAt = -1f;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    IEnumerator Start()
    {
        int parsed;
        string env = Environment.GetEnvironmentVariable("FND00B06_RUN");
        if (int.TryParse(env, out parsed) && parsed > 0) _run = parsed;

        _seed = 20261104 + _run;
        UnityEngine.Random.InitState(_seed);

        _ev = new FND00B06_Evidence(_run);
        _deadline = Time.realtimeSinceStartup + 300f;

        Debug.Log("[FND00B06] runner start; run=" + _run +
                  "; seed=" + _seed +
                  "; unity=" + Application.unityVersion +
                  "; evidenceDir=" + _ev.EvidenceDir);

        float waited = 0f;
        while (SceneManager.GetActiveScene().path != ScenePath && waited < 20f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        Debug.Log("[FND00B06] active scene=" + SceneManager.GetActiveScene().path +
                  "; loaded=" + SceneManager.GetActiveScene().isLoaded +
                  "; waited=" + waited.ToString("0.00") + "s");

        yield return null;
        yield return null;

        Debug.Log("[FND00B06] env timeScale=" + Time.timeScale +
                  "; paused=" + (PauseManager.instance != null && PauseManager.instance.IsPaused) +
                  "; gameStarted=" + GameFlow.GameStarted +
                  "; gameFlowInstance=" + (GameFlow.instance != null) +
                  "; isWiped=" + (GameFlow.instance != null && GameFlow.instance.IsWiped) +
                  "; level=" + GameData.level);

        // 第 6 项 Idle：在最干净的「尚未下达任何指令」状态取样（后续战斗会改变动画状态机）
        yield return SampleInitialIdle();

        yield return GroupAttack();
        yield return GroupAnimation();

        Finish();
    }

    void Update()
    {
        if (_finished) return;
        if (_ev == null) return;
        if (Time.realtimeSinceStartup <= _deadline) return;
        _ev.InfraFailure("all", "Runner.Watchdog", "运行超时（300 秒）被强制结束", "");
        _exitReason = "watchdog timeout";
        Finish();
    }

    void Finish()
    {
        if (_finished) return;
        _finished = true;

        if (_ev != null)
            _ev.WriteSummary(ScenePath, _seed, string.IsNullOrEmpty(_exitReason) ? "completed" : _exitReason,
                             QaJson.Dict("qaItems", "4,6"));

        int code;
        if (_ev == null || _ev.Infra > 0) code = 3;
        else if (_ev.Fail > 0 || _ev.Blocked > 0) code = 2;
        else code = 0;

        Debug.Log("[FND00B06] finish; exit=" + code +
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
            string exitPath = _ev != null ? _ev.ExitCodePath() : null;
            if (exitPath != null)
                System.IO.File.WriteAllText(exitPath, code.ToString() + Environment.NewLine,
                    new System.Text.UTF8Encoding(false));
        }
        catch (Exception e) { Debug.LogWarning("[FND00B06] exit code file write failed: " + e.Message); }
        Time.timeScale = 1f;
        EditorApplication.isPlaying = false;
#else
        Time.timeScale = 1f;
#endif
    }

    // ============================================================
    // 第 6 项 Idle 取样（战斗前）
    // ============================================================
    IEnumerator SampleInitialIdle()
    {
        UnitHealth p = FindObjectsByType<UnitHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(h => h != null && !h.name.StartsWith(FixturePrefix, StringComparison.Ordinal) && h.team == Team.Player && !h.dead)
            .FirstOrDefault(h => FindSword(h) != null && h.GetComponent<CharacterBrain>() != null);

        Animator a = null;
        NavMeshAgent pAgent = null;
        if (p != null)
        {
            var b = p.GetComponent<CharacterBrain>();
            a = b.unitAnimator != null ? b.unitAnimator : p.GetComponentInChildren<Animator>(true);
            pAgent = p.GetComponent<NavMeshAgent>();
            _idlePlayerName = p.name;
        }

        if (a == null)
        {
            _idleCaptured = false;
            Debug.LogWarning("[FND00B06] initial idle sampling skipped: no player animator");
            yield break;
        }

        for (int i = 0; i < 6; i++)
        {
            _idleSamples.Add(AnimSample(a, "idle_pre_command", pAgent));
            yield return null;
        }
        _idleCaptured = true;
        Debug.Log("[FND00B06] initial idle captured; player=" + _idlePlayerName +
                  "; state=" + (string)((Dictionary<string, object>)_idleSamples[_idleSamples.Count - 1])["stateName"]);
    }

    // ============================================================
    // 第 4 项：普攻
    // ============================================================
    IEnumerator GroupAttack()
    {
        Debug.Log("[FND00B06] === QA item 4: Attack ===");
        IQaSink sink = _ev;

        UnitHealth[] players;
        int monsterCount;
        try
        {
            UnitHealth[] all = FindObjectsByType<UnitHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            players = all.Where(h => h != null && !h.name.StartsWith(FixturePrefix, StringComparison.Ordinal) && h.team == Team.Player).ToArray();
            monsterCount = all.Count(h => h != null && !h.name.StartsWith(FixturePrefix, StringComparison.Ordinal) && h.team == Team.Monster);
        }
        catch (Exception e)
        {
            _ev.InfraFailure("4", "Attack.Fixture", e.Message, e.ToString());
            yield break;
        }

        var playerScan = players.Select(h => (object)QaJson.Dict(
            "id", h.GetInstanceID(),
            "name", h.name,
            "path", GetPath(h.transform),
            "team", h.team.ToString(),
            "hp", h.hp,
            "maxHp", h.maxHp,
            "dead", h.dead,
            "position", QaJson.V3(h.transform.position),
            "hasSelectable", h.GetComponentInChildren<Selectable>(true) != null,
            "hasCharacterBrain", h.GetComponent<CharacterBrain>() != null,
            "hasMeleeCombat", FindSword(h) != null,
            "hasRangedCombat", h.GetComponentInChildren<RangedCombat>(true) != null,
            "agentPresent", h.GetComponent<NavMeshAgent>() != null,
            "agentEnabled", h.GetComponent<NavMeshAgent>() != null && h.GetComponent<NavMeshAgent>().enabled,
            "agentOnNavMesh", h.GetComponent<NavMeshAgent>() != null && h.GetComponent<NavMeshAgent>().isOnNavMesh,
            "agentPosition", h.GetComponent<NavMeshAgent>() != null ? QaJson.V3(h.GetComponent<NavMeshAgent>().nextPosition) : null)).ToList();

        _player = players.FirstOrDefault(h => h != null && !h.dead &&
                                              FindSword(h) != null &&
                                              h.GetComponent<CharacterBrain>() != null);
        if (_player == null)
        {
            _ev.BlockedAssertion("4", "Attack.Fixture",
                QaJson.Dict("requirement", "场景存在可下达攻击指令的近战玩家单位"),
                "未找到「存活 + CharacterBrain + MeleeCombat」的玩家单位",
                QaJson.Dict("playerCount", players.Length, "playerScan", playerScan,
                     "sceneMonsterCount", monsterCount, "scene", SceneManager.GetActiveScene().path));
            for (int i = 1; i < AttackAssertionIds.Length; i++)
                _ev.BlockedAssertion("4", AttackAssertionIds[i],
                    QaJson.Dict("requirement", "见工单 §4"),
                    "无近战玩家单位，普攻链路无起点", QaJson.Dict("playerCount", players.Length));
            yield break;
        }

        _brain = _player.GetComponent<CharacterBrain>();
        _sword = FindSword(_player);
        _playerSel = _player.GetComponentInChildren<Selectable>(true);
        _pAgent = _player.GetComponent<NavMeshAgent>();
        _pAnim = _brain.unitAnimator != null ? _brain.unitAnimator : _player.GetComponentInChildren<Animator>(true);
        _swordTimerField = PrivField(typeof(MeleeCombat), "_timer");
        _swordTargetField = PrivField(typeof(MeleeCombat), "_target");
        _swordCurLenField = PrivField(typeof(MeleeCombat), "_curLen");
        _orderedTargetField = PrivField(typeof(CharacterBrain), "_orderedTarget");
        _enemySelectedField = PrivField(typeof(SelectionManager), "_enemySelected");

        _playerAgentOk = _pAgent != null && _pAgent.enabled && _pAgent.isOnNavMesh;
        _playerAgentWhy = _pAgent == null ? "缺 NavMeshAgent" :
            (!_pAgent.enabled ? "agent.enabled=false" :
            (!_pAgent.isOnNavMesh ? "agent.isOnNavMesh=false" : "ok"));

        // 隔离其他玩家单位：避免其「坚守自动攻击」命中夹具目标污染本项观测（仅运行时禁用，测后恢复）
        _othersDisabled.Clear();
        _othersBrainEnabled.Clear();
        foreach (var other in players)
        {
            if (other == null || other == _player) continue;
            var ob = other.GetComponent<CharacterBrain>();
            if (ob == null) continue;
            _othersBrainEnabled.Add(ob.enabled);
            _othersDisabled.Add(other);
            ob.enabled = false;
        }

        // ---- 敌人目标夹具（内存实例，既有预制体）----
        GameObject enemyPrefab = null;
        string enemyLoadError = null;
#if UNITY_EDITOR
        try { enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath); }
        catch (Exception e) { enemyLoadError = e.GetType().Name + ": " + e.Message; }
#else
        enemyLoadError = "非编辑器环境无法加载预制体";
#endif

        if (enemyPrefab == null)
        {
            _ev.InfraFailure("4", "Attack.Fixture", "无法加载 " + EnemyPrefabPath + "; " + (enemyLoadError ?? ""), "");
            for (int i = 1; i < AttackAssertionIds.Length; i++)
                _ev.BlockedAssertion("4", AttackAssertionIds[i], QaJson.Dict("requirement", "见工单 §4"),
                    "敌人夹具预制体不可用", QaJson.Dict("prefab", EnemyPrefabPath, "error", enemyLoadError));
            yield break;
        }

        Vector3 spawnPos;
        string spawnMethod;
        List<object> spawnTrials;
        bool spawnPlaced = TryPickSpawn(_player.transform.position, out spawnPos, out spawnMethod, out spawnTrials);

        if (!spawnPlaced)
        {
            _ev.BlockedAssertion("4", "Attack.Fixture",
                QaJson.Dict("requirement", "在玩家附近 NavMesh 上放置 1 个既有敌人预制体实例"),
                "附近未找到可通行 NavMesh 落点（4 个方向 / 7m、5.5m、4m）",
                QaJson.Dict("playerPosition", QaJson.V3(_player.transform.position), "spawnTrials", spawnTrials));
            for (int i = 1; i < AttackAssertionIds.Length; i++)
                _ev.BlockedAssertion("4", AttackAssertionIds[i], QaJson.Dict("requirement", "见工单 §4"),
                    "敌人夹具无法落位", QaJson.Dict("spawnTrials", spawnTrials));
            yield break;
        }

        try
        {
            float spawnT0 = Time.realtimeSinceStartup;
            Vector3 faceDir = Vector3.ProjectOnPlane(_player.transform.position - spawnPos, Vector3.up);
            if (faceDir.sqrMagnitude < 0.01f) faceDir = Vector3.forward;
            _targetGo = Instantiate(enemyPrefab, spawnPos, Quaternion.LookRotation(faceDir.normalized));
            _targetGo.name = FixturePrefix + "AttackTarget";
            _targetSpawnSeconds = Time.realtimeSinceStartup - spawnT0;
        }
        catch (Exception e)
        {
            _ev.InfraFailure("4", "Attack.Fixture", "敌人夹具实例化失败: " + e.Message, e.ToString());
            for (int i = 1; i < AttackAssertionIds.Length; i++)
                _ev.BlockedAssertion("4", AttackAssertionIds[i], QaJson.Dict("requirement", "见工单 §4"),
                    "敌人夹具实例化失败", QaJson.Dict("error", e.Message));
            yield break;
        }

        // 夹具目标关闭自主 AI，避免对手攻击/追击污染普攻观测（只改实例，不改生产资产）
        var monsterBrain = _targetGo.GetComponent<MonsterBrain>();
        _monsterBrainDisabled = false;
        if (monsterBrain != null && monsterBrain.enabled) { monsterBrain.enabled = false; _monsterBrainDisabled = true; }
        var wander = _targetGo.GetComponent<SkeletonWander>();
        if (wander != null && wander.enabled) wander.enabled = false;

        _target = _targetGo.GetComponent<UnitHealth>();
        if (_target == null)
        {
            _ev.InfraFailure("4", "Attack.Fixture", "敌人夹具缺少 UnitHealth", "");
            for (int i = 1; i < AttackAssertionIds.Length; i++)
                _ev.BlockedAssertion("4", AttackAssertionIds[i], QaJson.Dict("requirement", "见工单 §4"),
                    "敌人夹具缺少 UnitHealth", QaJson.Dict("prefab", EnemyPrefabPath));
            yield break;
        }

        yield return null;
        yield return null;

        _targetHpTracked = _target.hp;
        _targetDeathEvents = 0;
        _hitEventCount = 0;
        _target.OnHit += OnTargetHit;
        _target.OnDeath += OnTargetDeath;

        yield return AssertAttackFixture(sink, _player, _target, _pAgent, players.Length, monsterCount,
                                         playerScan, spawnMethod, spawnTrials);

        if (!_aFixturePass)
        {
            for (int i = 1; i < AttackAssertionIds.Length; i++)
                _ev.BlockedAssertion("4", AttackAssertionIds[i], QaJson.Dict("requirement", "见工单 §4"),
                    "夹具前置失败（玩家 Agent 不在 NavMesh 或目标无效）",
                    QaJson.Dict("playerAgentOk", _playerAgentOk, "agentReason", _playerAgentWhy));
            CleanupAttackFixture();
            yield break;
        }

        // ---- 真实指令：生产选择 + 生产命令（等价于点击敌人）----
        SelectionManager sm = SelectionManager.instance;
        bool selected = false, primaryOk = false, enemyMarked = false, orderedOk = false;
        string entryError = null;

        try
        {
            if (sm != null && _playerSel != null)
            {
                MethodInfo singleSelect = PrivMethod(typeof(SelectionManager), "SingleSelect");
                if (singleSelect != null) singleSelect.Invoke(sm, new object[] { _playerSel });
                selected = true;
            }
        }
        catch (Exception e)
        {
            entryError = e.GetType().Name + ": " + e.Message;
            Debug.LogError("[FND00B06] select player failed: " + e);
        }

        yield return null;
        primaryOk = sm != null && sm.Primary == _playerSel;

        _commandAt = Time.realtimeSinceStartup;
        TrackAnim(_pAnim, "pre_command");

        try
        {
            if (sm != null)
            {
                MethodInfo selectEnemy = PrivMethod(typeof(SelectionManager), "SelectEnemy");
                if (selectEnemy != null) selectEnemy.Invoke(sm, new object[] { _target });
                enemyMarked = _enemySelectedField != null && _enemySelectedField.GetValue(sm) as UnitHealth == _target;
            }

            _brain.CommandAttack(_target.transform);
            orderedOk = _orderedTargetField != null && _orderedTargetField.GetValue(_brain) as Transform == _target.transform;
        }
        catch (Exception e)
        {
            entryError = e.GetType().Name + ": " + e.Message;
            Debug.LogError("[FND00B06] command entry failed: " + e);
        }

        yield return AssertAttackCommandAccepted(sink, _playerSel, selected, primaryOk, enemyMarked, orderedOk,
                                                entryError, sm != null);

        if (!_aCommandPass)
        {
            for (int i = 2; i < AttackAssertionIds.Length; i++)
                _ev.BlockedAssertion("4", AttackAssertionIds[i], QaJson.Dict("requirement", "见工单 §4"),
                    "生产攻击命令入口未接受（详见 Attack.CommandAccepted）",
                    QaJson.Dict("error", entryError, "orderedTargetIsTarget", orderedOk));
            CleanupAttackFixture();
            yield break;
        }

        yield return AssertAttackApproach(sink, _player, _target, _brain);

        if (!_aApproachPass)
        {
            for (int i = 3; i < AttackAssertionIds.Length; i++)
                _ev.BlockedAssertion("4", AttackAssertionIds[i], QaJson.Dict("requirement", "见工单 §4"),
                    "未能通过生产移动链接敌（endReason=" + _approachEndReason + "）",
                    QaJson.Dict("endDistance", _approachEndDist, "attackRange", _approachConfiguredRange));
            CleanupAttackFixture();
            yield break;
        }

        // ---- 出招 / 持续攻击 / 伤害结算 ----
        yield return AssertAttackStarts(sink, _player, _target, _sword, _brain);
        yield return AssertAttackRepeats(sink, _target, _sword, _brain);
        yield return AssertAttackDamageResolved(sink, _target);
        yield return AssertAttackTargetDeath(sink, _target);

        CleanupAttackFixture();
        yield return null;
    }

    // ============================================================
    // 断言（AC-10 / R-8）：只经 IQaSink 出证据；夹具走参数，不读写引导状态
    // ============================================================
    IEnumerator AssertAttackFixture(IQaSink sink, UnitHealth player, UnitHealth target, NavMeshAgent playerAgent,
                                    int playerCount, int monsterCount, List<object> playerScan,
                                    string spawnMethod, List<object> spawnTrials)
    {
        NavMeshAgent targetAgent = target.GetComponent<NavMeshAgent>();
        bool monsterBrainPresent = _targetGo != null && _targetGo.GetComponent<MonsterBrain>() != null;
        var targetInfo = QaJson.Dict(
            "id", target.GetInstanceID(),
            "name", target.name,
            "path", GetPath(target.transform),
            "prefabSource", EnemyPrefabPath,
            "team", target.team.ToString(),
            "hp", target.hp,
            "maxHp", target.maxHp,
            "dead", target.dead,
            "position", QaJson.V3(target.transform.position),
            "distanceToPlayer", XZDistance(target.transform.position, player.transform.position),
            "hasSelectableComponent", target.GetComponentInChildren<Selectable>(true) != null,
            "agentPresent", targetAgent != null,
            "agentEnabled", targetAgent != null && targetAgent.enabled,
            "agentOnNavMesh", targetAgent != null && targetAgent.isOnNavMesh,
            "monsterBrainPresent", monsterBrainPresent,
            "monsterBrainDisabledForFixture", _monsterBrainDisabled,
            "fixtureTag", FixturePrefix);

        bool fixturePass = _playerAgentOk && target != null && !target.dead;
        _aFixturePass = fixturePass;
        sink.Record("4", "Attack.Fixture",
            QaJson.Dict(
                "requirement", "选中 1 名存活近战玩家；以既有普通敌人预制体在内存建立目标夹具；记录双方 ID/队伍/位置/生命/存活/可选中/Agent 导航状态",
                "playerEntity", "DungeonWorld 场景内玩家单位",
                "targetEntity", "既有敌人预制体内存实例（不写入场景）"),
            QaJson.Dict(
                "player", QaJson.Dict("id", player.GetInstanceID(), "name", player.name,
                               "hp", player.hp, "maxHp", player.maxHp, "dead", player.dead,
                               "agentOk", _playerAgentOk, "agentReason", _playerAgentWhy,
                               "agentPos", playerAgent != null ? QaJson.V3(playerAgent.transform.position) : null),
                "target", targetInfo,
                "sceneMonsterCount", monsterCount,
                "playerCount", playerCount),
            fixturePass, fixturePass ? "pass" : "blocked", "production",
            QaJson.Dict(
                "playerScan", playerScan,
                "spawnMethod", spawnMethod,
                "spawnTrials", spawnTrials,
                "spawnSeconds", _targetSpawnSeconds,
                "scene", SceneManager.GetActiveScene().path,
                "otherPlayersBrainIsolation", QaJson.Dict(
                    "count", _othersDisabled.Count,
                    "names", _othersDisabled.Select(h => h.name).ToList(),
                    "note", "仅运行时禁用其他玩家 CharacterBrain，避免其自动攻击夹具目标；测后恢复，不改资产"),
                "fixtureNote", "目标为内存夹具：Skeleton_Warrior 预制体实例，关闭其 MonsterBrain 以免反击/追击；场景真实怪物数为上值，不把夹具存在写成遭遇通过"));
        yield break;
    }

    IEnumerator AssertAttackCommandAccepted(IQaSink sink, Selectable playerSelectable, bool selected, bool primaryOk,
                                            bool enemyMarked, bool orderedOk, string entryError,
                                            bool selectionManagerPresent)
    {
        bool commandPass = selected && primaryOk && enemyMarked && orderedOk;
        _aCommandPass = commandPass;
        sink.Record("4", "Attack.CommandAccepted",
            QaJson.Dict(
                "requirement", "下发一次真实普攻命令，命令被接受且目标正确",
                "selectionEntry", "SelectionManager.SingleSelect(Selectable)（左键单选生产方法）",
                "attackCommandEntry", "SelectionManager.SelectEnemy(UnitHealth) + CharacterBrain.CommandAttack(Transform)",
                "chain", "SelectionManager.HandleClick → SelectEnemy + CharacterBrain.CommandAttack → CharacterBrain.Update → UnitBrain.AttackLoop → MeleeCombat.AutoAttack",
                "forbidden", "不得逐帧调用攻击执行器、不得直接扣血"),
            QaJson.Dict("playerSelected", selected, "primaryIsPlayer", primaryOk,
                 "enemyMarked", enemyMarked, "orderedTargetIsTarget", orderedOk,
                 "error", entryError),
            commandPass, commandPass ? "pass" : "blocked", "production",
            QaJson.Dict(
                "selectionManagerPresent", selectionManagerPresent,
                "playerSelectable", playerSelectable != null,
                "note", "batchmode 无真实鼠标射线输入，按工单允许使用生产选择/命令方法作自动等价输入"));
        yield break;
    }

    IEnumerator AssertAttackApproach(IQaSink sink, UnitHealth player, UnitHealth target, CharacterBrain brain)
    {
        _approachConfiguredRange = brain.AttackRange;
        _approachStartDist = XZDistance(player.transform.position, target.transform.position);
        float approachT0 = Time.realtimeSinceStartup;
        bool inRange = false;
        Vector3 lastPos = player.transform.position;
        _approachMaxStep = 0f;
        _approachTeleported = false;

        while (Time.realtimeSinceStartup - approachT0 < ApproachTimeoutSeconds)
        {
            if (target == null || target.dead) { _approachEndReason = "target dead"; break; }
            float dist = XZDistance(player.transform.position, target.transform.position);
            if (dist <= brain.AttackRange + 0.2f) { inRange = true; _approachEndReason = "in range"; break; }

            Vector3 p = player.transform.position;
            float step = Vector3.Distance(p, lastPos);
            if (step > _approachMaxStep) _approachMaxStep = step;
            if (step > 1.2f) _approachTeleported = true;
            lastPos = p;

            if (_moveSamples.Count < 24 && Time.frameCount % 4 == 0)
                _moveSamples.Add(AnimSample(_pAnim, "move", _pAgent));
            TrackAnim(_pAnim, "approach");
            yield return null;
        }

        _approachSeconds = Time.realtimeSinceStartup - approachT0;
        _approachEndDist = target != null ? XZDistance(player.transform.position, target.transform.position) : -1f;
        if (string.IsNullOrEmpty(_approachEndReason)) _approachEndReason = "timeout";

        bool approachPass = inRange && !_approachTeleported && target != null && !target.dead;
        _aApproachPass = approachPass;
        sink.Record("4", "Attack.Approach",
            QaJson.Dict(
                "requirement", "由现有命令/移动链接近至实际攻击范围；不得用测试 Warp 玩家冒充接敌",
                "attackRange", _approachConfiguredRange,
                "endCondition", "XZ 距离 <= attackRange + 0.2"),
            QaJson.Dict(
                "startDistance", _approachStartDist,
                "endDistance", _approachEndDist,
                "configuredRange", _approachConfiguredRange,
                "elapsedSeconds", _approachSeconds,
                "inRange", inRange,
                "endReason", _approachEndReason,
                "maxFrameStep", _approachMaxStep,
                "teleportDetected", _approachTeleported,
                "agentPathStatus", _pAgent != null ? _pAgent.pathStatus.ToString() : "no_agent",
                "agentRemainingDistance", _pAgent != null ? _pAgent.remainingDistance : -1f,
                "moveSamples", _moveSamples.Count),
            approachPass, approachPass ? "pass" : "product_fail", "production",
            QaJson.Dict(
                "moveSampleDetail", _moveSamples,
                "note", "全程仅通过 CharacterBrain.CommandAttack 触发的 AttackLoop 追击；无 Warp、无 transform 直写"));
    }

    IEnumerator AssertAttackStarts(IQaSink sink, UnitHealth player, UnitHealth target, MeleeCombat sword,
                                   CharacterBrain brain)
    {
        _attackWindowStart = Time.realtimeSinceStartup;
        float startWait = Time.realtimeSinceStartup;
        _prevSwordTimer = GetSwordTimer();

        while (Time.realtimeSinceStartup - startWait < StartTimeoutSeconds)
        {
            float timer = GetSwordTimer();
            string st = ResolveState(_pAnim);
            if (_firstAttackAt < 0f && (timer >= 0f || IsAttackState(st)))
            {
                _firstAttackAt = Time.realtimeSinceStartup - _attackWindowStart;
                _firstAttackState = st;
                _firstAttackTargetOk = SwordTargetIsTarget();
                break;
            }
            if (target == null || target.dead) break;
            yield return null;
        }

        bool startsPass = _firstAttackAt >= 0f && _firstAttackTargetOk;
        sink.Record("4", "Attack.Starts",
            QaJson.Dict(
                "requirement", "观察到实际出招事件/状态；仅靠近未出招为产品失败",
                "signal", "MeleeCombat 私有 _timer >= 0 或 Animator 进入攻击状态",
                "targetLocked", true),
            QaJson.Dict("firstAttackAtSeconds", _firstAttackAt,
                 "firstAttackState", _firstAttackState,
                 "swordTargetIsFixture", _firstAttackTargetOk,
                 "playerHp", player.hp,
                 "targetHp", target != null ? target.hp : -1f),
            startsPass, startsPass ? "pass" : "product_fail", "production",
            QaJson.Dict("attackStates", AttackStates,
                 "swordConfigured", QaJson.Dict("attackStateName", sword.attackStateName,
                                         "attackLayer", sword.attackLayer,
                                         "attackLen", sword.attackLen,
                                         "activeStart", sword.activeStart,
                                         "activeEnd", sword.activeEnd,
                                         "hitRange", sword.hitRange,
                                         "runtimeCurLen", _swordCurLenField != null ? (float)_swordCurLenField.GetValue(sword) : -1f,
                                         "statsAttackSpeed", brain.stats != null ? brain.stats.attackSpeed : -1f)));
    }

    IEnumerator AssertAttackRepeats(IQaSink sink, UnitHealth target, MeleeCombat sword, CharacterBrain brain)
    {
        float prevTimer = _prevSwordTimer;
        float windowEnd = Time.realtimeSinceStartup + DamageWindowSeconds;
        _targetDeathAt = -1f;
        int sampleTick = 0;
        while (Time.realtimeSinceStartup < windowEnd)
        {
            float timer = GetSwordTimer();
            if (prevTimer < 0f && timer >= 0f)
            {
                _swingCount++;
                _swingTimes.Add((float)Math.Round(Time.realtimeSinceStartup - _attackWindowStart, 3));
            }
            prevTimer = timer;

            TrackAnim(_pAnim, "attack");
            if (_attackSamples.Count < 120 && (sampleTick++ % 3 == 0))
                _attackSamples.Add(AnimSample(_pAnim, "attack", _pAgent));

            if (target != null && target.dead)
            {
                if (_targetDeathAt < 0f) _targetDeathAt = Time.realtimeSinceStartup;
                else if (Time.realtimeSinceStartup - _targetDeathAt > 1f) break;
            }
            yield return null;
        }

        float attackSpan = Time.realtimeSinceStartup - _attackWindowStart;

        // ---- Repeats（只统计被选中玩家造成的结算；其他来源单列，避免污染）----
        var attributed = _hitEvents.Select(e => (Dictionary<string, object>)e)
                                   .Where(d => (bool)d["sourceIsSelectedPlayer"]).ToList();
        int unattributed = _hitEvents.Count - attributed.Count;
        var intervals = new List<object>();
        for (int i = 1; i < attributed.Count; i++)
        {
            var a = attributed[i - 1];
            var b = attributed[i];
            intervals.Add(QaJson.Dict("from", a["tAbs"], "to", b["tAbs"],
                               "intervalSeconds", (float)Math.Round((float)b["tAbs"] - (float)a["tAbs"], 3)));
        }
        float expectedInterval = sword != null && brain.stats != null && brain.stats.attackSpeed > 0f
            ? sword.attackLen / brain.stats.attackSpeed : -1f;
        bool repeatsPass = attributed.Count >= 2 && unattributed == 0;
        sink.Record("4", "Attack.Repeats",
            QaJson.Dict(
                "requirement", "固定观察窗口内至少出现 2 次有效攻击结算；不得以动画播放次数替代伤害次数",
                "minHits", 2,
                "windowSeconds", DamageWindowSeconds,
                "expectedIntervalSeconds", expectedInterval,
                "attackSpeed", brain.stats != null ? brain.stats.attackSpeed : -1f),
            QaJson.Dict("hitCount", attributed.Count,
                 "unattributedHitCount", unattributed,
                 "swingCount", _swingCount,
                 "swingTimes", _swingTimes, "intervals", intervals,
                 "observationWindowSeconds", (float)Math.Round(attackSpan, 3),
                 "targetHp", target != null ? target.hp : -1f),
            repeatsPass, repeatsPass ? "pass" : "product_fail", "production",
            QaJson.Dict("hitEvents", _hitEvents,
                 "expectedIntervalFormula", "attackLen / UnitStats.attackSpeed",
                 "note", "判定以被选中玩家的 OnHit 结算次数为准；其他玩家单位的命中单列不计入，swingCount 仅作对照"));
    }

    IEnumerator AssertAttackDamageResolved(IQaSink sink, UnitHealth target)
    {
        var attributed = _hitEvents.Select(e => (Dictionary<string, object>)e)
                                   .Where(d => (bool)d["sourceIsSelectedPlayer"]).ToList();
        int unattributed = _hitEvents.Count - attributed.Count;
        bool damageConsistent = attributed.Count >= 2 && unattributed == 0;
        float totalDamage = 0f;
        foreach (var d in attributed)
        {
            float before = (float)d["hpBefore"];
            float after = (float)d["hpAfter"];
            float amount = (float)d["finalAmount"];
            totalDamage += amount;
            if (Math.Abs((before - after) - amount) > 0.01f) damageConsistent = false;
            if (amount <= 0f) damageConsistent = false;
        }
        sink.Record("4", "Attack.DamageResolved",
            QaJson.Dict(
                "requirement", "每次有效命中经既有生命/伤害入口结算；记录目标 HP 前后值、结算伤害、事件顺序与累计次数",
                "entry", "UnitHealth.TakeHit → ApplyDamageInfo → OnHit",
                "testWritesHp", false),
            QaJson.Dict("hitCount", attributed.Count, "unattributedHitCount", unattributed,
                 "totalDamage", totalDamage,
                 "targetHp", target != null ? target.hp : -1f,
                 "targetMaxHp", target != null ? target.maxHp : -1f,
                 "targetDead", target != null && target.dead,
                 "hpDeltaConsistent", damageConsistent),
            damageConsistent, damageConsistent ? "pass" : "product_fail", "production",
            QaJson.Dict("hitEvents", _hitEvents,
                 "note", "断言侧无任何 HP 写入路径；HP 变化全部来自生产伤害管线"));
        yield break;
    }

    IEnumerator AssertAttackTargetDeath(IQaSink sink, UnitHealth target)
    {
        var attributed = _hitEvents.Select(e => (Dictionary<string, object>)e)
                                   .Where(d => (bool)d["sourceIsSelectedPlayer"]).ToList();
        int unattributed = _hitEvents.Count - attributed.Count;
        bool died = target != null && target.dead;
        if (died)
        {
            bool deathPass = _targetDeathEvents == 1 && attributed.Count >= 2 && unattributed == 0;
            sink.Record("4", "Attack.TargetDeath",
                QaJson.Dict("requirement", "若观察窗口内目标死亡，记录死亡事件仅一次、后续不再重复结算",
                     "deathEvents", 1),
                QaJson.Dict("dead", true, "deathEvents", _targetDeathEvents,
                     "hitCount", attributed.Count,
                     "unattributedHitCount", unattributed,
                     "targetHp", target.hp,
                     "elapsedSeconds", _targetDeathAt >= 0f ? (float)Math.Round(_targetDeathAt - _attackWindowStart, 3) : -1f),
                deathPass, deathPass ? "pass" : "product_fail", "production",
                QaJson.Dict("hitEvents", _hitEvents,
                     "note", "死亡后仍保持 1 秒观察，未再出现新的伤害结算"));
        }
        else
        {
            sink.Record("4", "Attack.TargetDeath",
                QaJson.Dict("requirement", "若观察窗口内目标死亡则断言一次死亡事件；未死亡则记录最终 HP 且不据此判普攻失败",
                     "covered", true),
                QaJson.Dict("dead", false, "finalHp", target != null ? target.hp : -1f,
                     "targetMaxHp", target != null ? target.maxHp : -1f,
                     "hitCount", attributed.Count,
                     "unattributedHitCount", unattributed,
                     "windowSeconds", DamageWindowSeconds),
                true, "observed", "production",
                QaJson.Dict("note", "观察窗口内目标未死亡，本单不覆盖死亡分支，不据此单独判普攻失败"));
        }
        yield break;
    }

    void CleanupAttackFixture()
    {
        if (_target != null)
        {
            _target.OnHit -= OnTargetHit;
            _target.OnDeath -= OnTargetDeath;
        }
        if (_targetGo != null)
        {
            Destroy(_targetGo);
            _targetGo = null;
        }
        _target = null;
    }

    void OnTargetHit(DamageInfo info)
    {
        _hitEventCount++;
        float before = _targetHpTracked;
        float after = info.target != null ? info.target.hp : -1f;
        _hitEvents.Add(QaJson.Dict(
            "index", _hitEventCount,
            "tSinceCommand", (float)Math.Round(Time.realtimeSinceStartup - _commandAt, 3),
            "tAbs", (float)Math.Round(Time.realtimeSinceStartup, 3),
            "hpBefore", before,
            "hpAfter", after,
            "finalAmount", info.finalAmount,
            "rawAmount", info.rawAmount,
            "brokeDefense", info.brokeDefense,
            "damageType", info.damageType.ToString(),
            "sourceName", info.source != null ? info.source.name : null,
            "sourceIsSelectedPlayer", info.source != null && info.source == _player,
            "targetDeadAfter", info.target != null && info.target.dead));
        _targetHpTracked = after;
    }

    void OnTargetDeath()
    {
        _targetDeathEvents++;
    }

    // ============================================================
    // 第 6 项：角色动画
    // ============================================================
    IEnumerator GroupAnimation()
    {
        Debug.Log("[FND00B06] === QA item 6: Animation ===");
        IQaSink sink = _ev;
        RestoreOtherBrains();

        Animator refAnim = _pAnim;
        if (refAnim == null)
        {
            UnitHealth any = FindObjectsByType<UnitHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(h => h != null && !h.name.StartsWith(FixturePrefix, StringComparison.Ordinal) && h.team == Team.Player);
            if (any != null) refAnim = any.GetComponentInChildren<Animator>(true);
        }
        _animatorOwner = refAnim != null ? GetPath(refAnim.transform) : "none";

        DumpAnimator(refAnim);

        yield return AssertAnimationConfigMapping(sink, refAnim, _sword);

        // ---- Idle（使用进入战斗前「无任何指令」时的采样）----
        yield return AssertAnimationIdle(sink);

        // ---- 战后动画状态补充观察（不计入五类状态通过/失败）----
        yield return AssertAnimationPostCombatState(sink, _pAnim, _pAgent);

        // ---- Move（复用第 4 项接近过程）----
        yield return AssertAnimationMove(sink);

        // ---- Attack ----
        yield return AssertAnimationAttack(sink, _brain, _sword);

        string err = null;
        if (!CreateAnimationFixture("HitFixture", out err))
        {
            _ev.BlockedAssertion("6", "Animation.Hit",
                QaJson.Dict("requirement", "独立动画夹具中通过生产 UnitHealth.ApplyDamage 施加一次非致命伤害，观察受击状态/事件"),
                "动画夹具建立失败: " + err, QaJson.Dict("error", err));
        }
        else
        {
            yield return AssertAnimationHit(sink, _player);
        }
        yield return AssertAnimationDownedOrDeath(sink);
    }

    IEnumerator AssertAnimationConfigMapping(IQaSink sink, Animator refAnim, MeleeCombat sword)
    {
        var mismatches = new List<object>();
        if (refAnim == null) mismatches.Add("未找到可用 Animator");
        if (sword != null && _runtimeLayerCount >= 0 && sword.attackLayer >= _runtimeLayerCount)
            mismatches.Add("MeleeCombat.attackLayer=" + sword.attackLayer + " >= Animator.layerCount=" + _runtimeLayerCount);
        if (sword != null && _stateList.Count > 0 && !_stateList.Any(s => (string)s == sword.attackStateName))
            mismatches.Add("MeleeCombat.attackStateName=\"" + sword.attackStateName + "\" 不在控制器状态列表");
        foreach (var st in RequiredStates)
            if (_stateList.Count > 0 && !_stateList.Any(s => (string)s == st))
                mismatches.Add("缺少状态: " + st);
        foreach (var p in RequiredParams)
            if (_paramNames.Count > 0 && !_paramNames.Contains(p))
                mismatches.Add("缺少参数: " + p);

        bool mappingConsistent = mismatches.Count == 0 && refAnim != null;
        sink.Record("6", "Animation.ConfigMapping",
            QaJson.Dict(
                "requirement", "只读确认 Animator Controller/Override 的状态名或哈希、参数与预期映射；不得凭名称猜测播放成功",
                "expectedStates", RequiredStates,
                "expectedParams", RequiredParams),
            QaJson.Dict("controller", _controllerName,
                 "overrideBase", _overrideBaseName,
                 "layerCount", _runtimeLayerCount,
                 "swordAttackLayer", sword != null ? sword.attackLayer : -1,
                 "swordAttackStateName", sword != null ? sword.attackStateName : null,
                 "mappingConsistent", mappingConsistent,
                 "mismatchCount", mismatches.Count),
            mappingConsistent, "observed", "production",
            QaJson.Dict("animatorOwner", _animatorOwner,
                 "states", _stateList,
                 "parameters", _paramDump,
                 "mismatches", mismatches,
                 "note", "本记录只做只读映射登记；不使用 Animator.Play 制造通过"));
        yield break;
    }

    IEnumerator AssertAnimationIdle(IQaSink sink)
    {
        if (!_idleCaptured || _idleSamples.Count == 0)
        {
            sink.BlockedAssertion("6", "Animation.Idle",
                QaJson.Dict("requirement", "无指令时观察待机/Locomotion 状态与参数"),
                "进入战斗前未取得玩家 Animator 采样",
                QaJson.Dict("idleCaptured", _idleCaptured, "player", _idlePlayerName));
            yield break;
        }

        var last = (Dictionary<string, object>)_idleSamples[_idleSamples.Count - 1];
        string state = (string)last["stateName"];
        float sx = Math.Abs((float)last["speedX"]);
        float sz = Math.Abs((float)last["speedZ"]);
        bool grounded = (bool)last["grounded"];
        bool idlePass = !IsAttackState(state) && TrimState(state) == "Locomotion" && sx <= 0.3f && sz <= 0.3f && grounded;

        sink.Record("6", "Animation.Idle",
            QaJson.Dict("requirement", "无指令时进入 Locomotion 待机混合树，SpeedX/SpeedZ≈0，Grounded=true",
                 "expectedState", " Locomotion（控制器实际状态名含前导空格）",
                 "maxAbsSpeed", 0.3f,
                 "samplePoint", "下达任何指令之前（Play 进入后、第 4 项开始前）"),
            QaJson.Dict("stateName", state, "speedX", last["speedX"], "speedZ", last["speedZ"],
                 "grounded", grounded, "attackSpeedParam", last["attackSpeedParam"],
                 "player", _idlePlayerName),
            idlePass, idlePass ? "pass" : "product_fail", "production",
            QaJson.Dict("samples", _idleSamples,
                 "note", "Idle 取样点在战斗前，避免把「出招链未收尾」误判为待机失败；战后状态另记于 Animation.PostCombatState"));
        yield break;
    }

    IEnumerator AssertAnimationPostCombatState(IQaSink sink, Animator animator, NavMeshAgent agent)
    {
        float postT0 = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - postT0 < IdleWaitSeconds + 6f)
        {
            TrackAnim(animator, "post_combat");
            yield return null;
        }
        var postSamples = new List<object>();
        for (int i = 0; i < 4; i++)
        {
            postSamples.Add(AnimSample(animator, "post_combat", agent));
            TrackAnim(animator, "post_combat_sample");
            yield return null;
        }
        var postLast = (Dictionary<string, object>)postSamples[postSamples.Count - 1];
        string postState = (string)postLast["stateName"];
        bool notInLocomotion = TrimState(postState) != "Locomotion";

        sink.Record("6", "Animation.PostCombatState",
            QaJson.Dict("requirement", "补充观察：战斗结束（目标死亡）且无后续指令时 Animator 是否回到 Locomotion",
                 "countedAs", "not_counted（不计入五类状态通过/失败，仅记录产品表现）"),
            QaJson.Dict("stateName", postState,
                 "normalizedTime", postLast["normalizedTime"],
                 "swordTimer", postLast["swordTimer"],
                 "speedX", postLast["speedX"], "speedZ", postLast["speedZ"],
                 "notInLocomotion", notInLocomotion,
                 "waitSeconds", IdleWaitSeconds + 6f),
            false, "observed", "production",
            QaJson.Dict("samples", postSamples,
                 "stateTimeline", _stateTimeline,
                 "analysis", "控制器链路 Attack_Draw --Attack--> Attack_Release --Attack--> Locomotion；出招中途停止后没有新的 Attack 触发器推进，可长期停留在攻击状态",
                 "note", "本记录只陈述观察结果，不修复；若状态非 Locomotion 说明击杀/停止攻击后动画不回待机"));
    }

    IEnumerator AssertAnimationMove(IQaSink sink)
    {
        if (_moveSamples.Count == 0)
        {
            sink.BlockedAssertion("6", "Animation.Move",
                QaJson.Dict("requirement", "使用既有移动命令观察移动状态/混合参数；复用第 4 项接近过程"),
                "第 4 项未产生接近采样", QaJson.Dict("moveSamples", 0));
            yield break;
        }

        float maxSpeedX = 0f, maxSpeedZ = 0f, maxVel = 0f;
        foreach (var s in _moveSamples)
        {
            var d = (Dictionary<string, object>)s;
            maxSpeedX = Mathf.Max(maxSpeedX, Math.Abs((float)d["speedX"]));
            maxSpeedZ = Mathf.Max(maxSpeedZ, Math.Abs((float)d["speedZ"]));
            maxVel = Mathf.Max(maxVel, (float)d["agentVelocity"]);
        }
        bool anyLocomotion = _moveSamples.Any(s => TrimState((string)((Dictionary<string, object>)s)["stateName"]) == "Locomotion");
        bool movePass = maxVel > 0.5f && (maxSpeedX > 0.5f || maxSpeedZ > 0.5f) && anyLocomotion;

        sink.Record("6", "Animation.Move",
            QaJson.Dict("requirement", "追击移动期间 Animator 处于 Locomotion 且 SpeedX/SpeedZ 随速度非零",
                 "minAgentVelocity", 0.5f,
                 "expectedState", "Locomotion"),
            QaJson.Dict("maxAbsSpeedX", maxSpeedX, "maxAbsSpeedZ", maxSpeedZ,
                 "maxAgentVelocity", maxVel, "anyLocomotionSample", anyLocomotion,
                 "sampleCount", _moveSamples.Count),
            movePass, movePass ? "pass" : "product_fail", "production",
            QaJson.Dict("samples", _moveSamples,
                 "note", "不另判移动 QA，仅验证移动时动画参数与状态"));
        yield break;
    }

    IEnumerator AssertAnimationAttack(IQaSink sink, CharacterBrain brain, MeleeCombat sword)
    {
        if (_firstAttackAt < 0f)
        {
            sink.BlockedAssertion("6", "Animation.Attack",
                QaJson.Dict("requirement", "与实际出招和伤害时序对照，断言进入既有攻击状态"),
                "第 4 项未观察到出招", QaJson.Dict("firstAttackAt", _firstAttackAt));
            yield break;
        }

        var attackEntries = _stateTimeline.Select(s => (Dictionary<string, object>)s)
                                          .Where(d => IsMeleeAttackState((string)d["stateName"])).ToList();
        var firstEntry = attackEntries.FirstOrDefault();
        bool enteredAttackState = attackEntries.Count > 0 || IsMeleeAttackState(_firstAttackState);

        var firstDamage = _hitEvents.Select(e => (Dictionary<string, object>)e)
                                    .FirstOrDefault(d => (bool)d["sourceIsSelectedPlayer"]);
        float animEntryAt = firstEntry != null ? (float)firstEntry["tSinceCommand"] : -1f;
        float damageAt = firstDamage != null ? (float)firstDamage["tSinceCommand"] : -1f;

        sink.Record("6", "Animation.Attack",
            QaJson.Dict("requirement", "真实出招触发 Animator 进入 AttackMelee（单段，状态名=触发器名），且 AttackSpeed 参数与 UnitStats.attackSpeed 一致",
                 "attackStates", AttackStates,
                 "attackSpeedExpected", brain != null && brain.stats != null ? brain.stats.attackSpeed : -1f),
            QaJson.Dict("firstAttackState", _firstAttackState,
                 "enteredAttackState", enteredAttackState,
                 "attackStateEntryCount", attackEntries.Count,
                 "firstAttackStateAtSinceCommand", animEntryAt,
                 "firstDamageAtSinceCommand", damageAt,
                 "animationLagBehindDamageSeconds", (animEntryAt >= 0f && damageAt >= 0f) ? (float)Math.Round(animEntryAt - damageAt, 3) : -1f,
                 "attackSpeedParam", firstEntry != null ? firstEntry["attackSpeedParam"] : null,
                 "firstAttackAtSeconds", _firstAttackAt,
                 "swordTargetIsFixture", _firstAttackTargetOk,
                 "damageHits", _hitEvents.Count),
            enteredAttackState, enteredAttackState ? "pass" : "product_fail", "production",
            QaJson.Dict("stateTimeline", _stateTimeline,
                 "attackEntries", attackEntries,
                 "configuredMapping", QaJson.Dict("attackStateName", sword != null ? sword.attackStateName : null,
                                           "attackLayer", sword != null ? sword.attackLayer : -1,
                                           "layerCount", _runtimeLayerCount),
                 "note", "未直接设置 Attack 触发器；状态变化来自生产 MeleeCombat.DoAttack；时间轴按状态变化记录"));
        yield break;
    }

    IEnumerator AssertAnimationHit(IQaSink sink, UnitHealth player)
    {

        // 夹具生命周期：先让 CharacterBrain/UnitBrain 正常走完 Awake/Start（生产侧在此订阅
        // UnitHealth.OnHit → OnDamaged），再禁用以免夹具自主移动/索敌。
        // 禁用组件不会撤销已建立的委托订阅，因此受击反馈仍走生产链路。
        CharacterBrain fixtureBrain = _animFixtureGo.GetComponent<CharacterBrain>();
        bool brainEnabledAtCreate = fixtureBrain != null && fixtureBrain.enabled;
        int brainSettleFrames = 0;
        for (int i = 0; i < 2; i++) { yield return null; brainSettleFrames++; }
        bool hitSubscribedAfterStart = HasHitSubscriber(_animFixtureHealth);
        if (fixtureBrain != null) fixtureBrain.enabled = false;

        Animator anim = _animFixtureGo.GetComponentInChildren<Animator>(true);

        // 受击前置：Animator 参数置为静止稳态（与 PlayerCommandController.Update 静止时同值）
        if (anim != null)
        {
            anim.SetBool("Grounded", true);
            anim.SetFloat("SpeedX", 0f);
            anim.SetFloat("SpeedZ", 0f);
        }

        // 受击前置：轮询等待进入 Locomotion（上限 AnimWaitSeconds）；不成立则 blocked，不伪造通过
        bool locomotionReady = false;
        string preState = anim != null ? ResolveState(anim) : "no_animator";
        int locomotionWaitFrames = 0;
        float locomotionWaitT0 = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - locomotionWaitT0 < AnimWaitSeconds)
        {
            preState = ResolveState(anim);
            if (TrimState(preState) == "Locomotion") { locomotionReady = true; break; }
            locomotionWaitFrames++;
            yield return null;
        }
        float locomotionWaitSeconds = (float)Math.Round(Time.realtimeSinceStartup - locomotionWaitT0, 3);

        if (!locomotionReady)
        {
            sink.BlockedAssertion("6", "Animation.Hit",
                QaJson.Dict("requirement", "独立动画夹具中通过生产 UnitHealth.ApplyDamage 施加一次非致命伤害，观察受击状态/事件",
                     "expectedState", "Hit_A",
                     "precondition", "受击前 Animator 处于 Locomotion"),
                "夹具未能进入 Locomotion（轮询 " + locomotionWaitSeconds + "s）",
                QaJson.Dict("preState", preState,
                     "locomotionWaitFrames", locomotionWaitFrames,
                     "locomotionWaitSeconds", locomotionWaitSeconds,
                     "brainEnabledAtCreate", brainEnabledAtCreate,
                     "brainSettleFrames", brainSettleFrames,
                     "hitSubscribedAfterStart", hitSubscribedAfterStart));
            yield break;
        }

        _animFixtureHitEvents = 0;
        _animFixturePreHitHp = _animFixtureHealth.hp;
        _animFixturePostHitHp = _animFixtureHealth.hp;
        _animFixtureHitAmount = -1f;

        System.Action<DamageInfo> onHit = info =>
        {
            _animFixtureHitEvents++;
            _animFixtureHitAmount = info.finalAmount;
            _animFixturePostHitHp = info.target != null ? info.target.hp : -1f;
        };
        _animFixtureHealth.OnHit += onHit;

        Dictionary<string, object> preSample = AnimSample(anim, "hit_pre", null);
        preState = (string)preSample["stateName"];

        float damage = 5f;
        _animFixtureHealth.ApplyDamage(damage, _animFixtureHealth.transform.position + Vector3.up * 0.6f, null);

        bool hitStateObserved = false;
        string hitState = null;
        var samples = new List<object>();
        float t0 = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - t0 < AnimWaitSeconds)
        {
            Dictionary<string, object> s = AnimSample(anim, "hit", null);
            if (samples.Count < 30) samples.Add(s);
            string st = (string)s["stateName"];
            if (TrimState(st) == "Hit_A") { hitStateObserved = true; hitState = st; }
            if (hitStateObserved) break;
            yield return null;
        }
        _animFixtureHealth.OnHit -= onHit;

        bool eventOk = _animFixtureHitEvents == 1 &&
                       Math.Abs(_animFixturePreHitHp - _animFixturePostHitHp - _animFixtureHitAmount) < 0.01f &&
                       _animFixtureHitAmount > 0f;
        bool pass = eventOk && hitStateObserved;

        sink.Record("6", "Animation.Hit",
            QaJson.Dict("requirement", "独立动画夹具中经生产 UnitHealth.ApplyDamage 施加一次非致命伤害，观察受击状态/事件",
                 "expectedState", "Hit_A",
                 "damageEntry", "UnitHealth.ApplyDamage（生产入口）",
                 "notCountedInItem4", true),
            QaJson.Dict("hitEvents", _animFixtureHitEvents,
                 "hpBefore", _animFixturePreHitHp,
                 "hpAfter", _animFixturePostHitHp,
                 "finalAmount", _animFixtureHitAmount,
                 "preState", preState,
                 "hitStateObserved", hitStateObserved,
                 "hitState", hitState,
                 "playerHpUnaffected", player != null ? player.hp : -1f),
            pass, pass ? "pass" : "product_fail", "production",
            QaJson.Dict("fixtureName", _animFixtureGo != null ? _animFixtureGo.name : null,
                 "fixturePath", _animFixtureGo != null ? GetPath(_animFixtureGo.transform) : null,
                 "brainEnabledAtCreate", brainEnabledAtCreate,
                 "brainSettleFrames", brainSettleFrames,
                 "hitSubscribedAfterStart", hitSubscribedAfterStart,
                 "brainDisabledBeforeDamage", fixtureBrain != null && !fixtureBrain.enabled,
                 "locomotionWaitFrames", locomotionWaitFrames,
                 "locomotionWaitSeconds", locomotionWaitSeconds,
                 "samples", samples,
                 "note", "该伤害只作动画刺激，不计入第 4 项普攻结算；夹具为 Knight_Unit 内存实例，未改生产资产"));
    }

    IEnumerator AssertAnimationDownedOrDeath(IQaSink sink)
    {
        if (_animFixtureGo == null || _animFixtureHealth == null)
        {
            sink.BlockedAssertion("6", "Animation.DownedOrDeath",
                QaJson.Dict("requirement", "仅对一名夹具角色通过生产伤害入口施加致命伤害，记录 dead/isDown 与实际 Animator 状态"),
                "动画夹具未建立（Hit 夹具失败）", QaJson.Dict("fixturePresent", _animFixtureGo != null));
            yield break;
        }

        Animator anim = _animFixtureGo.GetComponentInChildren<Animator>(true);
        _animFixtureDeathEvents = 0;
        System.Action onDeath = () => { _animFixtureDeathEvents++; };
        _animFixtureHealth.OnDeath += onDeath;

        int downedBefore = DownedRescue.AllDowned.Count;
        bool wipedBefore = GameFlow.instance != null && GameFlow.instance.IsWiped;

        _animFixtureHealth.ApplyDamage(9999f, _animFixtureHealth.transform.position + Vector3.up * 0.6f, null);

        bool downedStateObserved = false;
        string downedState = null;
        var samples = new List<object>();
        float t0 = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - t0 < AnimWaitSeconds)
        {
            Dictionary<string, object> s = AnimSample(anim, "downed", null);
            if (samples.Count < 40) samples.Add(s);
            string st = (string)s["stateName"];
            if (TrimState(st) == "Downed") { downedStateObserved = true; downedState = st; }
            bool isDownNow = _animFixtureDowned != null && _animFixtureDowned.IsDown;
            if (downedStateObserved && isDownNow) break;
            yield return null;
        }
        _animFixtureHealth.OnDeath -= onDeath;

        bool dead = _animFixtureHealth.dead;
        bool isDown = _animFixtureDowned != null && _animFixtureDowned.IsDown;
        bool wipedAfter = GameFlow.instance != null && GameFlow.instance.IsWiped;
        float tsAfter = Time.timeScale;
        bool pass = dead && isDown && _animFixtureDeathEvents == 1 && downedStateObserved &&
                    !wipedAfter && Mathf.Approximately(tsAfter, 1f);

        sink.Record("6", "Animation.DownedOrDeath",
            QaJson.Dict("requirement", "对夹具角色施加致命伤害后：dead=true、IsDown=true、死亡事件仅 1 次、Animator 进入 Downed，且不制造团灭",
                 "expectedState", "Downed",
                 "deathEvents", 1),
            QaJson.Dict("dead", dead, "isDown", isDown, "deathEvents", _animFixtureDeathEvents,
                 "stateEntered", downedStateObserved, "state", downedState,
                 "wipedBefore", wipedBefore, "wipedAfter", wipedAfter, "timeScale", tsAfter,
                 "downedListBefore", downedBefore, "downedListAfter", DownedRescue.AllDowned.Count),
            pass, pass ? "pass" : "product_fail", "production",
            QaJson.Dict("fixtureName", _animFixtureGo != null ? _animFixtureGo.name : null,
                 "samples", samples,
                 "note", "当前玩家设计以倒地（Downed）代替彻底死亡；本断言不触发全员团灭，未改生产资产"));

        if (_animFixtureGo != null)
        {
            Destroy(_animFixtureGo);
            _animFixtureGo = null;
        }
        _animFixtureHealth = null;
        _animFixtureDowned = null;
        yield return null;
    }

    bool CreateAnimationFixture(string label, out string error)
    {
        error = null;
        GameObject prefab = null;
#if UNITY_EDITOR
        try { prefab = AssetDatabase.LoadAssetAtPath<GameObject>(KnightPrefabPath); }
        catch (Exception e) { error = e.GetType().Name + ": " + e.Message; }
#else
        error = "非编辑器环境无法加载预制体";
#endif
        if (prefab == null)
        {
            error = "无法加载 " + KnightPrefabPath + "; " + (error ?? "");
            return false;
        }

        Vector3 basePos = _player != null ? _player.transform.position : Vector3.zero;
        Vector3 want = basePos + new Vector3(0f, 0f, FixtureOffsetMeters);
        NavMeshHit navHit;
        Vector3 pos = NavMesh.SamplePosition(want, out navHit, 6f, NavMesh.AllAreas) ? navHit.position : want;

        try
        {
            _animFixtureGo = Instantiate(prefab, pos, Quaternion.identity);
            _animFixtureGo.name = FixturePrefix + label;
        }
        catch (Exception e)
        {
            error = e.GetType().Name + ": " + e.Message;
            return false;
        }

        // 不在此处禁用 CharacterBrain：生产链路只有 CharacterBrain.Start() 才执行
        // UnitHealth.OnHit += OnDamaged，Instantiate 后立刻禁用会让 Start 永不执行，
        // 受击时也就不会再 SetTrigger("Hit")（Spec-Diag-Skill 已用 red 证据确认）。
        // 禁用时机下移到 HitAnimationFixture：等 Awake/Start 走完再禁用。

        _animFixtureHealth = _animFixtureGo.GetComponent<UnitHealth>();
        _animFixtureDowned = _animFixtureGo.GetComponent<DownedRescue>();
        if (_animFixtureHealth == null)
        {
            error = "夹具缺少 UnitHealth";
            return false;
        }
        return true;
    }

    // ============================================================
    // 采样与工具
    // ============================================================
    Dictionary<string, object> AnimSample(Animator anim, string label, NavMeshAgent agent)
    {
        var d = new Dictionary<string, object>();
        d["label"] = label;
        d["t"] = (float)Math.Round(Time.realtimeSinceStartup, 3);
        if (anim == null)
        {
            d["stateName"] = "no_animator";
            d["stateHash"] = 0;
            d["normalizedTime"] = -1f;
            d["speedX"] = 0f;
            d["speedZ"] = 0f;
            d["grounded"] = false;
            d["attackSpeedParam"] = 0f;
            d["agentVelocity"] = 0f;
            d["swordTimer"] = -1f;
            return d;
        }
        AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
        d["stateName"] = ResolveState(anim);
        d["stateHash"] = info.shortNameHash;
        d["normalizedTime"] = info.normalizedTime % 1f;
        d["speedX"] = anim.GetFloat("SpeedX");
        d["speedZ"] = anim.GetFloat("SpeedZ");
        d["grounded"] = anim.GetBool("Grounded");
        d["attackSpeedParam"] = anim.GetFloat("AttackSpeed");
        d["agentVelocity"] = agent != null ? agent.velocity.magnitude : 0f;
        d["swordTimer"] = GetSwordTimer();
        return d;
    }

    /// <summary>按状态变化记录动画时间轴（只记录切换，避免逐帧噪声）。</summary>
    void TrackAnim(Animator anim, string phase)
    {
        string s = ResolveState(anim);
        if (s == _lastStateName) return;
        _lastStateName = s;
        if (_stateTimeline.Count >= 240) return;
        _stateTimeline.Add(QaJson.Dict(
            "phase", phase,
            "tAbs", (float)Math.Round(Time.realtimeSinceStartup, 3),
            "tSinceCommand", _commandAt > 0f ? (float)Math.Round(Time.realtimeSinceStartup - _commandAt, 3) : -1f,
            "stateName", s,
            "normalizedTime", anim != null ? anim.GetCurrentAnimatorStateInfo(0).normalizedTime % 1f : -1f,
            "attackSpeedParam", anim != null ? anim.GetFloat("AttackSpeed") : -1f,
            "swordTimer", GetSwordTimer()));
    }

    void RestoreOtherBrains()
    {
        for (int i = 0; i < _othersDisabled.Count; i++)
        {
            UnitHealth h = _othersDisabled[i];
            if (h == null) continue;
            CharacterBrain b = h.GetComponent<CharacterBrain>();
            if (b != null) b.enabled = _othersBrainEnabled[i];
        }
    }

    string ResolveState(Animator anim)
    {
        if (anim == null) return "no_animator";
        AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
        string n;
        if (_stateNames.TryGetValue(info.shortNameHash, out n)) return n;
        foreach (var candidate in KnownStates)
            if (info.IsName(candidate)) return candidate;
        return "unknown#" + info.shortNameHash;
    }

    static string TrimState(string s)
    {
        return s == null ? "" : s.Trim();
    }

    static bool IsAttackState(string s)
    {
        string t = TrimState(s);
        return t == "AttackMelee" || t == "Attack_Draw" || t == "Attack_Release";
    }

    /// <summary>单段近战口径：状态名 = 触发器名 = AttackMelee。</summary>
    static bool IsMeleeAttackState(string s)
    {
        return TrimState(s) == "AttackMelee";
    }

    float GetSwordTimer()
    {
        if (_sword == null || _swordTimerField == null) return -1f;
        return (float)_swordTimerField.GetValue(_sword);
    }

    bool SwordTargetIsTarget()
    {
        if (_sword == null || _swordTargetField == null || _target == null) return false;
        return _swordTargetField.GetValue(_sword) as Transform == _target.transform;
    }

    void DumpAnimator(Animator anim)
    {
        _stateNames.Clear();
        _stateList.Clear();
        _paramDump.Clear();
        _paramNames.Clear();
        _layerCount = -1;
        _runtimeLayerCount = anim != null ? anim.layerCount : -1;
        _controllerName = "none";
        _overrideBaseName = "none";
        if (anim == null) return;

        RuntimeAnimatorController rt = anim.runtimeAnimatorController;
        _controllerName = rt != null ? rt.name : "null";
        var oc = rt as AnimatorOverrideController;
        if (oc != null) _overrideBaseName = oc.runtimeAnimatorController != null ? oc.runtimeAnimatorController.name : "null";
        RuntimeAnimatorController baseCtrl = oc != null ? oc.runtimeAnimatorController : rt;

#if UNITY_EDITOR
        var ac = baseCtrl as UnityEditor.Animations.AnimatorController;
        if (ac != null)
        {
            _layerCount = ac.layers.Length;
            foreach (var layer in ac.layers)
                CollectStates(layer.stateMachine, _stateNames, _stateList);
            foreach (var p in ac.parameters)
            {
                _paramDump.Add(QaJson.Dict("name", p.name, "type", p.type.ToString(),
                                    "defaultFloat", p.defaultFloat, "defaultBool", p.defaultBool, "source", "controller"));
                if (!_paramNames.Contains(p.name)) _paramNames.Add(p.name);
            }
        }
#endif
        if (_layerCount < 0) _layerCount = _runtimeLayerCount;
        if (anim.parameters != null)
        {
            foreach (var p in anim.parameters)
            {
                if (_paramNames.Contains(p.name)) continue;
                _paramDump.Add(QaJson.Dict("name", p.name, "type", p.type.ToString(), "source", "runtime"));
                _paramNames.Add(p.name);
            }
        }
    }

#if UNITY_EDITOR
    static void CollectStates(AnimatorStateMachine sm, Dictionary<int, string> names, List<object> list)
    {
        if (sm == null) return;
        foreach (var child in sm.states)
        {
            if (child.state == null) continue;
            string n = child.state.name;
            names[Animator.StringToHash(n)] = n;
            if (!list.Any(x => (string)x == n)) list.Add(n);
        }
        foreach (var sub in sm.stateMachines)
            if (sub.stateMachine != null) CollectStates(sub.stateMachine, names, list);
    }
#endif

    bool TryPickSpawn(Vector3 playerPos, out Vector3 pos, out string method, out List<object> trials)
    {
        trials = new List<object>();
        float[] dists = { 7f, 5.5f, 4f };
        Vector3 fwd = _player != null ? _player.transform.forward : Vector3.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
        fwd.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
        Vector3[] dirs = { fwd, right, -fwd, -right };

        foreach (float d in dists)
        {
            foreach (var dir in dirs)
            {
                Vector3 want = playerPos + dir * d;
                NavMeshHit navHit;
                bool sampled = NavMesh.SamplePosition(want, out navHit, 3f, NavMesh.AllAreas);
                Vector3 cand = sampled ? navHit.position : want;
                NavMeshPath path = new NavMeshPath();
                bool pathOk = NavMesh.CalculatePath(playerPos, cand, NavMesh.AllAreas, path) &&
                              path.status == NavMeshPathStatus.PathComplete && path.corners.Length > 1;
                trials.Add(QaJson.Dict("distance", d, "requested", QaJson.V3(want), "sampled", sampled,
                                "candidate", QaJson.V3(cand), "pathStatus", path.status.ToString(), "cornerCount", path.corners.Length));
                if (sampled && pathOk)
                {
                    pos = cand;
                    method = "NavMesh.SamplePosition + NavMesh.CalculatePath（d=" + d + "m）";
                    return true;
                }
            }
        }
        pos = playerPos;
        method = "none";
        return false;
    }

    static MethodInfo PrivMethod(Type t, string name)
    {
        return t.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
    }

    static FieldInfo PrivField(Type t, string name)
    {
        return t.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
    }

    /// <summary>
    /// 只读检查生产事件订阅：C# event 编译为私有委托字段（UnitHealth 的 OnHit）。
    /// 用于确认 CharacterBrain.Start() 已执行 UnitHealth.OnHit += OnDamaged，而非靠猜测。
    /// </summary>
    static bool HasHitSubscriber(UnitHealth h)
    {
        if (h == null) return false;
        FieldInfo f = PrivField(typeof(UnitHealth), "OnHit");
        if (f == null) return false;
        return f.GetValue(h) as Delegate != null;
    }

    static MeleeCombat FindSword(UnitHealth h)
    {
        if (h == null) return null;
        var s = h.GetComponent<MeleeCombat>();
        if (s == null) s = h.GetComponentInChildren<MeleeCombat>(true);
        return s;
    }

    static float XZDistance(Vector3 a, Vector3 b)
    {
        Vector3 d = a - b; d.y = 0f;
        return d.magnitude;
    }

    /// <summary>层级路径（证据 data 里的稳定标识；Qa 层无等价物，留在套件内）。</summary>
    static string GetPath(Transform t)
    {
        var parts = new List<string>();
        while (t != null) { parts.Insert(0, t.name); t = t.parent; }
        return string.Join("/", parts.ToArray());
    }
}
