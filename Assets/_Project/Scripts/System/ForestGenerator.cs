using UnityEngine;
using UnityEngine.AI;

/// <summary>森林装饰一键生成：边界树沿四边、内部随机撒树/灌木/石头。
/// 自动生成碰撞体与 NavMeshObstacle（Carve 运行时挖洞，无需烘焙障碍）：
/// 石头=挡移动+挡视线（Obstacle 层）；树=挡移动（Tree 层）；灌木草=纯装饰。</summary>
public class ForestGenerator : MonoBehaviour
{
    [Header("模型（从 kaykit-forest 的 fbx(unity) 文件夹拖）")]
    public GameObject[] treePrefabs;
    public GameObject[] bushPrefabs;
    public GameObject[] rockPrefabs;

    [Header("布局")]
    public float groundSize = 200f;
    public float borderInset = 8f;
    public int borderTreeCount = 72;
    public int innerTreeCount = 20;
    public int bushCount = 30;
    public int rockCount = 15;

    [Header("避让区")]
    public Transform campCenter;
    public float campClearRadius = 12f;
    public float pathDirection = 180f;
    public float pathHalfWidth = 5f;
    public float pathLength = 100f;

    [Header("碰撞（层名需先在 Tags and Layers 里创建）")]
    [Tooltip("石头所在层：挡移动+挡视线")]
    public string obstacleLayerName = "Obstacle";
    [Tooltip("树所在层：挡移动、不挡视线")]
    public string treeLayerName = "Tree";

    [Header("随机")]
    public int seed = 42;
    public float minScale = 0.9f;
    public float maxScale = 1.3f;

    int _obstacleLayer, _treeLayer;

    void Start()
    {
        if (treePrefabs == null || treePrefabs.Length == 0)
        { Debug.LogError("[ForestGenerator] 未拖树模型！", this); return; }
        _obstacleLayer = LayerMask.NameToLayer(obstacleLayerName);
        _treeLayer = LayerMask.NameToLayer(treeLayerName);
        Random.InitState(seed);

        GenerateBorder();
        Scatter(innerTreeCount, treePrefabs, Kind.Tree);
        Scatter(bushCount, bushPrefabs, Kind.Decor);
        Scatter(rockCount, rockPrefabs, Kind.Rock);
    }

    enum Kind { Tree, Decor, Rock }

    void GenerateBorder()
    {
        float half = groundSize / 2f - borderInset;
        for (int i = 0; i < borderTreeCount; i++)
        {
            float t = i / (float)borderTreeCount * 4f;
            int side = Mathf.FloorToInt(t) % 4;
            float f = t - Mathf.FloorToInt(t);
            Vector3 p;
            switch (side)
            {
                case 0: p = new Vector3(Mathf.Lerp(-half, half, f), 0, half); break;
                case 1: p = new Vector3(half, 0, Mathf.Lerp(half, -half, f)); break;
                case 2: p = new Vector3(Mathf.Lerp(half, -half, f), 0, -half); break;
                default: p = new Vector3(-half, 0, Mathf.Lerp(-half, half, f)); break;
            }
            p += transform.position + new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
            Spawn(treePrefabs[Random.Range(0, treePrefabs.Length)], p, Kind.Tree);
        }
    }

    void Scatter(int count, GameObject[] prefabs, Kind kind)
    {
        if (prefabs == null || prefabs.Length == 0) return;
        float half = groundSize / 2f - borderInset - 5f;
        for (int n = 0; n < count; n++)
        {
            for (int tries = 0; tries < 30; tries++)
            {
                Vector3 p = transform.position + new Vector3(Random.Range(-half, half), 0, Random.Range(-half, half));
                if (InCamp(p) || InPath(p)) continue;
                Spawn(prefabs[Random.Range(0, prefabs.Length)], p, kind);
                break;
            }
        }
    }

    void Spawn(GameObject prefab, Vector3 pos, Kind kind)
    {
        var go = Instantiate(prefab, pos, Quaternion.Euler(0, Random.Range(0f, 360f), 0), transform);
        float s = Random.Range(minScale, maxScale);
        go.transform.localScale = Vector3.one * s;

        if (kind == Kind.Tree && _treeLayer >= 0)
        {
            go.layer = _treeLayer;
            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 3f, 0); col.height = 6f; col.radius = 0.5f;
            AddCarve(go, new Vector3(0, 1f, 0), new Vector3(1.2f, 2f, 1.2f));
        }
        else if (kind == Kind.Rock && _obstacleLayer >= 0)
        {
            go.layer = _obstacleLayer;
            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 0.8f, 0); col.height = 1.6f; col.radius = 1.1f;
            AddCarve(go, new Vector3(0, 0.5f, 0), new Vector3(2.6f, 1f, 2.6f));
        }
    }

    // NavMeshObstacle Carve：运行时从导航网格挖洞（石头挡路不靠烘焙）
    void AddCarve(GameObject go, Vector3 center, Vector3 size)
    {
        var o = go.AddComponent<NavMeshObstacle>();
        o.shape = NavMeshObstacleShape.Box;   // ★ 修正：枚举名是 NavMeshObstacleShape
        o.center = center;
        o.size = size;
        o.carving = true;
    }

    bool InCamp(Vector3 p)
    {
        return campCenter != null && Vector3.Distance(p, campCenter.position) < campClearRadius;
    }

    bool InPath(Vector3 p)
    {
        Vector3 dir = Quaternion.Euler(0, pathDirection, 0) * Vector3.forward;
        Vector3 local = p - transform.position;
        float along = Vector3.Dot(local, dir);
        if (along < 0 || along > pathLength) return false;
        Vector3 perp = local - dir * along;
        return perp.magnitude < pathHalfWidth;
    }
}