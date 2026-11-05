using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能输入（LoL 式施法）：QWER → 需目标技能进入瞄准模式（范围圈+指向线）。
/// 左键点选：目标在范围内 → 直接释放；目标在范围外 → 自动走向目标，
/// 进入范围自动释放（追放）。右键/Esc 取消；目标死亡、主选变更自动取消。
/// quickCast=快捷施法直接对最近敌人放。敌人列表节流缓存（0.25s）。
/// </summary>
public class SkillInputController : MonoBehaviour
{
    public static SkillInputController instance;
    public static bool IsTargeting { get; private set; }

    /// <summary>待放技能射程（瞄准/追放期间有效，取消后归零）；指针与测试读同一份数据。</summary>
    public float PendingRange { get; private set; }

    [InspectorName("快捷施法")]
    [Tooltip("快捷施法：跳过目标指示，直接对最近敌人释放")]
    public bool quickCast = false;

    [InspectorName("技能指示器")]
    public SkillIndicator indicator;
    [InspectorName("技能按键")]
    public KeyCode[] skillKeys = { KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R };

    [InspectorName("敌人缓存刷新间隔")]
    [Tooltip("敌人列表缓存刷新间隔（秒）")]
    public float enemyCacheInterval = 0.25f;

    int _pendingIndex = -1;
    SkillSystem _pendingCaster;      // 追放中的施法者（主选变更则取消）
    Transform _pendingTarget;        // 追放目标
    bool _chase;                     // 追放中
    readonly List<UnitHealth> _enemyCache = new List<UnitHealth>();
    float _cacheTimer;

    void Awake() { instance = this; }

    void Update()
    {
        _cacheTimer -= Time.deltaTime;
        if (AppFlow.IsPaused) return;
        if (UIManager.instance != null && UIManager.instance.IsInventoryOpen) return;

        var ss = PrimarySkillSystem();

        // ★ 追放：走向目标，进入射程自动释放
        if (_chase)
        {
            if (_pendingCaster == null || ss != _pendingCaster) { Cancel(); return; }
            var sk = _pendingCaster.GetSkill(_pendingIndex);
            if (sk == null) { Cancel(); return; }

            var puh = _pendingTarget != null ? _pendingTarget.GetComponent<UnitHealth>() : null;
            if (puh == null || puh.dead) { Cancel(); return; }

            float range = GetSkillRange(sk);
            PendingRange = range;
            Vector3 d = _pendingTarget.position - _pendingCaster.transform.position; d.y = 0f;

            if (d.magnitude <= range)
            {
                CastWithFeedback(_pendingCaster, _pendingIndex, _pendingTarget);   // 到位：自动释放
                Cancel();
                return;
            }

            // 追击：持续更新目的地；指示器保持显示，玩家能看到"还差多远"
            var brain = _pendingCaster.GetComponent<CharacterBrain>();
            if (brain != null) brain.CommandMove(_pendingTarget.position);
            if (indicator != null) indicator.UpdateVisual(_pendingCaster.transform.position, range);

            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                if (brain != null) brain.CommandMove(_pendingCaster.transform.position);  // 停下
                Cancel();
            }
            return;
        }

