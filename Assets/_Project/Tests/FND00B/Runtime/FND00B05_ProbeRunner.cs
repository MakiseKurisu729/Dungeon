using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Dungeon.Core;

/// <summary>
/// FND-00B-05 运行期探针（Play 模式）。
/// 只读取生产对象 + 在内存中建立夹具，不调用保存、不修改生产资产。
/// 覆盖 QA 第 11（传送）、12（团灭与游戏结束，含怪物遭遇前置数据）。
/// 顺序：11 传送 → 12 怪物数据 → 遭遇探针 → 全员倒地 → 游戏结束链 → 夹具控制探针。
/// </summary>
public class FND00B05_ProbeRunner : MonoBehaviour
{
    const string ScenePath = "Assets/Scenes/DungeonWorld.unity";
    const string FixturePrefix = "FND00B05_";

    const string FinishedKey = "FND00B05.Finished";
    const string ExitCodeKey = "FND00B05.ExitCode";

    const float PopulationWindowSeconds = 20f;
    const float EncounterWindowSeconds = 18f;
    const float GameEndWaitSeconds = 8f;
    const float FixtureControlWaitSeconds = 5f;
    const float EncounterRadiusMeters = 12f;

    FND00B05_Evidence _ev;
    int _run = 1;
    int _seed;
    float _deadline;
    bool _finished;
    string _exitReason = "";

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    IEnumerator Start()
    {
        int parsed;
        string env = Environment.GetEnvironmentVariable("FND00B05_RUN");
        if (int.TryParse(env, out parsed) && parsed > 0) _run = parsed;

        _seed = 20261103 + _run;
        UnityEngine.Random.InitState(_seed);

        _ev = new FND00B05_Evidence(_run);
        _deadline = Time.realtimeSinceStartup + 300f;

        Debug.Log("[FND00B05] runner start; run=" + _run +
                  "; seed=" + _seed +
                  "; unity=" + Application.unityVersion +
                  "; evidenceDir=" + _ev.EvidenceDir);

        float waited = 0f;
        while (SceneManager.GetActiveScene().path != ScenePath && waited < 20f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        Debug.Log("[FND00B05] active scene=" + SceneManager.GetActiveScene().path +
                  "; loaded=" + SceneManager.GetActiveScene().isLoaded +
                  "; waited=" + waited.ToString("0.00") + "s");

        yield return null;
        yield return null;

        Debug.Log("[FND00B05] env timeScale=" + Time.timeScale +
                  "; gameStarted=" + GameFlow.GameStarted +
                  "; gameFlowInstance=" + (GameFlow.instance != null) +
                  "; isWiped=" + (GameFlow.instance != null && GameFlow.instance.IsWiped) +
                  "; playerUnits=" + ActivePlayerUnits().Length +
                  "; monsterUnits=" + ScanMonsters().Length);

        yield return GroupTeleport();
        yield return GroupMonsterPopulation();
        yield return GroupEncounterProbe();
        yield return GroupPartyAllDown();
        yield return GroupGameEnd();

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

        if (_ev != null) _ev.WriteSummary(ScenePath, _seed, string.IsNullOrEmpty(_exitReason) ? "completed" : _exitReason);

        int code;
        if (_ev == null || _ev.Infra > 0) code = 3;
        else if (_ev.Fail > 0 || _ev.Blocked > 0) code = 2;
        else code = 0;

        Debug.Log("[FND00B05] finish; exit=" + code +
                  "; pass=" + (_ev != null ? _ev.Pass : -1) +
                  "; fail=" + (_ev != null ? _ev.Fail : -1) +
                  "; blocked=" + (_ev != null ? _ev.Blocked : -1) +
                  "; infra=" + (_ev != null ? _ev.Infra : -1) +
                  "; observed=" + (_ev != null ? _ev.Observed : -1) +
                  "; fixturePass=" + (_ev != null ? _ev.FixturePass : -1) +
                  "; fixtureFail=" + (_ev != null ? _ev.FixtureFail : -1));

#if UNITY_EDITOR
        SessionState.SetBool(FinishedKey, true);
        SessionState.SetInt(ExitCodeKey, code);
        try
        {
            if (_ev != null)
                System.IO.File.WriteAllText(
                    System.IO.Path.Combine(_ev.EvidenceDir, "05_ExitCode_run" + _run + ".txt"),
                    code.ToString() + Environment.NewLine,
                    new System.Text.UTF8Encoding(false));
        }
        catch (Exception e) { Debug.LogWarning("[FND00B05] exit code file write failed: " + e.Message); }
        Time.timeScale = 1f;
        EditorApplication.isPlaying = false;
#else
        Time.timeScale = 1f;
#endif
    }

    // ============================================================
    // 第 11 项：传送
    // ============================================================
    IEnumerator GroupTeleport()
    {
        Debug.Log("[FND00B05] === QA item 11: Teleport ===");

        DungeonDoor[] doors = FindObjectsByType<DungeonDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        TransitionFader[] faders = FindObjectsByType<TransitionFader>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        var teleportLike = new List<MonoBehaviour>();
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var b in behaviours)
        {
            if (b == null) continue;
            string typeName = b.GetType().Name;
            if (b is DungeonDoor ||
                typeName.IndexOf("Teleport", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("Portal", StringComparison.OrdinalIgnoreCase) >= 0)
                teleportLike.Add(b);
        }

        var loadedTeleportTypes = new List<string>();
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try { types = asm.GetTypes(); }
            catch { continue; }
            foreach (var t in types)
            {
                if (t == null || !typeof(MonoBehaviour).IsAssignableFrom(t)) continue;
                string n = t.Name;
                if (n.IndexOf("Teleport", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Portal", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n == "DungeonDoor" || n == "TransitionFader")
                    loadedTeleportTypes.Add(t.FullName + " @ " + asm.GetName().Name);
            }
        }
        loadedTeleportTypes.Sort(StringComparer.Ordinal);

        var entryList = teleportLike.Select(b => (object)Dict(
            "type", b.GetType().Name,
            "name", b.name,
            "path", GetPath(b.transform),
            "activeSelf", b.gameObject.activeSelf,
            "activeInHierarchy", b.gameObject.activeInHierarchy,
            "enabled", b.enabled,
            "hasInteractable", b.GetComponent<Interactable>() != null,
            "interactableEnabled", b.GetComponent<Interactable>() != null && b.GetComponent<Interactable>().enabled,
            "interactRange", b is DungeonDoor ? (object)((DungeonDoor)b).interactRange : null,
            "hasDungeonEntryPoint", b is DungeonDoor ? (object)(((DungeonDoor)b).dungeonEntryPoint != null) : null,
            "hasCameraPoint", b is DungeonDoor ? (object)(((DungeonDoor)b).cameraPoint != null) : null)).ToList();

        var census = Dict(
            "entryCount", teleportLike.Count,
            "dungeonDoorInstances", doors.Length,
            "transitionFaderInstances", faders.Length,
            "entries", entryList,
            "transitionFaders", faders.Select(f => (object)Dict(
                "name", f.name,
                "path", GetPath(f.transform),
                "activeSelf", f.gameObject.activeSelf,
                "activeInHierarchy", f.gameObject.activeInHierarchy,
                "enabled", f.enabled,
                "canvasGroupAssigned", f.canvasGroup != null,
                "fadeTime", f.fadeTime)).ToList(),
            "loadedTeleportRelatedTypes", loadedTeleportTypes,
            "dungeonDoorType", typeof(DungeonDoor).FullName + " @ " + typeof(DungeonDoor).Assembly.GetName().Name,
            "transitionFaderType", typeof(TransitionFader).FullName + " @ " + typeof(TransitionFader).Assembly.GetName().Name,
            "dungeonDoorScriptTypeLoaded", true,
            "searchScope", "FindObjectsByType(MonoBehaviour, Include) 运行时场景扫描；类型名含 Teleport/Portal 或 DungeonDoor",
            "note", "count=0 只表示运行期场景无可辨认入口实例，不推断代码不存在");

        _ev.Record("11", "Teleport.EntryCensus",
            Dict("requirement", "枚举场景中可辨认的传送入口/组件（含 inactive），输出数量、路径、激活与交互状态",
                 "acceptanceNote", "枚举完成不等于功能通过；无入口时只形成 OBSERVED_NO_ENTRY"),
            Dict("count", teleportLike.Count,
                 "dungeonDoorInstances", doors.Length,
                 "transitionFaderInstances", faders.Length,
                 "loadedTeleportRelatedTypeCount", loadedTeleportTypes.Count),
            teleportLike.Count > 0,
            teleportLike.Count > 0 ? "observed" : "observed_no_entry",
            "production",
            census);

        if (teleportLike.Count == 0)
        {
            Debug.Log("[FND00B05] Teleport: OBSERVED_NO_ENTRY; dungeonDoorInstances=0; faders=0");
        }

        // ---- TargetData ----
        if (doors.Length == 0)
        {
            _ev.BlockedAssertion("11", "Teleport.TargetData",
                Dict("requirement", "存在入口时输出源点坐标、配置目标坐标、目标场景/对象标识及有效性"),
                "OBSERVED_NO_ENTRY：场景无传送入口实例，无源点/目标配置可读",
                Dict("dungeonDoorInstances", doors.Length, "entryCount", teleportLike.Count,
                     "transitionFaderInstances", faders.Length));
        }
        else
        {
            var targets = new List<object>();
            bool allValid = true;
            foreach (var door in doors)
            {
                Transform point = door.dungeonEntryPoint;
                NavMeshHit hit;
                bool onNav = point != null && NavMesh.SamplePosition(point.position, out hit, 2f, NavMesh.AllAreas);
                bool valid = point != null && point.gameObject.activeInHierarchy;
                Transform camPoint = door.cameraPoint;
                allValid &= valid;
                targets.Add(Dict(
                    "entryPath", GetPath(door.transform),
                    "sourcePosition", V3(door.transform.position),
                    "targetObject", point != null ? point.name : null,
                    "targetPath", point != null ? GetPath(point) : null,
                    "targetScene", SceneManager.GetActiveScene().path,
                    "targetPosition", point != null ? (object)V3(point.position) : null,
                    "targetActiveInHierarchy", point != null && point.gameObject.activeInHierarchy,
                    "targetOnNavMesh", onNav,
                    "cameraPoint", camPoint != null ? camPoint.name : null,
                    "cameraPointPosition", camPoint != null ? (object)V3(camPoint.position) : null,
                    "interactRange", door.interactRange,
                    "valid", valid));
            }
            _ev.Record("11", "Teleport.TargetData",
                Dict("requirement", "输出源点、目标坐标、目标场景/对象标识及有效性"),
                Dict("targets", targets, "valid", allValid),
                allValid, allValid ? "pass" : "product_fail", "production",
                Dict("targets", targets));
        }

        // ---- ReachesTarget ----
        if (doors.Length == 0)
        {
            _ev.BlockedAssertion("11", "Teleport.ReachesTarget",
                Dict("requirement", "通过实际交互入口触发，断言角色到达配置目标坐标容差内",
                     "toleranceMeters", 0.5),
                "OBSERVED_NO_ENTRY：场景无传送入口，无法沿生产交互入口触发",
                Dict("entryCount", teleportLike.Count, "dungeonDoorInstances", 0));
            yield return TeleportFixtureProbe();
        }
        else
        {
            yield return TeleportProductionProbe(doors[0]);
        }
    }

    /// <summary>
    /// 夹具探针：内存中创建 DungeonDoor + TransitionFader，调用生产 Enter()，观察玩家落点。
    /// 仅证明实现机制，不作为第 11 项通过依据；不模拟 Input.GetKeyDown(F) 门控。
    /// </summary>
    IEnumerator TeleportFixtureProbe()
    {
        UnitHealth[] players = ActivePlayerUnits().OrderBy(h => h.name, StringComparer.Ordinal).ToArray();
        if (players.Length == 0)
        {
            _ev.InfraFailure("11", "Teleport.ReachesTarget.fixtureProbe", "场景无存活玩家单位，无法建立传送夹具", "");
            yield break;
        }

        Vector3[] before = players.Select(h => h.transform.position).ToArray();
        Vector3 basePos = before[0];
        Vector3 wanted = basePos + new Vector3(20f, 0f, 0f);
        Vector3 targetPos = wanted;
        NavMeshHit navHit;
        bool targetOnNavMesh = NavMesh.SamplePosition(wanted, out navHit, 10f, NavMesh.AllAreas);
        if (targetOnNavMesh) targetPos = navHit.position;

        GameObject targetGO = null;
        GameObject cameraPointGO = null;
        GameObject entryGO = null;
        GameObject faderGO = null;
        Transform camRig = null;
        Vector3 camRigPos = Vector3.zero;
        DebugCameraSwitcher camSwitcher = null;
        Transform previousFocus = null;
        bool faderActivated = false;
        bool faderCompleted = false;
        bool invoked = false;
        string invokeError = null;
        float elapsed = 0f;
        float t0 = 0f;
        var perPlayer = new List<object>();
        bool allNear = false;
        bool allMovedFar = false;

        try
        {
            targetGO = new GameObject(FixturePrefix + "TeleportTarget");
            targetGO.transform.position = targetPos;

            cameraPointGO = new GameObject(FixturePrefix + "CameraPoint");
            cameraPointGO.transform.position = targetPos + Vector3.up * 10f;

            entryGO = new GameObject(FixturePrefix + "TeleportEntry");
            entryGO.transform.position = basePos + new Vector3(0f, 0f, 1f);
            var door = entryGO.AddComponent<DungeonDoor>();
            door.dungeonEntryPoint = targetGO.transform;
            door.cameraPoint = cameraPointGO.transform;

            faderGO = new GameObject(FixturePrefix + "TransitionFader");
            faderGO.SetActive(false);
            var group = faderGO.AddComponent<CanvasGroup>();
            var fader = faderGO.AddComponent<TransitionFader>();
            fader.canvasGroup = group;
            fader.fadeTime = 0.15f;
            faderGO.SetActive(true);
            faderActivated = TransitionFader.instance == fader;

            camSwitcher = FindObjectOfType<DebugCameraSwitcher>();
            previousFocus = camSwitcher != null ? camSwitcher.focusTarget : null;
            camRig = Camera.main != null ? Camera.main.transform.root : null;
            camRigPos = camRig != null ? camRig.position : Vector3.zero;

            MethodInfo enter = typeof(DungeonDoor).GetMethod("Enter", BindingFlags.Instance | BindingFlags.NonPublic);
            if (enter == null) throw new MissingMethodException("DungeonDoor.Enter 未找到");

            t0 = Time.realtimeSinceStartup;
            enter.Invoke(door, null);
            invoked = true;
        }
        catch (Exception e)
        {
            invokeError = e.GetType().Name + ": " + e.Message;
            Debug.LogError("[FND00B05] teleport fixture failed: " + e);
        }

        if (invoked && invokeError == null)
        {
            while (Time.realtimeSinceStartup - t0 < 5f)
            {
                bool allMoved = true;
                for (int i = 0; i < players.Length; i++)
                    if (Vector3.Distance(players[i].transform.position, before[i]) < 0.5f) { allMoved = false; break; }

                bool fadeAlphaDone = TransitionFader.instance == null ||
                                     TransitionFader.instance.canvasGroup == null ||
                                     TransitionFader.instance.canvasGroup.alpha <= 0.01f;
                if (allMoved && fadeAlphaDone) break;
                yield return null;
            }
            elapsed = Time.realtimeSinceStartup - t0;

            var expectedOffsets = new List<Vector3>();
            for (int i = 0; i < players.Length; i++)
                expectedOffsets.Add(new Vector3(i % 2 == 0 ? -1.2f : 1.2f, 0f, (i / 2) * 1.8f));

            allNear = true;
            allMovedFar = true;
            for (int i = 0; i < players.Length; i++)
            {
                Vector3 pos = players[i].transform.position;
                float bestError = float.MaxValue;
                int bestIndex = -1;
                for (int j = 0; j < expectedOffsets.Count; j++)
                {
                    float d = Vector3.Distance(pos, targetPos + expectedOffsets[j]);
                    if (d < bestError) { bestError = d; bestIndex = j; }
                }
                float moved = Vector3.Distance(pos, before[i]);
                allNear &= bestError <= 0.5f;
                allMovedFar &= moved > 10f;
                perPlayer.Add(Dict(
                    "id", players[i].GetInstanceID(),
                    "name", players[i].name,
                    "path", GetPath(players[i].transform),
                    "before", V3(before[i]),
                    "after", V3(pos),
                    "movedDistance", moved,
                    "distanceToTargetPoint", Vector3.Distance(pos, targetPos),
                    "nearestExpectedOffsetIndex", bestIndex,
                    "nearestExpectedOffset", bestIndex >= 0 ? (object)V3(expectedOffsets[bestIndex]) : null,
                    "offsetError", bestError,
                    "withinTolerance", bestError <= 0.5f));
            }

            faderCompleted = TransitionFader.instance == null ||
                             TransitionFader.instance.canvasGroup == null ||
                             TransitionFader.instance.canvasGroup.alpha <= 0.01f;
        }

        // 让过场协程完成最后一步（把 blocksRaycasts 复位）后再记录
        yield return null;

        bool focusOk = camSwitcher == null || (targetGO != null && camSwitcher.focusTarget == targetGO.transform);
        bool cameraRigOk = camRig == null || cameraPointGO == null ||
                           Vector3.Distance(camRig.position, cameraPointGO.transform.position) <= 0.01f;

        bool pass = invoked && invokeError == null && faderActivated && faderCompleted &&
                    allNear && allMovedFar && focusOk;

        _ev.Record("11", "Teleport.ReachesTarget.fixtureProbe",
            Dict("requirement", "内存夹具：调用生产 DungeonDoor.Enter() 后，所有存活玩家落在 dungeonEntryPoint + 生产偏移（容差 0.5m），过场完成",
                 "toleranceMeters", 0.5,
                 "note", "不模拟 Input.GetKeyDown(KeyCode.F) 门控；仅证明实现机制，不作为第 11 项通过依据"),
            Dict("invoked", invoked, "invokeError", invokeError, "elapsedSeconds", elapsed,
                 "faderActivated", faderActivated, "faderCompleted", faderCompleted,
                 "allPlayersWithinTolerance", allNear, "allPlayersMovedOver10m", allMovedFar,
                 "cameraFocusSet", focusOk, "cameraRigMoved", cameraRigOk,
                 "playerCount", players.Length),
            pass, pass ? "fixture_pass" : "fixture_fail", "fixture_probe",
            Dict("targetPosition", V3(targetPos), "targetOnNavMesh", targetOnNavMesh,
                 "targetWantedPosition", V3(wanted),
                 "entryPosition", entryGO != null ? (object)V3(entryGO.transform.position) : null,
                 "cameraPointPosition", cameraPointGO != null ? (object)V3(cameraPointGO.transform.position) : null,
                 "cameraRigExists", camRig != null,
                 "cameraRigPositionAfter", camRig != null ? (object)V3(camRig.position) : null,
                 "players", perPlayer,
                 "productionSceneEntryCount", 0,
                 "productionSceneFaderCount", 0,
                 "cameraRigNote", "cameraRigMoved 在过场结束后读取；DebugCameraSwitcher 会在后续帧按焦点重算相机位置，false 仅代表该时刻未停在 cameraPoint，不判为接线失败",
                 "note", "生产场景无入口/无 TransitionFader；夹具自行补齐以验证 Enter() 的落点与过场实现"));

        // ---- 清理：恢复玩家位置/相机，销毁夹具 ----
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null) continue;
            var agent = players[i].GetComponent<NavMeshAgent>();
            if (agent != null && agent.enabled && agent.isOnNavMesh) agent.Warp(before[i]);
            else players[i].transform.position = before[i];
        }
        if (camRig != null) camRig.position = camRigPos;
        if (camSwitcher != null && previousFocus != null) camSwitcher.SetFocus(previousFocus);

        if (entryGO != null) Destroy(entryGO);
        if (faderGO != null) Destroy(faderGO);
        if (targetGO != null) Destroy(targetGO);
        if (cameraPointGO != null) Destroy(cameraPointGO);

        yield return null;
        yield return null;
        Debug.Log("[FND00B05] teleport fixture cleaned; players restored");
    }

