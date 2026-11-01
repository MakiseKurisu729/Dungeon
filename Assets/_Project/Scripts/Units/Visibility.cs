using UnityEngine;

/// <summary>视野逻辑层：敌人超出玩家视野半径或被障碍物挡视线 → 隐藏模型+头顶条 + 冻结 AI。
/// 同时控制 Renderer（模型）和 Canvas（头顶条）。遮挡层未配置时自动指向 Obstacle。</summary>
public class Visibility : MonoBehaviour
{
    [Tooltip("玩家视野半径：超出即隐身")]
    public float viewRadius = 10f;
    [Tooltip("遮挡视线的层（石头/地牢墙）")]
    public LayerMask obstacleMask;

    Renderer[] _renderers;
    Canvas[] _canvases;   // ★ 头顶条也是 Canvas
    UnitBrain _brain;

    /// <summary>本帧是否处于隐身（模型+血条已关）。WorldHealthBar 读取它，避免把血条重新点亮。</summary>
    public bool IsHidden { get; private set; }

    void Start()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _canvases = GetComponentsInChildren<Canvas>();
        _brain = GetComponent<UnitBrain>();
        if (obstacleMask.value == 0)
            obstacleMask = LayerMask.GetMask("Obstacle");
    }

    void Update()
    {
        bool visible = IsVisibleToAnyPlayer();
        IsHidden = !visible;
        foreach (var r in _renderers)
            if (r != null && r.enabled != visible) r.enabled = visible;
        foreach (var c in _canvases)                    // ★ 血条跟着一起藏
            if (c != null && c.enabled != visible) c.enabled = visible;
        if (_brain != null && _brain.enabled != visible) _brain.enabled = visible;
    }

    bool IsVisibleToAnyPlayer()
    {
        foreach (var h in FindObjectsByType<UnitHealth>(FindObjectsSortMode.None))
        {
            if (h.dead || h.team != Team.Player) continue;
            if (Vector3.Distance(transform.position, h.transform.position) > viewRadius) continue;

            Vector3 eye = transform.position + Vector3.up * 1.2f;
            Vector3 target = h.transform.position + Vector3.up * 0.9f;
            Vector3 dir = target - eye;
            if (Physics.Raycast(eye, dir.normalized, dir.magnitude, obstacleMask))
                continue;
            return true;
        }
        return false;
    }
}