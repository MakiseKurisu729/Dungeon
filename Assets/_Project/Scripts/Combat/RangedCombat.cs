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

    [Header("动画释放窗口（按释放状态 normalizedTime）")]
    [InspectorName("发射窗口起")]
    [Tooltip("释放状态 normalizedTime 的发射窗口起点（0~1）；留空前摇状态名时退回旧 releasePoint 逻辑")]
    [Range(0f, 1f)] public float releaseWindowStart = 0.2f;
    [InspectorName("发射窗口止")]
    [Tooltip("释放状态 normalizedTime 的发射窗口终点（0~1）")]
    [Range(0f, 1f)] public float releaseWindowEnd = 0.6f;
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
    bool _sawAttackAnim;
    Transform _target;

    float SpeedMult { get { return stats != null && stats.attackSpeed > 0f ? stats.attackSpeed : 1f; } }

    void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
    }

    public void AutoAttack(Transform target)
    {
        if (_timer >= 0f || target == null) return;
        // 上一段攻击动画尚未回落时不抢起手，避免用旧释放进度提前发射
        if (IsAttackAnimPlaying()) return;
        _target = target;
        _curLen = attackLen / SpeedMult;
        _timer = 0f;
        _fired = false;
        _sawAttackAnim = false;
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

        bool animPlaying;
        bool inRelease;
        float t01 = ReadAnimProgress(out animPlaying, out inRelease);

        // 动画读取失败（无 Animator/不在攻击状态）：回退代码时钟
        if (!animPlaying) t01 = _timer / _curLen;

        if (!_fired)
        {
            bool inWindow;
            if (animPlaying)
                inWindow = inRelease && t01 >= releaseWindowStart && t01 <= releaseWindowEnd;
            else
                inWindow = t01 >= releaseWindowStart && t01 <= releaseWindowEnd;

            if (inWindow)
            {
                _fired = true;
                Fire();
            }
        }

        // 动画仍在攻击状态时延长代码计时（动画周期可能长于 attackLen/SpeedMult）；动画结束即收招
        if (animPlaying)
        {
            _sawAttackAnim = true;
            if (_timer > MaxAttackSeconds) _timer = -1f;
        }
        else if (_sawAttackAnim)
        {
            _timer = -1f;
            _sawAttackAnim = false;
        }
        else if (_timer >= _curLen) _timer = -1f;
    }

    // 攻击动画兜底上限：正常由动画状态驱动收招，此值只防动画卡死导致 _timer 永不结束
    const float MaxAttackSeconds = 10f;

    bool TwoStateMode
    {
        get { return !string.IsNullOrEmpty(drawStateName) && !string.IsNullOrEmpty(releaseStateName); }
    }

    /// <summary>
    /// 读取攻击动画：animPlaying=处于攻击动画（前摇或释放）；inRelease=处于释放状态；
    /// 返回释放状态的 normalizedTime（前摇返回 -1）。
    /// 旧配置（前摇状态名为空，例如骷髅弓手 Shoot 单状态）沿用 releasePoint 比例映射。
    /// </summary>
    float ReadAnimProgress(out bool animPlaying, out bool inRelease)
    {
        animPlaying = false;
        inRelease = false;
        if (_animator == null) return -1f;

        var st = _animator.GetCurrentAnimatorStateInfo(0);
        if (TwoStateMode)
        {
            if (st.IsName(releaseStateName))
            {
                animPlaying = true;
                inRelease = true;
                return st.normalizedTime % 1f;
            }
            if (st.IsName(drawStateName))
            {
                animPlaying = true;
                return -1f;
            }
            return -1f;
        }

        // 旧配置：单状态映射到 releasePoint 之后
        if (!string.IsNullOrEmpty(releaseStateName) && st.IsName(releaseStateName))
        {
            animPlaying = true;
            inRelease = true;
            return releasePoint + (1f - releasePoint) * (st.normalizedTime % 1f);
        }
        if (!string.IsNullOrEmpty(drawStateName) && st.IsName(drawStateName))
        {
            animPlaying = true;
            return releasePoint * Mathf.Clamp01(st.normalizedTime % 1f);
        }
        return -1f;
    }

    bool IsAttackAnimPlaying()
    {
        if (_animator == null) return false;
        var st = _animator.GetCurrentAnimatorStateInfo(0);
        if (!string.IsNullOrEmpty(drawStateName) && st.IsName(drawStateName)) return true;
        if (!string.IsNullOrEmpty(releaseStateName) && st.IsName(releaseStateName)) return true;
        return false;
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