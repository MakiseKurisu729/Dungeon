using UnityEngine;

/// <summary>远程执行器：动画时钟轮询放箭帧；箭矢带伤害来源（DamageInfo 溯源）。</summary>
public class RangedCombat : MonoBehaviour, ICharacterCombat
{
    [InspectorName("弓")]
    public Transform bow;
    [InspectorName("箭矢预制体")]
    public GameObject arrowPrefab;
    [InspectorName("数值资产")]
    public UnitStats stats;
    [InspectorName("生命组件")]
    public UnitHealth health;
    [InspectorName("攻击触发器名")]
    [Tooltip("与动画器参数一致：玩家弓手填 Attack，骷髅弓手填 AttackRanged")]
    public string attackTrigger = "Attack";

    [Header("动画状态名（用于读取播放进度）")]
    [InspectorName("拉弓状态名")]
    public string drawStateName = "Attack_Draw";
    [InspectorName("放箭状态名")]
    public string releaseStateName = "Attack_Release";

    [InspectorName("攻击时长")]
    public float attackLen = 1.0f;
    [InspectorName("释放帧比例")]
    [Range(0f, 1f)] public float releasePoint = 0.45f;
    [InspectorName("伤害")]
    public float damage = 12f;
    [InspectorName("锋利度")]
    public float sharpness = 8f;
    [InspectorName("飞行时长")]
    public float flightDuration = 0.7f;
    [InspectorName("抛物线高度")]
    public float arcHeight = 1.2f;

    Animator _animator;
    float _timer = -1f;
    float _curLen;
    bool _fired;
    Transform _target;

    float SpeedMult { get { return stats != null && stats.attackSpeed > 0f ? stats.attackSpeed : 1f; } }

    void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
    }

    public void AutoAttack(Transform target)
    {
        if (_timer >= 0f || target == null) return;
        _target = target;
        _curLen = attackLen / SpeedMult;
        _timer = 0f;
        _fired = false;
        if (_animator != null)
        {
            _animator.ResetTrigger("Interrupt");
            _animator.SetTrigger(attackTrigger);
        }
    }

    public void CancelAttack()
    {
        if (_timer < 0f) return;
        _timer = -1f;
        if (_animator != null)
        {
            _animator.ResetTrigger(attackTrigger);
            _animator.SetTrigger("Interrupt");
        }
    }

    public void OnReleaseEvent()
    {
        if (_timer >= 0f && !_fired)
        {
            _fired = true;
            Fire();
        }
    }

    void Update()
    {
        if (_timer < 0f) return;
        _timer += Time.deltaTime;

        float t01 = ReadAnimProgress();
        if (t01 < 0f) t01 = _timer / _curLen;

        if (!_fired && t01 >= releasePoint)
        {
            _fired = true;
            Fire();
        }
        if (_timer >= _curLen) _timer = -1f;
    }

    float ReadAnimProgress()
    {
        if (_animator == null) return -1f;
        var st = _animator.GetCurrentAnimatorStateInfo(0);
        if (!string.IsNullOrEmpty(releaseStateName) && st.IsName(releaseStateName))
            return releasePoint + (1f - releasePoint) * (st.normalizedTime % 1f);
        if (!string.IsNullOrEmpty(drawStateName) && st.IsName(drawStateName))
            return releasePoint * Mathf.Clamp01(st.normalizedTime % 1f);
        return -1f;
    }

    void Fire()
    {
        if (arrowPrefab == null || bow == null || _target == null) return;
        var uh = _target.GetComponent<UnitHealth>();
        if (uh == null || uh.dead) return;

        var go = SimplePool.Get(arrowPrefab, bow.position, Quaternion.identity);
        var p = go.GetComponent<Projectile>();
        if (p != null)
        {
            p.target = uh;
            p.source = health;
            p.damage = damage;
            p.sharpness = sharpness;
            p.duration = flightDuration;
            p.arc = arcHeight;
            p.Launch(arrowPrefab);
        }
    }
}