using UnityEngine;

public enum DamageType { Slash, Thrust, Blunt }
public enum GripType { OneHandOnly, TwoHandOnly }

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Dungeon/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("基础")]
    [InspectorName("武器名称")]
    public string weaponName = "铁剑";
    [InspectorName("伤害类型")]
    public DamageType damageType = DamageType.Slash;
    [InspectorName("握持方式")]
    public GripType grip = GripType.TwoHandOnly;

    [Header("数值")]
    [InspectorName("基础伤害")]
    [Tooltip("基础伤害")]
    public float baseDamage = 10f;
    [InspectorName("锋利度")]
    [Tooltip("锋利度：≥部位坚硬度才能破防；蓄力可临时提高")]
    public float sharpness = 8f;
}