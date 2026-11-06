using UnityEngine;
using Dungeon.Content;
using Dungeon.Gameplay.Units;
namespace Dungeon.Gameplay.Combat {

    /// <summary>
    /// 伤害元素类型（飘字配色/后续抗性轴）。
    /// 与武器类别轴 <see cref="DamageType"/>（Slash/Thrust/Blunt）正交；后续类型在此追加。
    /// </summary>
    public enum DamageElement { Physical, Fire, Ice, Lightning, Poison, Holy, Shadow }

    /// <summary>伤害数据包：一次伤害结算的全部上下文（暴击/增伤/飘字/音效/统计的统一挂点）。</summary>
    public struct DamageInfo
    {
        public UnitHealth source;      // 来源（可为空）
        public UnitHealth target;
        public DamageType damageType;
        public DamageElement element;  // 伤害元素；枚举 0 值即默认 Physical（结构体字段不可写初始化器）
        public float rawAmount;
        public float finalAmount;
        public bool brokeDefense;     // false = 弹刀未破防
        public Vector3 hitPos;
    }

    /// <summary>伤害已结算（跨系统事件：飘字/音效/统计订阅它）。</summary>
    public struct DamageDealtEvent { public DamageInfo info; }

    /// <summary>单位死亡（掉落/任务/UI 订阅它）。</summary>
    public struct UnitDiedEvent { public UnitHealth unit; }
}