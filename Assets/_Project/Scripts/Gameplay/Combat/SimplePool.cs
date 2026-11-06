using System.Collections.Generic;
using UnityEngine;
namespace Dungeon.Gameplay.Combat {

    /// <summary>
    /// 极简对象池：按预制体分池复用。Get 取（空了自动实例化），Release 还。
    /// 消除运行时 Instantiate/Destroy 的卡顿来源——所有频繁生成物（箭/火球/伤害数字）都该走池。
    /// </summary>
    public static class SimplePool
    {
        static readonly Dictionary<GameObject, Queue<GameObject>> _pools = new Dictionary<GameObject, Queue<GameObject>>();

        public static GameObject Get(GameObject prefab, Vector3 pos, Quaternion rot)
        {
            if (_pools.TryGetValue(prefab, out var q) && q.Count > 0)
            {
                var item = q.Dequeue();
                item.transform.SetPositionAndRotation(pos, rot);
                item.SetActive(true);
                return item;
            }
            return Object.Instantiate(prefab, pos, rot);
        }

        public static void Release(GameObject prefab, GameObject item)
        {
            item.SetActive(false);
            if (!_pools.TryGetValue(prefab, out var q))
            {
                q = new Queue<GameObject>();
                _pools[prefab] = q;
            }
            q.Enqueue(item);
        }
    }
}