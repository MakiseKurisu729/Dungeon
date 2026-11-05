// ⑦ DungeonDoor.cs
using UnityEngine;
using UnityEngine.AI;

/// <summary>地牢入口：实现 <see cref="IInteractionTarget"/>（唯一 KeyInteractable 目标）。
/// 提示与 F 键由输入层（SelectionManager）轮询/分发，本组件不再读 Input、不查 SelectionManager、不写提示；
/// 交互 = 黑屏过场 → 全员传送进地牢 → 镜头切到地牢上方。</summary>
public class DungeonDoor : MonoBehaviour, IInteractionTarget
{
    [Tooltip("地牢内落点（所有角色传送到这周围）")]
    public Transform dungeonEntryPoint;
    [Tooltip("镜头落点（地牢上方的空物体，位置≈地牢中心+高10米）")]
    public Transform cameraPoint;
    [Tooltip("可交互距离")]
    public float interactRange = 3.5f;

    bool _used;

    void OnEnable() { InteractionRunner.RegisterKeyTarget(this); }

    void OnDisable() { InteractionRunner.UnregisterKeyTarget(this); }

    // ===== IInteractionTarget =====

    public Transform Anchor { get { return transform; } }

    public string Hint { get { return "按 F 进入地牢"; } }

    public InteractionPriority Priority { get { return InteractionPriority.Default; } }

    public bool KeyInteractable { get { return true; } }

    public bool IsAvailable(CharacterBrain unit) { return !_used; }

    public float Radius(CharacterBrain unit) { return interactRange; }

    public void Interact(CharacterBrain unit)
    {
        if (_used) return;
        Enter();
    }

    void Enter()
    {
        _used = true;
        TransitionFader.Transition(() =>
        {
            int i = 0;
            foreach (var uh in UnitRegistry.OfTeam(Team.Player))
            {
                if (uh.dead) continue;
                Vector3 p = dungeonEntryPoint.position +
                            new Vector3((i % 2 == 0 ? -1.2f : 1.2f), 0, (i / 2) * 1.8f);
                var agent = uh.GetComponent<NavMeshAgent>();
                if (agent != null && agent.isOnNavMesh) agent.Warp(p);
                uh.transform.position = p;
                i++;
            }
            var cam = DebugCameraSwitcher.instance;
            if (cam != null && dungeonEntryPoint != null) cam.SetFocus(dungeonEntryPoint);
            var rig = Camera.main != null ? Camera.main.transform.root : null;
            if (rig != null && cameraPoint != null) rig.position = cameraPoint.position;
        });
    }
}