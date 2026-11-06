using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace Dungeon.Gameplay.Units {

    /// <summary>
    /// 运动锁：技能等「原地动作」期间统一停住导航，并在任意结束路径复位（不新增组件、不落盘）。
    /// 只记住 Begin 前的 isStopped，不做引用计数——同一单位同一时刻只有一段技能协程，
    /// 重复 Begin 不覆盖原值、End 幂等（第二次 End 只是安全复位为「可动」）。
    /// 为什么不用计数：DownedRescue.OnDisable / CharacterBrain 离开技能状态 / 效果协程收尾
    /// 三条路径都会 End，计数一旦对不上就会永远解锁不了；「记原值 + 幂等 End」等价且不会卡死。
    /// </summary>
    public static class UnitMotion
    {
        /// <summary>agent 实例 → Begin 之前的 isStopped（键存在 = 当前被锁）。</summary>
        static readonly Dictionary<int, bool> WasStopped = new Dictionary<int, bool>();

        /// <summary>锁住：记下原值后 isStopped = true（不 ResetPath，不打断已有路径数据）。</summary>
        public static void Begin(NavMeshAgent agent)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
            int id = agent.GetInstanceID();
            if (!WasStopped.ContainsKey(id)) WasStopped[id] = agent.isStopped;
            agent.isStopped = true;
        }

        /// <summary>解锁：恢复 Begin 前的 isStopped；未 Begin 过则复位为可动（安全复位）。
        /// agent 为空 / 已禁用 / 未上导航时只清锁记录并安全返回（改 isStopped 会报错）。</summary>
        public static void End(NavMeshAgent agent)
        {
            if (agent == null) return;
            int id = agent.GetInstanceID();
            bool was;
            bool known = WasStopped.TryGetValue(id, out was);
            WasStopped.Remove(id);

            if (!agent.enabled || !agent.isOnNavMesh) return;
            agent.isStopped = known ? was : false;
        }

        /// <summary>当前是否被锁（断言/排查用）。</summary>
        public static bool IsHeld(NavMeshAgent agent)
        {
            if (agent == null) return false;
            return WasStopped.ContainsKey(agent.GetInstanceID());
        }
    }
}
