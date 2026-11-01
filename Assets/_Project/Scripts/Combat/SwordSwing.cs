using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 近战执行器：命中窗口读【动画时钟】（UpperBody 层 normalizedTime 轮询），与攻速/过渡同步；
/// 普攻=单体锁定（只结算发起时锁定的目标，校验剑程）；技能斩/扫掠模式=窗口内群体扫掠。
/// 技能斩可强制打断进行中的普攻。
/// </summary>
public class SwordSwing : MonoBehaviour, ICharacterCombat
{
    public enum AttackMode { SingleTarget, Sweep }   // 单体=锁定目标；扫掠=窗口内群体（平砍AOE留此开关）

    [Header("引用")]
    [InspectorName("武器")]
    public Transform sword;
    [InspectorName("盾牌")]
    public Transform shield;
    [InspectorName("武器资产")]
    public WeaponData weapon;
    [InspectorName("圈材质")]
    public Material ringMaterial;
    [InspectorName("拖尾材质")]
    public Material trailMaterial;
    [InspectorName("数值资产")]
    public UnitStats stats;
    [InspectorName("生命组件")]
    public UnitHealth health;

    [Header("攻击模式")]
    [InspectorName("攻击模式")]
    [Tooltip("单体=只打发起攻击时锁定的目标；扫掠=窗口内群体（留作平砍AOE开关）")]
    public AttackMode attackMode = AttackMode.SingleTarget;
    [InspectorName("命中距离")]
    [Tooltip("单体模式下允许命中的最远距离（对齐 UnitStats 攻击距离）")]
    public float hitRange = 2.2f;

    [Header("攻击动画")]
    [InspectorName("攻击时长")]
    public float attackLen = 0.8f;
    [InspectorName("判定窗口起")]
    [Range(0f, 1f)] public float activeStart = 0.35f;
    [InspectorName("判定窗口止")]
    [Range(0f, 1f)] public float activeEnd = 0.55f;

    [Header("动画状态名（UpperBody 层）")]
    [InspectorName("攻击状态名")]
    public string attackStateName = "Attack_Draw";
    [InspectorName("技能状态名")]
    public string skillStateName = "Skill";
    [InspectorName("攻击动画层")]
    public int attackLayer = 0;

    [Header("命中")]
    [InspectorName("剑刃长度")]
    public float bladeLength = 1.05f;
    [InspectorName("命中半径")]
    public float hitRadius = 0.28f;

    [Header("强化斩（技能用）")]
    [InspectorName("技能攻击时长")]
    public float skillLen = 1.3f;
    [InspectorName("技能判定窗口起")]
    [Range(0f, 1F)] public float skillActiveStart = 0.5f;
    [InspectorName("技能判定窗口止")]
    [Range(0f, 1f)] public float skillActiveEnd = 0.7f;

    [Header("伤害")]
    [InspectorName("力道基础")]
    public float strengthBase = 1f;
    [InspectorName("力道加成系数")]
    public float strengthDamageFactor = 0.5f;
    [InspectorName("伤害倍率")]
    public float dmgMult = 1f;

    [Header("调试可视化")]
    [InspectorName("调试可视化")]
    public bool showDebug = true;
    [InspectorName("拖尾保留时长")]
    public float trailPersistTime = 0.25f;

    Animator _animator;
    float _timer = -1f;
    float _curLen;
    bool _isSkill;
    float _sharpness, _radiusMult = 1f;
    Transform _target;               // ★ 单体模式：发起攻击时锁定的目标

    Vector3 _prevHilt, _prevTip;
    readonly HashSet<int> _hitIds = new HashSet<int>();
    LineRenderer _tipTrail, _hiltTrail;
    readonly LineRenderer[] _sphere = new LineRenderer[3];
    readonly List<Vector3> _tipPts = new List<Vector3>();
    readonly List<Vector3> _hiltPts = new List<Vector3>();
    float _trailLife;

    float SpeedMult { get { return stats != null && stats.attackSpeed > 0f ? stats.attackSpeed : 1f; } }

