using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 相机系统（RTS 化）：俯视=自由锚点相机
/// Z/X 旋转、左键拖动平移（allowPan 开关，后续由选择系统接管）、空格跳到焦点、Y 锁定跟随、Esc 暂停在 PauseManager。
/// V 仍可在四种模式间循环。
/// </summary>
public class DebugCameraSwitcher : MonoBehaviour
{
    /// <summary>场景内唯一实例（场景引用归资产所有者；这里只暴露运行时句柄，替代 FindObjectOfType）。</summary>
    public static DebugCameraSwitcher instance;

    [InspectorName("相机")]
    public Transform cam;
    [InspectorName("注视目标")]
    public Transform lookTarget;

    public bool IsDragging { get { return _dragging; } }

    [Header("俯视（默认，RTS 自由相机）")]
    [InspectorName("俯视偏移")]
    public Vector3 topOffset = new Vector3(0f, 9f, -5f);
    [InspectorName("俯视俯仰角")]
    [Range(30f, 85f)] public float topPitch = 55f;
    [InspectorName("俯视视野")]
    [Range(30f, 100f)] public float topFov = 50f;
    [InspectorName("旋转速度")]
    [Tooltip("Z/X 旋转速度（度/秒，暂停时也可用）")]
    public float rotateSpeed = 120f;
    [InspectorName("平移灵敏度")]
    [Tooltip("拖动平移灵敏度")]
    public float panScale = 0.05f;
    [InspectorName("允许平移")]
    [Tooltip("允许左键拖动平移（与选中状态无关，点击/拖动由 8px 阈值区分）")]
    public bool allowPan = true;
    [InspectorName("焦点目标")]
    [Tooltip("空格回中 / Y 锁定跟随的目标（暂时拖骑士，之后由选择系统设置）")]
    public Transform focusTarget;
    [InspectorName("瞄准约束")]
    public Behaviour aimConstraint;

    [Header("第一人称（实验性）")]
    [InspectorName("第一人称局部位置")]
    public Vector3 fpLocalPos = new Vector3(0f, 0.02f, -0.12f);
    [InspectorName("第一人称视野")]
    [Range(30f, 100f)] public float fpFov = 72f;

    [Header("第三人称·后上方")]
    [InspectorName("后方位局部位置")]
    public Vector3 backLocalPos = new Vector3(0f, 0.55f, -3.2f);
    [Header("第三人称·正面")]
    [InspectorName("前方位局部位置")]
    public Vector3 frontLocalPos = new Vector3(0f, 0.55f, 3f);
    [InspectorName("注视高度")]
    public float lookHeight = 0.8f;

    [Header("第一人称隐藏（PlayerBody 层）")]
    [InspectorName("身体层名")]
    public string bodyLayerName = "PlayerBody";
    [InspectorName("隐藏关键词")]
    public string[] hideKeywords = { "head", "helmet", "visor", "body", "cape", "belt", "torso", "beard" };

    int _mode;
    Camera _cam;
    int _bodyLayer;
    int _baseMask;

    Vector3 _shakeOffset;
    Coroutine _shakeCo;

    // 俯视状态
    float _topYaw;
    Vector3 _anchorPos;      // 自由相机锚点（空格/Y 会改它）
    Vector3 _panOffset;      // 平移累积偏移
    bool _lockFollow;        // Y 锁定
    Vector2 _dragStart;
    bool _dragging;
    bool _leftDragPan;       // 选择系统注入：选中单位时左键拖动也平移视角（超过阈值后置 true）

    /// <summary>由选择系统注入：左键拖拽平移视角进行中（选中单位时替代 allowPan 的左键）。</summary>
    public bool LeftDragPan { set { _leftDragPan = value; } }

