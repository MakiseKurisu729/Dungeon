using UnityEngine;
using Dungeon.Gameplay.Units;
namespace Dungeon.Gameplay.Interaction {

    /// <summary>
    /// 交互参数值对象（非资产、不序列化到场景）：把原来散在 CharacterBrain.CommandInteract 的常量集中在这里。
    /// <see cref="Default"/> 只是兜底常量；CharacterBrain.interactExtraRange 仍是 Inspector 可调的默认值来源。
    /// </summary>
    public readonly struct InteractionDefinition
    {
        /// <summary>兜底参数：落点半径 1.5m、额外距离 0.6m（与重构前 CharacterBrain.CommandInteract 一致）。</summary>
        public static readonly InteractionDefinition Default = new InteractionDefinition(1.5f, 0.6f);

        /// <summary>取不到碰撞体时的兜底落点半径。</summary>
        public readonly float DefaultRadius;

        /// <summary>取不到单位参数时的兜底额外距离。</summary>
        public readonly float RangePadding;

        public InteractionDefinition(float defaultRadius, float rangePadding)
        {
            DefaultRadius = defaultRadius;
            RangePadding = rangePadding;
        }

        /// <summary>交互落点半径：碰撞体 XZ 半宽 + 单位额外距离；无碰撞体时用兜底半径。
        /// 原实现内联在 CharacterBrain.CommandInteract，迁到目标侧后由三个交互物共用这一份算法。</summary>
        public static float RadiusFor(Component target, CharacterBrain unit)
        {
            if (target == null) return Default.DefaultRadius;
            var col = target.GetComponent<Collider>();
            if (col == null) return Default.DefaultRadius;
            float padding = unit != null ? unit.interactExtraRange : Default.RangePadding;
            return Mathf.Max(col.bounds.extents.x, col.bounds.extents.z) + padding;
        }
    }
}
