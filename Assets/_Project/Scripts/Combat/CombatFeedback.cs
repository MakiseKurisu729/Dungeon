using UnityEngine;

/// <summary>战斗反馈：订阅伤害事件 → 飘字。EventBus 跨系统通信的样板用法。</summary>
public class CombatFeedback : MonoBehaviour
{
    void Awake()
    {
        EventBus.Subscribe<DamageDealtEvent>(OnDamage);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<DamageDealtEvent>(OnDamage);
    }

    void OnDamage(DamageDealtEvent e)
    {
        if (!e.info.target.showDamageNumber) return;
        if (e.info.finalAmount <= 0f) return;
        DamageNumber.Show(e.info.hitPos, e.info.finalAmount.ToString("F0"), Color.yellow);
    }
}