    void Awake()
    {
        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Start()
    {
        if (lookTarget == null) lookTarget = transform;
        _cam = cam.GetComponent<Camera>();
        _baseMask = _cam.cullingMask;
        if (focusTarget == null) focusTarget = lookTarget;
        _anchorPos = lookTarget.position;

        _bodyLayer = LayerMask.NameToLayer(bodyLayerName);
        if (_bodyLayer == -1)
            Debug.LogError($"未找到图层 {bodyLayerName}：请先在 Layer 设置里添加它！");
        else
        {
            int count = 0;
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                string n = r.name.ToLower();
                foreach (var kw in hideKeywords)
                    if (n.Contains(kw)) { r.gameObject.layer = _bodyLayer; count++; break; }
            }
            Debug.Log($"{count} 个身体部件已挂到 {bodyLayerName} 层");
        }

        ApplyMode();
    }

    public void Shake(float duration, float strength)
    {
        if (_shakeCo != null) StopCoroutine(_shakeCo);
        _shakeCo = StartCoroutine(ShakeRoutine(duration, strength));
    }

    IEnumerator ShakeRoutine(float duration, float strength)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            _shakeOffset = (Vector3)(Random.insideUnitCircle * strength);
            yield return null;
        }
        _shakeOffset = Vector3.zero;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            _mode = (_mode + 1) % 4;
            ApplyMode();
        }

        if (_mode != 0) return;

        // 旋转（unscaled：暂停时也能转）
        if (Input.GetKey(KeyCode.Z)) _topYaw -= rotateSpeed * Time.unscaledDeltaTime;
        if (Input.GetKey(KeyCode.X)) _topYaw += rotateSpeed * Time.unscaledDeltaTime;

        // Y 锁定跟随
        if (Input.GetKeyDown(KeyCode.Y) && focusTarget != null)
            _lockFollow = !_lockFollow;

        // 空格：跳到焦点
        if (Input.GetKeyDown(KeyCode.Space) && focusTarget != null)
        {
            _anchorPos = focusTarget.position;
            _panOffset = Vector3.zero;
        }

        if (_lockFollow && focusTarget != null)
            _anchorPos = focusTarget.position;

        // 左键拖动平移（超过阈值才算拖动，否则视为"点击"留给选择系统；选中单位时由 LeftDragPan 接管）
        if (allowPan || _leftDragPan)
        {
            if (Input.GetMouseButtonDown(0))
            {
                _dragStart = Input.mousePosition;
                _dragging = false;
            }
            if (Input.GetMouseButton(0) && ((Vector2)Input.mousePosition - _dragStart).magnitude > 8f)
                _dragging = true;
            if (_dragging && Input.GetMouseButton(0))
            {
                Vector3 delta = new Vector3(-Input.GetAxis("Mouse X"), 0f, -Input.GetAxis("Mouse Y")) * panScale * 10f;
                _panOffset += Quaternion.Euler(0f, _topYaw, 0f) * delta;
            }
            if (Input.GetMouseButtonUp(0)) _dragging = false;
        }
    }

    void LateUpdate()
    {
        switch (_mode)
        {
            case 0:
                Vector3 rotatedOffset = Quaternion.Euler(0f, _topYaw, 0f) * topOffset;
                cam.position = _anchorPos + rotatedOffset + _panOffset + _shakeOffset;
                cam.rotation = Quaternion.Euler(topPitch, _topYaw, 0f);
                if (_cam != null) _cam.fieldOfView = topFov;
                break;
            case 1:
                cam.localPosition = fpLocalPos;
                if (_cam != null) _cam.fieldOfView = fpFov;
                break;
            case 2:
                cam.localPosition = backLocalPos;
                cam.localRotation = Quaternion.identity;
                break;
            case 3:
                Vector3 target = lookTarget.position + Vector3.up * lookHeight;
                cam.rotation = Quaternion.LookRotation(target - cam.position);
                break;
        }
    }

    void ApplyMode()
    {
        bool fp = _mode == 1;
        if (_cam != null && _bodyLayer != -1)
            _cam.cullingMask = fp ? (_baseMask & ~(1 << _bodyLayer)) : _baseMask;
        if (aimConstraint != null)
            aimConstraint.enabled = fp;
    }

    /// <summary>由选择系统调用：切换焦点角色。</summary>
    public void SetFocus(Transform t)
    {
        focusTarget = t != null ? t : lookTarget;
    }
}