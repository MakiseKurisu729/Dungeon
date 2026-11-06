using UnityEngine;
using UnityEngine.AI;
using Dungeon.Gameplay.Units;
namespace Dungeon.Gameplay.World {

    /// <summary>森林骷髅真随机生成：随机组数（每组 1~4 只随机）散布在 NavMesh 上，
    /// 自动挂 SkeletonWander + Visibility。延迟 0.3 秒生成，等 ForestGenerator 的
    /// NavMeshObstacle 挖洞完成，避免刷进石头里。</summary>
    public class SkeletonSpawner : MonoBehaviour
    {
        public GameObject[] skeletonPrefabs;
        public int groupCountMin = 5;
        public int groupCountMax = 8;
        public int groupSizeMin = 1;
        public int groupSizeMax = 4;
        public float mapRadius = 42f;
        public float minDistanceFromCamp = 16f;
        public Transform campCenter;
        [Tooltip("生成延迟（秒）：等环境障碍物 carve 导航网格")]
        public float spawnDelay = 0.3f;

        void Start()
        {
            Invoke(nameof(SpawnAll), spawnDelay);
        }

        void SpawnAll()
        {
            if (skeletonPrefabs == null || skeletonPrefabs.Length == 0) return;
            int groups = Random.Range(groupCountMin, groupCountMax + 1);
            for (int g = 0; g < groups; g++)
            {
                Vector3 c = RandomPoint();
                if (c == transform.position) continue;
                int n = Random.Range(groupSizeMin, groupSizeMax + 1);
                for (int i = 0; i < n; i++)
                {
                    Vector3 p = c + Random.insideUnitSphere * 3f; p.y = c.y;
                    if (!NavMesh.SamplePosition(p, out var hit, 3f, NavMesh.AllAreas)) continue;
                    var prefab = skeletonPrefabs[Random.Range(0, skeletonPrefabs.Length)];
                    var go = Instantiate(prefab, hit.position, Quaternion.Euler(0, Random.Range(0, 360f), 0));
                    if (go.GetComponent<SkeletonWander>() == null) go.AddComponent<SkeletonWander>();
                    if (go.GetComponent<Visibility>() == null) go.AddComponent<Visibility>();
                }
            }
        }

        Vector3 RandomPoint()
        {
            for (int tries = 0; tries < 20; tries++)
            {
                Vector3 p = transform.position + Random.insideUnitSphere * mapRadius;
                if (campCenter != null &&
                    Vector3.Distance(p, campCenter.position) < minDistanceFromCamp) continue;
                if (NavMesh.SamplePosition(p, out var hit, 5f, NavMesh.AllAreas))
                    return hit.position;
            }
            return transform.position;
        }
    }
}