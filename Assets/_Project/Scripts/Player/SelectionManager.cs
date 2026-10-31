using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 选择系统：左键=单选友军；Ctrl+左键=加选；Ctrl+A 全选；1~4 热键=切换选中；
/// 反引号清空；点敌人=选定：点亮其预制体下的 EnemyRing 子物体（LoL 式红圈）；
/// 换目标/点空地/目标死亡 → 圈切换或熄灭。瞄准技能时左键让给施法。
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

    void Awake()
    {
        instance = this;
        _cam = FindObjectOfType<DebugCameraSwitcher>();
    }

    void Start()
    {
        // ★ 兜底注册：OnEnable 时序不保证，开场统一收集一次
        foreach (var s in FindObjectsByType<Selectable>(FindObjectsSortMode.None))
            Register(s);
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
        if (PauseManager.instance != null && PauseManager.instance.IsPaused) return;

        // 1~4 快速选人：toggle——按1选骑士，按2加选弓手，再按取消
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

        // ★ 选定的敌人死亡 → 圈熄灭
        if (_enemySelected != null && _enemySelected.dead)
            SelectEnemy(null);

        if (_cam != null)
        {
            _cam.allowPan = _selected.Count == 0;
            if (Primary != null) _cam.SetFocus(Primary.transform);
        }
    }

    void HandleClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
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

        if (sel != null)
        {
            if (ctrl) ToggleSelect(sel);
            else SingleSelect(sel);
        }
        else if (enemy != null)
        {
            SelectEnemy(enemy);                      // ★ 点亮新目标的圈（旧的自动灭）
            foreach (var s in _selected)
            {
                var brain = s.GetComponent<CharacterBrain>();
                if (brain != null) brain.CommandAttack(enemy.transform);
            }
        }
        else if (inter != null)
        {
            foreach (var s in _selected)
            {
                var brain = s.GetComponent<CharacterBrain>();
                if (brain != null) brain.CommandInteract(inter);
            }
        }
        else
        {
            SingleSelect(null);
            SelectEnemy(null);                       // ★ 点空地：圈熄灭
        }
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

    // ===== 选定的敌人（LoL 式红圈 = 目标预制体下的 EnemyRing 子物体）=====
    UnitHealth _enemySelected;
    GameObject _enemyRing;   // 当前亮着的圈

    void SelectEnemy(UnitHealth uh)
    {
        // 旧圈先灭
        if (_enemyRing != null) { _enemyRing.SetActive(false); _enemyRing = null; }
        _enemySelected = uh;
        if (uh == null) return;

        // 新圈点亮：约定子物体名 EnemyRing（预制体里做好，默认隐藏）
        var ring = uh.transform.Find("EnemyRing");
        if (ring != null)
        {
            ring.gameObject.SetActive(true);
            _enemyRing = ring.gameObject;
        }
    }
}