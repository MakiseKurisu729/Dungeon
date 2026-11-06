using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// FND-00B-06 batchmode 主入口（唯一）。
/// 命令行：
///   Unity.exe -batchmode -projectPath &lt;repo&gt;
///     -executeMethod FND00B06_BatchAcceptance.Run
///     -logFile &lt;evidence&gt;/06_Automation_runN.log
///   环境变量 FND00B06_RUN=N 标记运行序号。
/// 流程：打开 DungeonWorld（只读，不保存）→ 进入 Play → FND00B06_ProbeRunner 执行 4→6 → 退出并返回退出码。
/// 不安装包、不修改生产代码/场景/预制体/ProjectSettings。
/// 引导状态机（SessionState 标记 / 看门狗 / 退出分叉）统一在 <see cref="QaLauncher"/>，本类只做装配并转交。
/// </summary>
public static class FND00B06_BatchAcceptance
{
    public const string ScenePath = "Assets/Scenes/DungeonWorld.unity";

    public static void Run()
    {
        QaLaunchSpec spec = QaLauncher.FND00B06;
        QaLauncher.LogStart(spec);
        try
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("[FND00B06] opened scene " + scene.path + "; loaded=" + scene.isLoaded +
                      "; dirty=" + scene.isDirty);

            QaLauncher.Begin(spec);
        }
        catch (Exception e)
        {
            QaLauncher.Fail(spec, e);
        }
    }
}
