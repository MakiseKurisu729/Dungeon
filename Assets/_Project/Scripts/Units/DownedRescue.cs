using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>倒地与救援（指令式、动画机驱动）：挂玩家单位，且同物体挂 Interactable。
/// 倒地：血空 → SetTrigger(Die) → Downed 保持末端；AI/移动禁用。
/// 救援：选中活着的队友 → 点自己下指令（CommandRescue）→ 走过去 → 进 rescueRange 循环播 Interact 读条；
/// 打断：救援者受击 / 收到移动指令 → 立即中断且 2s 内不自动重开（必须重新下令）；
/// 完成：SetTrigger(Revive) → Rise 起身 → 1血恢复。</summary>
public class DownedRescue : MonoBehaviour
{
    public static readonly List<DownedRescue> AllDowned = new List<DownedRescue>();

    [Header("引用（留空自动找）")]
    public UnitHealth health;
    public UnitBrain brain;
    public NavMeshAgent agent;
    public Animator animator;

    [Header("动画触发器名（与控制器参数一致）")]
    public string dieTrigger = "Die";
    public string reviveTrigger = "Revive";

    [Header("救援参数")]
    [Tooltip("救援读条时长（秒）")]
    public float rescueTime = 3f;
    [Tooltip("可救援距离（队友走到此范围自动开始读条）")]
    public float rescueRange = 2.5f;
    [Tooltip("救起后的血量")]
    public float reviveHp = 1f;
    [Tooltip("起身动画时长（秒），期间不恢复行动")]
    public float riseDuration = 1.2f;

    public bool IsDown { get; private set; }

    /// <summary>只读：是否正在读条（UI/QA 用；不参与序列化）。</summary>
    public bool IsChanneling { get { return _channeling; } }

    /// <summary>只读：当前承接救援指令的救援者（null = 没有进行中的救援）。</summary>
    public UnitHealth CurrentRescuer { get { return _rescuer; } }

    UnitHealth _rescuer;
    Interactable _interact;
    Coroutine _routine;
    Animator _rescuerAnim;
    bool _channeling;
    float _channelBlockUntil;    // 打断冷却截止时刻：此前不开读条（不自动重开）
    float _nextInteractPush;     // Interact 触发器重发节流

    /// <summary>跑位超时：指令下达后这么久还没进范围就放弃（不会无限等待）。</summary>
    const float ArriveTimeout = 15f;
    /// <summary>打断冷却：受击/移动打断后 2s 内不得自动重新开始读条。</summary>
    const float InterruptCooldown = 2f;
    /// <summary>触发器重发最小间隔，避免同一次状态交接里重复压栈。</summary>
    const float InteractRepushGap = 0.2f;

    void Start()
    {
        if (health == null) health = GetComponent<UnitHealth>();
        if (brain == null) brain = GetComponent<UnitBrain>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (health != null) health.OnDeath += OnDowned;

        _interact = GetComponent<Interactable>();
        if (_interact == null) _interact = GetComponentInParent<Interactable>();
        // hint 由 Interactable 自己配（“救援”）；留空时才兜底，避免覆盖检查器里的文案。
        if (_interact != null && string.IsNullOrEmpty(_interact.hint)) _interact.hint = "点击救援倒地队友";
    }

    void OnDowned()
    {
        if (IsDown) return;
        IsDown = true;
        AllDowned.Add(this);
        if (brain != null) brain.enabled = false;
        if (agent != null) { if (agent.hasPath) agent.ResetPath(); agent.enabled = false; }
        var sel = GetComponentInChildren<Selectable>();
        if (sel != null) sel.SetSelected(false);
        // 致死一击会先排入 Hit 触发器（CharacterBrain.OnDamaged）；不先清掉，AnyState→Hit_A 会在 Downed 之后
        // 把倒地动作抢走，导致站着不动却已倒地。
        if (animator != null) { animator.ResetTrigger("Hit"); animator.SetTrigger(dieTrigger); }
        MessageFeed.SetMessage($"{gameObject.name} 倒下了！选中队友点击他进行救援");
    }

