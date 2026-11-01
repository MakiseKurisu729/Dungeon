using UnityEngine;

/// <summary>带盾单位：正面扇形内受到的伤害减免（Boss 用）。
/// 弓手绕到背后可打出全额伤害——配合玩法的核心机制。</summary>
public class ShieldedUnitHealth : UnitHealth
{
    [Tooltip("正面减伤半角（度）：60 = 正面 120° 范围减伤")]
    public float frontHalfAngle = 60f;
    [Tooltip("正面伤害系数：0.5 = 正面伤害减半")]
    public float frontDamageFactor = 0.5f;

    public override bool TakeHit(float rawDamage, float sharpness, DamageType damageType, Vector3 hitPos, UnitHealth source = null)
    {
        if (source != null)
        {
            Vector3 to = source.transform.position - transform.position;
            to.y = 0f;
            if (Vector3.Angle(transform.forward, to) < frontHalfAngle)
                rawDamage *= frontDamageFactor;   // 正面：盾挡
        }
        return base.TakeHit(rawDamage, sharpness, damageType, hitPos, source);
    }
}