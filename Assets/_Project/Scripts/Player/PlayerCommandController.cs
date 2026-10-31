using UnityEngine;

/// <summary>
/// 右键指令 + 动画驱动。Animator 自动查找（不做拖线，杜绝槽位错配）。
/// 持械常态层在移动时淡出（手臂自然摆动）。
/// 右键仅"主选单位"的控制器响应并代表全组下达移动；无人选中时右键无效。
/// </summary>
public class PlayerCommandController : MonoBehaviour
{
    public float turnSpeed = 12f;
    public LayerMask groundMask = ~0;

    [Header("持械常态层")]
    public int stanceLayer = 1;
    public float stanceBlendSpeed = 8f;

    CharacterBrain _brain;
    Animator _animator;

    void Start()
    {
        _brain = GetComponent<CharacterBrain>();
        _animator = GetComponentInChildren<Animator>();   // ★ 自动找，永不拖错
    }

    void Update()
    {
        if (_brain == null || _brain.agent == null) return;
        if (PauseManager.instance != null && PauseManager.instance.IsPaused) return;
        if (UIManager.instance != null && UIManager.instance.IsInventoryOpen) return;

        if (Input.GetMouseButtonDown(1))
        {
            if (SkillInputController.instance != null && SkillInputController.IsTargeting) return;
            var sm = SelectionManager.instance;
            // ★ 只有主选单位的控制器响应右键（代表全组下达）；没选中任何人 → 无人响应
            if (sm == null || sm.Primary == null) return;
            if (sm.Primary.GetComponentInParent<CharacterBrain>() != _brain) return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 300f, groundMask))
                sm.ForEachSelectedBrain(b => b.CommandMove(hit.point));
        }

        if (_animator == null) return;
        Vector3 vel = _brain.agent.velocity;
        float speed = new Vector3(vel.x, 0f, vel.z).magnitude;

        Vector3 local = transform.InverseTransformDirection(vel);
        _animator.SetFloat("SpeedX", local.x);
        _animator.SetFloat("SpeedZ", local.z);
        _animator.SetBool("Grounded", true);

        float target = speed > 0.3f ? 0f : 1f;
        float w = _animator.GetLayerWeight(stanceLayer);
        _animator.SetLayerWeight(stanceLayer,
            Mathf.Lerp(w, target, Time.deltaTime * stanceBlendSpeed));

        Vector3 face = new Vector3(vel.x, 0f, vel.z);
        if (face.sqrMagnitude > 0.1f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(face), Time.deltaTime * turnSpeed);
    }
}