    void OnDestroy() { AllDowned.Remove(this); }

    // ===== 指令层 =====

    /// <summary>下达救援指令（唯一的救援入口，只有明确的点击指令会调用）。
    /// 校验通过后让救援者走到自己身边，由 RescueRoutine 在范围内开始循环读条。</summary>
    public void CommandRescue(UnitHealth rescuer)
    {
        if (!IsDown) return;
        if (rescuer == null || rescuer.dead) return;
        if (rescuer == health) return;

        // 重复/换人下令：先停掉上一条指令，避免两个协程同时读条
        if (_routine != null) { StopCoroutine(_routine); _routine = null; }
        _channeling = false;
        _rescuerAnim = null;

        // 先让救援者跑位：CommandInteract 内部会走一次 CommandMove → NotifyMoveCommand，
        // 此时 _rescuer 还没记录，所以不会被当成“移动打断”。
        var rBrain = rescuer.GetComponent<CharacterBrain>();
        if (rBrain != null && _interact != null)
        {
            rBrain.CommandInteract(_interact);
            // 读条动画（Interact 循环）由本组件自己推送；撤掉“到位补播一次”的待办，
            // 否则救援者跑到落点时会再压一次 Interact，打断后就会白播一轮交互动作。
            rBrain.CancelPendingInteract();
        }

        _rescuer = rescuer;
        _routine = StartCoroutine(RescueRoutine(rescuer));
    }

    /// <summary>救援者收到移动指令：由他进行的救援（跑位或读条）立刻作废，且 2s 内不自动重开。</summary>
    public static void NotifyMoveCommand(GameObject rescuer)
    {
        if (rescuer == null) return;
        for (int i = AllDowned.Count - 1; i >= 0; i--)
        {
            var d = AllDowned[i];
            if (d == null) continue;
            if (d._rescuer == null) continue;
            if (d._rescuer.gameObject != rescuer) continue;
            d.CancelByMove();
        }
    }

    /// <summary>移动打断：清状态 + 冷却；只有正在读条时才提示（纯粹跑位被改道不必吓人）。</summary>
    void CancelByMove()
    {
        bool wasChanneling = _channeling;
        ResetChannelTrigger();
        if (_routine != null) { StopCoroutine(_routine); _routine = null; }
        _channeling = false;
        _rescuer = null;
        _channelBlockUntil = Time.time + InterruptCooldown;
        if (wasChanneling) MessageFeed.SetMessage("救援被打断，请重新下达救援指令");
    }

    IEnumerator RescueRoutine(UnitHealth rescuer)
    {
        // 面向倒地者（跑位途中也保持朝向，读条时不会背对）
        Vector3 dir = transform.position - rescuer.transform.position; dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f) rescuer.transform.rotation = Quaternion.LookRotation(dir);

        // 1) 等抵达：超时 / 救援者阵亡 / 目标已不倒地 → 放弃，不自动重试
        float deadline = Time.time + ArriveTimeout;
        while (!InRange(rescuer))
        {
            if (!IsDown || rescuer == null || rescuer.dead || Time.time >= deadline) { Abort(); yield break; }
            yield return null;
        }

        // 2) 打断冷却：受击/移动打断后 2s 内不重新开读条（指令不丢，冷却到期再开始）
        while (Time.time < _channelBlockUntil)
        {
            if (!IsDown || rescuer == null || rescuer.dead) { Abort(); yield break; }
            yield return null;
        }

        // 3) 读条：Interact 动作循环，直到读完/被打断/出界
        _channeling = true;
        var rAnim = rescuer.GetComponentInChildren<Animator>();
        _rescuerAnim = rAnim;
        MessageFeed.SetMessage("救援中……受击或移动会打断！");

