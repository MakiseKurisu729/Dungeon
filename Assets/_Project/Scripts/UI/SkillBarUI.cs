using UnityEngine;
using UnityEngine.UI;

/// <summary>技能栏：跟随主选角色；图标/冷却遮罩(纵向缩放)/耗蓝变灰/按键标签。</summary>
public class SkillBarUI : MonoBehaviour
{
    [InspectorName("槽位图标")]
    public Image[] slotIcons = new Image[4];
    [InspectorName("冷却遮罩")]
    public Image[] cdMasks = new Image[4];     // 黑遮罩，轴心 Y=0，缩放 Y=剩余冷却比例
    [InspectorName("按键标签")]
    public Text[] keyLabels = new Text[4];
    [InspectorName("耗蓝标签")]
    public Text[] costLabels = new Text[4];
    [InspectorName("蓝量充足颜色")]
    public Color enoughMana = Color.white;
    [InspectorName("蓝量不足颜色")]
    public Color notEnoughMana = new Color(1f, 1f, 1f, 0.35f);

    void Update()
    {
        var ss = PrimarySkillSystem();
        for (int i = 0; i < 4; i++)
        {
            var s = ss != null ? ss.GetSkill(i) : null;

            if (slotIcons[i] != null)
            {
                slotIcons[i].enabled = s != null && s.icon != null;
                if (slotIcons[i].enabled) slotIcons[i].sprite = s.icon;
            }
            if (keyLabels[i] != null) keyLabels[i].text = "QWER"[i].ToString();
            if (costLabels[i] != null)
                costLabels[i].text = s != null ? ((int)s.manaCost).ToString() : "";

            if (cdMasks[i] != null)
            {
                if (s == null) { cdMasks[i].enabled = false; continue; }
                cdMasks[i].enabled = true;
                float r = ss.CdRemaining(i) / ss.CdTotal(i);
                cdMasks[i].rectTransform.localScale = new Vector3(1f, Mathf.Clamp01(r), 1f);

                bool manaOk = ss.mana == null || ss.mana.current >= s.manaCost;
                cdMasks[i].color = manaOk
                    ? new Color(0f, 0f, 0f, 0.6f)
                    : new Color(0f, 0f, 0.3f, 0.6f);
                if (slotIcons[i] != null) slotIcons[i].color = manaOk ? enoughMana : notEnoughMana;
            }
        }
    }

    static SkillSystem PrimarySkillSystem()
    {
        if (SelectionManager.instance == null || SelectionManager.instance.Primary == null) return null;
        return SelectionManager.instance.Primary.GetComponentInParent<SkillSystem>();
    }
}