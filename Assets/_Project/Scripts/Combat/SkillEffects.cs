using System.Collections;
using UnityEngine;

/// <summary>盾击：指定敌人，命中触发受击打断 + 线性击退 + 普攻伤害。
/// 时序参数在资产里调：Total Time=动画总时长；Hit Time=盾砸中帧÷采样率。
/// （基类 SkillEffectBase 在独立文件 SkillEffectBase.cs 中，此处不重复定义）</summary>
[CreateAssetMenu(menuName = "Dungeon/Skill Effects/Bash")]
public class BashEffect : SkillEffectBase
{
    [Header("时序（对齐动画帧）")]
    [InspectorName("盾击总时长")]
    public float totalTime = 0.6f;
    [InspectorName("盾击命中时机")]
    [Tooltip("结算时间点（秒）= 砸中帧 ÷ 采样率")]
    public float hitTime = 0.3f;

    [Header("击退参数")]
    [InspectorName("僵直时长")]
    public float stagger = 0.5f;
    [InspectorName("击退距离")]
    public float knockDist = 2f;
    [InspectorName("击退时长")]
    public float knockDuration = 0.3f;

    public override bool CanCast(SkillSystem caster, Transform target)
    {
        if (target == null) { GameData.SetMessage("盾击需要选择一个敌人目标"); return false; }
        var uh = target.GetComponent<UnitHealth>();
        if (uh == null || uh.dead) { GameData.SetMessage("盾击需要选择一个敌人目标"); return false; }
        Vector3 d = uh.transform.position - caster.transform.position; d.y = 0f;
        if (d.magnitude > range) { GameData.SetMessage("目标超出技能范围"); return false; }
        return true;
    }

    public override void Execute(SkillSystem caster, Transform target)
    {
        caster.StartCoroutine(BashRoutine(caster, target));
    }

    IEnumerator BashRoutine(SkillSystem caster, Transform target)
    {
        // ★ 新增：施法即停（LoL 手感：放技能时不再移动，避免边跑边砸）
        var agent = caster.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath(); }

        // 出招：收掉普攻（防双重判定）→ 面向目标 → 触发动画
        if (caster.sword != null) caster.sword.CancelAttack();
        Vector3 d = target.position - caster.transform.position; d.y = 0f;
        caster.transform.rotation = Quaternion.LookRotation(d);
        var anim = caster.GetComponentInChildren<Animator>();
        if (anim != null) anim.SetTrigger("Bash");

        // 前摇：等结算帧
        float t = 0f;
        while (t < hitTime) { t += Time.deltaTime; yield return null; }

        // 结算：伤害 + 击退 + 僵直
        var uh = target.GetComponent<UnitHealth>();
        if (uh != null && !uh.dead)
        {
            float dmg = caster.sword != null ? caster.sword.GetRawDamage() : 10f;
            uh.TakeHit(dmg, 999f, DamageType.Blunt, uh.transform.position + Vector3.up * 0.6f,
                       caster.GetComponent<UnitHealth>());
            var brain = uh.GetComponent<MonsterBrain>();
            if (brain != null)
            {
                Vector3 dir = uh.transform.position - caster.transform.position; dir.y = 0f;
                brain.ForceHit(dir, stagger, knockDist, knockDuration);
            }
        }

        // 后摇：等动画收尾
        while (t < totalTime) { t += Time.deltaTime; yield return null; }
    }
}