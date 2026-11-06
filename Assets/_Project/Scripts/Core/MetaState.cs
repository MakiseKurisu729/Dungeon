using Dungeon.Core;
using Dungeon.Gameplay.Combat;
using Dungeon.Gameplay.Units;
/// <summary>成长状态：等级 + 升级收益。
/// 生命周期 = 进程内（跨场景重载保留），与重构前 GameData 的 level 字段一致；本类不新增任何自动重置。</summary>
public static class MetaState
{
    /// <summary>等级。默认 1（与重构前一致）。</summary>
    public static int Level { get; set; } = 1;

    /// <summary>当前升级费用：50 * Level。</summary>
    public static int LevelCost() { return 50 * Level; }

    /// <summary>升级：金币够则扣费、等级 +1、骑士伤害倍率 +0.25、生命上限 +20，并通知变更。
    /// 与重构前 GameData 的 TryLevelUp 逐行等价（含 OnChanged 触发）。</summary>
    public static bool TryLevelUp(MeleeCombat sw, UnitHealth uh)
    {
        if (RunState.Gold < LevelCost()) return false;
        RunState.Gold -= LevelCost();
        Level++;
        if (sw != null) sw.dmgMult += 0.25f;
        if (uh != null) uh.AddMaxHp(20f);
        MessageFeed.RaiseChanged();
        return true;
    }
}
