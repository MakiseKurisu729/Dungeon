using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// DIAG-SKILL batchmode 唯一入口。
/// 命令行：
///   Unity.exe -batchmode -projectPath &lt;repo&gt;
///     -executeMethod DIAG_BatchAcceptance.Run
///     -logFile &lt;evidence&gt;/DIAG_probe.log
///   （可选环境变量 DIAG_MODE=probe）
/// 流程：打开 DungeonWorld（只读，不保存）→ 进入 Play → DIAG_ProbeRunner 断言 → 退出并返回退出码。
/// 不装配场景、不写任何生产资产；夹具全部在 Play 模式内存中创建并销毁。
/// 引导状态机（SessionState 标记 / 看门狗 / 退出分叉）统一在 <see cref="QaLauncher"/>，本类只做装配并转交。
/// </summary>
public static class DIAG_BatchAcceptance
{
    public const string ScenePath = "Assets/Scenes/DungeonWorld.unity";

    public static void Run()
    {
        QaLaunchSpec spec = QaLauncher.DIAG;
        QaLauncher.LogStart(spec);
        try
        {
            string head = QaScene.GetHead();
            SessionState.SetString("DIAG.Head", head);
            Debug.Log("[DIAG] head=" + head + "; mode=" + (Environment.GetEnvironmentVariable("DIAG_MODE") ?? "probe"));

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("[DIAG] opened scene " + scene.path + "; loaded=" + scene.isLoaded + "; dirty=" + scene.isDirty);

            QaLauncher.Begin(spec);
        }
        catch (Exception e)
        {
            QaLauncher.Fail(spec, e);
        }
    }
}
