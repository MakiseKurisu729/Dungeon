using UnityEngine;

/// <summary>【薄的适配层】队伍级流程宿主：保留场景组件与旧的静态/实例入口，逻辑全部转发到 AppFlow / RunFlow。
/// 原职责已拆分：场景·暂停·重开见 AppFlow，团灭判定与 R 重开见 RunFlow。
/// 倒地表现/救援仍由 DownedRescue 负责。</summary>
public class GameFlow : MonoBehaviour
{
    public static GameFlow instance;
    public static bool GameStarted { get { return AppFlow.GameStarted; } set { AppFlow.GameStarted = value; } }

    public bool IsWiped { get { return RunFlow.IsWiped; } }

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        RunFlow.EnterScene();
    }

    void Update()
    {
        RunFlow.Tick();
    }
}
