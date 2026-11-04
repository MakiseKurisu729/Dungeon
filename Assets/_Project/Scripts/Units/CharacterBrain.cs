using UnityEngine;

/// <summary>
/// 玩家 Driver：移动指令（先取消当前攻击）> 集火 > 坚守自动攻击；交互指令（走到范围→执行）。
/// </summary>
public class CharacterBrain : UnitBrain
{
    [Header("交互")]
    [InspectorName("单位动画器")]
    public Animator unitAnimator;
    [InspectorName("交互额外距离")]
    public float interactExtraRange = 0.6f;   // 比交互物尺寸再大一圈

    Transform _orderedTarget;
    Interactable _pendingInteract;
    bool _moving;
    UnitHealth _health;

    protected override Team MyTeam { get { return Team.Player; } }

    protected override void Start()
    {
        base.Start();
        _health = GetComponent<UnitHealth>();
        if (_health != null) _health.OnHit += OnDamaged;
    }

    void OnDestroy()
    {
        if (_health != null) _health.OnHit -= OnDamaged;
    }

    // 受击反馈：统一伤害入口（UnitHealth.ApplyDamage/TakeHit）触发动画器 Hit → Hit_A
    void OnDamaged(DamageInfo info)
    {
        Animator a = unitAnimator != null ? unitAnimator : GetComponentInChildren<Animator>();
        if (a != null) a.SetTrigger("Hit");
    }

    public void CommandAttack(Transform t)
    {
        _orderedTarget = t;
        ClearMove();
    }

    public void CommandInteract(Interactable it)
    {
        if (it == null) return;
        var col = it.GetComponent<Collider>();
        float r = 1.5f;
        if (col != null) r = Mathf.Max(col.bounds.extents.x, col.bounds.extents.z) + interactExtraRange;

        Vector3 d = transform.position - it.transform.position; d.y = 0f;
        if (d.sqrMagnitude < 0.01f) d = Vector3.forward;
        CommandMove(it.transform.position + d.normalized * r);
        _pendingInteract = it;
    }

    protected override void OnCommandMove(Vector3 point)
    {
        // 指令打断：收招再移动
        var c = combat as ICharacterCombat;
        if (c != null) c.CancelAttack();
        _pendingInteract = null;
        base.OnCommandMove(point);
    }

    void Update()
    {
        if (agent == null || combat == null) return;
        if (PauseManager.instance != null && PauseManager.instance.IsPaused) return;

        bool wasMoving = _moving;
        _moving = ExecutingMove;
        if (wasMoving && !_moving && _pendingInteract != null)
        {
            // 走到交互范围：播交互动作 + 执行
            if (unitAnimator != null) unitAnimator.SetTrigger("Interact");
            var chest = _pendingInteract.GetComponent<Chest>();
            if (chest != null) chest.OpenBy(this);
            _pendingInteract = null;
            return;
        }
        if (_moving) return;

        if (_orderedTarget != null)
        {
            var h = _orderedTarget.GetComponent<UnitHealth>();
            if (h == null || h.dead) _orderedTarget = null;
        }

        target = _orderedTarget != null ? _orderedTarget : FindNearestEnemy(AggroRange);
        if (target == null) return;

        if (_orderedTarget == null && XZDistance(target.position) > AttackRange)
            return;   // 坚守

        AttackLoop();
    }
}