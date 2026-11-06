using UnityEngine;
using Dungeon.Gameplay.Units;
namespace Dungeon.Gameplay.Combat {

    /// <summary>
    /// 一次技能施放的完整上下文：由 <see cref="SkillSystem.Cast"/> 组包后传给效果链。
    /// 效果只关心「这一招要做什么」，不再自己拼 Animator 触发器/状态名，也不必猜目标与施法者。
    /// 值类型：构造后不可变，需要换目标时用 <see cref="WithTarget"/> 派生新值。
    /// </summary>
    public readonly struct SkillCastContext
    {
        /// <summary>槽位：0..3 = Q/W/E/R。</summary>
        public readonly int Slot;
        /// <summary>本次施放要写的 Animator 触发器名（"Skill" / "Bash"）。</summary>
        public readonly string Trigger;
        /// <summary>与触发器对应的动画状态名（断言/等待用）。</summary>
        public readonly string StateName;
        /// <summary>目标（可能为 null：不需要目标的技能）。</summary>
        public readonly Transform Target;
        /// <summary>施法系统（原 CanCast/Execute 的第一个参数）。</summary>
        public readonly SkillSystem Caster;
        /// <summary>施法者单位（可能为 null；伤害来源统一用它）。</summary>
        public readonly UnitHealth CasterUnit;
        /// <summary>施法速度倍率（恒 > 0；1 = 原始时长）。效果时长 = 原值 / Speed。</summary>
        public readonly float Speed;

        public SkillCastContext(int slot, string trigger, string stateName, Transform target,
                                SkillSystem caster, UnitHealth casterUnit, float speed)
        {
            Slot = slot;
            Trigger = trigger;
            StateName = stateName;
            Target = target;
            Caster = caster;
            CasterUnit = casterUnit;
            Speed = speed;
        }

        /// <summary>便捷派生：只替换目标，其余字段保持不变。</summary>
        public SkillCastContext WithTarget(Transform target)
        {
            return new SkillCastContext(Slot, Trigger, StateName, target, Caster, CasterUnit, Speed);
        }
    }
}
