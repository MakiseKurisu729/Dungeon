using UnityEngine;

/// <summary>角色战斗执行器接口：自动攻击 + 指令打断。</summary>
public interface ICharacterCombat
{
    void AutoAttack(Transform target);
    void CancelAttack();
}