using System.Collections;
using UnityEngine;

/// <summary>火球术：施法前摇（对齐动画）→ 发射追踪火球 → 到达范围爆炸。时序参数在资产里调。</summary>
[CreateAssetMenu(menuName = "Dungeon/Skill Effects/Fireball")]
public class FireballEffect : SkillEffectBase
{
    [Header("火球")]
    [InspectorName("火球预制体")]
    public GameObject fireballPrefab;

    [Header("时序（对齐施法动画）")]
    [InspectorName("施法前摇")]
    public float castDelay = 0.4f;
    [InspectorName("飞行时长")]
    public float flightDuration = 0.8f;

    [Header("伤害")]
    [InspectorName("伤害")]
    public float damage = 25f;
    [InspectorName("爆炸半径")]
    public float explosionRadius = 2f;

    public override bool CanCast(SkillSystem caster, Transform target)
    {
        if (target == null) { GameData.SetMessage("火球需要选择一个敌人目标"); return false; }
        var uh = target.GetComponent<UnitHealth>();
        if (uh == null || uh.dead) { GameData.SetMessage("火球需要选择一个敌人目标"); return false; }
        Vector3 d = uh.transform.position - caster.transform.position; d.y = 0f;
        if (d.magnitude > range) { GameData.SetMessage("目标超出技能范围"); return false; }
        return true;
    }

    public override void Execute(SkillSystem caster, Transform target)
    {
        var anim = caster.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.ResetTrigger("Interrupt");
            anim.SetTrigger("Skill");
        }
        caster.StartCoroutine(FireRoutine(caster, target));
    }

    IEnumerator FireRoutine(SkillSystem caster, Transform target)
    {
        // 施法前摇：等动画抬手帧
        float t = 0f;
        while (t < castDelay) { t += Time.deltaTime; yield return null; }

        var uh = target != null ? target.GetComponent<UnitHealth>() : null;
        if (uh == null || uh.dead || fireballPrefab == null) yield break;

        Vector3 spawnPos = caster.transform.position + Vector3.up * 1.1f
                         + caster.transform.forward * 0.4f;
        var go = SimplePool.Get(fireballPrefab, spawnPos, Quaternion.identity);
        var p = go.GetComponent<FireballProjectile>();
        if (p != null)
        {
            p.target = uh;
            p.source = caster.GetComponent<UnitHealth>();
            p.damage = damage;
            p.sharpness = 999f;                 // 法术必破防
            p.duration = flightDuration;
            p.arc = 0.3f;
            p.explosionRadius = explosionRadius;
            p.Launch(fireballPrefab);
        }
    }
}