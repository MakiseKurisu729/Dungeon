using UnityEngine;

/// <summary>
/// 技能系统：QWER 四槽（数据驱动）；验蓝+验冷却（Cooldown 结构）；
/// 效果链执行（策略模式）。不认识任何具体技能——新增技能 = 新建效果类资产。
/// </summary>
public class SkillSystem : MonoBehaviour
{
    [InspectorName("蓝量组件")]
    public UnitMana mana;
    [InspectorName("近战执行器")]
    public MeleeCombat sword;

    [InspectorName("技能槽")]
    [Tooltip("Q/W/E/R，空槽留空")]
    public SkillData[] skills = new SkillData[4];

    readonly Cooldown[] _cds = new Cooldown[4];

    void Awake()
    {
        for (int i = 0; i < _cds.Length; i++) _cds[i] = new Cooldown();
    }

    public SkillData GetSkill(int i) { return i >= 0 && i < skills.Length ? skills[i] : null; }
    public float CdRemaining(int i) { return i < _cds.Length ? _cds[i].Remaining : 0f; }
    public float CdTotal(int i) { var s = GetSkill(i); return s != null ? s.cooldown.duration : 1f; }

    /// <summary>清空四槽冷却（调试重置用；不改技能数据，空槽保持空）。</summary>
    public void ResetCooldowns()
    {
        for (int i = 0; i < _cds.Length; i++) _cds[i] = default(Cooldown);
    }

    /// <summary>施法速度倍率：取武器数值资产的 skillSpeed；无资产或非正值按 1（行为不变）。</summary>
    float Speed
    {
        get
        {
            float v = sword != null && sword.stats != null ? sword.stats.skillSpeed : 1f;
            return v > 0f ? v : 1f;
        }
    }

    /// <summary>为某个效果组包本次施放上下文（触发器/状态名由效果自己声明，系统不认识具体技能）。</summary>
    SkillCastContext ContextFor(int index, SkillEffectBase effect, Transform target)
    {
        return new SkillCastContext(index, effect.Trigger, effect.StateName, target, this,
                                    GetComponent<UnitHealth>(), Speed);
    }

    /// <summary>施放：验蓝验冷却 → 效果链。target 仅需要目标的技能用。</summary>
    public bool Cast(int index, Transform target)
    {
        var s = GetSkill(index);
        if (s == null || s.effects == null || s.effects.Length == 0) return false;
        if (!_cds[index].Ready) { MessageFeed.SetMessage($"{s.skillName} 冷却中"); return false; }
        if (mana == null) { MessageFeed.SetMessage("蓝量不足"); return false; }

        // 先验效果可释放（CanCast），再扣蓝：施放失败不产生任何消耗，也不写冷却。
        foreach (var eff in s.effects)
        {
            if (!eff.CanCast(ContextFor(index, eff, target))) return false;
        }
        if (!mana.Consume(s.manaCost)) { MessageFeed.SetMessage("蓝量不足"); return false; }

        foreach (var eff in s.effects)
        {
            eff.Execute(ContextFor(index, eff, target));
        }

        _cds[index] = s.cooldown;
        _cds[index].Trigger();
        return true;
    }
}