using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 单位大脑基类：导航移动 + 敌对索敌 + 攻击循环。
/// 子类（Driver）只决定"目标从哪来"：玩家=指令，怪物=AI仇恨。
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public abstract class UnitBrain : MonoBehaviour
{
    public NavMeshAgent agent;
    public UnitStats stats;
    public MonoBehaviour combat;        // 实现 ICharacterCombat（剑/弓）

    protected Transform target;
    protected abstract Team MyTeam { get; }

    bool _hasMove;

    public float AttackRange { get { return stats != null ? stats.attackRange : 2.2f; } }
    public float AggroRange  { get { return stats != null ? stats.aggroRange : 8f; } }
    public float PreferredRange { get { return stats != null ? stats.preferredRange : 0f; } }
    public float DamageValue { get { return stats != null ? stats.damage : 10f; } }

    protected virtual void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (stats != null) agent.speed = stats.moveSpeed;
    }

    // ===== 指令层 =====
    public void CommandMove(Vector3 point)
    {
        _hasMove = true;
        OnCommandMove(point);
    }

    protected virtual void OnCommandMove(Vector3 point)
    {
        if (agent.isOnNavMesh) agent.SetDestination(point);
    }

    public void ClearMove() { _hasMove = false; }

    protected bool ExecutingMove
    {
        get
        {
            if (!_hasMove) return false;
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
            { _hasMove = false; return false; }
            return true;
        }
    }

    protected void StopAgent()
    {
        if (agent != null && agent.isOnNavMesh && agent.hasPath) agent.ResetPath();
    }

    protected float XZDistance(Vector3 p)
    {
        Vector3 d = p - transform.position; d.y = 0f;
        return d.magnitude;
    }

    // ===== 共用能力 =====
    protected Transform FindNearestEnemy(float range)
    {
        UnitHealth best = null;
        float bestD = range;
        foreach (var h in FindObjectsByType<UnitHealth>(FindObjectsSortMode.None))
        {
            if (h.dead || h.team == MyTeam) continue;
            float d = Vector3.Distance(transform.position, h.transform.position);
            if (d < bestD) { bestD = d; best = h; }
        }
        return best != null ? best.transform : null;
    }

    // 攻击循环：到位→面向→执行器开火；超出射程→追击（远程按站桩距离）
    protected void AttackLoop()
    {
        if (target == null) return;
        float dist = XZDistance(target.position);
        if (dist > AttackRange)
        {
            if (agent.isOnNavMesh)
            {
                Vector3 dest = target.position;
                if (PreferredRange > 0f)
                {
                    Vector3 dir = (transform.position - target.position).normalized;
                    dest = target.position + dir * PreferredRange;
                }
                agent.SetDestination(dest);
            }
            return;
        }
        StopAgent();
        Vector3 to = target.position - transform.position; to.y = 0f;
        if (to.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(to), Time.deltaTime * 10f);
        var c = combat as ICharacterCombat;
        if (c != null) c.AutoAttack(target);
    }
}