using UnityEngine;

[CreateAssetMenu(menuName = "Dungeon/Skill Data")]
public class SkillData : ScriptableObject
{
    public string skillName = "技能";
    public Sprite icon;
    public float manaCost = 20f;
    public Cooldown cooldown = new Cooldown { duration = 6f };
    [Tooltip("效果链：按顺序执行，可多个叠加")]
    public SkillEffectBase[] effects;
}