using UnityEngine;

/// <summary>法力值：固定上限、每秒自动回复、技能消耗。不衰减、不积攒。</summary>
public class UnitMana : MonoBehaviour
{
    public float max = 100f;
    public float regenPerSec = 10f;   // 测试期回复，平衡期再调
    public float current { get; private set; }

    void Start() { current = max; }

    void Update()
    {
        if (current < max)
            current = Mathf.Min(max, current + regenPerSec * Time.deltaTime);
    }

    public bool Consume(float v)
    {
        if (current < v) return false;
        current -= v;
        return true;
    }
}