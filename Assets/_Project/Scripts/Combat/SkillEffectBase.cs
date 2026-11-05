using UnityEngine;

/// <summary>
/// 技能效果策略：技能 = 效果组件列表（可叠加/组合）。
/// 新增效果 = 建一个继承类资产，SkillSystem 零改动。
/// </summary>
public abstract class SkillEffectBase : ScriptableObject
{
    [InspectorName("需要目标")]
    [Tooltip("是否需要选定敌方目标")]
    public bool needTarget = true;
    [InspectorName("范围")]
    public float range = 2.5f;

    /// <summary>本效果写 Animator 的触发器名；默认 "Skill"，盾击等改写。</summary>
    public virtual string Trigger { get { return "Skill"; } }

    /// <summary>与 <see cref="Trigger"/> 对应的动画状态名（断言/等待用）。</summary>
    public virtual string StateName { get { return "Skill"; } }

    public abstract bool CanCast(SkillCastContext ctx);
    public abstract void Execute(SkillCastContext ctx);
}