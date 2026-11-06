using UnityEngine;
using Dungeon.Core;
using Dungeon.Gameplay.Units;
namespace Dungeon.Gameplay.World {

    /// <summary>LoL 式战争迷雾：程序化网格板。材质手动拖入（URP Particles/Unlit + Surface=透明），
    /// 顶点色驱动：透明=已探索，暗色=迷雾。玩家周围一圈永久揭开。</summary>
    public class FogOfWar : MonoBehaviour
    {
        [Header("材质（手动建：URP Particles/Unlit + Surface=透明）")]
        public Material fogMaterial;

        [Header("布局")]
        [Tooltip("迷雾覆盖边长（米，以本物体为中心）")]
        public float mapSize = 210f;
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
            if (fogMaterial == null)
            { Debug.LogError("[FogOfWar] 材质槽为空！请拖入 Particles/Unlit 透明材质", this); return; }
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
            mf.mesh = _mesh;
            var mr = gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = fogMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        void Update()
        {
            if (_mesh == null) return;
            _timer += Time.deltaTime;
            if (_timer >= 0.1f) { _timer = 0f; RevealTick(); }
            LerpColors();
        }

        void RevealTick()
        {
            foreach (var h in UnitRegistry.OfTeam(Team.Player))
            {
                if (h.dead) continue;
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
                    if (v < _reveal[i]) _reveal[i] = v;
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
}