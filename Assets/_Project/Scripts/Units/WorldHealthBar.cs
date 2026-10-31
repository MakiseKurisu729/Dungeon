using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 头顶血蓝条（LoL 式）：世界空间 Canvas，面向相机；
/// 玩家=绿血+蓝条常驻；敌人=红血，仅"可见"时显示（当前逻辑：12 米内有存活玩家单位，迷雾接入后换判定）。
/// </summary>
public class WorldHealthBar : MonoBehaviour
{
    public UnitHealth health;
    public UnitMana mana;
    public Canvas canvas;
    public Image hpFill;
    public Image manaFill;

    [Header("可见性")]
    public float visibleRange = 12f;
    public Color playerColor = new Color(0.2f, 0.9f, 0.3f);
    public Color enemyColor = new Color(0.9f, 0.2f, 0.2f);

    static UnitHealth[] _allUnits;
    static float _cacheTime;

    void LateUpdate()
    {
        if (canvas == null || health == null) return;
        if (Camera.main != null) transform.rotation = Camera.main.transform.rotation;

        bool visible = !health.dead && (health.team == Team.Player || IsVisibleToPlayers());
        canvas.enabled = visible;
        if (!visible) return;

        hpFill.color = health.team == Team.Player ? playerColor : enemyColor;
        hpFill.rectTransform.localScale = new Vector3(Mathf.Clamp01(health.hp / health.maxHp), 1f, 1f);
        if (manaFill != null && mana != null)
            manaFill.rectTransform.localScale = new Vector3(Mathf.Clamp01(mana.current / mana.max), 1f, 1f);
    }

    bool IsVisibleToPlayers()
    {
        if (Time.time - _cacheTime > 0.25f)
        {
            _cacheTime = Time.time;
            _allUnits = FindObjectsByType<UnitHealth>(FindObjectsSortMode.None);
        }
        if (_allUnits == null) return false;
        foreach (var u in _allUnits)
        {
            if (u == null || u.dead || u.team != Team.Player) continue;
            if (Vector3.Distance(u.transform.position, transform.position) < visibleRange)
                return true;
        }
        return false;
    }
}