using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>倒地与救援（动画机驱动版）：挂玩家单位，且同物体挂 Interactable。
/// 倒地：血空 → SetTrigger(Die) → Downed 保持末端；AI/移动禁用。
/// 救援：点击交互物 → 选中队友走过去 → 进 rescueRange 自动播 Interact 读条；
/// 打断：救援者受击 → SetTrigger(Hit)；完成：SetTrigger(Revive) → Rise 起身 → 1血恢复。</summary>
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
    bool _rescuing;

    void Start()
    {
        if (health == null) health = GetComponent<UnitHealth>();
        if (brain == null) brain = GetComponent<UnitBrain>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (health != null) health.OnDeath += OnDowned;
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
        if (animator != null) animator.SetTrigger(dieTrigger);
        GameData.SetMessage($"{gameObject.name} 倒下了！选中队友点击他进行救援");
    }

    void OnDestroy() { AllDowned.Remove(this); }

    void Update()
    {
        if (!IsDown || _rescuing) return;
        if (PauseManager.instance != null && PauseManager.instance.IsPaused) return;
        if (SelectionManager.instance == null || SelectionManager.instance.Primary == null) return;

        var rescuer = SelectionManager.instance.Primary.GetComponentInParent<UnitHealth>();
        if (rescuer == null || rescuer.dead || rescuer == health) return;
        if (Vector3.Distance(rescuer.transform.position, transform.position) > rescueRange) return;

        StartCoroutine(RescueRoutine(rescuer));
    }

    IEnumerator RescueRoutine(UnitHealth rescuer)
    {
        _rescuing = true;
        GameData.SetMessage("救援中……受击会打断！");

        var rAnim = rescuer.GetComponentInChildren<Animator>();
        if (rAnim != null) rAnim.SetTrigger("Interact");
        Vector3 dir = transform.position - rescuer.transform.position; dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f) rescuer.transform.rotation = Quaternion.LookRotation(dir);

        float t = 0f;
        bool interrupted = false;
        System.Action<DamageInfo> onHit = _ => interrupted = true;
        rescuer.OnHit += onHit;

        while (t < rescueTime)
        {
            if (interrupted) break;
            if (Vector3.Distance(rescuer.transform.position, transform.position) > rescueRange + 1f) break;
            t += Time.deltaTime;
            yield return null;
        }
        rescuer.OnHit -= onHit;
        _rescuing = false;

        if (interrupted)
        {
            if (rAnim != null) rAnim.SetTrigger("Hit");
            GameData.SetMessage("救援被打断！");
            yield break;
        }
        if (t < rescueTime) yield break;

        Revive();
    }

    void Revive()
    {
        IsDown = false;
        AllDowned.Remove(this);
        health.Revive(reviveHp);
        if (agent != null) agent.enabled = true;
        if (animator != null) animator.SetTrigger(reviveTrigger);
        if (brain != null) brain.enabled = false;
        StartCoroutine(RestoreBrain());
        GameData.SetMessage("队友救起来了！");
    }

    IEnumerator RestoreBrain()
    {
        yield return new WaitForSeconds(riseDuration);
        if (brain != null && !IsDown) brain.enabled = true;
    }
}