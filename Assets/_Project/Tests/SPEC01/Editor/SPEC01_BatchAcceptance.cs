using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// SPEC-01 batchmode 主入口（唯一）。
/// 命令行：
///   Unity.exe -batchmode -projectPath &lt;repo&gt;
///     -executeMethod SPEC01_BatchAcceptance.Run
///     -logFile &lt;evidence&gt;/SPEC01_Automation_runN.log
///   环境变量 SPEC01_MODE=red|green；SPEC01_RUN=1|2|3。
/// Run：打开 DungeonWorld（不保存）→ 进入 Play → SPEC01_ProbeRunner 断言 → 退出并返回退出码。
/// 本入口不装配场景、不写生产资产；测试夹具全部在 Play 模式内存中创建并在结束前销毁。
/// 引导状态机（SessionState 标记 / 看门狗 / 退出分叉）统一在 <see cref="QaLauncher"/>，本类只做装配并转交。
/// </summary>
public static class SPEC01_BatchAcceptance
{
    public const string ScenePath = QaScene.ScenePath;
    public const string EnemyPrefabGuid = QaScene.EnemyPrefabGuid;

    public static void Run()
    {
        QaLaunchSpec spec = QaLauncher.SPEC01;
        QaLauncher.LogStart(spec);
        try
        {
            string head = QaScene.GetHead();
            SessionState.SetString("SPEC01.Head", head);
            Debug.Log("[SPEC01] head=" + head);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            string enemySourceGuid = QaScene.GetEnemySourceGuid(ScenePath);
            SessionState.SetString("SPEC01.EnemySourceGuid", enemySourceGuid);
            Debug.Log("[SPEC01] opened scene " + scene.path + "; loaded=" + scene.isLoaded +
                      "; dirty=" + scene.isDirty +
                      "; enemySourceGuid=" + enemySourceGuid +
                      "; mode=" + (Environment.GetEnvironmentVariable("SPEC01_MODE") ?? "green"));

            QaLauncher.Begin(spec);
        }
        catch (Exception e)
        {
            QaLauncher.Fail(spec, e);
        }
    }
}
