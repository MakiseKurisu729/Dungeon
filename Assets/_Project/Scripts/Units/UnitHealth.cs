using UnityEngine;

public enum BodyPartType { Normal, Weak, Hard, Critical }

/// <summary>统一生命：受击管线（破防/部位倍率）；DamageInfo + 事件。
/// TakeHit 为 virtual（带盾单位可重写）；支持 Revive 复活（倒地救援用）。</summary>
public class UnitHealth : MonoBehaviour
{
    public UnitStats stats;
    public Team team = Team.Monster;
    public float maxHp = 100f;
    public float hp { get; private set; }
    public bool dead { get; private set; }

    [Header("部位")]
    public BodyPartType partType = BodyPartType.Normal;
    public float hardness = 5f;
    public bool showDamageNumber = true;

    public event System.Action<DamageInfo> OnHit;
    public event System.Action OnDeath;
    public event System.Action OnRevived;

    Renderer[] _renderers;
    Color[] _colors;
    float _flashTimer;

    void Awake()
    {
        if (stats == null)
            Debug.LogError($"[UnitHealth] {name} 的 Stats 槽为空！队伍与 HP 不可靠！", this);
        else { maxHp = stats.maxHp; team = stats.team; }

        hp = maxHp;
        _renderers = GetComponentsInChildren<Renderer>();
        _colors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
            _colors[i] = _renderers[i].material.color;

        // ★ 单位注册（必须在 team 赋值之后）：全场扫描的唯一事实源，见 UnitRegistry
        UnitRegistry.Register(this);
    }

    void OnEnable() { UnitRegistry.SetActiveState(this, true); }

    // 只维护“激活”标志，不注销：SetActive(false→true) 不能丢身份
    void OnDisable() { UnitRegistry.SetActiveState(this, gameObject.activeInHierarchy); }

    void OnDestroy() { UnitRegistry.Unregister(this); }

    void Update()
    {
        if (_flashTimer > 0f)
        {
            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f)
                for (int i = 0; i < _renderers.Length; i++)
                    if (_renderers[i] != null) _renderers[i].material.color = _colors[i];
        }
    }

    public virtual bool TakeHit(float rawDamage, float sharpness, DamageType damageType, Vector3 hitPos, UnitHealth source = null)
    {
        if (dead) return true;
        var info = new DamageInfo
        {
            source = source, target = this, damageType = damageType,
            rawAmount = rawDamage, hitPos = hitPos
        };

        if (sharpness < hardness)
        {
            info.brokeDefense = false;
            info.finalAmount = 0f;
            Debug.Log($"弹刀！锋利 {sharpness:F1} < 坚硬 {hardness:F1}");
            return false;
        }

        float mult = partType == BodyPartType.Weak ? 1.5f
                   : partType == BodyPartType.Critical ? 2f : 1f;
        info.brokeDefense = true;
        ApplyDamageInfo(info, rawDamage * mult);
        return true;
    }

    public void ApplyDamage(float amount, Vector3 hitPos, UnitHealth source = null)
    {
        ApplyDamageInfo(new DamageInfo
        {
            source = source, target = this, damageType = DamageType.Blunt,
            rawAmount = amount, hitPos = hitPos, brokeDefense = true
        }, amount);
    }

    void ApplyDamageInfo(DamageInfo info, float final)
    {
        if (dead) return;
        info.finalAmount = final;
        hp -= final;
        Flash();

        OnHit?.Invoke(info);
        EventBus.Publish(new DamageDealtEvent { info = info });

        if (hp <= 0f)
        {
            dead = true;
            OnDeath?.Invoke();
            EventBus.Publish(new UnitDiedEvent { unit = this });
        }
    }

    // ★ 复活（倒地救援）
    public void Revive(float hpValue)
    {
        if (!dead) return;
        dead = false;
        hp = Mathf.Clamp(hpValue, 1f, maxHp);
        OnRevived?.Invoke();
    }

    public void HealFull() { if (!dead) hp = maxHp; }
    public void ScaleMaxHp(float mult) { maxHp = Mathf.RoundToInt(maxHp * mult); hp = maxHp; }
    public void AddMaxHp(float v) { maxHp += v; hp = maxHp; }

    void Flash()
    {
        foreach (var r in _renderers)
            if (r != null) r.material.color = Color.red;
        _flashTimer = 0.1f;
    }
}