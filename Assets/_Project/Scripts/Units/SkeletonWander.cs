using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>骷髅游荡：平时漫游态（AI 冻结、随机走动），玩家进入警觉距离或被攻击时
/// 唤醒 MonsterBrain 进入战斗。双向视野：玩家能看见漫游的怪，怪也能发现靠近的玩家。</summary>
[RequireComponent(typeof(NavMeshAgent))]
public class SkeletonWander : MonoBehaviour
{
    [Tooltip("玩家进入此距离 → 骷髅警觉（进入战斗）")]
    public float aggroRange = 8f;
    [Tooltip("漫游范围（以出生点为圆心）")]
    public float wanderRadius = 10f;
    [Tooltip("漫游移速")]
    public float wanderSpeed = 2.2f;

    NavMeshAgent _agent;
    MonsterBrain _brain;
    UnitHealth _health;
    Vector3 _home;

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _brain = GetComponent<MonsterBrain>();
        _health = GetComponent<UnitHealth>();
        _home = transform.position;
        _agent.speed = wanderSpeed;
        if (_brain != null) _brain.enabled = false;    // 漫游态：不索敌
        if (_health != null) _health.OnHit += _ => Wake();   // ★ 被打立刻醒（事件带参数，用 lambda 包一层）
        StartCoroutine(WanderLoop());
    }

    void Wake()
    {
        if (_brain != null) _brain.enabled = true;
        StopAllCoroutines();
        enabled = false;   // 战斗后本组件退休
    }

    IEnumerator WanderLoop()
    {
        while (true)
        {
            var p = NearestPlayer();
            if (p != null && Vector3.Distance(transform.position, p.position) < aggroRange)
            { Wake(); yield break; }

            if (_agent.isOnNavMesh && !_agent.hasPath)
            {
                Vector3 pt = _home + Random.insideUnitSphere * wanderRadius;
                if (NavMesh.SamplePosition(pt, out var hit, 4f, NavMesh.AllAreas))
                    _agent.SetDestination(hit.position);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    Transform NearestPlayer()
    {
        UnitHealth best = null; float bd = aggroRange;
        foreach (var h in FindObjectsByType<UnitHealth>(FindObjectsSortMode.None))
        {
            if (h.dead || h.team != Team.Player) continue;
            float d = Vector3.Distance(transform.position, h.transform.position);
            if (d < bd) { bd = d; best = h; }
        }
        return best != null ? best.transform : null;
    }
}