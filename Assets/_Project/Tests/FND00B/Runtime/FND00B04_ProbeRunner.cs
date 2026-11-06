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
using Dungeon.Content;
using Dungeon.Core;

/// <summary>
/// FND-00B-04 运行期探针（Play 模式）。
/// 只读取生产对象 + 在内存中建立夹具，不调用保存、不修改生产资产。
/// 覆盖 QA 第 7（宝箱）→ 8（迷雾）→ 9（救援现状）三个子项。
/// </summary>
public class FND00B04_ProbeRunner : MonoBehaviour
{
    const string ScenePath = "Assets/Scenes/DungeonWorld.unity";
    const string KnightPrefabPath = "Assets/_Project/Prefabs/Knight_Unit.prefab";
    const string FixturePrefix = "FND00B04_";

    const string FinishedKey = "FND00B04.Finished";
    const string ExitCodeKey = "FND00B04.ExitCode";

    FND00B04_Evidence _ev;
    int _run = 1;
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
        string env = Environment.GetEnvironmentVariable("FND00B04_RUN");
        if (int.TryParse(env, out parsed) && parsed > 0) _run = parsed;

        _ev = new FND00B04_Evidence(_run);
        _deadline = Time.realtimeSinceStartup + 240f;

        Debug.Log("[FND00B04] runner start; run=" + _run +
                  "; unity=" + Application.unityVersion +
                  "; evidenceDir=" + _ev.EvidenceDir);

        // 等待测试场景就位（Play 模式进入后场景由编辑器加载）
        float waited = 0f;
        while (SceneManager.GetActiveScene().path != ScenePath && waited < 15f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        Debug.Log("[FND00B04] active scene=" + SceneManager.GetActiveScene().path +
                  "; loaded=" + SceneManager.GetActiveScene().isLoaded +
                  "; waited=" + waited.ToString("0.00") + "s");

        // 再等两帧，确保场景内 Start / Awake 完成
        yield return null;
        yield return null;

        yield return GroupChest();
        yield return GroupFog();
        yield return GroupRescue();

        Finish();
    }

    void Update()
    {
        if (_finished) return;
        if (_ev == null) return;
        if (Time.realtimeSinceStartup <= _deadline) return;
        _ev.InfraFailure("all", "Runner.Watchdog", "运行超时（240 秒）被强制结束", "");
        _exitReason = "watchdog timeout";
        Finish();
    }

    void Finish()
    {
        if (_finished) return;
        _finished = true;

        if (_ev != null)
        {
            _ev.WriteSummary(ScenePath, string.IsNullOrEmpty(_exitReason) ? "completed" : _exitReason);
        }

        int code;
        if (_ev == null || _ev.Infra > 0) code = 3;
        else if (_ev.Fail > 0 || _ev.Blocked > 0) code = 2;
        else code = 0;

        Debug.Log("[FND00B04] finish; exit=" + code +
                  "; pass=" + (_ev != null ? _ev.Pass : -1) +
                  "; fail=" + (_ev != null ? _ev.Fail : -1) +
                  "; blocked=" + (_ev != null ? _ev.Blocked : -1) +
                  "; infra=" + (_ev != null ? _ev.Infra : -1) +
                  "; fixturePass=" + (_ev != null ? _ev.FixturePass : -1) +
                  "; fixtureFail=" + (_ev != null ? _ev.FixtureFail : -1));

#if UNITY_EDITOR
        SessionState.SetBool(FinishedKey, true);
        SessionState.SetInt(ExitCodeKey, code);
        try
        {
            if (_ev != null)
                System.IO.File.WriteAllText(
                    System.IO.Path.Combine(_ev.EvidenceDir, "04_ExitCode_run" + _run + ".txt"),
                    code.ToString() + Environment.NewLine,
                    new System.Text.UTF8Encoding(false));
        }
        catch (Exception e) { Debug.LogWarning("[FND00B04] exit code file write failed: " + e.Message); }
        Time.timeScale = 1f;
        EditorApplication.isPlaying = false;
#else
        Time.timeScale = 1f;
#endif
    }

    // ============================================================
    // 第 7 项：宝箱
    // ============================================================
    IEnumerator GroupChest()
    {
        Debug.Log("[FND00B04] === QA item 7: Chest ===");
        Chest[] chests = new Chest[0];
        try
        {
            chests = FindObjectsByType<Chest>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(c => c != null && !c.name.StartsWith(FixturePrefix, StringComparison.Ordinal))
                .ToArray();
        }
        catch (Exception e)
        {
            _ev.InfraFailure("7", "Chest.SceneCount", e.Message, e.ToString());
            yield break;
        }

        var instances = new List<object>();
        foreach (var c in chests)
        {
            var inter = c.GetComponent<Interactable>();
            var inv = c.GetComponent<Inventory>();
            instances.Add(Dict(
                "id", c.GetInstanceID(),
                "name", c.name,
                "path", GetPath(c.transform),
                "position", V3(c.transform.position),
                "activeSelf", c.gameObject.activeSelf,
                "activeInHierarchy", c.gameObject.activeInHierarchy,
                "chestEnabled", c.enabled,
                "interactablePresent", inter != null,
                "interactableEnabled", inter != null && inter.enabled,
                "interactableHint", inter != null ? inter.hint : null,
                "inventoryPresent", inv != null,
                "inventoryCapacity", inv != null ? inv.capacity : -1,
                "itemSlotCount", inv != null && inv.items != null ? inv.items.Count : -1,
                "lidAssigned", c.lid != null
            ));
        }

        var prefabAssets = new List<object>();
#if UNITY_EDITOR
        try
        {
            foreach (var guid in AssetDatabase.FindAssets("chest t:Prefab"))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.IndexOf("_Project/Prefabs", StringComparison.OrdinalIgnoreCase) >= 0) prefabAssets.Add(p);
            }
        }
        catch (Exception e) { Debug.LogWarning("[FND00B04] prefab scan failed: " + e.Message); }
