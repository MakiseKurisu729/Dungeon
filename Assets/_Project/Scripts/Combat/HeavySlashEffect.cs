using UnityEngine;

/// <summary>强化斩：立即释放一次大威力近战攻击（数据全在 MeleeCombat，无额外参数）。</summary>
[CreateAssetMenu(menuName = "Dungeon/Skill Effects/Heavy Slash")]
public class HeavySlashEffect : SkillEffectBase
{
    public HeavySlashEffect() { needTarget = false; }

    public override bool CanCast(SkillCastContext ctx) { return ctx.Caster.sword != null; }

    public override void Execute(SkillCastContext ctx)
    {
        ctx.Caster.sword.DoSkillAttack();
    }
}
