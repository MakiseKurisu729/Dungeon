using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// REFACTOR 护栏套件的编辑器入口（batch 与编辑器内菜单共用同一套装配）。
/// 命令行：
///   Unity.exe -batchmode -projectPath &lt;repo&gt;
///     -executeMethod REFACTOR_BatchAcceptance.Run
///     -logFile &lt;evidence&gt;/REFACTOR_Automation_runN.log
///   环境变量 REFACTOR_RUN=1|2|3；REFACTOR_MILESTONE=M0..M4（缺省用 DefaultMilestone）。
/// 本入口不装配场景、不写生产资产；测试夹具全部在 Play 模式内存中创建并在结束前销毁。
/// 引导状态机（SessionState 标记 / 看门狗 / 退出分叉）统一在 <see cref="QaLauncher"/>，本类只做装配并转交。
/// </summary>
public static class REFACTOR_BatchAcceptance
{
    public const string ScenePath = "Assets/Scenes/DungeonWorld.unity";

    /// <summary>当前里程碑（证据目录 REFACTOR-M&lt;x&gt; 的 x）。每进一个里程碑改一次。</summary>
    public const string DefaultMilestone = "M4";

    public static void Run()
    {
        QaLaunchSpec spec = QaLauncher.REFACTOR;
        QaLauncher.LogStart(spec);
        try
        {
            string head = QaScene.GetHead();
            SessionState.SetString("REFACTOR.Head", head);
            Debug.Log("[REFACTOR] head=" + head);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("[REFACTOR] opened scene " + scene.path + "; loaded=" + scene.isLoaded +
                      "; dirty=" + scene.isDirty +
                      "; milestone=" + (Environment.GetEnvironmentVariable("REFACTOR_MILESTONE") ?? DefaultMilestone));

            QaLauncher.Begin(spec);
        }
        catch (Exception e)
        {
            QaLauncher.Fail(spec, e);
        }
    }
}
