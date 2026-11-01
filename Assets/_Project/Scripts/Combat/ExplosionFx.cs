using System.Collections.Generic;
using UnityEngine;

/// <summary>3D 爆炸·瞬爆版：核心闪现即爆，碎片迸出（带重力），落地停留后湮灭。
/// 每个碎片有总寿命保险，杜绝残留。伤害在爆发帧结算。</summary>
public class ExplosionFx : MonoBehaviour
{
    [Header("爆发")]
    public float coreDuration = 0.08f;
    public Color coreColor = new Color(1f, 0.6f, 0.2f, 0.95f);

    [Header("碎片")]
    public int debrisCount = 18;
    public float debrisSpeed = 7f;
    public float debrisSize = 0.2f;
    public float gravity = 14f;
    public float debrisLinger = 1.2f;
    [Tooltip("碎片总寿命保险（秒），到点强制销毁")]
    public float debrisMaxLife = 4f;
    public Color debrisColorHot = new Color(1f, 0.85f, 0.3f, 0.95f);
    public Color debrisColorCool = new Color(0.85f, 0.25f, 0.08f, 0.9f);

    public float damageRadius;
    public float damage;
    public float sharpness;
    public UnitHealth source;

    Vector3 _coreTarget;
    Material _coreMat;
    Renderer _coreRenderer;
    float _groundY;
    readonly List<Transform> _debris = new List<Transform>();
    readonly List<Vector3> _debrisVel = new List<Vector3>();
    readonly List<Material> _debrisMats = new List<Material>();
    readonly List<Color> _debrisColors = new List<Color>();
    readonly List<bool> _debrisLanded = new List<bool>();
    readonly List<float> _landedTime = new List<float>();
    readonly List<float> _age = new List<float>();   // ★ 每个碎片的年龄
    float _t;
    bool _burst;

    void Start()
    {
        _coreTarget = transform.localScale;
        _groundY = transform.position.y - 0.15f;
        _coreRenderer = GetComponentInChildren<Renderer>();
        if (_coreRenderer != null)
        {
            _coreMat = _coreRenderer.material;
            _coreMat.color = coreColor;
        }
    }

    void Update()
    {
        _t += Time.deltaTime;

        if (!_burst)
        {
            float k = Mathf.Clamp01(_t / coreDuration);
            transform.localScale = _coreTarget * Mathf.Lerp(0.85f, 1f, k);
            if (k >= 1f) Burst();
            return;
        }

        bool anyAlive = false;
        for (int i = 0; i < _debris.Count; i++)
        {
            if (_debris[i] == null) continue;

            // ★ 总寿命保险：到点无条件销毁
            _age[i] += Time.deltaTime;
            if (_age[i] >= debrisMaxLife) { Destroy(_debris[i].gameObject); _debris[i] = null; continue; }

            anyAlive = true;

            if (!_debrisLanded[i])
            {
                var v = _debrisVel[i];
                v.y -= gravity * Time.deltaTime;
                _debrisVel[i] = v;
                _debris[i].position += v * Time.deltaTime;

                if (v.y < 0f && _debris[i].position.y <= _groundY)
                {
                    _debrisLanded[i] = true;
                    _debris[i].position = new Vector3(_debris[i].position.x, _groundY, _debris[i].position.z);
                }
            }
            else
            {
                _landedTime[i] += Time.deltaTime;
                float fade = Mathf.Clamp01(_landedTime[i] / debrisLinger);
                var c = _debrisColors[i];
                c.a = Mathf.Lerp(c.a, 0f, fade * 0.15f);
                _debrisMats[i].color = c;
                _debrisMats[i].SetColor("_EmissionColor", c * Mathf.Lerp(1.5f, 0f, fade));
                if (fade >= 1f) { Destroy(_debris[i].gameObject); _debris[i] = null; }
            }
        }

        if (!anyAlive) Destroy(gameObject);
    }

    void Burst()
    {
        _burst = true;
        if (_coreRenderer != null) _coreRenderer.enabled = false;

        for (int i = 0; i < debrisCount; i++)
        {
            var d = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(d.GetComponent<Collider>());
            d.transform.SetParent(null);
            d.transform.position = transform.position;
            d.transform.localScale = Vector3.one * debrisSize * Random.Range(0.5f, 1.4f);
            // ★ 引擎调度的延时销毁：不依赖 ExplosionFx 组件存活（fx 被提前 Destroy 也必清）
            Destroy(d, debrisMaxLife);

            var dm = new Material(_coreMat != null ? _coreMat.shader : Shader.Find("Universal Render Pipeline/Lit"));
            Vector3 dir = Random.onUnitSphere;
            dir.y = Mathf.Abs(dir.y) * 0.9f;
            float heat = Random.value;
            Color dc = Color.Lerp(debrisColorCool, debrisColorHot, heat);
            dm.color = dc;
            dm.EnableKeyword("_EMISSION");
            dm.SetColor("_EmissionColor", dc * 1.5f);
            d.GetComponent<Renderer>().material = dm;

            _debris.Add(d.transform);
            _debrisMats.Add(dm);
            _debrisColors.Add(dc);
            _debrisLanded.Add(false);
            _landedTime.Add(0f);
            _age.Add(0f);   // ★
            _debrisVel.Add(dir.normalized * debrisSpeed * Mathf.Lerp(0.5f, 1.3f, heat));
        }

        var hits = Physics.OverlapSphere(transform.position, damageRadius);
        foreach (var h in hits)
        {
            var uh = h.GetComponentInParent<UnitHealth>();
            if (uh != null && uh.team == Team.Monster && !uh.dead)
                uh.TakeHit(damage, sharpness, DamageType.Blunt,
                           uh.transform.position + Vector3.up * 0.6f, source);
        }
    }
}