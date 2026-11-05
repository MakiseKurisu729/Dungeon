using UnityEngine;

[CreateAssetMenu(menuName = "Dungeon/Unit Stats")]
public class UnitStats : ScriptableObject
{
    [InspectorName("单位名称")]
    public string unitName = "单位";
    [InspectorName("队伍")]
    public Team team = Team.Monster;
    [InspectorName("生命上限")]
    public float maxHp = 50f;
    [InspectorName("伤害")]
    public float damage = 10f;
    [InspectorName("攻击距离")]
    public float attackRange = 2.2f;
    [InspectorName("站桩距离")]
    public float preferredRange = 0f;
    [InspectorName("索敌范围")]
    public float aggroRange = 8f;
    [InspectorName("移速")]
    public float moveSpeed = 4f;
    [InspectorName("攻速倍率")]
    [Tooltip("攻速倍率：1=正常，1.3=快30%")]
    public float attackSpeed = 1f;
    [InspectorName("施法速度倍率")]
    [Tooltip("施法速度倍率：1=正常（默认）；2=效果时长减半。时长 = 原值 / 倍率，动画触发器时机不变")]
    public float skillSpeed = 1f;
}