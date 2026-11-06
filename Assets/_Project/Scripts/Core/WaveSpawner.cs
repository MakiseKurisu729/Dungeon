using System.Collections.Generic;
using UnityEngine;
using Dungeon.Core;

/// <summary>波次生成器：按 UnitHealth 判定存活；每波血量递增。</summary>
public class WaveSpawner : MonoBehaviour
{
    [System.Serializable]
    public class Wave
    {
        [InspectorName("单位预制体")]
        public GameObject[] unitPrefabs;    // 带 UnitHealth+UnitBrain 的完整单位预制体
        [InspectorName("生成偏移")]
        public Vector3[] offsets;
    }

    [InspectorName("波次列表")]
    public Wave[] waves;
    [InspectorName("波次间隔")]
    public float waveDelay = 3f;
    [InspectorName("每波血量增幅")]
    public float hpScalePerWave = 0.3f;

    readonly List<UnitHealth> _alive = new List<UnitHealth>();
    int _waveIdx;
    float _timer = -1f;
    bool _done;

    void Start() { SpawnWave(); }

    void Update()
    {
        if (_done) return;
        _alive.RemoveAll(h => h == null);
        if (_alive.Count > 0) return;

        if (_timer < 0f)
        {
            _timer = waveDelay;
            MessageFeed.SetMessage(_waveIdx + 1 >= waves.Length
                ? "胜利！地牢已清空！"
                : $"第 {_waveIdx + 2} 波即将来临……");
        }

        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            _waveIdx++;
            if (_waveIdx >= waves.Length) { _done = true; return; }
            SpawnWave();
            _timer = -1f;
        }
    }

    void SpawnWave()
    {
        var w = waves[_waveIdx];
        float hpMul = 1f + hpScalePerWave * _waveIdx;

        for (int i = 0; i < w.unitPrefabs.Length; i++)
        {
            Vector3 offset = (w.offsets != null && i < w.offsets.Length)
                ? w.offsets[i]
                : new Vector3(i * 1.5f - 2f, 0f, 0f);
            var go = Instantiate(w.unitPrefabs[i], transform.position + offset, transform.rotation);
            var uh = go.GetComponent<UnitHealth>();
            if (uh != null && hpMul != 1f) uh.ScaleMaxHp(hpMul);
            if (uh != null) _alive.Add(uh);
        }
        MessageFeed.SetMessage($"第 {_waveIdx + 1} 波！");
    }
}