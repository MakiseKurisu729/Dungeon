using UnityEngine;

/// <summary>
/// 技能效果策略：技能 = 效果组件列表（可叠加/组合）。
/// 新增效果 = 建一个继承类资产，SkillSystem 零改动。
/// </summary>
public abstract class SkillEffectBase : ScriptableObject
{
    [Tooltip("是否需要选定敌方目标")]
    public bool needTarget = true;
    public float range = 2.5f;

    public abstract bool CanCast(SkillSystem caster, Transform target);
    public abstract void Execute(SkillSystem caster, Transform target);
}