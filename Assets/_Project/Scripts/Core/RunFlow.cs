using UnityEngine;

/// <summary>局内结果：团灭判定 + R 重开。原 GameFlow.Start/Update 的逻辑主体。</summary>
public static class RunFlow
{
    /// <summary>本局是否已团灭（原 GameFlow 实例上的 IsWiped 属性）。</summary>
    public static bool IsWiped { get; private set; }

    /// <summary>进入场景（原 GameFlow.Start）：先清开局标志；存在玩家队伍则开局 + 恢复时间。
    /// 团灭标志随“新场景”复位（静态化后不会随旧 GameFlow 实例消失，等价于原实例字段默认 false）。
    /// 金币/等级/击杀/提示一律不清，保持跨场景重载保留的现状。</summary>
    public static void EnterScene()
    {
        IsWiped = false;
        AppFlow.GameStarted = false;

        // ★ 产品自动开局：本场景存在可操作队伍时直接进入局内（无主菜单步骤）。
        // 只有存在 team==Player 的 UnitHealth 才开局，避免无队伍场景（如 Playground）被误判团灭。
        if (AppFlow.AnyPlayerTeam())
        {
            AppFlow.GameStarted = true;
            Time.timeScale = 1f;
        }
    }

    /// <summary>每帧（原 GameFlow.Update）：团灭冻结 + R 重开。</summary>
    public static void Tick()
    {
        if (IsWiped)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                AppFlow.ReloadActiveScene();
            }
            return;
        }

        if (!AppFlow.GameStarted) return;

        bool anyStanding = false;
        foreach (var h in UnitRegistry.OfTeam(Team.Player))
        {
            var dr = h.GetComponent<DownedRescue>();
            bool down = dr != null && dr.IsDown;
            if (!h.dead && !down) { anyStanding = true; break; }
        }
        if (!anyStanding)
        {
            IsWiped = true;
            Time.timeScale = 0f;
            MessageFeed.SetMessage("全队覆没！按 R 重来");
        }
    }
}
