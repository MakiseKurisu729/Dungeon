using UnityEngine;
using Dungeon.Core;

/// <summary>
/// 玩家 Driver：移动指令（先取消当前攻击）> 集火 > 坚守自动攻击。
/// 交互指令（走到范围→补播 Interact→执行）交给同物体的 <see cref="InteractionRunner"/>，
/// 本类只保留“到位检测”的时点与兼容入口。
/// </summary>
public class CharacterBrain : UnitBrain
{
    [Header("交互")]
    [InspectorName("单位动画器")]
    public Animator unitAnimator;
    [InspectorName("交互额外距离")]
    public float interactExtraRange = 0.6f;   // 比交互物尺寸再大一圈

    Transform _orderedTarget;
    InteractionRunner _runner;
    bool _moving;
    UnitHealth _health;
    bool _wasInSkill;      // 上一帧是否处于技能状态（离开时解除运动锁）

    protected override Team MyTeam { get { return Team.Player; } }

    /// <summary>本单位的生命组件（只读；交互目标执行时按单位取用）。</summary>
    public UnitHealth Health
    {
        get
        {
            if (_health == null) _health = GetComponent<UnitHealth>();
            return _health;
        }
    }

    /// <summary>技能期间交互指令无效（Runner 与兼容入口共用这一条门禁）。</summary>
    public bool CanAcceptInteract { get { return !InSkillState(); } }

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
        if (InSkillState()) return;     // 技能期间攻击指令无效：不换目标
        _orderedTarget = t;
        ClearMove();
    }

    public void CommandInteract(Interactable it)
    {
        if (it == null) return;
        CommandInteract(it.Target());   // 兼容入口：Interactable 标记 → 目标契约
    }

    /// <summary>交互指令的新入口：直接按目标契约下单（输入层分发到这里）。</summary>
    public void CommandInteract(IInteractionTarget target)
    {
        if (target == null) return;
        if (_runner == null) _runner = InteractionRunner.Ensure(this);
        _runner.Order(target);
    }

    /// <summary>撤销“走到交互物落点后自动补播一次 Interact”的待办。
    /// 读条动画由 DownedRescue 自己循环推送；不撤的话，救援者跑到落点会再补一次 Interact，
    /// 若此时救援已被打断，就会在打断后白播一轮交互动作（违反 R-5“打断即停”）。</summary>
    public void CancelPendingInteract() { if (_runner != null) _runner.CancelPending(); }

    protected override void OnCommandMove(Vector3 point)
    {
        // 技能锁：当前状态是技能动画 → 移动指令整体无效（不 SetDestination、不取消技能、不切动画）
        if (InSkillState()) { ClearMove(); return; }

        // 指令打断：普攻（近战单段 / 远程前摇·释放）/交互让位给移动。
        // 过渡“进入”技能的瞬间（current 还是 Locomotion、next 已是 Q_1/W_1/E_1/R_1）不能算可打断，
        // 否则 CancelAttack 会把刚起手的技能一起收掉。
        if (!InSkillAnim())
        {
            var c = combat as ICharacterCombat;
            if (c != null) c.CancelAttack();
            // 只有可被移动打断的状态才发 Interrupt；技能状态不发，避免残留触发器误伤下一次普攻
            // 入场过渡中（current 还是 Locomotion、next 已是可打断状态）也算“正在出招”，
            // 否则按下移动后这一刀还会在移动中补播出来
            if (unitAnimator != null)
            {
                var st = unitAnimator.GetCurrentAnimatorStateInfo(0);
                var nx = unitAnimator.IsInTransition(0)
                    ? unitAnimator.GetNextAnimatorStateInfo(0) : default(AnimatorStateInfo);
                bool breaking = IsBreakableByMove(st) || IsBreakableByMove(nx);
                if (breaking) unitAnimator.SetTrigger("Interrupt");
            }
        }
        if (_runner != null) _runner.CancelPending();
        // 真正下达移动指令 = 放弃当前救援（受击位移不经过这里）；DownedRescue 自己负责 2s 冷却。
        DownedRescue.NotifyMoveCommand(gameObject);
        base.OnCommandMove(point);
    }

    /// <summary>可被移动打断的普攻/交互状态：近战单段、远程前摇、远程释放、交互。</summary>
    static bool IsBreakableByMove(AnimatorStateInfo st)
    {
        return st.IsName("AttackMelee") || st.IsName("Attack_Draw")
            || st.IsName("Attack_Release") || st.IsName("Interact");
    }

    /// <summary>当前状态就是技能动画（判定只看当前状态名，出口过渡中即视为已结束）。</summary>
    bool InSkillState()
    {
        if (unitAnimator == null) return false;
        return IsSkillState(unitAnimator.GetCurrentAnimatorStateInfo(0));
    }

    /// <summary>技能动画进行中（含入场过渡）：Q/W/E/R 四个子状态。移动指令不打断技能。</summary>
    bool InSkillAnim()
    {
        if (unitAnimator == null) return false;
        if (IsSkillState(unitAnimator.GetCurrentAnimatorStateInfo(0))) return true;
        return unitAnimator.IsInTransition(0) && IsSkillState(unitAnimator.GetNextAnimatorStateInfo(0));
    }

    /// <summary>技能状态名（与 SkillSystem 的槽位表一一对应：0..3 = Q_1/W_1/E_1/R_1）。</summary>
    static bool IsSkillState(AnimatorStateInfo st)
    {
        return st.IsName("Q_1") || st.IsName("W_1") || st.IsName("E_1") || st.IsName("R_1");
    }

    void Update()
    {
        if (agent == null || combat == null) return;
        if (AppFlow.IsPaused) return;

        // 离开技能状态的那一帧解除运动锁：技能协程正常收尾会自己 End，
        // 这里兜住「被打断 / 过渡打断 / 协程未走到 finally」等路径（End 幂等）。
        bool inSkill = InSkillState();
        if (_wasInSkill && !inSkill) UnitMotion.End(agent);
        _wasInSkill = inSkill;

        bool wasMoving = _moving;
        _moving = ExecutingMove;
        if (wasMoving && !_moving && _runner != null && _runner.HasPending)
        {
            // 走到交互范围：播交互动作 + 执行（逻辑同重构前内联；执行体改成目标契约）
            _runner.OnMovedTick(wasMoving, _moving);
            return;
        }
        if (_moving) return;

        // 技能期间不做追击/普攻决策：技能锁不允许任何指令（含自动追击）改变 destination
        if (inSkill) return;

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