    /// <summary>生产入口存在时的复用路径：反射调用其私有 Enter()（按键门控未模拟）。</summary>
    IEnumerator TeleportProductionProbe(DungeonDoor door)
    {
        UnitHealth[] players = ActivePlayerUnits().OrderBy(h => h.name, StringComparer.Ordinal).ToArray();
        Vector3[] before = players.Select(h => h.transform.position).ToArray();
        Vector3 targetPos = door.dungeonEntryPoint != null ? door.dungeonEntryPoint.position : door.transform.position;
        bool invoked = false;
        string error = null;
        try
        {
            MethodInfo enter = typeof(DungeonDoor).GetMethod("Enter", BindingFlags.Instance | BindingFlags.NonPublic);
            if (enter == null) throw new MissingMethodException("DungeonDoor.Enter 未找到");
            enter.Invoke(door, null);
            invoked = true;
        }
        catch (Exception e) { error = e.GetType().Name + ": " + e.Message; }

        float t0 = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - t0 < 5f) yield return null;

        var perPlayer = new List<object>();
        bool allNear = players.Length > 0;
        for (int i = 0; i < players.Length; i++)
        {
            float d = Vector3.Distance(players[i].transform.position, targetPos);
            allNear &= d <= 3f;
            perPlayer.Add(Dict("name", players[i].name, "before", V3(before[i]),
                               "after", V3(players[i].transform.position), "distanceToTarget", d));
        }

