using System.Collections;
using UnityEngine;

/// <summary>
/// 怪物 Driver：攻速缩放（时长+冷却）；Speed 驱动移动动画；
/// 命中窗口读动画时钟（attackStateName 状态 normalizedTime）；触发器化受击/死亡/收招。
/// </summary>
public class MonsterBrain : UnitBrain
{
    [Header("攻击")]
    [InspectorName("使用自身攻击动画")]
    public bool useOwnAttackAnim = true;
    [InspectorName("攻击触发器名")]
    public string attackTrigger = "AttackMelee";
    [InspectorName("攻击状态名")]
    [Tooltip("攻击状态名（动画时钟读进度用）：近战 Attack_Melee / 弩手 Shoot")]
    public string attackStateName = "Attack_Melee";
    [InspectorName("攻击时长")]
    public float attackLen = 0.8f;
    [InspectorName("判定窗口起")]
    [Range(0f, 1f)] public float activeStart = 0.35f;
    [InspectorName("判定窗口止")]
    [Range(0f, 1f)] public float activeEnd = 0.65f;
    [InspectorName("命中距离")]
    public float hitRange = 2.2f;
    [InspectorName("命中高度")]
    public float hitHeight = 1.5f;
    [InspectorName("攻击冷却")]
    public float attackCooldown = 2f;
    [InspectorName("收招距离倍率")]
    public float cancelAttackRangeMult = 1.6f;

    [Header("动画参数名")]
    [InspectorName("移速参数名")]
    public string speedParam = "Speed";
    [InspectorName("受击触发器名")]
    public string hitTrigger = "Hit";
    [InspectorName("死亡触发器名")]
    public string deathTrigger = "Die";

    UnitHealth _self;
    Animator _animator;
    float _cooldown;
    float _atkTimer = -1f;
    float _effAtkLen;
    float _hitUntil = -1f;
    bool _didHit;
    bool _dead;
    Coroutine _knockCo;

    protected override Team MyTeam { get { return Team.Monster; } }
    float SpeedMult { get { return stats != null && stats.attackSpeed > 0f ? stats.attackSpeed : 1f; } }

    protected override void Start()
    {
        base.Start();
        _self = GetComponent<UnitHealth>();
        _animator = GetComponentInChildren<Animator>();
        if (_self != null) _self.OnDeath += Die;
    }

    // 强制受击：僵直 + 线性击退（盾击等标记攻击调用）
    public void ForceHit(Vector3 knockDir, float stagger, float knockDist, float knockDur = 0.3f)
    {
        if (_dead) return;
        EnterStagger(stagger);
        if (_knockCo != null) StopCoroutine(_knockCo);
        _knockCo = StartCoroutine(KnockRoutine(knockDir, knockDist, knockDur));
    }

    IEnumerator KnockRoutine(Vector3 dir, float dist, float dur)
    {
        float t = 0f;
        Vector3 v = dir.normalized * (dist / Mathf.Max(0.01f, dur));
        while (t < dur)
        {
            t += Time.deltaTime;
            if (agent != null && agent.isOnNavMesh)
                agent.Move(v * Time.deltaTime);
            yield return null;
        }
    }

    void EnterStagger(float duration)
    {
        _hitUntil = Time.time + duration;
        _atkTimer = -1f;
        _didHit = false;
        StopAgent();
        if (_animator != null) _animator.SetTrigger(hitTrigger);
    }

    void Die()
    {
        _dead = true;
        StopAgent();
        enabled = false;
        GameData.AddGold(10);
        GameData.AddKill();
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        if (_animator != null) _animator.SetTrigger(deathTrigger);
        Destroy(gameObject, 2.5f);
    }

    void CancelAttack()
    {
        _atkTimer = -1f;
        if (_animator != null)
        {
            _animator.ResetTrigger(attackTrigger);
            _animator.SetTrigger("Interrupt");
        }
    }

    void Update()
    {
        if (_dead) return;
        if (PauseManager.instance != null && PauseManager.instance.IsPaused) return;

        if (_animator != null && !string.IsNullOrEmpty(speedParam))
            _animator.SetFloat(speedParam, agent != null ? new Vector3(agent.velocity.x, 0, agent.velocity.z).magnitude : 0f);

        if (Time.time < _hitUntil) return;

        if (_atkTimer >= 0f)
        {
            if (target == null || XZDistance(target.position) > AttackRange * cancelAttackRangeMult)
            {
                CancelAttack();
                return;
            }

            _atkTimer += Time.deltaTime;
            // ★ 动画时钟优先
            float t01 = ReadAnimProgress();
            if (t01 < 0f) t01 = _atkTimer / _effAtkLen;

            if (!_didHit && t01 >= activeStart && t01 <= activeEnd)
            {
                _didHit = true;
                var t = FindNearestEnemy(hitRange);
                if (t != null)
                {
                    float dy = Mathf.Abs(t.position.y - transform.position.y);
                    if (dy < hitHeight)
                        t.GetComponent<UnitHealth>().ApplyDamage(DamageValue, t.position + Vector3.up * 0.6f, _self);
                }
            }
            if (_atkTimer >= _effAtkLen) _atkTimer = -1f;
            return;
        }

        _cooldown -= Time.deltaTime * SpeedMult;

        target = FindNearestEnemy(AggroRange);
        if (target == null) { StopAgent(); return; }

        float dist = XZDistance(target.position);
        if (dist > AttackRange)
        {
            if (agent != null && agent.isOnNavMesh)
                agent.SetDestination(target.position);
        }
        else
        {
            StopAgent();
            Vector3 to = target.position - transform.position; to.y = 0f;
            if (to.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(to), Time.deltaTime * 8f);

            if (_cooldown <= 0f)
            {
                _effAtkLen = attackLen / SpeedMult;
                if (useOwnAttackAnim && _animator != null)
                {
                    _animator.SetTrigger(attackTrigger);
                    _atkTimer = 0f;
                    _didHit = false;
                }
                else
                {
                    var c = combat as ICharacterCombat;
                    if (c != null) c.AutoAttack(target);
                }
                _cooldown = attackCooldown;
            }
        }
    }

    float ReadAnimProgress()
    {
        if (_animator == null || string.IsNullOrEmpty(attackStateName)) return -1f;
        var st = _animator.GetCurrentAnimatorStateInfo(0);
        if (st.IsName(attackStateName)) return st.normalizedTime % 1f;
        return -1f;
    }
}