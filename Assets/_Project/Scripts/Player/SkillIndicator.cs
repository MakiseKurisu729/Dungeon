using UnityEngine;

/// <summary>技能指示器：脚下范围圈 + 中心到鼠标点的指向线。</summary>
public class SkillIndicator : MonoBehaviour
{
    public Material mat;   // RingMat 即可

    LineRenderer _ring;
    LineRenderer _aim;

    void Awake()
    {
        _ring = MakeLine(0.05f, true);
        _aim = MakeLine(0.03f, false);
        _aim.positionCount = 2;
        Hide();
    }

    LineRenderer MakeLine(float w, bool loop)
    {
        var lr = new GameObject(loop ? "ring" : "aim").AddComponent<LineRenderer>();
        lr.transform.SetParent(transform, false);
        lr.material = mat;
        lr.useWorldSpace = true;
        lr.loop = loop;
        lr.positionCount = loop ? 33 : 2;
        lr.startWidth = w;
        lr.endWidth = w;
        return lr;
    }

    public void Show()
    {
        _ring.enabled = true;
        _aim.enabled = true;
    }

    public void Hide()
    {
        _ring.enabled = false;
        _aim.enabled = false;
    }

    public void UpdateVisual(Vector3 center, float range)
    {
        for (int i = 0; i <= 32; i++)
        {
            float a = i / 32f * Mathf.PI * 2f;
            _ring.SetPosition(i, center + new Vector3(Mathf.Cos(a) * range, 0.05f, Mathf.Sin(a) * range));
        }

        Vector3 p = CursorGroundPoint(center);
        Vector3 d = p - center; d.y = 0f;
        if (d.magnitude > range) p = center + d.normalized * range;
        p.y = 0.05f;
        _aim.SetPosition(0, center + Vector3.up * 0.05f);
        _aim.SetPosition(1, p);
    }

    Vector3 CursorGroundPoint(Vector3 fallback)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Mathf.Abs(ray.direction.y) > 0.001f)
        {
            float t = -ray.origin.y / ray.direction.y;
            if (t > 0f) return ray.GetPoint(t);
        }
        return fallback;
    }
}