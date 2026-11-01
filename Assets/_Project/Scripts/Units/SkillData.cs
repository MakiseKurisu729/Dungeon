using UnityEngine;

[CreateAssetMenu(menuName = "Dungeon/Skill Data")]
public class SkillData : ScriptableObject
{
    [InspectorName("技能名称")]
    public string skillName = "技能";
    [InspectorName("图标")]
    public Sprite icon;
    [InspectorName("耗蓝")]
    public float manaCost = 20f;
    [InspectorName("冷却时长")]
    public Cooldown cooldown = new Cooldown { duration = 6f };
    [InspectorName("效果链")]
    [Tooltip("效果链：按顺序执行，可多个叠加")]
    public SkillEffectBase[] effects;
}