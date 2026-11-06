using UnityEngine;
using Dungeon.Content;
using Dungeon.Gameplay.Units;
namespace Dungeon.Gameplay.Combat {

    /// <summary>抛射物基类：追踪+抛物线+到达结算（DamageInfo 管线）+对象池回收。
    /// Launch 时清除拖尾残留，防止池化复用带旧轨迹。</summary>
    public class Projectile : MonoBehaviour
    {
        [HideInInspector] public UnitHealth target;
        [HideInInspector] public UnitHealth source;
        [HideInInspector] public float damage;
        [HideInInspector] public float sharpness;
        [HideInInspector] public float duration = 0.7f;
        [HideInInspector] public float arc = 1.2f;

        protected Vector3 start;
        Vector3 _lastPos;
        Vector3 _fallbackEnd;
        float _t;
        GameObject _prefab;

        public void Launch(GameObject prefab)
        {
            _prefab = prefab;
            start = transform.position;
            _lastPos = start;
            _t = 0f;
            _fallbackEnd = target != null ? target.transform.position : start + transform.forward * 5f;
            transform.localScale = Vector3.one;
            // ★ 清除拖尾残留（池化复用防旧轨迹）
            var trail = GetComponentInChildren<TrailRenderer>();
            if (trail != null) trail.Clear();
        }

        protected virtual Vector3 ComputeEnd()
        {
            if (target != null && !target.dead)
                return AimPoint(target);
            return _fallbackEnd;
        }

        /// <summary>瞄准点：目标所有 Renderer 的合并包围盒中心（不再写死 +0.6m 抬枪口）；
        /// 目标没有 Renderer（纯逻辑单位）时回退到 transform.position。</summary>
        static Vector3 AimPoint(UnitHealth unit)
        {
            var renderers = unit.GetComponentsInChildren<Renderer>();
            bool found = false;
            Bounds merged = default(Bounds);
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                if (!found) { merged = r.bounds; found = true; }
                else merged.Encapsulate(r.bounds);
            }
            return found ? merged.center : unit.transform.position;
        }

        protected virtual Vector3 ComputePosition(Vector3 end, float k)
        {
            Vector3 pos = Vector3.Lerp(start, end, k);
            pos.y += arc * 4f * k * (1f - k);
            return pos;
        }

        protected virtual void OnArrive(Vector3 end)
        {
            if (target != null && !target.dead)
                target.TakeHit(damage, sharpness, DamageType.Thrust, end, source);
        }

        void Update()
        {
            _t += Time.deltaTime;
            float k = Mathf.Clamp01(_t / duration);

            Vector3 end = ComputeEnd();
            if (target != null && target.dead) _fallbackEnd = end;

            Vector3 pos = ComputePosition(end, k);
            if ((pos - _lastPos).sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(pos - _lastPos);
            _lastPos = transform.position = pos;

            if (k >= 1f)
            {
                OnArrive(end);
                SimplePool.Release(_prefab, gameObject);
            }
        }
    }
}