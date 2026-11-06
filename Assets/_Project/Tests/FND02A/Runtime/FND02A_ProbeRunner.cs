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
using Dungeon.Core;
using Dungeon.Gameplay.Player;
using Dungeon.Gameplay.Units;
using Dungeon.Gameplay.World;
using Dungeon.App;
#endif

/// <summary>
/// FND-02A 运行期探针（Play 模式）。
/// 只读生产状态；测试自身唯一写入面为 Docs 证据与 SessionState 退出码。
/// 断言顺序：Startup.ProductEntry → Startup.OnceAndStable → Encounter.SceneCensus →
///           Encounter.FixedIdentity → Encounter.NavMesh → Encounter.RealApproach → Regression.NoNewErrors
/// 不使用 Warp、不写玩家坐标、不直接 SetDestination 冒充指令。
/// </summary>
public class FND02A_ProbeRunner : MonoBehaviour
{
    public const string ScenePath = "Assets/Scenes/DungeonWorld.unity";
    public const string EnemyPrefabPath = "Assets/_Project/Prefabs/Skeleton_Warrior.prefab";
    public const string EnemyPrefabGuid = "18dec5e6af31178468b146e0440bd081";
    public const string FixturePrefix = "FND02A_";

    public static readonly Vector3 EnemyPos = new Vector3(-2.72f, 0f, -12f);
    public const float PosTolXZ = 0.05f;
    public const float PosTolY = 0.10f;
    public const float RotTolDeg = 0.5f;
    public const float ApproachDestZ = -5f;      // 距敌 7m（> Knight aggro 6，指令段不被自动追击污染）
    public const float ApproachTimeout = 15f;
    public const float RegressionDestZ = -2f;    // 距敌 10m

    public const string FinishedKey = "FND02A.Finished";
    public const string ExitCodeKey = "FND02A.ExitCode";

    static FND02A_Evidence _ev;
    static bool _finished;
    static string _exitReason = "";

    int _run;
    string _mode;
    int _seed = 20261104;
    float _deadline;

    UnitHealth _enemy;
    UnitHealth _knight;
    CharacterBrain _knightBrain;
    Selectable _knightSel;

    // 接近测量
    float _approachStartDist = -1f;
    float _approachEndDist = -1f;
    float _approachTraveled;
    float _approachMaxStep;
    float _approachDuration;
    string _approachPathStatus = "none";
    int _approachCorners;

    static int _runCounter;

    void Awake()
    {
        _runCounter++;
        DontDestroyOnLoad(gameObject);
    }

