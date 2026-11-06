using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Dungeon.Core;
namespace Dungeon.Gameplay.Units {

    /// <summary>
    /// 头顶血蓝条（LoL 式）：世界空间 Canvas，面向相机；
    /// 玩家=绿血+蓝条常驻；敌人=红血，仅"可见"时显示（当前逻辑：12 米内有存活玩家单位，迷雾接入后换判定）。
    /// </summary>
    public class WorldHealthBar : MonoBehaviour
    {
        [InspectorName("生命组件")]
        public UnitHealth health;
        [InspectorName("蓝量组件")]
        public UnitMana mana;
        [InspectorName("画布")]
        public Canvas canvas;
        [InspectorName("血条填充")]
        public Image hpFill;
        [InspectorName("蓝条填充")]
        public Image manaFill;

        [Header("可见性")]
        [InspectorName("可见距离")]
        public float visibleRange = 12f;
        [InspectorName("玩家颜色")]
        public Color playerColor = new Color(0.2f, 0.9f, 0.3f);
        [InspectorName("敌人颜色")]
        public Color enemyColor = new Color(0.9f, 0.2f, 0.2f);

        static IReadOnlyList<UnitHealth> _allUnits;
        static float _cacheTime;
        Visibility _visibility;   // 有 Visibility 时以它的隐身状态为准（防止本类把血条重新点亮）

        void LateUpdate()
        {
            if (canvas == null || health == null) return;
            if (Camera.main != null) transform.rotation = Camera.main.transform.rotation;

            bool visible = !health.dead && (health.team == Team.Player || IsVisibleToPlayers());
            if (_visibility == null) _visibility = health.GetComponent<Visibility>();
            if (_visibility != null && _visibility.IsHidden) visible = false;   // 隐身单位：血条跟模型一起灭
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
                _allUnits = UnitRegistry.All();
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
}