/// <summary>局内状态：金币 / 击杀。
/// 生命周期 = 进程内（跨场景重载保留），与重构前的 GameData 静态字段一致；本类不新增任何自动重置。</summary>
public static class RunState
{
    /// <summary>金币。默认 0；负值不额外裁剪（保持原字段语义）。</summary>
    public static int Gold { get; set; }

    /// <summary>击杀数。默认 0。</summary>
    public static int Kills { get; set; }

    /// <summary>只加钱，不触发消息事件（消息由调用方显式 SetMessage）。</summary>
    public static void AddGold(int g) { Gold += g; }

    /// <summary>只加击杀。</summary>
    public static void AddKill() { Kills++; }
}
