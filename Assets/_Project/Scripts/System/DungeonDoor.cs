// ⑦ DungeonDoor.cs
using UnityEngine;
using UnityEngine.AI;

/// <summary>地牢入口：主选角色靠近 → 提示按 F → 黑屏过场 → 全员传送进地牢 → 镜头切到地牢上方。</summary>
public class DungeonDoor : MonoBehaviour
{
    [Tooltip("地牢内落点（所有角色传送到这周围）")]
    public Transform dungeonEntryPoint;
    [Tooltip("镜头落点（地牢上方的空物体，位置≈地牢中心+高10米）")]
    public Transform cameraPoint;
    [Tooltip("可交互距离")]
    public float interactRange = 3.5f;

    bool _used;

    void Update()
    {
        if (_used || SelectionManager.instance == null || SelectionManager.instance.Primary == null) return;
        var t = SelectionManager.instance.Primary.transform;
        if (Vector3.Distance(t.position, transform.position) > interactRange) return;

        GameData.SetMessage("按 F 进入地牢");
        if (Input.GetKeyDown(KeyCode.F)) Enter();
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