    void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _tipTrail = MakeLine(0.05f, false);
        _hiltTrail = MakeLine(0.025f, false);
        for (int i = 0; i < 3; i++) _sphere[i] = MakeLine(0.008f, true);
    }

    LineRenderer MakeLine(float width, bool loop)
    {
        var lr = new GameObject("dbg_line").AddComponent<LineRenderer>();
        lr.material = trailMaterial != null ? trailMaterial : ringMaterial;
        lr.useWorldSpace = true;
        lr.loop = loop;
        lr.positionCount = 33;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.enabled = false;
        return lr;
    }

    void Start()
    {
        dmgMult += (GameData.level - 1) * 0.25f;
        if (sword != null) sword.gameObject.SetActive(true);
        if (shield != null) shield.gameObject.SetActive(true);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            showDebug = !showDebug;
            if (!showDebug) _trailLife = 0f;
        }

        if (_timer < 0f) return;
        _timer += Time.deltaTime;

        float t01 = ReadAnimProgress();
        if (t01 < 0f) t01 = _timer / _curLen;      // 回退代码时钟

        float wS = _isSkill ? skillActiveStart : activeStart;
        float wE = _isSkill ? skillActiveEnd : activeEnd;

        if (t01 >= wS && t01 <= wE)
        {
            DetectHit();
            _tipPts.Add(sword.position + sword.up * bladeLength);
            _hiltPts.Add(sword.position);
        }

        if (_timer >= _curLen) _timer = -1f;
        UpdateDebug();
    }

    float ReadAnimProgress()
    {
        if (_animator == null || attackLayer >= _animator.layerCount) return -1f;
        var st = _animator.GetCurrentAnimatorStateInfo(attackLayer);
        if (st.IsName(attackStateName) || st.IsName(skillStateName))
            return st.normalizedTime % 1f;
        return -1f;
    }

    // 单体：锁定大脑/玩家指定的目标；扫掠：窗口内群体（原逻辑）
    public void AutoAttack(Transform target) { DoAttack(false, target); }

    public void CancelAttack()
    {
        if (_timer < 0f) return;
        _timer = -1f;
        if (_animator != null)
        {
            _animator.ResetTrigger("Attack");
            _animator.ResetTrigger("Skill");
            _animator.SetTrigger("Interrupt");
        }
    }

    // 技能斩强制结束普攻再放；技能斩永远走扫掠（技能=AOE）
    public void DoSkillAttack() { _timer = -1f; DoAttack(true, null); }

    public float GetRawDamage()
    {
        float baseDamage = weapon != null ? weapon.baseDamage : 10f;
        return baseDamage * (1f + strengthBase * strengthDamageFactor) * dmgMult;
    }

    void DoAttack(bool skill, Transform target)
    {
        if (_timer >= 0f) return;
        _isSkill = skill;
        _target = target;
        _sharpness = (weapon != null ? weapon.sharpness : 8f) + (skill ? 5f : 0f);
        _radiusMult = skill ? 1.6f : 1f;
        _curLen = (skill ? skillLen : attackLen) / SpeedMult;

        if (_animator != null)
        {
            _animator.ResetTrigger("Interrupt");
            _animator.ResetTrigger("Attack");
            _animator.SetTrigger(skill ? "Skill" : "Attack");
        }

        _prevHilt = sword.position;
        _prevTip = sword.position + sword.up * bladeLength;
        _hitIds.Clear();
        _tipPts.Clear();
        _hiltPts.Clear();
        _trailLife = trailPersistTime + _curLen;
        _timer = 0f;
    }

    void DetectHit()
    {
        // ★ 普攻单体模式：只结算锁定目标（校验剑程，目标跑了=空挥）
        if (!_isSkill && attackMode == AttackMode.SingleTarget)
        {
            SingleTargetHit();
            return;
        }
        // 技能斩 / 扫掠模式：窗口内逐帧扫掠（群体）
        SweepHit();
    }

    void SingleTargetHit()
    {
        if (_target == null) return;
        var uh = _target.GetComponent<UnitHealth>();
        if (uh == null || uh.dead || !_hitIds.Add(uh.GetInstanceID())) return;
        float dist = Vector3.Distance(transform.position, uh.transform.position);
        if (dist > hitRange) return;   // 目标已走出剑程：空挥
        uh.TakeHit(GetRawDamage(), _sharpness, DamageType.Slash,
                   uh.transform.position + Vector3.up * 0.6f, health);
    }

    void SweepHit()
    {
        Vector3 hilt = sword.position;
        Vector3 tip = sword.position + sword.up * bladeLength;
        CheckSegment(_prevHilt, hilt);
        CheckSegment(_prevTip, tip);
        _prevHilt = hilt;
        _prevTip = tip;
    }

    void CheckSegment(Vector3 from, Vector3 to)
    {
        Vector3 d = to - from;
        if (d.sqrMagnitude < 0.0001f) return;
        if (d.magnitude > 1.5f) return;

        float raw = GetRawDamage() * (_isSkill ? 2f : 1f);

        RaycastHit[] hits = Physics.SphereCastAll(from, hitRadius * _radiusMult, d.normalized, d.magnitude);
        foreach (var h in hits)
        {
            var uh = h.collider.GetComponentInParent<UnitHealth>();
            if (uh == null || uh.team != Team.Monster || !_hitIds.Add(uh.GetInstanceID())) continue;
            uh.TakeHit(raw, _sharpness, DamageType.Slash, h.point, health);
        }
    }

    void UpdateDebug()
    {
        bool showSphere = showDebug && sword != null;
        for (int i = 0; i < 3; i++) _sphere[i].enabled = showSphere;
        if (showSphere)
        {
            Vector3 center = sword.position + sword.up * bladeLength;
            float r = hitRadius * _radiusMult;
            for (int axis = 0; axis < 3; axis++)
                for (int i = 0; i <= 32; i++)
                {
                    float a = i / 32f * Mathf.PI * 2f;
                    Vector3 off = axis == 0 ? new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f)
                                : axis == 1 ? new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a))
                                : new Vector3(0f, Mathf.Cos(a), Mathf.Sin(a));
                    _sphere[axis].SetPosition(i, center + off * r);
                }
        }

        _trailLife -= Time.deltaTime;
        bool showTrail = _trailLife > 0f && _tipPts.Count > 1;
        _tipTrail.enabled = showTrail;
        _hiltTrail.enabled = showTrail;
        if (showTrail)
        {
            _tipTrail.positionCount = _tipPts.Count;
            _tipTrail.SetPositions(_tipPts.ToArray());
            _hiltTrail.positionCount = _hiltPts.Count;
            _hiltTrail.SetPositions(_hiltPts.ToArray());
        }
    }
}