using UnityEngine;

/// <summary>
/// 把 UnitStats.attackSpeed 写入子节点 Animator 的 "AttackSpeed" 浮点参数。
/// 动画器自动向下查找，根上不需要 Animator 组件。
/// </summary>
public class UnitAnimatorSpeed : MonoBehaviour
{
    public UnitStats stats;
    Animator _animator;

    void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (_animator == null) return;
        float s = stats != null ? stats.attackSpeed : 1f;
        if (!Mathf.Approximately(_animator.GetFloat("AttackSpeed"), s))
            _animator.SetFloat("AttackSpeed", s);
    }
}