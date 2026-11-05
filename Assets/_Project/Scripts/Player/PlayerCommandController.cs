using UnityEngine;

/// <summary>右键指令 + 动画驱动（单层版）：Animator 自动查找。
/// 右键仅主选单位响应，代表全组下达移动；无人选中时右键无效。
/// 全身一体化动画：无上下体分层，移动即跑步姿态。</summary>
public class PlayerCommandController : MonoBehaviour
{
    public float turnSpeed = 12f;
    public LayerMask groundMask = ~0;

    CharacterBrain _brain;
    Animator _animator;

    void Start()
    {
        _brain = GetComponent<CharacterBrain>();
        _animator = GetComponentInChildren<Animator>();   // 自动找，永不拖错
    }

    void Update()
    {
        if (_brain == null || _brain.agent == null) return;
        if (AppFlow.IsPaused) return;
        if (UIManager.instance != null && UIManager.instance.IsInventoryOpen) return;

        if (Input.GetMouseButtonDown(1))
        {
            if (SkillInputController.instance != null && SkillInputController.IsTargeting) return;
            var sm = SelectionManager.instance;
            if (sm == null || sm.Primary == null) return;
            if (sm.Primary.GetComponentInParent<CharacterBrain>() != _brain) return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 300f, groundMask))
                sm.ForEachSelectedBrain(b => b.CommandMove(hit.point));
        }

        if (_animator == null) return;
        Vector3 vel = _brain.agent.velocity;
        Vector3 local = transform.InverseTransformDirection(vel);
        _animator.SetFloat("SpeedX", local.x);
        _animator.SetFloat("SpeedZ", local.z);
        _animator.SetBool("Grounded", true);

        Vector3 face = new Vector3(vel.x, 0f, vel.z);
        if (face.sqrMagnitude > 0.1f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(face), Time.deltaTime * turnSpeed);
    }
}