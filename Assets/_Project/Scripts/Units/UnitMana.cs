using UnityEngine;

/// <summary>法力值：固定上限、每秒自动回复、技能消耗。不衰减、不积攒。</summary>
public class UnitMana : MonoBehaviour
{
    [InspectorName("蓝量上限")]
    public float max = 100f;
    [InspectorName("每秒回复")]
    public float regenPerSec = 10f;   // 测试期回复，平衡期再调
    public float current { get; private set; }

    void Start() { current = max; }

    void Update()
    {
        if (current < max)
            current = Mathf.Min(max, current + regenPerSec * Time.deltaTime);
    }

    /// <summary>回满蓝（调试重置用；不回改上限与回复速率）。</summary>
    public void Refill() { current = max; }

    public bool Consume(float v)
    {
        if (current < v) return false;
        current -= v;
        return true;
    }
}