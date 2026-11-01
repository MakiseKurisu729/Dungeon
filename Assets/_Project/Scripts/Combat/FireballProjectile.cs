using UnityEngine;

/// <summary>火球（法师普攻弹）：追踪+池化；到达时生成两段式爆炸并注入伤害参数，
/// 伤害在爆炸爆发帧（碎片迸出瞬间）结算。</summary>
public class FireballProjectile : Projectile
{
    [Tooltip("爆炸半径（米）")]
    public float explosionRadius = 2f;
    [Tooltip("爆炸特效预制体（挂两段式 ExplosionFx）")]
    public GameObject explosionEffect;

    protected override void OnArrive(Vector3 end)
    {
        if (explosionEffect == null) return;

        var fx = Instantiate(explosionEffect, end, Quaternion.identity);
        fx.transform.localScale = Vector3.one * (explosionRadius * 2.2f);

        var efx = fx.GetComponent<ExplosionFx>();
        if (efx != null)
        {
            efx.damageRadius = explosionRadius;
            efx.damage = damage;
            efx.sharpness = sharpness;
            efx.source = source;
        }
        // 5 秒兜底：碎片最长寿命 debrisMaxLife=4s，管理器至少活到那时（碎片另有引擎级自毁保险）
        Destroy(fx, 5f);
    }
}