        bool interrupted = false;
        System.Action<DamageInfo> onHit = _ => interrupted = true;
        rescuer.OnHit += onHit;
        float t = 0f;
        try
        {
            PushInteract(rAnim, true);
            while (t < rescueTime)
            {
                if (interrupted || !IsDown || rescuer.dead) break;
                if (AppFlow.IsPaused) { yield return null; continue; }
                if (!InRange(rescuer, 1f)) break;      // 出界按中断处理
                PushInteract(rAnim, false);            // 读条循环：Interact 播完/掉出就重发
                t += Time.deltaTime;
                yield return null;
            }
        }
        finally
        {
            rescuer.OnHit -= onHit;
            _channeling = false;
        }

        if (interrupted)
        {
            OnInterrupted("救援被打断，请重新下达救援指令");
            yield break;
        }
        if (t < rescueTime)
        {
            OnInterrupted("救援中断：脱离了救援范围，请重新下达救援指令");
            yield break;
        }

        _rescuer = null;
        _routine = null;
        ResetChannelTrigger();
        Revive();
    }

    /// <summary>读条循环：Animator 不在 Interact 或已接近播完 → 重发触发器（节流）。
    /// 正在“进入 Interact”的过渡期内不再压触发器，否则会留下一个没人消费的 Interact：
    /// 打断后它会被 Locomotion 消费，白播一整轮交互动作（R-5 要求打断即停）。</summary>
    void PushInteract(Animator rAnim, bool force)
    {
        if (rAnim == null) return;
        if (!force)
        {
            if (Time.time < _nextInteractPush) return;
            var st = rAnim.GetCurrentAnimatorStateInfo(0);
            bool inInteract = st.IsName("Interact");
            if (!inInteract && rAnim.IsInTransition(0) && rAnim.GetNextAnimatorStateInfo(0).IsName("Interact"))
                return;                                        // 已在进场过渡中
            if (inInteract && st.normalizedTime < 0.95f) return;
        }
        rAnim.SetTrigger("Interact");
        _nextInteractPush = Time.time + InteractRepushGap;
    }

    /// <summary>清掉救援者身上待消费的 Interact 触发器（打断/完成都要清，否则会自己回放一轮）。</summary>
    void ResetChannelTrigger()
    {
        if (_rescuerAnim != null) _rescuerAnim.ResetTrigger("Interact");
        _rescuerAnim = null;
    }

    bool InRange(UnitHealth rescuer, float pad = 0f)
    {
        if (rescuer == null) return false;
        Vector3 d = rescuer.transform.position - transform.position; d.y = 0f;
        return d.magnitude <= rescueRange + pad;
    }

    /// <summary>静默放弃（超时/目标已复活等），不提示、不冷却。</summary>
    void Abort()
    {
        ResetChannelTrigger();
        _channeling = false;
        _rescuer = null;
        _routine = null;
    }

    /// <summary>被打断：清状态 + 清残留触发器 + 提示 + 2s 冷却（冷却内即使条件满足也不重开读条）。</summary>
    void OnInterrupted(string message)
    {
        ResetChannelTrigger();
        _channeling = false;
        _rescuer = null;
        _routine = null;
        _channelBlockUntil = Time.time + InterruptCooldown;
        MessageFeed.SetMessage(message);
    }

    void Revive()
    {
        _channeling = false;
        _rescuer = null;
        IsDown = false;
        AllDowned.Remove(this);
        health.Revive(reviveHp);
        if (agent != null) agent.enabled = true;
        if (animator != null) animator.SetTrigger(reviveTrigger);
        if (brain != null) brain.enabled = false;
        StartCoroutine(RestoreBrain());
        MessageFeed.SetMessage("队友救起来了！");
    }

    IEnumerator RestoreBrain()
    {
        yield return new WaitForSeconds(riseDuration);
        if (brain != null && !IsDown) brain.enabled = true;
    }
}