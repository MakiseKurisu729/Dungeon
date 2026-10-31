using UnityEngine;

/// <summary>伤害数据包：一次伤害结算的全部上下文（暴击/增伤/飘字/音效/统计的统一挂点）。</summary>
public struct DamageInfo
{
    public UnitHealth source;      // 来源（可为空）
    public UnitHealth target;
    public DamageType damageType;
    public float rawAmount;
    public float finalAmount;
    public bool brokeDefense;     // false = 弹刀未破防
    public Vector3 hitPos;
}

/// <summary>伤害已结算（跨系统事件：飘字/音效/统计订阅它）。</summary>
public struct DamageDealtEvent { public DamageInfo info; }

/// <summary>单位死亡（掉落/任务/UI 订阅它）。</summary>
public struct UnitDiedEvent { public UnitHealth unit; }