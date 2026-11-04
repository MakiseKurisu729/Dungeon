using UnityEngine;

/// <summary>
/// 把 UnitStats.attackSpeed 写入子节点 Animator 的 "AttackSpeed" 浮点参数。
/// 动画器自动向下查找，根上不需要 Animator 组件。
/// </summary>
public class UnitAnimatorSpeed : MonoBehaviour
{
    [InspectorName("数值资产")]
    public UnitStats stats;
    Animator _animator;
    UnitBrain _brain;

    void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _brain = GetComponent<UnitBrain>();
    }

    void Update()
    {
        if (_animator == null) return;
        // ★ 优先取同一单位 Driver 的 stats：预制体变体（弓/法）会继承基础预制体的 stats 槽，
        //   导致 AttackSpeed 参数与角色真实攻速不同源。
        UnitStats s = _brain != null && _brain.stats != null ? _brain.stats : stats;
        float v = s != null ? s.attackSpeed : 1f;
        if (!Mathf.Approximately(_animator.GetFloat("AttackSpeed"), v))
            _animator.SetFloat("AttackSpeed", v);
    }
}