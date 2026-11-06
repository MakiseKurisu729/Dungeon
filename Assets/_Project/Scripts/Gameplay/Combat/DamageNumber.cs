using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Dungeon.Gameplay.Combat {

    /// <summary>
    /// 伤害飘字（恶意不息风格）：单个共享 Screen Space-Overlay 画布 + 对象池（默认 32）。
    /// 出生瞬间取世界点（敌人胸口 +1.2m）投影到屏幕，之后只做自身运动、不再跟随。
    /// 尺寸随伤害分档：字号 = BaseFontSize × Clamp(Lerp(1.5, 5, (amount-5)/(60-5)), 1.5, 5)；
    /// 弹出 0.12s 内 0.6→1.15→缓慢缩至 0.8，全程上飘 + 淡出，生命周期 0.9s。
    /// 纯脚本、零资产：颜色用静态字典（可 SetElementColor 覆盖），不建 SO。
    /// </summary>
    public class DamageNumber : MonoBehaviour
    {
        // ---- 共享画布 ----
        public const string OverlayName = "DamageNumberOverlay";

        // ---- 对象池 ----
        public const int DefaultPoolCapacity = 32;
        public static int PoolCapacity = DefaultPoolCapacity;

        // ---- 尺寸-伤害挂钩 ----
        public const float BaselineScale = 0.006f;   // 旧世界空间基线（仅作倍率语义参考；Overlay 下用字号实现）
        public const float BaseFontSize = 16f;
        public const float MinScaleMultiplier = 1.5f;
        public const float MaxScaleMultiplier = 5f;
        public const float MinDamage = 5f;
        public const float MaxDamage = 60f;

        // ---- 运动/曲线 ----
        public const float Lifetime = 0.9f;
        public const float PopDuration = 0.12f;
        public const float PopStartScale = 0.6f;
        public const float PopPeakScale = 1.15f;
        public const float PopEndScale = 0.8f;
        public const float FadeDuration = 0.35f;
        public const float ChestHeight = 1.2f;
        public const float MinAngle = 60f;
        public const float MaxAngle = 120f;
        public const float MinInitialOffset = 40f;
        public const float MaxInitialOffset = 70f;
        public const float DriftSpeed = 28f;

        static readonly Dictionary<DamageElement, Color> ElementColors = new Dictionary<DamageElement, Color>
        {
            { DamageElement.Physical, new Color(1f, 0.92f, 0.35f) },
            { DamageElement.Fire, new Color(1f, 0.45f, 0.15f) },
            { DamageElement.Ice, new Color(0.45f, 0.85f, 1f) },
            { DamageElement.Lightning, new Color(0.75f, 0.7f, 1f) },
            { DamageElement.Poison, new Color(0.55f, 0.95f, 0.35f) },
            { DamageElement.Holy, new Color(1f, 0.97f, 0.8f) },
            { DamageElement.Shadow, new Color(0.7f, 0.45f, 0.95f) }
        };

        static Canvas _sharedCanvas;
        static readonly List<DamageNumber> _pool = new List<DamageNumber>();    // 未激活
        static readonly List<DamageNumber> _active = new List<DamageNumber>();  // 活跃

        Text _text;
        CanvasGroup _group;
        float _age;
        float _appliedMultiplier = 1f;
        float _appliedFontSize = BaseFontSize;
        DamageElement _element = DamageElement.Physical;
        Vector2 _start;
        Vector2 _dir = Vector2.up;
        float _initialOffset;

        // ---- 测试/调试只读面 ----
        public static Canvas SharedCanvas { get { return _sharedCanvas; } }
        public static int ActiveCount { get { return _active.Count; } }
        public static int PooledCount { get { return _pool.Count; } }
        public float AppliedMultiplier { get { return _appliedMultiplier; } }
        public float AppliedFontSize { get { return _appliedFontSize; } }
        public DamageElement Element { get { return _element; } }
        public float Age { get { return _age; } }

        /// <summary>入口：世界坐标（敌人本体位置）+ 伤害数值 + 伤害元素。</summary>
        public static void Show(Vector3 worldPos, float amount, DamageElement element)
        {
            Canvas canvas = EnsureCanvas();
            if (canvas == null) return;
            DamageNumber dn = Rent();
            if (dn == null) return;
            dn.Play(worldPos, amount, element);
        }

        public static float ComputeMultiplier(float amount)
        {
            float t = Mathf.InverseLerp(MinDamage, MaxDamage, amount);
            return Mathf.Clamp(Mathf.Lerp(MinScaleMultiplier, MaxScaleMultiplier, t),
                               MinScaleMultiplier, MaxScaleMultiplier);
        }

        public static Color ColorFor(DamageElement element)
        {
            Color c;
            return ElementColors.TryGetValue(element, out c) ? c : Color.white;
        }

        /// <summary>覆盖某元素配色（替代 SO 资产；默认表见 ElementColors）。</summary>
        public static void SetElementColor(DamageElement element, Color color)
        {
            ElementColors[element] = color;
        }

        // ============================================================
        // 画布 / 池
        // ============================================================
        static Canvas EnsureCanvas()
        {
            if (_sharedCanvas != null) return _sharedCanvas;

            // 域重载关闭等场景下复用已存在的画布，避免出现第二个 Overlay 画布
            var existing = GameObject.Find(OverlayName);
            if (existing != null)
            {
                var c = existing.GetComponent<Canvas>();
                if (c != null)
                {
                    _sharedCanvas = c;
                    if (_pool.Count == 0 && _active.Count == 0)
                    {
                        foreach (var dn in c.GetComponentsInChildren<DamageNumber>(true))
                        {
                            if (dn == null) continue;
                            dn.gameObject.SetActive(false);
                            _pool.Add(dn);
                        }
                    }
                    return _sharedCanvas;
                }
            }

            var go = new GameObject(OverlayName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            Object.DontDestroyOnLoad(go);

            _sharedCanvas = canvas;
            return _sharedCanvas;
        }

        static DamageNumber Rent()
        {
            for (int i = _pool.Count - 1; i >= 0; i--)
            {
                var d = _pool[i];
                _pool.RemoveAt(i);
                if (d == null) continue;
                _active.Add(d);
                return d;
            }

            if (_active.Count >= PoolCapacity && _active.Count > 0)
            {
                // 池已满且无空闲：抢占最旧活跃实例，保证同时存在实例数 ≤ 容量
                var oldest = _active[0];
                _active.RemoveAt(0);
                if (oldest != null)
                {
                    _active.Add(oldest);
                    return oldest;
                }
            }

            var created = CreateInstance(EnsureCanvas());
            if (created != null) _active.Add(created);
            return created;
        }

        static DamageNumber CreateInstance(Canvas canvas)
        {
            if (canvas == null) return null;

            var go = new GameObject("DamageNumber", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(canvas.transform, false);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(240f, 80f);
            rt.localScale = Vector3.one;

            var textGo = new GameObject("txt", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            var tRT = text.rectTransform;
            tRT.anchorMin = Vector2.zero;
            tRT.anchorMax = Vector2.one;
            tRT.offsetMin = Vector2.zero;
            tRT.offsetMax = Vector2.zero;

            var group = go.GetComponent<CanvasGroup>();
            group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = false;

            var dn = go.AddComponent<DamageNumber>();
            dn._text = text;
            dn._group = group;
            go.SetActive(false);
            return dn;
        }

        void Play(Vector3 worldPos, float amount, DamageElement element)
        {
            gameObject.SetActive(true);
            _age = 0f;
            _element = element;
            _appliedMultiplier = ComputeMultiplier(amount);
            _appliedFontSize = BaseFontSize * _appliedMultiplier;

            if (_text != null)
            {
                _text.text = Mathf.RoundToInt(amount).ToString();
                _text.fontSize = Mathf.RoundToInt(_appliedFontSize);
                _text.color = ColorFor(element);
            }
            if (_group != null) _group.alpha = 1f;

            Canvas canvas = _sharedCanvas != null ? _sharedCanvas : EnsureCanvas();
            Vector2 screen = ScreenCenter();
            Camera cam = ResolveCamera();
            if (cam != null)
            {
                Vector3 sp = cam.WorldToScreenPoint(worldPos + Vector3.up * ChestHeight);
                screen = new Vector2(sp.x, sp.y);
            }

            Vector2 local;
            if (canvas != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)canvas.transform, screen, null, out local))
            {
                _start = local;
            }
            else
            {
                _start = Vector2.zero;
            }

            float angle = Random.Range(MinAngle, MaxAngle) * Mathf.Deg2Rad;
            _dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            _initialOffset = Random.Range(MinInitialOffset, MaxInitialOffset);

            var rt = (RectTransform)transform;
            // 与 ScreenPointToLocalPointInRectangle 的画布中心原点对齐：实例锚点必须是中心 (0.5,0.5)，
            // 沿用创建时的左下锚点 (0,0) 会让全部飘字系统性左下偏移半屏。
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = _start;
            rt.localScale = Vector3.one * PopStartScale;
        }

        static Vector2 ScreenCenter()
        {
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }

        static Camera ResolveCamera()
        {
            Camera cam = Camera.main;
            if (cam != null) return cam;
            var cams = Camera.allCameras;
            return cams.Length > 0 ? cams[0] : null;
        }

        void Update()
        {
            _age += Time.deltaTime;

            float jump = Mathf.Clamp01(_age / PopDuration);
            float ease = 1f - (1f - jump) * (1f - jump);
            float drift = Mathf.Max(0f, _age - PopDuration) * DriftSpeed;

            var rt = (RectTransform)transform;
            rt.anchoredPosition = _start + _dir * (_initialOffset * ease) + Vector2.up * drift;
            rt.localScale = Vector3.one * PopScale(_age);

            if (_group != null)
                _group.alpha = Mathf.Clamp01((Lifetime - _age) / FadeDuration);

            if (_age >= Lifetime) ReturnToPool();
        }

        static float PopScale(float age)
        {
            if (age < PopDuration)
                return Mathf.Lerp(PopStartScale, PopPeakScale, age / PopDuration);
            return Mathf.Lerp(PopPeakScale, PopEndScale, (age - PopDuration) / (Lifetime - PopDuration));
        }

        void ReturnToPool()
        {
            gameObject.SetActive(false);
            if (_active.Remove(this)) _pool.Add(this);
        }
    }
}