        if (IsTargeting)
        {
            if (ss == null || ss.GetSkill(_pendingIndex) == null) { Cancel(); return; }

            float range = GetSkillRange(ss.GetSkill(_pendingIndex));
            PendingRange = range;
            if (indicator != null) indicator.UpdateVisual(ss.transform.position, range);

            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                Cancel();
            }
            else if (Input.GetMouseButtonDown(0))
            {
                var targetHealth = PickEnemyAt(Input.mousePosition);
                if (targetHealth == null)
                {
                    MessageFeed.SetMessage("未选中敌人");   // 点空地=取消，但要说明原因
                    Cancel();
                    return;
                }
                Vector3 targetPos = targetHealth.transform.position;
                Vector3 d = targetPos - ss.transform.position; d.y = 0f;
                if (d.magnitude <= range)
                {
                    CastWithFeedback(ss, _pendingIndex, targetHealth.transform);   // 范围内：直接放
                    Cancel();
                }
                else
                {
                    // ★ 范围外：走向目标，进入范围自动放
                    MessageFeed.SetMessage("目标超出射程，正在接近");
                    _chase = true;
                    _pendingCaster = ss;
                    _pendingTarget = targetHealth.transform;
                    var brain = ss.GetComponent<CharacterBrain>();
                    if (brain != null) brain.CommandMove(targetPos);
                }
            }
            return;
        }

        if (ss == null) return;

        for (int i = 0; i < skillKeys.Length && i < 4; i++)
        {
            if (!Input.GetKeyDown(skillKeys[i])) continue;
            var s = ss.GetSkill(i);
            if (s == null || s.effects == null || s.effects.Length == 0) continue;

            if (quickCast)
                CastWithFeedback(ss, i, NearestEnemy(ss.transform, GetSkillRange(s)));
            else
            {
                _pendingIndex = i;
                IsTargeting = true;
                if (indicator != null) indicator.Show();
            }
        }
    }

    // ★ 范围取第一个效果的 range（效果资产上）
    static float GetSkillRange(SkillData s)
    {
        if (s.effects != null && s.effects.Length > 0 && s.effects[0] != null)
            return s.effects[0].range;
        return 3f;
    }

    void Cancel()
    {
        IsTargeting = false;
        _chase = false;
        _pendingIndex = -1;
        _pendingCaster = null;
        _pendingTarget = null;
        PendingRange = 0f;
        if (indicator != null) indicator.Hide();
    }

    SkillSystem PrimarySkillSystem()
    {
        if (SelectionManager.instance == null || SelectionManager.instance.Primary == null) return null;
        return SelectionManager.instance.Primary.GetComponentInParent<SkillSystem>();
    }

    /// <summary>施放并补一条失败提示：Cast 自身已写消息（冷却/蓝量）时保留，未写时补“技能释放失败”。</summary>
    void CastWithFeedback(SkillSystem caster, int index, Transform target)
    {
        string before = MessageFeed.Message;
        if (caster.Cast(index, target)) return;
        if (MessageFeed.Message == before) MessageFeed.SetMessage("技能释放失败");
    }

    /// <summary>屏幕坐标（像素）→ 最近存活敌人（Monster）；点击选敌的唯一入口。</summary>
    public UnitHealth PickEnemyAt(Vector3 screenPosition)
    {
        Camera cam = Camera.main;
        if (cam == null) return null;
        return PickEnemyFromRay(cam.ScreenPointToRay(screenPosition));
    }

    /// <summary>射线 → 最近存活敌人（Monster）。指针分类与点击选敌共用同一射线口径。</summary>
    public UnitHealth PickEnemyFromRay(Ray ray)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, 300f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            var uh = h.collider.GetComponentInParent<UnitHealth>();
            if (uh != null && uh.team == Team.Monster && !uh.dead)
                return uh;
        }
        return null;
    }

    Transform NearestEnemy(Transform from, float range)
    {
        RefreshEnemyCache();
        UnitHealth best = null;
        float bd = range;
        for (int i = _enemyCache.Count - 1; i >= 0; i--)
        {
            var h = _enemyCache[i];
            if (h == null || h.dead) { _enemyCache.RemoveAt(i); continue; }
            float d = Vector3.Distance(from.position, h.transform.position);
            if (d < bd) { bd = d; best = h; }
        }
        return best != null ? best.transform : null;
    }

    void RefreshEnemyCache()
    {
        if (_cacheTimer > 0f) return;
        _cacheTimer = enemyCacheInterval;
        _enemyCache.Clear();
        foreach (var h in UnitRegistry.OfTeam(Team.Monster))
            if (!h.dead)
                _enemyCache.Add(h);
    }
}