#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Dungeon.Core;

/// <summary>
/// 手动测试调试键位（仅编辑器，不进包、不改场景）：
///   F5  = 在启动时记录的场景骷髅位置补 1 只 Skeleton_Warrior；
///   F9  = 重载当前场景（等价重进 Play）；
///   F10 = 重置全体玩家（满血/复活、满蓝、技能冷却清零）。
/// 由 DebugTestToolsBootstrap 在进入 Play 时自动挂载，只在 DungeonWorld 场景生效。
/// 场景重载后宿主随 DontDestroyOnLoad 存活，并重新记录生成点。
/// </summary>
public static class DebugTestTools
{
    public const string SceneName = "DungeonWorld";
    public const string SkeletonPrefabPath = "Assets/_Project/Prefabs/Skeleton_Warrior.prefab";
    const string SkeletonUnitName = "Skeleton_Warrior";

    static Vector3 _spawnPoint;
    static bool _hasSpawnPoint;
    static int _spawnedCount;

    public static bool HasSpawnPoint { get { return _hasSpawnPoint; } }
    public static Vector3 SpawnPoint { get { return _spawnPoint; } }

    /// <summary>记录场景里那只骷髅的实际位置；场景重载后由宿主回调再记录一次。</summary>
    public static void RecordSceneSpawnPoint()
    {
        _hasSpawnPoint = false;
        foreach (var h in UnitRegistry.All(true))
        {
            if (h == null || h.team != Team.Monster) continue;
            if (!h.name.StartsWith(SkeletonUnitName, System.StringComparison.Ordinal)) continue;
            _spawnPoint = h.transform.position;
            _hasSpawnPoint = true;
            Debug.Log("[DebugTestTools] spawn point recorded at " + V3Text(_spawnPoint) + "; source=" + h.name);
            return;
        }
        Debug.Log("[DebugTestTools] 场景中暂未找到 " + SkeletonUnitName + "，F5 时将重试记录生成点");
    }

    /// <summary>F5：在记录的生成点实例化 1 只骷髅（存活、team=Monster）。返回实例；失败返回 null。</summary>
    public static GameObject SpawnSkeleton()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SkeletonPrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("[DebugTestTools] F5 失败：预制体缺失 " + SkeletonPrefabPath);
            return null;
        }

        if (!_hasSpawnPoint) RecordSceneSpawnPoint();
        if (!_hasSpawnPoint)
        {
            Debug.LogWarning("[DebugTestTools] F5 失败：场景中未找到可用的骷髅生成点");
            return null;
        }

        GameObject go = Object.Instantiate(prefab, _spawnPoint, prefab.transform.rotation);
        _spawnedCount++;
        go.name = SkeletonUnitName + "_Debug" + _spawnedCount;

        var health = go.GetComponent<UnitHealth>();
        bool ok = health != null && !health.dead && health.team == Team.Monster;
        if (ok)
            Debug.Log("[DebugTestTools] F5 spawned " + go.name + " at " + V3Text(go.transform.position) +
                      "; team=" + health.team + "; hp=" + F(health.hp) + "/" + F(health.maxHp));
        else
            Debug.LogWarning("[DebugTestTools] F5 生成物不是存活的 Monster 单位: " + go.name);
        return go;
    }

    /// <summary>F9：用编辑器 API 重载当前 Play 场景（不写资产、不保存场景）。</summary>
    public static void ReloadScene()
    {
        Scene active = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(active.path))
        {
            Debug.LogWarning("[DebugTestTools] F9 失败：当前场景没有资产路径，无法重载");
            return;
        }
        Debug.Log("[DebugTestTools] F9 reload scene " + active.path);
        EditorSceneManager.LoadSceneInPlayMode(active.path, new LoadSceneParameters(LoadSceneMode.Single));
    }

    /// <summary>F10：全体玩家满血（倒地则复活）、满蓝、技能冷却清零。返回处理到的玩家数。</summary>
    public static int ResetPlayers()
    {
        int revived = 0, healed = 0, manaRefilled = 0, cooldownReset = 0;
        foreach (var h in UnitRegistry.All())
        {
            if (h == null || h.team != Team.Player) continue;

            if (h.dead)
            {
                h.Revive(h.maxHp);
                revived++;
            }
            else
            {
                h.HealFull();
                healed++;
            }

            var mana = h.GetComponentInParent<UnitMana>();
            if (mana != null) { mana.Refill(); manaRefilled++; }

            var skills = h.GetComponentInParent<SkillSystem>();
            if (skills != null) { skills.ResetCooldowns(); cooldownReset++; }
        }

        Debug.Log("[DebugTestTools] F10 reset players; healed=" + healed + "; revived=" + revived +
                  "; manaRefilled=" + manaRefilled + "; cooldownReset=" + cooldownReset);
        return healed + revived;
    }

    static string V3Text(Vector3 v)
    {
        return "(" + F(v.x) + ", " + F(v.y) + ", " + F(v.z) + ")";
    }

    static string F(float v)
    {
        return v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
    }
}

/// <summary>调试键位宿主：运行期自动创建（不进场景），负责轮询 F5/F9/F10 与场景重载回调。</summary>
public class DebugTestToolsHost : MonoBehaviour
{
    /// <summary>运行期唯一宿主（DontDestroyOnLoad），替代 FindFirstObjectByType 的自举判重。</summary>
    public static DebugTestToolsHost instance;

    void Awake()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != DebugTestTools.SceneName) return;
        DebugTestTools.RecordSceneSpawnPoint();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5)) DebugTestTools.SpawnSkeleton();
        else if (Input.GetKeyDown(KeyCode.F9)) DebugTestTools.ReloadScene();
        else if (Input.GetKeyDown(KeyCode.F10)) DebugTestTools.ResetPlayers();
    }
}

/// <summary>进入 Play 时的自举钩子：只在 DungeonWorld 且非 batchmode 时创建调试宿主。</summary>
public static class DebugTestToolsBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AfterSceneLoadHook()
    {
        // batchmode（SPEC01 自动化）不挂调试工具，避免与断言观测面互相干扰。
        if (Application.isBatchMode) return;
        if (SceneManager.GetActiveScene().name != DebugTestTools.SceneName) return;
        if (DebugTestToolsHost.instance != null) return;

        var go = new GameObject("DebugTestTools");
        go.AddComponent<DebugTestToolsHost>();
        Debug.Log("[DebugTestTools] host created in " + DebugTestTools.SceneName + "; keys F5/F9/F10");
    }
}
#endif
