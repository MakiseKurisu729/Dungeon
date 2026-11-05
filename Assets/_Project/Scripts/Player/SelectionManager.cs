using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 选择系统：左键=单选友军；Ctrl+左键=加选；Ctrl+A 全选；1~4 热键=切换选中；
/// 反引号清空；点敌人=选定并标记（红圈）；点击交互物=选中单位走过去执行交互；
/// 瞄准技能时左键让给施法。
/// </summary>
public class SelectionManager : MonoBehaviour
{
    public static SelectionManager instance;

    public LayerMask clickableLayers = ~0;

    readonly List<Selectable> _all = new List<Selectable>();
    readonly List<Selectable> _selected = new List<Selectable>();
    DebugCameraSwitcher _cam;

    public Selectable Primary
    {
        get { return _selected.Count > 0 ? _selected[_selected.Count - 1] : null; }
    }

    public bool MultiSelected { get { return _selected.Count > 1; } }

    /// <summary>相机系统实例：Awake 先后顺序不保证，取不到时惰性重试（行为与 FindObjectOfType 一致）。</summary>
    DebugCameraSwitcher Cam
    {
        get
        {
            if (_cam == null) _cam = DebugCameraSwitcher.instance;
            return _cam;
        }
    }

    void Awake()
    {
        instance = this;
        _cam = DebugCameraSwitcher.instance;
    }

    void Start()
    {
        // 单位注册表 = 唯一事实源（原为全场扫描 Selectable）；Selectable 与 UnitHealth 同物体/子物体
        foreach (var h in UnitRegistry.All())
        {
            var s = h.GetComponentInChildren<Selectable>();
            if (s != null) Register(s);
        }
    }

    public void Register(Selectable s)
    {
        if (!_all.Contains(s)) _all.Add(s);
    }

    public void ClearSelection()
    {
        _selected.Clear();
        RefreshRings();
    }

    public void ForEachSelectedBrain(System.Action<CharacterBrain> action)
    {
        foreach (var s in _selected)
        {
            var b = s.GetComponentInParent<CharacterBrain>();
            if (b != null) action(b);
        }
    }

    Vector2 _downPos;
    bool _moved;

    void Update()
    {
        if (AppFlow.IsPaused) return;

        for (int i = 0; i < 4; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                var target = _all.Find(s => s != null && s.hotkeyIndex == i + 1);
                if (target != null) ToggleSelect(target);
            }
        }
        if (Input.GetKeyDown(KeyCode.A) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
        {
            _selected.Clear();
            foreach (var s in _all)
                if (s != null && s.GetComponentInParent<UnitHealth>() != null
                    && s.GetComponentInParent<UnitHealth>().team == Team.Player)
                    _selected.Add(s);
            RefreshRings();
        }

        if (Input.GetMouseButtonDown(0)) { _downPos = Input.mousePosition; _moved = false; }
        if (Input.GetMouseButton(0) && ((Vector2)Input.mousePosition - _downPos).magnitude > 8f)
            _moved = true;
        if (Input.GetMouseButtonUp(0) && !_moved && !(SkillInputController.instance != null && SkillInputController.IsTargeting))
            HandleClick();

        if (_enemySelected != null && _enemySelected.dead)
            SelectEnemy(null);

        var cam = Cam;
        if (cam != null)
        {
            cam.allowPan = true;   // 左键拖动平移常开；点击/拖动由 8px 阈值区分
            if (Primary != null) cam.SetFocus(Primary.transform);
        }
    }

    void HandleClick()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        HandleClick(cam.ScreenPointToRay(Input.mousePosition));
    }

    /// <summary>点击判定主体（拆出 Ray 重载：真实输入与测试共用同一条判定链，行为不变）。</summary>
    void HandleClick(Ray ray)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, 300f, clickableLayers);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        Selectable sel = null;
        UnitHealth enemy = null;
        Interactable inter = null;
        foreach (var h in hits)
        {
            if (sel == null)
            {
                var s = h.collider.GetComponentInParent<Selectable>();
                if (s != null) sel = s;
            }
            if (enemy == null)
            {
                var uh = h.collider.GetComponentInParent<UnitHealth>();
                if (uh != null && uh.team == Team.Monster && !uh.dead) enemy = uh;
            }
            if (inter == null && h.collider.GetComponentInParent<Interactable>() != null)
                inter = h.collider.GetComponentInParent<Interactable>();
        }

        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        // ★ 救援优先：点倒地队友且当前有活着的玩家选中 → 下达救援指令，不把倒地者当作选中目标。
        // 必须排在 sel != null 之前：倒地者身上仍挂着 Selectable，否则永远只会被“选中”。
        if (!ctrl && TryCommandRescue(inter)) return;

        if (sel != null)
        {
            if (ctrl) ToggleSelect(sel);
            else SingleSelect(sel);
        }
        else if (enemy != null)
        {
            SelectEnemy(enemy);
            foreach (var s in _selected)
            {
                var brain = s.GetComponent<CharacterBrain>();
                if (brain != null) brain.CommandAttack(enemy.transform);
            }
        }
        else if (inter != null)
        {
            // ★ 点击交互物：选中单位走过去执行（走到范围后由目标自己的逻辑接管，如 DownedRescue 自动读条）
            foreach (var s in _selected)
            {
                var brain = s.GetComponent<CharacterBrain>();
                if (brain != null) brain.CommandInteract(inter);
            }
        }
        else
        {
            SingleSelect(null);
            SelectEnemy(null);
        }
    }

    /// <summary>点中的是倒地队友、且当前选中里有活着的玩家 → 对每个选中玩家下达救援指令。
    /// 返回 true = 已下达（调用方不再走选中/攻击分支）。</summary>
    bool TryCommandRescue(Interactable inter)
    {
        if (inter == null) return false;
        var target = inter.GetComponent<DownedRescue>();
        if (target == null) target = inter.GetComponentInParent<DownedRescue>();
        if (target == null || !target.IsDown) return false;

        int issued = 0;
        foreach (var s in _selected)
        {
            if (s == null) continue;
            var h = s.GetComponentInParent<UnitHealth>();
            if (h == null || h.dead || h.team != Team.Player) continue;   // 倒地者 dead=true，救不了自己
            target.CommandRescue(h);
            issued++;
        }
        return issued > 0;
    }

    void SingleSelect(Selectable s)
    {
        _selected.Clear();
        if (s != null) _selected.Add(s);
        RefreshRings();
    }

    void ToggleSelect(Selectable s)
    {
        if (_selected.Contains(s)) _selected.Remove(s);
        else _selected.Add(s);
        RefreshRings();
    }

    void RefreshRings()
    {
        foreach (var s in _all) if (s != null) s.SetSelected(_selected.Contains(s));
    }

    // ===== 选定的敌人（红圈 = 预制体下的 EnemyRing 子物体）=====
    UnitHealth _enemySelected;
    GameObject _enemyRing;

    void SelectEnemy(UnitHealth uh)
    {
        if (_enemyRing != null) { _enemyRing.SetActive(false); _enemyRing = null; }
        _enemySelected = uh;
        if (uh == null) return;

        var ring = uh.transform.Find("EnemyRing");
        if (ring != null)
        {
            ring.gameObject.SetActive(true);
            _enemyRing = ring.gameObject;
        }
    }
}