        bool pass = invoked && error == null && allNear;
        _ev.Record("11", "Teleport.ReachesTarget",
            Dict("requirement", "通过实际交互入口触发，断言角色到达配置目标坐标容差内",
                 "toleranceMeters", 3, "note", "反射调用生产 Enter()；Input.GetKeyDown(F) 按键门控未模拟"),
            Dict("invoked", invoked, "error", error, "allPlayersNearTarget", allNear),
            pass, pass ? "pass" : "product_fail", "production",
            Dict("entryPath", GetPath(door.transform), "targetPosition", V3(targetPos), "players", perPlayer));
    }

    // ============================================================
    // 第 12 项：怪物前置数据
    // ============================================================
    IEnumerator GroupMonsterPopulation()
    {
        Debug.Log("[FND00B05] === QA item 12: Monster.Population ===");

        UnitHealth[] initial = ScanMonsters();
        var initialList = initial.Select(h => (object)MonsterData(h)).ToList();
        var spawners = ScanSpawners();

        var seen = new HashSet<int>(initial.Select(h => h.GetInstanceID()));
        var spawned = new List<object>();
        float start = Time.realtimeSinceStartup;
        int frames = 0;

        while (Time.realtimeSinceStartup - start < PopulationWindowSeconds)
        {
            foreach (var m in ScanMonsters())
            {
                if (m == null) continue;
                int id = m.GetInstanceID();
                if (!seen.Contains(id))
                {
                    seen.Add(id);
                    spawned.Add(MonsterData(m));
                }
            }
            frames++;
            yield return null;
        }

        float elapsed = Time.realtimeSinceStartup - start;
        UnitHealth[] final = ScanMonsters();

        _ev.Record("12", "Monster.Population",
            Dict("requirement", "团灭夹具前输出初始怪物数量、固定窗口内新增生成数量、怪物 ID/位置、生成器数量与状态",
                 "windowSeconds", PopulationWindowSeconds,
                 "acceptanceNote", "只采集数据；生成数量为 0 原样输出，不推断原因"),
            Dict("initialCount", initial.Length,
                 "newSpawnCount", spawned.Count,
                 "finalCount", final.Length,
                 "spawnerCount", spawners.Count,
                 "windowSeconds", elapsed,
                 "frames", frames,
                 "timeScale", Time.timeScale),
            true, "observed", "production",
            Dict("initialMonsters", initialList,
                 "newMonsters", spawned,
                 "finalMonsters", ScanMonsters().Select(h => (object)MonsterData(h)).ToList(),
                 "spawners", spawners,
                 "scenePath", SceneManager.GetActiveScene().path,
                 "note", "观察窗口内未新增生成时 newSpawnCount=0 原样保留；不写成“怪物不存在的原因”"));

        Debug.Log("[FND00B05] Monster.Population done; initial=" + initial.Length +
                  "; new=" + spawned.Count + "; spawners=" + spawners.Count);
    }

    IEnumerator GroupEncounterProbe()
    {
        Debug.Log("[FND00B05] === QA item 12: Monster.NavMeshState + EncounterProbe ===");

        UnitHealth[] monsters = ScanMonsters();
        var playerAgents = ActivePlayerUnits().Select(h => (object)AgentData(h, h.transform.position)).ToList();

        if (monsters.Length == 0)
        {
            _ev.Record("12", "Monster.NavMeshState",
                Dict("requirement", "逐只输出 NavMeshAgent enabled / isOnNavMesh / pathPending / hasPath / 位置与窗口位移",
                     "acceptanceNote", "数据不足时标未确认，不把“未生成或怪物无法移动”任一可能写成原因"),
                Dict("monsterCount", 0, "state", "未确认",
                     "reason", "运行期场景无 Team.Monster 单位，无法逐只采集"),
                false, "observed_unconfirmed", "production",
                Dict("monsterCount", 0,
                     "referencePlayerAgents", playerAgents,
                     "navMeshSampleAtPlayers", NavMeshSampleSummary(),
                     "note", "referencePlayerAgents 仅为参照，不用于推断怪物状态"));
        }
        else
        {
            var before = monsters.ToDictionary(m => m.GetInstanceID(), m => m.transform.position);
            yield return new WaitForSecondsRealtime(1f);
            var states = new List<object>();
            bool complete = true;
            foreach (var m in monsters)
            {
                if (m == null) { complete = false; continue; }
                int id = m.GetInstanceID();
                Vector3 startPos = before.ContainsKey(id) ? before[id] : m.transform.position;
                states.Add(AgentData(m, startPos));
            }
            _ev.Record("12", "Monster.NavMeshState",
                Dict("requirement", "逐只输出 NavMeshAgent enabled / isOnNavMesh / pathPending / hasPath / 位置与窗口位移"),
                Dict("monsterCount", monsters.Length, "states", states),
                complete, complete ? "observed" : "observed_unconfirmed", "production",
                Dict("monsters", states, "referencePlayerAgents", playerAgents,
                     "note", "只记录数据；不分析无法移动的原因"));
        }

        // ---- 遭遇探针：固定路线 + 固定窗口 ----
        UnitHealth[] players = ActivePlayerUnits().OrderBy(h => h.name, StringComparer.Ordinal).ToArray();
        if (players.Length == 0)
        {
            _ev.BlockedAssertion("12", "Monster.EncounterProbe",
                Dict("requirement", "沿可重复观察路径/固定窗口记录遭遇事件数、最近距离与怪物位移"),
                "场景无存活玩家单位，无法建立观察路径", Dict("monsterCount", monsters.Length));
            yield break;
        }

        UnitHealth probe = players[0];
        NavMeshAgent agent = probe.GetComponent<NavMeshAgent>();
        Vector3 origin = probe.transform.position;
        var route = new List<Vector3>();
        for (int i = 0; i < 6; i++)
        {
            float ang = i * 60f * Mathf.Deg2Rad;
            Vector3 wanted = origin + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * 22f;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(wanted, out hit, 8f, NavMesh.AllAreas)) route.Add(hit.position);
        }

        int waypointIndex = 0;
        bool waypointSet = false;
        float waypointSetAt = 0f;
        int waypointsReached = 0;
        int encounterEvents = 0;
        bool insideEncounter = false;
        float nearest = float.MaxValue;
        var firstPos = new Dictionary<int, Vector3>();
        var lastPos = new Dictionary<int, Vector3>();
        var names = new Dictionary<int, string>();
        int frameCount = 0;
        float start = Time.realtimeSinceStartup;

        while (Time.realtimeSinceStartup - start < EncounterWindowSeconds)
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh && route.Count > 0)
            {
                if (!waypointSet && waypointIndex < route.Count)
                {
                    agent.SetDestination(route[waypointIndex]);
                    waypointSet = true;
                    waypointSetAt = Time.realtimeSinceStartup;
                }
                else if (waypointSet)
                {
                    bool timeout = Time.realtimeSinceStartup - waypointSetAt > 4f;
                    bool arrived = !agent.pathPending && agent.hasPath &&
                                   agent.remainingDistance <= agent.stoppingDistance + 1.5f;
                    if (timeout || arrived)
                    {
                        waypointsReached++;
                        waypointIndex++;
                        waypointSet = false;
                    }
                }
            }

            foreach (var m in ScanMonsters())
            {
                if (m == null) continue;
                int id = m.GetInstanceID();
                if (!firstPos.ContainsKey(id)) { firstPos[id] = m.transform.position; names[id] = m.name; }
                lastPos[id] = m.transform.position;
                float d = Vector3.Distance(m.transform.position, probe.transform.position);
                if (d < nearest) nearest = d;
                if (d <= EncounterRadiusMeters)
                {
                    if (!insideEncounter) { encounterEvents++; insideEncounter = true; }
                }
                else insideEncounter = false;
            }

            frameCount++;
            yield return null;
        }

        float elapsed = Time.realtimeSinceStartup - start;
        Vector3 endPos = probe.transform.position;
        var displacements = new List<object>();
        foreach (var kv in firstPos)
        {
            Vector3 last = lastPos.ContainsKey(kv.Key) ? lastPos[kv.Key] : kv.Value;
            displacements.Add(Dict(
                "id", kv.Key,
                "name", names.ContainsKey(kv.Key) ? names[kv.Key] : "unknown",
                "start", V3(kv.Value),
                "end", V3(last),
                "displacement", Vector3.Distance(kv.Value, last)));
        }

        _ev.Record("12", "Monster.EncounterProbe",
            Dict("requirement", "固定观察路径/窗口内记录遭遇事件数、最近距离与怪物位移，断言数据已完整采集",
                 "windowSeconds", EncounterWindowSeconds,
                 "encounterRadiusMeters", EncounterRadiusMeters,
                 "acceptanceNote", "只复核差距，不扩展为怪物 AI 修复；无怪物时记录 0 与 null"),
            Dict("monsterCountAtStart", monsters.Length,
                 "monsterCountAtEnd", ScanMonsters().Length,
                 "encounterEvents", encounterEvents,
                 "nearestDistanceMeters", nearest == float.MaxValue ? (object)null : nearest,
                 "monsterDisplacementRecords", displacements.Count,
                 "probeActor", probe.name,
                 "routeWaypointsRequested", 6,
                 "routeWaypointsSampled", route.Count,
                 "routeWaypointsReached", waypointsReached,
                 "traveledDistanceMeters", Vector3.Distance(origin, endPos),
                 "elapsedSeconds", elapsed,
                 "frames", frameCount,
                 "timeScale", Time.timeScale),
            true, "observed", "production",
            Dict("probeActorPath", GetPath(probe.transform),
                 "startPosition", V3(origin),
                 "endPosition", V3(endPos),
                 "route", route.Select(p => (object)V3(p)).ToList(),
                 "monsterDisplacements", displacements,
                 "agentAvailable", agent != null,
                 "agentOnNavMesh", agent != null && agent.isOnNavMesh,
                 "note", "观察路径为内存采样点，未保存场景；玩家位置在探针结束后恢复"));

        // 恢复探针演员位置
        if (agent != null && agent.enabled && agent.isOnNavMesh) agent.Warp(origin);
        else probe.transform.position = origin;
        yield return null;
    }

    // ============================================================
    // 团灭夹具与游戏结束链
    // ============================================================
    IEnumerator GroupPartyAllDown()
    {
        Debug.Log("[FND00B05] === QA item 12: Party.AllDown ===");

        UnitHealth[] players = ActivePlayerUnits().OrderBy(h => h.name, StringComparer.Ordinal).ToArray();
        int withRescue = players.Count(h => h.GetComponent<DownedRescue>() != null);

        if (players.Length == 0 || withRescue != players.Length)
        {
            _ev.BlockedAssertion("12", "Party.AllDown",
                Dict("requirement", "通过实际伤害/倒地状态入口建立全员倒地，逐角色输出 ID/倒地时间/状态/存活计数"),
                "场景玩家单位缺失或并非全部具备 DownedRescue（players=" + players.Length + ", withDownedRescue=" + withRescue + "）",
                Dict("players", players.Select(h => (object)Dict(
                    "name", h.name, "path", GetPath(h.transform),
                    "hasDownedRescue", h.GetComponent<DownedRescue>() != null)).ToList()));
            yield break;
        }

        var beforeList = new List<object>();
        var downEvents = new List<object>();
        var deathTimes = new Dictionary<int, float>();
        var handlers = new Dictionary<int, Action>();

        foreach (var h in players)
        {
            var dr = h.GetComponent<DownedRescue>();
            beforeList.Add(Dict(
                "id", h.GetInstanceID(),
                "name", h.name,
                "path", GetPath(h.transform),
                "hp", h.hp,
                "maxHp", h.maxHp,
                "dead", h.dead,
                "isDown", dr != null && dr.IsDown,
                "team", h.team.ToString()));
            int id = h.GetInstanceID();
            UnitHealth captured = h;
            Action handler = () =>
            {
                deathTimes[id] = Time.realtimeSinceStartup;
                downEvents.Add(Dict("event", "OnDeath", "id", id, "name", captured.name,
                                    "t", Time.realtimeSinceStartup, "hp", captured.hp));
            };
            handlers[id] = handler;
            h.OnDeath += handler;
        }

        int standingBefore = CountStanding(players);
        var downedAt = new Dictionary<int, float>();

        foreach (var h in players)
        {
            h.ApplyDamage(h.maxHp + 10f, h.transform.position, null);
        }
        yield return null;
        yield return null;

        var afterList = new List<object>();
        bool allDown = players.Length > 0;
        foreach (var h in players)
        {
            var dr = h.GetComponent<DownedRescue>();
            bool isDown = dr != null && dr.IsDown;
            allDown &= isDown && h.dead;
            downedAt[h.GetInstanceID()] = dr != null ? Time.realtimeSinceStartup : -1f;
            afterList.Add(Dict(
                "id", h.GetInstanceID(),
                "name", h.name,
                "path", GetPath(h.transform),
                "hp", h.hp,
                "maxHp", h.maxHp,
                "dead", h.dead,
                "isDown", isDown,
                "brainEnabled", dr != null && dr.brain != null && dr.brain.enabled,
                "agentEnabled", dr != null && dr.agent != null && dr.agent.enabled,
                "downedAt", deathTimes.ContainsKey(h.GetInstanceID()) ? (object)deathTimes[h.GetInstanceID()] : null));
        }

        int standingAfter = CountStanding(players);

        _ev.Record("12", "Party.AllDown",
            Dict("requirement", "通过实际伤害/倒地状态入口令全员倒地；逐角色输出 ID/倒地时间/状态与存活计数",
                 "expectedAliveStandingCount", 0,
                 "note", "不调用 GameOver/IsWiped，只施加生产伤害管线"),
            Dict("playerCount", players.Length,
                 "allDown", allDown,
                 "aliveStandingBefore", standingBefore,
                 "aliveStandingAfter", standingAfter,
                 "downedRescueAllCount", DownedRescue.AllDowned.Count),
            allDown && standingAfter == 0, (allDown && standingAfter == 0) ? "pass" : "product_fail", "production",
            Dict("before", beforeList, "after", afterList,
                 "onDeathEvents", downEvents,
                 "damageEntry", "UnitHealth.ApplyDamage(maxHp + 10)（与 FND-00B-04 同一真实伤害入口）",
                 "gameStartedAtDown", GameFlow.GameStarted,
                 "note", "全员倒地后不主动调用任何游戏结束方法"));

        foreach (var h in players)
        {
            if (h == null) continue;
            int id = h.GetInstanceID();
            Action handler;
            if (handlers.TryGetValue(id, out handler)) h.OnDeath -= handler;
        }

        Debug.Log("[FND00B05] Party.AllDown done; allDown=" + allDown + "; standingAfter=" + standingAfter);
    }

    IEnumerator GroupGameEnd()
    {
        Debug.Log("[FND00B05] === QA item 12: GameEnd.TriggerChain + FinalState ===");

        UnitHealth[] players = ActivePlayerUnits();
        int standingAtStart = CountStanding(players);
        float start = Time.realtimeSinceStartup;
        bool triggered = false;
        float triggerAt = -1f;
        int risingEdges = 0;
        bool lastWiped = false;
        var samples = new List<object>();
        int sampleCounter = 0;

        while (Time.realtimeSinceStartup - start < GameEndWaitSeconds)
        {
            bool wiped = GameFlow.instance != null && GameFlow.instance.IsWiped;
            int standing = CountStanding(ActivePlayerUnits());
            if (wiped && !lastWiped)
            {
                risingEdges++;
                if (triggerAt < 0f) triggerAt = Time.realtimeSinceStartup - start;
            }
            if (wiped) triggered = true;
            lastWiped = wiped;

            if (sampleCounter % 30 == 0)
            {
                samples.Add(Dict("t", Time.realtimeSinceStartup - start,
                                 "aliveStanding", standing,
                                 "isWiped", wiped,
                                 "gameStarted", GameFlow.GameStarted,
                                 "timeScale", Time.timeScale,
                                 "message", GameData.message));
            }
            sampleCounter++;
            yield return null;
        }

        float waitElapsed = Time.realtimeSinceStartup - start;
        int edges = risingEdges;
        bool wipedNow = GameFlow.instance != null && GameFlow.instance.IsWiped;
        int standingNow = CountStanding(ActivePlayerUnits());
        bool exactlyOnce = triggered && edges == 1;

        // 稳定窗口：若已触发，再观察 2 秒是否二次触发/状态翻转
        bool stable = true;
        if (triggered)
        {
            float stableStart = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - stableStart < 2f)
            {
                bool w = GameFlow.instance != null && GameFlow.instance.IsWiped;
                if (w && !lastWiped) edges++;
                lastWiped = w;
                if (!w) stable = false;
                yield return null;
            }
        }

        exactlyOnce = triggered && edges == 1;
        bool pass = triggered && exactlyOnce && stable;

        var hud = FindObjectOfType<HUD>();
        string hudText = hud != null && hud.messageText != null ? hud.messageText.text : null;

        _ev.Record("12", "GameEnd.TriggerChain",
            Dict("requirement", "全员倒地 → aliveCount=0 → 游戏结束事件/状态 → UI 状态，限定时间内触发且仅触发一次",
                 "maxWaitSeconds", GameEndWaitSeconds,
                 "acceptanceNote", "不直接调用游戏结束方法来伪造触发链"),
            Dict("triggered", triggered,
                 "triggerSeconds", triggerAt < 0f ? (object)null : triggerAt,
                 "waitSeconds", waitElapsed,
                 "risingEdges", edges,
                 "exactlyOnce", exactlyOnce,
                 "stableAfterTrigger", stable,
                 "aliveStandingAtStart", standingAtStart,
                 "aliveStandingAtEnd", standingNow,
                 "isWipedAtEnd", wipedNow,
                 "gameStarted", GameFlow.GameStarted,
                 "timeScale", Time.timeScale,
                 "message", GameData.message,
                 "hudMessageText", hudText),
            pass, pass ? "pass" : "product_fail", "production",
            Dict("chain", BuildTriggerChain(samples, triggered, triggerAt),
                 "samples", samples,
                 "gameFlowInstanceExists", GameFlow.instance != null,
                 "gameStarted", GameFlow.GameStarted,
                 "note", "全员倒地后按生产 Update 判定等待；未触发时原样记录等待时长与实际状态"));

        // ---- FinalState ----
        bool finalPass = wipedNow && Mathf.Approximately(Time.timeScale, 0f);
        _ev.Record("12", "GameEnd.FinalState",
            Dict("requirement", "输出最终游戏状态、相关 UI active 状态与输入锁定状态",
                 "expected", "IsWiped=true, timeScale=0, 结束提示/UI 状态可见"),
            Dict("isWiped", wipedNow,
                 "timeScale", Time.timeScale,
                 "gameStarted", GameFlow.GameStarted,
                 "paused", PauseManager.instance != null && PauseManager.instance.IsPaused,
                 "message", GameData.message,
                 "hudMessageText", hudText,
                 "waitedSeconds", waitElapsed,
                 "inputLockedByTimeScale", Mathf.Approximately(Time.timeScale, 0f)),
            finalPass, finalPass ? "pass" : "product_fail", "production",
            CollectUiState(hud));

        // ---- 夹具控制：只在夹具内补齐 GameStarted 前置，隔离验证团灭链本身 ----
        yield return GameEndFixtureControl();

        // 夹具结束后恢复时间缩放，避免影响编辑器退出
        Time.timeScale = 1f;
        Debug.Log("[FND00B05] GameEnd group done; triggered=" + triggered + "; edges=" + edges + "; finalPass=" + finalPass);
    }

    IEnumerator GameEndFixtureControl()
    {
        if (GameFlow.instance == null)
        {
            _ev.InfraFailure("12", "GameEnd.TriggerChain.fixtureControl", "场景无 GameFlow 实例，无法验证团灭链", "");
            yield break;
        }

        bool previousGameStarted = GameFlow.GameStarted;
        bool alreadyWiped = GameFlow.instance.IsWiped;
        float previousTimeScale = Time.timeScale;

        bool triggered = alreadyWiped;
        float triggerAt = alreadyWiped ? 0f : -1f;
        int edges = alreadyWiped ? 1 : 0;
        bool lastWiped = alreadyWiped;
        float start = Time.realtimeSinceStartup;

        // GameFlow.GameStarted 的唯一生产写入者是 MainMenuPanel.OnClose；本场景无该面板。
        // 这里仅在夹具内设置该前置，不调用任何游戏结束方法。
        GameFlow.GameStarted = true;

        while (!triggered && Time.realtimeSinceStartup - start < FixtureControlWaitSeconds)
        {
            bool wiped = GameFlow.instance.IsWiped;
            if (wiped && !lastWiped)
            {
                edges++;
                triggerAt = Time.realtimeSinceStartup - start;
            }
            if (wiped) triggered = true;
            lastWiped = wiped;
            yield return null;
        }

        bool stable = true;
        if (triggered)
        {
            float stableStart = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - stableStart < 2f)
            {
                bool w = GameFlow.instance.IsWiped;
                if (w && !lastWiped) edges++;
                lastWiped = w;
                if (!w) stable = false;
                yield return null;
            }
        }

        float elapsed = Time.realtimeSinceStartup - start;
        bool wipedNow = GameFlow.instance.IsWiped;
        bool pass = triggered && wipedNow && Mathf.Approximately(Time.timeScale, 0f) && edges == 1 && stable;
        var hud = FindObjectOfType<HUD>();
        string hudText = hud != null && hud.messageText != null ? hud.messageText.text : null;

        _ev.Record("12", "GameEnd.TriggerChain.fixtureControl",
            Dict("requirement", "夹具内补齐 GameStarted=true 后，全员倒地应在限定时间内触发团灭且仅触发一次",
                 "precondition", "GameFlow.GameStarted=true（生产写入者 MainMenuPanel 在场景中缺席）",
                 "maxWaitSeconds", FixtureControlWaitSeconds,
                 "note", "只隔离验证团灭链本身，不作为第 12 项产品通过依据"),
            Dict("triggered", triggered,
                 "triggerSeconds", triggerAt < 0f ? (object)null : triggerAt,
                 "elapsedSeconds", elapsed,
                 "risingEdges", edges,
                 "stableAfterTrigger", stable,
                 "isWiped", wipedNow,
                 "timeScale", Time.timeScale,
                 "message", GameData.message,
                 "hudMessageText", hudText,
                 "previousGameStarted", previousGameStarted,
                 "previousTimeScale", previousTimeScale),
            pass, pass ? "fixture_pass" : "fixture_fail", "fixture_probe",
            Dict("ui", CollectUiState(hud),
                 "gameStartedWas", previousGameStarted,
                 "gameStartedSetByFixture", true,
                 "note", "该夹具证明：GameStarted=true 时团灭链可触发；产品路径是否可达由 GameEnd.TriggerChain 单独记录"));

        GameFlow.GameStarted = previousGameStarted;
        Time.timeScale = 1f;
        yield return null;
        yield return null;
    }

    // ============================================================
    // 工具
    // ============================================================
    UnitHealth[] ActivePlayerUnits()
    {
        return FindObjectsByType<UnitHealth>(FindObjectsSortMode.None)
            .Where(h => h != null && h.team == Team.Player && !h.name.StartsWith(FixturePrefix, StringComparison.Ordinal))
            .ToArray();
    }

    UnitHealth[] ScanMonsters()
    {
        return FindObjectsByType<UnitHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(h => h != null && h.team == Team.Monster && !h.name.StartsWith(FixturePrefix, StringComparison.Ordinal))
            .ToArray();
    }

    static int CountStanding(UnitHealth[] players)
    {
        int standing = 0;
        foreach (var h in players)
        {
            if (h == null || h.dead) continue;
            var dr = h.GetComponent<DownedRescue>();
            bool down = dr != null && dr.IsDown;
            if (!down) standing++;
        }
        return standing;
    }

    List<object> ScanSpawners()
    {
        var result = new List<object>();
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var b in behaviours)
        {
            if (b == null) continue;
            string typeName = b.GetType().Name;
            if (typeName.IndexOf("Spawner", StringComparison.OrdinalIgnoreCase) < 0) continue;
            var entry = Dict(
                "type", typeName,
                "name", b.name,
                "path", GetPath(b.transform),
                "activeSelf", b.gameObject.activeSelf,
                "activeInHierarchy", b.gameObject.activeInHierarchy,
                "enabled", b.enabled);
            var skeleton = b as SkeletonSpawner;
            if (skeleton != null)
            {
                entry["groupCountMin"] = skeleton.groupCountMin;
                entry["groupCountMax"] = skeleton.groupCountMax;
                entry["groupSizeMin"] = skeleton.groupSizeMin;
                entry["groupSizeMax"] = skeleton.groupSizeMax;
                entry["mapRadius"] = skeleton.mapRadius;
                entry["minDistanceFromCamp"] = skeleton.minDistanceFromCamp;
                entry["spawnDelay"] = skeleton.spawnDelay;
                entry["prefabCount"] = skeleton.skeletonPrefabs != null ? skeleton.skeletonPrefabs.Length : 0;
            }
            result.Add(entry);
        }
        return result;
    }

    Dictionary<string, object> MonsterData(UnitHealth h)
    {
        return Dict(
            "id", h.GetInstanceID(),
            "name", h.name,
            "path", GetPath(h.transform),
            "team", h.team.ToString(),
            "hp", h.hp,
            "maxHp", h.maxHp,
            "dead", h.dead,
            "activeSelf", h.gameObject.activeSelf,
            "activeInHierarchy", h.gameObject.activeInHierarchy,
            "position", V3(h.transform.position));
    }

    Dictionary<string, object> AgentData(UnitHealth h, Vector3 windowStartPosition)
    {
        var agent = h.GetComponent<NavMeshAgent>();
        return Dict(
            "id", h.GetInstanceID(),
            "name", h.name,
            "path", GetPath(h.transform),
            "team", h.team.ToString(),
            "agentExists", agent != null,
            "agentEnabled", agent != null && agent.enabled,
            "isOnNavMesh", agent != null && agent.isOnNavMesh,
            "pathPending", agent != null && agent.pathPending,
            "hasPath", agent != null && agent.hasPath,
            "agentVelocity", agent != null ? (object)V3(agent.velocity) : null,
            "position", V3(h.transform.position),
            "windowStartPosition", V3(windowStartPosition),
            "displacement", Vector3.Distance(windowStartPosition, h.transform.position));
    }

    Dictionary<string, object> NavMeshSampleSummary()
    {
        var list = new List<object>();
        foreach (var h in ActivePlayerUnits())
        {
            NavMeshHit hit;
            bool ok = NavMesh.SamplePosition(h.transform.position, out hit, 3f, NavMesh.AllAreas);
            list.Add(Dict("name", h.name, "sampleOk", ok, "distance", ok ? hit.distance : -1f));
        }
        var tri = NavMesh.CalculateTriangulation();
        return Dict("playerSamples", list,
                    "triangulationVertices", tri.vertices != null ? tri.vertices.Length : 0,
                    "triangulationIndices", tri.indices != null ? tri.indices.Length : 0);
    }

    Dictionary<string, object> CollectUiState(HUD hud)
    {
        var ui = UIManager.instance;
        var panelStates = new List<object>();
        if (ui != null)
        {
            AddPanelState(panelStates, "inventoryPanel", ui.inventoryPanel);
            AddPanelState(panelStates, "statsPanel", ui.statsPanel);
            AddPanelState(panelStates, "equipPanel", ui.equipPanel);
            AddPanelState(panelStates, "escMenuPanel", ui.escMenuPanel);
            AddPanelState(panelStates, "mainMenuPanel", ui.mainMenuPanel);
        }

        var gameOverLike = new List<object>();
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null) continue;
            string n = t.name;
            if (n.IndexOf("GameOver", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("GameEnd", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Wipe", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("Defeat", StringComparison.OrdinalIgnoreCase) >= 0 ||
                n.IndexOf("OverPanel", StringComparison.OrdinalIgnoreCase) >= 0)
                gameOverLike.Add(Dict("name", n, "path", GetPath(t),
                                      "activeSelf", t.gameObject.activeSelf,
                                      "activeInHierarchy", t.gameObject.activeInHierarchy));
        }

        return Dict(
            "timeScale", Time.timeScale,
            "paused", PauseManager.instance != null && PauseManager.instance.IsPaused,
            "pausedInstanceExists", PauseManager.instance != null,
            "uiManagerExists", ui != null,
            "panels", panelStates,
            "hudExists", hud != null,
            "hudActiveSelf", hud != null && hud.gameObject.activeSelf,
            "hudMessageText", hud != null && hud.messageText != null ? hud.messageText.text : null,
            "hudMessageTextActive", hud != null && hud.messageText != null && hud.messageText.gameObject.activeInHierarchy,
            "gameOverLikeObjects", gameOverLike,
            "inputLockProxy", Dict(
                "timeScaleZero", Mathf.Approximately(Time.timeScale, 0f),
                "playerCommandControllerEnabledCount",
                FindObjectsByType<PlayerCommandController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .Count(c => c != null && c.enabled)),
            "note", "工程未发现专门的 GameOver 面板；结束提示走 HUD.messageText（GameData.SetMessage）");
    }

    static void AddPanelState(List<object> list, string key, BasePanel panel)
    {
        list.Add(Dict(
            "slot", key,
            "assigned", panel != null,
            "isOpen", panel != null && panel.IsOpen,
            "activeSelf", panel != null && panel.gameObject.activeSelf,
            "activeInHierarchy", panel != null && panel.gameObject.activeInHierarchy));
    }

    List<object> BuildTriggerChain(List<object> samples, bool triggered, float triggerAt)
    {
        var chain = new List<object>();
        foreach (var h in ActivePlayerUnits().OrderBy(x => x.name, StringComparer.Ordinal))
        {
            var dr = h.GetComponent<DownedRescue>();
            chain.Add(Dict("step", "characterDown",
                           "name", h.name,
                           "hp", h.hp,
                           "dead", h.dead,
                           "isDown", dr != null && dr.IsDown,
                           "t", Time.realtimeSinceStartup));
        }
        chain.Add(Dict("step", "aliveCount=0", "t", Time.realtimeSinceStartup,
                       "aliveStanding", CountStanding(ActivePlayerUnits())));
        chain.Add(Dict("step", "gameEndState", "t", Time.realtimeSinceStartup,
                       "triggered", triggered, "triggerSeconds", triggerAt,
                       "isWiped", GameFlow.instance != null && GameFlow.instance.IsWiped,
                       "timeScale", Time.timeScale,
                       "message", GameData.message));
        chain.Add(Dict("step", "uiState", "t", Time.realtimeSinceStartup,
                       "hudMessage", hudTextSafe()));
        chain.Add(Dict("step", "sampleTimeline", "samples", samples));
        return chain;
    }

    static string hudTextSafe()
    {
        var hud = UnityEngine.Object.FindObjectOfType<HUD>();
        return hud != null && hud.messageText != null ? hud.messageText.text : null;
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