#endif

        bool countPass = chests.Length >= 1;
        _ev.Record("7", "Chest.SceneCount",
            Dict(
                "requirement", "DungeonWorld Play 中存在至少 1 个宝箱实例，可沿真实交互入口触发",
                "scene", SceneManager.GetActiveScene().path,
                "count", ">=1"
            ),
            Dict("count", chests.Length),
            countPass, countPass ? "pass" : "product_fail", "production",
            Dict(
                "instances", instances,
                "prefabAssetsFound", prefabAssets,
                "note", "缓存/资产存在不构成本项通过；仅以场景运行期实例计数为准"
            ));

        if (chests.Length == 0)
        {
            _ev.BlockedAssertion("7", "Chest.Interactable",
                Dict("requirement", "通过实际交互入口（CharacterBrain.CommandInteract → 走到范围 → Chest.OpenBy）触发并观察前后状态变化"),
                "场景宝箱实例 count=0，无交互入口可触发",
                Dict("sceneChestCount", 0, "prefabAssetsFound", prefabAssets));

            _ev.BlockedAssertion("7", "Chest.ProducesItem",
                Dict("requirement", "开箱后产出物数量 / 背包状态符合现有实现预期"),
                "场景宝箱实例 count=0，无法开箱，产出不可断言",
                Dict("sceneChestCount", 0, "prefabAssetsFound", prefabAssets));
            yield break;
        }

        // 有实例时执行真实交互路径（本单实际不进入此分支，保留供后续回归复用）
        Chest chest = chests.FirstOrDefault(c => c.isActiveAndEnabled) ?? chests[0];
        Interactable interactable = chest.GetComponent<Interactable>();
        Inventory chestInv = chest.GetComponent<Inventory>();
        ChestItem[] itemDefs = chest.GetComponentsInChildren<ChestItem>(true);

        CharacterBrain playerBrain = FindObjectsByType<CharacterBrain>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(b => b != null && !b.name.StartsWith(FixturePrefix, StringComparison.Ordinal))
            .FirstOrDefault(b =>
            {
                var h = b.GetComponent<UnitHealth>();
                return h != null && h.team == Team.Player && !h.dead;
            });

        Vector3 lidBefore = chest.lid != null ? chest.lid.localRotation.eulerAngles : Vector3.zero;
        var before = Dict(
            "inventorySlots", chestInv != null && chestInv.items != null ? chestInv.items.Count : -1,
            "inventoryNonEmpty", chestInv != null && chestInv.items != null ? chestInv.items.Count(i => i != null) : -1,
            "activeChestItems", itemDefs.Count(ci => ci.gameObject.activeSelf),
            "lidRotation", lidBefore
        );

        if (interactable == null || playerBrain == null)
        {
            string reason = interactable == null ? "宝箱缺少 Interactable 组件" : "场景中无可行动玩家单位";
            _ev.BlockedAssertion("7", "Chest.Interactable",
                Dict("requirement", "通过真实交互入口触发开箱"), reason, Dict("before", before));
            _ev.BlockedAssertion("7", "Chest.ProducesItem",
                Dict("requirement", "开箱产出符合现有预期"), reason, Dict("before", before));
            yield break;
        }

        playerBrain.CommandInteract(interactable);
        float t0 = Time.realtimeSinceStartup;
        bool opened = false;
        while (Time.realtimeSinceStartup - t0 < 12f)
        {
            if (itemDefs.Any(ci => ci.gameObject.activeSelf)) { opened = true; break; }
            if (chest.lid != null &&
                (chest.lid.localRotation.eulerAngles - lidBefore).sqrMagnitude > 1f)
            { opened = true; break; }
            yield return null;
        }

        var after = Dict(
            "inventorySlots", chestInv != null && chestInv.items != null ? chestInv.items.Count : -1,
            "inventoryNonEmpty", chestInv != null && chestInv.items != null ? chestInv.items.Count(i => i != null) : -1,
            "activeChestItems", itemDefs.Count(ci => ci.gameObject.activeSelf),
            "lidRotation", chest.lid != null ? chest.lid.localRotation.eulerAngles : Vector3.zero
        );

        _ev.Record("7", "Chest.Interactable",
            Dict("requirement", "交互前后宝箱状态发生预期变化（开盖/内容物现身）"),
            Dict("opened", opened, "elapsedSeconds", Time.realtimeSinceStartup - t0),
            opened, opened ? "pass" : "product_fail", "production",
            Dict("before", before, "after", after, "entry", "CharacterBrain.CommandInteract", "chestPath", GetPath(chest.transform)));

        int expectedItems = itemDefs.Count(ci => ci.data != null);
        int activeItems = itemDefs.Count(ci => ci.gameObject.activeSelf && ci.data != null);
        int invNonEmpty = chestInv != null && chestInv.items != null ? chestInv.items.Count(i => i != null) : -1;
        var itemIds = itemDefs.Where(ci => ci.data != null).Select(ci => (object)Dict(
            "assetName", ci.data.name,
            "itemName", ci.data.itemName,
            "goldValue", ci.data.goldValue,
            "active", ci.gameObject.activeSelf)).ToList();

        bool producePass = opened && expectedItems > 0 && activeItems == expectedItems && invNonEmpty >= expectedItems;
        _ev.Record("7", "Chest.ProducesItem",
            Dict("requirement", "开箱后内容物与背包计数一致且数量 > 0", "expectedItemCount", expectedItems),
            Dict("activeItems", activeItems, "expectedItems", expectedItems, "inventoryNonEmpty", invNonEmpty),
            producePass, producePass ? "pass" : "product_fail", "production",
            Dict("items", itemIds, "before", before, "after", after));
    }

    // ============================================================
    // 第 8 项：迷雾
    // ============================================================
    IEnumerator GroupFog()
    {
        Debug.Log("[FND00B04] === QA item 8: Fog ===");

        FogOfWar[] fogs = new FogOfWar[0];
        try
        {
            fogs = FindObjectsByType<FogOfWar>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(f => f != null && !f.name.StartsWith(FixturePrefix, StringComparison.Ordinal))
                .ToArray();
        }
        catch (Exception e)
        {
            _ev.InfraFailure("8", "Fog.MaskState", e.Message, e.ToString());
        }

        var maskList = new List<object>();
        foreach (var fog in fogs)
        {
            MeshFilter mf = fog.GetComponent<MeshFilter>();
            MeshRenderer mr = fog.GetComponent<MeshRenderer>();
            Mesh mesh = mf != null ? mf.sharedMesh : null;
            Color[] colors = null;
            if (mesh != null)
            {
                try { colors = mesh.colors; } catch (Exception e) { Debug.LogWarning("[FND00B04] read fog colors failed: " + e.Message); }
            }
            float minA = 1f, maxA = 0f, avgA = 0f;
            if (colors != null && colors.Length > 0)
            {
                minA = colors.Min(c => c.a);
                maxA = colors.Max(c => c.a);
                avgA = colors.Average(c => c.a);
            }

            maskList.Add(Dict(
                "id", fog.GetInstanceID(),
                "name", fog.name,
                "path", GetPath(fog.transform),
                "componentType", fog.GetType().FullName,
                "activeSelf", fog.gameObject.activeSelf,
                "activeInHierarchy", fog.gameObject.activeInHierarchy,
                "enabled", fog.enabled,
                "mapSize", fog.mapSize,
                "resolution", fog.resolution,
                "revealRadius", fog.revealRadius,
                "edgeSoftness", fog.edgeSoftness,
                "fogAlpha", fog.fogAlpha,
                "revealSpeed", fog.revealSpeed,
                "fogMaterialAssigned", fog.fogMaterial != null,
                "fogMaterialName", fog.fogMaterial != null ? fog.fogMaterial.name : null,
                "shaderName", fog.fogMaterial != null && fog.fogMaterial.shader != null ? fog.fogMaterial.shader.name : null,
                "shaderSupported", fog.fogMaterial != null && fog.fogMaterial.shader != null && fog.fogMaterial.shader.isSupported,
                "meshFilterPresent", mf != null,
                "meshVertexCount", mesh != null ? mesh.vertexCount : -1,
                "meshColorCount", colors != null ? colors.Length : -1,
                "alphaMin", minA,
                "alphaMax", maxA,
                "alphaAvg", avgA,
                "meshRendererPresent", mr != null,
                "meshRendererEnabled", mr != null && mr.enabled,
                "rendererBounds", mr != null ? V3(mr.bounds.size) : Vector3.zero
            ));
        }

        int fogLikeRenderers = 0;
        try
        {
            fogLikeRenderers = FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Count(r => r != null && r.name.IndexOf("fog", StringComparison.OrdinalIgnoreCase) >= 0);
        }
        catch (Exception) { }

        bool maskPass = fogs.Any(f => f.isActiveAndEnabled &&
                                      f.GetComponent<MeshFilter>() != null &&
                                      f.GetComponent<MeshFilter>().sharedMesh != null &&
                                      f.GetComponent<MeshRenderer>() != null &&
                                      f.GetComponent<MeshRenderer>().enabled);

        _ev.Record("8", "Fog.MaskState",
            Dict(
                "requirement", "Play 中场景存在 FogOfWar 遮罩实例，组件启用且 MeshRenderer 有效",
                "fogCount", ">=1",
                "rendererEnabled", true
            ),
            Dict("fogCount", fogs.Length, "fogLikeRendererCount", fogLikeRenderers),
            maskPass, maskPass ? "pass" : "product_fail", "production",
            Dict(
                "instances", maskList,
                "scenePath", SceneManager.GetActiveScene().path,
                "note", "只做组件/网格数据复核；画面是否可见由 04_人工目检.md 单列记录"
            ));

        // ---- EnemyVisibility ----
        yield return EnemyVisibilityProbe();

        // ---- VisionClearing（生产断言）----
        UnitHealth[] players = FindObjectsByType<UnitHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(h => h != null && !h.name.StartsWith(FixturePrefix, StringComparison.Ordinal) && h.team == Team.Player && !h.dead)
            .ToArray();

        FogOfWar activeFog = fogs.FirstOrDefault(f => f.isActiveAndEnabled && f.GetComponent<MeshFilter>() != null);
        if (fogs.Length == 0)
        {
            _ev.BlockedAssertion("8", "Fog.VisionClearing",
                Dict("requirement", "角色移动前后：视野范围内遮罩被清除、范围外保持迷雾", "revealRadius", "由遮罩实例提供"),
                "场景 FogOfWar 实例 count=0，无遮罩可采样",
                Dict("sceneFogCount", 0, "playerCount", players.Length));
        }
        else if (activeFog == null)
        {
            _ev.BlockedAssertion("8", "Fog.VisionClearing",
                Dict("requirement", "角色周围遮罩采样符合规则"),
                "场景 FogOfWar 实例未启用或缺少 MeshFilter，无法采样",
                Dict("sceneFogCount", fogs.Length, "playerCount", players.Length));
        }
        else if (players.Length == 0)
        {
            _ev.BlockedAssertion("8", "Fog.VisionClearing",
                Dict("requirement", "角色周围遮罩采样符合规则"),
                "场景中无存活的 Player 单位，无视野来源",
                Dict("sceneFogCount", fogs.Length, "playerCount", 0));
        }
        else
        {
            UnitHealth p = players[0];
            float nearAlpha = -1f, farAlpha = -1f;
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 5f)
            {
                nearAlpha = SampleFogAlpha(activeFog, p.transform.position);
                farAlpha = SampleFogAlpha(activeFog, p.transform.position + new Vector3(activeFog.revealRadius * 3f, 0f, 0f));
                if (nearAlpha >= 0f && nearAlpha < 0.1f) break;
                yield return null;
            }
            bool pass = nearAlpha >= 0f && nearAlpha < 0.1f && farAlpha > 0.6f;
            _ev.Record("8", "Fog.VisionClearing",
                Dict("requirement", "视野半径内 alpha→0（已清除），半径外保持 fogAlpha", "revealRadius", activeFog.revealRadius),
                Dict("nearAlpha", nearAlpha, "farAlpha", farAlpha,
                     "player", GetPath(p.transform), "playerPosition", V3(p.transform.position)),
                pass, pass ? "pass" : "product_fail", "production",
                Dict("sceneFogCount", fogs.Length, "note", "采样顶点色 alpha（0=已探索，fogAlpha=迷雾）"));
        }

        // ---- VisionClearing 夹具参考探针（生产无实例时验证算法本身，不计入生产结论）----
        yield return FogFixtureProbe();
    }

    IEnumerator EnemyVisibilityProbe()
    {
        UnitHealth[] players = FindObjectsByType<UnitHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(h => h != null && !h.name.StartsWith(FixturePrefix, StringComparison.Ordinal) && h.team == Team.Player && !h.dead)
            .ToArray();

        int monsterCount = FindObjectsByType<UnitHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(h => h != null && !h.name.StartsWith(FixturePrefix, StringComparison.Ordinal) && h.team == Team.Monster);

        if (players.Length == 0)
        {
            _ev.BlockedAssertion("8", "Fog.EnemyVisibility",
                Dict("requirement", "视野外敌人隐藏、进入视野后按现有规则显示"),
                "场景中无存活的 Player 单位，Visibility 无参照目标",
                Dict("playerCount", 0, "sceneMonsterCount", monsterCount));
            yield break;
        }

        UnitHealth player = players[0];

        // 临时禁用玩家大脑，避免探针期间角色自动接敌改变位置（结束后恢复原状态）
        var brains = FindObjectsByType<CharacterBrain>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(b => b != null && !b.name.StartsWith(FixturePrefix, StringComparison.Ordinal))
            .ToArray();
        var brainWasEnabled = brains.Select(b => b.enabled).ToArray();
        for (int i = 0; i < brains.Length; i++) brains[i].enabled = false;

        UnitStats stats = null;
        GameObject probe = null;
        Renderer probeRenderer = null;
        Visibility vis = null;
        string error = null;

        try
        {
            stats = ScriptableObject.CreateInstance<UnitStats>();
            stats.team = Team.Monster;
            stats.maxHp = 100f;
            stats.aggroRange = 0f;

            probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            probe.name = FixturePrefix + "EnemyProbe";
            probe.SetActive(false);

            var probeHealth = probe.AddComponent<UnitHealth>();
            probeHealth.stats = stats;

            vis = probe.AddComponent<Visibility>();
            vis.viewRadius = 10f;
            vis.obstacleMask = LayerMask.GetMask("Obstacle");

            probeRenderer = probe.GetComponent<Renderer>();
            probe.transform.position = player.transform.position + new Vector3(30f, 0.5f, 0f);
        }
        catch (Exception e)
        {
            error = e.GetType().Name + ": " + e.Message;
            Debug.LogError("[FND00B04] EnemyVisibilityProbe setup failed: " + e);
        }

        if (probe == null || vis == null)
        {
            for (int i = 0; i < brains.Length; i++) brains[i].enabled = brainWasEnabled[i];
            _ev.InfraFailure("8", "Fog.EnemyVisibility", error ?? "夹具建立失败", "");
            yield break;
        }

        probe.SetActive(true);
        yield return null;
        yield return null;
        yield return null;

        var outside = Dict(
            "enemyPosition", V3(probe.transform.position),
            "playerPosition", V3(player.transform.position),
            "distance", Vector3.Distance(probe.transform.position, player.transform.position),
            "viewRadius", vis.viewRadius,
            "isHidden", vis.IsHidden,
            "rendererEnabled", probeRenderer != null && probeRenderer.enabled,
            "lineOfSightBlocked", IsLineOfSightBlocked(probe.transform.position, player.transform.position)
        );

        probe.transform.position = player.transform.position + new Vector3(4f, 0.5f, 0f);
        yield return null;
        yield return null;
        yield return null;

        var inside = Dict(
            "enemyPosition", V3(probe.transform.position),
            "playerPosition", V3(player.transform.position),
            "distance", Vector3.Distance(probe.transform.position, player.transform.position),
            "viewRadius", vis.viewRadius,
            "isHidden", vis.IsHidden,
            "rendererEnabled", probeRenderer != null && probeRenderer.enabled,
            "lineOfSightBlocked", IsLineOfSightBlocked(probe.transform.position, player.transform.position)
        );

        bool outsideHidden = (bool)outside["isHidden"] && !(bool)outside["rendererEnabled"];
        bool insideVisible = !(bool)inside["isHidden"] && (bool)inside["rendererEnabled"];
        bool pass = outsideHidden && insideVisible;

        for (int i = 0; i < brains.Length; i++) if (brains[i] != null) brains[i].enabled = brainWasEnabled[i];
        Destroy(probe);
        if (stats != null) Destroy(stats);

        _ev.Record("8", "Fog.EnemyVisibility",
            Dict(
                "requirement", "视野外敌人隐藏（Renderer 关闭 / IsHidden=true），进入 10m 视野后显示",
                "outsideHidden", true,
                "insideVisible", true,
                "note", "使用内存夹具敌人（场景无 SkeletonSpawner，无真实怪物生成源），Visibility 为生产组件"
            ),
            Dict("outsideHidden", outsideHidden, "insideVisible", insideVisible),
            pass, pass ? "pass" : "product_fail", "production",
            Dict(
                "outside", outside,
                "inside", inside,
                "player", GetPath(player.transform),
                "sceneMonsterCount", monsterCount,
                "visibilityComponentType", typeof(Visibility).FullName,
                "fixtureNote", "夹具敌人为 Cube + UnitStats/UnitHealth/Visibility，仅存在于内存"
            ));
    }

    IEnumerator FogFixtureProbe()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Hidden/Internal-Colored");
        if (shader == null)
        {
            _ev.InfraFailure("8", "Fog.VisionClearing.fixtureProbe", "找不到可用 Shader，无法建立遮罩夹具", "");
            yield break;
        }

        Material mat = new Material(shader);
        mat.name = FixturePrefix + "FogMaterial";

        GameObject fogGo = null;
        GameObject playerGo = null;
        UnitStats pStats = null;
        FogOfWar fog = null;
        string error = null;

        try
        {
            fogGo = new GameObject(FixturePrefix + "FogFixture");
            fogGo.SetActive(false);
            fog = fogGo.AddComponent<FogOfWar>();
            fog.fogMaterial = mat;
            fog.mapSize = 210f;
            fog.resolution = 96;
            fog.revealRadius = 10f;
            fog.edgeSoftness = 4f;
            fog.fogAlpha = 0.82f;
            fog.revealSpeed = 5f;
            fogGo.transform.position = Vector3.zero;

            pStats = ScriptableObject.CreateInstance<UnitStats>();
            pStats.team = Team.Player;
            pStats.maxHp = 100f;

            playerGo = new GameObject(FixturePrefix + "FogPlayerProbe");
            playerGo.SetActive(false);
            var ph = playerGo.AddComponent<UnitHealth>();
            ph.stats = pStats;
            playerGo.transform.position = new Vector3(50f, 0f, 50f);
        }
        catch (Exception e)
        {
            error = e.GetType().Name + ": " + e.Message;
            Debug.LogError("[FND00B04] FogFixtureProbe setup failed: " + e);
        }

        if (fogGo == null || fog == null || playerGo == null)
        {
            _ev.InfraFailure("8", "Fog.VisionClearing.fixtureProbe", error ?? "夹具建立失败", "");
            if (playerGo != null) Destroy(playerGo);
            if (fogGo != null) Destroy(fogGo);
            if (mat != null) Destroy(mat);
            if (pStats != null) Destroy(pStats);
            yield break;
        }

        fogGo.SetActive(true);
        playerGo.SetActive(true);
        yield return null;
        yield return null;

        Vector3 beforePos = playerGo.transform.position;
        Vector3 farPoint = new Vector3(80f, 0f, 80f);
        var initial = Dict(
            "nearAlpha", SampleFogAlpha(fog, beforePos),
            "farAlpha", SampleFogAlpha(fog, farPoint)
        );

        float t0 = Time.realtimeSinceStartup;
        float near = SampleFogAlpha(fog, beforePos);
        while (Time.realtimeSinceStartup - t0 < 5f && !(near >= 0f && near < 0.05f))
        {
            yield return null;
            near = SampleFogAlpha(fog, beforePos);
        }
        float clearBefore = Time.realtimeSinceStartup - t0;

        var before = Dict(
            "playerPosition", V3(beforePos),
            "nearAlpha", near,
            "farAlpha", SampleFogAlpha(fog, farPoint),
            "nearSampleWorld", V3(Normalize(fog, beforePos)),
            "farSampleWorld", V3(farPoint),
            "clearSeconds", clearBefore
        );

        // 移动夹具角色 20m
        playerGo.transform.position = new Vector3(50f, 0f, 30f);
        Vector3 newPos = playerGo.transform.position;
        float t1 = Time.realtimeSinceStartup;
        float nearNew = SampleFogAlpha(fog, newPos);
        while (Time.realtimeSinceStartup - t1 < 5f && !(nearNew >= 0f && nearNew < 0.05f))
        {
            yield return null;
            nearNew = SampleFogAlpha(fog, newPos);
        }
        float clearAfter = Time.realtimeSinceStartup - t1;

        var afterMove = Dict(
            "playerPosition", V3(newPos),
            "newNearAlpha", nearNew,
            "oldNearAlpha", SampleFogAlpha(fog, beforePos),
            "farAlpha", SampleFogAlpha(fog, farPoint),
            "clearSeconds", clearAfter
        );

        bool nearCleared = (float)before["nearAlpha"] < 0.05f;
        bool farKept = (float)before["farAlpha"] > 0.6f;
        bool newCleared = (float)afterMove["newNearAlpha"] < 0.05f;
        bool oldStaysCleared = (float)afterMove["oldNearAlpha"] < 0.1f;
        bool farKeptAfter = (float)afterMove["farAlpha"] > 0.6f;
        bool pass = nearCleared && farKept && newCleared && oldStaysCleared && farKeptAfter;

        Destroy(playerGo);
        Destroy(fogGo);
        Destroy(mat);
        Destroy(pStats);

        _ev.Record("8", "Fog.VisionClearing.fixtureProbe",
            Dict(
                "requirement", "夹具内验证 FogOfWar 算法：初始全迷雾→角色周围清除→移动后新位置清除且旧区域保持已探索",
                "fogAlpha", 0.82f,
                "revealRadius", 10f,
                "nearAlpha", "<0.05",
                "farAlpha", ">0.6"
            ),
            Dict("initial", initial, "beforeMove", before, "afterMove", afterMove),
            pass, pass ? "pass" : "product_fail", "fixture_probe",
            Dict(
                "mapSize", 210f,
                "resolution", 96,
                "shader", shader.name,
                "shaderSupported", shader.isSupported,
                "note", "夹具参考探针：生产场景无 FogOfWar 实例，本记录只验证算法数据，不作为第 8 项通过依据"
            ));
    }

    // ============================================================
    // 第 9 项：救援（现状行为）
    // ============================================================
    IEnumerator GroupRescue()
    {
        Debug.Log("[FND00B04] === QA item 9: Rescue ===");

        DownedRescue[] allDowned = FindObjectsByType<DownedRescue>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(d => d != null && !d.name.StartsWith(FixturePrefix, StringComparison.Ordinal))
            .ToArray();

        var players = FindObjectsByType<UnitHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(h => h != null && !h.name.StartsWith(FixturePrefix, StringComparison.Ordinal) && h.team == Team.Player)
            .ToArray();
        int monsterCount = FindObjectsByType<UnitHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Count(h => h != null && !h.name.StartsWith(FixturePrefix, StringComparison.Ordinal) && h.team == Team.Monster);

        var scan = Dict(
            "sceneDownedRescueComponents", allDowned.Length,
            "scenePlayerUnits", players.Length,
            "sceneMonsterUnits", monsterCount,
            "sceneDownedComponents", allDowned.Select(d => (object)Dict(
                "name", d.name,
                "path", GetPath(d.transform),
                "isDown", d.IsDown,
                "hasHealth", d.health != null,
                "team", d.health != null ? d.health.team.ToString() : "null",
                "rescueTime", d.rescueTime,
                "rescueRange", d.rescueRange,
                "reviveHp", d.reviveHp)).ToList(),
            "scenePlayerCandidates", players.Select(h => (object)Dict(
                "name", h.name,
                "path", GetPath(h.transform),
                "team", h.team.ToString(),
                "hp", h.hp,
                "maxHp", h.maxHp,
                "dead", h.dead,
                "position", V3(h.transform.position),
                "hasSelectable", h.GetComponentInChildren<Selectable>(true) != null,
                "hasCharacterBrain", h.GetComponent<CharacterBrain>() != null,
                "hasDownedRescue", h.GetComponent<DownedRescue>() != null)).ToList()
        );

        DownedRescue target = allDowned
            .Where(d => d.health != null && !d.IsDown)
            .OrderByDescending(d => d.health.team == Team.Player)
            .FirstOrDefault();

        if (target == null)
        {
            _ev.BlockedAssertion("9", "Rescue.Fixture",
                Dict("requirement", "场景存在 1 个可倒地的玩家单位与 1 个可行动单位"),
                "场景中没有可使用的 DownedRescue 组件（或已处于倒地状态）",
                scan);
            _ev.BlockedAssertion("9", "Rescue.AutoTrigger",
                Dict("requirement", "靠近倒地角色自动触发救援"), "无倒地目标，无法建立救援场景", scan);
            _ev.BlockedAssertion("9", "Rescue.Completes",
                Dict("requirement", "救援在限定时间内完成"), "无倒地目标，无法建立救援场景", scan);
            yield break;
        }

        UnitHealth downedHealth = target.health;
        Vector3 downedPos = target.transform.position;

        var preState = Dict(
            "id", downedHealth.GetInstanceID(),
            "name", target.name,
            "path", GetPath(target.transform),
            "position", V3(downedPos),
            "hp", downedHealth.hp,
            "maxHp", downedHealth.maxHp,
            "dead", downedHealth.dead,
            "isDown", target.IsDown,
            "team", downedHealth.team.ToString()
        );

        // 事件链记录（真实事件，不调用最终结果方法）
        var chain = new List<object>();
        int revivedEvents = 0;
        System.Action onDeathHandler = () => chain.Add(Dict("event", "OnDeath", "t", Time.realtimeSinceStartup));
        System.Action onRevivedHandler = () =>
        {
            revivedEvents++;
            chain.Add(Dict("event", "OnRevived", "t", Time.realtimeSinceStartup));
        };
        downedHealth.OnDeath += onDeathHandler;
        downedHealth.OnRevived += onRevivedHandler;

        // 1) 通过真实伤害管线令其倒地（不是直接设置 state）
        downedHealth.ApplyDamage(downedHealth.maxHp + 10f, downedPos, null);
        yield return null;
        yield return null;

        if (!target.IsDown)
        {
            downedHealth.OnDeath -= onDeathHandler;
            downedHealth.OnRevived -= onRevivedHandler;
            _ev.Record("9", "Rescue.Fixture",
                Dict("requirement", "倒地夹具建立成功（IsDown=true，AI/移动被禁用）"),
                Dict("isDown", target.IsDown, "hp", downedHealth.hp, "dead", downedHealth.dead),
                false, "product_fail", "production",
                Dict("preState", preState, "scan", scan, "note", "伤害已结算但 DownedRescue 未进入倒地状态"));
            _ev.BlockedAssertion("9", "Rescue.AutoTrigger", Dict("requirement", "自动触发"), "倒地夹具未建立", Dict("preState", preState));
            _ev.BlockedAssertion("9", "Rescue.Completes", Dict("requirement", "限定时间完成"), "倒地夹具未建立", Dict("preState", preState));
            yield break;
        }

        var downedState = Dict(
            "hp", downedHealth.hp,
            "maxHp", downedHealth.maxHp,
            "dead", downedHealth.dead,
            "isDown", target.IsDown,
            "brainEnabled", target.brain != null && target.brain.enabled,
            "agentEnabled", target.agent != null && target.agent.enabled,
            "downedListCount", DownedRescue.AllDowned.Count
        );

        // 2) 建立可行动救援者：实例化真实 Knight 预制体
        GameObject prefab = null;
        string rescueError = null;
#if UNITY_EDITOR
        try { prefab = AssetDatabase.LoadAssetAtPath<GameObject>(KnightPrefabPath); }
        catch (Exception e) { rescueError = e.GetType().Name + ": " + e.Message; }
#else
        rescueError = "非编辑器环境无法通过 AssetDatabase 加载预制体";
#endif

        if (prefab == null)
        {
            _ev.InfraFailure("9", "Rescue.Fixture", "无法加载 " + KnightPrefabPath + "; " + (rescueError ?? ""), "");
            downedHealth.OnDeath -= onDeathHandler;
            downedHealth.OnRevived -= onRevivedHandler;
            yield break;
        }

        GameObject rescuer = null;
        try
        {
            rescuer = Instantiate(prefab, downedPos + new Vector3(6f, 0f, 0f), Quaternion.identity);
            rescuer.name = FixturePrefix + "Rescuer";
        }
        catch (Exception e)
        {
            rescueError = e.GetType().Name + ": " + e.Message;
            Debug.LogError("[FND00B04] rescuer instantiate failed: " + e);
        }

        if (rescuer == null)
        {
            _ev.InfraFailure("9", "Rescue.Fixture", "救援者实例化失败: " + (rescueError ?? ""), "");
            downedHealth.OnDeath -= onDeathHandler;
            downedHealth.OnRevived -= onRevivedHandler;
            yield break;
        }

        yield return null;
        yield return null;

        UnitHealth rescuerHealth = rescuer.GetComponent<UnitHealth>();
        Selectable rescuerSelectable = rescuer.GetComponentInChildren<Selectable>(true);
        CharacterBrain rescuerBrain = rescuer.GetComponent<CharacterBrain>();
        NavMeshAgent rescuerAgent = rescuer.GetComponent<NavMeshAgent>();

        bool fixturePass = target.IsDown &&
                           rescuerHealth != null && !rescuerHealth.dead &&
                           rescuerSelectable != null &&
                           rescuerHealth != downedHealth;

        _ev.Record("9", "Rescue.Fixture",
            Dict("requirement", "建立 1 个倒地角色（真实倒地管线）与 1 个可行动角色（真实 Knight 预制体实例）",
                 "downed", 1, "actionable", 1),
            Dict("downed", target.IsDown ? 1 : 0,
                 "actionable", rescuerHealth != null && !rescuerHealth.dead ? 1 : 0,
                 "fixturePass", fixturePass),
            fixturePass, fixturePass ? "pass" : "product_fail",
            "production",
            Dict(
                "downed", Dict(
                    "id", downedHealth.GetInstanceID(),
                    "name", target.name,
                    "path", GetPath(target.transform),
                    "position", V3(downedPos),
                    "preState", preState,
                    "downedState", downedState),
                "rescuer", rescuerHealth != null ? (object)Dict(
                    "id", rescuerHealth.GetInstanceID(),
                    "name", rescuer.name,
                    "path", GetPath(rescuer.transform),
                    "position", V3(rescuer.transform.position),
                    "hp", rescuerHealth.hp,
                    "maxHp", rescuerHealth.maxHp,
                    "dead", rescuerHealth.dead,
                    "hasSelectable", rescuerSelectable != null,
                    "hasBrain", rescuerBrain != null) : null,
                "distance", Vector3.Distance(downedPos, rescuer.transform.position),
                "scan", scan,
                "error", rescueError,
                "fixtureNote", "场景 3 个玩家单位均为 Knight_Unit 预制体变体（Archer/Mage 变体覆盖战斗与数值，沿用基类组件）；救援者为同一 Knight_Unit 预制体的内存实例，避免改动场景其余单位的位置与选中状态"
            ));

        if (!fixturePass)
        {
            _ev.BlockedAssertion("9", "Rescue.AutoTrigger", Dict("requirement", "靠近自动触发"), "救援夹具建立失败", Dict("error", rescueError));
            _ev.BlockedAssertion("9", "Rescue.Completes", Dict("requirement", "限定时间完成"), "救援夹具建立失败", Dict("error", rescueError));
            downedHealth.OnDeath -= onDeathHandler;
            downedHealth.OnRevived -= onRevivedHandler;
            Destroy(rescuer);
            yield break;
        }

        SelectionManager sm = SelectionManager.instance;
        bool selected = false;
        string selectMethod = "SelectionManager.SingleSelect（反射，等价于左键单选，不触发任何救援 API）";
        try
        {
            MethodInfo mi = typeof(SelectionManager).GetMethod("SingleSelect", BindingFlags.Instance | BindingFlags.NonPublic);
            if (sm != null && mi != null && rescuerSelectable != null)
            {
                mi.Invoke(sm, new object[] { rescuerSelectable });
                selected = true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[FND00B04] select failed: " + e);
        }
        yield return null;

        bool primaryIsRescuer = sm != null && sm.Primary == rescuerSelectable;

        // 3) 通过真实距离条件靠近：传送到 rescueRange 内，不使用点击交互指令
        Vector3 near = downedPos + new Vector3(1.2f, 0f, 0f);
        bool warped = false;
        string teleportMethod;
        if (rescuerAgent != null && rescuerAgent.enabled && rescuerAgent.isOnNavMesh)
        {
            warped = rescuerAgent.Warp(near);
            teleportMethod = "NavMeshAgent.Warp";
            if (!warped)
            {
                rescuerAgent.enabled = false;
                rescuer.transform.position = near;
                teleportMethod += " + agent disabled + transform.position";
            }
        }
        else
        {
            if (rescuerAgent != null) rescuerAgent.enabled = false;
            rescuer.transform.position = near;
            teleportMethod = "transform.position（agent 不可用或不在地形上）";
        }
        yield return null;

        float approachDistance = Vector3.Distance(rescuer.transform.position, downedPos);

        FieldInfo rescuingField = typeof(DownedRescue).GetField("_rescuing", BindingFlags.Instance | BindingFlags.NonPublic);

        // 4) 等待自动触发（由 DownedRescue.Update 依据选中者与距离自行开始）
        float trigStart = Time.realtimeSinceStartup;
        bool triggered = false;
        float triggeredAt = -1f;
        float triggerDistance = -1f;
        while (Time.realtimeSinceStartup - trigStart < 5f)
        {
            bool busy = rescuingField != null && (bool)rescuingField.GetValue(target);
            if (busy)
            {
                triggered = true;
                triggeredAt = Time.realtimeSinceStartup - trigStart;
                triggerDistance = Vector3.Distance(rescuer.transform.position, downedPos);
                break;
            }
            if (!target.IsDown) break; // 已直接完成（异常快路径）
            yield return null;
        }

        var triggerState = Dict(
            "selected", selected,
            "primaryIsRescuer", primaryIsRescuer,
            "primaryName", sm != null && sm.Primary != null ? sm.Primary.name : null,
            "teleportMethod", teleportMethod,
            "warped", warped,
            "approachDistance", approachDistance,
            "rescueRange", target.rescueRange,
            "triggered", triggered,
            "triggerDelaySeconds", triggeredAt,
            "triggerDistance", triggerDistance,
            "usedTargetClickFlow", false,
            "usedCommandInteract", false,
            "paused", PauseManager.instance != null && PauseManager.instance.IsPaused
        );

        bool triggerPass = selected && primaryIsRescuer &&
                           approachDistance <= target.rescueRange + 0.5f &&
                           (triggered || !target.IsDown);

        _ev.Record("9", "Rescue.AutoTrigger",
            Dict("requirement", "选中可行动角色并进入 rescueRange 后，无需点击目标即自动开始救援（现状行为）",
                 "rescueRange", target.rescueRange,
                 "usedTargetClickFlow", false),
            Dict("triggered", triggered, "triggerDelaySeconds", triggeredAt,
                 "triggerDistance", triggerDistance, "selected", selected, "primaryIsRescuer", primaryIsRescuer),
            triggerPass, triggerPass ? "pass" : "product_fail", "production",
            Dict(
                "state", triggerState,
                "selectionMethod", selectMethod,
                "rescuerId", rescuerHealth.GetInstanceID(),
                "downedId", downedHealth.GetInstanceID(),
                "note", "只验证当前自动救援；未断言'悬停图标→点击指令'目标需求"
            ));

        // 5) 等待完成
        float finishStart = Time.realtimeSinceStartup;
        bool completed = false;
        while (Time.realtimeSinceStartup - finishStart < 15f)
        {
            if (!target.IsDown && !downedHealth.dead) { completed = true; break; }
            yield return null;
        }
        float completeSeconds = Time.realtimeSinceStartup - finishStart;

        // 等起身动画结束，记录 brain 恢复状态
        float riseWaitStart = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - riseWaitStart < 3f && target.brain != null && !target.brain.enabled)
            yield return null;

        var finalState = Dict(
            "hp", downedHealth.hp,
            "maxHp", downedHealth.maxHp,
            "dead", downedHealth.dead,
            "isDown", target.IsDown,
            "brainEnabled", target.brain != null && target.brain.enabled,
            "agentEnabled", target.agent != null && target.agent.enabled,
            "downedListCount", DownedRescue.AllDowned.Count,
            "revivedEvents", revivedEvents,
            "eventChain", chain,
            "gameFlowWiped", GameFlow.instance != null && GameFlow.instance.IsWiped,
            "timeScale", Time.timeScale
        );

        bool completePass = completed &&
                            !target.IsDown &&
                            !downedHealth.dead &&
                            Mathf.Abs(downedHealth.hp - target.reviveHp) < 0.05f &&
                            revivedEvents >= 1;

        _ev.Record("9", "Rescue.Completes",
            Dict("requirement", "救援在限定时间内完成，倒地者恢复行动状态且血量符合 reviveHp",
                 "reviveHp", target.reviveHp,
                 "maxDurationSeconds", target.rescueTime + target.riseDuration + 3f),
            Dict("completed", completed, "completeSeconds", completeSeconds,
                 "hp", downedHealth.hp, "dead", downedHealth.dead, "isDown", target.IsDown,
                 "revivedEvents", revivedEvents),
            completePass, completePass ? "pass" : "product_fail", "production",
            Dict(
                "downedId", downedHealth.GetInstanceID(),
                "downedPath", GetPath(target.transform),
                "completeSeconds", completeSeconds,
                "rescueTime", target.rescueTime,
                "riseDuration", target.riseDuration,
                "finalState", finalState,
                "eventChain", chain,
                "note", "未让全员倒地、未触发团灭"
            ));

        downedHealth.OnDeath -= onDeathHandler;
        downedHealth.OnRevived -= onRevivedHandler;
        Destroy(rescuer);
    }

    // ============================================================
    // 工具
    // ============================================================
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

    static Vector3 Normalize(FogOfWar fog, Vector3 world)
    {
        int n = fog.resolution;
        float step = fog.mapSize / (n - 1);
        Vector3 local = fog.transform.InverseTransformPoint(world);
        int ix = Mathf.Clamp(Mathf.RoundToInt((local.x + fog.mapSize / 2f) / step), 0, n - 1);
        int iz = Mathf.Clamp(Mathf.RoundToInt((local.z + fog.mapSize / 2f) / step), 0, n - 1);
        Vector3 snapped = new Vector3(-fog.mapSize / 2f + ix * step, 0f, -fog.mapSize / 2f + iz * step);
        return fog.transform.TransformPoint(snapped);
    }

    static float SampleFogAlpha(FogOfWar fog, Vector3 world)
    {
        try
        {
            MeshFilter mf = fog.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return -1f;
            Color[] colors = mf.sharedMesh.colors;
            if (colors == null || colors.Length == 0) return -1f;

            int n = fog.resolution;
            float step = fog.mapSize / (n - 1);
            Vector3 local = fog.transform.InverseTransformPoint(world);
            int ix = Mathf.Clamp(Mathf.RoundToInt((local.x + fog.mapSize / 2f) / step), 0, n - 1);
            int iz = Mathf.Clamp(Mathf.RoundToInt((local.z + fog.mapSize / 2f) / step), 0, n - 1);
            int idx = iz * n + ix;
            if (idx < 0 || idx >= colors.Length) return -1f;
            return colors[idx].a;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[FND00B04] SampleFogAlpha failed: " + e.Message);
            return -1f;
        }
    }

    static bool IsLineOfSightBlocked(Vector3 enemyPos, Vector3 playerPos)
    {
        try
        {
            LayerMask mask = LayerMask.GetMask("Obstacle");
            if (mask.value == 0) return false;
            Vector3 eye = enemyPos + Vector3.up * 1.2f;
            Vector3 target = playerPos + Vector3.up * 0.9f;
            Vector3 dir = target - eye;
            return Physics.Raycast(eye, dir.normalized, dir.magnitude, mask);
        }
        catch (Exception) { return false; }
    }
}
