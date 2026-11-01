using UnityEngine;

/// <summary>LoL 式战争迷雾：程序化网格板（放高空，无碰撞不挡射线）。
/// 玩家单位周围一圈永久揭开，其余暗色迷雾；揭开边缘平滑过渡。
/// 材质用 Particles/Unlit（支持顶点色），完整透明参数。</summary>
public class FogOfWar : MonoBehaviour
{
    [Tooltip("迷雾覆盖边长（米，以本物体为中心）")]
    public float mapSize = 150f;
    [Tooltip("网格密度（每边顶点数）")]
    public int resolution = 96;
    [Tooltip("清晰半径（≈角色视野圈）")]
    public float revealRadius = 10f;
    [Tooltip("清晰圈边缘过渡宽度")]
    public float edgeSoftness = 4f;
    [Tooltip("迷雾不透明度")]
    public float fogAlpha = 0.82f;
    [Tooltip("揭开速度")]
    public float revealSpeed = 5f;

    Mesh _mesh;
    Vector3[] _verts;
    float[] _reveal;
    Color[] _colors;
    float _timer;

    void Start()
    {
        BuildMesh();
        int n = _verts.Length;
        _reveal = new float[n];
        _colors = new Color[n];
        for (int i = 0; i < n; i++) { _reveal[i] = 1f; _colors[i] = new Color(0.02f, 0.02f, 0.05f, fogAlpha); }
        _mesh.colors = _colors;
    }

    void BuildMesh()
    {
        int n = resolution;
        _mesh = new Mesh();
        _verts = new Vector3[n * n];
        var uv = new Vector2[n * n];
        var tris = new int[(n - 1) * (n - 1) * 6];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                _verts[y * n + x] = new Vector3(Mathf.Lerp(-mapSize / 2, mapSize / 2, x / (float)(n - 1)), 0,
                                                Mathf.Lerp(-mapSize / 2, mapSize / 2, y / (float)(n - 1)));
                uv[y * n + x] = new Vector2(x / (float)(n - 1), y / (float)(n - 1));
            }
        int ti = 0;
        for (int y = 0; y < n - 1; y++)
            for (int x = 0; x < n - 1; x++)
            {
                int i = y * n + x;
                tris[ti++] = i; tris[ti++] = i + n; tris[ti++] = i + 1;
                tris[ti++] = i + 1; tris[ti++] = i + n; tris[ti++] = i + n + 1;
            }
        _mesh.vertices = _verts; _mesh.uv = uv; _mesh.triangles = tris;
        var mf = gameObject.AddComponent<MeshFilter>();
        var mr = gameObject.AddComponent<MeshRenderer>();
        // ★ 首选自写最简 URP Unlit（Dungeon/FogOfWar：顶点色+透明+双面），
        //   不赌 URP Particles/Unlit 运行时拼属性；找不到时回退 Particles/Unlit 并补全全部透明参数
        var shader = Shader.Find("Dungeon/FogOfWar");
        Material mat;
        if (shader != null)
        {
            mat = new Material(shader);
        }
        else
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }
        mr.material = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= 0.1f) { _timer = 0f; RevealTick(); }
        LerpColors();
    }

    void RevealTick()
    {
        foreach (var h in FindObjectsByType<UnitHealth>(FindObjectsSortMode.None))
        {
            if (h.dead || h.team != Team.Player) continue;
            RevealAround(transform.InverseTransformPoint(h.transform.position));
        }
    }

    void RevealAround(Vector3 lp)
    {
        int n = resolution;
        float step = mapSize / (n - 1);
        int r = Mathf.CeilToInt((revealRadius + edgeSoftness) / step);
        int cx = Mathf.RoundToInt((lp.x + mapSize / 2) / step);
        int cy = Mathf.RoundToInt((lp.z + mapSize / 2) / step);
        for (int y = Mathf.Max(0, cy - r); y <= Mathf.Min(n - 1, cy + r); y++)
            for (int x = Mathf.Max(0, cx - r); x <= Mathf.Min(n - 1, cx + r); x++)
            {
                float d = Vector2.Distance(
                    new Vector2(x * step - mapSize / 2, y * step - mapSize / 2),
                    new Vector2(lp.x, lp.z));
                float v = Mathf.InverseLerp(revealRadius - edgeSoftness, revealRadius + edgeSoftness, d);
                int i = y * n + x;
                if (v < _reveal[i]) _reveal[i] = v;   // 只减不增 = 永久揭开
            }
    }

    void LerpColors()
    {
        float step = Time.deltaTime * revealSpeed * 0.4f;
        bool dirty = false;
        for (int i = 0; i < _reveal.Length; i++)
        {
            float a = Mathf.MoveTowards(_colors[i].a, _reveal[i] * fogAlpha, step);
            if (!Mathf.Approximately(a, _colors[i].a)) { _colors[i].a = a; dirty = true; }
        }
        if (dirty) _mesh.colors = _colors;
    }
}