    IEnumerator Start()
    {
        int parsedRun;
        string envRun = Environment.GetEnvironmentVariable("FND02A_RUN");
        if (int.TryParse(envRun, out parsedRun)) _run = parsedRun;
        string envMode = Environment.GetEnvironmentVariable("FND02A_MODE");
        _mode = string.IsNullOrEmpty(envMode) ? "green" : envMode.Trim().ToLowerInvariant();
        if (_mode == "red") _run = 0;
        else if (_run <= 0) _run = 1;

        _seed = 20261104 + _run;
        UnityEngine.Random.InitState(_seed);

        _ev = new FND02A_Evidence(_run, _mode);
        _deadline = Time.realtimeSinceStartup + 300f;

        Debug.Log("[FND02A] runner start; mode=" + _mode + "; run=" + _run +
                  "; seed=" + _seed + "; unity=" + Application.unityVersion +
                  "; evidenceDir=" + _ev.EvidenceDir +
                  "; head=" + GetHead());

        float waited = 0f;
        while (SceneManager.GetActiveScene().path != ScenePath && waited < 25f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        Scene scene = SceneManager.GetActiveScene();
        Debug.Log("[FND02A] active scene=" + scene.path + "; loaded=" + scene.isLoaded +
                  "; waited=" + waited.ToString("0.00") + "s");

        float obsWaited = 0f;
        while (FND02A_EdgeObserver.Instance == null && obsWaited < 10f)
        {
            obsWaited += Time.unscaledDeltaTime;
            yield return null;
        }
        FND02A_EdgeObserver obs = FND02A_EdgeObserver.Instance;
        if (obs == null)
        {
            _ev.InfraFailure("Startup.ProductEntry", "EdgeObserver.Present",
                "运行期观察者未被创建（RuntimeInitializeOnLoadMethod 未执行或 SessionState 未置位）", "");
            Finish();
            yield break;
        }
        Debug.Log("[FND02A] observer ready; waited=" + obsWaited.ToString("0.00") +
                  "s; baseline=" + FND02A_EdgeObserver.BaselineValue +
                  "; baselineTimeScale=" + FND02A_EdgeObserver.BaselineTimeScale +
                  "; firstValue=" + obs.FirstValue +
                  "; firstFrame=" + obs.FirstFrame);

        // 首帧已由观察者记录；等待状态稳定
        yield return new WaitForSecondsRealtime(1.0f);

        yield return AssertProductEntry(obs, scene);
        yield return AssertOnceAndStable(obs, scene);
        yield return AssertSceneCensus(scene);

        if (_enemy != null)
        {
            AssertFixedIdentity();
            AssertNavMesh();
            yield return AssertRealApproach();
        }
        else
        {
            _ev.BlockedAssertion("Encounter.FixedIdentity", "Encounter.FixedIdentity",
                "来源为 Skeleton_Warrior.prefab，位置/旋转等于批准值",
                "场景中未找到 team=Monster 的 UnitHealth", null);
            _ev.BlockedAssertion("Encounter.NavMesh", "Encounter.NavMesh",
                "Agent 启用且 isOnNavMesh=true，生产 AI 未禁用",
                "场景中未找到 team=Monster 的 UnitHealth", null);
            _ev.BlockedAssertion("Encounter.RealApproach", "Encounter.RealApproach",
                "通过现有选择/移动命令从玩家出生区接近固定遭遇",
                "场景中未找到 team=Monster 的 UnitHealth", null);
        }

        yield return AssertRegression(obs);

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

    // ============================================================
    // 1. Startup.ProductEntry
    // ============================================================
    IEnumerator AssertProductEntry(FND02A_EdgeObserver obs, Scene scene)
    {
        yield return null;
        yield return null;

        bool started = GameFlow.GameStarted;
        float ts = Time.timeScale;
        UIManager ui = UIManager.instance;
        bool uiPresent = ui != null;
        bool menuSlotNull = !uiPresent || ui.mainMenuPanel == null;
        int menuInstances = FindObjectsByType<MainMenuPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        int gameFlowInstances = FindObjectsByType<GameFlow>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        bool paused = PauseManager.instance != null && PauseManager.instance.IsPaused;

        bool pass = started && Mathf.Approximately(ts, 1f) && menuSlotNull && menuInstances == 0 && !paused;

        _ev.Record("Startup.ProductEntry", "Startup.ProductEntry",
            "产品自身令 GameStarted=true、timeScale=1，无主菜单阻塞，测试未写入任何状态",
            "gameStarted=" + started + "; timeScale=" + ts.ToString("0.###") +
            "; mainMenuPanelNull=" + menuSlotNull + "; mainMenuInstances=" + menuInstances,
            pass, pass ? "pass" : "product_fail", "production",
            Dict(
                "entryComponent", "GameFlow",
                "entryChain", "Unity 场景加载 → GameFlow.Awake(instance=this) → GameFlow.Start(): 若存在 team==Team.Player 的 UnitHealth 则 GameStarted=true, timeScale=1 → UIManager/SelectionManager 后续 Update 读取 public static GameStarted",
                "entryOwnerIsSole", true,
                "gameFlowInstances", gameFlowInstances,
                "mainMenuPanelSlotNull", menuSlotNull,
                "mainMenuPanelInstances", menuInstances,
                "paused", paused,
                "testWroteGameStarted", false,
                "testCalledEntry", false,
                "baselineBeforeStart", obs != null ? (object)FND02A_EdgeObserver.BaselineValue : null,
                "baselineTimeScale", obs != null ? (object)FND02A_EdgeObserver.BaselineTimeScale : null,
                "firstObservedValue", obs != null ? (object)obs.FirstValue : null,
                "firstObservedFrame", obs != null ? (object)obs.FirstFrame : null,
                "firstTrueRealtime", obs != null ? (object)obs.FirstTrueRealtime : null,
                "firstTrueFrame", obs != null ? (object)obs.FirstTrueFrame : null,
                "stableSampleRealtime", Time.realtimeSinceStartup,
                "stableGameStarted", started,
                "timeScale", ts,
                "head", GetHead(),
                "sceneInstanceFileID", GetSceneInstanceFileID(),
                "observerFrames", obs != null ? (object)obs.UpdateFrames : null,
                "gameStartedWriters", "GameFlow.Start（唯一实例化写入者）；MainMenuPanel.OnClose 为休眠写入者（场景无实例、槽位为 null），本单未修改"));
    }

    // ============================================================
    // 2. Startup.OnceAndStable
    // ============================================================
    IEnumerator AssertOnceAndStable(FND02A_EdgeObserver obs, Scene scene)
    {
        int edgesBefore = obs.RisingEdges;
        List<object> playerIds = PlayerIds();
        List<object> monsterIds = MonsterIds();
        int subCount = SubscriberCount();
        float observeStart = Time.realtimeSinceStartup;
        yield return new WaitForSecondsRealtime(2.0f);
        float window = Time.realtimeSinceStartup - observeStart;

        int edgesAfter = obs.RisingEdges;
        List<object> playerIdsAfter = PlayerIds();
        List<object> monsterIdsAfter = MonsterIds();
        bool stablePlayers = playerIds.Count == 3 && playerIdsAfter.Count == 3;
        bool stableMonsters = monsterIdsAfter.Count == monsterIds.Count;
        bool pass = edgesBefore == 1 && edgesAfter == 1 && stablePlayers && stableMonsters;

        _ev.Record("Startup.OnceAndStable", "Startup.OnceAndStable",
            "观察窗内仅 1 次 false→true；玩家仍为 3；敌人不重复生成；状态稳定",
            "risingEdges=" + edgesAfter + "（1s 时 " + edgesBefore + "）; players=" + playerIdsAfter.Count +
            "; monsters=" + monsterIdsAfter.Count + "; window=" + window.ToString("0.00") + "s",
            pass, pass ? "pass" : "product_fail", "production",
            Dict(
                "risingEdgesAt1s", edgesBefore,
                "risingEdgesAfterWindow", edgesAfter,
                "observedSeconds", window,
                "playerCountStart", playerIds.Count,
                "playerCountAfter", playerIdsAfter.Count,
                "monsterCountStart", monsterIds.Count,
                "monsterCountAfter", monsterIdsAfter.Count,
                "playerIds", playerIdsAfter,
                "monsterIds", monsterIdsAfter,
                "eventSubscriptionCount", subCount,
                "eventSubscriptionNote", "UnitHealth.OnDeath 委托订阅数（反射只读）；GameFlow 未定义事件",
                "observerUpdateFrames", obs.UpdateFrames,
                "timeScale", Time.timeScale,
                "gameStarted", GameFlow.GameStarted));
    }

    // ============================================================
    // 3. Encounter.SceneCensus
    // ============================================================
    IEnumerator AssertSceneCensus(Scene scene)
    {
        yield return null;

        UnitHealth[] all = FindObjectsByType<UnitHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var monsters = new List<UnitHealth>();
        int fixtures = 0;
        foreach (var h in all)
        {
            if (h == null) continue;
            if (h.name.StartsWith(FixturePrefix, StringComparison.Ordinal)) fixtures++;
            if (h.team == Team.Monster) monsters.Add(h);
        }
        monsters = monsters.OrderBy(h => h.name).ThenBy(h => h.GetInstanceID()).ToList();

        int spawners = FindObjectsByType<SkeletonSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length
                     + FindObjectsByType<WaveSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
        int forestGens = FindObjectsByType<ForestGenerator>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;

        if (monsters.Count > 0) _enemy = monsters[0];

        bool pass = monsters.Count == 1 && spawners == 0 && fixtures == 0
                    && _enemy != null && _enemy.gameObject.scene.path == ScenePath;

        var list = new List<object>();
        foreach (var m in monsters)
        {
            list.Add(Dict(
                "name", m.name,
                "instanceId", m.GetInstanceID(),
                "transformPath", GetPath(m.transform),
                "team", m.team.ToString(),
                "hp", m.hp,
                "maxHp", m.maxHp,
                "dead", m.dead,
                "activeInHierarchy", m.gameObject.activeInHierarchy,
                "scenePath", m.gameObject.scene.path,
                "prefabExpected", EnemyPrefabGuid));
        }

        _ev.Record("Encounter.SceneCensus", "Encounter.SceneCensus",
            "生产场景恰有 1 个普通敌人（非测试内存实例），未新增随机刷怪来源",
            "monsters=" + monsters.Count + "; spawners=" + spawners + "; testUnitHealth=" + fixtures,
            pass, pass ? "pass" : "product_fail", "production",
            Dict(
                "monsterCount", monsters.Count,
                "monsters", list,
                "spawnerCount", spawners,
                "forestGeneratorCount", forestGens,
                "testFixtureUnitHealthCount", fixtures,
                "scenePath", scene.path,
                "enemyId", _enemy != null ? (object)_enemy.GetInstanceID() : null,
                "enemyName", _enemy != null ? _enemy.name : "none",
                "enemyTeam", _enemy != null ? _enemy.team.ToString() : "none",
                "enemyHp", _enemy != null ? (object)_enemy.hp : null,
                "enemyActive", _enemy != null ? (object)_enemy.gameObject.activeInHierarchy : null));
    }

    // ============================================================
    // 4. Encounter.FixedIdentity
    // ============================================================
    void AssertFixedIdentity()
    {
        Vector3 p = _enemy.transform.position;
        Vector3 e = _enemy.transform.rotation.eulerAngles;
        float dx = Mathf.Abs(p.x - EnemyPos.x);
        float dy = Mathf.Abs(p.y - EnemyPos.y);
        float dz = Mathf.Abs(p.z - EnemyPos.z);
        float rotDelta = Quaternion.Angle(_enemy.transform.rotation, Quaternion.identity);

        string prefabPath = "unknown";
        string runtimeGuid = "unknown";
        string sceneSourceGuid = "none";
        bool isPrefabInstance = false;
        string instanceStatus = "unknown";
        string statsGuid = "unknown";
        MonsterBrain brain = _enemy.GetComponent<MonsterBrain>();
#if UNITY_EDITOR
        sceneSourceGuid = SessionState.GetString("FND02A.EnemySourceGuid", "none");
        isPrefabInstance = PrefabUtility.IsPartOfPrefabInstance(_enemy.gameObject);
        instanceStatus = PrefabUtility.GetPrefabInstanceStatus(_enemy.gameObject).ToString();
        GameObject src = PrefabUtility.GetCorrespondingObjectFromSource(_enemy.gameObject);
        if (src != null)
        {
            string assetPath = AssetDatabase.GetAssetPath(src);
            if (!string.IsNullOrEmpty(assetPath))
            {
                prefabPath = assetPath;
                runtimeGuid = AssetDatabase.AssetPathToGUID(assetPath);
            }
        }
        if (brain != null && brain.stats != null)
        {
            string statsPath = AssetDatabase.GetAssetPath(brain.stats);
            if (!string.IsNullOrEmpty(statsPath)) statsGuid = AssetDatabase.AssetPathToGUID(statsPath);
        }
#endif

        bool posOk = dx <= PosTolXZ && dy <= PosTolY && dz <= PosTolXZ;
        bool rotOk = rotDelta <= RotTolDeg;
        // 来源判定：场景 YAML m_SourcePrefab GUID（唯一 1 个实例）+ 运行时内容指纹。
        // 注：本工程预制体变体的根是嵌套 FBX 预制体对象，运行期 PrefabUtility 链接查询返回 NotAPrefab，
        //     故该值仅作诊断记录，不作为判定依据。
        bool nameOk = _enemy.name == "Skeleton_Warrior";
        bool guidOk = sceneSourceGuid == EnemyPrefabGuid && nameOk;
        bool contentOk = statsGuid == "520f29cc9754e2f40b32f1d017f3d3ed"; // SkeletonWarrior.asset
        bool pass = posOk && rotOk && guidOk && contentOk;

        _ev.Record("Encounter.FixedIdentity", "Encounter.FixedIdentity",
            "来源为批准的 Skeleton_Warrior.prefab，初始位置 (-2.72,0,-12)、旋转 (0,0,0)",
            "guid=" + sceneSourceGuid + "; pos=(" + F(p.x) + "," + F(p.y) + "," + F(p.z) + "); rotDelta=" + F(rotDelta),
            pass, pass ? "pass" : "product_fail", "production",
            Dict(
                "prefabPath", prefabPath,
                "sceneSourceGuid", sceneSourceGuid,
                "runtimeSourceGuid", runtimeGuid,
                "guidSource", "场景 YAML m_SourcePrefab（编辑期由 FND02A_BatchAcceptance 只读解析）",
                "isPrefabInstance", isPrefabInstance,
                "prefabInstanceStatus", instanceStatus,
                "expectedPrefabGuid", EnemyPrefabGuid,
                "monsterStatsGuid", statsGuid,
                "expectedStatsGuid", "520f29cc9754e2f40b32f1d017f3d3ed",
                "sceneInstanceFileID", GetSceneInstanceFileID(),
                "sceneInstanceName", _enemy.name,
                "position", V3(p),
                "expectedPosition", V3(EnemyPos),
                "deltaX", dx, "deltaY", dy, "deltaZ", dz,
                "toleranceXZ", PosTolXZ,
                "toleranceY", PosTolY,
                "rotationEuler", V3(e),
                "rotationDeltaDeg", rotDelta,
                "toleranceDeg", RotTolDeg,
                "testWroteTransform", false));
    }

    // ============================================================
    // 5. Encounter.NavMesh
    // ============================================================
    void AssertNavMesh()
    {
        NavMeshAgent agent = _enemy.GetComponent<NavMeshAgent>();
        MonsterBrain brain = _enemy.GetComponent<MonsterBrain>();
        NavMeshHit hit;
        bool sampled = NavMesh.SamplePosition(_enemy.transform.position, out hit, 1.0f, NavMesh.AllAreas);

        bool agentOk = agent != null && agent.enabled && agent.isOnNavMesh;
        bool brainOk = brain != null && brain.enabled && brain.agent != null;
        bool pass = agentOk && brainOk && sampled;

        _ev.Record("Encounter.NavMesh", "Encounter.NavMesh",
            "敌人 Agent 启用且 isOnNavMesh=true，既有生产 AI 未被测试禁用",
            "agentEnabled=" + (agent != null && agent.enabled) + "; isOnNavMesh=" + (agent != null && agent.isOnNavMesh) +
            "; brainEnabled=" + (brain != null && brain.enabled) + "; sampled=" + sampled,
            pass, pass ? "pass" : "product_fail", "production",
            Dict(
                "agentPresent", agent != null,
                "agentEnabled", agent != null && agent.enabled,
                "isOnNavMesh", agent != null && agent.isOnNavMesh,
                "agentTypeID", agent != null ? (object)agent.agentTypeID : null,
                "agentRadius", agent != null ? (object)agent.radius : null,
                "agentHeight", agent != null ? (object)agent.height : null,
                "agentSpeed", agent != null ? (object)agent.speed : null,
                "agentStoppingDistance", agent != null ? (object)agent.stoppingDistance : null,
                "agentBaseOffset", agent != null ? (object)agent.baseOffset : null,
                "agentHasPath", agent != null && agent.hasPath,
                "agentPathPending", agent != null && agent.pathPending,
                "agentIsStopped", agent != null && agent.isStopped,
                "brainPresent", brain != null,
                "brainEnabled", brain != null && brain.enabled,
                "brainAgentAssigned", brain != null && brain.agent != null,
                "brainStatsAssigned", brain != null && brain.stats != null,
                "navMeshSample", sampled,
                "navMeshSampleDistance", sampled ? (object)Vector3.Distance(hit.position, _enemy.transform.position) : null,
                "navMeshSamplePosition", sampled ? (object)V3(hit.position) : null,
                "testDisabledBrain", false,
                "testDisabledAgent", false));
    }

    // ============================================================
    // 6. Encounter.RealApproach
    // ============================================================
    IEnumerator AssertRealApproach()
    {
        var sm = SelectionManager.instance;
        if (sm == null)
        {
            _ev.BlockedAssertion("Encounter.RealApproach", "Encounter.RealApproach",
                "通过现有选择/移动命令从玩家出生区接近固定遭遇",
                "SelectionManager.instance 为空，无法执行生产选择链", null);
            yield break;
        }

        _knight = FindPlayerByName("Knight_Unit");
        if (_knight == null)
        {
            _ev.BlockedAssertion("Encounter.RealApproach", "Encounter.RealApproach",
                "通过现有选择/移动命令从玩家出生区接近固定遭遇",
                "未找到 Knight_Unit（生产玩家单位）", null);
            yield break;
        }
        _knightBrain = _knight.GetComponent<CharacterBrain>();
        _knightSel = _knight.GetComponent<Selectable>();
        if (_knightBrain == null || _knightSel == null || _knightBrain.agent == null)
        {
            _ev.BlockedAssertion("Encounter.RealApproach", "Encounter.RealApproach",
                "通过现有选择/移动命令从玩家出生区接近固定遭遇",
                "Knight 缺少 CharacterBrain/Selectable/NavMeshAgent，生产移动链不可用", null);
            yield break;
        }

        MethodInfo singleSelect = typeof(SelectionManager).GetMethod(
            "SingleSelect", BindingFlags.Instance | BindingFlags.NonPublic);
        if (singleSelect == null)
        {
            _ev.BlockedAssertion("Encounter.RealApproach", "Encounter.RealApproach",
                "通过现有选择/移动命令从玩家出生区接近固定遭遇",
                "SelectionManager.SingleSelect(Selectable) 不存在（生产选择链签名变化）", null);
            yield break;
        }
        singleSelect.Invoke(sm, new object[] { _knightSel });

        Vector3 startPos = _knight.transform.position;
        Vector3 enemyPos = _enemy.transform.position;
        Vector3 dest = new Vector3(EnemyPos.x, startPos.y, ApproachDestZ);
        _approachStartDist = XZDistance(startPos, enemyPos);

        NavMeshPath path = new NavMeshPath();
        bool pathOk = NavMesh.CalculatePath(startPos, dest, NavMesh.AllAreas, path);
        _approachPathStatus = path.status.ToString();
        _approachCorners = path.corners != null ? path.corners.Length : 0;

        sm.ForEachSelectedBrain(b => b.CommandMove(dest));

        NavMeshAgent agent = _knightBrain.agent;
        float elapsed = 0f;
        float prevDist = _approachStartDist;
        Vector3 prev = startPos;
        var samples = new List<object>();
        bool arrived = false;

        while (elapsed < ApproachTimeout)
        {
            yield return null;
            elapsed += Time.deltaTime;
            Vector3 p = _knight.transform.position;
            float step = Vector3.Distance(prev, p);
            if (step > _approachMaxStep) _approachMaxStep = step;
            _approachTraveled += step;
            prev = p;
            float d = XZDistance(p, enemyPos);
            if (samples.Count < 400 && Mathf.Abs(d - prevDist) > 0.5f)
            {
                samples.Add(Dict("t", elapsed, "distance", d, "position", V3(p)));
                prevDist = d;
            }
            if (!agent.pathPending && agent.hasPath && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
            { arrived = true; break; }
        }

        // 到点后等待减速结束再量终点（避免在自动减速帧采到半途位置）
        if (arrived)
        {
            float settle = 0f;
            while (settle < 1.0f && agent.velocity.sqrMagnitude > 0.0025f)
            {
                yield return null;
                settle += Time.deltaTime;
            }
        }

        Vector3 endPos = _knight.transform.position;
        _approachEndDist = XZDistance(endPos, enemyPos);
        _approachDuration = elapsed;

        bool distOk = _approachStartDist >= 11.8f && _approachEndDist <= 7.35f && _approachEndDist >= 6.6f;
        bool movedOk = _approachTraveled >= 4.0f;
        bool noWarp = _approachMaxStep <= 0.6f;
        bool pathOk2 = pathOk && path.status == NavMeshPathStatus.PathComplete && _approachCorners > 1;
        bool pass = arrived && distOk && movedOk && noWarp && pathOk2;

        _ev.Record("Encounter.RealApproach", "Encounter.RealApproach",
            "通过现有选择/移动命令从玩家出生区接近固定遭遇（无 Warp、不写坐标、不直接 SetDestination）",
            "start=" + F(_approachStartDist) + "m; end=" + F(_approachEndDist) +
            "m; moved=" + F(_approachTraveled) + "m; maxStep=" + F(_approachMaxStep) + "m; arrived=" + arrived,
            pass, pass ? "pass" : "product_fail", "production",
            Dict(
                "selectionEntry", "SelectionManager.SingleSelect(Selectable)（private，与 SelectionManager.HandleClick 同一方法；反射调用）",
                "moveEntry", "SelectionManager.ForEachSelectedBrain(b => b.CommandMove(Vector3))（与 PlayerCommandController 右键同一方法）",
                "chain", "SelectionManager.SingleSelect → SelectionManager.ForEachSelectedBrain → CharacterBrain.CommandMove(Vector3) → CharacterBrain.OnCommandMove → UnitBrain.OnCommandMove → NavMeshAgent.SetDestination",
                "commandSignature", "CharacterBrain.CommandMove(Vector3 point)（继承自 UnitBrain.CommandMove）",
                "unit", _knight.name,
                "unitInstanceId", _knight.GetInstanceID(),
                "startPosition", V3(startPos),
                "endPosition", V3(endPos),
                "destination", V3(dest),
                "enemyPosition", V3(enemyPos),
                "startDistance", _approachStartDist,
                "endDistance", _approachEndDist,
                "traveled", _approachTraveled,
                "maxSingleFrameDisplacement", _approachMaxStep,
                "durationSeconds", _approachDuration,
                "pathStatus", _approachPathStatus,
                "pathCorners", _approachCorners,
                "arrived", arrived,
                "samples", samples,
                "usedWarp", false,
                "wrotePlayerPosition", false,
                "calledAgentSetDestinationDirectly", false,
                "autoAggroInterference", "终点距敌 " + F(_approachEndDist) + "m > Knight aggroRange 6，指令段未触发自动追击"));
    }

    // ============================================================
    // 7. Regression.NoNewErrors
    // ============================================================
    IEnumerator AssertRegression(FND02A_EdgeObserver obs)
    {
        var sm = SelectionManager.instance;
        if (_knight == null) _knight = FindPlayerByName("Knight_Unit");
        if (_knight != null && _knightBrain == null) _knightBrain = _knight.GetComponent<CharacterBrain>();
        if (sm != null && _knight != null && _knightSel == null)
        {
            _knightSel = _knight.GetComponent<Selectable>();
            var singleSelect = typeof(SelectionManager).GetMethod(
                "SingleSelect", BindingFlags.Instance | BindingFlags.NonPublic);
            if (singleSelect != null && _knightSel != null) singleSelect.Invoke(sm, new object[] { _knightSel });
        }
        bool canMove = sm != null && _knight != null && _knightBrain != null && _knightBrain.agent != null;
        bool arrived = false;
        float startDist = -1f, endDist = -1f, elapsed = 0f;
        Vector3 dest = new Vector3(EnemyPos.x, _knight != null ? _knight.transform.position.y : 0f, RegressionDestZ);

        if (canMove)
        {
            Vector3 start = _knight.transform.position;
            startDist = Vector3.Distance(start, dest);
            sm.ForEachSelectedBrain(b => b.CommandMove(dest));
            NavMeshAgent agent = _knightBrain.agent;
            while (elapsed < 12f)
            {
                yield return null;
                elapsed += Time.deltaTime;
                if (!agent.pathPending && agent.hasPath && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
                { arrived = true; break; }
            }
            endDist = Vector3.Distance(_knight.transform.position, dest);
        }

        int errors = obs != null ? obs.ErrorCount : -1;
        var errList = obs != null ? obs.ErrorList() : new List<object>();
        bool pass = canMove && arrived && errors == 0;

        _ev.Record("Regression.NoNewErrors", "Regression.NoNewErrors",
            "无新增导航异常或阻塞性错误，FND-01 既有移动路线仍可用",
            "arrived=" + arrived + "; movedTo=" + F(startDist) + "→" + F(endDist) +
            "m; consoleErrors=" + errors,
            pass, pass ? "pass" : "product_fail", "production",
            Dict(
                "route", "玩家出生区 (-2.72,0,0) → (-2.72,0,-2)（生产 CommandMove 等价输入）",
                "regressionArrived", arrived,
                "regressionStartDistance", startDist,
                "regressionEndDistance", endDist,
                "regressionSeconds", elapsed,
                "consoleErrorCount", errors,
                "consoleErrors", errList,
                "errorCollectionWindow", "RuntimeInitializeOnLoadMethod(AfterSceneLoad) 起至断言结束",
                "navMeshErrors", errors > 0 ? errList : new List<object>(),
                "unityVersion", Application.unityVersion,
                "timeScale", Time.timeScale,
                "gameStarted", GameFlow.GameStarted));
    }

    // ============================================================
    // 收尾
    // ============================================================
    void Finish()
    {
        if (_finished) return;
        _finished = true;

        if (_ev != null)
        {
            _ev.WriteSummary(ScenePath, _seed, string.IsNullOrEmpty(_exitReason) ? "completed" : _exitReason,
                Dict(
                    "mode", _mode,
                    "head", GetHead(),
                    "sceneInstanceFileID", GetSceneInstanceFileID(),
                    "enemySourceGuid", GetEnemySourceGuid(),
                    "testWroteGameStarted", false,
                    "productEntry", "GameFlow.Start",
                    "enemyPosition", V3(EnemyPos)));
        }

        int code;
        if (_ev == null || _ev.Infra > 0) code = 3;
        else if (_ev.Fail > 0 || _ev.Blocked > 0) code = 2;
        else code = 0;

        Debug.Log("[FND02A] finish; mode=" + _mode + "; run=" + _run + "; exit=" + code +
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
        catch (Exception e) { Debug.LogWarning("[FND02A] exit code file write failed: " + e.Message); }

        if (FND02A_EdgeObserver.Instance != null)
            Destroy(FND02A_EdgeObserver.Instance.gameObject);
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
    static string GetHead()
    {
#if UNITY_EDITOR
        return SessionState.GetString("FND02A.Head", "unknown");
#else
        return "n/a";
#endif
    }

    static string GetSceneInstanceFileID()
    {
#if UNITY_EDITOR
        return SessionState.GetString("FND02A.SceneInstanceFileID", "none");
#else
        return "n/a";
#endif
    }

    static string GetEnemySourceGuid()
    {
#if UNITY_EDITOR
        return SessionState.GetString("FND02A.EnemySourceGuid", "none");
#else
        return "n/a";
#endif
    }

    static UnitHealth FindPlayerByName(string name)
    {
        foreach (var h in FindObjectsByType<UnitHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (h == null || h.team != Team.Player) continue;
            if (h.name == name || h.name.StartsWith(name + " ", StringComparison.Ordinal) ||
                h.name.StartsWith(name + "(", StringComparison.Ordinal))
                return h;
        }
        return null;
    }

    static List<object> PlayerIds()
    {
        var list = new List<object>();
        foreach (var h in FindObjectsByType<UnitHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (h == null || h.team != Team.Player) continue;
            list.Add(Dict("name", h.name, "instanceId", h.GetInstanceID(), "hp", h.hp, "dead", h.dead,
                          "active", h.gameObject.activeInHierarchy));
        }
        list.Sort((a, b) => ((int)((Dictionary<string, object>)a)["instanceId"]).CompareTo(
                             (int)((Dictionary<string, object>)b)["instanceId"]));
        return list;
    }

    static List<object> MonsterIds()
    {
        var list = new List<object>();
        foreach (var h in FindObjectsByType<UnitHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (h == null || h.team != Team.Monster) continue;
            list.Add(Dict("name", h.name, "instanceId", h.GetInstanceID(), "hp", h.hp,
                          "active", h.gameObject.activeInHierarchy));
        }
        return list;
    }

    static int SubscriberCount()
    {
        int total = 0;
        FieldInfo f = typeof(UnitHealth).GetField("OnDeath", BindingFlags.Instance | BindingFlags.NonPublic);
        if (f == null) return -1;
        foreach (var h in FindObjectsByType<UnitHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (h == null) continue;
            var d = f.GetValue(h) as Delegate;
            if (d != null) total += d.GetInvocationList().Length;
        }
        return total;
    }

    static float XZDistance(Vector3 a, Vector3 b)
    {
        Vector3 d = a - b; d.y = 0f;
        return d.magnitude;
    }

    static string F(float v)
    {
        return v.ToString("0.###", CultureInfo.InvariantCulture);
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

    static string GetPath(Transform t)
    {
        var parts = new List<string>();
        while (t != null) { parts.Insert(0, t.name); t = t.parent; }
        return string.Join("/", parts.ToArray());
    }
}

/// <summary>
/// 首帧观察者：由 RuntimeInitializeOnLoadMethod(AfterSceneLoad) 创建（早于所有 Start），
/// 只读 GameStarted / timeScale / Console，不写任何产品状态。
/// 用于记录"第一次 false→true"上升沿（Runner 自身创建可能晚于 GameFlow.Start）。
/// </summary>
public class FND02A_EdgeObserver : MonoBehaviour
{
    public static FND02A_EdgeObserver Instance;
    public static bool BaselineValue;
    public static float BaselineTimeScale;

    public bool FirstValue;
    public float FirstTimeScale;
    public int FirstFrame = -1;
    public float FirstTrueRealtime = -1f;
    public int FirstTrueFrame = -1;
    public int RisingEdges;
    public int UpdateFrames;
    public int ErrorCount;

    bool _prev;
    bool _firstRecorded;
    readonly List<string> _errors = new List<string>();
    readonly List<object> _transitions = new List<object>();

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _prev = BaselineValue;
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
        if (_errors.Count < 30)
            _errors.Add(type + ": " + message + (string.IsNullOrEmpty(stack) ? "" : " | " + FirstLine(stack)));
    }

    static string FirstLine(string s)
    {
        int i = s.IndexOf('\n');
        return i < 0 ? s : s.Substring(0, i);
    }

    void Update()
    {
        UpdateFrames++;
        bool v = GameFlow.GameStarted;
        if (!_firstRecorded)
        {
            _firstRecorded = true;
            FirstValue = v;
            FirstTimeScale = Time.timeScale;
            FirstFrame = Time.frameCount;
        }
        if (v != _prev)
        {
            if (v && !_prev && FirstTrueRealtime < 0f)
            {
                FirstTrueRealtime = Time.realtimeSinceStartup;
                FirstTrueFrame = Time.frameCount;
            }
            if (!v && _prev)
            {
                // false→false 回落（Esc 重开/团灭重来），单独计数
            }
            if (v && !_prev) RisingEdges++;
            if (_transitions.Count < 40)
                _transitions.Add(Dict("frame", Time.frameCount, "time", Time.realtimeSinceStartup,
                                      "from", _prev, "to", v, "timeScale", Time.timeScale));
            _prev = v;
        }
    }

    public List<object> ErrorList()
    {
        var list = new List<object>();
        for (int i = 0; i < _errors.Count; i++) list.Add(_errors[i]);
        return list;
    }

    public List<object> Transitions()
    {
        return new List<object>(_transitions);
    }

    static Dictionary<string, object> Dict(params object[] kvs)
    {
        var d = new Dictionary<string, object>();
        for (int i = 0; i + 1 < kvs.Length; i += 2) d[(string)kvs[i]] = kvs[i + 1];
        return d;
    }
}

/// <summary>进入 Play 时的自举钩子：仅在 FND02A.Active 置位时创建观察者。</summary>
public static class FND02A_BootHook
{
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AfterSceneLoadHook()
    {
        if (!SessionState.GetBool("FND02A.Active", false)) return;
        if (FND02A_EdgeObserver.Instance != null) return;

        FND02A_EdgeObserver.BaselineValue = GameFlow.GameStarted;
        FND02A_EdgeObserver.BaselineTimeScale = Time.timeScale;

        var go = new GameObject("FND02A_EdgeObserver");
        go.AddComponent<FND02A_EdgeObserver>();
        Debug.Log("[FND02A] edge observer created; baseline=" + FND02A_EdgeObserver.BaselineValue +
                  "; timeScale=" + FND02A_EdgeObserver.BaselineTimeScale +
                  "; frame=" + Time.frameCount);
    }
#endif
}
