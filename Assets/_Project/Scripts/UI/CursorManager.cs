using System.Collections.Generic;
using UnityEngine;

/// <summary>上下文光标：默认/选择/攻击/交互 四态。白色贴图自动染色，热点可调。
/// 交互光标（interactCursor）检查器自拖；悬停 Interactable 时换用并显示其 hint。</summary>
public class CursorManager : MonoBehaviour
{
    public static CursorManager instance;

    public enum Hover { None, Pointer, Select, Attack, Interact }

    [Header("光标贴图（白色/灰色系，程序染色）")]
    public Texture2D defaultCursor;
    public Texture2D pointerCursor;
    public Texture2D selectCursor;
    public Texture2D attackCursor;
    [Tooltip("交互光标：悬停可交互物（倒地队友/宝箱/门）时显示，自拖图标")]
    public Texture2D interactCursor;

    [Header("各光标热点（图标内点击点的像素坐标，原点=图标左上角）")]
    public Vector2 defaultHotspot = Vector2.zero;
    public Vector2 pointerHotspot = Vector2.zero;
    public Vector2 selectHotspot = Vector2.zero;
    public Vector2 attackHotspot = Vector2.zero;
    public Vector2 interactHotspot = Vector2.zero;

    [Header("状态颜色")]
    public Color selectColor = new Color(0.3f, 1f, 0.5f);
    public Color attackColor = new Color(1f, 0.25f, 0.25f);
    public Color interactColor = new Color(1f, 0.85f, 0.2f);

    [Header("点选射线（Layer）")]
    public LayerMask clickableLayers = ~0;

    public bool AlwaysPointer { get; set; }
    public bool ForceDefault { get; set; }

    Dictionary<Texture2D, Dictionary<Color, Texture2D>> _tintCache;
    Hover _hover = Hover.None;

    void Awake()
    {
        instance = this;
        _tintCache = new Dictionary<Texture2D, Dictionary<Color, Texture2D>>();
    }

    void Update()
    {
        if (UIManager.instance != null && UIManager.instance.IsInventoryOpen)
        { SetHover(Hover.Pointer); Apply(pointerCursor, pointerHotspot, Color.white); return; }

        // 瞄准态不再强制普通指针：与常态共用同一套 hover 分类，
        // 玩家才能从指针判断“这一下能不能打到谁”。
        Camera cam = Camera.main;
        if (cam == null) return;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        UnitHealth enemy;
        Interactable inter;
        Selectable selectable;
        float distance;
        Hover hover = ResolveHover(ray, out enemy, out inter, out selectable, out distance);
        SetHover(hover);

        if (hover == Hover.Interact)
        {
            if (interactCursor != null)
            {
                Apply(interactCursor, interactHotspot, interactColor);
                if (!string.IsNullOrEmpty(inter.hint)) GameData.SetMessage(inter.hint);
            }
            else
            {
                Apply(pointerCursor, pointerHotspot, Color.white);
            }
        }
        else if (hover == Hover.Select)
        {
            Apply(selectCursor, selectHotspot, selectColor);
        }
        else if (hover == Hover.Attack)
        {
            Apply(attackCursor, attackHotspot, attackColor);
        }
        else
        {
            Apply(pointerCursor, pointerHotspot, Color.white);
        }
    }

    /// <summary>射线 → hover 分类（纯查询，不写光标）。瞄准态与常态共用，batchmode 可用合成射线断言。</summary>
    public Hover ResolveHover(Ray ray)
    {
        UnitHealth enemy;
        float distance;
        return ResolveHover(ray, out enemy, out distance);
    }

    /// <summary>射线 → hover 分类，并输出命中的存活敌人与其射线距离（无敌人时 distance = -1）。</summary>
    public Hover ResolveHover(Ray ray, out UnitHealth enemy, out float distance)
    {
        Interactable inter;
        Selectable selectable;
        return ResolveHover(ray, out enemy, out inter, out selectable, out distance);
    }

    Hover ResolveHover(Ray ray, out UnitHealth enemy, out Interactable inter, out Selectable selectable,
                       out float distance)
    {
        enemy = null; inter = null; selectable = null; distance = -1f;
        RaycastHit[] hits = Physics.RaycastAll(ray, 300f, clickableLayers);
        foreach (var h in hits)
        {
            if (selectable == null && h.collider.GetComponentInParent<Selectable>() != null)
                selectable = h.collider.GetComponentInParent<Selectable>();
            if (enemy == null)
            {
                var uh = h.collider.GetComponentInParent<UnitHealth>();
                if (uh != null && uh.team == Team.Monster && !uh.dead)
                {
                    enemy = uh;
                    distance = h.distance;
                }
            }
            if (inter == null && h.collider.GetComponentInParent<Interactable>() != null)
                inter = h.collider.GetComponentInParent<Interactable>();
        }

        if (inter != null && selectable == null) return Hover.Interact;
        if (selectable != null) return Hover.Select;
        if (enemy != null) return Hover.Attack;
        return Hover.Pointer;
    }

    void SetHover(Hover h) { _hover = h; }

    void Apply(Texture2D tex, Vector2 hotspot, Color tint)
    {
        if (tex == null) return;
        bool needTint = tint != Color.white && IsGrayscaleish(tex);
        Texture2D finalTex = needTint ? GetTinted(tex, tint) : tex;
        Cursor.SetCursor(finalTex, hotspot, CursorMode.Auto);
    }

    bool IsGrayscaleish(Texture2D tex)
    {
        Color c = tex.GetPixel(tex.width / 2, tex.height / 2);
        return Mathf.Abs(c.r - c.g) < 0.05f && Mathf.Abs(c.g - c.b) < 0.05f;
    }

    Texture2D GetTinted(Texture2D src, Color tint)
    {
        if (!_tintCache.TryGetValue(src, out var inner))
        { inner = new Dictionary<Color, Texture2D>(); _tintCache[src] = inner; }
        if (inner.TryGetValue(tint, out var cached) && cached != null) return cached;

        var copy = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
        var px = src.GetPixels();
        for (int i = 0; i < px.Length; i++)
            px[i] = new Color(px[i].r * tint.r, px[i].g * tint.g, px[i].b * tint.b, px[i].a);
        copy.SetPixels(px);
        copy.Apply();
        inner[tint] = copy;
        return copy;
    }
}