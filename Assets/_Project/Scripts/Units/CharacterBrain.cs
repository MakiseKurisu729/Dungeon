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

    /// <summary>撤销“走到交互物落点后自动补播一次 Interact”的待办。
    /// 读条动画由 DownedRescue 自己循环推送；不撤的话，救援者跑到落点会再补一次 Interact，
    /// 若此时救援已被打断，就会在打断后白播一轮交互动作（违反 R-5“打断即停”）。</summary>
    public void CancelPendingInteract() { _pendingInteract = null; }

    protected override void OnCommandMove(Vector3 point)
    {
        // 指令打断：普攻/交互让位给移动；技能进行中整体跳过（动画不切、计时不停、伤害照常结算）
        if (!InSkillAnim())
        {
            var c = combat as ICharacterCombat;
            if (c != null) c.CancelAttack();
            // 只有可被移动打断的状态才发 Interrupt；技能状态不发，避免残留触发器误伤下一次普攻
            // 入场过渡中（current 还是 Locomotion、next 已是 AttackMelee/Interact）也算“正在出招”，
            // 否则按下移动后这一刀还会在移动中补播出来
            if (unitAnimator != null)
            {
                var st = unitAnimator.GetCurrentAnimatorStateInfo(0);
                var nx = unitAnimator.IsInTransition(0)
                    ? unitAnimator.GetNextAnimatorStateInfo(0) : default(AnimatorStateInfo);
                bool breaking = st.IsName("AttackMelee") || st.IsName("Interact")
                             || nx.IsName("AttackMelee") || nx.IsName("Interact");
                if (breaking) unitAnimator.SetTrigger("Interrupt");
            }
        }
        _pendingInteract = null;
        // 真正下达移动指令 = 放弃当前救援（受击位移不经过这里）；DownedRescue 自己负责 2s 冷却。
        DownedRescue.NotifyMoveCommand(gameObject);
        base.OnCommandMove(point);
    }

    /// <summary>技能动画进行中（含入场过渡）：Skill/Bash 与 Q/W/E/R 子状态。移动指令不打断技能。</summary>
    bool InSkillAnim()
    {
        if (unitAnimator == null) return false;
        if (IsSkillState(unitAnimator.GetCurrentAnimatorStateInfo(0))) return true;
        return unitAnimator.IsInTransition(0) && IsSkillState(unitAnimator.GetNextAnimatorStateInfo(0));
    }

    static bool IsSkillState(AnimatorStateInfo st)
    {
        return st.IsName("Skill") || st.IsName("Bash")
            || st.IsName("Q_1") || st.IsName("W_1") || st.IsName("E_1") || st.IsName("R_1");
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