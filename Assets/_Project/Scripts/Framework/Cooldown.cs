using UnityEngine;

/// <summary>冷却计时结构（商业标配）：一处定义，UI/逻辑共用同一数据源。</summary>
public struct Cooldown
{
    public float duration;
    float _until;

    public bool Ready { get { return Time.time >= _until; } }
    public float Remaining { get { return Mathf.Max(0f, _until - Time.time); } }
    public void Trigger() { _until = Time.time + duration; }
}