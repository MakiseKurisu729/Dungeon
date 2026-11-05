using UnityEngine;
using UnityEngine.AI;

/// <summary>Boss 驱动：追击+平砍+蓄力重击；半血唤醒沉睡骷髅；死亡激活奖励宝箱。
/// 正面减伤由 ShieldedUnitHealth 管（与本组件无关）。</summary>
[RequireComponent(typeof(NavMeshAgent))]
public class BossBrain : MonoBehaviour
{
    [Header("基础")]
    public UnitStats stats;
    public UnitHealth health;
    public Animator animator;
    public string attackTrigger = "AttackMelee";
    public string deathTrigger = "Die";
    public string speedParam = "Speed";

    [Header("近战平砍")]
    public float meleeDamage = 22f;
    public float meleeWindup = 0.9f;
    public float meleeCooldown = 2.2f;

    [Header("蓄力重击")]
    public float slamCooldown = 8f;
    public float slamWindup = 1.3f;
    public float slamRadius = 3.4f;
    public float slamDamage = 32f;
    public float slamMinRange = 7f;
    public GameObject slamFx;

    [Header("半血召唤")]
    public SleepingSkeleton[] sleepingMinions;   // ★ Boss 房里倒地的骷髅

    [Header("奖励")]
    public GameObject rewardChest;               // ★ 金宝箱（初始禁用，死亡激活）

    NavMeshAgent _agent;
    Transform _target;
    float _slamTimer, _meleeCd;
    bool _windingUp, _meleeSwing, _didSlam, _didMelee, _summoned;   // ★
    float _windupT, _swingT;
    LineRenderer _ring;

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (stats != null) _agent.speed = stats.moveSpeed;
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (health == null) health = GetComponent<UnitHealth>();
        if (health != null) health.OnDeath += Die;
        _slamTimer = slamCooldown * 0.5f;
    }

    void Die()
    {
        if (animator != null) animator.SetTrigger(deathTrigger);
        _agent.enabled = false;
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        enabled = false;
        if (rewardChest != null) rewardChest.SetActive(true);   // ★ 宝箱解锁
        RunState.AddGold(50);
        RunState.AddKill();
        MessageFeed.SetMessage("Boss 已讨伐！宝箱解锁 +50 金币");
        Destroy(gameObject, 2.5f);
    }

    void Update()
    {
        if (health == null || health.dead) return;
        if (AppFlow.IsPaused) return;
        if (animator != null && !string.IsNullOrEmpty(speedParam))
            animator.SetFloat(speedParam, _agent.velocity.magnitude);

        // ★ 半血召唤（只触发一次）
        if (!_summoned && health.hp <= health.maxHp * 0.5f)
        {
            _summoned = true;
            foreach (var s in sleepingMinions)
                if (s != null) s.WakeUp();
            MessageFeed.SetMessage("Boss 召唤了帮手！");
        }

        if (_windingUp) { TickWindup(); return; }
        if (_meleeSwing) { TickMelee(); return; }

        _slamTimer -= Time.deltaTime;
        _meleeCd -= Time.deltaTime;

        _target = FindNearestPlayer();
        if (_target == null) return;

        float dist = XZ(_target.position);
        if (dist > AttackRange)
        {
            if (_agent.isOnNavMesh) _agent.SetDestination(_target.position);
            return;
        }
        if (_agent.isOnNavMesh && _agent.hasPath) _agent.ResetPath();

        Face(_target.position);
        if (_slamTimer <= 0f && dist <= slamMinRange) StartWindup();
        else if (_meleeCd <= 0f) StartMelee();
    }

    void StartWindup()
    {
        _windingUp = true; _windupT = 0f; _didSlam = false;
        if (animator != null) animator.SetTrigger(attackTrigger);
        EnsureRing();
        _ring.gameObject.SetActive(true);
    }

    void TickWindup()
    {
        _windupT += Time.deltaTime;
        float k = Mathf.Clamp01(_windupT / slamWindup);
        float pulse = slamRadius * 2f * Mathf.Lerp(0.5f, 1f, k) * (1f + 0.06f * Mathf.Sin(_windupT * 25f));
        _ring.transform.localScale = Vector3.one * pulse;

        if (k >= 1f && !_didSlam)
        {
            _didSlam = true;
            _windingUp = false;
            _slamTimer = slamCooldown;
            _ring.gameObject.SetActive(false);

            var hits = Physics.OverlapSphere(transform.position, slamRadius);
            foreach (var h in hits)
            {
                var uh = h.GetComponentInParent<UnitHealth>();
                if (uh != null && !uh.dead && uh.team == Team.Player)
                    uh.TakeHit(slamDamage, 999f, DamageType.Blunt,
                               uh.transform.position + Vector3.up * 0.6f, health);
            }
            if (slamFx != null)
            {
                var fx = Instantiate(slamFx, transform.position, Quaternion.identity);
                fx.transform.localScale = Vector3.one * (slamRadius * 2.2f);
                Destroy(fx, 5f);
            }
        }
    }

    void StartMelee()
    {
        _meleeSwing = true; _swingT = 0f; _didMelee = false;
        if (animator != null) animator.SetTrigger(attackTrigger);
    }

    void TickMelee()
    {
        _swingT += Time.deltaTime;
        if (!_didMelee && _swingT >= meleeWindup)
        {
            _didMelee = true;
            if (_target != null)
            {
                float dy = Mathf.Abs(_target.position.y - transform.position.y);
                if (XZ(_target.position) <= AttackRange * 1.3f && dy < 1.5f)
                {
                    var uh = _target.GetComponent<UnitHealth>();
                    if (uh != null && !uh.dead)
                        uh.TakeHit(meleeDamage, 999f, DamageType.Blunt,
                                   uh.transform.position + Vector3.up * 0.6f, health);
                }
            }
        }
        if (_swingT >= meleeWindup + 0.3f) { _meleeSwing = false; _meleeCd = meleeCooldown; }
    }

    float AttackRange { get { return stats != null ? stats.attackRange : 2.8f; } }

    Transform FindNearestPlayer()
    {
        float range = stats != null ? stats.aggroRange : 25f;
        UnitHealth best = UnitRegistry.Nearest(transform.position, range, Team.Player, h => !h.dead);
        return best != null ? best.transform : null;
    }

    float XZ(Vector3 p) { var d = p - transform.position; d.y = 0f; return d.magnitude; }

    void Face(Vector3 p)
    {
        var to = p - transform.position; to.y = 0f;
        if (to.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(to), Time.deltaTime * 8f);
    }

    void EnsureRing()
    {
        if (_ring != null) return;
        var go = new GameObject("slam_warning_ring");
        _ring = go.AddComponent<LineRenderer>();
        _ring.useWorldSpace = false;
        _ring.loop = true;
        _ring.positionCount = 64;
        _ring.startWidth = 0.12f;
        _ring.endWidth = 0.12f;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetColor("_BaseColor", new Color(1f, 0.2f, 0.15f, 0.85f));
        mat.SetFloat("_Surface", 1f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        _ring.material = mat;
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        for (int i = 0; i < 64; i++)
        {
            float a = i / 64f * Mathf.PI * 2f;
            _ring.SetPosition(i, new Vector3(Mathf.Cos(a) * 0.5f, 0.06f, Mathf.Sin(a) * 0.5f));
        }
    }
}