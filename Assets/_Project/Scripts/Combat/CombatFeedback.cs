using System.Collections;
using UnityEngine;

/// <summary>战斗反馈：订阅伤害事件 → 飘字延迟到命中特效峰值（表现对齐表现）。
/// 伤害结算本身由逻辑层即时完成，本组件只管"何时让玩家看见"。</summary>
public class CombatFeedback : MonoBehaviour
{
    [Tooltip("飘字延迟（秒）：数字跳出来对齐爆炸涨开的时刻")]
    public float damageNumberDelay = 0.12f;

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
        StartCoroutine(ShowNumber(e.info));
    }

    IEnumerator ShowNumber(DamageInfo info)
    {
        float t = 0f;
        while (t < damageNumberDelay) { t += Time.deltaTime; yield return null; }
        DamageNumber.Show(info.hitPos, info.finalAmount.ToString("F0"), Color.yellow);
    }
}