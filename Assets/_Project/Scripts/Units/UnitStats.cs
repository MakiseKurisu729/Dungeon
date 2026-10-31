using UnityEngine;

[CreateAssetMenu(menuName = "Dungeon/Unit Stats")]
public class UnitStats : ScriptableObject
{
    public string unitName = "单位";
    public Team team = Team.Monster;
    public float maxHp = 50f;
    public float damage = 10f;
    public float attackRange = 2.2f;
    public float preferredRange = 0f;
    public float aggroRange = 8f;
    public float moveSpeed = 4f;
    [Tooltip("攻速倍率：1=正常，1.3=快30%")]
    public float attackSpeed = 1f;
}